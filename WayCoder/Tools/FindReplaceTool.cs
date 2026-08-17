using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// 查找替换工具 —— 纯 C# 实现，跨文件搜索并替换。
/// 支持正则匹配、逐文件预览、干跑模式。
/// 优势：可控制文件数上限、匹配数上限、输出大小。
/// </summary>
public class FindReplaceTool : ITool
{
    public string Name => "find_replace";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "跨文件查找并替换。支持正则、glob 文件过滤、干跑预览。返回每个文件的匹配详情。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索目录路径（默认当前目录）"))
            .Set("pattern", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索的正则表达式或纯文本"))
            .Set("replacement", JNode.Object()
                .Set("type", "string")
                .Set("description", "替换文本（为空则仅查找不替换）"))
            .Set("glob", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件过滤 glob，如 '*.cs'、'*.{md,txt}'（默认所有文本文件）"))
            .Set("max_files", JNode.Object()
                .Set("type", "integer")
                .Set("description", "最多扫描文件数（默认 50）"))
            .Set("max_per_file", JNode.Object()
                .Set("type", "integer")
                .Set("description", "每文件最多显示匹配数（默认 10）"))
            .Set("ignore_case", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "是否忽略大小写（默认 true）"))
            .Set("dry_run", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "仅预览不实际替换（默认 true）")))
        .Set("required", JNode.Array().Add("pattern"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString();
        var pattern = arguments.GetValueOrDefault("pattern")?.ToString() ?? "";
        var replacement = arguments.GetValueOrDefault("replacement")?.ToString();
        var glob = arguments.GetValueOrDefault("glob")?.ToString() ?? "*.*";
        var maxFiles = ToolArgs.GetInt(arguments, "max_files", 50);
        var maxPerFile = ToolArgs.GetInt(arguments, "max_per_file", 10);
        var ignoreCase = !arguments.TryGetValue("ignore_case", out var ic) || ic is not bool icb || icb;
        var dryRun = !arguments.TryGetValue("dry_run", out var dr) || dr is not bool drb || drb;

        return Task.FromResult(Execute(path, pattern, replacement, glob, maxFiles, maxPerFile, ignoreCase, dryRun));
    }

    private static string Execute(string? path, string pattern, string? replacement,
        string glob, int maxFiles, int maxPerFile, bool ignoreCase, bool dryRun)
    {
        if (string.IsNullOrEmpty(pattern))
            return "错误：pattern 参数不能为空";

        // 负值钳制：maxPerFile 为负时 Math.Min/lineCount>=maxPerFile 均立即成立，累计负匹配数 + 误导文案
        maxFiles = Math.Max(1, maxFiles);
        maxPerFile = Math.Max(1, maxPerFile);

        try
        {
            path ??= BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory();
            path = Path.GetFullPath(path, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
            if (!Directory.Exists(path))
                return $"错误：目录不存在 — {path}";

            // 编译正则
            var regexOptions = RegexOptions.Multiline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            Regex regex;
            try
            {
                regex = new Regex(pattern, regexOptions);
            }
            catch (RegexParseException)
            {
                // 不是有效正则，当作纯文本搜索
                regex = new Regex(Regex.Escape(pattern), regexOptions);
            }

            var hasReplacement = !string.IsNullOrEmpty(replacement);
            var mode = dryRun ? "预览" : (hasReplacement ? "查找替换" : "查找");

            var sb = new StringBuilder();
            sb.AppendLine($"## {mode}: `{pattern}`");
            if (hasReplacement)
                sb.AppendLine($"替换为: `{replacement}`");
            sb.AppendLine($"目录: {path}  |  glob: {glob}  |  {(ignoreCase ? "忽略大小写" : "区分大小写")}");
            sb.AppendLine();

            // 收集文件
            var files = new List<string>();
            CollectFiles(path, glob, files, ref maxFiles);

            var totalMatches = 0;
            var filesChanged = 0;
            var filesWithMatches = 0;

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    var matches = regex.Matches(content);

                    if (matches.Count == 0) continue;

                    totalMatches += Math.Min(matches.Count, maxPerFile);
                    var fileDisplayed = false;

                    var lineCount = 0;
                    foreach (Match match in matches)
                    {
                        if (lineCount >= maxPerFile) break;

                        if (!fileDisplayed)
                        {
                            sb.AppendLine($"### {Path.GetRelativePath(path, file)}  ({matches.Count} 处匹配)");
                            fileDisplayed = true;
                            filesWithMatches++;
                        }

                        // 获取匹配上下文（前后各 30 字符），起始/结束对齐码元边界避免切半代理对（emoji/CJK 扩展 B → U+FFFD）
                        var start = Math.Max(0, match.Index - 30);
                        var end = Math.Min(content.Length, match.Index + match.Length + 30);
                        while (start > 0 && char.IsLowSurrogate(content[start])) start--;
                        while (end < content.Length && char.IsLowSurrogate(content[end])) end++;
                        var context = content.Substring(start, end - start).Replace("\r", "").Replace("\n", "\\n");
                        var marker = new string(' ', Math.Min(30, match.Index - start));
                        sb.AppendLine($"  `{context.Trim()}`");
                        sb.AppendLine($"  {marker}«bold»^{new string('~', Math.Max(0, match.Length - 1))}«/»");
                        lineCount++;
                    }

                    if (matches.Count > maxPerFile)
                        sb.AppendLine($"  ... 还有 {matches.Count - maxPerFile} 处匹配");

                    // 执行替换
                    if (hasReplacement && !dryRun)
                    {
                        // 用 MatchEvaluator 返回字面量，避免 replacement 中的 '$' 被解析为
                        // 正则替换符（如 "cost $10" 会因组不存在抛 ArgumentException 被吞）。
                        var newContent = regex.Replace(content, m => replacement!);
                        File.WriteAllText(file, newContent, Encoding.UTF8);
                        filesChanged++;
                        sb.AppendLine($"  ✔ 已替换");
                    }

                    sb.AppendLine();
                }
                catch { /* 文件读取失败，跳过 */ }
            }

            sb.AppendLine($"---");
            sb.AppendLine($"扫描文件: {files.Count}  |  匹配文件: {filesWithMatches}  |  总匹配: {totalMatches}");
            if (!dryRun && hasReplacement)
                sb.AppendLine($"已修改: {filesChanged} 个文件");

            var result = sb.ToString();
            if (result.Length > 15_000)
                result = ContextManager.TruncateByRunes(result, 10_000) + $"\n... (已截断，共 {result.Length} 字符) ...\n" + ContextManager.TruncateTailByRunes(result, 3000);

            return result.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"find_replace 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static void CollectFiles(string dir, string glob, List<string> files, ref int maxFiles, int depth = 0)
    {
        if (maxFiles <= 0 || depth > 64) return; // 深度上限防符号链接环无限递归 → StackOverflow
        try
        {
            // 跳过隐藏目录
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                if (Path.GetFileName(subDir).StartsWith('.')) continue;
                if (subDir.EndsWith("node_modules") || subDir.EndsWith(".git")
                    || subDir.EndsWith("bin") || subDir.EndsWith("obj")
                    || subDir.EndsWith("__pycache__") || subDir.EndsWith(".vs"))
                    continue;
                CollectFiles(subDir, glob, files, ref maxFiles, depth + 1);
            }

            foreach (var file in ExpandBraces(glob).SelectMany(p => Directory.GetFiles(dir, p)))
            {
                if (maxFiles <= 0) break;
                if (Path.GetFileName(file).StartsWith('.')) continue;
                // 仅处理文本文件（按扩展名粗略判断）
                if (IsTextFile(file))
                {
                    if (!files.Contains(file)) // 花括号展开后可能重复命中同一文件，去重
                    {
                        files.Add(file);
                        maxFiles--;
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>把 `*.{md,txt}` 花括号 glob 展开为多个 pattern（.NET 的 GetFiles 不认花括号，否则静默匹配 0 个）。</summary>
    private static IEnumerable<string> ExpandBraces(string glob)
    {
        int open = glob.IndexOf('{');
        if (open < 0) { yield return glob; yield break; }
        int close = glob.IndexOf('}', open + 1);
        if (close < 0) { yield return glob; yield break; }
        var pre = glob[..open];
        var post = glob[(close + 1)..];
        foreach (var opt in glob[(open + 1)..close].Split(','))
            foreach (var expanded in ExpandBraces(pre + opt + post))
                yield return expanded;
    }

    private static bool IsTextFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".cs" or ".py" or ".js" or ".ts" or ".jsx" or ".tsx" or ".go" or ".rs"
            or ".java" or ".rb" or ".php" or ".swift" or ".kt" or ".lua" or ".dart" or ".r"
            or ".sh" or ".bash" or ".zsh" or ".ps1" or ".bat" or ".cmd"
            or ".html" or ".css" or ".scss" or ".less" or ".vue" or ".svelte"
            or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or ".ini" or ".cfg"
            or ".md" or ".txt" or ".rst" or ".tex" or ".sql" or ".c" or ".cpp" or ".h"
            or ".csproj" or ".sln" or ".props" or ".targets" or ".razor" or ".xaml"
            or ".proto" or ".graphql" or ".dockerfile" or ".env" or ".gitignore";
    }
}
