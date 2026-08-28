using System.Text;
using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 面板 —— 带边框 + 标题的嵌入式容器。
/// 与 TuiWindow 不同，Panel 是控件（TuiView 子类），可放入控件树任意位置。
/// 子控件渲染在边框内部（含标题栏）。
/// </summary>
public class TuiPanel : TuiView
{
    /// <summary>面板标题（空=无标题栏）</summary>
    public string Title { get; set; } = "";

    /// <summary>边框颜色（ANSI 色码）</summary>
    public int BorderColor { get; set; }

    public TuiPanel()
    {
        BorderColor = TuiTheme.Current.WindowBorderFocused;
    }

    /// <summary>标题前景色（0=使用边框色）</summary>
    public int TitleFg { get; set; }

    /// <summary>边框样式</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

    /// <summary>面板内边距（边框到内容的间距）</summary>
    public int PaddingTop { get; set; }
    public int PaddingLeft { get; set; } = 1;

    /// <summary>内容区域最小尺寸</summary>
    public int MinContentW { get; set; } = 10;
    public int MinContentH { get; set; } = 2;

    // ── 布局 ──

    public override void Layout()
    {
        // 子控件在内边距区域排列
        int contentX = 1 + PaddingLeft; // 左边框 = 1
        int contentY = (HasTitle ? 1 : 0) + PaddingTop;   // 标题行 = 1
        int contentW = Math.Max(MinContentW, Width - 2 - PaddingLeft * 2);
        int contentH = Math.Max(MinContentH, Height - (HasTitle ? 2 : 1) - PaddingTop);

        // 递归布局子控件
        foreach (var child in Children)
        {
            child.X = contentX;
            child.Y = contentY;
            if (child.Width > contentW) child.Width = contentW;
            if (child is TuiView childView)
                childView.Layout();
        }
    }

    private bool HasTitle => !string.IsNullOrEmpty(Title);

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int bc = BorderColor > 0 ? BorderColor : TuiTheme.Current.WindowBorderFocused;
        int tf = TitleFg > 0 ? TitleFg : bc;
        int w = Width;
        int h = Height;

        // 边框字符统一走 AnsiHelper（此前这里各自重写了一份 switch，缺 Solid/Dotted 等新样式）
        var bch = AnsiHelper.GetBorderChars(BorderStyle);
        string tl = bch.TL, tr = bch.TR, bl = bch.BL, br = bch.BR, hh = bch.H, vv = bch.V;

        // 背景填充
        if (Bg > 0)
        {
            for (int r = 0; r < h; r++)
            {
                int row = absY + r;
                if (row < ClipTop || row >= ClipBottom) continue;
                var rb = new RenderBuffer();
                rb.Write(row, absX, new string(' ', w), bg: Bg);
                sb.Append(rb.ToString());
            }
        }

        // ── 上边框 + 标题 ──
        WriteAt(sb, absY, absX, tl, bc, Bg);
        if (HasTitle)
        {
            WriteAt(sb, absY, absX + 1, $" {Title} ", tf, Bg);
            var rem = w - 2 - AnsiHelper.DisplayWidth($" {Title} ");
            if (rem > 0) WriteAt(sb, absY, absX + 1 + AnsiHelper.DisplayWidth($" {Title} "), !string.IsNullOrEmpty(hh) ? hh[..Math.Min(1, hh.Length)] : "─", bc, Bg);
        }
        else
        {
            WriteAt(sb, absY, absX + 1, new string(hh.Length > 0 ? hh[0] : '─', w - 2), bc, Bg);
        }
        WriteAt(sb, absY, absX + w - 1, tr, bc, Bg);

        int contentTop = HasTitle ? absY + 1 : absY;

        // 标题栏下方不再画分隔线，内容直接从标题行下开始

        // ── 竖边框 ──
        for (int i = 0; i < Math.Max(0, HasTitle ? h - 2 : h - 1); i++)
        {
            int row = contentTop + i;
            WriteAt(sb, row, absX, vv, bc, Bg);
            WriteAt(sb, row, absX + w - 1, vv, bc, Bg);
        }

        // ── 底边框 ──
        if (h > 1)
        {
            WriteAt(sb, absY + h - 1, absX, bl, bc, Bg);
            WriteAt(sb, absY + h - 1, absX + 1, new string(hh.Length > 0 ? hh[0] : '─', w - 2), bc, Bg);
            WriteAt(sb, absY + h - 1, absX + w - 1, br, bc, Bg);
        }

        // ── 渲染子控件（在内容区域内）──
        base.OnRender(sb, absX, absY);
    }

    // OnKey 继承自 TuiView，自动路由到子焦点控件
}
