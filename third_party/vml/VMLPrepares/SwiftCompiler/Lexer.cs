using System;
using System.Collections.Generic;
using CompilerBase;

namespace SwiftCompiler
{
    /// <summary>
    /// Swift词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        private List<Token> tokens;
        
        private static readonly Dictionary<string, TokenType> KEYWORDS = new Dictionary<string, TokenType>
        {
            // 声明关键字
            { "import", TokenType.Import },
            { "class", TokenType.Class },
            { "struct", TokenType.Struct },
            { "enum", TokenType.Enum },
            { "protocol", TokenType.Protocol },
            { "extension", TokenType.Extension },
            { "func", TokenType.Func },
            { "var", TokenType.Var },
            { "let", TokenType.Let },
            
            // 控制流关键字
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "switch", TokenType.Switch },
            { "case", TokenType.Case },
            { "default", TokenType.Default },
            { "for", TokenType.For },
            { "while", TokenType.While },
            { "repeat", TokenType.Repeat },
            { "do", TokenType.Do },
            { "in", TokenType.In },
            
            // 跳转关键字
            { "return", TokenType.Return },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "fallthrough", TokenType.Fallthrough },
            { "defer", TokenType.Defer },
            { "guard", TokenType.Guard },
            { "throw", TokenType.Throw },
            { "throws", TokenType.Throws },
            { "rethrows", TokenType.Rethrows },
            { "try", TokenType.Try },
            { "catch", TokenType.Catch },
            
            // 异步关键字
            { "async", TokenType.Async },
            { "await", TokenType.Await },
            { "actor", TokenType.Actor },
            
            // 类型关键字
            { "some", TokenType.Some },
            { "any", TokenType.Any },
            { "self", TokenType.Self },
            { "super", TokenType.Super },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "nil", TokenType.Nil },
            
            // 类型声明关键字
            { "typealias", TokenType.Typealias },
            { "associatedtype", TokenType.Associatedtype },
            { "where", TokenType.Where },
            
            // 修饰符关键字
            { "convenience", TokenType.Convenience },
            { "dynamic", TokenType.Dynamic },
            { "final", TokenType.Final },
            { "infix", TokenType.Infix },
            { "lazy", TokenType.Lazy },
            { "mutating", TokenType.Mutating },
            { "nonmutating", TokenType.Nonmutating },
            { "optional", TokenType.Optional },
            { "override", TokenType.Override },
            { "postfix", TokenType.Postfix },
            { "prefix", TokenType.Prefix },
            { "required", TokenType.Required },
            { "static", TokenType.Static },
            { "unowned", TokenType.Unowned },
            { "weak", TokenType.Weak },
            
            // 访问控制关键字
            { "private", TokenType.Private },
            { "fileprivate", TokenType.Fileprivate },
            { "internal", TokenType.Internal },
            { "public", TokenType.Public },
            { "open", TokenType.Open },
            { "native", TokenType.Native },
            { "external", TokenType.External },

            // 类型关键字
            { "Int", TokenType.Int },
            { "Double", TokenType.Double },
            { "Float", TokenType.Float },
            { "Bool", TokenType.Bool },
            { "String", TokenType.String },
            { "Character", TokenType.Character },
            { "Array", TokenType.Array },
            { "Dictionary", TokenType.Dictionary },
            { "Set", TokenType.Set }
        };
        
        public Lexer(string source) : base(source)
        {
            tokens = new List<Token>();
        }
        
        public List<Token> Tokenize()
        {
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                // 跳过空白字符
                if (char.IsWhiteSpace(current))
                {
                    SkipWhitespace();
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
                
                // 字符串字面量
                if (current == '"')
                {
                    ReadString();
                    continue;
                }
                
                // 数字字面量
                if (char.IsDigit(current))
                {
                    ReadNumber();
                    continue;
                }
                
                // 标识符和关键字
                if (char.IsLetter(current) || IsChineseChar(current) || current == '_')
                {
                    ReadIdentifier();
                    continue;
                }
                
                // 运算符和分隔符
                if (TryReadOperator())
                {
                    continue;
                }
                
                // 未知字符
                AddToken(TokenType.Error, current.ToString());
                _pos++;
                _col++;
            }
            
            // 添加文件结束标记
            AddToken(TokenType.EndOfFile, "");
            
            return tokens;
        }
        
        
        private void ReadString()
        {
            int startColumn = _col;
            _pos++; // 跳过开头的 "
            _col++;
            
            int start = _pos;
            bool inInterpolation = false;
            
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                if (current == '\\' && _pos + 1 < _source.Length)
                {
                    // 转义字符
                    _pos += 2;
                    _col += 2;
                }
                else if (current == '"' && !inInterpolation)
                {
                    // 字符串结束
                    break;
                }
                else if (current == '(' && _pos > 0 && _source[_pos - 1] == '\\')
                {
                    // 字符串插值开始
                    inInterpolation = true;
                    _pos++;
                    _col++;
                }
                else if (current == ')' && inInterpolation)
                {
                    // 字符串插值结束
                    inInterpolation = false;
                    _pos++;
                    _col++;
                }
                else
                {
                    _pos++;
                    _col++;
                }
            }
            
            string value = _source.Substring(start, _pos - start);
            AddToken(TokenType.StringLiteral, value, _line, startColumn);
            
            if (_pos < _source.Length && _source[_pos] == '"')
            {
                _pos++;
                _col++;
            }
        }
        
        private new void ReadNumber()
        {
            int start = _pos;
            int startColumn = _col;
            bool hasDecimal = false;
            bool hasExponent = false;
            
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                if (char.IsDigit(current))
                {
                    _pos++;
                    _col++;
                }
                else if (current == '.' && !hasDecimal && !hasExponent)
                {
                    // 检查是否是范围运算符 ... 或 ..< 的一部分，如果是则不作为小数点
                    if (_pos + 1 < _source.Length && _source[_pos + 1] == '.')
                    {
                        break;
                    }
                    hasDecimal = true;
                    _pos++;
                    _col++;
                }
                else if ((current == 'e' || current == 'E') && !hasExponent)
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
                else
                {
                    break;
                }
            }
            
            string value = _source.Substring(start, _pos - start);
            TokenType type = hasDecimal ? TokenType.FloatLiteral : TokenType.IntegerLiteral;
            AddToken(type, value, _line, startColumn);
        }
        


        private new void ReadIdentifier()
        {
            int start = _pos;
            int startColumn = _col;
            
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || IsChineseChar(_source[_pos]) || _source[_pos] == '_'))
            {
                _pos++;
                _col++;
            }
            
            string value = _source.Substring(start, _pos - start);
            
            if (KEYWORDS.TryGetValue(value, out TokenType type))
            {
                AddToken(type, value, _line, startColumn);
            }
            else
            {
                AddToken(TokenType.Identifier, value, _line, startColumn);
            }
        }
        
        private bool TryReadOperator()
        {
            char current = _source[_pos];
            int startColumn = _col;
            
            // 多字符运算符
            string[] multiCharOperators = {
                "==", "!=", "<=", ">=", "&&", "||", "+=", "-=", "*=", "/=", "%=",
                "->", "??", "...", "..<", "?.", "!."
            };
            
            foreach (var op in multiCharOperators)
            {
                if (_pos + op.Length <= _source.Length && _source.Substring(_pos, op.Length) == op)
                {
                    TokenType type = GetOperatorType(op);
                    AddToken(type, op, _line, startColumn);
                    _pos += op.Length;
                    _col += op.Length;
                    return true;
                }
            }
            
            // 单字符运算符
            TokenType? singleCharType = GetSingleCharOperatorType(current);
            if (singleCharType.HasValue)
            {
                AddToken(singleCharType.Value, current.ToString(), _line, startColumn);
                _pos++;
                _col++;
                return true;
            }
            
            return false;
        }
        
        private TokenType GetOperatorType(string op)
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
                "->" => TokenType.Arrow,
                "??" => TokenType.NilCoalescing,
                "..." => TokenType.Range,
                "..<" => TokenType.HalfOpenRange,
                "?." => TokenType.OptionalChaining,
                "!." => TokenType.ForceUnwrap,
                _ => TokenType.Operator
            };
        }
        
        private TokenType? GetSingleCharOperatorType(char c)
        {
            return c switch
            {
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Multiply,
                '/' => TokenType.Divide,
                '%' => TokenType.Modulo,
                '<' => TokenType.LessThan,
                '>' => TokenType.GreaterThan,
                '!' => TokenType.LogicalNot,
                '&' => TokenType.BitwiseAnd,
                '|' => TokenType.BitwiseOr,
                '^' => TokenType.BitwiseXor,
                '~' => TokenType.BitwiseNot,
                '=' => TokenType.Assignment,
                '?' => TokenType.Question,
                '.' => TokenType.Dot,
                ':' => TokenType.Colon,
                ',' => TokenType.Comma,
                ';' => TokenType.Semicolon,
                '(' => TokenType.LeftParen,
                ')' => TokenType.RightParen,
                '{' => TokenType.LeftBrace,
                '}' => TokenType.RightBrace,
                '[' => TokenType.LeftBracket,
                ']' => TokenType.RightBracket,
                '@' => TokenType.At,
                '#' => TokenType.Hash,
                '`' => TokenType.Backtick,
                _ => (TokenType?)null
            };
        }
        
        private void AddToken(TokenType type, string value, int _line, int _col)
        {
            tokens.Add(new Token(type, value, _line, _col));
        }
        
        private void AddToken(TokenType type, string value)
        {
            tokens.Add(new Token(type, value, _line, _col));
        }
    }
}