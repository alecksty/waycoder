using Microsoft.Maui.Controls.Shapes;

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

    private static bool isDark => Application.Current?.RequestedTheme == AppTheme.Dark;
    private static Color Res(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true ? (v as Color) ?? Colors.DimGray : Colors.DimGray;

    /// <summary>确认权限显示名（与 AgentService.GetStatus PermMode 一致）。</summary>
    private static string PermName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "Yolo",
        PermissionManager.Mode.SmartAuto => "SmartAuto",
        PermissionManager.Mode.Auto => "Auto",
        _ => "Ask",
    };

    private static string EconomyName(EconomyMode m) => m switch
    {
        EconomyMode.On => "开",
        EconomyMode.Auto => "自动",
        EconomyMode.Extreme => "极致",
        _ => "关",
    };

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

        // ── 模型横幅：点按 → 模型选择 ──
        var modelText = ConnectionConfig.FormatModelChannel(
            ConnectionConfig.CurrentMainChannel(), cfg.Provider, cfg.Model);
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
