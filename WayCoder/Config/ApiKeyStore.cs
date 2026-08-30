namespace WayCoder;

/// <summary>
/// API Key 管理器 —— 按「服务商」存储 API Key（key 跟服务商走，不跟模型走）。
/// 保存到全局 ~/.waycoder/api_keys.json，支持多个服务商的 Key。
/// 文件格式（对标 OpenCode/Crush 的多 key 全局存储）：
///   [ { "provider": "deepseek", "apikey": "sk-..." }, ... ]
/// </summary>
public static class ApiKeyStore
{
    /// <summary>单个 key 条目：API key + 可选有效期（null/永久 = 不限期）+ 该 key 调用的 baseURL
    /// （key 与其请求地址绑定，防同名供应商/网关间 key 混用）。</summary>
    public sealed record KeyEntry(string ApiKey, string? Expiry, string? BaseUrl = null);

    private static readonly object _lock = new();
    private static Dictionary<string, KeyEntry>? _keys;

    private static string FilePath =>
        // 统一走 Global.Home（桌面 = ~，移动端 MauiBootstrap 重定向为 AppDataDirectory）。
        // 若用裸 UserProfile，Android 上会解析成根路径 "/"，写 ~/.waycoder 直接 "access to the path '/' is denied"。
        Global.GlobalConfigPath("api_keys.json");

    /// <summary>获取指定服务商的 API Key，未存储返回 null</summary>
    public static string? Get(string providerId)
    {
        var keys = Load();
        return keys.TryGetValue(providerId.ToLowerInvariant(), out var entry) ? entry.ApiKey : null;
    }

    /// <summary>获取指定服务商 key 关联的 baseURL（该 key 应发往的地址；未存返回 null）。
    /// key 与地址绑定：同名供应商多网关时保证 key 发到正确地址。</summary>
    public static string? GetBaseUrl(string providerId)
    {
        var keys = Load();
        return keys.TryGetValue(providerId.ToLowerInvariant(), out var entry) ? entry.BaseUrl : null;
    }

    /// <summary>获取指定服务商的 API key 有效期（null = 永久）。</summary>
    public static string? GetExpiry(string providerId)
    {
        var keys = Load();
        return keys.TryGetValue(providerId.ToLowerInvariant(), out var entry) ? entry.Expiry : null;
    }

    /// <summary>
    /// 校验 API Key 合法字符：只允许英文字母数字 + `+-_.` 逗号，且非空。
    /// 环境变量引用（$VAR / ${VAR}）含 `$` 不在白名单 → 判非法（防把环境变量当 key 误存）。
    /// 用于手动输入 Key 的即时校验；导入路径宽松（真实 key 可能含 `=` 等，仅防环境变量引用）。
    /// </summary>
    public static bool IsValidApiKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        foreach (var c in key.Trim())
        {
            var ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                || c is '+' or '-' or '_' or '.' or ',';
            if (!ok) return false;
        }
        return true;
    }

    /// <summary>存储指定服务商的 API Key。返回是否保存成功（失败已记日志，调用方可提示用户）。
    /// 环境变量引用（$VAR）视为非法输入拒绝存储——防导入流程把环境变量当真实 key 误存。
    /// baseUrl 记录该 key 调用的地址（key 与地址绑定，供同名供应商多网关区分）。</summary>
    public static bool Set(string providerId, string apiKey, string? expiry = null, string? baseUrl = null)
    {
        lock (_lock)
        {
            // 写时复制：读路径（Get/Has/ListAll*）无锁返回共享 `_keys`，故写侧必须克隆后再改，
            // 否则并发读者会在原地变异的字典上枚举/取值，抛 InvalidOperationException 或读到撕裂状态。
            var keys = new Dictionary<string, KeyEntry>(Load());
            var pid = providerId.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(apiKey))
                keys.Remove(pid);
            else if (IsEnvVarRef(apiKey))
                return false; // 环境变量引用不是真实 key（$VAR），拒绝存储，防导入误判
            else
                keys[pid] = new KeyEntry(apiKey.Trim(), NormalizeExpiry(expiry),
                    string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim());
            return Save(keys);
        }
    }

    /// <summary>仅更新指定服务商 key 的有效期（不改动 key 本身）。返回是否成功（未存 key 返回 false）。</summary>
    public static bool SetExpiry(string providerId, string? expiry)
    {
        lock (_lock)
        {
            var keys = new Dictionary<string, KeyEntry>(Load());
            var pid = providerId.ToLowerInvariant();
            if (!keys.TryGetValue(pid, out var entry)) return false;
            keys[pid] = entry with { Expiry = NormalizeExpiry(expiry) };
            return Save(keys);
        }
    }

    /// <summary>删除指定服务商的 Key</summary>
    public static void Remove(string providerId)
    {
        lock (_lock)
        {
            var keys = new Dictionary<string, KeyEntry>(Load());
            keys.Remove(providerId.ToLowerInvariant());
            Save(keys);
        }
    }

    /// <summary>列出所有已存服务商（服务商ID → key）</summary>
    public static Dictionary<string, string> ListAll() =>
        Load().ToDictionary(kv => kv.Key, kv => kv.Value.ApiKey);

    /// <summary>列出所有已存服务商（服务商ID → key + 有效期）。</summary>
    public static Dictionary<string, KeyEntry> ListAllEntries() => new(Load());

    /// <summary>检查是否有指定服务商的 Key</summary>
    public static bool Has(string providerId) =>
        Load().ContainsKey(providerId.ToLowerInvariant());

    // ════════════════════════════════════════════════════════════
    //  有效期（expiry）：null/永久 = 不限期；日期 = 截止日
    // ════════════════════════════════════════════════════════════

    /// <summary>规范化有效期输入：空串/null/永久等 → null（不限期）；日期或自定义标签原样保留。</summary>
    public static string? NormalizeExpiry(string? expiry)
    {
        if (string.IsNullOrWhiteSpace(expiry)) return null;
        var s = expiry.Trim();
        if (s is "永久" or "permanent" or "forever" or "∞" or "0" or "-" or "无") return null;
        return s;
    }

    /// <summary>
    /// 有效期展示文本：永久 / 日期（剩 N 天）；已过期或剩 ≤7 天加 ⚠ 前缀；非日期标签原样显示。
    /// </summary>
    public static string ExpiryText(string? expiry)
    {
        var norm = NormalizeExpiry(expiry);
        if (norm == null) return "永久";
        if (DateTime.TryParse(norm, out var dt))
        {
            var days = (dt.Date - DateTime.Today).Days;
            if (days < 0) return $"⚠ {norm} 已过期";
            if (days <= 7) return $"⚠ {norm}（剩 {days} 天）";
            return $"{norm}（剩 {days} 天）";
        }
        return norm;
    }

    /// <summary>是否已过期（永久 / 非日期返回 false）。</summary>
    public static bool IsExpired(string? expiry)
    {
        var norm = NormalizeExpiry(expiry);
        if (norm == null) return false;
        return DateTime.TryParse(norm, out var dt) && dt.Date < DateTime.Today;
    }

    /// <summary>距到期剩余天数（永久 / 非日期返回 int.MaxValue）。</summary>
    public static int DaysLeft(string? expiry)
    {
        var norm = NormalizeExpiry(expiry);
        if (norm == null) return int.MaxValue;
        return DateTime.TryParse(norm, out var dt) ? (dt.Date - DateTime.Today).Days : int.MaxValue;
    }

    /// <summary>
    /// 按模型 ID 解析其服务商，返回该服务商已存 key；模型不在目录或未存 key 返回 null。
    /// 用于 env 无 key 时从全局 JSON 多 key 存储回退（对标 OpenCode/Crush）。
    /// </summary>
    public static string? ForModel(string modelId)
    {
        var info = ModelCatalog.Find(modelId);
        return info != null ? Get(info.ProviderId) : null;
    }

    /// <summary>获取 Key 的脱敏显示（如 sk-...abc123）</summary>
    public static string? Masked(string providerId)
    {
        var key = Get(providerId);
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (key.Length <= 10) return new string('*', key.Length);
        return key[..4] + new string('*', key.Length - 8) + key[^4..];
    }

    // ════════════════════════════════════════════════════════════
    //  Key 检测（含环境变量） + 自动导入其他软件的 Key
    // ════════════════════════════════════════════════════════════

    /// <summary>供应商 → 专属环境变量名（对齐 TUI ModelPicker.ProviderEnvVar）。</summary>
    public static readonly Dictionary<string, string> ProviderEnvVar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = "OPENAI_API_KEY",
        ["anthropic"] = "ANTHROPIC_API_KEY",
        ["deepseek"] = "DEEPSEEK_API_KEY",
        ["google"] = "GOOGLE_API_KEY",
        ["qwen"] = "DASHSCOPE_API_KEY",
        ["moonshot"] = "MOONSHOT_API_KEY",
        ["zhipu"] = "ZHIPU_API_KEY",
        ["bytedance"] = "ARK_API_KEY",
        ["01ai"] = "YI_API_KEY",
        ["xai"] = "XAI_API_KEY",
        ["mistral"] = "MISTRAL_API_KEY",
        ["siliconflow"] = "SILICONFLOW_API_KEY",
        ["meta"] = "META_API_KEY",
    };

    /// <summary>
    /// 检查指定模型是否有可用 Key（完整检测，对齐 TUI ModelPicker.ModelHasKey）：
    /// 1. ApiKeyStore 显式存储（按供应商）
    /// 2. 供应商专属环境变量（如 DEEPSEEK_API_KEY）
    /// 3. 通用模式 {PROVIDER}_API_KEY
    /// 4. 全局 WAYCODER_API_KEY → 仅当前配置的大小模型
    /// 5. local/custom 无需 key
    /// </summary>
    public static bool HasKeyFor(string providerId, string modelId)
    {
        if (!string.IsNullOrEmpty(Get(providerId)))
            return true;
        if (ProviderEnvVar.TryGetValue(providerId, out var envVar))
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                return true;
        }
        var genericEnv = $"{providerId}_API_KEY".ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(genericEnv)))
            return true;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYCODER_API_KEY")))
        {
            var cfg = Config.Instance;
            if (modelId == cfg.Model || modelId == cfg.SmallModel)
                return true;
        }
        if (providerId is "local" or "custom") return true;
        return false;
    }

    /// <summary>
    /// 从环境变量导入 Key（按供应商专属变量名 + 通用 {PROVIDER}_API_KEY）。
    /// 默认优先 api_keys.json：仅当该服务商 json 中无 key 时才把环境变量 key 导入（不覆盖已有 json key）。
    /// 返回已导入的供应商 ID 列表。纯逻辑便于自测（不读文件）。
    /// </summary>
    public static List<string> ImportFromEnvironment()
    {
        var imported = new List<string>();
        foreach (var (pid, envVar) in ProviderEnvVar)
        {
            var val = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(val) && !Has(pid))
            {
                Set(pid, val.Trim());
                imported.Add(pid);
            }
        }
        // 通用模式（供应商不在映射表内，但存在 {PROVIDER}_API_KEY 环境变量）
        foreach (var pid in ModelCatalog.ProviderIds)
        {
            if (pid is "local" or "custom" || imported.Contains(pid)) continue;
            var genericEnv = $"{pid}_API_KEY".ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
            var val = Environment.GetEnvironmentVariable(genericEnv);
            if (!string.IsNullOrWhiteSpace(val) && !Has(pid))
            {
                Set(pid, val.Trim());
                imported.Add(pid);
            }
        }
        return imported;
    }

    /// <summary>读取某服务商的环境变量 key（专属变量名 + 通用 {PROVIDER}_API_KEY），无则返回 null。
    /// 仅当 api_keys.json 无该服务商 key 时作回退使用（解析优先级：json > 环境变量）。</summary>
    public static string? EnvKey(string providerId)
    {
        if (ProviderEnvVar.TryGetValue(providerId, out var envVar))
        {
            var v = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        var genericEnv = $"{providerId}_API_KEY".ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
        var gv = Environment.GetEnvironmentVariable(genericEnv);
        return string.IsNullOrWhiteSpace(gv) ? null : gv.Trim();
    }

    /// <summary>根据环境变量名反查供应商 ID（ANTHROPIC_API_KEY → anthropic），查不到返回 null。</summary>
    public static string? ProviderFromEnvVarName(string envVarName)
    {
        if (string.IsNullOrWhiteSpace(envVarName)) return null;
        foreach (var (pid, envVar) in ProviderEnvVar)
            if (envVar.Equals(envVarName, StringComparison.OrdinalIgnoreCase)) return pid;
        // 通用模式 {PROVIDER}_API_KEY → 剥离后缀
        if (envVarName.EndsWith("_API_KEY", StringComparison.OrdinalIgnoreCase) ||
            envVarName.EndsWith("_AUTH_TOKEN", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = envVarName;
            var idx = baseName.LastIndexOf("_API_KEY", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) idx = baseName.LastIndexOf("_AUTH_TOKEN", StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                var pid = baseName[..idx].ToLowerInvariant().Replace('_', '-');
                if (pid.Length > 0) return pid;
            }
        }
        return null;
    }

    /// <summary>
    /// 从其他软件已知配置文件导入 API Key（Claude Code / Codex / OpenCode / Cursor）+ 环境变量。
    /// 全部容错：某文件缺失/解析失败只跳过该项。返回已导入的 (供应商ID, 来源) 列表。
    /// </summary>
    /// <summary>环境变量引用形式的伪 key（Unix `$VAR` / `${VAR}`、Windows `%VAR%`）——非真实字面 key，
    /// 从配置文件导入来源时应跳过；`$` 与 `%` 均不在合法 Key 字符集内，一律视为非法值。</summary>
    internal static bool IsEnvVarRef(string raw)
    {
        var s = raw.TrimStart();
        return s.StartsWith('$') || s.StartsWith("${") || s.StartsWith('%');
    }

    public static List<(string ProviderId, string Source)> ImportFromKnownSources()
    {
        var imported = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string providerId, string source)
        {
            if (providerId is "local" or "custom") return;
            if (!seen.Add(providerId)) return;
            imported.Add((providerId, source));
        }

        // 1. 环境变量（最可靠、跨平台）
        foreach (var pid in ImportFromEnvironment())
            Add(pid, "环境变量");

        var home = Global.Home;

        // 2. Claude Code ~/.claude/settings.json 的 env.{*_API_KEY|*_AUTH_TOKEN}
        try
        {
            var claudeSettings = Path.Combine(home, ".claude", "settings.json");
            if (File.Exists(claudeSettings))
            {
                var env = Json.Parse(File.ReadAllText(claudeSettings))?["env"];
                if (env != null)
                {
                    foreach (var (key, val) in env.Entries)
                    {
                        if (!key.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) &&
                            !key.Contains("AUTH_TOKEN", StringComparison.OrdinalIgnoreCase)) continue;
                        var raw = val?.AsString();
                        if (string.IsNullOrWhiteSpace(raw) || IsEnvVarRef(raw)) continue;
                        var pid = ProviderFromEnvVarName(key);
                        if (pid == null) continue;
                        if (!Has(pid)) Set(pid, raw.Trim());
                        Add(pid, "Claude Code");
                    }
                }
            }
        }
        catch { }

        // 3. Codex ~/.codex/auth.json（顶层 OPENAI_API_KEY 等）
        try
        {
            var codexAuth = Path.Combine(home, ".codex", "auth.json");
            if (File.Exists(codexAuth))
            {
                var root = Json.Parse(File.ReadAllText(codexAuth));
                if (root is { Kind: JKind.Object })
                {
                    foreach (var (key, val) in root.Entries)
                    {
                        if (key.Contains("tokens", StringComparison.OrdinalIgnoreCase)) continue;
                        var raw = val?.AsString();
                        if (string.IsNullOrWhiteSpace(raw) || IsEnvVarRef(raw)) continue;
                        var pid = ProviderFromEnvVarName(key);
                        if (pid == null) continue;
                        if (!Has(pid)) Set(pid, raw.Trim());
                        Add(pid, "Codex");
                    }
                }
            }
        }
        catch { }

        // 4. Cursor ~/.cursor/settings.json（openaiApiKey / anthropicApiKey）
        try
        {
            var cursorSettings = Path.Combine(home, ".cursor", "settings.json");
            if (File.Exists(cursorSettings))
            {
                var root = Json.Parse(File.ReadAllText(cursorSettings));
                foreach (var (pid, field) in new (string, string)[]
                {
                    ("openai", "openaiApiKey"),
                    ("anthropic", "anthropicApiKey"),
                })
                {
                    var raw = root?[field]?.AsString();
                    if (string.IsNullOrWhiteSpace(raw) || IsEnvVarRef(raw)) continue;
                    if (!Has(pid)) Set(pid, raw.Trim());
                    Add(pid, "Cursor");
                }
            }
        }
        catch { }

        // 5. OpenCode ~/.local/share/opencode/auth.json（{provider: {key}} 或 {provider: "key"}）
        try
        {
            foreach (var path in new[]
            {
                Path.Combine(home, ".local", "share", "opencode", "auth.json"),
                Path.Combine(home, ".config", "opencode", "auth.json"),
            })
            {
                if (!File.Exists(path)) continue;
                var root = Json.Parse(File.ReadAllText(path));
                if (root is not { Kind: JKind.Object }) continue;
                foreach (var (pid, val) in root.Entries)
                {
                    string? raw;
                    if (val is { Kind: JKind.Object })
                        raw = val["key"]?.AsString() ?? val["apiKey"]?.AsString();
                    else
                        raw = val?.AsString();
                    if (string.IsNullOrWhiteSpace(raw) || IsEnvVarRef(raw)) continue;
                    if (!Has(pid)) Set(pid, raw.Trim());
                    Add(pid, "OpenCode");
                }
            }
        }
        catch { }

        return imported;
    }

    // ════════════════════════════════════════════════════════════
    // 内部
    // ════════════════════════════════════════════════════════════

    private static Dictionary<string, KeyEntry> Load()
    {
        if (_keys != null) return _keys;

        lock (_lock)
        {
            if (_keys != null) return _keys;

            var result = new Dictionary<string, KeyEntry>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var root = Json.Parse(json);

                    if (root is { Kind: JKind.Array } arr)
                    {
                        // 新格式：[ { "provider": "...", "apikey": "...", "expiry": "2026-11-22" } ]
                        foreach (var item in arr.Items)
                        {
                            if (item.Kind != JKind.Object) continue;
                            var pid = item["provider"]?.AsString();
                            var key = item["apikey"]?.AsString() ?? item["key"]?.AsString();
                            if (!string.IsNullOrWhiteSpace(pid) && !string.IsNullOrWhiteSpace(key))
                                result[pid.ToLowerInvariant()] = new KeyEntry(key, NormalizeExpiry(item["expiry"]?.AsString()),
                                    item["base_url"]?.AsString());
                        }
                    }
                    else if (root is { Kind: JKind.Object } obj)
                    {
                        // 兼容旧格式：{ "deepseek": "sk-..." } 或 { "deepseek": { "type": "api", "key": "..." } }
                        foreach (var (pid, val) in obj.Entries)
                        {
                            var key = ParseCredentialKey(val);
                            if (key != null)
                                result[pid.ToLowerInvariant()] = new KeyEntry(key, null);
                        }
                    }
                }
            }
            catch
            {
                result = [];
            }

            _keys = result;
            return _keys;
        }
    }

    /// <summary>解析单个凭据值：对象 { key: "..." } 或纯字符串</summary>
    private static string? ParseCredentialKey(JNode? val)
    {
        if (val is { Kind: JKind.Object })
            return val["key"]?.AsString();
        if (val is { Kind: JKind.String })
            return val.AsString();
        return null;
    }

    private static bool Save(Dictionary<string, KeyEntry> keys)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var arr = JNode.Array();
            foreach (var (pid, entry) in keys)
            {
                var item = JNode.Object().Set("provider", pid).Set("apikey", entry.ApiKey);
                if (!string.IsNullOrWhiteSpace(entry.Expiry))
                    item.Set("expiry", entry.Expiry);
                if (!string.IsNullOrWhiteSpace(entry.BaseUrl))
                    item.Set("base_url", entry.BaseUrl); // key 关联的调用地址
                arr.Add(item);
            }

            // 原子写：先落临时文件再 rename 覆盖，避免中途崩溃留下半截 JSON（下次启动解析失败丢全部 key）。
            var json = arr.ToJson(true);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, FilePath, overwrite: true);

            _keys = keys;
            return true;
        }
        catch (Exception ex)
        {
            // 保存失败可见：此前静默吞 → 用户以为存了，重启后 Key 丢失
            ErrorLog.Error("ApiKeyStore", $"API Key 保存失败（{FilePath}）: {ex.Message}");
            return false;
        }
    }

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { lock (_lock) { _keys = null; } }
}
