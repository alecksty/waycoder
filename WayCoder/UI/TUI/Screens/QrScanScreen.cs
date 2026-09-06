using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Screens;

/// <summary>
/// 全屏二维码扫码屏 —— 把聊天里的小 ASCII 预览放大成覆盖整个终端的大二维码给手机扫屏。
/// PushScreen 进入（由 /sync-qr 触发），Esc / q 返回上一层（ChatScreen）。
///
/// 渲染关键（用户实机反馈根因 = 反色/对比，不是大小）：
/// PNG（白底黑块）手机能扫；终端深色主题用「前景 █ + 终端默认背景」画 ASCII 是反色（浅块深底），
/// 手机扫码兼容性差扫不到。故本屏用 <b>ANSI 背景色填充</b> 模拟真实「白底黑块」：
///   白模块区（含 quiet zone 4 模块白边）= 空格 + 背景白（TrueColor #FFFFFF）；
///   黑模块区 = 空格 + 背景黑（TrueColor #000000）铺满整个模块矩形；
///   半块兜底模式下▀▄█ 字形的前景色为黑、背景仍为白（透明半格透出白底）。
/// 渲染结果与 PNG 同对比度（白底矩形内黑块），与终端主题无关（不依赖默认背景色）。
/// QR 矩形之外保持终端原背景（不清屏为白色），只让「白色纸面 + 黑块」出现在 QR 区域。
/// </summary>
public class QrScanScreen : TuiScreen
{
    /// <summary>标准 quiet zone 宽度（模块数），与 <see cref="QrEncoder.AddQuietZone"/> 一致。</summary>
    public const int Quiet = 4;

    /// <summary>渲染模式：全块方形（主）或半块兜底（屏幕放不下全块时）。</summary>
    public enum QrRenderMode
    {
        /// <summary>每模块 = 2×Scale 列 × Scale 行终端格（方形，纯背景填充，白底黑块最可靠）。</summary>
        FullBlock,

        /// <summary>每模块 = 1 列宽 × 半行高，用 ▀▄█ 把 2 个模块行压进 1 个终端行（屏幕太矮/太窄时兜底）。</summary>
        HalfBlock,
    }

    /// <summary>全屏渲染布局（纯函数 <see cref="ComputeLayout"/> 计算，可自测）。</summary>
    public sealed class QrLayout
    {
        public QrRenderMode Mode { get; }
        public int Scale { get; }
        public int Grid { get; }
        public int BoxCols { get; }
        public int BoxRows { get; }

        public QrLayout(QrRenderMode mode, int scale, int grid, int boxCols, int boxRows)
        {
            Mode = mode;
            Scale = scale;
            Grid = grid;
            BoxCols = boxCols;
            BoxRows = boxRows;
        }
    }

    // ── 纯色（TrueColor，保证纯白/纯黑不随终端配色跑偏，与 PNG 白底黑块同对比）──
    private static readonly int WhiteBg = AnsiTty.RgbCode(255, 255, 255); // 白模块/quiet zone 白边
    private static readonly int BlackBg = AnsiTty.RgbCode(0, 0, 0);       // 黑模块（全块背景填充）
    private static readonly int BlackFg = AnsiTty.RgbCode(0, 0, 0);       // 黑模块（半块字形前景）

    private readonly bool[,] _matrix; // 原始矩阵 [y,x]，true=黑，不含 quiet zone
    private readonly int _size;       // 原始矩阵边长（模块）
    private readonly int _grid;       // 含 quiet zone 的网格边长（模块）= size + Quiet*2

    /// <summary>是否终端放得下（供 /sync-qr 预判：放不下则不全屏、只提示打开 PNG）。</summary>
    public static bool CanFit(bool[,] matrix, int cols, int rows)
    {
        if (matrix == null) return false;
        return ComputeLayout(matrix.GetLength(0) + Quiet * 2, cols, rows) != null;
    }

    /// <summary>
    /// 计算在 (cols, rows) 终端格下二维码能放下的最大方形布局。
    /// 网格边长 grid 已含 quiet zone（= 模块数 + 8）。
    /// 返回 null = 半块也放不下（QR 版本过高，屏显示残缺无意义）。
    ///
    /// 优先「全块方形」：每模块 2s 列 × s 行（等比放大，s≥1），约束 grid*2s ≤ cols 且 grid*s ≤ rows，
    /// s 取满足约束的最大值（尽量占满屏幕）。
    /// 全块放不下则退化「半块」：每模块 1 列宽 × 半行高（▀▄█ 压 2 个模块行到 1 个终端行），
    /// 约束 grid ≤ cols 且 ceil(grid/2) ≤ rows。
    /// </summary>
    internal static QrLayout? ComputeLayout(int grid, int cols, int rows)
    {
        if (grid <= 0 || cols <= 0 || rows <= 0) return null;

        int s = Math.Min(cols / (grid * 2), rows / grid);
        if (s >= 1)
            return new QrLayout(QrRenderMode.FullBlock, s, grid, grid * 2 * s, grid * s);

        int halfRows = (grid + 1) / 2; // ceil(grid/2)
        if (grid <= cols && halfRows <= rows)
            return new QrLayout(QrRenderMode.HalfBlock, 1, grid, grid, halfRows);

        return null;
    }

    /// <summary>半块模式单格字符（纯函数，可自测）：两相邻模块行 → █/▀/▄/空格。</summary>
    internal static char HalfBlockChar(bool topDark, bool botDark)
        => topDark ? (botDark ? '█' : '▀') : (botDark ? '▄' : ' ');

    public QrScanScreen(bool[,] matrix)
    {
        Name = "qr-scan";
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
        _size = matrix.GetLength(0);
        _grid = _size + Quiet * 2;
    }

    // ════════════════════════════════════════════════════════════════
    // 键盘
    // ════════════════════════════════════════════════════════════════

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // Esc / q → 返回上一层（ChatScreen）。Ctrl+Q 是主循环全局「紧急退出」，本屏不拦截。
        if (key.Key == ConsoleKey.Escape)
            return ExitToPrevious();
        if (!key.Modifiers.HasFlag(ConsoleModifiers.Control) &&
            key.Key == ConsoleKey.Q)
            return ExitToPrevious();

        return base.OnKey(key);
    }

    /// <summary>退出本屏返回上一层。返回 true 表示已处理。</summary>
    private bool ExitToPrevious()
    {
        if (Manager != null)
        {
            Manager.PopScreen();
            return true;
        }
        return false;
    }

    // ════════════════════════════════════════════════════════════════
    // 渲染
    // ════════════════════════════════════════════════════════════════

    public override void Render(StringBuilder sb)
    {
        base.Render(sb); // 空根视图：清脏标记；随后本屏直接绘制全屏 QR

        var layout = ComputeLayout(_grid, TW, TH);
        if (layout == null)
        {
            RenderTooBig(sb);
            return;
        }

        // 垂直居中白底 QR；上下余量 ≥1 行时画提示条
        int vFree = Math.Max(0, TH - layout.BoxRows);
        int topFree = vFree / 2;
        int boxTop = topFree;
        int boxLeft = Math.Max(0, (TW - layout.BoxCols) / 2);

        if (layout.Mode == QrRenderMode.FullBlock)
            RenderFullBlock(sb, layout, boxTop, boxLeft);
        else
            RenderHalfBlock(sb, layout, boxTop, boxLeft);

        int bottomFree = TH - (boxTop + layout.BoxRows);
        if (topFree >= 1)
            RenderBar(sb, 0, "  📱 手机扫屏同步 · 手机摄像头对准屏幕扫码  ");
        if (bottomFree >= 1)
            RenderBar(sb, TH - 1, "  Esc / q 返回聊天 · 已保存 sync-qr.png  ");
    }

    /// <summary>全块方形渲染：先铺白底矩形（quiet zone 白边含在内），再把黑模块用背景黑铺满。</summary>
    private void RenderFullBlock(StringBuilder sb, QrLayout layout, int top, int left)
    {
        int s = layout.Scale;
        int boxCols = layout.BoxCols;

        // 1. 白底（含 quiet zone 4 模块白边）
        var whiteRow = new string(' ', boxCols);
        for (int r = 0; r < layout.BoxRows; r++)
            WriteAt(sb, top + r, left, whiteRow, bg: WhiteBg);

        // 2. 黑模块：模块 = 2s 列 × s 行
        for (int gy = 0; gy < _grid; gy++)
        {
            for (int gx = 0; gx < _grid; gx++)
            {
                if (!IsDark(gy, gx)) continue;
                var black = new string(' ', 2 * s);
                for (int k = 0; k < s; k++)
                    WriteAt(sb, top + gy * s + k, left + gx * 2 * s, black, bg: BlackBg);
            }
        }
    }

    /// <summary>半块兜底渲染：1 列/模块，2 模块行压进 1 终端行（▀▄█）。背景白、字形前景黑。</summary>
    private void RenderHalfBlock(StringBuilder sb, QrLayout layout, int top, int left)
    {
        // 1. 白底（含 quiet zone 白边）
        var whiteRow = new string(' ', layout.BoxCols);
        for (int r = 0; r < layout.BoxRows; r++)
            WriteAt(sb, top + r, left, whiteRow, bg: WhiteBg);

        // 2. 黑模块（字形前景黑 + 背景白 → 透明半格透出白底）
        for (int line = 0; line < layout.BoxRows; line++)
        {
            int gy0 = line * 2;
            int gy1 = gy0 + 1;
            for (int gx = 0; gx < _grid; gx++)
            {
                bool topDark = IsDark(gy0, gx);
                bool botDark = gy1 < _grid && IsDark(gy1, gx);
                char ch = HalfBlockChar(topDark, botDark);
                if (ch == ' ') continue; // 全白模块：白底已铺，无需覆盖
                WriteAt(sb, top + line, left + gx, ch.ToString(), fg: BlackFg, bg: WhiteBg);
            }
        }
    }

    /// <summary>网格坐标 (gy, gx) 是否黑。quiet zone（外圈 4 模块）一律视为白。</summary>
    private bool IsDark(int gy, int gx)
    {
        int my = gy - Quiet;
        int mx = gx - Quiet;
        return my >= 0 && my < _size && mx >= 0 && mx < _size && _matrix[my, mx];
    }

    /// <summary>版本过高放不下：屏内提示打开 sync-qr.png，不渲染残缺 QR。</summary>
    private void RenderTooBig(StringBuilder sb)
    {
        string[] lines =
        {
            "❌ 二维码版本过高，当前终端放不下完整图形",
            $"（含 quiet zone 共 {_grid}×{_grid} 模块 · 终端 {TW} 列 × {TH} 行）",
            "",
            "请打开 sync-qr.png 扫码，或放大终端窗口后重试 /sync-qr",
            "",
            "按 Esc / q 返回聊天",
        };
        int start = Math.Max(0, (TH - lines.Length) / 2);
        for (int i = 0; i < lines.Length; i++)
            CenterText(sb, start + i, lines[i]);
    }

    /// <summary>在 row 行画一条全宽提示条（深灰底白字，与终端主题无关，QR 之外不干扰扫码）。</summary>
    private void RenderBar(StringBuilder sb, int row, string text)
    {
        WriteAt(sb, row, 0, new string(' ', TW), bg: AnsiColors.BgBrightBlack);
        WriteAt(sb, row, 0, PadCenter(text, TW), fg: AnsiColors.White, bg: AnsiColors.BgBrightBlack);
    }

    private void CenterText(StringBuilder sb, int row, string text)
    {
        WriteAt(sb, row, 0, PadCenter(text, TW));
    }

    /// <summary>把文本居中并补齐到 width 列（按显示宽度）。</summary>
    private static string PadCenter(string text, int width)
    {
        int vw = AnsiString.DisplayWidth(text);
        if (vw >= width) return AnsiString.TruncateByWidth(text, width);
        int left = (width - vw) / 2;
        int right = width - vw - left;
        return new string(' ', left) + text + new string(' ', right);
    }
}
