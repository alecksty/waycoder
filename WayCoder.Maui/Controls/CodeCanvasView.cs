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
///   详见 <see cref="EditorTypography.CanvasFontName"/> 与 <c>BuildAttributed</c>。
/// - **列宽用字体的设计值（`FontSize × 0.5`），不用实测值**：Android 会把行宽取整
///   （13pt 时拉丁真值 6.5 报成 7），照实测值定位每个拉丁字符多算 0.5pt。
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
    private readonly Dictionary<long, IAttributedText> _lineCache = new();
    private readonly List<long> _cacheOrder = [];

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

    /// <summary>
    /// 逐码点的实测宽度缓存（只收集非 ASCII）。
    ///
    /// 光标定位原先按「CJK 算两列」推算 —— 但**同一个字体里不同汉字的宽度未必相同**，
    /// 全角标点更是另一回事，于是中文行的光标和输入位置会对不上（用户实测：
    /// 「汉字光标定位和输入位置对不上，英文数字基本正确」）。ASCII 仍用统一的
    /// <see cref="_charWidth"/>（等宽字体下必然相等），其余字符各量各的。
    /// </summary>
    private readonly Dictionary<int, float> _runeWidths = [];

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
        _firstLine = 0;
        _scrollX = 0;
        _velocityX = _velocityY = 0;
        _caretLine = -1;
        _selAnchor = _selEnd = -1;
        Invalidate();
    }

    public void SetDark(bool isDark) { _isDark = isDark; Invalidate(); }

    /// <summary>
    /// 排版变了（字号调整）：字宽与行高都得重新实测。行缓存不用清 —— 它存的是
    /// 「文本 + 颜色 run」，位置是绘制时按新的行高算的。
    /// </summary>
    /// <summary>作废平台布局缓存（字号/字体变了就作废）。</summary>
    private void InvalidatePlatformLayout()
    {
#if ANDROID
        _androidLayout = null;
        _androidLayoutLine = null;
#endif
    }

    public void ResetTypography()
    {
        _charWidthMeasured = false;
        _runeWidths.Clear();   // 字号变了，之前量的宽度全部作废
        InvalidatePlatformLayout();
        _scrollX = 0;
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
            // 实测宽度（与绘制同一套）。按「视觉列 × 单字宽」估算会让中文行严重偏短，
            // 横向就滚不到真正的行尾。
            float w = MeasurePrefixWidth(line, line.Length);
            if (w > maxWidth) maxWidth = w;
        }
        return Math.Max(0, maxWidth - viewW + 24);
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
        if (wasPinching) { _moved = true; return; }   // 捏合结束：不触发 tap / 惯性

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

#if DEBUG
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
            // 逐个字符 + 逐前缀：整串比「逐字之和」少，就定位到具体是哪个字被吞掉了
            sb.Append(M("文", fs)).Append(' ').Append(M("，", fs)).Append(' ');
            sb.Append(M("。", fs)).Append(' ').Append(M("！", fs)).Append(' ');
            sb.Append(M("中文", fs)).Append(' ').Append(M("中文，", fs)).Append(' ');
            sb.Append(M("中文，。", fs)).Append(' ').Append(M("中中", fs)).Append(' ');
            sb.Append(M("，，", fs)).Append(' ').Append(M("。。", fs)).Append(' ');
            sb.Append(M("！！", fs)).Append(' ').Append(M("中 ", fs)).Append(' ');
            sb.Append(M("中 x", fs)).Append(' ');
            sb.Append($"line0=[{line0}] W={canvas.GetStringSize(line0, EditorTypography.CanvasFont, fs).Width:F2}");
            // 对照实验：按**渲染那套构造**（SpannableString + TypefaceSpan + 废弃构造器）测同一批串，
            // 看能不能消掉「紧跟全角标点的全角标点只剩半个宽」这个偏差。
            foreach (var t in new[] { "中文，。！", "，，", "aB3|END", line0 })
                sb.Append($"[alt:{t}]={AltMeasure(t, fs):F2} ");
            sb.Append($"[altOne:，，]={AltMeasureOne("，，", fs):F2} ");
            // 决定性一问：这个「标点压缩」是**字号相关**的吗？
            // 若在 13 下是 52、在 26 下是 104（=52×2），说明压缩恒定 ⇒ 渲染必然是别的字号；
            // 若在 26 下是 130（=65×2），说明压缩随字号消失 ⇒ 渲染用的就是大字号。
            // 网格方案的前提：内置 Sarasa 能否在**分段绘制这条路**（Font.ToTypeface → 有 asset 分支）
            // 下加载，且中英恰好是 1em : 0.5em（= 2 列 : 1 列）。
            var sarasa = new Microsoft.Maui.Graphics.Font("SarasaMonoSC-Regular.ttf");
            string S(string t) => $"[S:{t}]={canvas.GetStringSize(t, sarasa, fs).Width:F2}";
            sb.Append(S("a")).Append(' ').Append(S("W")).Append(' ')
              .Append(S("中")).Append(' ').Append(S("a中")).Append(' ')
              .Append(S("中文，。！")).Append(' ');

            foreach (var sz in new[] { 26f, 34.125f, 52f })
                sb.Append($"[ord@{sz}]={canvas.GetStringSize("中文，。！", EditorTypography.CanvasFont, sz).Width:F2} ");

            Android.Util.Log.Info("WCW", sb.ToString());
        }
        catch (Exception ex) { Android.Util.Log.Info("WCW", "ERR " + ex.Message); }
    }

    /// <summary>
    /// 对照测量：按**渲染那条路径的构造**建布局再取宽度。
    ///
    /// MAUI 的 <c>GetStringSize</c> 用的是 <c>TextLayoutUtils.CreateLayout</c>（Builder + 裸字符串），
    /// 而 <c>PlatformCanvas.DrawText</c> 用的是 <c>CreateLayoutForSpannedString</c>（废弃构造器 +
    /// SpannableString/TypefaceSpan）。两者对「紧邻全角标点」的处理实测不同，这个对照就是为了定位它。
    /// </summary>
    private static float AltMeasure(string text, float fontSize)
    {
        try
        {
            var paint = new Android.Text.TextPaint { TextSize = fontSize };
            paint.SetTypeface(Microsoft.Maui.Graphics.Platform.FontExtensions.ToTypeface(EditorTypography.CanvasFont));
            var span = new Android.Text.SpannableString(text);
            span.SetSpan(new Android.Text.Style.TypefaceSpan(EditorTypography.CanvasFontName),
                0, text.Length, Android.Text.SpanTypes.ExclusiveExclusive);
#pragma warning disable CS0618
            var layout = new Android.Text.StaticLayout(span, paint, int.MaxValue,
                Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
#pragma warning restore CS0618
            float w = layout.GetLineWidth(0);
            layout.Dispose();
            return w;
        }
        catch (Exception ex) { Android.Util.Log.Info("WCW", "altERR " + ex.Message); return -1; }
    }

    /// <summary>同 <see cref="AltMeasure"/>，但不加 TypefaceSpan —— 用来区分「是 span 的锅」还是「是构造器的锅」。</summary>
    private static float AltMeasureOne(string text, float fontSize)
    {
        try
        {
            var paint = new Android.Text.TextPaint { TextSize = fontSize };
            paint.SetTypeface(Microsoft.Maui.Graphics.Platform.FontExtensions.ToTypeface(EditorTypography.CanvasFont));
#pragma warning disable CS0618
            var layout = new Android.Text.StaticLayout(text, paint, int.MaxValue,
                Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
#pragma warning restore CS0618
            float w = layout.GetLineWidth(0);
            layout.Dispose();
            return w;
        }
        catch (Exception ex) { Android.Util.Log.Info("WCW", "altERR " + ex.Message); return -1; }
    }
#endif

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
            double probe = canvas.GetStringSize("0", EditorTypography.CanvasFont,
                EditorTypography.FontSize).Width;
#if DEBUG
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

        // 实测一次字符宽（等宽字体下 "0" 的宽度就是所有 ASCII 的宽度）。
        // 只做一次，之后整帧都用它 —— 放到每帧测会平白多一次文本测量。
        //
        // **必须用纯 advance，不能用 GetStringSize 的原始返回值**：后者除了字形推进量，
        // 还带一份**与字数无关的平台测量余量**。把它当「一个字多宽」再逐字符累加，
        // 等于每加一个字就多算一份余量 —— 实测 1K 字符的行上点行尾，光标插到了行中间
        // （偏出十几个字符）。见 AdvanceOf。
        if (!_charWidthMeasured)
        {
            // **用字体的设计值，不用实测值。**
            //
            // Sarasa Mono 的拉丁推进量恰好 0.5em、汉字恰好 1em（这正是选它的原因：
            // 「汉字 = 2 列」的网格与字体设计天然对齐）。而 `GetStringSize` 给出的行宽是
            // **取整**过的 —— 13pt 时拉丁真值 6.5 会报成 7，照它定位等于每个拉丁字符多算
            // 0.5pt：一行 7 个拉丁就是 3.5pt，实测红标尺比墨迹右端多出约 9px，正是这个数。
            //
            // 字体是我们自己打包的，度量是已知事实，没有理由去「量一个被取整过的近似值」。
            _charWidth = EditorTypography.HalfWidth;
            _wideCharWidth = EditorTypography.FontSize;
            _charWidthMeasured = true;
        }

        // ① 光标行 / 选择行底色（在文字下面）
        DrawLineBackgrounds(canvas, first, last, gutterW, w, lineH);

        // ② 正文（裁剪在行号栏右侧，横向滚动只影响这一层）
        float textX = gutterW + EditorTypography.TextLeftPad - _scrollX;
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
                CacheRuneWidths(canvas, line);   // 顺手把非 ASCII 字符的真实宽度收进缓存
                canvas.DrawText(editing ? BuildAttributed(line) : GetAttributed(i, line),
                    textX, y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
            }

            if (editing) DrawCaret(canvas, line, textX, y, lineH);

            DrawDiagnosticWave(canvas, i, y, textX, lineH);
        }
        canvas.RestoreState();

        // ③ 行号栏（最后画，压住横向滚出去的正文）
        DrawGutter(canvas, first, last, gutterW, h, lineH);

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

        _drawWatch.Stop();
        LastDrawMs = _drawWatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>最近一帧的绘制耗时（ms）。状态栏显示它 —— 「卡不卡」要看数字。</summary>
    public double LastDrawMs { get; private set; }

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

    private void DrawGutter(ICanvas canvas, long first, long last, float gutterW, float h, float lineH)
    {
        canvas.FillColor = _isDark ? EditorTypography.GutterBgDark : EditorTypography.GutterBg;
        canvas.FillRectangle(0, 0, gutterW, h);

        canvas.Font = EditorTypography.CanvasFont;
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

    /// <summary>自绘用的近似字宽：CJK/全角算 2 列，其余 1 列（与 AnsiString.CharWidth 同语义）。</summary>
    private static int RuneWidthApprox(Rune r)
    {
        int cp = r.Value;
        bool wide = cp >= 0x1100 && (cp <= 0x115F || cp >= 0x2E80 && cp <= 0xA4CF
            || cp >= 0xAC00 && cp <= 0xD7A3 || cp >= 0xF900 && cp <= 0xFAFF
            || cp >= 0xFE30 && cp <= 0xFE4F || cp >= 0xFF00 && cp <= 0xFF60
            || cp >= 0xFFE0 && cp <= 0xFFE6);
        return wide ? 2 : 1;
    }

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

    private readonly Dictionary<string, IAttributedText> _gutterCache = [];

    /// <summary>行号的带色文本（按「文本+颜色」缓存 —— 行号字符串高度重复，逐帧重建毫无必要）。</summary>
    private IAttributedText GutterAttributed(string label, Color color)
    {
        var key = label + "|" + color.ToHex();
        if (_gutterCache.TryGetValue(key, out var cached)) return cached;

        var attr = new AttributedText(label,
        [
            new AttributedTextRun(0, label.Length, new TextAttributes
            {
                [TextAttribute.Color] = color.ToHex(),
                [TextAttribute.FontName] = EditorTypography.CanvasFontName,
            }),
        ]);
        if (_gutterCache.Count < 512) _gutterCache[key] = attr;
        return attr;
    }

    /// <summary>尚未加载的行：画一个占位符，绝不在这里等 IO（滚动会被拖成一顿一顿的）。</summary>
    private static void DrawPending(ICanvas canvas, float x, float y, float lineH)
        => canvas.DrawString("⋯", x, y, HorizontalAlignment.Left);

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

    private IAttributedText GetAttributed(long index, string line)
    {
        if (_lineCache.TryGetValue(index, out var cached)) return cached;

        var attr = BuildAttributed(line);
        if (_lineCache.Count >= MaxCachedLines)
        {
            // 简单 FIFO 淘汰：滚动时被淘汰的正好是最久没看的那批
            int drop = Math.Min(64, _cacheOrder.Count);
            for (int i = 0; i < drop; i++) _lineCache.Remove(_cacheOrder[i]);
            _cacheOrder.RemoveRange(0, drop);
        }
        _lineCache[index] = attr;
        _cacheOrder.Add(index);
        return attr;
    }

    /// <summary>把一行文本变成带颜色 run 的 <see cref="IAttributedText"/>。</summary>
    private IAttributedText BuildAttributed(string line)
    {
        // 超长行跳过分词：minified 行上跑 tokenizer 会把一帧拖到几百毫秒，
        // 而且这类行本来也没什么「语法」可高亮。
        if (line.Length > EditorTypography.MaxTokenizeChars)
            return new AttributedText(line, []);

        var display = TextEditorMath.ExpandTabs(line, EditorTypography.TabColumns);
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
        return new AttributedText(display, runs);
    }

    /// <summary>清掉某行的渲染缓存（该行被编辑后调用）。</summary>
    public void InvalidateLine(long oneBased)
    {
        long idx = oneBased - 1;
        _lineCache.Remove(idx);
        _cacheOrder.Remove(idx);
        Invalidate();
    }

    /// <summary>整份内容变了（撤销/重做/多行粘贴）——行数都可能变，缓存必须全清。</summary>
    public void InvalidateAll()
    {
        _lineCache.Clear();
        _cacheOrder.Clear();
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
    private double _lastDrawMs;

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

        // 刻意极短：长文本会被 DrawString 折行/溢出，反而把要看的数字挤没
        var text = $"X{_scrollX:F0}/{ComputeMaxScrollX():F0} w{_charWidth:F1}/{_wideCharWidth:F1}"
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

    /// <summary>测量纯 advance 时的重复次数：够长以摊薄浮点误差，又不至于每次测量太贵。</summary>
    private const int AdvanceSampleCount = 10;

    /// <summary>
    /// 单个字符的**纯 advance**（pt）—— 从 <c>GetStringSize</c> 的结果里减掉那份与字数无关的固定余量。
    ///
    /// 直接拿 <c>GetStringSize("0")</c> 当字宽是错的：那个值 = 推进量 + 平台测量余量，
    /// 而余量**每累加一次就多算一份**。列宽本身只错一点点，但 1K 个字符的行上会累积成
    /// 「点行尾却插到行中间」；行号栏、波浪线、横向滚动上限全都跟着偏。
    ///
    /// 用「n 个字 − 1 个字」再除以 n−1：常量项相减抵消，剩下的正好是推进量。
    /// 量的是<b>与绘制同一个字体、同一个字号</b>（<see cref="EditorTypography.CanvasFont"/> /
    /// <see cref="EditorTypography.FontSize"/>），所以它就是要跟的列宽。
    /// </summary>
    private static float AdvanceOf(ICanvas canvas, Rune r)
    {
        var one = r.ToString();
        var many = string.Concat(Enumerable.Repeat(one, AdvanceSampleCount));

        double w1 = canvas.GetStringSize(one, EditorTypography.CanvasFont, EditorTypography.FontSize).Width;
        double wn = canvas.GetStringSize(many, EditorTypography.CanvasFont, EditorTypography.FontSize).Width;
        if (wn <= 0) return 0;

        float advance = (float)((wn - w1) / (AdvanceSampleCount - 1));
        // 极端情况下两个测量值一样大（余量项主导）→ 退回平均值，总比 0 好（0 会被下游当「无宽度」）
        return advance > 0 ? advance : (float)(wn / AdvanceSampleCount);
    }

    /// <summary>把一行里非 ASCII 字符的真实宽度收进缓存（每个码点只测一次）。</summary>
    private void CacheRuneWidths(ICanvas canvas, string line)
    {
        foreach (var r in line.EnumerateRunes())
        {
            if (r.Value < 0x80) continue;                      // ASCII 用统一的 _charWidth
            if (_runeWidths.ContainsKey(r.Value)) continue;
            float adv = AdvanceOf(canvas, r);
            if (adv > 0) _runeWidths[r.Value] = adv;
        }
    }

    /// <summary>某个字符的绘制宽度（优先用实测值，未测到时退回近似）。</summary>
    private float RuneWidth(Rune r, ref int charCol)
    {
        if (r.Value == '\t')
        {
            int next = (charCol / EditorTypography.TabColumns + 1) * EditorTypography.TabColumns;
            float w = (next - charCol) * _charWidth;
            charCol = next;
            return w;
        }
        charCol++;
        if (r.Value < 0x80) return _charWidth;
        if (_runeWidths.TryGetValue(r.Value, out var measured)) return measured;

        // 没量过就**现在量**：绘制路径每行都会 CacheRuneWidths，但光标定位 / 点击可能落在
        // 还没绘制过的行上（比如刚跳转过去的行）。退回「近似列宽 × 单字宽」会让**同一个字符
        // 在光标那里和文字那里宽度不同**，中英混排行里越往右偏得越多。
        // 量的字体与画文字用的 run 属性同源（同一个 CanvasFont / FontSize），所以必然吻合。
        if (_measureCanvas != null)
        {
            try
            {
                float adv = AdvanceOf(_measureCanvas, r);
                if (adv > 0)
                {
                    _runeWidths[r.Value] = adv;
                    return adv;
                }
            }
            catch { /* 画布已失效就退回近似值，下次绘制会补上 */ }
        }
        return RuneWidthApprox(r) == 2 ? _wideCharWidth : _charWidth;
    }

    /// <summary>
    /// 行内第 <paramref name="charIndex"/> 个 UTF-16 码元之前的**显示宽度**（pt）。
    ///
    /// 「字符位置 → 横坐标」在整个控件里**只此一处**：逐码点走 <see cref="RuneWidth"/>，
    /// 而它量的是**与绘制同一个字体、同一个字号**下的真实宽度（<c>GetStringSize</c> 传的
    /// <see cref="EditorTypography.CanvasFont"/>/<see cref="EditorTypography.FontSize"/>，
    /// 与 <c>DrawText</c> 的 run 属性出自同一份 <see cref="EditorTypography"/>）。
    ///
    /// 绝不按「字符数 × 单字宽」估：等宽字体里中文的实测宽度**不是** ASCII 宽的整数倍
    /// （手机上比 2 倍窄、比 1 倍宽），估出来的位置在中英混排行里会越往右偏得越多，
    /// 表现为「插入位置错了一个字符」「光标越往右越偏」。
    /// </summary>
    public float MeasurePrefixWidth(string? line, int charIndex)
    {
        if (string.IsNullOrEmpty(line) || charIndex <= 0) return 0;

        int limit = Math.Min(charIndex, line.Length);

        // **整段一次测量**，而不是逐字符累加。
        // 实测（1K 字符中英 emoji 混排行）：逐字累加 9771 vs 整行 10512 —— 差 7%。
        // 也就是说「每个字符的 advance 之和」并不等于字体的实际排布，累加出来的总宽偏小，
        // 横向滚动上限跟着偏小 ⇒ 拖到最右也到不了行尾、点击位置越往右偏得越多。
        // 直接量前缀，量的字体/字号与 DrawText 的 run 属性同源，就不存在这层换算误差。
        // 首选平台布局引擎（与渲染同源）
        float platform = PrefixWidthPlatform(line, limit);
        if (platform >= 0) return platform;

        if (_measureCanvas != null && limit <= WholeMeasureMaxChars)
        {
            try
            {
                return (float)_measureCanvas.GetStringSize(line[..limit],
                    EditorTypography.CanvasFont, EditorTypography.FontSize).Width;
            }
            catch { /* 量失败就退回累加 */ }
        }

        // 超长行 / 还没画过：退回逐字符累加（不精确，但不会为一条 4MB 的行分配整段前缀）
        float acc = 0;
        int idx = 0, col = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (idx >= limit) break;
            acc += RuneWidth(rune, ref col);
            idx += rune.Utf16SequenceLength;
        }
        return acc;
    }

    // ── 平台文本布局引擎 ────────────────────────────────────────────────
    //
    // **为什么不用自己算的字宽**：AOSP 在 `TextLine` 里明确指出，**对子串单独测量 ≠ 该字符
    // 在整行里的推进量** —— 字形替换（fallback）、连字、BiDi 都会让两者不等，
    // 所以 Android 的光标定位走 `Layout#getOffsetForHorizontal`，而它的内部用的是
    // `getRunAdvance`（在整行上下文里求推进量），不是「量一段子串」。
    // 我们原先拿 `GetStringSize(line[..n])` 当行内位置，正是被否掉的那一种：
    // 纯 ASCII 看不出来（monospace 无字形替换），中文/emoji 一行就现形。
    //
    // 这里直接建平台的 `StaticLayout` 来问，量出来的就是渲染时真正用的那份布局。

#if ANDROID
    private Android.Text.StaticLayout? _androidLayout;
    private Android.Text.TextPaint? _androidPaint;
    private string? _androidLayoutLine;
    private float _androidLayoutSize;

    /// <summary>显示屏密度：MAUI 侧坐标是 dp，而平台的 Paint/Canvas 按物理像素工作。</summary>
    private static float PlatformDensity
        => (float)Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;

    private Android.Text.StaticLayout? EnsureAndroidLayout(string line)
    {
        float size = EditorTypography.FontSize;
        bool hit = _androidLayout != null && _androidLayoutLine == line && _androidLayoutSize == size;
        if (hit) return _androidLayout;

        try
        {
            float density = PlatformDensity;
            ProbeDensity = density;
            // 诊断：CreateFromAsset 到底拿到没有 —— 拿不到就是 Typeface.Default（也等价于静默回落）
            var want = Android.Graphics.Typeface.CreateFromAsset(
                Android.App.Application.Context.Assets, EditorTypography.CanvasFontName);
            ProbeTypeface = want == null ? "asset=null"
                : (want.Equals(Android.Graphics.Typeface.Default) ? "==DEFAULT" : "ok");
            if (_androidPaint == null)
            {
                _androidPaint = new Android.Text.TextPaint();
                _androidPaint.AntiAlias = true;
            }
            _androidPaint.TextSize = size * density;
            // Paint.Typeface 是只读属性，必须走 SetTypeface。
            //
            // ⚠⚠ **必须用 Typeface.Create(name, style)，不能改用 CreateFromAsset**。
            // 原因在渲染那一侧：MAUI 把 `AttributedText` 的 run 字体名转成的是
            // **Android 原生的 `TypefaceSpan(string familyName)`**
            // （见 Graphics/Platforms/Android/Text/AttributedTextExtensions.cs），
            // 而它内部同样是 `Typeface.Create(familyName, style)` —— **只认系统族名**，
            // 且**没有 asset 重载**、MAUI 也没留传 `Typeface` 的口子。
            //
            // 所以画布上的文字实际上只能用系统字体族名渲染；我们这边要是拿
            // `CreateFromAsset` 加载打包字体去测，就变成「测量用一种字体、渲染用另一种」
            // —— 实测整行宽差约 16%（红标尺直接量出来的），点击定位随之偏移。
            // **测量与渲染必须走同一个解析路径**，这比"选一个更好的字体"重要得多。
            _androidPaint.SetTypeface(Android.Graphics.Typeface.Create(
                EditorTypography.CanvasFontName, Android.Graphics.TypefaceStyle.Normal));

            // 宽度给足，避免把一行折成多行（我们自己做横向滚动，不要平台的换行）
            float width = Math.Max(1f, (line.Length + 8) * size * density);
            var builder = Android.Text.StaticLayout.Builder
                .Obtain(line, 0, line.Length, _androidPaint, (int)width);
            builder.SetIncludePad(false);
            builder.SetLineSpacing(0f, 1f);

            _androidLayout = builder.Build();
            _androidLayoutLine = line;
            _androidLayoutSize = size;
            return _androidLayout;
        }
        catch
        {
            _androidLayout = null;   // 建不出来就退回自己算
            return null;
        }
    }

    /// <summary>点击横坐标（dp，相对正文起点）→ 字符下标。返回 -1 表示平台路径不可用。</summary>
    private int CharIndexAtXPlatform(string line, float xInLine)
    {
        // 暂时停用：平台 StaticLayout 与 MAUI 的渲染路径（SpannableString + FontPaint）
        // 对同一族名的解析结果不同，实测差约 13%。在没做到「与渲染逐字节同源」之前，
        // 宁可走 MAUI 自己的 GetStringSize（与画布文字同一条路）。
        if (true) return -1;
        var layout = EnsureAndroidLayout(line);
        if (layout == null) return -1;
        try
        {
            int off = layout.GetOffsetForHorizontal(0, xInLine * PlatformDensity);
            return Math.Clamp(off, 0, line.Length);
        }
        catch { return -1; }
    }

    /// <summary>第 charIndex 个码元处的横坐标（dp，相对正文起点）。返回 -1 表示不可用。</summary>
    private float PrefixWidthPlatform(string line, int charIndex)
    {
        if (true) return -1;   // 同上：见 CharIndexAtXPlatform 的注释
        var layout = EnsureAndroidLayout(line);
        if (layout == null) return -1;
        try
        {
            float x = layout.GetPrimaryHorizontal(charIndex);
            return x / PlatformDensity;
        }
        catch { return -1; }
    }
#endif

    /// <summary>走「整段前缀测量」的字符数上限 —— 再长就退回累加，免得为一条超长行分配整段字符串。</summary>
    private const int WholeMeasureMaxChars = 8192;

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

        // 首选平台文本布局引擎：它与渲染同源，不存在「子串测量 ≠ 行内推进量」的换算误差
        int platform = CharIndexAtXPlatform(line, xInLine);
        if (platform >= 0) return platform;

        // 与 MeasurePrefixWidth 同源：**二分找「前缀宽度 ≤ x」的最大下标**。
        // 一次前缀测量就与绘制逐字对齐，不必再靠「近似字宽」推算；
        // 逐字累加那条老路（同样本文件里差 7%）只留给超长行兜底。
        if (_measureCanvas != null && line.Length <= WholeMeasureMaxChars)
            return CharIndexAtXByPrefix(line, xInLine);

        float acc = 0;
        int idx = 0;
        int col = 0;   // tab stop 用
        foreach (var rune in line.EnumerateRunes())
        {
            float w = RuneWidth(rune, ref col);
            if (xInLine < acc + w / 2f) return idx;
            acc += w;
            idx += rune.Utf16SequenceLength;
        }
        return line.Length;
    }

    /// <summary>二分前缀测量定位。落在字符前半归它、后半归下一个（与逐字累加同语义）。</summary>
    private int CharIndexAtXByPrefix(string line, float xInLine)
    {
        int lo = 0, hi = line.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            // 对齐到码点边界：切在代理对中间的话量出来的是半个字符
            if (mid > 0 && mid < line.Length && char.IsLowSurrogate(line[mid])) mid++;
            if (mid <= lo) mid = lo + 1;
            if (mid > hi) break;

            if (MeasurePrefixWidth(line, mid) <= xInLine) lo = mid;
            else hi = mid - 1;
        }
        return lo;
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
