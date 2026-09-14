namespace CompilerBase;

/// <summary>
/// 函数名规范化工具 — 统一 snake_case / camelCase / arrow-notation 双向转换。
///
/// 转换规则:
///   canonical (snake_case)  →  camelCase    →  arrow-notation (Scheme)
///   int_to_str              →  intToStr      →  int->string
///   float_to_str            →  floatToStr    →  float->string
///   str_to_int              →  strToInt      →  string->int
///
/// 反向: 任意变体 → Normalize() → canonical snake_case
/// </summary>
public static class FunctionNameNormalizer
{
    /// <summary>类型名缩写映射 (完整名 → 缩写)</summary>
    private static readonly Dictionary<string, string> TypeAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = "str",   ["str"] = "str",
        ["boolean"] = "bool", ["bool"] = "bool",
        ["integer"] = "int",  ["int"] = "int",
        ["float"] = "float",  ["flt"] = "float",
        ["double"] = "double",["dbl"] = "double",
        ["long"] = "long",    ["ulong"] = "ulong",
        ["short"] = "short",  ["ushort"] = "ushort",
        ["byte"] = "byte",    ["sbyte"] = "sbyte",
        ["uint"] = "uint",    ["char"] = "char",
        ["wstr"] = "wstr",    ["ustr"] = "ustr",
        ["character"] = "char",
    };

    /// <summary>
    /// 将任意命名风格的函数名规范化为 canonical snake_case。
    /// intToStr → int_to_str  /  int->string → int_to_str  /  int_to_str → int_to_str
    /// 无法识别时返回 null。
    /// </summary>
    public static string? Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // 1. 已是标准 snake_case (含 _ 且无 ->)
        if (name.Contains('_') && !name.Contains("->"))
            return name;

        // 2. arrow-notation: int->string, float->string, string->int
        if (name.Contains("->"))
            return ConvertArrowToSnake(name);

        // 3. camelCase / PascalCase: intToStr, IntToStr, strToInt
        if (name.Any(char.IsUpper) || char.IsLower(name[0]))
            return ConvertCamelToSnake(name);

        return null; // 无法识别
    }

    /// <summary>
    /// 从 canonical snake_case 生成所有命名变体。
    /// </summary>
    public static FunctionNameVariants GetVariants(string canonicalName)
    {
        return new FunctionNameVariants
        {
            Canonical = canonicalName,
            CamelCase = SnakeToCamel(canonicalName),
            PascalCase = SnakeToPascal(canonicalName),
            ArrowNotation = SnakeToArrow(canonicalName),
        };
    }

    // ===== 转换实现 =====

    /// <summary>arrow-notation → snake_case: int->string → int_to_str</summary>
    private static string ConvertArrowToSnake(string name)
    {
        var parts = name.Split("->");
        if (parts.Length != 2) return name.Replace("->", "_to_");
        var fromType = Abbreviate(parts[0].Trim());
        var toType = Abbreviate(parts[1].Trim());
        return $"{fromType}_to_{toType}";
    }

    /// <summary>camelCase/PascalCase → snake_case: intToStr → int_to_str, IntToStr → int_to_str</summary>
    private static string ConvertCamelToSnake(string name)
    {
        // 在大小写边界插入 _
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !(i + 1 < name.Length && char.IsUpper(name[i + 1])))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        var snake = sb.ToString();

        // 缩写化类型名
        return AbbreviateSnakeParts(snake);
    }

    /// <summary>snake_case → camelCase: int_to_str → intToStr</summary>
    private static string SnakeToCamel(string snake)
    {
        var parts = snake.Split('_');
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            sb.Append(i == 0
                ? parts[i].ToLowerInvariant()
                : char.ToUpperInvariant(parts[i][0]) + parts[i][1..].ToLowerInvariant());
        }
        return sb.ToString();
    }

    /// <summary>snake_case → PascalCase: int_to_str → IntToStr</summary>
    private static string SnakeToPascal(string snake)
    {
        var parts = snake.Split('_');
        var sb = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant());
        }
        return sb.ToString();
    }

    /// <summary>snake_case → arrow-notation: int_to_str → int->string, str_to_int → string->int</summary>
    private static string SnakeToArrow(string snake)
    {
        // 查找 _to_ 分隔符
        int toIdx = FindToSeparator(snake);
        if (toIdx < 0) return snake;

        var fromPart = snake[..toIdx];
        var toPart = snake[(toIdx + 4)..]; // skip "_to_"

        var fromType = Expand(fromPart);
        var toType = Expand(toPart);
        return $"{fromType}->{toType}";
    }

    // ===== 辅助方法 =====

    /// <summary>在 snake_case 名中找到 _to_ 分隔符位置，未找到返回 -1</summary>
    private static int FindToSeparator(string snake)
    {
        var parts = snake.Split('_');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i + 1] == "to" && i + 2 < parts.Length)
                return string.Join('_', parts.Take(i + 1)).Length;
        }
        return -1;
    }

    /// <summary>缩写类型名: string→str, boolean→bool, integer→int</summary>
    private static string Abbreviate(string typeName)
    {
        return TypeAbbreviations.TryGetValue(typeName, out var abbr) ? abbr : typeName.ToLowerInvariant();
    }

    /// <summary>展开类型名: str→string, bool→boolean, int→integer (主要用于 arrow-notation)</summary>
    private static string Expand(string abbr)
    {
        return abbr.ToLowerInvariant() switch
        {
            "str" => "string",
            "bool" => "boolean",
            "int" => "integer",
            "flt" => "float",
            "dbl" => "double",
            "char" => "character",
            _ => abbr.ToLowerInvariant(),
        };
    }

    /// <summary>缩写化 snake_case 名中的各个类型部分</summary>
    private static string AbbreviateSnakeParts(string snake)
    {
        int toIdx = FindToSeparator(snake);
        if (toIdx < 0)
        {
            // 无 _to_ 分隔，全名缩写
            return Abbreviate(snake);
        }

        var fromPart = snake[..toIdx];
        var toPart = snake[(toIdx + 4)..]; // skip "_to_"
        return $"{Abbreviate(fromPart)}_to_{Abbreviate(toPart)}";
    }
}

/// <summary>函数名的各种命名变体</summary>
public class FunctionNameVariants
{
    public string Canonical { get; init; } = "";      // int_to_str
    public string CamelCase { get; init; } = "";       // intToStr
    public string PascalCase { get; init; } = "";      // IntToStr
    public string ArrowNotation { get; init; } = "";   // int->string

    /// <summary>所有变体 (不含 canonical)</summary>
    public IEnumerable<string> AllVariants
    {
        get
        {
            yield return CamelCase;
            yield return PascalCase;
            yield return ArrowNotation;
        }
    }
}
