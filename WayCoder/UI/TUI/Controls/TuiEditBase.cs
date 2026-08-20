namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 文字编辑控件抽象基类 —— 提供光标定位合约、键盘分发引擎、剪贴板、撤销栈等完整编辑能力。
/// 子类只需实现数据模型相关的底层原语（光标移动、字符增删、选择管理、撤销重做、粘贴），
/// 键盘处理逻辑（Ctrl 组合/Shift 选择/方向键/编辑键）由基类统一分发。
/// </summary>
public abstract class TuiEditBase : TuiControl
{
    // ═══════════════════════════════════════════════════════════════
    // 光标合约
    // ═══════════════════════════════════════════════════════════════

    public override bool HasCursor => true;

    /// <summary>计算并设置 _cursorRow / _cursorCol / _showCursor</summary>
    protected abstract void GotoCursorPos();

    protected override void EnsureCursorPosition()
    {
        if (!IsCursorOwner) return;
        GotoCursorPos();
    }

    /// <summary>OnRender 中记录光标位置的快速路径</summary>
    protected void RecordCursorPos(int row, int col)
    {
        _cursorRow = row;
        _cursorCol = col;
        _showCursor = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // 抽象编辑原语 —— 子类实现
    // ═══════════════════════════════════════════════════════════════

    // ── 光标移动 ──
    protected abstract void MoveCursorLeft();
    protected abstract void MoveCursorRight();
    protected abstract void MoveCursorHome();
    protected abstract void MoveCursorEnd();
    protected virtual void MoveCursorUp() { }
    protected virtual void MoveCursorDown() { }
    protected virtual void MoveCursorPageUp() { }
    protected virtual void MoveCursorPageDown() { }

    // ── 文本编辑 ──
    protected abstract void InsertChar(char ch);
    protected abstract void DeleteCharBefore();
    protected abstract void DeleteCharAfter();
    protected abstract void DeleteWordBefore();
    protected abstract void DeleteToLineEnd();
    /// <summary>插入换行。默认行为：触发 OnSubmit 提交（单行输入框用）。多行编辑区覆写为实际拆行。</summary>
    protected virtual void InsertNewLine() { OnSubmit?.Invoke(GetText()); }

    // ── 选择管理 ──
    public abstract bool HasSelection { get; }
    public abstract string? GetSelectedText();
    protected abstract void SelectAll();
    protected abstract void ClearSelection();
    protected abstract void StartSelection();
    protected abstract void ExtendSelection();
    protected abstract void DeleteSelection();

    // ── 撤销 / 重做 ──
    protected abstract void Undo();
    protected abstract void Redo();

    // ── 粘贴（单行/多行差异由子类处理）──
    protected abstract void PasteText(string text);

    // ── 获取全部文本（供默认 InsertNewLine 提交用）──
    protected abstract string GetText();

    /// <summary>公开粘贴入口：bracketed paste 事件由 ChatScreen.HandleBracketedPaste 路由到
    /// 模态对话框的焦点输入控件（api-key 输入框等，否则粘贴内容只进主输入框导致花屏）。</summary>
    public void PasteFromExternal(string text) => PasteText(text);

    // ═══════════════════════════════════════════════════════════════
    // 剪贴板组合操作
    // ═══════════════════════════════════════════════════════════════

    protected void CopySelection()
    {
        var text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
            CopyToClipboard(text);
    }

    protected void CutSelection()
    {
        CopySelection();
        DeleteSelection();
    }

    protected void PasteFromClipboard()
    {
        var text = GetClipboardText();
        if (!string.IsNullOrEmpty(text))
            PasteText(text);
    }

    /// <summary>内部剪贴板兜底：复制/剪切优先写这里，粘贴优先读这里 ——
    /// CLI 无 GUI 剪贴板会话时（如 Keypad 测试、SSH）系统剪贴板不可靠（读到残留），
    /// 内部兜底保证项目内复制→粘贴一致。</summary>
    private static string? _clipboard;

    /// <summary>内部剪贴板内容（ChatScreen.PasteAsync 等共享，保证复制→粘贴一致）。</summary>
    internal static string? InternalClipboard => _clipboard;

    protected static void CopyToClipboard(string text)
    {
        _clipboard = text;
        try { ClipboardHelper.SetText(text); } catch { }
    }

    protected static string? GetClipboardText()
    {
        if (_clipboard != null) return _clipboard; // 内部优先（刚复制/剪切的）
        try { return ClipboardHelper.GetText(); } catch { return null; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 撤销栈
    // ═══════════════════════════════════════════════════════════════

    protected const int MaxUndoHistory = 100;

    protected static void TrimStack<T>(Stack<T> stack, int max)
    {
        if (stack.Count <= max) return;
        var arr = stack.ToArray();
        stack.Clear();
        for (int i = arr.Length - 2; i >= 0; i--) stack.Push(arr[i]);
    }

    // ═══════════════════════════════════════════════════════════════
    // 共享属性
    // ═══════════════════════════════════════════════════════════════

    public bool ReadOnly { get; set; }
    public Action? OnTextChanged { get; set; }
    public Action<string>? OnSubmit { get; set; }

    /// <summary>是否接受 Tab 键作为文本输入（默认 false = Tab 切换焦点）。多行代码编辑器可覆写为 true。</summary>
    protected virtual bool AcceptsTab => false;

    protected void NotifyChanged()
    {
        OnTextChanged?.Invoke();
        MarkDirty();
    }

    // ═══════════════════════════════════════════════════════════════
    // 键盘分发引擎
    // ═══════════════════════════════════════════════════════════════

    /// <summary>判断按键是否为编辑操作（只读模式下仅允许导航键）</summary>
    private static bool IsEditKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        if (ctrl) return true; // Ctrl 组合全部拦截
        return key.Key switch
        {
            ConsoleKey.Backspace or ConsoleKey.Delete or ConsoleKey.Enter => true,
            _ => key.KeyChar >= ' ', // 可打印字符
        };
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // Hook 优先拦截（不受 Enabled/Focused/ReadOnly 限制）
        if (KeyHook != null && KeyHook(key))
            return true;

        if (!IsEnabled || !Focused) return false;
        if (ReadOnly && IsEditKey(key)) return false; // 只读：仅导航，不编辑

        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
        bool ctrl  = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        bool handled = ctrl ? HandleCtrlKey(key)
                     : shift ? HandleShiftKey(key)
                     : HandleRegularKey(key);

        // 处理了就一定重绘：光标移动、选择变化、撤销重做都改变绘制结果，
        // 但只有改文本的原语会走 NotifyChanged。这里兜底，省得每个原语各记一次
        // （单行 TuiInput 与多行 TuiTextArea 共用这条路径）。
        if (handled) MarkDirty();
        return handled;
    }

    /// <summary>Ctrl 组合键分发</summary>
    protected virtual bool HandleCtrlKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.A: SelectAll(); return true;
            case ConsoleKey.C: CopySelection(); return true;
            case ConsoleKey.X: CutSelection(); return true;
            case ConsoleKey.V: PasteFromClipboard(); return true;
            case ConsoleKey.Insert: CopySelection(); return true; // Ctrl+Insert = 复制（Ctrl+C 被全局保留为退出）
            case ConsoleKey.Z: Undo(); return true;
            case ConsoleKey.Y: Redo(); return true;
            case ConsoleKey.E: ClearSelection(); MoveCursorEnd(); return true;
            case ConsoleKey.K: DeleteToLineEnd(); return true;
            case ConsoleKey.Backspace: DeleteWordBefore(); return true;
            case ConsoleKey.Enter: ClearSelection(); OnSubmit?.Invoke(GetText()); return true;
            default: return false;
        }
    }

    /// <summary>Shift + 方向键 = 扩展选择；Shift + 可打印字符 = 输入标点/大写</summary>
    protected virtual bool HandleShiftKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:  if (!HasSelection) StartSelection(); MoveCursorLeft();  ExtendSelection(); return true;
            case ConsoleKey.RightArrow: if (!HasSelection) StartSelection(); MoveCursorRight(); ExtendSelection(); return true;
            case ConsoleKey.UpArrow:    if (!HasSelection) StartSelection(); MoveCursorUp();    ExtendSelection(); return true;
            case ConsoleKey.DownArrow:  if (!HasSelection) StartSelection(); MoveCursorDown();  ExtendSelection(); return true;
            case ConsoleKey.Home:       if (!HasSelection) StartSelection(); MoveCursorHome();  ExtendSelection(); return true;
            case ConsoleKey.End:        if (!HasSelection) StartSelection(); MoveCursorEnd();   ExtendSelection(); return true;
            case ConsoleKey.Insert:     PasteFromClipboard(); return true; // Shift+Insert = 粘贴（Linux/Win 通用）
            default:
                // 其余 Shift 组合若为可打印字符（!@#$%^&*()_+{}|:"<>? 及大写字母），直接输入
                if (key.KeyChar >= ' ')
                {
                    if (HasSelection) DeleteSelection();
                    InsertChar(key.KeyChar);
                    return true;
                }
                return false;
        }
    }

    /// <summary>常规按键分发</summary>
    protected virtual bool HandleRegularKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:  JumpToSelStartOrMoveLeft();  return true;
            case ConsoleKey.RightArrow: JumpToSelEndOrMoveRight();   return true;
            case ConsoleKey.UpArrow:    JumpToSelStartOrMoveUp();    return true;
            case ConsoleKey.DownArrow:  JumpToSelEndOrMoveDown();    return true;
            case ConsoleKey.Home:       MoveCursorHome(); ClearSelection(); return true;
            case ConsoleKey.End:        MoveCursorEnd();  ClearSelection(); return true;
            case ConsoleKey.PageUp:     MoveCursorPageUp();   ClearSelection(); return true;
            case ConsoleKey.PageDown:   MoveCursorPageDown(); ClearSelection(); return true;

            case ConsoleKey.Backspace:
                if (HasSelection) DeleteSelection();
                else DeleteCharBefore();
                return true;

            case ConsoleKey.Delete:
                if (HasSelection) DeleteSelection();
                else DeleteCharAfter();
                return true;

            case ConsoleKey.Enter:
                if (HasSelection) DeleteSelection();
                InsertNewLine();
                return true;

            case ConsoleKey.Escape:
                ClearSelection();
                return true;

            case ConsoleKey.Tab:
                if (AcceptsTab) { ClearSelection(); InsertChar('\t'); return true; }
                ClearSelection();
                return false; // 让父容器处理焦点切换

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

    // ── 方向键辅助：有选择→跳到边界，无选择→移动 ──

    private void JumpToSelStartOrMoveLeft()
    {
        if (HasSelection) { JumpToSelStart(); ClearSelection(); }
        else MoveCursorLeft();
    }

    private void JumpToSelEndOrMoveRight()
    {
        if (HasSelection) { JumpToSelEnd(); ClearSelection(); }
        else MoveCursorRight();
    }

    private void JumpToSelStartOrMoveUp()
    {
        if (HasSelection) { JumpToSelStart(); ClearSelection(); }
        else MoveCursorUp();
    }

    private void JumpToSelEndOrMoveDown()
    {
        if (HasSelection) { JumpToSelEnd(); ClearSelection(); }
        else MoveCursorDown();
    }

    /// <summary>跳到选择起点（子类覆写以提供正确的坐标比较）</summary>
    protected virtual void JumpToSelStart() => MoveCursorLeft();
    /// <summary>跳到选择终点</summary>
    protected virtual void JumpToSelEnd() => MoveCursorRight();
}
