using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GenDyn;

// ============================================================
// GenDyn v2 — VML Dynamic Library Pipeline
// ============================================================
// 用法:
//   gendyn -s [-r ROOT]                             扫描动态库符号
//   gendyn -b [-n NAME] [-r ROOT]                   编译 C 胶水库
//   gendyn -w [-n NAME] [-H HEADER] [-r ROOT]       生成 VML 包装器
//   gendyn -g [-n NAME] [-l LANG] [-r ROOT]         生成语言绑定
//   gendyn -A [-n NAME] [-l LANG] [-H HEADER] [-r ROOT]  一键全流程
// ============================================================

var allLangs = new[] { "basic", "c", "cpp", "csharp", "forth", "go", "java", "javascript",
    "kotlin", "ladder", "lua", "pascal", "python", "rust", "scheme", "swift", "ruby", "dart", "objc", "r", "d", "fortran" };

// ---- CLI 解析 ----
string cmd = "";
string libName = "";
string headerPath = "";
string lang = "all";
string root = ".";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-h": case "--help": PrintHelp(); return;
        case "-s": case "--scan": cmd = "scan"; break;
        case "-b": case "--build": cmd = "build"; break;
        case "-w": case "--wrap": cmd = "wrap"; break;
        case "-g": case "--bindings": cmd = "bindings"; break;
        case "-A": case "--all": cmd = "all"; break;
        case "-n": case "--name":
            if (i + 1 < args.Length) libName = args[++i];
            else { Console.WriteLine("错误: -n/--name 需要参数"); return; }
            break;
        case "-H": case "--header":
            if (i + 1 < args.Length) headerPath = args[++i];
            else { Console.WriteLine("错误: -H/--header 需要参数"); return; }
            break;
        case "-l": case "--lang":
            if (i + 1 < args.Length) lang = args[++i].ToLower();
            else { Console.WriteLine("错误: -l/--lang 需要参数"); return; }
            break;
        case "-r": case "--root":
            if (i + 1 < args.Length) root = args[++i];
            else { Console.WriteLine("错误: -r/--root 需要参数"); return; }
            break;
        default:
            // 兼容旧格式: 第一个非 flag 参数作为 dll 路径
            if (!args[i].StartsWith("-") && cmd == "")
                cmd = args[i];
            break;
    }
}

if (cmd == "") { PrintHelp(); return; }

// ---- 初始化路径 ----
var projectRoot = Path.GetFullPath(root);
var libRoot = Path.Combine(projectRoot, "Lib");
var dynDir = Path.Combine(libRoot, "dynamic");

if (!Directory.Exists(dynDir))
{
    Directory.CreateDirectory(dynDir);
    Console.WriteLine($"  已创建 {dynDir}");
}

// ---- 加载配置 ----
var config = DynLibConfig.LoadOrCreate(libRoot);

// ---- 旧格式兼容: 如果 cmd 是文件路径, 当作全流程 ----
if (File.Exists(cmd) && (cmd.EndsWith(".dll") || cmd.EndsWith(".so") || cmd.EndsWith(".dylib")))
{
    string dllPath = cmd;
    if (string.IsNullOrEmpty(libName))
        libName = Path.GetFileNameWithoutExtension(dllPath).Replace("lib", "");
    if (string.IsNullOrEmpty(headerPath))
    {
        // 尝试在同目录找 .h 文件
        string dir = Path.GetDirectoryName(dllPath) ?? ".";
        foreach (var h in Directory.GetFiles(dir, "*.h"))
        {
            if (Path.GetFileNameWithoutExtension(h).Contains(libName))
                { headerPath = h; break; }
        }
    }
    cmd = "all";
}

// ---- 执行命令 ----
switch (cmd)
{
    case "scan":
        CmdScan(libRoot, dynDir, libName, config);
        break;
    case "build":
        CmdBuild(libRoot, dynDir, libName, config);
        break;
    case "wrap":
        CmdWrap(libRoot, dynDir, libName, headerPath, config);
        break;
    case "bindings":
        CmdBindings(libRoot, libName, lang, config);
        break;
    case "all":
        if (string.IsNullOrEmpty(libName))
        {
            // -A 无 -n: 对 dynlibs.json 中所有库执行全流程
            foreach (var name in config.Keys.ToList())
            {
                Console.WriteLine($"\n==== {name} ====");
                CmdScan(libRoot, dynDir, name, config);
                CmdBuild(libRoot, dynDir, name, config);
                CmdWrap(libRoot, dynDir, name, "", config);
                CmdBindings(libRoot, name, lang, config);
            }
        }
        else
        {
            CmdScan(libRoot, dynDir, libName, config);
            CmdBuild(libRoot, dynDir, libName, config);
            CmdWrap(libRoot, dynDir, libName, headerPath, config);
            CmdBindings(libRoot, libName, lang, config);
        }
        break;
    default:
        Console.WriteLine($"未知命令: {cmd}");
        PrintHelp();
        break;
}

// ============================================================
// 命令实现
// ============================================================

/// <summary>-s: 扫描动态库符号 → 更新 dynlibs.json</summary>
void CmdScan(string libRoot, string dynDir, string wantName, Dictionary<string, DynLibDef> cfg)
{
    Console.WriteLine("=== 扫描动态库符号 ===");

    if (!string.IsNullOrEmpty(wantName) && cfg.TryGetValue(wantName, out var def))
    {
        ScanOne(libRoot, dynDir, wantName, def, cfg);
    }
    else if (!string.IsNullOrEmpty(wantName))
    {
        // 库名不在配置中, 尝试自动发现
        string? libFile = FindLibFile(dynDir, wantName);
        if (libFile != null)
        {
            def = new DynLibDef { Library = libFile };
            cfg[wantName] = def;
            ScanOne(libRoot, dynDir, wantName, def, cfg);
        }
        else
            Console.WriteLine($"  未找到动态库: {wantName}");
    }
    else
    {
        // 扫描所有动态库
        foreach (var dll in Directory.GetFiles(dynDir, "*.dylib")
            .Concat(Directory.GetFiles(dynDir, "*.so"))
            .Concat(Directory.GetFiles(dynDir, "*.dll")))
        {
            string name = DynLibConfig_StripLibPrefix(Path.GetFileNameWithoutExtension(dll));
            if (!cfg.ContainsKey(name))
                cfg[name] = new DynLibDef { Library = Path.GetFileName(dll) };
            ScanOne(libRoot, dynDir, name, cfg[name], cfg);
        }
    }
    DynLibConfig.Save(libRoot, cfg);
}

void ScanOne(string libRoot, string dynDir, string name, DynLibDef def, Dictionary<string, DynLibDef> cfg)
{
    string libPath = Path.Combine(dynDir, def.Library);
    if (!File.Exists(libPath))
    {
        Console.WriteLine($"  跳过 {name}: 库文件不存在 ({def.Library})");
        return;
    }

    Console.WriteLine($"  扫描 {name} ({def.Library})...");

    // 解析导出符号
    var exports = ExportParser.ParseDllExports(libPath);
    Console.WriteLine($"    {exports.Count} 个导出符号");

    // 解析头文件获取类型
    if (!string.IsNullOrEmpty(def.Header))
    {
        string hPath = Path.Combine(dynDir, def.Header);
        if (File.Exists(hPath))
        {
            var funcs = ExportParser.ParseHeaderFile(hPath);
            def.Functions = new();
            foreach (var f in funcs)
            {
                string paramStr = string.Join(", ",
                    f.Params.Select(p => $"{p.Type.ToString().ToLower()} {p.Name}"));
                def.Functions[f.Name] = new DynFuncDef
                {
                    Return = f.ReturnType.ToString().ToLower(),
                    Params = paramStr
                };
            }
            Console.WriteLine($"    {def.Functions.Count} 个函数 (含类型信息)");
        }
    }
    else if (exports.Count > 0)
    {
        // 无头文件, 用默认 int 类型
        Console.WriteLine("    未提供头文件, 参数默认为 int 类型");
    }
}

/// <summary>-b: 编译 C 胶水库 (调用系统 cc)</summary>
void CmdBuild(string libRoot, string dynDir, string wantName, Dictionary<string, DynLibDef> cfg)
{
    Console.WriteLine("=== 编译 C 胶水库 ===");

    foreach (var kv in cfg)
    {
        if (!string.IsNullOrEmpty(wantName) && kv.Key != wantName) continue;
        string name = kv.Key;
        var def = kv.Value;
        string gluePath = Path.Combine(dynDir, def.Glue);
        if (!File.Exists(gluePath))
        {
            Console.WriteLine($"  跳过 {name}: 胶水源码不存在 ({def.Glue})");
            continue;
        }
        Console.WriteLine($"  编译 {name}...");

        string outName = $"lib{name}_glue";
        if (OperatingSystem.IsMacOS())
        {
            outName += ".dylib";
            RunCmd("cc", $"-shared -o \"{Path.Combine(dynDir, outName)}\" \"{gluePath}\"");
        }
        else if (OperatingSystem.IsLinux())
        {
            outName += ".so";
            RunCmd("cc", $"-shared -o \"{Path.Combine(dynDir, outName)}\" \"{gluePath}\"");
        }
        else if (OperatingSystem.IsWindows())
        {
            outName += ".dll";
            RunCmd("cl", $"/nologo /LD /Fe:\"{Path.Combine(dynDir, outName)}\" \"{gluePath}\"");
        }
        Console.WriteLine($"    → {outName}");
    }
}

/// <summary>-w: 生成 VML 包装器</summary>
void CmdWrap(string libRoot, string dynDir, string wantName, string headerOverride, Dictionary<string, DynLibDef> cfg)
{
    Console.WriteLine("=== 生成 VML 包装器 ===");

    foreach (var kv in cfg)
    {
        if (!string.IsNullOrEmpty(wantName) && kv.Key != wantName) continue;
        string name = kv.Key;
        var def = kv.Value;

        // 获取函数列表
        List<FunctionInfo> functions;
        string hPath = !string.IsNullOrEmpty(headerOverride) ? headerOverride :
                       !string.IsNullOrEmpty(def.Header) ? Path.Combine(dynDir, def.Header) : "";

        if (!string.IsNullOrEmpty(hPath) && File.Exists(hPath))
        {
            functions = ExportParser.ParseHeaderFile(hPath);
            // 更新配置
            def.Header = Path.GetFileName(hPath);
            def.Functions = new();
            foreach (var f in functions)
            {
                string paramStr = string.Join(", ",
                    f.Params.Select(p => $"{p.Type.ToString().ToLower()} {p.Name}"));
                def.Functions[f.Name] = new DynFuncDef { Return = f.ReturnType.ToString().ToLower(), Params = paramStr };
            }
        }
        else if (def.Functions.Count > 0)
        {
            functions = def.Functions.Select(kvFunc =>
            {
                var fi = new FunctionInfo { Name = kvFunc.Key, OriginalName = kvFunc.Key, ReturnType = ParamType.Int };
                if (!string.IsNullOrEmpty(kvFunc.Value.Params))
                {
                    foreach (var p in kvFunc.Value.Params.Split(','))
                    {
                        string trimmed = p.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        var parts = trimmed.Split(' ');
                        fi.Params.Add(new ParamInfo { Name = parts.Last(), Type = ParamType.Int });
                    }
                }
                return fi;
            }).ToList();
        }
        else
        {
            string libPath = Path.Combine(dynDir, def.Library);
            if (!File.Exists(libPath))
            {
                Console.WriteLine($"  跳过 {name}: 动态库不存在");
                continue;
            }
            var exports = ExportParser.ParseDllExports(libPath);
            functions = ExportParser.ExportsToFunctions(exports);
        }

        if (functions.Count == 0)
        {
            Console.WriteLine($"  跳过 {name}: 无函数");
            continue;
        }

        // 生成 VML 包装器 → Lib/dynamic/{name}.vml
        var generator = new CodeGenerator(name, def.Library, functions);
        generator.GenerateVmlOnly(dynDir);
        Console.WriteLine($"  {name}: {functions.Count} 个函数 → {dynDir}/{name}.vml");
    }
    DynLibConfig.Save(libRoot, cfg);
}

/// <summary>-g: 生成语言绑定</summary>
void CmdBindings(string libRoot, string wantName, string lang, Dictionary<string, DynLibDef> cfg)
{
    Console.WriteLine("=== 生成语言绑定 ===");

    var targets = lang == "all" ? allLangs : new[] { lang };

    foreach (var kv in cfg)
    {
        if (!string.IsNullOrEmpty(wantName) && kv.Key != wantName) continue;
        string name = kv.Key;
        var def = kv.Value;

        // 获取函数列表
        List<FunctionInfo> functions;
        if (def.Functions.Count > 0)
        {
            functions = def.Functions.Select(kvFunc =>
            {
                var fi = new FunctionInfo { Name = kvFunc.Key, OriginalName = kvFunc.Key, ReturnType = ParamType.Int };
                if (!string.IsNullOrEmpty(kvFunc.Value.Params))
                {
                    foreach (var p in kvFunc.Value.Params.Split(','))
                    {
                        string trimmed = p.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        var parts = trimmed.Split(' ');
                        fi.Params.Add(new ParamInfo { Name = parts.Last(), Type = ParamType.Int });
                    }
                }
                return fi;
            }).ToList();
        }
        else
        {
            Console.WriteLine($"  跳过 {name}: 无函数信息 (先运行 -s -n {name})");
            continue;
        }

        var generator = new CodeGenerator(name, def.Library, functions);
        generator.GenerateLanguageHeadersOnly(libRoot, targets);
    }
}

// ============================================================
// 辅助函数
// ============================================================

string? FindLibFile(string dynDir, string name)
{
    foreach (var ext in new[] { ".dylib", ".so", ".dll" })
    {
        foreach (var prefix in new[] { $"lib{name}", name, $"lib{name}_glue" })
        {
            string path = Path.Combine(dynDir, prefix + ext);
            if (File.Exists(path)) return Path.GetFileName(path);
        }
    }
    return null;
}

string DynLibConfig_StripLibPrefix(string s)
{
    if (s.StartsWith("lib", StringComparison.OrdinalIgnoreCase) && s.Length > 3)
        s = s[3..];
    return s;
}

void RunCmd(string cmd, string args)
{
    try
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) { Console.WriteLine($"    警告: 无法启动 {cmd}"); return; }
        proc.WaitForExit(30000);
        if (proc.ExitCode != 0)
        {
            string err = proc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err))
                Console.WriteLine($"    {cmd} 错误: {err.Trim()}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    警告: {cmd} 不可用 ({ex.Message})");
    }
}

void PrintHelp()
{
    Console.WriteLine("GenDyn v2 — 动态库接口自动生成工具");
    Console.WriteLine();
    Console.WriteLine("用法:");
    Console.WriteLine("  gendyn -s [-n NAME] [-r ROOT]                           扫描动态库符号");
    Console.WriteLine("  gendyn -b [-n NAME] [-r ROOT]                           编译 C 胶水库");
    Console.WriteLine("  gendyn -w [-n NAME] [-H HEADER] [-r ROOT]               生成 VML 包装器");
    Console.WriteLine("  gendyn -g [-n NAME] [-l LANG] [-r ROOT]                 生成语言绑定");
    Console.WriteLine("  gendyn -A [-n NAME] [-l LANG] [-H HEADER] [-r ROOT]     一键全流程");
    Console.WriteLine();
    Console.WriteLine("选项:");
    Console.WriteLine("  -s, --scan      扫描 Lib/dynamic/*.dylib → 更新 dynlibs.json");
    Console.WriteLine("  -b, --build     编译 C 胶水代码 → lib{name}_glue.dylib");
    Console.WriteLine("  -w, --wrap      生成 VML FFI 包装器 → Lib/dynamic/{name}.vml");
    Console.WriteLine("  -g, --bindings  生成 22 语言绑定 → Lib/{lang}/ext/lib{name}.{ext}");
    Console.WriteLine("  -A, --all       一键全流程 (-s + -b + -w + -g)");
    Console.WriteLine("  -n, --name      库名称 (不指定则操作全部)");
    Console.WriteLine("  -H, --header    指定 C 头文件路径");
    Console.WriteLine("  -l, --lang      指定语言 (不指定则全部 22 种)");
    Console.WriteLine("  -r, --root      项目根目录 (默认: .)");
    Console.WriteLine("  -h, --help      显示帮助");
    Console.WriteLine();
    Console.WriteLine("文件布局:");
    Console.WriteLine("  Lib/dynamic/dynlibs.json    动态库注册表 (自动生成)");
    Console.WriteLine("  Lib/dynamic/{name}.vml       VML 包装器");
    Console.WriteLine("  Lib/dynamic/{name}_lib.c     C 胶水源码");
    Console.WriteLine("  Lib/dynamic/lib{name}_glue.dylib  编译产物");
    Console.WriteLine("  Lib/{lang}/ext/lib{name}.{ext}    语言绑定");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  gendyn -A -n opencv -H opencv.h        opencv 全流程");
    Console.WriteLine("  gendyn -s                               扫描所有动态库");
    Console.WriteLine("  gendyn -w -n sqlite3                    仅生成 sqlite3 VML 包装");
    Console.WriteLine("  gendyn -g -n opencv -l python           仅生成 Python 绑定");
}
