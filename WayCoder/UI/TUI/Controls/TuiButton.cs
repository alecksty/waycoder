using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 按钮控件 —— 对标 Crush button.go。
/// 支持快捷键下划线、选中高亮、悬停、点击回调。
/// </summary>
public class TuiButton : TuiControl
{
    /// <summary>
    /// 按钮文本
    /// </summary>
    public string Text { get; set; } = "OK";

    /// <summary>
    /// 点击回调
    /// </summary>
    public Action<TuiButton>? OnClick { get; set; }

    /// <summary>快捷键字符在文本中的索引（用于下划线标记），-1 = 无</summary>
    public int UnderlineIndex { get; set; } = -1;

    /// <summary>是否被选中/高亮</summary>
    public bool IsSelected { get; set; }

    /// <summary>最小显示宽度（不足补空格）</summary>
    public int MinWidth { get; set; }
    public int MaxWidth { get; set; }

    /// <summary>是否启用渐变背景</summary>
    public bool GradientBg { get; set; }

    /// <summary>渐变背景起始色（TrueColor 码）</summary>
    public int GradientBgStart { get; set; }

    /// <summary>渐变背景终止色（TrueColor 码）</summary>
    public int GradientBgEnd { get; set; }

    /// <summary>悬停状态（由 ButtonGroup 管理）</summary>
    public bool IsHovered { get; set; }

    public TuiButton()
    {
        Height = 1;
        Width = 10;
        TextAlign = EHAlign.Center;
    }

    public TuiButton(string text, Action<TuiButton>? onClick = null)
    {
        Text = text;
        OnClick = onClick;
        Height = 1;
        Width = AnsiHelper.DisplayWidth(text) + 4;
        TextAlign = EHAlign.Center;
    }

    /// <summary>创建带快捷键的按钮</summary>
    public TuiButton(string text, int underlineIndex, Action<TuiButton>? onClick = null) : this(text, onClick)
    {
        UnderlineIndex = underlineIndex;
    }

    /// <summary>
    /// 渲染按钮
    /// </summary>
    /// <param name="sb">渲染目标 StringBuilder</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;

        // 渲染渐变背景
        if (GradientBg && GradientBgStart >= 0x1000000 && GradientBgEnd >= 0x1000000)
        {
            ControlRenderer.DrawButtonGradientLine(sb, this, absX, absY,
                ControlRenderer.PadText(Text),
                TextAlign,
                t.ButtonFg,
                t.ControlFocusedFg,
                t.ControlDisabledFg,
                GradientBgStart,
                GradientBgEnd);
        }
        else
        {
            // 选中/悬停时使用特殊颜色
            int fg = t.ButtonFg, bg = t.ButtonBg;
            int focusedFg = t.ControlFocusedFg, focusedBg = t.ControlFocusedBg;

            if (IsSelected || Focused)
            {
                fg = focusedFg;
                bg = focusedBg;
            }
            else if (IsHovered)
            {
                fg = 37;
                bg = 44; // 蓝底白字
            }

            // 支持下划线快捷键
            var display = Text;

            if (MinWidth > 0 && display.Length < MinWidth)
                display = display.PadRight(MinWidth);

            ControlRenderer.DrawButtonLine(sb, this, absX, absY,
                ControlRenderer.PadText(display), TextAlign,
                fg, bg, focusedFg, focusedBg);
        }
    }

    /// <summary>
    /// 处理按键事件
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !Focused) return false;

        switch (key.Key)
        {
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                OnClick?.Invoke(this);
                return true;

            case ConsoleKey.LeftArrow:
            case ConsoleKey.UpArrow:
                FindRootView()?.FocusPrev();
                return true;

            case ConsoleKey.RightArrow:
            case ConsoleKey.DownArrow:
                FindRootView()?.FocusNext();
                return true;
        }

        return false;
    }

    /// <summary>沿 Parent 链找到顶层根视图（窗口的 RootView）</summary>
    private TuiView? FindRootView()
    {
        var p = Parent;
        while (p?.Parent != null)
        {
            p = p.Parent;
        }

        return (p ?? Parent) as TuiView;
    }

    /// <summary>鼠标左键点击触发按钮，hover 高亮</summary>
    public override bool OnMouse(InputEvent ev)
    {
        if (!IsEnabled) return false;
        if (ev.Type != InputType.Mouse) return false;

        // 悬停检测（鼠标移动事件）
        if (ev.MouseMotion)
        {
            int absX = GetAbsoluteX();
            int absY = GetAbsoluteY();
            bool inside = ev.MouseX >= absX && ev.MouseX < absX + Width &&
                          ev.MouseY >= absY && ev.MouseY < absY + Height;
            if (inside != IsHovered)
            {
                IsHovered = inside;
                MarkDirty();
            }

            return inside;
        }

        // 左键点击（需命中按钮区域，否则不触发 —— 否则点击按钮外也会误触）
        if (ev.MouseLeft)
        {
            int absX = GetAbsoluteX();
            int absY = GetAbsoluteY();
            bool inside = ev.MouseX >= absX && ev.MouseX < absX + Width &&
                          ev.MouseY >= absY && ev.MouseY < absY + Height;
            if (!inside) return false;
            Focused = true;
            OnClick?.Invoke(this);
            return true;
        }

        return false;
    }
}