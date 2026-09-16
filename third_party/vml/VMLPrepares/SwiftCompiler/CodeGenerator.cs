using VMLAssembler;
using System.Collections.Generic;
using System.Linq;
using CompilerBase;

namespace SwiftCompiler
{
    /// <summary>Swift 类型枚举 — 用于类型感知指令选择</summary>
    public enum SwiftType { Int, Int64, Float, Double, Bool, Char, String, Array, Dict, Struct, Enum, Protocol, Optional, Any }

    /// <summary>
    /// Swift语言代码生成器
    /// </summary>
    public partial class CodeGenerator : TypedCodeGen<SwiftType>
    {
        private Dictionary<string, int> _localVarOffsets = new Dictionary<string, int>();
        private int _localVarSize = 0;
        private Dictionary<string, List<StructField>> _structFields = new Dictionary<string, List<StructField>>();
        private Stack<List<Statement>> _deferStack = new Stack<List<Statement>>();
        /// <summary>变量名 → Swift 类型映射</summary>
        private Dictionary<string, SwiftType> _varTypes = new();

        public CodeGenerator()
        {
            InitSimpleCompiler(framePointerReg: 14);
        }

        // 类型辅助方法
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(SwiftType t) => t switch
        {
            SwiftType.Float => (4, true, false, false),
            SwiftType.Double => (8, false, true, false),
            SwiftType.Int64 => (8, false, false, true),  // 64 位整数 — 用 long 路径操作 (MOVEL/ADDL/DIVL)
            SwiftType.Bool or SwiftType.Char => (1, false, false, false),
            SwiftType.String or SwiftType.Array or SwiftType.Dict or SwiftType.Struct
                or SwiftType.Enum or SwiftType.Protocol or SwiftType.Optional or SwiftType.Any => (4, false, false, false),
            _ => (4, false, false, false)
        };

        private static SwiftType MapToSwiftType(string typeName) => typeName.ToLowerInvariant() switch
        {
            "int" or "int8" or "int16" or "int32" or "uint" or "uint8" or "uint16" or "uint32" => SwiftType.Int,
            "int64" or "uint64" => SwiftType.Int64,
            "float" or "float32" => SwiftType.Float,
            "double" or "float64" => SwiftType.Double,
            "bool" => SwiftType.Bool,
            "character" or "char" => SwiftType.Char,
            "string" => SwiftType.String,
            _ => SwiftType.Int
        };

        private Program? _program;

        public override VmlProgram GenerateCode()
        {
            if (_program == null) throw new System.InvalidOperationException("No AST program set");
            return Generate(_program);
        }

        public VmlProgram Generate(Program program)
        {
            _program = program;
            // 先处理非函数定义的语句，记录程序入口
            int mainEntry = instructions.Count;
            bool hasTopLevel = false;
            foreach (var statement in program.Statements)
                if (!(statement is FunctionDeclStatement))
                {
                    GenerateStatement(statement);
                    hasTopLevel = true;
                }

            string[] varDataKeys = dataSection.Keys.Where(k => k.StartsWith("var_")).ToArray();
            if (varDataKeys.Length > 0)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, varDataKeys[0]) }));
            int exitIndex = instructions.Count;
            EmitExit();

            // 后处理函数定义
            foreach (var statement in program.Statements)
                if (statement is FunctionDeclStatement funcDecl)
                    GenerateFunctionDecl(funcDecl);

            // 如果没有main标签，添加默认的main
            if (!labels.ContainsKey("main"))
            {
                labels["main"] = mainEntry;
                instructions.Insert(mainEntry, new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 13)]));
                instructions.Insert(mainEntry, new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 1048572)]));
                var shift = 2;
                var labelKeys = new List<string>(labels.Keys);
                foreach (var k in labelKeys)
                    if (labels[k] >= mainEntry && k != "main")
                        labels[k] += shift;
            }
            else if (hasTopLevel)
            {
                // 程序自己定义了 `func main()` 时，**顶层那段语句会变成死代码** ——
                // 而全局变量/全局数组的初始化恰恰就在那段里（`var A = [...]` 生成的是
                // "把数组基址存进 var_A"）。实测症状：全局数组指针恒为 0，所有 `A[i]`
                // 读写都落在地址 4/8/… 那片低内存上 —— 写进去 3、读回来是别的值，
                // 表现成"跨函数读全局数组不对"（同一函数内看着正常，因为读写都偏移同样错）。
                //
                // 修法**不搬指令、只挪标签**：入口仍旧指回顶层块（mainEntry），
                // 块尾那条 EXIT 前面插一句 `call __user_main`，于是
                // 「全局初始化 → 用户 main → 退出」。
                int userMain = labels["main"];

                // ⚠ 序列化器认的是**指令流里的 LABEL 伪指令**，不是编译器那张表 ——
                //    只改表的话，函数身上那个 `main` 标签还在流里，入口照样指向函数。
                for (int i = 0; i < instructions.Count; i++)
                {
                    var ins = instructions[i];
                    if (ins.Opcode == OpCode.LABEL && ins.Operands.Count > 0
                        && "main".Equals(ins.Operands[0].Value as string))
                        instructions[i] = new Instruction(OpCode.LABEL,
                            new List<Operand> { new Operand(OperandType.LABEL, "__user_main") });
                }

                labels.Remove("main");
                instructions.Insert(exitIndex,
                    new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "__user_main")]));
                instructions.Insert(mainEntry,
                    new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, "main")]));
                // 两处插入 ⇒ exitIndex 之后的标签 +2、夹在中间的 +1
                foreach (var k in new List<string>(labels.Keys))
                {
                    if (labels[k] >= exitIndex) labels[k] += 2;
                    else if (labels[k] >= mainEntry) labels[k] += 1;
                }
                labels["main"] = mainEntry;
                labels["__user_main"] = userMain + 2;
            }
            
            // 创建VML程序
            var vmlProgram = BuildProgram("main");
            vmlProgram.StackTop = 1048572;
            return vmlProgram;
        }
        
        private void GenerateStatement(Statement statement)
        {
            switch (statement)
            {
                case ExpressionStatement exprStmt:
                    GenerateExpression(exprStmt.Expression);
                    break;
                    
                case VariableDeclStatement varDecl:
                    GenerateVariableDecl(varDecl);
                    break;
                    
                case FunctionDeclStatement funcDecl:
                    GenerateFunctionDecl(funcDecl);
                    break;
                    
                case ReturnStatement returnStmt:
                    GenerateReturn(returnStmt);
                    break;
                    
                case IfStatement ifStmt:
                    GenerateIf(ifStmt);
                    break;
                    
                case WhileStatement whileStmt:
                    GenerateWhile(whileStmt);
                    break;
                    
                case ForStatement forStmt:
                    GenerateFor(forStmt);
                    break;
                    
                case Block blockStmt:
                    GenerateBlock(blockStmt);
                    break;
                    
                case PrintStatement printStmt:
                    GeneratePrint(printStmt);
                    break;

                case SwitchStatement switchStmt:
                    GenerateSwitch(switchStmt);
                    break;

                case DoWhileStatement doWhileStmt:
                    GenerateDoWhile(doWhileStmt);
                    break;

                case ThrowStatement throwStmt:
                    if (HandleMCUThrow(() => { if (throwStmt.Value != null) GenerateExpression(throwStmt.Value); }))
                        break;
                    if (throwStmt.Value != null)
                        GenerateExpression(throwStmt.Value);
                    else
                        AddRI(OpCode.MOVE, 0, 0);
                    instructions.Add(new Instruction(OpCode.THROW, [new Operand(OperandType.REGISTER, 0)]));
                    break;

                case DoStatement doStmt:
                    if (VMLPlugins.CompilerOptionsContext.Current.IsMCU || doStmt.Catches.Count == 0)
                    {
                        GenerateStatement(doStmt.Body);
                        break;
                    }

                    string catchLabel = $"do_catch_{labelCounter++}";
                    string doEndLabel = $"do_end_{labelCounter++}";

                    instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
                    GenerateStatement(doStmt.Body);
                    instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, doEndLabel)]));

                    labels[catchLabel] = instructions.Count;
                    foreach (var cc in doStmt.Catches)
                    {
                        if (!string.IsNullOrEmpty(cc.Pattern))
                        {
                            // R0 包含异常值，存储为局部变量 (pattern binding)
                            instructions.Add(new Instruction(OpCode.MOVE,
                                [new Operand(OperandType.LABEL, $"var_{cc.Pattern}"), new Operand(OperandType.REGISTER, 0)]));
                        }
                        GenerateStatement(cc.Body);
                        instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                    }
                    labels[doEndLabel] = instructions.Count;
                    break;

                case GuardStatement guardStmt:
                    {
                        string guardEndLabel = $"guard_end_{labelCounter++}";
                        GenerateExpression(guardStmt.Condition);
                        instructions.Add(new Instruction(OpCode.JNZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, guardEndLabel)]));
                        GenerateStatement(guardStmt.Body);
                        labels[guardEndLabel] = instructions.Count;
                    }
                    break;

                case GuardLetStatement guardLetStmt:
                    {
                        // guard let x = optional else { ... }: if optional is nil, execute else branch
                        string endLabel = $"guardlet_end_{labelCounter++}";
                        GenerateExpression(guardLetStmt.OptionalExpr);
                        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                        // If optional is NOT nil (success), skip the else block
                        instructions.Add(new Instruction(OpCode.JNE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, endLabel)]));
                        // Optional is nil: execute else body (must transfer control away, e.g. return/break)
                        GenerateStatement(guardLetStmt.ElseBranch);
                        labels[endLabel] = instructions.Count;
                    }
                    break;

                case IfLetStatement ifLetStmt:
                    Sta.EmitIf(
                        () => GenerateExpression(ifLetStmt.OptionalExpr),
                        () => GenerateStatement(ifLetStmt.ThenBranch),
                        ifLetStmt.ElseBranch != null ? () => GenerateStatement(ifLetStmt.ElseBranch) : null
                    );
                    break;

                case DeferStatement deferStmt:
                    // 延迟执行：入栈，函数返回前执行
                    if (_deferStack.Count > 0)
                        _deferStack.Peek().Add(deferStmt.Body);
                    break;

                case ForEachStatement foreachStmt:
                    GenerateForEach(foreachStmt);
                    break;

                case BreakStatement _:
                    Sta!.EmitBreak();
                    break;

                case ContinueStatement _:
                    Sta!.EmitContinue();
                    break;

                case StructDeclStatement structDecl:
                    _structFields[structDecl.Name] = structDecl.Fields;
                    break;

                case EnumDeclStatement enumDecl:
                    for (int i = 0; i < enumDecl.Members.Count; i++)
                    {
                        string label = $"{enumDecl.Name}_{enumDecl.Members[i]}";
                        dataSection[label] = i;
                    }
                    break;

                case ProtocolDeclStatement _:
                    // Protocols are type-checking only, no runtime codegen needed
                    break;

                case ExtensionDeclStatement extDecl:
                    foreach (var method in extDecl.Methods)
                    {
                        string label = $"ext_{extDecl.TypeName}_{method.Name}";
                        labels[label] = instructions.Count;
                        EmitPrologue();
                        for (int i = method.Parameters.Count - 1; i >= 0; i--)
                        {
                            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                        }
                        GenerateBlock(method.Body);
                        EmitEpilogue();
                    }
                    break;
                    
                default:
                    // 未知语句类型，忽略
                    break;
            }
        }
        
        private void GenerateExpression(Expression expression)
        {
            switch (expression)
            {
                case LiteralExpression literal:
                    GenerateLiteral(literal);
                    break;
                    
                case VariableExpression variable:
                    GenerateVariable(variable);
                    break;
                    
                case BinaryExpression binary:
                    GenerateBinary(binary);
                    break;
                    
                case UnaryExpression unary:
                    GenerateUnary(unary);
                    break;
                    
                case ConditionalExpression condExpr:
                    _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
                    break;

                case AssignmentExpression assign:
                    GenerateAssignment(assign);
                    break;
                    
                case CallExpression call:
                    GenerateCall(call);
                    break;
                    
                case StringInterpolationExpression interpolation:
                    GenerateStringInterpolation(interpolation);
                    break;
                    
                case ArrayLiteralExpression arrayLiteral:
                    GenerateArrayLiteral(arrayLiteral);
                    break;

                case DictionaryLiteralExpression dictLiteral:
                    GenerateDictionaryLiteral(dictLiteral);
                    break;

                case ClosureExpression closureExpr:
                    {
                        string clLabel = $"closure_{labelCounter++}";
                        labels[clLabel] = instructions.Count;
                        EmitPrologue();
                        for (int i = closureExpr.Parameters.Count - 1; i >= 0; i--)
                        {
                            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                        }
                        if (closureExpr.Body is Block block)
                        {
                            foreach (var stmt in block.Statements)
                                if (stmt != null) GenerateStatement(stmt);
                        }
                        EmitEpilogue();
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, clLabel)]));
                    }
                    break;

                case OptionalExpression optional:
                    GenerateOptional(optional);
                    break;

                case MemberExpression memberExpr:
                    GenerateExpression(memberExpr.Object);
                    // 计算结构体字段偏移
                    if (memberExpr.Object is VariableExpression objVarExpr && _structFields.TryGetValue(objVarExpr.Name, out var fields))
                    {
                        int fieldOffset = 0;
                        for (int fi = 0; fi < fields.Count; fi++)
                        {
                            if (fields[fi].Name == memberExpr.Property)
                            {
                                fieldOffset = fi * 4;
                                break;
                            }
                        }
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R0+{fieldOffset}"), new Operand(OperandType.REGISTER, 0)]));
                    }
                    else
                    {
                        // 尝试通过类型名查找结构体字段
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                    }
                    break;

                case IndexAccessExpression indexExpr:
                    GenerateIndexAccess(indexExpr);
                    break;
                    
                default:
                    // 未知表达式类型，生成默认值
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0)
                    }));
                    break;
            }
        }
        
        private void GenerateLiteral(LiteralExpression literal) => EmitLoadConstant(literal.Value);
        
        private void GenerateVariable(VariableExpression variable)
        {
            // 获取变量的 Swift 类型，选择正确的加载指令
            SwiftType varType = _varTypes.TryGetValue(variable.Name, out SwiftType vt) ? vt : SwiftType.Int;
            var loadOp = GetLoadInstruction(varType);

            if (_localVarOffsets.TryGetValue(variable.Name, out int vOff))
            {
                instructions.Add(new Instruction(loadOp, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, $"R14{vOff}")
                }));
            }
            else
            {
                // 简化处理：假设变量在数据段中
                string label = $"var_{variable.Name}";
                if (!dataSection.ContainsKey(label))
                    dataSection[label] = 0;
                // 统一使用 MEMORY 解引用 (GetFloatValue/GetDoubleValue 已正确处理 LABEL→内存读取,
                // 但 GetLongValue 对 LABEL 错误返回地址而非值, 故 MOVEL/MOVED/MOVEF 也改用 MEMORY)
                var opType = OperandType.MEMORY;
                instructions.Add(new Instruction(loadOp, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(opType, label)
                }));
            }
        }
        
        private bool IsFloatLiteral(Expression e) => e is LiteralExpression lit && lit.Type == "Float";

        private ExpType InferSwiftType(Expression expr)
        {
            if (expr is LiteralExpression lit)
            {
                if (lit.Type == "Float") return ExpType.F32;
                if (lit.Type == "Double") return ExpType.F64;
                if (lit.Type == "Int64") return ExpType.I64;  // v1.66.66: 64-bit整数
                if (lit.Type == "String") return ExpType.Ptr32;
                if (lit.Type == "Bool") return ExpType.I8;
            }
            // 查询变量类型表，正确处理 Float/Double/Int64 变量
            if (expr is VariableExpression varExpr && _varTypes.TryGetValue(varExpr.Name, out SwiftType vt))
            {
                return vt switch
                {
                    SwiftType.Float => ExpType.F32,
                    SwiftType.Double => ExpType.F64,
                    SwiftType.Int64 => ExpType.I64,  // 64 位整数用 long 路径 (MOVEL/ADDL/DIVL)
                    _ => ExpType.I32
                };
            }
            // 二元表达式：取左右操作数类型的 widen
            if (expr is BinaryExpression binExpr)
            {
                var l = InferSwiftType(binExpr.Left);
                var r = InferSwiftType(binExpr.Right);
                // 委托 ExpressionManager 做类型提升
                if (l == ExpType.I64 || r == ExpType.I64) return ExpType.I64;
                if (l == ExpType.F64 || r == ExpType.F64) return ExpType.F64;
                if (l == ExpType.F32 || r == ExpType.F32) return ExpType.F32;
                return ExpType.I32;
            }
            return ExpType.I32;
        }

        private ExpVar WrapExpr(Expression node)
        {
            return ExpVar.Eval(InferSwiftType(node), () => GenerateExpression(node));
        }

        private ExpVar WrapTargetExpr(Expression node)
        {
            if (node is VariableExpression varExpr)
            {
                // 查询变量类型，返回正确的 ExpType
                ExpType expType = InferSwiftType(varExpr);
                if (_localVarOffsets.TryGetValue(varExpr.Name, out int offset))
                    return ExpVar.Stack(offset, 14, expType);
                string label = $"var_{varExpr.Name}";
                if (!dataSection.ContainsKey(label))
                    dataSection[label] = 0;
                return ExpVar.Data(label, expType);
            }
            return WrapExpr(node);
        }

        private void GenerateBinary(BinaryExpression binary)
        {
            // 位运算使用 ExpressionManager 统一处理
            if (binary.Operator is TokenType.BitwiseAnd or TokenType.BitwiseOr or TokenType.BitwiseXor
                or TokenType.LeftShift or TokenType.RightShift)
            {
                var bitLeft = WrapExpr(binary.Left);
                var bitRight = WrapExpr(binary.Right);
                switch (binary.Operator)
                {
                    case TokenType.BitwiseAnd: _expr!.EmitBitAnd(bitLeft, bitRight); break;
                    case TokenType.BitwiseOr:  _expr!.EmitBitOr(bitLeft, bitRight); break;
                    case TokenType.BitwiseXor: _expr!.EmitBitXor(bitLeft, bitRight); break;
                    case TokenType.LeftShift:  _expr!.EmitShl(bitLeft, bitRight); break;
                    case TokenType.RightShift: _expr!.EmitShr(bitLeft, bitRight); break;
                }
                return;
            }

            var left = WrapExpr(binary.Left);
            var right = WrapExpr(binary.Right);

            switch (binary.Operator)
            {
                case TokenType.Plus:  _expr!.EmitBinOp(left, right, "+"); break;
                case TokenType.Minus: _expr!.EmitBinOp(left, right, "-"); break;
                case TokenType.Multiply: _expr!.EmitBinOp(left, right, "*"); break;
                case TokenType.Divide: _expr!.EmitBinOp(left, right, "/"); break;
                case TokenType.Modulo: _expr!.EmitBinOp(left, right, "%"); break;
                case TokenType.LogicalAnd: _expr!.EmitAnd(left, right); break;
                case TokenType.LogicalOr:  _expr!.EmitOr(left, right); break;
                case TokenType.Equal:          _expr!.EmitCmp(left, right, "=="); break;
                case TokenType.NotEqual:       _expr!.EmitCmp(left, right, "!="); break;
                case TokenType.LessThan:       _expr!.EmitCmp(left, right, "<"); break;
                case TokenType.LessThanOrEqual:   _expr!.EmitCmp(left, right, "<="); break;
                case TokenType.GreaterThan:       _expr!.EmitCmp(left, right, ">"); break;
                case TokenType.GreaterThanOrEqual: _expr!.EmitCmp(left, right, ">="); break;
                default:
                    _expr!.EmitBinOp(left, right, "+");
                    break;
            }
        }
        
        private void GenerateUnary(UnaryExpression unary)
        {
            switch (unary.Operator)
            {
                case TokenType.Minus:
                    _expr!.EmitNeg(WrapExpr(unary.Operand));
                    break;

                case TokenType.BitwiseNot:
                    GenerateExpression(unary.Operand);
                    instructions.Add(new Instruction(OpCode.NOT, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
                    break;

                default:
                    GenerateExpression(unary.Operand);
                    break;
            }
        }
        
        private void GenerateAssignment(AssignmentExpression assign)
        {
            // 复合赋值 (+=, -=, *=, /=, %=) 使用 ExpressionManager 统一处理
            if (assign.Operator != TokenType.Assignment && assign.Target is VariableExpression)
            {
                string op = assign.Operator switch
                {
                    TokenType.PlusEqual => "+",
                    TokenType.MinusEqual => "-",
                    TokenType.MultiplyEqual => "*",
                    TokenType.DivideEqual => "/",
                    TokenType.ModuloEqual => "%",
                    _ => "+"
                };
                _expr!.EmitCompoundAssign(WrapTargetExpr(assign.Target), WrapExpr(assign.Value), op);
                return;
            }

            GenerateExpression(assign.Value);

            if (assign.Target is IndexAccessExpression idxExpr)
            {
                // arr[i] = value → 先算值, 再算地址, 最后 STORE
                // Save value on stack
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));

                // Compute address: base + 4 + i*4
                // Compute address: base + header + index*4
                GenerateExpression(idxExpr.Target);
                AddInstruction(OpCode.PUSH, [Reg(0)]);
                GenerateExpression(idxExpr.Index);
                EmitArrayElementOffset();
                AddInstruction(OpCode.POP, [Reg(1)]);
                AddInstruction(OpCode.ADD, [Reg(1), Reg(1), Reg(0)]);

                // Pop value and store at address
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Mem("R1"), new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            if (assign.Target is VariableExpression varExpr)
            {
                // 使用类型正确的存储指令
                SwiftType targetType = _varTypes.TryGetValue(varExpr.Name, out SwiftType t) ? t : SwiftType.Int;

                // 将赋值值转换到目标变量类型 (Int/Float/Double/Int64 全转换)
                EmitConvertTo(targetType, assign.Value);

                var storeOp = GetStoreInstruction(targetType);
                // 统一 store: dest-first 顺序 (MEMORY first for store)
                if (_localVarOffsets.TryGetValue(varExpr.Name, out int aOffL))
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, $"R14{aOffL}"), new Operand(OperandType.REGISTER, 0)]));
                else
                {
                    string label = $"var_{varExpr.Name}";
                    if (!dataSection.ContainsKey(label)) dataSection[label] = 0;
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, label), new Operand(OperandType.REGISTER, 0)]));
                }
            }
        }
        
        private void GenerateCall(CallExpression call)
        {
            // Built-in: peek*(addr)
            if (call.Callee is VariableExpression svpeek && svpeek.Name.StartsWith("peek") && call.Arguments.Count == 1)
            {
                string runtimeFn = "vml_" + svpeek.Name;
                GenerateExpression(call.Arguments[0]);
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                return;
            }
            // Built-in: poke*(addr, val)
            if (call.Callee is VariableExpression ce && ce.Name.StartsWith("poke") && call.Arguments.Count == 2)
            {
                string runtimeFn = "vml_" + ce.Name;
                GenerateExpression(call.Arguments[0]);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                GenerateExpression(call.Arguments[1]);
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                return;
            }
            if (call.Callee is VariableExpression ce2 && ce2.Name == "chipasm" && call.Arguments.Count >= 2)
            {
                return;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Swift 通过 Lib/shared/vmlsys.c 调用系统功能

            // 类型构造函数: Int(x)/Float(x)/Double(x) → 全类型转换 (F2I/D2I/L2I/I2F/D2F/I2D/F2D)
            if (call.Callee is VariableExpression typeCtor && call.Arguments.Count == 1)
            {
                string ctorName = typeCtor.Name;
                SwiftType? target = ctorName switch
                {
                    "Int" => SwiftType.Int,
                    "Float" => SwiftType.Float,
                    "Double" => SwiftType.Double,
                    _ => null
                };
                if (target.HasValue)
                {
                    GenerateExpression(call.Arguments[0]);
                    EmitConvertTo(target.Value, call.Arguments[0]);
                    return;
                }
            }

            // 成员方法调用: arr.append(x) or str.count
            if (call.Callee is MemberExpression memberExpr && memberExpr.Object is VariableExpression objVar)
            {
                string method = memberExpr.Property;
                if (method == "hasPrefix" && call.Arguments.Count == 1)
                {
                    // CALL arr_startswith(array, prefix) → R0 = 1/0
                    GenerateExpression(memberExpr.Object); // R0 = array
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(call.Arguments[0]);  // R0 = prefix array
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    EmitCallBuiltin("arr_startswith");
                    return;
                }
                if (method == "hasSuffix" && call.Arguments.Count == 1)
                {
                    // CALL arr_endswith(array, suffix) → R0 = 1/0
                    GenerateExpression(memberExpr.Object); // R0 = array
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(call.Arguments[0]);  // R0 = suffix array
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    EmitCallBuiltin("arr_endswith");
                    return;
                }
                if (method == "append" && call.Arguments.Count == 1)
                {
                    GenerateExpression(memberExpr.Object); // R0 = array ptr
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(call.Arguments[0]);  // R0 = value
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                    EmitCallBuiltin("arr_push"); // arr_push(arr, value)
                    return;
                }
                if (method == "count" && call.Arguments.Count == 0)
                {
                    GenerateExpression(memberExpr.Object);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                    return;
                }
                if (method == "isEmpty" && call.Arguments.Count == 0)
                {
                    GenerateExpression(memberExpr.Object);
                    EmitCompareToBool(() =>
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                    }, OpCode.JE);
                    return;
                }
                if (method == "remove" && call.Arguments.Count == 1)
                {
                    GenerateExpression(memberExpr.Object); // R0 = array ptr
                    EmitCallBuiltin("arr_pop"); // arr_pop(arr) → R0 = removed value
                    return;
                }
                if (method == "removeAll" && call.Arguments.Count == 0)
                {
                    GenerateExpression(memberExpr.Object);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                    return;
                }
            }

            // 对 print 函数特殊处理: 按参数类型选择 print_int/print_float/print_double/print_str
            if (call.Callee is VariableExpression cv && cv.Name == "print")
            {
                foreach (var arg in call.Arguments)
                {
                    bool isString = arg is LiteralExpression lit && lit.Value is string
                        || (arg is CallExpression callEx && callEx.Callee is VariableExpression callVe && IsStringReturningFunc(callVe.Name));
                    var argType = InferSwiftType(arg);
                    if (argType == ExpType.F64)
                    {
                        // print_double (io64) 用 stdcall 栈传参, 与 print_float (寄存器) 不一致;
                        // 统一 D2F 降为 32 位浮点后走 print_float 寄存器路径
                        GenerateExpression(arg);
                        EmitD2F();
                        EmitPrintFloat();
                    }
                    else
                    {
                        EmitPrintArg(() => GenerateExpression(arg), isString, argType == ExpType.F32);
                    }
                }
                EmitPrintNewline();
                return;
            }

            // CCv2+C ABI 双兼容: 全部参数从右到左压栈（满足C函数的栈参数约定），
            // 同时将前4个参数加载到 R0-R3（满足CCv2寄存器约定）
            var args = call.Arguments;
            // 获取被调用函数名（用于类型推断）
            string? calleeName = (call.Callee is VariableExpression ve) ? ve.Name : null;
            // 预计算每个参数的字节大小 (v1.66.66: 支持 4/8 字节类型)
            var argSizes = new int[args.Count];
            int totalArgBytes = 0;
            for (int i = 0; i < args.Count; i++)
            {
                var t = InferSwiftType(args[i]);
                // 若函数名含 "float" 且参数为 Double，插入 D2F 后大小变为 4
                bool downgradeToFloat = calleeName != null
                    && calleeName.Contains("float", StringComparison.OrdinalIgnoreCase)
                    && t == ExpType.F64;
                argSizes[i] = (t == ExpType.F64 || t == ExpType.I64) ? (downgradeToFloat ? 4 : 8) : 4;
                totalArgBytes += argSizes[i];
            }
            // 全部参数从右到左压栈（C ABI: 栈传递）
            for (int i = args.Count - 1; i >= 0; i--)
            {
                GenerateExpression(args[i]);
                bool upgraded = false;
                // 若函数名含 "float" 且参数为 Double，插入 D2F 转换 (v1.66.66)
                if (calleeName != null && calleeName.Contains("float", StringComparison.OrdinalIgnoreCase))
                {
                    var argType = InferSwiftType(args[i]);
                    if (argType == ExpType.F64)
                    {
                        instructions.Add(new Instruction(OpCode.D2F, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                        upgraded = true;
                    }
                }
                int size = argSizes[i];
                if (size == 8)
                {
                    // 64-bit 参数: 分配8字节栈空间
                    var argType = InferSwiftType(args[i]);
                    var storeOp = argType == ExpType.I64 ? OpCode.MOVEL : OpCode.MOVED;
                    instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)]));
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.INDIRECT, 13), new Operand(OperandType.REGISTER, 0)]));
                }
                else
                {
                    // 32-bit 参数: 若为 D2F 转换后的 float，用 MOVEF 存储；否则用 PUSH
                    if (upgraded)
                    {
                        instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.MOVEF, [new Operand(OperandType.INDIRECT, 13), new Operand(OperandType.REGISTER, 0)]));
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    }
                }
            }
            // R0 已保留最后一个求值结果（args[0]），从栈加载 R1-R3
            int byteOff = 0;
            int first4Count = Math.Min(args.Count, 4);
            for (int i = 1; i < first4Count; i++)
            {
                byteOff += argSizes[i - 1];
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, i), new Operand(OperandType.MEMORY, $"R13+{byteOff}")]));
            }

            if (call.Callee is VariableExpression varExpr)
            {
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, varExpr.Name)]));
            }

            // 清理全部栈参数（C ABI: 调用者清理）
            if (totalArgBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgBytes)]));
            }
        }
        
        /// <summary>
        /// 递归收集函数体里的局部变量声明，在栈帧上给它们留位子（名字 → 负偏移）。
        /// 不递归进嵌套函数（那有它自己的帧）。
        /// </summary>
        private void ReserveLocals(Statement stmt)
        {
            switch (stmt)
            {
                case null:
                    return;
                case VariableDeclStatement vd:
                    if (!_localVarOffsets.ContainsKey(vd.Name))
                    {
                        _localVarSize += 4;
                        _localVarOffsets[vd.Name] = -_localVarSize;
                    }
                    return;
                case Block blk:
                    foreach (var s in blk.Statements) ReserveLocals(s);
                    return;
                case IfStatement iff:
                    ReserveLocals(iff.ThenBranch);
                    ReserveLocals(iff.ElseBranch);
                    return;
                case WhileStatement wh:
                    ReserveLocals(wh.Body);
                    return;
                case ForStatement fr:
                    ReserveLocals(fr.Initializer);
                    ReserveLocals(fr.Body);
                    return;
                case DoWhileStatement dw:
                    ReserveLocals(dw.Body);
                    return;
                case SwitchStatement sw:
                    foreach (var c in sw.Cases)
                        foreach (var s in c.Body) ReserveLocals(s);
                    return;
                default:
                    return;   // 嵌套函数有自己的帧，不进来
            }
        }

        private void GenerateVariableDecl(VariableDeclStatement varDecl)
        {
            // 记录变量类型
            SwiftType varType = MapToSwiftType(varDecl.TypeAnnotation ?? "Int");
            _varTypes[varDecl.Name] = varType;

            if (varDecl.Initializer != null)
            {
                GenerateExpression(varDecl.Initializer);

                // 将初始化值转换到目标变量类型 (Int/Float/Double/Int64 全转换)
                EmitConvertTo(varType, varDecl.Initializer);

                var storeOp = GetStoreInstruction(varType);
                // **栈帧上的局部变量优先**（前导里已经预扫描留好位子）——
                // 只有这样不同函数的同名局部才互不干扰。查不到才回退数据段：
                // 那种情况是**模块级**声明（`var A = [...]` 之类），本来就该是全局。
                if (_localVarOffsets.TryGetValue(varDecl.Name, out int lofs))
                {
                    instructions.Add(new Instruction(storeOp,
                        [new Operand(OperandType.MEMORY, $"R14{lofs}"), new Operand(OperandType.REGISTER, 0)]));
                    return;
                }

                // 在数据段中分配变量空间 (Int64 需要 8 字节)
                string label = $"var_{varDecl.Name}";
                dataSection[label] = varType == SwiftType.Int64 ? 0L : 0;

                // 统一 store: dest-first 顺序 (MEMORY first for store)
                instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, label), new Operand(OperandType.REGISTER, 0)]));
            }
        }

        /// <summary>将源表达式的值转换到目标类型 (Int/Float/Double/Int64 全转换, 相同类型跳过)</summary>
        private void EmitConvertTo(SwiftType targetType, Expression sourceExpr)
        {
            var srcType = InferSwiftType(sourceExpr);
            var srcSwift = srcType switch
            {
                ExpType.F32 => SwiftType.Float,
                ExpType.F64 => SwiftType.Double,
                ExpType.I64 => SwiftType.Int64,
                _ => SwiftType.Int,
            };
            EmitTypeConversion(srcSwift, targetType);
        }
        
        private void GenerateFunctionDecl(FunctionDeclStatement funcDecl)
        {
            string funcLabel = funcDecl.Name;
            AddLabel(funcLabel);

            // native/external 函数：标签已添加，跳过函数体（由外部共享库提供实现）
            if (funcDecl.IsNative)
                return;

            // 添加函数注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; -------------------------------------------"));
            // 生成源函数声明
            var sourceDecl = $"func {funcDecl.Name}(";
            for (var i = 0; i < funcDecl.Parameters.Count; i++)
            {
                var p = funcDecl.Parameters[i];
                if (p.ExternalName != "_")
                {
                    sourceDecl += $"{p.ExternalName}: ";
                }
                sourceDecl += $"{p.InternalName}: {p.Type}";
                if (i < funcDecl.Parameters.Count - 1)
                {
                    sourceDecl += ", ";
                }
            }
            sourceDecl += ")";
            if (funcDecl.ReturnType != null && funcDecl.ReturnType != "Void")
            {
                sourceDecl += $" -> {funcDecl.ReturnType}";
            }
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            // 生成函数名注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {funcDecl.Name}"));
            // 生成参数注释
            foreach (var param in funcDecl.Parameters)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param   : {param.Type} {param.InternalName}"));
            }
            // 生成返回类型注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; return   : {funcDecl.ReturnType ?? "Void"}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; --------------------------------------------"));

            // 函数序言: 保存R14并设置帧指针
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 13)]));

            // 重置局部变量表和 defer 栈
            _localVarOffsets.Clear();
            _localVarSize = 0;
            _deferStack.Clear();
            _deferStack.Push(new List<Statement>());

            // 保存参数到栈帧 (CCv2: 前4个参数在 R0-R3, 第5+在栈上)
            for (int pi = 0; pi < funcDecl.Parameters.Count; pi++)
            {
                string pName = funcDecl.Parameters[pi].InternalName;
                _localVarSize += 4;
                _localVarOffsets[pName] = -_localVarSize;
                if (pi < 4)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R14-{_localVarSize}"), new Operand(OperandType.REGISTER, pi)]));
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R14+{8 + (pi - 4) * 4}"), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R14-{_localVarSize}"), new Operand(OperandType.REGISTER, 0)]));
                }
            }
            // ⚠ v0.96.187 新增：**预扫描函数体里的局部变量声明**，在栈帧上给它们留位子。
            //   原来这里只给参数留位，而 `GenerateVariableDecl` 是**无条件写数据段**的
            //   ⇒ 局部变量全变成"全局"，**不同函数的同名局部互相踩**（读的地方先查
            //   `_localVarOffsets` 查不到、于是也走数据段，两边一致地错）。
            //   实测症状：`func count5() -> Int { var i = 0; var s = 0; while i < 5 {…} return s }`
            //   返回 **3** 而不是 5；数据段里能看到 `var_i` / `var_k` / `var_tries` 这些本该在栈上的东西。
            //   游戏里 `draw` / `step` / `occupied` 三处都有 `var_i` ⇒ 画面立刻烂。
            if (funcDecl.Body != null)
                ReserveLocals(funcDecl.Body);

            if (_localVarSize > 0)
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, _localVarSize + 8)]));

            // 生成函数体
            if (funcDecl.Body != null)
                GenerateBlock(funcDecl.Body);

            // 执行 defer 块（后进先出）
            var defers = _deferStack.Peek();
            for (int i = defers.Count - 1; i >= 0; i--)
                GenerateStatement(defers[i]);

            // 函数尾声
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }
        
        private void GenerateReturn(ReturnStatement returnStmt)
        {
            if (returnStmt.Value != null)
            {
                GenerateExpression(returnStmt.Value);

                // 若返回值为 Int64/Double (64 位路径), 需转回整数寄存器
                // (运行时从 registers[0] 读取返回值，而非 floatRegisters[0])
                var retType = InferSwiftType(returnStmt.Value);
                if (retType.IsDouble())
                {
                    instructions.Add(new Instruction(OpCode.D2I, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                }
                else if (retType.IsLong())
                {
                    instructions.Add(new Instruction(OpCode.L2I, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                }
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0)
                }));
            }
            
            // 恢复帧指针并返回
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }
        
        private void GenerateIf(IfStatement ifStmt)
        {
            Sta!.EmitIf(
                () => GenerateExpression(ifStmt.Condition),
                () => GenerateStatement(ifStmt.ThenBranch),
                ifStmt.ElseBranch != null ? () => GenerateStatement(ifStmt.ElseBranch) : null);
        }
        
        private void GenerateWhile(WhileStatement whileStmt)
        {
            Sta!.EmitWhile(
                () => GenerateExpression(whileStmt.Condition),
                () => GenerateStatement(whileStmt.Body));
        }
        
        private void GenerateFor(ForStatement forStmt)
        {
            System.Action? emitInit = forStmt.Initializer != null
                ? () => GenerateStatement(forStmt.Initializer) : null;

            Sta!.EmitFor(
                emitInit,
                forStmt.Condition != null ? () => GenerateExpression(forStmt.Condition) : null,
                forStmt.Increment != null ? () => GenerateExpression(forStmt.Increment) : null,
                () => GenerateStatement(forStmt.Body));
        }
        
        private void GenerateDoWhile(DoWhileStatement doStmt)
        {
            Sta!.EmitDoWhile(
                () => GenerateStatement(doStmt.Body),
                () => GenerateExpression(doStmt.Condition));
        }

        private void GenerateSwitch(SwitchStatement switchStmt)
        {
            var emitCaseValues = new List<Action>();
            var caseBodies = new List<Action>();
            Action? defaultBody = null;

            foreach (var sc in switchStmt.Cases)
            {
                if (sc.Value == null)
                {
                    defaultBody = () =>
                    {
                        foreach (var stmt in sc.Body)
                            GenerateStatement(stmt);
                    };
                }
                else
                {
                    var capSc = sc;
                    emitCaseValues.Add(() => GenerateExpression(capSc.Value!));
                    caseBodies.Add(() =>
                    {
                        foreach (var stmt in capSc.Body)
                            GenerateStatement(stmt);
                    });
                }
            }

            Sta!.EmitSwitchCustom(
                () => GenerateExpression(switchStmt.Value),
                emitCaseValues,
                caseBodies,
                defaultBody
            );
        }

        private void GenerateConditional(ConditionalExpression condExpr)
        {
            _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
        }

        private void GenerateForEach(ForEachStatement foreachStmt)
        {
            string startLabel = $"foreach_start_{labelCounter}";
            string endLabel = $"foreach_end_{labelCounter}";
            labelCounter++;

            Sta!.PushLoopLabels(endLabel, startLabel);

            var varLabel = $"var_{foreachStmt.VariableName}";
            dataSection[varLabel] = 0;

            // Range expression: for i in start...end { ... }
            if (foreachStmt.Collection is BinaryExpression rangeExpr &&
                (rangeExpr.Operator == TokenType.Range || rangeExpr.Operator == TokenType.HalfOpenRange))
            {
                bool halfOpen = rangeExpr.Operator == TokenType.HalfOpenRange;
                // 生成 range start 并存入循环变量
                GenerateExpression(rangeExpr.Left);
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, varLabel)]));

                // 生成 range end 并存入临时变量
                GenerateExpression(rangeExpr.Right);
                string endVarLabel = $"__fe_end_{labelCounter}";
                dataSection[endVarLabel] = 0;
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, endVarLabel)]));

                // 循环开始: 检查循环变量 <= end (或 < end for half-open)
                labels[startLabel] = instructions.Count;
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, varLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, endVarLabel)]));
                instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
                instructions.Add(new Instruction(halfOpen ? OpCode.JGE : OpCode.JG, [Reg(0), new Operand(OperandType.LABEL, endLabel)]));

                // 循环体
                GenerateStatement(foreachStmt.Body);

                // 循环变量递增
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, varLabel)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, varLabel)]));
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, startLabel)]));
                labels[endLabel] = instructions.Count;

                Sta!.PopLoopLabels();
                return;
            }

            // 数组/集合 foreach (原有逻辑)
            string arrLabel = $"__fe_arr_{labelCounter}";
            string idxLabel = $"__fe_idx_{labelCounter}";
            labelCounter++;

            dataSection[arrLabel] = 0;
            dataSection[idxLabel] = 0;

            GenerateExpression(foreachStmt.Collection);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));

            labels[startLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, endLabel)]));

            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            EmitArrayElementOffset();
            AddInstruction(OpCode.ADD, [Reg(0), Reg(1)]);
            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]);

            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, varLabel)]));

            GenerateStatement(foreachStmt.Body);

            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.IMMEDIATE, 1), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, startLabel)]));
            labels[endLabel] = instructions.Count;

            Sta!.PopLoopLabels();
        }

        private void GenerateBlock(Block blockStmt)
        {
            foreach (var statement in blockStmt.Statements)
            {
                GenerateStatement(statement);
            }
        }
        
        private void GeneratePrint(PrintStatement printStmt)
        {
            foreach (var arg in printStmt.Arguments)
            {
                GenerateExpression(arg);
                if (arg is LiteralExpression lit && lit.Value is string)
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "swift_print")]));
                else if (arg is LiteralExpression lit3 && lit3.Value is bool)
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "swift_print_bool")]));
                else if (IsFloatLiteral(arg))
                    EmitPrintFloat();
                else
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "swift_print_int")]));
            }
            // 无参数时输出新行
            if (printStmt.Arguments.Count == 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10)]));
                EmitPrintChar();
            }
        }
        
        private void GenerateStringInterpolation(StringInterpolationExpression interpolation)
        {
            // 字符串插值: "\\(expr)" — 整数→字符串转换
            if (interpolation.Parts.Count > 0)
            {
                GenerateExpression(interpolation.Parts[0]); // R0 = value
                // 分配字符串缓冲区 + 调用 shared_itoa
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save value
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 12)]));
                EmitAlloc(); // malloc 12 → buffer
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)])); // R1 = buffer
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // R0 = value
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "itoa")]));
            }
        }
        
        private void GenerateIndexAccess(IndexAccessExpression indexExpr)
        {
            // arr[i]: base + header + index*4, then LOAD
            GenerateExpression(indexExpr.Target);
            AddInstruction(OpCode.PUSH, [Reg(0)]);
            GenerateExpression(indexExpr.Index);
            EmitArrayElementOffset();
            AddInstruction(OpCode.POP, [Reg(1)]);
            AddInstruction(OpCode.ADD, [Reg(1), Reg(1), Reg(0)]);
            AddInstruction(OpCode.MOVE, [Mem("R1"), Reg(0)]);
        }

        private void GenerateArrayLiteral(ArrayLiteralExpression arrayLiteral)
        {
            int arrId = labelCounter++;
            string dataLabel = $"arr_data_{arrId}";
            // VML 数组布局: [0]=count, [1..N]=elements — 连续存储
            int count = arrayLiteral.Elements.Count;
            object[] arrayData = new object[1 + count];
            arrayData[0] = count;
            // 编译时常量元素直接填入 dataSection
            for (int i = 0; i < count; i++)
            {
                if (arrayLiteral.Elements[i] is LiteralExpression lit && lit.Value is int iv)
                    arrayData[1 + i] = iv;
                else
                    arrayData[1 + i] = 0;
            }
            dataSection[dataLabel] = arrayData;

            // 运行时填充非常量元素
            for (int i = 0; i < count; i++)
            {
                if (!(arrayLiteral.Elements[i] is LiteralExpression lit2 && lit2.Value is int))
                {
                    GenerateExpression(arrayLiteral.Elements[i]);
                    instructions.Add(new Instruction(OpCode.MOVE, [
                        new Operand(OperandType.LABEL, $"{dataLabel}_{1 + i}"),
                        new Operand(OperandType.REGISTER, 0)
                    ]));
                }
            }
            // R0 = 数组基地址 (LEA — load effective address)
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, dataLabel)
            ]));
        }

        private void GenerateDictionaryLiteral(DictionaryLiteralExpression dictLiteral)
        {
            // Simplified: just generate all values
            foreach (var entry in dictLiteral.Entries)
            {
                GenerateExpression(entry.Value);
            }
        }
        
        private void GenerateOptional(OptionalExpression optional)
        {
            // 生成基础表达式
            GenerateExpression(optional.Expression);
            
            // 根据操作符生成相应代码
            switch (optional.Operator)
            {
                case TokenType.Question:
                    // 可选类型声明：Type?
                    // 简化处理：不做特殊处理
                    break;
                    
                case TokenType.ForceUnwrap:
                    // 强制解包：expression!
                    // 调用可选类型解包函数
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand> {
                        new Operand(OperandType.LABEL, "Optional_unwrap")
                    }));
                    break;
                    
                default:
                    // 其他操作符，保持不变
                    break;
            }
        }

        private void AddDefaultMain()
        {
            // main标签指向程序开头（第一条指令是栈初始化）
            labels["main"] = 0;
            
            // 将所有现有标签偏移2（因为要插入2条指令）
            var keys = new List<string>(labels.Keys);
            foreach (var key in keys)
            {
                if (key != "main")
                    labels[key] = labels[key] + 2;
            }
            
            // 在指令列表开头插入栈初始化
            instructions.Insert(0, new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 13), // SP
                new Operand(OperandType.IMMEDIATE, 1048572)
            }));
            instructions.Insert(1, new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 14), // BP
                new Operand(OperandType.REGISTER, 13)
            }));
            
            // 在指令列表末尾添加退出
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> {
                new Operand(OperandType.IMMEDIATE, 3) // 退出程序
            }));
        }
    }
}
