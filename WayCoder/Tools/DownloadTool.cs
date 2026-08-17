namespace WayCoder.Tools;

/// <summary>
/// 下载 URL 到本地文件的工具。
/// 对应 Crush 的 download 工具。
/// </summary>
public class DownloadTool : ITool, ICancellableTool
{
    public string Name => "download";
    public string Description => "将 URL 的内容下载到本地文件。用于获取远程资源、下载依赖文件或保存外部数据。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("url", JNode.Object()
                .Set("type", "string")
                .Set("description", "要下载的 URL（仅支持 http/https）"))
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "保存下载内容的本地文件路径（绝对路径或相对于当前目录）"))
            .Set("timeout", JNode.Object()
                .Set("type", "integer")
                .Set("description", "下载超时时间，单位秒（默认 60，最大 600）")))
        .Set("required", JNode.Array().Add("url").Add("file_path"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时取消在途下载。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var timeout = Math.Clamp(ToolArgs.GetInt(arguments, "timeout", Config.Instance.DownloadTimeoutSec), 1, 600);

        // 安全检查：仅允许 http/https
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return $"错误：仅支持 HTTP/HTTPS URL，不支持: {url}";

        // 安全检查：拒绝本地文件
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return "错误：出于安全原因，不允许下载本地文件";

        // 将相对路径转为绝对路径
        if (string.IsNullOrWhiteSpace(filePath))
            return "错误：file_path 不能为空 — 请提供有效的文件路径。";
        if (!Path.IsPathRooted(filePath))
            filePath = Path.GetFullPath(filePath, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

        // 确保目标目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,  // 手动跟随重定向，每跳做 SSRF 校验
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeout),
            };

            // 设置 User-Agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");

            // 先发送 HEAD 请求获取内容长度（SSRF 校验在 SendWithRedirectAsync 内）
            long? totalSize = null;
            try
            {
                using var headResponse = await SendWithRedirectAsync(client, HttpMethod.Head, url, cancellationToken);
                totalSize = headResponse.Content.Headers.ContentLength;
            }
            catch (SsgfBlockedException)
            {
                throw; // SSRF 拦截向上抛出
            }
            catch
            {
                // HEAD 请求失败不影响后续下载
            }

            // 下载文件（网络故障带指数退避重试，仅 HttpRequestException，超时不重试）
            using var response = await RetryPolicy.RetryAsync(
                () => SendWithRedirectAsync(client, HttpMethod.Get, url, cancellationToken),
                new RetryConfig
                {
                    MaxRetries = 2,
                    BaseDelayMs = 500,
                    MaxDelayMs = 3000,
                    RetryableExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "System.Net.Http.HttpRequestException" },
                });
            response.EnsureSuccessStatusCode();

            // 检查响应大小（超过 500MB 拒绝下载）
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > 500 * 1024 * 1024)
                return $"错误：文件过大（{contentLength / 1024.0 / 1024.0:F1} MB），拒绝下载超过 500 MB 的文件";

            // 读取并写入文件：流式累计字节数——无 Content-Length 的 chunked 响应也要受 500MB 上限约束，
            // 否则可无界写盘耗尽磁盘
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(filePath);
            var buffer = new byte[81920];
            long written = 0;
            while (true)
            {
                int read = await stream.ReadAsync(buffer, cancellationToken);
                if (read <= 0) break;
                written += read;
                if (written > 500L * 1024 * 1024)
                {
                    try { fileStream.Dispose(); File.Delete(filePath); } catch { }
                    return $"错误：下载超过 500 MB 上限（{written / 1024.0 / 1024.0:F1} MB），已中止";
                }
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);
            var sizeStr = fileInfo.Length switch
            {
                < 1024 => $"{fileInfo.Length} B",
                < 1024 * 1024 => $"{fileInfo.Length / 1024.0:F1} KB",
                _ => $"{fileInfo.Length / 1024.0 / 1024.0:F2} MB",
            };

            return $"✅ 下载完成：{url}\n保存至：{filePath}\n大小：{sizeStr}";
        }
        catch (SsgfBlockedException ex)
        {
            return $"错误：{ex.Message}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 中断信号：清理未完成文件后向上传播
            try { File.Delete(filePath); } catch { }
            throw;
        }
        catch (TaskCanceledException)
        {
            // 清理未完成的文件
            try { File.Delete(filePath); } catch { }
            return $"错误：下载超时（{timeout} 秒）";
        }
        catch (HttpRequestException ex)
        {
            try { File.Delete(filePath); } catch { }
            return $"错误：HTTP 请求失败 — {ex.Message}";
        }
        catch (Exception ex)
        {
            try { File.Delete(filePath); } catch { }
            return $"错误：下载失败 — {ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 发送请求并手动跟随重定向，每跳做 SSRF 校验（防重定向到内网/云元数据）。
    /// SSRF 拦截时抛 <see cref="SsgfBlockedException"/>（不进入网络重试）。
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithRedirectAsync(HttpClient client, HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var currentUrl = url;
        var currentMethod = method;

        for (var redirect = 0; redirect < 10; redirect++)
        {
            var (safe, reason) = SsgfGuard.CheckUrl(currentUrl);
            if (!safe) throw new SsgfBlockedException(reason!);
            var dns = SsgfGuard.CheckDns(new Uri(currentUrl).Host);
            if (!dns.safe) throw new SsgfBlockedException(dns.reason!);

            var response = await client.SendAsync(
                new HttpRequestMessage(currentMethod, currentUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (SsgfGuard.IsRedirect((int)response.StatusCode) && response.Headers.Location != null)
            {
                var nextUri = new Uri(new Uri(currentUrl), response.Headers.Location);
                response.Dispose();
                currentUrl = nextUri.AbsoluteUri;
                currentMethod = HttpMethod.Get; // download 重定向后改 GET
                continue;
            }

            return response;
        }

        throw new HttpRequestException("重定向次数过多");
    }
}
