using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class AboutCommand : SlashCommand
{
    public override string Name => "/about";
    public override string Description => "关于 WayCoder";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg(
            $"{Global.AppFullName}\n" +
            $"版本: {Global.Version}\n" +
            $"开发者: {Global.Developer}\n" +
            $"仓库: {Global.RepoUrl}\n" +
            $"协议: {Global.License}");
        return Task.CompletedTask;
    }
}
