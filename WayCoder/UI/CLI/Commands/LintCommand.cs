using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class LintCommand : SlashCommand
{
    public override string Name => "/lint";
    public override string Description => "运行 Lint 检查";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new LintTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>());
        screen.AddSystemMsg($"🔍 Lint 结果:\n{result}");
    }
}
