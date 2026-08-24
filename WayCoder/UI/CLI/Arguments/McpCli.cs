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
            Console.WriteLine("未配置 MCP 服务器（--mcp-config <路径> 可指定配置文件）。");
            return 0;
        }
        Console.WriteLine($"MCP 服务器 ({servers.Count})");
        foreach (var s in servers)
        {
            var mark = s.Status switch
            {
                McpServerStatus.Connected => "✅",
                McpServerStatus.Connecting => "⏳",
                McpServerStatus.Failed => "❌",
                _ => "❓",
            };
            var src = s.Source == "claude" ? "〔Claude〕" : "";
            var line = $"{mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} 工具";
            if (s.ResourceCount > 0) line += $" · {s.ResourceCount} 资源";
            if (s.PromptCount > 0) line += $" · {s.PromptCount} 提示词";
            if (s.Error != null) line += $" — {s.Error}";
            Console.WriteLine(line);
        }
        Console.WriteLine("重连: --mcp reload [name]");
        return 0;
    }
}
