namespace VMLAssembler
{
    /// <summary>
    /// 操作数
    /// </summary>
    public class Operand
    {
        public OperandType Type { get; set; }
        public object Value { get; set; }
        public int Size { get; set; } = 32; // 操作数大小（位）

        /// <summary>
        /// 操作数构造函数
        /// </summary>
        /// <param name="type">操作数类型</param>
        /// <param name="value">操作数值</param>
        /// <param name="size">操作数大小（位）</param>
        public Operand(OperandType type, object value, int size = 32)
        {
            Type = type;
            Value = value;
            Size = size;
        }

        /// <summary>
        /// 序列化成 VML 文本（**(c) 规则**：寄存器一律带 `@` 标记）。
        ///
        /// <list type="bullet">
        /// <item><c>REGISTER</c> → <c>@R0</c>（寄存器号是全仓统一的：`F1`/`D1`/`L1`
        /// 分别就是 <c>R1</c>/<c>R17</c>/<c>R25</c>，所以一律写 <c>R&lt;n&gt;</c>）</item>
        /// <item><c>MEMORY</c> → <c>[@R12-8]</c>（**串里**的寄存器引用同样加标记 ——
        /// 「寄存器只活在文本格式里」这条对两种形态一视同仁）</item>
        /// <item><c>INDIRECT</c> → <c>[@R13]</c> / <c>[@R14-4]</c> —— **寻址一律写进方括号**，
        /// 与 <c>MEMORY</c> 同一个文本形态（运行时对两者本来就是同一条路：`GetAddress` + `GetMemory`）。
        /// 用户 2026-09-27 定的格式：**`@` 只做寄存器标记**，所以旧的裸 <c>@13</c> / <c>@R14-4</c> 已作废。</item>
        /// </list>
        ///
        /// <para>
        /// 为什么必须有这个标记：前端产出的 <c>move R0, f1</c>（f1 是**标签**）在**重新解析**时
        /// 会被读成 <c>F1</c> 寄存器 —— 而流水线确实会重新读文本（前端 → `.vml` 文本 →
        /// 汇编器再解析 → 链接器）。有了标记，(b) 规则（"这条指令里有 `@` ⇒ 其余裸 token 是标签"）
        /// 才成立。解析侧把 `@` 剥掉后**存进内存的还是裸名**（见 <see cref="RegisterSyntax"/>）。
        /// </para>
        /// </summary>
        /// <summary>
        /// 带上**寄存器类**的文本（`@L0` / `@D0` / `@F0` / `@R0`）—— 类由**助记符**决定
        /// （用户 2026-09-27 定的模型：`Rn`=32位 / `Ln`=64位 / `Fn`=32位 / `Dn`=64位）。
        ///
        /// <para>
        /// ⚠ 编号空间是**统一**的（0–15 通用、16–23=D0–D7、24–31=L0–L7），所以
        /// `movel @R24` 与 `movel @L0` 说的是同一只寄存器 —— 但前者**读的人看不出**
        /// 这是 64 位寄存器，而写错类（`movel @R0`）又恰恰是想让人一眼抓住的错。
        /// 解析两边都认（见 <c>RegisterSyntax.TryParseName</c>），这里只是**输出**怎么拼。
        /// </para>
        /// </summary>
        public string ToStringFor(OpCode op, int operandIndex, int operandCount)
        {
            if (Type != OperandType.REGISTER) return ToString() ?? "";
            int num;
            try { num = Convert.ToInt32(Value); } catch { return ToString() ?? ""; }
            var cls = RegisterClassTable.ClassOf(op, operandIndex, operandCount);
            return cls switch
            {
                RegClass.Long when num >= 24 => "@L" + (num - 24),
                RegClass.Double when num >= 16 => "@D" + (num - 16),
                RegClass.Float when num >= 0 && num < 16 => "@F" + num,
                _ => ToString() ?? "",
            };
        }

        public override string? ToString()
        {
            return Type switch
            {
                // ⚠ 寄存器号既可能是 int（绝大多数），也可能是**历史遗留的字符串**写法
                //   （`new Operand(REGISTER, "R1")`，全仓 4 处：`LoopOptimizationPass` 3 处 +
                //   `GoCompiler` 的演示工厂 1 处）。字符串那种在改动前渲染成 `RR1`（垃圾），
                //   这里按"它本来就是寄存器名"处理 ⇒ `@R1`，至少是**正确**的文本。
                OperandType.REGISTER => RegisterSyntax.Mark(Value is string raw ? raw : $"R{Value}"),
                OperandType.IMMEDIATE => $"#{Value}",
                OperandType.MEMORY => $"[{MarkMemoryText(Value)}]",
                OperandType.LABEL => $"{Value}",
                // ⚠ 寻址**一律进方括号**（`[@R1+n]`）——`@` 从此只做**寄存器标记**。
                //   值可能是 int（寄存器号）或 string（寄存器相对地址 `R14-4`）：
                //   前者要拼成 `R<n>` 才满足"`@` 后面必须是合法寄存器名"这条格式。
                OperandType.INDIRECT => $"[{MarkMemoryText(IndirectText(Value))}]",
                _ => Value.ToString()
            };
        }

        /// <summary>
        /// `INDIRECT` 的值 → 文本：寄存器号（<c>13</c>）补成寄存器名（<c>R13</c>），
        /// 字符串（寄存器相对地址 <c>R14-4</c>）原样。
        /// </summary>
        private static object IndirectText(object value) => value is int n ? $"R{n}" : value;

        /// <summary>内存操作数串里的寄存器引用加 `@`（`R12-8` → `@R12-8`）；标签等原样。</summary>
        private static object MarkMemoryText(object value)
            => value is string s && RegisterSyntax.LooksLikeReference(s) ? RegisterSyntax.Mark(s) : value;
    }
}
