using CoreCoderSharp.Terminal;
namespace CoreCoderSharp.UI;

/// <summary>
/// 进度条/状态条控件 —— 通过 AnsiText 封装层渲染。
/// </summary>
public static class TuiProgress
{
    /// <summary>渲染进度条，带百分比标签。</summary>
    public static void Bar(string label, double percent, int width = 30)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var filled = (int)(clamped / 100 * width);
        var empty = width - filled;

        var barColor = clamped switch
        {
            < 50 => TuiColors.Green,
            < 80 => TuiColors.Yellow,
            _ => TuiColors.Red,
        };

        var bar = $"{new string('█', filled)}{new string('░', empty)}";
        Console.Write($"  {AnsiText.Dim(TuiHelper.Esc(label))} " +
            $"{AnsiText.Fg(bar + " " + $"{clamped:F0}%", barColor)}");
        Console.WriteLine();
    }

    /// <summary>渲染水平分隔线。</summary>
    public static void Rule(string? title = null)
    {
        var w = TTY.Cols;
        if (title != null)
        {
            var t = $" {TuiHelper.Esc(title)} ";
            var tw = TuiHelper.DisplayWidth(title) + 2;
            var half = (w - tw) / 2;
            Console.WriteLine(AnsiText.Fg(
                $"{new string('─', half)}{t}{new string('─', w - half - tw)}",
                TuiColors.Yellow));
        }
        else
        {
            Console.WriteLine(AnsiText.Fg(new string('─', w), TuiColors.Yellow));
        }
    }
}
