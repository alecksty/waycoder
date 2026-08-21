using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Custom;

/// <summary>
/// 多行输入区 + 智能提示面板。
/// Enter 发送，Ctrl+Enter 硬换行，文本软折行。
/// 键入 / # ! 时弹出悬浮建议面板，箭头选择。
/// </summary>
public static class TuiChatInput
{
    private const int MaxScreenLines = 5;
    private const int MaxSuggestions = 10;

    private enum Mode { Normal, Suggest }
    private static Mode _mode;
    private static List<string> _suggestions = [];
    private static int _suggestIdx;

    // 内置命令列表（从注册表自动推导）
    private static string[] Commands => SlashCommandRegistry.AllNames
        .Concat(SlashCommandRegistry.Commands
            .Where(c => c.Usage != null)
            .Select(c => c.Usage!))
        .Append("quit")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>读取输入，Enter 发送返回文本，Esc 取消返回 null</summary>
    public static string? ReadInput()
    {
        var lines = new List<StringBuilder> { new() };
        int cy = 0, cx = 0;
        int scrScroll = 0;
        int tw = Tty.Cols;
        int contentW = Math.Max(20, tw - 4);
        
        _mode = Mode.Normal;
        _suggestions = [];
        _suggestIdx = 0;

        // 保存光标位置，后续每次渲染从这里开始清除+重绘
        Tty.WriteLine();
        Tty.SaveCursor();

        try
        {
            while (true)
            {
                tw = Tty.Cols;
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

                var key = Tty.ReadKey();
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
                    Tty.RestoreCursor();
                    Tty.Write(AnsiTty.ClearToEndScreen); // 恢复+清除输入区
                    Tty.WriteLine();
                    return JoinLines(lines);
                }
                if (key.Key == ConsoleKey.Escape)
                {
                    Tty.RestoreCursor();
                    Tty.Write(AnsiTty.ClearToEndScreen); // 恢复+清除输入区
                    return null;
                }
            }
        }
        finally
        {
            Tty.ShowCursor();
        }
    }

    // ---- 普通按键处理 ----
    private static void ApplyNormalKey(List<StringBuilder> lines,
        ref int cy, ref int cx, ref int _,
        ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        int cw = Math.Max(20, Tty.Cols - 4);

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
            '/' => Commands.Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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
        // start 与返回的 chars 均为 char 索引/计数（rune 感知，正确处理代理对/emoji）
        int chars = 0, vw = 0;
        int charIdx = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            int rl = rune.ToString().Length;
            if (charIdx < start) { charIdx += rl; continue; }
            var w = AnsiHelper.RuneWidth(rune);
            if (vw + w > maxVW) break;
            vw += w;
            chars += rl;
            charIdx += rl;
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
            var w = AnsiHelper.RuneWidth(rune);
            if (vw + w > scrCol) break;
            vw += w;
            cx += rune.ToString().Length;
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
        if (cx > 0)
        {
            cx--;
            // 光标不落在代理对中间（emoji/CJK 扩展 B）
            if (cx > 0 && char.IsHighSurrogate(lines[cy][cx - 1]) && char.IsLowSurrogate(lines[cy][cx]))
                cx--;
        }
        else if (cy > 0) { cy--; cx = lines[cy].Length; }
    }

    private static void MoveRight(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx < lines[cy].Length)
        {
            cx++;
            if (cx < lines[cy].Length && char.IsHighSurrogate(lines[cy][cx - 1]) && char.IsLowSurrogate(lines[cy][cx]))
                cx++;
        }
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
        if (cx > 0)
        {
            int delLen = cx >= 2 && char.IsHighSurrogate(lines[cy][cx - 2]) && char.IsLowSurrogate(lines[cy][cx - 1]) ? 2 : 1;
            lines[cy].Remove(cx - delLen, delLen);
            cx -= delLen;
        }
        else if (cy > 0)
        { cx = lines[cy - 1].Length; lines[cy - 1].Append(lines[cy]); lines.RemoveAt(cy); cy--; }
    }

    private static void DeleteFwd(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        if (cx < lines[cy].Length)
        {
            int delLen = cx + 1 < lines[cy].Length && char.IsHighSurrogate(lines[cy][cx]) && char.IsLowSurrogate(lines[cy][cx + 1]) ? 2 : 1;
            lines[cy].Remove(cx, delLen);
        }
        else if (cy < lines.Count - 1)
        { lines[cy].Append(lines[cy + 1]); lines.RemoveAt(cy + 1); }
    }

    private static void Paste(List<StringBuilder> lines, ref int cy, ref int cx)
    {
        var clip = ReadClipboard();
        if (string.IsNullOrEmpty(clip)) return;

        // 粘贴确认：超长或多行时确认
        var pasteLines = clip.Replace("\r\n", "\n").Split('\n');
        if (clip.Length > 500 || pasteLines.Length > 3)
        {
            var preview = clip.Length > 200 ? ContextManager.TruncateByRunes(clip, 200) + "..." : clip;
            Tty.RestoreCursor();
            Tty.Write(AnsiTty.ClearToEndScreen); // 恢复光标 + 清除输入区
            Tty.Write($"粘贴 {pasteLines.Length} 行 / {clip.Length} 字符? ");
            Tty.WriteLine(preview);
            Tty.Write("[Y] 确认粘贴  [N] 取消 ");
            var confirm = Tty.ReadKey();
            if (char.ToUpperInvariant(confirm.KeyChar) != 'Y') return;
        }

        foreach (var line in pasteLines)
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
        Tty.HideCursor();
        var sb = new StringBuilder();

        // 恢复保存的光标位置 + 清除到底，防止旧帧累积
        sb.Append(AnsiTty.CursorRestore);
        sb.Append(AnsiTty.ClearToEndScreen);

        // ---- 建议面板 ----
        if (_mode == Mode.Suggest && suggestH > 0)
        {
            RenderSuggestions(sb, tw, suggestH);
        }

        // ---- 输入区 ----

        // 顶线
        sb.Append(AnsiTty.SgrDim);
        sb.Append($"╭{new string('─', Math.Max(0, tw - 2))}╮");
        sb.Append(AnsiTty.SgrReset);
        sb.Append("\r\n");

        // 内容区
        for (int i = 0; i < vh; i++)
        {
            var si = scrScroll + i;
            sb.Append(AnsiTty.SgrDim);
            sb.Append('│');
            sb.Append(AnsiTty.SgrReset);
            sb.Append(' ');
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
            sb.Append(' ');
            sb.Append(AnsiTty.SgrDim);
            sb.Append('│');
            sb.Append(AnsiTty.SgrReset);
            sb.Append("\r\n");
        }

        // 底线
        sb.Append(AnsiTty.SgrDim);
        sb.Append($"╰{new string('─', Math.Max(0, tw - 2))}╯");
        sb.Append(AnsiTty.SgrReset);
        sb.Append("\r\n");

        // 状态栏
        var hardCount = hardLines.Count;
        var modeLabel = hardCount > 1 || (hardCount == 1 && hardLines[0].Length > contentW)
            ? "多行" : "聊天";
        var chCount = hardLines.Sum(l => l.Length);
        var status = $" {modeLabel}  L{cy + 1}:C{cx + 1}  {chCount}字符  Enter发送 Ctrl+Enter换行 Esc取消";
        var statusMax = Math.Max(0, tw - 4);
        if (status.Length > statusMax) status = status[..statusMax];
        sb.Append(AnsiTty.SgrDim);
        sb.Append($"│ {status}{new string(' ', Math.Max(0, statusMax - status.Length + 1))}│");
        sb.Append(AnsiTty.SgrReset);

        // 光标
        var (scrCy, scrCx) = HardToScreen(hardLines, cy, cx, contentW);
        // 光标行 = 建议面板行数 + 顶线(1) + 内容偏移 + 1-based
        // 光标：相对定位（比绝对定位更可靠，不受建议面板 CJK 宽度影响）
        var linesUp = vh + 1 - (scrCy - scrScroll);
        var cursorCol = 2 + scrCx + 1;
        cursorCol = Math.Clamp(cursorCol, 2, tw - 2);
        sb.Append('\r');
        sb.Append(AnsiTty.CursorUp(linesUp));
        sb.Append('\r');
        sb.Append(AnsiTty.CursorForward(cursorCol));
        sb.Append(AnsiTty.CursorShow);

        Tty.Write(sb.ToString());
    }

    private static void RenderSuggestions(StringBuilder sb, int tw, int h)
    {
        // 顶线
        var trigger = _suggestions.Count > 0 ? _suggestions[0][0] : '?';
        var title = trigger switch { '/' => "命令", '#' => "文件", '!' => "Shell", _ => "建议" };
        var hint = " ↑↓选择 Tab/Enter确认 Esc取消";
        var titleVW = 3 + VW(title) + VW(hint) + 1;
        sb.Append(AnsiTty.FgCode(AnsiColors.Cyan));
        sb.Append($"╭─ {title}{hint} {new string('─', Math.Max(0, tw - titleVW - 1))}╮");
        sb.Append(AnsiTty.SgrReset);
        sb.Append("\r\n");

        for (int i = 0; i < h; i++)
        {
            sb.Append(AnsiTty.FgCode(AnsiColors.Cyan));
            sb.Append('│');
            sb.Append(AnsiTty.SgrReset);
            sb.Append(' ');
            if (i < _suggestions.Count)
            {
                var text = _suggestions[i];
                // 截断
                var maxW = tw - 4;
                if (VW(text) > maxW)
                {
                    int vw = 0, ci = 0;
                    foreach (var r in text.EnumerateRunes())
                    { var w = AnsiHelper.RuneWidth(r); if (vw + w > maxW - 1) break; vw += w; ci += r.ToString().Length; }
                    text = text[..ci] + "…";
                }
                var isSel = i == _suggestIdx;
                var fill = Math.Max(0, tw - 4 - VW(text) - 2);
                if (isSel)
                {
                    sb.Append(AnsiTty.FgBgCode(AnsiColors.Black, AnsiColors.BgCyan));
                    sb.Append($" {text} {new string(' ', fill)}");
                    sb.Append(AnsiTty.SgrReset);
                }
                else
                    sb.Append($" {text} {new string(' ', fill)}");
            }
            else
            {
                sb.Append(new string(' ', tw - 4));
            }
            sb.Append(' ');
            sb.Append(AnsiTty.FgCode(AnsiColors.Cyan));
            sb.Append('│');
            sb.Append(AnsiTty.SgrReset);
            sb.Append("\r\n");
        }

        // 底线
        sb.Append(AnsiTty.FgCode(AnsiColors.Cyan));
        sb.Append($"╰{new string('─', Math.Max(0, tw - 2))}╯");
        sb.Append(AnsiTty.SgrReset);
    }

    // ================================================================
    // 辅助
    // ================================================================

    private record ScreenLine(int HardLine, int HardOffset, int Chars, int VW);

    private static string JoinLines(List<StringBuilder> lines) =>
        string.Join("\n", lines.Select(l => l.ToString())).TrimEnd();

    private static int VW(string s) => AnsiHelper.DisplayWidth(s);

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
            if (p == null) return null;
            // UI 线程：同步 ReadToEnd 无超时会永久卡死（剪贴板工具被锁）；且守护子进程继承管道会让读永不 EOF。
            // 改为并发读 + WaitForExit(2s) 超时杀进程 + 读完成再带 2s 兜底，总阻塞上限 ~4s。
            var readTask = p.StandardOutput.ReadToEndAsync();
            if (!p.WaitForExit(2000))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return null;
            }
            var finished = Task.WaitAny(readTask, Task.Delay(2000));
            if (finished != 0)
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return null;
            }
            return readTask.Result.TrimEnd();
        }
        catch { return null; }
    }
}
