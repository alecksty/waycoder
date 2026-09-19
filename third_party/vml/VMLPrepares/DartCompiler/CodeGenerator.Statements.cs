using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace DartCompiler;

public partial class CodeGenerator
{
    /// <summary>Dart 类型 → (byteSize, load/store opcode)</summary>
    private static (int size, OpCode op) TypeInfo(string type) =>
        type == "double" ? (8, OpCode.MOVED) : (4, OpCode.MOVE);

    private void GenerateStatement(ASTNode node)
    {
        if (node is ASTNode ast && ast.Line > 0)
            CurrentSourceLine = ast.Line;

        switch (node)
        {
            case MethodDeclNode method:
                GenerateMethod(method);
                break;
            case ClassDeclNode classDecl:
                GenerateClass(classDecl);
                break;
            case VarDeclNode varDecl:
                GenerateVarDecl(varDecl);
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
            case DoWhileNode doWhile:
                GenerateDoWhile(doWhile);
                break;
            case BreakNode:
                Sta!.EmitBreak();
                break;
            case ContinueNode:
                Sta!.EmitContinue();
                break;
            case AssignNode assign:
                GenerateAssign(assign);
                break;
            case OpAssignNode opAssign:
                GenerateOpAssign(opAssign);
                break;
            case BlockNode block:
                foreach (var stmt in block.Statements)
                    GenerateStatement(stmt);
                break;
            case ExprStmtNode exprStmt:
                GenerateExpression(exprStmt.Expr);
                break;
            case ThrowStmt throwStmt:
                if (HandleMCUThrow(() => AddRI(OpCode.MOVE, 0, -1)))
                    break;
                if (throwStmt.Expression != null) GenerateExpression(throwStmt.Expression);
                else AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.THROW, [Reg(0)]));
                break;
            case TryStmt tryStmt:
                if (HandleMCUTry(() => { foreach (var s in tryStmt.Body) GenerateStatement(s); if (tryStmt.FinallyBlock != null) foreach (var s in tryStmt.FinallyBlock) GenerateStatement(s); }))
                    break;
                else
                {
                    string catchLabel = $"catch_{labelCounter}";
                    string endTryLabel = $"endtry_{labelCounter++}";
                    instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
                    foreach (var stmt in tryStmt.Body) GenerateStatement(stmt);
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endTryLabel)]));
                    labels[catchLabel] = instructions.Count;
                    foreach (var cc in tryStmt.Catches)
                    {
                        if (cc.VarName != null)
                        {
                            nextStackOffset += 4;
                            symbolTable[cc.VarName] = (cc.ExcType ?? "Exception", nextStackOffset);
                            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{nextStackOffset}"), Reg(0)]));
                        }
                        foreach (var stmt in cc.Body) GenerateStatement(stmt);
                        instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                    }
                    if (tryStmt.FinallyBlock != null)
                        foreach (var stmt in tryStmt.FinallyBlock) GenerateStatement(stmt);
                    labels[endTryLabel] = instructions.Count;
                }
                break;
            case PrefixPostfixNode pp:
                GenerateExpression(pp);
                break;
            default:
                GenerateExpression(node);
                break;
        }
    }

    private void GenerateMethod(MethodDeclNode node, string? className = null)
    {
        string funcName = className != null ? $"{className}_{node.Name}" : node.Name;

        // external/native 方法：注册到外部方法集，调用时使用裸名 CALL
        if (node.IsExternal)
        {
            _externalMethods.Add(node.Name);
            return;
        }

        string funcLabel = $"func_{funcName}";

        string skipLabel = $"skip_{funcLabel}";
        AddInstruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, skipLabel) });

        AddLabel(funcLabel);
        EmitPrologue();

        // 同顶层：局部变量槽必须落在 SP 之下，否则每个 PUSH/CALL 都会写花局部变量。
        // 帧大小边生成边确定，故先占位、函数体生成完再回填。
        int framePatchIndex = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), Imm(0)], framePatchIndex));

        var savedSymbols = new Dictionary<string, (string type, int offset)>(symbolTable);
        int savedOffset = nextStackOffset;

        int paramBaseOffset = -12;
        for (int i = 0; i < node.Parameters.Count; i++)
        {
            symbolTable[node.Parameters[i].Name] = (node.Parameters[i].Type, paramBaseOffset - i * 4);
        }

        nextStackOffset = 0;

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // 回填帧大小 + 退栈（+8 安全边界，与 EmitPrologueWithFrame 口径一致）
        int frameSize = nextStackOffset + 8;
        instructions[framePatchIndex] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), Imm(frameSize)], framePatchIndex);
        instructions.Add(new Instruction(OpCode.ADD,
            [Reg(13), Reg(13), Imm(frameSize)], instructions.Count));

        EmitEpilogue();
        AddLabel(skipLabel);

        symbolTable = savedSymbols;
        nextStackOffset = savedOffset;
    }

    private void GenerateVarDecl(VarDeclNode node)
    {
        var (size, storeOp) = TypeInfo(node.Type);
        nextStackOffset += size;
        symbolTable[node.Name] = (node.Type, nextStackOffset);

        if (node.Init != null)
        {
            GenerateExpression(node.Init);
            instructions.Add(new Instruction(storeOp,
                new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{nextStackOffset}"), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
        }
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
        Sta!.EmitWhile(
            emitCondition: () => GenerateExpression(node.Condition),
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });
    }

    private void GenerateFor(ForNode node)
    {
        Sta!.EmitFor(
            emitInit: node.Init != null ? () => GenerateStatement(node.Init) : null,
            emitCondition: node.Condition != null ? () => GenerateExpression(node.Condition) : null,
            emitIncrement: node.Increment != null ? () => GenerateExpression(node.Increment) : null,
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });
    }

    private void GenerateDoWhile(DoWhileNode node)
    {
        Sta!.EmitDoWhile(
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); },
            emitCondition: () => GenerateExpression(node.Condition));
    }

    private void GenerateAssign(AssignNode node)
    {
        GenerateExpression(node.Value);
        StoreVar(node.Name);
    }

    private void GenerateOpAssign(OpAssignNode node)
    {
        LoadVar(node.Name);
        instructions.Add(new Instruction(OpCode.PUSH,
            new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        GenerateExpression(node.Value);
        instructions.Add(new Instruction(OpCode.POP,
            new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));

        OpCode op = node.Op switch
        {
            "+=" => OpCode.ADD,
            "-=" => OpCode.SUB,
            "*=" => OpCode.MUL,
            "/=" => OpCode.DIV,
            "%=" => OpCode.MOD,
            _ => OpCode.ADD,
        };
        instructions.Add(new Instruction(op,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));

        StoreVar(node.Name);
    }

    private void LoadVar(string name)
    {
        if (symbolTable.TryGetValue(name, out var entry))
        {
            var loadOp = TypeInfo(entry.type).op;
            instructions.Add(new Instruction(loadOp,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, MemOff(entry.offset)) },
                instructions.Count));
        }
        else
        {
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
            {
                // 局部表与 `dataSection` 都没有 ⇒ 这个名字**从未声明过**。
                // 此前这里顺手建个初值 0 的槽就当成全局 —— `int a = 1; int b = a + nosuch;`
                // 编得过、运行期静静按 0 算出个错答案。
                //（`LoadVar` 是本前端读变量的**唯一**收口处，三个调用点都汇聚到这里。）
                ReportUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                EmitUndefinedFallback();
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                instructions.Count));
        }
    }

    private void StoreVar(string name)
    {
        if (symbolTable.TryGetValue(name, out var entry))
        {
            var storeOp = TypeInfo(entry.type).op;
            instructions.Add(new Instruction(storeOp,
                new List<Operand> { new Operand(OperandType.MEMORY, MemOff(entry.offset)), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
        }
        else
        {
            if (!dataSection.ContainsKey($"var_{name}"))
            {
                nextStackOffset += 4;
                symbolTable[name] = ("var", nextStackOffset);
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{nextStackOffset}"), new Operand(OperandType.REGISTER, 0) },
                    instructions.Count));
                return;
            }
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.MEMORY, dataLabel), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
        }
    }
}
