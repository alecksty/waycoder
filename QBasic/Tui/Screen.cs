// QBasic/Tui/Screen.cs
// 全屏缓冲渲染：Cell 双缓冲 + 差分重绘（仅重绘变更的单元格）。
namespace QBasic.Tui;

public struct Cell
{
    public char Ch;
    public byte Fg;
    public byte Bg;
    public bool Bold;

    public Cell(char ch, byte fg, byte bg, bool bold)
    {
        Ch = ch;
        Fg = fg;
        Bg = bg;
        Bold = bold;
    }

    public static Cell Empty => new(' ', 7, 0, false);
}

public sealed class Screen
{
    private Cell[] _front;
    private Cell[] _back;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Screen(int width, int height)
    {
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        if (width <= 0) width = 1;
        if (height <= 0) height = 1;
        Width = width;
        Height = height;
        _front = new Cell[width * height];
        _back = new Cell[width * height];
        for (int i = 0; i < _front.Length; i++)
        {
            _front[i] = Cell.Empty;
            _back[i] = Cell.Empty;
        }
    }

    public void Clear(byte fg = 7, byte bg = 0)
    {
        for (int i = 0; i < _back.Length; i++)
            _back[i] = new Cell(' ', fg, bg, false);
    }

    private int Index(int x, int y) => y * Width + x;

    public void Put(int x, int y, char ch, byte fg = 7, byte bg = 0, bool bold = false)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _back[Index(x, y)] = new Cell(ch, fg, bg, bold);
    }

    public void PutString(int x, int y, string text, byte fg = 7, byte bg = 0, bool bold = false)
    {
        int col = x;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\n') continue;
            int w = char.IsSurrogatePair(text, i)
                ? (TextWidth.IsWide(char.ConvertToUtf32(text, i)) ? 2 : 1)
                : (TextWidth.IsWide(ch) ? 2 : 1);
            if (col >= 0 && col < Width) Put(col, y, ch, fg, bg, bold);
            if (w == 2 && col + 1 >= 0 && col + 1 < Width)
                Put(col + 1, y, '\0', fg, bg, bold); // 宽字符占位
            col += w;
            if (char.IsSurrogatePair(text, i)) i++;
        }
    }

    public Cell GetFront(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return Cell.Empty;
        return _front[Index(x, y)];
    }

    // 差分重绘：将 back 与 front 比较，仅输出变化的单元格，然后交换。
    public void Render()
    {
        int lastBg = -1, lastFg = -1;
        bool lastBold = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int i = Index(x, y);
                Cell b = _back[i];
                Cell f = _front[i];
                if (b.Ch == f.Ch && b.Fg == f.Fg && b.Bg == f.Bg && b.Bold == f.Bold)
                    continue;

                // 跳过宽字符的占位符
                if (b.Ch == '\0')
                {
                    _front[i] = b;
                    continue;
                }

                if (b.Fg != lastFg || b.Bg != lastBg || b.Bold != lastBold)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("\x1b[0");
                    if (b.Bold) sb.Append(";1");
                    sb.Append($";38;5;{b.Fg}m");
                    if (b.Bg != 0) sb.Append($";48;5;{b.Bg}m");
                    Terminal.Write(sb.ToString());
                    lastFg = b.Fg;
                    lastBg = b.Bg;
                    lastBold = b.Bold;
                }
                Terminal.Write(Ansi.MoveTo(y + 1, x + 1));
                Terminal.Write(b.Ch.ToString());
                _front[i] = b;
            }
        }
        Terminal.Write("\x1b[0m");
        Terminal.Flush();
    }

    // 强制全量重绘（下次 Render 全部输出）。
    public void InvalidateAll()
    {
        for (int i = 0; i < _front.Length; i++)
            _front[i] = default;
    }
}
