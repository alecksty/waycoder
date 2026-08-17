using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
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
        var win = new TuiWindow
        {
            Title = $"推理深度 — {modelName}",
            TitleBold = true,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = TuiTheme.Current.WindowBg,
            XScale = 0.6,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
            MinWidth = 44,
            MinHeight = 10,
            Height = 10,
        };
        // 渐变边框（紫→粉，呼应推理深度的紫色调）
        var g = TuiTheme.Current.GradPurplePink;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 当前过滤后的级别列表（搜索输入实时更新）
        var filtered = FilterLevels("");

        // 说明行
        var desc = new TuiLabel("选择模型的「思考」深度，越深推理越充分，但耗时越长")
        {
            Fg = TuiColors.Black, // 白底黑字（与 WindowBg 白底保持反差）
        };

        // 搜索框（聚焦，字母进过滤词）
        var search = new TuiInput
        {
            Height = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };

        // 级别列表
        var list = new TuiList
        {
            Items = filtered.Select(l => FormatItem(l, currentLevel)).ToList(),
            SelectedIndex = IndexOfCurrent(filtered, currentLevel),
            Height = 5,
            Fg = TuiColors.Black, // 白底黑字（与 WindowBg 白底保持反差）
        };

        // 帮助行
        var help = new TuiLabel("[↑/↓] 导航  [Enter] 确认  [Esc] 取消  [字母] 搜索  [←] 清除=默认")
        {
            Fg = TuiColors.BrightBlack,
        };

        var vbox = new TuiVBox { ChildHAlign = HAlign.Stretch };
        vbox.Add(desc);
        vbox.Add(search);
        vbox.Add(list);
        vbox.Add(help);
        win.RootView = vbox;

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
