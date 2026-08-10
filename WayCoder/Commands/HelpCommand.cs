using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class HelpCommand : SlashCommand
{
    public override string Name => "/help";
    public override string[] Aliases => ["/h"];
    public override string Description => "显示命令帮助";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var table = new List<string>();
        foreach (var cmd in SlashCommandRegistry.Commands)
        {
            var name = cmd.Usage ?? cmd.Name;
            var desc = cmd.Description;
            if (cmd.Aliases.Length > 0)
                name += $"  ({string.Join(", ", cmd.Aliases)})";
            table.Add($"{name,-36} {desc}");
        }

        screen.AddMessage("📋 **命令帮助**\n\n```\n" + string.Join("\n", table) + "\n```\n\n" +
            "快捷键: F1-F10 切换Agent | Ctrl+E 编辑器 | Ctrl+T 设置 | Ctrl+R 搜索 | Ctrl+M 切模型 | Ctrl+H 帮助 | Ctrl+B 面板 | Ctrl+Q 退出 | ↑↓ 历史",
            "system");
        return Task.CompletedTask;
    }
}
