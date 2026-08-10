using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class CompactCommand : SlashCommand
{
    public override string Name => "/compact";
    public override string[] Aliases => ["/c"];
    public override string Description => "压缩上下文";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return; }
        await agent.Context.MaybeCompressAsync(agent.Messages, ProgramContext.LLM);
        screen.AddSystemMsg("✔ 上下文已压缩");
    }
}
