using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// TuiManager —— TUI 根管理器。
/// 管理多屏幕切换、浮层窗口 Z-order、渲染循环、输入路由、主题广播。
/// 替代旧 ScreenManager + WindowManager 的职责。
/// </summary>
public class TuiManager : IDisposable
{
    // ── 单例 ──
    public static TuiManager Instance { get; } = new();

    // ── 鼠标支持 ──
    /// <summary>
    /// 是否启用鼠标输入（SGR 鼠标：点击/滚动/移动）。默认开启。
    /// 开关在设置文件（.env / 设置界面），WAYCODER_MOUSE=0 或设置页关闭可停用。
    /// </summary>
    public static bool MouseEnabled => Config.Instance.MouseEnabled;

    // ── 终端尺寸 ──
    public int TW { get; private set; }
    public int TH { get; private set; }
    public bool IsActive { get; private set; }

    // ── 屏幕栈 ──
    private readonly Stack<TuiScreen> _screenStack = new();
    public TuiScreen? ActiveScreen { get; private set; }

    /// <summary>渲染互斥锁：主循环 / RunAgentWithRenderLoop / 各对话框 RenderWait 可能跨线程调 Render，
    /// 串行化避免双线程并发遍历控件树与写终端（帧交错花屏）。</summary>
    private readonly object _renderLock = new();

    // ── 统一输入源：主线程唯一渲染 ──
    // 输入（按键/鼠标/粘贴/尺寸）与时钟都由共享 InputManager 泵线程（子线程）产出，经事件队列
    // 上抛给主线程；主线程是唯一渲染者（Render 恒持 _renderLock，动画 RenderAllDirect 在 Render 内）。
    // 泵线程回归「纯输入 + 纯诊断」，绝不写屏 —— 界面刷新只在主线程。spinner 动画由主渲染循环
    // 每帧推进；泵线程不再参与动画渲染，故无需独立动画线程（此前「独立心跳线程直写 spinner」已废弃）。

    // ── UI 渲染循环线程追踪 ──
    // 判定「当前线程是否 UI 循环线程」不能比对构造 ChatScreen 时的线程快照：async REPL 主循环
    // 在 await 后续体被调度到线程池线程（项目无 SynchronizationContext），构造线程 ≠ 运行循环的线程。
    // 于是 RenderWait 会把真正的循环线程误认成「后台线程」而只空转 —— 循环线程自己又阻塞在 RenderWait
    // 里「等自己渲染」，无人渲染无人读键 = 整机卡死。这里改由 Render() 与 ReadInput() 在任何渲染循环
    // 执行时更新「当前循环线程」（这两者都只可能由循环线程调用），TuiScreen.IsUiThread 据此对齐，
    // await 迁移后也能对上。后台线程误调用 Render/ReadInput 会短暂错记，但下个循环迭代即自愈。
    /// <summary>当前 UI 渲染循环所在线程 ID（-1=尚未渲染）。由 Render() / ReadInput() 更新。</summary>
    public static volatile int UiLoopThreadId = -1;
    private string _lastModelSnapshot = ""; // 上次心跳采样的 active connect 模型快照（5s 同步比较用）
    private int _heartbeatCount; // 心跳节拍计数（用于 1s/5s 的丰富条/CPU 采样节拍）

    // ── 主循环冻结看门狗 ──
    // 主循环每完成一个阶段更新 UiLoopTick + 标记当前阶段；看门狗（泵线程心跳）发现 UiLoopTick
    // 停滞 >3s 就记一条错误日志，含最后活动阶段 —— 排查「死机」时定位主循环卡在哪个阶段
    // （PumpUIQueue=某个 PostToUI 动作忙循环 / Render=渲染忙循环 / ReadInput=输入被阻塞）。
    public static volatile string UiLoopActivity = "idle";
    public static long UiLoopTick; // 用 Volatile.Read/Write 访问（volatile 不支持 long）
    private bool _freezeLogged;

    /// <summary>
    /// 统一更新主循环阶段标记：设 UiLoopActivity + 刷新心跳 tick + 阶段条入黑匣子。
    /// 各渲染循环（REPL 主循环 / RunAgentWithRenderLoop / RunWithUiLoop）都走这里，
    /// 顺带修复「子循环只设 UiLoopActivity 不更新 UiLoopTick → /loop 长任务误报冻结」的 bug。
    /// </summary>
    public static void SetActivity(string stage)
    {
        UiLoopActivity = stage;
        Volatile.Write(ref UiLoopTick, Environment.TickCount64);
        FreezeCapture.RecordPhase(stage);
    }

    private void StartAnimTicker()
    {
        // 统一输入源：动画心跳并入共享 InputManager 泵线程（不再另起独立线程）。
        // 泵线程是唯一读控制台 + 唯一驱动时钟的线程；本方法只是把心跳逻辑经回调注册到泵线程。
        // 用 Input（而非 _input）：Enter 早于首次 ReadInput，此时 _input 尚未创建；用 Input 惰性创建
        // 并注册，泵线程随后在首次 ReadInput→EnsurePumpStarted 时读到该回调并周期驱动。
        // 好处：少一条可能竞争/互相卡死的线程；卡死后动画与冻结侦测仍由泵线程周期驱动。
        Input.SetHeartbeat(HeartbeatTick);
    }

    /// <summary>
    /// 泵线程时钟回调（统一输入源驱动）：冻结看门狗 + 丰富条 + CPU 采样 + 模型兜底同步 + 定时 dump。
    /// 全为诊断/后台安全操作，不做任何渲染——界面刷新严格在主线程；泵线程（子线程）只产出输入/时钟消息。
    /// 由 InputManager 泵线程按 120ms 节拍调用。
    /// </summary>
    private void HeartbeatTick()
    {
        if (!IsActive) return;

        // 冻结看门狗：主循环 UiLoopTick 停滞 >3s → 记一条错误（一次性/冻结段），
        // 附最后活动阶段，并同步强制落盘完整现场。门控 UiLoopActivity != "idle"。
        long stale = UiLoopActivity != "idle" ? Environment.TickCount64 - Volatile.Read(ref UiLoopTick) : 0;
        if (stale > 3000)
        {
            if (!_freezeLogged)
            {
                _freezeLogged = true;
                var dumpPath = FreezeCapture.Trigger(UiLoopActivity, stale);
                ErrorLog.Error("UI.Freeze",
                    $"主循环冻结 {stale}ms，最后活动: {UiLoopActivity} —— 现场已落盘: {dumpPath}");
            }
        }
        else _freezeLogged = false;

        // 死机黑匣子丰富条（~1 条/s：8 × 120ms）——记录 Agent/上下文状态进环。
        if (++_heartbeatCount % 8 == 0)
            FreezeCapture.RecordRichSnapshot();

        // CPU 采样（~5s 一次：42 × 120ms）+ 更新共享值（动态栏显示/dump）。
        if (_heartbeatCount % 42 == 0)
        {
            var cpu = CpuMonitor.Sample();
            FreezeCapture.SetCpuPercent(cpu);
            if (cpu > 70 && FreezeCapture.Enabled)
                FreezeCapture.DumpNow($"CPU 高占用 {cpu:F0}%", UiLoopActivity, 0);

            // 模型显示 5s 兜底同步：切换路径（/connect/Ctrl+Shift+M 等）可能漏刷新，
            // 心跳比较 active connect 快照，变了才刷新动态栏/模型栏（防每 5s 全屏闪烁）。
            var snap = $"{Config.Instance.Provider}|{Config.Instance.Model}|{Config.Instance.SmallProvider}|{Config.Instance.SmallModel}";
            if (snap != _lastModelSnapshot)
            {
                _lastModelSnapshot = snap;
                if (ActiveScreen is UI.Tui.Screens.ChatScreen cs)
                    cs.RefreshModelStatus(); // 只标脏不碰控件树，后台线程安全
            }
        }

        // 定时 dump（用户需求：每分钟一次）——死机前最近一次快照即现场。
        FreezeCapture.PeriodicDumpTick(UiLoopActivity);
    }

    private void StopAnimTicker()
    {
        // 注销泵线程上的心跳回调（不再有独立动画线程需停）。_input 可能为 null（从未创建）则跳过。
        if (_input != null) _input.SetHeartbeat(null);
    }

    private InputManager? _input;
    /// <summary>共享输入管理器：主循环与 RenderWait（ModalPicker/DiffPreview 等阻塞对话框）共用，
    /// 统一 bracketed paste / CSI 解析——否则 RenderWait 用裸 ReadKey 会把粘贴的 \x1b[200~ 当 Esc 关闭对话框。</summary>
    public InputManager Input => _input ??= CreateInput();
    private InputManager CreateInput()
    {
        var mgr = new InputManager();
        mgr.Init();
        return mgr;
    }

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
        // 启用鼠标序列（?1000h/?1002h/?1006h SGR）——单靠它有鼠标是「输出」侧；Windows 还要开
        // VT 输入 + 读 stdin 原始字节（InputManager）才能「收到」鼠标。WinConsoleMode.Enable 内部
        // 有 Windows/重定向守卫，macOS/Linux 直接 no-op（return false），不碰 kernel32。
        if (MouseEnabled) Tty.EnableMouseForTerminal();
        WinConsoleMode.Enable();
        // Exit→Enter 往返（裸 `!` 跑 shell 命令后再进界面）里 Exit 刚用 Disable() 把控制台还原成
        // 行缓冲 + 回显，而 Enable 只在 InputManager.Init 里施过一次 ⇒ 必须每次重进都再施一遍，
        // 否则按键要按回车才到、回显叠在 TUI 自绘上。stdin 被重定向时无参 Enable() 作用不到
        // CONIN$（它判重定向就返回 false），所以走 InputManager 记下的设备句柄。
        // 用字段直取而非 Input 属性：属性会惰性创建 InputManager，而首次 Enter 时它还没建
        // （真需要时 Init 里刚施过模式），只在已存在时补施即可。
        _input?.ReapplyConsoleMode();
        (TW, TH) = (Tty.Cols, Tty.Rows);
        IsActive = true;
        // 进入备用屏后强制全刷新：否则 Render 读到上次残留的 _needsFullRefresh=false 走「无脏」路径
        // 直接 return 不重绘 → 黑屏（如 `!cmd` 退出再进入 TUI 后画面不刷新）
        IsDirty = true;
        _needsFullRefresh = true;
        StartAnimTicker(); // 独立动画心跳：主循环被堵时 spinner 仍转
    }

    /// <summary>恢复终端</summary>
    public void Exit()
    {
        StopAnimTicker(); // 先停心跳，避免退出时再往已还原的终端写
        Tty.DisableMouse();
        WinConsoleMode.Disable(); // 恢复 Windows 控制台输入模式（未启用则 no-op）
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
        // Activate 之后控件树才就绪（标记版界面在 BuildLayout 里才建树）—— 动态栏直写归属在此登记
        screen.RegisterDirectWriters();
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
        ActiveScreen?.RegisterDirectWriters(); // 回到本屏 → 重新认领动态栏直写
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
        // 记录当前循环线程（await 迁移后对齐；只有循环线程会走到这里）
        UiLoopThreadId = Environment.CurrentManagedThreadId;
        // 渲染互斥：主循环 / RunAgentWithRenderLoop / 对话框 RenderWait 可能跨线程调 Render，
        // 串行化避免双线程并发遍历控件树 + 写终端（帧交错花屏）
        lock (_renderLock)
        {
            if (!IsDirty && !_needsFullRefresh)
            {
                // 无脏变化也刷新直接写屏的动画控件（不依赖 Dirty 标志）
                TuiAnimatedText.RenderAllDirect();
                TuiDynamicBar.RenderAllDirect(); // 动态栏 spinner 直写屏幕（不等 dirty）
                ActiveScreen?.EmitCursor();      // 直写用 CursorPos 移动了光标 → 恢复，防输入区光标错位
                return;
            }
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

            // 4. 直接写屏的动画控件（不依赖 Dirty 标志，帧写完后叠加写终端）
            TuiAnimatedText.RenderAllDirect();
            TuiDynamicBar.RenderAllDirect();
            ActiveScreen?.EmitCursor(); // 直写也移动了光标 → 恢复（脏路径同样不能丢输入区光标）
        }
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
        return ActiveScreen?.OnMouse(ev) ?? false;
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
