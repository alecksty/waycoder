using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    void GenerateQbBoxOutlineMode13(QbLineStatement stmt)
    {
        // Box outline for mode 13: draw 4 lines (top, bottom, left, right)
        // Coordinates already in R0-R3, color in R4
        // x1=R0, y1=R1, x2=R2, y2=R3, color=R4
        // IMPORTANT: use rr≠0 in GenerateWritePixelMode13 so y-coord register is NOT clobbered
        //   R7=result (safe to clobber), R8=temp

        // Top edge: (x1,y1) to (x2,y1) — use R7 for addr calc (preserves R1=y1)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0) }));
        string bo13T = newLabel(), bo13TE = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13T) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, bo13TE) }));
        GenerateWritePixelMode13(5, 1, 7, 8, 4);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bo13T) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13TE) }));

        // Bottom edge: (x1,y2) to (x2,y2) — use R7 to preserve R3=y2
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0) }));
        string bo13B = newLabel(), bo13BE = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13B) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, bo13BE) }));
        GenerateWritePixelMode13(5, 3, 7, 8, 4);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bo13B) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13BE) }));

        // Left edge: (x1,y1+1) to (x1,y2-1) — use R7 to preserve R6 (y-counter)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1) }));
        string bo13L = newLabel(), bo13LE = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13L) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, bo13LE) }));
        GenerateWritePixelMode13(5, 6, 7, 8, 4);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bo13L) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13LE) }));

        // Right edge: (x2,y1+1) to (x2,y2-1) — use R7 to preserve R6 (y-counter)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1) }));
        string bo13R = newLabel(), bo13RE = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13R) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, bo13RE) }));
        GenerateWritePixelMode13(5, 6, 7, 8, 4);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bo13R) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bo13RE) }));
    }

    void GenerateQbBoxFillMode13(QbLineStatement stmt)
    {
        // Box fill for mode 13: double loop over rectangle, 1-byte pixel writes
        // R0=x1, R1=y1, R2=x2, R3=y2, R4=color_index
        // Save x1→R7 (preserved across GenerateWritePixelMode13 clobbering R0)
        // Save y1→R9 for Y-loop (GenerateWritePixelMode13 clobbers the Y register via r=ry)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 1) }));
        string bf13YLoop = newLabel(), bf13YEnd = newLabel();
        string bf13XLoop = newLabel(), bf13XEnd = newLabel();

        // Y loop: use R9 as y counter (not R1/R5 which get clobbered by pixel write)
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bf13YLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, bf13YEnd) }));

        // X loop: reset X from saved R7, use R9 as y for pixel write (ry=9)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bf13XLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, bf13XEnd) }));

        // Pixel write at (R6=x, R9=y) with color R4, result in R5 (safe, not used by loop), temp R8
        GenerateWritePixelMode13(6, 9, 5, 8, 4);

        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bf13XLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bf13XEnd) }));

        // Advance Y (R9)
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, bf13YLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, bf13YEnd) }));
    }

    /// Helper: write 1-byte pixel at (rx, ry) with color rc
    /// rx, ry, rc are register indices containing x, y, color
    /// rr=result register for VRAM address (must NOT be ry to avoid clobbering it)
    void GenerateWritePixelMode13(int rx, int ry, int rr, int rtmp, int rc)
    {
        // addr = VGA_BASE + ry*width + rx
        int r = rr != 0 ? rr : ry;
        if (rr != 0) instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, rr), new Operand(OperandType.REGISTER, ry) }));
        EmitGfxComputeAddrTo(r, ry, rx, rtmp);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, rc), new Operand(OperandType.MEMORY, $"R{r}") }));
    }

    // Draw a pixel at (R7, R8) — writes palette color index (1 byte, bpp=1)
    void GenEllipseDrawPixel(int colorIdxReg, int _unused1, int _unused2)
    {
        int vgaBase = VgaBase;
        string skipPx = newLabel();

        // Coordinate bounds check
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 7), new(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, skipPx)]));
        EmitLoadScreenWidth(10);
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 7), new(OperandType.REGISTER, 10)]));
        instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, skipPx)]));
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 8), new(OperandType.IMMEDIATE, 0)]));
        instructions.Add(new Instruction(OpCode.JL, [new(OperandType.LABEL, skipPx)]));
        EmitLoadScreenHeight(10);
        instructions.Add(new Instruction(OpCode.CMP, [new(OperandType.REGISTER, 8), new(OperandType.REGISTER, 10)]));
        instructions.Add(new Instruction(OpCode.JGE, [new(OperandType.LABEL, skipPx)]));

        // addr = vgaBase + y*width + x  (bpp=1, no multiply needed)
        instructions.Add(new Instruction(OpCode.MOVE, [new(OperandType.REGISTER, 9), new(OperandType.REGISTER, 8)]));
        EmitLoadScreenWidth(10);
        instructions.Add(new Instruction(OpCode.MUL, [new(OperandType.REGISTER, 9), new(OperandType.REGISTER, 10)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 9), new(OperandType.REGISTER, 7)]));
        instructions.Add(new Instruction(OpCode.ADD, [new(OperandType.REGISTER, 9), new(OperandType.IMMEDIATE, vgaBase)]));
        // Write palette color index (1 byte)
        instructions.Add(new Instruction(OpCode.MOVEB, [new(OperandType.REGISTER, colorIdxReg), new(OperandType.MEMORY, "R9")]));

        instructions.Add(new Instruction(OpCode.LABEL, [new(OperandType.LABEL, skipPx)]));
    }

    void GenerateQbCircleStatement(QbCircleStatement stmt)
    {
        if (UiGfx) { UiEmitCircleStatement(stmt); return; }
        // Evaluate: x, y, radius, color_index
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.X, 0); instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            GenerateSubExpression(stmt.Y, 1); instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
            GenerateSubExpression(stmt.Radius, 2);
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
            GenerateSubExpression(stmt.Color, 3);
        }
        else
        {
            GenerateExpression(stmt.X, 0); instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            GenerateExpression(stmt.Y, 1); instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
            GenerateExpression(stmt.Radius, 2);
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
            GenerateExpression(stmt.Color, 3);
        }

        // 保护被使用的寄存器 R4-R10 (R0-R3 是调用者的临时寄存器)
        EmitSaveRegisters(4, 5, 6, 7, 8, 9, 10);

        string qbCircleMode13Label = newLabel();
        string qbCircleEndLabel = newLabel();
        EmitGfxCheckBpp(qbCircleMode13Label, 5);

        // For high-res modes (SCREEN 9+, 640+ wide), skip coordinate scaling
        string qbCircleHighResLabel = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0x6FF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbCircleHighResLabel) }));

        // bpp=1 indexed mode: color IS the palette index, no lookup needed
        // Keep R3=color index, copy to R4 for GenCirclePixel writes
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 3) }));

        int palR = 4, palG = 4, palB = 4;

        // Ellipse support: if aspect is given and compile-time known, precompute points
        if (stmt.HasAspect && stmt.Radius is NumberLiteral rn && stmt.Aspect is NumberLiteral an)
        {
            double asp = (double)an.Value;
            int r = (int)rn.Value;
            int rx = r / 2;          // scaled x-radius
            int ry = (int)(r * asp) / 2; // scaled y-radius
            if (ry < 1) ry = 1;
            if (rx < 1) rx = 1;

            // Precompute ellipse boundary points at compile time using C# Math
            int steps = Math.Max(36, Math.Max(rx, ry));
            if (steps > 180) steps = 180;

            string ellEndLbl = newLabel();
            string ellFirstLbl = newLabel();
            int firstX = 0, firstY = 0;

            for (int i = 0; i <= steps; i++)
            {
                double ang = 2 * Math.PI * i / steps;
                int xOff = (int)(rx * Math.Cos(ang));
                int yOff = (int)(ry * Math.Sin(ang));

                if (i == 0)
                {
                    // PSET (cx + xOff, cy + yOff)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, xOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 0) })); // cx
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, yOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 1) })); // cy
                    // Draw pixel at (R7, R8)
                    GenEllipseDrawPixel(palR, palG, palB);
                    firstX = xOff; firstY = yOff;
                }
                else
                {
                    // Draw pixel at (cx + xOff, cy + yOff) 
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, xOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 0) })); // cx
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, yOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 1) })); // cy
                    GenEllipseDrawPixel(palR, palG, palB);
                }
            }

            // Exit early for ellipse - skip the standard circle algorithm
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbCircleEndLabel) }));
            goto end_of_circle; // C# code: skip rest of function
        }

        // Midpoint circle algorithm
        // R0=cx, R1=cy, R2=r, R4=R, R5=G, R6=B
        // x=0, y=r, d=1-r
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) })); // x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 2) })); // y
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 2) })); // d = 1 - r

        string circleLoop = newLabel();
        string circleEnd = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, circleLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, circleEnd) }));

        // Arc/sector support: determine which quadrants to draw based on start/end angles
        // QBasic CIRCLE angles: 0=right, pi/2=up, pi=left, 3pi/2=down (counter-clockwise)
        // We classify arcs into 4 half-circle types:
        //   upper: start~0, end~pi  | lower: start~pi, end~2pi
        //   left:  start~pi/2, end~3pi/2 | right: start~-pi/2, end~pi/2
        bool drawSW = true, drawSE = true, drawNE = true, drawNW = true; // sw=(+x,+y) etc.
        
        if (stmt.HasStart && stmt.HasEnd)
        {
            // Try to evaluate start/end as constants for compile-time optimization
            double s = 0, e = 0;
            bool canEval = stmt.Start is NumberLiteral sn && stmt.End is NumberLiteral en;
            if (canEval) { s = (double)((NumberLiteral)stmt.Start).Value; e = (double)((NumberLiteral)stmt.End).Value; }
            
            // Normalize angles to [0, 2*pi)
            double pi = 3.141592653589793;
            while (s < 0) s += 2*pi; while (s >= 2*pi) s -= 2*pi;
            while (e < 0) e += 2*pi; while (e >= 2*pi) e -= 2*pi;
            
            if (canEval)
            {
                // Determine arc type by checking which half-circle is covered
                // Check if arc covers the upper half (going counter-clockwise from right through top to left)
                
                // Simple heuristic: check start/end positions
                if (Math.Abs(s - 0) < 0.01 && Math.Abs(e - pi) < 0.01) { drawSE = false; drawSW = false; }       // upper half
                else if (Math.Abs(s - pi) < 0.01 && (Math.Abs(e - 2*pi) < 0.01 || e < 0.01)) { drawNE = false; drawNW = false; } // lower half
                else if (Math.Abs(s - pi/2) < 0.01 && Math.Abs(e - 3*pi/2) < 0.01) { drawNE = false; drawSE = false; } // left half
                else if ((s < 0.01 || Math.Abs(s - 2*pi) < 0.01) && Math.Abs(e - pi) < 0.01) { drawNW = false; drawSW = false; } // right half (via -pi/2 to pi/2)
                
                // Add radial lines for pie sector (from center to arc endpoints)
                // For now, just draw the arc (no sector lines)
            }
        }

        // Draw 8 symmetric points, skipping quadrants not in the arc
        if (drawSE) GenCirclePixel(1, 1);
        if (drawSW) GenCirclePixel(-1, 1);
        if (drawNE) GenCirclePixel(1, -1);
        if (drawNW) GenCirclePixel(-1, -1);
        if (drawSE) GenCirclePixelSwapped(1, 1);
        if (drawSW) GenCirclePixelSwapped(-1, 1);
        if (drawNE) GenCirclePixelSwapped(1, -1);
        if (drawNW) GenCirclePixelSwapped(-1, -1);

        // Sector radial lines can be drawn manually by the user with LINE + PAINT
        // For now, just the arc is drawn

        // if d < 0: d += 2*x + 3
        string dPos = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, dPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 10) }));
        string dCont = newLabel();
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, dCont) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, dPos) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 5) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 10) })); // y--

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, dCont) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 10) })); // x++
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, circleLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, circleEnd) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbCircleEndLabel) }));

    end_of_circle: ;
        // NOTE: Labels emitted here (after end_of_circle) — the ellipse path's
        // goto end_of_circle redirects here, so all deferred labels must be AFTER this point

        // === Mode 13 CIRCLE path: no scaling, 1-byte writes ===
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbCircleMode13Label) }));
        // R0=cx, R1=cy, R2=r, R3=color_index
        // Midpoint circle with 1-byte pixel writes
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) })); // x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 2) })); // y
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 2) })); // d = 1 - r
        string qbCircle13Loop = newLabel();
        string qbCircle13End = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbCircle13Loop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, qbCircle13End) }));
        GenCirclePixelMode13(1, 1);
        GenCirclePixelMode13(-1, 1);
        GenCirclePixelMode13(1, -1);
        GenCirclePixelMode13(-1, -1);
        GenCirclePixelMode13Swapped(1, 1);
        GenCirclePixelMode13Swapped(-1, 1);
        GenCirclePixelMode13Swapped(1, -1);
        GenCirclePixelMode13Swapped(-1, -1);
        // if d < 0: d += 2*x + 3
        string dPos13 = newLabel();
        string dCont13 = newLabel();
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, dPos13) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, dCont13) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, dPos13) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 2) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 5) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 9), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 1) })); // y--
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, dCont13) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 1) })); // x++
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbCircle13Loop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbCircle13End) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbCircleEndLabel) }));
        // 恢复寄存器
        EmitRestoreRegisters(4, 5, 6, 7, 8, 9, 10);
    }

    void GenCirclePixel(int sx, int sy)
    {
        // cx,cy, R,G,B are in R0,R1,R4,R5,R6; x,y in R7,R8
        // Compute address and write
        int vgaBase = VgaBase;

        // cx + sx*x, cy + sy*y
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) })); // px = cx
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) })); // py = cy (preserve R1!)

        if (sx >= 0)
        {
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 7) })); // px += x
        }
        else
        {
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 7) })); // px -= x
        }
        if (sy >= 0)
        {
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 8) })); // py += y
        }
        else
        {
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 8) })); // py -= y
        }

        // Compute addr in R11 to avoid corrupting R1 (cy)
        // addr = vgaBase + (py * 320 + px) * 3
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 3) })); // py to R11
            EmitLoadScreenWidth(10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 2) })); // +px
            EmitLoadScreenBpp(10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, vgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));

        // 边界检查: 跳过超出帧缓冲区的像素写入
        string circBoundsSkip = newLabel();
        int fbEnd = vgaBase + 1024 * 1024; // 1MB 安全上限
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, vgaBase) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, circBoundsSkip) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, fbEnd) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, circBoundsSkip) }));

        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R11") })); // color index from R10

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, circBoundsSkip) }));
    }

    void GenCirclePixelSwapped(int sx, int sy)
    {
        // Same as GenCirclePixel but with x and y swapped: px = cx + sx*y, py = cy + sy*x
        // cx,cy,R,G,B in R0,R1,R4,R5,R6; x=R7, y=R8
        int vgaBase = VgaBase;
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) })); // px = cx
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) })); // py = cy
        if (sx >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 8) })); // px += y (swapped!)
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 8) })); // px -= y
        if (sy >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 7) })); // py += x (swapped!)
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 7) })); // py -= x
        // Compute addr in R11 (preserve R1)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 3) }));
            EmitLoadScreenWidth(10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 2) }));
            EmitLoadScreenBpp(10);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, vgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.REGISTER, 10) }));

        // 边界检查
        string cpsBoundsSkip = newLabel();
        int fbEnd2 = vgaBase + 1024 * 1024;
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, vgaBase) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, cpsBoundsSkip) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 11), new Operand(OperandType.IMMEDIATE, fbEnd2) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, cpsBoundsSkip) }));

        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R11") }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, cpsBoundsSkip) }));
    }

    void GenCirclePixelMode13(int sx, int sy)
    {
        // cx=R0, cy=R1, color_index=R3, x=R7, y=R8
        // Compute pixel address: VgaBase + (cy+sy*y) * width + (cx+sx*x)
        // IMPORTANT: Must NOT corrupt R1 (cy) for subsequent calls
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        if (sx >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 7) }));
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 7) }));
        if (sy >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 8) }));
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 8) }));
        // addr in R5 to preserve R1 (cy) for next GenCirclePixelMode13 call
        EmitGfxComputeAddrTo(5, 4, 2, 10);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R5") }));
    }

    void GenCirclePixelMode13Swapped(int sx, int sy)
    {
        // Same as GenCirclePixelMode13 but with x and y swapped: px=cx+sx*y, py=cy+sy*x
        // cx=R0, cy=R1, color_index=R3, x=R7, y=R8
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1) }));
        if (sx >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 8) })); // px += y
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 8) }));
        if (sy >= 0)
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 7) })); // py += x
        else
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 7) }));
        // addr in R5, preserve R1 (cy)
        EmitGfxComputeAddrTo(5, 4, 2, 10);
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R5") }));
    }

    void GenerateQbPaintStatement(QbPaintStatement stmt)
    {
        if (UiGfx) { UiEmitPaintStatement(stmt); return; }
        // PAINT flood fill 鈥?stack-based iterative flood fill
        int vgaBase = VgaBase;
        int stackBase = 0x90000; // flood fill stack buffer (above StaticBase 0x80000) [x,y pairs] (safe: above StaticBase 0x7000)
        int stackPtrAddr = 0x8FFFC; // stack pointer address

        // Evaluate: color, x, y (border evaluated later after fill save)
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.Color, 0);
            GenerateSubExpression(stmt.X, 1);
            GenerateSubExpression(stmt.Y, 2);
        }
        else
        {
            GenerateExpression(stmt.Color, 0);
            GenerateExpression(stmt.X, 1);
            GenerateExpression(stmt.Y, 2);
        }

        // 保护内部使用的寄存器 R3-R10
        EmitSaveRegisters(3, 4, 5, 6, 7, 8, 9, 10);

        // Check SCREEN mode for mode 13 (only mode 13 uses special 256-color path)
        string qbPaintMode13Label = newLabel();
        string qbPaintEndLabel = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0x6FF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 13) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, qbPaintMode13Label) }));

        // Save fill color index at 0x6DF0 BEFORE R0 is clobbered by palette lookup
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R8") }));

        // Palette lookup for fill color (R0=color_index) -> R3=R, R4=G, R5=B
            EmitLoadScreenBpp(5);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, QB_PALETTE_ADDR) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R0") })); // fill_R
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R0") })); // fill_G
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R0") })); // fill_B

        // Border color: save index directly at 0x6DF1 (R6 = border color)
        if (stmt.HasBorder && stmt.Border != null)
        {
            GenerateExpression(stmt.Border, 6); // R6 = border color index
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R8") })); // 0x6DF1 = border index
        }

        // 无 border 时: 读取种子像素作为背景色参考
        if (!stmt.HasBorder || stmt.Border == null)
        {
            // 读取 (R1,R2) 处的像素 RGB → 保存到 0x6DF3-5
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2) }));
            EmitLoadScreenWidth(7);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            EmitLoadScreenBpp(7);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, vgaBase) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R0") })); // bg_R
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF3) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R8") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R0") })); // bg_G
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF4) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R8") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R0") })); // bg_B
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF5) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R8") }));
        }

        // Save starting position: R1=scaled_x, R2=scaled_y
        // Initialize stack pointer = 0
        AddRI(OpCode.MOVE, 0, stackPtrAddr);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));

        // Push start point (x, y) onto stack
        AddRI(OpCode.MOVE, 0, stackPtrAddr);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") })); // sp
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R7") })); // stack[sp] = x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R7") })); // stack[sp+4] = y
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 8) })); // sp += 8
        AddRI(OpCode.MOVE, 0, stackPtrAddr);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));

        // Flood fill loop
        string flLoop = newLabel();
        string flEnd = newLabel();
        string flCheck = newLabel();
        string flSkip = newLabel();
        string flPush = newLabel();

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));

        // Check if stack is empty (sp == 0)
        AddRI(OpCode.MOVE, 0, stackPtrAddr);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, flEnd) }));

        // Pop x, y
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 8) })); // sp -= 8
        AddRI(OpCode.MOVE, 0, stackPtrAddr);
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R7") })); // pop_x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R7") })); // pop_y

        // Bounds check
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
        EmitLoadScreenWidth(8);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
        EmitLoadScreenHeight(8);
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 8) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));

            // Read pixel at (x, y): addr = vgaBase + (y * 320 + x) * 3
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2) }));
            EmitLoadScreenWidth(7);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            EmitLoadScreenBpp(7);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, vgaBase) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 7) }));

        // Read 1 pixel byte (palette index) into R7
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.MEMORY, "R0") }));

        // Check already filled: pixel == fill index?
        string paintNotFilled = newLabel();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R8") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 8) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, paintNotFilled) }));

        // Border check
        if (stmt.HasBorder && stmt.Border != null)
        {
            string paintDoFill = newLabel();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R8") }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 8) }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, paintDoFill) }));
        }
        else
        {
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
        }

        // Write fill index (1 byte)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R8") }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R0") }));

        // Push neighbors (x+1,y), (x-1,y), (x,y+1), (x,y-1)
        string pushNeighbor = newLabel();

        // Macro: push a single (x,y) point
        void GenPushPixel()
        {
            AddRI(OpCode.MOVE, 0, stackPtrAddr);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") })); // sp
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R7") })); // stack[sp] = x
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R7") })); // stack[sp+4] = y
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 8) })); // sp += 8
            // Limit check: if sp >= 2048*8, stop
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 1048576) })); // 1MB stack
            string stackOk = newLabel();
            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, stackOk) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) })); // reset stack = abort fill
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, stackOk) }));
            AddRI(OpCode.MOVE, 0, stackPtrAddr);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));
        }

        // Push x+1, y
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        GenPushPixel();

        // Push x-1, y
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 2) }));
        GenPushPixel();

        // Push x, y+1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
        GenPushPixel();

        // Push x, y-1
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 2) }));
        GenPushPixel();

        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, flLoop) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, flEnd) }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, qbPaintEndLabel) }));

        // === Mode 13 PAINT path: no scaling, 1-byte pixels ===
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbPaintMode13Label) }));
        // R0=color_index, R1=x, R2=y (unscaled)
        int vgaBase13p = VgaBase;
        int stackBase13p = 0x90000;
        int stackPtrAddr13p = 0x8FFFC;
        // Save fill color at fixed addr
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R5") }));
        // Save start position
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) })); // saved_x
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 2) })); // saved_y
        // Initialize stack pointer = 0
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, stackPtrAddr13p) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R5") }));
        // Push start point
        void GenPushPixel13p()
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, stackPtrAddr13p) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R5") }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase13p) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R7") }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase13p) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R7") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 8) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, stackPtrAddr13p) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R5") }));
        }
        GenPushPixel13p();
        // Flood fill loop
        string flLoop13 = newLabel();
        string flEnd13 = newLabel();
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        // Check stack empty
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, stackPtrAddr13p) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, flEnd13) }));
        // Pop x, y
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 8) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, stackPtrAddr13p) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R5") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase13p) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R7") }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, stackBase13p) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 4) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R7") }));
        // Bounds check: 0-319, 0-199
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 320) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 200) }));
        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        // Read pixel at (x, y): 1 byte
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4) }));
            EmitLoadScreenWidth(5);
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, vgaBase13p) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R0") })); // pixel byte
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, 0x6DF0) }));
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R6") })); // fill_color
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 6) }));
        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        // Check if pixel is background (0)
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0) }));
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        // Write fill color to pixel (1 byte)
        instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.MEMORY, "R0") }));
        // Push neighbors (x+1,y), (x-1,y), (x,y+1), (x,y-1)
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1) }));
        GenPushPixel13p();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 2) }));
        GenPushPixel13p();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        GenPushPixel13p();
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 4) }));
        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
        GenPushPixel13p();
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, flLoop13) }));
        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, flEnd13) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, qbPaintEndLabel) }));
        // 恢复寄存器
        EmitRestoreRegisters(3, 4, 5, 6, 7, 8, 9, 10);
    }

}
