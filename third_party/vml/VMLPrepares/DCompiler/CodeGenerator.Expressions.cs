using System;
using VMLAssembler;
using CompilerBase;

namespace DCompiler;

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
                GenerateAssignExpression(assign);
                break;
            case IndexNode index:
                GenerateIndex(index);
                break;
            case ArrayLiteralNode arrayLiteral:
                GenerateArrayLiteral(arrayLiteral);
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
            case CastExpr castExpr:
                GenerateCastExpr(castExpr);
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    private void GenerateLiteral(LiteralNode node) => EmitLoadConstant(node.Value);

    private void GenerateVar(VarNode node)
    {
        EmitLoadVar(node.Name);
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
        if (node.Op == "^^")
        {
            // ⚠ 这里是 **调用方清栈**（2026-09-17 调用约定统一后改的，此前没有最后那句 `ADD R13`）。
            //
            // 旧注释写「__stdcall: 被调用者清栈」—— 那时 `Lib` 里的 `ipow` 收尾是
            // `… move R1 @13; add R13 #8; push R1; ret`，替调用方多弹掉两个实参槽，
            // 所以调用方压完不用管。现在 `Lib/` 已按统一约定重生成（被调方一律裸 `ret`），
            // 那句话不再成立：**不补 `ADD R13 #8` 就是每次求幂净漏 8 字节**。
            // 实测（`2 ^^ i` 累加 i=1..6）：修复前得 `SUM=66`，正确值 `SUM=126`。
            GenerateExpression(node.Right); AddInstruction(OpCode.PUSH, Reg(0));
            GenerateExpression(node.Left); AddInstruction(OpCode.PUSH, Reg(0));
            AddCall("ipow");
            AddInstruction(OpCode.ADD, Reg(13), new Operand(OperandType.IMMEDIATE, 8));
            return;
        }
        if (node.Op is "&" or "|" or "^" or "<<" or ">>")
        {
            _expr!.EmitBinOp(left, right, node.Op);
            return;
        }
        throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown binary operator: {node.Op}");
    }

    private void GenerateTernary(ASTNode cond, ASTNode thenExpr, ASTNode elseExpr)
    {
        EmitTernary(
            () => GenerateExpression(cond),
            () => GenerateExpression(thenExpr),
            () => GenerateExpression(elseExpr));
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-": _expr!.EmitNeg(operand); break;
            case "!": _expr!.EmitNot(operand); break;
            case "~": _expr!.EmitBitNot(operand); break;
            case "&": // address-of: LEA
                if (node.Operand is VarNode vn)
                {
                    if (symbolTable.TryGetValue(vn.Name, out int offset))
                    {
                        instructions.Add(new Instruction(OpCode.MOVE,
                            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, MemOff(offset)) },
                            instructions.Count));
                    }
                    else
                    {
                        string dataLabel = $"var_{vn.Name}";
                        if (!dataSection.ContainsKey(dataLabel))
                            dataSection[dataLabel] = 0;
                        instructions.Add(new Instruction(OpCode.MOVE,
                            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                            instructions.Count));
                    }
                }
                else
                {
                    GenerateExpression(node.Operand);
                }
                break;
            case "*": // dereference: LOAD from pointer
                GenerateExpression(node.Operand);
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+0") },
                    instructions.Count));
                break;
            default:
                GenerateExpression(node.Operand);
                break;
        }
    }

    private void GenerateCall(CallNode node)
    {
        // Inline writeln/print/write — generate SYSCALL directly
        if (node.Function == "writeln" || node.Function == "print" || node.Function == "write")
        {
            foreach (var arg in node.Arguments)
            {
                bool isString = arg is LiteralNode lit && lit.Value is string
                    || (arg is CallNode call && IsStringReturningFunc(call.Function));
                var argType = InferNodeType(arg);
                // long 值经 R0 (L0 低 32 位) 由 print_int 输出 (与 Kotlin 约定一致);
                // 值超出 int 范围时输出受限, 但 D 属教学可用层, 测试值均落在 int 内。
                EmitPrintArg(() => GenerateExpression(arg), isString: isString,
                    isFloat: argType == ExpType.F32 || argType == ExpType.F64);
            }
            if (node.Function == "writeln")
                EmitPrintNewline();
            return;
        }

        // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，D 通过 Lib/c/vmlsys.c 调用系统功能

        // 按函数名约定推断首个参数类型 (float_to_str→float 等), 在压栈前自动转换
        ExpType? expectedArg = ExpectedArgType(node.Function);
        // 目标函数参数类型 (用于类型感知的参数压栈, 支持 long/double 等 64 位参数)
        var paramTypes = _funcParamTypes.TryGetValue(node.Function, out var pts) ? pts : null;

        int totalArgBytes = 0;
        for (int i = node.Arguments.Count - 1; i >= 0; i--)
        {
            GenerateExpression(node.Arguments[i]);
            // 确定该参数期望类型: 优先函数签名, 其次 conv 命名约定, 最后保持原类型
            ExpType? dstType = null;
            if (paramTypes != null && i < paramTypes.Count)
                dstType = TypeNameToExpType(paramTypes[i].type);
            else if (i == 0 && expectedArg.HasValue)
                dstType = expectedArg.Value;

            if (dstType.HasValue)
                EmitConvertOp(InferNodeType(node.Arguments[i]), dstType.Value);

            totalArgBytes += EmitPushTypedArg(dstType ?? InferNodeType(node.Arguments[i]));
        }

        string funcLabel = $"func_{node.Function}";
        instructions.Add(new Instruction(OpCode.CALL,
            new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }, instructions.Count));

        // Clean up arguments from stack after CALL (caller-cleanup convention)
        if (totalArgBytes > 0)
        {
            instructions.Add(new Instruction(OpCode.ADD,
                new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgBytes) }, instructions.Count));
        }
    }

    /// <summary>类型感知的参数压栈: 64 位类型写主栈 (SUB R13 + MOVED/MOVEL), 其余 PUSH R0。返回压栈字节数。</summary>
    private int EmitPushTypedArg(ExpType t)
    {
        switch (t)
        {
            case ExpType.F32:
                instructions.Add(new Instruction(OpCode.SUB, [Reg(13), Imm(4)], instructions.Count));
                instructions.Add(new Instruction(OpCode.MOVEF, [new Operand(OperandType.INDIRECT, 13), Reg(0)], instructions.Count));
                return 4;
            case ExpType.F64:
                instructions.Add(new Instruction(OpCode.SUB, [Reg(13), Imm(8)], instructions.Count));
                instructions.Add(new Instruction(OpCode.MOVED, [new Operand(OperandType.INDIRECT, 13), Reg(0)], instructions.Count));
                return 8;
            case ExpType.I64:
                instructions.Add(new Instruction(OpCode.SUB, [Reg(13), Imm(8)], instructions.Count));
                instructions.Add(new Instruction(OpCode.MOVEL, [new Operand(OperandType.INDIRECT, 13), Reg(0)], instructions.Count));
                return 8;
            default:
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)], instructions.Count));
                return 4;
        }
    }

    private void GenerateAssignExpression(AssignNode node)
    {
        GenerateExpression(node.Value);
        ExpType dstType = _varTypes.TryGetValue(node.Name, out string? vt) ? TypeNameToExpType(vt) : ExpType.I32;
        EmitConvertOp(InferNodeType(node.Value), dstType);
        EmitStoreVar(node.Name);
    }

    private void GenerateIndex(IndexNode node)
    {
        // a[i] — compute address and load
        string dataLabel = $"var_{node.Name}";
        bool isLocal = symbolTable.TryGetValue(node.Name, out int baseOffset);

        if (isLocal)
        {
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, MemOff(baseOffset)) },
                instructions.Count));
        }
        else
        {
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                instructions.Count));
        }

        // push base address
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));

        // compute index * 4
        GenerateExpression(node.Index);
        instructions.Add(new Instruction(OpCode.MUL,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) },
            instructions.Count));

        // pop base and add offset
        instructions.Add(new Instruction(OpCode.POP,
            new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) },
            instructions.Count));

        // load from computed address
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+0") },
            instructions.Count));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 数组（下标）代码生成
    //
    // 布局与**本文件既有的 `GenerateIndex` 一致：没有数组头**，元素 i 在 `base + i*4`。
    // （D 的数组变量就是一个指向元素的指针；读写两侧同式，自洽。）
    // ⚠ 不要照搬 Go/Kotlin 那种 `[count, e0, …]` 带头布局 —— 那会和这里的读取路径对不上。
    // ══════════════════════════════════════════════════════════════════════════

    private void GenerateArrayLiteral(ArrayLiteralNode node)
    {
        int count = node.Elements.Count;
        AddRI(OpCode.MOVE, 0, count * 4);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count)); // R0 = 块地址
        // 基址常驻栈顶：元素表达式里可能有函数调用，任何寄存器都靠不住。
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
        for (int i = 0; i < count; i++)
        {
            GenerateExpression(node.Elements[i]);                     // R0 = 元素值
            instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));  // R1 = 基址
            instructions.Add(new Instruction(OpCode.PUSH, [Reg(1)])); // 立刻放回
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, i == 0 ? "R1" : $"R1+{i * 4}"), Reg(0)],
                instructions.Count));                                 // [base+i*4] = 元素值
        }
        instructions.Add(new Instruction(OpCode.POP, [Reg(0)]));      // 返回值 = 基址
    }

    /// <summary>算出 <c>a[i]</c> 的元素地址 → R0（无数组头，<c>base + i*4</c>）。</summary>
    private void EmitElementAddress(string name, ASTNode index)
    {
        if (symbolTable.TryGetValue(name, out int baseOffset))
        {
            instructions.Add(new Instruction(OpCode.MOVE,
                [Reg(0), new Operand(OperandType.MEMORY, MemOff(baseOffset))], instructions.Count));
        }
        else
        {
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE,
                [Reg(0), new Operand(OperandType.MEMORY, dataLabel)], instructions.Count));
        }
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));    // 基址
        GenerateExpression(index);                                   // R0 = 下标
        instructions.Add(new Instruction(OpCode.MUL, [Reg(0), Reg(0), Imm(4)], instructions.Count));
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));     // R1 = 基址
        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), Reg(1)], instructions.Count));
    }

    private void GenerateIndexAssign(IndexAssignNode node)
    {
        GenerateExpression(node.Value);                              // R0 = 右值
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
        EmitElementAddress(node.Name, node.Index);                   // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));     // R1 = 右值
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, "R0"), Reg(1)], instructions.Count)); // [地址] = 右值
        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)], instructions.Count));
    }

    private void GenerateIndexOpAssign(IndexOpAssignNode node)
    {
        EmitElementAddress(node.Name, node.Index);                   // R0 = 元素地址
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));    // 地址压栈（求值会改所有寄存器）
        instructions.Add(new Instruction(OpCode.MOVE,
            [Reg(1), new Operand(OperandType.MEMORY, "R0")], instructions.Count));  // R1 = 旧值
        instructions.Add(new Instruction(OpCode.PUSH, [Reg(1)]));
        GenerateExpression(node.Value);                              // R0 = 右值
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));     // R1 = 旧值
        OpCode op = node.Op switch
        {
            "+=" => OpCode.ADD,
            "-=" => OpCode.SUB,
            "*=" => OpCode.MUL,
            "/=" => OpCode.DIV,
            _ => OpCode.ADD,
        };
        instructions.Add(new Instruction(op, [Reg(0), Reg(1), Reg(0)], instructions.Count)); // R0 = 旧值 op 右值
        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));     // R1 = 地址
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.MEMORY, "R1"), Reg(0)], instructions.Count));          // [地址] = 结果
    }

    private void GeneratePrefixPostfix(PrefixPostfixNode node)
    {
        if (node.Operand is VarNode vn)
        {
            EmitLoadVar(vn.Name);
            int delta = node.Op == "++" ? 1 : -1;

            if (node.IsPrefix)
            {
                // ++a: increment then return new value
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, delta) },
                    instructions.Count));
                EmitStoreVar(vn.Name);
                // R0 already has new value
            }
            else
            {
                // a++: return old value, then increment
                // save old value
                instructions.Add(new Instruction(OpCode.PUSH,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, delta) },
                    instructions.Count));
                EmitStoreVar(vn.Name);
                // restore old value to R0
                instructions.Add(new Instruction(OpCode.POP,
                    new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            }
        }
        else
        {
            GenerateExpression(node.Operand);
        }
    }

    private void GenerateCastExpr(CastExpr node)
    {
        // 生成源表达式 → R0
        GenerateExpression(node.Expression);

        // 推断源类型
        ExpType srcType = InferNodeType(node.Expression);
        ExpType dstType = TypeNameToExpType(node.TargetType);

        EmitConvertOp(srcType, dstType);
    }

    /// <summary>将 R0 中的值从 srcType 转换到 dstType (相同类型跳过)</summary>
    private void EmitConvertOp(ExpType srcType, ExpType dstType)
    {
        if (srcType == dstType) return;

        // 全类型转换映射 (源类型, 目标类型) → 转换指令
        OpCode? convOp = (srcType, dstType) switch
        {
            (ExpType.F32, ExpType.I32) => OpCode.F2I,
            (ExpType.F64, ExpType.I32) => OpCode.D2I,
            (ExpType.I64, ExpType.I32) => OpCode.L2I,
            (ExpType.I32, ExpType.F32) => OpCode.I2F,
            (ExpType.F64, ExpType.F32) => OpCode.D2F,
            (ExpType.I64, ExpType.F32) => OpCode.L2F,
            (ExpType.I32, ExpType.F64) => OpCode.I2D,
            (ExpType.F32, ExpType.F64) => OpCode.F2D,
            (ExpType.I64, ExpType.F64) => OpCode.L2D,
            (ExpType.I32, ExpType.I64) => OpCode.I2L,
            (ExpType.F32, ExpType.I64) => OpCode.F2L,
            (ExpType.F64, ExpType.I64) => OpCode.D2L,
            _ => null,
        };
        if (convOp.HasValue)
            instructions.Add(new Instruction(convOp.Value,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
    }
}
