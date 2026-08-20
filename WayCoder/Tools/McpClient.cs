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

    /// <summary>所有已发现的 MCP 工具（仅经 <see cref="MutateTools"/> 修改，读者用快照）。</summary>
    public static List<ITool> DiscoveredTools { get; } = [];

    /// <summary>DiscoveredTools 互斥锁：多服务器并行发现（fire-and-forget）会并发 RemoveAll/Add，
    /// 同时 ToolRegistry.AllTools / McpCache.Save 在另一线程枚举 —— 无锁会抛 Collection was modified。</summary>
    private static readonly object _toolsLock = new();

    /// <summary>加锁执行工具列表变更（RemoveAll/Add/Clear 统一走这里）。</summary>
    private static void MutateTools(Action action)
    {
        lock (_toolsLock) action();
    }

    /// <summary>安全快照（读者用）：锁内拷贝，避免与并发发现竞态。</summary>
    public static List<ITool> GetDiscoveredToolsSnapshot()
    {
        lock (_toolsLock) return new List<ITool>(DiscoveredTools);
    }

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
    internal static McpTransportType DetectTransport(JNode server)
    {
        var transport = server["transport"]?.AsString();
        var url = server["url"]?.AsString();

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
            var servers = Json.Parse(json);
            if (servers == null) return;

            // 先尝试从缓存加载工具（加速启动）
            McpCache.Load(servers);

            foreach (var server in servers.Items)
            {
                var name = server?["name"]?.AsString();
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
    private static void RegisterState(string name, JNode server)
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
    private static async Task ConnectServerAsync(JNode server)
    {
        var name = server["name"]?.AsString() ?? "";
        var transportType = DetectTransport(server);
        var url = server["url"]?.AsString();
        var headers = ParseHeaders(server["headers"]);

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
                    var command = server["command"]?.AsString();
                    var args = server["args"]?.Items
                        ?.Select(a => a?.AsString() ?? "").ToArray() ?? [];

                    var env = new Dictionary<string, string>();
                    var envObj = server["env"];
                    if (envObj != null)
                    {
                        foreach (var kv in envObj.Entries)
                        {
                            var val = kv.Value?.AsString();
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
    internal static Dictionary<string, string>? ParseHeaders(JNode? headersObj)
    {
        if (headersObj == null || headersObj.Count == 0) return null;
        var headers = new Dictionary<string, string>();
        foreach (var kv in headersObj.Entries)
        {
            var val = kv.Value?.AsString();
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
            McpCache.Save(GetDiscoveredToolsSnapshot());
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
            McpCache.Save(GetDiscoveredToolsSnapshot());
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
            McpCache.Save(GetDiscoveredToolsSnapshot());
            UpdateInfo();
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} (SSE) 连接失败: {ex.GetType().Name}: {ex.Message}");
            SetStatus(name, McpServerStatus.Failed, error: ex.Message);
        }
    }

    /// <summary>执行 MCP 握手 + 能力发现（工具 / 资源 / 提示词，与传输无关）</summary>
    private static async Task DiscoverToolsAsync(McpConnection conn, string name)
    {
        // 握手: initialize
        var initResp = await conn.SendRequestAsync("initialize", JNode.Object()
            .Set("protocolVersion", "2024-11-05")
            .Set("capabilities", JNode.Object())
            .Set("clientInfo", JNode.Object()
                .Set("name", "WayCoder")
                .Set("version", "0.17.3")));

        if (initResp == null)
        {
            DebugLog.Log("mcp", $"MCP {name}: 握手失败");
            SetStatus(name, McpServerStatus.Failed, error: "握手超时");
            return;
        }

        // 发送 initialized 通知
        conn.SendNotification("notifications/initialized", JNode.Object());

        // 移除该服务器的旧工具/资源/提示词（缓存可能有旧版本）
        var prefix = $"mcp__{name}__";
        MutateTools(() => DiscoveredTools.RemoveAll(t => t.Name.StartsWith(prefix, StringComparison.Ordinal)));

        // 发现工具: tools/list
        int toolCount = 0;
        var toolsResp = await conn.SendRequestAsync("tools/list", JNode.Object());
        var tools = toolsResp?["result"]?["tools"];
        if (tools != null)
        {
            foreach (var toolNode in tools.Items)
            {
                MutateTools(() => DiscoveredTools.Add(new McpTool(name, toolNode, conn)));
                toolCount++;
            }
        }

        // 发现资源: resources/list
        var resourceCount = await DiscoverResourcesAsync(conn, name);

        // 发现提示词: prompts/list
        var promptCount = await DiscoverPromptsAsync(conn, name);

        _connections.Add(conn);
        SetStatus(name, McpServerStatus.Connected, toolCount: toolCount, connection: conn,
            resourceCount: resourceCount, promptCount: promptCount);
        UpdateInfo();
        DebugLog.Log("mcp", $"MCP {name}: 发现 {toolCount} 工具 · {resourceCount} 资源 · {promptCount} 提示词");
    }

    /// <summary>发现 MCP 资源（resources/list），注册为单个读取工具。返回资源数量。</summary>
    private static async Task<int> DiscoverResourcesAsync(McpConnection conn, string name)
    {
        try
        {
            var resp = await conn.SendRequestAsync("resources/list", JNode.Object());
            var resources = resp?["result"]?["resources"];
            if (resources == null || resources.Count == 0) return 0;
            MutateTools(() => DiscoveredTools.Add(new McpResourceTool(name, resources, conn)));
            return resources.Count;
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} 资源发现失败: {ex.GetType().Name}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>发现 MCP 提示词模板（prompts/list），每个注册为一个工具。返回提示词数量。</summary>
    private static async Task<int> DiscoverPromptsAsync(McpConnection conn, string name)
    {
        try
        {
            var resp = await conn.SendRequestAsync("prompts/list", JNode.Object());
            var prompts = resp?["result"]?["prompts"];
            if (prompts == null) return 0;
            int count = 0;
            foreach (var p in prompts.Items)
            {
                MutateTools(() => DiscoveredTools.Add(new McpPromptTool(name, p, conn)));
                count++;
            }
            return count;
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} 提示词发现失败: {ex.GetType().Name}: {ex.Message}");
            return 0;
        }
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
                if (st.ResourceCount > 0) sb.Append($" · {st.ResourceCount} 资源");
                if (st.PromptCount > 0) sb.Append($" · {st.PromptCount} 提示词");
                if (st.Error != null) sb.Append($" ({st.Error})");
                sb.Append('\n');
            }
            Info = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : "未配置";
        }
    }

    /// <summary>更新服务器状态。</summary>
    private static void SetStatus(string name, McpServerStatus status, int toolCount = 0,
        string? error = null, McpConnection? connection = null,
        int resourceCount = 0, int promptCount = 0)
    {
        lock (_stateLock)
        {
            if (!_states.TryGetValue(name, out var st)) return;
            st.Status = status;
            st.ToolCount = toolCount;
            st.ResourceCount = resourceCount;
            st.PromptCount = promptCount;
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
            try { conn.SendNotification("exit", JNode.Object()); } catch { }
            try { await conn.DisconnectAsync(); } catch { }
            _connections.Remove(conn);
        }

        // 移除该服务器的旧工具
        var prefix = $"mcp__{name}__";
        MutateTools(() => DiscoveredTools.RemoveAll(t => t.Name.StartsWith(prefix, StringComparison.Ordinal)));
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
            var servers = Json.Parse(File.ReadAllText(configPath, Encoding.UTF8));
            if (servers == null || servers.Count == 0) return "mcp_servers.json 为空或格式错误";

            var targets = new List<JNode>();
            foreach (var server in servers.Items)
            {
                var n = server?["name"]?.AsString();
                if (string.IsNullOrEmpty(n)) continue;
                if (name == null || n.Equals(name, StringComparison.OrdinalIgnoreCase))
                    targets.Add(server!);
            }

            if (targets.Count == 0)
                return name == null ? "无可用服务器" : $"未找到服务器 {name}";

            foreach (var server in targets)
            {
                var n = server["name"]!.AsString() ?? "";
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
                conn.SendNotification("exit", JNode.Object());
                _ = conn.DisconnectAsync();
            }
            catch { }
        }
        _connections.Clear();
        MutateTools(() => DiscoveredTools.Clear());
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
    public int ResourceCount { get; }
    public int PromptCount { get; }
    public string? Error { get; }

    public McpServerInfo(string name, string transport, McpServerStatus status, int toolCount, string? error,
        int resourceCount = 0, int promptCount = 0)
    {
        Name = name;
        Transport = transport;
        Status = status;
        ToolCount = toolCount;
        ResourceCount = resourceCount;
        PromptCount = promptCount;
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
    public int ResourceCount;
    public int PromptCount;
    public string? Error;
    public McpConnection? Connection;

    public McpServerInfo ToInfo() => new(Name, Transport, Status, ToolCount, Error, ResourceCount, PromptCount);
}

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
    public async Task<JNode?> SendRequestAsync(string method, JNode @params)
    {
        int id;
        lock (_lock) { id = _nextId++; }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await _transport.SendRequestAsync(id, method, @params, cts.Token);
    }

    /// <summary>发送 JSON-RPC 通知（无响应）</summary>
    public void SendNotification(string method, JNode @params)
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
    private readonly JNode _toolDef;
    private readonly McpConnection _connection;

    public string Name { get; }
    public string Description { get; }

    public JNode Parameters => _toolDef["inputSchema"]
        ?? JNode.Object().Set("type", "object").Set("properties", JNode.Object());

    public McpTool(string serverName, JNode toolDef, McpConnection connection)
    {
        _serverName = serverName;
        _toolDef = toolDef;
        _connection = connection;

        var toolName = toolDef["name"]?.AsString() ?? "unknown";
        Name = $"mcp__{serverName}__{toolName}";
        Description = toolDef["description"]?.AsString() ?? $"(MCP) {serverName}/{toolName}";
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var toolName = _toolDef["name"]?.AsString() ?? "";

        // 将参数字典转为 JsonObject
        var @params = JNode.Object()
            .Set("name", toolName)
            .Set("arguments", Json.Parse(JsonHelper.SerializeArgs(arguments))!);

        var resp = await _connection.SendRequestAsync("tools/call", @params);
        if (resp == null)
            return $"错误: MCP {_serverName}/{toolName} 调用超时";

        var error = resp["error"];
        if (error != null)
            return $"错误: MCP {_serverName}/{toolName} — {error["message"]?.AsString() ?? "未知错误"}";

        var result = resp["result"];
        var content = result?["content"];

        if (content is { Kind: JKind.Array } arr)
        {
            var texts = arr.Items.Select(n => n?["text"]?.AsString() ?? "").Where(t => t != "");
            return string.Join("\n", texts);
        }

        return result?.ToJson() ?? "(空结果)";
    }
}

// ============================================================
// MCP 资源 / 提示词 包装器
// ============================================================

/// <summary>
/// MCP 资源读取工具 — 将服务器的 resources 能力适配为 ITool。
/// 省略 uri 时列出全部资源；传 uri 时读取指定资源内容。
/// 工具名称格式: mcp__&lt;server&gt;__resources
/// </summary>
internal class McpResourceTool : ITool
{
    private readonly string _serverName;
    private readonly McpConnection _connection;
    private readonly List<(string Uri, string Name, string Desc)> _resources;

    public string Name { get; }
    public string Description { get; }

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("uri", JNode.Object()
                .Set("type", "string")
                .Set("description", "要读取的资源 URI（省略则列出所有可用资源）")));

    public McpResourceTool(string serverName, JNode resources, McpConnection connection)
    {
        _serverName = serverName;
        _connection = connection;
        _resources = [];

        foreach (var r in resources.Items)
        {
            var uri = r["uri"]?.AsString() ?? "";
            var rname = r["name"]?.AsString() ?? "";
            var desc = r["description"]?.AsString() ?? "";
            _resources.Add((uri, rname, desc));
        }

        Name = $"mcp__{serverName}__resources";

        var sb = new StringBuilder();
        sb.Append($"读取 MCP 服务器 {serverName} 提供的资源。省略 uri 参数列出全部资源；传入 uri 读取指定资源内容。可用资源：");
        foreach (var (uri, rname, desc) in _resources)
        {
            sb.Append($"\n- {rname} ({uri})");
            if (!string.IsNullOrEmpty(desc)) sb.Append($": {desc}");
        }
        Description = sb.ToString();
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        // 传了 uri → 读取指定资源；否则列出全部资源
        if (arguments.TryGetValue("uri", out var uriVal) && uriVal is string uri && !string.IsNullOrWhiteSpace(uri))
        {
            var readResp = await _connection.SendRequestAsync("resources/read", JNode.Object().Set("uri", uri));
            return FormatReadResult(readResp, uri);
        }

        var listResp = await _connection.SendRequestAsync("resources/list", JNode.Object());
        var resources = listResp?["result"]?["resources"];
        if (resources == null || resources.Count == 0) return "(无资源)";

        var sb = new StringBuilder();
        foreach (var r in resources.Items)
        {
            var u = r["uri"]?.AsString() ?? "";
            var n = r["name"]?.AsString() ?? "";
            var d = r["description"]?.AsString() ?? "";
            sb.Append($"{n} ({u})");
            if (!string.IsNullOrEmpty(d)) sb.Append($": {d}");
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    private string FormatReadResult(JNode? resp, string uri)
    {
        if (resp == null) return $"错误: MCP {_serverName} 读取资源 {uri} 超时";
        var error = resp["error"];
        if (error != null)
            return $"错误: MCP {_serverName} 读取资源 {uri} — {error["message"]?.AsString() ?? "未知错误"}";

        var contents = resp["result"]?["contents"];
        if (contents == null) return "(空资源)";

        var sb = new StringBuilder();
        foreach (var c in contents.Items)
        {
            var text = c["text"]?.AsString();
            if (text != null) { sb.Append(text); continue; }
            var blob = c["blob"]?.AsString();
            if (blob != null) { sb.Append($"[二进制 blob, base64 {blob.Length} 字符]"); continue; }
            var nestedUri = c["uri"]?.AsString();
            if (nestedUri != null) { sb.Append($"[嵌套资源: {nestedUri}]"); continue; }
        }
        return sb.Length > 0 ? sb.ToString() : "(空资源)";
    }
}

/// <summary>
/// MCP 提示词模板工具 — 将服务器的 prompts 能力适配为 ITool。
/// 每个提示词模板注册为一个工具，参数从模板 arguments 数组生成。
/// 工具名称格式: mcp__&lt;server&gt;__prompt__&lt;name&gt;
/// </summary>
internal class McpPromptTool : ITool
{
    private readonly string _serverName;
    private readonly JNode _promptDef;
    private readonly McpConnection _connection;

    public string Name { get; }
    public string Description { get; }
    public JNode Parameters { get; }

    public McpPromptTool(string serverName, JNode promptDef, McpConnection connection)
    {
        _serverName = serverName;
        _promptDef = promptDef;
        _connection = connection;

        var promptName = promptDef["name"]?.AsString() ?? "unknown";
        Name = $"mcp__{serverName}__prompt__{promptName}";
        Description = promptDef["description"]?.AsString() ?? $"(MCP) {serverName} 提示词 {promptName}";
        Parameters = BuildParameters(promptDef["arguments"]);
    }

    /// <summary>从 prompts/list 的 arguments 数组构造 inputSchema（纯逻辑，便于自测）。</summary>
    internal static JNode BuildParameters(JNode? args)
    {
        var properties = JNode.Object();
        if (args != null)
        {
            foreach (var a in args.Items)
            {
                var argName = a["name"]?.AsString();
                if (string.IsNullOrEmpty(argName)) continue;
                properties[argName] = JNode.Object()
                    .Set("type", "string")
                    .Set("description", a["description"]?.AsString() ?? argName);
            }
        }
        return JNode.Object().Set("type", "object").Set("properties", properties);
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var promptName = _promptDef["name"]?.AsString() ?? "";

        var @params = JNode.Object()
            .Set("name", promptName)
            .Set("arguments", Json.Parse(JsonHelper.SerializeArgs(arguments))!);

        var resp = await _connection.SendRequestAsync("prompts/get", @params);
        if (resp == null) return $"错误: MCP {_serverName} 提示词 {promptName} 调用超时";

        var error = resp["error"];
        if (error != null)
            return $"错误: MCP {_serverName} 提示词 {promptName} — {error["message"]?.AsString() ?? "未知错误"}";

        var messages = resp["result"]?["messages"];
        if (messages == null) return resp["result"]?.ToJson() ?? "(空提示词)";

        var sb = new StringBuilder();
        foreach (var m in messages.Items)
        {
            var role = m["role"]?.AsString() ?? "";
            var text = ExtractContentText(m["content"]);
            if (string.IsNullOrEmpty(text)) continue;
            sb.Append($"[{role}]\n{text}\n\n");
        }
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>提取 prompt 消息 content 的纯文本（content 可为字符串 / 对象 / 数组）。</summary>
    internal static string ExtractContentText(JNode? content)
    {
        if (content == null) return "";
        if (content.Kind == JKind.String)
            return content.AsString() ?? "";
        if (content.Kind == JKind.Object)
        {
            var text = content["text"]?.AsString();
            if (text != null) return text;
            var uri = content["uri"]?.AsString();
            if (uri != null) return $"[资源: {uri}]";
            return content.ToJson();
        }
        if (content.Kind == JKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var c in content.Items)
            {
                var text = c?["text"]?.AsString();
                if (text != null) sb.Append(text);
            }
            return sb.ToString();
        }
        return "";
    }
}
