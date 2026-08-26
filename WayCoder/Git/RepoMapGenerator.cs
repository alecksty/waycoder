using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 仓库地图生成器 — 扫描项目结构，提取关键符号，构建引用图谱。
///
/// 灵感源自 aider 的 repomap，但完全从零实现。
/// 特点：
///   - 尊重 .gitignore 规则
///   - 按语言提取函数/类/方法等关键符号
///   - 扫描 import/using 构建文件间引用关系
///   - PageRank 排序：被引用最多的文件优先展示
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

    // ---- 引用扫描：import/using/include 正则 ----

    private static readonly Dictionary<string, Regex> ImportPatterns = new()
    {
        [".cs"] = new Regex(@"^\s*using\s+(\S+)", RegexOptions.Multiline),
        [".py"] = new Regex(@"^\s*(?:from\s+(\S+)\s+import|import\s+(\S+))", RegexOptions.Multiline),
        [".js"] = new Regex(@"^\s*(?:import\s+.*\s+from\s+['""]([^'""]+)|require\s*\(\s*['""]([^'""]+))", RegexOptions.Multiline),
        [".ts"] = new Regex(@"^\s*(?:import\s+.*\s+from\s+['""]([^'""]+)|require\s*\(\s*['""]([^'""]+))", RegexOptions.Multiline),
        [".jsx"] = new Regex(@"^\s*(?:import\s+.*\s+from\s+['""]([^'""]+)|require\s*\(\s*['""]([^'""]+))", RegexOptions.Multiline),
        [".tsx"] = new Regex(@"^\s*(?:import\s+.*\s+from\s+['""]([^'""]+)|require\s*\(\s*['""]([^'""]+))", RegexOptions.Multiline),
        [".go"] = new Regex(@"^\s*import\s+(?:\(\s*)?(?:""([^""]+)""|(\S+)\s+""([^""]+)"")", RegexOptions.Multiline),
        [".rs"] = new Regex(@"^\s*(?:use\s+(\S+)|mod\s+(\S+))", RegexOptions.Multiline),
        [".java"] = new Regex(@"^\s*import\s+(\S+)", RegexOptions.Multiline),
        [".kt"] = new Regex(@"^\s*import\s+(\S+)", RegexOptions.Multiline),
        [".rb"] = new Regex(@"^\s*(?:require\s+['""]([^'""]+)|require_relative\s+['""]([^'""]+))", RegexOptions.Multiline),
        [".php"] = new Regex(@"^\s*(?:use\s+(\S+)|require(?:_once)?\s+['""]([^'""]+))", RegexOptions.Multiline),
        [".c"] = new Regex(@"^\s*#include\s*[<""]([^>""]+)", RegexOptions.Multiline),
        [".cpp"] = new Regex(@"^\s*#include\s*[<""]([^>""]+)", RegexOptions.Multiline),
        [".h"] = new Regex(@"^\s*#include\s*[<""]([^>""]+)", RegexOptions.Multiline),
        [".swift"] = new Regex(@"^\s*import\s+(\S+)", RegexOptions.Multiline),
        [".dart"] = new Regex(@"^\s*import\s+['""]([^'""]+)", RegexOptions.Multiline),
    };

    /// <summary>文件引用图谱：文件名 → 它引用了哪些文件</summary>
    private static readonly Dictionary<string, HashSet<string>> FileRefs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>文件被引用计数：文件名 → 被多少其他文件引用</summary>
    private static readonly Dictionary<string, int> FileRank = new(StringComparer.OrdinalIgnoreCase);

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

        // 1. 收集文件树 + 符号 + 引用
        FileRefs.Clear();
        FileRank.Clear();
        _entryCount = 0; // 重置条目计数（见 CollectEntries 上限）
        var entries = CollectEntries(root, root);

        // 2. 构建引用图谱（扫描 import/using）
        foreach (var entry in entries.Where(e => !e.IsDir))
        {
            var fullPath = Path.Combine(root, entry.RelativePath);
            ScanReferences(entry.RelativePath, fullPath);
        }

        // 3. PageRank 排序：计算每个文件的 rank（被引用次数）
        foreach (var (_, refs) in FileRefs)
        {
            foreach (var refFile in refs)
            {
                if (!FileRank.ContainsKey(refFile)) FileRank[refFile] = 0;
                FileRank[refFile]++;
            }
        }

        // 4. 添加排名摘要
        var topRanked = FileRank
            .OrderByDescending(kv => kv.Value)
            .Take(15)
            .Where(kv => kv.Value >= 2) // 至少被引用 2 次才显示
            .ToList();

        if (topRanked.Count > 0)
        {
            sb.AppendLine("### 🔗 核心文件（按引用热度排序）");
            sb.AppendLine();
            foreach (var (file, count) in topRanked)
            {
                var symbols = entries.FirstOrDefault(e =>
                    e.RelativePath.Equals(file, StringComparison.OrdinalIgnoreCase))?.SymbolInfo;
                var symStr = symbols != null ? $" — {symbols}" : "";
                sb.AppendLine($"- `{file}` ← 被 **{count}** 个文件引用{symStr}");
            }
            sb.AppendLine();
        }

        // 5. 文件树
        var tree = BuildTree(entries, root, topRanked.Select(t => t.Key).ToHashSet(StringComparer.OrdinalIgnoreCase));
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
        DateTime LastModified,
        string? SymbolInfo
    );

    private static readonly int EntryLimit = 300;   // 单次 RepoMap 收集条目上限
    private static int _entryCount;                  // 当前收集数（Generate 重置）

    private static List<FileEntry> CollectEntries(string root, string currentDir, int depth = 0)
    {
        var entries = new List<FileEntry>();
        // 深度上限防符号链接环 → StackOverflow；条目上限防 home/超大目录全量递归扫描卡死
        // （在 ~ 目录启动时 root 是 home，递归几十万文件导致 TUI 永远不出现）
        if (depth > 8 || _entryCount >= EntryLimit) return entries;

        try
        {
            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                if (_entryCount >= EntryLimit) break;
                var name = Path.GetFileName(dir);
                var relPath = Path.GetRelativePath(root, dir).Replace('\\', '/');

                if (IsIgnored(relPath) || IsIgnored(relPath + "/") || name.StartsWith('.'))
                    continue;

                _entryCount++;
                entries.Add(new FileEntry(relPath, name, true, Directory.GetLastWriteTime(dir), null));
                entries.AddRange(CollectEntries(root, dir, depth + 1));
            }

            foreach (var file in Directory.GetFiles(currentDir))
            {
                if (_entryCount >= EntryLimit) break;
                var name = Path.GetFileName(file);
                var relPath = Path.GetRelativePath(root, file).Replace('\\', '/');

                if (IsIgnored(relPath) || name.StartsWith('.') && name != ".env.example")
                    continue;

                _entryCount++;
                var fi = new FileInfo(file);
                var symbols = ExtractSymbols(file);
                var symbolInfo = string.IsNullOrEmpty(symbols) ? null : symbols;

                entries.Add(new FileEntry(relPath, name, false, fi.LastWriteTime, symbolInfo));
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
            if (content.Length > 65536) content = ContextManager.TruncateByRunes(content, 65536);

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

    /// <summary>扫描文件的 import/using 语句，构建引用图谱</summary>
    private static void ScanReferences(string relPath, string fullPath)
    {
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        if (!ImportPatterns.TryGetValue(ext, out var regex)) return;

        if (!FileRefs.ContainsKey(relPath))
            FileRefs[relPath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var content = File.ReadAllText(fullPath);
            if (content.Length > 65536) content = ContextManager.TruncateByRunes(content, 65536);

            var matches = regex.Matches(content);
            foreach (Match m in matches)
            {
                foreach (Group g in m.Groups)
                {
                    if (!g.Success || g.Index == 0 || string.IsNullOrEmpty(g.Value)) continue;
                    var import = g.Value;

                    // 只保留项目内部引用（不以 @ / 等外部前缀开头）
                    if (import.StartsWith('@') || import.StartsWith("http") || import.StartsWith("node:")) continue;

                    // 尝试匹配项目内文件
                    var importBase = import.Replace('.', '/').Replace("\\", "/");
                    // 对于 C# using，匹配命名空间 → 文件路径
                    if (ext == ".cs")
                    {
                        // using WayCoder.UI.Tui.TuiScreens → WayCoder/UI/TuiScreens/*.cs
                        var nsPath = import.Replace('.', '/');
                        FileRefs[relPath].Add(nsPath);
                    }
                    else
                    {
                        FileRefs[relPath].Add(importBase);
                    }
                }
            }
        }
        catch { /* 读取失败，跳过 */ }
    }

    private static string BuildTree(List<FileEntry> allEntries, string root, HashSet<string>? coreFiles = null)
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
                var isCore = coreFiles?.Contains(file.RelativePath) == true;
                var marker = file.SymbolInfo != null ? " ▶" : "";
                var star = isCore ? "⭐" : "";
                sb.Append($"{fIndent}{star}{file.Name}{marker}");

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
            var isCore = coreFiles?.Contains(file.RelativePath) == true;
            var marker = file.SymbolInfo != null ? " ▶" : "";
            var star = isCore ? "⭐" : "";
            sb.Append($"  {star}{file.Name}{marker}");
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
