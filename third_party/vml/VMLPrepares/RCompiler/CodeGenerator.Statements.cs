using VMLAssembler;
using CompilerBase;

namespace RCompiler;

public partial class CodeGenerator
{
    private void GenerateStatement(ASTNode node)
    {
        // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
        // `> 0`：行号是 1-based，Line 没填的节点是 0，置成 0 会把上一句的行号冲掉。
        if (node.Line > 0) CurrentSourceLine = node.Line;
        switch (node)
        {
            case AssignNode assign:
                GenerateAssign(assign);
                break;
            case IndexAssignNode idxAssign:
                GenerateIndexAssign(idxAssign);
                break;
            case ReturnNode ret:
                GenerateReturn(ret);
                break;
            case IfNode ifNode:
                GenerateIf(ifNode);
                break;
            case WhileNode whileNode:
                GenerateWhile(whileNode);
                break;
            case ForNode forNode:
                GenerateFor(forNode);
                break;
            case RepeatNode repeatNode:
                GenerateRepeat(repeatNode);
                break;
            case BreakNode _:
                GenerateBreak();
                break;
            case NextNode _:
                GenerateNext();
                break;
            case CallNode call:
                GenerateExpression(call);
                break;
            default:
                GenerateExpression(node);
                break;
        }
    }

    private void GenerateAssign(AssignNode node)
    {
        // Check if assigning a function definition
        if (node.Value is FuncDefNode funcDef)
        {
            GenerateFuncDef(funcDef, node.Name);
            return;
        }

        GenerateExpression(node.Value);
        EmitStoreVar(node.Name);
    }

    /// <summary>索引赋值: target[index] <- value</summary>
    private void GenerateIndexAssign(IndexAssignNode node)
    {
        // 1. 计算右值 → 存到 R2 避免栈操作覆盖局部变量
        GenerateExpression(node.Value);
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)], instructions.Count));

        // 2. 计算索引地址: base + (index-1)*4 (R 是 1-based)
        GenerateExpression(node.Target);               // R0 = base pointer
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)], instructions.Count)); // R1 = base
        GenerateExpression(node.Index);                // R0 = index
        instructions.Add(new Instruction(OpCode.SUB,
            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)],
            instructions.Count));                       // convert 1-based → 0-based
        instructions.Add(new Instruction(OpCode.MUL,
            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)],
            instructions.Count));                       // * 4 bytes per element
        instructions.Add(new Instruction(OpCode.ADD,
            [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)],
            instructions.Count));                       // R1 = base + offset

        // 3. STORE value at address (value in R2)
        instructions.Add(new Instruction(OpCode.MOVE,
            [Mem("R1"), new Operand(OperandType.REGISTER, 2)],
            instructions.Count));                       // STORE @1, R2
        // Return value in R0
        instructions.Add(new Instruction(OpCode.MOVE,
            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)], instructions.Count));
    }

    private void GenerateFuncDef(FuncDefNode node, string assignedName)
    {
        string funcLabel = $"func_{assignedName}";

        // jump over function body
        string skipLabel = $"skip_{funcLabel}";
        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, skipLabel) }, instructions.Count));

        AddLabel(funcLabel);

        // 函数序言 + **帧占位**（与顶层 GenerateCode 同一口径，见那段注释）。
        // ⚠ 这里此前是写死的 `EmitPrologueWithFrame(64)` —— 固定的 64 字节装不下真实函数的
        //    局部量与表达式临时区，超出的部分直接写进调用方的帧（踩内存）。
        //    骨架照不出来，因为骨架的函数都极小（`(define (f x) (+ x 1))`）。
        EmitPrologue();
        int framePatchIndex = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], framePatchIndex));

        // save old symbol table, set up parameters
        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        nextStackOffset = 0;   // 每个函数有自己的帧，不吃顶层/上一个函数留下的偏移
        int numParams = node.Parameters.Count;
        for (int i = 0; i < numParams; i++)
            symbolTable[node.Parameters[i]] = -(12 + i * 4);

        // generate body
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // 回填帧大小（+8 安全边界，与顶层同口径）
        int frameSize = nextStackOffset + 8;
        instructions[framePatchIndex] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)], framePatchIndex);

        // function epilogue — 先释放临时栈空间（用回填后的真实大小，不再是写死的 64）
        instructions.Add(new Instruction(OpCode.ADD,
            [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, frameSize)],
            instructions.Count));
        EmitEpilogue();

        // skip label
        AddLabel(skipLabel);

        // 恢复符号表 —— **必须就地恢复**（Clear + 逐项写回），不能 `symbolTable = savedSymbols`。
        // `_varOffsets`（EmitLoadVar/EmitStoreVar 用的那张表）持有的是 symbolTable **这个对象的引用**，
        // 换成新对象后两者就分家了：EmitStoreVar 的「新变量」分支把偏移写进 symbolTable，
        // 而 EmitLoadVar 在 `_varOffsets` 里查不到 ⇒ 回退成 dataSection 里的全局 `var_xxx`（初值 0）。
        // 现象就是「定义过函数之后，之后每个变量的初始化都丢掉、读回来恒为 0」，
        // 连带 `i` 读成 0 → `(0-1)*4 = -4` → 地址 FFFFFFFC 越界崩溃。
        symbolTable.Clear();
        foreach (var kv in savedSymbols)
            symbolTable[kv.Key] = kv.Value;
        nextStackOffset = savedOffset;

        // store function label reference in the variable (just store 1 as placeholder)
        // The function is callable via func_<name> label
        AddRI(OpCode.MOVE, 0, 1);
        EmitStoreVar(assignedName);
    }

    private void GenerateReturn(ReturnNode node)
        => EmitReturn(node.Value != null ? () => GenerateExpression(node.Value) : null);

    private void GenerateIf(IfNode node)
    {
        Sta!.EmitIf(
            emitCondition: () => GenerateExpression(node.Condition),
            emitThen: () => { foreach (var stmt in node.ThenBody) GenerateStatement(stmt); },
            emitElse: node.ElseBody != null ? () => { foreach (var stmt in node.ElseBody) GenerateStatement(stmt); } : null);
    }

    private void GenerateWhile(WhileNode node)
    {
        // save loop context for break/next
        string? savedBreak = currentBreakLabel;
        string? savedNext = currentNextLabel;
        currentBreakLabel = $"wend_{labelCounter++}";
        currentNextLabel = $"while_{labelCounter++}";

        // ⚠ **这两个标签以前从来没被落过** —— `GenerateBreak`/`GenerateNext` 发的是
        //   `JMP wend_N` / `JMP while_N`，而全 R 前端没有任何一处 `LABEL wend_N`。
        //   后果：循环里一写 `break`，那条跳转就指向一个不存在的标签
        //   （P2 之前是"链接期警告、运行到才崩"，P2 之后**直接编译不过**）。
        //   实测最小复现：`i<-0; while (i<10) { i<-i+1; if (i==5) { break } }; print(i)`
        //   → `error: 未定义的函数 'wend_0'`。
        //   （之前 grep 没找到引用方，是因为标签名是 `$"wend_{…}"` 插值拼的 ——
        //    字面量 `"wend_"` 只出现在赋值那行，"读"它的是同文件里两个生成函数。）
        //
        // 落点按 R 的语义定：`next` 跳回**循环体开头**（回去重新判条件），
        // `break` 跳**循环之后**。`EmitWhile` 自己那套 `_loopStack` 标签是给
        // 别的语言用的（`StatementManager` 根本没有 EmitBreak/EmitContinue），
        // R 走的是自己这条 `currentBreakLabel` 通路。
        string brk = currentBreakLabel!;
        string nxt = currentNextLabel!;

        Sta!.EmitWhile(
            emitCondition: () => GenerateExpression(node.Condition),
            emitBody: () =>
            {
                AddLabel(nxt);
                foreach (var stmt in node.Body) GenerateStatement(stmt);
            });

        AddLabel(brk);

        // restore loop context
        currentBreakLabel = savedBreak;
        currentNextLabel = savedNext;
    }

    private void GenerateFor(ForNode node)
    {
        // R for loop: for (var in seq) { body }
        // seq is a vector (allocated array). We iterate over elements.
        // Strategy: compute seq pointer, store its length, use index-based iteration.

        // Step 1: evaluate the sequence and store it
        GenerateExpression(node.Sequence);
        // R0 = pointer to sequence vector (assume first element is the start)
        // We need to know the length. For now, assume the sequence is a c() vector
        // and we iterate by index from 1 to length.

        // Store the sequence pointer temporarily at a stack variable
        string seqVar = $"_seq_{node.Line}_{node.Column}";
        nextStackOffset += 4;
        symbolTable[seqVar] = nextStackOffset;
        EmitStoreVar(seqVar);

        // Store the length (assume it's stored as a global or we compute it)
        // For simplicity, support iteration over c() vectors of known length
        // and also support 1:n range sequences.

        // Determine length: if the sequence is a SeqNode, use its element count
        int seqLength = 1;
        if (node.Sequence is SeqNode seqNode)
        {
            seqLength = seqNode.Elements.Count;
        }

        // Initialize the **hidden 1-based cursor** used by the loop.
        // ⚠ 游标**不能**复用 node.Variable：R 的 for 语义是「循环变量拿到元素值」，
        // 若把元素值写回循环变量，下一轮的「取元素」与「自增」就都作用在元素值上了
        //（`for (i in c(7,8))` 只跑一轮：i=1 → 元素 7 → i=7 → 7+1=8 > 2 ⇒ 直接退出）。
        string idxVar = $"_foridx_{node.Line}_{node.Column}";
        nextStackOffset += 4;
        symbolTable[idxVar] = nextStackOffset;
        AddRI(OpCode.MOVE, 0, 1);
        EmitStoreVar(idxVar);

        string startLabel = $"for_{labelCounter++}";
        string endLabel = $"forend_{labelCounter++}";

        // save loop context
        string? savedBreak = currentBreakLabel;
        string? savedNext = currentNextLabel;
        currentBreakLabel = endLabel;
        currentNextLabel = startLabel;

        AddLabel(startLabel);

        // check loop condition: cursor <= seqLength
        EmitLoadVar(idxVar);
        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, seqLength) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JG,
            new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

        // load the value from the sequence vector: seqPtr[cursor - 1]
        EmitLoadVar(seqVar);
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        EmitLoadVar(idxVar);
        // subtract 1 for 0-based indexing
        instructions.Add(new Instruction(OpCode.SUB,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) },
            instructions.Count));
        // multiply by 4 for word offset
        instructions.Add(new Instruction(OpCode.MUL,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.POP,
            new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));
        // load the element value
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), Mem("R1") },
            instructions.Count));
        // store it back into the loop variable (R semantics: var gets the value)
        EmitStoreVar(node.Variable);

        // loop body
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // increment the cursor (NOT the user's loop variable — it holds the element value)
        EmitLoadVar(idxVar);
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) },
            instructions.Count));
        EmitStoreVar(idxVar);

        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        // restore loop context
        currentBreakLabel = savedBreak;
        currentNextLabel = savedNext;
    }

    private void GenerateRepeat(RepeatNode node)
    {
        string startLabel = $"repeat_{labelCounter++}";
        string endLabel = $"repend_{labelCounter++}";

        // save loop context
        string? savedBreak = currentBreakLabel;
        string? savedNext = currentNextLabel;
        currentBreakLabel = endLabel;
        currentNextLabel = startLabel;

        AddLabel(startLabel);

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        // restore loop context
        currentBreakLabel = savedBreak;
        currentNextLabel = savedNext;
    }

    private void GenerateBreak()
    {
        if (currentBreakLabel == null)
            throw new CompilationException(ErrorCode.CodeGen_BreakOutsideLoop, "break used outside of loop");
        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, currentBreakLabel) }, instructions.Count));
    }

    private void GenerateNext()
    {
        if (currentNextLabel == null)
            throw new CompilationException(ErrorCode.CodeGen_ContinueOutsideLoop, "next used outside of loop");
        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, currentNextLabel) }, instructions.Count));
    }
}
