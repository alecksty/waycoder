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
    private readonly LogLevel _minLevel;
    /// <summary>句柄生命周期与轮转策略全在它手里（与 JsonLogSink 共用唯一实现）。</summary>
    private readonly RotatingFileWriter _file;

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
        _minLevel = minLevel;
        _file = new RotatingFileWriter(_directory, appName, ".log",
            rotateByDate: rotateByDate,
            maxFileSizeBytes: maxFileSizeBytes,
            flushEveryWrite: !buffered);
    }

    /// <summary>写出一条日志到文件。</summary>
    public void Write(LogEntry entry)
    {
        if (entry.Level < _minLevel) return;

        _file.Write(entry.ToString() + Environment.NewLine, entry.Timestamp);
    }

    /// <summary>刷盘，确保缓冲内容写入磁盘。</summary>
    public void Flush() => _file.Flush();

    /// <summary>关闭并释放文件句柄。</summary>
    public void Dispose() => _file.Dispose();
}
