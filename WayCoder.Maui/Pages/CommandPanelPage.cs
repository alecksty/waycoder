using Microsoft.Maui.Controls.Shapes;
using WayCoder.Maui.Services;

// ⚠ 用**别名**而不是 `using WayCoder.UI.Shared.Terminal;`：那个命名空间里也有一个 `Color`
//   （终端配色那套），全量引入会和 `Microsoft.Maui.Graphics.Color` 撞成 CS0104
//   （本文件处处在用 `Color`）。
using ShellSize = WayCoder.UI.Shared.Terminal.ShellSize;
using ShellSizeMode = WayCoder.UI.Shared.Terminal.ShellSizeMode;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 侧栏命令页（独立页，替代原右侧抽屉浮层）——模型横幅 + 命令区 + 模式/权限/经济循环行 + 关于。
/// 聊天页保持全宽；模式等全局切换在本页即时刷新，返回聊天页时由 ChatPage.OnAppearing 刷顶栏。
/// </summary>
public sealed class CommandPanelPage : ContentPage
{
    private readonly ScrollView _scroll = new();
    private VerticalStackLayout _body = new() { Padding = new Thickness(16, 12), Spacing = 10 };

    public CommandPanelPage()
    {
        Title = "侧栏";
        BackgroundColor = Res(isDark ? "CardBgDark" : "CardBgLight");
        _scroll.Content = _body;
        Content = _scroll;
    }

    private static bool isDark => MauiUi.IsDark;
    private static Color Res(string key) => MauiUi.Res(key);

    /// <summary>确认权限显示名（经 MauiUi 收敛）。</summary>
    private static string PermName(PermissionManager.Mode m) => MauiUi.PermName(m);

    private static string EconomyName(EconomyMode m) => MauiUi.EconomyName(m);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Build();
    }

    private async Task NavAsync(string route)
    {
        try { await Shell.Current.GoToAsync(route); }
        catch (Exception ex) { ErrorLog.Error("Chat", $"侧栏导航 {route}", ex); }
    }

    private void Build()
    {
        _body = new VerticalStackLayout { Padding = new Thickness(16, 12), Spacing = 10 };
        _scroll.Content = _body;

        var main = Res(isDark ? "MainTextDark" : "MainTextLight");
        var muted = Res(isDark ? "MutedTextDark" : "MutedTextLight");
        var inputBg = Res(isDark ? "InputBgDark" : "InputBgLight");
        var primary = Res("Primary");
        var cfg = Config.Instance;

        // ── 模型横幅：点按 → 模型选择（文本经 MauiUi 收敛） ──
        var modelText = MauiUi.ModelText();
        _body.Add(Card(
            new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = "🧠 " + modelText, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = primary, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 },
                    new Label { Text = "当前模型 · 点按选择", FontSize = 11, TextColor = muted },
                },
            },
            new Color(primary.Red, primary.Green, primary.Blue, 0.10f),
            async () => await NavAsync("modelpicker")));

        // ── 命令区 ──
        _body.Add(Section("命令", muted));
        _body.Add(Row("🗂 供应商 / 模型", null, async () => await NavAsync("models"), main, muted, inputBg));
        _body.Add(Row("🔄 代码同步", null, async () => await NavAsync("gitsync"), main, muted, inputBg));
        _body.Add(Row("📌 任务管理", null, async () =>
        {
            var items = new List<string>();
            try { items = WayCoder.Tools.TodoTool.Items.Select(t => $"{t.Status} · {t.Title}").ToList(); } catch { }
            if (items.Count == 0) { await DisplayAlertAsync("任务管理", "暂无任务", "关闭"); return; }
            await DisplayActionSheetAsync($"任务列表（{items.Count}）", "关闭", null, items.Take(20).ToArray());
        }, main, muted, inputBg));

        // ── 模式区：值行点按循环（本页重建刷新当前值） ──
        _body.Add(Section("模式", muted));
        _body.Add(Row("⚙ 工作模式", WorkModeManager.Format(WorkModeManager.CurrentMode),
            () => { WorkModeManager.CycleNext(); PersistAndRefresh(); }, main, muted, inputBg));
        _body.Add(Row("🔐 确认权限", PermName(PermissionManager.CurrentMode),
            () => { PermissionManager.CycleMode(); PersistAndRefresh(); }, main, muted, inputBg));
        _body.Add(Row("💸 经济模式", EconomyName(cfg.EconomyMode),
            () => { cfg.CycleEconomy(); PersistAndRefresh(); }, main, muted, inputBg));

        // ── 命令行显示 ──
        // 尺寸是**三个正交组合**（都不固定 / 横向固定 / 都固定），不是"自适应 vs 固定"两档 ——
        // 横向固定那一档专给**假设 80 列的老程序**用（表格/边框/进度条按 80 列排版，
        // 按手机宽度折会整片错位），而行数没必要跟着钉死。
        // 改完由命令行页的 OnAppearing 应用 —— 这里只负责落盘 + 本页刷新。
        _body.Add(Section("命令行显示", muted));
        _body.Add(Row("🔍 输出字号", $"{MauiShellStore.Font:0.#} 号（可双指无极缩放）",
            () => { MauiShellStore.Font = MauiShellStore.NextFont(MauiShellStore.Font); PersistAndRefresh(); },
            main, muted, inputBg));
        _body.Add(Row("🖥 尺寸模式", ShellSize.ModeText(MauiShellStore.Mode),
            () =>
            {
                MauiShellStore.SetMode(MauiShellStore.Mode switch
                {
                    ShellSizeMode.Auto       => ShellSizeMode.WidthFixed,
                    ShellSizeMode.WidthFixed => ShellSizeMode.Fixed,
                    _                                       => ShellSizeMode.Auto,
                });
                PersistAndRefresh();
            },
            main, muted, inputBg));
        _body.Add(Row("📐 终端列数", MauiShellStore.ColsText(MauiShellStore.RawCols),
            () => { MauiShellStore.SetColumns(MauiShellStore.Next(MauiShellStore.ColsChoices, MauiShellStore.RawCols)); PersistAndRefresh(); },
            main, muted, inputBg));
        _body.Add(Row("📏 终端行数", MauiShellStore.RowsText(MauiShellStore.RawRows),
            () => { MauiShellStore.SetRows(MauiShellStore.Next(MauiShellStore.RowsChoices, MauiShellStore.RawRows)); PersistAndRefresh(); },
            main, muted, inputBg));
        _body.Add(Row("🗃 回滚缓存", MauiShellStore.ScrollbackText(MauiShellStore.Scrollback),
            () => { MauiShellStore.Scrollback = MauiShellStore.Next(MauiShellStore.ScrollbackChoices, MauiShellStore.Scrollback); PersistAndRefresh(); },
            main, muted, inputBg));

        // ── 其它 ──
        _body.Add(Section("其它", muted));
        _body.Add(Row("ℹ️ 关于", null, async () => await NavAsync("about"), main, muted, inputBg));
    }

    /// <summary>模式/权限/经济循环后：落盘 + 本页刷新（聊天页顶栏由 OnAppearing 刷新）。</summary>
    private void PersistAndRefresh()
    {
        try { WayCoder.Maui.Services.MauiModeStore.Save(WorkModeManager.CurrentMode, PermissionManager.CurrentMode, Config.Instance.EconomyMode); } catch { }
        Build();
    }

    private static Label Section(string text, Color muted)
        => new() { Text = text, FontSize = 11, TextColor = muted, Margin = new Thickness(2, 8, 2, 0) };

    private static Border Row(string title, string? sub, Action onTap, Color fg, Color subColor, Color bg)
    {
        var inner = new VerticalStackLayout { Spacing = 1 };
        inner.Add(new Label { Text = title, FontSize = 15, TextColor = fg, LineBreakMode = LineBreakMode.TailTruncation });
        if (!string.IsNullOrEmpty(sub))
            inner.Add(new Label { Text = sub, FontSize = 11, TextColor = subColor });
        return Card(inner, bg, onTap);
    }

    private static Border Card(View content, Color bg, Action onTap)
    {
        var card = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 0,
            BackgroundColor = bg,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = content,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { try { onTap(); } catch (Exception ex) { ErrorLog.Error("Chat", "侧栏", ex); } };
        card.GestureRecognizers.Add(tap);
        return card;
    }
}
