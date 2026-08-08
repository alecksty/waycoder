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

    /// <summary>
    /// 在指定位置写纯文本+颜色。
    /// text 必须是纯文本（不含 ANSI 码）；颜色为 0 时不输出。
    /// 自动处理：定位 → 颜色 → 文本 → 重置。
    /// </summary>
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

    /// <summary>追加原始字符串（仅在底层使用）</summary>
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
