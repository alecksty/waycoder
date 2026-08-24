using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

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
        // 成本护栏：预算进度条（留空=未设预算）
        var cfg = ProgramContext.Config;
        if (cfg.MaxBudgetUsd != null && cfg.MaxBudgetUsd.Value > 0)
        {
            var spent = llm.EstimatedCost ?? 0;
            var budget = cfg.MaxBudgetUsd.Value;
            var pct = Math.Clamp(spent / budget * 100.0, 0, 100);
            sb.AppendLine($"  预算:     {ProgressBar(pct)} {pct:F0}%  (${spent:F4} / ${budget:F2})");
            if (spent >= budget)
                sb.AppendLine($"           🛑 已超预算上限");
            else if (cfg.BudgetWarnPercent > 0 && spent >= budget * cfg.BudgetWarnPercent / 100.0)
                sb.AppendLine($"           ⚠️ 已过预警线（{cfg.BudgetWarnPercent:F0}%）");
        }
        sb.AppendLine($"  延迟:     {llm.LastLatencyMs}ms");
        sb.AppendLine($"  消息:     {agent?.SnapshotMessages().Count ?? 0} 条");
        sb.AppendLine($"  会话:     {SessionManager.ListSessions().Count} 个");
        sb.AppendLine($"  权限:     {SandboxManager.Level}");

        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }

    /// <summary>把 0-100 百分比渲染成 10 段块进度条（成本护栏仪表盘用）。</summary>
    private static string ProgressBar(double percent)
    {
        const int segments = 10;
        var filled = (int)Math.Round(percent / 100.0 * segments);
        filled = Math.Clamp(filled, 0, segments);
        return "[" + new string('█', filled) + new string('░', segments - filled) + "]";
    }
}
