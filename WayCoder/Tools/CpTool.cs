namespace WayCoder.Tools;

/// <summary>
/// 文件复制工具 —— 纯 C# 实现。
/// 支持覆盖标志，自动创建目标目录。
/// </summary>
public class CpTool : ITool
{
    public string Name => "cp";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "复制文件或目录。自动创建父目录。纯 C# 实现，无 Shell 依赖。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("src", JNode.Object()
                .Set("type", "string")
                .Set("description", "源文件路径"))
            .Set("dest", JNode.Object()
                .Set("type", "string")
                .Set("description", "目标路径（文件或目录）"))
            .Set("overwrite", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "是否覆盖已存在的目标文件（默认 false）")))
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
            var srcPath = Path.GetFullPath(src, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory());
            var destPath = Path.GetFullPath(dest, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory());

            if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
                return $"错误：源不存在 — {srcPath}";

            // 如果 dest 是目录或以 / 结尾，则复制到目录内
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

            if (File.Exists(srcPath))
            {
                if (File.Exists(destPath) && !overwrite)
                    return $"⚠ 目标已存在，使用 overwrite=true 覆盖: {destPath}";
                File.Copy(srcPath, destPath, overwrite);
                return $"✔ 已复制: {srcPath} → {destPath}";
            }

            if (Directory.Exists(srcPath))
            {
                // 目标位于源目录内部（含自身）时复制会无限递归，直接拒绝
                var srcTrimmed = srcPath.TrimEnd(Path.DirectorySeparatorChar, '/');
                var srcPrefix = srcTrimmed + Path.DirectorySeparatorChar;
                if (destPath.Equals(srcTrimmed, StringComparison.OrdinalIgnoreCase)
                    || destPath.StartsWith(srcPrefix, StringComparison.OrdinalIgnoreCase))
                    return $"⚠ 无法复制：目标 '{destPath}' 位于源目录内部";

                // 递归复制目录
                CopyDirectory(srcPath, destPath, overwrite);
                return $"✔ 已复制目录: {srcPath} → {destPath}";
            }

            return $"错误：未知文件类型 — {srcPath}";
        }
        catch (Exception ex)
        {
            return $"错误：cp: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static void CopyDirectory(string srcDir, string destDir, bool overwrite, int depth = 0)
    {
        // 深度上限防符号链接环无限递归 → StackOverflow。抛错而非静默 return，避免产生不完整副本
        if (depth > 64) throw new IOException("目录层级过深（>64 层），已中止复制");
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(srcDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite);
        }
        foreach (var dir in Directory.GetDirectories(srcDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir, overwrite, depth + 1);
        }
    }
}
