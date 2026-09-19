using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace FortranCompiler;

/// <summary>Fortran 类型枚举 — 用于类型感知指令选择</summary>
public enum FortranType { Integer, Long, Real, DoublePrecision, Logical, Character, Complex }

public partial class CodeGenerator : TypedCodeGen<FortranType>
{
    /// <summary>
    /// 源文件里出现过 `implicit none` ⇒ **变量必须先声明**，未声明的引用报**错误**；
    /// 没出现则报**警告**（Fortran 默认的隐式类型是合法语义）。
    ///
    /// 由 <see cref="FortranCompiler"/> 从 `<c>Parser.ImplicitNone</c>` 传进来 ——
    /// 这是**每文件**的属性，不是每语言，所以不能做成 `ImplicitDeclarationAllowed`
    /// 那种编译期常量。
    /// </summary>
    public bool StrictDeclarations { get; set; }

    private Dictionary<string, int> symbolTable = null!;
    private Dictionary<string, (string? type, List<string>? paramsList)> _functionTable = new();
    private Dictionary<string, int> _arraySizes = new();
    /// <summary>变量名 → Fortran 类型映射 (从声明时记录)</summary>
    private Dictionary<string, FortranType> _varTypes = new();
    private int nextStackOffset;

    /// <summary>将 Fortran 类型映射为 (byteSize, isFloat, isDouble, isLong)</summary>
    protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(FortranType type) => type switch
    {
        FortranType.Integer => (4, false, false, false),
        FortranType.Long => (8, false, true, false),  // use double path for 64-bit int (MOVED)
        FortranType.Real => (4, true, false, false),
        FortranType.DoublePrecision => (8, true, true, false),
        FortranType.Logical => (4, false, false, false),
        FortranType.Character => (1, false, false, false),
        FortranType.Complex => (8, true, false, false),
        _ => (4, false, false, false)
    };

    /// <summary>将类型名字符串映射为 FortranType</summary>
    private static FortranType MapTypeName(string name) => name.ToLowerInvariant() switch
    {
        "integer" => FortranType.Integer,
        "long" => FortranType.Long,
        "real" => FortranType.Real,
        "double precision" or "doubleprecision" => FortranType.DoublePrecision,
        "logical" => FortranType.Logical,
        "character" => FortranType.Character,
        "complex" => FortranType.Complex,
        _ => FortranType.Integer // default
    };

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, int>();
        _varOffsets = symbolTable;
        nextStackOffset = 0;
        InitSimpleCompiler();
    }

    protected override string NormalizeVarName(string name) => name.ToLowerInvariant();
    protected override OpCode GetVarLoadOp(string name) => GetLoadInstruction(_varTypes.TryGetValue(name, out var t) ? t : FortranType.Integer);
    protected override OpCode GetVarStoreOp(string name) => GetStoreInstruction(_varTypes.TryGetValue(name, out var t) ? t : FortranType.Integer);
    protected override int GetNewVarSize(string name) => GetTypeInfo(_varTypes.TryGetValue(name, out var t) ? t : FortranType.Integer).byteSize;
    protected override int AllocAndRegisterVar(string name, int size)
    {
        nextStackOffset += size;
        symbolTable[name] = nextStackOffset;
        return nextStackOffset;
    }

#pragma warning disable CS0809
    [System.Obsolete("Use GenerateCode(ProgramNode) instead", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use GenerateCode(ProgramNode) instead");
#pragma warning restore CS0809

    public VmlProgram GenerateCode(ProgramNode program)
    {
        _arraySizes = program.ArraySizes;
        AddLabel("main");

        // Helper: collect functions from a list (including nested module bodies)
        void CollectFunctions(List<ASTNode> stmts)
        {
            foreach (var s in stmts)
            {
                if (s is ModuleNode mod) CollectFunctions(mod.Body);
                else if (s is SubroutineNode sub) _functionTable[sub.Name.ToLowerInvariant()] = (null, sub.Parameters);
                else if (s is FunctionNode func) _functionTable[func.Name.ToLowerInvariant()] = (func.ReturnType, func.Parameters);
            }
        }

        // First pass: collect all subroutine and function names (including those in modules)
        CollectFunctions(program.Statements);

        // First pass over main body: scan variable declarations to compute stack frame size
        int savedNextOffset = nextStackOffset;
        var savedSymbols = new Dictionary<string, int>(symbolTable);
        foreach (var stmt in program.Statements)
        {
            if (stmt is VarDeclNode decl)
                GenerateVarDecl(decl);
        }
        int mainFrameSize = nextStackOffset + 8; // +8 safety margin for PUSH/POP

        // stack frame prologue + reserve local variable space
        EmitPrologueWithFrame(mainFrameSize);

        // Helper
        void GenerateContained(List<ASTNode> stmts)
        {
            foreach (var s in stmts)
            {
                if (s is SubroutineNode sub2) GenerateSubroutine(sub2);
                else if (s is FunctionNode func2) GenerateFunction(func2);
                else if (s is ModuleNode mod2) GenerateContained(mod2.Body);
            }
        }

        // Generate code for top-level statements (skip subroutines/functions/modules)
        bool hasContained = false;
        foreach (var stmt in program.Statements)
        {
            if (stmt is SubroutineNode || stmt is FunctionNode || stmt is ModuleNode)
            {
                hasContained = true;
                continue;
            }
            if (!hasContained)
                GenerateStatement(stmt);
        }

        // Jump over contained subroutines/functions
        string endSkipLabel = $"skip_subs_{labelCounter++}";
        if (hasContained)
        {
            instructions.Add(new Instruction(OpCode.JMP,
                new List<Operand> { new Operand(OperandType.LABEL, endSkipLabel) },
                instructions.Count));
        }

        // Generate contained subroutines and functions (including those in modules)
        GenerateContained(program.Statements);

        if (hasContained)
        {
            AddLabel(endSkipLabel);
        }

        // Exit program
        AddSyscall(3); // exit

        return BuildProgram("main");
    }

    /// <summary>将 FortranType 映射为 ExpType</summary>
    private static ExpType ToExpType(FortranType ft) => ft switch
    {
        FortranType.Real => ExpType.F32,
        FortranType.DoublePrecision or FortranType.Long => ExpType.F64,
        FortranType.Complex => ExpType.F32,
        _ => ExpType.I32
    };

    /// <summary>推导 AST 节点表达式的结果类型</summary>
    private ExpType GetExprType(ASTNode node) => node switch
    {
        LiteralNode lit => lit.TypeName.ToLowerInvariant() switch
        {
            "real" => ExpType.F32,
            "double" => ExpType.F64,
            _ => ExpType.I32
        },
        VarNode vn => ToExpType(_varTypes.TryGetValue(vn.Name.ToLowerInvariant(), out var t) ? t : FortranType.Integer),
        // 数组元素 a(i) 的表达式类型 = 该数组的元素类型
        ArrayElemNode ae => ToExpType(_varTypes.TryGetValue(ae.Name.ToLowerInvariant(), out var et) ? et : FortranType.Integer),
        BinaryNode bn => ExpressionManager.WidenType(GetExprType(bn.Left), GetExprType(bn.Right)),
        UnaryNode un => GetExprType(un.Operand),
        FuncCallNode fc => fc.Name.ToLowerInvariant() switch
        {
            "int" or "ifix" or "idint" => ExpType.I32,
            "real" or "float" or "sngl" => ExpType.F32,
            "dble" or "dfloat" => ExpType.F64,
            _ => ExpType.I32
        },
        AssignNode an => GetExprType(an.Value),
        _ => ExpType.I32
    };

    private ExpVar WrapExpr(ASTNode node)
    {
        return ExpVar.Eval(GetExprType(node), () => GenerateExpression(node));
    }
}
