using System.Text.RegularExpressions;
using WayCoder.Infra;

namespace WayCoder.Tools;

/// <summary>
/// 增强版 Web 抓取工具（对标 crush fetch/web_fetch）。
///
/// 功能：
///   - HTML 净化：去除 script/style/nav/footer/header/aside 等噪音元素
///   - HTML → Markdown 转换（保留标题/链接/代码等结构）
///   - 多输出格式：text（纯文本）、markdown（结构化）
///   - 大内容处理（>50KB 截断提示）
///   - 反检测 Headers
/// </summary>
public class FetchTool : ITool
{
    public string Name => "fetch";
    public string Description => "抓取网页 URL 的内容，自动提取纯文本或 Markdown（去除 HTML 噪音）。用于查阅文档、阅读文章、获取最新信息。";

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
                ["description"] = "最大返回字符数（默认 8000，最大 100000）",
            },
            ["format"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "输出格式：'text'（纯文本）或 'markdown'（结构化），默认 'text'",
            },
        },
        ["required"] = new JsonArray("url"),
    };

    private static HttpClient _client => _lazyClient.Value;
    private static readonly Lazy<HttpClient> _lazyClient = new(() => new HttpClient(
        new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 })
    { Timeout = TimeSpan.FromSeconds(Config.Instance.FetchTimeoutSec) });

    /// <summary>需移除的噪音 HTML 元素（对标 crush）</summary>
    private static readonly string[] NoisyElements =
        ["script", "style", "nav", "footer", "header", "aside", "noscript", "iframe", "svg"];

    static FetchTool()
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        var maxChars = arguments.TryGetValue("max_chars", out var mc) && mc is int mi ? Math.Min(mi, 100_000) : 8000;
        var format = arguments.TryGetValue("format", out var fmt) ? fmt?.ToString()?.ToLowerInvariant() : "text";

        return await Execute(url, maxChars, format ?? "text");
    }

    private static async Task<string> Execute(string url, int maxChars, string format)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return "错误：URL 必须以 http:// 或 https:// 开头";

        try
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            // JSON 特殊处理
            if (contentType.Contains("json"))
            {
                var json = await response.Content.ReadAsStringAsync();
                return PrettyPrintJson(json, maxChars);
            }

            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return $"错误：不支持的内容类型 '{contentType}'（仅支持 HTML/纯文本/JSON）";

            var html = await response.Content.ReadAsStringAsync();
            var text = format == "markdown" ? ConvertToMarkdown(html) : StripHtml(html);

            // 压缩空白（合并连续空行）
            text = Regex.Replace(text, @"\n{4,}", "\n\n\n");
            text = Regex.Replace(text, @"[ \t]{2,}", " ");

            if (text.Length > maxChars)
            {
                text = text[..maxChars];
                var lastSpace = text.LastIndexOf(' ');
                if (lastSpace > maxChars * 3 / 4)
                    text = text[..lastSpace];
                text += $"\n\n... (已截断，原始约 {text.Length * 2} 字符)";
            }

            return string.IsNullOrWhiteSpace(text) ? "（页面无文本内容）" : text.Trim();
        }
        catch (HttpRequestException ex)
        {
            return $"请求失败：{ex.GetType().Name}: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            ErrorLog.ToolError("fetch", $"请求超时（{Config.Instance.FetchTimeoutSec} 秒）");
            return $"错误：请求超时（{Config.Instance.FetchTimeoutSec} 秒）";
        }
        catch (Exception ex)
        {
            return $"抓取错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    // ========================================================================
    // HTML 净化
    // ========================================================================

    /// <summary>
    /// 去除 HTML 标签和噪音元素，提取纯文本。
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // 1. 移除噪音元素（脚本、样式、导航等）
        foreach (var elem in NoisyElements)
        {
            html = Regex.Replace(html, $@"<{elem}[^>]*>.*?</{elem}>", "",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        // 2. 将块级元素替换为换行
        html = Regex.Replace(html, @"</?(div|p|h[1-6]|li|tr|br|hr|section|article)[^>]*/?>", "\n",
            RegexOptions.IgnoreCase);

        // 3. 移除所有 HTML 标签
        html = Regex.Replace(html, @"<[^>]+>", " ");

        // 4. 解码 HTML 实体
        html = System.Net.WebUtility.HtmlDecode(html);

        // 5. 清理空白
        html = Regex.Replace(html, @"\n\s*\n\s*\n", "\n\n");
        html = Regex.Replace(html, @"[ \t]+", " ");

        return html.Trim();
    }

    /// <summary>
    /// HTML → Markdown 简化转换（对标 crush 的 html-to-markdown）。
    /// </summary>
    private static string ConvertToMarkdown(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // 1. 移除噪音元素
        foreach (var elem in NoisyElements)
        {
            html = Regex.Replace(html, $@"<{elem}[^>]*>.*?</{elem}>", "",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        // 2. 保留结构元素 → Markdown
        html = Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", "\n# $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", "\n## $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", "\n### $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<a[^>]*href=""([^""]*)""[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<pre[^>]*><code[^>]*>(.*?)</code></pre>", "\n```\n$1\n```\n", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.Singleline);

        // 3. 块级元素换行
        html = Regex.Replace(html, @"</?(div|p|br|hr|section|article)[^>]*/?>", "\n", RegexOptions.IgnoreCase);

        // 4. 移除剩余标签
        html = Regex.Replace(html, @"<[^>]+>", "");

        // 5. 实体解码
        html = System.Net.WebUtility.HtmlDecode(html);

        // 6. 清理
        html = Regex.Replace(html, @"\n{4,}", "\n\n\n");
        html = Regex.Replace(html, @"[ \t]+", " ");

        return html.Trim();
    }

    // ========================================================================
    // JSON 美化
    // ========================================================================

    private static string PrettyPrintJson(string json, int maxChars)
    {
        try
        {
            var node = Json.Parse(json);
            var pretty = node == null ? json : Json.Serialize(node, indent: true);
            if (pretty.Length > maxChars)
                pretty = pretty[..maxChars] + "\n... (已截断)";
            return pretty;
        }
        catch
        {
            return json.Length > maxChars ? json[..maxChars] + "\n... (已截断)" : json;
        }
    }
}
