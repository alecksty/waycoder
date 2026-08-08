using CoreCoderSharp.Terminal;
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

    private static void Render(string title, string content, int borderFg, int titleFg)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var titleVw = string.IsNullOrEmpty(title) ? 0 : TuiHelper.DisplayWidth(title) + 2;
        var w = Math.Max(Math.Max(20, maxVw + 4), titleVw + 4);
        w = Math.Min(w, Console.WindowWidth - 4);

        var sb = new System.Text.StringBuilder();
        var border = AnsiText.BorderOpen(borderFg);

        // 顶边框 + 标题
        sb.Append(border);
        sb.Append("╭─");
        if (!string.IsNullOrEmpty(title))
        {
            sb.Append(AnsiText.Reset);
            sb.Append(' ').Append(AnsiText.BoldFg(title, titleFg)).Append(' ');
            sb.Append(border);
        }
        var topFill = w - 2 - (string.IsNullOrEmpty(title) ? 0 : titleVw + 2);
        if (topFill > 0) sb.Append(new string('─', topFill));
        sb.Append("╮");
        sb.Append(AnsiText.Reset);
        sb.Append('\n');

        // 内容行
        foreach (var line in lines)
        {
            var lw = TuiHelper.DisplayWidth(line);
            var pad = Math.Max(0, w - 4 - lw);
            sb.Append(border).Append("│").Append(AnsiText.Reset);
            sb.Append(' ').Append(line);
            if (pad > 0) sb.Append(new string(' ', pad));
            sb.Append(' ').Append(border).Append("│").Append(AnsiText.Reset);
            sb.Append('\n');
        }

        // 底边框
        sb.Append(border);
        sb.Append("╰").Append(new string('─', w - 2)).Append("╯");
        sb.Append(AnsiText.Reset);
        sb.Append('\n');

        var output = sb.ToString();

        try
        {
            if (ScreenManager.Instance.IsActive)
            {
                ScreenManager.Instance.AddSystemMsg(output);
                ScreenManager.Instance.Render();
            }
            else Console.WriteLine(output);
        }
        catch { Console.WriteLine(output); }
    }
}
