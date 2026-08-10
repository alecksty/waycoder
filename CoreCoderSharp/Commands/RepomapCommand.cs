using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class RepomapCommand : SlashCommand
{
    public override string Name => "/repomap";
    public override string Description => "刷新仓库地图";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        RepoMapGenerator.Invalidate();
        RepoMapGenerator.Generate();
        screen.AddSystemMsg("🗺 仓库地图已刷新");
        return Task.CompletedTask;
    }
}
