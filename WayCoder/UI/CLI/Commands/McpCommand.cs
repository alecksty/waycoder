using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /mcp —— 对标 Claude Code /mcp：查看 MCP 服务器状态 / 重连 / 生态目录一键添加。
/// 无参列出所有服务器（名称/传输/状态/工具数），`/mcp reload [name]` 重连，
/// `/mcp list [关键词]` 浏览内置目录，`/mcp add <name>` 一键写入并重连。
/// </summary>
public class McpCommand : SlashCommand
{
    public override string Name => "/mcp";
    public override string Description => L.Pick("查看 MCP 服务器状态 / 重连 / 目录添加", "Show MCP server status / reconnect / add from the catalog");
    public override string? Usage => L.Pick("/mcp [reload [name] | list [关键词] | add <name>]", "/mcp [reload [name] | list [keyword] | add <name>]");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        // /mcp add <name> —— 从内置目录一键添加
        if (args.StartsWith("add", StringComparison.OrdinalIgnoreCase))
        {
            var name = args.Length > "add".Length ? args["add".Length..].Trim() : "";
            if (name.Length == 0)
            {
                screen.AddSystemMsg(L.Pick("用法: /mcp add <名称>（用 /mcp list 查看可添加的服务器）", "Usage: /mcp add <name> (use /mcp list to see the servers you can add)"));
                return;
            }

            var entry = McpCatalog.Find(name);
            if (entry == null)
            {
                screen.AddSystemMsg(L.Pick($"❌ 目录中没有服务器「{name}」。用 /mcp list 查看可用名称。", $"❌ No server named {name} in the catalog. Use /mcp list to see the available names."));
                return;
            }

            var (ok, msg) = McpManager.AddServerToConfig(McpCatalog.ToServerNode(entry));
            if (!ok)
            {
                screen.AddSystemMsg($"❌ {msg}");
                return;
            }

            // 写配置成功 → 立即热重连（免重启生效）
            screen.AddSystemMsg(L.Pick($"✅ 已添加 {entry.Name}（{entry.Description}）\n{await McpManager.ReloadAsync(entry.Name)}", $"✅ Added {entry.Name} ({entry.Description})\n{await McpManager.ReloadAsync(entry.Name)}"));
            return;
        }

        // /mcp list [关键词] —— 浏览内置目录
        if (args.StartsWith("list", StringComparison.OrdinalIgnoreCase))
        {
            var kw = args.Length > "list".Length ? args["list".Length..].Trim() : "";
            var found = McpCatalog.Search(kw.Length == 0 ? null : kw);

            var catSb = new StringBuilder();
            catSb.AppendLine(kw.Length == 0
                ? L.Pick($"📦 内置 MCP 服务器目录 ({found.Count})", $"📦 Built-in MCP server catalog ({found.Count})")
                : L.Pick($"📦 匹配「{kw}」的 MCP 服务器 ({found.Count})", $"📦 MCP servers matching {kw} ({found.Count})"));

            string? lastCat = null;
            foreach (var e in found)
            {
                if (e.Category != lastCat)
                {
                    lastCat = e.Category;
                    catSb.AppendLine(L.Pick($"\n【{e.Category}】", $"\n[{e.Category}]"));
                }
                catSb.AppendLine($"  {e.Name,-20} {e.Description}");
            }
            catSb.AppendLine(L.Pick("\n一键添加: /mcp add <名称>（如 /mcp add git）", "\nAdd one with a single command: /mcp add <name> (e.g. /mcp add git)"));
            screen.AddSystemMsg(catSb.ToString().TrimEnd('\n'));
            return;
        }

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
            screen.AddSystemMsg(L.Pick("🔌 未配置 MCP 服务器。\n用 /mcp list 浏览内置目录，/mcp add <名称> 一键添加。", "🔌 No MCP server configured.\nUse /mcp list to browse the built-in catalog, and /mcp add <name> to add one."));
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(L.Pick($"🔌 MCP 服务器 ({servers.Count})", $"🔌 MCP servers ({servers.Count})"));
        foreach (var s in servers)
        {
            var mark = McpStatusIcon.Text(s.Status);var src = s.Source == "claude" ? L.Pick("〔Claude〕", "(Claude)") : "";
            sb.Append(L.Pick($"{mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} 工具", $"{mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} tools"));
            if (s.ResourceCount > 0) sb.Append(L.Pick($" · {s.ResourceCount} 资源", $" · {s.ResourceCount} resources"));
            if (s.PromptCount > 0) sb.Append(L.Pick($" · {s.PromptCount} 提示词", $" · {s.PromptCount} prompts"));
            if (s.Error != null) sb.Append($" — {s.Error}");
            sb.AppendLine();
        }
        sb.AppendLine(L.Pick("重连: /mcp reload [name]（省略 name 重连全部）", "Reconnect: /mcp reload [name] (omit name to reconnect all)"));
        sb.AppendLine(L.Pick("目录: /mcp list · 添加: /mcp add <名称>", "Catalog: /mcp list · Add: /mcp add <name>"));
        screen.AddSystemMsg(sb.ToString().TrimEnd('\n'));
    }
}
