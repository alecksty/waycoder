using VMLAssembler;
using CompilerBase;

namespace RubyCompiler;

public partial class CodeGenerator
{
    private void GenerateStatement(ASTNode node)
    {
        switch (node)
        {
            case DefNode def:
                GenerateDef(def);
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
            case CaseNode caseNode:
                GenerateCase(caseNode);
                break;
            case AssignNode assign:
                GenerateAssign(assign);
                break;
            case OpAssignNode opAssign:
                GenerateOpAssign(opAssign);
                break;
            case ModuleNode mod:
                // Collect methods for include inlining (already done in first pass)
                foreach (var s in mod.Body) GenerateStatement(s);
                break;
            case BeginRescueNode beginRescue:
                GenerateBeginRescue(beginRescue);
                break;
            case RaiseNode raiseNode:
                if (HandleMCUThrow(() => AddRI(OpCode.MOVE, 0, -1)))
                    break;
                else
                {
                    if (raiseNode.Expression != null) GenerateExpression(raiseNode.Expression);
                    else instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.THROW, [Reg(0)]));
                }
                break;
            case IncludeNode inc:
                // Inline module methods into current class (MCU compile-time mixin)
                if (_currentClassName != null && _moduleMethods.TryGetValue(inc.ModuleName, out var modMethods))
                {
                    foreach (var method in modMethods)
                    {
                        // Re-emit module method in current class scope (func_ label)
                        GenerateDef(method);
                    }
                }
                break;
            case CallNode call:
                GenerateExpression(call);
                break;
            default:
                GenerateExpression(node);
                break;
        }
    }

    private void GenerateDef(DefNode node)
    {
        // native 方法：注册到外部方法集，调用时使用裸名 CALL
        if (node.IsNative)
        {
            _nativeMethods.Add(node.Name);
            return;
        }

        string funcLabel = node.Name.StartsWith("self.") ? $"class_{node.Name.Replace("self.", "")}" : $"func_{node.Name}";

        // Track class context for include inlining
        string? prevClassName = _currentClassName;
        if (node.Name.StartsWith("self."))
            _currentClassName = node.Name.Replace("self.", "");

        // jump over function body
        string skipLabel = $"skip_{funcLabel}";
        instructions.Add(new Instruction(OpCode.JMP,
            new List<Operand> { new Operand(OperandType.LABEL, skipLabel) }, instructions.Count));

        AddLabel(funcLabel);

        // function prologue
        EmitPrologue();

        // save old symbol table, set up parameters
        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        int numParams = node.Parameters.Count;
        for (int i = 0; i < numParams; i++)
            symbolTable[node.Parameters[i]] = -(12 + i * 4);

        // generate body
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // function epilogue
        EmitEpilogue();
        AddLabel(skipLabel);

        // restore symbol table + class context
        symbolTable = savedSymbols;
        nextStackOffset = savedOffset;
        _currentClassName = prevClassName;
    }

    private void GenerateReturn(ReturnNode node)
        => EmitReturn(node.Value != null ? () => GenerateExpression(node.Value) : null);

    private void GenerateIf(IfNode node)
    {
        Sta!.EmitIf(
            emitCondition: () => GenerateExpression(node.Condition),
            emitThen: () => { foreach (var s in node.ThenBody) GenerateStatement(s); },
            emitElse: node.ElseBody != null ? () => { foreach (var s in node.ElseBody) GenerateStatement(s); } : null);
    }

    private void GenerateWhile(WhileNode node)
    {
        Sta!.EmitWhile(
            emitCondition: () => GenerateExpression(node.Condition),
            emitBody: () => { foreach (var s in node.Body) GenerateStatement(s); });
    }

    private void GenerateCase(CaseNode node)
    {
        // store condition value in a temp global
        string caseTemp = $"case_{labelCounter}";
        GenerateExpression(node.Condition);
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.MEMORY, caseTemp), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));
        dataSection[caseTemp] = 0;

        string endLabel = $"endcase_{labelCounter++}";
        var nextLabels = new List<string>();

        foreach (var when in node.WhenClauses)
        {
            string bodyLabel = $"when_{labelCounter++}";
            string nextLabel = $"nextwhen_{labelCounter++}";
            nextLabels.Add(nextLabel);

            foreach (var val in when.Values)
            {
                // reload case value into R1, evaluate when value into R0, compare
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, caseTemp) },
                    instructions.Count));
                GenerateExpression(val);
                instructions.Add(new Instruction(OpCode.CMP,
                    new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
                    instructions.Count));
                instructions.Add(new Instruction(OpCode.JE,
                    new List<Operand> { new Operand(OperandType.LABEL, bodyLabel) }, instructions.Count));
            }

            instructions.Add(new Instruction(OpCode.JMP,
                new List<Operand> { new Operand(OperandType.LABEL, nextLabel) }, instructions.Count));

            AddLabel(bodyLabel);

            foreach (var stmt in when.Body)
                GenerateStatement(stmt);

            instructions.Add(new Instruction(OpCode.JMP,
                new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

            AddLabel(nextLabel);
        }

        if (node.ElseBody != null)
        {
            foreach (var stmt in node.ElseBody)
                GenerateStatement(stmt);
        }

        AddLabel(endLabel);
    }

    private void GenerateFor(ForNode node)
    {
        // for var in from..to — 手动实现（EmitFor 固定 CMP R0,#0;JZ 不兼容区间判断）
        GenerateExpression(node.From);
        EmitStoreVar(node.Var);

        string startLabel = $"for_{labelCounter++}";
        string endLabel = $"forend_{labelCounter++}";

        AddLabel(startLabel);
        EmitLoadVar(node.Var);
        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        GenerateExpression(node.To);
        instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        EmitLoadVar(node.Var);
        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
        EmitStoreVar(node.Var);
        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, startLabel) }, instructions.Count));

        AddLabel(endLabel);
    }

    private void GenerateAssign(AssignNode node)
    {
        GenerateExpression(node.Value);
        EmitStoreVar(node.Name);
    }

    private void GenerateOpAssign(OpAssignNode node)
    {
        EmitLoadVar(node.Name);
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
            _ => OpCode.ADD,
        };
        instructions.Add(new Instruction(op,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));

        EmitStoreVar(node.Name);
    }

    private void GenerateBeginRescue(BeginRescueNode node)
    {
        if (HandleMCUTry(() => { foreach (var s in node.Body) GenerateStatement(s); if (node.EnsureBody != null) foreach (var s in node.EnsureBody) GenerateStatement(s); }))
            return;
        string catchLabel = $"catch_{labelCounter}";
        string endTryLabel = $"endtry_{labelCounter++}";
        instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
        foreach (var s in node.Body) GenerateStatement(s);
        if (node.ElseBody != null)
            foreach (var s in node.ElseBody) GenerateStatement(s);
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endTryLabel)]));
        labels[catchLabel] = instructions.Count;
        foreach (var rc in node.RescueClauses)
        {
            if (rc.Variable != null)
                EmitStoreVar(rc.Variable); // exception value in R0 → store to rescue variable
            foreach (var s in rc.Body) GenerateStatement(s);
            instructions.Add(new Instruction(OpCode.ENDCATCH, []));
        }
        if (node.EnsureBody != null)
            foreach (var s in node.EnsureBody) GenerateStatement(s);
        labels[endTryLabel] = instructions.Count;
    }
}
