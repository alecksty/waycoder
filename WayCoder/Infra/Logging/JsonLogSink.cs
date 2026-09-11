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
    private readonly LogLevel _minLevel;
    /// <summary>句柄生命周期与日期轮转全在它手里（与 FileLogSink 共用唯一实现）。</summary>
    private readonly RotatingFileWriter _file;

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
        _minLevel = minLevel;
        // NDJSON 逐条落盘，便于外部实时消费 —— 本 sink 不做按大小轮转
        _file = new RotatingFileWriter(_directory, appName, ".jsonl", flushEveryWrite: true);
    }

    /// <summary>以单行 JSON 写出一条日志。</summary>
    public void Write(LogEntry entry)
    {
        if (entry.Level < _minLevel) return;

        _file.Write(entry.ToJson() + "\n", entry.Timestamp);
    }

    /// <summary>刷盘。</summary>
    public void Flush() => _file.Flush();

    /// <summary>关闭并释放文件句柄。</summary>
    public void Dispose() => _file.Dispose();

}
