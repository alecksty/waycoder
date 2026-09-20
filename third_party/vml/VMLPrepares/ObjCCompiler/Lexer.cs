using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace ObjCCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        // C keywords
        ["int"] = TokenType.Int, ["void"] = TokenType.Void, ["char"] = TokenType.Char,
        ["float"] = TokenType.Float, ["double"] = TokenType.Double, ["short"] = TokenType.Short,
        ["long"] = TokenType.Long, ["signed"] = TokenType.Signed, ["unsigned"] = TokenType.Unsigned,
        ["if"] = TokenType.If, ["else"] = TokenType.Else, ["while"] = TokenType.While,
        ["for"] = TokenType.For, ["return"] = TokenType.Return, ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue, ["enum"] = TokenType.Enum, ["struct"] = TokenType.Struct,
        ["union"] = TokenType.Union, ["switch"] = TokenType.Switch, ["case"] = TokenType.Case,
        ["default"] = TokenType.Default, ["const"] = TokenType.Const, ["do"] = TokenType.Do,
        ["goto"] = TokenType.Goto, ["static"] = TokenType.Static, ["typedef"] = TokenType.Typedef,
        ["extern"] = TokenType.Extern, ["native"] = TokenType.Native,
        // ObjC keywords
        ["id"] = TokenType.IdType, ["Class"] = TokenType.Class, ["super"] = TokenType.Super,
        ["self"] = TokenType.Self, ["nil"] = TokenType.NilObj, ["YES"] = TokenType.YesObj,
        ["NO"] = TokenType.NoObj,
    };

    public Lexer(string source) : base(source) { }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            SkipWhitespace();
            if (Peek() == '\0') break;
            if (Peek() == '\n') { Advance(); Tokens.Add(new Token(TokenType.Newline, "\\n", _line, _col)); continue; }

            if (Peek() == '@')
            {
                Advance(); // skip @
                int sl = _line, sc = _col;
                string word = ReadWhile(c => char.IsLetter(c));
                switch (word)
                {
                    case "interface": Tokens.Add(new Token(TokenType.Interface, "@interface", sl, sc)); break;
                    case "implementation": Tokens.Add(new Token(TokenType.Implementation, "@implementation", sl, sc)); break;
                    case "protocol": Tokens.Add(new Token(TokenType.Protocol, "@protocol", sl, sc)); break;
                    case "end": Tokens.Add(new Token(TokenType.End, "@end", sl, sc)); break;
                    case "property": Tokens.Add(new Token(TokenType.Property, "@property", sl, sc)); break;
                    case "synthesize": Tokens.Add(new Token(TokenType.Synthesize, "@synthesize", sl, sc)); break;
                    case "dynamic": Tokens.Add(new Token(TokenType.Dynamic, "@dynamic", sl, sc)); break;
                    case "selector": Tokens.Add(new Token(TokenType.Selector, "@selector", sl, sc)); break;
                    case "class": Tokens.Add(new Token(TokenType.Class, "@class", sl, sc)); break;
                    case "try": Tokens.Add(new Token(TokenType.Try, "@try", sl, sc)); break;
                    case "catch": Tokens.Add(new Token(TokenType.Catch, "@catch", sl, sc)); break;
                    case "finally": Tokens.Add(new Token(TokenType.Finally, "@finally", sl, sc)); break;
                    case "throw": Tokens.Add(new Token(TokenType.Throw, "@throw", sl, sc)); break;
                    case "synchronized": Tokens.Add(new Token(TokenType.Synchronized, "@synchronized", sl, sc)); break;
                    case "autoreleasepool": Tokens.Add(new Token(TokenType.Autoreleasepool, "@autoreleasepool", sl, sc)); break;
                    case "public":
                    case "private":
                    case "protected":
                        Tokens.Add(new Token(TokenType.AtSign, "@" + word, sl, sc)); break; // 访问修饰符
                    case "\"": Tokens.Add(new Token(TokenType.ObjCString, ReadStringLiteral('"'), sl, sc)); break;
                    default: Tokens.Add(new Token(TokenType.AtSign, "@" + word, sl, sc)); break;
                }
                continue;
            }

            if (Peek() == '/' && Peek(1) == '/') { while (Peek() != '\n' && Peek() != '\0') Advance(); continue; }
            if (Peek() == '/' && Peek(1) == '*') { Advance(); Advance(); while (Peek() != '\0' && !(Peek() == '*' && Peek(1) == '/')) Advance(); Advance(); Advance(); continue; }

            if (Peek() == '"') { Tokens.Add(ReadString('"')); continue; }
            if (Peek() == '\'') { Tokens.Add(ReadChar()); continue; }

            if (Peek() == '#') { Advance(); Tokens.Add(new Token(TokenType.Hash, "#", _line, _col)); continue; }

            if (char.IsDigit(Peek())) { Tokens.Add(ReadNumber()); continue; }
            if (char.IsLetter(Peek()) || Peek() == '_') { Tokens.Add(ReadIdentifier()); continue; }

            char c = Advance();
            switch (c)
            {
                case '+': Tokens.Add(Match('+') ? new Token(TokenType.Increment, "++", _line, _col) : Match('=') ? new Token(TokenType.PlusAssign, "+=", _line, _col) : new Token(TokenType.Plus, "+", _line, _col)); break;
                case '-': Tokens.Add(Match('-') ? new Token(TokenType.Decrement, "--", _line, _col) : Match('=') ? new Token(TokenType.MinusAssign, "-=", _line, _col) : Match('>') ? new Token(TokenType.Arrow, "->", _line, _col) : new Token(TokenType.Minus, "-", _line, _col)); break;
                case '*': Tokens.Add(Match('=') ? new Token(TokenType.MulAssign, "*=", _line, _col) : new Token(TokenType.Star, "*", _line, _col)); break;
                case '/': Tokens.Add(Match('=') ? new Token(TokenType.DivAssign, "/=", _line, _col) : new Token(TokenType.Slash, "/", _line, _col)); break;
                case '%': Tokens.Add(Match('=') ? new Token(TokenType.ModAssign, "%=", _line, _col) : new Token(TokenType.Percent, "%", _line, _col)); break;
                case '=': Tokens.Add(Match('=') ? new Token(TokenType.Eq, "==", _line, _col) : new Token(TokenType.Assign, "=", _line, _col)); break;
                case '!': Tokens.Add(Match('=') ? new Token(TokenType.Neq, "!=", _line, _col) : new Token(TokenType.Not, "!", _line, _col)); break;
                case '<': Tokens.Add(Match('<') ? new Token(TokenType.LShift, "<<", _line, _col) : Match('=') ? new Token(TokenType.Le, "<=", _line, _col) : new Token(TokenType.Lt, "<", _line, _col)); break;
                case '>': Tokens.Add(Match('>') ? new Token(TokenType.RShift, ">>", _line, _col) : Match('=') ? new Token(TokenType.Ge, ">=", _line, _col) : new Token(TokenType.Gt, ">", _line, _col)); break;
                case '&': Tokens.Add(Match('&') ? new Token(TokenType.And, "&&", _line, _col) : new Token(TokenType.Amp, "&", _line, _col)); break;
                case '|': Tokens.Add(Match('|') ? new Token(TokenType.Or, "||", _line, _col) : new Token(TokenType.Pipe, "|", _line, _col)); break;
                case '^': Tokens.Add(new Token(TokenType.Caret, "^", _line, _col)); break;
                case '~': Tokens.Add(new Token(TokenType.Tilde, "~", _line, _col)); break;
                case '(': Tokens.Add(new Token(TokenType.LParen, "(", _line, _col)); break;
                case ')': Tokens.Add(new Token(TokenType.RParen, ")", _line, _col)); break;
                case '{': Tokens.Add(new Token(TokenType.LBrace, "{", _line, _col)); break;
                case '}': Tokens.Add(new Token(TokenType.RBrace, "}", _line, _col)); break;
                case '[': Tokens.Add(new Token(TokenType.LBracket, "[", _line, _col)); break;
                case ']': Tokens.Add(new Token(TokenType.RBracket, "]", _line, _col)); break;
                case ',': Tokens.Add(new Token(TokenType.Comma, ",", _line, _col)); break;
                case ';': Tokens.Add(new Token(TokenType.Semicolon, ";", _line, _col)); break;
                case ':': Tokens.Add(new Token(TokenType.Colon, ":", _line, _col)); break;
                case '?': Tokens.Add(new Token(TokenType.Question, "?", _line, _col)); break;
                case '.': Tokens.Add(Match('.') ? (Match('.') ? new Token(TokenType.Ellipsis, "...", _line, _col) : new Token(TokenType.Dot, "..", _line, _col)) : new Token(TokenType.Dot, ".", _line, _col)); break;
                default: Error($"意外的字符: {c}"); break;
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    private new Token ReadIdentifier()
    {
        int sl = _line, sc = _col;
        string v = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_');
        return Keywords.TryGetValue(v, out var kw) ? new Token(kw, v, sl, sc) : new Token(TokenType.Identifier, v, sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        var sb = new StringBuilder();
        while (char.IsDigit(Peek()) || Peek() == '.')
        {
            sb.Append(Advance());
        }
        // C 数字后缀: 3.14f, 123L, 123LL, 123U, 123UL, 123ULL, 123LU
        char c = Peek();
        while (c == 'f' || c == 'F' || c == 'l' || c == 'L' || c == 'u' || c == 'U')
        {
            sb.Append(Advance());
            c = Peek();
        }
        return new Token(TokenType.Number, sb.ToString(), sl, sc);
    }

    private Token ReadString(char quote)
    {
        int sl = _line, sc = _col;
        string content = ReadStringLiteral(quote);
        return new Token(TokenType.String, content, sl, sc);
    }

    private Token ReadChar()
    {
        int sl = _line, sc = _col;
        string content = ReadStringLiteral('\'');
        // 字符字面量: 转义序列由 ReadStringLiteral 处理, 取第一个字符
        char ch = content.Length > 0 ? content[0] : '\0';
        return new Token(TokenType.CharLiteral, ch.ToString(), sl, sc);
    }

}
