using VMLAssembler;

namespace CompilerBase
{
    /// <summary>
    /// 类型敏感代码生成器中间基类。
    /// 提取 Java/Python/Lua 等编译器中重复的 TypeInfo → SelectOp 模式 (~200 行)。
    /// 子类只需实现 GetTypeInfo() 即可获得完整的 Load/Store/Move/Push/Pop/Arithmetic/Compare 指令选择。
    /// 同时提供 GetExpType() 从 4-tuple 自动推导 ExpType，消除各编译器手写 Type→ExpType mapping。
    /// </summary>
    /// <typeparam name="TTypeEnum">语言特定的类型枚举</typeparam>
    public abstract class TypedCodeGen<TTypeEnum> : CodeGeneratorBase where TTypeEnum : struct, System.Enum
    {
        protected TypedCodeGen() : base() { }

        /// <summary>
        /// 子类实现：将语言类型映射为 (字节大小, 是否浮点, 是否双精度, 是否64位整数)
        /// </summary>
        protected abstract (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(TTypeEnum type);

        /// <summary>
        /// 从 GetTypeInfo 4-tuple 自动推导 ExpType — 消除各编译器手写 ToExpType/InferSwiftType 等 switch 语句
        /// </summary>
        protected ExpType GetExpType(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            if (l) return ExpType.I64;
            if (d) return ExpType.F64;
            if (f) return ExpType.F32;
            return s switch { 1 => ExpType.I8, 2 => ExpType.I16, _ => ExpType.I32 };
        }

        /// <summary>根据类型选择 LOAD 指令</summary>
        protected OpCode GetLoadInstruction(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectLoadOp(s, f, d, l);
        }

        /// <summary>根据类型选择 STORE 指令 (统一 MOVE 系列) — 调用方需使用 dest-first 操作数顺序: [mem], reg</summary>
        protected OpCode GetStoreInstruction(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectStoreUnifiedOp(s, f, d, l);
        }

        /// <summary>统一 store 到内存 — 自动处理 MOVE 系列 dest-first 操作数顺序 (dest=mem, src=reg)</summary>
        protected void EmitStore(TTypeEnum type, Operand memDest, Operand regSrc)
        {
            var op = GetStoreInstruction(type);
            AddInstruction(op, memDest, regSrc);
        }

        /// <summary>统一 store R0 到栈偏移 — 自动处理操作数顺序</summary>
        protected void EmitStoreToStack(TTypeEnum type, int offset, int frameReg = 12)
        {
            EmitStore(type, Mem($"{offset}(R{frameReg})"), Reg(0));
        }

        /// <summary>根据类型选择 MOVE 指令</summary>
        protected OpCode GetMoveInstruction(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectMoveOp(s, f, d, l);
        }

        /// <summary>根据类型选择 PUSH 指令</summary>
        protected OpCode GetPushInstruction(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectPushOp(s, f, d, l);
        }

        /// <summary>根据类型选择 POP 指令</summary>
        protected OpCode GetPopInstruction(TTypeEnum type)
        {
            var (s, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectPopOp(s, f, d, l);
        }

        /// <summary>根据类型选择算术指令（可覆盖以支持语言特有运算符如 Lua 的 ^ 幂）</summary>
        protected virtual OpCode GetArithmeticInstruction(string op, TTypeEnum type)
        {
            var (_, f, d, l) = GetTypeInfo(type);
            if (op is "+" or "-" or "*" or "/" or "%")
                return ExpressionManager.SelectArithmeticOp(op, f, d, l);
            return op switch
            {
                "&" => OpCode.AND,
                "|" => OpCode.OR,
                "^" => OpCode.XOR,
                "<<" => OpCode.SHL,
                ">>" => OpCode.SHR,
                _ => OpCode.ADD
            };
        }

        /// <summary>根据类型选择比较指令</summary>
        protected OpCode GetCompareInstruction(TTypeEnum type)
        {
            var (_, f, d, l) = GetTypeInfo(type);
            return ExpressionManager.SelectCompareOp(f, d, l);
        }

        /// <summary>根据源类型和目标类型选择转换指令 (I2F/F2I/D2I/I2D/...)</summary>
        protected OpCode? GetConversionInstruction(TTypeEnum fromType, TTypeEnum toType)
        {
            var (fs, ff, fd, fl) = GetTypeInfo(fromType);
            var (ts, tf, td, tl) = GetTypeInfo(toType);
            return ExpressionManager.SelectConversionOp(fs, ff, fd, ts, tf, td, fl, tl);
        }

        /// <summary>发出类型转换指令 (源→目标)，相同类型跳过</summary>
        protected void EmitTypeConversion(TTypeEnum fromType, TTypeEnum toType, int reg = 0)
        {
            if (EqualityComparer<TTypeEnum>.Default.Equals(fromType, toType)) return;
            var op = GetConversionInstruction(fromType, toType);
            if (op.HasValue)
                AddInstruction(op.Value, [Reg(reg), Reg(reg)]);
        }

        /// <summary>生成表达式并自动转换到目标类型</summary>
        protected void GenerateExprWithConversion(Action emitExpr, TTypeEnum sourceType, TTypeEnum targetType, int reg = 0)
        {
            emitExpr();
            EmitTypeConversion(sourceType, targetType, reg);
        }
    }
}
