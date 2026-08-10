using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class ReviewCommand : SlashCommand
{
    public override string Name => "/review";
    public override string Description => "代码审查";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg("🔍 代码审查模式 — 待实现");
        return Task.CompletedTask;
    }
}
