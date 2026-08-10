using System.Collections.Concurrent;

namespace WayCoder;

/// <summary>
/// 文件锁管理器 — 防止多个 Agent 并发修改同一文件。
/// 支持超时自动释放、死锁检测、UI 状态查询。
/// </summary>
public static class FileLockManager
{
    private static readonly ConcurrentDictionary<string, FileLock> _locks = new();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public record FileLock(string FilePath, string AgentId, DateTime AcquiredAt, TimeSpan Timeout)
    {
        public bool IsExpired => DateTime.UtcNow - AcquiredAt > Timeout;
        public string Status =>
            IsExpired ? "⏰ 已过期" :
            $"🔒 {AgentId} ({AcquiredAt:HH:mm:ss})";
    }

    /// <summary>尝试获取文件锁。返回 true 表示成功。</summary>
    public static bool TryAcquire(string filePath, string agentId = "main",
        TimeSpan? timeout = null)
    {
        var path = NormalizePath(filePath);
        var now = DateTime.UtcNow;
        var t = timeout ?? DefaultTimeout;

        // 清理过期锁
        CleanupExpired();

        var newLock = new FileLock(path, agentId, now, t);
        var existing = _locks.GetOrAdd(path, newLock);

        if (existing == newLock) return true; // 新锁成功

        // 已存在的锁
        if (existing.IsExpired)
        {
            // 过期锁 → 强制获取
            _locks.TryUpdate(path, newLock, existing);
            return true;
        }

        if (existing.AgentId == agentId)
        {
            // 同一 agent 可重新获取（续期）
            _locks.TryUpdate(path, newLock, existing);
            return true;
        }

        return false; // 被其他 agent 锁定
    }

    /// <summary>释放文件锁</summary>
    public static void Release(string filePath, string agentId = "main")
    {
        var path = NormalizePath(filePath);
        if (_locks.TryGetValue(path, out var existing) && existing.AgentId == agentId)
            _locks.TryRemove(path, out _);
    }

    /// <summary>释放某个 agent 的所有锁</summary>
    public static void ReleaseAll(string agentId = "main")
    {
        foreach (var kv in _locks.Where(kv => kv.Value.AgentId == agentId).ToList())
            _locks.TryRemove(kv.Key, out _);
    }

    /// <summary>检查文件是否被锁定（不含当前 agent）</summary>
    public static bool IsLockedByOther(string filePath, string agentId = "main")
    {
        var path = NormalizePath(filePath);
        CleanupExpired();
        return _locks.TryGetValue(path, out var l) && !l.IsExpired && l.AgentId != agentId;
    }

    /// <summary>获取文件锁信息（UI 用）</summary>
    public static FileLock? GetLockInfo(string filePath)
    {
        var path = NormalizePath(filePath);
        CleanupExpired();
        return _locks.TryGetValue(path, out var l) && !l.IsExpired ? l : null;
    }

    /// <summary>获取所有活跃锁（UI 用）</summary>
    public static List<FileLock> GetAllLocks()
    {
        CleanupExpired();
        return _locks.Values.Where(l => !l.IsExpired).OrderBy(l => l.FilePath).ToList();
    }

    /// <summary>等待获取锁（带超时）。返回 true 表示成功。</summary>
    public static async Task<bool> WaitForLockAsync(string filePath, string agentId = "main",
        TimeSpan? waitTimeout = null, CancellationToken ct = default)
    {
        var waitMs = (int)(waitTimeout ?? TimeSpan.FromSeconds(10)).TotalMilliseconds;
        var deadline = DateTime.UtcNow.AddMilliseconds(waitMs);

        while (DateTime.UtcNow < deadline)
        {
            if (TryAcquire(filePath, agentId)) return true;
            await Task.Delay(200, ct);
            if (ct.IsCancellationRequested) return false;
        }
        return false; // 超时
    }

    /// <summary>获取所有锁的摘要文本（状态栏用）</summary>
    public static string GetSummary()
    {
        var locks = GetAllLocks();
        if (locks.Count == 0) return "";
        return $"🔒 {locks.Count} 文件锁定";
    }

    // ---- 内部 ----

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).Replace('\\', '/');

    private static void CleanupExpired()
    {
        var expired = _locks.Where(kv => kv.Value.IsExpired).ToList();
        foreach (var kv in expired)
            _locks.TryRemove(kv.Key, out _);
    }
}
