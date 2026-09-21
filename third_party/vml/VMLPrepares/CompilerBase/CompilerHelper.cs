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
        /// 「预处理拼接后的行号」→「(原文件, 原行)」——这条规则的**唯一实现**
        /// （`LexerBase.MapOriginal` 与 `ParserBase.MapOriginal` 都只是转调它）。
        ///
        /// <para>
        /// `#include` 是把头文件内容**拼进同一个流**的，所以词法/语法/代码生成拿到的行号
        /// 是**拼接后**的。要报给用户"原文件第几行"，只有这一张表能换算
        /// （`Preprocessor.LineMap`，第 N 项 = 输出第 N 行）。
        /// </para>
        ///
        /// <para>
        /// 越界/没有映射 ⇒ 返回 <c>(null, 传入的行号)</c>：**宁可退回拼接行号，也不能报出别的
        /// 文件的行** —— 报错指错地方比报不出位置更糟（用户会去改一行根本没问题的代码）。
        /// </para>
        /// </summary>
        public static (string? File, int Line) MapOriginalLine(List<(string, int)>? lineMap, int processedLine)
            // 规则本体搬到 `VMLAssembler.SourceLineMapUtil` —— **链接器也要用它**
            // （「未定义的函数」是链接期才发现的错），而依赖方向是
            // `CompilerBase → VMLAssembler` 单向 ⇒ 实现必须在下层，上层转调。
            // 两处各写一份的话，规则一改必然只改一处。
            => VMLAssembler.SourceLineMapUtil.Map(lineMap, processedLine);

        /// <summary>`Preprocessor` 在主文件名未知时用的占位符（见 <see cref="MapOriginalLine"/>）。</summary>
        public const string UnknownFilePlaceholder = VMLAssembler.SourceLineMapUtil.UnknownFilePlaceholder;   // 唯一定义在 SourceLineMapUtil（链接器也要用）

        // ── 行号映射的「投递」────────────────────────────────────────────────
        //
        // 这条管道解决的是**用户报过的那个 bug**：`#include` 一展开，前端拿到的行号整体
        // 后移，于是「错在 `Lib/c/time.h` 第 30 行」被报成「用户文件第 112 行一句无害的注释」。
        //
        // `Preprocessor.Process()` 早就把映射表算好了（`LineMap`），21 门语言也早就把它
        // 拿到手了 —— 只是**全都扔掉了**（`source = pp.Process();` 之后就没人再提它）。
        // 逐门去接是 21 份手抄（本仓头号坑），所以改成**投递**：
        //   ① 生产者（`Preprocessor.Process()`）把「输出文本 + 映射表」一并挂出来；
        //   ② 消费者（词法器/解析器/代码生成器基类）**凭"这就是我手上这份源码"取回它**。
        //
        // 判据是**引用相等**（不是内容相等）：`Process()` 返回的那个字符串对象被各语言
        // 原样传给 `new Lexer(source)`，所以只要 `ReferenceEquals` 成立，这张表就一定
        // 对应这份源码；不成立就当作"没有映射"（**宁可退回拼接行号，也不能报出别的文件的行**）。
        // 这比"设一个全局变量、谁最后写谁赢"安全：一门语言这次没预处理，也不会捡到
        // 上一次编译留下的陈表（那正好会报出另一份文件的行号）。

        [ThreadStatic] private static string? _preprocessedOutput;
        [ThreadStatic] private static List<(string, int)>? _preprocessedLineMap;
        [ThreadStatic] private static List<(string, int)>? _activeLineMap;

        /// <summary>
        /// 生产者：`Preprocessor.Process()` 在返回前把结果登记在这里（**唯一调用点**）。
        /// </summary>
        public static void TrackPreprocessedOutput(string output, List<(string, int)>? lineMap)
        {
            _preprocessedOutput = output;
            _preprocessedLineMap = lineMap;
        }

        /// <summary>
        /// 「这份源码」对应的映射表；不是预处理产物就返回 null（见上面那段说明）。
        /// </summary>
        public static List<(string, int)>? LineMapForSource(string? source)
            => source != null && ReferenceEquals(source, _preprocessedOutput) ? _preprocessedLineMap : null;

        /// <summary>
        /// 当前这次编译「生效中的」映射表 —— 由**词法器构造时**认领
        /// （`LexerBase` 拿到源码就能判定它是不是预处理产物）。
        ///
        /// 解析器与代码生成器手里只有 token / AST，拿不到源码字符串，所以它们读这一份。
        /// 词法器每构造一次就**重写**一次（认不到匹配的表就写 null），
        /// 于是"上一次编译的陈表"进不来。
        /// </summary>
        public static List<(string, int)>? ActiveLineMap => _activeLineMap;

        /// <summary>词法器认领映射表（见 <see cref="ActiveLineMap"/>）。</summary>
        public static void SetActiveLineMap(List<(string, int)>? map) => _activeLineMap = map;

        /// <summary>
        /// 「预处理拼接后的行号」→「(原文件, 原行)」，用**生效中**的那张表
        /// （没有生效表时原样退回，与 <see cref="MapOriginalLine"/> 同语义）。
        /// </summary>
        public static (string? File, int Line) MapActiveOriginal(int processedLine)
            => MapOriginalLine(_activeLineMap, processedLine);

        /// <summary>
        /// 生效中的那张表**覆盖这一行吗**（= 这次映射到底可不可用）。
        ///
        /// <para>
        /// ⚠ 为什么需要单独一个判据、不能只看 <see cref="MapActiveOriginal"/> 的返回值：
        /// 那张表里的文件可能是占位符 `<unknown>`（内存里编的源码没有文件名），
        /// 而 <see cref="MapOriginalLine"/> 会把它**规范成 `null`**（对，那是刻意的 ——
        /// 让文件名退回调用方的默认值）。于是 `(null, 行)` 就有了两种含义：
        /// 「没有映射」与「有映射、但文件名未知」——**行号在后者里是对的，不能一起丢掉**。
        /// 实测踩过：只看 `file == null` 就退回拼接行号，于是「主文件第 5 行的错」
        /// 在有 `#include` 时又被报成拼接后的第 7 行。
        /// </para>
        /// </summary>
        public static bool ActiveMapCovers(int processedLine)
            => _activeLineMap != null && processedLine > 0 && processedLine <= _activeLineMap.Count;

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
            // ── 绘图 / 窗口那一套宿主接口 ────────────────────────────────────────
            // 实现在 `Lib/shared/vmlui.c` → `shared/vmlui.vml`（`ui_clear` / `ui_rect` /
            // `ui_win_open` / `ui_call_json_s` / `ui_timer_set` …）。
            //
            // ⚠ 此前这里**一条 `ui_` 都没有** ⇒ 任何**直接**引用它们的程序都链不到 vmlui：
            //   · C 侥幸躲过 —— `waycoder_ui.h` 里写着一句显式的 `.linked`/`#param`；
            //   · **Kotlin 躲不过**（`Examples/kotlin/sysinfo.kt` 直接调 `ui_call_json_s`），
            //     而 P2 之前这条只是"链接期警告"、运行时执行到才崩，所以谁都没发现；
            //     P2 把用户档的未解析升成**编译期硬错误**之后，这两个例子**当场编不过**。
            //   ⇒ 这就是 P2 的误报面，语料（out-probe）没覆盖到例子才漏掉的。
            ["ui_"] = "vmlui",
            // Lua 的表操作（`Lib/lua/luatable.vml`）—— 同上，映射表里先前一条都没有，
            // `Examples/lua/life.lua` 一编译就报 `lua_table_set`/`lua_table_get` 未定义。
            ["lua_table_"] = "luatable",
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
            // ⚠ **这张表在 vmlcli / MauiVml 这条链上是死代码**（2026-09-19 实测）。
            //
            // 实测方法：在 `AutoDetectSharedLibs` 入口插一句无条件 `Console.Error.WriteLine`，
            // 重新构建（已确认插桩字符串进了 `CompilerBase.dll`），再跑 C 和 Forth 各一个 ——
            // **一次都没打印**。即这个方法在这条链上根本没被调用。
            //
            // 真正决定「哪些库被链进来」的是**前端产出的 `.linked` 指令**：
            // 前端（GenLib 生成的 `Lib/<lang>/builtin.vml` / `builtins.vml`）写出 `.linked "x.vml"`，
            // 汇编器 `AssembleWithIncludes` 解析成路径，外层再 `LibraryLinker.LinkLibraries` 链上。
            // `vmlcli/Program.cs` 第 ④ 步那条注释说的就是这件事（"Pascal / Forth / Ladder / Basic
            // 不在自己的 CompileFileWithIncludes 里链接 —— 链接是在这里做的"）。
            //
            // ⇒ 想让某个模块对某门语言可用，要改的是 **`Lib/modules.json` 的 `Core` 标志
            //   （全局，会进所有 22 门的 builtin.vml，别轻易动）或那门语言自己的 `.linked` 清单**，
            //   **不是**往下面这张表里加条目（加了不生效 —— 本轮 Forth 的 `parserexp` 试过）。
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
            var dict = new Dictionary<string, string>
            {
                ["__VML__"] = "1",
                ["__VML_VERSION__"] = "\"1.65.170\"",
                ["__DATE__"] = $"\"{DateTime.Now:MMM dd yyyy}\"",
                ["__TIME__"] = $"\"{DateTime.Now:HH:mm:ss}\"",
            };
            ApplyEnvDefines(dict);
            return dict;
        }

        /// <summary>
        /// 把 `VMLTOOL_DEFINE` 环境变量里的宏定义并进字典。
        ///
        /// ## 为什么需要它（这是**一整类**缺口，不是某几个宏）
        ///
        /// autoconf 时代的老程序，宏常常是**构建系统喂进去的**：
        ///
        ///     gcc -DVERSION='"cmatrix 2.0 by abhishek"' -DHAVE_CONFIG_H ...
        ///
        /// 而我们是"把源文件直接丢给编译器"、**没有构建系统** ⇒ 这类宏全缺。
        /// 实测 `cmatrix`：`printf(VERSION, __TIME__, __DATE__)`（第 175 行），
        /// 缺了它整个程序编不过 —— 而**它不是库缺失**，补头文件补不出来
        /// （见 `docs/老程序兼容性.md` 第九节"新类别①"）。
        ///
        /// ## 格式
        ///
        /// 与 `VMLTOOL_INCLUDE` 同一族，但用**换行**分隔多条而**不是分号** ——
        /// 宏值本身常含分号（`VERSION="a; b"`），用分号当分隔符会歧义。
        ///
        ///     VMLTOOL_DEFINE=$'VERSION="cmatrix 2.0"\nPACKAGE="cmatrix"'
        ///
        /// 只有名字、没有 `=` 的条目按 C 命令行的惯例取 `1`（即 `-DFOO` 的语义）。
        ///
        /// ## 优先级
        ///
        /// 这里写进去的会被**显式传入的**宏覆盖 —— `BasePredefinedMacros()` 的
        /// 产物在各编译器里是当作 `extraDefines` 参数传下去的，而
        /// `Preprocessor` 是**最后**合并 `extraDefines`（见其构造函数），
        /// 所以"命令行 > 环境变量 > 内建默认"这个次序是自然成立的。
        /// </summary>
        public static void ApplyEnvDefines(Dictionary<string, string> dict)
        {
            var env = Environment.GetEnvironmentVariable("VMLTOOL_DEFINE");
            if (string.IsNullOrEmpty(env)) return;
            foreach (var raw in env.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var entry = raw.Trim();
                if (entry.Length == 0) continue;
                int eq = entry.IndexOf('=');
                if (eq < 0) dict[entry] = "1";
                else dict[entry.Substring(0, eq).Trim()] = entry.Substring(eq + 1).Trim();
            }
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
                //
                // ⚠ 位置用 `ex` 上**分开的**三个字段，正文用 `BareMessage` ——
                //   `AddError` 自己会拼 `文件:行:列: error: `，这里再喂一个拼好的
                //   `ex.Message` 就会拼成两层（实测：`<input>: error: <input>:4:18: error: …`，
                //   第一个 `error:` 前面**一个位置都没有**，宿主侧正则锚不到）。
                //   `ex.Line` 为 0（位置未知）时照旧如实报"无位置"。
                diagnostics.AddError(ex.File ?? file, ex.Line, ex.Column, ex.Code, ex.BareMessage);
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
                throw new CompilationException(ErrorCode.Compilation_InternalError, $"{file}: 内部错误: {ex.Message}", ex);
            }
        }
    }
}
