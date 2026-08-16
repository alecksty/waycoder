namespace WayCoder.UI.Tui.Edit;

/// <summary>
/// 语法高亮定义 —— 按文件扩展名匹配关键词和颜色方案。
/// </summary>
public class Syntax
{
    public string Name { get; init; } = "";
    public HashSet<string> Keywords { get; init; } = [];

    /// <summary>该语言用 # 作单行注释（Python/Shell/Ruby/YAML/PHP）</summary>
    public bool HashComments => Name is "Python" or "Shell" or "Ruby" or "YAML" or "PHP";

    /// <summary>该语言用 // 作单行注释（C 系语言）</summary>
    public bool SlashComments => Name is "C#" or "JavaScript" or "Java" or "C/C++" or "Go" or "Rust" or "Swift" or "Kotlin" or "PHP" or "Vue";

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

    /// <summary>根据语言名选择语法定义（用于代码块高亮）</summary>
    public static Syntax ByLanguage(string lang) => (lang.ToLowerInvariant()) switch
    {
        "csharp" or "cs" => CSharp(),
        "javascript" or "js" => JavaScript(),
        "typescript" or "ts" or "tsx" => JavaScript(),
        "python" or "py" => Python(),
        "go" or "golang" => Go(),
        "rust" or "rs" => Rust(),
        "java" => Java(),
        "c" or "cpp" or "c++" or "h" => Cpp(),
        "json" => Json(),
        "xml" or "html" or "svg" => Xml(),
        "markdown" or "md" => Markdown(),
        "shell" or "sh" or "bash" or "zsh" => Shell(),
        "yaml" or "yml" => Yaml(),
        "sql" => Sql(),
        "css" or "scss" => Css(),
        "ruby" or "rb" => Ruby(),
        "php" => Php(),
        "swift" => Swift(),
        "kotlin" or "kt" or "kts" => Kotlin(),
        "vue" => Vue(),
        _ => Plain(),
    };

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
            ".rb" => Ruby(),
            ".php" => Php(),
            ".swift" => Swift(),
            ".kt" or ".kts" => Kotlin(),
            ".vue" => Vue(),
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
            // Markdown 标题（行首 # / ## / ###）
            if (Name == "Markdown" && line[i] == '#' && i == 0)
            {
                int j = i;
                while (j < line.Length && line[j] == '#') j++;
                tokens.Add((line[i..j], Yellow));
                if (j < line.Length) tokens.Add((line[j..], Cyan));
                i = line.Length;
                continue;
            }

            // Markdown 行内代码 `code`
            if (Name == "Markdown" && line[i] == '`')
            {
                var end = line.IndexOf('`', i + 1);
                if (end < 0) end = line.Length - 1;
                tokens.Add((line[i..(end + 1)], Green));
                i = end + 1;
                continue;
            }

            // XML/HTML 标签 <tag ...>
            if (Name == "XML/HTML" && line[i] == '<')
            {
                var end = line.IndexOf('>', i);
                if (end < 0) { tokens.Add((line[i..], Cyan)); i = line.Length; }
                else { tokens.Add((line[i..(end + 1)], Cyan)); i = end + 1; }
                continue;
            }

            // JSON 键名（字符串后紧跟冒号）→ 紫色，区别于值
            if (Name == "JSON" && line[i] == '"')
            {
                var end = line.IndexOf('"', i + 1);
                if (end < 0) end = line.Length - 1;
                int k = end + 1;
                while (k < line.Length && line[k] == ' ') k++;
                int color = k < line.Length && line[k] == ':' ? Magenta : Green;
                tokens.Add((line[i..(end + 1)], color));
                i = end + 1;
                continue;
            }

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
            if (SlashComments && i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                tokens.Add((line[i..], Dim));
                i = line.Length;
                continue;
            }

            // 单行注释 #
            if (HashComments && line[i] == '#')
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
                bool allowDash = Name == "CSS"; // CSS 属性名含连字符（font-size）
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_' || (allowDash && line[i] == '-')))
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
        Keywords = [
            // 属性
            "color","background","border","margin","padding","width","height","display",
            "position","top","right","bottom","left","float","clear","overflow","z-index",
            "font","font-size","font-weight","font-family","font-style","text-align",
            "text-decoration","text-transform","text-indent","line-height","letter-spacing",
            "white-space","vertical-align","visibility","opacity","cursor","content",
            "outline","resize","min-width","max-width","min-height","max-height","gap",
            "flex","flex-direction","flex-wrap","flex-grow","flex-shrink","flex-basis",
            "grid","grid-template","grid-gap","justify-content","align-items","align-self",
            "justify-items","align-content","order","transition","transform","animation",
            "box-shadow","border-radius","border-collapse","border-spacing","box-sizing",
            "word-wrap","word-break","text-overflow","object-fit","pointer-events",
            // 值
            "none","block","inline","inline-block","table","absolute","relative","fixed",
            "static","sticky","hidden","visible","auto","center","start","end",
            "space-between","space-around","space-evenly","stretch","italic","bold",
            "normal","uppercase","lowercase","capitalize","underline","line-through",
            "pointer","transparent","inherit","initial","unset","revert","repeat",
            "no-repeat","cover","contain","scroll","column","row","wrap","nowrap",
        ],
    };

    private static Syntax Ruby() => new()
    {
        Name = "Ruby",
        Keywords = [
            "alias","and","begin","break","case","class","def","do","else","elsif",
            "end","ensure","false","for","if","in","module","next","nil","not","or",
            "redo","rescue","retry","return","self","super","then","true","undef",
            "unless","until","when","while","yield","require","include","extend",
            "attr_reader","attr_writer","attr_accessor","lambda","proc","puts","raise",
            "private","protected","public","initialize","new",
        ],
    };

    private static Syntax Php() => new()
    {
        Name = "PHP",
        Keywords = [
            "abstract","and","array","as","break","callable","case","catch","class",
            "clone","const","continue","declare","default","do","echo","else","elseif",
            "empty","enddeclare","endfor","endforeach","endif","endswitch","endwhile",
            "extends","final","finally","fn","for","foreach","function","global","goto",
            "if","implements","include","include_once","instanceof","insteadof","interface",
            "isset","list","match","namespace","new","or","print","private","protected",
            "public","readonly","require","require_once","return","static","switch",
            "throw","trait","try","unset","use","var","while","xor","yield","true",
            "false","null","self","parent","this",
        ],
    };

    private static Syntax Swift() => new()
    {
        Name = "Swift",
        Keywords = [
            "associatedtype","class","deinit","enum","extension","fileprivate","func",
            "import","init","inout","internal","let","open","operator","private",
            "protocol","public","rethrows","static","struct","subscript","typealias",
            "var","break","case","continue","default","defer","do","else","fallthrough",
            "for","guard","if","in","repeat","return","switch","where","while","as",
            "catch","false","is","nil","super","self","Self","throw","throws","true",
            "try","async","await","actor","some","any","weak","unowned","lazy",
            "mutating","nonmutating","override","required","convenience","final",
            "indirect","escaping","autoclosure",
        ],
    };

    private static Syntax Kotlin() => new()
    {
        Name = "Kotlin",
        Keywords = [
            "as","break","class","continue","do","else","false","for","fun","if","in",
            "interface","is","null","object","package","return","super","this","throw",
            "true","try","typealias","val","var","when","while","by","catch","constructor",
            "finally","get","import","init","set","where","actual","abstract","annotation",
            "companion","const","crossinline","data","enum","expect","external","final",
            "infix","inline","inner","internal","lateinit","noinline","open","operator",
            "out","override","private","protected","public","reified","sealed","suspend",
            "tailrec","vararg","field","it","unit",
        ],
    };

    private static Syntax Vue() => new()
    {
        Name = "Vue",
        Keywords = [
            // JavaScript（script 段）
            "async","await","break","case","catch","class","const","continue","debugger",
            "default","delete","do","else","enum","export","extends","false","finally",
            "for","function","if","import","in","instanceof","let","new","null","of",
            "return","super","switch","this","throw","true","try","typeof","var","void",
            "while","with","yield","static","get","set","from","as","interface","type",
            "implements","package","private","protected","public","readonly","abstract",
            // Vue 组合式 API 标识
            "ref","reactive","computed","watch","watchEffect","onMounted","onCreated",
            "onUnmounted","props","emits","setup","defineProps","defineEmits","toRefs",
        ],
    };

    private static Syntax Plain() => new()
    {
        Name = "纯文本",
        Keywords = [],
    };
}
