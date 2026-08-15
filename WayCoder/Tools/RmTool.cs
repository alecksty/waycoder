namespace WayCoder.Tools;

/// <summary>
/// 删除文件/目录工具 —— 纯 C# 实现。
/// 支持递归删除目录，内置安全防护（禁止删除系统关键路径）。
/// </summary>
public class RmTool : ITool
{
    public string Name => "rm";
    public string Description => "删除文件或目录。支持递归删除。禁止删除系统关键路径（C:\\Windows、/etc 等）。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要删除的文件或目录路径"))
            .Set("recursive", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "是否递归删除目录（默认 false）")))
        .Set("required", JNode.Array().Add("path"));

    // 系统关键路径（禁止删除）
    private static readonly HashSet<string> ProtectedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "C:\\Windows", "C:\\Windows\\System32", "C:\\",
        "/", "/etc", "/usr", "/bin", "/sbin", "/boot", "/sys", "/proc",
        "/System", "/Library", "/Applications",
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.GetFolderPath(Environment.SpecialFolder.System),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        var recursive = arguments.TryGetValue("recursive", out var r) && r is bool rb && rb;

        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        return Task.FromResult(Execute(path, recursive));
    }

    private static string Execute(string path, bool recursive)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            // 安全检查
            foreach (var p in ProtectedPaths)
            {
                if (fullPath.Equals(p, StringComparison.OrdinalIgnoreCase)
                    || fullPath.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return $"⚠ 已阻止：'{fullPath}' 位于受保护的系统路径中";
            }

            if (Directory.Exists(fullPath))
            {
                if (!recursive)
                {
                    var hasContent = Directory.GetFileSystemEntries(fullPath).Length > 0;
                    if (hasContent)
                        return $"⚠ 目录非空，请使用 recursive=true 确认递归删除: {fullPath}";
                }
                Directory.Delete(fullPath, recursive);
                return $"✔ 已删除目录: {fullPath}";
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return $"✔ 已删除文件: {fullPath}";
            }

            return $"错误：路径不存在 — {fullPath}";
        }
        catch (Exception ex)
        {
            return $"rm 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }
}
