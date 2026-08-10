using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

/// <summary>
/// /loop — 循环执行 Agent（实际逻辑由 Program.cs 预处理钩子完成）。
/// </summary>
public class LoopCommand : SlashCommand
{
    public override string Name => "/loop";
    public override string Description => "循环执行 Agent";
    public override string? Usage => "/loop [次数] <提示词>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg("🔄 Loop 模式 — 输入 /loop <次数> <提示词>");
        return Task.CompletedTask;
    }
}
