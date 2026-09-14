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
            PascalType.Char => ExpType.I8,
            PascalType.Boolean => ExpType.I8,
            _ => ExpType.I32,
        };

        private ExpVar WrapExpr(ExpressionNode node, PascalType type) =>
            ExpVar.Eval(PascalTypeToExpType(type), () => GenerateExpression(node));

        private ExpVar WrapExpr(ExpressionNode node) =>
            WrapExpr(node, IsFloatExpression(node) ? PascalType.Real : PascalType.Integer);

        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(PascalType type) => type switch
        {
            PascalType.Real => (4, true, false, false),
            PascalType.Char or PascalType.Boolean => (1, false, false, false),
            PascalType.String or PascalType.Array or PascalType.Record or PascalType.Set or PascalType.File or PascalType.Pointer => (4, false, false, false), // pointer/reference
            _ => (4, false, false, false) // Integer and default
        };
    }
}
