using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Gui;

/// <summary>
/// ANSI SGR int（Syntax.Tokenize 的中性 token）→ Avalonia 画刷。
/// 值与 Web app.js ANSI_FG/ANSI_BG、GUI MarkdownInlines.MarkupColors 同源，三端一致。
/// fg: 0=默认文本, 2=Dim(样式灰), 31红 32绿 33黄 34蓝 35品 36青；bg: 41红底 103黄底。
/// </summary>
public static class SyntaxBrushMap
{
    /// <summary>错误行背景（41）。</summary>
    public static IBrush ErrorBg => GuiColors.DiagErrorBg;

    /// <summary>警告行背景（103）。</summary>
    public static IBrush WarningBg => GuiColors.DiagWarnBg;

    /// <summary>把 ANSI int 映射为前景画刷（颜色随深浅主题）；0/未知回退默认文本画刷。</summary>
    public static IBrush ForFg(int ansi, IBrush defaultBrush) => ansi switch
    {
        0 => defaultBrush,
        36 => GuiColors.SyntaxCyan,
        32 => GuiColors.SyntaxGreen,
        33 => GuiColors.SyntaxYellow,
        35 => GuiColors.SyntaxMagenta,
        34 => GuiColors.SyntaxBlue,
        31 => GuiColors.SyntaxRed,
        2 => GuiColors.SyntaxDim,
        // 兜底：TrueColor（0x1000000|rgb）——今日 Syntax.Tokenize 不发，留扩展
        _ when (ansi & 0x1000000) != 0 => new SolidColorBrush(Color.FromRgb(
            (byte)((ansi & 0xFFFFFF) >> 16), (byte)((ansi & 0xFF00) >> 8), (byte)(ansi & 0xFF))),
        _ => defaultBrush,
    };

    /// <summary>把 ANSI int 映射为背景画刷；非背景码返回 null。</summary>
    public static IBrush? ForBg(int ansi)
        => ansi switch
        {
            41 => ErrorBg,
            103 => WarningBg,
            _ => null,
        };

    /// <summary>诊断严重度 → 背景画刷（gutter 标记）。</summary>
    public static IBrush DiagBg(Severity sev)
        => sev == Severity.Error ? ErrorBg : WarningBg;
}
