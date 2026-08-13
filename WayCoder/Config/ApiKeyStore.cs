using System.Text.Json;

namespace WayCoder;

/// <summary>
/// API Key 管理器 —— 按提供商存储 API Key，记住不用重复输入。
/// 保存到 ~/.waycoder/api_keys.json，支持多个模型的 Key。
/// </summary>
public static class ApiKeyStore
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _keys;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".waycoder", "api_keys.json");

    /// <summary>获取指定提供商的 API Key，未存储返回 null</summary>
    public static string? Get(string providerId)
    {
        var keys = Load();
        return keys.TryGetValue(providerId.ToLowerInvariant(), out var key) ? key : null;
    }

    /// <summary>存储指定提供商的 API Key</summary>
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

    /// <summary>删除指定提供商的 Key</summary>
    public static void Remove(string providerId)
    {
        lock (_lock)
        {
            var keys = Load();
            keys.Remove(providerId.ToLowerInvariant());
            Save(keys);
        }
    }

    /// <summary>列出所有已存提供商</summary>
    public static Dictionary<string, string> ListAll() => Load();

    /// <summary>检查是否有指定提供商的 Key</summary>
    public static bool Has(string providerId) =>
        Load().ContainsKey(providerId.ToLowerInvariant());

    /// <summary>
    /// 按模型 ID 解析其供应商，返回该供应商已存 key；模型不在目录或未存 key 返回 null。
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

            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                            ?? [];
                }
                else
                {
                    _keys = [];
                }
            }
            catch
            {
                _keys = [];
            }

            return _keys;
        }
    }

    private static void Save(Dictionary<string, string> keys)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            _keys = keys;
            var json = JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* 写入失败不崩溃 */ }
    }

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { lock (_lock) { _keys = null; } }
}
