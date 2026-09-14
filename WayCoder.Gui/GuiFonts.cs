using System.Globalization;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// GUI 全局字体 —— **编辑器必须用「拉丁 0.5em、汉字 1em」的等宽字体**。
///
/// 这不是审美选择，是网格模型的前提：编辑器把列宽定成 <c>字号 ÷ 2</c>（半角 1 列、
/// 汉字 2 列），而**拉丁等宽字体（Menlo / Monaco / SF Mono）的步进普遍是 0.6em**，
/// 与网格对不上 —— 用它们等于重演 MAUI 那八轮的「两把尺子」。
///
/// 这里内嵌 Sarasa Mono SC。实测（直接读 TTF 的 cmap + hmtx）：
/// 拉丁 <c>a</c>/<c>W</c>/<c>0</c> = **0.5000em**，汉字 <c>中</c>/<c>文</c> 与全角标点 = **1.0000em**
/// —— 「汉字 = 2 列」精确成立。（emoji 该字体无字形，靠系统 fallback。）
///
/// ⚠ **字体 URI 里的族名必须与字体内部一致**（<c>name</c> 表 nameID 1）。写错**不报错**，
/// 只会静默回落成系统字体 —— 那就正好复现我们要修的那个 bug。
/// 当前内部族名 = <c>Sarasa Mono SC</c>（从 TTF name 表读出，不是猜的）。
/// </summary>
public static class GuiFonts
{
    /// <summary>内嵌等宽家族（AssemblyName = <c>waycoder-gui</c>）。</summary>
    public static readonly FontFamily Mono = new("avares://waycoder-gui/Assets/Fonts#Sarasa Mono SC");

    /// <summary>编辑器绘制用的等宽 Typeface。</summary>
    public static readonly Typeface MonoTypeface = new(Mono);

    /// <summary>
    /// 自检：内嵌字体**到底加载上没有**。
    ///
    /// 这道检查必须留 —— URI 里族名写错时 Avalonia **不报错**，只是静默回落成系统字体，
    /// 而症状与我们要修的完全一样：测量与渲染用了两个字体、汉字不再正好 2 列。
    /// 判据用字体自己的设计比例（实测 0.5em / 1em）：半角宽 ≈ 字号 ÷ 2、全角宽 ≈ 字号。
    /// </summary>
    public static bool Verify(double fontSize = 13, Action<string>? log = null)
    {
        double half = Measure("a", fontSize);
        double wide = Measure("中", fontSize);
        bool ok = Math.Abs(half - fontSize / 2) < 0.6 && Math.Abs(wide - fontSize) < 0.6;
        log?.Invoke(ok
            ? $"[字体自检] OK  内嵌 Sarasa 已加载：a={half:F2}（期望 {fontSize / 2:F2}） 中={wide:F2}（期望 {fontSize:F2}）"
            : $"[字体自检] ❌ 回落了！a={half:F2} 中={wide:F2}，期望 {fontSize / 2:F2}/{fontSize:F2} —— 检查 GuiFonts.Mono 的 avares URI 与族名");
        return ok;
    }

    private static double Measure(string text, double size)
        => new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                             MonoTypeface, size, Brushes.White).Width;

    static GuiFonts() => Verify(13, msg => Console.Error.WriteLine(msg));
}
