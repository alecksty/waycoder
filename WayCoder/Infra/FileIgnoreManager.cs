using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 文件忽略规则管理器 —— 加载 .gitignore 和 .waycoderignore 规则，
/// 为 glob/grep/read_file 等工具提供 IsIgnored() 过滤能力。
///
/// 设计：
/// - 从 cwd 向上遍历每个目录，加载该目录的 .gitignore 和 .waycoderignore
/// - 规则按目录层级缓存，匹配时从低到高检查
/// - 始终忽略常见垃圾目录（.git, node_modules 等）
/// - 规则语法遵循 .gitignore 标准
/// </summary>
public static class FileIgnoreManager
{
    /// <summary>始终忽略的目录名</summary>
    private static readonly HashSet<string> AlwaysIgnoreDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg",
        "node_modules", "bower_components",
        "__pycache__", ".pytest_cache", ".mypy_cache",
        ".venv", "venv", ".tox", ".env",
        "dist", "build", "target", "out",
        ".idea", ".vscode", ".vs",
        ".next", ".nuxt", ".cache",
        "coverage", ".nyc_output",
        "vendor", "packages",
    };

    /// <summary>始终忽略的文件扩展名</summary>
    private static readonly HashSet<string> AlwaysIgnoreExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pyc", ".pyo", ".pyd",
        ".class", ".o", ".obj", ".so", ".dll", ".exe", ".dylib",
        ".zip", ".tar", ".gz", ".bz2", ".7z", ".rar",
        ".jpg", ".jpeg", ".png", ".gif", ".ico", ".svg", ".webp", ".bmp",
        ".mp3", ".mp4", ".avi", ".mov", ".wmv", ".flv",
        ".doc", ".xls", ".ppt",
        ".lock", ".sum",
    };

    /// <summary>始终忽略的通配模式</summary>
    private static readonly string[] AlwaysIgnorePatterns =
    [
        ".git/", ".svn/", "node_modules/",
    ];

    /// <summary>按目录缓存已解析的忽略规则（目录路径 → 规则列表）</summary>
    private static readonly Dictionary<string, List<IgnoreRule>> RuleCache = new();
    private static readonly object Lock = new();

    /// <summary>
    /// 检查路径是否被忽略。path 可以是相对或绝对路径。
    /// </summary>
    public static bool IsIgnored(string path, string? baseDir = null)
    {
        baseDir ??= Environment.CurrentDirectory;

        // 标准化路径
        var absPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDir, path));
        var normalized = absPath.Replace('\\', '/');

        // 检查文件名是否含始终忽略的目录
        var parts = normalized.Split('/');
        foreach (var part in parts)
        {
            if (AlwaysIgnoreDirs.Contains(part))
                return true;
        }

        // 检查文件扩展名
        var ext = Path.GetExtension(normalized);
        if (!string.IsNullOrEmpty(ext) && AlwaysIgnoreExtensions.Contains(ext))
            return true;

        // 加载并匹配目录树中的所有忽略规则
        var rules = LoadRulesForPath(absPath, baseDir);
        bool ignored = false;

        foreach (var rule in rules)
        {
            if (rule.Match(normalized))
                ignored = !rule.Negation; // 否定规则可以反转
        }

        return ignored;
    }

    /// <summary>
    /// 过滤文件列表，移除被忽略的文件。
    /// </summary>
    public static List<string> FilterIgnored(IEnumerable<string> paths, string? baseDir = null)
    {
        return paths.Where(p => !IsIgnored(p, baseDir)).ToList();
    }

    /// <summary>
    /// 检查目录是否应被跳过（整目录忽略）。
    /// </summary>
    public static bool ShouldSkipDirectory(string dirPath, string? baseDir = null)
    {
        baseDir ??= Environment.CurrentDirectory;

        var dirName = Path.GetFileName(dirPath.TrimEnd('/', '\\'));
        if (AlwaysIgnoreDirs.Contains(dirName))
            return true;

        // 检查隐藏目录（以 . 开头，除了 . 本身）
        if (dirName.StartsWith('.') && dirName.Length > 1)
            return true;

        return IsIgnored(dirPath, baseDir);
    }

    /// <summary>
    /// 清除规则缓存（用于文件系统变更后刷新）。
    /// </summary>
    public static void ClearCache()
    {
        lock (Lock)
        {
            RuleCache.Clear();
        }
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    /// <summary>
    /// 加载从 baseDir 到文件所在位置的所有 .gitignore 和 .waycoderignore 规则。
    /// </summary>
    private static List<IgnoreRule> LoadRulesForPath(string absFilePath, string baseDir)
    {
        var allRules = new List<IgnoreRule>();
        baseDir = Path.GetFullPath(baseDir).Replace('\\', '/');

        // 从文件所在目录向上直到 baseDir
        var targetDir = Path.GetDirectoryName(absFilePath);
        if (targetDir == null) return allRules;

        targetDir = targetDir.Replace('\\', '/');

        var currentDir = targetDir;
        var dirs = new List<string>();

        while (!string.IsNullOrEmpty(currentDir) && currentDir.Length >= baseDir.Length)
        {
            dirs.Add(currentDir);
            if (currentDir == baseDir) break;
            var parent = Path.GetDirectoryName(currentDir)?.Replace('\\', '/');
            if (parent == currentDir || string.IsNullOrEmpty(parent)) break;
            currentDir = parent;
        }

        // 从顶层到底层加载规则（子目录规则可覆盖父目录）
        dirs.Reverse();
        foreach (var dir in dirs)
        {
            allRules.AddRange(GetCachedRules(dir));
        }

        return allRules;
    }

    /// <summary>
    /// 获取指定目录的缓存忽略规则。
    /// </summary>
    private static List<IgnoreRule> GetCachedRules(string dirPath)
    {
        lock (Lock)
        {
            if (RuleCache.TryGetValue(dirPath, out var cached))
                return cached;
        }

        var rules = ParseIgnoreFiles(dirPath);

        lock (Lock)
        {
            RuleCache[dirPath] = rules;
        }

        return rules;
    }

    /// <summary>
    /// 解析目录中的 .gitignore 和 .waycoderignore 文件。
    /// </summary>
    private static List<IgnoreRule> ParseIgnoreFiles(string dirPath)
    {
        var rules = new List<IgnoreRule>();

        foreach (var fileName in new[] { ".gitignore", ".waycoderignore" })
        {
            var filePath = Path.Combine(dirPath, fileName).Replace('\\', '/');
            if (!File.Exists(filePath)) continue;

            try
            {
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    // 跳过空行和注释
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                        continue;

                    var negation = false;
                    var pattern = trimmed;

                    if (pattern.StartsWith('!'))
                    {
                        negation = true;
                        pattern = pattern[1..];
                    }

                    // 跳过空否定
                    if (string.IsNullOrEmpty(pattern))
                        continue;

                    // 移除尾部空格
                    pattern = pattern.TrimEnd();

                    // 目录前缀：以 / 开头表示相对于 .gitignore 所在目录
                    var anchored = pattern.StartsWith('/');
                    if (anchored)
                        pattern = pattern[1..];

                    rules.Add(new IgnoreRule
                    {
                        Pattern = pattern,
                        Negation = negation,
                        SourceDir = dirPath,
                        Anchored = anchored,
                    });
                }
            }
            catch
            {
                // 忽略读取错误
            }
        }

        return rules;
    }

    // ========================================================================
    // 内部类型
    // ========================================================================

    private class IgnoreRule
    {
        public string Pattern { get; init; } = "";
        public bool Negation { get; init; }
        public string SourceDir { get; init; } = "";
        public bool Anchored { get; init; }

        private Regex? _regex;

        /// <summary>
        /// 测试一个标准化的绝对路径是否匹配此规则。
        /// </summary>
        public bool Match(string normalizedAbsPath)
        {
            // 构建正则表达式（懒加载）
            _regex ??= BuildRegex();

            // 对于锚定规则，匹配相对于 SourceDir 的路径
            if (Anchored)
            {
                var sourceDir = SourceDir.Replace('\\', '/');
                if (!normalizedAbsPath.StartsWith(sourceDir))
                    return false;
                var relative = normalizedAbsPath[sourceDir.Length..].TrimStart('/');
                return _regex.IsMatch(relative);
            }

            // 对于非锚定规则，匹配任意位置
            return _regex.IsMatch(normalizedAbsPath);
        }

        private Regex BuildRegex()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('^');

            // 以 / 结尾 = 目录规则（匹配目录本身及其中所有内容）。先去掉尾随斜杠，
            // 否则 GlobSegmentToRegex 会把 / 作为字面量输出，与结尾的 "/" 叠加成 "//"
            // 导致规则永不命中（原 bug）。
            var endsWithSlash = Pattern.EndsWith('/');
            var p = endsWithSlash ? Pattern[..^1] : Pattern;

            // ReDoS 防护：单条规则星号过多（如 *a*a*a*.. 30 个）→ [^/]* 组合指数回溯
            // 会卡死扫描；用永不匹配的正则使该规则失效
            if (p.Count(c => c == '*') > 12)
                return new Regex("$^", RegexOptions.Compiled);

            // 非锚定且不含目录分隔符的规则可以在任意目录深度
            if (!Anchored && !p.Contains('/'))
            {
                sb.Append(@"(.*/)?");
            }

            // ** 匹配零或多个目录（globstar）
            sb.Append(GlobToRegex(p));

            // 目录规则：匹配目录本身及其中所有内容（logs/ → logs 及 logs/*）
            if (endsWithSlash)
                sb.Append(@"(/.*)?");

            sb.Append('$');

            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// 把含 **（globstar）的 glob 模式转为正则片段。
        /// ** 独立成段时匹配零或多个目录级：**/foo、a/**/b、a/**、孤立 **。
        /// 非独立 **（如 foo**bar）退化为 *。
        /// </summary>
        private static string GlobToRegex(string pattern)
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < pattern.Length)
            {
                char ch = pattern[i];

                if (ch == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    int j = i;
                    while (j < pattern.Length && pattern[j] == '*') j++;
                    bool prevSlash = i > 0 && pattern[i - 1] == '/';
                    bool nextSlash = j < pattern.Length && pattern[j] == '/';

                    // ** 独立成段：前后为 / 或边界（globstar）
                    if ((i == 0 || prevSlash) && (j == pattern.Length || nextSlash))
                    {
                        if (nextSlash)
                        {
                            // 前导或中间 **/：匹配零或多个目录（含目录后斜杠），
                            // 并吞掉紧随其后的 /（否则与字面 / 叠加成 //，永不命中零目录）
                            sb.Append(@"(?:[^/]*/)*");
                            i = j + 1;
                        }
                        else
                        {
                            // 尾随 /** 或孤立 **：匹配其后一切（含子路径）
                            sb.Append(@".*");
                            i = j;
                        }
                    }
                    else
                    {
                        // 非独立 **（如 foo**bar）等价于 *
                        sb.Append(@"[^/]*");
                        i = j;
                    }
                    continue;
                }

                AppendGlobChar(sb, ch);
                i++;
            }
            return sb.ToString();
        }

        private static void AppendGlobChar(System.Text.StringBuilder sb, char ch)
        {
            switch (ch)
            {
                case '*': sb.Append(@"[^/]*"); break;
                case '?': sb.Append(@"[^/]"); break;
                case '.': sb.Append(@"\."); break;
                case '+': case '(': case ')': case '^': case '$':
                case '{': case '}': case '|': case '\\':
                case '[': case ']': // gitignore 字符类/不成对方括号：按字面量转义，避免非法正则抛 ArgumentException
                    sb.Append('\\').Append(ch); break;
                default: sb.Append(ch); break;
            }
        }
    }
}
