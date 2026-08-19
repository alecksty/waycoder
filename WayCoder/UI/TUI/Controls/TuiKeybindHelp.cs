using System.Text;
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
    // 这张表是快捷键的唯一事实源（面板 + 启动首条消息 + docs/使用手册.md 都据此维护）。
    // 每一条都对着实现核对过；平台差异见下方 Footnote，别再往说明里塞条件把列撑爆。
    private static readonly List<(string Category, List<(string Key, string Desc)> Bindings)> Groups =
    [
        ("🌐 全局", [
            ("F1 - F10", "切换工作区槽位"),
            ("Esc", "中断当前 Agent（运行中）"),
            ("Ctrl+Z", "优雅暂停（本批次后停）"),
            ("Ctrl+C", "退出（先保存会话）"),
            ("Ctrl+Q", "强制退出（立即，无确认）"),
            ("Ctrl+S", "打开会话列表"),
            ("Ctrl+L", "全屏强制重绘"),
            ("F5", "刷新/重绘界面"),
        ]),
        ("✏ 编辑", [
            ("Enter", "发送消息"),
            ("Ctrl+Enter", "输入区换行"),
            ("Shift+Enter", "输入区换行（同上）"),
            ("Tab", "路径补全 / 插 4 空格"),
            ("Ctrl+V", "粘贴（超长/多行时确认）"),
            ("↑↓", "输入历史（输入区非空时）"),
            ("←→", "输入区光标移动"),
            ("Home / End", "输入区行首 / 行尾"),
        ]),
        ("🔄 模式", [
            ("Shift+Tab / Ctrl+K", "切模式 Build→Plan→Review→Auto"),
            ("Ctrl+M / /model", "打开模型选择对话框"),
            ("Ctrl+G", "切换推理深度"),
            ("Ctrl+P", "输入建议条（非命令面板）"),
        ]),
        ("🧭 导航", [
            ("↑↓", "聊天滚动（输入区为空时）"),
            ("Ctrl+↑ / ↓", "聊天滚动 3 行"),
            ("PgUp / PgDn", "聊天列表翻页"),
            ("Ctrl+Home / End", "聊天跳到顶 / 底部"),
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

    /// <summary>
    /// 启动简版只显示这些键 —— 常用高频键，一眼记住；完整表在帮助面板（Ctrl+H / F1）。
    /// 用键名做筛选：与 Groups 保持单一事实源，加新键默认只进完整版，要进启动版再来这里登记。
    /// 注意「↑↓」在编辑（历史）与导航（滚动）各出现一次，筛的是键名，两条会一起进简版——正好都要。
    /// </summary>
    private static readonly HashSet<string> StartupKeys = new(StringComparer.Ordinal)
    {
        "F1 - F10", "Esc", "Ctrl+Z", "Ctrl+C", "Ctrl+S",      // 全局
        "Enter", "Ctrl+V", "↑↓",                               // 编辑
        "Shift+Tab / Ctrl+K", "Ctrl+M / /model",               // 模式
        "PgUp / PgDn", "Ctrl+E", "Ctrl+B",                     // 导航
        "Ctrl+H",                                              // 工具
    };

    /// <summary>
    /// 脚注：作用域说明 + 平台差异。
    ///
    /// 作用域判定的唯一事实源是 <see cref="TuiKeyScope"/>：系统键只有 Ctrl+C，其余全是窗口键，
    /// 只在所属窗口是栈顶时生效。所以本表列的键都隐含前提「焦点在聊天界面上」。
    ///
    /// 平台差异根因：输入走 <c>Console.ReadKey</c>，Unix 拿到的是字节流，
    /// 而 Ctrl+M≡0x0D≡Enter、Ctrl+H≡0x08≡Backspace —— 这两个键在 Unix 上被 Enter/Backspace 抢走，
    /// 无解（除非改键），只能给斜杠命令兜底。
    /// Shift+Tab 曾经在 Windows 上也是坏的（只认 Unix 的 ESC[Z），已修，两平台均可用。
    /// </summary>
    private static readonly string[] Footnote =
    [
        "ℹ 上表快捷键仅在聊天界面有焦点时有效。对话框打开时键盘归对话框，",
        "  唯一例外是 Ctrl+C（唯一的系统键，任何时候都能退出）。",
        "⚠ Unix 终端下 Ctrl+M / Ctrl+H 与 Enter / Backspace 同码，收不到：",
        "  Ctrl+M 打开模型框 → 改用 /model",
        "  Ctrl+H 打开本面板 → 改用 /help",
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
        list.AddItem(new TuiLabel("") { Height = 1 });
        foreach (var line in Footnote)
            list.AddItem(new TuiLabel(line) { Height = 1, Fg = AnsiColors.Yellow });

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

    /// <summary>把快捷键表转为纯文本（供启动欢迎 / 槽位首条对话输出）。</summary>
    /// <summary>
    /// 快捷键速查表（启动首条消息用）—— 简版，只含 <see cref="StartupKeys"/> 里的高频键。
    /// 「键 │ 说明」两列表格，键列按显示宽度补齐（CJK 安全），全部左对齐 ——
    /// 添加时须显式 centered:false，否则会被前一条居中的 system 消息带偏成参差居中。
    /// </summary>
    public static string GetHelpText()
    {
        // 分隔线宽度 = 缩进 + 键列 + 分隔符 + 说明列，固定值让各分类块对齐成一张表
        const int DescW = 34;
        var rule = new string('─', 2 + KeyW + 3 + DescW);

        // 键名保持默认亮色，其余（标题/分类/分隔线/竖线/说明）走灰白 —— 用 «» 中间格式，
        // 由各端渲染器转具体呈现（CLI/TUI→ANSI、Web→HTML），不在内容层硬写 ANSI
        var sb = new StringBuilder();
        sb.AppendLine("«grey»⌨ 快捷键速查（常用）«/»");
        sb.AppendLine("«grey»" + rule + "«/»");
        foreach (var (cat, bindings) in Groups)
        {
            var important = bindings.Where(b => StartupKeys.Contains(b.Key)).ToList();
            if (important.Count == 0) continue; // 整类不显示（如鼠标：点按拖拽人人都会，不占启动位）
            sb.AppendLine("«grey»" + cat + "«/»");
            foreach (var (key, desc) in important)
                sb.AppendLine("  " + PadKey(key, KeyW) + " «grey»│ " + desc + "«/»");
            sb.AppendLine("«grey»" + rule + "«/»");
        }
        sb.AppendLine("«grey»完整快捷键：按 Ctrl+H 或 F1 打开速查面板«/»");
        sb.AppendLine("«grey»⚠ Unix 下 Ctrl+M / Ctrl+H 与 Enter / Backspace 同码收不到 → 用 /model、/help«/»");
        return sb.ToString().TrimEnd();
    }

    /// <summary>键名按显示宽度补齐到固定列宽（CJK 键名正确对齐）。</summary>
    private static string PadKey(string key, int width)
    {
        int w = AnsiHelper.DisplayWidth(key);
        return w >= width ? key : key + new string(' ', width - w);
    }
}
