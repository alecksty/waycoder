using WayCoder.UI;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

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
