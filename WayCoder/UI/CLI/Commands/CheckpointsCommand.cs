using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CheckpointsCommand : SlashCommand
{
    public override string Name => "/checkpoints";
    public override string Description => L.Pick("列出检查点（/checkpoints prune [N] 清理最旧）", "List checkpoints (/checkpoints prune [N] removes the oldest)");
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
                ? L.Pick($"🧹 已清理 {removed} 个最旧检查点（保留最近 {keep} 个）", $"🧹 Removed {removed} oldest checkpoints (kept the most recent {keep})")
                : L.Pick("✅ 检查点未超上限，无需清理。", "✅ Checkpoints are under the limit; nothing to clean up."));
            return Task.CompletedTask;
        }

        var cps = CheckpointManager.ListCheckpoints();
        screen.AddSystemMsg(L.Pick("📌 **检查点列表**\n", "📌 **Checkpoint list**\n") + cps + L.Pick("\n（/checkpoints prune [N] 可清理最旧）", "\n(/checkpoints prune [N] removes the oldest)"));
        return Task.CompletedTask;
    }
}
