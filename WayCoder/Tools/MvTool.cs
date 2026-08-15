namespace WayCoder.Tools;

/// <summary>
/// 文件移动/重命名工具 —— 纯 C# 实现。
/// 跨驱动器移动自动回退为复制+删除。
/// </summary>
public class MvTool : ITool
{
    public string Name => "mv";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "移动或重命名文件/目录。自动创建目标父目录，支持跨驱动器。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("src", JNode.Object()
                .Set("type", "string")
                .Set("description", "源文件或目录路径"))
            .Set("dest", JNode.Object()
                .Set("type", "string")
                .Set("description", "目标路径"))
            .Set("overwrite", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "是否覆盖已存在的目标（默认 false）")))
        .Set("required", JNode.Array().Add("src").Add("dest"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var src = arguments.GetValueOrDefault("src")?.ToString() ?? "";
        var dest = arguments.GetValueOrDefault("dest")?.ToString() ?? "";
        var overwrite = arguments.TryGetValue("overwrite", out var o) && o is bool ob && ob;

        if (string.IsNullOrWhiteSpace(src)) return Task.FromResult("错误：src 参数不能为空");
        if (string.IsNullOrWhiteSpace(dest)) return Task.FromResult("错误：dest 参数不能为空");

        return Task.FromResult(Execute(src, dest, overwrite));
    }

    private static string Execute(string src, string dest, bool overwrite)
    {
        try
        {
            var srcPath = Path.GetFullPath(src);
            var destPath = Path.GetFullPath(dest);

            if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
                return $"错误：源不存在 — {srcPath}";

            // 如果 dest 是目录，则移动到目录内
            if (dest.EndsWith(Path.DirectorySeparatorChar) || dest.EndsWith('/')
                || (Directory.Exists(destPath) && !File.Exists(destPath)))
            {
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);
                var name = Path.GetFileName(srcPath);
                destPath = Path.Combine(destPath, name);
            }

            // 确保目标目录存在
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            // 目标已存在
            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                if (!overwrite)
                    return $"⚠ 目标已存在，使用 overwrite=true 覆盖: {destPath}";
                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);
                else
                    File.Delete(destPath);
            }

            if (File.Exists(srcPath))
            {
                File.Move(srcPath, destPath);
                return $"✔ 已移动: {srcPath} → {destPath}";
            }

            if (Directory.Exists(srcPath))
            {
                // 尝试直接移动
                try
                {
                    Directory.Move(srcPath, destPath);
                }
                catch (IOException)
                {
                    // 跨驱动器 → 复制后删除
                    CopyDirectory(srcPath, destPath);
                    Directory.Delete(srcPath, true);
                }
                return $"✔ 已移动目录: {srcPath} → {destPath}";
            }

            return $"错误：未知路径类型 — {srcPath}";
        }
        catch (Exception ex)
        {
            return $"mv 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static void CopyDirectory(string srcDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(srcDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(srcDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }
}
