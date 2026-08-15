// =============================================================
// Screen.cs —— 全屏双缓冲 + 差分重绘
//
// Screen 维护一个二维单元格缓冲区，每次渲染先把整个画面画到
// 后缓冲，然后与前缓冲逐格比较，只把发生变化的单元格输出为
// 绝对定位 + 文本。这样大幅减少终端写入量，避免闪烁。
//
// Cell 记录字符、前景色、背景色、粗体等属性，用于精确重绘。
// =============================================================
using System.Text;

namespace QBasic.Tui;

/// <summary>单个屏幕单元格的属性。</summary>
public struct Cell
{
    public char Char;
    public Color Fg;
    public Color Bg;
    public bool Bold;
    public bool Reverse;
    public bool IsWideChar; // 此格是否为全角字符的"宽"表示（占2列）
    public bool Skip;       // 跳过（宽字符的第二列占位）

    public bool SameAs(in Cell other) =>
        Char == other.Char && Fg == other.Fg && Bg == other.Bg
        && Bold == other.Bold && Reverse == other.Reverse && Skip == other.Skip;
}

/// <summary>全屏双缓冲渲染器。</summary>
public sealed class Screen
{
    private Cell[,] _back;
    private Cell[,] _front;
    private bool _first = true;
    public int Rows { get; private set; }
    public int Cols { get; private set; }

    public Screen(int rows, int cols)
    {
        Rows = rows; Cols = cols;
        _back = new Cell[rows, cols];
        _front = new Cell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                _front[r, c].Char = '\0';
                _back[r, c].Char = ' ';
                _back[r, c].Bg = Color.Black;
                _back[r, c].Fg = Color.White;
                _front[r, c].Char = '\u0001'; // 保证首帧全量重绘
            }
    }

    public void Resize(int rows, int cols)
    {
        if (rows <= 0) rows = 1;
        if (cols <= 0) cols = 1;
        if (rows == Rows && cols == Cols) return;
        var nb = new Cell[rows, cols];
        var nf = new Cell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                nf[r, c].Char = '\u0001';
                nb[r, c].Char = ' ';
            }
        int minR = Math.Min(rows, Rows), minC = Math.Min(cols, Cols);
        for (int r = 0; r < minR; r++)
            for (int c = 0; c < minC; c++)
                nb[r, c] = _back[r, c];
        _back = nb; _front = nf;
        Rows = rows; Cols = cols;
        _first = true;
    }

    /// <summary>读取后缓冲的单元格引用，供绘制。</summary>
    public ref Cell Get(int row, int col) => ref _back[row, col];

    /// <summary>在 (row,col) 写一个字符。</summary>
    public void Put(int row, int col, char ch, Color fg, Color bg, bool bold = false, bool reverse = false)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;
        ref Cell c = ref _back[row, col];
        c.Char = ch; c.Fg = fg; c.Bg = bg; c.Bold = bold; c.Reverse = reverse;
        c.Skip = false; c.IsWideChar = false;
    }

    /// <summary>写入字符串（自动处理 CJK 宽度），返回消耗的列数。</summary>
    public int PutText(int row, int col, string text, Color fg, Color bg, bool bold = false, bool reverse = false)
    {
        int x = col;
        foreach (char ch in text)
        {
            if (row < 0 || row >= Rows) break;
            if (x < 0) { x++; continue; }
            if (x >= Cols) break;
            if (Cjk.IsWide(ch))
            {
                Put(row, x, ch, fg, bg, bold, reverse);
                x++;
                if (x < Cols)
                {
                    ref Cell c = ref _back[row, x];
                    c.Char = '\0'; c.Skip = true; c.Fg = fg; c.Bg = bg;
                }
                x++;
            }
            else
            {
                Put(row, x, ch, fg, bg, bold, reverse);
                x++;
            }
        }
        return x - col;
    }

    /// <summary>清除一行到背景色。</summary>
    public void ClearRow(int row, Color bg)
    {
        if (row < 0 || row >= Rows) return;
        for (int c = 0; c < Cols; c++)
        {
            ref Cell cell = ref _back[row, c];
            cell.Char = ' '; cell.Fg = Color.White; cell.Bg = bg;
            cell.Bold = false; cell.Reverse = false; cell.Skip = false; cell.IsWideChar = false;
        }
    }

    /// <summary>清除整个屏幕到背景色。</summary>
    public void Clear(Color bg)
    {
        for (int r = 0; r < Rows; r++) ClearRow(r, bg);
    }

    /// <summary>填充一个矩形区域。</summary>
    public void FillRect(int row, int col, int h, int w, char ch, Color fg, Color bg)
    {
        for (int r = row; r < row + h && r < Rows; r++)
            for (int c = col; c < col + w && c < Cols; c++)
            {
                ref Cell cell = ref _back[r, c];
                cell.Char = ch; cell.Fg = fg; cell.Bg = bg; cell.Skip = false;
            }
    }

    /// <summary>把前缓冲全部标记为脏，强制下一帧全量重绘。</summary>
    private void ForceFullRedraw()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _front[r, c].Char = '\u0001';
    }

    /// <summary>双缓冲差分重绘，返回要写入的 ANSI 字符串。</summary>
    public string Flush()
    {
        if (_first) { ForceFullRedraw(); _first = false; }
        var sb = new StringBuilder(4096);
        int lastRow = -1, lastCol = -1;   // 上一个输出位置（光标已停于此）
        for (int r = 0; r < Rows; r++)
        {
            int c = 0;
            while (c < Cols)
            {
                ref Cell b = ref _back[r, c];
                ref Cell f = ref _front[r, c];
                if (b.SameAs(f)) { c++; continue; }
                // 需要重绘，从 c 开始找一段连续需要重绘的格子
                // 仅当光标不在目标位置时才发出定位序列（减少写入量）
                if (lastRow != r || lastCol != c)
                {
                    sb.Append(Ansi.CursorTo(r + 1, c + 1));
                    lastRow = r; lastCol = c;
                }
                int endCol = c;
                for (int k = c; k < Cols; k++)
                {
                    ref Cell bk = ref _back[r, k];
                    ref Cell fk = ref _front[r, k];
                    if (bk.SameAs(fk)) break;
                    // 输出这一格
                    if (bk.Char == '\0' && bk.Skip)
                    {
                        // 宽字符第二列：只输出背景色占位
                        sb.Append(Ansi.Fg(bk.Fg)).Append(Ansi.Bg(bk.Bg)).Append(' ');
                        _front[r, k] = bk;
                        endCol = k + 1;
                        continue;
                    }
                    if (bk.Char == '\0') bk.Char = ' ';
                    sb.Append(Ansi.Fg(bk.Fg)).Append(Ansi.Bg(bk.Bg));
                    if (bk.Bold) sb.Append(Ansi.Bold);
                    if (bk.Reverse) sb.Append(Ansi.Reverse);
                    sb.Append(bk.Char).Append(Ansi.Reset);
                    _front[r, k] = bk;
                    endCol = k + 1;
                }
                lastCol = endCol; // 光标停在末格之后，供下一条定位序列复用
                c = Cols;
            }
        }
        return sb.ToString();
    }
}
