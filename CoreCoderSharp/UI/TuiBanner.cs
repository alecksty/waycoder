using System.Text;
using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 欢迎横幅 —— 大字体 Logo + 信息面板。
/// </summary>
public static class TuiBanner
{
    /// <summary>
    /// 显示应用横幅：FigletText 大字标题 + Panel 信息栏。
    /// </summary>
    public static void Show(string appName, string version, string model,
        string? apiUrl = null, bool debugMode = false)
    {
        // 1. 大字体 ASCII Art
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText(appName)
                .Centered()
                .Color(Color.Yellow));

        // 2. 信息面板
        var info = new StringBuilder();
        info.AppendLine($"[{TuiColors.AccentMarkup}]⚡ {TuiHelper.Esc(appName)}[/] " +
            $"[{TuiColors.DimMarkup}]v{TuiHelper.Esc(version)}[/]  ·  " +
            $"模型: [{TuiColors.SuccessMarkup}]{TuiHelper.Esc(model)}[/]");
        if (apiUrl != null)
            info.AppendLine($"  API: [{TuiColors.DimMarkup}]{TuiHelper.Esc(apiUrl)}[/]");
        info.Append($"[{TuiColors.DimMarkup}]  /help 帮助  quit 退出  Ctrl+C 取消[/]");

        var panel = new Panel(new Markup(info.ToString()))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = TuiColors.Border,
            Padding = new Padding(2, 0, 2, 0),
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (debugMode)
        {
            AnsiConsole.MarkupLine(
                $"[bold {TuiColors.WarnMarkup}]🐛 DEBUG 模式已开启 → logs/ 目录[/]");
            AnsiConsole.WriteLine();
        }
    }
}
