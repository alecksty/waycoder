using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 列表选单控件 —— Spectre.Console SelectionPrompt 的便捷封装。
/// </summary>
public static class TuiList
{
    /// <summary>
    /// 单选列表。返回用户选择的选项文本，取消返回 null。
    /// </summary>
    public static string? Select(string title, List<string> choices)
    {
        if (choices.Count == 0) return null;

        var escapedTitle = TuiHelper.Esc(title);
        var escapedChoices = choices.Select(c => TuiHelper.Esc(c)).ToList();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{TuiColors.HeadingMarkup}]{escapedTitle}[/]")
                .AddChoices(escapedChoices)
                .HighlightStyle(TuiColors.Accent));

        // 返回原始文本（非 escaped）
        var idx = escapedChoices.IndexOf(choice);
        return idx >= 0 ? choices[idx] : choice;
    }

    /// <summary>
    /// 多选列表。返回用户选择的所有选项文本。
    /// </summary>
    public static List<string> MultiSelect(string title, List<string> choices)
    {
        if (choices.Count == 0) return [];

        var escapedTitle = TuiHelper.Esc(title);
        var escapedChoices = choices.Select(c => TuiHelper.Esc(c)).ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[{TuiColors.HeadingMarkup}]{escapedTitle}[/]")
                .AddChoices(escapedChoices)
                .HighlightStyle(TuiColors.Accent));

        // 映射回原始文本
        var result = new List<string>();
        foreach (var s in selected)
        {
            var idx = escapedChoices.IndexOf(s);
            result.Add(idx >= 0 ? choices[idx] : s);
        }
        return result;
    }
}
