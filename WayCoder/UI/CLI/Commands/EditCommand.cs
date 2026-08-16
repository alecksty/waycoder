using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class EditCommand : SlashCommand
{
    public override string Name => "/edit";
    public override string Description => "终端源码编辑器";
    public override string? Usage => "/edit [文件路径]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        TuiManager.Instance.PushScreen(string.IsNullOrEmpty(args)
            ? new EditorScreen()
            : new EditorScreen(args));
        return Task.CompletedTask;
    }
}
