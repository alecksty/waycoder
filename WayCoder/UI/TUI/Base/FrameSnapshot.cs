using System.Text;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 屏幕区域快照 —— 模态对话框显示前截取背景，关闭后整块贴回还原。
///
/// 现有关闭路径是「默认背景清屏 + 裁剪重绘根视图」，当被遮挡区域的底色不是默认色、
/// 或重绘因增量渲染跳过干净控件时，会残留颜色不一致的条带。
/// 快照把区域帧用颜色感知解析器解释成 (字符, 前景色, 背景色) 格子，关闭时逐格重放，
/// 底色与内容完全一致，从根上消除残留。
/// </summary>
public sealed class FrameSnapshot
{
    /// <summary>快照区域（屏幕绝对坐标）</summary>
    public int X { get; }
    public int Y { get; }
    public int W { get; }
    public int H { get; }

    private readonly string[] _ch; // W×H 个格子（每格一个字形）
    private readonly int[] _fg;    // 前景色（0=无）
    private readonly int[] _bg;    // 背景色（0=无/默认）
    private readonly byte[] _style; // 文字特征位（粗体/淡色/斜体/下划线）

    // 文字特征位掩码（与 SGR 1/2/3/4 对应；供外部渲染器如 WPF 预览应用）
    public const int StBold = 1;
    public const int StDim = 2;
    public const int StItalic = 4;
    public const int StUnderline = 8;

    private FrameSnapshot(int x, int y, int w, int h)
    {
        X = x; Y = y; W = w; H = h;
        int n = w * h;
        _ch = new string[n];
        _fg = new int[n];
        _bg = new int[n];
        _style = new byte[n];
        for (int i = 0; i < n; i++) _ch[i] = " ";
    }

    /// <summary>从 ANSI 帧字符串截取矩形区域为快照。区域非法返回 null。</summary>
    public static FrameSnapshot? Capture(string ansi, int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return null;
        var snap = new FrameSnapshot(x, y, w, h);
        snap.Parse(ansi);
        return snap;
    }

    /// <summary>把快照贴回 StringBuilder（逐行同色合并，逐格带色重放）。</summary>
    public void Blit(StringBuilder sb)
    {
        var rb = new RenderBuffer();
        for (int r = 0; r < H; r++)
        {
            int c = 0;
            while (c < W)
            {
                int i = r * W + c;
                int fg = _fg[i], bg = _bg[i];
                int j = c;
                while (j + 1 < W && _fg[r * W + j + 1] == fg && _bg[r * W + j + 1] == bg)
                    j++;

                // 合并同色连续格为一段文本
                var text = new StringBuilder(j - c + 1);
                for (int k = c; k <= j; k++) text.Append(_ch[r * W + k]);

                rb.Write(Y + r, X + c, text.ToString(), fg, bg);
                c = j + 1;
            }
        }
        sb.Append(rb.ToString());
    }

    /// <summary>读取区域相对坐标的字符（测试/审计/外部渲染用；越界返回空串）</summary>
    public string CharAt(int relRow, int relCol)
    {
        if (relRow < 0 || relRow >= H || relCol < 0 || relCol >= W) return "";
        return _ch[relRow * W + relCol];
    }

    /// <summary>读取区域相对坐标的颜色 (前景, 背景)（测试/审计/外部渲染用；0=无/默认）</summary>
    public (int fg, int bg) ColorAt(int relRow, int relCol)
    {
        if (relRow < 0 || relRow >= H || relCol < 0 || relCol >= W) return (0, 0);
        int i = relRow * W + relCol;
        return (_fg[i], _bg[i]);
    }

    /// <summary>读取区域相对坐标的文字特征位（StBold/StDim/StItalic/StUnderline 组合；越界返回 0）</summary>
    public int StyleAt(int relRow, int relCol)
    {
        if (relRow < 0 || relRow >= H || relCol < 0 || relCol >= W) return 0;
        return _style[relRow * W + relCol];
    }

    // ── 颜色感知的 ANSI 解析 ──
    // 跟踪 CUP/HVP 光标与 SGR 前景/背景色（16/256/TrueColor），只记录矩形区域内的字符。

    private void Parse(string ansi)
    {
        int curR = 0, curC = 0, fg = 0, bg = 0, style = 0;
        int i = 0, len = ansi.Length;
        while (i < len)
        {
            char ch = ansi[i];
            if (ch == AnsiTty.AnsiCharPrefix && i + 1 < len && ansi[i + 1] == '[')
            {
                int j = i + 2;
                while (j < len && !(ansi[j] >= '@' && ansi[j] <= '~')) j++;
                if (j >= len) break;
                char final = ansi[j];
                string param = ansi.Substring(i + 2, j - (i + 2));
                i = j + 1;

                if (final == 'H' || final == 'f')
                {
                    int row = 1, col = 1;
                    var p = param.Split(';');
                    if (p.Length >= 1 && int.TryParse(p[0], out var rr)) row = rr;
                    if (p.Length >= 2 && int.TryParse(p[1], out var cc)) col = cc;
                    curR = row - 1;
                    curC = col - 1;
                }
                else if (final == 'm')
                {
                    ApplySgr(param, ref fg, ref bg, ref style);
                }
                // 其余 CSI（光标隐藏、清屏等）跳过
                continue;
            }
            if (ch == '\r') { curC = 0; i++; continue; }
            if (ch == '\n') { curR++; curC = 0; i++; continue; }
            if (ch == '\t') { curC += 4 - (curC % 4); i++; continue; }

            var rune = Rune.GetRuneAt(ansi, i);
            int vw = AnsiString.CharWidth(rune);
            string s = rune.ToString();
            i += rune.Utf16SequenceLength;

            int relR = curR - Y, relC = curC - X;
            if (relR >= 0 && relR < H && relC >= 0 && relC < W)
            {
                int idx = relR * W + relC;
                _ch[idx] = s;
                _fg[idx] = fg;
                _bg[idx] = bg;
                _style[idx] = (byte)style;
                if (vw == 2 && relC + 1 < W)
                    _ch[idx + 1] = " "; // 宽字符延续格留空（不记色）
            }
            curC += vw;
        }
    }

    private static void ApplySgr(string param, ref int fg, ref int bg, ref int style)
    {
        if (string.IsNullOrEmpty(param)) { fg = 0; bg = 0; style = 0; return; }
        var parts = param.Split(';');
        int k = 0;
        while (k < parts.Length)
        {
            int code = int.TryParse(parts[k], out var v) ? v : -1;
            switch (code)
            {
                case 0: fg = 0; bg = 0; style = 0; k++; break;
                case 1: style |= StBold; k++; break;
                case 2: style |= StDim; k++; break;
                case 3: style |= StItalic; k++; break;
                case 4: style |= StUnderline; k++; break;
                case 22: style &= ~(StBold | StDim); k++; break;
                case 23: style &= ~StItalic; k++; break;
                case 24: style &= ~StUnderline; k++; break;
                case 39: fg = 0; k++; break;
                case 49: bg = 0; k++; break;
                case 38: // 前景扩展色
                    if (k + 2 < parts.Length && int.TryParse(parts[k + 1], out var m5) && m5 == 5
                        && int.TryParse(parts[k + 2], out var c5)) { fg = c5; k += 3; }
                    else if (k + 4 < parts.Length && int.TryParse(parts[k + 1], out var m2) && m2 == 2
                        && int.TryParse(parts[k + 2], out var r) && int.TryParse(parts[k + 3], out var g)
                        && int.TryParse(parts[k + 4], out var b)) { fg = 0x1000000 + ((r & 0xFF) << 16 | (g & 0xFF) << 8 | (b & 0xFF)); k += 5; }
                    else k++;
                    break;
                case 48: // 背景扩展色
                    if (k + 2 < parts.Length && int.TryParse(parts[k + 1], out var b5) && b5 == 5
                        && int.TryParse(parts[k + 2], out var c55)) { bg = c55; k += 3; }
                    else if (k + 4 < parts.Length && int.TryParse(parts[k + 1], out var b2) && b2 == 2
                        && int.TryParse(parts[k + 2], out var r2) && int.TryParse(parts[k + 3], out var g2)
                        && int.TryParse(parts[k + 4], out var b22)) { bg = 0x1000000 + ((r2 & 0xFF) << 16 | (g2 & 0xFF) << 8 | (b22 & 0xFF)); k += 5; }
                    else k++;
                    break;
                case >= 30 and <= 37: fg = code; k++; break;
                case >= 90 and <= 97: fg = code; k++; break;
                case >= 40 and <= 47: bg = code; k++; break;
                case >= 100 and <= 107: bg = code; k++; break;
                default: k++; break; // 其余样式码（闪烁/反白等）忽略
            }
        }
    }
}
