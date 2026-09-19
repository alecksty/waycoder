using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace DCompiler;

public partial class CodeGenerator
{
    private void GenerateStatement(ASTNode node)
    {
        // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
        // `> 0`：行号是 1-based，Line 没填的节点是 0，置成 0 会把上一句的行号冲掉。
        if (node.Line > 0) { CurrentSourceLine = node.Line; CurrentSourceColumn = node.Column; }
        switch (node)
        {
            case FuncDefNode funcDef:
                GenerateFuncDef(funcDef);
                break;
            case VarDeclNode varDecl:
                GenerateVarDecl(varDecl);
                break;
            case ClassDeclNode classDecl:
                GenerateClassDecl(classDecl);
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
            case ForeachNode foreachNode:
                GenerateForeach(foreachNode);
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
            case CallNode call:
                GenerateExpression(call);
                break;
            case PrefixPostfixNode pp:
                GenerateExpression(pp);
                break;
            case SwitchNode switchNode:
                GenerateSwitch(switchNode);
                break;
            default:
                GenerateExpression(node);
                break;
        }
    }

    private void GenerateFuncDef(FuncDefNode node)
    {
        _funcReturnTypes[node.Name] = node.ReturnType;
        _funcParamTypes[node.Name] = node.Parameters;
        string funcLabel = node.Name.StartsWith("this.") ? $"class_{node.Name.Replace("this.", "")}" : $"func_{node.Name}";
        string skipLabel = $"skip_{funcLabel}";
        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, skipLabel) }, instructions.Count));

        AddLabel(funcLabel);
        EmitPrologue();
        int prologueEnd = instructions.Count; // 记录序言结束位置

        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        Vars!.ResetLocals();

        // 参数用 VarMemManager 分配 (D 约定: symbolTable 存负值→MemOff→R12+)
        // long/double 参数占 8 字节, 其余占 4 字节 (v1.66.64 修复 long 参数读取重叠)
        foreach (var param in node.Parameters)
        {
            int paramSize = TypeNameToExpType(param.type) is ExpType.I64 or ExpType.F64 ? 8 : 4;
            symbolTable[param.name] = -Vars.AllocParam(param.name, paramSize).Offset;
            _varTypes[param.name] = param.type;
        }

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // 根据局部变量实际大小插入 SUB R13 (在序言之后, body 之前)
        int frameSize = Vars.LocalFrameSize;
        if (frameSize > 0)
        {
            instructions.Insert(prologueEnd, new Instruction(OpCode.SUB,
                [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, frameSize + 8)]));
        }

        EmitEpilogue();
        AddLabel(skipLabel);

        symbolTable = savedSymbols;
        nextStackOffset = savedOffset;
    }

    private void GenerateVarDecl(VarDeclNode node)
    {
        _varTypes[node.Name] = node.TypeName;
        if (node.Initializer != null)
        {
            GenerateExpression(node.Initializer);
            EmitConvertOp(InferNodeType(node.Initializer), TypeNameToExpType(node.TypeName));
            EmitStoreVar(node.Name);
        }
        else
        {
            AddRI(OpCode.MOVE, 0, 0);
            EmitStoreVar(node.Name);
        }
    }

    private void GenerateClassDecl(ClassDeclNode node)
    {
        foreach (var member in node.Members)
        {
            if (member is FuncDefNode func)
            {
                string originalName = func.Name;
                GenerateStatement(func);
            }
            else
            {
                GenerateStatement(member);
            }
        }
    }

    private void GenerateReturn(ReturnNode node)
        => EmitReturn(node.Value != null ? () => GenerateExpression(node.Value) : null);

    private void GenerateIf(IfNode node)
    {
        GenerateExpression(node.Condition);
        string elseLabel = $"else_{labelCounter++}";
        string endLabel = $"endif_{labelCounter++}";

        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JE,
            new List<Operand> { new Operand(OperandType.LABEL, node.ElseBody != null ? elseLabel : endLabel) },
            instructions.Count));

        foreach (var stmt in node.ThenBody)
            GenerateStatement(stmt);

        if (node.ElseBody != null)
        {
            instructions.Add(new Instruction(OpCode.JMP,
                new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));
                AddLabel(elseLabel);

            foreach (var stmt in node.ElseBody)
                GenerateStatement(stmt);
        }

        AddLabel(endLabel);
    }

    private void GenerateWhile(WhileNode node)
    {
        string startLabel = $"while_{labelCounter++}";
        string endLabel = $"wend_{labelCounter++}";

        Sta!.PushLoopLabels(endLabel, startLabel);

        AddLabel(startLabel);

        GenerateExpression(node.Condition);
        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JE,
            new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        Sta!.PopLoopLabels();
    }

    private void GenerateDoWhile(DoWhileNode node)
    {
        string startLabel = $"dowhile_{labelCounter++}";
        string endLabel = $"dowhile_end_{labelCounter++}";

        Sta!.PushLoopLabels(endLabel, startLabel);

        AddLabel(startLabel);

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // continue label (for "continue" which should re-check condition)
        string continueLabel = $"dowhile_cont_{labelCounter++}";
        AddLabel(continueLabel);

        GenerateExpression(node.Condition);
        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JNE,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        Sta!.PopLoopLabels();
    }

    private void GenerateFor(ForNode node)
    {
        string startLabel = $"for_{labelCounter++}";
        string endLabel = $"forend_{labelCounter++}";
        string continueLabel = $"forcont_{labelCounter++}";

        Sta!.PushLoopLabels(endLabel, continueLabel);

        if (node.Init != null)
            GenerateStatement(node.Init);

        AddLabel(startLabel);

        if (node.Condition != null)
        {
            GenerateExpression(node.Condition);
            instructions.Add(new Instruction(OpCode.CMP,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.JE,
                new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));
        }

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        AddLabel(continueLabel);

        if (node.Increment != null)
            GenerateExpression(node.Increment);

        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        Sta!.PopLoopLabels();
    }

    private void GenerateForeach(ForeachNode node)
    {
        // Foreach: foreach (element; collection) { body }
        // Simplified: treat collection as array pointer, iterate by index
        // We need a hidden index counter
        string indexVar = $"__foreach_i_{labelCounter}";
        string startLabel = $"foreach_{labelCounter++}";
        string endLabel = $"foreach_end_{labelCounter++}";
        string continueLabel = $"foreach_cont_{labelCounter++}";

        Sta!.PushLoopLabels(endLabel, continueLabel);

        // index = 0 (hidden counter)
        if (!symbolTable.ContainsKey(indexVar))
        {
            var vi = Vars!.AllocLocal(indexVar, 4);
            symbolTable[indexVar] = -vi.Offset; // D 约定: 正值→MemOff→R12-
        }
        EmitLoadVar(indexVar);
        AddRI(OpCode.MOVE, 0, 0);
        EmitStoreVar(indexVar);

        // Compute collection length (simplified: if collection is a VarNode, assume array with length at label+0)
        // For now, assume array length is stored as first element or use a fixed bound
        // Simplified: just iterate 10 times as placeholder for non-indexed arrays
        string collectionLabel = $"__foreach_coll_{labelCounter}";

        AddLabel(startLabel);

        // Check index < 10 (simplified upper bound)
        EmitLoadVar(indexVar);
        instructions.Add(new Instruction(OpCode.CMP,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.JGE,
            new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

        // element = collection[index]
        if (node.Collection is VarNode collVar)
        {
            // Load collection base address
            EmitLoadVar(collVar.Name);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));

            // index * 4
            EmitLoadVar(indexVar);
            instructions.Add(new Instruction(OpCode.MUL,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) },
                instructions.Count));

            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.ADD,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+0") },
                instructions.Count));

            // Store into element variable
            if (!symbolTable.ContainsKey(node.VarName))
            {
                var vi = Vars!.AllocLocal(node.VarName, 4);
                symbolTable[node.VarName] = -vi.Offset;
            }
            EmitStoreVar(node.VarName);
        }

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // continue label
        AddLabel(continueLabel);

        // index++
        EmitLoadVar(indexVar);
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) },
            instructions.Count));
        EmitStoreVar(indexVar);

        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);

        Sta!.PopLoopLabels();
    }

    // Break/Continue now handled by Sta.EmitBreak()/EmitContinue() in GenerateStatement

    private void GenerateAssign(AssignNode node)
    {
        GenerateExpression(node.Value);
        EmitStoreVar(node.Name);
    }

    protected override void EmitLoadVar(string name)
    {
        // 获取类型感知的加载指令
        string typeName = _varTypes.TryGetValue(name, out string? vt) ? vt : "int";
        ExpType expType = TypeNameToExpType(typeName);
        OpCode loadOp = SelectLoadOp(expType);

        if (symbolTable.TryGetValue(name, out int offset))
        {
            instructions.Add(new Instruction(loadOp,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, MemOff(offset)) },
                instructions.Count));
        }
        else
        {
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
            {
                // 局部表与 `dataSection` 都没有 ⇒ 这个名字**从未声明过**。
                // 此前这里顺手建个初值 0 的槽就当成全局 ——
                // `int a = 1; int b = a + nosuch;` 编得过、运行期静静按 0 算出个错答案。
                ReportUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                EmitUndefinedFallback();
                return;
            }
            instructions.Add(new Instruction(loadOp,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                instructions.Count));
        }
    }

    /// <summary>类型名 → load opcode</summary>
    private static OpCode SelectLoadOp(ExpType expType) => expType switch
    {
        ExpType.F32 => OpCode.MOVEF,
        ExpType.F64 => OpCode.MOVED,
        ExpType.I64 => OpCode.MOVEL,
        _ => OpCode.MOVE,
    };

    /// <summary>类型名 → store opcode</summary>
    private static OpCode SelectStoreOp(ExpType expType) => expType switch
    {
        ExpType.F32 => OpCode.MOVEF,
        ExpType.F64 => OpCode.MOVED,
        ExpType.I64 => OpCode.MOVEL,
        _ => OpCode.MOVE,
    };

    protected override void EmitStoreVar(string name)
    {
        string typeName = _varTypes.TryGetValue(name, out string? vt) ? vt : "int";
        ExpType expType = TypeNameToExpType(typeName);
        OpCode storeOp = SelectStoreOp(expType);

        if (symbolTable.TryGetValue(name, out int offset))
        {
            instructions.Add(new Instruction(storeOp,
                new List<Operand> { new Operand(OperandType.MEMORY, MemOff(offset)), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
        }
        else
        {
            if (!symbolTable.ContainsKey(name) && !dataSection.ContainsKey($"var_{name}"))
            {
                var vi = Vars!.AllocLocal(name, expType == ExpType.I64 || expType == ExpType.F64 ? 8 : 4);
                symbolTable[name] = -vi.Offset;
                instructions.Add(new Instruction(storeOp,
                    new List<Operand> { new Operand(OperandType.MEMORY, Vars.FormatOffset(name)), new Operand(OperandType.REGISTER, 0) },
                    instructions.Count));
                return;
            }
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;
            instructions.Add(new Instruction(storeOp,
                new List<Operand> { new Operand(OperandType.MEMORY, dataLabel), new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
        }
    }

    private void GenerateSwitch(SwitchNode node)
    {
        // Use StatementManager.EmitSwitchCustom for full expression-based case values
        var emitCaseValues = new List<System.Action>();
        var caseBodies = new List<System.Action>();
        System.Action? defaultBody = null;

        foreach (var caseNode in node.Cases)
        {
            if (caseNode.Value == null)
            {
                defaultBody = () =>
                {
                    foreach (var stmt in caseNode.Body)
                        GenerateStatement(stmt);
                };
            }
            else
            {
                var capCase = caseNode;
                emitCaseValues.Add(() => GenerateExpression(capCase.Value!));
                caseBodies.Add(() =>
                {
                    foreach (var stmt in capCase.Body)
                        GenerateStatement(stmt);
                });
            }
        }

        Sta!.EmitSwitchCustom(
            emitSwitchValue: () => GenerateExpression(node.Expression),
            emitCaseValues: emitCaseValues,
            caseBodies: caseBodies,
            defaultBody: defaultBody
        );
    }
}
