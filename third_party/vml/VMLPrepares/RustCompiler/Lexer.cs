using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace RustCompiler
{
    /// <summary>
    /// Rust词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        private readonly List<Token> _tokens;
        
        public Lexer(string source) : base(source)
        {
            _tokens = new List<Token>();
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
                
                // 跳过注释
                if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '/')
                {
                    SkipLineComment();
                    continue;
                }
                
                if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '*')
                {
                    SkipBlockComment();
                    continue;
                }
                
                // 处理标识符和关键字
                if (char.IsLetter(current) || IsChineseChar(current) || current == '_')
                {
                    ReadIdentifierOrKeyword();
                    continue;
                }
                
                // 处理数字字面量
                if (char.IsDigit(current))
                {
                    ReadNumber();
                    continue;
                }
                
                // 处理字符串字面量
                if (current == '"')
                {
                    ReadString();
                    continue;
                }
                
                // 处理字符字面量
                if (current == '\'')
                {
                    ReadCharacter();
                    continue;
                }
                
                // 处理运算符和分隔符
                ReadOperatorOrDelimiter();
            }
            
            // 添加EOF token
            _tokens.Add(new Token(TokenType.EOF, "", _line, _col));
            return _tokens;
        }
        
        /// <summary>
        /// 读取标识符或关键字
        /// </summary>


        private void ReadIdentifierOrKeyword()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_' || IsChineseChar(_source[_pos])))
            {
                _pos++;
                _col++;
            }
            
            string value = _source.Substring(start, _pos - start);
            TokenType type = GetKeywordTokenType(value);
            
            _tokens.Add(new Token(type, value, startLine, startColumn));
        }
        
        /// <summary>
        /// 获取关键字对应的Token类型
        /// </summary>
        private TokenType GetKeywordTokenType(string value)
        {
            return value switch
            {
                "fn" => TokenType.FN,
                "let" => TokenType.LET,
                "mut" => TokenType.MUT,
                "const" => TokenType.CONST,
                "static" => TokenType.STATIC,
                "if" => TokenType.IF,
                "else" => TokenType.ELSE,
                "while" => TokenType.WHILE,
                "for" => TokenType.FOR,
                "loop" => TokenType.LOOP,
                "match" => TokenType.MATCH,
                "return" => TokenType.RETURN,
                "break" => TokenType.BREAK,
                "continue" => TokenType.CONTINUE,
                "true" => TokenType.TRUE,
                "false" => TokenType.FALSE,
                "as" => TokenType.AS,
                "use" => TokenType.USE,
                "mod" => TokenType.MOD,
                "struct" => TokenType.STRUCT,
                "enum" => TokenType.ENUM,
                "impl" => TokenType.IMPL,
                "trait" => TokenType.TRAIT,
                "where" => TokenType.WHERE,
                "type" => TokenType.TYPE,
                "pub" => TokenType.PUB,
                "crate" => TokenType.CRATE,
                "super" => TokenType.SUPER,
                "self" => TokenType.SELF,
                "Self" => TokenType.SELF_TYPE,
                
                // 类型关键字
                "i8" => TokenType.I8,
                "i16" => TokenType.I16,
                "i32" => TokenType.I32,
                "i64" => TokenType.I64,
                "i128" => TokenType.I128,
                "isize" => TokenType.ISIZE,
                "u8" => TokenType.U8,
                "u16" => TokenType.U16,
                "u32" => TokenType.U32,
                "u64" => TokenType.U64,
                "u128" => TokenType.U128,
                "usize" => TokenType.USIZE,
                "f32" => TokenType.F32,
                "f64" => TokenType.F64,
                "bool" => TokenType.BOOL,
                "char" => TokenType.CHAR,
                "str" => TokenType.STR,
                
                _ => TokenType.IDENTIFIER
            };
        }
        
        /// <summary>
        /// 读取数字字面量
        /// </summary>
        private new void ReadNumber()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            bool isFloat = false;

            // Handle 0x hex prefix
            if (_pos + 1 < _source.Length && _source[_pos] == '0' && (_source[_pos + 1] == 'x' || _source[_pos + 1] == 'X'))
            {
                _pos += 2; _col += 2;
                while (_pos < _source.Length && LexerHelper.IsHexDigit(_source[_pos]))
                {
                    _pos++; _col++;
                }
                string hexStr = _source.Substring(start, _pos - start);
                _tokens.Add(new Token(TokenType.INTEGER, hexStr, startLine, startColumn));
                return;
            }
            
            while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.' || _source[_pos] == 'e' || _source[_pos] == 'E'))
            {
                if (_source[_pos] == '.')
                {
                    // 检查是否是范围运算符的一部分（例如 "0..5"）
                    if (_pos + 1 < _source.Length && _source[_pos + 1] == '.')
                    {
                        // 这是范围运算符的开始，停止读取数字
                        break;
                    }
                    
                    if (isFloat)
                    {
                        // 多个小数点，错误
                        break;
                    }
                    isFloat = true;
                }
                _pos++;
                _col++;
            }
            
            // 检查类型后缀
            if (_pos < _source.Length && char.IsLetter(_source[_pos]))
            {
                // 跳过类型后缀
                while (_pos < _source.Length && char.IsLetter(_source[_pos]))
                {
                    _pos++;
                    _col++;
                }
            }
            
            string value = _source.Substring(start, _pos - start);
            TokenType type = isFloat ? TokenType.FLOAT : TokenType.INTEGER;
            
            _tokens.Add(new Token(type, value, startLine, startColumn));
        }
        
        /// <summary>
        /// 读取字符串字面量
        /// </summary>
        private void ReadString()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            
            _pos++; // 跳过开头的 "
            _col++;
            
            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                if (escaped)
                {
                    sb.Append(GetEscapeChar(current));
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    _pos++;
                    _col++;
                    break;
                }
                else
                {
                    sb.Append(current);
                }
                
                _pos++;
                _col++;
            }
            
            string value = sb.ToString();
            _tokens.Add(new Token(TokenType.STRING, value, startLine, startColumn));
        }
        
        /// <summary>
        /// 读取字符字面量
        /// </summary>
        private void ReadCharacter()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            
            _pos++; // 跳过开头的 '
            _col++;
            
            char value = '\0';
            
            if (_pos < _source.Length)
            {
                if (_source[_pos] == '\\')
                {
                    _pos++;
                    _col++;
                    
                    if (_pos < _source.Length)
                    {
                        value = GetEscapeChar(_source[_pos]);
                        _pos++;
                        _col++;
                    }
                }
                else
                {
                    value = _source[_pos];
                    _pos++;
                    _col++;
                }
                
                // 跳过结尾的 '
                if (_pos < _source.Length && _source[_pos] == '\'')
                {
                    _pos++;
                    _col++;
                }
            }
            
            _tokens.Add(new Token(TokenType.CHARACTER, value.ToString(), startLine, startColumn));
        }
        
        /// <summary>
        /// 获取转义字符
        /// </summary>
        private char GetEscapeChar(char c)
        {
            return c switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '\\' => '\\',
                '\'' => '\'',
                '"' => '"',
                '0' => '\0',
                _ => c
            };
        }
        
        /// <summary>
        /// 读取运算符或分隔符
        /// </summary>
        private void ReadOperatorOrDelimiter()
        {
            char current = _source[_pos];
            int startLine = _line;
            int startColumn = _col;
            
            // 检查多字符运算符
            if (_pos + 1 < _source.Length)
            {
                string twoChars = _source.Substring(_pos, 2);
                
                switch (twoChars)
                {
                    case "==": AddToken(TokenType.EQEQ, "=="); return;
                    case "!=": AddToken(TokenType.BANGEQ, "!="); return;
                    case "<=": AddToken(TokenType.LTEQ, "<="); return;
                    // >= is not emitted as a single token to avoid generic + = ambiguity.
                    // The parser handles > = sequences in comparison context.
                    case "&&": AddToken(TokenType.AND, "&&"); return;
                    case "||": AddToken(TokenType.OR, "||"); return;
                    case "<<": AddToken(TokenType.SHL, "<<"); return;
                    case ">>": AddToken(TokenType.SHR, ">>"); return;
                    case "->": AddToken(TokenType.ARROW, "->"); return;
                    case "=>": AddToken(TokenType.FAT_ARROW, "=>"); return;
                    case "..": 
                        if (_pos + 2 < _source.Length && _source[_pos + 2] == '=')
                        {
                            AddToken(TokenType.RANGE_INCLUSIVE, "..=");
                            return;
                        }
                        AddToken(TokenType.RANGE, "..");
                        return;
                    case "+=": AddToken(TokenType.PLUSEQ, "+="); return;
                    case "-=": AddToken(TokenType.MINUSEQ, "-="); return;
                    case "*=": AddToken(TokenType.STAREQ, "*="); return;
                    case "/=": AddToken(TokenType.SLASHEQ, "/="); return;
                    case "%=": AddToken(TokenType.PERCENTEQ, "%="); return;
                    case "^=": AddToken(TokenType.CARETEQ, "^="); return;
                    case "&=": AddToken(TokenType.AMPERSANDEQ, "&="); return;
                    case "|=": AddToken(TokenType.PIPEEQ, "|="); return;
                    case "<<=": 
                        if (_pos + 2 < _source.Length && _source.Substring(_pos, 3) == "<<=")
                        {
                            AddToken(TokenType.SHLEQ, "<<=");
                            return;
                        }
                        break;
                    case ">>=":
                        if (_pos + 2 < _source.Length && _source.Substring(_pos, 3) == ">>=")
                        {
                            AddToken(TokenType.SHREQ, ">>=");
                            return;
                        }
                        break;
                }
            }
            
            // 单字符运算符和分隔符
            switch (current)
            {
                case '+': AddToken(TokenType.PLUS, "+"); break;
                case '-': AddToken(TokenType.MINUS, "-"); break;
                case '*': AddToken(TokenType.STAR, "*"); break;
                case '/': AddToken(TokenType.SLASH, "/"); break;
                case '%': AddToken(TokenType.PERCENT, "%"); break;
                case '^': AddToken(TokenType.CARET, "^"); break;
                case '&': AddToken(TokenType.AMPERSAND, "&"); break;
                case '|': AddToken(TokenType.PIPE, "|"); break;
                case '~': AddToken(TokenType.TILDE, "~"); break;
                case '!': AddToken(TokenType.BANG, "!"); break;
                case '=': AddToken(TokenType.EQ, "="); break;
                case '<': AddToken(TokenType.LT, "<"); break;
                case '>': AddToken(TokenType.GT, ">"); break;
                case '(': AddToken(TokenType.LPAREN, "("); break;
                case ')': AddToken(TokenType.RPAREN, ")"); break;
                case '{': AddToken(TokenType.LBRACE, "{"); break;
                case '}': AddToken(TokenType.RBRACE, "}"); break;
                case '[': AddToken(TokenType.LBRACKET, "["); break;
                case ']': AddToken(TokenType.RBRACKET, "]"); break;
                case ',': AddToken(TokenType.COMMA, ","); break;
                case '.': AddToken(TokenType.DOT, "."); break;
                case ':': AddToken(TokenType.COLON, ":"); break;
                case ';': AddToken(TokenType.SEMICOLON, ";"); break;
                case '@': AddToken(TokenType.AT, "@"); break;
                case '#': AddToken(TokenType.HASH, "#"); break;
                case '$': AddToken(TokenType.DOLLAR, "$"); break;
                case '_': AddToken(TokenType.UNDERSCORE, "_"); break;
                case '?': AddToken(TokenType.QUESTION, "?"); break;
                default:
                    AddToken(TokenType.ERROR, current.ToString());
                    break;
            }
            
            void AddToken(TokenType type, string value)
            {
                _tokens.Add(new Token(type, value, startLine, startColumn));
                _pos += value.Length;
                _col += value.Length;
            }
        }
        
        /// <summary>
        /// 跳过多行注释
        /// </summary>
        
        /// <summary>
        /// 跳过单行注释
        /// </summary>
    }
}