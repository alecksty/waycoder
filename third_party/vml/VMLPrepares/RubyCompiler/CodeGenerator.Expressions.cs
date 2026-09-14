using VMLAssembler;
using CompilerBase;

namespace RubyCompiler;

public partial class CodeGenerator
{
    private void GenerateExpression(ASTNode node)
    {
        switch (node)
        {
            case LiteralNode literal:
                GenerateLiteral(literal);
                break;
            case VarNode varNode:
                GenerateVar(varNode);
                break;
            case BinaryNode binary:
                GenerateBinary(binary);
                break;
            case UnaryNode unary:
                GenerateUnary(unary);
                break;
            case CallNode call:
                GenerateCall(call);
                break;
            case ArrayNode array:
                GenerateArray(array);
                break;
            case TernaryNode tern:
                GenerateTernary(tern);
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    private void GenerateLiteral(LiteralNode node)
    {
        EmitLoadConstant(node.Value); // 统一字面量加载 (修复 float→MOVEF 而非 MOVE #float)
    }

    private void GenerateVar(VarNode node)
    {
        if (node.Name == "self")
        {
            // self is the receiver stored at R12-0
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R12-0") },
                instructions.Count));
            return;
        }

        if (symbolTable.TryGetValue(node.Name, out int offset))
        {
            string memRef = MemOff(offset);
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, memRef) },
                instructions.Count));
        }
        else
        {
            // global variable
            string dataLabel = $"var_{node.Name}";
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;

            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                instructions.Count));
        }
    }

    private void GenerateBinary(BinaryNode node)
    {
        var left = WrapExpr(node.Left);
        var right = WrapExpr(node.Right);

        switch (node.Op)
        {
            case "+": _expr!.EmitBinOp(left, right, "+"); break;
            case "-": _expr!.EmitBinOp(left, right, "-"); break;
            case "*": _expr!.EmitBinOp(left, right, "*"); break;
            case "/": _expr!.EmitBinOp(left, right, "/"); break;
            case "%": _expr!.EmitBinOp(left, right, "%"); break;
            case "**":
                // CALL shared_ipow (__stdcall: 被调用者清栈)
                GenerateExpression(node.Right);  // R0 = exp
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(node.Left);   // R0 = base
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "ipow")]));
                break;
            case "==": _expr!.EmitCmp(left, right, "=="); break;
            case "!=": _expr!.EmitCmp(left, right, "!="); break;
            case "<": _expr!.EmitCmp(left, right, "<"); break;
            case ">": _expr!.EmitCmp(left, right, ">"); break;
            case "<=": _expr!.EmitCmp(left, right, "<="); break;
            case ">=": _expr!.EmitCmp(left, right, ">="); break;
            case "<=>":
                _expr!.EmitCmp(left, right, "<=>");
                break;
            case "and":
                // logical AND — evaluate both operands (0/1), bitwise AND works
                GenerateExpression(node.Left);
                instructions.Add(new Instruction(OpCode.PUSH,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                GenerateExpression(node.Right);
                instructions.Add(new Instruction(OpCode.POP,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.AND,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                return;
            case "or":
                // logical OR — evaluate both operands (0/1), bitwise OR works
                GenerateExpression(node.Left);
                instructions.Add(new Instruction(OpCode.PUSH,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                GenerateExpression(node.Right);
                instructions.Add(new Instruction(OpCode.POP,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.OR,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                return;
            default:
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown binary operator: {node.Op}");
        }
    }

    private void GenerateTernary(TernaryNode node)
    {
        EmitTernary(
            () => GenerateExpression(node.Condition),
            () => GenerateExpression(node.TrueExpr),
            () => GenerateExpression(node.FalseExpr),
            OpCode.JZ);
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-": _expr!.EmitNeg(operand); break;
            case "!": _expr!.EmitNot(operand); break;
            default:
                GenerateExpression(node.Operand);
                break;
        }
    }

    private void GenerateCall(CallNode node)
    {
        // print / puts → SYSCALL output
        if (node.Method == "print" || node.Method == "puts")
        {
            foreach (var arg in node.Arguments)
            {
                bool isString = arg is LiteralNode lit && lit.Value is string
                    || (arg is CallNode call && IsStringReturningFunc(call.Method));
                EmitPrintArg(() => GenerateExpression(arg), isString: isString, isFloat: arg is LiteralNode flit && (flit.Value is float || flit.Value is double));
            }
            if (node.Method == "puts")
                EmitPrintNewline();
            return;
        }

        // push arguments
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            GenerateExpression(node.Arguments[i]);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        }

        // push receiver if present (for method calls)
        string funcLabel;
        if (_nativeMethods.Contains(node.Method))
        {
            // native 方法: 使用裸名 CALL，无 func_ 前缀
            funcLabel = node.Method;
        }
        else if (node.Receiver != null)
        {
            GenerateExpression(node.Receiver);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            // receiver-based call: lookup method on receiver
            funcLabel = $"func_{node.Method}";
        }
        else
        {
            funcLabel = $"func_{node.Method}";
        }

        instructions.Add(new Instruction(OpCode.CALL,
            new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }, instructions.Count));

        // Clean up arguments from stack after CALL (caller-cleanup convention)
        int totalArgs = node.Arguments.Count + (node.Receiver != null ? 1 : 0);
        if (totalArgs > 0)
        {
            instructions.Add(new Instruction(OpCode.ADD,
                new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgs * 4) }, instructions.Count));
        }
    }

    private void GenerateArray(ArrayNode node)
    {
        // allocate array: size * 4 bytes
        int size = node.Elements.Count;
        AddRI(OpCode.MOVE, 0, size * 4);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 2) }, instructions.Count)); // malloc

        // store elements
        for (int i = 0; i < node.Elements.Count; i++)
        {
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            GenerateExpression(node.Elements[i]);
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Mem("R1"), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
            if (i < node.Elements.Count - 1)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) },
                    instructions.Count));
            }
        }
    }
}
