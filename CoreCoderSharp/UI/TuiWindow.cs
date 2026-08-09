using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

/// <summary>
/// 窗口边框样式
/// </summary>
public enum WindowBorder
{
    None, Single, Double, Rounded, Thick,
    Solid, Dotted, Dashed, Ascii, Slash, Triangle
}

/// <summary>
/// 窗口 —— 带边框的浮层矩形区域，可模态/非模态，包含控件树。
/// 不是控件（不继承 TuiControl），由 TuiScreen 管理 Z-order。
///
/// 窗口特性：
/// - 默认有背景色（WinBg=7 浅灰），可设为 0 透明
/// - 可以有焦点（Focused），影响边框颜色
/// - 可以有标题栏（ShowTitle），独立于边框存在
/// - 可以有外框（Border），None 时无边框
/// </summary>
public class TuiWindow
{
    // ── 位置与大小 ──
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 30;
    public int Height { get; set; } = 10;

    // ── 标题 ──
    public string Title { get; set; } = "";

    /// <summary>是否显示标题栏。即使有标题文本，设为 false 也不显示标题栏。</summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>是否在标题栏下方绘制分隔线（默认 true；对话框通常设为 false）</summary>
    public bool ShowTitleSeparator { get; set; } = true;

    /// <summary>标题文字是否粗体</summary>
    public bool TitleBold { get; set; }

    // ── 焦点状态 ──

    /// <summary>窗口是否拥有焦点。影响边框渲染颜色。</summary>
    public bool Focused { get; set; }

    // ── 模态与遮罩 ──
    /// <summary>是否为模态窗口（阻塞下层输入）</summary>
    public bool Modal { get; set; }
    /// <summary>是否显示半透明遮罩覆盖下层</summary>
    public bool HasMask { get; set; }

    // ── Z-order ──
    /// <summary>层叠顺序（越大越在上）</summary>
    public int ZOrder { get; set; }

    // ── 边框 ──
    /// <summary>
    /// 窗口外框样式。WindowBorder.None 表示无边框。
    /// 无边框时内容区域 = 窗口区域，无标题栏。
    /// </summary>
    public WindowBorder Border { get; set; } = WindowBorder.Rounded;
    /// <summary>边框颜色（ANSI 色码）。聚焦时自动加亮。</summary>
    public int BorderColor { get; set; } = 36;    // Cyan

    /// <summary>失焦时边框颜色（0=自动使用 BorderColor 暗色版）</summary>
    public int UnfocusedBorderColor { get; set; }

    public string CustomBorder { get; set; } = "";  // 6 字符自定义边框

    // ── 样式 ──
    /// <summary>窗口背景色（ANSI 色码，0=透明，默认 7=浅灰）</summary>
    public int WinBg { get; set; } = 47;

    /// <summary>标题前景色（0=使用边框色）</summary>
    public int TitleFg { get; set; }
    /// <summary>标题背景色（0=透明）</summary>
    public int TitleBg { get; set; }
    /// <summary>内容前景色</summary>
    public int ContentFg { get; set; } = 37;
    /// <summary>选项前景色</summary>
    public int ItemFg { get; set; }
    /// <summary>选中项前景/背景色</summary>
    public int SelFg { get; set; } = 30;
    public int SelBg { get; set; } = 46;

    // ── 控件树 ──
    /// <summary>窗口根视图</summary>
    public TuiView RootView { get; set; } = new TuiVBox();

    // ── 内容文本（简单模式，无控件树时使用） ──
    public List<string> ContentLines { get; set; } = [];

    // ── 对话框结果 ──
    /// <summary>
    /// 对话框返回值。默认 -1 表示未选择/取消，有效选择 ≥ 0。
    /// 弹窗代码读取此属性获取用户选择。
    /// </summary>
    public object? Result { get; set; } = -1;

    // ── 键盘快捷键 ──
    /// <summary>
    /// 窗口级键盘快捷键映射。优先于控件树路由。
    /// 对话框注册 Y/N/A/Enter/Esc 等快捷键，用户无需 Tab 切换到按钮即可触发。
    /// </summary>
    public Dictionary<ConsoleKey, Action> KeyShortcuts { get; } = [];

    /// <summary>注册一个键盘快捷键</summary>
    public void RegisterShortcut(ConsoleKey key, Action action)
    {
        KeyShortcuts[key] = action;
    }

    /// <summary>
    /// 对话框是否用方向键（↑↓）在控件间移动焦点。
    /// 设为 true 时，Up/Down 箭头的效果与 Tab/Shift+Tab 相同。
    /// 默认为 false，由各对话框按需开启。
    /// </summary>
    public bool ArrowKeysNavigate { get; set; }

    // ── 关闭回调 ──
    public Action? OnClosed { get; set; }

    // ── 边框字符解析 ──
    public (string tl, string tr, string bl, string br, string h, string v) GetBorderChars() => Border switch
    {
        WindowBorder.None     => (" ", " ", " ", " ", " ", " "),
        WindowBorder.Double   => ("╔", "╗", "╚", "╝", "═", "║"),
        WindowBorder.Thick    => ("┏", "┓", "┗", "┛", "━", "┃"),
        WindowBorder.Single   => ("┌", "┐", "└", "┘", "─", "│"),
        WindowBorder.Solid    => ("█", "█", "█", "█", "█", "█"),
        WindowBorder.Dotted   => ("┌", "┐", "└", "┘", "┄", "┆"),
        WindowBorder.Dashed   => ("┌", "┐", "└", "┘", "┅", "┇"),
        WindowBorder.Ascii    => ("+", "+", "+", "+", "-", "|"),
        WindowBorder.Slash    => ("/", "\\", "\\", "/", "-", "|"),
        WindowBorder.Triangle => ("▶", "◀", "◀", "▶", "─", "│"),
        _                     => ("╭", "╮", "╰", "╯", "─", "│"), // Rounded
    };

    /// <summary>当前有效边框色（考虑焦点状态）</summary>
    public int EffectiveBorderColor => Focused ? BorderColor :
        UnfocusedBorderColor > 0 ? UnfocusedBorderColor :
        BorderColor > 0 ? (BorderColor % 2 == 0 ? BorderColor : BorderColor - 1) : // 暗一档
        BorderColor;

    /// <summary>内容可绘制区域左边界（不含边框）</summary>
    public int ContentLeft => Border == WindowBorder.None ? X : X + 1;
    /// <summary>内容可绘制区域上边界（不含边框，不含标题栏）</summary>
    public int ContentTop
    {
        get
        {
            if (Border == WindowBorder.None) return Y;
            // 有边框 + 标题栏 → 跳过标题行
            return ShowTitle && !string.IsNullOrEmpty(Title) ? Y + 2 : Y + 1;
        }
    }
    /// <summary>内容可绘制区域宽度（不含边框）</summary>
    public int ContentWidth  => Border == WindowBorder.None ? Width : Width - 2;
    /// <summary>内容可绘制区域高度（不含边框和标题栏）</summary>
    public int ContentHeight
    {
        get
        {
            if (Border == WindowBorder.None) return Height;
            var h = Height - 2; // 上下边框
            if (ShowTitle && !string.IsNullOrEmpty(Title)) h -= 1; // 标题行
            return Math.Max(0, h);
        }
    }

    /// <summary>窗口是否在终端 resize 时自动居中</summary>
    public bool AutoCenter { get; set; } = true;

    // ── 鼠标拖拽与缩放 ──

    /// <summary>是否允许鼠标拖拽标题栏移动窗口</summary>
    public bool IsMoveable { get; set; } = true;

    /// <summary>是否允许鼠标拖拽边缘调整窗口大小</summary>
    public bool IsResizeable { get; set; } = true;

    /// <summary>最小窗口尺寸</summary>
    public int MinWidth { get; set; } = 12;
    public int MinHeight { get; set; } = 3;

    /// <summary>最大窗口尺寸（0=无限制）</summary>
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }

    /// <summary>拖拽/缩放的边缘检测宽度（像素）</summary>
    public int ResizeBorderWidth { get; set; } = 2;

    // 拖拽/缩放状态
    private bool _dragging;
    private int _dragStartX, _dragStartY;  // 鼠标按下时的终端坐标
    private int _winStartX, _winStartY;    // 鼠标按下时的窗口左上角
    private int _winStartW, _winStartH;    // 鼠标按下时的窗口尺寸
    private ResizeEdge _resizeEdge = ResizeEdge.None;

    /// <summary>缩放边缘方向</summary>
    public enum ResizeEdge
    {
        None,
        Top, Bottom, Left, Right,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    /// <summary>当前激活的缩放边缘（渲染边框高亮用）</summary>
    public ResizeEdge ActiveResizeEdge => _resizeEdge != ResizeEdge.None || _dragging
        ? _resizeEdge : ResizeEdge.None;

    // ── 方法 ──

    /// <summary>让窗口居中于终端</summary>
    public void Center()
    {
        X = (TTY.Cols - Width) / 2;
        Y = (TTY.Rows - Height) / 2;
    }

    /// <summary>
    /// 终端尺寸变化通知。重新计算窗口位置、大小，
    /// 并将变化传播给控件树。
    /// </summary>
    public virtual void OnResize(int newTermW, int newTermH)
    {
        // 自动居中
        if (AutoCenter)
            Center();

        // 通知 RootView 重算布局
        RootView.Width = ContentWidth;
        RootView.Height = ContentHeight;
        RootView.OnResize(ContentWidth, ContentHeight);
    }

    /// <summary>路由按键到控件树。快捷键优先于控件路由。</summary>
    public bool OnKey(ConsoleKeyInfo key)
    {
        // ── 1. 窗口级快捷键（优先，无需控件焦点）──
        if (KeyShortcuts.Count > 0)
        {
            // 先精确匹配 ConsoleKey（区分大小写和修饰符）
            if (KeyShortcuts.TryGetValue(key.Key, out var action))
            {
                action();
                return true;
            }
            // 如果 KeyChar 是字母，再尝试以大写 ConsoleKey 匹配（'y'→ConsoleKey.Y）
            if (key.KeyChar >= 'a' && key.KeyChar <= 'z')
            {
                var upperKey = (ConsoleKey)char.ToUpperInvariant(key.KeyChar);
                if (KeyShortcuts.TryGetValue(upperKey, out var upperAction))
                {
                    upperAction();
                    return true;
                }
            }
        }

        // ── 2. 先路由到控件树，让控件先处理（输入框方向键、列表导航等）──
        if (RootView.OnKey(key))
            return true;

        // ── 3. 控件未处理的方向键 → 在控件间移动焦点（对话框开启时）──
        if (ArrowKeysNavigate)
        {
            if (key.Key == ConsoleKey.UpArrow) { FocusPrev(); return true; }
            if (key.Key == ConsoleKey.DownArrow) { FocusNext(); return true; }
        }

        return false;
    }

    /// <summary>
    /// 处理鼠标事件。返回 true 表示事件被窗口消费。
    /// 支持：标题栏拖拽移动、边缘拖拽缩放。
    /// </summary>
    public bool HandleMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse) return false;

        bool insideX = ev.MouseX >= X && ev.MouseX < X + Width;
        bool insideY = ev.MouseY >= Y && ev.MouseY < Y + Height;
        bool inside = insideX && insideY;

        // ── 鼠标释放：停止拖拽/缩放 ──
        if (ev.MouseRelease)
        {
            _dragging = false;
            _resizeEdge = ResizeEdge.None;
            if (inside) return true;
            return false;
        }

        // ── 持续拖拽中（鼠标移动）──
        if (_dragging)
        {
            int dx = ev.MouseX - _dragStartX;
            int dy = ev.MouseY - _dragStartY;
            X = Math.Clamp(_winStartX + dx, 0, TTY.Cols - Width);
            Y = Math.Clamp(_winStartY + dy, 0, TTY.Rows - Height);
            return true;
        }

        // ── 持续缩放中 ──
        if (_resizeEdge != ResizeEdge.None)
        {
            int dx = ev.MouseX - _dragStartX;
            int dy = ev.MouseY - _dragStartY;
            ApplyResize(_resizeEdge, dx, dy);
            return true;
        }

        // ── 鼠标按下 ──
        if (ev.MouseLeft)
        {
            // 1. 检测边框缩放
            if (IsResizeable)
            {
                var edge = DetectResizeEdge(ev.MouseX, ev.MouseY);
                if (edge != ResizeEdge.None)
                {
                    _resizeEdge = edge;
                    _dragStartX = ev.MouseX;
                    _dragStartY = ev.MouseY;
                    _winStartX = X;
                    _winStartY = Y;
                    _winStartW = Width;
                    _winStartH = Height;
                    return true;
                }
            }

            // 2. 检测标题栏拖拽
            if (IsMoveable && InsideTitleBar(ev.MouseX, ev.MouseY))
            {
                _dragging = true;
                _dragStartX = ev.MouseX;
                _dragStartY = ev.MouseY;
                _winStartX = X;
                _winStartY = Y;
                return true;
            }

            // 点击在窗口内，消费事件
            if (inside) return true;
        }

        return false;
    }

    /// <summary>检测鼠标位置对应的缩放边缘</summary>
    private ResizeEdge DetectResizeEdge(int mx, int my)
    {
        int rbw = ResizeBorderWidth;
        bool onLeft   = mx >= X && mx < X + rbw;
        bool onRight  = mx >= X + Width - rbw && mx < X + Width;
        bool onTop    = my >= Y && my < Y + rbw;
        bool onBottom = my >= Y + Height - rbw && my < Y + Height;

        if (onTop && onLeft)     return ResizeEdge.TopLeft;
        if (onTop && onRight)    return ResizeEdge.TopRight;
        if (onBottom && onLeft)  return ResizeEdge.BottomLeft;
        if (onBottom && onRight) return ResizeEdge.BottomRight;
        if (onTop)               return ResizeEdge.Top;
        if (onBottom)            return ResizeEdge.Bottom;
        if (onLeft)              return ResizeEdge.Left;
        if (onRight)             return ResizeEdge.Right;

        return ResizeEdge.None;
    }

    /// <summary>鼠标是否在标题栏区域</summary>
    private bool InsideTitleBar(int mx, int my)
    {
        if (my != Y) return false;
        // 标题栏在窗口最顶行（有边框时即上边框行），X 在窗口范围内
        return mx >= X && mx < X + Width;
    }

    /// <summary>根据拖拽增量应用缩放</summary>
    private void ApplyResize(ResizeEdge edge, int dx, int dy)
    {
        int newW = Width, newH = Height, newX = X, newY = Y;

        switch (edge)
        {
            case ResizeEdge.Right:
            case ResizeEdge.TopRight:
            case ResizeEdge.BottomRight:
                newW = _winStartW + dx;
                break;
            case ResizeEdge.Left:
            case ResizeEdge.TopLeft:
            case ResizeEdge.BottomLeft:
                newW = _winStartW - dx;
                newX = _winStartX + dx;
                break;
        }

        switch (edge)
        {
            case ResizeEdge.Bottom:
            case ResizeEdge.BottomLeft:
            case ResizeEdge.BottomRight:
                newH = _winStartH + dy;
                break;
            case ResizeEdge.Top:
            case ResizeEdge.TopLeft:
            case ResizeEdge.TopRight:
                newH = _winStartH - dy;
                newY = _winStartY + dy;
                break;
        }

        // 尺寸约束
        newW = Math.Clamp(newW, MinWidth, MaxWidth > 0 ? MaxWidth : TTY.Cols);
        newH = Math.Clamp(newH, MinHeight, MaxHeight > 0 ? MaxHeight : TTY.Rows);

        // 位置约束
        if (newX < 0) { newW += newX; newX = 0; }
        if (newY < 0) { newH += newY; newY = 0; }
        if (newX + newW > TTY.Cols) newW = TTY.Cols - newX;
        if (newY + newH > TTY.Rows) newH = TTY.Rows - newY;

        Width = newW; Height = newH; X = newX; Y = newY;

        // 通知 RootView 尺寸变化
        RootView.Width = ContentWidth;
        RootView.Height = ContentHeight;
        RootView.OnResize(ContentWidth, ContentHeight);
    }

    /// <summary>将焦点移到下一个可聚焦控件</summary>
    public void FocusNext() => RootView.FocusNext();

    /// <summary>将焦点移到上一个可聚焦控件</summary>
    public void FocusPrev() => RootView.FocusPrev();

    /// <summary>查找当前焦点控件</summary>
    public TuiControl? FocusedControl => RootView.FindFocused();

    /// <summary>将所有子控件的 Focused 置为 false</summary>
    public void ClearFocus()
    {
        foreach (var c in RootView.GetAllFocusable())
            c.Focused = false;
    }
}
