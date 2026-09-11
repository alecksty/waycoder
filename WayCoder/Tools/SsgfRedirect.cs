using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 「发送请求并手动跟随重定向、每跳做 SSRF 校验」的**唯一实现**。
///
/// `FetchTool` / `DownloadTool` / `DocTool` 此前各写一份（约 40 行/份，含几乎逐字的 doc 注释），
/// 且已经漂移：**重定向预算是 5 / 10 / 5，无理由地不同**（同一句 `"重定向次数过多"` 之下，
/// download 能吞 10 跳、fetch 只能 5 跳）。现在预算必须显式传，差异摆在调用点上、可审查。
///
/// 每跳都做 SSRF 校验（防重定向到内网 / 云元数据）；拦截时抛 <see cref="SsgfBlockedException"/>
/// （不进网络重试）。重定向后统一改用 GET 并丢弃请求体。
/// </summary>
internal static class SsgfRedirect
{
    /// <summary>默认重定向预算。</summary>
    internal const int DefaultMaxRedirects = 5;

    /// <summary>
    /// 发送请求并跟随重定向。
    /// </summary>
    /// <param name="checkDns">是否每跳额外做 DNS 解析校验。**默认 true**；
    /// DocTool 那条传 false 有明确理由：它的最终连接由 <c>SsgfGuard.CreateSafeHandler</c> 的
    /// ConnectCallback 原子完成「解析→校验→连接」，这里再 CheckDns 会二次解析、重开 DNS 重绑定窗口。</param>
    /// <param name="headers">仅首跳携带（重定向后改 GET，头也随之丢弃——与各调用点原行为一致）。</param>
    /// <param name="body">仅首跳携带。</param>
    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        string? body = null,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead,
        bool checkDns = true,
        int maxRedirects = DefaultMaxRedirects,
        CancellationToken cancellationToken = default)
    {
        var currentUrl = url;
        var currentMethod = method;

        for (var redirect = 0; redirect < maxRedirects; redirect++)
        {
            // SSRF 校验：字面量 IP / 特殊主机名（+ 按需 DNS 解析结果）
            var (safe, reason) = SsgfGuard.CheckUrl(currentUrl);
            if (!safe) throw new SsgfBlockedException(reason!);
            if (checkDns)
            {
                var dns = SsgfGuard.CheckDns(new Uri(currentUrl).Host);
                if (!dns.safe) throw new SsgfBlockedException(dns.reason!);
            }

            var req = new HttpRequestMessage(currentMethod, currentUrl);

            if (redirect == 0)
            {
                // Content-Type 单独走请求体 mediaType，不能加进 request.Headers
                string? bodyContentType = null;
                if (headers != null)
                {
                    foreach (var (k, v) in headers)
                    {
                        if (k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) { bodyContentType = v; continue; }
                        try { req.Headers.TryAddWithoutValidation(k, v); } catch { /* 跳过无效头 */ }
                    }
                }

                if (!string.IsNullOrEmpty(body))
                {
                    req.Content = new StringContent(body, Encoding.UTF8);
                    req.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(bodyContentType ?? "application/json");
                }
            }

            var response = await client.SendAsync(req, completion, cancellationToken);

            if (SsgfGuard.IsRedirect((int)response.StatusCode) && response.Headers.Location != null)
            {
                var nextUri = new Uri(new Uri(currentUrl), response.Headers.Location);
                response.Dispose();
                currentUrl = nextUri.AbsoluteUri;
                currentMethod = HttpMethod.Get; // 重定向后改 GET（丢弃请求体）
                continue;
            }

            return response;
        }

        throw new HttpRequestException("重定向次数过多");
    }

    /// <summary>字符串方法的便捷重载（FetchTool 用）。</summary>
    internal static Task<HttpResponseMessage> SendAsync(
        HttpClient client, string method, string url,
        IReadOnlyDictionary<string, string>? headers = null, string? body = null,
        bool checkDns = true, int maxRedirects = DefaultMaxRedirects,
        CancellationToken cancellationToken = default)
        => SendAsync(client, new HttpMethod(method), url, headers, body,
            HttpCompletionOption.ResponseContentRead, checkDns, maxRedirects, cancellationToken);
}
