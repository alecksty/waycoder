using System.IO.Compression;
using System.Text;
using WayCoder;

namespace WayCoder.Infra;

/// <summary>
/// Office 文档文本提取器（DOCX / XLSX / PPTX）
/// 纯 .NET 内置库实现，零外部依赖，AOT 兼容。
/// Office Open XML 格式本质是 ZIP 包内含 XML 文件。
/// XML 解析使用手搓 Xml/XNode（AOT 零反射，不依赖 System.Xml）。
/// </summary>
public static class OfficeExtractor
{
    /// <summary>单个 ZIP 条目解压大小上限（防 zip bomb 解压 OOM），64MB。</summary>
    private const int MaxEntryBytes = 64 * 1024 * 1024;

    /// <summary>
    /// 从 DOCX 提取纯文本。
    /// </summary>
    public static string ExtractDocx(string filePath, int maxChars = 50_000)
    {
        try
        {
            using var zip = ZipFile.OpenRead(filePath);
            var docEntry = zip.GetEntry("word/document.xml");
            if (docEntry == null) return "错误：无效的 DOCX 文件（缺少 word/document.xml）";

            var root = Xml.Parse(ReadEntryText(docEntry));
            if (root == null) return "(DOCX 文件无文本内容)";

            // w:document → w:body → (w:p | w:tbl)*，段落与表格是 body 的直接子元素
            var body = root.Children.FirstOrDefault(c => c.Kind == XKind.Element && Local(c) == "body") ?? root;

            var sb = new StringBuilder();
            foreach (var child in body.Children)
            {
                if (child.Kind != XKind.Element) continue;
                var local = Local(child);
                if (local == "p")
                {
                    var text = child.InnerText().Trim();
                    if (text.Length > 0)
                    {
                        sb.AppendLine(text);
                        if (sb.Length > maxChars) break;
                    }
                }
                else if (local == "tbl")
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.AppendLine(ExtractDocxTable(child));
                    if (sb.Length > maxChars) break;
                }
            }

            var result = sb.ToString().Trim();
            if (result.Length > maxChars)
                result = ContextManager.TruncateByRunes(result, maxChars) + $"\n...(截断于 {maxChars:N0} 字符)";

            return result.Length > 0 ? result : "(DOCX 文件无文本内容)";
        }
        catch (Exception ex)
        {
            return $"DOCX 读取错误: {ex.Message}";
        }
    }

    private static string ExtractDocxTable(XNode tbl)
    {
        var rows = new List<List<string>>();
        // w:tbl → w:tr → w:tc（直接子元素）
        foreach (var tr in DirectChildren(tbl, "tr"))
        {
            var row = new List<string>();
            foreach (var tc in DirectChildren(tr, "tc"))
                row.Add(tc.InnerText().Trim());
            if (row.Count > 0) rows.Add(row);
        }

        if (rows.Count == 0) return "";

        var sb = new StringBuilder();
        int maxCols = rows.Max(r => r.Count);
        foreach (var row in rows.Take(50))
        {
            var cells = row.Concat(Enumerable.Repeat("", maxCols - row.Count)).ToArray();
            sb.AppendLine("| " + string.Join(" | ", cells) + " |");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 从 XLSX 提取数据（前 N 行）。
    /// </summary>
    public static string ExtractXlsx(string filePath, int maxRows = 200, int maxChars = 50_000)
    {
        try
        {
            using var zip = ZipFile.OpenRead(filePath);

            // 1. 读取共享字符串表
            var sharedStrings = new List<string>();
            var sstEntry = zip.GetEntry("xl/sharedStrings.xml");
            if (sstEntry != null)
            {
                var sstRoot = Xml.Parse(ReadEntryText(sstEntry));
                if (sstRoot != null)
                {
                    // 富文本 <si> 内多个 <t>（runs）应拼接为一个字符串；直接展平所有 <t>
                    // 会导致加粗表头等富文本单元格内容错位、后续所有共享字符串索引整体偏移
                    foreach (var si in Elements(sstRoot, "si"))
                    {
                        var siText = new System.Text.StringBuilder();
                        foreach (var t in Elements(si, "t"))
                            siText.Append(t.InnerText());
                        sharedStrings.Add(siText.ToString());
                    }
                }
            }

            // 2. 读取第一个工作表
            var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
            if (sheetEntry == null) return "错误：无效的 XLSX 文件（缺少 xl/worksheets/sheet1.xml）";

            var sheetRoot = Xml.Parse(ReadEntryText(sheetEntry));
            if (sheetRoot == null) return "(XLSX 文件无数据)";

            var rows = new List<List<string>>();
            foreach (var row in Elements(sheetRoot, "row"))
            {
                var currentRow = new List<string>();
                foreach (var c in DirectChildren(row, "c"))
                {
                    var t = c.GetAttr("t"); // "s" = shared string, "n" = number
                    var v = DirectChildren(c, "v").FirstOrDefault();
                    var raw = v?.InnerText() ?? "";
                    if (t == "s" && int.TryParse(raw, out var idx) && idx >= 0 && idx < sharedStrings.Count)
                        currentRow.Add(sharedStrings[idx]);
                    else
                        currentRow.Add(raw);
                }
                if (currentRow.Count > 0) rows.Add(currentRow);
                if (rows.Count >= maxRows) break;
            }

            if (rows.Count == 0) return "(XLSX 文件无数据)";

            var sb = new StringBuilder();
            int maxCols = rows.Max(r => r.Count);

            // 表格头（第一行）
            if (rows.Count > 0)
            {
                var header = rows[0].Concat(Enumerable.Repeat("", maxCols - rows[0].Count)).ToArray();
                sb.AppendLine("| " + string.Join(" | ", header) + " |");
                sb.AppendLine("|" + string.Join("|", header.Select(_ => "---")) + "|");
            }

            foreach (var row in rows.Skip(1).Take(maxRows - 1))
            {
                var cells = row.Concat(Enumerable.Repeat("", maxCols - row.Count)).ToArray();
                sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                if (sb.Length > maxChars) break;
            }

            if (rows.Count > maxRows)
                sb.AppendLine($"...(省略 {rows.Count - maxRows} 行)");

            var result = sb.ToString();
            if (result.Length > maxChars)
                result = ContextManager.TruncateByRunes(result, maxChars) + $"\n...(截断于 {maxChars:N0} 字符)";
            return result;
        }
        catch (Exception ex)
        {
            return $"XLSX 读取错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 从 PPTX 提取文本（每页幻灯片）。
    /// </summary>
    public static string ExtractPptx(string filePath, int maxSlides = 30, int maxChars = 50_000)
    {
        try
        {
            using var zip = ZipFile.OpenRead(filePath);
            var sb = new StringBuilder();
            int slideNum = 0;

            for (int i = 1; i <= maxSlides + 5; i++)
            {
                var entry = zip.GetEntry($"ppt/slides/slide{i}.xml");
                if (entry == null) break;
                slideNum++;

                var root = Xml.Parse(ReadEntryText(entry));
                if (root == null) continue;

                var slideTexts = new List<string>();
                foreach (var t in Elements(root, "t"))
                {
                    var text = t.InnerText().Trim();
                    if (text.Length > 0) slideTexts.Add(text);
                }

                if (slideTexts.Count > 0)
                {
                    sb.AppendLine($"## 幻灯片 {i}");
                    foreach (var t in slideTexts)
                    {
                        // 标题检测（首段通常是标题）
                        if (slideTexts.IndexOf(t) == 0 && slideTexts.Count > 1)
                            sb.AppendLine($"### {t}");
                        else
                            sb.AppendLine(t);
                    }
                    sb.AppendLine();
                }

                if (sb.Length > maxChars) break;
            }

            var result = sb.ToString().Trim();
            if (result.Length > maxChars)
                result = ContextManager.TruncateByRunes(result, maxChars) + $"\n...(截断于 {maxChars:N0} 字符)";

            return result.Length > 0 ? result : $"(PPTX 文件，{slideNum} 张幻灯片，无文本内容)";
        }
        catch (Exception ex)
        {
            return $"PPTX 读取错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 解析 CSV 为 Markdown 表格。
    /// </summary>
    public static string ParseCsv(string text, int maxRows = 200)
    {
        try
        {
            var lines = text.Split('\n');
            if (lines.Length == 0) return "(空 CSV)";

            var rows = new List<string[]>();
            foreach (var line in lines.Take(maxRows))
            {
                var trimmed = line.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(trimmed) && rows.Count > 0) continue;
                rows.Add(ParseCsvLine(trimmed));
            }

            if (rows.Count == 0) return "(空 CSV)";

            var sb = new StringBuilder();
            int maxCols = rows.Max(r => r.Length);

            // 表头 + 分隔线
            var header = rows[0];
            sb.AppendLine("| " + string.Join(" | ", header.Concat(Enumerable.Repeat("", maxCols - header.Length))) + " |");
            sb.AppendLine("|" + string.Join("|", Enumerable.Repeat("---", maxCols)) + "|");

            foreach (var row in rows.Skip(1))
            {
                var cells = row.Concat(Enumerable.Repeat("", maxCols - row.Length)).ToArray();
                sb.AppendLine("| " + string.Join(" | ", cells) + " |");
            }

            if (lines.Length > maxRows)
                sb.AppendLine($"...(省略 {lines.Length - maxRows} 行)");

            return sb.ToString();
        }
        catch
        {
            return text; // 回退到原始文本
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else if (c == '"')
                    inQuotes = false;
                else
                    current.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',')
                { cells.Add(current.ToString().Trim()); current.Clear(); }
                else
                    current.Append(c);
            }
        }
        cells.Add(current.ToString().Trim());
        return cells.ToArray();
    }

    // ════════════════════════════════════════════════════════════
    // XNode DOM 辅助（OOXML 元素带命名空间前缀，如 w:p / a:t）
    // ════════════════════════════════════════════════════════════

    /// <summary>读取 ZIP 条目全文为 UTF-8 字符串（先校验解压大小，防 zip bomb OOM）。</summary>
    private static string ReadEntryText(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxEntryBytes)
            throw new InvalidDataException($"ZIP 条目过大（{entry.Length:N0} 字节），疑似 zip bomb");
        using var stream = entry.Open();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    /// <summary>取元素本地名（去掉命名空间前缀，如 "w:p" → "p"）。</summary>
    private static string Local(XNode n)
    {
        var name = n.Name;
        var i = name.IndexOf(':');
        return i >= 0 ? name[(i + 1)..] : name;
    }

    /// <summary>直接子元素中按本地名匹配。</summary>
    private static IEnumerable<XNode> DirectChildren(XNode node, string local)
        => node.Children.Where(c => c.Kind == XKind.Element && Local(c) == local);

    /// <summary>递归所有后代元素。</summary>
    private static IEnumerable<XNode> Descendants(XNode node)
    {
        foreach (var c in node.Children)
        {
            yield return c;
            foreach (var d in Descendants(c)) yield return d;
        }
    }

    /// <summary>所有后代元素中按本地名匹配。</summary>
    private static IEnumerable<XNode> Elements(XNode node, string local)
        => Descendants(node).Where(n => n.Kind == XKind.Element && Local(n) == local);
}
