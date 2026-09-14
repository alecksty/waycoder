namespace GoCompiler
{
    /// <summary>
    /// Go语言词法单元
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public object Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string SourceLine { get; set; }
        public string OriginalFile { get; set; }
        public int OriginalLine { get; set; }

        public Token(TokenType type, object value, int line = 0, int column = 0, string sourceLine = null, string originalFile = null, int originalLine = 0)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
            SourceLine = sourceLine;
            OriginalFile = originalFile;
            OriginalLine = originalLine;
        }

        public override string ToString()
        {
            return $"Token({Type.ToString()}, {Value}, {Line}:{Column})";
        }
    }
}
