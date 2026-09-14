using VMLAssembler;
using VMLPlugins;
using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// OOP 编译器共享代码生成器基类
    /// 处理类/方法/字段/调用等通用 OOP 模式
    /// </summary>
    public abstract class OopCodeGenerator : CodeGeneratorBase
    {

        protected OopCodeGenerator() : base()
        {
            Regs = new RegisterManager();
            InitSimpleCompiler(framePointerReg: 12);
        }

        // ====== 函数调用 ======

        /// <summary>生成函数调用（参数从右到左压栈）→ 委托给 EmitCdeclCall</summary>
        [System.Obsolete("Use EmitCdeclCall from CodeGeneratorBase instead")]
        protected void EmitFunctionCall(string label, int argCount, System.Action<int> emitArg)
        {
            EmitCdeclCall(label, argCount, emitArg);
        }

        /// <summary>生成方法调用，带 receiver 参数</summary>
        protected void EmitMethodCall(string label, int argCount, System.Action<int> emitArg)
        {
            // receiver 已作为 arg0 在参数列表中
            EmitCdeclCall(label, argCount, emitArg);
        }

        // ====== 类/对象支持 ======

        /// <summary>计算字段在对象中的偏移（每个字段 4 字节）</summary>
        protected int GetFieldOffset(int fieldIndex)
        {
            return fieldIndex * 4;
        }

        /// <summary>生成加载对象字段到 R0</summary>
        protected void EmitLoadField(int fieldOffset)
        {
            // R0 = 对象地址, 加载 R0[fieldOffset]
            if (fieldOffset > 0)
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, fieldOffset)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
        }

        /// <summary>生成存储 R0 到对象字段</summary>
        protected void EmitStoreField(int fieldOffset)
        {
            // R1 = 对象地址, R0 = 值
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{fieldOffset}"), new Operand(OperandType.REGISTER, 0)]));
        }
    }
}
