using Microsoft.Maui.Graphics;

namespace WayCoder.Maui.Controls;

/// <summary>
/// 编辑器排版常量的**单一真源** —— 自绘画布与浮动的单行输入框必须取同一份值，
/// 否则两者在切换的瞬间会跳一下（字号差 0.5、内边距差 2px 都看得出来）。
///
/// **字体只用一个等宽族**，且按平台取名：
/// - Android 用 <c>monospace</c>：<c>FontManager.Android</c> 只认
///   <c>monospace</c>/<c>sans-serif</c>/<c>serif</c>，其它名字走 <c>Typeface.Create</c> 失败后
///   **静默回落成 Roboto（比例字体）** —— 这正是改造前 `"Courier New"` 在 Android 上的实际下场。
/// - iOS 用 <c>Courier New</c>：反过来 iOS 上 <c>UIFont.FromName("monospace")</c> 拿不到，
///   会回落成系统比例字体。
///
/// 两端各自都拿到**真正的等宽字体**；中文由平台 fallback 补齐字形，其宽度不保证正好 2 列，
/// 所以列宽一律**实测**（见 <see cref="MeasureAdvance"/>），不按「CJK=2」推算。
/// </summary>
internal static class EditorTypography
{
    /// <summary>画布与输入框共用的等宽字体名（按平台取，见类型注释）。</summary>
    public const string FontFamilyName =
#if ANDROID
        "monospace";
#else
        "Courier New";
#endif

    /// <summary>字号（磅）。可在编辑器菜单里调（加大/缩小/重置），并持久化。</summary>
    public static float FontSize { get; set; } = 13f;

    /// <summary>字号可调范围 —— 太小看不清，太大一屏放不下几行。</summary>
    public const float MinFontSize = 9f;
    public const float MaxFontSize = 28f;

    /// <summary>
    /// 行高（磅）—— **固定值，不用平台行高**：自绘的行位置必须能被「第 N 行 → y 坐标」
    /// 精确算出来（否则点击定位、光标跟随、波浪线全都对不上）。
    /// 由字号派生，所以调字号时它会自动跟着变（取整避免累积出半像素的错位）。
    /// </summary>
    public static float LineHeight => MathF.Round(FontSize * 1.40f);

    /// <summary>行号栏与文字之间的留白。</summary>
    public const float GutterRightPad = 8f;

    /// <summary>正文左内边距（行号栏之后）。</summary>
    public const float TextLeftPad = 4f;

    /// <summary>上下内边距。</summary>
    public const float VerticalPad = 2f;

    /// <summary>
    /// 「行顶 → 文本落笔点」的补偿量，**实测为 0**：<c>ICanvas.DrawText</c> 的 y 就是行顶。
    ///
    /// 这里曾经放过 12（当时从「第 1 行看不见」反推出 y 落在基线上），结果**所有文字被整体
    /// 下推半行**，高亮条却停在行顶，用户实测「光标行背景卡在两行中间」——反推错了。
    /// 留着这个常量是为了将来真在某个平台上遇到偏移时**只改这一个数**，别再散到绘图循环里。
    /// </summary>
    public const float TextBaselineOffset = 0f;

    /// <summary>制表符宽度（列）。</summary>
    public const int TabColumns = 4;

    /// <summary>超过这个字符数的行**跳过分词**：一条 minified 行上跑 tokenizer 会把一帧拖到几百毫秒。</summary>
    public const int MaxTokenizeChars = 4096;

    /// <summary>Graphics 侧的字体对象（Controls 侧用 <see cref="FontFamilyName"/> 字符串即可）。</summary>
    public static readonly Microsoft.Maui.Graphics.Font CanvasFont = new(FontFamilyName);

    /// <summary>行号栏前景色。</summary>
    public static readonly Color GutterFg = Color.FromArgb("#8A8A8E");
    public static readonly Color GutterBg = Color.FromArgb("#F2F2F4");
    public static readonly Color GutterBgDark = Color.FromArgb("#1C1C1E");
    public static readonly Color EditorBg = Color.FromArgb("#FFFFFF");
    public static readonly Color EditorBgDark = Color.FromArgb("#121214");
    // ⚠ MAUI 的 Color.FromArgb 在 8 位十六进制下按 **#AARRGGBB** 解析（alpha 在前）。
    // 写成 #RRGGBBAA 的话，「白色 6% 透明」会被当成 alpha=FF 的**不透明黄色**（FFFF10），
    // 当前行就会顶出一条刺眼的黄条；而「黑色 6%」会变成完全透明（alpha=00）等于没画。
    public static readonly Color CaretLineBg = Color.FromArgb("#05000000");        // 黑 2%
    public static readonly Color CaretLineBgDark = Color.FromArgb("#0AFFFFFF");    // 白 4%
    // 当前行高亮是**长期停在屏幕上**的东西，压得越低越不累眼睛：
    // 它的作用是「让你知道光标在哪」，不是「吸引注意力」。实测 9% 白已经嫌刺眼。
    public static readonly Color SelectionBg = Color.FromArgb("#403B82F6");        // 蓝 25%
    public static readonly Color ErrorWave = Color.FromArgb("#E5484D");
    public static readonly Color WarnWave = Color.FromArgb("#F5A524");
    public static readonly Color InfoWave = Color.FromArgb("#3B82F6");
}
