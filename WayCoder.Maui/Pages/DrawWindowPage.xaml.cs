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
        CanvasView.GestureRecognizers.Add(pointer);

        CanvasView.Drawable = _canvas;
    }

    /// <summary>
    /// 画布的绘制体：**只负责把上一帧那张图贴上去**（按 AspectFit 居中）。
    ///
    /// 之所以把"出图"和"贴图"分开：出图要重新光栅化（几十毫秒，得放后台线程），
    /// 而贴图必须每帧即时完成。分开之后，后台算新帧时屏上一直是旧帧，算完再换 ⇒ 不闪。
    /// </summary>
    internal sealed class SceneCanvas : IDrawable
    {
        private Microsoft.Maui.Graphics.IImage? _image;

        /// <summary>最近一帧在屏幕上的贴图矩形（`AspectFit` 的结果）。</summary>
        private RectF _fit;

        /// <summary>场景尺寸（= PNG 的像素尺寸），坐标反算要用。</summary>
        private double _sceneW = 1, _sceneH = 1;

        public void SetFrame(Microsoft.Maui.Graphics.IImage img, double sceneW, double sceneH)
        {
            _image = img;
            _sceneW = sceneW <= 0 ? 1 : sceneW;
            _sceneH = sceneH <= 0 ? 1 : sceneH;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // 背景铺满整个视图（含 AspectFit 留出的黑边），否则未覆盖区会是平台默认底色
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(dirtyRect);
            if (_image == null) return;

            var iw = _image.Width;
            var ih = _image.Height;
            if (iw <= 0 || ih <= 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            // AspectFit：等比缩放到装得下并居中
            var s = Math.Min(dirtyRect.Width / iw, dirtyRect.Height / ih);
            var w = (float)(iw * s);
            var h = (float)(ih * s);
            var x = dirtyRect.X + (dirtyRect.Width - w) / 2f;
            var y = dirtyRect.Y + (dirtyRect.Height - h) / 2f;
            _fit = new RectF(x, y, w, h);
            canvas.DrawImage(_image, x, y, w, h);
        }

        /// <summary>
        /// 视图坐标 → **场景坐标**。程序收到的触摸坐标必须是场景坐标 ——
        /// 场景按 AspectFit 缩放过（还可能有黑边），直接用视图坐标会让程序算错格子。
        /// 落在图外返回 null（黑边上的点击不该被当成落子）。
        /// </summary>
        public (int X, int Y)? ToScene(Point view)
        {
            var f = _fit;
            if (f.Width <= 0 || f.Height <= 0) return null;
            if (view.X < f.X || view.Y < f.Y || view.X > f.Right || view.Y > f.Bottom) return null;
            var sx = (view.X - f.X) / f.Width * _sceneW;
            var sy = (view.Y - f.Y) / f.Height * _sceneH;
            return ((int)sx, (int)sy);
        }
    }

    private readonly SceneCanvas _canvas = new();

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

    /// <summary>
    /// 把自己的**画布视口**（不是整页）记进 <see cref="VmlUiCalls.MeasuredViewport"/>，
    /// 供 <c>SCREEN_W/H</c> 号段回报给 VML 程序。
    ///
    /// 这是"让程序按真实可用面积自适应"的唯一正确来源 —— 按屏幕尺寸减一个固定 chrome 估算，
    /// 底部一百多 dp 的内容会落在可视区外（实测就是这个症状）。MAUI 的长度单位就是 dp，
    /// 所以这里直接就是绘图单位，不需要再按密度换算。
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (CanvasScroll.Width > 0 && CanvasScroll.Height > 0)
            VmlUiCalls.MeasuredViewport = ((int)CanvasScroll.Width, (int)CanvasScroll.Height);

        // 布局到位后重算一次画布尺寸（首帧渲染时这里还是 0，见 FitCanvas 注释），
        // 并按需重画 —— 否则首帧用过兜底尺寸，转屏/分屏之后就再也不会修正。
        if (_scene is { } s && CanvasScroll.Width > 0)
        {
            var want = CanvasScroll.Width;
            if (Math.Abs(CanvasView.WidthRequest - want) > 0.5)
            {
                FitCanvas(s);
                CanvasView.Invalidate();
            }
        }
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

    /// <summary>
    /// 场景变化 → **只标脏，不直接渲染**，由 <see cref="OnAppearing"/> 里那个 40ms 轮询定时器统一出图。
    ///
    /// ⚠ 这里原来是「每条图元立刻渲染一次」。而渲染的代价是**重新编码一整张 PNG 再换掉
    /// `Image.Source`** —— 一次替换就是一次可见的闪。一个 VML 动画帧有几十条图元
    /// （`ui_clear` + 15 条横线 + 15 条竖线 + 棋子 + 文字），于是**下一手棋要闪几十下**
    /// （用户报的"下棋后会闪"就是这个）。
    ///
    /// 那个轮询定时器本来就在跑（按 `Version` 判断有没有变），所以"合并"不需要新机制：
    /// **把这里的立即渲染去掉**，一帧的几十条图元自然被合并成一次渲染。
    /// </summary>
    private void OnSceneChanged(VmlScene scene)
    {
        if (!ReferenceEquals(scene, _scene)) return;
        // 刻意不做任何事 —— 定时器会按 Version 发现变化并出图。
        // 方法体留着：注册点还在，将来若要"变化时唤醒定时器"也不必改调用方。
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
            try { ShowFrame(DrawRunner.ToPng(DrawRunner.Parse(dsl)), scene); }
            catch (Exception ex) { ErrorLog.Error("VmlDraw", "首帧渲染失败", ex); }
            finally { FinishRender(); }
            return;
        }

        // 光栅化 + 编码放后台线程：小画布很快，但没必要占着 UI 线程做压缩
        Task.Run(() =>
        {
            try
            {
                var png = DrawRunner.ToPng(DrawRunner.Parse(dsl));
                MainThread.BeginInvokeOnMainThread(() => ShowFrame(png, scene));
            }
            catch (Exception ex)
            {
                ErrorLog.Error("VmlDraw", "渲染场景失败", ex);
            }
            finally { FinishRender(); }
        });
    }

    /// <summary>
    /// 把一帧贴上去。**这是"换帧"的唯一出口**（首帧与后续帧走同一条路，免得两处漂）。
    ///
    /// 关键点：`GraphicsView` 是**就地重绘**的 —— 先备好 `IImage` 再 `Invalidate()`，
    /// 平台会在下一次绘制时把它画上去，中间旧帧一直在屏上。**不要**在这里去动任何控件
    /// 的 `Source`/`Content`：那样会先卸旧再装新，中间空一拍就是可见的闪。
    /// </summary>
    /// <summary>
    /// 给画布定尺寸：**宽 = 可用宽，高 = 宽 × 场景高宽比**（超出的部分交给外面的 ScrollView 滚）。
    ///
    /// 为什么不能让它按高度 `AspectFit`：GraphicsView 在 ScrollView 里、视口高度就一行，
    /// 一个"高瘦"的场景（手机竖屏绘图区 395×744 就是）会两边留大片黑边、内容缩成半屏宽 —— 实测如此。
    ///
    /// ⚠ **可用宽取不到时必须兜底成场景宽度**，不能"取不到就不设"：
    /// 首帧是在 `Attach` 里同步渲染的，那时页面还没布局、`CanvasScroll.Width` 是 0 ——
    /// 不设尺寸 ⇒ GraphicsView 零尺寸 ⇒ **画不出来、也点不到**（实测：整块画布全黑，
    /// 触摸全被 `ToScene` 判在图外丢掉）。而 `_renderedVersion` 此时已经记下，
    /// 后续帧不会再触发，于是永远黑着。
    /// </summary>
    private void FitCanvas(VmlScene scene)
    {
        if (scene.Width <= 0) return;
        var avail = CanvasScroll.Width > 0 ? CanvasScroll.Width : scene.Width;
        CanvasView.WidthRequest = avail;
        CanvasView.HeightRequest = avail * scene.Height / scene.Width;
    }

    private void ShowFrame(byte[] png, VmlScene scene)
    {
        try
        {
            FitCanvas(scene);
            _canvas.SetFrame(Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(new MemoryStream(png)),
                scene.Width, scene.Height);
            CanvasView.Invalidate();
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlDraw", "帧贴图失败", ex);
        }
    }

    /// <summary>
    /// 收尾：解除"正在渲染"并**补一次**。
    ///
    /// ⚠ 原来只是把 <c>_rendering = false</c> 一置就完事 —— 而 `Version` 是在**开始渲染之前**
    /// 就记下的，渲染期间若场景又变了，那个新版本既不会被这一帧画出来、也没有任何东西会再触发渲染 ⇒
    /// **最后一帧被静默丢掉**（表现是"下完子棋盘停在中间状态，要再点一下才刷新"）。
    /// 这里补一次调用；因为版本号已经变过，它会真的重新渲染，且渲染完会再查一次，天然收敛。
    /// </summary>
    private void FinishRender()
    {
        _rendering = false;
        RenderIfChanged();
    }

    // ── 输入 ──────────────────────────────────────────────────

    private void PostPointer(PointerEventArgs e, bool down = false, bool move = false, bool up = false)
    {
        var p = e.GetPosition(CanvasView);
        if (p is not { } pt) return;

        // ⚠ **必须换算成场景坐标**：画布是缩放贴上去的（还可能有黑边/滚动偏移），
        // 直接把视图坐标发过去，程序按自己的网格算就会偏 —— 屏幕越大/越扁偏得越多。
        if (_canvas.ToScene(pt) is not { } scene) return;
        var x = scene.X;
        var y = scene.Y;
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
