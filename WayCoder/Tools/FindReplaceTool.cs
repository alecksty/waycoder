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
    public string Description => L.Pick("跨文件查找并替换。支持正则、glob 文件过滤、干跑预览。返回每个文件的匹配详情。纯 C# 实现。", "Search and replace across files. Supports regex, glob file filtering, and dry-run preview. Returns match details for each file. Pure C# implementation.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Param("string", L.Pick("搜索目录路径（默认当前目录）", "Directory to search. Default: current directory.")))
            .Set("pattern", JNode.Param("string", L.Pick("搜索的正则表达式或纯文本", "Regular expression or plain text to search for.")))
            .Set("replacement", JNode.Param("string", L.Pick("替换文本（为空则仅查找不替换）", "Replacement text (empty means search only, no replacement).")))
            .Set("glob", JNode.Param("string", L.Pick("文件过滤 glob，如 '*.cs'、'*.{md,txt}'（默认所有文本文件）", "File filter glob, e.g. '*.cs' or '*.{md,txt}'. Default: all text files.")))
            .Set("max_files", JNode.Param("integer", L.Pick("最多扫描文件数（默认 50）", "Maximum number of files to scan. Default 50.")))
            .Set("max_per_file", JNode.Param("integer", L.Pick("每文件最多显示匹配数（默认 10）", "Maximum matches displayed per file. Default 10.")))
            .Set("ignore_case", JNode.Param("boolean", L.Pick("是否忽略大小写（默认 true）", "Whether to ignore case. Default true.")))
            .Set("dry_run", JNode.Param("boolean", L.Pick("仅预览不实际替换（默认 true）", "Preview only, do not actually replace. Default true."))))
        .Set("required", JNode.Array("pattern"));

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
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        return Task.FromResult(Execute(path, pattern, replacement, glob, maxFiles, maxPerFile, ignoreCase, dryRun, agentId));
    }

    private static string Execute(string? path, string pattern, string? replacement,
        string glob, int maxFiles, int maxPerFile, bool ignoreCase, bool dryRun, string agentId)
    {
        if (string.IsNullOrEmpty(pattern))
            return L.Pick("错误：pattern 参数不能为空", "Error: the pattern parameter cannot be empty");

        // 负值钳制：maxPerFile 为负时 Math.Min/lineCount>=maxPerFile 均立即成立，累计负匹配数 + 误导文案
        maxFiles = Math.Max(1, maxFiles);
        maxPerFile = Math.Max(1, maxPerFile);

        try
        {
            path ??= CwdContext.Root;
            path = CwdContext.Resolve(path); // cd 后相对路径基于被跟踪工作目录
            if (PathGuard.RequireDir(path) is { } e) return e;

            // 编译正则（带超时，防 (a+)+$ 类灾难性回溯卡死 Agent 主循环）
            var regexOptions = RegexOptions.Multiline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            var regexTimeout = TimeSpan.FromSeconds(Config.Instance.RegexTimeoutSec);
            Regex regex;
            try
            {
                regex = new Regex(pattern, regexOptions, regexTimeout);
            }
            catch (RegexParseException)
            {
                // 不是有效正则，当作纯文本搜索
                regex = new Regex(Regex.Escape(pattern), regexOptions, regexTimeout);
            }

            var hasReplacement = !string.IsNullOrEmpty(replacement);
            var mode = dryRun
                ? L.Pick("预览", "Preview")
                : hasReplacement ? L.Pick("查找替换", "Find and replace") : L.Pick("查找", "Find");

            var sb = new StringBuilder();
            sb.AppendLine($"## {mode}: `{pattern}`");
            if (hasReplacement)
                sb.AppendLine(L.Pick($"替换为: `{replacement}`", $"Replacement: `{replacement}`"));
            sb.AppendLine(L.Pick($"目录: {path}  |  glob: {glob}  |  {(ignoreCase ? "忽略大小写" : "区分大小写")}",
                                 $"Directory: {path}  |  glob: {glob}  |  {(ignoreCase ? "case-insensitive" : "case-sensitive")}"));
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
                            sb.AppendLine(L.Pick($"### {Path.GetRelativePath(path, file)}  ({matches.Count} 处匹配)",
                                                 $"### {Path.GetRelativePath(path, file)}  ({matches.Count} matches)"));
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
                        sb.AppendLine(L.Pick($"  ... 还有 {matches.Count - maxPerFile} 处匹配",
                                             $"  ... {matches.Count - maxPerFile} more matches"));

                    // 执行替换
                    if (hasReplacement && !dryRun)
                    {
                        // 敏感 + 沙箱边界走统一守卫（此前把 Guard 手工展开成两步，且文案与
                        // PathSafety.Guard 已经漂移 —— 少了「安全策略：」一截）。逐文件 continue 语义不变。
                        if (PathSafety.Guard(file) is { } blocked) { sb.AppendLine($"  {blocked}"); continue; }

                        // 文件锁（防多 Agent 并发改写同一文件）
                        var lockErr = FileLockManager.TryAcquireOrError(file, agentId);
                        if (lockErr != null) { sb.AppendLine($"  ❌ {lockErr}"); continue; }

                        try
                        {
                            // 用 MatchEvaluator 返回字面量，避免 replacement 中的 '$' 被解析为
                            // 正则替换符（如 "cost $10" 会因组不存在抛 ArgumentException 被吞）。
                            var newContent = regex.Replace(content, m => replacement!);
                            Global.WriteAllTextPreserveBom(file, newContent);
                            // 纳入变更追踪（与 write/edit/multiedit 对齐：自动 commit 精准暂存 + FileTracker 哈希）
                            EditFileTool.RecordChange(file, content, newContent);
                            FileTracker.RecordWrite(file);
                            filesChanged++;
                            sb.AppendLine(L.Pick("  ✔ 已替换", "  ✔ Replaced"));
                        }
                        finally
                        {
                            FileLockManager.Release(file, agentId);
                        }
                    }

                    sb.AppendLine();
                }
                catch (RegexMatchTimeoutException)
                {
                    // 正则匹配超时：报告并跳过该文件（而非静默吞，防误以为扫描完整）
                    sb.AppendLine(L.Pick($"### {Path.GetRelativePath(path, file)}  (正则匹配超时，已跳过)",
                                         $"### {Path.GetRelativePath(path, file)}  (regex match timed out; skipped)"));
                    sb.AppendLine();
                }
                catch (Exception ex)
                {
                    // 报告失败文件（而非静默吞，防 Agent 误以为全部替换成功）
                    sb.AppendLine(L.Pick($"### {Path.GetRelativePath(path, file)}  (处理失败: {ex.GetType().Name}: {ex.Message})",
                                         $"### {Path.GetRelativePath(path, file)}  (processing failed: {ex.GetType().Name}: {ex.Message})"));
                    sb.AppendLine();
                }
            }

            sb.AppendLine($"---");
            sb.AppendLine(L.Pick($"扫描文件: {files.Count}  |  匹配文件: {filesWithMatches}  |  总匹配: {totalMatches}",
                                 $"Files scanned: {files.Count}  |  Files matched: {filesWithMatches}  |  Total matches: {totalMatches}"));
            if (!dryRun && hasReplacement)
                sb.AppendLine(L.Pick($"已修改: {filesChanged} 个文件", $"Modified: {filesChanged} file(s)"));

            var result = sb.ToString();
            if (result.Length > 15_000)
                result = ContextManager.TruncateKeepHeadTail(result, 10_000, 3000,
                    L.Pick($"\n... (已截断，共 {result.Length} 字符) ...\n",
                           $"\n... (truncated, {result.Length} characters in total) ...\n"));

            return result.TrimEnd();
        }
        catch (Exception ex)
        {
            return ToolErrors.Error("find_replace ", ex);
        }
    }

    private static void CollectFiles(string dir, string glob, List<string> files, ref int maxFiles, int depth = 0)
    {
        // 遍历骨架 + 深度上限 + 跳过判断统一走 FileWalker。**注意**：本类的上限是「剩余额度」
        // （ref maxFiles，边收边减），而 FileWalker 用的是绝对结果数，故先用一个局部列表收集、
        // 收完再按剩余额度并入 —— 保持调用方「跨多次调用共享预算」的语义不变。
        int budget = maxFiles;   // ref 参数不能进 lambda（CS1628），先落到局部
        var collected = new List<string>();
        FileWalker.Walk(dir, collected, budget,
            (d, res) =>
            {
                foreach (var file in ExpandBraces(glob).SelectMany(p => Directory.GetFiles(d, p)))
                {
                    if (res.Count >= budget) return;
                    if (Path.GetFileName(file).StartsWith('.')) continue;
                    if (!IsTextFile(file)) continue;          // 仅处理文本文件（按扩展名粗略判断）
                    if (files.Contains(file) || res.Contains(file)) continue; // 花括号展开后可能重复命中
                    res.Add(file);
                }
            });

        foreach (var f in collected)
        {
            if (maxFiles <= 0) break;
            files.Add(f);
            maxFiles--;
        }
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
