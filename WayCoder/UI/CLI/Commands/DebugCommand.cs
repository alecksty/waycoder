using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class DebugOnCommand : SlashCommand
{
    public override string Name => "/debug-on";
    public override string Description => "开启调试日志";
    public override string? Usage => "/debug-on";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        DebugLog.Enable();
        screen.AddSystemMsg("🔊 调试日志已开启");
        return Task.CompletedTask;
    }
}

public class DebugOffCommand : SlashCommand
{
    public override string Name => "/debug-off";
    public override string Description => "关闭调试日志";
    public override string? Usage => "/debug-off";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        DebugLog.Disable();
        screen.AddSystemMsg("🔇 调试日志已关闭");
        return Task.CompletedTask;
    }
}
