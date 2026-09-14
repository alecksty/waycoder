using VMLAssembler;
using CompilerBase;

namespace RCompiler;

public partial class CodeGenerator
{
    private void GenerateStatement(ASTNode node)
    {
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

        // function prologue + 预留栈空间 (64 bytes)
        EmitPrologueWithFrame(64);

        // save old symbol table, set up parameters
        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        int numParams = node.Parameters.Count;
        for (int i = 0; i < numParams; i++)
            symbolTable[node.Parameters[i]] = -(12 + i * 4);

        // generate body
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // function epilogue — 先释放临时栈空间
        instructions.Add(new Instruction(OpCode.ADD,
            [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 64)],
            instructions.Count));
        EmitEpilogue();

        // skip label
        AddLabel(skipLabel);

        // restore symbol table
        symbolTable = savedSymbols;
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

        Sta!.EmitWhile(
            emitCondition: () => GenerateExpression(node.Condition),
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });

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

        // Initialize loop variable (1-based index in R)
        AddRI(OpCode.MOVE, 0, 1);
        EmitStoreVar(node.Variable);

        string startLabel = $"for_{labelCounter++}";
        string endLabel = $"forend_{labelCounter++}";

        // save loop context
        string? savedBreak = currentBreakLabel;
        string? savedNext = currentNextLabel;
        currentBreakLabel = endLabel;
        currentNextLabel = startLabel;

        AddLabel(startLabel);

        // check loop condition: variable <= seqLength
        EmitLoadVar(node.Variable);
        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, seqLength) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JG,
            new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

        // load the value from the sequence vector: seqPtr[variable - 1]
        EmitLoadVar(seqVar);
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        EmitLoadVar(node.Variable);
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

        // increment loop variable
        EmitLoadVar(node.Variable);
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) },
            instructions.Count));
        EmitStoreVar(node.Variable);

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
