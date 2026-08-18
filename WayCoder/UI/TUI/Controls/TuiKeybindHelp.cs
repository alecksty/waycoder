using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

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
            ("Ctrl+S", "打开会话列表"),
            ("Ctrl+L", "全屏强制重绘"),
            ("Ctrl+Shift+F1", "主题选择"),
            ("Ctrl+Shift+F2", "主题轮换"),
            ("F5", "刷新/重绘界面"),
        ]),
        ("✏ 编辑", [
            ("Enter", "发送消息"),
            ("Ctrl+Enter", "输入区换行"),
            ("Shift+Enter", "插入空行"),
            ("Tab", "切换焦点（输入区 ↔ 列表）"),
            ("Ctrl+V", "粘贴（超长/多行时确认）"),
            ("Ctrl+Up / Down", "输入历史翻页"),
            ("Ctrl+Home / End", "聊天跳到顶/底部"),
        ]),
        ("🔄 模式", [
            ("Shift+Tab", "切换工作模式 (Build→Plan→Review→Auto)"),
            ("Ctrl+M / /model", "打开模型选择对话框"),
            ("Ctrl+G", "切换推理深度"),
            ("Ctrl+P", "命令面板（斜杠/补全）"),
        ]),
        ("🧭 导航", [
            ("↑↓", "聊天列表滚动"),
            ("PgUp / PgDn", "聊天列表翻页"),
            ("Home / End", "跳到列表顶部/底部"),
            ("←→", "输入区光标移动"),
            ("Ctrl+E", "打开编辑器"),
            ("Ctrl+T / O", "打开设置"),
            ("Ctrl+B", "切换侧栏"),
        ]),
        ("🖱 鼠标", [
            ("左键点击", "选中/确认"),
            ("滚轮", "列表/面板滚动"),
            ("拖拽标题栏", "移动浮窗"),
            ("拖拽边缘", "缩放浮窗"),
        ]),
        ("🛠 工具", [
            ("Ctrl+R", "搜索对话历史"),
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

        // 标记加载：结构/ids 来自 keybindhelp.tui（布局写标记），列表项 code-behind 填充
        var res = TuiMarkup.LoadResource("dialogs/keybindhelp.tui");
        var win = res.Window ?? throw new InvalidOperationException("keybindhelp.tui 根应为 Window");
        win.Width = winW; win.Height = winH;
        win.MinWidth = MinW; win.MinHeight = 8;
        win.WinBg = TuiTheme.Current.WindowBg;
        var g = TuiTheme.Current.GradCyanBlue;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 控件接线（结构在标记里，列表项数据/样式在此）
        var list = res.Find<TuiListView>("list")!;
        var hint = res.Find<TuiLabel>("hint")!;
        list.Height = listH;
        list.IsAutoScrollToEnd = false;
        list.Focused = true;
        hint.Text = "↑↓ 滚动  PgUp/PgDn 翻页  Home/End 首尾  Esc / Q 关闭";
        foreach (var (cat, bindings) in Groups)
        {
            list.AddItem(new TuiLabel("─ " + cat + " ─") { Height = 1, Fg = AnsiColors.Cyan });
            foreach (var (key, desc) in bindings)
                list.AddItem(new TuiLabel("  " + PadKey(key, KeyW) + "  " + desc) { Height = 1, Fg = AnsiColors.White });
        }

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
        int w = AnsiHelper.DisplayWidth(key);
        return w >= width ? key : key + new string(' ', width - w);
    }
}
