using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class DebugOnCommand : SlashCommand
{
    public override string Name => "/debug-on";
    public override string Description => L.Pick("开启调试日志", "Enable the debug log");
    public override string? Usage => "/debug-on";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        DebugLog.Enable();
        screen.AddSystemMsg(L.Pick("🔊 调试日志已开启", "🔊 Debug log enabled"));
        return Task.CompletedTask;
    }
}

public class DebugOffCommand : SlashCommand
{
    public override string Name => "/debug-off";
    public override string Description => L.Pick("关闭调试日志", "Disable the debug log");
    public override string? Usage => "/debug-off";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        DebugLog.Disable();
        screen.AddSystemMsg(L.Pick("🔇 调试日志已关闭", "🔇 Debug log disabled"));
        return Task.CompletedTask;
    }
}
