using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class AboutCommand : SlashCommand
{
    public override string Name => "/about";
    public override string Description => L.Pick("关于 WayCoder", "About WayCoder");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg(
            $"{Global.AppFullName}\n" +
            L.Pick($"版本: {Global.Version}\n", $"Version: {Global.Version}\n") +
            L.Pick($"开发者: {Global.Developer}\n", $"Developer: {Global.Developer}\n") +
            L.Pick($"仓库: {Global.RepoUrl}\n", $"Repository: {Global.RepoUrl}\n") +
            L.Pick($"协议: {Global.License}", $"License: {Global.License}"));
        return Task.CompletedTask;
    }
}
