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
        var all = SlashCommandRegistry.Commands
            .Select(c => (Cmd: c, Name: c.Name, Alias: c.Aliases.Length > 0 ? $"({string.Join(", ", c.Aliases)})" : "", Desc: c.Description))
            .ToList();

        // 界面导航命令（移动端 MauiCommands.PageNav/OpenAnyCommand，IsNavCommand=true）单独成组置顶；
        // 判据用命令自带的 IsNavCommand 标记而非描述文本——描述以「打开」开头会误捕桌面 /menu「打开功能菜单…」。
        bool IsNav((ISlashCommand Cmd, string Name, string Alias, string Desc) c)
            => c.Cmd is SlashCommand s && s.IsNavCommand;

        var nav = all.Where(IsNav).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var cmds = all.Where(c => !IsNav(c)).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        static int RowWidth(IReadOnlyList<(ISlashCommand Cmd, string Name, string Alias, string Desc)> list)
            => list.Count > 0
                ? list.Max(c => AnsiString.DisplayWidth(c.Name) + (c.Alias.Length > 0 ? 1 + AnsiString.DisplayWidth(c.Alias) : 0))
                : 20;

        void DumpBlock(System.Text.StringBuilder sb, IReadOnlyList<(ISlashCommand Cmd, string Name, string Alias, string Desc)> list, int descCol)
        {
            foreach (var c in list)
            {
                var left = c.Name + (c.Alias.Length > 0 ? " " + c.Alias : "");
                int pad = descCol - AnsiString.DisplayWidth(left);
                if (pad > 0) left += new string(' ', pad);
                sb.Append(left).Append(c.Desc).Append('\n');
            }
        }

        var sb = new System.Text.StringBuilder();
        if (nav.Count > 0)
        {
            sb.Append("📱 **打开界面**（斜杠导航）\n\n```\n");
            DumpBlock(sb, nav, RowWidth(nav) + 4);
            sb.Append("```\n\n");
        }
        sb.Append("📋 **命令帮助**\n\n```\n");
        DumpBlock(sb, cmds, RowWidth(cmds) + 4);
        sb.Append("```\n\n");
        // 这行是 /help 的尾部速查 —— 键位以 TuiKeybindHelp.Groups 为准，改键时同步（此前写着
        // 「Ctrl+E 编辑器」「Ctrl+R 搜索」都不对：Ctrl+E=经济模式、Ctrl+R=同步二维码、搜索是 Ctrl+F）
        sb.Append("快捷键: F1-F10 槽位 | Shift+Tab 模式 | Ctrl+P 权限 | Ctrl+E 经济 | Ctrl+M 模型 | Ctrl+N 换连接 | Ctrl+U 菜单 | Ctrl+F 搜索 | Ctrl+H 帮助 | Ctrl+B 面板 | Ctrl+C 退出（紧急 Ctrl+Q）");

        screen.AddMessage(sb.ToString(), "system");
        return Task.CompletedTask;
    }
}
