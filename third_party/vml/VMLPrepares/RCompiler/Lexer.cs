using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace RCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["function"] = TokenType.Function,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["for"] = TokenType.For,
        ["while"] = TokenType.While,
        ["repeat"] = TokenType.Repeat,
        ["return"] = TokenType.Return,
        ["TRUE"] = TokenType.True,
        ["FALSE"] = TokenType.False,
        ["NULL"] = TokenType.Null,
        ["NA"] = TokenType.Na,
        ["Inf"] = TokenType.Inf,
        ["in"] = TokenType.In,
        ["break"] = TokenType.Break,
        ["next"] = TokenType.Next,
    };

    public Lexer(string source) : base(source) { }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            SkipWhitespace();
            if (Peek() == '\0') break;

            if (Peek() == '#')
            {
                while (Peek() != '\n' && Peek() != '\0') Advance();
                continue;
            }

            if (Peek() == '"' || Peek() == '\'')
            {
                Tokens.Add(ReadString(Peek()));
                continue;
            }

            if (Peek() == '`')
            {
                Tokens.Add(ReadBacktickString());
                continue;
            }

            if (char.IsDigit(Peek()))
            {
                Tokens.Add(ReadNumber());
                continue;
            }

            if (Peek() == '.')
            {
                if (char.IsDigit(Peek(1)))
                {
                    Tokens.Add(ReadNumber());
                    continue;
                }
                // standalone dot could be an identifier start in R
                if (char.IsLetter(Peek(1)) || Peek(1) == '_')
                {
                    Tokens.Add(ReadIdentifier());
                    continue;
                }
                Advance();
                Tokens.Add(new Token(TokenType.Dot, ".", _line, _col));
                continue;
            }

            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                Tokens.Add(ReadIdentifier());
                continue;
            }

            char c = Advance();
            switch (c)
            {
                case '+': Tokens.Add(new Token(TokenType.Plus, "+", _line, _col)); break;
                case '-':
                    if (Peek() == '>') { Advance(); Tokens.Add(new Token(TokenType.Assign, "<-", _line, _col)); break; }
                    Tokens.Add(new Token(TokenType.Minus, "-", _line, _col));
                    break;
                case '*': Tokens.Add(new Token(TokenType.Mul, "*", _line, _col)); break;
                case '/': Tokens.Add(new Token(TokenType.Div, "/", _line, _col)); break;
                case '^': Tokens.Add(new Token(TokenType.Pow, "^", _line, _col)); break;
                case '%':
                    if (Peek() == '%') { Advance(); Tokens.Add(new Token(TokenType.Mod, "%%", _line, _col)); break; }
                    // %xxx% custom infix operator like %in%
                    {
                        int sl = _line, sc = _col;
                        var sb2 = new StringBuilder();
                        sb2.Append('%');
                        while (!AtEnd && Peek() != '%')
                        {
                            if (Peek() == '\n' || Peek() == '\r') break;
                            sb2.Append(Advance());
                        }
                        if (Peek() == '%')
                        {
                            sb2.Append(Advance());
                            Tokens.Add(new Token(TokenType.PercentOp, sb2.ToString(), sl, sc));
                        }
                        else
                        {
                            string val = sb2.ToString();
                            if (val == "%")
                                Error("Unexpected char: %");
                        }
                    }
                    break;
                case '=':
                    if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.Eq, "==", _line, _col)); break; }
                    Tokens.Add(new Token(TokenType.Assign, "=", _line, _col));
                    break;
                case '!':
                    if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.Neq, "!=", _line, _col)); break; }
                    Tokens.Add(new Token(TokenType.Not, "!", _line, _col));
                    break;
                case '<':
                    if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.Le, "<=", _line, _col)); break; }
                    if (Peek() == '-') { Advance(); Tokens.Add(new Token(TokenType.Assign, "<-", _line, _col)); break; }
                    if (Peek() == '<')
                    {
                        Advance();
                        if (Peek() == '-') { Advance(); Tokens.Add(new Token(TokenType.SuperAssign, "<<-", _line, _col)); break; }
                        Error($"Unexpected char after <<: {Peek()}");
                    }
                    Tokens.Add(new Token(TokenType.Lt, "<", _line, _col));
                    break;
                case '>':
                    if (Peek() == '=') { Advance(); Tokens.Add(new Token(TokenType.Ge, ">=", _line, _col)); break; }
                    Tokens.Add(new Token(TokenType.Gt, ">", _line, _col));
                    break;
                case '&': if (Match('&')) { } Tokens.Add(new Token(TokenType.And, "&", _line, _col)); break;
                case '|': if (Match('|')) { } Tokens.Add(new Token(TokenType.Or, "|", _line, _col)); break;
                case '(': Tokens.Add(new Token(TokenType.LParen, "(", _line, _col)); break;
                case ')': Tokens.Add(new Token(TokenType.RParen, ")", _line, _col)); break;
                case '{': Tokens.Add(new Token(TokenType.LBrace, "{", _line, _col)); break;
                case '}': Tokens.Add(new Token(TokenType.RBrace, "}", _line, _col)); break;
                case '[': Tokens.Add(new Token(TokenType.LBracket, "[", _line, _col)); break;
                case ']': Tokens.Add(new Token(TokenType.RBracket, "]", _line, _col)); break;
                case ',': Tokens.Add(new Token(TokenType.Comma, ",", _line, _col)); break;
                case ';': Tokens.Add(new Token(TokenType.Semicolon, ";", _line, _col)); break;
                case '~': Tokens.Add(new Token(TokenType.Tilde, "~", _line, _col)); break;
                case ':': Tokens.Add(new Token(TokenType.Colon, ":", _line, _col)); break;
                case '$': Tokens.Add(new Token(TokenType.Dollar, "$", _line, _col)); break;
                default:
                    Error($"Unexpected char: {c}"); break;
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    private new Token ReadIdentifier()
    {
        int sl = _line, sc = _col;
        string v = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
        return Keywords.TryGetValue(v, out var kw) ? new Token(kw, v, sl, sc) : new Token(TokenType.Identifier, v, sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        bool isFloat = false;
        var sb = new StringBuilder();

        // digits before decimal
        while (char.IsDigit(Peek()))
            sb.Append(Advance());

        // decimal point
        if (Peek() == '.')
        {
            isFloat = true;
            sb.Append(Advance());
            while (char.IsDigit(Peek()))
                sb.Append(Advance());
        }

        // scientific notation: e+ or e-
        if (Peek() == 'e' || Peek() == 'E')
        {
            isFloat = true;
            sb.Append(Advance());
            if (Peek() == '+' || Peek() == '-')
                sb.Append(Advance());
            while (char.IsDigit(Peek()))
                sb.Append(Advance());
        }

        string v = sb.ToString();
        if (isFloat)
        {
            return new Token(TokenType.Float, v, sl, sc);
        }
        return new Token(TokenType.Integer, v, sl, sc);
    }

    private Token ReadString(char quote)
    {
        int sl = _line, sc = _col;
        string content = ReadStringLiteral(quote);
        return new Token(TokenType.String, content, sl, sc);
    }

    private Token ReadBacktickString()
    {
        int sl = _line, sc = _col;
        string content = ReadStringLiteral('`');
        return new Token(TokenType.String, content, sl, sc);
    }
}
