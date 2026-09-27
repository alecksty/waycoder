using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    void GenerateLocateStatement(LocateStatement stmt)
    {
        // UI 图形后端：图形模式下 LOCATE 改的是**窗口里那个光标**，文本模式仍是 CRT_GOTOXY。
        // 判据是运行期的 SCREEN 模式字节（`SCREEN Mode` 的 Mode 可以是变量），见那个实现。
        if (UiGfx) { UiEmitLocateStatement(stmt); return; }

        CrtMode = true;  // 激活 CRT 模式
        // LOCATE: ANSI CRT terminal — CRT_GOTOXY(col, row), both 1-based
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
    }

    void GenerateQbColorStatement(QbColorStatement stmt)
    {
        if (UiGfx) { UiEmitColorStatement(stmt); return; }
        CrtMode = true;  // 激活 CRT 模式
        // COLOR: ANSI CRT terminal — CRT_TEXTCOLOR(fg) + CRT_TEXTBACKGROUND(bg)
        if (currentSubName != null)
            GenerateSubExpression(stmt.Foreground, 0);
        else
            GenerateExpression(stmt.Foreground, 0);
        EmitCallBuiltin("CRT_TEXTCOLOR");

        if (stmt.HasBackground)
        {
            if (currentSubName != null)
                GenerateSubExpression(stmt.Background, 0);
            else
                GenerateExpression(stmt.Background, 0);
            EmitCallBuiltin("CRT_TEXTBACKGROUND");
        }
    }
    void GenerateQbWidthStatement(QbWidthStatement stmt)
    {
        // WIDTH: set text columns/rows (NOT pixel resolution, which is set by SCREEN)
        //
        // ⚠ UiGfx（默认）走文字层**真正在读的那三个槽**；老 PcGfx 路保持原样。
        if (UiGfx)
        {
            UiEmitWidthStatement(stmt);
            return;
        }
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.Cols, 0);
            GenerateSubExpression(stmt.Rows, 1);
        }
        else
        {
            GenerateExpression(stmt.Cols, 0);
            GenerateExpression(stmt.Rows, 1);
        }
        SysAddr(2, Sys.TextCols);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2") }));
        SysAddr(2, Sys.TextRows);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
    }
    /// <summary>
    /// 把一个编译期常量存进 `DRAW` 的运行期状态槽（`Sys.Draw*`）。
    ///
    /// <para>⚠ 存在的唯一理由是**别再写反操作数顺序**：这个动作原先在
    /// <see cref="GenerateDrawStatement"/> 里手抄了 5 遍，5 遍全是 `MOVE R0, [R1]`（读）。
    /// 收成一个函数之后，方向只写一次。</para>
    /// </summary>
    void DrawStore(string sysSlot, int value)
    {
        SysAddr(1, sysSlot);                        // R1 = &槽
        AddRI(OpCode.MOVE, 0, value);               // R0 = 值
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0) }));  // [R1] = R0
    }

    void GenerateDrawStatement(DrawStatement stmt)
    {
        // Generate inline VML code that parses the DRAW command string at runtime
        // Store draw state in the系统全局变量（见 SysVars）:
        //   Sys.DrawX: current X
        //   Sys.DrawY: current Y
        //   Sys.DrawColor: current color
        //   Sys.DrawScale: scale factor
        //   Sys.DrawAngle: angle

        // Initialize draw state
        //
        // ⚠⚠ **操作数方向全部写反了**（v0.96.501 修）：原来是
        //     `(REGISTER 0, MEMORY "R1")` = `MOVE R0, [R1]` —— 那是**读**，
        //     把槽里的初值（0）读进 R0，刚设好的 160/100/15/1/0 **一个都没存进去**。
        //   后果连锁：`DrawScale` 恒为 0 ⇒ 步数被 `MUL R1, 0` 归零 ⇒ 每条命令画 0 像素。
        //   （本仓在 `UiGfx.cs` 的 `GenerateQbColorStatement` 那里专门写了长注释讲这条：
        //     「`MOVEB [R1], R0` 才是"存"」—— 这里是同一个坑的第 N 次。）
        DrawStore(Sys.DrawX, 160);      // 起点（QBasic 默认在屏幕中心附近）
        DrawStore(Sys.DrawY, 100);
        DrawStore(Sys.DrawColor, 15);   // 默认白
        DrawStore(Sys.DrawScale, 1);    // 比例 1
        DrawStore(Sys.DrawAngle, 0);

        // Load DRAW string address into R2
        // Store string in data section and emit LEA to load address
        //
        // ⚠⚠ **操作数类型必须是 `LABEL` 不是 `IMMEDIATE`**（v0.96.501 修）：
        //   写成 `IMMEDIATE` 时那个标签**不会被汇编器登记成符号**，而 `dataSection` 里
        //   那一项也就不出现在产物里 —— 生成的 `move R2, #drawstr_2` 指向一个
        //   **不存在的标签**，读到的是内存 0 处的字节（0）⇒ 解析循环第一次比较
        //   `cb == 0` 就判定"串结束"，**整个 DRAW 一条线都不画**。
        //   ⚠ 而且**不报错**：汇编器对未定义标签不吭声（实测 `grep drawstr` 只有一个"用"、没有"定义"）。
        //   本仓其它 6 处字符串字面量（`Expressions.cs:107`、`Statements.IO.cs:248`、
        //   `Misc.cs:155/233`…）用的都是 `OperandType.LABEL` —— 只有这一处不一样。
        if (stmt.DrawString is StringLiteral strLit)
        {
            string drawStrLabel = NewLabel("drawstr");
            dataSection[drawStrLabel] = new DataString(strLit.Value);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.LABEL, drawStrLabel) }));
        }
        else if (currentSubName != null)
            GenerateSubExpression(stmt.DrawString, 2);
        else
            GenerateExpression(stmt.DrawString, 2);

        // DRAW parse loop
        string drawLoop = newLabel();
        string drawEnd = newLabel();

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawLoop) }));
        // Load current command char
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2") }));
        // Check for null terminator
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawEnd) }));

        // Skip spaces
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32) }));
        string drawNotSpace = newLabel();
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, drawNotSpace) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNotSpace) }));

        // Dispatch on command character (R0 holds char)
        // U=85, D=68, L=76, R=82, E=69, F=70, G=71, H=72, C=67, M=77
        // Advance past command char
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));

        // Parse numeric argument into R1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
        string drawNumLoop = newLabel();
        string drawNumEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumLoop) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, drawNumEnd) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 57) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drawNumEnd) }));
        // digit found
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 10) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 1), RegOf(OpCode.MUL, 1, 4) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 1), RegOf(OpCode.ADD, 1, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawNumLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumEnd) }));

        // Apply scale factor from Sys.DrawScale
        SysAddr(4, Sys.DrawScale);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 1), RegOf(OpCode.MUL, 1, 4) }));

        // ⚠ **步数必须在这里就存下来**（v0.96.501 修）：R1 此刻是"缩放后的步数"，
        //   而下面的分派分支会把它用掉/覆盖（`C` 分支改成颜色、`M` 分支改成 X 坐标，
        //   方向分支拿它做加减）。原先代码在"画线"那一段才取步数，取到的已经是
        //   `R1 = &Sys.DrawY` 这个**地址** ⇒ 步数恒为 DrawY 的值（初值 0）
        //   ⇒ 每条命令只画 1 个像素，而且画在起点上（DRAW 完全看不出效果）。
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 8), RegOf(OpCode.MOVE, 1, 1) })); // steps = R1

        // Save current X, Y for drawing line
        SysAddr(4, Sys.DrawX);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") })); // oldX
        SysAddr(5, Sys.DrawY);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") })); // oldY
        // Save old position for line drawing (R4,R5 will be modified by dispatch)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 6), RegOf(OpCode.MOVE, 1, 4) })); // oldX backup
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 7), RegOf(OpCode.MOVE, 1, 5) })); // oldY backup

        // Dispatch based on command char (in R0)
        // U: Y -= n
        string drawU = newLabel();
        string drawD = newLabel();
        string drawL = newLabel();
        string drawR = newLabel();
        string drawE = newLabel();
        string drawF = newLabel();
        string drawG = newLabel();
        string drawH = newLabel();
        string drawC = newLabel();
        string drawM = newLabel();
        string drawOther = newLabel();

        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 85) })); // 'U'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawU) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 68) })); // 'D'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawD) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 76) })); // 'L'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawL) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 82) })); // 'R'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawR) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 69) })); // 'E'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawE) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 70) })); // 'F'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawF) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 71) })); // 'G'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawG) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 72) })); // 'H'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawH) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 67) })); // 'C'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawC) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 77) })); // 'M'
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drawM) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));

        // U: Y -= n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawU) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 5), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // D: Y += n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawD) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 5), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // L: X -= n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawL) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 4), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // R: X += n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawR) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 4), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // E: X += n, Y -= n (up-right)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawE) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 4), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 5), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // F: X += n, Y += n (down-right)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawF) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 4), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 5), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // G: X -= n, Y += n (down-left)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawG) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 4), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 5), RegOf(OpCode.ADD, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // H: X -= n, Y -= n (up-left)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawH) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 4), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 5), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // C: set color
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawC) }));
        // ⚠ **不能借 R6 当"取地址"的暂存**（v0.96.501 修）：R6/R7 是**老位置的 X/Y**，
        //   下面"画这一段"要用它们当起点。原来写的是 `SysAddr(6, …)` ⇒ R6 被改成
        //   `&Sys.DrawColor` 这个**内存地址**（实测 1402），于是 `DRAW "C4 …"` 会先画一条
        //   `(1402,100)→(160,100)` 的**横贯屏幕的假线** —— 而且**不报错**，只是画面上多一条。
        //   R11 在那个时刻是空的（R0 命令字符 / R1 数值 / R2 串游标 / R3 数字暂存 /
        //   R4,R5 新位置 / R6,R7 老位置 / R8 步数 / R9,R10 方向 / R12 帧指针 / R13 栈）。
        SysAddr(11, Sys.DrawColor);
        // ⚠ 方向是**存**（`[R11] = R1`）：R1 此刻是刚从串里解析出来的那个数字。
        //   原来写的是 `MOVE R1, [R11]`（**读**）⇒ `C` 命令实际上什么也没设，
        //   而且顺手把 R1 冲成旧的 DrawColor —— 同一个"存取方向写反"的坑，这是第 6 处。
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R11"), new Operand(OperandType.REGISTER, 1) }));
        // ⚠ `C` 是**设色**不是**移动** ⇒ 设完就回去解析下一条命令，**不该画**。
        //   （原来落进 drawOther 画了一条零长度的线：UiGfx 下白跑一次 `ui_line`，
        //     老 PcGfx 下白画一个像素。）
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawLoop) }));
        // M: absolute move (x,y)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawM) }));
        // For M, R1 has the first number (x), need to parse comma and second number (y)
        // Check for + or - prefix for relative move
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 44) })); // ','
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) })); // skip ','

        // For now: treat M as absolute: set X = R1, parse Y next
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 4), RegOf(OpCode.MOVE, 1, 1) })); // set X

        // Parse Y value
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
        string drawNumY = newLabel();
        string drawNumYEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumY) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, drawNumYEnd) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 57) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drawNumYEnd) }));
        AddRI(OpCode.MOVE, 0, 10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 1), RegOf(OpCode.MUL, 1, 0) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 1), RegOf(OpCode.ADD, 1, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawNumY) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 5), RegOf(OpCode.MOVE, 1, 1) })); // set Y

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));

        // 到此为止：R6/R7 = 老位置、R4/R5 = 新位置（分派分支算出来的）、R8 = 步数。
        //
        // ⚠⚠ **这里原来是两次"读"**（v0.96.501 修）：写的是
        //     `SysAddr(1, Sys.DrawX); MOVE R4, [R1]` —— 那是把**表里的旧值读进 R4**，
        //     刚由分派算出来的新位置**当场被覆盖掉**。
        //   而注释写的是"Store newX, newY"（说明当初的意图就是存），
        //   指令方向却反了 —— 正是本仓记过的那个坑（`MOVEB [R1], R0` 才是"存"，
        //   见 `UiGfx.cs` 里 `GenerateQbColorStatement` 那段长注释）。
        //   后果：DRAW 的位置**永远不前进**（每条命令都从同一点再画一次），
        //   而且 `R8`（步数）在那之后被赋成 `R1 = &Sys.DrawY` 这个**地址** ⇒ 步数恒为 0。
        //   两个缺陷叠在一起，表现就是"DRAW 什么都不画"。
        SysAddr(1, Sys.DrawX);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 4) }));
        SysAddr(1, Sys.DrawY);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 5) }));

        // ── UI 后端：一条线段 = 一次 `ui_line`（手机/桌面那扇绘图窗口）────────────
        //
        // ⚠ **不要另写一套 DRAW 语法解析**：上面那个运行期解析器照旧是唯一一份，
        //   这里只是在"把这一段画出来"这个动作上分岔（与 PSET/LINE/CIRCLE 同一个形状）。
        //
        // ⚠ **只保护 R2/R4/R5**，不是整套 `UiClobbered`：
        //   · R2 = DRAW 串的游标，是这条循环里**唯一必须活过这次调用**的值；
        //   · R4/R5 = 新位置，调用回来还要拿它们去更新游标（其实已存进 .data，
        //     但顺手保住更省心）；
        //   · R6–R10 用完即弃（下一轮命令会从 .data 重新装载）。
        //   用 `UiEnter()`/`UiLeave()` 会连 R6–R10 一起存取、还**每条线段 present 一次**。
        if (UiGfx)
        {
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 4) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 5) }));

            int dcolor = 12;    // 借一个不在 R2/R4/R5/R6..R10 里的寄存器当暂存
            UiCall("ui_line",
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6)])),   // x1 = 老 X
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7)])),   // y1 = 老 Y
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)])),   // x2 = 新 X
                () => instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5)])),   // y2 = 新 Y
                // 颜色：把 `Sys.DrawColor` 里的**调色板索引**换成 0xAARRGGBB。
                // ⚠ 原来这段硬编码 `255,255,255`（白），`DRAW "C4 …"` 设定的颜色
                //   **从头到尾没被用过** —— 也是"设了没反应"。
                () => { SysAddr(dcolor, Sys.DrawColor);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R{dcolor}")]));
                        UiTranslateColorInR0(dcolor); },
                UiConst(1));    // 线宽

            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 5) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 4) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawLoop) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawEnd) }));
            return;
        }

        // ── 老 PcGfx 后端：Bresenham 逐像素写帧缓冲（本平台的宿主都不渲染它）──────
        // Compute direction: R9=sign(R4-R6), R10=sign(R5-R7)
        string drDxDone = newLabel(), drDyDone = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 4), RegOf(OpCode.CMP, 1, 6) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        string drDxPos = newLabel();
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drDxPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, -1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDxPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        // dy sign
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 5), RegOf(OpCode.CMP, 1, 7) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drDyDone) }));
        string drDyPos = newLabel();
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drDyPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, -1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drDyDone) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDyPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDyDone) }));

        // Draw loop: R8 steps, start at (R6,R7), step by (R9,R10)
        // If steps==0, draw at least 1 pixel
        string drLoop = newLabel(), drEnd = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drLoop) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 1) })); // draw at least 1
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, drEnd) }));

        // Compute VRAM addr for (R6=x, R7=y): addr = 帧缓冲基址 + (R7*width + R6)*bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 0), RegOf(OpCode.MOVE, 1, 7) })); // y
        EmitLoadScreenWidth(1);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 0), RegOf(OpCode.MUL, 1, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 0), RegOf(OpCode.ADD, 1, 6) })); // +x
        EmitLoadScreenBpp(1);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 0), RegOf(OpCode.MUL, 1, 1) }));
        FbBase(1);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 0), RegOf(OpCode.ADD, 1, 1) }));
        // Write white RGB pixel (3 bytes)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 255) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));

        // Step toward target
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 6), RegOf(OpCode.ADD, 1, 9) })); // x+=dx_sign
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 7), RegOf(OpCode.ADD, 1, 10) })); // y+=dy_sign
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 1) })); // steps--
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drEnd) }));

        // Continue parsing next command in DRAW string
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawEnd) }));
    }
    void GeneratePaletteStatement(PaletteStatement stmt)
    {
        if (UiGfx) { UiEmitPaletteStatement(stmt); return; }
        // PALETTE color_index, red, green, blue (0-63 each)
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.ColorIndex, 0);
            GenerateSubExpression(stmt.Red, 1);
            GenerateSubExpression(stmt.Green, 2);
            GenerateSubExpression(stmt.Blue, 3);
        }
        else
        {
            GenerateExpression(stmt.ColorIndex, 0);
            GenerateExpression(stmt.Red, 1);
            GenerateExpression(stmt.Green, 2);
            GenerateExpression(stmt.Blue, 3);
        }
        // Scale 0-63 to 0-255: multiply by 4
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 1), RegOf(OpCode.MUL, 1, 4) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 2), RegOf(OpCode.MUL, 1, 4) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 3), RegOf(OpCode.MUL, 1, 4) }));
        // Check mode: if mode 13 (bpp == 1), use Sys.Palette256; else use Sys.Palette16
        string palMode13 = newLabel();
        string palEnd = newLabel();
        SysAddr(4, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, palMode13) }));
        // Non-mode-13: 颜色表基址 = Sys.Palette16
        SysAddr(4, Sys.Palette16);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 4), RegOf(OpCode.ADD, 1, 0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, palEnd) }));
        // Mode 13: 颜色表基址 = Sys.Palette256
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, palMode13) }));
        SysAddr(4, Sys.Palette256);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 4), RegOf(OpCode.ADD, 1, 0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, palEnd) }));
    }

    // ==================== GET (Graphics Array) ====================

    void GenerateGetStatement(GetStatement stmt)
    {
        if (UiGfx) { UiEmitGetStatement(stmt); return; }
        // GET (x1,y1)-(x2,y2), arrayname
        // Store pixel data to array: arr(0)=width, arr(1)=height, arr(2+)=pixels
        if (string.IsNullOrEmpty(stmt.ArrayName)) return;
        EmitSaveRegisters(4, 5, 6, 7, 8, 9, 10, 11);
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.X1, 0);
            GenerateSubExpression(stmt.Y1, 1);
            GenerateSubExpression(stmt.X2, 2);
            GenerateSubExpression(stmt.Y2, 3);
        }
        else
        {
            GenerateExpression(stmt.X1, 0);
            GenerateExpression(stmt.Y1, 1);
            GenerateExpression(stmt.X2, 2);
            GenerateExpression(stmt.Y2, 3);
        }
        // R0=x1, R1=y1, R2=x2, R3=y2
        // Compute width = x2 - x1 + 1, height = y2 - y1 + 1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 4), RegOf(OpCode.MOVE, 1, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 4), RegOf(OpCode.SUB, 1, 0) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) })); // w
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 5), RegOf(OpCode.MOVE, 1, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { RegOf(OpCode.SUB, 0, 5), RegOf(OpCode.SUB, 1, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) })); // h

        // Get array base address
        string arrName = stmt.ArrayName.ToLower();
        int arrOffset = 0;
        if (arrayVariables.ContainsKey(arrName))
            arrOffset = arrayVariables[arrName].Offset * 4;
        else if (variables.ContainsKey(arrName))
            arrOffset = variables[arrName] * 4;
        int baseAddr = 8 + arrOffset;

        // Store width, height in arr(0), arr(1)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 6), RegOf(OpCode.MOVE, 1, 12) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, baseAddr) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R6") })); // arr(0)=w
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R6") })); // arr(1)=h

        // Save x1→R8, y1→R9, load bpp→R1 before loop
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 8), RegOf(OpCode.MOVE, 1, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 9), RegOf(OpCode.MOVE, 1, 1) }));
        EmitLoadScreenBpp(1); // R1 = bpp (1 for indexed, 3 for RGB)
        // Loop: for row=0 to h-1, for col=0 to w-1
        string getYLoop = newLabel(); string getYEnd = newLabel();
        string getXLoop = newLabel(); string getXEnd = newLabel();
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 4) })); // R6 at arr(2)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) })); // row=0
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getYLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 7), RegOf(OpCode.CMP, 1, 5) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, getYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) })); // col=0
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getXLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 10), RegOf(OpCode.CMP, 1, 4) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, getXEnd) }));
        // addr = 帧缓冲基址 + ((y1+row)*width + (x1+col)) * bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 11), RegOf(OpCode.MOVE, 1, 9) })); // y1
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 11), RegOf(OpCode.ADD, 1, 7) }));
        EmitLoadScreenWidth(3);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 11), RegOf(OpCode.MUL, 1, 3) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 3), RegOf(OpCode.MOVE, 1, 8) })); // x1
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 3), RegOf(OpCode.ADD, 1, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 11), RegOf(OpCode.ADD, 1, 3) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 11), RegOf(OpCode.MUL, 1, 1) })); // *bpp
        FbBase(3);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 11), RegOf(OpCode.ADD, 1, 3) }));
        // Read pixel: if bpp==3 read 3 bytes and pack RGB, else read 1 byte
        string getDoneLbl = newLabel(), getRgbLbl = newLabel();
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // B or index
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, getRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R6") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, getDoneLbl) }));
        // RGB: read G and R, pack (R<<16)|(G<<8)|B
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 2), RegOf(OpCode.MOVE, 1, 0) })); // R2=B
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // G
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 3), RegOf(OpCode.MOVE, 1, 0) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // R
        instructions.Add(new Instruction(OpCode.SHL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 16) }));
        instructions.Add(new Instruction(OpCode.SHL, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 0), RegOf(OpCode.ADD, 1, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 0), RegOf(OpCode.ADD, 1, 2) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R6") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getDoneLbl) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, getXLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getXEnd) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, getYLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getYEnd) }));
        EmitRestoreRegisters(4, 5, 6, 7, 8, 9, 10, 11);
    }

    // ==================== PUT (Graphics Array) ====================

    void GeneratePutStatement(PutStatement stmt)
    {
        if (UiGfx) { UiEmitPutStatement(stmt); return; }
        // PUT (x,y), arrayname, action — bpp-aware pixel write
        // 保护 BP、坐标寄存器、临时寄存器
        EmitSaveRegisters(3, 6, 8, 9, 12);
        if (currentSubName != null)
        { GenerateSubExpression(stmt.X, 0); GenerateSubExpression(stmt.Y, 1); }
        else
        { GenerateExpression(stmt.X, 0); GenerateExpression(stmt.Y, 1); }
        string arrName = stmt.ArrayName.ToLower();
        int arrOffset = 0;
        if (arrayVariables.ContainsKey(arrName)) arrOffset = arrayVariables[arrName].Offset * 4;
        else if (variables.ContainsKey(arrName)) arrOffset = variables[arrName] * 4;
        int baseAddr = 8 + arrOffset;
        // Load width, height, then advance R2 past header to pixel data
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 2), RegOf(OpCode.MOVE, 1, 12) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, baseAddr) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) })); // R2 → pixel data
        bool isXor = stmt.Action == "XOR";
        // Save x→R8, y→R9, load bpp→R1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 8), RegOf(OpCode.MOVE, 1, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 9), RegOf(OpCode.MOVE, 1, 1) }));
        EmitLoadScreenBpp(1); // R1=bpp
        string putYLoop = newLabel(), putYEnd = newLabel();
        string putXLoop = newLabel(), putXEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putYLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 7), RegOf(OpCode.CMP, 1, 5) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, putYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putXLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { RegOf(OpCode.CMP, 0, 10), RegOf(OpCode.CMP, 1, 4) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, putXEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.MEMORY, "R2") }));
        // Compute addr: 帧缓冲基址 + ((y+row)*width + (x+col)) * bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 12), RegOf(OpCode.MOVE, 1, 9) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 12), RegOf(OpCode.ADD, 1, 7) }));
        EmitLoadScreenWidth(6);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 12), RegOf(OpCode.MUL, 1, 6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 6), RegOf(OpCode.MOVE, 1, 8) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 6), RegOf(OpCode.ADD, 1, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 12), RegOf(OpCode.ADD, 1, 6) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { RegOf(OpCode.MUL, 0, 12), RegOf(OpCode.MUL, 1, 1) }));
        FbBase(6);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { RegOf(OpCode.ADD, 0, 12), RegOf(OpCode.ADD, 1, 6) }));
        // Write pixel by bpp
        string putDoneLbl = newLabel(), putRgbLbl = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, putRgbLbl) }));
        if (isXor) {
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R12") }));
            instructions.Add(new Instruction(OpCode.XOR, new List<Operand> { RegOf(OpCode.XOR, 0, 6), RegOf(OpCode.XOR, 1, 11) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R12") }));
        } else {
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.MEMORY, "R12") }));
        }
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, putDoneLbl) }));
        // RGB: unpack (R<<16|G<<8|B), use R0=G (NOT R5 which holds height!)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 3), RegOf(OpCode.MOVE, 1, 11) }));
        instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 255) })); // B
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 0), RegOf(OpCode.MOVE, 1, 11) }));
        instructions.Add(new Instruction(OpCode.SHR, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 255) })); // G (R0, not R5!)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { RegOf(OpCode.MOVE, 0, 6), RegOf(OpCode.MOVE, 1, 11) }));
        instructions.Add(new Instruction(OpCode.SHR, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 16) })); // R
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R12") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R12") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R12") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putDoneLbl) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, putXLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putXEnd) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, putYLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putYEnd) }));
        EmitRestoreRegisters(3, 6, 8, 9, 12);
    }

    /// <summary>Emit code to load actual screen width from the SCREEN-set variable (Sys.ScreenWidth) into register reg</summary>
    private void EmitLoadScreenWidth(int reg)
    {
        SysAddr(reg, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }

    /// <summary>Emit code to load actual screen height from the SCREEN-set variable (Sys.ScreenHeight) into register reg</summary>
    private void EmitLoadScreenHeight(int reg)
    {
        SysAddr(reg, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }

    /// <summary>Emit code to load bytes-per-pixel (BPP) from the SCREEN-set variable (Sys.ScreenBpp) into register reg</summary>
    private void EmitLoadScreenBpp(int reg)
    {
        SysAddr(reg, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }
}
