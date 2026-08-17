using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 目录树工具 —— 纯 C# 实现，生成 ASCII 目录结构。
/// 类似 Unix tree 命令的简化版。
/// </summary>
public class TreeTool : ITool
{
    public string Name => "tree";
    public string Description => "以树状图显示目录结构。可限制深度和最大条目数。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "起始目录路径（默认当前目录）"))
            .Set("depth", JNode.Object()
                .Set("type", "integer")
                .Set("description", "最大深度（默认 3）"))
            .Set("max", JNode.Object()
                .Set("type", "integer")
                .Set("description", "最大显示条目数（默认 100）")))
        .Set("required", JNode.Array());

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString();
        var depth = ToolArgs.GetInt(arguments, "depth", 3);
        var max = ToolArgs.GetInt(arguments, "max", 100);

        return Task.FromResult(Execute(path, depth, max));
    }

    private static string Execute(string? path, int maxDepth, int max)
    {
        try
        {
            path ??= BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory();
            path = Path.GetFullPath(path, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
            if (!Directory.Exists(path))
                return $"错误：目录不存在 — {path}";

            var sb = new StringBuilder();
            sb.AppendLine(path);
            var remaining = max;
            BuildTree(sb, path, "", maxDepth, ref remaining);
            if (remaining <= 0)
                sb.AppendLine("... (已达显示上限)");

            var result = sb.ToString();
            if (result.Length > 8000)
                result = ContextManager.TruncateByRunes(result, 6000) + "\n... (已截断) ...\n" + ContextManager.TruncateTailByRunes(result, 1000);
            return result.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"tree 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static void BuildTree(StringBuilder sb, string dir, string prefix,
        int maxDepth, ref int remaining)
    {
        if (maxDepth <= 0 || remaining <= 0) return;

        try
        {
            var entries = new List<string>();
            entries.AddRange(Directory.GetDirectories(dir));
            entries.AddRange(Directory.GetFiles(dir));
            entries.Sort(StringComparer.OrdinalIgnoreCase);
            // 先剔除隐藏项再算 isLast，否则目录末尾是隐藏项时最后一条可见项被误判为非最后（输出 ├── 而非 └──）
            entries.RemoveAll(e => Path.GetFileName(e).StartsWith('.'));

            for (int i = 0; i < entries.Count && remaining > 0; i++)
            {
                var isLast = i == entries.Count - 1;
                var name = Path.GetFileName(entries[i]);

                var connector = isLast ? "└── " : "├── ";
                var childPrefix = prefix + (isLast ? "    " : "│   ");

                var isDir = Directory.Exists(entries[i]);
                if (isDir)
                {
                    sb.AppendLine($"{prefix}{connector}📁 {name}/");
                    remaining--;
                    BuildTree(sb, entries[i], childPrefix, maxDepth - 1, ref remaining);
                }
                else
                {
                    var fi = new FileInfo(entries[i]);
                    sb.AppendLine($"{prefix}{connector}{name}  ({FormatSize(fi.Length)})");
                    remaining--;
                }
            }
        }
        catch { }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
