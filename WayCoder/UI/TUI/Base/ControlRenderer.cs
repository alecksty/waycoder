using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 控件渲染原语 —— 所有简单控件的绘制都走这里的 API。
///
/// 设计目标：
/// 1. 颜色解析：一处定义 override → focus → theme 的优先级
/// 2. 文本格式化：对齐 / padding / 截断
/// 3. 常用绘制：button 行、checkbox 行、进度条、分割线等
///
/// 复杂控件（List/Markdown/Editor/TreeView/ComboBox/Menu/Input 等）
/// 不在此类，保持自己的 OnRender。
/// </summary>
public static class ControlRenderer
{
    // ════════════════════════════════════════════════════════════
    // 颜色解析
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 交互控件的前景色解析。
    /// 优先级：实例 DisabledFg → 实例 FocusedFg → 实例 Fg → Theme 默认值
    /// </summary>
    public static int ResolveFg(TuiControl c, int themeDefault, int themeFocusDefault, int themeDisabledDefault)
    {
        if (!c.IsEnabled)
            return c.DisabledFg > 0 ? c.DisabledFg : (themeDisabledDefault > 0 ? themeDisabledDefault : themeDefault);
        if (c.Focused)
            return c.FocusedFg > 0 ? c.FocusedFg : (themeFocusDefault > 0 ? themeFocusDefault : themeDefault);
        return c.Fg > 0 ? c.Fg : themeDefault;
    }

    /// <summary>
    /// 交互控件的背景色解析。
    /// 优先级：实例 DisabledBg → 实例 FocusedBg → 实例 Bg → CascadedBg
    /// </summary>
    public static int ResolveBg(TuiControl c, int themeDefault, int themeFocusDefault)
    {
        if (!c.IsEnabled)
            return c.DisabledBg > 0 ? c.DisabledBg : (c.Bg > 0 ? c.Bg : TuiControl.CascadedBg);
        if (c.Focused)
            return c.FocusedBg > 0 ? c.FocusedBg : (themeFocusDefault > 0 ? themeFocusDefault : (c.Bg > 0 ? c.Bg : themeDefault));
        return c.Bg > 0 ? c.Bg : (themeDefault > 0 ? themeDefault : TuiControl.CascadedBg);
    }

    /// <summary>
    /// 纯展示控件的前景色（无 focus 概念）。
    /// 优先级：实例 Fg → Theme 默认值
    /// </summary>
    public static int ResolveStaticFg(TuiControl c, int themeDefault)
        => c.Fg > 0 ? c.Fg : themeDefault;

    /// <summary>
    /// 纯展示控件的背景色。
    /// 优先级：实例 Bg → Theme 默认值 → CascadedBg
    /// </summary>
    public static int ResolveStaticBg(TuiControl c, int themeDefault)
        => c.Bg > 0 ? c.Bg : (themeDefault > 0 ? themeDefault : TuiControl.CascadedBg);

    // ════════════════════════════════════════════════════════════
    // 文本格式化
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 按对齐方式填充文本到指定宽度。
    /// 超宽自动截断（保留视觉宽度）。
    /// </summary>
    public static string FormatAligned(string text, int width, EHAlign align)
    {
        if (string.IsNullOrEmpty(text)) return new string(' ', width);

        int vw = AnsiHelper.DisplayWidth(text);
        if (vw > width)
        {
            text = AnsiHelper.TruncateByWidth(text, width);
            vw = AnsiHelper.DisplayWidth(text);
        }

        int leftPad = align switch
        {
            EHAlign.Center => (width - vw) / 2,
            EHAlign.Right => width - vw,
            _ => 0
        };
        leftPad = Math.Max(0, leftPad);
        int rightPad = Math.Max(0, width - leftPad - vw);

        return new string(' ', leftPad) + text + new string(' ', rightPad);
    }

    /// <summary>
    /// 两端各加一个空格的内边距文本。
    /// "确定" → " 确定 "
    /// </summary>
    public static string PadText(string text) => $" {text} ";

    // ════════════════════════════════════════════════════════════
    // 常用绘制原语
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 绘制单行文本到 Screen StringBuilder。
    /// 这是最底层的绘制调用——所有简单控件都用它。
    /// </summary>
    public static void DrawLine(StringBuilder sb, int absRow, int absCol, string text, int fg, int bg)
    {
        var rb = new RenderBuffer();
        rb.Write(absRow, absCol, text, fg: fg, bg: bg);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 绘制按钮样式的单行（居中文本 + 状态感知配色）。
    /// 用于 TuiButton、TuiTabs 等。
    /// </summary>
    public static void DrawButtonLine(StringBuilder sb, TuiControl c,
        int absX, int absY, string text, EHAlign align,
        int themeFg, int themeBg, int themeFocusFg, int themeFocusBg)
    {
        int fg = ResolveFg(c, themeFg, themeFocusFg, TuiTheme.Current.ControlDisabledFg);
        int bg = ResolveBg(c, themeBg, themeFocusBg);

        var display = FormatAligned(text, c.Width, align);
        DrawLine(sb, absY, absX, display, fg, bg);
    }

    /// <summary>
    /// 绘制带渐变背景的按钮行。单次定位，逐字变色，避免错位。
    /// 焦点按钮：完整亮色渐变；非焦点按钮：暗化 50% 的渐变，视觉差异明显。
    /// </summary>
    public static void DrawButtonGradientLine(StringBuilder sb, TuiControl c,
        int absX, int absY, string text, EHAlign align,
        int themeFg, int themeFocusFg, int themeDisabledFg,
        int startBg, int endBg)
    {
        int fg = ResolveFg(c, themeFg, themeFocusFg, themeDisabledFg);

        // 非焦点按钮：渐变大幅暗化，与焦点按钮形成明显反差
        if (!c.Focused)
        {
            startBg = AnsiTty.DarkenRgb(startBg, 0.55f);
            endBg = AnsiTty.DarkenRgb(endBg, 0.55f);
        }

        var display = FormatAligned(text, c.Width, align);
        if (display.Length == 0) return;

        // 单次定位，逐字换背景色，中间不重置不重定位
        sb.Append(AnsiTty.CursorPos0(absY, absX));
        int charIdx = 0;
        foreach (var rune in display.EnumerateRunes())
        {
            float t = display.Length > 1 ? (float)charIdx / (display.Length - 1) : 0;
            int bg = AnsiTty.LerpRgb(startBg, endBg, t);
            sb.Append(AnsiTty.FgBgCode(fg, bg));
            sb.Append(rune.ToString()); // 逐 rune 输出，避免切半代理对成 U+FFFD
            charIdx += rune.Utf16SequenceLength;
        }

        // 末尾重置
        bool hasFg = fg > 0;
        if (hasFg) sb.Append(AnsiTty.SgrResetFg);
        sb.Append(AnsiTty.SgrResetBg);
    }

    /// <summary>
    /// 绘制纯展示标签行（无 focus 状态）。
    /// 用于 TuiLabel、TuiIcon、TuiSpinner 等。
    /// </summary>
    public static void DrawLabelLine(StringBuilder sb, TuiControl c,
        int absX, int absY, string text, EHAlign align,
        int themeFg, int themeBg)
    {
        int fg = ResolveStaticFg(c, themeFg);
        int bg = ResolveStaticBg(c, themeBg);
        var display = FormatAligned(text, c.Width, align);
        DrawLine(sb, absY, absX, display, fg, bg);
    }

    /// <summary>
    /// 绘制进度条样式的填充行（单色）。
    /// filledChar + emptyChar 组成完整 barWidth。
    /// 需要逐字符多色渲染的复杂 SeekBar 不走这里，保持自己的 OnRender。
    /// </summary>
    public static void DrawBarLine(StringBuilder sb, TuiControl c,
        int absX, int absY, int barWidth, int filled, string label,
        char filledChar, char emptyChar,
        int themeFg, int themeBg)
    {
        int fg = ResolveStaticFg(c, themeFg);
        int bg = ResolveStaticBg(c, themeBg);

        string bar = new string(filledChar, filled) + new string(emptyChar, barWidth - filled);
        var display = string.IsNullOrEmpty(label)
            ? bar
            : $"{label} {bar}";

        DrawLine(sb, absY, absX, display, fg, bg);
    }

    /// <summary>
    /// 绘制复选框/Radio 样式的行。
    /// marker 前缀（☑/☐/◉/○）+ label 文本。
    /// </summary>
    public static void DrawCheckLine(StringBuilder sb, TuiControl c,
        int absX, int absY, string marker, string label, EHAlign align,
        int themeFg, int themeBg, int themeFocusFg, int themeFocusBg)
    {
        int fg = ResolveFg(c, themeFg, themeFocusFg, TuiTheme.Current.ControlDisabledFg);
        int bg = ResolveBg(c, themeBg, themeFocusBg);

        var display = $"{marker} {label}";
        display = FormatAligned(display, c.Width, align);
        DrawLine(sb, absY, absX, display, fg, bg);
    }

    /// <summary>
    /// 绘制分割线（水平）。
    /// 可选居中文本，线条字符可自定义。
    /// </summary>
    public static void DrawSeparatorLine(StringBuilder sb, TuiControl c,
        int absX, int absY, string text, char lineChar,
        int themeFg, int themeBg)
    {
        int fg = ResolveStaticFg(c, themeFg);
        int bg = ResolveStaticBg(c, themeBg);

        if (!string.IsNullOrEmpty(text))
        {
            int textVw = AnsiHelper.DisplayWidth(text);
            int leftW = (c.Width - textVw - 2) / 2;
            int rightW = c.Width - textVw - 2 - leftW;
            DrawLine(sb, absY, absX, new string(lineChar, Math.Max(0, leftW)), fg, bg);
            DrawLine(sb, absY, absX + leftW, $" {text} ", fg, bg);
            DrawLine(sb, absY, absX + leftW + textVw + 2, new string(lineChar, Math.Max(0, rightW)), fg, bg);
        }
        else
        {
            DrawLine(sb, absY, absX, new string(lineChar, c.Width), fg, bg);
        }
    }

    // ════════════════════════════════════════════════════════════
    // 渐变条绘制（用于标题栏/状态栏整行渐变背景）
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 绘制整行渐变背景条（填充空格）。单次定位，逐字变色。
    /// 后续可调用 WriteGradientTextAt 在渐变条上叠写文本。
    /// </summary>
    public static void DrawGradientBarFill(StringBuilder sb, int row, int col, int width,
        int startBg, int endBg)
    {
        if (width <= 0) return;
        sb.Append(AnsiTty.CursorPos0(row, col));
        for (int i = 0; i < width; i++)
        {
            float t = width > 1 ? (float)i / (width - 1) : 0;
            int bg = AnsiTty.LerpRgb(startBg, endBg, t);
            sb.Append(AnsiTty.BgCode(bg));
            sb.Append(' ');
        }
        sb.Append(AnsiTty.SgrResetBg);
    }

    /// <summary>
    /// 在渐变背景条上写入文本。按 Rune 迭代，正确处理 CJK 双宽字符。
    /// col 为起始列，barCol/barWidth 用于计算渐变位置比例。
    /// </summary>
    public static void WriteGradientTextAt(StringBuilder sb, int row, int col, string text,
        int fg, int startBg, int endBg, int barCol, int barWidth)
    {
        if (string.IsNullOrEmpty(text)) return;
        int charCol = col;
        foreach (var rune in text.EnumerateRunes())
        {
            int rw = AnsiHelper.RuneWidth(rune);
            if (rw <= 0) continue; // 零宽字符跳过

            // 字符在渐变条范围内才绘制
            if (charCol >= barCol && charCol + rw <= barCol + barWidth)
            {
                float t = barWidth > 1 ? (float)(charCol - barCol) / (barWidth - 1) : 0;
                int bg = AnsiTty.LerpRgb(startBg, endBg, t);
                sb.Append(AnsiTty.CursorPos0(row, charCol));
                sb.Append(AnsiTty.FgBgCode(fg, bg));
                sb.Append(rune.ToString());
            }
            charCol += rw;
        }
        // 末尾重置
        if (fg > 0) sb.Append(AnsiTty.SgrResetFg);
        sb.Append(AnsiTty.SgrResetBg);
    }
}