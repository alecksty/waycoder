using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 进度条/状态条控件 —— 用于 Token 消耗、上下文使用率等可视化。
/// </summary>
public static class TuiProgress
{
    /// <summary>
    /// 渲染一个进度条，带百分比标签。
    /// </summary>
    /// <param name="label">标签文本（如 "上下文窗口"）</param>
    /// <param name="percent">百分比 (0-100)</param>
    /// <param name="width">进度条总宽度（字符数），默认 30</param>
    public static void Bar(string label, double percent, int width = 30)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var filled = (int)(clamped / 100 * width);
        var empty = width - filled;

        var barColor = clamped switch
        {
            < 50 => TuiColors.SuccessMarkup,
            < 80 => TuiColors.WarnMarkup,
            _ => TuiColors.ErrorMarkup,
        };

        AnsiConsole.Markup(
            $"  [{TuiColors.DimMarkup}]{TuiHelper.Esc(label)}[/] " +
            $"[{barColor}]{new string('█', filled)}{new string('░', empty)}[/] " +
            $"[{barColor}]{clamped:F0}%[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// 渲染一个简单的水平分隔线。
    /// </summary>
    public static void Rule(string? title = null)
    {
        if (title != null)
            AnsiConsole.Write(new Rule(TuiHelper.Esc(title)).RuleStyle(TuiColors.Border));
        else
            AnsiConsole.Write(new Rule().RuleStyle(TuiColors.Border));
    }
}
