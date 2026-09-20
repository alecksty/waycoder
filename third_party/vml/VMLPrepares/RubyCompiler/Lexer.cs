using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace RubyCompiler;

public class Lexer : LexerBase
{
    public List<Token> Tokens { get; } = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["def"] = TokenType.Def, ["end"] = TokenType.End, ["class"] = TokenType.Class,
        ["module"] = TokenType.Module, ["if"] = TokenType.If, ["elsif"] = TokenType.Elsif,
        ["else"] = TokenType.Else, ["unless"] = TokenType.Unless, ["while"] = TokenType.While,
        ["until"] = TokenType.Until, ["for"] = TokenType.For, ["in"] = TokenType.In,
        ["do"] = TokenType.Do, ["return"] = TokenType.Return, ["yield"] = TokenType.Yield,
        ["self"] = TokenType.Self, ["nil"] = TokenType.Nil, ["true"] = TokenType.True,
        ["false"] = TokenType.False, ["and"] = TokenType.And, ["or"] = TokenType.Or,
        ["not"] = TokenType.Not, ["begin"] = TokenType.Begin, ["rescue"] = TokenType.Rescue,
        ["include"] = TokenType.Include, ["extend"] = TokenType.Extend,
        ["ensure"] = TokenType.Ensure, ["raise"] = TokenType.RaiseKw,
        ["case"] = TokenType.Case, ["when"] = TokenType.When,
        ["native"] = TokenType.Native,
    };

    public Lexer(string source) : base(source) { }

    public void Tokenize()
    {
        while (Peek() != '\0')
        {
            SkipWhitespace();
            if (Peek() == '\0') break;
            if (Peek() == '#') { while (Peek() != '\n' && Peek() != '\0') Advance(); continue; }
            if (Peek() == '"' || Peek() == '\'') { Tokens.Add(ReadString(Peek())); continue; }
            if (Peek() == ':') { Advance(); Tokens.Add(new Token(TokenType.Symbol, ":" + ReadIdentifier(), _line, _col)); continue; }
            if (char.IsDigit(Peek())) { Tokens.Add(ReadNumber()); continue; }
            if (char.IsLetter(Peek()) || Peek() == '_' || Peek() == '@' || Peek() == '$') { Tokens.Add(ReadIdentifier()); continue; }
            char c = Advance();
            switch (c)
            {
                case '+': Tokens.Add(Match('=') ? new Token(TokenType.PlusAssign, "+=", _line, _col) : new Token(TokenType.Plus, "+", _line, _col)); break;
                case '-': Tokens.Add(Match('=') ? new Token(TokenType.MinusAssign, "-=", _line, _col) : new Token(TokenType.Minus, "-", _line, _col)); break;
                case '*': Tokens.Add(Match('*') ? new Token(TokenType.Pow, "**", _line, _col) : Match('=') ? new Token(TokenType.MulAssign, "*=", _line, _col) : new Token(TokenType.Mul, "*", _line, _col)); break;
                case '/': Tokens.Add(Match('=') ? new Token(TokenType.DivAssign, "/=", _line, _col) : new Token(TokenType.Div, "/", _line, _col)); break;
                case '%': Tokens.Add(new Token(TokenType.Mod, "%", _line, _col)); break;
                case '=': Tokens.Add(Match('=') ? new Token(TokenType.Eq, "==", _line, _col) : Match('>') ? new Token(TokenType.FatArrow, "=>", _line, _col) : new Token(TokenType.Assign, "=", _line, _col)); break;
                case '!': Tokens.Add(Match('=') ? new Token(TokenType.Neq, "!=", _line, _col) : new Token(TokenType.Not, "!", _line, _col)); break;
                case '<': Tokens.Add(Match('=') ? (Match('>') ? new Token(TokenType.Cmp, "<=>", _line, _col) : new Token(TokenType.Le, "<=", _line, _col)) : new Token(TokenType.Lt, "<", _line, _col)); break;
                case '>': Tokens.Add(Match('=') ? new Token(TokenType.Ge, ">=", _line, _col) : new Token(TokenType.Gt, ">", _line, _col)); break;
                case '(': Tokens.Add(new Token(TokenType.LParen, "(", _line, _col)); break;
                case ')': Tokens.Add(new Token(TokenType.RParen, ")", _line, _col)); break;
                case '{': Tokens.Add(new Token(TokenType.LBrace, "{", _line, _col)); break;
                case '}': Tokens.Add(new Token(TokenType.RBrace, "}", _line, _col)); break;
                case '[': Tokens.Add(new Token(TokenType.LBracket, "[", _line, _col)); break;
                case ']': Tokens.Add(new Token(TokenType.RBracket, "]", _line, _col)); break;
                case ',': Tokens.Add(new Token(TokenType.Comma, ",", _line, _col)); break;
                case '.': Tokens.Add(Match('.') ? (Match('.') ? new Token(TokenType.RangeEx, "...", _line, _col) : new Token(TokenType.Range, "..", _line, _col)) : new Token(TokenType.Dot, ".", _line, _col)); break;
                case '\n': Tokens.Add(new Token(TokenType.Newline, "\\n", _line, _col)); break;
                case ';': Tokens.Add(new Token(TokenType.Semicolon, ";", _line, _col)); break;
                case '?': Tokens.Add(new Token(TokenType.Question, "?", _line, _col)); break;
                // `&&` / `||` —— 此前词法器完全不认 `&` 与 `|`，一写就是
                // `Unexpected char: &`，于是「多个条件」只能写成嵌套 if
                // （`Examples/ruby/catch.rb` 开头那段注释记的就是这个坑）。
                // 词法产出**源码原文**（"&&"/"||"），由代码生成那边与 `and`/`or` 并列识别。
                case '&':
                    if (Match('&')) { Tokens.Add(new Token(TokenType.And, "&&", _line, _col)); break; }
                    throw new ParseException(ErrorCode.Lexer_UnknownCharacter,
                        $"Unexpected char: & at {_line}:{_col}（本前端只支持逻辑 `&&`/`||`，位运算 `& | ^` 尚未实现）");
                case '|':
                    if (Match('|')) { Tokens.Add(new Token(TokenType.Or, "||", _line, _col)); break; }
                    throw new ParseException(ErrorCode.Lexer_UnknownCharacter,
                        $"Unexpected char: | at {_line}:{_col}（本前端只支持逻辑 `&&`/`||`，位运算 `& | ^` 尚未实现）");
                default: throw new ParseException(ErrorCode.Lexer_UnknownCharacter, $"意外的字符: {c}（位置 {_line}:{_col}）");
            }
        }
        Tokens.Add(new Token(TokenType.EOF, "", _line, _col));
    }

    private new Token ReadIdentifier()
    {
        int sl = _line, sc = _col;
        string v = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_' || c == '?' || c == '!' || c == '@' || c == '$');
        return Keywords.TryGetValue(v, out var kw) ? new Token(kw, v, sl, sc) : new Token(TokenType.Identifier, v, sl, sc);
    }

    private new Token ReadNumber()
    {
        int sl = _line, sc = _col;
        bool isFloat = false;
        var sb = new StringBuilder();
        while (char.IsDigit(Peek()) || (Peek() == '.' && !isFloat))
        {
            if (Peek() == '.')
            {
                // don't consume dot if it's the start of a range (..)
                if (Peek(1) == '.') break;
                isFloat = true;
            }
            sb.Append(Advance());
        }
        string v = sb.ToString();
        return isFloat ? new Token(TokenType.Float, v, sl, sc) : new Token(TokenType.Integer, v, sl, sc);
    }

    private Token ReadString(char quote)
    {
        int sl = _line, sc = _col;
        string content = ReadStringLiteral(quote);
        return new Token(TokenType.String, content, sl, sc);
    }

}
