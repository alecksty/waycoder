using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

using WayCoder.UI.Tui.Edit;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

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
    // diff 有色背景：近黑的深色调（黑底带一点点红/绿/青 tint，柔和不刺眼）。
    // 用 TrueColor RGB 而非 256 色码：色值压到 50 以下，肉眼接近黑、又带一点色相，
    // 替代此前的 52/28/23（#5f0000/#008700/#005f5f 过饱和，与「代码背景默认黑」不协调）。
    private static readonly int BgDelete  = AnsiTty.RgbCode(50, 0, 0);   // 黑带一点点红（删除行）
    private static readonly int BgInsert  = AnsiTty.RgbCode(0, 50, 0);   // 黑带一点点绿（添加行）
    private static readonly int BgContext = AnsiTty.RgbCode(0, 35, 35);  // 黑带一点点青（当前 hunk 高亮行）

    // 行号/符号前缀前景：近白带一点点红/绿（区分增删，又不刺眼）
    private static readonly int FgDelNum = AnsiTty.RgbCode(255, 210, 210); // 白带一点点红（删除行行号）
    private static readonly int FgInsNum = AnsiTty.RgbCode(210, 255, 210); // 白带一点点绿（添加行行号）

    /// <summary>diff 有色行样式：深背景 + 前景（粗体可选）。
    /// 用 FgBgCode（自动识别 256 色/TrueColor），FgBg 只支持标准 16 色合并，传 256 色码会错乱。</summary>
    private static string ColorStyle(int fg, int bg, bool bold)
        => AnsiTty.FgBgCode(fg, bg) + (bold ? AnsiTty.Sgr(1) : "");

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
    /// <summary>
    /// 把 old/new 渲染成带中间格式标记（«red»/«green»）的 diff 文本，供聊天区展示源码对比。
    /// 三端渲染器统一解析：TUI→ANSI、Web→HTML、GUI→富文本。工具返回值里拼入此文本，
    /// 工具输出气泡即显示差异（YOLO 自动放行时不弹确认窗但保留可视化）。
    /// </summary>
    public static string RenderAsMarkup(string oldContent, string newContent, string filePath)
    {
        var hunks = BuildHunks(oldContent, newContent);
        if (hunks.Count == 0) return "";
        var sb = new StringBuilder();
        sb.Append("«bold»📄 变更预览：").Append(filePath).Append("«/»\n");
        foreach (var h in hunks)
        {
            if (!string.IsNullOrEmpty(h.Header))
                sb.Append("«dim»").Append(h.Header).Append("«/»\n");
            foreach (var l in h.Lines)
            {
                switch (l.Kind)
                {
                    case '+': sb.Append("«green»+").Append(l.Text).Append("«/»\n"); break;
                    case '-': sb.Append("«red»-").Append(l.Text).Append("«/»\n"); break;
                    default:  sb.Append("  ").Append(l.Text).Append('\n'); break;
                }
            }
        }
        return sb.ToString().TrimEnd('\n');
    }

    public static (Decision Decision, HashSet<int>? AcceptedHunks) Show(
        string oldContent, string newContent, string filePath)
    {
        // YOLO（畅通）：无任何确认阻止，直接接受全部变更 —— Web/TUI/GUI 三端统一
        // （AskUserQuestionTool 已做 YOLO 自动回答，diff 预览是 write/edit 的独立确认，此处补齐）
        if (PermissionManager.CurrentMode == PermissionManager.Mode.Yolo)
            return (Decision.AcceptAll, null);

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
            // Agent 执行期（外层 RunAgentWithRenderLoop 渲染 + 按「键位作用域闸」路由按键）→ readKeys:false 只等待，
            // 避免 RenderWait 与外层循环并发渲染（双线程 Render 竞态花屏）与双读控制台；
            // 命令场景（/diff，主循环阻塞无外层循环）→ readKeys:true 自己渲染 + 读键。
            UxHelper.RenderWait(screen, evt, 120_000, win, readKeys: !Program.InAgentRenderLoop);
        }
        catch { evt.Set(); }
        return (result, resultAccepted);
    }

    // ── 窗口构建 ──

    internal static TuiWindow BuildDiffWindow(List<Hunk> hunks, string filePath,
        TuiScreen? screen, Action<Decision, HashSet<int>?> onDone)
    {
        // 窗口尺寸：别逼近全屏。宽度 3/4、高度 70%（各留边距），小终端还能再小 ——
        // diff 内容可滚动，窗口不用为了显示全部而顶满整屏。
        int winW = Math.Clamp((int)(Tty.Cols * 0.75), 50, Tty.Cols - 4);
        int winH = Math.Clamp((int)(Tty.Rows * 0.7), 12, Tty.Rows - 4);
        int contentBg = AnsiColors.BgBlack; // diff 视图固定深色底，保证语法高亮/红绿行对比

        var accepted = new HashSet<int>();
        var syntax = GetSyntaxForFile(filePath);

        // 标记加载：窗口壳/状态栏来自 diffpreview.tui，DiffView 自定义控件 code 注入 body 首位
        var res = TuiMarkup.LoadResource("dialogs/diffpreview.tui");
        var win = res.Window ?? throw new InvalidOperationException("diffpreview.tui 根应为 Dialog");
        win.Title = $"Diff 预览: {filePath}  ({hunks.Count} hunks)";
        win.Width = winW; win.Height = winH;
        win.MinWidth = 60; win.MinHeight = 12;
        win.WinBg = contentBg;
        var g = TuiTheme.Current.DialogGradient;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        var body = res.Find<TuiVBox>("body")!;

        // diff 视图（自定义控件，code 注入 body 首位；flex=1 吃满剩余高度，按钮行固定底部）
        var diff = new DiffView(hunks, accepted, syntax)
        {
            Flex = 1,
            Height = 5,
            Bg = contentBg,
            Focused = true,
        };
        body.Children.Insert(0, diff);

        // ── 动作（仅按钮四个动作：接受/跳过/全部接受/完成）──

        void Finish(Decision d, HashSet<int>? a)
        {
            onDone(d, a);
            win.OnClosed?.Invoke();
        }

        void OnY()
        {
            // 接受当前 hunk 并前进到下一块：已接受行打 ✓、高亮立即移走（接受反馈即时可见）
            if (!accepted.Contains(diff.CurrentHunk))
                accepted.Add(diff.CurrentHunk);
            if (diff.CurrentHunk < hunks.Count - 1)
                diff.SetCurrentHunk(diff.CurrentHunk + 1);
            diff.MarkDirty();
        }
        void OnN()
        {
            // 跳过当前 hunk（不标记接受），前进到下一块
            diff.SetCurrentHunk(diff.CurrentHunk + 1);
            diff.MarkDirty();
        }
        void OnA()
        {
            // 全部接受：立即完成，无需再确认
            Finish(Decision.AcceptAll, null);
        }
        void Quit()
        {
            // 完成：已接受的部分提交（Partial）；一个都没接受则全部拒绝
            if (accepted.Count == 0) Finish(Decision.RejectAll, null);
            else Finish(Decision.Partial, accepted);
        }

        Wire(res, "btnAccept", OnY);
        Wire(res, "btnSkip", OnN);
        Wire(res, "btnAll", OnA);
        Wire(res, "btnCancel", Quit);

        // Y/N/A/Q 由按钮自带快捷键（标记 shortcut="y/n/a/q"）经 RegisterButtonShortcuts 注册到窗口：
        // 按下即触发对应按钮 OnClick，无需聚焦。仅保留 Esc 关闭（模态对话框必需，否则无法退出）。
        win.RegisterShortcut(ConsoleKey.Escape, Quit);
        return win;
    }

    /// <summary>把标记里的按钮接到动作上（缺 id 静默跳过，标记改名不至于崩窗口）。渐变底是 TuiButton 默认值。</summary>
    private static void Wire(TuiMarkupResult res, string id, Action action)
    {
        var btn = res.Find<TuiButton>(id);
        if (btn != null) btn.OnClick = _ => action();
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
        public override bool OnMouse(InputEvent ev)
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
            result = ContextManager.TruncateByRunes(result, 2500) + "\n...（diff 已截断）\n";
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
            // 已接受的删除行：符号位改 ✓（前缀仍 6 列对齐），行内容仍保留供回看
            var prefix = Prefix(line.OldLine, '-', isAccepted);
            int fg = isCurrentHunk ? 97 : 37; // 深背景配亮/白前景
            int bg = BgDelete;
            var maxTextW = tw - 7;
            FillBg(sb, absY, absX, tw, bg);
            // 行号/符号前缀用「白带一点点红」，代码区恢复普通前景
            sb.Append(ColorStyle(FgDelNum, bg, isCurrentHunk));
            sb.Append(prefix).Append(' ');
            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
            sb.Append(AnsiTty.SgrReset);
        }
        else if (line.Kind == '+')
        {
            // 已接受的添加行：符号位改 ✓（前缀仍 6 列对齐）
            var prefix = Prefix(0, '+', isAccepted);
            int fg = isCurrentHunk ? 97 : 37;
            int bg = BgInsert;
            var maxTextW = tw - 7;
            FillBg(sb, absY, absX, tw, bg);
            sb.Append(ColorStyle(FgInsNum, bg, isCurrentHunk));
            sb.Append(prefix).Append(' ');
            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
            sb.Append(AnsiTty.SgrReset);
        }
        else
        {
            var prefix = Prefix(line.OldLine, ' ', false);
            var maxTextW = tw - 7;
            if (isCurrentHunk)
            {
                FillBg(sb, absY, absX, tw, BgContext);
                sb.Append(ColorStyle(97, BgContext, false));
                sb.Append(prefix).Append(' ');
                AppendHighlightedCode(sb, line.Text, syntax, 97, BgContext, false, maxTextW);
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
            { leftFg = isCurrentHunk ? 97 : 37; leftBg = BgDelete; }
        else if (isCurrentHunk)
            { leftFg = 97; leftBg = BgContext; leftBold = false; }
        else
            { leftFg = 37; leftBg = 0; }

        var leftPrefix = row.LeftKind == '-'
            ? Prefix(row.LeftLineNo, '-', isAccepted)
            : row.LeftText.Length > 0 ? Prefix(row.LeftLineNo, ' ', false) : "      ";

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
            // 删除行行号前缀用「白带一点点红」，代码区恢复普通前景
            int prefixFg = row.LeftKind == '-' ? FgDelNum : leftFg;
            sb.Append(leftBg > 0 ? ColorStyle(prefixFg, leftBg, leftBold) : "");
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
            { rightFg = isCurrentHunk ? 97 : 37; rightBg = BgInsert; }
        else if (isCurrentHunk)
            { rightFg = 97; rightBg = BgContext; rightBold = false; }
        else
            { rightFg = 37; rightBg = 0; }

        var rightPrefix = row.RightKind == '+'
            ? Prefix(row.RightLineNo, '+', isAccepted)
            : row.RightText.Length > 0 ? Prefix(row.RightLineNo, ' ', false) : "      ";

        if (isAccepted && !isCurrentHunk && row.RightKind != '+')
        {
            sb.Append(AnsiTty.SgrDim);
            sb.Append(rightPrefix).Append(' ').Append(row.RightText);
            sb.Append(AnsiTty.SgrReset);
        }
        else
        {
            // 右面板实际宽度（tw 减去左面板 + 分隔符；奇数差时右面板多 1 列）
            int rightPanelW = tw - panelWidth - 3;
            // 添加行行号前缀用「白带一点点绿」，代码区恢复普通前景
            int prefixFg = row.RightKind == '+' ? FgInsNum : rightFg;
            sb.Append(rightBg > 0 ? ColorStyle(prefixFg, rightBg, rightBold) : "");
            sb.Append(rightPrefix).Append(' ');
            int maxCodeW = rightPanelW - VW(rightPrefix) - 1;
            var rightCode = TruncateByVW(row.RightText, maxCodeW);
            int codeVW = VW(rightCode);
            AppendHighlightedCode(sb, rightCode, syntax, rightFg, rightBg, rightBold, int.MaxValue);
            // 背景色填充到右面板边界（与左面板 leftPad 对称），否则 + 行色条只到文字末尾、左右不对齐
            int rightPad = Math.Max(0, rightPanelW - VW(rightPrefix) - 1 - codeVW);
            sb.Append(new string(' ', rightPad));
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
            if (bold) sb.Append(ColorStyle(baseFg, bgColor, true));
            else if (bgColor > 0) sb.Append(ColorStyle(baseFg, bgColor, false));
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
                sb.Append(ColorStyle(fg, bgColor, true));
            else if (bgColor > 0)
                sb.Append(ColorStyle(fg, bgColor, false));
            else
                sb.Append(AnsiTty.Fg(fg));
            sb.Append(t);
        }
    }

    /// <summary>
    /// 行前缀统一 6 列：4 位行号 + 2 列符号区。
    /// 符号区：非接受态 "- "/"+ "；接受态 "✓"。宽度一律经 <see cref="VW"/>（AnsiHelper.DisplayWidth，
    /// 委托到唯一宽度真源 AnsiString.CharWidth）补齐到 6 列 —— 不再假设某个符号的固定宽度
    /// （如 ✓ 在多数等宽字体按 1 列渲染，硬编码「✓ 宽 2」会导致打钩后文字错位）。
    /// </summary>
    private static string Prefix(int lineNo, char sign, bool accepted)
    {
        var num = (lineNo > 0 ? lineNo.ToString() : "").PadLeft(4);
        var symbol = accepted ? "✓" : sign + " ";
        var p = num + symbol;
        var vw = VW(p);
        return vw < 6 ? p + new string(' ', 6 - vw) : p;
    }

    private static int VW(string text) => AnsiHelper.DisplayWidth(text);
    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (AnsiHelper.DisplayWidth(text) <= maxVW) return text;
        int vw = 0, chars = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var w = AnsiHelper.RuneWidth(rune);
            if (vw + w + 2 > maxVW) break; // 预留 "…" 两列
            vw += w; chars += rune.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }
}
