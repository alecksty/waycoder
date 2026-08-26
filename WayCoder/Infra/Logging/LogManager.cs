namespace WayCoder;

/// <summary>
/// 日志系统统一入口。负责静态初始化配置、预配置 Console + File 双槽、
/// 提供便捷的静态日志方法，并在退出时优雅关闭。
/// </summary>
public static class LogManager
{
    private static readonly Lock _lock = new();
    private static volatile bool _initialized;

    private static ConsoleLogSink? _consoleSink;
    private static FileLogSink? _fileSink;
    private static JsonLogSink? _jsonSink;

    /// <summary>是否已完成初始化。</summary>
    public static bool IsInitialized => _initialized;

    /// <summary>已注册的控制台槽（初始化后非空）。</summary>
    public static ILogSink? ConsoleSink => _consoleSink;

    /// <summary>已注册的文件槽（初始化后非空）。</summary>
    public static ILogSink? FileSink => _fileSink;

    /// <summary>已注册的 JSON 槽（启用后非空）。</summary>
    public static ILogSink? JsonSink => _jsonSink;

    /// <summary>默认日志目录。未显式指定时，Unix 用 ~/.waycoder/logs，否则 ./logs。</summary>
    public static string DefaultDirectory()
    {
        var home = Global.Home;
        return string.IsNullOrEmpty(home)
            ? System.IO.Path.Combine(Environment.CurrentDirectory, "logs")
            : System.IO.Path.Combine(home, ".waycoder", "logs");
    }

    /// <summary>
    /// 初始化日志系统。可重复调用，但只有首次会实际注册槽。
    /// 预配置 Console（stderr、着色）与 File（默认目录、按大小/日期轮转）双槽。
    /// </summary>
    /// <param name="logDirectory">文件日志目录；null 使用 <see cref="DefaultDirectory"/>。</param>
    /// <param name="minLevel">全局最低日志级别。</param>
    /// <param name="consoleEnabled">是否启用控制台槽。</param>
    /// <param name="fileEnabled">是否启用文件槽。</param>
    /// <param name="jsonEnabled">是否额外启用 JSON 槽（默认 false）。</param>
    /// <param name="appName">文件与 JSON 日志的文件名前缀。</param>
    public static void Init(
        string? logDirectory = null,
        LogLevel minLevel = LogLevel.Info,
        bool consoleEnabled = true,
        bool fileEnabled = true,
        bool jsonEnabled = false,
        string appName = "waycoder")
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            Logger.SetLevel(minLevel);

            if (consoleEnabled)
            {
                _consoleSink = new ConsoleLogSink(minLevel);
                Logger.AddSink(_consoleSink);
            }

            if (fileEnabled)
            {
                var dir = logDirectory ?? DefaultDirectory();
                _fileSink = new FileLogSink(dir, appName, LogLevel.Trace,
                    maxFileSizeBytes: 10L * 1024 * 1024, rotateByDate: true, buffered: true);
                Logger.AddSink(_fileSink);
            }

            if (jsonEnabled)
            {
                var dir = logDirectory ?? DefaultDirectory();
                _jsonSink = new JsonLogSink(dir, appName, LogLevel.Trace);
                Logger.AddSink(_jsonSink);
            }

            _initialized = true;
        }
    }

    /// <summary>
    /// 确保已初始化（未初始化时用默认配置初始化）。供便捷方法内部调用。
    /// </summary>
    private static void EnsureInit()
    {
        if (!_initialized) Init();
    }

    /// <summary>记录一条日志。</summary>
    public static void Log(LogLevel level, string message, string? category = null,
        Exception? exception = null, IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        EnsureInit();
        Logger.Log(level, message, category, exception, tags, properties);
    }

    /// <summary>记录 Debug 级日志。</summary>
    public static void Debug(string message, string? category = null)
        => Log(LogLevel.Debug, message, category);

    /// <summary>记录 Info 级日志。</summary>
    public static void Info(string message, string? category = null)
        => Log(LogLevel.Info, message, category);

    /// <summary>记录 Warn 级日志。</summary>
    public static void Warn(string message, string? category = null)
        => Log(LogLevel.Warn, message, category);

    /// <summary>记录 Error 级日志。</summary>
    public static void Error(string message, string? category = null, Exception? exception = null)
        => Log(LogLevel.Error, message, category, exception);

    /// <summary>记录 Fatal 级日志。</summary>
    public static void Fatal(string message, string? category = null, Exception? exception = null)
        => Log(LogLevel.Fatal, message, category, exception);

    /// <summary>刷新所有已注册槽。</summary>
    public static void Flush()
    {
        if (!_initialized) return;
        Logger.FlushAll();
    }

    /// <summary>
    /// 优雅关闭日志系统：排空队列、刷新并释放文件句柄。
    /// 应用退出前应调用一次。
    /// </summary>
    public static void Shutdown()
    {
        if (!_initialized) return;
        _initialized = false;

        try
        {
            Logger.FlushAll();
            Logger.Shutdown();
        }
        finally
        {
            _consoleSink = null;
            _fileSink = null;
            _jsonSink = null;
        }
    }
}
