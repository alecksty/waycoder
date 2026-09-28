using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class TodoCommand : SlashCommand
{
    public override string Name => "/todo";
    public override string Description => L.Pick("查看任务列表", "Show the task list");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var items = TodoTool.Items;
        if (items.Count == 0)
            screen.AddSystemMsg(L.Pick("📋 任务列表为空", "📋 The task list is empty"));
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(L.Pick("📋 **任务列表**", "📋 **Task list**"));
            foreach (var item in items)
                sb.AppendLine($"  [{item.Status}] {item.Title}");
            screen.AddSystemMsg(sb.ToString());
        }
        return Task.CompletedTask;
    }
}
