using System.Diagnostics;
using System.Text;

namespace WayCoder.Tools;

// ============================================================
// 传输抽象层
// ============================================================

/// <summary>MCP 传输抽象基类 — 解耦通信方式与协议层</summary>
internal abstract class McpTransport
{
    /// <summary>发送 JSON-RPC 请求并等待匹配 id 的响应</summary>
    public abstract Task<JNode?> SendRequestAsync(int id, string method, JNode @params, CancellationToken ct);

    /// <summary>发送 JSON-RPC 通知（无 id，无响应）</summary>
    public abstract void SendNotification(string method, JNode @params);

    /// <summary>断开连接</summary>
    public abstract Task DisconnectAsync();

    /// <summary>连接是否活跃</summary>
    public abstract bool IsConnected { get; }
}

// ============================================================
// stdio 传输
// ============================================================

/// <summary>通过子进程 stdin/stdout 进行 MCP 通信</summary>
internal class StdioMcpTransport : McpTransport
{
    private Process? _process;
    private readonly object _writeLock = new();

    public override bool IsConnected => _process is { HasExited: false };

    public StdioMcpTransport(string command, string[] args, Dictionary<string, string>? env, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = string.Join(" ", args.Select(EscapeArg)),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };

        if (env != null)
        {
            foreach (var (key, value) in env)
                startInfo.EnvironmentVariables[key] = value;
        }

        _process = new Process { StartInfo = startInfo };
        _process.Start();
    }

    public override async Task<JNode?> SendRequestAsync(int id, string method, JNode @params, CancellationToken ct)
    {
        if (_process == null) return null;

        var request = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("id", id)
            .Set("method", method)
            .Set("params", @params);
        var json = request.ToJson();

        try
        {
            lock (_writeLock)
            {
                _process.StandardInput.WriteLine(json);
                _process.StandardInput.Flush();
            }

            // 读取响应（可能多行，找到匹配 id 的）
            while (!ct.IsCancellationRequested)
            {
                var line = await _process.StandardOutput.ReadLineAsync(ct);
                if (line == null) break;

                try
                {
                    var resp = Json.Parse(line);
                    if (resp != null && resp["id"]?.AsNumber() == id)
                        return resp;
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"stdio 请求 {method} 失败: {ex.GetType().Name}: {ex.Message}");
        }

        return null;
    }

    public override void SendNotification(string method, JNode @params)
    {
        if (_process == null) return;

        var notif = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("method", method)
            .Set("params", @params);

        try
        {
            lock (_writeLock)
            {
                _process.StandardInput.WriteLine(notif.ToJson());
                _process.StandardInput.Flush();
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"stdio 通知 {method} 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public override Task DisconnectAsync()
    {
        _process?.Kill(entireProcessTree: true);
        _process?.Dispose();
        _process = null;
        return Task.CompletedTask;
    }

    private static string EscapeArg(string arg)
    {
        if (arg.Contains(' ') || arg.Contains('"'))
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        return arg;
    }
}

// ============================================================
// HTTP/SSE 传输
// ============================================================

/// <summary>通过 HTTP POST + SSE 响应流进行 MCP 通信（Streamable HTTP 传输）</summary>
internal class HttpMcpTransport : McpTransport
{
    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private readonly string _url;
    private readonly Dictionary<string, string>? _headers;
    private bool _disposed;

    static HttpMcpTransport()
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/0.17.3");
    }

    public override bool IsConnected => !_disposed;

    public HttpMcpTransport(string url, Dictionary<string, string>? headers)
    {
        _url = url;
        _headers = headers;
    }

    public override async Task<JNode?> SendRequestAsync(int id, string method, JNode @params, CancellationToken ct)
    {
        if (_disposed) return null;

        var request = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("id", id)
            .Set("method", method)
            .Set("params", @params);
        var body = request.ToJson();

        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            // 添加自定义 headers
            if (_headers != null)
            {
                foreach (var (key, value) in _headers)
                    httpReq.Headers.TryAddWithoutValidation(key, value);
            }

            using var response = await _client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            // 读取 SSE 响应流，匹配 id
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var dataLines = new StringBuilder();
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                // SSE 事件边界：空行
                if (line.Length == 0)
                {
                    if (dataLines.Length > 0)
                    {
                        var data = dataLines.ToString();
                        dataLines.Clear();
                        try
                        {
                            var resp = Json.Parse(data);
                            if (resp != null && resp["id"]?.AsNumber() == id)
                                return resp;
                        }
                        catch { }
                    }
                    continue;
                }

                // 累积 data: 行
                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    var data = line.Substring(5);
                    if (data.StartsWith(' ')) data = data.Substring(1);
                    dataLines.Append(data);
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"HTTP 请求 {method} 失败: {ex.GetType().Name}: {ex.Message}");
        }

        return null;
    }

    public override void SendNotification(string method, JNode @params)
    {
        if (_disposed) return;

        var notif = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("method", method)
            .Set("params", @params);

        // 通知是 fire-and-forget，不等待响应
        _ = SendNotificationAsync(notif);
    }

    private async Task SendNotificationAsync(JNode notif)
    {
        try
        {
            var body = notif.ToJson();
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            if (_headers != null)
            {
                foreach (var (key, value) in _headers)
                    httpReq.Headers.TryAddWithoutValidation(key, value);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _client.SendAsync(httpReq, cts.Token);
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"HTTP 通知失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public override Task DisconnectAsync()
    {
        _disposed = true;
        return Task.CompletedTask;
    }
}

// ============================================================
// HTTP+SSE 传输（legacy SSE：GET /sse 事件流 + POST /message 发消息）
// ============================================================

/// <summary>
/// 通过 HTTP+SSE 双端点进行 MCP 通信（MCP 旧版 SSE 传输，2024-11-05 规范）。
/// 流程：GET {url} 建立 SSE 事件流 → 服务器推送 endpoint 事件（message 端点）→
///       客户端 POST JSON-RPC 到 message 端点 → 服务器通过 SSE 流推送响应。
/// </summary>
internal class SseMcpTransport : McpTransport
{
    private static readonly HttpClient _client = new()
    {
        Timeout = Timeout.InfiniteTimeSpan, // SSE 长连接，不设整体超时
    };

    private readonly string _sseUrl;
    private readonly Dictionary<string, string>? _headers;
    private readonly object _lock = new();
    private readonly Dictionary<int, TaskCompletionSource<JNode?>> _pending = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _endpointReady =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private string? _messageEndpoint;
    private bool _disposed;

    static SseMcpTransport()
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/0.17.3");
    }

    public override bool IsConnected => !_disposed && _messageEndpoint != null;

    public SseMcpTransport(string url, Dictionary<string, string>? headers)
    {
        _sseUrl = url;
        _headers = headers;
        _ = Task.Run(ReadSseLoopAsync);
    }

    /// <summary>将 SSE endpoint 事件的 data（可能是相对路径）解析为绝对 URL</summary>
    internal static string? ResolveEndpointUrl(string sseUrl, string? endpointData)
    {
        if (string.IsNullOrWhiteSpace(endpointData)) return null;
        try
        {
            return new Uri(new Uri(sseUrl), endpointData).ToString();
        }
        catch { return null; }
    }

    public override async Task<JNode?> SendRequestAsync(int id, string method, JNode @params, CancellationToken ct)
    {
        if (_disposed) return null;

        // 等待 endpoint 就绪（服务器推送 message 端点）
        try { await _endpointReady.Task.WaitAsync(ct); }
        catch (OperationCanceledException) { return null; }
        catch (TimeoutException) { return null; }

        var messageUrl = _messageEndpoint;
        if (messageUrl == null) return null;

        var tcs = new TaskCompletionSource<JNode?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock) { _pending[id] = tcs; }

        var request = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("id", id)
            .Set("method", method)
            .Set("params", @params);

        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, messageUrl)
            {
                Content = new StringContent(request.ToJson(), Encoding.UTF8, "application/json"),
            };
            if (_headers != null)
                foreach (var (key, value) in _headers)
                    httpReq.Headers.TryAddWithoutValidation(key, value);

            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            using var resp = await _client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, sendCts.Token);
            resp.EnsureSuccessStatusCode();

            // 等待 SSE 流推送匹配 id 的响应
            return await tcs.Task.WaitAsync(ct);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"SSE 请求 {method} 失败: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
        finally
        {
            lock (_lock) { _pending.Remove(id); }
        }
    }

    public override void SendNotification(string method, JNode @params)
    {
        if (_disposed) return;
        _ = SendNotificationAsync(method, @params);
    }

    private async Task SendNotificationAsync(string method, JNode @params)
    {
        // 等待 endpoint 就绪（最多 10s），失败则静默放弃
        try { await _endpointReady.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch { return; }

        var messageUrl = _messageEndpoint;
        if (messageUrl == null) return;

        try
        {
            var notif = JNode.Object()
                .Set("jsonrpc", "2.0")
                .Set("method", method)
                .Set("params", @params);
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, messageUrl)
            {
                Content = new StringContent(notif.ToJson(), Encoding.UTF8, "application/json"),
            };
            if (_headers != null)
                foreach (var (key, value) in _headers)
                    httpReq.Headers.TryAddWithoutValidation(key, value);

            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            using var resp = await _client.SendAsync(httpReq, sendCts.Token);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"SSE 通知 {method} 失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public override Task DisconnectAsync()
    {
        _disposed = true;
        _cts.Cancel();
        lock (_lock)
        {
            foreach (var (_, tcs) in _pending)
                tcs.TrySetResult(null);
            _pending.Clear();
        }
        return Task.CompletedTask;
    }

    /// <summary>持续读取 SSE 事件流，分发 endpoint / message 事件</summary>
    private async Task ReadSseLoopAsync()
    {
        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Get, _sseUrl);
            httpReq.Headers.Accept.ParseAdd("text/event-stream");
            if (_headers != null)
                foreach (var (key, value) in _headers)
                    httpReq.Headers.TryAddWithoutValidation(key, value);

            using var response = await _client.SendAsync(
                httpReq, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? eventName = null;
            var dataLines = new StringBuilder();

            while (!_cts.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(_cts.Token);
                if (line == null) break;

                if (line.Length == 0)
                {
                    HandleSseEvent(eventName, dataLines.ToString());
                    eventName = null;
                    dataLines.Clear();
                    continue;
                }

                if (line.StartsWith("event:", StringComparison.Ordinal))
                {
                    eventName = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    var data = line.Substring(5);
                    if (data.StartsWith(' ')) data = data.Substring(1);
                    if (dataLines.Length > 0) dataLines.Append('\n');
                    dataLines.Append(data);
                }
            }
        }
        catch (OperationCanceledException) { /* 正常断开 */ }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"SSE 读取循环异常: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _disposed = true;
            _endpointReady.TrySetResult(); // 唤醒等待者，避免永久阻塞
            lock (_lock)
            {
                foreach (var (_, tcs) in _pending)
                    tcs.TrySetResult(null);
                _pending.Clear();
            }
        }
    }

    private void HandleSseEvent(string? eventName, string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        if (eventName == "endpoint")
        {
            var resolved = ResolveEndpointUrl(_sseUrl, data);
            if (resolved != null)
            {
                _messageEndpoint = resolved;
                _endpointReady.TrySetResult();
                DebugLog.Log("mcp", $"SSE endpoint: {resolved}");
            }
            return;
        }

        // message 事件（或无 event 字段的默认事件）—— JSON-RPC 响应
        if (eventName == "message" || eventName == null)
        {
            try
            {
                var resp = Json.Parse(data);
                if (resp == null) return;
                var id = resp["id"]?.AsNumber();
                if (id == null) return;

                lock (_lock)
                {
                    if (_pending.TryGetValue((int)id.Value, out var tcs))
                    {
                        _pending.Remove((int)id.Value);
                        tcs.TrySetResult(resp);
                    }
                }
            }
            catch { }
        }
    }
}
