using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace DartCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["class"] = TokenType.Class, ["void"] = TokenType.Void,
        ["int"] = TokenType.Int, ["double"] = TokenType.DoubleKw,
        ["String"] = TokenType.StringKw, ["bool"] = TokenType.Bool,
        ["var"] = TokenType.Var, ["final"] = TokenType.Final,
        ["if"] = TokenType.If, ["else"] = TokenType.Else,
        ["for"] = TokenType.For, ["while"] = TokenType.While,
        ["return"] = TokenType.Return, ["true"] = TokenType.True,
        ["false"] = TokenType.False, ["null"] = TokenType.Null,
        ["do"] = TokenType.DoKw, ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["mixin"] = TokenType.Mixin, ["with"] = TokenType.WithKw,
        ["enum"] = TokenType.Enum, ["extension"] = TokenType.Extension,
        ["on"] = TokenType.OnKw,
        ["is"] = TokenType.IsKw, ["as"] = TokenType.AsKw,
        ["try"] = TokenType.TryKw, ["catch"] = TokenType.CatchKw,
        ["throw"] = TokenType.ThrowKw, ["finally"] = TokenType.FinallyKw,
        ["external"] = TokenType.External,
    };

    public Lexer(string source) : base(source) { }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            SkipWhitespace();
            if (Peek() == '\0') break;

            // Line comment
            if (Peek() == '/' && Peek(1) == '/')
            {
                SkipLineComment();
                continue;
            }

            // Block comment
            if (Peek() == '/' && Peek(1) == '*')
            {
                Advance(); Advance(); // skip /*
                SkipBlockComment();
                continue;
            }

            // String literal
            if (Peek() == '"')
            {
                int sl = _line, sc = _col;
                string content = ReadStringLiteral('"');
                Tokens.Add(new Token(TokenType.StringLit, content, sl, sc));
                continue;
            }

            // Numbers
            if (char.IsDigit(Peek()))
            {
                Tokens.Add(ReadNumber());
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                Tokens.Add(ReadIdentifierOrKeyword());
                continue;
            }

            // Operators and delimiters
            char c = Advance();
            switch (c)
            {
                case '+':
                    if (Match('+')) Tokens.Add(MakeToken(TokenType.Increment, "++"));
                    else Tokens.Add(Match('=') ? MakeToken(TokenType.PlusAssign, "+=") : MakeToken(TokenType.Plus, "+"));
                    break;
                case '-':
                    if (Match('-')) Tokens.Add(MakeToken(TokenType.Decrement, "--"));
                    else Tokens.Add(Match('=') ? MakeToken(TokenType.MinusAssign, "-=") : MakeToken(TokenType.Minus, "-"));
                    break;
                case '*': Tokens.Add(Match('=') ? MakeToken(TokenType.MulAssign, "*=") : MakeToken(TokenType.Mul, "*")); break;
                case '/': Tokens.Add(Match('=') ? MakeToken(TokenType.DivAssign, "/=") : MakeToken(TokenType.Div, "/")); break;
                case '%': Tokens.Add(Match('=') ? MakeToken(TokenType.ModAssign, "%=") : MakeToken(TokenType.Mod, "%")); break;
                case '=': Tokens.Add(Match('=') ? MakeToken(TokenType.Eq, "==") : MakeToken(TokenType.Assign, "=")); break;
                case '!': Tokens.Add(Match('=') ? MakeToken(TokenType.Neq, "!=") : MakeToken(TokenType.Not, "!")); break;
                case '<': Tokens.Add(Match('=') ? MakeToken(TokenType.Le, "<=") : MakeToken(TokenType.Lt, "<")); break;
                case '>': Tokens.Add(Match('=') ? MakeToken(TokenType.Ge, ">=") : MakeToken(TokenType.Gt, ">")); break;
                case '&': Tokens.Add(Match('&') ? MakeToken(TokenType.AndAnd, "&&") : MakeToken(TokenType.AndAnd, "&")); break;
                case '|': Tokens.Add(Match('|') ? MakeToken(TokenType.OrOr, "||") : MakeToken(TokenType.OrOr, "|")); break;
                case '(': Tokens.Add(MakeToken(TokenType.LParen, "(")); break;
                case ')': Tokens.Add(MakeToken(TokenType.RParen, ")")); break;
                case '{': Tokens.Add(MakeToken(TokenType.LBrace, "{")); break;
                case '}': Tokens.Add(MakeToken(TokenType.RBrace, "}")); break;
                case '[': Tokens.Add(MakeToken(TokenType.LBracket, "[")); break;
                case ']': Tokens.Add(MakeToken(TokenType.RBracket, "]")); break;
                case ',': Tokens.Add(MakeToken(TokenType.Comma, ",")); break;
                case ';': Tokens.Add(MakeToken(TokenType.Semicolon, ";")); break;
                case '.': Tokens.Add(MakeToken(TokenType.Dot, ".")); break;
                case '~': Tokens.Add(Match('/') ? MakeToken(TokenType.IntDiv, "~/") : MakeToken(TokenType.BitNot, "~")); break;
                case '^': Tokens.Add(MakeToken(TokenType.BitNot, "^")); break; // bitwise XOR
                case '?': Tokens.Add(MakeToken(TokenType.Question, "?")); break;
                case ':': Tokens.Add(MakeToken(TokenType.Colon, ":")); break;
                default: throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"意外的字符: {c}（位置 {_line}:{_col}）");
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    private Token ReadIdentifierOrKeyword()
    {
        int sl = _line, sc = _col;
        string v = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_');
        return Keywords.TryGetValue(v, out var kw)
            ? new Token(kw, v, sl, sc)
            : new Token(TokenType.Identifier, v, sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        bool isFloat = false;
        var sb = new StringBuilder();
        while (char.IsDigit(Peek()) || Peek() == '.')
        {
            if (Peek() == '.')
            {
                if (Peek(1) == '.') break; // avoid eating range dots
                isFloat = true;
            }
            sb.Append(Advance());
        }
        string v = sb.ToString();
        return isFloat
            ? new Token(TokenType.Float, v, sl, sc)
            : new Token(TokenType.Integer, v, sl, sc);
    }

    private void Expect(char expected)
    {
        if (Advance() != expected)
            throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"Expected '{expected}' at {_line}:{_col}");
    }

    private Token MakeToken(TokenType type, string value)
        => new Token(type, value, _line, _col);
}
