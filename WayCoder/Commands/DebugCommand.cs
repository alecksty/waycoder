using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class DebugCommand : SlashCommand
{
    public override string Name => "/debug-on";
    public override string[] Aliases => ["/debug-off"];
    public override string Description => "开启 / 关闭调试日志";
    public override string? Usage => "/debug-on 或 /debug-off";

    public override bool Matches(string input)
    {
        return string.Equals(input, "/debug-on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input, "/debug-off", StringComparison.OrdinalIgnoreCase);
    }

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (DebugLog.Enabled)
        {
            DebugLog.Disable();
            screen.AddSystemMsg("🔇 调试日志已关闭");
        }
        else
        {
            DebugLog.Enable();
            screen.AddSystemMsg("🔊 调试日志已开启");
        }
        return Task.CompletedTask;
    }
}
