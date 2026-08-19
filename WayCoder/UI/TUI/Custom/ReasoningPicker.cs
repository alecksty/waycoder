using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui;

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
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiLabel + TuiInput + TuiList，
/// 走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
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

        Result? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(currentLevel, modelName, screen, r => { result = r; evt.Set(); });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 30_000, win);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(string currentLevel, string modelName,
        TuiScreen? screen, Action<Result?> onDone)
    {
        // 标记加载：结构/ids 来自 reasoningpicker.tui（布局写标记），动态内容与事件 code-behind
        var res = TuiMarkup.LoadResource("dialogs/reasoningpicker.tui");
        var win = res.Window ?? throw new InvalidOperationException("reasoningpicker.tui 根应为 Dialog");
        win.Title = $"推理深度 — {modelName}";
        win.WinBg = TuiTheme.Current.WindowBg;
        win.XScale = 0.6; // 宽度 = 终端 60%（标记 scale 兜底，此处显式保证）
        // 渐变边框（紫→粉，沿用主题；标记 hex 为兜底）
        var g = TuiTheme.Current.GradOrangeYellow; // 统一对话框渐变（与 TuiDialog 系一致）
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 控件接线（结构在标记里，精确样式/数据/事件在此）
        var search = res.Find<TuiInput>("search")!;
        var list = res.Find<TuiList>("list")!;
        search.Fg = AnsiColors.White;
        search.Bg = AnsiColors.BgBlack;
        list.Fg = AnsiColors.Black;

        // 当前过滤后的级别列表（搜索输入实时更新）
        var filtered = FilterLevels("");
        list.Items = filtered.Select(l => FormatItem(l, currentLevel)).ToList();
        list.SelectedIndex = IndexOfCurrent(filtered, currentLevel);

        // ── 动作 ──

        void Finish(Result? r)
        {
            onDone(r);
            win.OnClosed?.Invoke(); // 关闭模态窗口
        }
        void Confirm()
        {
            int idx = list.SelectedIndex;
            if (idx >= 0 && idx < filtered.Count)
            {
                var level = filtered[idx];
                Config.Instance.ReasoningEffort = level.Id;
                Config.Instance.SaveToEnvFile();
                Finish(new Result(level.Id));
            }
        }
        void Cancel() => Finish(null);
        void Reset()
        {
            // 清除：恢复默认（不设置 reasoning_effort）
            Config.Instance.ReasoningEffort = "";
            Config.Instance.SaveToEnvFile();
            Finish(new Result(""));
        }

        // 搜索输入：字母进过滤词（OnTextChanged 实时过滤），↑↓ 导航列表，Enter 确认
        search.OnTextChanged = () =>
        {
            filtered = FilterLevels(search.Text);
            list.Items = filtered.Select(l => FormatItem(l, currentLevel)).ToList();
            list.SelectedIndex = 0;
            list.ScrollOffset = 0;
            list.MarkDirty();
            screen?.MarkDirty();
        };
        search.KeyHook = key =>
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    list.OnKey(key);
                    list.MarkDirty();
                    screen?.MarkDirty();
                    return true;
                case ConsoleKey.Enter:
                    Confirm();
                    return true;
            }
            return false; // 其余（字母/退格）交给输入框处理
        };
        list.OnSelect = _ => Confirm(); // Tab 聚焦列表后 Enter 亦可确认

        win.RegisterShortcut(ConsoleKey.Escape, Cancel);
        win.RegisterShortcut(ConsoleKey.LeftArrow, Reset);

        return win;
    }

    // ── 纯逻辑（AOT 安全，可自测）──

    /// <summary>按关键词过滤级别（Label / Id / Description 忽略大小写匹配）。</summary>
    private static List<ReasoningLevel> FilterLevels(string filter)
    {
        if (string.IsNullOrEmpty(filter)) return Levels.ToList();
        return Levels.Where(l =>
            l.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            l.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            l.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>格式化一行级别显示（标签对齐 + 描述 + 当前级别 ✓ 标记）。</summary>
    private static string FormatItem(ReasoningLevel level, string currentLevel)
    {
        var check = level.Id == currentLevel ? " ✓" : "";
        return $"{level.Label.PadRight(10)} — {level.Description}{check}";
    }

    /// <summary>定位当前级别在列表中的索引（未命中返回 0）。</summary>
    private static int IndexOfCurrent(List<ReasoningLevel> levels, string currentLevel)
    {
        for (int i = 0; i < levels.Count; i++)
            if (levels[i].Id == currentLevel) return i;
        return 0;
    }
}
