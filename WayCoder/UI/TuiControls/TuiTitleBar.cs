using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 标题栏控件 —— 顶行应用标识。
/// 渲染：应用名 + 模型名 + Git 分支，右对齐版本号。
/// 颜色由主题 StatusBarFg/StatusBarBg 控制。
/// </summary>
public class TuiTitleBar : TuiControl
{
    public override bool CanFocus => false;

    /// <summary>应用名/模型名</summary>
    public string Title { get; set; } = "";

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
        int fg = TuiColors.Black; // 金色底用黑字
        int row = absY;

        // 1. 整行渐变背景填充
        ControlRenderer.DrawGradientBarFill(sb, row, absX, Width, gs, ge);

        // 2. 左侧：应用名
        var title = Title.Length > 0 ? Title : Global.AppFullName;
        ControlRenderer.WriteGradientTextAt(sb, row, absX + 1, title,
            fg, gs, ge, absX, Width);

        int col = absX + 1 + TuiHelper.DisplayWidth(title);

        // Git 分支
        if (!string.IsNullOrEmpty(GitBranch))
        {
            var gitText = $"  🌿 {GitBranch}";
            ControlRenderer.WriteGradientTextAt(sb, row, col, gitText,
                fg, gs, ge, absX, Width);
            col += TuiHelper.DisplayWidth(gitText);
        }

        // 3. 右侧：版本号
        if (!string.IsNullOrEmpty(Version))
        {
            int vw = TuiHelper.DisplayWidth(Version);
            int rightCol = absX + Width - vw - 1;
            if (rightCol > col)
                ControlRenderer.WriteGradientTextAt(sb, row, rightCol, Version,
                    fg, gs, ge, absX, Width);
        }
    }
}