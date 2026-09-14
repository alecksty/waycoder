namespace KotlinCompiler;
public class Token(TokenType type, string value, int line, int col) {
    public TokenType Type => type;
    public string Value => value;
    public int Line => line;
    public int Column => col;
}
