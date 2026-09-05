using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /menu — 打开功能菜单（命令面板），一个入口调出大多数界面。
/// 对标 Claude Code / VS Code 命令面板。等价 Ctrl+Shift+P；经 ChatScreen.OnOpenCommandPalette
/// 回调打开（该回调仅在 TUI 界面存在时接线，故一次性模式/无界面下为空操作，安全）。
/// </summary>
public class MenuCommand : SlashCommand
{
    public override string Name => "/menu";
    public override string[] Aliases => ["/palette"];
    public override string Description => "打开功能菜单（模型/设置/会话/Diff 等界面直达 + 常用命令，等价 Ctrl+Shift+P）";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.OnOpenCommandPalette?.Invoke();
        return Task.CompletedTask;
    }
}
