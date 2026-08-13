using System.Diagnostics;
using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// MCP (Model Context Protocol) 客户端。
/// 支持 stdio / HTTP（Streamable）/ SSE（HTTP+SSE 双端点）三种传输方式，
/// 自动发现 MCP 服务器提供的工具并注册到 WayCoder。
///
/// 配置: .waycoder/mcp_servers.json（兼容读取 .corecoder/mcp_servers.json）
/// [
///   { "name": "filesystem", "command": "npx", "args": ["-y", "@modelcontextprotocol/server-filesystem", "."] },
///   { "name": "github", "transport": "http", "url": "https://api.example.com/mcp", "headers": {"Authorization": "Bearer ${GITHUB_TOKEN}"} },
///   { "name": "legacy", "transport": "sse", "url": "https://api.example.com/sse", "headers": {"Authorization": "Bearer ${TOKEN}"} }
/// ]
/// </summary>
public static class McpManager
{
    private static readonly List<McpConnection> _connections = [];
    private static readonly Dictionary<string, McpServerState> _states = [];
    private static readonly object _stateLock = new();
    private static bool _initialized;

    /// <summary>所有已发现的 MCP 工具</summary>
    public static List<ITool> DiscoveredTools { get; } = [];

    /// <summary>MCP 连接状态信息，供 UI 面板展示</summary>
    public static string Info { get; private set; } = "未配置";

    /// <summary>结构化服务器状态快照，供 /mcp 命令与侧栏展示</summary>
    public static IReadOnlyList<McpServerInfo> Servers
    {
        get
        {
            lock (_stateLock)
                return _states.Values
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(s => s.ToInfo())
                    .ToList();
        }
    }

    /// <summary>MCP 传输类型</summary>
    internal enum McpTransportType { Stdio, Http, Sse }

    /// <summary>识别服务器配置的传输类型（纯逻辑，便于自测）。默认 stdio。</summary>
    internal static McpTransportType DetectTransport(JsonNode server)
    {
        var transport = server["transport"]?.GetValue<string>();
        var url = server["url"]?.GetValue<string>();

        if ("sse".Equals(transport, StringComparison.OrdinalIgnoreCase))
            return McpTransportType.Sse;
        if (!string.IsNullOrEmpty(url) || "http".Equals(transport, StringComparison.OrdinalIgnoreCase))
            return McpTransportType.Http;
        return McpTransportType.Stdio;
    }

    /// <summary>
    /// 从配置文件初始化所有 MCP 服务器连接。
    /// 先尝试从缓存加载工具（快速启动），再异步连接发现。
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        var configPath = Global.FindConfigFileInTree(Environment.CurrentDirectory, "mcp_servers.json");
        if (configPath == null) return;

        try
        {
            var json = File.ReadAllText(configPath, Encoding.UTF8);
            var servers = JsonNode.Parse(json)?.AsArray();
            if (servers == null) return;

            // 先尝试从缓存加载工具（加速启动）
            McpCache.Load(servers);

            foreach (var server in servers)
            {
                var name = server?["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(name)) continue;
                RegisterState(name, server!);
                _ = ConnectServerAsync(server!);
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP 初始化失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>注册服务器状态（初始 Connecting）。</summary>
    private static void RegisterState(string name, JsonNode server)
    {
        lock (_stateLock)
        {
            if (!_states.ContainsKey(name))
                _states[name] = new McpServerState
                {
                    Name = name,
                    Transport = DetectTransport(server).ToString().ToLowerInvariant(),
                };
        }
    }

    /// <summary>解析并连接单个服务器（Init 与 Reload 共用）。</summary>
    private static async Task ConnectServerAsync(JsonNode server)
    {
        var name = server["name"]?.GetValue<string>() ?? "";
        var transportType = DetectTransport(server);
        var url = server["url"]?.GetValue<string>();
        var headers = ParseHeaders(server["headers"]?.AsObject());

        try
        {
            switch (transportType)
            {
                case McpTransportType.Sse when !string.IsNullOrEmpty(url):
                    // SSE 传输（HTTP+SSE 双端点：GET /sse + POST /message）
                    await ConnectAndDiscoverSseAsync(name, url, headers);
                    break;

                case McpTransportType.Http when !string.IsNullOrEmpty(url):
                    // HTTP（Streamable）传输
                    await ConnectAndDiscoverHttpAsync(name, url, headers);
                    break;

                case McpTransportType.Stdio:
                {
                    // stdio 传输（默认，向后兼容）
                    var command = server["command"]?.GetValue<string>();
                    var args = server["args"]?.AsArray()
                        ?.Select(a => a?.GetValue<string>() ?? "").ToArray() ?? [];

                    var env = new Dictionary<string, string>();
                    var envObj = server["env"]?.AsObject();
                    if (envObj != null)
                    {
                        foreach (var kv in envObj)
                        {
                            var val = kv.Value?.GetValue<string>();
                            if (!string.IsNullOrEmpty(val))
                                env[kv.Key] = val;
                        }
                    }

                    if (!string.IsNullOrEmpty(command))
                        await ConnectAndDiscoverStdioAsync(name, command, args, env);
                    else
                        SetStatus(name, McpServerStatus.Failed, error: "缺少 command 字段");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus(name, McpServerStatus.Failed, error: ex.Message);
        }
    }

    /// <summary>解析 headers 对象，展开 ${VAR} 环境变量</summary>
    internal static Dictionary<string, string>? ParseHeaders(JsonObject? headersObj)
    {
        if (headersObj == null || headersObj.Count == 0) return null;
        var headers = new Dictionary<string, string>();
        foreach (var kv in headersObj)
        {
            var val = kv.Value?.GetValue<string>();
            if (!string.IsNullOrEmpty(val))
                headers[kv.Key] = ExpandEnvVars(val);
        }
        return headers.Count > 0 ? headers : null;
    }

    /// <summary>展开字符串中的 ${VAR} 环境变量引用</summary>
    internal static string ExpandEnvVars(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("${")) return input;
        var sb = new StringBuilder(input.Length);
        int i = 0;
        while (i < input.Length)
        {
            if (i + 3 <= input.Length && input[i] == '$' && input[i + 1] == '{')
            {
                var end = input.IndexOf('}', i + 2);
                if (end > i + 2)
                {
                    var varName = input.Substring(i + 2, end - (i + 2));
                    var envVal = Environment.GetEnvironmentVariable(varName) ?? "";
                    sb.Append(envVal);
                    i = end + 1;
                    continue;
                }
            }
            sb.Append(input[i]);
            i++;
        }
        return sb.ToString();
    }

    /// <summary>stdio 传输：连接服务器并发现工具</summary>
    private static async Task ConnectAndDiscoverStdioAsync(string name, string command, string[] args,
        Dictionary<string, string>? env = null)
    {
        try
        {
            var transport = new StdioMcpTransport(command, args, env, Environment.CurrentDirectory);
            var conn = new McpConnection(name, transport);
            await DiscoverToolsAsync(conn, name);

            // 发现成功后更新缓存
            McpCache.Save(DiscoveredTools);
            UpdateInfo();
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} 连接失败: {ex.GetType().Name}: {ex.Message}");
            SetStatus(name, McpServerStatus.Failed, error: ex.Message);
        }
    }

    /// <summary>HTTP（Streamable）传输：连接服务器并发现工具</summary>
    private static async Task ConnectAndDiscoverHttpAsync(string name, string url,
        Dictionary<string, string>? headers)
    {
        try
        {
            var transport = new HttpMcpTransport(url, headers);
            var conn = new McpConnection(name, transport);
            await DiscoverToolsAsync(conn, name);

            // 发现成功后更新缓存
            McpCache.Save(DiscoveredTools);
            UpdateInfo();
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} (HTTP) 连接失败: {ex.GetType().Name}: {ex.Message}");
            SetStatus(name, McpServerStatus.Failed, error: ex.Message);
        }
    }

    /// <summary>SSE（HTTP+SSE 双端点）传输：连接服务器并发现工具</summary>
    private static async Task ConnectAndDiscoverSseAsync(string name, string url,
        Dictionary<string, string>? headers)
    {
        try
        {
            var transport = new SseMcpTransport(url, headers);
            var conn = new McpConnection(name, transport);
            await DiscoverToolsAsync(conn, name);

            // 发现成功后更新缓存
            McpCache.Save(DiscoveredTools);
            UpdateInfo();
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} (SSE) 连接失败: {ex.GetType().Name}: {ex.Message}");
            SetStatus(name, McpServerStatus.Failed, error: ex.Message);
        }
    }

    /// <summary>执行 MCP 握手 + 工具发现（与传输无关）</summary>
    private static async Task DiscoverToolsAsync(McpConnection conn, string name)
    {
        // 握手: initialize
        var initResp = await conn.SendRequestAsync("initialize", new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = "WayCoder",
                ["version"] = "0.17.3",
            },
        });

        if (initResp == null)
        {
            DebugLog.Log("mcp", $"MCP {name}: 握手失败");
            SetStatus(name, McpServerStatus.Failed, error: "握手超时");
            return;
        }

        // 发送 initialized 通知
        conn.SendNotification("notifications/initialized", new JsonObject());

        // 发现工具: tools/list
        var toolsResp = await conn.SendRequestAsync("tools/list", new JsonObject());
        var tools = toolsResp?["tools"]?.AsArray();
        if (tools == null || tools.Count == 0)
        {
            DebugLog.Log("mcp", $"MCP {name}: 无可用工具");
            SetStatus(name, McpServerStatus.Connected, toolCount: 0, connection: conn);
            _connections.Add(conn);
            UpdateInfo();
            return;
        }

        // 移除该服务器的旧工具（缓存可能有旧版本）
        var prefix = $"mcp__{name}__";
        DiscoveredTools.RemoveAll(t => t.Name.StartsWith(prefix, StringComparison.Ordinal));

        // 注册工具
        foreach (var toolNode in tools)
        {
            var tool = toolNode?.AsObject();
            if (tool == null) continue;

            var mcpTool = new McpTool(name, tool, conn);
            DiscoveredTools.Add(mcpTool);
        }

        _connections.Add(conn);
        SetStatus(name, McpServerStatus.Connected, toolCount: tools.Count, connection: conn);
        UpdateInfo();
        DebugLog.Log("mcp", $"MCP {name}: 发现 {tools.Count} 个工具");
    }

    /// <summary>更新 MCP 连接状态信息（由结构化状态生成）</summary>
    private static void UpdateInfo()
    {
        lock (_stateLock)
        {
            var sb = new StringBuilder();
            foreach (var st in _states.Values.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                var mark = st.Status switch
                {
                    McpServerStatus.Connected => "✓",
                    McpServerStatus.Connecting => "…",
                    McpServerStatus.Failed => "✗",
                    _ => "?",
                };
                sb.Append($"  {st.Name} {mark} {st.ToolCount} 工具");
                if (st.Error != null) sb.Append($" ({st.Error})");
                sb.Append('\n');
            }
            Info = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : "未配置";
        }
    }

    /// <summary>更新服务器状态。</summary>
    private static void SetStatus(string name, McpServerStatus status, int toolCount = 0,
        string? error = null, McpConnection? connection = null)
    {
        lock (_stateLock)
        {
            if (!_states.TryGetValue(name, out var st)) return;
            st.Status = status;
            st.ToolCount = toolCount;
            st.Error = error;
            if (connection != null) st.Connection = connection;
        }
    }

    /// <summary>断开单个服务器的连接并移除其工具。</summary>
    private static async Task DisconnectServerAsync(string name)
    {
        McpConnection? conn = null;
        lock (_stateLock)
        {
            if (_states.TryGetValue(name, out var st)) conn = st.Connection;
        }

        if (conn != null)
        {
            try { conn.SendNotification("exit", new JsonObject()); } catch { }
            try { await conn.DisconnectAsync(); } catch { }
            _connections.Remove(conn);
        }

        // 移除该服务器的旧工具
        var prefix = $"mcp__{name}__";
        DiscoveredTools.RemoveAll(t => t.Name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// 重连 MCP 服务器（/mcp reload）。name 为空则重连全部。
    /// 返回用户可读的结果信息。
    /// </summary>
    public static async Task<string> ReloadAsync(string? name)
    {
        var configPath = Global.FindConfigFileInTree(Environment.CurrentDirectory, "mcp_servers.json");
        if (configPath == null) return "未找到 mcp_servers.json 配置";

        try
        {
            var servers = JsonNode.Parse(File.ReadAllText(configPath, Encoding.UTF8))?.AsArray();
            if (servers == null || servers.Count == 0) return "mcp_servers.json 为空或格式错误";

            var targets = new List<JsonNode>();
            foreach (var server in servers)
            {
                var n = server?["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(n)) continue;
                if (name == null || n.Equals(name, StringComparison.OrdinalIgnoreCase))
                    targets.Add(server!);
            }

            if (targets.Count == 0)
                return name == null ? "无可用服务器" : $"未找到服务器 {name}";

            foreach (var server in targets)
            {
                var n = server["name"]!.GetValue<string>();
                await DisconnectServerAsync(n);
                SetStatus(n, McpServerStatus.Connecting);
                await ConnectServerAsync(server);
            }

            return $"已重连 {targets.Count} 个服务器";
        }
        catch (Exception ex)
        {
            return $"重连失败: {ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 断开所有 MCP 连接。
    /// </summary>
    public static void Shutdown()
    {
        foreach (var conn in _connections)
        {
            try
            {
                conn.SendNotification("exit", new JsonObject());
                _ = conn.DisconnectAsync();
            }
            catch { }
        }
        _connections.Clear();
        DiscoveredTools.Clear();
        lock (_stateLock) _states.Clear();
    }

    // FindConfigFileInTree defined in Global.cs (shared with McpCache)
}

// ============================================================
// MCP 服务器状态模型
// ============================================================

/// <summary>MCP 服务器连接状态。</summary>
public enum McpServerStatus
{
    /// <summary>连接中</summary>
    Connecting,
    /// <summary>已连接（工具已发现）</summary>
    Connected,
    /// <summary>连接失败</summary>
    Failed,
}

/// <summary>MCP 服务器状态快照（供 /mcp 命令与 UI 展示，不可变）。</summary>
public class McpServerInfo
{
    public string Name { get; }
    public string Transport { get; }
    public McpServerStatus Status { get; }
    public int ToolCount { get; }
    public string? Error { get; }

    public McpServerInfo(string name, string transport, McpServerStatus status, int toolCount, string? error)
    {
        Name = name;
        Transport = transport;
        Status = status;
        ToolCount = toolCount;
        Error = error;
    }
}

/// <summary>MCP 服务器运行时状态（内部，可变）。</summary>
internal class McpServerState
{
    public string Name = "";
    public string Transport = "stdio";
    public McpServerStatus Status = McpServerStatus.Connecting;
    public int ToolCount;
    public string? Error;
    public McpConnection? Connection;

    public McpServerInfo ToInfo() => new(Name, Transport, Status, ToolCount, Error);
}

// ============================================================
// 传输抽象层
// ============================================================

/// <summary>MCP 传输抽象基类 — 解耦通信方式与协议层</summary>
internal abstract class McpTransport
{
    /// <summary>发送 JSON-RPC 请求并等待匹配 id 的响应</summary>
    public abstract Task<JsonObject?> SendRequestAsync(int id, string method, JsonObject @params, CancellationToken ct);

    /// <summary>发送 JSON-RPC 通知（无 id，无响应）</summary>
    public abstract void SendNotification(string method, JsonObject @params);

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

    public override async Task<JsonObject?> SendRequestAsync(int id, string method, JsonObject @params, CancellationToken ct)
    {
        if (_process == null) return null;

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        };
        var json = request.ToJsonString();

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
                    var resp = JsonNode.Parse(line)?.AsObject();
                    if (resp != null && resp["id"]?.GetValue<int>() == id)
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

    public override void SendNotification(string method, JsonObject @params)
    {
        if (_process == null) return;

        var notif = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params,
        };

        try
        {
            lock (_writeLock)
            {
                _process.StandardInput.WriteLine(notif.ToJsonString());
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
        _process?.Kill();
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

    public override async Task<JsonObject?> SendRequestAsync(int id, string method, JsonObject @params, CancellationToken ct)
    {
        if (_disposed) return null;

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        };
        var body = request.ToJsonString();

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
                            var resp = JsonNode.Parse(data)?.AsObject();
                            if (resp != null && resp["id"]?.GetValue<int>() == id)
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

    public override void SendNotification(string method, JsonObject @params)
    {
        if (_disposed) return;

        var notif = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params,
        };

        // 通知是 fire-and-forget，不等待响应
        _ = SendNotificationAsync(notif);
    }

    private async Task SendNotificationAsync(JsonObject notif)
    {
        try
        {
            var body = notif.ToJsonString();
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
    private readonly Dictionary<int, TaskCompletionSource<JsonObject?>> _pending = [];
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

    public override async Task<JsonObject?> SendRequestAsync(int id, string method, JsonObject @params, CancellationToken ct)
    {
        if (_disposed) return null;

        // 等待 endpoint 就绪（服务器推送 message 端点）
        try { await _endpointReady.Task.WaitAsync(ct); }
        catch (OperationCanceledException) { return null; }
        catch (TimeoutException) { return null; }

        var messageUrl = _messageEndpoint;
        if (messageUrl == null) return null;

        var tcs = new TaskCompletionSource<JsonObject?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock) { _pending[id] = tcs; }

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        };

        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, messageUrl)
            {
                Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json"),
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

    public override void SendNotification(string method, JsonObject @params)
    {
        if (_disposed) return;
        _ = SendNotificationAsync(method, @params);
    }

    private async Task SendNotificationAsync(string method, JsonObject @params)
    {
        // 等待 endpoint 就绪（最多 10s），失败则静默放弃
        try { await _endpointReady.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch { return; }

        var messageUrl = _messageEndpoint;
        if (messageUrl == null) return;

        try
        {
            var notif = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["params"] = @params,
            };
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, messageUrl)
            {
                Content = new StringContent(notif.ToJsonString(), Encoding.UTF8, "application/json"),
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
                var resp = JsonNode.Parse(data)?.AsObject();
                if (resp == null) return;
                var id = resp["id"]?.GetValue<int>();
                if (id == null) return;

                lock (_lock)
                {
                    if (_pending.TryGetValue(id.Value, out var tcs))
                    {
                        _pending.Remove(id.Value);
                        tcs.TrySetResult(resp);
                    }
                }
            }
            catch { }
        }
    }
}

// ============================================================
// MCP 连接（协议层）
// ============================================================

/// <summary>
/// MCP 连接 — 管理与单个 MCP 服务器的 JSON-RPC 通信。
/// 持有传输层实例，负责任务 ID 序列和请求/通知组装。
/// </summary>
internal class McpConnection
{
    public string Name { get; }
    private readonly McpTransport _transport;

    private int _nextId = 1;
    private readonly object _lock = new();

    public McpConnection(string name, McpTransport transport)
    {
        Name = name;
        _transport = transport;
    }

    /// <summary>发送 JSON-RPC 请求并等待响应</summary>
    public async Task<JsonObject?> SendRequestAsync(string method, JsonObject @params)
    {
        int id;
        lock (_lock) { id = _nextId++; }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await _transport.SendRequestAsync(id, method, @params, cts.Token);
    }

    /// <summary>发送 JSON-RPC 通知（无响应）</summary>
    public void SendNotification(string method, JsonObject @params)
    {
        _transport.SendNotification(method, @params);
    }

    /// <summary>断开连接</summary>
    public async Task DisconnectAsync()
    {
        await _transport.DisconnectAsync();
    }
}

// ============================================================
// MCP 工具包装器
// ============================================================

/// <summary>
/// MCP 工具包装器 — 将 MCP 工具适配为 WayCoder ITool 接口。
/// 工具名称格式: mcp__&lt;server&gt;__&lt;tool&gt;
/// </summary>
internal class McpTool : ITool
{
    private readonly string _serverName;
    private readonly JsonObject _toolDef;
    private readonly McpConnection _connection;

    public string Name { get; }
    public string Description { get; }

    public JsonObject Parameters => _toolDef["inputSchema"]?.AsObject()
        ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() };

    public McpTool(string serverName, JsonObject toolDef, McpConnection connection)
    {
        _serverName = serverName;
        _toolDef = toolDef;
        _connection = connection;

        var toolName = toolDef["name"]?.GetValue<string>() ?? "unknown";
        Name = $"mcp__{serverName}__{toolName}";
        Description = toolDef["description"]?.GetValue<string>() ?? $"(MCP) {serverName}/{toolName}";
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var toolName = _toolDef["name"]?.GetValue<string>() ?? "";

        // 将参数字典转为 JsonObject
        var @params = new JsonObject
        {
            ["name"] = toolName,
            ["arguments"] = JsonNode.Parse(JsonHelper.SerializeArgs(arguments)),
        };

        var resp = await _connection.SendRequestAsync("tools/call", @params);
        if (resp == null)
            return $"错误: MCP {_serverName}/{toolName} 调用超时";

        var error = resp["error"];
        if (error != null)
            return $"错误: MCP {_serverName}/{toolName} — {error["message"]?.GetValue<string>() ?? "未知错误"}";

        var result = resp["result"];
        var content = result?["content"];

        if (content is JsonArray arr)
        {
            var texts = arr.Select(n => n?["text"]?.GetValue<string>() ?? "").Where(t => t != "");
            return string.Join("\n", texts);
        }

        return result?.ToJsonString() ?? "(空结果)";
    }
}
