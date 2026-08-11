using System.Security.Cryptography;

namespace WayCoder;

/// <summary>
/// 文件追踪器 — 检测 stale-read：当 Agent 读取的文件被外部修改时发出警告。
///
/// 对标 crush filetracker：read_file 记录文件哈希，bash 执行后检查变更，
/// 帮助 LLM 感知外部命令对已读文件的修改。
/// </summary>
public static class FileTracker
{
    /// <summary>已追踪的文件及其 SHA256 哈希</summary>
    private static readonly Dictionary<string, string> Tracked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>最多追踪的文件数</summary>
    private const int MaxTracked = 200;

    /// <summary>是否启用追踪（默认开启）</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// 记录文件读取事件，保存当前文件哈希用于后续变更检测。
    /// </summary>
    public static void RecordRead(string filePath)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var absPath = Path.GetFullPath(filePath);
            if (!File.Exists(absPath)) return;

            // LRU 淘汰：超出上限时清理最旧的条目
            if (Tracked.Count >= MaxTracked)
            {
                var oldest = Tracked.Keys.FirstOrDefault();
                if (oldest != null) Tracked.Remove(oldest);
            }

            var hash = ComputeHash(absPath);
            Tracked[absPath] = hash;
        }
        catch
        {
            // 静默失败 — 文件追踪不应影响正常工具执行
        }
    }

    /// <summary>
    /// 记录文件写入事件，更新哈希以反映 Agent 自己的修改。
    /// </summary>
    public static void RecordWrite(string filePath)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var absPath = Path.GetFullPath(filePath);
            if (!File.Exists(absPath)) return;

            var hash = ComputeHash(absPath);
            Tracked[absPath] = hash;
        }
        catch { }
    }

    /// <summary>
    /// 检查已追踪文件自上次记录以来是否发生了变更。
    /// 返回变更文件的路径列表。
    /// </summary>
    public static List<string> CheckForChanges()
    {
        var changed = new List<string>();

        if (!Enabled || Tracked.Count == 0) return changed;

        foreach (var (path, oldHash) in Tracked.ToList())
        {
            try
            {
                if (!File.Exists(path))
                {
                    // 文件被删除
                    changed.Add(path);
                    Tracked.Remove(path);
                    continue;
                }

                var newHash = ComputeHash(path);
                if (newHash != oldHash)
                {
                    changed.Add(path);
                    Tracked[path] = newHash; // 更新为最新哈希
                }
            }
            catch
            {
                // 无法访问的文件从追踪中移除
                Tracked.Remove(path);
            }
        }

        return changed;
    }

    /// <summary>
    /// 获取指定文件的当前追踪状态。
    /// 返回 (isTracked, isStale) — isStale 表示文件自上次记录以来已变更。
    /// </summary>
    public static (bool isTracked, bool isStale) GetStatus(string filePath)
    {
        if (!Enabled) return (false, false);

        try
        {
            var absPath = Path.GetFullPath(filePath);
            if (!Tracked.TryGetValue(absPath, out var oldHash))
                return (false, false);

            if (!File.Exists(absPath))
                return (true, true); // 文件被删除

            var newHash = ComputeHash(absPath);
            return (true, newHash != oldHash);
        }
        catch
        {
            return (false, false);
        }
    }

    /// <summary>
    /// 生成变更警告消息（用于注入到 bash 工具返回中）。
    /// </summary>
    public static string? GetChangeWarning()
    {
        var changes = CheckForChanges();
        if (changes.Count == 0) return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("⚠️ **文件变更警告：以下已读取的文件被外部修改：**");
        foreach (var path in changes.Take(10))
        {
            sb.AppendLine($"  - `{path}`");
        }
        if (changes.Count > 10)
            sb.AppendLine($"  ... 及其他 {changes.Count - 10} 个文件");
        return sb.ToString();
    }

    /// <summary>
    /// 清除所有追踪记录（Agent 重置时调用）。
    /// </summary>
    public static void Reset()
    {
        Tracked.Clear();
    }

    /// <summary>SHA256 哈希计算</summary>
    private static string ComputeHash(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}
