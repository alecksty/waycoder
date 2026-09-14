using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilerBase
{
    public partial class Preprocessor
    {
        /// <summary>剥离整数后缀 (U, L, UL, LU, LL, ULL, LLU)</summary>
        private static string StripIntSuffix(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return expr;
            int end = expr.Length;
            while (end > 0 && (expr[end - 1] == 'U' || expr[end - 1] == 'u' ||
                               expr[end - 1] == 'L' || expr[end - 1] == 'l'))
                end--;
            return end == expr.Length ? expr : expr.Substring(0, end);
        }

        /// <summary>剥离 C 风格注释（// ... 和 /* ... */）</summary>
        private static string StripCComments(string expr)
        {
            // 移除 /* ... */ 注释
            int blockIdx;
            while ((blockIdx = expr.IndexOf("/*")) >= 0)
            {
                int blockEnd = expr.IndexOf("*/", blockIdx + 2);
                if (blockEnd >= 0)
                    expr = expr.Substring(0, blockIdx) + expr.Substring(blockEnd + 2);
                else
                    expr = expr.Substring(0, blockIdx); // 未闭合，截断
            }
            // 移除 // 注释
            int lineIdx = expr.IndexOf("//");
            if (lineIdx >= 0)
                expr = expr.Substring(0, lineIdx);
            return expr;
        }

        private bool EvaluateCondition(string condition)
        {
            string rawCondition = condition.Trim();
            if (string.IsNullOrEmpty(rawCondition))
                return false;

            // 剥离 C 注释 — 必须在求值前移除，否则 /* ... */ 会影响表达式解析
            condition = StripCComments(rawCondition).Trim();
            if (string.IsNullOrEmpty(condition))
                return false;

            // 先处理 defined(MACRO) — 必须在宏展开之前求值，否则宏展开会将
            // defined(CBC) 中的 CBC 替换为 1，变成 defined(1) 永远为假
            string resolved = ResolveDefined(condition);

            // 再展开其他宏
            string expanded = ProcessMacros(resolved).Trim();
            if (string.IsNullOrEmpty(expanded))
                return false;

            int result = EvaluateExpr(expanded);
            if (DumpPreprocess)
            {
                string shortFile = System.IO.Path.GetFileName(currentFile);
                Console.Error.WriteLine($"[preprocess] {shortFile}:{currentLine}: #if '{rawCondition}' stripped='{condition}' resolved='{resolved}' expanded='{expanded}' => {(result != 0 ? "TRUE" : "FALSE")}");
            }
            return result != 0;
        }

        // Replace defined(MACRO) and defined MACRO with 1 or 0 before macro expansion
        private string ResolveDefined(string expr)
        {
            int idx;
            while ((idx = expr.IndexOf("defined")) >= 0)
            {
                int start = idx;
                idx += 7; // past "defined"
                // skip whitespace
                while (idx < expr.Length && expr[idx] == ' ') idx++;
                if (idx >= expr.Length) break;

                string macroName;
                int endIdx;
                if (expr[idx] == '(')
                {
                    // defined(MACRO)
                    int parenDepth = 1;
                    endIdx = idx + 1;
                    while (endIdx < expr.Length && parenDepth > 0)
                    {
                        if (expr[endIdx] == '(') parenDepth++;
                        else if (expr[endIdx] == ')') parenDepth--;
                        endIdx++;
                    }
                    macroName = expr.Substring(idx + 1, endIdx - idx - 2).Trim();
                }
                else
                {
                    // defined MACRO
                    endIdx = idx;
                    while (endIdx < expr.Length && (char.IsLetterOrDigit(expr[endIdx]) || expr[endIdx] == '_'))
                        endIdx++;
                    macroName = expr.Substring(idx, endIdx - idx).Trim();
                }
                int val = definitions.ContainsKey(macroName) ? 1 : 0;
                expr = expr.Substring(0, start) + val.ToString() + expr.Substring(endIdx);
            }
            return expr;
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
            // 注意: < 和 > 可能错误匹配 << 和 >>，需要在查找时排除
            string[] cmpOps = { "==", "!=", "<=", ">=", "<", ">" };
            foreach (var op in cmpOps)
            {
                int idx = FindTopLevelOp(expr, op);
                // 排除 < 匹配到 << 或 <=，排除 > 匹配到 >> 或 >=
                if (idx >= 0 && (op == "<" || op == ">"))
                {
                    if (idx + 1 < expr.Length && expr[idx + 1] == op[0])
                        continue; // << 或 >>
                }
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
                return (int)((uint)EvaluateExpr(expr.Substring(0, shrIdx)) >> EvaluateExpr(expr.Substring(shrIdx + 2)));

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
                string hexStr = StripIntSuffix(expr.Substring(2));
                // 优先尝试有符号 long，溢出时回退到 ulong
                if (long.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out long hexVal))
                    return (int)(hexVal & 0xFFFFFFFF);
                if (ulong.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out ulong uhexVal))
                    return (int)(uhexVal & 0xFFFFFFFF);
            }

            // 八进制常量
            if (expr.StartsWith("0") && expr.Length > 1 && expr[1] >= '0' && expr[1] <= '7')
            {
                string octStr = StripIntSuffix(expr);
                try { return (int)(Convert.ToInt64(octStr, 8) & 0xFFFFFFFF); } catch { }
            }

            // 整数常量 — 处理 U/UL/ULL/L 后缀和大数值
            {
                string clean = StripIntSuffix(expr);
                if (long.TryParse(clean, out long longVal))
                    return (int)(longVal & 0xFFFFFFFF);
            }

            // 宏名 (已展开后, 如果宏名仍存在说明未定义 → 0)
            if (expr.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                if (long.TryParse(expr, out long numVal))
                    return (int)(numVal & 0xFFFFFFFF);
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
        /// <summary>预编译的标准宏 Regex（避免每次调用重新编译）</summary>
        private static readonly Regex RE_LINE = new Regex(@"\b__LINE__\b", RegexOptions.Compiled);
        private static readonly Regex RE_FILE = new Regex(@"\b__FILE__\b", RegexOptions.Compiled);
        private static readonly Regex RE_FUNCTION = new Regex(@"\b__FUNCTION__\b", RegexOptions.Compiled);
        private static readonly Regex RE_FUNC = new Regex(@"\b__func__\b", RegexOptions.Compiled);
        private static readonly Regex RE_DATE = new Regex(@"\b__DATE__\b", RegexOptions.Compiled);
        private static readonly Regex RE_TIME = new Regex(@"\b__TIME__\b", RegexOptions.Compiled);
        private static readonly Regex RE_STDC = new Regex(@"\b__STDC__\b", RegexOptions.Compiled);
        private static readonly Regex RE_STDC_VERSION = new Regex(@"\b__STDC_VERSION__\b", RegexOptions.Compiled);

        /// <summary>所有已定义宏名的集合（define/undef 时增量更新）</summary>
        private HashSet<string> _macroNameSet = new HashSet<string>();

        /// <summary>重建宏名集合（初始化时调用一次）</summary>
        private void RebuildMacroNameSet()
        {
            _macroNameSet = new HashSet<string>();
            foreach (var k in definitions.Keys) { _macroNameSet.Add(k); if (!k.Contains('_')) _hasNonUnderscoreMacro = true; }
            foreach (var k in functionMacros.Keys) _macroNameSet.Add(k);
            _macroNameSet.Add("__LINE__");
            _macroNameSet.Add("__FILE__");
            _macroNameSet.Add("__FUNCTION__");
            _macroNameSet.Add("__func__");
            _macroNameSet.Add("__DATE__");
            _macroNameSet.Add("__TIME__");
            _macroNameSet.Add("__STDC__");
            _macroNameSet.Add("__STDC_VERSION__");
        }

        /// <summary>跟踪是否有宏名不含下划线（用于优化判断）</summary>
        private bool _hasNonUnderscoreMacro = false;

        /// <summary>快速检查行中是否可能包含宏名</summary>
        private bool LineContainsAnyMacro(string line)
        {
            // 快速否决：所有宏名都含下划线时才可用此优化
            if (!_hasNonUnderscoreMacro && line.IndexOf('_') < 0) return false;
            int len = line.Length;
            for (int i = 0; i < len; i++)
            {
                char c = line[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_')
                {
                    int end = i + 1;
                    while (end < len && ((line[end] >= 'a' && line[end] <= 'z') || (line[end] >= 'A' && line[end] <= 'Z') ||
                                         (line[end] >= '0' && line[end] <= '9') || line[end] == '_')) end++;
                    if (_macroNameSet.Contains(line.Substring(i, end - i))) return true;
                    i = end;
                }
            }
            return false;
        }

        /// 处理宏替换
        /// </summary>
        /// <param name="line">源代码行</param>
        /// <returns>替换后的源代码行</returns>
        private string ProcessMacros(string line)
        {
            // 预检查：快速跳过不含任何宏的行（绝大多数行）
            if (_macroNameSet.Count > 0 && !LineContainsAnyMacro(line))
                return line;

            string prev;
            int safety = 0;
            const int MAX_MACRO_PASSES = 100;
            do
            {
                prev = line;
                // 动态宏 — 使用预编译的静态 Regex（必须在循环内，以便捕获函数宏展开后的 __LINE__ 等）
                line = RE_LINE.Replace(line, currentLine.ToString());
                line = RE_FILE.Replace(line, $"\"{currentFile.Replace("\\", "\\\\")}\"");
                line = RE_FUNCTION.Replace(line, "\"\"");
                line = RE_FUNC.Replace(line, "\"\"");
                line = RE_DATE.Replace(line, $"\"{CompileDate}\"");
                line = RE_TIME.Replace(line, $"\"{CompileTime}\"");
                line = RE_STDC.Replace(line, "1");
                line = RE_STDC_VERSION.Replace(line, "199901L");
                // Expand function-style macros first
                if (functionMacros.Count > 0)
                    line = ExpandFunctionMacros(line);
                // Then expand object-style macros (skip string/char literals)
                foreach (var kvp in definitions)
                {
                    string macroName  = kvp.Key;
                    string macroValue = kvp.Value;
                    // 快速检查：如果行中不包含该宏名，跳过
                    if (!line.Contains(macroName))
                        continue;
                    line = ReplaceOutsideLiterals(line, macroName, macroValue);
                }
                if (++safety > MAX_MACRO_PASSES)
                {
                    if (PrepareLogMode) Console.Error.WriteLine($"[预处理] 宏展开超过 {MAX_MACRO_PASSES} 次迭代，可能有递归宏定义，停止展开");
                    break;
                }
            } while (line != prev);

            return line;
        }

        /// <summary>
        /// Replace macro name with value, skipping content inside string and char literals.
        /// </summary>
        private static string ReplaceOutsideLiterals(string line, string macroName, string macroValue)
        {
            var sb = new StringBuilder();
            bool inString = false;
            bool inChar = false;
            int i = 0;

            while (i < line.Length)
            {
                if (inString)
                {
                    if (line[i] == '\\') { sb.Append(line[i]); i++; if (i < line.Length) sb.Append(line[i]); }
                    else if (line[i] == '"') { inString = false; sb.Append(line[i]); }
                    else sb.Append(line[i]);
                    i++;
                }
                else if (inChar)
                {
                    if (line[i] == '\\') { sb.Append(line[i]); i++; if (i < line.Length) sb.Append(line[i]); }
                    else if (line[i] == '\'') { inChar = false; sb.Append(line[i]); }
                    else sb.Append(line[i]);
                    i++;
                }
                else if (line[i] == '"') { inString = true; sb.Append(line[i]); i++; }
                else if (line[i] == '\'') { inChar = true; sb.Append(line[i]); i++; }
                else if (IsWordAt(line, i, macroName))
                {
                    sb.Append(macroValue);
                    i += macroName.Length;
                }
                else
                {
                    sb.Append(line[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        private static bool IsWordAt(string line, int pos, string name)
        {
            if (pos + name.Length > line.Length) return false;
            for (int j = 0; j < name.Length; j++)
                if (line[pos + j] != name[j]) return false;
            if (pos > 0 && IsWordChar(line[pos - 1])) return false;
            if (pos + name.Length < line.Length && IsWordChar(line[pos + name.Length])) return false;
            return true;
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        /// <summary>
        /// Expand function-style macro invocations in the given line.
        /// </summary>
        private string ExpandFunctionMacros(string line)
        {
            foreach (var kvp in functionMacros)
            {
                string macroName = kvp.Key;
                // 快速跳过：行中不包含此宏名
                if (!line.Contains(macroName)) continue;
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

                    // 可变参数宏: 最后一个参数是 __VA_ARGS__
                    bool isVariadic = paramList.Count > 0 && paramList[paramList.Count - 1] == "__VA_ARGS__";
                    int namedParamCount = isVariadic ? paramList.Count - 1 : paramList.Count;

                    if (isVariadic)
                    {
                        if (args.Count < namedParamCount)
                        {
                            start = closeParen + 1;
                            continue;
                        }
                    }
                    else
                    {
                        if (args.Count != paramList.Count)
                        {
                            start = closeParen + 1;
                            continue;
                        }
                    }

                    // Build replacement: substitute params in body
                    string replacement = body;
                    for (int i = 0; i < paramList.Count; i++)
                    {
                        if (paramList[i] == "__VA_ARGS__")
                        {
                            string vaArgsStr = string.Join(", ", args.GetRange(namedParamCount, args.Count - namedParamCount));
                            replacement = replacement.Replace("__VA_ARGS__", vaArgsStr);
                        }
                        else
                        {
                            string paramName = paramList[i];
                            // Handle ## (token paste) FIRST — must be before # to avoid #n matching inside ##n
                            replacement = Regex.Replace(replacement, $@"{Regex.Escape(paramName)}\s*##\s*(\w+)",
                                match => {
                                    string right = match.Groups[1].Value;
                                    int ri = paramList.IndexOf(right);
                                    return args[i] + (ri >= 0 ? args[ri] : right);
                                });
                            replacement = Regex.Replace(replacement, $@"(\w+)\s*##\s*{Regex.Escape(paramName)}",
                                match => {
                                    string left = match.Groups[1].Value;
                                    int li = paramList.IndexOf(left);
                                    return (li >= 0 ? args[li] : left) + args[i];
                                });
                            // Handle # (stringification) — use negative lookbehind to avoid matching ##param
                            replacement = Regex.Replace(replacement, $@"(?<!#)#\s*{Regex.Escape(paramName)}",
                                match => $"\"{args[i]}\"");
                            // Regular parameter substitution
                            string paramPattern = $@"\b{Regex.Escape(paramName)}\b";
                            replacement = Regex.Replace(replacement, paramPattern, args[i]);
                        }
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
