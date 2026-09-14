using CompilerBase;
using VMLAssembler;

namespace BasicCompiler
{
    public partial class CodeGenerator
    {

        private void GenerateOnErrorStatement(OnErrorStatement stmt)
        {
            // Store error handler address at 0x6FC0
            // 0 = no handler, otherwise store a label reference
            if (stmt.DisableHandler)
            {
                // ON ERROR GOTO 0: disable handler
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC0) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
            else if (stmt.ResumeNext)
            {
                // ON ERROR RESUME NEXT: store -1 as flag for "skip and continue"
                AddRI(OpCode.MOVE, 0, -1);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC0) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
            else if (!string.IsNullOrEmpty(stmt.ErrorHandlerLabel))
            {
                // ON ERROR GOTO label: store the label address at 0x6FC0
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, stmt.ErrorHandlerLabel.ToLower()) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC0) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
            else if (stmt.ErrorHandlerLine > 0)
            {
                // ON ERROR GOTO line_number: store the line label
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC0) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
        }

        private void GenerateResumeStatement(ResumeStatement stmt)
        {
            if (stmt.ResumeNext)
            {
                // RESUME NEXT: jump to next line
                // Store -2 flag at 0x6FC4 to indicate RESUME NEXT was executed
                AddRI(OpCode.MOVE, 0, -2);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC4) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
            else
            {
                // RESUME: return from error handler (jump back to error address)
                // Store -3 flag at 0x6FC4 to indicate RESUME was executed
                AddRI(OpCode.MOVE, 0, -3);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FC4) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
        }

        private void GenerateReadStatement(ReadStatement stmt)
        {
            foreach (var variable in stmt.Variables)
            {
                // Load data pointer from 0x6FD0
                AddRI(OpCode.MOVE, 0, 0x6FD0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0") }));
                // Compute address: dynamic_base + ptr * 4
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
                instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
                EmitStaticAddr(1, STATIC_DATA_OFFSET);
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
                // Load data value into R1
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0") }));
                // Store to variable
                string varName = variable.Name;
                BasicType varType = GetVariableType(varName);
                if (currentSubName != null)
                {
                    if (varType == BasicType.String)
                        GenerateSubVariableStore(varName.ToLower(), 1);
                    else
                        GenerateSubVariableStore(varName.ToLower(), 1);
                }
                else
                {
                    int varOffset = variables[varName] * 4;
                    // 字符串变量：存储地址指针；数值变量：存储值
                    OpCode storeOp = varType == BasicType.String ? OpCode.MOVE : GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, 1) }));
                }
                // Increment data pointer
                AddRI(OpCode.MOVE, 0, 0x6FD0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R0") }));
            }
        }

        private void GenerateRestoreStatement()
        {
            // Reset data pointer to 0
            AddRI(OpCode.MOVE, 0, 0);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FD0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R14") }));
        }

        private void GenerateSleepStatement(SleepStatement stmt)
        {
            if (stmt.HasSeconds && stmt.Seconds != null)
            {
                if (currentSubName != null)
                    GenerateSubExpression(stmt.Seconds, 0);
                else
                    GenerateExpression(stmt.Seconds, 0);
            }
            else
            {
                AddRI(OpCode.MOVE, 0, 1);
            }
            // SLEEP: loop delay using R0 as count
            string slStart = GenerateLabel();
            string slEnd = GenerateLabel();
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, slStart) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, slEnd) }));
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>())); // delay
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, slStart) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, slEnd) }));
        }

        // GenerateAsmStatement() 已移除 — asm() 仅限 C/ObjC/C++ 语言
        // BASIC 通过 Lib/shared/vmlsys.c 调用系统功能

        private void GeneratePokeStatement(PokeStatement stmt)
        {
            // 内联 POKE: STOREB value, [address]
            GenerateExpression(stmt.Address, 1);   // addr → R1
            GenerateExpression(stmt.Value, 0);     // val → R0
            EmitSaveRegisters(2, 3, 4, 5);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            EmitRestoreRegisters(2, 3, 4, 5);
        }

        private void GenerateSubPokeStatement(PokeStatement stmt)
        {
            GenerateSubExpression(stmt.Address, 1);
            GenerateSubExpression(stmt.Value, 0);
            EmitSaveRegisters(2, 3, 4, 5);
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            EmitRestoreRegisters(2, 3, 4, 5);
        }

        private void GenerateSwapStatement(SwapStatement stmt)
        {
            // SWAP: load var1 to R0, var2 to R1, store back swapped
            if (stmt.Var1 is Identifier id1 && stmt.Var2 is Identifier id2)
            {
                string n1 = id1.Name.ToLower();
                string n2 = id2.Name.ToLower();
                if (variables.ContainsKey(n1) && variables.ContainsKey(n2))
                {
                    int o1 = variables[n1] * 4;
                    int o2 = variables[n2] * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{8 + o1}") }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12+{8 + o2}") }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12+{8 + o1}") }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{8 + o2}") }));
                }
            }
        }

        private void GeneratePrintStatement(PrintStatement stmt)
        {
            foreach (var expr in stmt.Expressions)
            {
                if (expr is StringLiteral strLiteral)
                {
                    // 字符串字面量: 始终 stdout + 条件 VGA
                    string strLabel = $"str_data_{GenerateLabel()}";
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, strLabel) }, instructions.Count));
                    EmitPrintString();
                    dataSection[strLabel] = new DataString(strLiteral.Value);
                }
                else if (expr is FunctionCallExpression funcCall &&
                         (funcCall.FunctionName.ToLowerInvariant() == "chr$" ||
                          funcCall.FunctionName.ToLowerInvariant() == "chr"))
                {
                    // CHR$(n): 直接输出字符码
                    if (currentSubName != null)
                        GenerateSubExpression(expr, 0);
                    else
                        GenerateExpression(expr, 0);
                    EmitPrintChar();
                }
                else
                {
                    // Compute expression, detect if it returns a string
                    BasicType exprType = InferExpressionType(expr);
                    if (currentSubName != null)
                        GenerateSubExpression(expr, 0);
                    else
                        GenerateExpression(expr, 0);

                    if (exprType == BasicType.String)
                    {
                        // R0 = string pointer — 始终 stdout + 条件 VGA
                        EmitPrintString();
                    }
                    else
                    {
                        // 浮点/双精度 → 整数转换（GenerateIntegerToString 从整数寄存器读取）
                        if (exprType == BasicType.Double || exprType == BasicType.Long)
                        {
                            instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
                        }
                        else if (exprType == BasicType.Single)
                        {
                            instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
                        }
                        // 整数→字符串并输出
                        GenerateIntegerToString(0);
                    }
                }
            }
            // 输出换行符: 始终 stdout + 条件 VGA
            AddRI(OpCode.MOVE, 0, 10);
            EmitPrintChar();
        }

        private void GenerateIntegerToString(int reg)
        {
            // 数字转字符串
            string startLabel = GenerateLabel();
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();
            string printLoopLabel = GenerateLabel();
            string printEndLabel = GenerateLabel();

            // 使用 R1 作为临时寄存器保存值
            if (reg != 1)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, reg) }));
            }

            // 检查是否为负数
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));
            // 输出负号
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'-') }));
            EmitPrintChar();
            // 取绝对值
            instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));
            // 检查是否为零
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            string entryLabel = GenerateLabel();
            // JNE 跳转到 entryLabel（会先初始化R4，再进入循环体）
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, entryLabel) }));
            // 输出零
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'0') }));
            EmitPrintChar();
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            // 循环入口：初始化计数器R4 = 0，然后进入循环体
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, entryLabel) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            // 计算余数 (R1 % 10)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.MOD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 10) }));
            // 转换为字符并保存到栈
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, (int)'0') }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));
            // 计数器+1
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
            // 计算商 (R1 / 10)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 10) }));
            instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 3) }));
            // 检查是否为零
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // 输出栈中的字符（R4 = 字符数）
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, printLoopLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, printEndLabel) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            EmitPrintChar();
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, printLoopLabel) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, printEndLabel) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        private void GenerateStringToInteger(int resultReg)
        {
            // 字符串转数字：使用 SYSCALL 5 逐个输入字符（R0 用于输入/输出）
            // 结果累积在 resultReg（默认=3），避免被 SYSCALL 5 的返回值覆盖
            // 最终 MOVE R0, resultReg 将结果放入 R0 供调用者存储
            string startLabel = GenerateLabel();
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();
            string negLabel = GenerateLabel();

            // 初始化结果为 0（使用 resultReg 而非 R0，避免被 InputChar 覆盖）
            int resReg = 3; // use R3 for accumulated result (R0 is used by InputChar)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, resReg), new Operand(OperandType.IMMEDIATE, 0) }));
            // 使用 R1 作为负数标记
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));
            // 输入一个字符（系统调用 5），R0=1 阻塞模式
            AddRI(OpCode.MOVE, 0, 1);
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 5) }));
            // 检查是否为换行符或回车符
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10) }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 13) }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            // 检查是否为负号
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'-') }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            // 处理负号，标记为负数
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            // 检查是否为数字字符 (0-9)
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'0') }));
            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'9') }));
            instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            // 转换为数字: digit = char - '0'
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'0') }));
            // 保存 digit，然后 结果 = 结果 * 10 + digit (R0=digit from InputChar, R3=accumulated result)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) })); // R2 = digit
            AddRI(OpCode.MOVE, 0, 10);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, resReg), new Operand(OperandType.REGISTER, 0) })); // res *= 10
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, resReg), new Operand(OperandType.REGISTER, 2) })); // res += digit
            // 继续输入下一个字符
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            // 检查是否为负数
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, negLabel) }));
            // 取负
            instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, resReg) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, negLabel) }));
            // Move result to R0 for caller (INPUT stores R0 to variable)
            if (resReg != 0)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, resReg) }));
        }

        private void GenerateInputStatement(InputStatement stmt)
        {
            foreach (var variable in stmt.Variables)
            {
                BasicType varType = GetVariableType(variable.Name.ToLower());
                bool isStringVar = varType == BasicType.String;

                if (isStringVar)
                {
                    // 字符串 INPUT: 逐字符读取到缓冲区, 存储指针到变量
                    string strBufLabel = $"__input_str_{labelCounter++}";
                    // Allocate 256-byte buffer in data section (filled at runtime)
                    for (int bi = 0; bi < 256; bi++)
                        dataSection[$"{strBufLabel}_{bi}"] = 0;
                    string strLoop = NewLabel(), strEnd = NewLabel();
                    int bufReg = 2, idxReg = 3, charReg = 4;
                    // Initialize: LEA bufReg → buffer, idxReg = 0
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, bufReg), new Operand(OperandType.LABEL, strBufLabel)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, idxReg), new Operand(OperandType.IMMEDIATE, 0)]));
                    AddLabel(strLoop);
                    // Read char via SYSCALL 5 (blocking)
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 5)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, charReg), new Operand(OperandType.REGISTER, 0)]));
                    // Check for newline (10) or CR (13) → end
                    instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, charReg), new Operand(OperandType.IMMEDIATE, 10)]));
                    instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, strEnd)]));
                    instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, charReg), new Operand(OperandType.IMMEDIATE, 13)]));
                    instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, strEnd)]));
                    // Store char to buffer[bufReg + idxReg]; idxReg++
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, bufReg), new Operand(OperandType.REGISTER, idxReg)]));
                    instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, charReg)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, idxReg), new Operand(OperandType.REGISTER, idxReg), new Operand(OperandType.IMMEDIATE, 1)]));
                    // Prevent buffer overflow: if idxReg >= 255, end
                    instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, idxReg), new Operand(OperandType.IMMEDIATE, 255)]));
                    instructions.Add(new Instruction(OpCode.JL, [new Operand(OperandType.LABEL, strLoop)]));
                    AddLabel(strEnd);
                    // Null-terminate: STOREB [bufReg + idxReg], 0
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, bufReg), new Operand(OperandType.REGISTER, idxReg)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)]));
                    // R0 = buffer address (for store to variable)
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, bufReg)]));
                }
                else
                {
                    // 数字 INPUT: 逐个输入字符并转换为数字
                    GenerateStringToInteger(0);
                }

                // 存储到变量
                OpCode storeOp = GetStoreInstruction(varType);
                if (currentSubName != null)
                {
                    // 在SUB/FUNCTION内部
                    if (currentLocalVars.ContainsKey(variable.Name.ToLower()))
                    {
                        // 局部变量
                        int slot = currentLocalVars[variable.Name.ToLower()];
                        int offset = -(slot + 1) * 4;
                        instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                    }
                    else
                    {
                        // 检查是否是参数
                        int paramIdx = FindParameterIndex(variable.Name.ToLower());
                        if (paramIdx >= 0)
                        {
                            int offset = 8 + paramIdx * 4;
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                        }
                        else if (variables.ContainsKey(variable.Name.ToLower()))
                        {
                            int varOffset = variables[variable.Name.ToLower()] * 4;
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, 0) }));
                        }
                        else
                        {
                            throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{variable.Name}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                        }
                    }
                }
                else
                {
                    // 全局变量
                    int varOffset = variables[variable.Name] * 4;
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, 0) }));
                }
            }
        }

    }
}
