using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

/// <summary>MCP 管理 CLI 纯逻辑（列出 / 重连，输出到 Console）。</summary>
public static class McpCli
{
    public static int Run(List<string> values)
    {
        if (values.Count > 0 && values[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            var name = values.Count > 1 ? values[1] : null;
            Console.WriteLine(McpManager.ReloadAsync(name).GetAwaiter().GetResult());
            return 0;
        }

        var servers = McpManager.Servers;
        if (servers.Count == 0)
        {
            Console.WriteLine(L.Pick(
                "未配置 MCP 服务器（--mcp-config <路径> 可指定配置文件）。",
                "No MCP servers configured (use --mcp-config <path> to point at a config file)."));
            return 0;
        }
        Console.WriteLine(L.Pick($"MCP 服务器 ({servers.Count})", $"MCP servers ({servers.Count})"));
        foreach (var s in servers)
        {
            var mark = McpStatusIcon.Text(s.Status);
            var src = s.Source == "claude" ? L.Pick("〔Claude〕", "[Claude]") : "";
            var line = L.Pick($"{mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} 工具",
                              $"{mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} tools");
            if (s.ResourceCount > 0)
                line += L.Pick($" · {s.ResourceCount} 资源", $" · {s.ResourceCount} resources");
            if (s.PromptCount > 0)
                line += L.Pick($" · {s.PromptCount} 提示词", $" · {s.PromptCount} prompts");
            if (s.Error != null) line += $" — {s.Error}";
            Console.WriteLine(line);
        }
        Console.WriteLine(L.Pick("重连: --mcp reload [name]", "Reconnect: --mcp reload [name]"));
        return 0;
    }
}
