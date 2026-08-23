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
        // 读 free.json 缓存（--model free 扫描生成）——不再每次扫描
        var available = ModelCli.LoadFreeJson();
        if (available.Count == 0)
        {
            screen.AddSystemMsg("⚠️ 尚无免费可用列表。先跑 `--model free` 扫描一次生成 free.json，之后 /free 直接读缓存弹窗（不重复扫描）");
            return Task.CompletedTask;
        }
        // 弹菜单：显示短名 + provider，选中切换
        var names = available
            .Select(c => $"{ModelCatalog.ShortDisplayName(c.ModelId)}  （{c.ProviderId}）")
            .ToList();
        screen.ShowWindow(TuiDialog.Select($"💰 免费模型（{available.Count} 个 · 缓存）", names, idx =>
        {
            if (idx >= 0 && idx < available.Count)
            {
                var c = available[idx];
                // 切换前记住当前模型（/free-restore 可恢复；未记录才记，不覆盖已记住的）
                ModelCli.RememberCurrentModel();
                ConnectionConfig.ApplyModelChoice(c.ProviderId, c.ModelId, isLarge: true, out var msg, c.BaseUrl);
                screen.AddSystemMsg($"✅ 已切换免费模型：{ModelCatalog.ShortDisplayName(c.ModelId)}（{c.ProviderId}）\n  /free-restore 恢复之前模型");
            }
        }));
        return Task.CompletedTask;
    }
}
