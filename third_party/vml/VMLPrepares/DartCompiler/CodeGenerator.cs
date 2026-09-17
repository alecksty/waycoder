using VMLAssembler;
using CompilerBase;

namespace DartCompiler;

public partial class CodeGenerator : OopCodeGenerator
{
    private Dictionary<string, (string type, int offset)> symbolTable = null!;
    private int nextStackOffset;
    private Dictionary<string, ClassDeclNode> _mixinDefs = new();
    internal HashSet<string> _externalMethods = new();

    public string SourceDirectory { get; set; } = ".";

    public CodeGenerator() : base()
    {
        symbolTable = new Dictionary<string, (string type, int offset)>();
        nextStackOffset = 0;
    }

#pragma warning disable CS0809
    [System.Obsolete("Use GenerateCode(ProgramNode) instead", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use GenerateCode(ProgramNode) instead");
#pragma warning restore CS0809

    public VmlProgram GenerateCode(ProgramNode program)
    {
        AddLabel("main");
        EmitPrologue();

        // 顶层也要**预留局部变量栈帧**（原来只有 EmitPrologue，没有 SUB R13）。
        // 局部变量按 [R12-4]、[R12-8]… 分配，而 R12 == R13 ⇒ 那些槽位全在 SP **之下**，
        // 任何 PUSH / CALL（压返回地址）都会把它们原地写花：实测 `int s = 7; int t = inc(3);`
        // 打印出 `S=33 T=61`（应为 7 / 4）。帧大小要等语句生成完才知道（变量边生成边分配），
        // 故先占位、最后回填 —— 与 R 前端同一套做法。
        int mainFramePatch = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), Imm(0)], mainFramePatch));

        // First pass: collect mixin definitions for inlining
        foreach (var stmt in program.Statements)
        {
            if (stmt is ClassDeclNode cd && cd.IsMixin)
            {
                _mixinDefs[cd.Name] = cd;
            }
        }

        // First pass: collect all function definitions and class methods, emit them
        // Second pass: emit top-level statements into main
        bool hasMainFunc = false;
        foreach (var stmt in program.Statements)
        {
            if (stmt is MethodDeclNode method)
            {
                if (method.Name == "main") hasMainFunc = true;
                GenerateMethod(method);
            }
            else if (stmt is ClassDeclNode classDecl && !classDecl.IsMixin)
            {
                GenerateClass(classDecl);
            }
            else if (stmt is BlockNode block && block.Statements.Count > 0)
            {
                // Extension methods desugar to BlockNode of MethodDeclNodes
                foreach (var bs in block.Statements)
                {
                    if (bs is MethodDeclNode bm) { GenerateMethod(bm); }
                    else GenerateStatement(bs);
                }
            }
            else
            {
                GenerateStatement(stmt);
            }
        }

        // 回填顶层帧大小（+8 安全边界，与 R 前端/EmitPrologueWithFrame 口径一致）
        int mainFrameSize = nextStackOffset + 8;
        instructions[mainFramePatch] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), Imm(mainFrameSize)], mainFramePatch);

        // If main() was defined, CALL it so it actually executes
        if (hasMainFunc)
        {
            instructions.Add(new Instruction(OpCode.CALL,
                new List<Operand> { new Operand(OperandType.LABEL, "func_main") }, instructions.Count));
        }

        EmitExit();

        return BuildProgram("main");
    }

    private ExpVar WrapExpr(ASTNode node)
    {
        return ExpVar.Eval(ExpType.I32, () => GenerateExpression(node));
    }

    private void GenerateClass(ClassDeclNode node)
    {
        // Collect all methods: mixin methods first, then class's own methods override
        // Dart linearization: later mixins in 'with' chain override earlier ones
        var allMethods = new Dictionary<string, MethodDeclNode>();

        // Step 1: Inline methods from mixins (later mixins override earlier)
        foreach (var mixinName in node.MixinNames)
        {
            if (_mixinDefs.TryGetValue(mixinName, out var mixinDef))
            {
                foreach (var member in mixinDef.Members)
                {
                    if (member is MethodDeclNode mixinMethod)
                    {
                        allMethods[mixinMethod.Name] = mixinMethod;
                    }
                }
            }
        }

        // Step 2: Class's own methods override mixin methods
        foreach (var member in node.Members)
        {
            if (member is MethodDeclNode classMethod)
            {
                allMethods[classMethod.Name] = classMethod;
            }
        }

        // Generate all collected methods with class-prefixed name
        foreach (var method in allMethods.Values)
        {
            var renamed = new MethodDeclNode(
                $"{node.Name}_{method.Name}",
                method.ReturnType,
                method.Parameters,
                method.Body,
                method.Line,
                method.Column);
            GenerateMethod(renamed, node.Name);
        }
    }
}
