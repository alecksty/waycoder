using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// Claude Code MCP 配置共用 —— 读取 Claude Code 已配置的 MCP 服务器，零配置复用到 WayCoder。
/// 已用 Claude Code 配好 MCP 的用户，打开 WayCoder 即可直接复用，无需重复配置。
///
/// Claude Code 的 MCP 配置存三处（scope 从高到低）：
///   1. 项目级 .mcp.json（cwd 向上查找）
///   2. user 级 ~/.claude.json 顶层 mcpServers
///   3. project 级 ~/.claude.json 的 projects.&lt;cwd 前缀&gt;.mcpServers
///
/// 格式差异：Claude Code 用「对象 keyed by name + type 字段」，WayCoder 用「数组 + transport 字段」，
/// 其余 command/args/env/url/headers 完全一致。转换后可直接喂给 McpManager。
/// 开关：WAYCODER_CLAUDE_MCP=0 关闭（默认开）。
/// </summary>
public static class ClaudeMcp
{
    /// <summary>是否启用 Claude Code MCP 共用（默认开，环境变量 WAYCODER_CLAUDE_MCP=0 关闭）。</summary>
    public static bool Enabled =>
        Environment.GetEnvironmentVariable("WAYCODER_CLAUDE_MCP") != "0";

    /// <summary>读取 Claude Code 三处 MCP 配置，转换为 WayCoder 数组格式（未配置/关闭返回空列表）。</summary>
    public static List<JNode> LoadServers()
    {
        var result = new List<JNode>();
        if (!Enabled) return result;

        // 1. 项目级 .mcp.json（cwd 向上逐级查找裸文件，非 .waycoder/ 下）
        var local = FindBareFileInTree(Environment.CurrentDirectory, ".mcp.json");
        if (local != null)
            MergeInto(result, ParseMcpServersFile(local));

        // 2/3. ~/.claude.json（user 级 + project 级）
        var claudeJson = Path.Combine(Global.Home, ".claude.json");
        if (File.Exists(claudeJson))
        {
            try
            {
                var root = Json.Parse(File.ReadAllText(claudeJson, Encoding.UTF8));
                if (root != null)
                {
                    // user 级
                    MergeInto(result, ParseMcpServersNode(root["mcpServers"]));
                    // project 级 projects.<cwd 前缀最长匹配>.mcpServers
                    MergeInto(result, ParseMcpServersNode(FindProjectScope(root)));
                }
            }
            catch (Exception ex)
            {
                DebugLog.Log("mcp", $"读取 Claude Code 配置失败: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>在 ~/.claude.json 的 projects 字典里找 cwd 的最长前缀匹配项，返回其 mcpServers。</summary>
    private static JNode? FindProjectScope(JNode root)
    {
        var projects = root["projects"];
        if (projects == null) return null;

        var cwd = Environment.CurrentDirectory;
        string? bestKey = null;
        foreach (var (key, _) in projects.Entries)
        {
            if (cwd.StartsWith(key, StringComparison.Ordinal)
                && (bestKey == null || key.Length > bestKey.Length))
                bestKey = key;
        }
        return bestKey == null ? null : projects[bestKey]?["mcpServers"];
    }

    /// <summary>解析单个 Claude Code mcpServers 对象（{ name: {...} }）为 WayCoder 数组。容忍空数组/空对象。</summary>
    private static List<JNode> ParseMcpServersNode(JNode? node)
    {
        var list = new List<JNode>();
        if (node == null) return list;

        foreach (var (name, cfg) in node.Entries)
        {
            var converted = ConvertEntry(name, cfg);
            if (converted != null) list.Add(converted);
        }
        return list;
    }

    /// <summary>解析 .mcp.json 文件（内容为 { mcpServers: {...} }）。</summary>
    private static List<JNode> ParseMcpServersFile(string file)
    {
        try
        {
            var root = Json.Parse(File.ReadAllText(file, Encoding.UTF8));
            return root == null ? [] : ParseMcpServersNode(root["mcpServers"] ?? root);
        }
        catch { return []; }
    }

    /// <summary>转换单个 Claude Code 条目 → WayCoder 节点：type → transport，其余字段透传。</summary>
    public static JNode? ConvertEntry(string name, JNode cc)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var node = JNode.Object().Set("name", name);

        var type = cc["type"]?.AsString() ?? "";
        if ("sse".Equals(type, StringComparison.OrdinalIgnoreCase))
            node.Set("transport", "sse");
        else if ("http".Equals(type, StringComparison.OrdinalIgnoreCase))
            node.Set("transport", "http");
        // stdio（type=="stdio" 或省略）：默认，无需 transport 字段

        if (cc.Has("command")) node.Set("command", cc["command"]!);
        if (cc.Has("args")) node.Set("args", cc["args"]!);
        if (cc.Has("env")) node.Set("env", cc["env"]!);
        if (cc.Has("url")) node.Set("url", cc["url"]!);
        if (cc.Has("headers")) node.Set("headers", cc["headers"]!);

        return node;
    }

    /// <summary>合并去重：同名（忽略大小写）跳过。</summary>
    private static void MergeInto(List<JNode> target, List<JNode> incoming)
    {
        foreach (var item in incoming)
        {
            var name = item["name"]?.AsString();
            if (string.IsNullOrEmpty(name)) continue;
            if (target.Any(t => string.Equals(t["name"]?.AsString(), name, StringComparison.OrdinalIgnoreCase)))
                continue;
            target.Add(item);
        }
    }

    /// <summary>从 cwd 向上逐级查找「裸文件」（不套 .waycoder/.corecoder 目录），返回完整路径，未找到 null。</summary>
    private static string? FindBareFileInTree(string cwd, string fileName)
    {
        var dir = cwd;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }
}
