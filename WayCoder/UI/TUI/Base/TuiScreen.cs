using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 屏幕 —— 一个完整的终端场景。
/// 持有根视图（内联控件树）和浮层窗口列表。
/// </summary>
public abstract class TuiScreen : TuiBase
{
    /// <summary>所属管理器引用（由 PushScreen 自动设置）</summary>
    public TuiManager? Manager { get; set; }

    /// <summary>标记需要重绘（通知 Manager + 根视图）</summary>
    public override void MarkDirty()
    {
        if (Manager != null) Manager.IsDirty = true;
        RootView.IsDirty = true;
        base.MarkDirty();
    }

    /// <summary>强制全屏刷新：递归标记所有控件为脏，确保下一帧完全重绘。</summary>
    public void InvalidateView()
    {
        RootView.Invalidate();
        MarkDirty();
    }

    /// <summary>立即渲染整个屏幕（便捷方法）</summary>
    public void Render()
    {
        Manager?.Render();
    }

    /// <summary>屏幕根视图（状态栏、主区域、输入区等）</summary>
    public TuiView RootView { get; set; } = new TuiVBox();

    // ── 跨线程安全 UI 调用 ──
    // 提炼到基类：任何后台线程（Agent 流式回调 / 工具执行 / 后台任务）都不允许直接碰控件树，
    // 只能 PostToUI 投递操作，由 UI 线程的渲染循环（REPL 主循环 / RunAgentWithRenderLoop / RenderWait）PumpUIQueue 消费。
    /// <summary>后台线程 → UI 线程消息队列：子线程永不直接碰控件树，只投递操作，UI 线程 PumpUIQueue 消费。</summary>
    private readonly ConcurrentQueue<Action> _uiQueue = new();

    /// <summary>UI 线程 ID（构造时捕获；PostToUI 据此判定直接执行还是投递）。所有 TuiScreen 都在 UI 线程构造。</summary>
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;

    /// <summary>
    /// 当前线程是否本屏幕所属 UI 线程。
    /// 渲染/读键/窗口栈只能有一个所有者：UI 线程调用对话框 RenderWait 时由本线程接管（主循环被阻塞）；
    /// 后台线程调用时必须只等待，由常驻主循环负责 —— 判定依据即此属性。
    /// </summary>
    public bool IsUiThread => Environment.CurrentManagedThreadId == _uiThreadId;

    /// <summary>
    /// 投递 UI 操作：UI 线程调用直接执行（无延迟），后台线程调用入队（UI 线程 PumpUIQueue 消费）。
    /// 这样所有 TuiScreen 子类的 UI 方法都能安全地从任意线程调用，控件树只被 UI 线程触碰。
    /// </summary>
    public void PostToUI(Action action)
    {
        if (action == null) return;
        if (Environment.CurrentManagedThreadId == _uiThreadId)
            action();
        else
            _uiQueue.Enqueue(action);
    }

    /// <summary>消费并执行 UI 操作队列（仅 UI 线程调用：REPL 主循环 / RunAgentWithRenderLoop / RenderWait）。</summary>
    public void PumpUIQueue()
    {
        while (_uiQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch { /* 单条操作失败不拖垮整帧 */ }
        }
    }

    /// <summary>浮层窗口列表</summary>
    public readonly List<TuiWindow> Windows = [];

    /// <summary>当前焦点窗口（键盘路由优先）</summary>
    public TuiWindow? FocusedWindow { get; set; }

    /// <summary>是否有模态窗口（窗口树任意节点为模态）</summary>
    public bool HasModal => Windows.Any(w => w.Modal || w.AnyDescendantModal());

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

    /// <summary>
    /// 浮层窗口可占用的顶部边界（含，即窗口须满足 Y ≥ 此值）。
    /// 默认等于 0；子类（如 ChatScreen）可覆盖以排除标题栏，
    /// 避免对话框顶边覆盖到标题栏。
    /// </summary>
    public virtual int OverlayTop => 0;

    /// <summary>
    /// 浮层窗口可占用的底部边界（不含，即窗口须满足 Y + Height ≤ 此值）。
    /// 默认等于终端高度；子类（如 ChatScreen）可覆盖以排除状态栏/输入区，
    /// 避免对话框底边碰撞到状态栏与输入框。
    /// </summary>
    public virtual int OverlayBottom => TH;

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

        // 2. 所有浮层窗口重新定位和布局（含子窗口子树，逐层钳制回内容区）
        foreach (var win in Windows)
        {
            win.OnResize(newW, newH);
            ClampTree(win);
        }
    }

    /// <summary>递归把窗口及其子窗口钳制回屏幕内容区。</summary>
    protected void ClampTree(TuiWindow win)
    {
        ClampOverlayToContent(win);
        foreach (var child in win.Children)
            ClampTree(child);
    }

    // ── 窗口管理 ──

    private int _nextZ;

    /// <summary>添加浮层窗口。
    /// 仅模态窗口自动挂为当前顶层模态的子窗口（「只能弹子窗口」）；非模态/无模态在场时挂为根窗口。
    /// 需要显式根挂的「替换型对话框」用 <see cref="AddRootWindow"/>（否则父窗关闭会递归把它一起关掉）。</summary>
    public TuiWindow AddWindow(TuiWindow win, TuiWindow? parent = null)
    {
        parent ??= win.Modal ? TopModal() : null;

        // ── 防环防御 ──
        // 窗口已在树中（已挂到某屏幕）时重复挂载，会在窗口树里形成环（A.Children∋B 且
        // B.Children∋A），渲染/键路由/关闭的递归遍历将无限递归 → 栈溢出 → 整机「死机」
        // （Ctrl+C 无效、无法输入）。摘除旧位置后再挂载：重复 ShowWindow 变成「移动窗口」。
        // 注意：旧屏可能是 RenderOnlyScreen（TuiDialog.Show 离屏预览）等临时屏，一律摘除允许跨屏挂载。
        if (win.Screen != null)
        {
            (win.Parent?.Children ?? win.Screen.Windows).Remove(win);
            win.Parent = null;
            win.Screen = null;
        }

        // 首个模态窗口出现时，保存并清除根视图焦点（防止光标穿透）
        if (win.Modal && !HasModal)
        {
            _savedRootFocus = RootView.FindFocused();
            if (_savedRootFocus != null) _savedRootFocus.Focused = false;
        }

        // 激活窗口获得焦点，之前的焦点窗口失焦。
        // 但非模态浮层（Toast 等）不得抢走模态对话框的焦点 —— 抢走了 OnKey 的模态分支就不成立，
        // 键会漏到根视图（表现为：对话框开着还能往聊天输入框里打字）。
        bool takesFocus = win.Modal || !HasModal;
        if (takesFocus)
        {
            if (FocusedWindow != null)
            {
                FocusedWindow.Focused = false;
                FocusedWindow.RootView.MarkDirty(); // 旧焦点窗边框焦点色要重绘
            }
            win.Focused = true;
        }

        win.Screen = this;
        win.Parent = parent;
        win.ZOrder = parent != null ? parent.AllocChildZ() : _nextZ++; // Z-order 仅同级比较
        if (parent != null) parent.Children.Add(win);
        else Windows.Add(win);
        if (takesFocus) FocusedWindow = win;
        win.OnCreate();
        win.OnResize(TW, TH);
        ClampOverlayToContent(win);

        // 兜底聚焦：窗口里没有任何控件带焦点时，TuiView.OnKey 只往「聚焦的子控件」派发，
        // 按键就无处可去，整个窗口键盘失灵（树形视图演示正是这样：建了控件却没人 Focused=true）。
        // 这里补一次「聚焦第一个可聚焦控件」，让忘了写 focused 的窗口也能用键盘。
        if (win.RootView?.FindFocused() == null)
            win.FocusNext();
        return win;
    }

    /// <summary>显式添加根窗口（不自动挂父）——供「替换型对话框」使用：
    /// 新对话框须比开启它的对话框长寿；若自动成为其子窗口，父关闭会递归把它一起关掉（如文件选择器的「输入路径」）。</summary>
    public TuiWindow AddRootWindow(TuiWindow win)
        => AddWindow(win, parent: null);

    /// <summary>
    /// 将浮层窗口的垂直位置收拢到内容区（OverlayTop..OverlayBottom）以内，
    /// 防止窗口顶边覆盖标题栏、底边碰撞状态栏/输入区。
    /// 窗口过高无法容纳时退化为贴顶（允许底部溢出，由菜单滚动等处理）。
    /// </summary>
    protected void ClampOverlayToContent(TuiWindow win)
    {
        int top = OverlayTop;
        int bottom = Math.Min(TH, OverlayBottom);
        int avail = bottom - top;
        if (avail <= 0) return;

        if (win.Y < top) win.Y = top;
        if (win.Y + win.Height > bottom)
            win.Y = win.Height <= avail ? bottom - win.Height : top;
    }

    /// <summary>添加浮层窗口并自动绑定关闭回调。保留调用方已设的 OnClosed（真正关闭后触发），
    /// 避免 EditorScreen 等「关闭后弹回上层屏幕」的回调被覆盖丢失。</summary>
    public void ShowWindow(TuiWindow win)
    {
        var userOnClosed = win.OnClosed;
        win.OnClosed = () =>
        {
            CloseWindow(win);
            userOnClosed?.Invoke();
        };
        AddWindow(win);
        MarkDirty();
    }

    /// <summary>关闭窗口。先递归关闭其所有子窗口（「关父窗 = 递归关子树」），再关自身。
    /// 背景快照回填机制已废弃，一律标记脏区：下一帧用默认背景擦除 + 裁剪重绘被遮挡的背景控件；
    /// 有残留窗口或关的是带子树的窗口时，整屏重绘兜底。</summary>
    public void CloseWindow(TuiWindow win) => CloseWindow(win, 0);

    private void CloseWindow(TuiWindow win, int depth)
    {
        if (depth > MaxTreeDepth) throw new InvalidOperationException("窗口树检测到环（CloseWindow）");
        // 先关子（子窗口永远在父之上，须先销毁），再关父。
        // hadChildren/subtreeHadModal 必须在关子之前捕获（关子后 Children 已空）
        bool hadChildren = win.Children.Count > 0;
        bool subtreeHadModal = win.Modal || win.AnyDescendantModal();
        foreach (var child in win.Children.ToList())
            CloseWindow(child, depth + 1);

        // 关窗刷新：记录脏区，下一帧用默认背景擦除窗口残影 + 裁剪重绘被遮挡的背景控件
        _dirtyRects.Add((win.X, win.Y, win.Width, win.Height));
        MarkDirtyInRect(RootView, 0, 0, win.X, win.Y, win.Width, win.Height);

        win.OnDestroy();
        win.Focused = false;
        (win.Parent?.Children ?? Windows).Remove(win);
        win.Parent = null;
        win.Screen = null;

        if (FocusedWindow == win)
        {
            FocusedWindow = FindTopMostWindow();
            if (FocusedWindow != null)
            {
                FocusedWindow.Focused = true;
                FocusedWindow.RootView.MarkDirty(); // 新焦点窗边框焦点色要重绘
            }
        }

        // 子树里曾有模态窗口且现已无任何模态 → 恢复根视图焦点（子窗自己的 CloseWindow 已处理的会置空，不重复）
        if (subtreeHadModal && !HasModal && _savedRootFocus != null)
        {
            _savedRootFocus.Focused = true;
            _savedRootFocus = null;
        }

        MarkDirty();

        // 多层/子树残留：关闭后仍有窗口留在其下方（尤其模态），须整屏重绘——
        // 增量 dirty-rect 只恢复根视图，无法重绘下层窗口的遮罩/边框/内容，会“穿透”。
        // 子窗口可能伸出父窗口矩形，脏区盖不到，也必须全刷。
        // 单层根窗口关闭保持增量恢复（只擦除该窗口区域），避免无谓闪烁。
        if (TotalWindowCount() > 0 || hadChildren)
            TuiManager.RequestFullRefresh();

        // 注意：不在此处调用 win.OnClosed，避免无限递归。
        // OnClosed 是窗口主动请求关闭的通知，由窗口内控件触发，
        // 调用方（如 TuiDialog 按钮）先触发 OnClosed，再到达此处。
    }

    /// <summary>
    /// 递归标记与指定矩形区域重叠的控件为脏。
    /// 通过 TuiView.EffectiveScrollOffset 正确处理滚动容器的坐标偏移。
    /// 同时标记 TuiView 容器自身，保证增量渲染时 parentDirty 级联生效。
    /// </summary>
    private static void MarkDirtyInRect(TuiControl control, int absX, int absY,
        int rectX, int rectY, int rectW, int rectH)
    {
        int left = absX + control.X;
        int top = absY + control.Y;
        int right = left + control.Width;
        int bottom = top + control.Height;

        // 不重叠则跳过整棵子树
        if (right <= rectX || left >= rectX + rectW ||
            bottom <= rectY || top >= rectY + rectH)
            return;

        // 标记自身（TuiView 容器也需要标记，保证 OnRender 中 parentDirty 级联）
        control.MarkDirty();

        if (control is TuiView view)
        {
            int scrollOff = view.EffectiveScrollOffset;
            foreach (var child in view.Children)
                MarkDirtyInRect(child, left, top - scrollOff,
                    rectX, rectY, rectW, rectH);
        }
    }

    /// <summary>
    /// 标记一个屏幕矩形区域需要重绘。浮层控件（如建议面板）移动/缩放/隐藏后，
    /// 其之前遮挡的根视图内容需补绘，否则会残留底色。
    /// </summary>
    protected void MarkDirtyRect(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        _dirtyRects.Add((x, y, w, h));
        MarkDirtyInRect(RootView, 0, 0, x, y, w, h);
    }

    /// <summary>关闭所有模态窗口（循环关栈顶，递归关子树）</summary>
    public void CloseAllModals()
    {
        while (TopModal() != null)
            CloseWindow(TopModal()!);
    }

    /// <summary>
    /// 在渲染前确定光标所有者。每屏只有一个控件拥有光标。
    /// 优先级：模态窗口内的焦点控件 → 根视图的焦点控件（仅当无浮层窗口时）。
    /// 有浮层窗口（含 Toast）时，隐藏根视图光标，防止白块穿透。
    /// </summary>
    public void SetCursorOwner()
    {
        // 清除旧所有者
        var oldOwner = _cursorOwner;
        if (oldOwner != null)
            oldOwner.IsCursorOwner = false;

        TuiControl? focused = null;
        if (HasModal && FocusedWindow != null)
        {
            focused = FocusedWindow.FocusedControl;
        }
        else if (TotalWindowCount() == 0)
        {
            // 仅在没有浮层窗口时才从根视图取光标（避免 Toast 等窗口下穿透显示）
            focused = RootView.FindFocused();
        }

        // 仅当焦点控件是输入类控件（HasCursor=true）时才赋予光标
        _cursorOwner = (focused != null && focused.IsEnabled && focused.HasCursor) ? focused : null;
        if (_cursorOwner != null)
        {
            _cursorOwner.IsCursorOwner = true;
            // 光标所有者变更时强制重绘，确保 _cursorRow/_cursorCol 在 Render 中更新
            if (_cursorOwner != oldOwner)
                _cursorOwner.Invalidate();
        }
    }

    /// <summary>
    /// 直接输出光标位置到终端（无脏帧直写动画移动了光标后调用）。
    /// 无脏分支不渲染整帧，光标会被 RenderDirect 的 CursorPos 拉走（如动态栏 spinner），
    /// 这里把光标恢复到所有者位置，避免输入区光标消失/错位。
    /// </summary>
    internal void EmitCursor()
    {
        if (_cursorOwner == null) return;
        var cs = _cursorOwner.GetCursorState();
        if (cs.HasValue)
        {
            var rb = new RenderBuffer();
            rb.CursorAt(cs.Value.row, cs.Value.col);
            Tty.Write(rb.ToString());
        }
    }

    // ── 鼠标 ──

    /// <summary>
    /// 处理鼠标事件。优先级：顶层模态窗口 → 顶层窗口（Z-order）→ 根视图。
    /// 返回 true 表示事件已被消费。
    /// </summary>
    public override bool OnMouse(InputEvent ev)
    {
        // 有模态窗口时，只路由给顶层模态窗口（窗口树最深层模态）
        if (HasModal)
        {
            var topModal = TopModal();
            if (topModal != null && topModal.OnMouse(ev))
                return true;
            // 模态遮罩：点击模态窗口外部也消费事件（防止穿透）
            return true;
        }

        // 非模态：按「最上优先」序（子先于父、后兄弟先于前兄弟）逐个命中测试
        foreach (var win in WindowsInTopDownOrder())
        {
            if (win.OnMouse(ev))
            {
                // 鼠标点击窗口 → 键盘焦点移到该窗口（Tab/方向键随后路由到它）
                if (ev.MouseLeft && FocusedWindow != win)
                    FocusWindowOnClick(win);
                return true;
            }
        }

        // Fallback：路由给根视图（控件级鼠标交互）
        return RootView.OnMouse(ev);
    }

    /// <summary>窗口树按「最上优先」的遍历序（反向渲染序：最深子 → 父 → 次深子…）。</summary>
    private IEnumerable<TuiWindow> WindowsInTopDownOrder()
    {
        var list = new List<TuiWindow>();
        void Walk(List<TuiWindow> wins, int depth)
        {
            if (depth > MaxTreeDepth) throw new InvalidOperationException("窗口树检测到环（WindowsInTopDownOrder）");
            foreach (var w in wins.OrderBy(x => x.ZOrder))
            {
                list.Add(w);
                if (w.Children.Count > 0) Walk(w.Children, depth + 1);
            }
        }
        Walk(Windows, 0);
        list.Reverse();
        return list;
    }

    /// <summary>鼠标点击窗口时，将键盘焦点转移到该窗口，并重绘新旧窗口的边框焦点色。</summary>
    private void FocusWindowOnClick(TuiWindow win)
    {
        if (FocusedWindow != null)
        {
            FocusedWindow.Focused = false;
            FocusedWindow.RootView.MarkDirty();
        }

        win.Focused = true;
        win.RootView.MarkDirty();
        FocusedWindow = win;
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
            var topModal = TopModal();
            if (topModal != null)
            {
                // 无论快捷键还是默认关闭，都记录时间戳，防按键重复触发退出框
                LastModalEscapeTime = DateTime.UtcNow;
                // 窗口级快捷键优先（如 Esc 注册为取消回调）
                if (topModal.OnKey(key))
                    return true;
                // 未处理 → 默认关闭（OnClosed 触发 ChatScreen 的 RenderWait 退出）
                topModal.OnClosed?.Invoke();
                return true;
            }
        }

        // Tab/Shift+Tab：先给焦点窗口处理（窗口快捷键 / AcceptsTab 控件），未处理才切换控件
        // （与 Esc 先路由到模态窗口的做法对齐，供 ModelPicker 等用 Tab 作快捷键的对话框使用）
        if (key.Key == ConsoleKey.Tab && FocusedWindow != null)
        {
            if (FocusedWindow.OnKey(key))
                return true;
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                FocusedWindow.FocusPrev();
            else
                FocusedWindow.FocusNext();
            return true;
        }

        // ── 路由到子节点：栈顶模态窗口 → 焦点窗口 → 根视图 ──
        // 模态在场就由它独占，且直接 return（不往下漏给根视图）：这就是
        // 「子窗口屏蔽父窗口的键」；它关闭后 CloseWindow 把焦点还给栈里上一个窗口，父层自然恢复。
        var modal = TopModal();
        if (modal != null)
            return modal.OnKey(key);

        if (FocusedWindow != null && FocusedWindow.OnKey(key))
            return true;

        return RootView.OnKey(key);
    }

    /// <summary>栈顶模态窗口 = 窗口树中最深层的模态窗口（DFS 渲染序最后一个模态节点）。
    /// 键路由以它为准，而非 FocusedWindow —— 非模态浮层可能叠在模态之上，认 FocusedWindow 会让键漏到根视图。</summary>
    private TuiWindow? TopModal()
    {
        TuiWindow? last = null;
        void Walk(List<TuiWindow> wins, int depth)
        {
            if (depth > MaxTreeDepth) throw new InvalidOperationException("窗口树检测到环（TopModal）");
            foreach (var w in wins.OrderBy(x => x.ZOrder))
            {
                if (w.Modal) last = w;
                if (w.Children.Count > 0) Walk(w.Children, depth + 1);
            }
        }
        Walk(Windows, 0);
        return last;
    }

    /// <summary>窗口树中最后一个存活窗口（DFS 渲染序末位）——泛化平铺的 Windows.LastOrDefault()。
    /// 关闭焦点窗口后用它回退焦点：顶层子窗被关回父、有更年轻兄弟则回同级次高层，自然满足。</summary>
    private TuiWindow? FindTopMostWindow()
    {
        TuiWindow? last = null;
        void Walk(List<TuiWindow> wins, int depth)
        {
            if (depth > MaxTreeDepth) throw new InvalidOperationException("窗口树检测到环（FindTopMostWindow）");
            foreach (var w in wins.OrderBy(x => x.ZOrder))
            {
                last = w;
                if (w.Children.Count > 0) Walk(w.Children, depth + 1);
            }
        }
        Walk(Windows, 0);
        return last;
    }

    /// <summary>窗口树节点总数（根窗口 + 全部后代）。</summary>
    private int TotalWindowCount()
    {
        int n = 0;
        void Walk(List<TuiWindow> wins, int depth)
        {
            if (depth > MaxTreeDepth) throw new InvalidOperationException("窗口树检测到环（TotalWindowCount）");
            foreach (var w in wins)
            {
                n++;
                if (w.Children.Count > 0) Walk(w.Children, depth + 1);
            }
        }
        Walk(Windows, 0);
        return n;
    }

    /// <summary>窗口树中是否存在带遮罩的窗口（递归）。</summary>
    private static bool AnyNodeHasMask(TuiWindow win)
        => win.HasMask || win.Children.Any(AnyNodeHasMask);

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
        //    遮罩铺满全屏，覆盖窗口外的根视图内容，实现真正的视觉遮挡。
        if (!incremental && Windows.Any(AnyNodeHasMask))
        {
            int maskBg = TuiTheme.Current.MaskBg;
            var rb = new RenderBuffer();
            for (int row = 0; row < TH; row++)
                rb.Write(row, 0, new string(' ', TW), bg: maskBg);
            sb.Append(rb.ToString());
        }

        // 3. 渲染窗口树（先序 DFS：父先画、子后画 → 子窗口永远盖在父窗口之上）
        //    层级刷新规则：任一窗口本帧整窗重绘（RenderWindow 会铺满自己的背景/边框，
        //    把重叠区域里上层窗口的内容盖掉）时，所有后续窗口（子窗口 + 更高兄弟）必须跟着整窗重绘 ——
        //    「父窗口刷新后自动刷新子对话框」，递归传播，否则子对话框会被父窗口的刷新刷没。
        //    粘性 forceFull 恰好实现这一传播；且子窗口永远在父之后绘制，「父整窗刷没子窗」结构上消除。
        bool forceFull = false;
        RenderLevel(sb, Windows, incremental, ref forceFull);

        // 3.5 重绘脏区域（窗口关闭后，补绘被遮挡的根视图控件）
        // 先擦除窗口残影（默认背景），再裁剪重绘根视图，保证背景不残留。
        foreach (var (x, y, w, h) in _dirtyRects)
        {
            // 用默认背景清除脏区域（擦除窗口 Mask / 边框 / 背景）。
            // 需先 SGR 复位，否则 bg=0 的空格不会清除残留底色（如窗口 Mask 的背景色）。
            for (int row = y; row < y + h && row < TH; row++)
            {
                if (row < 0) continue;
                var rb = new RenderBuffer();
                rb.Reset();
                rb.Write(row, x, new string(' ', w));
                sb.Append(rb.ToString());
            }

            // 裁剪重绘根视图控件
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

    /// <summary>
    /// 按同级 Z-order 渲染一层窗口树：每个窗口先画自己，再递归画其子窗口（先序 → 子永远盖在父上）。
    /// 粘性 forceFull：任一窗口整窗重绘后，其子窗口与后续兄弟全部强制整窗重绘——
    /// 否则父/低兄弟整窗铺满背景会把上层窗口内容刷没（增量脏控件补画救不回边框/背景/内容）。
    /// </summary>
    private void RenderLevel(StringBuilder sb, List<TuiWindow> wins, bool incremental, ref bool forceFull, int depth = 0)
    {
        if (depth > MaxTreeDepth)
            throw new InvalidOperationException($"窗口树检测到环或深度异常（depth>{MaxTreeDepth}）——某窗口被重复挂载成自身后代的子窗口。");

        foreach (var win in wins.OrderBy(w => w.ZOrder))
        {
            // 不能用 win.RootView.IsDirty 判定：MarkDirty 只标叶子不冒泡，子控件脏时 RootView 自身不脏，
            // 走脏控件路径会漏画边框/背景/按钮 —— 若此时底层（如 TuiScrollView 全量重绘）覆盖了窗口区域，
            // 窗口就只剩补画的脏控件，边框内容全被底层盖掉（设置界面弹框输入时花屏）。
            bool needFull = forceFull || !incremental || HasDirtyDescendant(win.RootView);
            if (needFull)
            {
                RenderWindow(sb, win);
                forceFull = true;
            }
            else
            {
                RenderWindowDirtyControls(sb, win);
            }

            if (win.Children.Count > 0)
                RenderLevel(sb, win.Children, incremental, ref forceFull, depth + 1);
        }
    }

    /// <summary>窗口树最大合法深度（正常对话框层数远小于此；超过即视为树环，防止递归无限导致死机）。</summary>
    private const int MaxTreeDepth = 64;

    /// <summary>递归查询控件自身或任一后代是否脏（含子树）。MarkDirty 不冒泡到父链，须显式递归。</summary>
    private static bool HasDirtyDescendant(TuiControl? control)
    {
        if (control == null) return false;
        if (control.IsDirty) return true;
        if (control is TuiView view)
        {
            foreach (var child in view.Children)
                if (HasDirtyDescendant(child)) return true;
        }
        return false;
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
        if (win.BorderStyle == WindowBorder.None)
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
        // 标题超宽时截断，避免覆盖左右边框（长标题会吃掉竖边框/右侧角）
        string titleText = drawTitle ? BoundTitle(win.Title, win.Width - 2) : "";
        if (grad)
        {
            // ── 渐变上边框（文字与线框一起渐变）──
            if (drawTitle)
            {
                // 标题嵌在渐变横线上，居中
                int titleVw = AnsiHelper.DisplayWidth(titleText);
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
                int tFg = win.TitleFg > 0 ? win.TitleFg : bc;
                int tBg = win.TitleBg > 0 ? win.TitleBg : fillBg;

                {
                    WriteAt(sb, win.Y, win.X + 1, titleText, tFg, tBg);
                    var rem = win.Width - 2 - AnsiHelper.DisplayWidth(titleText);
                    if (rem > 0) WriteAt(sb, win.Y, win.X + 1 + AnsiHelper.DisplayWidth(titleText), new string(hTop[0], rem), bc, fillBg);
                }
            }
            else
            {
                WriteAt(sb, win.Y, win.X + 1, new string(hTop[0], Math.Max(0, win.Width - 2)), bc, fillBg);
            }

            WriteAt(sb, win.Y, win.X + win.Width - 1, tr, bc, fillBg);
        }

        int contentTop = win.Y + 1; // 上边框下面一行
        int innerHeight = win.Height - 2; // 边框内部高度
        // 标题嵌在上边框行（Y），内容直接从其下 Y+1 开始（无标题分隔线）

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
            // 整窗重绘 = 内容区全量重画：边框/背景已重绘，内容必须同步全画。
            // 否则增量逻辑（child.IsDirty || parentDirty）下未脏的提示行/按钮不重画，
            // 被背景填充清成空白（弹框输入时「确定/取消」按钮消失）。
            win.RootView.Invalidate();
            win.RootView.Render(sb, win.ContentLeft, contentTop,
                clipL: win.ContentLeft, clipT: contentTop,
                clipR: win.ContentLeft + win.ContentWidth,
                clipB: contentTop + innerHeight);
            ClearDirtyRecursive(win.RootView); // 整窗重绘画完即清，防脏标记残留（RootView 不在屏幕 ClearDirty 范围）
            TuiControl.CascadedBg = savedEffectiveBg;
        }
        if (win.ContentLines.Count > 0)
        {
            for (int i = 0; i < Math.Min(innerHeight, win.ContentLines.Count); i++)
            {
                int row = contentTop + i;
                var line = win.ContentLines[i];
                if (AnsiHelper.DisplayWidth(line) > win.Width - 3)
                    line = AnsiHelper.TruncateByWidth(line, win.Width - 3);
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
            WriteAt(sb, win.Y + win.Height - 1, win.X + 1, new string(hBot[0], Math.Max(0, win.Width - 2)), bc, fillBg);
            WriteAt(sb, win.Y + win.Height - 1, win.X + win.Width - 1, br, bc, fillBg);
        }
    }

    /// <summary>绘制渐变水平线：左角 + N×横线(渐变色) + 右角</summary>
    private void WriteGradientHLine(StringBuilder sb, int row, int col, int width,
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
    private void WriteGradientVLine(StringBuilder sb, int startRow, int col, int height,
        string vChar, int startColor, int endColor, int bg)
    {
        for (int i = 0; i < height; i++)
        {
            float t = height > 1 ? (float)i / (height - 1) : 0;
            int c = AnsiTty.LerpRgb(startColor, endColor, t);
            WriteAt(sb, startRow + i, col, vChar, c, bg);
        }
    }

    /// <summary>
    /// 在指定位置写入 ANSI 文本。边界用屏幕高度 TH（而非终端高度 Tty.Rows）：
    /// 正常 App 里两者相等；预览/离屏渲染模拟更大屏幕时，TH 才是正确的可见边界。
    /// </summary>
    protected void WriteAt(StringBuilder sb, int row, int col, string text,
        int fg = 0, int bg = 0)
    {
        if (row < 0 || row >= TH) return;
        var rb = new RenderBuffer();
        rb.Write(row, col, text, fg: fg, bg: bg);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 将窗口标题截断到指定显示宽度，超宽时末尾加 "…"。
    /// 保证标题总宽 ≤ maxVw，避免长标题覆盖左右边框。
    /// </summary>
    private static string BoundTitle(string title, int maxVw)
    {
        var t = $" {title} ";
        if (maxVw <= 0) return "";
        if (AnsiHelper.DisplayWidth(t) <= maxVw) return t;

        const string ell = "…";
        int ellW = AnsiHelper.DisplayWidth(ell);
        var sb = new StringBuilder();
        int w = 0;
        foreach (var rune in t.EnumerateRunes())
        {
            int rw = AnsiHelper.RuneWidth(rune);
            if (w + rw + ellW > maxVw) break;
            w += rw;
            sb.Append(rune.ToString());
        }

        sb.Append(ell);
        return sb.ToString();
    }

    // ── 工厂方法 ──

    /// <summary>创建居中对话框</summary>
    public TuiWindow ShowDialog(string title, string content, int? width = null, int? height = null)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');

        var maxLineVw = lines.Max(l => AnsiHelper.DisplayWidth(l));

        var w = Math.Max(20, Math.Min(Tty.Cols - 8,
            width ?? Math.Max(maxLineVw + 4, AnsiHelper.DisplayWidth(title) + 4)));

        var h = Math.Min(Tty.Rows - 6, height ?? Math.Max(3, lines.Length + 4));

        var win = new TuiWindow
        {
            Width = w, Height = h,
            Title = title,
            ContentLines = [.. lines],
            Modal = true,
            HasMask = true,
            BorderColor = AnsiColors.Cyan,
            WinBg = AnsiColors.BgWhite, // 对话框默认背景
        };
        win.Center();
        AddWindow(win);
        return win;
    }

    /// <summary>创建通知提示框（右下角，自动消失）。恒为根窗口——
    /// 避免成为模态对话框的子窗口后，随模态关闭一起消失。</summary>
    public TuiWindow ShowToast(string message, int durationMs = 2000)
    {
        var vw = AnsiHelper.DisplayWidth(message);
        var w = Math.Min(Tty.Cols - 4, vw + 4);
        var win = new TuiWindow
        {
            X = Tty.Cols - w - 2, Y = Tty.Rows - 4,
            Width = w, Height = 3,
            ContentLines = [message],
            ContentFg = 37,
            Modal = false,
            HasMask = false,
            WinBg = AnsiColors.BgBrightBlack, // 深灰底色
            BorderColor = AnsiColors.Green,
        };
        AddRootWindow(win);
        // 自动消失：后台定时线程只投递，CloseWindow 在 UI 线程执行（Windows 列表无锁，
        // 后台直接改会与渲染循环 foreach 并发 → 帧交错花屏）。Windows.Contains 守卫：屏幕销毁后不再关闭。
        Task.Delay(durationMs).ContinueWith(_ => PostToUI(() =>
        {
            if (Windows.Contains(win))
                CloseWindow(win);
        }));
        return win;
    }
}