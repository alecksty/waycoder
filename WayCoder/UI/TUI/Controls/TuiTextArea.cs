using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Edit;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 多行文本编辑控件 —— 支持光标自由移动、自动换行、滚动、文本选择、撤销重做。
/// 可嵌入任何 View 容器中。
/// </summary>
public class TuiTextArea : TuiEditBase
{
    // ── 文本缓冲 ──

    /// <summary>文本行列表（每行不含换行符）。整表替换自动标脏；就地改 List 元素的调用方需自行 MarkDirty。</summary>
    public List<string> Lines
    {
        get => _lines;
        set => SetDirty(ref _lines, value);
    }
    private List<string> _lines = [""];

    /// <summary>获取/设置全部文本</summary>
    public string Text
    {
        get => string.Join("\n", Lines);
        set
        {
            Lines = string.IsNullOrEmpty(value)
                ? [""]
                : [.. value.Replace("\r\n", "\n").Split('\n')];
            MarkDirty(); // 新 List 与旧 List 内容可能相等，但引用换了、绘制结果也可能变
            // 整体替换文本：清空撤销/重做历史，防止旧操作引用已不存在的行导致越界崩溃
            _undoStack.Clear();
            _redoStack.Clear();
            CursorRow = 0;
            CursorCol = 0;
        }
    }

    // ── 光标 ──

    // 光标/滚动都影响绘制（光标行高亮、可视窗口位移），一律走 SetDirty
    /// <summary>光标行（0-based，相对于 Lines）</summary>
    public int CursorRow
    {
        get => _cursorRowIdx;
        set => SetDirty(ref _cursorRowIdx, value);
    }
    private int _cursorRowIdx;

    /// <summary>光标列（0-based，相对于当前行文本）</summary>
    public int CursorCol
    {
        get => _cursorColIdx;
        set => SetDirty(ref _cursorColIdx, value);
    }
    private int _cursorColIdx;

    // ── 滚动 ──

    /// <summary>垂直滚动偏移（行）</summary>
    public int ScrollRow
    {
        get => _scrollRow;
        set => SetDirty(ref _scrollRow, value);
    }
    private int _scrollRow;

    /// <summary>水平滚动偏移（字符列）</summary>
    public int ScrollCol
    {
        get => _scrollCol;
        set => SetDirty(ref _scrollCol, value);
    }
    private int _scrollCol;

    // ── 显示选项 ──

    /// <summary>是否显示行号</summary>
    public bool ShowLineNumbers { get; set; }

    /// <summary>占位文本（内容为空时显示）</summary>
    public string Placeholder { get; set; } = "";

    /// <summary>最大行数（0 = 不限）。超出时从顶部裁剪旧行。</summary>
    public int MaxLines { get; set; } = 0;

    /// <summary>文字自动换行列宽（0 = 不限）。超出此列宽自动折行，可视区 Width 可小于此值以实现水平滚动。</summary>
    public int MaxColumnWidth { get; set; } = 0;

    /// <summary>是否启用代码语法高亮（粘贴/输入代码时按内容自动检测语言并多色渲染）。</summary>
    public bool SyntaxHighlight { get; set; }

    // 懒缓存：文本未变不重测（渲染每帧读，避免反复扫描全文）
    private Syntax? _detectedSyntax;
    private string _detectedFor = "";

    /// <summary>按当前文本懒检测的语法（SyntaxHighlight 关闭时恒 null；文本变化才重测）。</summary>
    private Syntax? DetectedSyntax
    {
        get
        {
            if (!SyntaxHighlight) return null;
            var t = Text;
            if (_detectedFor == t) return _detectedSyntax;
            _detectedFor = t;
            _detectedSyntax = Syntax.Detect(t);
            return _detectedSyntax;
        }
    }

    // ── 样式 ──

    /// <summary>行号前景色</summary>
    public int LineNumFg { get; set; }
    /// <summary>光标行背景色</summary>
    public int CursorLineBg { get; set; }
    /// <summary>光标行前景色（反白白底时用黑字）</summary>
    public int CursorLineFg { get; set; }
    /// <summary>占位文本前景色</summary>
    public int PlaceholderFg { get; set; }

    // ── 文本选择 ──

    /// <summary>选择起始位置（行, 列），(-1, -1) = 无选择</summary>
    public int SelStartRow { get; set; } = -1;
    public int SelStartCol { get; set; } = -1;

    /// <summary>选择结束位置（行, 列）</summary>
    public int SelEndRow { get; set; } = -1;
    public int SelEndCol { get; set; } = -1;

    /// <summary>是否有活动的文本选择</summary>
    public override bool HasSelection =>
        SelStartRow >= 0 && SelEndRow >= 0 &&
        !(SelStartRow == SelEndRow && SelStartCol == SelEndCol);

    protected override void ClearSelection()
    {
        SelStartRow = SelEndRow = SelStartCol = SelEndCol = -1;
    }

    protected override void StartSelection()
    {
        SelStartRow = CursorRow;
        SelStartCol = CursorCol;
        SelEndRow = CursorRow;
        SelEndCol = CursorCol;
    }

    protected override void ExtendSelection()
    {
        SelEndRow = CursorRow;
        SelEndCol = CursorCol;
    }

    // ── 撤销 / 重做 ──

    private record struct EditAction(
        char Type,      // 'I'=insert, 'D'=delete, 'S'=split-line, 'J'=join-lines
        int Row,        // 操作所在行
        int Col,        // 操作所在列
        string Text     // 插入/删除的文本
    );

    private readonly Stack<EditAction> _undoStack = new();
    private readonly Stack<EditAction> _redoStack = new();

    private void RecordEdit(char type, int row, int col, string text)
    {
        if (string.IsNullOrEmpty(text) && type is 'I' or 'D') return;
        _undoStack.Push(new EditAction(type, row, col, text));
        TrimStack(_undoStack, MaxUndoHistory);
        _redoStack.Clear();
    }

    protected override void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        _redoStack.Push(action);
        ClearSelection();
        // 兜底：操作引用的行已被裁剪/重置 → 跳过，避免越界崩溃
        if (action.Row < 0 || action.Row >= Lines.Count) { NotifyChange(); return; }

        switch (action.Type)
        {
            case 'I':
                // 撤销插入 = 删除
                DeleteTextAt(action.Row, action.Col, action.Text);
                CursorRow = action.Row;
                CursorCol = action.Col;
                break;
            case 'D':
                // 撤销删除 = 插入
                InsertTextAt(action.Row, action.Col, action.Text);
                CursorRow = action.Row + action.Text.Count(c => c == '\n');
                int lastLineStart = action.Text.LastIndexOf('\n');
                CursorCol = lastLineStart >= 0 ? action.Text.Length - lastLineStart - 1 : action.Col + action.Text.Length;
                break;
            case 'S':
                // 撤销拆行 = 合并行（去掉自动缩进，避免重复空格污染文本）
                JoinLinesAt(action.Row, action.Text);
                CursorRow = action.Row;
                CursorCol = action.Col;
                break;
            case 'J':
                // 撤销合行 = 拆行
                SplitLineAt(action.Row, action.Col);
                CursorRow = action.Row + 1;
                CursorCol = 0;
                break;
        }
        NotifyChange();
    }

    protected override void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        _undoStack.Push(action);
        ClearSelection();
        if (action.Row < 0 || action.Row >= Lines.Count) { NotifyChange(); return; }

        switch (action.Type)
        {
            case 'I':
                InsertTextAt(action.Row, action.Col, action.Text);
                CursorRow = action.Row + action.Text.Count(c => c == '\n');
                int lastLineStart = action.Text.LastIndexOf('\n');
                CursorCol = lastLineStart >= 0 ? action.Text.Length - lastLineStart - 1 : action.Col + action.Text.Length;
                break;
            case 'D':
                DeleteTextAt(action.Row, action.Col, action.Text);
                CursorRow = action.Row;
                CursorCol = action.Col;
                break;
            case 'S':
                SplitLineAt(action.Row, action.Col);
                // 重新应用拆行缩进（InsertNewLine 时加上的），redo 恢复完整状态
                if (action.Text.Length > 1)
                {
                    var indent = action.Text[1..];
                    if (action.Row + 1 < Lines.Count && !Lines[action.Row + 1].StartsWith(indent))
                        Lines[action.Row + 1] = indent + Lines[action.Row + 1];
                }
                CursorRow = action.Row + 1;
                CursorCol = action.Col;
                break;
            case 'J':
                JoinLinesAt(action.Row);
                CursorRow = action.Row;
                CursorCol = action.Col;
                break;
        }
        NotifyChange();
    }

    /// <summary>在指定位置插入文本（支持多行）</summary>
    private void InsertTextAt(int row, int col, string text)
    {
        var parts = text.Replace("\r\n", "\n").Split('\n');
        var line = SafeLine(row);
        if (parts.Length == 1)
        {
            // 单行插入
            Lines[row] = line[..Math.Min(col, line.Length)] + parts[0] + line[Math.Min(col, line.Length)..];
        }
        else
        {
            // 多行插入
            var left = line[..Math.Min(col, line.Length)];
            var right = line[Math.Min(col, line.Length)..];
            Lines[row] = left + parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                Lines.Insert(row + i, parts[i]);
            }
            Lines.Insert(row + parts.Length - 1, parts[^1] + right);
        }
    }

    /// <summary>在指定位置删除文本（支持多行）</summary>
    private void DeleteTextAt(int row, int col, string text)
    {
        var parts = text.Replace("\r\n", "\n").Split('\n');
        if (parts.Length == 1)
        {
            // 单行删除
            var line = SafeLine(row);
            int start = Math.Min(col, line.Length);
            int end = Math.Min(start + text.Length, line.Length);
            Lines[row] = line[..start] + line[end..];
        }
        else
        {
            // 多行删除：从 row 行的 col 位置开始，跨越多行
            var firstLine = SafeLine(row);
            var lastLine = SafeLine(row + parts.Length - 1);
            int start = Math.Min(col, firstLine.Length);
            string remainingLastLine = lastLine[Math.Min(parts[^1].Length, lastLine.Length)..];
            Lines[row] = firstLine[..start] + remainingLastLine;
            // 删除中间行
            for (int i = parts.Length - 1; i >= 1; i--)
                Lines.RemoveAt(row + i);
        }
    }

    /// <summary>在指定行拆分（类似回车）</summary>
    private void SplitLineAt(int row, int col)
    {
        var line = SafeLine(row);
        Lines[row] = line[..Math.Min(col, line.Length)];
        Lines.Insert(row + 1, line[Math.Min(col, line.Length)..]);
    }

    /// <summary>合并指定行和下一行。</summary>
    /// <param name="undoText">撤销拆行时传入记录的 "\n"+缩进，从下一行头去掉自动缩进避免重复。</param>
    private void JoinLinesAt(int row, string? undoText = null)
    {
        if (row >= Lines.Count - 1) return;
        var cur = SafeLine(row);
        var next = SafeLine(row + 1);
        if (undoText != null && undoText.Length > 1)
        {
            var indent = undoText[1..]; // 去掉前导 "\n"
            if (next.StartsWith(indent)) next = next[indent.Length..];
        }
        Lines[row] = cur + next;
        Lines.RemoveAt(row + 1);
    }

    public TuiTextArea()
    {
        Height = 5;
        Width = 60;
        LineNumFg = TuiTheme.Current.TextAreaLineNumFg;
        CursorLineBg = TuiTheme.Current.TextAreaCursorLineBg;
        CursorLineFg = TuiTheme.Current.TextAreaCursorLineFg;
        PlaceholderFg = TuiTheme.Current.TextAreaPlaceholderFg;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visRows = Height;
        if (visRows <= 0) return;

        // 确保光标可见
        EnsureCursorVisible(visRows);

        int lineNumW = ShowLineNumbers ? (Lines.Count > 0 ? Lines.Count.ToString().Length + 1 : 3) : 0;
        int textW = Math.Max(1, Width - lineNumW);

        // 计算规范化的选择范围（确保 start ≤ end）
        int selStartR = -1, selStartC = -1, selEndR = -1, selEndC = -1;
        if (HasSelection)
        {
            NormalizeSelection(out selStartR, out selStartC, out selEndR, out selEndC);
        }

        // 渲染每一行
        for (int i = 0; i < visRows; i++)
        {
            int lineIdx = ScrollRow + i;
            int screenRow = absY + i;

            bool isCursorLine = lineIdx == CursorRow;

            // 行号
            if (ShowLineNumbers && lineIdx < Lines.Count)
            {
                var numStr = (lineIdx + 1).ToString().PadLeft(lineNumW - 1) + " ";
                var rb = new RenderBuffer();
                rb.Write(screenRow, absX, numStr, fg: LineNumFg);
                sb.Append(rb.ToString());
            }

            // 文本内容
            if (lineIdx < Lines.Count)
            {
                var line = SafeLine(lineIdx);
                int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                       : isCursorLine ? (CursorLineFg > 0 ? CursorLineFg : TuiTheme.Current.TextAreaFg)
                       : Focused ? (FocusedFg > 0 ? FocusedFg : TuiTheme.Current.TextAreaFg)
                       : (Fg > 0 ? Fg : TuiTheme.Current.TextAreaFg);
                int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                       : isCursorLine ? CursorLineBg : (Bg > 0 ? Bg : 0);

                // 截取可见部分
                int displayStart = ScrollCol;
                var fullDisplay = line.Length > displayStart ? line[displayStart..] : "";
                if (AnsiHelper.DisplayWidth(fullDisplay) > textW)
                    fullDisplay = AnsiHelper.TruncateByWidth(fullDisplay, textW);
                int vw = AnsiHelper.DisplayWidth(fullDisplay);
                var pad = Math.Max(0, textW - vw);

                // 检查此行是否在选择范围内
                bool hasSelOnThisLine = HasSelection && lineIdx >= selStartR && lineIdx <= selEndR;

                if (hasSelOnThisLine)
                {
                    int lineSelStartC = (lineIdx == selStartR) ? selStartC : 0;
                    int lineSelEndC = (lineIdx == selEndR) ? selEndC : line.Length;

                    // 转为可见区域的偏移
                    int visSelStart = Math.Max(0, lineSelStartC - ScrollCol);
                    int visSelEnd = Math.Max(0, lineSelEndC - ScrollCol);
                    int visLen = fullDisplay.Length;

                    int visSelStartClamped = Math.Clamp(visSelStart, 0, visLen);
                    int visSelEndClamped = Math.Clamp(visSelEnd, 0, visLen);

                    // 前段
                    if (visSelStartClamped > 0)
                    {
                        WriteAt(sb, screenRow, absX + lineNumW, fullDisplay[..visSelStartClamped], fg, bg);
                    }
                    // 选中段（反向色）
                    if (visSelEndClamped > visSelStartClamped)
                    {
                        var selPart = fullDisplay[visSelStartClamped..visSelEndClamped];
                        int selX = absX + lineNumW + AnsiHelper.DisplayWidth(fullDisplay[..visSelStartClamped]);
                        WriteAt(sb, screenRow, selX, selPart, bg > 0 ? bg : 7, fg > 0 ? fg : 0);
                    }
                    // 后段
                    if (visSelEndClamped < visLen)
                    {
                        var postPart = fullDisplay[visSelEndClamped..];
                        int postX = absX + lineNumW + AnsiHelper.DisplayWidth(fullDisplay[..visSelEndClamped]);
                        WriteAt(sb, screenRow, postX, postPart, fg, bg);
                    }
                    // 填充
                    if (pad > 0)
                    {
                        WriteAt(sb, screenRow, absX + lineNumW + vw, new string(' ', pad), fg, bg);
                    }
                }
                else
                {
                    // 语法高亮：按 token 逐段渲染（token 色优先，默认色用本行 fg）。选择/光标行仍单色。
                    var syn = DetectedSyntax;
                    if (syn != null)
                    {
                        int x = absX + lineNumW;
                        foreach (var (t, c) in syn.Tokenize(fullDisplay))
                        {
                            if (string.IsNullOrEmpty(t)) continue;
                            WriteAt(sb, screenRow, x, t, c > 0 ? c : fg, bg);
                            x += AnsiHelper.DisplayWidth(t);
                        }
                        if (pad > 0) WriteAt(sb, screenRow, x, new string(' ', pad), fg, bg);
                    }
                    else
                    {
                        // 无高亮：整行单色
                        WriteAt(sb, screenRow, absX + lineNumW, fullDisplay + new string(' ', pad), fg, bg);
                    }
                }
            }
            else if (lineIdx == 0 && string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
            {
                // 占位文本
                var ph = Placeholder;
                if (AnsiHelper.DisplayWidth(ph) > textW)
                    ph = AnsiHelper.TruncateByWidth(ph, textW);
                WriteAt(sb, screenRow, absX + lineNumW, ph, PlaceholderFg, 0);
            }

            // 光标指示
            if (IsCursorOwner && isCursorLine && CursorCol >= ScrollCol)
            {
                var line = SafeLine(lineIdx);
                var preCursorText = line.Length > ScrollCol
                    ? line[ScrollCol..Math.Min(CursorCol, line.Length)]
                    : "";
                int cursorVisualOffset = AnsiHelper.DisplayWidth(preCursorText);
                int cursorScreenCol = absX + lineNumW + cursorVisualOffset;
                if (cursorScreenCol < absX + Width && cursorScreenCol >= absX + lineNumW)
                {
                    RecordCursorPos(screenRow, cursorScreenCol);
                }
            }
            else if (IsCursorOwner && !isCursorLine)
            {
                _showCursor = false;
            }
        }
    }

    /// <summary>规范化选择范围（确保 Start ≤ End）</summary>
    private void NormalizeSelection(out int startR, out int startC, out int endR, out int endC)
    {
        if (SelStartRow < SelEndRow || (SelStartRow == SelEndRow && SelStartCol <= SelEndCol))
        {
            startR = SelStartRow; startC = SelStartCol;
            endR = SelEndRow; endC = SelEndCol;
        }
        else
        {
            startR = SelEndRow; startC = SelEndCol;
            endR = SelStartRow; endC = SelStartCol;
        }
    }

    /// <summary>获取选中的文本</summary>
    public override string? GetSelectedText()
    {
        if (!HasSelection) return null;
        NormalizeSelection(out int sr, out int sc, out int er, out int ec);
        if (sr == er)
        {
            var line = SafeLine(sr);
            return line[Math.Min(sc, line.Length)..Math.Min(ec, line.Length)];
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(SafeLine(sr)[Math.Min(sc, SafeLine(sr).Length)..]);
            for (int r = sr + 1; r < er; r++)
                sb.AppendLine(SafeLine(r));
            sb.Append(SafeLine(er)[..Math.Min(ec, SafeLine(er).Length)]);
            return sb.ToString();
        }
    }

    /// <summary>删除选中文本</summary>
    protected override void DeleteSelection()
    {
        if (!HasSelection) return;
        NormalizeSelection(out int sr, out int sc, out int er, out int ec);

        var deleted = GetSelectedText() ?? "";
        if (string.IsNullOrEmpty(deleted) && sr == er && sc == ec) { ClearSelection(); return; }

        RecordEdit('D', sr, sc, deleted);

        if (sr == er)
        {
            var line = SafeLine(sr);
            Lines[sr] = line[..Math.Min(sc, line.Length)] + line[Math.Min(ec, line.Length)..];
            CursorRow = sr;
            CursorCol = sc;
        }
        else
        {
            var firstLine = SafeLine(sr);
            var lastLine = SafeLine(er);
            Lines[sr] = firstLine[..Math.Min(sc, firstLine.Length)] + lastLine[Math.Min(ec, lastLine.Length)..];
            for (int i = er; i > sr; i--)
                Lines.RemoveAt(i);
            CursorRow = sr;
            CursorCol = sc;
        }
        ClearSelection();
        NotifyChange();
    }

    // ── 光标移动原语 ──

    protected override void MoveCursorLeft()  => MoveCursorCol(-1);
    protected override void MoveCursorRight() => MoveCursorCol(1);
    protected override void MoveCursorUp()    => MoveCursorRow(-1);
    protected override void MoveCursorDown()  => MoveCursorRow(1);
    protected override void MoveCursorHome()  { CursorCol = 0; }
    protected override void MoveCursorEnd()   { CursorCol = SafeLine(CursorRow).Length; }
    protected override void MoveCursorPageUp()
    {
        ScrollRow = Math.Max(0, ScrollRow - Height);
        CursorRow = Math.Max(0, CursorRow - Height);
        MarkDirty(); // 滚动偏移 + 光标行都变了，需重绘
    }
    protected override void MoveCursorPageDown()
    {
        ScrollRow = Math.Min(Math.Max(0, Lines.Count - 1), ScrollRow + Height);
        CursorRow = Math.Min(Lines.Count - 1, CursorRow + Height);
        MarkDirty();
    }

    protected override void JumpToSelStart()
    {
        NormalizeSelection(out int sr, out int sc, out _, out _);
        CursorRow = sr; CursorCol = sc;
    }
    protected override void JumpToSelEnd()
    {
        NormalizeSelection(out _, out _, out int er, out int ec);
        CursorRow = er; CursorCol = ec;
    }

    // ── 编辑原语 ──

    protected override void InsertChar(char ch)
    {
        var line = SafeLine(CursorRow);
        int pos = Math.Min(CursorCol, line.Length);
        Lines[CursorRow] = line[..pos] + ch + line[pos..];
        RecordEdit('I', CursorRow, CursorCol, ch.ToString());
        CursorCol++;
        WrapCurrentLine();
        TrimExcessLines();
        NotifyChange();
    }

    /// <summary>插入多字符文本（外部调用，不经过键盘）</summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ClearSelection();
        var processed = ApplyColumnWrap(text);
        int startRow = CursorRow, startCol = CursorCol;
        InsertTextAt(startRow, startCol, processed);
        RecordEdit('I', startRow, startCol, processed);
        int newLines = processed.Count(c => c == '\n');
        CursorRow = startRow + newLines;
        int lastNL = processed.LastIndexOf('\n');
        CursorCol = lastNL >= 0 ? processed.Length - lastNL - 1 : startCol + processed.Length;
        TrimExcessLines();
        NotifyChange();
    }

    protected override void DeleteCharBefore()
    {
        if (CursorCol > 0)
        {
            var line = SafeLine(CursorRow);
            int delLen = 1;
            if (CursorCol <= line.Length)
            {
                delLen = CursorCol >= 2 && char.IsHighSurrogate(line[CursorCol - 2]) && char.IsLowSurrogate(line[CursorCol - 1]) ? 2 : 1;
                var ch = line.Substring(CursorCol - delLen, delLen);
                Lines[CursorRow] = line[..(CursorCol - delLen)] + line[CursorCol..];
                RecordEdit('D', CursorRow, CursorCol - delLen, ch);
            }
            CursorCol -= delLen;
            NotifyChange();
        }
        else if (CursorRow > 0)
        {
            // 合并到上一行
            var prev = SafeLine(CursorRow - 1);
            var cur = SafeLine(CursorRow);
            CursorCol = prev.Length;
            Lines[CursorRow - 1] = prev + cur;
            RecordEdit('J', CursorRow - 1, prev.Length, "\n");
            Lines.RemoveAt(CursorRow);
            CursorRow--;
            NotifyChange();
        }
    }

    protected override void DeleteCharAfter()
    {
        var line = SafeLine(CursorRow);
        if (CursorCol < line.Length)
        {
            int delLen = CursorCol + 1 < line.Length && char.IsHighSurrogate(line[CursorCol]) && char.IsLowSurrogate(line[CursorCol + 1]) ? 2 : 1;
            var ch = line.Substring(CursorCol, delLen);
            Lines[CursorRow] = line[..CursorCol] + line[(CursorCol + delLen)..];
            RecordEdit('D', CursorRow, CursorCol, ch);
            NotifyChange();
        }
        else if (CursorRow < Lines.Count - 1)
        {
            // 合并下一行
            var next = SafeLine(CursorRow + 1);
            Lines[CursorRow] = line + next;
            RecordEdit('J', CursorRow, CursorCol, "\n");
            Lines.RemoveAt(CursorRow + 1);
            NotifyChange();
        }
    }

    protected override void DeleteWordBefore()
    {
        var line = SafeLine(CursorRow);
        int pos = CursorCol;
        while (pos > 0 && line[pos - 1] == ' ') pos--;
        while (pos > 0 && line[pos - 1] != ' ') pos--;
        var deleted = line[pos..CursorCol];
        Lines[CursorRow] = line[..pos] + line[CursorCol..];
        RecordEdit('D', CursorRow, pos, deleted);
        CursorCol = pos;
        NotifyChange();
    }

    protected override void DeleteToLineEnd()
    {
        var line = SafeLine(CursorRow);
        int col = Math.Min(CursorCol, line.Length);
        var deleted = line[col..];
        if (deleted.Length == 0) return;
        Lines[CursorRow] = line[..col];
        RecordEdit('D', CursorRow, col, deleted);
        NotifyChange();
    }

    /// <summary>Ctrl+Enter = 换行（多行输入；单行 TuiInput 走基类 Ctrl+Enter 提交）。
    /// 对话区 Enter 由 ChatScreen 拦截为提交，Ctrl+Enter 走这里实现多行换行。</summary>
    protected override bool HandleCtrlKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Enter)
        {
            InsertNewLine();
            return true;
        }
        return base.HandleCtrlKey(key);
    }

    protected override void InsertNewLine()
    {
        var line = SafeLine(CursorRow);
        var indent = GetIndent(line);
        Lines[CursorRow] = line[..Math.Min(CursorCol, line.Length)];
        Lines.Insert(CursorRow + 1, indent + line[Math.Min(CursorCol, line.Length)..]);
        RecordEdit('S', CursorRow, CursorCol, "\n" + indent);
        CursorRow++;
        CursorCol = indent.Length;
        TrimExcessLines();
        NotifyChange();
    }

    protected override void SelectAll()
    {
        SelStartRow = 0; SelStartCol = 0;
        SelEndRow = Lines.Count - 1;
        SelEndCol = SafeLine(Lines.Count - 1).Length;
        CursorRow = SelEndRow;
        CursorCol = SelEndCol;
    }

    protected override void PasteText(string text)
    {
        var processed = ApplyColumnWrap(text);
        int startRow = CursorRow, startCol = CursorCol;
        InsertTextAt(startRow, startCol, processed);
        int newLines = processed.Count(c => c == '\n');
        CursorRow = startRow + newLines;
        int lastNL = processed.LastIndexOf('\n');
        CursorCol = lastNL >= 0 ? processed.Length - lastNL - 1 : startCol + processed.Length;
        RecordEdit('I', startRow, startCol, processed);
        TrimExcessLines();
        NotifyChange();
    }

    protected override string GetText() => Text;

    // ── 自动换行 & 行数裁剪 ──

    /// <summary>对当前光标行检测是否需要按 MaxColumnWidth 折行，并执行折行。</summary>
    private void WrapCurrentLine()
    {
        if (MaxColumnWidth <= 0) return;
        var line = SafeLine(CursorRow);
        if (line.Length <= MaxColumnWidth) return;
        // 在 MaxColumnWidth 附近找空格作为折行点
        int breakCol = MaxColumnWidth;
        for (int i = MaxColumnWidth; i > 0; i--)
        {
            if (i < line.Length && line[i] == ' ')
            {
                breakCol = i + 1; // 在空格后断开
                break;
            }
        }
        // 如果找不到空格就直接在 MaxColumnWidth 处硬断
        if (breakCol >= line.Length) return;
        breakCol = SafeBreakCol(line, breakCol);
        var left = line[..breakCol];
        var right = line[breakCol..].TrimStart();
        Lines[CursorRow] = left;
        Lines.Insert(CursorRow + 1, right);
        // 调整光标
        if (CursorCol >= breakCol)
        {
            CursorRow++;
            CursorCol -= breakCol;
        }
    }

    /// <summary>折行点若落在代理对中间（高代理+低代理跨列），回退 1 列避免切出 U+FFFD。</summary>
    private static int SafeBreakCol(string line, int breakCol)
    {
        if (breakCol > 0 && breakCol < line.Length
            && char.IsHighSurrogate(line[breakCol - 1]) && char.IsLowSurrogate(line[breakCol]))
            return breakCol - 1;
        return breakCol;
    }

    /// <summary>对多行文本按 MaxColumnWidth 逐行折行，返回折行后的文本。</summary>
    private string ApplyColumnWrap(string text)
    {
        if (MaxColumnWidth <= 0) return text;
        var parts = text.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        foreach (var part in parts)
        {
            if (part.Length <= MaxColumnWidth)
            {
                result.Add(part);
            }
            else
            {
                // 逐段折行
                var remaining = part;
                while (remaining.Length > MaxColumnWidth)
                {
                    int breakCol = MaxColumnWidth;
                    for (int i = MaxColumnWidth; i > 0; i--)
                    {
                        if (i < remaining.Length && remaining[i] == ' ')
                        {
                            breakCol = i + 1;
                            break;
                        }
                    }
                    breakCol = SafeBreakCol(remaining, breakCol);
                    result.Add(remaining[..breakCol]);
                    remaining = remaining[breakCol..].TrimStart();
                }
                if (remaining.Length > 0)
                    result.Add(remaining);
            }
        }
        return string.Join("\n", result);
    }

    /// <summary>行数超出 MaxLines 时从顶部裁剪旧行，同步调整光标行。</summary>
    private void TrimExcessLines()
    {
        if (MaxLines <= 0 || Lines.Count <= MaxLines) return;
        int toRemove = Lines.Count - MaxLines;
        for (int i = 0; i < toRemove; i++)
            Lines.RemoveAt(0);
        CursorRow = Math.Max(0, CursorRow - toRemove);
        // 也调整滚动偏移
        ScrollRow = Math.Max(0, ScrollRow - toRemove);
        // 顶部行被裁剪：撤销/重做栈中引用被删行的操作丢弃（行已不存在），
        // 其余行号整体上移 toRemove，避免 undo 索引到不存在的行崩溃
        ShiftStack(_undoStack, toRemove);
        ShiftStack(_redoStack, toRemove);
    }

    /// <summary>裁剪后修正撤销/重做栈：丢弃引用已删除行的操作，其余行号上移。</summary>
    private static void ShiftStack(Stack<EditAction> stack, int shift)
    {
        var keep = new List<EditAction>();
        while (stack.Count > 0)
        {
            var a = stack.Pop();
            if (a.Row >= shift) keep.Add(a with { Row = a.Row - shift });
        }
        for (int i = keep.Count - 1; i >= 0; i--)
            stack.Push(keep[i]);
    }

    private static string GetIndent(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return line[..i];
    }

    // ── 光标移动辅助 ──

    private void MoveCursorCol(int delta)
    {
        var line = SafeLine(CursorRow);
        int newCol = Math.Clamp(CursorCol + delta, 0, line.Length);
        // 光标不落在代理对中间（emoji/CJK 扩展 B）
        if (newCol > 0 && newCol < line.Length
            && char.IsHighSurrogate(line[newCol - 1]) && char.IsLowSurrogate(line[newCol]))
            newCol += delta > 0 ? 1 : -1;
        CursorCol = newCol;
    }

    private void MoveCursorRow(int delta)
    {
        CursorRow = Math.Clamp(CursorRow + delta, 0, Lines.Count - 1);
        CursorCol = Math.Min(CursorCol, SafeLine(CursorRow).Length);
        MarkDirty(); // 光标行变了，光标行高亮（CursorLineBg）需随光标移动重绘
    }

    private void EnsureCursorVisible(int visRows)
    {
        if (CursorRow < ScrollRow) ScrollRow = CursorRow;
        if (CursorRow >= ScrollRow + visRows) ScrollRow = CursorRow - visRows + 1;
        ScrollRow = Math.Clamp(ScrollRow, 0, Math.Max(0, Lines.Count - visRows));
    }

    // ── 工具 ──

    private string SafeLine(int idx)
    {
        if (idx < 0 || idx >= Lines.Count) return "";
        return Lines[idx];
    }

    private void NotifyChange()
    {
        NotifyChanged();
    }

    /// <summary>
    /// 计算并设置光标屏幕坐标（多行 CJK 感知 + 滚动偏移）。
    /// 不依赖 OnRender 调用，保证即使控件未被重绘光标位置也正确。
    /// </summary>
    protected override void GotoCursorPos()
    {
        if (!IsCursorOwner) return;

        var absX = _lastAbsX;
        var absY = _lastAbsY;
        int lineNumW = ShowLineNumbers ? (Lines.Count > 0 ? Lines.Count.ToString().Length + 1 : 3) : 0;

        // 确保光标在当前视口内
        int visRows = Height;
        EnsureCursorVisible(visRows);

        // 计算光标所在行在屏幕上的位置
        int screenRow = absY + (CursorRow - ScrollRow);
        int textW = Math.Max(1, Width - lineNumW);

        var line = SafeLine(CursorRow);
        int displayStart = ScrollCol;
        var preCursorText = line.Length > displayStart
            ? line[displayStart..Math.Min(CursorCol, line.Length)]
            : "";
        int cursorVisualOffset = AnsiHelper.DisplayWidth(preCursorText);
        int cursorScreenCol = absX + lineNumW + cursorVisualOffset;

        // 保持在可视范围内
        if (cursorScreenCol < absX + lineNumW)
            cursorScreenCol = absX + lineNumW;
        if (cursorScreenCol >= absX + Width)
            cursorScreenCol = absX + Width - 1;

        _cursorRow = Math.Clamp(screenRow, absY, absY + visRows - 1);
        _cursorCol = cursorScreenCol;
        _showCursor = true;
    }

    /// <summary>点击定位光标（多行 + CJK 列换算）+ 聚焦。</summary>
    public override bool OnMouse(InputEvent ev)
    {
        if (!MouseInBounds(ev, out int relX, out int relY)) return false;
        if (!ev.MouseLeft) return false;

        Focused = true;

        // 行：视口内相对行 → 数据行
        int row = ScrollRow + relY;
        if (row >= 0 && row < Lines.Count)
        {
            // 列：内容区起点在 absX + lineNumW，文本显示自 ScrollCol 字符起（与 OnRender 一致）
            int lineNumW = ShowLineNumbers ? (Lines.Count > 0 ? Lines.Count.ToString().Length + 1 : 3) : 0;
            int textX = Math.Max(0, relX - lineNumW);
            var line = SafeLine(row);
            var visible = line.Length > ScrollCol ? line[ScrollCol..] : "";
            CursorRow = row;
            CursorCol = ScrollCol + VisualToCharCol(visible, textX);
        }
        else
        {
            // 点击可视区空白：光标落到最接近的边界行末尾
            CursorRow = Math.Clamp(row, 0, Math.Max(0, Lines.Count - 1));
            CursorCol = SafeLine(CursorRow).Length;
        }

        ClearSelection();
        MarkDirty();
        return true;
    }
}
