namespace WayCoder.Terminal;

/// <summary>
/// ANSI 格式化文本快捷方法 —— 一行调用生成带颜色的 ANSI 字符串。
/// 用于简单场景（权限提示、帮助文本等），复杂渲染请用 RenderBuffer。
/// 所有转义序列委托给 AnsiTty 统一管理。
/// </summary>
public static class AnsiText
{
    // ——— 控制码（不加文本，用于拼接）———

    // public const string Reset = AnsiTty.SgrReset;
    // public const string BoldOn = AnsiTty.SgrBold;
    // public const string DimOn = AnsiTty.SgrDim;
    // public const string ClearLine = AnsiTty.ClearToEnd;

    /// <summary>
    /// 获取前景色码。
    /// </summary>
    /// <param name="code">颜色码。</param>
    /// <returns>前景色码。</returns>
    public static string FgCode(int code) => AnsiTty.FgCode(code);
    
    /// <summary>
    /// 获取加粗前景色码。
    /// </summary>
    /// <param name="code">颜色码。</param>
    /// <returns>加粗前景色码。</returns>
    public static string BoldFgCode(int code) => AnsiTty.BoldFg(code);

    // ——— 包裹文本 ———

    /// <summary>
    /// 包裹文本为指定颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <param name="color">颜色码。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Fg(string text, int color) => AnsiTty.FgText(text, color);

    /// <summary>
    /// 包裹文本为指定前景色和背景色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <param name="fg">前景色码。</param>
    /// <param name="bg">背景色码。</param>
    /// <returns>包裹后的文本。</returns>
    public static string FgBg(string text, int fg, int bg) => AnsiTty.FgBgText(text, fg, bg);

    /// <summary>
    /// 包裹文本为指定颜色（加粗）。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <param name="color">颜色码。</param>
    /// <returns>包裹后的文本。</returns>
    public static string BoldFg(string text, int color) => AnsiTty.BoldFgText(text, color);

    /// <summary>
    /// 包裹文本为指定颜色（加粗）。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Accent(string text) => AnsiTty.Accent(text);

    /// <summary>
    /// 包裹文本为警告颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Warn(string text) => AnsiTty.Warn(text);

    /// <summary>
    /// 包裹文本为错误颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Error(string text) => AnsiTty.Error(text);

    /// <summary>
    /// 包裹文本为成功颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Success(string text) => AnsiTty.Success(text);

    /// <summary>
    /// 包裹文本为变暗颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Dim(string text) => AnsiTty.DimText(text);

    /// <summary>
    /// 包裹文本为加粗颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Bold(string text) => AnsiTty.BoldText(text);

    /// <summary>
    /// 包裹文本为标题颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Heading(string text) => AnsiTty.HeadingText(text);

    /// <summary>
    /// 包裹文本为提示颜色。
    /// </summary>
    /// <param name="text">待包裹的文本。</param>
    /// <returns>包裹后的文本。</returns>
    public static string Prompt(string text) => AnsiTty.PromptText(text);

    /// <summary>
    /// 包裹文本为边框颜色。
    /// </summary>
    /// <param name="fg">边框颜色码。</param>
    /// <returns>包裹后的文本。</returns>
    public static string BorderOpen(int fg) => AnsiTty.Fg(fg);

    /// <summary>
    /// 从字符串中剥离 ANSI 转义序列。
    /// </summary>
    /// <param name="text">待剥离的字符串。</param>
    /// <returns>剥离后的字符串。</returns>
    public static string Strip(string text) => AnsiString.Strip(text);
}