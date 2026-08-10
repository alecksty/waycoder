using WayCoder.Terminal;
namespace WayCoder.UI;

/// <summary>
/// 输入提示框 —— 通过 AnsiText 封装层渲染。
/// </summary>
public static class TuiPrompt
{
    /// <summary>聊天输入框。简洁的 ❯ 提示符。</summary>
    public static string ChatInput()
    {
        Console.Write(AnsiText.Prompt("❯ "));
        return Console.ReadLine() ?? "";
    }

    /// <summary>普通文本输入。空输入返回默认值。</summary>
    public static string Ask(string prompt, string? defaultValue = null)
    {
        var defSuffix = defaultValue != null ? $" [{AnsiText.Dim(defaultValue)}]" : "";
        Console.Write($"{AnsiText.Heading(TuiHelper.Esc(prompt))}{defSuffix} ");
        var result = Console.ReadLine() ?? "";
        return string.IsNullOrEmpty(result) ? (defaultValue ?? "") : result;
    }

    /// <summary>密码/密钥输入（不回显）。</summary>
    public static string Secret(string prompt)
    {
        Console.Write($"{AnsiText.Heading(TuiHelper.Esc(prompt))} ");
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Escape) { sb.Clear(); break; }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Length--;
                continue;
            }
            if (key.KeyChar >= ' ')
                sb.Append(key.KeyChar);
        }
        Console.WriteLine();
        return sb.ToString();
    }

    /// <summary>确认输入（y/n）。返回 true 表示确认。</summary>
    public static bool Confirm(string prompt)
    {
        Console.Write($"{AnsiText.Heading(TuiHelper.Esc(prompt))} [y/n] ");
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar == 'y' || key.KeyChar == 'Y') { Console.WriteLine("y"); return true; }
            if (key.KeyChar == 'n' || key.KeyChar == 'N') { Console.WriteLine("n"); return false; }
            if (key.Key == ConsoleKey.Escape) { Console.WriteLine("取消"); return false; }
        }
    }
}
