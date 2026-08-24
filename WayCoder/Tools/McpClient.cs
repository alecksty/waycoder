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

    /// <summary>加锁执行工具列表变更（RemoveAll/Add/Clear 统一走这里）。变更后使 AllTools 缓存失效。</summary>
    private static void MutateTools(Action action)
    {
        lock (_toolsLock) action();
        ToolRegistry.InvalidateAllToolsCache();
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

    /// <summary>自定义 MCP 配置文件路径（--mcp-config 指定）；null 则按默认查找 .waycoder/mcp_servers.json</summary>
    public static string? ConfigPathOverride { get; set; }

    /// <summary>
    /// 从配置文件初始化所有 MCP 服务器连接。
    /// 先尝试从缓存加载工具（快速启动），再异步连接发现。
    /// 配置来源两处：WayCoder 自己的 mcp_servers.json（优先）+ Claude Code 已配置的 MCP（去重追加）。
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        // 合并结果：WayCoder 自己的服务器在前，Claude Code 的服务器去重后追加。
        var merged = JNode.Array();
        var claudeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. WayCoder 自己的 mcp_servers.json（优先）
        var configPath = ConfigPathOverride ?? Global.FindConfigFileInTree(Environment.CurrentDirectory, "mcp_servers.json");
        if (configPath != null)
        {
            try
            {
                var own = Json.Parse(File.ReadAllText(configPath, Encoding.UTF8));
                if (own != null)
                    foreach (var s in own.Items) merged.Add(s);
            }
            catch (Exception ex)
            {
                DebugLog.Log("mcp", $"MCP 配置解析失败: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // 2. Claude Code 服务器（同名忽略大小写去重，来源标记 claude）
        foreach (var server in ClaudeMcp.LoadServers())
        {
            var name = server["name"]?.AsString();
            if (string.IsNullOrEmpty(name)) continue;
            if (merged.Items.Any(s => string.Equals(s["name"]?.AsString(), name, StringComparison.OrdinalIgnoreCase)))
                continue;
            merged.Add(server);
            claudeNames.Add(name);
        }

        if (merged.Count == 0) return;

        // 先尝试从缓存加载工具（加速启动）
        McpCache.Load(merged);

        foreach (var server in merged.Items)
        {
            var name = server["name"]?.AsString();
            if (string.IsNullOrEmpty(name)) continue;
            RegisterState(name, server, claudeNames.Contains(name) ? "claude" : "waycoder");
            _ = ConnectServerAsync(server);
        }
    }

    /// <summary>注册服务器状态（初始 Connecting）。source 标记配置来源：waycoder / claude。</summary>
    private static void RegisterState(string name, JNode server, string source = "waycoder")
    {
        lock (_stateLock)
        {
            if (!_states.ContainsKey(name))
                _states[name] = new McpServerState
                {
                    Name = name,
                    Transport = DetectTransport(server).ToString().ToLowerInvariant(),
                    Source = source,
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
                                env[kv.Key] = ExpandEnvVars(val);
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
        var configPath = ConfigPathOverride ?? Global.FindConfigFileInTree(Environment.CurrentDirectory, "mcp_servers.json");
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
    /// 把一个服务器节点写入 mcp_servers.json（去重 + 跳过示例项）。
    /// 供 /mcp add 使用：成功后返回 true 与配置路径；已存在返回 false。
    /// </summary>
    public static (bool Success, string Message) AddServerToConfig(JNode server)
    {
        var name = server["name"]?.AsString();
        if (string.IsNullOrEmpty(name))
            return (false, "服务器配置缺少 name");

        var cwd = Environment.CurrentDirectory;
        var waycoderDir = Global.FindExistingConfigDir(cwd);
        // FindExistingConfigDir 只返回目录名（.waycoder/.corecoder），且可能命中祖先目录；
        // 一律按 cwd 下同名目录写入，并确保目录存在（祖先目录命中时 cwd 下可能没有该目录）。
        var targetDir = Path.Combine(cwd, waycoderDir ?? ".waycoder");
        Directory.CreateDirectory(targetDir);

        var mcpPath = Path.Combine(targetDir, "mcp_servers.json");
        var existing = JNode.Array();
        if (File.Exists(mcpPath))
        {
            try
            {
                var existingJson = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
                if (existingJson is { Kind: JKind.Array } arr)
                {
                    foreach (var item in arr.Items)
                    {
                        var comment = item?["_comment"]?.AsString() ?? "";
                        if (!comment.Contains("示例"))
                            existing.Add(item!.Clone()!);
                    }
                }
            }
            catch { /* 旧配置损坏则重建 */ }
        }

        var existingNames = existing.Items
            .Select(e => e?["name"]?.AsString())
            .Where(n => n != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (existingNames.Contains(name))
            return (false, $"服务器 {name} 已存在配置中");

        existing.Add(server);
        try
        {
            File.WriteAllText(mcpPath, existing.ToJson(true), Encoding.UTF8);
            return (true, mcpPath);
        }
        catch (Exception ex)
        {
            return (false, $"写入失败: {ex.Message}");
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
    /// <summary>配置来源："waycoder"（本应用 mcp_servers.json）/ "claude"（Claude Code 共用）。</summary>
    public string Source { get; }

    public McpServerInfo(string name, string transport, McpServerStatus status, int toolCount, string? error,
        int resourceCount = 0, int promptCount = 0, string source = "waycoder")
    {
        Name = name;
        Transport = transport;
        Status = status;
        ToolCount = toolCount;
        ResourceCount = resourceCount;
        PromptCount = promptCount;
        Error = error;
        Source = source;
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
    public string Source = "waycoder";

    public McpServerInfo ToInfo() => new(Name, Transport, Status, ToolCount, Error, ResourceCount, PromptCount, Source);
}
