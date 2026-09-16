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
        ///
        /// ## 溢出必须报错，不能回绕（v0.96.187 / patches/0018）
        ///
        /// 算术全在 <c>unchecked</c> 下做的话，`int a[N * N]`（N=65536）会算成 **0** ⇒
        /// 数组在 `.data` 里**零个元素**、编译通过不报错，之后每个 `a[i]` 都静默越界；
        /// N=50000 时算成负数 ⇒ 代码生成里 `new int[负数]` 抛裸的 .NET `OverflowException`，
        /// 用户只看到一行「Arithmetic operation resulted in an overflow.」，没有文件行号。
        /// 所以这里一律 `checked`，溢出抛**带文件/行号**的编译错误（见 <see cref="ConstOverflow"/>）。
        /// </summary>
        /// <param name="constLookup">
        /// 可选的**标识符**求值器（枚举值用：把已定义的枚举成员当常量）。
        /// 传 null 表示标识符一律不算常量 —— 数组维度走的是这条路，
        /// 免得把 `int a[n]`（n 是变量，真 VLA）误折成常量。
        /// </param>
        private bool TryConstInt(ASTNode? node, out int value, System.Func<string, int?>? constLookup = null)
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

            if (constLookup != null && node is Identifier id)
            {
                // 已定义的枚举成员当常量用（`enum { A = 1, B = A + 1 }` 在 C 里合法）
                var c = constLookup(id.Name);
                if (c.HasValue) { value = c.Value; return true; }
                return false;
            }

            if (node is UnaryOp un)
            {
                if (!TryConstInt(un.Operand, out var v, constLookup)) return false;
                switch (un.Op)
                {
                    case "+": value = v; return true;
                    case "-":
                        // int.MinValue 取负会回绕成自己
                        try { value = checked(-v); }
                        catch (System.OverflowException) { throw ConstOverflow(un); }
                        return true;
                    case "!": value = v == 0 ? 1 : 0; return true;
                    case "~": value = ~v; return true;
                    default: return false;
                }
            }

            if (node is BinaryOp bin)
            {
                if (!TryConstInt(bin.Left, out var a, constLookup) || !TryConstInt(bin.Right, out var b, constLookup)) return false;
                try
                {
                    switch (bin.Op)
                    {
                        case "+": value = checked(a + b); return true;
                        case "-": value = checked(a - b); return true;
                        case "*": value = checked(a * b); return true;
                        // 除零不折叠（交给运行时的报错路径，别在编译期造一个假值出来）
                        // ⚠ 除法溢出（int.MinValue / -1）与 checked 无关，一定抛，所以整块都在 try 里
                        case "/": if (b == 0) return false; value = a / b; return true;
                        case "%": if (b == 0) return false; value = a % b; return true;
                        case "<<": if (!(b >= 0 && b < 32)) return false; value = a << b; return true;
                        case ">>": if (!(b >= 0 && b < 32)) return false; value = a >> b; return true;
                        case "&": value = a & b; return true;
                        case "|": value = a | b; return true;
                        case "^": value = a ^ b; return true;
                        default: return false;
                    }
                }
                catch (System.OverflowException) { throw ConstOverflow(bin); }
            }

            return false;
        }

        /// <summary>
        /// **数组维度**专用：折常量 + 拒绝负数维度（`int a[-1]` 此前折叠成功，
        /// 生成 `sub R13, R1` 而 R1 为负 ⇒ 栈指针**反向移动**；全局数组则抛
        /// `new int[负数]` 的裸 .NET 异常）。负数一律报错。
        /// </summary>
        private bool TryConstDim(ASTNode? node, out int dim)
        {
            if (!TryConstInt(node, out dim)) return false;
            if (dim < 0)
                throw Error(ErrorCode.Parser_UnexpectedToken,
                    $"数组维度不能为负数：{DescribeConstExpr(node)} = {dim}");
            return true;
        }

        /// <summary>
        /// 多维数组**总元素数**的乘法（`int a[A][B]` 的 A*B）。
        /// 回绕就是静默拿到一个错的尺寸 —— 与单个维度溢出同一类。
        /// </summary>
        private int MulArraySize(int a, int b)
        {
            try { return checked(a * b); }
            catch (System.OverflowException)
            {
                throw Error(ErrorCode.Parser_UnexpectedToken,
                    $"数组总元素个数在编译期溢出 int 范围：{a} * {b}");
            }
        }

        /// <summary>
        /// 常量表达式溢出 —— 必须是**编译错误**（带文件/行号），不能回绕成别的值、也不能
        /// 让它升到代码生成阶段变成裸的 .NET 异常。
        ///
        /// 用 <c>Parser_UnexpectedToken</c> 是因为**只有这个错误码是不可恢复的**
        /// （见 `Parser.Declarations.cs` 顶层容错恢复那段的约定）：其它码会被 `Parse()`
        /// 的 `catch (System.Exception)` 吞成一行 stderr 日志后继续编 —— 那就又回到
        /// 「编译通过、跑起来才错」。它在本处已确认「真该终止编译」。
        /// </summary>
        private CompilerBase.ParseException ConstOverflow(ASTNode expr)
            => Error(ErrorCode.Parser_UnexpectedToken,
                $"常量表达式在编译期溢出 int 范围：{DescribeConstExpr(expr)}"
                + "（回绕会静默算出一个错的值 —— 例如 int a[N*N] 会变成 0 个元素）");

        /// <summary>
        /// 求**枚举成员**的值（4 处 enum 解析共用一个实现 —— 匿名 / 命名 / typedef 两种形态）。
        ///
        /// 此前这 4 处**只认裸数字字面量**：`A = BASE + 1`（宏展开成 `10 + 1`，是个 BinaryOp）
        /// 既不更新 nextValue 也**不报错** ⇒ 静默沿用上一个成员的值，
        /// `A/B/C` 编成 `0/1/2` 而不是 `11/12/13`，整段后续成员一起错位。
        ///
        /// 现在走与数组维度**同一套常量折叠**（<see cref="TryConstInt"/>），并额外允许引用
        /// **已定义的枚举成员**（`enum { A = 1, B = A + 1 }`）。真算不出来就报错，不再静默。
        /// </summary>
        private int EvalEnumMemberValue(ASTNode? expr, Dictionary<string, int> current)
        {
            if (TryConstInt(expr, out var v, name =>
                    current.TryGetValue(name, out var c) ? c : LookupEnumConstant(name)))
                return v;

            throw Error(ErrorCode.Parser_UnexpectedToken,
                $"枚举值不是编译期整型常量表达式：{DescribeConstExpr(expr)}"
                + "（只支持字面量与常量算术；引用变量、函数调用等无法在编译期求值）");
        }

        /// <summary>在**已解析完**的枚举里找成员（与 CodeGenerator.Core 同一口径：后定义的覆盖先定义的）。</summary>
        private int? LookupEnumConstant(string name)
        {
            if (program?.EnumConstants == null) return null;
            int? found = null;
            foreach (var e in program.EnumConstants)
                if (e.Value != null && e.Value.TryGetValue(name, out var v)) found = v;
            return found;
        }

        /// <summary>把常量表达式还原成人看的文本（报错信息里要说清是**哪个**表达式溢出了）。</summary>
        private static string DescribeConstExpr(ASTNode? n)
        {
            switch (n)
            {
                case null: return "?";
                case NumberLiteral l: return l.Value?.ToString() ?? "?";
                case Identifier id: return id.Name;
                case UnaryOp u: return u.Op + DescribeConstExpr(u.Operand);
                case BinaryOp b: return $"({DescribeConstExpr(b.Left)} {b.Op} {DescribeConstExpr(b.Right)})";
                default: return "常量表达式";
            }
        }
    }
}
