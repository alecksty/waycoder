namespace WayCoder;

/// <summary>
/// 日志运行指标。按级别统计计数、保留最近 N 条日志的环形缓冲、
/// 统计每秒写入速率与总字节数，并提供 <see cref="Reset"/> 清零。
/// 线程安全。由 <see cref="Logger"/> 在后台线程自动调用 <see cref="Record"/>。
/// </summary>
public static class LogMetrics
{
    private static readonly long[] _counts = new long[6];
    private static readonly Lock _lock = new();

    private static readonly List<LogEntry> _ring = new();
    private static int _ringCapacity = 256;
    private static int _ringIndex;

    private static long _totalBytes;
    private static long _totalEntries;

    private const int RateWindowSeconds = 1;
    private static readonly Queue<(long Ticks, long Count)> _rateSamples = new();
    private static long _rateTotal;

    /// <summary>各级别日志计数快照。</summary>
    public static IReadOnlyList<long> Counts
    {
        get
        {
            lock (_lock)
            {
                var copy = new long[_counts.Length];
                Array.Copy(_counts, copy, _counts.Length);
                return copy;
            }
        }
    }

    /// <summary>累计日志总条数。</summary>
    public static long TotalEntries { get { lock (_lock) return _totalEntries; } }

    /// <summary>累计写入的总字节数（估算，UTF-8）。</summary>
    public static long TotalBytes { get { lock (_lock) return _totalBytes; } }

    /// <summary>环形缓冲保留的最近日志条数上限。</summary>
    public static int RingCapacity
    {
        get { lock (_lock) return _ringCapacity; }
        set
        {
            lock (_lock)
            {
                _ringCapacity = Math.Max(1, value);
                while (_ring.Count > _ringCapacity) _ring.RemoveAt(0);
            }
        }
    }

    /// <summary>每秒日志速率（基于最近 1 秒滑动窗口）。</summary>
    public static long RatePerSecond { get { lock (_lock) return ComputeRate(); } }

    /// <summary>记录一条日志的指标。由 <see cref="Logger"/> 调用。</summary>
    internal static void Record(LogEntry entry)
    {
        lock (_lock)
        {
            var idx = (int)entry.Level;
            if (idx >= 0 && idx < _counts.Length) _counts[idx]++;

            _totalEntries++;
            _totalBytes += System.Text.Encoding.UTF8.GetByteCount(entry.Message);

            if (_ring.Count < _ringCapacity)
            {
                _ring.Add(entry);
            }
            else
            {
                _ring[_ringIndex] = entry;
                _ringIndex = (_ringIndex + 1) % _ringCapacity;
            }

            var now = DateTime.UtcNow.Ticks;
            _rateTotal++;
            _rateSamples.Enqueue((now, 1));
            while (_rateSamples.Count > 0 &&
                   now - _rateSamples.Peek().Ticks > RateWindowSeconds * TimeSpan.TicksPerSecond)
            {
                _rateTotal -= _rateSamples.Dequeue().Count;
            }
        }
    }

    /// <summary>获取指定级别的累计计数。</summary>
    public static long GetCount(LogLevel level)
    {
        lock (_lock)
        {
            var idx = (int)level;
            return idx >= 0 && idx < _counts.Length ? _counts[idx] : 0;
        }
    }

    /// <summary>
    /// 返回最近日志的只读副本，按产生时间顺序排列（最旧在前）。
    /// </summary>
    public static IReadOnlyList<LogEntry> Recent(int count)
    {
        lock (_lock)
        {
            var take = Math.Min(count, _ring.Count);
            if (take <= 0) return Array.Empty<LogEntry>();
            var result = new LogEntry[take];
            if (_ring.Count < _ringCapacity)
            {
                // 尚未填满：直接取尾部。
                for (var i = 0; i < take; i++)
                    result[i] = _ring[_ring.Count - take + i];
            }
            else
            {
                // 已填满：从 _ringIndex 之后按环形顺序取。
                for (var i = 0; i < take; i++)
                    result[i] = _ring[(_ringIndex + _ring.Count - take + i) % _ringCapacity];
            }
            return result;
        }
    }

    /// <summary>清零所有计数、缓冲和速率样本。</summary>
    public static void Reset()
    {
        lock (_lock)
        {
            Array.Clear(_counts);
            _totalEntries = 0;
            _totalBytes = 0;
            _ring.Clear();
            _ringIndex = 0;
            _rateSamples.Clear();
            _rateTotal = 0;
        }
    }

    private static long ComputeRate()
    {
        var now = DateTime.UtcNow.Ticks;
        while (_rateSamples.Count > 0 &&
               now - _rateSamples.Peek().Ticks > RateWindowSeconds * TimeSpan.TicksPerSecond)
        {
            _rateTotal -= _rateSamples.Dequeue().Count;
        }
        return _rateTotal;
    }
}
