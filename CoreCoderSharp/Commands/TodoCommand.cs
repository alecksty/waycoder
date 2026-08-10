using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class TodoCommand : SlashCommand
{
    public override string Name => "/todo";
    public override string Description => "查看任务列表";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var items = TodoTool.Items;
        if (items.Count == 0)
            screen.AddSystemMsg("📋 任务列表为空");
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📋 **任务列表**");
            foreach (var item in items)
                sb.AppendLine($"  [{item.Status}] {item.Title}");
            screen.AddSystemMsg(sb.ToString());
        }
        return Task.CompletedTask;
    }
}
