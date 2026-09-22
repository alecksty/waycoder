using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilerBase
{
    public partial class Preprocessor
    {
        /// <summary>
        /// 报一条「找不到头文件」的**警告** —— **全仓唯一的措辞与级别**。
        ///
        /// 22 门里 C 走自己一份预处理器（`CCompiler/Preprocessor.cs`，它是整份拷贝），
        /// 其余 21 门走本类 ⇒ 同一句 `#include &lt;Windows.h&gt;` 从前有**两种结局**：
        /// 写成 `.c` 是**硬失败**（抛 `CompilationException`，编译直接结束），
        /// 写成 `.cpp` 只是跳过 + 警告。判据其实是同一件事，两处实现必然漂移 ——
        /// 所以措辞与级别收到这里一处，两边都调它。
        ///
        /// <para>
        /// **为什么定成警告而不是错误**（这一条是量出来的，不是拍的）：
        /// VML 的真实链接来源是 `#param lib(...)` 与 auto-link，**不是头文件正文** ——
        /// `scripts/vml-out-probe/langs/nat.c` 在 `stdio.h` 解析不到的环境下照样编过、
        /// 并跑出逐字节正确的结果。反过来把这一条升成硬错误，实测**直接打挂同一批语料
        /// 里的 2/30**（`nat.c` 缺 `stdio.h`、`nat.typedef.cpp` 缺 `time.h`，
        /// 而这两个头文件**就在 `Lib/c/` 下**，只是那个环境下路径没解析到）
        /// ⇒ 硬失败会把「路径没找对」放大成「整个程序编不了」，而且用户没有任何绕过手段。
        /// 归因并没有因此变模糊：警告点名了是哪个头文件、哪一行，下游的
        /// 「未声明的变量/函数」依旧照报（`game17.cpp` 的实测输出就是这样）。
        /// </para>
        ///
        /// <para>
        /// 报文形态是 GCC 的（`原文件:原行: warning: …`）：
        /// ① 位置就是 `#include` 那一行（调用点的 `currentFile`/`currentLine` 还指着
        /// **包含方**文件，没有被拼接流污染，不需要行号映射）；
        /// ② 级别词必须是 `warning`（宿主侧 `VmlDiagnostics` 按它判级别、给气泡配色）；
        /// ③ 句尾的 `[Code]` 会被宿主摘进 `Diagnostic.Code`。
        /// ⚠ 报文里**不要出现 ASCII 的 `error:`**（`vml-diag-probe` 的 dyn-global 组
        /// 就是按这个子串判"编不过"的）。
        /// </para>
        /// </summary>
        public static void ReportMissingInclude(string file, int line, string includedFile)
        {
            Console.Error.WriteLine(
                $"{file}:{line}: warning: 找不到头文件 \"{includedFile}\" —— "
                + "它不在 include 搜索路径里（VML 标准库只有 Lib 下那些）；"
                + "该头文件里声明的东西后面会以「未声明的变量/函数」的形式报出来。"
                + " [Preprocessor_IncludeNotFound]");
        }

        /// <summary>
        /// 处理 #include 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        /// <param name="processedSource">处理后的源代码</param>
        private void ProcessInclude(string rest, StringBuilder processedSource)
        {
            string includedFile = ParseInclude(rest);
            // 提取 #include 行尾可能存在的尾随代码
            // 当源文件中使用字面量 \n（非真正换行符）时，如 #include "conv.h"\nint main(){...}
            // 预处理器会将整行作为一行处理，头文件名后的内容需要保留
            string trailing = GetTrailingAfterInclude(rest);
            if (!string.IsNullOrEmpty(includedFile))
            {
                if (includedFiles.Count > 500) // safety: max 500 includes
                    throw new CompilationException(ErrorCode.Preprocessor_MaxIncludeDepth, $"{currentFile}: error: #include 嵌套深度超过 500 [Preprocessor_MaxIncludeDepth]");
                string fullPath = ResolveIncludePath(includedFile, currentFile);
                if (!includedFiles.Contains(fullPath))
                {
                    if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #include \"{includedFile}\" → {fullPath}");
                    includedFiles.Add(fullPath);
                    if (File.Exists(fullPath))
                    {
                        IncludeDepth++;
                        if (DumpMode) Console.Error.WriteLine($"  -> include stack++ : {IncludeDepth}, file: {Path.GetFileName(fullPath)}");
                        string       includeContent      = File.ReadAllText(fullPath);
                        Preprocessor includePreprocessor = new Preprocessor(includeContent, includePaths);
                        includePreprocessor.IncludeDepth = this.IncludeDepth; // 继承父级深度
                        includePreprocessor.includedFiles.AddRange(this.includedFiles);
                        includePreprocessor.definitions = new Dictionary<string, string>(this.definitions);
                        includePreprocessor.functionMacros = new Dictionary<string, (List<string>, string)>(this.functionMacros);
                        includePreprocessor._macroNameSet = new HashSet<string>(this._macroNameSet);
                        string processedInclude = includePreprocessor.Process(fullPath);
                        if (DumpMode) Console.Error.WriteLine($"  <- include stack-- : {IncludeDepth}, file: {Path.GetFileName(fullPath)}");
                        IncludeDepth--;
                        processedSource.Append(processedInclude);
                        // 添加头文件的行号映射
                        lineMap.AddRange(includePreprocessor.LineMap);
                        // 同步宏定义，但不同步ifStack
                        // 包含文件中定义的宏应该对后续代码可见
                        foreach (var kvp in includePreprocessor.definitions)
                        {
                            if (!this.definitions.ContainsKey(kvp.Key))
                            {
                                this.definitions[kvp.Key] = kvp.Value;
                                this._macroNameSet.Add(kvp.Key);
                                if (!kvp.Key.Contains('_')) this._hasNonUnderscoreMacro = true;
                            }
                        }
                        foreach (var kvp in includePreprocessor.functionMacros)
                        {
                            if (!this.functionMacros.ContainsKey(kvp.Key))
                            {
                                this.functionMacros[kvp.Key] = kvp.Value;
                                this._macroNameSet.Add(kvp.Key);
                                if (!kvp.Key.Contains('_')) this._hasNonUnderscoreMacro = true;
                            }
                        }
                        // 同步 #param lib/path 指令
                        foreach (var lib in includePreprocessor.ParamLibraries)
                        {
                            if (!this.ParamLibraries.Contains(lib))
                                this.ParamLibraries.Add(lib);
                        }
                        foreach (var path in includePreprocessor.ParamPaths)
                        {
                            if (!this.ParamPaths.Contains(path))
                                this.ParamPaths.Add(path);
                        }
                        if (includePreprocessor.ParamPrefixes.Count > 0 && this.ParamPrefixes.Count == 0)
                        {
                            this.ParamPrefixes.AddRange(includePreprocessor.ParamPrefixes);
                            this.definitions["VML_PREFIX"] = string.Join(" ", includePreprocessor.ParamPrefixes);
                        }
                    }
                    else
                    {
                        // 找不到头文件 —— **照旧不阻止编译**（与 GCC/Clang 的"可选头文件"容忍度一致，
                        // 改它会让一堆靠可选头文件的程序编不过），但**必须说出来**。
                        //
                        // ⚠ 原先这里是「静默跳过」（只有 `PrepareLogMode` 才打一行日志），
                        //   而手机上 `Console.Error` 是一条**看不见的流**、`PrepareLogMode` 也不开
                        //   ⇒ 用户看到的是一串「未声明的变量 'XXX'」+ 提示"名字拼错了也会报这一条"，
                        //   而真正的原因（`#include &lt;Windows.h&gt;` 这个头文件这里根本没有）**一个字都没提**。
                        //   用户的判定标准是「报错必需准确」——**归因错了就等于报错不对**。
                        //
                        // 报文形态是 GCC 的（`原文件:原行: warning: …`）：
                        //   ① 位置就是 `#include` 那一行（`currentFile`/`currentLine` 在进这个函数时
                        //      还指着**包含方**文件，没有被拼接流污染，不需要行号映射）；
                        //   ② 级别词必须是 `warning`（宿主侧 `VmlDiagnostics` 按它判级别、给气泡配色）；
                        //   ③ 句尾的 `[Code]` 会被宿主摘进 `Diagnostic.Code`。
                        //   ⚠ 报文里**不要出现 ASCII 的 `error:`**（`vml-diag-probe` 的 dyn-global 组
                        //     就是按这个子串判"编不过"的）。
                        if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #include 跳过(未找到): {includedFile}");
                        // 措辞与级别收到 `ReportMissingInclude` 一处 —— C 那份预处理器
                        // 要报同一件事时**也调它**，不许各写一份（见那里的说明）。
                        ReportMissingInclude(currentFile, currentLine, includedFile);
                    }
                }
                else
                {
                    // 文件已经包含过，跳过
                    // Console.WriteLine($"跳过已包含的文件: {fullPath}");
                }
            }
            // 输出尾随内容（头文件名后的代码），确保 #include 同行的代码不被丢弃
            if (!string.IsNullOrWhiteSpace(trailing))
            {
                var tail = trailing.TrimStart();
                processedSource.Append(tail);
                processedSource.Append('\n');
                // 这一支同样**输出了一行就得记一条映射**（尾随内容里的**字面量 `\n`**
                // 已被 `GetTrailingAfterInclude` 换成了真换行 ⇒ 可能不止一行，按换行数补）。
                int tailLines = 1;
                foreach (char ch in tail) if (ch == '\n') tailLines++;
                for (int i = 0; i < tailLines; i++) lineMap.Add((currentFile, currentLine));
            }
        }

        /// <summary>
        /// 提取 #include 行中头文件名后的尾随内容
        /// 用于处理字面量 \n 分隔的同行代码，如 #include "conv.h"\nint main(){...}
        /// 将字面量 \n 转换为真正的换行符，使后续代码成为独立行
        /// </summary>
        private static string GetTrailingAfterInclude(string rest)
        {
            string trailing = "";
            if (rest.StartsWith("\""))
            {
                int endQuote = rest.IndexOf('"', 1);
                if (endQuote != -1)
                    trailing = rest.Substring(endQuote + 1);
            }
            else if (rest.StartsWith("<"))
            {
                int endBracket = rest.IndexOf('>');
                if (endBracket != -1)
                    trailing = rest.Substring(endBracket + 1);
            }
            // 将字面量 \n 转换为真正的换行符
            // 仅处理引号/尖括号外的内容，避免破坏头文件名字符串
            if (!string.IsNullOrEmpty(trailing))
                trailing = trailing.Replace("\\n", "\n");
            return trailing;
        }

        /// <summary>
        /// 解析 #include 指令，提取头文件名
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        /// <returns>头文件名</returns>
        private string ParseInclude(string rest)
        {
            // 处理双引号包含的头文件
            if (rest.StartsWith("\""))
            {
                int endQuote = rest.IndexOf('"', 1);
                if (endQuote != -1)
                {
                    return rest.Substring(1, endQuote - 1);
                }
            }
            // 处理尖括号包含的头文件
            else if (rest.StartsWith("<"))
            {
                int endBracket = rest.IndexOf('>');
                if (endBracket != -1)
                {
                    return rest.Substring(1, endBracket - 1);
                }
            }

            throw new CompilationException(ErrorCode.Preprocessor_InvalidInclude, $"{currentFile}:{currentLine}: error: #include 指令格式无效 [Preprocessor_InvalidInclude]");
        }

        /// <summary>
        /// 解析包含路径
        /// </summary>
        /// <param name="includePath">头文件路径</param>
        /// <param name="currentFilePath">当前文件路径</param>
        /// <returns>解析后的完整路径</returns>
        private string ResolveIncludePath(string includePath, string currentFilePath)
        {
            // 如果是绝对路径，直接返回
            if (Path.IsPathRooted(includePath))
            {
                return includePath;
            }

            // 如果有当前文件路径，尝试在当前文件目录下查找
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                string dir      = Path.GetDirectoryName(currentFilePath);
                string fullPath = Path.Combine(dir, includePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 尝试在当前工作目录下查找
            string cwdPath = Path.Combine(Directory.GetCurrentDirectory(), includePath);
            if (File.Exists(cwdPath))
            {
                return cwdPath;
            }

            // 尝试在用户指定的头文件搜索路径中查找
            foreach (string dir in includePaths)
            {
                string fullPath = Path.Combine(dir, includePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 尝试在标准头文件目录下查找
            var includeDirs = new List<string> { ".", "./include", "/usr/include" };

            // 自动搜索 Lib/c/ 标准库路径（优先 VML_HOME，再从程序集目录向上搜索）
            string baseDir = (Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH")
                ?? AppContext.BaseDirectory).TrimEnd('/', '\\');
            for (int i = 0; i <= 5; i++)
            {
                includeDirs.Add(Path.Combine(baseDir, "Lib", "c"));
                includeDirs.Add(Path.Combine(baseDir, "lib", "c"));
                baseDir = Path.GetDirectoryName(baseDir);
                if (baseDir == null) break;
            }

            foreach (string dir in includeDirs)
            {
                string fullPath = Path.Combine(dir, includePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 如果都找不到，返回原始路径
            return includePath;
        }

        /// <summary>
        /// 处理 #define 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessDefine(string rest)
        {
            string trimmed = rest.TrimStart();
            if (string.IsNullOrEmpty(trimmed)) return;

            // Detect function-style macro: name must be followed by '(' with no whitespace before it
            int parenIdx = trimmed.IndexOf('(');
            int firstWs = trimmed.IndexOfAny(new[] { ' ', '\t' }); // 空格或 Tab

            if (parenIdx > 0 && (firstWs < 0 || parenIdx < firstWs))
            {
                // Function-style macro: #define name(params) body
                string macroName = trimmed.Substring(0, parenIdx).Trim();
                int closeParen = FindMatchingParen(trimmed, parenIdx);
                if (closeParen < 0)
                    throw new CompilationException(ErrorCode.Preprocessor_MacroMissingParen, $"{currentFile}:{currentLine}: error: 函数式宏缺少右括号: {macroName} [Preprocessor_MacroMissingParen]");

                string paramsStr = trimmed.Substring(parenIdx + 1, closeParen - parenIdx - 1).Trim();
                string body = trimmed.Substring(closeParen + 1).Trim();

                // 去除宏体中的注释 (C99 翻译阶段3: 注释在宏展开前删除)
                body = StripComments(body);

                // Remove leading/trailing whitespace from body; allow empty body
                var paramList = new List<string>();
                if (!string.IsNullOrEmpty(paramsStr))
                {
                    foreach (var p in SplitMacroArgs(paramsStr))
                    {
                        string pt = p.Trim();
                        if (!string.IsNullOrWhiteSpace(pt))
                        {
                            // 可变参数宏: #define name(...) 或 #define name(a, ...)
                            if (pt == "...")
                                paramList.Add("__VA_ARGS__");
                            else
                                paramList.Add(pt);
                        }
                    }
                }
                functionMacros[macroName] = (paramList, body);
                _macroNameSet.Add(macroName);
                if (!macroName.Contains('_')) _hasNonUnderscoreMacro = true;
            }
            else
            {
                if (firstWs == -1)
                {
                    // 无参数宏 (#define NAME)
                    string macroName = trimmed;
                    definitions[macroName] = "";
                    _macroNameSet.Add(macroName);
                    if (!macroName.Contains('_')) _hasNonUnderscoreMacro = true;
                }
                else
                {
                    // 有值宏 (#define NAME value) — 支持空格和 Tab 分隔
                    string macroName  = trimmed.Substring(0, firstWs).Trim();
                    string macroValue = trimmed.Substring(firstWs).Trim();

                    // 去除宏体中的注释 (C99 翻译阶段3: 注释在宏展开前删除)
                    macroValue = StripComments(macroValue);

                    definitions[macroName] = macroValue;
                    _macroNameSet.Add(macroName);
                    if (!macroName.Contains('_')) _hasNonUnderscoreMacro = true;
                    if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #define {trimmed}");
                }
            }
        }

        /// <summary>
        /// 去除 C 风格注释 (// ... 和 /* ... */)，用于清理宏定义值
        /// </summary>
        /// <remarks>
        /// ⚠ **必须认得字符串与字符字面量** —— 原先那份说明（C99 翻译阶段 3：注释在宏展开前删除）
        /// 只说对了一半：**阶段 3 删注释之前，字面量已经被识别出来了**，所以 `"a//b"` 里的 `//`
        /// **不是注释**。不认字面量就会把 `#define M "a//b"` 截成 `"a`，宏展开后表现为
        /// 「未结束的字符串」—— 同一个根因还会顺带报出「未知字符：\」「未声明的变量」等
        /// 一串**长得完全不同的错**，这正是它长期没被认出来的原因。
        ///
        /// 发现于 2026-09-22 老程序兼容性体检：`sl`（mtoyoda/sl，295 行）整个编不过，
        /// 因为 `sl.h` 里 `#define LWHL22 "//// \\_/      \\_/    "` 正是这一形态。
        /// 最小复现：`#define M "a//b"` 再使用 M（**同一个字符串直接写进代码反而没事**）。
        /// </remarks>
        private string StripComments(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];

                // 字符串 / 字符字面量：整段照抄（含转义），里面的 // 与 /* 都不算注释
                if (c == '"' || c == '\'')
                {
                    char quote = c;
                    sb.Append(c);
                    i++;
                    while (i < s.Length)
                    {
                        if (s[i] == '\\' && i + 1 < s.Length)
                        {
                            sb.Append(s[i]).Append(s[i + 1]); // 转义：连下一个字符一起抄
                            i += 2;
                            continue;
                        }
                        sb.Append(s[i]);
                        bool closed = s[i] == quote;
                        i++;
                        if (closed) break; // 收尾引号
                    }
                    continue;
                }

                if (i + 1 < s.Length && c == '/' && s[i + 1] == '/')
                    break; // 行尾注释，丢弃剩余部分
                if (i + 1 < s.Length && c == '/' && s[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/'))
                        i++;
                    i += 2; // 跳过 */
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// 处理 #undef 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessUndef(string rest)
        {
            string macroName = rest.Trim();
            if (definitions.ContainsKey(macroName))
            {
                definitions.Remove(macroName);
                _macroNameSet.Remove(macroName);
            }
            if (functionMacros.ContainsKey(macroName))
            {
                functionMacros.Remove(macroName);
                _macroNameSet.Remove(macroName);
            }
        }

        /// <summary>
        /// 处理 #if 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessIf(string rest)
        {
            bool condition = EvaluateCondition(rest);
            int  depth     = ifStack.Count > 0 ? ifStack[^1].Item2 + 1 : 1;
            ifStack.Add((condition ? 1 : 0, depth)); if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #if {rest.Trim()} → {condition}");
        }

        /// <summary>
        /// 处理 #ifdef 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessIfdef(string rest)
        {
            string macroName = rest.Trim();
            bool   condition = definitions.ContainsKey(macroName);
            int    depth     = ifStack.Count > 0 ? ifStack[^1].Item2 + 1 : 1;
            ifStack.Add((condition ? 1 : 0, depth)); if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #if {rest.Trim()} → {condition}");
        }

        /// <summary>
        /// 处理 #ifndef 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessIfndef(string rest)
        {
            string macroName = rest.Trim();
            bool   condition = !definitions.ContainsKey(macroName);
            int    depth     = ifStack.Count > 0 ? ifStack[^1].Item2 + 1 : 1;
            ifStack.Add((condition ? 1 : 0, depth)); if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #if {rest.Trim()} → {condition}");
        }

        /// <summary>
        /// 处理 #else 指令
        /// </summary>
        private void ProcessElse()
        {
            if (ifStack.Count == 0)
            {
                throw new CompilationException(ErrorCode.Preprocessor_ElseWithoutIf, $"{currentFile}:{currentLine}: error: #else 指令没有对应的 #if、#ifdef 或 #ifndef 指令 [Preprocessor_ElseWithoutIf]");
            }

            var (isTrue, depth) = ifStack[^1];
            // isTrue == -1: 前面的条件已满足，保持跳过状态
            // isTrue == 0:  前面的条件为假，进入 #else 体（设为活跃）
            // isTrue == 1:  前面的条件为真，跳过 #else 体（设为不活跃）
            ifStack[^1] = (isTrue == 0 ? 1 : 0, depth);
        }

        /// <summary>
        /// 处理 #elif 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessElif(string rest)
        {
            if (ifStack.Count == 0)
            {
                throw new CompilationException(ErrorCode.Preprocessor_ElifWithoutIf, $"{currentFile}:{currentLine}: error: #elif 指令没有对应的 #if、#ifdef 或 #ifndef 指令 [Preprocessor_ElifWithoutIf]");
            }

            var (isTrue, depth) = ifStack[^1];
            if (isTrue == 0)
            {
                // 前面的 #if / #elif 为假，求值这个 #elif
                bool condition = EvaluateCondition(rest);
                ifStack[^1] = (condition ? 1 : 0, depth);
            }
            else
            {
                // 前面的 #if / #elif 已经为真，跳过这个 #elif 及其后续 #else
                // 使用 -1 标记为"已解决"，确保后续 #else 也被跳过
                ifStack[^1] = (-1, depth);
            }
        }

        /// <summary>
        /// 处理 #endif 指令
        /// </summary>
        private void ProcessEndif()
        {
            if (ifStack.Count == 0)
            {
                throw new CompilationException(ErrorCode.Preprocessor_EndifWithoutIf, $"{currentFile}:{currentLine}: error: #endif 指令没有对应的 #if、#ifdef 或 #ifndef 指令 [Preprocessor_EndifWithoutIf]");
            }

            ifStack.RemoveAt(ifStack.Count - 1);
        }

        /// <summary>
        /// 处理 #line 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessLine(string rest)
        {
            string[] parts = rest.Split(new[] { ' ' }, 2);
            if (parts.Length > 0 && int.TryParse(parts[0], out int lineNum))
            {
                currentLine = lineNum - 1; // 因为下一行会自增
                if (parts.Length > 1 && (parts[1].StartsWith("\"")))
                {
                    int endQuote = parts[1].IndexOf('"', 1);
                    if (endQuote != -1)
                    {
                        currentFile = parts[1].Substring(1, endQuote - 1);
                    }
                }
            }
        }

        /// <summary>
        /// 处理 #error 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessError(string rest)
        {
            throw new CompilationException(ErrorCode.Preprocessor_ErrorDirective, $"{currentFile}:{currentLine}: error: {rest} [Preprocessor_ErrorDirective]");
        }

        /// <summary>
        /// 处理 #warning 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessWarning(string rest)
        {
            System.Console.WriteLine($"警告: 在文件 {currentFile} 第 {currentLine} 行: {rest}");
        }

        /// <summary>
        /// 处理 #param 指令
        /// 语法: #param lib(name) 或 #param lib("path.vml") 或 #param path("dir")
        /// </summary>
        private void ProcessParam(string rest)
        {
            rest = rest.Trim();
            if (string.IsNullOrEmpty(rest)) return;

            // 匹配 func(arg) 形式
            int parenOpen = rest.IndexOf('(');
            int parenClose = rest.LastIndexOf(')');
            if (parenOpen < 0 || parenClose < 0 || parenClose <= parenOpen)
                throw new CompilationException(ErrorCode.Preprocessor_ParamSyntaxError, $"{currentFile}:{currentLine}: error: #param 语法错误: {rest} [Preprocessor_ParamSyntaxError]");

            string func = rest.Substring(0, parenOpen).Trim().ToLowerInvariant();
            string arg = rest.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

            switch (func)
            {
                case "lib":
                    // 支持逗号分隔的多个库名
                    var libs = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var lib in libs)
                    {
                        var libTrimmed = lib.Trim();
                        // 去掉可选的外层引号
                        if (libTrimmed.Length >= 2 &&
                            ((libTrimmed.StartsWith('"') && libTrimmed.EndsWith('"')) ||
                             (libTrimmed.StartsWith('\'') && libTrimmed.EndsWith('\''))))
                        {
                            libTrimmed = libTrimmed.Substring(1, libTrimmed.Length - 2);
                        }
                        // 无引号和扩展名时自动补 .vml
                        if (!libTrimmed.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) &&
                            !libTrimmed.Contains('/') && !libTrimmed.Contains('\\'))
                        {
                            libTrimmed += ".vml";
                        }
                        if (!ParamLibraries.Contains(libTrimmed))
                            ParamLibraries.Add(libTrimmed);
                    }
                    break;

                case "path":
                    if (arg.Length >= 2 &&
                        ((arg.StartsWith('"') && arg.EndsWith('"')) ||
                         (arg.StartsWith('\'') && arg.EndsWith('\''))))
                    {
                        arg = arg.Substring(1, arg.Length - 2);
                    }
                    if (!ParamPaths.Contains(arg))
                        ParamPaths.Add(arg);
                    break;

                case "prefix":
                    // 支持逗号分隔的多个前缀
                    var prefixes = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in prefixes)
                    {
                        var prefix = p.Trim();
                        if (prefix.Length >= 2 &&
                            ((prefix.StartsWith('"') && prefix.EndsWith('"')) ||
                             (prefix.StartsWith('\'') && prefix.EndsWith('\''))))
                        {
                            prefix = prefix.Substring(1, prefix.Length - 2);
                        }
                        if (!string.IsNullOrEmpty(prefix) && !ParamPrefixes.Contains(prefix))
                            ParamPrefixes.Add(prefix);
                    }
                    if (ParamPrefixes.Count > 0)
                        definitions["VML_PREFIX"] = string.Join(" ", ParamPrefixes);
                    break;

                default:
                    throw new CompilationException(ErrorCode.Preprocessor_UnknownParamFunction, $"{currentFile}:{currentLine}: error: 未知的 #param 函数: {func} [Preprocessor_UnknownParamFunction]");
            }
        }

        /// <summary>
        /// 评估条件表达式
        /// 支持: defined(MACRO), !expr, expr && expr, expr || expr, 整数常量, 宏名
        /// </summary>
    }
}
