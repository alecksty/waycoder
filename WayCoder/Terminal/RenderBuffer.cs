namespace WayCoder.Terminal;

/// <summary>
/// 终端渲染缓冲区 —— 自动处理所有 ANSI 转义序列。
/// 外部代码只传纯文本+颜色，不碰 \x1b 字符串。
/// </summary>
public class RenderBuffer
{
    private readonly System.Text.StringBuilder _sb = new();

    // ================================================================
    // 光标定位
    // ================================================================

    /// <summary>光标移到 (row, col) — 0-based，内部转 1-based</summary>
    public RenderBuffer MoveTo(int row, int col)
    {
        _sb.Append(AnsiTty.CursorPos0(row, col));
        return this;
    }

    // ================================================================
    // 颜色/样式
    // ================================================================

    /// <summary>设置前景色（支持 16/256/True Color）</summary>
    public RenderBuffer Fg(int ansiCode)
    {
        _sb.Append(AnsiColorSeq(ansiCode, isBg: false));
        return this;
    }

    /// <summary>设置前景+背景色</summary>
    public RenderBuffer FgBg(int fgCode, int bgCode)
    {
        _sb.Append(AnsiColorPairSeq(fgCode, bgCode));
        return this;
    }

    /// <summary>仅背景色（支持 16/256/True Color）</summary>
    public RenderBuffer Bg(int ansiCode)
    {
        _sb.Append(AnsiColorSeq(ansiCode, isBg: true));
        return this;
    }

    /// <summary>粗体</summary>
    public RenderBuffer Bold()
    {
        _sb.Append(AnsiTty.SgrBold);
        return this;
    }

    /// <summary>灰色/淡化</summary>
    public RenderBuffer Dim()
    {
        _sb.Append(AnsiTty.SgrDim);
        return this;
    }

    /// <summary>重置所有颜色/样式</summary>
    public RenderBuffer Reset()
    {
        _sb.Append(AnsiTty.SgrReset);
        return this;
    }

    // ================================================================
    // 高级写入（自动处理定位+颜色+转义）
    // ================================================================

    // ── 颜色序列构建 ──

    /// <summary>为单个颜色码生成 ANSI 序列片段（委托给 AnsiTty）。</summary>
    private static string AnsiColorSeq(int code, bool isBg)
        => isBg ? AnsiTty.BgCode(code) : AnsiTty.FgCode(code);

    /// <summary>为前景+背景颜色码生成组合 ANSI 序列（委托给 AnsiTty）。</summary>
    private static string AnsiColorPairSeq(int fg, int bg)
        => AnsiTty.FgBgCode(fg, bg);

    /// <summary>在指定位置写纯文本+颜色。0=无色。</summary>
    public RenderBuffer Write(int row, int col, string text, int fg = 0, int bg = 0)
    {
        MoveTo(row, col);
        bool hasFg = fg > 0, hasBg = bg > 0;
        if (hasFg || hasBg)
            _sb.Append(AnsiColorPairSeq(fg, bg));
        _sb.Append(text);
        // 精确重置：分别重置 fg/bg，避免 SgrReset 全复位冲掉窗口底色
        if (hasFg) _sb.Append(AnsiTty.SgrResetFg);
        if (hasBg) _sb.Append(AnsiTty.SgrResetBg);
        return this;
    }

    // ================================================================
    // 超屏处理：截断 / 换行
    // ================================================================

    /// <summary>
    /// 写文本，超出屏幕右边界自动截断。
    /// maxCol = 屏幕最右可用列（0-based），超出部分丢弃。
    /// </summary>
    public RenderBuffer WriteTruncate(int row, int col, string text, int maxCol, int fg = 0, int bg = 0)
    {
        var avail = maxCol - col + 1; // 可用列数
        if (avail <= 0) return this;
        var vw = AnsiString.DisplayWidth(text);
        if (vw > avail)
            text = AnsiString.TruncateByWidth(text, avail);
        return Write(row, col, text, fg, bg);
    }

    /// <summary>
    /// 写文本，超出右边界自动换行。
    /// 换行后从 indentCol 列开始继续写。
    /// 返回最后写入的行号。
    /// </summary>
    public int WriteWrap(int startRow, int startCol, string text, int maxCol, int indentCol, int fg = 0, int bg = 0)
    {
        int row = startRow, col = startCol;
        int i = 0;
        while (i < text.Length)
        {
            var avail = maxCol - col + 1;
            if (avail <= 0)
            {
                row++;
                col = indentCol;
                continue;
            }

            // 取当前行能放下的子串
            int chars = 0, vw = 0;
            while (i + chars < text.Length)
            {
                var rune = System.Text.Rune.GetRuneAt(text, i + chars);
                var w = AnsiString.CharWidth(rune);
                if (vw + w > avail) break;
                vw += w;
                chars += rune.Utf16SequenceLength;
            }

            if (chars == 0) chars = 1; // 至少放一个字

            var line = text.Substring(i, chars);
            Write(row, col, line, fg, bg);
            i += chars;
            if (i < text.Length)
            {
                row++;
                col = indentCol;
            }
        }

        return row;
    }

    // ================================================================
    // 区域文本：多行 + 水平对齐 + 垂直对齐
    // ================================================================

    public enum HAlign
    {
        Left,
        Center,
        Right
    }

    public enum VAlign
    {
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// 在矩形区域内渲染多行文本。
    /// 超出宽度自动折行，超出高度自动截断。
    /// 支持水平/垂直对齐。
    /// </summary>
    /// <param name="row">区域起始行（0-based）</param>
    /// <param name="col">区域起始列（0-based）</param>
    /// <param name="width">区域宽度（列数）</param>
    /// <param name="height">区域高度（行数）</param>
    /// <param name="text">要渲染的文本（可含 \n）</param>
    /// <param name="hAlign">水平对齐</param>
    /// <param name="vAlign">垂直对齐</param>
    /// <param name="fg">前景色</param>
    /// <param name="bg">背景色</param>
    public void WriteRegion(int row, int col, int width, int height, string text,
        HAlign hAlign = HAlign.Left, VAlign vAlign = VAlign.Top,
        int fg = 0, int bg = 0)
    {
        if (width <= 0 || height <= 0) return;

        // 1. 将文本按区域宽度折行
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add("");
                continue;
            }

            int i = 0;
            while (i < paragraph.Length)
            {
                int chars = 0, vw = 0;
                while (i + chars < paragraph.Length)
                {
                    var rune = System.Text.Rune.GetRuneAt(paragraph, i + chars);
                    var w = AnsiString.CharWidth(rune);
                    if (vw + w > width) break;
                    vw += w;
                    chars += rune.Utf16SequenceLength;
                }

                if (chars == 0) chars = 1;
                lines.Add(paragraph.Substring(i, chars));
                i += chars;
            }
        }

        // 2. 裁剪到区域高度
        while (lines.Count > height) lines.RemoveAt(lines.Count - 1);

        // 3. 垂直对齐
        int topPad = 0;
        if (vAlign == VAlign.Middle)
            topPad = (height - lines.Count) / 2;
        else if (vAlign == VAlign.Bottom)
            topPad = height - lines.Count;

        // 4. 逐行渲染（水平对齐）
        for (int li = 0; li < lines.Count; li++)
        {
            int r = row + topPad + li;
            if (r >= row + height) break;

            var line = lines[li];
            var lineVw = AnsiString.DisplayWidth(line);
            int c = col;
            if (hAlign == HAlign.Center) c += (width - lineVw) / 2;
            else if (hAlign == HAlign.Right) c += width - lineVw;

            Write(r, c, line, fg, bg);
        }
    }

    // ================================================================
    // 区域填充 / 片段写入
    // ================================================================

    /// <summary>在指定行填充 count 列空白（覆盖背景色）</summary>
    public RenderBuffer Fill(int row, int col, int count, int bg = 0)
    {
        if (count <= 0) return this;
        MoveTo(row, col);
        if (bg > 0) _sb.Append(AnsiTty.BgCode(bg));
        _sb.Append(new string(' ', count));
        if (bg > 0) _sb.Append(AnsiTty.SgrResetBg);
        return this;
    }

    // ── Color 流畅 API ──

    /// <summary>写入一个文字片段（不移动光标，仅设置颜色+文本）</summary>
    public RenderBuffer Segment(string text, int fg = 0, int bg = 0)
    {
        if (fg > 0 || bg > 0)
            _sb.Append(AnsiTty.FgBgCode(fg, bg));
        _sb.Append(text);
        if (fg > 0) _sb.Append(AnsiTty.SgrResetFg);
        if (bg > 0) _sb.Append(AnsiTty.SgrResetBg);
        return this;
    }

    /// <summary>粗体片段（可指定前景色）</summary>
    public RenderBuffer SegmentBold(string text, int fg = 33)
    {
        _sb.Append(fg > 0
            ? $"{AnsiTty.SgrBold}{AnsiTty.FgCode(fg)}{text}{AnsiTty.SgrReset}"
            : $"{AnsiTty.SgrBold}{text}{AnsiTty.SgrReset}");
        return this;
    }

    /// <summary>灰色片段</summary>
    public RenderBuffer SegmentDim(string text)
    {
        _sb.Append($"{AnsiTty.SgrDim}{text}{AnsiTty.SgrReset}");
        return this;
    }

    /// <summary>用命名颜色写入（流畅写法）</summary>
    public RenderBuffer C(Color fg)
    {
        _sb.Append(AnsiTty.FgCode(fg.AnsiCode));
        return this;
    }

    public RenderBuffer C(Color fg, Color bg)
    {
        _sb.Append(AnsiTty.FgBgCode(fg.AnsiCode, bg.AnsiCode));
        return this;
    }

    public RenderBuffer BgC(Color bg)
    {
        _sb.Append(AnsiTty.BgCode(bg.AnsiCode));
        return this;
    }

    /// <summary>闪烁光标</summary>
    public RenderBuffer Blink()
    {
        _sb.Append($"{AnsiTty.SgrBlink} ▏{AnsiTty.SgrReset}");
        return this;
    }

    /// <summary>清除当前行从光标到行尾</summary>
    public RenderBuffer ClearToEndOfLine()
    {
        _sb.Append(AnsiTty.ClearToEnd);
        return this;
    }

    /// <summary>在指定位置显示光标</summary>
    public RenderBuffer CursorAt(int row, int col)
    {
        _sb.Append($"{AnsiTty.CursorPos0(row, col)}{AnsiTty.CursorShow}");
        return this;
    }

    /// <summary>隐藏光标</summary>
    public RenderBuffer HideCursor()
    {
        _sb.Append(AnsiTty.CursorHide);
        return this;
    }

    /// <summary>追加原始字符串（仅在 Terminal 层内部使用）</summary>
    internal RenderBuffer Raw(string s)
    {
        _sb.Append(s);
        return this;
    }

    /// <summary>获取内部 StringBuilder（用于兼容旧代码）</summary>
    public System.Text.StringBuilder Sb => _sb;

    // ================================================================
    // 输出
    // ================================================================

    /// <summary>写入终端</summary>
    public void Flush() => Console.Write(_sb.ToString());

    public override string ToString() => _sb.ToString();
    public void Clear() => _sb.Clear();
    public int Length => _sb.Length;

    /// <summary>隐式转换：RenderBuffer → StringBuilder（兼容旧代码）</summary>
    public static implicit operator System.Text.StringBuilder(RenderBuffer rb) => rb._sb;
}