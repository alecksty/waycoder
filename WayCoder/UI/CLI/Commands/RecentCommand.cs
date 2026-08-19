using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 最近修改文件列表。用法：/recent
/// （diff 预览已拆分到 /diff 命令，本命令只列文件名）
/// </summary>
public class RecentCommand : SlashCommand
{
    public override string Name => "/recent";
    public override string Description => "最近修改文件";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var changed = EditFileTool.ChangedFiles;
        if (changed.Count == 0)
        {
            screen.AddSystemMsg("📂 没有最近修改的文件");
            return Task.CompletedTask;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📂 **最近修改文件**");
        foreach (var f in changed.Take(20))
            sb.AppendLine($"  {f}");
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }
}
