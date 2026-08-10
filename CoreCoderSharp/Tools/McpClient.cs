using System.Diagnostics;
using System.Text;

namespace CoreCoderSharp.Tools;

/// <summary>
/// MCP (Model Context Protocol) 客户端。
/// 支持 stdio 和 HTTP 两种传输方式，使用 JSON-RPC 2.0 over stdin/stdout 或 HTTP/SSE 通信，
/// 自动发现 MCP 服务器提供的工具并注册到 WayCoder。
///
/// 配置: .corecoder/mcp_servers.json
/// [
///   { "name": "filesystem", "command": "npx", "args": ["-y", "@modelcontextprotocol/server-filesystem", "."] },
///   { "name": "github", "transport": "http", "url": "https://api.example.com/mcp", "headers": {"Authorization": "Bearer ${GITHUB_TOKEN}"} }
/// ]
/// </summary>
public static class McpManager
{
    private static readonly List<McpConnection> _connections = [];
    private static bool _initialized;

    /// <summary>所有已发现的 MCP 工具</summary>
    public static List<ITool> DiscoveredTools { get; } = [];

    /// <summary>MCP 连接状态信息，供 UI 面板展示</summary>
    public static string Info { get; private set; } = "未配置";

    /// <summary>
    /// 从配置文件初始化所有 MCP 服务器连接。
    /// 先尝试从缓存加载工具（快速启动），再异步连接发现。
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        var configPath = FindConfigFile();
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

                // 检测传输类型
                var url = server?["url"]?.GetValue<string>();
                var transportType = server?["transport"]?.GetValue<string>();

                if (!string.IsNullOrEmpty(url) || "http".Equals(transportType, StringComparison.OrdinalIgnoreCase))
                {
                    // HTTP 传输
                    if (!string.IsNullOrEmpty(url))
                    {
                        var headers = ParseHeaders(server?["headers"]?.AsObject());
                        _ = ConnectAndDiscoverHttpAsync(name, url, headers);
                    }
                }
                else
                {
                    // stdio 传输（默认，向后兼容）
                    var command = server?["command"]?.GetValue<string>();
                    var args = server?["args"]?.AsArray()
                        ?.Select(a => a?.GetValue<string>() ?? "").ToArray() ?? [];

                    var env = new Dictionary<string, string>();
                    var envObj = server?["env"]?.AsObject();
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
                    {
                        _ = ConnectAndDiscoverStdioAsync(name, command, args, env);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP 初始化失败: {ex.GetType().Name}: {ex.Message}");
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
            Info += $"\n  {name} ✗ 连接失败";
        }
    }

    /// <summary>HTTP 传输：连接服务器并发现工具</summary>
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
            Info += $"\n  {name} ✗ 连接失败";
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
        DebugLog.Log("mcp", $"MCP {name}: 发现 {tools.Count} 个工具");
    }

    /// <summary>更新 MCP 连接状态信息</summary>
    private static void UpdateInfo()
    {
        var sb = new StringBuilder();
        foreach (var conn in _connections)
        {
            var toolCount = DiscoveredTools.Count(t =>
                t.Name.StartsWith($"mcp__{conn.Name}__", StringComparison.Ordinal));
            sb.Append($"  {conn.Name} ✓ {toolCount} 工具");
            if (conn != _connections.Last()) sb.Append('\n');
        }
        if (sb.Length > 0)
            Info = sb.ToString();
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
    }

    private static string? FindConfigFile()
    {
        var cwd = Environment.CurrentDirectory;
        var dir = cwd;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, ".corecoder", "mcp_servers.json");
            if (File.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }
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
