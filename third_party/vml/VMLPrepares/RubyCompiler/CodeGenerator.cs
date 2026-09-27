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

    /// <summary>
    /// 变量名 → 寄存器**类**（`I32` / `F64`）。
    ///
    /// <para>
    /// ⚠ Ruby 前端此前**一点类型推断都没有** —— `WrapExpr` 恒返回 `ExpType.I32`，
    /// 于是 `3.14 * 2.0 * 100` 是拿 **32 位整数乘**去算的（值却在 `D0`/`F0` 里）
    /// ⇒ 实测打出 **956301400**（浮点位型）一类垃圾；`puts(...)` 也只按整数打。
    /// </para>
    /// <para>
    /// Ruby 只有两种数值：`Integer`（任意精度）与 `Float`（= IEEE 双精度）。
    /// 本平台的做法与 C#/Go/Python 一致：**超出 int32 的整数走双精度路径**
    /// （2^53 以内精确），于是"数值"只剩两档：`I32` 与 `F64`。
    /// </para>
    /// <para>
    /// 变量表**按函数作用域**保存/还原（与 `symbolTable`、`_stringVars` 同一处），
    /// 否则函数体内给 `i` 记的类型会漏到顶层同名的 `i` 上。
    /// </para>
    /// </summary>
    internal Dictionary<string, ExpType> _varTypes = new();

    /// <summary>
    /// 推断表达式的寄存器类。**唯一一份** —— `WrapExpr`（运算）与
    /// `NoteVarType`（变量表）都走它。
    /// </summary>
    internal ExpType InferType(ASTNode node) => node switch
    {
        null => ExpType.I32,
        // 字面量：Ruby 的 Float 就是双精度；整数**超出 int32 也按双精度**（见 `_varTypes` 注释）
        LiteralNode lit when lit.Value is double or float => ExpType.F64,
        LiteralNode lit when lit.Value is long l && (l < int.MinValue || l > int.MaxValue) => ExpType.F64,
        // 变量：查表（查不到按整数 —— 与原来的口径一致，不会比改之前更差）
        VarNode v when _varTypes.TryGetValue(v.Name, out var vt) => vt,
        // 一元：`!x` 是布尔、取负/取反跟随操作数
        UnaryNode u when u.Op == "!" => ExpType.I32,
        UnaryNode u => InferType(u.Operand),
        // 二元：比较类恒为布尔；算术**浮点优先**
        BinaryNode b when b.Op is "==" or "!=" or "<" or ">" or "<=" or ">=" or "<=>" or "&&" or "||" => ExpType.I32,
        BinaryNode b => (InferType(b.Left) == ExpType.F64 || InferType(b.Right) == ExpType.F64)
                        ? ExpType.F64 : ExpType.I32,
        // 下标读出来的元素类型不知道（Ruby 数组是异质的）⇒ 按整数（保守）
        IndexNode => ExpType.I32,
        // 调用结果：`to_f` 明确是浮点；其余按整数（与改之前一致）
        CallNode c when c.Method is "to_f" or "fdiv" or "Float" => ExpType.F64,
        _ => ExpType.I32,
    };

    /// <summary>把「这个名字现在装什么类」记进 <see cref="_varTypes"/>（赋值/形参处调）。</summary>
    internal void NoteVarType(string name, ASTNode value)
    {
        if (InferType(value) == ExpType.F64) _varTypes[name] = ExpType.F64;
        else _varTypes.Remove(name);
    }

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

    // ── 变量读写按**类**选指令（基类的三个钩子）─────────────────────────────
    //
    // ⚠ 基类的 `EmitLoadVar`/`EmitStoreVar` 已经会按这三个钩子取指令、并按类取寄存器号，
    //   这里只要回答"这个名字是浮点吗"。**不要另写一套读写** ——
    //   那正是本仓头号坑（同一规则两处实现）。
    protected override OpCode GetVarLoadOp(string name)
        => _varTypes.TryGetValue(name, out var t) && t == ExpType.F64 ? OpCode.MOVED : OpCode.MOVE;
    protected override OpCode GetVarStoreOp(string name) => GetVarLoadOp(name);
    protected override int GetNewVarSize(string name)
        => _varTypes.TryGetValue(name, out var t) && t == ExpType.F64 ? 8 : 4;

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
        // ⚠ 类由 `InferType` 给（原先**恒 `ExpType.I32`**）—— 见 `_varTypes` 的注释：
        //   浮点表达式按整数算，值却在 D0/F0 里，打出来是位型。
        return ExpVar.Eval(InferType(node), () => GenerateExpression(node));
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
