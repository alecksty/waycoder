using System.Text;

namespace CompilerBase
{
    public partial class Preprocessor
    {
        private string source;
        private List<string> includedFiles;
        private Dictionary<string, string> definitions;
        private Dictionary<string, (List<string> Params, string Body)> functionMacros;
        private List<(int, int)> ifStack;
        private int currentLine;
        private string currentFile;
        private List<(string, int)> lineMap;
        private List<string> includePaths;
        public List<string> ParamLibraries { get; } = new();
        public List<string> ParamPaths { get; } = new();
        public List<string> ParamPrefixes { get; } = new();
        public bool DumpMode = false;
        public bool PrepareLogMode = false;

        /// <summary>--dump-preprocess: 输出预处理中间状态（宏定义展开、条件求值等）</summary>
        public bool DumpPreprocess = false;

        /// <summary>当前 #include 嵌套深度 (用于进度输出)</summary>
        public int IncludeDepth = 0;

        private static readonly string CompileDate = DateTime.Now.ToString("MMM dd yyyy", System.Globalization.CultureInfo.InvariantCulture);
        private static readonly string CompileTime = DateTime.Now.ToString("HH:mm:ss");

        public Preprocessor(string source, List<string> includePaths = null, Dictionary<string, string> predefinedMacros = null)
        {
            this.source = source;
            includedFiles = new List<string>();
            definitions = new Dictionary<string, string>();
            functionMacros = new Dictionary<string, (List<string> Params, string Body)>();
            ifStack = new List<(int, int)>();
            currentLine = 0;
            currentFile = "<unknown>";
            lineMap = new List<(string, int)>();
            this.includePaths = includePaths ?? new List<string>();
            var envInclude = Environment.GetEnvironmentVariable("VMLTOOL_INCLUDE");
            if (!string.IsNullOrEmpty(envInclude))
            {
                char sep = envInclude.Contains(';') ? ';' : ':';
                foreach (var p in envInclude.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (!this.includePaths.Contains(p))
                        this.includePaths.Add(p);
            }

            if (predefinedMacros != null)
                foreach (var kvp in predefinedMacros)
                    definitions[kvp.Key] = kvp.Value;
            // 初始化宏名快速查找集合
            RebuildMacroNameSet();
        }

        public List<(string, int)> LineMap => lineMap;

        /// <summary>检查行中是否有未闭合的函数式宏调用（如 cast(type,\n expr)）</summary>
        private static bool HasUnclosedMacroCall(string line, Dictionary<string, (List<string>, string)> funcMacros)
        {
            foreach (var kvp in funcMacros)
            {
                int idx = line.IndexOf(kvp.Key);
                while (idx >= 0)
                {
                    // Check for ( after macro name
                    int p = idx + kvp.Key.Length;
                    while (p < line.Length && char.IsWhiteSpace(line[p])) p++;
                    if (p < line.Length && line[p] == '(')
                    {
                        // Check if matching ) exists
                        int depth = 1;
                        for (int i = p + 1; i < line.Length; i++)
                        {
                            if (line[i] == '(') depth++;
                            else if (line[i] == ')')
                            {
                                depth--;
                                if (depth == 0) break;
                            }
                        }

                        if (depth > 0) return true; // unclosed
                    }

                    idx = line.IndexOf(kvp.Key, idx + 1);
                }
            }

            return false;
        }

        /// <summary>快速检查: 当前是否处于活跃的条件编译块（所有层级均为 true，-1 表示已解决的同级 #if/<#elif）</summary>
        private bool IsActiveBlock()
        {
            if (ifStack.Count == 0) return true;
            for (int i = 0; i < ifStack.Count; i++)
                if (ifStack[i].Item1 <= 0)
                    return false; // 0=假, -1=前面的条件已满足→跳过
            return true;
        }

        public string Process(string filePath = "")
        {
            if (!string.IsNullOrEmpty(filePath)) currentFile = filePath;

            int srcLen = source.Length;
            var sb = new StringBuilder(srcLen);
            currentLine = 0;
            int pos = 0;

            // Progress tracking
            int totalLines = 0;
            {
                int i = 0;
                while (i < srcLen)
                {
                    if (source[i] == '\n') totalLines++;
                    i++;
                }
            }
            int reportEvery = totalLines / 100;
            if (reportEvery < 1) reportEvery = 1;
            int nextReport = reportEvery;

            while (pos < srcLen)
            {
                currentLine++;
                if (DumpMode && currentLine >= nextReport)
                {
                    int pct = totalLines > 0 ? (int)((long)currentLine * 100 / totalLines) : 100;
                    string shortFile = Path.GetFileName(currentFile);
                    Console.Error.WriteLine($"percent:{pct}%, line:{currentLine}/{totalLines}, file:{shortFile}, stack:{IncludeDepth}, phase:preprocess.");
                    nextReport = currentLine + reportEvery;
                }

                // 定位行首/行尾 — 第一段
                int lineStart = pos;
                while (pos < srcLen && source[pos] != '\n' && source[pos] != '\r') pos++;
                int lineLen = pos - lineStart;

                // 处理反斜杠行连接 (C99 翻译阶段2): 移除 \ 和随后的换行符
                // 如有行连接，收集各段到 segments 列表，最后拼接
                var segments = (List<(int start, int len)>?)null;
                while (lineLen > 0 && source[lineStart + lineLen - 1] == '\\' && pos < srcLen)
                {
                    segments ??= new List<(int, int)>();
                    segments.Add((lineStart, lineLen - 1)); // 去掉尾部的 \
                    // 跳过 \r\n 或 \n
                    pos++; // 跳过 \r
                    if (pos < srcLen && source[pos] == '\n') pos++; // \r\n 的情况，跳过 \n
                    currentLine++;
                    lineStart = pos; // 下一段的起始
                    while (pos < srcLen && source[pos] != '\n' && source[pos] != '\r') pos++;
                    lineLen = pos - lineStart;
                }

                // 跳过行终止符
                int termEnd = pos; // 记录终止符位置，用于 segments 重扫描边界
                if (pos < srcLen && source[pos] == '\r') pos++;
                if (pos < srcLen && source[pos] == '\n') pos++;

                if (segments != null)
                {
                    // 拼接各段（段数通常 2-3）→ 构造完整逻辑行
                    segments.Add((lineStart, lineLen)); // 最后一段
                    int totalLen = 0;
                    foreach (var (_, len) in segments) totalLen += len;
                    var lineSb = new StringBuilder(totalLen);
                    foreach (var (start, len) in segments)
                        if (len > 0)
                            lineSb.Append(source, start, len);
                    string line = lineSb.ToString();

                    // 快速路径：非 # 行直接输出（但需要宏展开）
                    // 注意：需跳过前导空白，处理缩进的 # 指令（如 "\t#ifdef"）
                    string trimmedForCheck = line.TrimStart();
                    bool isDirective = trimmedForCheck.Length > 0 && trimmedForCheck[0] == '#';
                    if (line.Length == 0 || !isDirective)
                    {
                        if (IsActiveBlock())
                        {
                            if (definitions.Count == 0 && functionMacros.Count == 0)
                            {
                                sb.Append(line);
                                sb.Append('\n');
                                // 同「真快速路径」：输出一行就要记一条（行连接分支上的那一份）
                                lineMap.Add((currentFile, currentLine));
                            }
                            else
                            {
                                line = ProcessMacros(line);
                                // Merge subsequent lines for unclosed function-macro calls (e.g., cast(type,\n expr))
                                while (HasUnclosedMacroCall(line, functionMacros) && pos < srcLen)
                                {
                                    int mergeStart = pos;
                                    while (pos < srcLen && source[pos] != '\n' && source[pos] != '\r') pos++;
                                    int mergeLen = pos - mergeStart;
                                    if (pos < srcLen && source[pos] == '\r') pos++;
                                    if (pos < srcLen && source[pos] == '\n') pos++;
                                    currentLine++;
                                    string nextLine = source.Substring(mergeStart, mergeLen).Trim();
                                    line += " " + nextLine;
                                    line = ProcessMacros(line);
                                }

                                sb.AppendLine(line);
                                lineMap.Add((currentFile, currentLine));
                            }
                        }

                        continue;
                    }

                    // 慢速路径：处理 # 指令
                    string trimmedLine = trimmedForCheck;
                    bool inFalseBlock = false;
                    foreach (var (isTrue, _) in ifStack)
                    {
                        if (isTrue <= 0)
                        {
                            inFalseBlock = true;
                            break;
                        }
                    }

                    if (trimmedLine.StartsWith("#"))
                    {
                        string[] dirParts = trimmedLine.Substring(1).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        string directive = dirParts[0].ToLower();
                        bool isCond = directive == "if" || directive == "ifdef" || directive == "ifndef"
                                      || directive == "else" || directive == "elif" || directive == "endif";
                        if (!inFalseBlock || isCond)
                        {
                            var tempSb = new StringBuilder();
                            if (ProcessDirective(trimmedLine, tempSb))
                            {
                                sb.Append(tempSb);
                                continue;
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(trimmedLine) && !inFalseBlock)
                    {
                        trimmedLine = ProcessMacros(trimmedLine);
                        sb.AppendLine(trimmedLine);
                        lineMap.Add((currentFile, currentLine));
                    }

                    continue;
                }
                // else: 无行连接 — 沿用原有快速路径

                // Fast path: line doesn't start with # (even after trimming whitespace)
                // 跳过前导空白以正确处理缩进的 # 指令（如 "\t#ifdef"）
                int checkPos = lineStart;
                while (checkPos < lineStart + lineLen && (source[checkPos] == ' ' || source[checkPos] == '\t'))
                    checkPos++;
                bool isDirectiveLine = (checkPos < lineStart + lineLen && source[checkPos] == '#');

                if (lineLen == 0 || !isDirectiveLine)
                {
                    if (IsActiveBlock())
                    {
                        if (definitions.Count == 0 && functionMacros.Count == 0)
                        {
                            // 真快速路径: 无宏定义，直接复制
                            sb.Append(source, lineStart, lineLen);
                            sb.Append('\n');
                            // ⚠ **输出了一行就必须记一条映射**：`lineMap[N-1]` 是「输出第 N 行」
                            //   的约定，漏记一行之后**后面每一行都指向前一行**（整张表错位）。
                            //   实测形态就是"报错指着用户文件里另一行" —— 而这里漏记之前在
                            //   这条路径上是**必然**发生的（文件头几行没有宏定义时全走它）。
                            lineMap.Add((currentFile, currentLine));
                        }
                        else
                        {
                            // 准快速路径: 有宏定义，需要展开（跳过 TrimStart + ifStack 遍历）
                            string lineStr = source.Substring(lineStart, lineLen);
                            if (!string.IsNullOrEmpty(lineStr))
                            {
                                lineStr = ProcessMacros(lineStr);
                                // Merge subsequent lines for unclosed function-macro calls
                                while (HasUnclosedMacroCall(lineStr, functionMacros) && pos < srcLen)
                                {
                                    int mergeStart = pos;
                                    while (pos < srcLen && source[pos] != '\n' && source[pos] != '\r') pos++;
                                    int mergeLen = pos - mergeStart;
                                    if (pos < srcLen && source[pos] == '\r') pos++;
                                    if (pos < srcLen && source[pos] == '\n') pos++;
                                    currentLine++;
                                    string nextLine = source.Substring(mergeStart, mergeLen).Trim();
                                    lineStr += " " + nextLine;
                                    lineStr = ProcessMacros(lineStr);
                                }

                                sb.AppendLine(lineStr);
                                lineMap.Add((currentFile, currentLine));
                            }
                        }
                    }

                    continue;
                }

                // Slow path: line starts with # — need string processing
                string line2 = source.Substring(lineStart, lineLen);
                line2 = line2.TrimStart();
                bool inFalseBlock2 = false;
                foreach (var (isTrue, _) in ifStack)
                {
                    if (isTrue <= 0)
                    {
                        inFalseBlock2 = true;
                        break;
                    }
                }

                if (line2.StartsWith("#"))
                {
                    string[] dirParts = line2.Substring(1).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

                    // ⚠ **光秃秃的 `#` 行不是预处理指令**，`dirParts` 会是**空数组**，
                    // 下一行 `dirParts[0]` 直接 IndexOutOfRange **把整个编译打挂**。
                    // 而 `#` 单独成行在 Python / BASIC / shell 里都是**合法注释**
                    // （实测：Python 源里写一行 `#` 就让编译器抛未捕获异常）。
                    // 空数组当"不是指令"处理，原样保留 —— 与"认不出来就当普通文本"一致。
                    if (dirParts.Length == 0)
                        continue;

                    string directive = dirParts[0].ToLower();
                    bool isCond = directive == "if" || directive == "ifdef" || directive == "ifndef"
                                  || directive == "else" || directive == "elif" || directive == "endif";
                    if (!inFalseBlock2 || isCond)
                    {
                        var tempSb = new StringBuilder();
                        if (ProcessDirective(line2, tempSb))
                        {
                            sb.Append(tempSb);
                            continue;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(line2) && !inFalseBlock2)
                {
                    line2 = ProcessMacros(line2);
                    sb.AppendLine(line2);
                    lineMap.Add((currentFile, currentLine));
                }
            }

            while (ifStack.Count > 0) ifStack.RemoveAt(ifStack.Count - 1);
            return sb.ToString();
        }

        /// <summary>
        /// 分发预处理器指令到对应的处理方法（定义在 partial class 文件中）
        /// </summary>
        private bool ProcessDirective(string directiveLine, StringBuilder processedSource)
        {
            // 解析指令名和参数 (如 "#define FOO bar" → dir="define", rest="FOO bar")
            // 注意: 某些源文件使用 tab 而非空格分隔，需同时处理
            string[] parts = directiveLine.Substring(1).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string dir = parts[0].ToLower();
            string rest = parts.Length > 1 ? parts[1].Trim() : "";
            switch (dir)
            {
                case "include":
                case "import":
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
                case "error":
                    ProcessError(rest);
                    return true;
                case "warning":
                    ProcessWarning(rest);
                    return true;
                case "line":
                    ProcessLine(rest);
                    return true;
                case "pragma": return true;
                case "param":
                    ProcessParam(rest);
                    return true;
            }

            return false;
        }
    }
}
