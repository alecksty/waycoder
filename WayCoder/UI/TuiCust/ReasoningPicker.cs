using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 推理深度选择器 —— 对标 Crush reasoning.go。
/// 全屏 ANSI 直写模式，为支持多级推理的模型选择推理深度。
///
/// 功能：
///   - 5 级推理深度（minimal / low / medium / high / max）
///   - 实时搜索过滤
///   - 上下键导航 / Enter 确认 / Esc 取消
///   - 当前选中的级别标记 ✓
///   - 帮助栏 + 键盘快捷键
/// </summary>
public static class ReasoningPicker
{
    /// <summary>推理深度级别定义</summary>
    public record ReasoningLevel(string Id, string Label, string Description);

    /// <summary>选择结果</summary>
    public record Result(string Level);

    /// <summary>所有可用的推理深度级别</summary>
    public static readonly ReasoningLevel[] Levels =
    [
        new("minimal", "Minimal",  "几乎不思考，最快速度"),
        new("low",     "Low",      "快速推理，适合简单任务"),
        new("medium",  "Medium",   "平衡速度与深度（推荐）"),
        new("high",    "High",     "深度推理，适合复杂逻辑"),
        new("max",     "Max",      "极致推理，最复杂的多步问题"),
    ];

    /// <summary>
    /// 显示推理深度选择对话框。返回选中的级别，null = 取消。
    /// </summary>
    /// <param name="currentLevel">当前已选择的推理级别（用于标记 ✓）</param>
    /// <param name="modelName">当前使用的模型名称（用于标题显示）</param>
    public static Result? Show(string? currentLevel = null, string? modelName = null)
    {
        currentLevel ??= Config.Instance.ReasoningEffort;
        modelName ??= Config.Instance.Model;

        var filter = "";
        int selectedIdx = 0;
        int scrollOffset = 0;

        var (tw, th) = (Tty.Cols, Tty.Rows);

        // 找到当前级别的索引
        var allLevels = Levels.ToList();
        for (int i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i].Id == currentLevel)
            {
                selectedIdx = i;
                break;
            }
        }

        while (true)
        {
            // 过滤
            var filtered = string.IsNullOrEmpty(filter)
                ? allLevels
                : allLevels.Where(l =>
                    l.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    l.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    l.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            // 可见行数
            int contentH = Math.Max(5, th - 7);
            int visibleItems = Math.Max(1, contentH - 1);

            // 滚动调整
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + visibleItems) scrollOffset = selectedIdx - visibleItems + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - visibleItems));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题栏
            var title = $"推理深度 — {modelName}";
            sb.Append(AnsiTty.FgBg(30, TuiColors.BgMagenta));
            sb.Append($"  {title}  ");
            sb.Append(new string(' ', Math.Max(0, tw - VW(title) - 4)));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 说明行
            sb.Append(AnsiTty.Fg(35)); // 紫色
            var desc = "  选择模型的「思考」深度，越深推理越充分，但耗时越长";
            sb.Append(desc);
            sb.Append(new string(' ', Math.Max(0, tw - VW(desc))));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 搜索栏
            sb.Append(AnsiTty.FgBg(30, 47)); // 白底黑字
            var searchPrompt = "搜索: ";
            var searchText = filter.Length > 0 ? filter : "输入关键词过滤...";
            var searchStyle = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append(searchPrompt).Append(searchStyle).Append(searchText).Append(AnsiTty.SgrReset);
            sb.Append(new string(' ', Math.Max(0, tw - VW(searchPrompt + searchText) - 2)));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 推理级别列表
            int listTop = 4;
            for (int i = 0; i < visibleItems; i++)
            {
                int mi = scrollOffset + i;
                sb.Append(AnsiTty.CursorPos(listTop + i, 1)).Append(AnsiTty.ClearToEnd);

                if (mi >= filtered.Count) continue;

                var level = filtered[mi];
                bool isSelected = mi == selectedIdx;
                bool isCurrent = level.Id == currentLevel;

                // 行前缀
                var prefix = isSelected ? "▶ " : "  ";
                var check = isCurrent ? " ✓" : "  ";

                // 颜色
                if (isSelected)
                {
                    sb.Append(AnsiTty.FgBg(TuiColors.Black, TuiColors.BgMagenta));
                }
                else if (isCurrent)
                {
                    sb.Append(AnsiTty.Fg(35)); // 紫色
                }

                // 显示：标签 + 描述
                var label = level.Label.PadRight(10);
                var display = $"{prefix}{label} — {level.Description}{check}";
                display = TruncateByVW(display, tw - 1);
                sb.Append(display);
                sb.Append(AnsiTty.SgrReset);
            }

            // 帮助栏
            int helpRow = listTop + visibleItems;
            sb.Append(AnsiTty.CursorPos(helpRow, 1));
            sb.Append(AnsiTty.FgBg(30, 47));
            var helpText = "[↑/↓] 导航  [Enter] 确认  [Esc] 取消  [字母] 搜索  [←] 清除=默认";
            sb.Append(helpText);
            sb.Append(new string(' ', Math.Max(0, tw - VW(helpText))));
            sb.Append(AnsiTty.SgrReset);

            Console.Write(sb.ToString());

            // ── 输入 ──
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selectedIdx > 0) selectedIdx--;
                    break;
                case ConsoleKey.DownArrow:
                    if (selectedIdx < filtered.Count - 1) selectedIdx++;
                    break;
                case ConsoleKey.Enter:
                    if (filtered.Count > 0 && selectedIdx < filtered.Count)
                    {
                        var selected = filtered[selectedIdx];
                        Config.Instance.ReasoningEffort = selected.Id;
                        Config.Instance.SaveToEnvFile();
                        return new Result(selected.Id);
                    }
                    break;
                case ConsoleKey.Escape:
                    return null;
                case ConsoleKey.Backspace:
                    if (filter.Length > 0)
                    {
                        filter = filter[..^1];
                        selectedIdx = 0;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                    // 清除：恢复默认（不设置 reasoning_effort）
                    Config.Instance.ReasoningEffort = "";
                    Config.Instance.SaveToEnvFile();
                    return new Result("");
                case ConsoleKey.Home:
                    selectedIdx = 0;
                    break;
                case ConsoleKey.End:
                    selectedIdx = Math.Max(0, filtered.Count - 1);
                    break;
                case ConsoleKey.PageUp:
                    selectedIdx = Math.Max(0, selectedIdx - visibleItems);
                    break;
                case ConsoleKey.PageDown:
                    selectedIdx = Math.Min(filtered.Count - 1, selectedIdx + visibleItems);
                    break;
                default:
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    {
                        filter += key.KeyChar;
                        selectedIdx = 0;
                    }
                    break;
            }
        }
    }

    // ── 工具 ──

    private static int VW(string text) => TuiHelper.DisplayWidth(text);

    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int vw = 0, chars = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w > maxVW) break;
            vw += w; chars += rune.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }
}
