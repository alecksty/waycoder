using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class StatsCommand : SlashCommand
{
    public override string Name => "/stats";
    public override string Description => "显示用量统计";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var llm = ProgramContext.LLM;
        var agent = ProgramContext.Agent;
        if (llm == null) { screen.AddSystemMsg("LLM 未初始化"); return Task.CompletedTask; }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📊 **用量统计**");
        sb.AppendLine();
        sb.AppendLine($"  模型:     {ProgramContext.Config.Model}");
        sb.AppendLine($"  Token:    {llm.TotalPromptTokens:N0} 入 / {llm.TotalCompletionTokens:N0} 出 / {llm.TotalPromptTokens + llm.TotalCompletionTokens:N0} 合计");
        var totalRmb = llm.EstimatedCost * 7.25;
        sb.AppendLine($"  总花费:   ¥{totalRmb?.ToString("F2") ?? "N/A"}");
        if (llm.TaskPromptTokens > 0 || llm.TaskCompletionTokens > 0)
        {
            var taskRmb = llm.TaskCost * 7.25;
            sb.AppendLine($"  当前任务: {llm.TaskPromptTokens:N0}+{llm.TaskCompletionTokens:N0} 词元 · ¥{taskRmb?.ToString("F4") ?? "N/A"}");
        }
        sb.AppendLine($"  延迟:     {llm.LastLatencyMs}ms");
        sb.AppendLine($"  消息:     {agent?.Messages.Count ?? 0} 条");
        sb.AppendLine($"  会话:     {SessionManager.ListSessions().Count} 个");
        sb.AppendLine($"  权限:     {SandboxManager.Level}");

        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }
}
