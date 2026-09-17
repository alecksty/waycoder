using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenLib;

internal class ModuleDef
{
    public string Source { get; set; } = "";
    public bool Core { get; set; }
    public List<string> Functions { get; set; } = new();
    public List<string> Includes { get; set; } = new();
}

internal static class ModuleConfig
{
    private static readonly Dictionary<string, string[]> CoreSourceFiles = new()
    {
        ["math"]    = new[] { "math.c", "float.c", "util.c" },
        ["string"]  = new[] { "string.c" },
        ["time"]    = new[] { "sysinfo.c" },
        ["file"]    = new[] { "file.c" },
        ["console"] = new[] { "io.c", "printf.c", "scanf.c", "readline.c" },
        ["system"]  = new[] { "builtins.c", "device.c", "memory.c" },
    };

    /// <summary>函数名到核心模块的快速匹配规则 (v1.66.55+: 裸名, 不再使用 shared_ 前缀)</summary>
    private static readonly (string pattern, string module)[] CorePatterns = new[]
    {
        ("peek", "system"), ("poke", "system"), ("peekb", "system"), ("pokeb", "system"),
        ("alloc", "system"), ("free", "system"),
        ("dev_", "system"), ("getconfig", "system"), ("exit", "system"),
        ("mem", "system"),

        ("abs", "math"), ("min", "math"), ("max", "math"), ("clamp", "math"),
        ("sqrt", "math"), ("pow", "math"), ("int_pow", "math"), ("int_sqrt", "math"),
        ("random", "math"), ("randomize", "math"), ("sum", "math"),
        ("sin", "math"), ("cos", "math"), ("tan", "math"),
        ("asin", "math"), ("acos", "math"), ("atan", "math"),
        ("exp", "math"), ("log", "math"),

        // str_to_* / wstr_to_* / ustr_to_* 是类型转换函数，归 conv 模块
        ("str_to_", "conv"),
        ("wstr_to_", "conv"),
        ("ustr_to_", "conv"),
        ("str", "string"),

        ("datetime", "time"), ("get_date", "time"), ("get_time", "time"),
        ("sleep", "time"), ("delay", "time"), ("get_tick", "time"),

        ("fopen", "file"), ("fclose", "file"), ("fread", "file"),
        ("fwrite", "file"), ("fseek", "file"), ("ftell", "file"),
        ("fsize", "file"), ("ftruncate", "file"),

        ("print", "console"), ("put", "console"), ("getchar", "console"),
        ("input_", "console"), ("puts", "console"), ("clear_", "console"),
        ("kb_hit", "console"), ("read_line", "console"),
        ("printf", "console"), ("sprintf", "console"), ("vsnprintf", "console"),
        ("sscanf", "console"), ("scanf", "console"),
    };

    public static Dictionary<string, ModuleDef> LoadOrCreate(string libRoot, List<FuncDef> allFuncs)
    {
        string path = Path.Combine(libRoot, "modules.json");
        if (File.Exists(path))
            return System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(path), GenLibJsonContext.Default.DictionaryStringModuleDef)!;

        var defaults = CreateDefaults(allFuncs);
        var json = System.Text.Json.JsonSerializer.Serialize(defaults, GenLibJsonContext.Default.DictionaryStringModuleDef);
        File.WriteAllText(path, json);
        Console.WriteLine($"  已创建 {path}（{defaults.Count} 模块，可编辑后重新生成）");
        return defaults;
    }

    static Dictionary<string, ModuleDef> CreateDefaults(List<FuncDef> allFuncs)
    {
        var modules = new Dictionary<string, ModuleDef>();

        // 初始化核心模块
        foreach (var kv in CoreSourceFiles)
        {
            modules[kv.Key] = new ModuleDef
            {
                Source = kv.Value[0],
                Core = true,
                Functions = new(),
                Includes = kv.Value.Select(f => Path.GetFileNameWithoutExtension(f) + ".vml").ToList()
            };
        }

        // 分配函数到模块
        foreach (var f in allFuncs)
        {
            string? matched = null;
            foreach (var (pattern, module) in CorePatterns)
            {
                if (f.Name.StartsWith(pattern))
                { matched = module; break; }
            }

            if (matched != null)
            {
                if (!modules[matched].Functions.Contains(f.Name))
                    modules[matched].Functions.Add(f.Name);
            }
            else
            {
                // 自定义模块：按源文件名分组
                string modName = Path.GetFileNameWithoutExtension(f.File);
                if (!modules.ContainsKey(modName))
                {
                    modules[modName] = new ModuleDef
                    {
                        Source = f.File,
                        Core = false,
                        Functions = new(),
                        Includes = new() { modName + ".vml" }
                    };
                }
                if (!modules[modName].Functions.Contains(f.Name))
                    modules[modName].Functions.Add(f.Name);
            }
        }

        return modules;
    }
}
