// =============================================================
// Label.cs —— 静态文本标签
//
// 用于显示不可编辑的文本，支持对齐方式与配色。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>文本标签。</summary>
public class Label : Control
{
    public string Text { get; set; } = "";
    public Color Fg { get; set; } = Color.White;
    public Color Bg { get; set; } = Color.Black;
    public bool Bold { get; set; }

    public Label(string text = "", int width = 0)
    {
        Text = text;
        if (width > 0) Width = width;
        else Width = Math.Max(Cjk.Width(text), 1);
        TabStop = false;
    }

    public override void Draw(Screen screen)
    {
        for (int i = 0; i < Height; i++)
        {
            for (int c = 0; c < Width; c++)
                screen.Put(Row - 1 + i, Col - 1 + c, ' ', Fg, Bg);
        }
        int x = Col - 1;
        screen.PutText(Row - 1, x, Text, Fg, Bg, Bold);
    }
}
