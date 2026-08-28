using System.Text;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 独立滚动条组件 —— 对标 Crush scrollbar.go。
/// 垂直滑块、百分比指示、自动隐藏、鼠标滚轮+拖拽。
/// </summary>
public class TuiScrollbar : TuiDisplayControl
{
    /// <summary>内容总高度（行数）</summary>
    public int ContentHeight { get; set; }

    /// <summary>视口高度（可见行数）</summary>
    public int ViewportHeight { get; set; } = 10;

    /// <summary>当前滚动偏移量（顶部行号）</summary>
    public int ScrollOffset { get; set; }

    /// <summary>滚动条样式：bar / dot / block</summary>
    public string Style { get; set; } = "bar";

    /// <summary>内容无需滚动时隐藏</summary>
    public bool AutoHide { get; set; } = true;

    /// <summary>轨道字符</summary>
    public char TrackChar { get; set; } = '│';

    /// <summary>滑块字符</summary>
    public char ThumbChar { get; set; } = '█';

    /// <summary>轨道前景色</summary>
    public int TrackFg { get; set; } = 90;

    /// <summary>滑块前景色</summary>
    public int ThumbFg { get; set; } = 37;

    /// <summary>滚动条背景色（覆盖 TuiControl.Bg）</summary>
    public new int Bg { get; set; }

    /// <summary>鼠标拖拽中</summary>
    private bool _dragging;

    /// <summary>关联的滚动回调（供外部绑定）</summary>
    public Action<int>? OnScroll { get; set; }

    public TuiScrollbar()
    {
        Width = 1;
        Height = 10;
    }


    // ── 计算属性 ──

    /// <summary>是否需要滚动条</summary>
    public bool IsNeeded => ContentHeight > ViewportHeight;

    /// <summary>滚动百分比 (0.0-1.0)</summary>
    public double Percent
    {
        get
        {
            int max = Math.Max(0, ContentHeight - ViewportHeight);
            return max > 0 ? (double)ScrollOffset / max : 0;
        }
    }

    /// <summary>滑块高度（行数）</summary>
    public int ThumbHeight
    {
        get
        {
            if (ContentHeight <= 0) return Height;
            int h = (int)((long)Height * ViewportHeight / ContentHeight);
            return Math.Max(1, Math.Min(Height, h));
        }
    }

    /// <summary>滑块在轨道中的行位置</summary>
    public int ThumbPos
    {
        get
        {
            if (!IsNeeded) return 0;
            int trackH = Height;
            int thumbH = ThumbHeight;
            int maxScroll = ContentHeight - ViewportHeight;
            int maxThumb = trackH - thumbH;
            return maxScroll > 0 ? (int)((long)ScrollOffset * maxThumb / maxScroll) : 0;
        }
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (AutoHide && !IsNeeded) return;
        if (!Visible || Height <= 0) return;

        int pos = ThumbPos;
        int thumbH = ThumbHeight;
        int trackH = Height;

        for (int i = 0; i < trackH; i++)
        {
            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            bool isThumb = Style == "bar" ? (i >= pos && i < pos + thumbH)
                         : Style == "block" ? (i >= pos && i < pos + thumbH)
                         : Style == "dot" ? (i == pos) : false;

            int fg = isThumb ? ThumbFg : TrackFg;
            char ch = isThumb ? ThumbChar : TrackChar;

            WriteAt(sb, row, absX, ch.ToString(), fg, Bg);
        }
    }

    // ── 鼠标 ──

    public override bool OnMouse(InputEvent ev)
    {
        if (!IsEnabled || !IsNeeded) return false;
        if (ev.Type != InputType.Mouse) return false;

        int relY = ev.MouseY - GetAbsoluteY(); // MouseY 是绝对屏幕坐标，需用绝对 Y 而非局部 Y

        if (ev.MouseScrollUp) { ScrollUp(3); return true; }
        if (ev.MouseScrollDown) { ScrollDown(3); return true; }

        // 拖拽中（移动）：必须先于「按下」分支判断 —— 拖动时同样满足 MouseLeft && !MouseRelease，
        // 若按下分支在前会先 return true，导致本分支永不触发、OnScroll 回调永远不调用。
        if (_dragging && ev.MouseLeft && !ev.MouseRelease)
        {
            ScrollOffset = OffsetFromY(relY);
            ScrollOffset = ClampOffset();
            OnScroll?.Invoke(ScrollOffset);
            return true;
        }

        // 鼠标按下：跳到点击位置并进入拖拽
        if (ev.MouseLeft && !ev.MouseRelease)
        {
            if (relY >= 0 && relY < Height)
            {
                _dragging = true;
                ScrollOffset = OffsetFromY(relY);
                ScrollOffset = ClampOffset();
                OnScroll?.Invoke(ScrollOffset);
            }
            return true;
        }

        if (ev.MouseRelease)
        {
            _dragging = false;
            return true;
        }

        return false;
    }

    private int OffsetFromY(int y)
    {
        if (!IsNeeded) return 0;
        int trackH = Height;
        int thumbH = ThumbHeight;
        int maxScroll = ContentHeight - ViewportHeight;
        int maxThumb = trackH - thumbH;
        if (maxThumb <= 0) return 0;
        double frac = (double)(y - thumbH / 2) / maxThumb;
        frac = Math.Clamp(frac, 0, 1);
        return (int)(frac * maxScroll);
    }

    private int ClampOffset() =>
        Math.Clamp(ScrollOffset, 0, Math.Max(0, ContentHeight - ViewportHeight));

    // ── 键盘滚动 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !IsNeeded) return false;
        switch (key.Key)
        {
            case ConsoleKey.PageUp: ScrollUp(ViewportHeight); return true;
            case ConsoleKey.PageDown: ScrollDown(ViewportHeight); return true;
            case ConsoleKey.Home: ScrollToTop(); return true;
            case ConsoleKey.End: ScrollToBottom(); return true;
        }
        return false;
    }

    // ── 滚动方法 ──

    public void ScrollUp(int lines = 1) => ScrollOffset = Math.Max(0, ScrollOffset - lines);
    public void ScrollDown(int lines = 1) =>
        ScrollOffset = Math.Min(Math.Max(0, ContentHeight - ViewportHeight), ScrollOffset + lines);
    public void ScrollToTop() => ScrollOffset = 0;
    public void ScrollToBottom() => ScrollOffset = Math.Max(0, ContentHeight - ViewportHeight);
}
