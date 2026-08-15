// =============================================================
// Button.cs —— 命令按钮
//
// 支持键盘 Enter/Space 触发、快捷键下划线，以及鼠标点击。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>按钮。</summary>
public class Button : Control
{
    public string Text { get; set; }
    /// <summary>点击回调。</summary>
    public Action? OnPressed { get; set; }
    /// <summary>唯一快捷键字符（Alt+字符 触发）。</summary>
    public char? Shortcut { get; set; }

    public Color Fg { get; set; } = Color.BrightWhite;
    public Color Bg { get; set; } = Color.Blue;
    public Color FocusBg { get; set; } = Color.BrightBlue;

    public Button(string text, Action? onPressed = null)
    {
        Text = text;
        OnPressed = onPressed;
        Height = 1;
        Width = Cjk.Width(text) + 2;
        TabStop = true;
    }

    public override void Draw(Screen screen)
    {
        var bg = Focused ? FocusBg : Bg;
        int row = Row - 1, col = Col - 1;
        for (int c = 0; c < Width; c++) screen.Put(row, col + c, ' ', Fg, bg);
        screen.Put(row, col, ' ', Fg, bg);
        screen.Put(row, col + 1, '[', Fg, bg);
        int tx = col + 2;
        int printed = screen.PutText(row, tx, Text, Fg, bg, Focused);
        tx += printed;
        screen.Put(row, tx, ']', Fg, bg);
    }

    public override bool OnKey(InputEvent ev)
    {
        if (ev.IsKey(KeyCode.Enter) || (ev.Key == KeyCode.None && ev.Ch == ' '))
        {
            OnPressed?.Invoke();
            return true;
        }
        if (Shortcut.HasValue && ev.Mods.HasFlag(KeyMods.Alt) && char.ToLowerInvariant(ev.Ch) == char.ToLowerInvariant(Shortcut.Value))
        {
            OnPressed?.Invoke();
            return true;
        }
        return false;
    }

    public override bool OnClick(int relRow, int relCol)
    {
        OnPressed?.Invoke();
        return true;
    }
}
