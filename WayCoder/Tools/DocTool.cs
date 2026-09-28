using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

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
        L.Pick("查最新库/框架文档。优先于训练数据使用，获取最新 API 和用法。\n" +
        "用法: action='search' + query='库名 问题' 搜索文档；action='fetch' + url='...' 抓取指定页面。",
        "Look up the latest library/framework documentation. Prefer this over training data to get the latest APIs and usage.\n" +
        "Usage: action='search' + query='library question' to search docs; action='fetch' + url='...' to fetch a specific page.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("description", L.Pick("操作类型: 'search' 搜索文档, 'fetch' 抓取指定 URL", "Operation type: 'search' to search docs, 'fetch' to fetch a specific URL"))
                .Set("enum", JNode.Array("search", "fetch")))
            .Set("query", JNode.Param("string", L.Pick("搜索关键词（action=search 时必填），如 'React useEffect cleanup' 或 'Next.js routing'", "Search keywords (required when action=search), e.g. 'React useEffect cleanup' or 'Next.js routing'")))
            .Set("url", JNode.Param("string", L.Pick("要抓取的文档 URL（action=fetch 时必填）", "Documentation URL to fetch (required when action=fetch)"))))
        .Set("required", JNode.Array("action"));

    // 统一 SSRF 安全 handler：ConnectCallback 原子「解析+校验+连接」杜绝 DNS 重绑定；禁自动重定向，
    // 所有请求（search/fetch）都手动跟随并每跳重校验，避免 AllowAutoRedirect 跳转目标绕过 SSRF。
    private static HttpClient _client => _lazyClient.Value;
    private static readonly Lazy<HttpClient> _lazyClient = new(() => new HttpClient(
        SsgfGuard.CreateSafeHandler())
    { Timeout = TimeSpan.FromSeconds(Config.Instance.FetchTimeoutSec) });

    /// <summary>会话级缓存：避免重复查询相同内容</summary>
    private static readonly ConcurrentDictionary<string, (string Result, DateTime Time)> _cache = new();
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
            return L.Pick("错误：action=search 需要提供 query 参数。", "Error: action=search requires the query parameter.");

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
                // 判断**问分类器**，不看字面量（原先 StartsWith("错误")，翻文案即静默失效）
                if (!string.IsNullOrEmpty(content) && !ToolResultClassifier.IsError(content))
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
                : L.Pick($"未找到 '{query}' 的文档。建议：\n" +
                  "1. 尝试更具体的关键词\n" +
                  "2. 使用 action='fetch' + url 直接抓取已知文档页面\n" +
                  "3. 用 web_search 工具搜索更多结果",
                  $"No documentation found for '{query}'. Suggestions:\n" +
                  "1. Try more specific keywords\n" +
                  "2. Use action='fetch' + url to fetch a known documentation page directly\n" +
                  "3. Use the web_search tool to find more results");

            _cache[cacheKey] = (output, DateTime.Now);
            return output;
        }
        catch (Exception ex)
        {
            return L.Pick($"文档搜索失败：{ex.GetType().Name}: {ex.Message}", $"Documentation search failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ================================================================
    // Fetch 模式
    // ================================================================

    private async Task<string> FetchDocAsync(Dictionary<string, object?> arguments)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(url))
            return L.Pick("错误：action=fetch 需要提供 url 参数。", "Error: action=fetch requires the url parameter.");

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return L.Pick("错误：URL 必须以 http:// 或 https:// 开头", "Error: the URL must start with http:// or https://");

        var cacheKey = $"fetch:{url}";
        if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.Time < CacheTtl)
            return cached.Result;

        try
        {
            // SSRF 校验 + 手动跟随重定向（每跳重校验），避免 AllowAutoRedirect 跳转目标绕过校验
            using var response = await FetchWithSsgfCheckAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return L.Pick($"错误：不支持的内容类型 '{contentType}'", $"Error: unsupported content type '{contentType}'");

            var html = await response.Content.ReadAsStringAsync();
            var text = HtmlText.StripHtml(html);

            if (text.Length > 6000)
                text = ContextManager.TruncateByRunes(text, 6000) + L.Pick($"\n\n... (已截断，原始共 {text.Length} 字符)", "(truncated, {text.Length} characters originally)");

            var result = string.IsNullOrWhiteSpace(text)
                ? L.Pick("（页面无文本内容）", "(the page has no text content)")
                : $"### {GetDomain(url)}\n{text}";

            _cache[cacheKey] = (result, DateTime.Now);
            return result;
        }
        catch (HttpRequestException ex)
        {
            return L.Pick($"请求失败：{ex.GetType().Name}: {ex.Message}", $"Request failed: {ex.GetType().Name}: {ex.Message}");
        }
        catch (SsgfBlockedException ex)
        {
            return ToolErrors.Error(ex);
        }
        catch (TaskCanceledException)
        {
            ErrorLog.ToolError("doc", $"请求超时（{Config.Instance.FetchTimeoutSec} 秒）");
            return L.Pick($"错误：请求超时（{Config.Instance.FetchTimeoutSec} 秒）", $"Error: request timed out ({Config.Instance.FetchTimeoutSec}s)");
        }
        catch (Exception ex)
        {
            return ToolErrors.Error(L.Pick("抓取", "fetch "), ex);
        }
    }

    /// <summary>
    /// 手动跟随跳转并对每一跳做 SSRF 校验。
    /// 最终连接由 SsgfGuard.CreateSafeHandler 的 ConnectCallback 原子完成「解析→校验→连接」，
    /// 故这里不再单独 CheckDns（那会再次解析 DNS，重开 DNS 重绑定窗口）；CheckUrl 仅做
    /// 字面量 IP / 特殊主机名 / 内网后缀的提前拒绝（无 DNS 解析，无 TOCTOU）。
    /// </summary>
    private static Task<HttpResponseMessage> FetchWithSsgfCheckAsync(string url)
        // checkDns: false —— 见上方注释（最终连接由 CreateSafeHandler 的 ConnectCallback 原子完成）
        => SsgfRedirect.SendAsync(_client, HttpMethod.Get, url, checkDns: false);

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
            ["react"] = [(L.Pick("React 官方", "React docs"), "https://react.dev/reference/react")],
            ["next.js"] = [(L.Pick("Next.js 官方", "Next.js docs"), "https://nextjs.org/docs")],
            ["nextjs"] = [(L.Pick("Next.js 官方", "Next.js docs"), "https://nextjs.org/docs")],
            ["vue"] = [(L.Pick("Vue 官方", "Vue docs"), "https://vuejs.org/api/")],
            ["svelte"] = [(L.Pick("Svelte 官方", "Svelte docs"), "https://svelte.dev/docs")],
            ["tailwind"] = [("Tailwind CSS", "https://tailwindcss.com/docs")],
            ["prisma"] = [(L.Pick("Prisma 官方", "Prisma docs"), "https://www.prisma.io/docs")],
            ["django"] = [(L.Pick("Django 官方", "Django docs"), "https://docs.djangoproject.com/")],
            ["flask"] = [(L.Pick("Flask 官方", "Flask docs"), "https://flask.palletsprojects.com/")],
            ["fastapi"] = [(L.Pick("FastAPI 官方", "FastAPI docs"), "https://fastapi.tiangolo.com/")],
            ["express"] = [(L.Pick("Express 官方", "Express docs"), "https://expressjs.com/")],
            ["nestjs"] = [(L.Pick("NestJS 官方", "NestJS docs"), "https://docs.nestjs.com/")],
            ["spring"] = [(L.Pick("Spring 官方", "Spring docs"), "https://docs.spring.io/spring-framework/reference/")],
            ["dotnet"] = [(L.Pick(".NET 官方", ".NET docs"), "https://learn.microsoft.com/en-us/dotnet/")],
            ["asp.net"] = [(L.Pick("ASP.NET 官方", "ASP.NET docs"), "https://learn.microsoft.com/en-us/aspnet/core/")],
            ["c#"] = [(L.Pick("C# 文档", "C# docs"), "https://learn.microsoft.com/en-us/dotnet/csharp/")],
            ["rust"] = [(L.Pick("Rust 标准库", "Rust standard library"), "https://doc.rust-lang.org/std/")],
            ["golang"] = [(L.Pick("Go 官方", "Go docs"), "https://pkg.go.dev/")],
            ["go"] = [(L.Pick("Go 官方", "Go docs"), "https://pkg.go.dev/")],
            ["python"] = [(L.Pick("Python 官方", "Python docs"), "https://docs.python.org/3/")],
            ["typescript"] = [(L.Pick("TypeScript 官方", "TypeScript docs"), "https://www.typescriptlang.org/docs/")],
            ["javascript"] = [("MDN", "https://developer.mozilla.org/en-US/docs/Web/JavaScript")],
            ["node"] = [(L.Pick("Node.js 官方", "Node.js docs"), "https://nodejs.org/docs/latest/api/")],
            ["postgresql"] = [(L.Pick("PostgreSQL 官方", "PostgreSQL docs"), "https://www.postgresql.org/docs/current/")],
            ["mysql"] = [(L.Pick("MySQL 官方", "MySQL docs"), "https://dev.mysql.com/doc/refman/8.0/en/")],
            ["redis"] = [(L.Pick("Redis 官方", "Redis docs"), "https://redis.io/docs/latest/")],
            ["docker"] = [(L.Pick("Docker 官方", "Docker docs"), "https://docs.docker.com/reference/")],
            ["kubernetes"] = [(L.Pick("Kubernetes 官方", "Kubernetes docs"), "https://kubernetes.io/docs/")],
            ["k8s"] = [(L.Pick("Kubernetes 官方", "Kubernetes docs"), "https://kubernetes.io/docs/")],
            ["nginx"] = [(L.Pick("NGINX 官方", "NGINX docs"), "https://nginx.org/en/docs/")],
            ["git"] = [(L.Pick("Git 官方", "Git docs"), "https://git-scm.com/docs")],
            ["llm"] = [(L.Pick("LangChain 官方", "LangChain docs"), "https://docs.langchain.com/")],
            ["langchain"] = [(L.Pick("LangChain 官方", "LangChain docs"), "https://docs.langchain.com/")],
            ["pytorch"] = [(L.Pick("PyTorch 官方", "PyTorch docs"), "https://pytorch.org/docs/stable/")],
            ["tensorflow"] = [(L.Pick("TensorFlow 官方", "TensorFlow docs"), "https://www.tensorflow.org/api_docs")],
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
            urls.Add((L.Pick("文档搜索", "Doc search"), $"https://www.google.com/search?q={encoded}+documentation"));
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
            using var response = await FetchWithSsgfCheckAsync(url);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.Contains("text/html") && !contentType.Contains("text/plain"))
                return (source, null);

            var html = await response.Content.ReadAsStringAsync();
            var text = HtmlText.StripHtml(html);

            if (text.Length > 4000)
                text = ContextManager.TruncateByRunes(text, 4000) + L.Pick("\n\n... (已截断)", "\n\n... (truncated)");

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

            using var response = await FetchWithSsgfCheckAsync(url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var text = HtmlText.StripHtml(html);

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
                ? L.Pick($"### 搜索结果: {query}\n" + string.Join("\n", snippets) +
                  "\n\n提示：使用 action='fetch' + 上述 URL 获取完整文档。",
                  $"### Search results: {query}\n" + string.Join("\n", snippets) +
                  "\n\nTip: use action='fetch' with the URLs above to get the full documentation.")
                : L.Pick($"未找到 '{query}' 的搜索结果。", $"No search results found for '{query}'.");
        }
        catch
        {
            return L.Pick($"### 搜索: {query}\n支持直接指定 URL: action='fetch' url='https://docs.example.com/...'", $"### Search: {query}\nYou can also pass a URL directly: action='fetch' url='https://docs.example.com/...'");
        }
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
