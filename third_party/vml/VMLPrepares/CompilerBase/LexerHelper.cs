namespace CompilerBase
{
    /// <summary>
    /// 词法分析公共辅助方法，供所有编译器的 Lexer 调用。
    /// </summary>
    public static class LexerHelper
    {
        /// <summary>
        /// 判断字符是否为汉字（覆盖 Unicode 基本区及各扩展区）。
        /// </summary>
        public static bool IsChineseChar(char ch)
        {
            return (ch >= 0x4e00 && ch <= 0x9fff) ||   // 基本汉字
                   (ch >= 0x3400 && ch <= 0x4dbf) ||   // 扩展 A
                   (ch >= 0x20000 && ch <= 0x2a6df) ||  // 扩展 B
                   (ch >= 0x2a700 && ch <= 0x2b73f) ||  // 扩展 C
                   (ch >= 0x2b740 && ch <= 0x2b81f) ||  // 扩展 D
                   (ch >= 0x2b820 && ch <= 0x2ceaf) ||  // 扩展 E
                   (ch >= 0xf900 && ch <= 0xfaff) ||    // 兼容汉字
                   (ch >= 0x2f800 && ch <= 0x2fa1f);    // 兼容补充
        }

        /// <summary>判断字符是否为十六进制数字 (0-9, a-f, A-F)</summary>
        public static bool IsHexDigit(char c) =>
            char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        /// <summary>判断字符是否为八进制数字 (0-7)</summary>
        public static bool IsOctalDigit(char c) => c >= '0' && c <= '7';

        /// <summary>判断字符是否为二进制数字 (0-1)</summary>
        public static bool IsBinaryDigit(char c) => c == '0' || c == '1';
    }
}
