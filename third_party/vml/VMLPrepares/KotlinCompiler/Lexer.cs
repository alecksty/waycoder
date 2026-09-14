using CompilerBase;
using System.Text;
namespace KotlinCompiler;
public class Lexer(string source) : LexerBase(source) {
    static readonly HashSet<string> Keywords = ["fun","val","var","if","else","when","for","while","do","return","break","continue","true","false","null","is","in","println","print","Int","String","Boolean","Unit","class","data","until","downTo","step","interface","sealed","and","or","xor","shl","shr","try","catch","finally","external","object"];
    // Uses base.Peek() and base.Advance() from LexerBase
    public List<Token> Tokenize() {
        var tokens = new List<Token>();
        while (_pos < _source.Length) {
            char c = Peek(); int sl = _line, sc = _col;
            if (char.IsWhiteSpace(c)) { Advance(); continue; }
            if (c == '/' && Peek(1) == '/') { while (Peek() != '\n' && Peek() != '\0') Advance(); continue; }
            if (c == '"') { tokens.Add(ReadString(sl, sc)); continue; }
            if (c == '(') { Advance(); tokens.Add(new Token(TokenType.LPAREN, "(", sl, sc)); }
            else if (c == ')') { Advance(); tokens.Add(new Token(TokenType.RPAREN, ")", sl, sc)); }
            else if (c == '{') { Advance(); tokens.Add(new Token(TokenType.LBRACE, "{", sl, sc)); }
            else if (c == '}') { Advance(); tokens.Add(new Token(TokenType.RBRACE, "}", sl, sc)); }
            else if (c == ',') { Advance(); tokens.Add(new Token(TokenType.COMMA, ",", sl, sc)); }
            else if (c == '+' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.PLUS_ASSIGN, "+=", sl, sc)); }
            else if (c == '-' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.MINUS_ASSIGN, "-=", sl, sc)); }
            else if (c == '*' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.STAR_ASSIGN, "*=", sl, sc)); }
            else if (c == '/' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.SLASH_ASSIGN, "/=", sl, sc)); }
            else if (c == '%' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.PERCENT_ASSIGN, "%=", sl, sc)); }
            else if (c == '-' && Peek(1) == '>') { Advance(); Advance(); tokens.Add(new Token(TokenType.ARROW, "->", sl, sc)); }
            else if (c == ':') { Advance(); tokens.Add(new Token(TokenType.COLON, ":", sl, sc)); }
            else if (c == '+' && Peek(1) == '+') { Advance(); Advance(); tokens.Add(new Token(TokenType.INCREMENT, "++", sl, sc)); }
            else if (c == '-' && Peek(1) == '-') { Advance(); Advance(); tokens.Add(new Token(TokenType.DECREMENT, "--", sl, sc)); }
            else if (c == '+') { Advance(); tokens.Add(new Token(TokenType.PLUS, "+", sl, sc)); }
            else if (c == '-') { Advance(); tokens.Add(new Token(TokenType.MINUS, "-", sl, sc)); }
            else if (c == '*') { Advance(); tokens.Add(new Token(TokenType.STAR, "*", sl, sc)); }
            else if (c == '/') { Advance(); tokens.Add(new Token(TokenType.SLASH, "/", sl, sc)); }
            else if (c == '%') { Advance(); tokens.Add(new Token(TokenType.PERCENT, "%", sl, sc)); }
            else if (c == '=' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.EQ, "==", sl, sc)); }
            else if (c == '!' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.NE, "!=", sl, sc)); }
            else if (c == '<' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.LE, "<=", sl, sc)); }
            else if (c == '>' && Peek(1) == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.GE, ">=", sl, sc)); }
            else if (c == '<') { Advance(); tokens.Add(new Token(TokenType.LT, "<", sl, sc)); }
            else if (c == '>') { Advance(); tokens.Add(new Token(TokenType.GT, ">", sl, sc)); }
            else if (c == '&' && Peek(1) == '&') { Advance(); Advance(); tokens.Add(new Token(TokenType.AND, "&&", sl, sc)); }
            else if (c == '|' && Peek(1) == '|') { Advance(); Advance(); tokens.Add(new Token(TokenType.OR, "||", sl, sc)); }
            else if (c == '!') { Advance(); tokens.Add(new Token(TokenType.NOT, "!", sl, sc)); }
            else if (c == '=') { Advance(); tokens.Add(new Token(TokenType.ASSIGN, "=", sl, sc)); }
            else if (c == '.' && Peek(1) == '.') { Advance(); Advance(); tokens.Add(new Token(TokenType.DOTDOT, "..", sl, sc)); }
            else if (c == '?' && Peek(1) == '.') { Advance(); Advance(); tokens.Add(new Token(TokenType.SAFE_DOT, "?.", sl, sc)); }
            else if (c == '?' && Peek(1) == ':') { Advance(); Advance(); tokens.Add(new Token(TokenType.ELVIS, "?:", sl, sc)); }
            else if (c == '.') { Advance(); tokens.Add(new Token(TokenType.DOT, ".", sl, sc)); }
            else if (c == ';') { Advance(); tokens.Add(new Token(TokenType.SEMICOLON, ";", sl, sc)); }
            else if (c == '[') { Advance(); tokens.Add(new Token(TokenType.LBRACKET, "[", sl, sc)); }
            else if (c == ']') { Advance(); tokens.Add(new Token(TokenType.RBRACKET, "]", sl, sc)); }
            else if (char.IsDigit(c)) tokens.Add(ReadNumber(sl, sc));
            else if (char.IsLetter(c) || c == '_') tokens.Add(ReadWord(sl, sc));
            else Advance();
        }
        tokens.Add(new Token(TokenType.EOF, "", _line, _col)); return tokens;
    }
    Token ReadString(int sl, int sc) {
        Advance(); var sb = new System.Text.StringBuilder();
        while (Peek() != '"' && Peek() != '\0') { sb.Append(Peek() == '\\' ? ReadEscape() : Advance()); }
        if (Peek() == '"') Advance();
        return new Token(TokenType.STRING, sb.ToString(), sl, sc);
    }
    Token ReadNumber(int sl, int sc) { var sb = new System.Text.StringBuilder(); while (char.IsDigit(Peek())) sb.Append(Advance()); if (Peek() == '.' && char.IsDigit(Peek(1))) { sb.Append('.'); Advance(); while (char.IsDigit(Peek())) sb.Append(Advance()); } char suf = Peek(); if (suf == 'f' || suf == 'F' || suf == 'L') { sb.Append(Advance()); } return new Token(TokenType.NUMBER, sb.ToString(), sl, sc); }
    Token ReadWord(int sl, int sc) { var sb = new System.Text.StringBuilder(); while (char.IsLetterOrDigit(Peek()) || Peek() == '_') sb.Append(Advance()); string v = sb.ToString(); return new Token(Keywords.Contains(v) ? TokenType.KEYWORD : TokenType.IDENTIFIER, v, sl, sc); }
}
