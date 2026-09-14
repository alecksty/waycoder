using VMLAssembler;
using CompilerBase;

namespace ObjCCompiler;

public partial class CodeGenerator
{
    private void GenerateExpression(ASTNode node)
    {
        switch (node)
        {
            case LiteralNode literal: GenerateLiteral(literal); break;
            case VarNode v: EmitLoadVar(v.Name); break;
            case BinaryNode b: GenerateBinary(b); break;
            case UnaryNode u: GenerateUnary(u); break;
            case CallNode call: GenerateCall(call); break;
            case MsgSendNode msg: GenerateMsgSend(msg); break;
            case ObjCBoxedNode boxed: GenerateBoxed(boxed); break;
            case CastNode cast: GenerateCast(cast); break;
            case ObjCBlockNode block: GenerateBlock(block); break;
            case MemberAccessNode ma: GenerateMemberAccess(ma); break;
            default: throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    private void GenerateLiteral(LiteralNode node) => EmitLoadConstant(node.Value);

    private void GenerateBinary(BinaryNode node)
    {
        // = 赋值：先算右值，再算左值地址，最后 STORE
        if (node.Op == "=")
        {
            GenerateExpression(node.Right);            // R0 = right value
            instructions.Add(new Instruction(OpCode.PUSH,
                [new Operand(OperandType.REGISTER, 0)],
                instructions.Count));                   // PUSH value
            GenerateAddressOf(node.Left);               // R0 = left address
            instructions.Add(new Instruction(OpCode.POP,
                [new Operand(OperandType.REGISTER, 1)],
                instructions.Count));                   // POP R1 = value
            // Select store opcode based on field type (for struct member access)
            OpCode storeOp = node.Left is MemberAccessNode ma
                ? SelectFieldStoreOp(GetStructFieldType(ResolveStructType(ma.Object), ma.MemberName))
                : OpCode.MOVE;
            instructions.Add(new Instruction(storeOp,
                [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)],
                instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)],
                instructions.Count));                   // R0 = value (return assigned value)
            return;
        }
        // [] 数组/指针读取：arr[i] 或 ptr[i] → 根据元素类型选择指令
        if (node.Op == "[]")
        {
            int elemSize = 4; bool isPtr = false; bool isStructField = false;
            if (node.Left is VarNode vn && varTypes.TryGetValue(vn.Name, out var vtype))
            {
                isPtr = vtype.Contains("*") && !vtype.Contains("[") && !vtype.Contains("(");
                if (vtype.Contains("char")) elemSize = 1;
                else if (vtype.Contains("short")) elemSize = 2;
            }
            // Struct array field: s.arr[i] — use ADDRESS not VALUE as base
            if (node.Left is MemberAccessNode ma)
            {
                isStructField = true;
                GenerateAddressOf(ma);                // R0 = &s.arr[0] (address)
                string? st = ResolveStructType(ma.Object);
                string? ft = GetStructFieldType(st, ma.MemberName);
                if (ft != null)
                {
                    if (ft.Contains("char")) elemSize = 1;
                    else if (ft.Contains("short")) elemSize = 2;
                }
            }
            else
                GenerateExpression(node.Left);       // R0 = base (address or value)
            instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
            GenerateExpression(node.Right);          // R0 = index
            if (elemSize == 2)
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), Reg(0), Imm(1)]));
            else if (elemSize >= 4)
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), Reg(0), Imm(2)]));
            if (!isPtr && !isStructField)            // VML array has +4 header; struct field arrays don't
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), Imm(4)]));
            instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
            instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), Reg(1)]));
            OpCode loadOp = elemSize == 1 ? OpCode.MOVEB : elemSize == 2 ? OpCode.MOVEH : OpCode.MOVE;
            instructions.Add(new Instruction(loadOp,
                [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            return;
        }
        var leftType = InferExpType(node.Left);
        var rightType = InferExpType(node.Right);
        var left = ExpVar.Eval(leftType, () => GenerateExpression(node.Left));
        var right = ExpVar.Eval(rightType, () => GenerateExpression(node.Right));
        switch (node.Op)
        {
            case "+": _expr!.EmitBinOp(left, right, "+"); break;
            case "-": _expr!.EmitBinOp(left, right, "-"); break;
            case "*": _expr!.EmitBinOp(left, right, "*"); break;
            case "/": _expr!.EmitBinOp(left, right, "/"); break;
            case "%": _expr!.EmitBinOp(left, right, "%"); break;
            case "==": _expr!.EmitCmp(left, right, "=="); break;
            case "!=": _expr!.EmitCmp(left, right, "!="); break;
            case "<": _expr!.EmitCmp(left, right, "<"); break;
            case ">": _expr!.EmitCmp(left, right, ">"); break;
            case "<=": _expr!.EmitCmp(left, right, "<="); break;
            case ">=": _expr!.EmitCmp(left, right, ">="); break;
            case "&": _expr!.EmitBinOp(left, right, "&"); break;
            case "|": _expr!.EmitBinOp(left, right, "|"); break;
            case "^": _expr!.EmitBinOp(left, right, "^"); break;
            case "<<": _expr!.EmitBinOp(left, right, "<<"); break;
            case ">>": _expr!.EmitBinOp(left, right, ">>"); break;
            case "&&": _expr!.EmitAnd(left, right); break;
            case "||": _expr!.EmitOr(left, right); break;
            case "?":
                // Ternary: node is (cond ? then) : else — nested BinaryNode
                // This is the inner ? node, just evaluate the condition
                _expr!.EmitCmp(left, right, "!=");
                break;
            case ":":
                // Ternary else branch: left is (cond ? then), right is else
                // left.Left=cond, left.Right=then, right=elseExpr
                if (node.Left is BinaryNode inner && inner.Op == "?")
                {
                    EmitTernary(
                        () => GenerateExpression(inner.Left),
                        () => GenerateExpression(inner.Right),
                        () => GenerateExpression(node.Right));
                }
                break;
            case "+=":
            case "-=":
            case "*=":
            case "/=":
                _expr!.EmitBinOp(left, right, node.Op[0].ToString()); break;
        }
    }

    /// <summary>
    /// 计算表达式的地址（用于赋值左值、取地址等）
    /// *ptr → ptr值(即地址); var → R12±offset; arr[i] → base+idx*size
    /// </summary>
    private void GenerateAddressOf(ASTNode node)
    {
        switch (node)
        {
            case VarNode v:
                // 局部变量地址: R12 + offset
                if (symbolTable.TryGetValue(v.Name, out int offset))
                {
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 12)],
                        instructions.Count));
                    if (offset > 0)
                        instructions.Add(new Instruction(OpCode.SUB,
                            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, offset)],
                            instructions.Count));
                    else if (offset < 0)
                        instructions.Add(new Instruction(OpCode.ADD,
                            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -offset)],
                            instructions.Count));
                }
                else
                {
                    // 全局变量: LEA label
                    string label = $"var_{v.Name}";
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label)],
                        instructions.Count));
                }
                break;
            case UnaryNode u when u.Op == "*":
                // *ptr — 指针解引用的地址就是ptr的值
                GenerateExpression(u.Operand);
                break;
            case MemberAccessNode ma:
                // struct.field: &base + field_offset
                string? st = ResolveStructType(ma.Object);
                int off = GetStructFieldOffset(st, ma.MemberName);
                if (ma.IsPointer)
                    GenerateExpression(ma.Object);
                else
                    GenerateAddress(ma.Object);
                if (off > 0)
                    instructions.Add(new Instruction(OpCode.ADD,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, off)],
                        instructions.Count));
                break;
            case BinaryNode b when b.Op == "[]":
                {
                // arr[i] — 计算数组元素地址
                int addrElemSize = 4; bool addrIsPtr = false; bool addrIsStruct = false;
                if (b.Left is VarNode avn && varTypes.TryGetValue(avn.Name, out var avtype))
                {
                    addrIsPtr = avtype.Contains("*") && !avtype.Contains("[") && !avtype.Contains("(");
                    if (avtype.Contains("char")) addrElemSize = 1;
                    else if (avtype.Contains("short")) addrElemSize = 2;
                }
                if (b.Left is MemberAccessNode)
                {
                    addrIsStruct = true;
                    GenerateAddressOf(b.Left);            // R0 = &s.arr[0] (address)
                }
                else
                    GenerateExpression(b.Left);           // R0 = base
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(b.Right);
                if (addrElemSize == 2)
                    instructions.Add(new Instruction(OpCode.SHL, [Reg(0), Reg(0), Imm(1)]));
                else if (addrElemSize >= 4)
                    instructions.Add(new Instruction(OpCode.SHL, [Reg(0), Reg(0), Imm(2)]));
                if (!addrIsPtr && !addrIsStruct)
                    instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), Imm(4)]));
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), Reg(1)]));
                break;
                }
            default:
                // 其他表达式: 先算值（假设值是地址）
                GenerateExpression(node);
                break;
        }
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-": _expr!.EmitNeg(operand); break;
            case "!": _expr!.EmitNot(operand); break;
            case "~": _expr!.EmitNot(operand); break;
            case "++": GenerateExpression(node.Operand); AddRI(OpCode.ADD, 0, 1); break;
            case "--": GenerateExpression(node.Operand); AddRI(OpCode.SUB, 0, 1); break;
            case "*": GenerateExpression(node.Operand); instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")], instructions.Count)); break;
            case "&": GenerateAddressOf(node.Operand); break;
            default: GenerateExpression(node.Operand); break;
        }
    }

    private void GenerateCall(CallNode node)
    {
        // Inline print functions — push arg to stack, then CALL print_str/print_int
        // print_* functions expect arg on stack (cleanup via add R13 #8 in epilogue)
        if (node.Name == "print_int" || node.Name == "NSLog")
        {
            foreach (var arg in node.Arguments)
            {
                bool isString = arg is LiteralNode lit && lit.Value is string
                    || (arg is CallNode call && IsStringReturningFunc(call.Name));
                bool isFloat = arg is LiteralNode flit && (flit.Value is float || flit.Value is double);
                string func = isFloat ? "print_float" : (isString ? "print_str" : "print_int");
                GenerateExpression(arg);
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, func)]));
            }
            return;
        }

        // printf — call the real printf implementation (cdecl, caller cleanup)
        if (node.Name == "printf")
        {
            // Push args right-to-left for cdecl convention
            int totalArgBytes = 0;
            for (int i = node.Arguments.Count - 1; i >= 0; i--)
            {
                var arg = node.Arguments[i];
                GenerateExpression(arg);
                var (size, isFloat, isDouble, isLong) = GetArgTypeInfo(arg);
                EmitPushArg(size, isFloat, isDouble, isLong);
                totalArgBytes += size;
            }
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "printf")]));
            // Caller cleanup (cdecl)
            if (totalArgBytes > 0)
                instructions.Add(new Instruction(OpCode.ADD,
                    [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgBytes)]));
            return;
        }

        // Block pointer indirect call: f 是 block 指针变量 (类型以 "(^)" 结尾)
        if (varTypes.TryGetValue(node.Name, out var vt) && vt.EndsWith("(^)"))
        {
            // 从右到左压栈参数 (块遵循 cdecl 约定, 与本地函数一致)
            for (int i = node.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(node.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
            }
            // 加载函数指针到 R0, 间接调用 CALL R0
            GenerateExpression(new VarNode(node.Name, node.Line, node.Column));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
            // 调用者清理 (cdecl)
            int blockArgBytes = node.Arguments.Count * 4;
            if (blockArgBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, blockArgBytes)], instructions.Count));
            }
            return;
        }

        // Check if this is a locally-defined function (has a body)
        bool isLocalFunc = false;
        if (_allStatements != null)
        {
            foreach (var stmt in _allStatements)
            {
                if (stmt is FuncDeclNode fn && fn.Name == node.Name && fn.Body != null)
                {
                    isLocalFunc = true;
                    break;
                }
            }
        }

        if (isLocalFunc)
        {
            // cdecl: 从右到左压栈, 调用者清理
            for (int i = node.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(node.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
            }
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, node.Name)], instructions.Count));

            // Clean up arguments from stack after CALL (caller-cleanup convention)
            int argBytes = node.Arguments.Count * 4;
            if (argBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD,
                    [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argBytes)], instructions.Count));
            }
        }
        else
        {
            // External functions use stdcall convention (callee cleans stack)
            // Push arguments right-to-left using EmitPushArg for type-aware stack writes
            for (int i = node.Arguments.Count - 1; i >= 0; i--)
            {
                var arg = node.Arguments[i];
                GenerateExpression(arg);
                var (size, isFloat, isDouble, isLong) = GetArgTypeInfo(arg);
                EmitPushArg(size, isFloat, isDouble, isLong);
            }
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, node.Name)], instructions.Count));
            // No caller cleanup — callee cleans its own stack (stdcall)
        }
    }

    /// <summary>Determine argument size and type for EmitPushArg</summary>
    private (int size, bool isFloat, bool isDouble, bool isLong) GetArgTypeInfo(ASTNode arg)
    {
        if (arg is LiteralNode lit)
        {
            if (lit.Value is float) return (4, true, false, false);
            if (lit.Value is double) return (8, false, true, false);
            if (lit.Value is long) return (8, false, false, true);
        }
        if (arg is VarNode vn && varTypes.TryGetValue(vn.Name, out var vt))
        {
            if (vt == "float") return (4, true, false, false);
            if (vt == "double") return (8, false, true, false);
            if (vt == "long" || vt == "long long") return (8, false, false, true);
        }
        if (arg is CastNode cast)
        {
            if (cast.TargetType == "float") return (4, true, false, false);
            if (cast.TargetType == "double") return (8, false, true, false);
            if (cast.TargetType == "long" || cast.TargetType == "long long") return (8, false, false, true);
        }
        return (4, false, false, false);
    }

    private void GenerateMsgSend(MsgSendNode node)
    {
        // push arguments first
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            GenerateExpression(node.Arguments[i]);
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
        }
        // push receiver
        GenerateExpression(node.Receiver);
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));

        string methodName = node.Method.Replace(":", "_");
        string label;
        // Native method: use bare name (no objc_ prefix)
        if (_nativeMethods.Contains(methodName))
        {
            label = methodName;
        }
        // Check for super dispatch: [super method:]
        else if (node.Receiver is VarNode { Name: "super" } && !string.IsNullOrEmpty(currentSuperClass))
        {
            label = $"objc_{currentSuperClass}_{methodName}";
        }
        else
        {
            label = $"objc_{methodName}";
        }
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, label)], instructions.Count));
    }

    /// <summary>Boxed expression @(expr) — 相当于 (id)expr，MCU 模式直接求值</summary>
    private void GenerateBoxed(ObjCBoxedNode node)
    {
        GenerateExpression(node.Expr);
    }

    /// <summary>C-style cast: (int)expr, (float)expr, etc.</summary>
    private void GenerateCast(CastNode node)
    {
        var srcType = InferExpType(node.Expr);
        GenerateExpression(node.Expr);
        var dstType = StrToObjCType(node.TargetType);
        var dstInfo = TypeInfo(dstType);
        _expr!.EmitConversion(srcType.ByteSize(), srcType.IsFloat(), srcType.IsDouble(),
                               dstInfo.byteSize, dstInfo.isFloat, dstInfo.isDouble,
                               srcType.IsLong(), dstInfo.isLong);
    }

    /// <summary>
    /// 将 R0 中的表达式结果从源类型隐式转换到目标变量类型。
    /// 关键: 字面量 100000 被解析为 int, 直接 MOVE 只写 32 位 registers[0]，
    /// 不更新 64 位 longRegisters[0] (L0)，后续 MOVEL 存储/长整数运算会读到 0。
    /// 因此赋值给 long/double/float 变量前必须显式 I2L/I2D/I2F 等转换。
    /// </summary>
    private void EmitTypeCoerce(ASTNode value, string targetType)
    {
        if (string.IsNullOrEmpty(targetType)) return;
        // struct/union/数组/指针 类型不做标量转换
        if (targetType.StartsWith("struct ") || targetType.StartsWith("union ")) return;
        if (targetType.Contains('*')) return;

        var src = InferExpType(value);
        var dst = StrToObjCType(targetType);
        var di = TypeInfo(dst);
        _expr!.EmitConversion(src.ByteSize(), src.IsFloat(), src.IsDouble(),
                              di.byteSize, di.isFloat, di.isDouble,
                              src.IsLong(), di.isLong);
    }

    /// <summary>ObjC block literal: ^int(int x){ return x*2; } — function pointer mode (no capture)</summary>
    private static int _blockCounter = 0;
    private void GenerateBlock(ObjCBlockNode node)
    {
        string blockLabel = $"__objc_block_{++_blockCounter}";
        string skipLabel = $"__objc_block_skip_{_blockCounter}";

        // Jump over the function body
        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, skipLabel)], instructions.Count));

        // Block entry point label
        AddLabel(blockLabel);
        EmitPrologue();

        var savedSymbols = new Dictionary<string, int>(symbolTable);
        int savedOffset = nextStackOffset;

        // Parameters are above BP after prologue + CALL overhead
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

        // Load block function address into R0 (as a function pointer value)
        // MOVE reg, label — 统一 LEA (取标签地址), 而非 MOVEL (64位内存读取)
        instructions.Add(new Instruction(OpCode.MOVE, [
            new Operand(OperandType.REGISTER, 0),
            new Operand(OperandType.LABEL, blockLabel)
        ], instructions.Count));
    }

    /// <summary>Struct member access: obj.field or ptr->field</summary>
    private void GenerateMemberAccess(MemberAccessNode node)
    {
        string? structType = ResolveStructType(node.Object);
        int offset = GetStructFieldOffset(structType, node.MemberName);
        string? fieldType = GetStructFieldType(structType, node.MemberName);

        if (node.IsPointer)
            GenerateExpression(node.Object);  // ptr value → R0
        else
            GenerateAddress(node.Object);    // &struct → R0

        // ADD field offset to base address
        if (offset > 0)
            instructions.Add(new Instruction(OpCode.ADD,
                [new Operand(OperandType.REGISTER, 0),
                 new Operand(OperandType.IMMEDIATE, offset)], instructions.Count));

        // Load value from [R0] with correct instruction for field type
        OpCode loadOp = SelectFieldLoadOp(fieldType);
        instructions.Add(new Instruction(loadOp,
            [new Operand(OperandType.REGISTER, 0),
             new Operand(OperandType.MEMORY, "R0")], instructions.Count));
    }

    /// <summary>Select load opcode based on struct field type</summary>
    private static OpCode SelectFieldLoadOp(string? fieldType) => fieldType switch
    {
        "char" or "unsigned char" => OpCode.MOVEB,
        "short" or "unsigned short" => OpCode.MOVEH,
        "long" or "long long" => OpCode.MOVEL,
        "float" => OpCode.MOVEF,
        "double" => OpCode.MOVED,
        _ => OpCode.MOVE
    };

    /// <summary>Select store opcode based on struct field type</summary>
    private static OpCode SelectFieldStoreOp(string? fieldType) => fieldType switch
    {
        "char" or "unsigned char" => OpCode.MOVEB,
        "short" or "unsigned short" => OpCode.MOVEH,
        "long" or "long long" => OpCode.MOVEL,
        "float" => OpCode.MOVEF,
        "double" => OpCode.MOVED,
        _ => OpCode.MOVE
    };

    /// <summary>Resolve the struct type name for a variable expression</summary>
    private string? ResolveStructType(ASTNode obj)
    {
        if (obj is VarNode vn && _variableStructTypes.TryGetValue(vn.Name, out var st)) return st;
        return null;
    }

    /// <summary>Get field offset within a struct</summary>
    private int GetStructFieldOffset(string? structTypeName, string fieldName)
    {
        if (structTypeName == null) return 0;
        string search = structTypeName.StartsWith("struct ") ? structTypeName[7..] : structTypeName;
        foreach (var kvp in _structDefs)
        {
            if (string.Equals(kvp.Key, search, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var f in kvp.Value.Fields)
                    if (f.Name == fieldName) return f.Offset;
                return 0;
            }
        }
        return 0;
    }

    /// <summary>Get field type name within a struct</summary>
    private string? GetStructFieldType(string? structTypeName, string fieldName)
    {
        if (structTypeName == null) return null;
        string search = structTypeName.StartsWith("struct ") ? structTypeName[7..] : structTypeName;
        foreach (var kvp in _structDefs)
        {
            if (string.Equals(kvp.Key, search, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var f in kvp.Value.Fields)
                    if (f.Name == fieldName) return f.Type;
                return null;
            }
        }
        return null;
    }

    /// <summary>Generate base address of a variable (for struct member access)</summary>
    private void GenerateAddress(ASTNode node)
    {
        if (node is VarNode vn)
        {
            if (symbolTable.TryGetValue(vn.Name, out int off))
            {
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 12)],
                    instructions.Count));
                if (off > 0)
                    instructions.Add(new Instruction(OpCode.SUB,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, off)],
                        instructions.Count));
                else if (off < 0)
                    instructions.Add(new Instruction(OpCode.ADD,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -off)],
                        instructions.Count));
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.LABEL, $"var_{vn.Name}")], instructions.Count));
            }
        }
        else
        {
            GenerateExpression(node); // hope it's already an address in R0
        }
    }
}
