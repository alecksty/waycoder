// =============================================================
// ListBox.cs —— 列表选择框
//
// 显示字符串列表，支持方向键选择、Home/End、滚动、鼠标点击。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>列表控件。</summary>
public class ListBox : Control
{
    public List<string> Items { get; } = new();
    public int Selected { get; private set; }
    public int Scroll { get; private set; }
    public Action? OnSelect { get; set; }

    public Color Fg { get; set; } = Color.White;
    public Color Bg { get; set; } = Color.Black;
    public Color SelBg { get; set; } = Color.Blue;

    public ListBox()
    {
        TabStop = true;
    }

    public void SetItems(IEnumerable<string> items)
    {
        Items.Clear();
        Items.AddRange(items);
        Selected = 0; Scroll = 0;
    }

    public override bool CanFocus => true;

    public override void Draw(Screen screen)
    {
        int row = Row - 1, col = Col - 1;
        for (int r = 0; r < Height; r++) screen.ClearRow(row + r, Bg);
        int count = Math.Min(Height, Items.Count - Scroll);
        for (int i = 0; i < count; i++)
        {
            int idx = Scroll + i;
            bool sel = idx == Selected && Focused;
            var bg = sel ? SelBg : Bg;
            screen.PutText(row + i, col, Cjk.Fit(Items[idx], Width), Fg, bg, sel);
        }
    }

    public override bool OnKey(InputEvent ev)
    {
        if (Items.Count == 0) return false;
        if (ev.IsKey(KeyCode.Up)) { MoveSel(Selected - 1); return true; }
        if (ev.IsKey(KeyCode.Down)) { MoveSel(Selected + 1); return true; }
        if (ev.IsKey(KeyCode.Home)) { MoveSel(0); return true; }
        if (ev.IsKey(KeyCode.End)) { MoveSel(Items.Count - 1); return true; }
        if (ev.IsKey(KeyCode.PgUp)) { MoveSel(Selected - Height); return true; }
        if (ev.IsKey(KeyCode.PgDn)) { MoveSel(Selected + Height); return true; }
        if (ev.IsKey(KeyCode.Enter)) { OnSelect?.Invoke(); return true; }
        return false;
    }

    private void MoveSel(int idx)
    {
        if (idx < 0) idx = 0;
        if (idx >= Items.Count) idx = Items.Count - 1;
        Selected = idx;
        if (Selected < Scroll) Scroll = Selected;
        if (Selected >= Scroll + Height) Scroll = Selected - Height + 1;
        OnSelect?.Invoke();
    }

    public override bool OnClick(int relRow, int relCol)
    {
        int idx = Scroll + relRow;
        if (idx >= 0 && idx < Items.Count)
        {
            Selected = idx;
            OnSelect?.Invoke();
        }
        return true;
    }
}
