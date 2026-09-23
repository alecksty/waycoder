using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    void GenerateRandomizeStatement(RandomizeStatement stmt)
    {
        // RANDOMIZE: seed the RNG.
        // If TIMER is used, we need a time-based seed.
        // For simplicity, use a fixed seed based on the instruction count.
        if (stmt.HasSeed && stmt.Seed != null)
        {
            if (currentSubName != null)
                GenerateSubExpression(stmt.Seed, 0);
            else
                GenerateExpression(stmt.Seed, 0);
        }
        else
        {
            // TIMER: use SYSCALL 53 (GetTick) for time-based seed
            EmitGetTick();
        }
        // Store seed at RNG seed address
        AddRI(OpCode.MOVE, 1, 0x9E000);
        AddInstruction(OpCode.MOVE, Reg(0), Mem("R1"));
    }
    void GenerateInkeyExpression(InkeyExpression expr, int reg)
    {
        // INKEY$: SYSCALL 5 (get key)。R0=0 ⇒ 非阻塞（没按键就返回 0）。
        AddRI(OpCode.MOVE, 0, 0);
        EmitInputChar();

        // ── `INKEY$` 是**字符串函数** ─────────────────────────────────────────────
        //
        // ⚠ 它此前把 SYSCALL 5 返回的**字符码**直接当结果交给调用方，而调用方拿它当
        //   **字符串指针**用（`Char$ = INKEY$` 存进字符串变量、`UCASE$(Char$) = "V"`
        //   去解引用）。于是 `Char$` 里躺着的是 86 这种小整数：
        //     · `Char$ = ""` 恒不成立（拿 86 当地址解引用）⇒ GORILLA 的
        //       `DO WHILE Char$ = "": Char$ = INKEY$: LOOP` **永远出不去**；
        //     · 按了键也认不出来（`UCASE$` 读的是零页里的字节，不是那个字符）。
        //   两个症状都不报错 —— 表现就是"程序卡在等按键"。
        //
        //   现在按 QBasic 语义补齐两条：
        //     ① **没按键 → 空串 `""`**（不是 0）；
        //     ② 有按键 → 走 `basic_chr` 变成**真正的 1 字符串**（与 `CHR$()` 同一条路，
        //        不在这里另造一个字符串构造 —— 那种"同一件事两处实现"迟早漂）。
        string noKeyLabel = GenerateLabel();
        string doneLabel = GenerateLabel();

        AddRI(OpCode.CMP, 0, 0);
        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, noKeyLabel) }));

        // 没按键：给一个长度 0 的常量串
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
        {
            new Operand(OperandType.REGISTER, 0),
            new Operand(OperandType.LABEL, EmptyStringLabel)
        }));
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, doneLabel) }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, noKeyLabel) }));
        // 有按键：字符码 → 字符串（库函数，调用方清那一个实参槽）
        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
        instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, "basic_chr") }));
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
        {
            new Operand(OperandType.REGISTER, 13),
            new Operand(OperandType.IMMEDIATE, 4)
        }));

        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, doneLabel) }));

        if (reg != 0)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
    }

    void GenerateTimerFunction(int reg)
    {
        // TIMER: return elapsed seconds via SYSCALL 53 (GetTick)
        EmitGetTick();
        // R0 = milliseconds, convert to seconds: R0 = R0 / 1000
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1000) }));
        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
    }

    void GenerateDateFunction(int reg)
    {
        // DATE$: SYSCALL 55 returns pointer to "YYYY-MM-DD\0" string in R0
        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 55) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
    }

    void GenerateTimeFunction(int reg)
    {
        // TIME$: SYSCALL 56 returns pointer to "HH:mm:ss\0" string in R0
        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 56) }));
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
    }
    // ==================== PLAY/SOUND ====================

    /// <summary>
    /// Parse a PLAY command string at compile time and generate SpeakerBeep SYSCALLs.
    /// Supports: A-G notes, #/+ sharp, - flat, On octave, Ln length, Tn tempo, Pn pause,
    /// . dotted, > octave up, &lt; octave down, ML/MN/MS articulation.
    /// </summary>
    void GeneratePlayStatement(PlayStatement stmt)
    {
        if (stmt.CommandString is StringLiteral strLit)
        {
            GeneratePlayCompileTime(strLit.Value);
        }
        else
        {
            // Runtime string expression: just beep once (fallback)
            if (currentSubName != null)
                GenerateSubExpression(stmt.CommandString, 0);
            else
                GenerateExpression(stmt.CommandString, 0);
            AddRI(OpCode.MOVE, 0, 7);
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 4) }));
        }
    }

    void GeneratePlayCompileTime(string command)
    {
        // Note frequencies at octave 4
        var baseFreqs = new Dictionary<char, double>
        {
            { 'C', 261.63 }, { 'D', 293.66 }, { 'E', 329.63 },
            { 'F', 349.23 }, { 'G', 392.00 }, { 'A', 440.00 }, { 'B', 493.88 }
        };

        int octave = 4;
        int tempo = 120;
        int noteLength = 4; // quarter note by default
        double articulation = 7.0 / 8.0; // MN normal
        int pos = 0;

        while (pos < command.Length)
        {
            char c = char.ToUpper(command[pos]);

            if (c == 'O' && pos + 1 < command.Length && char.IsDigit(command[pos + 1]))
            {
                octave = command[++pos] - '0';
                pos++;
            }
            else if (c == 'T' && pos + 1 < command.Length && char.IsDigit(command[pos + 1]))
            {
                int t = 0;
                pos++;
                while (pos < command.Length && char.IsDigit(command[pos]))
                    t = t * 10 + (command[pos++] - '0');
                if (t >= 32 && t <= 255) tempo = t;
            }
            else if (c == 'L' && pos + 1 < command.Length && char.IsDigit(command[pos + 1]))
            {
                int l = 0;
                pos++;
                while (pos < command.Length && char.IsDigit(command[pos]))
                    l = l * 10 + (command[pos++] - '0');
                if (l >= 1 && l <= 64) noteLength = l;
            }
            else if (c == 'P' && pos + 1 < command.Length && char.IsDigit(command[pos + 1]))
            {
                // Pause
                int p = 0;
                pos++;
                while (pos < command.Length && char.IsDigit(command[pos]))
                    p = p * 10 + (command[pos++] - '0');
                if (p >= 1 && p <= 64)
                {
                    double qNoteMs = 60000.0 / tempo;
                    int durMs = (int)(qNoteMs * (4.0 / p) + 0.5);
                    if (pos < command.Length && command[pos] == '.')
                    {
                        durMs = (int)(durMs * 1.5);
                        pos++;
                    }
                    if (durMs > 0)
                    {
                        AddRI(OpCode.MOVE, 0, 1);
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, durMs) }));
                        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 52) })); // Sleep
                    }
                }
            }
            else if (c == '>')
            {
                if (octave < 6) octave++;
                pos++;
            }
            else if (c == '<')
            {
                if (octave > 0) octave--;
                pos++;
            }
            else if (c == 'M')
            {
                pos++;
                if (pos < command.Length)
                {
                    switch (char.ToUpper(command[pos]))
                    {
                        case 'N': articulation = 7.0 / 8.0; break;
                        case 'L': articulation = 1.0; break;
                        case 'S': articulation = 3.0 / 4.0; break;
                    }
                    pos++;
                }
            }
            else if (c == 'N' && pos + 1 < command.Length && char.IsDigit(command[pos + 1]))
            {
                // Nn: play note n (0-84, 0=rest), ignoring octave/length
                pos++;
                int n = 0;
                while (pos < command.Length && char.IsDigit(command[pos]))
                    n = n * 10 + (command[pos++] - '0');
                if (n > 0 && n <= 84)
                {
                    n--; // 1 = C0
                    int semitone = n;
                    double freq = 440.0 * Math.Pow(2.0, (semitone - 33) / 12.0);
                    double qNoteMs = 60000.0 / tempo;
                    int durMs = (int)(qNoteMs * (4.0 / noteLength) * articulation + 0.5);
                    if (durMs < 1) durMs = 1;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)(freq + 0.5)) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, durMs) }));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 57) }));
                }
            }
            else if (baseFreqs.ContainsKey(c))
            {
                // Note A-G
                double freq = baseFreqs[c];
                pos++;

                // Check sharp (# or +) or flat (-)
                if (pos < command.Length)
                {
                    if (command[pos] == '#' || command[pos] == '+')
                    {
                        freq *= 1.059463;
                        pos++;
                    }
                    else if (command[pos] == '-')
                    {
                        freq *= 0.943874;
                        pos++;
                    }
                }

                // Adjust for octave: multiply by 2^(octave - 4)
                freq *= Math.Pow(2.0, octave - 4);

                // Parse overridden note length (e.g., A16)
                int effectiveLength = noteLength;
                if (pos < command.Length && char.IsDigit(command[pos]))
                {
                    int l = 0;
                    while (pos < command.Length && char.IsDigit(command[pos]))
                        l = l * 10 + (command[pos++] - '0');
                    if (l >= 1 && l <= 64) effectiveLength = l;
                }

                // Check dotted
                bool dotted = false;
                if (pos < command.Length && command[pos] == '.')
                {
                    dotted = true;
                    pos++;
                }

                double qNoteMs = 60000.0 / tempo;
                int durationMs = (int)(qNoteMs * (4.0 / effectiveLength) * articulation + 0.5);
                if (dotted) durationMs = (int)(durationMs * 1.5);
                if (durationMs < 1) durationMs = 1;

                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)(freq + 0.5)) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, durationMs) }));
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 57) }));
            }
            else
            {
                pos++; // Skip unrecognized character
            }
        }
    }

    void GenerateSoundStatement(SoundStatement stmt)
    {
        // SOUND freq, dur — QBasic: freq in Hz, dur in clock ticks (18.2/s)
        // Evaluate frequency (R0) and duration (R1)
        if (currentSubName != null)
        {
            GenerateSubExpression(stmt.Frequency, 0);
            GenerateSubExpression(stmt.Duration, 1);
        }
        else
        {
            GenerateExpression(stmt.Frequency, 0);
            GenerateExpression(stmt.Duration, 1);
        }

        // Convert duration from clock ticks to ms: R1 = R1 * 55
        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 55) }));
        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
        // R0 = freq, R1 = duration_ms; call SpeakerBeep(freq, dur)
        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 57) }));
    }
    // ==================== REDIM ====================

    void GenerateRedimStatement(RedimStatement stmt)
    {
        // REDIM: update array size and zero-fill elements
        string arrayName = stmt.ArrayName;

        if (!arrayVariables.ContainsKey(arrayName))
        {
            // Array not found - create a new one (simplified)
            return;
        }

        var arrayInfo = arrayVariables[arrayName];

        if (!stmt.Preserve)
        {
            // REDIM without PRESERVE: get new size from expression
            if (currentSubName != null)
                GenerateSubExpression(stmt.NewSize, 0);
            else
                GenerateExpression(stmt.NewSize, 0);

            // Zero-fill loop: from 0 to newsize
            string loop = newLabel();
            string end = newLabel();
            int baseAddr = 8 + arrayInfo.Offset * 4;

            // R0 = newsize, R1 = counter = 0
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loop) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, end) }));
            // arr[i] = 0
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 12) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, baseAddr) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R3") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loop) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, end) }));
        }
        else
        {
            // REDIM PRESERVE: expand to new size keeping existing data, zero-fill new slots
            int oldSize = arrayInfo.Size;
            if (currentSubName != null)
                GenerateSubExpression(stmt.NewSize, 0);
            else
                GenerateExpression(stmt.NewSize, 0);
            // R0 = newsize
            // Copy loop for elements beyond oldSize: zero-fill from oldSize..newsize
            string pLoop = newLabel();
            string pEnd = newLabel();
            // R1 = counter = oldSize
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, oldSize) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, pLoop) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, pEnd) }));
            int baseAddr = 8 + arrayInfo.Offset * 4;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 12) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, baseAddr) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R3") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, pLoop) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, pEnd) }));
        }
    }
    void GeneratePrintUsingStatement(PrintUsingStatement stmt)
    {
        // 瑙ｆ瀽鏍煎紡涓诧紙缂栬瘧鏈燂級
        string formatStr = "";
        if (stmt.Format is StringLiteral strLit)
            formatStr = strLit.Value;
        else if (stmt.Format is Expression expr)
        {
            // 濡傛灉鏍煎紡涓叉槸鍙橀噺锛屽洖閫€鍒伴€愬€艰緭鍑?
            foreach (var value in stmt.Values)
            {
                if (currentSubName != null) GenerateSubExpression(value, 0);
                else GenerateExpression(value, 0);
                if (currentSubName != null) GenerateSubExpression(value, 0);
                else GenerateExpression(value, 0);
            }
            return;
        }

        // 瑙ｆ瀽鏍煎紡妯″紡: 缁熻 # 鏁伴噺
        int intDigits = 0, decDigits = 0;
        bool afterDot = false;
        foreach (char ch in formatStr)
        {
            if (ch == '.') afterDot = true;
            else if (ch == '#') { if (afterDot) decDigits++; else intDigits++; }
        }

        // 鐢熸垚姣忎釜鍊肩殑鏍煎紡鍖栬緭鍑轰唬鐮?
        int valIdx = 0;
        foreach (var value in stmt.Values)
        {
            // 璁＄畻鍊煎埌 R0
            if (currentSubName != null) GenerateSubExpression(value, 0);
            else GenerateExpression(value, 0);

            if (valIdx > 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32)]));
                EmitPrintChar();
            }

            if (intDigits > 0 || decDigits > 0)
            {
                // 鎸夋牸寮忚緭鍑?
                int totalDigits = intDigits + decDigits;
                int divisor = 1;
                for (int i = 0; i < decDigits; i++) divisor *= 10;

                if (decDigits > 0)
                {
                    // 鍚皬鏁? 鍒嗙鏁存暟鍜屽皬鏁伴儴鍒?
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.DIV, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, divisor)]));
                    instructions.Add(new Instruction(OpCode.MOD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, divisor)]));
                    // 鏁存暟閮ㄥ垎鍙冲榻愯緭鍑?
                    EmitPrintInt(); // OutputInt
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 46)])); // '.'
                    EmitPrintChar();
                    // 灏忔暟閮ㄥ垎琛ラ浂
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    if (decDigits > 1)
                    {
                        int pad = 1;
                        for (int i = 1; i < decDigits; i++) pad *= 10;
                        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, pad)]));
                        string skipLabel = newLabel();
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, skipLabel)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 48)])); // '0'
                        EmitPrintChar();
                        instructions.Add(new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, skipLabel)]));
                    }
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    EmitPrintInt();
                }
                else
                {
                    // 绾暣鏁板彸瀵归綈
                    EmitPrintInt();
                }
            }
            else
            {
                EmitPrintInt();
            }
            valIdx++;
        }

        // 鎹㈣
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10)]));
        EmitPrintChar();
    }
}
