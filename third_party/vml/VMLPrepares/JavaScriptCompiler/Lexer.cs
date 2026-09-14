using CompilerBase;
using System.Collections.Generic;

namespace JavaScriptCompiler
{
    /// <summary>
    /// JavaScript 词法分析器 — 从源代码生成 Token 列表
    /// </summary>
    public class Lexer : LexerBase
    {

        public Lexer(string source) : base(source) { }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int position = 0;
            int line = 1;
            int column = 1;
            bool inTemplateLiteral = false;
            bool inTemplateExpr = false;
            int templateBraceDepth = 0;

            while (position < _source.Length)
            {
                char current = _source[position];

                // Template literal: static text mode (between backticks, not inside ${...})
                if (inTemplateLiteral && !inTemplateExpr)
                {
                    if (current == '`')
                    {
                        position++; column++;
                        tokens.Add(new Token(TokenType.Backtick, "`", line, column - 1));
                        inTemplateLiteral = false;
                        continue;
                    }
                    if (current == '$' && position + 1 < _source.Length && _source[position + 1] == '{')
                    {
                        position += 2; column += 2;
                        tokens.Add(new Token(TokenType.TemplateInterp, "${", line, column - 2));
                        inTemplateExpr = true;
                        templateBraceDepth = 1;
                        continue;
                    }
                    int tStart = position;
                    while (position < _source.Length)
                    {
                        if (_source[position] == '`') break;
                        if (_source[position] == '$' && position + 1 < _source.Length && _source[position + 1] == '{') break;
                        if (_source[position] == '\n') { line++; column = 1; }
                        else { column++; }
                        position++;
                    }
                    if (position > tStart)
                    {
                        string text = _source.Substring(tStart, position - tStart);
                        tokens.Add(new Token(TokenType.StringLiteral, text, line, column));
                    }
                    continue;
                }

                // 跳过空白字符 + 注释 (using LexerBase)
                if (char.IsWhiteSpace(current) || (current == '/' && position + 1 < _source.Length && (_source[position + 1] == '/' || _source[position + 1] == '*')))
                {
                    // Sync position tracking before using base methods
                    _pos = position; _line = line; _col = column;
                    if (current == '/' && position + 1 < _source.Length && _source[position + 1] == '/')
                        SkipLineComment();
                    else if (current == '/' && position + 1 < _source.Length && _source[position + 1] == '*')
                        SkipBlockComment();
                    else
                        SkipWhitespace();
                    // Read back position tracking
                    position = _pos; line = _line; column = _col;
                    continue;
                }

                // 字符串字面量 (using LexerBase.ReadStringLiteral)
                if (current == '"' || current == '\'')
                {
                    _pos = position; _line = line; _col = column;
                    string value = ReadStringLiteral(current);
                    position = _pos; line = _line; column = _col;
                    tokens.Add(new Token(TokenType.StringLiteral, value, line, column));
                    continue;
                }

                // 数字字面量 (using LexerBase.ReadNumber)
                if (char.IsDigit(current) || (current == '0' && position + 1 < _source.Length && (_source[position + 1] == 'x' || _source[position + 1] == 'X')))
                {
                    _pos = position; _line = line; _col = column;
                    string value = ReadNumber();
                    position = _pos; line = _line; column = _col;
                    tokens.Add(new Token(TokenType.NumberLiteral, value, line, column));
                    continue;
                }

                // 标识符和关键字 (using LexerBase.ReadIdentifier)
                if (char.IsLetter(current) || current == '_' || current == '$')
                {
                    _pos = position; _line = line; _col = column;
                    string value = ReadIdentifier();
                    position = _pos; line = _line; column = _col;
                    TokenType type = GetKeywordType(value);
                    tokens.Add(new Token(type, value, line, column));
                    continue;
                }

                // 模板字符串开始
                if (current == '`')
                {
                    tokens.Add(new Token(TokenType.Backtick, "`", line, column));
                    position++; column++;
                    inTemplateLiteral = true;
                    inTemplateExpr = false;
                    templateBraceDepth = 0;
                    continue;
                }

                // 运算符和分隔符
                TokenType? operatorType = GetOperatorType(position);
                if (operatorType.HasValue)
                {
                    string op = GetOperatorString(operatorType.Value);
                    tokens.Add(new Token(operatorType.Value, op, line, column));
                    position += op.Length;
                    column += op.Length;
                    continue;
                }

                // 分隔符
                TokenType? delimiterType = GetDelimiterType(current);
                if (delimiterType.HasValue)
                {
                    tokens.Add(new Token(delimiterType.Value, current.ToString(), line, column));
                    position++; column++;
                    if (inTemplateExpr)
                    {
                        if (delimiterType.Value == TokenType.LeftBrace)
                            templateBraceDepth++;
                        else if (delimiterType.Value == TokenType.RightBrace)
                        {
                            templateBraceDepth--;
                            if (templateBraceDepth == 0)
                                inTemplateExpr = false;
                        }
                    }
                    continue;
                }

                // 未知字符
                tokens.Add(new Token(TokenType.Unknown, current.ToString(), line, column));
                position++; column++;
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", line, column));
            return tokens;
        }

        private TokenType? GetOperatorType(int position)
        {
            if (position + 3 <= _source.Length)
            {
                string threeChar = _source.Substring(position, 3);
                switch (threeChar)
                {
                    case "===": return TokenType.StrictEqual;
                    case "!==": return TokenType.StrictNotEqual;
                    case ">>>": return TokenType.UnsignedRightShift;
                }
            }

            if (position + 2 <= _source.Length)
            {
                string twoChar = _source.Substring(position, 2);
                switch (twoChar)
                {
                    case "=>": return TokenType.Arrow;
                    case "==": return TokenType.Equal;
                    case "!=": return TokenType.NotEqual;
                    case "<=": return TokenType.LessThanOrEqual;
                    case ">=": return TokenType.GreaterThanOrEqual;
                    case "&&": return TokenType.LogicalAnd;
                    case "||": return TokenType.LogicalOr;
                    case "++": return TokenType.Increment;
                    case "--": return TokenType.Decrement;
                    case "+=": return TokenType.PlusAssign;
                    case "-=": return TokenType.MinusAssign;
                    case "*=": return TokenType.MultiplyAssign;
                    case "/=": return TokenType.DivideAssign;
                    case "%=": return TokenType.ModuloAssign;
                    case "**": return TokenType.Exponent;
                    case "<<": return TokenType.LeftShift;
                    case ">>": return TokenType.RightShift;
                    case "${": return TokenType.TemplateInterp;
                }
            }

            if (position < _source.Length)
            {
                char oneChar = _source[position];
                switch (oneChar)
                {
                    case '=': return TokenType.Assign;
                    case '+': return TokenType.Plus;
                    case '-': return TokenType.Minus;
                    case '*': return TokenType.Multiply;
                    case '/': return TokenType.Divide;
                    case '%': return TokenType.Modulo;
                    case '<': return TokenType.LessThan;
                    case '>': return TokenType.GreaterThan;
                    case '!': return TokenType.LogicalNot;
                    case '&': return TokenType.BitwiseAnd;
                    case '|': return TokenType.BitwiseOr;
                    case '^': return TokenType.BitwiseXor;
                    case '~': return TokenType.BitwiseNot;
                }
            }

            return null;
        }

        private static string GetOperatorString(TokenType type)
        {
            return type switch
            {
                TokenType.StrictEqual => "===",
                TokenType.StrictNotEqual => "!==",
                TokenType.UnsignedRightShift => ">>>",
                TokenType.Arrow => "=>",
                TokenType.Equal => "==",
                TokenType.NotEqual => "!=",
                TokenType.LessThanOrEqual => "<=",
                TokenType.GreaterThanOrEqual => ">=",
                TokenType.LogicalAnd => "&&",
                TokenType.LogicalOr => "||",
                TokenType.Increment => "++",
                TokenType.Decrement => "--",
                TokenType.PlusAssign => "+=",
                TokenType.MinusAssign => "-=",
                TokenType.MultiplyAssign => "*=",
                TokenType.DivideAssign => "/=",
                TokenType.ModuloAssign => "%=",
                TokenType.Exponent => "**",
                TokenType.LeftShift => "<<",
                TokenType.RightShift => ">>",
                TokenType.Assign => "=",
                TokenType.Plus => "+",
                TokenType.Minus => "-",
                TokenType.Multiply => "*",
                TokenType.Divide => "/",
                TokenType.Modulo => "%",
                TokenType.LessThan => "<",
                TokenType.GreaterThan => ">",
                TokenType.LogicalNot => "!",
                TokenType.BitwiseAnd => "&",
                TokenType.BitwiseOr => "|",
                TokenType.BitwiseXor => "^",
                TokenType.BitwiseNot => "~",
                _ => type.ToString()
            };
        }

        private static TokenType? GetDelimiterType(char ch)
        {
            return ch switch
            {
                '(' => TokenType.LeftParen,
                ')' => TokenType.RightParen,
                '{' => TokenType.LeftBrace,
                '}' => TokenType.RightBrace,
                '[' => TokenType.LeftBracket,
                ']' => TokenType.RightBracket,
                ',' => TokenType.Comma,
                ';' => TokenType.Semicolon,
                ':' => TokenType.Colon,
                '.' => TokenType.Dot,
                '?' => TokenType.Question,
                '`' => TokenType.Backtick,
                _ => null
            };
        }

        private static TokenType GetKeywordType(string value)
        {
            return value switch
            {
                "var" => TokenType.Keyword,
                "let" => TokenType.Keyword,
                "const" => TokenType.Keyword,
                "function" => TokenType.Keyword,
                "return" => TokenType.Keyword,
                "if" => TokenType.Keyword,
                "else" => TokenType.Keyword,
                "for" => TokenType.Keyword,
                "while" => TokenType.Keyword,
                "do" => TokenType.Keyword,
                "switch" => TokenType.Keyword,
                "case" => TokenType.Keyword,
                "default" => TokenType.Keyword,
                "break" => TokenType.Keyword,
                "continue" => TokenType.Keyword,
                "try" => TokenType.Keyword,
                "catch" => TokenType.Keyword,
                "finally" => TokenType.Keyword,
                "throw" => TokenType.Keyword,
                "new" => TokenType.Keyword,
                "delete" => TokenType.Keyword,
                "typeof" => TokenType.Keyword,
                "instanceof" => TokenType.Keyword,
                "in" => TokenType.Keyword,
                "of" => TokenType.Keyword,
                "this" => TokenType.Keyword,
                "super" => TokenType.Keyword,
                "class" => TokenType.Keyword,
                "extends" => TokenType.Keyword,
                "import" => TokenType.Keyword,
                "export" => TokenType.Keyword,
                "async" => TokenType.Keyword,
                "native" => TokenType.Keyword,
                "await" => TokenType.Keyword,
                "yield" => TokenType.Keyword,
                "true" => TokenType.True,
                "false" => TokenType.False,
                "null" => TokenType.Null,
                "undefined" => TokenType.Undefined,
                _ => TokenType.Identifier
            };
        }
    }
}
