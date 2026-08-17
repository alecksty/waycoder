namespace WayCoder;

/// <summary>
/// 控制台日志槽。按日志级别对消息着色，可带时间戳前缀，
/// 默认输出到 stderr（避免污染 stdout 的标准输出管道）。
/// </summary>
public sealed class ConsoleLogSink : ILogSink
{
    private readonly LogLevel _minLevel;
    private readonly bool _useTimestamp;
    private readonly bool _useColor;
    private readonly TextWriter _writer;
    private readonly Lock _lock = new();

    /// <summary>槽名称。</summary>
    public string Name => "console";

    /// <summary>槽是否启用。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建一个控制台日志槽。
    /// </summary>
    /// <param name="minLevel">该槽的最低日志级别。</param>
    /// <param name="useTimestamp">是否打印时间戳前缀。</param>
    /// <param name="useColor">是否使用 ANSI 颜色。</param>
    /// <param name="toStderr">输出到 stderr；否则输出到 stdout。</param>
    public ConsoleLogSink(
        LogLevel minLevel = LogLevel.Trace,
        bool useTimestamp = true,
        bool useColor = true,
        bool toStderr = true)
    {
        _minLevel = minLevel;
        _useTimestamp = useTimestamp;
        _useColor = useColor;
        _writer = toStderr ? Console.Error : Console.Out;
    }

    /// <summary>写出一条日志到控制台。</summary>
    public void Write(LogEntry entry)
    {
        if (entry.Level < _minLevel) return;

        lock (_lock)
        {
            try
            {
                if (_useColor)
                {
                    _writer.Write(entry.Level.AnsiColor());
                    _writer.Write(entry.Level.Emoji());
                    _writer.Write(' ');
                }

                if (_useTimestamp)
                    _writer.Write($"[{entry.Timestamp:HH:mm:ss.fff}] ");

                _writer.Write('[');
                _writer.Write(entry.Level.Label());
                _writer.Write("] ");

                if (entry.Category is not null)
                {
                    _writer.Write(entry.Category);
                    _writer.Write(": ");
                }

                _writer.WriteLine(entry.Message);

                if (entry.Exception is not null)
                {
                    _writer.WriteLine(entry.Exception.ToString());
                }

                if (_useColor)
                    _writer.Write(LogLevelExtensions.ResetColor());

                _writer.Flush();
            }
            catch (IOException)
            {
                // 控制台被关闭（如管道中断）时静默失败。
            }
        }
    }

    /// <summary>控制台写入已即时刷新，此方法为空实现。</summary>
    public void Flush() { }
}
