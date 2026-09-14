using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace LadderCompiler
{
    /// <summary>
    /// 梯形图数据类型枚举
    /// </summary>
    public enum LadderTypeEnum
    {
        Bool,       // BOOL - 1位
        Byte,       // BYTE - 8位
        Word,       // WORD - 16位
        DWord,      // DWORD - 32位
        LWord,      // LWORD - 64位
        Int,        // INT - 16位有符号
        DInt,       // DINT - 32位有符号
        LInt,       // LINT - 64位有符号
        Real,       // REAL - 32位浮点
        LReal,      // LREAL - 64位浮点
        String,     // STRING
        Timer,      // TIMER
        Counter,    // COUNTER
        Array,      // 数组
        Struct,     // 结构体
        Object      // 对象类型
    }

    public partial class CodeGenerator : TypedCodeGen<LadderTypeEnum>, IASTVisitor
    {
        private ProgramNode? _program;

        public CodeGenerator(ProgramNode program)
        {
            _program = program;
            InitSimpleCompiler();
        }
    }
}
