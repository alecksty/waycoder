using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace VMLAssembler
{
    /// <summary>标记字符串的宽度（用于区分 .string / .wstring / .ustring）</summary>
    public enum StringWidth
    {
        Byte = 8,
        Wide = 16,
        Unicode = 32
    }

    /// <summary>带宽度标记的字符串数据定义</summary>
    public class DataString
    {
        public string Value;
        public StringWidth Width;

        public DataString(string value, StringWidth width = StringWidth.Byte)
        {
            Value = value;
            Width = width;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// 宏定义
    /// </summary>
    public class MacroDefinition
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; }
        public List<string> BodyLines { get; set; }

        public MacroDefinition(string name)
        {
            Name = name;
            Parameters = new List<string>();
            BodyLines = new List<string>();
        }
    }

    /// <summary>
    /// VML 汇编器
    /// </summary>
    public partial class VmlAssembler
    {
        // 寄存器定义
        public const int NUM_GENERAL_REGISTERS = 16;
        public const int NUM_SPECIAL_REGISTERS = 8;

        private List<Instruction> instructions = new();
        private string entryPoint = "main";
        private int stackTop = 1024 * 1024;
        private int stackBottom = 0;
        private int vectorTable = 0;
        private int cpuSpeed = 0;
        private List<(int Address, int Size)> skipRegions = new();
        private Dictionary<string, int> labels = new();
        private Dictionary<string, object> dataSection = new();
        private Dictionary<string, object> constants = new();
        private Dictionary<string, MacroDefinition> macros = new();
        private Dictionary<string, string> exports = new(); // .export public → internal
        private List<string> linkedFiles = new();    // .linked <file>
        private HashSet<string> externs = new();     // .extern <label>

        // v1.65.167+: 旧指令名映射 (向后兼容 .vml 文件)
        private static readonly Dictionary<string, OpCode> SLegacyMap = new()
        {
            ["LOAD"] = OpCode.MOVE,
            ["STORE"] = OpCode.MOVE,
            ["LOADB"] = OpCode.MOVEB,
            ["STOREB"] = OpCode.MOVEB,
            ["LOADH"] = OpCode.MOVEH,
            ["STOREH"] = OpCode.MOVEH,
            ["FLOAD"] = OpCode.MOVEF,
            ["FSTORE"] = OpCode.MOVEF,
            ["DLOAD"] = OpCode.MOVED,
            ["DSTORE"] = OpCode.MOVED,
            ["LOADD"] = OpCode.MOVED,   // 旧双精度加载指令名
            ["STORED"] = OpCode.MOVED,  // 旧双精度存储指令名
            ["LEA"] = OpCode.MOVE,


        };

        // v1.65.170+: STORE-family 旧指令需要交换操作数 (旧格式 src-first, 新格式 dst-first)
        private static readonly HashSet<string> s_storeLegacyNames = new()
        {
            "STORE",
            "STOREB",
            "STOREH",
            "FSTORE",
            "DSTORE",
            "STORED",  // 旧双精度存储指令名
        };

        private int currentAddress;
        private string? _lastLabel;
        private string? _lastDataLabel;
        private List<object>? _lastDataValues;
        private bool _inDataSection;

        /// <summary>待裁定的「寄存器形」操作数来自哪种写法 —— 决定裁定成标签时该变成什么。</summary>
        private enum RegShapedKind
        {
            /// <summary>裸 token：标签 ⇒ <c>LABEL</c>，寄存器 ⇒ <c>REGISTER</c>。</summary>
            Bare,
            /// <summary><c>[...]</c> 里的内容：标签 ⇒ <c>MEMORY</c>，寄存器 ⇒ <c>INDIRECT</c>。</summary>
            Bracket,
        }

        /// <summary>
        /// 「寄存器形 token」的**待裁定表** —— 解析到它的当时只知道**形状**，不知道它到底是不是标签
        /// （标签可以定义在使用点**之后**，前向引用是常态），所以先登记下来，整份文件解析完再定
        /// （见 <see cref="ResolveRegisterShapedNames"/>）。
        ///
        /// <para>
        /// 为什么要这么绕：**形状判不出归属**。用户完全可以把全局变量叫 <c>f1</c> ——
        /// 实测那条链是「C 前端为全局标量产出 <c>[f1]</c> → 按形状读成寄存器 F1 → 运行时
        /// <c>registers[17]</c> 直接 <c>IndexOutOfRangeException</c>」（<c>d1</c> 走 D1 = 17）。
        /// 真正有信息量的是「**本文件有没有定义同名标签**」。
        /// </para>
        /// </summary>
        private readonly List<(Operand Op, string Name, RegShapedKind Kind, int Address)> _pendingRegShaped = [];

        /// <summary>
        /// 初始化汇编器
        /// </summary>
        public VmlAssembler()
        {
            Reset();
        }

        /// <summary>
        /// 解析操作数
        /// </summary>
        /// <param name="operandStr">操作数字符串</param>
        /// <param name="bareTokensAreLabels">
        /// 「这条指令里的**裸** token 不是寄存器」模式 —— 由 <see cref="ParseLine"/> 按两条规则决定：
        /// <list type="bullet">
        /// <item>(a) **标签位置的指令**（CALL / JMP / Jcc / CATCH / LABEL）：操作数只能是标签；</item>
        /// <item>(b) **本指令里出现了任一 `@` 标记的寄存器**（`@R0`）：其余裸 token 一律当标签
        /// （用户的原话："`move @R, f1` 说明第二个就不是寄存器"）。</item>
        /// </list>
        /// 置位后**裸**的 `R0` / `f1` / `d2` 一类 token 不再当寄存器（`@R0` 仍然当寄存器），
        /// `#` / `[...]` / `@` 三种写法与平时完全一样。
        /// </param>
        /// <returns>解析后的操作数</returns>
        /// <exception cref="ArgumentException">操作数字符串格式错误</exception>
        /// <param name="labelPosition">
        /// 本操作数处在**标签位置**（CALL / JMP / Jcc / CATCH / LABEL 的操作数）。这类位置的裸 token
        /// **只能是标签**，不参与「寄存器形」的推迟裁定 —— 见下面那段的说明。
        /// </param>
        public Operand ParseOperand(string operandStr, bool bareTokensAreLabels = false, bool labelPosition = false)
        {
            operandStr = operandStr.Trim();

            // 显式寄存器标记：@R0 / @F1 / @D2 / @L3 =「这个 token 是寄存器」。
            //
            // 这是 (b)/(c) 两条规则赖以成立的那个记号：序列化器给每个寄存器操作数写 `@`
            // （见 `Operand.ToString`），于是"一条指令里有 `@` ⇒ 裸 token 是标签"就永远成立。
            //
            // ⚠ **它改变了 `@` 的旧含义**：`@R0` 以前是「以 R0 为地址的间接寻址」
            //   （INDIRECT），现在是寄存器本身。间接寻址的写法是 `[R0]`（语义逐字相同，
            //   库存里唯一一处 `@R0` 间接用法 `Lib/shared/src/builtins.c` 的 peek/poke
            //   已一并改成 `[R0]`）。`@R14-4` / `@13` / `@[x]` 这些**非纯寄存器名**的
            //   间接写法一个字没动。
            string afterAt = operandStr.StartsWith("@") ? operandStr.Substring(1) : null;
            if (afterAt != null && TryParseRegisterName(afterAt, out int markedReg))
            {
                return new Operand(OperandType.REGISTER, markedReg);
            }

            // `@` 打头、长得像寄存器名却**越界**（`@R99` / `@F20`）⇒ 报错。
            // 不能放它过去：下面那条 `@` 分支会把它当**间接寻址**（`INDIRECT("R99")`）静默收下。
            if (afterAt != null && RegisterSyntax.OutOfRangeReason(afterAt) is string atReason)
            {
                throw new ArgumentException($"寄存器名越界：@{afterAt} —— {atReason}");
            }

            // ── 寄存器形的**裸** token：归属**推迟裁定** ─────────────────────────────
            //
            // 只看形状定不了它是寄存器还是标签：用户完全可以把全局变量叫 `f1` / `d1` ——
            // C 前端为全局标量产出的就是 `[f1]` 这种标签名，按形状读成寄存器 F1 之后
            // 运行时读 R1（`d1` 更狠，D1 = 寄存器 17）直接越界崩。
            // 真正有信息量的是「**本文件有没有定义同名标签**」，而标签可以定义在使用点**之后**
            // ⇒ 这里只登记形状与位置，等整份文件解析完再定（`ResolveRegisterShapedNames`）。
            //
            // ⚠ **标签位置除外**（规则 (a)）：`call f1` 的 `f1` 只能是标签，判不出来就该报
            //   「未找到标签」，绝不能悄悄变回 `call F1`（间接调用）—— 那正是语法变更前
            //   那类静默歧义的原始形态。间接调用按新写法写 `call @R0`。
            if (!labelPosition && RegisterSyntax.HasRegisterShape(operandStr))
            {
                // 越界的（`R99`）形状照样登记 —— 裁定完发现它不是标签，才报「越界」；
                // 若它**是**本文件的标签，那它就是那个标签（`int r99;` 完全合法）。
                var shaped = TryParseRegisterName(operandStr, out int shapedReg)
                    ? new Operand(OperandType.REGISTER, shapedReg)
                    : new Operand(OperandType.REGISTER, 0);
                _pendingRegShaped.Add((shaped, operandStr, RegShapedKind.Bare, currentAddress));
                return shaped;
            }

            // 立即数：#value 或直接数字
            if (operandStr.StartsWith("#"))
            {
                string valueStr = operandStr.Substring(1);
                // 检查是否是寄存器
                if (valueStr.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                {
                    string regNumStr = valueStr.Substring(1);
                    if (int.TryParse(regNumStr, out int regNum))
                    {
                        return new Operand(OperandType.REGISTER, regNum);
                    }
                }

                // 否则，解析为立即数
                object value = ParseValue(valueStr);
                return new Operand(OperandType.IMMEDIATE, value);
            }

            // 间接寻址：@reg 或 @[addr]
            if (operandStr.StartsWith("@"))
            {
                string inner = afterAt;
                if (inner.StartsWith("[") && inner.EndsWith("]"))
                {
                    object addr = ParseValue(inner.Substring(1, inner.Length - 2));
                    return new Operand(OperandType.INDIRECT, addr);
                }

                object value = ParseValue(inner);
                return new Operand(OperandType.INDIRECT, value);
            }

            // 内存寻址：[address]
            if (operandStr.StartsWith("[") && operandStr.EndsWith("]"))
            {
                string addrStr = operandStr.Substring(1, operandStr.Length - 2).Trim();
                // `[@R12-8]` / `[@R0]`：串里也带 `@` 标记（序列化器写的形态）——
                // 剥掉之后与裸写法走的是**同一条**路，所以内存表示与改动前逐字相同
                addrStr = RegisterSyntax.StripMarker(addrStr);

                // 寄存器形的内容（`[R0]` / `[f1]` / `[R99]`）：**归属推迟裁定**，理由与裸 token 同。
                //
                // ⚠ 这里**不能**直接按寄存器收下 —— 那正是「全局变量叫 f1」整条链崩掉的入口：
                //   C 前端为全局标量产出 `[f1]`，`f1` 形状上就是 F1，于是变成 `INDIRECT(1)`，
                //   运行时拿 R1 里的垃圾当地址。判据只能是「本文件有没有名叫 f1 的标签」，
                //   而标签集要等整份文件解析完才齐。
                if (RegisterSyntax.HasRegisterShape(addrStr))
                {
                    var shapedMem = TryParseRegisterName(addrStr, out int innerReg)
                        ? new Operand(OperandType.INDIRECT, innerReg)
                        : new Operand(OperandType.INDIRECT, 0);
                    _pendingRegShaped.Add((shapedMem, addrStr, RegShapedKind.Bracket, currentAddress));
                    return shapedMem;
                }

                // 检查是否是标签
                if (IsValidLabel(addrStr))
                {
                    return new Operand(OperandType.MEMORY, addrStr);
                }

                // 尝试解析为值
                object addr = ParseValue(addrStr);
                return new Operand(OperandType.MEMORY, addr);
            }

            // 标签或符号
            if (IsValidLabel(operandStr))
            {
                return new Operand(OperandType.LABEL, operandStr);
            }

            // GCC-style ASM placeholder: %0, %1, %rax, etc.
            if (operandStr.StartsWith("%"))
            {
                return new Operand(OperandType.IMMEDIATE, operandStr);
            }

            // 尝试解析为立即数
            try
            {
                object value = ParseValue(operandStr);
                return new Operand(OperandType.IMMEDIATE, value);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"无法解析操作数：{operandStr}");
            }
        }

        /// <summary>
        /// **(a) 规则**：这条指令的操作数是**标签位置**吗（`CALL` / `JMP` / 条件跳转 / `CATCH` / `LABEL`）。
        ///
        /// <para>
        /// 这些指令的跳转目标**只能是标签**（`VMLRuntime` 的 `ExecuteJmp` 就是
        /// "取第一个 LABEL 操作数"、`ExecuteCall` 取 `operands[0]`），所以裸的 `f1` 在这里
        /// 不可能是寄存器 —— 除非它带 `@` 标记（那是**间接**调用 `call @R0`，见 `ExecuteCall`
        /// 的 `REGISTER` 分支）。
        /// </para>
        /// <para>
        /// `JZ R0, label` 那种"寄存器 + 标签"的组合不受影响：`r0` 由序列化器写成 `@R0`
        /// （见 `Operand.ToString`），解析时走 `@` 标记分支仍是寄存器。
        /// </para>
        /// </summary>
        private static bool IsLabelPositionOpcode(OpCode opcode) => opcode switch
        {
            OpCode.CALL or OpCode.JMP or OpCode.JZ or OpCode.JNZ or OpCode.JE or OpCode.JNE
                or OpCode.JG or OpCode.JL or OpCode.JGE or OpCode.JLE or OpCode.CATCH or OpCode.LABEL => true,
            _ => false,
        };

        /// <summary>
        /// **(b) 规则**：这条指令里出现了 `@` 标记的寄存器吗（`@R0` / `@F1` / `@D2` / `@L3`）。
        ///
        /// <para>
        /// 出现了 ⇒ **本指令里其余的裸 token 一律当标签**。这是 (c)（序列化器给所有寄存器
        /// 写 `@`）成立之后，`move @R0, f1` 这种写法唯一的解释方式 —— 用户的原话是
        /// "`move @R, f1` 说明第二个就不是寄存器"。
        /// </para>
        /// </summary>
        /// <summary>
        /// 把待裁定的「寄存器形 token」按**本文件是否定义了同名标签**定下来 —— 整份文件解析完之后跑。
        ///
        /// <para>
        /// **规则**：一个不带 `@` 的寄存器形 token（<c>f1</c> / <c>d2</c> / <c>r100</c> / <c>R0</c>）——
        /// <list type="bullet">
        /// <item>本文件定义了**同名标签** ⇒ 它是**标签**（这是「变量就叫 f1」能正常工作的全部秘密）；</item>
        /// <item>否则 ⇒ 它是**寄存器**；越界（<c>R99</c> / <c>F20</c>）则**报错**。</item>
        /// </list>
        /// </para>
        /// <para>
        /// 为什么由标签集来裁定、而不是按形状猜：形状没有信息量（<c>f1</c> 与浮点寄存器 F1 逐字相同），
        /// 而**定义**是作者写下的、唯一的显式信号。这也让两条老规则各归其位 ——
        /// (a) 标签位置仍然是「只能是标签」；(b) 带 `@` 的仍然是「只能是寄存器」，
        /// 这里的裁定只管**两者都没说的**那些。
        /// </para>
        /// <para>
        /// ⚠ 代价（有意接受）：真定义了名叫 <c>R0</c> 的标签时，那条 <c>[R0]</c> 会变成标签引用 ——
        /// 要寄存器间接就写 <c>[@R0]</c>（`@` 永远优先）。`R0` 当变量名远比 <c>f1</c>/<c>d1</c>/<c>l1</c>
        /// 罕见，且**编译器自己产出的寄存器引用一律带 `@`**，所以这条代价只落在手写汇编上。
        /// </para>
        /// </summary>
        private void ResolveRegisterShapedNames()
        {
            foreach (var (op, name, kind, addr) in _pendingRegShaped)
            {
                if (labels.ContainsKey(name))
                {
                    // 同名标签存在 ⇒ 它就是这个标签（`f1` 作变量名 / 函数名的主路径）
                    op.Type = kind == RegShapedKind.Bracket ? OperandType.MEMORY : OperandType.LABEL;
                    op.Value = name;
                    continue;
                }

                // 不是标签 ⇒ 只能是寄存器。在范围内的，`ParseOperand` 登记时已经写好了号。
                if (TryParseRegisterName(name, out int reg))
                {
                    op.Type = kind == RegShapedKind.Bracket ? OperandType.INDIRECT : OperandType.REGISTER;
                    op.Value = reg;
                    continue;
                }

                // 既不是本文件的标签，又不是合法寄存器 ⇒ 越界，报出来。
                // （这条路以前是**静默**的：`R99` 满足 `IsValidLabel` ⇒ 悄悄变成标签或内存操作数。）
                throw new ArgumentException(
                    $"寄存器名越界：{(kind == RegShapedKind.Bracket ? $"[{name}]" : name)}（地址 {addr}）"
                    + $"—— {RegisterSyntax.OutOfRangeReason(name)}"
                    + "（若本意是标签，请在本文件里定义它）");
            }
        }

        private static bool HasMarkedRegister(List<string> operandStrs)
        {
            foreach (var s in operandStrs)
            {
                if (s == null || s.Length < 2) continue;

                // ① 自身就是带标记的寄存器操作数：`@R0`
                if (s[0] == '@' && RegisterSyntax.LooksLikeReference(s.Substring(1)))
                    return true;

                // ② 内存操作数里带标记：`[@R12-8]` / `[@R0]`
                if (s[0] == '[' && s.Length > 2 && s[1] == '@' &&
                    RegisterSyntax.LooksLikeReference(RegisterSyntax.StripMarker(s.Substring(2).TrimEnd(']'))))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 「这是一个寄存器名」的**唯一判据** —— 实现见 <see cref="RegisterSyntax.TryParseName"/>
        /// （序列化侧的 <see cref="RegisterSyntax.LooksLikeReference"/> 与它同一份规则；
        /// 裸 token 与 `@` 标记两种写法也共用它）。
        /// </summary>
        private static bool TryParseRegisterName(string str, out int regNum)
            => RegisterSyntax.TryParseName(str, out regNum);

        /// <summary>检查字符串是否是寄存器名 (R0-R31, F0-F15, D0-D7, L0-L7)。
        ///
        /// <para>
        /// ⚠ 这里**曾是第二份判据**：前缀 ∈ {R,F,D,L} + <c>int.TryParse</c> 能过，
        /// **不查任何范围**。于是同一条规则两份口径 —— 顶层判据（<see cref="TryParseRegisterName"/>）
        /// 拒掉的 <c>F20</c>，在 <c>[...]</c> 里照收；<c>L9</c> 也一样。现在只剩一份。
        /// </para>
        /// </summary>
        private static bool IsRegisterName(string str) => RegisterSyntax.TryParseName(str, out _);

        private bool IsValidLabel(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            if (!char.IsLetter(str[0]) && str[0] != '_')
                return false;
            for (int i = 1; i < str.Length; i++)
            {
                if (!char.IsLetterOrDigit(str[i]) && str[i] != '_' && str[i] != '$')
                    return false;
            }

            return true;
        }

        public object ParseValue(string valueStr)
        {
            valueStr = valueStr.Trim();

            // 处理字符串值
            if ((valueStr.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && valueStr.EndsWith("\"")) ||
                (valueStr.StartsWith("'", StringComparison.OrdinalIgnoreCase) && valueStr.EndsWith("'")))
            {
                // 检查是否是空字符串
                if (valueStr.Length < 2)
                {
                    // 空字符串或残损字符串: 返回空串继续编译
                    return "";
                }

                string str = valueStr.Substring(1, valueStr.Length - 2);
                // 处理转义序列
                str = str.Replace("\\n", "\n");
                str = str.Replace("\\t", "\t");
                str = str.Replace("\\r", "\r");
                str = str.Replace("\\\"", "\"");
                str = str.Replace("\\'", "'");
                str = str.Replace("\\\\", "\\");

                // 如果是单字符字面量，返回字符的ASCII码
                if (valueStr.StartsWith("'") && str.Length == 1)
                {
                    return (int)str[0];
                }

                return str;
            }

            if (valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(valueStr, 16);
            }
            else if (valueStr.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                if (valueStr.Length > 2)
                {
                    return Convert.ToInt32(valueStr.Substring(2), 2);
                }

                throw new FormatException("无效的二进制数");
            }
            else if (valueStr.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                if (valueStr.Length > 2)
                {
                    return Convert.ToInt32(valueStr.Substring(2), 8);
                }

                throw new FormatException("无效的八进制数");
            }
            else
            {
                // 先尝试解析为整数
                if (int.TryParse(valueStr, out int intValue))
                {
                    return intValue;
                }

                // 尝试解析为长整数（用于 .dword 的 IEEE 754 位模式等 64 位整数值）
                if (long.TryParse(valueStr, out long longValue))
                {
                    return longValue;
                }

                // 再尝试解析为浮点数
                if (double.TryParse(valueStr, out double doubleValue))
                {
                    return doubleValue;
                }

                // 如果不是数字，返回原始字符串
                return valueStr;
            }
        }

        /// <summary>
        /// 剥掉行内注释（`;` 与 `//`）—— **引号感知**，字符串字面量里的 `;` 不算注释。
        ///
        /// ⚠ **本文件里这条规则只许有这一份实现**。此前有三处：`ProcessSingleLine` 那份
        /// 是引号感知的，而 `ParseLine` 与 `ParseData` 这两份是裸 `IndexOf(";")`
        /// ⇒ **同一份 `.string` 走哪条路结果不同**，而且症状离现场很远：
        /// `L_x: .string "B1=AB;CD"` 被截成 `.string "B1=AB`（引号不配对 ⇒
        /// `ParseValue` 落到"不是字符串"那一支，把**开头那个引号**当内容返回）
        /// ⇒ 程序运行时打出 `"B1=AB`。
        ///
        /// 影响面：**所有带分号的字符串**。最要命的是 **ANSI 转义序列全是分号分隔的**
        /// （`\x1b[1;31m`、`\x1b[38;5;208m`）⇒ 256 色/真彩**整类失效**，
        /// 且失败得很安静（只是颜色不对，程序照跑）。
        ///
        /// 单引号（字符字面量 `';'`）同样要认：反过来，双引号串里的 `'`
        /// （如 `"it's"`）不该被当成字符字面量开头，所以两种引号各自独立跟踪。
        /// </summary>
        private static string StripLineComment(string line)
        {
            bool inString = false;   // 双引号字符串
            bool inChar   = false;   // 单引号字符字面量

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (c == '\\') { i++; continue; }   // 转义的下一个字符不参与判定（`\"` 不闭合）
                    if (c == '"')  inString = false;
                    continue;
                }
                if (inChar)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '\'') inChar = false;
                    continue;
                }

                if (c == '"')  { inString = true; continue; }
                if (c == '\'') { inChar   = true; continue; }
                if (c == ';')  return line[..i].TrimEnd();
                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                    return line[..i].TrimEnd();
            }
            return line;
        }

        public Instruction? ParseLine(string line)
        {
            line = line.Trim();

            // 空行与整行注释（`;` / `//` 在引号外才生效 —— 见 StripLineComment）
            line = StripLineComment(line);
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            // 检查标签（支持 "label1 label2:" 多标签语法，全部指向同一地址）
            string? label = null;
            if (line.Contains(":"))
            {
                string[] labelParts = line.Split(new[] { ":" }, 2, StringSplitOptions.None);
                string labelText = labelParts[0].Trim();
                // 按空格拆分标签文本，每个部分都是独立标签
                var multiLabels = labelText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var lbl in multiLabels)
                {
                    labels[lbl] = currentAddress;
                }

                label = multiLabels.Length > 0 ? multiLabels[0] : labelText;
                _lastLabel = label;
                line = labelParts[1].Trim();
                if (string.IsNullOrEmpty(line))
                {
                    return null;
                }
            }

            // 解析指令
            string[] parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            string opcodeName = parts[0].ToUpper();

            // v1.65.170+: 单 token 行无冒号且不是已知操作码 → 注释残留(如 "shared_;...")，跳过
            if (parts.Length == 1 && !line.Contains(":") &&
                !SLegacyMap.ContainsKey(opcodeName) && !Enum.TryParse<OpCode>(opcodeName, out _))
            {
                return null;
            }

            // 处理 LABEL 指令（支持 "LABEL name" 格式）
            if (opcodeName == "LABEL")
            {
                if (parts.Length > 1)
                {
                    string labelName = parts[1].Trim();
                    labels[labelName] = currentAddress;
                    _lastLabel = labelName;
                }

                return null; // LABEL 指令不生成实际的指令
            }

            // 处理 "name:" 格式的标签（已在上面处理）
            // 如果标签格式正确但 opcode 为空，跳过

            // STR 伪指令 — 在代码段中定义字符串常量
            // 格式: STR "text" (通常前有 LABEL name 指令)
            // 将字符串存入 dataSection，标签指向 data 地址
            if (opcodeName == "STR")
            {
                if (parts.Length > 1)
                {
                    string strValue = parts[1].Trim();
                    // 去掉引号
                    if (strValue.StartsWith("\"") && strValue.EndsWith("\""))
                    {
                        strValue = strValue.Substring(1, strValue.Length - 2);
                    }

                    // 使用上一个 LABEL 指令设置的名字（如果有）
                    string labelName = _lastLabel ?? $"_str_{Guid.NewGuid():N}";
                    // 确保 MOVE R0, label 能解析: labels 先记录代码偏移, 运行时 dataSection 初始化时覆盖为堆地址
                    if (!labels.ContainsKey(labelName))
                        labels[labelName] = currentAddress;
                    dataSection[labelName] = strValue;
                    _lastLabel = null;
                }

                return null;
            }

            OpCode opcode;
            bool isLegacyStore = false;
            // v1.65.167+: 旧指令名映射 (LOAD→MOVE, STORE→MOVE, 等)
            if (SLegacyMap.TryGetValue(opcodeName, out opcode))
            {
                isLegacyStore = s_storeLegacyNames.Contains(opcodeName);
            }
            else if (!Enum.TryParse(opcodeName, out opcode))
            {
                throw new ArgumentException($"未知指令：{opcodeName}");
            }

            // 解析操作数
            var operands = new List<Operand>();

            if (parts.Length > 1)
            {
                if (opcode == OpCode.ASM)
                {
                    // ASM 指令：展开内联汇编字符串为实际指令
                    var firstQuote = line.IndexOf('"');
                    var lastQuote = line.LastIndexOf('"');
                    if (firstQuote >= 0 && lastQuote > firstQuote)
                    {
                        string asmContent = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                        // 去除可能包含的尾引号（多行 ASM 内容时）
                        asmContent = asmContent.TrimEnd('"').Trim();
                        // 将 ASM 字符串按行拆分为独立的 VML 指令/伪指令
                        foreach (var asmLine in asmContent.Split('\n'))
                        {
                            var trimmedLine = asmLine.Trim();
                            if (string.IsNullOrEmpty(trimmedLine)) continue;
                            // 伪指令处理 (.export, .data, .skip 等)
                            if (trimmedLine.StartsWith(".export", StringComparison.OrdinalIgnoreCase))
                            {
                                ParsePseudoOp(trimmedLine);
                                continue;
                            }
                            var asmInstr = ParseLine(trimmedLine);
                            if (asmInstr != null)
                                instructions.Add(asmInstr);
                        }
                    }

                    return null; // ASM 已展开，不需要添加 ASM 指令
                }
                else
                {
                    // 智能分割操作数，处理带引号的字符串
                    var operandStrs = SplitOperands(parts[1]);

                    // 「裸 token 不是寄存器」模式 —— 两条规则（见 `ParseOperand` 的 bareTokensAreLabels），
                    // 判据合在一处，22 个前端/手写汇编走的是同一条路。
                    // `labelPosition` **单独留一份**：它要传给 `ParseOperand` 做「寄存器形 token
                    // 推迟裁定」的例外（标签位置只能当标签，见那里的说明）。
                    bool labelPosition = IsLabelPositionOpcode(opcode);
                    bool bareTokensAreLabels = labelPosition || HasMarkedRegister(operandStrs);

                    foreach (var opStr in operandStrs)
                    {
                        operands.Add(ParseOperand(opStr, bareTokensAreLabels, labelPosition));
                    }
                }
            }

            // v1.65.170+: STORE-family 旧指令交换操作数 (STORE src,dst → MOVE dst,src)
            if (isLegacyStore && operands.Count >= 2)
            {
                operands.Reverse();
            }

            Instruction instr = new Instruction(opcode, operands, currentAddress, label);
            currentAddress++;
            return instr;
        }

        private List<string> SplitOperands(string operandStr)
        {
            List<string> operands = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < operandStr.Length; i++)
            {
                char c = operandStr[i];

                if (c == '\\' && i + 1 < operandStr.Length && (operandStr[i + 1] == '"' || operandStr[i + 1] == '\''))
                {
                    current.Append(c);
                    current.Append(operandStr[++i]);
                }
                else if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                    current.Append(c);
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                    current.Append(c);
                }
                else if (!inQuotes && (char.IsWhiteSpace(c) || c == ','))
                {
                    if (current.Length > 0)
                    {
                        operands.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                operands.Add(current.ToString());
            }

            return operands;
        }

        private void ParsePseudoOp(string line)
        {
            line = line.Trim();
            if (line.StartsWith(".entry"))
            {
                var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1) entryPoint = parts[1].Trim();
            }
            else if (line.StartsWith(".stack"))
            {
                var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    // .stack address, size
                    int addr = (int)ParseValue(parts[1].TrimEnd(','));
                    int size = (int)ParseValue(parts[2].Trim());
                    stackBottom = addr;
                    stackTop = addr + size;
                }
                else if (parts.Length > 1)
                {
                    // .stack top (single param, backward compatible)
                    stackTop = (int)ParseValue(parts[1].Trim());
                }
            }
            else if (line.StartsWith(".vectors"))
            {
                var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1) vectorTable = (int)ParseValue(parts[1].Trim());
            }
            else if (line.StartsWith(".skip"))
            {
                // .skip <address>, <size> — 保留内存区域，防止分配器覆盖
                // 例: .skip 0x6000, 0x1000  (保护字库和VGA寄存器区域)
                var parts = line.Split(new[] { ' ', ',', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    int addr = (int)ParseValue(parts[1].Trim());
                    int size = (int)ParseValue(parts[2].Trim());
                    skipRegions.Add((addr, size));
                }
            }
            else if (line.StartsWith(".speed", StringComparison.OrdinalIgnoreCase))
            {
                // .speed 0        → 全速
                // .speed 1M       → 1,000,000 指令/秒
                // .speed 1000000  → 1,000,000 指令/秒
                var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    string speedStr = parts[1].Trim();
                    if (speedStr.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                    {
                        speedStr = speedStr.Substring(0, speedStr.Length - 1);
                        cpuSpeed = (int)(double.Parse(speedStr) * 1_000_000);
                    }
                    else if (speedStr.EndsWith("K", StringComparison.OrdinalIgnoreCase))
                    {
                        speedStr = speedStr.Substring(0, speedStr.Length - 1);
                        cpuSpeed = (int)(double.Parse(speedStr) * 1_000);
                    }
                    else
                    {
                        cpuSpeed = (int)ParseValue(speedStr);
                    }
                }
            }
            else if (line.StartsWith(".chipasm"))
            {
                // .chipasm "arch", "code"
                // e.g. .chipasm "6502", "MOV X, Y"
                // Parse quoted strings
                int firstQuote = line.IndexOf('"');
                if (firstQuote >= 0)
                {
                    int secondQuote = line.IndexOf('"', firstQuote + 1);
                    if (secondQuote > firstQuote)
                    {
                        string arch = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                        int thirdQuote = line.IndexOf('"', secondQuote + 1);
                        if (thirdQuote >= 0)
                        {
                            int fourthQuote = line.IndexOf('"', thirdQuote + 1);
                            if (fourthQuote > thirdQuote)
                            {
                                string code = line.Substring(thirdQuote + 1, fourthQuote - thirdQuote - 1);
                                var ops = new List<Operand> { new Operand(OperandType.LABEL, arch), new Operand(OperandType.LABEL, code) };
                                instructions.Add(new Instruction(OpCode.CHIPASM, ops));
                            }
                        }
                    }
                }
            }
            else if (line.StartsWith(".export", StringComparison.OrdinalIgnoreCase))
            {
                // .export public_name internal_name
                // 库文件中声明公开API映射: 链接器自动为 public_name 创建别名
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    string publicName = parts[1].Trim();
                    string internalName = parts[2].Trim();
                    if (!exports.ContainsKey(publicName))
                        exports[publicName] = internalName;
                }
            }
        }

        public void ParseData(string line)
        {
            line = line.Trim();

            // 行内注释（引号感知 —— 见 StripLineComment，`.string "a;b"` 的 `;` 不是注释）
            line = StripLineComment(line);

            // 处理 .data 段开始
            if (line == ".data")
            {
                // 开始数据段，不需要处理
                return;
            }

            // 处理 .text 段开始
            if (line == ".text")
            {
                FinalizeMultiWordData();
                // 开始代码段，不需要处理
                return;
            }

            // 处理 .global 指令（v1.66.31+: 必须带点前缀）
            if (line == ".global" || line.StartsWith(".global "))
            {
                // 全局标签，不需要处理
                return;
            }

            // ── 批量数据：`.word[1024] [默认值]`（v0.96.177）────────────────────
            //
            // ## 为什么要有它
            //
            // `int a[1024];` 原来生成 **1024 行 `.word 0`**（实测一个空 main 的 .vml 有 1071 行），
            // 而数据在**内存里本来就是压缩的**（编译器放的就是 `int[1024]`）——
            // 只有"落成文本"这一步把它摊平了。文本是给人看、给 diff 的，摊平之后一个数组
            // 就把整份文件淹掉，`.vml` 的体积也跟着涨。
            //
            // ## 语法
            //
            //     buf: .word[1024] 0        ; 1024 个字，初值 0
            //     buf: .word[1024]:0        ; 冒号写法也收（`类型[数量]:默认值` 形态）
            //     buf: .word[1024]          ; 默认值省略 = 0
            //
            // 别名 `.int[N]` / `.long[N]` / `.dword[N]` 等价（都是 4 字节字）。
            // ⚠ 只做了 **4 字节字**：`.byte[N]`/`.half[N]` 没做 —— VMB 数据段对数组是按
            //   "每个元素一个带 tag 的槽"写的，直接塞 `byte[]` 会被写成 4 字节（静默错），
            //   要做得再多动一处编码。C 前端的 `int a[N]` 正好是 4 字节字，够用。
            // ⚠ **兼容性**：`.word[N]` 是新写法，**旧版汇编器读不懂**（会退化成把 `[1024] 0`
            //   当值解析、静默变 0）。回灌上游时这条要一起说清楚。
            if (TryParseCompactData(line, out var compactLabel, out var compactCount, out var compactValue))
            {
                FinalizeMultiWordData();
                var compactArr = new int[compactCount];
                for (var ci = 0; ci < compactCount; ci++) compactArr[ci] = compactValue;
                if (compactLabel.Length > 0)
                {
                    if (!labels.ContainsKey(compactLabel)) labels[compactLabel] = currentAddress;
                    dataSection[compactLabel] = compactArr;
                }
                else if (_lastLabel != null)
                {
                    if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                    dataSection[_lastLabel] = compactArr;
                    _lastLabel = null;
                }
                return;
            }

            // 处理 .word 指令
            if (line.Contains(".word"))
            {
                var parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    // Labeled: label: .word value
                    FinalizeMultiWordData();
                    var label = parts[0].Trim();
                    var valueStr = parts[1].Trim().Replace(".word", "").Trim();
                    var value = ParseValue(valueStr);
                    if (!labels.ContainsKey(label)) labels[label] = currentAddress;
                    dataSection[label] = value;
                }
                else if (_lastLabel != null)
                {
                    // Unlabeled .word after a label-only line: add to multi-word list
                    // (v1.66.37 fix: 不覆盖, 加入 _lastDataValues 累积)
                    var valueStr = line.Replace(".word", "").Trim();
                    var value = ParseValue(valueStr);
                    if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                    if (_lastDataValues != null)
                        _lastDataValues.Add(value);
                    else
                        dataSection[_lastLabel] = value;
                    _lastLabel = null;
                }
                else if (_lastDataValues != null)
                {
                    // Unlabeled .word: continuation of multi-word data
                    var valueStr = line.Replace(".word", "").Trim();
                    var value = ParseValue(valueStr);
                    if (value is int iv) _lastDataValues.Add(iv);
                    else if (value != null) _lastDataValues.Add(value);
                }
            }
            // 处理 .string 指令 (8-bit)
            else if (line.Contains(".string") && !line.Contains(".wstring") && !line.Contains(".ustring"))
            {
                FinalizeMultiWordData();
                string[] parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string label = parts[0].Trim();
                    string valueStr = parts[1].Trim().Replace(".string", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(label)) labels[label] = currentAddress;
                    dataSection[label] = value is string s ? new DataString(s, StringWidth.Byte) : value;
                }
                else if (_lastLabel != null)
                {
                    // Unlabeled .string after a label-only line: "label:\n    .string \"value\""
                    string valueStr = line.Replace(".string", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                    dataSection[_lastLabel] = value is string s ? new DataString(s, StringWidth.Byte) : value;
                    _lastLabel = null;
                }
            }

            // 处理 .wstring 指令 (16-bit, UTF-16LE)
            else if (line.Contains(".wstring"))
            {
                FinalizeMultiWordData();
                string[] parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string label = parts[0].Trim();
                    string valueStr = parts[1].Trim().Replace(".wstring", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(label)) labels[label] = currentAddress;
                    dataSection[label] = new DataString(value?.ToString() ?? "", StringWidth.Wide);
                }
                else if (_lastLabel != null)
                {
                    string valueStr = line.Replace(".wstring", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                    dataSection[_lastLabel] = new DataString(value?.ToString() ?? "", StringWidth.Wide);
                    _lastLabel = null;
                }
            }

            // 处理 .ustring 指令 (32-bit, UTF-32LE)
            else if (line.Contains(".ustring"))
            {
                FinalizeMultiWordData();
                string[] parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string label = parts[0].Trim();
                    string valueStr = parts[1].Trim().Replace(".ustring", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(label)) labels[label] = currentAddress;
                    dataSection[label] = new DataString(value?.ToString() ?? "", StringWidth.Unicode);
                }
                else if (_lastLabel != null)
                {
                    string valueStr = line.Replace(".ustring", "").Trim();
                    object value = ParseValue(valueStr);
                    if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                    dataSection[_lastLabel] = new DataString(value?.ToString() ?? "", StringWidth.Unicode);
                    _lastLabel = null;
                }
            }

            // 处理 .const 指令 (支持 label: .const value 和 .const label value 两种格式)
            else if (line.Contains(".const"))
            {
                string[] parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    // label: .const value 格式
                    string label = parts[0].Trim();
                    string valueStr = parts[1].Trim().Replace(".const", "").Trim();
                    object value = ParseValue(valueStr);
                    constants[label] = value;
                }
                else if (line.StartsWith(".const"))
                {
                    // .const label value 格式
                    string[] constParts = line.Substring(6).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (constParts.Length >= 2)
                    {
                        string label = constParts[0];
                        object value = ParseValue(constParts[1]);
                        constants[label] = value;
                    }
                }
            }

            // 处理 .data 指令 (支持 label: .data [name] value 和 .data [name] value 两种格式)
            //
            // ⚠ 这里必须是 `else if`（v0.96.177 修复）：它原来是新起的 `if`，**链就断在这里** ——
            //   下面 `.dword`/`.byte`/`.halfword`/`.word` 那几条 `else if` 于是挂到了本分支上，
            //   于是一行 `.word 7` 会被处理两遍（上面 `.word` 专用分支加一次、`HandleDataDirective`
            //   再加一次），**每个元素都翻倍**。对数组初值是静默错值：`int c[3] = {7,8,9}`
            //   编出来是 `7,7,8,8,9,9` ⇒ `c[1]` 读到 7、`c[2]` 读到 8。
            //   零初值数组只是白占一倍内存（棋盘类程序因此一直看着正常），有初值的才露馅。
            else if (line.Contains(".data"))
            {
                string[] parts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    // label: .data [name] value — 使用冒号前的标签名，值取最后一个token
                    string label = parts[0].Trim();
                    string rest = parts[1].Replace(".data", "").Trim();
                    string[] tokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0)
                    {
                        // 最后一个token是值 (如 buf: .data buf 128 → value=128)
                        object value = ParseValue(tokens[^1]);
                        dataSection[label] = value;
                    }
                    else
                    {
                        // label: .data (无值) — 标记为数据段的标签
                        FinalizeMultiWordData();
                        _lastDataLabel = label;
                        _lastDataValues = new List<object>();
                        // 保留 labels 条目: VMLRuntime.DataSection 初始化时覆盖
                    }
                }
                else if (line.StartsWith(".data"))
                {
                    // .data [name] value 语法 (无标签前缀)
                    string[] dpParts = line.Substring(5).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (dpParts.Length >= 2)
                    {
                        string label = dpParts[0];
                        object value = ParseValue(dpParts[1]);
                        dataSection[label] = value;
                    }
                }
            }

            // .dword 指令 (v1.66.63: 添加标签支持，修复 double 字面量数据丢失)
            else if (line.Contains(".dword"))
            {
                HandleDataDirective(line, ".dword");
            }
            // .byte/.halfword/.hword/.word (遗留: .word 有自己独立的处理器)
            else if (line.StartsWith(".byte") || line.StartsWith(".halfword") || line.StartsWith(".half") || line.StartsWith(".hword") || line.StartsWith(".word"))
            {
                HandleDataDirective(line, null);
            }
        }

        /// <summary>
        /// 批量数据行 `.word[数量] [默认值]`（别名 `.int`/`.long`/`.dword`）→ 数量与默认值。
        ///
        /// 只认"整行就是这一条"的形态（前面可带 `label:`）。返回 false 表示不是这种写法，
        /// 交回原来逐元素的路径 —— **不能猜**，猜错会把普通 `.word 5` 解析成别的东西。
        ///
        /// 两处易错：① `label: .word[N]` 与 `.word[N]:V` 都有冒号，**别把后者那个当 label
        /// 分隔符**（判据是"冒号在方括号之前才算 label"）；② 数量要钳一下，
        /// `[999999999]` 会在这一行直接分配 4GB。
        /// </summary>
        private static bool TryParseCompactData(string line, out string label, out int count, out int value)
        {
            label = "";
            count = 0;
            value = 0;

            var s = (line ?? "").Trim();
            var bracket = s.IndexOf('[');
            if (bracket < 0) return false;
            var colon = s.IndexOf(':');
            if (colon >= 0 && colon < bracket)          // 冒号在 `[` 之前才是 label 分隔符
            {
                label = s[..colon].Trim();
                s = s[(colon + 1)..].Trim();
            }

            var m = System.Text.RegularExpressions.Regex.Match(s,
                @"^\.?(?:word|int|long|dword)\s*\[\s*(\d+)\s*\]\s*:?\s*(-?[0-9A-Fa-fxX]*)\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return false;

            if (!int.TryParse(m.Groups[1].Value, out count) || count <= 0) return false;
            if (count > 1 << 20) return false;          // 400 万字节的表已经不合理了，多半是写错

            var def = m.Groups[2].Value;
            if (def.Length > 0)
            {
                var parsed = ParseValueStatic(def);
                value = parsed is int iv ? iv : 0;
            }
            return true;
        }

        /// <summary>`ParseValue` 的静态版（批量数据在静态辅助里解析，拿不到实例）。</summary>
        private static object? ParseValueStatic(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return 0;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var hex))
                return (int)hex;
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec))
                return dec;
            if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var dbl))
                return (int)dbl;
            return 0;
        }

        private void FinalizeMultiWordData()
        {
            if (_lastDataLabel != null && _lastDataValues != null && _lastDataValues.Count > 0)
            {
                // Convert to int[] if all elements are ints (v1.66.37 fix)
                if (_lastDataValues.All(v => v is int))
                    dataSection[_lastDataLabel] = _lastDataValues.Select(v => (int)v).ToArray();
                else
                    dataSection[_lastDataLabel] = _lastDataValues.ToArray();
            }

            _lastDataLabel = null;
            _lastDataValues = null;
        }

        /// <summary>
        /// 统一处理 .dword / .byte / .halfword / .hword / .word 数据指令
        /// 支持 labeled (label: .dword value) 和 unlabeled (.dword value) 两种格式
        /// </summary>
        private void HandleDataDirective(string line, string? directive)
        {
            string? label = null;
            string valuePart;

            // 提取指令名 (如果未指定则从 line 中解析)
            if (directive == null)
            {
                // 从 line 开头提取指令名 (如 ".word", ".byte" 等)
                int spaceIdx = line.IndexOf(' ');
                directive = spaceIdx > 0 ? line.Substring(0, spaceIdx) : line;
            }

            // 处理 labeled 格式: label: .dword value
            string[] colonParts = line.Split(new[] { ':' }, 2, StringSplitOptions.None);
            if (colonParts.Length == 2)
            {
                FinalizeMultiWordData();
                label = colonParts[0].Trim();
                valuePart = colonParts[1].Trim();
                // 移除指令前缀
                if (valuePart.StartsWith(directive))
                    valuePart = valuePart.Substring(directive.Length).Trim();
            }
            else
            {
                valuePart = line.Trim();
                // 移除指令前缀
                if (valuePart.StartsWith(directive))
                    valuePart = valuePart.Substring(directive.Length).Trim();
            }

            // 解析值
            if (string.IsNullOrEmpty(valuePart))
                return;

            string[] valuesStr = valuePart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<object> values = [];
            foreach (string v in valuesStr)
            {
                values.Add(ParseValue(v));
            }

            object finalValue = values.Count == 1 ? values[0] : values;

            // .dword 必须是 64 位：int→long, float→double 保证运行时分配 8 字节
            if (directive == ".dword")
            {
                if (finalValue is int iv) finalValue = (long)iv;
                else if (finalValue is float fv) finalValue = (double)fv;
                else if (finalValue is List<object> list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] is int liv) list[i] = (long)liv;
                        else if (list[i] is float lfv) list[i] = (double)lfv;
                    }
                }
            }

            if (label != null)
            {
                // Labeled: 使用标签名作为 key
                if (!labels.ContainsKey(label)) labels[label] = currentAddress;
                dataSection[label] = finalValue;
            }
            else if (_lastDataLabel != null && _lastDataValues != null)
            {
                // 多字数据续行: 追加到 _lastDataValues
                foreach (var v in values)
                    _lastDataValues.Add(v);
            }
            else if (_lastLabel != null)
            {
                // 无标签指令 (在 label-only 行之后): 使用 _lastLabel
                if (!labels.ContainsKey(_lastLabel)) labels[_lastLabel] = currentAddress;
                dataSection[_lastLabel] = finalValue;
                _lastLabel = null;
            }
            else
            {
                // 完全无标签: 使用自动生成的 key (fallback)
                dataSection[$"_{dataSection.Count}"] = finalValue;
            }
        }

        /// <summary>
        /// 汇编程序
        /// </summary>
        /// <param name="source">源代码</param>
        /// <returns>汇编后的程序</returns>
        public VmlProgram Assemble(string source)
        {
            return AssembleWithIncludes(source, null);
        }

        public VmlProgram AssembleWithIncludes(string source, string? basePath)
        {
            return AssembleWithIncludes(source, basePath, null);
        }

        public VmlProgram AssembleWithIncludes(string source, string? basePath, IEnumerable<string>? defines)
        {
            Reset();
            if (defines != null) SetDefines(defines);
            ProcessVmlLines(source, basePath);
            // ⚠ 必须在 ProcessVmlLines **之后**：标签可以在使用点之后才定义，
            //   而「寄存器形 token 是寄存器还是标签」正是拿标签集裁定的。
            ResolveRegisterShapedNames();
            FinalizeMultiWordData();
            var prog = new VmlProgram(instructions, labels, dataSection, constants)
            {
                LinkedFiles = linkedFiles,
                Externs = externs
            };
            prog.EntryPoint = entryPoint;
            prog.StackTop = stackTop;
            prog.VectorTable = vectorTable;
            prog.CpuSpeed = cpuSpeed;
            prog.SkipRegions = skipRegions;
            foreach (var kvp in exports)
                prog.Exports[kvp.Key] = kvp.Value;
            return prog;
        }

        public void SetDefine(string name)
        {
            _defines.Add(name);
        }

        public void SetDefines(IEnumerable<string> names)
        {
            foreach (var n in names) _defines.Add(n);
        }

        private void ProcessVmlLines(string source, string? basePath)
        {
            string[] lines = source.Split(['\n'], StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].Trim();

                // 预处理指令 #ifdef / #ifndef / #else / #endif
                if (trimmedLine.StartsWith("#ifdef ", StringComparison.Ordinal))
                {
                    string define = trimmedLine.Substring(7).Trim();
                    i = ProcessConditionalBlock(lines, i, _defines.Contains(define), basePath);
                }
                else if (trimmedLine.StartsWith("#ifndef ", StringComparison.Ordinal))
                {
                    string define = trimmedLine.Substring(8).Trim();
                    i = ProcessConditionalBlock(lines, i, !_defines.Contains(define), basePath);
                }
                else
                {
                    i = ProcessSingleLine(lines, i, basePath);
                }
            }
        }

        /// <summary>
        /// 处理 #ifdef/#ifndef … #else … #endif 条件块
        /// </summary>
        /// <param name="lines">所有行</param>
        /// <param name="index">当前行（#ifdef/#ifndef 所在行）</param>
        /// <param name="active">初始条件块是否活跃</param>
        /// <param name="basePath">基础路径</param>
        /// <returns>匹配 #endif 所在行的 index</returns>
        private int ProcessConditionalBlock(string[] lines, int index, bool active, string? basePath)
        {
            int depth = 1;
            bool inActiveBlock = active;

            for (int i = index + 1; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].Trim();

                if (trimmedLine.StartsWith("#ifdef ", StringComparison.Ordinal))
                {
                    if (inActiveBlock)
                    {
                        string define = trimmedLine.Substring(7).Trim();
                        i = ProcessConditionalBlock(lines, i, _defines.Contains(define), basePath);
                    }
                    else
                    {
                        depth++;
                    }
                }
                else if (trimmedLine.StartsWith("#ifndef ", StringComparison.Ordinal))
                {
                    if (inActiveBlock)
                    {
                        string define = trimmedLine.Substring(8).Trim();
                        i = ProcessConditionalBlock(lines, i, !_defines.Contains(define), basePath);
                    }
                    else
                    {
                        depth++;
                    }
                }
                else if (trimmedLine == "#else" && depth == 1)
                {
                    inActiveBlock = !active; // 翻转活跃状态
                }
                else if (trimmedLine == "#endif")
                {
                    if (depth == 1)
                        return i;
                    depth--;
                }
                else if (inActiveBlock)
                {
                    i = ProcessSingleLine(lines, i, basePath);
                }
            }

            return lines.Length - 1;
        }

        private int ProcessSingleLine(string[] lines, int index, string? basePath)
        {
            string originalLine = lines[index];
            string trimmedLine = originalLine.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                return index;

            // ── 记住「最近一条 `; N:` 注释里的 N」────────────────────────────────
            // 前端把源码行号写成 `; 12: <那一行的原文>`（见 `VmlProgram.ToString`），
            // 汇编器要把它接回来，否则**汇编期/链接期报的错只有函数名、没有行列号**。
            //
            // ⚠ **必须在剥注释之前做**：这一段原先挂在函数后半程（`ParseLine` 调用处之前），
            //   而剥注释的早退就在下面几行 —— 一整行 `; 12: …` 剥完是空串、当场 `return index`，
            //   于是**那段捕获代码一次都执行不到**（实测：`.code` 里写 `; 12:` 之后，
            //   指令的 `SourceLine` 恒为 -1）。它读的 `trimmedLine` 那时也早已被剥空了。
            //
            // 只认 `^\s*;\s*(\d+):` 这一种形态：`; ----`、`; source : …`、`; 参数说明`
            // 这些都不匹配，不会被误当成行号。
            {
                var c = trimmedLine.StartsWith(";") ? trimmedLine[1..].TrimStart() : null;
                if (c != null)
                {
                    int ci = 0;
                    while (ci < c.Length && char.IsDigit(c[ci])) ci++;
                    if (ci > 0 && ci < c.Length && c[ci] == ':' &&
                        int.TryParse(c[..ci], out var srcLine))
                        _pendingSourceLine = srcLine;
                }
            }

            // v1.65.170+: 去除行内注释 — `;` / `//` 之后视为注释（引号内的不算）。
            // 判据统一走 `StripLineComment` —— 这里原先自带一份"引号感知"的实现，
            // 而 `ParseLine` / `ParseData` 那两份是裸 `IndexOf(";")` ⇒ **一字符串两结果**。
            trimmedLine = StripLineComment(trimmedLine);
            if (string.IsNullOrEmpty(trimmedLine))
                return index;

            // 处理多行 ASM：ASM 行有开引号但无闭引号，合并后续行
            if (trimmedLine.StartsWith("ASM", StringComparison.OrdinalIgnoreCase))
            {
                int firstQuote = trimmedLine.IndexOf('"');
                if (firstQuote >= 0)
                {
                    int lastQuote = trimmedLine.LastIndexOf('"');
                    if (lastQuote <= firstQuote) // 只有开引号，需跨行
                    {
                        for (int j = index + 1; j < lines.Length; j++)
                        {
                            var nextLine = lines[j].Trim();
                            lastQuote = nextLine.LastIndexOf('"');
                            if (lastQuote >= 0)
                            {
                                string afterContent = nextLine.Substring(0, lastQuote).Trim();
                                originalLine = originalLine.TrimEnd() + "\n" + afterContent;
                                trimmedLine = originalLine.Trim();
                                index = j;
                                break;
                            }

                            originalLine = originalLine.TrimEnd() + "\n" + nextLine;
                        }
                    }
                }
            }

            if (trimmedLine.StartsWith(".include", StringComparison.OrdinalIgnoreCase))
            {
                // .include 已弃用 — 转换为 .linked
                string file = trimmedLine.Substring(8).Trim().Trim('"', '\'');
                file = file.Replace('\\', '/');
                if (!string.IsNullOrEmpty(file) && !linkedFiles.Any(f => string.Equals(f, file, StringComparison.OrdinalIgnoreCase)))
                    linkedFiles.Add(file);
                return index;
            }

            if (trimmedLine.StartsWith(".linked", StringComparison.OrdinalIgnoreCase))
            {
                string file = trimmedLine.Substring(7).Trim().Trim('"', '\'');
                file = file.Replace('\\', '/');
                if (!string.IsNullOrEmpty(file) && !linkedFiles.Any(f => string.Equals(f, file, StringComparison.OrdinalIgnoreCase)))
                    linkedFiles.Add(file);
                return index;
            }

            if (trimmedLine.StartsWith(".extern", StringComparison.OrdinalIgnoreCase))
            {
                string label = trimmedLine.Substring(7).Trim().Trim('"', '\'');
                if (!string.IsNullOrEmpty(label))
                    externs.Add(label);
                return index;
            }

            if (trimmedLine.StartsWith(".if", StringComparison.OrdinalIgnoreCase) && !trimmedLine.StartsWith(".ifdef", StringComparison.OrdinalIgnoreCase) && !trimmedLine.StartsWith(".ifndef", StringComparison.OrdinalIgnoreCase))
                return ProcessConditionalBlock(lines, index, basePath);

            if (trimmedLine.StartsWith(".org", StringComparison.OrdinalIgnoreCase))
            {
                ProcessOrgDirective(trimmedLine);
                return index;
            }

            if (trimmedLine.StartsWith(".align", StringComparison.OrdinalIgnoreCase))
            {
                ProcessAlignDirective(trimmedLine);
                return index;
            }

            if (trimmedLine.StartsWith(".equ", StringComparison.OrdinalIgnoreCase) || trimmedLine.EndsWith(".equ"))
            {
                ProcessEquDirective(trimmedLine);
                return index;
            }

            if (trimmedLine.StartsWith(".macro", StringComparison.OrdinalIgnoreCase))
                return ProcessMacroDefinition(lines, index);

            if (IsMacroCall(trimmedLine))
            {
                ProcessMacroCall(trimmedLine);
                return index;
            }

            if (trimmedLine.StartsWith(".data") || trimmedLine.StartsWith(".text") || trimmedLine == ".global" || trimmedLine.StartsWith(".global "))
            {
                // Track section state
                if (trimmedLine.StartsWith(".data"))
                {
                    _inDataSection = true;
                    _lastLabel = null;
                }

                if (trimmedLine.StartsWith(".text")) _inDataSection = false;
                // Finalize pending multi-word data on section change
                FinalizeMultiWordData();
                ParseData(originalLine);
                return index;
            }

            if (trimmedLine.StartsWith(".entry") || trimmedLine.StartsWith(".stack") || trimmedLine.StartsWith(".vectors") || trimmedLine.StartsWith(".skip") || trimmedLine.StartsWith(".SPEED", StringComparison.OrdinalIgnoreCase) || trimmedLine.StartsWith(".export", StringComparison.OrdinalIgnoreCase))
            {
                ParsePseudoOp(originalLine);
                return index;
            }

            // ⚠ `.int[N]` / `.long[N]`（批量数据的别名）要**显式写在这儿**才走得到 `ParseData` ——
            //   这条调度只认下面这些子串，光在 `TryParseCompactData` 的正则里列出来是**够不到的**
            //   （别名等于死代码）。判据带上方括号（`.int[`）而不是 `.int`，免得把 `.interrupt`
            //   之类一并吞进来；也**不能用 `StartsWith`** —— 带标签的写法（`p: .int[3] 7`）
            //   开头是标签，实测就栽在这上面（报"未知指令：.INT[3]"）。
            if (trimmedLine.Contains(".word") || trimmedLine.Contains(".byte") || trimmedLine.Contains(".dword") || trimmedLine.Contains(".halfword") || trimmedLine.Contains(".hword") || trimmedLine.Contains(".string") || trimmedLine.Contains(".wstring") || trimmedLine.Contains(".ustring") || trimmedLine.Contains(".const") || trimmedLine.Contains(".data") || trimmedLine.Contains(".int[") || trimmedLine.Contains(".long["))
            {
                ParseData(originalLine);
                return index;
            }

            // Handle label-only line in data section: start of multi-word data
            // In text section, let ParseLine handle code labels
            if (_inDataSection && trimmedLine.EndsWith(":") && !trimmedLine.Contains(".word") && !trimmedLine.Contains(".halfword") && !trimmedLine.Contains(".hword") && !trimmedLine.Contains(".string") && !trimmedLine.Contains(".wstring") && !trimmedLine.Contains(".ustring"))
            {
                string labelText = trimmedLine.TrimEnd(':');
                // 支持多标签: "label1 label2:" → 全部指向同一地址
                var multiLabels = labelText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                FinalizeMultiWordData();
                foreach (var lbl in multiLabels)
                    labels[lbl] = currentAddress;
                _lastDataLabel = multiLabels.Length > 0 ? multiLabels[0] : labelText;
                _lastLabel = _lastDataLabel;  // 同步 _lastLabel, 确保 .string/.word 等指令能找到标签
                _lastDataValues = new List<object>();
                return index;
            }

            // Auto-detect exit from data section to code section
            if (_inDataSection)
            {
                FinalizeMultiWordData();
                _inDataSection = false;
            }

            // `; N:` 源码行号已在**函数开头**捕获（必须在剥注释之前 —— 见那处注释）。
            // 这里不再重复一遍：两处实现必然漂移，而其中一处还会因为 `trimmedLine` 已被剥空
            // 而恒不命中。

            var instr = ParseLine(trimmedLine);
            if (instr != null)
            {
                if (_pendingSourceLine > 0) instr.SourceLine = _pendingSourceLine;
                instructions.Add(instr);
            }

            return index;
        }

        private HashSet<string> _defines = new();

        /// <summary>
        /// 最近一条 `; N: …` 注释里的源码行号（-1 = 还不知道）。
        /// 见主解析循环里那段说明 —— 前端把行号写成注释，汇编器要把它接回来，
        /// 否则报错只能给名字、给不出行列号。
        /// </summary>
        private int _pendingSourceLine = -1;


        private bool IsMacroCall(string line)
        {
            // 检查是否是宏调用：宏名后面可能有参数
            string[] parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            string firstWord = parts[0];
            return macros.ContainsKey(firstWord);
        }

        /// <summary>
        /// 处理 .org ADDRESS 伪指令 — 设置当前位置
        /// </summary>
        private void ProcessOrgDirective(string line)
        {
            var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;
            int targetAddr = (int)ParseValue(parts[1]);
            int current = instructions.Count;
            if (targetAddr < current) return; // 不回溯
            int padding = targetAddr - current;
            for (int i = 0; i < padding; i++)
                instructions.Add(new Instruction(OpCode.NOP, new List<Operand>()));
        }

        /// <summary>
        /// 处理 .align N 伪指令 — 填充 NOP 到 N 字节对齐
        /// </summary>
        private void ProcessAlignDirective(string line)
        {
            var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;
            if (!int.TryParse(parts[1], out int align) || align < 1) return;

            int current = instructions.Count;
            int remainder = current % align;
            if (remainder == 0) return;

            int padding = align - remainder;
            for (int i = 0; i < padding; i++)
                instructions.Add(new Instruction(OpCode.NOP, new List<Operand>()));
        }

        /// <summary>
        /// 处理 .equ 伪指令 — 定义汇编期符号常量
        /// 语法: .equ NAME VALUE 或 NAME .equ VALUE
        /// </summary>
        private void ProcessEquDirective(string line)
        {
            string trimmed = line.Trim();
            // 格式1: .equ NAME VALUE
            if (trimmed.StartsWith(".equ", StringComparison.OrdinalIgnoreCase))
            {
                string rest = trimmed.Substring(4).Trim();
                int spaceIdx = rest.IndexOf(' ');
                if (spaceIdx < 0) return;
                string name = rest.Substring(0, spaceIdx).Trim();
                string valueStr = rest.Substring(spaceIdx + 1).Trim();
                int value = (int)ParseValue(valueStr);
                constants[name] = value;
            }
            // 格式2: NAME .equ VALUE
            else
            {
                int eqIdx = trimmed.IndexOf(".equ", StringComparison.OrdinalIgnoreCase);
                if (eqIdx < 0) return;
                string name = trimmed.Substring(0, eqIdx).Trim().TrimEnd(':');
                string valueStr = trimmed.Substring(eqIdx + 4).Trim();
                int value = (int)ParseValue(valueStr);
                constants[name] = value;
            }
        }

        /// <summary>
        /// 评估 .if/.elif 条件表达式
        /// 支持: SYMBOL (定义且非零), SYMBOL == value, SYMBOL != value
        /// </summary>
        private bool EvaluateCondition(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            expr = expr.Trim();

            // 检查 == 或 !=
            if (expr.Contains("=="))
            {
                var parts = expr.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string name = parts[0].Trim();
                    int expected = (int)ParseValue(parts[1].Trim());
                    if (constants.TryGetValue(name, out var cv))
                        return (int)cv == expected;
                    return false;
                }
            }
            if (expr.Contains("!="))
            {
                var parts = expr.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string name = parts[0].Trim();
                    int expected = (int)ParseValue(parts[1].Trim());
                    if (constants.TryGetValue(name, out var cv))
                        return (int)cv != expected;
                    return true; // 未定义视为 != 0
                }
            }

            // 否则: 符号已定义且非零
            if (constants.TryGetValue(expr, out var v))
                return (int)v != 0;
            // 也检查 labels
            if (labels.ContainsKey(expr))
                return true;
            return false;
        }

        /// <summary>
        /// 处理 .if/.elif/.else/.endif 条件汇编块
        /// 支持嵌套 — 用深度计数器跟踪
        /// </summary>
        private int ProcessConditionalBlock(string[] lines, int startIndex, string? basePath)
        {
            // 解析 .if 的条件表达式
            string ifLine = lines[startIndex].Trim();
            string condExpr = ifLine.Substring(3).Trim(); // 去掉 ".if"
            bool taking = EvaluateCondition(condExpr);
            int depth = 1; // 当前 .if 嵌套深度
            int lineIndex = startIndex + 1;

            while (lineIndex < lines.Length)
            {
                string line = lines[lineIndex].Trim();

                // 检测嵌套 .if (增加深度)
                if (line.StartsWith(".if", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith(".ifdef", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith(".ifndef", StringComparison.OrdinalIgnoreCase))
                {
                    depth++;
                    if (taking)
                    {
                        // 递归处理嵌套块
                        lineIndex = ProcessConditionalBlock(lines, lineIndex, basePath);
                        continue;
                    }
                }
                // .elif — 仅在当前深度 && 还没找到真分支时评估
                else if (line.StartsWith(".elif", StringComparison.OrdinalIgnoreCase) && depth == 1)
                {
                    if (!taking)
                    {
                        string elifExpr = line.Substring(5).Trim();
                        taking = EvaluateCondition(elifExpr);
                    }
                }
                // .else — 翻转
                else if (line.StartsWith(".else", StringComparison.OrdinalIgnoreCase) && depth == 1)
                {
                    if (!taking)
                        taking = true;
                    else
                        taking = false; // 已经执行过真分支，跳过 else
                }
                // .endif — 减少深度
                else if (line.StartsWith(".endif", StringComparison.OrdinalIgnoreCase))
                {
                    depth--;
                    if (depth == 0)
                        return lineIndex;
                }
                // 体内容 — 仅在 taking 时处理
                else if (taking)
                {
                    lineIndex = ProcessSingleLine(lines, lineIndex, basePath);
                }

                lineIndex++;
            }

            if (depth > 0)
                throw new ArgumentException(".if 缺少对应的 .endif");
            return lineIndex;
        }

        private int ProcessMacroDefinition(string[] lines, int startIndex)
        {
            // 解析 .macro 指令
            string macroLine = lines[startIndex].Trim();
            string[] parts = macroLine.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                throw new ArgumentException($"无效的 .macro 指令: {macroLine}");
            }

            string macroName = parts[1];
            var macro = new MacroDefinition(macroName);

            // 解析参数（如果有）
            if (parts.Length > 2)
            {
                for (int i = 2; i < parts.Length; i++)
                {
                    string param = parts[i].Trim();
                    if (!string.IsNullOrEmpty(param))
                    {
                        macro.Parameters.Add(param);
                    }
                }
            }

            // 收集宏体，直到遇到 .endm
            int lineIndex = startIndex + 1;
            while (lineIndex < lines.Length)
            {
                string line = lines[lineIndex].Trim();
                if (line.StartsWith(".endm", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                macro.BodyLines.Add(line);
                lineIndex++;
            }

            if (lineIndex >= lines.Length)
            {
                throw new ArgumentException($"宏 '{macroName}' 缺少 .endm 指令");
            }

            // 保存宏定义
            macros[macroName] = macro;
            return lineIndex; // 返回 .endm 所在的行索引
        }

        private void ProcessMacroCall(string line)
        {
            // 解析宏调用
            var parts = line.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
            string macroName = parts[0] ?? "";

            if (!macros.TryGetValue(macroName, out MacroDefinition macro))
            {
                throw new ArgumentException($"未定义的宏: {macroName}");
            }

            // 收集参数
            List<string> arguments = new List<string>();
            for (int i = 1; i < parts.Length; i++)
            {
                arguments.Add(parts[i].Trim());
            }

            // 检查参数数量
            if (arguments.Count != macro.Parameters.Count)
            {
                throw new ArgumentException($"宏 '{macroName}' 期望 {macro.Parameters.Count} 个参数，但提供了 {arguments.Count} 个");
            }

            // 展开宏：替换参数并处理宏体
            foreach (string bodyLine in macro.BodyLines)
            {
                string expandedLine = bodyLine;

                // 替换参数
                for (int i = 0; i < macro.Parameters.Count; i++)
                {
                    string param = macro.Parameters[i];
                    string arg = arguments[i];
                    expandedLine = expandedLine.Replace(param, arg);
                }

                // 处理展开后的行
                string trimmedExpandedLine = expandedLine.Trim();
                if (string.IsNullOrEmpty(trimmedExpandedLine))
                    continue;

                // 递归处理宏调用（支持嵌套宏）
                if (IsMacroCall(trimmedExpandedLine))
                {
                    ProcessMacroCall(trimmedExpandedLine);
                    continue;
                }

                // 解析指令和标签
                var instr = ParseLine(trimmedExpandedLine);
                if (instr != null)
                {
                    instructions.Add(instr);
                }
            }
        }

        public void Reset()
        {
            instructions = [];
            labels = new Dictionary<string, int>();
            dataSection = new Dictionary<string, object>();
            constants = new Dictionary<string, object>();
            macros = new Dictionary<string, MacroDefinition>();
            currentAddress = 0;
            _lastLabel = null;
            _lastDataLabel = null;
            _lastDataValues = null;
            _inDataSection = false;
            _pendingRegShaped.Clear();
            _defines.Clear();
            entryPoint = "main";
            stackTop = 1048576;
            vectorTable = 0;
            cpuSpeed = 0;
        }

        /// <summary>
        /// 汇编VML源代码并生成VMB二进制格式
        /// </summary>
        /// <param name="source">VML源代码</param>
        /// <returns>VMB二进制数据</returns>
        public byte[] AssembleToVmb(string source)
        {
            var program = Assemble(source);
            return program.ToVmbBytes();
        }

        /// <summary>
        /// 汇编VML源代码并保存为VMB文件
        /// </summary>
        /// <param name="source">VML源代码</param>
        /// <param name="outputPath">输出文件路径</param>
        public void AssembleToVmbFile(string source, string outputPath)
        {
            var program = Assemble(source);
            program.SaveToVmbFile(outputPath);
        }

        /// <summary>
        /// 从VMB文件加载并反汇编为VML程序
        /// </summary>
        /// <param name="vmbFilePath">VMB文件路径</param>
        /// <returns>VML程序对象</returns>
        public static VmlProgram LoadFromVmbFile(string vmbFilePath)
        {
            return VmlProgram.LoadFromVmbFile(vmbFilePath);
        }

        /// <summary>
        /// 从VMB二进制数据加载并反汇编为VML程序
        /// </summary>
        /// <param name="vmbData">VMB二进制数据</param>
        /// <returns>VML程序对象</returns>
        public static VmlProgram LoadFromVmbBytes(byte[] vmbData)
        {
            return VmlProgram.FromVmbBytes(vmbData);
        }
    }
}
