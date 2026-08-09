using System.Text;
using System.Text.RegularExpressions;

namespace CoreCoderSharp.Tools;

/// <summary>
/// 文档查询工具 —— 查最新库/框架文档。
/// 通过 web 搜索 + 页面抓取获取最新文档内容，弥补 LLM 训练数据时效性。
///
/// 两个子命令：
///   action: "search" — 搜索库/框架文档
///   action: "fetch"  — 抓取指定文档页面
/// </summary>
public class DocTool : ITool
{
    public string Name => "doc";
    public string Description =>
        "查最新库/框架文档。优先于训练数据使用，获取最新 API 和用法。\n" +
        "用法: action='search' + query='库名 问题' 搜索文档；action='fetch' + url='...' 抓取指定页面。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["action"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "操作类型: 'search' 搜索文档, 'fetch' 抓取指定 URL",
                ["enum"] = new JsonArray("search", "fetch"),
            },
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "搜索关键词（action=search 时必填），如 'React useEffect cleanup' 或 'Next.js routing'",
            },
            ["url"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要抓取的文档 URL（action=fetch 时必填）",
            },
        },
        ["required"] = new JsonArray("action"),
    };

    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(25),
    };

    /// <summary>会话级缓存：避免重复查询相同内容</summary>
    private static readonly Dictionary<string, (string Result, DateTime Time)> _cache = [];
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    static DocTool()
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 WayCoder/1.0 (compatible; AI coding assistant; doc lookup)");
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "search";

        return action switch
        {
            "fetch" => await FetchDocAsync(arguments),
            _ => await SearchDocAsync(arguments),
        };
    }

    // ================================================================
    // Search 模式
    // ================================================================

    private async Task<string> SearchDocAsync(Dictionary<string, object?> arguments)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(query))
            return "错误：action=search 需要提供 query 参数。";

        var cacheKey = $"search:{query}";
        if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.Time < CacheTtl)
            return cached.Result;

        try
        {
            // 优先查官方文档站点（docs.rs, devdocs.io, 官方域名）
            var results = new List<string>();
            var sources = new HashSet<string>();

            // 尝试抓取多个文档源
            var tasks = new List<Task<(string Source, string? Content)>>();

            // 检测查询中的关键词，尝试定向查询
            var qLower = query.ToLowerInvariant();
            var docUrls = SuggestDocUrls(query);

            foreach (var (source, url) in docUrls)
            {
                tasks.Add(FetchDocUrlAsync(source, url));
            }

            // 等待所有请求（任一成功即返回）
            var fetchResults = await Task.WhenAll(tasks);
            foreach (var (source, content) in fetchResults)
            {
                if (!string.IsNullOrEmpty(content) && !content.StartsWith("错误"))
                {
                    results.Add($"### {source}\n{content}");
                    sources.Add(source);
                }
            }

            if (results.Count == 0)
            {
                // 回退：用通用搜索
                var searchResult = await WebSearchAsync(query);
                if (!string.IsNullOrEmpty(searchResult))
                    results.Add(searchResult);
            }

            var output = results.Count > 0
                ? string.Join("\n\n---\n\n", results)
                : $"未找到 '{query}' 的文档。建议：\n" +
                  "1. 尝试更具体的关键词\n" +
                  "2. 使用 action='fetch' + url 直接抓取已知文档页面\n" +
                  "3. 用 web_search 工具搜索更多结果";

            _cache[cacheKey] = (output, DateTime.Now);
            return output;
        }
        catch (Exception ex)
        {
            return $"文档搜索失败：{ex.Message}";
        }
    }

    // ================================================================
    // Fetch 模式
    // ================================================================

    private async Task<string> FetchDocAsync(Dictionary<string, object?> arguments)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(url))
            return "错误：action=fetch 需要提供 url 参数。";

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return "错误：URL 必须以 http:// 或 https:// 开头";

        var cacheKey = $"fetch:{url}";
        if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.Time < CacheTtl)
            return cached.Result;

        try
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return $"错误：不支持的内容类型 '{contentType}'";

            var html = await response.Content.ReadAsStringAsync();
            var text = StripHtml(html);

            if (text.Length > 6000)
                text = text[..6000] + $"\n\n... (已截断，原始共 {text.Length} 字符)";

            var result = string.IsNullOrWhiteSpace(text)
                ? "（页面无文本内容）"
                : $"### {GetDomain(url)}\n{text}";

            _cache[cacheKey] = (result, DateTime.Now);
            return result;
        }
        catch (HttpRequestException ex)
        {
            return $"请求失败：{ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "错误：请求超时（25 秒）";
        }
        catch (Exception ex)
        {
            return $"抓取错误：{ex.Message}";
        }
    }

    // ================================================================
    // 文档源建议
    // ================================================================

    private static List<(string Source, string Url)> SuggestDocUrls(string query)
    {
        var urls = new List<(string, string)>();
        var q = query.ToLowerInvariant();

        // 常见库 → 官方文档 URL 映射
        var knownDocs = new Dictionary<string, List<(string, string)>>
        {
            ["react"] = [("React 官方", "https://react.dev/reference/react")],
            ["next.js"] = [("Next.js 官方", "https://nextjs.org/docs")],
            ["nextjs"] = [("Next.js 官方", "https://nextjs.org/docs")],
            ["vue"] = [("Vue 官方", "https://vuejs.org/api/")],
            ["svelte"] = [("Svelte 官方", "https://svelte.dev/docs")],
            ["tailwind"] = [("Tailwind CSS", "https://tailwindcss.com/docs")],
            ["prisma"] = [("Prisma 官方", "https://www.prisma.io/docs")],
            ["django"] = [("Django 官方", "https://docs.djangoproject.com/")],
            ["flask"] = [("Flask 官方", "https://flask.palletsprojects.com/")],
            ["fastapi"] = [("FastAPI 官方", "https://fastapi.tiangolo.com/")],
            ["express"] = [("Express 官方", "https://expressjs.com/")],
            ["nestjs"] = [("NestJS 官方", "https://docs.nestjs.com/")],
            ["spring"] = [("Spring 官方", "https://docs.spring.io/spring-framework/reference/")],
            ["dotnet"] = [(".NET 官方", "https://learn.microsoft.com/en-us/dotnet/")],
            ["asp.net"] = [("ASP.NET 官方", "https://learn.microsoft.com/en-us/aspnet/core/")],
            ["c#"] = [("C# 文档", "https://learn.microsoft.com/en-us/dotnet/csharp/")],
            ["rust"] = [("Rust 标准库", "https://doc.rust-lang.org/std/")],
            ["golang"] = [("Go 官方", "https://pkg.go.dev/")],
            ["go"] = [("Go 官方", "https://pkg.go.dev/")],
            ["python"] = [("Python 官方", "https://docs.python.org/3/")],
            ["typescript"] = [("TypeScript 官方", "https://www.typescriptlang.org/docs/")],
            ["javascript"] = [("MDN", "https://developer.mozilla.org/en-US/docs/Web/JavaScript")],
            ["node"] = [("Node.js 官方", "https://nodejs.org/docs/latest/api/")],
            ["postgresql"] = [("PostgreSQL 官方", "https://www.postgresql.org/docs/current/")],
            ["mysql"] = [("MySQL 官方", "https://dev.mysql.com/doc/refman/8.0/en/")],
            ["redis"] = [("Redis 官方", "https://redis.io/docs/latest/")],
            ["docker"] = [("Docker 官方", "https://docs.docker.com/reference/")],
            ["kubernetes"] = [("Kubernetes 官方", "https://kubernetes.io/docs/")],
            ["k8s"] = [("Kubernetes 官方", "https://kubernetes.io/docs/")],
            ["nginx"] = [("NGINX 官方", "https://nginx.org/en/docs/")],
            ["git"] = [("Git 官方", "https://git-scm.com/docs")],
            ["llm"] = [("LangChain 官方", "https://docs.langchain.com/")],
            ["langchain"] = [("LangChain 官方", "https://docs.langchain.com/")],
            ["pytorch"] = [("PyTorch 官方", "https://pytorch.org/docs/stable/")],
            ["tensorflow"] = [("TensorFlow 官方", "https://www.tensorflow.org/api_docs")],
        };

        foreach (var (keyword, sources) in knownDocs)
        {
            if (q.Contains(keyword))
            {
                foreach (var src in sources)
                    urls.Add(src);
                if (urls.Count >= 2) break; // 最多 2 个源
            }
        }

        // 通用：从查询中提取可能的搜索引擎查询
        if (urls.Count == 0)
        {
            var encoded = Uri.EscapeDataString(query);
            urls.Add(("文档搜索", $"https://www.google.com/search?q={encoded}+documentation"));
        }

        return urls;
    }

    // ================================================================
    // HTTP 帮助方法
    // ================================================================

    private async Task<(string Source, string? Content)> FetchDocUrlAsync(string source, string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return (source, null);

            var html = await response.Content.ReadAsStringAsync();
            var text = StripHtml(html);

            if (text.Length > 4000)
                text = text[..4000] + "\n\n... (已截断)";

            return (source, string.IsNullOrWhiteSpace(text) ? null : text);
        }
        catch
        {
            return (source, null);
        }
    }

    private async Task<string> WebSearchAsync(string query)
    {
        try
        {
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://www.google.com/search?q={encoded}+documentation";

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var text = StripHtml(html);

            // 提取搜索结果片段
            var snippets = new List<string>();
            var matches = Regex.Matches(text, @"https?://[^\s]+");
            var seen = new HashSet<string>();

            foreach (Match m in matches)
            {
                var link = m.Value;
                if (seen.Contains(link)) continue;
                seen.Add(link);

                // 过滤非文档链接
                if (link.Contains("google") || link.Contains("youtube") ||
                    link.Contains("facebook") || link.Contains("twitter") ||
                    link.Length > 200) continue;

                snippets.Add($"- {link}");
                if (snippets.Count >= 8) break;
            }

            return snippets.Count > 0
                ? $"### 搜索结果: {query}\n" + string.Join("\n", snippets) +
                  "\n\n提示：使用 action='fetch' + 上述 URL 获取完整文档。"
                : $"未找到 '{query}' 的搜索结果。";
        }
        catch
        {
            return $"### 搜索: {query}\n支持直接指定 URL: action='fetch' url='https://docs.example.com/...'";
        }
    }

    // ================================================================
    // HTML 清理
    // ================================================================

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<nav[^>]*>.*?</nav>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<footer[^>]*>.*?</footer>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<header[^>]*>.*?</header>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", " ");
        html = System.Net.WebUtility.HtmlDecode(html);
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n\s*\n", "\n\n");
        html = Regex.Replace(html, @"^\s+$", "", RegexOptions.Multiline);

        return html.Trim();
    }

    private static string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch { return url; }
    }
}
