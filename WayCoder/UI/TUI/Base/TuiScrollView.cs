using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 滚动视图 —— 内容实际高度（ContentHeight）可大于可见区域高度，
/// 通过 ScrollOffset 控制可见窗口位置。
/// 子控件以完整内容高度布局，渲染时自动偏移。
/// 滚动状态机（ScrollOffset / 四个滚动方法 / 跟底标志 / OnResize 钳制）在
/// <see cref="TuiScrollable"/>，本类只管「按内容高度布局 + 渲染偏移 + 跟底时机」。
/// </summary>
public class TuiScrollView : TuiScrollable
{
    public override void Layout()
    {
        var prevContentHeight = ContentHeight;
        int y = 0;
        foreach (var child in Children)
        {
            // 递归布局嵌套视图（与 TuiVBox/TuiHBox 一致），否则嵌套容器子控件高度未计算、布局错乱
            if (child is TuiView childView)
                childView.Layout();
            if (ChildHAlign == EHAlign.Stretch)
                child.Width = Width;
            child.X = AlignX(child.Width) + child.Margin.Left;
            child.Y = y + child.Margin.Top;
            y += child.Height + child.Margin.Vertical;
        }

        ContentHeight = y;

        // 内容增长时自动跟底
        if (IsAutoScrollToEnd && ContentHeight > prevContentHeight)
        {
            ScrollOffset = Math.Max(0, ContentHeight - Height);
        }
    }

    /// <summary>
    /// 渲染滚动视图内容。
    /// </summary>
    /// <param name="sb">渲染缓冲区</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 内容变更/滚动时整区擦除（仿 TuiListView）：子项渲染不补齐整行宽度，
        // 增删/切换后旧像素会残留在底部/右侧。仅在自身脏时填充，避免无变化时闪屏。
        if (IsDirty)
        {
            int fillBg = GetInheritedBg();
            int fl = Math.Max(ClipLeft, absX);
            int fr = Math.Min(ClipRight, absX + Width);
            int ft = Math.Max(ClipTop, absY);
            int fb = Math.Min(ClipBottom, absY + Height);
            if (fr > fl && fb > ft)
            {
                var rb = new RenderBuffer();
                if (fillBg <= 0) rb.Reset(); // 透明背景：先复位，空格才能清掉残留
                for (int row = ft; row < fb; row++)
                    rb.Fill(row, fl, fr - fl, fillBg);
                sb.Append(rb.ToString());
            }
        }

        // 调整裁剪区域：Y 偏移减去滚动量。须与父容器裁剪区取交集，否则嵌套在更紧裁剪的
        // 父容器内时，子控件会越过父裁剪边界越界绘制。
        var savedTop = ClipTop;
        var savedBottom = ClipBottom;
        ClipTop = Math.Max(savedTop, absY);
        ClipBottom = Math.Min(savedBottom, absY + Height);

        foreach (var child in Children)
        {
            if (!child.Visible) continue;
            var childAbsY = absY + child.Y - ScrollOffset;
            var childAbsX = absX + child.X;

            // 完全不可见则跳过
            if (childAbsY + child.Height <= ClipTop || childAbsY >= ClipBottom)
                continue;

            // 增量模式只画脏子项（对齐 TuiView.OnRender）：否则滚动视图内容每次 Render 全量重绘，
            // 会把盖在上层的模态弹框区域覆盖掉（弹框增量只补自身脏控件，边框/背景不重画 → 花屏）。
            // 视图容器（嵌套 TuiView）始终遍历递归查脏后代；滚动/内容变更时 MarkDirtyTree 已全树标脏，
            // 因此「只画脏」不影响滚动与增删的正确性。
            if (child is TuiView || child.IsDirty || IsDirty)
                child.Render(sb, absX, absY - ScrollOffset, ClipLeft, ClipTop, ClipRight, ClipBottom);
            child.IsDirty = false; // 渲染后清脏（TuiScrollView 不走 TuiView.OnRender 的清理路径）
        }

        ClipTop = savedTop;
        ClipBottom = savedBottom;
    }

    /// <summary>添加子控件后自动跟底</summary>
    public override void Add(TuiControl child)
    {
        base.Add(child);
        if (IsAutoScrollToEnd)
            ScrollToBottom();
    }

    /// <summary>更新布局后自动跟底</summary>
    public void RefreshLayout()
    {
        Layout();
        if (IsAutoScrollToEnd)
            ScrollToBottom();
    }
}