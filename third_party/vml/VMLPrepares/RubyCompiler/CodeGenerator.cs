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

        // First pass: collect module methods for include inlining
        foreach (var stmt in program.Statements)
        {
            if (stmt is ModuleNode mod)
                CollectModuleMethods(mod);
        }

        foreach (var stmt in program.Statements)
            GenerateStatement(stmt);

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
