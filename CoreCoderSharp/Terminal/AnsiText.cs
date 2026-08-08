namespace CoreCoderSharp.Terminal;

/// <summary>
/// ANSI 格式化文本快捷方法 —— 一行调用生成带颜色的 ANSI 字符串。
/// 用于简单场景（权限提示、帮助文本等），复杂渲染请用 RenderBuffer。
/// </summary>
public static class AnsiText
{
    // ——— 控制码（不加文本，用于拼接）———

    public const string Reset    = "\x1b[0m";
    public const string BoldOn   = "\x1b[1m";
    public const string DimOn    = "\x1b[2m";
    public const string ClearLine = "\x1b[K";

    public static string FgCode(int code) => $"\x1b[{code}m";
    public static string BoldFgCode(int code) => $"\x1b[1;{code}m";

    // ——— 包裹文本 ———

    public static string Fg(string text, int color) => $"\x1b[{color}m{text}\x1b[0m";
    public static string FgBg(string text, int fg, int bg) => $"\x1b[{fg};{bg}m{text}\x1b[0m";
    public static string BoldFg(string text, int color) => $"\x1b[1;{color}m{text}\x1b[0m";
    public static string Accent(string text) => $"\x1b[36m{text}\x1b[0m";
    public static string Warn(string text) => $"\x1b[33m{text}\x1b[0m";
    public static string Error(string text) => $"\x1b[31m{text}\x1b[0m";
    public static string Success(string text) => $"\x1b[32m{text}\x1b[0m";
    public static string Dim(string text) => $"\x1b[2m{text}\x1b[0m";
    public static string Bold(string text) => $"\x1b[1m{text}\x1b[0m";
    public static string Heading(string text) => $"\x1b[1;33m{text}\x1b[0m";       // 标题：黄字粗体
    public static string Prompt(string text) => $"\x1b[36m{text}\x1b[0m";            // 提示：青色
    public static string BorderOpen(int fg) => $"\x1b[{fg}m";                       // 边框色开启

    public static string Strip(string text) => AnsiString.Strip(text);
}
