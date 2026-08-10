using System.Security.Cryptography;
using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// MCP 工具发现缓存 — 持久化 tools/list 结果到磁盘，加速启动。
/// 缓存键基于服务器配置的 SHA256 哈希，配置变更自动失效。
/// 缓存有效期 24 小时，超时后后台异步刷新。
/// </summary>
internal static class McpCache
{
    private const int CacheTtlHours = 24;

    /// <summary>从缓存加载工具到 DiscoveredTools（同步，快速）</summary>
    public static void Load(JsonArray serverConfigs)
    {
        var cachePath = FindCacheFile();
        if (cachePath == null) return;

        try
        {
            var json = File.ReadAllText(cachePath, Encoding.UTF8);
            var cache = JsonNode.Parse(json)?.AsObject();
            var servers = cache?["servers"]?.AsArray();
            if (servers == null) return;

            foreach (var serverConfig in serverConfigs)
            {
                var name = serverConfig?["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(name)) continue;

                var canonicalId = GetCanonicalId(serverConfig!);
                if (canonicalId == null) continue;

                var key = ComputeCacheKey(name, canonicalId);

                // 查找匹配的缓存条目
                foreach (var entry in servers)
                {
                    var entryKey = entry?["key"]?.GetValue<string>();
                    if (entryKey != key) continue;

                    var cachedAtStr = entry?["cached_at"]?.GetValue<string>();
                    if (DateTime.TryParse(cachedAtStr, out var cachedAt))
                    {
                        if ((DateTime.UtcNow - cachedAt).TotalHours > CacheTtlHours)
                            continue; // 缓存已过期
                    }

                    var tools = entry?["tools"]?.AsArray();
                    if (tools == null || tools.Count == 0) continue;

                    // 从缓存恢复工具（使用 null! 连接，工具仅在缓存命中时存在）
                    var cachedCount = 0;
                    foreach (var toolNode in tools)
                    {
                        var toolObj = toolNode?.AsObject();
                        if (toolObj == null) continue;

                        // 缓存工具使用虚拟连接（实际使用时会走正常连接流程）
                        var mcpTool = new CachedMcpTool(name, toolObj);
                        McpManager.DiscoveredTools.Add(mcpTool);
                        cachedCount++;
                    }

                    DebugLog.Log("mcp", $"MCP 缓存命中: {name} ({cachedCount} 工具)");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP 缓存加载失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>将当前已发现的工具保存到缓存</summary>
    public static void Save(IReadOnlyList<ITool> discoveredTools)
    {
        if (discoveredTools.Count == 0) return;

        var configPath = Global.FindConfigFileInTree(Environment.CurrentDirectory, "mcp_servers.json");
        if (configPath == null) return;

        try
        {
            // 解析当前服务器配置以计算缓存键
            var configJson = File.ReadAllText(configPath, Encoding.UTF8);
            var servers = JsonNode.Parse(configJson)?.AsArray();
            if (servers == null) return;

            var entries = new JsonArray();

            foreach (var serverConfig in servers)
            {
                var name = serverConfig?["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(name)) continue;

                var canonicalId = GetCanonicalId(serverConfig!);
                if (canonicalId == null) continue;

                var key = ComputeCacheKey(name, canonicalId);
                var prefix = $"mcp__{name}__";

                // 提取该服务器的工具
                var serverTools = discoveredTools
                    .Where(t => t.Name.StartsWith(prefix, StringComparison.Ordinal))
                    .ToList();

                if (serverTools.Count == 0) continue;

                var toolsArr = new JsonArray();
                foreach (var tool in serverTools)
                {
                    var toolObj = new JsonObject
                    {
                        ["name"] = tool.Name.Substring(prefix.Length),
                        ["description"] = tool.Description,
                        ["inputSchema"] = tool.Parameters,
                    };
                    toolsArr.Add((JsonNode)toolObj);
                }

                entries.Add((JsonNode)new JsonObject
                {
                    ["key"] = key,
                    ["name"] = name,
                    ["tools"] = toolsArr,
                    ["cached_at"] = DateTime.UtcNow.ToString("o"),
                });
            }

            // 写入缓存文件
            var cacheDir = Path.GetDirectoryName(configPath)!;
            var cachePath = Path.Combine(cacheDir, "mcp_tool_cache.json");
            var cache = new JsonObject { ["servers"] = entries };
            File.WriteAllText(cachePath, cache.ToJsonString(), Encoding.UTF8);

            DebugLog.Log("mcp", $"MCP 缓存已保存: {entries.Count} 服务器");
        }
        catch (Exception ex)
        {
            DebugLog.Log("mcp", $"MCP 缓存保存失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>计算缓存键: SHA256(name|canonicalId)</summary>
    internal static string ComputeCacheKey(string name, string canonicalId)
    {
        var input = $"{name}|{canonicalId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hex = Convert.ToHexStringLower(hash);
        return $"{name}|{hex[..16]}"; // 前 16 个 hex 字符（64 bit）足够区分
    }

    /// <summary>获取服务器规范标识符，配置不变时标识符相同</summary>
    internal static string? GetCanonicalId(JsonNode serverConfig)
    {
        var url = serverConfig["url"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(url))
            return url;

        var command = serverConfig["command"]?.GetValue<string>();
        if (string.IsNullOrEmpty(command)) return null;

        var args = serverConfig["args"]?.AsArray()
            ?.Select(a => a?.GetValue<string>() ?? "").ToArray() ?? [];
        return $"{command}|{string.Join("|", args)}";
    }

    private static string? FindCacheFile()
    {
        var cwd = Environment.CurrentDirectory;
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in Global.ConfigDirSearchOrder)
            {
                var candidate = Path.Combine(dir, dirName, "mcp_tool_cache.json");
                if (File.Exists(candidate)) return candidate;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    // FindConfigFileInTree defined in Global.cs (shared with McpClient)
}

/// <summary>
/// 缓存的 MCP 工具 — 只在缓存命中时使用。
/// ExecuteAsync 会返回提示信息，真实调用走正常 MCP 连接。
/// 当后台异步发现完成时，缓存工具会被真实 McpTool 替换。
/// </summary>
internal class CachedMcpTool : ITool
{
    private readonly string _serverName;
    private readonly JsonObject _toolDef;

    public string Name { get; }
    public string Description { get; }

    public JsonObject Parameters => _toolDef["inputSchema"]?.AsObject()
        ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() };

    public CachedMcpTool(string serverName, JsonObject toolDef)
    {
        _serverName = serverName;
        _toolDef = toolDef;

        var toolName = toolDef["name"]?.GetValue<string>() ?? "unknown";
        Name = $"mcp__{serverName}__{toolName}";
        Description = toolDef["description"]?.GetValue<string>() ?? $"(MCP) {serverName}/{toolName}";
    }

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var toolName = _toolDef["name"]?.GetValue<string>() ?? "";
        return Task.FromResult(
            $"MCP 工具 {_serverName}/{toolName} 正在后台连接中，请稍后重试。\n" +
            $"缓存工具在服务器连接成功后会自动更新。");
    }
}
