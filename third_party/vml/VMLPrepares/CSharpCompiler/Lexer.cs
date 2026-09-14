using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace CSharpCompiler
{
    /// <summary>
    /// C#词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        
        public Lexer(string source) : base(source) { }
        
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            
            while (_pos < _source.Length)
            {
                var token = NextToken();
                if (token.Type != TokenType.SingleLineComment && token.Type != TokenType.MultiLineComment)
                {
                    tokens.Add(token);
                }
                
                if (token.Type == TokenType.EndOfFile)
                {
                    break;
                }
            }
            
            tokens.Add(new Token(TokenType.EndOfFile, "", _line, _col));
            return tokens;
        }
        
        private Token NextToken()
        {
            SkipWhitespace();
            
            if (_pos >= _source.Length)
            {
                return new Token(TokenType.EndOfFile, "", _line, _col);
            }
            
            char current = _source[_pos];
            
            // 单行注释
            if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '/')
            {
                return ReadSingleLineComment();
            }
            
            // 多行注释
            if (current == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '*')
            {
                return ReadMultiLineComment();
            }
            
            // 字符串字面量
            if (current == '"')
            {
                return ReadStringLiteral();
            }
            
            // 字符字面量
            if (current == '\'')
            {
                return ReadCharacterLiteral();
            }
            
            // 数字字面量
            if (char.IsDigit(current))
            {
                return ReadNumberLiteral();
            }
            
            // 标识符和关键字
            if (char.IsLetter(current) || IsChineseChar(current) || current == '_' || current == '@')
            {
                return ReadIdentifierOrKeyword();
            }
            
            // 运算符和分隔符
            return ReadOperatorOrDelimiter();
        }
        
        
        private Token ReadSingleLineComment()
        {
            int startLine = _line;
            int startColumn = _col;
            
            _pos += 2; // 跳过 "//"
            _col += 2;
            
            while (_pos < _source.Length && _source[_pos] != '\n')
            {
                _pos++;
                _col++;
            }
            
            return new Token(TokenType.SingleLineComment, "", startLine, startColumn);
        }
        
        private Token ReadMultiLineComment()
        {
            int startLine = _line;
            int startColumn = _col;
            
            _pos += 2; // 跳过 "/*"
            _col += 2;
            
            while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/'))
            {
                if (_source[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _pos++;
            }
            
            if (_pos + 1 < _source.Length)
            {
                _pos += 2; // 跳过 "*/"
                _col += 2;
            }
            
            return new Token(TokenType.MultiLineComment, "", startLine, startColumn);
        }
        
        private Token ReadStringLiteral()
        {
            int startLine = _line;
            int startColumn = _col;
            
            _pos++; // 跳过开头的引号
            _col++;
            
            StringBuilder value = new StringBuilder();
            
            while (_pos < _source.Length && _source[_pos] != '"')
            {
                if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
                {
                    // 处理转义字符
                    _pos++;
                    _col++;
                    char escaped = _source[_pos];
                    value.Append(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '"' => '"',
                        '\'' => '\'',
                        _ => escaped
                    });
                }
                else
                {
                    value.Append(_source[_pos]);
                }
                
                _pos++;
                _col++;
            }
            
            if (_pos < _source.Length && _source[_pos] == '"')
            {
                _pos++; // 跳过结尾的引号
                _col++;
            }
            
            return new Token(TokenType.StringLiteral, value.ToString(), startLine, startColumn);
        }
        
        private Token ReadCharacterLiteral()
        {
            int startLine = _line;
            int startColumn = _col;
            
            _pos++; // 跳过开头的单引号
            _col++;
            
            char value = '\0';
            
            if (_pos < _source.Length && _source[_pos] == '\\')
            {
                // 处理转义字符
                _pos++;
                _col++;
                if (_pos < _source.Length)
                {
                    value = _source[_pos] switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        _ => _source[_pos]
                    };
                    _pos++;
                    _col++;
                }
            }
            else if (_pos < _source.Length)
            {
                value = _source[_pos];
                _pos++;
                _col++;
            }
            
            if (_pos < _source.Length && _source[_pos] == '\'')
            {
                _pos++; // 跳过结尾的单引号
                _col++;
            }
            
            return new Token(TokenType.CharacterLiteral, value.ToString(), startLine, startColumn);
        }
        
        private Token ReadNumberLiteral()
        {
            int startLine = _line;
            int startColumn = _col;
            int start = _pos;
            bool hasDecimal = false;
            
            // 十六进制字面量: 0x 或 0X 前缀
            if (_pos + 1 < _source.Length && _source[_pos] == '0' &&
                (_source[_pos + 1] == 'x' || _source[_pos + 1] == 'X'))
            {
                _pos += 2; // skip 0x
                _col += 2;
                int hexStart = _pos;
                while (_pos < _source.Length && 
                       ((_source[_pos] >= '0' && _source[_pos] <= '9') ||
                        (_source[_pos] >= 'a' && _source[_pos] <= 'f') ||
                        (_source[_pos] >= 'A' && _source[_pos] <= 'F')))
                {
                    _pos++;
                    _col++;
                }
                string hexStr = _source.Substring(hexStart, _pos - hexStart);
                if (hexStr.Length > 0)
                {
                    int hexVal = Convert.ToInt32(hexStr, 16);
                    return new Token(TokenType.IntegerLiteral, hexVal.ToString(), startLine, startColumn);
                }
                // 无效的十六进制, 回退到普通数字
                _pos = start + 1;
                _col = startColumn + 1;
            }
            
            while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.'))
            {
                if (_source[_pos] == '.')
                {
                    if (hasDecimal) break;
                    hasDecimal = true;
                }
                _pos++;
                _col++;
            }
            
            string value = _source.Substring(start, _pos - start);
            TokenType type = hasDecimal ? TokenType.FloatLiteral : TokenType.IntegerLiteral;

            // 消费数值后缀，并保留后缀在 Value 中供 Parser 区分 float/double/long:
            //   f/F → float, d/D → double, l/L → long
            if (_pos < _source.Length && (_source[_pos] == 'f' || _source[_pos] == 'F'))
            {
                _pos++; _col++;
                value += 'f';
                type = TokenType.FloatLiteral;
            }
            else if (_pos < _source.Length && (_source[_pos] == 'd' || _source[_pos] == 'D'))
            {
                _pos++; _col++;
                value += 'd';
                type = TokenType.FloatLiteral;
            }
            else if (_pos < _source.Length && (_source[_pos] == 'l' || _source[_pos] == 'L'))
            {
                _pos++; _col++;
                value += 'L';
                // 整数 + L 后缀 → long；类型仍为 IntegerLiteral，Parser 按后缀识别
            }

            return new Token(type, value, startLine, startColumn);
        }



        private Token ReadIdentifierOrKeyword()
        {
            int startLine = _line;
            int startColumn = _col;
            int start = _pos;
            
            // 处理 @ 前缀
            if (_source[_pos] == '@')
            {
                _pos++;
                _col++;
            }
            
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || IsChineseChar(_source[_pos]) || _source[_pos] == '_'))
            {
                _pos++;
                _col++;
            }
            
            string value = _source.Substring(start, _pos - start);
            TokenType type = LookupKeyword(value.ToLower(), s_keywords, TokenType.Identifier);
            
            return new Token(type, value, startLine, startColumn);
        }
        
        private Token ReadOperatorOrDelimiter()
        {
            int startLine = _line;
            int startColumn = _col;
            char current = _source[_pos];
            
            // 检查多字符运算符
            string[] multiCharOps = { "==", "!=", "<=", ">=", "&&", "||", "+=", "-=", "*=", "/=", "%=", "++", "--", "??", "?.", "=>", "<<", ">>" };
            
            foreach (var op in multiCharOps)
            {
                if (_pos + op.Length <= _source.Length && _source.Substring(_pos, op.Length) == op)
                {
                    _pos += op.Length;
                    _col += op.Length;
                    return new Token(GetMultiCharOperatorType(op), op, startLine, startColumn);
                }
            }
            
            // 单字符运算符和分隔符
            _pos++;
            _col++;
            
            return current switch
            {
                '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
                '-' => new Token(TokenType.Minus, "-", startLine, startColumn),
                '*' => new Token(TokenType.Multiply, "*", startLine, startColumn),
                '/' => new Token(TokenType.Divide, "/", startLine, startColumn),
                '%' => new Token(TokenType.Modulo, "%", startLine, startColumn),
                '=' => new Token(TokenType.Assignment, "=", startLine, startColumn),
                '<' => new Token(TokenType.LessThan, "<", startLine, startColumn),
                '>' => new Token(TokenType.GreaterThan, ">", startLine, startColumn),
                '!' => new Token(TokenType.LogicalNot, "!", startLine, startColumn),
                '&' => new Token(TokenType.BitwiseAnd, "&", startLine, startColumn),
                '|' => new Token(TokenType.BitwiseOr, "|", startLine, startColumn),
                '^' => new Token(TokenType.BitwiseXor, "^", startLine, startColumn),
                '~' => new Token(TokenType.BitwiseNot, "~", startLine, startColumn),
                '?' => new Token(TokenType.Conditional, "?", startLine, startColumn),
                ':' => new Token(TokenType.Colon, ":", startLine, startColumn),
                '(' => new Token(TokenType.LeftParen, "(", startLine, startColumn),
                ')' => new Token(TokenType.RightParen, ")", startLine, startColumn),
                '{' => new Token(TokenType.LeftBrace, "{", startLine, startColumn),
                '}' => new Token(TokenType.RightBrace, "}", startLine, startColumn),
                '[' => new Token(TokenType.LeftBracket, "[", startLine, startColumn),
                ']' => new Token(TokenType.RightBracket, "]", startLine, startColumn),
                ',' => new Token(TokenType.Comma, ",", startLine, startColumn),
                ';' => new Token(TokenType.Semicolon, ";", startLine, startColumn),
                '.' => new Token(TokenType.Dot, ".", startLine, startColumn),
                _ => new Token(TokenType.Error, current.ToString(), startLine, startColumn)
            };
        }
        
        private static readonly Dictionary<string, TokenType> s_keywords = new()
        {
            {"using",TokenType.Using},{"namespace",TokenType.Namespace},{"class",TokenType.Class},
            {"struct",TokenType.Struct},{"interface",TokenType.Interface},{"enum",TokenType.Enum},
            {"delegate",TokenType.Delegate},{"event",TokenType.Event},
            {"if",TokenType.If},{"else",TokenType.Else},{"switch",TokenType.Switch},{"case",TokenType.Case},
            {"for",TokenType.For},{"foreach",TokenType.Foreach},{"while",TokenType.While},{"do",TokenType.Do},
            {"break",TokenType.Break},{"continue",TokenType.Continue},{"goto",TokenType.Goto},
            {"return",TokenType.Return},{"throw",TokenType.Throw},{"try",TokenType.Try},
            {"catch",TokenType.Catch},{"finally",TokenType.Finally},{"lock",TokenType.Lock},
            {"yield",TokenType.Yield},{"var",TokenType.Var},{"dynamic",TokenType.Dynamic},
            {"object",TokenType.Object},{"string",TokenType.String},{"bool",TokenType.Bool},
            {"byte",TokenType.Byte},{"sbyte",TokenType.SByte},{"short",TokenType.Short},
            {"ushort",TokenType.UShort},{"int",TokenType.Int},{"uint",TokenType.UInt},
            {"long",TokenType.Long},{"ulong",TokenType.ULong},{"float",TokenType.Float},
            {"double",TokenType.Double},{"decimal",TokenType.Decimal},{"char",TokenType.Char},
            {"void",TokenType.Void},{"true",TokenType.True},{"false",TokenType.False},
            {"null",TokenType.Null},{"default",TokenType.Default},
            {"async",TokenType.Async},{"await",TokenType.Await},{"fixed",TokenType.Fixed},
            {"unsafe",TokenType.Unsafe},{"stackalloc",TokenType.Stackalloc},
            {"checked",TokenType.Checked},{"unchecked",TokenType.Unchecked},
            {"partial",TokenType.Partial},{"get",TokenType.Get},{"set",TokenType.Set},
            {"in",TokenType.In},{"is",TokenType.Is},{"as",TokenType.As},
            {"typeof",TokenType.Typeof},{"sizeof",TokenType.Sizeof},{"new",TokenType.New},
            {"public",TokenType.Public},{"private",TokenType.Private},{"protected",TokenType.Protected},
            {"static",TokenType.Static},{"readonly",TokenType.Readonly},{"extern",TokenType.Extern},{"native",TokenType.Native},{"alias",TokenType.Alias},
            {"abstract",TokenType.Abstract},{"virtual",TokenType.Virtual},{"override",TokenType.Override},
            {"sealed",TokenType.Sealed},{"volatile",TokenType.Volatile},{"const",TokenType.Const},
            {"ref",TokenType.Ref},{"out",TokenType.Out},{"this",TokenType.This},{"base",TokenType.Base},
            {"params",TokenType.Params},{"operator",TokenType.Operator},
            {"implicit",TokenType.Implicit},{"explicit",TokenType.Explicit},
            {"add",TokenType.Add},{"remove",TokenType.Remove},{"method",TokenType.Method},
            {"property",TokenType.Property},{"field",TokenType.Field},
            {"constructor",TokenType.Constructor},{"destructor",TokenType.Destructor},
        };
        
        private TokenType GetMultiCharOperatorType(string op)
        {
            return op switch
            {
                "==" => TokenType.Equal,
                "!=" => TokenType.NotEqual,
                "<=" => TokenType.LessThanOrEqual,
                ">=" => TokenType.GreaterThanOrEqual,
                "&&" => TokenType.LogicalAnd,
                "||" => TokenType.LogicalOr,
                "+=" => TokenType.PlusEqual,
                "-=" => TokenType.MinusEqual,
                "*=" => TokenType.MultiplyEqual,
                "/=" => TokenType.DivideEqual,
                "%=" => TokenType.ModuloEqual,
                "++" => TokenType.Increment,
                "--" => TokenType.Decrement,
                "??" => TokenType.NullCoalescing,
                "?." => TokenType.NullConditional,
                "=>" => TokenType.Lambda,
                "<<" => TokenType.LeftShift,
                ">>" => TokenType.RightShift,
                _ => TokenType.OperatorToken
            };
        }
    }
}