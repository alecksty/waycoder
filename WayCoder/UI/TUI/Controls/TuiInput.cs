using System.Text;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>单行文本输入框 —— 支持光标移动、插入、删除、文本选择、撤销重做。</summary>
public class TuiInput : TuiEditBase
{
    public string Text { get; set; } = "";
    public int CursorPos { get; set; }
    public bool Password { get; set; }

    // ── 撤销 / 重做 ──
    private record struct EditAction(char Type, int Position, string Text); // 'I'=插入 'D'=删除
    private readonly Stack<EditAction> _undoStack = new();
    private readonly Stack<EditAction> _redoStack = new();
    /// <summary>记录一次编辑操作并清空重做栈</summary>
    private void RecordEdit(char type, int pos, string text)
    {
        _undoStack.Push(new EditAction(type, pos, text));
        TrimStack(_undoStack, MaxUndoHistory);
        _redoStack.Clear();
    }

    /// <summary>撤销最近一次操作</summary>
    protected override void Undo()
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
    protected override void Redo()
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
    public override bool HasSelection => SelectionStart >= 0 && SelectionEnd >= 0 && SelectionStart != SelectionEnd;

    /// <summary>获取选中的文本</summary>
    public override string? GetSelectedText()
    {
        if (!HasSelection) return null;
        int s = Math.Min(SelectionStart, SelectionEnd);
        int e = Math.Max(SelectionStart, SelectionEnd);
        if (s < 0 || e > Text.Length) return null;
        return Text[s..e];
    }

    protected override void ClearSelection() { SelectionStart = SelectionEnd = -1; }
    protected override void StartSelection() { SelectionStart = CursorPos; SelectionEnd = CursorPos; }
    protected override void ExtendSelection() { SelectionEnd = CursorPos; }

    protected override void DeleteSelection()
    {
        if (!HasSelection) return;
        int s = Math.Min(SelectionStart, SelectionEnd);
        int e = Math.Max(SelectionStart, SelectionEnd);
        var deleted = Text[s..e];
        Text = Text[..s] + Text[e..];
        CursorPos = s;
        ClearSelection();
        RecordEdit('D', s, deleted);
    }

    /// <summary>
    /// 计算并设置光标屏幕坐标（CJK 感知 + 滚动偏移）。
    /// 不依赖 OnRender 调用，保证即使控件未被重绘光标位置也正确。
    /// </summary>
    protected override void GotoCursorPos()
    {
        if (!IsCursorOwner) return;

        var absX = _lastAbsX;
        var absY = _lastAbsY;
        var originalText = Password ? new string('•', Text.Length) : Text;
        var visW = Width;

        // 计算滚动偏移：保证光标在可见区域内（rune 感知，与 OnRender 共用同一逻辑）
        int scrollStart = ComputeScrollStart(originalText, CursorPos, visW);

        // 光标在可见区域内的偏移
        int cursorInVisible = AnsiHelper.DisplayWidth(
            originalText[scrollStart..Math.Min(CursorPos, originalText.Length)]);

        _cursorRow = absY;
        _cursorCol = absX + Math.Min(cursorInVisible, visW - 1);
        _showCursor = true;
    }

    /// <summary>
    /// 计算使光标落入可见区的滚动起始字符索引（char 索引，rune 感知）。
    /// 光标前的视觉宽度超出可见宽时，回退足够字符使光标贴近可见区右缘。
    /// OnRender 与 GotoCursorPos 共用此逻辑，保证渲染文本与光标坐标一致。
    /// </summary>
    private static int ComputeScrollStart(string text, int cursorPos, int visW)
    {
        if (visW <= 0 || string.IsNullOrEmpty(text)) return 0;
        var before = text[..Math.Min(cursorPos, text.Length)];
        int cursorVisualEnd = AnsiHelper.DisplayWidth(before);
        if (cursorVisualEnd < visW) return 0;

        int needSkip = cursorVisualEnd - visW + 1;
        int skipped = 0;
        int scrollStart = 0;
        foreach (var r in text.EnumerateRunes())
        {
            if (skipped >= needSkip) break;
            skipped += AnsiHelper.RuneWidth(r);
            scrollStart += r.ToString().Length;
        }
        return scrollStart;
    }

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
        int scrollStart = ComputeScrollStart(originalText, CursorPos, visW);

        // 截取可见文本
        var visiblePart = originalText[scrollStart..];
        if (AnsiHelper.DisplayWidth(visiblePart) > visW)
            visiblePart = AnsiHelper.TruncateByWidth(visiblePart, visW);

        int vw = AnsiHelper.DisplayWidth(visiblePart);
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
                int selX = absX + AnsiHelper.DisplayWidth(visiblePart[..selVisStartClamped]);
                WriteAt(sb, absY, selX, selText, bg > 0 ? bg : 7, fg > 0 ? fg : 0);
            }
            // 后段（选择后）
            if (selVisEndClamped < visiblePartLen)
            {
                var postText = visiblePart[selVisEndClamped..];
                int postX = absX + AnsiHelper.DisplayWidth(visiblePart[..selVisEndClamped]);
                WriteAt(sb, absY, postX, postText, fg, bg);
            }
            // 填充
            if (pad > 0)
            {
                int totalVw = AnsiHelper.DisplayWidth(visiblePart);
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
            int cursorInVisible = AnsiHelper.DisplayWidth(
                originalText[scrollStart..Math.Min(CursorPos, originalText.Length)]);
            RecordCursorPos(absY, absX + Math.Min(cursorInVisible, visW - 1));
        }
        else
        {
            _showCursor = false;
        }
    }

    // ── 光标移动原语 ──

    protected override void MoveCursorLeft()
    {
        if (CursorPos == 0) return;
        CursorPos--;
        // 光标不落在代理对中间（emoji/CJK 扩展 B）
        if (CursorPos > 0 && char.IsHighSurrogate(Text[CursorPos - 1]) && char.IsLowSurrogate(Text[CursorPos]))
            CursorPos--;
    }

    protected override void MoveCursorRight()
    {
        if (CursorPos >= Text.Length) return;
        CursorPos++;
        if (CursorPos < Text.Length && char.IsHighSurrogate(Text[CursorPos - 1]) && char.IsLowSurrogate(Text[CursorPos]))
            CursorPos++;
    }
    protected override void MoveCursorHome()  { CursorPos = 0; }
    protected override void MoveCursorEnd()   { CursorPos = Text.Length; }

    protected override void JumpToSelStart() => CursorPos = Math.Min(SelectionStart, SelectionEnd);
    protected override void JumpToSelEnd()   => CursorPos = Math.Max(SelectionStart, SelectionEnd);

    // ── 编辑原语 ──

    protected override void InsertChar(char ch)
    {
        Text = Text[..CursorPos] + ch + Text[CursorPos..];
        RecordEdit('I', CursorPos, ch.ToString());
        CursorPos++;
        NotifyChanged();
    }

    protected override void DeleteCharBefore()
    {
        if (CursorPos <= 0) return;
        int delLen = CursorPos >= 2 && char.IsHighSurrogate(Text[CursorPos - 2]) && char.IsLowSurrogate(Text[CursorPos - 1]) ? 2 : 1;
        var ch = Text.Substring(CursorPos - delLen, delLen);
        Text = Text[..(CursorPos - delLen)] + Text[CursorPos..];
        CursorPos -= delLen;
        RecordEdit('D', CursorPos, ch);
        NotifyChanged();
    }

    protected override void DeleteCharAfter()
    {
        if (CursorPos >= Text.Length) return;
        int delLen = CursorPos + 1 < Text.Length && char.IsHighSurrogate(Text[CursorPos]) && char.IsLowSurrogate(Text[CursorPos + 1]) ? 2 : 1;
        var ch = Text.Substring(CursorPos, delLen);
        Text = Text[..CursorPos] + Text[(CursorPos + delLen)..];
        RecordEdit('D', CursorPos, ch);
        NotifyChanged();
    }

    protected override void DeleteWordBefore()
    {
        if (CursorPos == 0) return;
        int pos = CursorPos;
        while (pos > 0 && Text[pos - 1] == ' ') pos--;
        while (pos > 0 && Text[pos - 1] != ' ') pos--;
        var deleted = Text[pos..CursorPos];
        Text = Text[..pos] + Text[CursorPos..];
        RecordEdit('D', pos, deleted);
        CursorPos = pos;
        NotifyChanged();
    }

    protected override void DeleteToLineEnd()
    {
        if (CursorPos >= Text.Length) return;
        var deleted = Text[CursorPos..];
        Text = Text[..CursorPos];
        RecordEdit('D', CursorPos, deleted);
        NotifyChanged();
    }

    protected override void SelectAll()
    {
        SelectionStart = 0;
        SelectionEnd = Text.Length;
        CursorPos = Text.Length;
    }

    protected override void PasteText(string text)
    {
        var singleLine = text.Replace("\r\n", " ").Replace("\n", " ");
        Text = Text[..CursorPos] + singleLine + Text[CursorPos..];
        RecordEdit('I', CursorPos, singleLine);
        CursorPos += singleLine.Length;
        NotifyChanged();
    }

    protected override string GetText() => Text;
}