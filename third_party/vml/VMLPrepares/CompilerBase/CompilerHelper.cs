using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VMLAssembler;
using VMLPlugins;

namespace CompilerBase
{
    /// <summary>
    /// 编译器公共辅助方法
    /// </summary>
    public static class CompilerHelper
    {
        /// <summary>当前正在编译的源文件名（用于 CompileWithDiagnostics 生成准确的错误位置）。CompileFileStandard 自动设置。</summary>
        [ThreadStatic]
        public static string? CurrentSourceFile;

        /// <summary>
        /// 根据目标语言，将 -D/-U 宏定义注入为语言对应的常量声明。
        /// 返回: 注入后的源代码（C/C++ 使用 #define，其他语言使用对应语法）。
        /// </summary>
        /// <param name="source">原始源代码</param>
        /// <param name="language">语言名称（c, cpp, python, lua, go, rust, pascal, basic, forth, java, csharp, javascript, swift, ladder）</param>
        /// <param name="opts">编译器选项（含 Defines/Undefines 列表）</param>
        public static string InjectDefines(string source, string language, CompilerOptions opts)
        {
            var sb = new StringBuilder();
            bool isC = language is "c" or "cpp";

            // 自动注入 VML_WSTRING 宏: OS 模式 → 默认字符串为 wstring (UTF-16LE)
            // MCU 模式或 opts 为 null → 不定义该宏 → 默认 UTF-8
            if (opts != null && !opts.IsMCU)
            {
                if (isC)
                    sb.AppendLine("#define VML_WSTRING 1");
                else
                    sb.AppendLine(FormatDefine(language, "VML_WSTRING", "1"));
            }

            if (opts == null || (opts.Defines.Count == 0 && opts.Undefines.Count == 0))
            {
                if (sb.Length > 0) { sb.Append(source); return sb.ToString(); }
                return source;
            }

            foreach (var d in opts.Defines)
            {
                var eq = d.IndexOf('=');
                string name = eq >= 0 ? d[..eq] : d;
                string val = eq >= 0 ? d[(eq + 1)..] : "1";

                if (isC)
                {
                    // C/C++: 保留 #define 语法（由预处理器处理）
                    sb.AppendLine($"#define {name} {val}");
                }
                else
                {
                    sb.AppendLine(FormatDefine(language, name, val));
                }
            }
            foreach (var u in opts.Undefines)
            {
                if (isC)
                    sb.AppendLine($"#undef {u}");
                else
                    sb.AppendLine(FormatUndef(language, u));
            }

            sb.Append(source);
            return sb.ToString();
        }

        private static string FormatDefine(string lang, string name, string val)
        {
            bool isInt = int.TryParse(val, out _);
            return lang switch
            {
                "python" => $"{(isInt ? $"{name} = {val}" : $"{name} = '{val}'")}",
                "lua" => $"{name} = {(isInt ? val : $"'{val}'")}",
                "go" => $"const {name} = {val}",
                "rust" => $"const {name}: i32 = {val};",
                "pascal" => $"const {name} = {val};",
                "basic" => $"CONST {name} = {val}",
                "forth" => $"{val} CONSTANT {name}",
                "java" => $"public static final int {name} = {val};",
                "csharp" => $"const int {name} = {val};",
                "javascript" => $"var {name} = {val};",
                "swift" => $"let {name} = {(isInt ? val : $"\"{val}\"")}",
                "ladder" => $"// #define {name} {val}",
                "ruby" => $"{(isInt ? $"{name} = {val}" : $"{name} = '{val}'")}",
                "dart" => $"const {name} = {val};",
                "objc" => $"#define {name} {val}",
                "r" => $"{name} <- {val}",
                "d" => $"enum {name} = {val};",
                "fortran" => $"integer, parameter :: {name} = {val}",
                _ => $"// #define {name} {val}"
            };
        }

        private static string FormatUndef(string lang, string name)
        {
            return lang switch
            {
                "python" => $"# {name} = None  # undef",
                "lua" => $"-- {name} = nil  -- undef",
                "go" => $"// const {name} = ...  // undef",
                "rust" => $"// const {name}: i32 = ...;  // undef",
                _ => $"// #undef {name}"
            };
        }

        /// <summary>
        /// 编译文件并生成包含 .linked  伪指令的 VML 文本（通用模板）。
        /// 各静态编译器的 CompileFileWithIncludes 均调用此方法，避免重复代码。
        /// </summary>
        /// <param name="mainProgram">已编译（未链接库）的主程序</param>
        /// <param name="filePath">源文件路径（用于计算 basePath）</param>
        /// <param name="libraryPaths">用户指定的额外库路径</param>
        /// <param name="autoLinkStdLib">是否自动添加标准库 .linked  引用</param>
        /// <param name="useSharedLibrary">是否包含共享库 .linked  引用</param>
        /// <param name="langName">语言名称（如 "c"、"python"、"go"），用于定位标准库</param>
        public static string BuildCompileFileWithIncludes(
            VmlProgram mainProgram,
            string filePath,
            List<string> libraryPaths,
            bool autoLinkStdLib,
            bool useSharedLibrary,
            string langName)
        {
            var includeFiles = new List<string>();

            if (autoLinkStdLib)
            {
                var stdIncludes = IncludeProcessor.GetStandardLibraryIncludes(langName, useSharedLibrary);
                includeFiles.AddRange(stdIncludes);
            }

            if (libraryPaths != null && libraryPaths.Count > 0)
            {
                var userIncludes = IncludeProcessor.ConvertLibraryPathsToIncludes(libraryPaths);
                includeFiles.AddRange(userIncludes);
            }

            string basePath = Path.GetDirectoryName(Path.GetFullPath(filePath));
            return mainProgram.ToVmlTextWithIncludes(includeFiles, basePath);
        }

        /// <summary>
        /// 编译超时保护（默认30秒），防止编译器中死循环导致的永久阻塞。
        /// 所有编译器的 Compile 入口均使用此方法包裹。
        /// </summary>
        public static VmlProgram CompileWithTimeout(Func<VmlProgram> compileFn, string compilerName, int timeoutSeconds = 30)
        {
            var task = Task.Run(compileFn);
            if (task.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
                return task.Result;
            Console.Error.WriteLine($"[{compilerName}] 错误: 编译超时 ({timeoutSeconds}秒)");
            return null;
        }

        /// <summary>
        /// 链接语言的标准库到编译后的 VmlProgram
        /// 在 CompileFile 中 autoLinkStdLib=true 时调用
        /// 自动链接 builtin.vml（必备运行时）
        /// stdlib.vml / vmllib.vml 需用户显式指定
        /// </summary>
        /// <param name="program">编译后的主程序</param>
        /// <param name="langDir">语言目录名（如 "go"、"python"、"csharp"）</param>
        /// <param name="libraryPaths">用户额外指定的库路径</param>
        /// 自动检测程序中的 CALL 标签, 匹配共享库 (public: C编译器等可直接调用)
        // BareCNameMap 已删除 — 共享库函数使用裸名，不再需要前缀映射

        public static void AutoDetectSharedLibs(VmlProgram program, List<string> allPaths, string? langDir = null)
        {
            if (program?.Instructions == null) return;

            var neededLibs = new HashSet<string>();
            foreach (var inst in program.Instructions)
            {
                if (inst.Opcode != VMLAssembler.OpCode.CALL) continue;
                var labelOp = inst.Operands?.FirstOrDefault(o => o.Type == VMLAssembler.OperandType.LABEL);
                var label = labelOp?.Value?.ToString();
                if (string.IsNullOrEmpty(label)) continue;

                // 跳过用户自定义函数（已在 program.Labels 中定义）
                if (program.Labels.ContainsKey(label)) continue;

                // 1. .export 外置映射优先 — 库文件中声明的公开API
                string matchLabel = label;
                if (program.Exports.TryGetValue(label, out string? exportInternal))
                {
                    // .export 负责创建别名，用内部名称匹配 SharedPrefixMap
                    matchLabel = exportInternal;
                }
                // 2. 自动规范化: camelCase / arrow-notation → snake_case (仅用于匹配)
                else
                {
                    var canonical = FunctionNameNormalizer.Normalize(label);
                    if (canonical != null && canonical != label)
                    {
                        matchLabel = canonical;
                    }
                }

                // 3. 匹配已知共享库前缀（不break，允许多库链接）
                // 先尝试原始 matchLabel，再尝试剥离已知前缀
                var matchCandidates = new List<string> { matchLabel };
                // 剥离语言前缀 (如 c_int_to_str → int_to_str)
                if (langDir != null)
                {
                    string langPrefix = langDir + "_";
                    if (matchLabel.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase)
                        && matchLabel.Length > langPrefix.Length)
                        matchCandidates.Add(matchLabel[langPrefix.Length..]);
                }
                // 剥离常见内部前缀 (word_, method_, var_, func_)
                foreach (string knownPrefix in new[] { "word_", "method_", "var_", "func_" })
                {
                    if (matchLabel.StartsWith(knownPrefix, StringComparison.OrdinalIgnoreCase)
                        && matchLabel.Length > knownPrefix.Length)
                        matchCandidates.Add(matchLabel[knownPrefix.Length..]);
                }
                // 对于 Forth/BASIC/Pascal 等大写前缀包（WORD_INT_TO_STR → int_to_str），
                // 也尝试在第一个 `_` 之后截取
                int firstUnderscore = matchLabel.IndexOf('_');
                if (firstUnderscore > 0 && firstUnderscore < matchLabel.Length - 1)
                {
                    string afterFirstUnderscore = matchLabel[(firstUnderscore + 1)..];
                    if (!matchCandidates.Contains(afterFirstUnderscore))
                        matchCandidates.Add(afterFirstUnderscore);
                }
                // v1.66.65: 对每个候选名也尝试 FunctionNameNormalizer 规范化
                // 例如: func_strToInt → strToInt → (Normalize) → str_to_int
                //       这样 camelCase/PascalCase/arrow-notation 都能匹配 snake_case 的 SharedPrefixMap 键
                for (int i = matchCandidates.Count - 1; i >= 0; i--)
                {
                    var normalized = FunctionNameNormalizer.Normalize(matchCandidates[i]);
                    if (normalized != null && normalized != matchCandidates[i]
                        && !matchCandidates.Contains(normalized))
                        matchCandidates.Add(normalized);
                }

                foreach (var candidate in matchCandidates)
                {
                    foreach (var kvp in SharedPrefixMap)
                    {
                        if (candidate.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            neededLibs.Add(kvp.Value);
                        }
                    }
                }
            }

            // 自动链接检测到的共享库 (v1.66.63: 先搜语言目录，再 fallback shared)
            foreach (var lib in neededLibs)
            {
                string libFile = lib + ".vml";
                // 跳过已链接的库
                if (allPaths.Any(p => p.EndsWith(libFile, StringComparison.OrdinalIgnoreCase)))
                    continue;
                // v1.66.63: 共享实现体优先于语言包装器（确保包装器的 CALL 解析到实现体）
                // 两个都需要链接（包装器只做转发，实现体在共享库）
                string? sharedPath = FindLibPath("shared", libFile);
                if (sharedPath != null && !allPaths.Contains(sharedPath))
                    allPaths.Add(sharedPath);
                string? langPath = null;
                if (langDir != null)
                    langPath = FindLibPath(langDir, libFile);
                if (langPath != null && !allPaths.Contains(langPath))
                    allPaths.Add(langPath);
            }
        }

        /// <summary>扫描库文件中的 CALL 指令，发现传递依赖（如 stdio_funcs→shared_puts→io.vml）</summary>
        private static void ScanLibCalls(string libPath, List<string> allPaths)
        {
            try
            {
                if (!File.Exists(libPath)) return;
                var lines = File.ReadAllLines(libPath);
                foreach (var line in lines)
                {
                    if (!line.TrimStart().StartsWith("CALL", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int callIdx = line.IndexOf("CALL", StringComparison.OrdinalIgnoreCase) + 5;
                    var label = line.Substring(callIdx).Trim();
                    if (string.IsNullOrEmpty(label) || label.StartsWith("lib_")) continue;
                    // 匹配共享库
                    foreach (var kvp in SharedPrefixMap)
                        if (label.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            string libFile = kvp.Value + ".vml";
                            string? path = FindLibPath("shared", libFile);
                            if (path != null && !allPaths.Contains(path))
                                allPaths.Add(path);
                        }
                }
            }
            catch { /* 文件读取失败则跳过 */ }
        }

        public static void LinkStandardLibrary(VmlProgram program, string langDir, List<string>? libraryPaths = null)
        {
            var allPaths = new List<string>();
            if (libraryPaths != null)
                allPaths.AddRange(libraryPaths);

            // v1.66.63: 先自动检测共享库（实现体），确保共享库在语言包装器之前链接
            // 这样语言包装器中的 CALL (如 builtins.itoa→itoa) 正确解析到共享实现体
            int prevCount;
            do
            {
                prevCount = allPaths.Count;
                AutoDetectSharedLibs(program, allPaths, langDir);
                for (int i = prevCount; i < allPaths.Count; i++)
                    ScanLibCalls(allPaths[i], allPaths);
            } while (allPaths.Count > prevCount);

            // 语言特定的包装器库（在共享实现体之后链接，避免自引用）
            string builtinPath = FindLibPath(langDir, "builtin.vml");
            if (builtinPath != null && !allPaths.Contains(builtinPath))
                allPaths.Add(builtinPath);

            string builtinsPath = FindLibPath(langDir, "builtins.vml");
            if (builtinsPath != null && !allPaths.Contains(builtinsPath))
                allPaths.Add(builtinsPath);

            string consolePath = FindLibPath(langDir, "console.vml");
            if (consolePath != null && !allPaths.Contains(consolePath))
                allPaths.Add(consolePath);

            // 重新扫描所有库（包括新增的语言包装器），发现传递依赖
            do
            {
                prevCount = allPaths.Count;
                AutoDetectSharedLibs(program, allPaths, langDir);
                for (int i = prevCount; i < allPaths.Count; i++)
                    ScanLibCalls(allPaths[i], allPaths);
            } while (allPaths.Count > prevCount);

            // BASIC: 自动链接 basiclib.vml（C 实现的内置函数库）
            if (langDir.Equals("basic", StringComparison.OrdinalIgnoreCase))
            {
                string basiclibPath = FindLibPath("shared", "basiclib.vml");
                if (basiclibPath != null && !allPaths.Contains(basiclibPath))
                    allPaths.Add(basiclibPath);
            }

            // 语言特定的必需库 (AutoDetectSharedLibs 已处理共享库)
            if (langDir.Equals("lua", StringComparison.OrdinalIgnoreCase))
            {
                string metaPath = FindLibPath("lua", "lua_meta.vml");
                if (metaPath != null && !allPaths.Contains(metaPath))
                    allPaths.Add(metaPath);
            }
            if (langDir.Equals("scheme", StringComparison.OrdinalIgnoreCase))
            {
                string rtPath = FindLibPath("scheme", "scheme_rt.vml");
                if (rtPath != null && !allPaths.Contains(rtPath))
                    allPaths.Add(rtPath);
            }

            if (allPaths.Count > 0)
                VMLAssembler.LibraryLinker.LinkLibraries(program, allPaths);

            // 应用 .export 映射 (库文件中声明的公开API别名)
            program.ApplyExports();
        }

        private static string? FindLibPath(string langDir, string filename)
        {
            // 1. VML_HOME 环境变量
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            if (!string.IsNullOrEmpty(vmlHome))
            {
                var p = Path.Combine(vmlHome, "Lib", langDir, filename);
                if (File.Exists(p)) return p;
            }

            // 2. CWD + AppDomain
            string[] candidates = {
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", langDir, filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", langDir, filename),
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                    return c;
            }
            // 3. Walk up from BaseDirectory
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var probe = Path.Combine(dir, "Lib", langDir, filename);
                if (File.Exists(probe)) return probe;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static string? FindStdLibDir(string langDir)
        {
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            string[] candidates = {
                !string.IsNullOrEmpty(vmlHome) ? Path.Combine(vmlHome, "Lib", langDir) : null!,
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", langDir),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", langDir),
            };
            foreach (var c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && Directory.Exists(c) && Directory.GetFiles(c, "*.vml").Length > 0)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// 在搜索路径中解析库文件（import 自动链接 / 指令语法共用）
        /// 搜索顺序：源文件目录 → Lib/&lt;lang&gt;/ → Lib/shared/ → Lib/c/ → Lib/dynamic/ → CWD → BaseDir
        /// </summary>
        /// <param name="libName">库名（如 "network" 或 "network.vml"）</param>
        /// <param name="sourceDir">源文件所在目录</param>
        /// <param name="langDir">语言子目录名（如 "python"、"go"、"forth"）</param>
        /// <returns>完整路径，未找到返回 null</returns>
        // 已知共享库函数前缀 → .vml 文件映射
        private static readonly Dictionary<string, string> SharedPrefixMap = new()
        {
            // string.c — 新旧名称都支持
            ["str"] = "string", ["strlen"] = "string", ["strcmp"] = "string",
            ["strncmp"] = "string", ["strcpy"] = "string", ["strcat"] = "string",
            // math.c
            ["sin"] = "math", ["cos"] = "math", ["tan"] = "math",
            ["ipow"] = "math", ["isqrt"] = "math", ["ilog2"] = "math",
            ["abs"] = "math", ["min"] = "math", ["max"] = "math",
            ["sqrt"] = "math", ["pow"] = "math",
            ["random"] = "math", ["sign"] = "math", ["clamp"] = "math",
            // array.c — 新旧名称都支持
            ["arr_"] = "array", ["sort_bubble"] = "array", ["indexof"] = "array",
            // convert.c
            ["itoa"] = "convert", ["atoi"] = "convert",
            ["ftoa"] = "convert",
            // conv.c — 全类型转换 (int_to_str/float_to_str/bool_to_str 等)
            ["int_to_str"] = "conv", ["str_to_int"] = "conv",
            ["float_to_str"] = "conv", ["str_to_float"] = "conv",
            ["double_to_str"] = "conv", ["str_to_double"] = "conv",
            ["long_to_str"] = "conv", ["str_to_long"] = "conv",
            ["bool_to_str"] = "conv", ["str_to_bool"] = "conv",
            ["uint_to_str"] = "conv", ["str_to_uint"] = "conv",
            ["ulong_to_str"] = "conv", ["str_to_ulong"] = "conv",
            ["byte_to_str"] = "conv", ["str_to_byte"] = "conv",
            ["sbyte_to_str"] = "conv", ["short_to_str"] = "conv",
            ["ushort_to_str"] = "conv", ["char_to_str"] = "conv",
            ["str_to_char"] = "conv",
            // conv.c — camelCase 别名 (Kotlin/Java/Dart/Swift/Go/Rust 等生成)
            ["intToStr"] = "conv", ["strToInt"] = "conv",
            ["floatToStr"] = "conv", ["strToFloat"] = "conv",
            ["doubleToStr"] = "conv", ["strToDouble"] = "conv",
            ["longToStr"] = "conv", ["strToLong"] = "conv",
            ["boolToStr"] = "conv", ["strToBool"] = "conv",
            ["uintToStr"] = "conv", ["strToUint"] = "conv",
            ["ulongToStr"] = "conv", ["strToUlong"] = "conv",
            ["byteToStr"] = "conv", ["strToByte"] = "conv",
            ["sbyteToStr"] = "conv", ["shortToStr"] = "conv",
            ["ushortToStr"] = "conv", ["charToStr"] = "conv",
            ["strToChar"] = "conv",
            // conv.c — Scheme/Racket 风格 (int->string/bool->string 等)
            ["int->string"] = "conv", ["string->int"] = "conv",
            ["float->string"] = "conv", ["string->float"] = "conv",
            ["double->string"] = "conv", ["string->double"] = "conv",
            ["bool->string"] = "conv", ["string->bool"] = "conv",
            ["long->string"] = "conv", ["string->long"] = "conv",
            // 宽字符 / Unicode 版本
            ["int_to_wstr"] = "conv", ["wstr_to_int"] = "conv",
            ["int_to_ustr"] = "conv", ["ustr_to_int"] = "conv",
            ["long_to_wstr"] = "conv", ["wstr_to_long"] = "conv",
            ["long_to_ustr"] = "conv", ["ustr_to_long"] = "conv",
            ["float_to_wstr"] = "conv", ["wstr_to_float"] = "conv",
            ["double_to_wstr"] = "conv", ["wstr_to_double"] = "conv",
            // convert64.c — 64位转换 (ltoa/dtoa/atol/atod)
            ["ltoa"] = "convert64", ["dtoa"] = "convert64",
            ["atol"] = "convert64", ["atod"] = "convert64",
            // C stdio 裸名映射 (puts/getchar/putchar → io, printf系列 → printf)
            ["puts"] = "io", ["getchar"] = "io", ["putchar"] = "io",
            ["printf"] = "printf",
            ["sprintf"] = "printf", ["snprintf"] = "printf", ["vsnprintf"] = "printf",
            ["fprintf"] = "printf", ["scanf"] = "printf", ["sscanf"] = "util",
            // builtins.c + shared.vml (peek/poke等基础函数在shared.vml)
            // v1.66.58: vml_ 前缀已移除，改用直接函数名映射
            ["print_str"] = "builtins", ["print_int"] = "builtins", ["print_hex"] = "builtins",
            ["println_str"] = "builtins", ["println_int"] = "builtins",
            ["print_float"] = "builtins", ["print_bool"] = "builtins",
            ["newline"] = "builtins",
            ["sleep"] = "builtins", ["alloc"] = "builtins", ["malloc"] = "builtins", ["free"] = "builtins",
            ["get_tick"] = "builtins", ["debug_print"] = "builtins", ["assert"] = "builtins",
            ["is_power_of_two"] = "builtins", ["next_power_of_two"] = "builtins",
            // io64.vml — 64位/长整数/双精度 I/O
            ["print_long"] = "io64", ["print_hex_long"] = "io64",
            ["println_long"] = "io64", ["println_hex_long"] = "io64",
            ["input_long"] = "io64", ["print_double"] = "io64", ["println_double"] = "io64",
            // console.vml — 控制台 I/O (gets, input_float等)
            ["gets"] = "console", ["print_str_no_nl_impl"] = "console", ["input_float"] = "console",
            // ⚠ **C# 发的是 PascalCase 标签** —— `CSharpCompiler/CodeGenerator.cs` 的
            //   `memberLabels` 把 `Console.Write/WriteLine` 编成 `PrintlnStr` / `PrintStr` /
            //   `PrintlnInt` / `PrintInt`，注释里写着「标签定义在 Lib/csharp/console.vml」。
            //   但**映射表里原本一条都没有** ⇒ 自动链接无从得知该链 `console` ⇒
            //   程序里只有调用点、没有定义，报「未找到标签: PrintlnStr」
            //   （实测 `out.cs` 链的 51 个模块里**没有任何 console 模块**）。
            //   这四条补上，`console` 才会被链进来。
            ["PrintlnStr"] = "console", ["PrintlnInt"] = "console",
            ["PrintStr"] = "console", ["PrintInt"] = "console",
            // printx.vml — 格式化输出 (printx_int32, printlnx_string等)
            ["printx_"] = "printx", ["printlnx_"] = "printx",
            // sysinfo.vml — 系统信息
            ["get_date"] = "sysinfo", ["get_time"] = "sysinfo", ["exit"] = "sysinfo",
            ["vml_"] = "builtins", // 向后兼容旧编译器生成的 vml_ 前缀
            ["peek"] = "shared", ["poke"] = "shared",
            ["peekb"] = "shared", ["pokeb"] = "shared",
            ["peekh"] = "shared", ["pokeh"] = "shared",
            ["peekl"] = "shared", ["pokel"] = "shared",
            ["peekf"] = "shared", ["pokef"] = "shared",
            ["peekd"] = "shared", ["poked"] = "shared",
            // shared_ 前缀别名 (Go, Java, Dart等编译器生成)
            // bitlib.c
            ["bit_"] = "bitlib",
            // base64.c
            ["base64_"] = "base64",
            // rle.c
            ["rle_"] = "rle",
            // fsm.c
            ["fsm_"] = "fsm", ["event_queue"] = "fsm",
            // crc.c
            ["crc"] = "crc", ["djb2"] = "crc", ["sdbm"] = "crc", ["fnv1a"] = "crc",
            ["checksum"] = "crc",
            // complex.c
            ["cadd"] = "complex", ["csub"] = "complex", ["cmul"] = "complex", ["cdiv"] = "complex",
            ["complex_"] = "complex", ["cconj"] = "complex", ["cneg"] = "complex",
            ["cexp_"] = "complex", ["csqr"] = "complex", ["csqrt"] = "complex",
            // matrix.c
            ["mat2_"] = "matrix", ["mat3_"] = "matrix", ["mat4_"] = "matrix",
            ["vec2_"] = "matrix", ["vec3_"] = "matrix",
            // statistics.c
            ["linreg_"] = "statistics", ["sum_arr"] = "statistics",
            ["min_arr"] = "statistics", ["max_arr"] = "statistics",
            // fixed.c
            ["fixed_"] = "fixed",
            // ringbuf.c
            ["ringbuf_"] = "ringbuf",
            // pid.c
            ["pid_"] = "pid",
            // signal.c
            ["moving_avg"] = "signal", ["ema_"] = "signal", ["kalman_"] = "signal",
            ["lerp_table"] = "signal", ["deadband"] = "signal", ["hysteresis"] = "signal",
            // button.c
            ["button_"] = "button",
            // swtimer.c
            ["swtimer_"] = "swtimer",
            // scheduler.c
            ["sched_"] = "scheduler",
            // cli.c
            ["cli_"] = "cli",
            // color.c
            ["rgb"] = "color", ["grayscale"] = "color", ["color_"] = "color",
            ["hsv_"] = "color",
            // builtins_kotlin.c (Kotlin 语言独享内置函数)
            ["kotlin_"] = "builtins_kotlin",
            // v1.66.56: 按需链接库 (从 builtins.vml/builtin.vml 中移出, AutoDetectSharedLibs 自动检测)
            // ctype.vml — 字符分类
            ["isalpha"] = "ctype", ["isdigit"] = "ctype", ["isalnum"] = "ctype",
            ["isupper"] = "ctype", ["islower"] = "ctype", ["isspace"] = "ctype",
            ["toupper"] = "ctype", ["tolower"] = "ctype", ["isprint"] = "ctype",
            ["isxdigit"] = "ctype", ["iscntrl"] = "ctype", ["isgraph"] = "ctype",
            // bitops.vml — 位操作
            ["bit_set"] = "bitops", ["bit_clear"] = "bitops", ["bit_toggle"] = "bitops",
            ["bit_test"] = "bitops", ["ror"] = "bitops", ["rol"] = "bitops",
            ["popcount"] = "bitops", ["clz"] = "bitops", ["ctz"] = "bitops",
            // file.vml — 文件 I/O
            ["fopen"] = "file", ["fclose"] = "file", ["fread"] = "file",
            ["fwrite"] = "file", ["fseek"] = "file", ["ftell"] = "file",
            ["feof"] = "file", ["fgets"] = "file", ["fputs"] = "file",
            // time.vml — 时间函数
            // ⚠ `sleep` / `get_tick` 的**唯一实现**在 `shared/src/builtins.c`
            //   （`SYSCALL #52` / `#53`），`time` 模块里根本没有这两个函数。
            //   这里原先又写了一遍映射到 `time` —— C# 集合初始化器**后写覆盖先写**，
            //   于是实际生效的是这条错的，编译器会去链一个不含它们的模块。
            //   同一个键两处映射、其中一处还是错的，就是「改了一处没生效」的温床，已删。
            ["delay"] = "time",
            ["time_"] = "time", ["clock"] = "time",
            // encoding.vml — 编码
            ["url_encode"] = "encoding", ["url_decode"] = "encoding",
            ["utf8_"] = "encoding", ["encoding_"] = "encoding",
            // network.vml — 网络
            ["socket"] = "network", ["bind"] = "network", ["listen"] = "network",
            ["accept"] = "network", ["connect"] = "network", ["send"] = "network",
            ["recv"] = "network",
            // os.vml — 操作系统
            ["getenv"] = "os", ["exec"] = "os",
        };

        public static string? ResolveImportLibrary(string libName, string sourceDir, string langDir)
        {
            // 自动映射: 根据函数名前缀推断对应的共享库
            if (!libName.EndsWith(".vml", StringComparison.OrdinalIgnoreCase)
                && !libName.Contains('/') && !libName.Contains('\\'))
            {
                foreach (var kvp in SharedPrefixMap)
                {
                    if (libName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        libName = kvp.Value + ".vml";
                        break;
                    }
                }
            }

            // 生成所有候选文件名 (支持点号分隔的包名如 a.b.c.d)
            var candidateNames = new List<string>();

            if (libName.Contains('.'))
            {
                // a.b.c.d → [a.b.c.d, d, a/b/c/d]
                candidateNames.Add(libName);                           // a.b.c.d
                candidateNames.Add(libName[(libName.LastIndexOf('.') + 1)..]); // d (最后一段)
                candidateNames.Add(libName.Replace('.', Path.DirectorySeparatorChar)); // a\b\c\d (OS路径)
                // 也尝试 / 分隔符 (跨平台)
                char otherSep = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
                candidateNames.Add(libName.Replace('.', otherSep));    // 另一种分隔符
            }
            else
            {
                candidateNames.Add(libName);
            }

            // 去重并展开 _lib.vml 变体
            var searchNames = new List<string>();
            foreach (var name in candidateNames.Distinct())
            {
                string nameNoExt = name;
                if (nameNoExt.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
                    nameNoExt = nameNoExt[..^4];

                if (!name.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains('/') && !name.Contains('\\'))
                {
                    searchNames.Add(name + ".vml");
                }
                else if (!name.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
                {
                    // 路径形式: 也尝试加 .vml
                    searchNames.Add(name + ".vml");
                }
                else
                {
                    searchNames.Add(name);
                }
                searchNames.Add(nameNoExt + "_lib.vml");
            }

            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            var searchDirs = new List<string>
            {
                sourceDir,
                Path.Combine(sourceDir, "..", "..", "Lib", langDir),
                Path.Combine(sourceDir, "..", "..", "Lib", "shared"),
                Path.Combine(sourceDir, "..", "..", "Lib", "c"),
                Path.Combine(sourceDir, "Lib", langDir),
                Path.Combine(sourceDir, "Lib", "shared"),
                Path.Combine(sourceDir, "Lib", "c"),
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", langDir),
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", "shared"),
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", "c"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", langDir),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", "shared"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", "c"),
            };
            if (!string.IsNullOrEmpty(vmlHome))
            {
                searchDirs.Add(Path.Combine(vmlHome, "Lib", langDir));
                searchDirs.Add(Path.Combine(vmlHome, "Lib", "shared"));
                searchDirs.Add(Path.Combine(vmlHome, "Lib", "c"));
            }

            foreach (var dir in searchDirs)
            {
                foreach (var candidate in searchNames)
                {
                    string fullPath = Path.Combine(dir, candidate);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
            return null;
        }

        /// <summary>
        /// 创建基础预定义宏字典 (__VML__, __VML_VERSION__, __DATE__, __TIME__).
        /// 每个编译器在此基础上添加自己的语言标识宏。
        /// </summary>
        public static Dictionary<string, string> BasePredefinedMacros()
        {
            return new Dictionary<string, string>
            {
                ["__VML__"] = "1",
                ["__VML_VERSION__"] = "\"1.65.170\"",
                ["__DATE__"] = $"\"{DateTime.Now:MMM dd yyyy}\"",
                ["__TIME__"] = $"\"{DateTime.Now:HH:mm:ss}\"",
            };
        }

        /// <summary>
        /// 创建预定义宏字典 = BasePredefinedMacros() + 语言特有宏。
        /// 替代各编译器中重复的 PredefinedMacros 初始化模式。
        /// </summary>
        public static Dictionary<string, string> CreatePredefinedMacros(string langMacro)
        {
            var dict = new Dictionary<string, string>(BasePredefinedMacros());
            dict[$"__{langMacro}__"] = "1";
            return dict;
        }

        /// <summary>
        /// 统一预处理流程：InjectDefines → PreprocessorProcess。
        /// 替代各 Compile() 方法中重复的 4 行样板代码。
        /// </summary>
        public static string PreprocessSource(string source, Dictionary<string, string> macros, List<string>? includePaths = null)
        {
            source = InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, includePaths, macros);
                source = pp.Process();
            }
            return source;
        }

        /// <summary>
        /// 读取源文件，自动检测 BOM 并解码为 UTF-8 字符串。
        /// 支持: UTF-8 (带/不带 BOM), UTF-16LE, UTF-16BE。
        /// 自动检测二进制文件（含 null 字节）并拒绝。
        /// </summary>
        public static string ReadSourceFile(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            if (bytes.Length == 0) return string.Empty;

            // 二进制文件检测: 前 8KB 中出现 null 字节视为二进制文件
            int scanLen = Math.Min(8192, bytes.Length);
            for (int i = 0; i < scanLen; i++)
            {
                if (bytes[i] == 0)
                    throw new ParseException(ErrorCode.Compilation_BinaryFile,
                        $"{filePath}: error: 文件似乎是二进制格式（包含 null 字节），无法编译");
            }

            // UTF-16LE BOM: FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            // UTF-16BE BOM: FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            // UTF-8 BOM: EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            // No BOM: treat as UTF-8 (backward compatible)
            return Encoding.UTF8.GetString(bytes);
        }

        /// 标准 CompileFile 流程: 读文件 → 预处理 → 编译 → 链接库.
        /// 消除 17 个编译器中重复的 ~25 行库链接逻辑。
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <param name="langName">语言目录名 (如 "d", "ruby", "python")</param>
        /// <param name="compileFn">编译函数 (已含预处理)</param>
        /// <param name="extractImports">导入提取函数 (语言特定的 regex)</param>
        /// <param name="predefinedMacros">预定义宏字典</param>
        /// <param name="includePaths">头文件搜索路径</param>
        /// <param name="libraryPaths">额外库路径</param>
        /// <param name="autoLinkStdLib">是否自动链接标准库</param>
        public static VmlProgram CompileFileStandard(
            string filePath,
            string langName,
            Func<string, VmlProgram> compileFn,
            Func<string, List<string>> extractImports,
            Dictionary<string, string> predefinedMacros,
            List<string>? includePaths = null,
            List<string>? libraryPaths = null,
            bool autoLinkStdLib = true)
        {
            string source = ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;

            source = InjectDefines(source, "c", CompilerOptionsContext.Current);
            Preprocessor? pp = null;
            if (source.Contains('#'))
            {
                pp = new Preprocessor(source, includePaths, predefinedMacros);
                source = pp.Process(filePath);
            }

            var importLibs = extractImports(source);
            var prevFile = CurrentSourceFile;
            CurrentSourceFile = filePath;
            var prog = compileFn(source);
            CurrentSourceFile = prevFile;

            List<string> allLibPaths = new();
            if (libraryPaths != null) allLibPaths.AddRange(libraryPaths);
            if (pp != null)
                foreach (var lib in pp.ParamLibraries)
                    if (!importLibs.Contains(lib)) importLibs.Add(lib);

            foreach (var lib in importLibs)
            {
                string? resolved = ResolveImportLibrary(lib, sourceDir, langName);
                if (resolved != null && !allLibPaths.Contains(resolved))
                    allLibPaths.Add(resolved);
            }

            // ⚠ 链接期「用户代码调用了不存在的函数」会抛 `UnresolvedSymbolException`。
            //   走这条路的语言（Pascal / Forth / Ladder / Basic 等 `BuildCompileFileWithIncludes`
            //   的用户）**不经过 `CompileWithDiagnostics` 的 catch**，于是它会以一个
            //   `Unhandled exception` 的形态直接穿到调用方 —— 消息是对的、但看上去像崩溃，
            //   而且不是"编译失败"那个既能被 CLI 识别、又能被 `VmlDiagnostics` 解析成
            //   编辑器气泡的形态。这里统一翻成 `CompilationException`。
            try
            {
                if (autoLinkStdLib)
                    LinkStandardLibrary(prog, langName, allLibPaths);
                else if (allLibPaths.Count > 0)
                    VMLAssembler.LibraryLinker.LinkLibraries(prog, allLibPaths);
            }
            catch (VMLAssembler.UnresolvedSymbolException ex)
            {
                throw new CompilationException(ErrorCode.CodeGen_UndefinedFunction, ex.Message, ex);
            }

            return prog;
        }

        /// <summary>
        /// 统一的 GCC 风格编译包装器。
        /// 创建 DiagnosticBag，调用 compile 委托，捕获异常并按 GCC 格式报告。
        /// 所有编译器共享此方法，避免重复的 try/catch 模式。
        /// </summary>
        /// <param name="fileName">源文件名 (null = "&lt;input&gt;")</param>
        /// <param name="compile">编译函数，接收 DiagnosticBag 用于收集词法/语法错误</param>
        /// <returns>编译成功的 VmlProgram</returns>
        public static VmlProgram CompileWithDiagnostics(string? fileName, Func<DiagnosticBag, VmlProgram> compile)
        {
            var diagnostics = new DiagnosticBag();
            var file = fileName ?? CurrentSourceFile ?? "<input>";
            try
            {
                var prog = compile(diagnostics);
                // ⚠ **成功路径也要看 bag**。此前这里直接 `return compile(...)`，
                //   于是"收集不抛"的那条路（`LexerBase.ReadStringLiteral`，被 Lua/Ruby/JS/ObjC/
                //   Dart/R/D 七门用着）**收集到的错误被整个丢掉、程序照编照跑** ——
                //   实测一个未终止的字符串会静默编译通过。这是"一次多报"的前置 bug：
                //   连"收集到的"都扔了，还谈什么多报。
                if (diagnostics.HasErrors)
                    throw new CompilationException(diagnostics.FirstErrorCode, diagnostics.FormatAll());
                return prog;
            }
            catch (ParseException ex)
            {
                // 把"抛出来的这一条"与"之前已经收集到的若干条"**并成一份**再抛 ——
                // 原来的写法是二选一（`HasErrors ? FormatAll() : 单条`），
                // 于是之前收集的那些在"有抛出"的情况下反而不见了。
                diagnostics.AddError(file, 0, 0, ex.Code, ex.Message);
                throw new CompilationException(ex.Code, diagnostics.FormatAll(), ex);
            }
            catch (CodeGenerationException ex)
            {
                diagnostics.AddError(file, 0, 0, ex.Code, ex.Message);
                throw new CompilationException(ex.Code, diagnostics.FormatAll(), ex);
            }
            catch (CompilationException) { throw; }
            // 同上（见 CompilerPluginBase 里那段说明）：链接期的"未定义函数"是用户源码的错，
            // 别落进下面的兜底被标成 internal error。一次会把所有没定义的名字都列出来。
            catch (VMLAssembler.UnresolvedSymbolException ex)
            {
                throw new CompilationException(ErrorCode.CodeGen_UndefinedFunction, ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new CompilationException(ErrorCode.Compilation_InternalError, $"{file}: internal error: {ex.Message}", ex);
            }
        }
    }
}
