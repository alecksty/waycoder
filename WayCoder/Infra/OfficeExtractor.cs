using System.IO.Compression;
using System.Text;
using System.Xml;

namespace WayCoder.Infra;

/// <summary>
/// Office 文档文本提取器（DOCX / XLSX / PPTX）
/// 纯 .NET 内置库实现，零外部依赖，AOT 兼容。
/// Office Open XML 格式本质是 ZIP 包内含 XML 文件。
/// </summary>
public static class OfficeExtractor
{
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

            using var stream = docEntry.Open();
            var sb = new StringBuilder();

            using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });
            bool inParagraph = false;
            var paraText = new StringBuilder();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "p")
                {
                    inParagraph = true;
                    paraText.Clear();
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "p")
                {
                    inParagraph = false;
                    var text = paraText.ToString().Trim();
                    if (text.Length > 0)
                    {
                        sb.AppendLine(text);
                        if (sb.Length > maxChars) break;
                    }
                }
                else if (inParagraph && reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                {
                    reader.Read();
                    if (reader.NodeType == XmlNodeType.Text)
                        paraText.Append(reader.Value);
                }
                // 表格处理
                else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tbl")
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.AppendLine(ExtractDocxTable(reader));
                    if (sb.Length > maxChars) break;
                }
            }

            var result = sb.ToString().Trim();
            if (result.Length > maxChars)
                result = result[..maxChars] + $"\n...(截断于 {maxChars:N0} 字符)";

            return result.Length > 0 ? result : "(DOCX 文件无文本内容)";
        }
        catch (Exception ex)
        {
            return $"DOCX 读取错误: {ex.Message}";
        }
    }

    private static string ExtractDocxTable(XmlReader reader)
    {
        var rows = new List<List<string>>();
        var depth = reader.Depth;
        bool inRow = false, inCell = false;
        var cellText = new StringBuilder();
        List<string>? currentRow = null;

        while (reader.Read())
        {
            if (reader.Depth <= depth && reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tbl")
                break;

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tr")
            {
                currentRow = new List<string>();
                inRow = true;
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tr")
            {
                inRow = false;
                if (currentRow != null && currentRow.Count > 0)
                    rows.Add(currentRow);
            }
            else if (inRow && reader.NodeType == XmlNodeType.Element && reader.LocalName == "tc")
            {
                inCell = true;
                cellText.Clear();
            }
            else if (inRow && reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tc")
            {
                inCell = false;
                currentRow?.Add(cellText.ToString().Trim());
            }
            else if (inCell && reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
            {
                reader.Read();
                if (reader.NodeType == XmlNodeType.Text)
                    cellText.Append(reader.Value);
            }
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
                using var stream = sstEntry.Open();
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            sharedStrings.Add(reader.Value);
                        else
                            sharedStrings.Add("");
                    }
                }
            }

            // 2. 读取第一个工作表
            var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
            if (sheetEntry == null) return "错误：无效的 XLSX 文件（缺少 xl/worksheets/sheet1.xml）";

            using var sheetStream = sheetEntry.Open();
            using var reader2 = XmlReader.Create(sheetStream, new XmlReaderSettings { IgnoreWhitespace = true });

            var rows = new List<List<string>>();
            List<string>? currentRow = null;

            while (reader2.Read())
            {
                if (reader2.NodeType == XmlNodeType.Element && reader2.LocalName == "row")
                {
                    currentRow = new List<string>();
                }
                else if (reader2.NodeType == XmlNodeType.EndElement && reader2.LocalName == "row")
                {
                    if (currentRow != null && currentRow.Count > 0)
                        rows.Add(currentRow);
                    if (rows.Count >= maxRows) break;
                }
                else if (reader2.NodeType == XmlNodeType.Element && reader2.LocalName == "c" && currentRow != null)
                {
                    var t = reader2.GetAttribute("t"); // "s" = shared string, "n" = number
                    reader2.Read();
                    string val = "";
                    while (reader2.NodeType != XmlNodeType.EndElement || reader2.LocalName != "c")
                    {
                        if (reader2.NodeType == XmlNodeType.Element && reader2.LocalName == "v")
                        {
                            reader2.Read();
                            if (reader2.NodeType == XmlNodeType.Text)
                            {
                                var raw = reader2.Value;
                                if (t == "s" && int.TryParse(raw, out var idx) && idx < sharedStrings.Count)
                                    val = sharedStrings[idx];
                                else
                                    val = raw;
                            }
                        }
                        reader2.Read();
                    }
                    currentRow.Add(val);
                }
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
                result = result[..maxChars] + $"\n...(截断于 {maxChars:N0} 字符)";
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

                using var stream = entry.Open();
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });

                var slideTexts = new List<string>();
                var textBuf = new StringBuilder();
                bool inText = false;

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                    {
                        inText = true;
                        textBuf.Clear();
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "t")
                    {
                        inText = false;
                        var t = textBuf.ToString().Trim();
                        if (t.Length > 0) slideTexts.Add(t);
                    }
                    else if (inText && reader.NodeType == XmlNodeType.Text)
                    {
                        textBuf.Append(reader.Value);
                    }
                    // 换行符
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "br")
                    {
                        textBuf.Append('\n');
                    }

                    if (sb.Length > maxChars) break;
                }

                if (slideTexts.Count > 0)
                {
                    sb.AppendLine($"## 幻灯片 {i}");
                    foreach (var t in slideTexts)
                    {
                        // 标题检测（字号大的通常是标题）
                        if (slideTexts.IndexOf(t) == 0 && slideTexts.Count > 1)
                            sb.AppendLine($"### {t}");
                        else
                            sb.AppendLine(t);
                    }
                    sb.AppendLine();
                }
            }

            var result = sb.ToString().Trim();
            if (result.Length > maxChars)
                result = result[..maxChars] + $"\n...(截断于 {maxChars:N0} 字符)";

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
}
