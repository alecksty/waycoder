using VMLAssembler;

namespace BasicCompiler;

/// <summary>
/// **系统全局变量登记表** —— BASIC 前端所有"运行期状态"的**唯一真源**。
///
/// <para>## 为什么要有这个文件（用户 2026-09-24 定的规矩）</para>
///
/// <para><b>除了汇编与 C 之外，不允许直接使用固定地址。</b> BASIC 前端是 DOS 时代写的，
/// 到处把运行期状态写在 <c>0x6FE0</c>/<c>0x6FF0</c>/<c>0xA0000</c> 这类绝对地址上 ——
/// 那是**当年那台机器**的约定，本平台没有那块内存的任何语义：谁来清零、清了没有、
/// 别的程序有没有留下残留，全都不确定。**同一份产物在别的宿主上会读到别的程序留下的残值。**</para>
///
/// <para>换成 <c>.data</c> 段的槽位之后，**由链接器分配地址、由初值表装载初值** ——
/// 地址是链接期定的、初值是我们自己写下的，两件事都确定了。</para>
///
/// <para>## 换法不是新发明（照 Ui 路抄）</para>
///
/// <para>`CodeGenerator.Qbasic.UiGfx.cs` 那三个状态字节（<c>UiModeLabel</c> 等）与调色板表
/// （<c>UiPaletteLabel</c>）早就这么改了 —— 其中**调色板那次是真踩过坑**：第一版写死在
/// <c>0x9F000</c>，结果**整张表读出全 0**（每个颜色都是 <c>#FF000000</c>）。
/// 本文件只是把同一件事铺到其余全部地址上，没有新机制。</para>
///
/// <para>## 指令序列一个字都不用改</para>
///
/// <para><c>MOVE reg, &lt;标签&gt;</c> 取的**就是地址**（运行时是 LEA，见
/// <c>VMLRuntime.Instructions.cs</c> 的 <c>OperandType.LABEL</c> 分支），
/// 后面那句 <c>MOVEB reg, [reg]</c> / <c>MOVE reg, [reg]</c> 照旧取字节/字，
/// 写回是 <c>MOVEB [reg], reg</c>。所以迁移只动"地址从哪来"，语义一字不变。</para>
///
/// <para><b>⚠ 但 <c>ADD reg, #&lt;标签&gt;</c> / <c>CMP reg, #&lt;标签&gt;</c> 不可用</b> ——
/// 运行时的 <c>GetRegisterOrImmediate</c> 只认 REGISTER 与 IMMEDIATE，
/// 见到 LABEL 一律返回 0（静默算错，不报错）。需要"基址 + 偏移"时，
/// 先把标签地址取进一个寄存器，再走**寄存器对寄存器**的 ADD。</para>
///
/// <para>## 登记表只有一份</para>
///
/// <para>标签名与初值**全部**写在下面的 <see cref="Sys"/> 与 <see cref="SysVarInit"/> 里；
/// 调用点一律写 <c>Sys.XXX</c>，**不在原地拼字符串、也不在别处再列一张表**。
/// 本仓的头号坑就是"同一规则两处实现" —— 漂了之后只改一边，症状是"改了一处却没生效"。</para>
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// 系统全局变量的**标签名**（唯一真源）。
    ///
    /// <para>命名规则：<c>_sys_&lt;用途&gt;</c>，下划线前缀避免与用户 BASIC 变量/字符串常量撞名
    /// （用户标识符走 <c>var_</c>/<c>str_</c> 前缀，见 <c>BasicSymbol</c>）。</para>
    ///
    /// <para>**三个例外**：<see cref="ScreenMode"/> / <see cref="FgIndex"/> / <see cref="BgIndex"/>
    /// 直接引用 Ui 路已经迁好的那三个标签（<c>_ui_screen_mode</c> 等），不另起名字 ——
    /// 它们是**同一份状态**（Ui 后端与 PcGfx 后端读写的是同一批槽），
    /// 起两个名就是"同一规则两处实现"的翻版。</para>
    /// </summary>
    internal static class Sys
    {
        // ── 屏幕 / 显示状态 ────────────────────────────────────────────────
        /// <summary>SCREEN 模式字节（原 <c>0x6FF0</c>）。0 = 文本模式。</summary>
        public const string ScreenMode  = UiModeLabel;
        /// <summary>前景色索引（原 <c>0x6FFC</c>；文本模式里同时是 VGA 字符属性字节）。</summary>
        public const string FgIndex     = UiFgLabel;
        /// <summary>背景色索引（原 <c>0x6FFD</c>）。</summary>
        public const string BgIndex     = UiBgLabel;
        /// <summary>屏幕（像素）宽（原 <c>0x6FE0</c>）。</summary>
        public const string ScreenWidth = "_sys_screen_width";
        /// <summary>屏幕（像素）高（原 <c>0x6FE4</c>）。</summary>
        public const string ScreenHeight = "_sys_screen_height";
        /// <summary>每像素字节数（原 <c>0x6FF3</c>）。1 = 索引色，2 = 文本模式，3 = 真彩。</summary>
        public const string ScreenBpp   = "_sys_screen_bpp";
        /// <summary>文本列数（原 <c>0x6FF6</c>，WIDTH 语句设的，与像素宽不同）。</summary>
        public const string TextCols    = "_sys_text_cols";
        /// <summary>文本行数（原 <c>0x6FFA</c>）。</summary>
        public const string TextRows    = "_sys_text_rows";
        /// <summary>文本光标行（原 <c>0x6FF4</c>）。</summary>
        public const string TextCurRow  = "_sys_text_cursor_row";
        /// <summary>文本光标列（原 <c>0x6FF8</c>）。</summary>
        public const string TextCurCol  = "_sys_text_cursor_col";

        // ── 字库 ──────────────────────────────────────────────────────────
        /// <summary>字库模式（原 <c>0x6FE8</c>）：0 = 8x8 每行 32 位，1 = 8x16 每行 1 字节。</summary>
        public const string FontMode    = "_sys_font_mode";
        /// <summary>字宽（原 <c>0x6FE9</c>）。</summary>
        public const string FontWidth   = "_sys_font_width";
        /// <summary>字高（原 <c>0x6FEA</c>）。</summary>
        public const string FontHeight  = "_sys_font_height";
        /// <summary>字库基址（原 <c>0x6FEC</c>，32 位）。</summary>
        public const string FontAddr    = "_sys_font_addr";

        // ── 静态数据区 / 文件 / DATA ──────────────────────────────────────
        /// <summary>动态静态区基址（原 <c>0x6FD4</c>）。见 <c>EmitStaticBase</c> 的说明。</summary>
        public const string StaticBase  = "_sys_static_base";
        /// <summary>DATA 读指针（原 <c>0x6FD0</c>）。</summary>
        public const string DataPointer = "_sys_data_pointer";
        /// <summary>错误处理入口（原 <c>0x6FC0</c>）：0 = 无处理器，-1 = ON ERROR RESUME NEXT。</summary>
        public const string ErrorHandler = "_sys_error_handler";
        /// <summary>RESUME NEXT 标志（原 <c>0x6FC4</c>）。</summary>
        public const string ResumeFlag  = "_sys_resume_flag";
        /// <summary>文件句柄表（原 <c>0x9D000</c>，按文件号 × 4 索引）。</summary>
        public const string FileHandles = "_sys_file_handles";

        // ── 图形暂存 ──────────────────────────────────────────────────────
        /// <summary>当前填充色索引（原 <c>0x6DF0</c>）。</summary>
        public const string FillIndex   = "_sys_gfx_fill_index";
        /// <summary>PAINT 的边界色索引（原 <c>0x6DF1</c>）。</summary>
        public const string BorderIndex = "_sys_gfx_border_index";
        /// <summary>PAINT 种子像素的 R（原 <c>0x6DF3</c>）。</summary>
        public const string SeedR       = "_sys_gfx_seed_r";
        /// <summary>PAINT 种子像素的 G（原 <c>0x6DF4</c>）。</summary>
        public const string SeedG       = "_sys_gfx_seed_g";
        /// <summary>PAINT 种子像素的 B（原 <c>0x6DF5</c>）。</summary>
        public const string SeedB       = "_sys_gfx_seed_b";
        /// <summary>DRAW 当前位置 X（原 <c>0x6FA0</c>）。</summary>
        public const string DrawX       = "_sys_draw_x";
        /// <summary>DRAW 当前位置 Y（原 <c>0x6FA4</c>）。</summary>
        public const string DrawY       = "_sys_draw_y";
        /// <summary>DRAW 当前颜色（原 <c>0x6FA8</c>）。</summary>
        public const string DrawColor   = "_sys_draw_color";
        /// <summary>DRAW 缩放因子（原 <c>0x6FAC</c>）。</summary>
        public const string DrawScale   = "_sys_draw_scale";
        /// <summary>DRAW 角度（原 <c>0x6FB0</c>）。</summary>
        public const string DrawAngle   = "_sys_draw_angle";
        /// <summary>调色板（16 色，原 <c>0x9F000</c>；按 <c>索引 × 3</c> 索引的 RGB 字节）。</summary>
        public const string Palette16   = "_sys_palette16";
        /// <summary>调色板（256 色，原 <c>0x9F100</c>）。</summary>
        public const string Palette256  = "_sys_palette256";
        /// <summary>图形帧缓冲（原 <c>0xA0000</c>）。</summary>
        public const string Framebuffer = "_sys_framebuffer";
        /// <summary>文本帧缓冲（原 <c>0xB8000</c>，80×25 的"字符 + 属性"对）。</summary>
        public const string TextBuffer  = "_sys_text_buffer";
        /// <summary>PAINT 泛洪填充的栈（原 <c>0x90000</c>）。</summary>
        public const string PaintStack  = "_sys_paint_stack";
        /// <summary>PAINT 泛洪填充的栈指针（原 <c>0x8FFFC</c>）。</summary>
        public const string PaintStackPtr = "_sys_paint_stack_ptr";
        /// <summary>RANDOMIZE 的随机数种子（原 <c>0x9E000</c>）。</summary>
        public const string RngSeed     = "_sys_rng_seed";
    }

    // ── 各缓冲区的容量（字节 / 项）──────────────────────────────────────
    //  放在这里而不是调用点：容量与标签是同一件事的两面，分开写就会漂。
    /// <summary>文件句柄表项数（BASIC 文件号 1..255，取 256 项封顶）。</summary>
    const int SysFileHandleSlots = 256;
    /// <summary>16 色调色板：16 项 × 3 字节 = 48 字节（`int[12]` 正好 48 字节）。</summary>
    const int SysPalette16Ints = 12;
    /// <summary>256 色调色板：256 项 × 3 字节 = 768 字节。</summary>
    const int SysPalette256Ints = 192;
    /// <summary>文本帧缓冲：80×25 的字符/属性对 = 4000 字节。</summary>
    const int SysTextBufferInts = 1000;
    /// <summary>图形帧缓冲的**字节数**（运行期从堆上要）。</summary>
    const int SysFramebufferBytes = 0x10000;
    /// <summary>
    /// PAINT 泛洪填充栈的**字节数**（运行期从堆上要；每项一个 <c>(x,y)</c> = 8 字节）。
    ///
    /// <para>取 <c>0x10000</c> = 老代码**真正能用的那一段窗口**（<c>0x90000..0xA0000</c>，
    /// 上面紧挨着 VGA 帧缓冲）。老代码里那个"1MB 上限"的守卫是**失效的** ——
    /// 它允许写到 <c>0x190000</c>，而它实际拥有的只有 64KB。现在**容量与守卫用同一个常量**
    /// （见调用点），这才是"同一规则一处实现"。</para>
    /// </summary>
    const int SysPaintStackBytes = 0x10000;

    /// <summary>
    /// 堆缓冲的**容量表**（标签 → 字节数）—— 与 <see cref="Sys"/> 里的标签名一一对应，
    /// **是这两块缓冲容量的唯一来源**（<see cref="HeapBufAddr"/> 与序言分配都取它）。
    ///
    /// <para>新加一块堆缓冲时**两张表都要改**：往 <see cref="Sys"/> 加标签名、
    /// 往这里加容量。漏了后者会在第一次用到它时抛异常（不是静默给个错的容量）。</para>
    /// </summary>
    static readonly Dictionary<string, int> HeapBufBytes = new()
    {
        [Sys.Framebuffer] = SysFramebufferBytes,
        [Sys.PaintStack]  = SysPaintStackBytes,
    };

    /// <summary>
    /// **需要在程序序言里从堆上要一块、并把地址存进自己那个 `.data` 槽**的缓冲区
    /// 标签（登记顺序 = 分配顺序；容量一律现查 <see cref="HeapBufBytes"/>）。
    ///
    /// <para>为什么不直接做成 <c>int[N]</c> 的 `.data` 槽 —— 见下面
    /// <see cref="HeapBufAddr"/> 的说明（一句话：`.data` 总量有 64K 的硬上限，
    /// 而这两块缓冲按语义就该是 64KB 一个）。</para>
    /// </summary>
    readonly List<string> _heapBufs = new();

    /// <summary>这段"运行期分配堆缓冲"的指令**要插回到哪儿**（见 <see cref="HeapBufNoteAllocSite"/>）。</summary>
    int _heapBufInsertAt = -1;

    /// <summary>
    /// 堆缓冲基址 → <paramref name="reg"/>（首次调用会登记"要在序言里分配它"）。
    ///
    /// <para>**槽里存的是指针**（运行期拿到的地址），所以比一般系统全局变量多一句解引用：
    /// <c>reg = &amp;槽</c>，然后 <c>reg = [reg]</c>。</para>
    ///
    /// <para>## 为什么这两块缓冲不放 `.data`（实测踩过，记在这里免得下次又走一遍）</para>
    ///
    /// <para>第一版就是按"缓冲 = <c>int[N]</c>"写的（帧缓冲 64KB）。**结果 put_bitmap.bas
    /// 用 <c>--basicgfx pcgfx</c> 一跑就内存越界**：`main` 序言里那句
    /// <c>move [@R12+24], @R0</c> 打在了 <c>0x200000</c>（= 内存末尾）。</para>
    ///
    /// <para>根因是**运行时的栈顶回退**与**`.data` 总量**的相互作用：入口处
    /// <c>if (sp &lt;= memoryAllocPtr) sp = memory.Length - 4</c>（栈顶撞上堆时退到内存顶端），
    /// 而 `main` 的序言会往 <b>R12 之上</b>写若干槽（<c>[R12+8]…[R12+28]</c>）——
    /// 退到顶端之后只剩 4 字节余量，写 <c>[R12+24]</c> 就直接越界。也就是说：
    /// **`.data` 总量一旦越过 64K（默认栈大小），BASIC 程序就会踩**。
    /// 实测（临时探针读出来的）：<c>sp=65536 memoryAllocPtr=65580</c> ⇒ 触发了回退。</para>
    ///
    /// <para>而这两块缓冲按语义**最小也该是 64KB 一个**（模式 13 一次 CLS 就清 64000 字节）。
    /// 塞进 `.data` 就是"要么越过那条线把程序整体搞挂，要么给个装不下的小缓冲
    /// —— 而小缓冲写穿了会踩坏相邻的 `.data`，**比原来的固定地址还糟**：
    /// 从前写在 <c>0xA0000</c> 时那块内存是空的，踩不到自己的数据"。</para>
    ///
    /// <para>所以：**槽位只存指针，缓冲本体在运行期从堆上分配**（`SYSCALL 40` 可以给到 16MB，
    /// 而栈在内存另一头、不受影响）。`.data` 成本从 64KB+64KB 降到 8 字节，
    /// 于是这次迁移**不会把任何程序推过那条 64K 线**。</para>
    /// </summary>
    void HeapBufAddr(int reg, string label)
    {
        if (!_heapBufs.Contains(label))
        {
            // **没有登记容量就是编程错误** —— 不要用"默认给个 N"糊过去：那会让新增的堆缓冲
            // 悄悄拿一个不匹配的容量。查不到就当场炸（编译期的事，早响早好）。
            if (!HeapBufBytes.ContainsKey(label))
                throw new InvalidOperationException($"堆缓冲 '{label}' 没有在 HeapBufBytes 里登记容量");
            _heapBufs.Add(label);
        }
        SysAddr(reg, label);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}")]));
    }

    /// <summary>帧缓冲基址 → <paramref name="reg"/>（见 <see cref="HeapBufAddr"/>）。</summary>
    void FbBase(int reg) => HeapBufAddr(reg, Sys.Framebuffer);

    /// <summary>
    /// 「帧缓冲基址 + <paramref name="delta"/>」→ <paramref name="reg"/>（"上界比较"那种用途，
    /// 老代码写的是 <c>0xA0000 + 0x100000</c> 这种立即数）。
    ///
    /// <para>**必须分两句**：先取址（LEA），再 <c>ADD reg, #delta</c>（立即数，运行时认）。
    /// 直接写 <c>ADD reg, #&lt;标签&gt;</c> 是不行的 —— 运行时的 <c>GetRegisterOrImmediate</c>
    /// 见到 LABEL 一律返回 0，**静默算错、不报错**。</para>
    /// </summary>
    void FbBasePlus(int reg, int delta)
    {
        FbBase(reg);
        if (delta != 0) AddRI(OpCode.ADD, reg, delta);
    }

    /// <summary>PAINT 泛洪栈基址 → <paramref name="reg"/>（见 <see cref="HeapBufAddr"/>）。</summary>
    void PaintStackBase(int reg) => HeapBufAddr(reg, Sys.PaintStack);

    /// <summary>
    /// 在程序序言里（<c>EmitStaticBase</c> 之后）**记下插入点**。见 <see cref="_heapBufInsertAt"/>。
    /// </summary>
    void HeapBufNoteAllocSite() => _heapBufInsertAt = instructions.Count;

    /// <summary>
    /// 语句全部生成完之后调用：把**登记过的**堆缓冲各分配一次，指令插进序言那个点。
    ///
    /// <para>插入的指令只碰 R0/R1，而那个位置上 R0/R1 都是死的 —— <c>EmitStaticBase(1)</c>
    /// 刚算进 R1 的静态基址**紧接着就会被 `EmitStaticAddr` 重算**，所以这里覆盖掉没有影响
    /// （这一点是看着调用点确认过的，不是默认成立）。</para>
    /// </summary>
    void HeapBufEmitAllocs()
    {
        if (_heapBufs.Count == 0 || _heapBufInsertAt < 0) return;

        var alloc = new List<Instruction>();
        foreach (var label in _heapBufs)
        {
            SysVar(label);                      // 确保槽已登记进 .data 段
            alloc.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, HeapBufBytes[label])]));
            alloc.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 40)]));   // R0 = 分配到的地址
            /* ⚠ 这里**不能**用 `SysAddr(1, …)` —— 它把指令加进 `instructions`（主表），
               而这一段是要插回序言的**局部块**：结果就是"取槽地址"那句跑到了程序末尾、
               `MOVE [R1], R0` 于是往 R1 的**残值**上写（实测确实编出来了，会静默踩坏随便一块内存）。 */
            alloc.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, label)]));
            alloc.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));     // 槽里存指针
        }
        instructions.InsertRange(_heapBufInsertAt, alloc);
        _heapBufInsertAt = -1;
    }

    /// <summary>
    /// 系统全局变量的**初值表** —— 标签 → 初值。
    ///
    /// <para>## 标量一律不列，初值就是 <c>0</c></para>
    ///
    /// <para>**这不是偷懒，是为了"迁移前后行为逐字相同"**：老代码读的那些固定地址
    /// （<c>0x6FE0</c>/<c>0x6FF0</c>…）**没有任何一行代码写过它们**，而 VM 的内存是
    /// <c>new byte[]</c>（全 0），所以"没写过就读 0"正是老行为。
    /// 在这里给某个标量写个"看起来更合理"的初值（比如字宽写 8）**会静默改变程序行为** ——
    /// 那属于另一件事（修默认值），不该混进"换掉固定地址"这一刀里。
    /// 真要改默认值，改这里的数字即可，改完跑一遍探针就知道动了什么。</para>
    ///
    /// <para>## 缓冲区必须列出来（它们是这一表的真正内容）</para>
    ///
    /// <para>**标量写 int、缓冲写 <c>new int[N]</c>**；`int[N]` 在汇编输出里会压成
    /// 一句 <c>.word[N] 0</c>，不会把几百行摊进产物。</para>
    /// </summary>
    static readonly Dictionary<string, object> SysVarInit = new()
    {
        [Sys.FileHandles]    = new int[SysFileHandleSlots],
        [Sys.Palette16]      = new int[SysPalette16Ints],
        [Sys.Palette256]     = new int[SysPalette256Ints],
        // 帧缓冲与 PAINT 泛洪栈**不在这一表里**：它们只是 4 字节的指针槽（初值 0），
        // 缓冲本体在运行期从堆上要（见 HeapBufAddr / HeapBufBytes）。
        [Sys.TextBuffer]     = new int[SysTextBufferInts],
    };

    /// <summary>
    /// 把一个系统全局变量**登记进 <c>.data</c> 段**（懒发射：只登记一次，
    /// <c>dataSection</c> 里有就什么都不做），返回标签本身便于链式使用。
    ///
    /// <para>与 <c>UiEnsureStateSlots</c>/<c>UiEnsurePalette</c> 同一个套路。</para>
    /// </summary>
    string SysVar(string label)
    {
        if (!dataSection.ContainsKey(label))
            dataSection[label] = SysVarInit.TryGetValue(label, out var init) ? init : 0;
        return label;
    }

    /// <summary>
    /// 取一个系统全局变量的**地址**装进 <paramref name="reg"/>（= <c>MOVE reg, &lt;标签&gt;</c>，
    /// 运行时是 LEA）。原先是 <c>MOVE reg, #0x6FE0</c> 那种立即数地址，改法只是换个操作数来源。
    /// </summary>
    void SysAddr(int reg, string label)
    {
        SysVar(label);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.REGISTER, reg), new Operand(OperandType.LABEL, label)]));
    }

}
