using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class RecentCommand : SlashCommand
{
    public override string Name => "/recent";
    public override string Description => "最近修改文件";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var changed = EditFileTool.ChangedFiles;
        if (changed.Count == 0)
            screen.AddSystemMsg("📂 没有最近修改的文件");
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📂 **最近修改文件**");
            foreach (var f in changed.Take(15))
                sb.AppendLine($"  {f}");
            screen.AddSystemMsg(sb.ToString());
        }
        return Task.CompletedTask;
    }
}
