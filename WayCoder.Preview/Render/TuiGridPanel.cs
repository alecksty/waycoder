using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Preview.Render;

/// <summary>
/// 自绘网格面板 —— 把 FrameSnapshot（字符 + 前景/背景色）逐格绘制到 WPF 画布。
///
/// 宽字符处理（CJK/emoji 占 2 格）：
/// - cellW 取「ASCII 宽」与「CJK 宽/2」的较大者，使宽字符恰好占 2 格、ASCII 占 1 格；
/// - 宽字符的背景扩到 2 格（终端里宽字符底色占满），文字裁剪到所占格数，
///   杜绝字形略微超出 2 格而串到下一格被盖住右半。
/// </summary>
public sealed class TuiGridPanel : FrameworkElement
{
    private FrameSnapshot? _grid;
    private Typeface _typeface = new("Consolas");
    private double _fontSize = 14;
    private double _baseCellW = 10, _baseCellH = 18; // 100% 时的格宽/高
    private double _zoom = 1.0;
    private readonly Dictionary<int, SolidColorBrush> _brushes = new();

    /// <summary>面板底色（默认透明；设黑底便于诊断/截图）。</summary>
    public Brush? Background { get; set; }

    /// <summary>是否绘制单元格网格线（设计期看格子边界）。</summary>
    public bool ShowGrid { get; set; }

    /// <summary>设置待绘制网格并重绘。null = 清空。</summary>
    public void SetGrid(FrameSnapshot? grid)
    {
        _grid = grid;
        if (grid != null)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var ascii = new FormattedText("M", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                _typeface, _fontSize, Brushes.Black, dpi).Width;
            var cjk = new FormattedText("中", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                _typeface, _fontSize, Brushes.Black, dpi).Width;
            // 取较大者：保证 ASCII 落在 1 格内、宽字符恰好占 2 格（余量交给裁剪兜底）
            _baseCellW = Math.Max(ascii, cjk / 2);
            _baseCellH = new FormattedText("M", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                _typeface, _fontSize, Brushes.Black, dpi).Height;
        }
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>设置缩放倍率（0.25~4，直接重渲染：字号×缩放、格尺寸×缩放，滚动区尺寸自然正确）。</summary>
    public void SetZoom(double zoom)
    {
        _zoom = zoom;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private double CellW => _baseCellW * _zoom;
    private double CellH => _baseCellH * _zoom;
    private double FontSize => _fontSize * _zoom;

    protected override Size MeasureOverride(Size availableSize)
        => _grid == null ? new Size(0, 0) : new Size(_grid.W * CellW, _grid.H * CellH);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (Background != null)
            dc.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
        if (_grid == null) return;
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (int r = 0; r < _grid.H; r++)
        {
            double y = r * CellH;

            // ── 背景：先算每格有效背景（宽字符延续格继承宽字符背景），再合并相邻同色为整块矩形
            //    （消除逐格 DrawRectangle 的抗锯齿亚像素缝隙 —— 那是"默认看起来有网格"的根源）
            var effBg = new int[_grid.W];
            for (int c = 0; c < _grid.W; c++)
            {
                if (c > 0 && IsWide(_grid.CharAt(r, c - 1)))
                    effBg[c] = _grid.ColorAt(r, c - 1).bg; // 延续格 → 宽字符背景
                else
                    effBg[c] = _grid.ColorAt(r, c).bg;
            }
            int cc = 0;
            while (cc < _grid.W)
            {
                int bg = effBg[cc];
                int j = cc;
                while (j + 1 < _grid.W && effBg[j + 1] == bg) j++;
                if (bg > 0)
                {
                    // 背景矩形取整到整数像素边界（floor/ceil）：相邻行共享精确像素行，消除抗锯齿横缝/纵缝
                    int x0 = (int)Math.Floor(cc * CellW);
                    int x1 = (int)Math.Ceiling((j + 1) * CellW);
                    int y0 = (int)Math.Floor(r * CellH);
                    int y1 = (int)Math.Ceiling((r + 1) * CellH);
                    dc.DrawRectangle(AnsiToColor.GetBrush(bg, _brushes), null,
                        new Rect(x0, y0, x1 - x0, y1 - y0));
                }
                cc = j + 1;
            }

            // ── 前景：逐格画字符（宽字符延续格跳过，避免残留背景/重叠）
            for (int c = 0; c < _grid.W; c++)
            {
                if (c > 0 && IsWide(_grid.CharAt(r, c - 1))) continue;

                var (fg, bg) = _grid.ColorAt(r, c);
                var ch = _grid.CharAt(r, c);
                int span = IsWide(ch) ? 2 : 1; // 宽字符占 2 格
                double x = c * CellW;

                if (string.IsNullOrEmpty(ch) || ch == " ") continue;
                if (fg == 0 && bg == 0) continue;

                // 裁剪到所占格数：防字形略超 2 格串到下一格
                double textW = span * CellW;
                dc.PushClip(new RectangleGeometry(new Rect(x, y, textW, CellH)));
                var ft = new FormattedText(ch, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    _typeface, FontSize, fg > 0 ? AnsiToColor.GetBrush(fg, _brushes) : Brushes.White, dpi);
                dc.DrawText(ft, new Point(x, y));
                dc.Pop();
            }
        }

        // 设计期网格线（可选）：单元格边界
        if (ShowGrid)
        {
            var pen = new Pen(Brushes.DimGray, 1);
            double gw = _grid.W * CellW, gh = _grid.H * CellH;
            for (int c = 1; c < _grid.W; c++)
                dc.DrawLine(pen, new Point(c * CellW, 0), new Point(c * CellW, gh));
            for (int r = 1; r < _grid.H; r++)
                dc.DrawLine(pen, new Point(0, r * CellH), new Point(gw, r * CellH));
        }
    }

    /// <summary>判断字符是否为宽字符（显示宽度 ≥ 2，CJK/emoji 等）。</summary>
    private static bool IsWide(string ch)
    {
        if (string.IsNullOrEmpty(ch)) return false;
        foreach (var rune in ch.EnumerateRunes())
            return AnsiString.CharWidth(rune) >= 2;
        return false;
    }
}
