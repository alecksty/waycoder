using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文件详情工具 —— 纯 C# 实现。
/// 显示文件/目录的元数据：大小、时间、权限等。
/// </summary>
public class StatTool : ITool
{
    public string Name => "stat";
    public string Description => L.Pick("显示文件或目录的详细信息：大小、修改时间、创建时间、属性。纯 C# 实现。", "Show detailed information about a file or directory: size, modification time, creation time, and attributes. Pure C# implementation.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Param("string", L.Pick("文件或目录路径", "File or directory path"))))
        .Set("required", JNode.Array("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        return Task.FromResult(Execute(path));
    }

    private static string Execute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return L.Pick("错误：path 参数不能为空", "Error: the path parameter cannot be empty");

        try
        {
            var fullPath = CwdContext.Resolve(path); // cd 后相对路径基于被跟踪工作目录

            if (File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                var sb = new StringBuilder();
                sb.AppendLine(L.Pick($"📄 文件: {fi.FullName}", $"📄 File: {fi.FullName}"));
                sb.AppendLine(L.Pick($"  大小: {FormatUtil.FormatSize(fi.Length)} ({fi.Length:N0} bytes)", $"  Size: {FormatUtil.FormatSize(fi.Length)} ({fi.Length:N0} bytes)"));
                sb.AppendLine(L.Pick($"  创建: {fi.CreationTime:yyyy-MM-dd HH:mm:ss}", $"  Created: {fi.CreationTime:yyyy-MM-dd HH:mm:ss}"));
                sb.AppendLine(L.Pick($"  修改: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}", $"  Modified: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}"));
                sb.AppendLine(L.Pick($"  访问: {fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}", $"  Accessed: {fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}"));
                sb.AppendLine(L.Pick($"  属性: {(fi.Attributes == 0 ? "Normal" : fi.Attributes.ToString())}", $"  Attributes: {(fi.Attributes == 0 ? "Normal" : fi.Attributes.ToString())}"));
                sb.AppendLine(L.Pick($"  只读: {(fi.IsReadOnly ? "是" : "否")}", $"  Read-only: {(fi.IsReadOnly ? "yes" : "no")}"));
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
                sb.AppendLine(L.Pick($"📁 目录: {di.FullName}", $"📁 Directory: {di.FullName}"));
                sb.AppendLine(L.Pick($"  创建: {di.CreationTime:yyyy-MM-dd HH:mm:ss}", $"  Created: {di.CreationTime:yyyy-MM-dd HH:mm:ss}"));
                sb.AppendLine(L.Pick($"  修改: {di.LastWriteTime:yyyy-MM-dd HH:mm:ss}", $"  Modified: {di.LastWriteTime:yyyy-MM-dd HH:mm:ss}"));
                if (enumError != null)
                    sb.AppendLine(L.Pick($"  ⚠ 枚举失败（无权限？）: {enumError}", $"  ⚠ Enumeration failed (permission denied?): {enumError}"));
                else
                    sb.AppendLine(L.Pick($"  包含: {fileCount} 个文件, {dirCount} 个子目录", $"  Contains: {fileCount} files, {dirCount} subdirectories"));
                sb.AppendLine(L.Pick($"  属性: {(di.Attributes == 0 ? "Normal" : di.Attributes.ToString())}", $"  Attributes: {(di.Attributes == 0 ? "Normal" : di.Attributes.ToString())}"));
                return sb.ToString().TrimEnd();
            }

            return L.Pick($"错误：路径不存在 — {fullPath}", $"Error: path does not exist - {fullPath}");
        }
        catch (Exception ex)
        {
            return ToolErrors.Error("stat ", ex);
        }
    }

}
