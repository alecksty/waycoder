using VMLAssembler;
using CompilerBase;

namespace RubyCompiler;

public partial class CodeGenerator : OopCodeGenerator
{
    private Dictionary<string, int> symbolTable = null!;
    private int nextStackOffset;
    private Dictionary<string, List<DefNode>> _moduleMethods = new();
    private string? _currentClassName = null;
    internal HashSet<string> _nativeMethods = new();

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, int>();
        _varOffsets = symbolTable;
        nextStackOffset = 0;
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

        // stack frame prologue (使用基类方法)
        EmitPrologue();

        // 顶层也要**预留局部变量栈帧**（原来只有 EmitPrologue，没有 SUB R13）。
        // 局部变量按 [R12-4]、[R12-8]… 分配，而 R12 == R13 ⇒ 那些槽位全在 SP **之下**，
        // 任何 PUSH / CALL 都会把它们原地写花（`a = [1,2,3,4]` + `plus1(a[i])` 就是这种形状）。
        // 帧大小要等语句生成完才知道（变量边生成边分配），故先占位、最后回填（与 R 前端同款）。
        int mainFramePatch = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], mainFramePatch));

        // First pass: collect module methods for include inlining
        foreach (var stmt in program.Statements)
        {
            if (stmt is ModuleNode mod)
                CollectModuleMethods(mod);
        }

        foreach (var stmt in program.Statements)
            GenerateStatement(stmt);

        // 回填帧大小（+8 安全边界，与 EmitPrologueWithFrame 口径一致）
        int mainFrameSize = nextStackOffset + 8;
        instructions[mainFramePatch] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, mainFrameSize)], mainFramePatch);

        EmitExit();

        return BuildProgram("main");
    }

    private ExpVar WrapExpr(ASTNode node)
    {
        return ExpVar.Eval(ExpType.I32, () => GenerateExpression(node));
    }

    private void CollectModuleMethods(ModuleNode mod)
    {
        var methods = new List<DefNode>();
        foreach (var s in mod.Body)
        {
            if (s is DefNode def && !def.Name.StartsWith("self."))
                methods.Add(def);
        }
        _moduleMethods[mod.Name] = methods;
    }
}
