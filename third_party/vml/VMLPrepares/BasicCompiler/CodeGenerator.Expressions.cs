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
                GenerateArrayAccess(arrayAccess, reg, false);
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
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_rnd", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
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
                currentSubName != null);

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

        private void GenerateArrayAccess(ArrayAccessExpression arrayAccess, int reg, bool forAssignment)
        {
            string arrayName = arrayAccess.ArrayName;
            if (!arrayVariables.ContainsKey(arrayName))
            {
                if (!forAssignment)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                }
                return;
            }
            
            var arrayInfo = arrayVariables[arrayName];
            int arraySize = arrayInfo.Size;
            int arrayOffset = arrayInfo.Offset;
            var dimensions = arrayInfo.Dimensions;
            
            // 计算线性索引 (行优先)
            // 对于单维: linearIndex = indices[0]
            // 对于2D A(rows,cols): linearIndex = i * cols + j
            // 对于3D A(d1,d2,d3): linearIndex = i * (d2*d3) + j * d3 + k
            int linearReg = 1;
            if (dimensions.Count <= 1)
            {
                if (currentSubName != null)
                {
                    GenerateSubExpression(arrayAccess.Index, linearReg);
                }
                else
                {
                    GenerateExpression(arrayAccess.Index, linearReg);
                }
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
                    
                    // 获取当前索引值
                    if (currentSubName != null)
                    {
                        GenerateSubExpression(arrayAccess.Indices[i], 2);
                    }
                    else
                    {
                        GenerateExpression(arrayAccess.Indices[i], 2);
                    }
                    
                    // R1 = R1 + R2 * stride
                    if (stride > 1)
                    {
                        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, stride) }));
                    }
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                }
            }
            
            // 边界检查
            string rangeCheckLabel = GenerateLabel();
            string outOfRangeLabel = GenerateLabel();
            
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, arraySize) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, rangeCheckLabel) }));
            
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, outOfRangeLabel) }));
            if (!forAssignment)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
            }
            string endLabel = GenerateLabel();
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, rangeCheckLabel) }));
            
            // 计算元素地址: **静态区全局段** + 数组基址 + 线性索引 * 4
            //
            // ⚠ 这里原来是 `MOVE R2, R12; ADD R2, #(8 + arrayOffset*4)` —— 把数组当成了
            //   **当前帧里的局部量**（`R12+8` 之上）。而数组元素槽本来就是经
            //   `GetOrCreateVariable` 在模块级建的 ⇒ 它们住在静态区全局段（`EmitLoadVar`
            //   那条路）。两者只在"所有访问都在同一个栈深度"时才偶然重合：
            //     · 主程序里读写、以及**同一深度**的子过程之间传数据 —— 看着是对的
            //       （`R12+8` 那块其实是各次同深度调用共用的暂存区）；
            //     · 一旦跨层（`FUNCTION` 里读 `SUB` 写好的 `board(i)`、递归、嵌套调用），
            //       `R12+8` 换成了**另一层**的暂存区，读出来的就是垃圾 —— 实测
            //       `t9` 读出 `0 0 2 65528`（应为 `0 2 4 6`），而**一个错都不报**。
            //   改成静态区寻址之后，数组的位置与谁在读它无关。
            string elem0 = arrayName.ToLower() + "(0)";
            int arrayByteOffset = variables.ContainsKey(elem0) ? GetVarByteOffset(elem0) : arrayOffset * 4;
            EmitStaticAddr(2, STATIC_GLOBALS_OFFSET + arrayByteOffset);

            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1) }));
            
            if (forAssignment)
            {
                // 赋值：把**地址**放到 R0，由 GenerateArrayAssignment 再往里存
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2) }));
            }
            else
            {
                // 取值：**从地址里读出来**。
                //
                // ⚠ 这里原来写的是 `MOVE reg, R2` —— 那是把**地址本身**当成了元素值，
                //   于是 `arr(2)` 读出来永远是一个栈地址（实测 65556 = 0x10014），
                //   与"写没写进去"无关。读、写两条路各反了一次，叠在一起看着像"数组全是野值"。
                //   正确写法是寄存器间接寻址 `MOVE reg, @R2`。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, reg),
                    new Operand(OperandType.INDIRECT, 2) }));
            }
            
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }
        
        private void GenerateArrayAssignment(ArrayAccessExpression arrayAccess, int valueReg)
        {
            // 保存值到R3
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, valueReg) }));

            // 生成数组访问，获取地址到R0
            GenerateArrayAccess(arrayAccess, 0, true);

            // 把 R3 里的值**存进 R0 指向的地址**。
            //
            // ⚠ 这里原来是 `MOVE R3, R0` —— 方向反了：那是把**地址**写回 R3，
            //   值根本没落到数组里，而地址留在 R3 里被后续代码当成"刚赋的值"。
            //   现象极具迷惑性：`arr(2) = 7` 之后读 `arr(2)` 得到的是**一个栈地址**
            //   （实测 65556 = 0x10014），看着像"数组读出来是野值"，其实**写就没生效**。
            //
            // 正确写法是寄存器间接寻址 `MOVE @R0, R3`（`@Rn` = 地址在 Rn 里，
            // 见 OperandType.INDIRECT）。
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.INDIRECT, 0),
                new Operand(OperandType.REGISTER, 3) }));
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
                // Generate the record expression. For array access, it generates the element address in a register.
                // We need to get the base address, then add field offset, then load.
                // The record expr generates a value (the address for array access), so we evaluate it to R2
                GenerateExpression(fieldAccess.RecordExpression, 2);

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
                    int slot = currentLocalVars[recordName];
                    int offset = -(slot + 1) * 4;
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
