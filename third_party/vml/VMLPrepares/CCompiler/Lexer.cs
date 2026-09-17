using System.Collections.Generic;
using CompilerBase;

namespace CCompiler
{
    /// <summary>
    /// C 语言词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        private List<(string, int)> lineMap; // 行号映射，记录处理后的每一行对应原始文件和行号
        private List<int> _lineOffsets; // 每行在 _source 中的字符偏移量（用于 O(1) GetLine）
        public List<Token> Tokens { get; private set; }

        // 关键字映射
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            { "int", TokenType.INT },
            { "void", TokenType.VOID },
            { "char", TokenType.CHAR },
            { "float", TokenType.FLOAT },
            { "double", TokenType.DOUBLE },
            { "short", TokenType.SHORT },
            { "long", TokenType.LONG },
            { "signed", TokenType.SIGNED },
            { "unsigned", TokenType.UNSIGNED },
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "while", TokenType.WHILE },
            { "for", TokenType.FOR },
            { "return", TokenType.RETURN },
            { "break", TokenType.BREAK },
            { "continue", TokenType.CONTINUE },
            { "enum", TokenType.ENUM },
            { "struct", TokenType.STRUCT },
            { "union", TokenType.UNION },
            { "switch", TokenType.SWITCH },
            { "case", TokenType.CASE },
            { "default", TokenType.DEFAULT },
            { "asm", TokenType.ASM },
            { "auto", TokenType.AUTO },
            { "const", TokenType.CONST },
            { "do", TokenType.DO },
            { "goto", TokenType.GOTO },
            { "register", TokenType.REGISTER },
            { "sizeof", TokenType.SIZEOF },
            { "static", TokenType.STATIC },
            { "typedef", TokenType.TYPEDEF },
            { "volatile", TokenType.VOLATILE },
            { "extern", TokenType.EXTERN },
            { "interrupt", TokenType.INTERRUPT },
            { "__interrupt", TokenType.INTERRUPT },
            { "__chipasm__", TokenType.CHIPASM },
            { "__stdcall", TokenType.STDCALL },
            { "__fastcall", TokenType.FASTCALL },
            { "__cdecl", TokenType.CDECL },
            { "try", TokenType.TRY },
            { "catch", TokenType.CATCH },
            { "throw", TokenType.THROW },
            
            // C99 关键字
            { "inline", TokenType.INLINE },
            { "restrict", TokenType.RESTRICT },
            { "_Bool", TokenType.BOOL },
            { "_Complex", TokenType.COMPLEX },
            { "_Imaginary", TokenType.IMAGINARY },
            { "int8_t", TokenType.INT8 },
            { "int16_t", TokenType.INT16 },
            { "int32_t", TokenType.INT32 },
            { "int64_t", TokenType.INT64 },
            { "uint8_t", TokenType.UINT8 },
            { "uint16_t", TokenType.UINT16 },
            { "uint32_t", TokenType.UINT32 },
            { "uint64_t", TokenType.UINT64 },
            { "intptr_t", TokenType.INTPTR_T },
            { "uintptr_t", TokenType.UINTPTR_T },
            { "size_t", TokenType.SIZE_T },
            { "ssize_t", TokenType.SSIZE_T },
            { "ptrdiff_t", TokenType.PTRDIFF_T },
            { "wchar_t", TokenType.IDENTIFIER },
            { "char32_t", TokenType.IDENTIFIER },
        };

        /// <summary>单字符 Token 映射（static 避免每 token 重新分配）</summary>
        private static readonly Dictionary<char, TokenType> SINGLE_CHAR_TOKENS = new()
        {
            { '+', TokenType.PLUS },
            { '-', TokenType.MINUS },
            { '*', TokenType.STAR },
            { '/', TokenType.SLASH },
            { '%', TokenType.PERCENT },
            { '=', TokenType.ASSIGN },
            { '<', TokenType.LT },
            { '>', TokenType.GT },
            { '!', TokenType.NOT },
            { '&', TokenType.AMPERSAND },
            { '|', TokenType.PIPE },
            { '^', TokenType.CARET },
            { '~', TokenType.TILDE },
            { '.', TokenType.DOT },
            { '?', TokenType.QUESTION },
            { '(', TokenType.LPAREN },
            { ')', TokenType.RPAREN },
            { '{', TokenType.LBRACE },
            { '}', TokenType.RBRACE },
            { '[', TokenType.LBRACKET },
            { ']', TokenType.RBRACKET },
            { ',', TokenType.COMMA },
            { ';', TokenType.SEMICOLON },
            { ':', TokenType.COLON },
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source">源代码字符串</param>
        /// <param name="lineMap">行号映射，记录处理后的每一行对应原始文件和行号</param>
        public Lexer(string source, List<(string, int)> lineMap = null, string? fileName = null) : base(source, fileName)
        {
            this.lineMap = lineMap;
            Tokens = new List<Token>();
            if (fileName != null) FileName = fileName;
        }

        /// <summary>
        /// 抛出词法错误异常
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <exception cref="CompilerBase.ParseException">词法错误异常</exception>
        private new void Error(string message) => Error(ErrorCode.Unknown, message);

        private new void Error(ErrorCode code, string message)
        {
            var errorLine = GetLine(_line);
            var arrow = new string(' ', _col - 1) + "^";
            var (originalFile, originalLine) = GetOriginalFileAndLine(_line);
            var fileName = originalFile != null ? $"文件 {originalFile} " : "";
            var lineNumber = originalLine > 0 ? originalLine : _line;
            throw new CompilerBase.ParseException(code,
                $"词法错误 {fileName}在第{lineNumber}行{_col}列：{message}\n{errorLine}\n{arrow}");
        }

        /// <summary>
        /// 一次性扫描 _source 建立行起始偏移索引（O(chars)），后续 GetLine 即可 O(1)
        /// </summary>
        private void BuildLineOffsets()
        {
            _lineOffsets = new List<int>(_source.Length / 20) { 0 }; // 预估每行~20字符
            for (int i = 0; i < _source.Length; i++)
            {
                char c = _source[i];
                if (c == '\n')
                {
                    if (i + 1 < _source.Length) _lineOffsets.Add(i + 1);
                }
                else if (c == '\r')
                {
                    // CRLF: skip \n after \r
                    int next = i + 1;
                    if (next < _source.Length && _source[next] == '\n') i++;
                    if (i + 1 < _source.Length) _lineOffsets.Add(i + 1);
                }
            }
        }

        private string GetLine(int lineNumber)
        {
            if (_lineOffsets == null) BuildLineOffsets();
            int idx = lineNumber - 1;
            if (idx < 0 || idx >= _lineOffsets.Count) return "";
            int start = _lineOffsets[idx];
            int end = (idx + 1 < _lineOffsets.Count) ? _lineOffsets[idx + 1] : _source.Length;
            // 去掉行尾的换行符（可能为 \r\n 或 \n 或 \r）
            while (end > start && (_source[end - 1] == '\n' || _source[end - 1] == '\r')) end--;
            return _source.Substring(start, end - start);
        }

        /// <summary>
        /// 根据当前行号从行号映射中获取原始文件和行号信息
        /// </summary>
        /// <param name="currentLine">当前行号（预处理后的）</param>
        /// <returns>原始文件路径和行号</returns>
        private (string, int) GetOriginalFileAndLine(int currentLine)
        {
            if (lineMap != null && currentLine > 0 && currentLine <= lineMap.Count)
            {
                return lineMap[currentLine - 1];
            }
            return (null, 0);
        }

        private new char Peek(int offset = 0)
        {
            int p = _pos + offset;
            return p < _source.Length ? _source[p] : '\0';
        }

        private new char Advance()
        {
            if (_pos >= _source.Length) return '\0';
            char ch = _source[_pos++];
            if (ch == '\n') { _line++; _col = 1; } else _col++;
            return ch;
        }

        /// <summary>跳过空白字符（内联 Peek + char.IsWhiteSpace 减少调用开销）</summary>
        private new void SkipWhitespace()
        {
            int len = _source.Length;
            while (_pos < len && char.IsWhiteSpace(_source[_pos]))
            {
                if (_source[_pos] == '\n') { _line++; _col = 1; }
                else _col++;
                _pos++;
            }
        }

        private void SkipComment()
        {
            if (Peek() == '/' && Peek(1) == '/')
            {
                // 单行注释
                while (Peek() != '\n' && Peek() != '\0')
                {
                    Advance();
                }
            }
            else if (Peek() == '/' && Peek(1) == '*')
            {
                // 多行注释
                Advance(); // /
                Advance(); // *
                while (!(Peek() == '*' && Peek(1) == '/'))
                {
                    if (Peek() == '\0')
                    {
                        Error(ErrorCode.Lexer_UnterminatedComment, "未结束的注释");
                    }
                    Advance();
                }
                Advance(); // *
                Advance(); // /
            }
        }

        private Token ReadString(TokenType type = TokenType.STRING)
        {
            char quote = Advance(); // "
            int startLine = _line;
            int startCol = _col;
            var sb = new System.Text.StringBuilder();

            while (true)
            {
                char ch = Peek();
                if (ch == '\0')
                {
                    Error(ErrorCode.Lexer_UnterminatedString, "未结束的字符串");
                }
                if (ch == quote)
                {
                    Advance();
                    break;
                }

                // 转义字符
                if (ch == '\\')
                {
                    Advance(); // consume backslash
                    char escapeCh = Advance();
                    switch (escapeCh)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '0': sb.Append('\0'); break;
                        case 'a': sb.Append('\a'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'v': sb.Append('\v'); break;
                        case '\\': sb.Append('\\'); break;
                        case '\'': sb.Append('\''); break;
                        case '"': sb.Append('"'); break;
                        case '?': sb.Append('?'); break;
                        case 'x':
                            // 十六进制转义: \xNN
                            {
                                int hexStart = _pos;
                                while (char.IsDigit(Peek()) || (char.ToUpper(Peek()) >= 'A' && char.ToUpper(Peek()) <= 'F'))
                                    Advance();
                                int hexLen = _pos - hexStart;
                                if (hexLen == 0)
                                    Error(ErrorCode.Lexer_InvalidHexEscape, "无效的十六进制转义序列: \\x 后需要十六进制数字");
                                sb.Append((char)System.Convert.ToInt32(_source.Substring(hexStart, hexLen), 16));
                            }
                            break;
                        default:
                            if (escapeCh >= '0' && escapeCh <= '7')
                            {
                                // 八进制转义: \NNN (最多3位)
                                int octStart = _pos;
                                // escapeCh is first octal digit → already consumed; track _pos manually after reading more
                                int octCount = 1;
                                while (octCount < 3 && Peek() >= '0' && Peek() <= '7')
                                {
                                    Advance();
                                    octCount++;
                                }
                                // reconstruct: escapeCh + subsequent digits
                                int totalOctLen = 1 + (_pos - octStart);
                                char[] octChars = new char[totalOctLen];
                                octChars[0] = escapeCh;
                                if (totalOctLen > 1)
                                    _source.CopyTo(octStart, octChars, 1, totalOctLen - 1);
                                sb.Append((char)System.Convert.ToInt32(new string(octChars), 8));
                            }
                            else
                            {
                                sb.Append(escapeCh); // 未知转义序列，保留原字符
                            }
                            break;
                    }
                }
                else
                {
                    sb.Append(ch);
                    Advance();
                }
            }

            var (originalFile, originalLine) = GetOriginalFileAndLine(startLine);
            string sourceLine = GetLine(startLine);
            return new Token(type, sb.ToString(), startLine, startCol, sourceLine, originalFile, originalLine);
        }

        /// <summary>L'x' — wide character literal (16-bit wchar_t)</summary>
        private Token ReadCharAsWide()
        {
            int cp = ReadCharCore();
            return new Token(TokenType.WCHAR_LITERAL, ((int)cp).ToString(), _line, _col);
        }

        /// <summary>U'x' — unicode character literal (32-bit char32_t)</summary>
        private Token ReadCharAsUnicode()
        {
            int cp = ReadCharCore();
            return new Token(TokenType.UCHAR_LITERAL, ((int)cp).ToString(), _line, _col);
        }

        /// <summary>Core char literal parsing, returns the code point</summary>
        private int ReadCharCore()
        {
            Advance();  // opening quote
            if (_pos >= _source.Length)
                Error(ErrorCode.Lexer_EmptyCharLiteral, "空字符字面量");
            char first = Advance();
            int codePoint;
            if (first == '\\')
            {
                codePoint = ParseEscapeSequence();
            }
            else
            {
                codePoint = (int)first;
            }
            if (_pos >= _source.Length || _source[_pos] != '\'')
                Error(ErrorCode.Lexer_UnterminatedCharLiteral, "未终止的字符字面量");
            Advance();  // closing quote
            return codePoint;
        }

        /// 解析转义序列，返回 Unicode 码点
        private int ParseEscapeSequence()
        {
            char ch = Advance();
            switch (ch)
            {
                case 'n': return '\n';
                case 't': return '\t';
                case 'r': return '\r';
                case '0': return '\0';
                case 'a': return '\a';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'v': return '\v';
                case '\\': return '\\';
                case '\'': return '\'';
                case '"': return '"';
                case '?': return '?';
                case 'x':
                {
                    string hexVal = "";
                    while (char.IsDigit(Peek()) || (char.ToUpper(Peek()) >= 'A' && char.ToUpper(Peek()) <= 'F'))
                        hexVal += Advance();
                    if (hexVal.Length == 0)
                        Error(ErrorCode.Lexer_InvalidHexEscape, "无效的十六进制转义序列: \\x 后需要十六进制数字");
                    return System.Convert.ToInt32(hexVal, 16);
                }
                default:
                    if (char.IsDigit(ch))
                    {
                        string octVal = ch.ToString();
                        for (int i = 0; i < 2 && char.IsDigit(Peek()) && Peek() < '8'; i++)
                            octVal += Advance();
                        return System.Convert.ToInt32(octVal, 8);
                    }
                    return (int)ch;
            }
        }

        /// <summary>Read a character literal 'x' — returns the character as a string value</summary>
        private Token ReadChar()
        {
            char quote = Advance(); // '
            int startLine = _line;
            int startCol = _col;
            char value = '\0';

            // 检查是否为空字符
            if (Peek() == quote)
            {
                Error(ErrorCode.Lexer_EmptyCharLiteral, "空字符字面量");
            }

            // 转义字符
            if (Peek() == '\\')
            {
                Advance();
                char escapeCh = Advance();
                switch (escapeCh)
                {
                    case 'n': value = '\n'; break;
                    case 't': value = '\t'; break;
                    case 'r': value = '\r'; break;
                    case '0': value = '\0'; break;
                    case 'a': value = '\a'; break;
                    case 'b': value = '\b'; break;
                    case 'f': value = '\f'; break;
                    case 'v': value = '\v'; break;
                    case '\\': value = '\\'; break;
                    case '\'': value = '\''; break;
                    case '?': value = '?'; break;
                    case 'x':
                        // 十六进制转义: \xNN
                        {
                            string hexVal = "";
                            while (char.IsDigit(Peek()) || (char.ToUpper(Peek()) >= 'A' && char.ToUpper(Peek()) <= 'F'))
                                hexVal += Advance();
                            if (hexVal.Length == 0)
                                Error(ErrorCode.Lexer_InvalidHexEscape, "无效的十六进制转义序列: \\x 后需要十六进制数字");
                            value = (char)System.Convert.ToInt32(hexVal, 16);
                        }
                        break;
                    default:
                        if (escapeCh >= '0' && escapeCh <= '7')
                        {
                            // 八进制转义: \NNN (最多3位)
                            string octVal = escapeCh.ToString();
                            int octCount = 1;
                            while (octCount < 3 && Peek() >= '0' && Peek() <= '7')
                            {
                                octVal += Advance();
                                octCount++;
                            }
                            value = (char)System.Convert.ToInt32(octVal, 8);
                        }
                        else
                        {
                            value = escapeCh;
                        }
                        break;
                }
            }
            else
            {
                value = Advance();
            }

            // 检查结束引号
            if (Peek() != quote)
            {
                Error(ErrorCode.Lexer_CharLiteralTooLong, "字符字面量包含多个字符");
            }
            Advance();

            var (originalFile, originalLine) = GetOriginalFileAndLine(startLine);
            string sourceLine = GetLine(startLine);
            return new Token(TokenType.CHAR_LITERAL, value, startLine, startCol, sourceLine, originalFile, originalLine);
        }

        private Token ReadAsm()
        {
            var quote = Advance(); // `
            var startLine = _line;
            var startCol = _col;
            var value = "";

            while (true)
            {
                char ch = Peek();
                if (ch == '\0')
                {
                    Error("未结束的汇编指令");
                }
                if (ch == quote)
                {
                    Advance();
                    break;
                }

                value += ch;
                Advance();
            }

            var (originalFile, originalLine) = GetOriginalFileAndLine(startLine);
            string sourceLine = GetLine(startLine);
            return new Token(TokenType.ASM, value, startLine, startCol, sourceLine, originalFile, originalLine);
        }

        private new Token ReadNumber()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            // 处理十六进制
            if (Peek() == '0' && char.ToLower(Peek(1)) == 'x')
            {
                value += Advance(); // 0
                value += Advance(); // x/X
                while (char.IsDigit(Peek()) || (char.ToLower(Peek()) >= 'a' && char.ToLower(Peek()) <= 'f'))
                {
                    value += Advance();
                }
                // 处理十六进制数字的后缀
                string hexSuffix = "";
                while (char.IsLetter(Peek()))
                {
                    char suffixChar = char.ToUpper(Advance());
                    hexSuffix += suffixChar;
                    
                    // 检查后缀是否有效
                    if (suffixChar != 'L' && suffixChar != 'U')
                    {
                        throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidNumberSuffix, $"无效的十六进制常量后缀: '{suffixChar}'");
                    }
                }
                
                // 验证十六进制后缀组合的有效性
                ValidateSuffixCombination(hexSuffix);
                
                var (hexOriginalFile, hexOriginalLine) = GetOriginalFileAndLine(startLine);
                string hexSourceLine = GetLine(startLine);
                // ⚠ 两处硬伤（实测：`0xAAAAAAAAAAAAAAAAUL` / `0xFFFFFFFFUL` / `0xFFFFFFFFFFUL`
                //   等一律报 `Value was either too large or too small for a UInt64`，
                //   导致 `Lib/shared/src/bitops64.c` **整份编不出来**）：
                //   ① `value` 是**带 `0x` 前缀**的（上面拼进去的），而 .NET 的
                //      `Convert.To* (s, fromBase)` **不接受进制前缀** —— 必须先剥掉；
                //   ② 十六进制字面量**按有符号 `ToInt64` 解析** ——
                //      `0xAAAAAAAAAAAAAAAA` 这种"位模式"写法一超 int64 就抛，
                //      而 C 里十六进制常量本来就常用来表示**无符号位模式**
                //      ⇒ 一律按**无符号 64 位**解析，由后缀决定怎么装进 token。
                string hexOnly = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? value.Substring(2) : value;
                if (hexOnly.Length == 0 ||
                    !ulong.TryParse(hexOnly, System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out ulong u64))
                {
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidNumberSuffix,
                        $"无效的十六进制常量: '{value}{hexSuffix}'");
                }
                // token 的 Value 只承载 32 位（前端对 64 位常量的既有约定是保留低 32 位位模式），
                // 但**解析阶段不能因为超出 int64 就抛** —— 那是两个不同的问题。
                return new Token(TokenType.NUMBER, unchecked((int)(uint)u64),
                                 startLine, startCol, hexSourceLine, hexOriginalFile, hexOriginalLine);
            }

            // 处理八进制 (C标准: 前导0后跟0-7数字)
            if (Peek() == '0' && Peek(1) >= '0' && Peek(1) <= '7')
            {
                value += Advance(); // 前导 '0'
                while (Peek() >= '0' && Peek() <= '7')
                {
                    value += Advance();
                }
                // 检查是否有无效的八进制数字(8,9)
                if (char.IsDigit(Peek()))
                {
                    Error($"无效的八进制数字: '{Peek()}'");
                }
                // 处理后缀
                string octSuffix = "";
                while (char.IsLetter(Peek()))
                {
                    char suffixChar = char.ToUpper(Advance());
                    octSuffix += suffixChar;
                    if (suffixChar != 'L' && suffixChar != 'U')
                        throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidOctalDigit, $"无效的八进制常量后缀: '{suffixChar}'");
                }
                ValidateSuffixCombination(octSuffix);
                var (octFile, octLine) = GetOriginalFileAndLine(startLine);
                string octSourceLine = GetLine(startLine);
                try
                {
                    int octVal = System.Convert.ToInt32(value, 8);
                    if (octSuffix.Contains('U'))
                        return new Token(TokenType.NUMBER, unchecked((int)(uint)octVal), startLine, startCol, octSourceLine, octFile, octLine);
                    return new Token(TokenType.NUMBER, octVal, startLine, startCol, octSourceLine, octFile, octLine);
                }
                catch (OverflowException)
                {
                    return new Token(TokenType.NUMBER, double.Parse(value), startLine, startCol, octSourceLine, octFile, octLine);
                }
            }

            // 处理十进制
            while (char.IsDigit(Peek()))
            {
                value += Advance();
            }

            // 处理小数（简单支持）
            if (Peek() == '.' && char.IsDigit(Peek(1)))
            {
                value += Advance(); // .
                while (char.IsDigit(Peek()))
                {
                    value += Advance();
                }
                
                // 处理科学计数法
                if (char.ToUpper(Peek()) == 'E')
                {
                    value += Advance(); // E 或 e
                    
                    // 可选的符号
                    if (Peek() == '+' || Peek() == '-')
                    {
                        value += Advance();
                    }
                    
                    // 指数部分
                    while (char.IsDigit(Peek()))
                    {
                        value += Advance();
                    }
                }
                
                // 处理浮点数后缀
                string floatSuffix = "";
                while (char.IsLetter(Peek()))
                {
                    char suffixChar = char.ToUpper(Advance());
                    floatSuffix += suffixChar;
                    
                    // 检查后缀是否有效（浮点数只支持F后缀）
                    if (suffixChar != 'F' && suffixChar != 'L')
                    {
                        throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidFloatSuffix, $"无效的浮点数常量后缀: '{suffixChar}'");
                    }
                }
                
                var (decimalOriginalFile, decimalOriginalLine) = GetOriginalFileAndLine(startLine);
                string decimalSourceLine = GetLine(startLine);
                // 根据后缀决定数值类型: F后缀→float, 否则→double
                object numberValue = floatSuffix.Contains('F') ? float.Parse(value) : double.Parse(value);
                return new Token(TokenType.NUMBER, numberValue, startLine, startCol, decimalSourceLine, decimalOriginalFile, decimalOriginalLine);
            }

            // 处理科学计数法（整数也可能有科学计数法，虽然不常见）
            if (char.ToUpper(Peek()) == 'E')
            {
                value += Advance(); // E 或 e
                
                // 可选的符号
                if (Peek() == '+' || Peek() == '-')
                {
                    value += Advance();
                }
                
                // 指数部分
                while (char.IsDigit(Peek()))
                {
                    value += Advance();
                }
                
                // 科学计数法后的后缀
                string sciSuffix = "";
                while (char.IsLetter(Peek()))
                {
                    char suffixChar = char.ToUpper(Advance());
                    sciSuffix += suffixChar;
                    
                    // 检查后缀是否有效（科学计数法后只支持F后缀）
                    if (suffixChar != 'F')
                    {
                        throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidSciSuffix, $"无效的科学计数法常量后缀: '{suffixChar}'");
                    }
                }
                
                var (sciOriginalFile, sciOriginalLine) = GetOriginalFileAndLine(startLine);
                string sciSourceLine = GetLine(startLine);
                object sciNumberValue = sciSuffix.Contains('F') ? float.Parse(value) : double.Parse(value);
                return new Token(TokenType.NUMBER, sciNumberValue, startLine, startCol, sciSourceLine, sciOriginalFile, sciOriginalLine);
            }
            
            // 处理常量后缀
            string suffix = "";
            while (char.IsLetter(Peek()))
            {
                char suffixChar = char.ToUpper(Advance());
                suffix += suffixChar;
                
                // 检查后缀是否有效
                if (suffixChar != 'L' && suffixChar != 'U' && suffixChar != 'F')
                {
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidNumberSuffix, $"无效的常量后缀: '{suffixChar}'");
                }
            }
            
            // 验证后缀组合的有效性
            ValidateSuffixCombination(suffix);
            
            var (intOriginalFile, intOriginalLine) = GetOriginalFileAndLine(startLine);
            string intSourceLine = GetLine(startLine);
            
            // 根据后缀确定常量类型
            try
            {
                if (suffix.Contains('F'))
                {
                    // 浮点常量
                    return new Token(TokenType.NUMBER, double.Parse(value), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
                }
                else if (suffix.Contains('U') && suffix.Contains('L'))
                {
                    // UL/ULL: 64位无符号整数 → 转为有符号 long 保持位模式
                    ulong unsignedLongValue = ulong.Parse(value);
                    return new Token(TokenType.NUMBER, unchecked((long)unsignedLongValue), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
                }
                else if (suffix.Contains('U'))
                {
                    // 无符号整数常量 → 转为有符号 int32 保持位模式
                    uint unsignedValue = uint.Parse(value);
                    return new Token(TokenType.NUMBER, unchecked((int)unsignedValue), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
                }
                else if (suffix.Contains('L'))
                {
                    // 长整型常量 → int32 (MCU模式)
                    return new Token(TokenType.NUMBER, int.Parse(value), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
                }
                else
                {
                    // 普通整数常量
                    return new Token(TokenType.NUMBER, int.Parse(value), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
                }
            }
            catch (OverflowException)
            {
                return new Token(TokenType.NUMBER, double.Parse(value), startLine, startCol, intSourceLine, intOriginalFile, intOriginalLine);
            }
        }

        /// <summary>
        /// 验证常量后缀组合的有效性
        /// </summary>
        /// <param name="suffix">后缀字符串</param>
        private void ValidateSuffixCombination(string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
                return;
                
            // 检查后缀中是否有不允许的重复字符 (LL 是合法的 long long 后缀)
            var counts = new Dictionary<char, int>();
            foreach (char c in suffix)
            {
                counts.TryGetValue(c, out int n);
                counts[c] = n + 1;
            }
            foreach (var kv in counts)
            {
                // LL (long long) 和 ULL 合法, LLL 不合法
                if (kv.Value > 2 || (kv.Key != 'L' && kv.Value > 1))
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidSuffixCombination, $"无效的常量后缀组合: '{suffix}'（重复字符 '{kv.Key}'）");
            }
            
            // 检查后缀组合是否有效
            // 有效组合: U, L, UL, LU, F
            // 无效组合: UF, LF, ULF, LUF 等
            if (suffix.Contains('F'))
            {
                // 浮点数后缀只能单独使用F，或者与L组合（long double）
                if (suffix.Length > 1 && !(suffix.Length == 2 && suffix.Contains('L')))
                {
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidFloatSuffix, $"无效的浮点数常量后缀: '{suffix}'（F只能单独使用或与L组合）");
                }
            }
            
            // 检查U和L的组合顺序
            if (suffix.Contains('U') && suffix.Contains('L'))
            {
                // UL, LU, ULL, LLU 都有效 (long long)
                if (suffix != "UL" && suffix != "LU" && suffix != "ULL" && suffix != "LLU")
                {
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_InvalidNumberSuffix, $"无效的整数常量后缀: '{suffix}'");
                }
            }
        }

        private new Token ReadIdentifier()
        {
            int startLine = _line;
            int startCol = _col;
            int idStart = _pos; // 记录标识符起始位置

            while (char.IsLetterOrDigit(Peek()) || Peek() == '_' || IsChineseChar(Peek()))
            {
                Advance();
            }

            string value = _source.Substring(idStart, _pos - idStart); // 一次性提取

            // 检查是否为关键字
            TokenType tokenType = KEYWORDS.TryGetValue(value, out var kw) ? kw : TokenType.IDENTIFIER;
            var (identOriginalFile, identOriginalLine) = GetOriginalFileAndLine(startLine);
            string identSourceLine = GetLine(startLine);
            return new Token(tokenType, value, startLine, startCol, identSourceLine, identOriginalFile, identOriginalLine);
        }

        /// <summary>
        /// 检查字符是否为汉字（扩展 Unicode 汉字范围）
        /// </summary>
        /// <param name="ch">要检查的字符</param>
        /// <returns>如果字符为汉字（扩展 Unicode 汉字范围），则返回 true；否则返回 false</returns>

        /// <summary>
        /// 扫描下一个令牌
        /// </summary>
        /// <returns>下一个令牌</returns>
        private Token ScanToken()
        {
            while (true)
            {
                SkipWhitespace();
                // 处理注释
                if (Peek() == '/' && (Peek(1) == '/' || Peek(1) == '*'))
                {
                    SkipComment();
                }
                // 处理预处理器指令
                else if (Peek() == '#')
                {
                    // 处理预处理器指令
                    return ReadPreprocessorDirective();
                }
                else
                {
                    break;
                }
            }

            int startLine = _line;
            int startCol = _col;
            char ch = Peek();

            if (ch == '\0')
            {
                var (eofOriginalFile, eofOriginalLine) = GetOriginalFileAndLine(startLine);
                string eofSourceLine = GetLine(startLine);
                return new Token(TokenType.EOF, null, startLine, startCol, eofSourceLine, eofOriginalFile, eofOriginalLine);
            }

            // 字符串和字符字面量 (含 L / U 前缀)
            if (ch == 'L' && _pos + 1 < _source.Length && _source[_pos + 1] == '"')
            {
                // L"..."  wide string (16-bit)
                _pos++; // consume L
                return ReadString(TokenType.WSTRING);
            }
            else if (ch == 'U' && _pos + 1 < _source.Length && _source[_pos + 1] == '"')
            {
                // U"..."  unicode string (32-bit)
                _pos++; // consume U
                return ReadString(TokenType.USTRING);
            }
            else if (ch == 'u' && _pos + 1 < _source.Length && _source[_pos + 1] == '"')
            {
                // u"..."  UTF-16 string (C11), alias for L"..."
                _pos++; // consume u
                return ReadString(TokenType.WSTRING);
            }
            else if (ch == 'L' && _pos + 1 < _source.Length && _source[_pos + 1] == '\'')
            {
                // L'x'  wide char
                _pos++; // consume L
                return ReadCharAsWide();
            }
            else if (ch == 'U' && _pos + 1 < _source.Length && _source[_pos + 1] == '\'')
            {
                // U'x'  unicode char
                _pos++; // consume U
                return ReadCharAsUnicode();
            }
            else if (ch == 'u' && _pos + 1 < _source.Length && _source[_pos + 1] == '\'')
            {
                // u'x'  UTF-16 char (C11)
                _pos++; // consume u
                return ReadCharAsUnicode();
            }
            else if (ch == '"')
            {
                return ReadString();
            }
            else if (ch == '\'')
            {
                return ReadChar();
            }
            // 汇编指令
            else if (ch == '`')
            {
                return ReadAsm();
            }

            // 数字
            if (char.IsDigit(ch))
            {
                return ReadNumber();
            }

            // 标识符或关键字
            if (char.IsLetter(ch) || ch == '_' || IsChineseChar(ch))
            {
                return ReadIdentifier();
            }

            // 运算符和分隔符
            Advance();

            // 双字符运算符
            var nextCh = Peek();

            var (originalFile, originalLine) = GetOriginalFileAndLine(startLine);
            var sourceLine = GetLine(startLine);

            switch (ch)
            {
                case '+' when nextCh == '+':
                    Advance();
                    return new Token(TokenType.INCREMENT, "++", startLine, startCol, sourceLine, originalFile, originalLine);
                case '+' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.ADD_ASSIGN, "+", startLine, startCol, sourceLine, originalFile, originalLine);
                case '-' when nextCh == '-':
                    Advance();
                    return new Token(TokenType.DECREMENT, "--", startLine, startCol, sourceLine, originalFile, originalLine);
                case '-' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.SUB_ASSIGN, "-", startLine, startCol, sourceLine, originalFile, originalLine);
                case '*' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.MUL_ASSIGN, "*=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '/' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.DIV_ASSIGN, "/=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '%' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.MOD_ASSIGN, "%=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '=' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.EQ, "==", startLine, startCol, sourceLine, originalFile, originalLine);
                case '!' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.NE, "!=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '<' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.LE, "<=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '<' when nextCh == '<':
                {
                    Advance();
                    if (Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenType.LSHIFT_ASSIGN, "<<=", startLine, startCol, sourceLine, originalFile, originalLine);
                    }
                    return new Token(TokenType.LSHIFT, "<<", startLine, startCol, sourceLine, originalFile, originalLine);
                }
                case '>' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.GE, ">=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '>' when nextCh == '>':
                {
                    Advance();
                    if (Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenType.RSHIFT_ASSIGN, ">>=", startLine, startCol, sourceLine, originalFile, originalLine);
                    }
                    return new Token(TokenType.RSHIFT, ">>", startLine, startCol, sourceLine, originalFile, originalLine);
                }
                case '&' when nextCh == '&':
                    Advance();
                    return new Token(TokenType.AND, "&&", startLine, startCol, sourceLine, originalFile, originalLine);
                case '&' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.AND_ASSIGN, "&=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '|' when nextCh == '|':
                    Advance();
                    return new Token(TokenType.OR, "||", startLine, startCol, sourceLine, originalFile, originalLine);
                case '|' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.OR_ASSIGN, "|=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '^' when nextCh == '=':
                    Advance();
                    return new Token(TokenType.XOR_ASSIGN, "^=", startLine, startCol, sourceLine, originalFile, originalLine);
                case '-' when nextCh == '>':
                    Advance();
                    return new Token(TokenType.ARROW, "->", startLine, startCol, sourceLine, originalFile, originalLine);
                case '.' when nextCh == '.':
                {
                    Advance();
                    if (Peek() == '.')
                    {
                        Advance();
                        return new Token(TokenType.ELLIPSIS, "...", startLine, startCol, sourceLine, originalFile, originalLine);
                    }
                    else
                    {
                        return new Token(TokenType.DOT, ".", startLine, startCol, sourceLine, originalFile, originalLine);
                    }
                }
            }

            // 单字符
            if (SINGLE_CHAR_TOKENS.TryGetValue(ch, out var singleTokenType))
            {
                return new Token(singleTokenType, ch, startLine, startCol, sourceLine, originalFile, originalLine);
            }
            Error($"未知字符：{ch}");
            return null;
        }

        /// <summary>
        /// 读取预处理器指令
        /// </summary>
        /// <returns>预处理器指令令牌</returns>
        private Token ReadPreprocessorDirective()
        {
            int startLine = _line;
            int startCol = _col;
            Advance(); // 跳过 #

            // 读取指令名（记录起始/结束位置，避免 += 字符拼接）
            int dirStart = _pos;
            while (char.IsLetter(Peek())) Advance();
            string directive = _source.Substring(dirStart, _pos - dirStart);

            // 处理指令
            switch (directive.ToLower())
            {
                case "include":
                    return new Token(TokenType.INCLUDE, directive, startLine, startCol);
                case "define":
                    return new Token(TokenType.DEFINE, directive, startLine, startCol);
                case "if":
                    return new Token(TokenType.IF_PRE, directive, startLine, startCol);
                case "else":
                    return new Token(TokenType.ELSE_PRE, directive, startLine, startCol);
                case "endif":
                    return new Token(TokenType.ENDIF, directive, startLine, startCol);
                case "ifdef":
                    return new Token(TokenType.IFDEF, directive, startLine, startCol);
                case "ifndef":
                    return new Token(TokenType.IFNDEF, directive, startLine, startCol);
                default:
                    Error($"未知的预处理器指令：{directive}");
                    return null;
            }
        }

        /// <summary>启用 --dump-progress 时每 N 个 token 输出一次进度</summary>
        public bool DumpMode = false;

        public List<Token> Tokenize()
        {
            Tokens.Clear();
            if (DumpMode)
                Console.Error.WriteLine($"[Lexer] 开始词法分析, 源长度={_source.Length} 字符, DumpMode={DumpMode}");
            int srcLen = _source.Length;
            if (srcLen == 0)
            {
                Tokens.Add(new Token(TokenType.EOF, null));
                return Tokens;
            }
            int maxTokens = _source.Length * 2 + 1000; // safety: 2 tokens per char + margin
            int safetyCounter = 0;
            int reportInterval = DumpMode ? srcLen / 1000 : srcLen / 50; // DumpMode: fine, normal: every 2%
            if (reportInterval < 1) reportInterval = 1;
            int nextReportAt = reportInterval;
            int lastReportedPct = -1;
            while (true)
            {
                if (++safetyCounter > maxTokens)
                    throw new CompilerBase.ParseException(ErrorCode.Lexer_TooManyTokens, $"Lexer: 超过最大 token 数 ({maxTokens}), 源文件可能包含无限循环结构");
                var token = ScanToken();
                Tokens.Add(token);
                if (safetyCounter >= nextReportAt)
                {
                    int pct = (int)((long)_pos * 100 / srcLen);
                    if (DumpMode && pct >= lastReportedPct + 2)
                    {
                        System.Console.Error.WriteLine($"percent:{pct}%, line:{token.Line}, total tokens:{safetyCounter}.");
                        lastReportedPct = pct;
                    }
                    nextReportAt = safetyCounter + reportInterval;
                }
                if (token.Type == TokenType.EOF) break;
            }
            MergeAdjacentStrings();
            return Tokens;
        }

        /// <summary>
        /// C99: 相邻字符串字面量自动拼接 ("a" "b" → "ab")
        /// </summary>
        private void MergeAdjacentStrings()
        {
            for (int i = 0; i < Tokens.Count - 1; i++)
            {
                // Same-type string merge: byte-string / wide-string / unicode-string
                TokenType t = Tokens[i].Type;
                if ((t == TokenType.STRING || t == TokenType.WSTRING || t == TokenType.USTRING) &&
                    Tokens[i + 1].Type == t)
                {
                    string merged = (string)Tokens[i].Value + (string)Tokens[i + 1].Value;
                    Tokens[i] = new Token(t, merged, Tokens[i].Line, Tokens[i].Column,
                        Tokens[i].SourceLine, Tokens[i].OriginalFile, Tokens[i].OriginalLine);
                    Tokens.RemoveAt(i + 1);
                    i--;
                }
            }
        }
    }
}