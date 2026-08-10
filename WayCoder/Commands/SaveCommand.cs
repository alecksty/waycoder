using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class SaveCommand : SlashCommand
{
    public override string Name => "/save";
    public override string[] Aliases => ["/s"];
    public override string Description => "保存会话";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return Task.CompletedTask; }
        var id = SessionManager.SaveSession(agent.Messages, ProgramContext.Config.Model);
        screen.AddSystemMsg($"💾 会话已保存: {id}");
        return Task.CompletedTask;
    }
}
