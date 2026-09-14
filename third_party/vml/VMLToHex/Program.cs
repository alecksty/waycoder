using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using VMLAssembler;
using VMLPlugins.Interfaces;
using VMLTranslators;
using VMLToHex.Assemblers;

namespace VMLToHex;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            Help();
            return;
        }
        if (args[0] == "-v" || args[0] == "-V" || args[0] == "--version")
        {
            Console.WriteLine(VMLPlugins.VersionInfo.Product("VMLToHex"));
            return;
        }

        try
        {
            var inputFile = args[0];
            var outputFile = "";
            var format = "hex";
            var arch = "";
            var lang = "";
            var baseAddr = 0;
            var biosMode = "auto";
            var optLevel = 1;
            uint romBase = 0x08000000;
            uint ramBase = 0x20000000;
            uint ramSize = 0x10000;  // 64KB
            string? chip = null;
            var x86Mode = "32";       // x86 模式: 16 (实模式/COM) 或 32 (保护模式)
            var gasMode = false;       // GAS 汇编语法 (GCC)

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length) outputFile = args[++i];
                else if (args[i] == "-O" && i + 1 < args.Length) optLevel = int.Parse(args[++i]);
                else if (args[i] == "-O0") optLevel = 0;
                else if (args[i] == "-O1") optLevel = 1;
                else if (args[i] == "-O2") optLevel = 2;
                else if (args[i] == "-f" && i + 1 < args.Length) format = args[++i].ToLower();
                else if (args[i] == "-arch" && i + 1 < args.Length) arch = args[++i].ToLower();
                else if (args[i] == "--lang" && i + 1 < args.Length) lang = args[++i];
                else if (args[i] == "-a" && i + 1 < args.Length) baseAddr = Convert.ToInt32(args[++i], 16);
                else if (args[i] == "--bios") biosMode = "on";
                else if (args[i] == "--no-bios") biosMode = "off";
                else if (args[i] == "--rom-base" && i + 1 < args.Length) { romBase = Convert.ToUInt32(args[++i], 16); if (baseAddr == 0) baseAddr = (int)romBase; }
                else if (args[i] == "--ram-base" && i + 1 < args.Length) ramBase = Convert.ToUInt32(args[++i], 16);
                else if (args[i] == "--ram-size" && i + 1 < args.Length) ramSize = Convert.ToUInt32(args[++i], 16);
                else if (args[i] == "--chip" && i + 1 < args.Length) chip = args[++i].ToLower();
                else if (args[i] == "-m" && i + 1 < args.Length) x86Mode = args[++i];
                else if (args[i] == "--gas") gasMode = true;
            }

            var ext = Path.GetExtension(inputFile).ToLowerInvariant();
            byte[] data;
            int outAddr = baseAddr;

            // Auto-detect arch from .s file extension
            if (string.IsNullOrEmpty(arch))
            {
                foreach (var a in AssemblerFactory.SupportedArchitectures)
                    if (inputFile.Contains($".{a}.") || inputFile.EndsWith($".{a}.s"))
                    { arch = a; break; }
            }

            if (!string.IsNullOrEmpty(arch) && (ext == ".vml" || ext == ".vmb" || ext != ".s"))
            {
                // PIPELINE: Source/VML → translate → assemble → output
                VmlProgram? program;

                if (ext == ".vmb")
                    program = VmlProgram.LoadFromVmbFile(inputFile);
                else if (ext == ".vml")
                {
                    var source = File.ReadAllText(inputFile);
                    // 自动链接 VML-level SYSCALL helpers (OutputInt/Hex/InputString)
                    if (biosMode != "off")
                    {
                        var vmlHelpers = FindVmlHelpers();
                        if (vmlHelpers != null)
                        {
                            Console.WriteLine($"VML SYSCALL helpers: {vmlHelpers}");
                            source = File.ReadAllText(vmlHelpers) + "\n" + source;
                        }
                    }
                    program = new VMLAssembler.VmlAssembler().Assemble(source);
                }
                else
                {
                    // 内置编译器 — 无需 Plugins 目录
                    program = CompileSource(inputFile, lang);
                    if (program == null) { Console.Error.WriteLine("找不到编译器"); return; }
                }

                // 优化 VML IR
                if (optLevel > 0)
                {
                    var before = program.Instructions.Count;
                    var options = new OptimizationOptions
                    {
                        OptimizationLevel = optLevel,
                        EnableDeadCodeElimination = true,
                        EnableConstantFolding = optLevel >= 1,
                        EnableCopyPropagation = optLevel >= 2,
                        EnableDeadStoreElimination = optLevel >= 2,
                        EnableJumpChaining = true,
                        EnableNopElimination = true,
                        EnablePeepholeOptimization = optLevel >= 1,
                        EnableLoopOptimization = optLevel >= 2,
                        EnableDataFlowAnalysis = optLevel >= 2,
                    };
                    program = OptimizationPipeline.CreateDefault().Run(program, options);
                    var after = program.Instructions.Count;
                    if (before != after)
                        Console.WriteLine($"优化: {before} → {after} 条指令 (-{before - after})");
                }

                Console.WriteLine($"翻译: VML -> {arch.ToUpper()} 汇编{(arch == "x86" ? $" ({x86Mode}-bit)" : "")}");
                var translator = TranslatorFactory.GetTranslator(arch, program, x86Mode);
                translator.RomBase = romBase;
                translator.RamBase = ramBase;
                translator.RamSize = ramSize;
                translator.SkipHeader = (biosMode != "off");  // BIOS 提供向量表时跳过翻译器头部
                var result = translator.Translate();

                // 自动链接 BIOS (architecture-specific SYSCALL handlers)
                var code = result.Code;
                if (biosMode != "off")
                {
                    var biosFile = FindBiosFile(arch, chip);
                    if (biosFile != null)
                    {
                        Console.WriteLine($"BIOS: {biosFile}");
                        var biosCode = File.ReadAllText(biosFile);
                        code = biosCode + "\n" + code;
                    }
                    else if (biosMode == "on")
                    {
                        Console.Error.WriteLine($"警告: 未找到 {arch} 的 BIOS 文件");
                    }
                }

                // 汇编源码输出 (不汇编, 直接保存 .s)
                if (format == "asm" || format == "asm-only")
                {
                    if (format == "asm-only") code = result.Code; // 不含BIOS, 纯VML翻译
                    if (gasMode) code = ConvertToGas(code, arch);
                    // Split multi-directive lines: ".word 0 .word 0" → ".word 0\n.word 0"
                    var splitLines = new List<string>();
                    foreach (var l in code.Split('\n'))
                    {
                        var t = l.Trim();
                        if ((t.StartsWith(".word") || t.StartsWith(".hword") || t.StartsWith(".byte") || t.StartsWith(".short"))
                            && t.IndexOf(t.Substring(0, 5), 5, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            var parts = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var merged = new List<string>();
                            var indent = l.StartsWith("\t") ? "\t" : "";
                            foreach (var p in parts)
                            {
                                if (p == ".word" || p == ".hword" || p == ".byte" || p == ".short")
                                { if (merged.Count > 0) { splitLines.Add(indent + string.Join(" ", merged)); merged.Clear(); } }
                                merged.Add(p);
                            }
                            if (merged.Count > 0) splitLines.Add(indent + string.Join(" ", merged));
                        }
                        else splitLines.Add(l);
                    }
                    code = string.Join("\n", splitLines);
                    File.WriteAllText(outputFile, code);
                    Console.WriteLine($"ASM: {code.Split('\n').Length} 行 -> {outputFile}");
                    return;
                }

                Console.WriteLine($"汇编: {arch.ToUpper()} -> {format.ToUpper()}");
                var asm = AssemblerFactory.Create(arch);
                data = asm.Assemble(code, out var asmAddr, out _);
                // 保持用户指定的 outAddr (--rom-base / -a), 不被 assembler 覆盖
                if (outAddr == 0) outAddr = (int)romBase;
            }
            else if (ext == ".s" || arch != "")
            {
                // Direct .s file assembly
                if (string.IsNullOrEmpty(arch)) { Console.Error.WriteLine("请用 -arch 指定架构"); return; }
                var asmCode = File.ReadAllText(inputFile);
                var asm = AssemblerFactory.Create(arch);
                Console.WriteLine($"汇编: {arch.ToUpper()} ({asm.Description}) -> {format.ToUpper()}");
                data = asm.Assemble(asmCode, out outAddr, out _);
                if (baseAddr != 0) outAddr = baseAddr;
            }
            else
            {
                // Plain VML/VMB → HEX (VMB binary output)
                VmlProgram? program;
                if (ext == ".vmb") program = VmlProgram.LoadFromVmbFile(inputFile);
                else { var src = File.ReadAllText(inputFile); program = new VMLAssembler.VmlAssembler().Assemble(src); }
                data = program.ToVmbBytes();
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                outputFile = format switch
                {
                    "hex" => Path.ChangeExtension(inputFile, ".hex"),
                    "bin" => Path.ChangeExtension(inputFile, ".bin"),
                    "elf" => Path.ChangeExtension(inputFile, ".elf"),
                    "exe" => Path.ChangeExtension(inputFile, ".exe"),
                    "com" => Path.ChangeExtension(inputFile, ".com"),
                    "s19" or "srec" => Path.ChangeExtension(inputFile, ".s19"),
                    "vmb" => Path.ChangeExtension(inputFile, ".vmb"),
                    "vmapp" => Path.ChangeExtension(inputFile, ".sh"),
                    "jvm" or "java" => Path.ChangeExtension(inputFile, ".class"),
                    "dotnet" or "net" => Path.ChangeExtension(inputFile, ".exe"),
                    _ => Path.ChangeExtension(inputFile, ".bin"),
                };
            }

            OutputFormat.Write(format, data, outAddr, outputFile);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static bool TryKeilAssemble(string gnuAsm, uint romBase, string format, string outputFile, out byte[] data)
    {
        data = null!;
        var keilBin = Environment.GetEnvironmentVariable("KEIL_ARMCC_BIN");
        if (string.IsNullOrEmpty(keilBin))
        {
            var keilHome = Environment.GetEnvironmentVariable("KEIL_HOME");
            if (!string.IsNullOrEmpty(keilHome))
                keilBin = Path.Combine(keilHome, "ARM", "ARMCC", "bin");
            else
                keilBin = @"C:\Keil_v5\ARM\ARMCC\bin";
        }
        var armasm = Path.Combine(keilBin, "armasm.exe");
        if (!File.Exists(armasm)) return false;
        var tmp = Path.GetTempPath();
        var sf = Path.Combine(tmp, "vml.s"); var of = Path.Combine(tmp, "vml.o"); var ef = Path.Combine(tmp, "vml.elf");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("        PRESERVE8"); sb.AppendLine("        THUMB");
        foreach (var raw in gnuAsm.Split('\n'))
        {
            var l = raw.TrimEnd('\r').TrimStart();
            if (string.IsNullOrEmpty(l) || l.StartsWith(".syntax") || l.StartsWith(".thumb") || l.StartsWith(".align") || l.StartsWith(".section")) continue;
            if (l.StartsWith("@") && l.Contains("Vector")) { sb.AppendLine("        AREA RESET,DATA,READONLY"); sb.AppendLine("        EXPORT __Vectors"); sb.AppendLine("__Vectors"); continue; }
            if (l.StartsWith("@") && l.Contains("Exception")) { sb.AppendLine("        AREA |.text|,CODE,READONLY"); continue; }
            var c = l.Replace(".word","DCD").Replace(".set","EQU").Replace(".global","EXPORT").Replace("@",";");
            if (!string.IsNullOrEmpty(c)) sb.AppendLine(c);
        }
        sb.AppendLine("        END");
        File.WriteAllText(sf, sb.ToString());
        RunProc(armasm, $"--cpu=Cortex-M4 --thumb \"{sf}\" -o \"{of}\"");
        RunProc(Path.Combine(keilBin,"armlink.exe"), $"--ro-base=0x{romBase:X} --first=__Vectors \"{of}\" -o \"{ef}\"");
        string fopt = format=="hex" ? $"--i32combined --output=\"{outputFile}\" \"{ef}\"" : $"--bin --output=\"{outputFile}\" \"{ef}\"";
        RunProc(Path.Combine(keilBin,"fromelf.exe"), fopt);
        Console.WriteLine($"Keil armasm: {new FileInfo(outputFile).Length} 字节 -> {outputFile}");
        data = File.ReadAllBytes(outputFile);
        return true;
    }
    static void RunProc(string exe, string args) { var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, args){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false})!; p.WaitForExit(); }

    /// <summary>将 NASM 汇编转换为 GAS (GCC) 兼容格式</summary>
    static string ConvertToGas(string nasm, string arch)
    {
        // Pre-process: join multi-line strings (NASM allows string continuations)
        var lines = new List<string>();
        string? pending = null;
        foreach (var raw in nasm.Split('\n'))
        {
            var t = raw.TrimEnd('\r');
            if (pending != null)
            {
                pending += t.Trim();
                if (t.Contains("'")) { lines.Add(pending); pending = null; }
                continue;
            }
            var trimmed = t.Trim();
            if ((trimmed.Contains(" db ") || trimmed.StartsWith("db ")) && trimmed.Contains("'") && !trimmed.EndsWith("'"))
            {
                var idx = trimmed.IndexOf('\'');
                var rest = trimmed.Substring(idx + 1);
                if (!rest.Contains("'")) { pending = t; continue; }
            }
            lines.Add(t);
        }
        if (pending != null) lines.Add(pending);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(".intel_syntax noprefix");
        sb.AppendLine();

        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith(";")) { sb.AppendLine("# " + t.Substring(1).TrimStart()); continue; }
            if (t.StartsWith("bits ") || t.StartsWith("org ")) continue;
            if (t.StartsWith("global ")) { sb.AppendLine(t.Replace("global ", ".globl ")); continue; }
            // section handled below
            if (t.StartsWith("extern ")) { sb.AppendLine(".extern " + t.Substring(7).Trim()); continue; }

            // Data directives: convert NASM string to GAS .asciz
            if ((t.Contains(" db ") || t.StartsWith("db ")) && t.Contains("'") && !t.Contains("\""))
            {
                var idx = t.IndexOf('\'');
                var end = t.LastIndexOf('\'');
                if (end > idx)
                {
                    var label = t.Contains(":") ? t.Substring(0, t.IndexOf(':') + 1) : "";
                    var str = t.Substring(idx + 1, end - idx - 1);
                    str = str.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r");
                    var suffix = (t.Contains(", 0") || t.Contains(",0")) ? ".asciz" : ".string";
                    sb.AppendLine($"{label} {suffix} \"{EscapeAsmStr(str)}\"");
                    continue;
                }
            }
            // Generic db/dw/dd → .byte/.word/.long
            if (t.Contains(" db ") || t.StartsWith("db ")) { sb.AppendLine(RegexReplace(t, @"\bdb\b", ".byte")); continue; }
            if (t.Contains(" dw ") || t.StartsWith("dw ")) { sb.AppendLine(RegexReplace(t, @"\bdw\b", ".word")); continue; }
            if (t.Contains(" dd ") || t.StartsWith("dd ")) { sb.AppendLine(RegexReplace(t, @"\bdd\b", ".long")); continue; }

            // Fix section → .text/.data (GAS uses .text not .section .text)
            if (t == "section .text") { sb.AppendLine(".text"); continue; }
            if (t == "section .data") { sb.AppendLine(".data"); continue; }

            // Labels
            if (t.EndsWith(":") && !t.Contains(" ")) { sb.AppendLine(t); continue; }

            // Instructions
            sb.AppendLine("\t" + t);
        }
        return sb.ToString();
    }

    static string EscapeAsmStr(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");
    }

    static string RegexReplace(string input, string pattern, string replacement)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement);
    }

    static string? FindBiosFile(string arch, string? chip = null)
    {
        // 优先使用 VML_HOME 环境变量
        var root = Environment.GetEnvironmentVariable("VML_HOME")
            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH")
            ?? AppContext.BaseDirectory;
        for (int i = 0; i < 6; i++)
        {
            var probe = Path.Combine(root, "Lib", "vml", "Bios");
            if (Directory.Exists(probe))
            {
                var biosName = (arch.ToLower()) switch
                {
                    "avr" => "avr_bios.s",
                    "8051" => "mcs51_bios.asm",
                    "arm-cm" or "arm" => chip switch
                    {
                        "stm32f103" or "f103" => "arm_cm_f103_bios.s",
                        "stm32f429" or "f429" => "arm_cm_f429_bios.s",
                        _ => "arm_cm_bios.s"  // default F429
                    },
                    "pic" => "pic_bios.s",
                    "pic24" => "pic24_bios.s",
                    "msp430" => "msp430_bios.s",
                    "6502" => "mcs6502_bios.s",
                    "z80" => "z80_bios.s",
                    "x86" or "8086" => "x86_bios.s",
                    "riscv" or "risc-v" => "riscv_bios.s",
                    "mips" => "mips_bios.s",
                    "68000" or "m68k" => "m68k_bios.s",
                    "powerpc" or "ppc" => "ppc_bios.s",
                    "sparc" => "sparc_bios.s",
                    _ => null
                };
                if (biosName != null)
                {
                    var biosFile = Path.Combine(probe, biosName);
                    if (File.Exists(biosFile)) return biosFile;
                }
                return null;
            }
            var parent = Directory.GetParent(root);
            if (parent == null) break;
            root = parent.FullName;
        }
        return null;
    }

    static string? FindVmlHelpers()
    {
        var root = Environment.GetEnvironmentVariable("VML_HOME")
            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH")
            ?? AppContext.BaseDirectory;
        for (int i = 0; i < 6; i++)
        {
            var probe = Path.Combine(root, "Lib", "vml", "Bios", "syscall_helpers.vml");
            if (File.Exists(probe)) return probe;
            var parent = Directory.GetParent(root);
            if (parent == null) break;
            root = parent.FullName;
        }
        return null;
    }

    static void Help()
    {
        bool en = VMLPlugins.Localization.CurrentLang != "zh-CN" && VMLPlugins.Localization.CurrentLang != "zh-TW";
        Console.WriteLine(en
            ? "VML To Hex — Source/VML/ASM → HEX/BIN/ELF/EXE/COM/S19"
            : "VML 汇编转烧录 — 源/VML/汇编 → HEX/BIN/ELF/EXE/COM/S19");
        Console.WriteLine();
        if (en) Console.WriteLine("Pipeline:");
        else    Console.WriteLine("完整工作流:");
        Console.WriteLine(en
            ? "  vml2hex main.c -arch 6502               # Source→compile→translate→assemble→HEX"
            : "  vml2hex main.c -arch 6502                 # 源码→编译→翻译→汇编→HEX");
        Console.WriteLine(en
            ? "  vml2hex main.vml -arch z80              # VML→translate→assemble→HEX"
            : "  vml2hex main.vml -arch z80                # VML→翻译→汇编→HEX");
        Console.WriteLine(en
            ? "  vml2hex main.vml -arch avr              # VML→AVR→HEX (with BIOS)"
            : "  vml2hex main.vml -arch avr                # VML→AVR→HEX（含BIOS）");
        Console.WriteLine(en
            ? "  vml2hex main.vml -arch 8051 --no-bios   # VML→8051 (no BIOS)"
            : "  vml2hex main.vml -arch 8051 --no-bios     # VML→8051（不含BIOS）");
        Console.WriteLine();
        if (en) Console.WriteLine("Usage:");
        else    Console.WriteLine("用法:");
        Console.WriteLine(en
            ? "  vml2hex <file> -arch <arch> [-f fmt] [-o out] [-a addr] [--bios/--no-bios]"
            : "  vml2hex <file> -arch <arch> [-f fmt] [-o out] [-a addr] [--bios/--no-bios]");
        Console.WriteLine();
        if (en) Console.WriteLine("Architectures (-arch):");
        else    Console.WriteLine("架构 (-arch):");
        var asms = AssemblerFactory.SupportedArchitectures.ToList();
        foreach (var a in asms) Console.WriteLine($"  {a,-10} {AssemblerFactory.Create(a).Description}");
        Console.WriteLine();
        Console.WriteLine(en
            ? "  x86 mode (-m):  16 (real/COM) or 32 (protected, default)"
            : "  x86模式 (-m):  16 (实模式/COM) 或 32 (保护模式, 默认)");
        Console.WriteLine();
        Console.WriteLine(en
            ? "Format (-f):   hex(default) bin elf exe com s19 dump jvm/net"
            : "输出格式 (-f):  hex(默认) bin elf exe com s19 dump jvm/net");
        Console.WriteLine(en
            ? "Optimization:  -O0=none -O1=basic(default) -O2=aggressive"
            : "优化等级 (-O):  -O0=不优化 -O1=基本(默认) -O2=激进");
        Console.WriteLine(en
            ? "BIOS:          --bios(default) --no-bios"
            : "BIOS选项:       --bios(默认自动) --no-bios(跳过BIOS)");
        Console.WriteLine(en
            ? "Output types:  HEX(programmer) S19(Moto) BIN(raw) EXE(DOS) COM(CP/M)"
            : "烧录器支持:    HEX(通用烧录) S19(Moto) BIN(裸片) EXE(DOS) COM(CP/M)");
    }

    /// <summary>
    /// 根据文件扩展名或语言名直接调用内置编译器
    /// </summary>
    static VmlProgram? CompileSource(string file, string lang)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        var language = !string.IsNullOrEmpty(lang) ? lang.ToLower() : ext.TrimStart('.');

        Console.WriteLine($"编译: {file} -> VML");

        // 使用编译器插件的 CompileFile 方法
        IFrontendCompiler? compiler = language switch
        {
            "c" or "h" => new CCompiler.CCompilerPlugin(),
            "cpp" or "cc" or "cxx" or "hpp" => new CppCompiler.CppCompilerPlugin(),
            "bas" or "basic" => new BasicCompiler.BasicCompilerPlugin(),
            "js" or "javascript" => new JavaScriptCompiler.JavaScriptCompilerPlugin(),
            "py" or "python" => new PythonCompiler.PythonCompilerPlugin(),
            "lua" => new LuaCompiler.LuaCompilerPlugin(),
            "pas" or "pascal" => new PascalCompiler.PascalCompilerPlugin(),
            "fth" or "forth" => new ForthCompiler.ForthCompilerPlugin(),
            "go" => new GoCompiler.GoCompilerPlugin(),
            "rs" or "rust" => new RustCompiler.RustCompilerPlugin(),
            "ld" or "ladder" => new LadderCompiler.LadderCompilerPlugin(),
            "cs" or "csharp" => new CSharpCompiler.CSharpCompilerPlugin(),
            "java" => new JavaCompiler.JavaCompilerPlugin(),
            "swift" => new SwiftCompiler.SwiftCompilerPlugin(),
            "kt" or "kotlin" or "kts" => new KotlinCompiler.KotlinCompilerPlugin(),
            "scm" or "scheme" or "ss" => new SchemeCompiler.SchemePlugin(),
            _ => null
        };

        if (compiler == null) return null;
        return compiler.CompileFile(file);
    }
}
