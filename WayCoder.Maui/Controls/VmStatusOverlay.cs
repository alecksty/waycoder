using Microsoft.Maui.Controls.Shapes;
using WayCoder.Maui.Services;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Controls;

/// <summary>
/// 「VM 状态」浮层 —— 叠在画布（或命令行输出区）上的一小块半透明读数区，**可拖动、可关闭**。
///
/// ## 它解决的是什么
///
/// 真机上"游戏卡死"（画面定格、触摸没反应）那次，最后是靠**日志时间戳反推**
/// （汇编完成 08:22:05 + 超时 120 秒 = 最后出帧 08:24:05）才定下案 —— 而 PC 卡在哪、
/// 程序还在不在跑、超时还剩几秒，这些数**本来就摆在进程里**，只是没地方看。
/// 面板把"程序是死了还是在等"从推断变成读数。
///
/// ## 触摸：「可拖可关」与「不挡游戏」是对立的，只能收窄而不是消除
///
/// 面板要能拖、能点 ✕，就**必须接住触摸**；而它盖的正是游戏的操作面。
/// 本仓的定论是「**触摸命中看的是有没有背景**」（有背景的布局一定参与命中测试，
/// `Transparent` 的反而不参与）—— 所以这里按"**谁能点谁才有背景**"来分层：
///
/// <list type="bullet">
///   <item><b>可拖的"面"</b>（标题格 + 正文块）：都带**与面板同色**的背景。
///     视觉上融进去看不出边界，但对命中就是"有背景"⇒ 接得住触摸。**这正是它存在的意义**。</item>
///   <item><b>三个键</b>（小窗 / 大窗 / `✕`）：各自有点按，**不参与拖动** ——
///     重叠的话点一下就会与拖动抢手势。</item>
///   <item>⚠ <b>正文块是"可拖的面"之一</b>，所以 `InputTransparent = false`
///     ⇒ **点正文不再穿透给下面的画布**。这是用户明确要的取舍
///     （2026-09-25：「整条除了那几个图标按键，其他都可以拖动」），
///     换来的是"抓手"从很窄的标题一格变成整条。见 `_body` 那段注释。</item>
/// </list>
///
/// ## 两档形态：小窗 / 大窗
///
/// 正文**只有一份实现**（`VmlStatusSnapshot.FormatLines(detailed)`，纯逻辑在共享层可自测），
/// 面板这边只管摆：行数、字号、内边距、两个图标的明暗，全在 `ApplyMode` 一处。
/// 小窗不是"大窗少画几行"——它连内边距都收窄（浮层盖在游戏画面上，留白就是白占的地方）。
///
/// ⚠ 穿透这一条**必须真机验**（桌面与模拟器的命中语义未必一致）。若哪天发现正文区又开始挡操作，
/// 退路是把整块设成可交互并接受"遮住的那一小块归面板"。
///
/// ## 它挂在页面的**根网格**上（跨满行列），所以"浮在所有控件之上"
///
/// 两个宿主页（命令行页 / 绘图窗口）都把它当根网格的**最后一个子元素** + `ZIndex` ——
/// 于是它能被拖到顶栏、输入行、手柄区上去，拖动范围也是**整页**而不是原来那一格。
/// `ZIndex` 不是装饰：绘图页切横竖屏时会把两侧手柄按钮**摘下来再挂回去**，
/// 而平台子视图的插入次序是按 ZIndex 算的，不写就会被重挂上来的按钮压住。
///
/// ## 拖动的三条本仓铁律
///
/// ① **别用"相对视图"的增量驱动自身位移** —— 那是**无解的反馈回路**：Android 侧
///    `PanGestureRecognizer` 的 `TotalX/TotalY` 是拿 `MotionEvent.GetX()` 算的，而那个值含
///    `translationX`；我们一边读它、一边拿它改自己的 translation ⇒ 视图一动基准就动
///    ⇒ **手指停住也在抖**（真机实测报的"拖动抖动"，与编辑器运行面板 v0.96.432 同一个坑）。
///    所以 Android 走**原生触摸的 `RawX/RawY`（屏幕坐标）**，与视图移没移动无关。
///
///    ⚠⚠ **`RawX/RawY` 是屏幕像素，而 `TranslationX/Y` 是设备无关单位（dp）——必须除密度。**
///    这一条是**真机量出来的**，别再靠推理：`adb shell input swipe` 拖 (+200,+300) px，
///    浮层实测移动 (+540,+831) px（比值 2.70 / 2.77，屏幕密度 2.75）⇒ 拿"移动/dp"当像素用，
///    跑得比手指快 2.75 倍，看着就是"跟不住手、越拖越偏"。
///    平台侧的对应事实（`Microsoft.Maui.Platform.ViewExtensions`）：
///    `PlatformInterop.Set(…, platformView.ToPixels(view.TranslationX), …)` —— **它替我们乘密度**。
/// ② **一整段手势只认第一下**：父容器打断时会先 `Canceled` 再重新 `Started`，
///    每次 `Started` 都重取基准就等于把已拖走的距离重新算成 0 ⇒ 位置往回跳。
/// ③ **松手必须夹回父容器**：不然一次甩动就能把它拖到屏幕外，**再也找不回来**（只能重开页面）。
/// </summary>
public sealed class VmStatusOverlay : ContentView
{
    /// <summary>刷新间隔（毫秒）。250ms 足够"看着像实时"，又不至于每帧重排这段文本。</summary>
    private const int RefreshMs = 250;

    /// <summary>
    /// 面板底色。固定深色**不跟主题**：它叠在 VML 程序自己的画面上，而那个画面的底色多半是深色
    /// —— 跟随主题会在浅色主题下变成"白底盖白画面"。
    ///
    /// ⚠ **不透明度取 95%，不是更透**：它经常盖在命令行页的输出文字上（那一页是白底黑字），
    /// 80% 那版真机一看，下层的字整片透上来与读数叠在一起，**两行都读不清** ——
    /// 「半透明底衬」适合画在深色游戏画面上（`block_bench.c` 那种），盖在浅色文本上则是负担。
    /// </summary>
    private static readonly Color PanelBg = Color.FromArgb("#F20E0E12");

    private readonly Label _text;
    private readonly Border _body;
    private readonly Image _smallIcon;
    private readonly Image _bigIcon;
    private IDispatcherTimer? _timer;

    /// <summary>这一整段拖动是不是已经开始过（见类注释铁律 ①）。</summary>
    private bool _dragging;
    private double _startX, _startY;

    /// <summary>宿主实际出帧速率 —— 只有绘图页知道，由它每帧写进来（命令行页不设，恒 0）。</summary>
    public double Fps { get; set; }

    public VmStatusOverlay()
    {
        _text = new Label
        {
            // 等宽：面板是**列对齐**的一堆十六进制，比例字体会让每行长度不一、看着像乱的
            FontFamily = EditorTypography.FontFamilyName,
            FontSize = 10,
            TextColor = Color.FromArgb("#FFE6E6E6"),
        };

        // ── 把柄：`"VM 状态"` + `✕`，两者**各自带背景**（见类注释：有背景才参与命中测试）──
        //
        // ⚠ 它们是正文块的**兄弟**，不是子元素。放进去的话就得靠 `CascadeInputTransparent`
        //   把"容器不吃、子元素照吃"这层意思表达出来，而 `Border` 上根本没有这个属性
        //   （它是 `Layout` 的）—— 与其赌平台的级联语义，不如让结构本身没有歧义。
        var titleLabel = new Label
        {
            Text = L.Pick("VM 状态", "VM status"),
            FontFamily = EditorTypography.FontFamilyName,
            FontSize = 10,
            TextColor = Color.FromArgb("#FF9A9AA8"),
            VerticalOptions = LayoutOptions.Center,
        };
        var titleBox = new Border
        {
            BackgroundColor = PanelBg,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            // ⚠ **右上角必须是直角**：它右边紧挨着 ✕ 那一格，给这里也切圆角的话，
            //   两个圆角之间会漏出一条背景色的缝 —— 真机上就是"关闭按钮左边一个奇怪的缺口"。
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8, 0, 0, 0) },
            Padding = new Thickness(10, 4),
            Content = titleLabel,
        };

        // ✕ 用 Label + 手势而不是 Button：Button 在 Android 上有最小尺寸和自己的样式，
        // 这么小一格会被撑开。给它的背景是**必须的**（无背景就不参与命中测试，点不动）。
        var closeLabel = new Label
        {
            Text = "✕",
            FontSize = 12,
            TextColor = Color.FromArgb("#FFE6E6E6"),
            VerticalOptions = LayoutOptions.Center,
        };
        var closeBox = new Border
        {
            BackgroundColor = PanelBg,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0, 8, 0, 0) },
            Padding = new Thickness(12, 4),
            Content = closeLabel,
        };
        var closeTap = new TapGestureRecognizer();
        closeTap.Tapped += (_, _) => Close();
        closeBox.GestureRecognizers.Add(closeTap);

        // ── 小窗 / 大窗：两个图标，点哪个切哪个 ─────────────────────────────────
        //
        // 用 `Image` + `TapGestureRecognizer` 而不是 `ImageButton`：同 ✕ 那条理由
        // （Android 的按钮有 44dp 最小尺寸，这一格会被撑开，标题栏白白高一截）。
        // 图标**底色与面板同色**是必须的 —— 没背景的布局在 Android 上不参与命中测试（点不动）。
        //
        // 当前那一档**亮、另一档暗**（`Opacity`）：两个图标长得不一样，但"现在是小窗还是大窗"
        // 光看图标本身说不出来（两个都在屏幕上），必须有个态 —— 与本仓"能点与否要靠样子说"
        // 是同一条（那边是靠按钮的底/框，这边是靠明暗）。
        _smallIcon = new Image { Source = "icon_vm_small.png", WidthRequest = 15, HeightRequest = 15 };
        _bigIcon = new Image { Source = "icon_vm_big.png", WidthRequest = 15, HeightRequest = 15 };
        var smallBox = MakeIconBox(_smallIcon, () => SetDetailed(false),
            L.Pick("小窗（只看基本状态）", "Small panel (basic status only)"));
        var bigBox = MakeIconBox(_bigIcon, () => SetDetailed(true),
            L.Pick("大窗（含全部寄存器）", "Large panel (all registers)"));

        var bar = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            ],
        };
        Grid.SetColumn(titleBox, 0);
        Grid.SetColumn(smallBox, 1);
        Grid.SetColumn(bigBox, 2);
        Grid.SetColumn(closeBox, 3);
        bar.Children.Add(titleBox);
        bar.Children.Add(smallBox);
        bar.Children.Add(bigBox);
        bar.Children.Add(closeBox);

        // 拖动挂在**把柄那一格**上，而不是整块面板：整块挂上去等于整块挡触摸。
        // 也只挂 title 那一格（不含 ✕）—— 两者重叠的话，点 ✕ 会与拖动抢手势。
        //
        // ⚠ **Android 走原生触摸的屏幕绝对坐标，不用 `PanGestureRecognizer`**：
        //   那个的增量是**相对视图自身**的（Android 侧 `MotionEvent.GetX()` 含 translation），
        //   而这里恰恰是"用位移移动自身" ⇒ 视图一动基准跟着动 ⇒ **手指停着它也会来回抖**
        //   （真机报的"拖动抖动"；与本仓 v0.96.432 编辑器运行面板那条是同一个回路）。
        //   `RawX/RawY` 是**屏幕坐标**，与视图移没移动无关，回路自然断开。
        // ⚠ **整条都能拖**（除了小窗/大窗/✕ 三个键）：`titleBox` 那一格在 `bar` 里占
        //   `Star` 列（剩余宽度全归它），横着抓哪儿都行；**`_body` 那一整块正文也挂上** ——
        //   这一条是用户明确要的「整条除了那几个图标按键，其他都可以拖动」。
        //   三个键**不挂**：它们各自有点按，重叠会与拖动抢手势。
        // ⚠⚠ **只能挂 `titleBox`，`_body` 要等它建出来再挂**（见下面那段）——
        //   原先这里顺手把 `_body` 也挂上，而 `_body` 是在**这之后**才 `new` 的
        //   ⇒ `null.HandlerChanged += …` 直接抛 `NullReferenceException`，
        //   于是**一进命令行页 App 就退回桌面**（`ShellPage` 里就住着这个浮层）。
        //   编译期一个字都不报 —— 这类"顺序"错误只有跑起来才知道。
        //   真机日志特征：`ExceptionManager match exception type failed for com.tanso.waycoder`
        //   ＋ `willFinishToHome=true`，**没有** AndroidRuntime FATAL。
#if ANDROID
        titleBox.HandlerChanged += (_, _) => AttachNativeDrag(titleBox);
#else
        AttachPanDrag(titleBox);
#endif

        // ── 正文：**不吃触摸**（这块才是盖住游戏画面的部分）──
        //
        // 内边距/字号由 `ApplyMode` 按小窗/大窗再改一遍（小窗要的是"不占地方"，
        // 光少几行还不够 —— 上下那几圈留白在小窗里就是纯浪费）。
        // 所以这里存成字段而不是局部变量。
        _body = new Border
        {
            BackgroundColor = PanelBg,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0, 0, 8, 8) },
            Padding = new Thickness(10, 0, 10, 8),
            Content = _text,
            // ⚠⚠ **这里从 `true` 改成了 `false`** —— 这是"整条都能拖"的另一半实现，
            //   也是一次**有意的取舍交换**（用户 2026-09-25 明确要「整条除了图标按键都能拖」）：
            //     · 原样（`true`）= 面板不挡游戏：手指落在读数文字上会**穿到下面那层**
            //       （画布 / 输出区），代价是**只有很窄的标题那一格能抓**。
            //     · 现在（`false`）= 正文自己接住触摸 ⇒ **点正文不再穿透给画布**。
            // ⚠ 这一句由 `SetBodyDraggable` 按**宿主页面**覆写（见 `_bodyDraggable` 那段）：
            //   命令行页 ⇒ false（能拖）、绘图窗口页 ⇒ true（穿透，游戏照常玩）。
            //   想两者兼得是**做不到**的：Android 的手势序列独占，而 MAUI 的
            //   `InputTransparent` 又是静态属性，表达不了"条件穿透"。
            //   （试过"Down 先不吃、位移超阈值再吃"⇒ 面板直接收不到 Move，完全拖不动。）
            InputTransparent = !_bodyDraggable,
        };

        // 正文能不能拖由宿主页定 —— **必须在这里应用**（`_body` 到这一行才存在）
        _body.InputTransparent = !_bodyDraggable;

        // 挂拖动 —— **必须在这里挂**：`_body` 到这一行才存在，挂早了就是空引用
        //（见上面那段注释：那正是"一进命令行页就退回桌面"的成因）。
#if ANDROID
        _body.HandlerChanged += (_, _) => AttachNativeDrag(_body);
#else
        if (_bodyDraggable) AttachPanDrag(_body);
#endif

        var root = new Grid
        {
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)],
        };
        Grid.SetRow(bar, 0);
        Grid.SetRow(_body, 1);
        root.Children.Add(bar);
        root.Children.Add(_body);
        Content = root;

        ApplyMode();        // 首次就把形态摆对（`IsVisible` 还是 false，这里只摆样子）
        IsVisible = false;
    }

    /// <summary>
    /// 做标题栏里一个"图标格"：**与面板同色的底**（有背景才参与命中测试）+ 图标 + 点按回调。
    ///
    /// 左右两格都不切圆角 —— 它们夹在标题格与 ✕ 格之间，切了会在接缝处漏出背景色
    /// （真机上就是✕左边那条缺口，见 `titleBox` 的注释）。
    /// </summary>
    private static Border MakeIconBox(Image icon, Action onTap, string hint)
    {
        var box = new Border
        {
            BackgroundColor = PanelBg,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = new Thickness(9, 4),
            Content = icon,
        };
        SemanticProperties.SetHint(box, hint);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        box.GestureRecognizers.Add(tap);
        return box;
    }

    /// <summary>点了某个形态图标：**落盘**（两个页面共享）再就地摆成那个形态。</summary>
    private void SetDetailed(bool detailed)
    {
        MauiVmStatusStore.Detailed = detailed;
        ApplyMode();
        // 形态一变，面板的宽高跟着变 —— 夹一次，免得缩小之后还留在越界的位置上
        // （缩小时不会越界，**放大时才会**：右下角那一半可能被顶出父容器外）。
        ClampIntoParent();
    }

    /// <summary>
    /// 按当前形态摆样子：正文的**内容 / 字号 / 内边距** + 两个图标的明暗。
    ///
    /// 全部收在这一处 —— 三种时机（构造、显示、点图标）走的是同一条，
    /// 分开写必然出现"点了图标内容变了、字号没变"这类半对。
    /// </summary>
    private void ApplyMode()
    {
        bool detailed = MauiVmStatusStore.Detailed;
        _text.FontSize = detailed ? 10 : 9;
        _body.Padding = detailed ? new Thickness(10, 0, 10, 8) : new Thickness(8, 0, 8, 5);
        _smallIcon.Opacity = detailed ? 0.35 : 1.0;
        _bigIcon.Opacity = detailed ? 1.0 : 0.35;
        Refresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // 显示/隐藏都由**这一个属性**驱动：调用方只管 `IsVisible`，起停定时器与夹取位置在这里收口
        if (propertyName == IsVisibleProperty.PropertyName) Sync();
    }

    /// <summary>用户点了右上角的 ✕：**落盘**（两个页面共享那一个开关）并隐藏自己。</summary>
    private void Close()
    {
        MauiVmStatusStore.Visible = false;
        IsVisible = false;
    }

    private void Sync()
    {
        // 未附加到窗口时 `Dispatcher` 可能还没有 —— 那时什么都不做（附加后会再收到一次属性变更）
        if (IsVisible && Dispatcher is { } d)
        {
            _dragging = false;          // 上一次的拖动状态别带进这一次
            ApplyMode();                // 另一页可能刚改过形态（那个开关是全局的）
            ClampIntoParent();          // 期间可能转过屏 / 换过页面，位置先夹回可见范围
            _timer ??= CreateTimer(d);
            _timer.Start();
        }
        else
        {
            _timer?.Stop();
        }
    }

    private IDispatcherTimer CreateTimer(IDispatcher d)
    {
        var t = d.CreateTimer();
        t.Interval = TimeSpan.FromMilliseconds(RefreshMs);
        t.Tick += (_, _) => Refresh();
        return t;
    }

    private void Refresh()
    {
        var snap = VmlUiCalls.Current?.CaptureStatus(Fps) ?? new VmlStatusSnapshot();
        _text.Text = string.Join("\n", snap.FormatLines(MauiVmStatusStore.Detailed));
    }

    // ── 拖动 ──────────────────────────────────────────────────────────────

#if ANDROID
    /// <summary>按下时手指的**屏幕**坐标 —— 整个拖动过程的唯一基准（见类注释的回路说明）。</summary>
    private float _downRawX, _downRawY;
#endif

    // ⚠ `_dragAttached` **不属于 Android 段**：桌面/iOS 那条 `AttachPanDrag` 也要用它做去重
    //   （见下面 469 行附近）。先前它被圈在 Android 段里，而 `#else` 分支引用它 ——
    //   **Apple 目标一直编译不过**（桌面构建、Android 构建都看不出来）。

    /// <summary>
    /// 已经接过拖动的**格** —— 每格独立记账。
    ///
    /// ⚠ 原先是**一个 bool**（那时只有标题那一格要拖）。"整条都能拖"之后，多格共用
    ///   同一个 bool 会让第二格被自己的守卫挡在门外，表现是「标题能拖、正文拖不动」，
    ///   而**一点报错都没有** —— 这类"静默少挂一处"正是本仓最花时间的故障形态。
    ///   同时它也防重复：`HandlerChanged` 可能来不止一次，重复 `+=` 会让拖动跑 N 倍。
    /// </summary>
    private readonly HashSet<View> _dragAttached = new();

    // ⚠ **这里必须关掉 `#if ANDROID`**（v0.96.485 修）：下面这两个成员是**跨平台**的 ——
    //   `SetBodyDraggable` 里本身就有 `#if !ANDROID` 分支（"桌面/iOS 上正文拖不动"那处补挂），
    //   整段被圈进 Android 段之后那个分支成了**永远编译不到的死代码**，
    //   而且 `_body` 构造里对 `_bodyDraggable` 的引用（在 `#if` 外）直接编译不过 ——
    //   **表现是 macOS / iOS 从来没编译过**（桌面构建和 Android 构建都看不出来）。

    /// <summary>
    /// 正文块**是否也参与拖动**（由宿主页面设，见 <see cref="SetBodyDraggable"/>）。
    ///
    /// ⚠⚠ 这是**按页面分**的开关，不是风格选项 —— Android 的手势序列（Down→Move→Up）
    ///   是**独占**的：Down 被谁消费，后续事件就全归谁。所以同一块区域只有两种可能：
    ///     · **吃**（`InputTransparent = false`）⇒ 能拖，但**盖住的东西再也收不到触摸**；
    ///     · **不吃**（`= true`）              ⇒ 穿透，底下的画布照常玩，**但拖不动**。
    ///   两者**不可能兼得**。本仓 v0.96.438 已经定过这条：「浮层『可拖可关』与
    ///   『不挡游戏』是对立的，只能靠分层收窄」—— 这里是把它落成"按页面收窄"：
    ///     · **命令行页**：底下没有游戏 ⇒ 开（正文也能拖，抓哪儿都行）；
    ///     · **绘图窗口页**：底下就是游戏 ⇒ 关（正文穿透，只有把柄那一格能拖）。
    /// </summary>
    private bool _bodyDraggable;

    /// <summary>
    /// 设定正文块能不能拖（见 <see cref="_bodyDraggable"/> 那段注释：这是**按页面**的取舍）。
    /// 必须在构造之后调（`_body` 要先存在）。
    /// </summary>
    public void SetBodyDraggable(bool on)
    {
        _bodyDraggable = on;
        if (_body is null) return;
        _body.InputTransparent = !on;
#if !ANDROID
        // ⚠ **补挂**：构造那会儿 `_bodyDraggable` 还是默认值，非 Android 这条分支
        //   （下面构造里那句 `if (_bodyDraggable) AttachPanDrag(_body)`）当时没挂上，
        //   事后才打开就再也挂不上了 —— 表现是"桌面/iOS 上正文拖不动"。
        //   Android 那条是无条件挂的，不受影响（`AttachPanDrag` 自带去重）。
        if (on) AttachPanDrag(_body);
#endif
    }

#if ANDROID
    /// <summary>
    /// 把拖动接到把柄的**平台视图**上，收 `MotionEvent.RawX/RawY`（屏幕绝对坐标）。
    ///
    /// 为什么不用 `PanGestureRecognizer`：它的 `TotalX/TotalY` 在 Android 上是拿
    /// `MotionEvent.GetX()` 算的，而那个值**含 `translationX`** —— 我们一边读它、一边拿它改
    /// 自己的 translation ⇒ 视图一动基准就动 ⇒ 手指停住也会有几十像素的来回抖。
    /// 这与编辑器运行面板那条（v0.96.432）是同一个反馈回路，那边也是改走原生触摸才断开的。
    /// </summary>
    private void AttachNativeDrag(View target)
    {
        // ⚠ **先判平台视图、再记账**：Handler 还没建好时 `PlatformView` 是 null，
        //   这一趟若已经记了账，等 Handler 真建好那次就会被守卫挡掉，**再也接不上**。
        if (target.Handler?.PlatformView is not Android.Views.View v) return;
        if (!_dragAttached.Add(target)) return;
        v.Touch += OnPlatformTouch;
    }

    private void OnPlatformTouch(object? sender, Android.Views.View.TouchEventArgs e)
    {
        if (e.Event is not { } me) { e.Handled = false; return; }

        switch (me.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                _startX = TranslationX;
                _startY = TranslationY;
                _downRawX = me.RawX;
                _downRawY = me.RawY;
                break;

            case Android.Views.MotionEventActions.Move:
                // ⚠⚠ **`RawX/RawY` 是屏幕像素，而 `TranslationX/Y` 是设备无关单位（dp）**
                //   ⇒ 必须除密度换算。少这一步的表现**不是"完全不动"，而是"跑得比手指快"**
                //   （1080p/440dpi 上快 2.75 倍），看着就像乱飘、跟不住手。
                //   编辑器运行面板那份也是这么换的（`EditorPage._dragRawStartY` 那两句）。
                var density = DeviceDisplay.MainDisplayInfo.Density;
                if (density <= 0) density = 1;
                TranslationX = _startX + (me.RawX - _downRawX) / density;
                TranslationY = _startY + (me.RawY - _downRawY) / density;
                break;

            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
                // 手指划出屏幕 / 被系统截走时也要夹一次 —— 那条路径同样会把它留在界外
                ClampIntoParent();
                break;
        }

        // ⚠ **一律吃掉** —— 这一句是"能拖"的前提，不是可有可无的收尾：
        //   Android 的手势序列是**独占**的，Down 被谁消费，后续 Move/Up 就全归谁。
        //   曾经试过"Down 先不吃、等位移超阈值再开始吃"，想让轻点穿透给下面的画布 ——
        //   结果是**面板再也收不到 Move**（手势序列早被判给了下层），
        //   症状是「VM 状态条完全拖动不了」。这条路的结论：**同一块区域只能二选一**。
        e.Handled = true;
    }
#else
    /// <summary>给一格挂上桌面/iOS 的拖动（Android 那条走原生触摸，见 <see cref="AttachNativeDrag"/>）。</summary>
    private void AttachPanDrag(View target)
    {
        // 去重：`SetBodyDraggable` 可能事后再来挂一次（见那里的注释）
        if (!_dragAttached.Add(target)) return;
        // ⚠ `PanGestureRecognizer` **一个实例只能挂一处** —— 它自带手势状态，
        //   两格共用会在抬起/取消时串台。所以每格新建一个。
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        target.GestureRecognizers.Add(pan);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // ⚠ 铁律 ①：父容器/别的识别器打断会先 Canceled 再重新 Started ——
                //   每次都重取基准的话，已经拖走的距离会被重新算成 0，手指一动就跳回起点。
                if (_dragging) break;
                _dragging = true;
                _startX = TranslationX;
                _startY = TranslationY;
                break;

            case GestureStatus.Running:
                if (!_dragging) break;
                // `TotalX/Y` 是**相对手势起点**的累计位移（不是相对视图当前位置）——
                // 所以移动自身不会反过来污染它（编辑器那条"用相对坐标驱动自身尺寸"的回路在这里不存在）。
                TranslationX = _startX + e.TotalX;
                TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _dragging = false;
                ClampIntoParent();      // 铁律 ②
                break;
        }
    }
#endif

    /// <summary>
    /// 把面板夹回父容器范围内 —— **松手时必须做**：不然一次甩动就把它拖到屏幕外，
    /// 而它只是个浮层，没有"复位"入口，用户只能重开页面才找得回来。
    ///
    /// ⚠ **上下界必须按 `Margin` 折算**：面板的静止位置 = 父容器左上角 + Margin
    ///   （`ComputeFrame` 就是这么算的），而 `Translation` 是相对**那个位置**的增量。
    ///   写成 `[0, 父宽 - 面宽]` 的话，左边那 `Margin.Left` 一格**永远够不到**
    ///   （14dp 在 2.75 密度下就是 38px 的死区），右边同理能多拖出去那么多。
    ///   本仓记过同族的一条：「同一个函数两条分支只对一边」——边界这类东西，
    ///   写之前先问一句"这个量相对谁"。
    /// </summary>
    private void ClampIntoParent()
    {
        if (Parent is not VisualElement p) return;
        if (p.Width <= 0 || p.Height <= 0 || Width <= 0 || Height <= 0) return;
        double left = -Margin.Left, top = -Margin.Top;
        TranslationX = Math.Clamp(TranslationX, left, Math.Max(left, p.Width - Width - Margin.Left));
        TranslationY = Math.Clamp(TranslationY, top, Math.Max(top, p.Height - Height - Margin.Top));
    }
}
