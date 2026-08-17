using System.Text;
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
public class FetchTool : ITool, ICancellableTool
{
    public string Name => "fetch";
    public string Description => "抓取网页 URL 的内容，自动提取纯文本或 Markdown（去除 HTML 噪音）。支持 GET/POST/PUT/DELETE/PATCH/HEAD/OPTIONS 方法、自定义 headers 与 body，可调用 REST API。用于查阅文档、阅读文章、获取最新信息、调用接口。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("url", JNode.Object()
                .Set("type", "string")
                .Set("description", "要抓取的网页 URL (http/https)"))
            .Set("max_chars", JNode.Object()
                .Set("type", "integer")
                .Set("description", "最大返回字符数（默认 8000，最大 100000）"))
            .Set("format", JNode.Object()
                .Set("type", "string")
                .Set("description", "输出格式：'text'（纯文本）或 'markdown'（结构化），默认 'text'"))
            .Set("method", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("GET").Add("POST").Add("PUT").Add("DELETE").Add("PATCH").Add("HEAD").Add("OPTIONS"))
                .Set("description", "HTTP 方法，默认 GET。POST/PUT/DELETE 等用于调用 API"))
            .Set("headers", JNode.Object()
                .Set("type", "string")
                .Set("description", "请求头，JSON 对象字符串，如 {\"Authorization\":\"Bearer xxx\",\"Content-Type\":\"application/json\"}"))
            .Set("body", JNode.Object()
                .Set("type", "string")
                .Set("description", "请求体（POST/PUT/PATCH 时用），默认按 application/json 发送")))
        .Set("required", JNode.Array().Add("url"));

    private static HttpClient _client => _lazyClient.Value;
    private static readonly Lazy<HttpClient> _lazyClient = new(() => new HttpClient(
        new HttpClientHandler { AllowAutoRedirect = false })  // 手动跟随重定向，每跳做 SSRF 校验
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
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时取消在途 HTTP 请求。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        var maxChars = Math.Clamp(ToolArgs.GetInt(arguments, "max_chars", 8000), 1, 100_000);
        var format = arguments.TryGetValue("format", out var fmt) ? fmt?.ToString()?.ToLowerInvariant() : "text";
        var method = arguments.GetValueOrDefault("method")?.ToString() ?? "GET";
        var headers = ParseHeaders(arguments.GetValueOrDefault("headers")?.ToString());
        var body = arguments.GetValueOrDefault("body")?.ToString();

        return await Execute(url, maxChars, format ?? "text", method, headers, body, cancellationToken);
    }

    private static async Task<string> Execute(string url, int maxChars, string format, string method, Dictionary<string, string>? headers, string? body, CancellationToken cancellationToken)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return "错误：URL 必须以 http:// 或 https:// 开头";

        var methodUpper = method.Trim().ToUpperInvariant();
        if (methodUpper is not ("GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "HEAD" or "OPTIONS"))
            return $"错误：不支持的 HTTP 方法 '{method}'（支持 GET/POST/PUT/DELETE/PATCH/HEAD/OPTIONS）";

        try
        {
            // 网络请求带指数退避重试（仅 HttpRequestException 网络故障，超时不重试）
            // 每次重试重新构造 HttpRequestMessage，避免 Content 被消费后复用
            using var response = await RetryPolicy.RetryAsync(() =>
                SendWithRedirectAsync(methodUpper, url, headers, body, cancellationToken), new RetryConfig
            {
                MaxRetries = 2,
                BaseDelayMs = 500,
                MaxDelayMs = 3000,
                RetryableExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "System.Net.Http.HttpRequestException" },
            });

            // HEAD 请求无响应体，仅返回状态码与原因短语
            if (methodUpper == "HEAD")
                return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            // JSON 特殊处理
            if (contentType.Contains("json"))
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return PrettyPrintJson(json, maxChars);
            }

            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return $"错误：不支持的内容类型 '{contentType}'（仅支持 HTML/纯文本/JSON）";

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var text = format == "markdown" ? ConvertToMarkdown(html) : StripHtml(html);

            // 压缩空白（合并连续空行）
            text = Regex.Replace(text, @"\n{4,}", "\n\n\n");
            text = Regex.Replace(text, @"[ \t]{2,}", " ");

            var originalLen = text.Length;

            if (text.Length > maxChars)
            {
                text = ContextManager.TruncateByRunes(text, maxChars);
                var lastSpace = text.LastIndexOf(' ');
                if (lastSpace > maxChars * 3 / 4)
                    text = text[..lastSpace];
                text += $"\n\n... (已截断，原始约 {originalLen} 字符)";
            }

            return string.IsNullOrWhiteSpace(text) ? "（页面无文本内容）" : text.Trim();
        }
        catch (SsgfBlockedException ex)
        {
            return $"错误：{ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            return $"请求失败：{ex.GetType().Name}: {ex.Message}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // 中断信号（Web 停止 / Ctrl+C），向上传播，不吞掉
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

    /// <summary>
    /// 发送请求并手动跟随重定向，每跳做 SSRF 校验（防重定向到内网/云元数据）。
    /// SSRF 拦截时抛 <see cref="SsgfBlockedException"/>（不进入网络重试）。
    /// 重定向后统一改用 GET（丢弃请求体），fetch 场景重定向目标多为网页。
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithRedirectAsync(
        string method, string url, Dictionary<string, string>? headers, string? body, CancellationToken cancellationToken)
    {
        var currentUrl = url;
        var currentMethod = method;

        for (var redirect = 0; redirect < 5; redirect++)
        {
            // SSRF 校验：字面量 IP / 特殊主机名 + DNS 解析结果
            var (safe, reason) = SsgfGuard.CheckUrl(currentUrl);
            if (!safe) throw new SsgfBlockedException(reason!);
            var dns = SsgfGuard.CheckDns(new Uri(currentUrl).Host);
            if (!dns.safe) throw new SsgfBlockedException(dns.reason!);

            var req = new HttpRequestMessage(new HttpMethod(currentMethod), currentUrl);

            // 请求头（Content-Type 单独走请求体 mediaType，不能加进 request.Headers）
            string? bodyContentType = null;
            if (headers != null)
            {
                foreach (var (k, v) in headers)
                {
                    if (k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) { bodyContentType = v; continue; }
                    try { req.Headers.TryAddWithoutValidation(k, v); } catch { /* 跳过无效头 */ }
                }
            }

            // 请求体（仅首次请求；重定向后改 GET 丢弃 body）
            if (!string.IsNullOrEmpty(body) && redirect == 0)
            {
                req.Content = new StringContent(body, Encoding.UTF8);
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(bodyContentType ?? "application/json");
            }

            var response = await _client.SendAsync(req, cancellationToken);

            if (SsgfGuard.IsRedirect((int)response.StatusCode) && response.Headers.Location != null)
            {
                var nextUri = new Uri(new Uri(currentUrl), response.Headers.Location);
                response.Dispose();
                currentUrl = nextUri.AbsoluteUri;
                currentMethod = "GET";
                continue;
            }

            return response;
        }

        throw new HttpRequestException("重定向次数过多");
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
    // 请求头解析
    // ========================================================================

    /// <summary>解析 headers 的 JSON 对象字符串 → 字典。非法 JSON 或空返回 null。</summary>
    internal static Dictionary<string, string>? ParseHeaders(string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson)) return null;
        try
        {
            var node = Json.Parse(headersJson);
            if (node == null) return null;
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in node.Entries)
            {
                var v = value.AsString();
                if (v != null) dict[key] = v;
            }
            return dict.Count > 0 ? dict : null;
        }
        catch
        {
            return null;
        }
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
                pretty = ContextManager.TruncateByRunes(pretty, maxChars) + "\n... (已截断)";
            return pretty;
        }
        catch
        {
            return json.Length > maxChars ? ContextManager.TruncateByRunes(json, maxChars) + "\n... (已截断)" : json;
        }
    }
}
