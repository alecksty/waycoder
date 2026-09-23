using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    void GenerateLocateStatement(LocateStatement stmt)
    {
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
        // Store at text dimension addresses (0x6FF6=cols, 0x6FFA=rows)
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
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FF6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FFA) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
    }
    void GenerateDrawStatement(DrawStatement stmt)
    {
        // Generate inline VML code that parses the DRAW command string at runtime
        // Store draw state at fixed addresses:
        //   0x6FA0: current X
        //   0x6FA4: current Y
        //   0x6FA8: current color
        //   0x6FAC: scale factor
        //   0x6FB0: angle

        // Initialize draw state
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FA0) }));
        AddRI(OpCode.MOVE, 0, 160); // X center
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FA4) }));
        AddRI(OpCode.MOVE, 0, 100); // Y center
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FA8) }));
        AddRI(OpCode.MOVE, 0, 15); // white
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FAC) }));
        AddRI(OpCode.MOVE, 0, 1); // scale = 1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FB0) }));
        AddRI(OpCode.MOVE, 0, 0); // angle = 0
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

        // Load DRAW string address into R2
        // Store string in data section and emit LEA to load address
        if (stmt.DrawString is StringLiteral strLit)
        {
            string drawStrLabel = NewLabel("drawstr");
            dataSection[drawStrLabel] = new DataString(strLit.Value);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, drawStrLabel) }));
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
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawNumLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumEnd) }));

        // Apply scale factor from 0x6FAC
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0x6FAC) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));

        // Save current X, Y for drawing line
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0x6FA0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") })); // oldX
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0x6FA4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") })); // oldY
        // Save old position for line drawing (R4,R5 will be modified by dispatch)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 4) })); // oldX backup
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5) })); // oldY backup

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
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // D: Y += n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawD) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // L: X -= n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawL) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // R: X += n
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawR) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // E: X += n, Y -= n (up-right)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawE) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // F: X += n, Y += n (down-right)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawF) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // G: X -= n, Y += n (down-left)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawG) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // H: X -= n, Y -= n (up-left)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawH) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // C: set color
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawC) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0x6FA8) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R6") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        // M: absolute move (x,y)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawM) }));
        // For M, R1 has the first number (x), need to parse comma and second number (y)
        // Check for + or - prefix for relative move
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 44) })); // ','
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) })); // skip ','

        // For now: treat M as absolute: set X = R1, parse Y next
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) })); // set X

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
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 48) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drawNumY) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawNumYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) })); // set Y

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drawOther) }));

        // Draw line from (oldX, oldY) to (newX, newY)
        // Load oldX, oldY from R4, R5; newX, newY from 0x6FA0, 0x6FA4
        // Store newX, newY
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FA0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FA4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R1") }));

        // Draw line from old(R6,R7) to new(R4,R5) with R1 steps
        // Save step count to R8 (R1 may be used below)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 1) })); // steps=R1
        // Compute direction: R9=sign(R4-R6), R10=sign(R5-R7)
        string drDxDone = newLabel(), drDyDone = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        string drDxPos = newLabel();
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, drDxPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, -1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDxPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, drDxDone) }));
        // dy sign
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 7) }));
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

        // Compute VRAM addr for (R6=x, R7=y): addr = VgaBase + (R7*width + R6)*bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) })); // y
        EmitLoadScreenWidth(1);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6) })); // +x
        EmitLoadScreenBpp(1);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, VgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
        // Write white RGB pixel (3 bytes)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 255) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));

        // Step toward target
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 9) })); // x+=dx_sign
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 10) })); // y+=dy_sign
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
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 4) }));
        // Check mode: if mode 13 (bpp == 1), use QB_PALETTE13_ADDR; else use QB_PALETTE_ADDR
        string palMode13 = newLabel();
        string palEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0x6FF3) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, palMode13) }));
        // Non-mode-13: store at QB_PALETTE_ADDR
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, QB_PALETTE_ADDR) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R4") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, palEnd) }));
        // Mode 13: store at QB_PALETTE13_ADDR
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, palMode13) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, QB_PALETTE13_ADDR) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
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
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) })); // w
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 1) }));
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
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 12) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, baseAddr) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R6") })); // arr(0)=w
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R6") })); // arr(1)=h

        // Save x1→R8, y1→R9, load bpp→R1 before loop
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 1) }));
        EmitLoadScreenBpp(1); // R1 = bpp (1 for indexed, 3 for RGB)
        // Loop: for row=0 to h-1, for col=0 to w-1
        string getYLoop = newLabel(); string getYEnd = newLabel();
        string getXLoop = newLabel(); string getXEnd = newLabel();
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 4) })); // R6 at arr(2)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) })); // row=0
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getYLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, getYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) })); // col=0
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getXLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, getXEnd) }));
        // addr = VgaBase + ((y1+row)*width + (x1+col)) * bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 9) })); // y1
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 7) }));
        EmitLoadScreenWidth(3);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 8) })); // x1
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 1) })); // *bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, VgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 3) }));
        // Read pixel: if bpp==3 read 3 bytes and pack RGB, else read 1 byte
        string getDoneLbl = newLabel(), getRgbLbl = newLabel();
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // B or index
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, getRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R6") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, getDoneLbl) }));
        // RGB: read G and R, pack (R<<16)|(G<<8)|B
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, getRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) })); // R2=B
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // G
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R11") })); // R
        instructions.Add(new Instruction(OpCode.SHL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 16) }));
        instructions.Add(new Instruction(OpCode.SHL, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2) }));
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
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, baseAddr) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) })); // R2 → pixel data
        bool isXor = stmt.Action == "XOR";
        // Save x→R8, y→R9, load bpp→R1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 1) }));
        EmitLoadScreenBpp(1); // R1=bpp
        string putYLoop = newLabel(), putYEnd = newLabel();
        string putXLoop = newLabel(), putXEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putYLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, putYEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putXLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, putXEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.MEMORY, "R2") }));
        // Compute addr: VgaBase + ((y+row)*width + (x+col)) * bpp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 9) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 7) }));
        EmitLoadScreenWidth(6);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, VgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 6) }));
        // Write pixel by bpp
        string putDoneLbl = newLabel(), putRgbLbl = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, putRgbLbl) }));
        if (isXor) {
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R12") }));
            instructions.Add(new Instruction(OpCode.XOR, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 11) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R12") }));
        } else {
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.MEMORY, "R12") }));
        }
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, putDoneLbl) }));
        // RGB: unpack (R<<16|G<<8|B), use R0=G (NOT R5 which holds height!)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, putRgbLbl) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 11) }));
        instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 255) })); // B
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 11) }));
        instructions.Add(new Instruction(OpCode.SHR, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 255) })); // G (R0, not R5!)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 11) }));
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

    /// <summary>Emit code to load actual screen width from SCREEN-set variable (0x6FE0) into register reg</summary>
    private void EmitLoadScreenWidth(int reg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0x6FE0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }

    /// <summary>Emit code to load actual screen height from SCREEN-set variable (0x6FE4) into register reg</summary>
    private void EmitLoadScreenHeight(int reg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0x6FE4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }

    /// <summary>Emit code to load bytes-per-pixel (BPP) from SCREEN-set variable (0x6FF3) into register reg</summary>
    private void EmitLoadScreenBpp(int reg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0x6FF3) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
    }
}
