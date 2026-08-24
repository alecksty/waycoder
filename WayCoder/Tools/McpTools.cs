using System.Text;

namespace WayCoder.Tools;

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
