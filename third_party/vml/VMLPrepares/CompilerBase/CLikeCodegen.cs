using VMLAssembler;
using VMLPlugins;

namespace CompilerBase
{
    /// <summary>
    /// C/Go 风格语言共享代码生成器
    /// 处理类型敏感指令选择、控制流、函数调用的通用模式
    /// </summary>
    public abstract class CLikeCodegen<TSelf> : CodeGeneratorBase where TSelf : class
    {
        protected int _stackFrameSize;

        protected CLikeCodegen() : base()
        {
            _stackFrameSize = 0;
            Regs = new RegisterManager();
            InitSimpleCompiler(framePointerReg: 12);
        }

        // ====== 类型系统抽象（子类实现） ======

        /// <summary>根据类型名获取类型大小（字节）</summary>
        protected abstract int GetTypeSizeByEnum(int typeEnum);
        protected abstract bool IsFloatType(int typeEnum);
        protected abstract int GetDefaultType();

        /// <summary>是否为双精度浮点类型（子类可覆盖，默认 false）</summary>
        protected virtual bool IsDoubleType(int typeEnum) => false;
        /// <summary>是否为64位长整数类型（子类可覆盖，默认 false）</summary>
        protected virtual bool IsLongType(int typeEnum) => false;

        /// <summary>类型敏感指令选择（子类可覆盖以自定义）</summary>
        protected virtual OpCode GetLoadInstruction(int typeEnum) => OpCode.MOVE;
        protected virtual OpCode GetStoreInstruction(int typeEnum) => OpCode.MOVE;

        /// <summary>类型感知移动指令 (v1.66.31+: 使用 IsFloatType/IsDoubleType/IsLongType)</summary>
        protected virtual OpCode GetMoveInstruction(int typeEnum)
        {
            if (IsFloatType(typeEnum)) return OpCode.MOVEF;
            if (IsDoubleType(typeEnum)) return OpCode.MOVED;
            if (IsLongType(typeEnum)) return OpCode.MOVEL;
            int size = GetTypeSizeByEnum(typeEnum);
            if (size == 1) return OpCode.MOVEB;
            if (size == 2) return OpCode.MOVEH;
            return OpCode.MOVE;
        }

        /// <summary>类型感知压栈指令 (v1.66.31+)</summary>
        protected virtual OpCode GetPushInstruction(int typeEnum)
        {
            if (IsFloatType(typeEnum)) return OpCode.FPUSH;
            if (IsDoubleType(typeEnum)) return OpCode.DPUSH;
            if (IsLongType(typeEnum)) return OpCode.PUSHL;
            int size = GetTypeSizeByEnum(typeEnum);
            if (size == 1) return OpCode.PUSHB;
            if (size == 2) return OpCode.PUSHH;
            return OpCode.PUSH;
        }

        /// <summary>类型感知弹栈指令 (v1.66.31+)</summary>
        protected virtual OpCode GetPopInstruction(int typeEnum)
        {
            if (IsFloatType(typeEnum)) return OpCode.FPOP;
            if (IsDoubleType(typeEnum)) return OpCode.DPOP;
            if (IsLongType(typeEnum)) return OpCode.POPL;
            int size = GetTypeSizeByEnum(typeEnum);
            if (size == 1) return OpCode.POPB;
            if (size == 2) return OpCode.POPH;
            return OpCode.POP;
        }
        protected virtual OpCode GetArithmeticInstruction(string op, bool isFloat) => OpCode.ADD;
        protected virtual OpCode GetCompareInstruction(bool isFloat) => OpCode.CMP;

        // ====== 通用 VML 指令发射 ======

        protected void Emit(OpCode opcode, params Operand[] operands)
        {
            var ops = new List<Operand>(operands);
            AddInstruction(opcode, ops);
        }

        protected void Emit(OpCode opcode, List<Operand> operands)
        {
            instructions.Add(new Instruction(opcode, operands, instructions.Count));
        }

        protected new Operand Reg(int reg) => new(OperandType.REGISTER, reg);
        protected Operand Imm(int value) => new(OperandType.IMMEDIATE, value);
        protected new Operand LabelOp(string label) => new(OperandType.LABEL, label);
        protected new Operand Mem(string addr) => new(OperandType.MEMORY, addr);

        // ====== 函数调用 ======

        protected void EmitCall(string label, int argCount)
        {
            Emit(OpCode.CALL, LabelOp(label));
            if (argCount > 0)
                Emit(OpCode.ADD, Reg(13), Imm(argCount * 4));
        }

        protected void EmitCallArgs(int argCount, System.Action<int> genArg)
        {
            for (int i = argCount - 1; i >= 0; i--)
            {
                genArg(i);
                Emit(OpCode.PUSH, Reg(0));
            }
        }

        /// <summary>
        /// 类型感知的参数压栈: 根据 byteSize/isFloat/isDouble/isLong 选择 PUSH/PUSHB/PUSHH/FPUSH/DPUSH/PUSHL
        /// 返回压栈的字节数 (用于调用方计算 argSize/stackWordCount)
        /// 替代各编译器手写的 isDoubleArg ? DPUSH : PUSH
        /// v1.66.63: float/double/long 使用 sub R13 + movef/moved/movel @13 替代 FPUSH/DPUSH/PUSHL，
        ///   因为 typed stack 指令推到独立栈 (floatStack/doubleStack/longStack)，
        ///   而 C 函数 callee 始终从主栈 [R12+offset] 读取参数。
        /// </summary>
        protected int EmitPushArg(int byteSize, bool isFloat, bool isDouble, bool isLong)
        {
            // float/double/long: 用 sub R13 + move/movef/moved/movel @13 写入主栈
            // 因为 FPUSH/DPUSH/PUSHL 推到 typed stack，而 callee 从主栈读取
            if (isFloat)
            {
                Emit(OpCode.SUB, Reg(13), Imm(4));
                Emit(OpCode.MOVEF, new Operand(OperandType.INDIRECT, 13), Reg(0));
                return 4;
            }
            if (isDouble)
            {
                Emit(OpCode.SUB, Reg(13), Imm(8));
                Emit(OpCode.MOVED, new Operand(OperandType.INDIRECT, 13), Reg(0));
                return 8;
            }
            if (isLong)
            {
                Emit(OpCode.SUB, Reg(13), Imm(8));
                Emit(OpCode.MOVEL, new Operand(OperandType.INDIRECT, 13), Reg(0));
                return 8;
            }
            var pushOp = ExpressionManager.SelectPushOp(byteSize, isFloat, isDouble, isLong);
            Emit(pushOp, Reg(0));
            return pushOp switch
            {
                OpCode.PUSHB => 1, OpCode.PUSHH => 2,
                OpCode.FPUSH => 4, OpCode.PUSH => 4,
                OpCode.DPUSH => 8, OpCode.PUSHL => 8,
                _ => 4
            };
        }

        // ====== 立即数 ======

        protected void EmitLoadInt(int value)
        {
            Emit(OpCode.MOVE, Reg(0), Imm(value));
        }

        // ====== 类型推导入门（子类应扩展） ======

        protected int ResolveSize(int typeEnum)
        {
            if (IsFloatType(typeEnum)) return 4;
            return GetTypeSizeByEnum(typeEnum);
        }

        // ====== 数组索引偏移计算 ======

        /// <summary>
        /// 计算数组元素偏移: R0 = R0 + index * elementSize
        /// 调用前 R0 应为数组基地址，调用后 R0 = &arr[index]
        /// </summary>
        protected void EmitArrayIndexOffset(int elementSize)
        {
            // R0 = index value (from caller), compute byte offset
            if (elementSize == 4)
                Emit(OpCode.SHL, Reg(0), Imm(2));          // index * 4
            else if (elementSize == 8)
                Emit(OpCode.SHL, Reg(0), Imm(3));          // index * 8
            else if (elementSize == 2)
                Emit(OpCode.SHL, Reg(0), Imm(1));          // index * 2
            else if (elementSize == 1)
                { /* index * 1 — byte offset = index */ }
            else
            {
                // 通用乘法: R1 = elementSize, R0 = R0 * R1
                Emit(OpCode.MOVE, Reg(1), Imm(elementSize));
                Emit(OpCode.MUL, Reg(0), Reg(0), Reg(1));
            }
        }

        /// <summary>
        /// 计算多维数组偏移: R0 = (((i0*d1 + i1)*d2 + i2)*d3 + ...)
        /// indices 为索引值列表，dimensions 为各维度大小（不含最内层）
        /// 调用前 R0 = 第一个索引值，调用后 R0 = 线性偏移
        /// </summary>
        protected void EmitMultiDimOffset(int[] indices, int?[] dimensions, int elementSize)
        {
            if (indices.Length == 0) { Emit(OpCode.MOVE, Reg(0), Imm(0)); return; }
            // 第一个索引已在 R0 中
            for (int d = 1; d < indices.Length; d++)
            {
                if (dimensions[d].HasValue && dimensions[d].Value > 1)
                {
                    Emit(OpCode.MOVE, Reg(1), Imm(dimensions[d].Value));
                    Emit(OpCode.MUL, Reg(0), Reg(0), Reg(1));      // acc = acc * dim[d]
                }
                // PUSH acc, 计算第 d 个索引, POP 到 R1, ADD
                Emit(OpCode.PUSH, Reg(0));
                // 子类负责计算第 d 个索引到 R0 (此处不处理)
                Emit(OpCode.POP, Reg(1));
                Emit(OpCode.ADD, Reg(0), Reg(1), Reg(0));          // acc += index[d]
            }
            // 乘以元素大小
            EmitArrayIndexOffset(elementSize);
        }

        // ====== 间接函数调用 ======

        /// <summary>
        /// 间接函数调用: 从变量/寄存器加载函数地址并 CALL
        /// 适合函数指针变量调用 op(args)
        /// 子类负责将函数地址加载到指定寄存器后调用此方法
        /// </summary>
        protected void EmitIndirectCall(int addrReg, int argBytes)
        {
            Emit(OpCode.CALL, Reg(addrReg));
            if (argBytes > 0)
                Emit(OpCode.ADD, Reg(13), Imm(argBytes));
        }

        // ====== 结构体字段偏移 ======

        /// <summary>
        /// 根据结构体字段索引计算字段偏移量
        /// 默认假设字段从 offset 0 开始，sizeof(int)=4
        /// 子类如有 vtable/header 应覆盖
        /// </summary>
        protected virtual int GetFieldOffset(int fieldIndex, int fieldSize = 4)
        {
            return fieldIndex * fieldSize;
        }

        // ====== 取地址辅助 ======

        /// <summary>
        /// 栈变量的地址计算: R0 = R12 ± offset (BP相对寻址)
        /// 正值: R12 + offset (局部变量区在 BP 上方)
        /// 负值: R12 - offset
        /// </summary>
        protected void EmitStackVarAddress(int offset)
        {
            Emit(OpCode.MOVE, Reg(0), Reg(12));  // R0 = BP
            if (offset > 0)
                Emit(OpCode.ADD, Reg(0), Imm(offset));
            else if (offset < 0)
                Emit(OpCode.SUB, Reg(0), Imm(-offset));
        }

        /// <summary>
        /// 全局/静态变量的地址计算: R0 = LOAD label
        /// </summary>
        protected void EmitGlobalVarAddress(string label)
        {
            Emit(OpCode.MOVE, Reg(0), LabelOp(label));
        }

        // ====== 指针解引用与存储 ======

        /// <summary>
        /// 存储值到指针目标: *ptr = value
        /// 调用前: 子类需将 value 放入 R0, ptr 地址用 GenerateAddressOf 计算
        /// 调用: R1 = R0 (value), R0 = address, STORE R1, (R0)
        /// </summary>
        protected void EmitDerefStore()
        {
            // R0 = value (from caller), save and compute address
            Emit(OpCode.MOVE, Reg(1), Reg(0));       // R1 = value
            // R0 should already contain the target address (computed by caller)
            Emit(OpCode.MOVE, Mem("R0"), Reg(1));    // *R0 = R1
            Emit(OpCode.MOVE, Reg(0), Reg(1));         // R0 = value (return assigned value)
        }

        /// <summary>
        /// 通用赋值: *targetAddr = value
        /// 适用于 *ptr = val, arr[i] = val, struct.field = val 等
        /// targetAddr 由调用者通过 EmitStackVarAddress/EmitGlobalVarAddress 等计算
        /// </summary>
        protected void EmitStoreToAddress()
        {
            // R0 = value, save it and compute address
            Emit(OpCode.MOVE, Reg(1), Reg(0));        // R1 = value
            // R0 should already contain the target address (from EmitStackVarAddress etc.)
            Emit(OpCode.MOVE, Mem("R0"), Reg(1));     // *R0 = R1
            Emit(OpCode.MOVE, Reg(0), Reg(1));         // R0 = value
        }

        /// <summary>
        /// 保存 R0 值到 R2，安全地计算目标地址（R2 不被地址计算破坏）
        /// 适用于复杂左值: R2=value, R0=addr, STORE R2 (R0), R0=R2
        /// </summary>
        protected void EmitStoreViaR2()
        {
            Emit(OpCode.MOVE, Reg(2), Reg(0));        // R2 = value
            // R0 = address (computed by caller after this call)
            Emit(OpCode.MOVE, Mem("R0"), Reg(2));     // *R0 = R2
            Emit(OpCode.MOVE, Reg(0), Reg(2));         // R0 = value
        }

        // ====== 指针加载 ======

        /// <summary>
        /// 从指针加载值: R0 = *R0 (R0 应为地址)
        /// </summary>
        protected void EmitPointerLoad()
        {
            Emit(OpCode.MOVE, Reg(0), Mem("R0"));
        }

        // ====== VML 数组分配辅助 ======

        /// <summary>
        /// 在 dataSection 中分配 VML 数组: [count, e0, e1, ...]
        /// 返回标签名供 LEA 取地址使用
        /// </summary>
        protected string AllocateVmlArray(string varName, int elementCount)
        {
            string label = $"var_{varName}";
            object[] arrayData = new object[1 + elementCount];
            arrayData[0] = elementCount; // VML header = element count
            for (int i = 1; i <= elementCount; i++)
                arrayData[i] = 0;
            dataSection[label] = arrayData;
            return label;
        }

        /// <summary>
        /// 判断变量是否为 VML 数组 (dataSection 中 object[] 类型)
        /// </summary>
        protected bool IsVmlArray(string varName)
        {
            string label = $"var_{varName}";
            return dataSection.TryGetValue(label, out var val) && val is object[];
        }

        /// <summary>
        /// 加载数组基地址到 R0: LEA label (而非 LOAD [label])
        /// </summary>
        protected void EmitLoadArrayAddress(string label)
        {
            Emit(OpCode.MOVE, Reg(0), LabelOp(label));
        }

        /// <summary>
        /// 计算数组元素地址: R1 = base + 4 + index*elementSize
        /// 调用前: R0 = index, 调用后: R1 = element address
        /// 兼容 caller-save: 会 PUSH/POP R0 保存 index
        /// </summary>
        protected void EmitArrayElementAddress(int elementSize = 4)
        {
            EmitArrayIndexOffset(elementSize);           // R0 = index * elementSize
            Emit(OpCode.ADD, Reg(0), Reg(0), Imm(4));    // +4 (skip VML header)
            Emit(OpCode.POP, Reg(1));                     // R1 = base
            Emit(OpCode.ADD, Reg(1), Reg(1), Reg(0));     // R1 = base + offset
            Emit(OpCode.POP, Reg(0));                     // restore value to R0 (for store)
        }

        // ====== 类型转换辅助 ======

        /// <summary>
        /// 根据类型大小选择正确的加载指令 (MOVEB/MOVEH/MOVE/MOVEF/MOVED)
        /// </summary>
        protected OpCode SelectLoadOp(int size, bool isFloat, bool isDouble)
        {
            if (size == 1) return OpCode.MOVEB;
            if (size == 2) return OpCode.MOVEH;
            if (isDouble) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return OpCode.MOVE;
        }

        /// <summary>
        /// 根据类型大小选择正确的存储指令 (MOVEB/MOVEH/MOVE/MOVEF/MOVED)
        /// </summary>
        protected OpCode SelectStoreOp(int size, bool isFloat, bool isDouble)
        {
            if (size == 1) return OpCode.MOVEB;
            if (size == 2) return OpCode.MOVEH;
            if (isDouble) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return OpCode.MOVE;
        }
    }
}
