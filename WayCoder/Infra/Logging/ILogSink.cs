namespace WayCoder;

/// <summary>
/// 日志输出槽接口。所有日志目标（控制台、文件、JSON 等）都实现此接口。
/// <see cref="Write"/> 通常由后台线程调用，实现须保证线程安全。
/// </summary>
public interface ILogSink
{
    /// <summary>槽的唯一名称，用于识别与配置。</summary>
    string Name { get; }

    /// <summary>槽当前是否启用。停用的槽将跳过写入。</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 写入一条日志。该调用可能在任意线程上发生，实现必须线程安全。
    /// 不允许抛出异常——写入失败应被内部吞掉或降级处理。
    /// </summary>
    void Write(LogEntry entry);

    /// <summary>
    /// 将内部缓冲的数据刷新到最终目标（如刷盘）。阻塞直到完成。
    /// </summary>
    void Flush();
}
