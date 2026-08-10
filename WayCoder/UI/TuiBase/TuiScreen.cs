using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 屏幕 —— 一个完整的终端场景。
/// 持有根视图（内联控件树）和浮层窗口列表。
/// </summary>
public abstract class TuiScreen : TuiBase
{
    /// <summary>所属管理器引用（由 PushScreen 自动设置）</summary>
    public TuiManager? Manager { get; set; }

    /// <summary>标记需要重绘（通知 Manager + 根视图）</summary>
    public override void MarkDirty() {
        if (Manager != null) Manager.IsDirty = true;
        RootView.IsDirty = true;
        base.MarkDirty();
    }

    /// <summary>强制全屏刷新：递归标记所有控件为脏，确保下一帧完全重绘。</summary>
    public void InvalidateView() {
        RootView.Invalidate();
        MarkDirty();
    }

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

    /// <summary>是否为增量更新（仅脏控件刷新，跳过全屏清除和窗口背景/边框重绘）</summary>
    public bool IsIncrementalUpdate { get; set; }

    /// <summary>当前终端尺寸</summary>
    public int TW { get; protected set; }
    public int TH { get; protected set; }

    /// <summary>最近一次 Escape 关闭模态框的时间戳（防按键重复触发退出框）</summary>
    public DateTime LastModalEscapeTime { get; protected set; } = DateTime.MinValue;

    // ── 生命周期 ──

    /// <summary>屏幕激活时调用（初始化控件树、设置布局）</summary>
    public virtual void Activate()
    {
        TW = Tty.Cols;
        TH = Tty.Rows;
        RootView.Width = TW;
        RootView.Height = TH;
        RootView.Layout();
        OnCreate();
    }

    /// <summary>屏幕失活时调用</summary>
    public virtual void Deactivate()
    {
        OnDestroy();
        Windows.Clear();
        FocusedWindow = null;
    }

    /// <summary>屏幕创建 —— 递归初始化 RootView 和所有浮层窗口</summary>
    public override void OnCreate()
    {
        RootView.OnCreate();
        foreach (var win in Windows) win.OnCreate();
    }

    /// <summary>屏幕销毁 —— 递归清理所有浮层窗口和 RootView</summary>
    public override void OnDestroy()
    {
        foreach (var win in Windows) win.OnDestroy();
        RootView.OnDestroy();
        _savedRootFocus = null;
    }

    /// <summary>终端尺寸变化。递归通知根视图和所有浮层窗口。</summary>
    public override void OnResize(int newW, int newH)
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
        win.OnCreate();
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

        win.OnDestroy();
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
    /// 优先级：模态窗口内的焦点控件 → 根视图的焦点控件（仅当无浮层窗口时）。
    /// 有浮层窗口（含 Toast）时，隐藏根视图光标，防止白块穿透。
    /// </summary>
    public void SetCursorOwner()
    {
        // 清除旧所有者
        if (_cursorOwner != null)
            _cursorOwner.IsCursorOwner = false;

        TuiControl? focused = null;
        if (HasModal && FocusedWindow != null)
        {
            focused = FocusedWindow.FocusedControl;
        }
        else if (Windows.Count == 0)
        {
            // 仅在没有浮层窗口时才从根视图取光标（避免 Toast 等窗口下穿透显示）
            focused = RootView.FindFocused();
        }

        // 仅当焦点控件是输入类控件（HasCursor=true）时才赋予光标
        _cursorOwner = (focused != null && focused.IsEnabled && focused.HasCursor) ? focused : null;
        if (_cursorOwner != null)
            _cursorOwner.IsCursorOwner = true;
    }

    // ── 鼠标 ──

    /// <summary>
    /// 处理鼠标事件。优先级：顶层模态窗口 → 顶层窗口（Z-order）→ 根视图。
    /// 返回 true 表示事件已被消费。
    /// </summary>
    public override bool HandleMouse(InputEvent ev)
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

        // Fallback：路由给根视图（控件级鼠标交互）
        return RootView.HandleMouse(ev);
    }

    // ── 输入 ──

    /// <summary>
    /// 处理按键。返回 true 表示已处理。
    /// </summary>
    public override bool OnKey(ConsoleKeyInfo key)
    {
        // ── 屏幕自身处理：Escape 关模态、Tab 切焦点 ──

        // Esc 路由：先给模态窗口处理（快捷键拦截），未处理则关闭窗口
        if (key.Key == ConsoleKey.Escape && HasModal)
        {
            var topModal = Windows.LastOrDefault(w => w.Modal);
            if (topModal != null)
            {
                // 窗口级快捷键优先（如 Esc 注册为取消回调）
                if (topModal.OnKey(key))
                    return true;
                // 未处理 → 默认关闭（OnClosed 触发 ChatScreen 的 RenderWait 退出）
                topModal.OnClosed?.Invoke();
                LastModalEscapeTime = DateTime.UtcNow; // 记录时间戳，防按键重复
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

        // ── 路由到子节点：模态窗口 → 焦点窗口 → 根视图 ──
        if (FocusedWindow?.Modal == true)
            return FocusedWindow.OnKey(key);

        if (FocusedWindow != null && FocusedWindow.OnKey(key))
            return true;

        return RootView.OnKey(key);
    }

    // ── 渲染 ──

    /// <summary>
    /// 渲染整个屏幕（根视图 + 浮层窗口）到 StringBuilder。
    /// 增量模式下仅渲染脏控件，跳过遮罩和窗口背景/边框重绘。
    /// </summary>
    public virtual void Render(StringBuilder sb)
    {
        bool incremental = IsIncrementalUpdate;

        // 1. 渲染根视图（光标先隐藏，仅记录位置）
        //    增量模式：仅脏控件输出；全量模式：全量输出
        RootView.Render(sb, 0, 0);

        // 2. 渲染模态窗口遮罩（全量模式才需要，增量模式遮罩未变）
        if (!incremental)
        {
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
        }

        // 3. 按 Z-order 渲染窗口
        foreach (var win in Windows.OrderBy(w => w.ZOrder))
        {
            // 增量模式 + 窗口整体无变化 → 仅渲染窗口内脏控件
            if (incremental && !win.RootView.IsDirty)
            {
                RenderWindowDirtyControls(sb, win);
            }
            else
            {
                RenderWindow(sb, win);
            }
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

        // 5. 渲染完成后清除脏标记
        ClearDirtyRecursive(RootView);
    }

    /// <summary>递归清除控件树的脏标记</summary>
    private static void ClearDirtyRecursive(TuiControl control)
    {
        control.ClearDirty();
        if (control is TuiView view)
        {
            foreach (var child in view.Children)
                ClearDirtyRecursive(child);
        }
    }

    /// <summary>
    /// 增量渲染窗口内脏控件 —— 不重绘背景、边框、遮罩。
    /// 仅对 RootView 中标记为脏的子控件（如焦点切换的按钮）进行渲染。
    /// </summary>
    private void RenderWindowDirtyControls(StringBuilder sb, TuiWindow win)
    {
        if (win.RootView == null) return;

        int contentTop = win.ContentTop;
        int innerHeight = win.ContentHeight;
        if (innerHeight <= 0) return;

        // 设置裁剪区域 = 窗口内容区
        var savedEffectiveBg = TuiControl.CascadedBg;
        int fillBg = win.WinBg > 0 ? win.WinBg : 100;
        TuiControl.CascadedBg = fillBg;

        win.RootView.Width = win.ContentWidth;
        win.RootView.Height = innerHeight;
        win.RootView.Render(sb, win.ContentLeft, contentTop,
            clipL: win.ContentLeft, clipT: contentTop,
            clipR: win.ContentLeft + win.ContentWidth,
            clipB: contentTop + innerHeight);

        TuiControl.CascadedBg = savedEffectiveBg;
    }

    /// <summary>渲染单个窗口（边框 + 标题栏 + 内部控件树）</summary>
    protected virtual void RenderWindow(StringBuilder sb, TuiWindow win)
    {
        int bc = win.EffectiveBorderColor;
        int fillBg = win.WinBg > 0 ? win.WinBg : 100; // WinBg=0 时透明不填充

        // 无边框模式：直接渲染控件树 + 背景
        if (win.Border == WindowBorder.None)
        {
            if (fillBg > 0)
                for (int r = 0; r < win.Height; r++)
                {
                    int screenY = win.Y + r;
                    if (screenY < 0 || screenY >= TH) continue;
                    sb.Append(AnsiTty.CursorPos(screenY + 1, win.X + 1));
                    sb.Append(AnsiTty.BgCode(fillBg));
                    sb.Append(new string(' ', win.Width));
                }
            // 总是渲染 RootView（无边框窗口）。无子控件时 RootView 自身
            // 的 OnRender 可能绘制自定义内容（如 MenuView 直接画菜单项）。
            {
                var savedEffectiveBg = TuiControl.CascadedBg;
                TuiControl.CascadedBg = fillBg;
                win.RootView.Render(sb, win.X, win.Y);
                TuiControl.CascadedBg = savedEffectiveBg;
            }
            if (win.ContentLines.Count > 0)
            {
                int toastBg = win.WinBg > 0 ? win.WinBg : 100;
                for (int i = 0; i < Math.Min(win.ContentLines.Count, win.Height); i++)
                    WriteAt(sb, win.Y + i, win.X, win.ContentLines[i], win.ContentFg, toastBg);
            }
            return;
        }

        var (tl, tr, bl, br, hh, vv, hTop, hBot) = win.GetBorderChars();

        // 渐变色模式
        bool grad = win.GradientBorder && win.GradientStart >= 0x1000000 && win.GradientEnd >= 0x1000000;
        int gs = win.GradientStart, ge = win.GradientEnd;

        // ── 上边框 + 标题栏 ──
        bool drawTitle = win.ShowTitle && !string.IsNullOrEmpty(win.Title);
        if (grad)
        {
            // ── 渐变上边框（文字与线框一起渐变）──
            if (drawTitle && !win.TitleBold)
            {
                // 标题嵌在渐变横线上，居中
                var titleText = $" {win.Title} ";
                int titleVw = TuiHelper.DisplayWidth(titleText);
                int innerW = win.Width - 2;
                int leftPad = (innerW - titleVw) / 2;
                int rightPad = innerW - titleVw - leftPad;

                // 左角
                WriteAt(sb, win.Y, win.X, tl, gs, fillBg);
                // 标题左侧横线（start → 标题位置的渐变色）
                if (leftPad > 0)
                {
                    float leftEndT = (float)leftPad / Math.Max(1, innerW - 1);
                    for (int i = 0; i < leftPad; i++)
                    {
                        float t = leftPad > 1 ? leftEndT * i / (leftPad - 1) : 0;
                        WriteAt(sb, win.Y, win.X + 1 + i, hTop, AnsiTty.LerpRgb(gs, ge, t), fillBg);
                    }
                }
                // 标题（渐变中间色 ≈50%）
                int tFg = win.TitleFg > 0 ? win.TitleFg : AnsiTty.LerpRgb(gs, ge, 0.5f);
                int tBg = win.TitleBg > 0 ? win.TitleBg : fillBg;
                WriteAt(sb, win.Y, win.X + 1 + leftPad, titleText, tFg, tBg);
                // 标题右侧横线（标题位置 → end 的渐变色）
                if (rightPad > 0)
                {
                    float rightStartT = (float)(leftPad + titleVw) / Math.Max(1, innerW - 1);
                    for (int i = 0; i < rightPad; i++)
                    {
                        float t = rightPad > 1 ? rightStartT + (1 - rightStartT) * i / (rightPad - 1) : rightStartT;
                        WriteAt(sb, win.Y, win.X + 1 + leftPad + titleVw + i, hTop, AnsiTty.LerpRgb(gs, ge, t), fillBg);
                    }
                }
                // 右角
                WriteAt(sb, win.Y, win.X + win.Width - 1, tr, ge, fillBg);
            }
            else if (drawTitle && win.TitleBold)
            {
                // 粗体标题独占第二行 → 边框行纯渐变线
                WriteGradientHLine(sb, win.Y, win.X, win.Width, tl, hTop, tr, gs, ge, fillBg);
                var titleText = $" {win.Title} ";
                int tFg = win.TitleFg > 0 ? win.TitleFg : gs;
                int tBg = win.TitleBg > 0 ? win.TitleBg : fillBg;
                sb.Append(AnsiTty.CursorPos(win.Y + 1, win.X + 2));
                sb.Append(AnsiTty.BoldFg(tFg));
                if (tBg > 0) sb.Append(AnsiTty.BgCode(tBg));
                sb.Append(titleText);
                sb.Append(AnsiTty.SgrReset);
            }
            else
            {
                // 无标题：整行渐变线
                WriteGradientHLine(sb, win.Y, win.X, win.Width, tl, hTop, tr, gs, ge, fillBg);
            }
        }
        else
        {
            // ── 非渐变上边框（原逻辑）──
            WriteAt(sb, win.Y, win.X, tl, bc, fillBg);
            if (drawTitle)
            {
                var titleText = $" {win.Title} ";
                int tFg = win.TitleFg > 0 ? win.TitleFg : bc;
                int tBg = win.TitleBg > 0 ? win.TitleBg : fillBg;
                if (win.TitleBold)
                {
                    sb.Append(AnsiTty.CursorPos(win.Y + 1, win.X + 2));
                    sb.Append(AnsiTty.BoldFg(tFg));
                    if (tBg > 0) sb.Append(AnsiTty.BgCode(tBg));
                    sb.Append(titleText);
                    sb.Append(AnsiTty.SgrReset);
                }
                else
                {
                    WriteAt(sb, win.Y, win.X + 1, titleText, tFg, tBg);
                }
                var rem = win.Width - 2 - TuiHelper.DisplayWidth(titleText);
                if (rem > 0) WriteAt(sb, win.Y, win.X + 1 + TuiHelper.DisplayWidth(titleText), new string(hTop[0], rem), bc, fillBg);
            }
            else
            {
                WriteAt(sb, win.Y, win.X + 1, new string(hTop[0], win.Width - 2), bc, fillBg);
            }
            WriteAt(sb, win.Y, win.X + win.Width - 1, tr, bc, fillBg);
        }

        int contentTop = win.Y + 1;       // 上边框下面一行
        int innerHeight = win.Height - 2; // 边框内部高度

        // 标题栏分隔线（仅当 ShowTitleSeparator 时绘制）
        if (drawTitle && win.ShowTitleSeparator)
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
                sb.Append(AnsiTty.CursorPos(screenY + 1, win.ContentLeft + 1));
                sb.Append(AnsiTty.BgCode(fillBg));
                sb.Append(new string(' ', win.ContentWidth));
            }
        }

        // ── 竖边框：左=渐变起始色，右=渐变终止色，背景用窗口底色 ──
        if (grad)
        {
            for (int i = 0; i < innerHeight; i++)
            {
                int row = contentTop + i;
                WriteAt(sb, row, win.X, vv, gs, fillBg);
                WriteAt(sb, row, win.X + win.Width - 1, vv, ge, fillBg);
            }
        }
        else
        {
            for (int i = 0; i < innerHeight; i++)
            {
                int row = contentTop + i;
                WriteAt(sb, row, win.X, vv, bc, fillBg);
                WriteAt(sb, row, win.X + win.Width - 1, vv, bc, fillBg);
            }
        }

        // ── 内容区域 ──
        // 总是渲染 RootView。无子控件时 RootView 自身的 OnRender
        // 可能绘制自定义内容（如 MenuView 直接画菜单项）。
        {
            // 设置 RootView 尺寸为内容区，确保裁剪
            win.RootView.Width = win.ContentWidth;
            win.RootView.Height = innerHeight;

            // 控件树渲染：从内容区原点开始，传入窗口裁剪约束
            // 渲染在竖边框之后，控件 CursorAt 不会被边框覆盖
            // 设置 CascadedBg，让控件的 WriteAt 自动继承窗口底色
            var savedEffectiveBg = TuiControl.CascadedBg;
            TuiControl.CascadedBg = fillBg;
            win.RootView.Render(sb, win.ContentLeft, contentTop,
                clipL: win.ContentLeft, clipT: contentTop,
                clipR: win.ContentLeft + win.ContentWidth,
                clipB: contentTop + innerHeight);
            TuiControl.CascadedBg = savedEffectiveBg;
        }
        if (win.ContentLines.Count > 0)
        {
            for (int i = 0; i < Math.Min(innerHeight, win.ContentLines.Count); i++)
            {
                int row = contentTop + i;
                var line = win.ContentLines[i];
                if (TuiHelper.DisplayWidth(line) > win.Width - 3)
                    line = TuiHelper.TruncateByWidth(line, win.Width - 3);
                WriteAt(sb, row, win.X + 1, $" {line}", win.ContentFg, fillBg);
            }
        }

        // ── 底边框：背景统一用窗口底色，不污染相邻区域 ──
        if (grad)
        {
            WriteAt(sb, win.Y + win.Height - 1, win.X, bl, gs, fillBg);
            // 中间横线渐变色
            int midLen = win.Width - 2;
            for (int i = 0; i < midLen; i++)
            {
                float t = midLen > 1 ? (float)i / (midLen - 1) : 0;
                WriteAt(sb, win.Y + win.Height - 1, win.X + 1 + i, hBot, AnsiTty.LerpRgb(gs, ge, t), fillBg);
            }
            WriteAt(sb, win.Y + win.Height - 1, win.X + win.Width - 1, br, ge, fillBg);
        }
        else
        {
            WriteAt(sb, win.Y + win.Height - 1, win.X, bl, bc, fillBg);
            WriteAt(sb, win.Y + win.Height - 1, win.X + 1, new string(hBot[0], win.Width - 2), bc, fillBg);
            WriteAt(sb, win.Y + win.Height - 1, win.X + win.Width - 1, br, bc, fillBg);
        }
    }

    /// <summary>绘制渐变水平线：左角 + N×横线(渐变色) + 右角</summary>
    private static void WriteGradientHLine(StringBuilder sb, int row, int col, int width,
        string leftChar, string midChar, string rightChar,
        int startColor, int endColor, int bg)
    {
        // 左角
        WriteAt(sb, row, col, leftChar, startColor, bg);
        // 中间横线逐字渐变色
        int midLen = width - 2;
        for (int i = 0; i < midLen; i++)
        {
            float t = midLen > 1 ? (float)i / (midLen - 1) : 0;
            int c = AnsiTty.LerpRgb(startColor, endColor, t);
            WriteAt(sb, row, col + 1 + i, midChar, c, bg);
        }
        // 右角
        WriteAt(sb, row, col + width - 1, rightChar, endColor, bg);
    }

    /// <summary>绘制渐变竖线：height 行逐行渐变色</summary>
    private static void WriteGradientVLine(StringBuilder sb, int startRow, int col, int height,
        string vChar, int startColor, int endColor, int bg)
    {
        for (int i = 0; i < height; i++)
        {
            float t = height > 1 ? (float)i / (height - 1) : 0;
            int c = AnsiTty.LerpRgb(startColor, endColor, t);
            WriteAt(sb, startRow + i, col, vChar, c, bg);
        }
    }

    /// <summary>在指定位置写入 ANSI 文本</summary>
    protected static void WriteAt(StringBuilder sb, int row, int col, string text,
        int fg = 0, int bg = 0)
    {
        if (row < 0 || row >= Tty.Rows) return;
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
        var w = Math.Max(20, Math.Min(Tty.Cols - 8,
            width ?? Math.Max(maxLineVw + 4, TuiHelper.DisplayWidth(title) + 4)));
        var h = Math.Min(Tty.Rows - 6, height ?? Math.Max(3, lines.Length + 4));

        var win = new TuiWindow
        {
            Width = w, Height = h,
            Title = title,
            ContentLines = [..lines],
            Modal = true,
            HasMask = true,
            BorderColor = TuiColors.Cyan,
            WinBg = TuiColors.BgWhite,    // 对话框默认背景
        };
        win.Center();
        AddWindow(win);
        return win;
    }

    /// <summary>创建通知提示框（右下角，自动消失）</summary>
    public TuiWindow ShowToast(string message, int durationMs = 2000)
    {
        var vw = TuiHelper.DisplayWidth(message);
        var w = Math.Min(Tty.Cols - 4, vw + 4);
        var win = new TuiWindow
        {
            X = Tty.Cols - w - 2, Y = Tty.Rows - 4,
            Width = w, Height = 3,
            ContentLines = [message],
            ContentFg = 37,
            Modal = false,
            HasMask = false,
            WinBg = TuiColors.BgBrightBlack,     // 深灰底色
            BorderColor = TuiColors.Green,
        };
        AddWindow(win);
        // 使用 Windows.Contains 守卫：屏幕销毁后不再关闭窗口
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            if (Windows.Contains(win)) CloseWindow(win);
        });
        return win;
    }
}
