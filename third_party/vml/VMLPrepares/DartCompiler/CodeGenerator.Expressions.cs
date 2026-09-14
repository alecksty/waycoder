using VMLAssembler;
using CompilerBase;

namespace DartCompiler;

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
            case AssignNode assign:
                GenerateExpression(assign.Value);
                StoreVar(assign.Name);
                break;
            case OpAssignNode opAssign:
                GenerateOpAssign(opAssign); // → CodeGenerator.Statements.cs
                break;
            case PrefixPostfixNode pp:
                GeneratePrefixPostfix(pp);
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    private void GenerateLiteral(LiteralNode node) => EmitLoadConstant(node.Value);

    private void GenerateVar(VarNode node)
    {
        LoadVar(node.Name);
    }

    private void GenerateBinary(BinaryNode node)
    {
        // Ternary: (cond ? then) : else
        if (node.Op == ":" && node.Left is BinaryNode inner && inner.Op == "?")
        {
            GenerateTernary(inner.Left, inner.Right, node.Right);
            return;
        }

        var left = WrapExpr(node.Left);
        var right = WrapExpr(node.Right);

        if (_expr!.EmitStandardBinaryOps(node.Op, left, right)) return;
        if (_expr!.EmitBitwiseOps(node.Op, left, right)) return;
        if (node.Op == "~/") { _expr!.EmitBinOp(left, right, "/"); return; } // Dart ~/ = 截断除法
        if (node.Op == "&&") { _expr!.EmitLogicalAnd(() => GenerateExpression(node.Left), () => GenerateExpression(node.Right)); return; }
        if (node.Op == "||") { _expr!.EmitLogicalOr(() => GenerateExpression(node.Left), () => GenerateExpression(node.Right)); return; }
        if (node.Op == "??") { _expr!.EmitBinOp(left, right, "+"); return; } // 简化: 返回左值
        throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown binary operator: {node.Op}");
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-": _expr!.EmitNeg(operand); break;
            case "!": _expr!.EmitNot(operand); break;
            case "~": _expr!.EmitNot(operand); break; // Dart ~ = 按位非
            default:
                GenerateExpression(node.Operand);
                break;
        }
    }

    private void GenerateCall(CallNode node)
    {
        // Handle Dart built-in type conversion methods: .toInt(), .toDouble()
        // 解析器将接收者作为第一个参数传入 (args[0] = receiver)
        if (node.Name == "_dot_toInt")
        {
            // .toInt() converts double→int: load receiver, emit D2I
            if (node.Arguments.Count > 0)
                GenerateExpression(node.Arguments[0]);
            instructions.Add(new Instruction(OpCode.D2I,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            return;
        }
        if (node.Name == "_dot_toDouble")
        {
            // .toDouble() converts int→double: load receiver, emit I2D
            if (node.Arguments.Count > 0)
                GenerateExpression(node.Arguments[0]);
            instructions.Add(new Instruction(OpCode.I2D,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            return;
        }

        // Inline print — generate SYSCALL directly
        if (node.Name == "print")
        {
            foreach (var arg in node.Arguments)
            {
                bool isString = arg is LiteralNode lit && lit.Value is string
                    || (arg is CallNode fc && IsStringReturningFunc(fc.Name));
                EmitPrintArg(() => GenerateExpression(arg), isString: isString, isFloat: arg is LiteralNode flit && (flit.Value is float || flit.Value is double));
            }
            return;
        }

        // float_to_str / floatToStr: Dart 的 number 是 double (D0), 但 conv 的 float 参数走 F0, 需 D2F 转换
        bool needsFloatConvert = node.Name.Equals("floatToStr", StringComparison.OrdinalIgnoreCase)
            || node.Name.Equals("float_to_str", StringComparison.OrdinalIgnoreCase)
            || node.Name.Equals("ftoa", StringComparison.OrdinalIgnoreCase);

        for (int i = node.Arguments.Count - 1; i >= 0; i--)
        {
            GenerateExpression(node.Arguments[i]);
            if (i == 0 && needsFloatConvert)
            {
                instructions.Add(new Instruction(OpCode.D2F,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            }
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        }

        // 解析调用目标：_dot_ 前缀表示成员访问调用，去掉前缀获取真实方法名
        string callName = node.Name;
        if (callName.StartsWith("_dot_"))
            callName = callName.Substring(5); // 去掉 "_dot_" 前缀

        string funcLabel = _externalMethods.Contains(callName)
            ? callName                             // external 方法: 裸名 CALL
            : $"func_{node.Name}";                 // 常规方法: func_ 前缀

        instructions.Add(new Instruction(OpCode.CALL,
            new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }, instructions.Count));

        for (int i = 0; i < node.Arguments.Count; i++)
        {
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
        }
    }

    private void GenerateTernary(ASTNode cond, ASTNode thenExpr, ASTNode elseExpr)
    {
        EmitTernary(
            () => GenerateExpression(cond),
            () => GenerateExpression(thenExpr),
            () => GenerateExpression(elseExpr));
    }

    private void GeneratePrefixPostfix(PrefixPostfixNode node)
    {
        if (node.Operand is VarNode vn)
        {
            LoadVar(vn.Name);
            int delta = node.Op == "++" ? 1 : -1;

            if (node.IsPrefix)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, delta) },
                    instructions.Count));
                StoreVar(vn.Name);
            }
            else
            {
                instructions.Add(new Instruction(OpCode.PUSH,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, delta) },
                    instructions.Count));
                StoreVar(vn.Name);
                instructions.Add(new Instruction(OpCode.POP,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            }
        }
        else
        {
            GenerateExpression(node.Operand);
        }
    }
}
