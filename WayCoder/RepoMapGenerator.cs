using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 仓库地图生成器 — 扫描项目结构，提取关键符号，
/// 生成简洁的 ASCII 树状图供 LLM 理解代码库布局。
///
/// 灵感源自 aider 的 repomap，但完全从零实现。
/// 特点：
///   - 尊重 .gitignore 规则
///   - 按语言提取函数/类/方法等关键符号
///   - 按修改时间排序（最近修改的文件优先展示细节）
///   - 自动缓存，文件变更时自动刷新
/// </summary>
public static class RepoMapGenerator
{
    /// <summary>缓存的地图内容</summary>
    private static string? _cachedMap;
    private static DateTime _cacheTime;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    /// <summary>Gitignore 规则（glob 模式）</summary>
    private static List<string> _ignorePatterns = [];

    /// <summary>已扫描过符号的文件哈希，避免重复扫描</summary>
    private static readonly Dictionary<string, string> _symbolCache = new();

    // ---- 各语言的符号提取正则 ----

    private static readonly Dictionary<string, (Regex regex, string kind)> SymbolPatterns = new()
    {
        // C#
        [".cs"] = (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*(?:class|interface|struct|enum|record)\s+(\w+)", RegexOptions.Multiline), "type"),
        // Python
        [".py"] = (new Regex(@"^\s*(?:async\s+)?def\s+(\w+)|^\s*class\s+(\w+)", RegexOptions.Multiline), "def/class"),
        // JavaScript / TypeScript
        [".js"] = (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)|^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=|^\s*(?:export\s+)?class\s+(\w+)", RegexOptions.Multiline), "fn/class"),
        [".ts"] = (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)|^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*(?::\s*\w+)?\s*=|^\s*(?:export\s+)?(?:abstract\s+)?class\s+(\w+)|^\s*(?:export\s+)?interface\s+(\w+)", RegexOptions.Multiline), "fn/class/iface"),
        [".tsx"] = (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)|^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=|^\s*(?:export\s+)?(?:abstract\s+)?class\s+(\w+)|^\s*(?:export\s+)?interface\s+(\w+)", RegexOptions.Multiline), "fn/class/iface"),
        [".jsx"] = (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)|^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=|^\s*(?:export\s+)?class\s+(\w+)", RegexOptions.Multiline), "fn/class"),
        // Go
        [".go"] = (new Regex(@"^\s*func\s+(?:\([^)]*\)\s+)?(\w+)|^\s*type\s+(\w+)\s+struct|^\s*type\s+(\w+)\s+interface", RegexOptions.Multiline), "func/type"),
        // Rust
        [".rs"] = (new Regex(@"^\s*(?:pub\s+)?(?:async\s+)?fn\s+(\w+)|^\s*(?:pub\s+)?(?:struct|enum|trait|impl)\s+(\w+)", RegexOptions.Multiline), "fn/type"),
        // Java
        [".java"] = (new Regex(@"^\s*(?:public|private|protected)?\s*(?:static|final|abstract)?\s*(?:class|interface|enum)\s+(\w+)", RegexOptions.Multiline), "type"),
        // Ruby
        [".rb"] = (new Regex(@"^\s*def\s+(\w+)|^\s*class\s+(\w+)|^\s*module\s+(\w+)", RegexOptions.Multiline), "def/class"),
        // PHP
        [".php"] = (new Regex(@"^\s*(?:public\s+)?function\s+(\w+)|^\s*class\s+(\w+)", RegexOptions.Multiline), "fn/class"),
        // Swift
        [".swift"] = (new Regex(@"^\s*(?:public\s+)?func\s+(\w+)|^\s*(?:public\s+)?class\s+(\w+)|^\s*(?:public\s+)?struct\s+(\w+)", RegexOptions.Multiline), "func/type"),
        // Kotlin
        [".kt"] = (new Regex(@"^\s*(?:suspend\s+)?fun\s+(\w+)|^\s*(?:data\s+)?class\s+(\w+)|^\s*object\s+(\w+)", RegexOptions.Multiline), "fun/class"),
        // C / C++ header
        [".h"] = (new Regex(@"^\s*\w[\w\s*]+\s+(\w+)\s*\(|^\s*(?:class|struct)\s+(\w+)", RegexOptions.Multiline), "fn/type"),
        [".c"] = (new Regex(@"^\s*\w[\w\s*]+\s+(\w+)\s*\(", RegexOptions.Multiline), "fn"),
        [".cpp"] = (new Regex(@"^\s*\w[\w\s*:]+\s+(\w+)\s*\(|^\s*(?:class|struct)\s+(\w+)", RegexOptions.Multiline), "fn/type"),
        // Shell
        [".sh"] = (new Regex(@"^(\w+)\s*\(\s*\)|^\s*function\s+(\w+)", RegexOptions.Multiline), "fn"),
        // Lua
        [".lua"] = (new Regex(@"^\s*(?:local\s+)?function\s+(\w+)", RegexOptions.Multiline), "fn"),
        // Dart
        [".dart"] = (new Regex(@"^\s*(?:void|int|String|bool|dynamic|Future\w*|Widget|State)\s+(\w+)\s*\(|^\s*class\s+(\w+)", RegexOptions.Multiline), "fn/class"),
        // R
        [".r"] = (new Regex(@"^\s*(\w+)\s*<-\s*function", RegexOptions.Multiline), "fn"),
        [".R"] = (new Regex(@"^\s*(\w+)\s*<-\s*function", RegexOptions.Multiline), "fn"),
        // SQL
        [".sql"] = (new Regex(@"^\s*CREATE\s+(?:TABLE|INDEX|VIEW|PROCEDURE|FUNCTION)\s+(\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase), "ddl"),
        // Markdown
        [".md"] = (new Regex(@"^#+\s+(.+)", RegexOptions.Multiline), "heading"),
    };

    // ---- 公共 API ----

    /// <summary>
    /// 生成仓库地图（使用缓存）。
    /// </summary>
    public static string Generate(string? root = null, bool forceRefresh = false)
    {
        root ??= GetRepoRoot();

        if (!forceRefresh && _cachedMap != null && (DateTime.UtcNow - _cacheTime) < CacheTtl)
            return _cachedMap;

        _ignorePatterns = ParseGitignore(root);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## 仓库地图");
        sb.AppendLine();

        // 收集文件树 + 符号
        var entries = CollectEntries(root, root);
        var tree = BuildTree(entries, root);

        sb.Append(tree);
        sb.AppendLine();

        _cachedMap = sb.ToString();
        _cacheTime = DateTime.UtcNow;
        return _cachedMap;
    }

    /// <summary>
    /// 使缓存失效（通常在文件修改后调用）。
    /// </summary>
    public static void Invalidate() => _cachedMap = null;

    // ---- 内部实现 ----

    private static string GetRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static List<string> ParseGitignore(string root)
    {
        var patterns = new List<string> { ".git/", "node_modules/", "__pycache__/", ".venv/", "venv/",
            "bin/", "obj/", "dist/", "build/", ".vs/", ".idea/", "*.pyc", "*.pyo",
            ".DS_Store", "Thumbs.db", "*.user", "*.suo", "*.cache", "*.log",
            ".pytest_cache/", ".ruff_cache/", ".mypy_cache/", "coverage/",
            ".next/", ".nuxt/", "target/", "vendor/" };

        var gitignorePath = Path.Combine(root, ".gitignore");
        if (!File.Exists(gitignorePath)) return patterns;

        try
        {
            foreach (var line in File.ReadAllLines(gitignorePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                // 去前导斜杠和结尾斜杠
                if (trimmed.StartsWith('/')) trimmed = trimmed[1..];
                if (trimmed.EndsWith('/')) trimmed = trimmed[..^1];
                if (!string.IsNullOrEmpty(trimmed))
                    patterns.Add(trimmed);
            }
        }
        catch (Exception ex) { DebugLog.Log("RepoMap", $"解析 .gitignore 失败: {ex.Message}"); }

        return patterns;
    }

    private static bool IsIgnored(string relativePath)
    {
        foreach (var pattern in _ignorePatterns)
        {
            // 简单 glob 匹配
            if (pattern.Contains('*'))
            {
                var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(relativePath, regex)) return true;
                // 也匹配路径中的任何一层
                var nameOnly = Path.GetFileName(relativePath);
                if (Regex.IsMatch(nameOnly, regex)) return true;
            }
            else
            {
                if (relativePath.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
                // 匹配目录下的所有文件
                if (relativePath.Contains("/" + pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private record FileEntry(
        string RelativePath,
        string Name,
        bool IsDir,
        long Size,
        DateTime LastModified,
        string? SymbolInfo
    );

    private static List<FileEntry> CollectEntries(string root, string currentDir)
    {
        var entries = new List<FileEntry>();
        var rootPrefix = root.Replace('\\', '/').TrimEnd('/') + "/";

        try
        {
            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                var name = Path.GetFileName(dir);
                var relPath = Path.GetRelativePath(root, dir).Replace('\\', '/');

                if (IsIgnored(relPath) || IsIgnored(relPath + "/") || name.StartsWith('.'))
                    continue;

                entries.Add(new FileEntry(relPath, name, true, 0, Directory.GetLastWriteTime(dir), null));
                entries.AddRange(CollectEntries(root, dir));
            }

            foreach (var file in Directory.GetFiles(currentDir))
            {
                var name = Path.GetFileName(file);
                var relPath = Path.GetRelativePath(root, file).Replace('\\', '/');

                if (IsIgnored(relPath) || name.StartsWith('.') && name != ".env.example")
                    continue;

                var fi = new FileInfo(file);
                var symbols = ExtractSymbols(file);
                var symbolInfo = string.IsNullOrEmpty(symbols) ? null : symbols;

                entries.Add(new FileEntry(relPath, name, false, fi.Length, fi.LastWriteTime, symbolInfo));
            }
        }
        catch (Exception ex) { DebugLog.Log("RepoMap", $"目录扫描失败: {ex.Message}"); }

        return entries;
    }

    private static string ExtractSymbols(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SymbolPatterns.TryGetValue(ext, out var pair)) return "";

        try
        {
            var content = File.ReadAllText(filePath);
            // 只读前 64KB，大文件跳过
            if (content.Length > 65536) content = content[..65536];

            var matches = pair.regex.Matches(content);
            var names = new HashSet<string>();
            var count = 0;

            foreach (Match m in matches)
            {
                if (count++ > 20) break; // 每个文件最多 20 个符号
                foreach (Group g in m.Groups)
                {
                    if (g.Success && g.Index > 0 && !string.IsNullOrEmpty(g.Value) && g.Value.Length > 1)
                    {
                        // 跳过关键字
                        var v = g.Value;
                        if (v is "if" or "for" or "while" or "return" or "class" or "struct"
                            or "public" or "private" or "protected" or "static" or "void"
                            or "int" or "string" or "bool" or "var" or "let" or "const"
                            or "function" or "export" or "import" or "from" or "async"
                            or "await" or "new" or "this" or "super" or "extends"
                            or "implements" or "override" or "virtual" or "abstract") continue;
                        names.Add(v);
                    }
                }
            }

            if (names.Count == 0) return "";
            return $"[{pair.kind}] " + string.Join(" ", names.OrderBy(n => n).Take(12));
        }
        catch (Exception ex) { DebugLog.Log("RepoMap", $"LSP 符号提取失败: {ex.Message}"); return ""; }
    }

    private static string BuildTree(List<FileEntry> allEntries, string root)
    {
        // 按修改时间排序文件（最近的在前）
        // 目录按字母排序始终在最前

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"```");
        sb.AppendLine(root.Replace('\\', '/'));

        // 构建目录-文件层级结构
        var dirs = allEntries.Where(e => e.IsDir)
            .OrderBy(e => e.RelativePath)
            .ToList();
        var files = allEntries.Where(e => !e.IsDir)
            .OrderByDescending(e => e.LastModified)
            .ToList();

        // 最多显示 100 个条目
        var maxEntries = Math.Min(allEntries.Count, 100);
        var shownCount = 0;
        var shownFiles = new HashSet<string>();

        // 遍历目录树
        foreach (var dir in dirs.Take(50))
        {
            if (shownCount >= maxEntries) break;

            var depth = dir.RelativePath.Count(c => c == '/');
            var indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}{dir.Name}/");
            shownCount++;

            // 显示该目录中最近修改的文件（最多 5 个）
            var dirFiles = files
                .Where(f => f.RelativePath.StartsWith(dir.RelativePath + "/"))
                .Take(5)
                .ToList();

            foreach (var file in dirFiles)
            {
                if (shownCount >= maxEntries) break;
                shownFiles.Add(file.RelativePath);

                var fIndent = new string(' ', (depth + 1) * 2);
                var marker = file.SymbolInfo != null ? " ▶" : "";
                sb.Append($"{fIndent}{file.Name}{marker}");

                if (file.SymbolInfo != null)
                {
                    sb.Append($"  {file.SymbolInfo}");
                }

                sb.AppendLine();
                shownCount++;
            }
        }

        // 根目录尚未显示的文件
        var rootFiles = files
            .Where(f => !shownFiles.Contains(f.RelativePath) && !f.RelativePath.Contains('/'))
            .Take(maxEntries - shownCount)
            .ToList();

        foreach (var file in rootFiles)
        {
            var marker = file.SymbolInfo != null ? " ▶" : "";
            sb.Append($"  {file.Name}{marker}");
            if (file.SymbolInfo != null)
                sb.Append($"  {file.SymbolInfo}");
            sb.AppendLine();
            shownCount++;
        }

        // 嵌套深度超过 1 但目录未显示的剩余文件（简要列出）
        var remaining = files
            .Where(f => !shownFiles.Contains(f.RelativePath) && f.RelativePath.Contains('/'))
            .Take(maxEntries - shownCount);

        if (remaining.Any())
        {
            sb.AppendLine($"  ...({files.Count - shownCount} more files)");
        }

        sb.AppendLine("```");
        return sb.ToString();
    }
}
