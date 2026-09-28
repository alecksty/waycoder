using VMLAssembler;
using CompilerBase;

namespace ForthCompiler
{
    public partial class CodeGenerator
    {
        private object ParseNumberValue(NumberLiteral numLit)
        {
            string text = numLit.Value;
            if (numLit.IsFloat && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                return f;

            bool negative = text.StartsWith("-", StringComparison.Ordinal);
            string unsigned = negative ? text[1..] : text;
            int value;
            if (unsigned.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || unsigned.StartsWith("$", StringComparison.Ordinal))
            {
                string hex = unsigned.StartsWith("$") ? unsigned[1..] : unsigned[2..];
                value = Convert.ToInt32(hex, 16);
            }
            else if (unsigned.StartsWith("0b", StringComparison.OrdinalIgnoreCase) || unsigned.StartsWith("%", StringComparison.Ordinal))
            {
                string bin = unsigned.StartsWith("%") ? unsigned[1..] : unsigned[2..];
                value = Convert.ToInt32(bin, 2);
            }
            else
            {
                value = int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
                return value;
            }

            return negative ? -value : value;
        }

        private void GenerateConstantAccess(ConstantAccess constAccess)
        {
            if (constantValues.TryGetValue(constAccess.Name, out var value))
            {
                if (value is NumberLiteral num)
                {
                    GenerateNumberLiteral(num);
                    return;
                }
                if (value is StringLiteral str)
                {
                    PushStringAddressAndLength($"const_{MangleName(constAccess.Name)}", str.Value);
                    return;
                }
                if (value is IOOperation io && io.Argument is StringLiteral dotStr)
                {
                    PushStringAddressAndLength($"const_{MangleName(constAccess.Name)}", dotStr.Value);
                    return;
                }
                if (value is CharLiteral ch)
                {
                    AddRI(OpCode.MOVE, 0, (int)ch.Value);
                    AddInstruction(OpCode.PUSH, Reg(0));
                    stackPointer++;
                    return;
                }
            }

            AddInstruction(OpCode.MOVE, Reg(0), LabelOp($"const_{MangleName(constAccess.Name)}"));
            AddInstruction(OpCode.PUSH, Reg(0));
            stackPointer++;
        }

        private void GenerateWordDefinition(WordDefinition wordDef)
        {
            // 添加函数注释
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, "; -------------------------------------------"));
            // 生成源函数声明（Forth风格）
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; source   : : {wordDef.Name}"));
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; function : {wordDef.Name}"));
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; return   : stack"));
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, "; --------------------------------------------"));

            // 为词定义创建标签
            string labelName = $"word_{MangleName(wordDef.Name)}";
            AddLabel(labelName);

            // 保存返回地址：CALL 将 RA 推入数据栈，POP 到 R15 避免被数据操作覆盖
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                { new Operand(OperandType.REGISTER, 15) }, instructions.Count));

            // 生成词体
            foreach (var stmt in wordDef.Body)
            {
                GenerateStatement(stmt);
            }

            // 恢复返回地址并返回
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                { new Operand(OperandType.REGISTER, 15) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>(), instructions.Count));
        }

        private void GenerateVariableDefinition(VariableDefinition varDef)
        {
            // 在数据段分配变量空间
            string varLabel = $"var_{MangleName(varDef.Name)}";
            dataSection[varLabel] = 0; // 默认初始化为0
            
            // 推断变量类型
            ForthTypeEnum varType = ForthTypeEnum.Cell; // 默认单元类型
            
            if (varDef.InitialValue != null)
            {
                varType = InferExpressionType(varDef.InitialValue);
            }
            
            // 记录变量类型
            _varTypes[varDef.Name] = varType;
            
            // 如果有初始值，生成初始化代码
            if (varDef.InitialValue != null)
            {
                // 计算初始值
                GenerateStatement(varDef.InitialValue);
                
                // 从栈顶弹出值并存储到变量（使用类型敏感的指令）
                ForthTypeEnum valueType = InferExpressionType(varDef.InitialValue);
                OpCode popOp = GetPopInstruction(valueType);
                OpCode storeOp = GetStoreInstruction(varType);
                
                instructions.Add(new Instruction(popOp, new List<Operand> 
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(storeOp, new List<Operand> 
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, varLabel) }, instructions.Count));
                stackPointer--;
            }
        }

        private void GenerateCreateDefinition(CreateDefinition createDef)
        {
            string varLabel = $"var_{MangleName(createDef.Name)}";
            dataSection[varLabel] = new string(' ', Math.Max(1, createDef.AllotSize));
            _varTypes[createDef.Name] = ForthTypeEnum.Address;
        }
        
        private void GenerateVariableAccess(VariableAccess varAccess)
        {
            // 变量访问：将变量地址压栈（标准Forth语义：VARIABLE名返回地址）
            string varLabel = $"var_{MangleName(varAccess.Name)}";
            
            // 加载变量地址（LEA - Load Effective Address）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> 
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, varLabel) }, instructions.Count));
            
            // 将地址压栈
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> 
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer++;
        }

        private void GenerateWordCall(WordCall wordCall)
        {
            string wordName = wordCall.Name ?? string.Empty;
            string upperName = wordName.ToUpperInvariant();

            if (GenerateFloatWord(upperName) || GenerateFileWord(upperName) || GenerateSystemWord(upperName))
            {
                return;
            }

            // I: DO/LOOP 中返回当前循环索引
            if (Sta.HasLoopLabels && upperName == "I")
            {
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 2)]));
                stackPointer++;
                return;
            }

            // J: outer loop index (for nested DO...LOOP)
            if (Sta.HasLoopLabels && upperName == "J")
            {
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 4)]));
                stackPointer++;
                return;
            }

            // RANDOM: 生成随机数，压栈
            if (string.Equals(wordName, "random", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 50)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                stackPointer++;
                return;
            }

            // NEGATE: 栈顶取反
            if (string.Equals(wordName, "negate", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.NEG, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            // ABS: 栈顶绝对值 (简短内联, Forth栈与C栈约定不兼容)
            if (string.Equals(wordName, "abs", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                string absEnd = NewLabel();
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, absEnd)]));
                instructions.Add(new Instruction(OpCode.NEG, [new Operand(OperandType.REGISTER, 0)]));
                AddLabel(absEnd);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            // FNEGATE: float stack top negate
            if (string.Equals(wordName, "fnegate", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.FPOP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.FNEG, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.FPUSH, [new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            // FABS: float stack top absolute value
            if (string.Equals(wordName, "fabs", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.FPOP, [new Operand(OperandType.REGISTER, 0)]));
                string fabsEnd = NewLabel();
                instructions.Add(new Instruction(OpCode.FCMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, fabsEnd)]));
                instructions.Add(new Instruction(OpCode.FNEG, [new Operand(OperandType.REGISTER, 0)]));
                AddLabel(fabsEnd);
                instructions.Add(new Instruction(OpCode.FPUSH, [new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            // MIN/MAX: 弹出两个值，比较后压栈 (简短内联, Forth栈与C栈约定不兼容)
            if (string.Equals(wordName, "min", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(wordName, "max", StringComparison.OrdinalIgnoreCase))
            {
                bool isMax = string.Equals(wordName, "max", StringComparison.OrdinalIgnoreCase);
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                string pickR1 = NewLabel();
                instructions.Add(new Instruction(isMax ? OpCode.JL : OpCode.JG, [new Operand(OperandType.LABEL, pickR1)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                string mmEnd = NewLabel();
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, mmEnd)]));
                AddLabel(pickR1);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                AddLabel(mmEnd);
                return;
            }

            // 栈操作词
            if (string.Equals(wordName, "dup", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                stackPointer++;
                return;
            }
            if (string.Equals(wordName, "drop", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                stackPointer--;
                return;
            }
            if (string.Equals(wordName, "swap", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                return;
            }
            if (string.Equals(wordName, "over", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                stackPointer++;
                return;
            }
            if (string.Equals(wordName, "rot", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 2)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 2)]));
                return;
            }
            if (string.Equals(wordName, "depth", StringComparison.OrdinalIgnoreCase))
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, stackPointer)]));
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                stackPointer++;
                return;
            }

            // 预定义编译期词: . 打印, CR 换行, EMIT, KEY
            // 这些在 GenerateOperation 中处理

            string labelName = $"word_{MangleName(wordName)}";

            // ⚠ 两条路的**结果约定不同**，判据见 IsUserDefinedWord（CodeGenerator.cs）：
            //   词（本程序 `: … ;` 定义）—— 结果在**数据栈**上，且词会用 R15 存自己的返回地址
            //                                ⇒ 调用前后必须自己保存/恢复 R15；
            //   库（C 函数）        —— 结果在 **R0**，实参在数据栈上（arg1 紧邻栈顶 → 落 R12+12）。
            if (IsUserDefinedWord(wordName))
            {
                // 所有词调用都会覆盖 R15。把当前 R15（返回地址）保存到栈上，
                // 调用返回后恢复。操作：POP R1(arg) PUSH R15 PUSH R1(arg) CALL ... POP R1(res) POP R15 PUSH R1(res)
                {
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));   // 暂存参数
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 15)])); // 保存 R15 到栈
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));  // 恢复参数
                    stackPointer--; // POP then two PUSHes = net +1 on stack
                }

                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, labelName)]));

                {
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));   // 结果
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 15)]));  // 恢复 R15
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));  // 结果放回栈顶
                    // 净效果: 栈不变，R15 恢复
                }
                return;
            }

            // ── 库函数调用（C 约定）────────────────────────────────────────
            //
            // ① **实参区里不能插任何东西**：C 函数按 R12+12 / +16 / +20 … 读实参
            //    （见 `CodeGeneratorBase.GenerateFunctionPrologue` 的帧布局注释），
            //    所以实参必须**原样连着**躺在栈上。原来这里在调用前 `POP R1; PUSH R15; PUSH R1`
            //    往实参区里塞了一个 R15 保存槽 ⇒ **从第 2 个实参起全体偏一格**
            //    （`drift.fth` 的头注释记着这条，探针实测 DRIFT=8 而应 126）。
            // ② **不必在这里保存 R15**：C 函数的序言是 `push R15`、尾声 `pop R15; ret`（自己保存/恢复），
            //    而 `CALL` 本身不碰 R15（`VMLRuntime.ExecuteCall` 只压返回地址）⇒ 库里那条路
            //    R15 天然是完好的（词那条路才需要，见上）。
            // ③ **结果取 R0**：C 约定返回值在 R0。原来推的是 R1（调用前那个栈顶 = 实参本身）
            //    ⇒ 返回值被丢掉、栈顶留下的还是实参（`100 ui_rand .` 打出 100 就是这段）。
            //
            // 栈形状与改动前**一致**（吃掉栈顶一格、放回一个值），所以只修「值对不对」，
            // 不动任何现有程序的栈平衡。⚠ 实参的**清理**仍不在这一层：Forth 前端不知道被调方的
            // 形参个数（库里没有声明），多参与旧行为一样留给调用点自行 `DROP`（见 `drift.fth` 的说明）。
            //
            // ④ **循环状态要自己兜住**：C 约定里 R0–R3 是**被调方随便用**的，而本前端把
            //    DO/LOOP 的循环索引/上限就放在 **R2/R3**（见 `CodeGenerator.Operations.cs` 的 DO 生成）、
            //    `J` 放 R4 —— 这些是**跨语句活着**的。库里一次调用就能踩掉它们
            //    （实测：`ipow` 内部拿 R2 当计数器 ⇒ 外层循环的索引被清零 ⇒ 循环重跑到栈耗尽，
            //     报 `内存错误 … PUSH @R0 — 地址=FFFFFFFC`）。
            //    ⇒ 循环体内调库前后，把 R2/R3/R4 存进**数据段的专用槽**再取回。
            //    ⚠ 用内存槽而不是压栈：压栈会插进实参区（正是上面刚修掉的那个坑）。
            //    ⚠ 单份槽不会自嵌套 —— 库函数不会回调 Forth 代码（词调用那条路不在这里，
            //      它的嵌套要靠别的机制，不在本次改动范围内）。
            bool inLoop = Sta.HasLoopLabels;
            if (inLoop)
            {
                SaveRegToCallSlot(2);
                SaveRegToCallSlot(3);
                SaveRegToCallSlot(4);
            }

            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, labelName)]));

            if (inLoop)
            {
                RestoreRegFromCallSlot(2);
                RestoreRegFromCallSlot(3);
                RestoreRegFromCallSlot(4);
            }
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));    // 吃掉第 1 个实参那格
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));   // 结果（C 约定：R0）
        }

        /// <summary>把寄存器存进「库调用专用」的数据段槽（见 GenerateWordCall 的 ④）。</summary>
        private void SaveRegToCallSlot(int reg)
        {
            string slot = $"__fth_call_r{reg}";
            dataSection.TryAdd(slot, 0);
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, slot), new Operand(OperandType.REGISTER, reg)]));
        }

        /// <summary>把「库调用专用」槽里的值取回寄存器。</summary>
        private void RestoreRegFromCallSlot(int reg)
        {
            string slot = $"__fth_call_r{reg}";
            dataSection.TryAdd(slot, 0);
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, slot)]));
        }

        private bool GenerateFloatWord(string upperName)
        {
            if (upperName == "FLOAT" || upperName == "SFLOAT" || upperName == "DFLOAT")
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, upperName == "DFLOAT" ? 8 : 4) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer++;
                return true;
            }

            if (upperName is "F+" or "F-" or "F*" or "F/")
            {
                instructions.Add(new Instruction(OpCode.FPOP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.FPOP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                OpCode op = upperName switch
                {
                    "F-" => OpCode.FSUB,
                    "F*" => OpCode.FMUL,
                    "F/" => OpCode.FDIV,
                    _ => OpCode.FADD
                };
                instructions.Add(new Instruction(op, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.FPUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer--;
                return true;
            }

            if (upperName is "F=" or "F<" or "F>" or "F<=" or "F>=")
            {
                instructions.Add(new Instruction(OpCode.FPOP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.FPOP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.FCMP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                string trueLabel = NewLabel();
                string endLabel = NewLabel();
                OpCode jump = upperName switch
                {
                    "F<" => OpCode.JL,
                    "F>" => OpCode.JG,
                    "F<=" => OpCode.JLE,
                    "F>=" => OpCode.JGE,
                    _ => OpCode.JE
                };
                instructions.Add(new Instruction(jump, new List<Operand> { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));
                AddLabel(trueLabel);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -1) }, instructions.Count));
                AddLabel(endLabel);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer--;
                return true;
            }

            return false;
        }

        private bool GenerateFileWord(string upperName)
        {
            switch (upperName)
            {
                case "FOPEN":
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // mode
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }, instructions.Count)); // len
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // filename addr
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 110) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer -= 2;
                    return true;
                case "FCLOSE":
                    GenerateOneArgSyscall(111);
                    return true;
                case "FREAD":
                    GenerateThreeArgSyscall(112);
                    return true;
                case "FWRITE":
                    GenerateThreeArgSyscall(113);
                    return true;
                case "FSEEK":
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }, instructions.Count)); // offset
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // handle
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 114) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    return true;
                case "FTELL":
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 2) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 114) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    return true;
            }
            return false;
        }

        private bool GenerateSystemWord(string upperName)
        {
            switch (upperName)
            {
                case "GET-TICK":
                case "MILLISECONDS":
                    EmitGetTick();
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer++;
                    return true;
                case "DATE$":
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 55) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 10) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    stackPointer += 2;
                    return true;
                case "TIME$":
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 56) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 8) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    stackPointer += 2;
                    return true;
                case "BYE":
                    EmitExit();
                    return true;
            }
            return false;
        }

        private void GenerateOneArgSyscall(int syscall)
        {
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, syscall) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        }

        private void GenerateThreeArgSyscall(int syscall)
        {
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, syscall) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer -= 2;
        }

        private void GenerateNumberLiteral(NumberLiteral numLit)
        {
            object value = ParseNumberValue(numLit);
            if (value is float floatValue)
            {
                instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, floatValue) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.FPUSH, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer++;
                return;
            }

            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)value) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer++;
        }

        private void GenerateStringLiteral(StringLiteral strLit)
        {
            string label = $"str_{dataSection.Count}";
            dataSection[label] = strLit.Value;
            PushStringAddressAndLength(label, strLit.Value);
        }

        private void PushStringAddressAndLength(string label, string value)
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> 
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> 
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, value?.Length ?? 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer += 2;
        }

        // GenerateAsmStatement() 已移除 — asm() 仅限 C/ObjC/C++ 语言
        // Forth 通过 Lib/shared/vmlsys.c 调用系统功能

    }
}
