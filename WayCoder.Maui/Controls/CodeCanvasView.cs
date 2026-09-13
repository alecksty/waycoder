using System.Diagnostics;
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

    /// <summary>等宽字体的单字符宽度（惰性实测一次；用它算行号栏宽度与横向滚动范围）。</summary>
    private float _charWidth = 8f;

    // ── 事件（交给页面接）──

    /// <summary>单击某一行（1-based 行号）。</summary>
    public event Action<long>? LineTapped;

    /// <summary>长按某一行（1-based）——只读模式下弹出复制/选择菜单的入口。</summary>
    public event Action<long>? LineLongPressed;

    /// <summary>选择区间变化（1-based，闭区间）。</summary>
    public event Action<long, long>? SelectionChanged;

    /// <summary>滚动/内容变化（页面据此更新状态栏）。</summary>
    public event Action? ViewChanged;

    public CodeCanvasView()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;

        StartInteraction += OnStart;
        DragInteraction += OnDrag;
        EndInteraction += OnEnd;
        CancelInteraction += (_, _) => { _dragging = false; _longPress = false; };
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
        if (_scrollX < 0) _scrollX = 0;
    }

    // ── 触摸 ──

    private float _lastX, _lastY;
    private bool _dragging, _moved, _longPress;
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
        if (!_dragging || e.Touches.Length == 0) return;
        var p = e.Touches[0];
        float dx = p.X - _lastX, dy = p.Y - _lastY;
        _lastX = p.X;
        _lastY = p.Y;

        if (Math.Abs(p.X - _downX) > 8 || Math.Abs(p.Y - _downY) > 8) _moved = true;

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

    private void OnEnd(object? sender, TouchEventArgs e)
    {
        _dragging = false;
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
            LineTapped?.Invoke(line + 1);
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
                canvas.DrawText(editing ? BuildAttributed(line) : GetAttributed(i, line),
                    textX, y + EditorTypography.TextBaselineOffset, 1_000_000f, lineH);
            }

            if (editing) DrawCaret(canvas, line, textX, y, lineH);

            DrawDiagnosticWave(canvas, i, y, textX, lineH);
        }
        canvas.RestoreState();

        // ③ 行号栏（最后画，压住横向滚出去的正文）
        DrawGutter(canvas, first, last, gutterW, h, lineH);

        if (ShowDebugHud) DrawDebug(canvas, w, h, first, last, gutterW, lineH);

        RequestPrefetch(first - 40, last + 40);

        _drawWatch.Stop();
        LastDrawMs = _drawWatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>最近一帧的绘制耗时（ms）。状态栏显示它 —— 「卡不卡」要看数字。</summary>
    public double LastDrawMs { get; private set; }

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

            // 高亮条与文字**必须是同一个 y**：文字落笔点带了 TextBaselineOffset，
            // 条少了这个偏移就会整体偏上一截（用户实测「黄条没对齐行」）。
            float y = LineY(i, lineH) + EditorTypography.TextBaselineOffset - AscentApprox;
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

    /// <summary>
    /// 自绘光标。横向位置按字符宽近似（CJK 会偏一点，但打字时光标就在插入点附近，够用）；
    /// 精确到像素需要逐字素量宽，那会让每帧多出一次文本测量。
    /// </summary>
    private void DrawCaret(ICanvas canvas, string line, float textX, float y, float lineH)
    {
        int col = Math.Clamp(EditingCursor, 0, line.Length);
        float x = textX + col * Math.Max(1f, _charWidth);
        canvas.StrokeColor = _isDark ? Colors.White : Colors.Black;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(x, y + 2, x, y + lineH - 2);
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
        float x0 = textX + from * _charWidth;
        float width = Math.Min(WaveMaxWidth, Math.Max(24f, Math.Max(1, line.Length - from) * _charWidth));
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

    private void DrawDebug(ICanvas canvas, float w, float h, long first, long last, float gutterW, float lineH)
    {
        // 等宽自检：等宽字体下 "i" 与 "W" 必须一样宽，否则说明字体回落成了比例字体
        var si = canvas.GetStringSize("i", EditorTypography.CanvasFont, EditorTypography.FontSize);
        var sw = canvas.GetStringSize("W", EditorTypography.CanvasFont, EditorTypography.FontSize);
        bool mono = Math.Abs(si.Width - sw.Width) < 0.01f;
        _charWidth = si.Width > 0 ? si.Width : _charWidth;

        var text = $"行 {first + 1}-{last}/{_doc?.LineCount} · 帧 {_lastDrawMs:F1}ms · 缓存 {_lineCache.Count}"
                 + $" · {_doc?.EncodingName} · {(mono ? "等宽✓" : "非等宽✗")}";
        canvas.FontSize = 10;
        canvas.FontColor = Colors.White;
        canvas.FillColor = Color.FromArgb("#000000AA");
        canvas.FillRectangle(0, 0, Math.Min(w, text.Length * 6f + 12), 18);
        canvas.DrawString(text, 6, 2, HorizontalAlignment.Left);
    }

    /// <summary>字体的近似 ascent（用于把「基线」换算成「行顶」）。</summary>
    private static float AscentApprox => EditorTypography.FontSize * 0.92f;

    /// <summary>
    /// 保证编辑光标落在横向视野内。编辑长行时不做这件事，打着打着光标就跑出屏幕了
    /// （自绘层不会跟着输入框内部的横向滚动走）。
    /// </summary>
    public void EnsureCaretVisible()
    {
        if (EditingLine < 0) return;

        float gutter = GutterWidth();
        float caretX = gutter + EditorTypography.TextLeftPad + EditingCursor * Math.Max(1f, _charWidth);
        float viewW = Math.Max(40f, (float)Width - gutter);
        float margin = 48f;

        float left = caretX - _scrollX;
        if (left > viewW - margin) _scrollX = caretX - viewW + margin;
        else if (left < margin) _scrollX = Math.Max(0, caretX - margin);
        Invalidate();
    }

    /// <summary>取一行的显示文本（供页面做查找高亮/状态栏）。</summary>
    public string? GetLineText(long oneBased) => _doc?.GetLine(oneBased - 1);

    /// <summary>测量等宽字符宽度（首个 Draw 之后才准）。</summary>
    public float CharWidth => _charWidth;
}
