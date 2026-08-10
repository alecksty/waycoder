using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 带行号的文件读取。
/// </summary>
public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "读取文件内容并显示行号。修改文件之前始终先读取它。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["file_path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "文件路径",
            },
            ["offset"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "起始行（从 1 开始）。默认 1。",
            },
            ["limit"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "最大读取行数。默认 2000。",
            },
        },
        ["required"] = new JsonArray("file_path"),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var offset = arguments.TryGetValue("offset", out var o) && o is int oi ? oi : 1;
        var limit = arguments.TryGetValue("limit", out var l) && l is int li ? li : 2000;

        return Task.FromResult(Execute(filePath, offset, limit));
    }

    private static string Execute(string filePath, int offset, int limit)
    {
        try
        {
            var path = Path.GetFullPath(filePath);
            if (!File.Exists(path))
                return $"错误：{filePath} 未找到";
            if (Directory.Exists(path))
                return $"错误：{filePath} 是目录，不是文件";

            var text = File.ReadAllText(path, Encoding.UTF8);
            var lines = text.Split('\n');
            var total = lines.Length;

            var start = Math.Max(0, offset - 1);
            var chunk = lines.Skip(start).Take(limit).ToArray();

            var sb = new StringBuilder();
            for (int i = 0; i < chunk.Length; i++)
            {
                sb.AppendLine($"{start + i + 1}\t{chunk[i].TrimEnd('\r')}");
            }

            if (total > start + limit)
            {
                sb.AppendLine($"...（共 {total} 行，显示第 {start + 1}-{start + chunk.Length} 行）");
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "（空文件）";
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }
}
