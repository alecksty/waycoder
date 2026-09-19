using VMLAssembler;
using CompilerBase;

namespace LuaCompiler
{
    public partial class CodeGenerator : TypedCodeGen<LuaType>
    {
        private void GenerateStatement(ASTNode node)
        {
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）：
            // 它是产物里 `; N: <原文>` 注释的来源，汇编器再读回 Instruction.SourceLine
            // ⇒ 链接期/语义期报错才给得出**行列号**（否则只能报个名字）。
            // 判据 `> 0`：行号是 1-based，Line 没填的节点是 0，置成 0 会把上一句的行号冲掉。
            if (node.Line > 0) CurrentSourceLine = node.Line;
            switch (node)
            {
                case VariableDeclarationNode varDecl:
                    GenerateVariableDeclaration(varDecl);
                    break;
                    
                case AssignmentNode assignment:
                    GenerateAssignment(assignment);
                    break;
                    
                case ConstantNode constant:
                    // 常量表达式，计算值但不存储
                    GenerateConstant(constant);
                    break;
                    
                case IdentifierNode identifier:
                    // 标识符引用，加载值到R0
                    GenerateIdentifier(identifier);
                    break;
                    
                case BinaryOperationNode binaryOp:
                    // 二元运算，计算值到R0
                    GenerateBinaryOperation(binaryOp);
                    break;
                    
                case UnaryOperationNode unaryOp:
                    // 一元运算，计算值到R0
                    GenerateUnaryOperation(unaryOp);
                    break;
                    
                case FunctionDefinitionNode funcDef:
                    GenerateFunctionDefinition(funcDef);
                    break;
                    
                case IfStatementNode ifStmt:
                    GenerateIfStatement(ifStmt);
                    break;
                    
                case WhileStatementNode whileStmt:
                    GenerateWhileStatement(whileStmt);
                    break;
                    
                case RepeatStatementNode repeatStmt:
                    GenerateRepeatStatement(repeatStmt);
                    break;
                    
                case ForStatementNode forStmt:
                    GenerateForStatement(forStmt);
                    break;
                case ForInStatementNode forInStmt:
                    GenerateForInStatement(forInStmt);
                    break;
                    
                case BreakStatementNode:
                    Sta!.EmitBreak();
                    break;

                case ReturnStatementNode returnStmt:
                    GenerateReturnStatement(returnStmt);
                    break;
                    
                case FunctionCallNode funcCall:
                    GenerateFunctionCall(funcCall);
                    break;
                    
                case TableConstructorNode table:
                    GenerateTableConstructor(table);
                    break;
                    
                case TableAccessNode tableAccess:
                    GenerateTableAccess(tableAccess);
                    break;

                case GotoStatementNode gotoStmt:
                    Sta!.EmitJump($"label_{gotoStmt.LabelName}");
                    break;

                case LabelStatementNode labelStmt:
                    AddLabel($"label_{labelStmt.LabelName}");
                    break;
                    
                default:
                    throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
            }
        }
        
        private void GenerateVariableDeclaration(VariableDeclarationNode node)
        {
            // 为变量分配栈空间
            foreach (var name in node.Names)
            {
                if (!symbolTable.ContainsKey(name))
                {
                    symbolTable[name] = nextStackOffset;
                    nextStackOffset += 4; // 每个变量4字节
                }
            }
            
            // 如果有初始值，生成赋值
            for (int i = 0; i < node.Values.Count; i++)
            {
                if (i < node.Names.Count)
                {
                    var name = node.Names[i];
                    var value = node.Values[i];
                    
                    if (value is FunctionExpressionNode functionExpression)
                    {
                        functionExpression.GeneratedName = name;
                    }

                    GenerateExpression(value);
                    
                    // 存储到变量位置
                    int offset = symbolTable[name];
                    AddInstruction(OpCode.MOVE, Mem($"R12-{offset}"), Reg(0));
                }
            }
        }
        
        private void GenerateAssignment(AssignmentNode node)
        {
            // 简化：只处理单个变量赋值
            if (node.Variables.Count == 1 && node.Values.Count == 1)
            {
                var variable = node.Variables[0];
                var value = node.Values[0];
                
                if (value is FunctionExpressionNode functionExpression && variable is IdentifierNode functionIdentifier)
                {
                    functionExpression.GeneratedName = functionIdentifier.Name;
                }

                if (variable is IdentifierNode identifier)
                {
                    // 生成值到R0
                    GenerateExpression(value);
                    
                    // 推断值的类型并更新变量类型
                    LuaType valueType = InferExpressionType(value);
                    varTypes[identifier.Name] = valueType;
                    
                    // 获取正确的存储指令
                    OpCode storeOp = GetStoreInstruction(valueType);
                    
                    // 检查变量是否已声明
                    if (symbolTable.ContainsKey(identifier.Name))
                    {
                        int offset = symbolTable[identifier.Name];
                        // 统一 store: dest=mem first (MOVE 系列 dest-first 顺序)
                        instructions.Add(new Instruction(storeOp,
                            new List<Operand> {
                                new Operand(OperandType.MEMORY, $"R12-{offset}"),
                                new Operand(OperandType.REGISTER, 0)
                            },
                            instructions.Count));
                    }
                    else
                    {
                        // 全局变量，使用数据段
                        string dataLabel = $"var_{identifier.Name}";
                        dataSection[dataLabel] = 0; // 初始值0

                        // 统一 store: dest=mem first (MOVE 系列 dest-first 顺序)
                        instructions.Add(new Instruction(storeOp,
                            new List<Operand> {
                                new Operand(OperandType.MEMORY, dataLabel),
                                new Operand(OperandType.REGISTER, 0)
                            },
                            instructions.Count));
                    }
                }
                else if (variable is TableAccessNode tableAccess)
                {
                    // 值 / 表指针**都存栈上**再求值 key：求值表达式会拿 R1/R2 当临时寄存器，
                    // `t[i + 1] = v` 这种写法下只把表指针放 R1 会被后面对 key 的求值冲掉。
                    GenerateExpression(value);
                    instructions.Add(new Instruction(OpCode.PUSH,
                        new List<Operand> { new Operand(OperandType.REGISTER, 0) },
                        instructions.Count));
                    GenerateExpression(tableAccess.Table);
                    instructions.Add(new Instruction(OpCode.PUSH,
                        new List<Operand> { new Operand(OperandType.REGISTER, 0) },
                        instructions.Count));
                    GenerateExpression(tableAccess.Key);
                    instructions.Add(new Instruction(OpCode.MOVE,
                        new List<Operand> {
                            new Operand(OperandType.REGISTER, 2),
                            new Operand(OperandType.REGISTER, 0)
                        },
                        instructions.Count));
                    instructions.Add(new Instruction(OpCode.POP,
                        new List<Operand> { new Operand(OperandType.REGISTER, 1) },
                        instructions.Count)); // R1 = 表指针
                    instructions.Add(new Instruction(OpCode.POP,
                        new List<Operand> { new Operand(OperandType.REGISTER, 0) },
                        instructions.Count)); // R0 = 值
                    instructions.Add(new Instruction(OpCode.CALL,
                        new List<Operand> { new Operand(OperandType.LABEL, "lua_table_set") },
                        instructions.Count));
                }
            }
        }
    }
}
