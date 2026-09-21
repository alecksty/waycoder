using System;

namespace VMLAssembler
{
    /// <summary>
    /// 「寄存器在**文本形态**里长什么样」的**唯一判据** —— 序列化（加 `@` 标记）与解析（认 / 剥 `@`）
    /// 两边共用这一份，免得两处各写一遍必然漂移。
    ///
    /// <para>
    /// 背景：汇编器判寄存器是「前缀 ∈ {R,F,D,L} + 后面 <c>int.TryParse</c> 能过」且**大小写不敏感**，
    /// 于是用户写的函数名 <c>f1</c> 会被读成浮点寄存器 <c>F1</c> ⇒ <c>call f1</c> 变成 <c>call R1</c>。
    /// 修法是给**文本**加一个显式标记：凡是我们自己写出来的寄存器一律写 <c>@R&lt;n&gt;</c>
    /// （内存操作数串里也一样，<c>R12-8</c> → <c>@R12-8</c>），解析时再把它剥掉 ——
    /// `@` 只活在文本格式里，**进内存的表示一个字节都没变**（还是 <c>Operand(REGISTER, n)</c>
    /// 与 <c>MEMORY("R12-8")</c>），所以前端与运行时都不用改。
    /// </para>
    ///
    /// <para>
    /// 三条配套规则（缺一不可，都在 <see cref="VmlAssembler"/> / <see cref="Operand"/> 里）：
    /// <list type="number">
    /// <item>(a) 标签位置的指令（CALL/JMP/Jcc/CATCH/LABEL）的操作数必是标签；</item>
    /// <item>(b) 一条指令里出现任一 `@` 标记 ⇒ 该指令里其余**裸** token 当标签；</item>
    /// <item>(c) 序列化时给所有寄存器写 `@`（本类提供判据）。</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class RegisterSyntax
    {
        /// <summary>
        /// 「这是一个寄存器名」——**裸名**判据（`@` 已剥掉之后用）：
        /// <list type="bullet">
        /// <item><c>R</c> + 0..31 ⇒ 寄存器号 = n</item>
        /// <item><c>F</c> + 0..15 ⇒ 寄存器号 = n</item>
        /// <item><c>D</c> + 0..7 ⇒ 寄存器号 = n+16（与运行时一致）</item>
        /// <item><c>L</c> + 0..7 ⇒ 寄存器号 = n+24（与运行时一致）</item>
        /// </list>
        /// 大小写不敏感（<c>OrdinalIgnoreCase</c>）。
        ///
        /// <para>
        /// ⚠**<c>R</c> 的上界是 31，不是"没有上界"**（v0.96.328 起）。0–31 是**统一编号空间**：
        /// 0–15 通用、16–23 = D0–D7、24–31 = L0–L7，正是运行时那三个数组
        /// （<c>int[16]</c> / <c>double[8]</c> / <c>long[8]</c>）的下标范围。序列化器给 D/L 写出来的
        /// 就是 <c>@R16</c>…<c>@R31</c>（<see cref="Operand.ToString"/> 一律写 <c>R&lt;n&gt;</c>），
        /// 所以这一档**必须有**；而 R32+ 在运行时**没有对应物**。
        /// </para>
        /// <para>
        /// 旧版对 <c>R</c> 不查上界 ⇒ <c>R99</c> 一路编到底、到运行时才以
        /// <c>IndexOutOfRangeException</c> 崩（`registers` 只有 16 个）。现在在解析点就拒绝，
        /// 由调用方按「越界」报错（见 <see cref="OutOfRangeReason"/>）。
        /// </para>
        /// </summary>
        public static bool TryParseName(string str, out int regNum)
        {
            regNum = 0;
            if (string.IsNullOrEmpty(str) || str.Length < 2) return false;

            char prefix = char.ToUpperInvariant(str[0]);
            if (prefix != 'R' && prefix != 'F' && prefix != 'D' && prefix != 'L') return false;
            if (!int.TryParse(str.Substring(1), out int n)) return false;

            switch (prefix)
            {
                case 'R': if (n < 0 || n > 31) return false; regNum = n; return true;
                case 'F': if (n < 0 || n > 15) return false; regNum = n; return true;
                case 'D': if (n < 0 || n > 7) return false; regNum = n + 16; return true;
                default:  if (n < 0 || n > 7) return false; regNum = n + 24; return true;   // 'L'
            }
        }

        /// <summary>
        /// 「**长得**像寄存器名」—— 前缀 ∈ {R,F,D,L} + 后面**全是数字**，**不查范围**。
        ///
        /// <para>
        /// 它**不是**第二条解析路径（判寄存器永远只用 <see cref="TryParseName"/>）——
        /// 只用来把「越界」与「根本不是寄存器名」分开，好给出可读的报错：
        /// <c>R99</c> 该报"通用寄存器只有 R0–R31"，而 <c>foo</c> 该按标签/符号走。
        /// 越界若不走这一条，就会**静默退化成**标签（<c>R99</c> 满足 <c>IsValidLabel</c>！）
        /// 或内存操作数，那正是这轮要消灭的那类缺陷。
        /// </para>
        /// </summary>
        public static bool HasRegisterShape(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length < 2) return false;
            char prefix = char.ToUpperInvariant(str[0]);
            if (prefix != 'R' && prefix != 'F' && prefix != 'D' && prefix != 'L') return false;
            for (int i = 1; i < str.Length; i++)
                if (!char.IsDigit(str[i])) return false;
            return true;
        }

        /// <summary>
        /// 越界时的报错文案（<c>null</c> = 压根不是"寄存器形"，调用方按别的分支走）。
        ///
        /// <para>
        /// 数字用 <c>long.TryParse</c> 兜底：<c>R99999999999999999999</c> 这种要把
        /// <c>int.TryParse</c> 溢出当成"越界"报出来，不能让它抛 <c>OverflowException</c>。
        /// </para>
        /// </summary>
        public static string? OutOfRangeReason(string str)
        {
            if (!HasRegisterShape(str)) return null;
            char prefix = char.ToUpperInvariant(str[0]);
            if (!long.TryParse(str.Substring(1), out long n)) n = long.MaxValue;

            return prefix switch
            {
                'R' when n > 31 => $"R{n}：通用寄存器只有 R0–R31（R0–R15 通用 / R16–R23 = D0–D7 / R24–R31 = L0–L7）",
                'F' when n > 15 => $"F{n}：浮点寄存器只有 F0–F15",
                'D' when n > 7  => $"D{n}：双精度寄存器只有 D0–D7",
                'L' when n > 7  => $"L{n}：长整数寄存器只有 L0–L7",
                _ => null,
            };
        }

        /// <summary>
        /// 「这是一段**寄存器引用**的文本」—— 序列化时**该不该加 `@`** 的判据，也是解析时
        /// 「这个 token 带标记了吗」的判据。认得两种形态：
        /// <list type="bullet">
        /// <item>纯寄存器名：<c>R0</c> / <c>F2</c> / <c>D0</c> / <c>L3</c></item>
        /// <item>寄存器相对地址：<c>R12-8</c> / <c>R14+16</c>（前端 <c>MemOff</c>/<c>FormatOffset</c> 的产物）</item>
        /// </list>
        /// **不认** AT&T 那一套（<c>8(R12)</c>）：它是另一种文本形态、由前端直接拼字符串产生，
        /// 加 `@` 会变成 <c>8(@R12)</c> —— 而运行时的 AT&T 分支只认括号里以 `R` 开头，
        /// 加了反而坏；它们本来就只在「内存操作数」里、不参与寄存器/标签的歧义。
        /// </summary>
        public static bool LooksLikeReference(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s.Contains('(') || s.Contains(')')) return false;      // AT&T 形态：不标记
            if (TryParseName(s, out _)) return true;

            // 寄存器相对地址：R<数字><+|-><数字>（只认 R bank —— 前端只产这一种）
            int i = 1;
            if (s.Length < 3 || char.ToUpperInvariant(s[0]) != 'R' || !char.IsDigit(s[1])) return false;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i == 1 || i >= s.Length || (s[i] != '+' && s[i] != '-')) return false;
            for (int j = i + 1; j < s.Length; j++)
                if (!char.IsDigit(s[j]) && s[j] != '#') return false;
            return i + 1 < s.Length;
        }

        /// <summary>去掉前导 `@`（`@R0` → `R0`、`@R12-8` → `R12-8`）。没有标记时逐字返回。</summary>
        public static string StripMarker(string s)
            => !string.IsNullOrEmpty(s) && s[0] == '@' ? s.Substring(1) : s;

        /// <summary>`@` 标记的写法（`R0` → `@R0`）。只在文本序列化时用。</summary>
        public static string Mark(string s) => "@" + s;
    }
}
