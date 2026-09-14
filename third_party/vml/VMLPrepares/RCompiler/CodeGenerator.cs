using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace RCompiler;

/// <summary>R 类型枚举 — 用于类型感知指令选择</summary>
public enum RType { Numeric, Integer, Character, Logical, Complex, List, Vector }

public partial class CodeGenerator : TypedCodeGen<RType>
{
    private Dictionary<string, int> symbolTable = null!;
    private int nextStackOffset;

    // Loop context for break/next support
    private string? currentBreakLabel;
    private string? currentNextLabel;

    // 类型辅助方法
    protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(RType t) => t switch
    {
        RType.Numeric => (4, true, false, false), // R numeric = double-precision
        RType.Integer => (4, false, false, false),
        RType.Character => (1, false, false, false),
        RType.Logical => (1, false, false, false),
        RType.Complex => (8, true, false, false), // 2×float
        RType.List or RType.Vector => (4, false, false, false),
        _ => (4, false, false, false)
    };

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, int>();
        _varOffsets = symbolTable;
        nextStackOffset = 0;
        InitSimpleCompiler();
    }

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
        AddLabel("main");

        // stack frame prologue
        EmitPrologue();

        foreach (var stmt in program.Statements)
            GenerateStatement(stmt);

        // exit program
        EmitExit();

        return BuildProgram("main");
    }

    private ExpVar WrapExpr(ASTNode node)
    {
        return ExpVar.Eval(ExpType.I32, () => GenerateExpression(node));
    }

}
