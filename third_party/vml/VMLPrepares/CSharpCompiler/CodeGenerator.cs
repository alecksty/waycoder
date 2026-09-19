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
            // ⚠ v0.96.189：括号不改变类型（缺这条会把 `(f)` 里的 float/double 判成 Int32）
            if (expr is ParenthesizedExpression parenType)
                return InferCSharpType(parenType.Expression);
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
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
            // 判据 `> 0`：行号是 1-based，没填的节点是 0，置 0 会把上一句的行号冲掉。
            if (statement.Line > 0) { CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column; }
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
                    {
                        // **字段（含 const/static）不是栈上的局部变量**，而是数据段里的静态存储：
                        // 登记进 dataSection 并把初值折进去。v0.96.185 前这两件事都没做 ——
                        // 字段既没进数据段、初值也不生效（`const int N = 5;` 读出来是 0 或那个标签的地址）。
                        if (member is VariableDeclStatement field)
                        {
                            RegisterStaticField(field);
                            // ⚠ v0.96.190：字段初值若是数组（`static int[] sx = new int[N];`），
                            //   **必须把"分配数组块 + 把块地址存进 var_sx"生成出来**。
                            //   原来这里一律 `continue` —— 数组字段于是从来没有分配过
                            //   （`RegisterStaticField` 拿 `FoldConst(数组字面量)` 折出 0），
                            //   之后每一次 `sx[k]` 都从**地址 0 附近**读写，拿到的是低内存里的垃圾。
                            //   表象极隐蔽：棋盘画得好好的（不碰数组），只有用数组的那部分
                            //   （蛇身/食物占位）默默画到别处去。
                            //   这段**要留到 `Main` 里发** —— 入口 `main` 就是 `Main` 的标签，
                            //   在它之前发的指令全是死代码（与 Swift 的 patch 0011 ① 是同一类坑）。
                            if (field.Initializer is ArrayLiteralExpression arrFieldInit)
                                _staticArrayFieldInits.Add((field.Name, arrFieldInit));
                            continue;
                        }
                        GenerateStatement(member);
                    }
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
                            // 全局/静态变量：从数据段**取值**（不是取地址）
                            // ⚠ v0.96.185 前这里是 `OperandType.LABEL` —— 而 LABEL 在 VM 里的语义是
                            //    "**标签的地址**"（C 那边正是用它来物化指针），于是 `const int N = 5;`
                            //    读出来是 var_N 的地址（几千），`while (i < N)` 就跑几千圈。
                            //    数据段里带标签名的 `MEMORY` 操作数才是"取那个位置的值"。
                            string label = $"var_{ve.Name}";
                            if (!dataSection.ContainsKey(label))
                                dataSection[label] = 0;
                            instructions.Add(new Instruction(OpCode.MOVE,
                                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, label)]));
                        }
                    }
                    else
                    {
                        GenerateExpression(addrExpr.Target);
                    }
                    break;

                case ParenthesizedExpression paren:
                    // ⚠ v0.96.189 新增：**括号表达式原先根本没有分支**，直接落进下面的
                    //    `default:` 被编成 `move R0 #0` —— **`(任意表达式)` 恒等于 0**。
                    //    解析器（`Parser.Expressions.cs`）明明建了 `ParenthesizedExpression`，
                    //    代码生成却没接 ⇒ 编译全绿、跑起来错，最难查的那一类。
                    //    Swift 前端实测症状：`A[16 + (k) * 2]` 恒等于 `A[16]`。
                    GenerateExpression(paren.Expression);
                    break;

                default:
                    // ⚠ 不能静默发 0：这个 `default` 正是把 `ParenthesizedExpression` 吞成
                    //    恒 0 常量的那个洞。**编不过最省事**。
                    throw new CodeGenerationException(
                        ErrorCode.CodeGen_UnsupportedExpression,
                        $"C# 前端不支持这种表达式（代码生成缺分支）：{expression.GetType().Name}");
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

            // 回退到数据段（全局/静态变量或参数）—— 取**值**，见上面那段说明
            string label = $"var_{variable.Name}";
            if (!dataSection.ContainsKey(label))
            {
                // 局部表与 `dataSection` 都没有 ⇒ 这个名字**从未声明过**。
                // 此前这里顺手建个初值 0 的槽就当成全局/静态 ——
                // `int a = 1; int b = a + nosuch;` 编得过、运行期静静按 0 算出个错答案。
                ReportUndefined(variable.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                EmitUndefinedFallback();
                return;
            }

            instructions.Add(new Instruction(loadOp, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, label)]));
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
            // ⚠ v0.96.190 修：`MOVE Rd, LABEL x` 是 **LEA（取标签地址）**，不是"取标签里的值"！
            //   这里要的是**刚才存进 arrLabel 的数组基址**，必须用 `MEMORY`（带标签名的
            //   MEMORY 操作数才是"取那个位置的值"）。用 LABEL 拿到的是 `&arrLabel` 本身，
            //   于是**写入的基址是 `&__asn_arr_N`、读取的基址是 `&__idx_arr_N`** —— 两者差
            //   了 8 字节（中间还夹着 `__asn_idx_N` / `__idx_arr_N` 两个槽）
            //   ⇒ **写进一个地方、又到另一个地方去读**，读回来永远是 0。
            //   这与 patch 0010 修的"全局变量被读成标签地址"是同一个病（本仓库第五次）。
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, arrLabel)]));
            // 加载索引 → R0
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, idxLabel)]));
            // ⚠ v0.96.190 修（三处操作数顺序）。VM 的语义是 **dest 在前**：
            //   · 3 操作数 `ADD Rd, Rs, Rt` → Rd = Rs + Rt
            //   · 2 操作数 `ADD Rd, Rs`      → Rd = Rd + Rs
            //   而 `ExecuteAdd` 只写 `if (dest.Type == OperandType.REGISTER)` —— **dest 是立即数时结果被丢弃**。
            //   原来这里写的是 `SHL [#2, R0]` / `ADD [#4, R0]` ⇒ **两条全是空操作**（偏移没算），
            //   接着 `ADD [R0, R1]` 又把和写进了 R0，而下一句读的是 `[R1]`（还是数组基址）
            //   ⇒ **读数组元素永远读到数组头**。改完与 `EmitArrayElementOffset`（基类，寄存器在前）
            //   和写入路径的 3 操作数形式一致。
            instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
            // R1 = R1 + R0 → points to element
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
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
                // ⚠ v0.96.190：同样是 LABEL(LEA) → MEMORY(取值) —— 用 LABEL 拿到的是
                //   `&__lenptr_N` 而不是数组基址，`.Length` 于是恒不等于真实长度。
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, ptrLabel)]));
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

                // ⚠ v0.96.190 修（与 `GenerateIndex` 同族，共四处操作数顺序）：
                //   ① 这句原来是 `MOVE R0, R2` —— 反了（要的是"把值存进 R2"）；
                //   ②③ `SHL [#2, R0]` / `ADD [#4, R0]` 是**空操作**（dest 不能是立即数）；
                //   ④ `ADD [R0, R1]` 把和写进了 R0，而下一句存的是 `[R1]`（数组基址）
                //      ⇒ **写数组元素永远写进数组头**，任意下标都落到同一个槽。
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, arrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, idxLabel)]));
                instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
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
                    // 同上：写进"那个位置"，不是写进"那个地址的编号"
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, label), new Operand(OperandType.REGISTER, 0)]));
                }
            }
        }
        
        /// <summary>
        /// 类字段（含 <c>const</c> / <c>static</c>）→ 数据段里的静态槽 + 折好的初值。
        ///
        /// 为什么单独一条路：<c>GenerateVariableDecl</c> 是给**方法内局部变量**用的
        /// （在栈帧上分配），类字段走那条路会既进不了数据段、初值也丢掉。
        /// 类型仍记进 <c>_variableTypes</c>，否则后面的类型推断取不到它。
        /// </summary>
        /// <summary>
        /// 数组类型的静态字段（`static int[] sx = new int[N];`）—— 收集起来、到 `Main` 里再发。
        /// 理由见 `ClassDeclaration` 分支里的注释：入口就是 `Main` 的标签，提前发等于死代码。
        /// </summary>
        private readonly List<(string Name, Expression Init)> _staticArrayFieldInits = new();

        private void RegisterStaticField(VariableDeclStatement field)
        {
            if (!_variableTypes.ContainsKey(field.Name))
                _variableTypes[field.Name] = field.Type;
            dataSection[$"var_{field.Name}"] = FoldConst(field.Initializer);
        }

        /// <summary>字面量（可带正负号）→ 整数；折不出来给 0。只服务字段初值，不做通用常量折叠。</summary>
        private static int FoldConst(Expression expr)
        {
            if (expr is LiteralExpression lit)
            {
                if (lit.Value is int i) return i;
                if (lit.Value is long l) return (int)l;
                if (lit.Value is bool b) return b ? 1 : 0;
            }
            if (expr is UnaryExpression u && (u.Operator == TokenType.Minus || u.Operator == TokenType.Plus))
            {
                int v = FoldConst(u.Operand);
                return u.Operator == TokenType.Minus ? -v : v;
            }
            return 0;
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
                // ⚠ v0.96.190 修：这里原本写的是 `MOVE [R12+12+4i], R0` —— 而 `MOVE dest, src`
                //    的 dest 在前，那是**把 R0 存进调用方的实参槽**，不是"把实参读进 R0"。
                //    两个操作数写反的后果：实参槽被踩坏，而局部变量拿到的是**调用那一刻
                //    R0 里恰好留着的东西**（= 调用点最后一个被求值的实参）。
                //    症状极具误导性：`a1(x)` 侥幸对（调用点最后一句就是 `move R0 #10`，正好是 x），
                //    `a2(x,y)` 的 y 变成 x ⇒ 两个矩形**完全重叠**（所以"看着只有一个"），
                //    `a3(x,y,c)` 的 c 变成 x ⇒ 颜色近黑、在黑底上看不见。
                //    实测判据：解 PNG 逐像素量四个矩形的 bbox，四个全对才算修好。
                //    —— 与 Swift 的 `GenerateIndexAccess`（patch 0011）同一族：**操作数写反**。
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, Vars.FormatOffset(12 + pi * 4))]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, Vars.FormatOffset(stackOff)), new Operand(OperandType.REGISTER, 0)]));
            }

            // 数组类型的静态字段在这里初始化（**必须在 Main 体内**，理由见
            // `ClassDeclaration` 分支：入口就是 Main 的标签，提前发等于死代码）。
            if (method.Name == "Main" && _staticArrayFieldInits.Count > 0)
            {
                foreach (var (fName, fInit) in _staticArrayFieldInits)
                {
                    GenerateExpression(fInit);                 // R0 = 数组块地址
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.MEMORY, $"var_{fName}"), new Operand(OperandType.REGISTER, 0)]));
                }
                _staticArrayFieldInits.Clear();
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
                // ⚠ **调用方清栈**（2026-09-17 调用约定统一后补的）。
                // 旧注释写「PrintlnStr/shared_println_str 内部已 ADD R13 #8 清理参数, 调用方不需额外清理」——
                // 那说的是 `Lib` 里那两个函数**还没重生成**时的形态：它们替调用方多弹掉一格。
                // 现在 `Lib/` 已按统一约定重生成（被调方一律裸 `ret`；`Lib/csharp/console.vml` 的
                // 包装器只清它自己压的那一格）⇒ 这一句是**净漏 4 字节/次**的泄漏。
                // 同一个 `PrintlnStr` 在 `GenerateFunctionCall` 的 stdlibLabel 分支里本来就由调用方清
                // （`ADD R13, count*4`），两条路此前一个清一个不清 —— 现在统一成调用方清。
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, 4)]));
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
        
        /// <summary>
        /// `new T[N]` 的长度：字面量，或**已登记的常量**（局部/字段/枚举）。
        /// 折不出来就抛 —— 运行期长度需要分配器与边界这套支持，本编译器没有；
        /// 与其静默给一个零长数组（"编得过、跑不对"，这一轮已经栽过三次），不如编不过。
        /// </summary>
        private int FoldArraySize(Expression sizeExpr)
        {
            if (sizeExpr is LiteralExpression lit && lit.Value is int n) return n;
            if (sizeExpr is UnaryExpression u && u.Operator == TokenType.Minus)
                return -FoldArraySize(u.Operand);
            if (sizeExpr is VariableExpression ve)
            {
                string label = $"var_{ve.Name}";
                if (dataSection.TryGetValue(label, out var v) && v is int iv) return iv;
            }
            throw new CodeGenerationException(ErrorCode.CodeGen_TypeMismatch,
                "数组长度必须是编译期常量（`new T[N]` 的 N 只能是字面量或 const）");
        }

        private void GenerateArrayLiteral(ArrayLiteralExpression arrayLiteral)
        {
            int arrId = labelCounter++;
            string ptrLabel = $"arr_ptr_{arrId}";
            string dataLabel = $"arr_data_{arrId}";
            dataSection[ptrLabel] = 0;

            // ⚠ v0.96.190 重写：**改成与 Swift 前端完全同构的布局** ——
            //   一个数据标签、内容是一个数组 `[count, e0, e1, …]`。
            //   此前这里是"给每个元素各声明一个 `.word` 标签"（`arr_data_N_0`、`arr_data_N_1`…），
            //   再靠 `MOVE R0, LABEL dataLabel` 拿基址、用 `base + i*4 + 4` 索引 ——
            //   那**依赖"这些独立标签在内存里恰好按声明顺序连续"这个从未验证过的假设**。
            //   Swift 那边之所以一直是对的，正是因为它是"一个标签装一整块"。
            int count = arrayLiteral.Elements.Count;
            // `new T[N]`：长度在 `SizeExpr` 里（常量**标识符**的写法必须在这里折 ——
            // 常量表就在本类，`RegisterStaticField` 已把 `const` 字段折进了 `dataSection`）。
            if (arrayLiteral.SizeExpr != null)
                count = FoldArraySize(arrayLiteral.SizeExpr);
            if (count < 0 || count > 65536)
                throw new CodeGenerationException(ErrorCode.CodeGen_TypeMismatch,
                    $"数组长度不合法: {count}（本编译器只支持编译期常量长度）");
            var arrayData = new object[1 + count];
            arrayData[0] = count;
            // 常量元素静态折进数据段；`new T[N]` 没有元素表达式（长度在 SizeExpr 里）⇒ 全 0
            int litCount = Math.Min(count, arrayLiteral.Elements.Count);
            for (int i = 0; i < litCount; i++)
            {
                arrayData[1 + i] = (arrayLiteral.Elements[i] is LiteralExpression lit && lit.Value is int iv)
                    ? iv : 0;
            }
            dataSection[dataLabel] = arrayData;

            // 非常量元素在运行时填进去：R1 = 基址 + (i+1)*4，再存 R0
            // ⚠ 上界必须是 `Elements.Count` 而**不是** `count` —— `new T[N]` 的 `Elements` 是空的
            //   （长度在 `SizeExpr` 里），拿 N 去索引会越界（实测：`Index was out of range`）。
            int fillCount = Math.Min(count, arrayLiteral.Elements.Count);
            for (int i = 0; i < fillCount; i++)
            {
                if (arrayLiteral.Elements[i] is LiteralExpression l2 && l2.Value is int) continue;
                GenerateExpression(arrayLiteral.Elements[i]);
                instructions.Add(new Instruction(OpCode.MOVE, [
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.LABEL, dataLabel)
                ]));
                // 2 操作数 `ADD Rd, imm` → Rd = Rd + imm（dest 在前）
                instructions.Add(new Instruction(OpCode.ADD, [
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.IMMEDIATE, (i + 1) * 4)
                ]));
                instructions.Add(new Instruction(OpCode.MOVE, [
                    new Operand(OperandType.MEMORY, "R1"),
                    new Operand(OperandType.REGISTER, 0)
                ]));
            }

            // R0 = 数组块地址（[0]=count，[1..N]=元素）—— 索引一律用 `base + i*4 + 4`
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, dataLabel)
            ]));
            // 留档一份（有些旧路径按 `arr_ptr_N` 找）
            instructions.Add(new Instruction(OpCode.MOVE, [
                new Operand(OperandType.LABEL, ptrLabel),
                new Operand(OperandType.REGISTER, 0)
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