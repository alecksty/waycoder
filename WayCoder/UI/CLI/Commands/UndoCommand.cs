using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /undo —— 回退。两级：
///   /undo &lt;文件&gt; [n]     编辑级：回退该文件的 n 个编辑版本（默认 1 = 撤销最后一次编辑，来自 FileVersionStore）
///   /undo [编号] [文件]   轮级：回退到指定检查点（CheckpointManager 整树快照）
///   /undo -l              列出最近检查点文件
/// </summary>
public class UndoCommand : SlashCommand
{
    public override string Name => "/undo";
    public override string Description => "回退（/undo <文件> 编辑级 · /undo [编号] [文件] 检查点级）";
    public override string? Usage => "/undo <文件路径> [n] | /undo [检查点编号] [文件]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = (args ?? "").Trim();
        if (trimmed == "-l" || trimmed == "--list")
        {
            var files = CheckpointManager.GetCheckpointFiles();
            screen.AddSystemMsg(files.Count == 0
                ? "📌 没有检查点文件"
                : "📌 **检查点文件**\n" + string.Join("\n", files.Select(f => $"  {f}")));
            return;
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 编辑级回退：首参数非数字 → 当作文件路径（/undo <file> [n]）
        if (parts.Length > 0 && !int.TryParse(parts[0], out _))
        {
            var file = parts[0];
            int steps = 1;
            if (parts.Length > 1 && int.TryParse(parts[1], out var n) && n > 0) steps = n;
            if (FileVersionStore.Restore(file, steps))
                screen.AddSystemMsg($"↩️ 已回退 {file} 的 {steps} 个编辑版本（/versions {file} 查看历史）");
            else
                screen.AddSystemMsg($"🤷 无法回退 {file}：无可用编辑版本（需先编辑过该文件）。试试 /undo <检查点编号> [文件] 轮级回退。");
            return;
        }

        // 轮级回退：/undo [编号] [文件]
        int? checkpointId = null;
        string? filePath = null;
        if (parts.Length > 0 && int.TryParse(parts[0], out var id))
            checkpointId = id;
        if (parts.Length > 1)
            filePath = parts[1];

        var result = await CheckpointManager.UndoAsync(checkpointId, filePath);
        screen.AddSystemMsg(result);
    }
}
