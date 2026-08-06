using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 对话框/提示框控件 —— 基于 Spectre.Console Panel。
/// 提供 Info / Success / Warn / Error 四种预设样式。
/// </summary>
public static class TuiBox
{
    /// <summary>信息提示框（黄色边框）</summary>
    public static void Info(string title, string content)
    {
        Render(title, content, TuiColors.Border, TuiColors.HeadingMarkup);
    }

    /// <summary>成功提示框（绿色边框）</summary>
    public static void Success(string title, string content)
    {
        Render(title, content, TuiColors.SuccessBorder, TuiColors.SuccessMarkup);
    }

    /// <summary>警告提示框（橙色边框）</summary>
    public static void Warn(string title, string content)
    {
        Render(title, content, TuiColors.WarnBorder, TuiColors.WarnMarkup);
    }

    /// <summary>错误提示框（红色边框）</summary>
    public static void Error(string title, string content)
    {
        Render(title, content, TuiColors.ErrorBorder, TuiColors.ErrorMarkup);
    }

    // ---- 内部 ----

    private static void Render(string title, string content, Style borderStyle, string? titleColor)
    {
        var panel = new Panel(new Markup(TuiHelper.Esc(content)))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = borderStyle,
            Padding = new Padding(1, 0, 1, 0),
        };

        if (!string.IsNullOrEmpty(title))
        {
            var color = titleColor ?? TuiColors.HeadingMarkup;
            panel.Header = new PanelHeader($"[{color}]{TuiHelper.Esc(title)}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>无标题的简单提示框</summary>
    public static void Simple(string content, Style? borderStyle = null)
    {
        Render("", content, borderStyle ?? TuiColors.Border, null);
    }
}
