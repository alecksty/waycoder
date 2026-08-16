using System.Text;
using WayCoder.UI.Shared.Terminal;


namespace WayCoder.UI.Tui;

/// <summary>
/// TuiManager —— TUI 根管理器。
/// 管理多屏幕切换、浮层窗口 Z-order、渲染循环、输入路由、主题广播。
/// 替代旧 ScreenManager + WindowManager 的职责。
/// </summary>
public class TuiManager : IDisposable
{
    // ── 单例 ──
    public static TuiManager Instance { get; } = new();

    // ── 终端尺寸 ──
    public int TW { get; private set; }
    public int TH { get; private set; }
    public bool IsActive { get; private set; }

    // ── 屏幕栈 ──
    private readonly Stack<TuiScreen> _screenStack = new();
    public TuiScreen? ActiveScreen { get; private set; }

    // ── 渲染缓存 ──
    /// <summary>上一帧无浮层窗口的干净输出（窗口关闭时用于还原背景）</summary>
    public string LastCleanFrame { get; private set; } = "";

    /// <summary>脏标记：有输入或状态变化时置 true，Render 后置 false</summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>是否需要全屏清除+重绘（首帧/Resize/切屏=true，增量更新=false）</summary>
    private bool _needsFullRefresh = true;

    // ── 生命周期 ──

    /// <summary>初始化终端（备用屏 + 鼠标 + 尺寸）</summary>
    public void Enter()
    {
        Tty.EnterAltScreen();
        Tty.HideCursor();
        Tty.EnableMouse();
        (TW, TH) = (Tty.Cols, Tty.Rows);
        IsActive = true;
    }

    /// <summary>恢复终端</summary>
    public void Exit()
    {
        Tty.DisableMouse();
        Tty.ShowCursor();
        Tty.ExitAltScreen();
        IsActive = false;
    }

    /// <summary>刷新主题设置：从配置应用统一配色（TuiTheme 为唯一配色真源）。</summary>
    public void RefreshTheme()
    {
        TuiTheme.ApplyFromConfig(Config.Instance);
    }

    /// <summary>
    /// 标记活跃屏幕所有控件为脏（全屏 ANSI 对话框关闭后自动调用，还原被覆盖的 TUI 画面）。
    /// 与 ClearScreen 不同：不闪烁，仅让控件逐一重绘覆盖。
    /// </summary>
    public static void RequestFullRefresh()
    {
        if (Instance is { IsActive: true, ActiveScreen: not null })
        {
            Instance.IsDirty = true;
            Instance._needsFullRefresh = true;
            Instance.ActiveScreen.RootView.Invalidate();
        }
    }

    public void Dispose()
    {
        if (IsActive)
        {
            ActiveScreen?.Deactivate();
            _screenStack.Clear();
            Exit();
        }
    }

    // ── 屏幕管理 ──

    /// <summary>推入新屏幕（当前屏幕失活）</summary>
    public void PushScreen(TuiScreen screen)
    {
        IsDirty = true;
        _needsFullRefresh = true;
        ActiveScreen?.Deactivate();
        _screenStack.Push(screen);
        ActiveScreen = screen;
        screen.Manager = this;
        screen.Activate();
    }

    /// <summary>弹出当前屏幕，恢复上一层</summary>
    public TuiScreen? PopScreen()
    {
        if (_screenStack.Count == 0) return null;
        IsDirty = true;
        _needsFullRefresh = true;
        var popped = _screenStack.Pop();
        popped.Deactivate();
        ActiveScreen = _screenStack.Count > 0 ? _screenStack.Peek() : null;
        ActiveScreen?.Activate();
        return popped;
    }

    /// <summary>切换回主屏幕（弹出所有直至根屏幕）</summary>
    public void SwitchToRoot()
    {
        while (_screenStack.Count > 1)
            PopScreen();
    }

    // ── 全局热键 ──
    public Func<ConsoleKeyInfo, bool>? GlobalKeyHandler { get; set; }

    // ── 渲染 ──

    /// <summary>
    /// 全帧渲染。增量模式下仅重绘脏控件（跳过 ClearScreen），避免焦点切换时全屏闪烁。
    /// 全刷新模式下 ClearScreen + RootView 全量重绘。
    /// </summary>
    public void Render()
    {
        if (!IsActive) return;
        if (!IsDirty && !_needsFullRefresh) return;  // 没有变化，跳过渲染（全刷新请求除外）
        IsDirty = false;

        (TW, TH) = (Tty.Cols, Tty.Rows);

        // 确定当前光标所有者（每屏一个光标）
        ActiveScreen?.SetCursorOwner();

        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

        // 全刷新仅首帧 / 切屏 / Resize 时清除整个屏幕。
        // RootView 因子控件变化而标记脏时走增量路径：不清屏，控件原地重绘覆盖。
        bool fullRefresh = _needsFullRefresh;
        if (fullRefresh)
        {
            sb.Append(AnsiTty.ClearScreen);
            ActiveScreen?.RootView.Invalidate();
            _needsFullRefresh = false;
        }

        // 通知 Screen 当前是否为增量更新（仅脏控件刷新）
        if (ActiveScreen != null)
            ActiveScreen.IsIncrementalUpdate = !fullRefresh;

        // 1. 渲染活跃屏幕
        ActiveScreen?.Render(sb);

        // 2. 保存干净帧
        LastCleanFrame = sb.ToString();

        // 3. 全局输出
        Tty.Write(sb.ToString());
    }

    /// <summary>写入干净帧（窗口关闭后还原背景）</summary>
    public void RestoreCleanFrame()
    {
        if (!string.IsNullOrEmpty(LastCleanFrame))
            Tty.Write(LastCleanFrame);
    }

    // ── 输入路由 ──

    /// <summary>处理按键。返回 true 表示已处理。</summary>
    public bool OnKey(ConsoleKeyInfo key)
    {
        IsDirty = true;
        // 全局热键优先
        if (GlobalKeyHandler != null && GlobalKeyHandler(key))
            return true;

        // 活跃屏幕的模态窗口优先
        if (ActiveScreen?.HasModal == true && ActiveScreen.FocusedWindow != null)
            return ActiveScreen.OnKey(key);

        // 活跃屏幕处理
        return ActiveScreen?.OnKey(key) ?? false;
    }

    /// <summary>路由鼠标事件给活跃屏幕</summary>
    public bool HandleMouse(InputEvent ev)
    {
        IsDirty = true;
        return ActiveScreen?.HandleMouse(ev) ?? false;
    }

    /// <summary>通知尺寸变化</summary>
    public void OnResize()
    {
        IsDirty = true;
        _needsFullRefresh = true;
        (TW, TH) = (Tty.Cols, Tty.Rows);
        ActiveScreen?.OnResize(TW, TH);
    }

    // ── 便捷方法（委托给活跃屏幕） ──

    /// <summary>在活跃屏幕上显示对话框</summary>
    public TuiWindow? ShowDialog(string title, string content, int? width = null, int? height = null)
    {
        return ActiveScreen?.ShowDialog(title, content, width, height);
    }

    /// <summary>在活跃屏幕上显示 Toast</summary>
    public TuiWindow? ShowToast(string message, int durationMs = 2000)
    {
        return ActiveScreen?.ShowToast(message, durationMs);
    }

    /// <summary>关闭活跃屏幕上的窗口</summary>
    public void CloseWindow(TuiWindow win)
    {
        ActiveScreen?.CloseWindow(win);
    }
}
