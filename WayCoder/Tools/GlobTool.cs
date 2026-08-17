namespace WayCoder.Tools;

/// <summary>
/// 文件模式匹配。
/// </summary>
public class GlobTool : ITool
{
    public string Name => "glob";
    public string Description => "查找匹配 glob 模式的文件。支持 ** 进行递归匹配（如 '**/*.py'）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("pattern", JNode.Object()
                .Set("type", "string")
                .Set("description", "Glob 模式，如 '**/*.py' 或 'src/**/*.ts'"))
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索目录（默认：当前工作目录）")))
        .Set("required", JNode.Array().Add("pattern"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var pattern = arguments.GetValueOrDefault("pattern")?.ToString() ?? "";
        var searchPath = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(searchPath))
            searchPath = ".";

        return Task.FromResult(Execute(pattern, searchPath));
    }

    private static string Execute(string pattern, string searchPath)
    {
        try
        {
            var basePath = Path.GetFullPath(searchPath, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
            if (!Directory.Exists(basePath))
                return $"错误：{searchPath} 不是目录";

            var files = MatchGlob(basePath, pattern);

            // 过滤被忽略的文件
            files = FileIgnoreManager.FilterIgnored(files, basePath);

            // 按修改时间排序，最新的在前
            files.Sort((a, b) =>
            {
                var ma = File.Exists(a) ? File.GetLastWriteTime(a) : DateTime.MinValue;
                var mb = File.Exists(b) ? File.GetLastWriteTime(b) : DateTime.MinValue;
                return mb.CompareTo(ma);
            });

            var total = files.Count;
            var shown = files.Take(100).ToList();

            var result = string.Join("\n", shown);

            if (total > 100)
                result += $"\n...（共 {total} 个匹配，仅显示前 100 个）";

            return string.IsNullOrEmpty(result) ? "没有匹配的文件。" : result;
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 在指定目录中匹配 glob 模式。
    /// 支持 ** 递归、* 通配符。
    /// </summary>
    private static List<string> MatchGlob(string basePath, string pattern)
    {
        var results = new List<string>();

        if (pattern.Contains("**"))
        {
            // 递归匹配
            var parts = pattern.Split("**", 2);
            var prefix = parts[0].TrimEnd('/', '\\');
            var suffix = parts.Length > 1 ? parts[1].TrimStart('/', '\\') : "";

            var searchRoot = string.IsNullOrEmpty(prefix) ? basePath : Path.Combine(basePath, prefix);

            if (Directory.Exists(searchRoot))
            {
                var allFiles = Directory.GetFiles(searchRoot, "*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    var relative = Path.GetRelativePath(searchRoot, file);
                    if (MatchSimple(relative, suffix))
                        results.Add(file);
                }
            }
        }
        else
        {
            // 非递归：使用基础目录 + 模式
            var dir = Path.GetDirectoryName(Path.Combine(basePath, pattern)) ?? basePath;
            var filePattern = Path.GetFileName(pattern);
            if (Directory.Exists(dir))
            {
                results.AddRange(Directory.GetFiles(dir, filePattern, SearchOption.TopDirectoryOnly));
            }
        }

        return results;
    }

    /// <summary>
    /// 简单的通配符匹配（支持 * 和 ?）。
    /// </summary>
    private static bool MatchSimple(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        // 将简单通配符转换为正则表达式
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
            + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // 在路径分隔符上标准化
        var normalized = input.Replace('\\', '/');
        return regex.IsMatch(normalized);
    }
}
