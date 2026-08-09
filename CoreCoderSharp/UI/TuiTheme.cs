namespace CoreCoderSharp.UI;

/// <summary>
/// 主题配置 —— 集中管理所有 TUI 颜色和样式。
/// 覆盖 TuiManager、TuiScreen、TuiWindow、各控件的默认值。
/// </summary>
public class TuiTheme
{
    // ── 单例 ──
    public static TuiTheme Default { get; } = new();
    public static TuiTheme Current { get; set; } = Default;

    // ── 终端背景 ──
    public int TerminalBg { get; set; }       // 终端默认背景（0=黑）

    // ── 管理器层 ──
    public int MaskBg { get; set; } = 100;     // 模态遮罩背景

    // ── 窗口层 ──
    public int WindowBg { get; set; } = 7;      // 窗口默认背景
    public int WindowBorderFocused { get; set; } = 36;   // 聚焦边框
    public int WindowBorderUnfocused { get; set; } = 8;  // 失焦边框
    public int WindowTitleFg { get; set; }      // 标题前景
    public int WindowTitleBg { get; set; }      // 标题背景

    // ── 对话框 ──
    public int DialogInfoBorder { get; set; } = 36;      // 信息框边框
    public int DialogSuccessBorder { get; set; } = 32;   // 成功框边框
    public int DialogWarnBorder { get; set; } = 33;      // 警告框边框
    public int DialogErrorBorder { get; set; } = 31;     // 错误框边框
    public int DialogConfirmBorder { get; set; } = 33;   // 确认框边框

    // ── 控件通用 ──
    public int ControlFg { get; set; } = 37;     // 默认前景（白）
    public int ControlBg { get; set; }           // 默认背景（透明）
    public int ControlFocusedFg { get; set; } = 30; // 聚焦前景（黑）
    public int ControlFocusedBg { get; set; } = 46; // 聚焦背景（青）
    public int ControlDisabledFg { get; set; } = 90; // 禁用前景（暗灰）

    // ── 按钮 ──
    public int ButtonFg { get; set; } = 37;
    public int ButtonBg { get; set; } = 44;

    // ── 输入框 ──
    public int InputFg { get; set; } = 37;
    public int InputBg { get; set; }
    public int InputCursorBg { get; set; } = 44; // 聚焦时背景
    public int InputPlaceholderFg { get; set; } = 90;

    // ── 列表 ──
    public int ListFg { get; set; } = 37;
    public int ListSelFg { get; set; } = 30;
    public int ListSelBg { get; set; } = 46;

    // ── 文本区 ──
    public int TextAreaFg { get; set; } = 37;
    public int TextAreaCursorLineBg { get; set; } = 7;
    public int TextAreaLineNumFg { get; set; } = 90;
    public int TextAreaPlaceholderFg { get; set; } = 90;

    // ── 状态栏 ──
    public int StatusBarFg { get; set; } = 37;
    public int StatusBarBg { get; set; } = 44;

    // ── 聊天 ──
    public int ChatUserFg { get; set; } = 32;        // 用户消息
    public int ChatAssistantFg { get; set; } = 36;    // AI 消息
    public int ChatSystemFg { get; set; } = 33;       // 系统消息
    public int ChatToolFg { get; set; } = 90;         // 工具消息
    public int ChatTimeFg { get; set; } = 90;         // 时间戳
    public int ChatFooterFg { get; set; } = 90;       // 元信息

    // ── 代码块 ──
    public int CodeBlockFg { get; set; } = 37;        // 代码默认色
    public int CodeBlockBorderFg { get; set; } = 2;   // 边框色
    public int CodeLangFg { get; set; } = 2;          // 语言标签色

    // ── Markdown ──
    public int MdHeadingFg { get; set; } = 33;        // 标题 # 色
    public int MdH1H2Fg { get; set; } = 97;           // H1-H2 亮白
    public int MdTableBorderFg { get; set; } = 2;     // 表格边框
    public int MdListBulletFg { get; set; } = 33;     // 列表符号
    public int MdRuleFg { get; set; } = 2;            // 分割线

    // ── 进度条 ──
    public int ProgressFilledFg { get; set; } = 32;   // 完成部分
    public int ProgressEmptyFg { get; set; } = 90;    // 未完成

    // ── 图标─ ──
    public int IconUserFg { get; set; } = 32;
    public int IconAssistantFg { get; set; } = 36;
    public int IconSystemFg { get; set; } = 33;
    public int IconToolFg { get; set; } = 90;
    public int IconErrorFg { get; set; } = 31;
    public int IconWarnFg { get; set; } = 33;
    public int IconOkFg { get; set; } = 32;
    public int IconInfoFg { get; set; } = 36;
    public int IconFileFg { get; set; } = 37;
    public int IconFolderFg { get; set; } = 33;
    public int IconLockFg { get; set; } = 31;

    // ════════════════════════════════════════════
    // 预设主题
    // ════════════════════════════════════════════

    /// <summary>深色主题（默认）</summary>
    public static TuiTheme Dark => new();

    /// <summary>浅色主题</summary>
    public static TuiTheme Light => new()
    {
        TerminalBg = 7,
        WindowBg = 0,
        WindowBorderFocused = 34,
        WindowBorderUnfocused = 8,
        ControlFg = 30,
        ControlFocusedBg = 44,
        ButtonBg = 7,
        InputCursorBg = 7,
        TextAreaCursorLineBg = 7,
        StatusBarBg = 7,
        StatusBarFg = 30,
        ListSelBg = 44,
    };

    /// <summary>高对比度主题</summary>
    public static TuiTheme HighContrast => new()
    {
        WindowBorderFocused = 97,
        WindowBorderUnfocused = 37,
        ControlFg = 97,
        ControlFocusedBg = 47,
        ControlFocusedFg = 30,
        ButtonBg = 100,
        StatusBarBg = 7,
        StatusBarFg = 30,
        ChatUserFg = 92,
        ChatAssistantFg = 96,
        ChatSystemFg = 93,
    };

    // ── 应用主题到全局 ──

    /// <summary>设置为当前全局主题</summary>
    public static void Apply(TuiTheme theme)
    {
        Current = theme;
    }

    /// <summary>应用预设主题</summary>
    public static void ApplyDark() => Apply(Dark);
    public static void ApplyLight() => Apply(Light);
    public static void ApplyHighContrast() => Apply(HighContrast);

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
