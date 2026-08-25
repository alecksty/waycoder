using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /versions —— 列出文件的编辑版本历史（编辑级，来自 FileVersionStore）。
/// /versions &lt;文件&gt; 列指定文件；无参列出所有有版本的文件。
/// 回退用 /undo &lt;文件&gt; [n]。
/// </summary>
public class VersionsCommand : SlashCommand
{
    public override string Name => "/versions";
    public override string Description => "列出文件编辑版本历史";
    public override string? Usage => "/versions [文件路径]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var file = (args ?? "").Trim();

        if (file.Length == 0)
        {
            var all = FileVersionStore.ListAll();
            screen.AddSystemMsg(all.Count == 0
                ? "📭 暂无文件版本历史（编辑过文件后才有）。"
                : $"📚 有版本历史的文件（{all.Count}）：\n" + string.Join("\n", all.Select(f => $"  {f}")));
            return Task.CompletedTask;
        }

        var versions = FileVersionStore.List(file);
        if (versions.Count == 0)
        {
            screen.AddSystemMsg($"🤷 {file} 无编辑版本历史。");
            return Task.CompletedTask;
        }

        var sb = new StringBuilder($"📚 {file} 的编辑版本（{versions.Count}）：\n");
        foreach (var (ver, time) in versions)
            sb.AppendLine($"  v{ver:000}  {time:MM-dd HH:mm:ss}");
        sb.AppendLine($"回退: /undo {file} [n]");
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }
}
