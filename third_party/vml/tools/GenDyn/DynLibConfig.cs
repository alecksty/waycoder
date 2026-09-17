using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenDyn;

/// <summary>dynlibs.json 中的一个动态库条目</summary>
internal class DynLibDef
{
    public string Library { get; set; } = "";   // 原始动态库文件名 (如 libopencv.dylib)
    public string Header { get; set; } = "";    // C 头文件名
    public string Glue { get; set; } = "";      // C 胶水源码文件名
    public Dictionary<string, DynFuncDef> Functions { get; set; } = new();
}

internal class DynFuncDef
{
    public string Return { get; set; } = "int";
    public string Params { get; set; } = "";
}

/// <summary>System.Text.Json 源生成器上下文 — Native AOT 兼容</summary>
[JsonSerializable(typeof(Dictionary<string, DynLibDef>))]
[JsonSerializable(typeof(DynLibDef))]
[JsonSerializable(typeof(DynFuncDef))]
internal partial class DynLibJsonContext : JsonSerializerContext { }

internal static class DynLibConfig
{
    public static Dictionary<string, DynLibDef> LoadOrCreate(string libRoot)
    {
        string dir = Path.Combine(libRoot, "dynamic");
        string path = Path.Combine(dir, "dynlibs.json");

        if (File.Exists(path))
        {
            var existing = JsonSerializer.Deserialize(File.ReadAllText(path), DynLibJsonContext.Default.DictionaryStringDynLibDef)!;
            foreach (var kv in existing)
                kv.Value.Functions ??= new();
            return existing;
        }

        // 自动扫描 Lib/dynamic/ 下的已有文件推断配置
        var config = ScanExisting(dir);
        var json = JsonSerializer.Serialize(config, DynLibJsonContext.Default.DictionaryStringDynLibDef);
        File.WriteAllText(path, json);
        Console.WriteLine($"  已创建 {path}（可编辑后重新生成）");
        return config;
    }

    /// <summary>扫描 dynamic/ 目录下的已有文件，推断配置</summary>
    private static Dictionary<string, DynLibDef> ScanExisting(string dynDir)
    {
        var config = new Dictionary<string, DynLibDef>();
        if (!Directory.Exists(dynDir)) return config;

        // 找到所有 .dylib/.so/.dll 文件
        var libFiles = Directory.GetFiles(dynDir, "*.dylib")
            .Concat(Directory.GetFiles(dynDir, "*.so"))
            .Concat(Directory.GetFiles(dynDir, "*.dll"))
            .ToList();

        foreach (var libPath in libFiles)
        {
            string libFile = Path.GetFileName(libPath);
            string baseName = StripLibPrefix(Path.GetFileNameWithoutExtension(libFile));

            // 找对应的头文件
            string header = "";
            foreach (var h in new[] { $"{baseName}.h", $"{baseName}.hpp", $"{baseName}_lib.h" })
            {
                if (File.Exists(Path.Combine(dynDir, h))) { header = h; break; }
            }

            // 找对应的胶水源码
            string glue = "";
            foreach (var g in new[] { $"{baseName}_lib.c", $"{baseName}_glue.c" })
            {
                if (File.Exists(Path.Combine(dynDir, g))) { glue = g; break; }
            }

            // 解析头文件获取函数列表
            var functions = new Dictionary<string, DynFuncDef>();
            if (!string.IsNullOrEmpty(header))
            {
                var funcs = ExportParser.ParseHeaderFile(Path.Combine(dynDir, header));
                foreach (var f in funcs)
                {
                    string paramStr = string.Join(", ",
                        f.Params.Select(p => $"{p.Type.ToString().ToLower()} {p.Name}"));
                    functions[f.Name] = new DynFuncDef
                    {
                        Return = f.ReturnType.ToString().ToLower(),
                        Params = paramStr
                    };
                }
            }

            config[baseName] = new DynLibDef
            {
                Library = libFile,
                Header = header,
                Glue = glue,
                Functions = functions
            };
        }

        return config;
    }

    public static void Save(string libRoot, Dictionary<string, DynLibDef> config)
    {
        string path = Path.Combine(libRoot, "dynamic", "dynlibs.json");
        var json = JsonSerializer.Serialize(config, DynLibJsonContext.Default.DictionaryStringDynLibDef);
        File.WriteAllText(path, json);
    }

    private static string StripLibPrefix(string name)
    {
        if (name.StartsWith("lib", StringComparison.OrdinalIgnoreCase) && name.Length > 3)
            name = name[3..];
        return name;
    }
}
