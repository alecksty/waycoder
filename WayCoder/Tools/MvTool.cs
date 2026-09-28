namespace WayCoder.Tools;

/// <summary>
/// 文件移动/重命名工具 —— 纯 C# 实现。
/// 跨驱动器移动自动回退为复制+删除。
/// </summary>
public class MvTool : ITool
{
    public string Name => "mv";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => L.Pick("移动或重命名文件/目录。自动创建目标父目录，支持跨驱动器。纯 C# 实现。", "Move or rename a file/directory. Automatically creates the destination parent directory; supports cross-drive moves. Pure C# implementation.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("src", JNode.Param("string", L.Pick("源文件或目录路径", "Source file or directory path.")))
            .Set("dest", JNode.Param("string", L.Pick("目标路径", "Destination path.")))
            .Set("overwrite", JNode.Param("boolean", L.Pick("是否覆盖已存在的目标（默认 false）", "Whether to overwrite an existing destination. Default false."))))
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

            // 两端都走完整守卫：mv 对 src 是**删除**（源文件被移走）、对 dest 是写入，
            // 两处都要过沙箱。此前两端都只查敏感路径，漏了 SandboxManager.CheckWritable
            // ⇒ 项目写边界下 mv 能把文件搬到项目根之外。
            if (PathSafety.Guard(srcPath) is { } srcBlocked) return srcBlocked;
            if (PathSafety.Guard(destPath) is { } destBlocked) return destBlocked;

            if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
                return L.Pick($"错误：源不存在 — {srcPath}", $"Error: source does not exist — {srcPath}");

            // 如果 dest 是目录，则移动到目录内
            if (dest.EndsWith(Path.DirectorySeparatorChar) || dest.EndsWith('/')
                || (Directory.Exists(destPath) && !File.Exists(destPath)))
            {
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);
                var name = Path.GetFileName(srcPath);
                destPath = Path.Combine(destPath, name);
            }

            // 源与目标相同（如 mv file.txt . 且 . 恰是 file.txt 所在目录）：overwrite=true 会删掉源文件，需提前拦截
            if (string.Equals(srcPath, destPath, StringComparison.OrdinalIgnoreCase))
                return L.Pick($"⚠ 源与目标相同: {srcPath}", $"⚠ Source and destination are the same: {srcPath}");

            // 确保目标目录存在
            Global.EnsureDir(destPath);

            // 目标已存在
            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                if (!overwrite)
                    return L.Pick($"⚠ 目标已存在，使用 overwrite=true 覆盖: {destPath}", $"⚠ Destination already exists; pass overwrite=true to replace it: {destPath}");
                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);
                else
                    File.Delete(destPath);
            }

            if (File.Exists(srcPath))
            {
                File.Move(srcPath, destPath);
                return L.Pick($"✔ 已移动: {srcPath} → {destPath}", $"✔ Moved: {srcPath} → {destPath}");
            }

            if (Directory.Exists(srcPath))
            {
                // 目标位于源目录内部（含自身）时移动会无限递归，直接拒绝
                var srcTrimmed = srcPath.TrimEnd(Path.DirectorySeparatorChar, '/');
                var srcPrefix = srcTrimmed + Path.DirectorySeparatorChar;
                if (destPath.Equals(srcTrimmed, StringComparison.OrdinalIgnoreCase)
                    || destPath.StartsWith(srcPrefix, StringComparison.OrdinalIgnoreCase))
                    return L.Pick($"⚠ 无法移动：目标 '{destPath}' 位于源目录内部", $"⚠ Cannot move: destination '{destPath}' is inside the source directory");

                // 尝试直接移动
                try
                {
                    Directory.Move(srcPath, destPath);
                }
                catch (IOException)
                {
                    // 跨驱动器 → 复制后删除
                    FileOps.CopyDirectory(srcPath, destPath, overwrite: false);
                    Directory.Delete(srcPath, true);
                }
                return L.Pick($"✔ 已移动目录: {srcPath} → {destPath}", $"✔ Moved directory: {srcPath} → {destPath}");
            }

            return L.Pick($"错误：未知路径类型 — {srcPath}", $"Error: unknown path type — {srcPath}");
        }
        catch (Exception ex)
        {
            return ToolErrors.ErrorOpPrefix("mv", ex);
        }
    }


}
