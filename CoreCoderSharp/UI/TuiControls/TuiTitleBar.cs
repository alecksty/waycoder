using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.TuiControls;

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
    /// 渲染标题栏
    /// </summary>
    /// <param name="sb">输出缓冲区</param>
    /// <param name="absX">绝对列坐标</param>
    /// <param name="absY">绝对行坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var fg = TuiTheme.Current.StatusBarFg;
        var bg = TuiTheme.Current.StatusBarBg;
        var row = absY;
        var rb = new RenderBuffer();

        // ── 左侧：应用名 ──
        var title = Title.Length > 0 ? Title : Global.AppFullName;
        rb.Write(row, absX, $" {title}", fg: fg, bg: bg);

        int col = absX + 1 + TuiHelper.DisplayWidth(title);

        // Git 分支
        if (!string.IsNullOrEmpty(GitBranch))
        {
            var gitText = $"  🌿 {GitBranch}";
            rb.Write(row, col, gitText, fg: fg, bg: bg);
            col += TuiHelper.DisplayWidth(gitText);
        }

        // ── 右侧：版本号 ──
        if (!string.IsNullOrEmpty(Version))
        {
            int vw = TuiHelper.DisplayWidth(Version);
            int rightCol = absX + Width - vw - 1;
            if (rightCol > col)
                rb.Write(row, rightCol, Version, fg: fg, bg: bg);
        }

        sb.Append(rb.ToString());
    }
}