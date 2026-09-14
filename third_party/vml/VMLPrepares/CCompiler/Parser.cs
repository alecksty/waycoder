using CompilerBase;
using System.Collections.Generic;
using System.Linq;

namespace CCompiler
{
    /// <summary>
    /// C 语言语法分析器 — 主部分类 + 类型检查辅助方法
    /// </summary>
    public partial class Parser : ParserBase<Token, TokenType>
    {
        /// <summary>所有可作为类型开头的 Token（用于 Expect/Match 列表）</summary>
        private static readonly TokenType[] AllTypeTokens = {
            TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE,
            TokenType.SHORT, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED,
            TokenType.CONST, TokenType.VOLATILE, TokenType.VOID,
            TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION,
            TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64,
            TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64,
            TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T,
            TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T
        };

        /// <summary>类型修饰符 Token（可跟在类型开头的 Token 之后）</summary>
        private static readonly TokenType[] TypeModifierTokens = {
            TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED,
            TokenType.CONST, TokenType.VOLATILE, TokenType.CHAR, TokenType.INT,
            TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID,
            TokenType.STRUCT, TokenType.UNION,
            TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64,
            TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64,
            TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T,
            TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T
        };

        /// <summary>检查当前 Token 是否匹配给定类型之一</summary>
        private bool IsType(TokenType t) => System.Array.IndexOf(AllTypeTokens, Current().Type) >= 0;

        /// <summary>检查当前 Token 是否为类型修饰符</summary>
        private bool IsTypeModifier() => System.Array.IndexOf(TypeModifierTokens, Current().Type) >= 0;

        /// <summary>消费当前类型修饰符 Token，返回其字符串值</summary>
        private string AdvanceTypeModifier() {
            string m = Current().Value.ToString();
            Advance();
            return m;
        }
    }
}
