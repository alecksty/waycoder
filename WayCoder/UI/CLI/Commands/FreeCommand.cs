using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /free — 免费模型切换（省钱）：读 free.json 缓存（--model free 扫描生成）。
///   /free              无参数 → 弹框选择
///   /free N（1~可用数） → 直接切换第 N 个免费模型
///   /free restore       → 直接还原收费模型（等同 /free-restore）
/// </summary>
public class FreeCommand : SlashCommand
{
    public override string Name => "/free";
    public override string[] Aliases => ["/免费"];
    public override string Description => "免费模型切换：无参弹框 / N 直接切换 / restore 还原收费";
    public override string? Usage => "/free [N|restore]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 读 free.json 缓存（--model free 扫描生成）——不再每次扫描
        var available = ModelCli.LoadFreeJson();
        if (available.Count == 0)
        {
            screen.AddSystemMsg("⚠️ 尚无免费可用列表。先跑 `--model free` 扫描一次生成 free.json，之后 /free 直接读缓存（不重复扫描）");
            return Task.CompletedTask;
        }

        var arg = args.Trim();

        // /free restore → 直接还原收费模型
        if (arg.Equals("restore", StringComparison.OrdinalIgnoreCase) || arg == "回")
        {
            screen.AddSystemMsg(ModelCli.RestorePrevious());
            return Task.CompletedTask;
        }

        // /free N → 直接切换第 N 个免费模型（1-based）
        if (int.TryParse(arg, out var n))
        {
            if (n < 1 || n > available.Count)
            {
                screen.AddSystemMsg($"⚠️ 序号越界：可用 {available.Count} 个免费模型（/free 1~{available.Count} 或 /free restore）");
                return Task.CompletedTask;
            }
            var c = available[n - 1];
            // 切换前记住当前模型（/free restore 可恢复；未记录才记，不覆盖已记住的）
            ModelCli.RememberCurrentModel();
            ConnectionConfig.ApplyModelChoice(c.ProviderId, c.ModelId, isLarge: true, out var msg, c.BaseUrl);
            screen.AddSystemMsg($"✅ 已切换免费模型 #{n}：{ModelCatalog.ShortDisplayName(c.ModelId)}（{c.ProviderId}）\n  /free restore 还原收费模型");
            return Task.CompletedTask;
        }

        // 无参数（或非法参数）→ 弹框选择
        if (arg.Length > 0)
            screen.AddSystemMsg($"⚠️ 未知参数「{args}」。用法：/free 弹框 · /free N 直接切换 · /free restore 还原收费");
        screen.ShowWindow(TuiDialog.Select($"💰 免费模型（{available.Count} 个 · 缓存）", available
            .Select(c => $"{ModelCatalog.ShortDisplayName(c.ModelId)}  （{c.ProviderId}）")
            .ToList(), idx =>
        {
            if (idx >= 0 && idx < available.Count)
            {
                var c = available[idx];
                // 切换前记住当前模型（/free restore 可恢复；未记录才记，不覆盖已记住的）
                ModelCli.RememberCurrentModel();
                ConnectionConfig.ApplyModelChoice(c.ProviderId, c.ModelId, isLarge: true, out var msg, c.BaseUrl);
                screen.AddSystemMsg($"✅ 已切换免费模型：{ModelCatalog.ShortDisplayName(c.ModelId)}（{c.ProviderId}）\n  /free restore 还原收费模型");
            }
        }));
        return Task.CompletedTask;
    }
}
