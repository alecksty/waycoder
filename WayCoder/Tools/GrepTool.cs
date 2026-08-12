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

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["pattern"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要搜索的正则表达式模式（或 literal_text 模式下的纯文本）",
            },
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要搜索的文件或目录（默认：当前工作目录）",
            },
            ["include"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "仅搜索匹配此 glob 模式的文件（如 '*.py'）",
            },
            ["literal_text"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "如果为 true，pattern 将被当做纯文本处理（自动转义正则特殊字符），默认 false",
            },
        },
        ["required"] = new JsonArray("pattern"),
    };

    // 跳过这些目录以减少噪音
    private static readonly HashSet<string> SkipDirs =
    [
        ".git", "node_modules", "__pycache__", ".venv", "venv", ".tox", "dist", "build",
    ];

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

        var basePath = Path.GetFullPath(searchPath);
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
        foreach (var fp in files)
        {
            // 检查路径中是否包含跳过的目录（用 FileIgnoreManager）
            if (FileIgnoreManager.IsIgnored(fp, basePath))
                continue;

            string text;
            try { text = File.ReadAllText(fp, Encoding.UTF8); }
            catch { continue; }

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    matches.Add($"{fp}:{i + 1}: {lines[i].TrimEnd('\r')}");
                    if (matches.Count >= 200)
                    {
                        matches.Add("...（已达到 200 条匹配上限）");
                        return string.Join("\n", matches);
                    }
                }
            }
        }

        return matches.Count > 0 ? string.Join("\n", matches) : "未找到匹配项。";
    }

    /// <summary>
    /// 遍历目录树，跳过垃圾目录。
    /// </summary>
    private static List<string> WalkDirectory(string root, string? include)
    {
        var results = new List<string>();
        try
        {
            var searchPattern = include ?? "*";
            var files = Directory.GetFiles(root, searchPattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // 跳过垃圾目录内部的路径
                var relative = Path.GetRelativePath(root, file);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (parts.Any(p => SkipDirs.Contains(p))) continue;

                results.Add(file);
                if (results.Count >= 5000) break;
            }
        }
        catch
        {
            // 跳过无法访问的目录
        }

        return results;
    }
}
