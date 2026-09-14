using VMLAssembler;
using CompilerBase;

namespace ObjCCompiler;

public enum ObjCType { Void, Char, Short, Int, Long, Float, Double, Bool, Pointer, Id, Class }

public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
{
    private Dictionary<string, int> symbolTable = null!;
    private Dictionary<string, string> varTypes = null!; // variable name → type string
    private int nextStackOffset;
    private Dictionary<string, List<(string type, string name)>> funcParams = new();
    // ObjC class registry: className → superClassName
    private Dictionary<string, string> classRegistry = new();
    // Current class/superclass being generated (for super dispatch)
    private string currentClassName = "";
    private string currentSuperClass = "";
    // Native methods: use bare name CALL (no objc_ prefix)
    internal HashSet<string> _nativeMethods = new();
    // Struct definitions (copied from Parser after parse)
    internal Dictionary<string, StructDef> _structDefs = new();
    // Variable → struct type mapping (e.g., "p" → "Point")
    private Dictionary<string, string> _variableStructTypes = new();

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, int>();
        _varOffsets = symbolTable;
        varTypes = new Dictionary<string, string>();
        nextStackOffset = 0;
        // InitSimpleCompiler() is called by CLikeCodegen base constructor
    }

    protected override OpCode GetVarLoadOp(string name) => GetLoadOpForVar(name);
    protected override OpCode GetVarStoreOp(string name) => GetStoreOpForVar(name);
    protected override int GetNewVarSize(string name)
    {
        if (varTypes.TryGetValue(name, out var tn))
        {
            if (tn == "long" || tn == "double" || tn == "long long") return 8;
            // struct types: compute total size from field layout
            if (tn.StartsWith("struct "))
            {
                string search = tn[7..]; // strip "struct " prefix
                foreach (var kvp in _structDefs)
                {
                    if (string.Equals(kvp.Key, search, StringComparison.OrdinalIgnoreCase))
                    {
                        int total = 0;
                        foreach (var f in kvp.Value.Fields) total += f.Size;
                        return total > 0 ? total : 4;
                    }
                }
            }
        }
        return 4;
    }
    protected override int AllocAndRegisterVar(string name, int size)
    {
        nextStackOffset += size;
        symbolTable[name] = nextStackOffset;
        return nextStackOffset;
    }
    protected override void EmitLoadVar(string name)
    {
        var loadOp = GetVarLoadOp(name);
        if (_varOffsets != null && _varOffsets.TryGetValue(name, out int offset))
        {
            instructions.Add(new Instruction(loadOp,
                [Reg(0), new Operand(OperandType.MEMORY, MemOff(offset))], instructions.Count));
        }
        else
        {
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel)) dataSection[dataLabel] = 0;
            if (IsVmlArray(name))
                EmitLoadArrayAddress(dataLabel);
            else
                instructions.Add(new Instruction(loadOp,
                    [Reg(0), new Operand(OperandType.MEMORY, dataLabel)], instructions.Count));
        }
    }

    // ====== CLikeCodegen 抽象方法实现 ======
    protected override int GetTypeSizeByEnum(int typeEnum) => ((ObjCType)typeEnum) switch {
        ObjCType.Void => 0, ObjCType.Char => 1, ObjCType.Short => 2, ObjCType.Int => 4,
        ObjCType.Long => 8, ObjCType.Float => 4, ObjCType.Double => 8, ObjCType.Bool => 1,
        ObjCType.Pointer => 4, ObjCType.Id => 4, ObjCType.Class => 4, _ => 4
    };
    protected override bool IsFloatType(int typeEnum) => (ObjCType)typeEnum is ObjCType.Float or ObjCType.Double;
    protected override int GetDefaultType() => (int)ObjCType.Int;

    /// <summary>Get (byteSize, isFloat, isDouble, isLong) for ExpressionManager</summary>
    private static (int byteSize, bool isFloat, bool isDouble, bool isLong) TypeInfo(ObjCType type) => type switch
    {
        ObjCType.Char or ObjCType.Bool => (1, false, false, false),
        ObjCType.Short => (2, false, false, false),
        ObjCType.Int => (4, false, false, false),
        ObjCType.Long => (8, false, false, true),
        ObjCType.Float => (4, true, false, false),
        ObjCType.Double => (8, false, true, false),
        _ => (4, false, false, false)
    };

    /// <summary>将 ObjCType 映射为 ExpType</summary>
    private static ExpType ToExpType(ObjCType type) => type switch
    {
        ObjCType.Long => ExpType.I64,
        ObjCType.Float => ExpType.F32,
        ObjCType.Double => ExpType.F64,
        ObjCType.Char or ObjCType.Bool => ExpType.I8,
        ObjCType.Short => ExpType.I16,
        _ => ExpType.I32
    };

    /// <summary>Map string type name to ObjCType enum</summary>
    private static ObjCType StrToObjCType(string tn) => tn switch
    {
        "float" => ObjCType.Float,
        "double" => ObjCType.Double,
        "long" => ObjCType.Long,
        "long long" => ObjCType.Long,
        "short" => ObjCType.Short,
        "char" => ObjCType.Char,
        _ => ObjCType.Int
    };

    private static ObjCType MapToObjCType(string typeName) => typeName switch {
        "void" => ObjCType.Void, "char" => ObjCType.Char, "short" => ObjCType.Short,
        "int" => ObjCType.Int, "long" => ObjCType.Long, "float" => ObjCType.Float,
        "double" => ObjCType.Double, "bool" => ObjCType.Bool, "id" => ObjCType.Id,
        "Class" => ObjCType.Class, _ => ObjCType.Int
    };

#pragma warning disable CS0809
    [System.Obsolete("Use GenerateCode(ProgramNode) instead", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use GenerateCode(ProgramNode) instead");
#pragma warning restore CS0809

    public VmlProgram GenerateCode(ProgramNode program)
    {
        _allStatements = program.Statements;
        AddLabel("main");

        EmitPrologue();

        bool hasMainFunc = false;
        foreach (var stmt in program.Statements)
        {
            if (stmt is FuncDeclNode f && f.Name == "main")
                hasMainFunc = true;
            GenerateStatement(stmt);
        }

        // If main() was defined, CALL it so it actually executes
        if (hasMainFunc)
        {
            instructions.Add(new Instruction(OpCode.CALL,
                new List<Operand> { new Operand(OperandType.LABEL, "main") }, instructions.Count));
        }

        EmitExit();

        return BuildProgram("main");
    }

    private ExpVar WrapExpr(ASTNode node, string? varName = null) {
        ExpType expType = ExpType.I32;
        if (varName != null && varTypes.TryGetValue(varName, out var tn))
        {
            var ti = TypeInfo(StrToObjCType(tn));
            if (ti.isLong) expType = ExpType.I64;
            else if (ti.isDouble) expType = ExpType.F64;
            else if (ti.isFloat) expType = ExpType.F32;
        }
        return ExpVar.Eval(expType, () => GenerateExpression(node));
    }

    /// <summary>Infer expression type from an AST node (for binary operands)</summary>
    private ExpType InferExpType(ASTNode node)
    {
        if (node is VarNode vn && varTypes.TryGetValue(vn.Name, out var vtn))
        {
            var ti = TypeInfo(StrToObjCType(vtn));
            if (ti.isLong) return ExpType.I64;
            if (ti.isDouble) return ExpType.F64;
            if (ti.isFloat) return ExpType.F32;
        }
        if (node is LiteralNode ln)
        {
            if (ln.Value is long) return ExpType.I64;
            if (ln.Value is float) return ExpType.F32;
            if (ln.Value is double) return ExpType.F64;
        }
        if (node is CallNode call)
        {
            foreach (var stmt in _allStatements ?? [])
            {
                if (stmt is FuncDeclNode fn && fn.Name == call.Name)
                {
                    var ti = TypeInfo(StrToObjCType(fn.ReturnType));
                    if (ti.isLong) return ExpType.I64;
                    if (ti.isDouble) return ExpType.F64;
                    if (ti.isFloat) return ExpType.F32;
                    break;
                }
            }
        }
        if (node is BinaryNode bin && bin.Op != "=" && bin.Op != "[]")
        {
            return ExpressionManager.WidenType(InferExpType(bin.Left), InferExpType(bin.Right));
        }
        if (node is CastNode cast)
        {
            var ti = TypeInfo(StrToObjCType(cast.TargetType));
            if (ti.isLong) return ExpType.I64;
            if (ti.isDouble) return ExpType.F64;
            if (ti.isFloat) return ExpType.F32;
            return ExpType.I32;
        }
        return ExpType.I32;
    }

    private IList<ASTNode>? _allStatements = null;

    /// <summary>Set all program statements for type inference</summary>
    public void SetAllStatements(IList<ASTNode> stmts) { _allStatements = stmts; }

    private OpCode GetLoadOpForVar(string name)
    {
        if (varTypes.TryGetValue(name, out var tn))
        {
            var ti = TypeInfo(StrToObjCType(tn));
            return ExpressionManager.SelectLoadOp(ti.byteSize, ti.isFloat, ti.isDouble, ti.isLong);
        }
        return OpCode.MOVE;
    }
    private OpCode GetStoreOpForVar(string name)
    {
        if (varTypes.TryGetValue(name, out var tn))
        {
            var ti = TypeInfo(StrToObjCType(tn));
            return ExpressionManager.SelectStoreUnifiedOp(ti.byteSize, ti.isFloat, ti.isDouble, ti.isLong);
        }
        return OpCode.MOVE;
    }
    private int GetVarTypeSize(string name)
    {
        if (varTypes.TryGetValue(name, out var tn))
        {
            // struct/union types: compute size from field layout
            if (tn.StartsWith("struct ") || tn.StartsWith("union "))
            {
                string search = tn.StartsWith("struct ") ? tn[7..] : tn[6..];
                foreach (var kvp in _structDefs)
                {
                    if (string.Equals(kvp.Key, search, StringComparison.OrdinalIgnoreCase))
                    {
                        int maxSize = 0;
                        foreach (var f in kvp.Value.Fields)
                        {
                            if (tn.StartsWith("union "))
                            { if (f.Size > maxSize) maxSize = f.Size; }
                            else maxSize += f.Size;
                        }
                        return maxSize > 0 ? maxSize : 4;
                    }
                }
            }
            return GetTypeSizeByEnum((int)StrToObjCType(tn));
        }
        return 4;
    }

}
