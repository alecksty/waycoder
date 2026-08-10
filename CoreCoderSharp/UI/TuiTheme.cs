using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

/// <summary>
/// 主题配置 —— 集中管理所有 TUI 颜色和样式。
/// 所有颜色值使用 TuiColors 命名常量，不再使用魔数。
/// </summary>
public class TuiTheme
{
    // ── 单例 ──
    public static TuiTheme Default { get; } = new();
    public static TuiTheme Current { get; set; } = Default;

    // ── 终端背景 ──
    public int TerminalBg { get; set; }       // 终端默认背景（0=黑）

    // ── 管理器层 ──
    public int MaskBg { get; set; } = TuiColors.BgBrightBlack;  // 模态遮罩背景（深灰）

    // ── 窗口层 ──
    public int WindowBg { get; set; } = TuiColors.BgWhite;               // 窗口默认背景（白底）
    public int WindowBorderFocused { get; set; } = TuiColors.Cyan;       // 聚焦边框
    public int WindowBorderUnfocused { get; set; } = 8;                  // 失焦边框（隐藏）
    public int WindowTitleFg { get; set; }                               // 标题前景（0=用边框色）
    public int WindowTitleBg { get; set; }                               // 标题背景（0=用窗口色）

    // ── 对话框 ──
    public int DialogInfoBorder { get; set; } = TuiColors.Cyan;          // 信息框边框
    public int DialogSuccessBorder { get; set; } = TuiColors.Green;      // 成功框边框
    public int DialogWarnBorder { get; set; } = TuiColors.Yellow;        // 警告框边框
    public int DialogErrorBorder { get; set; } = TuiColors.Red;          // 错误框边框
    public int DialogConfirmBorder { get; set; } = TuiColors.Yellow;     // 确认框边框

    // ── 渐变预设（TrueColor RGB 码）──
    /// <summary>青→蓝渐变（信息框默认）</summary>
    public (int start, int end) GradCyanBlue => (
        AnsiTty.RgbCode(0, 230, 255),
        AnsiTty.RgbCode(0, 100, 220));
    /// <summary>绿→青渐变（成功框默认）</summary>
    public (int start, int end) GradGreenCyan => (
        AnsiTty.RgbCode(0, 255, 150),
        AnsiTty.RgbCode(0, 180, 200));
    /// <summary>橙→黄渐变（警告框默认）</summary>
    public (int start, int end) GradOrangeYellow => (
        AnsiTty.RgbCode(255, 180, 0),
        AnsiTty.RgbCode(255, 255, 80));
    /// <summary>红→橙渐变（错误框默认）</summary>
    public (int start, int end) GradRedOrange => (
        AnsiTty.RgbCode(255, 60, 60),
        AnsiTty.RgbCode(255, 150, 0));
    /// <summary>紫→粉渐变</summary>
    public (int start, int end) GradPurplePink => (
        AnsiTty.RgbCode(180, 80, 255),
        AnsiTty.RgbCode(255, 100, 200));

    // ── 按钮渐变预设（比边框亮 30%，层次区分）──
    /// <summary>按钮青→蓝（比边框亮）</summary>
    public (int start, int end) BtnCyanBlue => (
        AnsiTty.LightenRgb(AnsiTty.RgbCode(0, 230, 255), 0.3f),
        AnsiTty.LightenRgb(AnsiTty.RgbCode(0, 100, 220), 0.3f));
    /// <summary>按钮绿→青（比边框亮）</summary>
    public (int start, int end) BtnGreenCyan => (
        AnsiTty.LightenRgb(AnsiTty.RgbCode(0, 255, 150), 0.3f),
        AnsiTty.LightenRgb(AnsiTty.RgbCode(0, 180, 200), 0.3f));
    /// <summary>按钮橙→黄（比边框亮）</summary>
    public (int start, int end) BtnOrangeYellow => (
        AnsiTty.LightenRgb(AnsiTty.RgbCode(255, 180, 0), 0.3f),
        AnsiTty.LightenRgb(AnsiTty.RgbCode(255, 255, 80), 0.3f));
    /// <summary>按钮红→橙（比边框亮）</summary>
    public (int start, int end) BtnRedOrange => (
        AnsiTty.LightenRgb(AnsiTty.RgbCode(255, 60, 60), 0.3f),
        AnsiTty.LightenRgb(AnsiTty.RgbCode(255, 150, 0), 0.3f));

    // ── 控件通用 ──
    public int ControlFg { get; set; } = TuiColors.White;                // 默认前景（白）
    public int ControlBg { get; set; }                                   // 默认背景（透明）
    public int ControlFocusedFg { get; set; } = TuiColors.Black;         // 聚焦前景（黑）
    public int ControlFocusedBg { get; set; } = TuiColors.BgWhite;       // 聚焦背景（白底）
    public int ControlDisabledFg { get; set; } = TuiColors.BrightBlack;  // 禁用前景（暗灰）

    // ── 按钮 ──
    public int ButtonFg { get; set; } = TuiColors.Black;                 // 按钮文字（黑字，蓝底可读）
    public int ButtonBg { get; set; } = TuiColors.BgBlue;                // 按钮背景（蓝底）

    // ── 输入框 ──
    public int InputFg { get; set; } = TuiColors.White;
    public int InputBg { get; set; }
    public int InputCursorBg { get; set; } = TuiColors.BgBlue;           // 聚焦时输入框背景
    public int InputPlaceholderFg { get; set; } = TuiColors.BrightBlack;

    // ── 列表 ──
    public int ListFg { get; set; } = TuiColors.White;
    public int ListSelFg { get; set; } = TuiColors.Black;
    public int ListSelBg { get; set; } = TuiColors.BgCyan;

    // ── 文本区 ──
    public int TextAreaFg { get; set; } = TuiColors.White;
    public int TextAreaCursorLineBg { get; set; } = TuiColors.BgWhite;
    public int TextAreaLineNumFg { get; set; } = TuiColors.BrightBlack;
    public int TextAreaPlaceholderFg { get; set; } = TuiColors.BrightBlack;

    // ── 状态栏 ──
    public int StatusBarFg { get; set; } = TuiColors.White;
    public int StatusBarBg { get; set; } = TuiColors.BgBlue;

    // ── 聊天 ──
    public int ChatUserFg { get; set; } = TuiColors.Green;               // 用户消息
    public int ChatAssistantFg { get; set; } = TuiColors.Cyan;           // AI 消息
    public int ChatSystemFg { get; set; } = TuiColors.Yellow;            // 系统消息
    public int ChatToolFg { get; set; } = TuiColors.BrightBlack;         // 工具消息
    public int ChatTimeFg { get; set; } = TuiColors.BrightBlack;         // 时间戳
    public int ChatFooterFg { get; set; } = TuiColors.BrightBlack;       // 元信息

    // ── 代码块 ──
    public int CodeBlockFg { get; set; } = TuiColors.White;              // 代码默认色
    public int CodeBlockBorderFg { get; set; } = TuiColors.Green;        // 边框色
    public int CodeLangFg { get; set; } = TuiColors.Green;               // 语言标签色

    // ── Markdown ──
    public int MdHeadingFg { get; set; } = TuiColors.Yellow;             // 标题 # 色
    public int MdH1H2Fg { get; set; } = TuiColors.BrightWhite;           // H1-H2 亮白
    public int MdTableBorderFg { get; set; } = 2;                        // 表格边框
    public int MdListBulletFg { get; set; } = TuiColors.Yellow;          // 列表符号
    public int MdRuleFg { get; set; } = 2;                               // 分割线

    // ── 进度条 ──
    public int ProgressFilledFg { get; set; } = TuiColors.Green;         // 完成部分
    public int ProgressEmptyFg { get; set; } = TuiColors.BrightBlack;    // 未完成

    // ── 滑块轨道 ──
    public int SeekBarFilledFg { get; set; } = TuiColors.Cyan;           // 已填充轨道
    public int SeekBarEmptyFg { get; set; } = TuiColors.BrightBlack;     // 空轨道
    public int SeekBarThumbFg { get; set; } = TuiColors.Yellow;          // 滑块

    // ── 标签页 ──
    public int TabsBarBg { get; set; } = TuiColors.BgBlue;               // 标签栏背景
    public int TabsBarFg { get; set; } = TuiColors.White;                // 标签栏默认前景
    public int TabsActiveFg { get; set; } = TuiColors.Black;             // 选中标签前景
    public int TabsActiveBg { get; set; }                                // 选中标签背景（0=透明）
    public int TabsInactiveFg { get; set; } = TuiColors.BrightBlack;     // 非选中标签前景

    // ── 分割线 ──
    public int SeparatorFg { get; set; } = TuiColors.BrightBlack;

    // ── 加载动画 ──
    public int SpinnerFg { get; set; } = TuiColors.Cyan;

    // ── 横幅 ──
    public int BannerFg { get; set; } = TuiColors.Cyan;
    public int BannerSubFg { get; set; } = TuiColors.BrightBlack;

    // ── 树形视图 ──
    public int TreeViewFg { get; set; } = TuiColors.White;
    public int TreeViewSelBg { get; set; } = TuiColors.BgCyan;

    // ── 图标 ──
    public int IconUserFg { get; set; } = TuiColors.Green;
    public int IconAssistantFg { get; set; } = TuiColors.Cyan;
    public int IconSystemFg { get; set; } = TuiColors.Yellow;
    public int IconToolFg { get; set; } = TuiColors.BrightBlack;
    public int IconErrorFg { get; set; } = TuiColors.Red;
    public int IconWarnFg { get; set; } = TuiColors.Yellow;
    public int IconOkFg { get; set; } = TuiColors.Green;
    public int IconInfoFg { get; set; } = TuiColors.Cyan;
    public int IconFileFg { get; set; } = TuiColors.White;
    public int IconFolderFg { get; set; } = TuiColors.Yellow;
    public int IconLockFg { get; set; } = TuiColors.Red;

    // ════════════════════════════════════════════
    // 8 个预设主题
    // ════════════════════════════════════════════

    /// <summary>1. 深色（默认）—— 蓝底按钮白底选中 + 白字深底</summary>
    public static TuiTheme Dark => new();

    /// <summary>2. 浅色 —— 蓝聚焦 + 黑字浅底</summary>
    public static TuiTheme Light => new()
    {
        TerminalBg = TuiColors.BgWhite,
        WindowBg = 0,
        WindowBorderFocused = TuiColors.Blue,
        WindowBorderUnfocused = 8,
        ControlFg = TuiColors.Black,
        ControlFocusedBg = TuiColors.BgBlue,
        ButtonBg = TuiColors.BgWhite,
        InputCursorBg = TuiColors.BgWhite,
        TextAreaCursorLineBg = TuiColors.BgWhite,
        StatusBarBg = TuiColors.BgWhite,
        StatusBarFg = TuiColors.Black,
        ListSelBg = TuiColors.BgBlue,
    };

    /// <summary>3. 高对比度 —— 亮白边框 + 白底聚焦</summary>
    public static TuiTheme HighContrast => new()
    {
        WindowBorderFocused = TuiColors.BrightWhite,
        WindowBorderUnfocused = TuiColors.White,
        ControlFg = TuiColors.BrightWhite,
        ControlFocusedBg = TuiColors.BgWhite,
        ControlFocusedFg = TuiColors.Black,
        ButtonBg = TuiColors.BgBrightBlack,
        StatusBarBg = TuiColors.BgWhite,
        StatusBarFg = TuiColors.Black,
        ChatUserFg = TuiColors.BrightGreen,
        ChatAssistantFg = TuiColors.BrightCyan,
        ChatSystemFg = TuiColors.BrightYellow,
    };

    /// <summary>4. 海洋 —— 蓝色系，冷静专业</summary>
    public static TuiTheme Ocean => new()
    {
        WindowBorderFocused = TuiColors.Blue,
        WindowBorderUnfocused = TuiColors.Cyan,
        ControlFocusedBg = TuiColors.BgYellow,
        ControlFocusedFg = TuiColors.Black,
        ButtonBg = TuiColors.BgBlue,
        ButtonFg = TuiColors.White,
        StatusBarBg = TuiColors.BgCyan,
        StatusBarFg = TuiColors.White,
        ChatUserFg = TuiColors.Blue,
        ChatAssistantFg = TuiColors.Cyan,
        ChatSystemFg = TuiColors.Yellow,
        ListSelBg = TuiColors.BgYellow,
        DialogInfoBorder = TuiColors.Blue,
        DialogWarnBorder = TuiColors.Yellow,
        DialogConfirmBorder = TuiColors.Blue,
    };

    /// <summary>5. 森林 —— 绿色系，舒适护眼</summary>
    public static TuiTheme Forest => new()
    {
        WindowBorderFocused = TuiColors.Green,
        WindowBorderUnfocused = TuiColors.Green,    // 2→Green
        ControlFocusedBg = TuiColors.BgGreen,
        ControlFocusedFg = TuiColors.Black,
        ButtonBg = TuiColors.BgGreen,
        ButtonFg = TuiColors.White,
        StatusBarBg = TuiColors.BgGreen,
        StatusBarFg = TuiColors.White,
        ChatUserFg = TuiColors.Green,
        ChatAssistantFg = TuiColors.Cyan,
        ChatSystemFg = TuiColors.Yellow,
        ListSelBg = TuiColors.BgGreen,
        DialogInfoBorder = TuiColors.Green,
        DialogSuccessBorder = TuiColors.Green,
        DialogWarnBorder = TuiColors.Yellow,
        DialogConfirmBorder = TuiColors.Green,
    };

    /// <summary>6. 日落 —— 暖色系，橙黄基调</summary>
    public static TuiTheme Sunset => new()
    {
        WindowBorderFocused = TuiColors.Yellow,
        WindowBorderUnfocused = TuiColors.Yellow,     // 3→Yellow
        ControlFocusedBg = TuiColors.BgRed,
        ControlFocusedFg = TuiColors.White,
        ButtonBg = TuiColors.BgYellow,
        ButtonFg = TuiColors.Black,
        StatusBarBg = TuiColors.BgYellow,
        StatusBarFg = TuiColors.Black,
        ChatUserFg = TuiColors.Yellow,
        ChatAssistantFg = TuiColors.Cyan,
        ChatSystemFg = TuiColors.Green,
        ListSelBg = TuiColors.BgRed,
        DialogInfoBorder = TuiColors.Yellow,
        DialogSuccessBorder = TuiColors.Green,
        DialogWarnBorder = TuiColors.Red,
        DialogConfirmBorder = TuiColors.Yellow,
    };

    /// <summary>7. 单色 —— 灰度系，极简风格</summary>
    public static TuiTheme Monochrome => new()
    {
        WindowBorderFocused = TuiColors.White,
        WindowBorderUnfocused = TuiColors.BrightBlack,
        ControlFg = TuiColors.White,
        ControlFocusedBg = TuiColors.BgWhite,
        ControlFocusedFg = TuiColors.Black,
        ButtonBg = TuiColors.BgBrightBlack,
        ButtonFg = TuiColors.White,
        StatusBarBg = TuiColors.BgWhite,
        StatusBarFg = TuiColors.Black,
        ChatUserFg = TuiColors.White,
        ChatAssistantFg = TuiColors.BrightBlack,
        ChatSystemFg = TuiColors.BrightWhite,
        ListSelBg = TuiColors.BgWhite,
        DialogInfoBorder = TuiColors.White,
        DialogSuccessBorder = TuiColors.White,
        DialogWarnBorder = TuiColors.White,
        DialogConfirmBorder = TuiColors.White,
    };

    /// <summary>8. 复古 —— 琥珀色终端，怀旧风格</summary>
    public static TuiTheme Retro => new()
    {
        TerminalBg = 0,
        WindowBg = 0,
        WindowBorderFocused = TuiColors.Yellow,
        WindowBorderUnfocused = TuiColors.Yellow,       // dimmed yellow
        ControlFg = TuiColors.Yellow,
        ControlFocusedBg = TuiColors.BgYellow,
        ControlFocusedFg = 0,
        ButtonBg = TuiColors.BgYellow,
        ButtonFg = 0,
        StatusBarBg = TuiColors.BgYellow,
        StatusBarFg = 0,
        ChatUserFg = TuiColors.Yellow,
        ChatAssistantFg = TuiColors.BrightWhite,
        ChatSystemFg = TuiColors.BrightBlack,
        ChatTimeFg = TuiColors.Yellow,
        ChatFooterFg = TuiColors.Yellow,
        ChatToolFg = TuiColors.Yellow,
        ListFg = TuiColors.Yellow,
        ListSelFg = 0,
        ListSelBg = TuiColors.BgYellow,
        InputFg = TuiColors.Yellow,
        InputCursorBg = TuiColors.BgYellow,
        TextAreaFg = TuiColors.Yellow,
        TextAreaCursorLineBg = TuiColors.BgYellow,
        DialogInfoBorder = TuiColors.Yellow,
        DialogSuccessBorder = TuiColors.Yellow,
        DialogWarnBorder = TuiColors.Red,
        DialogConfirmBorder = TuiColors.Yellow,
        CodeBlockFg = TuiColors.Yellow,
        CodeBlockBorderFg = TuiColors.Yellow,
        CodeLangFg = TuiColors.Yellow,
        MdHeadingFg = TuiColors.BrightWhite,
        MdH1H2Fg = TuiColors.Yellow,
        MdTableBorderFg = TuiColors.Yellow,
        MdListBulletFg = TuiColors.Yellow,
        MdRuleFg = TuiColors.Yellow,
        SeparatorFg = TuiColors.Yellow,
        SpinnerFg = TuiColors.Yellow,
        TabsBarBg = TuiColors.BgYellow,
        TabsBarFg = 0,
        TabsActiveFg = 0,
        TabsActiveBg = TuiColors.BgYellow,
        TabsInactiveFg = TuiColors.Yellow,
    };

    // ── 主题列表（用于快捷键轮转）──

    /// <summary>所有预设主题（按轮转顺序）</summary>
    public static readonly TuiTheme[] Presets =
    [
        Dark, Light, HighContrast, Ocean, Forest, Sunset, Monochrome, Retro,
    ];

    /// <summary>预设名称（与 Presets 一一对应）</summary>
    public static readonly string[] PresetNames =
    [
        "深色 Dark", "浅色 Light", "高对比度 HC", "海洋 Ocean",
        "森林 Forest", "日落 Sunset", "单色 Mono", "复古 Retro",
    ];

    /// <summary>当前预设索引（-1=自定义主题）</summary>
    public static int CurrentPresetIndex { get; private set; } = 0;

    // ── 应用主题到全局 ──

    /// <summary>设置为当前全局主题</summary>
    public static void Apply(TuiTheme theme, int presetIndex = -1)
    {
        Current = theme;
        CurrentPresetIndex = presetIndex;
    }

    /// <summary>轮转到下一个预设主题，返回新主题名称</summary>
    public static string CycleNext()
    {
        var idx = (CurrentPresetIndex + 1) % Presets.Length;
        Apply(Presets[idx], idx);
        return PresetNames[idx];
    }

    /// <summary>应用命名预设</summary>
    public static void ApplyDark() => Apply(Dark, 0);
    public static void ApplyLight() => Apply(Light, 1);
    public static void ApplyHighContrast() => Apply(HighContrast, 2);

    /// <summary>从 Config 加载主题</summary>
    public static void ApplyFromConfig(Config cfg)
    {
        var preset = (cfg.ThemePreset ?? cfg.ColorScheme ?? "").ToLower();
        var theme = preset switch
        {
            "light" => Light,
            "highcontrast" or "hc" => HighContrast,
            _ => Dark,
        };
        // 覆盖自定义颜色
        if (!string.IsNullOrEmpty(cfg.BorderColor) && int.TryParse(cfg.BorderColor, out var bc))
            theme.WindowBorderFocused = bc;
        if (!string.IsNullOrEmpty(cfg.AccentColor) && int.TryParse(cfg.AccentColor, out var ac))
        {
            theme.WindowBorderFocused = ac;
            theme.ControlFocusedBg = ac;
        }
        Apply(theme);
    }
}
