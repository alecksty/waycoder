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
            case ArrayLiteralNode arrayLiteral:
                GenerateArrayLiteral(arrayLiteral);
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

    // ══════════════════════════════════════════════════════════════════════════
    // 数组（下标）代码生成
    //
    // 布局 `[count, e0, e1, …]`，每元素 4 字节 ⇒ 元素 i 在 `base + i*4 + 4`
    // （与 Go/Kotlin/JS/C#/Python 同一套；R/Fortran/Pascal 那种自建扁布局才没有 +4 头）。
    //
    // ⚠ 全程用 PUSH/POP 而不是寄存器暂存：右值表达式里可能有函数调用（`inc(a[i])`），
    //   任何寄存器都会被改掉。前提是**函数序言必须真的预留栈帧**（见 CodeGenerator.cs
    //   的帧回填）—— 没有帧的话 PUSH 会直接写进局部变量槽。
    // ══════════════════════════════════════════════════════════════════════════

    private void GenerateArrayLiteral(ArrayLiteralNode node)
    {
        int count = node.Elements.Count;
        AddRI(OpCode.MOVE, 0, (count + 1) * 4);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count));  // R0 = 块地址
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));    // 基址常驻栈顶

        AddRI(OpCode.MOVE, 0, count);
        PopReg(1);
        PushReg(1);
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Mem("R1"), Reg(0) }, instructions.Count));                       // [base] = count

        for (int i = 0; i < count; i++)
        {
            GenerateExpression(node.Elements[i]);                                                // R0 = 元素值
            PopReg(1);
            PushReg(1);
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Mem($"R1+{(i + 1) * 4}"), Reg(0) }, instructions.Count));     // [base+off] = 元素值
        }

        PopReg(0);                                                                               // 返回值 = 基址
    }

    /// <summary>算出元素地址到 R0：base + idx*4 + 4。base 由调用方压栈（栈顶）。</summary>
    private void EmitElementAddress(ASTNode target, ASTNode index)
    {
        GenerateExpression(target);                       // R0 = 基址
        PushReg(0);
        GenerateExpression(index);                        // R0 = 下标
        PopReg(1);                                        // R1 = 基址
        AddRI(OpCode.SHL, 0, 2);
        AddRI(OpCode.ADD, 0, 4);                          // 跳过数组头
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { Reg(0), Reg(0), Reg(1) }, instructions.Count));
    }

    private void GenerateIndexRead(IndexNode node)
    {
        EmitElementAddress(node.Target, node.Index);      // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Reg(0), Mem("R0") }, instructions.Count));   // R0 = 元素值（load）
    }

    private void GenerateIndexAssign(IndexAssignNode node)
    {
        GenerateExpression(node.Value);                   // R0 = 右值
        PushReg(0);
        EmitElementAddress(node.Target, node.Index);      // R0 = 元素地址
        PopReg(1);                                        // R1 = 右值
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Mem("R0"), Reg(1) }, instructions.Count));   // [地址] = 右值（dest 在前）
        AddReg(OpCode.MOVE, 0, 1);                        // 表达式值 = 右值
    }

    private void GenerateIndexOpAssign(IndexOpAssignNode node)
    {
        EmitElementAddress(node.Target, node.Index);      // R0 = 元素地址
        PushReg(0);                                       // 地址压栈（下面的求值会改所有寄存器）
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Reg(1), Mem("R0") }, instructions.Count));   // R1 = 旧值
        PushReg(1);
        GenerateExpression(node.Value);                   // R0 = 右值
        PopReg(1);                                        // R1 = 旧值
        var op = node.Op switch
        {
            "+=" => OpCode.ADD,
            "-=" => OpCode.SUB,
            "*=" => OpCode.MUL,
            "/=" => OpCode.DIV,
            "%=" => OpCode.MOD,
            _ => OpCode.ADD,
        };
        AddReg(op, 0, 1);                                 // R0 = 旧值 op 右值
        PopReg(1);                                        // R1 = 地址
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Mem("R1"), Reg(0) }, instructions.Count));   // [地址] = 结果
    }

    // ── 小助手（只在本文件用）──────────────────────────────────────────────────
    private void PushReg(int r) => instructions.Add(new Instruction(OpCode.PUSH,
        new List<Operand> { Reg(r) }, instructions.Count));

    private void PopReg(int r) => instructions.Add(new Instruction(OpCode.POP,
        new List<Operand> { Reg(r) }, instructions.Count));

    private void AddReg(OpCode op, int dst, int src) => instructions.Add(new Instruction(op,
        new List<Operand> { Reg(dst), Reg(src) }, instructions.Count));

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
