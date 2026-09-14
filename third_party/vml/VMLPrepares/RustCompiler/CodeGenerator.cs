using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    /// <summary>
    /// Rust 数据类型枚举
    /// </summary>
    public enum RustType
    {
        Int,        // 整数类型 (i32)
        I64,        // 64位整数 (i64) — 用 double 路径操作
        Float,      // 浮点类型 (f32)
        F64,        // 64位浮点 (f64)
        Bool,       // 布尔类型
        Char,       // 字符类型
        String,     // 字符串类型
        Array,      // 数组类型
        Tuple,      // 元组类型
        Struct,     // 结构体类型
        Enum,       // 枚举类型
        Reference,  // 引用类型
        Object      // 通用对象类型
    }

    public partial class CodeGenerator : TypedCodeGen<RustType>, IVisitor
    {
        private ProgramNode? _program;

        public CodeGenerator(ProgramNode program)
        {
            _program = program;
            InitSimpleCompiler(threeOperandInt: false, newLabel: () => NewLabel("L"));
        }

        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(RustType type) => type switch
        {
            RustType.Int => (4, false, false, false),
            RustType.I64 => (8, false, false, true),   // 64位整数 — 用 long 路径 (MOVEL/ADDL)
            RustType.Float => (4, true, false, false),
            RustType.F64 => (8, false, true, false),    // 64位浮点 — double 路径
            RustType.Bool => (1, false, false, false),
            RustType.Char => (1, false, false, false),
            RustType.String => (4, false, false, false), // pointer
            RustType.Array => (4, false, false, false),  // pointer
            RustType.Tuple => (4, false, false, false),  // pointer
            RustType.Struct => (4, false, false, false), // pointer
            RustType.Enum => (4, false, false, false),   // pointer
            RustType.Reference => (4, false, false, false),
            RustType.Object => (4, false, false, false),
            _ => (4, false, false, false)
        };
    }
}
