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
    private bool _disposed;

    /// <summary>忽略的目录模式</summary>
    private static readonly HashSet<string> IgnoreDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".corecoder", ".waycoder", "logs",
        ".idea", ".vs", "vendor", "__pycache__", ".pytest_cache",
        "dist", "build", "target", ".next", ".nuget",
    };

    /// <summary>关注的文件扩展名</summary>
    private static readonly HashSet<string> WatchExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".py", ".ts", ".js", ".tsx", ".jsx", ".go", ".rs", ".java",
        ".kt", ".swift", ".c", ".cpp", ".h", ".hpp", ".rb", ".php",
        ".vue", ".svelte", ".html", ".css", ".scss", ".less",
        ".md", ".txt", ".json", ".yaml", ".yml", ".toml", ".xml",
        ".sql", ".sh", ".bash", ".ps1", ".proto", ".graphql",
        ".r", ".m", ".scala", ".clj", ".ex", ".exs", ".dart",
        ".lua", ".zig", ".nim", ".fs", ".fsx", ".csx",
    };

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
        _debounceTimer?.Dispose();
        _debounceTimer = null;
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
        if (string.IsNullOrEmpty(ext) || !WatchExtensions.Contains(ext))
            return false;

        // 目录过滤
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
        {
            foreach (var part in dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (IgnoreDirs.Contains(part)) return false;
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
                    DebugLog.Log("watch", $"AI 注释触发: {filePath} -> {prompt[..Math.Min(prompt.Length, 80)]}");
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

        var lines = content.Split('\n');
        bool inBlockComment = false;
        string? blockCommentEnd = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // 处理块注释
            if (inBlockComment)
            {
                var endIdx = blockCommentEnd != null ? line.IndexOf(blockCommentEnd, StringComparison.Ordinal) : -1;
                if (endIdx >= 0)
                {
                    var blockText = line[..endIdx].Trim();
                    ExtractAiPrefix(blockText, results);
                    inBlockComment = false;
                    blockCommentEnd = null;
                }
                else
                {
                    ExtractAiPrefix(line, results);
                }
                continue;
            }

            // 检测块注释开始: /* AI! ... */ 或 <!-- AI! ... -->
            var blockStart = line.IndexOf("/*", StringComparison.Ordinal);
            if (blockStart >= 0)
            {
                var afterStart = line[(blockStart + 2)..];
                var blockEnd = afterStart.IndexOf("*/", StringComparison.Ordinal);
                if (blockEnd >= 0)
                {
                    // 单行块注释
                    ExtractAiPrefix(afterStart[..blockEnd].Trim(), results);
                }
                else
                {
                    // 多行块注释开始
                    inBlockComment = true;
                    blockCommentEnd = "*/";
                    ExtractAiPrefix(afterStart.Trim(), results);
                }
                continue;
            }

            var htmlStart = line.IndexOf("<!--", StringComparison.Ordinal);
            if (htmlStart >= 0)
            {
                var afterStart = line[(htmlStart + 4)..];
                var htmlEnd = afterStart.IndexOf("-->", StringComparison.Ordinal);
                if (htmlEnd >= 0)
                {
                    ExtractAiPrefix(afterStart[..htmlEnd].Trim(), results);
                }
                else
                {
                    inBlockComment = true;
                    blockCommentEnd = "-->";
                    ExtractAiPrefix(afterStart.Trim(), results);
                }
                continue;
            }

            // 行注释检测
            foreach (var prefix in lineComments)
            {
                var idx = line.IndexOf(prefix, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var comment = line[(idx + prefix.Length)..].Trim();
                    ExtractAiPrefix(comment, results);
                    break;
                }
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
            var prompt = trimmed[2..].Trim();
            if (prompt.Length > 0) results.Add($"请回答关于 {prompt} 的问题");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _watcher.Dispose();
        _debounceTimer?.Dispose();
    }
}
