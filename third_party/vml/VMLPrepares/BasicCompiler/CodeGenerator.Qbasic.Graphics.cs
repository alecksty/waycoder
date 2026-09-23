using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    void GenerateScreenStatement(ScreenStatement stmt)
    {
        // UI 后端：整条换掉（两条分派路自动同时生效，见 CodeGenerator.Qbasic.UiGfx.cs）
        if (UiGfx) { UiEmitScreenStatement(stmt); return; }
        // Extract screen mode value directly for reliable code generation
        int screenMode = -1;
        if (stmt.Mode is NumberLiteral nl)
            screenMode = (int)nl.Value;
        else if (stmt.Mode is Identifier id && constants.TryGetValue(id.Name.ToLower(), out var constVal) && constVal is int constInt)
            screenMode = constInt;

        if (screenMode >= 0)
        {
            AddRI(OpCode.MOVE, 0, screenMode);
        }
        else
        {
            // Runtime expression: evaluate mode into R0
            if (currentSubName != null)
                GenerateSubExpression(stmt.Mode, 0);
            else
                GenerateExpression(stmt.Mode, 0);
        }
        // Store mode at fixed address (byte value)
        SysAddr(1, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

        // 运行时模式: 如果模式是变量, 从 Sys.ScreenMode 重新加载
        if (screenMode < 0)
        {
            SysAddr(1, Sys.ScreenMode);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        }

        // Check if mode == 13
        string notMode13 = newLabel();
        string endScreen = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 13) }));
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, notMode13) }));

        // === Mode 13 setup ===
        // Set width = 320 进 Sys.ScreenWidth
        AddRI(OpCode.MOVE, 0, 320);
        SysAddr(1, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Set height = 200 进 Sys.ScreenHeight
        AddRI(OpCode.MOVE, 0, 200);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Set vga_mode = 13 (VGA 256-color) 进 Sys.ScreenMode
        AddRI(OpCode.MOVE, 0, 13);
        SysAddr(1, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Set bpp = 1 进 Sys.ScreenBpp
        AddRI(OpCode.MOVE, 0, 1);
        SysAddr(1, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Clear framebuffer
        GenerateClearFramebuffer13();
        // Initialize 256-entry palette
        GenerateInitPalette256();
        // Set default text dimensions: 40 cols, 25 rows (320/8=40, 200/8=25)
        AddRI(OpCode.MOVE, 0, 40);
        SysAddr(1, Sys.TextCols);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 25);
        SysAddr(1, Sys.TextRows);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endScreen) }));

        // Non-mode-13 path: set up width/height/bpp based on SCREEN mode
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notMode13) }));

        // Check mode-specific parameters
        // mode 0 (text): 80x25, bpp=2 (text cells), set width/height to 80x25
        string notMode0 = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, notMode0) }));
        // Mode 0: text mode
        AddRI(OpCode.MOVE, 0, 80);
        SysAddr(1, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 25);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 2);
        SysAddr(1, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Set default text dimensions: 80 cols, 25 rows
        AddRI(OpCode.MOVE, 0, 80);
        SysAddr(1, Sys.TextCols);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 25);
        SysAddr(1, Sys.TextRows);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endScreen) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notMode0) }));
        // Modes 1-12: graphics modes, set bpp=1 (索引色, 1字节/像素)
        AddRI(OpCode.MOVE, 0, 1);
        SysAddr(1, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Resolution table by SCREEN mode:
        // Mode 1,7: 320x200 | Mode 2,8: 640x200 | Mode 9,10: 640x350 | Mode 11,12: 640x480
        string mode7or13 = newLabel(), mode2or8 = newLabel(), mode9up = newLabel(), setDim = newLabel();
        SysAddr(1, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // If mode >= 9: high-res
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 9) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, mode9up) }));
        // Check mode 2 or 8 (640x200)
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, mode2or8) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, mode2or8) }));
        // Modes 1,7: 320x200
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, mode7or13) }));
        AddRI(OpCode.MOVE, 0, 320);
        SysAddr(1, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 200);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, setDim) }));
        // Modes 2,8: 640x200
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, mode2or8) }));
        AddRI(OpCode.MOVE, 0, 640);
        SysAddr(1, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 200);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, setDim) }));
        // Modes 9+: determine height by mode
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, mode9up) }));
        AddRI(OpCode.MOVE, 0, 640);
        SysAddr(1, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Reload mode to check height: mode 11,12 → 480; mode 9,10 → 350
        SysAddr(1, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 11) }));
        string mode11up = newLabel();
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, mode11up) }));
        // Modes 9,10: 640x350
        AddRI(OpCode.MOVE, 0, 350);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, setDim) }));
        // Modes 11,12: 640x480
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, mode11up) }));
        AddRI(OpCode.MOVE, 0, 480);
        SysAddr(1, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        // Set default text dimensions: 40 cols, 25 rows (320/8=40, 200/8=25)
        AddRI(OpCode.MOVE, 0, 40);
        SysAddr(1, Sys.TextCols);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 25);
        SysAddr(1, Sys.TextRows);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, setDim) }));
        GenerateInitPalette();

        // 初始化默认前景色 15 (亮白) 和背景色 0 (黑)
        AddRI(OpCode.MOVE, 0, 15);
        SysAddr(1, Sys.FgIndex);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        AddRI(OpCode.MOVE, 0, 0);
        SysAddr(1, Sys.BgIndex);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endScreen) }));
    }

    void GenerateClsStatement()
    {
        if (UiGfx) { UiEmitClsStatement(); return; }
        // CLS: clear screen based on current mode
        // Check screen mode from Sys.ScreenMode
        string clsText = newLabel();
        string clsEnd = newLabel();

        SysAddr(0, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, clsText) }));

        // Graphics mode: clear framebuffer（基址见 FbBase / Sys.Framebuffer）
        // R0 = 0 (fill value), R1 = 帧缓冲基址, R2 = size
        AddRI(OpCode.MOVE, 0, 0);
        FbBase(1);
        // Size = width * height * bpp, load from config
        SysAddr(2, Sys.ScreenWidth);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2") })); // width
        SysAddr(3, Sys.ScreenHeight);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R3") })); // height
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) })); // width*height
        SysAddr(3, Sys.ScreenBpp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R3") })); // bpp
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) })); // total bytes
        // Clear loop
        string gfxClsLoop = newLabel(), gfxClsEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, gfxClsLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, gfxClsEnd) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, gfxClsLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, gfxClsEnd) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, clsEnd) }));

        // Text mode (ANSI CRT): clear via ANSI escape, cursor home
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, clsText) }));
        CrtMode = true;  // 激活 CRT 模式, PRINT 走 TTY 通道
        EmitCallBuiltin("CRT_CLRSCR");
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, clsEnd) }));

        // End label (shared by graphics + text paths)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, clsEnd) }));
    }

    void GenerateInitPalette()
    {
        // Standard EGA 16-color palette (R, G, B)
        int[] pr = [0, 0, 0, 0, 170, 170, 170, 170, 85, 85, 85, 85, 255, 255, 255, 255];
        int[] pg = [0, 0, 170, 170, 0, 0, 85, 170, 85, 85, 255, 255, 85, 85, 255, 255];
        int[] pb = [0, 170, 0, 170, 0, 170, 0, 170, 85, 255, 85, 255, 85, 255, 0, 255];

        for (int i = 0; i < 16; i++)
        {
            // 表项地址 = 调色板基址 + i*3。基址是**标签**（`.data` 段），所以先取址再 ADD 偏移
            // —— `ADD reg, #<标签>` 运行时不认（见 SysVars 的说明）。
            int off = i * 3;
            AddRI(OpCode.MOVE, 0, pr[i]);
            SysAddr(1, Sys.Palette16);
            if (off != 0) AddRI(OpCode.ADD, 1, off);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            AddRI(OpCode.MOVE, 0, pg[i]);
            AddRI(OpCode.ADD, 1, 1);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            AddRI(OpCode.MOVE, 0, pb[i]);
            AddRI(OpCode.ADD, 1, 1);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        }
    }

    void GenerateClearFramebuffer13()
    {

string loop = newLabel();
        string loopEnd = newLabel();
        AddRI(OpCode.MOVE, 0, 0);
        FbBase(1);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 64000) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, loopEnd) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopEnd) }));
    }

    void GenerateInitPalette256()
    {
        // Standard VGA 256-color palette
        int[] pr16 = [0, 0, 0, 0, 170, 170, 170, 170, 85, 85, 85, 85, 255, 255, 255, 255];
        int[] pg16 = [0, 0, 170, 170, 0, 0, 85, 170, 85, 85, 255, 255, 85, 85, 255, 255];
        int[] pb16 = [0, 170, 0, 170, 0, 170, 0, 170, 85, 255, 85, 255, 85, 255, 0, 255];

        for (int i = 0; i < 256; i++)
        {
            int r, g, b;
            if (i < 16)
            {
                r = pr16[i]; g = pg16[i]; b = pb16[i];
            }
            else if (i < 232)
            {
                int idx = i - 16;
                r = ((idx / 36) % 6) * 51;
                g = ((idx / 6) % 6) * 51;
                b = (idx % 6) * 51;
            }
            else
            {
                int gray = (i - 232) * 11 + 8;
                r = gray; g = gray; b = gray;
            }
            // 与 `GenerateInitPalette` 同形：基址取标签，偏移用 ADD 加。
            int off = i * 3;
            AddRI(OpCode.MOVE, 0, r);
            SysAddr(1, Sys.Palette256);
            if (off != 0) AddRI(OpCode.ADD, 1, off);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            AddRI(OpCode.MOVE, 0, g);
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            AddRI(OpCode.MOVE, 0, b);
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
        }
    }

    void GeneratePsetStatement(PsetStatement stmt)
    {
        if (UiGfx) { UiEmitPsetStatement(stmt); return; }
        // Default color to bright white (15) if not specified
        int colorIndex = 15;
        if (stmt.Color is NumberLiteral cnl)
            colorIndex = (int)cnl.Value;

        // Evaluate: x, y, color_index into R0, R1, R2
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.X, 0);
            GenerateSubExpression(stmt.Y, 1);
        }
        else
        {
            GenerateExpression(stmt.X, 0);
            GenerateExpression(stmt.Y, 1);
        }
        // Set color directly
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, colorIndex) }));

        string mode13Label = newLabel();
        string modeEndLabel = newLabel();

        EmitGfxCheckBpp(mode13Label, 5);

        // === Non-mode-13 path: use native coords, palette lookup, 3 bytes ===
        // No coordinate scaling — PSET coordinates are already in native resolution.
        // (Scaling was only needed for adapted games using SCREEN 9 coords in SCREEN 7.)

            // Look up color in palette: addr = 调色板基址 + color_index * 3
            EmitLoadScreenBpp(5);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 5) }));
        SysAddr(5, Sys.Palette16);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 5) }));

        // Load R, G, B from palette
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R2") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R2") }));

        // Write pixel: 帧缓冲基址 + (y * 320 + x) * 3
        // path: R6 是这一带的临时寄存器（刚做完 bpp 的乘法，已经用完了）—— 复用它装基址。
        EmitLoadScreenWidth(6);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            EmitLoadScreenBpp(6);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 6) }));
        FbBase(6);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 6) }));

        // Bounds check: skip pixel write if address outside framebuffer
        string psetSkip = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, psetSkip) }));
        FbBasePlus(6, 1024 * 1024);   // 上界（老代码是「固定地址 + 1MB」）
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, psetSkip) }));

        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R1") }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, psetSkip) }));

        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, modeEndLabel) }));

        // === Mode 13 path: write 1 byte (color index), no scaling ===
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, mode13Label) }));
            EmitLoadScreenWidth(5);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
        FbBase(5);   // R5 是上面那个乘法的临时寄存器，用完即取基址
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R1") }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, modeEndLabel) }));
    }

    void GenerateQbLineStatement(QbLineStatement stmt)
    {
        if (UiGfx) { UiEmitLineStatement(stmt); return; }
        // Eval+PUSH coords onto stack, then POP to R0-R3, then eval color into R4.
        // Critical: POP coords BEFORE color eval (color eval may use R1-R3 as temps).
        int r0 = Regs.AllocInt(instructions);
        GenerateExpr(stmt.X1, r0);  instructions.Add(new Instruction(OpCode.PUSH, [new(OperandType.REGISTER, r0)])); Regs.FreeInt(r0, instructions);
        int r1 = Regs.AllocInt(instructions);
        GenerateExpr(stmt.Y1, r1);  instructions.Add(new Instruction(OpCode.PUSH, [new(OperandType.REGISTER, r1)])); Regs.FreeInt(r1, instructions);
        int r2 = Regs.AllocInt(instructions);
        GenerateExpr(stmt.X2, r2);  instructions.Add(new Instruction(OpCode.PUSH, [new(OperandType.REGISTER, r2)])); Regs.FreeInt(r2, instructions);
        int r3 = Regs.AllocInt(instructions);
        GenerateExpr(stmt.Y2, r3);  instructions.Add(new Instruction(OpCode.PUSH, [new(OperandType.REGISTER, r3)])); Regs.FreeInt(r3, instructions);
        // POP coords FIRST (before color eval clobbers registers)
        instructions.Add(new Instruction(OpCode.POP, [new(OperandType.REGISTER, 3)])); // Y2
        instructions.Add(new Instruction(OpCode.POP, [new(OperandType.REGISTER, 2)])); // X2
        instructions.Add(new Instruction(OpCode.POP, [new(OperandType.REGISTER, 1)])); // Y1
        instructions.Add(new Instruction(OpCode.POP, [new(OperandType.REGISTER, 0)])); // X1
        // Now evaluate color into R4 (R0-R3 already have coords)
        GenerateExpr(stmt.Color, 4);

        // 保护所有内部使用的寄存器 R4-R14 (R0-R3 已在上面恢复为坐标值)
        EmitSaveRegisters(4, 5, 6, 7, 8, 9, 10, 11, 14);

        // Handle box mode (B / BF) first — has its own mode 13 dispatch
        if (stmt.Box)
        {
            string boxMode13 = newLabel();
            string boxModeEnd = newLabel();
            EmitGfxCheckBpp(boxMode13, 5);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 4) }));
            if (stmt.Fill)
            {
                GenerateQbBoxFill(stmt);
            }
            else
            {
                GenerateQbBoxOutline(stmt);
            }
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, boxModeEnd) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, boxMode13) }));
            if (stmt.Fill)
            {
                GenerateQbBoxFillMode13(stmt);
            }
            else
            {
                GenerateQbBoxOutlineMode13(stmt);
            }
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, boxModeEnd) }));
            // 恢复寄存器后返回
            EmitRestoreRegisters(4, 5, 6, 7, 8, 9, 10, 11, 14);
            return;
        }

        string qbLineMode13Label = newLabel();
        string qbLineEndLabel = newLabel();
        string qbLineHighResLabel = newLabel();
        EmitGfxCheckBpp(qbLineMode13Label, 5);

        // For high-res modes (SCREEN 9+, native 640+ wide), skip coordinate scaling
        SysAddr(5, Sys.ScreenMode);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 9) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbLineHighResLabel) }));

        // bpp=1 indexed: color IS the palette index, no lookup needed
        // R5=R6=R7=R4 (color index for pixel writes)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 4) }));

        // Now R5=R, R6=G, R7=B, R0=x1, R1=y1, R2=x2, R3=y3
        // For simplicity, use VGADRAWLINE-style inline Bresenham
        // Store color regs somewhere safe
        // bpp=1: color IS the index (R4 unchanged from expression eval)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 4) }));

        // Save start/end (use R14 for x2 to preserve R9=G color component)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 0) })); // x1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 2) })); // x2 in R14

        // Calculate dx = |x2 - x1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0) }));
        string lAbsDx = newLabel();
        string eAbsDx = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lAbsDx) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lAbsDx) }));

        // Calculate dy = |y2 - y1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        string lAbsDy = newLabel();
        string eAbsDy = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lAbsDy) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lAbsDy) }));

        // Line drawing via linear interpolation (step-based approach)
        // For now, draw line by connecting points using step-based approach
        string lineLoop = newLabel();
        string lineEnd = newLabel();

        // Determine steps (max of dx, dy)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 7) }));
        string lSteps = newLabel();
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, lSteps) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lSteps) }));

        // Save y1 in R14 (preserved)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 1) })); // saved_y1 in R14
        
        // Draw each point along the line
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) })); // step counter
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lineLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lineEnd) }));

        // Compute current point: x = x1 + (x2-x1) * step / steps, y = y1 + (y2-y1) * step / steps
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 11) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 11) })); // cx

        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 14) })); // use saved_y1 (R14)
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 14) })); // cy = y1 + dy*step/steps

        // Write pixel at (cx, cy)
        // R5 = cx, R6 = cy
        string lineSkipPx = newLabel();

        // Coordinate bounds check: skip if x<0 or x>=width or y<0 or y>=height
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, lineSkipPx)]));
        EmitLoadScreenWidth(0);
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0)]));
        instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, lineSkipPx)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 6), new(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, lineSkipPx)]));
        EmitLoadScreenHeight(0);
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 0)]));
        instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, lineSkipPx)]));

        // Compute VRAM address: addr = 帧缓冲基址 + (cy * width + cx) * bpp
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 6)])); // R0 = cy
        EmitLoadScreenWidth(1);
        instructions.Add(new Instruction(OpCode.MUL, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 1)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 5)])); // +cx
        EmitLoadScreenBpp(1);
        instructions.Add(new Instruction(OpCode.MUL, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 1)]));
        FbBase(1);   // R1 是上面那两个乘法的临时寄存器，用完即取基址
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 1)]));

        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R0") })); // bpp=1: 1 byte per pixel

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lineSkipPx) }));

        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, lineLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lineEnd) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbLineEndLabel) }));

        // === Mode 13 LINE path: no scaling, 1-byte pixel writes ===
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbLineMode13Label) }));
        // R0=x1, R1=y1, R2=x2, R3=y2, R4=color_index

        // Handle BF (Box Fill) for mode 13
        if (stmt.Fill)
        {
            GenerateQbBoxFillMode13(stmt);
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbLineEndLabel) }));
        }
        else if (stmt.Box)
        {
            GenerateQbBoxOutlineMode13(stmt);
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbLineEndLabel) }));
        }

        // Save start and end points (don't use R11/R12 - R12 is BP)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 2) }));
        // dx = |x2 - x1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0) }));
        string lAbsDxa = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lAbsDxa) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lAbsDxa) }));
        // dy = |y2 - y1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        string lAbsDya = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lAbsDya) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lAbsDya) }));
        // steps = max(dx, dy)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 7) }));
        string lStepsa = newLabel();
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, lStepsa) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lStepsa) }));
        // Loop: draw each point
        string qbLine13Loop = newLabel();
        string qbLine13End = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbLine13Loop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, qbLine13End) }));
        // cx = x1 + (x2-x1) * step / steps  (x2 saved in R9)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 9) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 11) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 11) }));
        // cy = y1 + (y2-y1) * step / steps
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        // Write 1 byte at 帧缓冲基址 + cy*320 + cx
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6) }));
            EmitLoadScreenWidth(10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        FbBase(10);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R0") }));
        // Increment step
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbLine13Loop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbLine13End) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbLineEndLabel) }));
        // 恢复寄存器
        EmitRestoreRegisters(4, 5, 6, 7, 8, 9, 10, 11, 14);
    }

    void GenerateQbBoxOutline(QbLineStatement stmt)
    {
        // Draw 4 edges of a box: top, bottom, left, right
        // Coordinates already scaled: R0=x1, R1=y1, R2=x2, R3=y2
        // Colors in R8=R, R9=G, R10=B

        // Helper: draw one pixel at (x_reg, y_reg) to framebuffer
        void EmitBoxPixel(int xReg, int yReg)
        {
            string pxSkip = newLabel();
            // Coordinate bounds check
            instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, xReg), new(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, pxSkip)]));
            EmitLoadScreenWidth(7);
            instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, xReg), new(OperandType.REGISTER, 7)]));
            instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, pxSkip)]));
            instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, yReg), new(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, pxSkip)]));
            EmitLoadScreenHeight(7);
            instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, yReg), new(OperandType.REGISTER, 7)]));
            instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, pxSkip)]));

            // Compute VRAM address
            instructions.Add(new Instruction(OpCode.PUSH, [new(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, xReg)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, yReg)]));
            EmitLoadScreenWidth(7);
            instructions.Add(new Instruction(OpCode.MUL, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 7)]));
            instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 0)]));
            EmitLoadScreenBpp(7);
            instructions.Add(new Instruction(OpCode.MUL, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 7)]));
            FbBase(7);   // R7 是上面两个乘法的临时寄存器，用完即取基址
            instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 7)]));
            instructions.Add(new Instruction(OpCode.MOVEB, [new(OperandType.REGISTER, 8), new(OperandType.MEMORY, "R6")]));
            instructions.Add(new Instruction(OpCode.POP, [new(OperandType.REGISTER, 0)])); // restore R0 saved at top

            instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, pxSkip)]));
        }

        // Top edge: (x, y1) for x = x1..x2
        string topLoop = newLabel(); string topEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0)])); // R5 = x1
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, topLoop)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 2)])); // CMP x, x2
        instructions.Add(new Instruction(OpCode.JG, [new(OperandType.LABEL, topEnd)]));
        EmitBoxPixel(5, 1); // draw at (R5, R1)
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 4)])); // x++
        instructions.Add(new Instruction(OpCode.JMP, [new(OperandType.LABEL, topLoop)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, topEnd)]));

        // Bottom edge: (x, y2) for x = x1..x2
        string botLoop = newLabel(); string botEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, botLoop)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 2)]));
        instructions.Add(new Instruction(OpCode.JG, [new(OperandType.LABEL, botEnd)]));
        EmitBoxPixel(5, 3); // draw at (R5, R3)
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 4)]));
        instructions.Add(new Instruction(OpCode.JMP, [new(OperandType.LABEL, botLoop)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, botEnd)]));

        // Left edge: (x1, y) for y = y1..y2
        string leftLoop = newLabel(); string leftEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 1)])); // R5 = y1
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, leftLoop)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 3)])); // CMP y, y2
        instructions.Add(new Instruction(OpCode.JG, [new(OperandType.LABEL, leftEnd)]));
        EmitBoxPixel(0, 5); // draw at (R0, R5)
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 4)])); // y++
        instructions.Add(new Instruction(OpCode.JMP, [new(OperandType.LABEL, leftLoop)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, leftEnd)]));

        // Right edge: (x2, y) for y = y1..y2
        string rightLoop = newLabel(); string rightEnd = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 1)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, rightLoop)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 3)]));
        instructions.Add(new Instruction(OpCode.JG, [new(OperandType.LABEL, rightEnd)]));
        EmitBoxPixel(2, 5); // draw at (R2, R5)
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 1)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 4)]));
        instructions.Add(new Instruction(OpCode.JMP, [new(OperandType.LABEL, rightLoop)]));
        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, rightEnd)]));
    }

    void GenerateQbBoxFill(QbLineStatement stmt)
    {
        // Coordinates: R0=x1, R1=y1, R2=x2, R3=y2, Color: R8=index — all set by caller
        EmitGfxFillRect();
    }

}
