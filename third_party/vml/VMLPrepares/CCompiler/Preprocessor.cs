using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CompilerBase;

namespace CCompiler
{
    /// <summary>
    /// C 语言预处理器
    /// </summary>
    public class Preprocessor
    {
        private string                     source;
        private List<string>               includedFiles; // 用于防止循环包含
        private Dictionary<string, string> definitions;   // 用于存储宏定义
        private Dictionary<string, (List<string> Params, string Body)> functionMacros; // 函数式宏
        private List<(int, int)>           ifStack;       // 用于处理条件编译的栈
        private int                        currentLine;   // 当前行号
        private string                     currentFile;   // 当前文件名
        private List<(string, int)>        lineMap;       // 行号映射，记录处理后的每一行对应原始文件和行号
        private List<string>               includePaths;  // 头文件搜索路径
        /// <summary>通过 #param lib(...) 收集的库名/路径列表</summary>
        public List<string> ParamLibraries { get; } = new();
        /// <summary>通过 #param path(...) 收集的额外 include 路径</summary>
        public List<string> ParamPaths { get; } = new();
        /// <summary>通过 #param prefix(...) 设置的函数名前缀 (如 python_ → shared_print → python_print)</summary>
        public string? ParamPrefix { get; set; }

        // 编译时日期/时间 (整个编译过程共享)
        private static readonly string CompileDate = DateTime.Now.ToString("MMM dd yyyy", System.Globalization.CultureInfo.InvariantCulture);
        private static readonly string CompileTime = DateTime.Now.ToString("HH:mm:ss");

        /// <summary>
        /// 构造函数
        /// 初始化预处理器，设置源代码和头文件搜索路径
        /// </summary>
        /// <param name="source">源代码字符串</param>
        /// <param name="includePaths">头文件搜索路径列表</param>
        public Preprocessor(string source, List<string> includePaths = null, Dictionary<string, string>? extraDefines = null)
        {
            this.source       = source;
            includedFiles     = new List<string>();
            definitions       = new Dictionary<string, string>();
            functionMacros    = new Dictionary<string, (List<string> Params, string Body)>();
            ifStack           = new List<(int, int)>(); // (条件是否为真, 嵌套深度)
            currentLine       = 0;
            currentFile       = "<unknown>";
            lineMap           = new List<(string, int)>();
            this.includePaths = includePaths ?? new List<string>();
            // 从环境变量 VMLTOOL_INCLUDE 追加搜索路径
            var envInclude = Environment.GetEnvironmentVariable("VMLTOOL_INCLUDE");
            if (!string.IsNullOrEmpty(envInclude))
            {
                char sep = envInclude.Contains(';') ? ';' : ':';
                foreach (var p in envInclude.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (!this.includePaths.Contains(p))
                        this.includePaths.Add(p);
            }

            // 预定义宏
            var now = DateTime.Now;
            definitions["__STDC__"] = "1";
            definitions["__STDC_VERSION__"] = "199901L";
            definitions["__VML__"] = "1";
            definitions["__VML_VERSION__"] = "\"1.65.35\"";
            definitions["__DATE__"] = $"\"{now:MMM dd yyyy}\"";
            definitions["__TIME__"] = $"\"{now:HH:mm:ss}\"";
            // 合并额外预定义宏 (来自 CompilerBase.PredefinedMacros / -D 标志等)
            if (extraDefines != null)
                foreach (var kvp in extraDefines)
                    definitions[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// 行号映射，记录处理后的每一行对应原始文件和行号
        /// </summary>
        public List<(string, int)> LineMap => lineMap;

        /// <summary>
        /// 预处理源代码，处理各种预处理指令
        /// </summary>
        /// <param name="filePath">当前文件路径，用于解析相对路径的头文件</param>
        /// <returns>预处理后的源代码</returns>
        public string Process(string filePath = "")
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                currentFile = filePath;
            }

            StringBuilder processedSource = new StringBuilder();
            StringReader  reader          = new StringReader(source);
            string        line;
            currentLine = 0;

            while ((line = reader.ReadLine()) != null)
            {
                currentLine++;
                string originalLine = line;
                line = line.TrimStart();

                // 检查是否在条件编译块内，且条件为假
                bool inFalseBlock = false;
                foreach (var (isTrue, depth) in ifStack)
                {
                    if (isTrue == 0)
                    {
                        inFalseBlock = true;
                        break;
                    }
                }

                if (line.StartsWith("#"))
                {
                    // 处理预处理指令
                    if (ProcessDirective(line, processedSource))
                    {
                        continue;
                    }
                }
                else if (!string.IsNullOrEmpty(line) && !inFalseBlock)
                {
                    // 处理宏替换
                    line = ProcessMacros(line);
                    processedSource.AppendLine(line);
                    // 添加行号映射
                    lineMap.Add((currentFile, currentLine));
                }
            }

            // 检查是否有未闭合的条件编译指令
            if (ifStack.Count > 0)
            {
                throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_UnclosedIf, $"在文件 {currentFile} 中，存在未闭合的条件编译指令");
            }

            return processedSource.ToString();
        }

        /// <summary>
        /// 处理预处理指令
        /// </summary>
        /// <param name="directiveLine">预处理指令行</param>
        /// <param name="processedSource">处理后的源代码</param>
        /// <returns>是否成功处理了指令</returns>
        private bool ProcessDirective(string directiveLine, StringBuilder processedSource)
        {
            string[] parts     = directiveLine.Substring(1).Trim().Split(new[] { ' ' }, 2);
            string   directive = parts[0].ToLower();
            string   rest      = parts.Length > 1 ? parts[1].Trim() : "";

            switch (directive)
            {
                case "include":
                    ProcessInclude(rest, processedSource);
                    return true;
                case "define":
                    ProcessDefine(rest);
                    return true;
                case "undef":
                    ProcessUndef(rest);
                    return true;
                case "if":
                    ProcessIf(rest);
                    return true;
                case "ifdef":
                    ProcessIfdef(rest);
                    return true;
                case "ifndef":
                    ProcessIfndef(rest);
                    return true;
                case "else":
                    ProcessElse();
                    return true;
                case "elif":
                    ProcessElif(rest);
                    return true;
                case "endif":
                    ProcessEndif();
                    return true;
                case "line":
                    ProcessLine(rest);
                    return true;
                case "error":
                    ProcessError(rest);
                    return true;
                case "warning":
                    ProcessWarning(rest);
                    return true;
                case "param":
                    ProcessParam(rest);
                    return true;
                case "pragma":
                    // 暂时忽略 #pragma 指令
                    return true;
                default:
                    // 未知指令，忽略
                    return false;
            }
        }

        /// <summary>
        /// 处理 #include 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        /// <param name="processedSource">处理后的源代码</param>
        private void ProcessInclude(string rest, StringBuilder processedSource)
        {
            string includedFile = ParseInclude(rest);
            if (!string.IsNullOrEmpty(includedFile))
            {
                string fullPath = ResolveIncludePath(includedFile, currentFile);
                if (!includedFiles.Contains(fullPath))
                {
                    includedFiles.Add(fullPath);
                    if (File.Exists(fullPath))
                    {
                        string       includeContent      = File.ReadAllText(fullPath);
                        Preprocessor includePreprocessor = new Preprocessor(includeContent, includePaths);
                        includePreprocessor.includedFiles.AddRange(this.includedFiles);
                        includePreprocessor.definitions = new Dictionary<string, string>(this.definitions);
                        includePreprocessor.functionMacros = new Dictionary<string, (List<string>, string)>(this.functionMacros);
                        string processedInclude = includePreprocessor.Process(fullPath);
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
                            }
                        }
                        foreach (var kvp in includePreprocessor.functionMacros)
                        {
                            if (!this.functionMacros.ContainsKey(kvp.Key))
                            {
                                this.functionMacros[kvp.Key] = kvp.Value;
                            }
                        }
                        // 同步 #param lib/path/prefix 指令
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
                        if (includePreprocessor.ParamPrefix != null && this.ParamPrefix == null)
                        {
                            this.ParamPrefix = includePreprocessor.ParamPrefix;
                            this.definitions["VML_PREFIX"] = includePreprocessor.ParamPrefix;
                        }
                    }
                    else
                    {
                        throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_IncludeNotFound, $"在文件 {currentFile} 第 {currentLine} 行，找不到头文件: {fullPath}");
                    }
                }
                else
                {
                    // 文件已经包含过，跳过
                    // Console.WriteLine($"跳过已包含的文件: {fullPath}");
                }
            }
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

            throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_InvalidInclude, $"在文件 {currentFile} 第 {currentLine} 行，无效的 #include 指令格式");
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

            // Detect function-style macro: name must be followed by '(' with no space
            int parenIdx = trimmed.IndexOf('(');
            int spaceIdx = trimmed.IndexOf(' ');

            if (parenIdx > 0 && (spaceIdx < 0 || parenIdx < spaceIdx))
            {
                // Function-style macro: #define name(params) body
                string macroName = trimmed.Substring(0, parenIdx).Trim();
                int closeParen = FindMatchingParen(trimmed, parenIdx);
                if (closeParen < 0)
                    throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_MacroMissingParen, $"在文件 {currentFile} 第 {currentLine} 行，函数式宏缺少右括号: {macroName}");

                string paramsStr = trimmed.Substring(parenIdx + 1, closeParen - parenIdx - 1).Trim();
                string body = trimmed.Substring(closeParen + 1).Trim();

                // Remove leading/trailing whitespace from body; allow empty body
                var paramList = new List<string>();
                if (!string.IsNullOrEmpty(paramsStr))
                {
                    foreach (var p in SplitMacroArgs(paramsStr))
                        if (!string.IsNullOrWhiteSpace(p))
                            paramList.Add(p.Trim());
                }
                functionMacros[macroName] = (paramList, body);
            }
            else
            {
                int spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex == -1)
                {
                    // 无参数宏
                    string macroName = trimmed;
                    definitions[macroName] = "";
                }
                else
                {
                    // 有参数宏
                    string macroName  = trimmed.Substring(0, spaceIndex).Trim();
                    string macroValue = trimmed.Substring(spaceIndex).Trim();
                    definitions[macroName] = macroValue;
                }
            }
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
            ifStack.Add((condition ? 1 : 0, depth));
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
            ifStack.Add((condition ? 1 : 0, depth));
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
            ifStack.Add((condition ? 1 : 0, depth));
        }

        /// <summary>
        /// 处理 #else 指令
        /// </summary>
        private void ProcessElse()
        {
            if (ifStack.Count == 0)
            {
                throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_ElseWithoutIf, $"在文件 {currentFile} 第 {currentLine} 行，#else 指令没有对应的 #if、#ifdef 或 #ifndef 指令");
            }

            var (isTrue, depth) = ifStack[^1];
            ifStack[^1]         = (isTrue == 0 ? 1 : 0, depth);
        }

        /// <summary>
        /// 处理 #elif 指令
        /// </summary>
        /// <param name="rest">指令的剩余部分</param>
        private void ProcessElif(string rest)
        {
            if (ifStack.Count == 0)
            {
                throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_ElifWithoutIf, $"在文件 {currentFile} 第 {currentLine} 行，#elif 指令没有对应的 #if、#ifdef 或 #ifndef 指令");
            }

            var (isTrue, depth) = ifStack[^1];
            if (isTrue == 0)
            {
                bool condition = EvaluateCondition(rest);
                ifStack[^1] = (condition ? 1 : 0, depth);
            }
        }

        /// <summary>
        /// 处理 #endif 指令
        /// </summary>
        private void ProcessEndif()
        {
            if (ifStack.Count == 0)
            {
                throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_EndifWithoutIf, $"在文件 {currentFile} 第 {currentLine} 行，#endif 指令没有对应的 #if、#ifdef 或 #ifndef 指令");
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
            throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_ErrorDirective, $"在文件 {currentFile} 第 {currentLine} 行，预处理错误: {rest}");
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
                throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_ParamSyntaxError, $"在文件 {currentFile} 第 {currentLine} 行，#param 语法错误: {rest}");

            string func = rest.Substring(0, parenOpen).Trim().ToLowerInvariant();
            string arg = rest.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

            switch (func)
            {
                case "lib":
                    // 去掉可选的外层引号
                    if (arg.Length >= 2 &&
                        ((arg.StartsWith('"') && arg.EndsWith('"')) ||
                         (arg.StartsWith('\'') && arg.EndsWith('\''))))
                    {
                        arg = arg.Substring(1, arg.Length - 2);
                    }
                    // 无引号和扩展名时自动补 .vml
                    if (!arg.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Contains('/') && !arg.Contains('\\'))
                    {
                        arg += ".vml";
                    }
                    if (!ParamLibraries.Contains(arg))
                        ParamLibraries.Add(arg);
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
                    if (arg.Length >= 2 &&
                        ((arg.StartsWith('"') && arg.EndsWith('"')) ||
                         (arg.StartsWith('\'') && arg.EndsWith('\''))))
                    {
                        arg = arg.Substring(1, arg.Length - 2);
                    }
                    ParamPrefix = arg;
                    // 同步注入到 defines 中，使 VML_PREFIX 对 @ifdef 可见
                    definitions["VML_PREFIX"] = arg;
                    break;

                default:
                    throw new CompilerBase.CompilationException(ErrorCode.Preprocessor_UnknownParamFunction, $"在文件 {currentFile} 第 {currentLine} 行，未知的 #param 函数: {func}");
            }
        }

        /// <summary>
        /// 评估条件表达式
        /// 支持: defined(MACRO), !expr, expr && expr, expr || expr, 整数常量, 宏名
        /// </summary>
        private bool EvaluateCondition(string condition)
        {
            condition = condition.Trim();
            if (string.IsNullOrEmpty(condition))
                return false;

            // 先展开宏
            condition = ProcessMacros(condition).Trim();
            if (string.IsNullOrEmpty(condition))
                return false;

            return EvaluateExpr(condition) != 0;
        }

        /// <summary>
        /// 递归求值条件/算术表达式，返回整数值
        /// 支持: || && | ^ & == != < <= > >= << >> + - * / % ! ~ - (unary) ?: defined() 常量
        /// </summary>
        private int EvaluateExpr(string expr)
        {
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr))
                return 0;

            // 处理括号 (匹配外层)
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                int depth = 0;
                bool matched = true;
                for (int i = 0; i < expr.Length; i++)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    if (depth == 0 && i < expr.Length - 1) { matched = false; break; }
                }
                if (matched && depth == 0)
                    return EvaluateExpr(expr.Substring(1, expr.Length - 2));
            }

            // 处理三元运算符 a ? b : c (右结合, 匹配问号)
            int ternaryQ = FindTopLevelOp(expr, "?");
            if (ternaryQ >= 0)
            {
                // 找匹配的 : (考虑嵌套的 ? :)
                int qCount = 1, colIdx = -1, depth = 0;
                for (int i = ternaryQ + 1; i < expr.Length; i++)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    else if (depth == 0 && expr[i] == '?') qCount++;
                    else if (depth == 0 && expr[i] == ':') { qCount--; if (qCount == 0) { colIdx = i; break; } }
                }
                if (colIdx >= 0)
                {
                    int cond = EvaluateExpr(expr.Substring(0, ternaryQ));
                    if (cond != 0)
                        return EvaluateExpr(expr.Substring(ternaryQ + 1, colIdx - ternaryQ - 1));
                    else
                        return EvaluateExpr(expr.Substring(colIdx + 1));
                }
            }

            // 处理 || (短路求值, 返回 0 或 1)
            int orIdx = FindTopLevelOp(expr, "||");
            if (orIdx >= 0)
            {
                int left = EvaluateExpr(expr.Substring(0, orIdx));
                if (left != 0) return 1;
                return EvaluateExpr(expr.Substring(orIdx + 2)) != 0 ? 1 : 0;
            }

            // 处理 &&
            int andIdx = FindTopLevelOp(expr, "&&");
            if (andIdx >= 0)
            {
                int left = EvaluateExpr(expr.Substring(0, andIdx));
                if (left == 0) return 0;
                return EvaluateExpr(expr.Substring(andIdx + 2)) != 0 ? 1 : 0;
            }

            // 处理 | (位或)
            int bitOrIdx = FindTopLevelOp(expr, "|");
            if (bitOrIdx >= 0)
                return EvaluateExpr(expr.Substring(0, bitOrIdx)) | EvaluateExpr(expr.Substring(bitOrIdx + 1));

            // 处理 ^ (位异或)
            int xorIdx = FindTopLevelOp(expr, "^");
            if (xorIdx >= 0)
                return EvaluateExpr(expr.Substring(0, xorIdx)) ^ EvaluateExpr(expr.Substring(xorIdx + 1));

            // 处理 & (位与)
            int bitAndIdx = FindTopLevelOp(expr, "&");
            if (bitAndIdx >= 0)
                return EvaluateExpr(expr.Substring(0, bitAndIdx)) & EvaluateExpr(expr.Substring(bitAndIdx + 1));

            // 处理比较: == != <= >= < >
            string[] cmpOps = { "==", "!=", "<=", ">=", "<", ">" };
            foreach (var op in cmpOps)
            {
                int idx = FindTopLevelOp(expr, op);
                if (idx > 0)
                {
                    int lv = EvaluateExpr(expr.Substring(0, idx));
                    int rv = EvaluateExpr(expr.Substring(idx + op.Length));
                    return op switch
                    {
                        "==" => lv == rv ? 1 : 0,
                        "!=" => lv != rv ? 1 : 0,
                        "<=" => lv <= rv ? 1 : 0,
                        ">=" => lv >= rv ? 1 : 0,
                        "<"  => lv < rv ? 1 : 0,
                        ">"  => lv > rv ? 1 : 0,
                        _ => 0
                    };
                }
            }

            // 处理 << >>
            int shlIdx = FindTopLevelOp(expr, "<<");
            if (shlIdx >= 0)
                return EvaluateExpr(expr.Substring(0, shlIdx)) << EvaluateExpr(expr.Substring(shlIdx + 2));
            int shrIdx = FindTopLevelOp(expr, ">>");
            if (shrIdx >= 0)
                return EvaluateExpr(expr.Substring(0, shrIdx)) >> EvaluateExpr(expr.Substring(shrIdx + 2));

            // 处理 + -
            int addIdx = FindTopLevelOp(expr, "+");
            if (addIdx > 0)
                return EvaluateExpr(expr.Substring(0, addIdx)) + EvaluateExpr(expr.Substring(addIdx + 1));
            int subIdx = FindTopLevelOp(expr, "-");
            if (subIdx > 0)
                return EvaluateExpr(expr.Substring(0, subIdx)) - EvaluateExpr(expr.Substring(subIdx + 1));

            // 处理 * / %
            int mulIdx = FindTopLevelOp(expr, "*");
            if (mulIdx >= 0)
                return EvaluateExpr(expr.Substring(0, mulIdx)) * EvaluateExpr(expr.Substring(mulIdx + 1));
            int divIdx = FindTopLevelOp(expr, "/");
            if (divIdx >= 0)
            {
                int divisor = EvaluateExpr(expr.Substring(divIdx + 1));
                if (divisor == 0) return 0;
                return EvaluateExpr(expr.Substring(0, divIdx)) / divisor;
            }
            int modIdx = FindTopLevelOp(expr, "%");
            if (modIdx >= 0)
            {
                int divisor = EvaluateExpr(expr.Substring(modIdx + 1));
                if (divisor == 0) return 0;
                return EvaluateExpr(expr.Substring(0, modIdx)) % divisor;
            }

            // 处理一元运算符
            if (expr.StartsWith("!"))
                return EvaluateExpr(expr.Substring(1)) != 0 ? 0 : 1;
            if (expr.StartsWith("~"))
                return ~EvaluateExpr(expr.Substring(1));
            if (expr.StartsWith("-"))
                return -EvaluateExpr(expr.Substring(1));
            if (expr.StartsWith("+"))
                return EvaluateExpr(expr.Substring(1));

            // defined(MACRO)
            if (expr.StartsWith("defined(") && expr.EndsWith(")"))
            {
                string macroName = expr.Substring(8, expr.Length - 9).Trim();
                if (macroName.StartsWith("(") && macroName.EndsWith(")"))
                    macroName = macroName.Substring(1, macroName.Length - 2);
                return definitions.ContainsKey(macroName) ? 1 : 0;
            }
            if (expr.StartsWith("defined ") && expr.Length > 8)
            {
                string macroName = expr.Substring(8).Trim();
                return definitions.ContainsKey(macroName) ? 1 : 0;
            }

            // 字符常量 'x'
            if (expr.Length >= 3 && expr.StartsWith("'") && expr.EndsWith("'"))
            {
                string inner = expr.Substring(1, expr.Length - 2);
                if (inner.StartsWith("\\"))
                {
                    return inner switch
                    {
                        "\\n" => '\n', "\\t" => '\t', "\\r" => '\r',
                        "\\\\" => '\\', "\\'" => '\'', "\\0" => 0,
                        _ => (int)inner[1]
                    };
                }
                return (int)inner[0];
            }

            // 十六进制常量
            if ((expr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                 expr.StartsWith("0X", StringComparison.OrdinalIgnoreCase)) && expr.Length > 2)
            {
                if (int.TryParse(expr.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int hexVal))
                    return hexVal;
            }

            // 八进制常量
            if (expr.StartsWith("0") && expr.Length > 1 && expr[1] >= '0' && expr[1] <= '7')
            {
                try { return Convert.ToInt32(expr, 8); } catch { }
            }

            // 整数常量
            if (int.TryParse(expr, out int value))
                return value;

            // 宏名 (已展开后, 如果宏名仍存在说明未定义 → 0)
            if (expr.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                if (int.TryParse(expr, out int numVal))
                    return numVal;
                return 0;
            }

            return 0;
        }

        /// <summary>
        /// 查找顶层操作符位置 (不在括号内的)
        /// </summary>
        private int FindTopLevelOp(string expr, string op)
        {
            int depth = 0;
            for (int i = 0; i < expr.Length - op.Length + 1; i++)
            {
                if (expr[i] == '(') depth++;
                else if (expr[i] == ')') depth--;
                else if (depth == 0 && expr.Substring(i, op.Length) == op)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 处理宏替换
        /// </summary>
        /// <param name="line">源代码行</param>
        /// <returns>替换后的源代码行</returns>
        private string ProcessMacros(string line)
        {
            string prev;
            do
            {
                prev = line;
                // 动态宏 — 每次替换使用当前上下文（必须在循环内，以便捕获函数宏展开后的 __LINE__ 等）
                line = Regex.Replace(line, @"\b__LINE__\b", currentLine.ToString());
                line = Regex.Replace(line, @"\b__FILE__\b", $"\"{currentFile.Replace("\\", "\\\\")}\"");
                line = Regex.Replace(line, @"\b__FUNCTION__\b", "\"\"");
                line = Regex.Replace(line, @"\b__func__\b", "\"\"");
                line = Regex.Replace(line, @"\b__DATE__\b", $"\"{CompileDate}\"");
                line = Regex.Replace(line, @"\b__TIME__\b", $"\"{CompileTime}\"");
                line = Regex.Replace(line, @"\b__STDC__\b", "1");
                line = Regex.Replace(line, @"\b__STDC_VERSION__\b", "199901L");
                // Expand function-style macros first
                line = ExpandFunctionMacros(line);
                // Then expand object-style macros
                foreach (var kvp in definitions)
                {
                    string macroName  = kvp.Key;
                    string macroValue = kvp.Value;

                    string pattern = $@"\b{Regex.Escape(macroName)}\b";
                    line = Regex.Replace(line, pattern, macroValue);
                }
            } while (line != prev);

            return line;
        }

        /// <summary>
        /// Expand function-style macro invocations in the given line.
        /// </summary>
        private string ExpandFunctionMacros(string line)
        {
            foreach (var kvp in functionMacros)
            {
                string macroName = kvp.Key;
                var (paramList, body) = kvp.Value;
                if (paramList.Count == 0 && body.Length == 0)
                    continue;

                // Match macroName immediately followed by '(' (with optional whitespace)
                int start = 0;
                while (start < line.Length)
                {
                    int nameIdx = IndexOfWord(line, macroName, start);
                    if (nameIdx < 0) break;

                    // Check for '(' after name (allow whitespace)
                    int parenStart = nameIdx + macroName.Length;
                    while (parenStart < line.Length && char.IsWhiteSpace(line[parenStart]))
                        parenStart++;
                    if (parenStart >= line.Length || line[parenStart] != '(')
                    {
                        start = parenStart;
                        continue;
                    }

                    // Found macroName( — parse argument list
                    int closeParen = FindMatchingParen(line, parenStart);
                    if (closeParen < 0)
                    {
                        start = parenStart + 1;
                        continue;
                    }

                    string argsStr = line.Substring(parenStart + 1, closeParen - parenStart - 1);
                    var args = new List<string>();
                    if (!string.IsNullOrWhiteSpace(argsStr))
                        args = SplitMacroArgs(argsStr);

                    if (args.Count != paramList.Count)
                    {
                        // Argument count mismatch — skip this invocation
                        start = closeParen + 1;
                        continue;
                    }

                    // Build replacement: substitute params in body
                    string replacement = body;
                    for (int i = 0; i < paramList.Count; i++)
                    {
                        string paramPattern = $@"\b{Regex.Escape(paramList[i])}\b";
                        replacement = Regex.Replace(replacement, paramPattern, args[i]);
                    }

                    // Replace the entire invocation
                    line = line.Substring(0, nameIdx) + replacement + line.Substring(closeParen + 1);
                    start = nameIdx; // re-expand from same position
                }
            }

            return line;
        }

        /// <summary>
        /// Find the matching closing parenthesis for an opening paren at the given index.
        /// </summary>
        private static int FindMatchingParen(string s, int openIdx)
        {
            if (openIdx < 0 || openIdx >= s.Length || s[openIdx] != '(') return -1;
            int depth = 1;
            for (int i = openIdx + 1; i < s.Length; i++)
            {
                if (s[i] == '(') depth++;
                else if (s[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Split macro arguments by commas, respecting nested parentheses and quotes.
        /// Handles comma operators inside parenthesized sub-expressions.
        /// </summary>
        private static List<string> SplitMacroArgs(string args)
        {
            var result = new List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;
            bool inChar = false;
            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (inString)
                {
                    if (c == '\\') i++; // skip escaped char
                    else if (c == '"') inString = false;
                }
                else if (inChar)
                {
                    if (c == '\\') i++;
                    else if (c == '\'') inChar = false;
                }
                else if (c == '"') inString = true;
                else if (c == '\'') inChar = true;
                else if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(args.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            if (start < args.Length)
                result.Add(args.Substring(start).Trim());
            return result;
        }

        /// <summary>
        /// Find the index of a whole-word match in the string, starting from the given offset.
        /// Returns -1 if not found.
        /// </summary>
        private static int IndexOfWord(string text, string word, int startIndex)
        {
            for (;;)
            {
                int idx = text.IndexOf(word, startIndex, StringComparison.Ordinal);
                if (idx < 0) return -1;
                // Check word boundary before
                if (idx > 0 && (char.IsLetterOrDigit(text[idx - 1]) || text[idx - 1] == '_'))
                {
                    startIndex = idx + 1;
                    continue;
                }
                // Check word boundary after
                int after = idx + word.Length;
                if (after < text.Length && (char.IsLetterOrDigit(text[after]) || text[after] == '_'))
                {
                    startIndex = after;
                    continue;
                }
                return idx;
            }
        }
    }
}
