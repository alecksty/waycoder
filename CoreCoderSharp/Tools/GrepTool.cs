using System.Text;
using System.Text.RegularExpressions;

namespace CoreCoderSharp.Tools;

/// <summary>
/// 支持正则表达式的内容搜索。
/// </summary>
public class GrepTool : ITool
{
    public string Name => "grep";
    public string Description => "使用正则表达式搜索文件内容。返回匹配行，包含文件路径和行号。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["pattern"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要搜索的正则表达式模式",
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
        var searchPath = arguments.GetValueOrDefault("path")?.ToString() ?? ".";
        var include = arguments.TryGetValue("include", out var inc) ? inc?.ToString() : null;

        return Task.FromResult(Execute(pattern, searchPath, include));
    }

    private static string Execute(string pattern, string searchPath, string? include)
    {
        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(5));
        }
        catch (RegexParseException ex)
        {
            return $"无效的正则表达式：{ex.Message}";
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
