using System.Text;
using System.Text.RegularExpressions;

namespace CoreCoderSharp.Tools;

/// <summary>
/// Web 抓取工具 —— 获取网页内容并提取纯文本。
/// </summary>
public class FetchTool : ITool
{
    public string Name => "fetch";
    public string Description => "抓取网页 URL 的内容，自动提取纯文本（去除 HTML 标签）。用于查阅文档、阅读文章、获取最新信息。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["url"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要抓取的网页 URL (http/https)",
            },
            ["max_chars"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "最大返回字符数（默认 8000）",
            },
        },
        ["required"] = new JsonArray("url"),
    };

    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    static FetchTool()
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 CoreCoderSharp/1.0 (compatible; AI coding assistant)");
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        var maxChars = arguments.TryGetValue("max_chars", out var mc) && mc is int mi ? mi : 8000;

        return await Execute(url, maxChars);
    }

    private static async Task<string> Execute(string url, int maxChars)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return "错误：URL 必须以 http:// 或 https:// 开头";

        try
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return $"错误：不支持的内容类型 '{contentType}'（仅支持 HTML/纯文本）";

            var html = await response.Content.ReadAsStringAsync();
            var text = StripHtml(html);

            if (text.Length > maxChars)
                text = text[..maxChars] + $"\n\n... (已截断，原始共 {text.Length} 字符)";

            return string.IsNullOrWhiteSpace(text) ? "（页面无文本内容）" : text;
        }
        catch (HttpRequestException ex)
        {
            return $"请求失败：{ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "错误：请求超时（30 秒）";
        }
        catch (Exception ex)
        {
            return $"抓取错误：{ex.Message}";
        }
    }

    /// <summary>
    /// 去除 HTML 标签，提取纯文本。
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // 移除 script/style 标签及其内容
        html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 移除 HTML 标签
        html = Regex.Replace(html, @"<[^>]+>", " ");

        // 解码常见 HTML 实体
        html = System.Net.WebUtility.HtmlDecode(html);

        // 压缩空白行
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n\s*\n", "\n\n");

        return html.Trim();
    }
}
