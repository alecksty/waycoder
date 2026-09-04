using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class HelpCommand : SlashCommand
{
    public override string Name => "/help";
    public override string[] Aliases => ["/h"];
    public override string Description => "显示命令帮助";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        // ── CJK 感知对齐（对标 --help 的 AnsiString.DisplayWidth）──
        // 此前用 {name,-36} 按字符数填充：命令名/别名含中文/emoji（双宽）时左右列错位 → 帮助显示混乱；
        // 且左列用 cmd.Usage（部分超长）导致各行长度失控。改为：短名(Name+别名) 一列 + 描述一列，
        // 按 DisplayWidth 补空格（码点/代理对安全），仅 DisplayWidth 而非常规 String.Length。
        var cmds = SlashCommandRegistry.Commands
            .Select(c => (Name: c.Name, Alias: c.Aliases.Length > 0 ? $"({string.Join(", ", c.Aliases)})" : "", Desc: c.Description))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int nameW = cmds.Count > 0
            ? cmds.Max(c => AnsiString.DisplayWidth(c.Name) + (c.Alias.Length > 0 ? 1 + AnsiString.DisplayWidth(c.Alias) : 0))
            : 20;
        int descCol = nameW + 4;

        var sb = new System.Text.StringBuilder();
        sb.Append("📋 **命令帮助**\n\n```\n");
        foreach (var c in cmds)
        {
            var left = c.Name + (c.Alias.Length > 0 ? " " + c.Alias : "");
            int pad = descCol - AnsiString.DisplayWidth(left);
            if (pad > 0) left += new string(' ', pad);
            sb.Append(left).Append(c.Desc).Append('\n');
        }
        sb.Append("```\n\n");
        sb.Append("快捷键: F1-F10 切换Agent | Ctrl+E 编辑器 | Ctrl+T 设置 | Ctrl+R 搜索 | Ctrl+M 切模型 | Ctrl+H 帮助 | Ctrl+B 面板 | Ctrl+Q 退出 | ↑↓ 历史");

        screen.AddMessage(sb.ToString(), "system");
        return Task.CompletedTask;
    }
}
