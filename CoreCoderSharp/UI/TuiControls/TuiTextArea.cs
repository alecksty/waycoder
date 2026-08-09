using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 多行文本编辑控件 —— 支持光标自由移动、自动换行、滚动、文本选择、撤销重做。
/// 可嵌入任何 View 容器中。
/// </summary>
public class TuiTextArea : TuiControl
{
    // ── 文本缓冲 ──

    /// <summary>文本行列表（每行不含换行符）</summary>
    public List<string> Lines { get; set; } = [""];

    /// <summary>获取/设置全部文本</summary>
    public string Text
    {
        get => string.Join("\n", Lines);
        set => Lines = string.IsNullOrEmpty(value)
            ? [""]
            : [.. value.Replace("\r\n", "\n").Split('\n')];
    }

    // ── 光标 ──

    /// <summary>光标行（0-based，相对于 Lines）</summary>
    public int CursorRow { get; set; }

    /// <summary>光标列（0-based，相对于当前行文本）</summary>
    public int CursorCol { get; set; }

    // ── 滚动 ──

    /// <summary>垂直滚动偏移（行）</summary>
    public int ScrollRow { get; set; }

    /// <summary>水平滚动偏移（字符列）</summary>
    public int ScrollCol { get; set; }

    // ── 显示选项 ──

    /// <summary>是否显示行号</summary>
    public bool ShowLineNumbers { get; set; }

    /// <summary>是否只读</summary>
    public bool ReadOnly { get; set; }

    /// <summary>占位文本（内容为空时显示）</summary>
    public string Placeholder { get; set; } = "";

    // ── 事件 ──

    /// <summary>文本变化时触发</summary>
    public Action? OnTextChanged { get; set; }

    /// <summary>Ctrl+Enter 提交时触发</summary>
    public Action<string>? OnSubmit { get; set; }

    // ── 样式 ──

    /// <summary>行号前景色</summary>
    public int LineNumFg { get; set; }
    /// <summary>光标行背景色</summary>
    public int CursorLineBg { get; set; }
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
    public bool HasSelection =>
        SelStartRow >= 0 && SelEndRow >= 0 &&
        !(SelStartRow == SelEndRow && SelStartCol == SelEndCol);

    /// <summary>清除选择</summary>
    private void ClearSelection()
    {
        SelStartRow = SelEndRow = SelStartCol = SelEndCol = -1;
    }

    /// <summary>从光标位置开始选择</summary>
    private void StartSelection()
    {
        SelStartRow = CursorRow;
        SelStartCol = CursorCol;
        SelEndRow = CursorRow;
        SelEndCol = CursorCol;
    }

    /// <summary>更新选择终点</summary>
    private void ExtendSelection()
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
    private const int MaxUndoHistory = 100;

    private void RecordEdit(char type, int row, int col, string text)
    {
        if (string.IsNullOrEmpty(text) && type is 'I' or 'D') return;
        _undoStack.Push(new EditAction(type, row, col, text));
        TrimStack(_undoStack, MaxUndoHistory);
        _redoStack.Clear();
    }

    private static void TrimStack(Stack<EditAction> stack, int max)
    {
        if (stack.Count <= max) return;
        var arr = stack.ToArray();
        stack.Clear();
        for (int i = arr.Length - 2; i >= 0; i--) stack.Push(arr[i]);
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        _redoStack.Push(action);
        ClearSelection();

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
                // 撤销拆行 = 合并行
                JoinLinesAt(action.Row);
                CursorRow = action.Row;
                CursorCol = action.Col;
                break;
            case 'J':
                // 撤销合行 = 拆行
                SplitLineAt(action.Row, action.Col);
                CursorRow = action.Row + 1;
                CursorCol = action.Col;
                break;
        }
        NotifyChange();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        _undoStack.Push(action);
        ClearSelection();

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
                CursorRow = action.Row + 1;
                CursorCol = action.Col; // indent length
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

    /// <summary>合并指定行和下一行</summary>
    private void JoinLinesAt(int row)
    {
        if (row >= Lines.Count - 1) return;
        var cur = SafeLine(row);
        var next = SafeLine(row + 1);
        Lines[row] = cur + next;
        Lines.RemoveAt(row + 1);
    }

    public override bool HasCursor => true;

    public TuiTextArea()
    {
        Height = 5;
        Width = 60;
        LineNumFg = TuiTheme.Current.TextAreaLineNumFg;
        CursorLineBg = TuiTheme.Current.TextAreaCursorLineBg;
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
                       : Focused ? (FocusedFg > 0 ? FocusedFg : TuiTheme.Current.TextAreaFg)
                       : (Fg > 0 ? Fg : TuiTheme.Current.TextAreaFg);
                int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                       : isCursorLine ? CursorLineBg : (Bg > 0 ? Bg : 0);

                // 截取可见部分
                int displayStart = ScrollCol;
                var fullDisplay = line.Length > displayStart ? line[displayStart..] : "";
                if (TuiHelper.DisplayWidth(fullDisplay) > textW)
                    fullDisplay = TuiHelper.TruncateByWidth(fullDisplay, textW);
                int vw = TuiHelper.DisplayWidth(fullDisplay);
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
                        int selX = absX + lineNumW + TuiHelper.DisplayWidth(fullDisplay[..visSelStartClamped]);
                        WriteAt(sb, screenRow, selX, selPart, bg > 0 ? bg : 7, fg > 0 ? fg : 0);
                    }
                    // 后段
                    if (visSelEndClamped < visLen)
                    {
                        var postPart = fullDisplay[visSelEndClamped..];
                        int postX = absX + lineNumW + TuiHelper.DisplayWidth(fullDisplay[..visSelEndClamped]);
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
                    // 无选择：正常渲染
                    WriteAt(sb, screenRow, absX + lineNumW, fullDisplay + new string(' ', pad), fg, bg);
                }
            }
            else if (lineIdx == Lines.Count && string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
            {
                // 占位文本
                var ph = Placeholder;
                if (TuiHelper.DisplayWidth(ph) > textW)
                    ph = TuiHelper.TruncateByWidth(ph, textW);
                WriteAt(sb, screenRow, absX + lineNumW, ph, PlaceholderFg, 0);
            }

            // 光标指示
            if (IsCursorOwner && isCursorLine && CursorCol >= ScrollCol)
            {
                var line = SafeLine(lineIdx);
                var preCursorText = line.Length > ScrollCol
                    ? line[ScrollCol..Math.Min(CursorCol, line.Length)]
                    : "";
                int cursorVisualOffset = TuiHelper.DisplayWidth(preCursorText);
                int cursorScreenCol = absX + lineNumW + cursorVisualOffset;
                if (cursorScreenCol < absX + Width && cursorScreenCol >= absX + lineNumW)
                {
                    _cursorRow = screenRow;
                    _cursorCol = cursorScreenCol;
                    _showCursor = true;
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
    public string? GetSelectedText()
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
    private void DeleteSelection()
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

    // ── 输入处理 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !Focused || ReadOnly) return false;

        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        // Ctrl 组合键
        if (ctrl)
            return HandleCtrlKey(key);

        // Shift + 方向键（选择扩展）
        if (shift)
        {
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (!HasSelection) StartSelection();
                    MoveCursorCol(-1);
                    ExtendSelection();
                    return true;
                case ConsoleKey.RightArrow:
                    if (!HasSelection) StartSelection();
                    MoveCursorCol(1);
                    ExtendSelection();
                    return true;
                case ConsoleKey.UpArrow:
                    if (!HasSelection) StartSelection();
                    MoveCursorRow(-1);
                    ExtendSelection();
                    return true;
                case ConsoleKey.DownArrow:
                    if (!HasSelection) StartSelection();
                    MoveCursorRow(1);
                    ExtendSelection();
                    return true;
                case ConsoleKey.Home:
                    if (!HasSelection) StartSelection();
                    CursorCol = 0;
                    ExtendSelection();
                    return true;
                case ConsoleKey.End:
                    if (!HasSelection) StartSelection();
                    CursorCol = SafeLine(CursorRow).Length;
                    ExtendSelection();
                    return true;
            }
        }

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (HasSelection)
                {
                    NormalizeSelection(out int sr, out int sc, out _, out _);
                    CursorRow = sr; CursorCol = sc;
                    ClearSelection();
                }
                else MoveCursorCol(-1);
                return true;
            case ConsoleKey.RightArrow:
                if (HasSelection)
                {
                    NormalizeSelection(out _, out _, out int er, out int ec);
                    CursorRow = er; CursorCol = ec;
                    ClearSelection();
                }
                else MoveCursorCol(1);
                return true;
            case ConsoleKey.UpArrow:
                if (HasSelection) { NormalizeSelection(out int sr2, out int sc2, out _, out _); CursorRow = sr2; CursorCol = sc2; ClearSelection(); }
                else MoveCursorRow(-1);
                return true;
            case ConsoleKey.DownArrow:
                if (HasSelection) { NormalizeSelection(out _, out _, out int er2, out int ec2); CursorRow = er2; CursorCol = ec2; ClearSelection(); }
                else MoveCursorRow(1);
                return true;
            case ConsoleKey.Home:
                CursorCol = 0; ClearSelection(); return true;
            case ConsoleKey.End:
                CursorCol = SafeLine(CursorRow).Length; ClearSelection(); return true;
            case ConsoleKey.PageUp:
                ScrollRow = Math.Max(0, ScrollRow - Height);
                CursorRow = Math.Max(0, CursorRow - Height);
                ClearSelection();
                return true;
            case ConsoleKey.PageDown:
                ScrollRow = Math.Min(Math.Max(0, Lines.Count - 1), ScrollRow + Height);
                CursorRow = Math.Min(Lines.Count - 1, CursorRow + Height);
                ClearSelection();
                return true;
            case ConsoleKey.Backspace:
                if (HasSelection) DeleteSelection();
                else DeleteBefore();
                return true;
            case ConsoleKey.Delete:
                if (HasSelection) DeleteSelection();
                else DeleteAfter();
                return true;
            case ConsoleKey.Enter:
                if (HasSelection) DeleteSelection();
                InsertNewline();
                return true;
            case ConsoleKey.Escape:
                ClearSelection();
                return true;
            default:
                if (key.KeyChar >= ' ')
                {
                    if (HasSelection) DeleteSelection();
                    InsertChar(key.KeyChar);
                    return true;
                }
                return false;
        }
    }

    private bool HandleCtrlKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.A: // 全选
                SelStartRow = 0; SelStartCol = 0;
                SelEndRow = Lines.Count - 1;
                SelEndCol = SafeLine(Lines.Count - 1).Length;
                CursorRow = SelEndRow;
                CursorCol = SelEndCol;
                return true;
            case ConsoleKey.C: // 复制
                if (HasSelection)
                {
                    var st = GetSelectedText();
                    if (!string.IsNullOrEmpty(st))
                        CopyToClipboard(st);
                }
                return true;
            case ConsoleKey.X: // 剪切
                if (HasSelection)
                {
                    var st = GetSelectedText();
                    if (!string.IsNullOrEmpty(st))
                        CopyToClipboard(st);
                    DeleteSelection();
                }
                return true;
            case ConsoleKey.V: // 粘贴
                var pasteText = GetClipboardText();
                if (!string.IsNullOrEmpty(pasteText))
                {
                    if (HasSelection) DeleteSelection();
                    InsertTextAt(CursorRow, CursorCol, pasteText);
                    int newLines = pasteText.Count(c => c == '\n');
                    CursorRow += newLines;
                    int lastNL = pasteText.LastIndexOf('\n');
                    CursorCol = lastNL >= 0 ? pasteText.Length - lastNL - 1 : CursorCol + pasteText.Length;
                    RecordEdit('I', CursorRow - newLines, lastNL >= 0 ? 0 : CursorCol - pasteText.Length, pasteText);
                    NotifyChange();
                }
                return true;
            case ConsoleKey.Z: // 撤销
                Undo();
                return true;
            case ConsoleKey.Y: // 重做
                Redo();
                return true;
            case ConsoleKey.E: // 行尾
                ClearSelection();
                CursorCol = SafeLine(CursorRow).Length;
                return true;
            case ConsoleKey.K: // 删至行尾
                {
                    ClearSelection();
                    var line = SafeLine(CursorRow);
                    int col = Math.Min(CursorCol, line.Length);
                    var deleted = line[col..];
                    Lines[CursorRow] = line[..col];
                    RecordEdit('D', CursorRow, col, deleted);
                    NotifyChange();
                    return true;
                }
            case ConsoleKey.Enter:
                ClearSelection();
                OnSubmit?.Invoke(Text);
                return true;
            case ConsoleKey.Backspace: // Ctrl+Backspace: 删一个词
                ClearSelection();
                DeleteWordBefore();
                return true;
        }
        return false;
    }

    // ── 编辑操作 ──

    private void InsertChar(char ch)
    {
        var line = SafeLine(CursorRow);
        int pos = Math.Min(CursorCol, line.Length);
        Lines[CursorRow] = line[..pos] + ch + line[pos..];
        RecordEdit('I', CursorRow, CursorCol, ch.ToString());
        CursorCol++;
        NotifyChange();
    }

    /// <summary>插入多字符文本（外部调用，不经过键盘）</summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ClearSelection();
        InsertTextAt(CursorRow, CursorCol, text);
        RecordEdit('I', CursorRow, CursorCol, text);
        int newLines = text.Count(c => c == '\n');
        CursorRow += newLines;
        int lastNL = text.LastIndexOf('\n');
        CursorCol = lastNL >= 0 ? text.Length - lastNL - 1 : CursorCol + text.Length;
        NotifyChange();
    }

    private void DeleteBefore()
    {
        ClearSelection();
        if (CursorCol > 0)
        {
            var line = SafeLine(CursorRow);
            if (CursorCol <= line.Length)
            {
                var ch = line[CursorCol - 1].ToString();
                Lines[CursorRow] = line[..(CursorCol - 1)] + line[CursorCol..];
                RecordEdit('D', CursorRow, CursorCol - 1, ch);
            }
            CursorCol--;
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

    private void DeleteAfter()
    {
        ClearSelection();
        var line = SafeLine(CursorRow);
        if (CursorCol < line.Length)
        {
            var ch = line[CursorCol].ToString();
            Lines[CursorRow] = line[..CursorCol] + line[(CursorCol + 1)..];
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

    private void InsertNewline()
    {
        var line = SafeLine(CursorRow);
        var indent = GetIndent(line);
        Lines[CursorRow] = line[..Math.Min(CursorCol, line.Length)];
        Lines.Insert(CursorRow + 1, indent + line[Math.Min(CursorCol, line.Length)..]);
        RecordEdit('S', CursorRow, CursorCol, "\n" + indent);
        CursorRow++;
        CursorCol = indent.Length;
        NotifyChange();
    }

    private void DeleteWordBefore()
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

    private static string GetIndent(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return line[..i];
    }

    // ── 光标移动 ──

    private void MoveCursorCol(int delta)
    {
        CursorCol = Math.Clamp(CursorCol + delta, 0, SafeLine(CursorRow).Length);
    }

    private void MoveCursorRow(int delta)
    {
        CursorRow = Math.Clamp(CursorRow + delta, 0, Lines.Count - 1);
        CursorCol = Math.Min(CursorCol, SafeLine(CursorRow).Length);
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
        OnTextChanged?.Invoke();
        base.MarkDirty(); // 沿 Parent 链传播脏标记到屏幕
    }

    /// <summary>复制文本到系统剪贴板（尽力而为）</summary>
    private static void CopyToClipboard(string text)
    {
        ClipboardHelper.SetText(text);
    }

    /// <summary>从系统剪贴板获取文本（尽力而为）</summary>
    private static string? GetClipboardText()
    {
        var text = ClipboardHelper.GetText();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
