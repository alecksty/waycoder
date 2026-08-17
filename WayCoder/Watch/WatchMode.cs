namespace WayCoder;

/// <summary>
/// Watch 模式 — 监听外部编辑器文件变更，自动处理 AI! / AI? 注释。
/// 兼容 Aider 的 AI 注释语法。
/// </summary>
public class WatchMode : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Action<string> _onAiPrompt;
    private readonly HashSet<string> _pendingFiles = [];
    private readonly object _lock = new();
    private Timer? _debounceTimer;
    private volatile bool _disposed; // volatile：Dispose 在调用线程写、FileSystemWatcher 回调线程读

    /// <summary>默认忽略的目录模式（可被 Config.WatchIgnoreDirs 追加）</summary>
    private static readonly HashSet<string> DefaultIgnoreDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".corecoder", ".waycoder", "logs",
        ".idea", ".vs", "vendor", "__pycache__", ".pytest_cache",
        "dist", "build", "target", ".next", ".nuget",
    };

    /// <summary>默认关注的文件扩展名（可被 Config.WatchExtensions 追加）</summary>
    private static readonly HashSet<string> DefaultWatchExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".py", ".ts", ".js", ".tsx", ".jsx", ".go", ".rs", ".java",
        ".kt", ".swift", ".c", ".cpp", ".h", ".hpp", ".rb", ".php",
        ".vue", ".svelte", ".html", ".css", ".scss", ".less",
        ".md", ".txt", ".json", ".yaml", ".yml", ".toml", ".xml",
        ".sql", ".sh", ".bash", ".ps1", ".proto", ".graphql",
        ".r", ".m", ".scala", ".clj", ".ex", ".exs", ".dart",
        ".lua", ".zig", ".nim", ".fs", ".fsx", ".csx",
    };

    /// <summary>运行时合并后的忽略目录（默认 + 用户配置）</summary>
    private static HashSet<string> GetIgnoreDirs()
    {
        var dirs = new HashSet<string>(DefaultIgnoreDirs, StringComparer.OrdinalIgnoreCase);
        var cfg = Config.Instance;
        if (!string.IsNullOrWhiteSpace(cfg.WatchIgnoreDirs))
        {
            foreach (var d in cfg.WatchIgnoreDirs.Split(',', StringSplitOptions.RemoveEmptyEntries))
                dirs.Add(d.Trim());
        }
        return dirs;
    }

    /// <summary>运行时合并后的扩展名（默认 + 用户配置）</summary>
    private static HashSet<string> GetWatchExtensions()
    {
        var exts = new HashSet<string>(DefaultWatchExtensions, StringComparer.OrdinalIgnoreCase);
        var cfg = Config.Instance;
        if (!string.IsNullOrWhiteSpace(cfg.WatchExtensions))
        {
            foreach (var e in cfg.WatchExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var ext = e.Trim();
                if (!ext.StartsWith('.')) ext = "." + ext;
                exts.Add(ext);
            }
        }
        return exts;
    }

    public WatchMode(string directory, Action<string> onAiPrompt)
    {
        _onAiPrompt = onAiPrompt;
        _watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = false,
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;

        // 过滤无关目录
        _watcher.Filter = "*.*";
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
        DebugLog.Log("watch", "Watch 模式已启动");
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
        // 与 OnFileChanged 的锁内访问互斥，避免 Stop 后回调又重建 Timer（泄漏）
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;
        if (!ShouldWatch(e.FullPath)) return;

        lock (_lock)
        {
            _pendingFiles.Add(e.FullPath);
            // 防抖：500ms 内同一文件的后续修改合并为一次处理
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => ProcessPendingFiles(), null, 500, Timeout.Infinite);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_disposed) return;
        if (ShouldWatch(e.FullPath))
            OnFileChanged(sender, (FileSystemEventArgs)e);
    }

    private bool ShouldWatch(string path)
    {
        // 扩展名过滤
        var ext = Path.GetExtension(path);
        var watchExts = GetWatchExtensions();
        if (string.IsNullOrEmpty(ext) || !watchExts.Contains(ext))
            return false;

        // 目录过滤：仅检查监视根之下的目录段，避免祖先目录名（如 /Users/x/target/…）误判
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
        {
            var ignoreDirs = GetIgnoreDirs();
            var relative = Path.GetRelativePath(Path.GetFullPath(_watcher.Path), dir);
            foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (ignoreDirs.Contains(part)) return false;
            }
        }

        return true;
    }

    private void ProcessPendingFiles()
    {
        string[] files;
        lock (_lock)
        {
            files = _pendingFiles.ToArray();
            _pendingFiles.Clear();
        }

        foreach (var filePath in files)
        {
            try
            {
                if (!File.Exists(filePath)) continue;
                var content = File.ReadAllText(filePath);
                var prompts = ExtractAiComments(content, filePath);
                foreach (var prompt in prompts)
                {
                    DebugLog.Log("watch", $"AI 注释触发: {filePath} -> {ContextManager.TruncateByRunes(prompt, 80)}");
                    _onAiPrompt(prompt);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Log("watch", $"处理文件变更异常: {filePath} - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 从文件内容中提取 AI! / AI? 注释。
    /// 支持格式：
    ///   // AI! 指令文字
    ///   # AI! 指令文字
    ///   -- AI! 指令文字
    ///   /* AI? 问题文字 */
    ///   <!-- AI! 指令文字 -->
    /// </summary>
    public static List<string> ExtractAiComments(string content, string filePath)
    {
        var results = new List<string>();
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        // 确定注释语法
        var lineComments = ext switch
        {
            ".cs" or ".java" or ".kt" or ".swift" or ".go" or ".rs" or ".c" or ".cpp" or ".h" or ".hpp"
                or ".js" or ".ts" or ".jsx" or ".tsx" or ".scala" or ".dart" or ".zig"
                => new[] { "//" },
            ".py" or ".rb" or ".sh" or ".bash" or ".pl" or ".r" or ".yaml" or ".yml" or ".toml"
                => new[] { "#" },
            ".sql" or ".lua" or ".nim"
                => new[] { "--" },
            ".html" or ".xml" or ".vue" or ".svelte" or ".md" or ".svg"
                => new[] { "<!--", "//" },
            ".css" or ".scss" or ".less"
                => new[] { "/*", "//" },
            ".fs" or ".fsx" or ".csx"
                => new[] { "//" },
            _ => new[] { "//", "#", "--" },
        };

        // 是否支持 C 风格块注释 /* */ 与 HTML 块注释 <!-- -->（仅适用语言才检测，
        // 否则 .py 等文件里含 "/*" 的字符串/URL 会被误判为块注释，吞掉后续行）
        var supportsCBlock = ext is ".cs" or ".java" or ".kt" or ".swift" or ".go" or ".rs"
            or ".c" or ".cpp" or ".h" or ".hpp" or ".js" or ".ts" or ".jsx" or ".tsx"
            or ".scala" or ".dart" or ".zig" or ".css" or ".scss" or ".less"
            or ".fs" or ".fsx" or ".csx";
        var supportsHtmlBlock = ext is ".html" or ".xml" or ".vue" or ".svelte" or ".md" or ".svg";

        var lines = content.Split('\n');
        bool inBlockComment = false;
        string? blockCommentEnd = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // 用 while 循环处理同一行上的多个注释段：块注释结束后可能紧跟行注释
            // （如 `/* ... */ // AI! 指令`），此前块注释后直接 continue 会吞掉行注释。
            while (line.Length > 0)
            {
                // ── 已在块注释内 ──
                if (inBlockComment)
                {
                    int endIdx = blockCommentEnd != null ? line.IndexOf(blockCommentEnd, StringComparison.Ordinal) : -1;
                    if (endIdx < 0)
                    {
                        ExtractAiPrefix(line, results);
                        break; // 本行剩余全在块注释内
                    }
                    ExtractAiPrefix(line[..endIdx].Trim(), results);
                    inBlockComment = false;
                    line = line[(endIdx + blockCommentEnd!.Length)..].Trim();
                    blockCommentEnd = null;
                    continue; // 解析块注释后的剩余（可能含行注释）
                }

                // ── 检测 C 风格块注释 /* ... */（仅适用语言，否则含 "/*" 的字符串会被误判）──
                if (supportsCBlock)
                {
                    int bs = line.IndexOf("/*", StringComparison.Ordinal);
                    if (bs >= 0)
                    {
                        var afterStart = line[(bs + 2)..];
                        int be = afterStart.IndexOf("*/", StringComparison.Ordinal);
                        if (be >= 0)
                        {
                            // 单行块注释：提取后继续解析块后的剩余
                            ExtractAiPrefix(afterStart[..be].Trim(), results);
                            line = afterStart[(be + 2)..].Trim();
                            continue;
                        }
                        // 多行块注释开始
                        inBlockComment = true;
                        blockCommentEnd = "*/";
                        ExtractAiPrefix(afterStart.Trim(), results);
                        break;
                    }
                }

                // ── 检测 HTML 块注释 <!-- ... -->（仅适用语言）──
                if (supportsHtmlBlock)
                {
                    int bs = line.IndexOf("<!--", StringComparison.Ordinal);
                    if (bs >= 0)
                    {
                        var afterStart = line[(bs + 4)..];
                        int be = afterStart.IndexOf("-->", StringComparison.Ordinal);
                        if (be >= 0)
                        {
                            ExtractAiPrefix(afterStart[..be].Trim(), results);
                            line = afterStart[(be + 3)..].Trim();
                            continue;
                        }
                        inBlockComment = true;
                        blockCommentEnd = "-->";
                        ExtractAiPrefix(afterStart.Trim(), results);
                        break;
                    }
                }

                // ── 行注释检测 ──
                foreach (var prefix in lineComments)
                {
                    int idx = line.IndexOf(prefix, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        ExtractAiPrefix(line[(idx + prefix.Length)..].Trim(), results);
                        break;
                    }
                }
                break; // 行注释到行尾（或无注释），本行结束
            }
        }

        return results;
    }

    private static void ExtractAiPrefix(string comment, List<string> results)
    {
        var trimmed = comment.Trim();
        if (trimmed.StartsWith("AI!") || trimmed.StartsWith("ai!"))
        {
            var prompt = trimmed[3..].Trim();
            if (prompt.Length > 0) results.Add(prompt);
        }
        else if (trimmed.StartsWith("AI?") || trimmed.StartsWith("ai?"))
        {
            var prompt = trimmed[3..].Trim();
            if (prompt.Length > 0) results.Add($"请回答关于 {prompt} 的问题");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _watcher.Dispose();
    }
}
