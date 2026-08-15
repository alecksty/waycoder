using System.Text;
using WayCoder.Infra;
using WayCoder.UI;

namespace WayCoder.Tools;

/// <summary>
/// 增强版文件读取（对标 crush view 工具）。
///
/// 功能：
///   - 带行号的文件内容读取
///   - PDF 文本提取（PdfPig，支持分页）
///   - Markdown 结构化渲染（标题/代码块/表格/列表）
///   - 文件不存在时提供相似文件名建议（"Did you mean?"）
///   - UTF-8 验证
///   - 图片文件识别与提示
///   - 大文件保护（>100KB 截断）
///   - FileIgnoreManager 过滤
/// </summary>
public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "读取文件内容。支持代码文件（行号）、PDF（文本提取分页）、Office文档（docx/xlsx/pptx文本提取）、Markdown（结构化渲染）、CSV（表格）、HTML（标签剥离）。修改文件之前始终先读取它。";

    private const int MaxFileSize = 100 * 1024; // 100KB for text, PDF handles separately
    private const int DefaultLimit = 2000;
    private const int MaxLineLength = 2000;
    private const int PdfMaxPages = 20;

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件路径。支持 .cs .py .js .ts .md .pdf .html .json .txt 等。"))
            .Set("offset", JNode.Object()
                .Set("type", "integer")
                .Set("description", "起始行（从 1 开始）。PDF 文件此参数表示起始页码。默认 1。"))
            .Set("limit", JNode.Object()
                .Set("type", "integer")
                .Set("description", "最大读取行数。PDF 文件此参数表示最大页数（默认 20）。默认 2000。")))
        .Set("required", JNode.Array().Add("file_path"));

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
            if (string.IsNullOrWhiteSpace(filePath))
                return "错误：file_path 不能为空 — 请提供有效的文件路径。";

            var path = Path.GetFullPath(filePath);

            // 目录检查
            if (Directory.Exists(path))
                return $"错误：{filePath} 是目录，不是文件";

            // 文件不存在 → "Did you mean?" 建议
            if (!File.Exists(path))
                return FileNotFoundMessage(filePath, path);

            var ext = Path.GetExtension(path).ToLowerInvariant();

            // ── PDF 文件 ──
            if (ext == ".pdf")
                return ReadPdfFile(path, offset, limit);

            // ── Office 文档 ──
            if (ext == ".docx")
                return ReadOfficeDoc(path, "DOCX", OfficeExtractor.ExtractDocx(path));
            if (ext == ".xlsx")
                return ReadOfficeDoc(path, "XLSX", OfficeExtractor.ExtractXlsx(path));
            if (ext == ".pptx")
                return ReadOfficeDoc(path, "PPTX", OfficeExtractor.ExtractPptx(path));

            // ── Markdown 文件 ──
            if (ext == ".md" || ext == ".markdown")
                return ReadMarkdownFile(path, offset, limit);

            // ── CSV 文件 ──
            if (ext == ".csv")
                return ReadCsvFile(path);

            // ── HTML 文件 ──
            if (ext is ".html" or ".htm")
                return ReadHtmlFile(path);

            // ── 图片文件 ──
            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".ico" or ".svg")
            {
                var info = new FileInfo(path);
                return $"📷 这是一个图片文件: {filePath}\n大小: {FormatSize(info.Length)}\n格式: {ext.TrimStart('.')}\n\n💡 提示：使用 view 查看或 download 下载。";
            }

            // ── 普通文本文件 ──
            return ReadTextFile(path, filePath, offset, limit);
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    // ════════════════════════════════════════════════════════════
    // PDF 文件读取
    // ════════════════════════════════════════════════════════════
    private static string ReadPdfFile(string path, int startPage, int pageLimit)
    {
        var info = new FileInfo(path);
        // PDF 文件不再受 100KB 限制
        if (info.Length > 50 * 1024 * 1024)
            return $"⚠ PDF 文件过大: {FormatSize(info.Length)}（最大 50 MB）";

        pageLimit = Math.Min(pageLimit, PdfMaxPages);
        var result = PdfExtractor.Extract(path, startPage, pageLimit);

        // PDF 文件追踪
        FileTracker.RecordRead(path);

        return result.ToMarkdown();
    }

    // ════════════════════════════════════════════════════════════
    // Markdown 文件读取（结构化渲染）
    // ════════════════════════════════════════════════════════════
    private static string ReadMarkdownFile(string path, int offset, int limit)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSize * 5) // Markdown 给 500KB
            return $"⚠ 文件过大: {FormatSize(fileInfo.Length)}（最大 {FormatSize(MaxFileSize * 5)}）";

        try { _ = Encoding.UTF8.GetString(File.ReadAllBytes(path)); }
        catch { return $"错误：{path} 不是 UTF-8 文本文件"; }

        // 文件追踪
        FileTracker.RecordRead(path);

        var text = File.ReadAllText(path, Encoding.UTF8);

        // 按行 limit 限制（只渲染需要的部分）
        var allLines = text.Split('\n');
        var start = Math.Max(0, offset - 1);
        var chunk = allLines.Skip(start).Take(limit).ToArray();
        var chunkText = string.Join("\n", chunk);

        // 使用 MarkdownParser 解析
        var nodes = MarkdownParser.Parse(chunkText);

        if (nodes.Count == 0)
        {
            // 回退到纯文本格式（无 markdown 结构）
            return FormatAsTextFile(chunk, start, allLines.Length);
        }

        var sb = new StringBuilder();
        sb.AppendLine("<markdown>");
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 行 {offset}-{offset + chunk.Length - 1} / {allLines.Length}");
        sb.AppendLine();

        foreach (var node in nodes)
        {
            switch (node)
            {
                case MdHeading h:
                    sb.AppendLine(new string('#', h.Level) + " " + h.Text);
                    break;
                case MdCodeBlock cb:
                    sb.AppendLine($"```{cb.Language}");
                    // 代码块限制行数
                    var codeLines = cb.Code.Split('\n');
                    if (codeLines.Length > 80)
                    {
                        sb.AppendLine(string.Join("\n", codeLines.Take(80)));
                        sb.AppendLine($"... (省略 {codeLines.Length - 80} 行)");
                    }
                    else
                        sb.AppendLine(cb.Code.TrimEnd());
                    sb.AppendLine("```");
                    break;
                case MdTable t:
                    sb.Append("| " + string.Join(" | ", t.Headers) + " |");
                    sb.AppendLine();
                    sb.Append("|" + string.Join("|", t.Headers.Select(_ => "---")) + "|");
                    sb.AppendLine();
                    foreach (var row in t.Rows.Take(30))
                    {
                        sb.Append("| " + string.Join(" | ", row) + " |");
                        sb.AppendLine();
                    }
                    if (t.Rows.Count > 30)
                        sb.AppendLine($"... (省略 {t.Rows.Count - 30} 行)");
                    break;
                case MdListItem li:
                    var prefix = li.Ordered ? $"{li.OrderNum}. " : "- ";
                    sb.AppendLine(prefix + li.Text);
                    break;
                case MdParagraph p:
                    if (!string.IsNullOrWhiteSpace(p.Text))
                        sb.AppendLine(p.Text);
                    break;
                default:
                    sb.AppendLine(node.ToString() ?? "");
                    break;
            }
        }

        var hasMore = allLines.Length > start + limit;
        if (hasMore)
            sb.AppendLine($"\n(文件还有更多行。使用 offset={start + chunk.Length + 1} 读取后续内容)");

        sb.Append("</markdown>");

        // 附加缓存的 LSP 诊断信息
        var diag = DiagnosticManager.FormatForLLM(path);
        if (diag != null)
        {
            sb.AppendLine();
            sb.Append(diag);
        }

        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // 普通文本文件读取
    // ════════════════════════════════════════════════════════════
    private static string ReadTextFile(string path, string filePath, int offset, int limit)
    {
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

        // 文件追踪
        FileTracker.RecordRead(path);

        var start = Math.Max(0, offset - 1);
        var chunk = lines.Skip(start).Take(limit).ToArray();

        var result = FormatAsTextFile(chunk, start, total);

        // 附加缓存的 LSP 诊断信息
        var diag = DiagnosticManager.FormatForLLM(path);
        if (diag != null)
            result += "\n" + diag;

        return result;
    }

    private static string FormatAsTextFile(string[] chunk, int start, int total)
    {
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

        var hasMore = total > start + chunk.Length;
        if (hasMore)
        {
            sb.AppendLine();
            sb.AppendLine($"(文件还有更多行。使用 offset={start + chunk.Length + 1} 读取后续内容)");
        }

        sb.Append("</file>");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // Office 文档读取
    // ════════════════════════════════════════════════════════════
    private static string ReadOfficeDoc(string path, string format, string content)
    {
        var info = new FileInfo(path);
        FileTracker.RecordRead(path);

        if (content.StartsWith("错误") || content.StartsWith(format))
            return content;

        var sb = new StringBuilder();
        sb.AppendLine($"<{format.ToLower()}>");
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 大小: {FormatSize(info.Length)} | 格式: {format}");
        sb.AppendLine();
        sb.Append(content);
        sb.AppendLine();
        sb.Append($"</{format.ToLower()}>");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // CSV 文件读取（表格格式）
    // ════════════════════════════════════════════════════════════
    private static string ReadCsvFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileSize * 5)
            return $"⚠ 文件过大: {FormatSize(info.Length)}（最大 {FormatSize(MaxFileSize * 5)}）";

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {path}"; }

        try { _ = Encoding.UTF8.GetString(raw); }
        catch { return $"错误：{path} 不是 UTF-8 文本文件"; }

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);
        var table = OfficeExtractor.ParseCsv(text);

        var sb = new StringBuilder();
        sb.AppendLine("<csv>");
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 大小: {FormatSize(info.Length)}");
        sb.AppendLine();
        sb.Append(table);
        sb.Append("</csv>");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // HTML 文件读取（去除标签，保留结构）
    // ════════════════════════════════════════════════════════════
    private static string ReadHtmlFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileSize * 5)
            return $"⚠ 文件过大: {FormatSize(info.Length)}（最大 {FormatSize(MaxFileSize * 5)}）";

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {path}"; }

        try { _ = Encoding.UTF8.GetString(raw); }
        catch { return $"错误：{path} 不是 UTF-8 文本文件"; }

        FileTracker.RecordRead(path);
        var html = File.ReadAllText(path, Encoding.UTF8);

        // 提取 title
        string? title = null;
        var titleMatch = System.Text.RegularExpressions.Regex.Match(html,
            @"<title[^>]*>(.*?)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (titleMatch.Success)
            title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());

        // 移除 script, style, head
        html = System.Text.RegularExpressions.Regex.Replace(html,
            @"<(script|style|head|nav|footer|header|aside|noscript|iframe|svg)[^>]*>.*?</\1>",
            "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        // 解码常见实体
        html = System.Net.WebUtility.HtmlDecode(html);

        // 标签 → 结构化
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"</?(p|div|tr|h[1-6]|li|section|article)[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", ""); // 移除所有标签
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\n{3,}", "\n\n"); // 压缩多空行
        html = html.Trim();

        if (html.Length > 10_000)
            html = html[..10_000] + $"\n...(截断于 10,000 字符，原始 {info.Length:N0} 字节)";

        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        if (title != null)
            sb.AppendLine($"# {title}");
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 大小: {FormatSize(info.Length)}");
        sb.AppendLine();
        sb.Append(html);
        sb.AppendLine();
        sb.Append("</html>");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // 文件不存在建议
    // ════════════════════════════════════════════════════════════

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
