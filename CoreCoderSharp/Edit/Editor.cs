using System.Text;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI;

namespace CoreCoderSharp;

/// <summary>
/// 终端源码编辑器 —— 支持语法高亮、行号、键盘操作。
/// 用法: Editor.RunAsync(filePath) / Editor.PickAndRunAsync()
/// </summary>
public class Editor
{
    // ---- 缓冲区 ----
    private readonly List<StringBuilder> _lines = [];
    private string _filePath = "";
    private bool _modified;

    // ---- 光标 (0-based) ----
    private int _cy, _cx;
    // ---- 滚动偏移 ----
    private int _scroll;

    // ---- 终端尺寸 ----
    private int _tw, _th;

    // ---- 撤销 ----
    private readonly Stack<EditAction> _undo = new();
    private record EditAction(int Line, int Col, string OldLine, string NewLine, int OldCount);

    // ---- 剪贴板 ----
    private static string _clipboard = "";

    // ---- 语法 ----
    private readonly Syntax _syntax;

    // ================================================================
    // 入口
    // ================================================================

    /// <summary>打开文件编辑器。文件不存在则创建新文件。</summary>
    public static async Task RunAsync(string? filePath)
    {
        filePath ??= "untitled.txt";
        if (!File.Exists(filePath) && !filePath.Contains('.'))
            filePath += ".txt";

        var ed = new Editor(filePath);
        await ed.RunLoopAsync();
    }

    /// <summary>无参数时：让用户输入文件名或选择最近修改的文件。</summary>
    public static async Task PickAndRunAsync()
    {
        // 尝试显示最近修改的文件供选择
        var recent = EditFileTool.ChangedFiles.ToList();
        if (recent.Count > 0)
        {
            var choices = new List<string> { "📁 输入文件路径..." };
            choices.AddRange(recent.Take(9));
            var pick = UI.TuiList.Select("选择要编辑的文件 ↑↓", choices);
            if (pick == null) return;
            if (pick.StartsWith("📁"))
            {
                var path = UI.TuiPrompt.Ask("文件路径");
                if (string.IsNullOrWhiteSpace(path)) return;
                await RunAsync(path.Trim());
            }
            else
            {
                await RunAsync(pick);
            }
        }
        else
        {
            var path = UI.TuiPrompt.Ask("文件路径 (可创建新文件)");
            if (string.IsNullOrWhiteSpace(path)) return;
            await RunAsync(path.Trim());
        }
    }

    // ================================================================
    // 构造
    // ================================================================

    private Editor(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
        _syntax = Syntax.ForFile(_filePath);

        if (File.Exists(_filePath))
        {
            foreach (var line in File.ReadAllLines(_filePath, System.Text.Encoding.UTF8))
                _lines.Add(new StringBuilder(line));
        }

        if (_lines.Count == 0)
            _lines.Add(new StringBuilder());

        _cy = 0;
        _cx = 0;
        _scroll = 0;
        _modified = false;
    }

    // ================================================================
    // 主循环
    // ================================================================

    private async Task RunLoopAsync()
    {
        (_tw, _th) = (Console.WindowWidth, Console.WindowHeight);
        Console.CursorVisible = false;
        Console.TreatControlCAsInput = true;

        // 如果 ScreenManager 已激活，不切换屏幕
        var smActive = ScreenManager.Instance.IsActive;

        try
        {
            Render();
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(30);
                    if (Console.WindowWidth != _tw || Console.WindowHeight != _th)
                    {
                        (_tw, _th) = (Console.WindowWidth, Console.WindowHeight);
                        Render();
                    }
                    continue;
                }

                var key = Console.ReadKey(intercept: true);
                if (!HandleKey(key)) break;
                Render();
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.TreatControlCAsInput = false;
            if (!smActive) Console.Clear();
        }
    }

    // ================================================================
    // 键盘处理
    // ================================================================

    private bool HandleKey(ConsoleKeyInfo key)
    {
        // 修饰键
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        switch (key.Key)
        {
            // ---- 退出 ----
            case ConsoleKey.Escape:
            case ConsoleKey.Q when ctrl:
                if (_modified) PromptSave();
                return false;

            // ---- 保存 ----
            case ConsoleKey.S when ctrl:
                Save();
                return true;

            // ---- 撤销 ----
            case ConsoleKey.Z when ctrl:
                Undo();
                return true;

            // ---- 光标移动 ----
            case ConsoleKey.UpArrow:    MoveCursor(0, -1); return true;
            case ConsoleKey.DownArrow:  MoveCursor(0, 1); return true;
            case ConsoleKey.LeftArrow:  MoveCursor(-1, 0); return true;
            case ConsoleKey.RightArrow: MoveCursor(1, 0); return true;
            case ConsoleKey.Home:       _cx = 0; return true;
            case ConsoleKey.End:        _cx = _lines[_cy].Length; return true;
            case ConsoleKey.PageUp:     _cy = Math.Max(0, _cy - (_th - 5)); MoveCursor(0, 0); return true;
            case ConsoleKey.PageDown:   _cy = Math.Min(_lines.Count - 1, _cy + (_th - 5)); MoveCursor(0, 0); return true;

            // ---- 编辑 ----
            case ConsoleKey.Backspace:  Backspace(); return true;
            case ConsoleKey.Delete:     Delete(); return true;
            case ConsoleKey.Enter:      NewLine(); return true;
            case ConsoleKey.Tab:        InsertText("    "); return true;

            // ---- 剪切/复制/粘贴 (Ctrl+X/C/V) ----
            case ConsoleKey.X when ctrl: CutLine(); return true;
            case ConsoleKey.C when ctrl:
                _clipboard = _lines[_cy].ToString();
                return true;
            case ConsoleKey.V when ctrl:
                if (!string.IsNullOrEmpty(_clipboard)) InsertText(_clipboard);
                return true;

            // ---- 删除行 ----
            case ConsoleKey.Y when ctrl:
                DeleteLine();
                return true;

            // ---- 跳转 ----
            case ConsoleKey.G when ctrl:
                JumpToLine();
                return true;

            default:
                // 普通字符输入
                if (key.KeyChar >= ' ' && key.KeyChar <= '~' ||
                    key.KeyChar > 127)
                {
                    InsertText(key.KeyChar.ToString());
                    return true;
                }
                return true;
        }
    }

    // ================================================================
    // 编辑操作
    // ================================================================

    private void MoveCursor(int dx, int dy)
    {
        _cx = Math.Clamp(_cx + dx, 0, _lines[_cy].Length);
        _cy = Math.Clamp(_cy + dy, 0, _lines.Count - 1);
        _cx = Math.Min(_cx, _lines[_cy].Length);
    }

    private void InsertText(string text)
    {
        PushUndo();
        _lines[_cy].Insert(_cx, text);
        _cx += text.Length;
        _modified = true;
    }

    private void Backspace()
    {
        if (_cx > 0)
        {
            PushUndo();
            _lines[_cy].Remove(_cx - 1, 1);
            _cx--;
            _modified = true;
        }
        else if (_cy > 0)
        {
            // 合并到上一行
            PushUndo();
            _cx = _lines[_cy - 1].Length;
            _lines[_cy - 1].Append(_lines[_cy]);
            _lines.RemoveAt(_cy);
            _cy--;
            _modified = true;
        }
    }

    private void Delete()
    {
        if (_cx < _lines[_cy].Length)
        {
            PushUndo();
            _lines[_cy].Remove(_cx, 1);
            _modified = true;
        }
        else if (_cy < _lines.Count - 1)
        {
            // 合并下一行
            PushUndo();
            _lines[_cy].Append(_lines[_cy + 1]);
            _lines.RemoveAt(_cy + 1);
            _modified = true;
        }
    }

    private void NewLine()
    {
        PushUndo();
        var rest = _lines[_cy].ToString()[_cx..];
        _lines[_cy].Remove(_cx, _lines[_cy].Length - _cx);
        _lines.Insert(_cy + 1, new StringBuilder(rest));
        _cy++;
        _cx = 0;
        _modified = true;
    }

    private void CutLine()
    {
        _clipboard = _lines[_cy].ToString();
        if (_lines.Count > 1)
        {
            PushUndo();
            _lines.RemoveAt(_cy);
            if (_cy >= _lines.Count) _cy = _lines.Count - 1;
            _cx = 0;
            _modified = true;
        }
        else
        {
            PushUndo();
            _lines[_cy].Clear();
            _cx = 0;
            _modified = true;
        }
    }

    private void DeleteLine()
    {
        CutLine();
    }

    private void JumpToLine()
    {
        var input = UI.TuiPrompt.Ask($"跳转到行 (1-{_lines.Count})");
        if (int.TryParse(input, out var ln) && ln >= 1 && ln <= _lines.Count)
        {
            _cy = ln - 1;
            _cx = 0;
            Render();
        }
    }

    private void PushUndo()
    {
        // 简化撤销：保存当前行快照
        _undo.Push(new EditAction(_cy, _cx,
            _lines[_cy].ToString(), "", _lines.Count));
    }

    private void Undo()
    {
        if (!_undo.TryPop(out var act)) return;
        _lines[act.Line] = new StringBuilder(act.OldLine);
        while (_lines.Count > act.OldCount)
            _lines.RemoveAt(_lines.Count - 1);
        _cy = act.Line;
        _cx = act.Col;
        _modified = true;
    }

    // ================================================================
    // 保存
    // ================================================================

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = string.Join("\n", _lines.Select(sb => sb.ToString()));
            File.WriteAllText(_filePath, content, System.Text.Encoding.UTF8);
            _modified = false;

            // 异步运行 lint 诊断
            _ = Task.Run(async () =>
            {
                await DiagnosticManager.RunLintAsync(_filePath);
                Render();
            });
        }
        catch (Exception ex)
        {
            UI.TuiBox.Error("保存失败", ex.Message);
            Thread.Sleep(1200);
        }
    }

    private void PromptSave()
    {
        if (!_modified) return;
        var choice = UI.TuiList.Select("文件已修改，是否保存？",
            ["💾 保存并退出", "🗑 不保存退出", "↩ 继续编辑"]);
        if (choice == null || choice.StartsWith("↩")) return;
        if (choice.StartsWith("💾")) Save();
    }

    // ================================================================
    // 渲染 — 全屏编辑 + 双行状态栏
    // ================================================================
    //  布局:  顶边 (1) + 编辑区 (th-5) + 分隔 (1) + 状态1 (1) + 状态2 (1) + 底边 (1)
    // ================================================================

    private void Render()
    {
        (_tw, _th) = (Console.WindowWidth, Console.WindowHeight);
        var vh = _th - 5; // 编辑区可视行数

        // 调整滚动
        if (_cy < _scroll) _scroll = _cy;
        if (_cy >= _scroll + vh) _scroll = _cy - vh + 1;
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _lines.Count - vh));

        var sb = new StringBuilder();

        // 获取诊断汇总
        var (totalErrors, totalWarnings) = DiagnosticManager.GetSummary(_filePath);
        var cursorDiagnostics = DiagnosticManager.GetForLine(_filePath, _cy + 1);

        // 隐藏光标 + 清屏
        sb.Append("[?25l[2J[H");

        // ═══════ 顶边 ═══════
        var title = $" /edit: {Path.GetFileName(_filePath)} ";
        if (_modified) title += "[已修改] ";
        var titleVW = VW(title);
        var topDash = Math.Max(0, _tw - titleVW - 2); // -2 for ╭─ and ╮
        sb.Append($"[33m╭─{title}{new string('─', topDash)}╮[0m\r\n");

        // 前缀宽度: │ + 行号(含空格) + │ + 空格
        // 格式: "│   N │ " = 1 + 4 + 1 + 1 + 1 = 8 或 9 (光标行少一个空格)
        const int PrefixVisual = 9; // 光标行的前缀视觉宽度
        var contentMaxVW = _tw - PrefixVisual - 1; // -1 for closing │

        // ═══════ 编辑区 ═══════
        for (int i = 0; i < vh; i++)
        {
            var li = _scroll + i;
            var isCursor = li == _cy;

            // 左边框 + 行号 + gutter 指示器
            if (li < _lines.Count)
            {
                var lineDiags = DiagnosticManager.GetForLine(_filePath, li + 1);
                var hasError = lineDiags.Any(d => d.Severity == Severity.Error);
                var hasWarning = !hasError && lineDiags.Any(d => d.Severity == Severity.Warning);
                var gutter = hasError ? "[31m●[0m" : hasWarning ? "[33m▲[0m" : " ";

                var ln = (li + 1).ToString().PadLeft(4);
                sb.Append("[33m│[0m");
                if (isCursor)
                    sb.Append($"{gutter}[36m{ln} [33m│[0m ");
                else
                    sb.Append($"{gutter}[2m{ln}  [22m│ ");
            }
            else
            {
                sb.Append("[33m│[0m       │ [2m~[0m");
            }

            // 内容 + 截断适配终端宽度 + 诊断背景色
            if (li < _lines.Count)
            {
                var lineDiags = DiagnosticManager.GetForLine(_filePath, li + 1);
                var hasError = lineDiags.Any(d => d.Severity == Severity.Error);
                var hasWarning = !hasError && lineDiags.Any(d => d.Severity == Severity.Warning);
                var bgColor = hasError ? Syntax.ErrorBg : hasWarning ? Syntax.WarningBg : 0;
                RenderLineTruncated(sb, _lines[li].ToString(), contentMaxVW, bgColor);
            }

            // 右填充到终端边界
            sb.Append("[0m[K\r\n");
        }

        // ═══════ 分隔线 ═══════
        var sepW = Math.Max(0, _tw - 2);
        sb.Append($"[33m├{new string('─', sepW)}┤[0m\r\n");

        // ═══════ 状态行 1 ═══════
        var totalChars = _lines.Sum(l => l.Length);
        var totalLines = _lines.Count;
        var fileSize = FormatSize(
            System.Text.Encoding.UTF8.GetByteCount(
                string.Join("\n", _lines.Select(l => l.ToString()))));

        var stat1 = $"  L{_cy + 1}:C{_cx + 1}  │  " +
                    $"行:{totalLines:N0}  字符:{totalChars:N0}  │  " +
                    $"{fileSize}  │  {_syntax.Name}  ·  UTF-8";
        // 追加诊断计数
        if (totalErrors > 0 || totalWarnings > 0)
        {
            stat1 += "  │  ";
            if (totalErrors > 0) stat1 += $"[31m● {totalErrors} errors[0m";
            if (totalErrors > 0 && totalWarnings > 0) stat1 += "  ";
            if (totalWarnings > 0) stat1 += $"[33m▲ {totalWarnings} warnings[0m";
        }
        RenderStatusLine(sb, stat1);

        // ═══════ 状态行 2 ═══════
        var stat2 = $" ^S保存 ^Z撤销 ^G跳行 ^X剪切 ^C复制 ^V粘贴 ^Y删行 Esc退出";
        if (_modified) stat2 += "  [已修改]";
        // 当前行诊断消息
        if (cursorDiagnostics.Count > 0)
        {
            var firstDiag = cursorDiagnostics[0];
            var msg = firstDiag.Message.Length > 60
                ? firstDiag.Message[..57] + "..."
                : firstDiag.Message;
            stat2 += $"  │  [{(firstDiag.Severity == Severity.Error ? "31" : "33")}m{msg}[0m";
        }
        RenderStatusLine(sb, stat2);

        // ═══════ 底边 ═══════
        var botW = Math.Max(0, _tw - 2);
        sb.Append($"[33m╰{new string('─', botW)}╯[0m");

        // 恢复光标到编辑位置 (CJK 宽度感知)
        var screenRow = _cy - _scroll + 1;
        var lineBeforeCursor = _lines[_cy].ToString();
        var cxVisual = _cx > 0 ? VW(lineBeforeCursor[..Math.Min(_cx, lineBeforeCursor.Length)]) : 0;
        var screenCol = PrefixVisual + cxVisual;
        // gutter 增加 1 个视觉字符宽度
        screenCol += 1;
        screenCol = Math.Min(screenCol, _tw - 1);
        sb.Append($"[{screenRow};{screenCol}H");
        sb.Append("[?25h");

        Console.Write(sb.ToString());
    }

    /// <summary>渲染一行状态，右侧自动填充到终端边界</summary>
    private void RenderStatusLine(StringBuilder sb, string text)
    {
        var textVW = VW(text);
        // "│ " + text + spaces + " │"
        // 1 + 1 + textVW + pad + 1 + 1 = textVW + pad + 4 = _tw
        var pad = Math.Max(0, _tw - textVW - 4);
        sb.Append($"[33m│[0m [2m{text}{new string(' ', pad)} [0m[33m│[0m\r\n");
    }

    /// <summary>渲染一行内容，按视觉宽度截断适配终端。bgColor=0 表示无背景。</summary>
    private void RenderLineTruncated(StringBuilder sb, string line, int maxVW, int bgColor = 0)
    {
        if (string.IsNullOrEmpty(line))
        {
            if (bgColor > 0) sb.Append($"[{bgColor}m");
            sb.Append(' ');
            return;
        }

        var hasBg = bgColor > 0;
        var tokens = _syntax.Tokenize(line);
        var vw = 0;
        foreach (var (text, ansiColor) in tokens)
        {
            var textVW = VW(text);
            if (vw + textVW > maxVW)
            {
                var remain = maxVW - vw;
                if (remain > 0)
                {
                    var truncated = TruncateTextByVW(text, remain);
                    if (hasBg)
                        sb.Append($"[{bgColor};{ansiColor}m{truncated}[0m");
                    else
                        sb.Append($"[{ansiColor}m{truncated}[0m");
                }
                break;
            }
            if (hasBg)
                sb.Append($"[{bgColor};{ansiColor}m{text}[0m");
            else
                sb.Append($"[{ansiColor}m{text}[0m");
            vw += textVW;
        }
    }

    /// <summary>按视觉宽度截断纯文本</summary>
    private static string TruncateTextByVW(string text, int maxVW)
    {
        var vw = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
        {
            var w = runes[i].Value > 127 ? 2 : 1;
            if (vw + w > maxVW)
                return text[..(i > 0 ? i : 0)];
            vw += w;
        }
        return text;
    }

    // ================================================================
    // 辅助
    // ================================================================

    private static int VW(string s)
    {
        int w = 0;
        foreach (char c in s) w += c > 127 ? 2 : 1;
        return w;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
