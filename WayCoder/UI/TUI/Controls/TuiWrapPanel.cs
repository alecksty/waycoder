using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 流式布局面板 —— 子控件从左到右排列，超出宽度自动换行。
/// 类似 CSS flex-wrap: wrap。
/// </summary>
public class TuiWrapPanel : TuiView
{
    /// <summary>水平方向（默认）。Vertical 为从上到下、超出高度自动换列。</summary>
    public Orientation Direction { get; set; } = Orientation.Horizontal;

    /// <summary>列间距（水平方向间隔）</summary>
    public int ColumnSpacing { get; set; } = 1;

    /// <summary>行间距（垂直方向间隔）</summary>
    public int RowSpacing { get; set; }

    /// <summary>子控件统一宽度（0=使用控件自身宽度）</summary>
    public int ItemWidth { get; set; }

    /// <summary>子控件统一高度（0=使用控件自身高度）</summary>
    public int ItemHeight { get; set; }

    public TuiWrapPanel()
    {
        Width = 40;
        Height = 10;
    }

    public override void Layout()
    {
        if (Direction == Orientation.Horizontal)
            LayoutHorizontal();
        else
            LayoutVertical();
    }

    /// <summary>水平流式布局：左→右，超出宽度换行</summary>
    private void LayoutHorizontal()
    {
        int x = 0, y = 0, rowHeight = 0;

        foreach (var child in Children)
        {
            int cw = ItemWidth > 0 ? ItemWidth : child.Width;
            int ch = ItemHeight > 0 ? ItemHeight : child.Height;

            // 超出容器宽度 → 换行
            if (x > 0 && x + cw > Width)
            {
                x = 0;
                y += rowHeight + RowSpacing;
                rowHeight = 0;
            }

            // 设置子控件位置和尺寸
            child.X = x + child.Margin.Left;
            child.Y = y + child.Margin.Top;
            if (ItemWidth > 0) child.Width = ItemWidth;
            if (ItemHeight > 0) child.Height = ItemHeight;

            // 递归布局嵌套视图
            if (child is TuiView childView)
                childView.Layout();

            x += cw + child.Margin.Horizontal + ColumnSpacing;
            rowHeight = Math.Max(rowHeight, ch + child.Margin.Vertical);
        }

        // 更新面板高度（至少能容纳所有行）
        int totalH = y + rowHeight;
        if (totalH > Height) Height = totalH;
    }

    /// <summary>垂直流式布局：上→下，超出高度换列</summary>
    private void LayoutVertical()
    {
        int y = 0, x = 0, colWidth = 0;

        foreach (var child in Children)
        {
            int cw = ItemWidth > 0 ? ItemWidth : child.Width;
            int ch = ItemHeight > 0 ? ItemHeight : child.Height;

            // 超出容器高度 → 换列
            if (y > 0 && y + ch > Height)
            {
                y = 0;
                x += colWidth + ColumnSpacing;
                colWidth = 0;
            }

            child.X = x + child.Margin.Left;
            child.Y = y + child.Margin.Top;
            if (ItemWidth > 0) child.Width = ItemWidth;
            if (ItemHeight > 0) child.Height = ItemHeight;

            if (child is TuiView childView)
                childView.Layout();

            y += ch + child.Margin.Vertical + RowSpacing;
            colWidth = Math.Max(colWidth, cw + child.Margin.Horizontal);
        }

        int totalW = x + colWidth;
        if (totalW > Width) Width = totalW;
    }
}

/// <summary>布局方向</summary>
public enum Orientation { Horizontal, Vertical }
