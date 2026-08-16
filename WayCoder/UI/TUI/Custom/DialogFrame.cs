using System.Text;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui;

/// <summary>
/// 居中带边框对话框的外框绘制 —— 橙→黄渐变实心边框 + 暗化背景。
/// 统一 ModelPicker / FilePicker / SessionPicker / CommandPalette 的「非全屏 + 有边框」外观，
/// 渐变配色与 ModelPicker 一致（对标权限确认对话框 GradOrangeYellow）。
/// </summary>
public static class DialogFrame
{
    // 渐变外框色 —— 橙→黄
    public static readonly int GradStart = AnsiTty.RgbCode(255, 180, 0);   // 橙色
    public static readonly int GradEnd   = AnsiTty.RgbCode(255, 255, 80);  // 黄色
    public static readonly int DimBg     = AnsiTty.RgbCode(8, 8, 12);      // 暗蓝黑底

    // 分隔线色 —— 取渐变色 30% 位置
    public static readonly int Sep = AnsiTty.LerpRgb(GradStart, GradEnd, 0.3f);

    /// <summary>计算居中盒子：返回 (bx, by, dw, dh, innerW)。</summary>
    public static (int bx, int by, int dw, int dh, int innerW) Layout(int minW, int minH)
    {
        int tw = Tty.Cols, th = Tty.Rows;
        int dw = Math.Max(minW, tw * 2 / 3);
        int dh = Math.Max(minH, th * 2 / 3);
        // 防止窄终端溢出：不超过终端宽高（各留 2 格边距）
        dw = Math.Min(dw, Math.Max(10, tw - 2));
        dh = Math.Min(dh, Math.Max(8, th - 2));
        int bx = Math.Max(1, (tw - dw) / 2);
        int by = Math.Max(1, (th - dh) / 2);
        return (bx, by, dw, dh, dw - 2);
    }

    /// <summary>暗化对话框占据的矩形背景。</summary>
    public static void DimArea(StringBuilder sb, int bx, int by, int dw, int dh)
    {
        for (int y = 0; y < dh; y++)
            FillRow(sb, by + y, bx, dw, 0, DimBg, ' ');
    }

    /// <summary>顶部边框 ┌─…─┐。</summary>
    public static void TopBorder(StringBuilder sb, int y, int bx, int dw)
    {
        WriteGradChar(sb, y, bx, '┌', dw, 0);
        for (int i = 1; i < dw - 1; i++) WriteGradChar(sb, y, bx + i, '─', dw, i);
        WriteGradChar(sb, y, bx + dw - 1, '┐', dw, dw - 1);
    }

    /// <summary>底部边框 └─…─┘。</summary>
    public static void BottomBorder(StringBuilder sb, int y, int bx, int dw)
    {
        WriteGradChar(sb, y, bx, '└', dw, 0);
        for (int i = 1; i < dw - 1; i++) WriteGradChar(sb, y, bx + i, '─', dw, i);
        WriteGradChar(sb, y, bx + dw - 1, '┘', dw, dw - 1);
    }

    /// <summary>横向分隔线 ├─…─┤。</summary>
    public static void SepLine(StringBuilder sb, int y, int bx, int dw)
    {
        WriteGradChar(sb, y, bx, '├', dw, 0);
        for (int i = 1; i < dw - 1; i++) WriteGradChar(sb, y, bx + i, '─', dw, i);
        WriteGradChar(sb, y, bx + dw - 1, '┤', dw, dw - 1);
    }

    /// <summary>左侧竖线（渐变起始色·橙）。</summary>
    public static void SideL(StringBuilder sb, int row, int bx)
    {
        sb.Append(AnsiTty.CursorPos(row, bx))
          .Append(AnsiTty.FgBgCode(GradStart, DimBg))
          .Append('│');
    }

    /// <summary>右侧竖线（渐变终止色·黄）。</summary>
    public static void SideR(StringBuilder sb, int row, int bx, int dw)
    {
        sb.Append(AnsiTty.CursorPos(row, bx + dw - 1))
          .Append(AnsiTty.FgBgCode(GradEnd, DimBg))
          .Append('│');
    }

    /// <summary>清除盒子内部一行（单色背景，绘制内容前打底）。</summary>
    public static void FillInner(StringBuilder sb, int row, int bx, int innerW, int fg, int bg)
    {
        FillRow(sb, row, bx + 1, innerW, fg, bg, ' ');
    }

    // ── 内部 ──

    private static void WriteGradChar(StringBuilder sb, int row, int col, char ch, int totalW, int pos)
    {
        float t = totalW > 1 ? (float)pos / (totalW - 1) : 0;
        int c = AnsiTty.LerpRgb(GradStart, GradEnd, t);
        sb.Append(AnsiTty.CursorPos(row, col))
          .Append(AnsiTty.FgBgCode(c, DimBg))
          .Append(ch);
    }

    private static void FillRow(StringBuilder sb, int row, int col, int w, int fg, int bg, char fill)
    {
        sb.Append(AnsiTty.CursorPos(row, col))
          .Append(AnsiTty.FgBgCode(fg, bg))
          .Append(new string(fill, w));
    }
}
