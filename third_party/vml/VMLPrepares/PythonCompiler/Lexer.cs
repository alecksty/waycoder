using System.Collections.Generic;
using CompilerBase;

namespace PythonCompiler
{
    /// <summary>
    /// Python 词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        public List<Token> Tokens { get; private set; }

        // Python 关键字（case-insensitive 检测后转换）
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            { "False",  TokenType.FALSE },
            { "None",   TokenType.NONE },
            { "True",   TokenType.TRUE },
            { "and",    TokenType.AND },
            { "as",     TokenType.AS },
            { "assert", TokenType.ASSERT },
            { "async",  TokenType.ASYNC },
            { "await",  TokenType.AWAIT },
            { "break",  TokenType.BREAK },
            { "class",  TokenType.CLASS },
            { "continue", TokenType.CONTINUE },
            { "def",    TokenType.DEF },
            { "del",    TokenType.DEL },
            { "elif",   TokenType.ELIF },
            { "else",   TokenType.ELSE },
            { "except", TokenType.EXCEPT },
            { "finally", TokenType.FINALLY },
            { "for",    TokenType.FOR },
            { "from",   TokenType.FROM },
            { "global", TokenType.GLOBAL },
            { "if",     TokenType.IF },
            { "import", TokenType.IMPORT },
            { "in",     TokenType.IN },
            { "is",     TokenType.IS },
            { "lambda", TokenType.LAMBDA },
            { "match",  TokenType.MATCH },
            { "case",   TokenType.CASE },
            { "nonlocal", TokenType.NONLOCAL },
            { "not",    TokenType.NOT },
            { "or",     TokenType.OR },
            { "pass",   TokenType.PASS },
            { "raise",  TokenType.RAISE },
            { "return", TokenType.RETURN },
            { "try",    TokenType.TRY },
            { "while",  TokenType.WHILE },
            { "with",   TokenType.WITH },
            { "yield",  TokenType.YIELD }
        };

        public Lexer(string source) : base(source)
        {
            Tokens = new List<Token>();
        }

        private new void Error(string message)
        {
            throw new ParseException(ErrorCode.Lexer_UnknownCharacter, message);
        }




        private void SkipComment()
        {
            if (Peek() == '#')
            {
                while (Peek() != '\n' && Peek() != '\0')
                {
                    Advance();
                }
            }
        }

        private Token ReadString(char quote)
        {
            int startLine = _line;
            int startCol = _col;
            Advance(); // 跳过开始引号
            
            var sb = new System.Text.StringBuilder();
            while (Peek() != quote && Peek() != '\0')
            {
                if (Peek() == '\\')
                {
                    Advance();
                    char escape = Advance();
                    sb.Append(escape switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        _ => escape
                    });
                }
                else
                {
                    sb.Append(Advance());
                }
            }
            
            if (Peek() != quote)
            {
                Error("未终止的字符串");
            }
            Advance(); // 跳过结束引号
            
            return new Token(TokenType.STRING, sb.ToString(), startLine, startCol);
        }

        private Token ReadFString(char quote)
        {
            int startLine = _line;
            int startCol = _col;
            Advance(); // 跳过开始引号
            
            var sb = new System.Text.StringBuilder();
            while (Peek() != quote && Peek() != '\0')
            {
                if (Peek() == '\\')
                {
                    Advance();
                    char escape = Advance();
                    sb.Append(escape switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        _ => escape
                    });
                }
                else if (Peek() == '{')
                {
                    // f-string表达式开始
                    Advance();
                    var exprBuilder = new System.Text.StringBuilder();
                    while (Peek() != '}' && Peek() != '\0')
                    {
                        exprBuilder.Append(Advance());
                    }
                    if (Peek() != '}')
                    {
                        Error("未终止的f-string表达式");
                    }
                    Advance(); // 跳过}
                    
                    // 将表达式作为特殊标记添加到字符串中
                    sb.Append($"{{{exprBuilder}}}");
                }
                else
                {
                    sb.Append(Advance());
                }
            }
            
            if (Peek() != quote)
            {
                Error("未终止的f-string");
            }
            Advance(); // 跳过结束引号
            
            return new Token(TokenType.FSTRING, sb.ToString(), startLine, startCol);
        }

        private new Token ReadNumber()
        {
            int startLine = _line;
            int startCol = _col;
            var sb = new System.Text.StringBuilder();
            bool isFloat = false;
            
            // 前缀检测：0x/0o/0b
            if (Peek() == '0' && (Peek(1) == 'x' || Peek(1) == 'X' ||
                                   Peek(1) == 'o' || Peek(1) == 'O' ||
                                   Peek(1) == 'b' || Peek(1) == 'B'))
            {
                char prefix = char.ToLower(Advance()); // 0
                char baseChar = char.ToLower(Advance()); // x/o/b
                sb.Append(prefix);
                sb.Append(baseChar);
                
                while ((baseChar == 'x' && LexerHelper.IsHexDigit(Peek())) ||
                       (baseChar == 'o' && LexerHelper.IsOctalDigit(Peek())) ||
                       (baseChar == 'b' && LexerHelper.IsBinaryDigit(Peek())) ||
                       Peek() == '_')
                {
                    if (Peek() != '_') sb.Append(Advance());
                    else Advance();
                }
                
                TokenType type = baseChar == 'x' ? TokenType.HEX_INTEGER
                               : baseChar == 'o' ? TokenType.OCT_INTEGER
                               : TokenType.BIN_INTEGER;
                return new Token(type, sb.ToString(), startLine, startCol);
            }
            
            // 小数/普通整数
            while (char.IsDigit(Peek()) || Peek() == '_')
            {
                if (Peek() != '_') sb.Append(Advance());
                else Advance();
            }
            
            if (Peek() == '.' && char.IsDigit(Peek(1)))
            {
                isFloat = true;
                sb.Append(Advance()); // .
                while (char.IsDigit(Peek()) || Peek() == '_')
                {
                    if (Peek() != '_') sb.Append(Advance());
                    else Advance();
                }
            }
            
            // 科学计数法
            if (Peek() == 'e' || Peek() == 'E')
            {
                isFloat = true;
                sb.Append(Advance()); // e/E
                if (Peek() == '+' || Peek() == '-') sb.Append(Advance());
                while (char.IsDigit(Peek()) || Peek() == '_')
                {
                    if (Peek() != '_') sb.Append(Advance());
                    else Advance();
                }
            }
            
            // 虚数后缀 j/J
            if (Peek() == 'j' || Peek() == 'J')
            {
                isFloat = true;
                sb.Append(Advance());
            }
            
            string value = sb.ToString();
            return new Token(isFloat ? TokenType.FLOAT : TokenType.INTEGER, value, startLine, startCol);
        }


        private new Token ReadIdentifier()
        {
            int startLine = _line;
            int startCol = _col;
            var sb = new System.Text.StringBuilder();
            
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_' || IsChineseChar(Peek()))
            {
                sb.Append(Advance());
            }
            
            string value = sb.ToString();
            TokenType type = KEYWORDS.ContainsKey(value) ? KEYWORDS[value] : TokenType.IDENTIFIER;
            return new Token(type, value, startLine, startCol);
        }

        private Token ReadIndent()
        {
            int startLine = _line;
            int startCol = _col;
            int spaces = 0;
            
            while (Peek() == ' ' || Peek() == '\t')
            {
                if (Advance() == ' ')
                    spaces++;
                else
                    spaces += 4; // tab = 4 spaces
            }
            
            return new Token(TokenType.INDENT, spaces.ToString(), startLine, startCol);
        }

        public void Tokenize()
        {
            List<int> indentStack = new List<int>(); // 缩进级别栈
            int currentIndent = 0;
            bool atLineStart = true;
            
            while (Peek() != '\0')
            {
                // 处理行首缩进
                if (atLineStart)
                {
                    if (Peek() == ' ' || Peek() == '\t')
                    {
                        Token indent = ReadIndent();
                        int newIndent = int.Parse(indent.Value);
                        
                        if (newIndent > currentIndent)
                        {
                            Tokens.Add(new Token(TokenType.INDENT, "", indent.Line, indent.Column));
                            indentStack.Add(newIndent);
                            currentIndent = newIndent;
                        }
                        else if (newIndent < currentIndent)
                        {
                            while (currentIndent > newIndent && indentStack.Count > 0)
                            {
                                Tokens.Add(new Token(TokenType.DEDENT, "", indent.Line, indent.Column));
                                indentStack.RemoveAt(indentStack.Count - 1);
                                currentIndent = indentStack.Count > 0 ? indentStack[indentStack.Count - 1] : 0;
                            }
                        }
                    }
                    else if (currentIndent > 0)
                    {
                        // 行首无缩进但 currentIndent > 0 → 需要补 DEDENT
                        while (indentStack.Count > 0)
                        {
                            Tokens.Add(new Token(TokenType.DEDENT, "", _line, _col));
                            indentStack.RemoveAt(indentStack.Count - 1);
                        }
                        currentIndent = 0;
                    }
                    atLineStart = false;
                    continue;
                }
                
                // 跳过空白、回车符和注释
                if (Peek() == ' ' || Peek() == '\t' || Peek() == '\r')
                {
                    Advance();
                    continue;
                }
                
                if (Peek() == '#')
                {
                    SkipComment();
                    continue;
                }
                
                // 换行处理
                if (Peek() == '\n')
                {
                    Advance();
                    Tokens.Add(new Token(TokenType.NEWLINE, "", _line, _col));
                    atLineStart = true;
                    continue;
                }
                
                atLineStart = false;
                
                // 字符串（包括f-string）
                if (Peek() == 'f' || Peek() == 'F')
                {
                    // 检查是否是f-string
                    int savedPos = _pos;
                    char savedPeek = Peek();
                    Advance(); // 跳过f/F
                    if (Peek() == '"' || Peek() == '\'')
                    {
                        Tokens.Add(ReadFString(Peek()));
                        continue;
                    }
                    else
                    {
                        // 不是f-string，回退
                        _pos = savedPos;
                        Peek(); // 恢复peek缓存
                    }
                }
                
                if (Peek() == '"' || Peek() == '\'')
                {
                    Tokens.Add(ReadString(Peek()));
                    continue;
                }
                
                // 数字
                if (char.IsDigit(Peek()))
                {
                    Tokens.Add(ReadNumber());
                    continue;
                }
                
                // 标识符
                if (char.IsLetter(Peek()) || Peek() == '_' || IsChineseChar(Peek()))
                {
                    Tokens.Add(ReadIdentifier());
                    continue;
                }
                
                // 运算符和标点
                char c = Advance();
                switch (c)
                {
                    case '+':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.PLUS_ASSIGN, "+=", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.PLUS, "+", _line, _col));
                        break;
                    case '-':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.MINUS_ASSIGN, "-=", _line, _col)); }
                        else if (Peek() == '>') { Advance(); Tokens.Add(new Token(TokenType.ARROW, "->", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.MINUS, "-", _line, _col));
                        break;
                    case '*':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.MUL_ASSIGN, "*=", _line, _col)); }
                        else if (Peek() == '*') { Advance(); Tokens.Add(new Token(TokenType.POWER, "**", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.MUL, "*", _line, _col));
                        break;
                    case '/':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.DIV_ASSIGN, "/=", _line, _col)); }
                        else if (Peek() == '/') { Advance(); if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.FLOOR_DIV_ASSIGN, "//=", _line, _col)); } else Tokens.Add(new Token(TokenType.FLOOR_DIV, "//", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.DIV, "/", _line, _col));
                        break;
                    case '%': if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.MOD_ASSIGN, "%=", _line, _col)); } else Tokens.Add(new Token(TokenType.MOD, "%", _line, _col)); break;
                    case '(': Tokens.Add(new Token(TokenType.LPAREN, "(", _line, _col)); break;
                    case ')': Tokens.Add(new Token(TokenType.RPAREN, ")", _line, _col)); break;
                    case '[': Tokens.Add(new Token(TokenType.LBRACKET, "[", _line, _col)); break;
                    case ']': Tokens.Add(new Token(TokenType.RBRACKET, "]", _line, _col)); break;
                    case '{': Tokens.Add(new Token(TokenType.LBRACE, "{", _line, _col)); break;
                    case '}': Tokens.Add(new Token(TokenType.RBRACE, "}", _line, _col)); break;
                    case ':': Tokens.Add(new Token(TokenType.COLON, ":", _line, _col)); break;
                    case ';': Tokens.Add(new Token(TokenType.SEMICOLON, ";", _line, _col)); break;
                    case ',': Tokens.Add(new Token(TokenType.COMMA, ",", _line, _col)); break;
                    case '.': Tokens.Add(new Token(TokenType.DOT, ".", _line, _col)); break;
                    case '=':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.EQ, "==", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.ASSIGN, "=", _line, _col));
                        break;
                    case '!':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.NE, "!=", _line, _col)); }
                        else Error($"意外的字符: {c}");
                        break;
                    case '<':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.LE, "<=", _line, _col)); }
                        else if (Peek() == '<') { Advance(); Tokens.Add(new Token(TokenType.LSHIFT, "<<", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.LT, "<", _line, _col));
                        break;
                    case '>':
                        if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.GE, ">=", _line, _col)); }
                        else if (Peek() == '>') { Advance(); Tokens.Add(new Token(TokenType.RSHIFT, ">>", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.GT, ">", _line, _col));
                        break;
                    case '&': Tokens.Add(new Token(TokenType.BITAND, "&", _line, _col)); break;
                    case '|': Tokens.Add(new Token(TokenType.BITOR, "|", _line, _col)); break;
                    case '^': Tokens.Add(new Token(TokenType.BITXOR, "^", _line, _col)); break;
                    case '~': Tokens.Add(new Token(TokenType.BITNOT, "~", _line, _col)); break;
                    case '@': Tokens.Add(new Token(TokenType.AT, "@", _line, _col)); break;
                    default:
                        Error($"意外的字符: {c}");
                        break;
                }
            }
            
            // 文件结束，添加剩余的 DEDENT
            while (indentStack.Count > 0)
            {
                Tokens.Add(new Token(TokenType.DEDENT, "", _line, _col));
                indentStack.RemoveAt(indentStack.Count - 1);
            }
            
            Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
        }
    }
}
