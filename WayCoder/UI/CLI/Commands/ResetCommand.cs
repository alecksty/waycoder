using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class ResetCommand : SlashCommand
{
    public override string Name => "/reset";
    public override string[] Aliases => ["/r"];
    public override string Description => L.Pick("清空对话历史", "Clear the conversation history");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        ProgramContext.Agent?.Reset();
        screen.AddSystemMsg(L.Pick("♻ 对话已重置", "♻ Conversation reset"));
        return Task.CompletedTask;
    }
}
