namespace CoreCoderSharp.Terminal;

/// <summary>
/// ANSI 格式化文本快捷方法 —— 一行调用生成带颜色的 ANSI 字符串。
/// 用于简单场景（权限提示、帮助文本等），复杂渲染请用 RenderBuffer。
/// 所有转义序列委托给 AnsiTty 统一管理。
/// </summary>
public static class AnsiText
{
    // ——— 控制码（不加文本，用于拼接）———

    public const string Reset    = AnsiTty.SgrReset;
    public const string BoldOn   = AnsiTty.SgrBold;
    public const string DimOn    = AnsiTty.SgrDim;
    public const string ClearLine = AnsiTty.ClearToEnd;

    public static string FgCode(int code) => AnsiTty.FgCode(code);
    public static string BoldFgCode(int code) => AnsiTty.BoldFg(code);

    // ——— 包裹文本 ———

    public static string Fg(string text, int color) => AnsiTty.FgText(text, color);
    public static string FgBg(string text, int fg, int bg) => AnsiTty.FgBgText(text, fg, bg);
    public static string BoldFg(string text, int color) => AnsiTty.BoldFgText(text, color);
    public static string Accent(string text) => AnsiTty.Accent(text);
    public static string Warn(string text) => AnsiTty.Warn(text);
    public static string Error(string text) => AnsiTty.Error(text);
    public static string Success(string text) => AnsiTty.Success(text);
    public static string Dim(string text) => AnsiTty.DimText(text);
    public static string Bold(string text) => AnsiTty.BoldText(text);
    public static string Heading(string text) => AnsiTty.HeadingText(text);
    public static string Prompt(string text) => AnsiTty.PromptText(text);
    public static string BorderOpen(int fg) => AnsiTty.Fg(fg);

    public static string Strip(string text) => AnsiString.Strip(text);
}
