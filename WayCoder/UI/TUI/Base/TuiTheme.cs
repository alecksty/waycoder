using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui;

/// <summary>
/// 主题配置 —— 集中管理所有 TUI 颜色和样式。
/// 所有颜色值使用 AnsiColors 命名常量，不再使用魔数。
/// </summary>
public class TuiTheme
{
    // ── 单例 ──
    public static TuiTheme Default { get; } = new();
    public static TuiTheme Current { get; set; } = Default;

    // ── 终端背景 ──
    public int TerminalBg { get; set; } = AnsiColors.Black; // 终端默认背景（0=黑）

    // ── 管理器层 ──
    public int MaskBg { get; set; } = AnsiColors.BgBrightBlack; // 模态遮罩背景（深灰）

    // ── 窗口层 ──
    public int WindowBg { get; set; } = AnsiColors.BgWhite; // 窗口默认背景（白底）
    public int WindowBorderFocused { get; set; } = AnsiColors.Cyan; // 聚焦边框
    public int WindowBorderUnfocused { get; set; } = 8; // 失焦边框（隐藏）
    public int WindowTitleFg { get; set; } // 标题前景（0=用边框色）
    public int WindowTitleBg { get; set; } // 标题背景（0=用窗口色）

    // ── 对话框 ──
    public int DialogInfoBorder { get; set; } = AnsiColors.Cyan; // 信息框边框
    public int DialogSuccessBorder { get; set; } = AnsiColors.Green; // 成功框边框
    public int DialogWarnBorder { get; set; } = AnsiColors.Yellow; // 警告框边框
    public int DialogErrorBorder { get; set; } = AnsiColors.Red; // 错误框边框
    public int DialogConfirmBorder { get; set; } = AnsiColors.Yellow; // 确认框边框

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

    /// <summary>金色渐变（标题栏/状态栏默认）—— 暖金 → 琥珀</summary>
    public (int start, int end) GradTitleBar => (
        AnsiTty.RgbCode(255, 215, 0),
        AnsiTty.RgbCode(255, 140, 0));

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
    public int ControlFg { get; set; } = AnsiColors.White; // 默认前景（白）
    public int ControlBg { get; set; } // 默认背景（透明）
    public int ControlFocusedFg { get; set; } = AnsiColors.Black; // 聚焦前景（黑）
    public int ControlFocusedBg { get; set; } = AnsiColors.BgWhite; // 聚焦背景（白底）
    public int ControlDisabledFg { get; set; } = AnsiColors.BrightBlack; // 禁用前景（暗灰）

    // ── 按钮 ──
    public int ButtonFg { get; set; } = AnsiColors.Black; // 按钮文字（黑字，蓝底可读）
    public int ButtonBg { get; set; } = AnsiColors.BgBlue; // 按钮背景（蓝底）

    // ── 输入框 ──
    public int InputFg { get; set; } = AnsiColors.White;
    public int InputBg { get; set; }
    public int InputCursorBg { get; set; } = AnsiColors.BgBlue; // 聚焦时输入框背景
    public int InputPlaceholderFg { get; set; } = AnsiColors.BrightBlack;

    // ── 列表 ──
    public int ListFg { get; set; } = AnsiColors.White;
    public int ListSelFg { get; set; } = AnsiColors.Black;
    public int ListSelBg { get; set; } = AnsiColors.BgCyan;

    // ── 文本区 ──
    public int TextAreaFg { get; set; } = AnsiColors.White;
    public int TextAreaCursorLineBg { get; set; } = AnsiColors.BgWhite; // 光标行反白高亮
    public int TextAreaCursorLineFg { get; set; } = AnsiColors.Black; // 反白行文字用黑字（白底可读）
    public int TextAreaLineNumFg { get; set; } = AnsiColors.BrightBlack;
    public int TextAreaPlaceholderFg { get; set; } = AnsiColors.BrightBlack;

    // ── 状态栏 ──
    public int StatusBarFg { get; set; } = AnsiColors.White;
    public int StatusBarBg { get; set; } = AnsiColors.BgBlue;

    // ── 聊天 ──
    public int ChatUserFg { get; set; } = AnsiColors.Green; // 用户消息
    public int ChatAssistantFg { get; set; } = AnsiColors.Cyan; // AI 消息
    public int ChatSystemFg { get; set; } = AnsiColors.Yellow; // 系统消息
    public int ChatToolFg { get; set; } = AnsiColors.BrightBlack; // 工具消息
    public int ChatTimeFg { get; set; } = AnsiColors.BrightBlack; // 时间戳
    public int ChatFooterFg { get; set; } = AnsiColors.BrightBlack; // 元信息

    // ── 代码块 ──
    public int CodeBlockFg { get; set; } = AnsiColors.White; // 代码默认色
    public int CodeBlockBorderFg { get; set; } = AnsiColors.Green; // 边框色
    public int CodeLangFg { get; set; } = AnsiColors.Green; // 语言标签色

    // ── Markdown ──
    public int MdHeadingFg { get; set; } = AnsiColors.Yellow; // 标题 # 色
    public int MdH1H2Fg { get; set; } = AnsiColors.BrightWhite; // H1-H2 亮白
    public int MdTableBorderFg { get; set; } = 2; // 表格边框
    public int MdListBulletFg { get; set; } = AnsiColors.Yellow; // 列表符号
    public int MdRuleFg { get; set; } = 2; // 分割线

    // ── 进度条 ──
    public int ProgressFilledFg { get; set; } = AnsiColors.Green; // 完成部分
    public int ProgressEmptyFg { get; set; } = AnsiColors.BrightBlack; // 未完成

    // ── 滑块轨道 ──
    public int SeekBarFilledFg { get; set; } = AnsiColors.Cyan; // 已填充轨道
    public int SeekBarEmptyFg { get; set; } = AnsiColors.BrightBlack; // 空轨道
    public int SeekBarThumbFg { get; set; } = AnsiColors.Yellow; // 滑块

    // ── 标签页 ──
    public int TabsBarBg { get; set; } = AnsiColors.BgBlue; // 标签栏背景
    public int TabsBarFg { get; set; } = AnsiColors.White; // 标签栏默认前景
    public int TabsActiveFg { get; set; } = AnsiColors.Black; // 选中标签前景
    public int TabsActiveBg { get; set; } = AnsiColors.BgWhite; // 选中标签背景（白底反白高亮）
    public int TabsInactiveFg { get; set; } = AnsiColors.BrightBlack; // 非选中标签前景

    // ── 分割线 ──
    public int SeparatorFg { get; set; } = AnsiColors.BrightBlack;

    // ── 加载动画 ──
    public int SpinnerFg { get; set; } = AnsiColors.Cyan;

    // ── 横幅 ──
    public int BannerFg { get; set; } = AnsiColors.Cyan;
    public int BannerSubFg { get; set; } = AnsiColors.BrightBlack;

    // ── 树形视图 ──
    public int TreeViewFg { get; set; } = AnsiColors.White;
    public int TreeViewSelBg { get; set; } = AnsiColors.BgCyan;

    // ── 图标 ──
    public int IconUserFg { get; set; } = AnsiColors.Green;
    public int IconAssistantFg { get; set; } = AnsiColors.Cyan;
    public int IconSystemFg { get; set; } = AnsiColors.Yellow;
    public int IconToolFg { get; set; } = AnsiColors.BrightBlack;
    public int IconErrorFg { get; set; } = AnsiColors.Red;
    public int IconWarnFg { get; set; } = AnsiColors.Yellow;
    public int IconOkFg { get; set; } = AnsiColors.Green;
    public int IconInfoFg { get; set; } = AnsiColors.Cyan;
    public int IconFileFg { get; set; } = AnsiColors.White;
    public int IconFolderFg { get; set; } = AnsiColors.Yellow;
    public int IconLockFg { get; set; } = AnsiColors.Red;

    // ════════════════════════════════════════════
    // 8 个预设主题
    // ════════════════════════════════════════════

    /// <summary>1. 黄金甲（默认）—— 金色渐变标题栏 + 蓝底按钮白底选中 + 白字深底</summary>
    public static TuiTheme Dark => new();

    /// <summary>2. 浅色 —— 蓝聚焦 + 黑字浅底</summary>
    public static TuiTheme Light => new()
    {
        TerminalBg = AnsiColors.BgWhite,
        WindowBg = 0,
        WindowBorderFocused = AnsiColors.Blue,
        WindowBorderUnfocused = 8,
        ControlFg = AnsiColors.Black,
        ControlFocusedBg = AnsiColors.BgBlue,
        ControlFocusedFg = AnsiColors.White,
        ButtonBg = AnsiColors.BgWhite,
        InputFg = AnsiColors.Black, // 白底输入框用黑字，避免白字白底不可见
        InputCursorBg = AnsiColors.BgWhite,
        TextAreaFg = AnsiColors.Black,
        TextAreaCursorLineBg = AnsiColors.BgWhite,
        ListFg = AnsiColors.Black,
        TreeViewFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgWhite,
        StatusBarFg = AnsiColors.Black,
        ListSelFg = AnsiColors.White,
        ListSelBg = AnsiColors.BgBlue,
    };

    /// <summary>3. 高对比度 —— 亮白边框 + 白底聚焦</summary>
    public static TuiTheme HighContrast => new()
    {
        WindowBg = 0, // 黑底窗口，亮白字白框高对比（避免白字白底不可见）
        WindowBorderFocused = AnsiColors.BrightWhite,
        WindowBorderUnfocused = AnsiColors.White,
        ControlFg = AnsiColors.BrightWhite,
        ControlFocusedBg = AnsiColors.BgWhite,
        ControlFocusedFg = AnsiColors.Black,
        ButtonBg = AnsiColors.BgBrightBlack,
        StatusBarBg = AnsiColors.BgWhite,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.BrightGreen,
        ChatAssistantFg = AnsiColors.BrightCyan,
        ChatSystemFg = AnsiColors.BrightYellow,
    };

    /// <summary>4. 海洋 —— 蓝色系，冷静专业</summary>
    public static TuiTheme Ocean => new()
    {
        WindowBorderFocused = AnsiColors.Blue,
        WindowBorderUnfocused = AnsiColors.Cyan,
        ControlFocusedBg = AnsiColors.BgYellow,
        ControlFocusedFg = AnsiColors.Black,
        ButtonBg = AnsiColors.BgBlue,
        ButtonFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgCyan,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.BrightCyan,
        ChatAssistantFg = AnsiColors.Cyan,
        ChatSystemFg = AnsiColors.Yellow,
        ListSelBg = AnsiColors.BgYellow,
        DialogInfoBorder = AnsiColors.Blue,
        DialogWarnBorder = AnsiColors.Yellow,
        DialogConfirmBorder = AnsiColors.Blue,
    };

    /// <summary>5. 森林 —— 绿色系，舒适护眼</summary>
    public static TuiTheme Forest => new()
    {
        WindowBorderFocused = AnsiColors.Green,
        WindowBorderUnfocused = AnsiColors.Green, // 2→Green
        ControlFocusedBg = AnsiColors.BgGreen,
        ControlFocusedFg = AnsiColors.Black,
        ButtonBg = AnsiColors.BgGreen,
        ButtonFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgGreen,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.Green,
        ChatAssistantFg = AnsiColors.Cyan,
        ChatSystemFg = AnsiColors.Yellow,
        ListSelBg = AnsiColors.BgGreen,
        DialogInfoBorder = AnsiColors.Green,
        DialogSuccessBorder = AnsiColors.Green,
        DialogWarnBorder = AnsiColors.Yellow,
        DialogConfirmBorder = AnsiColors.Green,
    };

    /// <summary>6. 日落 —— 暖色系，橙黄基调</summary>
    public static TuiTheme Sunset => new()
    {
        WindowBorderFocused = AnsiColors.Yellow,
        WindowBorderUnfocused = AnsiColors.Yellow, // 3→Yellow
        ControlFocusedBg = AnsiColors.BgRed,
        ControlFocusedFg = AnsiColors.White,
        ButtonBg = AnsiColors.BgYellow,
        ButtonFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgYellow,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.Yellow,
        ChatAssistantFg = AnsiColors.Cyan,
        ChatSystemFg = AnsiColors.Green,
        ListSelFg = AnsiColors.White,
        ListSelBg = AnsiColors.BgRed,
        DialogInfoBorder = AnsiColors.Yellow,
        DialogSuccessBorder = AnsiColors.Green,
        DialogWarnBorder = AnsiColors.Red,
        DialogConfirmBorder = AnsiColors.Yellow,
    };

    /// <summary>7. 单色 —— 灰度系，极简风格</summary>
    public static TuiTheme Monochrome => new()
    {
        WindowBg = 0, // 黑底窗口，白字白框高对比（避免白字白底不可见）
        WindowBorderFocused = AnsiColors.White,
        WindowBorderUnfocused = AnsiColors.BrightBlack,
        ControlFg = AnsiColors.White,
        ControlFocusedBg = AnsiColors.BgWhite,
        ControlFocusedFg = AnsiColors.Black,
        ButtonBg = AnsiColors.BgBrightBlack,
        ButtonFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgWhite,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.White,
        ChatAssistantFg = AnsiColors.BrightBlack,
        ChatSystemFg = AnsiColors.BrightWhite,
        ListSelBg = AnsiColors.BgWhite,
        DialogInfoBorder = AnsiColors.White,
        DialogSuccessBorder = AnsiColors.White,
        DialogWarnBorder = AnsiColors.White,
        DialogConfirmBorder = AnsiColors.White,
    };

    /// <summary>8. 复古 —— 琥珀色终端，怀旧风格</summary>
    public static TuiTheme Retro => new()
    {
        TerminalBg = 0,
        WindowBg = 0,
        WindowBorderFocused = AnsiColors.Yellow,
        WindowBorderUnfocused = AnsiColors.Yellow, // dimmed yellow
        ControlFg = AnsiColors.Yellow,
        ControlFocusedBg = AnsiColors.BgYellow,
        ControlFocusedFg = AnsiColors.Black,
        ButtonBg = AnsiColors.BgYellow,
        ButtonFg = AnsiColors.Black,
        StatusBarBg = AnsiColors.BgYellow,
        StatusBarFg = AnsiColors.Black,
        ChatUserFg = AnsiColors.Yellow,
        ChatAssistantFg = AnsiColors.BrightWhite,
        ChatSystemFg = AnsiColors.BrightBlack,
        ChatTimeFg = AnsiColors.Yellow,
        ChatFooterFg = AnsiColors.Yellow,
        ChatToolFg = AnsiColors.Yellow,
        ListFg = AnsiColors.Yellow,
        ListSelFg = AnsiColors.Black,
        ListSelBg = AnsiColors.BgYellow,
        InputFg = AnsiColors.Black,
        InputCursorBg = AnsiColors.BgYellow,
        TextAreaFg = AnsiColors.Yellow,
        TextAreaCursorLineBg = AnsiColors.BgYellow,
        DialogInfoBorder = AnsiColors.Yellow,
        DialogSuccessBorder = AnsiColors.Yellow,
        DialogWarnBorder = AnsiColors.Red,
        DialogConfirmBorder = AnsiColors.Yellow,
        CodeBlockFg = AnsiColors.Yellow,
        CodeBlockBorderFg = AnsiColors.Yellow,
        CodeLangFg = AnsiColors.Yellow,
        MdHeadingFg = AnsiColors.BrightWhite,
        MdH1H2Fg = AnsiColors.Yellow,
        MdTableBorderFg = AnsiColors.Yellow,
        MdListBulletFg = AnsiColors.Yellow,
        MdRuleFg = AnsiColors.Yellow,
        SeparatorFg = AnsiColors.Yellow,
        SpinnerFg = AnsiColors.Yellow,
        TabsBarBg = 0, // 黑底标签栏，黄色文字可见
        TabsBarFg = AnsiColors.Yellow,
        TabsActiveFg = AnsiColors.Black, // 选中标签：黄底黑字
        TabsActiveBg = AnsiColors.BgYellow,
        TabsInactiveFg = AnsiColors.Yellow,
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
        "黄金甲", "浅色 Light", "高对比度 HC", "海洋 Ocean",
        "森林 Forest", "日落 Sunset", "单色 Mono", "复古 Retro",
    ];

    /// <summary>预设规范英文键（与 Presets 一一对应，用于配置存储 / 名称归一化）</summary>
    public static readonly string[] PresetKeys =
    [
        "dark", "light", "hc", "ocean", "forest", "sunset", "mono", "retro",
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

    /// <summary>把任意主题名（英文名/中文标签/旧名）归一化到规范英文键。未命中返回 null。</summary>
    public static string? NormalizeKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = name.Trim().ToLowerInvariant();

        if (key is "dark" or "default") return "dark";
        if (key is "light") return "light";
        if (key is "hc" or "highcontrast" or "high contrast") return "hc";
        if (key is "ocean") return "ocean";
        if (key is "forest") return "forest";
        if (key is "sunset") return "sunset";
        if (key is "mono" or "monochrome") return "mono";
        if (key is "retro") return "retro";

        // 中文标签（「深色 Dark」「海洋 Ocean」）回退：按中英文关键字匹配
        if (key.Contains("dark") || key.Contains("深色") || key.Contains("黄金甲") || key.Contains("gold")) return "dark";
        if (key.Contains("light") || key.Contains("浅色")) return "light";
        if (key.Contains("ocean") || key.Contains("海洋")) return "ocean";
        if (key.Contains("forest") || key.Contains("森林")) return "forest";
        if (key.Contains("sunset") || key.Contains("日落")) return "sunset";
        if (key.Contains("mono") || key.Contains("单色")) return "mono";
        if (key.Contains("retro") || key.Contains("复古")) return "retro";
        if (key.Contains("hc") || key.Contains("高对比") || key.Contains("contrast")) return "hc";
        return null;
    }

    /// <summary>按名称应用预设主题（兼容英文名/中文标签/旧名）。返回是否命中。</summary>
    public static bool ApplyByName(string? name)
    {
        var key = NormalizeKey(name);
        if (key == null) return false;
        var idx = Array.IndexOf(PresetKeys, key);
        if (idx < 0) return false;
        Apply(Presets[idx], idx);
        return true;
    }

    /// <summary>浅拷贝主题（配色字段均为 int/tuple 值类型，浅拷贝即完整独立副本）。
    /// 用于自定义颜色覆盖，避免污染静态预设实例。</summary>
    public TuiTheme Clone() => (TuiTheme)MemberwiseClone();

    /// <summary>从 Config 加载主题</summary>
    public static void ApplyFromConfig(Config cfg)
    {
        // 优先 ThemePreset，其次 ColorScheme；都未命中则默认 dark
        if (!ApplyByName(cfg.ThemePreset ?? cfg.ColorScheme))
            ApplyByName("dark");

        // 自定义颜色覆盖：克隆当前主题再改，避免污染静态预设（Current 指向共享实例）
        if (!string.IsNullOrEmpty(cfg.BorderColor) || !string.IsNullOrEmpty(cfg.AccentColor))
        {
            var clone = Current.Clone();
            if (!string.IsNullOrEmpty(cfg.BorderColor) && int.TryParse(cfg.BorderColor, out var bc))
                clone.WindowBorderFocused = bc;
            if (!string.IsNullOrEmpty(cfg.AccentColor) && int.TryParse(cfg.AccentColor, out var ac))
            {
                clone.WindowBorderFocused = ac;
                clone.ControlFocusedBg = ac;
            }

            Apply(clone, -1); // -1 = 自定义主题
        }
    }
}