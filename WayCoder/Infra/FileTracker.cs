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

    /// <summary>文件上次读取时间（用于强制"先读后改"保护，对标 Crush last_read_time）</summary>
    private static readonly Dictionary<string, DateTime> LastReadTimes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>最多追踪的文件数</summary>
    private const int MaxTracked = 200;

    /// <summary>串行化对共享字典的访问（多槽位并行 Agent 并发读写）</summary>
    private static readonly Lock _lock = new();

    /// <summary>持久化文件路径（.waycoder/file-tracker.json，仅存哈希 + 读取时间，无数据库）</summary>
    private static string StorePath => Global.WriteConfigPath(Environment.CurrentDirectory, "file-tracker.json");

    /// <summary>是否已从磁盘加载过持久化数据</summary>
    private static bool _loaded;

    /// <summary>是否启用追踪（默认开启）</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// 记录文件读取事件，保存当前文件哈希用于后续变更检测。
    /// </summary>
    public static void RecordRead(string filePath)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(filePath)) return;

        lock (_lock)
        {
            try
            {
                EnsureLoaded();

                var absPath = Path.GetFullPath(filePath);
                if (!File.Exists(absPath)) return;

                // LRU 淘汰：超出上限时清理最久未读取的条目。
                // 不能用 Tracked.Keys.FirstOrDefault()——Dictionary 覆盖已存在键不改变枚举顺序，
                // 那只会淘汰「最早插入」的键（FIFO），热点文件照样被先清掉。
                if (Tracked.Count >= MaxTracked)
                {
                    string? oldest = null;
                    DateTime oldestTime = DateTime.MaxValue;
                    foreach (var path in Tracked.Keys)
                    {
                        var t = LastReadTimes.TryGetValue(path, out var v) ? v : DateTime.MinValue;
                        if (t < oldestTime) { oldestTime = t; oldest = path; }
                    }
                    if (oldest != null)
                    {
                        Tracked.Remove(oldest);
                        LastReadTimes.Remove(oldest);
                    }
                }

                var hash = ComputeHash(absPath);
                Tracked[absPath] = hash;
                LastReadTimes[absPath] = DateTime.UtcNow;

                Save();
            }
            catch
            {
                // 静默失败 — 文件追踪不应影响正常工具执行
            }
        }
    }

    /// <summary>
    /// 记录文件写入事件，更新哈希以反映 Agent 自己的修改。
    /// </summary>
    public static void RecordWrite(string filePath)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(filePath)) return;

        lock (_lock)
        {
            try
            {
                EnsureLoaded();

                var absPath = Path.GetFullPath(filePath);
                if (!File.Exists(absPath)) return;

                // LRU 淘汰：写入新路径同样执行上限淘汰。此前 RecordWrite 只增不减，
                // 大规模写入会令 Tracked/LastReadTimes 无界增长（违反 MaxTracked 上限）。
                if (!Tracked.ContainsKey(absPath) && Tracked.Count >= MaxTracked)
                {
                    string? oldest = null;
                    DateTime oldestTime = DateTime.MaxValue;
                    foreach (var path in Tracked.Keys)
                    {
                        var t = LastReadTimes.TryGetValue(path, out var v) ? v : DateTime.MinValue;
                        if (t < oldestTime) { oldestTime = t; oldest = path; }
                    }
                    if (oldest != null)
                    {
                        Tracked.Remove(oldest);
                        LastReadTimes.Remove(oldest);
                    }
                }

                var hash = ComputeHash(absPath);
                Tracked[absPath] = hash;
                // Agent 自己写入后同步「读取时间」，避免下一次编辑被 ValidatePreEdit
                // 用 fileModTime > lastRead+1s 误判为「自上次读取后被外部修改」。
                LastReadTimes[absPath] = DateTime.UtcNow;

                Save();
            }
            catch { }
        }
    }

    /// <summary>
    /// 检查已追踪文件自上次记录以来是否发生了变更。
    /// 返回变更文件的路径列表。
    /// </summary>
    public static List<string> CheckForChanges()
    {
        if (!Enabled) return new List<string>();

        lock (_lock)
        {
            EnsureLoaded();

            var changed = new List<string>();
            if (Tracked.Count == 0) return changed;

            var mutated = false;
            foreach (var (path, oldHash) in Tracked.ToList())
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        // 文件被删除
                        changed.Add(path);
                        Tracked.Remove(path);
                        mutated = true;
                        continue;
                    }

                    var newHash = ComputeHash(path);
                    if (newHash != oldHash)
                    {
                        changed.Add(path);
                        Tracked[path] = newHash; // 更新为最新哈希
                        mutated = true;
                    }
                }
                catch
                {
                    // 无法访问的文件从追踪中移除
                    Tracked.Remove(path);
                    mutated = true;
                }
            }

            if (mutated) Save();
            return changed;
        }
    }

    /// <summary>
    /// 获取指定文件的当前追踪状态。
    /// 返回 (isTracked, isStale) — isStale 表示文件自上次记录以来已变更。
    /// </summary>
    public static (bool isTracked, bool isStale) GetStatus(string filePath)
    {
        if (!Enabled) return (false, false);

        lock (_lock)
        {
            try
            {
                EnsureLoaded();

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
    /// 编辑前校验：确保文件先被 read_file 读取过，且读取后未被外部修改。
    /// 对标 Crush last_read_time 保护。
    /// 返回 null 表示通过，返回非 null 字符串表示警告/错误消息。
    /// </summary>
    public static string? ValidatePreEdit(string filePath)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(filePath)) return null;

        lock (_lock)
        {
            try
            {
                EnsureLoaded();

                var absPath = Path.GetFullPath(filePath);

                // 文件不存在 = 新建文件，不需要先读
                if (!File.Exists(absPath)) return null;

                // 从未被 read_file 读取过
                if (!LastReadTimes.TryGetValue(absPath, out var lastRead))
                    return $"⚠️ 文件 \"{filePath}\" 尚未被 read_file 读取。请先读取文件内容后再编辑，以确保编辑准确。";

                // 文件自上次读取后被外部修改
                var fileModTime = File.GetLastWriteTimeUtc(absPath);
                if (fileModTime > lastRead.AddSeconds(1))
                    return $"⚠️ 文件 \"{filePath}\" 自上次读取（{lastRead:HH:mm:ss}）后被外部修改（{fileModTime:HH:mm:ss}）。请重新 read_file 获取最新内容后再编辑。";

                return null;
            }
            catch
            {
                // 静默失败 — 文件追踪不应阻断正常工具执行
                return null;
            }
        }
    }

    /// <summary>
    /// 清除所有追踪记录（Agent 重置时调用）。
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _loaded = true; // 防止后续 EnsureLoaded 重新加载已清除的旧缓存
            Tracked.Clear();
            LastReadTimes.Clear();
            Save();
        }
    }

    /// <summary>首次使用时从磁盘加载持久化数据（惰性加载，仅一次）。</summary>
    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        Load();
    }

    /// <summary>测试钩子：清空内存并重新从磁盘加载，模拟进程重启。</summary>
    internal static void ReloadForTest()
    {
        lock (_lock)
        {
            _loaded = false;
            Tracked.Clear();
            LastReadTimes.Clear();
            EnsureLoaded();
        }
    }

    /// <summary>从 .waycoder/file-tracker.json 读取上次会话的哈希与读取时间。</summary>
    private static void Load()
    {
        try
        {
            var path = StorePath;
            if (!File.Exists(path)) return;

            var node = Json.Parse(File.ReadAllText(path));
            if (node?.Kind != JKind.Array) return;

            foreach (var item in node!.Items)
            {
                if (item.Kind != JKind.Object) continue;
                var p = item["path"]?.AsString();
                var h = item["hash"]?.AsString();
                if (string.IsNullOrEmpty(p) || string.IsNullOrEmpty(h)) continue;

                Tracked[p] = h;
                if (DateTime.TryParse(item["last_read"]?.AsString(), null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    LastReadTimes[p] = dt;

                // 上限保护：磁盘数据超出上限时丢弃多余条目
                if (Tracked.Count >= MaxTracked) break;
            }
        }
        catch
        {
            // 静默失败 — 缓存损坏或不可读时退化为内存模式，不影响正常执行
        }
    }

    /// <summary>将当前追踪状态写入 .waycoder/file-tracker.json（原子写，防止损坏）。</summary>
    private static void Save()
    {
        try
        {
            var arr = JNode.Array();
            foreach (var kv in Tracked)
            {
                arr.Add(JNode.Object()
                    .Set("path", kv.Key)
                    .Set("hash", kv.Value)
                    .Set("last_read", (LastReadTimes.TryGetValue(kv.Key, out var t) ? t : DateTime.UtcNow).ToString("O")));
            }

            var path = StorePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, arr.ToJson());
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            // 静默失败 — 持久化是尽力而为，内存追踪仍正常工作
        }
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
