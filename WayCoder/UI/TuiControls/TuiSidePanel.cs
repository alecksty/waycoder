using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

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
    public int BorderColor { get; set; } = TuiColors.BrightBlack;

    /// <summary>边框样式（控制竖线/分隔线字符）</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

    /// <summary>分区标题颜色</summary>
    public int SectionHeaderFg { get; set; } = TuiColors.Cyan;

    /// <summary>分隔线颜色</summary>
    public int SeparatorColor { get; set; } = TuiColors.BrightBlack;

    /// <summary>内容垂直滚动偏移</summary>
    public int ScrollOffset { get; set; }

    public TuiSidePanel()
    {
        Width = 30; Height = 20;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (!PanelVisible) return;

        int contentW = Width - BorderWidth;
        if (contentW <= 0) return;

        var bc = TuiHelper.GetBorderChars(BorderStyle);
        int bg = Bg > 0 ? Bg : TuiTheme.Current.WindowBg;
        int fg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
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
            rb.Write(screenRow, borderCol, bc.V, fg: BorderColor, bg: bg);
            sb.Append(rb.ToString());
        }

        // ── 内容区 ──
        // 计算所有分区需要的总行数
        int totalLines = 0;
        foreach (var sec in Sections)
        {
            if (sec.Collapsed) continue;
            totalLines += 1; // 标题行
            totalLines += sec.Lines.Count;
        }

        // 裁剪滚动
        int startLine = Math.Max(0, ScrollOffset);
        int renderedLines = 0;
        int curRow = row;

        foreach (var sec in Sections)
        {
            if (sec.Collapsed) continue;
            if (curRow >= row + Height) break;

            int sectionStart = renderedLines;
            int sectionEnd = renderedLines + 1 + sec.Lines.Count;

            // 计算该分区在当前滚动窗口内的可见行
            if (sectionEnd <= startLine)
            {
                renderedLines = sectionEnd;
                continue;
            }

            int visibleOffset = Math.Max(0, startLine - sectionStart);

            // ── 标题行 ──
            if (visibleOffset == 0 && curRow < row + Height)
            {
                if (curRow >= ClipTop && curRow < ClipBottom)
                {
                    var titleRb = new RenderBuffer();
                    string title = sec.Title.Length > contentW - 2
                        ? TuiHelper.TruncateByWidth(sec.Title, contentW - 2)
                        : sec.Title;
                    titleRb.Write(curRow, contentX, " " + title, fg: SectionHeaderFg, bg: bg);
                    sb.Append(titleRb.ToString());
                }
                curRow++;
            }

            // ── 分隔线（标题下方）──
            if (visibleOffset <= 1 && curRow < row + Height)
            {
                if (curRow >= ClipTop && curRow < ClipBottom)
                {
                    var sepRb = new RenderBuffer();
                    int sepLen = Math.Min(contentW - 1, 20);
                    sepRb.Write(curRow, contentX + 1, new string(bc.H[0], sepLen), fg: SeparatorColor, bg: bg);
                    sb.Append(sepRb.ToString());
                }
                curRow++;
                visibleOffset = Math.Max(0, visibleOffset - 2);
            }
            else
            {
                visibleOffset = Math.Max(0, visibleOffset - 2);
            }

            // ── 内容行 ──
            for (int i = visibleOffset; i < sec.Lines.Count && curRow < row + Height; i++)
            {
                if (curRow >= ClipTop && curRow < ClipBottom)
                {
                    var line = sec.Lines[i];
                    int maxVw = contentW - 2;
                    if (TuiHelper.DisplayWidth(line) > maxVw)
                        line = TuiHelper.TruncateByWidth(line, maxVw);
                    var lineRb = new RenderBuffer();
                    lineRb.Write(curRow, contentX + 1, line, fg: fg, bg: bg);
                    sb.Append(lineRb.ToString());
                }
                curRow++;
            }

            renderedLines = sectionEnd;
        }
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
