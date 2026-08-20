namespace WayCoder;

/// <summary>
/// API Key 管理器 —— 按「服务商」存储 API Key（key 跟服务商走，不跟模型走）。
/// 保存到全局 ~/.waycoder/api_keys.json，支持多个服务商的 Key。
/// 文件格式（对标 OpenCode/Crush 的多 key 全局存储）：
///   [ { "provider": "deepseek", "apikey": "sk-..." }, ... ]
/// </summary>
public static class ApiKeyStore
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _keys;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".waycoder", "api_keys.json");

    /// <summary>获取指定服务商的 API Key，未存储返回 null</summary>
    public static string? Get(string providerId)
    {
        var keys = Load();
        return keys.TryGetValue(providerId.ToLowerInvariant(), out var key) ? key : null;
    }

    /// <summary>存储指定服务商的 API Key。返回是否保存成功（失败已记日志，调用方可提示用户）。</summary>
    public static bool Set(string providerId, string apiKey)
    {
        lock (_lock)
        {
            var keys = Load();
            var pid = providerId.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(apiKey))
                keys.Remove(pid);
            else
                keys[pid] = apiKey.Trim();
            return Save(keys);
        }
    }

    /// <summary>删除指定服务商的 Key</summary>
    public static void Remove(string providerId)
    {
        lock (_lock)
        {
            var keys = Load();
            keys.Remove(providerId.ToLowerInvariant());
            Save(keys);
        }
    }

    /// <summary>列出所有已存服务商（服务商ID → key）</summary>
    public static Dictionary<string, string> ListAll() => Load();

    /// <summary>检查是否有指定服务商的 Key</summary>
    public static bool Has(string providerId) =>
        Load().ContainsKey(providerId.ToLowerInvariant());

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
    /// 返回已导入的供应商 ID 列表。纯逻辑便于自测（不读文件）。
    /// </summary>
    public static List<string> ImportFromEnvironment()
    {
        var imported = new List<string>();
        foreach (var (pid, envVar) in ProviderEnvVar)
        {
            var val = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(val))
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
            if (!string.IsNullOrWhiteSpace(val))
            {
                Set(pid, val.Trim());
                imported.Add(pid);
            }
        }
        return imported;
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

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

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
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        var pid = ProviderFromEnvVarName(key);
                        if (pid == null) continue;
                        Set(pid, raw.Trim());
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
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        var pid = ProviderFromEnvVarName(key);
                        if (pid == null) continue;
                        Set(pid, raw.Trim());
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
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    Set(pid, raw.Trim());
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
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    Set(pid, raw.Trim());
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

    private static Dictionary<string, string> Load()
    {
        if (_keys != null) return _keys;

        lock (_lock)
        {
            if (_keys != null) return _keys;

            var result = new Dictionary<string, string>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var root = Json.Parse(json);

                    if (root is { Kind: JKind.Array } arr)
                    {
                        // 新格式：[ { "provider": "...", "apikey": "..." } ]
                        foreach (var item in arr.Items)
                        {
                            if (item.Kind != JKind.Object) continue;
                            var pid = item["provider"]?.AsString();
                            var key = item["apikey"]?.AsString() ?? item["key"]?.AsString();
                            if (!string.IsNullOrWhiteSpace(pid) && !string.IsNullOrWhiteSpace(key))
                                result[pid.ToLowerInvariant()] = key;
                        }
                    }
                    else if (root is { Kind: JKind.Object } obj)
                    {
                        // 兼容旧格式：{ "deepseek": "sk-..." } 或 { "deepseek": { "type": "api", "key": "..." } }
                        foreach (var (pid, val) in obj.Entries)
                        {
                            var key = ParseCredentialKey(val);
                            if (key != null)
                                result[pid.ToLowerInvariant()] = key;
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

    private static bool Save(Dictionary<string, string> keys)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var arr = JNode.Array();
            foreach (var (pid, key) in keys)
                arr.Add(JNode.Object().Set("provider", pid).Set("apikey", key));

            _keys = keys;
            var json = arr.ToJson(true);
            File.WriteAllText(FilePath, json);
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
