namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言词法单元
    /// </summary>
    public class Token : CompilerBase.ITokenPosition
    {
        public TokenType Type { get; }
        public object Value { get; }
        public int Line { get; }
        public int Column { get; }
        public string SourceLine { get; }

        public Token(TokenType type, object value, int line, int column, string sourceLine = null)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
            SourceLine = sourceLine;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) at {Line}:{Column}";
        }
    }
}