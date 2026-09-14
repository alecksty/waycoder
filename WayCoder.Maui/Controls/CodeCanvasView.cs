using System.Diagnostics;
using System.Text;
using Microsoft.Maui.Graphics.Text;
using WayCoder.Infra;
using WayCoder.Maui.Markup;
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

    // ── 选择与光标 ──

    private long _caretLine = -1;
    private long _selAnchor = -1, _selEnd = -1;
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
    /// **缓存是字体无关的**（颜色与文本都不含字号）⇒ 调字号**不必**清这份缓存。
    /// 只有**文本变了**才清（见 <see cref="InvalidateLine"/> / <see cref="InvalidateAll"/>）。
    /// </summary>
    private sealed class LineRuns
    {
        /// <summary>整行一次的带色文本（语法色是它内部的多个 run）。空行 / 无内容为 null。</summary>
        public IAttributedText? Whole;

        /// <summary>空行（什么都不画）—— 共用一个实例，免得每个空行都建一个对象。</summary>
        public static readonly LineRuns Empty = new();
    }

    private readonly Dictionary<long, LineRuns> _lineCache = new();
    private readonly List<long> _cacheOrder = [];

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
        CancelInteraction += (_, _) => { _dragging = false; _longPress = false; _dragBar = Bar.None; };
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
        _lineCache.Clear();
        _cacheOrder.Clear();
        _lineWidths.Clear();
        _editingRuns = null;
        _editingRunsFor = null;
        _firstLine = 0;
        _scrollX = 0;
        _scrollFontSize = EditorTypography.FontSize;   // 与 _scrollX 成对（见 ResetTypography）
        _velocityX = _velocityY = 0;
        _caretLine = -1;
        _selAnchor = _selEnd = -1;
        Invalidate();
    }

    /// <summary>
    /// 切亮/暗配色。
    ///
    /// ⚠ 必须**连行缓存一起清**：缓存里的每个段都烘进了当时的配色（<c>BuildLineRuns</c> 按
    /// <c>_isDark</c> 取色），只 <c>Invalidate()</c> 的话正文会保留旧主题的颜色直到缓存被淘汰。
    /// 目前主题是在 <see cref="SetDocument"/> 时一次性传进来的（那条路本来就会清缓存），
    /// 所以这个方法是给「运行中切主题」预留的 —— 保持它自身正确，别留成陷阱。
    /// </summary>
    public void SetDark(bool isDark)
    {
        if (_isDark == isDark) return;
        _isDark = isDark;
        _lineCache.Clear();
        _cacheOrder.Clear();
        _editingRuns = null;
        _editingRunsFor = null;
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

        _charWidthMeasured = false;   // 推进量要按新字号重量（MeasureAdvances）
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

    public void SetCaretLine(long oneBased)
    {
        _caretLine = oneBased - 1;
        Invalidate();
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
    private bool _dragging, _moved, _longPress;
    private float _pinchStartDist;
    private float _pinchStartFontSize;
    private long _downTicks;
    private float _downX, _downY;

    private void OnStart(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        var p = e.Touches[0];
        _lastX = _downX = p.X;
        _lastY = _downY = p.Y;
        _downTicks = Environment.TickCount64;
        _dragging = true;
        _moved = false;
        _longPress = false;
        _samples.Clear();
        _samples.Enqueue((_downTicks, p.Y, p.X));
        StopFling();
        _drawMsPeak = 0;   // 新手势 ⇒ 重新开始记最差帧

        // 按在滚动条上 → 这一手势归滚动条，不当成内容拖拽（也就不会触发惯性/长按选择）。
        // 按在滑块上保持抓取偏移（不跳），按在轨道上视作「跳到此处」。
        { var (bw, bh) = BarCanvas(); _dragBar = HitBar(p.X, p.Y, bw, bh); }
        if (_dragBar != Bar.None)
        {
            var (start, len) = _dragBar == Bar.Vertical
                ? VerticalThumb((float)Height) : HorizontalThumb((float)Width);
            float pos = _dragBar == Bar.Vertical ? p.Y : p.X;
            _barGrab = pos >= start && pos <= start + len ? pos - start : len / 2f;
            _dragging = false;
            Invalidate();   // 立刻画粗版，让「按住了」有即时反馈
        }
    }

    /// <summary>
    /// 限流重绘：触摸事件在 Android 上可达 120~240Hz，而**每帧要重排可见的几十行文本**
    /// （每行一次原生 StaticLayout）。逐事件重绘等于把同样的活干两到四遍，滚动就会发涩。
    /// 压到 ~60fps 后，位置照样每次都更新，只是合并到下一帧一起画。
    /// </summary>
    private void ThrottledInvalidate()
    {
        long now = Environment.TickCount64;
        if (now - _lastPaintTicks < 16) return;
        _lastPaintTicks = now;
        Invalidate();
    }

    private long _lastPaintTicks;

    private void OnDrag(object? sender, TouchEventArgs e)
    {
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
            ScrollingStarted?.Invoke();   // 开始拖动 ⇒ 让页面先结束编辑（见事件注释）
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
        bool wasPinching = _pinchStartDist > 0;
        _pinchStartDist = 0;
        _dragging = false;
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

        // 长按（未移动且超过 500ms）：只读模式下弹复制/选择菜单
        if (!_moved && elapsed >= 500)
        {
            _caretLine = hitLine;
            LineLongPressed?.Invoke(hitLine + 1);
            Invalidate();
            return;
        }

        if (!_moved && elapsed < 500)
        {
            // 单击：定位行
            long line = hitLine;
            _caretLine = line;

            if (_selecting)
            {
                _selEnd = line;
                var (a, b) = _selAnchor <= _selEnd ? (_selAnchor, _selEnd) : (_selEnd, _selAnchor);
                SelectionChanged?.Invoke(a + 1, b + 1);
            }
            float xInLine = _lastX - GutterWidth() - EditorTypography.TextLeftPad + _scrollX;
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

    /// <summary>惯性停止阈值（pt/s）——低于它就看不出在动了。</summary>
    private const float MinFlingVelocity = 40f;

    /// <summary>
    /// 每帧速度保留比例 —— **滑多远由它决定**：总位移 = 初速 × dt / 行高 / (1 − friction)。
    /// 0.98 ⇒ 约 50 倍单帧位移（0.95 ⇒ 20 倍，0.97 ⇒ 33 倍）。
    /// 这个数直接决定手感（实测调过两轮），改动前先按上式估一下。
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

        if (Math.Abs(_velocityY) < MinFlingVelocity && Math.Abs(_velocityX) < MinFlingVelocity) return;

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

        if (Math.Abs(_velocityY) < MinFlingVelocity && Math.Abs(_velocityX) < MinFlingVelocity) StopFling();
    }

    private void StopFling()
    {
        _fling?.Stop();
        _velocityX = _velocityY = 0;
    }

    /// <summary>进入/退出「选择行」模式（长按触发，或工具栏按钮）。</summary>
    public void BeginSelection()
    {
        _selecting = true;
        _selAnchor = _selEnd = _caretLine;
        Invalidate();
    }

    public void ClearSelection()
    {
        _selecting = false;
        _selAnchor = _selEnd = -1;
        SelectionChanged?.Invoke(0, 0);
        Invalidate();
    }

    /// <summary>取当前选中的行文本（闭区间，1-based）。无选择返回空串。</summary>
    public string GetSelectedText()
    {
        if (_doc == null || _selAnchor < 0 || _selEnd < 0) return "";
        long a = Math.Min(_selAnchor, _selEnd), b = Math.Max(_selAnchor, _selEnd);
        var sb = new System.Text.StringBuilder();
        for (long i = a; i <= b && i < _doc.LineCount; i++)
        {
            var line = _doc.GetLine(i);
            if (line == null) { _ = _doc.PrefetchAsync(i, i); continue; }
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.ToString();
    }

    public bool HasSelection => _selAnchor >= 0 && _selEnd >= 0;

    /// <summary>选择范围的显示文本（「3」或「3-7」），无选择返回空串。状态栏用。</summary>
    public string SelectionChangedRange
    {
        get
        {
            if (_selAnchor < 0 || _selEnd < 0) return "";
            long a = Math.Min(_selAnchor, _selEnd) + 1;
            long b = Math.Max(_selAnchor, _selEnd) + 1;
            return a == b ? $"{a}" : $"{a}-{b}";
        }
    }

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
                    float advA = (float)canvas.GetStringSize("a", EditorTypography.CanvasFont, full).Width;
                    float advWide = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont, full).Width;
                    bool ok = Math.Abs(advA - half) < 1.0f && Math.Abs(advWide - full) < 1.0f;
                    string tag = ok ? "[字体自检] OK  内嵌 Sarasa 已加载" : "[字体自检] ❌ 字体回落了！检查 CanvasFontName";
                    System.Diagnostics.Debug.WriteLine($"{tag} a={advA:F2}(期望 {half:F2}) 中={advWide:F2}(期望 {full:F2}) 名={EditorTypography.CanvasFontName}");
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
            float sum = l0 == null ? 0 : MeasurePrefixWidth(l0, l0.Length);
            float whole = l0 == null ? 0 : (float)canvas.GetStringSize(l0,
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

        // ② 正文（裁剪在行号栏右侧，横向滚动只影响这一层）
        float textX = gutterW + EditorTypography.TextLeftPad - _scrollX;
        // 本文件有没有诊断 —— **只查一次**。DrawDiagnosticWave 是每行每帧都要查一次表的
        // （内部走 LINQ `.Where().ToList()`，没数据时也要分配一个空 List），
        // 而移动端压根没有诊断数据源（依赖 LintTool，MAUI 里是桩，见 CLAUDE.md）⇒ 整段空跑。
        bool hasDiags = HasDiagnostics();
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
                    canvas.DrawText(new AttributedText(seg, []), segX,
                        y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
            }
            else if (line.Length > 0)
            {
                // 整行一次绘制（见 DrawLineRuns —— 位置由网格与平台共同保证一致）。
                // 几何来自行缓存（编辑行走 EditingRuns，它只在击键时失效）。
                DrawLineRuns(canvas, editing ? EditingRuns(line) : GetLineRuns(i, line),
                    textX, y, lineH);
            }

            if (editing) DrawCaret(canvas, line, textX, y, lineH);

            if (hasDiags) DrawDiagnosticWave(canvas, i, y, textX, lineH);
        }
        canvas.RestoreState();

        // ③ 行号栏（最后画 —— 它会盖掉光标行底色横跨过来的那一段）
        //
        // **视口正在移动的这一帧不画行号数字**。理由是最小字号下的账：
        // 每可见行要两次平台文本绘制（正文一次、行号一次），而 `DrawText` 每次都得新建
        // `StaticLayout`（`ICanvas` 没有缓存入口）—— 字号 8 时一屏 50 多行，行号栏就是其中一半。
        // 滚动中数字本来也看不清，等停下再补：**判据是「本帧滚动位置与上帧是否相同」**，
        // 不依赖手势状态机（拖拽/惯性/程序滚动三条路都自动覆盖），停下后的下一帧位姿不变 ⇒ 数字回来。
        bool viewMoving = Math.Abs(_firstLine - _lastDrawnFirstLine) > 0.01f
                          || Math.Abs(_scrollX - _lastDrawnScrollX) > 0.01f;
        _lastDrawnFirstLine = _firstLine;
        _lastDrawnScrollX = _scrollX;
        DrawGutter(canvas, first, last, gutterW, h, lineH, withNumbers: !viewMoving);

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
                    + MeasurePrefixWidth(rulerLine, rulerLine.Length) - _scrollX;
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
            string probeText = "";
            for (long i = first; i < Math.Min(first + 40, _doc.LineCount); i++)
            {
                var l = _doc.GetLine(i);
                if (l is { Length: > 60 }) { probeText = l; break; }
            }
            if (probeText.Length > 0)
            {
                var disp = TextEditorMath.ExpandTabs(probeText, EditorTypography.TabColumns);
                double worst = 0;
                foreach (float size in new[] { 8f, 12f, 13f, 16f, 28f, 30f })
                {
                    // 该字号下的实测推进量（与 MeasureAdvances 同源，但这里是临时量、不写入字段）
                    float lat = (float)canvas.GetStringSize("0", EditorTypography.CanvasFont, size).Width;
                    if (lat <= 0) continue;
                    foreach (int n in new[] { 30, 60, 120 })
                    {
                        var prefix = RunePrefix(disp, n);
                        if (prefix.Length == 0) continue;
                        // 我们逐字累加会算出的宽度（按同一套半角/全角判据）
                        float mine = 0;
                        foreach (var r in prefix.EnumerateRunes())
                            mine += WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r) > 1
                                ? (float)canvas.GetStringSize("中", EditorTypography.CanvasFont, size).Width
                                : lat;
                        float platW = (float)canvas.GetStringSize(prefix,
                            EditorTypography.CanvasFont, size).Width;
                        worst = Math.Max(worst, Math.Abs(platW - mine));
                    }
                }
                bool ok = worst < 1.5;
                Android.Util.Log.Info("WCFONT",
                    $"{(ok ? "[排版自检] OK" : "[排版自检] ❌ 推进量不可加！")} "
                    + $"逐字累加 vs 平台排版 最大偏差={worst:F2}px 字号={EditorTypography.FontSize:F1}");
            }
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

    /// <summary>
    /// 上一帧画的是哪个视口位姿（首个可见行 + 横向偏移）—— 用来判断「本帧视口是否在动」，
    /// 决定行号数字要不要跳过（见 <see cref="DrawGutter"/> 的 <c>withNumbers</c>）。
    /// 初值取 0 与构造函数里的初始位姿一致，所以**第一帧算「没动」**、正常画行号。
    /// </summary>
    private float _lastDrawnFirstLine;
    private float _lastDrawnScrollX;

    /// <summary>
    /// 上次更新 <see cref="_scrollX"/> 时的字号。改字号时用它把横向偏移**按比例**换算到新字号
    /// （见 <see cref="ResetTypography"/>）—— 否则缩放会把视口拽回最左边。
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

    private void DrawLineBackgrounds(ICanvas canvas, long first, long last,
        float gutterW, float w, float lineH)
    {
        long selA = _selAnchor < 0 ? -1 : Math.Min(_selAnchor, _selEnd);
        long selB = _selAnchor < 0 ? -1 : Math.Max(_selAnchor, _selEnd);

        for (long i = first; i < last; i++)
        {
            bool selected = selA >= 0 && i >= selA && i <= selB;
            bool caret = i == _caretLine;
            if (!selected && !caret) continue;

            // 高亮条与文字用**同一个 y**（都是行顶）。二者曾经因为一处算了基线补偿、
            // 另一处没算而差开半行 —— 现在两边都直接取行顶，没有第二套算法。
            float y = LineY(i, lineH) + EditorTypography.TextBaselineOffset;
            canvas.FillColor = selected
                ? EditorTypography.SelectionBg
                : (_isDark ? EditorTypography.CaretLineBgDark : EditorTypography.CaretLineBg);
            canvas.FillRectangle(0, y, w, lineH);
        }
    }

    /// <param name="withNumbers">
    /// 是否画行号**数字**。视口正在移动（本帧滚动位置与上帧不同）时传 false ——
    /// 见 <see cref="Draw"/> 里对 <c>_lastDrawnFirstLine</c> 的说明。
    /// </param>
    private void DrawGutter(ICanvas canvas, long first, long last, float gutterW, float h, float lineH,
        bool withNumbers)
    {
        // 底色**始终画**：只跳数字，不跳行号栏本身 —— 否则滚动时左边缘会露出一条与正文同色的
        // 空白，看着像界面在抖。滚动中行号栏保持是「一条安静的灰边」，停下再补上数字。
        canvas.FillColor = _isDark ? EditorTypography.GutterBgDark : EditorTypography.GutterBg;
        canvas.FillRectangle(0, 0, gutterW, h);
        if (!withNumbers) return;

        // 字号比正文小一号（行号是辅助信息，不该和代码抢注意力）。
        //
        // ⚠ **不要在这里设 `canvas.Font`**：`CanvasFont` 是 static readonly，正文前已经设过，
        // 这里再设一遍是纯冗余 —— 而它并不是免费的：`PlatformCanvasState.Font` 的写入会让字体族
        // 解析作废，下一次 `FontPaint` 访问就要重走 `FontExtensions.ToTypeface()`（那条路**没有缓存**）。
        // 每次重解析 = 把打包字体读一遍。字体压缩进 APK 时每次 ~110ms（见 csproj 里
        // `AndroidStoreUncompressedFileExtensions` 的注释），那是编辑器卡顿的真身。
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
            // 行号也用 DrawText 而非 DrawString：两者的 y 语义在平台上并不一致
            // （DrawString/Android 是 em 底、iOS 是基线），混用会让行号与代码整体错开。
            canvas.DrawText(GutterAttributed(label, color), x, y + EditorTypography.TextBaselineOffset,
                1_000_000f, lineH);
        }
        canvas.RestoreState();
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
    private void DrawCaret(ICanvas canvas, string line, float textX, float y, float lineH)
    {
        int col = Math.Clamp(EditingCursor, 0, line.Length);
        float x = textX + MeasurePrefixWidth(line, col);

        // 深色底用纯白、浅色底用纯黑：系统那个光标跟随主题色（Android 上是 Material 紫），
        // 在深色代码背景上很不起眼。这里明确取对比度最高的两色，并加粗到一目了然。
        canvas.StrokeColor = _isDark ? Colors.White : Colors.Black;
        canvas.StrokeSize = 2.5f;
        canvas.DrawLine(x, y + 2, x, y + lineH - 3);
    }

    private readonly Dictionary<string, IAttributedText> _gutterCache = [];

    /// <summary>行号的带色文本（按「文本+颜色」缓存 —— 行号字符串高度重复，逐帧重建毫无必要）。</summary>
    private IAttributedText GutterAttributed(string label, Color color)
    {
        var key = label + "|" + color.ToHex();
        if (_gutterCache.TryGetValue(key, out var cached)) return cached;

        // ⚠ 这里**不能**写 FontName —— 与正文段同一条铁律（见 BuildLineRuns 的长注释）：
        // run 上写 FontName 会被 MAUI 变成 Android 的 `TypefaceSpan(族名)`，而那个 API 只认
        // **系统字体族名、没有 asset 重载**，喂资产名 `SarasaMonoSC-Regular.ttf` 进去解析不到，
        // 只会**静默回落成平台默认的比例字体** —— 行号数字于是不是等宽的（右对齐的位数会歪）。
        // 不写则布局回落用 `canvas.Font`（= EditorTypography.CanvasFont），走的才是
        // `CreateFromAsset` 分支、能加载打包字体。
        //
        // 附带的好处：不写 FontName，这份缓存就**只跟文本+颜色绑定**，不受字号影响；
        // 而写进去的 FontName 会让每次 `DrawText` 都去做一次注定失败的族名解析。
        var attr = new AttributedText(label,
        [
            new AttributedTextRun(0, label.Length, new TextAttributes
            {
                [TextAttribute.Color] = color.ToHex(),
            }),
        ]);
        if (_gutterCache.Count < 512) _gutterCache[key] = attr;
        return attr;
    }

    /// <summary>尚未加载的行：画一个占位符，绝不在这里等 IO（滚动会被拖成一顿一顿的）。</summary>
    private static void DrawPending(ICanvas canvas, float x, float y, float lineH)
        => canvas.DrawString("⋯", x, y, HorizontalAlignment.Left);

    /// <summary>
    /// 本文件是否存在诊断 —— 给绘制循环做**整段短路**用（每帧只查一次，而不是每行查一次）。
    /// 空路径、无数据源（移动端就是这种）、查询异常一律判为「没有」。
    /// </summary>
    private bool HasDiagnostics()
    {
        if (_filePath.Length == 0) return false;
        try { return DiagnosticManager.GetAll(_filePath).Count > 0; }
        catch { return false; }
    }

    /// <summary>
    /// 语法错误波浪线 —— 在该行文字下方画一段三角波。
    /// 宽度只覆盖有诊断的列区间（拿不到列号时覆盖整行），并**限制最大长度**：
    /// 一条长行上画几千个折点会把一帧拖垮。
    /// </summary>
    private const float WaveAmplitude = 1.6f;
    private const float WaveStep = 3f;
    private const float WaveMaxWidth = 600f;

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
        float baseY = y + lineH - 3f;

        canvas.StrokeColor = worst.Severity switch
        {
            Severity.Error => EditorTypography.ErrorWave,
            Severity.Warning => EditorTypography.WarnWave,
            _ => EditorTypography.InfoWave,
        };
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

    // ── 行渲染（带缓存的唯一实现）──

    private LineRuns GetLineRuns(long index, string line)
    {
        if (_lineCache.TryGetValue(index, out var cached)) return cached;

        var runs = BuildLineRuns(line);
        if (_lineCache.Count >= MaxCachedLines)
        {
            // 简单 FIFO 淘汰：滚动时被淘汰的正好是最久没看的那批
            int drop = Math.Min(64, _cacheOrder.Count);
            for (int i = 0; i < drop; i++) _lineCache.Remove(_cacheOrder[i]);
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
            offset += len;
        }

        // 一个色 run 都没切出来（无分词器 / tokenizer 不给内容）：整行素色画。
        return new LineRuns { Whole = new AttributedText(display, runs) };
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
    private static void DrawLineRuns(ICanvas canvas, LineRuns runs, float textX, float y, float lineH)
    {
        if (runs.Whole is null) return;
        canvas.DrawText(runs.Whole, textX, y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
    }

    /// <summary>
    /// 编辑中的那一行也走缓存 —— 它的文本只在**击键时**变，而先前是逐帧 <c>BuildAttributed</c>
    /// 重建（重新分词 + 重切段 + 重算列号）。整份文本作键，击键即失效、不动时零成本。
    /// </summary>
    private LineRuns EditingRuns(string line)
    {
        if (_editingRuns is not null && _editingRunsFor == line) return _editingRuns;
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
        _lineCache.Remove(idx);
        _cacheOrder.Remove(idx);
        _lineWidths.Remove(idx);        // 列数缓存与行内容绑定，一并失效
        _editingRuns = null;          // 编辑行的段几何也失效（文本可能正是这一行）
        _editingRunsFor = null;
        Invalidate();
    }

    /// <summary>整份内容变了（撤销/重做/多行粘贴）——行数都可能变，缓存必须全清。</summary>
    public void InvalidateAll()
    {
        _lineCache.Clear();
        _cacheOrder.Clear();
        _lineWidths.Clear();
        _editingRuns = null;
        _editingRunsFor = null;
        Invalidate();
    }

    /// <summary>
    /// 正在编辑的那一行（1-based；-1 = 无）。
    ///
    /// **这一行同样由画布自绘**（文字取自 <see cref="EditingText"/>、光标取自
    /// <see cref="EditingCursor"/>）。原先是让原生 <c>Entry</c> 显示这一行、画布跳过它，
    /// 结果是两层各按自己的规则算位置（画布用「行顶+基线补偿」，Entry 用它自己的内边距），
    /// 必然错位。现在 <c>Entry</c> 只作为**输入法通道**存在（文字透明），显示层只有一套。
    /// </summary>
    public long EditingLine { get; set; } = -1;

    /// <summary>编辑行的当前文本（由页面的输入框实时同步过来）。</summary>
    public string? EditingText { get; set; }

    /// <summary>编辑行的光标位置（UTF-16 码元下标）。</summary>
    public int EditingCursor { get; set; }

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

    private (float Start, float Length) VerticalThumb(float h)
    {
        float track = Math.Max(1f, h - 2 * EditorTypography.BarMargin);
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
    private (float Left, float Track) HorizontalTrack(float w)
    {
        float left = GutterWidth() + EditorTypography.BarMargin;
        return (left, Math.Max(1f, w - left - EditorTypography.BarMargin));
    }

    /// <summary>横向滑块的（起点, 长度），坐标是画布横向。</summary>
    private (float Start, float Length) HorizontalThumb(float w)
    {
        var (left, track) = HorizontalTrack(w);
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
    private void DrawScrollbars(ICanvas canvas, float w, float h)
    {
        bool active = _dragBar != Bar.None;
        float thick = active ? EditorTypography.BarThick : EditorTypography.BarThin;

        canvas.FillColor = _isDark
            ? (active ? EditorTypography.BarActiveDark : EditorTypography.BarIdleDark)
            : (active ? EditorTypography.BarActive : EditorTypography.BarIdle);

        if (_doc != null && _doc.LineCount > VisibleLines)
        {
            var (y, len) = VerticalThumb(h);
            canvas.FillRoundedRectangle(w - EditorTypography.BarMargin - thick, y, thick, len, thick / 2);
        }

        if (ComputeMaxScrollX() > 0)
        {
            var (x, len) = HorizontalThumb(w);
            canvas.FillRoundedRectangle(x, h - EditorTypography.BarMargin - thick, len, thick, thick / 2);
        }
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
        var text = $"{LastDrawMs:F1}ms 峰{_drawMsPeak:F1} X{_scrollX:F0}/{ComputeMaxScrollX():F0}"
                 + $" w{_charWidth:F1}/{_wideCharWidth:F1}"
                 + $" {_dragBar} H{_drawH:F0} d({_downX:F0},{_downY:F0}) w{_drawW:F0}";
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
        float margin = 48f;

        float left = caretX - _scrollX;
        if (left > viewW - margin) _scrollX = caretX - viewW + margin;
        else if (left < margin) _scrollX = Math.Max(0, caretX - margin);
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
        float x = 0;
        int i = 0;
        foreach (var r in line.EnumerateRunes())
        {
            if (i >= limit) break;
            x += AdvanceOf(r, x);
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

        // **<see cref="MeasurePrefixWidth"/> 的逆**：同样逐字形累加实测推进量。
        // 落在一个字形格子的**前半** → 归到它前面；后半 → 归到它后面。
        // 两边必须用同一套推进量，否则「点哪儿」与「光标画哪儿」会差一格。
        float x = 0;
        int i = 0;
        foreach (var r in line.EnumerateRunes())
        {
            float adv = AdvanceOf(r, x);
            if (xInLine < x + adv * 0.5f) return i;
            x += adv;
            i += r.Utf16SequenceLength;
        }
        return line.Length;
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
    private void MeasureAdvances(ICanvas canvas)
    {
        float lat, wide;
        try
        {
            lat = (float)canvas.GetStringSize("0", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
            wide = (float)canvas.GetStringSize("中", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
        }
        catch { lat = 0; wide = 0; }

        // 量不到就退回设计值 —— 宁可差一点，也不能让字宽变成 0（除零会把整屏算崩）
        if (lat <= 0) lat = Math.Max(1f, EditorTypography.HalfWidth);
        if (wide < lat) wide = lat * 2f;

        _charWidth = lat;
        _wideCharWidth = wide;
    }

    /// <summary>
    /// 单个码元占多宽。半角/全角的判据与列模型**同一个**（<c>AnsiString.CharWidth</c>，
    /// 全仓唯一真源）；Tab 推进到下一个 tab stop，与 <c>ExpandTabs</c> 的列语义一致。
    /// </summary>
    private float AdvanceOf(Rune r, float currentX)
    {
        if (r.Value == '\t')
        {
            float stop = EditorTypography.TabColumns * _charWidth;
            return stop <= 0 ? 0 : (MathF.Floor(currentX / stop) + 1) * stop - currentX;
        }
        return WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r) > 1 ? _wideCharWidth : _charWidth;
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
