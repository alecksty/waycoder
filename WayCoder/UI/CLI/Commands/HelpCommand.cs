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
            .Select(c => (Name: c.Name, Alias: c.Aliases.Length > 0 ? $"({string.Join(", ", c.Aliases)})" : "", Desc: c.Description))
            .ToList();

        // MAUI 端注入的界面导航命令（/open /sessions /files …）单独成组置顶，便于手机首屏看到；
        // 桌面无此类命令（Desc 均以「打开」开头）→ 分组为空时不影响原有帮助布局。
        bool IsNav((string Name, string Alias, string Desc) c)
            => c.Name == "/open" || c.Desc.StartsWith("打开", StringComparison.Ordinal);

        var nav = all.Where(IsNav).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var cmds = all.Where(c => !IsNav(c)).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        static int RowWidth(IReadOnlyList<(string Name, string Alias, string Desc)> list)
            => list.Count > 0
                ? list.Max(c => AnsiString.DisplayWidth(c.Name) + (c.Alias.Length > 0 ? 1 + AnsiString.DisplayWidth(c.Alias) : 0))
                : 20;

        void DumpBlock(System.Text.StringBuilder sb, IReadOnlyList<(string Name, string Alias, string Desc)> list, int descCol)
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
        sb.Append("快捷键: F1-F10 切换Agent | Ctrl+E 编辑器 | Ctrl+T 设置 | Ctrl+R 搜索 | Ctrl+M 切模型 | Ctrl+H 帮助 | Ctrl+B 面板 | Ctrl+Q 退出 | ↑↓ 历史");

        screen.AddMessage(sb.ToString(), "system");
        return Task.CompletedTask;
    }
}
