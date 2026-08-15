namespace WayCoder.Infra;

/// <summary>
/// PDF 文本提取器 — 使用手搓 PdfParser（纯 BCL，零第三方依赖）。
/// 提取 PDF 文本内容，支持分页。
/// </summary>
public static class PdfExtractor
{
    /// <summary>
    /// 从 PDF 文件提取纯文本。
    /// </summary>
    /// <param name="filePath">PDF 文件路径</param>
    /// <param name="startPage">起始页码（1-based），默认 1</param>
    /// <param name="pageLimit">最大页数，默认 20</param>
    /// <returns>提取结果（文本 + 元数据）</returns>
    public static PdfExtractResult Extract(string filePath, int startPage = 1, int pageLimit = 20)
    {
        try
        {
            var pdf = PdfParser.Open(filePath);
            if (pdf == null)
                return new PdfExtractResult
                {
                    FilePath = filePath,
                    Error = "PDF 解析失败：文件损坏、加密或使用了不支持的结构（object stream 等）。",
                };

            var totalPages = pdf.NumberOfPages;
            var endPage = Math.Min(startPage + pageLimit - 1, totalPages);

            var pages = new List<PdfPageContent>();
            int totalChars = 0;

            for (int i = startPage; i <= endPage; i++)
            {
                var text = pdf.ExtractPageText(i) ?? "";

                // 压缩连续空行：最多保留 1 个空行
                var lines = text.Split('\n');
                var cleaned = new List<string>();
                bool prevEmpty = false;
                foreach (var line in lines)
                {
                    var trimmed = line.TrimEnd('\r', ' ');
                    var isEmpty = string.IsNullOrWhiteSpace(trimmed);

                    if (isEmpty)
                    {
                        if (!prevEmpty)
                            cleaned.Add("");
                        prevEmpty = true;
                    }
                    else
                    {
                        cleaned.Add(trimmed);
                        prevEmpty = false;
                    }
                }

                var pageText = string.Join("\n", cleaned).Trim();
                totalChars += pageText.Length;

                pages.Add(new PdfPageContent
                {
                    PageNumber = i,
                    Text = pageText,
                    CharCount = pageText.Length,
                });
            }

            return new PdfExtractResult
            {
                FilePath = filePath,
                TotalPages = totalPages,
                PagesExtracted = pages.Count,
                StartPage = startPage,
                TotalChars = totalChars,
                Pages = pages,
                HasMore = endPage < totalPages,
                Title = pdf.Title,
            };
        }
        catch (Exception ex)
        {
            return new PdfExtractResult
            {
                FilePath = filePath,
                Error = $"PDF 读取失败: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// 快速获取 PDF 元数据（不提取全文）。
    /// </summary>
    public static PdfMeta? GetMeta(string filePath)
    {
        try
        {
            var pdf = PdfParser.Open(filePath);
            if (pdf == null) return null;
            return new PdfMeta
            {
                Pages = pdf.NumberOfPages,
                Title = pdf.Title,
            };
        }
        catch
        {
            return null;
        }
    }
}

public class PdfExtractResult
{
    public string FilePath { get; set; } = "";
    public int TotalPages { get; set; }
    public int PagesExtracted { get; set; }
    public int StartPage { get; set; }
    public int TotalChars { get; set; }
    public List<PdfPageContent> Pages { get; set; } = [];
    public bool HasMore { get; set; }
    public string? Title { get; set; }
    public string? Error { get; set; }

    public bool IsError => Error != null;

    /// <summary>
    /// 格式化为 LLM 可读的文本。
    /// </summary>
    public string ToMarkdown()
    {
        if (IsError) return Error!;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<pdf>");
        if (Title != null)
            sb.AppendLine($"# {Title}");
        sb.AppendLine($"总页数: {TotalPages} | 当前: 第 {StartPage}-{StartPage + PagesExtracted - 1} 页 | 共 {TotalChars:N0} 字符");
        sb.AppendLine();

        foreach (var page in Pages)
        {
            if (Pages.Count > 1)
                sb.AppendLine($"## 第 {page.PageNumber} 页 ({page.CharCount:N0} 字符)");
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }

        if (HasMore)
            sb.AppendLine($"(还有 {TotalPages - (StartPage + PagesExtracted - 1)} 页。使用 page 参数读取后续内容。)");

        sb.Append("</pdf>");
        return sb.ToString();
    }
}

public class PdfPageContent
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = "";
    public int CharCount { get; set; }
}

public class PdfMeta
{
    public int Pages { get; set; }
    public string? Title { get; set; }
}
