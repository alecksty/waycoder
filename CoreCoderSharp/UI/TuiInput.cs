using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

/// <summary>
/// 多行输入区 + 智能提示面板。
/// Enter 发送，Ctrl+Enter 硬换行，文本软折行。
/// 键入 / # ! 时弹出悬浮建议面板，箭头选择。
/// </summary>
public static class TuiInput
{
    private const int MaxScreenLines = 5;
    private const int MaxSuggestions = 10;

    private enum Mode { Normal, Suggest }
    private static Mode _mode;
    private static List<string> _suggestions = [];
    private static int _suggestIdx;

    // 内置命令列表
    private static readonly string[] _commands =
    [
        "/help", "/reset", "/model", "/model <名称>", "/tokens",
        "/compact", "/diff", "/save", "/sessions",
        "/debug-on", "/debug-off", "/permissions", "/perm <模式>",
        "/plan", "/todo", "/git-status", "/git-log", "/git-diff",
        "/review", "/lint", "/search <关键词>",
        "/checkpoint", "/undo [编号]", "/checkpoints",
        "/repomap", "/pr [标题]", "/edit [文件]", "/about", "/settings",
        "quit",
    ];

    /// <summary>读取输入，Enter 发送返回文本，Esc 取消返回 null</summary>
    public static string? ReadInput()
    {
        var lines = new List<StringBuilder> { new() };
        int cy = 0, cx = 0;
        int scrScroll = 0;
        int tw = TTY.Cols;
        int contentW = Math.Max(20, tw - 4);
        _mode = Mode.Normal;
        _suggestions = [];
        _suggestIdx = 0;

        // 保存光标位置，后续每次渲染从这里开始清除+重绘
        Console.WriteLine();
        Console.Write("[s"); // ANSI 保存光标位置

        try
        {
            while (true)
            {
                tw = TTY.Cols;
                contentW = Math.Max(20, tw - 4);

                // 计算建议面板
                UpdateSuggestions(lines);
                var suggestH = _mode == Mode.Suggest
                    ? Math.Min(_suggestions.Count, MaxSuggestions) : 0;

                var scrLines = BuildScreenLines(lines, contentW);
                var totalScr = scrLines.Count;
                var vh = totalScr <= MaxScreenLines ? Math.Max(1, totalScr) : MaxScreenLines;
                var (scrCy, scrCx) = HardToScreen(lines, cy, cx, contentW);

                if (scrCy < scrScroll) scrScroll = scrCy;
                if (scrCy >= scrScroll + vh) scrScroll = scrCy - vh + 1;
                scrScroll = Math.Clamp(scrScroll, 0, Math.Max(0, totalScr - vh));

                RenderAll(lines, cy, cx, scrScroll, tw, contentW, vh, scrLines, suggestH);

                var key = Console.ReadKey(intercept: true);
                bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

                // ---- 建议模式下的特殊处理 ----
                if (_mode == Mode.Suggest)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            _mode = Mode.Normal;
                            continue;
                        case ConsoleKey.UpArrow:
                            if (_suggestIdx > 0) _suggestIdx--;
                            continue;
                        case ConsoleKey.DownArrow:
                            if (_suggestIdx < _suggestions.Count - 1
                                && _suggestIdx < MaxSuggestions - 1)
                                _suggestIdx++;
                            continue;
                        case ConsoleKey.Enter:
                            // 接受建议
                            AcceptSuggestion(lines, ref cy, ref cx, contentW);
                            _mode = Mode.Normal;
                            continue;
                        case ConsoleKey.Tab:
                            // Tab 也接受
                            AcceptSuggestion(lines, ref cy, ref cx, contentW);
                            continue;
                        case ConsoleKey.Backspace:
                            ApplyNormalKey(lines, ref cy, ref cx, ref scrScroll,
                                key, ctrl, shift);
                            continue;
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.RightArrow:
                            _mode = Mode.Normal; // 移动光标=退出建议
                            ApplyNormalKey(lines, ref cy, ref cx, ref scrScroll,
                                key, ctrl, shift);
                            continue;
                        default:
                            ApplyNormalKey(lines, ref cy, ref cx, ref scrScroll,
                                key, ctrl, shift);
                            continue;
                    }
                }

                // ---- 普通模式 ----
                ApplyNormalKey(lines, ref cy, ref cx, ref scrScroll,
                    key, ctrl, shift);
                if (key.Key == ConsoleKey.Enter && !ctrl && !shift)
                {
                    Console.Write("[u[J"); // 恢复+清除输入区
                    Console.WriteLine();
                    return JoinLines(lines);
                }
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.Write("[u[J"); // 恢复+清除输入区
                    return null;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    // ---- 普通按键处理 ----
    private static void ApplyNormalKey(List<StringBuilder> lines,
        ref int cy, ref int cx, ref int _,
        ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        int cw = Math.Max(20, TTY.Cols - 4);

        switch (key.Key)
        {
            case ConsoleKey.Enter when ctrl || shift:
                NewHardLine(lines, ref cy, ref cx);
                break;
            case ConsoleKey.UpArrow:
                MoveScreenUp(lines, ref cy, ref cx, cw);
                break;
            case ConsoleKey.DownArrow:
                MoveScreenDown(lines, ref cy, ref cx, cw);
                break;
            case ConsoleKey.LeftArrow:
                MoveLeft(lines, ref cy, ref cx);
                break;
            case ConsoleKey.RightArrow:
                MoveRight(lines, ref cy, ref cx);
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

    // ================================================================
    // 建议系统
    // ================================================================

    private static void UpdateSuggestions(List<StringBuilder> lines)
    {
        var text = JoinLines(lines).TrimStart();
        if (string.IsNullOrEmpty(text)) { _mode = Mode.Normal; return; }

        char trigger = text[0];
        if (trigger != '/' && trigger != '#' && trigger != '!')
        { _mode = Mode.Normal; return; }

        // 只在光标在首行行首附近时触发
        if (_mode == Mode.Normal && lines.Count == 1) { /* 允许 */ }
        else if (_mode != Mode.Suggest) return;

        var prefix = text;
        _suggestions = trigger switch
        {
            '/' => _commands.Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                           .Take(MaxSuggestions).ToList(),
            '#' => GetFileSuggestions(prefix.Length > 1 ? prefix[1..] : ""),
            '!' => ["!<shell 命令> — 直接执行 bash", "!git status", "!ls -la", "!npm test", "!dotnet build"],
            _ => [],
        };

        _mode = Mode.Suggest;
        _suggestIdx = 0;
    }

    private static void AcceptSuggestion(List<StringBuilder> lines, ref int cy, ref int cx, int cw)
    {
        if (_suggestIdx < 0 || _suggestIdx >= _suggestions.Count) return;
        var chosen = _suggestions[_suggestIdx];

        // 替换当前输入
        lines.Clear();
        lines.Add(new StringBuilder(chosen));
        cy = 0;
        cx = chosen.Length;
    }

    private static List<string> GetFileSuggestions(string partial)
    {
        var results = new List<string>();
        try
        {
            var dir = ".";
            var prefix = partial;
            // 解析路径
            var lastSep = partial.LastIndexOfAny(['/', '\\']);
            if (lastSep >= 0)
            {
                dir = partial[..(lastSep + 1)];
                prefix = partial[(lastSep + 1)..];
            }

            if (!Directory.Exists(dir)) return results;

            var entries = Directory.GetFileSystemEntries(dir)
                .Select(p => Path.GetFileName(p))
                .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => !Directory.Exists(Path.Combine(dir, f))) // 目录优先
                .ThenBy(f => f)
                .Take(MaxSuggestions);

            foreach (var entry in entries)
            {
                var name = entry;
                var full = dir.TrimEnd('/') + "/" + name;
                if (Directory.Exists(full)) name += "/";
                results.Add("#" + (lastSep >= 0 ? partial[..(lastSep + 1)] : "") + name);
            }
        }
        catch { }
        return results;
    }

    // ================================================================
    // 屏幕行计算（软折行）
    // ================================================================

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
        if (result.Count == 0)
            result.Add(new ScreenLine(0, 0, 0, 0));
        return result;
    }

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

    private static (int scrLine, int scrCol) HardToScreen(
        List<StringBuilder> hardLines, int cy, int cx, int contentWidth)
    {
        var screenLines = BuildScreenLines(hardLines, contentWidth);
        for (int i = 0; i < screenLines.Count; i++)
        {
            var sl = screenLines[i];
            if (sl.HardLine == cy && cx >= sl.HardOffset && cx <= sl.HardOffset + sl.Chars)
            {
                var beforeText = hardLines[cy].ToString()[sl.HardOffset..cx];
                return (i, VW(beforeText));
            }
        }
        // 光标在末尾
        var last = screenLines[^1];
        return (screenLines.Count - 1, last.VW);
    }

    private static (int cy, int cx) ScreenToHard(
        List<StringBuilder> hardLines, int scrLine, int scrCol, int contentWidth)
    {
        var screenLines = BuildScreenLines(hardLines, contentWidth);
        if (scrLine >= screenLines.Count)
        { scrLine = screenLines.Count - 1; scrCol = screenLines[^1].VW; }
        if (scrLine < 0) { scrLine = 0; scrCol = 0; }

        var sl = screenLines[scrLine];
        var text = hardLines[sl.HardLine].ToString();
        var slice = text.Substring(sl.HardOffset, Math.Min(sl.Chars, text.Length - sl.HardOffset));
        int cx = sl.HardOffset, vw = 0;
        foreach (var rune in slice.EnumerateRunes())
        {
            var w = rune.Value > 127 ? 2 : 1;
            if (vw + w > scrCol) break;
            vw += w;
            cx++;
        }
        return (sl.HardLine, cx);
    }

    // ---- 光标移动 ----

    private static void MoveScreenUp(List<StringBuilder> lines, ref int cy, ref int cx, int cw)
    {
        var sl = BuildScreenLines(lines, cw);
        var (scrCy, scrCx) = HardToScreen(lines, cy, cx, cw);
        if (scrCy > 0) (cy, cx) = ScreenToHard(lines, scrCy - 1, scrCx, cw);
    }

    private static void MoveScreenDown(List<StringBuilder> lines, ref int cy, ref int cx, int cw)
    {
        var sl = BuildScreenLines(lines, cw);
        var (scrCy, scrCx) = HardToScreen(lines, cy, cx, cw);
        if (scrCy < sl.Count - 1) (cy, cx) = ScreenToHard(lines, scrCy + 1, scrCx, cw);
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

    // ---- 编辑 ----

    private static void InsertChar(List<StringBuilder> lines, ref int cy, ref int cx, char ch)
    { lines[cy].Insert(cx, ch); cx++; }

    private static void NewHardLine(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        var rest = lines[cy].ToString()[cx..];
        lines[cy].Remove(cx, lines[cy].Length - cx);
        lines.Insert(cy + 1, new StringBuilder(rest));
        cy++; cx = 0;
    }

    private static void Backspace(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx > 0) { lines[cy].Remove(cx - 1, 1); cx--; }
        else if (cy > 0)
        { cx = lines[cy - 1].Length; lines[cy - 1].Append(lines[cy]); lines.RemoveAt(cy); cy--; }
    }

    private static void DeleteFwd(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx < lines[cy].Length) lines[cy].Remove(cx, 1);
        else if (cy < lines.Count - 1)
        { lines[cy].Append(lines[cy + 1]); lines.RemoveAt(cy + 1); }
    }

    private static void Paste(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        var clip = ReadClipboard();
        if (string.IsNullOrEmpty(clip)) return;
        foreach (var line in clip.Replace("\r\n", "\n").Split('\n'))
        {
            if (lines.Count > 1 || lines[0].Length > 0 || cy > 0)
                NewHardLine(lines, ref cy, ref cx);
            foreach (char ch in line) InsertChar(lines, ref cy, ref cx, ch);
        }
    }

    // ================================================================
    // 渲染
    // ================================================================

    private static void RenderAll(List<StringBuilder> hardLines, int cy, int cx,
        int scrScroll, int tw, int contentW, int vh,
        List<ScreenLine> scrLines, int suggestH)
    {
        Console.CursorVisible = false;
        var sb = new StringBuilder();

        // 恢复保存的光标位置 + 清除到底，防止旧帧累积
        sb.Append("[u[J");

        // ---- 建议面板 ----
        if (_mode == Mode.Suggest && suggestH > 0)
        {
            RenderSuggestions(sb, tw, suggestH);
        }

        // ---- 输入区 ----

        // 顶线
        sb.Append($"[2m╭{new string('─', Math.Max(0, tw - 2))}╮[0m\r\n");

        // 内容区
        for (int i = 0; i < vh; i++)
        {
            var si = scrScroll + i;
            sb.Append("[2m│[0m ");
            if (si < scrLines.Count)
            {
                var sl = scrLines[si];
                if (sl.Chars > 0)
                {
                    var text = hardLines[sl.HardLine].ToString();
                    var slice = text.Substring(sl.HardOffset,
                        Math.Min(sl.Chars, text.Length - sl.HardOffset));
                    sb.Append(slice);
                    var pad = contentW - sl.VW;
                    if (pad > 0) sb.Append(new string(' ', pad));
                }
                else sb.Append(new string(' ', contentW));
            }
            else sb.Append(new string(' ', contentW));
            sb.Append(" [2m│[0m\r\n");
        }

        // 底线
        sb.Append($"[2m╰{new string('─', Math.Max(0, tw - 2))}╯[0m\r\n");

        // 状态栏
        var hardCount = hardLines.Count;
        var modeLabel = hardCount > 1 || (hardCount == 1 && hardLines[0].Length > contentW)
            ? "多行" : "聊天";
        var chCount = hardLines.Sum(l => l.Length);
        var status = $" {modeLabel}  L{cy + 1}:C{cx + 1}  {chCount}字符  Enter发送 Ctrl+Enter换行 Esc取消";
        var statusMax = tw - 4;
        if (status.Length > statusMax) status = status[..statusMax];
        sb.Append($"[2m│ {status}{new string(' ', Math.Max(0, statusMax - status.Length + 1))}│[0m");

        // 光标
        var (scrCy, scrCx) = HardToScreen(hardLines, cy, cx, contentW);
        // 光标行 = 建议面板行数 + 顶线(1) + 内容偏移 + 1-based
        // 光标：相对定位（比绝对定位更可靠，不受建议面板 CJK 宽度影响）
      var linesUp = vh + 1 - (scrCy - scrScroll);
        var cursorCol = 2 + scrCx + 1;
        cursorCol = Math.Clamp(cursorCol, 2, tw - 2);
        sb.Append($"\r[{linesUp}A\r[{cursorCol}C[?25h");

        Console.Write(sb.ToString());
    }

    private static void RenderSuggestions(StringBuilder sb, int tw, int h)
    {
        // 顶线
        var trigger = _suggestions.Count > 0 ? _suggestions[0][0] : '?';
        var title = trigger switch { '/' => "命令", '#' => "文件", '!' => "Shell", _ => "建议" };
        var hint = " ↑↓选择 Tab/Enter确认 Esc取消";
        var titleVW = 3 + VW(title) + VW(hint) + 1;
        sb.Append($"[36m╭─ {title}{hint} {new string('─', Math.Max(0, tw - titleVW - 1))}╮[0m\r\n");

        for (int i = 0; i < h; i++)
        {
            sb.Append("[36m│[0m ");
            if (i < _suggestions.Count)
            {
                var text = _suggestions[i];
                // 截断
                var maxW = tw - 4;
                if (VW(text) > maxW)
                {
                    int vw = 0, ci = 0;
                    foreach (var r in text.EnumerateRunes())
                    { var w = r.Value > 127 ? 2 : 1; if (vw + w > maxW - 1) break; vw += w; ci++; }
                    text = text[..ci] + "…";
                }
                var isSel = i == _suggestIdx;
                var fill = Math.Max(0, tw - 4 - VW(text) - 2);
                if (isSel)
                    sb.Append($"[30;46m {text} {new string(' ', fill)}[0m");
                else
                    sb.Append($" {text} {new string(' ', fill)}");
            }
            else
            {
                sb.Append(new string(' ', tw - 4));
            }
            sb.Append(" [36m│[0m\r\n");
        }

        // 底线
        sb.Append($"[36m╰{new string('─', Math.Max(0, tw - 2))}╯[0m");
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
