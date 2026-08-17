using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class SettingsCommand : SlashCommand
{
    public override string Name => "/settings";
    public override string Description => "设置界面 (图形化)";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        TuiManager.Instance.PushScreen(new SettingsScreen());
        return Task.CompletedTask;
    }
}
