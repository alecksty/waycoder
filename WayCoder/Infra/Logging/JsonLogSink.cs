using System.Text;

namespace WayCoder;

/// <summary>
/// JSON 日志槽。每条日志一行 JSON（NDJSON 格式），包含完整结构化字段
/// （时间戳、级别、消息、类别、标签、异常、属性），按日期分文件。
/// 文件名格式：`{appName}.{yyyyMMdd}.jsonl`。
/// </summary>
public sealed class JsonLogSink : ILogSink
{
    private readonly string _directory;
    private readonly string _appName;
    private readonly LogLevel _minLevel;
    private readonly Lock _lock = new();

    private StreamWriter? _writer;
    private DateTime _currentDate;

    /// <summary>槽名称。</summary>
    public string Name => "json";

    /// <summary>槽是否启用。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON 日志文件所在目录。</summary>
    public string Directory => _directory;

    /// <summary>
    /// 创建一个 JSON 日志槽。
    /// </summary>
    /// <param name="directory">日志目录，自动创建。</param>
    /// <param name="appName">日志文件名前缀。</param>
    /// <param name="minLevel">该槽的最低日志级别。</param>
    public JsonLogSink(string directory, string appName = "app", LogLevel minLevel = LogLevel.Trace)
    {
        _directory = Path.GetFullPath(directory);
        _appName = string.IsNullOrWhiteSpace(appName) ? "app" : appName;
        _minLevel = minLevel;
        System.IO.Directory.CreateDirectory(_directory);
    }

    /// <summary>以单行 JSON 写出一条日志。</summary>
    public void Write(LogEntry entry)
    {
        if (entry.Level < _minLevel) return;

        var json = entry.ToJson();
        lock (_lock)
        {
            try
            {
                EnsureOpen(entry.Timestamp);
                _writer!.Write(json);
                _writer.Write('\n');
                _writer.Flush(); // NDJSON 逐条落盘，便于外部实时消费
            }
            catch (IOException)
            {
                // 磁盘不可写时静默失败。
            }
        }
    }

    /// <summary>刷盘。</summary>
    public void Flush()
    {
        lock (_lock)
        {
            try { _writer?.Flush(); } catch (IOException) { /* 忽略 */ }
        }
    }

    /// <summary>关闭并释放文件句柄。</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            try { _writer?.Flush(); } catch { /* 忽略 */ }
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void EnsureOpen(DateTimeOffset ts)
    {
        var now = ts.LocalDateTime;
        if (_writer is not null && now.Date != _currentDate)
        {
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }

        if (_writer is null)
        {
            _currentDate = now.Date;
            var name = $"{_appName}.{now:yyyyMMdd}.jsonl";
            var path = System.IO.Path.Combine(_directory, name);
            _writer = new StreamWriter(
                new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}
