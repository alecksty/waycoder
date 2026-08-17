using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

using WayCoder.UI.Tui.Edit;

using WayCoder.UI.Shared;
namespace WayCoder.UI.Tui;

/// <summary>
/// Diff 预览 + 逐 Hunk 确认对话框。
/// 对标竞品的 diff 确认流程 —— 写文件前让用户看到差异。
///
/// 模式：
///   AcceptAll  — 接受全部变更
///   RejectAll  — 拒绝全部变更
///   ReviewHunks — 逐 hunk 审查（y=接受此 hunk / n=跳过此 hunk / q=取消剩余）
///
/// 实现：TuiWindow（模态大窗）+ 内嵌只读 DiffView 控件渲染 diff（统一/分屏），
///       Y/N/A/Q 映射 RegisterShortcut，箭头/滚轮滚动；不再自造 Console.ReadKey 循环。
/// </summary>
public static class DiffPreview
{
    /// <summary>
    /// 一个 diff hunk：上下文行 + 删除行 + 添加行。
    /// </summary>
    public class Hunk
    {
        public int OldStart, OldCount, NewStart, NewCount;
        public string Header = "";
        public List<HunkLine> Lines = [];
    }

    public class HunkLine
    {
        public char Kind;  // ' ' 上下文, '-' 删除, '+' 添加
        public string Text = "";
        public int OldLine, NewLine;
    }

    public enum Decision { AcceptAll, RejectAll, Partial }

    /// <summary>
    /// 显示 diff 预览并返回决策。
    /// oldContent = 原始文件内容, newContent = 修改后内容, filePath = 文件名。
    /// 返回：(决策, 被接受的 hunk 索引集合)
    /// </summary>
    public static (Decision Decision, HashSet<int>? AcceptedHunks) Show(
        string oldContent, string newContent, string filePath)
    {
        var hunks = BuildHunks(oldContent, newContent);

        // 无实际变更 → 直接放行
        if (hunks.Count == 0 || hunks.All(h => h.Lines.All(l => l.Kind == ' ')))
            return (Decision.AcceptAll, null);

        // Web 模式：经交互桥弹浏览器 diff 对话框（阻塞等待，无 SynchronizationContext 死锁风险）
        if (UxHelper.WebInteraction != null)
        {
            var result = UxHelper.WebInteraction.DiffConfirmAsync(filePath, hunks, 120_000)
                .GetAwaiter().GetResult();
            if (result == null)
                return (Decision.RejectAll, null); // 取消/超时 → 拒绝
            return (result.Decision, result.AcceptedHunks);
        }

        return TuiManager.Instance.ActiveScreen is ChatScreen
            ? ShowFullScreen(filePath, hunks)
            : ShowFallback(oldContent, newContent, filePath);
    }

    // ================================================================
    // 全屏交互模式（TuiWindow + DiffView 控件）
    // ================================================================

    private static (Decision, HashSet<int>?) ShowFullScreen(string filePath, List<Hunk> hunks)
    {
        Decision result = Decision.RejectAll;
        HashSet<int>? resultAccepted = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildDiffWindow(hunks, filePath, screen, (d, a) =>
            {
                result = d;
                resultAccepted = a;
                evt.Set();
            });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 120_000, win);
        }
        catch { evt.Set(); }
        return (result, resultAccepted);
    }

    // ── 窗口构建 ──

    internal static TuiWindow BuildDiffWindow(List<Hunk> hunks, string filePath,
        TuiScreen? screen, Action<Decision, HashSet<int>?> onDone)
    {
        int winW = Math.Max(40, Tty.Cols - 2);
        int winH = Math.Max(10, Tty.Rows - 2);
        int contentBg = TuiColors.BgBlack; // diff 视图固定深色底，保证语法高亮/红绿行对比

        var accepted = new HashSet<int>();
        var syntax = GetSyntaxForFile(filePath);
        bool isAllMode = false; // "review" | "all"

        // diff 视图（只读控件，占满除状态栏外的内容区）
        var diff = new DiffView(hunks, accepted, syntax)
        {
            Flex = 1,
            Height = 5,
            Bg = contentBg,
            Focused = true,
        };

        // 状态栏（白底黑字，对标旧实现）
        var status = new TuiLabel
        {
            Height = 1,
            Fg = TuiColors.Black,
            Bg = TuiColors.BgWhite,
        };

        var vbox = new TuiVBox { ChildHAlign = HAlign.Stretch };
        vbox.Add(diff);
        vbox.Add(status);

        var win = new TuiWindow
        {
            Title = $"Diff 预览: {filePath}  ({hunks.Count} hunks)",
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = contentBg,
            Width = winW, Height = winH,
            MinWidth = 40, MinHeight = 10,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
            RootView = vbox,
        };
        var g = TuiTheme.Current.GradOrangeYellow;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // ── 刷新 / 动作 ──

        void UpdateStatus()
        {
            status.Text = isAllMode
                ? " 全部接受?  [Y]是  [N]否  "
                : $" [{diff.CurrentHunk + 1}/{hunks.Count}]  [Y]接受 [N]跳过 [A]全接受 [Q]取消  ";
            screen?.MarkDirty();
        }

        void Finish(Decision d, HashSet<int>? a)
        {
            onDone(d, a);
            win.OnClosed?.Invoke();
        }

        void ToggleCurrent()
        {
            if (accepted.Contains(diff.CurrentHunk)) accepted.Remove(diff.CurrentHunk);
            else accepted.Add(diff.CurrentHunk);
            diff.MarkDirty();
            UpdateStatus();
        }

        void SkipCurrent()
        {
            if (accepted.Contains(diff.CurrentHunk)) accepted.Remove(diff.CurrentHunk);
            else diff.SetCurrentHunk(diff.CurrentHunk + 1);
            diff.MarkDirty();
            UpdateStatus();
        }

        void Quit()
        {
            if (accepted.Count == 0) Finish(Decision.RejectAll, null);
            else Finish(Decision.Partial, accepted);
        }

        void OnY()
        {
            if (isAllMode) { Finish(Decision.AcceptAll, null); return; }
            ToggleCurrent();
        }
        void OnN()
        {
            if (isAllMode) { isAllMode = false; UpdateStatus(); return; }
            SkipCurrent();
        }
        void OnA()
        {
            if (isAllMode) return;
            isAllMode = true;
            UpdateStatus();
        }

        diff.OnChanged = UpdateStatus;

        win.RegisterShortcut(ConsoleKey.Y, OnY);
        win.RegisterShortcut(ConsoleKey.N, OnN);
        win.RegisterShortcut(ConsoleKey.A, OnA);
        win.RegisterShortcut(ConsoleKey.Q, Quit);
        win.RegisterShortcut(ConsoleKey.Escape, Quit);
        win.RegisterShortcut(ConsoleKey.Enter, () =>
        {
            if (!isAllMode && accepted.Count > 0) Finish(Decision.Partial, accepted);
        });

        UpdateStatus();
        return win;
    }

    /// <summary>
    /// 只读 diff 渲染控件 —— 统一/分屏两种模式，↑↓ 切 hunk、←→/PgUp/PgDn 滚动。
    /// 渲染走 AnsiTty（无 Console.* / 无裸转义），由 TuiControl.OnRender 驱动。
    /// </summary>
    private sealed class DiffView : TuiControl
    {
        private readonly List<Hunk> _hunks;
        private readonly HashSet<int> _accepted;
        private readonly Syntax? _syntax;
        private int _currentHunk;
        private int _scrollOffset;
        private int _lastWidth = -1;
        private bool _splitMode;
        private readonly List<(int hunkIdx, HunkLine line)> _lines = [];
        private List<SplitRow>? _splitRows;

        public int CurrentHunk => _currentHunk;
        public HashSet<int> Accepted => _accepted;
        public Action? OnChanged;

        public DiffView(List<Hunk> hunks, HashSet<int> accepted, Syntax? syntax)
        {
            _hunks = hunks;
            _accepted = accepted;
            _syntax = syntax;
            Focused = true;
        }

        public void SetCurrentHunk(int h)
            => _currentHunk = Math.Clamp(h, 0, Math.Max(0, _hunks.Count - 1));

        private int TotalLines => _splitMode ? _splitRows!.Count : _lines.Count;

        /// <summary>按当前宽度重建视觉行（统一模式 _lines / 分屏模式 _splitRows）。</summary>
        private void Rebuild(int width)
        {
            _lastWidth = width;
            _splitMode = width >= 120;
            _lines.Clear();
            for (int hi = 0; hi < _hunks.Count; hi++)
            {
                var h = _hunks[hi];
                if (_lines.Count > 0) _lines.Add((-1, new HunkLine { Kind = ' ', Text = "" }));
                _lines.Add((-2, new HunkLine { Kind = '@', Text = h.Header }));
                foreach (var l in h.Lines) _lines.Add((hi, l));
            }
            _splitRows = _splitMode ? BuildSplitRows(_hunks, width) : null;
        }

        /// <summary>自动滚动，保证当前 hunk 的第一行可见。</summary>
        private void EnsureVisible(int contentH)
        {
            int total = TotalLines;
            int maxScroll = Math.Max(0, total - contentH);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

            int currentLine = 0;
            if (_splitMode)
            {
                for (int i = 0; i < _splitRows!.Count; i++)
                    if (_splitRows[i].HunkIdx == _currentHunk && !_splitRows[i].IsHeader)
                    { currentLine = i; break; }
            }
            else
            {
                for (int i = 0; i < _lines.Count; i++)
                    if (_lines[i].hunkIdx == _currentHunk && _lines[i].line.Kind != '@')
                    { currentLine = i; break; }
            }

            if (currentLine < _scrollOffset) _scrollOffset = currentLine;
            if (currentLine >= _scrollOffset + contentH) _scrollOffset = currentLine - contentH + 1;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, total - contentH));
        }

        public override bool OnKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    if (_currentHunk > 0) { SetCurrentHunk(_currentHunk - 1); Changed(); }
                    return true;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    if (_currentHunk < _hunks.Count - 1) { SetCurrentHunk(_currentHunk + 1); Changed(); }
                    return true;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.H:
                    if (_scrollOffset > 0) { _scrollOffset = Math.Max(0, _scrollOffset - 3); Changed(); }
                    return true;
                case ConsoleKey.RightArrow:
                case ConsoleKey.L:
                    _scrollOffset = Math.Min(Math.Max(0, TotalLines - Height), _scrollOffset + 3);
                    Changed();
                    return true;
                case ConsoleKey.PageUp:
                    _scrollOffset = Math.Max(0, _scrollOffset - Height);
                    Changed();
                    return true;
                case ConsoleKey.PageDown:
                    _scrollOffset = Math.Min(Math.Max(0, TotalLines - Height), _scrollOffset + Height);
                    Changed();
                    return true;
            }
            return false;
        }

        /// <summary>鼠标滚轮滚动 diff 内容（每格 3 行），防止超出屏幕。</summary>
        public override bool HandleMouse(InputEvent ev)
        {
            if (ev.Type != InputType.Mouse) return false;

            int absX = GetAbsoluteX();
            int absY = GetAbsoluteY();
            if (ev.MouseX < absX || ev.MouseX >= absX + Width ||
                ev.MouseY < absY || ev.MouseY >= absY + Height)
                return false;

            if (ev.MouseScrollUp) { ScrollBy(-3); return true; }
            if (ev.MouseScrollDown) { ScrollBy(3); return true; }
            return false;
        }

        /// <summary>按行数滚动，钳制到有效范围。</summary>
        private void ScrollBy(int delta)
        {
            int maxScroll = Math.Max(0, TotalLines - Height);
            int next = Math.Clamp(_scrollOffset + delta, 0, maxScroll);
            if (next == _scrollOffset) return;
            _scrollOffset = next;
            Changed();
        }

        private void Changed()
        {
            MarkDirty();
            OnChanged?.Invoke();
        }

        protected override void OnRender(StringBuilder sb, int absX, int absY)
        {
            int width = Width;
            if (width != _lastWidth) Rebuild(width);
            EnsureVisible(Height);

            int total = TotalLines;
            bool showBar = total > Height && width > 2;
            int contentW = showBar ? width - 1 : width;

            for (int i = 0; i < Height; i++)
            {
                int li = _scrollOffset + i;
                if (li >= total) break;
                if (_splitMode)
                    RenderSplitRow(sb, _splitRows![li], contentW, _currentHunk, _accepted, _syntax, absY + i, absX);
                else
                    RenderUnifiedLine(sb, _lines[li], contentW, _currentHunk, _accepted, _syntax, absY + i, absX);
            }

            // 右侧滚动条（▉ 滑块 + │ 轨道），提示内容可滚动、定位当前位置
            if (showBar)
            {
                int barH = Math.Max(1, Height * Height / total);
                int barPos = Height * _scrollOffset / Math.Max(1, total - Height);
                barPos = Math.Clamp(barPos, 0, Height - barH);
                for (int i = 0; i < Height; i++)
                {
                    var ch = (i >= barPos && i < barPos + barH) ? "█" : "│";
                    sb.Append(AnsiTty.CursorPos0(absY + i, absX + contentW));
                    sb.Append(AnsiTty.SgrDim).Append(ch).Append(AnsiTty.SgrReset);
                }
            }
        }
    }

    // ================================================================
    // 非全屏回退模式
    // ================================================================

    private static (Decision, HashSet<int>?) ShowFallback(
        string oldContent, string newContent, string filePath)
    {
        var diff = GenerateUnifiedDiff(oldContent, newContent, filePath);
        Tty.WriteLine(AnsiText.Accent($"\n=== Diff 预览: {filePath} ==="));
        Tty.WriteLine(diff);
        Tty.WriteLine();

        var choice = UxHelper.Select("如何处理此变更？",
            ["全部接受 (Y)", "全部拒绝 (N)", "逐项审查 (R)"]);
        return choice switch
        {
            "全部接受 (Y)" => (Decision.AcceptAll, null),
            "全部拒绝 (N)" => (Decision.RejectAll, null),
            _ => (Decision.RejectAll, null), // TUI 回退不支持逐项
        };
    }

    // ================================================================
    // Hunk 构建
    // ================================================================

    /// <summary>
    /// 将旧/新内容拆分为 hunk 列表。
    /// 使用简单的 LCS 行级 diff。
    /// </summary>
    public static List<Hunk> BuildHunks(string oldContent, string newContent)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var newLines = newContent.Replace("\r\n", "\n").Split('\n');

        // 简单逐行比较，分组为 hunks
        var edits = ComputeLineEdits(oldLines, newLines);
        var hunks = GroupIntoHunks(edits, oldLines, newLines, contextLines: 3);
        return hunks;
    }

    private static List<(int OldIdx, int NewIdx, char Kind)> ComputeLineEdits(
        string[] oldL, string[] newL)
    {
        var result = new List<(int, int, char)>();

        // 使用简单的 Myers 式逐行比较
        int oi = 0, ni = 0;
        while (oi < oldL.Length || ni < newL.Length)
        {
            if (oi < oldL.Length && ni < newL.Length && oldL[oi] == newL[ni])
            {
                result.Add((oi, ni, ' '));
                oi++; ni++;
            }
            else
            {
                // 查找同步点
                int syncOld = -1, syncNew = -1;
                for (int so = oi; so < Math.Min(oi + 10, oldL.Length) && syncOld < 0; so++)
                {
                    for (int sn = ni; sn < Math.Min(ni + 10, newL.Length); sn++)
                    {
                        if (oldL[so] == newL[sn])
                        { syncOld = so; syncNew = sn; break; }
                    }
                }

                if (syncOld >= 0)
                {
                    // 删除行到同步点
                    while (oi < syncOld) { result.Add((oi, -1, '-')); oi++; }
                    // 添加行到同步点
                    while (ni < syncNew) { result.Add((-1, ni, '+')); ni++; }
                }
                else
                {
                    // 无同步点：剩余全部不同
                    if (oi < oldL.Length) { result.Add((oi, -1, '-')); oi++; }
                    else if (ni < newL.Length) { result.Add((-1, ni, '+')); ni++; }
                }
            }
        }
        return result;
    }

    private static List<Hunk> GroupIntoHunks(
        List<(int OldIdx, int NewIdx, char Kind)> edits,
        string[] oldL, string[] newL, int contextLines)
    {
        // 1. 收集变更块：连续非上下文行（Kind != ' '）的 [start,end) 区间
        var blocks = new List<(int Start, int End)>();
        int bi = 0;
        while (bi < edits.Count)
        {
            while (bi < edits.Count && edits[bi].Kind == ' ') bi++;
            if (bi >= edits.Count) break;
            int s = bi;
            while (bi < edits.Count && edits[bi].Kind != ' ') bi++;
            blocks.Add((s, bi));
        }

        // 2. 每个块前后扩展 contextLines 上下文
        var ranges = new List<(int S, int E)>();
        foreach (var (bs, be) in blocks)
            ranges.Add((Math.Max(0, bs - contextLines), Math.Min(edits.Count, be + contextLines)));

        // 3. 合并重叠区间（变更相距 < 2*contextLines 时并成同一 hunk，避免重复行/重叠 hunk）
        var merged = new List<(int S, int E)>();
        foreach (var (s, e) in ranges)
        {
            if (merged.Count > 0 && s <= merged[^1].E)
                merged[^1] = (merged[^1].S, Math.Max(merged[^1].E, e));
            else
                merged.Add((s, e));
        }

        // 4. 由合并后的区间构建 hunk
        var hunks = new List<Hunk>();
        foreach (var (hs, he) in merged)
        {
            var hunk = new Hunk();
            int oldCount = 0, newCount = 0;

            for (int j = hs; j < he; j++)
            {
                var (oi, ni, kind) = edits[j];
                var text = kind switch
                {
                    '-' => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                    '+' => (ni >= 0 && ni < newL.Length) ? newL[ni] : "",
                    _ => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                };
                int oldLineNo = oi >= 0 ? oi + 1 : 0;
                int newLineNo = ni >= 0 ? ni + 1 : 0;
                hunk.Lines.Add(new HunkLine { Kind = kind, Text = text, OldLine = oldLineNo, NewLine = newLineNo });
                if (kind == '-' || kind == ' ') oldCount++;
                if (kind == '+' || kind == ' ') newCount++;
            }

            hunk.OldStart = hunk.Lines.FirstOrDefault(l => l.OldLine > 0)?.OldLine ?? 1;
            hunk.NewStart = hunk.Lines.FirstOrDefault(l => l.NewLine > 0)?.NewLine ?? 1;
            hunk.OldCount = oldCount;
            hunk.NewCount = newCount;
            hunk.Header = $"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@";
            hunks.Add(hunk);
        }

        return hunks;
    }

    // ================================================================
    // 统一 Diff 生成（回退 + 调试）
    // ================================================================

    public static string GenerateUnifiedDiff(string oldContent, string newContent, string filePath)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var newLines = newContent.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();

        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");

        var hunks = BuildHunks(oldContent, newContent);
        foreach (var h in hunks)
        {
            sb.AppendLine(h.Header);
            foreach (var l in h.Lines)
                sb.AppendLine($"{l.Kind}{l.Text}");
        }

        var result = sb.ToString();
        if (result.Length > 3000)
            result = result[..2500] + "\n...（diff 已截断）\n";
        return result;
    }

    /// <summary>
    /// 将接受的 hunks 应用到旧内容，生成最终内容。
    /// 拒绝的 hunk 保留原行；接受的 hunk 应用删除/添加。
    /// 相邻 hunk 共享的上下文行只输出一次。
    /// </summary>
    public static string ApplyAccepted(string oldContent, List<Hunk> hunks, HashSet<int> accepted)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        int oldIdx = 0;

        foreach (var (h, hi) in hunks.Select((h, i) => (h, i)))
        {
            bool accept = accepted.Contains(hi);
            int hunkStart = h.OldStart - 1;

            while (oldIdx < hunkStart && oldIdx < oldLines.Length)
                result.Add(oldLines[oldIdx++]);

            foreach (var l in h.Lines)
            {
                if (l.Kind == '-' || l.Kind == ' ')
                {
                    int lineIdx = l.OldLine - 1;
                    if (oldIdx > lineIdx) continue;
                    if (l.Kind == '-' && accept)
                    {
                        oldIdx++;
                    }
                    else
                    {
                        if (oldIdx < oldLines.Length) result.Add(oldLines[oldIdx]);
                        oldIdx++;
                    }
                }
                else if (accept)
                {
                    result.Add(l.Text);
                }
            }
        }

        while (oldIdx < oldLines.Length)
            result.Add(oldLines[oldIdx++]);

        return string.Join('\n', result);
    }

    // ================================================================
    // 分屏 Diff 渲染（终端宽度 >= 120 列时自动启用）
    // ================================================================

    /// <summary>
    /// 分屏模式的一行数据：左右各一段文本。
    /// </summary>
    private class SplitRow
    {
        public int HunkIdx;
        public string LeftText = "";   // 旧文件内容（删除行或上下文）
        public int LeftLineNo;
        public char LeftKind;          // '-' 或 ' '
        public string RightText = "";  // 新文件内容（添加行或上下文）
        public int RightLineNo;
        public char RightKind;         // '+' 或 ' '
        public bool IsHeader;          // hunk 头部
        public string HeaderText = "";
    }

    /// <summary>
    /// 将 hunk 列表转换为分屏行对（左旧右新）。
    /// 删除行显示在左侧、添加行显示在右侧、上下文行左右同时显示。
    /// </summary>
    private static List<SplitRow> BuildSplitRows(List<Hunk> hunks, int terminalWidth)
    {
        var rows = new List<SplitRow>();
        int panelWidth = (terminalWidth - 3) / 2; // 3 = " │ " 分隔符
        int textWidth = Math.Max(20, panelWidth - 6); // 6 = 行号(4) + 标记(1) + 空格(1)

        foreach (var (h, hi) in hunks.Select((h, i) => (h, i)))
        {
            // hunk 头
            rows.Add(new SplitRow { IsHeader = true, HeaderText = h.Header });

            // 将 hunk 内的行配对
            var adds = h.Lines.Where(l => l.Kind == '+').ToList();

            // 将删除行和添加行按顺序配对
            int ai = 0;
            var consumedAdds = new HashSet<int>();
            foreach (var line in h.Lines)
            {
                if (line.Kind == ' ')
                {
                    // 上下文行：左右同时显示
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = TruncateByVW(line.Text, textWidth),
                        LeftLineNo = line.OldLine, LeftKind = ' ',
                        RightText = TruncateByVW(line.Text, textWidth),
                        RightLineNo = line.NewLine, RightKind = ' ',
                    });
                }
                else if (line.Kind == '-')
                {
                    // 删除行：左边显示，尝试配对一个添加行到右边
                    string? rightText = null;
                    int rightLine = 0;
                    while (ai < adds.Count && consumedAdds.Contains(ai))
                        ai++;
                    if (ai < adds.Count)
                    {
                        rightText = TruncateByVW(adds[ai].Text, textWidth);
                        rightLine = adds[ai].NewLine;
                        consumedAdds.Add(ai);
                        ai++;
                    }
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = TruncateByVW(line.Text, textWidth),
                        LeftLineNo = line.OldLine, LeftKind = '-',
                        RightText = rightText ?? "",
                        RightLineNo = rightLine, RightKind = rightText != null ? '+' : ' ',
                    });
                }
            }
            // 处理未配对的添加行（右边显示，左留空）
            for (int i = 0; i < adds.Count; i++)
            {
                if (!consumedAdds.Contains(i))
                {
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = "", LeftLineNo = 0, LeftKind = ' ',
                        RightText = TruncateByVW(adds[i].Text, textWidth),
                        RightLineNo = adds[i].NewLine, RightKind = '+',
                    });
                }
            }
        }
        return rows;
    }

    // ================================================================
    // 行渲染（统一 / 分屏）—— 走 AnsiTty，无 Console.* / 无裸转义
    // ================================================================

    /// <summary>渲染统一模式的一行（有色行铺满整行背景）。</summary>
    private static void RenderUnifiedLine(StringBuilder sb, (int hunkIdx, HunkLine line) entry,
        int tw, int currentHunk, HashSet<int> accepted, Syntax? syntax, int absY, int absX)
    {
        var (hi, line) = entry;
        sb.Append(AnsiTty.CursorPos0(absY, absX));

        if (hi == -1)
        {
            // hunk 分隔线
            sb.Append(AnsiTty.SgrDim);
            sb.Append(new string('─', Math.Min(tw, 60)));
            sb.Append(AnsiTty.SgrReset);
            return;
        }
        if (hi == -2)
        {
            // hunk 头
            var hdr = TruncateByVW(line.Text, tw - 1);
            sb.Append(AnsiTty.Fg(36)).Append(hdr).Append(AnsiTty.SgrReset);
            return;
        }

        bool isCurrentHunk = hi == currentHunk;
        bool isAccepted = accepted.Contains(hi);

        if (line.Kind == '-')
        {
            var prefix = $"{Padding(line.OldLine),4} -";
            int fg = isCurrentHunk ? 30 : 37;
            int bg = 41;
            var maxTextW = tw - 7;
            FillBg(sb, absY, absX, tw, bg);
            sb.Append(isCurrentHunk ? AnsiTty.Sgr(fg, bg, 1) : AnsiTty.FgBg(fg, bg));
            sb.Append(prefix).Append(' ');
            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
            sb.Append(AnsiTty.SgrReset);
        }
        else if (line.Kind == '+')
        {
            var prefix = "     +";
            int fg = isCurrentHunk ? 30 : 37;
            int bg = 42;
            var maxTextW = tw - 7;
            FillBg(sb, absY, absX, tw, bg);
            sb.Append(isCurrentHunk ? AnsiTty.Sgr(fg, bg, 1) : AnsiTty.FgBg(fg, bg));
            sb.Append(prefix).Append(' ');
            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
            sb.Append(AnsiTty.SgrReset);
        }
        else
        {
            var prefix = $"{Padding(line.OldLine),4}  ";
            var maxTextW = tw - 7;
            if (isCurrentHunk)
            {
                FillBg(sb, absY, absX, tw, 46);
                sb.Append(AnsiTty.FgBg(30, 46));
                sb.Append(prefix).Append(' ');
                AppendHighlightedCode(sb, line.Text, syntax, 30, 46, false, maxTextW);
                sb.Append(AnsiTty.SgrReset);
            }
            else if (isAccepted)
            {
                sb.Append(AnsiTty.SgrDim);
                sb.Append(prefix).Append(' ');
                // 已接受的上下文行：不语法高亮，直接 dim
                var t = TruncateByVW(line.Text, maxTextW);
                sb.Append(t);
                sb.Append(AnsiTty.SgrReset);
            }
            else
            {
                // 普通上下文行：语法高亮，无背景色
                sb.Append(prefix).Append(' ');
                AppendHighlightedCode(sb, line.Text, syntax, 37, 0, false, maxTextW);
            }
        }
    }

    /// <summary>渲染分屏模式的一行。格式：lnno - 旧内容... │ lnno + 新内容...</summary>
    private static void RenderSplitRow(StringBuilder sb, SplitRow row,
        int tw, int currentHunk, HashSet<int> accepted, Syntax? syntax, int absY, int absX)
    {
        sb.Append(AnsiTty.CursorPos0(absY, absX));
        int panelWidth = (tw - 3) / 2;

        if (row.IsHeader)
        {
            var hdr = TruncateByVW(row.HeaderText, tw - 1);
            sb.Append(AnsiTty.Fg(36)).Append(hdr).Append(AnsiTty.SgrReset);
            return;
        }

        bool isCurrentHunk = row.HunkIdx == currentHunk;
        bool isAccepted = accepted.Contains(row.HunkIdx);

        // ── 左面板 ──
        int leftFg, leftBg;
        bool leftBold = isCurrentHunk && row.LeftKind == '-';
        if (row.LeftKind == '-')
            { leftFg = isCurrentHunk ? 30 : 37; leftBg = 41; }
        else if (isCurrentHunk)
            { leftFg = 30; leftBg = 46; leftBold = false; }
        else
            { leftFg = 37; leftBg = 0; }

        var leftPrefix = row.LeftKind == '-'
            ? $"{Padding(row.LeftLineNo),4} -"
            : row.LeftText.Length > 0 ? $"{Padding(row.LeftLineNo),4}  " : "      ";

        if (isAccepted && !isCurrentHunk && row.LeftKind != '-')
        {
            // 已接受的上下文：dim 渲染，不高亮
            sb.Append(AnsiTty.SgrDim);
            var lc = leftPrefix + " " + row.LeftText;
            var lp = Math.Max(0, panelWidth - VW(lc));
            sb.Append(lc).Append(new string(' ', lp)).Append(AnsiTty.SgrReset);
        }
        else
        {
            sb.Append(leftBold ? AnsiTty.Sgr(leftFg, leftBg, 1) :
                     leftBg > 0 ? AnsiTty.FgBg(leftFg, leftBg) : "");
            sb.Append(leftPrefix).Append(' ');
            int maxCodeW = panelWidth - VW(leftPrefix) - 1;
            var leftCode = TruncateByVW(row.LeftText, maxCodeW);
            int codeVW = VW(leftCode);
            AppendHighlightedCode(sb, leftCode, syntax, leftFg, leftBg, leftBold, int.MaxValue);
            int leftPad = Math.Max(0, panelWidth - VW(leftPrefix) - 1 - codeVW);
            sb.Append(new string(' ', leftPad));
            sb.Append(AnsiTty.SgrReset);
        }

        // 分隔符
        sb.Append(AnsiTty.SgrDim).Append(" │ ").Append(AnsiTty.SgrReset);

        // ── 右面板 ──
        int rightFg, rightBg;
        bool rightBold = isCurrentHunk && row.RightKind == '+';
        if (row.RightKind == '+')
            { rightFg = isCurrentHunk ? 30 : 37; rightBg = 42; }
        else if (isCurrentHunk)
            { rightFg = 30; rightBg = 46; rightBold = false; }
        else
            { rightFg = 37; rightBg = 0; }

        var rightPrefix = row.RightKind == '+'
            ? $"{Padding(row.RightLineNo),4} +"
            : row.RightText.Length > 0 ? $"{Padding(row.RightLineNo),4}  " : "      ";

        if (isAccepted && !isCurrentHunk && row.RightKind != '+')
        {
            sb.Append(AnsiTty.SgrDim);
            sb.Append(rightPrefix).Append(' ').Append(row.RightText);
            sb.Append(AnsiTty.SgrReset);
        }
        else
        {
            sb.Append(rightBold ? AnsiTty.Sgr(rightFg, rightBg, 1) :
                     rightBg > 0 ? AnsiTty.FgBg(rightFg, rightBg) : "");
            sb.Append(rightPrefix).Append(' ');
            int maxCodeW = panelWidth - VW(rightPrefix) - 1;
            var rightCode = TruncateByVW(row.RightText, maxCodeW);
            AppendHighlightedCode(sb, rightCode, syntax, rightFg, rightBg, rightBold, int.MaxValue);
            sb.Append(AnsiTty.SgrReset);
        }
    }

    /// <summary>填满整行背景（光标已在行首定位），再回到行首供后续写内容。</summary>
    private static void FillBg(StringBuilder sb, int absY, int absX, int width, int bg)
    {
        sb.Append(AnsiTty.BgCode(bg));
        sb.Append(new string(' ', width));
        sb.Append(AnsiTty.SgrResetBg);
        sb.Append(AnsiTty.CursorPos0(absY, absX));
    }

    // ================================================================
    // 工具方法
    // ================================================================

    // ================================================================
    // 语法高亮
    // ================================================================

    /// <summary>根据文件路径获取语法定义（缓存友好，一次 diff 仅调用一次）</summary>
    private static Syntax? GetSyntaxForFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        try { return Syntax.ForFile(filePath); }
        catch { return null; }
    }

    /// <summary>
    /// 在 diff 背景色上渲染语法高亮代码。
    /// 每个 token 使用语法颜色作为前景色，diff 背景色作为背景色。
    /// </summary>
    private static void AppendHighlightedCode(StringBuilder sb, string code,
        Syntax? syntax, int baseFg, int bgColor, bool bold, int maxWidth)
    {
        if (syntax == null || string.IsNullOrEmpty(code))
        {
            var t = TruncateByVW(code, maxWidth);
            if (bold) sb.Append(AnsiTty.Sgr(baseFg, bgColor, 1));
            else if (bgColor > 0) sb.Append(AnsiTty.FgBg(baseFg, bgColor));
            sb.Append(t);
            return;
        }

        var tokens = syntax.Tokenize(code);
        int remaining = maxWidth;
        foreach (var (text, tokFg) in tokens)
        {
            if (remaining <= 0) break;
            var t = TruncateByVW(text, remaining);
            if (t.Length == 0) continue;
            remaining -= VW(t);

            int fg = tokFg > 0 ? tokFg : baseFg;
            if (bold)
                sb.Append(AnsiTty.Sgr(fg, bgColor, 1));
            else if (bgColor > 0)
                sb.Append(AnsiTty.FgBg(fg, bgColor));
            else
                sb.Append(AnsiTty.Fg(fg));
            sb.Append(t);
        }
    }

    private static int VW(string text) => TuiHelper.DisplayWidth(text);
    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (TuiHelper.DisplayWidth(text) <= maxVW) return text;
        int vw = 0, chars = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w + 2 > maxVW) break; // 预留 "…" 两列
            vw += w; chars += rune.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }
    private static string Padding(int n) => n > 0 ? n.ToString() : "";
}
