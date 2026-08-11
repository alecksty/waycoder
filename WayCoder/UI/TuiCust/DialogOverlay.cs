namespace WayCoder.UI;

/// <summary>
/// 对话框叠层管理器 —— 对标 Crush 的 Dialog Overlay 栈。
/// 在现有 TuiScreen/TuiWindow 系统之上提供栈式对话框管理 + 类型化结果。
///
/// 用法：
///   var overlay = new DialogOverlay(screen);
///   var result = overlay.Push("confirm", TuiDialog.Confirm(...),
///       onClose: action => { ... });
///   overlay.Pop("confirm");
/// </summary>
public class DialogOverlay
{
    private readonly TuiScreen _screen;
    private readonly Stack<OverlayEntry> _stack = new();
    private readonly Dictionary<string, OverlayEntry> _byId = new();

    /// <summary>当前叠层深度</summary>
    public int Depth => _stack.Count;

    /// <summary>是否有活跃的对话框</summary>
    public bool HasOverlay => _stack.Count > 0;

    /// <summary>栈顶对话框 ID（无则为 null）</summary>
    public string? TopId => _stack.Count > 0 ? _stack.Peek().Id : null;

    public DialogOverlay(TuiScreen screen)
    {
        _screen = screen;
    }

    /// <summary>
    /// 入栈一个对话框。
    /// 如果同 ID 已存在，先关闭旧对话框再入栈新对话框。
    /// </summary>
    /// <param name="id">唯一标识符</param>
    /// <param name="window">TuiWindow 对话框</param>
    /// <param name="onResult">类型化结果回调</param>
    public void Push(string id, TuiWindow window, Action<object>? onResult = null)
    {
        // 同 ID 替换
        if (_byId.TryGetValue(id, out var existing))
        {
            _screen.CloseWindow(existing.Window);
            _byId.Remove(id);
            // 从栈中移除（可能在中间位置）
            var temp = new Stack<OverlayEntry>();
            while (_stack.Count > 0)
            {
                var entry = _stack.Pop();
                if (entry.Id != id) temp.Push(entry);
            }
            while (temp.Count > 0) _stack.Push(temp.Pop());
        }

        // 保存当前焦点
        var savedFocus = _screen.FocusedWindow;

        var newEntry = new OverlayEntry
        {
            Id = id,
            Window = window,
            OnResult = onResult,
            SavedFocus = savedFocus,
        };

        // 拦截窗口关闭
        var originalOnClosed = window.OnClosed;
        window.OnClosed = () =>
        {
            originalOnClosed?.Invoke();
            // 触发结果回调
            var result = window.Result ?? DialogAction.CloseAction;
            onResult?.Invoke(result);
            // 清理
            RemoveFromStack(id);
        };

        _stack.Push(newEntry);
        _byId[id] = newEntry;

        _screen.ShowWindow(window);
    }

    /// <summary>
    /// 弹出（关闭）指定 ID 的对话框及其之上的所有对话框。
    /// </summary>
    public void Pop(string id)
    {
        // 弹出该 ID 及其之上的所有对话框
        while (_stack.Count > 0)
        {
            var top = _stack.Peek();
            _screen.CloseWindow(top.Window);
            _byId.Remove(top.Id);
            _stack.Pop();
            if (top.Id == id) break;
        }

        // 恢复焦点到栈顶对话框或根视图
        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            _screen.FocusedWindow = next.Window;
        }
    }

    /// <summary>弹出栈顶对话框</summary>
    public void PopTop()
    {
        if (_stack.Count > 0)
            Pop(_stack.Peek().Id);
    }

    /// <summary>关闭所有对话框</summary>
    public void Clear()
    {
        while (_stack.Count > 0)
        {
            var top = _stack.Pop();
            _screen.CloseWindow(top.Window);
            _byId.Remove(top.Id);
        }
    }

    /// <summary>检查指定 ID 的对话框是否存在</summary>
    public bool Contains(string id) => _byId.ContainsKey(id);

    /// <summary>
    /// 输入路由：栈顶对话框获得优先处理权。
    /// 返回 true 表示输入已被处理。
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (_stack.Count == 0) return false;

        var top = _stack.Peek();
        if (top.Window.OnKey(key)) return true;

        // Esc 默认关闭栈顶
        if (key.Key == ConsoleKey.Escape)
        {
            PopTop();
            return true;
        }

        return false;
    }

    // ── 内部 ──

    private void RemoveFromStack(string id)
    {
        _byId.Remove(id);
        var temp = new Stack<OverlayEntry>();
        while (_stack.Count > 0)
        {
            var entry = _stack.Pop();
            if (entry.Id != id) temp.Push(entry);
        }
        while (temp.Count > 0) _stack.Push(temp.Pop());
    }

    private class OverlayEntry
    {
        public string Id { get; init; } = "";
        public TuiWindow Window { get; init; } = null!;
        public Action<object>? OnResult { get; init; }
        public TuiWindow? SavedFocus { get; init; }
    }
}
