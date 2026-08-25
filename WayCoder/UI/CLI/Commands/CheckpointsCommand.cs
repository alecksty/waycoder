using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CheckpointsCommand : SlashCommand
{
    public override string Name => "/checkpoints";
    public override string Description => "列出检查点（/checkpoints prune [N] 清理最旧）";
    public override string? Usage => "/checkpoints [prune [N]]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = (args ?? "").Trim();
        if (trimmed.StartsWith("prune", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int keep = Config.Instance.CheckpointMax;
            if (parts.Length > 1 && int.TryParse(parts[1], out var n) && n > 0) keep = n;
            var removed = CheckpointManager.Prune(keep);
            screen.AddSystemMsg(removed > 0
                ? $"🧹 已清理 {removed} 个最旧检查点（保留最近 {keep} 个）"
                : "✅ 检查点未超上限，无需清理。");
            return Task.CompletedTask;
        }

        var cps = CheckpointManager.ListCheckpoints();
        screen.AddSystemMsg("📌 **检查点列表**\n" + cps + "\n（/checkpoints prune [N] 可清理最旧）");
        return Task.CompletedTask;
    }
}
