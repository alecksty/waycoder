using System.Text;

namespace CoreCoderSharp.UI;

/// <summary>
/// 多行输入区 —— Enter 发送，Ctrl+Enter 换行，文本软折行。
/// 最多 5 个屏幕行可见，超出可滚动。
/// </summary>
public static class TuiInput
{
    private const int MaxScreenLines = 5;

    /// <summary>读取输入，Enter 发送返回文本，Esc 取消返回 null</summary>
    public static string? ReadInput()
    {
        var lines = new List<StringBuilder> { new() }; // 硬行列表
        int cy = 0, cx = 0;        // 硬行内光标
        int screenScroll = 0;      // 屏幕滚动偏移（屏幕行）
        int tw = Console.WindowWidth;
        var contentWidth = tw - 4; // 可用内容宽度

        // 起始行
        Console.WriteLine();
        var startRow = Console.CursorTop;

        try
        {
            while (true)
            {
                tw = Console.WindowWidth;
                contentWidth = Math.Max(20, tw - 4);

                // 计算屏幕行
                var screenLines = BuildScreenLines(lines, contentWidth);
                var totalScreen = screenLines.Count;
                // 框高：1~5，内容超出5行时框固定5行
                var vh = totalScreen <= MaxScreenLines ? Math.Max(1, totalScreen) : MaxScreenLines;
                var (scrCy, scrCx) = HardToScreen(lines, cy, cx, contentWidth);

                // 光标超出可见区 → 滚动
                if (scrCy < screenScroll) screenScroll = scrCy;
                if (scrCy >= screenScroll + vh) screenScroll = scrCy - vh + 1;
                screenScroll = Math.Clamp(screenScroll, 0, Math.Max(0, totalScreen - vh));

                RenderArea(lines, cy, cx, screenScroll, tw, contentWidth, vh, screenLines, startRow);

                var key = Console.ReadKey(intercept: true);
                bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

                switch (key.Key)
                {
                    // ---- Enter = 发送 / Ctrl+Enter = 硬换行 ----
                    case ConsoleKey.Enter when ctrl || shift:
                        NewHardLine(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Enter:
                        ClearArea(startRow, tw);
                        return JoinLines(lines);

                    case ConsoleKey.Escape:
                        ClearArea(startRow, tw);
                        return null;

                    // ---- 光标 ----
                    case ConsoleKey.UpArrow:
                        MoveScreenUp(lines, ref cy, ref cx, contentWidth);
                        break;
                    case ConsoleKey.DownArrow:
                        MoveScreenDown(lines, ref cy, ref cx, contentWidth);
                        break;
                    case ConsoleKey.LeftArrow:
                        MoveLeft(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.RightArrow:
                        MoveRight(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Home: cx = 0; break;
                    case ConsoleKey.End: cx = lines[cy].Length; break;

                    // ---- 编辑 ----
                    case ConsoleKey.Backspace:
                        Backspace(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Delete:
                        DeleteFwd(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Tab:
                        for (int t = 0; t < 4; t++) InsertChar(lines, ref cy, ref cx, ' ');
                        break;
                    case ConsoleKey.K when ctrl:
                        if (cx < lines[cy].Length) lines[cy].Remove(cx, lines[cy].Length - cx);
                        break;
                    case ConsoleKey.V when ctrl:
                        Paste(lines, ref cy, ref cx);
                        break;

                    default:
                        if (key.KeyChar >= ' ')
                            InsertChar(lines, ref cy, ref cx, key.KeyChar);
                        break;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    // ================================================================
    // 屏幕行计算（软折行）
    // ================================================================

    /// <summary>将硬行列表折叠为屏幕行列表</summary>
    private static List<ScreenLine> BuildScreenLines(List<StringBuilder> hardLines, int contentWidth)
    {
        var result = new List<ScreenLine>();
        for (int hi = 0; hi < hardLines.Count; hi++)
        {
            var text = hardLines[hi].ToString();
            int offset = 0;
            bool first = true;
            while (offset < text.Length || first)
            {
                first = false;
                var (vch, vcw) = MeasureSlice(text, offset, contentWidth);
                result.Add(new ScreenLine(hi, offset, vch, vcw));
                offset += vch;
            }
        }
        // 至少一行
        if (result.Count == 0)
            result.Add(new ScreenLine(0, 0, 0, 0));
        return result;
    }

    /// <summary>从 offset 开始测量 contentWidth 视觉宽度内的字符数</summary>
    private static (int chars, int vw) MeasureSlice(string text, int start, int maxVW)
    {
        int chars = 0, vw = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = start; i < runes.Count; i++)
        {
            var w = runes[i].Value > 127 ? 2 : 1;
            if (vw + w > maxVW) break;
            vw += w;
            chars++;
        }
        return (chars, vw);
    }

    /// <summary>硬行坐标 → 屏幕坐标</summary>
    private static (int scrLine, int scrCol) HardToScreen(
        List<StringBuilder> hardLines, int cy, int cx, int contentWidth)
    {
        var screenLines = BuildScreenLines(hardLines, contentWidth);
        int scrLine = 0, scrCol = 0;
        for (int i = 0; i < screenLines.Count; i++)
        {
            var sl = screenLines[i];
            if (sl.HardLine == cy && cx >= sl.HardOffset && cx <= sl.HardOffset + sl.Chars)
            {
                scrLine = i;
                var beforeText = hardLines[cy].ToString()[sl.HardOffset..cx];
                scrCol = VW(beforeText);
                return (scrLine, scrCol);
            }
            if (sl.HardLine < cy || (sl.HardLine == cy && sl.HardOffset + sl.Chars <= cx))
                scrLine = i + 1;
        }
        // 光标在末尾
        scrLine = screenLines.Count - 1;
        var last = screenLines[^1];
        scrCol = last.VW;
        return (scrLine, scrCol);
    }

    /// <summary>屏幕坐标 → 硬行坐标</summary>
    private static (int cy, int cx) ScreenToHard(
        List<StringBuilder> hardLines, int scrLine, int scrCol, int contentWidth)
    {
        var screenLines = BuildScreenLines(hardLines, contentWidth);
        if (scrLine >= screenLines.Count)
        {
            scrLine = screenLines.Count - 1;
            scrCol = screenLines[^1].VW;
        }
        if (scrLine < 0) { scrLine = 0; scrCol = 0; }

        var sl = screenLines[scrLine];
        var text = hardLines[sl.HardLine].ToString();
        var slice = text.Substring(sl.HardOffset, Math.Min(sl.Chars, text.Length - sl.HardOffset));
        // 在 slice 中找到 scrCol 视觉宽度对应的字符位置
        int cx = sl.HardOffset;
        int vw = 0;
        foreach (var rune in slice.EnumerateRunes())
        {
            var w = rune.Value > 127 ? 2 : 1;
            if (vw + w > scrCol) break;
            vw += w;
            cx++;
        }
        return (sl.HardLine, cx);
    }

    // ================================================================
    // 光标移动（软折行感知）
    // ================================================================

    private static void MoveScreenUp(List<StringBuilder> lines, ref int cy, ref int cx, int cw)
    {
        var screenLines = BuildScreenLines(lines, cw);
        var (scrCy, scrCx) = HardToScreen(lines, cy, cx, cw);
        if (scrCy > 0)
        {
            (cy, cx) = ScreenToHard(lines, scrCy - 1, scrCx, cw);
        }
    }

    private static void MoveScreenDown(List<StringBuilder> lines, ref int cy, ref int cx, int cw)
    {
        var screenLines = BuildScreenLines(lines, cw);
        var (scrCy, scrCx) = HardToScreen(lines, cy, cx, cw);
        if (scrCy < screenLines.Count - 1)
        {
            (cy, cx) = ScreenToHard(lines, scrCy + 1, scrCx, cw);
        }
    }

    private static void MoveLeft(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx > 0) cx--;
        else if (cy > 0) { cy--; cx = lines[cy].Length; }
    }

    private static void MoveRight(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx < lines[cy].Length) cx++;
        else if (cy < lines.Count - 1) { cy++; cx = 0; }
    }

    // ================================================================
    // 编辑
    // ================================================================

    private static void InsertChar(List<StringBuilder> lines, ref int cy, ref int cx, char ch)
    {
        lines[cy].Insert(cx, ch);
        cx++;
    }

    private static void NewHardLine(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        var rest = lines[cy].ToString()[cx..];
        lines[cy].Remove(cx, lines[cy].Length - cx);
        lines.Insert(cy + 1, new StringBuilder(rest));
        cy++;
        cx = 0;
    }

    private static void Backspace(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx > 0) { lines[cy].Remove(cx - 1, 1); cx--; }
        else if (cy > 0)
        {
            cx = lines[cy - 1].Length;
            lines[cy - 1].Append(lines[cy]);
            lines.RemoveAt(cy);
            cy--;
        }
    }

    private static void DeleteFwd(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx < lines[cy].Length) lines[cy].Remove(cx, 1);
        else if (cy < lines.Count - 1)
        {
            lines[cy].Append(lines[cy + 1]);
            lines.RemoveAt(cy + 1);
        }
    }

    private static void Paste(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        var clip = ReadClipboard();
        if (string.IsNullOrEmpty(clip)) return;
        foreach (var line in clip.Replace("\r\n", "\n").Split('\n'))
        {
            if (lines.Count > 1 || lines[0].Length > 0 || cy > 0)
                NewHardLine(lines, ref cy, ref cx);
            foreach (char ch in line)
                InsertChar(lines, ref cy, ref cx, ch);
        }
    }

    // ================================================================
    // 渲染
    // ================================================================

    private static void RenderArea(List<StringBuilder> hardLines, int cy, int cx,
        int screenScroll, int tw, int contentWidth, int vh,
        List<ScreenLine> screenLines, int startRow)
    {
        Console.CursorVisible = false;
        var sb = new StringBuilder();
        sb.Append($"[{startRow};1H[?25l");

        // ---- 顶线 ----
        sb.Append($"[2m╭{new string('─', Math.Max(0, tw - 2))}╮[0m\r\n");

        // ---- 内容区 (固定 vh 行高) ----
        for (int i = 0; i < vh; i++)
        {
            var si = screenScroll + i;
            sb.Append("[2m│[0m ");

            if (si < screenLines.Count)
            {
                var sl = screenLines[si];
                if (sl.Chars > 0)
                {
                    var text = hardLines[sl.HardLine].ToString();
                    var slice = text.Substring(sl.HardOffset,
                        Math.Min(sl.Chars, text.Length - sl.HardOffset));
                    sb.Append(slice);
                    var pad = contentWidth - sl.VW;
                    if (pad > 0) sb.Append(new string(' ', pad));
                }
                else
                {
                    sb.Append(new string(' ', contentWidth));
                }
            }
            else
            {
                // 空行填充
                sb.Append(new string(' ', contentWidth));
            }

            sb.Append(" [2m│[0m\r\n");
        }

        // ---- 底线 ----
        sb.Append($"[2m╰{new string('─', Math.Max(0, tw - 2))}╯[0m\r\n");

        // ---- 状态栏 ----
        var hardLinesCount = hardLines.Count;
        var mode = hardLinesCount > 1 || (hardLinesCount == 1 && hardLines[0].Length > tw - 4)
            ? "多行" : "聊天";
        var chCount = hardLines.Sum(l => l.Length);
        var status = $" {mode}  L{cy + 1}:C{cx + 1}  {chCount}字符  Enter发送 Ctrl+Enter换行 Esc取消";
        var maxSLen = tw - 4;
        if (status.Length > maxSLen) status = status[..maxSLen];
        sb.Append($"[2m│ {status}{new string(' ', Math.Max(0, maxSLen - status.Length + 1))}│[0m");

        // ---- 光标 ----
        var (scrCy, scrCx) = HardToScreen(hardLines, cy, cx, contentWidth);
        var cursorScreenRow = startRow + 1 + (scrCy - screenScroll);
        var cursorScreenCol = 2 + scrCx + 1; // +1: ANSI 列号从1开始
        cursorScreenCol = Math.Clamp(cursorScreenCol, 2, tw - 2);
        sb.Append($"[{cursorScreenRow};{cursorScreenCol}H[?25h");

        Console.Write(sb.ToString());
    }

    private static void ClearArea(int startRow, int tw)
    {
        Console.Write($"[{startRow};1H[0J");
    }

    // ================================================================
    // 辅助
    // ================================================================

    private record ScreenLine(int HardLine, int HardOffset, int Chars, int VW);

    private static string JoinLines(List<StringBuilder> lines) =>
        string.Join("\n", lines.Select(l => l.ToString())).TrimEnd();

    private static int VW(string s)
    {
        int w = 0;
        foreach (var rune in s.EnumerateRunes()) w += rune.Value > 127 ? 2 : 1;
        return w;
    }

    private static string? ReadClipboard()
    {
        try
        {
            var cmd = OperatingSystem.IsMacOS() ? "pbpaste"
                : OperatingSystem.IsLinux() ? "xclip -o 2>/dev/null || xsel -b 2>/dev/null"
                : OperatingSystem.IsWindows() ? "powershell -Command Get-Clipboard"
                : null;
            if (cmd == null) return null;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {cmd}" : $"-c \"{cmd}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            var output = p?.StandardOutput.ReadToEnd();
            p?.WaitForExit(2000);
            return output?.TrimEnd();
        }
        catch { return null; }
    }
}
