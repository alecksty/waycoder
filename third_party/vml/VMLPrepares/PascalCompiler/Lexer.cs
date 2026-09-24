using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {

        /// <summary>通过 {$param lib(...)} 或 (*$param lib(...)*) 收集的库名列表</summary>
        public List<string> ParamLibraries = new();
        /// <summary>通过 {$param path(...)} 或 (*$param path(...)*) 收集的路径列表</summary>
        public List<string> ParamPaths = new();

        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            // 程序结构关键字
            { "program", TokenType.PROGRAM },
            { "unit", TokenType.UNIT },
            { "interface", TokenType.INTERFACE },
            { "implementation", TokenType.IMPLEMENTATION },
            { "uses", TokenType.USES },
            { "var", TokenType.VAR },
            { "const", TokenType.CONST },
            { "type", TokenType.TYPE },
            { "begin", TokenType.BEGIN },
            { "end", TokenType.END },
            
            // 类型关键字
            { "integer", TokenType.INTEGER },
            { "real", TokenType.REAL },
            { "boolean", TokenType.BOOLEAN },
            { "char", TokenType.CHAR },
            { "string", TokenType.STRING },
            { "array", TokenType.ARRAY },
            { "of", TokenType.OF },
            { "record", TokenType.RECORD },
            { "set", TokenType.SET },
            
            // 控制流关键字
            { "if", TokenType.IF },
            { "then", TokenType.THEN },
            { "else", TokenType.ELSE },
            { "while", TokenType.WHILE },
            { "do", TokenType.DO },
            { "for", TokenType.FOR },
            { "to", TokenType.TO },
            { "downto", TokenType.DOWNTO },
            { "repeat", TokenType.REPEAT },
            { "until", TokenType.UNTIL },
            { "case", TokenType.CASE },
            { "otherwise", TokenType.OTHERWISE },
            { "break", TokenType.BREAK },
            { "continue", TokenType.CONTINUE },
            { "with", TokenType.WITH },
            { "goto", TokenType.GOTO },
            { "label", TokenType.LABEL },
            
            // 过程函数关键字
            { "function", TokenType.FUNCTION },
            { "procedure", TokenType.PROCEDURE },
            { "forward", TokenType.FORWARD },
            
            // 布尔值
            { "true", TokenType.TRUE },
            { "false", TokenType.FALSE },
            { "nil", TokenType.NIL },
            
            // 运算符关键字
            { "and", TokenType.AND },
            { "or", TokenType.OR },
            { "not", TokenType.NOT },
            { "div", TokenType.DIV },
            { "mod", TokenType.MOD },
            { "xor", TokenType.XOR },
            { "shl", TokenType.SHL },
            { "shr", TokenType.SHR },
            
            // 参数方向
            { "in", TokenType.IN },
            { "out", TokenType.OUT },
            { "inout", TokenType.INOUT },
            
            /* ── 文件类型：**刻意不列成保留字**（`File`/`Text`/`TextFile`）────────────
             *
             * Turbo Pascal 里它们是**预定义类型标识符**（standard identifier），
             * **不是保留字** —— 用户可以把它们重新定义成变量名/形参名，这是合法且常见的写法：
             * `var Text: string;`、`procedure P(text: string);`（实测这两条在旧版本里
             * 一律报「期望 ')'」/「期望 'begin'」）。而 `Text` 恰恰是老程序里
             * **最常见的"文字缓冲区"变量名**之一。
             *
             * 代价是它随仓库发的库文件也中招：`Lib/pascal/gfx.pas` 有一句
             * `procedure GfxPrint(x, y: integer; text: string; color: integer);`
             * ⇒ **整个单元解析失败**，于是 `uses Gfx` 的程序的声明**一个都登记不上**、
             * 报一堆"未声明的变量"，而错在一个跟用户源码无关的库里。
             *
             * 所以：**从保留字表里拿出来**，改由 `Parser.ParseType` **按名字**认
             * （它本来就这么处理 `TextFile`，见那里的分支）。这样它们在类型位置仍是文件类型，
             * 在别处（变量名/形参名/表达式）恢复成普通标识符。
             *
             * ⚠ 改这里要连带看 `Parser.cs` 里 `TokenType.FILE` / `TokenType.TEXT` 的**全部**
             * 使用点 —— 目前只有 `ParseType` 两处（`file` 一个、`text` 一个），
             * 已一并改成按名字判。新增别的使用点会让这条"当成普通标识符"的语义破功。 */
            // Delphi/FreePascal OOP 关键字 (v1.66.32+)
            { "class", TokenType.CLASS },
            { "object", TokenType.OBJECT },
            { "constructor", TokenType.CONSTRUCTOR },
            { "destructor", TokenType.DESTRUCTOR },
            { "property", TokenType.PROPERTY },
            { "inherited", TokenType.INHERITED },
            { "virtual", TokenType.VIRTUAL },
            { "override", TokenType.OVERRIDE },
            { "abstract", TokenType.ABSTRACT },
            { "dynamic", TokenType.DYNAMIC },
            { "try", TokenType.TRY },
            { "except", TokenType.EXCEPT },
            { "finally", TokenType.FINALLY },
            { "raise", TokenType.RAISE },
            { "as", TokenType.AS },
            { "is", TokenType.IS }
        };

        public Lexer(string source) : base(source) { }




        private void SkipComment()
        {
            if (Peek() == '{')
            {
                Advance();
                int contentStart = _pos;
                while (true)
                {
                    char ch = Peek();
                    if (ch == '\0') Error("未结束的注释");
                    if (ch == '}')
                    {
                        string content = _source.Substring(contentStart, _pos - contentStart).Trim();
                        Advance();
                        if (content.StartsWith("$param"))
                            ParseParamDirective(content);
                        break;
                    }
                    Advance();
                }
            }
            else if (Peek() == '(' && Peek(1) == '*')
            {
                Advance(); // (
                Advance(); // *
                int contentStart = _pos;
                while (true)
                {
                    char ch = Peek();
                    if (ch == '\0') Error("未结束的注释");
                    if (ch == '*' && Peek(1) == ')')
                    {
                        string content = _source.Substring(contentStart, _pos - contentStart).Trim();
                        Advance(); // *
                        Advance(); // )
                        if (content.StartsWith("$param"))
                            ParseParamDirective(content);
                        break;
                    }
                    Advance();
                }
            }
        }

        private void ParseParamDirective(string content)
        {
            string rest = content.Substring("$param".Length).Trim();
            int parenOpen = rest.IndexOf('(');
            int parenClose = rest.LastIndexOf(')');
            if (parenOpen < 0 || parenClose < 0 || parenClose <= parenOpen) return;

            string func = rest.Substring(0, parenOpen).Trim().ToLowerInvariant();
            string arg = rest.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

            if (arg.Length >= 2 &&
                ((arg.StartsWith('"') && arg.EndsWith('"')) ||
                 (arg.StartsWith('\'') && arg.EndsWith('\''))))
            {
                arg = arg.Substring(1, arg.Length - 2);
            }

            switch (func)
            {
                case "lib":
                    if (!arg.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Contains('/') && !arg.Contains('\\'))
                        arg += ".vml";
                    if (!ParamLibraries.Contains(arg))
                        ParamLibraries.Add(arg);
                    break;
                case "path":
                    if (!ParamPaths.Contains(arg))
                        ParamPaths.Add(arg);
                    break;
            }
        }


        private new Token ReadIdentifier()
        {
            int startLine = _line;
            int startCol = _col;
            StringBuilder value = new StringBuilder();

            while (char.IsLetterOrDigit(Peek()) || Peek() == '_' || IsChineseChar(Peek()))
            {
                value.Append(Advance());
            }

            string identifier = value.ToString().ToLower();
            TokenType tokenType = keywords.ContainsKey(identifier) ? keywords[identifier] : TokenType.IDENTIFIER;
            
            return new Token(tokenType, value.ToString(), startLine, startCol);
        }

        private Token ReadHexNumber()
        {
            int startLine = _line;
            int startCol = _col;
            StringBuilder sb = new StringBuilder("$");
            Advance(); // skip $
            while (char.IsDigit(Peek()) || (Peek() >= 'A' && Peek() <= 'F') || (Peek() >= 'a' && Peek() <= 'f'))
            {
                sb.Append(Advance());
            }
            string hexStr = sb.ToString();
            int hexVal = Convert.ToInt32(hexStr.Substring(1), 16);
            return new Token(TokenType.INTEGER_LITERAL, (long)hexVal, startLine, startCol);
        }

        private new Token ReadNumber()
        {
            int startLine = _line;
            int startCol = _col;
            StringBuilder value = new StringBuilder();

            // 读取整数部分
            while (char.IsDigit(Peek()))
            {
                value.Append(Advance());
            }

            // 检查是否有小数部分
            if (Peek() == '.')
            {
                char nextChar = Peek(1);
                // 如果下一个字符也是'.'，那么这是范围操作符，不是小数部分
                if (nextChar == '.')
                {
                    // 返回整数字面量，'.'留给后面的范围操作符处理
                    return new Token(TokenType.INTEGER_LITERAL, long.Parse(value.ToString()), startLine, startCol);
                }
                else if (char.IsDigit(nextChar))
                {
                    // 这是小数部分
                    value.Append(Advance());
                    while (char.IsDigit(Peek()))
                    {
                        value.Append(Advance());
                    }
                    return new Token(TokenType.REAL_LITERAL, double.Parse(value.ToString()), startLine, startCol);
                }
                else
                {
                    // 单独的'.'，可能是记录字段访问或程序结束
                    // 返回整数字面量，'.'留给后面的操作符处理
                    return new Token(TokenType.INTEGER_LITERAL, long.Parse(value.ToString()), startLine, startCol);
                }
            }

            return new Token(TokenType.INTEGER_LITERAL, long.Parse(value.ToString()), startLine, startCol);
        }

        private Token ReadString()
        {
            int startLine = _line;
            int startCol = _col;
            Advance(); // 跳过起始引号
            StringBuilder value = new StringBuilder();

            while (true)
            {
                char ch = Peek();
                if (ch == '\0')
                {
                    Error("未结束的字符串");
                }
                if (ch == '"')
                {
                    Advance();
                    break;
                }

                // 处理转义字符
                if (ch == '\\')
                {
                    Advance();
                    char escapeCh = Advance();
                    switch (escapeCh)
                    {
                        case 'n': value.Append('\n'); break;
                        case 't': value.Append('\t'); break;
                        case 'r': value.Append('\r'); break;
                        case '\\': value.Append('\\'); break;
                        case '"': value.Append('"'); break;
                        default: value.Append(escapeCh); break;
                    }
                }
                else
                {
                    value.Append(ch);
                    Advance();
                }
            }

            return new Token(TokenType.STRING_LITERAL, value.ToString(), startLine, startCol);
        }

        /// <summary>
        /// Pascal **字面量**：一段 `'…'` 与若干个 `#nn` 是**同一个**字面量，要拼起来。
        ///
        /// <para>
        /// `'text'#13#10` / `#13#10` / `#219#219` 是 Turbo Pascal 最常见的三种写法
        /// （换行、制表、制表符画框），而此前词法器**每段各吐一个 token** ——
        /// 语法分析拿到 `STRING_LITERAL` 后面紧跟一个 `CHAR_LITERAL`，只能报
        /// `期望 ')'`（实测 `Write(#219#219)` 报的就是它）。语料里
        /// `'This line is blinking…'#10#13#10#13`、`#219#219#219` 都是这个形态。
        /// </para>
        /// <para>
        /// 合并规则照 Turbo Pascal：**只拼紧挨着的**（中间有空白就算两个独立字面量，
        /// 那在 Pascal 里本来就是语法错，不该被我们"修好"）。全部拼完长度为 1
        /// 仍是 <see cref="TokenType.CHAR_LITERAL"/>（单个 `'a'` / 单个 `#13` 的旧行为一字不变），
        /// 否则是 <see cref="TokenType.STRING_LITERAL"/>。
        /// </para>
        /// </summary>
        private Token ReadPascalLiteral()
        {
            int startLine = _line;
            int startCol = _col;
            var value = new StringBuilder();

            if (Peek() == '#')
                value.Append(ReadCharEscapeValue());
            else
                value.Append(ReadPascalStringCore());

            // 紧跟其后的 `#nn` / `'…'` 段继续拼（`#` 与 `'` 两种起始都走这里）
            while (true)
            {
                if (Peek() == '#') value.Append(ReadCharEscapeValue());
                else if (Peek() == '\'') value.Append(ReadPascalStringCore());
                else break;
            }

            if (value.Length == 1)
                return new Token(TokenType.CHAR_LITERAL, value[0], startLine, startCol);
            return new Token(TokenType.STRING_LITERAL, value.ToString(), startLine, startCol);
        }

        /// <summary>`#nn` 字符常量 —— 十进制 `#65` 与十六进制 `#$FF` 两种写法都认
        /// （`#$FF` 是 Turbo Pascal 的写法，语料里 `g7iles_mario.pas` 一份就用了 2209 处）。</summary>
        private char ReadCharEscapeValue()
        {
            Advance(); // 跳过'#'字符

            bool hex = Peek() == '$';
            if (hex) Advance();

            StringBuilder value = new StringBuilder();
            while (hex ? LexerHelper.IsHexDigit(Peek()) : char.IsDigit(Peek()))
            {
                value.Append(Advance());
            }

            if (value.Length == 0)
            {
                Error("字符转义需要数字");
            }

            int charCode = Convert.ToInt32(value.ToString(), hex ? 16 : 10);
            if (charCode < 0 || charCode > 255)
            {
                Error($"无效的字符代码: {charCode}");
            }

            return (char)charCode;
        }

        /// <summary>读一段 `'…'`（不含 `#nn`），返回其正文。`''` = 一个转义的单引号。</summary>
        private string ReadPascalStringCore()
        {
            Advance(); // 跳过起始单引号
            StringBuilder value = new StringBuilder();

            while (true)
            {
                char ch = Peek();
                if (ch == '\0')
                {
                    Error("未结束的字符串");
                }
                if (ch == '\'')
                {
                    Advance();
                    // 检查是否是两个连续的单引号（表示转义的单引号）
                    if (Peek() == '\'')
                    {
                        value.Append('\'');
                        Advance();
                        continue;
                    }
                    break;
                }

                // ⚠ **Pascal 字符串里没有反斜杠转义** —— `\` 就是一个普普通通的字符。
                //   此前这里有一整套 `\n`/`\t`/`\'` 的处理（注释还写着"Pascal中通常不支持
                //   转义，但我们可以支持基本转义"），代价是**把老程序编坏**：
                //   `'d:\turbo\tp\'`（语料 `swag_graphics_0069.pas` 里就有）里的 `\'`
                //   被当成"转义的单引号"⇒ 字符串**从那里继续往下吃**，本该结束的字符串
                //   再也没结束，后面第一个 `{` 也不再是注释 —— 于是报出一串
                //   `未知字符: !` / `未知字符: ?` / `未知字符: }`，**错误位置离病根十万八千里**
                //   （`avc_file_select.pas` 报在 753 行，病根是 178 行的 `'d:\'`）。
                //   这正是一整类「语言前端替用户发明语法」的坑：作者以为在帮忙，
                //   实际是让本来正确的老代码编译不过。
                value.Append(ch);
                Advance();
            }

            return value.ToString();
        }

        private new void Error(string message)
        {
            Error(ErrorCode.Lexer_UnknownCharacter, message);   // 位置交给 LexerBase 的唯一出口（不再自拼「在第N行M列」）
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (_pos < _source.Length)
            {
                SkipWhitespace();

                char current = Peek();
                if (current == '\0')
                {
                    break;
                }

                // 跳过注释
                if (current == '{' || (current == '(' && Peek(1) == '*'))
                {
                    SkipComment();
                    continue;
                }

                // `//` **行注释** —— 此前词法器只认 `{ }` 与 `(* *)`，
                // `//` 会原样吐成两个 `/` 交给语法分析 ⇒ 报的是莫名其妙的语法错
                //（台账里那条「注释是 `{ }` 不是 `//`」说的就是这个）。
                // 现代 Pascal（Delphi / Free Pascal）都认 `//`，而本前端的目标就是它们
                //（`writeln` / `program … end.` 那一套）。
                // 语义与 `{}` 的区别只有一条：**到行尾就结束**，不跨行。
                if (current == '/' && Peek(1) == '/')
                {
                    while (Peek() != '\n' && Peek() != '\0') Advance();
                    continue;
                }

                // 标识符或关键字
                if (char.IsLetter(current) || current == '_' || IsChineseChar(current))
                {
                    tokens.Add(ReadIdentifier());
                    continue;
                }

                // Pascal字符常量（#十进制 / #$十六进制）—— 与紧邻的字符串段合并
                if (current == '#')
                {
                    tokens.Add(ReadPascalLiteral());
                    continue;
                }

                // Pascal 十六进制: $FF
                if (current == '$')
                {
                    tokens.Add(ReadHexNumber());
                    continue;
                }

                // 数字
                if (char.IsDigit(current))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                // Pascal 字符串（使用单引号）—— 后面紧跟的 `#nn` 段一并拼进来
                if (current == '\'')
                {
                    tokens.Add(ReadPascalLiteral());
                    continue;
                }

                // C风格字符串（双引号）- 保留用于兼容性
                if (current == '"')
                {
                    tokens.Add(ReadString());
                    continue;
                }

                // 运算符和分隔符
                int startLine = _line;
                int startCol = _col;

                // 双字符运算符
                char next = Peek(1);
                if (current == ':' && next == '=')
                {
                    Advance(); // :
                    Advance(); // =
                    tokens.Add(new Token(TokenType.ASSIGN, ":=", startLine, startCol));
                }
                else if (current == '<' && next == '>')
                {
                    Advance(); // <
                    Advance(); // >
                    tokens.Add(new Token(TokenType.NOT_EQUALS, "<>", startLine, startCol));
                }
                else if (current == '<' && next == '=')
                {
                    Advance(); // <
                    Advance(); // =
                    tokens.Add(new Token(TokenType.LESS_EQUAL, "<=", startLine, startCol));
                }
                else if (current == '>' && next == '=')
                {
                    Advance(); // >
                    Advance(); // =
                    tokens.Add(new Token(TokenType.GREATER_EQUAL, ">=", startLine, startCol));
                }
                else if (current == '.' && next == '.')
                {
                    Advance(); // .
                    Advance(); // .
                    tokens.Add(new Token(TokenType.RANGE, "..", startLine, startCol));
                }
                else
                {
                    // 单字符运算符
                    Advance();
                    switch (current)
                    {
                        case '+': tokens.Add(new Token(TokenType.PLUS, "+", startLine, startCol)); break;
                        case '-': tokens.Add(new Token(TokenType.MINUS, "-", startLine, startCol)); break;
                        case '*': tokens.Add(new Token(TokenType.STAR, "*", startLine, startCol)); break;
                        case '/': tokens.Add(new Token(TokenType.SLASH, "/", startLine, startCol)); break;
                        case '=': tokens.Add(new Token(TokenType.EQUALS, "=", startLine, startCol)); break;
                        case '<': tokens.Add(new Token(TokenType.LESS_THAN, "<", startLine, startCol)); break;
                        case '>': tokens.Add(new Token(TokenType.GREATER_THAN, ">", startLine, startCol)); break;
                        case '(': tokens.Add(new Token(TokenType.LPAREN, "(", startLine, startCol)); break;
                        case ')': tokens.Add(new Token(TokenType.RPAREN, ")", startLine, startCol)); break;
                        case '[': tokens.Add(new Token(TokenType.LBRACKET, "[", startLine, startCol)); break;
                        case ']': tokens.Add(new Token(TokenType.RBRACKET, "]", startLine, startCol)); break;
                        case '.': tokens.Add(new Token(TokenType.DOT, ".", startLine, startCol)); break;
                        case ',': tokens.Add(new Token(TokenType.COMMA, ",", startLine, startCol)); break;
                        case ':': tokens.Add(new Token(TokenType.COLON, ":", startLine, startCol)); break;
                        case ';': tokens.Add(new Token(TokenType.SEMICOLON, ";", startLine, startCol)); break;
                        case '^': tokens.Add(new Token(TokenType.CARET, "^", startLine, startCol)); break;
                        case '@': tokens.Add(new Token(TokenType.AT, "@", startLine, startCol)); break;
                        case '|': tokens.Add(new Token(TokenType.PIPE, "|", startLine, startCol)); break;
                        case '\\': tokens.Add(new Token(TokenType.BACKSLASH, "\\", startLine, startCol)); break;
                        default:
                            // 跳过 CP/M EOF 标记 (0x1A) 和 null 字节 (v1.66.33)
                            if (current == '\x1A' || current == '\0')
                                break;
                            // 忽略不可打印的控制字符 (v1.66.33)
                            if (current < 32 && current != '\n' && current != '\r' && current != '\t')
                                break;
                            Error($"未知字符: {current}");
                            break;
                    }
                }
            }

            // 添加 EOF 标记
            tokens.Add(new Token(TokenType.EOF, null, _line, _col));
            return tokens;
        }
    }
}
