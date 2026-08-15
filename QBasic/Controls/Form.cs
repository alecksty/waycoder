// =============================================================
// Form.cs —— 模态对话框窗口
//
// 在屏幕中央绘制一个带边框的窗口，可包含子控件，支持关闭按钮。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>对话框。</summary>
public class Form : Control
{
    public string Title { get; set; } = "";
    public List<Control> Controls { get; } = new();
    public bool Modal { get; set; }
    public Color BorderFg { get; set; } = Color.BrightBlue;
    public Color Bg { get; set; } = Color.Black;
    public Color TitleFg { get; set; } = Color.BrightWhite;

    /// <summary>关闭回调。</summary>
    public Action? OnClose { get; set; }

    /// <summary>在屏幕上自动居中一个对话框。</summary>
    public static Form Center(int w, int h, string title, bool modal = true)
    {
        var (rows, cols) = Terminal.GetSize();
        var f = new Form
        {
            Width = w,
            Height = h,
            Row = (rows - h) / 2 + 1,
            Col = (cols - w) / 2 + 1,
            Title = title,
            Modal = modal,
        };
        return f;
    }

    public void Add(Control c)
    {
        Controls.Add(c);
        c.App = App;
    }

    public override bool CanFocus => true;

    public override void Draw(Screen screen)
    {
        int row = Row - 1, col = Col - 1;
        // 边框
        for (int c = 0; c < Width; c++)
        {
            screen.Put(row, col + c, c == 0 ? '┌' : c == Width - 1 ? '┐' : '─', BorderFg, Bg);
            screen.Put(row + Height - 1, col + c, c == 0 ? '└' : c == Width - 1 ? '┘' : '─', BorderFg, Bg);
        }
        for (int r = 0; r < Height; r++)
        {
            screen.Put(row + r, col, '│', BorderFg, Bg);
            screen.Put(row + r, col + Width - 1, '│', BorderFg, Bg);
        }
        // 内部
        for (int r = 1; r < Height - 1; r++)
            for (int c = 1; c < Width - 1; c++)
                screen.Put(row + r, col + c, ' ', TitleFg, Bg);
        // 标题
        screen.PutText(row, col + 2, Cjk.Fit(Title, Width - 4), TitleFg, Bg, true);
        // 子控件
        foreach (var c in Controls)
        {
            if (!c.Visible) continue;
            var savedRow = c.Row; var savedCol = c.Col; var savedApp = c.App;
            c.Row = Row + c.Row - 1;
            c.Col = Col + c.Col - 1;
            c.App = App;
            c.Draw(screen);
            c.Row = savedRow; c.Col = savedCol; c.App = savedApp;
        }
    }

    public override bool OnKey(InputEvent ev)
    {
        if (ev.IsKey(KeyCode.Escape) && Modal)
        {
            OnClose?.Invoke();
            return true;
        }
        // 子控件
        foreach (var c in Controls)
            if (c.Visible && c.Enabled && c.Focused && c.OnKey(ev)) return true;
        return false;
    }
}
