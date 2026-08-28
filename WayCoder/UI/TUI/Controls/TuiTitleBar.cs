using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 标题栏控件 —— 顶行应用标识。
/// 渲染：左侧 App/模型 + 居中标题 + 右侧版本号。
/// 颜色由主题 StatusBarFg/StatusBarBg 控制。
/// </summary>
public class TuiTitleBar : TuiDisplayControl
{

    /// <summary>应用名/模型名</summary>
    public string Title { get; set; } = "";

    /// <summary>居中文本（如 "💬 智能体 1 · 🔨 建造模式"）</summary>
    public string CenterText { get; set; } = "";

    /// <summary>Git 分支名（null/空=不显示）</summary>
    public string? GitBranch { get; set; }

    /// <summary>版本号（右侧）</summary>
    public string Version { get; set; } = "";

    public TuiTitleBar()
    {
        Height = 1;
    }

    /// <summary>
    /// 渲染标题栏（金色渐变背景）
    /// </summary>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;
        var (gs, ge) = t.GradTitleBar;
        int fg = AnsiColors.Black; // 金色底用黑字
        int row = absY;

        // 1. 整行渐变背景填充
        ControlRenderer.DrawGradientBarFill(sb, row, absX, Width, gs, ge);

        // 2. 左侧：应用名
        var title = Title.Length > 0 ? Title : Global.AppFullName;
        ControlRenderer.WriteGradientTextAt(sb, row, absX + 1, title,
            fg, gs, ge, absX, Width);

        // 3. 居中文本（在左右之间居中显示）
        if (!string.IsNullOrEmpty(CenterText))
        {
            int cw = AnsiHelper.DisplayWidth(CenterText);
            int centerCol = absX + (Width - cw) / 2;
            // 确保不覆盖左侧标题
            int minCenter = absX + 1 + AnsiHelper.DisplayWidth(title) + 2;
            if (centerCol < minCenter) centerCol = minCenter;
            // 确保不覆盖右侧版本号
            int vw = string.IsNullOrEmpty(Version) ? 0 : AnsiHelper.DisplayWidth(Version) + 1;
            int maxCenter = absX + Width - vw - cw - 2;
            if (centerCol + cw > maxCenter) centerCol = Math.Max(minCenter, maxCenter - cw);
            if (centerCol >= minCenter)
                ControlRenderer.WriteGradientTextAt(sb, row, centerCol, CenterText,
                    fg, gs, ge, absX, Width);
        }

        // 4. 右侧：版本号
        if (!string.IsNullOrEmpty(Version))
        {
            int vw = AnsiHelper.DisplayWidth(Version);
            int rightCol = absX + Width - vw - 1;
            ControlRenderer.WriteGradientTextAt(sb, row, rightCol, Version,
                fg, gs, ge, absX, Width);
        }
    }
}