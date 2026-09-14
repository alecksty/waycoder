namespace CppCompiler
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line = 0, int column = 0)
        {
            Type = type; Value = value; Line = line; Column = column;
        }

        public override string ToString() => $"Token({Type}, '{Value}')";
    }
}
