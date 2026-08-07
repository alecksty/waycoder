namespace CoreCoderSharp;

/// <summary>
/// 语法高亮定义 —— 按文件扩展名匹配关键词和颜色方案。
/// </summary>
public class Syntax
{
    public string Name { get; init; } = "";
    public HashSet<string> Keywords { get; init; } = [];
    private readonly List<(string Pattern, int Color)> _patterns = [];

    // ANSI 颜色码
    public const int Cyan = 36;
    public const int Green = 32;
    public const int Yellow = 33;
    public const int Magenta = 35;
    public const int Blue = 34;
    public const int Dim = 2;
    public const int Red = 31;
    public const int Default = 0;

    // 诊断标注色
    public const int ErrorBg = 41;    // 红色背景
    public const int WarningBg = 103; // 亮黄背景

    /// <summary>根据文件扩展名选择语法定义</summary>
    public static Syntax ForFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => CSharp(),
            ".js" or ".ts" or ".jsx" or ".tsx" => JavaScript(),
            ".py" => Python(),
            ".go" => Go(),
            ".rs" => Rust(),
            ".java" => Java(),
            ".c" or ".h" or ".cpp" or ".hpp" or ".cc" => Cpp(),
            ".json" or ".csproj" or ".sln" => Json(),
            ".xml" or ".html" or ".htm" or ".svg" => Xml(),
            ".md" or ".mdx" => Markdown(),
            ".sh" or ".bash" or ".zsh" => Shell(),
            ".yml" or ".yaml" => Yaml(),
            ".sql" => Sql(),
            ".css" or ".scss" => Css(),
            _ => Plain(),
        };
    }

    /// <summary>将一行文本拆分为 (text, ansiColor) 的 token 序列</summary>
    public List<(string Text, int Color)> Tokenize(string line)
    {
        var tokens = new List<(string, int)>();
        if (string.IsNullOrEmpty(line))
        {
            tokens.Add((" ", Default));
            return tokens;
        }

        int i = 0;
        while (i < line.Length)
        {
            // 字符串字面量 "..."
            if (line[i] == '"')
            {
                var end = line.IndexOf('"', i + 1);
                if (end < 0) end = line.Length - 1;
                tokens.Add((line[i..(end + 1)], Green));
                i = end + 1;
                continue;
            }

            // 单引号字符串 '...'
            if (line[i] == '\'')
            {
                var end = line.IndexOf('\'', i + 1);
                if (end < 0) end = line.Length - 1;
                tokens.Add((line[i..(end + 1)], Green));
                i = end + 1;
                continue;
            }

            // 单行注释 //
            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                tokens.Add((line[i..], Dim));
                i = line.Length;
                continue;
            }

            // 单行注释 #
            if (line[i] == '#')
            {
                tokens.Add((line[i..], Dim));
                i = line.Length;
                continue;
            }

            // 数字
            if (char.IsDigit(line[i]))
            {
                var start = i;
                while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.' || line[i] == 'x' || line[i] == 'f'))
                    i++;
                tokens.Add((line[start..i], Yellow));
                continue;
            }

            // 单词
            if (char.IsLetter(line[i]) || line[i] == '_')
            {
                var start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    i++;
                var word = line[start..i];
                tokens.Add((word, Keywords.Contains(word) ? Cyan : Default));
                continue;
            }

            // 其他字符
            tokens.Add((line[i].ToString(), Default));
            i++;
        }

        return tokens;
    }

    // ================================================================
    // 语法定义
    // ================================================================

    private static Syntax CSharp() => new()
    {
        Name = "C#",
        Keywords = [
            "abstract","as","base","bool","break","byte","case","catch","char","checked",
            "class","const","continue","decimal","default","delegate","do","double","else",
            "enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
            "long","namespace","new","null","object","operator","out","override","params",
            "private","protected","public","readonly","record","ref","return","sbyte",
            "sealed","short","sizeof","stackalloc","static","string","struct","switch",
            "this","throw","true","try","typeof","uint","ulong","unchecked","unsafe",
            "ushort","using","var","virtual","void","volatile","while","async","await",
            "from","join","let","orderby","select","where","yield","get","set","init",
            "required","global","partial","when","not","and","or","nameof","typeof","sizeof",
        ],
    };

    private static Syntax JavaScript() => new()
    {
        Name = "JavaScript",
        Keywords = [
            "async","await","break","case","catch","class","const","continue","debugger",
            "default","delete","do","else","enum","export","extends","false","finally",
            "for","function","if","import","in","instanceof","let","new","null","of",
            "return","super","switch","this","throw","true","try","typeof","var","void",
            "while","with","yield","static","get","set","from","as","interface","type",
            "implements","package","private","protected","public","readonly","abstract",
        ],
    };

    private static Syntax Python() => new()
    {
        Name = "Python",
        Keywords = [
            "False","None","True","and","as","assert","async","await","break","class",
            "continue","def","del","elif","else","except","finally","for","from","global",
            "if","import","in","is","lambda","nonlocal","not","or","pass","raise","return",
            "try","while","with","yield","match","case","self","cls",
        ],
    };

    private static Syntax Go() => new()
    {
        Name = "Go",
        Keywords = [
            "break","case","chan","const","continue","default","defer","else","fallthrough",
            "for","func","go","goto","if","import","interface","map","package","range",
            "return","select","struct","switch","type","var","nil","true","false","iota",
        ],
    };

    private static Syntax Rust() => new()
    {
        Name = "Rust",
        Keywords = [
            "as","async","await","break","const","continue","crate","dyn","else","enum",
            "extern","false","fn","for","if","impl","in","let","loop","match","mod","move",
            "mut","pub","ref","return","self","Self","static","struct","super","trait",
            "true","type","unsafe","use","where","while","yield","macro","union",
        ],
    };

    private static Syntax Java() => new()
    {
        Name = "Java",
        Keywords = [
            "abstract","assert","boolean","break","byte","case","catch","char","class",
            "const","continue","default","do","double","else","enum","extends","final",
            "finally","float","for","goto","if","implements","import","instanceof","int",
            "interface","long","native","new","package","private","protected","public",
            "return","short","static","strictfp","super","switch","synchronized","this",
            "throw","throws","transient","try","void","volatile","while","var","record",
            "sealed","permits","yield",
        ],
    };

    private static Syntax Cpp() => new()
    {
        Name = "C/C++",
        Keywords = [
            "auto","break","case","char","const","continue","default","do","double","else",
            "enum","extern","float","for","goto","if","int","long","register","return",
            "short","signed","sizeof","static","struct","switch","typedef","union",
            "unsigned","void","volatile","while","bool","catch","class","const_cast",
            "delete","dynamic_cast","explicit","false","friend","inline","mutable",
            "namespace","new","operator","private","protected","public","reinterpret_cast",
            "static_cast","template","this","throw","true","try","typeid","typename",
            "using","virtual","wchar_t","nullptr","override","final","noexcept",
            "include","define","ifdef","ifndef","endif","pragma",
        ],
    };

    private static Syntax Json() => new()
    {
        Name = "JSON",
        Keywords = ["true", "false", "null"],
    };

    private static Syntax Xml() => new()
    {
        Name = "XML/HTML",
        Keywords = [],
    };

    private static Syntax Markdown() => new()
    {
        Name = "Markdown",
        Keywords = [],
    };

    private static Syntax Shell() => new()
    {
        Name = "Shell",
        Keywords = [
            "if","then","else","elif","fi","case","esac","for","while","until","do",
            "done","in","function","return","exit","export","local","readonly","declare",
            "source","echo","cd","ls","rm","mv","cp","mkdir","cat","grep","sed","awk",
            "git","docker","npm","curl","wget","ssh","chmod","chown",
        ],
    };

    private static Syntax Yaml() => new()
    {
        Name = "YAML",
        Keywords = ["true", "false", "null", "yes", "no", "on", "off"],
    };

    private static Syntax Sql() => new()
    {
        Name = "SQL",
        Keywords = [
            "SELECT","FROM","WHERE","INSERT","UPDATE","DELETE","CREATE","ALTER","DROP",
            "TABLE","INDEX","VIEW","INTO","VALUES","SET","JOIN","LEFT","RIGHT","INNER",
            "OUTER","ON","AND","OR","NOT","NULL","IS","IN","LIKE","BETWEEN","ORDER","BY",
            "GROUP","HAVING","LIMIT","OFFSET","UNION","ALL","AS","DISTINCT","COUNT",
            "SUM","AVG","MAX","MIN","PRIMARY","KEY","FOREIGN","REFERENCES","CASCADE",
            "select","from","where","insert","update","delete","create","alter","drop",
            "table","index","view","into","values","set","join","left","right","inner",
            "on","and","or","not","null","is","in","like","order","by","group","having",
            "limit","offset","union","all","as","distinct","count","sum","avg","max","min",
        ],
    };

    private static Syntax Css() => new()
    {
        Name = "CSS",
        Keywords = [],
    };

    private static Syntax Plain() => new()
    {
        Name = "纯文本",
        Keywords = [],
    };
}
