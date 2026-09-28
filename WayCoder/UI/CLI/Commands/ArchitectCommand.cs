using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /architect — 切换 Architect 双模型模式。
///
/// 开启后，每次对话先用大模型出执行计划，再用小模型按计划执行。
/// 大模型做分析规划，小模型做具体编码 —— 兼顾质量和速度/成本。
/// </summary>
public class ArchitectCommand : SlashCommand
{
    public override string Name => "/architect";
    public override string[] Aliases => ["/架构师", "/a"];
    public override string Description => L.Pick("Architect 双模型模式：大模型出计划，小模型执行", "Architect two-model mode: big model plans, small model executes");
    public override string? Usage => "/architect [on|off]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null)
        {
            screen.AddMessage(L.Pick("⚠ Agent 未初始化", "⚠ Agent not initialized"), "system");
            return Task.CompletedTask;
        }

        var arg = args.Trim().ToLower();

        // 切换
        if (arg == "on" || arg == "1" || arg == "true")
        {
            if (agent.ArchitectMode)
            {
                screen.AddMessage(L.Pick("✅ Architect 模式已在运行中。\n\n大模型 → 出计划 → 小模型 → 执行代码", "✅ Architect mode is already running.\n\nBig model → plan → small model → execute code"), "system");
                return Task.CompletedTask;
            }
            agent.ArchitectMode = true;
            screen.AddMessage(L.Pick(
                $"🧠 **Architect 模式已开启**\n\n" +
                $"**流程**：你的需求 → **{ProgramContext.Config.Model}** (分析出计划) → **{ProgramContext.Config.SmallModel}** (逐步执行)\n\n" +
                "使用 **/architect off** 关闭此模式。",
                $"🧠 **Architect mode enabled**\n\n" +
                $"**Flow**: your request → **{ProgramContext.Config.Model}** (produces a plan) → **{ProgramContext.Config.SmallModel}** (executes step by step)\n\n" +
                "Use **/architect off** to turn it off."), "system");
        }
        else if (arg == "off" || arg == "0" || arg == "false")
        {
            if (!agent.ArchitectMode)
            {
                screen.AddMessage(L.Pick("ℹ Architect 模式未开启。使用 /architect on 开启。", "ℹ Architect mode is not on. Use /architect on to enable it."), "system");
                return Task.CompletedTask;
            }
            agent.ArchitectMode = false;
            // 清除 model override
            agent.LlmClient.ModelOverride = null;
            screen.AddMessage(L.Pick("✅ Architect 模式已关闭，恢复默认单模型模式。", "✅ Architect mode disabled; back to the default single-model mode."), "system");
        }
        else
        {
            // 无参数 → 显示当前状态
            var status = agent.ArchitectMode
                ? L.Pick($"🟢 **Architect 模式已开启**\n\n大模型: `{ProgramContext.Config.Model}` (规划)\n小模型: `{ProgramContext.Config.SmallModel}` (执行)", $"🟢 **Architect mode enabled**\n\nBig model: `{ProgramContext.Config.Model}` (planning)\nSmall model: `{ProgramContext.Config.SmallModel}` (execution)")
                : L.Pick("⚪ Architect 模式未开启\n\n使用 **/architect on** 开启双模型模式", "⚪ Architect mode is off\n\nUse **/architect on** to enable two-model mode");
            screen.AddMessage(status, "system");
        }

        return Task.CompletedTask;
    }
}
