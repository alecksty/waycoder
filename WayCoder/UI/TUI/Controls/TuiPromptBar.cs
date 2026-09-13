using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 提示栏 —— 输入框上方可显示/隐藏的提示列表。
/// Bg==0 时绘制边框无底色；Bg>0 时全行填充底色 + 底部分隔线。
/// </summary>
public class TuiPromptBar : TuiBorderedControl
{
    #region 属性

    // 可聚焦输入栏：覆写基类（TuiDisplayControl）的 CanFocus=false 恢复聚焦
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

    /// <summary>
    /// 选中行的前景色。默认 <see cref="AnsiColors.BgBrightBlue"/> 是**历史值**——
    /// 早先把背景色常量当前景色用（亮蓝字配黑底），改默认值会影响既有 `PromptBar` 观感，故保留。
    /// 行内选择（<see cref="EPromptKind.Choice"/>）传黑字配黄底。
    /// </summary>
    public int HighlightFg { get; set; } = AnsiColors.BgBrightBlue;

    /// <summary>选中行的底色（0 = 不填充，即历史行为）。行内选择传 <see cref="AnsiColors.BgYellow"/>。</summary>
    public int HighlightBg { get; set; } = 0;

    /// <summary>选中行是否加 <c>❯ </c> 箭头指示（非选中行补等宽空格对齐）。</summary>
    public bool ShowArrow { get; set; } = false;

    /// <summary>多选模式：每项前缀 <c>[x] </c>/<c>[ ] </c> 勾选框，Space 切换（Enter 仍是提交）。</summary>
    public bool MultiSelect { get; set; } = false;

    /// <summary>
    /// 页头文本（多页问卷的横向标签行 / 分步骤的「步骤 k/n」），非空时占 1 行、排在选项之上。
    /// 由调用方负责把它算进 <see cref="Height"/>（本控件只按它少渲染一行选项）。
    /// </summary>
    public string HeaderText { get; set; } = "";

    /// <summary>页头前景色</summary>
    public int HeaderFg { get; set; } = AnsiColors.BrightYellow;

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
        // 页头占 1 行（多页问卷的标签行 / 步骤行），选项行数相应减 1
        var headerRows = string.IsNullOrEmpty(HeaderText) ? 0 : 1;
        // 实际可见行数 = 高度减去边框/分隔线后能容纳的行数，受 MaxVisible 封顶。
        // 不能用 MaxVisible 硬算：ShowPromptBar 把 Height 设为「条目数+边框」，条目少时按
        // MaxVisible(8) 渲染会把空行和底边框画到控件下方（盖住动态栏/分隔线）造成花屏。
        var visibleCount = Math.Min(MaxVisible, Math.Max(0, Height - headerRows - (bordered ? 2 : 1)));

        var fg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
        var borderFg = SeparatorColor;
        var bc = GetBorderChars();

        // ── 上边框 ──
        if (bordered)
        {
            WriteBorder(sb, absY, absX, bc.TL, bc.HT, bc.TR, Width, borderFg);
        }

        var contentStartY = (bordered ? absY + 1 : absY) + headerRows; // 页头行之下才是选项
        var leftPad = bordered ? 1 : 0; // 边框内缩

        var fillLeft = Math.Max(absX + 1, ClipLeft);
        // 右界取「右框内侧列」且**含**它：只填到 Width-3 会漏掉 Width-2 那一列，
        // 旧帧在那里写过分隔线「─」时新帧不覆盖 → 框内右下永久留半截横线（--keypad 帧可见）。
        var fillRight = Math.Min(absX + Width - 2, ClipRight);
        var strSpaces = new string(' ', Math.Max(0, fillRight - fillLeft + 1)); // 负值（左缘被裁过右缘）防崩溃

        // ── 箭头列（行内选择用）：选中行 ❯，非选中行补等宽空格保持后续列对齐 ──
        var arrowCol = absX + 1 + leftPad;
        var arrowW = ShowArrow ? AnsiHelper.DisplayWidth("❯") + 1 : 0; // +1 = 箭头后的空格

        // ── 对齐列：详情统一从「图标+标签」最宽处后的固定列开始，避免长短不齐 ──
        var labelStartCol = absX + 1 + leftPad + arrowW;
        var contentMaxCol = absX + Width - 1; // 右框内缘列
        var maxPrefixVW = 0;
        for (var ai = 0; ai < Items.Count; ai++)
        {
            var it = Items[ai];
            var pv = AnsiHelper.DisplayWidth(PrefixOf(it)) + AnsiHelper.DisplayWidth(it.Label);
            if (pv > maxPrefixVW) maxPrefixVW = pv;
        }
        // 至少留 3 列给 " 详情"，防止对齐把详情挤出右框
        var maxPrefixAllowed = Math.Max(0, contentMaxCol - labelStartCol - 3);
        if (maxPrefixVW > maxPrefixAllowed) maxPrefixVW = maxPrefixAllowed;

        // ── 页头（多页问卷的标签行 / 分步骤的「步骤 k/n」）──
        // 必须与普通内容行**同构**：左右边框 + 内容区整行填充 + 文本。
        // 只写文本不填充会让上一帧留在该行的内容露出来（典型是动态栏直写留下的 spinner 与
        // 百分比粘在页头行上，--keypad 帧可见）；不写边框则整行像个豁口。
        if (headerRows > 0)
        {
            var headRow = contentStartY - 1;
            var maxW = Math.Max(1, contentMaxCol - labelStartCol);
            var htext = AnsiHelper.DisplayWidth(HeaderText) > maxW
                ? AnsiHelper.TruncateByWidth(HeaderText, maxW)
                : HeaderText;
            var hbBg = Bg > 0 ? Bg : AnsiColors.BgBlack;
            var hb = new RenderBuffer();
            if (bordered) hb.Write(headRow, absX, bc.V, fg: borderFg, bg: hbBg);
            if (strSpaces.Length > 0) hb.Write(headRow, fillLeft, strSpaces, bg: hbBg);
            hb.Write(headRow, labelStartCol - arrowW, htext, fg: HeaderFg, bg: hbBg);
            if (bordered) hb.Write(headRow, absX + Width - 1, bc.V, fg: borderFg, bg: hbBg);
            sb.Append(hb.ToString());
        }

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
                var itemFg = sel ? HighlightFg : fg;

                // 选中行底色：显式配置优先，未配置则保持历史行为（恒黑底 + 亮色前景表选中）
                var rowBg = sel && HighlightBg > 0 ? HighlightBg : AnsiColors.BgBlack;
                // ? (sel ? highlightBg : 0)
                // : (sel ? highlightBg : (Bg > 0 ? Bg : TuiTheme.Current.WindowBg));

                // Bg>0 模式下全行填充
                if (!bordered)
                {
                    if (strSpaces.Length > 0)
                    {
                        rb.Write(row, fillLeft, strSpaces, itemFg, rowBg);
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
                if (strSpaces.Length > 0)
                    rb.Write(row, fillLeft, strSpaces, bg: Bg > 0 ? Bg : rowBg);

                // 箭头（必须写在背景填充**之后**：填充从 fillLeft 起笔，会把这列擦成空格）
                if (ShowArrow)
                    rb.Write(row, arrowCol, sel ? "❯ " : new string(' ', arrowW),
                        fg: itemFg, bg: rowBg);

                // 图标 + 标签（补位对齐）+ 详情
                var col = labelStartCol;
                var iconStr = PrefixOf(item);
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
            var sepRow = contentStartY + visibleCount * ItemHeight;
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
    /// 条目在「图标位」实际渲染的前缀：多选模式是勾选框，否则是图标+空格。
    /// 渲染与对齐列计算必须共用同一份 —— 各写一遍就会错列（详情列左右横跳）。
    /// </summary>
    private string PrefixOf(PromptItem it)
        => MultiSelect ? (it.Checked ? "[x] " : "[ ] ") : it.Icon + " ";

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

            // 多选：Space 切换勾选（Enter 仍是提交，避免「按回车只勾了一项」）
            case ConsoleKey.Spacebar:
                if (MultiSelect && SelectedIndex >= 0 && SelectedIndex < Items.Count)
                {
                    Items[SelectedIndex].Checked = !Items[SelectedIndex].Checked;
                    return true;
                }

                return false;

            case ConsoleKey.Enter:
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    OnSelect?.Invoke(Items[SelectedIndex]);
                return true;
        }

        return false;
    }
}