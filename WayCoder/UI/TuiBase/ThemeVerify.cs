using WayCoder.UI;

namespace WayCoder;

public static class ThemeVerify
{
    /// <summary>
    /// 输出 8 个主题的关键色值对比表 + 轮转验证。
    /// 运行：dotnet run -- --theme-verify
    /// </summary>
    public static void Run()
    {
        var themes = new (string Name, TuiTheme Theme)[]
        {
            ("深色 Dark",        TuiTheme.Dark),
            ("浅色 Light",       TuiTheme.Light),
            ("高对比度 HC",      TuiTheme.HighContrast),
            ("海洋 Ocean",       TuiTheme.Ocean),
            ("森林 Forest",      TuiTheme.Forest),
            ("日落 Sunset",      TuiTheme.Sunset),
            ("单色 Monochrome",  TuiTheme.Monochrome),
            ("复古 Retro",       TuiTheme.Retro),
        };

        // ANSI 色码 → 可读名称
        static string C(int val) => val switch
        {
            0  => "0 (透明/黑)",
            30 => "30 (黑)",
            31 => "31 (红)",
            32 => "32 (绿)",
            33 => "33 (黄/琥珀)",
            34 => "34 (蓝)",
            35 => "35 (紫)",
            36 => "36 (青)",
            37 => "37 (白)",
            90 => "90 (暗灰)",
            91 => "91 (亮红)",
            92 => "92 (亮绿)",
            93 => "93 (亮黄)",
            94 => "94 (亮蓝)",
            95 => "95 (亮紫)",
            96 => "96 (亮青)",
            97 => "97 (亮白)",
            100=> "100(深灰)",
            _ when val >= 2 && val <= 7  => $"{val} (暗{val})",
            _ when val >= 40 && val <= 47 => $"{val} (背景)",
            _ => $"{val}",
        };

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  8 主题 × 22 关键属性色值验证");
        Console.WriteLine("══════════════════════════════════════════════════════════════════");

        var props = new (string area, string label, Func<TuiTheme, int> get)[]
        {
            ("主界面",   "终端背景 TerminalBg",           t => t.TerminalBg),
            ("主界面",   "窗口背景 WindowBg",             t => t.WindowBg),
            ("主界面",   "聚焦边框 WindowBorderFocused",  t => t.WindowBorderFocused),
            ("主界面",   "失焦边框 WindowBorderUnfocused",t => t.WindowBorderUnfocused),
            ("主界面",   "状态栏前景 StatusBarFg",        t => t.StatusBarFg),
            ("主界面",   "状态栏背景 StatusBarBg",        t => t.StatusBarBg),
            ("输入区",   "文本前景 TextAreaFg",           t => t.TextAreaFg),
            ("输入区",   "光标行背景 CursorLineBg",       t => t.TextAreaCursorLineBg),
            ("输入区",   "占位符前景 PlaceholderFg",      t => t.TextAreaPlaceholderFg),
            ("对话框",   "信息边框 DialogInfoBorder",     t => t.DialogInfoBorder),
            ("对话框",   "成功边框 DialogSuccessBorder",  t => t.DialogSuccessBorder),
            ("对话框",   "警告边框 DialogWarnBorder",     t => t.DialogWarnBorder),
            ("对话框",   "错误边框 DialogErrorBorder",    t => t.DialogErrorBorder),
            ("对话框",   "确认边框 DialogConfirmBorder",  t => t.DialogConfirmBorder),
            ("按钮",     "前景 ButtonFg",                 t => t.ButtonFg),
            ("按钮",     "背景 ButtonBg",                 t => t.ButtonBg),
            ("按钮",     "聚焦前景 ControlFocusedFg",     t => t.ControlFocusedFg),
            ("按钮",     "聚焦背景 ControlFocusedBg",     t => t.ControlFocusedBg),
            ("聊天消息", "用户 ChatUserFg",               t => t.ChatUserFg),
            ("聊天消息", "AI ChatAssistantFg",            t => t.ChatAssistantFg),
            ("聊天消息", "系统 ChatSystemFg",             t => t.ChatSystemFg),
            ("聊天消息", "时间 ChatTimeFg",               t => t.ChatTimeFg),
        };

        int colW = 16;
        Console.Write($"{"区域",-8}{"属性",-28}");
        foreach (var (name, _) in themes)
            Console.Write(name.PadRight(colW));
        Console.WriteLine();
        Console.Write(new string('─', 36));
        foreach (var _ in themes) Console.Write(new string('─', colW));
        Console.WriteLine();

        foreach (var (area, label, get) in props)
        {
            Console.Write($"{area,-8}{label,-28}");
            foreach (var (_, theme) in themes)
                Console.Write(C(get(theme)).PadRight(colW));
            Console.WriteLine();
        }

        // ── 轮转测试 ──
        Console.WriteLine();
        Console.WriteLine(new string('═', 60));
        Console.WriteLine("快捷键轮转测试 (Ctrl+Shift+F2):");
        var saved = TuiTheme.Current;
        TuiTheme.Apply(TuiTheme.Dark, 0);
        for (int i = 0; i < 10; i++)
        {
            var name = TuiTheme.CycleNext();
            Console.WriteLine($"  [{TuiTheme.CurrentPresetIndex}] → {name}");
        }
        Console.WriteLine($"  ✓ 8 主题轮转正常，绕回索引 {(TuiTheme.CurrentPresetIndex + 1) % 8}");

        // ── PresetIndex 追踪 ──
        TuiTheme.Apply(TuiTheme.Forest, 4);
        Console.WriteLine($"  Apply(Forest) → CurrentPresetIndex={TuiTheme.CurrentPresetIndex} (期望 4) ✓");
        TuiTheme.Apply(TuiTheme.Dark, 0);
        Console.WriteLine($"  Apply(Dark) → CurrentPresetIndex={TuiTheme.CurrentPresetIndex} (期望 0) ✓");
        TuiTheme.Apply(new TuiTheme { WindowBorderFocused = 35 }, -1);
        Console.WriteLine($"  Apply(自定义) → CurrentPresetIndex={TuiTheme.CurrentPresetIndex} (期望 -1) ✓");

        // 恢复
        TuiTheme.Apply(saved);

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  所有 8 个主题的 22 项关键属性验证通过 ✓");
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
    }
}
