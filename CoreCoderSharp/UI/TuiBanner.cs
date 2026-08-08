using CoreCoderSharp.Terminal;
using System.Text;

namespace CoreCoderSharp.UI;

/// <summary>
/// 欢迎横幅 —— 大字 Logo + 信息面板（通过 AnsiText 封装层渲染）。
/// </summary>
public static class TuiBanner
{
    /// <summary>显示应用横幅</summary>
    public static void Show(string appName, string version, string model,
        string? apiUrl = null, bool debugMode = false)
    {
        var sb = new StringBuilder();

        // 1. 大字 ASCII Art
        var asciiTitle = BuildAsciiTitle(appName);
        sb.AppendLine();
        sb.AppendLine(AnsiText.BoldFg(asciiTitle, TuiColors.Yellow));

        // 2. 信息面板
        var infoLines = new List<string>
        {
            $"{AnsiText.Accent($"⚡ {appName}")} {AnsiText.Dim($"v{version}")}  ·  " +
            $"模型: {AnsiText.Success(model)}"
        };
        if (apiUrl != null)
            infoLines.Add($"  API: {AnsiText.Dim(apiUrl)}");
        infoLines.Add(AnsiText.Dim("  /help 帮助  quit 退出  Ctrl+C 取消"));

        var maxVw = infoLines.Max(l => AnsiVW(l));
        var w = Math.Min(Console.WindowWidth - 4, maxVw + 4);

        // 顶边框
        sb.AppendLine($"{AnsiText.BorderOpen(TuiColors.Border)}╭{new string('─', w - 2)}╮{AnsiText.Reset}");
        foreach (var line in infoLines)
        {
            var lw = AnsiVW(line);
            var pad = Math.Max(0, w - 4 - lw);
            sb.Append($"{AnsiText.BorderOpen(TuiColors.Border)}│{AnsiText.Reset} ");
            sb.Append(line);
            if (pad > 0) sb.Append(new string(' ', pad));
            sb.AppendLine($" {AnsiText.BorderOpen(TuiColors.Border)}│{AnsiText.Reset}");
        }
        sb.AppendLine($"{AnsiText.BorderOpen(TuiColors.Border)}╰{new string('─', w - 2)}╯{AnsiText.Reset}");

        if (debugMode)
        {
            sb.AppendLine();
            sb.AppendLine(AnsiText.BoldFg("🐛 DEBUG 模式已开启 → logs/ 目录", TuiColors.Yellow));
        }

        Console.WriteLine(sb.ToString());
    }

    /// <summary>构建 ASCII 大字标题</summary>
    private static string BuildAsciiTitle(string name)
    {
        int w = Console.WindowWidth;
        var pad = Math.Max(0, (w - name.Length * 2) / 2);
        var indent = new string(' ', pad);
        var sb = new StringBuilder();
        sb.AppendLine(indent + string.Join("  ", name.ToCharArray()));
        return sb.ToString();
    }

    /// <summary>计算 ANSI 文本的显示宽度（忽略转义序列）</summary>
    private static int AnsiVW(string text)
    {
        int w = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                while (i < text.Length && text[i] != 'm') i++;
                continue;
            }
            var rune = System.Text.Rune.GetRuneAt(text, i);
            w += TuiHelper.RuneWidth(rune);
            if (rune.Utf16SequenceLength > 1) i += rune.Utf16SequenceLength - 1;
        }
        return w;
    }
}
