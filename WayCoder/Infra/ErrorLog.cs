using System.Runtime.ExceptionServices;

namespace WayCoder;

/// <summary>
/// 统一错误日志系统 — 自动记录所有错误、警告和信息到日志文件。
///
/// 特性：
///   - 四级日志：Info / Warning / Error / Fatal
///   - 自动按天轮转日志文件（logs/error_YYYYMMDD.log）
///   - 结构化条目：时间戳 + 级别 + 来源 + 消息 + 异常信息
///   - 与 DebugLog 协同：Debug 模式下同时写入 LLM 会话日志
///   - 线程安全写入
///   - 自动捕获未处理异常（FirstChanceException）
///   - 内存缓冲 + 定时刷盘，减少 I/O 开销
/// </summary>
public static class ErrorLog
{
    public enum Level { Info, Warning, Error, Fatal }

    private static readonly Lock _lock = new();
    private static string? _logDir;
    private static string? _currentLogFile;
    private static string _currentDate = "";
    private static readonly List<string> _buffer = [];
    private static volatile bool _dirty;
    private static Timer? _flushTimer;
    private static volatile bool _initialized;
    private static volatile bool _catchAllExceptions;

    /// <summary>缓冲区最大条目数（达到后自动刷盘）</summary>
    private const int BufferFlushSize = 20;

    /// <summary>自动刷盘间隔（秒）</summary>
    private const int FlushIntervalSec = 5;

    /// <summary>日志目录名</summary>
    public const string LogDirName = "logs";

    /// <summary>是否已初始化</summary>
    public static bool Initialized => _initialized;

    /// <summary>
    /// 初始化日志系统。需要在程序启动早期调用。
    /// catchAllExceptions=true 时注册 FirstChanceException 处理。
    /// </summary>
    public static void Initialize(string? baseDir = null, bool catchAllExceptions = true)
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            var root = baseDir ?? Directory.GetCurrentDirectory();
            _logDir = Path.Combine(root, LogDirName);
            Directory.CreateDirectory(_logDir);

            _currentDate = DateTime.Now.ToString("yyyyMMdd");

            // 启动定时刷盘
            _flushTimer = new Timer(_ => Flush(), null,
                TimeSpan.FromSeconds(FlushIntervalSec),
                TimeSpan.FromSeconds(FlushIntervalSec));

            // 进程退出时刷盘
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                Flush();
                _flushTimer?.Dispose();
            };

            // 全局未处理异常
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Fatal("AppDomain.UnhandledException",
                    ex?.Message ?? args.ExceptionObject?.ToString() ?? "未知致命错误",
                    ex);
                Flush();
            };

            // Task 未观察异常
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Error("TaskScheduler.UnobservedTaskException",
                    $"未观察到的任务异常: {args.Exception?.Message}", args.Exception);
                args.SetObserved(); // 防止进程崩溃
            };

            // 首次机会异常（捕获所有 throw 的异常，无论是否被 catch）
            if (catchAllExceptions)
            {
                _catchAllExceptions = true;
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            }

            _initialized = true;
        }

        Info("ErrorLog", "错误日志系统已初始化", new Dictionary<string, object?>
        {
            ["logDir"] = _logDir,
            ["catchAll"] = catchAllExceptions
        });
    }

    /// <summary>
    /// 关闭日志系统并刷盘所有缓冲。
    /// </summary>
    public static void Shutdown()
    {
        Info("ErrorLog", "错误日志系统正在关闭...");

        if (_catchAllExceptions)
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
            _catchAllExceptions = false;
        }

        Flush();
        _flushTimer?.Dispose();
        _flushTimer = null;
        _initialized = false;
    }

    // ================================================================
    // 核心日志方法
    // ================================================================

    /// <summary>记录信息</summary>
    public static void Info(string source, string message, object? context = null)
        => Write(Level.Info, source, message, null, context);

    /// <summary>记录警告</summary>
    public static void Warning(string source, string message, Exception? ex = null, object? context = null)
        => Write(Level.Warning, source, message, ex, context);

    /// <summary>记录错误</summary>
    public static void Error(string source, string message, Exception? ex = null, object? context = null)
        => Write(Level.Error, source, message, ex, context);

    /// <summary>记录致命错误</summary>
    public static void Fatal(string source, string message, Exception? ex = null, object? context = null)
        => Write(Level.Fatal, source, message, ex, context);

    /// <summary>
    /// 记录工具执行错误。自动提取工具名和参数信息。
    /// </summary>
    public static void ToolError(string toolName, string message, Exception? ex = null,
        Dictionary<string, object?>? args = null)
    {
        var context = new Dictionary<string, object?>();
        if (args != null)
        {
            foreach (var (k, v) in args)
            {
                if (k == "content" || k == "old_string" || k == "new_string")
                    context[k] = ContextManager.TruncateByRunes(v?.ToString() ?? "", 100) + "...";
                else
                    context[k] = v?.ToString();
            }
        }
        Error($"Tool:{toolName}", message, ex, context);
    }

    /// <summary>
    /// 记录 LLM API 错误。自动提取模型名和 endpoint。
    /// </summary>
    public static void LlmError(string model, string endpoint, string message, Exception? ex = null)
    {
        Error("LLM",
            $"[{model}] {message}",
            ex,
            new Dictionary<string, object?> { ["endpoint"] = endpoint, ["model"] = model });
    }

    /// <summary>
    /// 强制刷盘所有缓冲日志到文件。
    /// </summary>
    public static void Flush()
    {
        List<string> pending;
        lock (_lock)
        {
            if (!_dirty || _buffer.Count == 0) return;
            pending = [.. _buffer];
            _buffer.Clear();
            _dirty = false;
        }

        AppendToFile(pending);
    }

    /// <summary>把日志行追加到当前日志文件（含日期轮转），锁外执行。</summary>
    private static void AppendToFile(List<string> lines)
    {
        if (_logDir == null || lines.Count == 0) return;

        // 检测日期变更 → 轮转
        var today = DateTime.Now.ToString("yyyyMMdd");
        if (today != _currentDate || _currentLogFile == null)
        {
            _currentDate = today;
            _currentLogFile = Path.Combine(_logDir, $"error_{today}.log");
        }

        try
        {
            File.AppendAllLines(_currentLogFile, lines, System.Text.Encoding.UTF8);
        }
        catch
        {
            // 日志写入失败不能抛异常（否则可能造成无限递归）
        }
    }

    // ================================================================
    // 内部
    // ================================================================

    private static void Write(Level level, string source, string message,
        Exception? ex, object? context)
    {
        var entry = FormatEntry(level, source, message, ex, context);

        // 同步写入 DebugLog 会话文件（如果调试模式启用）
        if (DebugLog.Enabled && level >= Level.Warning)
        {
            DebugLog.Log("error", $"[{level}] [{source}] {message}\n{ex}");
        }

        List<string>? pending = null;
        lock (_lock)
        {
            _buffer.Add(entry);
            _dirty = true;

            if (_buffer.Count >= BufferFlushSize)
            {
                pending = [.. _buffer];
                _buffer.Clear();
                _dirty = false; // 待刷盘数据已取出，buffer 清空
            }
        }

        // 锁外刷盘：文件 IO 不进锁，避免阻塞其他线程写入
        if (pending != null)
            AppendToFile(pending);
    }

    private static string FormatEntry(Level level, string source, string message,
        Exception? ex, object? context)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level switch
        {
            Level.Info => "INFO",
            Level.Warning => "WARN",
            Level.Error => "ERROR",
            Level.Fatal => "FATAL",
            _ => "????"
        };

        var sb = new System.Text.StringBuilder();
        sb.Append($"[{ts}] [{levelStr,5}] [{source}] {message}");

        if (context is Dictionary<string, object?> dict)
        {
            sb.Append(" | ");
            var first = true;
            foreach (var (k, v) in dict)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{k}={v}");
                first = false;
            }
        }

        if (ex != null)
        {
            sb.AppendLine();
            sb.Append(new string(' ', 34)); // 对齐时间戳
            sb.Append($"Exception: {ex.GetType().Name}: {ex.Message}");
            if (ex.StackTrace != null)
            {
                sb.AppendLine();
                sb.Append(new string(' ', 34));
                // 截取前 500 码点的堆栈（码点安全，避免代理对切半）
                var stack = ContextManager.TruncateByRunes(ex.StackTrace, 500);
                if (ex.StackTrace.Length > stack.Length) stack += "...";
                sb.Append($"Stack: {stack.Replace("\n", " → ")}");
            }

            // 递归记录内部异常
            var inner = ex.InnerException;
            var depth = 0;
            while (inner != null && depth < 3)
            {
                sb.AppendLine();
                sb.Append(new string(' ', 34));
                sb.Append($"Inner[{depth}]: {inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
                depth++;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 首次机会异常处理器 — 捕获所有 throw 的异常。
    /// 仅记录，不阻止正常的 catch 处理。
    /// </summary>
    private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        // 过滤噪音：忽略常见非致命异常
        var ex = e.Exception;
        if (FCEFilter(ex)) return;

        // 使用 Error 级别（这些异常可能被上层 catch 处理，
        // 但我们仍然记录它们以便发现隐藏问题）
        Write(Level.Info, "FirstChance",
            $"{ex.GetType().Name}: {ex.Message}",
            null,
            new Dictionary<string, object?>
            {
                ["source"] = ex.Source ?? "?",
                ["hresult"] = $"0x{ex.HResult:X8}"
            });
    }

    /// <summary>
    /// 过滤不应记录的首次机会异常。
    /// </summary>
    private static bool FCEFilter(Exception ex)
    {
        return ex is OperationCanceledException
            || ex is TaskCanceledException
            || ex is TimeoutException
            || ex is IOException
            || ex is UnauthorizedAccessException
            || ex is System.Net.Sockets.SocketException
            || ex.GetType().Name.Contains("HttpRequestException")
            || ex.GetType().Name.Contains("Win32Exception")
            || ex.StackTrace?.Contains("System.Console") == true
            || ex.StackTrace?.Contains("System.IO.FileSystemWatcher") == true
            || ex.StackTrace?.Contains("FileSystemEnumerator") == true
            || ex.Message?.Contains("断开的管道") == true
            || ex.Message?.Contains("broken pipe") == true
            || ex.Message?.Contains("Access to the path") == true
            || ex.Message?.Contains("Access is denied") == true;
    }
}
