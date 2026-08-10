using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

/// <summary>
/// /watch — 切换 Watch 模式（实际逻辑由 Program.cs 预处理钩子完成）。
/// </summary>
public class WatchCommand : SlashCommand
{
    public override string Name => "/watch";
    public override string Description => "切换 Watch 模式";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg("👁 Watch 模式 — 请通过 Program 钩子切换");
        return Task.CompletedTask;
    }
}
