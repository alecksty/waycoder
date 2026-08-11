using System.Text.Json.Nodes;

namespace WayCoder.Tools;

/// <summary>
/// 下载 URL 到本地文件的工具。
/// 对应 Crush 的 download 工具。
/// </summary>
public class DownloadTool : ITool
{
    public string Name => "download";
    public string Description => "将 URL 的内容下载到本地文件。用于获取远程资源、下载依赖文件或保存外部数据。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["url"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要下载的 URL（仅支持 http/https）",
            },
            ["file_path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "保存下载内容的本地文件路径（绝对路径或相对于当前目录）",
            },
            ["timeout"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "下载超时时间，单位秒（默认 60，最大 600）",
            },
        },
        ["required"] = new JsonArray("url", "file_path"),
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var url = arguments.GetValueOrDefault("url")?.ToString() ?? "";
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var timeout = arguments.TryGetValue("timeout", out var t) && t is int timeoutVal
            ? Math.Clamp(timeoutVal, 1, 600)
            : 60;

        // 安全检查：仅允许 http/https
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return $"错误：仅支持 HTTP/HTTPS URL，不支持: {url}";

        // 安全检查：拒绝本地文件
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return "错误：出于安全原因，不允许下载本地文件";

        // 将相对路径转为绝对路径
        if (!Path.IsPathRooted(filePath))
            filePath = Path.GetFullPath(filePath);

        // 确保目标目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeout),
            };

            // 设置 User-Agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");

            // 先发送 HEAD 请求获取内容长度
            long? totalSize = null;
            try
            {
                var headResponse = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, url));
                totalSize = headResponse.Content.Headers.ContentLength;
            }
            catch
            {
                // HEAD 请求失败不影响后续下载
            }

            // 下载文件
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // 检查响应大小（超过 500MB 拒绝下载）
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > 500 * 1024 * 1024)
                return $"错误：文件过大（{contentLength / 1024.0 / 1024.0:F1} MB），拒绝下载超过 500 MB 的文件";

            // 读取并写入文件
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            var fileInfo = new FileInfo(filePath);
            var sizeStr = fileInfo.Length switch
            {
                < 1024 => $"{fileInfo.Length} B",
                < 1024 * 1024 => $"{fileInfo.Length / 1024.0:F1} KB",
                _ => $"{fileInfo.Length / 1024.0 / 1024.0:F2} MB",
            };

            return $"✅ 下载完成：{url}\n保存至：{filePath}\n大小：{sizeStr}";
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
}
