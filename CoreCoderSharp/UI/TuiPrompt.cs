using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 输入提示框 —— Spectre.Console TextPrompt 的便捷封装。
/// 支持光标移动编辑（← → Home End）、复制粘贴（Ctrl+V）、删除（Backspace / Delete）。
/// </summary>
public static class TuiPrompt
{
    /// <summary>
    /// 聊天输入框。简洁的 ❯ 提示符，白色输入文本。
    /// 支持光标移动编辑、复制粘贴。
    /// </summary>
    public static string ChatInput()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[{TuiColors.AccentMarkup}]❯[/] ")
                .AllowEmpty()
                .PromptStyle(new Style(foreground: Color.White)));
    }

    /// <summary>
    /// 普通文本输入。返回用户输入的文本，空输入返回默认值。
    /// </summary>
    public static string Ask(string prompt, string? defaultValue = null)
    {
        var p = new TextPrompt<string>($"[{TuiColors.HeadingMarkup}]{TuiHelper.Esc(prompt)}[/]")
            .AllowEmpty()
            .PromptStyle(TuiColors.Accent);

        if (defaultValue != null)
            p.DefaultValue(defaultValue);

        return AnsiConsole.Prompt(p);
    }

    /// <summary>
    /// 密码/密钥输入（输入内容不回显）。
    /// </summary>
    public static string Secret(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[{TuiColors.HeadingMarkup}]{TuiHelper.Esc(prompt)}[/]")
                .Secret());
    }

    /// <summary>
    /// 确认输入（y/n）。返回 true 表示确认。
    /// </summary>
    public static bool Confirm(string prompt)
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt($"[{TuiColors.HeadingMarkup}]{TuiHelper.Esc(prompt)}[/]"));
    }
}
