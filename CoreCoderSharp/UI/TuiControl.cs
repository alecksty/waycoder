using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

/// <summary>外边距 / 内边距（上右下左，默认 0）</summary>
public struct EdgeInsets
{
    public int Top, Right, Bottom, Left;
    public EdgeInsets(int all = 0) => Top = Right = Bottom = Left = all;
    public EdgeInsets(int top, int right, int bottom, int left)
    {
        Top = top; Right = right; Bottom = bottom; Left = left;
    }
    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;
}

/// <summary>
/// 控件基类 —— 所有 TUI 控件的抽象根。
/// 控件位于其父容器（View 或 Window）内，坐标相对于父容器原点。
/// 内建裁剪：渲染内容不会超出控件边界（Width × Height）。
/// 内建底色/前景色：Fg/Bg 设置控件的默认前后景色。
/// Margin 控制父容器在布局时为本控件留出的外部间距。
/// Padding 控制控件内部内容区的缩进（渲染时自动内移裁剪区）。
/// </summary>
public abstract class TuiControl
{
    // ── 布局 ──
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 10;
    public int Height { get; set; } = 1;

    /// <summary>外部间距（父容器布局时在四周留出的空白）</summary>
    public EdgeInsets Margin { get; set; }

    /// <summary>内部间距（内容区向内缩进，渲染裁剪区自动扣除）</summary>
    public EdgeInsets Padding { get; set; }

    // ── 状态 ──
    public bool Visible { get; set; } = true;
    public bool Focused { get; set; }
    public TuiView? Parent { get; set; }

    /// <summary>
    /// 是否拥有光标控制权。每屏只有一个控件拥有光标。
    /// 由 TuiScreen.SetCursorOwner() 在每帧渲染前设置。
    /// </summary>
    public bool IsCursorOwner { get; set; }

    /// <summary>光标请求的行/列（OnRender 时记录，由 Screen 在最后统一输出）</summary>
    protected int _cursorRow, _cursorCol;
    protected bool _showCursor;

    /// <summary>获取光标状态（仅光标所有者返回有效值）</summary>
    public virtual (int row, int col, bool show)? GetCursorState()
        => _showCursor ? (_cursorRow, _cursorCol, true) : null;

    // ── 颜色 ──
    /// <summary>默认前景色（ANSI 色码，0=继承父容器）</summary>
    public int Fg { get; set; }
    /// <summary>默认背景色（ANSI 色码，0=透明/继承）</summary>
    public int Bg { get; set; }

    /// <summary>渲染上下文中继承的背景色（由窗口填充设置，WriteAt 在 Bg=0 时自动使用）</summary>
    public static int EffectiveBg { get; set; }

    // ── 裁剪区域（绝对值，每帧由 Render 计算；子类可覆写以支持滚动等效果） ──
    protected int ClipLeft { get; set; }
    protected int ClipTop { get; set; }
    protected int ClipRight { get; set; }
    protected int ClipBottom { get; set; }

    // ── 渲染入口（模板方法） ──

    /// <summary>
    /// 渲染控件。计算裁剪区域 → 填充底色 → 调用子类 OnRender。
    /// clipL/T/R/B 为父容器的裁剪约束（可选），控件实际裁剪区取交集。
    /// Padding 自动内移裁剪区，Margin 由父容器布局时处理。
    /// </summary>
    public void Render(StringBuilder sb, int parentAbsX, int parentAbsY,
        int clipL = int.MinValue, int clipT = int.MinValue,
        int clipR = int.MaxValue, int clipB = int.MaxValue)
    {
        if (!Visible) return;

        var absX = parentAbsX + X;
        var absY = parentAbsY + Y;

        // 控件自身裁剪区（含 Padding 内缩）
        var selfL = absX + Padding.Left;
        var selfT = absY + Padding.Top;
        var selfR = absX + Width - Padding.Right;
        var selfB = absY + Height - Padding.Bottom;

        // 与父容器约束取交集
        ClipLeft   = Math.Max(selfL, clipL);
        ClipTop    = Math.Max(selfT, clipT);
        ClipRight  = Math.Min(selfR, clipR);
        ClipBottom = Math.Min(selfB, clipB);

        // 完全不可见则跳过
        if (ClipLeft >= ClipRight || ClipTop >= ClipBottom) return;

        // 每个控件初始不显示光标（只有光标所有者在其 OnRender 内设置 _showCursor）
        _showCursor = false;

        // 渲染底色（只在控件自身区域内填充，受父约束裁剪）
        if (Bg > 0)
        {
            for (int r = ClipTop; r < ClipBottom; r++)
            {
                var rb = new RenderBuffer();
                rb.Write(r, ClipLeft, new string(' ', ClipRight - ClipLeft), bg: Bg);
                sb.Append(rb.ToString());
            }
        }

        // 调用子类渲染（内容原点右移 Padding.Left、下移 Padding.Top）
        OnRender(sb, absX + Padding.Left, absY + Padding.Top);
    }

    /// <summary>子类实现：绘制内容。absX/absY 为控件左上角绝对坐标。</summary>
    protected abstract void OnRender(StringBuilder sb, int absX, int absY);

    // ── 输入 ──
    public virtual bool HandleKey(ConsoleKeyInfo key) => false;
    public virtual void OnResize(int newParentW, int newParentH) { }

    // ── 裁剪写入工具 ──

    /// <summary>
    /// 写入文本到绝对坐标 (row, col)，超出控件边界自动裁剪。
    /// </summary>
    protected void WriteAt(StringBuilder sb, int row, int col, string text, int fg = 0, int bg = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (col >= ClipRight || row < ClipTop || row >= ClipBottom) return;

        var textVw = TuiHelper.DisplayWidth(text);
        if (col + textVw <= ClipLeft) return;

        // 左侧裁剪：跳过被遮挡的字符
        int skipVw = 0;
        int charIdx = 0;
        if (col < ClipLeft)
        {
            int needSkip = ClipLeft - col;
            foreach (var rune in text.EnumerateRunes())
            {
                int rw = TuiHelper.RuneWidth(rune);
                if (skipVw + rw > needSkip) break;
                skipVw += rw;
                charIdx++;
            }
            text = text[charIdx..];
            col = ClipLeft;
        }

        // 右侧裁剪：截断超出部分
        int maxVw = ClipRight - col;
        if (TuiHelper.DisplayWidth(text) > maxVw)
            text = TuiHelper.TruncateByWidth(text, maxVw);

        if (string.IsNullOrEmpty(text)) return;

        var rb = new RenderBuffer();
        var effectiveBg = bg > 0 ? bg : (Bg > 0 ? Bg : EffectiveBg);
        rb.Write(row, col, text, fg: fg > 0 ? fg : Fg, bg: effectiveBg);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 在控件内相对位置写入文本。row/col 相对于控件左上角 (0, 0)。
    /// </summary>
    protected void WriteLine(StringBuilder sb, int row, int col, string text, int fg = 0, int bg = 0)
    {
        WriteAt(sb, ClipTop + row, ClipLeft + col, text, fg, bg);
    }

    /// <summary>
    /// 用字符填充控件内一行（自动裁剪到控件宽度）。
    /// </summary>
    protected void FillLine(StringBuilder sb, int row, char ch = ' ', int? fg = null, int? bg = null)
    {
        int y = ClipTop + row;
        if (y < ClipTop || y >= ClipBottom) return;
        var fill = new string(ch, Width);
        var rb = new RenderBuffer();
        rb.Write(y, ClipLeft, fill, fg: fg ?? (Fg > 0 ? Fg : 0), bg: bg ?? (Bg > 0 ? Bg : 0));
        sb.Append(rb.ToString());
    }
}
