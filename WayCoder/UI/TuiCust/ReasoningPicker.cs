using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 推理深度选择器 —— 对标 Crush reasoning.go。
/// 居中带边框对话框（非全屏），为支持多级推理的模型选择推理深度。
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

    private const int MinW = 58, MinH = 15;
    private const int FrameH = 8; // 顶框1+标题1+说明1+搜索1+上分隔1 + 下分隔1+帮助1+底框1

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

        try
        {
        while (true)
        {
            // 过滤
            var filtered = string.IsNullOrEmpty(filter)
                ? allLevels
                : allLevels.Where(l =>
                    l.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    l.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    l.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            var (bx, by, dw, dh, innerW) = DialogFrame.Layout(MinW, MinH);
            int listH = Math.Max(3, dh - FrameH);

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            // 滚动调整
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + listH) scrollOffset = selectedIdx - listH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - listH));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide);
            DialogFrame.DimArea(sb, bx, by, dw, dh);
            DialogFrame.TopBorder(sb, by, bx, dw);

            // 标题行（紫底）
            int y = by + 1;
            DialogFrame.SideL(sb, y, bx);
            DialogFrame.FillInner(sb, y, bx, innerW, TuiColors.White, TuiColors.BgMagenta);
            var title = $"推理深度 — {modelName}";
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, TuiColors.BgMagenta))
              .Append(AnsiTty.SgrBold).Append(TruncateByVW(title, innerW - 4)).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 说明行
            y = by + 2;
            DialogFrame.SideL(sb, y, bx);
            DialogFrame.FillInner(sb, y, bx, innerW, TuiColors.Magenta, DialogFrame.DimBg);
            var desc = "选择模型的「思考」深度，越深推理越充分，但耗时越长";
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.Magenta, DialogFrame.DimBg))
              .Append(TruncateByVW(desc, innerW - 4))
              .Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 搜索行
            y = by + 3;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DialogFrame.DimBg));
            var searchPrompt = "搜索: ";
            var searchText = filter.Length > 0 ? filter : "输入关键词过滤...";
            var searchStyle = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append(searchPrompt).Append(searchStyle).Append(TruncateByVW(searchText, innerW - 4 - VW(searchPrompt)))
              .Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 上分隔线
            y = by + 4;
            DialogFrame.SepLine(sb, y, bx, dw);

            // 推理级别列表
            int dataTop = by + 5;
            for (int i = 0; i < listH; i++)
            {
                int mi = scrollOffset + i, row = dataTop + i;
                DialogFrame.SideL(sb, row, bx);

                if (mi >= filtered.Count)
                {
                    DialogFrame.FillInner(sb, row, bx, innerW, TuiColors.White, DialogFrame.DimBg);
                    DialogFrame.SideR(sb, row, bx, dw);
                    continue;
                }

                var level = filtered[mi];
                bool isSelected = mi == selectedIdx;
                bool isCurrent = level.Id == currentLevel;

                int bg = isSelected ? TuiColors.BgMagenta : DialogFrame.DimBg;
                int fg = isSelected ? TuiColors.Black : (isCurrent ? TuiColors.Magenta : TuiColors.White);
                DialogFrame.FillInner(sb, row, bx, innerW, fg, bg);

                var prefix = isSelected ? "▶ " : "  ";
                var check = isCurrent ? " ✓" : "  ";
                var label = level.Label.PadRight(10);
                var display = TruncateByVW($"{prefix}{label} — {level.Description}{check}", innerW - 2);

                sb.Append(AnsiTty.CursorPos(row, bx + 2))
                  .Append(AnsiTty.FgBgCode(fg, bg))
                  .Append(display)
                  .Append(AnsiTty.SgrReset);

                DialogFrame.SideR(sb, row, bx, dw);
            }

            // 下分隔线
            int sep2 = dataTop + listH;
            DialogFrame.SepLine(sb, sep2, bx, dw);

            // 帮助行
            DialogFrame.SideL(sb, sep2 + 1, bx);
            sb.Append(AnsiTty.CursorPos(sep2 + 1, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append(TruncateByVW("[↑/↓] 导航  [Enter] 确认  [Esc] 取消  [字母] 搜索  [←] 清除=默认", innerW - 4));
            DialogFrame.SideR(sb, sep2 + 1, bx, dw);

            // 底框
            DialogFrame.BottomBorder(sb, sep2 + 2, bx, dw);

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
                    selectedIdx = Math.Max(0, selectedIdx - listH);
                    break;
                case ConsoleKey.PageDown:
                    selectedIdx = Math.Min(filtered.Count - 1, selectedIdx + listH);
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
        finally
        {
            Console.Write(AnsiTty.CursorShow);
            TuiManager.RequestFullRefresh();
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
