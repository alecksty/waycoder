using System.Text;

namespace WayCoder;

/// <summary>
/// 模型管理核心逻辑 —— 供 /model 斜杠命令与 --model 命令行参数共用，
/// 返回纯文本，由调用方决定输出到屏幕（ChatScreen）还是控制台（Console）。
/// 覆盖：模型列表 / 选中（自动 base-url + 持久化）/ API key 管理。
/// </summary>
public static partial class ModelCli
{
    /// <summary>显示当前模型（大模型 / 小模型 / base-url）</summary>

    public static string Current()
    {
        var cfg = Config.Instance;
        var sb = new StringBuilder();
        sb.AppendLine($"当前大模型：{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.Provider), cfg.Model)}");
        sb.AppendLine($"当前小模型：{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.SmallProvider), cfg.SmallModel)}");
        var bigProv = ConnectionConfig.ResolveProvider(cfg.Provider);
        sb.AppendLine($"BaseUrl：{cfg.BaseUrl ?? bigProv?.BaseUrl ?? "?"}");
        var active = ConnectionConfig.CurrentByConfig();
        if (active != null)
            sb.AppendLine($"激活连接：{active.Name}（大={active.BigConnect} 小={active.SmallConnect}）");
        sb.AppendLine("\n列出目录: --model list　选大模型: --model name <id>　选小模型: --model small <id>　存 key: --model key <供应商> <key>");
        return sb.ToString();
    }

    /// <summary>设置连接地址（base-url），写入 .env 持久化</summary>
    public static string Connect(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "用法: --model connect <base-url>";
        Config.Instance.BaseUrl = baseUrl.Trim();
        Config.Instance.SaveToEnvFile();
        return $"BaseUrl 已设为 {baseUrl.Trim()}（已写入 .env）";
    }

    /// <summary>列出模型目录（按供应商分组，当前模型标注），可传关键词过滤</summary>
    public static string List(string? filter = null)
    {
        var models = string.IsNullOrWhiteSpace(filter)
            ? ModelCatalog.All
            : ModelCatalog.Search(filter);

        if (models.Length == 0)
            return "未找到匹配的模型。用 --model list 查看全部。";

        var current = Config.Instance.Model;
        var sb = new StringBuilder();
        sb.AppendLine($"模型目录（共 {models.Length} 个）：");

        // 第一列宽度：取所有模型短名的最大长度（+2 余量），避免名称溢出到第二列
        var nameWidth = 0;
        foreach (var m in models)
            nameWidth = Math.Max(nameWidth, ModelCatalog.ShortDisplayName(m.Id).Length);
        nameWidth = Math.Max(nameWidth + 2, 20);

        foreach (var g in models.GroupBy(m => ModelCatalog.ProviderDisplayName(m.ProviderId)))
        {
            sb.AppendLine();
            sb.AppendLine($"【{g.Key}】");
            foreach (var m in g.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                // 忙闲两种价格（有闲时价则显示「忙$in/out 闲$in/out」）
                var price = m.InputPrice > 0
                    ? (m.InputPriceOffpeak > 0
                        ? $"忙${m.InputPrice}/{m.OutputPrice} 闲${m.InputPriceOffpeak}/{m.OutputPriceOffpeak}"
                        : $"${m.InputPrice}/{m.OutputPrice}")
                    : "?";
                var ctx = FormatCtx(m.ContextWindow);
                var mark = m.Id == current ? "  ← 当前" : "";
                // 显示用短名（去 openrouter 类路由前缀），切换/调用仍用完整 id
                sb.AppendLine($"  {ModelCatalog.ShortDisplayName(m.Id).PadRight(nameWidth)} {ctx,-5}ctx  {price,-30}  [{m.Category}]{mark}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("选中: --model name <id> 或 --model <id>");
        sb.AppendLine("存 key: --model key <供应商> <key>　查 key: --model key");
        return sb.ToString();
    }

    /// <summary>检查当前模型或指定 connect 的能力特性（think / tools / vision / 格式 / 上下文 / key）。</summary>
    public static string Check(string? connectId = null)
    {
        string pid, mid;
        if (!string.IsNullOrWhiteSpace(connectId))
        {
            var c = ConnectionConfig.FindConnect(connectId.Trim());
            if (c == null) return $"❌ 未找到 connect「{connectId}」（--connect list 查看）";
            pid = c.ProviderId; mid = c.ModelId;
        }
        else
        {
            pid = Config.Instance.Provider; mid = Config.Instance.Model;
        }
        var caps = ModelCatalog.ResolveModelCallConstraints(mid, Config.Instance.BaseUrl);
        var fmt = ModelCatalog.ResolveApiFormat(mid, Config.Instance.BaseUrl);
        var hasKey = ApiKeyStore.Has(pid);
        var sb = new StringBuilder();
        sb.AppendLine($"模型: {ModelCatalog.ShortDisplayName(mid)}（{pid}）");
        sb.AppendLine($"  API 格式: {fmt}");
        sb.AppendLine($"  思考 think: {(caps.SupportsThinking ? "✅ 支持" : "❌ 不支持")}" +
            (caps.ReasoningEffortAllowed is { Length: > 0 } and not "none" ? $"（允许 {caps.ReasoningEffortAllowed}）" : ""));
        sb.AppendLine($"  工具 tools: {(caps.SupportsTools ? "✅ 支持" : "❌ 不支持")}");
        sb.AppendLine($"  视觉 vision: {(caps.SupportsVision ? "✅ 支持" : "❌ 不支持")}");
        sb.AppendLine($"  温度精度: {caps.TemperaturePrecision} 位小数");
        sb.AppendLine($"  上下文: {ModelCatalog.ResolveContextWindow(mid)} tokens");
        sb.AppendLine($"  API key: {(hasKey ? "🔑 已配置" : "⚠ 未配置（--model key 或设官方环境变量自动导入）")}");
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 交互确认（Y/N）：删除 key 等敏感操作必须询问用户。
    /// 非交互环境（管道/一次性模式/无终端）默认返回 false（保留，不删除）。
    /// </summary>
    private static bool ConfirmDelete(string prompt)
    {
        if (Console.IsInputRedirected || !Environment.UserInteractive) return false;
        Console.Write($"{prompt} [y/N] ");
        try
        {
            var r = Console.ReadLine()?.Trim().ToLowerInvariant();
            return r is "y" or "yes";
        }
        catch { return false; }
    }

    /// <summary>解析可选超时参数（秒），非法/空返回默认 60。</summary>
    private static int ParseTimeout(string? arg, int fallback = 60)
    {
        if (int.TryParse(arg, out var s) && s is > 0 and <= 600) return s;
        return fallback;
    }

    /// <summary>探测单个模型连通性：发一个简单 chat 请求，返回 (可用, 说明/原因)。</summary>

    public static string Clean()
    {
        var sb = new StringBuilder();
        var all = ModelCatalog.All.ToList();  // 只读快照，仅用于遍历标记

        // 遍历时只做标记，统一在最后删除（不能一边遍历一边删除）
        var toRemoveModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);  // 模型 id
        var toRemoveConnects = new List<string>();                                   // connect 名
        int merged = 0, removedProv = 0, removedConn = 0;

        // 1. 合并重复（同 Id + DefaultBaseUrl，大小写不敏感）
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in all)
        {
            var key = $"{m.ProviderId}|{m.Id}|{m.DefaultBaseUrl ?? ""}";
            if (!seen.Add(key)) { toRemoveModels.Add(m.Id); merged++; }
        }

        // 2. 标记不存在的服务商模型（ProviderId 不在 Providers 注册表）
        foreach (var m in all)
        {
            if (!ModelCatalog.Providers.ContainsKey(m.ProviderId))
            {
                toRemoveModels.Add(m.Id);
                removedProv++;
            }
        }

        // 3. 标记不存在的模型 connect（模型在目录找不到）
        foreach (var c in ConnectionConfig.ListConnects())
        {
            bool bad = !ModelCatalog.Providers.ContainsKey(c.ProviderId)
                || ModelCatalog.Find(c.ModelId, null) == null;
            if (bad) { toRemoveConnects.Add(c.Name); removedConn++; }
        }

        // 统一删除（遍历完成后一起删；HashSet 自动去重，同一模型不会删两次）
        foreach (var id in toRemoveModels) ModelCatalog.RemoveCustom(id);
        foreach (var name in toRemoveConnects) ConnectionConfig.RemoveConnect(name, out _);

        // 供应商地址去重（同地址 = 同供应商）：归并重复地址的供应商并移除
        var mergedProv = ModelCatalog.DeduplicateProviders();

        sb.AppendLine($"✅ 清理完成：合并重复 {merged} 个，删除无效服务商模型 {removedProv} 个，删除无效 connect {removedConn} 个" +
            (mergedProv > 0 ? $"，归并重复地址供应商 {mergedProv} 个" : ""));
        if (merged + removedProv + removedConn == 0)
            sb.AppendLine("（模型目录与 connect 已干净，无需清理）");
        return sb.ToString().TrimEnd();
    }

    /// <summary>选中模型：按目录解析，自动设置 base-url，经 connect 统一入口持久化</summary>
    public static string Select(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            ConnectionConfig.ApplyModelChoice(Config.Instance.Provider, modelId.Trim(), isLarge: true, out _);
            return $"已切换至 **{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.Provider), modelId.Trim())}** 模型（目录外模型，已写入 .env）。若非 OpenAI 兼容端点请另行 --config set BaseUrl <url>";
        }

        ConnectionConfig.ApplyModelChoice(info.ProviderId, info.Id, isLarge: true, out _, info.DefaultBaseUrl);

        var keyHint = info.DefaultBaseUrl != null
            && !ApiKeyStore.Has(info.ProviderId)
            && info.ProviderId is not ("openai" or "local" or "custom")
            ? $"\n  该供应商需 API key：--model key {info.ProviderId} <key>"
            : "";

        return $"已切换至 **{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(info.ProviderId), info.Id)}** 模型并写入 .env" +
            (info.DefaultBaseUrl != null ? $"\n  BaseUrl 已自动设为 {info.DefaultBaseUrl}" : "") + keyHint;
    }

    /// <summary>选中小模型：按目录解析，经 connect 统一入口持久化（同步小模型服务商）</summary>
    public static string SelectSmall(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            ConnectionConfig.ApplyModelChoice(Config.Instance.SmallProvider, modelId.Trim(), isLarge: false, out _);
            return $"已切换至 **{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.SmallProvider), modelId.Trim())}** 小模型（目录外模型，已写入 .env）";
        }

        ConnectionConfig.ApplyModelChoice(info.ProviderId, info.Id, isLarge: false, out _);

        return $"已切换至 **{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(info.ProviderId), info.Id)}** 小模型并写入 .env";
    }

    /// <summary>列出已保存的 API keys（打码 + 有效期）</summary>

    public static string Remove(string modelId)
    {
        var removed = ModelCatalog.RemoveCustom(modelId.Trim());
        return removed.Length > 0
            ? $"已删除自定义模型 `{modelId}`（从 {string.Join("、", removed)} 移除）"
            : $"未找到自定义模型 `{modelId}`（内置模型不可删除，可被自定义覆盖）。";
    }

    /// <summary>删除某服务商下的所有自定义模型（内置供应商不可删）</summary>
    public static string RemoveProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return "用法: --model remove provider <供应商ID>";
        var n = ModelCatalog.RemoveCustomByProvider(providerId.Trim());
        return n > 0
            ? $"已删除服务商 `{providerId.Trim()}` 的 {n} 个自定义模型（内置模型不可删）。"
            : $"未找到服务商 `{providerId.Trim()}` 的自定义模型（内置供应商不可删，可被自定义覆盖）。";
    }

    /// <summary>删除某服务商的 API key（从全局 api_keys.json 移除）</summary>
    public static string RemoveKey(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return "用法: --model remove key <供应商ID>";
        if (!ApiKeyStore.Has(providerId.Trim()))
            return $"未找到服务商 `{providerId.Trim()}` 的 API key。";
        ApiKeyStore.Remove(providerId.Trim());
        return $"已删除服务商 `{providerId.Trim()}` 的 API key。";
    }

    /// <summary>手动添加一个自定义模型（id + 供应商ID + 可选 baseUrl），写入全局模型库</summary>
    public static string AddModel(string id, string? providerId = null, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "用法: --model add model <id> [<供应商ID> [baseUrl]]";
        // 不指定服务商 → 当前 provider（Config.Provider）；否则规范化指定 id
        var pid = ModelCatalog.NormalizeId(string.IsNullOrWhiteSpace(providerId) ? Config.Instance.Provider : providerId);
        if (pid.Length == 0) pid = "custom";
        var display = ModelCatalog.ProviderDisplayName(pid);
        // 未指定 baseUrl → 用服务商默认地址
        var effBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? (ModelCatalog.Providers.TryGetValue(pid, out var pp) && !string.IsNullOrEmpty(pp.DefaultBaseUrl) ? pp.DefaultBaseUrl : null)
            : baseUrl.Trim();
        var info = new ModelCatalog.ModelInfo(id.Trim(), id.Trim(), display, pid, "*", "Custom",
            0, 0, 0, effBaseUrl, $"手动添加（{pid}）", 0);
        var path = ModelCatalog.AddCustom(info);
        return $"已添加模型 `{id.Trim()}`（服务商 `{pid}`）到 {path}";
    }

    /// <summary>手动添加一个服务商（注册为同名模型条目，携带可选 baseUrl），写入全局模型库</summary>
    public static string AddProvider(string providerId, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return "用法: --model add provider <供应商ID> [baseUrl]";
        var pid = providerId.Trim();
        var info = new ModelCatalog.ModelInfo(pid, pid, pid, pid.ToLowerInvariant(), "*", "Custom",
            0, 0, 0, string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim(), $"手动添加服务商（{pid}）", 0);
        var path = ModelCatalog.AddCustom(info);
        return $"已添加服务商 `{pid}`" +
            (string.IsNullOrWhiteSpace(baseUrl) ? "" : $"（base_url {baseUrl.Trim()}）") +
            $" → {path}";
    }

    /// <summary>连通性探测任务。</summary>
    private sealed record ProbeTarget(string ProviderId, string Display, string? BaseUrl, string? Key,
        string[] Models, bool IsLocal);

    /// <summary>
    /// 并发探测一组端点（每项独立 HttpClient、4s 超时），保持传入顺序返回结果。
    /// 顺序探测 N 个端点累计 N×超时；并发后总耗时 ≈ 最慢单端点超时，
    /// 显著加快 --model test 与 /models/scan（自测最慢项从 ~15s 降到 ~4s）。
    /// </summary>

    private static string? EffectiveBaseUrl(ModelCatalog.ModelInfo m)
    {
        // 注册表地址 > model 默认地址（复用核心解析），本地服务兜底 localhost 是探测场景专属增量（MAUI/Web 无）
        var url = ModelCatalog.ResolveBaseUrl(m, m.ProviderId, null);
        if (!string.IsNullOrWhiteSpace(url)) return url;
        if (m.ProviderId is "ollama" or "local") return "http://localhost:11434";
        return null;
    }

    /// <summary>解析服务商的 base_url：注册表 > 目录内该服务商模型 > null（无法解析）。
    /// 注册表查询复用 <see cref="ModelCatalog.BaseUrlOf"/>（providers.json 实时覆盖 + 大小写不敏感），目录模型默认走 <see cref="EffectiveBaseUrl"/>。</summary>
    private static string? ResolveProviderBaseUrl(string providerId)
    {
        var pid = providerId.Trim().ToLowerInvariant();
        var reg = ModelCatalog.BaseUrlOf(pid);
        if (!string.IsNullOrEmpty(reg)) return reg;
        var m = ModelCatalog.All.FirstOrDefault(x => x.ProviderId.Equals(pid, StringComparison.OrdinalIgnoreCase));
        return m != null ? EffectiveBaseUrl(m) : null;
    }

    /// <summary>探测一个 OpenAI 兼容端点：GET {base}/models（401/403=密钥无效，404/405 时回退 /v1/models）</summary>
    internal static (bool Ok, string Detail) ProbeEndpoint(string? baseUrl, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return (false, "无端点（未配置 base_url）");
        try
        {
            var b = ModelCatalog.NormalizeBaseUrl(baseUrl);
            var urls = new List<string> { b + "/models" };
            if (!b.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                urls.Add(b + "/v1/models");

            var gotHttpResponse = false;
            foreach (var url in urls)
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
                    using var resp = client.SendAsync(req).GetAwaiter().GetResult();
                    gotHttpResponse = true;
                    var code = (int)resp.StatusCode;
                    if (code >= 200 && code < 300) return (true, $"已连接（{code}）");
                    if (code is 401 or 403) return (false, $"密钥无效（{code}）");
                    if (code is 404 or 405) continue;  // 该路径不存在，试 /v1/models
                    return (false, $"HTTP {code}");
                }
                catch { break; } // 网络层失败（超时/拒绝）：同一主机第二个 URL 结果相同，直接放弃，
                                 // 避免不可达端点每个 URL 各耗满 4s（两 URL 共 8s）拖慢 --model test / /models/scan
            }
            return gotHttpResponse
                ? (false, "端点可达但无 /models 接口（可能非 OpenAI 兼容端点）")
                : (false, "无法连接（超时/拒绝）");
        }
        catch { return (false, "无法连接"); }
    }

    // 上下文整数 → 文本统一走 Global.FormatContext（- / 128K / 1.1M）；此处 0 值随全局显示「-」
    private static string FormatCtx(int ctx) => Global.FormatContext(ctx);
}
