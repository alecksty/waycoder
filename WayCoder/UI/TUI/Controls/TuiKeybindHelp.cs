using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;
namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 快捷键速查面板 —— 居中带边框帮助窗口。
/// 按 Ctrl+H 或 F1 打开，Esc / Q 关闭，↑↓ 滚动浏览。
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiListView（分类头 + 键位行，聚焦滚动）
///       + TuiLabel（底部提示），走 UxHelper.RenderWait 阻塞 → 事件桥接，
///       不再自造 Console.ReadKey 循环，也不再用裸 ANSI 转义手绘全屏。
/// </summary>
public static class TuiKeybindHelp
{
    private static readonly List<(string Category, List<(string Key, string Desc)> Bindings)> Groups =
    [
        ("🌐 全局", [
            ("F1 - F10", "切换工作区槽位"),
            ("Ctrl+C", "中断当前 Agent 操作"),
            ("Ctrl+Q", "退出 WayCoder"),
            ("Ctrl+L", "清屏（保留输入区）"),
            ("Ctrl+S", "保存当前会话"),
        ]),
        ("✏ 编辑", [
            ("Enter", "发送消息"),
            ("Ctrl+Enter", "输入区换行"),
            ("Shift+Enter", "插入空行"),
            ("Tab", "切换焦点（输入区 ↔ 列表）"),
            ("Ctrl+V", "粘贴（超长/多行时确认）"),
            ("Ctrl+Up / Down", "输入历史翻页"),
        ]),
        ("🔄 模式", [
            ("Shift+Tab", "切换工作模式 (Build→Plan→Review→Auto)"),
            ("Ctrl+M", "打开模型选择对话框"),
            ("Ctrl+G", "打开文件选择对话框"),
            ("Ctrl+P", "打开命令面板"),
        ]),
        ("🧭 导航", [
            ("↑↓", "聊天列表滚动"),
            ("PgUp / PgDn", "聊天列表翻页"),
            ("Home / End", "跳到列表顶部/底部"),
            ("←→", "输入区光标移动"),
        ]),
        ("🖱 鼠标", [
            ("左键点击", "选中/确认"),
            ("滚轮", "列表/面板滚动"),
            ("拖拽标题栏", "移动浮窗"),
            ("拖拽边缘", "缩放浮窗"),
        ]),
        ("🛠 工具", [
            ("Ctrl+D", "显示 Diff 预览"),
            ("Ctrl+R", "切换推理深度"),
            ("F5", "刷新/重绘界面"),
            ("Ctrl+H", "打开本帮助面板"),
        ]),
    ];

    private const int MinW = 52, MaxW = 84;
    private const int KeyW = 20;  // 键名列显示宽度
    private const int ListH = 16; // 列表可见行数

    /// <summary>
    /// 显示快捷键速查面板。返回 true 表示面板正常关闭。
    /// </summary>
    public static bool Show()
    {
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(screen, () => evt.Set());
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 30_000, win);
        }
        catch { evt.Set(); }
        return true;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(TuiScreen? screen, Action onDone)
    {
        int winW = Math.Clamp(Tty.Cols - 2, MinW, MaxW);
        int winH = Math.Min(Tty.Rows - 2, ListH + 3);
        int listH = Math.Max(5, winH - 3);

        var win = new TuiWindow
        {
            Title = "⌨ 快捷键速查",
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = TuiTheme.Current.WindowBg,
            Width = winW, Height = winH,
            MinWidth = MinW, MinHeight = 8,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
        };
        var g = TuiTheme.Current.GradCyanBlue;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 列表（分类头 + 键位行，聚焦后 ↑↓ 滚动）
        var list = new TuiListView { Height = listH, IsAutoScrollToEnd = false, Focused = true };
        foreach (var (cat, bindings) in Groups)
        {
            list.AddItem(new TuiLabel("─ " + cat + " ─") { Height = 1, Fg = TuiColors.Cyan });
            foreach (var (key, desc) in bindings)
                list.AddItem(new TuiLabel("  " + PadKey(key, KeyW) + "  " + desc) { Height = 1, Fg = TuiColors.White });
        }

        // 底部提示行
        var hint = new TuiLabel
        {
            Height = 1,
            Fg = TuiColors.BrightBlack,
            Text = "↑↓ 滚动  PgUp/PgDn 翻页  Home/End 首尾  Esc / Q 关闭",
        };

        var vbox = new TuiVBox { ChildHAlign = HAlign.Stretch };
        vbox.Add(list);
        vbox.Add(hint);
        win.RootView = vbox;

        void Close()
        {
            onDone();
            win.OnClosed?.Invoke();
        }

        win.RegisterShortcut(ConsoleKey.Escape, Close);
        win.RegisterShortcut(ConsoleKey.Q, Close);
        win.RegisterShortcut(ConsoleKey.H, Close); // Ctrl+H 再按一次关闭

        return win;
    }

    // ── 工具 ──

    /// <summary>键名按显示宽度补齐到固定列宽（CJK 键名正确对齐）。</summary>
    private static string PadKey(string key, int width)
    {
        int w = TuiHelper.DisplayWidth(key);
        return w >= width ? key : key + new string(' ', width - w);
    }
}
