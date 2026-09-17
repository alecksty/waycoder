using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenLib;

internal class LanguageNaming
{
    public string LabelStyle { get; set; } = "snake_lower";
    public string PrimaryPrefix { get; set; } = "";
    public List<AliasDef> Aliases { get; set; } = new();
}

internal class AliasDef
{
    public string Prefix { get; set; } = "";
    public string Module { get; set; } = "";
}

internal static class NamingConfig
{
    public static Dictionary<string, LanguageNaming> LoadOrCreate(string libRoot)
    {
        string path = Path.Combine(libRoot, "naming.json");
        if (File.Exists(path))
            return System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(path), GenLibJsonContext.Default.DictionaryStringLanguageNaming)!;

        var defaults = CreateDefaults();
        var json = System.Text.Json.JsonSerializer.Serialize(defaults, GenLibJsonContext.Default.DictionaryStringLanguageNaming);
        File.WriteAllText(path, json);
        Console.WriteLine($"  已创建 {path}（默认值，可编辑后重新生成）");
        return defaults;
    }

    static Dictionary<string, LanguageNaming> CreateDefaults()
    {
        return new()
        {
            ["c"]           = new() { LabelStyle = "snake_lower", PrimaryPrefix = "c_" },
            ["cpp"]         = new() { LabelStyle = "snake_lower", PrimaryPrefix = "cpp_" },
            ["basic"]       = new() { LabelStyle = "SCREAMING_SNAKE", PrimaryPrefix = "BASIC_" },
            ["python"]      = new() { LabelStyle = "snake_lower", PrimaryPrefix = "python_" },
            ["go"]          = new() { LabelStyle = "snake_lower", PrimaryPrefix = "go_",
                Aliases = new() { new() { Prefix = "math_", Module = "math" }, new() { Prefix = "fmt_", Module = "console" } } },
            ["pascal"]      = new() { LabelStyle = "SCREAMING_SNAKE", PrimaryPrefix = "PASCAL_" },
            ["forth"]       = new() { LabelStyle = "snake_lower", PrimaryPrefix = "forth_",
                Aliases = new() { new() { Prefix = "word_", Module = "conv" }, new() { Prefix = "word_", Module = "string" },
                    new() { Prefix = "word_", Module = "math" }, new() { Prefix = "word_", Module = "io" },
                    new() { Prefix = "word_", Module = "convert" }, new() { Prefix = "word_", Module = "printf" } } },
            ["ladder"]      = new() { LabelStyle = "SCREAMING_SNAKE", PrimaryPrefix = "LADDER_" },
            ["lua"]         = new() { LabelStyle = "snake_lower", PrimaryPrefix = "lua_" },
            ["rust"]        = new() { LabelStyle = "snake_lower", PrimaryPrefix = "rust_" },
            ["java"]        = new() { LabelStyle = "pascalCase_method", PrimaryPrefix = "",
                Aliases = new() { new() { Prefix = "Math_", Module = "math" }, new() { Prefix = "String_", Module = "string" } } },
            ["csharp"]      = new() { LabelStyle = "PascalCase_method", PrimaryPrefix = "",
                Aliases = new() { new() { Prefix = "Math_", Module = "math" }, new() { Prefix = "String_", Module = "string" } } },
            ["javascript"]  = new() { LabelStyle = "pascalCase_method", PrimaryPrefix = "",
                Aliases = new() { new() { Prefix = "Math_", Module = "math" }, new() { Prefix = "String_", Module = "string" } } },
            ["swift"]       = new() { LabelStyle = "snake_lower", PrimaryPrefix = "swift_" },
            ["kotlin"]      = new() { LabelStyle = "snake_lower", PrimaryPrefix = "kotlin_" },
            ["scheme"]      = new() { LabelStyle = "snake_lower", PrimaryPrefix = "scheme_" },
            ["ruby"]        = new() { LabelStyle = "snake_lower", PrimaryPrefix = "ruby_" },
            ["dart"]        = new() { LabelStyle = "snake_lower", PrimaryPrefix = "dart_" },
            ["objc"]        = new() { LabelStyle = "snake_lower", PrimaryPrefix = "objc_" },
            ["r"]           = new() { LabelStyle = "snake_lower", PrimaryPrefix = "r_" },
            ["d"]           = new() { LabelStyle = "snake_lower", PrimaryPrefix = "d_" },
            ["fortran"]     = new() { LabelStyle = "snake_lower", PrimaryPrefix = "fortran_" },
        };
    }

    /// <summary>将函数名转为语言特定的标签名</summary>
    public static string ToLabel(string funcName, LanguageNaming naming)
    {
        // v1.66.55+: 共享库使用裸名，无需去除前缀
        string baseName = funcName;

        return naming.LabelStyle switch
        {
            "SCREAMING_SNAKE" => naming.PrimaryPrefix + baseName.ToUpper(),
            "PascalCase_method" => naming.PrimaryPrefix + ToPascalCase(baseName),
            "pascalCase_method" => naming.PrimaryPrefix + ToCamelCase(baseName, methodStyle: true),
            "kebab-case" => naming.PrimaryPrefix + baseName.Replace('_', '-'),
            _ => naming.PrimaryPrefix + baseName // snake_lower default
        };
    }

    /// <summary>将类前缀别名标签</summary>
    public static string ToAliasLabel(string funcName, string aliasPrefix)
    {
        // v1.66.55+: 共享库使用裸名
        string baseName = funcName;
        return aliasPrefix + ToPascalCase(baseName);
    }

    internal static string ToPascalCase(string snake) =>
        string.Concat(snake.Split('_').Select(s => s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : ""));

    static string ToCamelCase(string snake, bool methodStyle)
    {
        var pascal = ToPascalCase(snake);
        if (pascal.Length == 0) return "";
        return methodStyle ? char.ToLower(pascal[0]) + pascal[1..] : pascal;
    }

    /// <summary>
    /// Apply a named style to a snake_case function name.
    /// Used by NativeBindingGenerator for --style option.
    /// </summary>
    public static string ApplyStyle(string snakeName, string style)
    {
        return style switch
        {
            "OneTwo" => ToPascalCase(snakeName),
            "oneTwo" => ToCamelCase(snakeName, methodStyle: true),
            "ONETWO" => snakeName.Replace("_", "").ToUpper(),
            "One_Two" => string.Join("_", snakeName.Split('_').Select(s =>
                s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : "")),
            "ONE_TWO" => snakeName.ToUpper(),
            "onetwo" => snakeName.Replace("_", "").ToLower(),
            _ => snakeName // one_two (default)
        };
    }
}
