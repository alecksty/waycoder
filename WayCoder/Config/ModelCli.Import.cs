using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCli
{
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
        // models.dev：开源模型数据库（api.json 专用格式，非 OpenAI /models 端点，ImportOnline 特判）
        new("models.dev", "https://models.dev/api.json", "models.dev"),
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

        // models.dev 是 api.json 专用格式（非 OpenAI /models 端点），URL 即文件本身
        var isModelsDev = src.KeyProvider == "models.dev";
        onProgress?.Invoke($"🔍 拉取 {src.Name} 模型列表（{(isModelsDev ? src.BaseUrl : src.BaseUrl + "/models")}）...");
        string json;
        try
        {
            json = client.GetStringAsync(isModelsDev ? src.BaseUrl : src.BaseUrl + "/models").GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var hint = string.IsNullOrEmpty(key)
                ? $"需要 {src.KeyProvider} 的 API Key（--model key {src.KeyProvider} &lt;key&gt; 设置后重试）"
                : $"「{src.KeyProvider}」的 API Key 无效或已失效";
            return $"在线导入（{src.Name}）失败：{hint}（{ex.Message}）";
        }

        onProgress?.Invoke($"🔄 解析 {src.Name} 模型列表（{json.Length / 1024} KB）…");
        var list = isModelsDev
            ? ModelCatalog.ImportModelsDev(json)
            : ModelCatalog.ImportOpenCodeApi(json, src.BaseUrl);
        if (list.Count == 0)
            return $"在线导入（{src.Name}）未返回可识别的模型";
        onProgress?.Invoke($"🔄 解析完成 {list.Count} 个模型，写入模型库…");
        // 不再按内置 id 跳过：models.dev 等数据刷新内置（同 id+同 baseUrl 由 AddCustomRange/All 覆盖，
        // 网关托管同名模型 baseUrl 不同 = 不同服务商，正常追加）；仅跳过无端点模型（baseUrl 空 = 无法使用，
        // RegisterImportProviders 也不会注册其供应商）
        var toAdd = list.Where(m => !string.IsNullOrWhiteSpace(m.DefaultBaseUrl)).ToList();
        ModelCatalog.AddCustomRange(toAdd);
        RegisterImportProviders(toAdd);
        var providerCount = toAdd.Select(m => m.ProviderId).Distinct().Count();
        onProgress?.Invoke($"✅ 完成：{providerCount} 个供应商 / {toAdd.Count} 个模型");
        return $"✅ 在线导入（{src.Name}）{providerCount} 个供应商 / {toAdd.Count} 个模型" +
            (list.Count - toAdd.Count > 0 ? $"，跳过 {list.Count - toAdd.Count} 个无端点模型" : "") +
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
        // 导入自愈：AddCustomRange 落盘早于注册，同批同地址多来源名可能产生未注册别名（addrExists 跳过注册）
        // —— 归并把别名模型挂到刚注册的同地址供应商。幂等：无别名时零写零备份，不影响正常导入。
        ModelCatalog.ReconcileModels(false);
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
                    var added = ModelCatalog.RegisterProvider(values[1], values[2], values[3]);
                    if (added) { Console.WriteLine($"✅ 已添加服务商 {values[1]} → {values[3]}"); return 0; }
                    var owner = ModelCatalog.FindProviderByBaseUrl(values[3]);
                    Console.WriteLine($"❌ 添加失败：该地址已被服务商「{owner}」占用（同地址 = 同供应商，不允许重复）");
                    return 1;
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
                case "reconcile":
                {
                    // 默认预览（安全）：CLI 贪婪参数会吞掉 --dry-run（-- 开头被当独立旗标），
                    // 故 CLI 显式 apply/run/force 才执行真归并，其余一律 dry-run。
                    var a = values.Skip(1).Select(v => v.ToLowerInvariant()).ToList();
                    var apply = a.Any(v => v is "apply" or "run" or "force");
                    Console.WriteLine(ReconcileText(!apply));
                    return 0;
                }
                default:
                    Console.WriteLine($"未知子命令「{cmd}」。用法: --provider list|add <id> <名称> <base-url>|rm <id>|select <id>|key <id> <key>|test|import [source]|clean|reconcile [apply]");
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

        /// <summary>模型归并报告文本：别名 providerId → 同 base_url 的注册供应商。dryRun 只预览不落盘。</summary>
        public static string ReconcileText(bool dryRun)
        {
            var rep = ModelCatalog.ReconcileModels(dryRun);
            var sb = new StringBuilder();
            sb.AppendLine(dryRun
                ? $"🔍 模型归并预览（未写盘，实际归并请去掉 --dry-run）：{rep.Moved} 移动 / {rep.DuplicateSkip} 重复跳过 / {rep.AlreadyCanonical} 已正确 / {rep.NoUrl} 无URL / {rep.Unresolved} 无归属"
                : $"✅ 模型归并完成（已备份）：{rep.Moved} 移动 / {rep.DuplicateSkip} 重复跳过 / {rep.AlreadyCanonical} 已正确 / {rep.NoUrl} 无URL / {rep.Unresolved} 无归属");
            if (rep.Backups.Count > 0)
                sb.AppendLine($"  💾 备份 {rep.Backups.Count} 个文件（{Path.GetFileName(rep.Backups[0])} …）");
            if (rep.DeletedFiles.Count > 0)
                sb.AppendLine($"  🗑 删除 {rep.DeletedFiles.Count} 个空别名文件");
            if (rep.FailedFiles.Count > 0)
                sb.AppendLine($"  ❌ 写入失败 {rep.FailedFiles.Count} 个文件：{string.Join("、", rep.FailedFiles.Select(Path.GetFileName))}");
            if (dryRun && rep.Changes != null)
            {
                var shown = rep.Changes.Where(c => c.Action is "moved" or "duplicate-skip").Take(20).ToList();
                foreach (var c in shown)
                    sb.AppendLine($"  · {c.SourceProviderId}/{c.ModelId} → {c.TargetProviderId}（{c.Action}）");
                if (shown.Count < rep.Changes.Count(c => c.Action is "moved" or "duplicate-skip"))
                    sb.AppendLine($"  · … 其余省略");
            }
            if (rep.Unresolved > 0)
                sb.AppendLine($"  ℹ️ {rep.Unresolved} 个模型地址无注册归属（未动）：可 --provider add 注册该地址，或改模型 baseUrl 后重跑");
            return sb.ToString().TrimEnd();
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
}
