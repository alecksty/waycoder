using System.Collections.Generic;
using CompilerBase;

namespace GoCompiler
{
    /// <summary>
    /// Go语言词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        public List<Token> Tokens { get; private set; }
        private List<int> _lineOffsets; // 行偏移索引（O(1) GetLine）

        // 关键字映射
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            // 包和导入
            { "package", TokenType.PACKAGE },
            { "import", TokenType.IMPORT },

            // 声明
            { "func", TokenType.FUNC },
            { "var", TokenType.VAR },
            { "const", TokenType.CONST },
            { "type", TokenType.TYPE },

            // 类型
            { "struct", TokenType.STRUCT },
            { "interface", TokenType.INTERFACE },
            { "map", TokenType.MAP },
            { "chan", TokenType.CHAN },

            // 流程控制
            { "go", TokenType.GO },
            { "defer", TokenType.DEFER },
            { "select", TokenType.SELECT },
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "for", TokenType.FOR },
            { "range", TokenType.RANGE },
            { "switch", TokenType.SWITCH },
            { "case", TokenType.CASE },
            { "default", TokenType.DEFAULT },
            { "break", TokenType.BREAK },
            { "continue", TokenType.CONTINUE },
            { "return", TokenType.RETURN },
            { "fallthrough", TokenType.FALLTHROUGH },
            { "goto", TokenType.GOTO },

            // 基础类型
            { "int", TokenType.INT },
            { "int8", TokenType.INT8 },
            { "int16", TokenType.INT16 },
            { "int32", TokenType.INT32 },
            { "int64", TokenType.INT64 },
            { "uint", TokenType.UINT },
            { "uint8", TokenType.UINT8 },
            { "uint16", TokenType.UINT16 },
            { "uint32", TokenType.UINT32 },
            { "uint64", TokenType.UINT64 },
            { "uintptr", TokenType.UINTPTR },
            { "float32", TokenType.FLOAT32 },
            { "float64", TokenType.FLOAT64 },
            { "complex64", TokenType.COMPLEX64 },
            { "complex128", TokenType.COMPLEX128 },
            { "bool", TokenType.BOOL },
            { "string", TokenType.STRING },
            { "byte", TokenType.BYTE },
            { "rune", TokenType.RUNE },
            { "error", TokenType.ERROR },
        };

        public Lexer(string _source) : base(_source)
        {
            Tokens = new List<Token>();
        }




        private Token ReadLineComment()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            Advance(); // /
            Advance(); // /

            while (Peek() != '\n' && Peek() != '\0')
            {
                value += Advance();
            }

            return new Token(TokenType.COMMENT, value, startLine, startCol);
        }

        private Token ReadBlockComment()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            Advance(); // /
            Advance(); // *

            while (!(Peek() == '*' && Peek(1) == '/'))
            {
                if (Peek() == '\0')
                {
                    Error("未结束的块注释");
                }
                value += Advance();
            }

            Advance(); // *
            Advance(); // /

            return new Token(TokenType.COMMENT, value, startLine, startCol);
        }

        private Token ReadRawString()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            char quote = Advance(); // `

            while (Peek() != '`' && Peek() != '\0')
            {
                value += Advance();
            }

            if (Peek() == '\0')
            {
                Error("未结束的原始字符串");
            }

            Advance(); // `

            return new Token(TokenType.RAW_STRING, value, startLine, startCol);
        }

        private Token ReadInterpretedString()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            char quote = Advance(); // "

            while (Peek() != '"' && Peek() != '\0')
            {
                char ch = Peek();
                if (ch == '\\')
                {
                    Advance();
                    char escapeCh = Advance();
                    switch (escapeCh)
                    {
                        case 'n':
                            value += '\n';
                            break;
                        case 't':
                            value += '\t';
                            break;
                        case 'r':
                            value += '\r';
                            break;
                        case '\\':
                            value += '\\';
                            break;
                        case '"':
                            value += '"';
                            break;
                        case 'u':
                            value += "\\u";
                            break;
                        default:
                            value += escapeCh;
                            break;
                    }
                }
                else
                {
                    value += Advance();
                }
            }

            if (Peek() == '\0')
            {
                Error("未结束的字符串");
            }

            Advance(); // "

            return new Token(TokenType.INTERPRETED_STRING, value, startLine, startCol);
        }

        private Token ReadChar()
        {
            int startLine = _line;
            int startCol = _col;
            string value = "";

            Advance(); // '
            while (Peek() != '\'' && Peek() != '\0')
            {
                if (Peek() == '\\')
                {
                    Advance();
                    char escapeCh = Advance();
                    value += escapeCh switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '\'' => '\'',
                        _ => escapeCh
                    };
                }
                else
                {
                    value += Advance();
                }
            }

            if (Peek() == '\0')
            {
                Error("未结束的字符字面量");
            }

            Advance(); // '
            return new Token(TokenType.CHAR, value, startLine, startCol);
        }

        private new Token ReadNumber()
        {
            int startLine = _line;
            int startCol = _col;
            int numStart = _pos;

            // 处理十六进制
            if (Peek() == '0' && (char.ToLower(Peek(1)) == 'x' || char.ToLower(Peek(1)) == 'o' || char.ToLower(Peek(1)) == 'b'))
            {
                Advance(); Advance(); // 0 + x/o/b
                while (char.IsDigit(Peek()) || (char.ToLower(Peek()) >= 'a' && char.ToLower(Peek()) <= 'f'))
                    Advance();
                return new Token(TokenType.NUMBER, _source.Substring(numStart, _pos - numStart), startLine, startCol);
            }

            // 处理八进制
            if (Peek() == '0')
            {
                Advance();
                while (Peek() >= '0' && Peek() <= '7')
                    Advance();
                if (char.IsDigit(Peek()))
                {
                    while (char.IsDigit(Peek())) Advance();
                    Error($"无效的八进制数: {_source.Substring(numStart, _pos - numStart)}");
                }
                return new Token(TokenType.NUMBER, _source.Substring(numStart, _pos - numStart), startLine, startCol);
            }

            // 处理十进制
            while (char.IsDigit(Peek())) Advance();

            // 处理小数
            if (Peek() == '.' && char.IsDigit(Peek(1)))
            {
                Advance(); // .
                while (char.IsDigit(Peek())) Advance();
            }

            // 处理指数
            if (char.ToLower(Peek()) == 'e')
            {
                Advance(); // e
                if (Peek() == '+' || Peek() == '-') Advance();
                while (char.IsDigit(Peek())) Advance();
            }

            // 处理虚数后缀
            if (char.ToLower(Peek()) == 'i') Advance();

            return new Token(TokenType.NUMBER, _source.Substring(numStart, _pos - numStart), startLine, startCol);
        }



        private new Token ReadIdentifier()
        {
            int startLine = _line;
            int startCol = _col;
            int idStart = _pos;

            while (char.IsLetter(Peek()) || IsChineseChar(Peek()) || char.IsDigit(Peek()) || Peek() == '_')
            {
                Advance();
            }

            string value = _source.Substring(idStart, _pos - idStart);

            // 检查关键字
            TokenType tokenType = KEYWORDS.TryGetValue(value, out var kw) ? kw : TokenType.IDENTIFIER;
            return new Token(tokenType, value, startLine, startCol);
        }

        private new void Error(string message)
        {
            var errorLine = GetLine(_line);
            var arrow = new string(' ', _col - 1) + "^";
            throw new ParseException(ErrorCode.Lexer_UnknownCharacter, message);
        }

        /// <summary>
        /// 一次性扫描 _source 建立行起始偏移索引，后续 GetLine 即可 O(1)
        /// </summary>
        private void BuildLineOffsets()
        {
            _lineOffsets = new List<int>(_source.Length / 20) { 0 };
            for (int i = 0; i < _source.Length; i++)
            {
                char c = _source[i];
                if (c == '\n')
                {
                    if (i + 1 < _source.Length) _lineOffsets.Add(i + 1);
                }
                else if (c == '\r')
                {
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
            while (end > start && (_source[end - 1] == '\n' || _source[end - 1] == '\r')) end--;
            return _source.Substring(start, end - start);
        }

        private Token ScanToken()
        {
            SkipWhitespace();

            int startLine = _line;
            int startCol = _col;
            char ch = Peek();

            if (ch == '\0')
            {
                return new Token(TokenType.EOF, null, startLine, startCol);
            }

            // 注释
            if (ch == '/' && (Peek(1) == '/' || Peek(1) == '*'))
            {
                if (Peek(1) == '/')
                {
                    return ReadLineComment();
                }
                else
                {
                    return ReadBlockComment();
                }
            }

            // 字符串
            if (ch == '`')
            {
                return ReadRawString();
            }
            if (ch == '"')
            {
                return ReadInterpretedString();
            }
            if (ch == '\'')
            {
                return ReadChar();
            }

            // 数字
            if (char.IsDigit(ch))
            {
                return ReadNumber();
            }

            // 标识符
            if (char.IsLetter(ch) || IsChineseChar(ch) || ch == '_')
            {
                return ReadIdentifier();
            }

            // 换行
            if (ch == '\n')
            {
                Advance();
                return new Token(TokenType.NEWLINE, "\n", startLine, startCol);
            }

            // 运算符和分隔符
            Advance();

            switch (ch)
            {
                case '+' when Peek() == '+':
                    Advance();
                    return new Token(TokenType.INCREMENT, "++", startLine, startCol);
                case '+' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.ADD_ASSIGN, "+=", startLine, startCol);
                case '-' when Peek() == '-':
                    Advance();
                    return new Token(TokenType.DECREMENT, "--", startLine, startCol);
                case '-' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.SUB_ASSIGN, "-=", startLine, startCol);
                case '-' when Peek() == '>':
                    Advance();
                    return new Token(TokenType.ARROW, "->", startLine, startCol);
                case '*' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.MUL_ASSIGN, "*=", startLine, startCol);
                case '/' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.DIV_ASSIGN, "/=", startLine, startCol);
                case '%' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.MOD_ASSIGN, "%=", startLine, startCol);
                case '=' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.EQ, "==", startLine, startCol);
                case '=':
                    return new Token(TokenType.ASSIGN, "=", startLine, startCol);
                case '!' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.NE, "!=", startLine, startCol);
                case '!':
                    return new Token(TokenType.NOT, "!", startLine, startCol);
                case '<' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.LE, "<=", startLine, startCol);
                case '<' when Peek() == '<':
                    Advance();
                    if (Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenType.LSHIFT_ASSIGN, "<<=", startLine, startCol);
                    }
                    if (Peek() == '&')
                    {
                        Advance();
                        return new Token(TokenType.AND_NOT_ASSIGN, "&^=", startLine, startCol);
                    }
                    return new Token(TokenType.LSHIFT, "<<", startLine, startCol);
                case '<' when Peek() == '-':
                    Advance();
                    return new Token(TokenType.ARROW, "<-", startLine, startCol);
                case '>' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.GE, ">=", startLine, startCol);
                case '>' when Peek() == '>':
                    Advance();
                    if (Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenType.RSHIFT_ASSIGN, ">>=", startLine, startCol);
                    }
                    return new Token(TokenType.RSHIFT, ">>", startLine, startCol);
                case '&' when Peek() == '&':
                    Advance();
                    return new Token(TokenType.AND, "&&", startLine, startCol);
                case '&' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.AND_ASSIGN, "&=", startLine, startCol);
                case '&' when Peek() == '^':
                    Advance();
                    return new Token(TokenType.AND_NOT, "&^", startLine, startCol);
                case '|' when Peek() == '|':
                    Advance();
                    return new Token(TokenType.OR, "||", startLine, startCol);
                case '|' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.OR_ASSIGN, "|=", startLine, startCol);
                case '^' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.XOR_ASSIGN, "^=", startLine, startCol);
                case ':' when Peek() == '=':
                    Advance();
                    return new Token(TokenType.COLON_ASSIGN, ":=", startLine, startCol);
                case '.' when Peek() == '.':
                    Advance();
                    if (Peek() == '.')
                    {
                        Advance();
                        return new Token(TokenType.ELLIPSIS, "...", startLine, startCol);
                    }
                    return new Token(TokenType.PERIOD, ".", startLine, startCol);
                case '[':
                    return new Token(TokenType.LBRACKET, "[", startLine, startCol);
                case ']':
                    return new Token(TokenType.RBRACKET, "]", startLine, startCol);
                case '(':
                    return new Token(TokenType.LPAREN, "(", startLine, startCol);
                case ')':
                    return new Token(TokenType.RPAREN, ")", startLine, startCol);
                case '{':
                    return new Token(TokenType.LBRACE, "{", startLine, startCol);
                case '}':
                    return new Token(TokenType.RBRACE, "}", startLine, startCol);
                case ',':
                    return new Token(TokenType.COMMA, ",", startLine, startCol);
                case '.':
                    return new Token(TokenType.PERIOD, ".", startLine, startCol);
                case ';':
                    return new Token(TokenType.SEMICOLON, ";", startLine, startCol);
                case ':':
                    return new Token(TokenType.COLON, ":", startLine, startCol);
                case '+':
                    return new Token(TokenType.PLUS, "+", startLine, startCol);
                case '-':
                    return new Token(TokenType.MINUS, "-", startLine, startCol);
                case '*':
                    return new Token(TokenType.STAR, "*", startLine, startCol);
                case '/':
                    return new Token(TokenType.SLASH, "/", startLine, startCol);
                case '%':
                    return new Token(TokenType.PERCENT, "%", startLine, startCol);
                case '&':
                    return new Token(TokenType.AMPERSAND, "&", startLine, startCol);
                case '|':
                    return new Token(TokenType.PIPE, "|", startLine, startCol);
                case '^':
                    return new Token(TokenType.CARET, "^", startLine, startCol);
                case '<':
                    return new Token(TokenType.LT, "<", startLine, startCol);
                case '>':
                    return new Token(TokenType.GT, ">", startLine, startCol);
                case '\\':
                    // Handle source-level escape sequences for inline code (v1.66.53)
                    // \n → newline; skip backslash+char pair and continue scanning
                    {
                        char esc = Advance();
                        if (esc == 'n') { _line++; _col = 0; }
                        return ScanToken();
                    }
            }

            Error($"未知字符: {ch}");
            return null;
        }

        public List<Token> Tokenize()
        {
            Tokens.Clear();
            while (true)
            {
                var token = ScanToken();
                Tokens.Add(token);
                if (token.Type == TokenType.EOF)
                {
                    break;
                }
            }
            return Tokens;
        }
    }
}
