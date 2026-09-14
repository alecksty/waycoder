using VMLAssembler;
using System.Collections.Generic;
using System.Linq;
using CompilerBase;

namespace CSharpCompiler
{
    /// <summary>
    /// C#语言代码生成器（精简版）
    /// </summary>
    public class CodeGenerator : OopCodeGenerator
    {
        private Dictionary<string, string> functionEndLabels = new Dictionary<string, string>();
        private Dictionary<string, string> functionReturnTypes = new Dictionary<string, string>();
        private Dictionary<string, string> _nativeAliases = new Dictionary<string, string>();
        private string _currentClassName = "";
        
        // 栈帧局部变量追踪
        private List<string> localVarNames = new List<string>();
        private Dictionary<string, int> localVarOffsets = new Dictionary<string, int>();
        private Dictionary<string, string> _variableTypes = new Dictionary<string, string>();
        private List<string> _usingNamespaces = new();

        // OopCodeGenerator 基类已调用 InitSimpleCompiler(framePointerReg:12)

        private ExpType InferCSharpType(Expression expr)
        {
            if (expr is LiteralExpression lit)
            {
                if (lit.Value is float) return ExpType.F32;
                if (lit.Value is double) return ExpType.F64;
                // int/long 字面量在上层通过目标类型决定 (long→F64)
                if (lit.Value is long) return ExpType.F64;
                return ExpType.I32;
            }
            if (expr is VariableExpression varExpr2 && _variableTypes.TryGetValue(varExpr2.Name, out string vt2))
            {
                return vt2 switch
                {
                    "float" => ExpType.F32,
                    "double" => ExpType.F64,
                    "long" => ExpType.F64,   // 64-bit int 使用 double 路径
                    _ => ExpType.I32
                };
            }
            if (expr is CastExpression castExpr2)
            {
                return castExpr2.TargetType switch
                {
                    "float" => ExpType.F32,
                    "double" => ExpType.F64,
                    "long" => ExpType.F64,
                    _ => ExpType.I32
                };
            }
            if (expr is BinaryExpression bin)
            {
                // 比较运算符始终返回 I32
                if (bin.Operator is TokenType.Equal or TokenType.NotEqual
                    or TokenType.LessThan or TokenType.LessThanOrEqual
                    or TokenType.GreaterThan or TokenType.GreaterThanOrEqual
                    or TokenType.LogicalAnd or TokenType.LogicalOr)
                    return ExpType.I32;
                // 算术运算：取两个操作数的更宽类型
                ExpType leftType = InferCSharpType(bin.Left);
                ExpType rightType = InferCSharpType(bin.Right);
                // F64 比 F32 宽, F32 比 I32 宽
                if (leftType == ExpType.F64 || rightType == ExpType.F64) return ExpType.F64;
                if (leftType == ExpType.F32 || rightType == ExpType.F32) return ExpType.F32;
                return ExpType.I32;
            }
            return ExpType.I32;
        }

        private ExpVar WrapExpr(Expression expr)
            => ExpVar.Eval(InferCSharpType(expr), () => GenerateExpression(expr));

        private ExpVar WrapTargetExpr(Expression expr)
        {
            if (expr is VariableExpression varExpr)
            {
                ExpType expType = InferCSharpType(varExpr);
                if (localVarOffsets.TryGetValue(varExpr.Name, out int offset))
                    return ExpVar.Stack(offset, 12, expType);
                string label = $"var_{varExpr.Name}";
                if (!dataSection.ContainsKey(label))
                    dataSection[label] = 0;
                return ExpVar.Data(label, expType);
            }
            return WrapExpr(expr);
        }

        private Program? _program;

        public override VmlProgram GenerateCode()
        {
            if (_program == null) throw new System.InvalidOperationException("No AST program set");
            return Generate(_program);
        }

        public VmlProgram Generate(Program program)
        {
            _program = program;
            // 保存 using 命名空间用于类型解析
            _usingNamespaces = program.Namespaces;
            
            // 生成程序代码
            foreach (var statement in program.Statements)
            {
                GenerateStatement(statement);
            }
            
            // 如果没有main标签，添加默认的main
            if (!labels.ContainsKey("main"))
            {
                // 如果有函数但没有 Main 方法，自动生成 main 调用第一个函数
                // 以便 FloatToInt/Int64/Float64 等测试可以直接断言 R0 返回值
                if (functionEndLabels.Count > 0)
                {
                    string firstFunc = functionEndLabels.Keys.First();
                    labels["main"] = instructions.Count;
                    // 设置帧指针 (SP由运行时设置)
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 13)]));
                    // 调用第一个函数，返回值在 R0
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, firstFunc)]));
                    // 如果函数返回 long/double/float，需要将返回值从专用寄存器转到 R0
                    if (functionReturnTypes.TryGetValue(firstFunc, out string retType))
                    {
                        if (retType == "long" || retType == "double")
                            EmitD2I();
                        else if (retType == "float")
                            instructions.Add(new Instruction(OpCode.F2I, [Reg(0), Reg(0)]));
                    }
                    // 退出程序
                    EmitExit();
                }
                else
                {
                    EmitDefaultMain();
                }
            }
            
            // 创建VML程序
            return BuildProgram("main");
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
                    
                case ReturnStatement returnStmt:
                    GenerateReturn(returnStmt);
                    break;
                    
                case ConsoleWriteLineStatement writeLineStmt:
                    GenerateConsoleWriteLine(writeLineStmt);
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

                case ForEachStatement foreachStmt:
                    GenerateForEach(foreachStmt);
                    break;
                    
                case EnumDeclStatement enumDecl:
                    GenerateEnum(enumDecl);
                    break;

                case MethodDeclStatement methodDecl:
                    GenerateMethodDecl(methodDecl);
                    break;
                    
                case Block blockStmt:
                    GenerateBlock(blockStmt);
                    break;
                    
                case BreakStatement breakStmt:
                    Sta!.EmitBreak();
                    break;

                case ContinueStatement continueStmt:
                    Sta!.EmitContinue();
                    break;
                case PropertyDeclaration propDecl:
                    // 属性简化：作为字段存储
                    string propLabel = $"prop_{propDecl.Name}";
                    if (!dataSection.ContainsKey(propLabel))
                        dataSection[propLabel] = 0;
                    break;
                case ConstructorDeclaration ctorDecl:
                    // 构造函数看作以类名命名的方法
                    GenerateMethodDecl(new MethodDeclStatement(ctorDecl.Name, ctorDecl.Name, ctorDecl.Parameters, ctorDecl.Body));
                    break;
                case ClassDeclaration classDecl:
                    // 类成员逐个生成（方法等）
                    string prevClass = _currentClassName;
                    _currentClassName = classDecl.Name;
                    foreach (var member in classDecl.Members)
                        GenerateStatement(member);
                    _currentClassName = prevClass;
                    break;

                case SwitchStatement switchStmt:
                    GenerateSwitch(switchStmt);
                    break;

                case DoWhileStatement doWhileStmt:
                    GenerateDoWhile(doWhileStmt);
                    break;

                case TryStatement tryStmt:
                    GenerateTry(tryStmt);
                    break;

                case ThrowStatement throwStmt:
                    GenerateThrow(throwStmt);
                    break;

                case UnsafeBlock unsafeBlock:
                    // unsafe { ... } — just generate body (pointer ops handled in expressions)
                    GenerateBlock(unsafeBlock.Body);
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
                    
                case AssignmentExpression assign:
                    GenerateAssignment(assign);
                    break;
                    
                case ArrayLiteralExpression arrayLiteral:
                    GenerateArrayLiteral(arrayLiteral);
                    break;

                case CallExpression callExpr:
                    GenerateFunctionCall(callExpr);
                    break;

                case NewExpression newExpr:
                    GenerateNewExpression(newExpr);
                    break;

                case MemberExpression memberExpr:
                    GenerateMember(memberExpr);
                    break;

                case ConditionalExpression condExpr:
                    _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
                    break;

                case IndexExpression indexExpr:
                    GenerateIndex(indexExpr);
                    break;

                case CastExpression castExpr:
                    // 类型转换：(int)float → F2I, (int)double → D2I, (float)int → I2F, (double)int → I2D
                    {
                        ExpType srcType = InferCSharpType(castExpr.Operand);
                        string targetType = castExpr.TargetType;
                        GenerateExpression(castExpr.Operand);
                        // 根据目标和源类型插入转换指令
                        if (targetType == "int")
                        {
                            if (srcType == ExpType.F32)
                                instructions.Add(new Instruction(OpCode.F2I, [Reg(0), Reg(0)]));
                            else if (srcType == ExpType.F64)
                                EmitD2I();
                            // I32 → I32: no conversion needed
                        }
                        else if (targetType == "float")
                        {
                            if (srcType == ExpType.I32)
                                EmitI2F();
                            else if (srcType == ExpType.F64)
                                instructions.Add(new Instruction(OpCode.D2F, [Reg(0), Reg(0)]));
                        }
                        else if (targetType == "double" || targetType == "long")
                        {
                            if (srcType == ExpType.I32)
                                EmitI2D();
                            else if (srcType == ExpType.F32)
                                EmitF2D();
                        }
                    }
                    break;

                case DerefExpression derefExpr:
                    // *ptr → indirect load: LOAD R0, [MEMORY R0]
                    GenerateExpression(derefExpr.Target);
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
                    break;

                case AddrOfExpression addrExpr:
                    // &var → LEA to get effective address
                    if (addrExpr.Target is VariableExpression ve)
                    {
                        if (localVarOffsets.TryGetValue(ve.Name, out int loff))
                        {
                            // Local variable: compute address from frame pointer
                            instructions.Add(new Instruction(OpCode.MOVE,
                                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, Vars.FormatOffset(loff))]));
                        }
                        else
                        {
                            // Global/static variable: load data section label address
                            string label = $"var_{ve.Name}";
                            if (!dataSection.ContainsKey(label))
                                dataSection[label] = 0;
                            instructions.Add(new Instruction(OpCode.MOVE,
                                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label)]));
                        }
                    }
                    else
                    {
                        GenerateExpression(addrExpr.Target);
                    }
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
            if (variable.Name == "this") return;  // this 引用，R0 已经指向当前对象

            // 根据变量类型选择加载指令：float→MOVEF, double/long→MOVED, 其他→MOVE
            OpCode loadOp = OpCode.MOVE;
            if (_variableTypes.TryGetValue(variable.Name, out string vtype))
            {
                loadOp = vtype switch
                {
                    "float" => OpCode.MOVEF,
                    "double" => OpCode.MOVED,
                    "long" => OpCode.MOVED,   // long 使用 double 路径
                    _ => OpCode.MOVE
                };
            }

            // 优先从栈帧加载局部变量
            if (localVarOffsets.TryGetValue(variable.Name, out int offset))
            {
                instructions.Add(new Instruction(loadOp, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, Vars.FormatOffset(offset))]));
                return;
            }

            // 回退到数据段（全局/静态变量或参数）
            string label = $"var_{variable.Name}";
            if (!dataSection.ContainsKey(label))
            {
                dataSection[label] = 0;
            }

            instructions.Add(new Instruction(loadOp, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label)]));
        }
        
        private void GenerateIndex(IndexExpression indexExpr)
        {
            string arrLabel = $"__idx_arr_{labelCounter}";
            string idxLabel = $"__idx_val_{labelCounter}";
            labelCounter++;
            dataSection[arrLabel] = 0;
            dataSection[idxLabel] = 0;

            // 计算数组表达式 → R0
            GenerateExpression(indexExpr.Object);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, arrLabel), new Operand(OperandType.REGISTER, 0)]));

            // 计算索引表达式 → R0
            GenerateExpression(indexExpr.Index);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));

            // 加载数组指针 → R1
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            // 加载索引 → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            // R0 = index * 4 + 4
            instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.IMMEDIATE, 4), new Operand(OperandType.REGISTER, 0)]));
            // R1 = R1 + R0 → points to element
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
            // Load element → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));
        }

        private void GenerateConditional(ConditionalExpression condExpr)
        {
            _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
        }

        private void GenerateNewExpression(NewExpression newExpr)
        {
            int objSize = 4 + newExpr.Arguments.Count * 4; // vtable ptr + fields
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, objSize)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "alloc")]));
            // R0 = object pointer
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
            // Store constructor args as fields
            for (int i = 0; i < newExpr.Arguments.Count; i++)
            {
                GenerateExpression(newExpr.Arguments[i]);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{4 + i * 4}"), new Operand(OperandType.REGISTER, 0)]));
            }
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
        }

        private void GenerateMember(MemberExpression memberExpr)
        {
            if (memberExpr.Object is VariableExpression varExpr)
            {
                string enumLabel = $"{varExpr.Name}_{memberExpr.Member}";
                if (dataSection.ContainsKey(enumLabel))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, enumLabel)]));
                    return;
                }
            }
            // array.Length → 加载数组指针，读取 [ptr+0]
            if (memberExpr.Member == "Length" || memberExpr.Member == "Count")
            {
                string ptrLabel = $"__lenptr_{labelCounter}";
                labelCounter++;
                dataSection[ptrLabel] = 0;
                GenerateExpression(memberExpr.Object);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, ptrLabel), new Operand(OperandType.REGISTER, 0)]));
                // Load array_ptr → R1, then [R1] is length
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, ptrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
        }

        private void GenerateBinary(BinaryExpression binary)
        {
            // NullCoalescing (??): manual label-based handling
            if (binary.Operator is TokenType.NullCoalescing)
            {
                // a ?? b: if a != null, use a; otherwise use b
                GenerateExpression(binary.Left);   // R0 = left
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(binary.Right);  // R0 = right
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)])); // R1 = left, R0 = right
                string useLeft = $"nc_left_{labelCounter}";
                string endLabel = $"nc_end_{labelCounter++}";
                // If left (R1) != null, use it
                instructions.Add(new Instruction(OpCode.JNZ, [Reg(1), new Operand(OperandType.LABEL, useLeft)]));
                // left is null, right is already in R0
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));
                labels[useLeft] = instructions.Count;
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)])); // R0 = left (non-null)
                labels[endLabel] = instructions.Count;
                return;
            }

            var left = WrapExpr(binary.Left);
            var right = WrapExpr(binary.Right);
            string op = binary.Operator switch
            {
                TokenType.Plus => "+", TokenType.Minus => "-", TokenType.Multiply => "*",
                TokenType.Divide => "/", TokenType.Modulo => "%",
                TokenType.Equal => "==", TokenType.NotEqual => "!=",
                TokenType.LessThan => "<", TokenType.LessThanOrEqual => "<=",
                TokenType.GreaterThan => ">", TokenType.GreaterThanOrEqual => ">=",
                TokenType.LogicalAnd => "&&", TokenType.LogicalOr => "||",
                TokenType.BitwiseAnd => "&", TokenType.BitwiseOr => "|", TokenType.BitwiseXor => "^",
                TokenType.LeftShift => "<<", TokenType.RightShift => ">>",
                _ => ""
            };
            if (_expr!.EmitStandardBinaryOps(op, left, right)) return;
            if (_expr!.EmitBitwiseOps(op, left, right)) return;
        }
        
        private void GenerateUnary(UnaryExpression unary)
        {
            switch (unary.Operator)
            {
                case TokenType.Minus:
                    _expr!.EmitNeg(WrapExpr(unary.Operand));
                    break;
                case TokenType.LogicalNot:
                case TokenType.BitwiseNot:
                    _expr!.EmitNot(WrapExpr(unary.Operand));
                    break;
            }
        }
        
        private void GenerateAssignment(AssignmentExpression assign)
        {
            // *ptr = value → indirect store via MEMORY addressing
            if (assign.Target is DerefExpression deref)
            {
                if (assign.Value != null)
                    GenerateExpression(assign.Value);
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));  // save value
                GenerateExpression(deref.Target);                          // R0 = ptr address
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));   // R1 = value, R0 = address
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.MEMORY, "R0"), Reg(1)]));
                return;
            }

            if (assign.Target is IndexExpression idxExpr)
            {
                string arrLabel = $"__asn_arr_{labelCounter}";
                string idxLabel = $"__asn_idx_{labelCounter}";
                labelCounter++;
                dataSection[arrLabel] = 0;
                dataSection[idxLabel] = 0;

                GenerateExpression(idxExpr.Object);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, arrLabel), new Operand(OperandType.REGISTER, 0)]));

                GenerateExpression(idxExpr.Index);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));

                if (assign.Value != null)
                    GenerateExpression(assign.Value);

                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
                instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.IMMEDIATE, 4), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 2)]));
                return;
            }

            // 自增/自减：++expr 或 expr++
            if (assign.Operator == TokenType.Increment || assign.Operator == TokenType.Decrement)
            {
                if (assign.Target is VariableExpression)
                {
                    bool isPostfix = assign.Value == null;
                    var target = WrapTargetExpr(assign.Target);
                    if (assign.Operator == TokenType.Increment)
                    {
                        if (isPostfix) _expr!.EmitPostfixInc(target);
                        else _expr!.EmitPrefixInc(target);
                    }
                    else
                    {
                        if (isPostfix) _expr!.EmitPostfixDec(target);
                        else _expr!.EmitPrefixDec(target);
                    }
                }
                return;
            }

            // 复合赋值: +=, -=, *= 等
            if (assign.Operator != TokenType.Assignment && assign.Target is VariableExpression)
            {
                var target = WrapTargetExpr(assign.Target);
                string op = assign.Operator switch
                {
                    TokenType.PlusEqual => "+",
                    TokenType.MinusEqual => "-",
                    TokenType.MultiplyEqual => "*",
                    TokenType.DivideEqual => "/",
                    TokenType.ModuloEqual => "%",
                    _ => throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown compound operator: {assign.Operator}")
                };
                _expr!.EmitCompoundAssign(target, WrapExpr(assign.Value), op);
                return;
            }

            if (assign.Value != null)
                GenerateExpression(assign.Value);
            else return;

            if (assign.Target is VariableExpression varExpr2)
            {
                // 根据变量类型选择存储指令
                OpCode storeOp = OpCode.MOVE;
                if (_variableTypes.TryGetValue(varExpr2.Name, out string varType))
                {
                    storeOp = varType switch
                    {
                        "float" => OpCode.MOVEF,
                        "double" => OpCode.MOVED,
                        "long" => OpCode.MOVED,   // long 使用 double 路径
                        _ => OpCode.MOVE
                    };
                }

                // 优先存到栈帧局部变量
                if (localVarOffsets.TryGetValue(varExpr2.Name, out int loff))
                {
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, Vars.FormatOffset(loff)), new Operand(OperandType.REGISTER, 0)]));
                }
                else
                {
                    string label = $"var_{varExpr2.Name}";
                    if (!dataSection.ContainsKey(label))
                        dataSection[label] = 0;
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.LABEL, label), new Operand(OperandType.REGISTER, 0)]));
                }
            }
        }
        
        private void GenerateVariableDecl(VariableDeclStatement varDecl)
        {
            // 在栈帧上分配局部变量
            string varName = varDecl.Name;
            // 记录变量类型
            if (!_variableTypes.ContainsKey(varName))
                _variableTypes[varName] = varDecl.Type;

            // 根据类型确定大小和存储指令
            bool isFloat = varDecl.Type == "float";
            bool isDoubleOrLong = varDecl.Type == "double" || varDecl.Type == "long";
            int size = isDoubleOrLong ? 8 : 4;
            OpCode storeOp = isFloat ? OpCode.MOVEF : (isDoubleOrLong ? OpCode.MOVED : OpCode.MOVE);

            if (!localVarOffsets.ContainsKey(varName))
            {
                var varInfo = Vars.AllocLocal(varName, size);
                localVarNames.Add(varName);
                localVarOffsets[varName] = varInfo.Offset;
                // 分配栈空间
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, size)]));
            }

            if (varDecl.Initializer != null)
            {
                // 生成初始值表达式，结果在 R0
                ExpType srcType = InferCSharpType(varDecl.Initializer);
                GenerateExpression(varDecl.Initializer);

                // 如果源类型与目标类型的寄存器文件不匹配，插入转换指令
                // float→double: F2D, int→float: I2F, int→double/long: I2D, double→float: D2F
                if (isFloat && srcType == ExpType.I32)
                    instructions.Add(new Instruction(OpCode.I2F, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                else if (isFloat && srcType == ExpType.F64)
                    instructions.Add(new Instruction(OpCode.D2F, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                else if (isDoubleOrLong && srcType == ExpType.I32)
                    instructions.Add(new Instruction(OpCode.I2D, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                else if (isDoubleOrLong && srcType == ExpType.F32)
                    instructions.Add(new Instruction(OpCode.F2D, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));

                // 统一 MOVE 系列: dest-first 操作数顺序 [Mem, Reg]
                int loff = localVarOffsets[varName];
                instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, Vars.FormatOffset(loff)), new Operand(OperandType.REGISTER, 0)]));
            }
        }
        
        private void GenerateEnum(EnumDeclStatement enumDecl)
        {
            for (int i = 0; i < enumDecl.Members.Count; i++)
            {
                string label = $"{enumDecl.Name}_{enumDecl.Members[i]}";
                dataSection[label] = i;
            }
        }

        private void GenerateMethodDecl(MethodDeclStatement method)
        {
            // native 方法：存储别名映射，不生成函数体
            if (method.IsNative)
            {
                string qualifiedName = string.IsNullOrEmpty(_currentClassName)
                    ? method.Name
                    : $"{_currentClassName}_{method.Name}";
                _nativeAliases[qualifiedName] = method.AliasName ?? method.Name;
                // 也注册不带类前缀的名称（用于直接调用）
                _nativeAliases[method.Name] = method.AliasName ?? method.Name;
                // 注册返回类型
                functionReturnTypes[method.Name] = method.ReturnType;
                return;
            }

            string funcLabel = method.Name;
            string endLabel = $"{funcLabel}_end";
            functionEndLabels[funcLabel] = endLabel;
            functionReturnTypes[funcLabel] = method.ReturnType;
            string prevFunc = _currentFunctionName;
            _currentFunctionName = funcLabel;
            
            // 重置栈帧局部变量追踪（每个方法独立）
            Vars.ResetLocals();
            localVarNames.Clear();
            localVarOffsets.Clear();

            if (!labels.ContainsKey(funcLabel))
                labels[funcLabel] = instructions.Count;

            // 添加函数注释
            var headerParams = new List<(string, string)>();
            foreach (var p in method.Parameters) headerParams.Add((p.Type, p.Name));
            EmitMethodHeader(method.Name, method.ReturnType, headerParams);
            // 添加函数标签指令
            AddLabel(funcLabel);

            // Main方法同时注册为main入口点
            if (funcLabel == "Main" && !labels.ContainsKey("main"))
            {
                labels["main"] = instructions.Count;
            }

            EmitPrologue();

            // Parameters are at [R12+12], [R12+16], etc. (after saved R12, R15, return_addr)
            for (int pi = 0; pi < method.Parameters.Count; pi++)
            {
                var param = method.Parameters[pi];
                int stackOff = -(localVarNames.Count + 1) * 4;
                Vars.AllocLocal(param.Name, 4);
                localVarNames.Add(param.Name);
                localVarOffsets[param.Name] = stackOff;
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, Vars.FormatOffset(12 + pi * 4)), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, Vars.FormatOffset(stackOff)), new Operand(OperandType.REGISTER, 0)]));
            }

            GenerateBlock(method.Body);

            // Main 方法自动生成退出系统调用
            if (method.Name == "Main")
            {
                EmitExit();
            }

            labels[endLabel] = instructions.Count;
            EmitEpilogue();

            _currentFunctionName = prevFunc;
        }

        private void GenerateReturn(ReturnStatement returnStmt)
        {
            if (returnStmt.Value != null)
            {
                GenerateExpression(returnStmt.Value);
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0)
                }));
            }

            if (functionEndLabels.TryGetValue(_currentFunctionName, out string endLabel))
            {
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> {
                    new Operand(OperandType.LABEL, endLabel)
                }));
            }
        }

        private string _currentFunctionName = "";
        
        private void GenerateFunctionCall(CallExpression call)
        {
            string funcName = "";
            if (call.Callee is VariableExpression calleeVar)
                funcName = calleeVar.Name;
            else if (call.Callee is MemberExpression member)
                funcName = ResolveMemberName(member);
            else if (call.Callee is Expression expr)
                funcName = expr.GetType().Name; // fallback

            // native 方法：直接 CALL 共享库标签
            if (_nativeAliases.TryGetValue(funcName, out string nativeLabel))
            {
                for (int i = call.Arguments.Count - 1; i >= 0; i--)
                {
                    GenerateExpression(call.Arguments[i]);
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, nativeLabel)]));
                if (call.Arguments.Count > 0)
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)]));
                return;
            }

            // Built-in: peek*(addr) → shared_peek*
            if (funcName.StartsWith("peek") && call.Arguments.Count == 1)
            {
                string runtimeFn = funcName;
                GenerateExpression(call.Arguments[0]);
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]));
                return;
            }
            // Built-in: poke*(addr, val) → poke*
            if (funcName.StartsWith("poke") && call.Arguments.Count == 2)
            {
                string runtimeFn = funcName;
                GenerateExpression(call.Arguments[0]); // addr → R0
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateExpression(call.Arguments[1]); // val → R0
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)])); // R1 = val
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // R0 = addr
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]));
                return;
            }
            if (funcName == "chipasm" && call.Arguments.Count >= 2)
            {
                return;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，C# 通过 Lib/shared/vmlsys.c 调用系统功能

            // 成员方法名 → stdlib 标签映射（标签定义在 Lib/csharp/console.vml）
            var memberLabels = new Dictionary<string, string>
            {
                { "Console_WriteLine", "PrintlnStr" },
                { "Console_Write", "PrintStr" },
                { "Console_WriteLine_int", "PrintlnInt" },
                { "Console_Write_int", "PrintInt" },
                { "System_Console_WriteLine", "PrintlnStr" },
                { "System_Console_Write", "PrintStr" },
                { "System_Console_WriteLine_int", "PrintlnInt" },
                { "System_Console_Write_int", "PrintInt" },
            };
            // 如果直接名称未找到，尝试 using 命名空间前缀
            if (!memberLabels.TryGetValue(funcName, out var stdlibLabel))
            {
                foreach (var ns in _usingNamespaces)
                {
                    string prefixed = $"{ns}_{funcName}";
                    if (memberLabels.TryGetValue(prefixed, out stdlibLabel))
                        break;
                }
            }
            if (stdlibLabel != null)
            {
                // v1.66.58: System.Console.Write/WriteLine 根据参数类型选择 PrintInt/PrintStr
                if (call.Arguments.Count == 1 && (funcName == "System_Console_Write" || funcName == "System_Console_WriteLine"))
                {
                    bool isInt = IsIntExpression(call.Arguments[0]);
                    bool isNewline = funcName == "System_Console_WriteLine";
                    stdlibLabel = isInt ? (isNewline ? "PrintlnInt" : "PrintInt")
                                       : (isNewline ? "PrintlnStr" : "PrintStr");
                }
                for (int i = call.Arguments.Count - 1; i >= 0; i--)
                {
                    GenerateExpression(call.Arguments[i]);
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, stdlibLabel)]));
                if (call.Arguments.Count > 0)
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)]));
                return;
            }

            int totalArgBytes = 0;
            for (int i = call.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(call.Arguments[i]);
                totalArgBytes += PushCallArg(call.Arguments[i]);
            }
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, funcName)]));
            if (totalArgBytes > 0)
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgBytes)]));
        }

        /// <summary>
        /// 类型感知的参数压栈 — 返回值是压栈的字节数。
        /// double/long 字面量是 8 字节，需用 MOVED/MOVEL 写入主栈 [R13] 而非 PUSH R0 (4 字节)，
        /// 否则高 32 位丢失，double_to_str/long_to_str 等共享库函数会收到错误的值 (0.0 / 0)。
        /// </summary>
        private int PushCallArg(Expression arg)
        {
            if (arg is LiteralExpression lit && lit.Value is double)
            {
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)]));
                instructions.Add(new Instruction(OpCode.MOVED, [new Operand(OperandType.INDIRECT, 13), new Operand(OperandType.REGISTER, 0)]));
                return 8;
            }
            if (arg is LiteralExpression litLong && litLong.Value is long l && (l < int.MinValue || l > int.MaxValue))
            {
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)]));
                instructions.Add(new Instruction(OpCode.MOVEL, [new Operand(OperandType.INDIRECT, 13), new Operand(OperandType.REGISTER, 0)]));
                return 8;
            }
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            return 4;
        }

        // 将 MemberExpression 递归解析为下划线分隔的函数名（如 "Console_WriteLine"）
        private string ResolveMemberName(MemberExpression member)
        {
            var parts = new List<string> { member.Member };
            var obj = member.Object;
            while (obj is MemberExpression inner)
            {
                parts.Insert(0, inner.Member);
                obj = inner.Object;
            }
            if (obj is VariableExpression ve)
                parts.Insert(0, ve.Name);
            return string.Join("_", parts);
        }

        private bool IsIntExpression(Expression arg)
        {
            // 字面量整数
            if (arg is LiteralExpression lit && lit.Value is int)
                return true;
            // 变量表达式 — 检查类型
            if (arg is VariableExpression varExpr)
            {
                if (_variableTypes.TryGetValue(varExpr.Name, out string type))
                    return IsNumericType(type);
                return false; // unknown type, treat as string
            }
            // 二元/一元表达式产生整数结果
            if (arg is BinaryExpression || arg is UnaryExpression)
                return true;
            // 索引表达式（数组访问）产生整数
            if (arg is IndexExpression)
                return true;
            // 函数调用 — 检查返回类型 (v1.66.58: Conv.StrToInt 等返回 int)
            if (arg is CallExpression call)
            {
                string name = "";
                if (call.Callee is VariableExpression ve2)
                    name = ve2.Name;
                else if (call.Callee is MemberExpression me)
                    name = ResolveMemberName(me);
                // 匹配整数返回函数 (驼峰 Java 风格 + 蛇形 C 风格)
                string lowerName = name.ToLower();
                if (name.EndsWith("StrToInt") || name.EndsWith("StrToLong") || name.EndsWith("StrToUint")
                    || name.EndsWith("StrToUlong") || name.EndsWith("StrToByte") || name.EndsWith("StrToSByte")
                    || name.EndsWith("StrToShort") || name.EndsWith("StrToUshort")
                    || name.EndsWith("StrToBool") || name.EndsWith("StrToChar")
                    || name.EndsWith("Atol") || name.EndsWith("Atoi") || name.EndsWith("Ltoa")
                    || name.Contains("StrToInt") || name.Contains("StrToLong")
                    || lowerName.Contains("str_to_int") || lowerName.Contains("str_to_long")
                    || lowerName.Contains("str_to_uint") || lowerName.Contains("str_to_ulong")
                    || lowerName == "atoi" || lowerName == "atol" || lowerName == "ltoa")
                    return true;
            }
            return false;
        }

        private bool IsNumericType(string type)
        {
            return type is "int" or "long" or "short" or "byte" or "sbyte" or "ushort" or "uint" or "ulong"
                or "float" or "double" or "decimal" or "bool" or "char";
        }

        private bool IsStringExpression(Expression arg)
        {
            // 字符串字面量
            if (arg is LiteralExpression lit && lit.Value is string)
                return true;
            // 字符串变量
            if (arg is VariableExpression varExpr)
            {
                if (_variableTypes.TryGetValue(varExpr.Name, out string type))
                    return type == "string";
                return true; // 未知类型，保守视为字符串
            }
            return false;
        }

        private void GenerateConsoleWriteLine(ConsoleWriteLineStatement writeLineStmt)
        {
            bool newline = writeLineStmt.HasNewLine;
            foreach (var arg in writeLineStmt.Arguments)
            {
                GenerateExpression(arg);
                // 库函数从栈上取参 [R12+12]，需要 PUSH R0 传参
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));

                // v1.65.171+: 映射到 Lib/csharp/console.vml 中的实际标签
                string label;
                if (IsIntExpression(arg))
                    label = newline ? "PrintlnInt" : "PrintInt";
                else if (IsStringExpression(arg))
                    label = newline ? "PrintlnStr" : "PrintStr";
                else
                    label = newline ? "PrintlnStr" : "PrintStr";

                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> {
                    new Operand(OperandType.LABEL, label)
                }));
                // PrintlnStr/shared_println_str 内部已 ADD R13 #8 清理参数, 调用方不需额外清理
            }
            if (writeLineStmt.Arguments.Count == 0 && writeLineStmt.HasNewLine)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10)]));
                EmitPrintChar();
            }
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
        
        private void GenerateForEach(ForEachStatement foreachStmt)
        {
            string startLabel = $"foreach_start_{labelCounter}";
            string endLabel = $"foreach_end_{labelCounter}";
            string arrLabel = $"__foreach_arr_{labelCounter}";
            string idxLabel = $"__foreach_idx_{labelCounter}";
            labelCounter++;

            dataSection[arrLabel] = 0;
            dataSection[idxLabel] = 0;

            // 计算集合表达式（结果在R0中：数组指针）
            GenerateExpression(foreachStmt.Collection);

            // 保存数组指针到数据段
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, arrLabel), new Operand(OperandType.REGISTER, 0)]));

            // 索引 = 0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));

            labels[startLabel] = instructions.Count;

            // 加载数组指针 → R1
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            // 读取数组长度（第1个字）→ R2
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R1")]));
            // 加载索引 → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            // R0 >= R2? 则跳出
            instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, endLabel)]));

            // 重新加载数组指针 → R1，索引 → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            // R0 = (index * 4) + 4 → 元素地址偏移
            instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.IMMEDIATE, 4), new Operand(OperandType.REGISTER, 0)]));
            // R1 = R1 + R0 → 指向元素
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
            // 加载元素值 → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));

            // 保存到循环变量（使用数据段变量）
            string varLabel = $"var_{foreachStmt.VariableName}";
            dataSection[varLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, varLabel), new Operand(OperandType.REGISTER, 0)]));

            // 循环体
            GenerateStatement(foreachStmt.Body);

            // 索引++
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.IMMEDIATE, 1), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));

            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, startLabel)]));
            labels[endLabel] = instructions.Count;
        }

        private void GenerateBlock(Block blockStmt)
        {
            foreach (var statement in blockStmt.Statements)
            {
                GenerateStatement(statement);
            }
        }
        
        private void GenerateArrayLiteral(ArrayLiteralExpression arrayLiteral)
        {
            int arrId = labelCounter++;
            // 数据段布局: [0]=pointer占位, [1]=length, [2..]=elements
            string ptrLabel = $"arr_ptr_{arrId}";
            string dataLabel = $"arr_data_{arrId}";
            dataSection[ptrLabel] = 0;
            dataSection[dataLabel] = arrayLiteral.Elements.Count;
            
            // 预分配元素空间
            for (int i = 0; i < arrayLiteral.Elements.Count; i++)
            {
                dataSection[$"{dataLabel}_{i}"] = 0;
            }
            
            // 填充每个元素（运行时求值）
            for (int i = 0; i < arrayLiteral.Elements.Count; i++)
            {
                GenerateExpression(arrayLiteral.Elements[i]);
                instructions.Add(new Instruction(OpCode.MOVE, [
                    new Operand(OperandType.LABEL, $"{dataLabel}_{i}"),
                    new Operand(OperandType.REGISTER, 0)
                ]));
            }
            
            // R0 = array地址（指向dataLabel，即length所在位置）
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, dataLabel)
            ]));
            // 保存array指针
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.LABEL, ptrLabel),
                new Operand(OperandType.REGISTER, 0)
            ]));
            // R0恢复为array指针（供调用者使用）
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, ptrLabel)
            ]));
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

        private void GenerateDoWhile(DoWhileStatement doWhileStmt)
        {
            Sta!.EmitDoWhile(
                () => GenerateStatement(doWhileStmt.Body),
                () => GenerateExpression(doWhileStmt.Condition));
        }

        private void GenerateTry(TryStatement tryStmt)
        {
            if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
            {
                // MCU: 仅执行 try body
                GenerateStatement(tryStmt.Body);
                return;
            }

            // OS 模式: CATCH/ENDCATCH 异常处理
            string catchLabel = $"try_catch_{labelCounter++}";
            string endLabel = $"try_end_{labelCounter++}";

            instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
            GenerateStatement(tryStmt.Body);
            instructions.Add(new Instruction(OpCode.ENDCATCH, []));
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));

            labels[catchLabel] = instructions.Count;
            foreach (var cc in tryStmt.Catches)
            {
                if (cc.VariableName != null)
                {
                    // R0 包含异常对象引用，存储到全局标签
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.LABEL, $"var_{cc.VariableName}"), new Operand(OperandType.REGISTER, 0)]));
                }
                GenerateStatement(cc.Body);
                instructions.Add(new Instruction(OpCode.ENDCATCH, []));
            }
            labels[endLabel] = instructions.Count;
        }

        private void GenerateThrow(ThrowStatement throwStmt)
        {
            if (HandleMCUThrow(() => { if (throwStmt.Value != null) GenerateExpression(throwStmt.Value); }))
                return;
            // OS 模式: THROW 指令
            if (throwStmt.Value != null)
                GenerateExpression(throwStmt.Value);
            else
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.THROW, [new Operand(OperandType.REGISTER, 0)]));
        }
    }
}