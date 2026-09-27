using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal数据类型枚举
    /// </summary>
    public enum PascalType
    {
        Integer,
        Real,
        /// <summary>
        /// `Double` / `Extended` / `Comp` / `Currency` —— **8 字节双精度**。
        ///
        /// ⚠ 这张表原先**只有 `Real`**（32 位单精度），于是 `d: Double` 落到 `GetPascalType`
        /// 的兜底 `Integer` ⇒ `d := 3.14` 把 3.14 **截成 3**（实测 `Trunc(d*2.0*100)` 得 600 而不是 628）。
        /// Turbo Pascal 的 `Real` 确实是 6 字节软浮点、`Single` 是 32 位、`Double` 是 64 位 ——
        /// 三者**宽度不同**，一张表里必须分开。
        /// </summary>
        Double,
        /// <summary>`Int64` / `QWord` —— 8 字节整数（`LongInt` 是 32 位，**不在此列**）。</summary>
        Int64,
        Char,
        Boolean,
        String,
        Array,
        Record,
        Set,
        File,
        Pointer
    }

    /// <summary>
    /// Pascal代码生成器 - 支持完整Pascal语法
    /// </summary>
    public partial class CodeGenerator : TypedCodeGen<PascalType>
    {
        private static ExpType PascalTypeToExpType(PascalType t) => t switch
        {
            PascalType.Real => ExpType.F32,
            PascalType.Double => ExpType.F64,
            PascalType.Int64 => ExpType.I64,
            PascalType.Char => ExpType.I8,
            PascalType.Boolean => ExpType.I8,
            _ => ExpType.I32,
        };

        private ExpVar WrapExpr(ExpressionNode node, PascalType type) =>
            ExpVar.Eval(PascalTypeToExpType(type), () => GenerateExpression(node));

        /// <summary>
        /// **非浮点**表达式的 Pascal 类 —— 只有 `Integer`（32 位）与 `Int64`（64 位）两档。
        ///
        /// <para>
        /// ⚠ 原先 `WrapExpr` 把**所有**非浮点都归成 `Integer` ⇒ 带 `Int64` 变量的表达式
        /// 全部按 32 位算：实测 `(big + add) / 1e9` 编出来是 `push @R0`（而值在 `L0`）
        /// `div @R0 @R1 @R0` —— 32 位除 + 垃圾操作数，结果恒 0。
        /// 判据只用"**这个表达式里有没有 64 位的东西**"，与 `GetTypeInfo` 同一张表。
        /// </para>
        /// </summary>
        private PascalType GuessIntType(ExpressionNode node) => node switch
        {
            LiteralNode lit when lit.Value is long l && (l < int.MinValue || l > int.MaxValue) => PascalType.Int64,
            LiteralNode => PascalType.Integer,
            VariableNode v => GetVariablePascalType(v.Name) == PascalType.Int64 ? PascalType.Int64 : PascalType.Integer,
            BinaryOpNode b => (GuessIntType(b.Left) == PascalType.Int64 || GuessIntType(b.Right) == PascalType.Int64)
                              ? PascalType.Int64 : PascalType.Integer,
            UnaryOpNode u => GuessIntType(u.Operand),
            TypeCastNode c => GetPascalType(c.TypeName) == PascalType.Int64 ? PascalType.Int64 : PascalType.Integer,
            FunctionCallNode f => ExternalFuncTypes.TryGetValue(f.Name.ToLower(), out var et)
                                  && GetPascalType(et) == PascalType.Int64 ? PascalType.Int64 : PascalType.Integer,
            _ => PascalType.Integer,
        };

        /// <summary>
        /// **浮点**表达式的 Pascal 类：`Real`（32 位单精度）还是 `Double`（64 位）。
        /// 与 <see cref="GuessIntType"/> 成对 —— 原先一律返回 `Real`，
        /// 于是 `d1 * d2`（两个 `Double` 变量）按**单精度**乘（实测 628 变 600 一类）。
        /// </summary>
        private PascalType GuessFloatType(ExpressionNode node) => node switch
        {
            // ⚠ **64 位的东西一律按双精度算**（`Double` 变量，以及 `Int64` 变量/字面量）：
            //   Pascal 的 `/` 结果是实数，但**单精度存不下 5e9 这个量级**
            //   （float 只有 24 位尾数）⇒ `(5000000000 / 5) / 1e9` 用单精度算得 0.999…，
            //   `Trunc` 成 **0**（应 2）。Int64 参与实数运算时走双精度是**唯一**能保住
            //   这些整数精确性的选择（双精度尾数 53 位，2^53 以内精确）。
            VariableNode v => GetVariablePascalType(v.Name) is PascalType.Double or PascalType.Int64
                              ? PascalType.Double : PascalType.Real,
            LiteralNode lit when lit.Value is long l && (l < int.MinValue || l > int.MaxValue) => PascalType.Double,
            BinaryOpNode b => (GuessFloatType(b.Left) == PascalType.Double || GuessFloatType(b.Right) == PascalType.Double)
                              ? PascalType.Double : PascalType.Real,
            UnaryOpNode u => GuessFloatType(u.Operand),
            TypeCastNode c => GetPascalType(c.TypeName) is PascalType.Double or PascalType.Int64
                              ? PascalType.Double : PascalType.Real,
            // ⚠ 实数字面量判作 **Real**（单精度）—— 与**它的装载**保持一致：
            //   `CodeGenerator.Expressions` 里字面量一律按 `flt_` + `MOVEF` 发
            //   （"Pascal 的 REAL 是 32 位"）。判成 `Double` 会让"转换决策"说 D2F、
            //   而实际装载的是 F0 ⇒ `d2f @F0 @D0` 把刚载入的字面量覆盖掉（实测全 0）。
            //   **想要双精度就把值放进 `Double` 变量**（`d := 1e9; x / d`）——
            //   那才是这门语言里表达"我要双精度"的方式。
            _ => PascalType.Real,
        };

        private ExpVar WrapExpr(ExpressionNode node) =>
            WrapExpr(node, IsFloatExpression(node) ? GuessFloatType(node) : GuessIntType(node));

        /// <summary>
        /// 求值后**显式转成双精度**再包成 `F64` 的 ExpVar —— 供 Pascal 的 `/` 用
        /// （见 `TokenType.SLASH` 那处：64 位整数参与的除法必须走实数路径）。
        /// 转换走共享的 `EmitConversion`（按 (size,float,double,long) 选指令、按类取寄存器）。
        /// </summary>
        private ExpVar WrapExprAsDouble(ExpressionNode node)
        {
            var src = IsFloatExpression(node) ? GuessFloatType(node) : GuessIntType(node);
            var (s, f, d, l) = TypeInfo(src);
            return ExpVar.Eval(ExpType.F64,
                () => { GenerateExpression(node); _expr!.EmitConversion(s, f, d, 8, false, true, l, false); });
        }

        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(PascalType type) => type switch
        {
            PascalType.Real => (4, true, false, false),
            PascalType.Double => (8, false, true, false),
            PascalType.Int64 => (8, false, false, true),
            PascalType.Char or PascalType.Boolean => (1, false, false, false),
            PascalType.String or PascalType.Array or PascalType.Record or PascalType.Set or PascalType.File or PascalType.Pointer => (4, false, false, false), // pointer/reference
            _ => (4, false, false, false) // Integer and default
        };
    }
}
