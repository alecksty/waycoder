using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
namespace WayCoder.UI.Tui;

/// <summary>
/// 命令面板对话框 —— 对标 Crush command palette。
/// 居中带边框对话框（非全屏），模糊搜索所有命令、分组显示、实时过滤、Enter 执行。
///
/// 功能：
///   - 模糊搜索（标签/分类/描述/快捷键，忽略大小写）
///   - 分类头独立行 + 命令行（label + 描述 + 快捷键右对齐）
///   - ↑↓/Home/End/PgUp/PgDn 导航（跳过分类头）、Enter 执行、Esc 取消
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiInput（搜索）+ TuiListView（分类头/命令行），
/// 走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
/// </summary>
public static class CommandPalette
{
    public record Command(string Id, string Label, string Category, string Shortcut, string Desc, Action Action);

    private const int MinW = 52;
    private const int MaxW = 90;
    private const int ListH = 10;

    /// <summary>显示命令面板。返回 true 表示执行了命令，false = 取消。</summary>
    public static bool Show(List<Command> commands)
    {
        Command? toRun = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(commands, screen, c => { toRun = c; evt.Set(); });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 30_000, win);
        }
        catch { evt.Set(); }
        // 窗口关闭后再执行命令，避免命令内部再弹模态框造成 RenderWait 嵌套
        toRun?.Action?.Invoke();
        return toRun != null;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(List<Command> commands, TuiScreen? screen, Action<Command?> onDone)
    {
        int winW = Math.Clamp(Tty.Cols - 4, MinW, MaxW);
        int listW = Math.Max(10, winW - 2); // 内容区宽（去左右边框）
        int winH = ListH + 4;               // 上框+搜索+列表+帮助下框

        var win = new TuiWindow
        {
            Title = "🔍 命令面板",
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = TuiTheme.Current.WindowBg,
            Width = winW, Height = winH,
            MinWidth = MinW, MinHeight = 10,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
        };
        var g = TuiTheme.Current.GradCyanBlue;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 搜索框（聚焦，字母进过滤词）
        var search = new TuiInput
        {
            Height = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };

        // 命令列表（分类头 + 命令行，选中行手动反白）
        var list = new TuiListView
        {
            Height = ListH,
            IsAutoScrollToEnd = false,
        };

        // 帮助行（兼显示过滤计数）
        var help = new TuiLabel { Height = 1, Fg = TuiColors.BrightBlack };

        var vbox = new TuiVBox { ChildHAlign = HAlign.Stretch };
        vbox.Add(search);
        vbox.Add(list);
        vbox.Add(help);
        win.RootView = vbox;

        // ── 过滤 / 行状态 ──
        var filtered = new List<Command>();
        var rows = new List<(bool IsHeader, int CmdIdx, string Cat)>();
        var cmdRowLabels = new List<TuiLabel>(); // 与 filtered 命令一一对应
        int sel = -1;                            // 高亮行（rows 索引，恒指向命令行）

        void RefreshHighlight()
        {
            for (int i = 0; i < cmdRowLabels.Count; i++)
            {
                bool isSel = sel >= 0 && sel < rows.Count && !rows[sel].IsHeader && rows[sel].CmdIdx == i;
                var lbl = cmdRowLabels[i];
                lbl.Text = FormatCommandRow(filtered[i], isSel, listW);
                lbl.Fg = isSel ? TuiTheme.Current.ListSelFg : TuiColors.White;
                lbl.Bg = isSel ? TuiTheme.Current.ListSelBg : 0;
            }
            list.SelectedIndex = sel; // 驱动 TuiListView 自动滚动到选中项
            list.MarkDirty();
            screen?.MarkDirty(); // RenderWait 循环不走 Manager.OnKey，必须通知屏幕否则高亮冻结
        }

        void Rebuild()
        {
            filtered = string.IsNullOrEmpty(search.Text)
                ? commands
                : commands.Where(c =>
                    c.Label.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    c.Category.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    c.Desc.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    c.Shortcut.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToList();

            rows.Clear();
            cmdRowLabels.Clear();
            list.ClearItems();

            string? prevCat = null;
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].Category != prevCat)
                {
                    rows.Add((true, i, filtered[i].Category));
                    list.AddItem(new TuiLabel("─ " + filtered[i].Category + " ─") { Fg = TuiColors.Cyan });
                    prevCat = filtered[i].Category;
                }
                rows.Add((false, i, filtered[i].Category));
                var lbl = new TuiLabel();
                cmdRowLabels.Add(lbl);
                list.AddItem(lbl);
            }

            help.Text = $"↑↓ 导航  Enter 执行  Esc 取消  {filtered.Count}/{commands.Count}";

            sel = rows.Count > 0 ? StepToCommand(rows, -1, +1) : -1;
            RefreshHighlight();
            screen?.MarkDirty();
        }

        // ── 动作 ──

        void Finish(Command? c)
        {
            onDone(c);
            win.OnClosed?.Invoke();
        }

        void Execute()
        {
            if (sel >= 0 && sel < rows.Count && !rows[sel].IsHeader)
                Finish(filtered[rows[sel].CmdIdx]);
        }

        void JumpTo(int from, int dir)
        {
            if (rows.Count == 0) return;
            sel = StepToCommand(rows, from, dir);
            RefreshHighlight();
        }

        // 搜索输入：字母进过滤词（OnTextChanged 实时过滤），↑↓ 导航列表，Enter 执行
        search.OnTextChanged = Rebuild;
        search.KeyHook = key =>
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:   JumpTo(sel, -1); return true;
                case ConsoleKey.DownArrow: JumpTo(sel, +1); return true;
                case ConsoleKey.Home:      JumpTo(-1, +1); return true;
                case ConsoleKey.End:       JumpTo(rows.Count, -1); return true;
                case ConsoleKey.PageUp:    JumpTo(Math.Max(0, sel - ListH), -1); return true;
                case ConsoleKey.PageDown:  JumpTo(Math.Min(rows.Count - 1, sel + ListH), +1); return true;
                case ConsoleKey.Enter:     Execute(); return true;
            }
            return false; // 其余（字母/退格）交给输入框处理
        };

        win.RegisterShortcut(ConsoleKey.Escape, () => Finish(null));

        Rebuild();
        return win;
    }

    // ── 纯逻辑（AOT 安全，可自测）──

    /// <summary>移动到相邻命令行（跳过分类头）。dir=+1 向下、-1 向上；越界停在最近的命令行。</summary>
    internal static int StepToCommand(List<(bool IsHeader, int CmdIdx, string Cat)> rows, int from, int dir)
    {
        int p = from + dir;
        if (dir > 0)
        {
            if (p < 0) p = 0;
            while (p < rows.Count && rows[p].IsHeader) p++;
            if (p >= rows.Count)
            {
                p = rows.Count - 1;
                while (p >= 0 && rows[p].IsHeader) p--;
            }
        }
        else
        {
            if (p >= rows.Count) p = rows.Count - 1;
            while (p >= 0 && rows[p].IsHeader) p--;
            if (p < 0)
            {
                p = 0;
                while (p < rows.Count && rows[p].IsHeader) p++;
            }
        }
        return p;
    }

    /// <summary>格式化一行命令（label + 描述 + 快捷键右对齐），恰占 width 列。</summary>
    private static string FormatCommandRow(Command cmd, bool isSel, int width)
    {
        var prefix = isSel ? "▶ " : "  ";
        var shortcut = string.IsNullOrEmpty(cmd.Shortcut) ? "" : " " + cmd.Shortcut;
        int sw = TuiHelper.DisplayWidth(shortcut);
        int bodyMax = Math.Max(1, width - sw);
        var body = prefix + cmd.Label;
        int spare = bodyMax - TuiHelper.DisplayWidth(body);
        body = spare >= 4 && !string.IsNullOrEmpty(cmd.Desc)
            ? TuiHelper.TruncateByWidth(body + " " + cmd.Desc, bodyMax)
            : TuiHelper.TruncateByWidth(body, bodyMax);
        int bodyW = TuiHelper.DisplayWidth(body);
        return body + new string(' ', Math.Max(0, width - bodyW - sw)) + shortcut;
    }
}
