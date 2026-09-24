using System.Diagnostics;
using System.Text;
using Microsoft.Maui.Graphics.Text;
using WayCoder.Infra;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Controls;

/// <summary>
/// 自绘代码画布 —— 虚拟滚动 + 等宽字体 + 语法高亮 + 行号栏 + 错误波浪线。
///
/// **为什么自绘**：改造前是「透明 <c>Editor</c> + 垫底高亮 <c>Label</c>」，两层都把**整份文件**
/// 变成控件内容 —— 大代码块会拆出上万 Span（项目里已有的 100KB 降级护栏注释就写着这会 ANR）。
/// 自绘只画**可见的那几十行**，代价与文件大小无关。
///
/// **几个刻意的选择**（都是踩过或查到实证的，别改回去）：
/// - 文本与行号都用 <c>DrawText(IAttributedText, …)</c> 而非 <c>DrawString</c>：Android 的
///   <c>DrawString</c> 每次都新建一个 <c>StaticLayout</c>，且 y 语义与 iOS 差一整个字号
///   （Android 是 em 底、iOS 是基线）；<c>DrawText</c> 是左上角锚定、两端一致。
/// - 触摸走 <c>GraphicsView</c> 自带的 Start/Drag/EndInteraction：它已经把同一批触摸从平台
///   转发了，再叠 <c>PanGestureRecognizer</c> 等于同一手势被两套代码处理。
/// - 滚动自己做（不用 <c>ScrollView</c> 包巨型画布）：250 万行 × 18pt 的内容高度远超
///   View 尺寸上限，而且 Android 滚动时子 View 不重绘。
/// - 行高固定、不算平台行高：「第 N 行 → y」必须能精确算出来。
/// - **run 上不写 `TextAttribute.FontName`**：写了会被 MAUI 变成 Android 的
///   `TypefaceSpan(族名)`，而那个 API 只认系统字体族名、没有 asset 重载 —— 打包字体
///   喂进去静默回落成比例字体，「汉字 = 2 列」立刻不成立。不写则布局回落用 `canvas.Font`，
///   走的是 <c>FontExtensions.ToTypeface</c> 的 <c>CreateFromAsset</c> 分支，能加载打包字体。
///   详见 <see cref="EditorTypography.CanvasFontName"/> 与 <c>BuildLineRuns</c>。
/// - **定位用平台实测推进量，不用设计值**（<see cref="MeasureAdvances"/>）：Android 把**每个字形的
///   推进量取整**，13 号下半角真值 6.5 实际按 7 走 —— 用设计值定位等于拿一把平台没在用的尺子，
///   长行会越往右越偏（实测 107 列差 53.5px）。逐字形累加实测推进量 ⇒ 与渲染同源，任何字号都对得上。
/// - **整行一次 <c>DrawText</c>**（<see cref="DrawLineRuns"/>）：语法色是同一串里的多个 run。
///   早先按网格逐段画是为了「每段重新按回网格、截断取整误差」；改用实测推进量之后误差不存在了，
///   于是可以把一行十几次调用收成 1 次（小字号下屏上几百行，这一下差十几倍）。
/// </summary>
public sealed class CodeCanvasView : GraphicsView, IDrawable
{
    // ── 数据 ──

    private ITextSource? _doc;
    private string _filePath = "";
    private Syntax? _syntax;
    private bool _isDark;
    private bool _editable;

    // ── 视口 ──

    private float _firstLine;      // 首个可见行（float：惯性滚动要平滑，不能整行跳）
    private float _scrollX;
    private float _velocityY, _velocityX;
    private IDispatcherTimer? _fling;

    /// <summary>
    /// 上一次布局分配的**高度** —— 用来识别「视口大小变了」（软键盘弹出/收起、旋转、分屏）。
    /// 初值 0 且首帧必定更高，所以第一次布局不会被误判成收缩。
    /// </summary>
    private double _lastHeight;

    // ── 选择与光标 ──

    private long _caretLine = -1;

    /// <summary>
    /// 浏览光标（只读态那根<b>不闪烁</b>的竖线）所在列 —— 字符下标。
    /// 与编辑态的 <see cref="EditingCursor"/> 是两套：后者属于平台输入框，
    /// 前者只读浏览时也要有，否则「按上下键到底动没动」根本看不出来。
    /// </summary>
    private int _caretCol;

    /// <summary>
    /// 长按已经成词、正在**拖动扩选**中。为 true 时拖动改的是选区端点（<see cref="ExtendSelectionTo"/>），
    /// 而不是滚动视口 —— 一个手势里「长按」和「拖动」的语义要靠这个标志分开。
    /// </summary>
    private bool _selecting;

    // ── 行渲染缓存 ──

    private const int MaxCachedLines = 512;

    /// <summary>
    /// 一行的**绘制几何**——按行号缓存，建一次、每帧复用。
    ///
    /// 只缓存整行 <c>AttributedText</c> 是不够的：分词每帧都重做一遍的话，滚动时全烧在 CPU 上。
    /// 这里把「整行的带色文本对象」（= 分词 + 切 run + 取色 + new 对象）一次建好，
    /// 绘制循环里只剩一次 <c>DrawText</c>。
    ///
    /// **Android 上还缓存平台排版**（见 <see cref="NativeLayout"/>）—— 那份缓存**与字号绑定**，
    /// 所以调字号**必须**把整份行缓存清掉（<see cref="ResetTypography"/>）。这一点与「文本变了才清」
    /// 是两条不同的失效条件，先前这里写着「缓存是字体无关的、调字号不必清」，加了排版缓存之后
    /// 那句话就不成立了。
    /// </summary>
    private sealed class LineRuns
    {
        /// <summary>整行一次的带色文本（语法色是它内部的多个 run）。空行 / 无内容为 null。</summary>
        public IAttributedText? Whole;

        /// <summary>空行（什么都不画）—— 共用一个实例，免得每个空行都建一个对象。</summary>
        public static readonly LineRuns Empty = new();

#if WINDOWS
        /// <summary>
        /// 每段的（起点、长度、颜色）。**Windows 专用**：Win2D 后端没有
        /// <c>DrawText(IAttributedText, …)</c> 的实现（那一支直接 throw，见
        /// <see cref="DrawLineRuns"/> 的注释），只能逐段 <c>DrawString</c> —— 而逐段画就得知道
        /// 每段画什么色、从哪个 x 起。颜色**直接存 <see cref="Color"/>**，不从
        /// <c>TextAttribute.Color</c> 那个十六进制串每帧反解一遍（这是绘制热路径）。
        /// null = 整行素色（<see cref="Single"/> 的超长行兜底）。
        /// </summary>
        public List<(int Start, int Length, Color Color)>? Segments;
#endif

#if ANDROID
        /// <summary>
        /// 本行的**平台排版缓存**（Android）。见 <see cref="TryDrawCachedLayout"/>：
        /// MAUI 的 <c>DrawText</c> 每次调用都无条件 <c>new StaticLayout(...)</c> 并随即 <c>Dispose</c>，
        /// 没有任何缓存缝合点 —— 一屏 55 行、每行一次完整排版，实测就是 **78ms/帧**（真机小米 13）。
        /// 这里把编译好的排版留着复用，绘制退化成一次 <c>layout.Draw</c>。
        /// </summary>
        public Android.Text.StaticLayout? NativeLayout;

        /// <summary>排版所依赖的 <c>SpannableString</c> —— **不能 Dispose**：layout 画的时候还要读它。</summary>
        public Android.Text.SpannableString? NativeSpan;

        /// <summary>这份排版是按哪个字号编出来的。字号一变就得重编。</summary>
        public float NativeFontSize;
#endif
    }

    /// <summary>
    /// 把一份平台排版连同它的 span 一起释放。
    ///
    /// 行缓存有**四个**丢弃点（淘汰 / <see cref="InvalidateLine"/> / <see cref="InvalidateAll"/> /
    /// <see cref="ResetTypography"/> 走 <see cref="ClearLineCache"/>），漏一个就是原生对象泄漏 ——
    /// 所以释放只有这一个出口。
    ///
    /// **非 Android 上退化成空操作**（那边没有排版缓存，走平台原路）：这样调用点就不必到处套
    /// `#if ANDROID` —— 少一处守卫就少一次「只在某个平台上编译不过」的机会。
    /// </summary>
    private static void DisposeLine(LineRuns? runs)
    {
#if ANDROID
        if (runs is null || ReferenceEquals(runs, LineRuns.Empty)) return;
        runs.NativeLayout?.Dispose();
        runs.NativeLayout = null;
        runs.NativeSpan?.Dispose();
        runs.NativeSpan = null;
#endif
    }

    private readonly Dictionary<long, LineRuns> _lineCache = new();
    private readonly List<long> _cacheOrder = [];

    /// <summary>
    /// 清空整份行缓存（连排缓存一起释放）。
    /// **所有丢弃点都必须走这里** —— 逐个 <c>Clear()</c> 的话，Android 上那些
    /// <c>StaticLayout</c>/<c>SpannableString</c> 原生对象就没人释放了。
    /// </summary>
    private void ClearLineCache()
    {
        // ⚠ **先释放再 Clear** —— 反了就是遍历一个空字典，原生对象一个都没释放。
        foreach (var runs in _lineCache.Values) DisposeLine(runs);
        _lineCache.Clear();
        _cacheOrder.Clear();
    }

    /// <summary>
    /// 每行的**显示总宽**——只给 <see cref="ComputeMaxScrollX"/> 用（横向滚动上限）。
    ///
    /// 量与光标定位同一把尺子（<see cref="MeasurePrefixWidth"/>，逐字形累加实测推进量），
    /// 所以「能滚到的最右边」正好是「行尾文字所在处」，不会差一截。
    /// </summary>
    private readonly Dictionary<long, float> _lineWidths = [];

    /// <summary>
    /// 等宽字体的单字符宽度（pt）。**必须在第一次绘制时实测**：行号栏宽度、点击→字符下标换算、
    /// 自绘光标位置、超长行窗口化全都依赖它，用猜的初值会一路偏下去。
    /// （曾经只在调试 HUD 里测，而 HUD 默认关闭 ⇒ 从没测过，点击定位就总是偏几个字符。）
    /// </summary>
    private float _charWidth = 8f;
    private bool _charWidthMeasured;

    /// <summary>
    /// CJK/全角字符的实测宽度。**不等于** 2 × <see cref="_charWidth"/> —— 等宽字体的中文
    /// 未必正好是拉丁的两倍，按「占两列」推算会让中文行的光标位置差出一两个字符
    /// （用户实测「插入位置错了一个字符」）。所以两者各量一次。
    /// </summary>
    private float _wideCharWidth = 16f;

    /// <summary>最近一次绘制用的画布 —— 仅供缓存未命中时补测字宽（量宽不依赖画布状态）。</summary>
    private ICanvas? _measureCanvas;

    /// <summary>诊断：画布实际类型 + 用它量一个字符的原始结果（两者都是 0 说明这个 canvas 不支持测量）。</summary>
    public string MeasureProbe { get; private set; } = "?";

    /// <summary>诊断：平台路径实际用的密度（用于和「屏幕物理宽 ÷ 控件 dp 宽」比对）。</summary>
    public float ProbeDensity { get; private set; }

    /// <summary>诊断：打包字体是否真的加载到了（拿不到就是静默回落成默认字体）。</summary>
    public string ProbeTypeface { get; private set; } = "-";

#if DEBUG
    /// <summary>
    /// 诊断：上一次点击的**完整换算链** —— 点击屏幕坐标 → 行内横坐标 xInLine → 平台引擎算出的字符下标
    /// → 再反算该下标处的横坐标。
    ///
    /// 分辨偏差出在哪一段：`x…→i…` 若就不对，是坐标换算（density/gutter/_scrollX）错了；
    /// `i…→x…` 若与前者对不上，是我们建的 StaticLayout 与画布自绘的实际字体不一致。
    /// 两种原因修法完全不同 —— 没有这串数字只能靠看截图猜，而目测误差比偏差本身还大。
    /// </summary>
    public string TapProbe { get; private set; } = "-";
#endif

    /// <summary>字宽是**首次绘制时**才测出来的，而页面打开时状态栏就已经刷过一次 —— 不补一次通知，状态栏会永远停在「测量前的值」。</summary>
    private bool _measuredNotified;

    /// <summary>
    /// 最近一次绘制用的画布尺寸。滚动条的几何必须**与绘制同源** ——
    /// <see cref="VisualElement.Height"/> 在绘制之外读到的值与绘制时用的不一定相同（时机差），
    /// 两个尺寸各算一次就会「看到的滑块」和「点得中的滑块」错位，甚至画到屏幕外。
    /// </summary>
    private float _drawW, _drawH;

    // ── 事件（交给页面接）──

    /// <summary>
    /// 单击某一行（1-based 行号 + **行内横坐标**，pt，相对正文左边缘、已含横向滚动）。
    /// 带上横坐标是为了能把输入光标定位到**点到的那一格**，而不是一律落在行尾。
    /// </summary>
    public event Action<long, float>? LineTapped;

    /// <summary>长按某一行（1-based）——只读模式下弹出复制/选择菜单的入口。</summary>
    public event Action<long>? LineLongPressed;

    /// <summary>选择区间变化（1-based，闭区间）。</summary>
    public event Action<long, long>? SelectionChanged;

    /// <summary>滚动/内容变化（页面据此更新状态栏）。</summary>
    public event Action? ViewChanged;

    /// <summary>
    /// 双指捏合请求的字号（pt）。手势由触摸事件里的多点信息驱动 ——
    /// <c>GraphicsView</c> 已经把平台的触摸转发过来了，再叠 <c>PinchGestureRecognizer</c>
    /// 等于同一手势被两套代码处理（同 Start/Drag/End 那条注释的道理）。
    /// </summary>
    public event Action<float>? PinchZoomed;

    /// <summary>
    /// **开始拖动**时触发（真正移动了才算，单击不算）。
    /// 页面据此结束当前行的编辑：编辑态下浮着一个输入框，一滚动它就和自绘的行对不上，
    /// 而「滑动」本身就是「我要浏览，不是在打字」——先收尾再滚，比一边编辑一边滚可靠得多。
    /// </summary>
    public event Action? ScrollingStarted;

    /// <summary>
    /// **捏合结束**（手指离开）时触发一次。
    ///
    /// 存在的理由：缩放期间字号每变一档都要重排，而「落盘 + 提示」这类**一次性收尾**
    /// 不该跟着每个触摸事件做（Android 上可达 120~240Hz）。页面据此把
    /// <c>MauiEditorStore.SetFontSize</c>（同步写文件）与 Toast 合并到手势结束再做一次。
    /// </summary>
    public event Action? PinchEnded;

    public CodeCanvasView()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;

        StartInteraction += OnStart;
        DragInteraction += OnDrag;
        EndInteraction += OnEnd;
        // 手势被系统取消（来电、切走 App、父容器截走触摸…）—— 必须把**本手势的每一个状态**都清掉：
        // 漏一个 `_dragHandle`，之后任何一次拖动都会继续去挪那个手柄；漏 `_selecting`，
        // 则下一次拖动变成「扩选」而不是滚动。这类标志「粘住」的症状是「莫名其妙开始选东西」，
        // 而且只在被打断过之后才复现，最难查。
        CancelInteraction += (_, _) =>
        {
            _dragging = false;
            _dragBar = Bar.None;
            _dragHandle = 0;
            _selecting = false;
            _bubbleGrab = false;
            _longPressTimer?.Stop();

            // 捏合被系统打断（来电、切走 App、父容器截走触摸）时**必须当成一次正常结束**：
            // 目标字号在 PinchZoomed 里已经落进 EditorTypography 了，而「落盘 + 提示」那类一次性
            // 收尾只在 PinchEnded 做 —— 只清 _pinchStartDist 的话，屏幕上字号明明变了、
            // 下次打开又变回去（用户视角就是「改了没保存」），而且连个提示都没有。
            bool wasPinching = _pinchStartDist > 0;
            _pinchStartDist = 0;
            if (wasPinching) PinchEnded?.Invoke();
        };
    }

    // ── 外部设置 ──

    public ITextSource? Document => _doc;

    /// <summary>当前显示的文档（null = 空）。</summary>
    public void SetDocument(ITextSource? doc, string filePath, bool isDark, bool editable)
    {
        _doc = doc;
        _filePath = filePath;
        _isDark = isDark;
        _editable = editable;
        _syntax = doc == null ? null : Syntax.ForFile(filePath);
        ClearLineCache();
        _lineWidths.Clear();
        DisposeEditingRuns();
        _firstLine = 0;
        _scrollX = 0;
        _scrollFontSize = EditorTypography.FontSize;   // 与 _scrollX 成对（见 ResetTypography）
        _velocityX = _velocityY = 0;
        _caretLine = -1;
        _selALine = _selBLine = -1;
        _selACol = _selBCol = 0;
        _selecting = false;
        Invalidate();
    }

    /// <summary>
    /// 切亮/暗配色。
    ///
    /// ⚠ 必须**连行缓存一起清**：缓存里的每个段都烘进了当时的配色（<c>BuildLineRuns</c> 按
    /// <c>_isDark</c> 取色），只 <c>Invalidate()</c> 的话正文会保留旧主题的颜色直到缓存被淘汰。
    /// 由 `EditorPage.OnAppThemeChanged` 在系统主题切换时调用（那是**唯一**的另一条路 ——
    /// <see cref="SetDocument"/> 是打开文件时那一条）。
    /// </summary>
    public void SetDark(bool isDark)
    {
        if (_isDark == isDark) return;
        _isDark = isDark;
        ClearLineCache();
        ClearTextCache();   // 行号的颜色也烘进了 span，换主题必须重建
        DisposeEditingRuns();
        Invalidate();
    }

    public void ResetTypography()
    {
        // ⚠ **横向滚动要按比例接过去，不能清零**。
        // 清零的表现就是「明明滚到了行中间，一缩放就被拽回最左边」—— 因为横向偏移是**像素**，
        // 字号一变它的含义就变了；但「清零」不是解法，「换算」才是。
        //
        // 推进量与字号成正比（平台的逐字形取整只是零头），所以「原来停在左边第几列」在新字号下
        // 的偏移 ≈ 旧偏移 × (新字号 / 旧字号)。纵向（_firstLine 是行号，与字号无关）本来就不动。
        float oldSize = _scrollFontSize > 0.5f ? _scrollFontSize : EditorTypography.FontSize;
        _scrollX = Math.Max(0f, _scrollX * (EditorTypography.FontSize / oldSize));
        // ⚠ **必须把基准改成新字号**。捏合期间每接受一档就调一次本函数 —— 不更新的话
        // 每次都在拿「文档打开时那个字号」当基准，比例是**连乘**的（∏(sᵢ/s₀) 而不是 s/s₀），
        // 缩放几下就被 ClampScroll 甩到行尾，而且缩回去也回不来。
        _scrollFontSize = EditorTypography.FontSize;

        _charWidthMeasured = false;   // 推进量要按新字号重量（MeasureAdvances）
        _lineWidths.Clear();          // 行宽也随字号变 —— 不清则 MaxScrollX 还是旧值（长行尾巴滚不到）
        ClearLineCache();             // ⚠ 行缓存里挂着**按字号编好的平台排版**（见 LineRuns.NativeLayout）
        ClampScroll();
        Invalidate();
    }

    /// <summary>当前光标行（1-based；-1 = 无）。</summary>
    public long CaretLine => _caretLine < 0 ? -1 : _caretLine + 1;

    /// <summary>视口能放下多少行。</summary>
    public long VisibleLines => Math.Max(1, (long)((Height > 0 ? Height : 400) / EditorTypography.LineHeight));

    public long FirstVisibleLine => (long)_firstLine;

    // ── 滚动与定位 ──

    /// <summary>把某行滚进视口（1-based）。</summary>
    public void ScrollToLine(long oneBased, bool center = false)
    {
        if (_doc == null) return;
        _firstLine = TextEditorMath.EnsureVisible((long)_firstLine, oneBased - 1,
            VisibleLines, _doc.LineCount, center);
        ClampScroll();
        Invalidate();
    }

    public void SetCaretLine(long oneBased, int col = -1)
    {
        _caretLine = oneBased - 1;
        // col < 0 ⇒ 只换行、列沿用（上下键跨行就是这么用的：短行上夹住、不改写目标列）
        _caretCol = col >= 0 ? Math.Max(0, col) : Math.Max(0, _caretCol);
        Invalidate();
    }

    /// <summary>浏览光标（只读态那根不闪的竖线）所在列 —— 字符下标。</summary>
    public int CaretCol => _caretCol;

    /// <summary>
    /// 视口大小变了 —— **这是「软键盘挡住光标」的唯一正确触发点**。
    ///
    /// 点一行靠下的位置时键盘还没弹，视口是满屏 ⇒ 那一行的判定是「露得全」，一个字都不滚；
    /// 等键盘把画布压掉半屏，它就被盖住了。此前靠 `EnsureEditorVisibleAsync` 里「点完等 260ms
    /// 再滚一次」猜时机 —— 键盘动画在 200~400ms 之间，这个数是猜的：猜早了算的还是旧视口
    /// （等于没做），猜晚了用户已经看着自己被挡住。改成由**真实的高度变化**驱动，
    /// 与键盘动画耗时无关。
    ///
    /// 只对**变矮**做回收（变高 = 收起键盘/旋转到横屏，光标只会更露，不该无端跳一下），
    /// 但纵向边界两种情况**都要收口**：视口变高时 `count - VisibleLines` 变小，
    /// 旧的 `_firstLine` 可能已经越界。
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (Math.Abs(height - _lastHeight) < 0.5) return;   // 宽度变化不改行数，横向另有收口
        bool shrank = height < _lastHeight;
        _lastHeight = height;

        // ⚠ **必须排到下一次调度再算**：本回调里 `Height` 还没有落到控件上（新值只在参数里），
        // 而 ScrollToLine / ClampScroll 都读 `Height` —— 直接调等于拿旧视口算，白跑一趟。
        Dispatcher.Dispatch(() =>
        {
            ClampScroll();
            if (shrank && _caretLine >= 0) ScrollToLine(_caretLine + 1);
            EnsureCaretVisible();   // 横向：编辑长行时同理（非编辑态自身立即返回）
            Invalidate();
        });
    }

    private void ClampScroll()
    {
        long count = _doc?.LineCount ?? 0;
        float maxFirst = Math.Max(0, count - VisibleLines);
        _firstLine = Math.Clamp(_firstLine, 0, maxFirst);

        // 横向也要有上限：此前只挡了小于 0，往左可以无限滚 —— 一直拖下去所有文字都会被
        // 推出画布外，屏幕上只剩行号栏（实测「横向滚一下就啥都看不见了」）。
        _scrollX = Math.Clamp(_scrollX, 0, ComputeMaxScrollX());
    }

    /// <summary>
    /// 横向可滚的最大偏移：按**可见行**里最长的那条算。
    /// 遍历全文件找最长行太贵（100MB 是几百万行），而横向滚动本来就只关心眼前这一屏。
    /// </summary>
    private float ComputeMaxScrollX()
    {
        if (_doc == null) return 0;
        float viewW = Math.Max(40f, (float)Width - GutterWidth() - EditorTypography.TextLeftPad);

        long first = Math.Max(0, (long)_firstLine - 1);
        long last = Math.Min(first + VisibleLines + 2, _doc.LineCount);
        float maxWidth = 0;
        for (long i = first; i < last; i++)
        {
            var line = _doc.GetLine(i);
            if (line == null) continue;
            // 实测宽度，**与光标/点击同一把尺子**（MeasurePrefixWidth 逐字形累加实测推进量）。
            // 按「视觉列 × 半列宽」估算会让中文行严重偏短，横向就滚不到真正的行尾。
            //
            // 走记忆化：本函数**每个触摸事件都要跑一遍**（ClampScroll ← OnDrag），
            // 而累加要逐码点扫描 —— 不缓存就是手指一动按 240Hz 重扫整屏字符。
            float w = LineWidth(i, line);
            if (w > maxWidth) maxWidth = w;
        }

        // ⚠ **气泡的右缘也要算进来**。气泡左缘 = 锚点、只向右展开、**不夹进屏幕**（用户定的），
        // 于是它常常比代码行还宽：不算进来的话，横向最多只能滚到「行尾 + 24」，
        // 而气泡右上角那个 ✕ 还在更右边 ⇒ **滚不到、也就点不着**（那等于把关闭手段弄丢了）。
        // 用的是上一帧绘制时记下的**内容坐标**（不含横向滚动），所以不会与 _scrollX 互相追。
        maxWidth = Math.Max(maxWidth, _bubbleRightCodeX);
        return Math.Max(0, maxWidth - viewW + 24);
    }

    /// <summary>某行的显示总宽（按行号记忆化）。</summary>
    private float LineWidth(long index, string line)
    {
        if (_lineWidths.TryGetValue(index, out var w)) return w;
        w = MeasurePrefixWidth(line, line.Length);
        // 纯记忆化：无界增长不如整体清空（重建一次比维护 LRU 便宜得多）
        if (_lineWidths.Count >= MaxCachedLines) _lineWidths.Clear();
        _lineWidths[index] = w;
        return w;
    }

    // ── 触摸 ──

    private float _lastX, _lastY;
    private bool _dragging, _moved;

    /// <summary>长按判定用的单次定时器（见 <see cref="OnStart"/>：长按必须在手指还按着时判定）。</summary>
    private IDispatcherTimer? _longPressTimer;

    /// <summary>本手势抓住的是哪个选区手柄（0 = 没有，1 = 起点，2 = 终点）。</summary>
    private int _dragHandle;

    /// <summary>
    /// 本手势被诊断气泡吞掉了（点在 ✕ 或气泡正文上）。**必须记下来**：气泡浮在代码上层，
    /// 手势穿过去的话，点一下气泡就会把光标挪到它底下那一行、甚至开始滚动。
    /// 也与「点正文不关气泡」配套 —— 只吞不关。
    /// </summary>
    private bool _bubbleGrab;
    private float _pinchStartDist;
    private float _pinchStartFontSize;
    private long _downTicks;
    private float _downX, _downY;

    private void OnStart(object? sender, TouchEventArgs e)
    {
        MarkBarActivity();   // 摸屏幕就续命：滚动条 5 秒没交互才淡出
        if (e.Touches.Length == 0) return;
        var p = e.Touches[0];
        _lastX = _downX = p.X;
        _lastY = _downY = p.Y;
        _downTicks = Environment.TickCount64;
        _dragging = true;
        _moved = false;
        _samples.Clear();
        _samples.Enqueue((_downTicks, p.Y, p.X));
        StopFling();
        _drawMsPeak = 0;   // 新手势 ⇒ 重新开始记最差帧
        _bubbleGrab = false;

        // 按在**小目标**上（展开态的 ✕ = 收起这一条 / 收起态的小圆点 = 展开这一条）。
        // **先于滚动条与手柄判**：它的热区小、必须点得中，而滚动条那条热区有 20pt 宽的外扩，
        // 排在后面就会被它抢走。
        if (HitDiagToggle(p.X, p.Y) is { } diagHit)
        {
            _dragging = false;
            _bubbleGrab = true;      // OnEnd 不许再把它当成单击
            SetDiagExpanded(diagHit.Spot, !diagHit.Expanded);
            Invalidate();
            return;
        }

        // 按在滚动条上 → 这一手势归滚动条，不当成内容拖拽（也就不会触发惯性/长按选择）。
        // 按在滑块上保持抓取偏移（不跳），按在轨道上视作「跳到此处」。
        { var (bw, bh) = BarCanvas(); _dragBar = HitBar(p.X, p.Y, bw, bh); }
        if (_dragBar != Bar.None)
        {
            // ⚠ 让角参数必须与 DrawScrollbars **同源**，否则按下的落点与画出来的条对不上
            // （拖起来会"差一截"，越靠角越明显）。
            var (bv, bh2) = BarsShown();
            var (start, len) = _dragBar == Bar.Vertical
                ? VerticalThumb((float)Height, bh2 ? BarCorner() : 0f)
                : HorizontalThumb((float)Width, bv ? BarCorner() : 0f);
            float pos = _dragBar == Bar.Vertical ? p.Y : p.X;
            _barGrab = pos >= start && pos <= start + len ? pos - start : len / 2f;
            _dragging = false;
            Invalidate();   // 立刻画粗版，让「按住了」有即时反馈
        }

        // **长按定时器**：长按必须在「手指还按着」的时候就判定 —— 只在抬手时按耗时判断的话，
        // 就永远做不出「长按选中一个词，再拖着扩选」这个标准手势（抬手=手势结束，没得拖了）。
        // 500ms 内一动就取消（那是滑动，不是长按）。
        // 按在选区手柄上 → 这一手势归手柄（调选区端点），不滚动、不长按
        if (HasSelection)
        {
            _dragHandle = HitHandle(p.X, p.Y);
            if (_dragHandle != 0)
            {
                _dragging = false;      // 不是内容拖拽：不滚、不惯性
                return;
            }
        }

        // 按在气泡**正文**上 → 什么都不做（**只有 ✕ 收起**：正文正是用户要读的东西，长报错尤其容易误触）。
        // 但手势要**吞掉** —— 不吞就会穿过去把光标挪到气泡底下那一行、或者开始滚动。
        if (HitBubbleBody(p.X, p.Y))
        {
            _dragging = false;
            _bubbleGrab = true;
            return;
        }

        _longPressTimer ??= Dispatcher.CreateTimer();
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(500);
        _longPressTimer.IsRepeating = false;
        _longPressTimer.Tick -= OnLongPressTick;
        _longPressTimer.Tick += OnLongPressTick;
        _longPressTimer.Stop();
        _longPressTimer.Start();
    }

    /// <summary>长按触发：选中落点处的词，并把后续拖动切到「扩选」而不是「滚动」。</summary>
    private void OnLongPressTick(object? sender, EventArgs e)
    {
        _longPressTimer?.Stop();
        if (_moved || !_dragging || _dragBar != Bar.None || _doc == null) return;   // 已经滑走/松手/在拖滚动条
        if (_pinchStartDist > 0) return;                                           // 捏合中不算长按

        _selecting = true;                       // 后续 DragInteraction 走扩选
        StopFling();

        long line = (long)(_firstLine + (_downY - EditorTypography.VerticalPad) / EditorTypography.LineHeight);
        line = Math.Clamp(line, 0, Math.Max(0, _doc.LineCount - 1));

        // **先通知页面收尾**（它要 CommitEditingLine），再选词。
        // 不通知的话：正在编辑的那一行只活在浮动输入框/EditingText 里，而 `GetSelectedText`
        // 读的是 `_doc.GetLine()` —— 长按后复制会**复制到编辑前的旧文本**，屏幕上却是新的。
        LineLongPressed?.Invoke(line + 1);
        SelectWordAt(line, _downX - GutterWidth() - EditorTypography.TextLeftPad + _scrollX);
    }

    /// <summary>
    /// 限流重绘：触摸事件在 Android 上可达 120~240Hz，而**每帧要重排可见的几十行文本**
    /// （每行一次原生 StaticLayout）。逐事件重绘等于把同样的活干两到四遍，滚动就会发涩。
    /// 压到 ~60fps 后，位置照样每次都更新，只是合并到下一帧一起画。
    /// </summary>
    /// <summary>
    /// 视口变了：按帧率节流地**重绘 + 通知页面**。
    ///
    /// ⚠ 通知不能只在手势结束时发 —— 选区操作条要跟着选区走（滚出视口就收起来），
    /// 只发一次的话条子会停在原处「乱飘」，而那个「滚出视口就隐藏」的判据永远不会被求值。
    /// 触摸事件可达 240Hz，所以和重绘共用同一道 16ms 闸门。
    /// </summary>
    private void ThrottledInvalidate()
    {
        long now = Environment.TickCount64;
        if (now - _lastPaintTicks < 16) return;
        _lastPaintTicks = now;
        Invalidate();
        ViewChanged?.Invoke();
    }

    private long _lastPaintTicks;

    private void OnDrag(object? sender, TouchEventArgs e)
    {
        MarkBarActivity();   // 摸屏幕就续命：滚动条 5 秒没交互才淡出
        if (e.Touches.Length == 0) return;

        // 拖滚动条：整条路都归它（不进内容拖拽、不攒惯性速度）
        if (_dragBar != Bar.None)
        {
            if (e.Touches.Length >= 2) return;   // 双指只在内容编辑区起缩放作用
            var tp = e.Touches[0];
            if (_dragBar == Bar.Vertical) DragBarVertical(tp.Y);
            else DragBarHorizontal(tp.X);
            ClampScroll();
            ThrottledInvalidate();
            return;
        }

        // 拖手柄：只调选区端点，不滚动、不触发长按
        if (_dragHandle != 0)
        {
            var hp = e.Touches[0];
            long hl = (long)(_firstLine + (hp.Y - EditorTypography.VerticalPad) / EditorTypography.LineHeight);
            if (_doc != null) hl = Math.Clamp(hl, 0, Math.Max(0, _doc.LineCount - 1));
            MoveSelectionHandle(_dragHandle, hl,
                hp.X - GutterWidth() - EditorTypography.TextLeftPad + _scrollX);
            return;
        }

        if (!_dragging) return;

        // 双指 = 缩放字号（不滚动）
        if (e.Touches.Length >= 2)
        {
            float d = Distance(e.Touches[0], e.Touches[1]);
            if (d <= 1) return;
            if (_pinchStartDist <= 0)
            {
                _pinchStartDist = d;
                _pinchStartFontSize = EditorTypography.FontSize;
                return;
            }
            PinchZoomed?.Invoke(_pinchStartFontSize * (d / _pinchStartDist));
            return;
        }

        var p = e.Touches[0];
        float dx = p.X - _lastX, dy = p.Y - _lastY;
        _lastX = p.X;
        _lastY = p.Y;

        if (!_moved && (Math.Abs(p.X - _downX) > 8 || Math.Abs(p.Y - _downY) > 8))
        {
            _moved = true;
            // 长按已经成词的拖动 = **扩选**，不是滚动 —— 选区固定住，只把活动端跟到手指。
            if (_selecting)
            {
                long dl = (long)(_firstLine + (p.Y - EditorTypography.VerticalPad) / EditorTypography.LineHeight);
                if (_doc != null) dl = Math.Clamp(dl, 0, Math.Max(0, _doc.LineCount - 1));
                ExtendSelectionTo(dl, p.X - GutterWidth() - EditorTypography.TextLeftPad + _scrollX);
                return;
            }
            ScrollingStarted?.Invoke();   // 开始拖动 ⇒ 让页面先结束编辑（见事件注释）
        }

        // 扩选进行中：拖动只改选区端点，视口不动（否则一边选一边滚，选中的内容跟着跑）
        if (_selecting)
        {
            long el = (long)(_firstLine + (p.Y - EditorTypography.VerticalPad) / EditorTypography.LineHeight);
            if (_doc != null) el = Math.Clamp(el, 0, Math.Max(0, _doc.LineCount - 1));
            ExtendSelectionTo(el, p.X - GutterWidth() - EditorTypography.TextLeftPad + _scrollX);
            return;
        }

        // 采样最近一段的触摸点（见 StartFling：松手速度只能从这里算，
        // 用「总位移」估出来的既不是速度也不是任何有意义的量）
        long now = Environment.TickCount64;
        _samples.Enqueue((now, p.Y, p.X));
        while (_samples.Count > 1 && (now - _samples.Peek().Ticks > VelocityWindowMs || _samples.Count > 8))
            _samples.Dequeue();

        // 拖动方向与内容移动方向一致（手指下拖 = 看上面的内容）
        _firstLine -= dy / EditorTypography.LineHeight;
        _scrollX -= dx;
        ClampScroll();
        ThrottledInvalidate();
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private void OnEnd(object? sender, TouchEventArgs e)
    {
        MarkBarActivity();   // 摸屏幕就续命：滚动条 5 秒没交互才淡出

        // 这一手势归诊断气泡（点在 ✕ 或气泡正文上）—— 不当成单击、不进惯性。
        // ⚠ 少了这道闸，OnStart 里那句 `return` 只挡住了一半：OnEnd 的「单击定位」分支
        // 不看 `_dragging`，抬手照样会把光标挪到气泡底下那一行去。
        if (_bubbleGrab)
        {
            _bubbleGrab = false;
            _dragging = false;
            _longPressTimer?.Stop();
            return;
        }

        bool wasPinching = _pinchStartDist > 0;
        _pinchStartDist = 0;
        _dragging = false;
        _longPressTimer?.Stop();          // 抬手了，长按不再可能
        if (_dragHandle != 0)
        {
            // 手柄拖完了：选区留着，交给页面刷新操作条位置
            _dragHandle = 0;
            ViewChanged?.Invoke();
            Invalidate();
            return;
        }
        if (_selecting)
        {
            // 扩选手势结束：**选区留着**（交给页面弹操作条），只是不再是「拖动中」。
            // 不进下面的 tap/惯性分支 —— 这一手势从头到尾都是选词，不是点击也不是滑动。
            _selecting = false;
            _moved = true;
            ViewChanged?.Invoke();
            Invalidate();
            return;
        }
        if (wasPinching)
        {
            _moved = true;          // 捏合结束 ⇒ 不是 tap，也不进长按选择
            PinchEnded?.Invoke();   // 一次性的收尾（落盘 / 提示）挪到这里做
            return;
        }

        if (_dragBar != Bar.None)
        {
            _dragBar = Bar.None;
            _moved = true;          // 拖过滚动条 ⇒ 不是 tap，也不进长按选择
            ViewChanged?.Invoke();  // 状态栏跟着刷新（拖滚动条同样是「视口变了」）
            Invalidate();           // 回到细版
            return;
        }

        long elapsed = Environment.TickCount64 - _downTicks;

        long hitLine = (long)(_firstLine + (_lastY - EditorTypography.VerticalPad) / EditorTypography.LineHeight);
        if (_doc != null) hitLine = Math.Clamp(hitLine, 0, Math.Max(0, _doc.LineCount - 1));

        // 长按（未移动且超过 500ms）：**选词**。真正的长按在计时器里就已经处理过了
        // （见 OnStart 的 _longPressTimer）—— 这里兜住「按满 500ms 但计时器还没跑完就抬手」
        // 这一瞬间的边界（判定只差几毫秒，不该表现成「按了没反应」）。
        if (!_moved && elapsed >= 400 && !_selecting)
        {
            _caretLine = hitLine;
            SelectWordAt(hitLine, _lastX - GutterWidth() - EditorTypography.TextLeftPad + _scrollX);
            return;
        }

        if (!_moved && elapsed < 400)
        {
            // 单击：定位行（并收起选区 —— 与桌面编辑器一致：点一下就是「不要选了」）
            long line = hitLine;
            _caretLine = line;
            if (HasSelection) ClearSelection();

            float xInLine = _lastX - GutterWidth() - EditorTypography.TextLeftPad + _scrollX;

            // 只读态那根浏览竖线也要跟着点击走 —— 它画在 `_caretCol` 上，
            // 不更新的话「点哪儿」与「竖线在哪」就是两回事（点完竖线还在行首）。
            _caretCol = CharIndexAtX(_doc?.GetLine(line) ?? "", xInLine);
#if DEBUG
            var tapLine = _doc?.GetLine(line) ?? "";
            if (tapLine.Length > 0)
            {
                int ci = CharIndexAtX(tapLine, xInLine);
                // 真实 scale = 屏幕物理宽 ÷ 控件 dp 宽；与平台路径用的密度不同 ⇒ 字号喂错了
                float realScale = Width > 0
                    ? (float)(Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Width / Width) : 0;
                // G = MAUI Graphics 的 GetStringSize（用 CanvasFont 这个名字解析）；
                // W 来自平台 StaticLayout（用 CreateFromAsset 拿到的 Typeface）。
                // 两者不等 ⇒ Graphics 侧解析不到资产名、也在回落，渲染与测量仍不同源。
                float g = _measureCanvas == null ? -1f
                    : (float)_measureCanvas.GetStringSize(tapLine, EditorTypography.CanvasFont,
                        EditorTypography.FontSize).Width;
                TapProbe = $"x{xInLine:F0}→i{ci} W{MeasurePrefixWidth(tapLine, tapLine.Length):F0}"
                         + $"/L{tapLine.Length} G{g:F0} tf:{ProbeTypeface}";
            }
#endif
            LineTapped?.Invoke(line + 1, xInLine);
            ViewChanged?.Invoke();
            Invalidate();
            return;
        }

        StartFling(e);
    }

    /// <summary>算松手速度用的时间窗：只取最近这段时间内的触摸点。</summary>
    private const long VelocityWindowMs = 100;

    /// <summary>惯性**起步**阈值（pt/s）——低于它就不触发惯性，内容停在手指松开的位置。</summary>
    private const float MinFlingVelocity = 40f;

    /// <summary>
    /// 惯性**收手**阈值（pt/s）——低于它就停止滑行。
    ///
    /// 与起步阈值分开是有意的：调「滑多远」的手感时，**起步门槛不该跟着动**
    /// （把起步门槛一起抬高 = 轻扫一下干脆不滑了，那是另一码事）。
    /// 60 的意思：速度掉到看不太出来时就收手，别留一段「几乎不动却还在飘」的尾巴。
    /// </summary>
    private const float FlingStopVelocity = 60f;

    /// <summary>
    /// 松手速度 → 滑行初速的**放大倍数**（v0.96.137 新增）。
    ///
    /// 用户要的是「**松手后第一秒滑得更远，但不是持续时间变长**」——这两件事对应的旋钮不同：
    /// 调大 <see cref="FlingFriction"/> 会同时把距离和**时长**一起拉长（尾巴慢慢飘，正是他不想要的）；
    /// 而放大初速只按比例放大**第一秒**的位移，时长基本不变
    /// （时长 = ln(v_stop / v0) / ln(friction)，v0 翻倍只是把对数里的一项挪一点）。
    /// 实测量级：轻扫一下第一秒从约 23 行变成约 60 行（1.7 倍时是 39 行，用户实测「还不够」，
    /// 于是加到 2.6），而滑行时长 2.37s → 2.6s（几乎不变）。
    ///
    /// 放大**放在起步阈值判断之后** —— 否则 30pt/s 的轻扫会被放大成 51 而越过门槛，
    /// 等于顺手把起步门槛降低了，那是另一个改动。
    /// </summary>
    private const float FlingLaunchGain = 2.6f;

    /// <summary>
    /// 每帧速度保留比例 —— **滑多远由它决定**：总位移 = 初速 × dt / 行高 ÷ (1 − friction)。
    /// 0.98 ⇒ 约 50 倍单帧位移（0.95 ⇒ 20 倍，0.97 ⇒ 33 倍）。
    /// 这个数直接决定手感（实测调过两轮），改动前先按上式估一下 —— **它是个除法**，
    /// 从 0.98 挪到 0.99 就是翻倍，别看着「只差 0.01」就随手调。
    ///
    /// ⚠ **它同时决定「滑多久」**（时长 = ln(v_stop/v0)/ln(friction)），而用户要的是
    /// 「第一秒滑得更远、但别拖更久」⇒ 那件事由 <see cref="FlingLaunchGain"/> 负责，**别调这里**。
    /// v0.96.137 一度把它改成 0.99，实测就是「尾巴变长」而不是「起步更快」，已改回。
    /// </summary>
    private const float FlingFriction = 0.98f;

    private readonly Queue<(long Ticks, float Y, float X)> _samples = new();

    private void StartFling(TouchEventArgs e)
    {
        // 松手速度 = **最近 100ms 内的位移 ÷ 时间**。
        // 之前是拿「按下到松手的总位移 × 4」估的：轻轻一甩位移很小 ⇒ 估出的速度接近 0 ⇒ 几乎不滑；
        // 按住拖很远再松手反而滑过头 —— 手感「要么不动、要么窜出去」就是这么来的。
        _velocityY = _velocityX = 0;
        if (_samples.Count >= 2)
        {
            var first = _samples.Peek();
            var last = _samples.Last();
            float sec = (last.Ticks - first.Ticks) / 1000f;
            if (sec > 0.01f)
            {
                _velocityY = -(last.Y - first.Y) / sec;   // 手指上滑 ⇒ 内容下滚（_firstLine 增大）
                _velocityX = -(last.X - first.X) / sec;
            }
        }
        _samples.Clear();

        // 慢速松手（不触发惯性）—— 行号数字由 Draw 里的「本帧还在动就再排一帧」负责补回来
        // （见那边的注释：滚动的最后一帧恰好还在动时，没有后续帧是行号永不回来的根因）。
        if (Math.Abs(_velocityY) < MinFlingVelocity && Math.Abs(_velocityX) < MinFlingVelocity)
        {
            StopFling();
            return;
        }

        // 过了门槛才放大（见 FlingLaunchGain 的注释：放在门槛之前等于顺手把门槛降低了）
        _velocityY *= FlingLaunchGain;
        _velocityX *= FlingLaunchGain;

        _fling ??= Dispatcher.CreateTimer();
        _fling.Interval = TimeSpan.FromMilliseconds(16);
        _fling.Tick -= OnFlingTick;
        _fling.Tick += OnFlingTick;
        _fling.Start();
    }

    private void OnFlingTick(object? sender, EventArgs e)
    {
        const float dt = 0.016f;        // 16ms 一帧
        _velocityY *= FlingFriction;
        _velocityX *= FlingFriction;
        _firstLine += _velocityY * dt / EditorTypography.LineHeight;   // 位移(pt) ÷ 行高 = 行数
        _scrollX += _velocityX * dt;
        ClampScroll();
        ThrottledInvalidate();

        if (Math.Abs(_velocityY) < FlingStopVelocity && Math.Abs(_velocityX) < FlingStopVelocity) StopFling();
    }

    private void StopFling()
    {
        _fling?.Stop();
        _velocityX = _velocityY = 0;
    }

    // ── 选区（**字符级**：两个端点各是「行 + 行内码元下标」）──
    //
    // 换掉原先的「行级选区」（`_selAnchor/_selEnd` 是行号、底色整行铺）。行级那套是给
    // 「长按 → 弹菜单 → 选行范围」用的，粒度太粗，而且那套菜单是**平台的**弹窗 ——
    // 平台上任何「它自己算坐标」的东西都会和我们的自绘错开一点，所以选择与复制粘贴
    // 一并改成自己做（见 EditorPage 的选区操作条）。

    private long _selALine = -1;
    private int _selACol;
    private long _selBLine = -1;
    private int _selBCol;

    /// <summary>有没有**非空**选区（两端点重合 = 没有选中内容）。</summary>
    public bool HasSelection => _selALine >= 0 && _selBLine >= 0
        && (_selALine != _selBLine || _selACol != _selBCol);

    /// <summary>选区上端所在行（1-based）—— 页面据此把操作条摆到选区上方。</summary>
    public long SelectionTopLine => _selALine < 0 ? -1 : Math.Min(_selALine, _selBLine) + 1;

    /// <summary>选区所在的**屏幕 y**（选区的上端行顶）—— 操作条定位用。</summary>
    public float SelectionTopY => _selALine < 0
        ? -1
        : LineY(Math.Min(_selALine, _selBLine), EditorTypography.LineHeight);

    /// <summary>
    /// **选中落点处的词**（长按触发）—— 词 = 连续的同类字符（字母/数字/下划线算一类，
    /// CJK 算一类，其余各自成词）。落在空白上则选整行。
    /// 之后拖动改的是 <c>_selB*</c>（另一端固定不动），与桌面编辑器的习惯一致。
    /// </summary>
    public void SelectWordAt(long line, float xInLine)
    {
        var text = _doc?.GetLine(line);
        if (text == null) return;

        int idx = Math.Clamp(CharIndexAtX(text, xInLine), 0, text.Length);
        int a, b;
        if (idx >= text.Length || IsWordBreak(text[idx]))
        {
            a = 0; b = text.Length;                    // 空白/行尾 → 整行（空行则无选区）
        }
        else
        {
            int cls = WordClass(text[idx]);
            a = idx;
            while (a > 0 && WordClass(text[a - 1]) == cls) a--;
            b = idx;
            while (b < text.Length && WordClass(text[b]) == cls) b++;
        }

        _selALine = line; _selACol = a;
        _selBLine = line; _selBCol = b;
        _caretLine = line;
        RaiseSelectionChanged();
        Invalidate();
    }

    /// <summary>拖动选区的活动端（长按之后拖动走这里）。</summary>
    public void ExtendSelectionTo(long line, float xInLine)
    {
        if (_selALine < 0) return;
        var text = _doc?.GetLine(line);
        if (text == null) return;
        _selBLine = line;
        _selBCol = Math.Clamp(CharIndexAtX(text, xInLine), 0, text.Length);
        RaiseSelectionChanged();
        Invalidate();
    }

    /// <summary>
    /// 拖动**某一个手柄**：<paramref name="which"/> 1 = 起点，2 = 终点。
    ///
    /// 手柄的意义是「手势结束之后还能调」—— 只有长按拖动的话，选区一旦定下来就只能
    /// 重新长按再来一次，够不到「把左端再往左挪一点」这种最常见的微调。
    /// </summary>
    public void MoveSelectionHandle(int which, long line, float xInLine)
    {
        if (_selALine < 0) return;
        var text = _doc?.GetLine(line);
        if (text == null) return;
        int col = Math.Clamp(CharIndexAtX(text, xInLine), 0, text.Length);
        if (which == 1) { _selALine = line; _selACol = col; }
        else { _selBLine = line; _selBCol = col; }
        RaiseSelectionChanged();
        Invalidate();
    }

    /// <summary>选区两端按「谁在前」归一化后的 (起点行/列, 终点行/列)。</summary>
    private (long LA, int CA, long LB, int CB) NormalizedSelection()
    {
        bool aFirst = _selALine < _selBLine || (_selALine == _selBLine && _selACol <= _selBCol);
        return aFirst
            ? (_selALine, _selACol, _selBLine, _selBCol)
            : (_selBLine, _selBCol, _selALine, _selACol);
    }

    // ── 给页面用的只读出口（气泡层 / 选区操作 / 输入归一化）──
    //
    // 这三样都是「页面需要、但页面**不该自己再算一遍**」的东西：宽度换算、选区排序、
    // 行→Y 换算在这里各有一把尺子，页面另算一份就会和绘制错开（本仓库「两把尺子」的老坑）。

    /// <summary>本文件用的语法定义（与高亮绘制同源）。文档为空时是 null。</summary>
    public Syntax? SyntaxForFile => _syntax;

    /// <summary>选区两端按「谁在前」归一化后的 (起点行/列, 终点行/列)，0-based 行 + 行内码元下标。</summary>
    public (long LA, int CA, long LB, int CB) SelectionRange => NormalizedSelection();

    /// <summary>
    /// 单元格（1-based 行 + 1-based 列）在**视口坐标**里的锚点：<paramref name="x"/> 是该字符格的左缘、
    /// <paramref name="y"/> 是该行的上缘。给诊断气泡的尾巴定位用。
    ///
    /// 与 <c>DrawCaret</c> / <c>DrawDiagnosticWave</c> / <c>HandlePositions</c> **同一把尺子**
    /// （GutterWidth + TextLeftPad − 横向滚动 + MeasurePrefixWidth），所以气泡箭头与那一行下方的
    /// 波浪线起点逐像素同源，不会出现「箭头指这儿、波浪线画那儿」。
    ///
    /// ⚠ 返回值**含滚动偏移** ⇒ 跟着滚动就失效，调用方要在 <c>ViewChanged</c> 里重排
    /// （照页面里选区操作条的做法）。
    /// </summary>
    public bool TryGetCellAnchor(long oneBasedLine, int oneBasedColumn, out float x, out float y)
    {
        x = y = 0;
        if (_doc == null || oneBasedLine < 1 || oneBasedLine > _doc.LineCount) return false;

        long idx = oneBasedLine - 1;
        var line = _doc.GetLine(idx) ?? "";
        // 列号越界一律夹到行长：带 #define/#include 的 C 文件，编译器报的列是**预处理之后**
        // 那一行的列，可能超出原始行长度 —— 不夹住就会把气泡甩到屏幕外。
        int col = Math.Clamp(oneBasedColumn - 1, 0, line.Length);

        x = GutterWidth() + EditorTypography.TextLeftPad - _scrollX + MeasurePrefixWidth(line, col);
        y = LineY(idx, EditorTypography.LineHeight);
        return true;
    }

    /// <summary>
    /// 两个手柄的屏幕位置（起点、终点）。**画在哪与点哪算命中共用这一个** ——
    /// 各算一次就会出现「看到的和点得中的错开」（滚动条那边踩过同样的坑）。
    /// </summary>
    private ((float X, float Y) A, (float X, float Y) B)? HandlePositions()
    {
        if (!HasSelection || _doc == null) return null;
        var (la, ca, lb, cb) = NormalizedSelection();
        float lineH = EditorTypography.LineHeight;
        float textX = GutterWidth() + EditorTypography.TextLeftPad - _scrollX;
        return ((textX + MeasurePrefixWidth(_doc.GetLine(la), ca), LineY(la, lineH) + lineH - 2f),
                (textX + MeasurePrefixWidth(_doc.GetLine(lb), cb), LineY(lb, lineH) + lineH - 2f));
    }

    /// <summary>按下的点是不是落在某个手柄上（热区比视觉半径大一截，手指才点得中）。</summary>
    private int HitHandle(float x, float y)
    {
        var h = HandlePositions();
        if (h == null) return 0;
        float r = EditorTypography.HandleTouchRadius;
        if (Dist2(x, y, h.Value.A.X, h.Value.A.Y) <= r * r) return 1;
        if (Dist2(x, y, h.Value.B.X, h.Value.B.Y) <= r * r) return 2;
        return 0;

        static float Dist2(float ax, float ay, float bx, float by)
        { float dx = ax - bx, dy = ay - by; return dx * dx + dy * dy; }
    }

    /// <summary>全选。</summary>
    public void SelectAll()
    {
        if (_doc == null || _doc.LineCount == 0) return;
        long last = _doc.LineCount - 1;
        _selALine = 0; _selACol = 0;
        _selBLine = last; _selBCol = (_doc.GetLine(last) ?? "").Length;
        RaiseSelectionChanged();
        Invalidate();
    }

    public void ClearSelection()
    {
        if (_selALine < 0) return;
        _selALine = _selBLine = -1;
        _selACol = _selBCol = 0;
        RaiseSelectionChanged();
        Invalidate();
    }

    private void RaiseSelectionChanged()
        => SelectionChanged?.Invoke(HasSelection ? Math.Min(_selALine, _selBLine) + 1 : 0,
                                    HasSelection ? Math.Max(_selALine, _selBLine) + 1 : 0);

    /// <summary>复制选区的字符数上限 —— 与「全选并复制」同一个口径，防超大只读文件上一把复制出几百 MB。</summary>
    public const int MaxCopyChars = 2_000_000;

    /// <summary>上一次 <see cref="GetSelectedText"/> 是否因为太长被截断（页面据此提示用户）。</summary>
    public bool SelectedTextTruncated { get; private set; }

    /// <summary>选中文本（**原样的行内容**，含 tab；跨行用 <c>\n</c> 连接）。无选区返回空串。</summary>
    public string GetSelectedText()
    {
        SelectedTextTruncated = false;
        if (_doc == null || !HasSelection) return "";

        // 归一化：A 在 B 之前（按行、再按列）
        long la = _selALine, lb = _selBLine;
        int ca = _selACol, cb = _selBCol;
        if (la > lb || (la == lb && ca > cb)) { (la, lb) = (lb, la); (ca, cb) = (cb, ca); }

        if (la == lb) return Slice(_doc.GetLine(la), ca, cb);

        var sb = new System.Text.StringBuilder();
        sb.Append(Slice(_doc.GetLine(la), ca, int.MaxValue));
        for (long i = la + 1; i < lb && i < _doc.LineCount; i++)
        {
            // ⚠ 超出上限就**停**（而不是继续拼空行）：只读大文件的内容不在内存里，
            // `GetLine` 对 LRU 窗口之外的行返回 null —— 继续拼下去会得到一份
            // 「行数对、内容几乎全空」的假文本，还报「已复制 N 字符」。
            if (sb.Length >= MaxCopyChars) { SelectedTextTruncated = true; break; }
            var mid = _doc.GetLine(i);
            if (mid == null)
            {
                // 不在缓存里：请一行（这是**用户主动复制**，不是渲染路径，等得起）
                _ = _doc.PrefetchAsync(i, i);
                SelectedTextTruncated = true;   // 本行拿不到 —— 让页面如实提示，别假装复制全了
                continue;
            }
            sb.Append('\n');
            sb.Append(mid);
        }
        if (!SelectedTextTruncated)
        {
            sb.Append('\n');
            sb.Append(Slice(_doc.GetLine(lb), 0, cb));
        }
        return sb.ToString();

        static string Slice(string? s, int from, int to)
        {
            if (string.IsNullOrEmpty(s)) return "";
            from = Math.Clamp(from, 0, s.Length);
            to = Math.Clamp(to, from, s.Length);
            return s.Substring(from, to - from);
        }
    }

    /// <summary>选区的显示文本（「3」或「3-7」行），无选区返回空串。状态栏用。</summary>
    public string SelectionChangedRange
    {
        get
        {
            if (!HasSelection) return "";
            long a = Math.Min(_selALine, _selBLine) + 1;
            long b = Math.Max(_selALine, _selBLine) + 1;
            return a == b ? $"{a}" : $"{a}-{b}";
        }
    }

    /// <summary>词的分类：0 = 空白/界外，1 = 字母数字下划线，2 = CJK，3 = 其它（各自成词）。</summary>
    private static int WordClass(char c)
    {
        if (char.IsWhiteSpace(c)) return 0;
        // ⚠ **CJK 必须先判**：`char.IsLetterOrDigit('中')` 是 **true**（汉字是 Unicode 字母类 Lo），
        // 放在后面的话第 2 类永远轮不到汉字 —— 长按 `value中文名` 会把整串当成一个词，
        // 而注释与更新日志写的都是「CJK 单独一类」。
        if (c >= 0x2E80 && c <= 0x9FFF) return 2;      // CJK（按码元判够用：区外没有代理对）
        if (char.IsLetterOrDigit(c) || c == '_') return 1;
        return 3;
    }

    private static bool IsWordBreak(char c) => WordClass(c) == 0;

    // ── 绘制 ──

#if DEBUG && ANDROID
    private bool _widthProbeDone;

    /// <summary>
    /// 宽度自检：把「测量」与「渲染」摆在同一行日志里。
    ///
    /// 点击定位的偏差全在「量出来的宽度」与「画出来的宽度」不一致上，而这两个数
    /// 在屏幕上用肉眼是比不出来的（渲染的墨迹右端与标尺位置差几个像素，看着都像「差不多」）。
    /// 所以直接把逐字符的实测值打到 logcat，再拿真机截图量墨迹，两者一对就定性了。
    /// </summary>
    private void LogWidthProbe(ICanvas canvas)
    {
        try
        {
            float fs = EditorTypography.FontSize;
            float d = (float)Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
            string M(string t, float size) =>
                $"[{t}]={canvas.GetStringSize(t, EditorTypography.CanvasFont, size).Width:F2}";

            var line0 = _doc?.GetLine(0) ?? "";
            var sb = new System.Text.StringBuilder();
            sb.Append($"fs={fs} density={d} ");
            sb.Append(M("a", fs)).Append(' ').Append(M("中", fs)).Append(' ');
            sb.Append(M("W", fs)).Append(' ').Append(M("|", fs)).Append(' ');
            sb.Append(M("中", fs * d)).Append(' ').Append(M("a", fs * d)).Append(' ');
            sb.Append(M("aB3|END", fs)).Append(' ').Append(M("中文，。！", fs)).Append(' ');
            // 整串 vs 单字之和：整串偏小就说明有字符被吞掉了。
            // 历史上全角标点连排踩过这个（见 CHANGELOG v0.96.116/117），留着这条，
            // 将来换字体或升级 MAUI 时一眼能看出来。
            sb.Append($"line0=[{line0}] W={canvas.GetStringSize(line0, EditorTypography.CanvasFont, fs).Width:F2}");

            Android.Util.Log.Info("WCW", sb.ToString());
        }
        catch (Exception ex) { Android.Util.Log.Info("WCW", "ERR " + ex.Message); }
    }
#endif

    /// <summary>字体自检只做一次（跨平台，见 Draw）。</summary>
    private bool _fontChecked;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = (float)Width, h = (float)Height;
        if (w <= 0 || h <= 0) return;

        _drawWatch.Restart();
        canvas.FillColor = _isDark ? EditorTypography.EditorBgDark : EditorTypography.EditorBg;
        canvas.FillRectangle(0, 0, w, h);

        if (_doc == null || _doc.LineCount == 0) return;

        float lineH = EditorTypography.LineHeight;
        float gutterW = GutterWidth();
        long first = (long)Math.Floor(_firstLine);
        int visible = (int)Math.Ceiling(h / lineH) + 2;
        long last = Math.Min(first + visible, _doc.LineCount);

        canvas.Font = EditorTypography.CanvasFont;
        canvas.FontSize = EditorTypography.FontSize;
        _measureCanvas = canvas;   // 供绘制之外的路径补测字宽（见 RuneWidth）
        _drawW = w;
        _drawH = h;
        try
        {
            // 这里原先每次绘制都调一次 GetStringSize("0") 量「一个字符多宽」，结果赋给一个
            // **从未被读取**的局部变量 —— 白烧一次平台文本测量（Release 下这个 try 里就只剩它）。
            // 字宽现在直接用字体设计值（见下方 _charWidthMeasured 块），不需要测。
#if DEBUG
            // 字体自检（一次性，跨平台）：**证明打包字体真的加载上了**。
            //
            // 字体名写错时平台**不报错**，只是静默回落成系统比例字体 —— 而定位走的是
            // 2 列网格 ⇒ 渲染比例、定位网格，症状就是「光标对不上位置」。
            // iOS 上正是这么栽的：CanvasFontName 写成了 SarasaMonoSC-Regular，
            // 而 UIFont.FromName 要的是 PostScript 名 Sarasa-Mono-SC-Regular。
            // 判据用字体自己的设计比例：半角 = 字号÷2、全角 = 字号。
            if (!_fontChecked)
            {
                _fontChecked = true;
                try
                {
                    float half = EditorTypography.HalfWidth, full = EditorTypography.FontSize;
#if WINDOWS
                    // 自检也必须用「渲染同引擎」的量法：`GetStringSize` 在 Win2D 上报的是错数，
                    // 用它自检会把**合格的等宽字体误判成「回落」**（实测 NSimSun 明明精确 7/14）。
                    var (advA, advWide) = MeasureAdvancesWin2D(canvas);
#else
                    float advA = (float)canvas.GetStringSize("a", EditorTypography.CanvasFont, full).Width;
                    float advWide = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont, full).Width;
#endif
                    bool ok = Math.Abs(advA - half) < 1.0f && Math.Abs(advWide - full) < 1.0f;
                    string tag = ok ? "[字体自检] OK  内嵌 Sarasa 已加载" : "[字体自检] ❌ 字体回落了！检查 CanvasFontName";
                    System.Diagnostics.Debug.WriteLine($"{tag} a={advA:F2}(期望 {half:F2}) 中={advWide:F2}(期望 {full:F2}) 名={EditorTypography.CanvasFontName}");
                    DiagLog.Write("字体自检", $"{tag} a={advA:F2}(期望 {half:F2}) 中={advWide:F2}(期望 {full:F2}) 名={EditorTypography.CanvasFontName}");
                }
                catch { }
            }
#endif
#if DEBUG && ANDROID
            if (!_widthProbeDone && ShowDebugHud) { _widthProbeDone = true; LogWidthProbe(canvas); }
#endif
            // 触摸坐标是相对本控件的；把画布尺寸和最后一次按下的坐标一起报出来，
            // 才能判断「滚动条热区为什么没命中」
#if DEBUG
            // 探针每次绘制都要量一遍整行，1K 字符的行并不便宜 —— 只在调试构建里算。
            // 用途：比对「逐字累加」与「整行一次测量」是否一致（不一致就说明宽度模型有偏）。
            var l0 = _doc?.GetLine((long)first);
            float sum = (l0 == null || l0.Length > EditorTypography.MaxTokenizeChars) ? 0 : MeasurePrefixWidth(l0, l0.Length);
            float whole = (l0 == null || l0.Length > EditorTypography.MaxTokenizeChars) ? 0 : (float)canvas.GetStringSize(l0,
                EditorTypography.CanvasFont, EditorTypography.FontSize).Width;
            MeasureProbe = $"sum{sum:F0}/whole{whole:F0} ratio{(whole > 0 ? sum / whole : 0):F2}";
#endif
        }
        catch (Exception ex) { MeasureProbe = "ERR:" + ex.GetType().Name; }

        // 量一次**平台真实推进量**（半角 / 全角各一个）。只做一次，之后整帧都用它
        // —— 放到每帧测会平白多两次文本测量。
        if (!_charWidthMeasured)
        {
            MeasureAdvances(canvas);
            _charWidthMeasured = true;
        }

        // ① 光标行 / 选择行底色（在文字下面）
        DrawLineBackgrounds(canvas, first, last, gutterW, w, lineH);
        _tBg = (float)_drawWatch.Elapsed.TotalMilliseconds;

        // ② 正文（裁剪在行号栏右侧，横向滚动只影响这一层）
        float textX = gutterW + EditorTypography.TextLeftPad - _scrollX;
        // 本文件的诊断 —— **只查一次**。DrawDiagnosticWave 是每行每帧都要查一次表的
        // （内部走 LINQ `.Where().ToList()`，没数据时也要分配一个空 List），
        // 而移动端多数文件压根没有诊断（依赖 LintTool，MAUI 里是桩，见 CLAUDE.md）⇒ 整段空跑。
        // 气泡层也吃这一份（同一批数据、同一次查询）。
        var diags = SnapshotDiagnostics();
        bool hasDiags = diags.Count > 0;
        canvas.SaveState();
        canvas.ClipRectangle(gutterW, 0, Math.Max(0, w - gutterW), h);

        for (long i = first; i < last; i++)
        {
            float y = LineY(i, lineH);
            bool editing = i == EditingLine - 1;

            var line = editing ? (EditingText ?? _doc.GetLine(i)) : _doc.GetLine(i);
            if (line == null)
            {
                // 只画占位、**不在这里补取**：逐行启动 Task 的话，60fps 下每帧几十个任务堆积。
                // 补取统一走 Draw 末尾的那一次批量预取（它已覆盖可见区间）。
                DrawPending(canvas, textX, y, lineH);
                continue;
            }

            if (line.Length > EditorTypography.MaxTokenizeChars)
            {
                // 超长行（minified JSON 那种整份一行的）：只把**可见的那一段**交给平台排版。
                // 整条塞进去的话每帧都要布局几万个字符，滚动必然卡死；而且这种行本来也不上色。
                var (seg, segX) = ClipToViewport(line, textX);
                if (seg.Length > 0)
                {
#if WINDOWS
                    // 同上：Win2D 只有 DrawString 可用
                    canvas.FontColor = MarkupToFormattedString.ColorForToken(0, _isDark);
                    canvas.DrawString(seg, segX, Win2DStringY(y + EditorTypography.TextBaselineOffset),
                        HorizontalAlignment.Left);
#else
                    canvas.DrawText(new AttributedText(seg, []), segX,
                        y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
#endif
                }
            }
            else if (line.Length > 0)
            {
                // 整行一次绘制（见 DrawLineRuns —— 位置由网格与平台共同保证一致）。
                // 几何来自行缓存（编辑行走 EditingRuns，它只在击键时失效）。
                DrawLineRuns(canvas, editing ? EditingRuns(line) : GetLineRuns(i, line),
                    textX, y, lineH);
            }

            if (editing) DrawCaret(canvas, line, textX, y, lineH);
            else if (i == _caretLine) DrawBrowseCaret(canvas, line, textX, y, lineH);

            if (hasDiags) DrawDiagnosticWave(canvas, i, y, textX, lineH);
        }
        canvas.RestoreState();
        _tText = (float)_drawWatch.Elapsed.TotalMilliseconds - _tBg;

        // 选区手柄画在正文之上（端点要能压住字），但在行号栏之下（别糊到行号上去）
        DrawSelectionHandles(canvas);

        // ③ 行号栏（最后画 —— 它会盖掉光标行底色横跨过来的那一段）
        //
        // **行号始终画**（v0.96.141 起）。这里原先有一句「视口在动的这一帧跳过数字」的优化，
        // 判据是「本帧位姿与上帧是否相同」；省下的时间不多，代价却是**数字一直在闪**
        // （用户实测反馈「行号容易闪烁，还是一直显示比较好」）。现在行号与正文共用同一套
        // **缓存排版**（见 TryDrawTextCached），一次编译反复绘制 ⇒ 不闪，而且比以前更快。
        DrawGutter(canvas, first, last, gutterW, h, lineH);
        _tGutter = (float)_drawWatch.Elapsed.TotalMilliseconds - _tBg - _tText;

        // ④ 编译诊断气泡 —— **最上层**（正文 / 波浪线 / 手柄 / 行号栏之后；只有滚动条与调试 HUD
        //    压在它上面）。它就是代码层的一部分：跟着滚动、跟着缩放，滚出屏幕就看不见。
        //    无诊断时也要走一次（它只做命中表清空）—— 上一帧的气泡没了却留着命中表，
        //    那些位置就会继续吃触摸。
        if (hasDiags) DrawDiagnosticBubbles(canvas, diags, w, h, lineH);
        else _bubbleHits.Clear();

#if DEBUG
        // 调试标尺：在**测量出来的行尾**画一条竖线（仅在调试 HUD 打开时）。
        //
        // 它的价值在于**把「偏了多少」从目测变成可量** —— 前面正是靠它（配合截图取墨迹列）
        // 定位到「测量比渲染窄」，也是靠它验证了修复（偏差 24.5px → 1.5px）。
        // 保留不删：这类「两边看着都差不多、实际差一截」的问题，肉眼比不出来。
        if (ShowDebugHud)
        {
            var rulerLine = _doc?.GetLine(first);
            if (rulerLine is { Length: > 0 })
            {
                float endX = gutterW + EditorTypography.TextLeftPad
                    + (rulerLine.Length > EditorTypography.MaxTokenizeChars ? 0f
                        : MeasurePrefixWidth(rulerLine, rulerLine.Length)) - _scrollX;
                canvas.StrokeColor = Colors.Red;
                canvas.StrokeSize = 1f;
                canvas.DrawLine(endX, 0, endX, lineH * 3);
            }
        }
#endif
        DrawScrollbars(canvas, w, h);   // 最上层：HUD 之前，免得被正文覆盖
        if (ShowDebugHud) DrawDebug(canvas, w, h, first, last, gutterW, lineH);

        // 首帧画完，字宽/行数这些「测量后才准」的值才算数 —— 通知一次让状态栏补上
        if (!_measuredNotified && _charWidthMeasured)
        {
            _measuredNotified = true;
            ViewChanged?.Invoke();
        }

        RequestPrefetch(first - 40, last + 40);

#if ANDROID
        // 【不变量自检·一次性】校验光标定位用的那把尺子与平台渲染**真的是同一把**。
        //
        // 定位现在是「逐字形累加平台实测推进量」（MeasureAdvances 量半角/全角各一个字符）。
        // 这条等式成立要求**推进量可加**：N 个字形的排版宽度 == N × 单个字形的推进量。
        // 若平台只在「整行」上取整（而不是逐字形），可加性就会破，我们逐字累加就会越加越偏
        // —— 那正是这行自检要抓的。
        //
        // 早期版本测的是「平台排版 == 列网格」，得出过一条结论：**Android 逐字形取整**，
        // 于是只有偶数号两边才相等（实测偶数号 0.00px、13 号 53.5px）。定位改成实测推进量之后
        // 这条限制不再需要 —— 但**先分别验两种字号**，别默认它对所有字号都成立。
        //
        // ⚠ 量短前缀、别拿整行去量：`GetStringSize` 走 `PlatformStringSizeService`，是无界排版
        // （`boundedWidth: null` ⇒ 宽 int.MaxValue，**不会折行**）取 `GetLineWidth(i)` 的真实浮点宽，
        // 所以它报的是真值。用前缀是为了专门看「前 N 个字」这一段。
        if (!_widthAuditDone && _doc != null && first < _doc.LineCount)
        {
            _widthAuditDone = true;
            // **要挑到「难的那几行」**：只拿一条纯 ASCII 行来验，等于没验到 emoji / CJK / tab
            // 这三条分支（它们各有各的字体与推进量）。所以最多挑 4 条：带 emoji 的、带 tab 的、
            // 带中文的、以及第一条较长的 —— 每条都跑一遍可加性。
            var probes = new List<string>();
            long scanTo = Math.Min(first + 60, _doc.LineCount);
            for (long i = first; i < scanTo && probes.Count < 4; i++)
            {
                var l = _doc.GetLine(i);
                if (string.IsNullOrEmpty(l) || l.Length < 12) continue;
                bool hasEmoji = false, hasTab = l.Contains('\t'), hasCjk = false;
                foreach (var r in l.EnumerateRunes())
                {
                    int cp = r.Value;
                    if (cp is >= 0x1F000 and <= 0x1FAFF or >= 0x2600 and <= 0x27BF
                        or >= 0x2B00 and <= 0x2BFF or >= 0x23E9 and <= 0x23F3) hasEmoji = true;
                    if (cp is >= 0x4E00 and <= 0x9FFF) hasCjk = true;
                }
                bool want = hasEmoji || hasTab || hasCjk || (l.Length > 60 && probes.Count == 0);
                if (!want) continue;
                if (probes.Any(p => p == l)) continue;
                probes.Add(l);
            }
            double worst = 0;
            foreach (var probeText in probes)
            {
                var disp = TextEditorMath.ExpandTabs(probeText, EditorTypography.TabColumns);
                foreach (float size in new[] { 8f, 12f, 13f, 16f, 28f, 30f })
                {
                    // 该字号下的实测推进量（与 MeasureAdvances 同源，但这里是临时量、不写入字段）
                    float lat = (float)canvas.GetStringSize("0", EditorTypography.CanvasFont, size).Width;
                    if (lat <= 0) continue;
                    foreach (int n in new[] { 30, 60, 120 })
                    {
                        var prefix = RunePrefix(disp, n);
                        if (prefix.Length == 0) continue;
                        // 我们逐字累加会算出的宽度：半角 / 打包字体的全角 / 其它宽字符（逐个实测）
                        float wide = 0, mine = 0;
                        foreach (var r in prefix.EnumerateRunes())
                        {
                            int cw = WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r);
                            if (cw <= 0) continue;
                            if (cw == 1) { mine += lat; continue; }
                            if (IsPackedFontWide(r))
                            {
                                if (wide <= 0)
                                    wide = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont, size).Width;
                                mine += wide;
                            }
                            else
                                mine += (float)canvas.GetStringSize(r.ToString(),
                                    EditorTypography.CanvasFont, size).Width;
                        }
                        float platW = (float)canvas.GetStringSize(prefix,
                            EditorTypography.CanvasFont, size).Width;
                        worst = Math.Max(worst, Math.Abs(platW - mine));
                    }
                }
            }
            bool ok = worst < 1.5;
            // emoji 与 CJK **是不是同一个推进量**：不是的话就说明它们由不同字体渲染，
            // 「宽字符一律按全角宽算」会错 —— 这正是 AdvanceOf 里要分两档、逐个实测的理由。
            float cjkAdv = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
            float emojiAdv = (float)canvas.GetStringSize("😀", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
            string emojiNote = Math.Abs(emojiAdv - cjkAdv) < 0.01f
                ? "同宽" : "**不同宽 ⇒ 必须实测**";
            Android.Util.Log.Info("WCFONT",
                $"{(ok ? "[排版自检] OK" : "[排版自检] ❌ 推进量不可加！")} "
                + $"逐字累加 vs 平台排版 最大偏差={worst:F2}px 探针行={probes.Count} 字号={EditorTypography.FontSize:F1} "
                + $"中={cjkAdv:F2} 😀={emojiAdv:F2}（{emojiNote}）");
        }
#endif

        _drawWatch.Stop();
        LastDrawMs = _drawWatch.Elapsed.TotalMilliseconds;
        if (LastDrawMs > _drawMsPeak) _drawMsPeak = LastDrawMs;
    }

    /// <summary>
    /// 最近一帧的**本控件绘制耗时**（ms）—— 只计 <see cref="Draw"/> 内部，不含平台合成。
    ///
    /// 这是调性能时唯一该看的数：gfxinfo 报的是整帧（含合成/GPU），分不出「是我们慢」
    /// 还是「别处慢」。开「设置 → 编辑器调试 HUD」后它显示在画布顶部，滚动/缩放时是活的。
    /// </summary>
    public double LastDrawMs { get; private set; }

    /// <summary>本次手势期间的**最差**一帧（每次手指按下清零）——卡顿看峰值，不看均值。</summary>
    private double _drawMsPeak;

    // （原先这里有 _lastDrawnFirstLine/_lastDrawnScrollX 两个字段，用来判断「本帧视口是否在动」
    //  从而跳过行号数字。v0.96.141 起行号**始终画**、并改用缓存排版，这套判断连同字段一起删了。
    //  「跳过绘制」这类优化一旦去掉触发它的理由，留下的字段就是纯粹的误导。）

    /// <summary>
    /// 「非打包字体的宽字符」的实测推进量缓存（emoji 等，按码点）。改字号时清空（见 <see cref="MeasureAdvances"/>）。
    /// 上限存在的意义只是防病态输入把内存撑爆 —— 正常文件里这类字符是个位数。
    /// </summary>
    private const int MaxAdvanceCache = 4096;
    private readonly Dictionary<int, float> _advanceCache = [];

    /// <summary>
    /// 上次更新 <see cref="_scrollX"/> 时的字号。改字号时用它把横向偏移**按比例**换算到新字号
    /// （见 <see cref="ResetTypography"/>）—— 否则缩放会把视口拽回最左边。
    /// **每次换算完必须就地更新**，否则比例会连乘。
    /// </summary>
    private float _scrollFontSize;


#if ANDROID
    /// <summary>「平台排版 == 网格」的自检只做一次（见 Draw 里那段）。</summary>
    private bool _widthAuditDone;

    /// <summary>取前 n 个**码元**（不是 char —— 代理对不能被劈开）。</summary>
    private static string RunePrefix(string s, int runes)
    {
        int taken = 0, end = 0;
        foreach (var r in s.EnumerateRunes())
        {
            if (taken++ >= runes) break;
            end += r.Utf16SequenceLength;
        }
        return end >= s.Length ? s : s[..end];
    }
#endif

    /// <summary>诊断用：最近一次点击的画布坐标、画布高度、算出的行号。</summary>
    public string LastHitDebug { get; private set; } = "";

    private long _pfFrom = -1, _pfTo = -1, _pfTicks;

    /// <summary>
    /// 批量预取（带去重）：Draw 每帧都会走到这里，不节流的话 60fps 下每秒会启动几十个 Task。
    /// 同一区间在 400ms 内只发一次；区间变了立即发（滚动时预取窗口跟着走）。
    /// </summary>
    private void RequestPrefetch(long from, long to)
    {
        if (_doc == null) return;
        long now = Environment.TickCount64;
        if (from == _pfFrom && to == _pfTo && now - _pfTicks < 400) return;
        _pfFrom = from; _pfTo = to; _pfTicks = now;
        _ = _doc.PrefetchAsync(from, to);
    }

    /// <summary>行号栏宽度：按最大行号位数算（等宽字体 ⇒ 位数 × 字宽）。</summary>
    private float GutterWidth()
    {
        int digits = Math.Max(3, (_doc?.LineCount ?? 1).ToString().Length);
        return digits * _charWidth + EditorTypography.GutterRightPad + EditorTypography.TextLeftPad;
    }

    private float LineY(long line, float lineH)
        => EditorTypography.VerticalPad + (float)(line - _firstLine) * lineH;

    /// <summary>
    /// 光标行底色 + **字符级选区**底色（都在文字下面）。
    ///
    /// 选区按**字符跨度**铺，不是整行铺：起始列的左边不涂、结束列的右边不涂，
    /// 起点/终点都取「字符格子的左边缘」—— 与光标同源（<see cref="MeasurePrefixWidth"/>），
    /// 所以选区的边界与文字边界永远对得上。
    /// </summary>
    private void DrawLineBackgrounds(ICanvas canvas, long first, long last,
        float gutterW, float w, float lineH)
    {
        bool selActive = HasSelection;
        var (selA, colA, selB, colB) = selActive ? NormalizedSelection() : (-1L, 0, -1L, 0);

        float textX = gutterW + EditorTypography.TextLeftPad - _scrollX;

        for (long i = first; i < last; i++)
        {
            bool caret = i == _caretLine;
            bool inSel = selActive && i >= selA && i <= selB;
            bool matchLine = i == _matchALine || i == _matchBLine;
            if (!inSel && !caret && !matchLine) continue;

            // 高亮条与文字用**同一个 y**（都是行顶）。二者曾经因为一处算了基线补偿、
            // 另一处没算而差开半行 —— 现在两边都直接取行顶，没有第二套算法。
            float y = LineY(i, lineH) + EditorTypography.TextBaselineOffset;

            // ⚠ **整行底色只给光标行与选区行**：配对括号只是两个**字符格**，
            // 让它们也铺整行的话，光标停在一个括号旁边时会多出一条横贯整屏的色带。
            if (caret || inSel)
            {
                canvas.FillColor = _isDark ? EditorTypography.CaretLineBgDark : EditorTypography.CaretLineBg;
                canvas.FillRectangle(0, y, w, lineH);
            }

            if (matchLine) DrawBracketMatch(canvas, i, textX, y, lineH);

            if (!inSel) continue;

            var line = _doc?.GetLine(i);
            if (line == null) { canvas.FillColor = EditorTypography.SelectionBg;
                canvas.FillRectangle(0, y, w, lineH); continue; }

            int from = i == selA ? colA : 0;
            int to = i == selB ? colB : line.Length;
            if (to < from) (from, to) = (to, from);

            float x0 = textX + MeasurePrefixWidth(line, from);
            // 行尾/空行的选区给一个「一个字符宽」的最小可见段，否则选中空行时什么都看不见
            float x1 = to > from ? textX + MeasurePrefixWidth(line, to) : x0 + _charWidth;
            canvas.FillColor = EditorTypography.SelectionBg;
            canvas.FillRectangle(x0, y, Math.Max(1f, x1 - x0), lineH);
        }
    }

    // ── 配对括号高亮 ──
    //
    // ⚠ **配对逻辑不在这里**：画布只拿得到"当前可见的那几行"，而配对要跨行扫描 ——
    // 放在这里会出现"滚动一下配对就变了"这种最难查的现象。宿主（EditorPage）算好两个位置传进来，
    // 这里只负责把两个**字符格**涂上色（与选区同一套坐标：起点取字符格左边缘，
    // 与 MeasurePrefixWidth 同源，所以色块边界与文字边界永远对得上）。
    private long _matchALine = -1;
    private int _matchACol;
    private long _matchBLine = -1;
    private int _matchBCol;

    /// <summary>
    /// 设置"光标贴着的那个括号 + 它配对的那一个"（宿主算好传进来；没有就传 null）。
    /// 两个位置可以同行（`()` 挨在一起）。
    /// </summary>
    public void SetBracketMatch((long Line, int Col)? a, (long Line, int Col)? b)
    {
        var al = a?.Line ?? -1;
        var bl = b?.Line ?? -1;
        var ac = a?.Col ?? 0;
        var bc = b?.Col ?? 0;
        if (al == _matchALine && bl == _matchBLine && ac == _matchACol && bc == _matchBCol) return;

        _matchALine = al; _matchACol = ac;
        _matchBLine = bl; _matchBCol = bc;
        InvalidateAll();
    }

    /// <summary>画出这一行上属于配对的两个字符格（可能两个都在这一行）。</summary>
    private void DrawBracketMatch(ICanvas canvas, long line, float textX, float y, float lineH)
    {
        var text = _doc?.GetLine(line);
        if (text == null) return;
        canvas.FillColor = _isDark ? EditorTypography.BracketMatchBgDark : EditorTypography.BracketMatchBg;
        if (line == _matchALine) FillBracketCell(canvas, text, _matchACol, textX, y, lineH);
        if (line == _matchBLine) FillBracketCell(canvas, text, _matchBCol, textX, y, lineH);
    }

    private void FillBracketCell(ICanvas canvas, string text, int col, float textX, float y, float lineH)
    {
        if (col < 0 || col >= text.Length) return;
        var x0 = textX + MeasurePrefixWidth(text, col);
        var x1 = textX + MeasurePrefixWidth(text, Math.Min(col + 1, text.Length));
        canvas.FillRectangle(x0, y, Math.Max(1f, x1 - x0), lineH);
    }

    /// <summary>
    /// 画选区两端的**手柄**（自己画的，不用平台的）。
    ///
    /// 位置全部来自 <see cref="HandlePositions"/> —— 与命中测试**同一份**计算，
    /// 所以「看到的圆点」和「抓得住的圆点」永远重合。外圈白环是为了压在任何底色上都看得见。
    /// </summary>
    private void DrawSelectionHandles(ICanvas canvas)
    {
        if (!HasSelection) return;
        var h = HandlePositions();
        if (h is not { } hs) return;

        float r = EditorTypography.HandleRadius;
        foreach (var (x, y) in new[] { hs.A, hs.B })
        {
            // 视口外的端点不画（拖到屏外时它会跟着跑，画在屏外没有意义）
            if (y < -lineHGuard || y > (float)Height + lineHGuard) continue;
            canvas.FillColor = EditorTypography.HandleRing;
            canvas.FillCircle(x, y, r + 1.6f);
            canvas.FillColor = EditorTypography.HandleFill;
            canvas.FillCircle(x, y, r);
        }
    }

    /// <summary>手柄的可见性判据里那点余量（把行高另存一份没有意义，直接用行的两倍）。</summary>
    private float lineHGuard => EditorTypography.LineHeight * 2f;

    /// <param name="withNumbers">
    /// 是否画行号**数字**。视口正在移动（本帧滚动位置与上帧不同）时传 false ——
    /// 见 <see cref="Draw"/> 里对 <c>_lastDrawnFirstLine</c> 的说明。
    /// </param>
    /// <summary>
    /// 一行**单色纯文本**的缓存条目 —— **行号栏与诊断气泡共用这一份缓存**。
    ///
    /// 两者都是「频繁重绘的单色短文本」，而 Android 的 <c>DrawText</c> 每次都新建一个
    /// <c>StaticLayout</c>（见 <see cref="TryDrawCachedLayout"/> 的长注释）—— 各建一套缓存
    /// 就是两份一模一样的机制（本仓库反复记下的「同一规则两处实现」）。
    /// </summary>
    private sealed class TextEntry
    {
        public IAttributedText Text = null!;
#if ANDROID
        public Android.Text.StaticLayout? Layout;
        public Android.Text.SpannableString? Span;
        /// <summary>这份排版是按哪个字号编的（行号比正文小一号、气泡与正文同号）。</summary>
        public float FontSize;
#endif
    }

#if ANDROID
    /// <summary>用缓存排版画一行单色文本；拿不到原生画布/画不成时返回 false（调用方回退 DrawText）。</summary>
    private static bool TryDrawTextCached(ICanvas canvas, TextEntry entry, float x, float y, float fontSize)
    {
        if (canvas is not Microsoft.Maui.Graphics.Platform.PlatformCanvas pc) return false;
        var native = pc.Canvas;
        if (native is null) return false;

        float size = fontSize;
        if (entry.Layout is null || Math.Abs(entry.FontSize - size) > 0.01f)
        {
            entry.Layout?.Dispose();
            entry.Span?.Dispose();
            entry.Span = BuildSpannable(entry.Text);
            if (entry.Span is null) return false;
            entry.Layout = new Android.Text.StaticLayout(entry.Span, BuildTextPaint(size),
                int.MaxValue, Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
            entry.FontSize = size;
        }

        native.Save();
        native.Translate(x, y);
        entry.Layout!.Draw(native);
        native.Restore();
        return true;
    }
#endif

    private void DrawGutter(ICanvas canvas, long first, long last, float gutterW, float h, float lineH)
    {
        canvas.FillColor = _isDark ? EditorTypography.GutterBgDark : EditorTypography.GutterBg;
        canvas.FillRectangle(0, 0, gutterW, h);

        // ⚠ **行号始终画，不要再「滚动时跳过」**（v0.96.141 改回来）。
        //
        // 那是 v0.96.130 为省时间做的：每行一次平台文本绘制，字号 8 时一屏 50 多行、
        // 行号栏占其中一半。但**代价是数字一直在闪** —— 判据是「本帧视口位姿与上帧是否相同」，
        // 于是惯性滚动期间数字忽有忽无，视觉上比省下的那点时间糟得多（用户实测反馈）。
        // 真正的解法不是「少画」而是「别每帧重排版」：行号字符串高度重复（就那几十个数字），
        // 现在与正文走**同一套缓存排版**（见 TextEntry），一次编译反复绘制 ⇒
        // 既不闪、又比原来快。
        canvas.FontSize = EditorTypography.FontSize - 1;
        canvas.SaveState();
        canvas.ClipRectangle(0, 0, gutterW, h);
        for (long i = first; i < last; i++)
        {
            var label = (i + 1).ToString();
            float y = LineY(i, lineH);
            float x = gutterW - EditorTypography.GutterRightPad - label.Length * _charWidth;
            var color = i == _caretLine
                ? (_isDark ? Colors.White : Colors.Black)
                : EditorTypography.GutterFg;
            // 与正文同一条路：能拿到原生画布就用**缓存好的排版**画，否则回退到 DrawText。
            // （行号也用排版而非 DrawString：两者的 y 语义在平台上并不一致
            //  —— DrawString/Android 是 em 底、iOS 是基线 —— 混用会让行号与代码整体错开。）
            // 落笔路径与诊断气泡**共用一个实现**（见 DrawPlainText）。
            DrawPlainText(canvas, label, color, x, y, lineH, EditorTypography.FontSize - 1);
        }
        canvas.RestoreState();
    }

    /// <summary>
    /// 画一行**单色纯文本** —— 行号栏与诊断气泡**共用这一条落笔路径**（平台的差异只在这里一处）。
    ///
    /// 为什么不用 <c>DrawString</c> 一把梭：它的 y 语义在平台上并不一致
    /// （Android 是 em 底、iOS 是基线），而 <c>DrawText</c> 是左上角锚定、两端一致 ——
    /// 混用会让两处文本整体错开。调用方负责把 <c>canvas.FontSize</c> 设成
    /// <paramref name="fontSize"/>（只有 Windows 的 <c>DrawString</c> 那条路会读它）。
    /// </summary>
    private void DrawPlainText(ICanvas canvas, string text, Color color,
        float x, float y, float lineH, float fontSize)
    {
        if (text.Length == 0) return;
        var entry = TextEntryFor(text, color, fontSize);
#if ANDROID
        if (TryDrawTextCached(canvas, entry, x, y + EditorTypography.TextBaselineOffset, fontSize)) return;
#endif
#if WINDOWS
        // Win2D 无 DrawText(IAttributedText) 实现 ⇒ 走 DrawString，y 要补一个字号
        // （见 Win2DStringY）。这类文本都是单色，直接设 FontColor 即可。
        canvas.FontColor = color;
        canvas.DrawString(text, x, Win2DStringY(y + EditorTypography.TextBaselineOffset),
            HorizontalAlignment.Left);
#else
        canvas.DrawText(entry.Text, x, y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
#endif
    }

    /// <summary>
    /// 自绘光标。
    ///
    /// **为什么必须自绘**：光标本来交给原生 <c>Entry</c> 画，但它的位置取决于平台自己的
    /// 内边距与行内对齐（Android 的 EditText 单行默认垂直居中），我们算不出、也就对不齐 ——
    /// 实测表现就是「光标在光标行的下方乱飘」。用 <c>Entry.setCursorVisible(false)</c> 把系统
    /// 光标藏掉、改由画布画，它的位置就只由行高与列宽决定，和文字出自同一套计算。
    ///
    /// 横向按**视觉列**定位（CJK 占两列）而不是字符数：中文行里两者差一倍。
    /// </summary>
    /// <summary>
    /// **只读态的浏览光标**：一根**不闪烁**的竖线。
    ///
    /// 存在的理由（Windows 实机反馈）：以前只读态「光标」只是那一层
    /// <c>CaretLineBgDark</c>（白 4%）的行底色 —— 深色代码底上等于看不见，
    /// 于是「按上下键到底动没动」完全无法判断。行底色是「当前行」的弱提示，
    /// 真正的「我在第几列」必须有这根线。
    ///
    /// **刻意不闪烁**（编辑态那根才闪）：浏览时闪一下没一下反而更难确认位置，
    /// 而且这里的用途之一就是「截屏能看见」。
    /// </summary>
    private void DrawBrowseCaret(ICanvas canvas, string line, float textX, float y, float lineH)
    {
        int col = Math.Clamp(_caretCol, 0, line.Length);
        float x = textX + MeasurePrefixWidth(line, col);

        canvas.StrokeColor = _isDark ? Colors.White : Colors.Black;
        canvas.StrokeSize = Math.Clamp(_charWidth * 0.2f, 1f, 2f);
        canvas.DrawLine(x, y + 2, x, y + lineH - 3);
    }

    private void DrawCaret(ICanvas canvas, string line, float textX, float y, float lineH)
    {
        int col = Math.Clamp(EditingCursor, 0, line.Length);
        float x = textX + MeasurePrefixWidth(line, col);

        // 深色底用纯白、浅色底用纯黑：系统那个光标跟随主题色（Android 上是 Material 紫），
        // 在深色代码背景上很不起眼。这里明确取对比度最高的两色。
        canvas.StrokeColor = _isDark ? Colors.White : Colors.Black;

        // ⚠ **光标宽度必须跟着「一个字符多宽」走，不能写死。**
        // 原先固定 2.5pt（≈6.9px）是按正文字号的手感定的，但格子宽度是随字号缩的：
        // 字号 8 时半角格子只有 **11px**，6.9px 的光标占了格子大半，左右各压到相邻字形上
        // ——用户看到的就是「光标叠在 s 字母上」。它本身**位置是对的**（落在字符边界、
        // 与 MeasurePrefixWidth 同源），纯粹是太胖，把「在间隔里」画成了「压在字上」。
        // 取格子宽度的 1/5，并夹在「看得见」与「别太胖」之间：
        // 字号 8 → 1.0pt（约 2.8px，格子 11px）、14 → 1.4pt、24 → 2.0pt（5.5px，格子 33px）。
        //
        // ⚠ 上限**不能大**：光标是**居中画在格线上**的（与 MeasurePrefixWidth 同源，位置本来就是对的），
        // 于是它左右各伸出一半宽度。而 `P`/`H` 这类**左竖笔紧贴格线**的字形，左留白几乎是 0 ——
        // 光标一胖就把那根竖笔整个盖住，用户看到的就是「光标完全压在 P 上面」。
        // 所以宽度要小于「两个字形之间的空隙」，而不是「格子的某个比例」：
        // 实测本字体在字号 24 下字形两侧留白各约 4px，2pt(5.5px) 居中 → 单侧 2.8px，正好塞得下。
        // （v0.96.137 第一次改成 0.25 并夹 2.5pt 上限，字号 24 时被上限吃满 ⇒ 等于没改。）
        canvas.StrokeSize = Math.Clamp(_charWidth * 0.2f, 1f, 2f);
        if (_caretOn) canvas.DrawLine(x, y + 2, x, y + lineH - 3);

        // **调试标记：光标两头的小三角，不闪烁**（用户提的：截屏时得能稳定看到光标在哪）。
        // 只在调试 HUD 打开时画，正式用户看不到。颜色刻意用洋红 —— 语法高亮与选区都不用这个色，
        // 于是「按颜色找光标」在截屏分析里是一行代码的事，不受闪烁相位影响。
        // ⚠ **三角标记必须在闪烁判断之外**（用户实测踩到过）：一开始把
        // `if (!_caretOn) return;` 写在函数开头，于是「灭」的半周期整个函数提前返回、
        // **标记也一起没了** —— 而标记存在的全部意义就是「截屏时一定看得到光标」。
        // 现在只有那根竖线受 `_caretOn` 约束。
        //
        // 开关是「设置 → 编辑器 → 调试 HUD」：用户自己就能打开，打开后截屏里稳定看得到光标在哪
        if (ShowDebugHud) DrawCaretMarkers(canvas, x, y, lineH);
    }

    private static void DrawCaretMarkers(ICanvas canvas, float x, float y, float lineH)
    {
        canvas.FillColor = Colors.Magenta;
        const float w = 4f, h = 5f;   // 半宽 / 高
        var top = new PathF();
        top.MoveTo(x, y + 2);            // 尖端指向光标顶端
        top.LineTo(x - w, y + 2 - h);
        top.LineTo(x + w, y + 2 - h);
        top.Close();
        canvas.FillPath(top);

        var bottom = new PathF();
        bottom.MoveTo(x, y + lineH - 3); // 尖端指向光标底端
        bottom.LineTo(x - w, y + lineH - 3 + h);
        bottom.LineTo(x + w, y + lineH - 3 + h);
        bottom.Close();
        canvas.FillPath(bottom);
    }

    private readonly Dictionary<string, TextEntry> _textCache = [];

    /// <summary>
    /// 单色文本（行号 / 诊断气泡正文）的带色对象，按「文本 + 颜色 + **字号**」缓存 ——
    /// 这两处的字符串高度重复（行号就那几十个数字、气泡正文只在诊断变化时才变），逐帧重建毫无必要；
    /// Android 上连**平台排版**一起缓存（见 <see cref="TryDrawTextCached"/>）。
    ///
    /// ⚠ 缓存键**必须带上字号**：排版的编译与字号绑定，而同一个字符串完全可能以两种字号出现
    /// （行号是「正文 − 1」、气泡与正文同号；一行内容恰好等于某个行号数字时就会撞上）。
    /// 键里漏了它，那一项就会**每帧重建两次排版**（平台上每次都是一次完整 StaticLayout）——
    /// 正是本仓「缓存键忘了带上会影响它的那个输入」那类坑。
    /// </summary>
    private TextEntry TextEntryFor(string label, Color color, float fontSize)
    {
        var key = label + "|" + color.ToHex() + "|" + fontSize.ToString("0.##");
        if (_textCache.TryGetValue(key, out var cached)) return cached;

        // ⚠ 这里**不能**写 FontName —— 与正文段同一条铁律（见 BuildLineRuns 的长注释）：
        // run 上写 FontName 会被 MAUI 变成 Android 的 `TypefaceSpan(族名)`，而那个 API 只认
        // **系统字体族名、没有 asset 重载**，喂资产名 `SarasaMonoSC-Regular.ttf` 进去解析不到，
        // 只会**静默回落成平台默认的比例字体** —— 行号数字于是不是等宽的（右对齐的位数会歪）。
        // 不写则布局回落用 `canvas.Font`（= EditorTypography.CanvasFont），走的才是
        // `CreateFromAsset` 分支、能加载打包字体。
        var entry = new TextEntry
        {
            Text = new AttributedText(label,
            [
                new AttributedTextRun(0, label.Length, new TextAttributes
                {
                    [TextAttribute.Color] = color.ToHex(),
                }),
            ]),
        };
        if (_textCache.Count < 512) _textCache[key] = entry;
        return entry;
    }

    /// <summary>清空单色文本缓存（连排版一起释放）。改字号 / 换主题时调。</summary>
    private void ClearTextCache()
    {
#if ANDROID
        foreach (var e in _textCache.Values) { e.Layout?.Dispose(); e.Span?.Dispose(); }
#endif
        _textCache.Clear();
    }

    /// <summary>尚未加载的行：画一个占位符，绝不在这里等 IO（滚动会被拖成一顿一顿的）。</summary>
    private static void DrawPending(ICanvas canvas, float x, float y, float lineH)
        => canvas.DrawString("⋯", x, y, HorizontalAlignment.Left);

    /// <summary>
    /// 本文件的诊断快照（**每帧只取一次**，而不是每行查一次）。
    ///
    /// 返回的就是 <c>DiagnosticManager</c> 里那份表本身（不复制）—— 那边增删是**整体替换引用**
    /// （见 <c>DiagnosticManager.CacheDiagnostics</c>），所以拿引用做「变了没有」的比较是成立的
    /// （气泡内容就是照这个缓存的，见 <see cref="EnsureBubbleGroups"/>）。
    /// 空路径、查询异常一律判为「没有」。
    /// </summary>
    private List<Diagnostic> SnapshotDiagnostics()
    {
        if (_filePath.Length == 0) return [];
        try { return DiagnosticManager.GetAll(_filePath); } catch { return []; }
    }

    /// <summary>
    /// 语法错误波浪线 —— 在该行文字下方画一段三角波。
    /// 宽度只覆盖有诊断的列区间（拿不到列号时覆盖整行），并**限制最大长度**：
    /// 一条长行上画几千个折点会把一帧拖垮。
    /// </summary>
    private const float WaveAmplitude = 1.6f;
    private const float WaveStep = 3f;
    private const float WaveMaxWidth = 600f;

    /// <summary>
    /// 波浪线基线相对「行下缘」的上移量。**收起态那个小圆点也用它** ——
    /// 圆点要落在波浪线的**起点**上（同一个 x 换算、同一个 y），这里各写一个 3f
    /// 就是「同一规则两处实现」，改一处就会让圆点与波浪线错开。
    /// </summary>
    private const float WaveBaseInset = 3f;

    private void DrawDiagnosticWave(ICanvas canvas, long lineIndex, float y, float textX, float lineH)
    {
        if (_filePath.Length == 0) return;
        List<Diagnostic> diags;
        try { diags = DiagnosticManager.GetForLine(_filePath, (int)lineIndex + 1); }
        catch { return; }
        if (diags.Count == 0) return;

        var worst = diags[0];
        foreach (var d in diags)
            if (d.Severity < worst.Severity) worst = d;

        var line = _doc?.GetLine(lineIndex) ?? "";
        int from = Math.Max(0, worst.Column - 1);
        float x0 = textX + MeasurePrefixWidth(line, from);
        float width = Math.Min(WaveMaxWidth, Math.Max(24f,
            MeasurePrefixWidth(line, line.Length) - MeasurePrefixWidth(line, from)));
        float baseY = y + lineH - WaveBaseInset;

        canvas.StrokeColor = EditorTypography.WaveColor(worst.Severity);
        canvas.StrokeSize = 1.2f;

        var path = new PathF();
        path.MoveTo(x0, baseY);
        bool up = true;
        for (float dx = WaveStep; dx <= width; dx += WaveStep)
        {
            path.LineTo(x0 + dx, up ? baseY - WaveAmplitude : baseY);
            up = !up;
        }
        canvas.DrawPath(path);
    }

    // ══════════════════════════════════════════════════════════════════
    // 编译诊断气泡（**画布绘制层**）
    //
    // 数据源**只有 DiagnosticManager**（与行下那条波浪线、页面上的错误列表同一份）——
    // 所以 ✕ 关掉一个气泡时，波浪线与列表一起消失，不会「两边各说各话」。
    //
    // **为什么画在画布上而不是叠一层控件**：它要跟代码一起滚、一起缩放、一起裁剪 ——
    // 那本来就是画布的坐标系。做成控件就得每帧把「视口坐标」再算一遍搬给布局
    // （原先 EditorPage 里的 RelayoutBubbles 干的正是这件事），于是同一个位置有了两把尺子。
    //
    // **模型：每条诊断一个「展开 / 收起」状态**（不是两套独立机制）：
    // · **展开** = 画气泡；**收起** = 在波浪线起点画一个小圆点（点圆点又能展开，是可逆的）；
    // · 设置里的总开关定的是**默认值**（开 = 默认展开、关 = 默认收起），不是「能不能显示」；
    // · 用户逐条的选择（✕ = 收起、点圆点 = 展开）**覆盖**默认值，且**切总开关时不清空**；
    // · 诊断换了一批（重新编译 / 换文件 ⇒ `DiagnosticManager.Inject` 整体替换）时逐条状态作废
    //   —— 那些 (行,列) 已经指向别的地方了。
    //
    // 几何规则（用户定的规格，逐条照做）：
    // ① **可见性看「诊断那一行」**：锚点行与视口无交集 ⇒ 整个跳过，一个像素都不画
    //    （气泡与收起态的小圆点都一样）。
    //    判据**不是**「气泡矩形在不在视口里」—— 那样会出现「错误行已经滚出屏幕、
    //    它的气泡还挂在屏幕上」；
    // ② **位置只由 (错误行, 错误列) 决定**：永远摆在错误行**正下方**、锚点右侧。
    //    **不做任何避让** —— 不翻到行上方、不夹进视口、不与别的气泡错开堆叠
    //    （两条错误行挨得近、气泡互相压住是允许的）；
    // ③ **同一 (行,列) 合并成一个气泡**，组内多条按 `1. …` `2. …` 往下排；
    // ④ **宽度固定** = 每行字数（设置里的 `MauiEditorStore.BubbleChars`）× 半角字宽，
    //    不随屏幕变、也不夹进视口（屏幕能横向滚，用户明确不要为宽度让步）；
    // ⑤ **高度按内容** = 折行后的行数 × 行高 + 上下内边距（**不设行数上限**）；
    // ⑥ 收起点那个小圆点画在**波浪线的起点**上（与 `DrawDiagnosticWave` 同一把尺子：
    //    同一个「起始列 → x」换算、同一个 y），颜色就是那一档的波浪色；
    // ⑦ 形状是**一体路径**：圆角矩形，但**左上角不收圆角、直接收成一个尖**，
    //    尖端落在锚点上（不是「圆角矩形 + 另画一个小三角」—— 那要多一个图形、还容易对不齐）；
    // ⑧ 内边距、圆角、尖、✕、圆点全部由字号推导（见 `EditorTypography` 的气泡几何那一节）。
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// **同一严重度**、同一 (行,列) 的一组诊断 —— 合并成**一个**气泡（组内多条排成 `1. …` / `2. …`）。
    ///
    /// ⚠ 分组键是 **(行, 列, 严重度)**：同一个位置上「又是错误又是警告」时**不合并到一起**，
    /// 而是拆成两个气泡上下挨着摆（用户定的：错误合并错误、警告合并警告）。
    /// </summary>
    private sealed class DiagGroup
    {
        /// <summary>本组的严重度（决定气泡底色）。枚举顺序即严重度：Error &lt; Warning &lt; Info。</summary>
        public Severity Severity;

        /// <summary>组内的原始诊断。</summary>
        public readonly List<Diagnostic> Items = [];

        /// <summary>已按显示列折好的正文行。</summary>
        public string[] Lines = [];
    }

    /// <summary>
    /// 一个 **(行,列) 位置**上的全部诊断 —— 按严重度分成若干组，组间**上下挨着**摆（错误在上）。
    ///
    /// **展开/收起状态是整个位置共用一个**（用户定的）：点 ✕ 收起该位置的**全部**气泡，
    /// 收成**一个小圆点**（颜色取该位置最严重的那档，即错误优先红）；点圆点再全部展开。
    /// 这正好与「状态表按 (行,列) 记」对得上 —— 见 <see cref="_bubbleOverrides"/>。
    /// </summary>
    private sealed class DiagSpot
    {
        public int Line;
        public int Column;

        /// <summary>该位置最严重的一档（收起态圆点的颜色、以及「谁在最上面」的顺序）。</summary>
        public Severity Severity;

        /// <summary>按严重度升序的组（Error → Warning → Info）—— 也就是**从上到下**的顺序。</summary>
        public readonly List<DiagGroup> Groups = [];
    }

    /// <summary>
    /// 一条诊断**算好的几何**（展开态 = 气泡、收起态 = 小圆点）—— 绘制与命中测试**共用这一份实例**。
    ///
    /// ⚠ 绝不在触摸处理里重算一遍：那是本仓库反复踩的「两把尺子」（看到的 ✕ 与点得中的 ✕ 错开）。
    /// 那个**小目标**（✕ / 圆点）的「画出来的形状」与「点得中的热区」是两个矩形（热区明显大一圈），
    /// 也在同一处算出来。
    /// </summary>
    private sealed class BubbleHit
    {
        /// <summary>这一小块属于哪个位置 —— 切换展开/收起时按它写状态（**整个位置一起切**）。</summary>
        public DiagSpot Spot = null!;

        /// <summary>本帧画的是展开态（气泡）还是收起态（小圆点）。</summary>
        public bool Expanded;

        /// <summary>展开态 = 气泡本体（同时是「点正文吞掉手势」的判据）；收起态 = 圆点那一小块。</summary>
        public RectF Body;

        /// <summary>
        /// 那个小目标的**命中热区**（比画出来的形状大一圈 —— 手指点不中 17dp 的方块 / 8dp 的圆点）。
        /// 它是**由画出来的那个矩形就地外扩**得到的（`Inflate(glyph, …)`，见 DrawBubble / DrawDot），
        /// 所以这里只留结果：绘制矩形与热区出自同一个表达式，不会各算一份而错开。
        /// </summary>
        public RectF ToggleTouch;
    }

    /// <summary>本帧算出的气泡/圆点几何（每帧重算）。触摸命中测试读的就是它。</summary>
    private readonly List<BubbleHit> _bubbleHits = [];

    /// <summary>
    /// 本帧所有气泡**最右缘的内容坐标**（不含横向滚动）—— 给横向滚动上限用
    /// （见 <see cref="ComputeMaxScrollX"/>：不把它算进去，气泡右侧的 ✕ 就永远滚不到）。
    /// </summary>
    private float _bubbleRightCodeX;

    /// <summary>
    /// **用户逐条的展开/收起选择**（值：true = 展开），键是分组键 (行, 列)。
    ///
    /// 没进这张表的条目走默认值（设置里的总开关）。**切总开关时不清空这张表**（规格要求：
    /// 逐条选择覆盖默认值），只在**诊断换了一批**时清（重建分组那一步，见 EnsureBubbleGroups）。
    /// </summary>
    private readonly Dictionary<(int Line, int Column), bool> _bubbleOverrides = [];

    // 气泡**内容**的缓存：文本折行与分组只在「诊断变了 / 每行字数变了」时重做。
    // 诊断表是**整体替换引用**的（见 SnapshotDiagnostics），所以比引用就够。
    //
    // ⚠ 键里**故意不含字号**：折行只按「每行几个字符」（`MauiEditorStore.BubbleChars`）算，
    // 与字号无关；而所有像素级的尺寸都是每帧现读 `EditorTypography` 算的 ⇒
    // **捏合缩放时不需要重建任何东西**（这正是「画在画布上」比「叠一层控件」好的地方：
    // 老实现每改一次字号都要 `RebuildBubbles()` 把整批视图重建一遍）。
    private List<DiagSpot>? _bubbleSpots;
    private List<Diagnostic>? _bubbleSource;
    private int _bubbleCols;

    /// <summary>
    /// 分组 + 折行（**纯内容**，与视口、与字号都无关）。内容没变就复用上一次的结果。
    /// </summary>
    private List<DiagSpot> EnsureDiagSpots(List<Diagnostic> all)
    {
        int cols = MauiEditorStore.BubbleChars;

        // 无诊断时每帧拿到的可能都是**新的空表**（DiagnosticManager.GetAll 找不到就返回新 List），
        // 所以这种情况单独认「上次也是空」——否则每帧都要重建一次空表。
        bool sameSource = all.Count == 0
            ? _bubbleSpots is not null && _bubbleSpots.Count == 0 && _bubbleSource is null
            : ReferenceEquals(_bubbleSource, all);
        if (_bubbleSpots is not null && sameSource && _bubbleCols == cols) return _bubbleSpots;

        // **诊断换了一批** ⇒ 展开/收起状态作废：那张表按 (行,列) 记，
        // 而新的一批诊断里同样的 (行,列) 指的已经是别的东西了（换文件、重新编译都是这条路）。
        // ⚠ 改「每行字数」**不算**换了一批 —— 那条路要保住用户的选择。
        if (!sameSource) _bubbleOverrides.Clear();

        _bubbleCols = cols;
        _bubbleSource = all.Count == 0 ? null : all;
        _bubbleSpots = BuildDiagSpots(all, cols);
        return _bubbleSpots;
    }

    /// <summary>
    /// 按 **(行, 列, 严重度)** 分组，再按 (行,列) 收成「位置」。**纯函数**。
    ///
    /// 同一位置上的**错误与警告是两个气泡**（用户定的：错误合并错误、警告合并警告），
    /// 位置上按严重度升序排 ⇒ **错误在上、警告在下、提示更下**（严重的优先）。
    /// 这个顺序就是屏幕上的上下顺序，想调就调这里那句 Sort。
    /// </summary>
    private static List<DiagSpot> BuildDiagSpots(List<Diagnostic> all, int cols)
    {
        var map = new Dictionary<(int Line, int Column), DiagSpot>();
        foreach (var d in all)
        {
            var key = (d.Line, d.Column);
            if (!map.TryGetValue(key, out var spot))
            {
                spot = new DiagSpot { Line = d.Line, Column = d.Column, Severity = d.Severity };
                map[key] = spot;
            }
            if (d.Severity < spot.Severity) spot.Severity = d.Severity;   // 位置最严重的那档

            // 同一严重度合成一组（组内几条 → 同一个气泡里的 `1. …` / `2. …`）
            var g = spot.Groups.Find(x => x.Severity == d.Severity);
            if (g is null) { g = new DiagGroup { Severity = d.Severity }; spot.Groups.Add(g); }
            g.Items.Add(d);
        }

        var spots = new List<DiagSpot>(map.Count);
        foreach (var kv in map) spots.Add(kv.Value);
        // 稳定顺序（按行、再按列）：Dictionary 的遍历顺序是实现细节，而绘制顺序应当可预期
        spots.Sort((a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Column.CompareTo(b.Column));

        foreach (var spot in spots)
        {
            // 上下顺序 = 严重度升序（Error 0 → Warning 1 → Info 2）⇒「错误在最上面」
            spot.Groups.Sort((a, b) => a.Severity.CompareTo(b.Severity));
            foreach (var g in spot.Groups) WrapGroup(g, cols);
        }
        return spots;
    }

    /// <summary>把一个严重度组的正文按显示列折好（单条带「第 N 行：」、多条编号）。</summary>
    private static void WrapGroup(DiagGroup g, int cols)
    {
        var lines = new List<string>();
        for (int i = 0; i < g.Items.Count; i++)
        {
            var d = g.Items[i];

            // 正文**不带**那个方括号里的英文错误码（用户要求：`[Parser_UnexpectedToken]` 与消息
            // 意思重复）。⚠ **只改气泡**：`Diagnostic.Code` 本身、以及错误列表里对它的显示都不动。
            // 气泡宽度与这条无关 —— 它恒等于「每行字数 × 半角字宽」，不随正文长短伸缩。

            // 单条 → 保持改造前的观感（带上「第 N 行：」）；多条 → 用户要的 `1. …` / `2. …` 编号。
            // 编号占掉的列数要从**可用列**里扣掉，否则第一行会顶出气泡宽度
            // （续行因此比气泡窄几个列宽，宁可短一点也不许顶出去）。
            string text = g.Items.Count == 1
                ? (d.Line > 0 ? $"第 {d.Line} 行：{d.Message}" : d.Message)
                : $"{i + 1}. {d.Message}";
            int prefix = g.Items.Count == 1 ? 0 : $"{i + 1}. ".Length;

            foreach (var ln in WrapByColumns(text, cols - prefix).Split('\n'))
                if (ln.Length > 0) lines.Add(ln);   // 原文本里的空行不占高度
        }
        g.Lines = lines.Count > 0 ? [.. lines] : [""];
    }

    /// <summary>
    /// 按**显示列数**硬折行 —— 每行最多 <paramref name="maxCols"/> 列（全角算 2 列）。
    ///
    /// 为什么要自己折、而不是交给平台排版：平台的折行是**按控件宽度**算的，
    /// 而这里要的是**按字符数**（"一行超过 32 字符就换行"）。两者只在字宽恰好均匀时才等价，
    /// 而气泡里常混着中英文 —— 交给平台折，换行位置会随字号、字体、取整各处漂移，
    /// 于是"每行 32 字"这条规矩根本立不住。
    ///
    /// 宽度真源用 <c>AnsiString.CharWidth</c>（全仓唯一那份，别在这儿另写一张表
    /// —— 本仓为"同一规则两处实现"付过很多次代价）。
    /// </summary>
    private static string WrapByColumns(string text, int maxCols)
    {
        if (maxCols < 1 || text.Length == 0) return text;
        var sb = new StringBuilder(text.Length + 16);
        int col = 0;
        // ⚠ 按**码点**遍历（`EnumerateRunes`）而不是 `foreach (char)`：
        //   emoji / CJK 扩展 B 是 UTF-16 代理对（两个 char），逐 char 走会把它们拆开
        //   —— 既是本仓明令禁止的（字符串处理一律按 Rune），`CharWidth` 本身也只收 `Rune`。
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\n') { sb.Append('\n'); col = 0; continue; }
            int w = WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(rune);
            if (col + w > maxCols) { sb.Append('\n'); col = 0; }
            sb.Append(rune.ToString());
            col += w;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 画诊断标记（**展开 = 气泡 / 收起 = 小圆点**）—— 由 <see cref="Draw"/> 在
    /// **代码、波浪线、手柄、行号栏之后**调用（最上层；只有滚动条与调试 HUD 压在它上面）。
    ///
    /// 每条标记的几何只由它自己那条错误行 + 它自己的展开/收起状态决定；
    /// 本函数顺带把几何记进 <see cref="_bubbleHits"/> 供触摸命中测试使用
    /// （**同一处算出来**，见 <see cref="BubbleHit"/>）。
    /// </summary>
    private void DrawDiagnosticBubbles(ICanvas canvas, List<Diagnostic> all,
        float w, float h, float lineH)
    {
        _bubbleHits.Clear();
        _bubbleRightCodeX = 0f;
        var spots = EnsureDiagSpots(all);
        if (spots.Count == 0) return;

        // 所有像素尺寸**每帧现读**（全部由 `EditorTypography.FontSize` 推导）⇒ 捏合缩放自动跟随
        float halfW = Math.Max(1f, _charWidth);          // 半角字宽（与定位同一把尺子）
        int cols = MauiEditorStore.BubbleChars;
        float pad = EditorTypography.BubblePad;
        float tipW = EditorTypography.BubbleTipWidth;
        float tipH = EditorTypography.BubbleTipHeight;
        float closeW = EditorTypography.BubbleCloseSize;
        float dotR = EditorTypography.BubbleDotRadius;
        float inflate = EditorTypography.BubbleHitInflate;
        float textW = cols * halfW;
        float bodyW = textW + pad * 2f + closeW;
        float radius = EditorTypography.BubbleRadius;
        var textColor = EditorTypography.BubbleTextColor;
        // 默认展开还是默认收起由**设置里的总开关**定；用户逐条的选择覆盖它（见 _bubbleOverrides）
        bool defaultExpanded = MauiEditorStore.DefaultExpandBubbles;

        // 行号栏刚把字号减 1（见 DrawGutter），气泡正文要还原成与代码同号（用户要求「一般大」）
        canvas.FontSize = EditorTypography.FontSize;

        foreach (var spot in spots)
        {
            // ── 锚点 ──
            // **锚不上就一个像素都不画**（用户 2026-09-24 定的两条，合起来把「锚不上」收干净了）：
            //   · 「没有位置的（行列都没有的），不要显示气泡」；
            //   · 「假如错误或警告超出范围，也不要显示，比如程序就 10 行，报错 -1 行或者 100 行，
            //      就不要显示气泡了，完全就是胡说八道的」。
            //
            // 理由是一样的：气泡上那个尖的**全部意义就是指向某一行**。指不到任何一行时，
            // 画出来的只能是一块**贴在视口顶部的浮层** —— 挡在代码上，却和它说的那句话毫无关系。
            // 实测触发形态：链接器那条「库代码里有 N 个未解析标签」，一个 7 行、编得过跑得动的
            // Pascal 程序上顶着盖住第 1~3 行的黄框。越界那条更没得辩（10 行的文件报 100 行）。
            //
            // ⚠ 它们**照旧留在编辑器的「错误列表」里**（那是文字行、不遮挡代码），所以
            //   「编译器到底说了什么」并没有丢 —— 那正是这里从前有一支「贴到顶部」兜底的理由。
            //
            // ⚠ 不能写成 `bool anchored = cond && TryGetCellAnchor(out …)`：`&&` 的右侧不保证求值，
            //   编译器会判 out 变量「可能未赋值」（CS0165）。分开写。
            float anchorX = 0f, lineTop = 0f;
            if (spot.Line <= 0 ||
                !TryGetCellAnchor(spot.Line, spot.Column > 0 ? spot.Column : 1, out anchorX, out lineTop))
                continue;
            // **可见性判据 = 诊断那一行在不在视口里**（不是气泡/圆点的矩形）：
            // 错误行滚出屏幕 ⇒ 它的标记一个像素都不画（气泡与小圆点都是一条规矩）。
            if (lineTop + lineH <= 0f || lineTop >= h) continue;
            // **永远在错误行正下方**（用户定的）：尖就落在那一行的下缘，绝不翻到上方。
            float tipY = lineTop + lineH;

            // 展开/收起是**整个位置共用**的（用户定的）⇒ 一个位置上要么全是气泡、要么只有一个小圆点；
            // 圆点的颜色取该位置**最严重**的那一档（错误优先红）。
            bool expanded = _bubbleOverrides.TryGetValue((spot.Line, spot.Column), out var ov)
                ? ov : defaultExpanded;

            if (!expanded)
            {
                DrawDot(canvas, spot, EditorTypography.WaveColor(spot.Severity),
                    anchorX, lineTop, lineH, dotR, inflate, w, h);
                continue;
            }

            // 同一位置的多组**上下挨着**摆：最上面那个的尖接锚点，下面那些紧挨着它往下排。
            // 只有最上面那个画尖 —— 尖是「指向锚点」的，下面那个的尖会指进上一个气泡里去。
            //
            // `y` 记的是**传给 DrawBubble 的那个 tipY**（= 有尖时是尖端、无尖时就是本体上缘）——
            // 两件事混在一个变量里，所以推进时必须先算出**本体的上缘**再往下走。
            // 这里曾写成 `y += bodyH + (withTip ? tipH : gap)`，看似等价，实际把「尖高的补偿」
            // 与「两个气泡之间的缝」混成了一条式子：无尖那一支用的是 gap，可
            // `BubblePath` 那边又**无条件**加了 tipH ⇒ 叠在下面的气泡被多推了一整个尖高
            // （13 号字 7.8px、15 号字 9px），而它本意只要 gap。现在两边都按「本体上缘」算。
            float y = tipY;
            for (int i = 0; i < spot.Groups.Count; i++)
            {
                var g = spot.Groups[i];
                // 锚不上的诊断走不到这里（上面就 `continue` 了）⇒ 最上面那个恒带尖。
                bool withTip = i == 0;
                float bodyH = g.Lines.Length * lineH + pad * 2f;

                // 底色**按严重度取**（`BubbleFill` 内部再分白天/夜间两套显式值）——
                // 不再传波浪线那档饱和色：白天要的是同色系的浅色调，不是它。
                // 字色**跟随主题**（`BubbleTextColor`，夜间浅字/日间深字）—— 六档底色都已按
                // 「与这一档字色 ≥4.5:1」调过，所以这里不必也不能按单个气泡的底色去挑。
                DrawBubble(canvas, spot, g, EditorTypography.BubbleFill(g.Severity), withTip, anchorX, y,
                    pad, tipW, tipH, closeW, inflate, bodyW, radius, textColor, lineH, w, h);

                // 下一个的本体上缘 = 这个的下边缘 + 一道缝（带尖的那个，本体从 tipY+tipH 起算）
                float bodyTop = withTip ? y + tipH : y;
                y = bodyTop + bodyH + EditorTypography.BubbleStackGap;
            }
        }
    }

    /// <summary>
    /// 展开态的一个气泡：一体路径（最上面那个带尖、被压在下面的用普通圆角矩形）
    /// + 正文 + 右上角的 ✕。
    ///
    /// <paramref name="withTip"/> = 是否画左上角那个尖：同一位置上**只有最上面那个画尖**
    /// （尖的用途是「指向锚点」，下面那个的尖会指进上一个气泡里去，不合理）。
    /// 传 false 时 <paramref name="tipY"/> 就是**本体的上边缘**（尖的那段高度不参与）。
    /// </summary>
    private void DrawBubble(ICanvas canvas, DiagSpot spot, DiagGroup g, Color fill, bool withTip,
        float bx, float tipY, float pad, float tipW, float tipH, float closeW, float inflate,
        float bodyW, float radius, Color textColor, float lineH, float w, float h)
    {
        float bodyH = g.Lines.Length * lineH + pad * 2f;
        float bodyTop = withTip ? tipY + tipH : tipY;   // 上边缘（带尖时尖在它上面 tipH 处）

        // 一体路径：圆角矩形，**左上角不收圆角、直接收成一个尖**
        // （不是「圆角矩形 + 另画一个小三角」：那要多一个图形，还要处理两者的对齐/接缝）。
        // 填充与描边**用同一个 path**（先 fill 再 stroke）—— 两者于是严丝合缝，
        // 不会出现「描边把尖啃掉一块」那种毛刺。
        // 记下最右缘（**内容坐标** = 视口坐标 + 横向滚动）供横向滚动上限用 —— 见 _bubbleRightCodeX
        _bubbleRightCodeX = MathF.Max(_bubbleRightCodeX, bx + _scrollX + bodyW);

        var path = BubblePath(bx, tipY, bodyW, bodyH, tipW, tipH, radius, withTip);
        canvas.FillColor = fill;
        canvas.FillPath(path);
        // 细描边：把气泡与同色系的代码分开（没它时两者容易糊在一起）。颜色取主题边框色系。
        canvas.StrokeColor = EditorTypography.BubbleStroke;
        canvas.StrokeSize = EditorTypography.BubbleStrokeSize;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);

        // 正文（字号 = 编辑器字号；颜色跟随主题，见 EditorTypography.BubbleTextColor）
        float textX = bx + pad;
        for (int i = 0; i < g.Lines.Length; i++)
            DrawPlainText(canvas, g.Lines[i], textColor, textX,
                bodyTop + pad + i * lineH, lineH, EditorTypography.FontSize);

        // ✕（右上角）：画出来的笔迹在 `glyph` 里（内缩三分之一），
        // 命中热区是它外扩一圈的方块 —— **两者都在这里算**，触摸处理直接用。
        var glyph = new RectF(bx + bodyW - pad - closeW, bodyTop + pad, closeW, closeW);
        canvas.StrokeColor = textColor;
        canvas.StrokeSize = MathF.Max(1.5f, EditorTypography.FontSize * 0.12f);
        canvas.StrokeLineCap = LineCap.Round;
        float inset = closeW / 3f;
        canvas.DrawLine(glyph.X + inset, glyph.Y + inset, glyph.Right - inset, glyph.Bottom - inset);
        canvas.DrawLine(glyph.Right - inset, glyph.Y + inset, glyph.X + inset, glyph.Bottom - inset);

        _bubbleHits.Add(new BubbleHit
        {
            Spot = spot,
            Expanded = true,
            // 本体矩形含左上角那个尖（tipH 那一段），这样「点在尖上」也算点在气泡上
            Body = new RectF(bx, tipY, bodyW, withTip ? bodyH + tipH : bodyH),
            ToggleTouch = Inflate(glyph, inflate, w, h),
        });
    }

    /// <summary>
    /// 收起态：**一个小圆点，画在波浪线的起点上**。
    ///
    /// 「波浪线起点」= 该诊断起始列换算出来的 x（`MeasurePrefixWidth`，与 <see cref="DrawDiagnosticWave"/>
    /// 同一个换算）与那一条波浪线的 y —— **同一把尺子**，所以圆点正好落在波浪线头上，
    /// 不会与它错开。颜色就是那一档的波浪色（错误红 / 警告黄 / 提示绿）。
    /// 点它就把这一条**展开**（可逆 —— 与「彻底删掉这条诊断」不是一回事）。
    /// </summary>
    private void DrawDot(ICanvas canvas, DiagSpot spot, Color wave,
        float anchorX, float lineTop, float lineH, float dotR, float inflate, float w, float h)
    {
        // y 与 DrawDiagnosticWave **同源**（那边是 `y + lineH - WaveBaseInset`）：
        // 圆点于是正好落在波浪线的头上。
        // ⚠ 这里**不再有** `anchored ? … : 贴视口顶部` 那一支：锚不上的诊断根本走不到这儿
        //   （`DrawDiagnosticBubbles` 在取锚点时就 `continue` 掉了），所以 `lineTop` 恒可信。
        float cy = lineTop + lineH - WaveBaseInset;

        // 圆点的右缘也记进内容最右缘（与气泡同理）：锚点列偏右时，横向滚得到才点得着。
        _bubbleRightCodeX = MathF.Max(_bubbleRightCodeX, anchorX + _scrollX + dotR);

        canvas.FillColor = wave;
        canvas.FillCircle(anchorX, cy, dotR);

        var box = new RectF(anchorX - dotR, cy - dotR, dotR * 2f, dotR * 2f);
        _bubbleHits.Add(new BubbleHit
        {
            Spot = spot,
            Expanded = false,
            // 收起态没有「本体」可言：那一块就是圆点的热区，点它就是展开
            Body = Inflate(box, inflate, w, h),
            ToggleTouch = Inflate(box, inflate, w, h),
        });
    }

    /// <summary>把一个小目标的绘制矩形外扩成**命中热区**，并收进画布内。</summary>
    private static RectF Inflate(RectF r, float inflate, float w, float h)
        => ClampToCanvas(new RectF(r.X - inflate, r.Y - inflate,
            r.Width + inflate * 2f, r.Height + inflate * 2f), w, h);

    /// <summary>
    /// 气泡轮廓 —— 圆角矩形，**左上角不收圆角、直接收成一个尖**（尖端 = <paramref name="tipX"/>,
    /// <paramref name="tipY"/>），其余三个角照旧圆角。
    ///
    /// 为什么要一个「尖」而不是「圆角矩形 + 另画的三角尾巴」：尾巴是**居中挂在气泡边上**的，
    /// 要向左伸出半个尾巴宽 —— 而错误在第 1 列时锚点已经在正文最左，**没有向左伸的余地**
    /// （要么尾巴被裁掉，要么把气泡右移、于是尖端又对不准）。尖是气泡自己的左上顶点，
    /// 气泡只向右展开，天然没有这个问题；顺带还少画一个图形、不会出现「尾巴与气泡对不齐」。
    ///
    /// <paramref name="withTip"/>=false（诊断没有行列信息，以及**堆叠时被压在下面那些**）
    /// 时退化成**普通圆角矩形 —— 四个角都是圆的**。那时 <paramref name="tipY"/> 就是
    /// **本体的上边缘**（尖的那段高度不参与），与 <see cref="DrawBubble"/> 的契约一致。
    ///
    /// ⚠ 无尖那一支曾有两个毛病（都是「以为对方会做」造成的）：
    /// ① `y0 = tipY + tipH` **无条件**加尖高 —— 而调用方（<see cref="DrawDiagnosticBubbles"/>
    /// 的堆叠循环）与它自己的注释都假设「无尖时上缘 == tipY」⇒ 叠在下面的气泡**凭空多让出
    /// 一整个尖高**（13 号字 7.8px、15 号字 9px），而它本意只要 <see cref="EditorTypography.BubbleStackGap"/>；
    /// ② 左上角**直接从 <c>(x0, y0)</c> 起步**（那句 `MoveTo` 是给尖用的），于是四个角只圆了三个 ——
    /// 用户真机看到的就是「下面那个气泡左上角是直角」。
    /// </summary>
    private static PathF BubblePath(float tipX, float tipY, float bodyW, float bodyH,
        float tipW, float tipH, float r, bool withTip)
    {
        float x0 = tipX, x1 = tipX + bodyW;
        // 无尖 ⇒ 本体上缘就是 tipY（尖那一段根本不存在，别加它的高度）
        float y0 = withTip ? tipY + tipH : tipY;
        float y1 = y0 + bodyH;
        r = Math.Max(0f, Math.Min(r, Math.Min(bodyW, bodyH) / 2f));

        var p = new PathF();
        if (withTip)
        {
            float jx = x0 + tipW, jy = y0;             // 斜边与上边缘的交点
            // 交点处用一小段圆弧过渡（半径取尖高的 40%，并夹在斜边长度内）——
            // 两条边直接相交会在那儿留一个**折角**，放大字号时看得出来是个毛刺。
            float diag = MathF.Sqrt(tipW * tipW + tipH * tipH);
            float rj = MathF.Min(tipH * 0.4f, diag * 0.45f);
            float ux = diag > 0.01f ? (jx - x0) / diag : 1f;   // 斜边方向单位向量
            float uy = diag > 0.01f ? (jy - tipY) / diag : 0f;

            p.MoveTo(x0, tipY);                        // 尖端（锚点：错误格左缘 × 该行下缘）
            p.LineTo(jx - ux * rj, jy - uy * rj);      // 斜边
            p.QuadTo(jx, jy, jx + rj, jy);             // 交点的圆滑过渡 → 上边缘
        }
        else
        {
            p.MoveTo(x0 + r, y0);                      // 左上角**也**要圆（起点让出半径）
        }
        p.LineTo(x1 - r, y0);                          // 上边缘
        p.QuadTo(x1, y0, x1, y0 + r);                  // 右上角
        p.LineTo(x1, y1 - r);                          // 右边缘
        p.QuadTo(x1, y1, x1 - r, y1);                  // 右下角
        p.LineTo(x0 + r, y1);                          // 下边缘
        p.QuadTo(x0, y1, x0, y1 - r);                  // 左下角
        if (withTip)
        {
            p.LineTo(x0, y0);                          // 左边缘（上端与尖的斜边相连）
        }
        else
        {
            p.LineTo(x0, y0 + r);                      // 左边缘
            p.QuadTo(x0, y0, x0 + r, y0);              // 左上角（与另外三角同一份 r）
        }
        p.Close();
        return p;
    }

    /// <summary>把一个矩形收进画布内（气泡溢出屏幕时，✕ 的热区不该跟到画布外面去）。</summary>
    private static RectF ClampToCanvas(RectF r, float w, float h)
    {
        float x0 = Math.Clamp(r.X, 0f, w), y0 = Math.Clamp(r.Y, 0f, h);
        float x1 = Math.Clamp(r.Right, 0f, w), y1 = Math.Clamp(r.Bottom, 0f, h);
        return new RectF(x0, y0, Math.Max(0f, x1 - x0), Math.Max(0f, y1 - y0));
    }

    /// <summary>
    /// 按下的点是不是落在某个**小目标**上 —— 展开态的 ✕ / 收起态的小圆点。
    /// 命中几何来自上一帧绘制（见 <see cref="_bubbleHits"/>），**不在触摸里另算一份**。
    /// </summary>
    private BubbleHit? HitDiagToggle(float x, float y)
    {
        foreach (var b in _bubbleHits)
            if (b.ToggleTouch.Contains(x, y)) return b;
        return null;
    }

    /// <summary>
    /// 按下的点是不是落在气泡**正文**上（点正文不切换状态，但手势要吞掉）。
    ///
    /// 点正文**不**关气泡：正文正是用户要读的东西，长报错尤其容易误触。
    /// </summary>
    private bool HitBubbleBody(float x, float y)
    {
        foreach (var b in _bubbleHits)
            if (b.Expanded && b.Body.Contains(x, y)) return true;
        return false;
    }

    /// <summary>
    /// 把一个**位置**切成展开（气泡）或收起（小圆点）—— **点 ✕ 与点圆点都走这里**，
    /// 于是「展开/收起」这条规则只有一处实现。
    ///
    /// ⚠ 状态按**位置**（行,列）记、不按单个气泡：同一位置上错误/警告是两个气泡，
    /// 点其中任何一个的 ✕ 都把**整个位置**收成一个小圆点（用户定的语义）。
    ///
    /// ⚠ 这里**不动** `DiagnosticManager`：那条诊断仍然存在（行下波浪线照画、错误列表照列），
    /// 只是它在编辑器里的标记换了个形态。这正是「✕ 不是删掉、而是收起来」的落地方式 ——
    /// 所以它**可逆**（点那一点小圆点就能再展开）。
    /// </summary>
    private void SetDiagExpanded(DiagSpot spot, bool expanded)
    {
        _bubbleOverrides[(spot.Line, spot.Column)] = expanded;

        // 几何从「气泡」变成「小圆点」（或反过来），命中表必须作废：留着的话紧接着的第二次点击
        // 会按**旧几何**再切一次（那会儿那块位置可能已经换成了别的诊断）。
        _bubbleHits.Clear();
    }

    // ── 行渲染（带缓存的唯一实现）──

    private LineRuns GetLineRuns(long index, string line)
    {
        if (_lineCache.TryGetValue(index, out var cached)) return cached;

        var runs = BuildLineRuns(line);
        if (_lineCache.Count >= MaxCachedLines)
        {
            // 简单 FIFO 淘汰：滚动时被淘汰的正好是最久没看的那批
            int drop = Math.Min(64, _cacheOrder.Count);
            for (int i = 0; i < drop; i++)
            {
                long victim = _cacheOrder[i];
                if (_lineCache.TryGetValue(victim, out var old))
                {
                    DisposeLine(old);   // 淘汰也要释放排版（原生对象不归 GC 的托管堆管）
                    _lineCache.Remove(victim);
                }
            }
            _cacheOrder.RemoveRange(0, drop);
        }
        _lineCache[index] = runs;
        _cacheOrder.Add(index);
        return runs;
    }

    /// <summary>把一行切成「可直接绘制」的段：段文本 + 段起点列号 + 段带色对象（见 <see cref="LineRuns"/>）。</summary>
    /// <summary>
    /// 把一行裁到「当前横向可见的那几列」，返回 (片段, 片段起点的 x)。
    /// 只给**超长行**用（普通行走按行号缓存的正路）。
    /// </summary>
    private (string Text, float X) ClipToViewport(string line, float textX)
    {
        float charW = Math.Max(1f, _charWidth);
        int skip = _scrollX <= 0 ? 0 : (int)(_scrollX / charW);
        int cols = (int)((Width - textX) / charW) + 4;
        if (cols <= 0) return ("", textX);

        int start = Math.Clamp(skip, 0, Math.Max(0, line.Length - 1));
        int len = Math.Min(cols, line.Length - start);
        if (len <= 0) return ("", textX);
        return (line.Substring(start, len), textX + start * charW);
    }

    private LineRuns BuildLineRuns(string line)
    {
        var display = TextEditorMath.ExpandTabs(line, EditorTypography.TabColumns);
        if (display.Length == 0) return LineRuns.Empty;   // 空行：什么都不画

        // 超长行跳过分词：minified 行上跑 tokenizer 会把一帧拖到几百毫秒，
        // 而且这类行本来也没什么「语法」可高亮。
        // （普通路径下这类行在 Draw 里就被 ClipToViewport 接走了，这里是兜底。）
        if (line.Length > EditorTypography.MaxTokenizeChars) return Single(display);

        var tokens = _syntax?.Tokenize(display) ?? [];
        var runs = new List<IAttributedTextRun>(tokens.Count);
#if WINDOWS
        var segs = new List<(int Start, int Length, Color Color)>(tokens.Count);   // Windows 逐段自绘用
#endif
        int offset = 0;
        foreach (var (text, color) in tokens)
        {
            if (text.Length == 0) continue;

            // ⚠ run 的范围**必须夹在文本长度之内**：Android 端会把它直接喂给
            // SpannableString.setSpan，越界就是 IndexOutOfBoundsException 直接崩（不是渲染异常）。
            // 而 tokenizer 并不保证总长等于源文本 —— 空行时它返回的是一个空格 token，
            // 源文本长度却是 0，于是 (0,1) 越界。这条只在真机原生层才暴露，自测覆盖不到。
            int len = Math.Min(text.Length, display.Length - offset);
            if (len <= 0) break;

            runs.Add(new AttributedTextRun(offset, len, new TextAttributes
            {
                [TextAttribute.Color] = MarkupToFormattedString.ColorForToken(color, _isDark).ToHex(),
                // ⚠ **这里刻意不写 FontName**。写了的话 MAUI 会把它变成 Android 的
                // `TypefaceSpan(族名)` —— 那个 API 只认**系统字体族名**、没有 asset 重载
                // （见 `Graphics/Platforms/Android/Text/AttributedTextExtensions.cs`），
                // 我们的资产名喂进去解析不到，只会**静默回落成平台默认的比例字体**：
                // 中文与拉丁的宽度比就不再是 2:1，而测量那边量的是打包字体 ⇒ 越往右越偏。
                //
                // 不给 FontName，run 就没有 TypefaceSpan，布局回落用 `FontPaint` 的字体，
                // 而那正是 `canvas.Font`（= EditorTypography.CanvasFont）：它走的是
                // `FontExtensions.ToTypeface` 的 **`CreateFromAsset` 分支**，打包字体在这里能加载。
                // 于是「绘制用的字体」与「测量的字体」是同一个 —— 这才是同源。
            }));
#if WINDOWS
            segs.Add((offset, len, MarkupToFormattedString.ColorForToken(color, _isDark)));
#endif
            offset += len;
        }

        // 一个色 run 都没切出来（无分词器 / tokenizer 不给内容）：整行素色画。
        var result = new LineRuns { Whole = new AttributedText(display, runs) };
#if WINDOWS
        result.Segments = segs;
#endif
        return result;
    }

    /// <summary>一整行作为无 run 的素文本（超长行的兜底路径）。</summary>
    private static LineRuns Single(string text)
        => new() { Whole = new AttributedText(text, []) };

    /// <summary>
    /// 画一行 —— **整行一次 <c>DrawText</c>**，语法色是它内部的多个 run。
    ///
    /// 早先这里是「按网格列逐段定位、一段一次 DrawText」（v0.96.118 的做法），那时是**对的**：
    /// 平台把每个字形的推进量**取整到整数**，而网格用精确的 0.5em，两者会越走越远
    /// （实测奇数号下一行 107 列能差出 53.5px）—— 逐段定位就是靠「每段重新按回网格」截断误差。
    ///
    /// 改成一次画完的依据是一条实测结论：**字号为偶数 ⇒ 半列宽是整数 ⇒ 平台的推进量与网格
    /// 逐字完全相等（4/6/8/12/16/28/30 号下整行偏差都是 0.00px）**。等式一成立就没有「两把尺子」，
    /// 整行交给平台排版与按网格定位是同一个结果，而调用次数从「一行十几个」降到 **1**
    /// —— 正文是滚动时唯一的大头，小字号下屏上几百行，这一下就是十几倍的差距。
    ///
    /// ⚠ 两条前提，**别单独推翻**：① <see cref="EditorTypography.FontSize"/> 只允许偶数
    /// （在那边的 setter 里夹住）；② run 上不写 <c>TextAttribute.FontName</c>
    /// （见 <see cref="BuildLineRuns"/> 的长注释）。任一条破了，这里就会「渲染按平台、光标按网格」。
    /// </summary>
    private void DrawLineRuns(ICanvas canvas, LineRuns runs, float textX, float y, float lineH)
    {
        if (runs.Whole is null) return;
        float baseline = y + EditorTypography.TextBaselineOffset;
#if WINDOWS
        // 【Windows 专用】Win2D 后端**根本没有实现** `DrawText(IAttributedText, …)` ——
        // `Microsoft.Maui.Graphics.Win2D.W2DCanvas` 里那一支就是一句
        // `throw new NotImplementedException()`（已反编译核对）。异常从 Win2D 的绘制回调里
        // 逃出去，被 WinUI 包成 stowed exception（事件日志 0xC000027B，故障模块
        // Microsoft.UI.Xaml.dll），**进程当场闪退** —— 症状就是「打开编辑器先卡死再闪退」。
        // Win2D 可用的只有两个 `DrawString` 重载，所以这里**逐色段自己定位绘制**。
        //
        // x 用的是**同一把尺子**（<see cref="AdvancePrefix"/>，与光标/点击命中同源），
        // 所以这不是「渲染一套、定位一套」——段起点与自绘光标落在同一个坐标系里。
        // 单段内部的字形间距由平台排版决定，与 Android 侧「实测推进量可加」是同一个前提。
        if (runs.Segments is null || runs.Segments.Count == 0)
        {
            canvas.FontColor = MarkupToFormattedString.ColorForToken(0, _isDark);   // 素色 = 默认正文色
            canvas.DrawString(runs.Whole.Text ?? "", textX, Win2DStringY(baseline), HorizontalAlignment.Left);
            return;
        }
        var display = runs.Whole.Text ?? "";
        foreach (var (start, len, color) in runs.Segments)
        {
            // run 范围越界就跳过（tokenizer 不保证总长等于源文本，Android 那边为此崩过）
            if (len <= 0 || start < 0 || start + len > display.Length) continue;
            canvas.FontColor = color;
            canvas.DrawString(display.Substring(start, len),
                textX + AdvancePrefix(display, start), Win2DStringY(baseline), HorizontalAlignment.Left);
        }
        return;
#else
#if ANDROID
        if (TryDrawCachedLayout(canvas, runs, textX, baseline)) return;
#endif
        canvas.DrawText(runs.Whole, textX, baseline, 1_000_000f, lineH);
#endif
    }

#if WINDOWS
    /// <summary>
    /// Win2D 的 <c>DrawString(value, x, y, …)</c> 把文字**顶边**放在 <c>y - FontSize</c>
    /// （<c>W2DCanvas</c>：`_rect.Y = y - FontSize` 且 `VerticalAlignment = Top`），而
    /// <c>DrawText</c> 是**左上角锚定** —— 两者差整整一个字号。要让 Windows 的文字落在与
    /// Android/iOS 相同的行位上，传下去的必须是「顶边 + 字号」。
    /// </summary>
    private static float Win2DStringY(float top) => top + EditorTypography.FontSize;
#endif

#if ANDROID
    /// <summary>
    /// 用**缓存的平台排版**画这一行，返回是否画成功（false ⇒ 调用方回退到 <c>canvas.DrawText</c>）。
    ///
    /// 存在的理由（真机实测）：`PlatformCanvas.DrawText` 的实现是
    /// <c>new SpannableString(...)</c> → 逐 run <c>SetSpan</c> → <c>new StaticLayout(...)</c>
    /// → <c>layout.Draw</c> → <c>Dispose()</c> —— **每次调用都从头排版一次，画完立刻销毁**。
    /// 一屏 55 行就是 55 次完整排版：实测字号 8 时正文段 **78ms/帧**（行号栏才 11.7ms、
    /// 底色 0.3ms —— 所以瓶颈就是「每行一次排版」这件事本身）。
    /// 编译好的排版与行内容、字号绑定，而这两个在滚动时都不变 ⇒ 完全可以留着复用，
    /// 每帧只剩一次 `layout.Draw`。
    ///
    /// **为什么能拿到原生画布**：`PlatformCanvas.Canvas` 是 public 的（`get => _canvas;`），
    /// 而 `CodeCanvasView.Draw` 收到的正是那个 `PlatformCanvas` ⇒ 在**同一张画布、同一个 z 位置**
    /// 上画，外层那条「底色 → 正文 → 行号栏 → 手柄 → HUD」的顺序一点不用动。
    /// （`CurrentState.FontPaint` 是 protected、`TextLayoutUtils` 是 internal，两者都够不到，
    ///  而本仓禁用反射 ⇒ 排版与 paint 只能自己按 MAUI 的源码逐字复刻，见下面两处引用。）
    ///
    /// **回退条件见 <see cref="CanCacheLayout"/>** —— 只认我们自己产出的「纯颜色 run」这一种形态，
    /// 形态一变就走平台原路（宁可慢，不可画错）。
    /// </summary>
    private static bool TryDrawCachedLayout(ICanvas canvas, LineRuns runs, float x, float y)
    {
        if (canvas is not Microsoft.Maui.Graphics.Platform.PlatformCanvas pc) return false;
        var native = pc.Canvas;
        if (native is null) return false;

        float size = EditorTypography.FontSize;
        if (runs.NativeLayout is null || Math.Abs(runs.NativeFontSize - size) > 0.01f)
        {
            if (!CanCacheLayout(runs.Whole!)) return false;
            DisposeLine(runs);
            var span = BuildSpannable(runs.Whole!);
            if (span is null) return false;
            runs.NativeSpan = span;
            runs.NativeLayout = new Android.Text.StaticLayout(span, BuildTextPaint(size),
                int.MaxValue, Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
            runs.NativeFontSize = size;
        }

        // 与 MAUI 的 DrawText 同一套落笔动作：Save → Translate(x, y) → Draw → Restore。
        // （`GetOffsetsToDrawText` 在 VerticalAlignment.Top 下就是原样返回 (x, y)，所以直接平移。）
        // Save/Restore 是必须的：外层给正文加过左右裁剪（裁掉行号栏那一列），平移不能带着裁剪一起丢。
        native.Save();
        native.Translate(x, y);
        runs.NativeLayout!.Draw(native);
        native.Restore();
        return true;
    }

    /// <summary>
    /// 这份 run 集合能不能安全地缓存排版 —— 只认「**只有颜色**」这一种形态。
    ///
    /// 我们自己产出的 run 只写 <c>TextAttribute.Color</c>（见 <see cref="BuildLineRuns"/>），
    /// 而 MAUI 的 <c>HandleFormatRun</c> 还会处理字体名/粗体/斜体/下划线/背景/上下标/删除线/列表
    /// 共 9 种 span —— 我们只复刻了颜色那一种。将来谁往 run 上加了别的属性而忘了同步这里，
    /// 缓存版就会**静默丢样式**（屏幕上只是「粗体不见了」，不会报错）。
    /// 与其留这个坑，不如判一下：出现任何非颜色属性就整个走平台原路 —— 慢一点，但一定对。
    /// </summary>
    private static bool CanCacheLayout(IAttributedText text)
    {
        foreach (var run in text.Runs)
        {
            var a = run.Attributes;
            if (a is null) continue;
            if (!string.IsNullOrEmpty(a.GetFontName())) return false;
            if (a.GetBold() || a.GetItalic() || a.GetUnderline()) return false;
            if (a.GetSubscript() || a.GetSuperscript()) return false;
            if (a.GetStrikethrough() || a.GetUnorderedList()) return false;
            if (!string.IsNullOrEmpty(a.GetBackgroundColor())) return false;
        }
        return true;
    }

    /// <summary>
    /// 按 MAUI 的 <c>AttributedTextExtensions.AsSpannableString</c> 复刻（那个类是 internal，够不到）。
    /// 只做颜色那一种 span —— 其余的由 <see cref="CanCacheLayout"/> 挡在外面。
    /// </summary>
    private static Android.Text.SpannableString? BuildSpannable(IAttributedText text)
    {
        if (string.IsNullOrEmpty(text.Text)) return null;
        var span = new Android.Text.SpannableString(text.Text);
        foreach (var run in text.Runs)
        {
            int start = run.Start;
            int end = start + run.Length;
            // 与 BuildLineRuns 同一条护栏：范围越界时 setSpan 抛 IndexOutOfBoundsException（原生崩溃）。
            if (start < 0 || end > text.Text.Length || start >= end) continue;

            var hex = run.Attributes?.GetForegroundColor();
            if (string.IsNullOrEmpty(hex)) continue;

            // `ToHex()` 的逆（run 上存的就是它）。解析不了就**整行**走平台原路 ——
            // 半行有颜色、半行没有，比慢一点难查得多。
            Microsoft.Maui.Graphics.Color parsed;
            try { parsed = Microsoft.Maui.Graphics.Color.FromArgb(hex); }
            catch { return null; }

            int argb = Android.Graphics.Color.Argb(
                (int)Math.Round(parsed.Alpha * 255),
                (int)Math.Round(parsed.Red * 255),
                (int)Math.Round(parsed.Green * 255),
                (int)Math.Round(parsed.Blue * 255));
            // ⚠ 这个 ctor 收的是 Android.Graphics.Color（绑定如此），不是 int ——
            // 直接喂 int 会被解析成 Parcel 重载，报「无法从 int 转换为 Android.OS.Parcel」。
            span.SetSpan(new Android.Text.Style.ForegroundColorSpan(new Android.Graphics.Color(argb)),
                start, end, Android.Text.SpanTypes.ExclusiveExclusive);
        }
        return span;
    }

    /// <summary>
    /// 复刻 `PlatformCanvasState.FontPaint` 的初始化（那份状态是 protected，够不到）：
    /// <c>new TextPaint(); SetARGB(1,0,0,0); AntiAlias = true; SetTypeface(font.ToTypeface() ?? Default)</c>，
    /// 加上 <c>FontSize</c> setter 里的 <c>TextSize = 字号 × ScaleX</c>。
    ///
    /// **`ScaleX` 恒为 1**：它只被 <c>canvas.Scale()</c> 改写（`PlatformCanvasState.Scale`），
    /// 而本控件从不调 <c>Scale</c>（`DisplayScale` 只用于 pattern bitmap，与此无关）——
    /// 所以 TextSize 就等于字号，与测量路径（`PlatformStringSizeService.GetStringSize` 也是
    /// `new TextPaint { TextSize = fontSize }` + 同一个 ToTypeface）**同源**。
    /// 这条等式是「渲染宽度 == 测量宽度」的前提，别想当然地往这里塞个 density 缩放。
    /// </summary>
    private static Android.Text.TextPaint BuildTextPaint(float fontSize)
    {
        var paint = new Android.Text.TextPaint();
        paint.SetARGB(1, 0, 0, 0);
        paint.AntiAlias = true;
        // 扩展方法在 Microsoft.Maui.Graphics.Platform 下（本文件没有该 using，全限定调用）
        paint.SetTypeface(
            Microsoft.Maui.Graphics.Platform.FontExtensions.ToTypeface(EditorTypography.CanvasFont)
            ?? Android.Graphics.Typeface.Default);
        paint.TextSize = fontSize;

        // ⚠ **必须开亚像素定位，否则光标会沿行漂进字里**（v0.96.137 的根因）。
        //
        // 不开这个标志时，平台在**定位**每个字形时会把推进量**取整到整数设备像素**；
        // 而 `GetStringSize`（= `Layout.GetLineWidth`）报的是**未取整**的小数 ——
        // 于是「测量的尺子」和「渲染的尺子」每字差一点点，**沿行累积**：
        // 实测（模拟器 420dpi、字号 14）排版报 18.375px，画出来的栅距却是**正好 18px**
        // （一行 40 个 H，相邻墨迹起点差全是 18）；到第 16 个字就差 6px、行尾差 15px
        // ⇒ 光标落在**字符格里**而不是格与格的边界上（用户实测「光标完全压在 P 上面」，
        // 且「**有些字号是可以的**」：`字号 × 0.5 × 屏幕密度` 恰好落在整数上时就不差）。
        //
        // 修法**不是**把我们的尺子也取整 —— 那等于替用户决定缩放的粒度，
        // 用户明确反对（「文字宽度不要取整，这样无法无极缩放」）。正解是**让渲染别再取整**：
        // 开了亚像素定位，平台画出来的推进量就是那个小数（18.375），与量到的**同源**，
        // 任何字号（含小数）都逐字对齐，而且字形定位本身也更精细。
        paint.SubpixelText = true;
        return paint;
    }
#endif

    /// <summary>
    /// 编辑中的那一行也走缓存 —— 它的文本只在**击键时**变，而先前是逐帧 <c>BuildAttributed</c>
    /// 重建（重新分词 + 重切段 + 重算列号）。整份文本作键，击键即失效、不动时零成本。
    /// </summary>
    private LineRuns EditingRuns(string line)
    {
        if (_editingRuns is not null && _editingRunsFor == line) return _editingRuns;
        DisposeLine(_editingRuns);   // 换掉的那份排版要释放
        _editingRuns = BuildLineRuns(line);
        _editingRunsFor = line;
        return _editingRuns;
    }

    private LineRuns? _editingRuns;
    private string? _editingRunsFor;

    /// <summary>清掉某行的渲染缓存（该行被编辑后调用）。</summary>
    public void InvalidateLine(long oneBased)
    {
        long idx = oneBased - 1;
        if (_lineCache.TryGetValue(idx, out var old))
        {
            DisposeLine(old);
            _lineCache.Remove(idx);
        }
        _cacheOrder.Remove(idx);
        _lineWidths.Remove(idx);        // 列数缓存与行内容绑定，一并失效
        DisposeEditingRuns();           // 编辑行的段几何也失效（文本可能正是这一行）
        Invalidate();
    }

    /// <summary>整份内容变了（撤销/重做/多行粘贴）——行数都可能变，缓存必须全清。</summary>
    public void InvalidateAll()
    {
        ClearLineCache();
        _lineWidths.Clear();
        DisposeEditingRuns();
        Invalidate();
    }

    /// <summary>丢掉编辑行的段几何缓存（连排版一起释放）。</summary>
    private void DisposeEditingRuns()
    {
        DisposeLine(_editingRuns);
        _editingRuns = null;
        _editingRunsFor = null;
    }

    /// <summary>
    /// 正在编辑的那一行（1-based；-1 = 无）。
    ///
    /// **这一行同样由画布自绘**（文字取自 <see cref="EditingText"/>、光标取自
    /// <see cref="EditingCursor"/>）。原先是让原生 <c>Entry</c> 显示这一行、画布跳过它，
    /// 结果是两层各按自己的规则算位置（画布用「行顶+基线补偿」，Entry 用它自己的内边距），
    /// 必然错位。现在 <c>Entry</c> 只作为**输入法通道**存在（文字透明），显示层只有一套。
    /// </summary>
    private long _editingLine = -1;

    /// <summary>
    /// 正在编辑的那一行（1-based；-1 = 没在编辑）。
    /// 进/出编辑态时顺带开关**光标闪烁**（见 <see cref="OnCaretBlink"/>）。
    /// </summary>
    public long EditingLine
    {
        get => _editingLine;
        set
        {
            if (_editingLine == value) return;
            _editingLine = value;
            if (value >= 0) StartCaretBlink();
            else StopCaretBlink();
        }
    }

    /// <summary>编辑行的当前文本（由页面的输入框实时同步过来）。</summary>
    public string? EditingText { get; set; }

    private int _editingCursor;

    /// <summary>
    /// 编辑行的光标位置（UTF-16 码元下标）。
    /// 光标一移动就把闪烁**重置成「亮」并重新计时** —— 与系统编辑器一致：
    /// 打字/移动之后应该立刻看得见光标，而不是运气不好正赶上「灭」的那半秒。
    /// </summary>
    public int EditingCursor
    {
        get => _editingCursor;
        set
        {
            if (_editingCursor == value) return;
            _editingCursor = value;
            RestartCaretBlink();
        }
    }

    // ── 光标闪烁 ──

    private IDispatcherTimer? _caretBlink;
    private bool _caretOn = true;

    /// <summary>闪烁半周期（ms）—— 与 Android/桌面编辑器一致，500ms 亮 / 500ms 灭。</summary>
    private const int CaretBlinkMs = 500;

    private void StartCaretBlink()
    {
        _caretOn = true;
        _caretBlink ??= Dispatcher.CreateTimer();
        _caretBlink.Interval = TimeSpan.FromMilliseconds(CaretBlinkMs);
        _caretBlink.Tick -= OnCaretBlink;
        _caretBlink.Tick += OnCaretBlink;
        _caretBlink.Start();
        Invalidate();
    }

    private void StopCaretBlink()
    {
        _caretBlink?.Stop();
        _caretOn = true;      // 下次进编辑态时从「亮」开始
    }

    /// <summary>把闪烁相位推回「亮」并重新计时（光标移动 / 打字时调）。</summary>
    private void RestartCaretBlink()
    {
        if (_editingLine < 0 || _caretBlink is null) return;
        _caretOn = true;
        _caretBlink.Stop();
        _caretBlink.Start();
        Invalidate();
    }

    private void OnCaretBlink(object? sender, EventArgs e)
    {
        _caretOn = !_caretOn;
        // 只重画，不改内容 —— 用非节流的 Invalidate：闪烁本身就是「这一帧必须画」
        Invalidate();
    }

    /// <summary>行号栏宽度（pt）——浮动的输入框左边界要对齐到它。</summary>
    public float GutterWidthPx => GutterWidth();

    /// <summary>一行在屏幕上的 y 偏移（pt，相对画布顶部）；不在视口内返回 null。</summary>
    public float? LineScreenY(long oneBased, float canvasHeight)
    {
        float y = LineY(oneBased - 1, EditorTypography.LineHeight);
        return y + EditorTypography.LineHeight < 0 || y > canvasHeight ? null : y;
    }

    // ── 调试 HUD ──

    /// <summary>调试开关：显示可见行区间 / 帧耗时 / 缓存命中 / 索引进度 / 等宽自检。</summary>
    public bool ShowDebugHud { get; set; }

    private readonly Stopwatch _drawWatch = new();

    // ── 分段耗时（本帧，ms）—— 定位「一帧到底花在哪」用 ──
    //
    // 起因：真机（小米 13）上字号 8 滑动，`峰` 116ms / 稳态 ~32ms，而 gfxinfo 的 GPU 只占 2ms
    // ⇒ 瓶颈 100% 在 CPU 的绘制路径。但总量看不出该改哪儿：行号栏和正文各自都是
    // 「每可见行一次平台文本绘制」（`DrawText` 每次新建 `StaticLayout`，`ICanvas` 无缓存入口），
    // 而 ① 底色 ② 正文 ③ 行号栏 三段里的哪一段是主犯，只能拆开量。
    // 这三段正好按绘制顺序串行，读一次 `_drawWatch` 的累计值做差即可，不用三个秒表。
    private float _tBg, _tText, _tGutter;

    // ── 滚动条（自绘；内容超出视口才出现，按住变粗、松手变细）──────────────

    private enum Bar { None, Vertical, Horizontal }

    /// <summary>当前被按住/拖动的那条滚动条（<see cref="Bar.None"/> = 没在拖）。</summary>
    private Bar _dragBar = Bar.None;

    /// <summary>按住滑块那一刻的抓取偏移 —— 没有它，手指刚按下滑块就会「跳」到手指位置。</summary>
    private float _barGrab;

    /// <summary>
    /// 纵向滑块的（起点, 长度），坐标是画布纵向。
    /// **绘制与命中测试共用这一份** —— 各算一份的话，「看到的滑块」和「点得中的滑块」会错位。
    /// </summary>
    /// <summary>滚动条几何用的画布尺寸：优先用最近一次绘制的（与画出来的那条同源），没画过才退回布局尺寸。</summary>
    private (float W, float H) BarCanvas()
        => (_drawW > 0 ? _drawW : (float)Width, _drawH > 0 ? _drawH : (float)Height);

    private (float Start, float Length) VerticalThumb(float h, float reserveEnd = 0f)
    {
        float track = Math.Max(1f, h - 2 * EditorTypography.BarMargin - reserveEnd);
        long total = _doc?.LineCount ?? 0;
        if (total <= 0) return (EditorTypography.BarMargin, track);

        float len = Math.Max(EditorTypography.BarMinThumb,
            track * Math.Clamp((float)VisibleLines / total, 0f, 1f));
        long scrollable = Math.Max(1, total - VisibleLines);
        float t = Math.Clamp((float)_firstLine / scrollable, 0f, 1f);
        return (EditorTypography.BarMargin + (track - len) * t, len);
    }

    /// <summary>
    /// 横向轨道：(起点, 可用长度)。
    ///
    /// 起点从**行号栏右侧**开始，不贴屏幕左缘：那里是系统的边缘返回手势区，
    /// 滑块停在最左时手指按上去会被系统截走 —— 实测「拖滚动条直接退出了编辑器」。
    /// 顺带也符合直觉：行号栏不参与横滚。
    /// </summary>
    private (float Left, float Track) HorizontalTrack(float w, float reserveEnd = 0f)
    {
        float left = GutterWidth() + EditorTypography.BarMargin;
        return (left, Math.Max(1f, w - left - EditorTypography.BarMargin - reserveEnd));
    }

    /// <summary>横向滑块的（起点, 长度），坐标是画布横向。</summary>
    private (float Start, float Length) HorizontalThumb(float w, float reserveEnd = 0f)
    {
        var (left, track) = HorizontalTrack(w, reserveEnd);
        float maxX = ComputeMaxScrollX();
        if (maxX <= 0) return (left, track);

        // 可见内容宽 / 总内容宽 —— 与纵向同一个「视口占内容的比例」语义
        float viewW = Math.Max(40f, w - GutterWidth());
        float len = Math.Max(EditorTypography.BarMinThumb,
            track * Math.Clamp(viewW / (viewW + maxX), 0f, 1f));
        float t = Math.Clamp(_scrollX / maxX, 0f, 1f);
        return (left + (track - len) * t, len);
    }

    /// <summary>触摸点落在哪条滚动条上。热区比视觉宽得多（不然 2.5pt 的条手指点不中）。</summary>
    private Bar HitBar(float x, float y, float w, float h)
    {
        // 右下角两条重叠时**横向优先**：纵向还能靠拖动内容代替，横向没有别的办法
        if (ComputeMaxScrollX() > 0
            && y >= h - EditorTypography.BarMargin - EditorTypography.BarTouchSlop)
            return Bar.Horizontal;

        if (_doc != null && _doc.LineCount > VisibleLines
            && x >= w - EditorTypography.BarMargin - EditorTypography.BarTouchSlop)
            return Bar.Vertical;

        return Bar.None;
    }

    /// <summary>按位置滚动纵向（t ∈ [0,1] → 首个可见行）。</summary>
    private void DragBarVertical(float y)
    {
        long total = _doc?.LineCount ?? 0;
        if (total <= VisibleLines) return;

        float h = (float)Height;
        float track = Math.Max(1f, h - 2 * EditorTypography.BarMargin);
        var (_, len) = VerticalThumb(h);
        float t = Math.Clamp(
            (y - _barGrab - EditorTypography.BarMargin) / Math.Max(1f, track - len), 0f, 1f);
        _firstLine = t * (total - VisibleLines);
    }

    /// <summary>按位置滚动横向。</summary>
    private void DragBarHorizontal(float x)
    {
        float w = (float)Width;
        float maxX = ComputeMaxScrollX();
        if (maxX <= 0) return;

        var (left, track) = HorizontalTrack(w);
        var (_, len) = HorizontalThumb(w);
        float t = Math.Clamp((x - _barGrab - left) / Math.Max(1f, track - len), 0f, 1f);
        _scrollX = t * maxX;
    }

    /// <summary>
    /// 自绘两条滚动条：**内容超出视口才出现**；按住/拖动时变粗变浓，松手回到细淡。
    /// </summary>
    /// <summary>两条滚动条当前**该不该显示**（内容够长才有）。</summary>
    private (bool V, bool H) BarsShown()
        => (_doc != null && _doc.LineCount > VisibleLines, ComputeMaxScrollX() > 0);

    /// <summary>
    /// 两条同时出现时，各自要为对方让出的**角落长度**。
    ///
    /// 不让的话，纵向条（贴右缘、通到底）与横向条（贴底缘、通到右）会在右下角**交叉重叠**：
    /// 两条半透明黑叠在一起就是一块更深的方块，且角上那一截既不属于纵条也不属于横条，
    /// 看着像画错了（用户实测「交叉处会交叉」）。
    /// 让角 = 横条占用的底边高度（= 最粗时的厚度 + 留白），用 <see cref="EditorTypography.BarThick"/>
    /// 而不是当前厚度 —— 否则**按下变粗的那一瞬几何会跳**。
    /// </summary>
    private static float BarCorner()
        => EditorTypography.BarThick + EditorTypography.BarMargin;

    private void DrawScrollbars(ICanvas canvas, float w, float h)
    {
        if (!BarsShouldShow()) return;   // 5 秒没交互就淡出；「到点那一帧谁来画」由 _barTimer 负责
        bool active = _dragBar != Bar.None;
        float thick = active ? EditorTypography.BarThick : EditorTypography.BarThin;
        var (vShown, hShown) = BarsShown();

        canvas.FillColor = _isDark
            ? (active ? EditorTypography.BarActiveDark : EditorTypography.BarIdleDark)
            : (active ? EditorTypography.BarActive : EditorTypography.BarIdle);

        if (vShown)
        {
            var (y, len) = VerticalThumb(h, hShown ? BarCorner() : 0f);
            canvas.FillRoundedRectangle(w - EditorTypography.BarMargin - thick, y, thick, len, thick / 2);
        }

        if (hShown)
        {
            var (x, len) = HorizontalThumb(w, vShown ? BarCorner() : 0f);
            canvas.FillRoundedRectangle(x, h - EditorTypography.BarMargin - thick, len, thick, thick / 2);
        }
    }

    // ── 滚动条自动淡出 ────────────────────────────────────────────
    // 最后一次触摸的时间；超过 BarAutoHideSeconds 就不画滚动条（下次触摸立刻回来）。
    private DateTime _barTouched = DateTime.MinValue;
    private IDispatcherTimer? _barTimer;

    /// <summary>滚动条此刻该不该显示：正在拖 或 距上次触摸不到 5 秒。</summary>
    private bool BarsShouldShow()
    {
        if (_dragBar != Bar.None) return true;
        // 从未触摸过（刚打开文件）时也先显示 5 秒，给一个"这里还有内容"的提示
        if (_barTouched == DateTime.MinValue) return true;
        return (DateTime.UtcNow - _barTouched).TotalSeconds < EditorTypography.BarAutoHideSeconds;
    }

    /// <summary>
    /// 续命：任何触摸都把它刷新，并确保有个定时器在**到点那一刻重画一帧**。
    ///
    /// ⚠ 这个定时器不是可有可无的：`Draw` 里"不满足条件就不画滚动条"只对**下一次**绘制生效，
    /// 而五秒后没有任何事件会触发那一次绘制 ⇒ 滚动条会永远留在屏上。
    /// 这正是本仓记过的「凡是『满足条件就跳过绘制』的优化，都要回答不满足条件的那一刻谁负责画」。
    /// 它在隐藏后立刻自停，不空转。
    /// </summary>
    private void MarkBarActivity()
    {
        _barTouched = DateTime.UtcNow;
        if (_barTimer != null) return;
        if (Dispatcher is null) return;
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(300);
        timer.Tick += (_, _) =>
        {
            if (BarsShouldShow()) return;    // 活跃期内不重画（省掉每秒三次无谓重绘）
            timer.Stop();
            _barTimer = null;
            Invalidate();                    // 到点这一帧由我们画 —— 否则滚动条永不消失
        };
        _barTimer = timer;
        timer.Start();
    }

    private void DrawDebug(ICanvas canvas, float w, float h, long first, long last, float gutterW, float lineH)
    {
        // 等宽自检：等宽字体下 "i" 与 "W" 必须一样宽，否则说明字体回落成了比例字体
        var si = canvas.GetStringSize("i", EditorTypography.CanvasFont, EditorTypography.FontSize);
        var sw = canvas.GetStringSize("W", EditorTypography.CanvasFont, EditorTypography.FontSize);
        bool mono = Math.Abs(si.Width - sw.Width) < 0.01f;

        // 刻意极短：长文本会被 DrawString 折行/溢出，反而把要看的数字挤没。
        // **帧耗时放最前面**（调性能时它是要看的那个数）：`帧` 是本帧、`峰` 是本次手势最差那帧
        // ——卡顿看峰值，均值会把偶发的长帧平掉。每次手指按下峰值清零，所以「滑一下然后看数」
        // 就是这一段手势的真实表现。
        // 注意：HUD 自己每帧要量两次字宽（等宽自检），开着 HUD 的数比关着略高一点。
        // ⚠ **整串必须短于 512**：`DrawString(text, x, y, HorizontalAlignment.Left)` 那个重载
        // 内部把边界写死成 512，超了会**折行** —— 而 HUD 底色带只有 18 高，第二行看不见、
        // 第一行被顶掉一半（加了分段耗时字段之后就踩到了：读不到 `w` 实测推进量）。
        // 所以这里只留调性能时真正要看的量，拖拽诊断那几个字段（H/d/w）挪走。
        var text = $"{LastDrawMs:F1}ms 峰{_drawMsPeak:F1} X{_scrollX:F0}/{ComputeMaxScrollX():F0}"
                 + $" w{_charWidth:F2}/{_wideCharWidth:F2}"
                 + $" 底{_tBg:F1}文{_tText:F1}号{_tGutter:F1} 行{last - first} {_dragBar}";
        canvas.FontSize = 10;
        canvas.FontColor = Colors.White;
        // MAUI 的 Color.FromArgb 按 #AARRGGBB 解析 —— 写成 #000000AA 的话 alpha=0x00，
        // 整个 HUD 是透明的（此前一直「看不见」就是这个原因）。
        canvas.FillColor = Color.FromArgb("#EE000000");
        canvas.FillRectangle(0, 0, w, 18);   // 占满宽度：截断的宽度框会把关键数字挡住
        canvas.DrawString(text, 6, 2, HorizontalAlignment.Left);
    }

    /// <summary>
    /// 保证编辑光标落在横向视野内。编辑长行时不做这件事，打着打着光标就跑出屏幕了
    /// （自绘层不会跟着输入框内部的横向滚动走）。
    /// </summary>
    public void EnsureCaretVisible()
    {
        if (EditingLine < 0) return;

        float gutter = GutterWidth();
        float caretX = gutter + EditorTypography.TextLeftPad
            + MeasurePrefixWidth(EditingText, EditingCursor);
        float viewW = Math.Max(40f, (float)Width - gutter);
        // **最小滚动：只在光标格真的露不全时才挪，且只挪到刚好露出来。**
        //
        // 原来这里是 48f（约 2.5 列）—— 后果是点一个本来就在屏幕里的字，屏幕也会横向
        // 跳一下。原则是**屏幕适应光标，不是光标适应屏幕**：中间的字点了，一个字都不该动。
        // 留「一列」余量，正对应「光标右边要还能显示下一个字符，否则左滚一格」。
        float margin = EditorTypography.HalfWidth;

        float left = caretX - _scrollX;
        if (left > viewW - margin) _scrollX = caretX - viewW + margin;
        else if (left < margin) _scrollX = Math.Max(0, caretX - margin);

        // ⚠ 上面两式**自己不带边界**，算出来的可能是负数或超过行尾 —— 收口到 ClampScroll，
        // 边界守卫只有那一处实现（否则点一次行尾就永久「滚过头」，屏幕上留着一段空白边，
        // 实测 X3504/3412 就是这样来的）。
        ClampScroll();
        Invalidate();
    }

    /// <summary>
    /// 行内第 <paramref name="charIndex"/> 个 UTF-16 码元之前的**显示宽度**（pt）。
    ///
    /// **逐字形累加平台实测推进量**（<see cref="MeasureAdvances"/>），而不是「列号 × 半列宽」。
    ///
    /// 为什么必须按实测推进量走：Android 会把**每个字形的推进量取整**，于是 13 号下半角的真实
    /// 推进是 7 而不是设计值 6.5 —— 实测每个半角字形差 0.5，一行 107 列能差出 **53.5px**。
    /// 用设计值定位等于拿一把**平台没在用的尺子**：文字按平台的排、光标按我们的网格走，
    /// 长行越往右越对不上。实测推进量则与渲染同源 ⇒ 任何字号（含小数）都逐字对齐。
    ///
    /// （GUI/Avalonia 的自绘编辑器仍走 <see cref="TextEditorMath"/> 的列网格 —— 那边的文字栈
    /// 是否也取整未被验证过，不能想当然跟着改。）
    /// </summary>
    public float MeasurePrefixWidth(string? line, int charIndex)
    {
        if (string.IsNullOrEmpty(line) || charIndex <= 0) return 0;

        int limit = Math.Min(charIndex, line.Length);

        // **先展开 tab、再累加** —— 展开后的串就是画布实际绘制的那一串，于是
        // 「画出来的宽度」与「光标落在哪」不可能再用两套 tab 规则（那是本文件踩过的坑）。
        var (expanded, map) = TextEditorMath.ExpandTabsWithMap(line, EditorTypography.TabColumns);
        return AdvancePrefix(expanded, map[limit]);
    }

    /// <summary>展开串里前 <paramref name="expandedIndex"/> 个码元的横坐标（纯累加，不含 tab）。</summary>
    private float AdvancePrefix(string expanded, int expandedIndex)
    {
        if (expandedIndex <= 0 || expanded.Length == 0) return 0;
        int limit = Math.Min(expandedIndex, expanded.Length);
        float x = 0;
        int i = 0;
        foreach (var r in expanded.EnumerateRunes())
        {
            if (i >= limit) break;
            x += r.Value == '\t' ? 0 : AdvanceOf(r);
            i += r.Utf16SequenceLength;
        }
        return x;
    }


    /// <summary>走「整段前缀测量」的字符数上限 —— 再长就退回累加，免得为一条超长行分配整段字符串。</summary>

    /// <summary>
    /// 行内横坐标（pt，相对正文起点）→ 字符下标。
    ///
    /// **逐字累加实测宽度**，而不是「字符数 × 平均字宽」：中文与拉丁的宽度比不是整数，
    /// 按比例算在中文行里会差出一两个字符（用户实测「插入位置错了一个字符」）。
    /// 落在某个字符的前半 → 归到它前面；后半 → 归到它后面。
    /// </summary>
    public int CharIndexAtX(string line, float xInLine)
    {
        if (string.IsNullOrEmpty(line) || xInLine <= 0) return 0;

        // **<see cref="MeasurePrefixWidth"/> 的逆**：同样先展开 tab，在展开串上逐字形累加实测推进量
        // （落在一个字形格子的**前半** → 归它前面，后半 → 归它后面），再把展开下标映射回原串下标。
        // 两边共用「展开 + 映射」这一条路，所以点哪儿与光标画哪儿不可能差一格。
        var (expanded, map) = TextEditorMath.ExpandTabsWithMap(line, EditorTypography.TabColumns);
        if (expanded.Length == 0) return 0;

        float x = 0;
        int i = 0, hit = expanded.Length;
        foreach (var r in expanded.EnumerateRunes())
        {
            float adv = r.Value == '\t' ? 0 : AdvanceOf(r);
            if (xInLine < x + adv * 0.5f) { hit = i; break; }
            x += adv;
            i += r.Utf16SequenceLength;
        }
        return SourceIndexOf(map, hit, line.Length);
    }

    /// <summary>
    /// 展开串下标 → 原串下标：取**最后一个**满足 `map[i] &lt;= expandedIndex` 的 i
    /// （map 单调不减，所以这就是「该展开位置所属的那个原字符」）。
    /// </summary>
    private static int SourceIndexOf(int[] map, int expandedIndex, int lineLength)
    {
        int lo = 0, hi = lineLength;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (map[mid] <= expandedIndex) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>
    /// 量出**平台真实推进量**：半角一个字符多宽、全角一个字符多宽。
    ///
    /// 不能用设计值（`FontSize × 0.5`）：Android 把每个字形的推进量**取整**，
    /// 13 号下半角真实推进是 7 而非 6.5（实测每个半角字形 +0.5）。量出来的才是渲染在用的那把尺子。
    ///
    /// 量两个字符就够：代码里的字符按宽度只有两档（半角 / 全角），Tab 另有 tab stop 规则。
    /// 字号一变就要重量（<see cref="ResetTypography"/> 会清掉 <c>_charWidthMeasured</c>）。
    /// </summary>
#if WINDOWS
    /// <summary>
    /// 用 Win2D 的排版对象量「一个半角 / 一个全角」的推进量 —— **与 <c>DrawString</c> 同一个引擎**。
    ///
    /// `GetCaretPosition(1, false).X` 就是「第 1 个字符之后的落笔位置」，也就是该字形的推进量 ——
    /// 渲染真正在用的那个数。返回 (0,0) 表示量不到（调用方退回设计值）。
    ///
    /// ⚠ 排版格式必须与 <c>W2DCanvas.DrawString</c> 里那句逐字一致（族名/字号/两个对齐），
    /// 否则又是「两把尺子」——这正是本文件反复栽的那个坑。
    /// </summary>
    private static (float Half, float Wide) MeasureAdvancesWin2D(ICanvas canvas, string? family = null)
    {
        try
        {
            if (canvas is not Microsoft.Maui.Graphics.Win2D.W2DCanvas w2d) return (0f, 0f);

            float One(string s)
            {
                using var layout = new Microsoft.Graphics.Canvas.Text.CanvasTextLayout(
                    w2d.Session, s, BuildProbeFormat(family), 100000f, 200f);
                return layout.GetCaretPosition(1, false).X;
            }
            return (One("0"), One("中"));
        }
        catch { return (0f, 0f); }
    }

    /// <summary>与 <c>W2DCanvas.DrawString</c> 内部那句 <c>ToCanvasTextFormat</c> 等价的排版格式。</summary>
    private static Microsoft.Graphics.Canvas.Text.CanvasTextFormat BuildProbeFormat(string? family = null)
        => new()
        {
            FontFamily = family ?? EditorTypography.CanvasFontName,
            FontSize = EditorTypography.FontSize,
            VerticalAlignment = Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment.Top,
            HorizontalAlignment = Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment.Left,
        };
#endif

    private void MeasureAdvances(ICanvas canvas)
    {
        float lat, wide;
#if WINDOWS
        // 【Windows 真根因】尺子必须用**与渲染同一个排版引擎**去量。
        //
        // `GetStringSize` 在 Win2D 后端上返回的数**既不是 0.5em 也不是 1em**：实测字号 14 时
        // a=6.12、中=10.61，**连比例都不对**（6.12/7=0.874，10.61/14=0.758）。它自己另建排版对象，
        // 而 `DrawString` 走的是 `W2DCanvas` 里那套（FontFamily=font.Name、VerticalAlignment=Top、
        // HorizontalAlignment=Left）。同一个 NSimSun，用渲染那套量出来是**精确的 7.00 / 14.00**。
        //
        // 定位（逐段绘制 x、光标 x、点击命中）全走这把尺子 ⇒ 拿错的数定位，汉字之后必然越走越偏
        // （用户实测：「汉字和英文衔接处容易偏」）——**与字体无关，换任何字体都治不好**。
        // 早先把它误判成「字体回落」，是因为自检也用了这把错尺子（量什么字体都返回那个错数）。
        (lat, wide) = MeasureAdvancesWin2D(canvas);
#else
        try
        {
            lat = (float)canvas.GetStringSize("0", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
            wide = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
        }
        catch { lat = 0; wide = 0; }
#endif

        // 量不到就退回设计值 —— 宁可差一点，也不能让字宽变成 0（除零会把整屏算崩）
        if (lat <= 0) lat = Math.Max(1f, EditorTypography.HalfWidth);
        if (wide < lat) wide = lat * 2f;

#if ANDROID
        // ⚠ **把推进量吸附到「整数设备像素」，与渲染同格**（v0.96.140，光标压字的真根因）。
        //
        // 平台**绘制**时会把每个字形的推进量取整到整数设备像素（未开亚像素定位时），
        // 而 `GetStringSize`（= `Layout.GetLineWidth`）报的是**未取整的小数**：
        // 实测（模拟器 420dpi、字号 14）排版报 18.375px，**画出来的栅距精确 18.000px**
        // （一行 40 个 H，相邻墨迹中点间距全是 18）。
        // 于是每字差 0.375px、沿行累积 —— 到第 19 列就是 7px ≈ **半个格子**，
        // 光标于是画在**字符格的中间**而不是格线上（用户实测「var 的光标压在 a 上面」）。
        //
        // 对齐的条件不是「字号是整数」（14 也是整数号，照样差），而是
        // **「字号 × 0.5 × 屏幕密度」恰好落在整数上** —— 24 号在 2.75 密度下是 33.0px，
        // 所以「24 没问题」，而 14 号在 2.625 下是 18.375px，就出问题。
        //
        // ⚠ 这**不是**「把字号取整」（那会毁掉无极缩放，用户明确反对）：吸附的是
        // **「一个字形推进多少」这个长度**，字号本身仍然连续可取 —— 字号每变一点，
        // 排版和这个长度都跟着变；只是这个长度落在与渲染同一张网格上。
        // 换句话说：**平台画多宽，我们就按多宽算**。
        // ⚠ **不能用「实测值」去吸附，要用「字体设计值」**（v0.96.143 定的）。
        //
        // 平台的绘制规则是：**每个字形的推进量 = round(字体设计值 × 屏幕比例) 个设备像素**。
        // 而 `GetStringSize` 报的是排版算出来的小数，并且**实测比设计值偏大** ——
        // 用户手机（字号 37、比例 2.75）：实测报 18.8dp（=51.7px），而画出来的是
        // **整整 51.000px**（一行 19 个 H、相邻墨迹中点差全是 51），设计值 18.5dp×2.75 = 50.88 → round = 51 ✓。
        // 拿偏大的实测值去吸附就得到 52px ⇒ **每字多 1px**，到第 10 列就偏 10px（半个格子），
        // 光标于是落进字格里 —— 这正是用户反复反馈的「压在字母上」。
        //
        // 所以这里反过来：**从设计值算，吸到设备像素网格**。
        // 两端实测吻合：模拟器（14 号 / 比例 2.625）→ 18px ✓；用户手机（37 号 / 2.75）→ 51px ✓。
        //
        // 比例取「屏幕物理宽 ÷ 控件 dp 宽」而不是 `MainDisplayInfo.Density` —— 本文件 `OnEnd`
        // 里的调试探针早就写着这两者并不总是相等（「与平台路径用的密度不同 ⇒ 字号喂错了」）。
        float scale = Width > 0.5f && Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Width > 0
            ? (float)(Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Width / Width)
            : (float)Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
        if (scale > 0.01f)
        {
            lat = MathF.Round(EditorTypography.HalfWidth * scale) / scale;
            wide = MathF.Round(EditorTypography.FullWidth * scale) / scale;
        }
#endif

        _charWidth = lat;
        _wideCharWidth = wide;
        _advanceCache.Clear();   // 字号变了，非打包字体字符的推进量也得重量
        ClearTextCache();      // 行号/气泡的文本缓存里也挂着按字号编好的排版
    }

    /// <summary>
    /// 单个码元占多宽。
    ///
    /// **这里没有 tab 分支是故意的**：调用方一律先把行交给
    /// <see cref="TextEditorMath.ExpandTabsWithMap"/>，在展开后的串上累加 —— tab 的推进规则
    /// 因此只有那一处实现（见那边关于「曾经两套 tab 规则、中文后面跟 tab 就差一格」的注释）。
    ///
    /// ⚠ **不能简单地把「宽字符」都当成同一个宽度**。`AnsiString.CharWidth` 把 CJK 与
    /// **emoji / 杂项符号**都判成 2 列，但打包的 Sarasa **没有 emoji 字形** —— 那些字符是平台用
    /// **回落字体**（Noto Color Emoji）画的，推进量不一定等于 Sarasa 的全角推进量。
    /// 一律按 `_wideCharWidth` 算，光标在 emoji 之后就会偏（正是「有时还是不对」的那一类）。
    ///
    /// 所以分两档：**确定在 Sarasa 里**的 CJK/全角区间直接用实测全角宽（快路径，不测量）；
    /// 其余宽字符**逐个实测并缓存**（emoji 每个码点只量一次），拿到的就是平台真正在用的推进量。
    /// </summary>
    private float AdvanceOf(Rune r)
    {
        int w = WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r);
        if (w <= 0) return 0;
        if (w == 1) return _charWidth;
        if (IsPackedFontWide(r)) return _wideCharWidth;

        if (_advanceCache.TryGetValue(r.Value, out var cached)) return cached;

        float measured = 0;
        try
        {
            if (_measureCanvas != null)
                measured = (float)_measureCanvas.GetStringSize(r.ToString(),
                    EditorTypography.CanvasFont, EditorTypography.FontSize).Width;
        }
        catch { measured = 0; }
        if (measured <= 0) measured = _wideCharWidth;   // 量不到就退回全角宽，绝不返回 0（会让整行算崩）

        if (_advanceCache.Count < MaxAdvanceCache) _advanceCache[r.Value] = measured;
        return measured;
    }

    /// <summary>
    /// 该宽字符**确定由打包字体（Sarasa Mono SC）渲染**吗？—— 是的话推进量就是实测的全角宽。
    ///
    /// 列的都是 CJK / 全角字形区间（与 <c>AnsiString.CharWidth</c> 里判 2 列的范围取交集）。
    /// 剩下的宽字符（emoji、杂项符号、dingbats…）不在内 —— 它们可能是回落字体画的，得实测。
    /// </summary>
    private static bool IsPackedFontWide(Rune r)
    {
        int cp = r.Value;
        return cp is >= 0x1100 and <= 0x115F      // 韩文字母
            or >= 0x2E80 and <= 0xA4CF            // CJK 部首 ~ 彝文
            or >= 0xA960 and <= 0xA97C            // 韩文扩展
            or >= 0xAC00 and <= 0xD7A3            // 韩文音节
            or >= 0xF900 and <= 0xFAFF            // CJK 兼容汉字
            or >= 0xFE10 and <= 0xFE19            // 竖排标点
            or >= 0xFE30 and <= 0xFE6F            // CJK 兼容标点
            or >= 0xFF01 and <= 0xFF60            // 全角 ASCII
            or >= 0xFFE0 and <= 0xFFE6            // 全角符号
            or >= 0x20000 and <= 0x3FFFD;         // CJK 扩展 B+
    }

    /// <summary>取一行的显示文本（供页面做查找高亮/状态栏）。</summary>
    public string? GetLineText(long oneBased) => _doc?.GetLine(oneBased - 1);

    /// <summary>测量等宽字符宽度（首个 Draw 之后才准）。</summary>
    public float CharWidth => _charWidth;

    // ── 诊断（状态栏用；HUD 画在画布上会和首行正文重叠，字看不清）──
    public float WideCharWidth => _wideCharWidth;
    public float ScrollX => _scrollX;
    public float MaxScrollX => ComputeMaxScrollX();
    public string BarDebug => _dragBar.ToString();
}
