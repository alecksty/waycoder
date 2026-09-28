using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class RepomapCommand : SlashCommand
{
    public override string Name => "/repomap";
    public override string Description => L.Pick("刷新仓库地图", "Refresh the repository map");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        RepoMapGenerator.Invalidate();
        RepoMapGenerator.Generate();
        screen.AddSystemMsg(L.Pick("🗺 仓库地图已刷新", "🗺 Repository map refreshed"));
        return Task.CompletedTask;
    }
}
