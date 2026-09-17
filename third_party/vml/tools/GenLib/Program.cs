using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenLib;

// ============================================================
// GenLib — VML Shared Library Pipeline
// ============================================================
// 用法:
//   genlib -s                             扫描导出函数
//   genlib -b [-r ROOT]                   编译 C → VML
//   genlib -m [-l LANG] [-r ROOT]         生成模块包装 .vml
//   genlib -a [-l LANG] [-r ROOT]         生成聚合文件
//   genlib -g [-l LANG] [-p PKG] [-r ROOT] 生成源文件绑定
//   genlib -n [-l LANG] [-c CLASS] [-o FILE] [--style STYLE] [--prefix] [-r ROOT]
//                                             生成 native 封装源文件
//   genlib -A [-l LANG] [-p PKG] [-r ROOT] 一键全流程
// ============================================================

var allLangs = new[] { "basic", "c", "cpp", "csharp", "forth", "go", "java", "javascript",
    "kotlin", "ladder", "lua", "pascal", "python", "rust", "scheme", "swift", "ruby", "dart", "objc", "r", "d", "fortran" };

// ---- CLI 解析 ----
string cmd = "";
string lang = "all";
string package = "";
string root = ".";
string className = "";
string outputFile = "";
string style = "one_two";
bool usePrefix = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-h": case "--help": PrintHelp(); return;
        case "-s": case "--scan": cmd = "scan"; break;
        case "-b": case "--build": cmd = "build"; break;
        case "-m": case "--modules": cmd = "modules"; break;
        case "-a": case "--aggregators": cmd = "aggregators"; break;
        case "-g": case "--bindings": cmd = "bindings"; break;
        case "-A": case "--all": cmd = "all"; break;
        case "-l": case "--lang":
            if (i + 1 < args.Length) { lang = args[++i].ToLower(); }
            else { Console.WriteLine("错误: -l 需要参数"); return; }
            break;
        case "-p": case "--package":
            if (i + 1 < args.Length) { package = args[++i]; }
            else { Console.WriteLine("错误: -p 需要参数"); return; }
            break;
        case "-r": case "--root":
            if (i + 1 < args.Length) { root = args[++i]; }
            else { Console.WriteLine("错误: -r 需要参数"); return; }
            break;
        case "-n": case "--gen-native": cmd = "native"; break;
        case "-c": case "--class":
            if (i + 1 < args.Length) { className = args[++i]; }
            else { Console.WriteLine("错误: -c 需要参数"); return; }
            break;
        case "-o": case "--file":
            if (i + 1 < args.Length) { outputFile = args[++i]; }
            else { Console.WriteLine("错误: -o 需要参数"); return; }
            break;
        case "--style":
            if (i + 1 < args.Length) { style = args[++i]; }
            else { Console.WriteLine("错误: --style 需要参数"); return; }
            break;
        case "--prefix": usePrefix = true; break;
        default:
            if (!args[i].StartsWith("-") && cmd == "")
                cmd = args[i].ToLower();
            else if (!args[i].StartsWith("-") && lang == "all" && !args[i].EndsWith('/') && !args[i].EndsWith('\\'))
                lang = args[i].ToLower();
            else if (args[i] == "." || args[i].EndsWith('/') || args[i].EndsWith('\\'))
                root = args[i];
            break;
    }
}

if (cmd == "") { PrintHelp(); return; }

// ---- 初始化路径 ----
var projectRoot = Path.GetFullPath(root);
var libRoot = Path.Combine(projectRoot, "Lib");
var sharedSrc = Path.Combine(libRoot, "shared", "src");
var sharedDir = Path.Combine(libRoot, "shared");

if (!Directory.Exists(sharedSrc))
{
    Console.WriteLine($"错误: 共享库源码目录不存在: {sharedSrc}");
    return;
}

// ---- 执行命令 ----
switch (cmd)
{
    case "scan":
        Scan(sharedSrc);
        break;
    case "build":
        BuildShared(sharedSrc, sharedDir);
        break;
    case "modules":
    {
        var funcs = ParseFunctions(sharedSrc);
        var cfg = ModuleConfig.LoadOrCreate(libRoot, funcs);
        var naming = NamingConfig.LoadOrCreate(libRoot);
        var funcMap = funcs.ToDictionary(f => f.Name);
        var targets = lang == "all" ? allLangs : new[] { lang };
        Parallel.ForEach(targets, t => GenModules(t, libRoot, cfg, naming, funcMap));
        break;
    }
    case "aggregators":
    {
        var funcs = ParseFunctions(sharedSrc);
        var cfg = ModuleConfig.LoadOrCreate(libRoot, funcs);
        var targets = lang == "all" ? allLangs : new[] { lang };
        Parallel.ForEach(targets, t => GenAggregators(t, libRoot, cfg));
        break;
    }
    case "bindings":
    {
        var funcs = ParseFunctions(sharedSrc);
        var targets = lang == "all" ? allLangs : new[] { lang };
        var gen = new BindingGenerator(libRoot, funcs);
        Parallel.ForEach(targets, t => gen.Generate(t, package));
        break;
    }
    case "native":
    {
        var funcs = ParseFunctions(sharedSrc);
        var cfg = ModuleConfig.LoadOrCreate(libRoot, funcs);
        // 按 -c 指定的类名过滤函数
        List<FuncDef> filtered;
        if (!string.IsNullOrEmpty(className) && cfg.TryGetValue(className, out var mod))
        {
            var funcSet = new HashSet<string>(mod.Functions);
            filtered = funcs.Where(f => funcSet.Contains(f.Name)).ToList();
            Console.WriteLine($"模块 '{className}': {filtered.Count} 个函数");
        }
        else if (!string.IsNullOrEmpty(className))
        {
            // 类名不在模块表中，尝试按源文件名匹配
            filtered = funcs.Where(f =>
                Path.GetFileNameWithoutExtension(f.File).Equals(className, StringComparison.OrdinalIgnoreCase)).ToList();
            if (filtered.Count == 0)
            {
                Console.WriteLine($"错误: 未知模块或源文件 '{className}'");
                Console.WriteLine($"可用模块: {string.Join(", ", cfg.Keys)}");
                return;
            }
            Console.WriteLine($"源文件 '{className}.c': {filtered.Count} 个函数");
        }
        else
        {
            filtered = funcs;
            Console.WriteLine($"所有函数: {filtered.Count} 个");
        }
        var cn = string.IsNullOrEmpty(className) ? "Native" : className;
        var gen = new NativeBindingGenerator(filtered, libRoot, cn, outputFile, usePrefix, style);
        var targets = lang == "all" ? allLangs : new[] { lang };
        foreach (var t in targets) gen.Generate(t);
        break;
    }
    case "all":
    {
        BuildShared(sharedSrc, sharedDir);
        var funcs = ParseFunctions(sharedSrc);
        var cfg = ModuleConfig.LoadOrCreate(libRoot, funcs);
        var naming = NamingConfig.LoadOrCreate(libRoot);
        var funcMap = funcs.ToDictionary(f => f.Name);
        var targets = lang == "all" ? allLangs : new[] { lang };
        Parallel.ForEach(targets, t =>
        {
            GenModules(t, libRoot, cfg, naming, funcMap);
            GenAggregators(t, libRoot, cfg);
        });
        var gen = new BindingGenerator(libRoot, funcs);
        Parallel.ForEach(targets, t => gen.Generate(t, package));
        break;
    }
    default:
        Console.WriteLine($"未知命令: {cmd}");
        PrintHelp();
        break;
}

// ============================================================
// 命令实现
// ============================================================

static void PrintHelp()
{
    Console.WriteLine("GenLib — VML 共享库流水线");
    Console.WriteLine("用法: genlib <命令> [选项]");
    Console.WriteLine();
    Console.WriteLine("命令:");
    Console.WriteLine("  -s, --scan               扫描 Lib/shared/src/*.c 导出函数");
    Console.WriteLine("  -b, --build             编译 C 源码 → VML (进程内调用 CCompiler)");
    Console.WriteLine("  -m, --modules           生成每种语言的模块包装 .vml");
    Console.WriteLine("  -a, --aggregators       生成 builtin/stdlib/vmllib 聚合文件");
    Console.WriteLine("  -g, --bindings          生成源文件级绑定 (shared.go, shared.py, ...)");
    Console.WriteLine("  -n, --gen-native        生成 native 封装源文件 (类/模块包装)");
    Console.WriteLine("  -A, --all               一键全流程 (-b + -m + -a + -g)");
    Console.WriteLine();
    Console.WriteLine("选项:");
    Console.WriteLine("  -l, --lang <语言>       指定语言 (默认: all)");
    Console.WriteLine("  -c, --class <类名>      类名/模块名 (用于 -n, 默认: 全部函数)");
    Console.WriteLine("  -o, --file <路径>       输出文件路径 (用于 -n)");
    Console.WriteLine("  -p, --package <包名>    指定包名/命名空间 (用于 -g)");
    Console.WriteLine("  --style <style>         命名风格 (用于 -n: one_two/OneTwo/oneTwo/...)");
    Console.WriteLine("  --prefix                带语言前缀 (用于 -n)");
    Console.WriteLine("  -r, --root <路径>       项目根目录 (默认: .)");
    Console.WriteLine("  -h, --help              显示帮助");
}

static void Scan(string sharedSrc)
{
    Console.WriteLine($"扫描 {sharedSrc}");
    var funcs = ParseFunctions(sharedSrc);
    Console.WriteLine($"发现 {funcs.Count} 个导出函数 (来自 {funcs.Select(f => f.File).Distinct().Count()} 个文件):");
    foreach (var f in funcs)
        Console.WriteLine($"  {f.Name}({f.Params}) -> {f.ReturnType} [{f.Convention}] @ {f.File}");
}

static void BuildShared(string sharedSrc, string sharedDir)
{
    Console.WriteLine($"编译共享库 (CCompiler 内置)...");
    int compiled = 0, skipped = 0;

    foreach (var cfile in Directory.GetFiles(sharedSrc, "*.c").OrderBy(f => f))
    {
        var name = Path.GetFileNameWithoutExtension(cfile);
        var vml = Path.Combine(sharedDir, name + ".vml");

        if (File.Exists(vml) && File.GetLastWriteTime(vml) >= File.GetLastWriteTime(cfile))
        { skipped++; continue; }

        Console.Write($"  {name}.c -> {name}.vml ... ");
        try
        {
            // 与 VMLTool 默认一致 (Int64Mode.Hard + Float64Mode.Hard)：
            // 共享库 convert64.vml/conv.vml 必须用硬件 64 位/双精度指令 (PUSHL/DIVL/I2L + MOVED/DPUSH)，
            // 否则 Soft 模式会生成 softint64/softdouble 库调用，与 VMLRuntime 的 L0-L7/D0-D7 寄存器语义不一致。
            var prog = VMLPlugins.CompilerOptionsContext.RunWith(
                new VMLPlugins.CompilerOptions {
                    Int64Mode = VMLPlugins.Int64Mode.Hard,
                    Float64Mode = VMLPlugins.Float64Mode.Hard
                },
                () => CCompiler.CCompiler.CompileFile(cfile, includePaths: null, libraryPaths: null, autoLinkStdLib: false));
            var lines = prog.ToString().Split('\n')
                .Where(l => !l.TrimStart().StartsWith(".entry") && !l.TrimStart().StartsWith(".stack") && !l.TrimStart().StartsWith(".vectors"))
                .ToArray();
            File.WriteAllText(vml, string.Join('\n', lines));
            Console.WriteLine("OK");
            compiled++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"失败: {ex.Message}");
        }
    }

    Console.WriteLine($"完成: {compiled} 编译, {skipped} 跳过");
}

static int ParamCount(string ps) =>
    string.IsNullOrEmpty(ps) || ps.Trim().Equals("void", StringComparison.OrdinalIgnoreCase) ? 0 : ps.Split(',').Length;

/// 从参数声明中提取类型（如 "double d" → "double", "long long x" → "long long"）
static string[] GetParamTypes(string ps)
{
    if (string.IsNullOrEmpty(ps) || ps.Trim().Equals("void", StringComparison.OrdinalIgnoreCase))
        return Array.Empty<string>();
    return ps.Split(',')
        .Select(p => {
            var trimmed = p.Trim();
            int lastSpace = trimmed.LastIndexOf(' ');
            return lastSpace >= 0 ? trimmed.Substring(0, lastSpace).Trim() : trimmed;
        })
        .ToArray();
}

/// 根据参数类型返回正确的 PUSH 指令
static (string[] lines, int bytes) EmitPushParam(string paramType, int regIdx)
{
    var t = (paramType ?? "").Trim().ToLower();
    switch (t)
    {
        case "float":
            return (new[] { $"    sub R13 #4", $"    movef @13 R{regIdx}" }, 4);
        case "double":
            return (new[] { $"    sub R13 #8", $"    moved @13 R{regIdx}" }, 8);
        case "long":
        case "long long":
        case "unsigned long":
        case "unsigned long long":
            return (new[] { $"    sub R13 #8", $"    movel @13 R{regIdx}" }, 8);
        // 1 字节整数: char/signed char/unsigned char
        // 实现体用 moveb 读取 + add R13 #5 清栈, 包装器必须只压 1 字节
        case "char":
        case "signed char":
        case "unsigned char":
        case "int8_t":
        case "uint8_t":
            return (new[] { $"    sub R13 #1", $"    moveb @13 R{regIdx}" }, 1);
        // 2 字节整数: short/unsigned short
        // 实现体用 moveh 读取 + add R13 #6 清栈, 包装器必须只压 2 字节
        case "short":
        case "short int":
        case "signed short":
        case "signed short int":
        case "unsigned short":
        case "unsigned short int":
        case "int16_t":
        case "uint16_t":
            return (new[] { $"    sub R13 #2", $"    moveh @13 R{regIdx}" }, 2);
        default:
            return (new[] { $"    PUSH R{regIdx}" }, 4);
    }
}

static void GenModules(string lang, string libRoot, Dictionary<string, ModuleDef> modules,
    Dictionary<string, LanguageNaming> naming, Dictionary<string, FuncDef> funcMap)
{
    var langDir = Path.Combine(libRoot, lang);
    if (!Directory.Exists(langDir)) { Console.WriteLine($"  {lang}: SKIP (目录不存在)"); return; }
    if (!naming.TryGetValue(lang, out var langNaming)) { Console.WriteLine($"  {lang}: SKIP (无命名配置)"); return; }

    int count = 0;
    foreach (var kv in modules)
    {
        var modName = kv.Key;
        var modDef = kv.Value;

        if (modDef.Functions.Count == 0) continue;

        var sb = new StringBuilder();
        sb.AppendLine($"; Auto-generated by GenLib — {lang} {modName} module");
        sb.AppendLine($"; 使用: .linked \"{modName}.vml\"");
        sb.AppendLine();

        // linked 共享库
        foreach (var inc in modDef.Includes)
            sb.AppendLine($".linked \"../shared/{inc}\"");

        sb.AppendLine();

        // 生成包装器
        foreach (var funcName in modDef.Functions)
        {
            var label = NamingConfig.ToLabel(funcName, langNaming);
            int pCount = 0;
            bool isCdecl = false;
            string[] paramTypes = Array.Empty<string>();
            if (funcMap.TryGetValue(funcName, out var fdef))
            {
                pCount = ParamCount(fdef.Params);
                isCdecl = string.IsNullOrEmpty(fdef.Convention);
                paramTypes = GetParamTypes(fdef.Params);
            }
            else
            {
                if (funcName.EndsWith("_str") || funcName.EndsWith("_wstr") || funcName.EndsWith("_ustr")
                    || funcName.EndsWith("_int") || funcName.EndsWith("_hex") || funcName.EndsWith("_float")
                    || funcName.EndsWith("_double") || funcName.EndsWith("_long") || funcName.EndsWith("_char"))
                    pCount = 1;
                else if (funcName.EndsWith("_str_no_nl")) pCount = 1;
                isCdecl = true; // v1.66.63: 未知函数默认 cdecl，包装器负责清理参数栈
            }

            sb.AppendLine($"LABEL {label}");
            int totalArgBytes = 0;
            for (int i = pCount - 1; i >= 0; i--)
            {
                string type = i < paramTypes.Length ? paramTypes[i] : "";
                var (lines, bytes) = EmitPushParam(type, i);
                foreach (var line in lines)
                    sb.AppendLine(line);
                totalArgBytes += bytes;
            }
            sb.AppendLine($"    CALL {funcName}");
            if (isCdecl && totalArgBytes > 0)
                sb.AppendLine($"    ADD R13 #{totalArgBytes}");
            sb.AppendLine("    RET");
            sb.AppendLine();
        }

        // 生成类风格别名 (配置中定义的 + 自动模块名PascalCase)
        var aliasPrefixes = new HashSet<string>();
        foreach (var alias in langNaming.Aliases)
        {
            if (alias.Module != modName) continue;
            aliasPrefixes.Add(alias.Prefix);
        }
        // 自动类别名: 模块名 PascalCase → 如 conv → Conv_
        if (langNaming.LabelStyle is "PascalCase_method" or "pascalCase_method")
        {
            var autoPrefix = char.ToUpper(modName[0]) + modName[1..] + "_";
            aliasPrefixes.Add(autoPrefix);
        }
        // 自动别名: func_ / method_ 原始名 (兼容旧编译器)
        if (langNaming.LabelStyle == "snake_lower")
            aliasPrefixes.Add("func_");
        if (langNaming.LabelStyle == "pascalCase_method")
            aliasPrefixes.Add("method_");

        foreach (var prefix in aliasPrefixes)
        {
            foreach (var funcName in modDef.Functions)
            {
                var primaryLabel = NamingConfig.ToLabel(funcName, langNaming);
                // func_/method_/word_: 生成 snake_case + PascalCase + camelCase 三种版本
                if (prefix == "func_" || prefix == "method_" || prefix == "word_")
                {
                    var pascal = NamingConfig.ToPascalCase(funcName);
                    // 1. func_int_to_str (snake_case 原始名, Fortran 等使用)
                    var aliasSnake = prefix + funcName;
                    if (aliasSnake != primaryLabel)
                    {
                        sb.AppendLine($"LABEL {aliasSnake}");
                        sb.AppendLine($"    JMP {primaryLabel}");
                        sb.AppendLine();
                    }
                    // 2. func_IntToStr (PascalCase, Dart/R 编译器可能使用)
                    var aliasPascal = prefix + pascal;
                    if (aliasPascal != aliasSnake && aliasPascal != primaryLabel)
                    {
                        sb.AppendLine($"LABEL {aliasPascal}");
                        sb.AppendLine($"    JMP {primaryLabel}");
                        sb.AppendLine();
                    }
                    // 3. func_intToStr (camelCase, Dart 编译器实际使用)
                    var aliasCamel = prefix + char.ToLower(pascal[0]) + pascal[1..];
                    if (aliasCamel != aliasSnake && aliasCamel != aliasPascal && aliasCamel != primaryLabel)
                    {
                        sb.AppendLine($"LABEL {aliasCamel}");
                        sb.AppendLine($"    JMP {primaryLabel}");
                        sb.AppendLine();
                    }
                }
                else
                {
                    var aliasLabel = NamingConfig.ToAliasLabel(funcName, prefix);
                    if (aliasLabel != primaryLabel)
                    {
                        sb.AppendLine($"LABEL {aliasLabel}");
                        sb.AppendLine($"    JMP {primaryLabel}");
                        sb.AppendLine();
                    }
                }
            }
        }

        var outPath = Path.Combine(langDir, modName + ".vml");
        File.WriteAllText(outPath, sb.ToString());
        count++;
    }

    Console.WriteLine($"  {lang}: {count} 模块");
}

static void GenAggregators(string lang, string libRoot, Dictionary<string, ModuleDef> modules)
{
    var langDir = Path.Combine(libRoot, lang);
    if (!Directory.Exists(langDir)) { Console.WriteLine($"  {lang}: SKIP (目录不存在)"); return; }

    var coreMods = modules.Where(kv => kv.Value.Core).Select(kv => kv.Key).ToList();

    // ---- builtin.vml ----
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; Auto-generated by GenLib — {lang} Built-in Library");
        sb.AppendLine($"; 编译器自动链接, 无需手动 .linked");
        sb.AppendLine();
        sb.AppendLine(".linked \"builtins.vml\"");
        sb.AppendLine($".linked \"builtins_{lang}.vml\"");
        sb.AppendLine(".linked \"device.vml\"");
        sb.AppendLine(".linked \"sysinfo.vml\"");
        sb.AppendLine();
        sb.AppendLine("#ifdef VML_FLOAT32_SOFT");
        sb.AppendLine(".linked \"softfloat.vml\"");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef VML_FLOAT64_SOFT");
        sb.AppendLine(".linked \"softdouble.vml\"");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef VML_INT64_SOFT");
        sb.AppendLine(".linked \"softint64.vml\"");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // builtin 通用模块: 链接所有存在模块包装器的库
        foreach (var mod in new[] { "math", "system", "convert", "conv",
            "string", "io", "printf", "scanf", "ctype", "bitops", "util", "float",
            "file", "time", "encoding", "memory", "network", "os" })
        {
            if (File.Exists(Path.Combine(langDir, $"{mod}.vml")))
                sb.AppendLine($".linked \"{mod}.vml\"");
        }
        // Lua 额外运行时
        if (lang == "lua" && File.Exists(Path.Combine(langDir, "lua_meta.vml")))
            sb.AppendLine(".linked \"lua_meta.vml\"");

        File.WriteAllText(Path.Combine(langDir, "builtin.vml"), sb.ToString());
    }

    // ---- stdlib.vml ----
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; Auto-generated by GenLib — {lang} Standard Library");
        sb.AppendLine($"; 使用: .linked \"stdlib.vml\"");
        sb.AppendLine();
        sb.AppendLine(".linked \"builtin.vml\"");
        sb.AppendLine();

        foreach (var mod in new[] { "string", "time", "file" })
        {
            if (coreMods.Contains(mod))
                sb.AppendLine($".linked \"{mod}.vml\"");
        }

        // 直接引用共享模块（无语言特定包装的模块）
        sb.AppendLine(".linked \"printf.vml\"");
        sb.AppendLine(".linked \"scanf.vml\"");
        sb.AppendLine(".linked \"ctype.vml\"");
        sb.AppendLine(".linked \"bitops.vml\"");
        sb.AppendLine(".linked \"convert.vml\"");
        sb.AppendLine(".linked \"memory.vml\"");
        sb.AppendLine(".linked \"readline.vml\"");

        File.WriteAllText(Path.Combine(langDir, "stdlib.vml"), sb.ToString());
    }

    // ---- vmllib.vml ----
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; Auto-generated by GenLib — {lang} Full VM Library");
        sb.AppendLine($"; 使用: .linked \"vmllib.vml\"");
        sb.AppendLine();
        sb.AppendLine(".linked \"stdlib.vml\"");
        sb.AppendLine(".linked \"os.vml\"");
        sb.AppendLine(".linked \"network.vml\"");
        sb.AppendLine(".linked \"vga_text.vml\"");

        File.WriteAllText(Path.Combine(langDir, "vmllib.vml"), sb.ToString());
    }

    Console.WriteLine($"  {lang}: builtin + stdlib + vmllib");
}

// ============================================================
// 共享函数解析
// ============================================================

static List<FuncDef> ParseFunctions(string srcDir)
{
    var result = new List<FuncDef>();
    foreach (var file in Directory.GetFiles(srcDir, "*.c").OrderBy(f => f))
    {
        var src = File.ReadAllText(file);
        var matches = Regex.Matches(src, @"(?<!\bstatic\s)(?<!\bstatic\n)(?:(__stdcall|__cdecl|__fastcall)\s+)?((?:const\s+)?(?:unsigned\s+)?[a-zA-Z_][a-zA-Z0-9_]*\s*\*?)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(([^)]*)\)");
        foreach (Match m in matches)
        {
            var fnName = m.Groups[3].Value;
            if (IsKeyword(fnName) || fnName.StartsWith("__") || fnName == "main") continue;
            result.Add(new FuncDef
            {
                File = Path.GetFileName(file),
                Name = fnName,
                ReturnType = m.Groups[2].Value.Trim(),
                Convention = m.Groups[1].Value.Trim(),
                Params = m.Groups[4].Value.Trim()
            });
        }
    }
    return result.DistinctBy(f => f.Name).ToList();
}

static bool IsKeyword(string s) => s is "if" or "while" or "for" or "sizeof" or "return" or "switch" or "else" or "do" or "goto" or "case";

// ============================================================
// 源文件绑定生成器（原有 gen 逻辑）
// ============================================================

internal class BindingGenerator
{
    readonly string _libRoot;
    readonly List<FuncDef> _funcs;
    readonly List<FuncDef> _exported;

    public BindingGenerator(string libRoot, List<FuncDef> funcs)
    {
        _libRoot = libRoot;
        _funcs = funcs;
        _exported = funcs.ToList(); // v1.66.55+: 所有函数均为公开导出，无前缀过滤
    }

    public void Generate(string lang, string package = "")
    {
        var dir = Path.Combine(_libRoot, lang);
        if (!Directory.Exists(dir)) return;

        switch (lang)
        {
            case "c": GenC(dir); break;
            case "cpp": GenCpp(dir); break;
            case "basic": GenBasic(dir); break;
            case "python": GenPython(dir); break;
            case "rust": GenRust(dir); break;
            case "go": GenGo(dir, package); break;
            case "java": GenJava(dir, package); break;
            case "csharp": GenCSharp(dir, package); break;
            case "javascript": GenJavaScript(dir); break;
            case "lua": GenLua(dir); break;
            case "swift": GenSwift(dir); break;
            case "kotlin": GenKotlin(dir); break;
            case "pascal": GenPascal(dir); break;
            case "scheme": GenScheme(dir); break;
            case "forth": GenForth(dir); break;
            case "ladder": GenLadder(dir); break;
            case "ruby": WriteStub(dir, "shared.rb", "ruby"); break;
            case "dart": WriteStub(dir, "shared.dart", "dart"); break;
            case "objc": WriteStub(dir, "shared.h", "objc"); break;
            case "r": WriteStub(dir, "shared.r", "r"); break;
            case "d": WriteStub(dir, "shared.d", "d"); break;
            case "fortran": WriteStub(dir, "shared.f90", "fortran"); break;
            default: return;
        }
        Console.WriteLine($"  {lang}: OK");
    }

    void GenC(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — C bindings");
        foreach (var g in _exported.GroupBy(f => Path.GetFileNameWithoutExtension(f.File)).OrderBy(x => x.Key))
        {
            sb.AppendLine($"// {g.Key}.c");
            foreach (var f in g) sb.AppendLine($"{f.Convention} {f.ReturnType} {f.Name}({f.Params});");
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(dir, "shared_bindings.h"), sb.ToString());
    }

    void GenCpp(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib");
        sb.AppendLine("extern \"C\" {");
        foreach (var f in _exported) sb.AppendLine($"    {f.Convention} {f.ReturnType} {f.Name}({f.Params});");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(dir, "shared_bindings.hpp"), sb.ToString());
    }

    void GenBasic(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("' Auto-generated by GenLib");
        foreach (var f in _exported)
        {
            var kw = f.ReturnType == "void" ? "SUB" : "FUNCTION";
            var bparams = string.IsNullOrEmpty(f.Params) || f.Params == "void" ? "" :
                string.Join(", ", Enumerable.Range(0, f.Params.Split(',').Length).Select(i => $"a{i} AS INTEGER"));
            sb.AppendLine($"DECLARE {kw} {f.Name}({bparams}){(f.ReturnType == "void" ? "" : " AS INTEGER")}");
            sb.AppendLine($"    asm(\"CALL {f.Name}\")");
            if (f.ReturnType != "void") sb.AppendLine($"    {f.Name} = 0");
            sb.AppendLine($"END {kw}\n");
        }
        File.WriteAllText(Path.Combine(dir, "shared.bas"), sb.ToString());
    }

    void GenPython(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated by GenLib — Python shared bindings");
        foreach (var f in _exported)
        {
            var parts = string.IsNullOrEmpty(f.Params) || f.Params == "void" ? new List<string>() :
                f.Params.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            var pnames = string.Join(", ", Enumerable.Range(0, parts.Count).Select(i => $"a{i}"));
            sb.AppendLine($"def {f.Name}({pnames}):");
            if (f.ReturnType != "void") sb.AppendLine("    r0 = asm(\"R0\")");
            for (int i = parts.Count - 1; i >= 0; i--)
                sb.AppendLine($"    asm(f\"PUSH R0\")  # push a{i}");
            sb.AppendLine($"    asm(\"CALL {f.Name}\")");
            if (f.ReturnType != "void") sb.AppendLine("    return r0");
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(dir, "shared.py"), sb.ToString());
    }

    void GenRust(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Rust shared bindings");
        foreach (var f in _exported)
        {
            var rp = string.IsNullOrEmpty(f.Params) || f.Params == "void" ? "" :
                string.Join(", ", Enumerable.Range(0, f.Params.Split(',').Length).Select(i => $"a{i}: i32"));
            sb.AppendLine($"fn {f.Name}({rp}){(f.ReturnType == "void" ? "" : " -> i32")} {{");
            sb.AppendLine($"    asm!(\"CALL {f.Name}\")");
            if (f.ReturnType != "void") { sb.AppendLine("    let r: i32;"); sb.AppendLine("    asm!(\"MOVE {{0}}, R0\", out(reg) r);"); sb.AppendLine("    r"); }
            sb.AppendLine("}\n");
        }
        File.WriteAllText(Path.Combine(dir, "shared.rs"), sb.ToString());
    }

    void GenGo(string dir, string package)
    {
        var pkg = string.IsNullOrEmpty(package) ? "main" : package;
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — Go shared bindings");
        sb.AppendLine($"package {pkg}\n");
        foreach (var f in _exported)
        {
            var gp = string.IsNullOrEmpty(f.Params) || f.Params == "void" ? "" :
                string.Join(", ", Enumerable.Range(0, f.Params.Split(',').Length).Select(i => $"a{i} int32"));
            sb.AppendLine($"func {f.Name}({gp}){(f.ReturnType == "void" ? "" : " int32")} {{");
            sb.AppendLine($"    vml.Call(\"{f.Name}\")");
            if (f.ReturnType != "void") sb.AppendLine("    return vml.R0()");
            sb.AppendLine("}\n");
        }
        File.WriteAllText(Path.Combine(dir, "shared.go"), sb.ToString());
    }

    void GenJava(string dir, string package)
        => WriteStub(dir, "shared.java", "java", package);

    void GenCSharp(string dir, string package)
    {
        var ns = string.IsNullOrEmpty(package) ? "VML" : package;
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib — C# shared bindings");
        sb.AppendLine($"namespace {ns} {{");
        foreach (var f in _exported)
            sb.AppendLine($"    // extern {f.ReturnType} {f.Name}({f.Params});  // CALL {f.Name}");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(dir, "shared.cs"), sb.ToString());
    }

    void GenJavaScript(string dir)
        => WriteStub(dir, "shared.js", "javascript");

    void GenLua(string dir)
        => WriteStub(dir, "shared.lua", "lua");

    void GenSwift(string dir)
        => WriteStub(dir, "shared.swift", "swift");

    void GenKotlin(string dir)
        => WriteStub(dir, "shared.kt", "kotlin");

    void GenPascal(string dir)
        => WriteStub(dir, "shared.pas", "pascal");

    void GenScheme(string dir)
        => WriteStub(dir, "shared.scm", "scheme");

    void GenForth(string dir)
        => WriteStub(dir, "shared.fth", "forth");

    void GenLadder(string dir)
        => WriteStub(dir, "shared.ld", "ladder");

    void WriteStub(string dir, string filename, string lang, string package = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// Auto-generated by GenLib — {lang} shared bindings");
        if (!string.IsNullOrEmpty(package))
            sb.AppendLine($"// package: {package}");
        foreach (var f in _exported)
        {
            sb.AppendLine($"// extern fn {f.Name}({f.Params}) -> {f.ReturnType}");
            sb.AppendLine($"// CALL {f.Name}");
        }
        File.WriteAllText(Path.Combine(dir, filename), sb.ToString());
        Console.WriteLine($"  ({_exported.Count} stubs)");
    }
}
