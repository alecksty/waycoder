using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace WayCoder.Tools;

/// <summary>
/// Web 搜索工具 — 通过 DuckDuckGo HTML 版进行网页搜索，
/// 无需 API 密钥，返回结果标题、摘要和链接。
/// </summary>
public class WebSearchTool : ITool, ICancellableTool
{
    public string Name => "web_search";
    public string Description => "在互联网上搜索信息，返回结果标题、摘要和链接。主引擎 DuckDuckGo，失败自动回退 Bing（国内可达）。无需 API 密钥。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("query", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索关键词"))
            .Set("num", JNode.Object()
                .Set("type", "integer")
                .Set("description", "返回结果数量（1-10，默认 5）")))
        .Set("required", JNode.Array().Add("query"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时取消在途搜索请求。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return "错误: 请提供搜索关键词 (query)";

        var num = Math.Clamp(ToolArgs.GetInt(arguments, "num", 5), 1, 10);

        // 请求节流（防搜索引擎封 IP）
        await ThrottleAsync(cancellationToken);

        // 主引擎 DuckDuckGo，失败或空结果回退 Bing（国内 DDG 常不可达）
        var results = await SearchWithFallback(query, num, cancellationToken);

        if (results.Count == 0)
            return $"未找到与 \"{query}\" 相关的结果（已尝试 DuckDuckGo + Bing）。";

        // 格式化输出
        var output = new System.Text.StringBuilder();
        output.AppendLine($"🔍 搜索: {query}");
        output.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            output.AppendLine($"{(i + 1)}. {r.Title}");
            output.AppendLine($"   {r.Snippet}");
            output.AppendLine($"   🔗 {r.Url}");
            output.AppendLine();
        }

        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// 测试注入：非空时跳过真实网络，直接请求该地址（{0}=URL 编码后的 query）。
    /// 自测用本地 mock 服务器返回假 HTML，避免依赖外网可达性（国内 DDG 常不可达、Bing 慢，真实搜索会卡满 15s 超时）。
    /// </summary>
    internal static string? OverrideSearchUrl { get; set; }

    /// <summary>
    /// 依次尝试 DuckDuckGo 与 Bing，任一返回结果即停。
    /// </summary>
    private static async Task<List<SearchResult>> SearchWithFallback(string query, int num, CancellationToken cancellationToken)
    {
        // 测试注入：单引擎直连本地 mock，快且确定
        if (!string.IsNullOrEmpty(OverrideSearchUrl))
            return await TrySearch(query, num, "Mock",
                OverrideSearchUrl + "?q={0}", ParseBingResults, cancellationToken) ?? [];

        var ddg = await TrySearch(query, num, "DuckDuckGo",
            "https://html.duckduckgo.com/html/?q={0}", ParseDuckDuckGoResults, cancellationToken);
        if (ddg is { Count: > 0 }) return ddg;

        var bing = await TrySearch(query, num, "Bing",
            "https://www.bing.com/search?q={0}", ParseBingResults, cancellationToken);
        if (bing is { Count: > 0 }) return bing;

        return [];
    }

    /// <summary>
    /// 抓取单个搜索引擎结果。任何异常都返回 null（由上层回退），并记录错误日志。
    /// </summary>
    private static async Task<List<SearchResult>?> TrySearch(
        string query, int num, string engine, string urlTemplate,
        Func<string, int, List<SearchResult>> parser, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);

            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = string.Format(urlTemplate, encodedQuery);
            var html = await client.GetStringAsync(url, cancellationToken);
            return parser(html, num);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // 中断信号向上传播
        }
        catch (TaskCanceledException)
        {
            ErrorLog.ToolError("web_search", $"{engine} 搜索超时（15 秒）");
            return null;
        }
        catch (HttpRequestException ex)
        {
            ErrorLog.ToolError("web_search", $"{engine} 请求失败: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            ErrorLog.ToolError("web_search", $"{engine} 异常: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>请求节流：相邻请求间隔 ≥2 秒，防止搜索引擎封 IP。</summary>
    private static long _lastRequestTicks;

    private static async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        var minTicks = TimeSpan.FromSeconds(2).Ticks;
        var last = Interlocked.Read(ref _lastRequestTicks);
        if (last != 0)
        {
            var wait = minTicks - (DateTime.UtcNow.Ticks - last);
            if (wait > 0)
                await Task.Delay(TimeSpan.FromTicks(wait), cancellationToken);
        }
        Interlocked.Exchange(ref _lastRequestTicks, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// 解析 DuckDuckGo HTML 搜索结果。
    /// </summary>
    internal static List<SearchResult> ParseDuckDuckGoResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        // DuckDuckGo HTML 版的结果在 <a class="result__a"> 和 <a class="result__snippet"> 中
        var blockPattern = new Regex(
            @"<a[^>]*class=""result__a""[^>]*href=""([^""]+)""[^>]*>([^<]+)</a>.*?<a[^>]*class=""result__snippet""[^>]*>([^<]*)</a>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var matches = blockPattern.Matches(html);
        foreach (Match m in matches)
        {
            if (results.Count >= maxResults) break;

            var url = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
            var title = HtmlText.StripHtml(m.Groups[2].Value.Trim(), stripNoise: false);
            var snippet = HtmlText.StripHtml(m.Groups[3].Value.Trim(), stripNoise: false);

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
            {
                results.Add(new SearchResult
                {
                    Title = HttpUtility.HtmlDecode(title),
                    Url = url,
                    Snippet = HttpUtility.HtmlDecode(snippet)
                });
            }
        }

        // 备用解析：有些结果可能用不同的 class
        if (results.Count == 0)
        {
            var altPattern = new Regex(
                @"<a[^>]*href=""(https?://[^""]+)""[^>]*class=""[^""]*result[^""]*""[^>]*>([^<]*)</a>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var altMatches = altPattern.Matches(html);
            foreach (Match m in altMatches)
            {
                if (results.Count >= maxResults) break;
                var url = m.Groups[1].Value.Trim();
                var title = HtmlText.StripHtml(m.Groups[2].Value.Trim(), stripNoise: false);
                if (!string.IsNullOrWhiteSpace(title) && !url.Contains("duckduckgo.com"))
                {
                    results.Add(new SearchResult
                    {
                        Title = HttpUtility.HtmlDecode(title),
                        Url = url,
                        Snippet = ""
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 解析 Bing 搜索结果（备用引擎）。
    /// Bing 结果在 &lt;li class="b_algo"&gt; 中，标题在 &lt;h2&gt;&lt;a&gt;，摘要在其后的 &lt;p&gt;。
    /// </summary>
    internal static List<SearchResult> ParseBingResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        var blockPattern = new Regex(
            @"<li\s+class=""b_algo"".*?<h2[^>]*>.*?<a[^>]*href=""([^""]+)""[^>]*>(.*?)</a>.*?</h2>.*?(?:<p[^>]*>(.*?)</p>)?",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match m in blockPattern.Matches(html))
        {
            if (results.Count >= maxResults) break;

            var url = HttpUtility.HtmlDecode(m.Groups[1].Value.Trim());
            var title = HttpUtility.HtmlDecode(HtmlText.StripHtml(m.Groups[2].Value.Trim(), stripNoise: false));
            var snippet = HttpUtility.HtmlDecode(HtmlText.StripHtml(m.Groups[3].Value.Trim(), stripNoise: false));

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)) continue;
            if (url.Contains("bing.com") || url.Contains("microsoft.com/bing")) continue;

            results.Add(new SearchResult { Title = title, Url = url, Snippet = snippet });
        }

        return results;
    }

    internal class SearchResult
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Snippet { get; set; } = "";
    }
}
