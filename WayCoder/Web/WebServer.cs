using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WayCoder.Web;

/// <summary>解析后的 HTTP 请求。</summary>
public sealed class HttpRequest
{
    public string Method = "";
    public string Path = "";
    public string Query = "";
    public string Version = "";
    public Dictionary<string, string> Headers = new(StringComparer.OrdinalIgnoreCase);
    public string Body = "";

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
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;

    /// <summary>普通请求处理器（返回 null 表示 404）。</summary>
    public Func<HttpRequest, HttpResponse?>? OnRequest;

    /// <summary>SSE 长连接处理器（阻塞写事件直到客户端断开）。</summary>
    public Func<StreamWriter, Task>? OnSse;

    /// <summary>SSE 端点路径。</summary>
    public string SsePath { get; set; } = "/events";

    public HttpServer(int port) => _port = port;

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
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var raw = await ReadRequestAsync(stream);
            if (raw == null) return;
            var req = ParseHttpRequest(raw);
            if (req == null) return;

            // SSE 长连接
            if (req.Method == "GET" && req.Path == SsePath && OnSse != null)
            {
                await WriteSseAsync(stream, OnSse);
                return;
            }

            var resp = OnRequest?.Invoke(req) ?? HttpResponse.NotFound();
            await WriteResponseAsync(stream, resp);
        }
        catch { /* 连接异常静默关闭 */ }
        finally { try { client.Dispose(); } catch { } }
    }

    // ═══════════════════════════════════════════════════════════
    //  纯函数：请求解析 / SSE 格式化 / 响应头解析（可自测）
    // ═══════════════════════════════════════════════════════════

    /// <summary>解析完整 HTTP 报文（请求行 + 头 + 正文）为结构化对象。畸形返回 null。</summary>
    public static HttpRequest? ParseHttpRequest(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int headerEnd = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        string headPart = headerEnd >= 0 ? raw[..headerEnd] : raw;
        string body = headerEnd >= 0 ? raw[(headerEnd + 4)..] : "";

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

        req.Body = body;
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

    // ═══════════════════════════════════════════════════════════
    //  网络 IO
    // ═══════════════════════════════════════════════════════════

    private static async Task<string?> ReadRequestAsync(NetworkStream stream)
    {
        var ms = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            int n = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (n <= 0) break;
            ms.Write(buffer, 0, n);

            var data = ms.ToArray();
            int headerEnd = FindHeaderEnd(data);
            if (headerEnd < 0) continue;

            var headerText = Encoding.UTF8.GetString(data, 0, headerEnd);
            int contentLength = ParseContentLength(headerText);
            int bodyStart = headerEnd + 4;
            int bodyHave = data.Length - bodyStart;

            while (bodyHave < contentLength)
            {
                n = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, contentLength - bodyHave));
                if (n <= 0) break;
                ms.Write(buffer, 0, n);
                bodyHave += n;
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        return null;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, HttpResponse resp)
    {
        var head = $"HTTP/1.1 {resp.Status} {resp.Reason}\r\n" +
                   $"Content-Type: {resp.ContentType}\r\n" +
                   $"Content-Length: {resp.Body.Length}\r\n" +
                   "Connection: close\r\n" +
                   "\r\n";
        var headBytes = Encoding.UTF8.GetBytes(head);
        await stream.WriteAsync(headBytes, 0, headBytes.Length);
        if (resp.Body.Length > 0)
            await stream.WriteAsync(resp.Body, 0, resp.Body.Length);
        await stream.FlushAsync();
    }

    private static async Task WriteSseAsync(NetworkStream stream, Func<StreamWriter, Task> onSse)
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
        await onSse(writer);
    }
}
