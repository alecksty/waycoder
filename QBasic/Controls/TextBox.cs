// =============================================================
// TextBox.cs —— 多行文本编辑框
//
// 支持光标移动、插入/删除、滚动、Insert/Overwrite 切换、
// Home/End/PgUp/PgDn、剪贴板剪切复制粘贴。文本以行列表存储。
// =============================================================
using QBasic.Compiler;
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>多行文本框。</summary>
public class TextBox : Control
{
    public List<string> Lines { get; } = new();
    public int CursorLine { get; private set; }
    public int CursorCol { get; private set; }
    public int ScrollRow { get; private set; }   // 顶部可见行
    public int ScrollCol { get; private set; }   // 左侧可见列
    public bool Overwrite { get; set; }

    public Color Fg { get; set; } = Color.White;
    public Color Bg { get; set; } = Color.Black;
    public Color SelBg { get; set; } = Color.BrightBlack;

    // ---- BASIC 语法高亮配色 ----
    public Color KeywordColor { get; set; } = Color.BrightCyan;
    public Color NumberColor { get; set; } = Color.BrightYellow;
    public Color StringColor { get; set; } = Color.Green;
    public Color CommentColor { get; set; } = Color.BrightBlack;
    public bool HighlightSyntax { get; set; } = true;

    public int SelStartLine = -1, SelStartCol = -1;

    public TextBox()
    {
        Lines.Add("");
        TabStop = true;
    }

    public void SetText(string text)
    {
        Lines.Clear();
        if (string.IsNullOrEmpty(text)) Lines.Add("");
        else
        {
            foreach (var ln in text.Split('\n'))
                Lines.Add(ln.TrimEnd('\r'));
        }
        CursorLine = 0; CursorCol = 0; ScrollRow = 0; ScrollCol = 0;
    }

    public string GetText() => string.Join("\n", Lines);

    public void SetCursor(int line, int col)
    {
        if (line < 0) line = 0;
        if (line >= Lines.Count) line = Lines.Count - 1;
        if (col < 0) col = 0;
        if (col > Lines[line].Length) col = Lines[line].Length;
        CursorLine = line; CursorCol = col;
        EnsureCursorVisible();
    }

    public override bool CanFocus => true;

    public override void Draw(Screen screen)
    {
        int row = Row - 1, col = Col - 1;
        for (int r = 0; r < Height; r++)
            screen.ClearRow(row + r, Bg);
        int maxLines = Math.Min(Height, Lines.Count - ScrollRow);
        for (int li = 0; li < maxLines; li++)
        {
            string line = Lines[ScrollRow + li];
            if (HighlightSyntax) RenderLine(screen, row + li, col, line, Width);
            else
            {
                int len = line.Length;
                int vis = Math.Max(0, len - ScrollCol);
                string visible = vis > 0 ? line.Substring(ScrollCol, Math.Min(vis, Width)) : "";
                screen.PutText(row + li, col, Cjk.Fit(visible, Width), Fg, Bg);
            }
        }
    }

    /// <summary>以 BASIC 语法高亮渲染一行（正确处理水平滚动与宽度裁剪）。</summary>
    private void RenderLine(Screen screen, int row, int col, string line, int maxWidth)
    {
        int x = col;
        int budget = maxWidth;
        if (string.IsNullOrEmpty(line))
        {
            screen.PutText(row, x, new string(' ', maxWidth), Fg, Bg);
            return;
        }
        int consumed = 0;
        foreach (var (text, color) in TokenizeForHighlight(line))
        {
            if (consumed + text.Length <= ScrollCol) { consumed += text.Length; continue; }
            int skip = Math.Max(0, ScrollCol - consumed);
            string t = text.Substring(skip);
            consumed += text.Length;
            int w = Cjk.Width(t);
            if (w > budget)
            {
                t = Cjk.Fit(t, budget);
                screen.PutText(row, x, t, color, Bg);
                break;
            }
            screen.PutText(row, x, t, color, Bg);
            x += w; budget -= w;
            if (budget <= 0) break;
        }
        if (budget > 0) screen.PutText(row, x, new string(' ', budget), Fg, Bg);
    }

    /// <summary>把一行 BASIC 源码切成带颜色的片段。</summary>
    internal List<(string Text, Color Color)> TokenizeForHighlight(string line)
    {
        var segs = new List<(string, Color)>();
        int i = 0, n = line.Length;
        while (i < n)
        {
            char c = line[i];
            // 注释：单引号
            if (c == '\'')
            {
                segs.Add((line.Substring(i), CommentColor));
                break;
            }
            // 字符串
            if (c == '"')
            {
                int j = i + 1;
                while (j < n && line[j] != '"') j++;
                if (j < n) j++; // 结束引号
                segs.Add((line.Substring(i, j - i), StringColor));
                i = j;
                continue;
            }
            // 数字
            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(line[i + 1])))
            {
                int j = i;
                while (j < n && (char.IsDigit(line[j]) || line[j] == '.')) j++;
                segs.Add((line.Substring(i, j - i), NumberColor));
                i = j;
                continue;
            }
            // 标识符 / 关键字
            if (char.IsLetter(c) || c == '_')
            {
                int j = i;
                while (j < n && (char.IsLetterOrDigit(line[j]) || line[j] == '_' || line[j] == '$')) j++;
                string word = line.Substring(i, j - i);
                if (word.Equals("REM", StringComparison.OrdinalIgnoreCase))
                {
                    // REM 注释：整行剩余都是注释
                    segs.Add((line.Substring(i), CommentColor));
                    break;
                }
                bool kw = Keywords.Set.Contains(word.ToUpperInvariant());
                segs.Add((line.Substring(i, j - i), kw ? KeywordColor : Fg));
                i = j;
                continue;
            }
            // 空白
            if (c == ' ' || c == '\t')
            {
                int j = i;
                while (j < n && (line[j] == ' ' || line[j] == '\t')) j++;
                segs.Add((line.Substring(i, j - i), Fg));
                i = j;
                continue;
            }
            // 其它单个字符
            segs.Add((c.ToString(), Fg));
            i++;
        }
        return segs;
    }

    /// <summary>确保光标在可视区域。</summary>
    public void EnsureCursorVisible()
    {
        if (CursorLine < ScrollRow) ScrollRow = CursorLine;
        if (CursorLine >= ScrollRow + Height) ScrollRow = CursorLine - Height + 1;
        if (CursorCol < ScrollCol) ScrollCol = CursorCol;
        if (CursorCol >= ScrollCol + Width) ScrollCol = CursorCol - Width + 1;
    }

    public override bool OnKey(InputEvent ev)
    {
        // 光标位置（相对可视区）
        if (ev.IsKey(KeyCode.Up)) { MoveCursor(CursorLine - 1, CursorCol); return true; }
        if (ev.IsKey(KeyCode.Down)) { MoveCursor(CursorLine + 1, CursorCol); return true; }
        if (ev.IsKey(KeyCode.Left)) { MoveCursor(CursorLine, CursorCol - 1); return true; }
        if (ev.IsKey(KeyCode.Right)) { MoveCursor(CursorLine, CursorCol + 1); return true; }
        if (ev.IsKey(KeyCode.Home)) { if (ev.Mods.HasFlag(KeyMods.Ctrl)) { CursorLine = 0; CursorCol = 0; } else CursorCol = 0; EnsureCursorVisible(); return true; }
        if (ev.IsKey(KeyCode.End)) { if (ev.Mods.HasFlag(KeyMods.Ctrl)) { CursorLine = Lines.Count - 1; CursorCol = Lines[CursorLine].Length; } else CursorCol = Lines[CursorLine].Length; EnsureCursorVisible(); return true; }
        if (ev.IsKey(KeyCode.PgUp)) { MoveCursor(CursorLine - Math.Max(1, Height - 1), CursorCol); return true; }
        if (ev.IsKey(KeyCode.PgDn)) { MoveCursor(CursorLine + Math.Max(1, Height - 1), CursorCol); return true; }
        if (ev.IsKey(KeyCode.Backspace)) { Backspace(); return true; }
        if (ev.IsKey(KeyCode.Delete)) { Delete(); return true; }
        if (ev.IsKey(KeyCode.Enter)) { InsertNewline(); return true; }
        if (ev.IsKey(KeyCode.Tab)) { InsertChar('\t'); return true; }
        if (ev.IsKey(KeyCode.Insert)) { Overwrite = !Overwrite; return true; }
        if (ev.Key == KeyCode.None && ev.Mods.HasFlag(KeyMods.Ctrl))
        {
            switch (char.ToLowerInvariant(ev.Ch))
            {
                case 'x': Cut(); return true;
                case 'c': Copy(); return true;
                case 'v': Paste(); return true;
                case 'a': SelectAll(); return true;
            }
        }
        if (ev.Key == KeyCode.None && !ev.Mods.HasFlag(KeyMods.Ctrl) && !ev.Mods.HasFlag(KeyMods.Alt))
        {
            InsertChar(ev.Ch);
            return true;
        }
        if (ev.Key == KeyCode.Paste && ev.Text != null)
        {
            foreach (char ch in ev.Text) InsertChar(ch);
            return true;
        }
        return false;
    }

    public static string? Clipboard { get; set; }

    private void SelectAll()
    {
        SelStartLine = 0; SelStartCol = 0;
        CursorLine = Lines.Count - 1; CursorCol = Lines[CursorLine].Length;
        EnsureCursorVisible();
    }

    private void Copy()
    {
        if (SelStartLine < 0) return;
        int l1 = Math.Min(SelStartLine, CursorLine), l2 = Math.Max(SelStartLine, CursorLine);
        if (l1 == l2)
        {
            int c1 = Math.Min(SelStartCol, CursorCol), c2 = Math.Max(SelStartCol, CursorCol);
            Clipboard = Lines[l1].Substring(c1, c2 - c1);
        }
        else
        {
            var parts = new List<string>();
            parts.Add(Lines[l1].Substring(Math.Min(SelStartCol, CursorCol)));
            for (int i = l1 + 1; i < l2; i++) parts.Add(Lines[i]);
            parts.Add(Lines[l2].Substring(0, Math.Max(SelStartCol, CursorCol)));
            Clipboard = string.Join("\n", parts);
        }
    }

    private void Cut()
    {
        if (SelStartLine < 0) return;
        Copy();
        DeleteSelection();
    }

    private void Paste()
    {
        if (Clipboard == null) return;
        if (SelStartLine >= 0) DeleteSelection();
        foreach (char ch in Clipboard)
        {
            if (ch == '\n') InsertNewline();
            else InsertChar(ch);
        }
    }

    private void DeleteSelection()
    {
        if (SelStartLine < 0) return;
        int l1 = Math.Min(SelStartLine, CursorLine), l2 = Math.Max(SelStartLine, CursorLine);
        int c1, c2;
        bool backward = SelStartLine < CursorLine || (SelStartLine == CursorLine && SelStartCol < CursorCol);
        if (l1 == l2)
        {
            c1 = Math.Min(SelStartCol, CursorCol); c2 = Math.Max(SelStartCol, CursorCol);
            Lines[l1] = Lines[l1].Remove(c1, c2 - c1);
            CursorLine = l1; CursorCol = c1;
        }
        else
        {
            string first = Lines[l1].Substring(0, Math.Min(SelStartCol, CursorCol));
            string last = Lines[l2].Substring(Math.Max(SelStartCol, CursorCol));
            Lines[l1] = first + last;
            Lines.RemoveRange(l1 + 1, l2 - l1);
            CursorLine = l1; CursorCol = first.Length;
        }
        SelStartLine = -1; SelStartCol = -1;
        EnsureCursorVisible();
    }

    private void MoveCursor(int line, int col)
    {
        if (line < 0) return;
        if (line >= Lines.Count) return;
        if (col < 0) col = 0;
        if (col > Lines[line].Length) col = Lines[line].Length;
        CursorLine = line; CursorCol = col;
        if (SelStartLine < 0) { SelStartLine = line; SelStartCol = col; }
        EnsureCursorVisible();
    }

    private void Backspace()
    {
        if (SelStartLine >= 0) { DeleteSelection(); return; }
        if (CursorCol > 0)
        {
            Lines[CursorLine] = Lines[CursorLine].Remove(CursorCol - 1, 1);
            CursorCol--;
        }
        else if (CursorLine > 0)
        {
            string prev = Lines[CursorLine - 1];
            CursorCol = prev.Length;
            Lines[CursorLine - 1] = prev + Lines[CursorLine];
            Lines.RemoveAt(CursorLine);
            CursorLine--;
        }
        EnsureCursorVisible();
    }

    private void Delete()
    {
        if (SelStartLine >= 0) { DeleteSelection(); return; }
        if (CursorCol < Lines[CursorLine].Length)
        {
            Lines[CursorLine] = Lines[CursorLine].Remove(CursorCol, 1);
        }
        else if (CursorLine < Lines.Count - 1)
        {
            Lines[CursorLine] += Lines[CursorLine + 1];
            Lines.RemoveAt(CursorLine + 1);
        }
        EnsureCursorVisible();
    }

    private void InsertNewline()
    {
        if (SelStartLine >= 0) DeleteSelection();
        string line = Lines[CursorLine];
        string head = line.Substring(0, CursorCol);
        string tail = line.Substring(CursorCol);
        Lines[CursorLine] = head;
        Lines.Insert(CursorLine + 1, tail);
        CursorLine++;
        CursorCol = 0;
        EnsureCursorVisible();
    }

    private void InsertChar(char ch)
    {
        if (SelStartLine >= 0) DeleteSelection();
        string line = Lines[CursorLine];
        if (ch == '\t')
        {
            int spaces = 4 - (CursorCol % 4);
            line = line.Insert(CursorCol, new string(' ', spaces));
            CursorCol += spaces;
        }
        else if (Overwrite && CursorCol < line.Length)
        {
            line = line.Remove(CursorCol, 1).Insert(CursorCol, ch.ToString());
            CursorCol++;
        }
        else
        {
            line = line.Insert(CursorCol, ch.ToString());
            CursorCol++;
        }
        Lines[CursorLine] = line;
        EnsureCursorVisible();
    }
}
