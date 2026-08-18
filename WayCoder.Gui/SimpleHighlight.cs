using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// 极简源码语法高亮（对齐 Web 的 tok-kw/str/num/fn/com 配色）。
/// 手写单遍扫描，识别注释/字符串/数字/关键字/普通标识符，输出着色 Inline。
/// </summary>
public static class SimpleHighlight
{
    // 通用编程关键字（C#/Python/JS/TS/Go/Rust/Java 等常见集合）
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "else", "elif", "for", "while", "foreach", "do", "switch", "case", "break", "continue",
        "return", "function", "def", "class", "struct", "interface", "enum", "extends", "implements",
        "public", "private", "protected", "internal", "static", "readonly", "const", "var", "let",
        "new", "this", "base", "super", "import", "from", "using", "namespace", "package",
        "void", "int", "float", "double", "decimal", "long", "short", "byte", "bool", "char", "string",
        "true", "false", "null", "None", "True", "False", "async", "await", "try", "catch", "finally",
        "throw", "throws", "typeof", "sizeof", "is", "as", "in", "of", "not", "and", "or", "with",
        "yield", "lambda", "match", "defer", "go", "range", "map", "func", "type", "val", "sealed",
        "override", "virtual", "abstract", "record", "init", "required", "global", "partial",
    };

    // Web tok 配色
    private static readonly Color Kw = Color.Parse("#ff7b72");
    private static readonly Color Str = Color.Parse("#a5d6ff");
    private static readonly Color Num = Color.Parse("#79c0ff");
    private static readonly Color Fn = Color.Parse("#d2a8ff");
    private static readonly Color Com = Color.Parse("#7d8590");
    private static readonly Color Plain = Color.Parse("#c9d1d9");

    /// <summary>把源码文本分词为着色 Inline 列表。</summary>
    public static List<Inline> Highlight(string code)
    {
        var result = new List<Inline>();
        var run = new Run();
        var buf = new System.Text.StringBuilder();

        void Flush(Color c)
        {
            if (buf.Length == 0) return;
            result.Add(new Run(buf.ToString()) { Foreground = new SolidColorBrush(c) });
            buf.Clear();
        }

        var chars = code;
        int i = 0;
        while (i < chars.Length)
        {
            char c = chars[i];

            // 行注释 // 或 #
            if ((c == '/' && i + 1 < chars.Length && chars[i + 1] == '/') || c == '#')
            {
                Flush(Plain);
                int start = i;
                while (i < chars.Length && chars[i] != '\n') i++;
                result.Add(new Run(chars[start..i]) { Foreground = new SolidColorBrush(Com) });
                continue;
            }
            // 块注释 /* */
            if (c == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
            {
                Flush(Plain);
                int start = i;
                i += 2;
                while (i < chars.Length && !(chars[i] == '*' && i + 1 < chars.Length && chars[i + 1] == '/')) i++;
                if (i < chars.Length) i += 2;
                result.Add(new Run(chars[start..i]) { Foreground = new SolidColorBrush(Com) });
                continue;
            }
            // 字符串 "..." '...'
            if (c is '"' or '\'' or '`')
            {
                Flush(Plain);
                char quote = c;
                int start = i;
                i++;
                while (i < chars.Length)
                {
                    if (chars[i] == '\\' && i + 1 < chars.Length) { i += 2; continue; }
                    if (chars[i] == quote) { i++; break; }
                    if (chars[i] == '\n') break;
                    i++;
                }
                result.Add(new Run(chars[start..i]) { Foreground = new SolidColorBrush(Str) });
                continue;
            }
            // 数字
            if (char.IsDigit(c) || (c == '.' && i + 1 < chars.Length && char.IsDigit(chars[i + 1])))
            {
                Flush(Plain);
                int start = i;
                while (i < chars.Length && (char.IsLetterOrDigit(chars[i]) || chars[i] is '.' or '_'))
                    i++;
                result.Add(new Run(chars[start..i]) { Foreground = new SolidColorBrush(Num) });
                continue;
            }
            // 标识符（关键字 / 普通）
            if (char.IsLetter(c) || c == '_')
            {
                Flush(Plain);
                int start = i;
                while (i < chars.Length && (char.IsLetterOrDigit(chars[i]) || chars[i] == '_')) i++;
                var word = chars[start..i];
                var color = Keywords.Contains(word) ? Kw : Plain;
                result.Add(new Run(word) { Foreground = new SolidColorBrush(color) });
                continue;
            }

            buf.Append(c);
            i++;
        }
        Flush(Plain);
        return result;
    }
}
