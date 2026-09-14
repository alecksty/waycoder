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
                    throw new CompilationException(ErrorCode.Preprocessor_MaxIncludeDepth, $"{currentFile}: error: #include nesting depth exceeds 500 [Preprocessor_MaxIncludeDepth]");
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
                        // 静默跳过头文件（与 GCC/Clang 行为一致，缺失的可选头文件不阻止编译）
                        if (PrepareLogMode) Console.Error.WriteLine($"[预处理] #include 跳过(未找到): {includedFile}");
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
                processedSource.Append(trailing.TrimStart());
                processedSource.Append('\n');
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

            throw new CompilationException(ErrorCode.Preprocessor_InvalidInclude, $"{currentFile}:{currentLine}: error: invalid #include directive format [Preprocessor_InvalidInclude]");
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
                    throw new CompilationException(ErrorCode.Preprocessor_MacroMissingParen, $"{currentFile}:{currentLine}: error: function-like macro missing closing parenthesis: {macroName} [Preprocessor_MacroMissingParen]");

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
        private string StripComments(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                if (i + 1 < s.Length && s[i] == '/' && s[i + 1] == '/')
                    break; // 行尾注释，丢弃剩余部分
                if (i + 1 < s.Length && s[i] == '/' && s[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/'))
                        i++;
                    i += 2; // 跳过 */
                    continue;
                }
                sb.Append(s[i]);
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
                throw new CompilationException(ErrorCode.Preprocessor_ElseWithoutIf, $"{currentFile}:{currentLine}: error: #else without matching #if/#ifdef/#ifndef [Preprocessor_ElseWithoutIf]");
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
                throw new CompilationException(ErrorCode.Preprocessor_ElifWithoutIf, $"{currentFile}:{currentLine}: error: #elif without matching #if/#ifdef/#ifndef [Preprocessor_ElifWithoutIf]");
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
                throw new CompilationException(ErrorCode.Preprocessor_EndifWithoutIf, $"{currentFile}:{currentLine}: error: #endif without matching #if/#ifdef/#ifndef [Preprocessor_EndifWithoutIf]");
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
                throw new CompilationException(ErrorCode.Preprocessor_ParamSyntaxError, $"{currentFile}:{currentLine}: error: #param syntax error: {rest} [Preprocessor_ParamSyntaxError]");

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
                    throw new CompilationException(ErrorCode.Preprocessor_UnknownParamFunction, $"{currentFile}:{currentLine}: error: unknown #param function: {func} [Preprocessor_UnknownParamFunction]");
            }
        }

        /// <summary>
        /// 评估条件表达式
        /// 支持: defined(MACRO), !expr, expr && expr, expr || expr, 整数常量, 宏名
        /// </summary>
    }
}
