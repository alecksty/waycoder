using System;
using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>寄存器**类** —— 用户 2026-09-27 定下的模型：<c>Rn</c>=32位 / <c>Ln</c>=64位 / <c>Fn</c>=32位浮点 / <c>Dn</c>=64位浮点。</summary>
    public enum RegClass
    {
        /// <summary>不检查（内存/间接/立即数/标签位，或与寄存器无关的指令）。</summary>
        Any = 0,
        /// <summary>通用 32 位整数寄存器 R0–R15（编号 0–15）。</summary>
        Int,
        /// <summary>单精度 F0–F15（编号 0–15，与 R **同号不同组**）。</summary>
        Float,
        /// <summary>双精度 D0–D7（编号 **16–23**）。</summary>
        Double,
        /// <summary>长整数 L0–L7（编号 **24–31**）。</summary>
        Long,
    }

    /// <summary>
    /// 「这条指令的这个操作数该是**哪一类寄存器**」的**唯一判据** —— 汇编期校验、序列化拼写、
    /// 以及运行时的越界判据都从这一份推，免得三处各写一遍必然漂移
    /// （本仓头号坑就是"同一规则两处实现"）。
    ///
    /// <para>
    /// **为什么需要它**：VM 里那套编号空间是**统一**的（0–15 通用、16–23 = D0–D7、24–31 = L0–L7，
    /// 见 <see cref="RegisterSyntax"/>），而**类由助记符决定** —— 同一个 `@R0` 在 `MOVE` 里是整数、
    /// 在 `MOVEF` 里是浮点、在 `MOVED` 里是 D0、在 `MOVEL` 里是 L0。这种"数字同、含义看助记符"
    /// 的约定一旦**用错**就是**静默算错**（实测：32 位 `MOVE` 把值写进 `registers[]`、紧随的
    /// `MOVEL` 从 `longRegisters[]` 读 ⇒ 数组元素整个读成 0；`R8–R15` 更是**必然截断**）。
    /// 所以「用错寄存器」必须是**显式报错**，不能靠运行时兜。
    /// </para>
    ///
    /// <para>
    /// 表按**操作数位置**给（转换指令的两个操作数类不同：`I2L dst, src` = 先 L 后 Int）。
    /// 返回 <c>null</c> 或长度不足 = 该位置不检查。
    /// </para>
    /// </summary>
    public static class RegisterClassTable
    {
        /// <summary>该指令各操作数位置期望的寄存器类（null = 不检查；数组长度不足的位置 = 不检查）。</summary>
        public static RegClass[]? Expected(OpCode op) => Expected(op, 3);

        /// <summary>
        /// 按**操作数个数**取形态 —— 只有 64 位移位分两种写法，但**两种的寄存器位置不同**：
        /// <list type="bullet">
        /// <item><c>SHRL dst, src, count</c>（3 个）—— 第 3 个是**次数**；</item>
        /// <item><c>SHRL dst, count</c>（2 个）—— 第 2 个就是**次数**（原地移位）。</item>
        /// </list>
        /// VM 那边 <c>Count2(operands)</c> 正是这么取的，而且次数一律走
        /// <c>GetRegisterOrImmediate</c>（读 32 位 <c>registers[]</c>，16 项）——
        /// 第一版把两种形态都当成"三个长寄存器" ⇒ 次数发成 L 寄存器 ⇒
        /// 运行时 <c>IndexOutOfRangeException</c>（实测 `t64all` 崩在这）。
        /// </summary>
        public static RegClass[]? Expected(OpCode op, int operandCount) => op switch
        {
            OpCode.SHLL or OpCode.SHRL or OpCode.SHRUL
                => operandCount > 2 ? LongLInt3 : LongLInt2,
            _ => Expected3(op),
        };

        private static RegClass[]? Expected3(OpCode op) => op switch
        {
            // ── 搬运：类由助记符后缀定 ──
            OpCode.MOVE or OpCode.MOVEB or OpCode.MOVEH => Int1,
            OpCode.MOVEF => Float1,
            OpCode.MOVED => Double1,
            OpCode.MOVEL => Long1,

            // ── 压栈/弹栈 ──
            OpCode.PUSH or OpCode.PUSHB or OpCode.PUSHH or OpCode.POP or OpCode.POPB or OpCode.POPH => Int1,
            OpCode.FPUSH or OpCode.FPOP => Float1,
            OpCode.DPUSH or OpCode.DPOP => Double1,
            OpCode.PUSHL or OpCode.POPL => Long1,

            // ── 单精度算术/比较/取负 ──
            OpCode.FADD or OpCode.FSUB or OpCode.FMUL or OpCode.FDIV => Float3,
            OpCode.FCMP => Float2,
            OpCode.FNEG => Float2,

            // ── 双精度 ──
            OpCode.DADD or OpCode.DSUB or OpCode.DMUL or OpCode.DDIV => Double3,
            OpCode.DCMP => Double2,
            OpCode.DNEG => Double2,

            // ── 64 位整数（含有符号/无符号/位移/按位）──
            OpCode.ADDL or OpCode.SUBL or OpCode.MULL or OpCode.DIVL or OpCode.MODL
                or OpCode.DIVUL or OpCode.MODUL
                or OpCode.ANDL or OpCode.XORL => Long3,
            OpCode.NOTL or OpCode.NEGL => Long2,
            OpCode.CMPL or OpCode.CMPUL => Long2,

            // ── 转换：两个操作数的类**逐个不同**（先目标后源）──
            //
            // ⚠ **绝不能按"目标类"把几条归一组**（第一版就是这么写的：`L2I/F2I/D2I`
            //   当成 `[Int, Long]`）—— 它们的**源类**一个是 Long、一个是 Float、一个是
            //   Double，归成一组就等于让前端照着错的类发寄存器：实测 `d2i @R0 @R24`
            //   （拿 L0 当双精度读）⇒ 运行时 `无效的双精度寄存器：D8 或 F24`。
            OpCode.I2L => IFromI,       // dst Long ← src Int
            OpCode.F2L => IFromF,       // dst Long ← src Float
            OpCode.D2L => IFromD,       // dst Long ← src Double
            OpCode.L2I => IFromL,       // dst Int  ← src Long
            OpCode.F2I => IFromF2I,     // dst Int  ← src Float
            OpCode.D2I => IFromD2I,     // dst Int  ← src Double
            OpCode.I2F => IFromI2F,     // dst Float← src Int
            OpCode.L2F => IFromL2F,     // dst Float← src Long
            OpCode.D2F => IFromD2F,     // dst Float← src Double
            OpCode.F2D => IFromF2D,     // dst Double ← src Float
            OpCode.I2D => IFromI2D,     // dst Double ← src Int
            OpCode.L2D => IFromL2D,     // dst Double ← src Long
            OpCode.ZEXTL => IFromI,     // 零扩展 int→long（**不是** SEXT 那一类，源是 32 位）

            // ── 32 位整数（含无符号除法/取模、符号扩展）──
            OpCode.ADD or OpCode.SUB or OpCode.MUL or OpCode.DIV or OpCode.MOD
                or OpCode.AND or OpCode.OR or OpCode.XOR => Int3,
            OpCode.SHL or OpCode.SHR or OpCode.SHLV or OpCode.SHRV => Int3,
            OpCode.DIVU or OpCode.MODU => Int3,
            OpCode.NEG or OpCode.NOT or OpCode.TEST => Int2,
            OpCode.CMP => Int2,
            OpCode.INC or OpCode.DEC or OpCode.ROL or OpCode.ROR => Int1,
            OpCode.SEXTB or OpCode.SEXTH => Int2,
            OpCode.CMOVZ or OpCode.CMOVNZ => Int3,

            _ => null,
        };

        private static readonly RegClass[] Int1 = [RegClass.Int];
        private static readonly RegClass[] Int2 = [RegClass.Int, RegClass.Int];
        private static readonly RegClass[] Int3 = [RegClass.Int, RegClass.Int, RegClass.Int];
        private static readonly RegClass[] Float1 = [RegClass.Float];
        private static readonly RegClass[] Float2 = [RegClass.Float, RegClass.Float];
        private static readonly RegClass[] Float3 = [RegClass.Float, RegClass.Float, RegClass.Float];
        private static readonly RegClass[] Double1 = [RegClass.Double];
        private static readonly RegClass[] Double2 = [RegClass.Double, RegClass.Double];
        private static readonly RegClass[] Double3 = [RegClass.Double, RegClass.Double, RegClass.Double];
        private static readonly RegClass[] Long1 = [RegClass.Long];
        private static readonly RegClass[] Long2 = [RegClass.Long, RegClass.Long];
        private static readonly RegClass[] Long3 = [RegClass.Long, RegClass.Long, RegClass.Long];
        /// <summary>64 位移位（3 操作数）：dst/src 是长整数，**次数是 32 位**。</summary>
        private static readonly RegClass[] LongLInt3 = [RegClass.Long, RegClass.Long, RegClass.Int];
        /// <summary>64 位移位（2 操作数，原地）：`dst, count`。</summary>
        private static readonly RegClass[] LongLInt2 = [RegClass.Long, RegClass.Int];
        // 转换指令：逐条给（目标是哪个类、源是哪个类）
        private static readonly RegClass[] IFromI  = [RegClass.Long,   RegClass.Int];
        private static readonly RegClass[] IFromF  = [RegClass.Long,   RegClass.Float];
        private static readonly RegClass[] IFromD  = [RegClass.Long,   RegClass.Double];
        private static readonly RegClass[] IFromL  = [RegClass.Int,    RegClass.Long];
        private static readonly RegClass[] IFromF2I= [RegClass.Int,    RegClass.Float];
        private static readonly RegClass[] IFromD2I= [RegClass.Int,    RegClass.Double];
        private static readonly RegClass[] IFromI2F= [RegClass.Float,  RegClass.Int];
        private static readonly RegClass[] IFromL2F= [RegClass.Float,  RegClass.Long];
        private static readonly RegClass[] IFromD2F= [RegClass.Float,  RegClass.Double];
        private static readonly RegClass[] IFromF2D= [RegClass.Double, RegClass.Float];
        private static readonly RegClass[] IFromI2D= [RegClass.Double, RegClass.Int];
        private static readonly RegClass[] IFromL2D= [RegClass.Double, RegClass.Long];

        /// <summary>
        /// 该指令第 <paramref name="index"/> 个操作数该用的**寄存器基址**（进编号空间）。
        /// 前端发射指令时用它把"本类第 n 号寄存器"换算成编号 —— 与校验用的是**同一张表**，
        /// 免得"发射一个类、校验另一个类"（本仓头号坑）。
        /// </summary>
        public static int BankOfOperand(OpCode op, int index, int operandCount = 3)
        {
            var table = Expected(op, operandCount);
            var cls = (table != null && index < table.Length) ? table[index] : RegClass.Int;
            return cls switch
            {
                RegClass.Long => 24,
                RegClass.Double => 16,
                _ => 0,          // Int / Float / Any：F 与 R **同号不同组**
            };
        }

        /// <summary>该指令第 0 个操作数（对多数指令就是累加器/目标）的基址。</summary>
        public static int BankOf(OpCode op) => BankOfOperand(op, 0);

        /// <summary>编号范围（含端点）—— 与 <see cref="RegisterSyntax.TryParseName"/> 的映射同一张表。</summary>
        public static (int Lo, int Hi) Range(RegClass cls) => cls switch
        {
            RegClass.Int => (0, 15),
            RegClass.Float => (0, 15),
            RegClass.Double => (16, 23),
            RegClass.Long => (24, 31),
            _ => (0, 31),
        };

        /// <summary>类的可读名（报错用）。</summary>
        public static string Name(RegClass cls) => cls switch
        {
            RegClass.Int => "R0–R15（32 位通用）",
            RegClass.Float => "F0–F15（单精度）",
            RegClass.Double => "D0–D7（双精度，编号 16–23）",
            RegClass.Long => "L0–L7（64 位，编号 24–31）",
            _ => "任意",
        };

        /// <summary>
        /// 单个操作数是否合法。返回 null = 合法；否则是可读的错因（**不含**指令位置前缀）。
        /// 只检查 <see cref="OperandType.REGISTER"/>；内存/间接/立即数/标签一律放行
        /// （间接的 `@13` 是**地址寄存器**，本就该是通用寄存器，与值的类无关）。
        /// </summary>
        public static string? CheckOperand(OpCode op, int operandIndex, Operand operand, int operandCount = 3)
        {
            var table = Expected(op, operandCount);
            if (table == null || operandIndex >= table.Length) return null;
            var want = table[operandIndex];
            if (want == RegClass.Any) return null;
            if (operand.Type != OperandType.REGISTER) return null;

            int num = Convert.ToInt32(operand.Value);
            var (lo, hi) = Range(want);
            if (num >= lo && num <= hi) return null;

            // 越界的具体类型：说清"给的是什么、要的是什么"，别只说"非法"
            var given = num switch
            {
                >= 24 and <= 31 => "L" + (num - 24),
                >= 16 and <= 23 => "D" + (num - 16),
                >= 0 and <= 15 => "R" + num,
                _ => "R" + num,
            };
            return $"{op} 的第 {operandIndex + 1} 个操作数要 {Name(want)}，给的是 {given}（编号 {num}）";
        }

        /// <summary>校验整个程序，返回**全部**违规（每条含指令下标与可读错因）。</summary>
        public static List<string> Validate(VmlProgram program)
        {
            var errors = new List<string>();
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                for (int k = 0; k < instr.Operands.Count; k++)
                {
                    var err = CheckOperand(instr.Opcode, k, instr.Operands[k], instr.Operands.Count);
                    if (err != null)
                        errors.Add($"[{i}]" + (string.IsNullOrEmpty(instr.Label) ? "" : $" ({instr.Label})") + $" {err}");
                }
            }
            return errors;
        }
    }
}
