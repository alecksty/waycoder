using VMLAssembler;

namespace BasicCompiler;

/// <summary>
/// PC VGA 绘图原语 — 封装 bpp=1 索引色模式下的像素写入、地址计算、边界检查等。
/// 放在 BasicCompiler 项目中，不影响可移植的 CompilerBase。
/// </summary>
public partial class CodeGenerator
{
    // ═══════════════════════════════════════════════════
    //  VGA 常量
    // ═══════════════════════════════════════════════════
    protected const int VGA_BASE = 0xA0000;
    protected const int VGA_WIDTH_ADDR   = 0x6FE0;
    protected const int VGA_HEIGHT_ADDR  = 0x6FE4;
    protected const int VGA_BPP_ADDR     = 0x6FF3;
    protected const int VGA_FONT_MODE    = 0x6FE8; // 字库模式: 0=8x8,32bit/row; 1=8x16,byte/row
    protected const int VGA_FONT_WIDTH   = 0x6FE9; // 字宽(像素)
    protected const int VGA_FONT_HEIGHT  = 0x6FEA; // 字高(像素)
    protected const int VGA_FONT_ADDR    = 0x6FEC; // 字库基址(32-bit)

    // ═══════════════════════════════════════════════════
    //  0. 字库配置 — 从 MMIO 寄存器加载字库参数
    // ═══════════════════════════════════════════════════

    /// <summary>加载字库基址到 dstReg (从 FONT_ADDR 寄存器, 32-bit)</summary>
    public void EmitGfxLoadFontAddr(int dstReg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.IMMEDIATE, VGA_FONT_ADDR) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.MEMORY, $"R{dstReg}") }));
    }

    /// <summary>加载字库高度到 dstReg (从 FONT_HEIGHT 寄存器, 1字节)</summary>
    public void EmitGfxLoadFontHeight(int dstReg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.IMMEDIATE, VGA_FONT_HEIGHT) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.MEMORY, $"R{dstReg}") }));
    }

    /// <summary>加载字库模式到 dstReg (从 FONT_MODE 寄存器, 1字节)</summary>
    public void EmitGfxLoadFontMode(int dstReg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.IMMEDIATE, VGA_FONT_MODE) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.MEMORY, $"R{dstReg}") }));
    }

    /// <summary>加载字库宽度到 dstReg (从 FONT_WIDTH 寄存器, 1字节)</summary>
    public void EmitGfxLoadFontWidth(int dstReg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.IMMEDIATE, VGA_FONT_WIDTH) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, dstReg), new(OperandType.MEMORY, $"R{dstReg}") }));
    }

    // ═══════════════════════════════════════════════════
    //  1. 像素写入 — bpp=1，写 1 字节调色板索引
    // ═══════════════════════════════════════════════════

    /// <summary>写入 1 字节调色板索引到 VRAM [addrReg]，然后 addrReg += 1</summary>
    public void EmitGfxWritePixel(int colorIdxReg, int addrReg)
    {
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> {
            new(OperandType.REGISTER, colorIdxReg),
            new(OperandType.MEMORY, $"R{addrReg}")
        }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, addrReg), new(OperandType.IMMEDIATE, 1) }));
    }

    /// <summary>跳过 1 像素：addrReg += 1</summary>
    public void EmitGfxSkipPixel(int addrReg)
    {
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, addrReg), new(OperandType.IMMEDIATE, 1) }));
    }

    // ═══════════════════════════════════════════════════
    //  2. VRAM 地址计算 — addr = VGA_BASE + y*width + x
    // ═══════════════════════════════════════════════════

    /// <summary>将 R0 设为 VGA VRAM 地址: VGA_BASE + R(Y)*width + R(X)</summary>
    protected void EmitGfxComputeAddr(int yReg, int xReg, int tmpReg = 10)
    {
        // addr = VGA_BASE + y * width + x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, yReg) }));
        EmitGfxLoadWidth(tmpReg);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, tmpReg) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, xReg) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.IMMEDIATE, VGA_BASE) }));
    }

    /// <summary>将指定寄存器设为 VGA VRAM 地址: VGA_BASE + R(yReg)*width + R(xReg)。不硬编码 R0。</summary>
    protected void EmitGfxComputeAddrTo(int resultReg, int yReg, int xReg, int tmpReg = 10)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, resultReg), new(OperandType.REGISTER, yReg) }));
        EmitGfxLoadWidth(tmpReg);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, resultReg), new(OperandType.REGISTER, tmpReg) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, resultReg), new(OperandType.REGISTER, xReg) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, resultReg), new(OperandType.IMMEDIATE, VGA_BASE) }));
    }

    /// <summary>加载 BPP，若 bpp==1 跳到 mode13Label。用 tmpReg 作为临时寄存器。</summary>
    protected void EmitGfxCheckBpp(string mode13Label, int tmpReg = 5)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, tmpReg), new(OperandType.IMMEDIATE, VGA_BPP_ADDR) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, tmpReg), new(OperandType.MEMORY, $"R{tmpReg}") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, tmpReg), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new(OperandType.LABEL, mode13Label) }));
    }

    /// <summary>用 bpp=1 写像素到 (xReg, yReg)，颜色=colorReg。包含边界检查。用 tmpReg1, tmpReg2 做临时寄存器。</summary>
    protected void EmitGfxWritePixelAt(int xReg, int yReg, int colorReg, int tmpReg1 = 7, int tmpReg2 = 8)
    {
        string skip = newLabel();
        EmitGfxBoundsCheck(xReg, yReg, skip, tmpReg1);
        EmitGfxComputeAddrTo(tmpReg1, yReg, xReg, tmpReg2);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, colorReg), new(OperandType.MEMORY, $"R{tmpReg1}") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, skip) }));
    }

    // ═══════════════════════════════════════════════════
    //  3. 边界检查 — 0 <= (x,y) < (width,height)
    // ═══════════════════════════════════════════════════

    /// <summary>如果 Rx &lt; 0 或 Rx >= width 或 Ry &lt; 0 或 Ry >= height，跳到 skipLabel</summary>
    protected void EmitGfxBoundsCheck(int xReg, int yReg, string skipLabel, int tmpReg = 10)
    {
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, xReg), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new(OperandType.LABEL, skipLabel) }));
        EmitGfxLoadWidth(tmpReg);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, xReg), new(OperandType.REGISTER, tmpReg) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, skipLabel) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, yReg), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new(OperandType.LABEL, skipLabel) }));
        EmitGfxLoadHeight(tmpReg);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, yReg), new(OperandType.REGISTER, tmpReg) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, skipLabel) }));
    }

    // ═══════════════════════════════════════════════════
    //  4. 颜色加载 — bpp=1 时颜色索引 = 参数值
    // ═══════════════════════════════════════════════════

    /// <summary>加载调色板颜色索引到 R(resultReg)。bpp=1：颜色即索引。</summary>
    protected void EmitGfxLoadColor(Expression colorExpr, int resultReg)
    {
        GenerateExpr(colorExpr, resultReg);
        // bpp=1 indexed mode: color IS the palette index, no lookup needed
    }

    // ═══════════════════════════════════════════════════
    //  5. 坐标求值 — 求值后 PUSH 到栈
    // ═══════════════════════════════════════════════════

    /// <summary>求值坐标表达式，结果压栈。使用 Regs 管理器分配临时寄存器。</summary>
    protected int EmitGfxPushCoord(Expression expr)
    {
        int r = Regs.AllocInt(instructions);
        GenerateExpr(expr, r);
        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new(OperandType.REGISTER, r) }));
        Regs.FreeInt(r, instructions);
        return r;
    }

    /// <summary>安全求值表达式——先保存 R0-R4，求值后恢复。防止求值器覆盖调用方寄存器。</summary>
    protected void EmitGfxSafeEval(Expression expr, int resultReg)
    {
        EmitSaveRegs(0, 1, 2, 3, 4);
        GenerateExpr(expr, resultReg);
        // Move result to safe location before restore
        if (resultReg <= 4)
        {
            int safe = Regs.AllocInt(instructions);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, safe), new(OperandType.REGISTER, resultReg) }));
            EmitRestoreRegs(0, 1, 2, 3, 4);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, resultReg), new(OperandType.REGISTER, safe) }));
            Regs.FreeInt(safe, instructions);
        }
        else
        {
            EmitRestoreRegs(0, 1, 2, 3, 4);
        }
    }

    /// <summary>安全求值+PUSH：保护 R0-R4，求值到临时寄存器，压栈，恢复 R0-R4</summary>
    protected void EmitGfxSafePushCoord(Expression expr)
    {
        int r = Regs.AllocInt(instructions);
        EmitPreserveRegs(() => GenerateExpr(expr, r), 0, 1, 2, 3, 4);
        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new(OperandType.REGISTER, r) }));
        Regs.FreeInt(r, instructions);
    }

    // ═══════════════════════════════════════════════════
    //  6. 辅助 — 加载 width/height
    // ═══════════════════════════════════════════════════

    protected void EmitGfxLoadWidth(int reg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, reg), new(OperandType.IMMEDIATE, VGA_WIDTH_ADDR) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, reg), new(OperandType.MEMORY, $"R{reg}") }));
    }

    protected void EmitGfxLoadHeight(int reg)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, reg), new(OperandType.IMMEDIATE, VGA_HEIGHT_ADDR) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, reg), new(OperandType.MEMORY, $"R{reg}") }));
    }

    // ═══════════════════════════════════════════════════
    //  7. 图形语句参数求值 — LINE/CIRCLE/PAINT 坐标+颜色
    // ═══════════════════════════════════════════════════

    /// <summary>LINE 参数求值：X1,Y1,X2,Y2 → PUSH → POP R0-R3，Color → R4</summary>
    protected void EmitGfxLineArgs(Expression x1, Expression y1, Expression x2, Expression y2, Expression color)
    {
        // 求值+PUSH 四个坐标
        EmitGfxPushCoord(x1);
        EmitGfxPushCoord(y1);
        EmitGfxPushCoord(x2);
        EmitGfxPushCoord(y2);
        // 先 POP 坐标到 R0-R3（颜色求值可能用 R0-R3 做临时寄存器）
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 3) })); // Y2
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 2) })); // X2
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 1) })); // Y1
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 0) })); // X1
        // 颜色求值（坐标已安全在 R0-R3）
        EmitGfxLoadColor(color, 4);
    }

    /// <summary>CIRCLE 参数求值：X,Y,R → R0-R2, Color → R3</summary>
    protected void EmitGfxCircleArgs(Expression x, Expression y, Expression radius, Expression color)
    {
        EmitGfxPushCoord(x);
        EmitGfxPushCoord(y);
        GenerateExpr(radius, 2);
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 1) })); // Y
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 0) })); // X
        EmitGfxLoadColor(color, 3);
    }

    /// <summary>PAINT 参数求值：X,Y → R1-R2, Color → R0, Border → R6 (if hasBorder)</summary>
    protected void EmitGfxPaintArgs(Expression x, Expression y, Expression color,
        bool hasBorder, Expression border = null)
    {
        GenerateExpr(color, 0);     // R0 = fill color
        EmitGfxPushCoord(x);        // PUSH X
        EmitGfxPushCoord(y);        // PUSH Y
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 2) })); // Y
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, 1) })); // X
        // Save fill index at 0x6DF0 BEFORE any other code clobbers R0
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 8), new(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.MEMORY, "R8") }));
        if (hasBorder && border != null)
        {
            GenerateExpr(border, 6); // R6 = border index
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 8), new(OperandType.IMMEDIATE, 0x6DF1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.MEMORY, "R8") }));
        }
    }

    // ═══════════════════════════════════════════════════
    //  8. 屏幕模式检查
    // ═══════════════════════════════════════════════════

    /// <summary>如果 SCREEN 模式是 13，跳到 mode13Label。R5 被用作临时寄存器。</summary>
    protected void EmitGfxCheckMode13(string mode13Label)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0x6FF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 13) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new(OperandType.LABEL, mode13Label) }));
    }

    /// <summary>如果 SCREEN 模式是 0 (文本模式)，跳到 textModeLabel。R5 被用作临时寄存器。</summary>
    protected void EmitGfxCheckMode0(string textModeLabel)
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0x6FF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new(OperandType.LABEL, textModeLabel) }));
    }

    // ═══════════════════════════════════════════════════
    //  9. 清屏 — CLS: graphics framebuffer → 0, text buffer → spaces
    // ═══════════════════════════════════════════════════

    /// <summary>CLS 清屏：图形模式填 0，文本模式填空格。R0-R3 被修改。</summary>
    protected void EmitGfxClearScreen()
    {
        string clsText = newLabel(), clsEnd = newLabel();
        EmitGfxCheckMode0(clsText); // 文本模式 → 跳转到文本清屏

        // Graphics mode: fill VGA_BASE with zeros
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, VGA_BASE) }));
        EmitGfxLoadWidth(2);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 3), new(OperandType.IMMEDIATE, VGA_HEIGHT_ADDR) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 3), new(OperandType.MEMORY, "R3") }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.REGISTER, 3) }));
        string gfxClsLoop = newLabel(), gfxClsEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, gfxClsLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new(OperandType.LABEL, gfxClsEnd) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, gfxClsLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, gfxClsEnd) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, clsEnd) }));

        // Text mode: fill with spaces
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, clsText) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.IMMEDIATE, 0x20) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 0xB8000) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.IMMEDIATE, 4000) }));
        string txtClsLoop = newLabel(), txtClsEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, txtClsLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new(OperandType.LABEL, txtClsEnd) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 3), new(OperandType.IMMEDIATE, 7) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 3), new(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, txtClsLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, txtClsEnd) }));

        // Reset cursor
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, clsEnd) }));
        ResetCursor();
    }

    /// <summary>重置 VGA 文本光标到 (0,0)</summary>
    private void ResetCursor()
    {
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 0x6FF4) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.MEMORY, "R1") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.IMMEDIATE, 0x6FF8) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.MEMORY, "R1") }));
    }

    // ═══════════════════════════════════════════════════
    //  10. 矩形填充 — LINE BF 的本质操作
    //  R0=x1, R1=y1, R2=x2, R3=y2, R8=color index
    //  自动处理 X/Y 交换确保 x1<=x2, y1<=y2
    // ═══════════════════════════════════════════════════

    /// <summary>矩形填充：填充 (x1,y1)-(x2,y2) 矩形，颜色在 R8</summary>
    protected void EmitGfxFillRect()
    {
        string yLoop = newLabel(), yEnd = newLabel();
        string xLoop = newLabel(), xEnd = newLabel();

        // Swap X if x1 > x2
        string xSwapEnd = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new(OperandType.LABEL, xSwapEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 2), new(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, xSwapEnd) }));

        // Swap Y if y1 > y2
        string ySwapEnd = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new(OperandType.LABEL, ySwapEnd) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 1), new(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 3), new(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, ySwapEnd) }));

        // Y loop: R5 = y1 .. y2
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, yLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new(OperandType.LABEL, yEnd) }));

        // X loop: R6 = x1 .. x2
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, xLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new(OperandType.LABEL, xEnd) }));

        // VRAM addr: R7 = VGA_BASE + R5*width + R6
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 5) }));
        EmitGfxLoadWidth(4);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.IMMEDIATE, VGA_BASE) }));

        // Bounds check + write
        string skip = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.IMMEDIATE, VGA_BASE) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new(OperandType.LABEL, skip) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.IMMEDIATE, VGA_BASE + 0x100000) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, skip) }));

        EmitGfxWritePixel(8, 7);  // STOREB R8, [R7]; R7+=1 (not used after)

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, skip) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, xLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, xEnd) }));

        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, yLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, yEnd) }));
    }

    // ═══════════════════════════════════════════════════
    //  11. 直线绘制 — Bresenham 步进算法
    //  R0=x1, R1=y1, R2=x2, R3=y2, R8=color index
    // ═══════════════════════════════════════════════════

    /// <summary>绘制直线 (x1,y1)-(x2,y2)，颜色在 R8。自动保护 R8,R11,R14。</summary>
    protected void EmitGfxLine()
    {
        EmitSaveRegs(8, 11, 14); // protect color, x1_saved, y1_saved
        string lineLoop = newLabel(), lineEnd = newLabel();

        // Save endpoints
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 11), new(OperandType.REGISTER, 0) })); // x1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 14), new(OperandType.REGISTER, 1) })); // y1

        // dx = |x2 - x1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0) }));
        string absDx = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, absDx) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, absDx) }));

        // dy = |y2 - y1|
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 1) }));
        string absDy = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, absDy) }));
        instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, absDy) }));

        // steps = max(dx, dy)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 5) }));
        string stepsDone = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new(OperandType.LABEL, stepsDone) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, stepsDone) }));

        // Step loop: R4 = 0..steps
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, lineLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 4), new(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, lineEnd) }));

        // cx = x1 + (x2-x1)*step/steps
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 11) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 11) })); // cx

        // cy = y1 + (y2-y1)*step/steps
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 14) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 14) })); // cy

        // Bounds check + VRAM write
        string skip = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new(OperandType.LABEL, skip) }));
        EmitGfxLoadWidth(0);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 5), new(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, skip) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new(OperandType.LABEL, skip) }));
        EmitGfxLoadHeight(0);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new(OperandType.REGISTER, 6), new(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new(OperandType.LABEL, skip) }));

        // VRAM addr
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 6) })); // y
        EmitGfxLoadWidth(1);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 5) })); // +x
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 0), new(OperandType.IMMEDIATE, VGA_BASE) }));

        EmitGfxWritePixel(8, 0);  // STOREB R8, [R0]; R0+=1

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, skip) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new(OperandType.REGISTER, 4), new(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new(OperandType.LABEL, lineLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new(OperandType.LABEL, lineEnd) }));
        EmitRestoreRegs(8, 11, 14);
    }
}
