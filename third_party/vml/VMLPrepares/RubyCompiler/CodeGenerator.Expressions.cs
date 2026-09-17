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
            case IndexNode index:
                GenerateIndexRead(index);
                break;
            case IndexAssignNode indexAssign:
                GenerateIndexAssign(indexAssign);
                break;
            case IndexOpAssignNode indexOpAssign:
                GenerateIndexOpAssign(indexOpAssign);
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
        // 数组字面量。布局 = **无数组头**的扁平块：元素 i 在 `base + i*4`
        // （读/写两侧同式，和 R 前端一种路数；`[count, e0, …]` 那种带头布局是另一族）。
        int count = node.Elements.Count;

        // ⚠ 原来这里写的是 `SYSCALL #2` —— 而 **#2 是 InputString（从 stdin 读一个字符串）**，
        //   不是 malloc（`Alloc = 40`）。分配到的根本不是内存，后面所有元素都写飞了。
        AddRI(OpCode.MOVE, 0, count * 4);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count)); // R0 = 块地址

        // ⚠ 原来循环里 `PUSH R0` 指望 R0 一直是基址，可第一轮之后 R0 已经是**上一个元素的值**；
        //   而 `POP R1` 又冲掉了正在当游标用的 R1 ⇒ 元素 1..n-1 全写到"上一个元素值"那个地址。
        //   改法：基址常驻栈顶，每存一个元素重新取一次（元素表达式里可能有函数调用，
        //   任何寄存器都靠不住）。
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));   // 基址常驻栈顶
        for (int i = 0; i < count; i++)
        {
            GenerateExpression(node.Elements[i]);                   // R0 = 元素值
            instructions.Add(new Instruction(OpCode.POP, [Reg(1)])); // R1 = 基址
            instructions.Add(new Instruction(OpCode.PUSH, [Reg(1)]));// 立刻放回
            instructions.Add(new Instruction(OpCode.MOVE,
                [Mem(i == 0 ? "R1" : $"R1+{i * 4}"), Reg(0)],
                instructions.Count));                               // [base+i*4] = 元素值
        }
        instructions.Add(new Instruction(OpCode.POP, [Reg(0)]));    // 返回值 = 基址
    }

    /// <summary>元素地址 → R0：<c>base + idx*4</c>（无数组头）。</summary>
    private void EmitElementAddress(ASTNode target, ASTNode index)
    {
        GenerateExpression(target);                                // R0 = 基址
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
        GenerateExpression(index);                                 // R0 = 下标
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));   // R1 = 基址
        instructions.Add(new Instruction(OpCode.SHL,
            [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 2)], instructions.Count));
        instructions.Add(new Instruction(OpCode.ADD,
            [Reg(0), Reg(0), Reg(1)], instructions.Count));
    }

    private void GenerateIndexRead(IndexNode node)
    {
        EmitElementAddress(node.Target, node.Index);               // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.MOVE,
            [Reg(0), Mem("R0")], instructions.Count));             // R0 = 元素值（load）
    }

    private void GenerateIndexAssign(IndexAssignNode node)
    {
        GenerateExpression(node.Value);                            // R0 = 右值
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
        EmitElementAddress(node.Target, node.Index);               // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));   // R1 = 右值
        instructions.Add(new Instruction(OpCode.MOVE,
            [Mem("R0"), Reg(1)], instructions.Count));             // [地址] = 右值（dest 在前）
        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)], instructions.Count));
    }

    private void GenerateIndexOpAssign(IndexOpAssignNode node)
    {
        EmitElementAddress(node.Target, node.Index);               // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));  // 地址压栈（求值会改所有寄存器）
        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Mem("R0")], instructions.Count)); // R1 = 旧值
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(1)]));
        GenerateExpression(node.Value);                            // R0 = 右值
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));   // R1 = 旧值
        OpCode op = node.Op switch
        {
            "+=" => OpCode.ADD,
            "-=" => OpCode.SUB,
            "*=" => OpCode.MUL,
            "/=" => OpCode.DIV,
            _ => OpCode.ADD,
        };
        instructions.Add(new Instruction(op, [Reg(0), Reg(1), Reg(0)], instructions.Count)); // R0 = 旧值 op 右值
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));   // R1 = 地址
        instructions.Add(new Instruction(OpCode.MOVE,
            [Mem("R1"), Reg(0)], instructions.Count));             // [地址] = 结果
    }
}
