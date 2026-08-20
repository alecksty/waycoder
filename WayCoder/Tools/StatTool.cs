using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文件详情工具 —— 纯 C# 实现。
/// 显示文件/目录的元数据：大小、时间、权限等。
/// </summary>
public class StatTool : ITool
{
    public string Name => "stat";
    public string Description => "显示文件或目录的详细信息：大小、修改时间、创建时间、属性。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件或目录路径")))
        .Set("required", JNode.Array().Add("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        return Task.FromResult(Execute(path));
    }

    private static string Execute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "错误：path 参数不能为空";

        try
        {
            var fullPath = Path.GetFullPath(path, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

            if (File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                var sb = new StringBuilder();
                sb.AppendLine($"📄 文件: {fi.FullName}");
                sb.AppendLine($"  大小: {FormatSize(fi.Length)} ({fi.Length:N0} bytes)");
                sb.AppendLine($"  创建: {fi.CreationTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  修改: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  访问: {fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  属性: {(fi.Attributes == 0 ? "Normal" : fi.Attributes.ToString())}");
                sb.AppendLine($"  只读: {(fi.IsReadOnly ? "是" : "否")}");
                return sb.ToString().TrimEnd();
            }

            if (Directory.Exists(fullPath))
            {
                var di = new DirectoryInfo(fullPath);
                var fileCount = 0;
                var dirCount = 0;
                string? enumError = null;
                try
                {
                    fileCount = Directory.GetFiles(fullPath).Length;
                    dirCount = Directory.GetDirectories(fullPath).Length;
                }
                catch (Exception ex) { enumError = ex.Message; } // 权限问题不能误报「0 个文件」

                var sb = new StringBuilder();
                sb.AppendLine($"📁 目录: {di.FullName}");
                sb.AppendLine($"  创建: {di.CreationTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"  修改: {di.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                if (enumError != null)
                    sb.AppendLine($"  ⚠ 枚举失败（无权限？）: {enumError}");
                else
                    sb.AppendLine($"  包含: {fileCount} 个文件, {dirCount} 个子目录");
                sb.AppendLine($"  属性: {(di.Attributes == 0 ? "Normal" : di.Attributes.ToString())}");
                return sb.ToString().TrimEnd();
            }

            return $"错误：路径不存在 — {fullPath}";
        }
        catch (Exception ex)
        {
            return $"stat 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };
}
