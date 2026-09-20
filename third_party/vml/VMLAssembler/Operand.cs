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
        /// <item><c>INDIRECT</c> → <c>@{Value}</c>（本来就是 `@` 打头：<c>@13</c> / <c>@R14-4</c>）</item>
        /// </list>
        ///
        /// <para>
        /// 为什么必须有这个标记：前端产出的 <c>move R0, f1</c>（f1 是**标签**）在**重新解析**时
        /// 会被读成 <c>F1</c> 寄存器 —— 而流水线确实会重新读文本（前端 → `.vml` 文本 →
        /// 汇编器再解析 → 链接器）。有了标记，(b) 规则（"这条指令里有 `@` ⇒ 其余裸 token 是标签"）
        /// 才成立。解析侧把 `@` 剥掉后**存进内存的还是裸名**（见 <see cref="RegisterSyntax"/>）。
        /// </para>
        /// </summary>
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
                OperandType.INDIRECT => $"@{Value}",
                _ => Value.ToString()
            };
        }

        /// <summary>内存操作数串里的寄存器引用加 `@`（`R12-8` → `@R12-8`）；标签等原样。</summary>
        private static object MarkMemoryText(object value)
            => value is string s && RegisterSyntax.LooksLikeReference(s) ? RegisterSyntax.Mark(s) : value;
    }
}
