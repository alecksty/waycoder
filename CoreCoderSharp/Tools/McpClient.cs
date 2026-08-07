using System.Diagnostics;
using System.Text;

namespace CoreCoderSharp.Tools;

/// <summary>
/// MCP (Model Context Protocol) stdio 客户端。
/// 通过启动外部进程，使用 JSON-RPC over stdin/stdout 通信，
/// 自动发现 MCP 服务器提供的工具并注册到 CoreCoder。
///
/// 配置: .corecoder/mcp_servers.json
/// [
///   { "name": "filesystem", "command": "npx", "args": ["-y", "@modelcontextprotocol/server-filesystem", "."] }
/// ]
/// </summary>
public static class McpManager
{
    private static readonly List<McpConnection> _connections = [];
    private static bool _initialized;

    /// <summary>所有已发现的 MCP 工具</summary>
    public static List<ITool> DiscoveredTools { get; } = [];

    /// <summary>
    /// 从配置文件初始化所有 MCP 服务器连接。
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

            foreach (var server in servers)
            {
                var name = server?["name"]?.GetValue<string>();
                var command = server?["command"]?.GetValue<string>();
                var args = server?["args"]?.AsArray()
                    ?.Select(a => a?.GetValue<string>() ?? "").ToArray() ?? [];

                // 解析环境变量（MCP 服务器通常需要 API Key 等）
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

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(command))
                {
                    _ = ConnectAndDiscoverAsync(name, command, args, env);
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP 初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 连接服务器并发现工具（异步，不阻塞启动）。
    /// </summary>
    private static async Task ConnectAndDiscoverAsync(string name, string command, string[] args,
        Dictionary<string, string>? env = null)
    {
        try
        {
            var conn = new McpConnection(name, command, args);

            // 启动进程
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args.Select(EscapeArg)),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            // 注入环境变量（MCP 服务器 API Key 等）
            if (env != null)
            {
                foreach (var (key, value) in env)
                    startInfo.EnvironmentVariables[key] = value;
            }

            conn.Process = new Process { StartInfo = startInfo };
            conn.Process.Start();

            // 握手: initialize
            var initResp = await conn.SendRequestAsync("initialize", new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = "WayCoder",
                    ["version"] = "0.7.0",
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
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {name} 连接失败: {ex.Message}");
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
                conn.Process?.Kill();
                conn.Process?.Dispose();
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

    private static string EscapeArg(string arg)
    {
        if (arg.Contains(' ') || arg.Contains('"'))
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        return arg;
    }
}

/// <summary>
/// MCP 连接 — 管理与单个 MCP 服务器的 JSON-RPC 通信。
/// </summary>
internal class McpConnection
{
    public string Name { get; }
    public string Command { get; }
    public string[] Args { get; }
    public Process? Process { get; set; }

    private int _nextId = 1;
    private readonly object _lock = new();

    public McpConnection(string name, string command, string[] args)
    {
        Name = name;
        Command = command;
        Args = args;
    }

    /// <summary>
    /// 发送 JSON-RPC 请求并等待响应。
    /// </summary>
    public async Task<JsonObject?> SendRequestAsync(string method, JsonObject @params)
    {
        if (Process == null) return null;

        int id;
        lock (_lock) { id = _nextId++; }

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        };

        try
        {
            var json = request.ToJsonString();
            lock (_lock)
            {
                Process.StandardInput.WriteLine(json);
                Process.StandardInput.Flush();
            }

            // 读取响应（可能多行，找到匹配 id 的）
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!cts.Token.IsCancellationRequested)
            {
                var line = await Process.StandardOutput.ReadLineAsync(cts.Token);
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
            DebugLog.Log("mcp", $"MCP {Name}: 请求 {method} 失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 发送 JSON-RPC 通知（无响应）。
    /// </summary>
    public void SendNotification(string method, JsonObject @params)
    {
        if (Process == null) return;

        var notif = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params,
        };

        try
        {
            lock (_lock)
            {
                Process.StandardInput.WriteLine(notif.ToJsonString());
                Process.StandardInput.Flush();
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP {Name}: 通知 {method} 失败: {ex.Message}");
        }
    }
}

/// <summary>
/// MCP 工具包装器 — 将 MCP 工具适配为 CoreCoder ITool 接口。
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
