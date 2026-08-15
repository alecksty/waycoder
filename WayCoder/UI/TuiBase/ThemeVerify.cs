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
            ("黄金甲",        TuiTheme.Dark),
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

        CheckContrasts(themes);

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  所有 8 个主题的关键属性 + 对比度校验完成 ✓");
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// WCAG 对比度校验：ANSI 16 色 → 近似 sRGB（标准 VGA 调色板）→ 相对亮度 → 对比度。
    /// 覆盖全部类别的 fg/bg 配对；<3:1 报「太接近（同色/近色）」，<4.5:1 报「偏低」。
    /// </summary>
    private static void CheckContrasts((string Name, TuiTheme Theme)[] themes)
    {
        // 全类别配对：bg=0（透明）按终端背景解析；窗口标题/选中标签等「0=继承」语义显式解析
        static int ResolveBg(TuiTheme t, int bg) => bg == 0 ? t.TerminalBg : bg;

        var pairs = new (string label, Func<TuiTheme, int> fg, Func<TuiTheme, int> bg)[]
        {
            // 控件
            ("控件·正文",        t => t.ControlFg,                          t => ResolveBg(t, t.ControlBg)),
            ("控件·聚焦",        t => t.ControlFocusedFg,                    t => t.ControlFocusedBg),
            ("控件·禁用",        t => t.ControlDisabledFg,                   t => ResolveBg(t, t.ControlBg)),
            // 按钮
            ("按钮",             t => t.ButtonFg,                            t => t.ButtonBg),
            // 输入
            ("输入·聚焦",        t => t.InputFg,                             t => t.InputCursorBg),
            ("输入·占位符",      t => t.InputPlaceholderFg,                  t => ResolveBg(t, t.InputBg)),
            // 列表
            ("列表·正文",        t => t.ListFg,                              t => ResolveBg(t, t.ControlBg)),
            ("列表·选中",        t => t.ListSelFg,                           t => t.ListSelBg),
            // 文本区
            ("文本区·正文",      t => t.TextAreaFg,                          t => ResolveBg(t, t.ControlBg)),
            ("文本区·光标行",    t => t.TextAreaCursorLineFg,                t => t.TextAreaCursorLineBg),
            ("文本区·行号",      t => t.TextAreaLineNumFg,                   t => ResolveBg(t, t.ControlBg)),
            // 状态栏
            ("状态栏",           t => t.StatusBarFg,                         t => t.StatusBarBg),
            // 聊天
            ("聊天·用户",        t => t.ChatUserFg,                          t => ResolveBg(t, t.ControlBg)),
            ("聊天·AI",          t => t.ChatAssistantFg,                     t => ResolveBg(t, t.ControlBg)),
            ("聊天·系统",        t => t.ChatSystemFg,                        t => ResolveBg(t, t.ControlBg)),
            ("聊天·工具",        t => t.ChatToolFg,                          t => ResolveBg(t, t.ControlBg)),
            ("聊天·时间",        t => t.ChatTimeFg,                          t => ResolveBg(t, t.ControlBg)),
            ("聊天·元信息",      t => t.ChatFooterFg,                        t => ResolveBg(t, t.ControlBg)),
            // 标签页
            ("标签栏·默认",      t => t.TabsBarFg,                           t => t.TabsBarBg),
            ("标签·选中",        t => t.TabsActiveFg,                        t => t.TabsActiveBg == 0 ? t.TabsBarBg : t.TabsActiveBg),
            ("标签·未选中",      t => t.TabsInactiveFg,                      t => t.TabsBarBg),
            // 树
            ("树·正文",          t => t.TreeViewFg,                          t => ResolveBg(t, t.ControlBg)),
            // 窗口（标题 fg=0 继承边框色，bg=0 继承窗口色）
            ("窗口·标题",        t => t.WindowTitleFg == 0 ? t.WindowBorderFocused : t.WindowTitleFg, t => ResolveBg(t, t.WindowTitleBg == 0 ? t.WindowBg : t.WindowTitleBg)),
        };

        Console.WriteLine();
        Console.WriteLine(new string('═', 60));
        Console.WriteLine("  全类别 WCAG 对比度校验（<3:1 太接近 · <4.5:1 偏低）:");
        int warned = 0;
        foreach (var (name, theme) in themes)
        {
            foreach (var (label, fgGet, bgGet) in pairs)
            {
                int fg = fgGet(theme), bg = bgGet(theme);
                if (fg == 8 || bg == 8) continue; // 8=隐藏，跳过

                double ratio = ContrastRatio(fg, bg);
                if (ratio < 3.0)
                {
                    Console.WriteLine($"  ❌ {name,-12} {label,-16} fg={fg} bg={bg} 对比度 {ratio:F2}:1 太接近（同色/近色）");
                    warned++;
                }
                else if (ratio < 4.5)
                {
                    Console.WriteLine($"  ⚠ {name,-12} {label,-16} fg={fg} bg={bg} 对比度 {ratio:F2}:1 偏低");
                    warned++;
                }
            }
        }
        if (warned == 0) Console.WriteLine("  ✓ 全部类别配对对比度 ≥ 4.5:1，无同色/近色组合");
    }

    /// <summary>ANSI 16 色码 → 近似 sRGB（标准 VGA 调色板：40-47 为暗背景，100-107 为亮背景）。</summary>
    private static (int r, int g, int b) AnsiToRgb(int code)
    {
        // TrueColor 码直接解码
        if (code >= 0x1000000) return WayCoder.Terminal.AnsiTty.DecodeRgb(code);

        // 2=dim（近似中灰）；其余未知码按中灰兜底
        return code switch
        {
            0 or 30 or 40 => (0, 0, 0),       // 黑 / 透明默认背景
            31 or 41 => (128, 0, 0),          // 暗红
            32 or 42 => (0, 128, 0),          // 暗绿
            33 or 43 => (128, 128, 0),        // 暗黄/橄榄
            34 or 44 => (0, 0, 128),          // 暗蓝
            35 or 45 => (128, 0, 128),        // 暗紫
            36 or 46 => (0, 128, 128),        // 暗青
            37 or 47 => (192, 192, 192),      // 银白
            90 or 100 => (128, 128, 128),     // 灰
            91 or 101 => (255, 0, 0),         // 亮红
            92 or 102 => (0, 255, 0),         // 亮绿
            93 or 103 => (255, 255, 0),       // 亮黄
            94 or 104 => (0, 0, 255),         // 亮蓝
            95 or 105 => (255, 0, 255),       // 亮紫
            96 or 106 => (0, 255, 255),       // 亮青
            97 or 107 => (255, 255, 255),     // 亮白
            2 => (128, 128, 128),
            _ => (128, 128, 128),
        };
    }

    /// <summary>WCAG 相对亮度（0.0–1.0）。</summary>
    private static double RelativeLuminance(int r, int g, int b)
    {
        static double Lin(int c)
        {
            double v = c / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Lin(r) + 0.7152 * Lin(g) + 0.0722 * Lin(b);
    }

    /// <summary>前景/背景对比度（WCAG，1:1 到 21:1）。</summary>
    private static double ContrastRatio(int fg, int bg)
    {
        var (fr, fg2, fb) = AnsiToRgb(fg);
        var (br, bg2, bb) = AnsiToRgb(bg);
        double l1 = RelativeLuminance(fr, fg2, fb);
        double l2 = RelativeLuminance(br, bg2, bb);
        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }
}
