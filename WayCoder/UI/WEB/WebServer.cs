using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WayCoder.UI.Web;

/// <summary>请求正文（或请求头）超过 <see cref="HttpServer.MaxRequestBytes"/> 时抛出，触发 413 响应。</summary>
internal sealed class RequestTooLargeException : Exception
{
    public RequestTooLargeException() : base("request too large") { }
}

/// <summary>解析后的 HTTP 请求。</summary>
public sealed class HttpRequest
{
    public string Method = "";
    public string Path = "";
    public string Query = "";
    public string Version = "";
    public Dictionary<string, string> Headers = new(StringComparer.OrdinalIgnoreCase);
    public string Body = "";
    /// <summary>原始正文字节（文本请求与 <see cref="Body"/> 等价；二进制上传走这里，避免 UTF-8 解码损坏）。</summary>
    public byte[] RawBody = Array.Empty<byte>();

    public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;
}

/// <summary>HTTP 响应（服务器负责序列化为 HTTP/1.1 报文）。</summary>
public sealed class HttpResponse
{
    public int Status = 200;
    public string Reason = "OK";
    public string ContentType = "text/plain; charset=utf-8";
    public byte[] Body = Array.Empty<byte>();

    public static HttpResponse Text(string body, string contentType = "text/plain; charset=utf-8")
        => new() { ContentType = contentType, Body = Encoding.UTF8.GetBytes(body) };

    public static HttpResponse Html(string body) => Text(body, "text/html; charset=utf-8");

    public static HttpResponse JsonBody(string body) => Text(body, "application/json; charset=utf-8");

    public static HttpResponse Empty() => new();

    public static HttpResponse NotFound()
        => new() { Status = 404, Reason = "Not Found", Body = Encoding.UTF8.GetBytes("404 Not Found") };
}

/// <summary>
/// 手搓 HTTP/1.1 服务端（纯 BCL，AOT 安全，零反射）——对标 deepseek-harness 的 web 服务。
/// 基于 TcpListener，每连接一个 Task；普通请求走 <see cref="OnRequest"/>，SSE 长连接走 <see cref="OnSse"/>。
/// </summary>
public sealed class HttpServer
{
    /// <summary>请求（头 + 正文）累计大小上限，防内存耗尽（OOM）攻击。</summary>
    public const int MaxRequestBytes = 1_048_576; // 1 MB

    /// <summary>请求头部分上限（正文另计，普通请求正文仍受 <see cref="MaxRequestBytes"/> 约束）。</summary>
    public const int MaxHeaderBytes = 64 * 1024; // 64 KB

    /// <summary>内置编辑器保存正文上限（源文件保存放宽到 8 MB，普通请求仍 1 MB）。</summary>
    public const int MaxEditorSaveBytes = 8 * 1024 * 1024; // 8 MB

    /// <summary>二进制上传（/upload）正文上限：图片 ≤5MB、音频 ≤25MB，取 32MB 兜底。</summary>
    public const int MaxUploadBytes = 32 * 1024 * 1024; // 32 MB

    /// <summary>并发连接上限，防连接/线程耗尽（每个连接一个 Task）。</summary>
    public const int MaxConnections = 32;

    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private readonly System.Threading.SemaphoreSlim _connLimiter = new(MaxConnections);

    /// <summary>普通请求处理器（返回 null 表示 404）。异步委托：处理器内部 await 后返回，避免同步阻塞线程池线程。</summary>
    public Func<HttpRequest, Task<HttpResponse?>>? OnRequest;

    /// <summary>SSE 长连接处理器（阻塞写事件直到客户端断开）。接收请求对象以读取 query（如 client 标识）。</summary>
    public Func<HttpRequest, StreamWriter, Task>? OnSse;

    /// <summary>SSE 端点路径。</summary>
    public string SsePath { get; set; } = "/events";

    public HttpServer(int port) => _port = port;

    /// <summary>尝试占用一个连接槽位（同步非阻塞）。满则返回 false，调用方须在结束时 <see cref="ReleaseConnectionSlot"/>。</summary>
    internal bool TryAcquireConnectionSlot() => _connLimiter.Wait(0);

    /// <summary>释放一个连接槽位。</summary>
    internal void ReleaseConnectionSlot() { try { _connLimiter.Release(); } catch { } }

    /// <summary>启动后绑定的实际端口（传入 0 时由系统分配）。</summary>
    public int ActualPort { get; private set; }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        ActualPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _cts = new CancellationTokenSource();
        _acceptTask = Task.Run(AcceptLoopAsync);
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
    }

    private async Task AcceptLoopAsync()
    {
        var token = _cts!.Token;
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener!.AcceptTcpClientAsync(token); }
            catch { break; }
            // 连接限流：槽位满则立即关闭新连接（不占 Task/线程）
            if (!TryAcquireConnectionSlot())
            {
                try { client.Dispose(); } catch { }
                continue;
            }
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            try
            {
                // 读请求超时：防 slowloris（慢滴灌请求头占满连接槽，MaxConnections=32 被耗尽后正常请求无法建立）。
                // 仅作用于读请求阶段；SSE 长连接在读完请求后进入写阶段，不受此超时影响。
                byte[]? raw;
                using (var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    raw = await ReadRequestAsync(stream, readCts.Token);
                if (raw == null) return;
                var req = ParseHttpRequest(raw);
                if (req == null) return;

                // SSE 长连接
                if (req.Method == "GET" && req.Path == SsePath && OnSse != null)
                {
                    await WriteSseAsync(stream, req, OnSse);
                    return;
                }

                var resp = OnRequest != null ? await OnRequest.Invoke(req) : HttpResponse.NotFound();
                if (resp == null) resp = HttpResponse.NotFound();
                await WriteResponseAsync(stream, resp);
            }
            catch (RequestTooLargeException)
            {
                try { await WriteResponseAsync(stream, PayloadTooLarge()); } catch { }
            }
            catch (Infra.JsonParseException)
            {
                // 畸形 JSON 请求体：返回 400 JSON（原静默关闭连接，前端 fetch 无反馈只能吞错误）
                try
                {
                    var body = Encoding.UTF8.GetBytes("{\"ok\":false,\"error\":\"请求体不是有效 JSON\"}");
                    await WriteResponseAsync(stream, new HttpResponse { Status = 400, Reason = "Bad Request", Body = body });
                }
                catch { }
            }
        }
        catch { /* 连接异常静默关闭 */ }
        finally
        {
            try { client.Dispose(); } catch { }
            ReleaseConnectionSlot();
        }
    }

    private static HttpResponse PayloadTooLarge()
        => new() { Status = 413, Reason = "Payload Too Large", Body = Encoding.UTF8.GetBytes("413 Payload Too Large") };

    // ═══════════════════════════════════════════════════════════
    //  纯函数：请求解析 / SSE 格式化 / 响应头解析（可自测）
    // ═══════════════════════════════════════════════════════════

    /// <summary>解析完整 HTTP 报文（请求行 + 头 + 正文）为结构化对象。畸形返回 null。</summary>
    public static HttpRequest? ParseHttpRequest(string raw)
        => string.IsNullOrEmpty(raw) ? null : ParseHttpRequest(Encoding.UTF8.GetBytes(raw));

    /// <summary>从原始字节解析 HTTP 报文，正文按字节保存到 <see cref="HttpRequest.RawBody"/>（文本另镜像到 Body）。</summary>
    public static HttpRequest? ParseHttpRequest(byte[] raw)
    {
        if (raw == null || raw.Length == 0) return null;
        int headerEnd = FindHeaderEnd(raw);
        string headPart = headerEnd >= 0
            ? Encoding.UTF8.GetString(raw, 0, headerEnd)
            : Encoding.UTF8.GetString(raw);

        var lines = headPart.Split("\r\n");
        if (lines.Length == 0) return null;

        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2) return null;

        var req = new HttpRequest
        {
            Method = requestLine[0],
            Version = requestLine.Length >= 3 ? requestLine[2] : "",
        };
        var pathAndQuery = requestLine[1];
        int q = pathAndQuery.IndexOf('?');
        if (q >= 0) { req.Path = pathAndQuery[..q]; req.Query = pathAndQuery[(q + 1)..]; }
        else req.Path = pathAndQuery;

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            int colon = line.IndexOf(':');
            if (colon > 0)
                req.Headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        if (headerEnd >= 0)
        {
            int bodyStart = headerEnd + 4;
            int bodyLen = raw.Length - bodyStart;
            if (bodyLen > 0)
            {
                req.RawBody = new byte[bodyLen];
                Array.Copy(raw, bodyStart, req.RawBody, 0, bodyLen);
            }
        }
        req.Body = Encoding.UTF8.GetString(req.RawBody);
        return req;
    }

    /// <summary>格式化 SSE 事件（event: 类型 + data: JSON + 空行分隔）。</summary>
    public static string SseEvent(string type, string dataJson)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(type))
            sb.Append("event: ").Append(type).Append('\n');
        sb.Append("data: ").Append(dataJson).Append("\n\n");
        return sb.ToString();
    }

    /// <summary>在字节流中查找 HTTP 头结束标记 "\r\n\r\n" 的位置，未找到返回 -1。</summary>
    public static int FindHeaderEnd(byte[] data)
    {
        for (int i = 0; i + 3 < data.Length; i++)
            if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                return i;
        return -1;
    }

    /// <summary>从头文本解析 Content-Length（无则返回 0）。</summary>
    public static int ParseContentLength(string headerText)
    {
        foreach (var line in headerText.Split('\n'))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var name = line[..colon].Trim();
            if (!name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(line[(colon + 1)..].Trim(), out var v)) return v;
        }
        return 0;
    }

    /// <summary>Content-Length 是否超过 <see cref="MaxRequestBytes"/> 上限（纯逻辑，便于自测）。</summary>
    public static bool IsRequestTooLarge(int contentLength) => contentLength > MaxRequestBytes;

    // ═══════════════════════════════════════════════════════════
    //  网络 IO
    // ═══════════════════════════════════════════════════════════

    private static async Task<byte[]?> ReadRequestAsync(NetworkStream stream, CancellationToken ct)
    {
        var ms = new MemoryStream();
        var buffer = new byte[8192];

        // 1. 读请求头（限制 MaxHeaderBytes，未闭合也受约束）
        int headerEnd = -1;
        while (true)
        {
            int n = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (n <= 0) return null; // 连接关闭
            ms.Write(buffer, 0, n);
            if (ms.Length > MaxHeaderBytes)
                throw new RequestTooLargeException();
            headerEnd = FindHeaderEnd(ms.ToArray());
            if (headerEnd >= 0) break;
        }

        var headerText = Encoding.UTF8.GetString(ms.ToArray(), 0, headerEnd);
        int contentLength = ParseContentLength(headerText);

        // 正文上限：上传端点放宽到 MaxUploadBytes，编辑器保存放宽到 MaxEditorSaveBytes，其余受 MaxRequestBytes 约束
        var reqPath = ParsePath(headerText);
        int maxBody = reqPath == "/upload" ? MaxUploadBytes
            : reqPath.StartsWith("/editor/", StringComparison.Ordinal) ? MaxEditorSaveBytes
            : MaxRequestBytes;
        if (contentLength > maxBody)
            throw new RequestTooLargeException();

        // 2. 读正文（可能已随头读入一部分）
        int bodyStart = headerEnd + 4;
        while ((int)(ms.Length - bodyStart) < contentLength)
        {
            int remaining = contentLength - (int)(ms.Length - bodyStart);
            int n = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remaining), ct);
            if (n <= 0) break;
            ms.Write(buffer, 0, n);
            if (ms.Length > bodyStart + maxBody)
                throw new RequestTooLargeException();
        }

        return ms.ToArray();
    }

    /// <summary>从请求头文本提取路径（请求行第二段，去掉 query）。纯逻辑便于自测。</summary>
    internal static string ParsePath(string headerText)
    {
        if (string.IsNullOrEmpty(headerText)) return "";
        int lineEnd = headerText.IndexOf('\n');
        var line = (lineEnd >= 0 ? headerText[..lineEnd] : headerText).TrimEnd('\r');
        var parts = line.Split(' ');
        if (parts.Length < 2) return "";
        var pathAndQuery = parts[1];
        int q = pathAndQuery.IndexOf('?');
        return q >= 0 ? pathAndQuery[..q] : pathAndQuery;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, HttpResponse resp)
    {
        var head = $"HTTP/1.1 {resp.Status} {resp.Reason}\r\n" +
                   $"Content-Type: {resp.ContentType}\r\n" +
                   $"Content-Length: {resp.Body.Length}\r\n" +
                   "Cache-Control: no-store, no-cache, must-revalidate\r\n" +
                   "Connection: close\r\n" +
                   "\r\n";
        var headBytes = Encoding.UTF8.GetBytes(head);
        await stream.WriteAsync(headBytes, 0, headBytes.Length);
        if (resp.Body.Length > 0)
            await stream.WriteAsync(resp.Body, 0, resp.Body.Length);
        await stream.FlushAsync();
    }

    private static async Task WriteSseAsync(NetworkStream stream, HttpRequest req, Func<HttpRequest, StreamWriter, Task> onSse)
    {
        var head = "HTTP/1.1 200 OK\r\n" +
                   "Content-Type: text/event-stream\r\n" +
                   "Cache-Control: no-cache\r\n" +
                   "Connection: close\r\n" +
                   "\r\n";
        var headBytes = Encoding.UTF8.GetBytes(head);
        await stream.WriteAsync(headBytes, 0, headBytes.Length);
        await stream.FlushAsync();

        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        await onSse(req, writer);
    }
}
