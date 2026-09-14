using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace DCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["module"] = TokenType.Module, ["import"] = TokenType.Import,
        ["void"] = TokenType.Void, ["int"] = TokenType.Int,
        ["float"] = TokenType.Float, ["double"] = TokenType.Double,
        ["long"] = TokenType.Long,
        ["bool"] = TokenType.Bool, ["string"] = TokenType.String,
        ["char"] = TokenType.Char, ["class"] = TokenType.Class,
        ["struct"] = TokenType.Struct, ["interface"] = TokenType.Interface,
        ["enum"] = TokenType.Enum, ["if"] = TokenType.If,
        ["else"] = TokenType.Else, ["for"] = TokenType.For,
        ["while"] = TokenType.While, ["do"] = TokenType.Do,
        ["return"] = TokenType.Return, ["true"] = TokenType.True,
        ["false"] = TokenType.False, ["null"] = TokenType.Null,
        ["new"] = TokenType.New, ["this"] = TokenType.This,
        ["foreach"] = TokenType.Foreach, ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["try"] = TokenType.Try, ["catch"] = TokenType.Catch,
        ["finally"] = TokenType.Finally, ["throw"] = TokenType.Throw,
        ["switch"] = TokenType.Switch, ["case"] = TokenType.Case,
        ["default"] = TokenType.Default,
        ["auto"] = TokenType.Auto, ["immutable"] = TokenType.Immutable,
        ["const"] = TokenType.Const, ["scope"] = TokenType.Scope,
        ["union"] = TokenType.Union,
        ["with"] = TokenType.With,
        ["cast"] = TokenType.Cast,
    };

    public Lexer(string source) : base(source) { }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            SkipWhitespace();
            if (Peek() == '\0') break;

            // Line comment //
            if (Peek() == '/' && Peek(1) == '/')
            {
                Advance(); Advance();
                int sl = _line, sc = _col;
                SkipLineComment();
                Tokens.Add(new Token(TokenType.LineComment, "", sl, sc));
                continue;
            }

            // Block comment /* */
            if (Peek() == '/' && Peek(1) == '*')
            {
                Advance(); Advance();
                int sl = _line, sc = _col;
                SkipBlockComment();
                Tokens.Add(new Token(TokenType.BlockComment, "", sl, sc));
                continue;
            }

            // D nested block comment /+ +/ (nesting supported)
            if (Peek() == '/' && Peek(1) == '+')
            {
                Advance(); Advance();
                int sl = _line, sc = _col;
                int depth = 1;
                while (depth > 0 && Peek() != '\0')
                {
                    if (Peek() == '/' && Peek(1) == '+') { Advance(); Advance(); depth++; }
                    else if (Peek() == '+' && Peek(1) == '/') { Advance(); Advance(); depth--; }
                    else Advance();
                }
                Tokens.Add(new Token(TokenType.BlockComment, "", sl, sc));
                continue;
            }

            // String literal (double quote)
            if (Peek() == '"')
            {
                int sl = _line, sc = _col;
                string content = ReadStringLiteral('"');
                Tokens.Add(new Token(TokenType.StringLiteral, content, sl, sc));
                continue;
            }

            // Char literal (single quote)
            if (Peek() == '\'')
            {
                int sl = _line, sc = _col;
                string content = ReadStringLiteral('\'');
                Tokens.Add(new Token(TokenType.CharLiteral, content, sl, sc));
                continue;
            }

            // Numbers
            if (char.IsDigit(Peek()))
            {
                Tokens.Add(ReadNumber());
                continue;
            }

            // Identifiers
            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                Tokens.Add(ReadIdentifierOrKeyword());
                continue;
            }

            char c = Advance();
            switch (c)
            {
                // Operators
                case '+':
                    if (Match('+')) Tokens.Add(new Token(TokenType.Increment, "++", _line, _col));
                    else Tokens.Add(Match('=') ? new Token(TokenType.PlusAssign, "+=", _line, _col) : new Token(TokenType.Plus, "+", _line, _col));
                    break;
                case '-':
                    if (Match('-')) Tokens.Add(new Token(TokenType.Decrement, "--", _line, _col));
                    else Tokens.Add(Match('=') ? new Token(TokenType.MinusAssign, "-=", _line, _col) : new Token(TokenType.Minus, "-", _line, _col));
                    break;
                case '*': Tokens.Add(Match('=') ? new Token(TokenType.MulAssign, "*=", _line, _col) : new Token(TokenType.Mul, "*", _line, _col)); break;
                case '/': Tokens.Add(Match('=') ? new Token(TokenType.DivAssign, "/=", _line, _col) : new Token(TokenType.Div, "/", _line, _col)); break;
                case '%': Tokens.Add(new Token(TokenType.Mod, "%", _line, _col)); break;
                case '^': Tokens.Add(Match('^') ? new Token(TokenType.Pow, "^^", _line, _col) : new Token(TokenType.Caret, "^", _line, _col)); break;
                case '~': Tokens.Add(new Token(TokenType.Tilde, "~", _line, _col)); break;
                case '?': Tokens.Add(new Token(TokenType.Question, "?", _line, _col)); break;
                case '=': Tokens.Add(Match('=') ? new Token(TokenType.Eq, "==", _line, _col) : new Token(TokenType.Assign, "=", _line, _col)); break;
                case '!': Tokens.Add(Match('=') ? new Token(TokenType.Neq, "!=", _line, _col) : new Token(TokenType.Not, "!", _line, _col)); break;
                case '<':
                    if (Match('<')) Tokens.Add(new Token(TokenType.LShift, "<<", _line, _col));
                    else Tokens.Add(Match('=') ? new Token(TokenType.Le, "<=", _line, _col) : new Token(TokenType.Lt, "<", _line, _col));
                    break;
                case '>':
                    if (Match('>')) Tokens.Add(new Token(TokenType.RShift, ">>", _line, _col));
                    else Tokens.Add(Match('=') ? new Token(TokenType.Ge, ">=", _line, _col) : new Token(TokenType.Gt, ">", _line, _col));
                    break;
                case '&': Tokens.Add(Match('&') ? new Token(TokenType.And, "&&", _line, _col) : new Token(TokenType.Amp, "&", _line, _col)); break;
                case '|': Tokens.Add(Match('|') ? new Token(TokenType.Or, "||", _line, _col) : new Token(TokenType.Pipe, "|", _line, _col)); break;

                // Delimiters
                case '(': Tokens.Add(new Token(TokenType.LParen, "(", _line, _col)); break;
                case ')': Tokens.Add(new Token(TokenType.RParen, ")", _line, _col)); break;
                case '{': Tokens.Add(new Token(TokenType.LBrace, "{", _line, _col)); break;
                case '}': Tokens.Add(new Token(TokenType.RBrace, "}", _line, _col)); break;
                case '[': Tokens.Add(new Token(TokenType.LBracket, "[", _line, _col)); break;
                case ']': Tokens.Add(new Token(TokenType.RBracket, "]", _line, _col)); break;
                case ',': Tokens.Add(new Token(TokenType.Comma, ",", _line, _col)); break;
                case ';': Tokens.Add(new Token(TokenType.Semicolon, ";", _line, _col)); break;
                case '.': Tokens.Add(new Token(TokenType.Dot, ".", _line, _col)); break;
                case ':': Tokens.Add(new Token(TokenType.Colon, ":", _line, _col)); break;
                case '@':
                    // D 属性: @safe @nogc @system @trusted — MCU 模式忽略
                    ReadWhile(c => char.IsLetter(c));
                    break;
                case '$':
                    // D 数组长度运算符 $ — MCU 模式替换为常量 0
                    Tokens.Add(new Token(TokenType.Integer, "0", _line, _col));
                    break;

                default: Error($"Unexpected char: {c}"); break;
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    private Token ReadIdentifierOrKeyword()
    {
        int sl = _line, sc = _col;
        string v = ReadIdentifier();
        return Keywords.TryGetValue(v, out var kw) ? new Token(kw, v, sl, sc) : new Token(TokenType.Identifier, v, sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        bool isFloat = false;

        // Hex literal: 0x...
        if (Peek() == '0' && (Peek(1) == 'x' || Peek(1) == 'X'))
        {
            string v = base.ReadNumber(); // uses LexerBase's hex-aware ReadNumber
            return new Token(TokenType.Integer, v, sl, sc);
        }

        var sb = new StringBuilder();
        while (char.IsDigit(Peek()) || Peek() == '.')
        {
            if (Peek() == '.')
            {
                if (Peek(1) == '.') break; // don't eat range operator
                isFloat = true;
            }
            sb.Append(Advance());
        }
        string val = sb.ToString();
        // D 数值后缀: 123456789L, 3.14f — 保留后缀以便解析器区分类型
        if (Peek() == 'L' || Peek() == 'l' || Peek() == 'f' || Peek() == 'F') val += Advance();
        return isFloat ? new Token(TokenType.FloatLiteral, val, sl, sc) : new Token(TokenType.Integer, val, sl, sc);
    }

}
