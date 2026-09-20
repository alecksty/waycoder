using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CompilerBase;

namespace FortranCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["program"] = TokenType.Program,
        ["subroutine"] = TokenType.Subroutine,
        ["function"] = TokenType.Function,
        ["end"] = TokenType.End,
        ["integer"] = TokenType.Integer,
        ["real"] = TokenType.Real,
        ["double"] = TokenType.Double,
        ["complex"] = TokenType.Complex,
        ["logical"] = TokenType.Logical,
        ["character"] = TokenType.Character,
        ["if"] = TokenType.If,
        ["then"] = TokenType.Then,
        ["else"] = TokenType.Else,
        ["do"] = TokenType.Do,
        ["while"] = TokenType.While,
        ["call"] = TokenType.Call,
        ["return"] = TokenType.Return,
        ["stop"] = TokenType.Stop,
        [".true."] = TokenType.True,
        [".false."] = TokenType.False,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["parameter"] = TokenType.Parameter,
        ["print"] = TokenType.Print,
        ["write"] = TokenType.Write,
        ["read"] = TokenType.Read,
        ["contains"] = TokenType.Contains,
        ["implicit"] = TokenType.Implicit,
        ["none"] = TokenType.None,
        // "result" is context-sensitive (only in function result clause);
        // handled specially in parser
        ["dimension"] = TokenType.Dimension,
        ["allocatable"] = TokenType.Allocatable,
        ["allocate"] = TokenType.Allocate,
        ["deallocate"] = TokenType.Deallocate,
        ["intent"] = TokenType.Intent,
        ["in"] = TokenType.In,
        ["out"] = TokenType.Out,
        ["inout"] = TokenType.InOut,
        ["exit"] = TokenType.Exit,
        ["cycle"] = TokenType.Cycle,
        ["module"] = TokenType.Module,
        ["use"] = TokenType.Use,
        ["only"] = TokenType.Only,
        [".and."] = TokenType.LogicalAnd,
        [".or."] = TokenType.LogicalOr,
        [".not."] = TokenType.LogicalNot,
        [".eqv."] = TokenType.LogicalEqv,
        [".neqv."] = TokenType.LogicalNeqv,
        [".eq."] = TokenType.Eq,
        [".ne."] = TokenType.Neq,
        [".lt."] = TokenType.Lt,
        [".gt."] = TokenType.Gt,
        [".le."] = TokenType.Le,
        [".ge."] = TokenType.Ge,
    };

    public Lexer(string source) : base(source) { }

    /// <summary>
    /// Skip spaces and tabs only (preserves newlines for statement separation).
    /// </summary>
    private void SkipHorizontalWhitespace()
    {
        while (!AtEnd && (Peek() == ' ' || Peek() == '\t' || Peek() == '\r'))
            Advance();
    }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            // Fortran free-form: ! anywhere is an inline comment
            if (Peek() == '!')
            {
                SkipLineComment();
                continue;
            }

            // Handle Windows \r\n and Unix \n newlines
            if (Peek() == '\r') Advance(); // skip \r (carriage return)
            if (Peek() == '\n')
            {
                int nl = _line, nc = _col;
                Advance();
                Tokens.Add(new Token(TokenType.Newline, "\\n", nl, nc));
                continue;
            }

            // Skip spaces and tabs only (preserve newlines for statement separation)
            SkipHorizontalWhitespace();
            if (Peek() == '\0') break;

            // After skipping whitespace, check again for comments
            if (Peek() == '!')
            {
                SkipLineComment();
                continue;
            }

            // Fixed-form column 1-5 label area: digits followed by whitespace
            // In fixed form, if we're in columns 1-5 and see a digit, skip it as a label
            // We detect fixed form by checking if after newline we're at col <= 6

            // String literals (Fortran uses single quotes, and '' for escaped quote)
            if (Peek() == '\'')
            {
                Tokens.Add(ReadFortranString());
                continue;
            }

            // Double-quoted strings (extension, some compilers support)
            if (Peek() == '"')
            {
                Tokens.Add(ReadFortranString('"'));
                continue;
            }

            // Dotted keywords: .true., .false., .and., .or., .not., .eq., etc.
            if (Peek() == '.' && Peek(1) != '\0' && char.IsLetter(Peek(1)))
            {
                Tokens.Add(ReadDottedKeyword());
                continue;
            }

            // Numbers
            if (char.IsDigit(Peek()) || (Peek() == '.' && char.IsDigit(Peek(1))))
            {
                Tokens.Add(ReadNumber());
                continue;
            }

            // Identifiers and keywords (Fortran is case-insensitive)
            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                int sl = _line, sc = _col;
                string ident = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_');
                Tokens.Add(Keywords.TryGetValue(ident, out var kw)
                    ? new Token(kw, ident.ToLowerInvariant(), sl, sc)
                    : new Token(TokenType.Identifier, ident, sl, sc));
                continue;
            }

            // Operators and delimiters
            char c = Advance();
            int l = _line, col = _col;
            switch (c)
            {
                case '+': Tokens.Add(new Token(TokenType.Plus, "+", l, col)); break;
                case '-': Tokens.Add(new Token(TokenType.Minus, "-", l, col)); break;
                case '*':
                    if (Match('*')) Tokens.Add(new Token(TokenType.Pow, "**", l, col));
                    else Tokens.Add(new Token(TokenType.Asterisk, "*", l, col));
                    break;
                case '/':
                    if (Match('=')) Tokens.Add(new Token(TokenType.Neq, "/=", l, col));
                    else Tokens.Add(new Token(TokenType.Div, "/", l, col));
                    break;
                case '=':
                    if (Match('=')) Tokens.Add(new Token(TokenType.Eq, "==", l, col));
                    else Tokens.Add(new Token(TokenType.Assign, "=", l, col));
                    break;
                case '<':
                    if (Match('=')) Tokens.Add(new Token(TokenType.Le, "<=", l, col));
                    else Tokens.Add(new Token(TokenType.Lt, "<", l, col));
                    break;
                case '>':
                    if (Match('=')) Tokens.Add(new Token(TokenType.Ge, ">=", l, col));
                    else Tokens.Add(new Token(TokenType.Gt, ">", l, col));
                    break;
                case '(': Tokens.Add(new Token(TokenType.LParen, "(", l, col)); break;
                case ')': Tokens.Add(new Token(TokenType.RParen, ")", l, col)); break;
                case ',': Tokens.Add(new Token(TokenType.Comma, ",", l, col)); break;
                case ':':
                    if (Match(':')) Tokens.Add(new Token(TokenType.ColonColon, "::", l, col));
                    else Tokens.Add(new Token(TokenType.Colon, ":", l, col));
                    break;
                case ';': Tokens.Add(new Token(TokenType.Semicolon, ";", l, col)); break;
                case '.':
                    Tokens.Add(new Token(TokenType.Dot, ".", l, col));
                    break;
                default:
                    throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"意外的字符: {c} (0x{(int)c:X2})（位置 {l}:{col}）");
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    /// <summary>
    /// Read a dotted keyword: .true., .false., .and., .or., .not., .eq., .ne., etc.
    /// </summary>
    private Token ReadDottedKeyword()
    {
        int sl = _line, sc = _col;
        var sb = new StringBuilder();
        sb.Append(Advance()); // leading dot
        while (!AtEnd && (char.IsLetter(Peek()) || Peek() == '_'))
            sb.Append(Advance());
        if (!AtEnd && Peek() == '.')
            sb.Append(Advance()); // trailing dot
        string kw = sb.ToString();
        return Keywords.TryGetValue(kw, out var type)
            ? new Token(type, kw.ToLowerInvariant(), sl, sc)
            : new Token(TokenType.Dot, ".", sl, sc);
    }

    /// <summary>
    /// Read a Fortran single-quoted string. '' inside is an escaped single quote.
    /// </summary>
    private Token ReadFortranString(char quote = '\'')
    {
        int sl = _line, sc = _col;
        Advance(); // skip opening quote
        var sb = new StringBuilder();
        while (!AtEnd)
        {
            if (Peek() == quote)
            {
                Advance();
                if (Peek() == quote)
                {
                    // Double quote = escaped quote
                    sb.Append(quote);
                    Advance();
                }
                else
                {
                    // End of string
                    break;
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }
        return new Token(TokenType.StringLiteral, sb.ToString(), sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        bool isReal = false;
        var sb = new StringBuilder();
        while (char.IsDigit(Peek()) || Peek() == '.')
        {
            if (Peek() == '.')
            {
                if (isReal) break; // second dot, stop
                isReal = true;
            }
            sb.Append(Advance());
        }
        // Scientific notation: 1.0E+10 or 1.0D+10 (double precision)
        bool isDouble = false;
        if (Peek() == 'E' || Peek() == 'e' || Peek() == 'D' || Peek() == 'd')
        {
            isDouble = (Peek() == 'D' || Peek() == 'd');
            sb.Append(Advance());
            if (Peek() == '+' || Peek() == '-') sb.Append(Advance());
            while (char.IsDigit(Peek())) sb.Append(Advance());
            isReal = true;
        }
        // Underscore kind specifier: 1.0_8
        if (Peek() == '_')
        {
            sb.Append(Advance());
            while (char.IsDigit(Peek()) || char.IsLetter(Peek())) sb.Append(Advance());
        }
        string v = sb.ToString();
        if (isReal)
            return isDouble
                ? new Token(TokenType.DoubleLiteral, v, sl, sc)
                : new Token(TokenType.RealLiteral, v, sl, sc);
        return new Token(TokenType.IntLiteral, v, sl, sc);
    }

}
