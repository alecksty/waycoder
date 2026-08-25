using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

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
        // 实际可见行数 = 高度减去边框/分隔线后能容纳的行数，受 MaxVisible 封顶。
        // 不能用 MaxVisible 硬算：ShowPromptBar 把 Height 设为「条目数+边框」，条目少时按
        // MaxVisible(8) 渲染会把空行和底边框画到控件下方（盖住动态栏/分隔线）造成花屏。
        var visibleCount = Math.Min(MaxVisible, Math.Max(0, Height - (bordered ? 2 : 1)));

        var fg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
        var borderFg = SeparatorColor;
        var bc = AnsiHelper.GetBorderChars(BorderStyle);

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

        // ── 对齐列：详情统一从「图标+标签」最宽处后的固定列开始，避免长短不齐 ──
        var labelStartCol = absX + 1 + leftPad;
        var contentMaxCol = absX + Width - 1; // 右框内缘列
        var maxPrefixVW = 0;
        for (var ai = 0; ai < Items.Count; ai++)
        {
            var it = Items[ai];
            var pv = AnsiHelper.DisplayWidth(it.Icon + " ") + AnsiHelper.DisplayWidth(it.Label);
            if (pv > maxPrefixVW) maxPrefixVW = pv;
        }
        // 至少留 3 列给 " 详情"，防止对齐把详情挤出右框
        var maxPrefixAllowed = Math.Max(0, contentMaxCol - labelStartCol - 3);
        if (maxPrefixVW > maxPrefixAllowed) maxPrefixVW = maxPrefixAllowed;

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

                // 图标 + 标签（补位对齐）+ 详情
                var col = labelStartCol;
                var iconStr = item.Icon + " ";
                var iconVW = AnsiHelper.DisplayWidth(iconStr);
                rb.Write(row, col, iconStr, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += iconVW;

                // 标签：本行占宽 = 对齐宽 - 图标宽；超出截断
                var label = item.Label;
                var labelColW = Math.Max(0, maxPrefixVW - iconVW);
                if (AnsiHelper.DisplayWidth(label) > labelColW)
                    label = AnsiHelper.TruncateByWidth(label, labelColW);

                rb.Write(row, col, label, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += AnsiHelper.DisplayWidth(label);

                // 补空格到对齐列（让详情从固定列开始）
                var padW = labelColW - AnsiHelper.DisplayWidth(label);
                if (padW > 0)
                    rb.Write(row, col, new string(' ', padW), bg: rowBg > 0 || !bordered ? rowBg : 0);

                // 详情（固定列对齐，超宽截断防挤出右框）
                // 选中行详情用与标签相同的蓝色：此前用 ControlFocusedFg（默认黑）配黑底 = 黑字黑底隐形，
                // 用户反馈「光标处说明不见了」——选中时文字必须可见。
                if (!string.IsNullOrEmpty(item.Detail))
                    rb.WriteTruncate(row, labelStartCol + maxPrefixVW + 2, item.Detail,
                        contentMaxCol,
                        fg: sel ? itemFg : AnsiColors.BrightBlack,
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
    public override bool OnMouse(InputEvent ev)
    {
        if (!MouseInBounds(ev, out _, out int relY)) return false;
        if (Items.Count == 0) return false;

        var bordered = Bg == 0;
        int contentStart = bordered ? 1 : 0; // 有边框时内容区从 absY+1 开始

        // 滚轮：上下选择（与 Up/Down 同语义）
        if (ev.MouseScrollUp)
        {
            Focused = true;
            SelectedIndex = Math.Max(0, SelectedIndex - 1);
            UpdateSelectedIndex(SelectedIndex);
            MarkDirty();
            return true;
        }
        if (ev.MouseScrollDown)
        {
            Focused = true;
            SelectedIndex = Math.Min(Items.Count - 1, SelectedIndex + 1);
            UpdateSelectedIndex(SelectedIndex);
            MarkDirty();
            return true;
        }
        if (!ev.MouseLeft) return false;

        int relRow = relY - contentStart;
        if (relRow < 0) return false; // 上边框
        int i = relRow / ItemHeight;
        int pos = ViewIndex + i;
        if (pos < 0 || pos >= Items.Count) return false;

        Focused = true;
        SelectedIndex = pos;
        UpdateSelectedIndex(pos);
        OnSelect?.Invoke(Items[pos]); // 点击 = 选中并激活（对齐 Enter 语义）
        MarkDirty();
        return true;
    }

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