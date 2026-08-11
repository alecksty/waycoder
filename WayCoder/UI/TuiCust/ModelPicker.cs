using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 模型选择对话框 —— 对标 Crush models.go。
/// 全屏 ANSI 直写模式（与 DiffPreview 同模式），提供丰富的交互体验。
///
/// 功能：
///   - 模型列表（按供应商分组 + 最近使用）
///   - Tab 切换大模型/小模型类型
///   - 输入即搜索过滤
///   - 上下键导航 / Enter 确认 / Esc 取消
///   - 键盘快捷键帮助栏
/// </summary>
public static class ModelPicker
{
    /// <summary>模型条目</summary>
    public record ModelEntry(string Id, string Name, string Provider, bool IsConfigured);

    /// <summary>选择结果</summary>
    public record Result(string ModelId, bool IsLarge);

    /// <summary>
    /// 显示模型选择对话框。返回选中的模型，null = 取消。
    /// </summary>
    public static Result? Show()
    {
        var cfg = Config.Instance;
        var currentLarge = cfg.Model;
        var currentSmall = cfg.SmallModel;

        // ── 构建模型列表 ──
        var availableModels = GetAvailableModels();
        bool isLarge = true; // 默认大模型
        var filter = "";
        int selectedIdx = 0;
        int scrollOffset = 0;

        var (tw, th) = (Tty.Cols, Tty.Rows);

        while (true)
        {
            // 过滤后的模型列表
            var filtered = string.IsNullOrEmpty(filter)
                ? availableModels
                : availableModels.Where(m =>
                    m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    m.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            // 当前选择的大/小模型
            var currentModel = isLarge ? currentLarge : currentSmall;
            // 标记当前使用的模型
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].Id == currentModel)
                {
                    selectedIdx = Math.Min(selectedIdx, filtered.Count - 1);
                    if (selectedIdx < i - 5 || selectedIdx > i + 5)
                        selectedIdx = i;
                    break;
                }
            }
            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            // 可见行数
            int contentH = Math.Max(5, th - 6); // 标题(1) + 搜索(1) + 帮助(1) + 边距
            int visibleItems = Math.Max(1, contentH - 1);

            // 滚动调整
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + visibleItems) scrollOffset = selectedIdx - visibleItems + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - visibleItems));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题栏
            var title = isLarge ? "选择大模型 (复杂任务)" : "选择小模型 (简单任务)";
            sb.Append(AnsiTty.FgBg(30, TuiColors.BgCyan));
            sb.Append($"  {title}  ");
            sb.Append(new string(' ', Math.Max(0, tw - VW(title) - 4)));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 搜索栏 + 类型切换
            sb.Append(AnsiTty.FgBg(30, 47)); // 白底黑字
            var searchPrompt = "搜索: ";
            var searchText = filter.Length > 0 ? filter : "输入关键词过滤...";
            var searchStyle = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            var typeLabel = isLarge ? "[大模型]  " : " 大模型   ";
            var typeLabel2 = !isLarge ? "[小模型]  " : " 小模型   ";
            sb.Append(searchPrompt).Append(searchStyle).Append(searchText).Append(AnsiTty.SgrReset);
            var typeInfo = $"  Tab切换  {AnsiTty.Sgr(36, 47, 1)}{typeLabel}{AnsiTty.SgrReset} {typeLabel2}";
            var padRight = Math.Max(0, tw - VW(searchPrompt + searchText) - VW(typeInfo) - 2);
            sb.Append(new string(' ', padRight)).Append(typeInfo);
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 模型列表
            int listTop = 3;
            for (int i = 0; i < visibleItems; i++)
            {
                int mi = scrollOffset + i;
                sb.Append(AnsiTty.CursorPos(listTop + i, 1)).Append(AnsiTty.ClearToEnd);

                if (mi >= filtered.Count) continue;

                var model = filtered[mi];
                bool isSelected = mi == selectedIdx;
                bool isCurrent = model.Id == currentModel;

                // 行前缀
                var prefix = isSelected ? "▶ " : "  ";
                var check = isCurrent ? " ✓" : "  ";

                // 颜色
                int rowFg, rowBg;
                if (isSelected)
                {
                    rowFg = TuiColors.Black; rowBg = TuiColors.BgCyan;
                }
                else if (isCurrent)
                {
                    rowFg = 32; rowBg = 0;
                }
                else
                {
                    rowFg = 37; rowBg = 0;
                }

                sb.Append(isSelected ? AnsiTty.FgBg(rowFg, rowBg) :
                         isCurrent ? AnsiTty.Fg(32) : "");

                // 供应商标签
                var provTag = $"[{model.Provider}]";
                var provStyled = isSelected
                    ? provTag
                    : AnsiTty.SgrDim + provTag + AnsiTty.SgrReset;

                var display = $"{prefix}{model.Name,-36} {provStyled}{check}";
                display = TruncateByVW(display, tw - 1);
                sb.Append(display);
                sb.Append(AnsiTty.SgrReset);
            }

            // 帮助栏
            int helpRow = listTop + visibleItems;
            sb.Append(AnsiTty.CursorPos(helpRow, 1));
            sb.Append(AnsiTty.FgBg(30, 47)); // 白底黑字
            var helpText = "[↑/↓] 导航  [Enter] 确认  [Esc] 取消  [Tab] 切换大小模型  [字母] 搜索";
            sb.Append(helpText);
            sb.Append(new string(' ', Math.Max(0, tw - VW(helpText))));
            sb.Append(AnsiTty.SgrReset);

            // 滚动指示
            if (filtered.Count > visibleItems)
            {
                var pct = filtered.Count > 1 ? scrollOffset * 100 / (filtered.Count - visibleItems) : 0;
                sb.Append(AnsiTty.CursorPos(helpRow, tw - 6))
                  .Append(AnsiTty.FgBg(30, 47))
                  .Append($"{pct}%")
                  .Append(AnsiTty.SgrReset);
            }

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
                case ConsoleKey.Tab:
                    isLarge = !isLarge;
                    filter = "";
                    selectedIdx = 0;
                    break;
                case ConsoleKey.Enter:
                    if (filtered.Count > 0 && selectedIdx < filtered.Count)
                    {
                        var selected = filtered[selectedIdx];
                        if (isLarge) cfg.Model = selected.Id;
                        else cfg.SmallModel = selected.Id;
                        cfg.SaveToEnvFile();
                        return new Result(selected.Id, isLarge);
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
                    // 可打印字符 → 搜索过滤
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    {
                        filter += key.KeyChar;
                        selectedIdx = 0;
                    }
                    break;
            }
        }
    }

    // ── 模型数据 ──

    private static List<ModelEntry> GetAvailableModels()
    {
        var models = new List<ModelEntry>();

        // 从 Config schema 和 FallbackChain 提取
        var cfg = Config.Instance;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 常用模型（硬编码供应商映射 + 回退链模型）
        var knownModels = new (string Id, string Provider)[]
        {
            ("deepseek-v4-pro", "DeepSeek"),
            ("deepseek-v4-flash", "DeepSeek"),
            ("deepseek-chat", "DeepSeek"),
            ("deepseek-reasoner", "DeepSeek"),
            ("gpt-5.4", "OpenAI"),
            ("gpt-5.5", "OpenAI"),
            ("gpt-4o", "OpenAI"),
            ("gpt-4o-mini", "OpenAI"),
            ("gpt-5.4-mini", "OpenAI"),
            ("claude-opus-5", "Anthropic"),
            ("claude-sonnet-5", "Anthropic"),
            ("claude-fable-5", "Anthropic"),
            ("claude-haiku-4-5", "Anthropic"),
            ("gemini-2.0-flash", "Google"),
            ("gemini-2.5-pro", "Google"),
            ("gemini-2.5-flash", "Google"),
            ("qwen-turbo", "Qwen"),
            ("qwen-plus", "Qwen"),
            ("qwen-max", "Qwen"),
            ("glm-4-flash", "Zhipu"),
            ("glm-4-plus", "Zhipu"),
        };

        foreach (var (id, provider) in knownModels)
        {
            if (seen.Add(id))
                models.Add(new ModelEntry(id, id, provider, true));
        }

        // 从回退链添加
        if (!string.IsNullOrEmpty(cfg.FallbackChain))
        {
            foreach (var m in cfg.FallbackChain.Split(','))
            {
                var trimmed = m.Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    models.Add(new ModelEntry(trimmed, trimmed, "自定义", true));
            }
        }

        return models;
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
