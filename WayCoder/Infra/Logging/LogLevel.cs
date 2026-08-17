using WayCoder.UI.Shared.Terminal;

namespace WayCoder;

/// <summary>
/// 日志级别，从最详细到最严重递增。
/// </summary>
public enum LogLevel
{
    /// <summary>最详细的追踪信息，通常用于诊断内部流程。</summary>
    Trace = 0,

    /// <summary>调试信息，帮助定位问题。</summary>
    Debug = 1,

    /// <summary>常规信息，记录正常流程。</summary>
    Info = 2,

    /// <summary>警告，可能存在问题但流程继续。</summary>
    Warn = 3,

    /// <summary>错误，发生了失败但可恢复。</summary>
    Error = 4,

    /// <summary>致命错误，程序可能无法继续运行。</summary>
    Fatal = 5,
}

/// <summary>
/// <see cref="LogLevel"/> 的扩展方法：Emoji、ANSI 颜色和短标签映射。
/// </summary>
public static class LogLevelExtensions
{
    /// <summary>日志级别对应的 Emoji 图标。</summary>
    public static string Emoji(this LogLevel level) => level switch
    {
        LogLevel.Trace => "🔍",
        LogLevel.Debug => "🐛",
        LogLevel.Info => "ℹ️",
        LogLevel.Warn => "⚠️",
        LogLevel.Error => "❌",
        LogLevel.Fatal => "💥",
        _ => "ℹ️",
    };

    /// <summary>日志级别对应的 ANSI 前景色转义序列（不包含重置码）。统一走 AnsiTty，避免裸 \x1b。</summary>
    public static string AnsiColor(this LogLevel level) => level switch
    {
        LogLevel.Trace => AnsiTty.Fg(36),        // 青色
        LogLevel.Debug => AnsiTty.Fg(90),        // 亮黑
        LogLevel.Info => AnsiTty.Fg(36),         // 青色
        LogLevel.Warn => AnsiTty.Fg(33),         // 黄色
        LogLevel.Error => AnsiTty.Fg(31),        // 红色
        LogLevel.Fatal => AnsiTty.BoldFg(31),    // 亮红加粗
        _ => AnsiTty.SgrReset,
    };

    /// <summary>日志级别的三位短标签。</summary>
    public static string Label(this LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warn => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Fatal => "FTL",
        _ => "???",
    };

    /// <summary>重置 ANSI 颜色。</summary>
    public static string ResetColor() => AnsiTty.SgrReset;
}
