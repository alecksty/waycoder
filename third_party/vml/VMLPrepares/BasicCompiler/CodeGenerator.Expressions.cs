using VMLAssembler;
using CompilerBase;

namespace BasicCompiler
{
    public partial class CodeGenerator
    {
        private ExpType InferExpType(Expression expr)
        {
            return GetExpType(InferExpressionType(expr));
        }

        /// <summary>
        /// 生成表达式，支持类型转换
        /// </summary>
        private void GenerateExpressionWithType(Expression expr, int reg, BasicType expectedType = BasicType.Integer)
        {
            // 生成表达式
            GenerateExpression(expr, reg);

            // 推断表达式的实际类型
            BasicType actualType = InferExpressionType(expr);

            // 如果需要类型转换，生成转换指令
            GenerateTypeConversion(actualType, expectedType, reg);
        }

        /// <summary>
        /// 根据上下文调用合适的表达式生成方法 (SUB内用GenerateSubExpression, 全局用GenerateExpression)
        /// </summary>
        private void GenerateExpr(Expression expr, int reg)
        {
            if (currentSubName != null)
                GenerateSubExpression(expr, reg);
            else
                GenerateExpression(expr, reg);
        }

        /// <summary>Tracks whether the last expression evaluated to float/double</summary>
        private BasicType? _lastExprFloatType = null;

        /// <summary>Add F2I/D2I (float→int) conversion if the expression result is float/double</summary>
        private void AddF2I(int reg)
        {
            if (_lastExprFloatType != null)
            {
                var convOp = _lastExprFloatType == BasicType.Double ? OpCode.D2I : OpCode.F2I;
                instructions.Add(new Instruction(convOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                _lastExprFloatType = null;
            }
        }

        /// <summary>Evaluate expression as int coordinate for graphics. Adds F2I if needed.</summary>
        private void EvalIntCoord(Expression expr, int reg)
        {
            if (currentSubName != null)
                GenerateSubExpression(expr, reg);
            else
                GenerateExpression(expr, reg);
            AddF2I(reg);
        }

        private void GenerateExpression(Expression expr, int reg)
        {
            _lastExprFloatType = null;
            if (expr is InkeyExpression inkey)
            {
                GenerateInkeyExpression(inkey, reg);
            }
            else if (expr is TimerFunctionExpression)
            {
                GenerateTimerFunction(reg);
            }
            else if (expr is DateFunctionExpression)
            {
                GenerateDateFunction(reg);
            }
            else if (expr is TimeFunctionExpression)
            {
                GenerateTimeFunction(reg);
            }
            else if (expr is NumberLiteral numLiteral)
            {
                double val = numLiteral.Value;
                if (val == (int)val && val >= int.MinValue && val <= int.MaxValue)
                    AddRI(OpCode.MOVE, reg, (int)val);
                else {
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, (float)val) }));
                    _lastExprFloatType = BasicType.Single;
                }
            }
            else if (expr is StringLiteral strLit)
            {
                if (string.IsNullOrEmpty(strLit.Value))
                {
                    // 空串走**唯一那个空串地址**（见 `CodeGenerator.EmptyStringLabel`）：
                    // 字符串比较是指针比较，`s$ = ""` 只有在"两边都是同一个空串地址"时才成立。
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.LABEL, EmptyStringLabel) }));
                }
                else
                {
                    // Store string in data section, emit LEA to load address into reg
                    string strLabel = $"str_data_{GenerateLabel()}";
                    dataSection[strLabel] = new DataString(strLit.Value);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.LABEL, strLabel) }));
                }
            }
            else if (expr is Identifier ident)
            {
                // Check if this is a compile-time constant
                if (constants.ContainsKey(ident.Name.ToLower()))
                {
                    var val = constants[ident.Name.ToLower()];
                    if (val is int intVal)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, intVal) }));
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    }
                    return;
                }
                GetOrCreateVariable(ident.Name);
                BasicType varType = GetVariableType(ident.Name);
                // 走 EmitLoadVar：全局变量（模块级）从静态区全局段读，SUB 局部/参数走 R12 相对。
                // ⚠ 原来这里是就地拼 `R12+{8 + index*4}` —— 一来进不了全局段，
                //   二来它按"索引×4"而 VarMemRef 按 GetVarByteOffset（Double/Long 算 8 字节），
                //   同一处地址两套算法，有 8 字节类型就会漂。
                EmitLoadVar(reg, ident.Name);
                if (varType == BasicType.Single || varType == BasicType.Double)
                    _lastExprFloatType = varType;
            }
            else if (expr is BinaryExpression binary)
            {
                // 字符串拼接 `a$ + b$` —— 必须**排在整数加法之前**分叉。
                // 交给下面的算术路径就是"把两个字符串指针相加"，结果是野指针，
                // 打出来是空行且**不报错**（实测 `b$ = "x" + "y"` → 空串）。
                if (GenerateStringConcat(binary, reg)) return;

                // 推断左右操作数类型
                ExpType leftType = InferExpType(binary.Left);
                ExpType rightType = InferExpType(binary.Right);

                // 确定运算结果类型（类型提升）
                ExpType resultType = ExpressionManager.WidenType(leftType, rightType);
                bool isFloat = resultType.IsFloat() || resultType.IsDouble();
                BasicType basicResultType = resultType.IsDouble() ? BasicType.Double :
                                           isFloat ? BasicType.Single : BasicType.Integer;

                // 使用寄存器管理器分配临时寄存器，避免与目标 reg 冲突
                int leftReg, rightReg;
                if (isFloat) {
                    leftReg = reg;
                    rightReg = (reg + 4) % 8;
                } else {
                    leftReg = Regs.AllocInt(instructions);
                    rightReg = Regs.AllocInt(instructions);
                }
                Regs.ReserveInt(reg, instructions);

                // 生成左操作数
                GenerateExpressionWithType(binary.Left, leftReg, basicResultType);
                // 生成右操作数
                GenerateExpressionWithType(binary.Right, rightReg, basicResultType);

                // 根据结果类型选择运算指令
                switch (binary.Operator)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "\\":      // BASIC 的整除：与 `/` 同一条除法路径（VML 的 DIV 对整数就是整除）
                    case "MOD":
                        OpCode arithmeticOp = binary.Operator == "MOD" ? OpCode.MOD
                            : ExpressionManager.SelectArithmeticOp(
                                binary.Operator == "\\" ? "/" : binary.Operator, isFloat, resultType.IsDouble());
                        if (!isFloat) {
                            if (rightReg == reg) {
                                if (binary.Operator == "-" || binary.Operator == "/" || binary.Operator == "\\" || binary.Operator == "MOD") {
                                    // Non-commutative: save right value (in reg) to R0 before MOVE clobbers it
                                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, reg) }));
                                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
                                } else {
                                    // Commutative: right already in reg, just op with leftReg
                                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                                }
                            } else {
                                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                                instructions.Add(new Instruction(arithmeticOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, rightReg) }));
                            }
                        } else {
                            instructions.Add(new Instruction(arithmeticOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, rightReg) }));
                        }
                        break;
                    case "^":
                    {
                        // Exponentiation: result = base ^ exp (via loop)
                        // 使用栈上临时变量存储循环计数器和基数，避免寄存器冲突
                        int expReg = rightReg;
                        int baseReg = leftReg;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        string powLoop = GenerateLabel();
                        string powEnd = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, powLoop) }));
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, expReg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, powEnd) }));
                        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, baseReg) }));
                        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, expReg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, powLoop) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, powEnd) }));
                        break;
                    }
                    case "=":
                    case "<>":
                    case "<":
                    case "<=":
                    case ">":
                    case ">=":
                        // 使用类型敏感的比较指令
                        OpCode compareOp = ExpressionManager.SelectCompareOp(isFloat, resultType.IsDouble());
                        instructions.Add(new Instruction(compareOp, new List<Operand> { new Operand(OperandType.REGISTER, leftReg), new Operand(OperandType.REGISTER, rightReg) }));
                        
                        // 根据运算符选择跳转指令
                        OpCode jumpOp = OpCode.JMP;
                        switch (binary.Operator)
                        {
                            case "=": jumpOp = OpCode.JNE; break;  // 不相等跳转
                            case "<>": jumpOp = OpCode.JE; break;  // 相等跳转
                            case "<": jumpOp = OpCode.JGE; break;  // 大于等于跳转
                            case "<=": jumpOp = OpCode.JG; break;  // 大于跳转
                            case ">": jumpOp = OpCode.JLE; break;  // 小于等于跳转
                            case ">=": jumpOp = OpCode.JL; break;  // 小于跳转
                        }
                        
                        // 使用跳转指令模拟SET指令
                        string falseLabel = GenerateLabel();
                        string endLabel = GenerateLabel();
                        instructions.Add(new Instruction(jumpOp, new List<Operand> { new Operand(OperandType.LABEL, falseLabel) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, falseLabel) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                        break;
                    case "AND":
                        if (rightReg == reg) {
                            // Commutative: right already in reg
                            instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                        } else {
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                            instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, rightReg) }));
                        }
                        break;
                    case "OR":
                        if (rightReg == reg) {
                            // Commutative: right already in reg
                            instructions.Add(new Instruction(OpCode.OR, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                        } else {
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, leftReg) }));
                            instructions.Add(new Instruction(OpCode.OR, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, rightReg) }));
                        }
                        break;
                }

                if (!isFloat)
                {
                    Regs.FreeInt(rightReg, instructions);
                    Regs.FreeInt(leftReg, instructions);
                }
                else
                {
                    _lastExprFloatType = basicResultType;
                }
            }
            else if (expr is UnaryExpression unary)
            {
                GenerateExpression(unary.Expression, reg);
                if (unary.Operator == "-")
                {
                    instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, reg) }));
                }
                else if (unary.Operator == "NOT")
                {
                    string notLabel1 = GenerateLabel();
                    string notLabel2 = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, notLabel1) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, notLabel2) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notLabel1) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notLabel2) }));
                }
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                GenerateArrayAccess(arrayAccess, reg);
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                GenerateMainFunctionCall(funcCall, reg);
            }
            else if (expr is FieldAccessExpression fieldAccess)
            {
                GenerateFieldAccessExpression(fieldAccess, reg);
            }
        }

        /// <summary>
        /// `a$ + b$` → `CALL basic_concat`（库里的静态缓冲区版本）。
        ///
        /// 判据是**两边都推断为 String**：这样 `1 + 2`、`a$ + 1` 之类不受影响。
        /// 递归形状（`a$ + b$ + c$`）靠 `InferExpressionType` 里那条同名规则支撑 ——
        /// 它把「String + String」的二元节点也判成 String，于是左结合的三元链
        /// 每次都会走到这里。
        ///
        /// ⚠ 与其它库调用一样要 `EmitSaveRegsExcept`：库函数会把 R0–R5 用掉，
        ///   而这可能发生在另一个表达式求值的中途。
        /// </summary>
        /// <summary>拼接点 → 结果缓冲区槽位。**每个拼接点（AST 节点）各占一槽**。</summary>
        private int _concatSlot;

        /// <summary>与库侧 `basiclib.c` 的 `CONCAT_SLOTS` 同值（跨语言契约）。</summary>
        private const int ConcatSlots = 16;

        private bool GenerateStringConcat(BinaryExpression binary, int reg)
        {
            if (binary.Operator != "+") return false;
            if (InferExpressionType(binary.Left) != BasicType.String) return false;
            if (InferExpressionType(binary.Right) != BasicType.String) return false;

            // ⚠ **每个拼接点要自己的结果缓冲区**（v0.96.330 修）。
            //   从前大家都用库里那块唯一的 `_buf3` ⇒ 两条拼接先后求值互相覆盖：
            //   `a$ = "AAA" + STR$(1)` 紧跟 `b$ = "BBB" + STR$(2)` 之后
            //   **a 和 b 都等于 "BBB2"**（实测）。字符串在 BASIC 里是**值**，
            //   两个同时活着的拼接结果必须各有各的地方。
            //
            //   槽位在这里**按代码生成顺序**分配，而代码生成对每个 AST 节点**只跑一次**
            //   ⇒ 同一处代码（哪怕在循环里）永远用同一槽，不同表达式点天然错开。
            //   拼接点超过槽位数时绕回，最坏退化成从前那种行为（库侧会把越界夹回 0）。
            int slot = _concatSlot;
            _concatSlot = (_concatSlot + 1) % ConcatSlots;

            var call = new FunctionCallExpression(binary.Line, binary.Column, "basic_concat_slot");
            call.Arguments.Add(binary.Left);
            call.Arguments.Add(binary.Right);
            call.Arguments.Add(new NumberLiteral(binary.Line, binary.Column, slot));

            EmitSaveRegsExcept(reg, 0, 1, 2, 3, 4, 5);
            GenerateLibraryCall("basic_concat_slot", call, reg);
            EmitRestoreRegsExcept(reg, 0, 1, 2, 3, 4, 5);
            return true;
        }

        private void GenerateMainFunctionCall(FunctionCallExpression funcCall, int reg)
        {
            string funcName = funcCall.FunctionName.ToLower();
            
            // 内置函数分发
            switch (funcName)
            {
                // === 数学函数 (C 库实现) ===
                case "sqr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_sqr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "int": case "fix":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_int", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "rnd":
                    // `RND(1)` —— 见 `GenerateRndValue`：库给的是原始随机整数，
                    // 要换算成 QBasic 语义的 [0,1) 单精度分数，**两处内置表都要走它**
                    // （这一处与 `CodeGenerator.Expressions.cs` 的 `GenerateMainFunctionCall`
                    // 是同一个 switch 的两份，本仓的老毛病）。
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateRndValue(reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "sin": case "cos": case "tan": case "exp": case "log": case "atn": case "atan":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5);
                    switch (funcName) {
                        case "sin": GenerateLibraryCall("basic_sin", funcCall, reg); break;
                        case "cos": GenerateLibraryCall("basic_cos", funcCall, reg); break;
                        case "tan": GenerateLibraryCall("basic_tan", funcCall, reg); break;
                        case "exp": GenerateLibraryCall("basic_exp", funcCall, reg); break;
                        case "log": GenerateLibraryCall("basic_log", funcCall, reg); break;
                        default: GenerateLibraryCall("basic_atn", funcCall, reg); break;
                    }
                    EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case string p when p == "peek" || p == "peekb" || p == "peekh" || p == "peekl" || p == "peekf" || p == "peekd":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateBuiltInPeek(funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "point":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_point", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "command$": case "command":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateCommand(funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // === C 库实现 (CALL basic_xxx) ===
                case "abs":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_abs", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "sgn":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_sgn", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "len":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_len", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "chr$": case "chr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_chr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "asc":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_asc", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "left$": case "left":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_left", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "right$": case "right":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_right", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "mid$": case "mid":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_mid3", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "str$": case "str":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_str_int", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "val":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_val", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "space$": case "space":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_space", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "ucase$": case "ucase":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_ucase", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "lcase$": case "lcase":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_lcase", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "ltrim$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_ltrim", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "rtrim$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_rtrim", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // 新 C 库函数
                case "instr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_instr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "string$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5);
                    if (funcCall.Arguments.Count >= 2 && funcCall.Arguments[1] is StringLiteral)
                        GenerateLibraryCall("basic_stringS", funcCall, reg);
                    else
                        GenerateLibraryCall("basic_stringN", funcCall, reg);
                    EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "hex$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_hex", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "oct$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_oct", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "date$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_date_str", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "time$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_time_str", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "timer":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_timer", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "input$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_inputN", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // === 类型转换函数 (直接生成转换 opcode) ===
                case "csng":  // Convert to Single (float)
                {
                    GenerateExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Double)
                        instructions.Add(new Instruction(OpCode.D2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Single;
                    return;
                }
                case "cdbl":  // Convert to Double
                {
                    GenerateExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Double;
                    return;
                }
                case "clng":  // Convert to Long (BASIC Long uses Double/F64)
                {
                    GenerateExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Double;
                    return;
                }
                case "cint":  // Convert to Integer
                {
                    GenerateExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Double)
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = null;
                    return;
                }
                case "varptr":
                case "lof":
                case "eof":
                    // Stub: return 0
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                default:
                    break;
            }
            
            // 获取函数声明以检查参数是否为 BYREF + native
            funcMap.TryGetValue(SymbolKey(funcCall.FunctionName), out var funcDecl);

            // native FUNCTION: 使用裸名 CALL (无 func_ 前缀)
            string funcLabel = (funcDecl != null && funcDecl.IsNative)
                ? SymbolKey(funcCall.FunctionName)
                : FunctionLabel(funcCall.FunctionName);

            int argBytes = EmitCallArguments(funcCall.Arguments,
                i => funcDecl != null && i < funcDecl.Parameters.Count && funcDecl.Parameters[i].IsByRef,
                currentSubName != null,
                i => funcDecl != null && i < funcDecl.Parameters.Count
                        ? ParamDeclaredType(funcDecl.Parameters[i]) : BasicType.Integer);

            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }));

            if (argBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argBytes) }));
            }

            if (reg != 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
            }
        }

        /// <summary>
        /// 数组元素**取值** → <paramref name="reg"/>（历史入口，`GenerateExpression` 在用）。
        ///
        /// <para>「整个数组」(`arr()`，`IsWholeArray`) 在这一层给的是**基址** —— 与 QBasic 的
        /// 语义一致（数组名就是它的基址）；BYREF 实参那条路走
        /// <see cref="GenerateVariableAddress"/>，那里同样只要基址。真正的元素读写
        /// 都在 <see cref="GenerateArrayElementAddr"/> 一处。</para>
        /// </summary>
        private void GenerateArrayAccess(ArrayAccessExpression arrayAccess, int reg)
        {
            if (arrayAccess.IsWholeArray)
            {
                if (!GenerateArrayBaseAddr(arrayAccess.ArrayName, reg))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                }
                return;
            }

            if (!GenerateArrayElementAddr(arrayAccess, reg))
            {
                // 不是已知数组（既不在 `arrayVariables` 里、也不是数组形参）——
                // 与从前一样给 0。**只给 0、不报错**是刻意的：QBasic 允许
                // 「不 DIM 直接用 `a(i)`」的隐式数组，而本前端不支持那种数组
                // （它的元素槽一个都没建），报错会把一批能编的老程序打掉。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
            }
        }

        /// <summary>
        /// 数组元素**地址** → <paramref name="reg"/>；三种用法由参数挑：
        /// <list type="bullet">
        /// <item><description>默认（<paramref name="addressOnly"/> = false、<paramref name="storeReg"/> &lt; 0）：
        /// <b>读</b> —— 从算出来的地址里把值读进 <paramref name="reg"/>（`arr(i)`）；</description></item>
        /// <item><description><paramref name="storeReg"/> ≥ 0：<b>写</b> —— 把那个寄存器的值存进地址
        /// （`arr(i) = v`，存指令留在界内那一段里）；</description></item>
        /// <item><description><paramref name="addressOnly"/> = true：**只要地址**，既不读也不写
        /// （`arr(i).Field` 的基址、`arr(i)` 当 BYREF 实参）。</description></item>
        /// </list>
        /// <para>
        /// ⚠ 三种用法的差别只在**末尾那一条指令**上，前面"下标 + 基址 + 步长"完全一样 ——
        /// 所以必须收在一个函数里。分开写就会出现"取地址那条路顺手把值也读了"
        /// （实测 `b(0).XCoor` 的基址被读成 `[基址]` 里的垃圾、写进去的字段全落到别处）。
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// 两条来源，**同一个元素地址公式**（基址 + 线性下标 × 元素字节数）：
        /// </para>
        /// <list type="number">
        /// <item><description><b>数组变量</b>（模块级 `DIM` / SUB 内 `DIM`）：基址 = 静态区全局段里
        /// `<名字>(0)` 那个槽的地址（`EmitStaticAddr`）；</description></item>
        /// <item><description><b>数组形参</b>（`SUB f (a() AS …)`）：**基址存在形参槽里** ——
        /// 与标量 BYREF 形参同一个约定（调用方压进来的是实参的地址，见
        /// `EmitCallArguments`）。所以这里读的是「槽里的值」（`MOVE R2, [R12+8+4i]`），
        /// 而不是"槽的地址"。</description></item>
        /// </list>
        /// <para>
        /// 为什么必须有第二条：数组形参此前**根本没有被登记**（`arrayVariables` 只从 `DIM`
        /// 收），于是 `b(i) = …` 静默什么都不生成、`PRINT b(i)` 恒为 0。
        /// 实测最小复现：`SUB fill (BYREF b() AS ANY) / b(0) = 11` + `CALL fill(arr())` ⇒
        /// 读回 0。GORILLA.BAS 的城市天际线整座画不出来就是它 —— `MakeCityScape (BCoor() AS
        /// XYPoint)` 里 15 座楼的坐标全写进了地址 0。
        /// </para>
        /// </remarks>
        private bool GenerateArrayElementAddr(ArrayAccessExpression arrayAccess, int reg, int storeReg = -1, bool addressOnly = false)
        {
            string arrayName = arrayAccess.ArrayName;

            // **形参优先于数组变量**：SUB 体内一个名字同时是形参和模块数组时，
            // QBasic 的语义是**形参遮蔽**（形参才是调用方给的那一份）。
            // 反过来的话，`PlayGame` 里 `DIM BCoor(0 TO 30) AS XYPoint` 一旦先被处理，
            // 后面 `SUB MakeCityScape (BCoor() AS XYPoint)` 体内的 `BCoor` 就会被
            // 当成那个**同名全局数组** —— 好在这次两者是同一个数组，
            // 换个名字（`SUB f (a() AS T)` 配模块级 `a`）就是静默读错一份数据。
            ArrayInfo arrayInfo = null;
            int arrayParamIdx = -1;
            int pIdx0 = currentSubName != null ? FindParameterIndex(arrayName.ToLower()) : -1;
            var pDecl0 = pIdx0 >= 0 ? FindParameterDecl(pIdx0) : null;
            if (pDecl0 != null && pDecl0.IsArray)
            {
                arrayParamIdx = pIdx0;
            }
            else if (!arrayVariables.TryGetValue(arrayName, out arrayInfo))
            {
                return false;
            }

            // 元素字节数：标量数组 4；**用户自定义类型数组**是整个记录的大小
            // （`DimStatement` 那里按 `TotalSize` 向上取整记下来的）。
            // 不区分的话 `BCoor(1).XCoor` 会落在 `BCoor(0).YCoor` 上。
            //
            // 数组形参没有 `ArrayInfo`，元素的字节数只能从**形参声明的类型名**推
            // （`BCoor() AS XYPoint` → `dimAsVariables` → `typeDefinitions`）。
            // 声明成 `AS ANY` 时推不出来 ⇒ 退回 4 —— 那种写法与"元素是标量"没有区别，
            // 但**元素的真实步长只有调用方知道**，所以 UDT 数组形参必须写类型名。
            int elemBytes = 4;
            if (arrayInfo != null)
            {
                elemBytes = arrayInfo.ElementBytes > 0 ? arrayInfo.ElementBytes : 4;
            }
            else if (pDecl0 != null && !string.IsNullOrEmpty(pDecl0.TypeName)
                     && dimAsVariables.TryGetValue(pDecl0.Name.ToLower(), out var pTypeName))
            {
                elemBytes = ArrayElementSlots(pTypeName) * 4;
            }

            var dimensions = arrayInfo?.Dimensions;

            // 计算线性索引 (行优先)
            // 对于单维: linearIndex = indices[0]
            // 对于2D A(rows,cols): linearIndex = i * cols + j
            // 对于3D A(d1,d2,d3): linearIndex = i * (d2*d3) + j * d3 + k
            int linearReg = 1;
            if (arrayInfo == null)
            {
                // 数组形参**没有维数信息**（调用方那边才有）⇒ 只支持单下标。
                // 多维要么猜、要么静默按一维算 —— 猜错的形状是"值全对但位置全错"，
                // 比报错难查得多，所以这里选择响亮地报错。
                if (arrayAccess.Indices.Count > 1)
                {
                    throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression,
                        $"数组形参 '{arrayName}' 不支持多余一维的下标（形参没有维数信息，无法算行优先下标）");
                }
                var idxExpr = arrayAccess.Indices.Count == 1 ? arrayAccess.Indices[0] : arrayAccess.Index;
                if (currentSubName != null)
                    GenerateSubExpression(idxExpr, linearReg);
                else
                    GenerateExpression(idxExpr, linearReg);
            }
            else if (dimensions.Count <= 1)
            {
                if (currentSubName != null)
                {
                    GenerateSubExpression(arrayAccess.Index, linearReg);
                }
                else
                {
                    GenerateExpression(arrayAccess.Index, linearReg);
                }
                // 下标 → 槽位：`DIM a(1 TO 2)` 的合法下标是 1..2，而槽位是 0..1
                // ⇒ 减掉下界。不减的话 `GorillaX(2)` 落到第 3 个槽（越界）而被**静默跳过**。
                int lo1 = arrayInfo.LowerBounds.Count > 0 ? arrayInfo.LowerBounds[0] : 0;
                if (lo1 != 0) AddRI(OpCode.SUB, linearReg, lo1);
            }
            else
            {
                // 多维: 计算线性索引
                // R1 = 0 (accumulator)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
                
                for (int i = 0; i < dimensions.Count; i++)
                {
                    // 计算当前维度的步长: stride = product(dimensions[i+1...])
                    int stride = 1;
                    for (int j = i + 1; j < dimensions.Count; j++)
                    {
                        stride *= dimensions[j];
                    }
                    
                    // 获取当前索引值（同样要减掉**这一维的**下界）
                    if (currentSubName != null)
                    {
                        GenerateSubExpression(arrayAccess.Indices[i], 2);
                    }
                    else
                    {
                        GenerateExpression(arrayAccess.Indices[i], 2);
                    }
                    int dimLo = arrayInfo.LowerBounds.Count > i ? arrayInfo.LowerBounds[i] : 0;
                    if (dimLo != 0) AddRI(OpCode.SUB, 2, dimLo);
                    
                    // R1 = R1 + R2 * stride
                    if (stride > 1)
                    {
                        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, stride) }));
                    }
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                }
            }
            
            // 边界检查
            //
            // ⚠ **只有数组变量查**（`arrayInfo != null`）：数组形参没有尺寸信息
            //   （尺寸在调用方那头），而 QBasic 本身也不做下标检查 —— 跳过是"与 QBasic 一致"，
            //   不是"漏了"。GORILLA 的 `BCoor(0 TO 30)` 里 `CurBuilding` 会走到 31，
            //   原版在 QBasic 上也照样越界读后面那块内存。
            string endLabel = GenerateLabel();
            if (arrayInfo != null)
            {
                string rangeCheckLabel = GenerateLabel();
                string outOfRangeLabel = GenerateLabel();

                // 界是 **槽位** 的界（0 .. 元素个数-1）—— 线性下标已经减过下界，
                // 所以这里跟 `Size` 比，不要再跟"下界"比。
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
                instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, arrayInfo.Size) }));
                instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, rangeCheckLabel) }));

                // 越界：**存的那条指令必须留在界内**。
                //
                // ⚠ 从前是"地址算完由调用方 `GenerateArrayAssignment` 再发一条存指令"，
                //   而 `endLabel` 是在本函数里、那条存指令之前就落下的 ⇒ 越界分支跳过去之后
                //   **照样执行那条存**，地址是 R0 里的残留值 —— 实测野写到 `0xFFFFFFF0`
                //   当场「内存错误(PC=…): MOVE @R0, @3」。所以值寄存器的写入挪进界内这一段
                //   （这也是本函数多一个 `storeReg` 的唯一理由）。
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
                if (storeReg < 0)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                }
                // （`storeReg >= 0` = 赋值：越界就**什么都不做** —— 这才是 QBasic 的
                //   "写到界外"该有的样子；跳过去之后那条存指令在 endLabel 之后，
                //   而 `EmitFieldStore`/`GenerateArrayAssignment` 都不会再补一条。）
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, rangeCheckLabel) }));
            }

            // 计算元素地址: **基址** + 线性索引 * 元素字节数
            //
            // ⚠ 基址从前写的是 `MOVE R2, R12; ADD R2, #(8 + arrayOffset*4)` —— 把数组当成了
            //   **当前帧里的局部量**（`R12+8` 之上）。而数组元素槽本来就是经
            //   `GetOrCreateVariable` 在模块级建的 ⇒ 它们住在静态区全局段（`EmitLoadVar`
            //   那条路）。两者只在"所有访问都在同一个栈深度"时才偶然重合：
            //     · 主程序里读写、以及**同一深度**的子过程之间传数据 —— 看着是对的
            //       （`R12+8` 那块其实是各次同深度调用共用的暂存区）；
            //     · 一旦跨层（`FUNCTION` 里读 `SUB` 写好的 `board(i)`、递归、嵌套调用），
            //       `R12+8` 换成了**另一层**的暂存区，读出来的就是垃圾 —— 实测
            //       `t9` 读出 `0 0 2 65528`（应为 `0 2 4 6`），而**一个错都不报**。
            //   改成静态区寻址之后，数组的位置与谁在读它无关。
            //
            // 数组形参走另一条：**基址就在形参槽里**（`MOVE R2, [R12+8+4i]`）。
            if (arrayInfo != null)
            {
                string elem0 = arrayName.ToLower() + "(0)";
                int arrayByteOffset = variables.ContainsKey(elem0) ? GetVarByteOffset(elem0) : arrayInfo.Offset * 4;
                EmitStaticAddr(2, STATIC_GLOBALS_OFFSET + arrayByteOffset);
            }
            else
            {
                // 形参槽里存的是**实参数组的基址**（调用方压进来的是"实参的地址"，
                // 而"数组实参的地址"就是它的基址 —— 见 `GenerateArrayBaseAddr`）。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.MEMORY, $"R12+{8 + arrayParamIdx * 4}") }));
            }

            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, elemBytes) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1) }));

            if (storeReg >= 0)
            {
                // 赋值：**地址与存指令同处一段**（都在这条 `endLabel` 之前）。
                // 值在 `storeReg` 里，地址在 R2 里 —— 间接寻址 `MOVE @R2, R3`。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.INDIRECT, 2),
                    new Operand(OperandType.REGISTER, storeReg) }));
            }
            else
            {
                // 地址 → 目标寄存器（`reg == 2` 时已经在里面了）
                if (reg != 2)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                }

                if (!addressOnly)
                {
                    // 取值：**从地址里读出来**。
                    //
                    // ⚠ 这里原来写的是 `MOVE reg, R2` —— 那是把**地址本身**当成了元素值，
                    //   于是 `arr(2)` 读出来永远是一个栈地址（实测 65556 = 0x10014），
                    //   与"写没写进去"无关。读、写两条路各反了一次，叠在一起看着像"数组全是野值"。
                    //   正确写法是寄存器间接寻址 `MOVE reg, @reg`。
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.REGISTER, reg),
                        new Operand(OperandType.INDIRECT, reg) }));
                }
            }

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            return true;
        }

        /// <summary>
        /// `记录表达式.字段` 的**字段偏移**（查不到类型/字段时返回 -1）。
        ///
        /// <para>记录表达式有三张脸：数组元素（`arr(i).F`）、变量（`v.F`）、
        /// 以及旧的 `RecordName` 形态。三者都要先解析出**记录名**再查
        /// `dimAsVariables`（数组/变量 → 类型名）与 `typeDefinitions`（类型 → 字段偏移）。</para>
        /// <para>⚠ 这套"解析记录名 + 找字段偏移"在别处还有**三份内联写法**
        /// （读侧 `GenerateFieldAccessExpression`、模块级写侧与 SUB 写侧各一份）——
        /// 它们各自还要为"字段不存在"报错，所以没并进来；**新增取地址这类用法请走本函数**，
        /// 不要再抄第四份。</para>
        /// </summary>
        private int ResolveFieldOffset(FieldAccessExpression fieldAccess)
        {
            if (fieldAccess.RecordExpression == null) return -1;
            string recordName = fieldAccess.RecordExpression switch
            {
                Identifier id => id.Name.ToLowerInvariant(),
                ArrayAccessExpression arr => arr.ArrayName.ToLowerInvariant(),
                _ => fieldAccess.RecordName?.ToLowerInvariant() ?? ""
            };
            if (string.IsNullOrEmpty(recordName)) return -1;
            if (!dimAsVariables.TryGetValue(recordName, out var typeName)) return -1;
            if (!typeDefinitions.TryGetValue(typeName, out var typeDef)) return -1;
            string field = fieldAccess.FieldName.ToLowerInvariant();
            foreach (var f in typeDef.Fields)
                if (f.Name.ToLowerInvariant() == field) return f.Offset;
            return -1;
        }

        /// <summary>
        /// 记录表达式的**基址** → <paramref name="reg"/>（`arr(i).Field` 里那个 `arr(i)`；
        /// 标量记录变量 `v.Field` 里那个 `v`）。
        ///
        /// <para>读字段与写字段**共用这一份**：两处各写一次的话，"取地址还是取值"这件事
        /// 就会像从前那样只对一半（读对齐了写没对齐，或者反过来）。</para>
        /// </summary>
        private void EmitRecordBaseAddr(Expression recordExpr, int reg)
        {
            // 数组元素：元素地址就是记录的基址（`addressOnly` —— 别顺手把值也读进来）
            if (recordExpr is ArrayAccessExpression acc && !acc.IsWholeArray)
            {
                if (GenerateArrayElementAddr(acc, reg, addressOnly: true)) return;
            }
            // 标量记录变量：变量地址就是记录的基址
            else if (recordExpr is Identifier)
            {
                GenerateVariableAddress(recordExpr, reg);
                return;
            }

            // 认不出来：给 0（与各调用点原来的兜底一致）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
        }

        /// <summary>
        /// 记录字段**赋值**的唯一实现：把 <paramref name="valueReg"/> 写进
        /// `<paramref name="recordExpr"/>.<paramref name="fieldOffset"/>` 那个地址
        /// （读侧是 `EmitRecordBaseAddr`）。
        ///
        /// <para>
        /// ⚠ **值要先压栈、地址算完再弹回来** —— 值的寄存器（调用方给了 R0/R1）与算地址要用的
        /// R1/R2 是同一批，而数组下标表达式一求值（`b(i + 1).X = v`）就会把它们覆盖掉。
        /// 直接"先算地址再写值"在简单下标下看着是对的（`b(0).X` 的下标不占寄存器），
        /// 换个下标就静默写错地方。
        /// </para>
        /// </summary>
        private void EmitFieldStore(Expression recordExpr, int fieldOffset, int valueReg, OpCode storeOp)
        {
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, valueReg) }));
            EmitRecordBaseAddr(recordExpr, 2);
            if (fieldOffset != 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, fieldOffset) }));
            }
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 3) }));
            instructions.Add(new Instruction(storeOp, new List<Operand>
            {
                new Operand(OperandType.MEMORY, "R2"),
                new Operand(OperandType.REGISTER, 3)
            }));
        }

        /// <summary>
        /// 数组**基址** → <paramref name="reg"/>（不是元素地址，也不做下标）。
        ///
        /// <para>BYREF 实参的取地址入口 —— `CALL MakeCityScape(BCoor())` 压进去的就是它。
        /// 两条来源与 <see cref="GenerateArrayElementAddr"/> 完全对称：数组变量取
        /// 静态区全局段里 `<名>(0)` 的地址；数组形参**转发**槽里的那个基址（读值，不取址 ——
        /// 取址就把"槽的地址"传下去了，被调方会从那里开始算下标，整体错位一格间接）。</para>
        /// </summary>
        private bool GenerateArrayBaseAddr(string arrayName, int reg)
        {
            // 形参优先（同 GenerateArrayElementAddr 的说明）
            int pIdx = currentSubName != null ? FindParameterIndex(arrayName.ToLower()) : -1;
            if (pIdx >= 0)
            {
                var pDecl = FindParameterDecl(pIdx);
                if (pDecl != null && pDecl.IsArray)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.REGISTER, reg),
                        new Operand(OperandType.MEMORY, $"R12+{8 + pIdx * 4}") }));
                    return true;
                }
            }

            if (arrayVariables.TryGetValue(arrayName, out var info))
            {
                string elem0 = arrayName.ToLower() + "(0)";
                int arrayByteOffset = variables.ContainsKey(elem0) ? GetVarByteOffset(elem0) : info.Offset * 4;
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + arrayByteOffset);
                return true;
            }
            return false;
        }

        /// <summary>数组元素赋值：值在 <paramref name="valueReg"/>，存进算出来的元素地址。</summary>
        private void GenerateArrayAssignment(ArrayAccessExpression arrayAccess, int valueReg)
        {
            // 值先落 **R3**（`GenerateArrayElementAddr` 算地址要用 R1/R2）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, valueReg) }));

            if (!GenerateArrayElementAddr(arrayAccess, 0, storeReg: 3))
            {
                // 不是已知数组 ⇒ 与从前一样什么都不做（静默 0 那条路，见 GenerateArrayAccess）
                return;
            }
        }

        private string GenerateLabel()
        {
            return "label_" + labelCounter++;
        }

        private void GenerateFieldAccessExpression(FieldAccessExpression fieldAccess, int reg)
        {
            string fieldName = fieldAccess.FieldName.ToLower();

            // Handle expression-based record (e.g., array access: sammy(1).head)
            if (fieldAccess.RecordExpression != null)
            {
                // 取记录的**基址**到 R2，再加字段偏移去读。
                //
                // ⚠ 原来这里写的是 `GenerateExpression(RecordExpression, 2)` —— 那是**取值**：
                //   数组元素取值走的是"从地址里读出来"，标量记录取值是把记录槽里的头 4 字节
                //   当整数读。两条都拿到一个**值**，却被当成"记录基址"往后加字段偏移
                //   ⇒ `b(0).XCoor` 与 `b(0).YCoor` 落在两个与 b 无关的地址上
                //   （实测 `b(0).XCoor`/`b(0).YCoor`/`b(3).XCoor`/`b(3).YCoor` 四个读出来
                //   全是 `1212`）。正解是**取地址**：`arr(i)` → 元素地址、标量 → 变量地址。
                EmitRecordBaseAddr(fieldAccess.RecordExpression, 2);

                // Now we need the type from the record expression to get field offset
                string recName;
                if (fieldAccess.RecordExpression is Identifier ident)
                {
                    recName = ident.Name.ToLower();
                }
                else if (fieldAccess.RecordExpression is ArrayAccessExpression arrExpr)
                {
                    recName = arrExpr.ArrayName.ToLower();
                }
                else
                {
                    recName = fieldAccess.RecordName?.ToLower() ?? "";
                }

                // Resolve type via dimAsVariables
                if (string.IsNullOrEmpty(recName) || !dimAsVariables.ContainsKey(recName))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                }

                string typName = dimAsVariables[recName];
                if (!typeDefinitions.ContainsKey(typName))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                }

                var tDef = typeDefinitions[typName];
                int fieldOff = 0;
                bool found3 = false;
                foreach (var f in tDef.Fields)
                {
                    if (f.Name.ToLower() == fieldName)
                    {
                        fieldOff = f.Offset;
                        found3 = true;
                        break;
                    }
                }

                if (!found3)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                }

                // R2 holds base address of array element, add field offset, then load
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, fieldOff) }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
                return;
            }

            string recordName = fieldAccess.RecordName.ToLower();

            // Find the type of this record variable
            if (!dimAsVariables.ContainsKey(recordName))
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                return;
            }

            string typeName = dimAsVariables[recordName];
            if (!typeDefinitions.ContainsKey(typeName))
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                return;
            }

            var typeDef2 = typeDefinitions[typeName];
            int fieldOffset = 0;
            bool found2 = false;
            foreach (var f in typeDef2.Fields)
            {
                if (f.Name.ToLower() == fieldName)
                {
                    fieldOffset = f.Offset;
                    found2 = true;
                    break;
                }
            }

            if (!found2)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                return;
            }

            // Compute address: base address of record + field offset
            if (currentSubName != null)
            {
                // In SUB context
                if (currentLocalVars.ContainsKey(recordName))
                {
                    int offset = LocalVarOffset(recordName);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, offset + fieldOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 14) }));
                }
                else if (variables.ContainsKey(recordName))
                {
                    EmitRecordFieldAddr(reg, recordName, fieldOffset);   // 全局记录走静态区全局段
                }
            }
            else
            {
                if (variables.ContainsKey(recordName))
                {
                    EmitRecordFieldAddr(reg, recordName, fieldOffset);   // 全局记录走静态区全局段
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                }
            }

            // Load the value at the computed address
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
        }
    }
}
