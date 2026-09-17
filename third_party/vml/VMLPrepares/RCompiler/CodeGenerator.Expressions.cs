using VMLAssembler;
using CompilerBase;

namespace RCompiler;

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
            case SeqNode seq:
                GenerateSeq(seq);
                break;
            case ListNode list:
                GenerateList(list);
                break;
            case IndexNode index:
                GenerateIndex(index);
                break;
            case FuncDefNode funcDef:
                GenerateFuncDef(funcDef, "_anon_" + labelCounter);
                break;
            case AssignNode assign:
                // 表达式中的赋值(如<<-): 求值右值并存到变量
                GenerateExpression(assign.Value);
                EmitStoreVar(assign.Name);
                break;
            case IndexAssignNode idxAssign:
                GenerateIndexAssign(idxAssign);
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    private void GenerateLiteral(LiteralNode node)
    {
        EmitLoadConstant(node.Value);
    }

    private void GenerateVar(VarNode node)
    {
        EmitLoadVar(node.Name);
    }

    private void GenerateBinary(BinaryNode node)
    {
        var left = WrapExpr(node.Left);
        var right = WrapExpr(node.Right);

        switch (node.Op)
        {
            case "+": case "-": case "*": case "/":
                _expr!.EmitBinOp(left, right, node.Op);
                break;
            case "^":
                // R's ^ is exponentiation. 保持内联 (R栈约定与C __stdcall不兼容)
                GenerateExpression(node.Right);
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(node.Left);
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(2), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                {
                    string pwl = $"pl_{labelCounter}"; string pwe = $"pe_{labelCounter++}";
                    instructions.Add(new Instruction(OpCode.CMP, [Reg(1), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.JLE, [new Operand(OperandType.LABEL, pwe)]));
                    labels[pwl] = instructions.Count;
                    instructions.Add(new Instruction(OpCode.MUL, [Reg(0), Reg(0), Reg(2)]));
                    instructions.Add(new Instruction(OpCode.SUB, [Reg(1), Reg(1), new Operand(OperandType.IMMEDIATE, 1)]));
                    instructions.Add(new Instruction(OpCode.JNZ, [Reg(1), new Operand(OperandType.LABEL, pwl)]));
                    labels[pwe] = instructions.Count;
                }
                break;
            case "%/%":
                _expr!.EmitBinOp(left, right, "/"); // R 整除 = VML 整数除法
                break;
            case "%%":
                _expr!.EmitBinOp(left, right, "%");
                break;
            case "==": case "!=": case "<": case ">": case "<=": case ">=":
                _expr!.EmitCmp(left, right, node.Op);
                break;
            case "&":
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
            case "|":
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
            case "~":
                // formula operator — evaluate both sides, result is the RHS
                GenerateExpression(node.Left);
                GenerateExpression(node.Right);
                return;
            case "$":
                // list member access — 简化：求值左侧对象，R0 = 成员偏移
                GenerateExpression(node.Left);
                GenerateExpression(node.Right);
                return;
            case "%in%":
                // membership test — simplified: always returns 0 for now
                GenerateExpression(node.Left);
                GenerateExpression(node.Right);
                AddRI(OpCode.MOVE, 0, 0);
                return;
            case ":":
                _expr!.EmitBinOp(left, right, "+");
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown binary operator: {node.Op}");
        }
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-":
                _expr!.EmitNeg(operand);
                break;
            case "!":
                _expr!.EmitNot(operand);
                break;
            default:
                GenerateExpression(node.Operand);
                break;
        }
    }

    private void GenerateCall(CallNode node)
    {
        // class(x) — return default type "numeric" (S3 class tag)
        if (node.Name == "class" && node.Arguments.Count >= 1)
        {
            GenerateExpression(node.Arguments[0]); // evaluate x (side effects)
            string clsLabel = AddStringCached("numeric");
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, clsLabel)]));
            return;
        }
        // matrix(data, nrow, ncol) — allocate matrix, for now return data as-is
        if (node.Name == "matrix" && node.Arguments.Count >= 1)
        {
            GenerateExpression(node.Arguments[0]);
            return;
        }
        // length(x) — return fixed value for now (vectors don't track length at runtime)
        if (node.Name == "length" && node.Arguments.Count >= 1)
        {
            GenerateExpression(node.Arguments[0]); // evaluate x
            AddRI(OpCode.MOVE, 0, 1); // default length = 1
            return;
        }
        // dim(x) — return NULL (no dim attribute yet)
        if (node.Name == "dim" && node.Arguments.Count >= 1)
        {
            GenerateExpression(node.Arguments[0]);
            AddRI(OpCode.MOVE, 0, 0); // NULL
            return;
        }
        // sum(x) — compile-time sum for SeqNode, runtime pass-through
        if (node.Name == "sum" && node.Arguments.Count >= 1)
        {
            if (node.Arguments[0] is SeqNode seq)
            {
                int total = 0;
                foreach (var e in seq.Elements)
                {
                    if (e is LiteralNode lit && lit.Value is int iv) total += iv;
                }
                AddRI(OpCode.MOVE, 0, total);
            }
            else GenerateExpression(node.Arguments[0]);
            return;
        }
        // mean(x) — compile-time average for SeqNode, runtime pass-through
        if (node.Name == "mean" && node.Arguments.Count >= 1)
        {
            if (node.Arguments[0] is SeqNode seq && seq.Elements.Count > 0)
            {
                int total = 0, count = 0;
                foreach (var e in seq.Elements)
                {
                    if (e is LiteralNode lit && lit.Value is int iv) { total += iv; count++; }
                }
                AddRI(OpCode.MOVE, 0, count > 0 ? total / count : 0);
            }
            else GenerateExpression(node.Arguments[0]);
            return;
        }
        // min(x) / max(x) — compile-time for SeqNode, runtime pass-through
        if ((node.Name == "min" || node.Name == "max") && node.Arguments.Count >= 1)
        {
            if (node.Arguments[0] is SeqNode seq && seq.Elements.Count > 0)
            {
                int val = 0; bool first = true;
                foreach (var e in seq.Elements)
                {
                    if (e is LiteralNode lit && lit.Value is int iv)
                    {
                        if (first) { val = iv; first = false; }
                        else val = node.Name == "min" ? Math.Min(val, iv) : Math.Max(val, iv);
                    }
                }
                AddRI(OpCode.MOVE, 0, val);
            }
            else GenerateExpression(node.Arguments[0]);
            return;
        }
        // sd(x) / var(x) — compile-time for SeqNode
        if ((node.Name == "sd" || node.Name == "var") && node.Arguments.Count >= 1)
        {
            if (node.Arguments[0] is SeqNode seq && seq.Elements.Count > 1)
            {
                int n = 0, sum = 0;
                var vals = new List<int>();
                foreach (var e in seq.Elements)
                {
                    if (e is LiteralNode lit && lit.Value is int iv) { sum += iv; vals.Add(iv); n++; }
                }
                float avg = (float)sum / n;
                float variance = 0;
                foreach (int v in vals) { float d = v - avg; variance += d * d; }
                variance /= (node.Name == "sd" ? n - 1 : n); // sd uses n-1 denominator
                if (node.Name == "sd") variance = (float)Math.Sqrt(variance);
                AddRI(OpCode.MOVE, 0, (int)variance);
            }
            else AddRI(OpCode.MOVE, 0, 0);
            return;
        }
        // range(x) — compile-time c(min, max)
        if (node.Name == "range" && node.Arguments.Count >= 1)
        {
            if (node.Arguments[0] is SeqNode seq && seq.Elements.Count > 0)
            {
                int minV = int.MaxValue, maxV = int.MinValue;
                foreach (var e in seq.Elements)
                {
                    if (e is LiteralNode lit && lit.Value is int iv)
                    { if (iv < minV) minV = iv; if (iv > maxV) maxV = iv; }
                }
                // Return as 2-element vector: alloc 8 bytes, store min+max
                AddRI(OpCode.MOVE, 0, 8);
                EmitAlloc();
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                AddRI(OpCode.MOVE, 0, minV);
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Mem("R1"), Reg(0)]));
                AddRI(OpCode.MOVE, 0, maxV);
                instructions.Add(new Instruction(OpCode.MOVE, [Mem("1+4"), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)]));
            }
            else GenerateExpression(node.Arguments[0]);
            return;
        }
        // seq(from, to, by) — compile-time sequence generator
        if (node.Name == "seq" && node.Arguments.Count >= 2)
        {
            if (node.Arguments[0] is LiteralNode lFrom && node.Arguments[1] is LiteralNode lTo
                && lFrom.Value is int from && lTo.Value is int to)
            {
                int by = 1;
                if (node.Arguments.Count >= 3 && node.Arguments[2] is LiteralNode lBy && lBy.Value is int byV) by = byV;
                int count = (to - from) / by + 1;
                if (count <= 0) count = 0;
                // Allocate count*4 bytes, fill with sequence
                AddRI(OpCode.MOVE, 0, count * 4);
                EmitAlloc();
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                for (int i = 0; i < count; i++)
                {
                    int val = from + i * by;
                    if (i > 0) instructions.Add(new Instruction(OpCode.ADD, [Reg(1), Reg(1), new Operand(OperandType.IMMEDIATE, 4)]));
                    else instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    AddRI(OpCode.MOVE, 2, val);
                    instructions.Add(new Instruction(OpCode.MOVE, [Mem("R1"), Reg(2)]));
                }
                instructions.Add(new Instruction(OpCode.POP, [Reg(0)]));
            }
            else GenerateExpression(node.Arguments[0]);
            return;
        }
        // print(x) / cat(...) — output value to console (MUST come before argument push)
        if ((node.Name == "print" || node.Name == "cat") && node.Arguments.Count >= 1)
        {
            bool isString = node.Arguments[0] is LiteralNode lit && lit.Value is string
                || (node.Arguments[0] is CallNode call && IsStringReturningFunc(call.Name));
            bool isFloat = node.Arguments[0] is LiteralNode flit && (flit.Value is float || flit.Value is double);

            // ⚠ 压一个实参**必须跟着清**（2026-09-17 调用约定统一后补的最后那句 `ADD R13 #4`）。
            //
            // 旧注释说 `print_int`/`print_str`/`print_float`（Lib/console.vml）的收尾是
            // 「被调用方清参数」的蹦床：
            //     move R1 @13        ; R1 = 返回地址
            //     add R13 #8         ; 跳过「返回地址 + 1 个实参」
            //     push R1 / ret
            // 那时**压一个实参正好抵消那一格**，调用前后 R13 完全守恒。
            //
            // 现在 `Lib/` 已按统一约定重生成（被调方一律裸 `ret`、调用方清栈）——
            // 那一格没人弹了，**不补 `ADD R13 #4` 就是每次 print/cat 净漏 4 字节**
            // （与 D/Fortran 的 `^^`/`**`、Forth 的 `."` 是同一族回归，那几处已同样补上）。
            // 保留压栈而不是改成裸 CALL，是因为这几个函数从 R0 取参、压进去的是**同一个值**：
            // 压着清掉，将来被调方改成读 `[R12+12]` 也不会错。
            GenerateExpression(node.Arguments[0]);
            instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)], instructions.Count));
            string printFn = isFloat ? "print_float" : (isString ? "print_str" : "print_int");
            instructions.Add(new Instruction(OpCode.CALL,
                [new Operand(OperandType.LABEL, printFn)], instructions.Count));
            instructions.Add(new Instruction(OpCode.ADD,
                [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)], instructions.Count));
            return;
        }
        // push arguments in reverse order
        for (int i = node.Arguments.Count - 1; i >= 0; i--)
        {
            GenerateExpression(node.Arguments[i]);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        }

        string funcLabel = $"func_{node.Name}";
        instructions.Add(new Instruction(OpCode.CALL,
            new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }, instructions.Count));

        // Clean up arguments from stack after CALL (caller-cleanup convention)
        int argBytes = node.Arguments.Count * 4;
        if (argBytes > 0)
        {
            instructions.Add(new Instruction(OpCode.ADD,
                new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argBytes) }, instructions.Count));
        }
    }

    private void GenerateSeq(SeqNode node)
    {
        // allocate array: elements.Count * 4 bytes
        int count = node.Elements.Count;
        int size = count * 4;
        AddRI(OpCode.MOVE, 0, size);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count)); // SYS_ALLOC = malloc

        if (count == 0)
        {
            AddRI(OpCode.MOVE, 0, 0);
            return;
        }

        // R0 = base pointer, use R1 as working pointer
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));

        for (int i = 0; i < count; i++)
        {
            GenerateExpression(node.Elements[i]); // R0 = value
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Mem("R1"), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
            if (i < count - 1)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) },
                    instructions.Count));
            }
        }

        // Restore R0 to original base: R1 = base + (count-1)*4 → R0 = R1 - (count-1)*4 = base
        instructions.Add(new Instruction(OpCode.SUB,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, (count - 1) * 4) },
            instructions.Count));
    }

    private void GenerateIndex(IndexNode node)
    {
        GenerateExpression(node.Target);
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        GenerateExpression(node.Index);
        instructions.Add(new Instruction(OpCode.SUB,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.MUL,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.POP,
            new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), Mem("R1") },
            instructions.Count));
    }

    private void GenerateList(ListNode node)
    {
        int count = node.Elements.Count;
        int size = count * 8; // each element = value (4 bytes) + tag pointer (4 bytes)
        AddRI(OpCode.MOVE, 0, size);
        instructions.Add(new Instruction(OpCode.SYSCALL,
            new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count));

        for (int i = 0; i < count; i++)
        {
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));

            // store value
            GenerateExpression(node.Elements[i].value);
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Mem("R1"), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));

            // advance pointer for next element
            if (i < count - 1)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 8) },
                    instructions.Count));
            }
        }

        // restore R0 to start of list
        instructions.Add(new Instruction(OpCode.SUB,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, (count - 1) * 8) },
            instructions.Count));
        if (count == 0)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }
    }
}
