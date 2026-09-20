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
    [System.Obsolete("应改用 GenerateCode(ProgramNode)", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("应改用 GenerateCode(ProgramNode)");
#pragma warning restore CS0809

    public VmlProgram GenerateCode(ProgramNode program)
    {
        AddLabel("main");

        // stack frame prologue
        EmitPrologue();

        // 顶层也要**预留局部变量栈帧**（此前只 EmitPrologue，没有 SUB R13）。
        // 局部变量按 [R12-4]、[R12-8]… 分配，而 R12 == R13 ⇒ 那些槽位全在 SP **之下**，
        // 任何 PUSH / CALL（压返回地址）都会把它们原地写花 —— 表现是循环跑一两轮就乱、
        // 或读回 0（`(0-1)*4 = -4` ⇒ 地址 FFFFFFFC 的内存越界）。
        // 帧大小要等语句生成完才知道（变量是边生成边分配的），故先占位、最后回填。
        int framePatchIndex = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], framePatchIndex));

        foreach (var stmt in program.Statements)
            GenerateStatement(stmt);

        // 回填帧大小（+8 安全边界，与 EmitPrologueWithFrame 的口径一致）
        int frameSize = nextStackOffset + 8;
        instructions[framePatchIndex] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)], framePatchIndex);

        // exit program
        EmitExit();

        return BuildProgram("main");
    }

    private ExpVar WrapExpr(ASTNode node)
    {
        return ExpVar.Eval(ExpType.I32, () => GenerateExpression(node));
    }

}
