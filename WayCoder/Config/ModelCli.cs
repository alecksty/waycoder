using System.Text;

namespace WayCoder;

/// <summary>
/// 模型管理核心逻辑 —— 供 /model 斜杠命令与 --model 命令行参数共用，
/// 返回纯文本，由调用方决定输出到屏幕（ChatScreen）还是控制台（Console）。
/// 覆盖：模型列表 / 选中（自动 base-url + 持久化）/ API key 管理。
/// </summary>
public static class ModelCli
{
    /// <summary>显示当前模型（大模型 / 小模型 / base-url）</summary>
    public static string Current()
    {
        var cfg = Config.Instance;
        var sb = new StringBuilder();
        sb.AppendLine($"当前大模型：{ConnectionConfig.FormatModel(cfg.Provider, cfg.Model)}");
        sb.AppendLine($"当前小模型：{ConnectionConfig.FormatModel(cfg.SmallProvider, cfg.SmallModel)}");
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

        foreach (var g in models.GroupBy(m => m.Provider))
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
                var ctx = m.ContextWindow > 0
                    ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                    : "?";
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
    private static (bool Ok, string Detail) ProbeChat(string providerId, string modelId, string baseUrl, int timeoutSec = 60)
    {
        try
        {
            var key = ApiKeyStore.Get(providerId) ?? "";
            // timeoutSeconds 固定单次超时（不渐进加长重试）：连通性探测要快速可控，别被全局重试链拖住
            var llm = new LLM(modelId, key, baseUrl, maxTokens: 16, timeoutSeconds: timeoutSec);
            var resp = llm.ChatAsync(
                new List<JNode> { JNode.Object().Set("role", "user").Set("content", "只回复两个字：ok") },
                cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec)).Token
            ).GetAwaiter().GetResult();
            var content = (resp.Content ?? "").Trim();
            if (resp.IsFatalError) return (false, "致命错误");
            if (content.Length > 0) return (true, "回复: " + content[..Math.Min(content.Length, 20)]);
            if (resp.ToolCalls.Count > 0) return (true, "工具调用");
            // think 模型：内容在 reasoning_content（不并入 Content），有思考即算连通，别误判为不可用
            if (resp.ReasoningTokens > 0) return (true, $"思考 {resp.ReasoningTokens} tok");
            return (false, "空回复（模型可能只思考不输出）");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 测试所有 connect 的模型连通性，生成报告：
    /// 遍历每个 connect（有 key 的 + 本地无需 key 的），发简单请求，列出可用 / 失败原因。
    /// </summary>
    public static string Report(string? timeoutArg = null)
    {
        var connects = ConnectionConfig.ListConnects();
        if (connects.Count == 0) return "暂无 connect（--connect add <name> <providerId> <modelId> 添加）";
        var timeout = ParseTimeout(timeoutArg);
        var sb = new StringBuilder($"模型连通性报告（{connects.Count} 个 connect，单模型超时 {timeout}s）：\n");
        var seen = new HashSet<(string, string)>();
        int ok = 0, fail = 0, skip = 0, total = 0;
        foreach (var c in connects)
        {
            if (!seen.Add((c.ProviderId, c.ModelId))) continue;
            total++;
            var prov = ConnectionConfig.ResolveProvider(c.ProviderId);
            var baseUrl = prov?.BaseUrl ?? ModelCatalog.Find(c.ModelId, null)?.DefaultBaseUrl ?? "";
            // 本地服务（localhost/127.0.0.1）无需 key；Ollama 云端（ollama.com）也要 key——按地址判断，不看 providerId
            var isLocal = ModelCatalog.IsLocalUrl(baseUrl);
            var hasKey = ApiKeyStore.Has(c.ProviderId);
            if (!isLocal && !hasKey)
            {
                skip++;
                sb.AppendLine($"  ⏭ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）无 key");
                continue;
            }
            // 实时进度（stderr，不污染报告）：让用户知道正在扫哪个
            Console.Error.WriteLine($"正在测试 [{c.ProviderId}] 第 {total}/{connects.Count} 个（{ModelCatalog.ShortDisplayName(c.ModelId)}）...");
            var (ok2, detail) = ProbeChat(c.ProviderId, c.ModelId, baseUrl, timeout);
            if (ok2) { ok++; sb.AppendLine($"  ✅ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）{detail}"); }
            else { fail++; sb.AppendLine($"  ❌ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）{detail}"); }
        }
        sb.AppendLine($"\n汇总：✅ {ok} 可用　❌ {fail} 失败　⏭ {skip} 跳过(无key)");
        return sb.ToString();
    }

    /// <summary>
    /// 切换免费模型前记住的模型（/free-restore / --model restore 恢复）。
    /// 持久化到 config.json（freePrevProvider/Model/BaseUrl）：跨会话可恢复（CLI 一次性进程也能还原）。
    /// </summary>
    public static (string Provider, string Model, string? BaseUrl)? PreviousModel
    {
        get
        {
            var c = Config.Instance;
            if (string.IsNullOrEmpty(c.FreePrevModel)) return null;
            return (c.FreePrevProvider ?? "", c.FreePrevModel, c.FreePrevBaseUrl);
        }
        set
        {
            var c = Config.Instance;
            if (value is { } v)
            {
                c.FreePrevProvider = v.Provider;
                c.FreePrevModel = v.Model;
                c.FreePrevBaseUrl = v.BaseUrl;
            }
            else
            {
                c.FreePrevProvider = null;
                c.FreePrevModel = null;
                c.FreePrevBaseUrl = null;
            }
            c.SaveToConfigJson(); // 跨会话持久化
        }
    }

    /// <summary>记住当前模型（切换免费模型前调用；未记录才记，避免覆盖已记住的）。</summary>
    public static void RememberCurrentModel()
    {
        if (PreviousModel == null)
            PreviousModel = (Config.Instance.Provider, Config.Instance.Model, Config.Instance.BaseUrl);
    }

    /// <summary>恢复 /free 切换前的模型（三端共用：TUI /free-restore、Web /free-restore、CLI --model restore）。</summary>
    public static string RestorePrevious()
    {
        if (PreviousModel is not { } prev) return "⚠️ 无之前模型可恢复（先切换免费模型）";
        ConnectionConfig.ApplyModelChoice(prev.Provider, prev.Model, isLarge: true, out var msg, prev.BaseUrl);
        PreviousModel = null;
        return $"✅ 已恢复之前模型：{ModelCatalog.ShortDisplayName(prev.Model)}（{prev.Provider}）";
    }

    /// <summary>枚举模型库中所有 free 模型（id 含 free，同 provider+id 去重）。</summary>
    public static List<ModelCatalog.ModelInfo> EnumerateFreeModels()
        => ModelCatalog.All
            .Where(m => m.Id.ToLowerInvariant().Contains("free"))
            .GroupBy(m => $"{m.ProviderId}|{m.Id.ToLowerInvariant()}")   // 同 provider+id 去重
            .Select(g => g.First())
            .ToList();

    /// <summary>免费可用 connect（/free 弹窗切换用，持久化到 free.json）。</summary>
    public sealed record FreeConnect(string ProviderId, string ModelId, string? BaseUrl);

    /// <summary>免费可用列表缓存路径（~/.waycoder/free.json，--model free 扫描生成，/free 直接读不重扫）。</summary>
    public static string FreeJsonPath => Global.GlobalConfigPath("free.json");

    /// <summary>保存可用免费 connect 列表到 free.json（--model free 扫描完写入）。</summary>
    public static void SaveFreeJson(List<FreeConnect> items)
    {
        try
        {
            var arr = JNode.Array();
            foreach (var c in items)
                arr.Add(JNode.Object()
                    .Set("providerId", JNode.From(c.ProviderId))
                    .Set("modelId", JNode.From(c.ModelId))
                    .Set("baseUrl", JNode.From(c.BaseUrl ?? "")));
            var root = JNode.Object().Set("free", arr);
            var path = FreeJsonPath;
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, Json.Serialize(root, indent: true));
            File.Move(tmp, path, overwrite: true); // 原子替换
        }
        catch { /* 写失败不阻塞 */ }
    }

    /// <summary>读取 free.json 缓存的免费可用 connect 列表（空 = 尚未扫描生成）。</summary>
    public static List<FreeConnect> LoadFreeJson()
    {
        var result = new List<FreeConnect>();
        try
        {
            var path = FreeJsonPath;
            if (!File.Exists(path)) return result;
            var root = Json.Parse(File.ReadAllText(path));
            if (root is not { Kind: JKind.Object }) return result;
            var arr = root["free"];
            if (arr == null) return result;
            foreach (var item in arr.Items)
            {
                var pid = item["providerId"]?.AsString();
                var mid = item["modelId"]?.AsString();
                if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(mid)) continue;
                result.Add(new FreeConnect(pid, mid, item["baseUrl"]?.AsString()));
            }
        }
        catch { /* 损坏静默忽略 */ }
        return result;
    }

    /// <summary>
    /// 测试模型库中所有 free 模型（id 含 free 的 openrouter :free / opencode zen -free），列出可用的，
    /// 并把可用列表写入 free.json（/free 之后直接读缓存弹窗，不再每次扫描）。
    /// </summary>
    public static string Free(string? timeoutArg = null)
    {
        var freeModels = EnumerateFreeModels();
        if (freeModels.Count == 0)
            return "模型库中没有 free 模型（--model import online opencode-zen / openrouter 导入后测试）";
        // 默认每模型 5s：没回复就跳过（连通性探测，不拖沓）。可传参覆盖（--model free <秒>）
        var timeout = ParseTimeout(timeoutArg, fallback: 5);
        var sb = new StringBuilder($"Free 模型连通性测试（{freeModels.Count} 个，单模型超时 {timeout}s，没回复即跳过）：\n");
        int ok = 0, fail = 0, skip = 0, total = 0;
        var okList = new List<FreeConnect>();
        foreach (var m in freeModels)
        {
            total++;
            var key = ApiKeyStore.Get(m.ProviderId);
            if (string.IsNullOrEmpty(key))
            {
                skip++;
                sb.AppendLine($"  ⏭ {ModelCatalog.ShortDisplayName(m.Id)}（{m.ProviderId}）无 key");
                continue;
            }
            // 实时进度（stderr）：让用户知道正在扫哪个 provider 第几个
            Console.Error.WriteLine($"正在扫描 [{m.ProviderId}] 第 {total}/{freeModels.Count} 个（{ModelCatalog.ShortDisplayName(m.Id)}）...");
            var baseUrl = m.DefaultBaseUrl ?? ConnectionConfig.ResolveProvider(m.ProviderId)?.BaseUrl ?? "";
            var (ok2, detail) = ProbeChat(m.ProviderId, m.Id, baseUrl, timeout);
            if (ok2)
            {
                ok++;
                okList.Add(new FreeConnect(m.ProviderId, m.Id, string.IsNullOrEmpty(baseUrl) ? null : baseUrl));
                // 增量持久化：每扫到可用立即写 free.json——28 个模型逐个探测可能较久，
                // 中途 Ctrl+C / 超时也能用已扫到的可用项（下次 /free 直接读缓存）
                SaveFreeJson(okList);
                sb.AppendLine($"  ✅ {ModelCatalog.ShortDisplayName(m.Id)}（{m.ProviderId}）{detail}");
            }
            else { fail++; sb.AppendLine($"  ❌ {ModelCatalog.ShortDisplayName(m.Id)}（{m.ProviderId}）{detail}"); }
        }
        if (okList.Count > 0) SaveFreeJson(okList); // 最终全量（幂等）
        sb.AppendLine($"\n汇总：可用 {ok}　失败 {fail}　跳过(无key) {skip}");
        if (okList.Count > 0)
            sb.AppendLine($"已把 {okList.Count} 个可用免费模型写入 free.json（/free 直接读缓存，不再重复扫描；重新扫描跑 --model free）");
        return sb.ToString();
    }

    /// <summary>
    /// 清理脏数据：
    /// 1. 合并重复模型（同 id + baseUrl 只保留一个）
    /// 2. 删除「不存在的服务商」模型（ProviderId 不在注册表）
    /// 3. 删除「不存在的模型」connect（模型在目录中找不到）
    /// </summary>
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

        sb.AppendLine($"✅ 清理完成：合并重复 {merged} 个，删除无效服务商模型 {removedProv} 个，删除无效 connect {removedConn} 个");
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
            return $"已切换至 **{ConnectionConfig.FormatModel(Config.Instance.Provider, modelId.Trim())}** 模型（目录外模型，已写入 .env）。若非 OpenAI 兼容端点请另行 --config set BaseUrl <url>";
        }

        ConnectionConfig.ApplyModelChoice(info.ProviderId, info.Id, isLarge: true, out _, info.DefaultBaseUrl);

        var keyHint = info.DefaultBaseUrl != null
            && !ApiKeyStore.Has(info.ProviderId)
            && info.ProviderId is not ("openai" or "local" or "custom")
            ? $"\n  该供应商需 API key：--model key {info.ProviderId} <key>"
            : "";

        return $"已切换至 **{ConnectionConfig.FormatModel(info.ProviderId, info.Id)}** 模型（{info.DisplayName}）并写入 .env" +
            (info.DefaultBaseUrl != null ? $"\n  BaseUrl 已自动设为 {info.DefaultBaseUrl}" : "") + keyHint;
    }

    /// <summary>选中小模型：按目录解析，经 connect 统一入口持久化（同步小模型服务商）</summary>
    public static string SelectSmall(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            ConnectionConfig.ApplyModelChoice(Config.Instance.SmallProvider, modelId.Trim(), isLarge: false, out _);
            return $"已切换至 **{ConnectionConfig.FormatModel(Config.Instance.SmallProvider, modelId.Trim())}** 小模型（目录外模型，已写入 .env）";
        }

        ConnectionConfig.ApplyModelChoice(info.ProviderId, info.Id, isLarge: false, out _);

        return $"已切换至 **{ConnectionConfig.FormatModel(info.ProviderId, info.Id)}** 小模型（{info.DisplayName}）并写入 .env";
    }

    /// <summary>列出已保存的 API keys（打码 + 有效期）</summary>
    public static string ListKeys()
    {
        var entries = ApiKeyStore.ListAllEntries();
        if (entries.Count == 0)
            return "未保存任何 API key。用 --model key <供应商> <key> [有效期] 保存。";

        var sb = new StringBuilder();
        sb.AppendLine("已保存 API keys：");
        var expired = 0;
        var expiringSoon = 0;
        foreach (var (pid, entry) in entries)
        {
            var expiryText = ApiKeyStore.ExpiryText(entry.Expiry);
            if (ApiKeyStore.IsExpired(entry.Expiry)) expired++;
            else if (ApiKeyStore.DaysLeft(entry.Expiry) <= 7) expiringSoon++;
            sb.AppendLine($"  {pid,-12} = {ApiKeyStore.Masked(pid),-30}有效期: {expiryText}");
        }
        if (expired > 0) sb.AppendLine($"⚠ {expired} 个 key 已过期，请及时更换");
        if (expiringSoon > 0) sb.AppendLine($"⚠ {expiringSoon} 个 key 临近到期（≤7 天）");
        sb.AppendLine("设置/修改有效期：--model key expiry <供应商> <有效期>");
        return sb.ToString();
    }

    /// <summary>保存指定供应商的 API key（可选有效期：永久 / 截止日期）</summary>
    public static string SetKey(string providerId, string key, string? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(key))
            return "用法: --model key <供应商> <key> [有效期]";
        ApiKeyStore.Set(providerId, key, expiry);
        return $"已保存 {providerId} 的 API key：{ApiKeyStore.Masked(providerId)}（有效期: {ApiKeyStore.ExpiryText(expiry)}）";
    }

    /// <summary>给已存 key 设置/修改有效期（不改动 key 本身）。</summary>
    public static string SetKeyExpiry(string providerId, string? expiry)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return "用法: --model key expiry <供应商> <有效期>";
        if (!ApiKeyStore.Has(providerId))
            return $"服务商 {providerId} 未保存 key，先用 --model key <供应商> <key> 保存";
        ApiKeyStore.SetExpiry(providerId, expiry);
        return $"已设置 {providerId} 的 API key 有效期：{ApiKeyStore.ExpiryText(expiry)}";
    }

    /// <summary>
    /// 导入外部模型数据库（OpenCode / OpenClaw / Crush / Claude Code / Codex / 通用 JSON 文件 / 内置目录），写入全局模型库。
    /// source: null/auto/all=自动探测全部；逗号分隔多来源（opencode,codex,claude）；单来源；builtin=恢复被清空的内置目录；否则视为文件路径。
    /// </summary>
    public static string Import(string? source = null, Action<string>? onProgress = null)
    {
        var home = Global.Home;
        var imported = new List<ModelCatalog.ModelInfo>();
        var reports = new List<string>();
        bool restoredBuiltIn = false;

        // 解析来源列表：null/auto/all → 全部；否则按逗号拆分（支持「本地导入」勾选多来源）
        var s = source?.Trim();
        var sources = string.IsNullOrWhiteSpace(s) || s.ToLowerInvariant() is "auto" or "all"
            ? new[] { "opencode", "openclaw", "crush", "claude", "codex" }.ToList()
            : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        string? localServiceReport = null;

        foreach (var raw in sources)
        {
            var src = raw.ToLowerInvariant();
            onProgress?.Invoke($"🔍 正在导入 {raw} ...");
            if (src == "builtin")
            {
                ModelCatalog.RestoreBuiltIn();
                restoredBuiltIn = true;
                continue;
            }
            if (src is "ollama" or "lmstudio" or "cc-switch")
            {
                // 本地服务（Ollama/LM Studio/CC Switch）从本地官方接口实时拉取真实模型
                localServiceReport = ImportLocalServices(onProgress);
                continue;
            }
            if (src is "opencode" or "openclaw" or "crush" or "claude" or "claudecode" or "codex")
            {
                imported.AddRange(ImportOne(src, home, reports));
                continue;
            }
            // 文件路径（支持 JSONC/JSON5 注释与裸 key；.toml 按 Codex 解析）
            if (!File.Exists(raw))
                return $"❌ 未找到文件: {raw}";
            var text = File.ReadAllText(raw);
            var list = raw.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)
                ? ModelCatalog.ImportCodex(text)
                : ModelCatalog.ImportFromJson(ModelCatalog.NormalizeJson5(text));
            imported.AddRange(list);
            reports.Add($"文件 {raw}");
        }

        // 第三方数据库（Crush/OpenCode 等）里的本地模型条目（ollama/lmstudio/local）是静态假数据——
        // 识别并全部过滤；真实本地模型由 ImportLocalServices 从本地服务接口实时拉取（Ollama /api/tags、LM Studio /v1/models）
        var fakeLocal = imported.Where(m => m.ProviderId is "ollama" or "lmstudio" or "local").ToList();
        if (fakeLocal.Count > 0)
            imported = imported.Where(m => m.ProviderId is not "ollama" and not "lmstudio" and not "local").ToList();

        var sb = new StringBuilder();
        if (restoredBuiltIn)
            sb.AppendLine("✅ 已恢复内置模型目录（清空标记清除）");
        if (localServiceReport != null)
            sb.AppendLine(localServiceReport);

        if (imported.Count == 0)
            return sb.Length > 0
                ? sb.ToString().Trim()
                : "❌ 未导入任何模型（未找到可识别的模型配置）。\n   支持: --model import [builtin|opencode|openclaw|crush|claude|codex|ollama|lmstudio|cc-switch|<配置文件路径>]";

        // 去重：同一 (Id, baseUrl) 只保留第一个（地址不同=不同服务商，同 id 不同地址都保留）
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        imported = imported.Where(m => seenIds.Add(ModelCatalog.ModelKey(m.ProviderId, m.Id))).ToList();

        // 跳过内置：仅当同 id 且同 baseUrl（地址不同视为不同服务商，不跳过）
        var builtInIds = new HashSet<string>(
            ModelCatalog.BuiltIn.Select(m => ModelCatalog.ModelKey(m.ProviderId, m.Id)),
            StringComparer.OrdinalIgnoreCase);
        var added = new List<ModelCatalog.ModelInfo>();
        var skipped = new List<string>();
        foreach (var m in imported)
        {
            if (builtInIds.Contains(ModelCatalog.ModelKey(m.ProviderId, m.Id)))
                skipped.Add(m.Id);
            else
                added.Add(m);
        }
        ModelCatalog.AddCustomRange(added);
        RegisterImportProviders(added); // 批量一次写（防 N 次磁盘写）

        sb.AppendLine($"✅ 导入 {added.Count} 个模型到全局模型库（{string.Join("、", reports)}）" +
            (skipped.Count > 0 ? $"，跳过 {skipped.Count} 个内置已有" : "") + "：");
        foreach (var m in added)
            sb.AppendLine($"  {m.Id,-32} {m.Provider,-10} ctx={FormatCtx(m.ContextWindow),-6} ${m.InputPrice}/{m.OutputPrice}");
        sb.AppendLine("已写入: " + ModelCatalog.GlobalProviderDir + "/（按供应商分类）");
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 从本地服务接口导入已安装模型（Ollama /api/tags、LM Studio /v1/models）——
    /// 实时反映本地模型库（比静态目录更准确）。服务未运行则跳过。
    /// </summary>
    public static string ImportLocalServices(Action<string>? onProgress = null)
    {
        var added = new List<ModelCatalog.ModelInfo>();
        var reports = new List<string>();

        void AddLocal(string endpoint, string pid, string pname, string baseUrl, Func<JNode?, IEnumerable<string>> extract)
        {
            try
            {
                onProgress?.Invoke($"🔍 探测 {pname}（{endpoint}）...");
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var json = client.GetStringAsync(endpoint).GetAwaiter().GetResult();
                var root = Json.Parse(json);
                foreach (var id in extract(root))
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    // 本地服务模型不主动发 thinking（reasoning_effort 会被 Ollama 等 400 拒绝）：
                    // 显式 SupportsThinking=false；SupportsTools 留 null 走推断（gemma2:2b→false，qwen3→true）
                    added.Add(new ModelCatalog.ModelInfo(
                        id, id, pname, pid, "L", "Local", 0, 0, 0, baseUrl,
                        $"从 {pname} 接口导入", 0, SupportsThinking: false));
                }
                reports.Add(pname);
            }
            catch { /* 本地服务未运行 / 请求失败 → 跳过 */ }
        }

        // Ollama：GET /api/tags → { models: [{ name: "qwen2.5:7b", ... }] }
        AddLocal("http://localhost:11434/api/tags", "ollama", "Ollama", "http://localhost:11434",
            j => j?["models"]?.Items.Select(m => m?["name"]?.AsString() ?? "").Where(n => n.Length > 0) ?? []);
        // LM Studio：GET /v1/models → { data: [{ id: "qwen2.5-7b-instruct", ... }] }
        AddLocal("http://localhost:1234/v1/models", "lmstudio", "LM Studio", "http://localhost:1234",
            j => j?["data"]?.Items.Select(m => m?["id"]?.AsString() ?? "").Where(n => n.Length > 0) ?? []);
        // CC Switch（本地 API 路由）：GET /v1/models → { data: [{ id, ... }] }（默认端口 15721）
        AddLocal("http://127.0.0.1:15721/v1/models", "cc-switch", "CC Switch", "http://127.0.0.1:15721",
            j => j?["data"]?.Items.Select(m => m?["id"]?.AsString() ?? "").Where(n => n.Length > 0) ?? []);

        if (added.Count == 0)
            return "未发现本地模型服务（Ollama 11434 / LM Studio 1234 未运行或无已安装模型）。";

        ModelCatalog.AddCustomRange(added);
        RegisterImportProviders(added);
        var sb = new StringBuilder();
        sb.AppendLine($"✅ 从本地服务接口导入 {added.Count} 个模型：");
        foreach (var m in added.OrderBy(m => m.Id))
            sb.AppendLine($"  {m.Id,-32} {m.Provider}");
        sb.AppendLine("已写入 locals.json（本地模型，无需 API Key）");
        return sb.ToString().Trim();
    }

    /// <summary>在线导入来源（OpenAI 兼容 /models 端点 + 所需 key 服务商）。</summary>
    public record OnlineSource(string Name, string BaseUrl, string KeyProvider);

    /// <summary>在线导入可选端点（除 opencode 外，支持 OpenRouter/Groq/SiliconFlow/Together/DeepSeek/OpenAI 等）。</summary>
    public static readonly OnlineSource[] OnlineSources =
    [
        new("OpenCode Go", "https://opencode.ai/zen/go/v1", "opencode-go"),
        new("OpenCode Zen", "https://opencode.ai/zen/v1", "opencode-zen"),
        new("OpenRouter", "https://openrouter.ai/api/v1", "openrouter"),
        new("Groq", "https://api.groq.com/openai/v1", "groq"),
        new("SiliconFlow", "https://api.siliconflow.cn/v1", "siliconflow"),
        new("Together AI", "https://api.together.xyz/v1", "together"),
        new("DeepSeek", "https://api.deepseek.com/v1", "deepseek"),
        new("OpenAI", "https://api.openai.com/v1", "openai"),
        new("Moonshot", "https://api.moonshot.cn/v1", "moonshot"),
    ];

    /// <summary>
    /// 在线导入：拉取指定端点 /models，写入全局模型库。返回报告。
    /// 拉取模型列表本身不需要 key（opencode 等端点公开）；有 key 才带 Authorization 头，
    /// 无 key / key 无效（401/403）时给出友好提示，而不是直接拒绝导入。
    /// </summary>
    public static string ImportOnline(OnlineSource src, Action<string>? onProgress = null)
    {
        var key = ApiKeyStore.Get(src.KeyProvider);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");
        if (!string.IsNullOrEmpty(key))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

        onProgress?.Invoke($"🔍 拉取 {src.Name} 模型列表（{src.BaseUrl}/models）...");
        string json;
        try
        {
            json = client.GetStringAsync(src.BaseUrl + "/models").GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var hint = string.IsNullOrEmpty(key)
                ? $"需要 {src.KeyProvider} 的 API Key（--model key {src.KeyProvider} &lt;key&gt; 设置后重试）"
                : $"「{src.KeyProvider}」的 API Key 无效或已失效";
            return $"在线导入（{src.Name}）失败：{hint}（{ex.Message}）";
        }

        var list = ModelCatalog.ImportOpenCodeApi(json, src.BaseUrl);
        if (list.Count == 0)
            return $"在线导入（{src.Name}）未返回可识别的模型";
        var builtInIds = new HashSet<string>(ModelCatalog.BuiltIn.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
        var toAdd = list.Where(m => !builtInIds.Contains(m.Id)).ToList();
        ModelCatalog.AddCustomRange(toAdd);
        RegisterImportProviders(toAdd);
        return $"✅ 在线导入（{src.Name}）{toAdd.Count} 个模型" +
            (list.Count - toAdd.Count > 0 ? $"，跳过 {list.Count - toAdd.Count} 内置" : "") +
            (string.IsNullOrEmpty(key) ? "（未配置 API Key，导入的模型需设 key 后使用）" : "") +
            "：\n  " + string.Join("\n  ", toAdd.Select(m => m.Id));
    }

    /// <summary>
    /// 在线导入所有端点（或按名称/服务商过滤指定一个）：拉取各 /models 写入全局模型库。
    /// CLI 入口 `--model import online [源名...]`；空 = 全部。
    /// </summary>
    public static string ImportOnlineAll(IReadOnlyList<string>? names = null, Action<string>? onProgress = null)
    {
        var sources = names is { Count: > 0 }
            ? OnlineSources.Where(s =>
                names.Any(n => s.Name.Contains(n.Trim(), StringComparison.OrdinalIgnoreCase)
                            || s.KeyProvider.Contains(n.Trim(), StringComparison.OrdinalIgnoreCase))).ToList()
            : OnlineSources.ToList();
        if (sources.Count == 0)
            return "未找到匹配的在线源（可用: " + string.Join(", ", OnlineSources.Select(s => s.Name)) + "）";
        var reports = sources.Select(s => ImportOnline(s, onProgress)).ToList();
        return string.Join("\n", reports);
    }

    /// <summary>
    /// 确认导入的服务商地址正确可用（探测 /models 端点），可用的写入 providers.json（服务商数据库）。
    /// 连通(2xx) 或端点存在但需认证(401/403) 都视为地址正确。
    /// </summary>
    public static void RegisterImportProviders(IEnumerable<ModelCatalog.ModelInfo> models)
    {
        foreach (var g in models
            .Where(m => !string.IsNullOrWhiteSpace(m.DefaultBaseUrl) && !string.IsNullOrWhiteSpace(m.ProviderId))
            .GroupBy(m => m.ProviderId))
        {
            var first = g.First();
            var baseUrl = (first.DefaultBaseUrl ?? "").Trim().TrimEnd('/');
            if (baseUrl.Length == 0) continue;
            // 服务商按地址去重：该地址已注册（不管 providerId）→ 跳过，避免同地址重复服务商
            bool addrExists = ModelCatalog.Providers.Values.Any(p =>
                string.Equals((p.DefaultBaseUrl ?? "").Trim().TrimEnd('/'), baseUrl, StringComparison.OrdinalIgnoreCase));
            if (addrExists) continue;
            var key = ApiKeyStore.Get(g.Key);
            var (ok, detail) = ProbeEndpoint(first.DefaultBaseUrl, key);
            // 可用：连通(2xx) 或端点存在但需认证(401/403) —— 都说明地址正确
            if (ok || detail.Contains("401") || detail.Contains("403"))
                ModelCatalog.RegisterProvider(g.Key, first.Provider, first.DefaultBaseUrl!);
        }
    }

    /// <summary>服务商管理 CLI（--provider list/add/rm/clean，管理 providers.json 数据库）。</summary>
    public static class ProviderCli
    {
        public static int Run(List<string> values)
        {
            var cmd = values.Count > 0 ? values[0].ToLowerInvariant() : "list";
            switch (cmd)
            {
                case "list":
                case "ls":
                {
                    // 只显示「有无 key」（🔑/—），绝不输出 key 本身
                    var sb = new StringBuilder();
                    foreach (var (id, p) in ModelCatalog.Providers.OrderBy(x => x.Key))
                    {
                        var hasKey = ApiKeyStore.Has(id);
                        // keyMark 统一占 2 列显示宽度（🔑 宽 emoji 2 列，— 窄 1 列需补空格），否则有/无 key 行第二列起错位
                        var keyMark = hasKey ? "🔑" : "— ";
                        sb.AppendLine($"  {keyMark} {id,-20} {p.DisplayName,-22} {p.DefaultBaseUrl}");
                    }
                    Console.WriteLine($"服务商数据库（{ModelCatalog.Providers.Count}）：\n{sb.ToString().TrimEnd()}");
                    return 0;
                }
                case "add":
                case "new":
                {
                    if (values.Count < 4) { Console.WriteLine("用法: --provider add <id> <名称> <base-url>"); return 1; }
                    ModelCatalog.RegisterProvider(values[1], values[2], values[3]);
                    Console.WriteLine($"✅ 已添加服务商 {values[1]} → {values[3]}");
                    return 0;
                }
                case "rm":
                case "remove":
                case "delete":
                case "del":
                {
                    if (values.Count < 2) { Console.WriteLine("用法: --provider rm <id>"); return 1; }
                    ModelCatalog.RemoveProvider(values[1]);
                    Console.WriteLine($"🗑 已移除服务商 {values[1]}");
                    return 0;
                }
                case "select":
                case "switch":
                case "use":
                {
                    if (values.Count < 2) { Console.WriteLine("用法: --provider select <id>"); return 1; }
                    var pid = values[1].Trim().ToLowerInvariant();
                    ConnectionConfig.ApplyModelChoice(pid, Config.Instance.Model, isLarge: true, out var msg);
                    Console.WriteLine($"✅ {msg}");
                    return 0;
                }
                case "key":
                case "apikey":
                {
                    if (values.Count >= 3)
                    {
                        Console.WriteLine(SetKey(values[1], values[2], values.Count > 3 ? values[3] : null));
                        return 0;
                    }
                    Console.WriteLine(ListKeys());
                    return 0;
                }
                case "keyexpiry":
                case "expiry":
                {
                    if (values.Count >= 3) { Console.WriteLine(SetKeyExpiry(values[1], values[2])); return 0; }
                    Console.WriteLine("用法: --provider key expiry <供应商> <有效期>");
                    return 1;
                }
                case "test":
                    Console.WriteLine(Test());
                    return 0;
                case "import":
                    Console.WriteLine(Import(values.Count > 1 ? values[1] : null));
                    return 0;
                case "clean":
                case "prune":
                {
                    Console.WriteLine(CleanText());
                    return 0;
                }
                default:
                    Console.WriteLine($"未知子命令「{cmd}」。用法: --provider list|add <id> <名称> <base-url>|rm <id>|select <id>|key <id> <key>|test|import [source]|clean");
                    return 1;
            }
        }

        /// <summary>统一清理无效服务商（探测 /models 失败）：移除 providers.json 条目 + API Key + 模型文件。返回报告。</summary>
        public static string CleanText()
        {
            var allIds = ModelCatalog.Providers.Keys
                .Union(ApiKeyStore.ListAll().Keys)
                .Where(id => id is not "local" and not "custom")
                .Distinct()
                .ToList();
            int removed = 0;
            foreach (var id in allIds)
            {
                var baseUrl = ModelCatalog.Providers.TryGetValue(id, out var p) ? p.DefaultBaseUrl
                    : ResolveProviderBaseUrl(id);
                // 只删 baseUrl 无效的服务商（空 / 非 http(s) 合法 URL）——
                // 地址有效就保留（探测失败 / 无 key / 临时网络都不是删除理由，避免误删 opencode-go 等）
                bool badUrl = string.IsNullOrWhiteSpace(baseUrl)
                    || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps);
                if (!badUrl) continue;
                // 只删无效的 provider 注册与模型，绝不删 key（key 永不自动删除，要删走 --model key rm）
                if (ModelCatalog.Providers.ContainsKey(id))
                    ModelCatalog.RemoveProvider(id);
                if (ModelCatalog.RemoveCustomByProvider(id) > 0) removed++;
            }
            return removed > 0 ? $"🗑 已清理 {removed} 个无效服务商（保留 API key，模型已删）" : "没有无效服务商";
        }
    }

    private static List<ModelCatalog.ModelInfo> ImportOne(string source, string home, List<string> reports)
    {
        var src = source is "claudecode" ? "claude" : source;

        // Crush：crush.json（自定义 providers）+ providers.json（Catwalk 目录），Windows/Unix 双位置
        if (src == "crush")
            return ImportCrushFiles(home, reports);

        var (path, name) = src switch
        {
            "opencode" => (Path.Combine(home, ".config", "opencode", "opencode.json"), "OpenCode"),
            "openclaw" => (Path.Combine(home, ".openclaw", "openclaw.json"), "OpenClaw"),
            "claude" => (Path.Combine(home, ".claude", "settings.json"), "Claude Code"),
            "codex" => (Path.Combine(home, ".codex", "config.toml"), "Codex"),
            _ => ("", ""),
        };

        if (!File.Exists(path))
        {
            // OpenCode 兼容 .jsonc
            if (src == "opencode")
            {
                var jsonc = Path.Combine(home, ".config", "opencode", "opencode.jsonc");
                if (File.Exists(jsonc)) path = jsonc;
                else return [];
            }
            else return [];
        }

        try
        {
            var text = File.ReadAllText(path);
            var list = src switch
            {
                "opencode" => ModelCatalog.ImportOpenCode(text),
                "openclaw" => ModelCatalog.ImportOpenClaw(text),
                "claude" => ModelCatalog.ImportClaude(text),
                "codex" => ModelCatalog.ImportCodex(text),
                _ => [],
            };
            if (list.Count > 0) reports.Add(name);
            return list;
        }
        catch { return []; }
    }

    /// <summary>导入 Crush 模型数据：crush.json（用户自定义 providers）+ providers.json（Catwalk 内置目录）。
    /// Windows 用 %LOCALAPPDATA%\crush，Unix 用 ~/.config/crush；旧 config.json 兜底兼容。</summary>
    private static List<ModelCatalog.ModelInfo> ImportCrushFiles(string home, List<string> reports)
    {
        var result = new List<ModelCatalog.ModelInfo>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var candidates = new[]
        {
            (Path.Combine(localAppData, "crush", "crush.json"),      "Crush 配置"),
            (Path.Combine(localAppData, "crush", "providers.json"),  "Crush 模型目录"),
            (Path.Combine(home, ".config", "crush", "crush.json"),   "Crush 配置"),
            (Path.Combine(home, ".config", "crush", "providers.json"), "Crush 模型目录"),
            (Path.Combine(home, ".config", "crush", "config.json"),  "Crush 配置"), // 旧文档兼容
        };

        foreach (var (path, name) in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var list = ModelCatalog.ImportCrush(File.ReadAllText(path));
                if (list.Count > 0)
                {
                    result.AddRange(list);
                    reports.Add(name);
                }
            }
            catch { }
        }
        return result;
    }

    /// <summary>删除自定义模型（从全局+本地模型库移除）</summary>
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
        var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
            ? p.DisplayName : pid;
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
    private static (ProbeTarget Target, bool Ok, string Detail)[] RunProbes(List<ProbeTarget> targets)
    {
        var results = new (ProbeTarget, bool, string)[targets.Count];
        var indexed = targets.Select((t, i) => (t, i)).ToArray();
        System.Threading.Tasks.Parallel.ForEach(indexed,
            new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 },
            item =>
            {
                var (t, i) = item;
                var (o, d) = ProbeEndpoint(t.BaseUrl, t.Key);
                results[i] = (t, o, d); // 各索引只写一次，无竞态；顺序由数组下标保证
            });
        return results;
    }

    /// <summary>
    /// 模型连通性测试：逐一测试所有「已存 API key」的端点 + 所有「本地模型」端点能否连上。
    /// 返回 Markdown 文本报告。
    /// </summary>
    public static string Test()
    {
        var sb = new StringBuilder();
        sb.AppendLine("**模型连通性测试**");
        int ok = 0, total = 0;

        var targets = new List<ProbeTarget>();

        // ── 1. 已存 API key：按供应商逐一测试（含目录内无模型的供应商） ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;
            targets.Add(new ProbeTarget(pid, display, baseUrl, key, models, IsLocal: false));
        }

        // ── 2. 本地端点（无需 key）：按 base_url 分组探测 ──
        var localGroups = ModelCatalog.All
            .Where(m =>
            {
                var baseUrl = EffectiveBaseUrl(m);
                return m.ProviderId is "ollama" or "lmstudio" or "local" || ModelCatalog.IsOllamaBaseUrl(baseUrl);
            })
            .GroupBy(m => EffectiveBaseUrl(m) ?? "")
            .ToList();

        foreach (var g in localGroups)
            targets.Add(new ProbeTarget(g.First().ProviderId, g.First().Provider, g.Key, null,
                g.Select(m => m.Id).Distinct().ToArray(), IsLocal: true));

        if (targets.Count == 0)
            return "没有可测试的端点：既无已存 key，也无本地模型。\n" +
                   "  存 key: --model key <供应商> <key>　本地模型: --model connect <localhost:port>";

        if (targets.Any(t => !t.IsLocal))
        {
            sb.AppendLine();
            sb.AppendLine($"### API Key（{targets.Count(t => !t.IsLocal)} 个供应商）");
        }

        // 并发探测（每项独立 HttpClient+4s 超时），保持原顺序输出
        bool localHeaderShown = false;
        foreach (var (t, o, d) in RunProbes(targets))
        {
            if (t.IsLocal && !localHeaderShown)
            {
                sb.AppendLine();
                sb.AppendLine("### 本地端点（无需 key）");
                localHeaderShown = true;
            }
            total++;
            if (o) ok++;
            sb.AppendLine($"【{t.Display}】{(string.IsNullOrEmpty(t.BaseUrl) ? "" : " " + t.BaseUrl)}");
            sb.AppendLine($"  {(o ? "✅" : "❌")} {d}" + (t.Models.Length > 0 ? $"  —  {string.Join(", ", t.Models)}" : ""));
        }

        sb.AppendLine();
        sb.AppendLine($"**结论：{ok} / {total} 个端点可连接**");
        return sb.ToString().Trim();
    }

    /// <summary>连通性探测结果（结构化，供 Web 序列化为 JSON）。</summary>
    public record EndpointProbe(string ProviderId, string Display, string? BaseUrl, bool Ok, string Detail, string[] Models);

    /// <summary>
    /// 结构化连通性测试：返回所有「已存 API key 的供应商」+「本地端点」的探测结果列表。
    /// 与 <see cref="Test"/> 相同的数据源，但不生成 Markdown，供 Web /models/scan 直接序列化。
    /// </summary>
    public static List<EndpointProbe> TestList()
    {
        var targets = new List<ProbeTarget>();

        // ── 1. 已存 API key：按供应商逐一测试 ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;
            targets.Add(new ProbeTarget(pid, display, baseUrl, key, models, IsLocal: false));
        }

        // ── 2. 本地端点（无需 key）：按 base_url 分组探测 ──
        var localGroups = ModelCatalog.All
            .Where(m =>
            {
                var baseUrl = EffectiveBaseUrl(m);
                return m.ProviderId is "ollama" or "lmstudio" or "local" || ModelCatalog.IsOllamaBaseUrl(baseUrl);
            })
            .GroupBy(m => EffectiveBaseUrl(m) ?? "")
            .ToList();

        foreach (var g in localGroups)
            targets.Add(new ProbeTarget(g.First().ProviderId, g.First().Provider, g.Key, null,
                g.Select(m => m.Id).Distinct().ToArray(), IsLocal: true));

        // 并发探测，保持原顺序
        return RunProbes(targets)
            .Select(r => new EndpointProbe(r.Target.ProviderId, r.Target.Display, r.Target.BaseUrl,
                r.Ok, r.Detail, r.Target.Models))
            .ToList();
    }

    /// <summary>
    /// 剪除失效供应商：逐一测试所有已存 API key，对失效供应商自动清理。
    /// 仅 key 无效（401/403）→ 只删 key、保留模型；无端点（供应商不存在/未配置 base_url）或无法连接（写错地址）→ 删 key + 所有自定义模型。
    /// 内置供应商/模型不删（仅删 key）；本地端点不参与。返回 Markdown 文本报告。
    /// </summary>
    public static string Prune()
    {
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        if (keys.Length == 0)
            return "没有已存 API key 可清理。存 key: --model key <供应商> <key>";

        var sb = new StringBuilder();
        sb.AppendLine("**清理失效供应商**");
        sb.AppendLine();
        int removedKeys = 0, removedModels = 0, kept = 0;

        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;

            // 无端点：供应商不存在或未配置 base_url（写错地址/拼错供应商）→ 删模型，key 保留
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var n = ModelCatalog.RemoveCustomByProvider(pid);
                removedModels += n;
                sb.AppendLine($"🗑️  【{display}】无端点（供应商不存在或未配置 base_url）— 已删模型" + (n > 0 ? $" {n} 个" : "") + "（key 保留）");
                continue;
            }

            var (ok, detail) = ProbeEndpoint(baseUrl, key);
            if (ok)
            {
                kept++;
                sb.AppendLine($"✅ 【{display}】{detail} — 保留");
                continue;
            }

            // 无效 key：询问用户是否删除（交互确认）；非交互（管道/一次性）默认保留
            if (detail.StartsWith("密钥无效", StringComparison.Ordinal))
            {
                if (ConfirmDelete($"【{display}】检测到无效 API key（{pid}），是否删除？"))
                {
                    ApiKeyStore.Remove(pid);
                    removedKeys++;
                    sb.AppendLine($"🗑️  【{display}】{detail} — 已删除无效 key");
                }
                else
                {
                    sb.AppendLine($"⚠️  【{display}】{detail} — key 保留（--model key rm {pid} 显式删除）");
                }
                continue;
            }

            var m = ModelCatalog.RemoveCustomByProvider(pid);
            removedModels += m;
            sb.AppendLine($"🗑️  【{display}】{detail} — 已删模型" + (m > 0 ? $" {m} 个" : "") + "（key 保留）");
        }

        sb.AppendLine();
        sb.AppendLine($"**结论：删除 {removedKeys} 个失效供应商的 key，移除 {removedModels} 个自定义模型；保留 {kept} 个**");
        return sb.ToString().Trim();
    }

    /// <summary>解析模型的有效 base_url：provider 唯一地址优先 > 模型默认地址 > 本地(Ollama)默认 localhost:11434</summary>
    private static string? EffectiveBaseUrl(ModelCatalog.ModelInfo m)
    {
        if (ModelCatalog.Providers.TryGetValue(m.ProviderId, out var p) && !string.IsNullOrEmpty(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        if (!string.IsNullOrWhiteSpace(m.DefaultBaseUrl)) return m.DefaultBaseUrl;
        if (m.ProviderId is "ollama" or "local") return "http://localhost:11434";
        return null;
    }

    /// <summary>解析服务商的 base_url：注册表 > 目录内该服务商模型 > null（无法解析）</summary>
    private static string? ResolveProviderBaseUrl(string providerId)
    {
        var pid = providerId.Trim().ToLowerInvariant();
        if (ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
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
            var b = baseUrl.Trim().TrimEnd('/');
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

    private static string FormatCtx(int ctx) =>
        ctx <= 0 ? "?" : ctx >= 1_000_000 ? $"{ctx / 1_000_000}M" : $"{ctx / 1000}K";
}
