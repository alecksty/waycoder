using VMLAssembler;
using CompilerBase;

namespace LuaCompiler
{
    public partial class CodeGenerator : TypedCodeGen<LuaType>
    {
        private void GenerateExpression(ASTNode node)
        {
            switch (node)
            {
                case ConstantNode constant:
                    GenerateConstant(constant);
                    break;
                    
                case IdentifierNode identifier:
                    GenerateIdentifier(identifier);
                    break;
                    
                case BinaryOperationNode binaryOp:
                    GenerateBinaryOperation(binaryOp);
                    break;
                    
                case UnaryOperationNode unaryOp:
                    GenerateUnaryOperation(unaryOp);
                    break;
                    
                case FunctionCallNode functionCall:
                    GenerateFunctionCall(functionCall);
                    break;

                case FunctionExpressionNode functionExpression:
                    GenerateFunctionExpression(functionExpression);
                    break;
                    
                case TableConstructorNode table:
                    GenerateTableConstructor(table);
                    break;
                    
                case TableAccessNode tableAccess:
                    GenerateTableAccess(tableAccess);
                    break;
                    
                default:
                    throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
            }
        }
        
        private void GenerateConstant(ConstantNode node)
        {
            LuaType luaType = GetLuaTypeFromValue(node.Value, node.Type);
            OpCode moveOp = GetMoveInstruction(luaType);
            
            if (node.Value == null) // nil
            {
                AddRI(moveOp, 0, 0);
            }
            else if (node.Type == "number")
            {
                double value = (double)node.Value;
                // 整数档 → `MOVE R0, #imm`（**保持 4 字节槽 / 通用寄存器**，与 Lib 的
                // 调用约定一致）；浮点档 → `MOVED @D0, dbl_N`（双精度）。
                // 判据与 `GetLuaTypeFromValue` **同一条**（那是唯一一份）。
                if (value == Math.Truncate(value) && value >= int.MinValue && value <= int.MaxValue)
                {
                    instructions.Add(new Instruction(OpCode.MOVE,
                        new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)value) },
                        instructions.Count));
                }
                else
                {
                    EmitLoadConstant(value);
                }
            }
            else if (node.Type == "string")
            {
                string value = (string)node.Value;
                // 字符串去重: 相同内容的字符串复用同一个标签
                string strLabel = AddStringCached(value);
                
                instructions.Add(new Instruction(OpCode.MOVE, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.LABEL, strLabel)
                    }, 
                    instructions.Count));
            }
            else if (node.Type == "boolean")
            {
                bool value = (bool)node.Value;
                instructions.Add(new Instruction(moveOp, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, value ? "1" : "0")
                    }, 
                    instructions.Count));
            }
        }
        
        private void GenerateIdentifier(IdentifierNode node)
        {
            // 获取变量类型
            LuaType varType = LuaType.Number; // 默认数字类型
            if (varTypes.TryGetValue(node.Name, out LuaType type))
            {
                varType = type;
            }
            
            OpCode loadOp = GetLoadInstruction(varType);
            
            if (symbolTable.ContainsKey(node.Name))
            {
                int offset = symbolTable[node.Name];
                // 目的寄存器**按类取**（`MOVED` 的 0 号是 D0，写 `REGISTER 0` 会被类闸判死）
                instructions.Add(new Instruction(loadOp, 
                    new List<Operand> { 
                        TRegOf(loadOp, 0),
                        new Operand(OperandType.MEMORY, $"R12-{offset}")
                    }, 
                    instructions.Count));
            }
            else
            {
                // 全局变量，从数据段加载
                string dataLabel = $"var_{node.Name}";
                if (!dataSection.ContainsKey(dataLabel))
                {
                    dataSection[dataLabel] = 0; // 默认值0
                }
                
                instructions.Add(new Instruction(loadOp, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, dataLabel)
                    }, 
                    instructions.Count));
            }
        }
        
        private static ExpType LuaTypeToExpType(LuaType t) => t switch
        {
            LuaType.Boolean => ExpType.I8,
            LuaType.String or LuaType.Table or LuaType.Function => ExpType.Ptr32,
            // `Number`（整数档）留在 32 位、`Float` 走双精度 —— 见 `LuaType.Float` 的注释
            LuaType.Float => ExpType.F64,
            _ => ExpType.I32,
        };

        private ExpVar WrapExpr(ASTNode node)
        {
            var luaType = InferExpressionType(node);
            return ExpVar.Eval(LuaTypeToExpType(luaType), () => GenerateExpression(node));
        }

        private void GenerateUnaryOperation(UnaryOperationNode node)
        {
            switch (node.Operator)
            {
                case TokenType.NOT:
                    _expr!.EmitNot(WrapExpr(node.Operand));
                    break;
                case TokenType.MINUS:
                    _expr!.EmitNeg(WrapExpr(node.Operand));
                    break;
                case TokenType.LEN:
                    GenerateExpression(node.Operand);
                    instructions.Add(new Instruction(OpCode.CALL,
                        new List<Operand> { new Operand(OperandType.LABEL, "lua_len") },
                        instructions.Count));
                    break;
            }
        }
        
        private void GenerateBinaryOperation(BinaryOperationNode node)
        {
            // Special: POW (power) — CALL shared_ipow (__stdcall)
            if (node.Operator == TokenType.POW)
            {
                GenerateExpression(node.Right);  // R0 = exp
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateExpression(node.Left);   // R0 = base
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "ipow")]));
                return;
            }

            // Special: CONCAT — call lua_concat
            if (node.Operator == TokenType.CONCAT)
            {
                GenerateExpression(node.Left);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
                GenerateExpression(node.Right);
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)], instructions.Count));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "lua_concat")], instructions.Count));
                return;
            }

            var left = WrapExpr(node.Left);
            var right = WrapExpr(node.Right);

            switch (node.Operator)
            {
                case TokenType.PLUS:  _expr!.EmitBinOp(left, right, "+"); break;
                case TokenType.MINUS: _expr!.EmitBinOp(left, right, "-"); break;
                case TokenType.MUL:   _expr!.EmitBinOp(left, right, "*"); break;
                case TokenType.DIV:   _expr!.EmitBinOp(left, right, "/"); break;
                case TokenType.FLOOR_DIV: _expr!.EmitBinOp(left, right, "/"); break;
                case TokenType.MOD:   _expr!.EmitBinOp(left, right, "%"); break;
                case TokenType.AND:   _expr!.EmitAnd(left, right); break;
                case TokenType.OR:    _expr!.EmitOr(left, right); break;
                case TokenType.EQ: _expr!.EmitCmp(left, right, "=="); break;
                case TokenType.NE: _expr!.EmitCmp(left, right, "!="); break;
                case TokenType.LT: _expr!.EmitCmp(left, right, "<"); break;
                case TokenType.LE: _expr!.EmitCmp(left, right, "<="); break;
                case TokenType.GT: _expr!.EmitCmp(left, right, ">"); break;
                case TokenType.GE: _expr!.EmitCmp(left, right, ">="); break;
            }
        }
    }
}
