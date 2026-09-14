namespace CCompiler
{
    /// <summary>
    /// 词法单元
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public object Value { get; set; }
        public int Line { get; set; } // 预处理后的行号
        public int Column { get; set; }
        public string SourceLine { get; set; } // 存储该 Token 所在行的源代码内容
        public string OriginalFile { get; set; } // 原始文件路径
        public int OriginalLine { get; set; } // 原始文件中的行号

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">Token 类型</param>
        /// <param name="value">Token 值</param>
        /// <param name="line">Token 所在行号</param>
        /// <param name="column">Token 所在列号</param>
        /// <param name="sourceLine">Token 所在行的源代码内容</param>
        /// <param name="originalFile">Token 所在原始文件路径</param>
        /// <param name="originalLine">Token 所在原始文件中的行号</param>
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