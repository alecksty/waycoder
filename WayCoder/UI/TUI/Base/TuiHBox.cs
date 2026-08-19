using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 水平布局容器 —— 子控件从左到右排列。
/// 支持垂直对齐（ChildVAlign）和内容水平对齐。
/// </summary>
public class TuiHBox : TuiView
{
    public int Spacing { get; set; }

    /// <summary>内容水平对齐方式（当子控件总宽小于容器宽时）</summary>
    public EHAlign ContentHAlign { get; set; } = EHAlign.Left;

    public override void Layout()
    {
        // ── 0. Flex 弹性分配（在测量之前设置 Flex>0 子控件的宽度；浮动控件不参与流式布局）──
        int totalFlex = 0;
        int totalFixedW = 0;
        int flowCount = 0;
        foreach (var child in Children)
        {
            if (child.Floating || !child.Visible) continue;
            flowCount++;
            if (child.Flex > 0)
                totalFlex += child.Flex;
            else
                totalFixedW += child.Width + child.Margin.Horizontal;
        }

        if (totalFlex > 0)
        {
            int flexMarginW = 0;
            foreach (var child in Children)
                if (child.Flex > 0 && !child.Floating && child.Visible)
                    flexMarginW += child.Margin.Horizontal;
            int remaining = Width - totalFixedW - flexMarginW - Math.Max(0, flowCount - 1) * Spacing;
            if (remaining > 0)
            {
                int allocated = 0;
                // 最后一个 Flex 子控件拿剩余，避免取整损失
                TuiBase? lastFlexChild = null;
                foreach (var child in Children)
                {
                    if (child.Flex > 0 && !child.Floating && child.Visible)
                    {
                        int w = Math.Max(1, remaining * child.Flex / totalFlex);
                        child.Width = w;
                        allocated += w;
                        lastFlexChild = child;
                    }
                }

                // 修正取整误差：最后一个 Flex 子控件补齐
                if (lastFlexChild != null && remaining > allocated)
                    lastFlexChild.Width += remaining - allocated;
            }
        }

        // 第一遍：计算总宽度（含 child margin）和最大行高，递归布局嵌套视图（浮动子视图仍递归布局内部）
        int totalW = 0;
        int maxH = 1;
        foreach (var child in Children)
        {
            if (child is TuiView childView)
                childView.Layout();
            if (child.Floating || !child.Visible) continue;
            if (ChildVAlign == EVAlign.Stretch)
                child.Height = Height;
            totalW += child.Width + child.Margin.Horizontal + Spacing;
            maxH = Math.Max(maxH, child.Height + child.Margin.Vertical);
        }

        if (totalW > 0) totalW -= Spacing;

        // 内容水平对齐偏移
        int contentOffset = ContentHAlign switch
        {
            EHAlign.Center => (Width - totalW) / 2,
            EHAlign.Right => Width - totalW,
            EHAlign.Stretch => 0,
            _ => 0
        };

        // 第二遍：设置位置（Margin.Left/Right/Top 偏移；浮动控件跳过，保留手动 X/Y）
        int x = Math.Max(0, contentOffset);
        foreach (var child in Children)
        {
            if (child.Floating || !child.Visible) continue;
            child.X = x + child.Margin.Left;
            child.Y = AlignY(child.Height + child.Margin.Vertical, maxH) + child.Margin.Top;
            x += child.Width + child.Margin.Horizontal + Spacing;
        }

        Height = maxH;
        if (contentOffset == 0)
            Width = Math.Max(1, x - Spacing);
    }
}