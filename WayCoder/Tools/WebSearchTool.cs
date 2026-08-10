using System.Text.RegularExpressions;
using System.Web;

namespace WayCoder.Tools;

/// <summary>
/// Web 搜索工具 — 通过 DuckDuckGo HTML 版进行网页搜索，
/// 无需 API 密钥，返回结果标题、摘要和链接。
/// </summary>
public class WebSearchTool : ITool
{
    public string Name => "web_search";
    public string Description => "在互联网上搜索信息（通过 DuckDuckGo），返回结果标题、摘要和链接。无需 API 密钥。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "搜索关键词"
            },
            ["num"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "返回结果数量（1-10，默认 5）"
            }
        },
        ["required"] = new JsonArray("query")
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return "错误: 请提供搜索关键词 (query)";

        var num = 5;
        if (arguments.TryGetValue("num", out var numVal) && numVal != null)
        {
            try { num = Math.Clamp(Convert.ToInt32(numVal), 1, 10); }
            catch { }
        }

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = $"https://html.duckduckgo.com/html/?q={encodedQuery}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetStringAsync(url);
            var results = ParseDuckDuckGoResults(response, num);

            if (results.Count == 0)
                return $"未找到与 \"{query}\" 相关的结果。";

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
        catch (TaskCanceledException)
        {
            return "错误: 搜索超时（15 秒），请稍后重试。";
        }
        catch (HttpRequestException ex)
        {
            return $"错误: 网络请求失败 — {ex.GetType().Name}: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"错误: 搜索异常 — {ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 解析 DuckDuckGo HTML 搜索结果。
    /// </summary>
    private static List<SearchResult> ParseDuckDuckGoResults(string html, int maxResults)
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
            var title = StripHtml(m.Groups[2].Value.Trim());
            var snippet = StripHtml(m.Groups[3].Value.Trim());

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
                var title = StripHtml(m.Groups[2].Value.Trim());
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

    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var result = Regex.Replace(input, @"<[^>]+>", "");
        result = Regex.Replace(result, @"\s+", " ");
        return result.Trim();
    }

    private class SearchResult
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Snippet { get; set; } = "";
    }
}
