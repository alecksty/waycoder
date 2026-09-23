using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        /// <summary>uses crt 时 Write/WriteLn 走 TTY 通道</summary>
        internal bool UseCrtOutput;

        /// <summary>根据 CRT 模式选择 SYSCALL 编号</summary>
        private int OutputSyscall(int stdNum) => UseCrtOutput ? stdNum switch
        {
            1 => 401,  // OutputString → TTY_WriteString
            4 => 400,  // OutputChar   → TTY_WriteChar
            6 => 402,  // OutputInt    → TTY_PrintInt
            _ => stdNum
        } : stdNum;

        private void GenerateFileIoCall(ProcedureCallNode call)
        {
            string name = call.Name.ToLower();
            if (call.Arguments.Count == 0 || call.Arguments[0] is not VariableNode fileVar)
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"{call.Name} 的第一个参数必须是文件变量");

            if (name == "assign")
            {
                GenerateExpression(call.Arguments[1]);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
                GenerateVariableAddress(fileVar);
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                return;
            }

            if (name == "reset" || name == "rewrite" || name == "append")
            {
                int mode = name == "reset" ? 0 : (name == "rewrite" ? 1 : 2);
                GenerateVariableAddress(fileVar);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 8) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, mode) }));
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 110) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3) }));
                if (name == "append")
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 114) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 114) }));
                }
                return;
            }

            if (name == "close")
            {
                LoadFileHandle(fileVar);
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 111) }));
                return;
            }

            if (name == "write" || name == "writeln")
            {
                for (int i = 1; i < call.Arguments.Count; i++)
                {
                    WriteFileArgument(fileVar, call.Arguments[i]);
                }
                if (name == "writeln")
                    WriteFileChar(fileVar, 10);
                return;
            }

            if (name == "read" || name == "readln")
            {
                if (call.Arguments.Count > 1 && call.Arguments[1] is VariableNode target)
                {
                    LoadFileHandle(fileVar);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0) }));
                    GenerateVariableAddress(target);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, GetVariablePascalType(target.Name) == PascalType.Char ? 1 : 4) }));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 112) }));
                }
                return;
            }
        }

        private void LoadFileHandle(VariableNode fileVar)
        {
            GenerateVariableAddress(fileVar);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0") }));
        }

        private void WriteFileArgument(VariableNode fileVar, ExpressionNode arg)
        {
            GenerateExpression(arg);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
            LoadFileHandle(fileVar);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 4) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, EstimateWriteSize(arg)) }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 113) }));
        }

        private void WriteFileChar(VariableNode fileVar, int value)
        {
            string label = $"file_ch_{labelCounter++}";
            dataSection[label] = ((char)value).ToString();
            LoadFileHandle(fileVar);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, label) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 113) }));
        }

        private int EstimateWriteSize(ExpressionNode arg)
        {
            if (arg is LiteralNode literal)
            {
                if (literal.Type == TokenType.STRING_LITERAL) return literal.Value.ToString().Length;
                if (literal.Type == TokenType.CHAR_LITERAL) return 1;
            }
            if (arg is VariableNode variable && GetVariableType(variable.Name) == "STRING")
                return 255;
            return 4;
        }

        private void GenerateNewCall(ProcedureCallNode call)
        {
            if (call.Arguments.Count == 0 || call.Arguments[0] is not VariableNode ptr)
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, "New参数必须是指针变量");
            int bytes = GetPointerTargetSize(ptr.Name) * 4;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, bytes) }));
            EmitAlloc();
            AlignAllocatedPointer();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            GenerateVariableAddress(ptr);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
        }

        private void GenerateDisposeCall(ProcedureCallNode call)
        {
            if (call.Arguments.Count == 0)
                return;
            GenerateExpression(call.Arguments[0]);
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 41) }));
        }

        private void GenerateSetLengthCall(ProcedureCallNode call)
        {
            if (call.Arguments.Count < 2 || call.Arguments[0] is not VariableNode arrayVar)
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, "SetLength参数必须是动态数组变量和长度");
            GenerateExpression(call.Arguments[1]);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) }));
            EmitAlloc();
            AlignAllocatedPointer();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            GenerateVariableAddress(arrayVar);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
        }
        
        private void GenerateUserDefinedProcedureCall(ProcedureCallNode call)
        {
            // 需要知道被调用过程的参数声明来判断哪些是var参数
            // 查找对应的过程声明：先找**本文件**的，再找 `uses` 单元的 interface 声明。
            //
            // ⚠ 第二处查找不是锦上添花，而是**库函数的地址传递**能不能成立的全部：
            //   `DrawPoly(4, Poly)` / `GetImage(l,t,r,b,Buf)` / `PutImage(l,t,Buf,0)` 在 BGI 里
            //   收的是**数组地址**（BGI 的形参是 untyped `var`），而"压地址还是压值"这个判断
            //   只能来自形参声明。单元声明看不见的话，`Poly` 会被当成一个普通变量**压它的值**
            //   ⇒ 库那边拿一个随机地址去读，程序照跑、画面全错、一个错都不报。
            SubprogramDeclarationNode? targetSubprogram = FindSubprogram(call.Name);
            if (targetSubprogram == null
                && UnitSubprograms.TryGetValue(call.Name.ToLower(), out var unitSub))
                targetSubprogram = unitSub;

            // 参数传递: 从右到左压栈
            for (int i = call.Arguments.Count - 1; i >= 0; i--)
            {
                // 检查是否是var参数
                bool isVarParam = false;
                if (targetSubprogram != null && i < targetSubprogram.Parameters.Count)
                {
                    isVarParam = targetSubprogram.Parameters[i].IsVarParameter;
                }

                if (isVarParam && call.Arguments[i] is VariableNode varArg)
                {
                    // var参数: 压入变量地址
                    GenerateVariableAddress(varArg);
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else
                {
                    // 值参数: 压入值
                    GenerateExpression(call.Arguments[i]);
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }

            // CALL过程
            //
            // 标签用**声明里的那个大小写**：Pascal 不区分大小写（`Initgraph`/`INITGRAPH`
            // 都合法），而 VML 的标签表**区分** ⇒ 照源码原样发出去的话，
            // 写法与库里差一个字母就是"未解析标签"（致命），而这对用户毫无提示价值。
            string callLabel = UnitSubprograms.TryGetValue(call.Name.ToLower(), out var decl)
                ? decl.Name : call.Name;
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
            {
                new Operand(OperandType.LABEL, callLabel)
            }));
            
            // 清理参数栈(调用者清理)
            if (call.Arguments.Count > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 13), // SP
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)
                }));
            }
        }
        
        private SubprogramDeclarationNode? FindSubprogram(string name)
        {
            if (astNode is ProgramNode prog)
            {
                foreach (var sub in prog.Subprograms)
                {
                    if (sub.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return sub;
                }
            }
            return null;
        }

        private void GenerateWriteArgument(ExpressionNode arg)
        {
            string exprType = GetExpressionPascalType(arg);

            if (arg is LiteralNode literal && literal.Type == TokenType.STRING_LITERAL)
            {
                string str = literal.Value.ToString();
                foreach (char c in str)
                {
                    AddInstruction(OpCode.MOVE, Reg(0), Imm((int)c));
                    AddInstruction(OpCode.SYSCALL, Imm(OutputSyscall(4)));
                }
            }
            else if (exprType == "CHAR")
            {
                if (arg is LiteralNode cl)
                    AddInstruction(OpCode.MOVE, Reg(0), Imm((int)(char)cl.Value));
                else
                    GenerateExpression(arg);
                AddInstruction(OpCode.SYSCALL, Imm(OutputSyscall(4)));
            }
            else if (exprType == "REAL")
            {
                GenerateExpression(arg);
                AddInstruction(OpCode.SYSCALL, Imm(8));
            }
            else if (exprType == "STRING")
            {
                // 字符串变量：加载字符串指针值（不是地址的地址）
                GenerateExpression(arg);
                AddInstruction(OpCode.SYSCALL, Imm(OutputSyscall(1)));
            }
            else
            {
                GenerateExpression(arg);
                AddInstruction(OpCode.SYSCALL, Imm(OutputSyscall(6)));
            }
        }

        private string GetExpressionPascalType(ExpressionNode expr)
        {
            if (expr is LiteralNode literal)
            {
                return literal.Type switch
                {
                    TokenType.INTEGER_LITERAL => "INTEGER",
                    TokenType.REAL_LITERAL => "REAL",
                    TokenType.CHAR_LITERAL => "CHAR",
                    TokenType.STRING_LITERAL => "STRING",
                    TokenType.BOOLEAN => "BOOLEAN",
                    _ => "INTEGER"
                };
            }
            if (expr is VariableNode varNode)
            {
                if (varNode.Field != null)
                {
                    var (_, fieldType) = ResolveFieldChain(varNode.Name, varNode.Field, varNode.Fields);
                    return ResolveTypeName(fieldType);
                }
                return GetVariableType(varNode.Name);
            }
            if (expr is BinaryOpNode binaryOp)
            {
                if (binaryOp.Operator == TokenType.SLASH) return "REAL";
                if (IsFloatExpression(binaryOp)) return "REAL";
                return "INTEGER";
            }
            if (expr is FunctionCallNode funcCall)
            {
                if (astNode is ProgramNode program)
                    foreach (var sub in program.Subprograms)
                        if (sub is FunctionDeclarationNode func && func.Name == funcCall.Name)
                            return GetTypeName(func.ReturnType);
                if (ExternalFuncTypes.TryGetValue(funcCall.Name.ToLower(), out var et))
                    return et;
                return "INTEGER";
            }
            if (expr is UnaryOpNode unaryOp)
                return IsFloatExpression(unaryOp) ? "REAL" : "INTEGER";
            return "INTEGER";
        }

        /// <summary>解析类型别名到基础类型 (MyStr → STRING)</summary>
        private string ResolveTypeName(string typeName)
        {
            string upper = typeName.ToUpper();
            if (definedTypeAliases.TryGetValue(upper, out var resolved))
            {
                if (resolved is SimpleTypeNode simple)
                    return simple.TypeName.ToUpper();
            }
            return upper;
        }

        private string GenerateLabel()
        {
            // `L_` 前缀不能省：`L0`–`L7` 会被汇编器当成**长整数寄存器**，跳转静默失效（v0.96.192）
            string label = $"L_{labelCounter}";
            labelCounter++;
            return label;
        }
        
        private void GenerateRuntimeError(string message)
        {
            // 输出错误信息（简化实现）
            // 在实际实现中，应该调用系统调用或设置错误标志
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, -1) // 错误代码
            }));
            EmitExit();
        }

        private new void AddLabel(string label)
        {
            labels[label] = instructions.Count;
            // 注意：我们不添加LABEL指令，标签信息已经保存在labels字典中
            // VmlProgram.ToString()方法会根据labels字典在适当位置插入标签
        }

        private string GetTypeName(TypeNode typeNode)
        {
            if (typeNode is SimpleTypeNode simple)
                return simple.TypeName.ToUpper();
            if (typeNode is ArrayTypeNode array)
                return "ARRAY";
            if (typeNode is PointerTypeNode ptr)
                return "^" + GetTypeName(ptr.TargetType);
            if (typeNode is ProcedureTypeNode)
                return "PROCEDURE";
            if (typeNode is SubrangeTypeNode)
                return "INTEGER";
            if (typeNode is RecordTypeNode)
                return "RECORD";
            if (typeNode is SetTypeNode)
                return "SET";
            if (typeNode is FileTypeNode)
                return "FILE";
            return "INTEGER";
        }

        private bool IsFloatExpression(ExpressionNode expr)
        {
            if (expr is LiteralNode literal)
                return literal.Type == TokenType.REAL_LITERAL;
            if (expr is VariableNode variable)
                return IsFloatVariable(variable.Name);
            if (expr is BinaryOpNode binaryOp)
            {
                // / 运算符在Pascal中总是产生浮点结果
                if (binaryOp.Operator == TokenType.SLASH)
                    return true;
                return IsFloatExpression(binaryOp.Left) || IsFloatExpression(binaryOp.Right);
            }
            if (expr is FunctionCallNode funcCall)
            {
                // 检查函数返回类型
                if (astNode is ProgramNode program)
                {
                foreach (var sub in program.Subprograms)
                {
                    if (sub is FunctionDeclarationNode func && func.Name == funcCall.Name)
                    {
                        return GetTypeName(func.ReturnType) == "REAL";
                    }
                }
                }
            }
            return false;
        }

        private bool IsFloatVariable(string name)
        {
            if (localVarTypes.ContainsKey(name))
                return localVarTypes[name] == "REAL";
            if (globalVarTypes.ContainsKey(name))
                return globalVarTypes[name] == "REAL";
            return false;
        }

        private string GetVariableType(string name)
        {
            if (localVarTypes.ContainsKey(name))
                return localVarTypes[name];
            if (globalVarTypes.ContainsKey(name))
                return globalVarTypes[name];
            return "INTEGER"; // 默认类型
        }

        /// <summary>
        /// 将类型名称字符串转换为PascalType枚举
        /// </summary>
        private PascalType GetPascalType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return PascalType.Integer;
                
            string upperType = typeName.ToUpper();
            
            // Strip pointer prefix: ^INTEGER → INTEGER
            if (upperType.StartsWith("^"))
                return GetPascalType(upperType.Substring(1));
            
            switch (upperType)
            {
                case "INTEGER":
                case "INT":
                    return PascalType.Integer;
                case "REAL":
                case "FLOAT":
                case "SINGLE":
                    return PascalType.Real;
                case "CHAR":
                    return PascalType.Char;
                case "BOOLEAN":
                case "BOOL":
                    return PascalType.Boolean;
                case "STRING":
                    return PascalType.String;
                case "ARRAY":
                    return PascalType.Array;
                case "RECORD":
                    return PascalType.Record;
                case "SET":
                    return PascalType.Set;
                case "FILE":
                case "TEXT":
                    return PascalType.File;
                case "POINTER":
                    return PascalType.Pointer;
                default:
                    // 检查是否包含特定关键字
                    if (upperType.Contains("ARRAY"))
                        return PascalType.Array;
                    if (upperType.Contains("RECORD"))
                        return PascalType.Record;
                    if (upperType.Contains("SET"))
                        return PascalType.Set;
                    if (upperType.Contains("FILE"))
                        return PascalType.File;
                    return PascalType.Integer; // 默认整数类型
            }
        }

        /// <summary>
        /// 获取变量的Pascal类型
        /// </summary>
        private PascalType GetVariablePascalType(string name)
        {
            string typeName = GetVariableType(name);
            return GetPascalType(typeName);
        }

        /// <summary>
        /// For pointer variables declared as "^Type", determine the pointed-to PascalType.
        /// </summary>
        private PascalType GetPointedPascalType(string ptrVarName)
        {
            string fullType = GetVariableType(ptrVarName).ToUpper();
            if (fullType.StartsWith("^"))
                return GetPascalType(fullType.Substring(1));
            // For general pointer type, check if the var is a pointer and return its target if known
            if (GetPascalType(fullType) == PascalType.Pointer)
                return PascalType.Integer;
            return PascalType.Integer;
        }

        /// <summary>
        /// 将 PascalType 映射为 (byteSize, isFloat, isDouble) 三元组，供 ExpressionManager 使用
        /// </summary>
        private static (int byteSize, bool isFloat, bool isDouble, bool isLong) TypeInfo(PascalType type)
        {
            switch (type)
            {
                case PascalType.Real:
                    return (4, true, false, false);
                case PascalType.Char:
                    return (1, false, false, false);
                case PascalType.Boolean:
                    return (1, false, false, false);
                default:
                    return (4, false, false, false); // Integer, String, Array, Record, Set, File, Pointer
            }
        }

        /// <summary>
        /// 根据数据类型获取加载指令
        /// </summary>
        private new OpCode GetLoadInstruction(PascalType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectLoadOp(s, f, d, l);
        }

        /// <summary>
        /// 根据数据类型获取存储指令
        /// </summary>
        private new OpCode GetStoreInstruction(PascalType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectStoreUnifiedOp(s, f, d, l);
        }

        /// <summary>
        /// 根据数据类型获取移动指令
        /// </summary>
        private new OpCode GetMoveInstruction(PascalType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectMoveOp(s, f, d, l);
        }

        /// <summary>
        /// 根据数据类型获取压栈指令
        /// </summary>
        private new OpCode GetPushInstruction(PascalType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectPushOp(s, f, d, l);
        }

        /// <summary>
        /// 根据数据类型获取弹栈指令
        /// </summary>
        private new OpCode GetPopInstruction(PascalType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectPopOp(s, f, d, l);
        }

        /// <summary>
        /// 根据数据类型和运算符获取算术运算指令
        /// </summary>
        private new OpCode GetArithmeticInstruction(string op, PascalType type)
        {
            var (_, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectArithmeticOp(op, f, d, l);
        }

        /// <summary>
        /// 根据数据类型获取比较指令
        /// </summary>
        private new OpCode GetCompareInstruction(PascalType type)
        {
            var (_, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectCompareOp(f, d, l);
        }

        private bool IsSetVariable(string name)
        {
            string typeName = null;
            if (localVarTypes.ContainsKey(name))
                typeName = localVarTypes[name];
            else if (globalVarTypes.ContainsKey(name))
                typeName = globalVarTypes[name];
            
            if (typeName == null)
                return false;
            
            // 检查类型名是否为"SET"或者类型定义是集合类型
            if (typeName == "SET")
                return true;
            
            // 检查是否是类型别名（如SmallSet = set of 0..31）
            // 这里需要检查类型定义，但简化处理：如果类型名包含"SET"就认为是集合类型
            if (typeName.Contains("SET", StringComparison.OrdinalIgnoreCase))
                return true;
            
            return false;
        }

        private int GetPointerTargetSize(string name)
        {
            TypeNode typeNode = null;
            if (localVarDeclarations.ContainsKey(name)) typeNode = localVarDeclarations[name];
            else if (globalVarDeclarations.ContainsKey(name)) typeNode = globalVarDeclarations[name];
            if (typeNode is PointerTypeNode pointerType)
                return Math.Max(1, GetVariableSlots(pointerType.TargetType));
            return 1;
        }

        private void AlignAllocatedPointer()
        {
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 3)
            }));
            instructions.Add(new Instruction(OpCode.AND, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, -4)
            }));
        }

        private int GetVariableSlotsForVariable(string name)
        {
            // 获取变量的类型信息
            TypeNode typeNode = null;
            if (localVarDeclarations.ContainsKey(name))
                typeNode = localVarDeclarations[name];
            else if (globalVarDeclarations.ContainsKey(name))
                typeNode = globalVarDeclarations[name];
            
            if (typeNode != null)
                return GetVariableSlots(typeNode);
            
            return 1; // 默认返回1个槽位
        }

        private bool IsSetExpression(ExpressionNode expr)
        {
            if (expr is VariableNode varNode)
                return IsSetVariable(varNode.Name);
            if (expr is SetExpressionNode)
                return true;

            // 其他表达式类型暂时不支持集合
            return false;
        }

        private bool IsSetTypeExpression(ExpressionNode expr)
        {
            // 检查表达式是否为集合类型
            // 对于IN运算符，右操作数应该是集合
            return IsSetExpression(expr);
        }

        private void GenerateSetOperation(BinaryOpNode binaryOp, OpCode op, bool negateSecond = false)
        {
            // 假设两个操作数都是集合变量
            // 获取左操作数地址到R2
            GenerateExpression(binaryOp.Left);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 获取右操作数地址到R3
            GenerateExpression(binaryOp.Right);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 获取集合大小（假设两个集合大小相同）
            int slots = 1; // 默认大小
            if (binaryOp.Left is VariableNode leftVar)
                slots = GetVariableSlotsForVariable(leftVar.Name);
            
            // 为结果分配临时空间（在栈上）
            int tempOffset = currentLocalSize;
            currentLocalSize += slots;
            
            // 计算临时空间的地址到R4
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.REGISTER, 12)
            }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.IMMEDIATE, tempOffset * 4)
            }));
            
            // 对每个槽位执行运算
            for (int i = 0; i < slots; i++)
            {
                // 加载左操作数的第i个槽位到R0
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.IMMEDIATE, i * 4)
                }));
                
                // 加载右操作数的第i个槽位到R1
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 3),
                    new Operand(OperandType.IMMEDIATE, i * 4)
                }));
                
                if (negateSecond)
                {
                    // 对右操作数取反
                    instructions.Add(new Instruction(OpCode.NOT, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 1)
                    }));
                }
                
                // 执行位运算
                instructions.Add(new Instruction(op, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 1)
                }));
                
                // 存储结果到临时空间
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 4),
                    new Operand(OperandType.IMMEDIATE, i * 4)
                }));
            }
            
            // 返回临时空间的地址到R0
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 4)
            }));
        }

        private void GenerateInOperation(BinaryOpNode binaryOp)
        {
            // IN 运算：element IN set
            // 左操作数是元素，右操作数是集合
            
            // 生成元素值到R0
            GenerateExpression(binaryOp.Left);
            // 保存元素值到R1
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 生成集合地址到R2
            GenerateExpression(binaryOp.Right);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 获取集合的下界（假设集合类型是 set of 0..31）
            int lowerBound = 0; // 默认下界
            // 默认上界为 31
            
            // 计算元素在位图中的位置
            // 元素索引 = 元素值 - 下界
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.IMMEDIATE, lowerBound)
            }));
            
            // 计算字节偏移和位偏移
            // 字节偏移 = 元素索引 / 32 * 4
            // 位偏移 = 元素索引 % 32
            
            // 保存元素索引到R3
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 计算字节偏移：R0 = (R3 / 32) * 4
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 32)
            }));
            instructions.Add(new Instruction(OpCode.DIV, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 0)
            }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 4)
            }));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 3),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 计算位偏移：R4 = R1 % 32
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 32)
            }));
            instructions.Add(new Instruction(OpCode.MOD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.REGISTER, 0)
            }));
            
            // 加载集合的相应槽位到R0
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 2),
                new Operand(OperandType.REGISTER, 3)
            }));
            
            // 创建位掩码：R5 = 1 << 位偏移
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 5),
                new Operand(OperandType.IMMEDIATE, 1)
            }));
            // 循环移位
            string shiftLoop = $"shift_loop_{labelCounter++}";
            string shiftEnd = $"shift_end_{labelCounter++}";
            
            AddLabel(shiftLoop);
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.IMMEDIATE, 0)
            }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand>
            {
                new Operand(OperandType.LABEL, shiftEnd)
            }));
            instructions.Add(new Instruction(OpCode.SHL, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 5),
                new Operand(OperandType.REGISTER, 5),
                new Operand(OperandType.IMMEDIATE, 1)
            }));
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.REGISTER, 4),
                new Operand(OperandType.IMMEDIATE, 1)
            }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
            {
                new Operand(OperandType.LABEL, shiftLoop)
            }));
            AddLabel(shiftEnd);
            
            // 测试位：R0 = (集合槽位 & 位掩码) != 0
            instructions.Add(new Instruction(OpCode.AND, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 5)
            }));
            
            // 转换为布尔值：如果R0 != 0则R0 = 1，否则R0 = 0
            EmitCompareToBool(() => {}, OpCode.JNE);
        }

        /// <summary>
        /// 生成带类型转换的表达式 - 支持类型敏感的指令选择
        /// </summary>
        private void GenerateExpressionWithType(ExpressionNode expr, int targetReg, PascalType resultType)
        {
            // 生成表达式到R0
            GenerateExpression(expr);
            
            // 检查是否需要类型转换
            bool exprIsFloat = IsFloatExpression(expr);
            bool needsConversion = (resultType == PascalType.Real && !exprIsFloat) ||
                                   (resultType != PascalType.Real && exprIsFloat);
            
            if (needsConversion)
            {
                if (resultType == PascalType.Real)
                {
                    // 整数转浮点
                    instructions.Add(new Instruction(OpCode.I2F, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else
                {
                    // 浮点转整数（截断）
                    instructions.Add(new Instruction(OpCode.F2I, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }
            
            // 移动到目标寄存器
            if (targetReg != 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, targetReg),
                    new Operand(OperandType.REGISTER, 0)
                }));
            }
        }

        private void Error(string message)
        {
            throw new CompilationException(ErrorCode.Compilation_InternalError, $"代码生成错误: {message}");
        }
    }
}
