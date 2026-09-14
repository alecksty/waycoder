using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace LuaCompiler
{
    /// <summary>
    /// Lua 词法分析器 — 继承 LexerBase 复用字符扫描基础设施
    /// </summary>
    public class Lexer : LexerBase
    {
        public List<Token> Tokens { get; private set; }

        // Lua 关键字
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            { "and", TokenType.AND },
            { "break", TokenType.BREAK },
            { "do", TokenType.DO },
            { "else", TokenType.ELSE },
            { "elseif", TokenType.ELSEIF },
            { "end", TokenType.END },
            { "false", TokenType.FALSE },
            { "for", TokenType.FOR },
            { "function", TokenType.FUNCTION },
            { "goto", TokenType.GOTO },
            { "if", TokenType.IF },
            { "in", TokenType.IN },
            { "local", TokenType.LOCAL },
            { "nil", TokenType.NIL },
            { "not", TokenType.NOT },
            { "or", TokenType.OR },
            { "repeat", TokenType.REPEAT },
            { "return", TokenType.RETURN },
            { "then", TokenType.THEN },
            { "true", TokenType.TRUE },
            { "until", TokenType.UNTIL },
            { "while", TokenType.WHILE }
        };

        public Lexer(string source) : base(source)
        {
            Tokens = new List<Token>();
        }

        private void SkipComment()
        {
            if (Peek() == '-' && Peek(1) == '-')
            {
                Advance(); // 第一个 '-'
                Advance(); // 第二个 '-'

                // 长注释 [[ ... ]]
                if (Peek() == '[' && Peek(1) == '[')
                {
                    Advance(); // '['
                    Advance(); // 第二个 '['
                    SkipLongComment();
                    return;
                }

                // 单行注释
                while (Peek() != '\n' && Peek() != '\0')
                {
                    Advance();
                }
            }
        }

        private void SkipLongComment()
        {
            int depth = 1;
            while (depth > 0 && Peek() != '\0')
            {
                if (Peek() == ']' && Peek(1) == ']')
                {
                    Advance(); // 第一个 ']'
                    Advance(); // 第二个 ']'
                    depth--;
                }
                else if (Peek() == '[' && Peek(1) == '[')
                {
                    Advance(); // 第一个 '['
                    Advance(); // 第二个 '['
                    depth++;
                }
                else
                {
                    Advance();
                }
            }
        }

        private Token ReadNumberToken()
        {
            int startLine = _line;
            int startCol = _col;
            string numStr = base.ReadNumber();
            return new Token(TokenType.NUMBER, numStr, startLine, startCol);
        }

        private Token ReadString(char quote)
        {
            int startLine = _line;
            int startCol = _col;
            string content = ReadStringLiteral(quote);
            return new Token(TokenType.STRING, content, startLine, startCol);
        }

        private Token ReadIdentifierToken()
        {
            int startLine = _line;
            int startCol = _col;
            string value = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_' || LexerHelper.IsChineseChar(c));

            TokenType type = KEYWORDS.ContainsKey(value) ? KEYWORDS[value] : TokenType.IDENTIFIER;
            return new Token(type, value, startLine, startCol);
        }

        public void Tokenize()
        {
            while (Peek() != '\0')
            {
                SkipWhitespace();

                if (Peek() == '\0')
                    break;

                // 注释
                if (Peek() == '-' && Peek(1) == '-')
                {
                    SkipComment();
                    continue;
                }

                // 字符串
                if (Peek() == '"' || Peek() == '\'')
                {
                    Tokens.Add(ReadString(Peek()));
                    continue;
                }

                // 数字
                if (char.IsDigit(Peek()))
                {
                    Tokens.Add(ReadNumberToken());
                    continue;
                }

                // 标识符
                if (char.IsLetter(Peek()) || Peek() == '_' || LexerHelper.IsChineseChar(Peek()))
                {
                    Tokens.Add(ReadIdentifierToken());
                    continue;
                }

                // 运算符和标点
                char c = Advance();
                switch (c)
                {
                    case '+': Tokens.Add(new Token(TokenType.PLUS, "+", _line, _col)); break;
                    case '-': Tokens.Add(new Token(TokenType.MINUS, "-", _line, _col)); break;
                    case '*': Tokens.Add(new Token(TokenType.MUL, "*", _line, _col)); break;
                    case '/':
                        if (Peek() == '/')
                        {
                            Advance();
                            Tokens.Add(new Token(TokenType.FLOOR_DIV, "//", _line, _col));
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.DIV, "/", _line, _col));
                        }
                        break;
                    case '%': Tokens.Add(new Token(TokenType.MOD, "%", _line, _col)); break;
                    case '^': Tokens.Add(new Token(TokenType.POW, "^", _line, _col)); break;
                    case '#': Tokens.Add(new Token(TokenType.LEN, "#", _line, _col)); break;
                    case '=':
                        if (Peek() == '=')
                        {
                            Advance();
                            Tokens.Add(new Token(TokenType.EQ, "==", _line, _col));
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.ASSIGN, "=", _line, _col));
                        }
                        break;
                    case '~':
                        if (Peek() == '=')
                        {
                            Advance();
                            Tokens.Add(new Token(TokenType.NE, "~=", _line, _col));
                        }
                        else
                        {
                            throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"意外的字符: {c} 在第{_line}行{_col}列");
                        }
                        break;
                    case '<':
                        if (Peek() == '=')
                        {
                            Advance();
                            Tokens.Add(new Token(TokenType.LE, "<=", _line, _col));
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.LT, "<", _line, _col));
                        }
                        break;
                    case '>':
                        if (Peek() == '=')
                        {
                            Advance();
                            Tokens.Add(new Token(TokenType.GE, ">=", _line, _col));
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.GT, ">", _line, _col));
                        }
                        break;
                    case '.':
                        if (Peek() == '.')
                        {
                            Advance();
                            if (Peek() == '.')
                            {
                                Advance();
                                Tokens.Add(new Token(TokenType.DOTS, "...", _line, _col));
                            }
                            else
                            {
                                Tokens.Add(new Token(TokenType.CONCAT, "..", _line, _col));
                            }
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.DOT, ".", _line, _col));
                        }
                        break;
                    case '(': Tokens.Add(new Token(TokenType.LPAREN, "(", _line, _col)); break;
                    case ')': Tokens.Add(new Token(TokenType.RPAREN, ")", _line, _col)); break;
                    case '{': Tokens.Add(new Token(TokenType.LBRACE, "{", _line, _col)); break;
                    case '}': Tokens.Add(new Token(TokenType.RBRACE, "}", _line, _col)); break;
                    case '[': Tokens.Add(new Token(TokenType.LBRACKET, "[", _line, _col)); break;
                    case ']': Tokens.Add(new Token(TokenType.RBRACKET, "]", _line, _col)); break;
                    case ',': Tokens.Add(new Token(TokenType.COMMA, ",", _line, _col)); break;
                    case ';': Tokens.Add(new Token(TokenType.SEMICOLON, ";", _line, _col)); break;
                    case ':':
                        if (Peek() == ':') { Advance(); Tokens.Add(new Token(TokenType.DOUBLE_COLON, "::", _line, _col)); }
                        else Tokens.Add(new Token(TokenType.COLON, ":", _line, _col));
                        break;
                    default:
                        if (c == '\r' || c < 32)
                        {
                            // 跳过
                        }
                        else
                        {
                            throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"意外的字符: {c} 在第{_line}行{_col}列");
                        }
                        break;
                }
            }

            Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
        }
    }
}
