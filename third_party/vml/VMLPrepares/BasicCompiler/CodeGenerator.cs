using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace BasicCompiler
{
    /// <summary>
    /// BASIC 数据类型枚举
    /// </summary>
    public enum BasicType
    {
        Integer,
        Single,
        Double,
        String,
        Byte,
        Boolean,
        Long,
        Custom
    }

    public partial class CodeGenerator : TypedCodeGen<BasicType>
    {
        /// <summary>
        /// 源文件里出现过 `OPTION EXPLICIT` ⇒ **变量必须先声明**，未声明的引用报**错误**；
        /// 没出现则报**警告**（QBasic 默认的「未声明即隐式全局」是合法语义）。
        ///
        /// 由 <c>BasicCompiler</c> 从 `<c>Parser.OptionExplicit</c>` 传进来 ——
        /// 这是**每文件**的属性，不能做成 `ImplicitDeclarationAllowed` 那种编译期常量。
        /// </summary>
        public bool StrictDeclarations { get; set; }

        // 字符串缓冲区（data section 分配，链接器解析地址，非固定地址）
        public const string StringBufferLabel = "__strbuf";

        /// <summary>
        /// **空串的规范表示**（长度 0 的 data 常量）—— 全前端**只有这一个空串地址**。
        ///
        /// <para>
        /// 为什么必须"唯一"：本前端的字符串比较就是**指针比较**（`cmp`），所以
        /// 「两个空串相等」只有靠"它们是同一个地址"才成立。
        /// 从前有**三种**表示并存 —— 主路径给 `""` 各建一个 `str_data_N` 标签、
        /// SUB 路径把 `""` 编成**整数 0**、而 `INKEY$` 无键时又返回另一个地址 ——
        /// 于是 `WHILE INKEY$ <> "": WEND`（QBasic 清键盘缓冲的标准写法）里
        /// 三个空串两两不等，**循环永远出不去**。
        /// 实测最小复现（SUB 内）：`SUB SP() / WHILE INKEY$ <> "": WEND / PRINT "drained" / END SUB`
        /// —— 修前 `drained` 一个字都不打。
        /// </para>
        /// </summary>
        public const string EmptyStringLabel = "__empty_str";

        /// <summary>
        /// BASIC 名字 → VML 符号名的**唯一换算**（声明侧与调用侧共用）。
        ///
        /// <para>
        /// <b>为什么必须有它</b>：声明侧与调用侧对同一个函数名**做过两次不同的处理**，
        /// 于是 `FUNCTION g$(n)` 定出来的是 `func_g`、而 `PRINT g$(3)` 调的是 `func_g$`
        /// —— 链接期报「未定义的函数 'func_g$'」，而两处各自看都"没错"。
        /// </para>
        /// <para>
        /// 声明侧的那一次在<b>解析器</b>里（`Parser.Functions.cs` 的
        /// `ParseFunctionDeclaration`）：名字带 `$` 后缀时把 `$` 摘掉、同时置
        /// `IsStringFunction`。所以这里的换算对<b>已经摘过</b>的名字是幂等的，
        /// 两种来源（`funcDecl.Name` / 调用点的原始标识符文本）都能安全地过一遍。
        /// </para>
        /// <para>
        /// ⚠ `$` **摘掉**（解析器对函数名已经摘过一次，这里幂等），`% ! # &amp;`
        /// **转义成 `_pct`/`_sng`/`_dbl`/`_lng`**：不转义的话符号名里带着 `#`
        /// （`func_getnum#`），而 `VMLAssembler.IsValidLabel` 不认 `#` ⇒ 那个 CALL 被当成
        /// 立即数、运行期 `ExecuteCall` 抛 `InvalidCastException`（详见下面的实现说明）。
        /// 转义而不是删除，是为了让 `FUNCTION f` 与 `FUNCTION f%` 仍是两个符号。
        /// </para>
        /// <para>
        /// <b>它是 subMap/funcMap 的**唯一**键</b>（声明侧与查表侧都走它）——
        /// 从前声明侧写 `.ToLower()`、查表侧写 `SymbolKey()`，名字里带 `$`/`#` 时
        /// 两边不是同一个键 ⇒ 查不到声明 ⇒ **形参的 BYREF 判据失效**
        /// （调用方按值传、被调方按地址读，实测 `FUNCTION G#(a,b)` 恒返回 0）。
        /// </para>
        /// </summary>
        private static string BasicSymbol(string name)
        {
            string symbol = name.EndsWith("$", StringComparison.Ordinal) ? name.Substring(0, name.Length - 1) : name;

            /* ⚠ 名字里剩下的 `% ! # &` 必须**转义**，否则汇编层认不出它是标签。
               `VMLAssembler.IsValidLabel` 只接受「字母/数字/`_`/`$`」，于是
               `func_getnum#`（`FUNCTION GetNum#` 的符号名）走到"不是标签"那一支、
               被当成**立即数**收下（`Operand(IMMEDIATE, "func_getnum#")`），
               运行期 `ExecuteCall` 一 `(int)operand.Value` 就抛
               `InvalidCastException: Unable to cast 'System.String' to 'System.Int32'`
               —— 而且**只在真的调到那个函数时才炸**（实测 GORILLA.BAS：
               前面全跑得动，走到 `DoShot` 里的 `GetNum#(2, …)` 才崩）。

               映射成固定后缀而不是直接删掉：`FUNCTION f` 与 `FUNCTION f%` 在 BASIC 里
               是两个不同的函数，删掉后缀会让它们撞成同一个符号。 */
            return symbol
                .Replace("%", "_pct")
                .Replace("!", "_sng")
                .Replace("#", "_dbl")
                .Replace("&", "_lng");
        }

        /// <summary>`FUNCTION` 的符号名（`func_&lt;名&gt;`），见 <see cref="BasicSymbol"/>。</summary>
        private static string FunctionLabel(string name) => "func_" + BasicSymbol(name).ToLowerInvariant();

        /// <summary>`SUB` 的符号名（`sub_&lt;名&gt;`），见 <see cref="BasicSymbol"/>。</summary>
        private static string SubLabel(string name) => "sub_" + BasicSymbol(name).ToLowerInvariant();

        /// <summary>`SUB`/`FUNCTION` 声明表的查表键（小写、已按 <see cref="BasicSymbol"/> 归一）。</summary>
        private static string SymbolKey(string name) => BasicSymbol(name).ToLowerInvariant();

        /// <summary>
        /// **外部符号**（`NATIVE SUB` / `NATIVE FUNCTION`）的 `CALL` 目标名 —— 保留声明时的**大小写**。
        ///
        /// <para>
        /// BASIC 的关键字与自己的标识符**不分大小写**，但 <c>NATIVE</c> 声明的那个名字是
        /// **别人的符号**（多半是 C 写的外部接口 / 共享库函数），链接器按**逐字节**匹配 ——
        /// 大小写一改就找不到。此前这三处调用点一律走 <see cref="SymbolKey"/>（**全部小写**），
        /// 于是 `NATIVE FUNCTION MyCFunc (...)` 编出 `CALL mycfunc`，链接期报
        /// 「未定义的函数 'mycfunc'」—— 消息里的名字与源码里写的不是同一个，
        /// 查的人只能靠猜。
        /// </para>
        /// <para>
        /// ⚠ 取的是**声明处**的拼写（`funcDecl.Name`），不是调用处的：同一个外部函数
        /// 在程序里可能被写成 `MyCFunc` / `mycfunc` / `MYCFUNC`，**外部符号只有一个**，
        /// 定义它的那一处才是权威（与 `Lib/c/waycoder_ui.h` 里的拼写对齐）。
        /// 调用处找不到声明时（没写 `NATIVE FUNCTION` 那行）退回调用处的拼写 —— 那是
        /// 唯一能拿到的信息，比擅自改成小写好。
        /// </para>
        /// <para>
        /// `%`/`!`/`#`/`&` 的转义一并保留（<see cref="BasicSymbol"/>）：外部符号本来
        /// 不会有这些字符，但"转义规则只有一份"比"少调一次函数"值钱。
        /// </para>
        /// </summary>
        private static string NativeLabel(string declaredName) => BasicSymbol(declaredName);

        // 动态分配的静态数据区 — 程序启动时通过 SYSCALL #40 分配
        //
        // ⚠ **基址槽不再是固定地址**（v0.96.331）：原先它写在 `0x6FD4`，本次按用户定的规矩
        //   （**除汇编与 C 外不允许直接使用固定地址**）改成 `.data` 段里的 `Sys.StaticBase`
        //   （见 `CodeGenerator.Qbasic.SysVars.cs`）。读法一字未变，只是地址从立即数换成标签：
        //   `MOVE reg, <标签>` 取的**就是地址**，紧接着那句 `MOVE reg, [reg]` 照旧是取基址。
        //
        //   下面那六个 `STATIC_*_OFFSET` 是**相对基址的偏移**，不是绝对地址 —— 按规矩原样留着。
        // 布局: [0x0000:DATA] [0x1000:文件句柄] [0x2000:字符串缓冲] [0x3000:Palette]
        //       [0x3800:Palette13] [0x4000:Sound] [0x5000..:全局变量]
        public const int STATIC_DATA_OFFSET   = 0x0000; // DATA area
        public const int STATIC_FILE_OFFSET   = 0x1000; // File handles
        public const int STATIC_STRING_OFFSET = 0x2000; // String buffer
        public const int STATIC_PAL_OFFSET    = 0x3000; // EGA palette
        public const int STATIC_PAL13_OFFSET  = 0x3800; // VGA 256 palette
        public const int STATIC_SOUND_OFFSET  = 0x4000; // Sound buffer
        // 全局变量区：**追加在现有硬编码分区之后**（0x0000 DATA / 0x1000 文件句柄 / 0x3000 调色板
        // / 0x3800 VGA 调色板 / 0x4000 声音），不动它们，免得重新编号引入新错。
        public const int STATIC_GLOBALS_OFFSET = 0x5000;
        public const int STATIC_TOTAL_SIZE    = 0x7000; // 原 0x5000；尾部 8KB(≈2048 个 int) 给全局变量

        /// <summary>Emit code to load dynamic static base address into reg</summary>
        private void EmitStaticBase(int reg)
        {
            SysAddr(reg, Sys.StaticBase);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new(OperandType.REGISTER, reg), new(OperandType.MEMORY, $"R{reg}") }));
        }

        /// <summary>Emit code to compute addr = dynamic_base + offset into reg</summary>
        private void EmitStaticAddr(int reg, int offset)
        {
            EmitStaticBase(reg);
            if (offset != 0)
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> {
                    new(OperandType.REGISTER, reg), new(OperandType.IMMEDIATE, offset) }));
        }

        /// <summary>当前 BASIC 方言 (v1.66.32+)</summary>
        private VMLPlugins.BasicDialect CurrentDialect =>
            VMLPlugins.CompilerOptionsContext.Current.BasicDialect;
        private bool IsDialect(VMLPlugins.BasicDialect d) => CurrentDialect == d;

        /// <summary>输出未实现特性警告 — 编译通过但无实际功能 (v1.66.32+)</summary>
        private void WarnUnimplemented(string feature)
        {
            instructions.Add(new Instruction(OpCode.LABEL,
                [new Operand(OperandType.IMMEDIATE, 0)]) { Label = $"; 警告: {feature} — 本前端尚未实现，运行时没有任何效果" });
        }

        private BasicProgram program;
        private Dictionary<string, int> variables;
        private Dictionary<string, BasicType> variableTypes;
        private Dictionary<string, ArrayInfo> arrayVariables;

        /// <summary>
        /// 变量**分配时定下的字节数**（槽数 × 4）—— 地址计算只认这一份，之后永不改变。
        ///
        /// <para>
        /// <b>为什么必须有它</b>：`GetVarByteOffset` 原来是现算的
        /// （`GetVarByteSize(GetVariableType(名))`），而 `GetVariableType` 会被**后续的赋值**
        /// 改写（`Sub.cs` 的 `GenerateSubLetStatement`：`gravity# = VAL(grav$)` ⇒
        /// `variableTypes["gravity#"] = Single`）。于是同一个全局变量，在**主程序**里是
        /// 8 字节（`#` 后缀 ⇒ Double）、在**后面生成的 SUB** 里变成 4 字节
        /// ⇒ 它之后所有全局变量的字节偏移在两边**各差 4**，而主程序那份地址早已编进指令里了。
        /// </para>
        /// <para>
        /// 实测症状（GORILLA.BAS）：`Mode` 在主程序里读 `#21988`、在 `MakeCityScape` 里读
        /// `#21984`（= 主程序里 `ScrWidth` 的槽）⇒ `IF Mode = 9` 走进 else 分支，
        /// `BottomLine` 从 335 变成 190、`HtInc` 6 ⇒ 整座城市画到屏幕外/尺寸全错，
        /// **一个错都不报**。这类"同一份数据两处算法"正是本仓的头号坑。
        /// </para>
        /// </summary>
        private Dictionary<string, int> varByteSizes;
        private HashSet<string> _sharedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>模块级变量（含 DIM SHARED）—— 放静态区的全局段，主程序与 SUB 共用同一份内存。</summary>
        private HashSet<string> _globalVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 这个变量走**静态区的全局段**吗？
        ///
        /// <para>两个集合要**一起判**：主程序里的变量全部进 <see cref="_globalVars"/>
        /// （见 <c>GetOrCreateVariable</c>），而 `SUB` 体里的 `DIM SHARED` 只进
        /// <see cref="_sharedVariables"/> —— 但它要的同样是"主程序与 SUB 共用同一份内存"，
        /// 与全局变量是**同一件事**。此前那条路走 <see cref="VarMemRef"/> 的绝对数值寻址分支，
        /// 编出来的是 `MEMORY "&lt;固定地址+8+偏移&gt;"`：那个固定地址（<c>0x6FD4</c>）
        /// 随本次"去固定地址"改造被拿掉了，而且它**本来就指不到全局段**
        /// （全局段在 `基址 + 0x5000`，不是 `0x6FD4 + 8`）。收进来之后，"SHARED = 全局"只有一条路。
        /// </para>
        /// </summary>
        private bool IsGlobalVar(string name)
            => _globalVars.Contains(name) || _sharedVariables.Contains(name);

        /// <summary>
        /// 变量的内存引用字符串。
        ///
        /// <para>⚠ 全局变量**不走这里**（见 <see cref="IsGlobalVar"/> 与 <see cref="EmitLoadVar"/>）——
        /// 它们要用 <see cref="EmitStaticAddr"/> 现算"基址 + 全局段偏移"，而基址是运行期才知道的
        /// （存在 <c>Sys.StaticBase</c> 那个槽里），**编不出一个绝对的数值串**。
        /// 这里只剩 `R12` 相对寻址那一种形态。</para>
        /// </summary>
        private string VarMemRef(string varName)
        {
            int offset = GetVarByteOffset(varName);
            return $"R12+{8 + offset}";
        }

        /// <summary>
        /// 读变量到 <paramref name="reg"/>。**全局变量（模块级）走静态区的全局段**，用
        /// <see cref="EmitStaticAddr"/> 就地算出绝对地址再间接读 —— 于是主程序与 SUB 指向同一块内存；
        /// 其余（SUB 局部/参数）仍走 `R12+偏移` 的相对寻址。
        ///
        /// 为什么不"让某个寄存器长期存基址"：已确认 R7–R11 全被大量使用（33~123 处），
        /// **没有空闲寄存器**可以专用；所以就地在目标寄存器里算地址，只用一个寄存器。
        /// </summary>
        private void EmitLoadVar(int reg, string name)
        {
            if (IsGlobalVar(name))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                // ⚠ 读也要**按类型**（MOVEF/MOVED/MOVEL）—— 与 EmitStoreVar 的
                //   `GetStoreInstruction(GetVariableType(name))` 对称。固定发 MOVE 的话，
                //   浮点/64 位全局变量会写进 F 寄存器组、却用整数寄存器读回来（值进不了目标组）。
                instructions.Add(new Instruction(GetLoadInstruction(GetVariableType(name)), new List<Operand>
                {
                    new Operand(OperandType.REGISTER, reg), new Operand(OperandType.INDIRECT, reg)
                }));
                return;
            }
            instructions.Add(new Instruction(GetLoadInstruction(GetVariableType(name)), new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, VarMemRef(name))
            }));
        }

        /// <summary>
        /// 把 <paramref name="srcReg"/> 写进变量。全局变量同样写进静态区的全局段。
        ///
        /// 地址临时用 **R2**：调用点（Let 语句）把值放在 R0/R1（浮点用 R0、整数用 R1），
        /// R2 在那一带本来就是拿来算地址的临时寄存器（见 `Statements.cs` 里 `MOVE R2,[R14+off]` 那段）。
        /// </summary>
        private void EmitStoreVar(string name, int srcReg)
        {
            if (IsGlobalVar(name))
            {
                EmitStaticAddr(2, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                instructions.Add(new Instruction(GetStoreInstruction(GetVariableType(name)), new List<Operand>
                {
                    new Operand(OperandType.INDIRECT, 2), new Operand(OperandType.REGISTER, srcReg)
                }));
                return;
            }
            instructions.Add(new Instruction(GetStoreInstruction(GetVariableType(name)), new List<Operand>
            {
                new Operand(OperandType.MEMORY, VarMemRef(name)), new Operand(OperandType.REGISTER, srcReg)
            }));
        }

        /// <summary>
        /// 记录（TYPE）字段的地址 → <paramref name="reg"/>。全局记录走静态区全局段
        /// （基址 + 全局段偏移 + 字段偏移），与 <see cref="EmitLoadVar"/>/<see cref="EmitStoreVar"/> 同源；
        /// 其余（SUB 内以 STATIC 登记的名字）仍按 R12 相对。
        ///
        /// 为什么要有这个 helper：字段地址原先在**六处**各拼一遍 `MOVE reg,#8+索引*4+字段偏移; ADD reg,R12`
        /// —— ① 全局记录被写到主帧/子帧上，与已经改走全局段的记录整体读写**各写各的**
        /// （SUB 里 `p.Y = 42`、主程序 `PRINT p.Y` 读回 0）；② 用的是"索引×4"，而同一变量的整体
        /// 寻址走 `GetVarByteOffset`（Double/Long 算 8 字节）—— 同一件事两处算法，有 8 字节类型就漂。
        /// 现在两处算法只有一份。
        /// </summary>
        private void EmitRecordFieldAddr(int reg, string recordName, int fieldOffset)
        {
            if (_globalVars.Contains(recordName))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(recordName) + fieldOffset);
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + variables[recordName] * 4 + fieldOffset)
            }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12)
            }));
        }

        /// <summary>
        /// 取变量本身的**地址**（不是值）→ <paramref name="reg"/>。用于 BYREF 实参。
        /// 全局变量必须给静态区全局段的地址 —— 给 `R12+8+偏移`（主帧）等于把一个跟变量无关的
        /// 栈地址传进去，被调方按地址读写的是主帧，与变量的实际位置无关。
        /// </summary>
        private void EmitVarAddr(int reg, string name)
        {
            if (_globalVars.Contains(name))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + GetVarByteOffset(name))
            }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12)
            }));
        }

        /// <summary>获取变量类型对应的字节偏移步长 — 委托到 TypedCodeGen.GetTypeInfo</summary>
        private int GetVarByteSize(BasicType t) => GetTypeInfo(t).byteSize;

        /// <summary>根据变量索引计算实际字节偏移 (考虑 Double/Long 的 8 字节宽度)</summary>
        /// <summary>
        /// `DIM arr(n) AS Type` 里**每个元素占几个 4 字节槽**（标量数组 = 1）。
        ///
        /// <para>与 <c>DIM x AS Type</c> 那处（`CodeGenerator.Sub.cs` 的 `DimAsStatement`
        /// 分支、`CodeGenerator.cs` 的同名分支）**同一口径**：用户自定义类型按
        /// `TotalSize` 向上取整到 4 的倍数 —— 元素槽与元素地址步长必须同源，
        /// 否则 UDT 数组的元素会互相重叠（实测 `b(0).XCoor` 与 `b(1).XCoor` 读到同一个值）。</para>
        /// </summary>
        private int ArrayElementSlots(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName) && typeDefinitions.TryGetValue(typeName.ToLower(), out var td))
                return Math.Max(1, (td.TotalSize + 3) / 4);
            return 1;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SUB / FUNCTION 的**栈上局部量**：槽位大小与地址
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>局部量**分配时定下的字节数**（4 或 8）—— 与 `varByteSizes` 同一口径。</summary>
        private Dictionary<string, int> localVarSizes;

        /// <summary>
        /// 登记一个局部量：记下它的**字节数**、推进帧内计数，返回它的槽序（4 字节为单位）。
        ///
        /// <para>
        /// <b>为什么局部量也要按字节数分配</b>：局部量的读写走
        /// <c>GetLoadInstruction/GetStoreInstruction(类型)</c> —— `Double`/`Long` 是
        /// **8 字节**的 `MOVED`/`MOVEL`。而槽位原来是"一个局部一个 4 字节槽"
        /// （地址 `R12-(slot+1)*4`、帧大小 `count*4`）⇒ 一个 `?` 局部写出 8 字节，
        /// 多出来的 4 字节**正好盖在帧头 [R12+0] 上**（`ENTER` 存进来的调用方 BP；
        /// [R12+4] 还存着返回地址）。本函数返回时 `LEAVE` 把那个被覆盖的值弹回 BP。
        /// </para>
        /// <para>
        /// 实测（GORILLA.BAS 的 `SUB Rest (t#)`，只有 `s#`/`t2#` 两个局部）：
        /// 反汇编是 <c>moved [@R12-4] @R1</c>（`s#` 落在第一个槽），写 8 字节 ⇒
        /// 帧头里躺下这个 double 的**高半字**；返回后 `LEAVE` 把 BP 恢复成
        /// `40000000`（2.0 的高半字），紧接着的 `MOVE @R0,[@R12+12]` 报
        /// 「内存越界：地址=4000000C」。
        /// 症状还会**随数据变**（跑出来是 1.0 的高半字 `3FF00000`，另一次是 2.0），
        /// 所以光看"地址是垃圾"很容易误判成"某个寄存器被踩了"。
        /// </para>
        /// <para>
        /// ⚠ **4 字节的局部地址一个字节都没变**（`slot*4 + 4` = 原来的 `(slot+1)*4`）——
        /// 不含 `#`/`&` 局部的程序生成结果**逐字节相同**，这条是刻意的：
        /// 免得为了修一个 double 局部去动全仓基本盘的地址布局。
        /// </para>
        /// </summary>
        private int DeclareLocal(string name)
        {
            name = name.ToLower();
            if (currentLocalVars.TryGetValue(name, out int existing)) return existing;

            int size = Math.Max(4, GetVarByteSize(GetVariableType(name)));
            currentLocalVars[name] = currentLocalVarCount;
            localVarSizes[name] = size;
            currentLocalVarCount += size / 4;      // 4 字节 = 1 槽，8 字节 = 2 槽
            return currentLocalVars[name];
        }

        /// <summary>局部量的**字节数**（分配时定下，之后不随类型改写而变）。</summary>
        private int LocalVarSize(string name)
            => localVarSizes.TryGetValue(name.ToLower(), out int s) ? s : 4;

        /// <summary>
        /// 局部量在帧内的**字节偏移**（负数，相对 R12）—— 全前端**唯一**的算法。
        ///
        /// <para>
        /// 局部区从 `R12` 往下长：第 i 个局部（`currentLocalVars[名]` = i，单位是 4 字节槽）
        /// 占 <c>[R12-(i*4+size), R12-i*4)</c>，`size` 是它自己的字节数。
        /// 于是 8 字节的局部**正好压在两个槽里**、不会碰到帧头。
        /// </para>
        /// <para>
        /// ⚠ 别再在别处现算 `-(slot+1)*4` —— 那正是本仓的头号坑
        /// （同一规则两处实现，有 8 字节类型时就漂）。本文件下面的
        /// <see cref="LocalVarMemRef"/> 与各调用点全部走这一个函数。
        /// </para>
        /// </summary>
        private int LocalVarOffset(string name)
        {
            name = name.ToLower();
            int units = currentLocalVars.TryGetValue(name, out int u) ? u : 0;
            return -(units * 4 + LocalVarSize(name));
        }

        /// <summary>局部量的内存引用串（<c>R12-&lt;n&gt;</c>）—— 与 <see cref="LocalVarOffset"/> 同源。</summary>
        private string LocalVarMemRef(string name) => $"R12-{-LocalVarOffset(name)}";

        /// <summary>一次新的 SUB/FUNCTION 生成开始：清空局部量表（地址与大小**必须一起清**）。</summary>
        private void ResetLocalVars()
        {
            currentLocalVars = new Dictionary<string, int>();
            localVarSizes = new Dictionary<string, int>();
            currentLocalVarCount = 0;
        }

        private int GetVarByteOffset(string varName)
        {
            int idx = variables[varName];
            int offset = 0;
            // 按索引顺序累加每个变量的字节宽度。
            //
            // ⚠ **用分配时记下的那个字节数**（`varByteSizes`），不要现算
            //   `GetVarByteSize(GetVariableType(名))` —— 类型会被后续赋值改写，
            //   而这是"算地址"的路（主程序早把地址编进指令里了），改了就是
            //   "同一个全局变量在主程序和 SUB 里地址不同"，见 `varByteSizes` 的说明。
            foreach (var kv in variables)
            {
                if (kv.Value < idx)
                {
                    offset += varByteSizes.TryGetValue(kv.Key, out var size)
                        ? size
                        : GetVarByteSize(GetVariableType(kv.Key));
                }
            }
            return offset;
        }

        private int variableCount;
        private Dictionary<int, int> lineLabels;
        
        // SUB/FUNCTION support
        private List<SubDeclaration> subDeclarations;
        private List<FunctionDeclaration> funcDeclarations;
        private Dictionary<string, SubDeclaration> subMap;
        private Dictionary<string, FunctionDeclaration> funcMap;
        private string currentSubName;
        private string currentClassName; // Track which class's method we're generating (Phase 2)
        private HashSet<string> methodSubs = new HashSet<string>(); // SUB names synthesized from methods
        // GPIO 库是否已链接 (v1.66.32+), reserved for future use
#pragma warning disable CS0414
        private bool _gpioLinked = false;
#pragma warning restore CS0414
        private Dictionary<string, int> currentLocalVars;
        private int currentLocalVarCount;
        private int currentParamCount;
        
        // 数据值列表 (用于 DATA/READ/RESTORE)
        private List<int> dataValues = new List<int>();
        private List<bool> dataIsString = new List<bool>();   // true if DATA value is string
        private List<string> dataStringLabels = new List<string>(); // string labels for string DATA
        
        // TYPE 定义元数据
        private Dictionary<string, TypeDeclaration> typeDefinitions = new Dictionary<string, TypeDeclaration>();
        // CLASS 定义元数据 (Phase 2)
        private Dictionary<string, ClassDeclaration> classDefinitions = new Dictionary<string, ClassDeclaration>();
        // DIM AS 变量映射: varName -> typeName
        private Dictionary<string, string> dimAsVariables = new Dictionary<string, string>();

        // DEF type ranges: DEFSNG/DEFINT/DEFSTR 字母范围 → 目标类型
        private List<(char Start, char End, BasicType Type)> _defTypeRanges = new List<(char, char, BasicType)>();

        // BasicTypeToExpType → 使用 TypedCodeGen.GetExpType(t) (自动从 GetTypeInfo 4-tuple 推导)

        private class ArrayInfo
        {
            public int Size { get; set; }
            public int Offset { get; set; }
            public List<int> Dimensions { get; set; }

            /// <summary>每一维的**下界**（`DIM a(1 TO 2)` → `[1]`；`DIM a(10)` → `[0]`）。
            /// 下标 → 槽位的换算要用它：`下标 - 下界` 才是槽位号。</summary>
            public List<int> LowerBounds { get; set; } = new List<int>();

            /// <summary>
            /// **每个元素的字节数**（标量数组 = 4）。用户自定义类型数组是整个记录的
            /// `TotalSize` 向上取整到 4 —— 元素地址的步长必须与元素槽的分配一致，
            /// 否则 `BCoor(1).XCoor` 会落在 `BCoor(0).YCoor` 上（实测两个元素读出同一对数）。
            /// </summary>
            public int ElementBytes { get; set; } = 4;
        }

        public CodeGenerator(BasicProgram program)
        {
            this.program = program;
            InitSimpleCompiler(newLabel: () => NewLabel(), placeLabel: lbl => { });
            Regs = new RegisterManager();
            variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            variableTypes = new Dictionary<string, BasicType>();
            // ⚠ **大小写不敏感**：BASIC 里 `BCoor` 与 `bcoord` 是同一个数组。
            //   原来是默认的区分大小写比较器，于是 DIM 写成 `B(0 TO 3)`、用的时候写 `b(i)`
            //   就"找不到这个数组"（静默 0 / 不生成代码）；`variables` 那张表**本来就是
            //   不敏感的**，两张表口径不同只会制造"有时对有时不对"。
            arrayVariables = new Dictionary<string, ArrayInfo>(StringComparer.OrdinalIgnoreCase);
            varByteSizes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            variableCount = 0;

            lineLabels = new Dictionary<int, int>();
            
            subDeclarations = new List<SubDeclaration>();
            funcDeclarations = new List<FunctionDeclaration>();
            subMap = new Dictionary<string, SubDeclaration>();
            funcMap = new Dictionary<string, FunctionDeclaration>();
            currentSubName = null;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = 0;
        }

        /// <summary>
        /// `DIM x AS &lt;类型&gt;` 的**类型登记** —— 主程序与 SUB 两处生成代码**共用这一份**。
        ///
        /// 只把类型记进 `dimAsVariables` 是不够的：那张表只用来算**记录字段偏移**，
        /// 而"这个变量是整数还是字符串"由 `GetVariableType` 决定，它只看 `$/%/!/#/&amp;` 后缀
        /// 与 `DEFtype` 字母范围。于是 `DIM s AS STRING` 之后：
        ///   · `s = "hello"` 把**字符串指针**存进一个整数槽（能存下，没人拦）；
        ///   · `PRINT s` 按整数把它打出来 —— 实测 `1024`（一个地址）。
        /// 声明里白纸黑字写着 STRING 却被当成整数，是最容易让人怀疑"字符串坏了"的那种形态。
        /// </summary>
        private void RegisterDimAsType(string varName, string typeName)
        {
            if (string.Equals(typeName, "string", StringComparison.OrdinalIgnoreCase))
                variableTypes[varName] = BasicType.String;
        }

        private int GetOrCreateVariable(string name)
        {
            if (!variables.ContainsKey(name))
            {
                // 模块级表里没有，且 SUB 的局部/形参表里也没有 ⇒ 这个名字**从未声明过**。
                //
                // 这是全前端**唯一**「没见过就造一个」的出口 —— 报错/警告的判据收在这一处，
                // 而不是散在四五个调用点（`Expressions.cs` 有一条是**无条件**调用的）。
                //
                // QBasic 的默认语义就是「未声明即隐式全局、值 0」⇒ **默认只警告**；
                // 写了 `OPTION EXPLICIT` 才升级成错误。此前无论写没写都静默建槽，那条指令形同虚设。
                if (currentLocalVars == null || !currentLocalVars.ContainsKey(name))
                {
                    if (StrictDeclarations)
                        ReportUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                    else
                        WarnUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量",
                            "QBasic 默认「未声明即隐式全局」(值为 0)；"
                            + "要让这类引用直接报错，请在程序开头写 `OPTION EXPLICIT`。");
                }

                variables[name] = variableCount;
                // **模块级创建的变量就是全局变量** —— 放静态区全局段。SUB 的局部/参数在
                // currentLocalVars 里、本来就不进 variables，所以不受影响。
                if (currentSubName == null) _globalVars.Add(name);
                // Double 和 Long 类型需要 8 字节，分配 2 个槽位
                BasicType varType = GetVariableType(name);
                int slots = (varType == BasicType.Double || varType == BasicType.Long) ? 2 : 1;
                variableCount += slots;
                // **字节数在这里定下**（= 槽数 × 4），之后 `GetVarByteOffset` 只认它。
                // 注意是"槽数×4"而不是 `GetVarByteSize(类型)`：分配永远是按 4 字节槽走的
                // （`Byte`/`Boolean` 也占一格），用类型算出来的 1 字节会让后面的变量
                // 与它重叠 3 个字节。
                varByteSizes[name] = slots * 4;
            }
            return variables[name];
        }

        public override VmlProgram GenerateCode()
        {
            // Built-in QBasic constants
            constants["true"] = -1;
            constants["false"] = 0;
            // ENUM values from dialect (v1.66.32+)
            foreach (var ev in program.EnumValues)
                constants[ev.Key.ToLower()] = ev.Value;
            
            // Load array types from parser's first pass
            foreach (var kv in program.ArrayTypes)
            {
                dimAsVariables[kv.Key.ToLower()] = kv.Value.ToLower();
            }
            // 第一遍：收集变量和行号标签
            CollectVariablesAndLabels();

            // 第二遍：生成代码
            GenerateInstructions();

            // 创建标签映射（从 LABEL 指令扫描 + StatementManager 写入的标签合并）
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Opcode == OpCode.LABEL && instructions[i].Operands.Count > 0)
                {
                    string labelName = instructions[i].Operands[0].Value.ToString().ToLower();
                    labels[labelName] = i;
                }
            }

            // 创建数据段（字符串缓冲区由链接器解析，无固定地址）
            this.dataSection[StringBufferLabel] = 0;
            // **空串的规范表示**（见 EmptyStringLabel）：所有 `""` 与 `INKEY$` 无键
            // 都指向这一个地址 —— 指针比较才判得出"两个空串相等"。
            this.dataSection[EmptyStringLabel] = new DataString("");

            // 创建数据段（合并类字段中的字符串数据）
            foreach (var variable in variables)
            {
                if (!this.dataSection.ContainsKey(variable.Key))
                    this.dataSection[variable.Key] = 0;
            }

            // 创建 VML 程序
            return BuildProgram("main");
        }

        private void CollectVariablesAndLabels()
        {
            // 先处理 DEF type 语句 (DEFINT/DEFLNG/DEFDBL 等)，以便后续变量分配时知道正确类型
            foreach (var statement in program.Statements)
            {
                if (statement is DefTypeStatement defType)
                {
                    BasicType targetType = defType.TypeName switch
                    {
                        "DEFSNG" => BasicType.Single,
                        "DEFDBL" => BasicType.Double,
                        "DEFLNG" => BasicType.Long,
                        "DEFSTR" => BasicType.String,
                        "DEFINT" => BasicType.Integer,
                        _ => BasicType.Integer
                    };
                    foreach (var (start, end) in defType.Ranges)
                        _defTypeRanges.Add((start, end, targetType));
                }
            }

            // Collect SUB/FUNCTION/DEF FN declarations
            foreach (var statement in program.Statements)
            {
                if (statement is SubDeclaration subDecl)
                {
                    subDeclarations.Add(subDecl);
                    subMap[SymbolKey(subDecl.Name)] = subDecl;
                }
                else if (statement is FunctionDeclaration funcDecl)
                {
                    funcDeclarations.Add(funcDecl);
                    funcMap[SymbolKey(funcDecl.Name)] = funcDecl;
                }
                else if (statement is DefFnStatement defFn)
                {
                    // Convert DEF FN to FUNCTION internally
                    string defFnName = defFn.FullFnName;
                    var defFuncDecl = new FunctionDeclaration(defFn.Line, defFn.Column, defFnName);
                    foreach (var p in defFn.Parameters)
                    {
                        defFuncDecl.Parameters.Add(new ParameterNode(p.Line, p.Column, p.Name, false, false));
                    }
                    // Body: LET funcName = expression
                    var defLetStmt = new LetStatement(defFn.Line, defFn.Column);
                    defLetStmt.Variable = new Identifier(defFn.Line, defFn.Column, defFnName);
                    defLetStmt.Expression = defFn.BodyExpression;
                    defFuncDecl.Body.Add(defLetStmt);
                    defFuncDecl.IsStringFunction = false;

                    funcDeclarations.Add(defFuncDecl);
                    funcMap[SymbolKey(defFnName)] = defFuncDecl;
                }
            }
            
            // Collect TYPE definitions (including those inside SUB/FUNCTION bodies)
            foreach (var statement in program.Statements)
            {
                CollectTypeDeclarations(statement);
            }

            /* **NATIVE 声明的形参强制 BYVAL。**
               形参默认改成 BYREF（QBasic 语义）之后必须同时钉住这一条：NATIVE SUB/FUNCTION
               没有函数体，实参是被 `Lib/**` 里那个 VML 包装函数按 **`[R12+12+4i]` 的值**读的
               （`ui_rect(x, y, w, …)` 就是这个约定）。传地址过去，包装函数会拿地址当坐标画。
               放在这里是因为 `IsNative` 是**解析完之后**才由 `Parser.Core` 回填的
               （`_pendingNative`），解析形参那一刻还不知道。 */
            foreach (var d in subMap.Values) if (d.IsNative) ForceByVal(d.Parameters);
            foreach (var d in funcMap.Values) if (d.IsNative) ForceByVal(d.Parameters);

            static void ForceByVal(List<ParameterNode> ps)
            {
                foreach (var p in ps) p.IsByRef = false;
            }

            // Phase 2: 将 CLASS 方法合成为 SUB 声明
            foreach (var kv in classDefinitions)
            {
                var classDecl = kv.Value;
                string clsName = classDecl.Name;
                foreach (var method in classDecl.Methods)
                {
                    string methodName = $"{clsName}_{method.Name}";
                    var subDecl = new SubDeclaration(method.Line, method.Column, methodName);
                    // THIS 指针作为第一个隐式参数
                    subDecl.Parameters.Add(new ParameterNode(method.Line, method.Column, "this", false, false));
                    // 显式参数
                    foreach (var pName in method.Parameters)
                        subDecl.Parameters.Add(new ParameterNode(method.Line, method.Column, pName, false, false));
                    subDecl.Body = method.Body;
                    subDeclarations.Add(subDecl);
                    subMap[SymbolKey(methodName)] = subDecl;
                    methodSubs.Add(methodName.ToLower());
                }
                // Constructor → SubDeclaration
                if (classDecl.ConstructorBody != null && classDecl.ConstructorBody.Count > 0)
                {
                    string ctorName = $"{clsName}_constructor";
                    var ctorSub = new SubDeclaration(classDecl.Line, classDecl.Column, ctorName);
                    ctorSub.Parameters.Add(new ParameterNode(classDecl.Line, classDecl.Column, "this", false, false));
                    ctorSub.Body = classDecl.ConstructorBody;
                    subDeclarations.Add(ctorSub);
                    subMap[SymbolKey(ctorName)] = ctorSub;
                    methodSubs.Add(ctorName.ToLower());
                }
                // Destructor → SubDeclaration (v1.66.32+)
                if (classDecl.DestructorBody != null && classDecl.DestructorBody.Count > 0)
                {
                    string dtorName = $"{clsName}_destructor";
                    var dtorSub = new SubDeclaration(classDecl.Line, classDecl.Column, dtorName);
                    dtorSub.Parameters.Add(new ParameterNode(classDecl.Line, classDecl.Column, "this", false, false));
                    dtorSub.Body = classDecl.DestructorBody;
                    subDeclarations.Add(dtorSub);
                    subMap[SymbolKey(dtorName)] = dtorSub;
                }
            }

            // Collect global variables
            foreach (var statement in program.Statements)
            {
                if (statement is SubDeclaration || statement is FunctionDeclaration)
                    continue;
                CollectVariablesFromStatement(statement);
            }
        }

        private void CollectTypeDeclarations(Statement statement)
        {
            if (statement is TypeDeclaration typeDecl)
            {
                typeDefinitions[typeDecl.Name.ToLower()] = typeDecl;
                int offset = 0;
                foreach (var field in typeDecl.Fields)
                {
                    field.Offset = offset;
                    switch (field.FieldType.ToUpper())
                    {
                        case "INTEGER": offset += 4; break;
                        case "SINGLE": offset += 4; break;
                        case "STRING": offset += (field.StringLength > 0 ? field.StringLength : 32); break;
                        default: offset += 4; break;
                    }
                }
                typeDecl.TotalSize = offset;
            }
            else if (statement is ClassDeclaration classDecl)
            {
                // CLASS → 注册为 TYPE (字段) + 存储方法列表 (v1.66.31+ Phase 2)
                var clsTypeDecl = new TypeDeclaration(classDecl.Line, classDecl.Column)
                    { Name = classDecl.Name, Fields = classDecl.Fields };
                int coff = 0;
                foreach (var f in classDecl.Fields)
                {
                    f.Offset = coff;
                    coff += f.FieldType.ToUpper() switch { "INTEGER" => 4, "SINGLE" => 4, _ => 4 };
                }
                clsTypeDecl.TotalSize = coff;
                typeDefinitions[classDecl.Name.ToLower()] = clsTypeDecl;
                classDefinitions[classDecl.Name.ToLower()] = classDecl;
            }
            else if (statement is SubDeclaration subDecl)
            {
                foreach (var bodyStmt in subDecl.Body)
                    CollectTypeDeclarations(bodyStmt);
            }
            else if (statement is FunctionDeclaration funcDecl)
            {
                foreach (var bodyStmt in funcDecl.Body)
                    CollectTypeDeclarations(bodyStmt);
            }
        }

        private void CollectVariablesFromStatement(Statement statement)
        {
            if (statement == null) return;
            // ⚠ **这一遍（第一遍：收集变量）也要给游标盖位置**。
            //
            // 未声明变量的报错**就发生在这一遍**（`GetOrCreateVariable` 是全前端唯一
            // 「没见过就造一个」的出口，报错判据收在那一处），而第二遍 `GenerateInstructions`
            // 里那句 `CurrentSourceLine = statement.Line` **根本还没执行到** —— 此前
            // `CurrentSourceLine` 停在初值 `-1` ⇒ `LocationString` 走降级分支，用户看到的是
            // `<input>: error: …`，**一个行号都没有**。修前实测：BASIC 是 22 门里唯一
            // 连行号都拿不到的（`DiagProbe` 的 NOPOS 档）。
            //
            // `Line > 0` 才盖：没填行号的节点是 0，置 0 会把上一句的正确行号冲成"无位置"。
            if (statement.Line > 0) { CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column; }
            if (statement is SequenceStatement seq)
            {
                foreach (var s in seq.Statements)
                    CollectVariablesFromStatement(s);
                return;
            }
            if (statement is LetStatement letStmt)
            {
                // 收集赋值目标中的变量
                if (letStmt.Variable is Identifier ident)
                {
                    if (!variables.ContainsKey(ident.Name))
                    {
                        GetOrCreateVariable(ident.Name);
                    }
                }
                else if (letStmt.Variable is ArrayAccessExpression arrayAccess)
                {
                    // 数组访问中的索引表达式可能包含变量
                    CollectVariablesFromExpression(arrayAccess.Index);
                }
                
                // 收集表达式中的变量
                CollectVariablesFromExpression(letStmt.Expression);
            }
            else if (statement is PrintStatement printStmt)
            {
                foreach (var expr in printStmt.Expressions)
                {
                    CollectVariablesFromExpression(expr);
                }
            }
            else if (statement is InputStatement inputStmt)
            {
                foreach (var var in inputStmt.Variables)
                {
                    if (!variables.ContainsKey(var.Name))
                    {
                        GetOrCreateVariable(var.Name);
                    }
                }
            }
            else if (statement is DimStatement dimStmt)
            {
                if (dimStmt.Dimensions.Count == 0 && dimStmt.Size == 1)
                {
                    // 简单变量声明: DIM SHARED varname / DIM varname
                    string varKey = dimStmt.VariableName;
                    if (!variables.ContainsKey(varKey))
                    {
                        GetOrCreateVariable(varKey);
                    }
                    if (dimStmt.IsShared)
                    {
                        _sharedVariables.Add(varKey);
                    }
                }
                else
                {
                    // 数组变量信息
                    if (!arrayVariables.ContainsKey(dimStmt.VariableName))
                    {
                        // 用户自定义类型数组：**每个元素占整个记录的字节数**，不是一个 4 字节槽。
                        // 判据与寻址必须同源：`GenerateArrayElementAddr` 的步长用的就是这个数。
                        int elemSlots = ArrayElementSlots(dimStmt.TypeName);
                        arrayVariables[dimStmt.VariableName] = new ArrayInfo
                        {
                            Size = dimStmt.Size,
                            Offset = variableCount,
                            ElementBytes = elemSlots * 4,
                            Dimensions = dimStmt.Dimensions.Count > 0 ? new List<int>(dimStmt.Dimensions) : new List<int> { dimStmt.Size },
                            LowerBounds = dimStmt.LowerBounds.Count > 0 ? new List<int>(dimStmt.LowerBounds) : new List<int> { 0 }
                        };

                        // 为数组元素分配变量槽位（UDT 元素 → 一个元素 = `elemSlots` 个槽，
                        // 紧跟在该元素后面，命名沿用 `DIM x AS T` 那处的 `_slot_N` 约定）。
                        for (int i = 0; i < dimStmt.Size; i++)
                        {
                            string elementName = $"{dimStmt.VariableName}({i})";
                            for (int s = 0; s < elemSlots; s++)
                            {
                                string slotName = s == 0 ? elementName : $"{elementName}_slot_{s}";
                                if (!variables.ContainsKey(slotName))
                                {
                                    GetOrCreateVariable(slotName);
                                }
                                // 元素槽的字节数也**定死**（`Byte` 等也占一格 4 字节），
                                // 否则 `GetVarByteOffset` 现算类型会与分配脱钩。
                                varByteSizes[slotName] = 4;
                            }
                        }
                    }
                }
                // If the array has a type (DIM arr(size) AS TypeName), register it for field access
                if (!string.IsNullOrEmpty(dimStmt.TypeName))
                {
                    dimAsVariables[dimStmt.VariableName.ToLower()] = dimStmt.TypeName.ToLower();
                }
            }
            else if (statement is ForStatement forStmt)
            {
                if (!variables.ContainsKey(forStmt.Variable.Name))
                {
                    GetOrCreateVariable(forStmt.Variable.Name);
                }
                CollectVariablesFromExpression(forStmt.InitialValue);
                CollectVariablesFromExpression(forStmt.EndValue);
                if (forStmt.StepValue != null)
                {
                    CollectVariablesFromExpression(forStmt.StepValue);
                }
                foreach (var bodyStmt in forStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
            }
            else if (statement is WhileStatement whileStmt)
            {
                CollectVariablesFromExpression(whileStmt.Condition);
                foreach (var bodyStmt in whileStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
            }
            else if (statement is CallStatement callStmt)
            {
                foreach (var arg in callStmt.Arguments)
                    CollectVariablesFromExpression(arg);
            }
            else if (statement is DoLoopStatement doLoopStmt)
            {
                foreach (var bodyStmt in doLoopStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
                if (doLoopStmt.HasCondition && doLoopStmt.Condition != null)
                {
                    CollectVariablesFromExpression(doLoopStmt.Condition);
                }
            }
            else if (statement is DataStatement dataStmt)
            {
                // Collect data values at compile time
                foreach (var val in dataStmt.Values)
                {
                    if (val is NumberLiteral num)
                    {
                        dataValues.Add((int)num.Value);
                        dataIsString.Add(false);
                    }
                    else if (val is StringLiteral str)
                    {
                        string lbl = $"_data_str_{dataStringLabels.Count}";
                        dataSection[lbl] = new DataString(str.Value);
                        dataValues.Add(0);
                        dataIsString.Add(true);
                        dataStringLabels.Add(lbl);
                    }
                    else
                    {
                        dataValues.Add(0);
                        dataIsString.Add(false);
                    }
                }
            }
            else if (statement is ReadStatement readStmt)
            {
                // Collect variables used in READ
                foreach (var target in readStmt.Variables)
                {
                    // 数组元素（`READ a(i)`）登记的是**数组名**，下标里的变量由
                    // `CollectVariablesFromExpression` 那一遍收（这里只保证数组名在表里）。
                    string readName = target switch
                    {
                        Identifier ri => ri.Name,
                        ArrayAccessExpression ra => ra.ArrayName,
                        _ => null!,
                    };
                    if (string.IsNullOrEmpty(readName)) continue;
                    if (!variables.ContainsKey(readName))
                    {
                        GetOrCreateVariable(readName);
                    }
                    if (target is ArrayAccessExpression rae)
                        foreach (var ix in rae.Indices)
                            CollectVariablesFromExpression(ix);
                }
            }
            else if (statement is ConstStatement constStmt)
            {
                // Store constant value at compile time
                if (constStmt.Value is NumberLiteral num)
                {
                    constants[constStmt.Name.ToLower()] = (int)num.Value;
                }
                else if (constStmt.Value is StringLiteral str)
                {
                    constants[constStmt.Name.ToLower()] = str.Value;
                }
            }
            else if (statement is IfStatement ifStmt)
            {
                CollectVariablesFromExpression(ifStmt.Condition);
                if (ifStmt.ThenBranch != null)
                {
                    CollectVariablesFromStatement(ifStmt.ThenBranch);
                }
                if (ifStmt.ElseBranch != null)
                {
                    CollectVariablesFromStatement(ifStmt.ElseBranch);
                }
            }
            else if (statement is DrawStatement drawStmt)
            {
                CollectVariablesFromExpression(drawStmt.DrawString);
            }
            else if (statement is PrintUsingStatement usingStmt)
            {
                CollectVariablesFromExpression(usingStmt.Format);
                foreach (var v in usingStmt.Values)
                    CollectVariablesFromExpression(v);
            }
            else if (statement is DimAsStatement dimAsStmt)
            {
                string varName = dimAsStmt.VariableName.ToLower();
                if (!variables.ContainsKey(varName))
                {
                    GetOrCreateVariable(varName);
                }
                dimAsVariables[varName] = dimAsStmt.TypeName.ToLower();
                RegisterDimAsType(varName, dimAsStmt.TypeName);
                // Allocate slots based on type size
                if (typeDefinitions.ContainsKey(dimAsStmt.TypeName.ToLower()))
                {
                    int slots = (typeDefinitions[dimAsStmt.TypeName.ToLower()].TotalSize + 3) / 4;
                    for (int i = 0; i < slots - 1; i++)
                    {
                        string slotName = $"{varName}_slot_{i + 1}";
                        if (!variables.ContainsKey(slotName))
                            GetOrCreateVariable(slotName);
                    }
                }
            }
            else if (statement is PlayStatement playStmt)
            {
                CollectVariablesFromExpression(playStmt.CommandString);
            }
            else if (statement is SoundStatement soundStmt)
            {
                CollectVariablesFromExpression(soundStmt.Frequency);
                CollectVariablesFromExpression(soundStmt.Duration);
            }
            else if (statement is RedimStatement redimStmt)
            {
                CollectVariablesFromExpression(redimStmt.NewSize);
            }
            else if (statement is DefFnStatement defFnStmt)
            {
                // 参数变量不需要提前收集, 它们是函数内的局部变量
                CollectVariablesFromExpression(defFnStmt.BodyExpression);
            }
            else if (statement is GetStatement getStmt)
            {
                // GET uses array - collect array variable
                if (!string.IsNullOrEmpty(getStmt.ArrayName))
                {
                    string arrName = getStmt.ArrayName.ToLower();
                    if (!arrayVariables.ContainsKey(arrName) && !variables.ContainsKey(arrName))
                    {
                        // Add as a scalar variable reference (the array will be separately declared)
                        GetOrCreateVariable(arrName);
                    }
                }
                if (getStmt.X1 != null)
                    CollectVariablesFromExpression(getStmt.X1);
                if (getStmt.Y1 != null)
                    CollectVariablesFromExpression(getStmt.Y1);
                if (getStmt.X2 != null)
                    CollectVariablesFromExpression(getStmt.X2);
                if (getStmt.Y2 != null)
                    CollectVariablesFromExpression(getStmt.Y2);
            }
            else if (statement is PutStatement putStmt)
            {
                // PUT uses array
                string arrName = putStmt.ArrayName.ToLower();
                if (!arrayVariables.ContainsKey(arrName) && !variables.ContainsKey(arrName))
                {
                    GetOrCreateVariable(arrName);
                }
                CollectVariablesFromExpression(putStmt.X);
                CollectVariablesFromExpression(putStmt.Y);
            }
            else if (statement is PaletteStatement palStmt)
            {
                CollectVariablesFromExpression(palStmt.ColorIndex);
                CollectVariablesFromExpression(palStmt.Red);
                CollectVariablesFromExpression(palStmt.Green);
                CollectVariablesFromExpression(palStmt.Blue);
            }
            // 行标签: LabelName: body — 递归收集 body 中的变量
            else if (statement is LabelStatement labelStmt)
            {
                if (labelStmt.Body != null)
                    CollectVariablesFromStatement(labelStmt.Body);
            }
            // QBasic LINE: (x1,y1)-(x2,y2), color
            else if (statement is QbLineStatement qbLine)
            {
                CollectVariablesFromExpression(qbLine.X1);
                CollectVariablesFromExpression(qbLine.Y1);
                CollectVariablesFromExpression(qbLine.X2);
                CollectVariablesFromExpression(qbLine.Y2);
                if (qbLine.Color != null)
                    CollectVariablesFromExpression(qbLine.Color);
            }
            // QBasic CIRCLE: (x,y), radius, color
            else if (statement is QbCircleStatement qbCircle)
            {
                CollectVariablesFromExpression(qbCircle.X);
                CollectVariablesFromExpression(qbCircle.Y);
                CollectVariablesFromExpression(qbCircle.Radius);
                if (qbCircle.Color != null)
                    CollectVariablesFromExpression(qbCircle.Color);
            }
            // QBasic PAINT: (x,y), color [,border]
            else if (statement is QbPaintStatement qbPaint)
            {
                CollectVariablesFromExpression(qbPaint.X);
                CollectVariablesFromExpression(qbPaint.Y);
                CollectVariablesFromExpression(qbPaint.Color);
                if (qbPaint.Border != null)
                    CollectVariablesFromExpression(qbPaint.Border);
            }
            // PSET (x,y), color
            else if (statement is PsetStatement pset)
            {
                CollectVariablesFromExpression(pset.X);
                CollectVariablesFromExpression(pset.Y);
                if (pset.Color != null)
                    CollectVariablesFromExpression(pset.Color);
            }
            // SCREEN mode
            else if (statement is ScreenStatement screen)
            {
                if (screen.Mode != null)
                    CollectVariablesFromExpression(screen.Mode);
            }
            // LOCATE row, col
            else if (statement is LocateStatement locate)
            {
                if (locate.Row != null)
                    CollectVariablesFromExpression(locate.Row);
                if (locate.Col != null)
                    CollectVariablesFromExpression(locate.Col);
            }
            // COLOR fg [,bg]
            else if (statement is QbColorStatement colorStmt)
            {
                CollectVariablesFromExpression(colorStmt.Foreground);
                if (colorStmt.Background != null)
                    CollectVariablesFromExpression(colorStmt.Background);
            }
            // WIDTH cols, rows
            else if (statement is QbWidthStatement widthStmt)
            {
                if (widthStmt.Cols != null)
                    CollectVariablesFromExpression(widthStmt.Cols);
                if (widthStmt.Rows != null)
                    CollectVariablesFromExpression(widthStmt.Rows);
            }
            // RANDOMIZE [seed]
            else if (statement is RandomizeStatement randomStmt)
            {
                if (randomStmt.Seed != null)
                    CollectVariablesFromExpression(randomStmt.Seed);
            }
            // Turbo Basic statements -- collect variables from declarations
            else if (statement is LocalDeclaration localDecl)
            {
                foreach (var varIdent in localDecl.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is StaticDeclaration staticDecl)
            {
                foreach (var varIdent in staticDecl.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is SharedStatement sharedStmt)
            {
                foreach (var varIdent in sharedStmt.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is CommonStatement commonStmt)
            {
                foreach (var varName in commonStmt.VariableNames)
                {
                    if (!variables.ContainsKey(varName))
                    {
                        GetOrCreateVariable(varName);
                    }
                }
            }
            else if (statement is OptionBaseStatement)
            {
                // OPTION BASE is just a flag, no variable allocation needed
            }
            else if (statement is SelectCaseStatement selectCase)
            {
                // Collect variables from SELECT CASE expression and each case branch
                CollectVariablesFromExpression(selectCase.TestExpression);
                foreach (var caseBlock in selectCase.CaseBlocks)
                {
                    foreach (var cond in caseBlock.Conditions)
                    {
                        if (cond.Value != null)
                            CollectVariablesFromExpression(cond.Value);
                        if (cond.FromValue != null)
                            CollectVariablesFromExpression(cond.FromValue);
                        if (cond.ToValue != null)
                            CollectVariablesFromExpression(cond.ToValue);
                        if (cond.CompareValue != null)
                            CollectVariablesFromExpression(cond.CompareValue);
                    }
                    foreach (var bodyStmt in caseBlock.Body)
                        CollectVariablesFromStatement(bodyStmt);
                }
                foreach (var elseStmt in selectCase.ElseBlock)
                    CollectVariablesFromStatement(elseStmt);
            }
        }

        private void CollectVariablesFromExpression(Expression expr)
        {
            if (expr == null) return;
            // 与 `CollectVariablesFromStatement` 同源：本遍也会报「未声明的变量」，
            // 游标必须跟着**当前正在看的这个表达式节点**走。放在递归入口处 ⇒
            // 越往里越精确 —— `PRINT nosuch` 报到 `nosuch` 那一列，而不是 `PRINT` 那一列。
            if (expr.Line > 0) { CurrentSourceLine = expr.Line; CurrentSourceColumn = expr.Column; }
            if (expr is Identifier ident)
            {
                // Skip compile-time constants
                if (constants.ContainsKey(ident.Name.ToLower()))
                    return;
                if (!variables.ContainsKey(ident.Name))
                {
                    GetOrCreateVariable(ident.Name);
                }
            }
            else if (expr is BinaryExpression binary)
            {
                CollectVariablesFromExpression(binary.Left);
                CollectVariablesFromExpression(binary.Right);
            }
            else if (expr is UnaryExpression unary)
            {
                CollectVariablesFromExpression(unary.Expression);
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                CollectVariablesFromExpression(arrayAccess.Index);
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                foreach (var arg in funcCall.Arguments)
                    CollectVariablesFromExpression(arg);
            }
            else if (expr is FieldAccessExpression fieldAccess)
            {
                string recName = fieldAccess.RecordName;
                if (string.IsNullOrEmpty(recName) && fieldAccess.RecordExpression != null)
                {
                    // Derive record name from expression (e.g., array access)
                    if (fieldAccess.RecordExpression is Identifier recId)
                        recName = recId.Name;
                    else if (fieldAccess.RecordExpression is ArrayAccessExpression arrExpr)
                        recName = arrExpr.ArrayName;
                    // Also collect variables from the record expression recursively
                    CollectVariablesFromExpression(fieldAccess.RecordExpression);
                }
                if (!string.IsNullOrEmpty(recName))
                {
                    string recKey = recName.ToLower();
                    if (!variables.ContainsKey(recKey))
                    {
                        GetOrCreateVariable(recKey);
                    }
                }
            }
            else if (expr is FnCallExpression fnCall)
            {
                foreach (var arg in fnCall.Arguments)
                    CollectVariablesFromExpression(arg);
            }
        }

        private void GenerateInstructions()
        {
            Regs.Reset();
            // 生成 main 函数
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, "main") }));

            // 函数序言
            EmitPrologue();
            // 为临时变量/溢出槽预分配栈空间 (R12-4 到 R12-256)
            // 避免与 CALL/GOSUB 压入的返回地址碰撞
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256) }));

            // 初始化寄存器
            AddRI(OpCode.MOVE, 0, 0);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0) }));

            // 生成变量初始化
            foreach (var variable in variables)
            {
                AddRI(OpCode.MOVE, 0, 0);
                BasicType varType = GetVariableType(variable.Key);
                OpCode storeOp = GetStoreInstruction(varType);
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + variable.Value * 4}"), new Operand(OperandType.REGISTER, 0) }));
            }

            // 动态分配静态数据区 (替代固定地址 StaticBase)
            AddRI(OpCode.MOVE, 0, STATIC_TOTAL_SIZE);
            EmitAlloc();

            // ⚠ **分配结果必须存进基址槽** —— `EmitAlloc`（SYSCALL 40）把地址放在 **R0**，
            //   而 `Sys.StaticBase` 那个槽此前**只有读、从来没有写**：`EmitStaticBase` 只是
            //   把它的值读进寄存器。少了这一句，基址**恒为 0** ⇒ 所有"全局变量"都被
            //   按**绝对偏移** 0x5000.. 访问，与别的内存撞车 ——
            //   实测症状：GORILLA.BAS 里 `x`/`y` 读出来是一串**全局槽地址**
            //   （0x5000 / 0x5004 / 0x5008…，见 `vmlcli --trace-draw`），
            //   于是楼画成黑色、精灵贴到 (20480, 20488) 这种坐标上。
            //   它是**老大难**：本仓此前那条"清全局区"的循环还会顺手把基址槽一起抹掉
            //   （见下面 zeroLoop 的注释），两件事叠加让这个槽长期是 0。
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new(OperandType.MEMORY, Sys.StaticBase),
                new(OperandType.REGISTER, 0) }));

            EmitStaticBase(1);

            // 堆缓冲（帧缓冲 / PAINT 泛洪栈）的运行期分配**要插在这个位置**
            // （见 HeapBufNoteAllocSite 的说明）—— 此刻还定不下来用不用得上，
            // 语句全部生成完由 HeapBufEmitAllocs 决定插哪几块。
            HeapBufNoteAllocSite();

            // 把**全局变量区清 0** —— BASIC 语义里"未初始化的变量是 0"，
            // 而这块是 EmitAlloc 出来的裸内存（不保证是零）。清的范围只有全局段，
            // DATA 区等由各自的初始化逻辑负责，不碰。
            {
                string zeroLoop = GenerateLabel();
                string zeroDone = GenerateLabel();
                // R1 = 静态基址 + STATIC_GLOBALS_OFFSET —— 用现成的 EmitStaticAddr。
                // ⚠ 此前这两行是 `MOVE R1,#0x5000` + `ADD R1,[R1]`：上一行刚被 EmitStaticBase 算进 R1 的
                //   基址被立刻冲掉，`[R1]` 读的是**它自己要清的那块内存** ⇒ R1 = 0x5000 + mem[0x5000]。
                //   后果两层：① 全局区（基址+0x5000）一个字节都没清，"未初始化的全局变量是 0" 不成立；
                //   ② 8KB 被清到 0x5000+该值 的随机绝对地址上，还会把基址槽 0x6FD4 一起抹掉。
                EmitStaticAddr(1, STATIC_GLOBALS_OFFSET);
                AddRI(OpCode.MOVE, 2, STATIC_TOTAL_SIZE - STATIC_GLOBALS_OFFSET);   // R2 = 剩余字节数
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, zeroLoop) }));
                AddRI(OpCode.CMP, 2, 0);
                instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, zeroDone) }));
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.INDIRECT, 1), new Operand(OperandType.REGISTER, 0) }));
                AddRI(OpCode.ADD, 1, 4);
                AddRI(OpCode.SUB, 2, 4);
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, zeroLoop) }));
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, zeroDone) }));
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

            // 初始化 DATA 指针 (offset 0 in static area)
            //
            // ⚠ **MOVE 是 dest-first**（v0.96.330 修）：`move [mem], reg` 是**存**、
            //   `move reg, [mem]` 是**取**。这条链上从前**四处**全写反了，方向整片是反的：
            //   数据指针没被写过、DATA 值也没被写进静态区 ⇒ `READ` 永远读回 0。
            //   （同一族的坑本仓记过：SUB 内 FOR 的初值/自增两处也是这么反的。）
            AddRI(OpCode.MOVE, 0, 0);
            SysAddr(1, Sys.DataPointer);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0) }));

            // 初始化 DATA 值到动态分配的静态区
            int strPtr = 0;
            for (int i = 0; i < dataValues.Count; i++)
            {
                if (i < dataIsString.Count && dataIsString[i] && strPtr < dataStringLabels.Count)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, dataStringLabels[strPtr++]) }));
                }
                else
                {
                    AddRI(OpCode.MOVE, 0, dataValues[i]);
                }
                EmitStaticAddr(1, STATIC_DATA_OFFSET + i * 4);
                // ⚠ 这里必须是**存**（dest-first，见上面那段说明）—— 从前写成 `move R0, [R1]`
                //   是取，于是 DATA 值一个都没落进静态区。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0) }));
            }

            // 生成语句
            if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                System.Console.Error.WriteLine($"program.Statements.Count={program.Statements.Count}");
            foreach (var statement in program.Statements)
            {
                if (statement == null) { continue; }
                if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                    System.Console.Error.WriteLine($"Processing: {statement.GetType().Name}, Line={statement.Line}");
                CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column;
                if (statement is SubDeclaration || statement is FunctionDeclaration
                    || statement is TypeDeclaration || statement is ClassDeclaration)
                {
                    continue; // Skip sub/function/type/class declarations in main program
                }
                
                // 为行号生成标签（GOTO/GOSUB目标）
                // 老式行号 BASIC 用 BasicLineNumber (如 "10 PRINT" 的 10)；无行号时回退到物理行号
                int labelLine = statement.BasicLineNumber > 0 ? statement.BasicLineNumber : statement.Line;
                if (labelLine > 0)
                {
                    instructions.Add(new Instruction(OpCode.LABEL,
                        new List<Operand> { new Operand(OperandType.LABEL, $"line_{labelLine}") },
                        instructions.Count));
                }
                // 为命名标签生成小写标签（GOSUB目标：ShowTitle: → label "showtitle"）
                if (statement is LabelStatement labelSt)
                {
                    instructions.Add(new Instruction(OpCode.LABEL,
                        new List<Operand> { new Operand(OperandType.LABEL, labelSt.Name.ToLower()) },
                        instructions.Count));
                }
                
                GenerateStatement(statement);
            }

            // 程序结束 - 加载第一个全局变量到 R0 作为退出码，然后退出
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R12+8") }));
            EmitExit();

            // Generate SUB procedures
            foreach (var subDecl in subDeclarations)
            {
                // Phase 2: 设置方法上下文，使字段访问通过 THIS 指针
                if (methodSubs.Contains(subDecl.Name.ToLower()))
                {
                    // 从方法名中提取类名: "ClassName_methodName" → "ClassName"
                    int lastUnderscore = subDecl.Name.LastIndexOf('_');
                    if (lastUnderscore > 0)
                    {
                        // 构造函数: "ClassName_constructor" → extract "ClassName"
                        string potentialClass = subDecl.Name.Substring(0, lastUnderscore);
                        if (classDefinitions.ContainsKey(potentialClass.ToLower()))
                            currentClassName = potentialClass;
                    }
                }
                GenerateSubDeclaration(subDecl);
                currentClassName = null;
            }

            // Generate FUNCTIONs
            foreach (var funcDecl in funcDeclarations)
            {
                GenerateFunctionDeclaration(funcDecl);
            }

            // UI 后端的文本子程序（PRINT/LOCATE 攒行 → ui_text）。
            // **必须在所有 SUB/FUNCTION 之后**：它们由 CALL 进入、只被前面的语句引用，
            // 放这里就不用管"SUB 里的 PRINT 也得能调到"这件事。
            UiTextEmitHelpers();

            // 堆缓冲：程序用过哪几块就补哪几段的运行期分配（插回序言里记下的那个位置）。
            HeapBufEmitAllocs();

            // vga_text_putchar 无操作存根（链接共享库时会被覆盖）
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, "vga_text_putchar") }));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }

    }
}
