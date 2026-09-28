using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CompactCommand : SlashCommand
{
    public override string Name => "/compact";
    public override string[] Aliases => ["/c"];
    public override string Description => L.Pick("压缩上下文", "Compact the context");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return; }
        // 传活列表而非 SnapshotMessages()：MaybeCompressAsync 就地 Clear/Add 重写列表，
        // 传快照副本只会压缩副本、真实 _messages 一条不少（/compact 变成空操作）。
        await agent.Context.MaybeCompressAsync(agent.Messages, ProgramContext.LLM);
        screen.AddSystemMsg(L.Pick("✔ 上下文已压缩", "✔ Context compacted"));
    }
}
