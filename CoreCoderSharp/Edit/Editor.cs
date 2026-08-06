using System.Text;
using CoreCoderSharp.Tools;

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

        try
        {
            Render();
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(30);
                    // 检测终端尺寸变化
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
            Console.Clear();
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

            // 通知模块
            UI.TuiBox.Success("已保存", _filePath);
            Thread.Sleep(800);
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
    // 渲染
    // ================================================================

    private void Render()
    {
        (_tw, _th) = (Console.WindowWidth, Console.WindowHeight);
        var vh = _th - 3; // 可视行数: 顶部边框 + 底部边框 + 状态栏

        // 调整滚动
        if (_cy < _scroll) _scroll = _cy;
        if (_cy >= _scroll + vh) _scroll = _cy - vh + 1;
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _lines.Count - vh));

        var sb = new System.Text.StringBuilder();
        // 隐藏光标
        sb.Append("[?25l");

        // 清屏 + 回左上角
        sb.Append("[2J[H");

        // ---- 顶边 ----
        var title = $" /edit: {Path.GetFileName(_filePath)} ";
        if (_modified) title += "[已修改] ";
        var titleW = VW(title);
        var dashR = Math.Max(0, _tw - titleW - 2);
        sb.Append($"[33m╭─{title}{new string('─', dashR)}╮[0m\r\n");

        // ---- 内容区 ----
        for (int i = 0; i < vh; i++)
        {
            var li = _scroll + i;
            sb.Append("[33m│[0m");

            if (li < _lines.Count)
            {
                // 行号
                var ln = (li + 1).ToString().PadLeft(4);
                var isCursorLine = li == _cy;
                sb.Append(isCursorLine
                    ? $" [36m{ln} [33m│[0m "
                    : $" [2m{ln}  [22m│ ");

                // 内容（语法高亮）
                var line = _lines[li].ToString();
                RenderLine(sb, line, isCursorLine);
            }
            else
            {
                sb.Append("      │ ");
            }

            // 填充到右边界
            sb.Append("[0m[K\r\n");
        }

        // ---- 底边 + 状态栏 ----
        var status = $" {Path.GetFileName(_filePath)}  L{_cy + 1}:C{_cx + 1}  {_syntax.Name}  " +
                     $"{( _modified ? "[已修改]" : "")}  Ctrl+S 保存  Ctrl+Q/Esc 退出  Ctrl+G 跳转";
        var sw = VW(status);
        var padR = Math.Max(0, _tw - sw - 2);
        sb.Append($"[33m╰{status}{new string('─', padR)}╯[0m");

        // 恢复光标到编辑位置
        var screenRow = _cy - _scroll + 1; // +1 因为顶边占一行
        var screenCol = 8 + _cx;           // 边框 + 行号 + 分隔
        sb.Append($"[{screenRow};{screenCol}H");
        sb.Append("[?25h"); // 显示光标

        Console.Write(sb.ToString());
    }

    private void RenderLine(System.Text.StringBuilder sb, string line, bool isCursor)
    {
        if (string.IsNullOrEmpty(line))
        {
            sb.Append(' ');
            return;
        }

        // 使用语法高亮扫描行
        var tokens = _syntax.Tokenize(line);
        foreach (var (text, color) in tokens)
        {
            var escaped = text
                .Replace("", "")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            sb.Append($"[{color}m{escaped}[0m");
        }
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
}
