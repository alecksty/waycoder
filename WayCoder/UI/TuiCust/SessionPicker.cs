using WayCoder.Terminal;
using WayCoder.UI.TuiControls;

namespace WayCoder.UI;

/// <summary>
/// 会话管理器对话框 —— 对标 Crush sessions.go。
/// 居中带边框对话框（非全屏），浏览/切换/重命名/删除历史会话。
///
/// 功能：
///   - 列出所有历史会话（ID + 时间 + 模型 + 当前标记 ✓）
///   - 实时搜索过滤
///   - Enter 切换会话 / R 重命名 / Del 删除
///   - 底部按钮行（打开/重命名/删除/关闭）
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiInput（搜索）+ TuiListView（会话行，手动反白）
///       + TuiButton 行，走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
/// 重命名复用 TuiDialog.InputLine、删除复用 TuiDialog.Confirm3（把旧 Normal/Renaming/Deleting
/// 三态状态机拆成「主窗 + 弹子对话框」）；顺带修复旧实现「重命名后列表陈旧」bug。
/// </summary>
public static class SessionPicker
{
    /// <summary>选择结果</summary>
    public record Result(string Action, string SessionId, string? NewName = null)
    {
        public static Result SwitchTo(string id) => new("switch", id);
        public static Result Rename(string id, string newName) => new("rename", id, newName);
        public static Result Delete(string id) => new("delete", id);
    }

    private const int MinW = 68, MaxW = 100;
    private const int ListH = 12; // 列表可见行数

    /// <summary>
    /// 显示会话管理对话框。返回操作结果，null = 取消。
    /// </summary>
    /// <param name="currentSessionId">当前会话 ID（用于标记 ✓）</param>
    public static Result? Show(string? currentSessionId = null)
    {
        Result? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(currentSessionId, screen, r => { result = r; evt.Set(); });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 60_000, win);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(string? currentSessionId, TuiScreen? screen, Action<Result?> onDone)
    {
        int winW = Math.Clamp(Tty.Cols - 2, MinW, MaxW);
        int listW = Math.Max(10, winW - 2);          // 内容区宽（去左右边框）
        int winH = ListH + 6;                         // 统计+搜索+列表+按钮+帮助 + 上下边框

        var win = new TuiWindow
        {
            Title = "会话管理",
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = TuiTheme.Current.WindowBg,
            Width = winW, Height = winH,
            MinWidth = MinW, MinHeight = winH,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
        };
        var g = TuiTheme.Current.GradCyanBlue;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // ── 状态 ──
        var sessions = SessionManager.ListSessions(limit: 50);
        var filtered = new List<SessionInfo>();
        var rowLabels = new List<TuiLabel>(); // 与 filtered 一一对应
        int sel = -1;

        // 统计行
        var stats = new TuiLabel { Height = 1, Fg = TuiColors.Blue };

        // 搜索行（标签 + 输入框，输入框聚焦）
        var search = new TuiInput
        {
            Height = 1,
            Flex = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };
        var searchRow = new TuiHBox { Spacing = 1 };
        searchRow.Add(new TuiLabel("搜索:") { Width = 6, Fg = TuiColors.BrightBlack });
        searchRow.Add(search);

        // 会话列表（单行格式化字符串，选中/当前手动着色）
        var list = new TuiListView { Height = ListH, IsAutoScrollToEnd = false };

        // 按钮行
        var openBtn = new TuiButton("打开 (Enter)") { Flex = 1 };
        var renameBtn = new TuiButton("重命名 (R)") { Flex = 1 };
        var delBtn = new TuiButton("删除 (Del)") { Flex = 1 };
        var closeBtn = new TuiButton("关闭 (Esc)") { Flex = 1 };
        var btnRow = new TuiHBox { Spacing = 2 };
        btnRow.Add(openBtn); btnRow.Add(renameBtn); btnRow.Add(delBtn); btnRow.Add(closeBtn);
        Grad(openBtn, TuiTheme.Current.BtnCyanBlue);
        Grad(renameBtn, TuiTheme.Current.BtnOrangeYellow);
        Grad(delBtn, TuiTheme.Current.BtnRedOrange);
        Grad(closeBtn, TuiTheme.Current.BtnOrangeYellow);

        // 帮助行
        var help = new TuiLabel { Height = 1, Fg = TuiColors.BrightBlack };

        var vbox = new TuiVBox { ChildHAlign = HAlign.Stretch };
        vbox.Add(stats);
        vbox.Add(searchRow);
        vbox.Add(list);
        vbox.Add(btnRow);
        vbox.Add(help);
        win.RootView = vbox;

        // ── 刷新 ──

        void RefreshHighlight()
        {
            for (int i = 0; i < rowLabels.Count; i++)
            {
                var s = filtered[i];
                bool isSel = i == sel;
                bool isCur = s.Id == currentSessionId;
                var lbl = rowLabels[i];
                lbl.Text = FormatSessionRow(s, isSel, isCur, listW);
                lbl.Fg = isSel ? TuiTheme.Current.ListSelFg : (isCur ? TuiColors.Blue : TuiColors.White);
                lbl.Bg = isSel ? TuiTheme.Current.ListSelBg : 0;
            }
            list.SelectedIndex = sel; // 驱动 TuiListView 自动滚动到选中项
            list.MarkDirty();
        }

        void Rebuild(bool resetSel)
        {
            filtered = string.IsNullOrEmpty(search.Text)
                ? sessions
                : sessions.Where(s =>
                    s.Id.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    s.Preview.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    s.Model.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToList();

            rowLabels.Clear();
            list.ClearItems();
            foreach (var s in filtered)
            {
                var lbl = new TuiLabel();
                rowLabels.Add(lbl);
                list.AddItem(lbl);
            }

            if (resetSel || sel < 0 || sel >= filtered.Count)
            {
                sel = -1;
                if (filtered.Count > 0)
                {
                    sel = 0;
                    if (currentSessionId != null)
                        for (int i = 0; i < filtered.Count; i++)
                            if (filtered[i].Id == currentSessionId) { sel = i; break; }
                }
            }

            stats.Text = $"{sessions.Count} 个历史会话" + (currentSessionId != null ? "  ← 当前标记 ✓" : "");
            help.Text = "↑↓ 导航  Enter 切换  R 重命名  Del 删除  Esc 关闭";
            RefreshHighlight();
            screen?.MarkDirty();
        }

        // 重命名/删除后重新载入会话列表，修复「列表陈旧」bug
        void Reload()
        {
            sessions = SessionManager.ListSessions(limit: 50);
            Rebuild(true);
        }

        // ── 动作 ──

        void Finish(Result? r)
        {
            onDone(r);
            win.OnClosed?.Invoke();
        }

        void SetSel(int v)
        {
            if (filtered.Count == 0) { sel = -1; return; }
            sel = Math.Clamp(v, 0, filtered.Count - 1);
            RefreshHighlight();
            screen?.MarkDirty();
        }
        void MoveSel(int d) => SetSel(sel + d);

        void SwitchTo()
        {
            if (sel >= 0 && sel < filtered.Count)
                Finish(Result.SwitchTo(filtered[sel].Id));
        }

        void Rename()
        {
            if (sel < 0 || sel >= filtered.Count) return;
            var s = filtered[sel];
            var sub = TuiDialog.InputLine("重命名会话", $"为「{s.Id}」输入新名称", s.Id, newName =>
            {
                if (!string.IsNullOrWhiteSpace(newName) && newName != s.Id)
                {
                    SessionManager.RenameSession(s.Id, newName);
                    Reload();
                }
            });
            screen?.ShowWindow(sub);
        }

        void Delete()
        {
            if (sel < 0 || sel >= filtered.Count) return;
            var s = filtered[sel];
            if (s.Id == currentSessionId) return; // 不能删除当前会话
            var sub = TuiDialog.Confirm3("删除会话", $"确认删除会话「{s.Id}」？", r =>
            {
                if (r == TuiDialog.DialogResult.Yes)
                    Finish(Result.Delete(s.Id));
            });
            screen?.ShowWindow(sub);
        }

        // 搜索输入：字母进过滤词（OnTextChanged 实时过滤），↑↓ 导航列表，Enter 切换；
        // R 在「筛选空」时触发重命名，否则作为普通字符进过滤词。
        search.OnTextChanged = () => Rebuild(true);
        search.KeyHook = key =>
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:   MoveSel(-1); return true;
                case ConsoleKey.DownArrow: MoveSel(+1); return true;
                case ConsoleKey.Home:      SetSel(0); return true;
                case ConsoleKey.End:       SetSel(Math.Max(0, filtered.Count - 1)); return true;
                case ConsoleKey.PageUp:    SetSel(Math.Max(0, sel - ListH)); return true;
                case ConsoleKey.PageDown:  SetSel(Math.Min(filtered.Count - 1, sel + ListH)); return true;
                case ConsoleKey.Enter:     SwitchTo(); return true;
                case ConsoleKey.R:
                    if (search.Text.Length == 0) { Rename(); return true; }
                    return false; // 有搜索词 → 交给输入框当过滤字符
                case ConsoleKey.Delete:    Delete(); return true;
            }
            return false; // 其余（字母/退格）交给输入框处理
        };

        openBtn.OnClick = _ => SwitchTo();
        renameBtn.OnClick = _ => Rename();
        delBtn.OnClick = _ => Delete();
        closeBtn.OnClick = _ => Finish(null);

        win.RegisterShortcut(ConsoleKey.Escape, () => Finish(null));

        Rebuild(true);
        return win;
    }

    private static void Grad(TuiButton b, (int start, int end) grad)
    {
        b.GradientBg = true;
        b.GradientBgStart = grad.start;
        b.GradientBgEnd = grad.end;
    }

    // ── 工具（纯逻辑，AOT 安全）──

    /// <summary>格式化一行会话（▶ 选中 / ✓ 当前 / 时间 / 模型），按显示宽度截断。</summary>
    private static string FormatSessionRow(SessionInfo s, bool isSel, bool isCur, int width)
    {
        var prefix = isSel ? "▶ " : "  ";
        var time = FormatRelativeTime(s.SavedAt);
        var check = isCur ? " ✓" : "";
        var line = $"{prefix}{s.Id}  {time}  [{s.Model}]{check}";
        return TruncateByVW(line, Math.Max(1, width - 1)); // 预留 1 列滚动条
    }

    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int vw = 0, chars = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w > maxVW) break;
            vw += w; chars += rune.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }

    /// <summary>格式化相对时间</summary>
    private static string FormatRelativeTime(string savedAt)
    {
        if (!DateTime.TryParse(savedAt, out var dt))
            return savedAt.Length > 14 ? savedAt[..14] : savedAt;

        var diff = DateTime.Now - dt;

        if (diff.TotalSeconds < 60) return "刚刚";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} 分钟前";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} 小时前";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} 天前";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} 周前";
        return dt.ToString("MM-dd HH:mm");
    }
}
