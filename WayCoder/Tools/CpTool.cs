namespace WayCoder.Tools;

/// <summary>
/// 文件复制工具 —— 纯 C# 实现。
/// 支持覆盖标志，自动创建目标目录。
/// </summary>
public class CpTool : ITool
{
    public string Name => "cp";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => L.Pick("复制文件或目录。自动创建父目录。纯 C# 实现，无 Shell 依赖。", "Copy files or directories. Parent directories are created automatically. Pure C# implementation, no shell dependency.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("src", JNode.Param("string", L.Pick("源文件路径", "Source file path")))
            .Set("dest", JNode.Param("string", L.Pick("目标路径（文件或目录）", "Destination path (file or directory)")))
            .Set("overwrite", JNode.Param("boolean", L.Pick("是否覆盖已存在的目标文件（默认 false）", "Whether to overwrite an existing destination file. Default false."))))
        .Set("required", JNode.Array("src", "dest"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var src = arguments.GetValueOrDefault("src")?.ToString() ?? "";
        var dest = arguments.GetValueOrDefault("dest")?.ToString() ?? "";
        var overwrite = arguments.TryGetValue("overwrite", out var o) && o is bool ob && ob;

        if (string.IsNullOrWhiteSpace(src)) return Task.FromResult(L.Pick("错误：src 参数不能为空", "Error: the src parameter cannot be empty"));
        if (string.IsNullOrWhiteSpace(dest)) return Task.FromResult(L.Pick("错误：dest 参数不能为空", "Error: the dest parameter cannot be empty"));

        return Task.FromResult(Execute(src, dest, overwrite));
    }

    private static string Execute(string src, string dest, bool overwrite)
    {
        try
        {
            var srcPath = CwdContext.Resolve(src);
            var destPath = CwdContext.Resolve(dest);

            // src 只查敏感路径（**读**语义，与 read_file 一致：沙箱管的是「能写到哪」，
            // 从项目外读一份文件进来不该被项目写边界拦住）；
            // dest 走完整守卫（敏感 + 沙箱可写）—— 此前 dest 只查敏感，漏了沙箱 ⇒ 项目写边界下
            // cp 能把文件写到项目根之外。
            var srcSensitive = PathSafety.CheckSensitive(srcPath);
            if (srcSensitive != null)
                return L.Pick($"❌ 已阻止：{srcSensitive}（安全策略：敏感文件读写受保护）",
                              $"❌ Blocked: {srcSensitive} (security policy: sensitive files are protected)");
            if (PathSafety.Guard(destPath) is { } destBlocked) return destBlocked;

            if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
                return L.Pick($"错误：源不存在 — {srcPath}", $"Error: source does not exist — {srcPath}");

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
            Global.EnsureDir(destPath);

            if (File.Exists(srcPath))
            {
                if (File.Exists(destPath) && !overwrite)
                    return L.Pick($"⚠ 目标已存在，使用 overwrite=true 覆盖: {destPath}", $"⚠ Destination already exists; pass overwrite=true to replace it: {destPath}");
                File.Copy(srcPath, destPath, overwrite);
                return L.Pick($"✔ 已复制: {srcPath} → {destPath}", $"✔ Copied: {srcPath} → {destPath}");
            }

            if (Directory.Exists(srcPath))
            {
                // 目标位于源目录内部（含自身）时复制会无限递归，直接拒绝
                var srcTrimmed = srcPath.TrimEnd(Path.DirectorySeparatorChar, '/');
                var srcPrefix = srcTrimmed + Path.DirectorySeparatorChar;
                if (destPath.Equals(srcTrimmed, StringComparison.OrdinalIgnoreCase)
                    || destPath.StartsWith(srcPrefix, StringComparison.OrdinalIgnoreCase))
                    return L.Pick($"⚠ 无法复制：目标 '{destPath}' 位于源目录内部", $"⚠ Cannot copy: destination '{destPath}' is inside the source directory");

                // 递归复制目录
                FileOps.CopyDirectory(srcPath, destPath, overwrite);
                return L.Pick($"✔ 已复制目录: {srcPath} → {destPath}", $"✔ Copied directory: {srcPath} → {destPath}");
            }

            return L.Pick($"错误：未知文件类型 — {srcPath}", $"Error: unknown file type — {srcPath}");
        }
        catch (Exception ex)
        {
            return ToolErrors.ErrorOpPrefix("cp", ex);
        }
    }


}
