using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /free — 扫描可用的免费模型，弹菜单一键切换（省钱）。
/// 覆盖 opencode zen `-free` / openrouter `:free` 等模型，列出可用项供选择。
/// </summary>
public class FreeCommand : SlashCommand
{
    public override string Name => "/free";
    public override string[] Aliases => ["/免费"];
    public override string Description => "扫描可用免费模型，菜单切换（省钱）";
    public override string? Usage => "/free";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 扫描可用 free 模型（发简单请求验证，有 key 的才测）
        screen.AddSystemMsg("🔍 正在扫描可用免费模型（每模型 ~15s，请稍候）…");
        var available = ModelCli.ScanFreeAvailable(15);
        if (available.Count == 0)
        {
            screen.AddSystemMsg("⚠️ 暂无可用免费模型（确认已 --model import online opencode-zen / openrouter 导入且有 key）");
            return Task.CompletedTask;
        }
        // 弹菜单：显示短名 + provider，选中切换
        var names = available
            .Select(m => $"{ModelCatalog.ShortDisplayName(m.Id)}  （{m.ProviderId}）")
            .ToList();
        TuiDialog.Select("💰 免费模型（可用，省钱）", names, idx =>
        {
            if (idx >= 0 && idx < available.Count)
            {
                var m = available[idx];
                // 切换前记住当前模型（/free-restore 可恢复；未记录才记，不覆盖已记住的）
                ModelCli.RememberCurrentModel();
                ConnectionConfig.ApplyModelChoice(m.ProviderId, m.Id, isLarge: true, out var msg, m.DefaultBaseUrl);
                screen.AddSystemMsg($"✅ 已切换免费模型：{ModelCatalog.ShortDisplayName(m.Id)}（{m.ProviderId}）\n  /free-restore 恢复之前模型");
            }
        });
        return Task.CompletedTask;
    }
}
