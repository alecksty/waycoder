using VMLAssembler;
using CompilerBase;

namespace LadderCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(ContactNode node)
        {
            // 加载接触点变量到R0
            // 如果是输入变量，从输入区域读取
            if (_inputVars.Contains(node.Variable))
            {
                ReadInput(node.Variable);
            }
            else
            {
                LoadVariable(node.Variable);
            }
            
            // 如果是常闭接触点，取反
            if (!node.NormallyOpen)
                EmitCompareToBool(() => {}, OpCode.JE);
        }

        public void Visit(CoilNode node)
        {
            // 线圈输出：R0的值赋给线圈变量
            switch (node.Type)
            {
                case CoilType.Normal:
                    // 如果是输出变量，写入输出区域
                    if (_outputVars.Contains(node.Variable))
                    {
                        WriteOutput(node.Variable);
                    }
                    else
                    {
                        StoreVariable(node.Variable);
                    }
                    break;
                case CoilType.Set:
                    // SET: 如果R0非零，设置变量为1
                    AddInstruction(OpCode.CMP,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0));
                    string setSkip = NewLabel("set_skip");
                    string setEnd = NewLabel("set_end");
                    AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, setSkip));
                    AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    if (_outputVars.Contains(node.Variable))
                        WriteOutput(node.Variable);
                    else
                        StoreVariable(node.Variable);
                    AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, setEnd));
                    labels[setSkip] = instructions.Count;
                    labels[setEnd] = instructions.Count;
                    break;
                case CoilType.Reset:
                    // RESET: 如果R0非零，重置变量为0
                    AddInstruction(OpCode.CMP,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0));
                    string resetSkip = NewLabel("reset_skip");
                    string resetEnd = NewLabel("reset_end");
                    AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, resetSkip));
                    AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                    if (_outputVars.Contains(node.Variable))
                        WriteOutput(node.Variable);
                    else
                        StoreVariable(node.Variable);
                    AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, resetEnd));
                    labels[resetSkip] = instructions.Count;
                    labels[resetEnd] = instructions.Count;
                    break;
                default:
                    if (_outputVars.Contains(node.Variable))
                        WriteOutput(node.Variable);
                    else
                        StoreVariable(node.Variable);
                    break;
            }
        }

        public void Visit(FunctionBlockNode node)
        {
            // 函数块调用 - 根据名称生成相应代码
            // 实例名称可能是 "DelayTimer"，需要从VAR声明中推断类型
            // 或者从函数块符号中提取类型（如TON、TOF等）
            string instanceName = node.Name;
            string fbType = DetectFunctionBlockType(node);
            
            switch (fbType)
            {
                case "ADD":
                    GenerateArithmeticBlock(node, OpCode.ADD);
                    break;
                case "SUB":
                    GenerateArithmeticBlock(node, OpCode.SUB);
                    break;
                case "MUL":
                    GenerateArithmeticBlock(node, OpCode.MUL);
                    break;
                case "DIV":
                    GenerateArithmeticBlock(node, OpCode.DIV);
                    break;
                case "MOD":
                    GenerateArithmeticBlock(node, OpCode.MOD);
                    break;
                case "MOVE":
                    GenerateMoveBlock(node);
                    break;
                case "SEL":
                case "MUX":
                case "LIMIT":
                case "MAX":
                case "MIN":
                case "ABS":
                case "SQRT":
                case "LN":
                case "LOG":
                case "EXP":
                case "SIN":
                case "COS":
                case "TAN":
                case "ASIN":
                case "ACOS":
                case "ATAN":
                    GenerateStandardFunctionBlock(node, fbType);
                    break;
                case "SHL":
                    GenerateArithmeticBlock(node, OpCode.SHL);
                    break;
                case "SHR":
                    GenerateArithmeticBlock(node, OpCode.SHR);
                    break;
                case "TON":
                    GenerateTON(node, instanceName);
                    break;
                case "TOF":
                    GenerateTOF(node, instanceName);
                    break;
                case "TP":
                    GenerateTP(node, instanceName);
                    break;
                case "CTU":
                    GenerateCTU(node, instanceName);
                    break;
                case "CTD":
                    GenerateCTD(node, instanceName);
                    break;
                case "CTUD":
                    GenerateCTUD(node, instanceName);
                    break;
                case "EQ":
                    GenerateComparisonBlock(node, OpCode.JE);
                    break;
                case "NE":
                    GenerateComparisonBlock(node, OpCode.JNE);
                    break;
                case "GT":
                    GenerateComparisonBlock(node, OpCode.JG);
                    break;
                case "LT":
                    GenerateComparisonBlock(node, OpCode.JL);
                    break;
                case "GE":
                    GenerateComparisonBlock(node, OpCode.JGE);
                    break;
                case "LE":
                    GenerateComparisonBlock(node, OpCode.JLE);
                    break;
                case string name when name.Contains("_TO_"):
                    GenerateConversionBlock(node, fbType);
                    break;
                default:
                    // 未知函数块，跳过 (待实现)
                    break;
            }
        }

        /// <summary>
        /// 检测函数块类型
        /// 从实例名称推断类型（通过检查_allVars中的类型声明）
        /// 或从参数中推断
        /// </summary>
        private string DetectFunctionBlockType(FunctionBlockNode node)
        {
            // 首先检查是否有显式的类型信息
            // 如果实例名称是 "DelayTimer"，查找VAR声明中的类型
            foreach (var varDecl in _allVars)
            {
                if (varDecl.Name == node.Name)
                {
                    return varDecl.Type.ToUpper();
                }
            }
            
            // 如果找不到，尝试从名称推断
            string upperName = node.Name.ToUpper();
            if (upperName.Contains("TON")) return "TON";
            if (upperName.Contains("TOF")) return "TOF";
            if (upperName.Contains("TP") && !upperName.Contains("TON") && !upperName.Contains("TOF")) return "TP";
            if (upperName.Contains("CTU") && !upperName.Contains("CTUD")) return "CTU";
            if (upperName.Contains("CTD") && !upperName.Contains("CTUD")) return "CTD";
            if (upperName.Contains("CTUD")) return "CTUD";
            if (upperName.Contains("ADD")) return "ADD";
            if (upperName.Contains("SUB")) return "SUB";
            if (upperName.Contains("MUL")) return "MUL";
            if (upperName.Contains("DIV")) return "DIV";
            if (upperName.Contains("MOD")) return "MOD";
            if (upperName.Contains("MOVE") || upperName.Contains("MOV")) return "MOVE";
            if (upperName.Contains("LIMIT")) return "LIMIT";
            if (upperName.Contains("SEL")) return "SEL";
            if (upperName.Contains("MUX")) return "MUX";
            if (upperName.Contains("MAX")) return "MAX";
            if (upperName.Contains("MIN")) return "MIN";
            if (upperName.Contains("ABS")) return "ABS";
            if (upperName.Contains("SQRT")) return "SQRT";
            if (upperName.Contains("LN")) return "LN";
            if (upperName.Contains("LOG")) return "LOG";
            if (upperName.Contains("EXP")) return "EXP";
            if (upperName.Contains("SIN")) return "SIN";
            if (upperName.Contains("COS")) return "COS";
            if (upperName.Contains("TAN")) return "TAN";
            if (upperName.Contains("ASIN")) return "ASIN";
            if (upperName.Contains("ACOS")) return "ACOS";
            if (upperName.Contains("ATAN")) return "ATAN";
            
            return "UNKNOWN";
        }

        private void GenerateMoveBlock(FunctionBlockNode node)
        {
            // MOVE: IN -> OUT
            if (node.Parameters.TryGetValue("IN", out var input))
                Visit(input);
            // 将结果存储到 OUT 变量
            if (node.Outputs.TryGetValue("OUT", out var outVar) && !string.IsNullOrEmpty(outVar))
                StoreVariable(outVar);
        }

        private void GenerateConversionBlock(FunctionBlockNode node, string fbType)
        {
            // IEC 61131-3 类型转换: BOOL_TO_INT, INT_TO_DINT, DINT_TO_BOOL 等
            // 获取输入
            if (!node.Parameters.TryGetValue("IN", out var input))
            {
                // 尝试第一个参数作为输入
                foreach (var param in node.Parameters.Values)
                {
                    input = param;
                    break;
                }
            }
            if (input == null) return;

            Visit(input);

            // 解析源类型和目标类型
            var parts = fbType.Split(new[] { "_TO_" }, StringSplitOptions.None);
            if (parts.Length != 2) return;

            string srcType = parts[0];
            string dstType = parts[1];

            // 在 VML 中，大多数类型转换只需保留 R0 的值
            // 需要截断或符号扩展的情况
            var srcLadderType = GetLadderTypeEnum(srcType);
            var dstLadderType = GetLadderTypeEnum(dstType);

            int srcSize = GetTypeSize(srcLadderType);
            int dstSize = GetTypeSize(dstLadderType);

            if (srcSize > dstSize)
            {
                // 截断: 高位清零
                uint mask = (1u << (dstSize * 8)) - 1;
                AddInstruction(OpCode.AND, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, (int)mask) });
            }
            // 符号扩展: 对于有符号类型且目标更大的情况
            else if (dstSize > srcSize && IsSignedType(srcLadderType))
            {
                // VML 使用 32 位寄存器，小类型向大类型转换只需保持值
                // 值已经在 R0 中，无需额外操作
            }
            // 对于 BOOL 类型转换: 强制为 0 或 1
            if (srcType == "BOOL" || dstType == "BOOL")
            {
                // 如果是转换为 BOOL，将非零值归一化
                if (dstType == "BOOL")
                {
                    string labelNonZero = NewLabel("CV");
                    string labelEnd = NewLabel("CV");
                    AddInstruction(OpCode.JNZ, new Operand(OperandType.LABEL, labelNonZero));
                    // R0 已经是 0，跳转到结束
                    AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, labelEnd));
                    AddInstruction(OpCode.LABEL, new Operand(OperandType.LABEL, labelNonZero));
                    AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    AddInstruction(OpCode.LABEL, new Operand(OperandType.LABEL, labelEnd));
                }
            }
        }

        private bool IsSignedType(LadderTypeEnum type)
        {
            return type == LadderTypeEnum.Int
                || type == LadderTypeEnum.DInt
                || type == LadderTypeEnum.LInt;
        }

        private void GenerateStandardFunctionBlock(FunctionBlockNode node, string name)
        {
            var call = new CallNode { FunctionName = name, Line = node.Line, Column = node.Column };
            foreach (var preferred in new[] { "G", "K", "MN", "IN", "MX", "IN0", "IN1", "IN2", "IN3", "IN4" })
            {
                if (node.Parameters.TryGetValue(preferred, out var arg))
                    call.Arguments.Add(arg);
            }

            if (call.Arguments.Count == 0)
            {
                foreach (var arg in node.Parameters.Values)
                    call.Arguments.Add(arg);
            }

            Visit(call);
        }

        private void GenerateArithmeticBlock(FunctionBlockNode node, OpCode op)
        {
            // 获取参数
            if (node.Parameters.TryGetValue("IN1", out var in1) && node.Parameters.TryGetValue("IN2", out var in2))
            {
                // 推断参数类型
                LadderTypeEnum in1Type = InferExpressionType(in1);
                LadderTypeEnum in2Type = InferExpressionType(in2);
                LadderTypeEnum resultType = in1Type; // 使用第一个参数的类型作为结果类型
                
                // 获取正确的压栈/弹栈指令
                OpCode pushOp = GetPushInstruction(resultType);
                OpCode popOp = GetPopInstruction(resultType);
                
                Visit(in1);
                AddInstruction(pushOp, new Operand(OperandType.REGISTER, 0));
                Visit(in2);
                AddInstruction(popOp, new Operand(OperandType.REGISTER, 1));
                
                // 根据类型和操作符获取正确的算术指令
                string opStr = op switch
                {
                    OpCode.ADD => "+",
                    OpCode.SUB => "-",
                    OpCode.MUL => "*",
                    OpCode.DIV => "/",
                    _ => "+"
                };
                OpCode arithmeticOp = GetArithmeticInstruction(opStr, resultType);
                
                AddInstruction(arithmeticOp,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 1));
            }
        }

        private void GenerateComparisonBlock(FunctionBlockNode node, OpCode branchOp)
        {
            if (node.Parameters.TryGetValue("IN1", out var in1) && node.Parameters.TryGetValue("IN2", out var in2))
            {
                // 加载两个操作数
                Visit(in1);
                AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                Visit(in2);
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
                EmitBoolFromBranch(branchOp);
            }
        }

        // 定时器/计数器状态跟踪
        private readonly Dictionary<string, string> _timerPrevInputs = new(); // 定时器实例 -> 前一状态变量名
        private readonly Dictionary<string, string> _counterPrevInputs = new(); // 计数器实例 -> 前一状态变量名

        private string GetTimerPrevVar(string instanceName)
        {
            if (!_timerPrevInputs.TryGetValue(instanceName, out var varName))
            {
                varName = $"_timer_prev_{instanceName}";
                _timerPrevInputs[instanceName] = varName;
                dataSection[varName] = 0;
            }
            return varName;
        }

        private string GetCounterPrevVar(string instanceName)
        {
            if (!_counterPrevInputs.TryGetValue(instanceName, out var varName))
            {
                varName = $"_counter_prev_{instanceName}";
                _counterPrevInputs[instanceName] = varName;
                dataSection[varName] = 0;
            }
            return varName;
        }

        /// <summary>
        /// 加载变量或立即值到R0
        /// </summary>
        private void LoadVariableOrValue(string nameOrValue)
        {
            if (int.TryParse(nameOrValue, out int val))
            {
                // 对于立即值，使用默认的MOVE指令
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, val));
            }
            else if (dataSection.ContainsKey(nameOrValue))
            {
                // 获取变量类型并使用正确的加载指令
                LadderTypeEnum varType = _varTypes.ContainsKey(nameOrValue) ? _varTypes[nameOrValue] : LadderTypeEnum.Bool;
                OpCode loadOp = GetLoadInstruction(varType);
                
                AddInstruction(loadOp,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.LABEL, nameOrValue));
            }
            else
            {
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0));
            }
        }

        /// <summary>
        /// 从函数块参数获取变量名或字面值
        /// </summary>
        private string GetVariableOrLiteral(FunctionBlockNode node, string paramName)
        {
            if (node.Parameters.TryGetValue(paramName, out var expr))
            {
                if (expr is LiteralNode lit)
                {
                    if (lit.Value is bool b) return b ? "1" : "0";
                    if (lit.Value is int i) return i.ToString();
                    if (lit.Value is string s)
                    {
                        if (int.TryParse(s, out int v)) return v.ToString();
                        return s;
                    }
                }
                if (expr is IdentifierNode id) return id.Name;
            }
            return "0";
        }

        /// <summary>
        /// 获取时间参数(毫秒)
        /// </summary>
        private string GetTimeParameter(FunctionBlockNode node, string paramName)
        {
            if (node.Parameters.TryGetValue(paramName, out var expr))
            {
                if (expr is LiteralNode lit && lit.Value is string s)
                {
                    int ms = ParseTimeValue(s);
                    return ms.ToString();
                }
                // 处理标识符形式的时间值 (如 T#5S 被解析为标识符 "T")
                if (expr is IdentifierNode id)
                {
                    // 尝试将标识符名作为时间值解析
                    int ms = ParseTimeValue(id.Name);
                    if (ms > 0)
                        return ms.ToString();
                }
            }
            return "1000"; // 默认1秒
        }

        /// <summary>
        /// 解析IEC时间值 (T#5S, T#100MS, T#1M等) 转换为毫秒
        /// 也支持纯数字(直接表示毫秒)
        /// </summary>
        private int ParseTimeValue(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr))
                return 1000;

            timeStr = timeStr.Trim().ToUpper();
            if (timeStr.StartsWith("T#"))
                timeStr = timeStr.Substring(2);
            else if (timeStr.StartsWith("TIME#"))
                timeStr = timeStr.Substring(5);

            // 如果是纯数字，直接作为毫秒
            if (int.TryParse(timeStr, out int directMs))
                return directMs;

            int ms = 0;
            int pos = 0;
            while (pos < timeStr.Length)
            {
                int numStart = pos;
                while (pos < timeStr.Length && char.IsDigit(timeStr[pos]))
                    pos++;
                if (pos == numStart) break;
                
                int value = int.Parse(timeStr.Substring(numStart, pos - numStart));
                
                if (pos < timeStr.Length)
                {
                    char unit = timeStr[pos];
                    switch (unit)
                    {
                        case 'D': ms += value * 86400000; break;
                        case 'H': ms += value * 3600000; break;
                        case 'S': ms += value * 1000; break;
                        case 'M': // 分钟或毫秒
                            if (pos + 1 < timeStr.Length && timeStr[pos + 1] == 'S')
                            {
                                ms += value; // 毫秒
                                pos++; // 跳过S
                            }
                            else
                            {
                                ms += value * 60000; // 分钟
                            }
                            break;
                    }
                    pos++;
                }
            }

            return ms > 0 ? ms : 1000;
        }

        /// <summary>
        /// 生成TON(接通延时定时器)代码
        /// </summary>
        private void GenerateTON(FunctionBlockNode node, string instanceName)
        {
            // TON 接通延时: IN 上升沿开始计时，ET 达到 PT 后 Q=1
            // 使用 SYSCALL 53 (GetTick) 获取真实毫秒时间
            string inVar = GetVariableOrLiteral(node, "IN");
            string ptMs = GetTimeParameter(node, "PT");
            string qVar = $"{instanceName}.Q";
            string etVar = $"{instanceName}.ET";
            string startVar = $"{instanceName}.startTime";
            string prevVar = GetTimerPrevVar(instanceName);

            dataSection[qVar] = 0;
            dataSection[etVar] = 0;
            dataSection[startVar] = 0;

            LoadVariableOrValue(inVar);

            string inFalse = NewLabel("ton_in_false");
            string tonCompute = NewLabel("ton_compute");
            string tonEnd = NewLabel("ton_end");

            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, inFalse));

            // IN=true: 检测上升沿 (prev=0, now=1)
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(prevVar);
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
            string noRising = NewLabel("ton_no_rising");
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, noRising));
            // 上升沿: 记录 startTime
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, startVar));

            labels[noRising] = instructions.Count;
            // 恢复 IN 值到 R0
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 0));

            // ET = GetTick() - startTime
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.LABEL, startVar));
            AddInstruction(OpCode.SUB, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, etVar));

            // ET >= PT ?
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.IMMEDIATE, int.Parse(ptMs)));
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 2));
            string tonQTrue = NewLabel("ton_q_true");
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, tonQTrue));

            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tonEnd));

            labels[tonQTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tonEnd));

            labels[inFalse] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, etVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, startVar));

            labels[tonEnd] = instructions.Count;
            LoadVariableOrValue(inVar);
            StoreVariable(prevVar);

            LoadVariable(qVar);
        }

        /// <summary>
        /// 生成TOF(断开延时定时器)代码
        /// </summary>
        private void GenerateTOF(FunctionBlockNode node, string instanceName)
        {
            // TOF 断开延时: IN 下降沿开始计时，ET 达到 PT 后 Q=0
            // 使用 SYSCALL 53 (GetTick) 获取真实毫秒时间
            string inVar = GetVariableOrLiteral(node, "IN");
            string ptMs = GetTimeParameter(node, "PT");
            string qVar = $"{instanceName}.Q";
            string etVar = $"{instanceName}.ET";
            string startVar = $"{instanceName}.startTime";
            string prevVar = GetTimerPrevVar(instanceName);

            dataSection[qVar] = 0; // TOF Q 初始等于 IN 状态
            dataSection[etVar] = 0;
            dataSection[startVar] = -1; // -1 确保首次扫描下降沿检测正常工作

            LoadVariableOrValue(inVar);

            string inTrue = NewLabel("tof_in_true");
            string tofEnd = NewLabel("tof_end");

            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, inTrue));

            // IN=false: 检测下降沿 (prev=1, now=0)
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(prevVar);
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
            string noFalling = NewLabel("tof_no_falling");
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, noFalling));
            // 下降沿: 记录 startTime
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, startVar));

            labels[noFalling] = instructions.Count;
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 0));

            // ET = GetTick() - startTime
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.LABEL, startVar));
            AddInstruction(OpCode.SUB, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, etVar));

            // ET >= PT ?
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.IMMEDIATE, int.Parse(ptMs)));
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 2));
            string tofQFalse = NewLabel("tof_q_false");
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, tofQFalse));

            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tofEnd));

            labels[tofQFalse] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tofEnd));

            labels[inTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, etVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, startVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));

            labels[tofEnd] = instructions.Count;
            LoadVariableOrValue(inVar);
            StoreVariable(prevVar);

            LoadVariable(qVar);
        }

        /// <summary>
        /// 生成TP(脉冲定时器)代码
        /// </summary>
        private void GenerateTP(FunctionBlockNode node, string instanceName)
        {
            string inVar = GetVariableOrLiteral(node, "IN");
            string ptMs = GetTimeParameter(node, "PT");
            string qVar = $"{instanceName}.Q";
            string etVar = $"{instanceName}.ET";
            string startVar = $"{instanceName}.startTime";
            string prevVar = GetTimerPrevVar(instanceName);

            dataSection[qVar] = 0;
            dataSection[etVar] = 0;
            dataSection[startVar] = 0;

            LoadVariableOrValue(inVar);

            string tpEnd = NewLabel("tp_end");
            string tpTick = NewLabel("tp_tick");

            // 检查上升沿: IN=true && prev=false
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(prevVar);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));

            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, tpTick));

            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, tpTick));

            // 上升沿: 启动脉冲, 记录 startTime
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, startVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, etVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tpEnd));

            labels[tpTick] = instructions.Count;
            LoadVariable(qVar);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, tpEnd));

            // ET = GetTick() - startTime
            EmitGetTick();
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.LABEL, startVar));
            AddInstruction(OpCode.SUB, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, etVar));

            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.IMMEDIATE, int.Parse(ptMs)));
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 2));
            string tpExpired = NewLabel("tp_expired");
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, tpExpired));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, tpEnd));

            labels[tpExpired] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));

            labels[tpEnd] = instructions.Count;
            LoadVariableOrValue(inVar);
            StoreVariable(prevVar);

            LoadVariable(qVar);
        }

        /// <summary>
        /// CTU: 加计数器
        /// CU上升沿时CV+1, CV>=PV时Q=true; R=true时复位CV=0, Q=false
        /// </summary>
        private void GenerateCTU(FunctionBlockNode node, string instanceName)
        {
            string cuVar = GetVariableOrLiteral(node, "CU");
            string rVar = GetVariableOrLiteral(node, "R");
            string pvStr = GetVariableOrLiteral(node, "PV");
            string qVar = $"{instanceName}.Q";
            string cvVar = $"{instanceName}.CV";
            string prevVar = GetCounterPrevVar(instanceName);

            dataSection[qVar] = 0;
            dataSection[cvVar] = 0;

            int pv = int.TryParse(pvStr, out pv) ? pv : 10;

            string ctuEnd = NewLabel("ctu_end");
            string ctuNotReset = NewLabel("ctu_not_reset");
            string ctuNoCount = NewLabel("ctu_no_count");
            string ctuQTrue = NewLabel("ctu_q_true");

            // 检查R(reset)
            LoadVariableOrValue(rVar);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctuNotReset));
            
            // R=true: 复位
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctuEnd));

            labels[ctuNotReset] = instructions.Count;
            // 检查CU上升沿
            LoadVariableOrValue(cuVar);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(prevVar);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            
            // CU=true && prev=false
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctuNoCount));
            
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, ctuNoCount));
            
            // 上升沿: CV++ (带边界检查)
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32767)); // 最大INT值
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, ctuNoCount)); // 溢出时不增加
            AddInstruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) });
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));

            labels[ctuNoCount] = instructions.Count;
            // 检查CV >= PV
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, pv));
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, ctuQTrue));
            
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctuEnd));

            labels[ctuQTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));

            labels[ctuEnd] = instructions.Count;
            LoadVariableOrValue(cuVar);
            StoreVariable(prevVar);

            LoadVariable(qVar);
        }

        /// <summary>
        /// CTD: 减计数器
        /// CD下降沿时CV-1, CV<=0时Q=true; LD=true时加载PV到CV
        /// </summary>
        private void GenerateCTD(FunctionBlockNode node, string instanceName)
        {
            string cdVar = GetVariableOrLiteral(node, "CD");
            string ldVar = GetVariableOrLiteral(node, "LD");
            string pvStr = GetVariableOrLiteral(node, "PV");
            string qVar = $"{instanceName}.Q";
            string cvVar = $"{instanceName}.CV";
            string prevVar = GetCounterPrevVar(instanceName);

            dataSection[qVar] = 0;
            dataSection[cvVar] = 0;

            int pv = int.TryParse(pvStr, out pv) ? pv : 10;

            string ctdEnd = NewLabel("ctd_end");
            string ctdNotLoad = NewLabel("ctd_not_load");
            string ctdNoCount = NewLabel("ctd_no_count");
            string ctdQTrue = NewLabel("ctd_q_true");

            // 检查LD(load)
            LoadVariableOrValue(ldVar);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctdNotLoad));
            
            // LD=true: 加载PV
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, pv));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctdEnd));

            labels[ctdNotLoad] = instructions.Count;
            // 检查CD下降沿: CD=false && prev=true
            LoadVariableOrValue(cdVar);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(prevVar);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, ctdNoCount));
            
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctdNoCount));
            
            // 下降沿: CV-- (带边界检查)
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -32768)); // 最小INT值
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, ctdNoCount)); // 下溢时不减少
            AddInstruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) });
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));

            labels[ctdNoCount] = instructions.Count;
            // 检查CV <= 0
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, ctdQTrue));
            
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctdEnd));

            labels[ctdQTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qVar));

            labels[ctdEnd] = instructions.Count;
            LoadVariableOrValue(cdVar);
            StoreVariable(prevVar);

            LoadVariable(qVar);
        }

        /// <summary>
        /// CTUD: 加减计数器
        /// CU上升沿CV+1, CD上升沿CV-1, CV>=PV时QU=true, CV<=0时QD=true, R=true时复位
        /// </summary>
        private void GenerateCTUD(FunctionBlockNode node, string instanceName)
        {
            string cuVar = GetVariableOrLiteral(node, "CU");
            string cdVar = GetVariableOrLiteral(node, "CD");
            string rVar = GetVariableOrLiteral(node, "R");
            string ldVar = GetVariableOrLiteral(node, "LD");
            string pvStr = GetVariableOrLiteral(node, "PV");
            string quVar = $"{instanceName}.QU";
            string qdVar = $"{instanceName}.QD";
            string cvVar = $"{instanceName}.CV";
            string cuPrev = GetCounterPrevVar(instanceName + "_cu");
            string cdPrev = GetCounterPrevVar(instanceName + "_cd");

            dataSection[quVar] = 0;
            dataSection[qdVar] = 0;
            dataSection[cvVar] = 0;

            int pv = int.TryParse(pvStr, out pv) ? pv : 10;

            string ctudEnd = NewLabel("ctud_end");
            string ctudNotReset = NewLabel("ctud_not_reset");
            string ctudNotLoad = NewLabel("ctud_not_load");
            string ctudUpdateFlags = NewLabel("ctud_update_flags");
            string ctudUpdateQd = NewLabel("ctud_update_qd");
            string ctudNoInc = NewLabel("ctud_no_inc");
            string ctudNoDec = NewLabel("ctud_no_dec");
            string ctudQuTrue = NewLabel("ctud_qu_true");
            string ctudQdTrue = NewLabel("ctud_qd_true");

            // 检查R(reset)
            LoadVariableOrValue(rVar);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctudNotReset));
            
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, quVar));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qdVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctudEnd));

            labels[ctudNotReset] = instructions.Count;
            // 检查LD(load)
            LoadVariableOrValue(ldVar);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctudNotLoad));
            
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, pv));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctudUpdateFlags));

            labels[ctudNotLoad] = instructions.Count;
            // CU上升沿: CV++
            LoadVariableOrValue(cuVar);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(cuPrev);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctudNoInc));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, ctudNoInc));
            
            // 上升沿: CV++ (带边界检查)
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32767));
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, ctudNoInc));
            AddInstruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) });
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));

            labels[ctudNoInc] = instructions.Count;
            // CD上升沿: CV-- (带边界检查)
            LoadVariableOrValue(cdVar);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            LoadVariable(cdPrev);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, ctudNoDec));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, ctudNoDec));

            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -32768));
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, ctudNoDec));
            AddInstruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) });
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));

            labels[ctudNoDec] = instructions.Count;
            // 更新QU和QD标志
            labels[ctudUpdateFlags] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, pv));
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, ctudQuTrue));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, quVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctudUpdateQd));

            labels[ctudQuTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, quVar));

            labels[ctudUpdateQd] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, cvVar));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, ctudQdTrue));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qdVar));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, ctudEnd));

            labels[ctudQdTrue] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, qdVar));

            labels[ctudEnd] = instructions.Count;
            LoadVariableOrValue(cuVar);
            StoreVariable(cuPrev);
            LoadVariableOrValue(cdVar);
            StoreVariable(cdPrev);

            LoadVariable(quVar);
        }
    }
}
