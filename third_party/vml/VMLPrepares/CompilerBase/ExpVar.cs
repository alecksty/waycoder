using VMLAssembler;

namespace CompilerBase
{
    /// <summary>统一表达式类型 — 屏蔽各编译器特有的类型系统</summary>
    public enum ExpType : byte
    {
        I8, I16, I32, I64,     // 有符号整数
        U8, U16, U32, U64,     // 无符号整数
        F32, F64,              // 浮点
        Ptr32,                 // 32位指针 (4字节，默认)
        Ptr64,                 // 64位指针 (8字节，预留)
    }

    /// <summary>ExpType 扩展方法</summary>
    public static class ExpTypeExtensions
    {
        public static int ByteSize(this ExpType t) => t switch
        {
            ExpType.I8 or ExpType.U8 => 1,
            ExpType.I16 or ExpType.U16 => 2,
            ExpType.I64 or ExpType.U64 or ExpType.F64 or ExpType.Ptr64 => 8,
            _ => 4 // I32, U32, F32, Ptr32
        };

        public static bool IsFloat(this ExpType t) => t is ExpType.F32 or ExpType.F64;
        public static bool IsLong(this ExpType t) => t is ExpType.I64 or ExpType.U64;

        /// <summary>
        /// 是不是**无符号**类型 —— 决定 `/` `%` `>>` 比较发哪条指令。
        ///
        /// 与 <see cref="IsLong"/> 并列放这里（而不是各发射器各判一次）：
        /// 「无符号」此前在**所有**发射器里都是一个不存在的维度
        /// （`ExprType`/`ExpType` 早就带着 `U8..U64`，但没人读它），
        /// 于是 `4000000000u % 10` 走有符号 MOD 得 **-6**。
        /// 收成一份谓词，将来加类型只需改这里。
        /// </summary>
        public static bool IsUnsigned(this ExpType t) => t is ExpType.U8 or ExpType.U16 or ExpType.U32 or ExpType.U64;
        public static bool IsDouble(this ExpType t) => t == ExpType.F64;

        /// <summary>指针类型只能做加减运算</summary>
        public static bool IsPtr(this ExpType t) => t is ExpType.Ptr32 or ExpType.Ptr64;

        /// <summary>
        /// 该类型的**寄存器组基址** —— 用户 2026-09-27 定下的模型
        /// （`Rn`=32位 / `Ln`=64位 / `Fn`=32位 / `Dn`=64位）在编号空间里的落点：
        /// <list type="bullet">
        /// <item><c>long</c> → **24**（L0–L7，见 <c>RegisterSyntax.TryParseName</c>）；</item>
        /// <item><c>double</c> → **16**（D0–D7）；</item>
        /// <item>其余（int/float/指针）→ **0**（R0–R15，浮点与整数**同号不同组**）。</item>
        /// </list>
        /// <para>
        /// ⚠ 这是**唯一真源**：序列化拼写（`@L0`/`@D0`）、汇编期校验（<c>RegisterClassTable</c>）、
        /// 运行时那三个数组（<c>registers[16]</c>/<c>doubleRegisters[8]</c>/<c>longRegisters[8]</c>）
        /// 与前端发射都从这一份推。此前 `ExpVar.RegBankBase` 就写着同样的式子，但**零调用点**
        /// —— 前端全用 R0 当累加器，于是 64 位值一进 R8–R15 就被静默截断（实测数组元素读成 0）。
        /// </para>
        /// </summary>
        public static int BankBase(this ExpType t)
            => t == ExpType.F64 ? 16 : (t.IsLong() ? 24 : 0);
    }

    /// <summary>变量存储位置</summary>
    public enum ExpLoc { Reg, Stack, Data, Imm }

    /// <summary>
    /// 统一表达式变量 — 封装类型 + 存储位置，屏蔽编译器差异
    /// 纯描述对象；Eval() 工厂支持延迟求值，由 ExpressionManager 在需要时触发
    /// </summary>
    public class ExpVar
    {
        public ExpType Type { get; }
        public ExpLoc Loc { get; }
        public int RegNum { get; }       // Loc==Reg 时
        public int StackOffset { get; }  // Loc==Stack 时，相对于帧指针
        public int FpReg { get; }        // Loc==Stack 时的帧指针寄存器号
        public string? Label { get; }    // Loc==Data 时
        public object? ImmValue { get; } // Loc==Imm 时

        /// <summary>延迟求值回调：ExpressionManager.EmitLoad 时调用，生成代码将值放入 R0</summary>
        internal Action? EvalAction { get; init; }

        // ==== 类型信息 (供 ExpressionManager 使用) ====
        public int ByteSize => Type switch
        {
            ExpType.I8 or ExpType.U8 => 1,
            ExpType.I16 or ExpType.U16 => 2,
            ExpType.I64 or ExpType.U64 or ExpType.F64 or ExpType.Ptr64 => 8,
            _ => 4, // I32, U32, F32, Ptr32
        };

        public bool IsFloat => Type is ExpType.F32 or ExpType.F64;
        public bool IsDouble => Type == ExpType.F64;
        public bool IsLong => Type is ExpType.I64 or ExpType.U64;
        public bool IsPtr => Type is ExpType.Ptr32 or ExpType.Ptr64;

        /// <summary>寄存器组基址偏移: double→16 (R16-R23=D0-D7), long→24 (R24-R31=L0-L7), 其他→0</summary>
        public int RegBankBase => Type.BankBase();

        /// <summary>指针所指类型的字节大小。0=非指针, 1=char*, 2=short*, 4=int*, 8=long*</summary>
        public int PointedTypeSize { get; }

        /// <summary>是否为引用变量：槽中存储的是地址而非值，Load/Store 需间接访问</summary>
        public bool IsReference { get; }

        // ==== 构造 ====
        private ExpVar(ExpType type, ExpLoc loc, int regNum = 0, int stackOffset = 0, int fpReg = 12, string? label = null, object? immValue = null, Action? evalAction = null, int pointedTypeSize = 0, bool isReference = false)
        {
            Type = type; Loc = loc; RegNum = regNum; StackOffset = stackOffset; FpReg = fpReg; Label = label; ImmValue = immValue; EvalAction = evalAction; PointedTypeSize = pointedTypeSize; IsReference = isReference;
        }

        // ==== 静态工厂 ====

        /// <summary>寄存器中的值 (R0=累加器, R1-R15=通用)。不触发求值</summary>
        public static ExpVar Reg(int n, ExpType type = ExpType.I32) => new(type, ExpLoc.Reg, regNum: n);

        /// <summary>延迟求值表达式：首次 EmitLoad 时调用 emit() 生成代码填入 R0</summary>
        public static ExpVar Eval(ExpType type, Action emit) => new(type, ExpLoc.Reg, regNum: 0, evalAction: emit);

        /// <summary>栈变量，相对于帧指针偏移</summary>
        /// <param name="offset">正数=R{reg}+offset, 负数=R{reg}-abs(offset)</param>
        public static ExpVar Stack(int offset, int fpReg, ExpType type = ExpType.I32) => new(type, ExpLoc.Stack, stackOffset: offset, fpReg: fpReg);

        /// <summary>Data section 全局变量</summary>
        public static ExpVar Data(string label, ExpType type = ExpType.I32) => new(type, ExpLoc.Data, label: label);

        /// <summary>立即数</summary>
        public static ExpVar Imm(object value, ExpType type = ExpType.I32) => new(type, ExpLoc.Imm, immValue: value);

        // ==== 便捷方法 ====

        /// <summary>格式化栈偏移为 VML memory 语法: R14+8 / R12-4</summary>
        public string FormatOffset()
        {
            if (Loc != ExpLoc.Stack) return "";
            return StackOffset >= 0 ? $"R{FpReg}+{StackOffset}" : $"R{FpReg}{StackOffset}";
        }

        /// <summary>克隆并修改类型</summary>
        public ExpVar WithType(ExpType newType) => new(newType, Loc, RegNum, StackOffset, FpReg, Label, ImmValue, EvalAction, pointedTypeSize: PointedTypeSize, isReference: IsReference);

        /// <summary>克隆并修改寄存器号</summary>
        public ExpVar WithReg(int n) => new(Type, ExpLoc.Reg, regNum: n);

        /// <summary>克隆并设置指针所指类型的字节大小</summary>
        public ExpVar WithPointedTypeSize(int size) => new(Type, Loc, RegNum, StackOffset, FpReg, Label, ImmValue, EvalAction, pointedTypeSize: size, isReference: IsReference);

        /// <summary>克隆并设置引用标记</summary>
        public ExpVar WithIsReference(bool isRef = true) => new(Type, Loc, RegNum, StackOffset, FpReg, Label, ImmValue, EvalAction, pointedTypeSize: PointedTypeSize, isReference: isRef);

        public override string ToString() => Loc switch
        {
            ExpLoc.Reg => $"R{RegNum}:{Type}",
            ExpLoc.Stack => $"[{FormatOffset()}]:{Type}",
            ExpLoc.Data => $"{Label}:{Type}",
            ExpLoc.Imm => $"#{ImmValue}:{Type}",
            _ => "?"
        };
    }
}
