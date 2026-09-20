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

    /// <summary>
    /// 已知装着**字符串**的变量名。
    ///
    /// 为什么需要它：`puts`/`print` 在 VML 里有两条完全不同的实现
    /// （`print_str` 收地址、`print_int` 收数值），而值本身**没有类型标记** ——
    /// 选哪一条只能靠编译期判断。原先的判据是「字符串字面量 或 名字像返回串的函数」，
    /// **变量一律不算** ⇒ `s = "VAR"; puts(s)` 走整数那条，**把地址打了出来**
    /// （实测 1024 而不是 VAR；`f1("LIT")` 里 `puts(a)` 同理）。
    ///
    /// 这里按「赋值来源」记一笔：字面量串/返回串的调用 → 记；赋了别的 → 取消。
    /// 保守方向是**取消**（认成整数）—— 认错成整数只是打印难看的数字，
    /// 认错成字符串则会拿一个小整数当地址去解引用。
    /// </summary>
    internal HashSet<string> _stringVars = new();

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
    [System.Obsolete("应改用 GenerateCode(ProgramNode)", true)]
    public override VmlProgram GenerateCode() => throw new System.NotSupportedException("应改用 GenerateCode(ProgramNode)");
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

        // 预扫描：`def f(s) … puts(s) … end` 里那个 `s` 是不是字符串，**在函数体生成的时候
        // 是看不出来的**（Ruby 没有类型标注，而调用点可能还在后面）。所以先整棵 AST 走一遍，
        // 把「哪个函数在第几个实参位上收到过字符串」记下来，生成函数体时据此标记形参。
        // 不做这一步的话 `def shout(m) puts(m) end; shout("HI")` 会把**地址**打出来。
        CollectStringArgPositions(program.Statements);

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

    /// <summary>函数名 → 已知传过**字符串**的实参下标集合（见 <see cref="CollectStringArgPositions"/>）。</summary>
    private readonly Dictionary<string, HashSet<int>> _stringArgPos = new();

    /// <summary>
    /// 函数名 → 已知传过**非字符串字面量**（数字/浮点/true/false/nil）的实参下标集合。
    ///
    /// 为什么要单独记反证：一个形参只有**一个槽**，而**不同调用点可以传不同类型**
    /// （`f1(7)` 与 `f1("LIT")` 同时存在是合法的 Ruby）。静态判不了这种情形 ——
    /// 认成字符串，整数那次就会拿 7 当地址去解引用（实测打出**空行**）；
    /// 认成整数，字符串那次打出地址。**两个都不对**。
    /// 所以只在「所有已知调用点一致」时才标记字符串，混合时退回原来的整数口径
    /// —— 保证不比改之前更差。
    ///
    /// 传递「非字符串」的证据**只认字面量**：变量不算 —— 它的类型我们多半也还不知道，
    /// 把「未知」当成「非字符串」会让 `s = "x"; f(s)` 这种最常见的写法反而判不出来。
    /// </summary>
    private readonly Dictionary<string, HashSet<int>> _nonStringArgPos = new();

    /// <summary>
    /// 预扫描整棵 AST，记下「哪个函数在第几个实参位上收到过字符串字面量/返回串的调用」。
    /// 生成形参时据此把它标成字符串（<see cref="_stringVars"/>），`puts(形参)` 才会走对实现。
    /// </summary>
    private void CollectStringArgPositions(List<ASTNode> nodes)
    {
        foreach (var n in nodes) CollectStringArgPositions(n);
    }

    private static void AddPos(Dictionary<string, HashSet<int>> map, string func, int pos)
    {
        if (!map.TryGetValue(func, out var set)) map[func] = set = new HashSet<int>();
        set.Add(pos);
    }

    /// <summary>形参 <paramref name="i"/> 能不能认定是字符串：见过字符串、且**没见过**非字符串。</summary>
    private bool ParamIsString(string func, int i)
        => _stringArgPos.TryGetValue(func, out var s) && s.Contains(i)
           && !(_nonStringArgPos.TryGetValue(func, out var n) && n.Contains(i));

    private void CollectStringArgPositions(ASTNode? node)
    {
        switch (node)
        {
            case null: return;
            case ProgramNode p: CollectStringArgPositions(p.Statements); return;
            case DefNode d: CollectStringArgPositions(d.Body); return;
            case ModuleNode m: CollectStringArgPositions(m.Body); return;
            case IfNode i:
                CollectStringArgPositions(i.Condition); CollectStringArgPositions(i.ThenBody);
                if (i.ElseBody is not null) CollectStringArgPositions(i.ElseBody);
                return;
            case WhileNode w: CollectStringArgPositions(w.Condition); CollectStringArgPositions(w.Body); return;
            case ForNode f: CollectStringArgPositions(f.From); CollectStringArgPositions(f.To); CollectStringArgPositions(f.Body); return;
            case ReturnNode r: CollectStringArgPositions(r.Value); return;
            case AssignNode a: CollectStringArgPositions(a.Value); return;
            case OpAssignNode oa: CollectStringArgPositions(oa.Value); return;
            case BinaryNode b: CollectStringArgPositions(b.Left); CollectStringArgPositions(b.Right); return;
            case UnaryNode u: CollectStringArgPositions(u.Operand); return;
            case TernaryNode t:
                CollectStringArgPositions(t.Condition); CollectStringArgPositions(t.TrueExpr); CollectStringArgPositions(t.FalseExpr); return;
            case ArrayNode arr: CollectStringArgPositions(arr.Elements); return;
            case IndexNode ix: CollectStringArgPositions(ix.Target); CollectStringArgPositions(ix.Index); return;
            case IndexAssignNode ia: CollectStringArgPositions(ia.Target); CollectStringArgPositions(ia.Index); CollectStringArgPositions(ia.Value); return;
            case IndexOpAssignNode ioa: CollectStringArgPositions(ioa.Target); CollectStringArgPositions(ioa.Index); CollectStringArgPositions(ioa.Value); return;
            case StringInterpolateNode si: CollectStringArgPositions(si.Parts); return;
            case CaseNode cs:
                CollectStringArgPositions(cs.Condition);
                foreach (var wc in cs.WhenClauses)
                {
                    CollectStringArgPositions(wc.Values);
                    CollectStringArgPositions(wc.Body);
                }
                if (cs.ElseBody is not null) CollectStringArgPositions(cs.ElseBody);
                return;
            case BeginRescueNode br:
                CollectStringArgPositions(br.Body);
                foreach (var rc in br.RescueClauses) CollectStringArgPositions(rc.Body);
                if (br.EnsureBody is not null) CollectStringArgPositions(br.EnsureBody);
                if (br.ElseBody is not null) CollectStringArgPositions(br.ElseBody);
                return;
            case RaiseNode rn: CollectStringArgPositions(rn.Expression); return;
            case CallNode c:
                for (int i = 0; i < c.Arguments.Count; i++)
                {
                    var arg = c.Arguments[i];
                    if (arg is LiteralNode { Value: string }
                        || (arg is CallNode inner && IsStringReturningFunc(inner.Method)))
                        AddPos(_stringArgPos, c.Method, i);
                    else if (arg is LiteralNode)      // 数字/浮点/true/false/nil 字面量 = 反证
                        AddPos(_nonStringArgPos, c.Method, i);
                    CollectStringArgPositions(arg);
                }
                CollectStringArgPositions(c.Receiver);
                return;
        }
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
