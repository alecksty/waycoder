// =============================================================
// Menu.cs —— 菜单栏 / 下拉菜单 / 菜单项
//
// 顶部菜单栏（MenuBar）包含若干菜单（Menu），每个菜单含菜单项
// （MenuItem）。支持 Alt+快捷键打开菜单、方向键导航、Enter 触发。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>菜单项。</summary>
public class MenuItem
{
    public string Text { get; set; }
    public char? Shortcut { get; set; }
    public Action? Action { get; set; }
    public bool Enabled { get; set; } = true;

    public MenuItem(string text, Action? action = null, char? shortcut = null)
    {
        Text = text;
        Action = action;
        Shortcut = shortcut;
    }
}

/// <summary>下拉菜单。</summary>
public class Menu
{
    public string Title { get; set; }
    public char Shortcut { get; set; }
    public List<MenuItem> Items { get; } = new();

    public Menu(string title, char shortcut)
    {
        Title = title;
        Shortcut = shortcut;
    }
}

/// <summary>菜单栏（屏幕顶部一行）。</summary>
public class MenuBar
{
    public List<Menu> Menus { get; } = new();
    public bool Open { get; set; }
    public int OpenIndex { get; set; } = -1;
    public int ItemIndex { get; set; }
    public Action<string>? OnSelect;

    public Color Fg { get; set; } = Color.BrightWhite;
    public Color Bg { get; set; } = Color.Blue;
    public Color SelBg { get; set; } = Color.BrightBlue;

    public void Add(Menu m) => Menus.Add(m);

    public void Draw(Screen screen, int row)
    {
        for (int c = 0; c < screen.Cols; c++) screen.Put(row - 1, c, ' ', Fg, Bg);
        int x = 1;
        for (int i = 0; i < Menus.Count; i++)
        {
            bool active = Open && OpenIndex == i;
            var bg = active ? Color.BrightBlack : Bg;
            var fg = active ? Color.BrightWhite : Fg;
            screen.PutText(row - 1, x, " " + Menus[i].Title + " ", fg, bg);
            x += Cjk.Width(Menus[i].Title) + 2;
        }
        if (Open && OpenIndex >= 0)
            DrawDropDown(screen, row);
    }

    private void DrawDropDown(Screen screen, int barRow)
    {
        var menu = Menus[OpenIndex];
        int top = barRow + 1;
        int left = 1;
        for (int i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];
            int row = top + i;
            bool sel = i == ItemIndex;
            var bg = sel ? Color.BrightBlue : Color.Blue;
            var fg = sel ? Color.White : Color.BrightWhite;
            if (!item.Enabled) fg = Color.BrightBlack;
            screen.PutText(row, left, " ", fg, bg);
            screen.PutText(row, left + 1, Cjk.Fit(item.Text, 20), fg, bg, sel);
            screen.PutText(row, left + 21, " ", fg, bg);
        }
    }

    /// <summary>处理键盘事件；返回是否消费。</summary>
    public bool OnKey(InputEvent ev, out string? selected)
    {
        selected = null;
        // Alt+字符 打开对应菜单
        if (!Open && ev.Mods.HasFlag(KeyMods.Alt) && ev.Key == KeyCode.None)
        {
            char c = char.ToLowerInvariant(ev.Ch);
            for (int i = 0; i < Menus.Count; i++)
                if (char.ToLowerInvariant(Menus[i].Shortcut) == c)
                {
                    Open = true; OpenIndex = i; ItemIndex = 0;
                    return true;
                }
        }
        if (Open)
        {
            var menu = Menus[OpenIndex];
            if (ev.IsKey(KeyCode.Escape)) { Open = false; return true; }
            if (ev.IsKey(KeyCode.Left)) { OpenIndex = (OpenIndex - 1 + Menus.Count) % Menus.Count; ItemIndex = 0; return true; }
            if (ev.IsKey(KeyCode.Right)) { OpenIndex = (OpenIndex + 1) % Menus.Count; ItemIndex = 0; return true; }
            if (ev.IsKey(KeyCode.Up)) { ItemIndex = (ItemIndex - 1 + menu.Items.Count) % menu.Items.Count; return true; }
            if (ev.IsKey(KeyCode.Down)) { ItemIndex = (ItemIndex + 1) % menu.Items.Count; return true; }
            if (ev.IsKey(KeyCode.Enter) || ev.IsKey(KeyCode.Space))
            {
                var item = menu.Items[ItemIndex];
                if (item.Enabled)
                {
                    Open = false;
                    selected = item.Text;
                    return true;
                }
                return true;
            }
        }
        return false;
    }
}
