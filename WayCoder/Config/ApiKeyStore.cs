using System.Text.Json;

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

    /// <summary>存储指定服务商的 API Key</summary>
    public static void Set(string providerId, string apiKey)
    {
        lock (_lock)
        {
            var keys = Load();
            var pid = providerId.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(apiKey))
                keys.Remove(pid);
            else
                keys[pid] = apiKey.Trim();
            Save(keys);
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
                    var root = JsonNode.Parse(json);

                    if (root is JsonArray arr)
                    {
                        // 新格式：[ { "provider": "...", "apikey": "..." } ]
                        foreach (var item in arr)
                        {
                            if (item is not JsonObject o) continue;
                            var pid = o["provider"]?.GetValue<string>();
                            var key = o["apikey"]?.GetValue<string>() ?? o["key"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(pid) && !string.IsNullOrWhiteSpace(key))
                                result[pid.ToLowerInvariant()] = key;
                        }
                    }
                    else if (root is JsonObject obj)
                    {
                        // 兼容旧格式：{ "deepseek": "sk-..." } 或 { "deepseek": { "type": "api", "key": "..." } }
                        foreach (var (pid, val) in obj)
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
    private static string? ParseCredentialKey(JsonNode? val)
    {
        if (val is JsonObject o)
            return o["key"]?.GetValue<string>();
        if (val is JsonValue v && v.TryGetValue<string>(out var s))
            return s;
        return null;
    }

    private static void Save(Dictionary<string, string> keys)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var arr = new JsonArray();
            foreach (var (pid, key) in keys)
                arr.Add(new JsonObject { ["provider"] = pid, ["apikey"] = key });

            _keys = keys;
            var json = arr.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* 写入失败不崩溃 */ }
    }

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { lock (_lock) { _keys = null; } }
}
