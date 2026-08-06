using System.Text;

namespace CoreCoderSharp.UI;

/// <summary>
/// 多行输入区 — 分隔线 + 内容区(1~5行) + 状态栏。
/// Enter 换行，Ctrl+Enter 发送，Esc 取消，方向键自由移动光标。
/// </summary>
public static class TuiInput
{
    private const int MaxVisibleLines = 5;
    private static string _lastInput = "";

    /// <summary>读取多行输入，取消返回 null</summary>
    public static string? ReadInput()
    {
        var lines = new List<StringBuilder> { new() };
        int cy = 0, cx = 0;
        int scroll = 0;
        int tw = Console.WindowWidth;

        // 初始：绘制空输入区
        Console.WriteLine(); // 确保在独立行开始
        var startRow = Console.CursorTop;
        RenderArea(lines, cy, cx, scroll, tw, 1, startRow);

        try
        {
            while (true)
            {
                tw = Console.WindowWidth;
                var vh = Math.Clamp(lines.Count, 1, MaxVisibleLines);
                if (cy < scroll) scroll = cy;
                if (cy >= scroll + vh) scroll = cy - vh + 1;
                scroll = Math.Clamp(scroll, 0, Math.Max(0, lines.Count - vh));

                RenderArea(lines, cy, cx, scroll, tw, vh, startRow);

                var key = Console.ReadKey(intercept: true);
                bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

                switch (key.Key)
                {
                    case ConsoleKey.Enter when ctrl || shift:
                        _lastInput = JoinLines(lines);
                        // 清除输入区，回到发送状态
                        ClearArea(startRow, tw);
                        return _lastInput;

                    case ConsoleKey.Escape:
                        ClearArea(startRow, tw);
                        return null;

                    case ConsoleKey.Enter:
                        NewLine(lines, ref cy, ref cx);
                        break;

                    case ConsoleKey.UpArrow:
                        if (cy > 0) { cy--; cx = ClampCol(cx, lines[cy]); }
                        break;
                    case ConsoleKey.DownArrow:
                        if (cy < lines.Count - 1) { cy++; cx = ClampCol(cx, lines[cy]); }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (cx > 0) cx--;
                        else if (cy > 0) { cy--; cx = lines[cy].Length; }
                        break;
                    case ConsoleKey.RightArrow:
                        if (cx < lines[cy].Length) cx++;
                        else if (cy < lines.Count - 1) { cy++; cx = 0; }
                        break;
                    case ConsoleKey.Home: cx = 0; break;
                    case ConsoleKey.End: cx = lines[cy].Length; break;

                    case ConsoleKey.Backspace:
                        Backspace(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Delete:
                        DeleteFwd(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.Tab:
                        for (int t = 0; t < 4; t++) Insert(lines, ref cy, ref cx, ' ');
                        break;

                    case ConsoleKey.V when ctrl:
                        Paste(lines, ref cy, ref cx);
                        break;
                    case ConsoleKey.K when ctrl:
                        // 删除从光标到行尾
                        if (cx < lines[cy].Length)
                            lines[cy].Remove(cx, lines[cy].Length - cx);
                        break;
                    case ConsoleKey.U when ctrl:
                        // 删除当前行
                        if (lines.Count > 1)
                        {
                            lines.RemoveAt(cy);
                            if (cy >= lines.Count) cy = lines.Count - 1;
                            cx = 0;
                        }
                        else { lines[cy].Clear(); cx = 0; }
                        break;

                    default:
                        if (key.KeyChar >= ' ')
                            Insert(lines, ref cy, ref cx, key.KeyChar);
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
    // 渲染
    // ================================================================

    private static void RenderArea(List<StringBuilder> lines, int cy, int cx,
        int scroll, int tw, int vh, int startRow)
    {
        Console.CursorVisible = false;
        var sb = new StringBuilder();
        sb.Append($"[{startRow};1H"); // 跳到起始行
        sb.Append("[?25l");          // 隐藏光标

        // ---- 顶分隔线 ----
        sb.Append($"[2m╭{new string('─', Math.Max(0, tw - 2))}╮[0m\r\n");

        // ---- 内容区 ----
        for (int i = 0; i < vh; i++)
        {
            int bi = scroll + i;
            sb.Append("[2m│[0m");

            if (bi < lines.Count)
            {
                var text = lines[bi].ToString();
                bool isCursorLine = (bi == cy);
                // 前缀：边框+空格(2) + 内容
                var maxVW = tw - 4;
                int vw = 0;
                foreach (var rune in text.EnumerateRunes())
                {
                    var w = rune.Value > 127 ? 2 : 1;
                    if (vw + w > maxVW) break;
                    sb.Append(rune.ToString());
                    vw += w;
                }
                // 填充到右边界
                var padding = Math.Max(0, maxVW - vw);
                sb.Append(new string(' ', padding));
            }
            else
            {
                sb.Append(new string(' ', tw - 4));
            }

            sb.Append("  [2m│[0m\r\n");
        }

        // ---- 底分隔 ----
        sb.Append($"[2m╰{new string('─', Math.Max(0, tw - 2))}╯[0m\r\n");

        // ---- 状态栏 ----
        var mode = lines.Count > 1 ? "多行" : "聊天";
        var chCount = lines.Sum(l => l.Length);
        var status = $" {mode}  L{cy + 1}:C{cx + 1}  {chCount}字符  " +
                     $"Ctrl+Enter 发送  Esc 取消";
        // 不截断，左对齐
        var maxSLen = tw - 4;
        if (status.Length > maxSLen) status = status[..maxSLen];
        sb.Append($"[2m│ {status}{new string(' ', Math.Max(0, maxSLen - status.Length + 1))}│[0m");

        // ---- 光标定位 ----
        // 光标在屏幕上的坐标
        var contentTop = startRow + 1; // 跳过顶线
        var cursorScreenRow = contentTop + (cy - scroll);
        var textBeforeCursor = cx > 0 ? lines[cy].ToString()[..Math.Min(cx, lines[cy].Length)] : "";
        var cursorScreenCol = 2 + VW(textBeforeCursor) + 1; // +1 是因为 ANSI 列从 1 开始
        cursorScreenCol = Math.Min(cursorScreenCol, tw - 2);
        sb.Append($"[{cursorScreenRow};{cursorScreenCol}H");
        sb.Append("[?25h");

        Console.Write(sb.ToString());
    }

    private static void ClearArea(int startRow, int tw)
    {
        var sb = new StringBuilder();
        sb.Append($"[{startRow};1H");
        sb.Append("[0J"); // 清除从光标到屏幕底
        Console.Write(sb.ToString());
    }

    // ================================================================
    // 编辑操作
    // ================================================================

    private static void Insert(List<StringBuilder> lines, ref int cy, ref int cx, char ch)
    {
        lines[cy].Insert(cx, ch);
        cx++;
    }

    private static void NewLine(List<StringBuilder> lines, ref int cy, ref int cx)
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
        if (cx < lines[cy].Length)
            lines[cy].Remove(cx, 1);
        else if (cy < lines.Count - 1)
        {
            lines[cy].Append(lines[cy + 1]);
            lines.RemoveAt(cy + 1);
        }
    }

    private static void Paste(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        string? clip = null;
        try
        {
            clip = OperatingSystem.IsMacOS() ? RunCmd("pbpaste")
                 : OperatingSystem.IsLinux() ? RunCmd("xclip -o 2>/dev/null || xsel -b 2>/dev/null")
                 : OperatingSystem.IsWindows() ? RunCmd("powershell -Command Get-Clipboard")
                 : null;
        }
        catch { }

        if (string.IsNullOrEmpty(clip)) return;

        var pasteLines = clip.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < pasteLines.Length; i++)
        {
            if (i > 0) NewLine(lines, ref cy, ref cx);
            foreach (char ch in pasteLines[i])
                Insert(lines, ref cy, ref cx, ch);
        }
    }

    // ================================================================
    // 辅助
    // ================================================================

    private static int ClampCol(int col, StringBuilder line) =>
        Math.Clamp(col, 0, line.Length);

    private static string JoinLines(List<StringBuilder> lines) =>
        string.Join("\n", lines.Select(l => l.ToString())).TrimEnd();

    private static int VW(string s)
    {
        int w = 0;
        foreach (var rune in s.EnumerateRunes())
            w += rune.Value > 127 ? 2 : 1;
        return w;
    }

    private static string? RunCmd(string cmd)
    {
        try
        {
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
