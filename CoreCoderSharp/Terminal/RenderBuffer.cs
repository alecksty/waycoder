namespace CoreCoderSharp.Terminal;

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
        _sb.Append($"\x1b[{row + 1};{col + 1}H");
        return this;
    }

    // ================================================================
    // 颜色/样式
    // ================================================================

    /// <summary>设置前景色</summary>
    public RenderBuffer Fg(int ansiCode) { _sb.Append($"\x1b[{ansiCode}m"); return this; }
    /// <summary>设置前景+背景色</summary>
    public RenderBuffer FgBg(int fgCode, int bgCode) { _sb.Append($"\x1b[{fgCode};{bgCode}m"); return this; }
    /// <summary>仅背景色</summary>
    public RenderBuffer Bg(int ansiCode) { _sb.Append($"\x1b[{ansiCode}m"); return this; }
    /// <summary>粗体</summary>
    public RenderBuffer Bold() { _sb.Append("\x1b[1m"); return this; }
    /// <summary>灰色/淡化</summary>
    public RenderBuffer Dim() { _sb.Append("\x1b[2m"); return this; }
    /// <summary>重置所有颜色/样式</summary>
    public RenderBuffer Reset() { _sb.Append("\x1b[0m"); return this; }

    // ================================================================
    // 高级写入（自动处理定位+颜色+转义）
    // ================================================================

    /// <summary>在指定位置写纯文本+颜色。0=无色。</summary>
    public RenderBuffer Write(int row, int col, string text, int fg = 0, int bg = 0)
    {
        MoveTo(row, col);
        bool hasColor = fg > 0 || bg > 0;
        if (fg > 0 && bg > 0) _sb.Append($"\x1b[{fg};{bg}m");
        else if (fg > 0) _sb.Append($"\x1b[{fg}m");
        else if (bg > 0) _sb.Append($"\x1b[{bg}m");
        _sb.Append(text);
        if (hasColor) _sb.Append("\x1b[0m");
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
            if (avail <= 0) { row++; col = indentCol; continue; }

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
            if (i < text.Length) { row++; col = indentCol; }
        }
        return row;
    }

    // ================================================================
    // Color 流畅 API
    // ================================================================

    /// <summary>用命名颜色写入（流畅写法）</summary>
    public RenderBuffer C(Color fg) { _sb.Append($"\x1b[{fg.AnsiCode}m"); return this; }
    public RenderBuffer C(Color fg, Color bg) { _sb.Append($"\x1b[{fg.AnsiCode};{bg.AnsiCode}m"); return this; }
    public RenderBuffer BgC(Color bg) { _sb.Append($"\x1b[{bg.AnsiCode}m"); return this; }

    /// <summary>在指定行填充 count 列空白（覆盖背景色）</summary>
    public RenderBuffer Fill(int row, int col, int count, int bg = 0)
    {
        if (count <= 0) return this;
        MoveTo(row, col);
        if (bg > 0) _sb.Append($"\x1b[{bg}m");
        _sb.Append(new string(' ', count));
        if (bg > 0) _sb.Append("\x1b[0m");
        return this;
    }

    // ================================================================
    // 片段写入（不移动光标，用于同一行多彩色文本）
    // ================================================================

    /// <summary>写入一个文字片段（不移动光标，仅设置颜色+文本）</summary>
    public RenderBuffer Segment(string text, int fg = 0, int bg = 0)
    {
        if (fg > 0 && bg > 0) _sb.Append($"\x1b[{fg};{bg}m");
        else if (fg > 0) _sb.Append($"\x1b[{fg}m");
        else if (bg > 0) _sb.Append($"\x1b[{bg}m");
        _sb.Append(text);
        if (fg > 0 || bg > 0) _sb.Append("\x1b[0m");
        return this;
    }

    /// <summary>粗体片段（可指定前景色）</summary>
    public RenderBuffer SegmentBold(string text, int fg = 33) {
        _sb.Append(fg > 0 ? $"\x1b[1;{fg}m{text}\x1b[0m" : $"\x1b[1m{text}\x1b[0m"); return this; }
    /// <summary>灰色片段</summary>
    public RenderBuffer SegmentDim(string text) { _sb.Append($"\x1b[2m{text}\x1b[0m"); return this; }
    /// <summary>闪烁光标</summary>
    public RenderBuffer Blink() { _sb.Append("\x1b[5m ▏\x1b[0m"); return this; }
    /// <summary>清除当前行从光标到行尾</summary>
    public RenderBuffer ClearToEndOfLine() { _sb.Append("\x1b[K"); return this; }
    /// <summary>在指定位置显示光标</summary>
    public RenderBuffer CursorAt(int row, int col) { _sb.Append($"\x1b[{row + 1};{col + 1}H\x1b[?25h"); return this; }
    /// <summary>隐藏光标</summary>
    public RenderBuffer HideCursor() { _sb.Append("\x1b[?25l"); return this; }

    /// <summary>追加原始字符串（仅在 Terminal 层内部使用）</summary>
    internal RenderBuffer Raw(string s) { _sb.Append(s); return this; }

    /// <summary>获取内部 StringBuilder（用于兼容旧代码）</summary>
    public System.Text.StringBuilder SB => _sb;

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
