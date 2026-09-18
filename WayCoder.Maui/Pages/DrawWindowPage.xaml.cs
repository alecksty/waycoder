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
        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += (_, e) => PostPointer(e, down: true);
        pointer.PointerMoved += (_, e) => PostPointer(e, move: true);
        pointer.PointerReleased += (_, e) => PostPointer(e, up: true);
        CanvasView.GestureRecognizers.Add(pointer);

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
        _canvas.UseVector = false;      // 下一帧起走光栅；当前这帧已经画好了，不重绘（免得闪成空白）
        ErrorLog.Warning("VmlDraw", $"矢量后端画不了 {string.Join("/", kinds)} ⇒ 本窗口回退光栅后端");
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
            // 背景铺满整个视图（含 AspectFit 留出的黑边），否则未覆盖区会是平台默认底色
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(dirtyRect);

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

            // 场景底色（`canvas w h <bg>` 那条），与光栅路径的起始填充一致
            canvas.FillColor = MauiVectorTarget.ColOf(doc.Background);
            canvas.FillRectangle(0, 0, doc.Width, doc.Height);

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

        _scene = scene;
        Title = scene.Title;
        _renderedVersion = -1;
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
        if (CanvasHost.Width > 0 && CanvasHost.Height > 0)
        {
            var now = (Width: (int)CanvasHost.Width, Height: (int)CanvasHost.Height);

            // **视口真的变了就告诉程序**（`WindowResize`）。这条消息协议里一直有，
            // 但宿主**从来没发过** —— 于是转屏、折叠屏、以及这条折叠条收起手柄，
            // 程序全都不知道，还按开窗时的尺寸排着版（用户看到的就是"收起了手柄但画面没变大"）。
            // 判据是"与上次实测值不同"，而不是"OnSizeAllocated 被调用"：这个回调在布局期
            // 会连着触发好几次，不比较就会把一堆无意义的 resize 灌进消息队列。
            if (VmlUiCalls.MeasuredViewport is { } prev && prev != now && !_closing)
                VmlUiCalls.Current?.PostInput(VmlMsgType.WindowResize, now.Width, now.Height);

            VmlUiCalls.MeasuredViewport = now;
        }

        // ⛔ **横屏布局切换暂时停用**（2026-09-18）。
        //
        // 实测它引入了两个回归：竖屏下画布完全看不见、横屏也不对。原因是"运行时搬控件"
        // 这条路对布局时序很敏感（先解除父级、改行列定义、再挂回去，中间任何一步让
        // `CanvasHost` 量到 0 就会连锁失败），而在没有实测数据的情况下盲改只会越叠越多。
        //
        // 方法与 XAML 里的命名都**保留着**（见 ApplyOrientation），下次重做时直接启用即可 ——
        // 但重做前必须先拿到 `RefitCanvasIfNeeded` 里那行 `[WC-DRAW]` 的真实数值。
        // ApplyOrientation();

        // 这里只做兜底（首帧渲染时 CanvasHost 尺寸还是 0）；**转屏的重算靠
        // `CanvasHost.SizeChanged`** —— 理由见 Attach 里的注释（页面回调的时序不可靠）。
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

    /// <summary>
    /// 按屏幕方向切换布局。
    ///
    /// **竖屏**（原样）：画布在上、手柄整排在下。
    /// **横屏**：十字键去最左列、X/Y/A/B 去最右列、画布居中 —— 手柄不再横着摊掉近一半高度；
    ///           SELECT / START 塞进左右键盘区的**内侧角落**（左区右下、右区左下，掌机那个经典摆法）；
    ///           折叠条与 Shell 的 TabBar 一并隐藏，整屏高度都留给画面。
    ///
    /// ⚠ 只做"搬控件"，**不做两套 XAML** —— 后者会让每个按钮的事件处理器挂两遍。
    /// ⚠ `Grid.Add(view, column, row)` 的参数是**列在前**（与 `Grid.SetRow/SetColumn` 的书写顺序相反），
    ///    极易写反；所以统一走 <see cref="PutInGrid"/> / 下面这种带注释的 Add。
    /// </summary>
    private void ApplyOrientation()
    {
        bool landscape = Width > Height;
        if (_landscape == landscape) return;   // 方向没变就别折腾（搬控件有代价）
        _landscape = landscape;

        // 一律先"**全部**离场"。
        //
        // ⚠ **必须包含根级那三个**（CanvasHost / CollapseBar / PadArea）：`ApplyOrientation`
        //    首次被调用时它们还挂在 XAML 定义的父子关系上，只摘手柄那几块是不够的 ——
        //    `Add` 会抛 `IllegalStateException: The specified child already has a parent`。
        //    真机实测直接闪退，堆栈落在 `ViewGroup.addViewInner`。
        RootGrid.Children.Remove(CanvasHost);
        RootGrid.Children.Remove(CollapseBar);
        RootGrid.Children.Remove(PadArea);
        PadArea.Children.Remove(PadLeftArea);
        PadArea.Children.Remove(PadCenterArea);
        PadArea.Children.Remove(PadRightArea);
        PadCenterArea.Children.Remove(BtnSelect);
        PadCenterArea.Children.Remove(BtnStart);
        PadLeftArea.Children.Remove(BtnSelect);
        PadRightArea.Children.Remove(BtnStart);

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (landscape)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));   // 0 左手柄
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));   // 1 画布
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));   // 2 右手柄
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            RootGrid.Children.Remove(PadArea);      // 整排手柄的容器在横屏下不再需要
            PutInGrid(CanvasHost, 0, 1);
            PutInGrid(PadLeftArea, 0, 0);
            PutInGrid(PadRightArea, 0, 2);

            PadLeftArea.Add(BtnSelect, 2, 2);       // 左区右下角
            PadRightArea.Add(BtnStart, 0, 2);       // 右区左下角（column=0, row=2）

            CollapseBar.IsVisible = false;
            Shell.SetTabBarIsVisible(this, false);
        }
        else
        {
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            PutInGrid(CanvasHost, 0, 0);
            PutInGrid(CollapseBar, 1, 0);
            RootGrid.Add(PadArea, 0, 2);            // column=0, row=2

            PadArea.Add(PadLeftArea, 0, 0);
            PadArea.Add(PadCenterArea, 1, 0);
            PadArea.Add(PadRightArea, 2, 0);

            PadCenterArea.Add(BtnSelect);
            PadCenterArea.Add(BtnStart);

            CollapseBar.IsVisible = true;
            Shell.SetTabBarIsVisible(this, true);
        }
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
        var (w, h) = FitSize(s, CanvasHost.Width, CanvasHost.Height);
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
    /// 1.5 秒足够任何守规矩的程序反应，又短到用户察觉不出"卡了一下"。
    /// </summary>
    private const int CloseGraceMs = 1500;

    private IDispatcherTimer? _closeWatchdog;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closing = true;
        // 页面走了，按住的那个手柄键不可能再收到 Released —— 补一条 KeyUp，
        // 否则程序里那条"按住连发"会一直挂着（虽然马上要终止了，但日志里会留个假象）。
        if (_padDownKey != 0) { PostKeyUp(_padDownKey); _padDownKey = 0; }
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
        if (!synchronous)
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
        var dsl = scene.EverPresented ? scene.TakePresentedDsl() : scene.BuildDsl();
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
        var (w, h) = FitSize(scene, CanvasHost.Width, CanvasHost.Height);
        if (w <= 0) return;
        CanvasView.WidthRequest = w;
        CanvasView.HeightRequest = h;
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
    /// 收起 / 展开手柄区（折叠条中间那个箭头）。
    ///
    /// 隐藏整块而不是去改行高：那一行是 `Auto`，`IsVisible=false` 之后自然塌成 0，
    /// 画布（`*`）立刻吃掉腾出来的空间 —— 一个高度都不用自己算。
    ///
    /// ⚠ 画布高度变了，但 VML 程序**在开窗那一刻**就问过 `SCREEN_W/H` 并按它排好版了，
    /// 之后它并不知道窗口变了（协议里没有"尺寸变化"这条消息）。所以折叠适合
    /// 「先收起来再看」或「这个程序本来就不用手柄」，别指望跑着的游戏会跟着重排。
    /// </summary>
    private void OnTogglePad(object? sender, EventArgs e)
    {
        var collapse = PadArea.IsVisible;
        PadArea.IsVisible = !collapse;
        PadToggleBtn.Text = collapse ? "▼ 展开手柄" : "▲ 收起手柄";
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
