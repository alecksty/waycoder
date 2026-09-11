namespace WayCoder.Tools;

/// <summary>
/// 删除文件/目录工具 —— 纯 C# 实现。
/// 支持递归删除目录，内置安全防护（禁止删除系统关键路径）。
/// </summary>
public class RmTool : ITool
{
    public string Name => "rm";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "删除文件或目录。支持递归删除。禁止删除系统关键路径（C:\\Windows、/etc 等）。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Param("string", "要删除的文件或目录路径"))
            .Set("recursive", JNode.Param("boolean", "是否递归删除目录（默认 false）")))
        .Set("required", JNode.Array("path"));

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
            var fullPath = CwdContext.Resolve(path); // cd 后相对路径基于被跟踪工作目录

            // 敏感路径 + 沙箱可写走统一守卫。此前只查敏感路径，**漏了 SandboxManager.CheckWritable**
            // ⇒ 项目写边界（移动端 / isProjectWrite）下 rm 能删到项目根之外，而 write_file/edit_file
            // 不能 —— 同一条边界轴规则执行率只有一半。文案由 PathSafety.Guard 统一产出，别再手拼。
            if (PathSafety.Guard(fullPath) is { } blocked) return blocked;

            // 安全检查
            foreach (var p in ProtectedPaths)
            {
                // 非 Windows 平台 GetFolderPath(Windows/System) 返回空串，
                // 空串会让 StartsWith("/") 误拦截所有绝对路径，须跳过。
                if (string.IsNullOrEmpty(p)) continue;
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
            return ToolErrors.ErrorOpPrefix("rm", ex);
        }
    }
}
