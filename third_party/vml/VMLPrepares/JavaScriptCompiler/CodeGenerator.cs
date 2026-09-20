using VMLAssembler;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace JavaScriptCompiler
{
    /// <summary>JavaScript 类型枚举 — 用于类型感知指令选择</summary>
    public enum JSType { Number, String, Boolean, Object, Array, Function, Null, Undefined }

    /// <summary>
    /// JavaScript语言代码生成器
    /// </summary>
    public partial class CodeGenerator : TypedCodeGen<JSType>
    {
        private Dictionary<string, int> _localVarOffsets = new Dictionary<string, int>();
        private int _localVarSize = 0;
        private Dictionary<string, int> _classFields = new Dictionary<string, int>();
        private int _currentFieldOffset = 4;
        private Dictionary<string, string> _classParentMap = new Dictionary<string, string>();
        private Dictionary<string, Dictionary<string, int>> _classFieldMaps = new();
        private Dictionary<string, string> _varClassMap = new();
        private string _currentClassName = null;
        // 类型辅助方法
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(JSType t) => t switch
        {
            JSType.Number => (4, true, false, false), // JS Number = double-precision, but MCU uses float
            JSType.Boolean => (1, false, false, false),
            JSType.String or JSType.Object or JSType.Array or JSType.Function => (4, false, false, false),
            JSType.Null or JSType.Undefined => (4, false, false, false),
            _ => (4, false, false, false)
        };

        public CodeGenerator()
        {
            InitSimpleCompiler(framePointerReg: 14);  // JS uses R14 as frame pointer
        }

        private Program? _program;

        public override VmlProgram GenerateCode()
        {
            if (_program == null) throw new System.InvalidOperationException("尚未设置 AST 程序");
            return Generate(_program);
        }

        public VmlProgram Generate(Program program)
        {
            _program = program;
            // 先处理非函数定义的语句，记录程序入口
            int mainEntry = instructions.Count;
            
            // 第一遍：处理所有非函数定义语句
            foreach (var statement in program.Statements)
            {
                if (!(statement is FunctionDeclStatement))
                    GenerateStatement(statement);
            }
            
            // 全局代码结束后自动退出（防止执行流落入函数定义）
            if (dataSection.Keys.Any(k => k.StartsWith("var_")))
            {
                string firstVar = dataSection.Keys.First(k => k.StartsWith("var_"));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, firstVar)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
            }
            EmitExit();
            
            // 第二遍：处理所有函数定义
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclStatement funcDecl)
                    GenerateFunctionDecl(funcDecl);
            }
            
            // 如果没有main标签，添加默认的main
            if (!labels.ContainsKey("main"))
            {
                labels["main"] = mainEntry;
                
                // 在 mainEntry 处插入帧指针初始化 + 变量存储区（SP由运行时设置）
                // R1 用作变量存储基址，需要指向已分配的内存
                instructions.Insert(mainEntry, new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 13)]));
                instructions.Insert(mainEntry + 1, new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 1024)]));
                instructions.Insert(mainEntry + 2, new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)]));
                
                // 修正 mainEntry 之后的标签偏移
                int shift = 3;
                var labelKeys = new List<string>(labels.Keys);
                foreach (var k in labelKeys)
                    if (labels[k] >= mainEntry && k != "main")
                        labels[k] += shift;
            }
            
            var vmlProgram = BuildProgram("main");
            vmlProgram.StackTop = 1048572;
            return vmlProgram;
        }
        
        private void GenerateStatement(Statement statement)
        {
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
            // 判据 `> 0`：行号是 1-based，没填的节点是 0，置 0 会把上一句的行号冲掉。
            if (statement.Line > 0) { CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column; }
            switch (statement)
            {
                case ExpressionStatement exprStmt:
                    if (exprStmt.Expression != null)
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
                    
                case BreakStatement _:
                    Sta!.EmitBreak();
                    break;

                case ContinueStatement _:
                    Sta!.EmitContinue();
                    break;

                case DoWhileStatement doWhileStmt:
                    GenerateDoWhile(doWhileStmt);
                    break;

                case SwitchStatement switchStmt:
                    GenerateSwitch(switchStmt);
                    break;

                case ThrowStatement throwStmt:
                    if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                    {
                        // MCU模式: throw → 致命错误退出 (与其他编译器一致)
                        if (throwStmt.Value != null) GenerateExpression(throwStmt.Value);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -1)]));
                        instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 99)]));
                        break;
                    }
                    if (throwStmt.Value != null) GenerateExpression(throwStmt.Value);
                    instructions.Add(new Instruction(OpCode.THROW, [new Operand(OperandType.REGISTER, 0)]));
                    break;

                case ClassDeclStatement classDecl:
                    GenerateClassDecl(classDecl);
                    break;

                case ArrayDestructureStatement arrDest:
                    GenerateArrayDestructure(arrDest);
                    break;

                case ObjectDestructureStatement objDest:
                    GenerateObjectDestructure(objDest);
                    break;

                case TryStatement tryStmt:
                    if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                    {
                        VMLPlugins.WarningEmitter.Emit("javascript", "MCU模式: try/catch异常处理被忽略（不支持异常）");
                        GenerateStatement(tryStmt.Body);
                        break;
                    }
                    string handlerLabel = $"try_handler_{labelCounter++}";
                    string endTryLabel = $"try_end_{labelCounter++}";
                    instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, handlerLabel)]));
                    GenerateStatement(tryStmt.Body);
                    instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endTryLabel)]));
                    labels[handlerLabel] = instructions.Count;
                    foreach (var catchClause in tryStmt.Catches)
                    {
                        if (catchClause.VariableName != null)
                        {
                            string catchVarLabel = $"var_{catchClause.VariableName}";
                            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, catchVarLabel), new Operand(OperandType.REGISTER, 0)]));
                        }
                        GenerateStatement(catchClause.Body);
                    }
                    instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                    labels[endTryLabel] = instructions.Count;
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
                    
                case CallExpression call:
                    GenerateCall(call);
                    break;

                case ConditionalExpression condExpr:
                    GenerateConditional(condExpr);
                    break;

                case NewExpression newExpr:
                    GenerateNewExpression(newExpr);
                    break;

                case InstanceofExpression instExpr:
                    GenerateInstanceof(instExpr);
                    break;

                case InExpression inExpr:
                    GenerateInExpression(inExpr);
                    break;

                case SuperExpression superExpr:
                    GenerateSuperCall(superExpr);
                    break;

                case ArrowFunctionExpression arrowExpr:
                    {
                        string arrowLabel = $"arrow_{labelCounter++}";
                        string skipLabel = $"arrow_skip_{labelCounter++}";
                        // 将函数地址加载到 R0 并跳过函数体
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, arrowLabel)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, skipLabel)]));
                        AddLabel(arrowLabel);
                        // JS调用约定: PUSH R14; MOVE R14, R13 (R14 = local frame pointer)
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(14)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(14), Reg(13)]));
                        // 从栈帧读取参数: R14+8 = 第1个参数 (跳过 old_R14 + return_addr)
                        for (int i = arrowExpr.Parameters.Count - 1; i >= 0; i--)
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R14+{8 + i * 4}")]));
                            var apd = arrowExpr.Parameters[i];
                            if (apd.DefaultValue != null)
                            {
                                string skipDefaultLabel = $"arrow_skip_default_{labelCounter++}";
                                instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, skipDefaultLabel)]));
                                GenerateExpression(apd.DefaultValue);
                                labels[skipDefaultLabel] = instructions.Count;
                            }
                            // Store parameter to variable
                            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, $"var_{apd.Name}"), Reg(0)]));
                        }
                        GenerateStatement(arrowExpr.Body);
                        // JS调用约定尾声: MOVE R13, R14; POP R14; RET
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(13), Reg(14)]));
                        instructions.Add(new Instruction(OpCode.POP, [Reg(14)]));
                        instructions.Add(new Instruction(OpCode.RET, []));
                        AddLabel(skipLabel);
                    }
                    break;

                case FunctionExpression funcExpr:
                    // Create a unique label for the function
                    string funcLabel = $"func_expr_{labelCounter++}";
                    string feSkipLabel = $"fe_skip_{labelCounter++}";
                    // 将函数地址加载到 R0 并跳过函数体
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, funcLabel)]));
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, feSkipLabel)]));
                    AddLabel(funcLabel);
                    // JS调用约定: PUSH R14; MOVE R14, R13 (R14 = local frame pointer)
                    instructions.Add(new Instruction(OpCode.PUSH, [Reg(14)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(14), Reg(13)]));
                    // 从栈帧读取参数: R14+8 = 第1个参数
                    for (int i = funcExpr.Parameters.Count - 1; i >= 0; i--)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R14+{8 + i * 4}")]));
                        var pd = funcExpr.Parameters[i];
                        if (pd.DefaultValue != null)
                        {
                            string skipDefaultLabel = $"fe_skip_default_{labelCounter++}";
                            instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, skipDefaultLabel)]));
                            GenerateExpression(pd.DefaultValue);
                            labels[skipDefaultLabel] = instructions.Count;
                        }
                        // Store parameter to variable
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, $"var_{pd.Name}"), Reg(0)]));
                    }
                    // Generate body
                    GenerateBlock(funcExpr.Body);
                    // JS调用约定尾声: MOVE R13, R14; POP R14; RET
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
                    instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
                    AddLabel(feSkipLabel);
                    break;

                case ArrayLiteralExpression array:
                    GenerateArrayLiteral(array);
                    break;

                case ObjectLiteralExpression obj:
                    GenerateObjectLiteral(obj);
                    break;

                case IndexExpression indexExpr:
                    GenerateIndexRead(indexExpr);
                    break;

                case MemberExpression member:
                    GenerateMember(member);
                    break;

                case TemplateExpression template:
                    // Template literals: emit first part as a basic string
                    if (template.Parts.Count > 0)
                        GenerateExpression(template.Parts[0]);
                    else
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    break;

                case ParenthesizedExpression paren:
                    // ⚠ v0.96.189 新增：**括号表达式原先根本没有分支**，直接落进下面的
                    //    `default:` 被编成 `move R0 #0` —— **`(任意表达式)` 恒等于 0**。
                    //    解析器（`Parser.cs`）建了 `ParenthesizedExpression`、代码生成却没接。
                    GenerateExpression(paren.Expression);
                    break;

                default:
                    // ⚠ 不能静默发 0：这个 `default` 正是把 `ParenthesizedExpression` 吞成
                    //    恒 0 常量的那个洞。**编不过最省事**。
                    throw new CodeGenerationException(
                        ErrorCode.CodeGen_UnsupportedExpression,
                        $"JavaScript 前端不支持这种表达式（代码生成缺分支）：{expression.GetType().Name}");
            }
        }
        
        private void GenerateBlock(Block blockStmt)
        {
            foreach (var statement in blockStmt.Statements)
            {
                GenerateStatement(statement);
            }
        }

        private int GetFieldOffset(string fieldName)
        {
            if (!_classFields.TryGetValue(fieldName, out int offset))
            {
                offset = _currentFieldOffset;
                _classFields[fieldName] = offset;
                _currentFieldOffset += 4;
            }
            // Track per-class field
            if (_currentClassName != null && _classFieldMaps.TryGetValue(_currentClassName, out var classFields))
            {
                classFields[fieldName] = offset;
            }
            return offset;
        }

        /// <summary>
        /// Look up a field offset by walking the class hierarchy (child -> parent chain).
        /// Returns -1 if the field is not found in any class in the chain.
        /// </summary>
        private int GetClassFieldOffset(string className, string fieldName)
        {
            string current = className;
            while (current != null)
            {
                if (_classFieldMaps.TryGetValue(current, out var fields) && fields.TryGetValue(fieldName, out int offset))
                    return offset;
                _classParentMap.TryGetValue(current, out current);
            }
            return -1;
        }

        private string FindThisLabel()
        {
            foreach (var key in dataSection.Keys)
                if (key.StartsWith("this_"))
                    return key;
            string newLabel = $"this_{labelCounter++}";
            dataSection[newLabel] = 0;
            return newLabel;
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
            EmitExit();
        }
    }
}
