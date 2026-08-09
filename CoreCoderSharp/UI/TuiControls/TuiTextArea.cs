using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 多行文本编辑控件 —— 支持光标自由移动、自动换行、滚动。
/// 可嵌入任何 View 容器中。
/// </summary>
public class TuiTextArea : TuiControl
{
    // ── 文本缓冲 ──

    /// <summary>文本行列表（每行不含换行符）</summary>
    public List<string> Lines { get; set; } = [""];

    /// <summary>获取/设置全部文本</summary>
    public string Text
    {
        get => string.Join("\n", Lines);
        set => Lines = string.IsNullOrEmpty(value)
            ? [""]
            : [.. value.Replace("\r\n", "\n").Split('\n')];
    }

    // ── 光标 ──

    /// <summary>光标行（0-based，相对于 Lines）</summary>
    public int CursorRow { get; set; }

    /// <summary>光标列（0-based，相对于当前行文本）</summary>
    public int CursorCol { get; set; }

    // ── 滚动 ──

    /// <summary>垂直滚动偏移（行）</summary>
    public int ScrollRow { get; set; }

    /// <summary>水平滚动偏移（字符列）</summary>
    public int ScrollCol { get; set; }

    // ── 显示选项 ──

    /// <summary>是否显示行号</summary>
    public bool ShowLineNumbers { get; set; }

    /// <summary>是否只读</summary>
    public bool ReadOnly { get; set; }

    /// <summary>占位文本（内容为空时显示）</summary>
    public string Placeholder { get; set; } = "";

    // ── 事件 ──

    /// <summary>文本变化时触发</summary>
    public Action? OnTextChanged { get; set; }

    /// <summary>Ctrl+Enter 提交时触发</summary>
    public Action<string>? OnSubmit { get; set; }

    // ── 样式 ──

    /// <summary>行号前景色</summary>
    public int LineNumFg { get; set; } = 90; // 暗灰
    /// <summary>光标行背景色</summary>
    public int CursorLineBg { get; set; } = 7; // 浅灰
    /// <summary>占位文本前景色</summary>
    public int PlaceholderFg { get; set; } = 90;

    public TuiTextArea()
    {
        Height = 5;
        Width = 60;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visRows = Height;
        if (visRows <= 0) return;

        // 确保光标可见
        EnsureCursorVisible(visRows);

        int lineNumW = ShowLineNumbers ? (Lines.Count > 0 ? Lines.Count.ToString().Length + 1 : 3) : 0;
        int textW = Width - lineNumW;

        // 渲染每一行
        for (int i = 0; i < visRows; i++)
        {
            int lineIdx = ScrollRow + i;
            int screenRow = absY + i;

            bool isCursorLine = lineIdx == CursorRow;

            // 行号
            if (ShowLineNumbers && lineIdx < Lines.Count)
            {
                var numStr = (lineIdx + 1).ToString().PadLeft(lineNumW - 1) + " ";
                var rb = new RenderBuffer();
                rb.Write(screenRow, absX, numStr, fg: LineNumFg);
                sb.Append(rb.ToString());
            }

            // 文本内容
            if (lineIdx < Lines.Count)
            {
                var line = Lines[lineIdx];
                int displayStart = ScrollCol;
                var display = line.Length > displayStart ? line[displayStart..] : "";
                if (display.Length > textW)
                    display = display[..textW];
                display = display.PadRight(textW - (display.Length > textW ? 0 : Math.Min(display.Length, textW) - TuiHelper.DisplayWidth(display)) + TuiHelper.DisplayWidth(display) - display.Length);
                // Pad to fill width
                var pad = Math.Max(0, textW - TuiHelper.DisplayWidth(display));
                display += new string(' ', pad);

                int fg = Focused ? (Fg > 0 ? Fg : 37) : (Fg > 0 ? Fg : 37);
                int bg = isCursorLine ? CursorLineBg : (Bg > 0 ? Bg : 0);

                var rb2 = new RenderBuffer();
                rb2.Write(screenRow, absX + lineNumW, display, fg: fg, bg: bg);
                sb.Append(rb2.ToString());
            }
            else if (lineIdx == Lines.Count && string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
            {
                // 占位文本
                var ph = Placeholder;
                if (TuiHelper.DisplayWidth(ph) > textW)
                    ph = TuiHelper.TruncateByWidth(ph, textW);
                var rb = new RenderBuffer();
                rb.Write(screenRow, absX + lineNumW, ph, fg: PlaceholderFg);
                sb.Append(rb.ToString());
            }

            // 光标指示：记录位置，由 Screen 在最后统一输出
            if (IsCursorOwner && isCursorLine && CursorCol >= ScrollCol)
            {
                var line = SafeLine(lineIdx);
                var preCursorText = line.Length > ScrollCol
                    ? line[ScrollCol..Math.Min(CursorCol, line.Length)]
                    : "";
                int cursorVisualOffset = TuiHelper.DisplayWidth(preCursorText);
                int cursorScreenCol = absX + lineNumW + cursorVisualOffset;
                if (cursorScreenCol < absX + Width && cursorScreenCol >= absX + lineNumW)
                {
                    _cursorRow = screenRow;
                    _cursorCol = cursorScreenCol;
                    _showCursor = true;
                }
            }
            else if (IsCursorOwner && !isCursorLine)
            {
                _showCursor = false;
            }
        }
    }

    // ── 输入处理 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (!Focused || ReadOnly) return false;

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            return HandleCtrlKey(key);

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                MoveCursorCol(-1);
                return true;
            case ConsoleKey.RightArrow:
                MoveCursorCol(1);
                return true;
            case ConsoleKey.UpArrow:
                MoveCursorRow(-1);
                return true;
            case ConsoleKey.DownArrow:
                MoveCursorRow(1);
                return true;
            case ConsoleKey.Home:
                CursorCol = 0;
                return true;
            case ConsoleKey.End:
                CursorCol = SafeLine(CursorRow).Length;
                return true;
            case ConsoleKey.PageUp:
                ScrollRow = Math.Max(0, ScrollRow - Height);
                CursorRow = Math.Max(0, CursorRow - Height);
                return true;
            case ConsoleKey.PageDown:
                ScrollRow = Math.Min(Math.Max(0, Lines.Count - 1), ScrollRow + Height);
                CursorRow = Math.Min(Lines.Count - 1, CursorRow + Height);
                return true;
            case ConsoleKey.Backspace:
                DeleteBefore();
                return true;
            case ConsoleKey.Delete:
                DeleteAfter();
                return true;
            case ConsoleKey.Enter:
                InsertNewline();
                return true;
            default:
                if (key.KeyChar >= ' ')
                {
                    InsertChar(key.KeyChar);
                    return true;
                }
                return false;
        }
    }

    private bool HandleCtrlKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.A: CursorCol = 0; return true;          // 行首
            case ConsoleKey.E: CursorCol = SafeLine(CursorRow).Length; return true; // 行尾
            case ConsoleKey.K: // 删至行尾
                var line = SafeLine(CursorRow);
                Lines[CursorRow] = line[..Math.Min(CursorCol, line.Length)];
                NotifyChange();
                return true;
            case ConsoleKey.Enter:
                OnSubmit?.Invoke(Text);
                return true;
            case ConsoleKey.Backspace: // Ctrl+Backspace: 删一个词
                DeleteWordBefore();
                return true;
            // Ctrl+V 粘贴暂不支持同步剪贴板读取
        }
        return false;
    }

    // ── 编辑操作 ──

    private void InsertChar(char ch)
    {
        var line = SafeLine(CursorRow);
        int pos = Math.Min(CursorCol, line.Length);
        Lines[CursorRow] = line[..pos] + ch + line[pos..];
        CursorCol++;
        NotifyChange();
    }

    /// <summary>插入多字符文本（粘贴等）</summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\n')
                InsertNewline();
            else if (rune.Value >= 32 || rune.Value == '\t')
                InsertChar((char)rune.Value);
        }
        MarkDirty();
    }

    /// <summary>通知屏幕需要重绘（用于外部操作后）</summary>
    public void MarkDirty() { /* 通过 Parent 链触发，或由调用方调用 screen.MarkDirty() */ }

    private void DeleteBefore()
    {
        if (CursorCol > 0)
        {
            var line = SafeLine(CursorRow);
            if (CursorCol <= line.Length)
                Lines[CursorRow] = line[..(CursorCol - 1)] + line[CursorCol..];
            CursorCol--;
            NotifyChange();
        }
        else if (CursorRow > 0)
        {
            // 合并到上一行
            var prev = SafeLine(CursorRow - 1);
            var cur = SafeLine(CursorRow);
            CursorCol = prev.Length;
            Lines[CursorRow - 1] = prev + cur;
            Lines.RemoveAt(CursorRow);
            CursorRow--;
            NotifyChange();
        }
    }

    private void DeleteAfter()
    {
        var line = SafeLine(CursorRow);
        if (CursorCol < line.Length)
        {
            Lines[CursorRow] = line[..CursorCol] + line[(CursorCol + 1)..];
            NotifyChange();
        }
        else if (CursorRow < Lines.Count - 1)
        {
            // 合并下一行
            var next = SafeLine(CursorRow + 1);
            Lines[CursorRow] = line + next;
            Lines.RemoveAt(CursorRow + 1);
            NotifyChange();
        }
    }

    private void InsertNewline()
    {
        var line = SafeLine(CursorRow);
        var indent = GetIndent(line);
        Lines[CursorRow] = line[..Math.Min(CursorCol, line.Length)];
        Lines.Insert(CursorRow + 1, indent + line[Math.Min(CursorCol, line.Length)..]);
        CursorRow++;
        CursorCol = indent.Length;
        NotifyChange();
    }

    private void DeleteWordBefore()
    {
        var line = SafeLine(CursorRow);
        int pos = CursorCol;
        // 跳过空格
        while (pos > 0 && line[pos - 1] == ' ') pos--;
        // 跳过单词字符
        while (pos > 0 && line[pos - 1] != ' ') pos--;
        Lines[CursorRow] = line[..pos] + line[CursorCol..];
        CursorCol = pos;
        NotifyChange();
    }

    private static string GetIndent(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return line[..i];
    }

    // ── 光标移动 ──

    private void MoveCursorCol(int delta)
    {
        CursorCol = Math.Clamp(CursorCol + delta, 0, SafeLine(CursorRow).Length);
    }

    private void MoveCursorRow(int delta)
    {
        CursorRow = Math.Clamp(CursorRow + delta, 0, Lines.Count - 1);
        CursorCol = Math.Min(CursorCol, SafeLine(CursorRow).Length);
    }

    private void EnsureCursorVisible(int visRows)
    {
        if (CursorRow < ScrollRow) ScrollRow = CursorRow;
        if (CursorRow >= ScrollRow + visRows) ScrollRow = CursorRow - visRows + 1;
        ScrollRow = Math.Clamp(ScrollRow, 0, Math.Max(0, Lines.Count - visRows));
    }

    // ── 工具 ──

    private string SafeLine(int idx)
    {
        if (idx < 0 || idx >= Lines.Count) return "";
        return Lines[idx];
    }

    private void NotifyChange()
    {
        OnTextChanged?.Invoke();
    }

}
