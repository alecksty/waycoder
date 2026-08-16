using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class UndoCommand : SlashCommand
{
    public override string Name => "/undo";
    public override string Description => "回退检查点";
    public override string? Usage => "/undo [编号] [文件]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (args == "-l" || args == "--list")
        {
            var files = CheckpointManager.GetCheckpointFiles();
            screen.AddSystemMsg(files.Count == 0
                ? "📌 没有检查点文件"
                : "📌 **检查点文件**\n" + string.Join("\n", files.Select(f => $"  {f}")));
            return;
        }

        // 解析参数: /undo [编号] [文件]
        int? checkpointId = null;
        string? filePath = null;
        if (!string.IsNullOrEmpty(args))
        {
            var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[0], out var id))
                checkpointId = id;
            if (parts.Length > 1)
                filePath = parts[1];
        }

        var result = await CheckpointManager.UndoAsync(checkpointId, filePath);
        screen.AddSystemMsg(result);
    }
}
