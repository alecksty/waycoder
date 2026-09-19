using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenLib;
using static GenLib.GenWriter;


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
        // 跳过名单：`modules.json` 里那些在 `Lib/shared/src/*.c` 里查不到的**虚构函数名**。
        // 并发（Parallel.ForEach）所以用 ConcurrentQueue；语言之间会重复报同一个名字，
        // 最后去重再打印 —— 打印的是**模块.函数**，直接对应 `modules.json` 里该删的条目。
        var skipped = new System.Collections.Concurrent.ConcurrentQueue<string>();
        Parallel.ForEach(targets, t => GenModules(t, libRoot, cfg, naming, funcMap, skipped));
        var distinct = skipped.Distinct().OrderBy(x => x).ToList();
        if (distinct.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"⚠ {distinct.Count} 个 modules.json 条目在 Lib/shared/src/*.c 里**不存在**，已跳过（没有生成包装器）：");
            foreach (var s in distinct) Console.WriteLine($"    {s}");
            Console.WriteLine("  ⇒ 这些是虚构条目：它们生成的 `LABEL x / CALL x` 是死包装器，");
            Console.WriteLine("    会让正常程序在链接期冒出「未解析标签」。请从 modules.json 里删掉。");
        }
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
        var skippedInAll = new System.Collections.Concurrent.ConcurrentQueue<string>();
        Parallel.ForEach(targets, t =>
        {
            GenModules(t, libRoot, cfg, naming, funcMap, skippedInAll);
            GenAggregators(t, libRoot, cfg);
        });
        var distinctInAll = skippedInAll.Distinct().OrderBy(x => x).ToList();
        if (distinctInAll.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"⚠ {distinctInAll.Count} 个 modules.json 条目在 Lib/shared/src/*.c 里**不存在**，已跳过：");
            foreach (var s in distinctInAll) Console.WriteLine($"    {s}");
            Console.WriteLine("  ⇒ 这些是虚构条目，应从 modules.json 里删掉（它们生成的是死包装器）。");
        }
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
            WriteGen(vml, string.Join('\n', lines));
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
            // ⚠ 统一约定：形参槽一律 4 字节（C 前端 ParamStackBytes），所以**压满一格**。
            // 按自然大小压 1 字节会让后续形参整体错位 —— 与脚本判据 p6（short 形参）同一个病。
            return (new[] { $"    sub R13 #4", $"    moveb @13 R{regIdx}" }, 4);
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
            // ⚠ 同上：short 也压满一格（4 字节），不再按自然大小压 2 字节。
            return (new[] { $"    sub R13 #4", $"    moveh @13 R{regIdx}" }, 4);
        default:
            return (new[] { $"    PUSH R{regIdx}" }, 4);
    }
}

static void GenModules(string lang, string libRoot, Dictionary<string, ModuleDef> modules,
    Dictionary<string, LanguageNaming> naming, Dictionary<string, FuncDef> funcMap,
    System.Collections.Concurrent.ConcurrentQueue<string> skipped)
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
            var label = SafeWrapperLabel(NamingConfig.ToLabel(funcName, langNaming), funcName, lang);

            // ⚠ 主标签与 C 符号名**同名**时，下面生成的 `LABEL x … CALL x` 是**自调用**（无限递归）。
            //
            // 这不是理论风险：`LabelStyle = pascalCase_method` 的语言（javascript / java）
            // 走 camelCase，而**单词名**（`ipow` / `pow` / `abs` / `sqrt`）camelCase 之后还是它自己，
            // 只要 `PrimaryPrefix` 为空就会撞上。实测 `Lib/javascript/math.vml` 的
            // `LABEL ipow … CALL ipow` → 递归到底、SP 归零。
            // 更阴的是**它靠链接顺序决定要不要发作**：同一份 `LABEL ipow / CALL ipow`，
            // Java 那次链接器选了共享实现所以看着正常，JS 这次选了自己 —— 这种运气不能留。
            //
            // 判据：标签必须与 C 符号名不同。修法是给该语言的 `PrimaryPrefix` 一个非空值
            //（java 用 `java_`、javascript 用 `js_`），与其余 19 种语言的做法一致。
            //
            // ⚠ 只在**函数真的存在于共享库**（`funcMap` 里查得到）时才报错。
            // `modules.json` 里有一批**虚构条目**（例如 `array64` 模块下的 `Arrays` ——
            // C 源里没有这个函数），它们生成的包装器是死代码，报出来只会误导。
            // 真实冲突才算数：那意味着「调用这个库函数就会无限递归」。
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
                // ⚠ **这里的兜底分支曾经是「编造签名照样生成」** —— 而 `modules.json` 里有一批
                //   **虚构的函数名**（`Lib/shared/src/*.c` 里根本没有：`Sector` / `Arrays` /
                //   `CMD_BUF` / `Font` / `Manipulation` …，2026-09-19 实测共 59 个、涉及 21 个模块）。
                //   照着它们生成出来的就是 `LABEL c_Sector` + `CALL Sector` 这种**死包装器**：
                //   自己没有实现、调用的名字也不存在。
                //
                //   后果分两种，都不报错：
                //     · basic / csharp / ladder / pascal 是 `SCREAMING_SNAKE`/`PascalCase`，
                //       **不生成 `func_*` 别名** ⇒ 死调用赤裸裸地变成**未解析标签**，
                //       正常程序一编译就冒出 49~60 条警告；
                //     · 另外 18 门因为链接器的别名机制把 `CALL Arrays` 救成了
                //       `func_Arrays → JMP c_Arrays → CALL Arrays` ⇒ 未解析数 0，
                //       但那是**一条自递归死循环**（与 `Lib/javascript/math.vml` 的
                //       `LABEL ipow / CALL ipow` 完全同型）。
                //
                //   ⇒ **查不到就跳过**，并累计到 `_skippedFunctions` 里在最后报告。
                //     判据由「编译期不报错」变成「生成器自己喊出来」——
                //     这正是本仓那条规矩：**宁可报错也不静默丢**。
                skipped.Enqueue($"{modName}.{funcName}");
                continue;
            }

            sb.AppendLine($"LABEL {label}");

            // ⚠ **包装器从自己的栈帧读实参**（2026-09-17 调用约定统一）。
            //
            // 原先这里直接发 `PUSH R{i}` —— 那是**旧寄存器 ABI**（第 i 个形参在 R{i}）。
            // 统一之后实参一律在调用方的栈上，包装器再去 R1-R3 里找就是残留值：
            // 单参调用时 R0 恰好等于刚求值完的那个实参，所以**看不出问题**；
            // 多参才露馅（实测 `ipow(2, 3)` 经包装器得 `1` = `ipow(x, 0)`，因为 R1 恰为 0）。
            // C 前端能跑只是因为它在调用点额外做了 R0-R3 镜像（commit 5655c305）。
            //
            // 现在包装器只认一条规则「实参在栈上、右到左」，不再关心调用方是谁 ——
            // 顺带把那套镜像从「每个前端都要记得做」变成「包装器这一层做一次」。
            sb.AppendLine("    push R15");
            sb.AppendLine("    push R12");
            sb.AppendLine("    move R12 R13");

            // 实参区布局：`[R12+12]` = 第 1 个参数
            //（R12 上方依次是序言存下的 R12、R15，再往上是 CALL 压的返回地址，之后才是实参）
            int[] argOff = new int[pCount];
            {
                int off = 12;
                for (int i = 0; i < pCount; i++)
                {
                    argOff[i] = off;
                    off += EmitPushParam(i < paramTypes.Length ? paramTypes[i] : "", 0).bytes;
                }
            }

            int totalArgBytes = 0;
            for (int i = pCount - 1; i >= 0; i--)
            {
                string type = i < paramTypes.Length ? paramTypes[i] : "";
                // 先把第 i 个实参从帧里取到 R0，再复用同一套按类型压栈的指令
                sb.AppendLine($"    move R0 [R12+{argOff[i]}]");
                var (lines, bytes) = EmitPushParam(type, 0);
                foreach (var line in lines)
                    sb.AppendLine(line);
                totalArgBytes += bytes;
            }

            // 镜像 arg0..arg3 进 R0-R3 —— 给实现体里那 543 处 `asm("SYSCALL #6")` 用
            //（参数直接吃 R0，把「第一个形参在 R0」烙死在源码里）。
            // ⚠ 必须在所有压栈**之后**做：压栈过程本身会用到 R0。
            {
                int off = 0;
                for (int i = 0; i < pCount && i < 4; i++)
                {
                    sb.AppendLine($"    move R{i} [R13+{off}]");
                    off += EmitPushParam(i < paramTypes.Length ? paramTypes[i] : "", 0).bytes;
                }
            }

            sb.AppendLine($"    CALL {funcName}");
            // ⚠ 统一约定：**一律由调用方清栈**（原来只在 isCdecl 时发）。
            // 旧行为下非 cdecl 的包装器不发清栈，是指望被调方弹掉自己压的那格 ——
            // 那正是 Lib/c/*.vml 里 `PUSH R0 / CALL x / RET` thunk 的成因，
            // 也是「两套栈清理约定并存」那一族缺陷的源头。
            if (totalArgBytes > 0)
                sb.AppendLine($"    ADD R13 #{totalArgBytes}");
            // 拆掉本包装器自己的帧（序言压的 R12/R15）—— 被调方已是裸 `ret`，不弹实参
            sb.AppendLine("    move R13 R12");
            sb.AppendLine("    pop R12");
            sb.AppendLine("    pop R15");
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
                // 虚构条目在上面的包装器循环里已经跳过 —— 别名段**必须同样跳过**，
                // 否则会生成 `LABEL func_Arrays / JMP c_Arrays`，而 `c_Arrays` 根本不存在：
                // 未解析标签换个名字继续存在（而且更隐蔽 —— 调用点写的是裸名 `Arrays`，
                // 链接器的别名机制会把它引到这里）。
                if (!funcMap.ContainsKey(funcName)) continue;
                var primaryLabel = SafeWrapperLabel(NamingConfig.ToLabel(funcName, langNaming), funcName, lang);
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

        // ⚠ **一个包装器都没生成出来 ⇒ 不要写文件，并把已存在的删掉。**
        //
        // 模块的条目全是被跳过的虚构名时（`syscall` 就是 6/6 全虚），这里以前会写出一个
        // 「只有 `.linked` 头、没有任何 LABEL」的文件并留在盘上。那种**孤儿产物**没有任何判据抓得到：
        // `check-vml-patches.sh` 的判据①只比对"补丁该产出的那些文件"、判据②a 只比对
        // "GenLib 写过的文件" —— 一个不再被生成器写、却还躺在盘上的文件，两边都不覆盖。
        // （这正是"陈旧产物"长出来的方式，记在 `FRONTEND_DEFECTS.md` 的工具链那节。）
        // ⚠ 判据是「**既没有 LABEL、也没有 `.linked`**」，不是「没有 LABEL」。
        //   第一版只看了 LABEL，于是把 `util` / `syscall` 这两个**聚合模块**也删了 ——
        //   它们的 `Includes` 非空（`.linked "../shared/util.vml"`），
        //   而 `Lib/<lang>/builtin.vml:29` 正写着 `.linked "util.vml"` ⇒ 删完那条引用就断了
        //   （降级成「文件不存在」警告，共享模块也跟着链不进来）。
        //   一个"包装器全被跳光、但还 re-export 共享模块"的模块**是有用的**，必须照写。
        if (!sb.ToString().Contains("\nLABEL ") && modDef.Includes.Count == 0)
        {
            var orphan = Path.Combine(langDir, modName + ".vml");
            if (File.Exists(orphan))
            {
                File.Delete(orphan);
                Console.WriteLine($"  {lang}: 删除孤儿产物 {modName}.vml（该模块已无有效函数）");
            }
            continue;
        }

        var outPath = Path.Combine(langDir, modName + ".vml");
        WriteGen(outPath, sb.ToString());
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
        // ⚠ **`console` 必须在列** —— `CSharpCompiler/CodeGenerator.cs` 把
        //   `Console.Write/WriteLine` 编成 **PascalCase 标签** `PrintlnStr` / `PrintStr` /
        //   `PrintlnInt` / `PrintInt`，并注明「标签定义在 `Lib/csharp/console.vml`」。
        //   清单里没有 `console` ⇒ 那个模块永远不会被链进来 ⇒ C# 程序里
        //   **只有调用点、没有定义**，运行期抛「未找到标签: PrintlnStr」
        //   （实测 `out.cs` 链的 43~51 个模块里始终没有任何 console 模块）。
        //   `File.Exists` 兜着 —— 没有该模块的语言不会被多链。
        foreach (var mod in new[] { "math", "system", "convert", "conv",
            "string", "io", "printf", "scanf", "ctype", "bitops", "util", "float",
            "console",
            "file", "time", "encoding", "memory", "network", "os" })
        {
            if (File.Exists(Path.Combine(langDir, $"{mod}.vml")))
                sb.AppendLine($".linked \"{mod}.vml\"");
        }
        // Lua 额外运行时
        if (lang == "lua" && File.Exists(Path.Combine(langDir, "lua_meta.vml")))
            sb.AppendLine(".linked \"lua_meta.vml\"");

        WriteGen(Path.Combine(langDir, "builtin.vml"), sb.ToString());
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

        WriteGen(Path.Combine(langDir, "stdlib.vml"), sb.ToString());
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

        WriteGen(Path.Combine(langDir, "vmllib.vml"), sb.ToString());
    }

    Console.WriteLine($"  {lang}: builtin + stdlib + vmllib");
}

// ============================================================
// 共享函数解析
// ============================================================

/// <summary>
/// 包装器主标签的**撞名保险**。
///
/// 若生成的标签与 C 符号名相同，那么下面那句 `CALL {funcName}` 会解析到**包装器自己**
/// ——`LABEL x … CALL x` 就是无限递归，实测把栈跑到 SP 归零。
///
/// 哪些情况会撞：`LabelStyle` 是 PascalCase/camelCase 的语言（java / csharp / javascript）
/// 遇到**本身就是 PascalCase、或单词小写**的 C 函数名 ——
/// `ipow`（camelCase 后还是 `ipow`）、`GetDate`/`GetTime`（`dos.c`，camelCase/PascalCase 都是它自己）。
/// 2026-09-17 修的 javascript 就是这个病；当时 java 没发作只是**链接器碰巧选了共享实现**，
/// 靠链接顺序的运气不能留 —— 所以这里一律改名，而不是指望各语言的前缀配置记得填。
///
/// 改成 `{lang}_{原标签}`：别名（`func_xxx` / `Math_xxx` …）指向它，调用方照旧；
/// 包装器里的 `CALL {funcName}` 于是解析到共享实现而不是自己。
/// </summary>
static string SafeWrapperLabel(string label, string funcName, string lang)
    => label == funcName ? $"{lang}_{label}" : label;

/// <summary>把 C 源码里的注释换成等长空白（保住偏移与行号，正则的 `\n` 前瞻仍然可用）。</summary>
static string StripComments(string src)
{
    var sb = new System.Text.StringBuilder(src.Length);
    for (int i = 0; i < src.Length; i++)
    {
        if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '/')
        {
            while (i < src.Length && src[i] != '\n') { sb.Append(' '); i++; }
            if (i < src.Length) sb.Append('\n');
        }
        else if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '*')
        {
            sb.Append("  "); i += 2;
            while (i < src.Length && !(src[i] == '*' && i + 1 < src.Length && src[i + 1] == '/'))
            {
                sb.Append(src[i] == '\n' ? '\n' : ' '); i++;
            }
            if (i < src.Length) { sb.Append("  "); i++; }
        }
        else
        {
            sb.Append(src[i]);
        }
    }
    return sb.ToString();
}

static List<FuncDef> ParseFunctions(string srcDir)
{
    var result = new List<FuncDef>();
    foreach (var file in Directory.GetFiles(srcDir, "*.c").OrderBy(f => f))
    {
        // ⚠ **先剥注释再匹配**（2026-09-17 加）。正则 `RET NAME(params)` 看不出注释与代码的区别，
        // 于是 `array64.c` 第 4 行的
        //     // VML Shared Array64 Library — 64-bit Integer Arrays (long* with long indices)
        // 被读成「返回 Integer、函数名 Arrays、参数 long* with long indices」——
        // 从此 funcMap 与 `Lib/modules.json` 里多了一批**不存在的函数**，每个还会生成一个
        // `LABEL x … CALL x` 的**自调用死包装器**（与 2026-09-17 修的 java/javascript
        // 撞名自调用是同一个机制：将来撞上真标签就是静默劫持）。
        // 实测剥离之前有 5 个虚构条目：Arrays / Manipulation / CRC / GetDate / GetTime。
        var src = StripComments(File.ReadAllText(file));
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
        WriteGen(Path.Combine(dir, "shared_bindings.h"), sb.ToString());
    }

    void GenCpp(string dir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by GenLib");
        sb.AppendLine("extern \"C\" {");
        foreach (var f in _exported) sb.AppendLine($"    {f.Convention} {f.ReturnType} {f.Name}({f.Params});");
        sb.AppendLine("}");
        WriteGen(Path.Combine(dir, "shared_bindings.hpp"), sb.ToString());
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
        WriteGen(Path.Combine(dir, "shared.bas"), sb.ToString());
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
        WriteGen(Path.Combine(dir, "shared.py"), sb.ToString());
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
        WriteGen(Path.Combine(dir, "shared.rs"), sb.ToString());
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
        WriteGen(Path.Combine(dir, "shared.go"), sb.ToString());
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
        WriteGen(Path.Combine(dir, "shared.cs"), sb.ToString());
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
        WriteGen(Path.Combine(dir, filename), sb.ToString());
        Console.WriteLine($"  ({_exported.Count} stubs)");
    }
}

namespace GenLib
{
    /// <summary>
    /// 生成物写出：统一成 LF。
    /// StringBuilder.AppendLine 用的是 Environment.NewLine，
    /// Windows 上是 CRLF、Linux/macOS 上是 LF，
    /// 会让 Lib/ 的字节随平台而变，
    /// 于是 check-vml-patches.sh 的「重生成 == 工作区」
    /// 判据在任何行尾设置下都不可能两边都绿。
    /// （2026-09-17 实测：同一个 Lib/cpp/math.vml，
    /// 忽略 CR 逐字节相同、不忽略差 2114 行。）
    /// </summary>
    internal static class GenWriter
    {
        public static void WriteGen(string path, string text)
            => File.WriteAllText(path, text.Replace("\r\n", "\n"));
    }
}
