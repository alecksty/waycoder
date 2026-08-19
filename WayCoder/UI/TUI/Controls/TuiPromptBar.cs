using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 提示栏 —— 输入框上方可显示/隐藏的提示列表。
/// Bg==0 时绘制边框无底色；Bg>0 时全行填充底色 + 底部分隔线。
/// </summary>
public class TuiPromptBar : TuiControl
{
    #region 属性

    public override bool CanFocus => true;

    /// <summary>提示条目列表</summary>
    public List<PromptItem> Items { get; set; } = [];

    /// <summary>当前高亮索引 (-1 = 无选中)</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>当前可见索引</summary>
    public int ViewIndex { get; set; } = 0;

    /// <summary>最大可见条目数</summary>
    public int MaxVisible { get; set; } = 8;

    /// <summary>空列表时的提示文本</summary>
    public string EmptyHint { get; set; } = "";

    /// <summary>选中回调</summary>
    public Action<PromptItem>? OnSelect { get; set; }

    /// <summary>列表项高度（行）</summary>
    public int ItemHeight { get; set; } = 1;

    /// <summary>边框/分隔线颜色</summary>
    public int SeparatorColor { get; set; } = AnsiColors.BrightBlack;

    /// <summary>边框样式（Bg==0 时生效）</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

    #endregion

    public TuiPromptBar()
    {
        Height = 1;
    }

    /// <summary>
    /// 渲染提示栏。
    /// </summary>
    /// <param name="sb">渲染缓冲区</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var bordered = Bg == 0;
        var visibleCount = MaxVisible;

        var fg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
        var borderFg = SeparatorColor;
        var bc = AnsiHelper.GetBorderChars(BorderStyle);
        var highlightFg = TuiTheme.Current.ControlFocusedFg;
        var highlightBg = TuiTheme.Current.ControlFocusedBg;

        // ── 上边框 ──
        if (bordered)
        {
            WriteBorder(sb, absY, absX, bc.TL, bc.HT, bc.TR, Width, borderFg);
        }

        var contentStartY = bordered ? absY + 1 : absY;
        var leftPad = bordered ? 1 : 0; // 边框内缩

        var fillLeft = Math.Max(absX + 1, ClipLeft);
        var fillRight = Math.Min(absX + Width - 2, ClipRight);
        var strSpaces = new string(' ', Math.Max(0, fillRight - fillLeft)); // 负值（左缘被裁过右缘）防崩溃

        // ── 列表行（只渲染实际条目，不预留空行；高度由 ShowPromptBar 按条目数设定）──
        for (var i = 0; i < visibleCount; i++)
        {
            var row = contentStartY + i * ItemHeight;
            // if (row < ClipTop || row >= ClipBottom) continue;
            var pos = i + ViewIndex;
            var rb = new RenderBuffer();
            var hasItem = (pos < Items.Count);
            // 选中状态
            var sel = pos == SelectedIndex;

            if (hasItem)
            {
                var item = Items[pos % Items.Count];
                var itemFg = sel ? AnsiColors.BgBrightBlue : fg;

                var rowBg = AnsiColors.BgBlack; //TuiTheme.Current.WindowBg;
                // ? (sel ? highlightBg : 0)
                // : (sel ? highlightBg : (Bg > 0 ? Bg : TuiTheme.Current.WindowBg));

                // Bg>0 模式下全行填充
                if (!bordered)
                {
                    if (fillLeft < fillRight)
                    {
                        rb.Write(row, fillLeft, new string(' ', fillRight - fillLeft), itemFg, rowBg);
                    }
                }
                else if (sel)
                {
                    // 边框模式下选中行高亮填充（不含边框列）
                    rb.Write(row, absX + 1, new string(' ', Math.Max(0, Width - 2)), itemFg, rowBg);
                }

                // 左边框
                if (bordered)
                {
                    rb.Write(row, absX, bc.V, fg: borderFg, rowBg);
                }

                // 行背景填充（先于图标写入，避免把图标列擦成空格）
                if (fillLeft < fillRight)
                    rb.Write(row, fillLeft, strSpaces, bg: Bg > 0 ? Bg : rowBg);

                // 图标 + 标签 + 详情
                var col = absX + 1 + leftPad;
                var iconStr = item.Icon + " ";
                rb.Write(row, col, iconStr, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += AnsiHelper.DisplayWidth(iconStr);

                // 标签（截断）
                var detailW = string.IsNullOrEmpty(item.Detail)
                    ? 0
                    : AnsiHelper.DisplayWidth(item.Detail) + 3;

                var labelMax = Width - leftPad * 2 - (col - absX) - detailW - 2;
                var label = item.Label;

                if (AnsiHelper.DisplayWidth(label) > labelMax)
                    label = AnsiHelper.TruncateByWidth(label, labelMax);

                rb.Write(row, col, label, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += AnsiHelper.DisplayWidth(label);

                // 详情
                if (!string.IsNullOrEmpty(item.Detail))
                    rb.Write(row, col + 2, item.Detail,
                        fg: sel ? highlightFg : AnsiColors.BrightBlack,
                        bg: rowBg > 0 || !bordered ? rowBg : 0);

                // 右边框
                if (bordered)
                    rb.Write(row, absX + Width - 1, bc.V, fg: borderFg);
            }
            else
            {
                // 空白行
                if (!bordered)
                {
                    // var fillLeft = Math.Max(absX, ClipLeft);
                    // var fillRight = Math.Min(absX + Width, ClipRight);
                    // var strSpaces = new string(' ', fillRight - fillLeft);

                    // if (fillLeft < fillRight)
                    {
                        rb.Write(row, fillLeft, strSpaces, bg: Bg > 0 ? Bg : TuiTheme.Current.WindowBg);
                    }
                }
                else
                {
                    rb.Write(row, absX, bc.V, fg: borderFg);
                    rb.Write(row, absX + Width - 1, bc.V, fg: borderFg);
                }
            }

            sb.Append(rb.ToString());
        }

        // ── 下边框 / 分隔线（紧贴最后一条目）──
        if (bordered)
        {
            var botRow = contentStartY + visibleCount * ItemHeight;
            WriteBorder(sb, botRow, absX, bc.BL, bc.HB, bc.BR, Width, borderFg);
        }
        else
        {
            var sepRow = absY + visibleCount * ItemHeight;
            if (sepRow < ClipBottom)
            {
                var fillBg = Bg > 0 ? Bg : TuiTheme.Current.WindowBg;
                var sepRb = new RenderBuffer();
                sepRb.Write(sepRow, absX, new string('─', Width), fg: SeparatorColor, bg: fillBg);
                sb.Append(sepRb.ToString());
            }
        }
    }

    /// <summary>
    /// 渲染边框。
    /// 用于绘制提示栏的边框，支持自定义字符和颜色。
    /// </summary>
    /// <param name="sb">渲染缓冲区</param>
    /// <param name="row">行坐标</param>
    /// <param name="col">列坐标</param>
    /// <param name="left">左框字符</param>
    /// <param name="mid">中间框字符</param>
    /// <param name="right">右框字符</param>
    /// <param name="width">宽度</param>
    /// <param name="fg">前景颜色</param>
    private static void WriteBorder(StringBuilder sb, int row, int col,
        string left, string mid, string right, int width, int fg)
    {
        var rb = new RenderBuffer();
        rb.Write(row, col, left + new string(mid[0], Math.Max(0, width - 2)) + right, fg: fg);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 更新选中索引，确保在可见范围内。
    /// </summary>
    /// <param name="sel">当前选中索引</param>
    /// <returns>更新后的选中索引</returns>
    private void UpdateSelectedIndex(int sel)
    {
        if (sel < 0)
            return;

        if (sel >= Items.Count)
        {
            return;
        }

        if (sel < ViewIndex)
        {
            ViewIndex = sel;
        }

        if (sel > ViewIndex + MaxVisible - 1)
        {
            ViewIndex = sel - (MaxVisible - 1);
        }
    }

    /// <summary>
    /// 处理键盘输入。
    /// </summary>
    /// <param name="key">按下的键</param>
    /// <returns>是否处理了该键</returns>
    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !CanFocus) return false;
        if (Items.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex <= 0)
                {
                    return true;
                }

                SelectedIndex -= 1;
                UpdateSelectedIndex(SelectedIndex);
                return true;

            case ConsoleKey.DownArrow:
                if (SelectedIndex >= Items.Count - 1)
                {
                    return true;
                }

                SelectedIndex += 1;
                UpdateSelectedIndex(SelectedIndex);
                return true;

            case ConsoleKey.Home:
                SelectedIndex = 0;
                ViewIndex = 0;
                return true;

            case ConsoleKey.End:
                SelectedIndex = Items.Count - 1;
                UpdateSelectedIndex(SelectedIndex);
                return true;

            case ConsoleKey.Enter:
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    OnSelect?.Invoke(Items[SelectedIndex]);
                return true;
        }

        return false;
    }
}