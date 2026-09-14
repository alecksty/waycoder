using VMLAssembler;
using CompilerBase;

namespace DCompiler;

public partial class CodeGenerator : OopCodeGenerator
{
    private Dictionary<string, int> symbolTable = null!;
    private int nextStackOffset;

    /// <summary>变量类型追踪: varName → typeName (int/float/double/long)</summary>
    private Dictionary<string, string> _varTypes = new();
    /// <summary>函数返回类型追踪: funcName → returnTypeName</summary>
    private Dictionary<string, string> _funcReturnTypes = new();
    /// <summary>函数参数类型追踪: funcName → [(paramName, paramType)] (用于类型感知的参数压栈)</summary>
    private Dictionary<string, List<(string name, string type)>> _funcParamTypes = new();

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, int>();
        _varOffsets = symbolTable;
        nextStackOffset = 0;
    }

    protected override OpCode GetVarLoadOp(string name)
    {
        string tn = _varTypes.TryGetValue(name, out var vt) ? vt : "int";
        return TypeNameToExpType(tn) switch
        {
            ExpType.F32 => OpCode.MOVEF, ExpType.F64 => OpCode.MOVED,
            ExpType.I64 => OpCode.MOVEL, _ => OpCode.MOVE
        };
    }
    protected override OpCode GetVarStoreOp(string name) => GetVarLoadOp(name);
    protected override int GetNewVarSize(string name)
    {
        string tn = _varTypes.TryGetValue(name, out var vt) ? vt : "int";
        var et = TypeNameToExpType(tn);
        return (et == ExpType.I64 || et == ExpType.F64) ? 8 : 4;
    }
    protected override int AllocAndRegisterVar(string name, int size)
    {
        var vi = Vars!.AllocLocal(name, size);
        symbolTable[name] = -vi.Offset;
        return -vi.Offset;
    }

#pragma warning disable CS0809
    [System.Obsolete("Use GenerateCode(ProgramNode) instead", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use GenerateCode(ProgramNode) instead");
#pragma warning restore CS0809

    public VmlProgram GenerateCode(ProgramNode program)
    {
        AddLabel("main");
        EmitPrologue();
        bool hasMainFunc = false;
        foreach (var stmt in program.Statements)
        {
            if (stmt is FuncDefNode f && f.Name == "main")
                hasMainFunc = true;
            GenerateStatement(stmt);
        }

        // If main() was defined as a function, CALL it so it actually executes
        if (hasMainFunc)
        {
            instructions.Add(new Instruction(OpCode.CALL,
                new List<Operand> { new Operand(OperandType.LABEL, "func_main") }, instructions.Count));
        }

        EmitExit();

        return BuildProgram("main");
    }

    /// <summary>类型名称 → ExpType 映射</summary>
    private static ExpType TypeNameToExpType(string typeName) => typeName switch
    {
        "float" => ExpType.F32,
        "double" => ExpType.F64,
        "long" => ExpType.I64,
        _ => ExpType.I32,
    };

    /// <summary>从 AST 节点推断表达式类型</summary>
    private ExpType InferNodeType(ASTNode node) => node switch
    {
        LiteralNode lit => lit.Value switch
        {
            float => ExpType.F32,
            double => ExpType.F64,
            long => ExpType.I64,
            _ => ExpType.I32,
        },
        VarNode varNode => _varTypes.TryGetValue(varNode.Name, out string? vt) ? TypeNameToExpType(vt) : ExpType.I32,
        CallNode call => _funcReturnTypes.TryGetValue(call.Function, out var rt) ? TypeNameToExpType(rt) : ExpType.I32,
        CastExpr cast => TypeNameToExpType(cast.TargetType),
        // 二元运算: 类型提升规则 (double 优先于 float 优先于 long 优先于 int)
        BinaryNode bin => PromoteType(InferNodeType(bin.Left), InferNodeType(bin.Right)),
        UnaryNode un => InferNodeType(un.Operand),
        _ => ExpType.I32,
    };

    /// <summary>二元运算类型提升: 高精度类型优先</summary>
    private static ExpType PromoteType(ExpType a, ExpType b)
    {
        int Rank(ExpType t) => t switch
        {
            ExpType.F64 => 4,
            ExpType.F32 => 3,
            ExpType.I64 => 2,
            _ => 1,
        };
        return Rank(a) >= Rank(b) ? a : b;
    }

    /// <summary>按命名约定推断 conv 库函数的首个参数类型 (float_to_str→float 等)</summary>
    private static ExpType? ExpectedArgType(string funcName)
    {
        var f = funcName.ToLower();
        if (f.StartsWith("float_") || f.StartsWith("floatto") || f == "ftoa") return ExpType.F32;
        if (f.StartsWith("double_") || f.StartsWith("doubleto") || f == "dtoa") return ExpType.F64;
        if (f.StartsWith("long_") || f.StartsWith("ulong_") || f.StartsWith("longto") || f == "ltoa") return ExpType.I64;
        return null;
    }

    private ExpVar WrapExpr(ASTNode node)
    {
        var expType = InferNodeType(node);
        return ExpVar.Eval(expType, () => GenerateExpression(node));
    }
}
