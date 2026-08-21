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
        sb.AppendLine($"当前大模型：{cfg.Model}（服务商 {cfg.Provider}）");
        sb.AppendLine($"当前小模型：{cfg.SmallModel}（服务商 {cfg.SmallProvider}）");
        if (!string.IsNullOrWhiteSpace(cfg.BaseUrl))
            sb.AppendLine($"BaseUrl：{cfg.BaseUrl}");
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

        foreach (var g in models.GroupBy(m => m.Provider))
        {
            sb.AppendLine();
            sb.AppendLine($"【{g.Key}】");
            foreach (var m in g)
            {
                var price = m.InputPrice > 0 ? $"${m.InputPrice}/${m.OutputPrice}" : "?";
                var ctx = m.ContextWindow > 0
                    ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                    : "?";
                var mark = m.Id == current ? "  ← 当前" : "";
                sb.AppendLine($"  {m.Id,-28} {ctx,-5}ctx  {price,-13}/MTok  [{m.Category}]{mark}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("选中: --model name <id> 或 --model <id>");
        sb.AppendLine("存 key: --model key <供应商> <key>　查 key: --model key");
        return sb.ToString();
    }

    /// <summary>选中模型：按目录解析，自动设置 base-url，写入 .env 持久化</summary>
    public static string Select(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            Config.Instance.Model = modelId.Trim();
            Config.Instance.SaveToEnvFile();
            return $"已设置模型为 `{modelId}`（目录外模型，已写入 .env）。若非 OpenAI 兼容端点请另行 --config set BaseUrl <url>";
        }

        Config.Instance.Model = info.Id;
        Config.Instance.Provider = info.ProviderId;   // 同步当前服务商（key 跟服务商走）
        // 两层架构：provider 唯一地址优先，模型默认地址兜底
        var selBaseUrl = (ModelCatalog.Providers.TryGetValue(info.ProviderId, out var sp) && !string.IsNullOrEmpty(sp.DefaultBaseUrl) ? sp.DefaultBaseUrl : null)
            ?? info.DefaultBaseUrl;
        if (selBaseUrl != null)
            Config.Instance.BaseUrl = selBaseUrl;
        Config.Instance.SaveToEnvFile();

        var keyHint = info.DefaultBaseUrl != null
            && !ApiKeyStore.Has(info.ProviderId)
            && info.ProviderId is not ("openai" or "local" or "custom")
            ? $"\n  该供应商需 API key：--model key {info.ProviderId} <key>"
            : "";

        return $"已选中 **{info.DisplayName}**（`{info.Id}`，服务商 `{info.ProviderId}`）并写入 .env" +
            (info.DefaultBaseUrl != null ? $"\n  BaseUrl 已自动设为 {info.DefaultBaseUrl}" : "") + keyHint;
    }

    /// <summary>选中小模型：按目录解析，写入 .env 持久化（同步小模型服务商）</summary>
    public static string SelectSmall(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            Config.Instance.SmallModel = modelId.Trim();
            Config.Instance.SaveToEnvFile();
            return $"已设置小模型为 `{modelId}`（目录外模型，已写入 .env）";
        }

        Config.Instance.SmallModel = info.Id;
        Config.Instance.SmallProvider = info.ProviderId;   // 同步小模型服务商
        Config.Instance.SaveToEnvFile();

        return $"已选中小模型 **{info.DisplayName}**（`{info.Id}`，服务商 `{info.ProviderId}`）并写入 .env";
    }

    /// <summary>列出已保存的 API keys（打码）</summary>
    public static string ListKeys()
    {
        var keys = ApiKeyStore.ListAll();
        if (keys.Count == 0)
            return "未保存任何 API key。用 --model key <供应商> <key> 保存。";

        var sb = new StringBuilder();
        sb.AppendLine("已保存 API keys：");
        foreach (var (pid, _) in keys)
            sb.AppendLine($"  {pid,-12} = {ApiKeyStore.Masked(pid)}");
        return sb.ToString();
    }

    /// <summary>保存指定供应商的 API key</summary>
    public static string SetKey(string providerId, string key)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(key))
            return "用法: --model key <供应商> <key>";
        ApiKeyStore.Set(providerId, key);
        return $"已保存 {providerId} 的 API key：{ApiKeyStore.Masked(providerId)}";
    }

    /// <summary>
    /// 导入外部模型数据库（OpenCode / OpenClaw / Crush / Claude Code / Codex / 通用 JSON 文件 / 内置目录），写入全局模型库。
    /// source: null/auto/all=自动探测全部；逗号分隔多来源（opencode,codex,claude）；单来源；builtin=恢复被清空的内置目录；否则视为文件路径。
    /// </summary>
    public static string Import(string? source = null)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var imported = new List<ModelCatalog.ModelInfo>();
        var reports = new List<string>();
        bool restoredBuiltIn = false;

        // 解析来源列表：null/auto/all → 全部；否则按逗号拆分（支持「本地导入」勾选多来源）
        var s = source?.Trim();
        var sources = string.IsNullOrWhiteSpace(s) || s.ToLowerInvariant() is "auto" or "all"
            ? new[] { "opencode", "openclaw", "crush", "claude", "codex" }.ToList()
            : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        foreach (var raw in sources)
        {
            var src = raw.ToLowerInvariant();
            if (src == "builtin")
            {
                ModelCatalog.RestoreBuiltIn();
                restoredBuiltIn = true;
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

        var sb = new StringBuilder();
        if (restoredBuiltIn)
            sb.AppendLine("✅ 已恢复内置模型目录（清空标记清除）");

        if (imported.Count == 0)
            return sb.Length > 0
                ? sb.ToString().Trim()
                : "❌ 未导入任何模型（未找到可识别的模型配置）。\n   支持: --model import [builtin|opencode|openclaw|crush|claude|codex|<配置文件路径>]";

        // 去重：同一 (Id, baseUrl) 只保留第一个（地址不同=不同服务商，同 id 不同地址都保留）
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        imported = imported.Where(m => seenIds.Add(ModelCatalog.ModelKey(m.Id, m.DefaultBaseUrl))).ToList();

        // 跳过内置：仅当同 id 且同 baseUrl（地址不同视为不同服务商，不跳过）
        var builtInIds = new HashSet<string>(
            ModelCatalog.BuiltIn.Select(m => ModelCatalog.ModelKey(m.Id, m.DefaultBaseUrl)),
            StringComparer.OrdinalIgnoreCase);
        var added = new List<ModelCatalog.ModelInfo>();
        var skipped = new List<string>();
        foreach (var m in imported)
        {
            if (builtInIds.Contains(ModelCatalog.ModelKey(m.Id, m.DefaultBaseUrl)))
                skipped.Add(m.Id);
            else
                added.Add(m);
        }
        ModelCatalog.AddCustomRange(added); // 批量一次写（防 N 次磁盘写）

        sb.AppendLine($"✅ 导入 {added.Count} 个模型到全局模型库（{string.Join("、", reports)}）" +
            (skipped.Count > 0 ? $"，跳过 {skipped.Count} 个内置已有" : "") + "：");
        foreach (var m in added)
            sb.AppendLine($"  {m.Id,-32} {m.Provider,-10} ctx={FormatCtx(m.ContextWindow),-6} ${m.InputPrice}/{m.OutputPrice}");
        sb.AppendLine("已写入: " + ModelCatalog.GlobalProviderDir + "/（按供应商分类）");
        return sb.ToString().Trim();
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
            return "用法: --model add model <id> <供应商ID> [baseUrl]";
        var pid = string.IsNullOrWhiteSpace(providerId) ? "custom" : providerId.Trim().ToLowerInvariant();
        var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
            ? p.DisplayName
            : (string.IsNullOrWhiteSpace(providerId) ? "custom" : providerId.Trim());
        var info = new ModelCatalog.ModelInfo(id.Trim(), id.Trim(), display, pid, "*", "Custom",
            0, 0, 0, string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim(), $"手动添加（{pid}）", 0);
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

    /// <summary>
    /// 模型连通性测试：逐一测试所有「已存 API key」的端点 + 所有「本地模型」端点能否连上。
    /// 返回 Markdown 文本报告。
    /// </summary>
    public static string Test()
    {
        var sb = new StringBuilder();
        sb.AppendLine("**模型连通性测试**");
        int ok = 0, total = 0;

        // ── 1. 已存 API key：按供应商逐一测试（含目录内无模型的供应商） ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        if (keys.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"### API Key（{keys.Length} 个供应商）");
            foreach (var (pid, key) in keys)
            {
                var baseUrl = ResolveProviderBaseUrl(pid);
                var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
                var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                    ? p.DisplayName : pid;
                var (o, d) = ProbeEndpoint(baseUrl, key);
                total++;
                if (o) ok++;
                sb.AppendLine($"【{display}】{(string.IsNullOrEmpty(baseUrl) ? "" : " " + baseUrl)}");
                sb.AppendLine($"  {(o ? "✅" : "❌")} {d}" + (models.Length > 0 ? $"  —  {string.Join(", ", models)}" : ""));
            }
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

        if (localGroups.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### 本地端点（无需 key）");
            foreach (var g in localGroups)
            {
                var (o, d) = ProbeEndpoint(g.Key, null);
                total++;
                if (o) ok++;
                sb.AppendLine($"【{g.First().Provider}】{g.Key}");
                sb.AppendLine($"  {(o ? "✅" : "❌")} {d}  —  {string.Join(", ", g.Select(m => m.Id).Distinct())}");
            }
        }

        if (total == 0)
            return "没有可测试的端点：既无已存 key，也无本地模型。\n" +
                   "  存 key: --model key <供应商> <key>　本地模型: --model connect <localhost:port>";

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
        var result = new List<EndpointProbe>();

        // ── 1. 已存 API key：按供应商逐一测试 ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;
            var (o, d) = ProbeEndpoint(baseUrl, key);
            result.Add(new EndpointProbe(pid, display, baseUrl, o, d, models));
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
        {
            var (o, d) = ProbeEndpoint(g.Key, null);
            result.Add(new EndpointProbe(g.First().ProviderId, g.First().Provider, g.Key, o, d,
                g.Select(m => m.Id).Distinct().ToArray()));
        }

        return result;
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

            // 无端点：供应商不存在或未配置 base_url（写错地址/拼错供应商）→ 删除
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var n = ModelCatalog.RemoveCustomByProvider(pid);
                ApiKeyStore.Remove(pid);
                removedKeys++;
                removedModels += n;
                sb.AppendLine($"🗑️  【{display}】无端点（供应商不存在或未配置 base_url）— 已删除 key" + (n > 0 ? $" + {n} 个自定义模型" : ""));
                continue;
            }

            var (ok, detail) = ProbeEndpoint(baseUrl, key);
            if (ok)
            {
                kept++;
                sb.AppendLine($"✅ 【{display}】{detail} — 保留");
                continue;
            }

            // 仅 key 无效（401/403）：供应商真实可达，模型保留，只删 key
            if (detail.StartsWith("密钥无效", StringComparison.Ordinal))
            {
                ApiKeyStore.Remove(pid);
                removedKeys++;
                sb.AppendLine($"🗑️  【{display}】{detail} — 已删除 key（模型保留）");
                continue;
            }

            // 其余失效（无法连接/写错地址/无 /models 接口）：供应商本身不可用 → 删 key + 模型
            var m = ModelCatalog.RemoveCustomByProvider(pid);
            ApiKeyStore.Remove(pid);
            removedKeys++;
            removedModels += m;
            sb.AppendLine($"🗑️  【{display}】{detail} — 已删除 key" + (m > 0 ? $" + {m} 个自定义模型" : ""));
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
    private static (bool Ok, string Detail) ProbeEndpoint(string? baseUrl, string? apiKey)
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
                catch { /* 该 URL 失败，试下一个 */ }
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
