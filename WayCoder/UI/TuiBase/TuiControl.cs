using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

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
public abstract class TuiControl : TuiBase
{
    // ── 布局（X/Y 来自 TuiBase） ──

    /// <summary>外部间距（父容器布局时在四周留出的空白）</summary>
    public EdgeInsets Margin { get; set; }

    /// <summary>内部间距（内容区向内缩进，渲染裁剪区自动扣除）</summary>
    public EdgeInsets Padding { get; set; }

    /// <summary>文本对齐方式（子控件在渲染时读取）</summary>
    public HAlign TextAlign { get; set; } = HAlign.Left;

    // ── 状态 ──
    public bool Visible { get; set; } = true;

    /// <summary>是否可用。禁用时跳过渲染且不响应输入。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>是否可获得键盘焦点。展示控件应设为 false。</summary>
    public virtual bool CanFocus => true;

    /// <summary>是否在获得焦点时显示终端光标。仅输入类控件应覆写为 true。</summary>
    public virtual bool HasCursor => false;

    public bool Focused { get; set; }
    public TuiView? Parent { get; set; }

    // ── 增量渲染 ──

    /// <summary>标记控件需要重绘，沿 Parent 链向上传播到根。</summary>
    public override void MarkDirty()
    {
        if (IsDirty) return; // 已标记，避免重复遍历
        IsDirty = true;
        Parent?.MarkDirty();
    }

    /// <summary>强制刷新：递归标记控件及其所有子控件为脏，确保下一帧完全重绘。</summary>
    public override void Invalidate()
    {
        IsDirty = true;
    }

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
    {
        // 每次取光标状态时强制刷新位置——不依赖 OnRender 是否被调用
        if (IsCursorOwner)
            EnsureCursorPosition();
        return _showCursor ? (_cursorRow, _cursorCol, true) : null;
    }

    /// <summary>
    /// 确保光标坐标是最新的。子类覆写以提供精确的光标位置计算。
    /// 如果 OnRender 已设置光标（_showCursor=true），跳过以节省计算。
    /// 默认实现：基于绝对坐标。
    /// </summary>
    protected virtual void EnsureCursorPosition()
    {
        if (!IsCursorOwner || _showCursor) return; // 已由 OnRender 设置
        _cursorRow = GetAbsoluteY();
        _cursorCol = GetAbsoluteX();
        _showCursor = true;
    }

    // ── 颜色 ──
    /// <summary>默认前景色（ANSI 色码，0=继承父容器）</summary>
    public int Fg { get; set; }
    /// <summary>默认背景色（ANSI 色码，0=透明/继承）</summary>
    public int Bg { get; set; }

    /// <summary>获得焦点时的前景色（0=使用 Fg）</summary>
    public int FocusedFg { get; set; }
    /// <summary>获得焦点时的背景色（0=使用 Bg）</summary>
    public int FocusedBg { get; set; }

    /// <summary>禁用时的前景色（0=自动变灰）</summary>
    public int DisabledFg { get; set; }
    /// <summary>禁用时的背景色（0=使用 Bg）</summary>
    public int DisabledBg { get; set; }

    /// <summary>
    /// 获取当前有效的前景色（自动根据 Focused/IsEnabled 状态选择）。
    /// 子控件在 OnRender 中调用此方法获取正确颜色。
    /// </summary>
    public int EffectiveFg
    {
        get
        {
            if (!IsEnabled) return DisabledFg > 0 ? DisabledFg : 90; // 默认灰色
            if (Focused && FocusedFg > 0) return FocusedFg;
            return Fg;
        }
    }

    /// <summary>
    /// 获取当前有效的背景色（自动根据 Focused/IsEnabled 状态选择）。
    /// </summary>
    public int EffectiveBg
    {
        get
        {
            if (!IsEnabled) return DisabledBg > 0 ? DisabledBg : Bg;
            if (Focused && FocusedBg > 0) return FocusedBg;
            return Bg;
        }
    }

    /// <summary>继承/级联的背景色（由窗口/父容器设置，WriteAt 在 Bg=0 时自动使用）</summary>
    public static int CascadedBg { get; set; }

    /// <summary>
    /// 沿 Parent 链向上查找第一个非零背景色。
    /// 递归到 Window 为止，若全链为 0 则返回 CascadedBg。
    /// </summary>
    public int GetInheritedBg()
    {
        var p = Parent;
        while (p != null)
        {
            if (p.Bg > 0) return p.Bg;
            p = p.Parent;
        }
        return CascadedBg;
    }

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
        // 禁用状态使用 DisabledBg（若有设置），否则使用默认 Bg
        int effectiveBg = !IsEnabled && DisabledBg > 0 ? DisabledBg : Bg;
        if (effectiveBg > 0)
        {
            for (int r = ClipTop; r < ClipBottom; r++)
            {
                var rb = new RenderBuffer();
                rb.Write(r, ClipLeft, new string(' ', ClipRight - ClipLeft), bg: effectiveBg);
                sb.Append(rb.ToString());
            }
        }

        // 调用子类渲染（内容原点右移 Padding.Left、下移 Padding.Top）
        OnRender(sb, absX + Padding.Left, absY + Padding.Top);
    }

    /// <summary>子类实现：绘制内容。absX/absY 为控件左上角绝对坐标。</summary>
    protected abstract void OnRender(StringBuilder sb, int absX, int absY);

    // ── 输入 ──

    /// <summary>
    /// 按键钩子。返回 true 表示已消费按键（不再继续处理）。
    /// 在 OnKey 最前面调用，优先级高于 Enabled/CanFocus/Focused 检查。
    /// 典型用途：PromptBar 钩住 InputArea 拦截 ↑↓/Enter/Esc。
    /// </summary>
    public Func<ConsoleKeyInfo, bool>? KeyHook { get; set; }

    /// <summary>
    /// 按键入口。检查 Hook → Enabled/CanFocus → 交给子类处理。
    /// 容器子类（TuiView）覆写此方法以加入子节点路由。
    /// </summary>
    public override bool OnKey(ConsoleKeyInfo key)
    {
        // Hook 优先拦截（不受 Enabled/CanFocus 限制）
        if (KeyHook != null && KeyHook(key))
            return true;

        if (!IsEnabled) return false;
        if (!CanFocus) return false;
        return false;
    }
    /// <summary>鼠标事件处理。子类覆写以支持点击等交互。</summary>
    public override bool HandleMouse(InputEvent ev) => false;
    public override void OnResize(int newParentW, int newParentH) { }

    // ── 生命周期 ──

    /// <summary>控件加入控件树时调用（Add 设置 Parent 后）。初始化子对象、订阅事件。</summary>
    public override void OnCreate() { }

    /// <summary>控件从控件树移除时调用（Remove/Clear 前）。取消订阅、释放资源。</summary>
    public override void OnDestroy() { }

    // ── 命中测试 ──

    /// <summary>
    /// 检测指定绝对坐标是否命中本控件。
    /// 返回最深层的命中控件（默认返回自身），子类（如容器）可覆写以递归到子控件。
    /// </summary>
    public virtual TuiControl? HitTest(int absX, int absY)
    {
        if (!Visible || !IsEnabled) return null;
        // 计算控件在屏幕上的绝对位置（需要父容器坐标）
        int myAbsX = 0, myAbsY = 0;
        if (Parent != null)
        {
            // 父容器坐标由 Render 时传入的 parentAbsX/Y 确定，
            // 但 HitTest 时没有该上下文。使用 X/Y + 递归查找根坐标。
            myAbsX = GetAbsoluteX();
            myAbsY = GetAbsoluteY();
        }
        else
        {
            myAbsX = X;
            myAbsY = Y;
        }
        if (absX >= myAbsX && absX < myAbsX + Width &&
            absY >= myAbsY && absY < myAbsY + Height)
            return this;
        return null;
    }

    /// <summary>沿 Parent 链计算绝对 X 坐标</summary>
    protected int GetAbsoluteX()
    {
        int x = X;
        var p = Parent;
        while (p != null)
        {
            x += p.X;
            p = p.Parent;
        }
        return x;
    }

    /// <summary>沿 Parent 链计算绝对 Y 坐标</summary>
    protected int GetAbsoluteY()
    {
        int y = Y;
        var p = Parent;
        while (p != null)
        {
            y += p.Y;
            p = p.Parent;
        }
        return y;
    }

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
        var effectiveBg = bg > 0 ? bg : (Bg > 0 ? Bg : GetInheritedBg());
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
