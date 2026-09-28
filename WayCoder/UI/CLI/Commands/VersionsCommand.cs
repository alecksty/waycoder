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
    public override string Description => L.Pick("列出文件编辑版本历史", "List a file's edit version history");
    public override string? Usage => L.Pick("/versions [文件路径]", "/versions [file path]");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var file = (args ?? "").Trim();

        if (file.Length == 0)
        {
            var all = FileVersionStore.ListAll();
            screen.AddSystemMsg(all.Count == 0
                ? L.Pick("📭 暂无文件版本历史（编辑过文件后才有）。", "📭 No file version history yet (it appears once a file has been edited).")
                : L.Pick($"📚 有版本历史的文件（{all.Count}）：\n", $"📚 Files with version history ({all.Count}):\n") + string.Join("\n", all.Select(f => $"  {f}")));
            return Task.CompletedTask;
        }

        var versions = FileVersionStore.List(file);
        if (versions.Count == 0)
        {
            screen.AddSystemMsg(L.Pick($"🤷 {file} 无编辑版本历史。", $"🤷 {file} has no edit version history."));
            return Task.CompletedTask;
        }

        var sb = new StringBuilder(L.Pick($"📚 {file} 的编辑版本（{versions.Count}）：\n", $"📚 Edit versions of {file} ({versions.Count}):\n"));
        foreach (var (ver, time) in versions)
            sb.AppendLine($"  v{ver:000}  {time:MM-dd HH:mm:ss}");
        sb.AppendLine(L.Pick($"回退: /undo {file} [n]", $"Revert: /undo {file} [n]"));
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }
}
