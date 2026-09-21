using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
// ⚠ `WayCoder.UI.Shared.Terminal` 里也有个 `Color`（终端色码那条链的类型）——
//   这里要的是绘图用的那个，显式取别名（本仓在 CommandPanelPage 上踩过一次同样的 CS0104）。
using Color = Microsoft.Maui.Graphics.Color;

namespace WayCoder.Maui.Controls;

/// <summary>
/// **自绘的字符网格** —— 命令行页用来显示"画面"（全屏程序的输出）的那一块。
///
/// ## 为什么不能用 <c>Label</c>（这是本仓付过学费的结论）
///
/// 画面的一行是 `|␣␣␣␣␣␣␣␣␣␣␣␣␣␣␣␣|` 这种"**用空格撑出来的位置**"，
/// 而 `Label` 把这段交给平台的排版器去量。平台那边至少三层会改写宽度：
///
///   · **连续空格被折叠**（实测：`+---+` 上边框右端在 x≈455，中间几行的右竖线在 250~300，
///     而且**每行还都不一样**）—— 看着像"字体不是等宽"，其实是空格宽度被改写；
///   · 换 NBSP 绕开折叠之后，**行宽仍比上边框短约 7 个字符**（NBSP 可能走了字体回退，
///     度量与主字体不同）；
///   · 再叠上 `LineBreakMode` / 空白裁剪之类的通用规则。
///
/// 移动端编辑器在 v0.96.114~121 为同一件事折腾过八轮，最后定的是
/// **「自建网格模型，尺子只有一把：位置一律自己算，不看字体度量」**（见 CLAUDE.md）。
/// 字符网格与编辑器是同一类东西 —— 每格宽度必须是**常量**。所以这里照那条走：
///
///   · 格宽 = 字号 × 0.5，格高 = 字号 × 1.2（**设计值，不是实测值**）；
///   · 每段（同色连续文本）按**它自己的起始列**定位 ⇒ 即使某处字形有偏差，
///     **误差也不会跨段累积**（这正是编辑器那条的关键）。
///
/// 与编辑器的差别：这里**没有编辑**，所以不需要浮动输入框、光标、选区那一套 ——
/// 只画，不交互。
/// </summary>
public class TerminalGrid : GraphicsView
{
    /// <summary>格宽 ÷ 字号。等宽字体的半角推进量就是 0.5em（Sarasa Mono SC 的设计值）。</summary>
    private const double CellWFactor = 0.5;

    /// <summary>格高 ÷ 字号（行距 1.2）。</summary>
    private const double CellHFactor = 1.2;

    private readonly GridDrawable _drawable = new();

    private IReadOnlyList<string> _lines = [];
    private double _fontSize = 12;
    private bool _isDark = true;

    /* ── 滚动：**自己滚**（外面不套 `ScrollView`）──
     *
     * 用户点破的那条：「编辑器应该滚动条都是自己画的，所以没使用 scrollview 吧」——
     * 对，编辑器正文就是一个裸 `CodeCanvasView`。**不套 `ScrollView` 的好处是没人跟画布
     * 抢触摸流**：套着的时候双指的第二根手指会被滚动容器截走，捏合根本不触发（实测）。
     * 滚动条也用同一套画笔自己画（`EditorTypography.BarIdle/Active` 那组颜色），
     * 于是"系统那条叠成两层""轨道几何两处各算一遍"这些问题一并消失。 */
    private double _scrollX;
    private double _scrollY;
    private double _contentW;
    private double _contentH;
    private bool _followEnd = true;              // 贴着底时才跟着新内容走（终端的老规矩）
    private PointF _dragLast;
    private bool _dragging;

    /// <summary>内容高（像素）—— 滚动边界与滚动条都按它算。</summary>
    public double ContentHeight => _contentH;

    /// <summary>视口高（像素）。</summary>
    public double ViewportHeight => Height;

    /// <summary>当前纵向偏移。</summary>
    public double ScrollY => _scrollY;

    /// <summary>滚到底（贴底跟随时用）。</summary>
    public void ScrollToEnd()
    {
        _scrollY = Math.Max(0, _contentH - ViewportHeight);
        _scrollX = 0;                     // 纵向跟底时横向**左对齐**（终端语义）
        _followEnd = true;
        Invalidate();
    }

    /// <summary>
    /// 回到**左上角**（内容坐标原点）—— 「固定屏幕」那一档用它。
    ///
    /// 用户定的两种屏幕模式的落点（原话）：
    ///   · **固定屏幕**（行列都钉死）= 老显示器：屏幕就是 <c>行×列</c> 那一块，
    ///     所以画面要**贴屏幕左上角**对齐，多出来的部分靠滚动条看；
    ///   · **行列不固定**（滚屏）= 真终端：屏幕跟着内容走，默认停在**最后一屏**。
    ///
    /// ⚠ 内容比视口**小**时两种模式落点是同一个：横向左对齐、纵向顶部对齐
    ///   （用户点名的「只要内容小于窗口，横向左对齐，纵向顶部对齐」）——
    ///   这在两个方法里都是**自然结果**：`Math.Max(0, …)` 与 `_scrollX = 0`
    ///   在装得下时都算 0。别为它再写一条特判。
    /// </summary>
    public void ScrollToHome()
    {
        _scrollX = 0;
        _scrollY = 0;
        _followEnd = false;               // 钉在顶上，新内容不该把它拽走
        Invalidate();
    }

    /// <summary>
    /// 双指缩放的**目标字号**（`字号 = 起始字号 × 两指距离比`）。
    ///
    /// ⚠ 不用 `PinchGestureRecognizer` 的理由见 <see cref="Tapped"/>：往这个画布上挂
    ///   **任何**手势识别器都会让 `Start/Drag/EndInteraction` 集体失效 ⇒ 捏合自然也废。
    ///   这里从 `DragInteraction` 的**两个触点**自己算距离比（编辑器就是这么做缩放的）。
    /// </summary>
    public event Action<double>? PinchScaled;

    /// <summary>
    /// **点了一下**（单指、几乎没移动）。
    ///
    /// ⚠⚠ **绝不能用 `TapGestureRecognizer` 来实现这件事**（本仓实测踩到的硬约束）：
    ///   只要往这个 `GraphicsView` 上挂**任何**手势识别器，`StartInteraction` /
    ///   `DragInteraction` / `EndInteraction` 就**一个都不再触发** —— 平台那一层的触摸
    ///   被手势系统接走之后，画布自己这套事件就再也收不到，"滑不动、捏不动"就是这么来的。
    ///   反证：编辑器画布（`CodeCanvasView`）**一个手势识别器都没挂**，它一直是好的；
    ///   命令行页当初为了"点一下聚焦输入框"给它挂了个 `TapGestureRecognizer`，于是整块画布
    ///   对触摸**完全没有反应**（看着就像界面卡死 —— 用户报的"好像卡死了"）。
    ///   要点"点一下"，就在自己的 `EndInteraction` 里按位移判（编辑器也是这么做的）。
    /// </summary>
    public event Action? Tapped;

    /// <summary>
    /// **捏合结束**（手指离开，或手势被系统打断）时触发一次。
    ///
    /// 为什么必须有：缩放期间每变一档都要**落盘 + 按新字号重新折行**（`ShellWrap` 要把
    /// 所有行重切一遍），这两件事都不该跟着每个触摸事件做（Android 上可达 120~240Hz）。
    /// 而一次性的收尾动作挂在"结束"上时，**"被打断"也是一条结束路径** ——
    /// 来电 / 切走 App / 父容器截走触摸都要当成正常结束（本仓在移动端编辑器那轮踩过：
    /// 只清状态不发结束事件，屏幕上的字号明明变了、下次打开又变回去，用户视角是"改了没保存"）。
    /// </summary>
    public event Action? PinchEnded;

    private float _pinchStartDist;
    private double _movedDist;
    private double _pinchStartFont = 12;

    public TerminalGrid()
    {
        Drawable = _drawable;
        // ⚠ **必须 Fill**。这里原先写的是 `Start`（= 按自身期望尺寸摆放），那是
        //   "外面还套着 ScrollView、怕被居中"时的遗留 —— 套着时它还有个内容尺寸，
        //   去掉之后 `GraphicsView` **没有固有尺寸**（它不 Measure 任何东西）⇒ 期望尺寸 0
        //   ⇒ 视口 0×0：滚动条不出现（`DrawBars` 在 vw<=0 时早退）、滚动边界算成"内容全高"、
        //   而内容**照样画得出来**（绘制不依赖 `Width`，且画布不裁剪到自己的范围，
        //   见 `Draw` 里的 `ClipRectangle`）—— 三个症状各走各的，所以很难一眼归到"尺寸是 0"。
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        // 透明底：与编辑器画布一致（不画底，露出页面的主题底色）
        BackgroundColor = Colors.Transparent;

        // 触摸走 `GraphicsView` 自带的那三个事件（理由见 `PinchScaled` 的说明）
        StartInteraction += OnTouchStart;
        DragInteraction += OnTouchDrag;
        EndInteraction += OnTouchEnd;
        // 手势被系统取消（来电、切走 App、父容器截走触摸…）：**本手势的每一个状态都要清掉**，
        // 而且捏合被这样打断时**要当成一次正常结束**（理由见 `PinchEnded`）。
        CancelInteraction += (_, _) =>
        {
            bool wasPinching = _pinchStartDist > 0;
            _pinchStartDist = 0;
            _dragging = false;
            if (wasPinching) PinchEnded?.Invoke();
        };
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // 视口一变，"能滚多远"就变了（内容尺寸没变）⇒ 边界与滚动条都要重算
        if (_followEnd) _scrollY = Math.Max(0, _contentH - ViewportHeight);
        ClampScroll();
        Invalidate();
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 1 && TryHitBar(e.Touches[0], out var bar))
        {
            // 按在滚动条的滑块上 = **拖滚动条**（不是拖内容）。
            // 记下起点与当时的偏移，之后按"滑块走了多少 → 内容该滚多少"线性换算。
            _barDrag = bar;
            _barDragStart = e.Touches[0];
            _barDragStartScroll = bar == BarDrag.Vertical ? _scrollY : _scrollX;
            _dragging = false;
            return;
        }
        if (e.Touches.Length >= 2)
        {
            // 基准值**不在这里设**（懒设在 `OnTouchDrag` 里，理由见那段注释）——
            // 这里只把"单指滚动"关掉：缩放期间不许再拿第一根手指当滚动用。
            _dragging = false;
            // ⚠ **第二根手指落下要撤销"拖滚动条"** —— 第一根手指正好落在滑块上是常事
            //   （滑块就贴着屏幕右边），不撤销的话下面的 `OnTouchDrag` 会先看 `_barDrag`
            //   并早退，把整个捏合**劫持成拖滚动条**：用户想缩放，画面却在滚。（实测拦下）
            _barDrag = BarDrag.None;
        }
        else if (e.Touches.Length == 1)
        {
            _dragLast = e.Touches[0];
            _movedDist = 0;
            _dragging = true;
        }
    }

    private void OnTouchDrag(object? sender, TouchEventArgs e)
    {
        // 拖滚动条（**优先于一切**：滑块在内容之上，按到它就不该再拖内容）
        if (_barDrag != BarDrag.None && e.Touches.Length >= 1)
        {
            var q = e.Touches[0];
            if (_barDrag == BarDrag.Vertical)
            {
                var (track, thumb) = VerticalBarGeometry();
                if (track - thumb > 0.5)
                    _scrollY = _barDragStartScroll
                             + (q.Y - _barDragStart.Y) * Math.Max(0, _contentH - ViewportHeight) / (track - thumb);
            }
            else
            {
                var (track, thumb) = HorizontalBarGeometry();
                if (track - thumb > 0.5)
                    _scrollX = _barDragStartScroll
                             + (q.X - _barDragStart.X) * Math.Max(0, _contentW - Width) / (track - thumb);
            }
            ClampScroll();
            _followEnd = false;      // 手动拖过滚动条就不再跟底（同"往上翻过"的规矩）
            Invalidate();
            return;
        }

        // 双指 = 缩放（**优先**：缩放的每一拍都在变，不能同时当成滚动）
        if (e.Touches.Length >= 2)
        {
            _dragging = false;
            float d = Distance(e.Touches[0], e.Touches[1]);
            if (d <= 1) return;
            // ⚠⚠ **捏合基准必须在这里"懒设"，不能只靠 `StartInteraction`** ——
            //   这是照编辑器抄的那一条（`CodeCanvasView.OnDrag` 里同一个写法）。
            //   第二根手指落下时，平台**不一定**再发一次 `StartInteraction`
            //   （实测：`Start` 只在第一根手指按下时来一次），于是 `_pinchStartDist` 一直是 0，
            //   下面那句早退就把**每一拍**都吃掉 ⇒ 屏幕上表现就是"**捏不动**"，
            //   而且不报错、不崩、单指滚动一切正常 —— 最难查的那种。
            //   所以：第一次看见双指的那一拍只记基准、**不缩放**，之后每拍按距离比算。
            if (_pinchStartDist <= 0)
            {
                _pinchStartDist = d;
                _pinchStartFont = _fontSize;
                return;
            }
            PinchScaled?.Invoke(_pinchStartFont * (d / _pinchStartDist));
            return;
        }

        // 单指 = 滚动（像素跟手；`ScrollView` 没了，这就是唯一的滚动入口）
        if (!_dragging || e.Touches.Length == 0) return;
        var p = e.Touches[0];
        _movedDist += Math.Abs(p.X - _dragLast.X) + Math.Abs(p.Y - _dragLast.Y);
        _scrollX -= p.X - _dragLast.X;
        _scrollY -= p.Y - _dragLast.Y;
        _dragLast = p;
        ClampScroll();
        // 只有**本来就贴着底**才继续跟底：往上翻过之后新内容不该把他拽回去（终端的老规矩）
        _followEnd = _scrollY >= Math.Max(0, _contentH - ViewportHeight) - 1;
        Invalidate();
    }

    private void OnTouchEnd(object? sender, TouchEventArgs e)
    {
        bool wasPinching = _pinchStartDist > 0;
        bool wasBarDrag = _barDrag != BarDrag.None;
        // 单指、几乎没动 = 点了一下（**不用 `TapGestureRecognizer`**，理由见 `Tapped`）
        // 拖滚动条不算"点了一下"（`_dragging` 在 `OnTouchStart` 里就没置位，这里再兜一道）
        if (_dragging && !wasPinching && !wasBarDrag && _movedDist < 10) Tapped?.Invoke();
        _pinchStartDist = 0;
        _dragging = false;
        _barDrag = BarDrag.None;
        if (wasPinching) PinchEnded?.Invoke();
    }

    // ── 滚动条：**画**与**拖**共用同一份几何 ──────────────────────────
    //
    // ⚠ 几何只此一份（`VerticalBarGeometry` / `HorizontalBarGeometry`）：画的时候用、
    //   命中判定用、拖动换算用，全走它们。分头算的后果本仓记过很多次 ——
    //   滚动条画在一处、热区在另一处，差几个像素就是"看得见却点不中、点中了却对不上"。
    //
    // 条的**可见宽度只有 3**，手指根本按不上去 ⇒ 命中判定要**向外扩一大圈**
    // （编辑器那条滚动条的热区就是 20pt 宽的外扩，这里照抄那个做法）。

    /// <summary>条宽（可见）。</summary>
    private const float BarThickness = 3f;

    /// <summary>条与视口边缘的间距。</summary>
    private const float BarInset = 3f;

    /// <summary>命中判定的外扩量（手指 vs 3 像素的条）。</summary>
    private const float BarSlop = 22f;

    /// <summary>滑块最短长度 —— 内容再长也要留一个能按住的东西。</summary>
    private const float BarMinThumb = 24f;

    private enum BarDrag { None, Vertical, Horizontal }

    private BarDrag _barDrag;
    private PointF _barDragStart;
    private double _barDragStartScroll;

    /// <summary>竖条的（轨道长，滑块长）—— 内容不超出视口时返回 0。</summary>
    private (float Track, float Thumb) VerticalBarGeometry()
    {
        float vh = (float)Height;
        if (vh <= 0 || _contentH <= vh) return (0, 0);
        float track = vh - BarInset * 2;
        float thumb = Math.Max(BarMinThumb, track * (float)(vh / _contentH));
        return (track, Math.Min(thumb, track));
    }

    /// <summary>横条的（轨道长，滑块长）。</summary>
    private (float Track, float Thumb) HorizontalBarGeometry()
    {
        float vw = (float)Width;
        if (vw <= 0 || _contentW <= vw) return (0, 0);
        float track = vw - BarInset * 2;
        float thumb = Math.Max(BarMinThumb, track * (float)(vw / _contentW));
        return (track, Math.Min(thumb, track));
    }

    /// <summary>滑块矩形（画与拖都用它）—— 返回 false = 这条不该出现。</summary>
    private bool VerticalThumbRect(out RectF rect)
    {
        var (track, thumb) = VerticalBarGeometry();
        rect = default;
        if (track <= 0) return false;
        float maxScroll = (float)Math.Max(0, _contentH - ViewportHeight);
        float t = maxScroll <= 0 ? 0 : (float)(_scrollY / maxScroll);
        // 滑块左缘 = 视口右边内侧；热区另行外扩（见 `TryHitBar`）
        rect = new RectF((float)Width - BarThickness - BarInset,
                         BarInset + t * (track - thumb), BarThickness, thumb);
        return true;
    }

    /// <summary>横条的滑块矩形。</summary>
    private bool HorizontalThumbRect(out RectF rect)
    {
        var (track, thumb) = HorizontalBarGeometry();
        rect = default;
        if (track <= 0) return false;
        float maxScroll = (float)Math.Max(0, _contentW - Width);
        float t = maxScroll <= 0 ? 0 : (float)(_scrollX / maxScroll);
        rect = new RectF(BarInset + t * (track - thumb),
                         (float)Height - BarThickness - BarInset, thumb, BarThickness);
        return true;
    }

    /// <summary>按点在不在滑块（**含外扩热区**）上 —— 在就返回是哪一条。</summary>
    private bool TryHitBar(PointF p, out BarDrag which)
    {
        which = BarDrag.None;
        // 竖条优先：它贴着右边，横向内容再宽也不会和手指的常规落点打架
        if (VerticalThumbRect(out var v))
        {
            var hot = new RectF(v.X - BarSlop, v.Y - BarSlop / 2, v.Width + BarSlop, v.Height + BarSlop);
            if (hot.Contains(p)) { which = BarDrag.Vertical; return true; }
        }
        if (HorizontalThumbRect(out var h))
        {
            var hot = new RectF(h.X - BarSlop / 2, h.Y - BarSlop, h.Width + BarSlop, h.Height + BarSlop);
            if (hot.Contains(p)) { which = BarDrag.Horizontal; return true; }
        }
        return false;
    }

    private void ClampScroll()
    {
        double maxY = Math.Max(0, _contentH - ViewportHeight);
        double maxX = Math.Max(0, _contentW - Width);
        _scrollY = Math.Clamp(_scrollY, 0, maxY);
        _scrollX = Math.Clamp(_scrollX, 0, maxX);
    }

    /// <summary>
    /// 光标：画在**第几行、第几列**（行是显示行号，-1 = 不画）。
    ///
    /// 终端里的光标是**程序的状态**，不是内容 —— 全屏程序每帧都会把光标摆到"下一个字符
    /// 要落在哪"，用户看到它就知道程序停在哪。用户点名的：「光标位置也要显示光标，
    /// 除非指令关闭了光标」—— 后半句对应 `ESC[?25l`（DECTCEM），由 `FrameBuffer` 解析，
    /// 结果显示为 <paramref name="visible"/> 为 false。
    /// </summary>
    public void SetCursor(int line, int col, bool visible)
    {
        int newLine = visible ? line : -1;
        if (_cursorLine == newLine && _cursorCol == col) return;
        _cursorLine = newLine;
        _cursorCol = col;
        Invalidate();
    }

    private int _cursorLine = -1;
    private int _cursorCol;

    /// <summary>格宽（像素）—— 由字号**算**出来，不问平台。</summary>
    public double CellWidth => Math.Max(1, _fontSize * CellWFactor);

    /// <summary>格高（像素）。</summary>
    public double CellHeight => Math.Max(1, _fontSize * CellHFactor);

    /// <summary>
    /// 装内容：`markupLines` 是**已经是中间格式**（`«»` 标记）的行，一行 = 屏幕上一行。
    ///
    /// 只解 `«»` 标记（<see cref="MarkdownParser.ParseMarkupOnly"/>）—— 画面是数据，不是文档：
    /// 走 Markdown 会被**逐行 Trim** 掉行首空格，横向位置当场全丢。
    /// </summary>
    public void SetLines(IReadOnlyList<string> markupLines, double fontSize, bool isDark)
    {
        _lines = markupLines;
        _fontSize = fontSize;
        _isDark = isDark;

        int cols = 0;
        foreach (var l in markupLines)
            cols = Math.Max(cols, ShellWrap.VisibleWidth(l));

        // 尺寸自己定：宽 = 最宽行的列数 × 格宽，高 = 行数 × 格高。
        // ⚠ 这两个数**必须与绘制用的是同一套尺子**（同一个 `CellWidth/CellHeight`），
        //   否则滚动条会与实际内容对不上（本仓记过："同一件事两处实现"）。
        _contentW = Math.Max(1, cols * CellWidth);
        _contentH = Math.Max(1, markupLines.Count * CellHeight);
        // ⚠ 这里**不设 WidthRequest/HeightRequest**（旧版设过）：画布的尺寸由布局给
        //   （它现在占满输出区），内容尺寸另有 `_contentW/_contentH` —— 两者不是一回事，
        //   混用会让"视口 = 内容"从而永远不需要滚动。
        if (_followEnd) _scrollY = Math.Max(0, _contentH - ViewportHeight);
        ClampScroll();

        _drawable.Grid = this;
        Invalidate();
    }

    /// <summary>清空（`cls` / 切会话时用）。</summary>
    public void Clear() => SetLines(Array.Empty<string>(), _fontSize, _isDark);

    /// <summary>
    /// **只改字号、不换内容** —— 双指缩放期间走这条。
    ///
    /// ⚠ 与 <see cref="SetLines"/> 分开是必须的：缩放的每一拍都重建视图的话，
    ///   **正在接手势的那个视图会被销毁**，手势当场断掉（实测：捏一下就没反应了）。
    ///   内容本来就是缓存着的（`_lines`），重画一遍就行。
    /// </summary>
    public void SetFontSize(double fontSize)
    {
        if (Math.Abs(_fontSize - fontSize) < 0.01) return;
        var lines = _lines;
        SetLines(lines, fontSize, _isDark);
    }

    private sealed class GridDrawable : IDrawable
    {
        public TerminalGrid? Grid { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var g = Grid;
            if (g == null || g._lines.Count == 0) return;

            double cw = g.CellWidth;
            double ch = g.CellHeight;
            double size = g._fontSize;
            bool dark = g._isDark;

            // ⚠⚠ **字体必须显式设**，而且要用编辑器那份（`EditorTypography.CanvasFont`）——
            //   三个名字互不相同、写错**只静默回落、不报错**（本仓记过的"字体名一坑三吃"）：
            //     Android → 资产文件名 `SarasaMonoSC-Regular.ttf`（走 CreateFromAsset）
            //     iOS     → PostScript 名 `Sarasa-Mono-SC-Regular`（UIFont.FromName）
            //     桌面    → 族名
            //   `EditorTypography` 已经把这套按平台的取值收在一处，**照用即可，别再写字面量**。
            //
            //   漏了这一步的症状正是用户报的「**字体还没换对**」——不设 `Font` 时画布用
            //   **默认比例字体**：格子位置是对的（自绘的），但字形宽窄不齐、看着像没对齐。
            canvas.Font = EditorTypography.CanvasFont;
            canvas.FontSize = (float)size;

            // 视口为 0 = 还没布局好，这一帧没什么可画的（**必须早退**：下面按
            // `_scrollY / ch` 和 `_scrollY + vh` 算可见行区间，`vh == 0` 会让上界算成
            // 无穷大、`(int)` 溢出成 `int.MinValue` ⇒ 一行都不画；边界值要在这里拦住，
            // 别留给下面的算式 —— 本仓那条「收口几何计算的辅助函数要把边界守卫一起收进去」）。
            float vw = (float)g.Width, vh = (float)g.Height;
            if (vw <= 0 || vh <= 0) return;

            // 自己滚：整幅内容按偏移平移（外面没有 `ScrollView` 替我们做这件事）
            canvas.SaveState();

            // ⚠⚠ **必须自己裁剪到自己的范围**，否则内容会画到画布外面去。
            //   平台**不会**替我们裁：`View.draw` 的 canvas 并不裁到本视图的 bounds
            //   （Android 只在 `ViewGroup` 那一层裁"子视图超出父容器"的部分，不管子视图
            //   自己往外画）。实测症状：往上滚之后，**滚出上边的内容压在顶栏那几个按钮上**
            //   （"自适应 50 列 / 固定 80×25 / 清屏"被一片蓝色盖住一半），
            //   看着像界面错乱，其实是画布没裁 —— 而它跟"触摸收不到"是**两个**毛病，
            //   当初把两个症状看成一个，白绕了一圈。
            canvas.ClipRectangle(0, 0, vw, vh);

            canvas.Translate((float)-g._scrollX, (float)-g._scrollY);

            // 视口裁剪：只画**看得见的那几行**。输出动辄几千行，逐行解析 `«»` 标记再交给
            // 平台排版，全画一遍是纯浪费（编辑器的 `CodeCanvasView` 同理，见它的虚拟滚动）。
            int first = Math.Max(0, (int)Math.Floor(g._scrollY / ch));
            int last = Math.Min(g._lines.Count - 1, (int)Math.Ceiling((g._scrollY + vh) / ch));

            for (int li = first; li <= last; li++)
            {
                double y = li * ch;
                // ⚠ `DrawString` 的 y 是**基线**位置（本仓在自绘编辑器上踩过：
                //   第 1 行会被画到画布上方看不见）。这里用 VerticalAlignment.Top 的框式画法，
                //   把整行框定在 [y, y+ch)，由引擎按框顶对齐，绕开基线补偿那套手工换算。
                int col = 0;

                foreach (var (text, color, bg) in MarkdownParser.ParseMarkupOnly(g._lines[li]))
                {
                    if (text.Length == 0) continue;

                    if (bg >= 30)
                    {
                        // 底色按**格**铺满整段（含空格）—— 空格也要铺，老程序整屏都是这么画的
                        int w = 0;
                        foreach (var r in text.EnumerateRunes()) w += Math.Max(1, AnsiString.CharWidth(r));
                        canvas.FillColor = ResolveColor(bg, dark ? Colors.Black : Colors.White);
                        canvas.FillRectangle((float)(col * cw), (float)y, (float)(w * cw), (float)ch);
                    }

                    var fg = ResolveColor(color, dark ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#1A1A1A"));
                    // 逐**段**定位：起点按"这一段自己的起始列"算 ⇒ 误差不跨段累积
                    canvas.FontColor = fg;
                    canvas.DrawString(text, (float)(col * cw), (float)y, (float)(text.Length * cw * 2), (float)ch,
                        HorizontalAlignment.Left, VerticalAlignment.Top);

                    foreach (var r in text.EnumerateRunes()) col += Math.Max(1, AnsiString.CharWidth(r));
                }
            }

            // 光标：内容之后、裁剪解除之前画（它属于内容坐标系，要跟着一起滚）
            DrawCursor(canvas, g, cw, ch);

            canvas.RestoreState();
            DrawBars(canvas, g);
        }

        /// <summary>
        /// 画**光标** —— 一个半透明的方块，盖在它所在的那一格上。
        ///
        /// 为什么不画"下划线"或"竖线"：终端的默认光标就是**反白方块**，而反白要交换前后景，
        /// 我们这边前景/背景都是逐段解析出来的，交换得逐段改（`«»` 标记是纯文本，
        /// 得重新拼一遍）。半透明方块是等价观感里最省事、也最不会跟内容打架的做法。
        ///
        /// ⚠ 画在 `RestoreState` **之前**（即内容坐标系里）—— 它要跟着内容一起滚，
        ///   不能像滚动条那样钉在视口上。裁剪也还生效着，滚出去的光标自然不画。
        /// </summary>
        private static void DrawCursor(ICanvas canvas, TerminalGrid g, double cw, double ch)
        {
            if (g._cursorLine < 0 || g._cursorLine >= g._lines.Count) return;
            canvas.SaveState();
            canvas.FillColor = g._isDark ? Color.FromRgba(255, 255, 255, 110) : Color.FromRgba(0, 0, 0, 90);
            canvas.FillRectangle((float)(g._cursorCol * cw), (float)(g._cursorLine * ch), (float)cw, (float)ch);
            canvas.RestoreState();
        }

        /// <summary>
        /// 自己画滚动条（**只在内容超出视口时出现**）。
        /// 颜色取编辑器那一组（`BarIdle`/`BarIdleDark`）—— 同一种东西不要两套配色。
        /// </summary>
        private static void DrawBars(ICanvas canvas, TerminalGrid g)
        {
            if (g.Width <= 0 || g.Height <= 0) return;

            // 几何**取自外面那几个方法**（`VerticalThumbRect` / `HorizontalThumbRect`）——
            // 画在这里、命中判定在 `TryHitBar`、拖动换算在 `OnTouchDrag`，三处同源。
            // 正在拖的那条画成"激活色"（与编辑器同一组配色），手感上能看出抓住了。
            canvas.FillColor = g._isDark ? EditorTypography.BarIdleDark : EditorTypography.BarIdle;
            if (g.VerticalThumbRect(out var v)) canvas.FillRoundedRectangle(v.X, v.Y, v.Width, v.Height, v.Width / 2);
            if (g.HorizontalThumbRect(out var h)) canvas.FillRoundedRectangle(h.X, h.Y, h.Width, h.Height, h.Height / 2);

            var active = g._isDark ? EditorTypography.BarActiveDark : EditorTypography.BarActive;
            canvas.FillColor = active;
            if (g._barDrag == BarDrag.Vertical && g.VerticalThumbRect(out var av))
                canvas.FillRoundedRectangle(av.X, av.Y, av.Width, av.Height, av.Width / 2);
            if (g._barDrag == BarDrag.Horizontal && g.HorizontalThumbRect(out var ah))
                canvas.FillRoundedRectangle(ah.X, ah.Y, ah.Width, ah.Height, ah.Height / 2);
        }

        /// <summary>
        /// 色码 → 颜色。与 `MarkupToFormattedString.ResolveColor` **同一套约定**
        /// （真彩 `≥0x1000000`、16 色查表、256 色走 xterm 算法）。
        /// ⚠ 这里**自己解析**而不是复用那份：那份在 `MarkupToFormattedString`（MAUI 侧、
        ///   面向前景/背景两个 `Color`），返回值语义不同。色码约定本身是**跨端契约**
        ///   （`«»` 中间格式），两边各自实现对得上就行 —— 但**改了要两边一起改**。
        /// </summary>
        private static Color ResolveColor(int code, Color fallback)
        {
            if (code >= 0x1000000)
                return Color.FromRgb((code >> 16) & 0xFF, (code >> 8) & 0xFF, code & 0xFF);
            if (code >= 30 && code <= 37) return Named16(code - 30, false);
            if (code >= 90 && code <= 97) return Named16(code - 90, true);
            if (code is >= 40 and <= 47) return Named16(code - 40, false);
            if (code is >= 100 and <= 107) return Named16(code - 100, true);
            if (code is >= 16 and <= 255) return FromXterm256(code);
            return fallback;
        }

        /// <summary>16 色（与 TUI 那张表同源的取值）。</summary>
        private static Color Named16(int idx, bool bright)
        {
            if (bright) idx += 8;
            return idx switch
            {
                0 => Color.FromRgb(0x00, 0x00, 0x00),
                1 => Color.FromRgb(0xAA, 0x00, 0x00),
                2 => Color.FromRgb(0x00, 0xAA, 0x00),
                3 => Color.FromRgb(0xAA, 0xAA, 0x00),
                4 => Color.FromRgb(0x00, 0x00, 0xAA),
                5 => Color.FromRgb(0xAA, 0x00, 0xAA),
                6 => Color.FromRgb(0x00, 0xAA, 0xAA),
                7 => Color.FromRgb(0xAA, 0xAA, 0xAA),
                8 => Color.FromRgb(0x55, 0x55, 0x55),
                9 => Color.FromRgb(0xFF, 0x55, 0x55),
                10 => Color.FromRgb(0x55, 0xFF, 0x55),
                11 => Color.FromRgb(0xFF, 0xFF, 0x55),
                12 => Color.FromRgb(0x55, 0x55, 0xFF),
                13 => Color.FromRgb(0xFF, 0x55, 0xFF),
                14 => Color.FromRgb(0x55, 0xFF, 0xFF),
                _ => Color.FromRgb(0xFF, 0xFF, 0xFF),
            };
        }

        /// <summary>xterm 256 色 → RGB（6×6×6 立方 + 24 级灰阶）。与 `MarkupToFormattedString.FromXterm256` 同算法。</summary>
        private static Color FromXterm256(int code)
        {
            if (code < 16) return Named16(code % 8, code >= 8);
            if (code >= 232)
            {
                int g = 8 + (code - 232) * 10;
                return Color.FromRgb(g, g, g);
            }
            int c = code - 16;
            int r = c / 36, gg = (c / 6) % 6, b = c % 6;
            static int Lvl(int v) => v == 0 ? 0 : 55 + v * 40;
            return Color.FromRgb(Lvl(r), Lvl(gg), Lvl(b));
        }
    }
}
