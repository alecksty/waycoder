using CoreCoderSharp.Terminal;
namespace CoreCoderSharp.UI;

/// <summary>
/// 列表选单控件 —— 通过 AnsiText 封装层渲染。
/// </summary>
public static class TuiList
{
    /// <summary>单选列表，返回选中的项。取消返回 null。</summary>
    public static string? Select(string title, List<string> choices)
    {
        if (choices.Count == 0) return null;

        Console.WriteLine(AnsiText.Heading(TuiHelper.Esc(title)));
        for (int i = 0; i < choices.Count; i++)
            Console.WriteLine($"  [{i + 1}] {TuiHelper.Esc(choices[i])}");
        Console.Write(AnsiText.Prompt($"选择 (1-{choices.Count}, q=取消): "));

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar == 'q' || key.KeyChar == 'Q' || key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("取消");
                return null;
            }
            if (int.TryParse(key.KeyChar.ToString(), out var idx) && idx >= 1 && idx <= choices.Count)
            {
                Console.WriteLine(choices[idx - 1]);
                return choices[idx - 1];
            }
        }
    }

    /// <summary>多选列表。完成按 Enter，取消按 q。</summary>
    public static List<string> MultiSelect(string title, List<string> choices)
    {
        if (choices.Count == 0) return [];
        var selected = new HashSet<int>();

        Console.WriteLine(AnsiText.Heading(TuiHelper.Esc(title)));
        Console.WriteLine(AnsiText.Dim("  空格=切换  回车=确认  q=取消"));

        RenderMultiList(choices, selected);
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar == 'q' || key.KeyChar == 'Q' || key.Key == ConsoleKey.Escape)
                return [];
            if (key.Key == ConsoleKey.Enter)
                break;
            if (int.TryParse(key.KeyChar.ToString(), out var idx) && idx >= 1 && idx <= choices.Count)
            {
                if (selected.Contains(idx - 1)) selected.Remove(idx - 1);
                else selected.Add(idx - 1);
            }
            // 光标回到列表开头
            Console.CursorTop -= choices.Count;
            RenderMultiList(choices, selected);
        }

        return choices.Where((_, i) => selected.Contains(i)).ToList();
    }

    private static void RenderMultiList(List<string> choices, HashSet<int> selected)
    {
        foreach (var _ in choices) Console.Write("\r                                   \r");
        Console.CursorTop -= choices.Count;
        for (int i = 0; i < choices.Count; i++)
        {
            var marker = selected.Contains(i) ? AnsiText.Success("✓") : " ";
            Console.WriteLine($" [{marker}] {i + 1}. {TuiHelper.Esc(choices[i])}");
        }
    }
}
