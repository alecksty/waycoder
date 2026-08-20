using System.Text;
using WayCoder.Infra;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Edit;

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
    public string Description => "读取文件内容。支持代码文件（行号）、PDF（文本提取分页）、Office文档（docx/xlsx/pptx 及老式 doc/xls/ppt、WPS 的 wps/et/dps 文本提取）、Markdown（结构化渲染）、CSV（表格）、HTML（标签剥离）、JSON（美化）、INI（结构化）、tail 读取末尾 N 行。修改文件之前始终先读取它。";

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
                .Set("description", "最大读取行数。PDF 文件此参数表示最大页数（默认 20）。默认 2000。"))
            .Set("tail", JNode.Object()
                .Set("type", "integer")
                .Set("description", "读取文件末尾 N 行（与 offset/limit 互斥，优先于 offset）。适合查看日志/大文件末尾。默认 0 禁用。")))
        .Set("required", JNode.Array().Add("file_path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var offset = ToolArgs.GetInt(arguments, "offset", 1);
        var limit = Math.Max(1, ToolArgs.GetInt(arguments, "limit", DefaultLimit));
        var tail = Math.Max(0, ToolArgs.GetInt(arguments, "tail", 0));

        return Task.FromResult(Execute(filePath, offset, limit, tail));
    }

    private static string Execute(string filePath, int offset, int limit, int tail)
    {
        var result = ResolveExecute(filePath, offset, limit, tail);
        // 提示注入防护：读取内容含「忽略之前指令/你现在是…」等注入模式时附加警告
        return result + (WayCoder.Infra.PromptInjection.WarningIfInjected(result, $"文件 {filePath}") ?? "");
    }

    private static string ResolveExecute(string filePath, int offset, int limit, int tail)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "错误：file_path 不能为空 — 请提供有效的文件路径。";

            var path = Path.GetFullPath(filePath, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

            // 敏感路径防护（SSH 密钥/云凭据/系统凭据，防提示注入读泄露）
            var sensitive = PathSafety.CheckSensitive(path);
            if (sensitive != null)
                return $"❌ 已阻止：{sensitive}（安全策略：敏感文件读写受保护）";

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

            // ── Office 文档（OOXML）──
            if (ext == ".docx")
                return ReadOfficeDoc(path, "DOCX", OfficeExtractor.ExtractDocx(path));
            if (ext == ".xlsx")
                return ReadOfficeDoc(path, "XLSX", OfficeExtractor.ExtractXlsx(path));
            if (ext == ".pptx")
                return ReadOfficeDoc(path, "PPTX", OfficeExtractor.ExtractPptx(path));

            // ── 老式二进制 Office / WPS（.doc/.xls/.ppt/.wps/.et/.dps）──
            // 扩展名不可靠，LegacyOffice 按文件头魔数识别 CFB/ZIP/RTF/HTML/纯文本并路由。
            if (ext is ".doc" or ".wps")
                return ReadOfficeDoc(path, "DOC", LegacyOffice.Extract(path));
            if (ext is ".xls" or ".et")
                return ReadOfficeDoc(path, "XLS", LegacyOffice.Extract(path));
            if (ext is ".ppt" or ".dps")
                return ReadOfficeDoc(path, "PPT", LegacyOffice.Extract(path));

            // ── Markdown 文件 ──
            if (ext == ".md" || ext == ".markdown")
                return ReadMarkdownFile(path, offset, limit, tail);

            // ── CSV 文件 ──
            if (ext == ".csv")
                return ReadCsvFile(path);

            // ── JSON 文件（结构化美化）──
            if (ext == ".json")
                return ReadJsonFile(path);

            // ── INI 配置文件（结构化）──
            if (ext is ".ini" or ".cfg" or ".conf")
                return ReadIniFile(path);

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
            return ReadTextFile(path, filePath, offset, limit, tail);
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
    private static string ReadMarkdownFile(string path, int offset, int limit, int tail)
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
        // 末尾换行产生空元素（"a\nb\n" → 3 元素实际 2 行），去掉避免行数虚增（对齐 ReadTextFile）
        if (allLines.Length > 1 && allLines[^1].Length == 0)
            allLines = allLines[..^1];
        int start;
        string[] chunk;
        if (tail > 0)
        {
            start = Math.Max(0, allLines.Length - tail);
            chunk = allLines.Skip(start).ToArray();
        }
        else
        {
            start = Math.Max(0, offset - 1);
            chunk = allLines.Skip(start).Take(limit).ToArray();
        }
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
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 行 {start + 1}-{start + chunk.Length} / {allLines.Length}");
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

        var hasMore = allLines.Length > start + chunk.Length;
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
    private static string ReadTextFile(string path, string filePath, int offset, int limit, int tail)
    {
        // 大文件检查
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSize)
        {
            return $"⚠ 文件过大: {FormatSize(fileInfo.Length)}（最大 {FormatSize(MaxFileSize)}）\n💡 提示：使用 offset/limit 分段读取。";
        }

        // 读取 + 二进制/UTF-8 验证
        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {filePath}"; }

        if (IsBinaryContent(raw))
            return $"错误：{filePath} 是二进制文件（检测到 NUL 字节），read_file 只能读取文本文件";

        try { _ = new UTF8Encoding(false, true).GetString(raw); }
        catch { return $"错误：{filePath} 不是 UTF-8 文本文件（read_file 只能读取文本文件）"; }

        var text = File.ReadAllText(path, Encoding.UTF8);
        var lines = text.Split('\n');
        // 末尾换行会产生一个空元素（"a\nb\n" → 3 元素，实际 2 行），去掉避免行数虚增
        if (lines.Length > 1 && lines[^1].Length == 0)
            lines = lines[..^1];
        var total = lines.Length;

        // 文件追踪
        FileTracker.RecordRead(path);

        int start;
        string[] chunk;
        if (tail > 0)
        {
            start = Math.Max(0, total - tail);
            chunk = lines.Skip(start).ToArray();
        }
        else
        {
            start = Math.Max(0, offset - 1);
            chunk = lines.Skip(start).Take(limit).ToArray();
        }

        var result = FormatAsTextFile(chunk, start, total);

        // 附加缓存的 LSP 诊断信息
        var diag = DiagnosticManager.FormatForLLM(path);
        if (diag != null)
            result += "\n" + diag;

        return result;
    }

    /// <summary>检测二进制内容：前 8KB 含 NUL 字节即判定为二进制。</summary>
    private static bool IsBinaryContent(byte[] raw)
    {
        var n = Math.Min(raw.Length, 8192);
        for (int i = 0; i < n; i++)
            if (raw[i] == 0) return true;
        return false;
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
                line = ContextManager.TruncateByRunes(line, MaxLineLength) + "...";
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
    // JSON 文件读取（结构化美化）
    // ════════════════════════════════════════════════════════════
    private static string ReadJsonFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileSize * 5)
            return $"⚠ 文件过大: {FormatSize(info.Length)}（最大 {FormatSize(MaxFileSize * 5)}）";

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {path}"; }

        if (IsBinaryContent(raw))
            return $"错误：{path} 是二进制文件";

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            var node = Json.Parse(text);
            if (node != null)
            {
                var pretty = Json.Serialize(node, indent: true);
                return $"<json>\n文件: {Path.GetFileName(path)} | 大小: {FormatSize(info.Length)}\n\n{pretty}\n</json>";
            }
        }
        catch { }

        // JSON 解析失败 → 回退纯文本
        return ReadTextFile(path, Path.GetFileName(path), 1, DefaultLimit, 0);
    }

    // ════════════════════════════════════════════════════════════
    // INI 配置文件读取（结构化）
    // ════════════════════════════════════════════════════════════
    private static string ReadIniFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileSize * 5)
            return $"⚠ 文件过大: {FormatSize(info.Length)}（最大 {FormatSize(MaxFileSize * 5)}）";

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {path}"; }

        if (IsBinaryContent(raw))
            return $"错误：{path} 是二进制文件";

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);

        var sb = new StringBuilder();
        sb.AppendLine("<ini>");
        sb.AppendLine($"文件: {Path.GetFileName(path)} | 大小: {FormatSize(info.Length)}");
        sb.AppendLine();

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith(';') || line.StartsWith('#'))
            {
                sb.AppendLine($"  {line}");
                continue;
            }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                sb.AppendLine($"[{line[1..^1]}]");
            }
            else if (line.Contains('='))
            {
                var idx = line.IndexOf('=');
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                sb.AppendLine($"  {key} = {val}");
            }
        }

        sb.Append("</ini>");
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
            html = ContextManager.TruncateByRunes(html, 10_000) + $"\n...(截断于 10,000 字符，原始 {info.Length:N0} 字节)";

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
