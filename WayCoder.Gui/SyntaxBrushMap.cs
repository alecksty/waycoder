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
    private static readonly Dictionary<int, IBrush> Fg = new()
    {
        [36] = new SolidColorBrush(Color.Parse("#39c5cf")), // cyan
        [32] = new SolidColorBrush(Color.Parse("#3fb950")), // green
        [33] = new SolidColorBrush(Color.Parse("#d29922")), // yellow
        [35] = new SolidColorBrush(Color.Parse("#bc8cff")), // magenta
        [34] = new SolidColorBrush(Color.Parse("#58a6ff")), // blue
        [31] = new SolidColorBrush(Color.Parse("#ff7b72")), // red
        [2]  = new SolidColorBrush(Color.Parse("#6e7681")), // dim → 灰
    };

    /// <summary>错误行背景（41）。</summary>
    public static readonly IBrush ErrorBg = new SolidColorBrush(Color.Parse("#6e2222"));

    /// <summary>警告行背景（103）。</summary>
    public static readonly IBrush WarningBg = new SolidColorBrush(Color.Parse("#6e5c2e"));

    /// <summary>把 ANSI int 映射为前景画刷；0/未知回退默认文本画刷。</summary>
    public static IBrush ForFg(int ansi, IBrush defaultBrush)
    {
        if (ansi == 0) return defaultBrush;
        if (Fg.TryGetValue(ansi, out var b)) return b;
        // 兜底：TrueColor（0x1000000|rgb）——今日 Syntax.Tokenize 不发，留扩展
        if ((ansi & 0x1000000) != 0)
        {
            var rgb = ansi & 0xFFFFFF;
            return new SolidColorBrush(Color.FromRgb(
                (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        }
        return defaultBrush;
    }

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
