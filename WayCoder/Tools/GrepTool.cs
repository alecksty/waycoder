using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// 增强版内容搜索（对标 crush grep）。
///
/// 功能：
///   - 正则表达式搜索 + literal_text 模式（自动转义特殊字符）
///   - ripgrep 优先集成（速度更快）
///   - MIME 类型检测跳过二进制文件
///   - FileIgnoreManager 过滤
/// </summary>
public class GrepTool : ITool
{
    public string Name => "grep";
    public string Description => "使用正则表达式搜索文件内容。返回匹配行，包含文件路径和行号。支持 literal_text 模式。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("pattern", JNode.Param("string", "要搜索的正则表达式模式（或 literal_text 模式下的纯文本）"))
            .Set("path", JNode.Param("string", "要搜索的文件或目录（默认：当前工作目录）"))
            .Set("include", JNode.Param("string", "仅搜索匹配此 glob 模式的文件（如 '*.py'）"))
            .Set("literal_text", JNode.Param("boolean", "如果为 true，pattern 将被当做纯文本处理（自动转义正则特殊字符），默认 false")))
        .Set("required", JNode.Array("pattern"));

    // 在权威跳过表（FileIgnoreManager）之上额外跳过的噪音目录（.git/node_modules/__pycache__
    // 等已由权威表覆盖，这里只剩 grep 特有的 dist/build —— 它们是构建产物，grep 进不去更安静）。
    private static readonly HashSet<string> ExtraSkipDirs = ["dist", "build"];

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var pattern = arguments.GetValueOrDefault("pattern")?.ToString() ?? "";
        var searchPath = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(searchPath))
            searchPath = ".";
        var include = arguments.TryGetValue("include", out var inc) ? inc?.ToString() : null;
        var literalText = arguments.TryGetValue("literal_text", out var lt) &&
                          lt?.ToString()?.ToLowerInvariant() == "true";

        return Task.FromResult(Execute(pattern, searchPath, include, literalText));
    }

    private static string Execute(string pattern, string searchPath, string? include, bool literalText)
    {
        // literal_text 模式：转义正则特殊字符
        var searchPattern = literalText ? Regex.Escape(pattern) : pattern;

        Regex regex;
        try
        {
            regex = new Regex(searchPattern, RegexOptions.None, TimeSpan.FromSeconds(Config.Instance.RegexTimeoutSec));
        }
        catch (RegexParseException ex)
        {
            return $"无效的正则表达式：{ex.GetType().Name}: {ex.Message}";
        }

        var basePath = CwdContext.Resolve(searchPath); // cd 后相对路径基于被跟踪工作目录
        if (!File.Exists(basePath) && !Directory.Exists(basePath))
            return $"错误：{searchPath} 未找到";

        List<string> files;
        if (File.Exists(basePath))
        {
            files = [basePath];
        }
        else
        {
            files = WalkDirectory(basePath, include);
        }

        var matches = new List<string>();
        try
        {
            foreach (var fp in files)
            {
                // 检查路径中是否包含跳过的目录（用 FileIgnoreManager）
                if (FileIgnoreManager.IsIgnored(fp, basePath))
                    continue;

                string text;
                try { text = File.ReadAllText(fp, Encoding.UTF8); }
                catch { continue; }

                // 去掉末尾换行再 Split：否则以 \n 结尾的文件会产生一个幻影空行，
                // `^$` 等空行匹配会误报一行不存在的末尾
                var lines = text.TrimEnd('\r', '\n').Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        matches.Add($"{fp}:{i + 1}: {lines[i].TrimEnd('\r')}");
                        if (matches.Count >= Global.MaxGrepResultLines)
                        {
                            matches.Add($"...（已达到 {Global.MaxGrepResultLines} 条匹配上限）");
                            return string.Join("\n", matches);
                        }
                    }
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // 灾难性回溯正则超时：返回已匹配的部分结果，而非把整个错误抛给上层丢全部结果
            return matches.Count > 0
                ? string.Join("\n", matches) + $"\n...（正则匹配超时（>{Config.Instance.RegexTimeoutSec}s），已返回部分结果）"
                : $"错误：正则匹配超时（>{Config.Instance.RegexTimeoutSec}s）。请简化正则表达式。";
        }

        return matches.Count > 0 ? string.Join("\n", matches) : "未找到匹配项。";
    }

    /// <summary>
    /// 遍历目录树，跳过垃圾目录。
    /// </summary>
    private static List<string> WalkDirectory(string root, string? include)
    {
        var results = new List<string>();
        WalkRecursive(root, include ?? "*", results);
        return results;
    }

    /// <summary>逐目录递归收集文件，每个目录独立 try/catch —— 单个不可访问子目录
    /// 不再像 <c>Directory.GetFiles(..., AllDirectories)</c> 那样让整棵树搜索失败。</summary>
    private static void WalkRecursive(string dir, string searchPattern, List<string> results, int depth = 0)
        // 遍历骨架 + 结果上限 + 深度上限 + 跳过判断统一走 FileWalker（此前本类自写一份，
    // 且同时用了 5000 与 Global.MaxGrepResults 两个不同的上限，后者才是配置项）
        => FileWalker.Walk(dir, results, Global.MaxGrepResults,
            (d, res) =>
            {
                foreach (var file in Directory.GetFiles(d, searchPattern))
                {
                    res.Add(file);
                    if (res.Count >= Global.MaxGrepResults) return;
                }
            },
            ExtraSkipDirs);
}
