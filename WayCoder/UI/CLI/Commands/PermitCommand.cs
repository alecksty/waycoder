using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>切换权限模式（极简TINY/问答ACK/自动AUTO/智能SMART/畅通YOLO）。</summary>
public class PermitCommand : SlashCommand
{
    public override string Name => "/permit";
    public override string Description => "切换权限模式（极简TINY/问答ACK/自动AUTO/智能SMART/畅通YOLO）";
    public override string? Usage => "/permit <tiny|ack|auto|smart|yolo>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            screen.AddSystemMsg($"当前权限: {PermissionManager.FormatMode()}\n可选: tiny(极简·仅聊天) ack(问答) auto(自动) smart(智能) yolo(畅通)");
            return Task.CompletedTask;
        }
        PermissionManager.SetMode(args);
        screen.AddSystemMsg($"✅ 权限模式: {PermissionManager.FormatMode()}");
        return Task.CompletedTask;
    }
}
