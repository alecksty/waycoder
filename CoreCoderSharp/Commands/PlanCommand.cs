using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

/// <summary>
/// /plan — 计划模式（实际逻辑由 Program.cs 预处理钩子完成）。
/// </summary>
public class PlanCommand : SlashCommand
{
    public override string Name => "/plan";
    public override string Description => "计划模式";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg("📋 计划模式 — 请输入需求描述，Agent 将先规划再执行");
        return Task.CompletedTask;
    }
}
