using VMLAssembler;
using CompilerBase;
using System.Collections.Generic;

namespace ObjCCompiler;

public partial class CodeGenerator
{
    private void GenerateStatement(ASTNode node)
    {
        switch (node)
        {
            case FuncDeclNode func: GenerateFuncDecl(func); break;
            case VarDeclNode varDecl: GenerateVarDecl(varDecl); break;
            case ReturnNode ret: GenerateReturn(ret); break;
            case IfNode ifNode: GenerateIf(ifNode); break;
            case WhileNode w: GenerateWhile(w); break;
            case DoWhileNode dw: GenerateDoWhile(dw); break;
            case ForNode f: GenerateFor(f); break;
            case SwitchNode sw: GenerateSwitch(sw); break;
            case BreakNode: Sta!.EmitBreak(); break;
            case ContinueNode: Sta!.EmitContinue(); break;
            case AssignNode assign: GenerateAssign(assign); break;
            case ObjCInterfaceNode iface:
                // Record class → superclass mapping for super dispatch
                if (!string.IsNullOrEmpty(iface.Name) && !string.IsNullOrEmpty(iface.SuperClass))
                    classRegistry[iface.Name] = iface.SuperClass;
                break; // skip - interfaces are metadata
            case ObjCProtocolNode _: break; // @protocol — metadata only, no codegen in MCU mode
            case ObjCImplNode impl: GenerateImpl(impl); break;
            case ObjCMethodNode method: GenerateMethod(method); break;
            case ObjCPropertyNode _: break; // skip — interface metadata
            case ObjCSynthesizeNode synth: GenerateSynthesize(synth); break;
            case ObjCDynamicNode _: break; // @dynamic — accessors provided at runtime, no codegen needed
            case GotoNode gt:
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, $"goto_{gt.Label}")], instructions.Count));
                break;
            case LabelNode lbl:
                AddLabel($"goto_{lbl.Name}");
                break;
            case AsmStatement asmStmt:
                // 内联汇编: 直接发射 ASM 指令, 由运行时逐行解析执行
                instructions.Add(new Instruction(OpCode.ASM,
                    [new Operand(OperandType.IMMEDIATE, asmStmt.Code)], instructions.Count));
                break;
            case ObjCTryNode t: GenerateTry(t); break;
            case ObjCThrowNode th: GenerateThrow(th); break;
            case ObjCForEachNode fe: GenerateForEach(fe); break;
            case ObjCSynchronizedNode sync: GenerateSynchronized(sync); break;
            case ObjCAutoreleasepoolNode ap: GenerateAutoreleasepool(ap); break;
            case BlockNode block:
                foreach (var stmt in block.Statements)
                    GenerateStatement(stmt);
                break;
            default: GenerateExpression(node); break;
        }
    }

    private void GenerateFuncDecl(FuncDeclNode node)
    {
        // native C 函数: 标签已由链接器提供，跳过函数体
        if (node.IsNative)
        {
            _nativeMethods.Add(node.Name);
            return;
        }

        string funcLabel = node.Name;
        string skipLabel = $"skip_func_{node.Name}";
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, skipLabel)], instructions.Count));

        AddLabel(funcLabel);
        EmitPrologue();

        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        funcParams[node.Name] = node.Parameters;

        // Parameters are above BP. Prologue: PUSH R15(4), PUSH R12(4), MOVE R12,R13
        // + CALL pushed return_addr(4). First param at BP+12 → offset=-12 (R12+12).
        int paramOffset = -12;
        foreach (var (pt, pn) in node.Parameters)
        {
            symbolTable[pn] = paramOffset;
            varTypes[pn] = pt;
            int paramSize = (pt == "long" || pt == "double" || pt == "long long") ? 8 : 4;
            paramOffset -= paramSize;
        }

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        EmitEpilogue();
        AddLabel(skipLabel);

        symbolTable = savedSymbols;
        nextStackOffset = savedOffset;
        _varOffsets = symbolTable;
    }

    private void GenerateVarDecl(VarDeclNode node)
    {
        // Track variable type
        varTypes[node.Name] = node.Type;
        // Track struct/union type for member access
        if (node.Type.StartsWith("struct ") || node.Type.StartsWith("union "))
            _variableStructTypes[node.Name] = node.Type;

        // 数组声明: int arr[N] — 使用基类共享方法分配 VML 数组
        if (node.ArraySize > 0)
        {
            AllocateVmlArray(node.Name, node.ArraySize);
            return;
        }

        if (node.Init != null)
        {
            // 分配局部变量（先于初始化求值，确保 EmitStoreVar 能找到符号表条目）
            if (!symbolTable.ContainsKey(node.Name))
            {
                int varSize = GetVarTypeSize(node.Name);
                nextStackOffset += varSize;
                symbolTable[node.Name] = nextStackOffset;
                // 立即分配栈空间，防止 PUSH 等操作覆盖已分配的局部变量
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, varSize)]));
            }
            GenerateExpression(node.Init);
            EmitTypeCoerce(node.Init, node.Type);
            EmitStoreVar(node.Name);
        }
        else
        {
            if (!symbolTable.ContainsKey(node.Name))
            {
                int varSize = GetVarTypeSize(node.Name);
                nextStackOffset += varSize;
                symbolTable[node.Name] = nextStackOffset;
                // 立即分配栈空间
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, varSize)]));
            }
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

    private void GenerateWhile(WhileNode node) =>
        Sta!.EmitWhile(
            emitCondition: () => GenerateExpression(node.Condition),
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });

    private void GenerateDoWhile(DoWhileNode node) =>
        Sta!.EmitDoWhile(
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); },
            emitCondition: () => GenerateExpression(node.Condition));

    private void GenerateFor(ForNode node) =>
        Sta!.EmitFor(
            emitInit: node.Init != null ? () => GenerateStatement(node.Init) : null,
            emitCondition: node.Condition != null ? () => GenerateExpression(node.Condition) : null,
            emitIncrement: node.Update != null ? () => GenerateStatement(node.Update) : null,
            emitBody: () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });

    private void GenerateSwitch(SwitchNode node)
    {
        string endLabel = $"switch_end_{labelCounter++}";
        var caseLabels = new List<(ASTNode? val, string label)>();

        GenerateExpression(node.Expr);
        // 分配临时变量（先于存储，确保 EmitStoreVar 能找到符号表条目）
        if (!symbolTable.ContainsKey("__switch_tmp"))
        {
            nextStackOffset += 4;
            symbolTable["__switch_tmp"] = nextStackOffset;
        }
        EmitStoreVar("__switch_tmp");

        foreach (var (caseVal, _) in node.Cases)
        {
            string caseLabel = $"case_{labelCounter++}";
            caseLabels.Add((caseVal, caseLabel));
            if (caseVal != null)
            {
                // Load switch expr, compare with case val
                EmitLoadVar("__switch_tmp");
                GenerateExpression(caseVal);
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)], instructions.Count));
                instructions.Add(new Instruction(OpCode.JE, [new Operand(OperandType.LABEL, caseLabel)], instructions.Count));
            }
        }

        // Default fallthrough: jump to end (or first default case)
        foreach (var (caseVal, caseLabel) in caseLabels)
        {
            if (caseVal == null)
            {
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, caseLabel)], instructions.Count));
                break;
            }
        }
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)], instructions.Count));

        // Generate case bodies
        for (int i = 0; i < node.Cases.Count; i++)
        {
            var (_, label) = caseLabels[i];
            AddLabel(label);
            foreach (var stmt in node.Cases[i].body)
                GenerateStatement(stmt);
        }

        AddLabel(endLabel);
    }

    private void GenerateAssign(AssignNode node)
    {
        GenerateExpression(node.Value);
        if (varTypes.TryGetValue(node.Name, out var assignTargetType))
            EmitTypeCoerce(node.Value, assignTargetType);
        EmitStoreVar(node.Name);
    }

    private void GenerateImpl(ObjCImplNode node)
    {
        // Track current class for super dispatch
        var prevClassName = currentClassName;
        var prevSuperClass = currentSuperClass;
        currentClassName = node.Name;
        classRegistry.TryGetValue(node.Name, out string? sc);
        currentSuperClass = sc ?? "";

        foreach (var method in node.Methods)
            GenerateStatement(method);

        currentClassName = prevClassName;
        currentSuperClass = prevSuperClass;
    }

    private void GenerateMethod(ObjCMethodNode node)
    {
        string methodName = node.Name.Replace(":", "_");

        // native 方法: 注册到外部方法集，跳过函数体
        if (node.IsNative)
        {
            _nativeMethods.Add(methodName);
            return;
        }

        string label = $"objc_{methodName}";
        string skipLabel = $"skip_{label}";
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, skipLabel)], instructions.Count));

        // Emit class-scoped label first (for super dispatch), then regular label
        if (!string.IsNullOrEmpty(currentClassName))
            AddLabel($"objc_{currentClassName}_{methodName}");
        AddLabel(label);
        EmitPrologue();

        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;
        funcParams[node.Name] = node.Parameters;

        // self is first implicit param in R12
        symbolTable["self"] = 0;

        // Explicit params are above BP, after prologue + CALL overhead (12 bytes)
        int paramOffset = -12;
        foreach (var (_, pn) in node.Parameters)
        {
            symbolTable[pn] = paramOffset;
            paramOffset -= 4;
        }

        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        EmitEpilogue();
        AddLabel(skipLabel);

        symbolTable = savedSymbols;
        nextStackOffset = savedOffset;
    }

    /// <summary>@try { } @catch { } @finally { } — MCU 模式生成 try 体，跳过 catch</summary>
    private void GenerateTry(ObjCTryNode node)
    {
        // MCU 模式：执行 try 体，catch 块不生成（无异常机制），@finally 在 try 后执行
        foreach (var stmt in node.TryBody)
            GenerateStatement(stmt);
        // Warn about skipped catch blocks
        foreach (var (excType, excName, _) in node.Catches)
        {
            string catchInfo = excName != null ? $"@catch({excName})" : "@catch(...)";
            VMLPlugins.WarningEmitter.Emit("objc",
                $"MCU 模式: {catchInfo} 块被跳过（MCU 无异常处理机制）");
        }
        if (node.FinallyBody != null)
        {
            foreach (var stmt in node.FinallyBody)
                GenerateStatement(stmt);
        }
    }

    /// <summary>@throw expr; — MCU 模式生成 SYSCALL exit + 警告</summary>
    private void GenerateThrow(ObjCThrowNode node)
    {
        VMLPlugins.WarningEmitter.Emit("objc",
            "MCU 模式: @throw 转换为 SYSCALL exit（MCU 无异常处理机制）");
        if (node.Expr != null)
            GenerateExpression(node.Expr);
        EmitExit();
    }

    /// <summary>for (type var in collection) { body } — 遍历循环</summary>
    private void GenerateForEach(ObjCForEachNode node)
    {
        // 简化实现：分配变量，CALL 消息发送 count/objectAtIndex 遍历
        // 实际 MCU 模式用标准数组遍历
        string idxVar = $"__fe_idx_{labelCounter}";
        string countVar = $"__fe_cnt_{labelCounter}";
        string collectionVar = $"__fe_coll_{labelCounter}";
        int baseOffset = nextStackOffset;

        nextStackOffset += 4;
        symbolTable[collectionVar] = nextStackOffset;
        nextStackOffset += 4;
        symbolTable[idxVar] = nextStackOffset;
        nextStackOffset += 4;
        symbolTable[countVar] = nextStackOffset;

        // store collection
        GenerateExpression(node.Collection);
        EmitStoreVar(collectionVar);

        // int idxVar = 0;
        AddRI(OpCode.MOVE, 0, 0);
        EmitStoreVar(idxVar);

        // int countVar = [collection count];
        EmitLoadVar(collectionVar);
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "objc_count")], instructions.Count));
        EmitStoreVar(countVar);

        string loopStart = $"foreach_loop_{labelCounter++}";
        string loopEnd = $"foreach_end_{labelCounter}";

        AddLabel(loopStart);
        EmitLoadVar(idxVar);
        EmitLoadVar(countVar);
        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)], instructions.Count));
        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, loopEnd)], instructions.Count));

        // node.varName = [collection objectAtIndex:idxVar];
        // Implicit to store into the iteration var
        EmitLoadVar(collectionVar);
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
        EmitLoadVar(idxVar);
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "objc_objectAtIndex:")], instructions.Count));
        // 分配变量（先于存储，确保 EmitStoreVar 能找到符号表条目）
        if (!symbolTable.ContainsKey(node.VarName))
        {
            nextStackOffset += 4;
            symbolTable[node.VarName] = nextStackOffset;
        }
        EmitStoreVar(node.VarName);

        // body
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);

        // idxVar++
        EmitLoadVar(idxVar);
        AddRI(OpCode.ADD, 0, 1);
        EmitStoreVar(idxVar);
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopStart)], instructions.Count));

        AddLabel(loopEnd);

        // restore stack
        nextStackOffset = baseOffset;
    }

    /// <summary>@synchronized(expr) { body } — MCU 模式视为普通代码块</summary>
    private void GenerateSynchronized(ObjCSynchronizedNode node)
    {
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);
    }

    /// <summary>@autoreleasepool { body } — MCU 模式视为普通代码块</summary>
    private void GenerateAutoreleasepool(ObjCAutoreleasepoolNode node)
    {
        foreach (var stmt in node.Body)
            GenerateStatement(stmt);
    }

    /// <summary>@synthesize propName = ivarName → 生成 getter + setter</summary>
    private void GenerateSynthesize(ObjCSynthesizeNode synth)
    {
        string prop = synth.PropertyName;
        string ivar = synth.IvarName;
        string capProp = char.ToUpper(prop[0]) + prop.Substring(1);

        // 确保 ivar 在符号表中
        if (!symbolTable.ContainsKey(ivar))
        {
            nextStackOffset += 4;
            symbolTable[ivar] = nextStackOffset;
        }

        // getter: -(type)propName { EmitLoadVar(ivarName); RET; }
        string getLabel = $"objc_{prop}";
        string getSkip = $"skip_{getLabel}";
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, getSkip)], instructions.Count));
        AddLabel(getLabel);
        EmitPrologue();
        EmitLoadVar(ivar);
        instructions.Add(new Instruction(OpCode.RET, [], instructions.Count));
        AddLabel(getSkip);

        // setter: -(void)setPropName:(type)value { EmitStoreVar(ivar); RET; }
        string setLabel = $"objc_set{capProp}_";
        string setSkip = $"skip_{setLabel}";
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, setSkip)], instructions.Count));
        AddLabel(setLabel);
        EmitPrologue();
        EmitStoreVar(ivar);
        instructions.Add(new Instruction(OpCode.RET, [], instructions.Count));
        AddLabel(setSkip);
    }
}
