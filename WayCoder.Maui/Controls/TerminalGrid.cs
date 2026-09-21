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

    public TerminalGrid()
    {
        Drawable = _drawable;
        // 画面是自绘的：不要让它被布局居中/拉满（ScrollView 在内容小于视口时会居中）
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
    }

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
        WidthRequest = Math.Max(1, cols * CellWidth);
        HeightRequest = Math.Max(1, markupLines.Count * CellHeight);

        _drawable.Grid = this;
        Invalidate();
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

            for (int li = 0; li < g._lines.Count; li++)
            {
                double y = li * ch;
                // ⚠ `DrawString` 的 y 是**基线**位置（本仓在自绘编辑器上踩过：
                //   第 1 行会被画到画布上方看不见）。这里用 VerticalAlignment.Top 的框式画法，
                //   把整行框定在 [y, y+ch)，由引擎按框顶对齐，绕开基线补偿那套手工换算。
                double x = 0;
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
