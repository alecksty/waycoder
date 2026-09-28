using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class TokensCommand : SlashCommand
{
    public override string Name => "/tokens";
    public override string[] Aliases => ["/t"];
    public override string Description => L.Pick("显示 Token 用量", "Show token usage");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var llm = ProgramContext.LLM;
        if (llm == null) { screen.AddSystemMsg(L.Pick("LLM 未初始化", "LLM not initialized")); return Task.CompletedTask; }
        var p = llm.TotalPromptTokens;
        var c = llm.TotalCompletionTokens;
        var latency = llm.LastLatencyMs;
        var tps = latency > 0 ? c / (latency / 1000.0) : 0;
        screen.AddSystemMsg(L.Pick(
            $"📊 Token 用量\n" +
            $"  输入: {p:N0}  |  输出: {c:N0}  |  合计: {p + c:N0}\n" +
            $"  延迟: {latency}ms  |  速度: {tps:F0} tok/s  |  请求: {llm.TotalRequests}",
            $"📊 Token usage\n" +
            $"  Input: {p:N0}  |  Output: {c:N0}  |  Total: {p + c:N0}\n" +
            $"  Latency: {latency}ms  |  Speed: {tps:F0} tok/s  |  Requests: {llm.TotalRequests}"));
        return Task.CompletedTask;
    }
}
