using System;

namespace CCompiler
{
    /// <summary>
    /// 数组初始化器里的**元素常量折叠**：`-1`、`1+2`、`~0`、`1 &lt;&lt; 3` …
    ///
    /// ## 为什么需要它（v0.96.212）
    ///
    /// 两条把初始化器摊平成 `dataSection` 值的路径 —— 全局数组
    /// （<c>CodeGenerator.Functions.FlattenArrayInitializer</c>）与 `static` 局部数组
    /// （<c>CodeGenerator.Statements.FlattenArrayInitValues</c>）—— 都只认
    /// `NumberLiteral` / `CharLiteral` / `StringLiteral` / `Identifier`，
    /// **其余一律 `result.Add(0)`**。
    ///
    /// 而 `-1` 根本不是 `NumberLiteral`，它是 `UnaryOp("-", NumberLiteral(1))`
    /// （见 `Parser.Expressions.cs` 的一元分支）⇒ **初始化器里每个负数都被静默编成 0**。
    ///
    /// ```c
    /// int A[4] = {  0,  1,  0, -1 };   /* 实际读出来是  0  1  0  0  */
    /// int B[3] = { -5,  7, -9 };       /* 实际读出来是  0  7  0     */
    /// ```
    ///
    /// 正数全对，**只有负数错** —— 所以只看源码矩阵永远看不出来。这不是学术问题：
    /// 方向表（`int DX[4] = {0,1,0,-1}`）正是最典型的一处，`Examples/c/pacman.c`
    /// 就因此**只能往右和往下走**（表面症状是"按键有反应、人不动"）。
    ///
    /// ## 语义
    ///
    /// 整数运算走 **`unchecked`**（= C 的 int 回绕语义）：`int x = 0xFFFFFFFF;` 就是 -1，
    /// 收敛到 int 是**对的**。

    /// 这一点与 `Parser.TryConstInt` 刻意不同 —— 那个用于**数组维度**，必须 `checked`
    /// 并在溢出时报错（`int a[N*N]` 回绕成 0 会静默编出零长度数组，见 patches/0018）。
    /// 两处的需求相反，所以是两份实现，不是重复。
    ///
    /// 折叠不出来的一律返回 `false`，由调用方保持原有行为（兜底 0）。
    /// </summary>
    internal static class ConstFold
    {
        /// <summary>能否折叠成一个编译期数字（<c>int</c> 或 <c>double</c>）。</summary>
        public static bool TryNumber(ASTNode? node, out object? value)
        {
            value = null;
            if (node == null) return false;

            if (node is NumberLiteral lit) return TryLiteral(lit, out value);
            if (node is CharLiteral ch) { value = (int)ch.Value; return true; }

            if (node is UnaryOp un)
            {
                if (un.IsPostfix) return false;          // x++ / x-- 不是常量
                if (!TryNumber(un.Operand, out var inner)) return false;

                if (inner is int iv)
                {
                    switch (un.Op)
                    {
                        case "+": value = iv; return true;
                        case "-": value = unchecked(-iv); return true;
                        case "!": value = iv == 0 ? 1 : 0; return true;
                        case "~": value = ~iv; return true;
                        default: return false;
                    }
                }
                if (inner is double dv)
                {
                    switch (un.Op)
                    {
                        case "+": value = dv; return true;
                        case "-": value = -dv; return true;
                        default: return false;
                    }
                }
                return false;
            }

            if (node is BinaryOp bin)
            {
                if (!TryNumber(bin.Left, out var l) || !TryNumber(bin.Right, out var r)) return false;

                if (l is int li && r is int ri) return TryIntBinary(bin.Op, li, ri, out value);

                if (TryToDouble(l, out var ld) && TryToDouble(r, out var rd))
                {
                    switch (bin.Op)
                    {
                        case "+": value = ld + rd; return true;
                        case "-": value = ld - rd; return true;
                        case "*": value = ld * rd; return true;
                        case "/": if (rd == 0) return false; value = ld / rd; return true;
                        default: return false;              // 位运算/取模对浮点无定义
                    }
                }
                return false;
            }

            return false;
        }

        private static bool TryIntBinary(string op, int a, int b, out object? value)
        {
            value = null;
            unchecked
            {
                switch (op)
                {
                    case "+": value = a + b; return true;
                    case "-": value = a - b; return true;
                    case "*": value = a * b; return true;
                    case "/": if (b == 0) return false; value = a / b; return true;
                    case "%": if (b == 0) return false; value = a % b; return true;
                    case "&": value = a & b; return true;
                    case "|": value = a | b; return true;
                    case "^": value = a ^ b; return true;
                    // 移位：C 里移位量 ≥ 位宽或为负是未定义行为，这里直接放弃折叠，
                    // 不猜一个结果出来（猜错就是又一个"静默的错"）。
                    case "<<": if (b < 0 || b > 31) return false; value = a << b; return true;
                    case ">>": if (b < 0 || b > 31) return false; value = a >> b; return true;
                    case "<": value = a < b ? 1 : 0; return true;
                    case ">": value = a > b ? 1 : 0; return true;
                    case "<=": value = a <= b ? 1 : 0; return true;
                    case ">=": value = a >= b ? 1 : 0; return true;
                    case "==": value = a == b ? 1 : 0; return true;
                    case "!=": value = a != b ? 1 : 0; return true;
                    case "&&": value = (a != 0 && b != 0) ? 1 : 0; return true;
                    case "||": value = (a != 0 || b != 0) ? 1 : 0; return true;
                    default: return false;
                }
            }
        }

        private static bool TryLiteral(NumberLiteral lit, out object? value)
        {
            value = null;
            var suffix = (lit.Suffix ?? string.Empty).ToUpperInvariant();

            if (suffix.Contains('F'))                    // 1.5f —— 浮点字面量
            {
                if (!TryToDouble(lit.Value, out var f)) return false;
                value = f; return true;
            }

            if (lit.Value is int i) { value = i; return true; }
            if (lit.Value is long l) { value = unchecked((int)l); return true; }

            if (TryToDouble(lit.Value, out var d))
            {
                // 整值收敛成 int（VML 的字长就是 32 位），带小数的保持 double
                if (d == Math.Floor(d) && d >= int.MinValue && d <= int.MaxValue)
                {
                    value = (int)d; return true;
                }
                value = d; return true;
            }
            return false;
        }

        private static bool TryToDouble(object? v, out double d)
        {
            switch (v)
            {
                case int i: d = i; return true;
                case long l: d = l; return true;
                case double dd: d = dd; return true;
                case float f: d = f; return true;
                case char c: d = c; return true;
                case string s when double.TryParse(s, out var p): d = p; return true;
                default: d = 0; return false;
            }
        }
    }
}
