using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 垂直布局容器 —— 子控件从上到下排列。
/// 支持水平对齐（ChildHAlign）和内容垂直对齐（ContentVAlign）。
/// </summary>
public class TuiVBox : TuiView
{
    public int Spacing { get; set; }

    public override void Layout()
    {
        // ── 0. Flex 弹性分配（在测量之前设置 Flex>0 子控件的高度；浮动控件不参与流式布局）──
        var totalFlex = 0;
        var totalFixedH = 0;
        var flowCount = 0;
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            flowCount++;
            if (child.Flex > 0)
                totalFlex += child.Flex;
            else
                totalFixedH += child.Height + child.Margin.Vertical;
        }

        if (totalFlex > 0)
        {
            int flexMarginH = 0;
            foreach (var child in Children)
                if (child.Flex > 0 && !child.Floating)
                    flexMarginH += child.Margin.Vertical;
            int remaining = Height - totalFixedH - flexMarginH - Math.Max(0, flowCount - 1) * Spacing;
            if (remaining > 0)
            {
                int allocated = 0;
                TuiBase? lastFlexChild = null;
                foreach (var child in Children)
                {
                    if (child.Flex > 0 && !child.Floating)
                    {
                        int h = Math.Max(1, remaining * child.Flex / totalFlex);
                        child.Height = h;
                        allocated += h;
                        lastFlexChild = child;
                    }
                }

                if (lastFlexChild != null && remaining > allocated)
                    lastFlexChild.Height += remaining - allocated;
            }
        }

        // 第一遍：计算总高度（含 child margin），递归布局嵌套视图（浮动子视图仍递归布局内部）。
        // 先拉伸子宽度再布局：子容器（如 HBox）需用正确的容器宽做内部 flex 分配，
        // 否则会用过小的旧宽度算 remaining（负 → 不分配 → 子控件按默认宽排布溢出）。
        var totalH = 0;
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            if (ChildHAlign == EHAlign.Stretch)
                child.Width = Width;
            if (child is TuiView childView)
                childView.Layout();
            totalH += child.Height + child.Margin.Vertical + Spacing;
        }

        if (totalH > 0) totalH -= Spacing;

        // 内容垂直对齐偏移
        var contentOffset = ContentVAlign switch
        {
            EVAlign.Middle => (Height - totalH) / 2,
            EVAlign.Bottom => Height - totalH,
            _ => 0
        };

        // 第二遍：设置位置（Margin.Top 偏移，Margin.Left 水平对齐；浮动控件跳过，保留手动 X/Y）
        var y = Math.Max(0, contentOffset);
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            child.Y = y + child.Margin.Top;
            child.X = AlignX(child.Width) + child.Margin.Left;
            y += child.Height + child.Margin.Vertical + Spacing;
        }

        // 如果内容超出容器，更新 Height
        if (contentOffset == 0)
            Height = Math.Max(1, y - Spacing);
    }
}