using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

/// <summary>
/// 屏幕 —— 一个完整的终端场景。
/// 持有根视图（内联控件树）和浮层窗口列表。
/// </summary>
public abstract class TuiScreen
{
    /// <summary>屏幕名称（用于切换标识）</summary>
    public string Name { get; set; } = "";

    /// <summary>所属管理器引用（由 PushScreen 自动设置）</summary>
    public TuiManager? Manager { get; set; }

    /// <summary>标记需要重绘</summary>
    public void MarkDirty() { if (Manager != null) Manager.IsDirty = true; }

    /// <summary>立即渲染整个屏幕（便捷方法）</summary>
    public void Render() { Manager?.Render(); }

    /// <summary>屏幕根视图（状态栏、主区域、输入区等）</summary>
    public TuiView RootView { get; set; } = new TuiVBox();

    /// <summary>浮层窗口列表</summary>
    public readonly List<TuiWindow> Windows = [];

    /// <summary>当前焦点窗口（键盘路由优先）</summary>
    public TuiWindow? FocusedWindow { get; set; }

    /// <summary>是否有模态窗口</summary>
    public bool HasModal => Windows.Any(w => w.Modal);

    /// <summary>模态窗口出现前保存的根视图焦点控件</summary>
    private TuiControl? _savedRootFocus;

    /// <summary>当前光标所有者</summary>
    private TuiControl? _cursorOwner;

    /// <summary>需要重绘的脏区域（窗口关闭时记录其覆盖区）</summary>
    private readonly List<(int x, int y, int w, int h)> _dirtyRects = [];

    /// <summary>当前终端尺寸</summary>
    public int TW { get; protected set; }
    public int TH { get; protected set; }

    // ── 生命周期 ──

    /// <summary>屏幕激活时调用（初始化控件树、设置布局）</summary>
    public virtual void Activate()
    {
        TW = TTY.Cols;
        TH = TTY.Rows;
        RootView.Width = TW;
        RootView.Height = TH;
        RootView.Layout();
    }

    /// <summary>屏幕失活时调用</summary>
    public virtual void Deactivate()
    {
        Windows.Clear();
        FocusedWindow = null;
    }

    /// <summary>终端尺寸变化。递归通知根视图和所有浮层窗口。</summary>
    public virtual void OnResize(int newW, int newH)
    {
        TW = newW;
        TH = newH;

        // 1. 根视图重算布局
        RootView.Width = newW;
        RootView.Height = newH;
        RootView.OnResize(newW, newH);

        // 2. 所有浮层窗口重新定位和布局
        foreach (var win in Windows)
            win.OnResize(newW, newH);
    }

    // ── 窗口管理 ──

    private int _nextZ;

    /// <summary>添加浮层窗口</summary>
    public void AddWindow(TuiWindow win)
    {
        // 首个模态窗口出现时，保存并清除根视图焦点（防止光标穿透）
        if (win.Modal && !HasModal)
        {
            _savedRootFocus = RootView.FindFocused();
            if (_savedRootFocus != null) _savedRootFocus.Focused = false;
        }

        // 激活窗口获得焦点，之前的焦点窗口失焦
        if (FocusedWindow != null) FocusedWindow.Focused = false;
        win.ZOrder = _nextZ++;
        win.Focused = true;
        Windows.Add(win);
        FocusedWindow = win;
    }

    /// <summary>添加浮层窗口并自动绑定关闭回调</summary>
    public void ShowWindow(TuiWindow win)
    {
        win.OnClosed = () => CloseWindow(win);
        AddWindow(win);
        MarkDirty();
    }

    /// <summary>关闭窗口</summary>
    public void CloseWindow(TuiWindow win)
    {
        // 记录窗口覆盖区域，用于关闭后重绘被遮挡的控件
        _dirtyRects.Add((win.X, win.Y, win.Width, win.Height));

        bool wasModal = win.Modal;
        win.Focused = false;
        Windows.Remove(win);
        if (FocusedWindow == win)
        {
            FocusedWindow = Windows.LastOrDefault();
            if (FocusedWindow != null) FocusedWindow.Focused = true;
        }

        // 最后一个模态窗口关闭后，恢复根视图焦点
        if (wasModal && !HasModal && _savedRootFocus != null)
        {
            _savedRootFocus.Focused = true;
            _savedRootFocus = null;
        }

        MarkDirty();

        // 注意：不在此处调用 win.OnClosed，避免无限递归。
        // OnClosed 是窗口主动请求关闭的通知，由窗口内控件触发，
        // 调用方（如 TuiDialog 按钮）先触发 OnClosed，再到达此处。
    }

    /// <summary>关闭所有模态窗口</summary>
    public void CloseAllModals()
    {
        foreach (var w in Windows.Where(w => w.Modal).ToList())
            Windows.Remove(w);
        FocusedWindow = Windows.LastOrDefault();
    }

    /// <summary>
    /// 在渲染前确定光标所有者。每屏只有一个控件拥有光标。
    /// 优先级：模态窗口内的焦点控件 → 根视图的焦点控件。
    /// </summary>
    public void SetCursorOwner()
    {
        // 清除旧所有者
        if (_cursorOwner != null)
            _cursorOwner.IsCursorOwner = false;

        TuiControl? focused;
        if (HasModal && FocusedWindow != null)
            focused = FocusedWindow.FocusedControl;
        else
            focused = RootView.FindFocused();

        _cursorOwner = focused;
        if (_cursorOwner != null)
            _cursorOwner.IsCursorOwner = true;
    }

    // ── 鼠标 ──

    /// <summary>
    /// 处理鼠标事件。优先级：顶层模态窗口 → 顶层窗口（Z-order）→ 根视图。
    /// 返回 true 表示事件已被消费。
    /// </summary>
    public virtual bool HandleMouse(InputEvent ev)
    {
        // 有模态窗口时，只路由给顶层模态窗口
        if (HasModal)
        {
            var topModal = Windows.LastOrDefault(w => w.Modal);
            if (topModal != null && topModal.HandleMouse(ev))
                return true;
            // 模态遮罩：点击模态窗口外部也消费事件（防止穿透）
            return true;
        }

        // 按 Z-order 从高到低路由给窗口
        foreach (var win in Windows.OrderByDescending(w => w.ZOrder))
        {
            if (win.HandleMouse(ev))
                return true;
        }

        return false;
    }

    // ── 输入 ──

    /// <summary>
    /// 处理按键。返回 true 表示已处理。
    /// 优先：Esc 关闭顶层模态窗口 → 路由给焦点窗口 → 路由给根视图。
    /// </summary>
    public virtual bool HandleKey(ConsoleKeyInfo key)
    {
        // Esc 关闭顶层模态窗口
        if (key.Key == ConsoleKey.Escape && HasModal)
        {
            var topModal = Windows.LastOrDefault(w => w.Modal);
            if (topModal != null)
            {
                CloseWindow(topModal);
                return true;
            }
        }

        // Tab/Shift+Tab 在焦点窗口内切换控件
        if (key.Key == ConsoleKey.Tab && FocusedWindow != null)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                FocusedWindow.FocusPrev();
            else
                FocusedWindow.FocusNext();
            return true;
        }

        // 优先路由给焦点窗口（模态窗口拦截所有输入）
        if (FocusedWindow?.Modal == true)
            return FocusedWindow.HandleKey(key);

        // 路由给非模态焦点窗口
        if (FocusedWindow != null && FocusedWindow.HandleKey(key))
            return true;

        // 最后路由给根视图
        return RootView.HandleKey(key);
    }

    // ── 渲染 ──

    /// <summary>
    /// 渲染整个屏幕（根视图 + 浮层窗口）到 StringBuilder。
    /// </summary>
    public virtual void Render(StringBuilder sb)
    {
        // 1. 渲染根视图（光标先隐藏，仅记录位置）
        RootView.Render(sb, 0, 0);

        // 2. 渲染模态窗口遮罩
        foreach (var win in Windows.Where(w => w.HasMask))
        {
            for (int row = 0; row < win.Height; row++)
            {
                int screenY = win.Y + row;
                if (screenY < 0 || screenY >= TH) continue;
                var rb = new RenderBuffer();
                int maskBg = win.WinBg > 0 ? win.WinBg : 100;
                rb.Write(screenY, win.X, new string(' ', win.Width), bg: maskBg);
                sb.Append(rb.ToString());
            }
        }

        // 3. 按 Z-order 渲染窗口
        foreach (var win in Windows.OrderBy(w => w.ZOrder))
        {
            RenderWindow(sb, win);
        }

        // 3.5 重绘脏区域（窗口关闭后，补绘被遮挡的根视图控件）
        foreach (var (x, y, w, h) in _dirtyRects)
        {
            RootView.Render(sb, 0, 0,
                clipL: x, clipT: y, clipR: x + w, clipB: y + h);
        }
        _dirtyRects.Clear();

        // 4. 最后统一输出光标：只有光标所有者的位置有效，其余绘制不会显示光标
        if (_cursorOwner != null)
        {
            var cs = _cursorOwner.GetCursorState();
            if (cs.HasValue)
            {
                var rb = new RenderBuffer();
                rb.CursorAt(cs.Value.row, cs.Value.col);
                sb.Append(rb.ToString());
            }
        }
    }

    /// <summary>渲染单个窗口（边框 + 标题栏 + 内部控件树）</summary>
    protected virtual void RenderWindow(StringBuilder sb, TuiWindow win)
    {
        int bc = win.EffectiveBorderColor;
        int fillBg = win.WinBg > 0 ? win.WinBg : 100; // 边框背景与遮罩一致

        // 无边框模式：直接渲染控件树 + 背景
        if (win.Border == WindowBorder.None)
        {
            if (fillBg > 0)
                for (int r = 0; r < win.Height; r++)
                {
                    int screenY = win.Y + r;
                    if (screenY < 0 || screenY >= TH) continue;
                    sb.Append($"\x1b[{screenY + 1};{win.X + 1}H");
                    sb.Append($"\x1b[{fillBg}m");
                    sb.Append(new string(' ', win.Width));
                }
            if (win.RootView.Children.Count > 0)
            {
                var savedEffectiveBg = TuiControl.EffectiveBg;
                TuiControl.EffectiveBg = fillBg;
                win.RootView.Render(sb, win.X, win.Y);
                TuiControl.EffectiveBg = savedEffectiveBg;
            }
            else if (win.ContentLines.Count > 0)
                for (int i = 0; i < Math.Min(win.ContentLines.Count, win.Height); i++)
                    WriteAt(sb, win.Y + i, win.X, win.ContentLines[i], win.ContentFg);
            return;
        }

        var (tl, tr, bl, br, hh, vv) = win.GetBorderChars();

        // ── 上边框 + 标题栏 ──
        WriteAt(sb, win.Y, win.X, tl, bc, fillBg);

        bool drawTitle = win.ShowTitle && !string.IsNullOrEmpty(win.Title);
        if (drawTitle)
        {
            var titleText = $" {win.Title} ";
            WriteAt(sb, win.Y, win.X + 1, titleText, win.TitleFg > 0 ? win.TitleFg : bc, win.TitleBg > 0 ? win.TitleBg : fillBg);
            var rem = win.Width - 2 - TuiHelper.DisplayWidth(titleText);
            if (rem > 0) WriteAt(sb, win.Y, win.X + 1 + TuiHelper.DisplayWidth(titleText), new string(hh[0], rem), bc, fillBg);
        }
        else
        {
            WriteAt(sb, win.Y, win.X + 1, new string(hh[0], win.Width - 2), bc, fillBg);
        }
        WriteAt(sb, win.Y, win.X + win.Width - 1, tr, bc, fillBg);

        int contentTop = win.Y + 1;       // 上边框下面一行
        int innerHeight = win.Height - 2; // 边框内部高度

        // 如果有标题栏，标题行下面是分隔线，内容再下一行
        if (drawTitle)
        {
            WriteAt(sb, win.Y + 1, win.X, vv, bc, fillBg);
            WriteAt(sb, win.Y + 1, win.X + 1, new string(hh[0], win.Width - 2), bc, fillBg);
            WriteAt(sb, win.Y + 1, win.X + win.Width - 1, vv, bc, fillBg);
            contentTop = win.Y + 2;
            innerHeight -= 1;
        }

        // ── 窗口背景填充（raw ANSI，不重置，底色持续到后续控件渲染）──
        if (fillBg > 0 && innerHeight > 0)
        {
            for (int r = 0; r < innerHeight; r++)
            {
                int screenY = contentTop + r;
                if (screenY < 0 || screenY >= TH) continue;
                // 直接写 ANSI：定位 + 设背景色 + 填空格，不重置
                sb.Append($"\x1b[{screenY + 1};{win.X + 2}H");
                sb.Append($"\x1b[{fillBg}m");
                sb.Append(new string(' ', win.Width - 2));
            }
        }

        // ── 竖边框：先于内容绘制（内容的光标位置不会被边框覆盖）──
        for (int i = 0; i < innerHeight; i++)
        {
            int row = contentTop + i;
            WriteAt(sb, row, win.X, vv, bc, fillBg);
            WriteAt(sb, row, win.X + win.Width - 1, vv, bc, fillBg);
        }

        // ── 内容区域 ──
        if (win.RootView.Children.Count > 0)
        {
            // 设置 RootView 尺寸为内容区，确保裁剪
            win.RootView.Width = win.ContentWidth;
            win.RootView.Height = innerHeight;

            // 控件树渲染：从内容区原点开始，传入窗口裁剪约束
            // 渲染在竖边框之后，控件 CursorAt 不会被边框覆盖
            // 设置 EffectiveBg，让控件的 WriteAt 自动继承窗口底色
            var savedEffectiveBg = TuiControl.EffectiveBg;
            TuiControl.EffectiveBg = fillBg;
            win.RootView.Render(sb, win.ContentLeft, contentTop,
                clipL: win.ContentLeft, clipT: contentTop,
                clipR: win.ContentLeft + win.ContentWidth,
                clipB: contentTop + innerHeight);
            TuiControl.EffectiveBg = savedEffectiveBg;
        }
        else if (win.ContentLines.Count > 0)
        {
            for (int i = 0; i < Math.Min(innerHeight, win.ContentLines.Count); i++)
            {
                int row = contentTop + i;
                var line = win.ContentLines[i];
                if (TuiHelper.DisplayWidth(line) > win.Width - 3)
                    line = TuiHelper.TruncateByWidth(line, win.Width - 3);
                WriteAt(sb, row, win.X + 1, $" {line}", win.ContentFg);
            }
        }

        // ── 底边框 ──
        WriteAt(sb, win.Y + win.Height - 1, win.X, bl, bc, fillBg);
        WriteAt(sb, win.Y + win.Height - 1, win.X + 1, new string(hh[0], win.Width - 2), bc, fillBg);
        WriteAt(sb, win.Y + win.Height - 1, win.X + win.Width - 1, br, bc, fillBg);
    }

    /// <summary>在指定位置写入 ANSI 文本</summary>
    protected static void WriteAt(StringBuilder sb, int row, int col, string text,
        int fg = 0, int bg = 0)
    {
        if (row < 0 || row >= TTY.Rows) return;
        var rb = new RenderBuffer();
        rb.Write(row, col, text, fg: fg, bg: bg);
        sb.Append(rb.ToString());
    }

    // ── 工厂方法 ──

    /// <summary>创建居中对话框</summary>
    public TuiWindow ShowDialog(string title, string content, int? width = null, int? height = null)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxLineVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var w = Math.Max(20, Math.Min(TTY.Cols - 8,
            width ?? Math.Max(maxLineVw + 4, TuiHelper.DisplayWidth(title) + 4)));
        var h = Math.Min(TTY.Rows - 6, height ?? Math.Max(3, lines.Length + 4));

        var win = new TuiWindow
        {
            Width = w, Height = h,
            Title = title,
            ContentLines = [..lines],
            Modal = true,
            HasMask = true,
            BorderColor = 36,
            WinBg = 0,     // 对话框默认透明背景
        };
        win.Center();
        AddWindow(win);
        return win;
    }

    /// <summary>创建通知提示框（右下角，自动消失）</summary>
    public TuiWindow ShowToast(string message, int durationMs = 2000)
    {
        var vw = TuiHelper.DisplayWidth(message);
        var w = Math.Min(TTY.Cols - 4, vw + 4);
        var win = new TuiWindow
        {
            X = TTY.Cols - w - 2, Y = TTY.Rows - 4,
            Width = w, Height = 3,
            ContentLines = [message],
            Modal = false,
            HasMask = false,
            BorderColor = 32, // Green
        };
        AddWindow(win);
        Task.Delay(durationMs).ContinueWith(_ => CloseWindow(win));
        return win;
    }
}
