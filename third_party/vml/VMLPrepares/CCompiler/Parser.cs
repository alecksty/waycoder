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

        /// <summary>
        /// 把**常量表达式**折成整数（数组维度、枚举值这类"本该是编译期常量"的位置用它）。
        ///
        /// ## 为什么要有它
        ///
        /// 维度表达式是用 <c>ParseExpression</c> 解析的（所以 `MAX + 8` 这种写法能解析出来），
        /// 但各调用点此前**只认裸的 <c>NumberLiteral</c>** —— 于是
        /// <code>#define BW 10
        /// #define BH 20
        /// int b[BW * BH];   /* 宏展开后是 10 * 20，不是字面量 */</code>
        /// 被判成"运行时维度"进 <c>VlaDimensions</c>，编译期尺寸留空 ⇒ **静默**按 1 个元素分配。
        /// 之后所有 <c>b[i]</c> 都写到别的变量上：**编译不报错，跑起来数据全乱**，
        /// 是最难查的一类。`Parser.Expressions.cs` 里那句注释"支持常量表达式如 MAXPATHSIZE + 8"
        /// 说的就是这个意图，但那条路一直没实现。
        ///
        /// 只做**能在编译期算出唯一结果**的折叠：字面量、一元 +/−/!、以及两侧都是常量的
        /// 算术/位运算。任何一样不确定就返回 false，调用方照旧走 VLA 路径（真 VLA 仍然支持）。
        /// </summary>
        private static bool TryConstInt(ASTNode? node, out int value)
        {
            value = 0;
            if (node == null) return false;

            if (node is NumberLiteral lit)
            {
                // 带 F/L 之类后缀的浮点字面量不算整数常量
                if (!string.IsNullOrEmpty(lit.Suffix) && lit.Suffix.ToUpperInvariant().Contains('F')) return false;
                try { value = System.Convert.ToInt32(lit.Value); }
                catch { return false; }
                return true;
            }

            if (node is UnaryOp un)
            {
                if (!TryConstInt(un.Operand, out var v)) return false;
                switch (un.Op)
                {
                    case "+": value = v; return true;
                    case "-": value = -v; return true;
                    case "!": value = v == 0 ? 1 : 0; return true;
                    case "~": value = ~v; return true;
                    default: return false;
                }
            }

            if (node is BinaryOp bin)
            {
                if (!TryConstInt(bin.Left, out var a) || !TryConstInt(bin.Right, out var b)) return false;
                switch (bin.Op)
                {
                    case "+": value = a + b; return true;
                    case "-": value = a - b; return true;
                    case "*": value = a * b; return true;
                    // 除零不折叠（交给运行时的报错路径，别在编译期造一个假值出来）
                    case "/": if (b == 0) return false; value = a / b; return true;
                    case "%": if (b == 0) return false; value = a % b; return true;
                    case "<<": value = b >= 0 && b < 32 ? a << b : 0; return b >= 0 && b < 32;
                    case ">>": value = b >= 0 && b < 32 ? a >> b : 0; return b >= 0 && b < 32;
                    case "&": value = a & b; return true;
                    case "|": value = a | b; return true;
                    case "^": value = a ^ b; return true;
                    default: return false;
                }
            }

            return false;
        }
    }
}
