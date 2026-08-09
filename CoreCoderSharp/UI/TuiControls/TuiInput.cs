using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>单行文本输入框 —— 支持光标移动、插入、删除、文本选择、撤销重做。</summary>
public class TuiInput : TuiControl
{
    public string Text { get; set; } = "";
    public int CursorPos { get; set; }
    public Action<string>? OnSubmit { get; set; }
    public bool Password { get; set; }

    // ── 撤销 / 重做 ──
    private record struct EditAction(char Type, int Position, string Text); // 'I'=插入 'D'=删除
    private readonly Stack<EditAction> _undoStack = new();
    private readonly Stack<EditAction> _redoStack = new();
    private const int MaxUndoHistory = 100;

    /// <summary>记录一次编辑操作并清空重做栈</summary>
    private void RecordEdit(char type, int pos, string text)
    {
        _undoStack.Push(new EditAction(type, pos, text));
        if (_undoStack.Count > MaxUndoHistory)
            _undoStack.TryPop(out _); // 实际 Stack 没有 TryPop，用循环
        while (_undoStack.Count > MaxUndoHistory) { var arr = _undoStack.ToArray(); _undoStack.Clear(); for (int i = arr.Length - 2; i >= 0; i--) _undoStack.Push(arr[i]); }
        _redoStack.Clear();
    }

    /// <summary>撤销最近一次操作</summary>
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        _redoStack.Push(action);
        ClearSelection();
        if (action.Type == 'I')
        {
            // 撤销插入 = 删除
            int len = action.Text.Length;
            Text = Text[..action.Position] + Text[(action.Position + len)..];
            CursorPos = action.Position;
        }
        else if (action.Type == 'D')
        {
            // 撤销删除 = 插入
            Text = Text[..action.Position] + action.Text + Text[action.Position..];
            CursorPos = action.Position + action.Text.Length;
        }
    }

    /// <summary>重做最近一次撤销</summary>
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        _undoStack.Push(action);
        ClearSelection();
        if (action.Type == 'I')
        {
            Text = Text[..action.Position] + action.Text + Text[action.Position..];
            CursorPos = action.Position + action.Text.Length;
        }
        else if (action.Type == 'D')
        {
            int len = action.Text.Length;
            Text = Text[..action.Position] + Text[(action.Position + len)..];
            CursorPos = action.Position;
        }
    }

    // ── 文本选择 ──
    /// <summary>选择起始字符索引（-1 = 无选择）</summary>
    public int SelectionStart { get; set; } = -1;
    /// <summary>选择结束字符索引（-1 = 无选择，与 Start 相同时 = 光标位）</summary>
    public int SelectionEnd { get; set; } = -1;

    /// <summary>是否有活动的文本选择</summary>
    public bool HasSelection => SelectionStart >= 0 && SelectionEnd >= 0 && SelectionStart != SelectionEnd;

    /// <summary>获取选中的文本</summary>
    public string? SelectedText
    {
        get
        {
            if (!HasSelection) return null;
            int s = Math.Min(SelectionStart, SelectionEnd);
            int e = Math.Max(SelectionStart, SelectionEnd);
            if (s < 0 || e > Text.Length) return null;
            return Text[s..e];
        }
    }

    /// <summary>清除选择</summary>
    private void ClearSelection() { SelectionStart = SelectionEnd = -1; }

    /// <summary>从光标位置开始选择</summary>
    private void StartSelection() { SelectionStart = CursorPos; SelectionEnd = CursorPos; }

    /// <summary>更新选择终点（保持起点不变）</summary>
    private void ExtendSelection() { SelectionEnd = CursorPos; }

    /// <summary>删除选中文本并返回删除的文本</summary>
    private string? DeleteSelection()
    {
        if (!HasSelection) return null;
        int s = Math.Min(SelectionStart, SelectionEnd);
        int e = Math.Max(SelectionStart, SelectionEnd);
        var deleted = Text[s..e];
        Text = Text[..s] + Text[e..];
        CursorPos = s;
        ClearSelection();
        return deleted;
    }

    public override bool HasCursor => true;

    public TuiInput() { Height = 1; Width = 20; }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var originalText = Password ? new string('•', Text.Length) : Text;
        var visW = Width;

        // ── 确定选择范围（可视化坐标） ──
        int selVisStart = -1, selVisEnd = -1;
        if (HasSelection && !Password)
        {
            selVisStart = Math.Min(SelectionStart, SelectionEnd);
            selVisEnd = Math.Max(SelectionStart, SelectionEnd);
        }

        // ── CJK 宽度感知的滚动逻辑 ──
        int cursorVisualEnd = TuiHelper.DisplayWidth(originalText[..Math.Min(CursorPos, originalText.Length)]);

        // 确定滚动起始字符索引，使光标在可见区域内
        int scrollStart = 0;
        if (cursorVisualEnd >= visW)
        {
            int needSkip = cursorVisualEnd - visW + 1;
            int skipped = 0;
            for (int i = 0; i < originalText.Length; i++)
            {
                int rw = TuiHelper.RuneWidth(originalText.EnumerateRunes().ElementAt(i));
                if (skipped + rw > needSkip) break;
                skipped += rw;
                scrollStart = i + 1;
            }
        }

        // 截取可见文本
        var visiblePart = originalText[scrollStart..];
        if (TuiHelper.DisplayWidth(visiblePart) > visW)
            visiblePart = TuiHelper.TruncateByWidth(visiblePart, visW);

        int vw = TuiHelper.DisplayWidth(visiblePart);
        var pad = Math.Max(0, visW - vw);

        int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
               : Focused ? (FocusedFg > 0 ? FocusedFg : TuiTheme.Current.InputFg)
               : (Fg > 0 ? Fg : TuiTheme.Current.ControlFg);
        int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : (Bg > 0 ? Bg : TuiTheme.Current.InputBg))
               : Focused ? (FocusedBg > 0 ? FocusedBg : TuiTheme.Current.InputCursorBg)
               : (Bg > 0 ? Bg : TuiTheme.Current.InputBg);

        // ── 选择区域反向色渲染 ──
        if (HasSelection && !Password && selVisStart >= 0)
        {
            int selStartInVisible = selVisStart - scrollStart; // 字符索引偏移
            int selEndInVisible = selVisEnd - scrollStart;

            // 在可见范围内 clamp
            int visiblePartLen = visiblePart.Length;
            int selVisStartClamped = Math.Clamp(selStartInVisible, 0, visiblePartLen);
            int selVisEndClamped = Math.Clamp(selEndInVisible, 0, visiblePartLen);

            // 前段（选择前）
            if (selVisStartClamped > 0)
            {
                var preText = visiblePart[..selVisStartClamped];
                WriteAt(sb, absY, absX, preText, fg, bg);
            }
            // 选中段（反向色）
            if (selVisEndClamped > selVisStartClamped)
            {
                var selText = visiblePart[selVisStartClamped..selVisEndClamped];
                int selX = absX + TuiHelper.DisplayWidth(visiblePart[..selVisStartClamped]);
                WriteAt(sb, absY, selX, selText, bg > 0 ? bg : 7, fg > 0 ? fg : 0);
            }
            // 后段（选择后）
            if (selVisEndClamped < visiblePartLen)
            {
                var postText = visiblePart[selVisEndClamped..];
                int postX = absX + TuiHelper.DisplayWidth(visiblePart[..selVisEndClamped]);
                WriteAt(sb, absY, postX, postText, fg, bg);
            }
            // 填充
            if (pad > 0)
            {
                int totalVw = TuiHelper.DisplayWidth(visiblePart);
                WriteAt(sb, absY, absX + totalVw, new string(' ', pad), fg, bg);
            }
        }
        else
        {
            // 无选择：正常渲染
            WriteAt(sb, absY, absX, visiblePart + new string(' ', pad), fg, bg);
        }

        // ── 光标：记录位置，由 Screen 在最后统一输出 ──
        if (IsCursorOwner)
        {
            int cursorInVisible = TuiHelper.DisplayWidth(
                originalText[scrollStart..Math.Min(CursorPos, originalText.Length)]);
            _cursorRow = absY;
            _cursorCol = absX + Math.Min(cursorInVisible, visW - 1);
            _showCursor = true;
        }
        else
        {
            _showCursor = false;
        }
    }

    // ── 输入 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !Focused) return false;

        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        // Ctrl 组合键（不依赖 Shift）
        if (ctrl)
        {
            switch (key.Key)
            {
                case ConsoleKey.A: // 全选
                    SelectionStart = 0;
                    SelectionEnd = Text.Length;
                    CursorPos = Text.Length;
                    return true;
                case ConsoleKey.C: // 复制选中文本到剪贴板
                    if (HasSelection)
                    {
                        var st = SelectedText;
                        if (st != null)
                            CopyToClipboard(st);
                    }
                    return true;
                case ConsoleKey.X: // 剪切
                    if (HasSelection)
                    {
                        var st = SelectedText;
                        if (st != null)
                            CopyToClipboard(st);
                    }
                    DeleteSelection();
                    return true;
                case ConsoleKey.V: // 粘贴
                    var pasteText = GetClipboardText();
                    if (!string.IsNullOrEmpty(pasteText))
                    {
                        var deleted = DeleteSelection(); // 先删除选中内容
                        if (deleted != null) RecordEdit('D', CursorPos, deleted);
                        var singleLine = pasteText.Replace("\r\n", " ").Replace("\n", " ");
                        Text = Text[..CursorPos] + singleLine + Text[CursorPos..];
                        RecordEdit('I', CursorPos, singleLine);
                        CursorPos += singleLine.Length;
                    }
                    return true;
                case ConsoleKey.Z: // 撤销
                    Undo();
                    return true;
                case ConsoleKey.Y: // 重做
                    Redo();
                    return true;
                case ConsoleKey.Backspace: // Ctrl+Backspace 删除一个词
                    DeleteWordBefore();
                    return true;
            }
        }

        // 方向键（含 Shift 选择扩展）
        if (shift)
        {
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (!HasSelection) StartSelection();
                    if (CursorPos > 0) CursorPos--;
                    ExtendSelection();
                    return true;
                case ConsoleKey.RightArrow:
                    if (!HasSelection) StartSelection();
                    if (CursorPos < Text.Length) CursorPos++;
                    ExtendSelection();
                    return true;
                case ConsoleKey.Home:
                    if (!HasSelection) StartSelection();
                    CursorPos = 0;
                    ExtendSelection();
                    return true;
                case ConsoleKey.End:
                    if (!HasSelection) StartSelection();
                    CursorPos = Text.Length;
                    ExtendSelection();
                    return true;
            }
        }

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (HasSelection)
                {
                    // 有选择时按非 Shift 方向键：跳到选择边界并清除选择
                    CursorPos = Math.Min(SelectionStart, SelectionEnd);
                    ClearSelection();
                }
                else if (CursorPos > 0) CursorPos--;
                return true;
            case ConsoleKey.RightArrow:
                if (HasSelection)
                {
                    CursorPos = Math.Max(SelectionStart, SelectionEnd);
                    ClearSelection();
                }
                else if (CursorPos < Text.Length) CursorPos++;
                return true;
            case ConsoleKey.Home:
                CursorPos = 0; ClearSelection(); return true;
            case ConsoleKey.End:
                CursorPos = Text.Length; ClearSelection(); return true;
            case ConsoleKey.Backspace:
                if (HasSelection)
                {
                    var deleted = DeleteSelection();
                    if (deleted != null) RecordEdit('D', CursorPos, deleted);
                }
                else if (CursorPos > 0)
                {
                    var ch = Text[CursorPos - 1].ToString();
                    Text = Text[..(CursorPos - 1)] + Text[CursorPos..];
                    CursorPos--;
                    RecordEdit('D', CursorPos, ch);
                }
                return true;
            case ConsoleKey.Delete:
                if (HasSelection)
                {
                    var deleted = DeleteSelection();
                    if (deleted != null) RecordEdit('D', CursorPos, deleted);
                }
                else if (CursorPos < Text.Length)
                {
                    var ch = Text[CursorPos].ToString();
                    Text = Text[..CursorPos] + Text[(CursorPos + 1)..];
                    RecordEdit('D', CursorPos, ch);
                }
                return true;
            case ConsoleKey.Enter:
                ClearSelection();
                OnSubmit?.Invoke(Text);
                return true;
            case ConsoleKey.Escape:
                ClearSelection();
                return true;
            case ConsoleKey.Tab:
                ClearSelection();
                return false; // 让父容器处理焦点切换
            default:
                if (key.KeyChar >= ' ')
                {
                    var deleted = DeleteSelection(); // 输入前先删除选中内容
                    if (deleted != null) RecordEdit('D', CursorPos, deleted);
                    Text = Text[..CursorPos] + key.KeyChar + Text[CursorPos..];
                    RecordEdit('I', CursorPos, key.KeyChar.ToString());
                    CursorPos++;
                    return true;
                }
                return false;
        }
    }

    // ── 工具 ──

    private void DeleteWordBefore()
    {
        if (CursorPos == 0) return;
        ClearSelection();
        int pos = CursorPos;
        while (pos > 0 && Text[pos - 1] == ' ') pos--;
        while (pos > 0 && Text[pos - 1] != ' ') pos--;
        var deleted = Text[pos..CursorPos];
        Text = Text[..pos] + Text[CursorPos..];
        RecordEdit('D', pos, deleted);
        CursorPos = pos;
    }

    /// <summary>复制文本到系统剪贴板（尽力而为）</summary>
    private static void CopyToClipboard(string text)
    {
        try
        {
            ClipboardHelper.SetText(text);
        }
        catch { /* 剪贴板不可用时静默忽略 */ }
    }

    /// <summary>从系统剪贴板获取文本（尽力而为）</summary>
    private static string? GetClipboardText()
    {
        try
        {
            return ClipboardHelper.GetText();
        }
        catch { return null; }
    }
}
