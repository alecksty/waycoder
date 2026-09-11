using System.Text;

namespace WayCoder;

/// <summary>
/// 「按日期（可选再按大小）轮转的文件写入器」—— **唯一实现**。
///
/// `FileLogSink`（按大小 + 按日期，带 `_rotateSeq`）与 `JsonLogSink`（仅按日期、逐条 flush）
/// 此前各写一遍 `EnsureOpen` / `Flush` / `Dispose` 样板，连 `StreamWriter` 的构造都逐字相同。
/// 这类代码是典型的「样板漏改一处」重灾区：**漏 Flush 会丢日志、漏 `FileShare.Read` 会把文件锁死**
/// （外部想 tail 一下都打不开），而两处各写一份时改一处忘另一处不会有任何编译期提示。
///
/// 差异（是否按大小轮转 / 是否逐条 flush）全部提成构造参数，语义仍由各 sink 自己决定。
/// </summary>
internal sealed class RotatingFileWriter : IDisposable
{
    private readonly string _directory;
    private readonly string _appName;
    private readonly string _extension;
    private readonly bool _rotateByDate;
    private readonly long _maxFileSizeBytes;
    private readonly bool _flushEveryWrite;
    private readonly Lock _lock = new();

    private StreamWriter? _writer;
    private DateTime _currentDate;
    private long _currentSize;
    private int _rotateSeq;

    /// <param name="extension">含点号，如 ".log" / ".jsonl"。</param>
    /// <param name="maxFileSizeBytes">单文件超过此大小即轮转（0 = 不按大小轮转）。</param>
    /// <param name="flushEveryWrite">是否每条即时落盘（NDJSON 供外部实时消费时用 true）。</param>
    internal RotatingFileWriter(string directory, string appName, string extension,
        bool rotateByDate = true, long maxFileSizeBytes = 0, bool flushEveryWrite = false)
    {
        _directory = Path.GetFullPath(directory);
        _appName = string.IsNullOrWhiteSpace(appName) ? "app" : appName;
        _extension = extension;
        _rotateByDate = rotateByDate;
        _maxFileSizeBytes = maxFileSizeBytes;
        _flushEveryWrite = flushEveryWrite;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>写出一段文本（换行由调用方自带）。</summary>
    internal void Write(string text, DateTimeOffset ts)
    {
        lock (_lock)
        {
            try
            {
                EnsureOpen(ts);
                _writer!.Write(text);
                _currentSize += Encoding.UTF8.GetByteCount(text);
                if (_flushEveryWrite) _writer.Flush();
            }
            catch (IOException)
            {
                // 磁盘不可写等场景静默失败，避免影响主流程。
            }
        }
    }

    /// <summary>刷盘，确保缓冲内容写入磁盘。</summary>
    internal void Flush()
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
            _writer = Open(BuildPath(_currentDate, _rotateSeq));
        }
        else if (_maxFileSizeBytes > 0 && _currentSize >= _maxFileSizeBytes)
        {
            _writer.Flush();
            _writer.Dispose();
            _rotateSeq++;
            _currentSize = 0;
            _writer = Open(BuildPath(_currentDate, _rotateSeq));
        }
    }

    /// <summary>打开文件句柄。`FileShare.Read` 让外部能 tail 日志，不加则会把文件独占锁死。</summary>
    private static StreamWriter Open(string path) => new(
        new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    private string BuildPath(DateTime date, int seq)
        => Path.Combine(_directory,
            $"{_appName}.{date:yyyyMMdd}" + (seq > 0 ? $".{seq}" : "") + _extension);
}
