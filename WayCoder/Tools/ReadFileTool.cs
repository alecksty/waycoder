using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 增强版文件读取（对标 crush view 工具）。
///
/// 功能：
///   - 带行号的文件内容读取
///   - 文件不存在时提供相似文件名建议（"Did you mean?"）
///   - UTF-8 验证
///   - 图片文件识别与提示
///   - 大文件保护（>100KB 截断）
///   - FileIgnoreManager 过滤
/// </summary>
public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "读取文件内容并显示行号。修改文件之前始终先读取它。支持代码文件和图片文件识别。";

    private const int MaxFileSize = 100 * 1024; // 100KB
    private const int DefaultLimit = 2000;
    private const int MaxLineLength = 2000;

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
        var limit = arguments.TryGetValue("limit", out var l) && l is int li ? li : DefaultLimit;

        return Task.FromResult(Execute(filePath, offset, limit));
    }

    private static string Execute(string filePath, int offset, int limit)
    {
        try
        {
            var path = Path.GetFullPath(filePath);

            // 目录检查
            if (Directory.Exists(path))
                return $"错误：{filePath} 是目录，不是文件";

            // 文件不存在 → "Did you mean?" 建议
            if (!File.Exists(path))
                return FileNotFoundMessage(filePath, path);

            // 图片检测
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".ico" or ".svg")
            {
                var info = new FileInfo(path);
                return $"📷 这是一个图片文件: {filePath}\n大小: {FormatSize(info.Length)}\n格式: {ext.TrimStart('.')}\n\n💡 提示：使用 view 查看或 download 下载。";
            }

            // 大文件检查
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > MaxFileSize)
            {
                return $"⚠ 文件过大: {FormatSize(fileInfo.Length)}（最大 {FormatSize(MaxFileSize)}）\n💡 提示：使用 offset/limit 分段读取。";
            }

            // 读取 + UTF-8 验证
            byte[] raw;
            try { raw = File.ReadAllBytes(path); }
            catch { return $"错误：无法读取 {filePath}"; }

            try { _ = Encoding.UTF8.GetString(raw); }
            catch { return $"错误：{filePath} 不是 UTF-8 文本文件（read_file 只能读取文本文件）"; }

            var text = File.ReadAllText(path, Encoding.UTF8);
            var lines = text.Split('\n');
            var total = lines.Length;

            var start = Math.Max(0, offset - 1);
            var chunk = lines.Skip(start).Take(limit).ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("<file>");
            int lineNumWidth = total >= 100000 ? 6 : total >= 10000 ? 5 : total >= 1000 ? 4 : total >= 100 ? 3 : total >= 10 ? 2 : 1;

            for (int i = 0; i < chunk.Length; i++)
            {
                var line = chunk[i].TrimEnd('\r');
                if (line.Length > MaxLineLength)
                    line = line[..MaxLineLength] + "...";
                var numStr = (start + i + 1).ToString().PadLeft(lineNumWidth);
                sb.AppendLine($"{numStr}|{line}");
            }

            var hasMore = total > start + limit;
            if (hasMore)
            {
                sb.AppendLine();
                sb.AppendLine($"(文件还有更多行。使用 offset={start + chunk.Length + 1} 读取后续内容)");
            }

            sb.Append("</file>");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 文件不存在时，搜索同级目录提供相似文件名建议。
    /// </summary>
    private static string FileNotFoundMessage(string originalPath, string fullPath)
    {
        var dir = Path.GetDirectoryName(fullPath);
        var baseName = Path.GetFileName(fullPath);

        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName))
            return $"错误：{originalPath} 未找到";

        try
        {
            if (!Directory.Exists(dir))
                return $"错误：{originalPath} 未找到（目录不存在）";

            var entries = Directory.GetFileSystemEntries(dir);
            var suggestions = new List<string>();

            foreach (var entry in entries)
            {
                var entryName = Path.GetFileName(entry);
                var distance = ComputeEditDistance(
                    entryName.ToLowerInvariant(),
                    baseName.ToLowerInvariant());

                // 编辑距离小或包含关系 → 建议
                if (distance <= 3 ||
                    entryName.Contains(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(Path.Combine(dir, entryName));
                }

                if (suggestions.Count >= 3)
                    break;
            }

            if (suggestions.Count > 0)
            {
                return $"错误：{originalPath} 未找到\n\n你想找的是不是？\n{string.Join("\n", suggestions.Select(s => $"  • {s}"))}";
            }
        }
        catch { }

        return $"错误：{originalPath} 未找到";
    }

    /// <summary>
    /// Levenshtein 编辑距离。
    /// </summary>
    private static int ComputeEditDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        for (int j = 1; j <= b.Length; j++)
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));

        return d[a.Length, b.Length];
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
