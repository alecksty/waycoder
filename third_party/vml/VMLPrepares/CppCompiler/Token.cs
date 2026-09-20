namespace CppCompiler
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        /// <summary>**预处理拼接后**的行号（`#include` 会把头文件内容拼进同一个流）。</summary>
        public int Line { get; }
        public int Column { get; }

        /// <summary>
        /// **原文件**路径；<c>null</c> = 未知（没有行号映射时）。
        /// 与 <see cref="OriginalLine"/> 一起由 <c>Lexer.Tokenize</c> 末尾统一用
        /// <c>Preprocessor.LineMap</c> 填（**不在这 8 个构造点各映射一次**）。
        /// </summary>
        public string? OriginalFile { get; internal set; }

        /// <summary>**原文件**里的行号（1-based）；<b>0 = 未知</b>（报错时退回 <see cref="Line"/>）。</summary>
        public int OriginalLine { get; internal set; }

        public Token(TokenType type, string value, int line = 0, int column = 0)
        {
            Type = type; Value = value; Line = line; Column = column;
        }

        public override string ToString() => $"Token({Type}, '{Value}')";
    }
}
