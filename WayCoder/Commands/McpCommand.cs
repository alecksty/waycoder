using System.Text;
using WayCoder.Tools;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /mcp —— 对标 Claude Code /mcp：查看 MCP 服务器状态 / 重连。
/// 无参列出所有服务器（名称/传输/状态/工具数），`/mcp reload [name]` 重连。
/// </summary>
public class McpCommand : SlashCommand
{
    public override string Name => "/mcp";
    public override string Description => "查看 MCP 服务器状态 / 重连";
    public override string? Usage => "/mcp [reload [name]]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        // /mcp reload [name]
        if (args.StartsWith("reload", StringComparison.OrdinalIgnoreCase))
        {
            var name = args.Length > "reload".Length ? args["reload".Length..].Trim() : null;
            if (string.IsNullOrEmpty(name)) name = null;
            screen.AddSystemMsg($"🔌 {await McpManager.ReloadAsync(name)}");
            return;
        }

        // 无参：列出所有服务器状态
        var servers = McpManager.Servers;
        if (servers.Count == 0)
        {
            screen.AddSystemMsg("🔌 未配置 MCP 服务器。\n在 .waycoder/mcp_servers.json 添加后重启生效。");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"🔌 MCP 服务器 ({servers.Count})");
        foreach (var s in servers)
        {
            var mark = s.Status switch
            {
                McpServerStatus.Connected => "✅",
                McpServerStatus.Connecting => "⏳",
                McpServerStatus.Failed => "❌",
                _ => "❓",
            };
            sb.Append($"{mark} {s.Name} [{s.Transport}] {s.ToolCount} 工具");
            if (s.Error != null) sb.Append($" — {s.Error}");
            sb.AppendLine();
        }
        sb.AppendLine("重连: /mcp reload [name]（省略 name 重连全部）");
        screen.AddSystemMsg(sb.ToString().TrimEnd('\n'));
    }
}
