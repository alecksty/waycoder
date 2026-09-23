using CompilerBase;
using VMLAssembler;

namespace BasicCompiler;

/// <summary>
/// QBasic 图形语句的 **UI 后端** —— 把 SCREEN / CLS / PSET / LINE / CIRCLE / PAINT / COLOR /
/// PALETTE / GET / PUT 路由到宿主的 <c>ui_*</c> 图元（手机与桌面的那扇绘图窗口）。
///
/// <para>**为什么要有这一层**：老路是把像素写进 DOS 显存 <c>0xA0000</c>，而
/// **现有所有宿主（桌面 vmlcli / MAUI 手机端）都不渲染那一段内存** —— 于是 BASIC 的图形
/// 语句等于画进虚空：程序跑完，屏幕上一个像素都没有。路由到 <c>ui_*</c> 是纯收益。</para>
///
/// <para>**为什么默认走 UI**：老路在**每一个宿主**上都是空操作，所以"保留老路"只是保留一个
/// 什么都看不见的行为。要旧行为可以用 <c>--basicgfx pcgfx</c> / <c>&lt;BasicGraphics&gt;pcgfx&lt;/BasicGraphics&gt;</c>。</para>
///
/// <para>**接线方式**：每个语句的生成函数**开头**一行 <c>if (UiGfx) { UiEmitXxx(stmt); return; }</c>。
/// 这样主程序（<c>CodeGenerator.Statements.cs</c>）与 SUB 体（<c>CodeGenerator.Sub.cs</c>）两条
/// 分派路都自动生效 —— 不必去动那两处 if 链（LOCATE / COLOR 当初就是这么整条换掉的）。</para>
///
/// <para>**屏幕状态**（模式 / 前景背景色 / 是否已开窗）编译期跟着走：
/// <see cref="_uiMode"/> 记当前 SCREEN 模式，<c>-1</c> = 运行期才知道。运行期模式那一路
/// （GORILLA.BAS 就是 <c>SCREEN Mode</c>，Mode 是变量）用一段运行期比较链选分辨率，
/// 并把"已经开出的窗口模式"存在 <see cref="UiOpenedModeAddr"/>，同模式重复 SCREEN 不重复开窗。</para>
/// </summary>
public partial class CodeGenerator
{
    // ── 运行期状态地址（沿用这一批已有的固定暂存区 0x6FA0~0x6FFF）──────────────
    /// <summary>已经开出的绘图窗口的 SCREEN 模式；0 = 还没开窗（0 本身是文本模式，不占语义）</summary>
    const int UiOpenedModeAddr = 0x6FE8;   // 注意：0x6FE8 在 PcGfx 里是 VGA_FONT_MODE，只有 pcgfx 后端用
    const int UiModeAddr       = 0x6FF0;   // SCREEN 模式字节（沿用既有的那个地址，CLS 等运行期分支读它）
    const int UiFgAddr         = 0x6FFC;   // 前景色索引（沿用既有的）
    const int UiBgAddr         = 0x6FFD;   // 背景色索引（沿用既有的）

    /// <summary>默认窗口标题（写进 data section，只分配一次）</summary>
    const string UiTitleLabel = "ui_win_title_basic";

    // ── 编译期跟踪的屏幕状态 ──────────────────────────────────────────────
    int  _uiMode        = -1;    // 当前 SCREEN 模式；-1 = 运行期才知道
    int  _uiFg          = 15;    // COLOR 设的前景色索引
    int  _uiBg          = 0;     // COLOR 设的背景色索引（CLS 用它）
    bool _uiPaletteReady;        // 调色板是否已经初始化过（懒发射，只发一次）
    int  _uiOpenedConstMode = -1;// 编译期已知"已经开出窗口"的那个模式（-1 = 没有）
    bool _uiOpenedKnownAtCompileTime = true;  // 出现过运行期 SCREEN 之后置 false

    /// <summary>图形后端是不是 UI。<c>--basicgfx ui</c>（默认）/ <c>pcgfx</c>（老的写显存）。</summary>
    bool UiGfx => VMLPlugins.CompilerOptionsContext.Current.BasicGraphics == VMLPlugins.BasicGraphics.Ui;

    // ══════════════════════════════════════════════════════════════════════
    //  通用发射：调一个 ui_* 图元
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 调一个 <c>ui_*</c> 宿主图元。
    ///
    /// **调用约定（逐字照抄 <c>Lib/shared/vmlui.vml</c> 的包装函数）**：
    /// 参数**右到左**压栈（最后一个参数先 push ⇒ 第一个参数最后 push、落在 <c>[R12+12]</c>），
    /// 然后 <c>CALL &lt;裸标签小写&gt;</c>，最后**调用方**清栈 <c>ADD R13, #参数个数*4</c>。
    /// 例：<c>ui_rect</c> 从 <c>[@R12+12]</c> 起连续读 8 个参数。
    ///
    /// 每个参数是一个"求值到 R0"的小委托 —— R0 是唯一的传递寄存器，压栈之后就可以随便用了。
    /// </summary>
    void UiCall(string label, params Action[] argEmitters)
    {
        for (int i = argEmitters.Length - 1; i >= 0; i--)
        {
            argEmitters[i]();                                // 求值 → R0
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
        }
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, label)]));
        if (argEmitters.Length > 0)
            instructions.Add(new Instruction(OpCode.ADD,
                [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argEmitters.Length * 4)]));
    }

    /// <summary>参数：整数立即数（发射 MOVE R0, imm）</summary>
    Action UiConst(int v) => () => AddRI(OpCode.MOVE, 0, v);

    /// <summary>参数：表达式求值到 R0（SUB 内外走对路，浮点还会补 F2I）</summary>
    Action UiEval(Expression e) => () => EvalIntCoord(e, 0);

    /// <summary>参数：调色板索引表达式 → 0xAARRGGBB</summary>
    Action UiEvalColor(Expression e, int defaultIndex = 15) => () =>
    {
        if (e == null)
            AddRI(OpCode.MOVE, 0, defaultIndex);
        else
            EvalIntCoord(e, 0);
        UiTranslateColorInR0();
    };

    // ══════════════════════════════════════════════════════════════════════
    //  调色板：索引 → 0xAARRGGBB
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 调色板表 —— **`.data` 段里的 16 项 <c>int[16]</c>**，每项一个 <c>0xAARRGGBB</c>。
    ///
    /// ## 为什么是 data 段而不是固定内存地址
    ///
    /// 第一版写死在 <c>0x9F000</c>（PcGfx 的老调色板地址），**结果整张表读出全 0**
    /// —— 实测 gfx_demo 的 DSL 里每个颜色都是 <c>#FF000000</c>（全黑）。
    /// 固定地址那块内存的初值不由我们说了算（谁来清零、清了没有都不确定）。
    /// 放进 <c>.data</c> 则**由链接器分配、由初值表装载**，是**已经初始化好**的。
    ///
    /// ## 它是运行期表，不是只读常量
    ///
    /// <c>PALETTE</c> 语句改的就是它（<see cref="UiEmitPaletteStatement"/>），
    /// 每次用到颜色时现查（<see cref="UiTranslateColorInR0"/>）。我们窗口是真彩，
    /// 所以"调色板"就是这张"索引 → 真彩"的翻译表。
    /// </summary>
    const string UiPaletteLabel = "_ui_palette_argb";

    /// <summary>标准 EGA 16 色（与 <c>GenerateInitPalette</c> 的 pr/pg/pb 是同一张表的 0-255 值）</summary>
    static readonly int[] UiEgaR = [0, 0, 0, 0, 170, 170, 170, 170, 85, 85, 85, 85, 255, 255, 255, 255];
    static readonly int[] UiEgaG = [0, 0, 170, 170, 0, 0, 85, 170, 85, 85, 255, 255, 85, 85, 255, 255];
    static readonly int[] UiEgaB = [0, 170, 0, 170, 0, 170, 0, 170, 85, 255, 85, 255, 85, 255, 0, 255];

    /// <summary>
    /// 取"这个模式下的调色板初值"（16 项 ARGB）。
    ///
    /// <para>**调色板随模式不同**（用户点名要注意的那条）：</para>
    /// <list type="bullet">
    ///   <item>**模式 1（CGA 320×200 4 色）**：默认 CGA 组 1 —— 0 黑 / 1 青 / 2 品红 / 3 白，
    ///         4-15 沿用 EGA 值。`COLOR , n` 切到 CGA 组 2 会换成 0 黑 / 1 绿 / 2 红 / 3 棕，
    ///         但**本后端没有跟踪 `COLOR , n` 的那一位**（`QbColorStatement` 不带它），
    ///         所以组 2 只在 `PALETTE` 手动改写时才生效 —— 如实写在这里，不假装支持。</item>
    ///   <item>**模式 2（CGA 640×200 2 色）**：只有黑白两色（0 黑 / 1 白），其余沿用 EGA。</item>
    ///   <item>**其余图形模式**：标准 EGA 16 色（与 `GenerateInitPalette` 同一张表）。</item>
    ///   <item>**模式 13（320×200 256 色）**：本后端**只做 16 色**，索引一律掩到 15
    ///         （见 <see cref="UiTranslateColorInR0"/>）—— 256 色的那张表要靠 PALETTE
    ///         逐项设，没做。</item>
    /// </list>
    /// </summary>
    int[] UiDefaultPalette(int mode)
    {
        var p = new int[16];
        for (int i = 0; i < 16; i++)
            p[i] = unchecked((int)(0xFF000000u | ((uint)UiEgaR[i] << 16) | ((uint)UiEgaG[i] << 8) | (uint)UiEgaB[i]));
        if (mode == 2)
        {
            // CGA 2 色：0 黑 / 1 白，2-15 仍是 EGA（老程序很少用模式 2 画彩色）
            p[1] = unchecked((int)0xFFFFFFFFu);
        }
        else if (mode == 1)
        {
            // CGA 4 色 组 1：0 黑 / 1 青 / 2 品红 / 3 白
            p[0] = unchecked((int)0xFF000000u);
            p[1] = unchecked((int)0xFF00AAAAu);
            p[2] = unchecked((int)0xFFAA00AAu);
            p[3] = unchecked((int)0xFFFFFFFFu);
        }
        return p;
    }

    /// <summary>
    /// 懒发射调色板初值（同一次编译只发一次）。放进 `.data`，由链接器分配地址与装载初值。
    /// 模式已知时按模式取表（<see cref="UiDefaultPalette"/>）；运行期模式取 EGA 那张。
    /// </summary>
    void UiEnsurePalette()
    {
        if (_uiPaletteReady) return;
        _uiPaletteReady = true;
        dataSection[UiPaletteLabel] = UiDefaultPalette(_uiMode);
    }

    /// <summary>
    /// <c>R0</c> 里的调色板索引 → 翻成 <c>0xAARRGGBB</c>（结果留在 R0）。
    ///
    /// <para>**暂存寄存器必须从 <see cref="Regs"/> 申请，不能写死一个号**：
    /// 调用点可能在 R1/R3… 里**正拿着活值**（LINE 的 B/BF 那条路把 4 个坐标与一个临时量
    /// 放在寄存器里，而颜色是在它们之后才求值的）—— 写死 R1 会把坐标整个冲掉，
    /// 表现是**矩形画不出来**（宽高算成了垃圾），实测踩过。</para>
    ///
    /// <para>**索引超界一律掩到 15**：模式 13 的 256 色没做（见 <see cref="UiDefaultPalette"/>）。
    /// 与其假装支持，不如让 16 色以上落到表里那一项 —— 行为是确定的、可解释的。</para>
    ///
    /// <para><paramref name="pinned">≥0</paramref> 时用**调用方指定的**暂存寄存器、且不归还有关池
    /// —— 文本子程序（<c>CodeGenerator.Qbasic.UiGfx.Text.cs</c>）里全程用固定寄存器编号，
    /// 不能借 <see cref="Regs"/>（它的池从 R1 开始，正好和那边要保护的值撞上）。</para>
    /// </summary>
    void UiTranslateColorInR0(int pinned = -1)
    {
        UiEnsurePalette();
        int t = pinned >= 0 ? pinned : Regs.AllocInt(instructions);
        AddRI(OpCode.MOVE, t, 15);
        AddRR(OpCode.AND, 0, t);                      // R0 = 索引 & 15
        AddRI(OpCode.MOVE, t, 4);
        AddRR(OpCode.MUL, 0, t);                      // R0 = 索引 * 4（每项一个 32 位 ARGB）
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, t), new Operand(OperandType.LABEL, UiPaletteLabel)]));
        AddRR(OpCode.ADD, 0, t);                      // R0 = 表项地址
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, t), new Operand(OperandType.MEMORY, "R0")]));
        AddRR(OpCode.MOVE, 0, t);                     // R0 = ARGB
        if (pinned < 0) Regs.FreeInt(t, instructions);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  每个图形语句一圈的保护：把 R1..R11/R14 存起来
    // ══════════════════════════════════════════════════════════════════════
    //
    // ui_* 是共享库函数（会 `push R15/R12; sub R13 #16`），而且底下是 syscall ——
    // 宿主处理器可能把寄存器当草稿纸。老代码（PSET/LINE/CIRCLE）本来就习惯
    // `EmitSaveRegisters(4,5,...,14)` 再说，这里照做，省得"某个语句悄悄踩了循环变量"。
    static readonly int[] UiClobbered = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14];

    /// <summary>UI 语句两端成对使用：{ EmitSaveRegisters(UiClobbered) … UiLeave() }</summary>
    void UiEnter() => EmitSaveRegisters(UiClobbered);

    /// <summary>
    /// 语句收尾：**先 <c>ui_present()</c> 再恢复寄存器**（顺序不能反 —— present 自己也会
    /// 用寄存器，恢复放在它前面等于没恢复）。
    ///
    /// <para>
    /// **为什么要在这里 present**：QBasic 是**立即模式** —— 一条图形语句执行完，屏幕上
    /// 就该看到它。<c>ui_*</c> 图元只改场景，真正"上屏"靠 <c>ui_present()</c>；
    /// C 程序都是自己每帧末尾调一次，而 BASIC **没有任何语句能表达它** ⇒ 一个不写
    /// <c>NATIVE SUB ui_present()</c> 的 BASIC 图形程序**永远不出图**：设备上窗口一直空着
    /// （GORILLA.BAS 就是这样，场景里 512 个图元、窗口上 0 个像素），`vmlcli --frame`
    /// 也拿不到帧（它只认 present 拍下的快照）。所以由编译器在每条图形语句末尾补一次。
    /// </para>
    ///
    /// <para>
    /// 代价可控：设备侧渲染挂在 40ms 定时器上比对 <c>PresentVersion</c>，
    /// 一串语句里的多次 present 会被合并成一次渲染（见 <c>VmlScene.Present</c> 的注释）。
    /// </para>
    /// </summary>
    void UiLeave()
    {
        UiCall("ui_present");
        EmitRestoreRegisters(UiClobbered);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SCREEN
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>模式 → (宽, 高)。未知模式按 <c>640x480</c> 兜底，与既有 PcGfx 表一致。</summary>
    static (int W, int H) UiResolutionOf(int mode) => mode switch
    {
        1  => (320, 200),   // CGA 4 色
        2  => (640, 200),   // CGA 2 色
        7  => (320, 200),   // EGA 16 色
        8  => (640, 200),   // EGA 16 色
        9  => (640, 350),   // EGA 16 色
        11 => (640, 480),   // VGA 2 色
        12 => (640, 480),   // VGA 16 色
        13 => (320, 200),   // VGA 256 色
        _  => (640, 480),   // 未知/文本以外的模式：兜底
    };

    /// <summary>
    /// <c>SCREEN mode</c> —— 用户特别交代的语义：
    /// **模式 0 = 文本模式（命令行），绝不弹窗**；**其它模式 = 图形模式 ⇒ 弹窗**。
    ///
    /// <para>**编译期知道的模式**（字面量 / 常量，且此前没出现过运行期 SCREEN）就地判定，
    /// 直接发"开窗"或"只清屏"，不生成任何比较链。</para>
    ///
    /// <para>**运行期模式**（`SCREEN Mode`，Mode 是变量 —— GORILLA.BAS 就是这一路）走：
    /// <code>
    ///   R0 = mode ; [0x6FF0] = mode
    ///   if mode == 0          → 文本模式：已开窗模式清零，结束（不碰窗口）
    ///   if mode == _ui_opened → 同一模式重复 SCREEN：不开窗，只 ui_clear
    ///   else                  → 选分辨率 → ui_win_open_ex → _ui_opened = mode → ui_clear
    /// </code>
    /// "已开窗模式"存**本前端自己的 data 段全局**（<see cref="UiOpenedModeLabel"/>，
    /// 初值 0 = 没开过）—— 用固定内存地址的话那块内存的初值不由我们说了算，
    /// 一旦恰好等于当前模式就永远不会开窗（实测踩过）。</para>
    /// </summary>
    void UiEmitScreenStatement(ScreenStatement stmt)
    {
        int constMode = TryConstInt(stmt.Mode);   // -1 = 运行期

        // ① 模式求值 → R0，并写回 0x6FF0
        // ⚠ **必须是"存"**（`MOVEB [R1], R0` = 操作数 `[MEMORY "R1"], [REGISTER 0]`）——
        //   `MOVE` 是 **dest-first**，写成 `[REGISTER 0, MEMORY "R1"]` 是**读**，
        //   于是这个字节永远是 0、CLS 的运行期分支永远走文本模式（实测踩过）。
        //   PcGfx 老代码里同样的写法是"读"，只是那条路在写显存、没人验过。
        if (constMode >= 0)
            AddRI(OpCode.MOVE, 0, constMode);
        else
            EvalIntCoord(stmt.Mode, 0);
        AddRI(OpCode.MOVE, 1, UiModeAddr);
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));

        _uiMode = constMode;
        UiEnsureOpenedModeVar();

        // ② 编译期能把整件事定下来：此前没出现过运行期 SCREEN，且本次是常量模式
        if (constMode >= 0 && _uiOpenedKnownAtCompileTime)
        {
            if (constMode == 0)
            {
                // 文本模式：不弹窗；"已开窗模式"清零（下次图形 SCREEN 重新开窗）
                _uiOpenedConstMode = -1;
                UiEmitStoreOpenedMode(UiConst(0));
                return;
            }
            if (constMode != _uiOpenedConstMode)
            {
                _uiOpenedConstMode = constMode;
                EmitWindowOpen(constMode);
                UiCall("ui_clear", UiConstIndexed(UiBgAddr));
            }
            else
            {
                // 同一模式重复 SCREEN：不开窗，只清屏（QBasic 的 SCREEN 本来就会清屏）
                UiEnsurePalette();
                UiCall("ui_clear", UiConstIndexed(UiBgAddr));
            }
            return;
        }

        // ③ 运行期决定性：此后一律按运行期处理（编译期跟踪失效）
        _uiOpenedKnownAtCompileTime = false;
        string textMode = newLabel();
        string sameMode = newLabel();
        string done     = newLabel();

        // 模式 0 = 文本模式 —— 不弹窗（老程序靠它从图形退回命令行）
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, textMode)]));

        // 同一模式重复 SCREEN：不重复开窗
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, UiOpenedModeLabel)]));
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
        instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, sameMode)]));

        // 选分辨率 → R4=宽, R5=高（常量的就地取，运行期的走比较链）
        if (constMode >= 0)
        {
            var (w, h) = UiResolutionOf(constMode);
            AddRI(OpCode.MOVE, 4, w);
            AddRI(OpCode.MOVE, 5, h);
        }
        else
        {
            UiEmitResolutionChain();
        }

        // ⚠ **模式要先存到栈上再开窗** —— `ui_win_open_ex` 的返回值占着 R0，
        //   开窗之后再想拿"这次是什么模式"就没地方取了（实测第一版就是这么错的：
        //   存进去的是返回码 1，于是第二次 SCREEN 12 被当成"同一模式"跳过开窗）。
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));

        // ui_win_open_ex(标题, 宽, 高, 可转屏, 手柄)
        //   可转屏 = 0（老接口：跟随旋转但**不动坐标系**）—— BASIC 程序不会处理转屏后的
        //   重排，声明成"可换坐标系"只会让画面被裁掉。
        //   手柄 = 1：手机端底部那排屏幕手柄，游戏程序要的就是它。
        if (!dataSection.ContainsKey(UiTitleLabel))
            dataSection[UiTitleLabel] = "BASIC";
        UiCall("ui_win_open_ex",
            UiConstLabel(UiTitleLabel),
            () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)])),
            () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5)])),
            UiConst(0),
            UiConst(1));

        // CGA 模式的调色板与 EGA 不同，但**运行期才知道是不是 CGA** ⇒ 在这里补一次改写。
        // 覆盖在 `UiEnsurePalette()` 的 EGA 默认值之上（那张表是运行期表，PALETTE 语句走同一条路）。
        // 编译期就知道模式时不需要这段（`EmitWindowOpen` 直接按模式取表）。
        UiEmitCgaPaletteFixup();

        // 记住已开窗模式（刚 POP 回来）+ 文本网格（模式还在 R0，网格链正需要它）+ 清屏
        UiEmitStoreOpenedMode(() => instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])));
        UiTextEmitGridChain();
        UiTextResetCursor();
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, sameMode)]));

        // 文本模式：把"已开窗模式"清零（下次图形 SCREEN 会重新开窗）
        AddLabel(textMode);
        UiEmitStoreOpenedMode(UiConst(0));
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, done)]));

        // 同模式重复 SCREEN：QBasic 的 SCREEN 会清屏，这里保持
        AddLabel(sameMode);
        UiEnsurePalette();
        UiCall("ui_clear", UiConstIndexed(UiBgAddr));

        AddLabel(done);
    }

    /// <summary>本前端自己的"已开窗模式"全局（`.data`，初值 0；0 是文本模式故不占语义）</summary>
    const string UiOpenedModeLabel = "_ui_opened_screen_mode";

    void UiEnsureOpenedModeVar()
    {
        if (!dataSection.ContainsKey(UiOpenedModeLabel))
            dataSection[UiOpenedModeLabel] = 0;
    }

    /// <summary>先求值到 R0（<paramref name="emitValue"/>），再把它按字节存进"已开窗模式"</summary>
    void UiEmitStoreOpenedMode(Action emitValue)
    {
        UiEnsureOpenedModeVar();
        emitValue();
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, UiOpenedModeLabel), new Operand(OperandType.REGISTER, 0)]));
    }

    /// <summary>
    /// 运行期 SCREEN 的 **CGA 调色板补丁**：`mode == 1` 写 CGA 组 1（0 黑/1 青/2 品红/3 白）、
    /// `mode == 2` 写 {0 黑, 1 白}。其余模式不动（EGA 那张默认表就是对的）。
    ///
    /// <para>**为什么不只做编译期**：`SCREEN Mode`（Mode 是变量）是老游戏的常态，
    /// 而"这个模式是不是 CGA"要到运行期才知道。补丁很小（两次比较 + 最多 8 个 32 位存），
    /// 发一次就够。</para>
    ///
    /// <para>`COLOR , n` 切换 CGA 组 2（0 黑/1 绿/2 红/3 棕）**没做** —— `QbColorStatement`
    /// 里根本没有那一位（只有 fg/bg），如实记在这里，不假装支持。</para>
    /// </summary>
    void UiEmitCgaPaletteFixup()
    {
        UiEnsurePalette();
        UiEnter();
        string skip1 = newLabel(), skip2 = newLabel(), done = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.JNE, [new Operand(OperandType.LABEL, skip1)]));
        UiEmitPaletteStore(0, unchecked((int)0xFF000000));
        UiEmitPaletteStore(1, unchecked((int)0xFF00AAAA));
        UiEmitPaletteStore(2, unchecked((int)0xFFAA00AA));
        UiEmitPaletteStore(3, unchecked((int)0xFFFFFFFF));
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, done)]));
        AddLabel(skip1);
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2)]));
        instructions.Add(new Instruction(OpCode.JNE, [new Operand(OperandType.LABEL, skip2)]));
        UiEmitPaletteStore(0, unchecked((int)0xFF000000));
        UiEmitPaletteStore(1, unchecked((int)0xFFFFFFFF));
        AddLabel(skip2);
        AddLabel(done);
        UiLeave();
    }

    /// <summary>写调色板表的一项（<c>R0</c> 会被用掉）</summary>
    void UiEmitPaletteStore(int index, int argb)
    {
        AddRI(OpCode.MOVE, 1, index);
        AddRI(OpCode.MOVE, 2, 4);
        AddRR(OpCode.MUL, 1, 2);
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.LABEL, UiPaletteLabel)]));
        AddRR(OpCode.ADD, 1, 2);
        AddRI(OpCode.MOVE, 2, argb);
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 2)]));
    }

    /// <summary>编译期已知"该开窗"的这一路：发一次 <c>ui_win_open_ex</c>（常量模式）</summary>
    void EmitWindowOpen(int mode)
    {
        var (w, h) = UiResolutionOf(mode);
        if (!dataSection.ContainsKey(UiTitleLabel))
            dataSection[UiTitleLabel] = "BASIC";
        UiCall("ui_win_open_ex", UiConstLabel(UiTitleLabel), UiConst(w), UiConst(h), UiConst(0), UiConst(1));
        UiEnsurePalette();
        // 文本网格（列数/行数/格高）与光标 —— 开窗是唯一"知道这次窗口多大"的地方
        UiTextApplyGridConst(mode);
        UiTextResetCursor();
    }

    /// <summary>MOVE R0, &lt;标签&gt; —— 取字符串常量地址</summary>
    Action UiConstLabel(string label) => () =>
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label)]));

    /// <summary>参数：读一个**运行期字节**（如当前背景色索引）→ 翻成 ARGB</summary>
    Action UiConstIndexed(int addr) => () =>
    {
        AddRI(OpCode.MOVE, 0, addr);
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
        UiTranslateColorInR0();
    };

    /// <summary>
    /// 运行期模式 → 分辨率比较链（模式 1/2/7/8/9/11/12/13，其余兜底 640x480）。
    /// 与 <see cref="UiResolutionOf"/> 共用同一张表的**数值**，但表在 C# 侧只写一遍：
    /// 这里是从 <see cref="UiResolutionOf"/> 反查出来的分支，不另抄一份数字。
    /// </summary>
    void UiEmitResolutionChain()
    {
        string chosen = newLabel();
        AddRI(OpCode.MOVE, 4, 640);      // 兜底
        AddRI(OpCode.MOVE, 5, 480);
        // 按模式枚举，取 (宽,高) 相同的归并成一组比较
        var groups = new (int Mode, int W, int H)[] { (1, 320, 200), (2, 640, 200), (7, 320, 200), (8, 640, 200), (9, 640, 350), (11, 640, 480), (12, 640, 480), (13, 320, 200) };
        var emitted = new HashSet<(int, int)>();
        foreach (var g in groups)
        {
            var res = UiResolutionOf(g.Mode);
            if (!emitted.Add(res)) continue;
            var modes = groups.Where(x => UiResolutionOf(x.Mode) == res).Select(x => x.Mode).ToArray();
            string next = newLabel();
            foreach (int m in modes)
            {
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, m)]));
                instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, next)]));
            }
            string skip = newLabel();
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, skip)]));
            AddLabel(next);
            AddRI(OpCode.MOVE, 4, res.W);
            AddRI(OpCode.MOVE, 5, res.H);
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, chosen)]));
            AddLabel(skip);
        }
        AddLabel(chosen);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CLS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>CLS —— 图形模式 <c>ui_clear(背景色)</c>；文本模式保持原样（走 CRT）。</summary>
    void UiEmitClsStatement()
    {
        // 文本/图形是**运行期**才知道的（SCREEN Mode 的 Mode 可以是变量），
        // 所以这里与老代码同构地判一次 0x6FF0。
        string gfx = newLabel();
        string done = newLabel();
        AddRI(OpCode.MOVE, 0, UiModeAddr);
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JNE, [new Operand(OperandType.LABEL, gfx)]));

        // 文本模式：老路（ANSI 清屏）
        CrtMode = true;
        EmitCallBuiltin("CRT_CLRSCR");
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, done)]));

        AddLabel(gfx);
        UiCall("ui_clear", UiConstIndexed(UiBgAddr));
        // QBasic 的 CLS 会把光标归位（1,1）—— 文本光标与窗口光标都要；这里只做窗口那个
        UiTextResetCursor();
        AddLabel(done);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PSET / LINE / CIRCLE / PAINT
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>PSET (x, y), color → <c>ui_pixel(x, y, color)</c></summary>
    void UiEmitPsetStatement(PsetStatement stmt)
    {
        UiEnter();
        UiCall("ui_pixel", UiEval(stmt.X), UiEval(stmt.Y), UiEvalColor(stmt.Color, _uiFg));
        UiLeave();
    }

    /// <summary>
    /// LINE (x1,y1)-(x2,y2), color [, B | BF] →
    /// 普通线 <c>ui_line</c>；<c>B</c> 空心矩形 <c>ui_rect(fill=0)</c>；<c>BF</c> 实心 <c>ui_rect(fill=1)</c>。
    /// </summary>
    void UiEmitLineStatement(QbLineStatement stmt)
    {
        UiEnter();
        if (stmt.Box || stmt.Fill)
        {
            // 矩形：QBasic 的 (x1,y1)-(x2,y2) 是**包含两端**的，宽高要 +1
            //   w = |x2-x1| + 1，h = |y2-y1| + 1
            int x1 = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.X1, x1);
            int y1 = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.Y1, y1);
            int x2 = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.X2, x2);
            int y2 = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.Y2, y2);
            int tw = Regs.AllocInt(instructions);   // 宽/高/上左角的临时量
            int fill = stmt.Fill ? 1 : 0;
            UiCall("ui_rect",
                // x = min(x1,x2)
                () => { EmitMin(tw, x1, x2); instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, tw)])); },
                // y = min(y1,y2)
                () => { EmitMin(tw, y1, y2); instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, tw)])); },
                // w = |x2-x1| + 1
                () => { EmitAbsDiff(tw, x1, x2); instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, tw)])); },
                // h = |y2-y1| + 1
                () => { EmitAbsDiff(tw, y1, y2); instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, tw)])); },
                UiEvalColor(stmt.Color, _uiFg),
                UiConst(fill),
                UiConst(1),     // 线宽
                UiConst(0));    // 圆角
            Regs.FreeInt(tw, instructions);
            Regs.FreeInt(y2, instructions);
            Regs.FreeInt(x2, instructions);
            Regs.FreeInt(y1, instructions);
            Regs.FreeInt(x1, instructions);
        }
        else
        {
            UiCall("ui_line",
                UiEval(stmt.X1), UiEval(stmt.Y1), UiEval(stmt.X2), UiEval(stmt.Y2),
                UiEvalColor(stmt.Color, _uiFg),
                UiConst(1));    // 线宽
        }
        UiLeave();
    }

    /// <summary>tw = min(a, b)</summary>
    void EmitMin(int dst, int a, int b)
    {
        AddRR(OpCode.MOVE, dst, a);
        string done = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, b), new Operand(OperandType.REGISTER, a)]));
        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, done)]));
        AddRR(OpCode.MOVE, dst, b);
        AddLabel(done);
    }

    /// <summary>tw = |a - b| + 1</summary>
    void EmitAbsDiff(int dst, int a, int b)
    {
        AddRR(OpCode.MOVE, dst, a);
        AddRR(OpCode.SUB, dst, b);
        string done = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, dst), new Operand(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, done)]));
        AddRI(OpCode.MOVE, 0, 0);
        AddRR(OpCode.SUB, 0, dst);
        AddRR(OpCode.MOVE, dst, 0);
        AddLabel(done);
        AddRI(OpCode.MOVE, 0, 1);
        AddRR(OpCode.ADD, dst, 0);
    }

    /// <summary>
    /// CIRCLE (x,y), r, color [, start, end, aspect] → <c>ui_circle</c> / <c>ui_ellipse</c>。
    ///
    /// <para>**弧（start/end）不支持，而且不静默** —— 见 <see cref="UiWarnArcUnsupported"/>：
    /// 编译期打一条真正的告警（`; 警告:` 注释进汇编 + DiagnosticBag），运行期**画整圆**。
    /// 本平台没有任何"按角度画弧"的宿主图元（<c>ui_circle</c> 只画整圆、
    /// <c>ui_draw_pie</c> 是扇形填充），所以要真做就得往共享库加 <c>ui_arc</c> —— 那一步没做。</para>
    /// </summary>
    void UiEmitCircleStatement(QbCircleStatement stmt)
    {
        UiEnter();

        bool hasArc = stmt.HasStart || stmt.HasEnd;
        bool hasAspect = stmt.HasAspect;

        if (hasArc)
            UiWarnArcUnsupported();

        if (hasAspect)
        {
            // ui_ellipse(cx, cy, rx, ry, color, fill, lw)
            int rx = Regs.AllocInt(instructions);
            int ry = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.Radius, rx);
            EmitAspectRadius(ry, stmt.Aspect, rx);
            UiCall("ui_ellipse",
                UiEval(stmt.X), UiEval(stmt.Y),
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, rx)])),
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, ry)])),
                UiEvalColor(stmt.Color, _uiFg),
                UiConst(0),     // fill
                UiConst(1));    // lw
            Regs.FreeInt(ry, instructions);
            Regs.FreeInt(rx, instructions);
        }
        else
        {
            UiCall("ui_circle",
                UiEval(stmt.X), UiEval(stmt.Y), UiEval(stmt.Radius),
                UiEvalColor(stmt.Color, _uiFg),
                UiConst(0),     // fill
                UiConst(1));    // lw
        }
        UiLeave();
    }

    /// <summary>aspect → y 半径：<c>ry = |radius * aspect|</c>（QBasic 的 aspect = y/x）</summary>
    void EmitAspectRadius(int dst, Expression aspect, int rx)
    {
        EvalIntCoord(aspect, dst);
        AddRI(OpCode.MOVE, 0, 0);
        string pos = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, dst), new Operand(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, pos)]));
        AddRR(OpCode.SUB, 0, dst);
        AddRR(OpCode.MOVE, dst, 0);
        AddLabel(pos);
        AddRR(OpCode.MUL, dst, rx);
    }

    /// <summary>
    /// CIRCLE 带起始/结束角的**响亮告警**（不静默画错）：汇编里落一条 `; 警告: …` 注释，
    /// 同时进 <see cref="Diags"/>（编译器诊断），并且只报一次（同一处重复的 CIRCLE 不刷屏）。
    /// </summary>
    void UiWarnArcUnsupported()
    {
        WarnUnimplemented("CIRCLE 的起始/结束角（弧）—— 本平台没有按角度画弧的宿主图元，已落回整圆");
        if (_uiArcWarned) return;
        _uiArcWarned = true;
        Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UnsupportedExpression,
            "CIRCLE 的 start/end（画弧）尚未实现：宿主图元里没有按角度画弧的接口，"
            + "本后端**落回整圆**（画出来会比原程序多出弧以外的部分，不是静默-忽略而是可见的差异）。"
            + "纵横比 aspect 是支持的（走 ui_ellipse）。");
    }

    bool _uiArcWarned;

    /// <summary>
    /// `PAINT (x, y), color [, border]` → <c>ui_flood_fill(x, y, color, border)</c>。
    ///
    /// <para>⚠ **省掉 border 时，界色取"填充色"，不是前景色**。这条是"画一个闭合图形
    /// 再灌色"这个惯用法的关键：图形是用 `color` 画的、灌的也是 `color`
    /// （GORILLA 的太阳就是 `CIRCLE (x,y), r, SUNATTR` + `PAINT (x,y), SUNATTR`），
    /// 界色若取前景色（那份程序里是 7），填充会**穿过图形轮廓漫过整个屏幕**
    /// ——实测天空被灌成 SUNATTR 色、整屏只剩一个颜色，而"一个错都不报"。</para>
    /// </summary>
    void UiEmitPaintStatement(QbPaintStatement stmt)
    {
        UiEnter();
        UiCall("ui_flood_fill",
            UiEval(stmt.X),
            UiEval(stmt.Y),
            UiEvalColor(stmt.Color, _uiFg),
            stmt.HasBorder ? UiEvalColor(stmt.Border, _uiFg)
                           : UiEvalColor(stmt.Color, _uiFg));   // 省略 border ⇒ 界色 = 填充色
        UiLeave();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  COLOR / PALETTE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// COLOR fg [, bg] —— 图形模式下把前景/背景色**索引**记进 0x6FFC/0x6FFD
    /// （后面 PSET/LINE/CIRCLE 的默认色、CLS 的背景色都读它），
    /// **同时保留原有的 CRT 调用**（文本模式那条路一个字没改）。
    /// </summary>
    void UiEmitColorStatement(QbColorStatement stmt)
    {
        // 编译期常量就顺手记下来，给"省略颜色的 PSET/LINE"当默认值
        int fg = TryConstInt(stmt.Foreground);
        int bg = stmt.HasBackground ? TryConstInt(stmt.Background) : -1;
        if (fg >= 0) _uiFg = fg;
        if (bg >= 0) _uiBg = bg;

        // 运行期写回索引（老路也读这两个字节，所以无论如何都要写）
        // ⚠ 操作数顺序：`MOVEB [R1], R0` 才是"存"（见 SCREEN 那处的长注释）
        UiEnter();
        AddRI(OpCode.MOVE, 1, UiFgAddr);
        EvalIntCoord(stmt.Foreground, 0);
        instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
        if (stmt.HasBackground)
        {
            AddRI(OpCode.MOVE, 1, UiBgAddr);
            EvalIntCoord(stmt.Background, 0);
            instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
        }
        UiLeave();

        // 原有的 CRT 调用**保留**（文本模式下 COLOR 仍然要改终端配色）
        CrtMode = true;
        EvalIntCoord(stmt.Foreground, 0);
        EmitCallBuiltin("CRT_TEXTCOLOR");
        if (stmt.HasBackground)
        {
            EvalIntCoord(stmt.Background, 0);
            EmitCallBuiltin("CRT_TEXTBACKGROUND");
        }
    }

    /// <summary>
    /// PALETTE idx [, r, g, b] —— 改写**运行期调色板表**（索引 → RGB）。
    ///
    /// <para>我们窗口是真彩，所以"调色板"就是"索引 → 0xAARRGGBB 的翻译表"，
    /// 由 <see cref="UiTranslateColorInR0"/> 在每次用到颜色时查。
    /// 于是 PALETTE 只要改那张表就够了 —— 不需要任何渲染侧配合。</para>
    ///
    /// <para>两种形式：</para>
    /// <list type="bullet">
    ///   <item><c>PALETTE idx, r, g, b</c>（r/g/b 各 0-63）→ 直接写这一项（0-63 缩放到 0-255）。</item>
    ///   <item><c>PALETTE idx, c</c>（QBasic 的"把显示色 c 赋给属性 idx"）→ 把第 c 项复制到第 idx 项。
    ///         GORILLA.BAS 的 <c>PALETTE 4, 0</c> 就是这个形式。</item>
    /// </list>
    /// <para><c>PALETTE</c>（无参，恢复默认）**没做** —— 见报告。</para>
    /// </summary>
    void UiEmitPaletteStatement(PaletteStatement stmt)
    {
        UiEnsurePalette();
        UiEnter();

        // 表项地址 = 表首 + (idx & 15) * 4
        int addr = Regs.AllocInt(instructions);
        int tmp  = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.ColorIndex, addr);
        AddRI(OpCode.MOVE, tmp, 15);
        AddRR(OpCode.AND, addr, tmp);
        AddRI(OpCode.MOVE, tmp, 4);
        AddRR(OpCode.MUL, addr, tmp);
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, tmp), new Operand(OperandType.LABEL, UiPaletteLabel)]));
        AddRR(OpCode.ADD, addr, tmp);

        if (stmt.Green == null && stmt.Blue == null)
        {
            // 两参形式：PALETTE idx, color —— QBasic 语义是"把显示色 color 赋给属性 idx"，
            // 也就是**把第 color 项复制到第 idx 项**（GORILLA.BAS 的 `PALETTE 4, 0` 就是它）。
            int val = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.Red, val);
            AddRI(OpCode.MOVE, tmp, 15);
            AddRR(OpCode.AND, val, tmp);
            AddRI(OpCode.MOVE, tmp, 4);
            AddRR(OpCode.MUL, val, tmp);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, tmp), new Operand(OperandType.LABEL, UiPaletteLabel)]));
            AddRR(OpCode.ADD, val, tmp);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, val), new Operand(OperandType.MEMORY, $"R{val}")]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R{addr}"), new Operand(OperandType.REGISTER, val)]));
            Regs.FreeInt(val, instructions);
        }
        else
        {
            // 三参形式：PALETTE idx, r, g, b（各 0-63）→ 0xAARRGGBB（0-63 乘 4 得 0-252，够用）
            int rr = Regs.AllocInt(instructions);
            int gg = Regs.AllocInt(instructions);
            int bb = Regs.AllocInt(instructions);
            EvalIntCoord(stmt.Red, rr);
            EvalIntCoord(stmt.Green, gg);
            EvalIntCoord(stmt.Blue, bb);
            AddRI(OpCode.MOVE, tmp, 4);
            AddRR(OpCode.MUL, rr, tmp);
            AddRR(OpCode.MUL, gg, tmp);
            AddRR(OpCode.MUL, bb, tmp);
            AddRI(OpCode.MOVE, tmp, 65536);
            AddRR(OpCode.MUL, rr, tmp);          // r << 16
            AddRI(OpCode.MOVE, tmp, 256);
            AddRR(OpCode.MUL, gg, tmp);          // g << 8
            AddRR(OpCode.ADD, rr, gg);
            AddRR(OpCode.ADD, rr, bb);
            AddRI(OpCode.MOVE, tmp, unchecked((int)0xFF000000));
            AddRR(OpCode.ADD, rr, tmp);          // 补不透明 alpha
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R{addr}"), new Operand(OperandType.REGISTER, rr)]));
            Regs.FreeInt(bb, instructions);
            Regs.FreeInt(gg, instructions);
            Regs.FreeInt(rr, instructions);
        }
        Regs.FreeInt(tmp, instructions);
        Regs.FreeInt(addr, instructions);
        UiLeave();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GET / PUT —— **没有映射到 ui_get_image / ui_put_image**（响亮告警，不静默）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// `GET` / `PUT` 在 UI 后端下**不生成任何代码**，只报一条编译期告警。
    ///
    /// <para>**为什么不做**（如实说，不粉饰）：QBasic 的 GET/PUT 靠一个**数组**装像素，
    /// 而宿主那套是**句柄**（<c>ui_get_image</c> 返回 ≥1 的句柄）。接起来要在编译期给每个
    /// 数组名分配一个"句柄槽"、把 GET 的结果存进去再让 PUT 取出来 —— 这一层没做。
    /// 更要紧的是 **PUT 的 `XOR`/`AND`/`OR` 三种方式宿主根本没有对应语义**
    /// （这正是任务里点名"映射不了要明确报不支持"的那一条）。</para>
    ///
    /// <para>**为什么不落回老路**：老路写 DOS 显存，在**每一个现有宿主**上都是空操作 ——
    /// 悄悄走下去等于让用户以为 GET/PUT 生效了。所以这里**发告警 + 跳过生成**：
    /// 编得过、但用户明确知道这一对语句没有任何效果。</para>
    /// </summary>
    void UiWarnGetPutUnsupported(string stmtName)
    {
        WarnUnimplemented($"{stmtName} —— UI 图形后端没有映射到宿主的 ui_get_image/ui_put_image（本语句无任何效果）");
        if (!_uiGetPutWarned)
        {
            _uiGetPutWarned = true;
            Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UnsupportedExpression,
                "GET/PUT 尚未映射到宿主图元：QBasic 用**数组**装像素、宿主那套用**句柄**，"
                + "这一层转换没做；而且 PUT 的 XOR/AND/OR 方式宿主没有对应语义。"
                + "本后端下 GET/PUT **不产生任何效果**（既不写显存也不画窗口）。"
                + "要旧行为（写 0xA0000，本平台没有宿主渲染）用 --basicgfx pcgfx。");
        }
    }

    bool _uiGetPutWarned;

    // ══════════════════════════════════════════════════════════════════════
    //  GET / PUT —— 宿主**图像句柄**那一套（syscall 584/585）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// `GET (x1,y1)-(x2,y2), arr` → <c>ui_get_image</c>，**句柄号存进 <c>arr(0)</c>**。
    ///
    /// <para><b>为什么是"存进数组"而不是"数组就是像素"</b>：QBasic 的 GET 把像素塞进一个
    /// 整型数组（<c>arr(0)=宽, arr(1)=高, arr(2+)=像素</c>），而宿主那套是**句柄**
    /// （`ui_get_image` 把那块光栅化结果存起来、返回一个 ≥1 的号）。两者无法逐字对应，
    /// 但用户看得见的那件事 —— "GET 下来、PUT 回去" —— 可以：把句柄当成"那块像素的唯一
    /// 标识"放进数组的第一个元素，PUT 再取出来。</para>
    /// <para>代价说清楚：<b>数组里除了 <c>arr(0)</c> 之外没有像素数据</b>。老程序若自己去看
    /// <c>arr(1)</c>/<c>arr(k)</c>（GORILLA.BAS 只看句柄），读到的是 0 —— 这是"不逐字等价"
    /// 的地方；所以它只在 UI 后端生效，`--basicgfx pcgfx` 仍走老的显存那条。</para>
    /// </summary>
    void UiEmitGetStatement(GetStatement stmt)
    {
        if (string.IsNullOrEmpty(stmt.ArrayName)) return;
        if (!IsKnownArray(stmt.ArrayName) && !variables.ContainsKey(stmt.ArrayName))
        {
            // 数组本身不认识 ⇒ 没有地方放句柄。响亮说一句，别静默丢掉。
            Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UndefinedArray,
                $"GET 的目标 '{stmt.ArrayName}' 不是已知数组 —— 这一条 GET 没有任何效果。");
            return;
        }

        // 记下"这个数组装的是句柄"—— `PUT` 的"来源是不是句柄"判据要用（见 `UiEmitPutStatement`）。
        _uiGetArrays.Add(stmt.ArrayName.ToLowerInvariant());

        UiEnter();
        // x = min(x1,x2)、y = min(y1,y2)、w = |x2-x1|+1、h = |y2-y1|+1
        //（QBasic 的 (x1,y1)-(x2,y2) 含两端 ⇒ 宽高 +1；反向画的那一维由 min/abs 兜住）
        int x1 = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.X1, x1);
        int y1 = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.Y1, y1);
        int x2 = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.X2, x2);
        int y2 = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.Y2, y2);
        int tmp = Regs.AllocInt(instructions);
        UiCall("ui_get_image",
            () => { EmitMin(tmp, x1, x2); AddRR(OpCode.MOVE, 0, tmp); },
            () => { EmitMin(tmp, y1, y2); AddRR(OpCode.MOVE, 0, tmp); },
            () => { EmitAbsDiff(tmp, x1, x2); AddRR(OpCode.MOVE, 0, tmp); },
            () => { EmitAbsDiff(tmp, y1, y2); AddRR(OpCode.MOVE, 0, tmp); });

        // 句柄在 R0 —— 先挪到 tmp（`EmitStaticAddr` 会借 R2），再写进 arr(0)
        AddRR(OpCode.MOVE, tmp, 0);
        if (GenerateArrayBaseAddr(stmt.ArrayName, 2))
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, tmp) }));
        }

        Regs.FreeInt(tmp, instructions);
        Regs.FreeInt(y2, instructions);
        Regs.FreeInt(x2, instructions);
        Regs.FreeInt(y1, instructions);
        Regs.FreeInt(x1, instructions);
        UiLeave();
    }

    /// <summary>
    /// `PUT (x,y), arr [,action]` → <c>ui_put_image(x, y, handle, mode)</c>，
    /// 句柄取自 <c>arr(0)</c>（= 上一次对同一数组做的 GET）。
    ///
    /// <para>方式映射：<c>PSET</c>（或缺省）→ COPY(0)；<c>XOR</c> → XOR(1)；
    /// <c>AND</c>/<c>OR</c>/<c>PRESET</c> → **响亮告警 + 不画**（宿主没有对应语义，
    /// 而"随便挑一个"会画出**错的画面** —— 那比不画更难查）。</para>
    /// </summary>
    void UiEmitPutStatement(PutStatement stmt)
    {
        if (string.IsNullOrEmpty(stmt.ArrayName)) return;
        if (!IsKnownArray(stmt.ArrayName) && !variables.ContainsKey(stmt.ArrayName))
        {
            Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UndefinedArray,
                $"PUT 的来源 '{stmt.ArrayName}' 不是已知数组 —— 这一条 PUT 没有任何效果。");
            return;
        }

        // ── 来源数组**从没被本程序的 GET 写过** ⇒ 它装的不是句柄，这条 PUT 画不出东西 ──────
        //
        // 判据是**静态的、在生成期维护的一张表**（`_uiGetArrays`，`UiEmitGetStatement` 往里记）。
        // 它挡不住"先 PUT 后 GET"这种乱序写法（那种情况这里不告警，行为与从前一样是"不画"），
        // 但**能挡住真正会踩的那一类**：老 BASIC 游戏把精灵位图**手打包在 `DATA` 里**、
        // 用 `READ` 灌进数组，然后直接 `PUT` —— 数组里第 0 项是那份 QBasic 位图块的首字
        // （GORILLA.BAS 的 `LBan&(0) = 458758`），**根本不是** `ui_get_image` 给的句柄。
        // 宿主按句柄查不到就什么都不画，而**一个错都不报** —— 实测整个游戏的香蕉全程不见，
        // 猩猩却好好的（它们走 `LINE` 画 + 真 `GET`/`PUT`）。
        //
        // 为什么不在这条路里顺手把"手工位图"解码画出来：那要在宿主侧认 **QBasic 的
        // GET/PUT 块格式**（EGA 逐位平面、每行 `ceil(w/8)*4` 字节、4 字节头两个 word 是
        // 宽高减一），并把它接成一个**新 syscall**（老号加参数就是静默的未定义行为）。
        // 那是另一件事，本轮没做 —— 所以这里**响亮地说出来**，不假装画了。
        if (!_uiGetArrays.Contains(stmt.ArrayName.ToLowerInvariant()))
        {
            if (!_uiPutNotFromGetWarned.Contains(stmt.ArrayName.ToLowerInvariant()))
            {
                _uiPutNotFromGetWarned.Add(stmt.ArrayName.ToLowerInvariant());
                Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UnsupportedExpression,
                    $"PUT 的来源 '{stmt.ArrayName}' 在本程序里**没有**被 GET 写过 ⇒ 它里面不是图像句柄。"
                    + "宿主 ui_put_image 只认 GET 给的句柄（本平台没有 DOS 显存，块内容在宿主侧保管），"
                    + "所以这一条 PUT **不会画出任何东西**。"
                    + "若这个数组是 `DATA` 里手打包的位图（老 BASIC 游戏的精灵写法），"
                    + "本平台暂不支持该格式 —— 请改用 GET/PUT 保存-贴回，或用 ui_rect 逐块画。");
            }
            WarnUnimplemented($"PUT 的来源 '{stmt.ArrayName}' 不是 GET 得到的句柄 —— 这一条 PUT 不会有任何画面"
                + "（宿主只认 ui_get_image 的返回值）");
            return;
        }

        string action = (stmt.Action ?? "").ToUpperInvariant();
        int mode;
        switch (action)
        {
            case "":
            case "PSET":
                mode = 0;                       // COPY：直接贴
                break;
            case "XOR":
                mode = 1;                       // 宿主原生支持（读目的像素做异或）
                break;
            default:
                // PRESET / AND / OR：宿主图元里没有对应语义。
                UiWarnPutActionUnsupported(action);
                return;
        }

        UiEnter();
        int handle = Regs.AllocInt(instructions);
        if (GenerateArrayBaseAddr(stmt.ArrayName, 2))
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, handle), new Operand(OperandType.MEMORY, "R2") }));
        }
        else
        {
            AddRI(OpCode.MOVE, handle, 0);
        }
        UiCall("ui_put_image",
            UiEval(stmt.X),
            UiEval(stmt.Y),
            () => AddRR(OpCode.MOVE, 0, handle),
            UiConst(mode));
        Regs.FreeInt(handle, instructions);
        UiLeave();
    }

    bool _uiPutActionWarned;

    /// <summary>被本程序的 `GET` 写过的数组名（小写）—— `PUT` 的"来源是不是句柄"判据。</summary>
    readonly HashSet<string> _uiGetArrays = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>已经就"来源不是 GET 句柄"告过警的数组名（每个数组只说一次）。</summary>
    readonly HashSet<string> _uiPutNotFromGetWarned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>PUT 的 AND/OR/PRESET 方式：宿主只认 COPY/XOR ⇒ 告警 + 不画（不猜一个近似的）。</summary>
    void UiWarnPutActionUnsupported(string action)
    {
        WarnUnimplemented($"PUT 的 {action} 方式 —— 宿主 ui_put_image 只有 COPY/XOR，本语句无任何效果");
        if (_uiPutActionWarned) return;
        _uiPutActionWarned = true;
        Diags.AddWarning("<basic>", CurrentSourceLine, 0, ErrorCode.CodeGen_UnsupportedExpression,
            $"PUT 的 {action} 方式本平台不支持：宿主 ui_put_image 只实现了 COPY（PSET）与 XOR 两种。"
            + "AND/OR/PRESET 需要逐位合成语义，乱挑一个会画出**错的画面**（比不画更难查），"
            + "所以这一条 PUT 被跳过并在此明确告警。");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  取编译期常量
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>字面量 / 已知常量 → int；否则 -1（表示"运行期才知道"）</summary>
    int TryConstInt(Expression e)
    {
        if (e is NumberLiteral nl) return (int)nl.Value;
        if (e is Identifier id && constants.TryGetValue(id.Name.ToLower(), out var v))
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
        }
        return -1;
    }
}
