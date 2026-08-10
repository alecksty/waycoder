using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class DiffCommand : SlashCommand
{
    public override string Name => "/diff";
    public override string[] Aliases => ["/d"];
    public override string Description => "显示修改文件差异";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var changed = EditFileTool.ChangedFiles;
        if (changed.Count == 0)
        {
            screen.AddSystemMsg("📝 没有修改过的文件");
            return Task.CompletedTask;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📝 **修改文件**");
        foreach (var f in changed)
            sb.AppendLine($"  {f}");
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }
}
