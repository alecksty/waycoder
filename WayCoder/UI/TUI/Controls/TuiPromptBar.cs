using System.Text;
using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>提示条目的类型</summary>
public enum PromptKind
{
    Command,
    File,
    Shell,
    Slash,
    History,
    Recent
}

/// <summary>提示条目</summary>
public class PromptItem
{
    public PromptKind Kind { get; set; }
    public string Label { get; set; } = "";
    public string? Detail { get; set; }
    public string? Value { get; set; }

    /// <summary>
    /// 获取提示条目的图标。
    /// </summary>
    /// <returns>图标文本。</returns>
    public string Icon => Kind switch
    {
        PromptKind.Command => "⌘",
        PromptKind.File => "📄",
        PromptKind.Shell => "⚡",
        PromptKind.Slash => "/",
        PromptKind.History => "↺",
        PromptKind.Recent => "⏱",
        _ => "·",
    };
}

/// <summary>
/// 提示栏 —— 输入框上方可显示/隐藏的提示列表。
/// Bg==0 时绘制边框无底色；Bg>0 时全行填充底色 + 底部分隔线。
/// </summary>
public class TuiPromptBar : TuiControl
{
    public override bool CanFocus => true;

    /// <summary>提示条目列表</summary>
    public List<PromptItem> Items { get; set; } = [];

    /// <summary>当前高亮索引 (-1 = 无选中)</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>最大可见条目数</summary>
    public int MaxVisible { get; set; } = 8;

    /// <summary>空列表时的提示文本</summary>
    public string EmptyHint { get; set; } = "";

    /// <summary>选中回调</summary>
    public Action<PromptItem>? OnSelect { get; set; }

    /// <summary>列表项高度（行）</summary>
    public int ItemHeight { get; set; } = 1;

    /// <summary>边框/分隔线颜色</summary>
    public int SeparatorColor { get; set; } = TuiColors.BrightBlack;

    /// <summary>边框样式（Bg==0 时生效）</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

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
        bool bordered = Bg == 0;
        int visibleCount = Math.Min(Items.Count, MaxVisible);
        int fg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
        int borderFg = SeparatorColor;

        var bc = TuiHelper.GetBorderChars(BorderStyle);

        int highlightBg = bordered
            ? TuiTheme.Current.ControlFocusedBg > 0 ? TuiTheme.Current.ControlFocusedBg : TuiColors.BgBlue
            : TuiTheme.Current.ControlFocusedBg > 0
                ? TuiTheme.Current.ControlFocusedBg
                : TuiColors.BgBlue;
        int highlightFg = TuiColors.Black; // 亮底配黑字（原 BgWhite=背景码当前景，白字白底不可见）

        // ── 上边框 ──
        if (bordered)
            WriteBorder(sb, absY, absX, bc.TL, bc.HT, bc.TR, Width, borderFg);

        int contentStartY = bordered ? absY + 1 : absY;
        int leftPad = bordered ? 1 : 0; // 边框内缩

        // ── 列表行（只渲染实际条目，不预留空行；高度由 ShowPromptBar 按条目数设定）──
        for (int i = 0; i < visibleCount; i++)
        {
            int row = contentStartY + i * ItemHeight;
            if (row < ClipTop || row >= ClipBottom) continue;

            var rb = new RenderBuffer();
            bool hasItem = i < visibleCount;

            if (hasItem)
            {
                var item = Items[i];
                bool selected = i == SelectedIndex;

                int itemFg = selected ? highlightFg : fg;
                int rowBg = bordered
                    ? (selected ? highlightBg : 0)
                    : (selected ? highlightBg : (Bg > 0 ? Bg : TuiTheme.Current.WindowBg));

                // Bg>0 模式下全行填充
                if (!bordered)
                {
                    int fillLeft = Math.Max(absX, ClipLeft);
                    int fillRight = Math.Min(absX + Width, ClipRight);
                    if (fillLeft < fillRight)
                        rb.Write(row, fillLeft, new string(' ', fillRight - fillLeft), bg: rowBg);
                }
                else if (selected)
                {
                    // 边框模式下选中行高亮填充（不含边框列）
                    rb.Write(row, absX + 1, new string(' ', Math.Max(0, Width - 2)), bg: rowBg);
                }

                // 左边框
                if (bordered)
                    rb.Write(row, absX, bc.V, fg: borderFg);

                // 图标 + 标签 + 详情
                int col = absX + 1 + leftPad;
                var iconStr = item.Icon + " ";
                rb.Write(row, col, iconStr, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += TuiHelper.DisplayWidth(iconStr);

                // 标签（截断）
                int detailW = string.IsNullOrEmpty(item.Detail)
                    ? 0
                    : TuiHelper.DisplayWidth(item.Detail) + 3;
                // 钳到 ≥1，避免窄宽度/长详情时负值截断崩溃
                int labelMax = Math.Max(1, Width - leftPad * 2 - (col - absX) - detailW - 2);
                var label = item.Label;
                if (TuiHelper.DisplayWidth(label) > labelMax)
                    label = TuiHelper.TruncateByWidth(label, labelMax);
                rb.Write(row, col, label, fg: itemFg, bg: rowBg > 0 || !bordered ? rowBg : 0);
                col += TuiHelper.DisplayWidth(label);

                // 详情（按剩余宽度截断，避免溢出右边界）
                if (!string.IsNullOrEmpty(item.Detail))
                {
                    var detailText = item.Detail;
                    int detailMax = Math.Max(1, Width - (col - absX) - 3);
                    if (TuiHelper.DisplayWidth(detailText) > detailMax)
                        detailText = TuiHelper.TruncateByWidth(detailText, detailMax);
                    rb.Write(row, col + 2, detailText,
                        fg: selected ? highlightFg : TuiColors.BrightBlack,
                        bg: rowBg > 0 || !bordered ? rowBg : 0);
                }

                // 右边框
                if (bordered)
                    rb.Write(row, absX + Width - 1, bc.V, fg: borderFg);
            }
            else
            {
                // 空白行
                if (!bordered)
                {
                    int fillLeft = Math.Max(absX, ClipLeft);
                    int fillRight = Math.Min(absX + Width, ClipRight);
                    if (fillLeft < fillRight)
                        rb.Write(row, fillLeft, new string(' ', fillRight - fillLeft),
                            bg: Bg > 0 ? Bg : TuiTheme.Current.WindowBg);
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
            int botRow = contentStartY + visibleCount * ItemHeight;
            WriteBorder(sb, botRow, absX, bc.BL, bc.HB, bc.BR, Width, borderFg);
        }
        else
        {
            int sepRow = absY + visibleCount * ItemHeight;
            if (sepRow < ClipBottom)
            {
                int fillBg = Bg > 0 ? Bg : TuiTheme.Current.WindowBg;
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
                SelectedIndex = SelectedIndex <= 0 ? Items.Count - 1 : SelectedIndex - 1;
                return true;
            case ConsoleKey.DownArrow:
                SelectedIndex = SelectedIndex >= Items.Count - 1 ? 0 : SelectedIndex + 1;
                return true;
            case ConsoleKey.Home:
                SelectedIndex = 0;
                return true;
            case ConsoleKey.End:
                SelectedIndex = Items.Count - 1;
                return true;
            case ConsoleKey.Enter:
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    OnSelect?.Invoke(Items[SelectedIndex]);
                return true;
        }

        return false;
    }
}