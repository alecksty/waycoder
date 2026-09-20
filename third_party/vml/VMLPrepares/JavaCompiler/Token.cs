namespace JavaCompiler
{
    /// <summary>
    /// Java词法单元
    /// </summary>
    public class Token : CompilerBase.ITokenPosition
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }
        
        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override string ToString()
        {
            return $"Token({Type}, '{Value}', line:{Line}, col:{Column})";
        }
    }
}