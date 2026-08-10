using CoreCoderSharp.Terminal;
using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.UI;

/// <summary>
/// 对话框/提示框控件 —— 使用 AnsiText 封装层渲染。
/// </summary>
public static class TuiBox
{
    public static void Info(string title, string content)
        => Render(title, content, TuiColors.Border, TuiColors.HeadingFg);

    public static void Success(string title, string content)
        => Render(title, content, TuiColors.Green, TuiColors.Green);

    public static void Warn(string title, string content)
        => Render(title, content, TuiColors.Yellow, TuiColors.Yellow);

    public static void Error(string title, string content)
        => Render(title, content, TuiColors.Red, TuiColors.Red);

    public static void Simple(string content, int? borderColor = null)
        => Render("", content, borderColor ?? TuiColors.Border, TuiColors.Border);

    /// <summary>
    /// 渲染对话框/提示框控件。
    /// </summary>
    /// <param name="title">标题框标题。</param>
    /// <param name="content">内容框内容。</param>
    /// <param name="borderFg">边框颜色。</param>
    /// <param name="titleFg">标题颜色。</param>
    private static void Render(string title, string content, int borderFg, int titleFg)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var titleVw = string.IsNullOrEmpty(title) ? 0 : TuiHelper.DisplayWidth(title) + 2;
        var w = Math.Max(Math.Max(20, maxVw + 4), titleVw + 4);
        w = Math.Min(w, Tty.Cols - 4);

        var sb = new System.Text.StringBuilder();
        var border = AnsiText.BorderOpen(borderFg);

        // 顶边框 + 标题
        sb.Append(border);
        sb.Append("╭─");
        if (!string.IsNullOrEmpty(title))
        {
            sb.Append(AnsiTty.SgrReset);
            sb.Append(' ').Append(AnsiText.BoldFg(title, titleFg)).Append(' ');
            sb.Append(border);
        }

        var topFill = w - 2 - (string.IsNullOrEmpty(title) ? 0 : titleVw + 2);
        if (topFill > 0) sb.Append(new string('─', topFill));
        sb.Append("╮");
        sb.Append(AnsiTty.SgrReset);
        sb.Append('\n');

        // 内容行
        foreach (var line in lines)
        {
            var lw = TuiHelper.DisplayWidth(line);
            var pad = Math.Max(0, w - 4 - lw);
            sb.Append(border).Append("│").Append(AnsiTty.SgrReset);
            sb.Append(' ').Append(line);
            if (pad > 0) sb.Append(new string(' ', pad));
            sb.Append(' ').Append(border).Append("│").Append(AnsiTty.SgrReset);
            sb.Append('\n');
        }

        // 底边框
        sb.Append(border);
        sb.Append("╰").Append(new string('─', w - 2)).Append("╯");
        sb.Append(AnsiTty.SgrReset);
        sb.Append('\n');

        var output = sb.ToString();

        try
        {
            var chatScreen = TuiManager.Instance.ActiveScreen as ChatScreen;
            if (chatScreen != null)
            {
                chatScreen.AddSystemMsg(output);
                TuiManager.Instance.Render();
            }
            else Console.WriteLine(output);
        }
        catch
        {
            Console.WriteLine(output);
        }
    }
}