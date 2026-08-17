using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class ResetCommand : SlashCommand
{
    public override string Name => "/reset";
    public override string[] Aliases => ["/r"];
    public override string Description => "清空对话历史";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        ProgramContext.Agent?.Reset();
        screen.AddSystemMsg("♻ 对话已重置");
        return Task.CompletedTask;
    }
}
