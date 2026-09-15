using WayCoder.Infra;
using WayCoder.Maui.Services;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

/// <summary>
/// VML 程序的绘图窗口。三种职责，各自有明确的线程归属：
///   · **渲染**：把 <see cref="VmlScene"/> 的图元翻成绘图 DSL，交给既有的
///     <see cref="DrawRunner"/>（<c>Parse</c> + <c>ToPng</c>）出图，再塞进 <see cref="Image"/>。
///     复用现成渲染链的好处是**图元语义只有一处实现**，而且窗口里看到的东西可以用同一个
///     `draw` 工具出一张 PNG 来对照 —— 出问题时能二分是"场景错了"还是"渲染错了"。
///   · **输入**：指针/按键事件投进 <see cref="VmlUiCalls"/> 的消息队列（UI 线程投递、VM 线程取走）。
///   · **生命周期**：用户点返回箭头 = 发一条 WindowClose 消息，程序据此退出自己的主循环。
///
/// **按版本号重绘**：场景每次变化 <c>Version</c> 自增，定时器只在版本变了才重新编码 PNG。
/// 不这么做的话，一个 30fps 的定时器会把整段 DSL 重新解析 + 重新压缩编码 30 次/秒，
/// 而画面可能根本没变。
/// </summary>
public partial class DrawWindowPage : ContentPage
{
    private VmlScene? _scene;
    private IDispatcherTimer? _timer;
    private int _renderedVersion = -1;
    private bool _rendering;

    public DrawWindowPage()
    {
        InitializeComponent();

        // 指针事件：按下/移动/抬起 → 触摸消息（同时补一对鼠标消息，
        // 让按"鼠标"写法的程序在手机上也直接能跑）
        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += (_, e) => PostPointer(e, down: true);
        pointer.PointerMoved += (_, e) => PostPointer(e, move: true);
        pointer.PointerReleased += (_, e) => PostPointer(e, up: true);
        CanvasImage.GestureRecognizers.Add(pointer);
    }

    /// <summary>由宿主在打开窗口时注入场景。</summary>
    internal void Attach(VmlScene scene)
    {
        _scene = scene;
        HeaderLabel.Text = scene.Title;
        Title = scene.Title;
        _renderedVersion = -1;
        // **首帧同步渲染**：异步那条路要等 40ms 的定时器，而实测「窗口一闪而过、什么也没看到」
        // —— 程序若很快调 WIN_CLOSE（或退出），异步首帧根本来不及出。这里就地把第一帧出掉，
        // 之后的变化再走定时器。画布小（几百像素见方），同步编码的代价可以接受。
        RenderIfChanged(synchronous: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VmlUiCalls.OnSceneChanged -= OnSceneChanged;
        VmlUiCalls.OnSceneChanged += OnSceneChanged;

        // 定时器只做"版本变了就重绘"，不参与画面合成
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(40); // ~25fps 的检查频率，实际编码次数取决于场景变化
        _timer.Tick += (_, _) => RenderIfChanged();
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
        VmlUiCalls.OnSceneChanged -= OnSceneChanged;

        // 用户点了返回箭头（或被导航走）：告诉程序窗口没了。
        // **这必须在 OnDisappearing 里做** —— 放在别处会漏掉"手势返回"这条路径，
        // 程序就会一直等在 MsgWait 上，直到超时。
        VmlUiCalls.Current?.MarkWindowClosed();
    }

    private void OnSceneChanged(VmlScene scene)
    {
        if (!ReferenceEquals(scene, _scene)) return;
        // 用 lambda 而非方法组：RenderIfChanged 带可选参数，方法组转不成 Action
        Dispatcher.Dispatch(() => RenderIfChanged());
    }

    private void RenderIfChanged(bool synchronous = false)
    {
        var scene = _scene;
        if (scene == null || _rendering) return;
        if (scene.Version == _renderedVersion) return;
        _renderedVersion = scene.Version;
        _rendering = true;

        var dsl = scene.BuildDsl();

        if (synchronous)
        {
            try { CanvasImage.Source = ImageSource.FromStream(() => new MemoryStream(DrawRunner.ToPng(DrawRunner.Parse(dsl)))); }
            catch (Exception ex) { ErrorLog.Error("VmlDraw", "首帧渲染失败", ex); }
            finally { _rendering = false; }
            return;
        }

        // 编码放后台线程：小画布很快，但没必要占着 UI 线程做压缩
        Task.Run(() =>
        {
            try
            {
                var png = DrawRunner.ToPng(DrawRunner.Parse(dsl));
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // FromStream 的工厂每次都会重新读流，所以把字节闭包进去即可
                    CanvasImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
                });
            }
            catch (Exception ex)
            {
                ErrorLog.Error("VmlDraw", "渲染场景失败", ex);
            }
            finally { _rendering = false; }
        });
    }

    // ── 输入 ──────────────────────────────────────────────────

    private void PostPointer(PointerEventArgs e, bool down = false, bool move = false, bool up = false)
    {
        var p = e.GetPosition(CanvasImage);
        if (p is not { } pt) return;
        var x = (int)pt.X;
        var y = (int)pt.Y;
        var calls = VmlUiCalls.Current;
        if (calls == null) return;

        if (down)
        {
            calls.PostInput(VmlMsgType.TouchDown, x, y);
            calls.PostInput(VmlMsgType.MouseDown, x, y);
        }
        if (move)
        {
            calls.PostInput(VmlMsgType.TouchMove, x, y);
            calls.PostInput(VmlMsgType.MouseMove, x, y);
        }
        if (up)
        {
            calls.PostInput(VmlMsgType.TouchUp, x, y);
            calls.PostInput(VmlMsgType.MouseUp, x, y);
        }
    }

    // 屏幕手柄：手机没有物理键盘，不把这些键做出来的话「方向键 + 动作键写的游戏」在真机上没法玩。
    // 键码全部取自 VmlKeys（唯一真源），且刻意映射到自然键盘等价键 —— 同一份程序接物理键盘也能玩。
    private void OnPadLeft(object? sender, EventArgs e) => PostKey(VmlKeys.Left);
    private void OnPadRight(object? sender, EventArgs e) => PostKey(VmlKeys.Right);
    private void OnPadUp(object? sender, EventArgs e) => PostKey(VmlKeys.Up);
    private void OnPadDown(object? sender, EventArgs e) => PostKey(VmlKeys.Down);

    private void OnFaceA(object? sender, EventArgs e) => PostKey(VmlKeys.PadA);
    private void OnFaceB(object? sender, EventArgs e) => PostKey(VmlKeys.PadB);
    private void OnFaceX(object? sender, EventArgs e) => PostKey(VmlKeys.PadX);
    private void OnFaceY(object? sender, EventArgs e) => PostKey(VmlKeys.PadY);

    private void OnStart(object? sender, EventArgs e) => PostKey(VmlKeys.Start);
    // SELECT 与 PAUSE 都映射到「SELECT 键」，中间区只留两个键（手柄上也就是这两个）
    private void OnSelect(object? sender, EventArgs e) => PostKey(VmlKeys.Select);

    private static void PostKey(int code)
    {
        var calls = VmlUiCalls.Current;
        if (calls == null) return;
        calls.PostInput(VmlMsgType.KeyDown, code, 0);
        calls.PostInput(VmlMsgType.KeyUp, code, 0);
    }
}
