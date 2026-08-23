using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /free-restore — 恢复 /free 切换免费模型之前的模型（省钱探索后一键切回）。
/// </summary>
public class FreeRestoreCommand : SlashCommand
{
    public override string Name => "/free-restore";
    public override string[] Aliases => ["/恢复模型"];
    public override string Description => "恢复 /free 切换前的模型";
    public override string? Usage => "/free-restore";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg(ModelCli.RestorePrevious());
        return Task.CompletedTask;
    }
}
