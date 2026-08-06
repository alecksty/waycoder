using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 统一配色常量 —— 黄/青/灰 主色调。
/// Spectre.Console Style 对象，直接用于 Panel、Table、Markup 等控件。
/// </summary>
public static class TuiColors
{
    // 面板边框
    public static readonly Style Border = new(foreground: Color.Yellow);
    public static readonly Style SuccessBorder = new(foreground: Color.Green);
    public static readonly Style WarnBorder = new(foreground: Color.Orange3);
    public static readonly Style ErrorBorder = new(foreground: Color.Red);

    // 标题 / 头部
    public static readonly Style Heading = new(foreground: Color.Yellow, decoration: Decoration.Bold);

    // 正文
    public static readonly Style Accent = new(foreground: Color.Cyan);
    public static readonly Style Dim = new(foreground: Color.Grey);
    public static readonly Style Success = new(foreground: Color.Green);
    public static readonly Style Warn = new(foreground: Color.Orange3);
    public static readonly Style Error = new(foreground: Color.Red);

    // 表格
    public static readonly Style TableBorder = new(foreground: Color.Yellow);
    public static readonly Style TableHeading = new(foreground: Color.Yellow, decoration: Decoration.Bold);

    // Markup 快捷字符串
    public const string AccentMarkup = "cyan";
    public const string DimMarkup = "dim";
    public const string SuccessMarkup = "green";
    public const string WarnMarkup = "orange3";
    public const string ErrorMarkup = "red";
    public const string HeadingMarkup = "bold yellow";
}
