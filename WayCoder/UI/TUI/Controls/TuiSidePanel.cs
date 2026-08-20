using System.Text;
using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 侧栏面板 —— 多区域同时显示信息。
/// 布局：左边框竖线 + 多个分区（标题 + 分隔线 + 内容），垂直堆叠。
/// 颜色由主题 WindowBg / ControlBg 控制。
/// </summary>
public class TuiSidePanel : TuiControl
{
    public override bool CanFocus => false;

    /// <summary>面板分区列表</summary>
    public List<PanelSection> Sections { get; set; } = [];

    /// <summary>是否可见</summary>
    public bool PanelVisible { get; set; } = true;

    /// <summary>左边框宽度（列数）</summary>
    public int BorderWidth { get; set; } = 1;

    /// <summary>左边框颜色</summary>
    public int BorderColor { get; set; } = AnsiColors.BrightBlack;

    /// <summary>边框样式（控制竖线/分隔线字符）</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

    /// <summary>分区标题颜色</summary>
    public int SectionHeaderFg { get; set; } = AnsiColors.BrightBlack; // 分区标题：灰色（不抢眼）

    /// <summary>分隔线颜色</summary>
    public int SeparatorColor { get; set; } = AnsiColors.BrightBlack;

    // 曾有个 ScrollOffset 属性：面板 CanFocus=false、没有滚动键位，全项目没有一处写它，
    // 恒为 0 —— 渲染里那段滚动裁剪是死代码。改由 AllocateHeights 按高度分配，删掉不留假接口。

    public TuiSidePanel()
    {
        Width = 30; Height = 20;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (!PanelVisible) return;

        int contentW = Width - BorderWidth;
        if (contentW <= 0) return;

        var bc = AnsiHelper.GetBorderChars(BorderStyle);
        int bg = Bg > 0 ? Bg : TuiTheme.Current.TerminalBg;
        int fg = Fg > 0 ? Fg : AnsiColors.BrightBlack; // 内容行：灰色
        int contentBg = 0; // 内容（标题/分隔线/行）透明背景，只显示文字，不铺色块
        int row = absY;

        int borderCol = absX;
        int contentX = absX + BorderWidth;

        // ── 左边框竖线 ──
        for (int r = 0; r < Height; r++)
        {
            int screenRow = row + r;
            if (screenRow < ClipTop || screenRow >= ClipBottom) continue;
            if (borderCol < ClipLeft || borderCol >= ClipRight) continue;
            var rb = new RenderBuffer();
            rb.Write(screenRow, borderCol, bc.V, fg: BorderColor, bg: contentBg);
            sb.Append(rb.ToString());
        }

        // ── 内容区 ──
        // 每个分区能显示几行内容，由 AllocateHeights 按当前可用高度分配：
        // 数据变多时分区就地向下扩张，扩到面板底就不再扩，多出来的折成「… +N」
        var quotas = AllocateHeights(Sections, Height);
        int curRow = row;
        int qi = 0;

        foreach (var sec in Sections)
        {
            if (sec.Collapsed) continue;
            int quota = qi < quotas.Count ? quotas[qi] : 0;
            qi++;
            if (quota < 0) break;                 // 高度已耗尽，后面的分区一行都放不下
            if (curRow >= row + Height) break;

            curRow++; // section 上方空行（两边间隔）
            // ── 标题行 ──
            if (curRow >= ClipTop && curRow < ClipBottom)
            {
                var titleRb = new RenderBuffer();
                string title = AnsiHelper.DisplayWidth(sec.Title) > contentW - 2
                    ? AnsiHelper.TruncateByWidth(sec.Title, contentW - 2)
                    : sec.Title;
                // 名称 + 横线到边（少一格）：名称(num)────…… 横线长度随内容区宽，动数据不错位
                int titleW = AnsiHelper.DisplayWidth(title);
                int sepLen = Math.Max(1, contentW - 2 - titleW);
                titleRb.Write(curRow, contentX, " " + title + new string(bc.H[0], sepLen), fg: SectionHeaderFg, bg: contentBg);
                sb.Append(titleRb.ToString());
            }
            curRow++; // 标题行
            curRow++; // section 下方空行（两边间隔）

            // ── 内容行 ──
            bool clipped = quota < sec.Lines.Count;
            int shown = clipped ? Math.Max(0, quota - 1) : sec.Lines.Count;  // 留一行给「… +N」
            for (int i = 0; i < shown && curRow < row + Height; i++)
            {
                if (curRow >= ClipTop && curRow < ClipBottom)
                    WriteContentLine(sb, curRow, contentX, contentW, sec.Lines[i], fg, contentBg);
                curRow++;
            }
            // 配额 0（高度耗尽）时不再显示「… 还有 N 条」——空分区（如 Todo (0)）只留标题即可，
            // 避免「0 项却显示 … 还有 1 条」的误导截断
            if (clipped && quota > 0 && curRow < row + Height)
            {
                if (curRow >= ClipTop && curRow < ClipBottom)
                    WriteContentLine(sb, curRow, contentX, contentW,
                        $"  … 还有 {sec.Lines.Count - shown} 条", SeparatorColor, contentBg);
                curRow++;
            }
        }
    }

    private static void WriteContentLine(StringBuilder sb, int rowY, int contentX, int contentW,
        string line, int fg, int bg)
    {
        int maxVw = contentW - 2;
        if (AnsiHelper.DisplayWidth(line) > maxVw)
            line = AnsiHelper.TruncateByWidth(line, maxVw);
        var rb = new RenderBuffer();
        rb.Write(rowY, contentX + 1, line, fg: fg, bg: bg);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 给每个未折叠分区分配「内容行」配额（不含标题行与分隔线这 2 行开销）。
    /// 返回值与未折叠分区一一对应；<c>-1</c> 表示高度已耗尽、该分区整个放不下。
    ///
    /// 规则（对应「位置满了会往下扩张，除非扩张不动了」）：
    ///   1. 总高度够 → 每个分区全量显示，内容多的自然向下挤占空白；
    ///   2. 不够 → 先保证靠前的分区各拿到「2 行开销 + 至少 1 行内容」，装不下的分区整个丢弃；
    ///   3. 剩余行数按需均分，谁要得少谁先拿满，省下的再轮给还没喂饱的（不浪费一行）。
    /// 纯函数，便于自测直接断言分配结果。
    /// </summary>
    public static List<int> AllocateHeights(IReadOnlyList<PanelSection> sections, int height)
    {
        const int Overhead = 3;   // section 上间隔 + 标题横线行 + 下间隔
        var visible = new List<int>();          // 各分区内容行需求
        foreach (var s in sections)
            if (!s.Collapsed) visible.Add(s.Lines.Count);

        var quota = new List<int>(new int[visible.Count]);
        if (visible.Count == 0 || height <= 0) return quota;

        // 1) 高度够摆下全部：直接全量
        int need = 0;
        foreach (var n in visible) need += Overhead + n;
        if (need <= height)
        {
            for (int i = 0; i < visible.Count; i++) quota[i] = visible[i];
            return quota;
        }

        // 2) 从前往后收纳，每个分区至少 2 行开销 + 1 行内容；放不下的标 -1
        int budget = height, kept = 0;
        for (int i = 0; i < visible.Count; i++)
        {
            if (budget >= Overhead + 1)
            {
                budget -= Overhead;
                quota[i] = 0;
                kept++;
            }
            else
            {
                quota[i] = -1;
            }
        }

        // 3) 剩余行均分：要得少的先拿满，余出来的轮着补给还差的，直到发完或都喂饱
        while (budget > 0 && kept > 0)
        {
            bool progressed = false;
            int share = Math.Max(1, budget / kept);
            for (int i = 0; i < visible.Count && budget > 0; i++)
            {
                if (quota[i] < 0 || quota[i] >= visible[i]) continue;
                int give = Math.Min(share, Math.Min(budget, visible[i] - quota[i]));
                if (give <= 0) continue;
                quota[i] += give;
                budget -= give;
                progressed = true;
            }
            if (!progressed) break;   // 全部喂饱，剩余高度用不掉
        }
        return quota;
    }
}

/// <summary>
/// 侧栏分区 —— 一个可折叠的信息区块。
/// </summary>
public class PanelSection
{
    /// <summary>分区标题（如 "📋 Todo"）</summary>
    public string Title { get; set; } = "";

    /// <summary>内容行列表</summary>
    public List<string> Lines { get; set; } = [];

    /// <summary>是否折叠</summary>
    public bool Collapsed { get; set; }
}
