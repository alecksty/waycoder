using CompilerBase;
using VMLAssembler;

namespace BasicCompiler;

/// <summary>
/// QBasic **文本语句**（PRINT / LOCATE / COLOR 的默认值 / CLS 的光标归位）的 UI 后端
/// —— 把图形模式下打在屏幕上的文字送进宿主的绘图窗口（<c>ui_text</c> + <c>ui_rect</c>）。
///
/// <para>**为什么必须做**：本后端此前只发了 <c>ui_circle/clear/ellipse/flood_fill/line/
/// pixel/present/rect/win_open_ex</c>，**没有 <c>ui_text</c>** ⇒ 开了窗口的程序里
/// `LOCATE n,m` / `PRINT` 仍然走命令行 CRT，窗口里一个字都没有。老游戏的标题、比分、
/// 输入回显全在 PRINT 上（GORILLA.BAS 的 <c>GorillaIntro</c> / <c>DoShot</c> /
/// <c>GetNum#</c> / <c>PlayGame</c> 都是 <c>LOCATE + PRINT</c>），所以"窗口是空的、
/// 文字全在终端里"。</para>
///
/// <para>**模型**：QBasic 图形模式下的文字是**字符格**——80×25（模式 9）/ 80×30（模式 12）/
/// 40×25（模式 1、7、13），格宽恒为 <b>8</b> 像素（四种模式下都是 <c>宽 ÷ 列数 = 8</c>），
/// 格高 = <c>高 ÷ 行数</c>（模式 9 是 14、模式 12 是 16、CGA 是 8）。于是：</para>
/// <list type="number">
///   <item>**光标**（行、列，1 起）存在本前端自己的 data 段变量里，`LOCATE` 就是写它两个；</item>
///   <item>`PRINT` **先把字符攒进一行缓冲**，行结束（或语句结束）时一次性
///         `ui_rect`（铺背景）+ `ui_text`（画字）+ `ui_present`；</item>
///   <item>换行 = 光标行 +1、列回 1；缓冲里的东西按**行首列**落笔。</item>
/// </list>
///
/// <para>**为什么攒一行而不是一个字符一次 <c>ui_text</c>**：字符级落笔要么自己算 x 步进
/// （与宿主的字形宽度、字距耦合，本仓在这条路上踩过"两把尺子"的坑），要么就得为每个字符
/// 付一次 syscall + 一次场景图元。攒成一行是**唯一既便宜又不依赖度量**的做法。</para>
///
/// <para>**为什么背景要铺**：QBasic 在图形模式下的文字输出会**填满整格**（字形用前景、
/// 其余用背景色）—— 这正是 `PRINT SPACE$(n)` 能"擦掉"旧内容的原因，而老程序大量靠它
/// 擦输入框（GORILLA 的 `DoShot`、`GetInputs`、`GetNum#` 都是）。只画字形的话
/// 擦除变成空操作，输入框里会留着上一次的数字。</para>
///
/// <para><b>命令行那份输出怎么办</b>：**图形模式下不再走 CRT**（见
/// <see cref="UiTextEmitPutChar"/> 里运行期的那次模式判断）。文本模式（`SCREEN 0`）一个字
/// 没变，仍是原来的 TTY 通道 —— 老程序的文字菜单（GORILLA 的 `Intro`）照旧显示在终端里。
/// 判据是**运行期**的模式字节 0x6FF0，不是编译期标志：`SCREEN Mode` 里 Mode 可以是变量，
/// 而且同一个 PRINT 语句可能在两种模式各执行一次。</para>
/// </summary>
public partial class CodeGenerator
{
    // ══════════════════════════════════════════════════════════════════════
    //  data 段状态
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>光标行（1 起；QBasic 的 LOCATE 就是写它）</summary>
    const string UiTxtRowLabel = "_ui_txt_row";
    /// <summary>光标列（1 起）</summary>
    const string UiTxtColLabel = "_ui_txt_col";
    /// <summary>行缓冲里已攒的字节数（0 = 这一行还没有待落笔的内容）</summary>
    const string UiTxtLenLabel = "_ui_txt_len";
    /// <summary>行缓冲本体（<c>int[64]</c> = 256 个零字节，零初始化正好当空串）</summary>
    const string UiTxtBufLabel = "_ui_txt_buf";
    /// <summary>字符格高（= 窗口高 ÷ 文本行数；模式 9 = 14、模式 12 = 16、CGA = 8）</summary>
    const string UiTxtCellHLabel = "_ui_txt_cellh";
    /// <summary>文本列数（模式 1/7/13 = 40，其余 80）—— 换行折行用</summary>
    const string UiTxtMaxColLabel = "_ui_txt_maxcol";
    /// <summary>文本行数（模式 9/1/7/13 = 25、模式 12 = 30）—— 行溢出钳制用</summary>
    const string UiTxtMaxRowLabel = "_ui_txt_maxrow";

    /// <summary>行缓冲容量（字节）。一个字符一个字节，够 GORILLA 那种 80 列的一行还有余量。</summary>
    const int UiTxtBufCap = 240;

    // ── 三个运行期子程序（发射在程序末尾，由 CALL 进入）────────────────────
    /// <summary>追加一个字符（R0 = 字符码）；换行 / 文本模式下直发通道都在里面</summary>
    const string UiTxtPutcLabel = "basic_ui_text_putc";
    /// <summary>追加一个 NUL 结尾串（R0 = 串地址）</summary>
    const string UiTxtPutsLabel = "basic_ui_text_puts";
    /// <summary>把行缓冲落在当前光标位置（+ 铺背景 + present）</summary>
    const string UiTxtFlushLabel = "basic_ui_text_flush";

    /// <summary>
    /// **文本模式下走 stdout 的那一对入口**（<c>CrtMode == false</c> 的调用点用）。
    ///
    /// <para>为什么要分两套入口：老代码的字符出口是
    /// <c>SYSCALL (CrtMode ? 400 : 4)</c>、串是 <c>(CrtMode ? 401 : 1)</c> ——
    /// **TTY 通道与 stdout 在宿主里是两条不同的流**（缓冲/刷新时机不同）。
    /// 图形模式改走窗口之后，如果文本模式一律用 TTY，控制台文字虽然一字不差，
    /// 但**流的次序会变**（实测：ANSI 转义与后续文本的相对位置变了，抓出一条探针红）。
    /// 判据是编译期的 <c>CrtMode</c>（与老代码同一个标志、同一个取值时机），
    /// 所以挑哪一套入口就在调用点定，运行期只再判"文本/图形"。</para>
    /// </summary>
    const string UiTxtPutcOutLabel = "basic_ui_text_putc_out";
    /// <summary>见 <see cref="UiTxtPutcOutLabel"/></summary>
    const string UiTxtPutsOutLabel = "basic_ui_text_puts_out";
    /// <summary>两个入口共用的攒行体</summary>
    const string UiTxtPutcUiLabel = "basic_ui_text_putc_ui";
    /// <summary>见 <see cref="UiTxtPutcUiLabel"/></summary>
    const string UiTxtPutsUiLabel = "basic_ui_text_puts_ui";

    /// <summary>编译期跟踪的"是否已经开过绘图窗口"（和 SCREEN 那套共用一条线索）</summary>
    bool _uiTextUsed;      // 有语句用到文本后端 ⇒ 末尾要发射三个子程序
    bool _uiTextVarsDone;  // data 段变量已建

    /// <summary>
    /// 文本网格（列数、行数）—— **按 QBasic 各模式的真实文本尺寸**，不是"一律 80×25"。
    ///
    /// <para>模式 12（640×480）的文本是 80×30 而不是 25：格高 16。写成 25 的话每行会多出
    /// 4 像素、第 25 行之后整片偏下，而 GORILLA 的比分正好在底部。</para>
    ///
    /// <para>模式 1/7/13 只有 40 列（320 像素宽 ÷ 40 = 8，与 80 列时的格宽相同）。</para>
    /// </summary>
    static (int Cols, int Rows) UiTextGridOf(int mode) => mode switch
    {
        1 or 7 or 13 => (40, 25),   // CGA 320×200 / EGA 320×200 / VGA 256 色
        9            => (80, 25),   // EGA 640×350
        2 or 8       => (80, 25),   // CGA 640×200 / EGA 640×200
        _            => (80, 30),   // 11/12 以及未知模式（640×480）
    };

    // ══════════════════════════════════════════════════════════════════════
    //  data 段变量
    // ══════════════════════════════════════════════════════════════════════

    void UiEnsureTextVars()
    {
        _uiTextUsed = true;
        if (_uiTextVarsDone) return;
        _uiTextVarsDone = true;
        if (!dataSection.ContainsKey(UiTxtRowLabel)) dataSection[UiTxtRowLabel] = 1;
        if (!dataSection.ContainsKey(UiTxtColLabel)) dataSection[UiTxtColLabel] = 1;
        if (!dataSection.ContainsKey(UiTxtLenLabel)) dataSection[UiTxtLenLabel] = 0;
        // int[64] = 256 个零字节。**必须是全零**：flush 靠 NUL 收尾、缓冲靠 len 判断空，
        // 两者都要求初值干净；用 `.string "   …"` 会带上"这串到底有没有被裁掉尾空格"的问题。
        if (!dataSection.ContainsKey(UiTxtBufLabel)) dataSection[UiTxtBufLabel] = new int[64];
        if (!dataSection.ContainsKey(UiTxtCellHLabel)) dataSection[UiTxtCellHLabel] = 16;
        if (!dataSection.ContainsKey(UiTxtMaxColLabel)) dataSection[UiTxtMaxColLabel] = 80;
        if (!dataSection.ContainsKey(UiTxtMaxRowLabel)) dataSection[UiTxtMaxRowLabel] = 25;
    }

    /// <summary>把光标归位并丢掉行缓冲（CLS / 开窗之后调）</summary>
    void UiTextResetCursor()
    {
        UiEnsureTextVars();
        AddRI(OpCode.MOVE, 1, 1);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, UiTxtRowLabel), new Operand(OperandType.REGISTER, 1)]));
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, UiTxtColLabel), new Operand(OperandType.REGISTER, 1)]));
        AddRI(OpCode.MOVE, 1, 0);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, UiTxtLenLabel), new Operand(OperandType.REGISTER, 1)]));
    }

    /// <summary>写 <paramref name="label"/>（int 槽）= 立即数</summary>
    void UiTextStoreConst(string label, int value)
    {
        AddRI(OpCode.MOVE, 1, value);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, label), new Operand(OperandType.REGISTER, 1)]));
    }

    /// <summary>
    /// 把"窗口尺寸"换算成文本网格：<c>cellh = H ÷ rows</c>、列数、行数三个常量。
    ///
    /// <para>**只能在开窗的地方调**（那儿才知道这次开的是什么模式的窗口）。
    /// 编译期已知模式就发常量；运行期模式走 <see cref="UiTextEmitGridChain"/>。</para>
    /// </summary>
    void UiTextApplyGridConst(int mode)
    {
        UiEnsureTextVars();
        var (cols, rows) = UiTextGridOf(mode);
        var (_, h) = UiResolutionOf(mode);
        UiTextStoreConst(UiTxtMaxColLabel, cols);
        UiTextStoreConst(UiTxtMaxRowLabel, rows);
        UiTextStoreConst(UiTxtCellHLabel, Math.Max(1, h / rows));
    }

    /// <summary>
    /// 运行期模式（R0 = SCREEN 模式）→ 写文本网格三项。
    /// 与 <see cref="UiTextGridOf"/> / <see cref="UiResolutionOf"/> 共用**同一张表**，
    /// 不另抄一份数字（按模式枚举、相同的 (cols, rows, cellh) 归并成一组比较）。
    /// </summary>
    void UiTextEmitGridChain()
    {
        UiEnsureTextVars();
        string chosen = newLabel();
        // 兜底 = 默认那档（未知模式按 640×480 的 80×30）
        var def = UiTextGridOf(-1);
        UiTextStoreConst(UiTxtMaxColLabel, def.Cols);
        UiTextStoreConst(UiTxtMaxRowLabel, def.Rows);
        UiTextStoreConst(UiTxtCellHLabel, Math.Max(1, 480 / def.Rows));

        var emitted = new HashSet<(int, int, int)>();
        foreach (int m in new[] { 1, 2, 7, 8, 9, 11, 12, 13 })
        {
            var (cols, rows) = UiTextGridOf(m);
            var (_, h) = UiResolutionOf(m);
            var key = (cols, rows, Math.Max(1, h / rows));
            if (!emitted.Add(key)) continue;
            var modes = new[] { 1, 2, 7, 8, 9, 11, 12, 13 }
                .Where(x => { var (c2, r2) = UiTextGridOf(x); var (_, h2) = UiResolutionOf(x);
                              return (c2, r2, Math.Max(1, h2 / r2)) == key; })
                .ToArray();
            string skip = newLabel();
            foreach (int mm in modes)
            {
                instructions.Add(new Instruction(OpCode.CMP,
                    [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, mm)]));
                instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, skip)]));
            }
            string next = newLabel();
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, next)]));
            AddLabel(skip);
            UiTextStoreConst(UiTxtMaxColLabel, key.Item1);
            UiTextStoreConst(UiTxtMaxRowLabel, key.Item2);
            UiTextStoreConst(UiTxtCellHLabel, key.Item3);
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, chosen)]));
            AddLabel(next);
        }
        AddLabel(chosen);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  字符 / 串出口（PRINT 那条链的落点）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 输出一个字符（R0 = 字符码）。**运行期按 SCREEN 模式分派**：
    /// 文本模式（0x6FF0 == 0）→ 原来的 TTY 通道（SYSCALL #400）；图形模式 → 攒进行缓冲。
    ///
    /// <para>分派点放在**子程序里**，所以每个调用点只多一条 `CALL`；而"当前是文本还是图形"
    /// 是**运行期**的事（`SCREEN Mode` 里的 Mode 可以是变量），编译期标志表达不了。</para>
    /// </summary>
    void UiTextEmitPutChar()
    {
        UiEnsureTextVars();
        instructions.Add(new Instruction(OpCode.CALL,
            [new Operand(OperandType.LABEL, CrtMode ? UiTxtPutcLabel : UiTxtPutcOutLabel)]));
    }

    /// <summary>输出一个 NUL 结尾串（R0 = 串地址）—— 同上，运行期分派</summary>
    void UiTextEmitPutString()
    {
        UiEnsureTextVars();
        instructions.Add(new Instruction(OpCode.CALL,
            [new Operand(OperandType.LABEL, CrtMode ? UiTxtPutsLabel : UiTxtPutsOutLabel)]));
    }

    /// <summary>把行缓冲落笔（语句末尾 / LOCATE 之前调）</summary>
    void UiTextEmitFlush()
    {
        UiEnsureTextVars();
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, UiTxtFlushLabel)]));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PRINT 的分隔符语义
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 一个表达式打完之后按分隔符收尾：
    /// <list type="bullet">
    ///   <item><c>';'</c> —— 紧跟其后：**立刻落笔**（不落笔的话后面等输入的循环里看不到字），
    ///         光标停在原处，下一个 PRINT 接着打。</item>
    ///   <item><c>','</c> —— 不换行，但光标跳到**下一个 14 列打印区**（QBasic 的 zone 宽度）。</item>
    ///   <item><c>'\0'</c> —— 此处结束：发一个换行（子程序里换行 = 落笔 + 行 +1、列回 1）。</item>
    /// </list>
    ///
    /// <para>**旧行为**是"每个 PRINT 语句末尾无条件补一个换行"，`PRINT "Angle:";` 也会换行
    /// —— 那既不是 QBasic 的语义，也把输入回显顶到了下一行。</para>
    /// </summary>
    void UiTextEmitSeparator(char sep)
    {
        switch (sep)
        {
            case ';':
                UiTextEmitFlush();
                break;

            case ',':
                UiTextEmitFlush();
                // col = ((col-1) / 14 + 1) * 14 + 1
                {
                    Ins(OpCode.MOVE, UiReg(1), UiLabelOp(UiTxtColLabel));
                    Ins(OpCode.MOVE, UiReg(1), UiMem("R1"));
                    Ins(OpCode.SUB, UiReg(1), UiImm(1));
                    Ins(OpCode.MOVE, UiReg(2), UiImm(14));
                    Ins(OpCode.DIV, UiReg(1), UiReg(2));
                    Ins(OpCode.ADD, UiReg(1), UiImm(1));
                    Ins(OpCode.MUL, UiReg(1), UiReg(2));
                    Ins(OpCode.ADD, UiReg(1), UiImm(1));
                    Ins(OpCode.MOVE, UiReg(2), UiLabelOp(UiTxtColLabel));
                    Ins(OpCode.MOVE, UiMem("R2"), UiReg(1));
                }
                break;

            default:
                AddRI(OpCode.MOVE, 0, 10);      // '\n'
                UiTextEmitPutChar();
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LOCATE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// `LOCATE row, col` —— 图形模式改**窗口里那个光标**；文本模式仍是 CRT 的 `CRT_GOTOXY`。
    ///
    /// <para>两个路径**都要发**（运行期二选一），因为同一个 <c>LOCATE</c> 语句可能在
    /// `SCREEN 0` 和 `SCREEN 9` 下各跑一次（老程序在图形/文本之间来回切）。</para>
    ///
    /// <para>换光标之前先**落笔**：光标是"这段文字画在哪儿"的一部分，先挪位置会把
    /// 已经攒好但还没画的那行画到新位置上去。</para>
    /// </summary>
    void UiEmitLocateStatement(LocateStatement stmt)
    {
        UiEnsureTextVars();

        string textPath = newLabel();
        string done = newLabel();

        // 文本模式 → 老路（CRT_GOTOXY(col, row)）
        UiStateAddr(2, UiModeLabel);
        instructions.Add(new Instruction(OpCode.MOVEB,
            [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2")]));
        instructions.Add(new Instruction(OpCode.CMP,
            [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, textPath)]));

        // 图形模式：落笔 → 写行、列（钳到 ≥1）
        UiTextEmitFlush();
        int rowReg = Regs.AllocInt(instructions);
        int colReg = Regs.AllocInt(instructions);
        EvalIntCoord(stmt.Row, rowReg);
        EmitClampMin1(rowReg);
        EvalIntCoord(stmt.Col, colReg);
        EmitClampMin1(colReg);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, UiTxtRowLabel), new Operand(OperandType.REGISTER, rowReg)]));
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, UiTxtColLabel), new Operand(OperandType.REGISTER, colReg)]));
        Regs.FreeInt(colReg, instructions);
        Regs.FreeInt(rowReg, instructions);
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, done)]));

        AddLabel(textPath);
        CrtMode = true;
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.Col, 0);   // x -> R0
            GenerateSubExpression(stmt.Row, 1);   // y -> R1
        }
        else
        {
            GenerateExpression(stmt.Col, 0);
            GenerateExpression(stmt.Row, 1);
        }
        EmitCallBuiltin("CRT_GOTOXY");

        AddLabel(done);
    }

    /// <summary><paramref name="reg"/> = max(reg, 1) —— `LOCATE 0` / 负列（居中算式会把长串算到 1 以下）不许落到缓冲外</summary>
    void EmitClampMin1(int reg)
    {
        string ok = newLabel();
        instructions.Add(new Instruction(OpCode.CMP,
            [new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, ok)]));
        AddRI(OpCode.MOVE, reg, 1);
        AddLabel(ok);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  三个运行期子程序的实体（发射在程序末尾）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 发射文本子程序。由 <c>CodeGenerator.cs</c> 在**所有 SUB/FUNCTION 之后**调一次。
    ///
    /// <para>为什么是子程序而不是内联：每个字符都内联一遍要 ~20 条指令 × 每个打印点，
    /// 而 <c>GenerateIntegerToString</c> 里每个数字就有好几个打印点。子程序还让
    /// "保存哪些寄存器"只写一遍（调用点在任意上下文里，R1/R2/R4 可能正拿着活值）。</para>
    /// </summary>
    void UiTextEmitHelpers()
    {
        if (!_uiTextUsed || _uiTextHelpersDone) return;
        _uiTextHelpersDone = true;
        UiEnsureTextVars();

        EmitUiTxtPutc();
        EmitUiTxtPuts();
        EmitUiTxtFlush();
    }

    bool _uiTextHelpersDone;

    static Operand UiReg(int n) => new(OperandType.REGISTER, n);
    static Operand UiImm(int n) => new(OperandType.IMMEDIATE, n);
    static Operand UiMem(string s) => new(OperandType.MEMORY, s);
    static Operand UiLabelOp(string s) => new(OperandType.LABEL, s);

    void Ins(OpCode op, params Operand[] ops) =>
        instructions.Add(new Instruction(op, new List<Operand>(ops)));

    /// <summary>子程序入口：把 R1..R9 全压栈（下面三段都用固定编号，靠这里保护调用方）</summary>
    void UiTxtPrologue()
    {
        for (int r = 1; r <= 9; r++) Ins(OpCode.PUSH, UiReg(r));
    }

    /// <summary>子程序出口：按相反顺序恢复</summary>
    void UiTxtEpilogue()
    {
        for (int r = 9; r >= 1; r--) Ins(OpCode.POP, UiReg(r));
        Ins(OpCode.RET);
    }

    /// <summary>
    /// 文本出口的**一个入口**：压 R1..R9 → 判 SCREEN 模式 →
    /// 文本模式就发 <paramref name="textSyscall"/> 并收尾，图形模式跳进共用的攒行体
    /// <paramref name="uiBody"/>（那一段负责 pop + RET，所以两条路都汇到同一个
    /// <paramref name="done"/>）。
    ///
    /// <para>两个入口只在 <paramref name="textSyscall"/> 上不同（TTY 400/401 与 stdout 4/1）
    /// —— 见 <see cref="UiTxtPutcOutLabel"/> 的说明。</para>
    /// </summary>
    void EmitUiTxtEntry(string entryLabel, int textSyscall, string done, string uiBody)
    {
        AddLabel(entryLabel);
        UiTxtPrologue();
        UiStateAddr(1, UiModeLabel);
        Ins(OpCode.MOVEB, UiReg(1), UiMem("R1"));
        Ins(OpCode.CMP, UiReg(1), UiImm(0));
        string uiPath = newLabel();
        Ins(OpCode.JNE, UiLabelOp(uiPath));
        Ins(OpCode.SYSCALL, UiImm(textSyscall));
        Ins(OpCode.JMP, UiLabelOp(done));
        AddLabel(uiPath);
        Ins(OpCode.JMP, UiLabelOp(uiBody));
    }

    /// <summary>
    /// `basic_ui_text_putc`：R0 = 字符码。
    /// <code>
    ///   模式 == 0        → 走文本出口（TTY #400 或 stdout #4，按编译期的 CrtMode 挑）
    ///   字符 == '\n'     → 落笔 + 行+1、列=1
    ///   其余            → 攒进行缓冲（控制字符丢掉；缓冲区满了丢掉）
    /// </code>
    /// </summary>
    void EmitUiTxtPutc()
    {
        string done = newLabel();

        // 两个入口：文本模式发哪个通道是**编译期**决定的（见 UiTxtPutcOutLabel 的说明），
        // 攒行体则是共用的一段 —— 两个入口都 push R1..R9 之后跳进去，由那一段统一 pop + RET。
        EmitUiTxtEntry(UiTxtPutcLabel, 400, done, UiTxtPutcUiLabel);
        EmitUiTxtEntry(UiTxtPutcOutLabel, 4, done, UiTxtPutcUiLabel);

        AddLabel(UiTxtPutcUiLabel);

        // 控制字符：换行（10）与回车（13）都当换行，其余（BEEP 的 7、TAB 的 9…）丢掉
        string append = newLabel();
        Ins(OpCode.CMP, UiReg(0), UiImm(10));
        Ins(OpCode.JE, UiLabelOp(append));        // 换行走同一段（下面按 R0 分两支）
        Ins(OpCode.CMP, UiReg(0), UiImm(13));
        string crOk = newLabel();
        Ins(OpCode.JE, UiLabelOp(crOk));
        Ins(OpCode.CMP, UiReg(0), UiImm(32));
        Ins(OpCode.JL, UiLabelOp(done));          // < 32 且不是 CR/LF ⇒ 丢
        AddLabel(crOk);

        AddLabel(append);
        string isNl = newLabel();
        Ins(OpCode.CMP, UiReg(0), UiImm(10));
        Ins(OpCode.JE, UiLabelOp(isNl));
        // CR：改写成 LF 再走换行那一支（13 也要换行）
        Ins(OpCode.CMP, UiReg(0), UiImm(13));
        string notNl = newLabel();
        Ins(OpCode.JNE, UiLabelOp(notNl));

        AddLabel(isNl);
        {
            // 落笔 + 行 +1 / 列 = 1（行溢出钳到最后一行：QBasic 会滚屏，这里没有滚屏语义）
            instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp(UiTxtFlushLabel)]));
            Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtRowLabel));
            Ins(OpCode.ADD, UiReg(1), UiImm(1));
            Ins(OpCode.MOVE, UiReg(2), UiMem(UiTxtMaxRowLabel));
            string rowOk = newLabel();
            Ins(OpCode.CMP, UiReg(1), UiReg(2));
            Ins(OpCode.JLE, UiLabelOp(rowOk));
            Ins(OpCode.MOVE, UiReg(1), UiReg(2));
            AddLabel(rowOk);
            Ins(OpCode.MOVE, UiMem(UiTxtRowLabel), UiReg(1));
            Ins(OpCode.MOVE, UiReg(1), UiImm(1));
            Ins(OpCode.MOVE, UiMem(UiTxtColLabel), UiReg(1));
            Ins(OpCode.JMP, UiLabelOp(done));
        }

        AddLabel(notNl);
        {
            // 折行：下一个字符落在"列 + 已攒长度"；超过列数就先落笔、再换到下一行行首。
            // ⚠ **换行之前必须落笔** —— 不然攒着的那半行会被直接丢掉（光标一格一格的模型里
            //   没有"回退"这东西），表现是长行折行处**少一截字**。
            Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtColLabel));
            Ins(OpCode.MOVE, UiReg(2), UiMem(UiTxtLenLabel));
            Ins(OpCode.ADD, UiReg(1), UiReg(2));
            Ins(OpCode.MOVE, UiReg(3), UiMem(UiTxtMaxColLabel));
            string noWrap = newLabel();
            Ins(OpCode.CMP, UiReg(1), UiReg(3));
            Ins(OpCode.JLE, UiLabelOp(noWrap));
            instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp(UiTxtFlushLabel)]));
            Ins(OpCode.MOVE, UiReg(1), UiImm(1));
            Ins(OpCode.MOVE, UiMem(UiTxtColLabel), UiReg(1));
            Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtRowLabel));
            Ins(OpCode.ADD, UiReg(1), UiImm(1));
            Ins(OpCode.MOVE, UiReg(3), UiMem(UiTxtMaxRowLabel));
            Ins(OpCode.CMP, UiReg(1), UiReg(3));
            string rowOk2 = newLabel();
            Ins(OpCode.JLE, UiLabelOp(rowOk2));
            Ins(OpCode.MOVE, UiReg(1), UiReg(3));
            AddLabel(rowOk2);
            Ins(OpCode.MOVE, UiMem(UiTxtRowLabel), UiReg(1));
            AddLabel(noWrap);

            // 缓冲还没满就追加：buf[len] = 字符；len++（flush 已把 len 清 0）
            Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtLenLabel));
            Ins(OpCode.CMP, UiReg(1), UiImm(UiTxtBufCap));
            Ins(OpCode.JGE, UiLabelOp(done));
            Ins(OpCode.MOVE, UiReg(2), UiLabelOp(UiTxtBufLabel));
            Ins(OpCode.ADD, UiReg(2), UiReg(1));
            Ins(OpCode.MOVEB, UiMem("R2"), UiReg(0));
            Ins(OpCode.ADD, UiReg(1), UiImm(1));
            Ins(OpCode.MOVE, UiMem(UiTxtLenLabel), UiReg(1));
        }

        AddLabel(done);
        UiTxtEpilogue();
    }

    /// <summary>
    /// `basic_ui_text_puts`：R0 = NUL 结尾串地址。
    /// 文本模式走**一次**串输出 syscall（<c>#401</c> 或 <c>#1</c>，按编译期的 <c>CrtMode</c> 挑，
    /// 与从前逐字相同 —— 不是逐字符循环：老的闪烁边框每帧打 80 字符，逐字符会把控制台输出放大 80 倍）；
    /// 图形模式逐字节喂给攒行体。
    /// </summary>
    void EmitUiTxtPuts()
    {
        string done = newLabel();
        EmitUiTxtEntry(UiTxtPutsLabel, 401, done, UiTxtPutsUiLabel);
        EmitUiTxtEntry(UiTxtPutsOutLabel, 1, done, UiTxtPutsUiLabel);

        AddLabel(UiTxtPutsUiLabel);
        Ins(OpCode.MOVE, UiReg(2), UiReg(0));      // R2 = 游标
        string loop = newLabel();
        AddLabel(loop);
        Ins(OpCode.MOVEB, UiReg(0), UiMem("R2"));
        Ins(OpCode.CMP, UiReg(0), UiImm(0));
        Ins(OpCode.JE, UiLabelOp(done));
        instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp(UiTxtPutcLabel)]));
        Ins(OpCode.ADD, UiReg(2), UiImm(1));
        Ins(OpCode.JMP, UiLabelOp(loop));

        AddLabel(done);
        UiTxtEpilogue();
    }

    /// <summary>
    /// 光标 → 像素：<c>R4 = (col-1)×8</c>（字符格宽恒为 8）、<c>R5 = (row-1)×格高</c>、
    /// <c>R7 = 格高</c>。**落在固定编号的寄存器里**（子程序里不能借 <see cref="Regs"/>），
    /// 所以调用方要么在 <c>ui_*</c> 之前用它、要么之后**重算一遍**（见 flush 里的说明）。
    /// </summary>
    void EmitUiTxtXY()
    {
        Ins(OpCode.MOVE, UiReg(4), UiMem(UiTxtColLabel));
        Ins(OpCode.SUB, UiReg(4), UiImm(1));
        Ins(OpCode.MUL, UiReg(4), UiImm(8));
        Ins(OpCode.MOVE, UiReg(5), UiMem(UiTxtRowLabel));
        Ins(OpCode.SUB, UiReg(5), UiImm(1));
        Ins(OpCode.MOVE, UiReg(7), UiMem(UiTxtCellHLabel));
        Ins(OpCode.MUL, UiReg(5), UiReg(7));
    }

    /// <summary>
    /// `basic_ui_text_flush`：把行缓冲按 (row, col) 落笔。
    /// <code>
    ///   x = (col-1) * 8          y = (row-1) * cellh
    ///   ui_rect(x, y, len*8, cellh, bg, fill=1, lw=1, round=0)   ← 整格铺背景（PRINT SPACE$ 的擦除靠它）
    ///   ui_text(x, y, buf, fg, cellh, anchor=0)
    ///   ui_present()                                             ← 不画这一下，等输入的循环里看不到字
    ///   col += len;  len = 0
    /// </code>
    /// 文本模式直接返回（字符/串在 putc/puts 里已经直发了）。
    /// </summary>
    void EmitUiTxtFlush()
    {
        AddLabel(UiTxtFlushLabel);
        UiTxtPrologue();

        string done = newLabel();

        UiStateAddr(1, UiModeLabel);
        Ins(OpCode.MOVEB, UiReg(1), UiMem("R1"));
        Ins(OpCode.CMP, UiReg(1), UiImm(0));
        Ins(OpCode.JE, UiLabelOp(done));

        Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtLenLabel));   // R1 = len
        Ins(OpCode.CMP, UiReg(1), UiImm(0));
        Ins(OpCode.JLE, UiLabelOp(done));

        // buf[len] = 0
        Ins(OpCode.MOVE, UiReg(2), UiLabelOp(UiTxtBufLabel));
        Ins(OpCode.ADD, UiReg(2), UiReg(1));
        Ins(OpCode.MOVEB, UiMem("R2"), UiImm(0));

        // R4 = x, R5 = y, R6 = w, R7 = cellh
        EmitUiTxtXY();
        Ins(OpCode.MOVE, UiReg(6), UiReg(1));
        Ins(OpCode.MUL, UiReg(6), UiImm(8));

        // ── 背景格：ui_rect(x, y, w, h, 背景色, fill=1, lw=1, round=0) ──
        //    参数右到左压栈（与 UiCall 同一约定），最后一个是 x 落在 [R12+12]
        Ins(OpCode.MOVE, UiReg(0), UiImm(0));
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.MOVE, UiReg(0), UiImm(1));
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.MOVE, UiReg(0), UiImm(1));
        Ins(OpCode.PUSH, UiReg(0));
        UiStateAddr(0, UiBgLabel);
        Ins(OpCode.MOVEB, UiReg(0), UiMem("R0"));
        UiTranslateColorInR0(8);                 // R8 = 暂存（我们自己的固定编号）
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.PUSH, UiReg(7));
        Ins(OpCode.PUSH, UiReg(6));
        Ins(OpCode.PUSH, UiReg(5));
        Ins(OpCode.PUSH, UiReg(4));
        instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp("ui_rect")]));
        Ins(OpCode.ADD, UiReg(13), UiImm(32));

        // ⚠ **x/y/格高必须重算**：`ui_*` 的包装函数把实参读进 **R0–R7** 再发 syscall
        //   （`Lib/shared/vmlui.vml` 的 `move @R4 [@R12+28]` 那一串），所以调用返回后
        //   R4/R5/R7 里躺着的是 **`ui_rect` 自己的参数**（色、填充、圆角）。
        //   实测症状极有指向性：文字画在 `x=0xFF000000`（= 刚刚那个背景色）、
        //   `y=1`（= 填充开关）、`size=0`（= 圆角）—— 位置全错、字号 0 ⇒ **一个字都看不见**，
        //   而背景格是对的（它用的是压栈前的值），看上去像"文字没画"。
        EmitUiTxtXY();

        // ── 文字：ui_text_v(x, y, buf, 前景色, cellh, anchor=0, valign=vtop, style=0) ──
        //
        // ⚠ **一律用带竖对齐的 #581，且要 `vtop`（3）**：宿主的文字契约是"**y 就是基线**"
        //   （`TextVOffset` 的 `base` 档 = 老行为），而这里算出来的 y 是**字符格的顶**。
        //   差一个"上升"（≈0.8×字号）看着像"文字整体往上跑了一截"。
        //   换算交给宿主那一档做（它内部有一处 `TextAscentRatio`）—— **别在这里自己乘 0.8**：
        //   那就成了"同一规则两处实现"，将来宿主换了比例，这里就对不上了。
        Ins(OpCode.MOVE, UiReg(0), UiImm(0));       // style
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.MOVE, UiReg(0), UiImm(3));       // valign = VML_VANCHOR_TOP
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.MOVE, UiReg(0), UiImm(0));       // anchor = 左
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.PUSH, UiReg(7));                 // size = cellh
        UiStateAddr(0, UiFgLabel);
        Ins(OpCode.MOVEB, UiReg(0), UiMem("R0"));
        UiTranslateColorInR0(8);
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.MOVE, UiReg(0), UiLabelOp(UiTxtBufLabel));
        Ins(OpCode.PUSH, UiReg(0));
        Ins(OpCode.PUSH, UiReg(5));
        Ins(OpCode.PUSH, UiReg(4));
        instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp("ui_text_v")]));
        Ins(OpCode.ADD, UiReg(13), UiImm(32));

        // present：与图形语句一样"一条语句执行完就该看见"
        instructions.Add(new Instruction(OpCode.CALL, [UiLabelOp("ui_present")]));

        // col += len; len = 0
        Ins(OpCode.MOVE, UiReg(1), UiMem(UiTxtLenLabel));
        Ins(OpCode.MOVE, UiReg(2), UiMem(UiTxtColLabel));
        Ins(OpCode.ADD, UiReg(2), UiReg(1));
        Ins(OpCode.MOVE, UiMem(UiTxtColLabel), UiReg(2));
        Ins(OpCode.MOVE, UiReg(1), UiImm(0));
        Ins(OpCode.MOVE, UiMem(UiTxtLenLabel), UiReg(1));

        AddLabel(done);
        UiTxtEpilogue();
    }
}
