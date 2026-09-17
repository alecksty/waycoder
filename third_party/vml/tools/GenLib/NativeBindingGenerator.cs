using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenLib;

/// <summary>
/// Native binding generator — generates language-specific source files
/// that wrap shared C library functions with native declarations.
///
/// Supports --class (class/module name), --style (naming convention),
/// --prefix (language prefix), --file (output path override).
/// </summary>
internal class NativeBindingGenerator
{
    readonly List<FuncDef> _funcs;
    readonly string _className;
    readonly string _outputFile;
    readonly bool _usePrefix;
    readonly string _style;
    readonly string _libRoot;

    public NativeBindingGenerator(List<FuncDef> funcs, string libRoot,
        string className = "", string outputFile = "",
        bool usePrefix = false, string style = "one_two")
    {
        _funcs = funcs;
        _libRoot = libRoot;
        _className = className;
        _outputFile = outputFile;
        _usePrefix = usePrefix;
        _style = style;
    }

    public void Generate(string lang)
    {
        var dir = Path.Combine(_libRoot, lang);
        if (!Directory.Exists(dir)) return;

        switch (lang)
        {
            case "c": GenC(dir); break;
            case "cpp": GenCpp(dir); break;
            case "csharp": GenCSharp(dir); break;
            case "java": GenJava(dir); break;
            case "kotlin": GenKotlin(dir); break;
            case "dart": GenDart(dir); break;
            case "pascal": GenPascal(dir); break;
            case "go": GenGo(dir); break;
            case "python": GenPython(dir); break;
            case "basic": GenBasic(dir); break;
            case "rust": GenRust(dir); break;
            case "javascript": GenJavaScript(dir); break;
            case "lua": GenLua(dir); break;
            case "swift": GenSwift(dir); break;
            case "ruby": GenRuby(dir); break;
            case "scheme": GenScheme(dir); break;
            case "forth": GenForth(dir); break;
            case "d": GenD(dir); break;
            case "objc": GenObjC(dir); break;
            case "fortran": GenFortran(dir); break;
            case "r": GenR(dir); break;
            case "ladder": GenLadder(dir); break;
            default: return;
        }
        Console.WriteLine($"  {lang}: OK");
    }

    // ============================================================
    // Naming style helpers
    // ============================================================

    string ApplyStyle(string snakeName)
    {
        return _style switch
        {
            "OneTwo" => ToPascalCase(snakeName),
            "oneTwo" => ToCamelCase(snakeName),
            "ONETWO" => snakeName.Replace("_", "").ToUpper(),
            "One_Two" => ToPascalSnake(snakeName),
            "ONE_TWO" => snakeName.ToUpper(),
            "onetwo" => snakeName.Replace("_", "").ToLower(),
            _ => snakeName // one_two (default)
        };
    }

    static string ToPascalCase(string snake)
    {
        var parts = snake.Split('_');
        return string.Concat(parts.Select(s => s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : ""));
    }

    static string ToCamelCase(string snake)
    {
        var pascal = ToPascalCase(snake);
        if (pascal.Length == 0) return "";
        return char.ToLower(pascal[0]) + pascal[1..];
    }

    static string ToPascalSnake(string snake)
    {
        return string.Join("_", snake.Split('_').Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : ""));
    }

    string FuncLabel(string cName) => _usePrefix ? $"{_className.ToLower()}_{cName}" : cName;

    // ============================================================
    // Type mapping
    // ============================================================

    static string MapType(string cType, string lang)
    {
        var t = cType.Trim();
        // Normalize
        t = t.Replace("unsigned int", "uint")
             .Replace("unsigned short", "ushort")
             .Replace("unsigned char", "byte")
             .Replace("signed char", "sbyte")
             .Replace("unsigned long", "ulong")
             .Replace("long long", "long")
             .Replace("const char*", "string")
             .Replace("char*", "string")
             .Replace("const char *", "string")
             .Replace("int8_t", "sbyte")
             .Replace("uint8_t", "byte")
             .Replace("int16_t", "short")
             .Replace("uint16_t", "ushort")
             .Replace("int32_t", "int")
             .Replace("uint32_t", "uint")
             .Replace("int64_t", "long")
             .Replace("uint64_t", "ulong")
             .Replace("size_t", "int");

        return lang switch
        {
            "csharp" => t switch
            {
                "int" => "int", "long" => "long", "float" => "float",
                "double" => "double", "char" => "char", "void" => "void",
                "string" => "string", "bool" => "bool",
                "uint" => "uint", "ushort" => "ushort", "byte" => "byte",
                "sbyte" => "sbyte", "short" => "short", "ulong" => "ulong",
                _ => "int"
            },
            "java" => t switch
            {
                "int" => "int", "long" => "long", "float" => "float",
                "double" => "double", "char" => "char", "void" => "void",
                "string" => "String", "bool" => "boolean",
                "uint" => "int", "ushort" => "short", "byte" => "byte",
                "sbyte" => "byte", "short" => "short", "ulong" => "long",
                _ => "int"
            },
            "kotlin" => t switch
            {
                "int" => "Int", "long" => "Long", "float" => "Float",
                "double" => "Double", "char" => "Char", "void" => "Unit",
                "string" => "String", "bool" => "Boolean",
                "uint" => "Int", "ushort" => "Short", "byte" => "Byte",
                "sbyte" => "Byte", "short" => "Short", "ulong" => "Long",
                _ => "Int"
            },
            "pascal" => t switch
            {
                "int" => "Integer", "long" => "Int64", "float" => "Single",
                "double" => "Double", "char" => "Char", "void" => "",
                "string" => "String", "bool" => "Boolean",
                "uint" => "Cardinal", "ushort" => "Word", "byte" => "Byte",
                "sbyte" => "ShortInt", "short" => "SmallInt", "ulong" => "UInt64",
                _ => "Integer"
            },
            "go" => t switch
            {
                "int" => "int32", "long" => "int64", "float" => "float32",
                "double" => "float64", "char" => "byte", "void" => "",
                "string" => "string", "bool" => "bool",
                "uint" => "uint32", "ushort" => "uint16", "byte" => "uint8",
                "sbyte" => "int8", "short" => "int16", "ulong" => "uint64",
                _ => "int32"
            },
            "python" => t switch
            {
                "void" => "None", _ => t
            },
            "rust" => t switch
            {
                "int" => "i32", "long" => "i64", "float" => "f32",
                "double" => "f64", "char" => "u8", "void" => "",
                "string" => "*const u8", "bool" => "bool",
                "uint" => "u32", "ushort" => "u16", "byte" => "u8",
                "sbyte" => "i8", "short" => "i16", "ulong" => "u64",
                _ => "i32"
            },
            "swift" => t switch
            {
                "int" => "Int", "long" => "Int64", "float" => "Float",
                "double" => "Double", "char" => "CChar", "void" => "Void",
                "string" => "String", "bool" => "Bool",
                "uint" => "UInt", "ushort" => "UInt16", "byte" => "UInt8",
                "sbyte" => "Int8", "short" => "Int16", "ulong" => "UInt64",
                _ => "Int"
            },
            "d" => t switch
            {
                "int" => "int", "long" => "long", "float" => "float",
                "double" => "double", "char" => "char", "void" => "void",
                "string" => "string", "bool" => "bool",
                "uint" => "uint", "ushort" => "ushort", "byte" => "ubyte",
                "sbyte" => "byte", "short" => "short", "ulong" => "ulong",
                _ => "int"
            },
            "objc" => t switch
            {
                "int" => "NSInteger", "long" => "int64_t", "float" => "float",
                "double" => "double", "char" => "char", "void" => "void",
                "string" => "NSString*", "bool" => "BOOL",
                "uint" => "NSUInteger", _ => "NSInteger"
            },
            _ => t // C, C++, JS, Lua, etc. — use C types as-is or simplified
        };
    }

    // ============================================================
    // C type → parameter names
    // ============================================================

    static List<(string name, string cType)> ParseParams(string cParams)
    {
        if (string.IsNullOrEmpty(cParams) || cParams.Trim() == "void")
            return new();
        var result = new List<(string, string)>();
        var parts = cParams.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (string.IsNullOrEmpty(p)) continue;
            // Parse "type name" or just "type"
            var words = p.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                result.Add((words.Last(), string.Join(" ", words.Take(words.Length - 1))));
            else
                result.Add(($"a{i}", words[0]));
        }
        return result;
    }

    // ============================================================
    // Output path helper
    // ============================================================

    string GetOutputPath(string dir, string ext, string? classNameOverride = null)
    {
        if (!string.IsNullOrEmpty(_outputFile))
            return _outputFile;
        var cn = classNameOverride ?? _className;
        // PascalCase class name for file; lowercase for non-OOP languages
        var fname = ext switch
        {
            ".dart" or ".go" or ".py" or ".rs" or ".rb" or ".scm" or ".lua" or ".js" => cn.ToLower(),
            ".h" or ".hpp" => cn.ToLower(),
            _ => ToPascalCase(cn) == cn ? cn : cn  // keep as-is
        };
        // Fix: ensure first char is uppercase for .cs/.java/.kt/.pas etc.
        if (!string.IsNullOrEmpty(fname) && char.IsLower(fname[0]) && ext is ".cs" or ".java" or ".kt" or ".pas" or ".swift")
            fname = char.ToUpper(fname[0]) + fname[1..];
        return Path.Combine(dir, $"{fname}{ext}");
    }

    // ============================================================
    // Language generators
    // ============================================================

    // ---- OOP languages (class wrapper) ----

    void GenCSharp(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — C# native bindings");
        sb.AppendLine();
        sb.AppendLine($"class {ToPascalCase(_className)}");
        sb.AppendLine("{");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "csharp");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{MapType(p.cType, "csharp")} {ApplyStyle(p.name)}"));
            sb.AppendLine($"    native static {ret} {nativeName}({parmStr}) alias \"{f.Name}\";");
        }
        sb.AppendLine("}");
        File.WriteAllText(GetOutputPath(dir, ".cs"), sb.ToString());
    }

    void GenJava(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Java native bindings");
        sb.AppendLine();
        sb.AppendLine($"class {ToPascalCase(_className)} {{");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "java");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{MapType(p.cType, "java")} {ApplyStyle(p.name)}"));
            sb.AppendLine($"    native static {ret} {nativeName}({parmStr}); // CALL {f.Name}");
        }
        sb.AppendLine("}");
        File.WriteAllText(GetOutputPath(dir, ".java"), sb.ToString());
    }

    void GenKotlin(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Kotlin native bindings");
        sb.AppendLine();
        sb.AppendLine($"object {ToPascalCase(_className)} {{");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "kotlin");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{ApplyStyle(p.name)}: {MapType(p.cType, "kotlin")}"));
            sb.AppendLine($"    external fun {nativeName}({parmStr}): {ret} // CALL {f.Name}");
        }
        sb.AppendLine("}");
        File.WriteAllText(GetOutputPath(dir, ".kt"), sb.ToString());
    }

    void GenDart(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Dart native bindings");
        sb.AppendLine();
        sb.AppendLine($"class {ToPascalCase(_className)} {{");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "csharp"); // Dart uses similar types to C#
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{MapType(p.cType, "csharp")} {ApplyStyle(p.name)}"));
            sb.AppendLine($"    static native {ret} {nativeName}({parmStr}); // CALL {f.Name}");
        }
        sb.AppendLine("}");
        File.WriteAllText(GetOutputPath(dir, ".dart"), sb.ToString());
    }

    // ---- Non-OOP languages (function declarations) ----

    void GenPascal(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Pascal native bindings");
        sb.AppendLine();
        sb.AppendLine($"unit {ToPascalCase(_className)};");
        sb.AppendLine();
        sb.AppendLine("interface");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "pascal");
            var parms = ParseParams(f.Params);
            if (f.ReturnType.Trim() == "void")
            {
                var parmStr = string.Join("; ", parms.Select(p => $"{ApplyStyle(p.name)}: {MapType(p.cType, "pascal")}"));
                if (!string.IsNullOrEmpty(parmStr)) parmStr = $"({parmStr})";
                sb.AppendLine($"procedure {nativeName}{parmStr}; external '{f.Name}';");
            }
            else
            {
                var parmStr = string.Join("; ", parms.Select(p => $"{ApplyStyle(p.name)}: {MapType(p.cType, "pascal")}"));
                if (!string.IsNullOrEmpty(parmStr)) parmStr = $"({parmStr})";
                sb.AppendLine($"function {nativeName}{parmStr}: {ret}; external '{f.Name}';");
            }
        }
        sb.AppendLine();
        sb.AppendLine("implementation");
        sb.AppendLine();
        sb.AppendLine("end.");
        File.WriteAllText(GetOutputPath(dir, ".pas"), sb.ToString());
    }

    void GenGo(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Go native bindings");
        sb.AppendLine();
        sb.AppendLine($"package {_className.ToLower()}");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "go");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{ApplyStyle(p.name)} {MapType(p.cType, "go")}"));
            sb.AppendLine($"func {nativeName}({parmStr}) {ret} {{");
            sb.AppendLine($"    return vml.Call(\"{f.Name}\")");
            sb.AppendLine("}");
        }
        File.WriteAllText(GetOutputPath(dir, ".go"), sb.ToString());
    }

    void GenPython(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated by GenLib — Python native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            var parmNames = string.Join(", ", parms.Select(p => ApplyStyle(p.name)));
            sb.AppendLine($"def {nativeName}({parmNames}):");
            sb.AppendLine($"    \"\"\"Native call: {f.Name}({f.Params}) -> {f.ReturnType}\"\"\"");
            sb.AppendLine($"    return __native_call__(\"{f.Name}\", {parmNames})");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".py"), sb.ToString());
    }

    void GenBasic(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("' Auto-generated by GenLib — BASIC native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            if (f.ReturnType.Trim() == "void")
            {
                var parmStr = string.Join(", ", parms.Select(p => $"{ApplyStyle(p.name)} AS INTEGER"));
                sb.AppendLine($"SUB {nativeName}({parmStr})");
                sb.AppendLine($"    ASM(\"CALL {f.Name}\")");
                sb.AppendLine("END SUB");
            }
            else
            {
                var parmStr = string.Join(", ", parms.Select(p => $"{ApplyStyle(p.name)} AS INTEGER"));
                sb.AppendLine($"FUNCTION {nativeName}({parmStr}) AS INTEGER");
                sb.AppendLine($"    ASM(\"CALL {f.Name}\")");
                sb.AppendLine($"    {nativeName} = 0");
                sb.AppendLine("END FUNCTION");
            }
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".bas"), sb.ToString());
    }

    void GenRust(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Rust native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "rust");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{ApplyStyle(p.name)}: {MapType(p.cType, "rust")}"));
            if (f.ReturnType.Trim() == "void")
            {
                sb.AppendLine($"fn {nativeName}({parmStr}) {{");
                sb.AppendLine($"    unsafe {{ vml_call!(\"{f.Name}\") }};");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine($"fn {nativeName}({parmStr}) -> {ret} {{");
                sb.AppendLine($"    unsafe {{ vml_call!(\"{f.Name}\") }}");
                sb.AppendLine("}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".rs"), sb.ToString());
    }

    void GenJavaScript(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — JavaScript native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            var parmNames = string.Join(", ", parms.Select(p => ApplyStyle(p.name)));
            sb.AppendLine($"function {nativeName}({parmNames}) {{");
            sb.AppendLine($"    return __native__(\"{f.Name}\", {parmNames});");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".js"), sb.ToString());
    }

    void GenLua(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Auto-generated by GenLib — Lua native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            var parmNames = string.Join(", ", parms.Select(p => ApplyStyle(p.name)));
            sb.AppendLine($"function {nativeName}({parmNames})");
            sb.AppendLine($"    return vml.call(\"{f.Name}\", {parmNames})");
            sb.AppendLine("end");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".lua"), sb.ToString());
    }

    void GenSwift(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Swift native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "swift");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"_ {ApplyStyle(p.name)}: {MapType(p.cType, "swift")}"));
            sb.AppendLine($"func {nativeName}({parmStr}) -> {ret} {{");
            sb.AppendLine($"    return vmlCall(\"{f.Name}\")");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".swift"), sb.ToString());
    }

    void GenRuby(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated by GenLib — Ruby native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            var parmNames = string.Join(", ", parms.Select(p => ApplyStyle(p.name)));
            sb.AppendLine($"def {nativeName}({parmNames})");
            sb.AppendLine($"    VML.call(\"{f.Name}\", {parmNames})");
            sb.AppendLine("end");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".rb"), sb.ToString());
    }

    void GenScheme(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine(";; Auto-generated by GenLib — Scheme native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var parms = ParseParams(f.Params);
            var parmNames = string.Join(" ", parms.Select(p => ApplyStyle(p.name)));
            sb.AppendLine($"(define ({nativeName} {parmNames})");
            sb.AppendLine($"    (vml-call \"{f.Name}\" {parmNames}))");
            sb.AppendLine();
        }
        File.WriteAllText(GetOutputPath(dir, ".scm"), sb.ToString());
    }

    void GenForth(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\\ Auto-generated by GenLib — Forth native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            // Forth uses uppercase by convention
            sb.AppendLine($": {nativeName.ToUpper()} CALL {f.Name} ;");
        }
        File.WriteAllText(GetOutputPath(dir, ".fth"), sb.ToString());
    }

    void GenD(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — D native bindings");
        sb.AppendLine();
        sb.AppendLine("extern(C) {");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "d");
            var parms = ParseParams(f.Params);
            var parmStr = string.Join(", ", parms.Select(p => $"{MapType(p.cType, "d")} {ApplyStyle(p.name)}"));
            sb.AppendLine($"    {ret} {nativeName}({parmStr}); // alias {f.Name}");
        }
        sb.AppendLine("}");
        File.WriteAllText(GetOutputPath(dir, ".d"), sb.ToString());
    }

    void GenObjC(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Objective-C native bindings");
        sb.AppendLine();
        sb.AppendLine($"@interface {ToPascalCase(_className)} : NSObject");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            var ret = MapType(f.ReturnType, "objc");
            var parms = ParseParams(f.Params);
            var parmParts = new List<string>();
            foreach (var p in parms)
                parmParts.Add($"{ApplyStyle(p.name)}:({MapType(p.cType, "objc")}){ApplyStyle(p.name)}");
            var parmStr = string.Join(" ", parmParts);
            if (f.ReturnType.Trim() == "void")
                sb.AppendLine($"- (void){nativeName}{(parmParts.Count > 0 ? " " : "")}{parmStr}; // extern {f.Name}");
            else
                sb.AppendLine($"- ({ret}){nativeName}{(parmParts.Count > 0 ? " " : "")}{parmStr}; // extern {f.Name}");
        }
        sb.AppendLine("@end");
        File.WriteAllText(GetOutputPath(dir, ".h"), sb.ToString());
    }

    // ---- Header-only languages ----

    void GenC(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — C native bindings");
        sb.AppendLine($"// Module: {_className}");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            sb.AppendLine($"{f.ReturnType} {nativeName}({f.Params}); // extern");
        }
        var cn = string.IsNullOrEmpty(_className) ? "native" : _className.ToLower();
        var path = string.IsNullOrEmpty(_outputFile)
            ? Path.Combine(dir, $"{cn}.h")
            : _outputFile;
        File.WriteAllText(path, sb.ToString());
    }

    void GenCpp(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — C++ native bindings");
        sb.AppendLine($"// Module: {_className}");
        sb.AppendLine();
        sb.AppendLine("extern \"C\" {");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            sb.AppendLine($"    {f.ReturnType} {nativeName}({f.Params}); // extern");
        }
        sb.AppendLine("}");
        var cn = string.IsNullOrEmpty(_className) ? "native" : _className.ToLower();
        var path = string.IsNullOrEmpty(_outputFile)
            ? Path.Combine(dir, $"{cn}.hpp")
            : _outputFile;
        File.WriteAllText(path, sb.ToString());
    }

    // ---- Stub generators ----

    void GenFortran(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("! Auto-generated by GenLib — Fortran native bindings");
        sb.AppendLine($"! Module: {_className}");
        sb.AppendLine();
        sb.AppendLine("interface");
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            sb.AppendLine($"    ! function {nativeName}(...) result(r)");
            sb.AppendLine($"    !   bind(C, name=\"{f.Name}\")");
            sb.AppendLine($"    ! end function {nativeName}");
        }
        sb.AppendLine("end interface");
        File.WriteAllText(GetOutputPath(dir, ".f90"), sb.ToString());
    }

    void GenR(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated by GenLib — R native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            sb.AppendLine($"# {nativeName} <- function(...) .Call(\"{f.Name}\", ...)");
        }
        File.WriteAllText(GetOutputPath(dir, ".r"), sb.ToString());
    }

    void GenLadder(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Ladder native bindings");
        sb.AppendLine();
        foreach (var f in _funcs)
        {
            var nativeName = ApplyStyle(f.Name);
            sb.AppendLine($"// FB_{nativeName}: CALL {f.Name}  (R{_className}_{nativeName})");
        }
        File.WriteAllText(GetOutputPath(dir, ".ld"), sb.ToString());
    }
}
