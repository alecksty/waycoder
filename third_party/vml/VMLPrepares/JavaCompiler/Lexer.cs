using System.Collections.Generic;
using CompilerBase;

namespace JavaCompiler
{
    /// <summary>
    /// Java语言词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        public List<Token> Tokens { get; private set; }

        // Java关键字映射
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            // 访问修饰符
            { "public", TokenType.Public },
            { "private", TokenType.Private },
            { "protected", TokenType.Protected },
            
            // 类相关
            { "class", TokenType.Class },
            { "interface", TokenType.Interface },
            { "enum", TokenType.Enum },
            { "extends", TokenType.Extends },
            { "implements", TokenType.Implements },
            
            // 类型
            { "int", TokenType.Int },
            { "long", TokenType.Long },
            { "short", TokenType.Short },
            { "byte", TokenType.Byte },
            { "char", TokenType.Char },
            { "float", TokenType.Float },
            { "double", TokenType.Double },
            { "boolean", TokenType.Boolean },
            { "void", TokenType.Void },
            
            // 控制流
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "for", TokenType.For },
            { "while", TokenType.While },
            { "do", TokenType.Do },
            { "switch", TokenType.Switch },
            { "case", TokenType.Case },
            { "default", TokenType.Default },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "return", TokenType.Return },
            
            // 修饰符
            { "static", TokenType.Static },
            { "final", TokenType.Final },
            { "abstract", TokenType.Abstract },
            { "synchronized", TokenType.Synchronized },
            { "volatile", TokenType.Volatile },
            { "transient", TokenType.Transient },
            { "native", TokenType.Native },
            
            // 其他
            { "new", TokenType.New },
            { "this", TokenType.This },
            { "super", TokenType.Super },
            { "instanceof", TokenType.Instanceof },
            { "package", TokenType.Package },
            { "import", TokenType.Import },
            { "try", TokenType.Try },
            { "catch", TokenType.Catch },
            { "finally", TokenType.Finally },
            { "throw", TokenType.Throw },
            { "throws", TokenType.Throws },
            { "assert", TokenType.Assert },
            
            // 字面量关键字
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "null", TokenType.Null }
        };

        public Lexer(string source) : base(source)
        {
            Tokens = new List<Token>();
        }

        /// <summary>
        /// 词法分析主方法
        /// </summary>
        public List<Token> Tokenize()
        {
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                // 跳过空白字符
                if (char.IsWhiteSpace(current))
                {
                    if (current == '\n')
                    {
                        _line++;
                        _col = 1;
                    }
                    else
                    {
                        _col++;
                    }
                    _pos++;
                    continue;
                }
                
                // 单行注释
                if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '/')
                {
                    SkipLineComment();
                    continue;
                }
                
                // 多行注释
                if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '*')
                {
                    SkipBlockComment();
                    continue;
                }
                
                // 标识符和关键字
                if (char.IsLetter(current) || IsChineseChar(current) || current == '_' || current == '$')
                {
                    ReadIdentifier();
                    continue;
                }
                
                // 数字字面量
                if (char.IsDigit(current))
                {
                    ReadNumber();
                    continue;
                }
                
                // 字符串字面量
                if (current == '"')
                {
                    ReadString();
                    continue;
                }
                
                // 字符字面量
                if (current == '\'')
                {
                    ReadChar();
                    continue;
                }
                
                // 运算符和分隔符
                ReadOperatorOrDelimiter();
            }
            
            // 添加文件结束标记
            Tokens.Add(new Token(TokenType.EndOfFile, "", _line, _col));
            return Tokens;
        }


        private new void ReadIdentifier()
        {
            int start = _pos;
            int startColumn = _col;
            
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || IsChineseChar(_source[_pos]) || _source[_pos] == '_' || _source[_pos] == '$'))
            {
                _pos++;
                _col++;
            }
            
            string text = _source.Substring(start, _pos - start);
            
            // 检查是否为关键字
            if (KEYWORDS.TryGetValue(text, out TokenType type))
            {
                Tokens.Add(new Token(type, text, _line, startColumn));
            }
            else
            {
                Tokens.Add(new Token(TokenType.Identifier, text, _line, startColumn));
            }
        }

        private new void ReadNumber()
        {
            int start = _pos;
            int startColumn = _col;
            bool hasDecimal = false;
            bool hasExponent = false;

            // Java hex literal: 0x / 0X prefix
            if (_pos + 1 < _source.Length && _source[_pos] == '0' && (_source[_pos + 1] == 'x' || _source[_pos + 1] == 'X'))
            {
                _pos += 2; _col += 2;
                while (_pos < _source.Length && LexerHelper.IsHexDigit(_source[_pos]))
                { _pos++; _col++; }
                // Handle long suffix
                if (_pos < _source.Length && (_source[_pos] == 'l' || _source[_pos] == 'L'))
                { _pos++; _col++; }
                Tokens.Add(new Token(TokenType.IntegerLiteral, _source.Substring(start, _pos - start), _line, startColumn));
                return;
            }
            
            while (_pos < _source.Length)
            {
                char c = _source[_pos];
                
                if (char.IsDigit(c))
                {
                    _pos++;
                    _col++;
                }
                else if (c == '.' && !hasDecimal && !hasExponent)
                {
                    hasDecimal = true;
                    _pos++;
                    _col++;
                }
                else if ((c == 'e' || c == 'E') && !hasExponent)
                {
                    hasExponent = true;
                    _pos++;
                    _col++;
                    
                    // 检查指数符号
                    if (_pos < _source.Length && (_source[_pos] == '+' || _source[_pos] == '-'))
                    {
                        _pos++;
                        _col++;
                    }
                }
                else if (c == 'f' || c == 'F' || c == 'd' || c == 'D' || c == 'l' || c == 'L')
                {
                    // 类型后缀 - 检查后面是否是字母或数字
                    if (_pos + 1 >= _source.Length || !char.IsLetterOrDigit(_source[_pos + 1]))
                    {
                        _pos++;
                        _col++;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            
            string text = _source.Substring(start, _pos - start);
            Tokens.Add(new Token(hasDecimal ? TokenType.FloatLiteral : TokenType.IntegerLiteral, text, _line, startColumn));
        }

        private void ReadString()
        {
            int startColumn = _col;
            _pos++; // 跳过开头的 "
            _col++;
            
            int start = _pos;
            while (_pos < _source.Length && _source[_pos] != '"')
            {
                if (_source[_pos] == '\\')
                {
                    _pos++; // 跳过转义字符
                    _col++;
                }
                
                if (_pos < _source.Length)
                {
                    _pos++;
                    _col++;
                }
            }
            
            string text = _source.Substring(start, _pos - start);
            
            if (_pos < _source.Length && _source[_pos] == '"')
            {
                _pos++; // 跳过结尾的 "
                _col++;
            }
            
            Tokens.Add(new Token(TokenType.StringLiteral, text, _line, startColumn));
        }

        private void ReadChar()
        {
            int startColumn = _col;
            _pos++; // 跳过开头的 '
            _col++;
            
            int start = _pos;
            if (_pos < _source.Length && _source[_pos] == '\\')
            {
                _pos++; // 跳过转义字符
                _col++;
            }
            
            if (_pos < _source.Length)
            {
                _pos++;
                _col++;
            }
            
            string text = _source.Substring(start, _pos - start);
            
            if (_pos < _source.Length && _source[_pos] == '\'')
            {
                _pos++; // 跳过结尾的 '
                _col++;
            }
            
            Tokens.Add(new Token(TokenType.CharLiteral, text, _line, startColumn));
        }

        private void ReadOperatorOrDelimiter()
        {
            char current = _source[_pos];
            int startColumn = _col;
            
            // 检查多字符运算符
            if (_pos + 1 < _source.Length)
            {
                string twoChar = _source.Substring(_pos, 2);
                
                switch (twoChar)
                {
                    case "++": AddToken(TokenType.Increment, "++"); _pos += 2; _col += 2; return;
                    case "--": AddToken(TokenType.Decrement, "--"); _pos += 2; _col += 2; return;
                    case "==": AddToken(TokenType.Equal, "=="); _pos += 2; _col += 2; return;
                    case "!=": AddToken(TokenType.NotEqual, "!="); _pos += 2; _col += 2; return;
                    case "<=": AddToken(TokenType.LessThanOrEqual, "<="); _pos += 2; _col += 2; return;
                    case ">=": AddToken(TokenType.GreaterThanOrEqual, ">="); _pos += 2; _col += 2; return;
                    case "&&": AddToken(TokenType.LogicalAnd, "&&"); _pos += 2; _col += 2; return;
                    case "||": AddToken(TokenType.LogicalOr, "||"); _pos += 2; _col += 2; return;
                    case "<<":
                        if (_pos + 2 < _source.Length && _source[_pos + 2] == '=')
                        {
                            AddToken(TokenType.LeftShiftAssign, "<<=");
                            _pos += 3; _col += 3;
                            return;
                        }
                        AddToken(TokenType.LeftShift, "<<"); _pos += 2; _col += 2; return;
                    case ">>":
                        if (_pos + 2 < _source.Length && _source[_pos + 2] == '=')
                        {
                            AddToken(TokenType.RightShiftAssign, ">>=");
                            _pos += 3; _col += 3;
                            return;
                        }
                        if (_pos + 2 < _source.Length && _source[_pos + 2] == '>')
                        {
                            if (_pos + 3 < _source.Length && _source[_pos + 3] == '=')
                            {
                                AddToken(TokenType.UnsignedRightShiftAssign, ">>>=");
                                _pos += 4; _col += 4;
                                return;
                            }
                            AddToken(TokenType.UnsignedRightShift, ">>>");
                            _pos += 3; _col += 3;
                            return;
                        }
                        AddToken(TokenType.RightShift, ">>"); _pos += 2; _col += 2; return;
                    case "+=": AddToken(TokenType.PlusAssign, "+="); _pos += 2; _col += 2; return;
                    case "-=": AddToken(TokenType.MinusAssign, "-="); _pos += 2; _col += 2; return;
                    case "*=": AddToken(TokenType.MultiplyAssign, "*="); _pos += 2; _col += 2; return;
                    case "/=": AddToken(TokenType.DivideAssign, "/="); _pos += 2; _col += 2; return;
                    case "%=": AddToken(TokenType.ModuloAssign, "%="); _pos += 2; _col += 2; return;
                    case "&=": AddToken(TokenType.AndAssign, "&="); _pos += 2; _col += 2; return;
                    case "|=": AddToken(TokenType.OrAssign, "|="); _pos += 2; _col += 2; return;
                    case "^=": AddToken(TokenType.XorAssign, "^="); _pos += 2; _col += 2; return;
                    case "->": AddToken(TokenType.Arrow, "->"); _pos += 2; _col += 2; return;
                    case "::": AddToken(TokenType.DoubleColon, "::"); _pos += 2; _col += 2; return;
                }
            }
            
            // 单字符运算符和分隔符
            switch (current)
            {
                case '+': AddToken(TokenType.Plus, "+"); break;
                case '-': AddToken(TokenType.Minus, "-"); break;
                case '*': AddToken(TokenType.Multiply, "*"); break;
                case '/': AddToken(TokenType.Divide, "/"); break;
                case '%': AddToken(TokenType.Modulo, "%"); break;
                case '=': AddToken(TokenType.Assign, "="); break;
                case '<': AddToken(TokenType.LessThan, "<"); break;
                case '>': AddToken(TokenType.GreaterThan, ">"); break;
                case '!': AddToken(TokenType.LogicalNot, "!"); break;
                case '&': AddToken(TokenType.BitwiseAnd, "&"); break;
                case '|': AddToken(TokenType.BitwiseOr, "|"); break;
                case '^': AddToken(TokenType.BitwiseXor, "^"); break;
                case '~': AddToken(TokenType.BitwiseNot, "~"); break;
                case '?': AddToken(TokenType.QuestionMark, "?"); break;
                case ':': AddToken(TokenType.Colon, ":"); break;
                case ';': AddToken(TokenType.Semicolon, ";"); break;
                case ',': AddToken(TokenType.Comma, ","); break;
                case '.':
                    if (_pos + 2 < _source.Length && _source[_pos + 1] == '.' && _source[_pos + 2] == '.')
                    {
                        AddToken(TokenType.Ellipsis, "...");
                        _pos += 2; // +1 from post-increment = skip 3 dots total
                    }
                    else
                    {
                        AddToken(TokenType.Dot, ".");
                    }
                    break;
                case '(': AddToken(TokenType.LeftParen, "("); break;
                case ')': AddToken(TokenType.RightParen, ")"); break;
                case '[': AddToken(TokenType.LeftBracket, "["); break;
                case ']': AddToken(TokenType.RightBracket, "]"); break;
                case '{': AddToken(TokenType.LeftBrace, "{"); break;
                case '}': AddToken(TokenType.RightBrace, "}"); break;
                case '@': AddToken(TokenType.AtSymbol, "@"); break;
                default:
                    AddToken(TokenType.Error, current.ToString());
                    break;
            }
            
            _pos++;
            _col++;
        }

        private void AddToken(TokenType type, string value)
        {
            Tokens.Add(new Token(type, value, _line, _col));
        }
    }
}