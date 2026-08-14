using System.Text;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 按钮组 —— 对标 Crush button.go ButtonGroup。
/// 管理一组 TuiButton，支持水平/垂直布局、Tab 切换焦点、方向键导航、字母快捷键。
/// </summary>
public class TuiButtonGroup : TuiControl
{
    /// <summary>布局方向</summary>
    public enum LayoutMode { Horizontal, Vertical }

    /// <summary>布局方向</summary>
    public LayoutMode Direction { get; set; } = LayoutMode.Horizontal;

    /// <summary>按钮间距（水平：字符，垂直：行）</summary>
    public int Gap { get; set; } = 2;

    /// <summary>按钮列表</summary>
    public List<TuiButton> Buttons { get; } = [];

    /// <summary>当前焦点按钮索引（-1 = 无）</summary>
    public int ActiveIndex { get; private set; } = -1;

    /// <summary>激活回调（被激活的按钮索引）</summary>
    public Action<int>? OnButtonActivated { get; set; }

    /// <summary>允许循环导航</summary>
    public bool Wrap { get; set; } = true;

    public TuiButtonGroup()
    {
        Height = 1;
    }

    public override bool CanFocus => true;

    // ── 按钮管理 ──

    /// <summary>添加按钮</summary>
    public TuiButton Add(string text, int underlineIndex = -1, Action<TuiButton>? onClick = null)
    {
        var btn = new TuiButton(text, underlineIndex, onClick) { Parent = Parent };
        Buttons.Add(btn);
        if (Buttons.Count == 1) SetActive(0);
        RecalcLayout();
        return btn;
    }

    /// <summary>批量添加（自动检测大写字母为快捷键）</summary>
    public void AddRange(params string[] labels)
    {
        foreach (var label in labels)
        {
            int ul = -1;
            for (int i = 0; i < label.Length; i++)
            {
                if (char.IsUpper(label[i]) && label[i] != ' ')
                { ul = i; break; }
            }
            Add(label, ul);
        }
    }

    /// <summary>清空所有按钮</summary>
    public void Clear()
    {
        Buttons.Clear();
        ActiveIndex = -1;
    }

    /// <summary>获取活跃按钮</summary>
    public TuiButton? ActiveButton =>
        ActiveIndex >= 0 && ActiveIndex < Buttons.Count ? Buttons[ActiveIndex] : null;

    // ── 布局 ──

    public void RecalcLayout()
    {
        int offset = 0;
        foreach (var btn in Buttons)
        {
            int size = TuiHelper.DisplayWidth(btn.Text) + 4; // 左右边距
            if (btn.MinWidth > size) size = btn.MinWidth;

            if (Direction == LayoutMode.Horizontal)
            {
                btn.X = offset;
                btn.Y = 0;
                btn.Height = 1;
                btn.Width = size;
                offset += size + Gap;
            }
            else
            {
                btn.X = 0;
                btn.Y = offset;
                btn.Height = 1;
                btn.Width = size;
                offset += 1 + Gap;
            }
        }

        Height = Direction == LayoutMode.Horizontal ? 1
            : Math.Max(1, offset - Gap);
    }

    // ── 焦点管理 ──

    /// <summary>设置活跃按钮</summary>
    public void SetActive(int index)
    {
        if (index < 0 || index >= Buttons.Count) return;

        // 取消旧焦点
        var old = ActiveButton;
        if (old != null) { old.IsSelected = false; old.IsHovered = false; old.Focused = false; }

        ActiveIndex = index;
        var btn = Buttons[index];
        btn.IsSelected = true;
        btn.Focused = true;
        OnButtonActivated?.Invoke(index);
    }

    public void Next()
    {
        if (Buttons.Count == 0) return;
        int n = ActiveIndex + 1;
        if (n >= Buttons.Count) n = Wrap ? 0 : Buttons.Count - 1;
        SetActive(n);
    }

    public void Prev()
    {
        if (Buttons.Count == 0) return;
        int p = ActiveIndex - 1;
        if (p < 0) p = Wrap ? Buttons.Count - 1 : 0;
        SetActive(p);
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 按钮组本身不渲染背景，逐个渲染内部按钮（按钮不挂在父视图 Children 树上）
        foreach (var btn in Buttons)
        {
            if (!btn.Visible) continue;
            btn.Render(sb, absX, absY, ClipLeft, ClipTop, ClipRight, ClipBottom);
        }
    }

    // ── 输入 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || Buttons.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.Tab:
                if ((key.Modifiers & ConsoleModifiers.Shift) != 0) Prev(); else Next();
                return true;

            case ConsoleKey.RightArrow:
            case ConsoleKey.DownArrow:
                Next();
                return true;

            case ConsoleKey.LeftArrow:
            case ConsoleKey.UpArrow:
                Prev();
                return true;

            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                ActiveButton?.OnClick?.Invoke(ActiveButton);
                return true;

            default:
                // 字母快捷键
                char ch = char.ToUpperInvariant(key.KeyChar);
                if (ch >= 'A' && ch <= 'Z')
                {
                    for (int i = 0; i < Buttons.Count; i++)
                    {
                        var btn = Buttons[i];
                        if (btn.UnderlineIndex >= 0 && btn.UnderlineIndex < btn.Text.Length
                            && char.ToUpperInvariant(btn.Text[btn.UnderlineIndex]) == ch)
                        {
                            SetActive(i);
                            btn.OnClick?.Invoke(btn);
                            return true;
                        }
                    }
                }
                return false;
        }
    }

    /// <summary>鼠标点击按钮组中的按钮</summary>
    public override bool HandleMouse(InputEvent ev)
    {
        if (!IsEnabled || Buttons.Count == 0) return false;
        if (ev.Type != InputType.Mouse) return false;

        foreach (var btn in Buttons)
        {
            if (btn.HandleMouse(ev)) return true;
        }
        return false;
    }
}
