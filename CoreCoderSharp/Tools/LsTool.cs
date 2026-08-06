using System.Text;

namespace CoreCoderSharp.Tools;

/// <summary>
/// 目录列表工具 —— 纯 C# 实现，无 Shell 依赖。
/// 列出文件和子目录，含大小、修改时间。
/// 优势：可控输出大小、超时、无转义问题。
/// </summary>
public class LsTool : ITool
{
    public string Name => "ls";
    public string Description => "列出目录中的文件和子目录。支持通配符过滤、递归深度限制、最大条目数。纯 C# 实现，无需 Shell。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "目录路径（默认：当前工作目录）",
            },
            ["pattern"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "文件名通配符过滤，如 '*.cs'、'*.{md,txt}'",
            },
            ["max"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "最大显示条目数（默认 100，防止输出爆炸）",
            },
            ["depth"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "递归深度（1=仅当前目录，默认 1）",
            },
            ["long"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "是否显示详细信息（大小、时间，默认 false）",
            },
        },
        ["required"] = new JsonArray(),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString();
        var pattern = arguments.GetValueOrDefault("pattern")?.ToString() ?? "*";
        var max = arguments.TryGetValue("max", out var m) && m is int mi ? mi : 100;
        var depth = arguments.TryGetValue("depth", out var d) && d is int di ? di : 1;
        var longFormat = arguments.TryGetValue("long", out var l) && l is bool lb && lb;

        return Task.FromResult(Execute(path, pattern, max, depth, longFormat));
    }

    private static string Execute(string? path, string pattern, int max, int depth, bool longFormat)
    {
        try
        {
            path ??= BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory();
            path = Path.GetFullPath(path);

            if (!Directory.Exists(path))
                return $"错误：目录不存在 — {path}";

            var sb = new StringBuilder();
            sb.AppendLine(path);

            ListDir(sb, path, pattern, depth, 1, ref max, longFormat);

            if (max <= 0)
                sb.AppendLine("... (已达显示上限)");

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"ls 错误：{ex.Message}";
        }
    }

    private static void ListDir(StringBuilder sb, string dir, string pattern,
        int maxDepth, int currentDepth, ref int remaining, bool longFormat)
    {
        if (remaining <= 0) return;

        try
        {
            // 子目录
            var dirs = Directory.GetDirectories(dir);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            foreach (var d in dirs)
            {
                if (remaining <= 0) break;
                var name = Path.GetFileName(d);
                if (name.StartsWith('.')) continue; // 跳过隐藏目录

                var indent = new string(' ', currentDepth * 2);
                if (longFormat)
                {
                    var di = new DirectoryInfo(d);
                    sb.AppendLine($"{indent}📁 {name}/  [{di.LastWriteTime:yyyy-MM-dd HH:mm}]");
                }
                else
                    sb.AppendLine($"{indent}📁 {name}/");

                remaining--;
                if (currentDepth < maxDepth)
                    ListDir(sb, d, pattern, maxDepth, currentDepth + 1, ref remaining, longFormat);
            }

            // 文件
            var files = Directory.GetFiles(dir, pattern);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                if (remaining <= 0) break;
                var name = Path.GetFileName(f);
                if (name.StartsWith('.')) continue;

                var indent = new string(' ', currentDepth * 2);
                if (longFormat)
                {
                    var fi = new FileInfo(f);
                    sb.AppendLine($"{indent}  {name}  {FormatSize(fi.Length),8}  [{fi.LastWriteTime:yyyy-MM-dd HH:mm}]");
                }
                else
                    sb.AppendLine($"{indent}  {name}");

                remaining--;
            }
        }
        catch { /* 权限不足等，静默跳过 */ }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };
}
