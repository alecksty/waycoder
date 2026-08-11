using System.Text;

namespace WayCoder;

/// <summary>
/// 文件日志槽。自动创建目录，支持按大小和按日期两种轮转策略，
/// 写入时使用缓冲的 <see cref="StreamWriter"/>，并通过 <see cref="Flush"/> 刷盘。
/// 文件名格式：`{appName}.{yyyyMMdd}.log`。
/// </summary>
public sealed class FileLogSink : ILogSink
{
    private readonly string _directory;
    private readonly string _appName;
    private readonly LogLevel _minLevel;
    private readonly long _maxFileSizeBytes;
    private readonly bool _rotateByDate;
    private readonly bool _buffered;
    private readonly Lock _lock = new();

    private StreamWriter? _writer;
    private DateTime _currentDate;
    private long _currentSize;
    private int _rotateSeq;

    /// <summary>槽名称。</summary>
    public string Name => "file";

    /// <summary>槽是否启用。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>日志文件所在目录。</summary>
    public string Directory => _directory;

    /// <summary>
    /// 创建一个文件日志槽。
    /// </summary>
    /// <param name="directory">日志目录，自动创建。</param>
    /// <param name="appName">日志文件名前缀。</param>
    /// <param name="minLevel">该槽的最低日志级别。</param>
    /// <param name="maxFileSizeBytes">单个文件超过此大小即轮转（0 表示不按大小轮转）。默认 10MB。</param>
    /// <param name="rotateByDate">跨天时是否新建文件。</param>
    /// <param name="buffered">是否使用写缓冲（减少 IO）；false 时每条即时写盘。</param>
    public FileLogSink(
        string directory,
        string appName = "app",
        LogLevel minLevel = LogLevel.Trace,
        long maxFileSizeBytes = 10L * 1024 * 1024,
        bool rotateByDate = true,
        bool buffered = true)
    {
        _directory = Path.GetFullPath(directory);
        _appName = string.IsNullOrWhiteSpace(appName) ? "app" : appName;
        _minLevel = minLevel;
        _maxFileSizeBytes = maxFileSizeBytes;
        _rotateByDate = rotateByDate;
        _buffered = buffered;
        System.IO.Directory.CreateDirectory(_directory);
    }

    /// <summary>写出一条日志到文件。</summary>
    public void Write(LogEntry entry)
    {
        if (entry.Level < _minLevel) return;

        var line = entry.ToString();
        var text = line + Environment.NewLine;

        lock (_lock)
        {
            try
            {
                EnsureOpen(entry.Timestamp);
                _writer!.Write(text);
                _currentSize += Encoding.UTF8.GetByteCount(text);
                if (!_buffered) _writer.Flush();
            }
            catch (IOException)
            {
                // 磁盘不可写等场景静默失败，避免影响主流程。
            }
        }
    }

    /// <summary>刷盘，确保缓冲内容写入磁盘。</summary>
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
        if (_writer is not null && _rotateByDate && now.Date != _currentDate)
        {
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
            _rotateSeq = 0;
        }

        if (_writer is null)
        {
            _currentDate = now.Date;
            _currentSize = 0;
            var path = BuildPath(_currentDate, _rotateSeq);
            _writer = new StreamWriter(
                new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        else if (_maxFileSizeBytes > 0 && _currentSize >= _maxFileSizeBytes)
        {
            _writer.Flush();
            _writer.Dispose();
            _rotateSeq++;
            var path = BuildPath(_currentDate, _rotateSeq);
            _currentSize = 0;
            _writer = new StreamWriter(
                new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private string BuildPath(DateTime date, int seq)
    {
        var name = $"{_appName}.{date:yyyyMMdd}" + (seq > 0 ? $".{seq}" : string.Empty) + ".log";
        return System.IO.Path.Combine(_directory, name);
    }
}
