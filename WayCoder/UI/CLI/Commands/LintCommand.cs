using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class LintCommand : SlashCommand
{
    public override string Name => "/lint";
    public override string Description => L.Pick("运行 Lint 检查", "Run the lint check");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new LintTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>());
        screen.AddSystemMsg(L.Pick($"🔍 Lint 结果:\n{result}", $"🔍 Lint result:\n{result}"));
    }
}
