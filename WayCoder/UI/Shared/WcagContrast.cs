namespace WayCoder.UI.Shared;

/// <summary>
/// WCAG 2.x 对比度数学 —— **纯数学，只吃整数**（(r,g,b) 分量或打包的 <c>0xRRGGBB</c>）。
///
/// ## ⚠ 生产路径**不再调用**本类 —— 它是**调色板的契约检查，只给自测用**
///
/// 它原先的调用方是 <c>EditorTypography.BubbleTextOn</c>（按每个气泡自己的底色在两个字色里
/// 二选一）。用户后来把气泡字色改成**跟随系统主题**（日间一律深字、夜间一律浅字），
/// 那个「二选一」的判据连同它的 `PickHigherContrast` 一起删掉了（那半截留着就是死代码）
/// ⇒ 生产链上**已经没有调用点**，现存的唯一调用点是 <c>SelfTest.Chunk27</c>。
/// 这里只剩「算对比度」这一件事，没有任何挑选/推导颜色的规则。
///
/// **留着它的理由**：那条「六个气泡底色 × 主题字色 ≥ 4.5:1」的契约（用户要「字读得清」）
/// 正是靠它才写得出来 —— 少了这个纯数学层，桌面自测就碰不到 `EditorTypography`
/// （那边的类型用了 `Microsoft.Maui.Graphics.Color`，桌面工程没有那个程序集），
/// 「亮底色 + 浅字」这类退化就只能等真机目视发现。**别删**：删了等于把这条契约的守卫也删了。
/// 顺带它也是「底色该取多深才够 4.5」的计算器（本次夜间警告底就是这么定下来的）。
///
/// ⚠ 仓里另有两处 <b>WCAG 相对亮度</b>同族实现，<b>都不是这一份的替代品</b>：
/// <list type="bullet">
/// <item><c>UI/TUI/Base/ThemeVerify.cs</c> 的私有 <c>RelativeLuminance</c>/<c>ContrastRatio</c>
/// 吃的是 **ANSI 16 色码**（要先过它自己那张 VGA 调色板表），而且整个 <c>UI/TUI/**</c>
/// 被 MAUI 工程排除 —— 移动端根本编译不到；</item>
/// <item><c>WayCoder.Maui/Markup/MarkupToFormattedString.cs</c> 的私有 <c>RelativeLuminance</c>
/// 吃的是 <c>Color</c>，只在 MAUI 侧存在，桌面自测碰不到。</item>
/// </list>
/// 三处**公式必须一致**：sRGB 反伽马那一段（<see cref="ToLinear"/>）最容易写成别的变体，
/// 而它一错，判定就会在临界色上翻面 —— 本仓的临界色不少（夜间错误底 <c>#C4382F</c> 4.74、
/// 夜间警告底 <c>#996000</c> 4.66，都离 4.5 的门槛不远）。
/// </summary>
public static class WcagContrast
{
    /// <summary>
    /// sRGB 分量（0–255）→ 线性光度分量（0.0–1.0）。
    ///
    /// 阈值用 WCAG 2.2 勘误后的 <c>0.04045</c>（不是 2.0 版的 <c>0.03928</c>）。
    /// ⚠ 对**8 位整数输入**这两个阈值给出的结果**逐位相同** —— 落在两者之间的只有
    /// <c>10.02…10.31</c> 这一段，而 0–255 里没有任何一个整数除以 255 落进去
    /// （10/255=0.0392 两个阈值都取线性、11/255=0.0431 两个都取幂）。所以这里换来换去
    /// **不会有可观测差异**，别把它当成一次「行为变更」。
    /// </summary>
    private static double ToLinear(int c)
    {
        double v = c / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    /// <summary>WCAG 相对亮度（0.0–1.0）—— 线性化后按人眼敏感度加权。</summary>
    public static double RelativeLuminance(int r, int g, int b)
        => 0.2126 * ToLinear(r) + 0.7152 * ToLinear(g) + 0.0722 * ToLinear(b);

    /// <summary>WCAG 相对亮度 —— 打包形式 <c>0xRRGGBB</c>（**不是** MAUI 那边的 <c>#AARRGGBB</c>）。</summary>
    public static double RelativeLuminance(int rgb)
        => RelativeLuminance((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

    /// <summary>
    /// 两个颜色的对比度，**1.0–21.0**（同色 = 1、纯黑对纯白 = 21）。
    /// 顺序无关（内部分出亮暗），所以传「字色在前」还是「底色在前」都一样。
    /// </summary>
    public static double Ratio(int r, int g, int b, int r2, int g2, int b2)
    {
        double l1 = RelativeLuminance(r, g, b);
        double l2 = RelativeLuminance(r2, g2, b2);
        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>对比度 —— 打包形式 <c>0xRRGGBB</c>。</summary>
    public static double Ratio(int rgb1, int rgb2) => Ratio(
        (rgb1 >> 16) & 0xFF, (rgb1 >> 8) & 0xFF, rgb1 & 0xFF,
        (rgb2 >> 16) & 0xFF, (rgb2 >> 8) & 0xFF, rgb2 & 0xFF);
}
