using CompilerBase;
namespace SchemeCompiler;
public class Lexer(string source) : LexerBase(source) {
    // Uses base.Peek() and base.Advance() from LexerBase

    public List<Token> Tokenize() {
        var tokens = new List<Token>();
        while (_pos < _source.Length) {
            char c = Peek(); int sl = _line, sc = _col;
            if (char.IsWhiteSpace(c)) { Advance(); continue; }
            if (c == ';') { while (Peek() != '\n' && Peek() != '\0') Advance(); continue; }
            if (c == '(') { Advance(); tokens.Add(new Token(TokenType.LPAREN, "(", sl, sc)); continue; }
            if (c == ')') { Advance(); tokens.Add(new Token(TokenType.RPAREN, ")", sl, sc)); continue; }
            if (c == '"') { tokens.Add(ReadString(sl, sc)); continue; }
            if (c == '#') {
                Advance();
                if (Peek() == 't' || Peek() == 'f') { bool v = Peek() == 't'; Advance(); tokens.Add(new Token(TokenType.TRUE, v ? "#t" : "#f", sl, sc)); continue; }
                if (Peek() == '(') { Advance(); tokens.Add(new Token(TokenType.LPAREN, "(", sl, sc)); tokens.Add(new Token(TokenType.SYMBOL, "vector", sl, sc)); continue; }
                // Unknown # sequence — skip
                continue;
            }
            if (c == '\'') { Advance(); tokens.Add(new Token(TokenType.QUOTE, "'", sl, sc)); continue; }
            tokens.Add(char.IsDigit(c) || (c == '-' && char.IsDigit(Peek(1))) ? ReadNumber(sl, sc) : ReadSymbol(sl, sc));
        }
        tokens.Add(new Token(TokenType.EOF, "", _line, _col));
        return tokens;
    }

    Token ReadString(int sl, int sc) {
        Advance(); var sb = new System.Text.StringBuilder();
        while (Peek() != '"' && Peek() != '\0') { if (Peek() == '\\') { Advance(); } sb.Append(Advance()); }
        if (Peek() == '"') Advance();
        return new Token(TokenType.STRING, sb.ToString(), sl, sc);
    }
    Token ReadNumber(int sl, int sc) { var sb = new System.Text.StringBuilder(); sb.Append(Advance()); while (char.IsDigit(Peek())) sb.Append(Advance()); if (Peek() == '.') { sb.Append('.'); Advance(); while (char.IsDigit(Peek())) sb.Append(Advance()); } return new Token(TokenType.NUMBER, sb.ToString(), sl, sc); }
    Token ReadSymbol(int sl, int sc) { var sb = new System.Text.StringBuilder(); while (!char.IsWhiteSpace(Peek()) && Peek() != '(' && Peek() != ')' && Peek() != '"' && Peek() != ';' && Peek() != '\0') sb.Append(Advance()); return new Token(TokenType.SYMBOL, sb.ToString(), sl, sc); }
}
public class Token(TokenType t, string v, int l, int c) { public TokenType Type => t; public string Value => v; public int Line => l; public int Column => c; }
