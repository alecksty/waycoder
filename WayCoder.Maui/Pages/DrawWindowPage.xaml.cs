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
/// **按「一帧画完了」重绘**：程序的 `ui_present()` 每调一次算一帧（<c>PresentVersion</c> 自增），
/// 定时器只在标记变了才重新编码 PNG。不这么做的话，一个 25fps 的定时器会把整段 DSL 重新解析 +
/// 重新压缩编码 25 次/秒，而画面可能根本没变。
///
/// ⚠ **别用 `Version` 当这个标记**（v0.96.178 修的就是它）：那个数每个图元都 +1，定时器撞上
/// 任意一次就会把**画到一半的场景**贴出去 —— 用户看到的是「棋盘一闪一闪」（见 `RenderIfChanged`）。
/// </summary>
public partial class DrawWindowPage : ContentPage
{
    private VmlScene? _scene;
    /// <summary>页面正在关闭 —— 这之后不再往消息队列投 `WindowResize`（程序已经在退出了）。</summary>
    private bool _closing;
    private IDispatcherTimer? _timer;
    /// <summary>上次出图时的标记（presented 程序 = `PresentVersion`，否则 = `Version`）。</summary>
    private int _renderedVersion = -1;

    /// <summary>
    /// 「矢量后端画不了 ⇒ 换光栅再画一遍」的**一次性**放行牌。
    /// 置位时渲染那一拍跳过版本守卫、并且**现拍** DSL（不走 `TakePresentedDsl`，那份快照已被取走）。
    /// 一次一帧就够：换过去之后一直是光栅，不会反复。
    /// </summary>
    private bool _forceRasterRender;
    /// <summary>上一拍看到的 `Version` —— 只给「没调 present 的老程序」判"这一拍还在画"用。</summary>
    private int _lastSeenVersion = -1;
    private bool _rendering;

    /// <summary>后台那半的分段耗时，随帧交到 UI 线程一起打点（`(DSL, 解析, 光栅+PNG, PNG 字节, 图元数)`）。</summary>
    private (double DslMs, double ParseMs, double RasterMs, int PngBytes, int Figures)? _pendingStats;

    /// <summary>每多少帧打一条分段耗时日志（0 = 关）。</summary>
    internal static int StatsEvery = 30;
    private int _statsFrames;

    /// <summary>窗口**实际**出了多少帧 —— 分段耗时只说明"单帧多快"，这个说明"每秒真出几帧"。</summary>
    private readonly System.Diagnostics.Stopwatch _statsWindow = System.Diagnostics.Stopwatch.StartNew();
    private int _statsWindowFrames;

    /// <summary>当前按住不放的手柄键（0 = 没有）。只用于「滑到另一个键时补一条 KeyUp」。</summary>
    private int _padDownKey;

    public DrawWindowPage()
    {
        InitializeComponent();

        // 指针事件：按下/移动/抬起 → 触摸消息（同时补一对鼠标消息，
        // 让按"鼠标"写法的程序在手机上也直接能跑）
        //
        // ⚠ **不能用 `PointerGestureRecognizer.PointerMoved`**（v0.96.326 实测）：
        //   它在安卓上是 **hover 语义**，手指**拖动**过程中一次都不发 —— 而本仓几个
        //   拖条瞄准的游戏（`Examples/basic/gorilla.bas` 的角度/力度条）全靠它。
        //   症状：拖的时候条子一动不动、松手后停在**按下那一刻**的位置（看起来像
        //   "点得中、拖不动"，最容易被误判成程序没处理 `TOUCHMOVE`）。
        //   模拟器实测：3 秒的长 swipe 期间每 250ms 采一次画面，绿色填充**一个像素都没动**；
        //   而把同一个位置改成单击，条子立刻跳到该处 ⇒ 按下/抬起是好的、**只有移动丢了**。
        //
        //   改用 `GraphicsView` 自带的三段交互：安卓上由平台触摸事件直接驱动，
        //   拖动中每一帧都来。这套还**跨平台同源**（iOS/桌面同一份代码就有同样的语义），
        //   不必再写一份 `#if ANDROID` 的平台触摸处理器。
        CanvasView.StartInteraction  += (_, e) => PostTouch(e.Touches, down: true);
        CanvasView.DragInteraction   += (_, e) => PostTouch(e.Touches, move: true);
        CanvasView.EndInteraction    += (_, e) => PostTouch(e.Touches, up: true);
        // 取消（父容器截走触摸 / 来电切走）也要收尾 —— 只清状态不发抬起，
        // 程序那边的手势就永远停在"按着"（本仓踩过同类坑，见 CLAUDE.md 的捏合那段）。
        CanvasView.CancelInteraction += (_, _) => PostTouch([], up: true);

        CanvasView.Drawable = _canvas;
        _canvas.UseVector = UseVectorBackend;
        _canvas.OnUnsupported = FallbackToRaster;
    }

    /// <summary>
    /// **矢量后端总开关**（v0.96.180）。开了之后每帧只做"拼 DSL + 解析"，图元直接画到平台画布上：
    /// 不再手搓光栅化、不再编 PNG、UI 侧也不解码 —— 那条路上每帧要新建一张 333KB 位图，
    /// 25fps ≈ 10MB/s 垃圾、10 秒里 GC 跑 355 次（真机实测），"抖动"里属于平台的正是它。
    ///
    /// 落笔面走 MAUI 的 `ICanvas`，**一套代码两端跑**；按用户要求先在安卓上验证，
    /// iOS/桌面不用重写、开这个开关即可。真机上若发现画得不对，把它置 false 就整体回到光栅后端。
    /// </summary>
    internal static bool UseVectorBackend = true;

    /// <summary>
    /// 矢量后端画不出来的图元（自定义指令没实现矢量画法）⇒ **整个窗口回退光栅后端**。
    /// 宁可慢，也别默默少画东西。
    /// </summary>
    private void FallbackToRaster(IReadOnlyCollection<string> kinds)
    {
        if (!_canvas.UseVector) return;
        _canvas.UseVector = false;
        ErrorLog.Warning("VmlDraw", $"矢量后端画不了 {string.Join("/", kinds)} ⇒ 本窗口回退光栅后端");

        // ⚠⚠ **必须重出这一帧** —— 只翻标志位是"下一帧起走光栅"，而**静态程序没有下一帧**。
        //
        // 这里从前写着"当前这帧已经画好了，不重绘（免得闪成空白）"，那句话对**逐帧重画的游戏**
        // 成立（下一拍自然就补上了），对**画完一帧就 `ui_wait()` 挂着的程序**是错的：
        // 屏上会**永久**停在"矢量后端画不出来的那一块是空的"的状态。
        //
        // 实测就是这么发现的（v0.96.306 的渐变描边）：`examples/c/draw_brush.c` 第 5 行
        // 三格 —— 圆（只有渐变描边）整格空白、矩形只剩填充没有边框、渐变直线不见，
        // 而同帧里矢量**支持**的椭圆渐变填充好好的。每一处观察都指向"矢量画了一半就被丢下"。
        //
        // 重出**不会**闪成空白：光栅那条路是后台算好再换（旧帧一直贴在屏上），
        // 这里只是让**正确的帧**稍后替换掉**残缺的帧**。
        _renderedVersion = -1;          // 否则下面那圈"版本没变就返回"会把重出挡掉
        _forceRasterRender = true;
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

        /// <summary>矢量后端要画的那一帧（解析后的文档）。非空且 <see cref="UseVector"/> 时走矢量。</summary>
        private WayCoder.Infra.DrawDocument? _doc;

        /// <summary>本帧里画不出来的图元（自定义指令没实现矢量画法）⇒ 宿主据此回退光栅后端。</summary>
        public Action<IReadOnlyCollection<string>>? OnUnsupported;

        /// <summary>最近一帧在屏幕上的贴图矩形（`AspectFit` 的结果）。</summary>
        private RectF _fit;

        /// <summary>场景尺寸（= PNG 的像素尺寸），坐标反算要用。</summary>
        private double _sceneW = 1, _sceneH = 1;

        /// <summary>
        /// **矢量后端开关**。开了之后不再光栅化、不再编解码 PNG ——
        /// 图元直接画到平台画布上（GPU 光栅化），每帧的托管分配几乎归零。
        /// </summary>
        public bool UseVector { get; set; }

        public void SetFrame(Microsoft.Maui.Graphics.IImage img, double sceneW, double sceneH)
        {
            _image = img;
            _sceneW = sceneW <= 0 ? 1 : sceneW;
            _sceneH = sceneH <= 0 ? 1 : sceneH;
        }

        /// <summary>矢量后端：把这一帧的文档挂上去（引用赋值，天然是原子的）。</summary>
        public void SetDocument(WayCoder.Infra.DrawDocument doc)
        {
            _doc = doc;
            _sceneW = doc.Width <= 0 ? 1 : doc.Width;
            _sceneH = doc.Height <= 0 ? 1 : doc.Height;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // 背景铺满整个视图（含 AspectFit 留出的黑边），否则未覆盖区会是平台默认底色。
            // ⚠ 走 FillSolid 而不是裸 `FillColor = …` —— 理由同下面那处（见 MauiVectorTarget
            //    类注释「渐变的余荫」）。这一处眼下是安全的（每次 Draw 都是新画布），
            //    但同一条链上留一句两种写法，迟早有人照着错的那句写。
            MauiVectorTarget.FillSolid(canvas, 0xFF000000, dirtyRect);

            if (UseVector && _doc is { } doc)
            {
                DrawVectorFrame(canvas, dirtyRect, doc);
                return;
            }

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
        /// 矢量后端出图：把文档里的图元**逐条画到平台画布**上。
        ///
        /// 坐标：场景坐标 → 屏幕坐标用与光栅路径**同一个 AspectFit 结果**（`_fit`），
        /// 通过 `Translate + Scale` 交给画布，图元本身仍按场景坐标画 ——
        /// 这样触摸反算（`ToScene`）用的还是同一个 `_fit`，两条后端不会各算一套。
        /// </summary>
        private void DrawVectorFrame(ICanvas canvas, RectF dirtyRect, WayCoder.Infra.DrawDocument doc)
        {
            if (doc.Width <= 0 || doc.Height <= 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            var s = Math.Min(dirtyRect.Width / doc.Width, dirtyRect.Height / doc.Height);
            var w = (float)(doc.Width * s);
            var h = (float)(doc.Height * s);
            var x = dirtyRect.X + (dirtyRect.Width - w) / 2f;
            var y = dirtyRect.Y + (dirtyRect.Height - h) / 2f;
            _fit = new RectF(x, y, w, h);

            canvas.SaveState();
            canvas.Translate(x, y);
            canvas.Scale((float)s, (float)s);

            // 场景底色（`canvas w h <bg>` 那条），与光栅路径的起始填充一致。
            // ⚠ 必须走 FillSolid 而不是裸的 `FillColor = …`：后者清不掉上一个渐变挂上去的
            //    shader（见 MauiVectorTarget 类注释「渐变的余荫」）。
            MauiVectorTarget.FillSolid(canvas, doc.Background, new RectF(0, 0, doc.Width, doc.Height));

            var target = new MauiVectorTarget(canvas, doc.Width, doc.Height);
            foreach (var f in doc.Figures)
                WayCoder.Infra.DrawCommandRegistry.Get(f.Kind)?.Vector(target, f);
            canvas.RestoreState();

            if (target.Unsupported.Count > 0) OnUnsupported?.Invoke(target.Unsupported);
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

    /// <summary>
    /// 由宿主在打开窗口时注入场景。
    ///
    /// ⚠ 标题**只写 <see cref="Page.Title"/> 这一处**（= Shell 标题栏）。页面里原来还有一个
    /// 自绘的 HeaderLabel 写同一句话，屏幕上就是上下两个一模一样的标题 —— 已删。
    /// </summary>
    internal void Attach(VmlScene scene)
    {
        // 画布重算挂在**容器自己的** SizeChanged 上（先撤再挂，防多次 Attach 重复订阅）。
        //
        // 为什么不靠页面的 `OnSizeAllocated`：它在**转屏时可能只触发一次**，而那一刻
        // `CanvasHost` 的新尺寸还没算出来 ⇒ 读到旧值 ⇒ 判"不用重排" ⇒ 之后再没有第二次机会。
        // 实测表现就是用户报的「转 90° 再转回来，游戏尺寸回不来了」。
        // 容器自己发的事件则一定发生在它**新尺寸已确定**之后。
        CanvasHost.SizeChanged -= OnCanvasHostSizeChanged;
        CanvasHost.SizeChanged += OnCanvasHostSizeChanged;

        // 浮层面板在转屏 / 画布长高之后可能落到屏幕外 ⇒ 尺寸一变就重新夹一次。
        // 挂在**面板自己**上（不是 RootGrid）：它的新尺寸一定已经落定，夹取才拿得到真实值。
        PcKeyboard.SizeChanged -= OnPcKeyboardSizeChanged;
        PcKeyboard.SizeChanged += OnPcKeyboardSizeChanged;

        // ⚠ **清掉上一次留下的画布尺寸请求**。
        //
        // Shell 导航会**复用页面实例**（本仓自己在 SettingsGroupPage 的注释里记过这条），
        // 于是这次的 `WidthRequest` 还是上一局算出来的值。而它会引发一个"没人来纠正"的死角：
        //   · `ShowFrame` 里那句 `if (CanvasView.WidthRequest <= 0) FitCanvas(scene)` 因为非 0 而跳过；
        //   · 页面与容器的尺寸这次都没变 ⇒ `OnSizeAllocated` / `SizeChanged` **一个都不触发**；
        // ⇒ 画面就定格在上一局的尺寸上。
        // 实测复现路径正是用户给的：**先横屏一下（算出并留下横屏的小尺寸）→ 转回 → 退出 → 再进**。
        // 置 -1 让 ShowFrame 的兜底重新生效，随后再主动重算一次拿到精确值。
        CanvasView.WidthRequest = -1;
        CanvasView.HeightRequest = -1;

        // 同一个道理：手柄的收起状态也**别带进新的一局**（页面复用会留着上一局的）。
        // 只置字段、不在这里刷可见性 —— `OnDisappearing` 清了方向判定 ⇒ 这次
        // `OnAppearing`/`OnSizeAllocated` 的 `ApplyOrientation` 必定整套重摆一遍。
        _padCollapsed = false;

        // 同上：上一局报过的方向不能算这一局的（否则新程序一开窗就白收一条 `WindowOrient`）。
        _publishedOrientation = null;
        // "两拍一致"的中间值也要清 —— 新窗口的第一拍不该跟上一局的残留凑成"一致"。
        _pendingViewport = null;

        _scene = scene;
        Title = scene.Title;
        _renderedVersion = -1;

        // 窗口开出来的**两个声明**（`WIN_OPEN_EX` 的 R3/R4，老号一律给默认值）。
        // 必须在 render 之前定下来：`SCR_W/H` 报的可用绘图区随"要不要手柄"变，
        // 程序接下来会照它排版 —— 晚一步就是"先按小画布排一次、再收到 resize 重排"。
        _needGamepad = scene.NeedGamepad;
        _rotation = scene.Rotation;
        _kind = scene.Kind;
        _needKeyboard = scene.NeedKeyboard;
        _keyboardCollapsed = false;      // 新的一局重置收起状态（页面实例被复用）
        _kbFloating = false;             // 连同拖动留下的浮层位置一起复位（`ApplyKeyboardPanelPlacement`）
        _pcDownKey = 0;                  // 上一局按到一半的键别留给下一局
        ApplyOrientationLock(_rotation);
        ApplyPadVisibility();
        ApplyKeyboard();
        InstallHardwareKeyboard();
        // **首帧同步渲染**：异步那条路要等 40ms 的定时器，而实测「窗口一闪而过、什么也没看到」
        // —— 程序若很快调 WIN_CLOSE（或退出），异步首帧根本来不及出。这里就地把第一帧出掉，
        // 之后的变化再走定时器。画布小（几百像素见方），同步编码的代价可以接受。
        RenderIfChanged(synchronous: true);

        // 上面那次首帧兜底跑在**布局完成之前**，`CanvasHost` 尺寸还不可靠；下一帧布局落定后
        // 再精确重算一次（差值超容差才会真设，不会引起抖动）。
        Dispatcher.Dispatch(RefitCanvasIfNeeded);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 整页占满的页面不该带底部 tab 栏（编辑器 / 游戏窗口里没有切页的需求，返回箭头就够）。
        // ⚠ XAML 上的 Shell.TabBarIsVisible 对这种 push 出来的页面**不生效**（实机验过：tab 栏照旧），
        //   必须在 code-behind 设。
        Shell.SetTabBarIsVisible(this, false);

        // 页面是**复用的**（Shell 导航会留下同一个实例），方向判定在 `OnDisappearing`
        // 里已经清掉 ⇒ 这里按当前方向重摆一次。放在 `OnSizeAllocated` 之外是因为
        // 复用页面时尺寸可能一点没变、那个回调根本不再触发。
        ApplyOrientation();

        VmlUiCalls.OnSceneChanged -= OnSceneChanged;
        VmlUiCalls.OnSceneChanged += OnSceneChanged;

        // 定时器只做"版本变了就重绘"，不参与画面合成
        // （另外每拍顺手把**已落定**的视口报出去，见 PublishViewport）
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(40); // ~25fps 的检查频率，实际编码次数取决于场景变化
        _timer.Tick += (_, _) => { PublishViewport(); RenderIfChanged(); };
        _timer.Start();

        Dispatcher.Dispatch(PublishViewport);
    }

    /// <summary>
    /// 把**已经落定的**画布视口报给宿主：记进 <see cref="VmlUiCalls.MeasuredViewport"/>（供
    /// <c>SCREEN_W/H</c> 号段回报给 VML 程序），变了就发一条 `WindowResize`。
    ///
    /// 这是"让程序按真实可用面积自适应"的唯一正确来源 —— 按屏幕尺寸减一个固定 chrome 估算，
    /// 底部一百多 dp 的内容会落在可视区外（实测就是这个症状）。MAUI 的长度单位就是 dp，
    /// 所以这里直接就是绘图单位，不需要再按密度换算。
    ///
    /// ⚠ **只能在 40ms 定时器那拍做，不能放在 `OnSizeAllocated` 里。**
    /// 那个回调是**布局期**回调，转屏时它会被夹在"页面已经变矮、新布局还没换上去"的
    /// 中间态里调一次 —— 那一刻 `CanvasHost` 只剩几十 dp 高（实测横屏转场里量到 `545x46`）。
    /// 记账的后果是**下一局游戏照这个尺寸开窗**：横屏重开俄罗斯方块开出来一个 `544x46`
    /// 的窗口，棋盘被压成一条，整个画面是花的；发消息的后果是**正在跑的游戏被要求重排成
    /// 那个畸形尺寸**。定时器跑在布局落定之后，读到的才是最终值。
    /// </summary>
    private void PublishViewport()
    {
        if (_closing) return;

        // **先方向、后尺寸** —— 同一个 tick 里两条都发时，程序收到尺寸那条时
        // 已经知道新方向了，不必再插一次查询、也不会拿旧方向配新尺寸排一次版。
        PublishOrientation();

        if (CanvasHost.Width <= 0 || CanvasHost.Height <= 0) return;

        var now = (Width: (int)Math.Round(CanvasHost.Width), Height: (int)Math.Round(CanvasHost.Height));

        // ⚠ **连续两拍读到同一个值才算数。**
        //
        // "跑在定时器里"只保证了"不在布局回调内部"，**不保证布局已经结束** ——
        // 转屏要连着走好几趟布局，定时器完全可能落在两趟之间：实测转回竖屏那一拍读到
        // `host=411x525` 之前先读到过 `411x780`（手柄那一行还没被量出来），
        // 于是场景被改成 411×780、还发了一条同值的 `WindowResize` ——
        // 程序照着排的版比真实画布高一截，画面被压下去 0.67 倍。
        // 要求"两拍一致"之后，中间态最多活一拍就被下一个值顶掉，永远不会被当成结论。
        // 代价是真变化晚 40ms 生效，肉眼不可见。
        if (_pendingViewport != now)
        {
            _pendingViewport = now;
            return;
        }

        if (VmlUiCalls.MeasuredViewport == now) return;

        // **视口真的变了就告诉程序**（`WindowResize`）。这条消息协议里一直有，
        // 但宿主**从来没发过** —— 于是转屏、折叠屏、以及折叠条收起手柄，程序全都不知道，
        // 还按开窗时的尺寸排着版（用户看到的就是"收起了手柄但画面没变大"）。
        // 已经有了旧值 ⇒ 这是一次真变化；第一次只是把值记下来。
        if (VmlUiCalls.MeasuredViewport is not null)
        {
            // ① **先把新坐标空间给到场景**，再发消息 —— 程序收到消息就会按新尺寸重画，
            //    那一刻它的坐标系必须已经是新的，否则第一笔就画到界外（被光栅裁掉）。
            //
            //    ⚠ **只给声明过 `VML_WIN_ROTATABLE` 的程序换**（见 `WindowRotation` 的注释）：
            //    老程序（`ui_win_open`）压根不知道坐标系会变，换了之后它继续按老坐标画，
            //    空间变小就**被裁掉一大截** —— 那比原来的"整幅等比缩小"更糟。
            //    老程序保持原样：场景尺寸不动，宿主缩放着显示。
            if (_rotation == WindowRotation.Follow) ResizeScene(now);

            // ② 然后告诉程序（方向那条已经在上面的 `PublishOrientation` 里发过了）
            VmlUiCalls.Current?.PostInput(VmlMsgType.WindowResize, now.Width, now.Height);
        }

        VmlUiCalls.MeasuredViewport = now;
    }

    /// <summary>
    /// **视口变了，就把新的坐标空间整个给到场景**（宿主支持运行期改场景尺寸）。
    ///
    /// 为什么不让程序自己 `ui_win_close()` + 重新 `ui_win_open()` 换空间：那条路要拆掉再建
    /// 一个窗口，屏幕上会闪一下，程序还得自己处理"关窗之后我还跑不跑"这类边界。
    /// 宿主改一行尺寸就够 —— 场景本来就是"程序只追加、宿主负责渲染"的解耦结构，
    /// 尺寸属于渲染侧。
    ///
    /// ⚠ **只在"视口真的变了"时改，不碰开窗时程序自己指定的尺寸**：
    /// `ui_win_open(title, w, h)` 是程序对自己坐标系的主张，在它还没画第一笔时就改掉，
    /// 等于把"我就要 320×240"这个意图抹了（`Examples/c/draw_prims.c` 正是这种：它要的
    /// 就是固定格距的体检图）。**变化之后**旧空间已经没有意义（画布形状都变了），
    /// 这时把可用绘图区整个给它，才是"给程序一块能重新排版的地方"。
    ///
    /// ⚠ 尺寸变了 ⇒ 已渲染的那一帧作废（`_renderedVersion = -1`）：否则画面会停在
    /// 旧尺寸上，而程序可能正卡在 `ui_wait` 上等输入、根本不会重画。
    /// </summary>
    private void ResizeScene((int Width, int Height) box)
    {
        if (_scene is not { } scene) return;
        if (scene.Width == box.Width && scene.Height == box.Height) return;

        scene.Resize(box.Width, box.Height);
        _renderedVersion = -1;
        // 新尺寸立刻落到画布控件上 —— **不能等下一帧的 `SizeChanged`**：
        // 视口变化时画布控件的格子确实会变（会触发），但这里是"内容变了而格子没变"的另一半，
        // 少这一次就有一帧停在旧比例上。
        FitCanvas(scene);
        CanvasView.Invalidate();
    }

    /// <summary>
    /// 屏幕方向变了就给程序发一条 `WindowOrient`（A = 新方向，见 <see cref="VmlUi.Portrait"/>）。
    ///
    /// **"屏幕变了"是两条消息，不是一条**：尺寸（<see cref="VmlMsgType.WindowResize"/>）说的是
    /// "你能画多大"，方向（这一条）说的是"机器横着还是竖着拿"。程序按后者决定怎么分栏
    /// （棋盘放左还是放上、面板横排还是竖排），按前者决定格子算多大 —— 两件事，
    /// 少一条就得让程序自己从尺寸里猜方向（而画布形状是宿主排版算出来的二手信息，会变）。
    ///
    /// ⚠ **判据与 `SCR_ORIENT`（#569）查询共用同一处实现**（<see cref="VmlUiCalls.ScreenOrientation"/>）：
    /// 两处各算一次的话，"查到的"和"收到的"会在某个边界上不一致，那是最难查的一类。
    /// ⚠ 第一次（程序刚开窗）**不发** —— 它自己问过 `SCR_ORIENT` 才开的窗，再被告知一遍
    /// 只会让它白排一次版；与 `WindowResize` 的处理一致。
    /// </summary>
    private void PublishOrientation()
    {
        var orient = VmlUiCalls.ScreenOrientation();
        if (_publishedOrientation == orient) return;
        if (_publishedOrientation is not null && !_closing)
            VmlUiCalls.Current?.PostInput(VmlMsgType.WindowOrient, orient, 0);
        _publishedOrientation = orient;
    }

    /// <summary>本窗口已经报过的方向（`null` = 还没报过，见 <see cref="Attach"/>）。</summary>
    private int? _publishedOrientation;

    /// <summary>上一拍读到的视口 —— 与这一拍相同才认为布局落定（见 <see cref="PublishViewport"/>）。</summary>
    private (int Width, int Height)? _pendingViewport;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // 方向变了就先换布局 —— **必须在 `RefitCanvasIfNeeded` 读 `CanvasHost` 尺寸之前**：
        // 换布局会改它的格子，先读就是拿旧尺寸去算新画布。
        // （`ApplyOrientation` 自带"方向没变就直接返回"，所以这里可以每帧调。）
        ApplyOrientation();

        // 这里只做兜底（首帧渲染时 CanvasHost 尺寸还是 0）；**转屏的重算靠
        // `CanvasHost.SizeChanged`** —— 理由见 Attach 里的注释（页面回调的时序不可靠）。
        // 换过布局那一支由 `SizeChanged` 接手重算（画布的格子变了，它必然发一次）。
        RefitCanvasIfNeeded();
    }

    /// <summary>
    /// 容器尺寸变了（转屏/分屏/收起手柄）就按新视口重算画布。幂等，多处调用无副作用。
    ///
    /// ⚠ **必须推迟到下一帧**：`SizeChanged` 会在**布局过程中**触发，那一刻
    /// `CanvasHost.Width/Height` 还是中间值（远小于最终值）。就地算的话，
    /// `FitCanvas` 会把这个"中间尺寸"写进 `CanvasView.WidthRequest` **定格住**，
    /// 而容器尺寸之后再变也不一定再发一次事件 —— 实测症状就是「再开一局游戏，
    /// 画面小得几乎看不见，飘在一大片空白中间」（棋盘被缩成几十像素的小方块）。
    /// 调度到下一帧时布局已落定，读到的是最终视口。
    /// </summary>
    private void OnCanvasHostSizeChanged(object? sender, EventArgs e)
        => Dispatcher.Dispatch(RefitCanvasIfNeeded);

    private bool? _landscape;

    /// <summary>手柄是否被折叠条收起（横竖屏共用一个状态）。</summary>
    private bool _padCollapsed;

    /// <summary>
    /// 按屏幕方向切换布局。
    ///
    /// **竖屏**（原样）：画布在上、手柄整排在下（`RootGrid` 的三行）。
    /// **横屏**：十字键去最左列、X/Y/A/B 去最右列、画布居中 —— 手柄不再横着摊掉近一半高度；
    ///           SELECT / START 塞进左右键盘区的**内侧角落**（左区右下、右区左下，掌机那个经典摆法）；
    ///           Shell 的 TabBar 隐藏，整屏高度都留给画面。
    ///
    /// ⚠ **只改附加属性（行/列/跨列），不搬控件、不做两套 XAML。**
    ///
    /// 前一版是"运行时搬控件"（全部摘下来再按新布局挂回去），真机上留下过两个回归：
    /// 先闪退（`IllegalStateException: The specified child already has a parent` ——
    /// 漏摘了根级那三个），修完又变成竖屏看不见画面（`CanvasHost` 量到 0）。
    /// 根因是这条路对布局时序太敏感：**摘挂之间控件是没有父级的**，中途任何一次布局
    /// 都能量到 0 并把 0 定格下来。改附加属性则父子关系自始至终不变，没有中间态。
    ///
    /// 唯一的例外是两个按键（SELECT/START）：横屏要落进左右键盘区的角落，跨了父级。
    /// 那只能摘了再挂 —— 所以走 <see cref="MoveBtn"/>，**先摘再挂**，一步都不能省。
    /// </summary>
    private void ApplyOrientation()
    {
        bool landscape = Width > Height;
        if (_landscape == landscape) return;   // 方向没变就别折腾（改布局有代价）
        _landscape = landscape;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (landscape)
        {
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));   // 0 手柄 + 画布
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));   // 1 折叠条
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));   // 2 屏幕键盘
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));   // 0 左手柄
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));   // 1 画布
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));   // 2 右手柄

            PutInGrid(CanvasHost, 0, 1);
            PutInGrid(PadLeftArea, 0, 0);
            PutInGrid(PadRightArea, 0, 2);
            // 键盘横跨三列（它在自己的行上，不会碰到左右手柄区）—— 屏幕键多，宽度就是可读性
            PutInGrid(PcKeyboard, 2, 0);
            Grid.SetColumnSpan(PcKeyboard, 3);
            // 折叠条只占**画布那一列**：两条细线不会横穿两侧手柄区。
            PutInGrid(CollapseBar, 1, 1);
            // ⚠ 跨列**两个方向都要显式重置**：竖屏置过 3，横屏不写回 1 就会从画布列
            //   一直跨到右手柄列（挤掉右侧手柄）。反之亦然。
            Grid.SetColumnSpan(CanvasHost, 1);
            Grid.SetColumnSpan(CollapseBar, 1);

            // SELECT / START 落进左右键盘区的内侧角落（用户指定：左区右下、右区左下）。
            MoveBtn(BtnSelect, PadLeftArea, row: 1, column: 1, margin: new Thickness(4, 4, 0, 0));
            MoveBtn(BtnStart, PadRightArea, row: 1, column: 0, margin: new Thickness(0, 4, 4, 0));

            // 横屏那点高度（实测页面只有 220.7）经不起 TabBar 再吃掉一截。
            Shell.SetTabBarIsVisible(this, false);
        }
        else
        {
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));   // 3 屏幕键盘
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            PutInGrid(CanvasHost, 0, 0);
            Grid.SetColumnSpan(CanvasHost, 3);
            PutInGrid(PcKeyboard, 3, 0);
            Grid.SetColumnSpan(PcKeyboard, 3);
            PutInGrid(CollapseBar, 1, 0);
            Grid.SetColumnSpan(CollapseBar, 3);
            PutInGrid(PadLeftArea, 2, 0);
            PutInGrid(PadCenterArea, 2, 1);
            PutInGrid(PadRightArea, 2, 2);

            // 回中列（竖屏那个"两个键夹在左右手柄中间"的原样）。
            MoveBtn(BtnSelect, PadCenterArea, row: 0, column: 0, margin: Thickness.Zero);
            MoveBtn(BtnStart, PadCenterArea, row: 0, column: 0, margin: Thickness.Zero);

            // 竖屏也别把 TabBar 放回来：它是 VML 程序独占的显示区，底部一条切页栏
            // 既没用又占地方（用户明确要求游戏窗口不带 tab 栏）。
            Shell.SetTabBarIsVisible(this, false);
        }

        ApplyPadVisibility();
        ApplyKeyboard();
    }

    /// <summary>
    /// 把手柄键挪到另一个父级里（横屏时 SELECT/START 要进左右键盘区的角落）。
    ///
    /// ⚠ **必须先摘再挂**：控件还挂在旧父级上时直接 `Add`，Android 侧会抛
    ///   `IllegalStateException: The specified child already has a parent`
    ///   （真机实测直接闪退，堆栈落在 `ViewGroup.addViewInner`）。
    /// ⚠ 落点是 `VerticalStackLayout`（竖屏中列）时行列无效，靠的是**加入顺序** ——
    ///   所以竖屏分支里 SELECT 先于 START 调用。横屏的槽位用 `Margin` 留出缝，
    ///   而不是给外层加 `RowSpacing`：那样竖屏空着的那一格也会算进 3px。
    /// </summary>
    private static void MoveBtn(Button btn, Layout target, int row, int column, Thickness margin)
    {
        (btn.Parent as Layout)?.Children.Remove(btn);
        btn.Margin = margin;
        Grid.SetRow(btn, row);
        Grid.SetColumn(btn, column);
        target.Add(btn);
    }

    /// <summary>
    /// 手柄的显示/隐藏（折叠条中间那个箭头，横竖屏共用）。
    ///
    /// 隐藏整块而不是去改行高：那一行是 `Auto`，`IsVisible=false` 之后自然塌成 0，
    /// 画布（`*` / 手柄两侧的中间列）立刻吃掉腾出来的空间 —— 一个高度都不用自己算。
    /// 横屏下这就是用户要的「键盘可以往两边伸缩」。
    ///
    /// ⚠ 中列（`PadCenterArea`）**只在竖屏**参与：横屏时两个键已经挪进左右键盘区，
    ///   它是个空栈，置可见会让它重新占住画布那一格。
    /// </summary>
    private void ApplyPadVisibility()
    {
        // `_needGamepad` 是这个窗口开出来时的声明（`WIN_OPEN_EX` 的 R4）：
        // 声明"不要手柄"的程序（画图表、放幻灯片）**整块连同折叠条一起不显示** ——
        // 留一条"▲ 收起手柄"给它点，等于让用户去关一个本来就不该出现的东西。
        bool show = _needGamepad && !_padCollapsed;
        PadLeftArea.IsVisible = show;
        PadRightArea.IsVisible = show;
        PadCenterArea.IsVisible = show && _landscape != true;
        CollapseBar.IsVisible = _needGamepad;
        PadToggleBtn.Text = _padCollapsed ? "▼ 展开手柄" : "▲ 收起手柄";
    }

    // ── 屏幕键盘（电脑屏窗口专用）────────────────────────────────────────
    //
    // ## 为什么电脑屏要键盘而图形窗口不要
    //
    // 图形窗口是给**手机游戏**用的，输入是"触摸 + 手柄"；电脑屏窗口是给**PC/Linux 老程序**
    // 用的，那些程序的输入就是键盘。手机上不给键盘，这类程序在真机上根本没法操作。
    // 所以键盘**跟着窗口种类走**：`_needKeyboard` 来自 `WIN_OPEN_PC` 的 R4 声明，
    // 图形窗口恒为 false（不占那一块屏幕）。
    //
    // ## 为什么走 `PostInput` 消息而不是 `getch()` 那条命令行输入队列
    //
    // 两类窗口对应**两条输入路径**，别混：
    //   · conio/curses 那类老程序**跑在命令行页**（它们不画窗口），键盘走 `ShellPage._keys`
    //     → `MauiVml.CaptureIo.ReadChar`（逐字符、带行编辑语义）；
    //   · 调 `ui_win_open_pc` 的程序**自己有窗口**，输入就是 `VML_MSG_KEYDOWN` 消息，
    //     与手柄走的是同一条路（`PostKeyDown`）。
    // 把键盘做在绘图页、却往命令行页的队列里投，就是"按了没反应"最常见的那种错法。
    //
    // ## 键位表是**一处数据**
    //
    // 值一律照 Win32 虚拟键码（`VmlKeys` 是唯一真源），布局照 PC 键盘 —— 老程序作者
    // 脑子里那张键盘就长这样，标签对得上比好看重要。

    /// <summary>
    /// 屏幕键盘的键位表：每行一组 `(标签, 键码, 占几格)`。
    ///
    /// ⚠ 每行**格数合计**决定这一行被切成几列（`Grid` 的星号列），所以行与行之间
    ///   只要格数一致，左右就是对齐的 —— 这正是"看起来像一张键盘"的全部要求。
    /// </summary>
    private static readonly (string Label, int Key, int Span)[][] PcKeyboardRows =
    [
        // 功能键行（老程序的重启/帮助/退出常挂在 F 键上）
        [("Esc", VmlKeys.Escape, 1),
         ("F1", VmlKeys.F(1), 1),   ("F2", VmlKeys.F(2), 1),   ("F3", VmlKeys.F(3), 1),
         ("F4", VmlKeys.F(4), 1),   ("F5", VmlKeys.F(5), 1),   ("F6", VmlKeys.F(6), 1),
         ("F7", VmlKeys.F(7), 1),   ("F8", VmlKeys.F(8), 1),   ("F9", VmlKeys.F(9), 1),
         ("F10", VmlKeys.F(10), 1), ("F11", VmlKeys.F(11), 1), ("F12", VmlKeys.F(12), 1)],

        // 数字行
        [("`", VmlKeys.OemTilde, 1),
         ("1", '1', 1), ("2", '2', 1), ("3", '3', 1), ("4", '4', 1), ("5", '5', 1),
         ("6", '6', 1), ("7", '7', 1), ("8", '8', 1), ("9", '9', 1), ("0", '0', 1),
         ("-", VmlKeys.OemMinus, 1), ("=", VmlKeys.OemPlus, 1), ("⌫", VmlKeys.Backspace, 1)],

        // QWERTY 行
        [("Tab", VmlKeys.Tab, 1),
         ("q", 'Q', 1), ("w", 'W', 1), ("e", 'E', 1), ("r", 'R', 1), ("t", 'T', 1),
         ("y", 'Y', 1), ("u", 'U', 1), ("i", 'I', 1), ("o", 'O', 1), ("p", 'P', 1),
         ("[", VmlKeys.OemOpenBracket, 1), ("]", VmlKeys.OemCloseBracket, 1),
         ("\\", VmlKeys.OemBackslash, 1)],

        // ASDF 行
        [("Ctrl", VmlKeys.Ctrl, 1),
         ("a", 'A', 1), ("s", 'S', 1), ("d", 'D', 1), ("f", 'F', 1), ("g", 'G', 1),
         ("h", 'H', 1), ("j", 'J', 1), ("k", 'K', 1), ("l", 'L', 1),
         (";", VmlKeys.OemSemicolon, 1), ("'", VmlKeys.OemQuotes, 1),
         ("Enter", VmlKeys.Enter, 1)],

        // ZXCV 行
        [("Shift", VmlKeys.Select, 1),
         ("z", 'Z', 1), ("x", 'X', 1), ("c", 'C', 1), ("v", 'V', 1), ("b", 'B', 1),
         ("n", 'N', 1), ("m", 'M', 1), (",", VmlKeys.OemComma, 1),
         (".", VmlKeys.OemPeriod, 1), ("/", VmlKeys.OemQuestion, 1),
         ("Shift", VmlKeys.Select, 2)],

        // 底行：修饰键 + 方向键（老程序的方向键用得极多）
        [("Ctrl", VmlKeys.Ctrl, 2), ("Alt", VmlKeys.Alt, 2), ("空格", VmlKeys.Space, 5),
         ("Alt", VmlKeys.Alt, 2), ("←", VmlKeys.Left, 1), ("↑", VmlKeys.Up, 1),
         ("↓", VmlKeys.Down, 1), ("→", VmlKeys.Right, 1)],

        // 编辑/翻页键（curses 类程序翻页、跳行靠这一排）
        [("Ins", VmlKeys.Insert, 2), ("Del", VmlKeys.Delete, 2), ("Home", VmlKeys.Home, 2),
         ("End", VmlKeys.End, 2), ("PgUp", VmlKeys.PageUp, 3), ("PgDn", VmlKeys.PageDown, 3)],
    ];

    /// <summary>上一次按下的屏幕键盘键 —— 手指滑走时旧键的 `Released` 会丢，靠它补一条 `KeyUp`。</summary>
    private int _pcDownKey;

    /// <summary>
    /// 按需建出屏幕键盘（**只建一次**：页面实例被 Shell 复用，每局重建会越堆越多）。
    ///
    /// 布局是"每行一个 `Grid`、按格数切星号列"，所以行内对齐、行间也大体对齐；
    /// 按钮的 `Pressed`/`Released` 与手柄同一套语义（按住不放要能连发）。
    /// </summary>
    private void BuildPcKeyboard()
    {
        if (_pcKbBuilt) return;
        _pcKbBuilt = true;

        foreach (var row in PcKeyboardRows)
        {
            var grid = new Grid { ColumnSpacing = 3 };
            int cols = 0;
            foreach (var k in row) cols += k.Span;
            for (int i = 0; i < cols; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int col = 0;
            foreach (var k in row)
            {
                var b = new Button
                {
                    Text = k.Label,
                    FontSize = 11,
                    HeightRequest = 30,
                    Padding = 0,
                    CornerRadius = 6,
                };
                int key = k.Key;
                b.Pressed  += (_, _) => OnPcKeyPressed(key);
                b.Released += (_, _) => OnPcKeyReleased(key);
                Grid.SetColumn(b, col);
                Grid.SetColumnSpan(b, k.Span);
                grid.Add(b);
                col += k.Span;
            }
            PcKeyRows.Add(grid);
        }
    }

    /// <summary>
    /// 按下：发 `VmlMsgType.KeyDown`。
    ///
    /// ⚠ 与手柄同款的那条补丁：手指从 A 键滑到 B 键时，Android 只发 B 的 `Pressed`、
    ///   A 的 `Released` **永远不来** ⇒ 不补一条 `KeyUp` 的话程序以为两个键同时按着
    ///   （`Ctrl`/`Shift` 这类修饰键尤其致命：之后每个键都变成组合键）。
    /// </summary>
    private void OnPcKeyPressed(int key)
    {
        if (_pcDownKey != 0 && _pcDownKey != key) PostKeyUp(_pcDownKey);
        _pcDownKey = key;
        PostKeyDown(key);
    }

    private void OnPcKeyReleased(int key)
    {
        if (_pcDownKey == key) _pcDownKey = 0;
        PostKeyUp(key);
    }

    /// <summary>收起/展开屏幕键盘。收起来画布就整块露出来（键盘是**浮在画布上**的，不占布局）。</summary>
    private void OnTogglePcKeyboard(object? sender, EventArgs e)
    {
        _keyboardCollapsed = !_keyboardCollapsed;
        ApplyKeyboard();
    }

    // ── 面板拖动（拖动一开始就"浮层化"）────────────────────────────────
    //
    // ## 为什么拖一下要换成浮层
    //
    // 面板平时**停靠在自己那一行**（`RootGrid` 最后一个 `Auto` 行）—— 那是刻意的：
    // chrome 让画布变小，而不是压在画布上面（老程序的状态行、提示语都画在**底部**）。
    // 但"让画布变小"就意味着它**天生顶不开**：只做平移的话那一行的高度还在，
    // 面板拖上去了、底下留一条空带，画面一点没多出来 —— 拖了等于白拖。
    // 所以拖动一开始就把面板改成**跨整页、贴底**的浮层：只改 `Grid` 附加属性，
    // **不搬控件**（父子关系自始至终不变，理由见 `ApplyOrientation` 的注释：
    // 运行时先摘再挂撞过 `IllegalStateException`，也让 `CanvasHost` 量到过 0）。
    // 换过去的那一刻位置**一模一样**（停靠时它就在页面最底部，浮层贴底也在那儿），
    // 所以看不到跳变；差别只有一条：空掉的那一行塌下来，画布当场长高。
    //
    // ## 为什么不给容器挂手势识别器
    //
    // 本仓记过这条：给**容器**挂 `PanGestureRecognizer` 会把它子控件的按压/点击
    // 整个吃掉（真机表现是"按钮看着在、按下去没反应"）。所以手势只挂在
    // `PcKeyGrip` 那一个 `Label` 上 —— 它没有子控件，拖它碰不到任何按键。
    //
    // ## 夹取
    //
    // 拖出屏幕就找不回来。夹取只用**两个尺寸**算（`RootGrid` 与面板自己的），
    // 不读 `X/Y`：面板的布局矩形是确定的 —— 全宽、贴底（`VerticalOptions=End`，
    // 无 `Margin`），所以基准位就是 `(0, 网格高 − 面板高)`。
    // 全宽 ⇒ 横向余量恒为 0（这是键盘全宽的自然结果，不是夹取写错了）；
    // 纵向可以从"贴底"一路推到"顶到屏幕最上沿"。

    /// <summary>面板已经脱离布局行、变成可拖动的浮层。</summary>
    private bool _kbFloating;

    /// <summary>浮层的平移量（相对"全宽贴底"的基准位）。</summary>
    private double _kbTx;
    private double _kbTy;

    /// <summary>本次拖动开始时的平移量（`PanUpdated` 的 `TotalX/TotalY` 是**累计**值）。</summary>
    private double _kbDragBaseX;
    private double _kbDragBaseY;

    private void OnPcKeyPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                BeginKeyboardFloat();
                _kbDragBaseX = _kbTx;
                _kbDragBaseY = _kbTy;
                break;

            case GestureStatus.Running:
                // `Started` 不是所有平台都发（手势要越过触摸阈值才开始 pan），
                // 所以这里再兜一次 —— 幂等。
                BeginKeyboardFloat();
                SetKeyboardOffset(_kbDragBaseX + e.TotalX, _kbDragBaseY + e.TotalY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // 收尾再夹一次：拖动过程中的夹取用的是"那一刻"的尺寸，
                // 而结束这一拍布局可能已经落定成新值（转屏 / 键盘收起都会变）。
                ClampKeyboardPanel();
                break;
        }
    }

    /// <summary>第一次拖动时才真的换布局 —— 没拖过的用户看到的与从前一字不差。</summary>
    private void BeginKeyboardFloat()
    {
        if (_kbFloating) return;
        _kbFloating = true;
        ApplyKeyboardPanelPlacement();
        ClampKeyboardPanel();
    }

    private void SetKeyboardOffset(double tx, double ty)
    {
        _kbTx = tx;
        _kbTy = ty;
        ClampKeyboardPanel();
    }

    /// <summary>
    /// 按当前状态把面板摆回停靠行 / 切成整页浮层（**只改附加属性**）。
    ///
    /// 停靠态**不在这里设行号** —— 竖屏是第 3 行、横屏是第 2 行，那是
    /// <see cref="ApplyOrientation"/> 的事，两处各设一份必然漂移。
    /// 本方法只负责"复位"与"浮层"两件事。
    /// </summary>
    private void ApplyKeyboardPanelPlacement()
    {
        if (!_kbFloating)
        {
            PcKeyboard.ZIndex = 0;
            PcKeyboard.VerticalOptions = LayoutOptions.Fill;
            PcKeyboard.TranslationX = 0;
            PcKeyboard.TranslationY = 0;
            _kbTx = 0;
            _kbTy = 0;
            return;
        }

        Grid.SetRow(PcKeyboard, 0);
        Grid.SetRowSpan(PcKeyboard, RootGrid.RowDefinitions.Count);
        Grid.SetColumn(PcKeyboard, 0);
        Grid.SetColumnSpan(PcKeyboard, RootGrid.ColumnDefinitions.Count);
        PcKeyboard.VerticalOptions = LayoutOptions.End;
        // 盖在画布/手柄之上：浮层的全部意义就是"能拖到别处去"。
        PcKeyboard.ZIndex = 20;
    }

    private void OnPcKeyboardSizeChanged(object? sender, EventArgs e) => ClampKeyboardPanel();

    /// <summary>把平移量夹到「整块面板都还在页面里」。</summary>
    private void ClampKeyboardPanel()
    {
        double pw = PcKeyboard.Width;
        double ph = PcKeyboard.Height;
        double gw = RootGrid.Width;
        double gh = RootGrid.Height;
        // 还没量出来就这一拍不动（布局期会被调到，别拿半成品尺寸去夹）。
        if (pw <= 0 || ph <= 0 || gw <= 0 || gh <= 0) return;

        double by = gh - ph;                                    // 基准位：全宽、贴底
        _kbTx = Math.Clamp(_kbTx, 0, Math.Max(0, gw - pw));
        _kbTy = Math.Clamp(_kbTy, -by, 0);
        PcKeyboard.TranslationX = _kbTx;
        PcKeyboard.TranslationY = _kbTy;
    }

    /// <summary>
    /// 接上物理键盘（Android 的 Activity 级键分发，见 `Services/HardwareKeys`）。
    ///
    /// ⚠ **只有电脑屏窗口接**（图形窗口的输入契约是"触摸 + 手柄"，照旧不动它）——
    ///   而且是**按窗口种类**接、不是按 `_needKeyboard`：
    ///   那两个参数管的是**两件事** —— 种类决定"这类程序的输入是键盘"，R4 只是说
    ///   "别把屏幕键盘画出来"（程序想要那块屏幕高度）。声明了不要屏幕键盘的程序，
    ///   接个蓝牙键盘照样该能用。
    /// </summary>
    private void InstallHardwareKeyboard()
    {
#if ANDROID
        if (_kind != VmlWinKind.PcScreen) { Services.HardwareKeys.Sink = null; return; }
        Services.HardwareKeys.Sink = (vk, down) =>
        {
            if (down) PostKeyDown(vk); else PostKeyUp(vk);
        };
#endif
    }

    private static void UninstallHardwareKeyboard()
    {
#if ANDROID
        Services.HardwareKeys.Sink = null;
#endif
    }

    /// <summary>
    /// 屏幕键盘的显隐。
    ///
    /// <para>
    /// 键盘在 `RootGrid` 里**占自己的一行**（竖屏 row 3 / 横屏 row 2），与手柄区同一种做法 ——
    /// **chrome 让画布变小，而不是压在画布上面**。这一条是用户定的，理由也直白：
    /// 老程序的状态行、命令行、提示语都画在**底部**，被盖住就是"这个程序用不了"。
    /// </para>
    ///
    /// <para>
    /// ⚠ 把 `PcKeyRows` 置不可见之后，那一行是 `Auto` ⇒ 高度塌成 0，
    /// 画布立刻把空间吃回来（只留那个 26dp 的开关，好让用户再展开）。
    /// **不要**改成 `VerticalOptions="End"` 的浮层去"省得动 `ApplyOrientation`" ——
    /// 那正是第一版的做法，代价是画面底部永远被压掉一块。
    /// </para>
    ///
    /// <para>
    /// ⚠ 画布变矮 ⇒ `PublishViewport` 记进 `MeasuredViewport` 的值也跟着变，
    /// 而它是个**静态**值。对电脑屏窗口本身没有影响（那个窗口永不 `ResizeScene`，
    /// 场景尺寸就是程序声明的分辨率），但**下一局别的程序**可能照这个矮尺寸开窗。
    /// 这是**既有**行为（收手柄、转屏都走同一条路，`WindowResize` 会纠正能纠正的程序），
    /// 不是这里新引入的；真要根治得让 `MeasuredViewport` 区分"含 chrome"与"不含"。
    /// </para>
    /// </summary>
    private void ApplyKeyboard()
    {
        if (!_needKeyboard)
        {
            PcKeyboard.IsVisible = false;
            return;
        }

        BuildPcKeyboard();
        PcKeyboard.IsVisible = true;
        PcKeyRows.IsVisible = !_keyboardCollapsed;
        PcKeyToggle.Text = _keyboardCollapsed ? "⌨ 展开键盘" : "⌨ 收起键盘";
        // 停靠/浮层的附加属性在这里重放一次：`ApplyOrientation` 刚刚设过行号，
        // 而浮层态要把它们整个换掉（也就这里能保证"竖屏 3 行 / 横屏 2 行"仍由那一处说了算）。
        ApplyKeyboardPanelPlacement();
    }

    /// <summary>这个窗口要不要屏幕手柄区（`ui_win_open_ex` 的 R4）；老接口一律 true。</summary>
    private bool _needGamepad = true;

    /// <summary>
    /// 窗口种类（见 `VmlUi.KindOfWinOpen`）。**触摸策略按它分支** ——
    /// 电脑屏窗口只发鼠标（老程序处理的是鼠标，同时再收一对触摸会让它们
    /// 把一次点击当两次输入）。
    /// </summary>
    private VmlWinKind _kind = VmlWinKind.Graphic;

    /// <summary>要不要屏幕键盘（`WIN_OPEN_PC` 的 R4）。图形窗口恒为 false。</summary>
    private bool _needKeyboard;

    /// <summary>用户把键盘收起来了（与手柄一样，是**用户**的选择，不写回场景）。</summary>
    private bool _keyboardCollapsed;

    /// <summary>键位表只建一次（页面实例会被 Shell 复用）。</summary>
    private bool _pcKbBuilt;

    /// <summary>这个窗口的转屏声明（`ui_win_open_ex` 的 R3）；老接口一律 <see cref="WindowRotation.Legacy"/>。</summary>
    private WindowRotation _rotation = WindowRotation.Legacy;

    /// <summary>
    /// 按窗口的声明决定**屏幕要不要跟着转**（`ui_win_open_ex` 的 R3，三档）。
    ///
    /// 只有一种排版的程序（棋盘必须竖着看、赛车必须横着看）与其让它去处理第二种排版，
    /// 不如**根本不让它遇到** —— 锁比"跟着转再缩放"省事，也不会画出它从没写过的形状。
    ///
    /// ⚠ 用 `Portrait`/`Landscape` 而不是 `Unspecified` 锁：前者是"锁死这一种"，
    /// 后者是"交还给系统"（用户开着自动旋转时照样会转）。
    /// ⚠ 退出时必须还原成 `Unspecified`，否则**整个 App 都被这一个游戏锁住方向**。
    /// </summary>
    private void ApplyOrientationLock(WindowRotation rotation)
    {
#if ANDROID
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) return;
            // ⚠ 枚举成员**必须全限定写**：`var so = Android.Content.PM.ScreenOrientation;`
            //   是"把类型当表达式"，CS0119 编不过（踩过：publish 明明失败了，
            //   我只看了管道末尾的退出码就当成成功，结果设备上跑的还是旧包）。
            activity.RequestedOrientation = rotation switch
            {
                WindowRotation.PortraitOnly => Android.Content.PM.ScreenOrientation.Portrait,
                WindowRotation.LandscapeOnly => Android.Content.PM.ScreenOrientation.Landscape,
                // Legacy / Follow：交给系统（用户开着自动旋转就跟着转）
                _ => Android.Content.PM.ScreenOrientation.Unspecified,
            };
        }
        catch (Exception ex)
        {
            // 锁不住方向不是致命问题（程序还能跑，只是会跟着转）—— 记一笔就好
            ErrorLog.Error("VmlDraw", "锁定屏幕方向失败", ex);
        }
#endif
    }

    /// <summary>设置控件在网格中的行列（`Grid.Add` 是列在前，这里统一成 row/column 更好读）。</summary>
    private static void PutInGrid(View view, int row, int column)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
    }

    /// <summary>
    /// 视口尺寸与当前画布请求尺寸不一致就重算。
    /// ⚠ 比的是 **FitSize 算出来的两个数**，不是"宽度变没变"：两维取小之后，
    /// 宽度可能没变而高度变了（视口变矮 ⇒ 要缩得更多），只比宽度就会漏掉这次重排。
    /// </summary>
    private void RefitCanvasIfNeeded()
    {
        // 【诊断脚手架】定位"画面缩成小方块 / 干脆看不见"。定位完即撤。
        //
        // ⚠ 必须走 `Android.Util.Log`（→ logcat），**不能用 `Console.WriteLine`**：
        //   `MauiVml.RunProgram` 在 VML 运行期间会把 `Console.Out` 重定向进一个 StringWriter
        //   （为了把程序输出交回工具返回值），而这个函数正是在运行期间被触发的 ⇒ 输出被它吞掉、
        //   logcat 里一个字都看不到（实测踩过：以为"函数没被调用"，其实是日志被劫持了）。
        // ⚠ **放在所有守卫之前**：守卫里那个 `CanvasHost.Width <= 0` 正是"看不见画面"的
        //   头号嫌疑，放在它后面就永远看不到这次调用到底发生了什么。
#if ANDROID
        Android.Util.Log.Info("WC-DRAW",
            $"scene={(_scene is null ? "null" : $"{_scene.Width}x{_scene.Height}")} " +
            $"host={CanvasHost.Width:F1}x{CanvasHost.Height:F1} root={RootGrid.Width:F1}x{RootGrid.Height:F1} " +
            $"req={CanvasView.WidthRequest:F1}x{CanvasView.HeightRequest:F1} page={Width:F1}x{Height:F1} " +
            $"land={_landscape?.ToString() ?? "?"}");
#endif
        if (_scene is not { } s) return;
        if (CanvasHost.Width <= 0 || CanvasHost.Height <= 0) return;
        var box = CanvasBox();
        var (w, h) = FitSize(s, box.W, box.H);
        if (w <= 0) return;
        if (Math.Abs(CanvasView.WidthRequest - w) <= 0.5 && Math.Abs(CanvasView.HeightRequest - h) <= 0.5) return;
        FitCanvas(s);
        CanvasView.Invalidate();
    }

    /// <summary>
    /// 用户退出游戏窗口后，给程序**多久**自行收场（毫秒）—— 到点还在跑就强制终止。
    ///
    /// 为什么要宽限：正常程序收到 `WindowClose` 后会在下一帧退出主循环（那是**优雅退出**，
    /// 该让它自己走完，比如落盘存档）。但程序**可以不理这条消息**（卡在自己的循环里/死循环），
    /// 那时它就一直在后台烧 CPU —— 用户按了返回却什么都没停掉，这是不可接受的。
    ///
    /// ⚠ 1.5 秒实测**偏长**（用户按返回后能感觉到"卡了一下"）⇒ 收到反馈后改成 **0.5 秒**。
    ///   代价：收场动作超过半秒的程序会被强制终止（落盘那种毫秒级的不受影响）。
    ///   这个值只影响"关窗口"这条路径；"强制停止"按钮是立刻生效的（不等宽限）。
    /// </summary>
    private const int CloseGraceMs = 500;

    private IDispatcherTimer? _closeWatchdog;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closing = true;

        // 清掉方向判定 ⇒ **下次进来一定会重摆一次**（页面实例是复用的，不清就会
        // 带着"我已经摆好了"的状态回来，而 TabBar / 按键位置未必还在）。
        // 之前那版会把 `_landscape` 留着，于是退出时横屏、再进来还是横屏尺寸却仍是旧摆法。
        _landscape = null;

        // **把方向还回去**：锁是给"这一个窗口"用的（只竖屏/只横屏），
        // 不还原就是整个 App 被它锁住方向，退出后首页也转不动了。
        // 传 Legacy = 交给系统（尊重用户自己的自动旋转开关）。
        ApplyOrientationLock(WindowRotation.Legacy);
        // 页面走了，按住的那个手柄键不可能再收到 Released —— 补一条 KeyUp，
        // 否则程序里那条"按住连发"会一直挂着（虽然马上要终止了，但日志里会留个假象）。
        if (_padDownKey != 0) { PostKeyUp(_padDownKey); _padDownKey = 0; }
        if (_pcDownKey != 0) { PostKeyUp(_pcDownKey); _pcDownKey = 0; }
        // 物理键盘的"路由口"必须**跟着页面走**：留着的话，退出这个窗口之后
        // 整个 App 的按键还会继续被投给一个已经关掉的程序（换页、打字全都不对劲）。
        UninstallHardwareKeyboard();
        VmlAudio.StopAll();   // 退出窗口就别再响了（BGM 留着比不响更糟）
        _timer?.Stop();
        _timer = null;
        VmlUiCalls.OnSceneChanged -= OnSceneChanged;

        // 用户点了返回箭头（或被导航走）：告诉程序窗口没了。
        // **这必须在 OnDisappearing 里做** —— 放在别处会漏掉"手势返回"这条路径，
        // 程序就会一直等在 MsgWait 上，直到超时。
        VmlUiCalls.Current?.MarkWindowClosed();

        // 再上一道保险：**退出窗口 = 终止程序**（用户原话「随时按返回，需要终止程序」）。
        // 先礼后兵 —— 上面那条消息是"你自己收场"，这里给 1.5 秒；到点还在跑就直接取消 token。
        // 只发消息不兜底的话，一个不理会 WindowClose 的程序会一直烧着 CPU 活在后台，
        // 而用户以为"我已经退出游戏了"。
        _closeWatchdog?.Stop();
        var watchdog = Dispatcher.CreateTimer();
        watchdog.Interval = TimeSpan.FromMilliseconds(CloseGraceMs);
        watchdog.IsRepeating = false;
        watchdog.Tick += (_, _) =>
        {
            watchdog.Stop();
            _closeWatchdog = null;
            ShellPage.CancelRunningVml();   // 已经自己退了的话，这就是个空操作
        };
        _closeWatchdog = watchdog;
        watchdog.Start();
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

        // ── 出帧判据：**「一帧画完了」，不是「内容变了」**（v0.96.178 修闪烁）──────────
        //
        // 原来判 `scene.Version`，而那个数**每个图元都 +1**：一帧 200 个图元就是 200 次"变了"。
        // 40ms 的定时器撞上哪一次，就把**当时那一刻**的场景贴上去 —— 多半是画到一半的画面
        // （`ui_clear()` 刚清完、棋子还没画出来）。用户看到的就是「俄罗斯方块有时抖动闪烁」：
        // 棋盘一闪一闪地清空又出现。**"内容变了"与"一帧画完了"是两件事。**
        //
        // 判据与程序对齐：`ui_present()` 就是"这帧画完了"（`waycoder_ui.h` 的用法示例每帧末尾
        // 都调，两个游戏也都调）。**没调过 present 的老程序**退到"这一拍内容没再变"——
        // 同样是"画完再说"，只是晚一拍；**都不再是"一变就出图"**。
        // 首帧（synchronous）不受此限：那时程序可能一个图元都还没画，等一拍就会"一闪而过"。
        // `_forceRasterRender` 时不走版本守卫：回退是"同一份内容换条路再画一遍"，
        // 版本号当然没变，按版本判会直接返回、那一帧永远补不回来。
        if (!synchronous && !_forceRasterRender)
        {
            int mark;
            if (scene.EverPresented)
            {
                mark = scene.PresentVersion;
                if (mark == _renderedVersion) return;
            }
            else
            {
                mark = scene.Version;
                var stillChanging = mark != _lastSeenVersion;
                _lastSeenVersion = mark;
                if (stillChanging) return;        // 这一拍还在画 ⇒ 等下一拍
                if (mark == _renderedVersion) return;
            }
            _renderedVersion = mark;
        }
        else
        {
            _renderedVersion = scene.EverPresented ? scene.PresentVersion : scene.Version;
        }

        _rendering = true;

        // ── 这一帧的文本从哪来：**present 那一刻当场拍的快照** ────────────────────
        // 原来是"此刻现拍"（定时器醒来的那一刻 `BuildDsl()`），而 present 到定时器醒来
        // 之间最多隔 40ms —— 期间 VM 早已开始画下一帧（先 `ui_clear()` 再重画），拍到的是
        // **半成品**（实测日志里图元数 17/28/30/36/60 参差不齐就是这个）。
        // 快照已在 `VmlScene.Present()` 里当场拍好，这里只取；老程序（没调 present）
        // 才退到此刻现拍 —— 那条路上"这一拍内容没再变"已经保证程序不在画。
        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        // 回退重出时**不能走 Take** —— 上一拍已经把快照取走了（Take 会清），再取是 null，
        // 整帧直接 return（那条 `if (dsl == null)` 分支）。静态程序本来就只有一帧，
        // 所以这里现拍一次。
        var dsl = (_forceRasterRender || !scene.EverPresented) ? scene.BuildDsl() : scene.TakePresentedDsl();
        _forceRasterRender = false;
        var dslMs = swTotal.Elapsed.TotalMilliseconds;
        if (dsl == null)
        {
            // 同一帧的快照已被另一条路径取走（`FinishRender` 与定时器都会走到这里）
            _rendering = false;
            return;
        }

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
                var swParse = System.Diagnostics.Stopwatch.StartNew();
                var doc = DrawRunner.Parse(dsl);
                var parseMs = swParse.Elapsed.TotalMilliseconds;
                var figures = dsl.Count(c => c == '\n') - 2;   // 去掉 canvas / antialias 两行

                // ── 矢量后端：到这里就完了 ────────────────────────────────────
                // 不再光栅化、不再编 PNG、UI 侧也不解码 —— 把文档挂给画布、让它自己重绘。
                // 每帧的托管分配几乎归零（原来每帧一张 333KB 位图 ⇒ 25fps ≈ 10MB/s 垃圾）。
                if (_canvas.UseVector)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _pendingStats = (dslMs, parseMs, 0, 0, figures);
                        _canvas.SetDocument(doc);
                        CanvasView.Invalidate();
                        LogFrameStats(0, 0, 0);
                    });
                    return;
                }

                var swRaster = System.Diagnostics.Stopwatch.StartNew();
                var png = DrawRunner.ToPng(doc);
                var rasterMs = swRaster.Elapsed.TotalMilliseconds;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _pendingStats = (dslMs, parseMs, rasterMs, png.Length, figures);
                    ShowFrame(png, scene);
                });
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
    /// 给画布定尺寸：**等比缩放到两个方向都装得下**（`min(视口宽/场景宽, 视口高/场景高)`）。
    ///
    /// ## 为什么不只按宽度缩（v0.96.173 改的）
    ///
    /// 原来写的是「宽 = 视口宽，高 = 宽 × 场景高宽比」—— 宽度那一维装得下，**高度完全没管**。
    /// 于是只要场景比视口高，下半截就被 ScrollView 截在可视区外：用户报的
    /// 「内容超出绘图区，下面被键盘区挡住」就是这个（游戏必须能滚动才看得全 = 已经不能玩了）。
    /// 场景为什么会偏高：程序开窗前会先问 `SCREEN_W/H`，而这两个数在**第一次开窗之前**
    /// 只能靠 `AvailableArea` 估算（真实视口要等页面布局完才量得到）。估算偏大 ⇒ 场景偏高。
    ///
    /// 改成两维取小之后，**无论程序按什么尺寸开窗，整幅场景一定完整可见**（多余的那一维留边），
    /// "被切掉一半"这种情形从结构上就不可能出现。代价是估算偏大时画面会等比缩小一点
    /// （用户要的正是"弄小点点"），而估算准的时候缩放比恰好是 1、与原来完全一致。
    ///
    /// ⚠ **视口取不到时必须兜底成场景尺寸**，不能"取不到就不设"：
    /// 首帧是在 `Attach` 里同步渲染的，那时页面还没布局、`CanvasHost.Width/Height` 都是 0 ——
    /// 不设尺寸 ⇒ GraphicsView 零尺寸 ⇒ **画不出来、也点不到**（实测：整块画布全黑，
    /// 触摸全被 `ToScene` 判在图外丢掉）。而 `_renderedVersion` 此时已经记下，
    /// 后续帧不会再触发，于是永远黑着。
    /// </summary>
    private void FitCanvas(VmlScene scene)
    {
        var box = CanvasBox();
        var (w, h) = FitSize(scene, box.W, box.H);
        if (w <= 0) return;
        CanvasView.WidthRequest = w;
        CanvasView.HeightRequest = h;
    }

    /// <summary>
    /// 画布**真正能用**的盒子 = `CanvasHost` 的尺寸再扣掉 `CanvasView` 自己的外边距。
    ///
    /// ⚠ 不扣这一项，画布就比容器大 16dp（上下左右各 8）：贴上去之后底部越界，
    /// 压到下面那个折叠条上 —— 横屏实测能直接看到棋盘下沿被「▲ 收起手柄」那条盖住。
    /// 边距是**从控件本身读的**，不在这里写死数字：改 XAML 里的 `Margin` 不必回来改这里
    /// （写死就是又立了一张"必须手工同步的平行表"）。
    /// </summary>
    private (double W, double H) CanvasBox()
    {
        var m = CanvasView.Margin;
        return (CanvasHost.Width - m.HorizontalThickness,
                CanvasHost.Height - m.VerticalThickness);
    }

    /// <summary>
    /// 画布尺寸的纯计算（拆出来是为了让"要不要重排"能比较**同一个结果**，而不是各算一遍）。
    /// 视口尺寸传 0 表示"还没量到" —— 那一维退回按场景原尺寸算（缩放比 1）。
    /// </summary>
    private static (double W, double H) FitSize(VmlScene scene, double viewW, double viewH)
    {
        if (scene.Width <= 0 || scene.Height <= 0) return (0, 0);
        var scale = 1.0;
        if (viewW > 0) scale = viewW / scene.Width;
        if (viewH > 0)
        {
            var byH = viewH / scene.Height;
            if (byH < scale) scale = byH;
        }
        if (scale <= 0) scale = 1;
        return (Math.Max(1, scene.Width * scale), Math.Max(1, scene.Height * scale));
    }

    private void ShowFrame(byte[] png, VmlScene scene)
    {
        try
        {
            // ⚠ 尺寸**只在还没有尺寸时兜底设一次**（首帧在 Attach 里同步出图，那时页面还没布局）。
            //   别每帧都重算：`FitSize` 是按视口比例算的**带小数的值**，视口测量有一点浮动
            //   （ScrollView 内容变化、滚动条出现/消失）就跟着变 ⇒ 画布**每帧微调一次尺寸**，
            //   整块棋盘跟着缩放/位移，看着就是「抖动」（实测连拍里抓到过两张比满格小 1% 的）。
            //   之后一律交给 `OnSizeAllocated` —— 那里才是"视口真的变了"的判据（带 0.5dp 容差）。
            if (CanvasView.WidthRequest <= 0) FitCanvas(scene);

            // UI 线程这一半也要计时：PNG **解码** + 平台贴图，在真机上未必比后台那半便宜。
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _canvas.SetFrame(Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(new MemoryStream(png)),
                scene.Width, scene.Height);
            var decodeMs = sw.Elapsed.TotalMilliseconds;
            CanvasView.Invalidate();
            var blitMs = sw.Elapsed.TotalMilliseconds - decodeMs;

            LogFrameStats(decodeMs, blitMs, png.Length);
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlDraw", "帧贴图失败", ex);
        }
    }

    /// <summary>
    /// 每 <see cref="StatsEvery"/> 帧往 logcat 打**一行分段耗时**（tag `WCVML`）。
    ///
    /// 为什么要它：真机上"卡"是个笼统的体感，而这一帧的代价分在**两半**上 ——
    /// 后台线程的 `DSL 拼装 / 解析 / 光栅化+PNG 编码`，与 UI 线程的 `PNG 解码 / 贴图`。
    /// 不分开量就不知道优化该往哪儿使劲（桌面基准只覆盖了后台那半）。
    /// 频率压得很低（每 30 帧一条），对帧率的影响可以忽略；要临时静音把 <see cref="StatsEvery"/> 调大即可。
    ///
    /// 读法：`adb logcat -s WCVML`
    /// </summary>
    private void LogFrameStats(double decodeMs, double blitMs, int pngBytes)
    {
        if (StatsEvery <= 0) return;
        var st = _pendingStats;
        _pendingStats = null;
        var windowFrames = ++_statsWindowFrames;
        if (++_statsFrames % StatsEvery != 0) return;

        // 窗口实际帧率（2 秒窗口）—— 与"单帧耗时"是两件事：单帧 10ms 也可能因为
        // 出帧判据/定时器节拍只出 2 帧/秒（本仓在编辑器那边吃过一次"快的其实是没人要"）。
        var elapsed = _statsWindow.Elapsed.TotalMilliseconds;
        _statsWindow.Restart();
        _statsWindowFrames = 0;
        var realFps = elapsed > 0 ? windowFrames * 1000.0 / elapsed : 0;

        var uiMs = decodeMs + blitMs;
        var bgMs = st?.DslMs + st?.ParseMs + st?.RasterMs ?? 0;
        var msg = $"每帧：DSL {st?.DslMs ?? 0:F1} / 解析 {st?.ParseMs ?? 0:F1} / 光栅+PNG {st?.RasterMs ?? 0:F1} " +
                  $"= 后台 {bgMs:F1}ms｜解码 {decodeMs:F1} / 贴图 {blitMs:F1} = UI {uiMs:F1}ms" +
                  $"｜图元 {st?.Figures ?? 0}｜PNG {pngBytes / 1024}KB｜合计 {bgMs + uiMs:F0}ms" +
                  $"｜**实际 {realFps:F1} fps**（{windowFrames} 帧 / {elapsed:F0}ms）";

        // ⚠ 平台守卫：`Android.Util.Log` 在 iOS 上不存在 —— 本页是两端共编的，
        //   少一处守卫就多一次「只在某个平台编译不过」（本仓在编辑器探针上踩过一轮）。
#if ANDROID
        Android.Util.Log.Info("WCVML", msg);
#else
        System.Diagnostics.Debug.WriteLine("[WCVML] " + msg);
#endif
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

    private void PostTouch(PointF[] points, bool down = false, bool move = false, bool up = false)
    {
        var calls = VmlUiCalls.Current;
        if (calls == null) return;

        // 取消事件（`CancelInteraction`）**不带坐标** —— 用最近一次的位置收尾。
        // 宁可位置略有偏差，也不能让程序那边的手势永远停在"按着"。
        Point pt;
        if (points is { Length: > 0 }) { pt = new Point(points[0].X, points[0].Y); _lastTouch = pt; }
        else if (_lastTouch is { } last) pt = last;
        else return;

        // ⚠ **必须换算成场景坐标**：画布是缩放贴上去的（还可能有黑边/滚动偏移），
        // 直接把视图坐标发过去，程序按自己的网格算就会偏 —— 屏幕越大/越扁偏得越多。
        if (_canvas.ToScene(pt) is not { } scene) return;
        var x = scene.X;
        var y = scene.Y;

        /* **两对消息里发哪一对，由窗口种类决定**（判据收在 `VmlUi.SuppressTouch` 一处）。
           图形窗口：两对都发（老行为一字不改）。
           电脑屏窗口：**只发鼠标** —— 老程序处理的是鼠标；同时收到一对触摸
           会把一次点击算成两次输入，症状是"点一下动两下"。 */
        bool wantTouch = !VmlUi.SuppressTouch(_kind);
        if (down)
        {
            if (wantTouch) calls.PostInput(VmlMsgType.TouchDown, x, y);
            calls.PostInput(VmlMsgType.MouseDown, x, y);
        }
        if (move)
        {
            if (wantTouch) calls.PostInput(VmlMsgType.TouchMove, x, y);
            calls.PostInput(VmlMsgType.MouseMove, x, y);
        }
        if (up)
        {
            if (wantTouch) calls.PostInput(VmlMsgType.TouchUp, x, y);
            calls.PostInput(VmlMsgType.MouseUp, x, y);
        }

        // 【诊断脚手架】证明"拖动真的来了"。默认关（`TraceTouch` 打开才打），
        // 因为拖动每帧一条、开着会淹掉 logcat。读法：`adb logcat -s WC-TOUCH`
#if ANDROID
        if (TraceTouch)
            Android.Util.Log.Info("WC-TOUCH",
                $"{(down ? "down" : move ? "move" : "up")} view=({pt.X:F1},{pt.Y:F1}) scene=({x},{y})");
#endif
    }

    /// <summary>最近一次触摸的**视图**坐标（取消事件不带坐标时收尾用）。</summary>
    private Point? _lastTouch;

    /// <summary>把每一次指针事件打进 logcat（`adb logcat -s WC-TOUCH`）。默认关 —— 拖动每帧一条。</summary>
    internal static bool TraceTouch;

    // 屏幕手柄：手机没有物理键盘，不把这些键做出来的话「方向键 + 动作键写的游戏」在真机上没法玩。
    // 键码全部取自 VmlKeys（唯一真源），且刻意映射到自然键盘等价键 —— 同一份程序接物理键盘也能玩。
    //
    // ## 为什么是 Pressed/Released 而不是 Clicked（v0.96.173）
    //
    // `Clicked` 是「抬手时」才触发一次，于是**按住不放没有任何后续事件**：俄罗斯方块那种
    // "按住 ← 连续左移"就做不出来（程序只能收到一次 KeyDown）。改成 `Pressed`/`Released`
    // 之后，按下发 `KeyDown`、抬手发 `KeyUp`，程序就能自己拿定时器做连发（DAS）——
    // 这也是所有游戏手柄的语义。单点仍然是"先 Down 后 Up"，只是中间隔了真实的按压时长。
    private void OnPadPressed(object? sender, EventArgs e)
    {
        var key = PadKeyOf(sender);
        if (key == 0) return;
        // 手指从一个键滑到另一个键时，Android 只发新键的 Pressed，旧键的 Released 就丢了。
        // 先替旧键补一条 KeyUp，否则程序会以为**两个键同时按着**（连发会一直挂在旧方向上）。
        if (_padDownKey != 0 && _padDownKey != key) PostKeyUp(_padDownKey);
        _padDownKey = key;
        PostKeyDown(key);
    }

    private void OnPadReleased(object? sender, EventArgs e)
    {
        var key = PadKeyOf(sender);
        if (key == 0) return;
        if (_padDownKey == key) _padDownKey = 0;
        PostKeyUp(key);
    }

    /// <summary>手柄按键 → 键码（0 = 认不出）。这张表就是"哪个按钮是哪个键"的**唯一**一处。</summary>
    private int PadKeyOf(object? sender)
    {
        if (ReferenceEquals(sender, PadLeft)) return VmlKeys.Left;
        if (ReferenceEquals(sender, PadRight)) return VmlKeys.Right;
        if (ReferenceEquals(sender, PadUp)) return VmlKeys.Up;
        if (ReferenceEquals(sender, PadDown)) return VmlKeys.Down;
        if (ReferenceEquals(sender, FaceA)) return VmlKeys.PadA;
        if (ReferenceEquals(sender, FaceB)) return VmlKeys.PadB;
        if (ReferenceEquals(sender, FaceX)) return VmlKeys.PadX;
        if (ReferenceEquals(sender, FaceY)) return VmlKeys.PadY;
        if (ReferenceEquals(sender, BtnStart)) return VmlKeys.Start;
        // SELECT 与 PAUSE 都映射到「SELECT 键」，中间区只留两个键（手柄上也就是这两个）
        if (ReferenceEquals(sender, BtnSelect)) return VmlKeys.Select;
        return 0;
    }

    /// <summary>
    /// 收起 / 展开手柄区（折叠条中间那个箭头）。横屏下就是用户要的「键盘可以往两边伸缩」。
    ///
    /// ⚠ 画布尺寸变了，程序**在开窗那一刻**就问过 `SCREEN_W/H` 并按它排好版了 ——
    /// 之后靠 `WindowResize` 消息补（见 `OnSizeAllocated`），但那是"能重排的程序才跟得上"。
    /// 所以折叠适合「先收起来再看」或「这个程序本来就不用手柄」。
    /// </summary>
    private void OnTogglePad(object? sender, EventArgs e)
    {
        _padCollapsed = !_padCollapsed;
        ApplyPadVisibility();
        ApplyKeyboard();
    }

    private static void PostKeyDown(int code)
    {
        var calls = VmlUiCalls.Current;
        if (calls == null) return;
        calls.PostInput(VmlMsgType.KeyDown, code, 0);
    }

    private static void PostKeyUp(int code)
    {
        var calls = VmlUiCalls.Current;
        if (calls == null) return;
        calls.PostInput(VmlMsgType.KeyUp, code, 0);
    }
}
