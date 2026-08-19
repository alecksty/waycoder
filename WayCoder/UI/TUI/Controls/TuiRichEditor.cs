using System.Text;
using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Tui.Edit;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 增强版富文本编辑控件 —— 语法高亮、行号、诊断 Gutter、CJK 感知光标。
/// 绑定 EditorCore 数据模型，负责渲染和键盘交互。
///
/// 键盘：
///   ↑↓←→ — 光标移动（Shift+方向 = 扩展选择）
///   Home/End — 行首/行尾 · PgUp/PgDn — 翻页
///   Backspace/Delete — 删除 · Enter — 换行
///   Tab / Shift+Tab — 缩进 / 反缩进（选中态整块）
///   Ctrl+←/→ — 词级移动 · Ctrl+Backspace/Ctrl+Delete — 删词 · Ctrl+K — 删到行尾
///   Ctrl+D — 重复行 · Ctrl+Shift+K — 删除整行 · Ctrl+E — 到行尾
///   Ctrl+Z — 撤销 · Ctrl+Y / Ctrl+Shift+Z — 重做
///   Ctrl+X/C/V — 剪切/复制/粘贴（无选区时整行）· Shift+Insert — 粘贴
///   Ctrl+A — 全选 · Ctrl+F — 搜索 · Ctrl+G — 跳转行 · Ctrl+S — 保存
///   Esc / Ctrl+Q — 退出
///   可打印字符 — 插入
/// </summary>
public class TuiRichEditor : TuiEditBase
{
    // ── 数据模型 ──
    private EditorCore _core = new();
    public EditorCore Core
    {
        get => _core;
        set
        {
            if (_core != null) _core.OnContentChanged -= OnCoreChanged;
            _core = value;
            _core.OnContentChanged += OnCoreChanged;
            ResetDirtyState();
        }
    }

    /// <summary>只读模式（转发 Core.ReadOnly）：不允许修改缓冲区，只能查看/滚动/查找。</summary>
    public bool ReadOnly
    {
        get => Core.ReadOnly;
        set { Core.ReadOnly = value; MarkDirty(); }
    }

    // ── 按行增量重绘状态 ──
    private readonly HashSet<int> _dirtyLines = new();
    private bool _allDirty = true;
    private int _lastScroll = -1;
    private int _lastLineCount = -1;
    private int _lastCursorLine = -1;

    /// <summary>内容变更（插入/删除/粘贴/撤销/重做）→ 精确标记受影响行为脏。</summary>
    private void OnCoreChanged()
    {
        var range = Core.LastChange;
        int newCount = Core.Lines.Count;
        if (range == null || newCount != _lastLineCount)
        {
            // 结构变化（增删行→后续行整体位移）或无法确定范围 → 全量重绘
            _allDirty = true;
            _lastLineCount = newCount;
        }
        else
        {
            for (int li = range.Value.Start; li <= range.Value.End; li++)
                MarkLineDirty(li);
        }
        _lastCursorLine = Core.Cy;
        MarkDirty();
    }

    private void ResetDirtyState()
    {
        _dirtyLines.Clear();
        _allDirty = true;
        _lastScroll = Core.Scroll;
        _lastLineCount = Core.Lines.Count;
        _lastCursorLine = Core.Cy;
        MarkDirty();
    }

    private void MarkLineDirty(int li) { if (li >= 0) _dirtyLines.Add(li); }

    /// <summary>外部光标跳转/文件重载后调用：强制全量重绘。</summary>
    public void MarkFullRedraw() { _allDirty = true; MarkDirty(); }

    /// <summary>编辑器内容区的基础背景色（0=继承/透明）。</summary>
    private int BaseBg => Bg > 0 ? Bg : GetInheritedBg();

    /// <summary>整行底色填充（覆盖旧内容，保证增量重绘正确清除残留高亮）。</summary>
    private void FillLineBg(StringBuilder sb, int row, int col, int width, int bg)
    {
        var rb = new RenderBuffer();
        if (bg > 0)
            rb.Write(row, col, new string(' ', width), bg: bg);
        else
        {
            rb.Reset();
            rb.Write(row, col, new string(' ', width));
        }
        sb.Append(rb.ToString());
    }

    // ── 外观 ──
    public int LineNumberWidth { get; set; } = 5;
    public int GutterWidth { get; set; } = 1;
    public int CursorFg { get; set; }
    public int CursorBg { get; set; }
    /// <summary>括号配对高亮背景（光标处括号与其配对括号）</summary>
    public int BracketMatchBg { get; set; } = AnsiColors.BgBrightCyan;
    /// <summary>软换行：超宽行按可视宽度折行显示（不改缓冲区，仅显示层）。</summary>
    public bool SoftWrap { get; set; }
    public int TitleFg { get; set; }
    public int SeparatorFg { get; set; }
    public int GutterErrorFg { get; set; }
    public int GutterWarningFg { get; set; }
    public int LineNumFg { get; set; }
    public int BorderFg { get; set; }

    // ── 事件 ──
    public event Action? OnSaveRequested;
    public event Action? OnJumpRequested;
    public event Action? OnFindRequested;
    public event Action? OnExitRequested;
    /// <summary>鼠标点击编辑区时触发（用于把焦点切回编辑区）。</summary>
    public event Action? OnFocusRequested;

    // ── 软换行：预计算的视觉行模型 ──
    private readonly List<WrappedRow> _wrapped = new();
    private readonly List<int> _lineStartRow = new();   // _lineStartRow[li] = 缓冲行 li 的首个视觉行
    private record struct WrappedRow(int BufferLine, int SegStart, int SegEnd);

    /// <summary>富文本编辑器接受 Tab 键作为缩进输入。</summary>
    protected override bool AcceptsTab => true;

    /// <summary>可见行数（从 Height 推导）</summary>
    public int VisibleLines => Height > 0 ? Height : 10;

    /// <summary>内容区起始列（跳过行号和 Gutter）</summary>
    private int ContentStart => LineNumberWidth + GutterWidth;

    public TuiRichEditor()
    {
        Width = 80;
        Height = 24;
        Focused = true;
        Fg = TuiTheme.Current.ControlFg;
        CursorFg = TuiTheme.Current.ControlFocusedFg;
        CursorBg = TuiTheme.Current.ControlFocusedBg;
        BracketMatchBg = AnsiColors.BgBrightCyan;
        TitleFg = TuiTheme.Current.ChatSystemFg;
        SeparatorFg = TuiTheme.Current.ChatSystemFg;
        GutterErrorFg = TuiTheme.Current.IconErrorFg;
        GutterWarningFg = TuiTheme.Current.IconWarnFg;
        LineNumFg = TuiTheme.Current.TextAreaLineNumFg;
        BorderFg = TuiTheme.Current.ChatSystemFg;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 调整滚动确保光标可见（可能因光标越界而改变 Scroll）
        AdjustScroll();
        int vh = VisibleLines;
        int prefixW = LineNumberWidth + GutterWidth;

        // 检测滚动变化（鼠标滚轮 / 光标越界自动滚动）→ 视口整体位移，全量重绘
        if (Core.Scroll != _lastScroll)
        {
            _allDirty = true;
            _lastScroll = Core.Scroll;
        }

        // 检测外部光标行变化（跳转/查找等不经 MoveCursor 原语）→ 旧/新行高亮切换
        if (Core.Cy != _lastCursorLine)
        {
            if (!_allDirty)
            {
                MarkLineDirty(_lastCursorLine);
                MarkLineDirty(Core.Cy);
            }
            _lastCursorLine = Core.Cy;
        }

        // 无按行脏标记却需要重绘（如焦点切换的 Invalidate）→ 视为全量
        if (!_allDirty && _dirtyLines.Count == 0) _allDirty = true;

        // 括号匹配：光标在括号上时，计算其配对括号（用于高亮）
        var match = Core.MatchingBracketAt(Core.Cy, Core.Cx);
        int baseBg = BaseBg;

        for (int i = 0; i < vh; i++)
        {
            int li = Core.Scroll + i;
            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            // 按行增量：非全量且该行未标记脏 → 跳过（保持上一帧内容）
            if (!_allDirty && !_dirtyLines.Contains(li)) continue;

            bool isCursor = li == Core.Cy && IsEnabled;
            int contentW = Math.Max(0, Width - prefixW);

            // ── 整行背景填充（光标行高亮 / 普通行透明基底），覆盖残留高亮 ──
            FillLineBg(sb, row, absX, Width, isCursor ? CursorBg : baseBg);

            if (li < Core.Lines.Count)
            {
                var lineDiags = Core.GetDiagnosticsAtLine(li + 1);
                var hasError = lineDiags.Any(d => d.Severity == Severity.Error);
                var hasWarning = !hasError && lineDiags.Any(d => d.Severity == Severity.Warning);
                var hasInfo = !hasError && !hasWarning && lineDiags.Any(d => d.Severity == Severity.Info);

                // ── 诊断指示符（Gutter） ──
                int gutterFg;
                string gutterSymbol;
                if (hasError) { gutterFg = GutterErrorFg; gutterSymbol = "●"; }
                else if (hasWarning) { gutterFg = GutterWarningFg; gutterSymbol = "▲"; }
                else if (hasInfo) { gutterFg = 96; gutterSymbol = "i"; }
                else { gutterFg = 90; gutterSymbol = "·"; }

                // 诊断背景色（复用 Syntax 常量）
                int diagBg = hasError ? Syntax.ErrorBg : hasWarning ? Syntax.WarningBg : 0;

                // ── 行号 ──
                var lnText = (li + 1).ToString().PadLeft(4);
                int lnFg = isCursor ? CursorFg : LineNumFg;
                int lnBg = isCursor ? CursorBg : (Bg > 0 ? Bg : 0);
                WriteAt(sb, row, absX, lnText, lnFg, lnBg);

                // 行号后空格
                if (diagBg > 0 && isCursor)
                    WriteAt(sb, row, absX + 4, " ", lnFg, diagBg);
                else if (isCursor)
                    WriteAt(sb, row, absX + 4, " ", lnFg, CursorBg);
                else
                    WriteAt(sb, row, absX + 4, " ", lnFg, lnBg);

                // ── Gutter 符号 ──
                WriteAt(sb, row, absX + LineNumberWidth, gutterSymbol, gutterFg,
                    isCursor ? (diagBg > 0 ? diagBg : CursorBg) : Bg);

                // ── 语法高亮内容（Tab 展开为 4 空格显示，缓冲区保留原字符）──
                var rawLine = Core.Lines[li].ToString();
                RenderSyntaxLine(sb, row, absX + prefixW, ExpandTabs(rawLine),
                    contentW, isCursor ? CursorBg : (diagBg > 0 ? diagBg : Bg),
                    isCursor ? CursorFg : Fg);

                // ── 括号匹配高亮：光标处括号 + 配对括号（可能在同一行或另一行）──
                if (match != null && IsEnabled)
                {
                    if (li == Core.Cy)
                        HighlightBracket(sb, row, absX, rawLine, Core.Cx);
                    if (li == match.Value.Line)
                        HighlightBracket(sb, row, absX, rawLine, match.Value.Col);
                }

                // ── 光标位置 ──
                if (IsCursorOwner && isCursor && IsEnabled)
                {
                    var preCursor = rawLine.Length > 0
                        ? rawLine[..Math.Min(Core.Cx, rawLine.Length)]
                        : "";
                    int cursorVisualOffset = AnsiHelper.DisplayWidth(ExpandTabs(preCursor));
                    RecordCursorPos(row, absX + prefixW + cursorVisualOffset);
                }
            }
            else
            {
                // 空行（缓冲区末尾之后）
                var tildeFg = isCursor ? CursorFg : 2;
                var tildeBg = isCursor ? CursorBg : Bg;
                WriteAt(sb, row, absX, "    ~", tildeFg, tildeBg);

                // ── 光标位置（空行） ──
                if (IsCursorOwner && isCursor && IsEnabled)
                {
                    RecordCursorPos(row, absX + prefixW);
                }
            }
        }

        // 本帧已按行增量重绘完成，清除脏标记
        _allDirty = false;
        _dirtyLines.Clear();
    }

    /// <summary>渲染语法高亮的一行内容，CJK 宽度感知截断</summary>
    private void RenderSyntaxLine(StringBuilder sb, int row, int col, string line,
        int maxVw, int bg, int defaultFg)
    {
        if (string.IsNullOrEmpty(line))
        {
            if (bg > 0)
            {
                var rb = new RenderBuffer();
                rb.Write(row, col, " ", bg: bg);
                sb.Append(rb.ToString());
            }
            return;
        }

        // 禁用时全部使用禁用前景色
        bool disabled = !IsEnabled;
        int disabledFg = DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg;

        var tokens = Core.Syntax.Tokenize(line);
        int vw = 0;
        foreach (var (text, ansiColor) in tokens)
        {
            int textVw = AnsiHelper.DisplayWidth(text);
            if (vw + textVw > maxVw)
            {
                int remain = maxVw - vw;
                if (remain > 0)
                {
                    var truncated = TruncateByVw(text, remain);
                    int c = disabled ? disabledFg : (ansiColor > 0 ? ansiColor : defaultFg);
                    WriteAt(sb, row, col + vw, truncated, c, bg);
                }
                break;
            }
            int color = disabled ? disabledFg : (ansiColor > 0 ? ansiColor : defaultFg);
            WriteAt(sb, row, col + vw, text, color, bg);
            vw += textVw;
        }
    }

    /// <summary>在指定缓冲区列上以配对高亮背景重绘一个括号字符（Tab/CJK 宽度感知）。</summary>
    private void HighlightBracket(StringBuilder sb, int row, int absX, string rawLine, int col)
    {
        if (col < 0 || col >= rawLine.Length) return;
        var pre = rawLine[..col];
        int vcol = AnsiHelper.DisplayWidth(ExpandTabs(pre));
        WriteAt(sb, row, absX + LineNumberWidth + GutterWidth + vcol,
            rawLine[col].ToString(), AnsiColors.Black, BracketMatchBg);
    }

    /// <summary>Tab 展开为 4 空格（仅显示层，不修改缓冲区内容）。</summary>
    private static string ExpandTabs(string s) => s.Replace("\t", "    ");

    /// <summary>按视觉宽度截断文本（CJK 安全）</summary>
    private static string TruncateByVw(string text, int maxVw)
    {
        int vw = 0;
        int bytePos = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
        {
            int w = runes[i].Value == '\t' ? 4 : AnsiHelper.DisplayWidth(runes[i].ToString());
            if (vw + w > maxVw)
                return text.Substring(0, bytePos);
            vw += w;
            bytePos += runes[i].Utf16SequenceLength;
        }
        return text;
    }

    /// <summary>确保光标在可见区域内</summary>
    private void AdjustScroll()
    {
        int vh = VisibleLines;
        if (Core.Cy < Core.Scroll) Core.Scroll = Core.Cy;
        if (Core.Cy >= Core.Scroll + vh) Core.Scroll = Core.Cy - vh + 1;
        Core.Scroll = Math.Clamp(Core.Scroll, 0, Math.Max(0, Core.Lines.Count - vh));
    }

    /// <summary>
    /// 计算并设置光标屏幕坐标（基于 EditorCore 数据模型）。
    /// 不依赖 OnRender 调用，保证即使控件未被重绘光标位置也正确。
    /// </summary>
    protected override void GotoCursorPos()
    {
        if (!IsCursorOwner) return;

        var absX = _lastAbsX;
        var absY = _lastAbsY;
        int prefixW = LineNumberWidth + GutterWidth;
        int vh = VisibleLines;

        // 确保光标在视口内
        if (Core.Cy < Core.Scroll) Core.Scroll = Core.Cy;
        if (Core.Cy >= Core.Scroll + vh) Core.Scroll = Core.Cy - vh + 1;
        Core.Scroll = Math.Clamp(Core.Scroll, 0, Math.Max(0, Core.Lines.Count - vh));

        int screenRow = absY + (Core.Cy - Core.Scroll);

        // 计算光标列偏移（Tab 展开为 4 空格）
        string line = Core.Cy < Core.Lines.Count ? Core.Lines[Core.Cy].ToString() : "";
        var preCursor = line.Length > 0
            ? line[..Math.Min(Core.Cx, line.Length)]
            : "";
        int cursorVisualOffset = AnsiHelper.DisplayWidth(ExpandTabs(preCursor));

        _cursorRow = Math.Clamp(screenRow, absY, absY + vh - 1);
        _cursorCol = absX + prefixW + cursorVisualOffset;
        _showCursor = true;
    }

    // ── 键盘处理 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── Ctrl 组合键（编辑器专属 + 行感知剪贴板 + 词级/行级操作）──
        if (ctrl)
        {
            switch (key.Key)
            {
                // 编辑器专属（触发屏幕级事件）
                case ConsoleKey.F: OnFindRequested?.Invoke(); return true;
                case ConsoleKey.G: OnJumpRequested?.Invoke(); return true;
                case ConsoleKey.S: OnSaveRequested?.Invoke(); return true;
                case ConsoleKey.Q: OnExitRequested?.Invoke(); return true;

                // 剪贴板（无选区时整行，对齐旧行为）
                case ConsoleKey.X: Core.CutLine(); return true;
                case ConsoleKey.C: Core.CopyLine(); return true;
                case ConsoleKey.V: Core.PasteClipboard(); return true;
                case ConsoleKey.A: Core.SelectAll(); return true;

                // 撤销 / 重做
                case ConsoleKey.Z:
                    if (shift) Core.Redo(); else Core.Undo();
                    return true;
                case ConsoleKey.Y: Core.Redo(); return true;

                // 词级 / 行级（新增）
                case ConsoleKey.D: Core.DuplicateLine(); return true;
                case ConsoleKey.LeftArrow: Core.MoveWord(-1); return true;
                case ConsoleKey.RightArrow: Core.MoveWord(1); return true;
                case ConsoleKey.Delete: Core.DeleteWordAfter(); return true;
                case ConsoleKey.K when shift: Core.DeleteLine(); return true;
            }
        }

        // ── 退出 ──
        if (key.Key == ConsoleKey.Escape)
        {
            OnExitRequested?.Invoke();
            return true;
        }

        // ── Tab / Shift+Tab：选中态整块缩进/反缩进，否则按缩进模式插入 ──
        if (key.Key == ConsoleKey.Tab)
        {
            if (shift) Core.IndentBlock(-1);
            else if (Core.HasSelection) Core.IndentBlock(1);
            else Core.InsertTab();
            return true;
        }

        // 其余交给基类分发引擎：
        //   Ctrl+E(行尾)/Ctrl+K(删到行尾)/Ctrl+Backspace(删词)/Ctrl+Insert(复制)、
        //   Shift+方向(扩展选择)/Shift+Insert(粘贴)、方向键/Home/End/PgUp/PgDn、
        //   Backspace/Delete/Enter、可打印字符（选区感知）
        return base.OnKey(key);
    }

    public override void OnResize(int newParentW, int newParentH)
    {
        Width = Math.Max(40, newParentW);
        Height = Math.Max(5, newParentH);
    }

    // ── 鼠标 ──

    /// <summary>
    /// 鼠标滚轮滚动编辑区（3 行/格）；左键点击定位光标（Tab/CJK 宽度感知）。
    /// </summary>
    public override bool OnMouse(InputEvent ev)
    {
        if (!IsEnabled) return false;
        if (ev.Type != InputType.Mouse) return false;

        int absX = GetAbsoluteX();
        int absY = GetAbsoluteY();
        if (ev.MouseX < absX || ev.MouseX >= absX + Width ||
            ev.MouseY < absY || ev.MouseY >= absY + Height)
            return false;

        // 滚轮滚动（视口位移 → 全量重绘）
        if (ev.MouseScrollUp)
        {
            Core.Scroll = Math.Max(0, Core.Scroll - 3);
            MarkFullRedraw();
            return true;
        }
        if (ev.MouseScrollDown)
        {
            int maxScroll = Math.Max(0, Core.Lines.Count - VisibleLines);
            Core.Scroll = Math.Min(maxScroll, Core.Scroll + 3);
            MarkFullRedraw();
            return true;
        }

        // 左键定位光标（跨行跳转 → 全量重绘）
        if (ev.MouseLeft)
        {
            OnFocusRequested?.Invoke();
            int relY = ev.MouseY - absY;
            int line = Math.Clamp(Core.Scroll + relY, 0, Math.Max(0, Core.Lines.Count - 1));
            int relX = Math.Max(0, ev.MouseX - (absX + ContentStart));
            string text = line < Core.Lines.Count ? Core.Lines[line].ToString() : "";
            Core.ClearSelection();
            Core.Cy = line;
            Core.Cx = VisualToCol(text, relX);
            MarkFullRedraw();
            return true;
        }

        return base.OnMouse(ev);
    }

    /// <summary>把点击的视觉列（相对内容区起点）映射回缓冲区字符列（Tab/CJK 宽度感知）。</summary>
    internal static int VisualToCol(string line, int visualCol)
    {
        if (string.IsNullOrEmpty(line) || visualCol <= 0) return 0;
        int v = 0, idx = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            int w = rune.Value == '\t' ? 4 : AnsiHelper.DisplayWidth(rune.ToString());
            if (visualCol < v + w) return idx;
            v += w;
            idx += rune.Utf16SequenceLength;
        }
        return line.Length;
    }

    // ── 便捷方法 ──

    /// <summary>加载文件并准备编辑</summary>
    public void LoadFile(string? filePath)
    {
        filePath ??= "untitled.txt";
        if (!File.Exists(filePath) && !filePath.Contains('.'))
            filePath += ".txt";
        Core.LoadFile(filePath);
        ResetDirtyState();
    }

    // ── 抽象原语（委托给 EditorCore）──

    protected override void MoveCursorLeft()      { Core.MoveCursor(-1, 0); OnCursorMoved(); }
    protected override void MoveCursorRight()     { Core.MoveCursor(1, 0); OnCursorMoved(); }
    protected override void MoveCursorUp()        { Core.MoveCursor(0, -1); OnCursorMoved(); }
    protected override void MoveCursorDown()      { Core.MoveCursor(0, 1); OnCursorMoved(); }
    protected override void MoveCursorHome()      { Core.MoveHome(); OnCursorMoved(); }
    protected override void MoveCursorEnd()       { Core.MoveEnd(); OnCursorMoved(); }
    protected override void MoveCursorPageUp()    { Core.MovePageUp(VisibleLines); OnCursorMoved(); }
    protected override void MoveCursorPageDown()  { Core.MovePageDown(VisibleLines); OnCursorMoved(); }

    /// <summary>光标移动后：标记旧/新光标行脏（高亮切换），仅跨行才需重绘。</summary>
    private void OnCursorMoved()
    {
        int cy = Core.Cy;
        if (cy == _lastCursorLine)
        {
            // 行内移动：光标列变化，仅当前行括号匹配高亮可能变化 → 只重绘当前行
            MarkLineDirty(cy);
            MarkDirty();
            return;
        }
        MarkLineDirty(_lastCursorLine);
        MarkLineDirty(cy);
        _lastCursorLine = cy;
        MarkDirty();
    }

    protected override void InsertChar(char ch)        => Core.InsertText(ch.ToString());
    protected override void DeleteCharBefore()          => Core.Backspace();
    protected override void DeleteCharAfter()           => Core.Delete();
    protected override void InsertNewLine()             => Core.NewLine();
    protected override void Undo()                      => Core.Undo();
    protected override void Redo()                      => Core.Redo();
    protected override void PasteText(string text)      => Core.InsertText(text);
    protected override string GetText()                 => string.Join("\n", Core.Lines.Select(l => l.ToString()));

    protected override void DeleteWordBefore() => Core.DeleteWordBefore();
    protected override void DeleteToLineEnd() => Core.DeleteToLineEnd();

    // 选择（委托给 EditorCore 的锚点选区模型）
    public override bool HasSelection => Core.HasSelection;
    public override string? GetSelectedText() => Core.GetSelectedText();
    protected override void SelectAll() => Core.SelectAll();
    protected override void ClearSelection() => Core.ClearSelection();
    protected override void StartSelection() => Core.StartSelection();
    protected override void ExtendSelection() => Core.ExtendSelection();
    protected override void DeleteSelection() => Core.DeleteSelection();
}
