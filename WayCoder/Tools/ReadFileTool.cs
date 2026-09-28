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
///   - PDF 文本提取（手搓 PdfParser，纯 BCL，支持分页）
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
    public string Description => L.Pick("读取文件内容。支持代码文件（行号）、PDF（文本提取分页）、Office文档（docx/xlsx/pptx 及老式 doc/xls/ppt、WPS 的 wps/et/dps 文本提取）、Markdown（结构化渲染）、CSV（表格）、HTML（标签剥离）、JSON（美化）、INI（结构化）、tail 读取末尾 N 行。修改文件之前始终先读取它。", "Read file contents. Supports code files (with line numbers), PDF (paged text extraction), Office documents (docx/xlsx/pptx plus legacy doc/xls/ppt and WPS wps/et/dps text extraction), Markdown (structured rendering), CSV (tables), HTML (tag stripping), JSON (pretty-printing), INI (structured), and tail to read the last N lines. Always read a file before modifying it.");

    private const int MaxFileSize = 100 * 1024; // 100KB for text, PDF handles separately
    private const int DefaultLimit = 2000;
    private const int MaxLineLength = 2000;
    private const int PdfMaxPages = 20;

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Param("string", L.Pick("文件路径。支持 .cs .py .js .ts .md .pdf .html .json .txt 等。", "File path. Supports .cs .py .js .ts .md .pdf .html .json .txt, etc.")))
            .Set("offset", JNode.Param("integer", L.Pick("起始行（从 1 开始）。PDF 文件此参数表示起始页码。默认 1。", "Starting line (1-based). For PDF files this is the starting page number. Default 1.")))
            .Set("limit", JNode.Param("integer", L.Pick("最大读取行数。PDF 文件此参数表示最大页数（默认 20）。默认 2000。", "Maximum number of lines to read. For PDF files this is the maximum number of pages (default 20). Default 2000.")))
            .Set("tail", JNode.Param("integer", L.Pick("读取文件末尾 N 行（与 offset/limit 互斥，优先于 offset）。适合查看日志/大文件末尾。默认 0 禁用。", "Read the last N lines of the file (mutually exclusive with offset/limit; takes precedence over offset). Useful for viewing the end of logs or large files. Default 0 (disabled)."))))
        .Set("required", JNode.Array("file_path"));

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
        return result + (WayCoder.Infra.PromptInjection.WarningIfInjected(result, L.Pick($"文件 {filePath}", $"file {filePath}")) ?? "");
    }

    private static string ResolveExecute(string filePath, int offset, int limit, int tail)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return L.Pick("错误：file_path 不能为空 — 请提供有效的文件路径。",
                              "Error: file_path must not be empty - please provide a valid file path.");

            var path = CwdContext.Resolve(filePath); // cd 后相对路径基于被跟踪工作目录

            // 敏感路径防护（SSH 密钥/云凭据/系统凭据，防提示注入读泄露）
            var sensitive = PathSafety.CheckSensitive(path);
            if (sensitive != null)
                return L.Pick($"❌ 已阻止：{sensitive}（安全策略：敏感文件读写受保护）",
                              $"❌ Blocked: {sensitive} (security policy: reading and writing sensitive files is protected)");

            // 目录检查
            if (Directory.Exists(path))
                return L.Pick($"错误：{filePath} 是目录，不是文件",
                              $"Error: {filePath} is a directory, not a file");

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
                return L.Pick($"📷 这是一个图片文件: {filePath}\n大小: {FormatUtil.FormatSize(info.Length)}\n格式: {ext.TrimStart('.')}\n\n💡 提示：使用 view 查看或 download 下载。",
                              $"📷 This is an image file: {filePath}\nSize: {FormatUtil.FormatSize(info.Length)}\nFormat: {ext.TrimStart('.')}\n\n💡 Tip: use view to look at it, or download to save it.");
            }

            // ── 普通文本文件 ──
            return ReadTextFile(path, filePath, offset, limit, tail);
        }
        catch (Exception ex)
        {
            return ToolErrors.Error("", ex);
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
            return L.Pick($"⚠ PDF 文件过大: {FormatUtil.FormatSize(info.Length)}（最大 50 MB）",
                          $"⚠ PDF file is too large: {FormatUtil.FormatSize(info.Length)} (maximum 50 MB)");

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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(fileInfo.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize * 5)}）",
                          $"⚠ File is too large: {FormatUtil.FormatSize(fileInfo.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize * 5)})");

        try { _ = Encoding.UTF8.GetString(File.ReadAllBytes(path)); }
        catch { return L.Pick($"错误：{path} 不是 UTF-8 文本文件", $"Error: {path} is not a UTF-8 text file"); }

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
        sb.AppendLine(L.Pick($"文件: {Path.GetFileName(path)} | 行 {start + 1}-{start + chunk.Length} / {allLines.Length}",
                             $"File: {Path.GetFileName(path)} | lines {start + 1}-{start + chunk.Length} / {allLines.Length}"));
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
                        sb.AppendLine(L.Pick($"... (省略 {codeLines.Length - 80} 行)",
                                             $"... ({codeLines.Length - 80} lines omitted)"));
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
                        sb.AppendLine(L.Pick($"... (省略 {t.Rows.Count - 30} 行)",
                                             $"... ({t.Rows.Count - 30} lines omitted)"));
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
            sb.AppendLine(L.Pick($"\n(文件还有更多行。使用 offset={start + chunk.Length + 1} 读取后续内容)",
                                 $"\n(There are more lines. Use offset={start + chunk.Length + 1} to read the rest.)"));

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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(fileInfo.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize)}）\n💡 提示：使用 offset/limit 分段读取。",
                          $"⚠ File is too large: {FormatUtil.FormatSize(fileInfo.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize)})\n💡 Tip: read it in chunks with offset/limit.");
        }

        // 读取 + 二进制/UTF-8 验证
        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return L.Pick($"错误：无法读取 {filePath}", $"Error: could not read {filePath}"); }

        if (TextEncoding.IsBinaryContent(raw))
            return L.Pick($"错误：{filePath} 是二进制文件（检测到 NUL 字节），read_file 只能读取文本文件",
                          $"Error: {filePath} is a binary file (NUL bytes detected); read_file can only read text files");

        try { _ = new UTF8Encoding(false, true).GetString(raw); }
        catch { return L.Pick($"错误：{filePath} 不是 UTF-8 文本文件（read_file 只能读取文本文件）",
                              $"Error: {filePath} is not a UTF-8 text file (read_file can only read text files)"); }

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
            sb.AppendLine(L.Pick($"(文件还有更多行。使用 offset={start + chunk.Length + 1} 读取后续内容)",
                                 $"(There are more lines. Use offset={start + chunk.Length + 1} to read the rest.)"));
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

        // 判断**问分类器**，不看字面量 —— 这样"错误"翻成 "Error:" 之后这里照样成立
        // （原先是 StartsWith("错误")，翻文案即静默失效）。
        if (ToolResultClassifier.IsError(content) || content.StartsWith(format))
            return content;

        var sb = new StringBuilder();
        sb.AppendLine($"<{format.ToLower()}>");
        sb.AppendLine(L.Pick($"文件: {Path.GetFileName(path)} | 大小: {FormatUtil.FormatSize(info.Length)} | 格式: {format}",
                             $"File: {Path.GetFileName(path)} | Size: {FormatUtil.FormatSize(info.Length)} | Format: {format}"));
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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(info.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize * 5)}）",
                          $"⚠ File is too large: {FormatUtil.FormatSize(info.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize * 5)})");

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return L.Pick($"错误：无法读取 {path}", $"Error: could not read {path}"); }

        try { _ = Encoding.UTF8.GetString(raw); }
        catch { return L.Pick($"错误：{path} 不是 UTF-8 文本文件", $"Error: {path} is not a UTF-8 text file"); }

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);
        var table = OfficeExtractor.ParseCsv(text);

        var sb = new StringBuilder();
        sb.AppendLine("<csv>");
        sb.AppendLine(L.Pick($"文件: {Path.GetFileName(path)} | 大小: {FormatUtil.FormatSize(info.Length)}",
                             $"File: {Path.GetFileName(path)} | Size: {FormatUtil.FormatSize(info.Length)}"));
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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(info.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize * 5)}）",
                          $"⚠ File is too large: {FormatUtil.FormatSize(info.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize * 5)})");

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return L.Pick($"错误：无法读取 {path}", $"Error: could not read {path}"); }

        if (TextEncoding.IsBinaryContent(raw))
            return L.Pick($"错误：{path} 是二进制文件", $"Error: {path} is a binary file");

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            var node = Json.Parse(text);
            if (node != null)
            {
                var pretty = Json.Serialize(node, indent: true);
                return L.Pick($"<json>\n文件: {Path.GetFileName(path)} | 大小: {FormatUtil.FormatSize(info.Length)}\n\n{pretty}\n</json>",
                              $"<json>\nFile: {Path.GetFileName(path)} | Size: {FormatUtil.FormatSize(info.Length)}\n\n{pretty}\n</json>");
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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(info.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize * 5)}）",
                          $"⚠ File is too large: {FormatUtil.FormatSize(info.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize * 5)})");

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return L.Pick($"错误：无法读取 {path}", $"Error: could not read {path}"); }

        if (TextEncoding.IsBinaryContent(raw))
            return L.Pick($"错误：{path} 是二进制文件", $"Error: {path} is a binary file");

        FileTracker.RecordRead(path);
        var text = File.ReadAllText(path, Encoding.UTF8);

        var sb = new StringBuilder();
        sb.AppendLine("<ini>");
        sb.AppendLine(L.Pick($"文件: {Path.GetFileName(path)} | 大小: {FormatUtil.FormatSize(info.Length)}",
                             $"File: {Path.GetFileName(path)} | Size: {FormatUtil.FormatSize(info.Length)}"));
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
            return L.Pick($"⚠ 文件过大: {FormatUtil.FormatSize(info.Length)}（最大 {FormatUtil.FormatSize(MaxFileSize * 5)}）",
                          $"⚠ File is too large: {FormatUtil.FormatSize(info.Length)} (maximum {FormatUtil.FormatSize(MaxFileSize * 5)})");

        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return L.Pick($"错误：无法读取 {path}", $"Error: could not read {path}"); }

        try { _ = Encoding.UTF8.GetString(raw); }
        catch { return L.Pick($"错误：{path} 不是 UTF-8 文本文件", $"Error: {path} is not a UTF-8 text file"); }

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
            html = ContextManager.TruncateByRunes(html, 10_000)
                   + L.Pick($"\n...(截断于 10,000 字符，原始 {info.Length:N0} 字节)",
                            $"\n...(truncated at 10,000 characters; the original is {info.Length:N0} bytes)");

        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        if (title != null)
            sb.AppendLine($"# {title}");
        sb.AppendLine(L.Pick($"文件: {Path.GetFileName(path)} | 大小: {FormatUtil.FormatSize(info.Length)}",
                             $"File: {Path.GetFileName(path)} | Size: {FormatUtil.FormatSize(info.Length)}"));
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
            return L.Pick($"错误：{originalPath} 未找到", $"Error: {originalPath} not found");

        try
        {
            if (!Directory.Exists(dir))
                return L.Pick($"错误：{originalPath} 未找到（目录不存在）",
                              $"Error: {originalPath} not found (the directory does not exist)");

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
                return L.Pick($"错误：{originalPath} 未找到\n\n你想找的是不是？\n{string.Join("\n", suggestions.Select(s => $"  • {s}"))}",
                              $"Error: {originalPath} not found\n\nDid you mean one of these?\n{string.Join("\n", suggestions.Select(s => $"  • {s}"))}");
            }
        }
        catch { }

        return L.Pick($"错误：{originalPath} 未找到", $"Error: {originalPath} not found");
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

}
