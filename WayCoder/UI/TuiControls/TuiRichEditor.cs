using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 增强版富文本编辑控件 —— 语法高亮、行号、诊断 Gutter、CJK 感知光标。
/// 绑定 EditorCore 数据模型，负责渲染和键盘交互。
///
/// 键盘：
///   ↑↓←→ — 光标移动
///   Home/End — 行首/行尾
///   PgUp/PgDn — 翻页
///   Backspace/Delete — 删除
///   Enter — 换行
///   Tab — 4 空格
///   Ctrl+Z — 撤销
///   Ctrl+X/C/V — 剪切/复制/粘贴
///   Ctrl+Y — 删除行
///   Ctrl+G — 跳转行（触发 OnJumpRequested）
///   Ctrl+S — 保存（触发 OnSaveRequested）
///   可打印字符 — 插入
/// </summary>
public class TuiRichEditor : TuiEditBase
{
    // ── 数据模型 ──
    public EditorCore Core { get; set; } = new();

    // ── 外观 ──
    public int LineNumberWidth { get; set; } = 5;
    public int GutterWidth { get; set; } = 1;
    public int CursorFg { get; set; }
    public int CursorBg { get; set; }
    public int TitleFg { get; set; }
    public int SeparatorFg { get; set; }
    public int GutterErrorFg { get; set; }
    public int GutterWarningFg { get; set; }
    public int LineNumFg { get; set; }
    public int BorderFg { get; set; }

    // ── 事件 ──
    public event Action? OnSaveRequested;
    public event Action? OnJumpRequested;
    public event Action? OnExitRequested;

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
        // 调整滚动确保光标可见
        AdjustScroll();
        int vh = VisibleLines;
        int prefixW = LineNumberWidth + GutterWidth;

        for (int i = 0; i < vh; i++)
        {
            int li = Core.Scroll + i;
            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            bool isCursor = li == Core.Cy && IsEnabled;
            int contentW = Math.Max(0, Width - prefixW);

            // ── 光标行整行高亮（仅启用且聚焦时） ──
            if (isCursor)
            {
                var rbBg = new RenderBuffer();
                rbBg.Write(row, absX, new string(' ', Width), bg: CursorBg);
                sb.Append(rbBg.ToString());
            }

            // 禁用状态下所有文字变灰
            int textFg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg) : 0;

            if (li < Core.Lines.Count)
            {
                var lineDiags = Core.GetDiagnosticsAtLine(li + 1);
                var hasError = lineDiags.Any(d => d.Severity == Severity.Error);
                var hasWarning = !hasError && lineDiags.Any(d => d.Severity == Severity.Warning);

                // ── 诊断指示符（Gutter） ──
                int gutterFg;
                string gutterSymbol;
                if (hasError) { gutterFg = GutterErrorFg; gutterSymbol = "●"; }
                else if (hasWarning) { gutterFg = GutterWarningFg; gutterSymbol = "▲"; }
                else { gutterFg = 90; gutterSymbol = "·"; }

                // 诊断背景色
                int diagBg = hasError ? 41 : hasWarning ? 103 : 0;

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

                // ── 语法高亮内容 ──
                RenderSyntaxLine(sb, row, absX + prefixW, Core.Lines[li].ToString(),
                    contentW, isCursor ? CursorBg : (diagBg > 0 ? diagBg : Bg));

                // ── 光标位置 ──
                if (IsCursorOwner && isCursor && IsEnabled)
                {
                    var line = Core.Lines[li].ToString();
                    var preCursor = line.Length > 0
                        ? line[..Math.Min(Core.Cx, line.Length)]
                        : "";
                    int cursorVisualOffset = TuiHelper.DisplayWidth(preCursor);
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
    }

    /// <summary>渲染语法高亮的一行内容，CJK 宽度感知截断</summary>
    private void RenderSyntaxLine(StringBuilder sb, int row, int col, string line,
        int maxVw, int bg)
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
            int textVw = TuiHelper.DisplayWidth(text);
            if (vw + textVw > maxVw)
            {
                int remain = maxVw - vw;
                if (remain > 0)
                {
                    var truncated = TruncateByVw(text, remain);
                    int c = disabled ? disabledFg : (ansiColor > 0 ? ansiColor : (bg > 0 ? 37 : Fg));
                    WriteAt(sb, row, col + vw, truncated, c, bg);
                }
                break;
            }
            int color = disabled ? disabledFg : (ansiColor > 0 ? ansiColor : (bg > 0 ? 37 : Fg));
            WriteAt(sb, row, col + vw, text, color, bg);
            vw += textVw;
        }
    }

    /// <summary>按视觉宽度截断文本（CJK 安全）</summary>
    private static string TruncateByVw(string text, int maxVw)
    {
        int vw = 0;
        int bytePos = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
        {
            int w = runes[i].Value > 127 ? 2 : 1;
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

        // 计算光标列偏移
        string line = Core.Cy < Core.Lines.Count ? Core.Lines[Core.Cy].ToString() : "";
        var preCursor = line.Length > 0
            ? line[..Math.Min(Core.Cx, line.Length)]
            : "";
        int cursorVisualOffset = TuiHelper.DisplayWidth(preCursor);

        _cursorRow = Math.Clamp(screenRow, absY, absY + vh - 1);
        _cursorCol = absX + prefixW + cursorVisualOffset;
        _showCursor = true;
    }

    // ── 键盘处理 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        switch (key.Key)
        {
            // ── 光标移动 ──
            case ConsoleKey.UpArrow:    Core.MoveCursor(0, -1); return true;
            case ConsoleKey.DownArrow:  Core.MoveCursor(0, 1); return true;
            case ConsoleKey.LeftArrow:  Core.MoveCursor(-1, 0); return true;
            case ConsoleKey.RightArrow: Core.MoveCursor(1, 0); return true;
            case ConsoleKey.Home:       Core.MoveHome(); return true;
            case ConsoleKey.End:        Core.MoveEnd(); return true;
            case ConsoleKey.PageUp:     Core.MovePageUp(VisibleLines); return true;
            case ConsoleKey.PageDown:   Core.MovePageDown(VisibleLines); return true;

            // ── 编辑 ──
            case ConsoleKey.Backspace:  Core.Backspace(); return true;
            case ConsoleKey.Delete:     Core.Delete(); return true;
            case ConsoleKey.Enter:      Core.NewLine(); return true;
            case ConsoleKey.Tab:        Core.InsertTab(); return true;

            // ── Ctrl 组合键 ──
            case ConsoleKey.Z when ctrl:
                Core.Undo();
                return true;
            case ConsoleKey.X when ctrl:
                Core.CutLine();
                return true;
            case ConsoleKey.C when ctrl:
                Core.CopyLine();
                return true;
            case ConsoleKey.V when ctrl:
                Core.PasteClipboard();
                return true;
            case ConsoleKey.Y when ctrl:
                Core.DeleteLine();
                return true;
            case ConsoleKey.G when ctrl:
                OnJumpRequested?.Invoke();
                return true;
            case ConsoleKey.S when ctrl:
                OnSaveRequested?.Invoke();
                return true;

            // ── 退出 ──
            case ConsoleKey.Escape:
            case ConsoleKey.Q when ctrl:
                OnExitRequested?.Invoke();
                return true;

            default:
                // 可打印字符插入
                if ((key.KeyChar >= ' ' && key.KeyChar <= '~') || key.KeyChar > 127)
                {
                    Core.InsertText(key.KeyChar.ToString());
                    return true;
                }
                return false;
        }
    }

    public override void OnResize(int newParentW, int newParentH)
    {
        Width = Math.Max(40, newParentW);
        Height = Math.Max(5, newParentH);
    }

    // ── 便捷方法 ──

    /// <summary>加载文件并准备编辑</summary>
    public void LoadFile(string? filePath)
    {
        filePath ??= "untitled.txt";
        if (!File.Exists(filePath) && !filePath.Contains('.'))
            filePath += ".txt";
        Core.LoadFile(filePath);
    }

    // ── 抽象原语（委托给 EditorCore）──

    protected override void MoveCursorLeft()      => Core.MoveCursor(-1, 0);
    protected override void MoveCursorRight()     => Core.MoveCursor(1, 0);
    protected override void MoveCursorUp()        => Core.MoveCursor(0, -1);
    protected override void MoveCursorDown()      => Core.MoveCursor(0, 1);
    protected override void MoveCursorHome()      => Core.MoveHome();
    protected override void MoveCursorEnd()       => Core.MoveEnd();
    protected override void MoveCursorPageUp()    => Core.MovePageUp(VisibleLines);
    protected override void MoveCursorPageDown()  => Core.MovePageDown(VisibleLines);

    protected override void InsertChar(char ch)        => Core.InsertText(ch.ToString());
    protected override void DeleteCharBefore()          => Core.Backspace();
    protected override void DeleteCharAfter()           => Core.Delete();
    protected override void InsertNewLine()             => Core.NewLine();
    protected override void Undo()                      => Core.Undo();
    protected override void Redo()                      => Core.Undo(); // EditorCore 无双栈
    protected override void PasteText(string text)      => Core.InsertText(text);
    protected override string GetText()                 => string.Join("\n", Core.Lines.Select(l => l.ToString()));

    protected override void DeleteWordBefore()
    {
        var line = Core.Lines[Core.Cy].ToString();
        int pos = Core.Cx;
        while (pos > 0 && line[pos - 1] == ' ') pos--;
        while (pos > 0 && line[pos - 1] != ' ') pos--;
        var deleted = line[pos..Core.Cx];
        Core.Lines[Core.Cy] = new StringBuilder(line[..pos] + line[Core.Cx..]);
        Core.Cx = pos;
        if (deleted.Length > 0) Core.Undo(); // push undo
    }

    protected override void DeleteToLineEnd()
    {
        var line = Core.Lines[Core.Cy].ToString();
        int col = Math.Min(Core.Cx, line.Length);
        if (col >= line.Length) return;
        Core.Lines[Core.Cy] = new StringBuilder(line[..col]);
    }

    // 选择（简化实现 —— TuiRichEditor 不使用基类的选择分发）
    public override bool HasSelection => false;
    public override string? GetSelectedText() => null;
    protected override void SelectAll() { }
    protected override void ClearSelection() { }
    protected override void StartSelection() { }
    protected override void ExtendSelection() { }
    protected override void DeleteSelection() { }
}
