namespace LadderCompiler
{
    /// <summary>
    /// 梯形图词法单元
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        
        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override string ToString()
        {
            return $"Token({Type}, '{Value}', L{Line}:C{Column})";
        }
    }
}