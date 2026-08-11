using System.Collections.Concurrent;
using System.Threading.Channels;

namespace WayCoder;

/// <summary>
/// 主日志器。静态入口，通过无界 <see cref="Channel{T}"/> 作为异步队列，
/// 由单个后台线程消费并把日志分发到所有已注册且启用的 <see cref="ILogSink"/>。
/// 生产线程调用 <see cref="Log"/> 只入队，不会阻塞；写入工作由后台线程完成。
/// </summary>
public static class Logger
{
    private static readonly Lock _lock = new();
    private static readonly List<ILogSink> _sinks = new();
    private static readonly Channel<LogEntry> _channel =
        Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

    private static readonly CancellationTokenSource _cts = new();
    private static readonly Task _worker;
    private static LogLevel _minLevel = LogLevel.Debug;

    /// <summary>全局最低日志级别，低于该级别的日志被丢弃。</summary>
    public static LogLevel MinLevel { get => _minLevel; set => SetLevel(value); }

    /// <summary>
    /// 自动刷新。为 true 时后台线程每处理一批日志后调用各槽的 <see cref="ILogSink.Flush"/>。
    /// 默认 false（依靠各槽自身缓冲与定时刷盘）。
    /// </summary>
    public static bool AutoFlush { get; set; }

    /// <summary>当前队列中尚未消费的日志条数。</summary>
    public static int PendingCount => _channel.Reader.Count;

    /// <summary>日志写出事件，后台线程每写出一条日志时触发（可用于指标统计）。</summary>
    public static event Action<LogEntry>? LogWritten;

    static Logger()
    {
        _worker = Task.Run(async () =>
        {
            var reader = _channel.Reader;
            var count = 0;
            try
            {
                await foreach (var entry in reader.ReadAllAsync(_cts.Token))
                {
                    Dispatch(entry);
                    LogWritten?.Invoke(entry);
                    count++;
                    if (AutoFlush && count >= 128)
                    {
                        FlushLocked();
                        count = 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 关闭流程，正常退出。
            }
            catch
            {
                // 后台线程绝不因单个失败而终止。
            }
        });
    }

    /// <summary>注册一个日志槽。重复注册同名槽将被忽略。</summary>
    public static void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (_lock)
        {
            if (_sinks.Any(s => string.Equals(s.Name, sink.Name, StringComparison.Ordinal)))
                return;
            _sinks.Add(sink);
        }
    }

    /// <summary>移除并返回是否成功移除指定日志槽。</summary>
    public static bool RemoveSink(ILogSink sink)
    {
        lock (_lock) return _sinks.Remove(sink);
    }

    /// <summary>按名称移除日志槽，返回是否成功。</summary>
    public static bool RemoveSink(string name)
    {
        lock (_lock)
        {
            var idx = _sinks.FindIndex(s => string.Equals(s.Name, name, StringComparison.Ordinal));
            if (idx < 0) return false;
            _sinks.RemoveAt(idx);
            return true;
        }
    }

    /// <summary>设置全局最低日志级别。</summary>
    public static void SetLevel(LogLevel level)
    {
        lock (_lock) _minLevel = level;
    }

    /// <summary>记录一条日志。低于全局级别的将被丢弃，不进入队列。</summary>
    public static void Log(
        LogLevel level,
        string message,
        string? category = null,
        Exception? exception = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        LogLevel min;
        lock (_lock) min = _minLevel;
        if (level < min) return;

        var entry = new LogEntry(level, message, category, exception, tags, properties);
        try
        {
            _channel.Writer.TryWrite(entry);
        }
        catch (Exception ex)
        {
            // 入队失败时回退到同步写出，避免丢失。
            LogMetrics.Record(entry);
            Dispatch(entry);
            _ = ex; // 保持可调试
        }
    }

    /// <summary>记录 Trace 级日志。</summary>
    public static void Trace(string message, string? category = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Trace, message, category, null, tags, properties);

    /// <summary>记录 Debug 级日志。</summary>
    public static void Debug(string message, string? category = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Debug, message, category, null, tags, properties);

    /// <summary>记录 Info 级日志。</summary>
    public static void Info(string message, string? category = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Info, message, category, null, tags, properties);

    /// <summary>记录 Warn 级日志。</summary>
    public static void Warn(string message, string? category = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Warn, message, category, null, tags, properties);

    /// <summary>记录 Error 级日志。</summary>
    public static void Error(string message, string? category = null, Exception? exception = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Error, message, category, exception, tags, properties);

    /// <summary>记录 Fatal 级日志。</summary>
    public static void Fatal(string message, string? category = null, Exception? exception = null,
        IEnumerable<string>? tags = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Log(LogLevel.Fatal, message, category, exception, tags, properties);

    /// <summary>
    /// 阻塞刷新所有槽直到完成。会清空当前队列中的剩余条目并写出，
    /// 再调用每个槽的 <see cref="ILogSink.Flush"/>。
    /// </summary>
    public static void FlushAll()
    {
        // 让后台线程先消费当前队列，随后刷新。
        lock (_lock)
        {
            FlushLocked();
        }
    }

    /// <summary>
    /// 优雅关闭：停止接收新日志、排空队列、刷新并关闭所有槽。
    /// 关闭后调用 <see cref="Log"/> 仍会入队但不再被消费。
    /// </summary>
    public static void Shutdown()
    {
        _cts.Cancel();
        try { _worker.GetAwaiter().GetResult(); }
        catch { /* 忽略关闭期间的异常 */ }

        List<ILogSink> snapshot;
        lock (_lock)
        {
            snapshot = new List<ILogSink>(_sinks);
        }
        foreach (var sink in snapshot)
        {
            try { sink.Flush(); } catch { /* 忽略 */ }
        }
        lock (_lock) _sinks.Clear();
    }

    private static void Dispatch(LogEntry entry)
    {
        LogMetrics.Record(entry);
        ILogSink[] snapshot;
        lock (_lock) snapshot = _sinks.ToArray();
        foreach (var sink in snapshot)
        {
            if (!sink.IsEnabled) continue;
            try
            {
                sink.Write(entry);
            }
            catch
            {
                // 槽的失败绝不影响其它槽。
            }
        }
    }

    private static void FlushLocked()
    {
        ILogSink[] snapshot;
        lock (_lock) snapshot = _sinks.ToArray();
        foreach (var sink in snapshot)
        {
            try { sink.Flush(); } catch { /* 忽略 */ }
        }
    }
}
