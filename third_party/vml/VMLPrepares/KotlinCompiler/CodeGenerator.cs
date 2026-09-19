using VMLAssembler;
using System.Linq;
using CompilerBase;

namespace KotlinCompiler;
public partial class CodeGenerator : OopCodeGenerator {
    Program _program;
    int nextStrId = 0;
    readonly HashSet<string> _externalFuncs = [];

    // ── 顶层属性（`val` / `var` 写在所有 `fun` 外面）────────────────────────────
    //
    // 它们**不属于任何函数的栈帧**（`_varOffsets` 每进一个函数就 Clear），所以放**数据段**，
    // 由 `main` 开头统一初始化一次。不这么做的话：声明被解析器丢掉（见 Parser.Parse）、
    // 引用处又因为查不到偏移而**不生成任何指令** ⇒ 读到的永远是 0。
    readonly Dictionary<string, string> _globalLabel = new();   // 名字 → 数据段标签
    readonly Dictionary<string, string> _globalTypes = new();   // 名字 → 声明类型
    readonly List<VarDecl> _globalInits = new();
    readonly HashSet<string> _stringGlobals = new();            // 顶层里装着字符串的那几个

    /// <summary>
    /// 当前函数里**已知装着字符串**的变量名。
    ///
    /// 为什么需要它：`println`/`print` 在 VML 里有两条实现（`print_str` 收地址、`print_int` 收数值），
    /// 值本身没有类型标记，只能编译期判。原判据是「字符串字面量 或 名字像返回串的函数」——
    /// **变量一律不算** ⇒ `val s = "abc"; println(s)` 走整数那条，**把地址打了出来**
    /// （实测 1032）。与 Ruby 的 `puts(变量)` 是同一个族。
    /// 每条函数入口清空（局部名会互相遮蔽），顶层那几个另放 <see cref="_stringGlobals"/>。
    /// </summary>
    readonly HashSet<string> _stringVars = new();

    public CodeGenerator(Program program) {
        _program = program;
    }

    public override VmlProgram GenerateCode() {
        // First pass: collect extension functions, interfaces, and class defs
        // 顶层属性先登记（数据段 + 类型），再谈生成函数 —— 函数体里引用它们时要查得到
        foreach (var f in _program.Functions) {
            if (f is not VarDecl gv) continue;
            string glabel = "g_" + gv.Name;
            dataSection[glabel] = 0;
            _globalLabel[gv.Name] = glabel;
            _globalTypes[gv.Name] = gv.Type ?? "Int";
            // ⚠ 就写进 `_arrayVars`（它**不随函数清空**），不要再立一张 `_globalArrayVars` ——
            //   下标那两处判据各有 `_arrayVars.Contains(...)`，两份表就是"改一处忘一处"。
            if (gv.Init is CallExpr ac && ac.Name is "arrayOf" or "listOf" or "mutableListOf")
                _arrayVars.Add(gv.Name);
            if (IsStringInit(gv.Init)) _stringGlobals.Add(gv.Name);
            _globalInits.Add(gv);
        }

        foreach (var f in _program.Functions) {
            if (f is ExtensionDecl ed) {
                _extMethods[$"{ed.ReceiverType}_{ed.Func.Name}"] = ed;
            }
            if (f is InterfaceDecl iface) {
                _interfaces[iface.Name] = iface;
            }
            if (f is ClassDecl cd) {
                _classDefs[cd.Name] = (cd.Name, cd.Props, cd.IsSealed, cd.Interfaces);
                // Register subclass relationships for sealed class exhaustiveness checking
                foreach (var ifName in cd.Interfaces)
                {
                    if (!_sealedSubclasses.ContainsKey(ifName))
                        _sealedSubclasses[ifName] = new List<string>();
                    _sealedSubclasses[ifName].Add(cd.Name);
                }
            }
        }

        foreach (var f in _program.Functions) {
            if (f is ExtensionDecl ed) {
                var fn = ed.Func;
                string label = $"{ed.ReceiverType}_{fn.Name}";
                EmitMethodHeader($"{ed.ReceiverType}.{fn.Name}", "Any",
                    new List<(string, string)> { ("Any", $"this ({ed.ReceiverType})") }
                    .Concat(fn.Parameters.Select(p => ("Any", p))).ToList());
                AddLabel(label);
                _varOffsets.Clear();
                _varTypes.Clear();
                _frameBytes = -1;
                EmitPrologue();
                // First param is the receiver (this)
                int paramSlots = fn.Parameters.Count + 1; // +1 for receiver
                int thisOff = AllocVar("this", "Any");
                int stackOff = 12 + (paramSlots - 1) * 4;
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{stackOff}")]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{thisOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                // Remaining params
                for (int pi = 0; pi < fn.Parameters.Count; pi++) {
                    int off = AllocVar(fn.Parameters[pi], "Int");
                    stackOff = 12 + (paramSlots - 2 - pi) * 4;
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{stackOff}")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{off + 12}"), new Operand(OperandType.REGISTER, 0)]));
                }
                _stringVars.Clear();
                int prologueIdx = instructions.Count;
                GenerateNode(fn.Body);
                int frameSize = _frameBytes < 0 ? 8 : _frameBytes + 12;
                instructions.Insert(prologueIdx, new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, frameSize)]));
                EmitEpilogue();
            }
            else if (f is FunctionDecl fn) {
                if (fn.IsExternal) {
                    _externalFuncs.Add(fn.Name);
                    continue; // 外部函数不生成函数体
                }
                EmitMethodHeader(fn.Name, "Any",
                    fn.Parameters.Select(p => ("Any", p)).ToList());
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, fn.Name)]));
                // Clear var offsets for each function
                _varOffsets.Clear();
                _varTypes.Clear();
                _frameBytes = -1;
                // function prologue
                EmitPrologue();
                // All params are on stack (pushed right-to-left at call site)
                // Stack layout after prologue:
                //   R12+0  = saved R12  (BP)
                //   R12+4  = saved R15  (RA)
                //   R12+8  = return addr
                //   R12+12 = argN-1     (rightmost arg, pushed first)
                //   R12+16 = argN-2
                //   ...
                //   R12+12+(paramSlots-1)*4 = arg0 (leftmost arg, pushed last)
                int paramSlots = fn.Parameters.Count;
                for (int pi = 0; pi < paramSlots; pi++) {
                    int off = AllocVar(fn.Parameters[pi], "Int");
                    // Load param from stack (R12+12+(paramSlots-1-pi)*4) into local var slot
                    int stackOff = 12 + (paramSlots - 1 - pi) * 4;
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{stackOff}")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{off + 12}"), new Operand(OperandType.REGISTER, 0)]));
                }
                _stringVars.Clear();
                int prologueIdx2 = instructions.Count;
                // 顶层属性的初始化**塞进 main 的最前面**（程序从 main 进，这是唯一保证会跑到的地方）。
                // ⚠ 必须在 `_varOffsets.Clear()` **之后** —— 它清的是上一个函数的局部表，
                //   而顶层属性走的是 `_globalLabel`（另一张表），两者互不影响。
                if (fn.Name == "main") EmitGlobalInits();
                GenerateNode(fn.Body);
                // Allocate stack frame for all local vars (+2 for R12/R15 slots)
                int frameSize = _frameBytes < 0 ? 8 : _frameBytes + 12;
                instructions.Insert(prologueIdx2, new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, frameSize)]));
                // Insert shifts all body instructions +1; fix Sta-placed labels in dict
                foreach (var key in labels.Keys.ToList())
                    if (labels[key] >= prologueIdx2)
                        labels[key]++;
                if (fn.Name == "main") {
                    // main: load last user-declared var (skip hidden vars like __for_end_* and this)
                    if (_varOffsets.Count > 0) {
                        var userVars = _varOffsets.Where(kv => !kv.Key.StartsWith("__") && kv.Key != "this").ToList();
                        if (userVars.Count > 0) {
                            int lastOff = userVars.Max(kv => kv.Value);
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{lastOff + 12}")]));
                        }
                    }
                    EmitExit();
                } else {
                    // epilogue for regular functions
                    EmitEpilogue();
                }
            }
            else if (f is ClassDecl cd)
            {
                GenerateNode(cd);
            }
            else if (f is InterfaceDecl iface)
            {
                GenerateNode(iface);
            }
        }
        return BuildProgram("main");
    }

    /// <summary>`println`/`print` 选哪条实现：字面量串、名字像返回串的调用、或**已知装着串的变量**。</summary>
    bool IsStringArg(ASTNode? arg) => arg switch {
        StringLiteral => true,
        CallExpr c => IsStringReturningFunc(c.Name),
        VarRef v => _stringVars.Contains(v.Name) || _stringGlobals.Contains(v.Name),
        _ => false,
    };

    /// <summary>变量的初始值是不是字符串（用来登记 <see cref="_stringVars"/> / <see cref="_stringGlobals"/>）。</summary>
    static bool IsStringInit(ASTNode? init)
        => init is StringLiteral || (init is CallExpr c && IsStringReturningFunc(c.Name));

    /// <summary>把顶层属性逐个初始化一次（值算出来存进数据段标签）。</summary>
    void EmitGlobalInits() {
        foreach (var gv in _globalInits) {
            string vt = gv.Type ?? "Int";
            if (gv.Init != null) {
                GenerateNode(gv.Init);
                EmitKotlinConvert(gv.Init, vt);
            } else {
                EmitLoadConstant(vt switch { "Double" => (object)0.0, "Float" => 0f, "Long" => 0L, _ => 0 });
            }
            instructions.Add(new(StoreOpFor(vt), [new Operand(OperandType.MEMORY, _globalLabel[gv.Name]), new Operand(OperandType.REGISTER, 0)]));
        }
    }

    void GenerateNode(ASTNode node) {
        switch (node) {
            case Block b: foreach (var s in b.Statements) GenerateNode(s); break;
            case VarDecl vd: {
                string vt = vd.Type ?? "Int";
                // ⚠ v0.96.194：`arrayOf(...)` 这类**数组变量要单独标出来** —— 它的下标语义
                //   与字符串完全不同（字符串按 1 字节、数组按 4 字节且要跳过 VML 数组头）。
                //   原来一律按"字符串那套"编，于是 `a[3]` 算的是 `base + 3`。
                //   ⚠ **用独立的集合、不要动 `vt`**：`vt` 会流进 `AllocVar(vt)`/`StoreOpFor(vt)`/
                //   `EmitKotlinConvert(init, vt)`，塞一个假的 "Array" 进去会把变量的**槽大小与
                //   存储指令一起改坏**（实测：指针根本没存进去，读出来恒 0）。
                if (vd.Init is CallExpr arrCall && arrCall.Name is "arrayOf" or "listOf" or "mutableListOf")
                    _arrayVars.Add(vd.Name);
                // 记下「这个局部变量现在装的是不是字符串」—— 供 println/print 选对实现
                if (IsStringInit(vd.Init)) _stringVars.Add(vd.Name);
                else _stringVars.Remove(vd.Name);
                _varTypes[vd.Name] = vt;
                if (vd.Init != null) {
                    GenerateNode(vd.Init);
                    EmitKotlinConvert(vd.Init, vt);
                } else {
                    EmitLoadConstant(vt switch { "Double" => (object)0.0, "Float" => 0f, "Long" => 0L, _ => 0 });
                }
                // store to local frame (类型感知: Float→MOVEF, Double→MOVED, Long→MOVEL)
                int off = AllocVar(vd.Name, vt);
                instructions.Add(new(StoreOpFor(vt), [new Operand(OperandType.MEMORY, $"R12-{off + 12}"), new Operand(OperandType.REGISTER, 0)]));
                break;
            }
            case IntLiteral i: {
    if (i.Value >= int.MinValue && i.Value <= int.MaxValue)
        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)i.Value)]));
    else
        instructions.Add(new(OpCode.MOVEL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, i.Value)]));
    break;
}
            case StringLiteral s: {
                var val = s.Value;
                if (val.Contains("$")) {
                    var parts = new List<(bool isVar, string text)>();
                    int si = 0;
                    while (si < val.Length) {
                        int d = val.IndexOf('$', si);
                        if (d < 0) { parts.Add((false, val.Substring(si))); break; }
                        if (d > si) parts.Add((false, val.Substring(si, d - si)));
                        // Check for ${expr} syntax
                        if (d + 1 < val.Length && val[d + 1] == '{') {
                            int close = val.IndexOf('}', d + 2);
                            if (close > d + 2) {
                                string expr = val.Substring(d + 2, close - d - 2);
                                parts.Add((true, expr)); // Treat expression as variable name for now
                                si = close + 1;
                                continue;
                            }
                        }
                        // Read simple variable name: $name
                        int ve = d + 1;
                        while (ve < val.Length && (char.IsLetterOrDigit(val[ve]) || val[ve] == '_')) ve++;
                        string vn = val.Substring(d + 1, ve - d - 1);
                        if (vn.Length > 0) parts.Add((true, vn));
                        si = ve;
                    }
                    foreach (var part in parts) {
                        if (part.isVar) {
                            if (_varOffsets.TryGetValue(part.text, out int vo))
                                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{vo + 12}")]));
                            else instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                        } else {
                            string lb = $"_str{nextStrId++}";
                            dataSection[lb] = WStr(part.text);
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lb)]));
                        }
                        instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    }
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                } else {
                    string lbl = $"_str{nextStrId++}";
                    dataSection[lbl] = WStr(val);
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lbl)]));
                }
                break;
            }
            case BoolLiteral b: instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, b.Value ? 1 : 0)])); break;
            case VarRef vr: {
                if (vr.Name == "__when_val__") {
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "(R13)")]));
                } else if (_varOffsets.TryGetValue(vr.Name, out int off)) {
                    instructions.Add(new(LoadOpFor(VarType(vr.Name)), [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{off + 12}")]));
                } else if (_globalLabel.TryGetValue(vr.Name, out var glabel)) {
                    // 顶层属性：从数据段读。**这条分支以前没有** —— 查不到局部偏移就什么都不生成，
                    // R0 留着上一步的残值，读出来永远是 0（台账那条"顶层 arrayOf 读回是 0"的真身）。
                    instructions.Add(new(LoadOpFor(VarType(vr.Name)), [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, glabel)]));
                } else if (_varOffsets.TryGetValue("this", out int thisOff)) {
                    // Try to resolve as property of 'this'
                    foreach (var cd in _classDefs.Values) {
                        int idx = cd.props.FindIndex(p => p.Item1 == vr.Name);
                        if (idx >= 0) {
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{thisOff + 12}")])); // load this
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R0+{(idx + 1) * 4}")])); // load property
                            break;
                        }
                    }
                }
                // ⚠ **五条分支全落空 = 这个标识符从来没被声明过**。
                //
                // 这里此前**什么都不发**（直接掉到 `break`）—— 比"建个初值 0 的槽"更隐蔽：
                // 另外 15 门查不到会 `dataSection["var_x"] = 0` 至少给个确定的 0，
                // 而 Kotlin 这条让 **R0 保留上一条指令的残值** ⇒ 读出来的是**上一次运算的结果**，
                // 看着像个"有时对有时不对"的随机 bug。
                //
                // 现在：报一条「未声明的变量」+ 发 `MOVE R0,#0` 占位，**继续生成** ——
                // 一个文件里写错多个名字会一次全报出来（收口在 `BuildProgram`）。
                else
                {
                    if (!ImplicitDeclarationAllowed)
                        ReportUndefined(vr.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                    EmitUndefinedFallback();
                }
                break;
            }
            case AssignStmt a: {
                // ⚠ 下标赋值 `a[i] = v` / `a[i] op= v` —— **必须自己算地址**。
                //   原来落到下面 `a.Target != null` 那条通用支路，而它对 IndexExpr 只是
                //   `GenerateNode(a.Target)` ⇒ 拿到的是**元素值**（IndexExpr 的读法收尾是
                //   `MOVE R0, [R0]`），不是地址；再 `MOVE R1, [R0]` 又是 **load** ——
                //   于是「以元素值为地址读一次」吞掉待写值，**元素从来没被写过**。
                //   两条都属于"地址当值 / 操作数写反"族。
                if (a.Target is IndexExpr idxT && idxT.Target is VarRef idxBase && _arrayVars.Contains(idxBase.Name)) {
                    // 地址：块地址 + idx*4 + 4（跳过 VML 数组头），全程用**栈**保存中间量
                    //（右值表达式里可能调函数，R1/R2 都可能被改）。
                    GenerateNode(idxT.Target);                                    // R0 = 块地址
                    instructions.Add(new(OpCode.PUSH, [Reg(0)]));
                    GenerateNode(idxT.Index);                                     // R0 = 下标
                    instructions.Add(new(OpCode.MOVE, [Reg(1), Imm(4)]));
                    instructions.Add(new(OpCode.MUL, [Reg(0), Reg(1)]));          // R0 = idx*4
                    instructions.Add(new(OpCode.ADD, [Reg(0), Imm(4)]));
                    instructions.Add(new(OpCode.POP, [Reg(1)]));                  // R1 = 块地址
                    instructions.Add(new(OpCode.ADD, [Reg(0), Reg(1), Reg(0)]));  // R0 = 元素地址
                    instructions.Add(new(OpCode.PUSH, [Reg(0)]));                 // 地址留栈上

                    if (a.Op != "=") {
                        // 复合赋值：先按地址取旧值（`MOVE R0, [R0]` 是 load）
                        instructions.Add(new(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new(OpCode.PUSH, [Reg(0)]));
                    }
                    GenerateNode(a.Value);                                        // R0 = 右值
                    if (a.Op != "=") {
                        instructions.Add(new(OpCode.POP, [Reg(1)]));              // R1 = 旧值
                        // 2 操作数是 dest 在前：R1 = R1 op R0（加减乘除取模都不需要交换）
                        OpCode binOp = a.Op switch {
                            "+=" => OpCode.ADD, "-=" => OpCode.SUB, "*=" => OpCode.MUL,
                            "/=" => OpCode.DIV, "%=" => OpCode.MOD, _ => OpCode.ADD };
                        instructions.Add(new(binOp, [Reg(1), Reg(0)]));
                        instructions.Add(new(OpCode.MOVE, [Reg(0), Reg(1)]));
                    }
                    instructions.Add(new(OpCode.POP, [Reg(1)]));                  // R1 = 元素地址
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)])); // [R1] = R0
                    break;
                }
                if (a.Target != null) {
                    // Member assignment: obj.field = value
                    GenerateNode(a.Value); // value in R0
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    // Compute address of member
                    if (a.Target is MemberAccess ma && ma.Object is VarRef vr && _varOffsets.ContainsKey(vr.Name)) {
                        int baseOff = _varOffsets[vr.Name];
                        // R0 = BP - (baseOff + 12) * 4  (address of local var p)
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 12)]));
                        instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -(baseOff + 12) * 4)]));
                        // Load object pointer (p is a reference to heap-allocated object)
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                        // Add member offset
                        foreach (var cd in _classDefs.Values) {
                            int idx = cd.props.FindIndex(p => p.Item1 == ma.Member);
                            if (idx >= 0) {
                                instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (idx + 1) * 4)]));
                                break;
                            }
                        }
                    } else {
                        GenerateNode(a.Target); // fallback
                    }
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]));
                } else if (a.Op != "=" && _varOffsets.ContainsKey(a.Name)) {
                    _expr!.EmitCompoundAssign(WrapTargetExpr(new VarRef(a.Name)), WrapExpr(a.Value), a.Op.TrimEnd('='));
                } else {
                    GenerateNode(a.Value);
                    if (_varOffsets.TryGetValue(a.Name, out int off)) {
                        string vt = VarType(a.Name);
                        EmitKotlinConvert(a.Value, vt);
                        instructions.Add(new(StoreOpFor(vt), [new Operand(OperandType.MEMORY, $"R12-{off + 12}"), new Operand(OperandType.REGISTER, 0)]));
                    } else if (_globalLabel.TryGetValue(a.Name, out var gstore)) {
                        // 顶层 `var` 赋值：写回数据段（与读那条对称，别再漏一次）
                        string vt = VarType(a.Name);
                        EmitKotlinConvert(a.Value, vt);
                        instructions.Add(new(StoreOpFor(vt), [new Operand(OperandType.MEMORY, gstore), new Operand(OperandType.REGISTER, 0)]));
                    }
                }
                break;
            }
            case FloatLiteral fl: {
                EmitLoadConstant(fl.Value);
                break;
            }
            case DoubleLiteral dl: {
                EmitLoadConstant(dl.Value);
                break;
            }
            case ClassDecl cd: {
                _classDefs[cd.Name] = (cd.Name, cd.Props, cd.IsSealed, cd.Interfaces);
                // Store type_info label in data section for RTTI / 'is' checks
                string typeLabel = $"{cd.Name}_typeid";
                dataSection[typeLabel] = cd.Name;
                // Generate constructor function
                string ctorLabel = $"{cd.Name}";
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, ctorLabel)]));
                EmitPrologue();
                int objSize = cd.Props.Count * 4 + 4;
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, objSize)]));
                instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 40)]));
                // Store type_info pointer as first word for RTTI / 'is' checks
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.LABEL, typeLabel)]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                for (int i = 0; i < cd.Props.Count; i++) {
                    int argOff = i * 4;
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R13-{argOff + 12}")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R0+{(i + 1) * 4}")]));
                }
                EmitEpilogue();

                // Data class auto-generated methods
                if (cd.IsData) {
                    // toString(): outputs "ClassName(prop1=val1, prop2=val2)"
                    string toStringLabel = $"{cd.Name}_toString";
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, toStringLabel)]));
                    EmitPrologue();
                    // Output class name
                    string classNameStr = $"_str{nextStrId++}";
                    dataSection[classNameStr] = $"{cd.Name}(";
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, classNameStr)]));
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    for (int pi = 0; pi < cd.Props.Count; pi++) {
                        if (pi > 0) {
                            string commaStr = $"_str{nextStrId++}";
                            dataSection[commaStr] = ", ";
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, commaStr)]));
                            instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                        }
                        string propNameStr = $"_str{nextStrId++}";
                        dataSection[propNameStr] = $"{cd.Props[pi].Item1}=";
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, propNameStr)]));
                        instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+12")])); // this ptr
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R0+{(pi + 1) * 4}")]));
                        instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    }
                    string closeStr = $"_str{nextStrId++}";
                    dataSection[closeStr] = ")";
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, closeStr)]));
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    EmitEpilogue();

                    // equals(other): compare all fields
                    string equalsLabel = $"{cd.Name}_equals";
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, equalsLabel)]));
                    EmitPrologue();
                    for (int pi = 0; pi < cd.Props.Count; pi++) {
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+12")])); // this
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R0+{(pi + 1) * 4}")]));
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+16")])); // other
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R0+{(pi + 1) * 4}")]));
                        instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                        string eqNext = NewLabel();
                        instructions.Add(new(OpCode.JE, [new Operand(OperandType.LABEL, eqNext)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                        EmitEpilogue();
                        instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, eqNext)]));
                    }
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    EmitEpilogue();
                }

                // Generate methods
                foreach (var method in cd.Methods) {
                    string mLabel = $"{cd.Name}_{method.Name}";
                    instructions.Add(new(OpCode.NOP, [], 0, "; --------------------------------------------"));
                    var methodSourceDecl = $"fun {method.Name}(";
                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        methodSourceDecl += $"{method.Parameters[i]}: Any";
                        if (i < method.Parameters.Count - 1) methodSourceDecl += ", ";
                    }
                    methodSourceDecl += "): Any";
                    instructions.Add(new(OpCode.NOP, [], 0, $"; source   : {methodSourceDecl}"));
                    instructions.Add(new(OpCode.NOP, [], 0, $"; function : {cd.Name}.{method.Name}"));
                    foreach (var param in method.Parameters)
                    {
                        instructions.Add(new(OpCode.NOP, [], 0, $"; param   : Any {param}"));
                    }
                    instructions.Add(new(OpCode.NOP, [], 0, $"; return   : Any"));
                    instructions.Add(new(OpCode.NOP, [], 0, "; --------------------------------------------"));
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, mLabel)]));
                    EmitPrologue();
                    int thisOff = AllocVar("this", "Any");
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{thisOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                    GenerateNode(method.Body);
                    EmitEpilogue();
                }
                // Generate interface dispatch stubs
                foreach (var ifaceName in cd.Interfaces) {
                    if (_interfaces.TryGetValue(ifaceName, out var iface)) {
                        foreach (var imethod in iface.Methods) {
                            var cmethod = cd.Methods.FirstOrDefault(m => m.Name == imethod.Name);
                            if (cmethod != null) {
                                string stubLabel = $"{ifaceName}_{imethod.Name}";
                                for (int i = 0; i < instructions.Count; i++) {
                                    if (instructions[i].Opcode == OpCode.LABEL && instructions[i].Operands.Count > 0
                                        && instructions[i].Operands[0].Value is string ln && ln == stubLabel) {
                                        instructions[i + 1] = new(OpCode.JMP, [new Operand(OperandType.LABEL, $"{cd.Name}_{cmethod.Name}")]);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            }
            case NewExpr ne: {
                // Constructor call: alloc + init via constructor function
                if (_classDefs.ContainsKey(ne.ClassName)) {
                    var cd = _classDefs[ne.ClassName];
                    // Push args right to left
                    for (int i = ne.Args.Count - 1; i >= 0; i--) {
                        GenerateNode(ne.Args[i]);
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    }
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, ne.ClassName)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, ne.Args.Count * 4)]));
                }
                break;
            }
            case IndexExpr ie: {
                // ⚠ v0.96.194 修（两处，与 Go 的 patch 0015 同族）：
                //   ① 数组下标原来**完全按字符串那套算**（`base + idx`，1 字节步长、不跳数组头）
                //      ⇒ 算出来的是个错地址；数组元素 i 在 `base + i*4 + 4`
                //      （`kotlin_array_alloc` 的布局是 `[count, e0, e1, …]`）。
                //   ② 最后那句"load byte"写的是 `MOVEB R0, R0` —— **自赋值、空操作**，
                //      地址被当成元素值返回。**"地址当值"族第七次**（前六次：C# 的 LABEL/MEMORY、
                //      BASIC 的 `MOVE reg, R2`、Swift 的读数组、Go 的 `MOVE R0,R0` …）。
                bool isArray = ie.Target is VarRef arrRef && _arrayVars.Contains(arrRef.Name);
                // ⚠ v0.96.194：数组这条**不用 PUSH/POP 保存基址，改存 R2** ——
                //   原来的 `push base … pop R1` 与**外层表达式**自己的 push/pop 交织在一起，
                //   两套栈操作在同一段序列里交错，实测读数会随下标漂（`a[0]` 读到下一格、
                //   `a[1]` 读到野值）。用寄存器存基址就没有这层耦合。
                if (isArray) {
                    GenerateNode(ie.Target);                                  // R0 = 块地址
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]));
                    GenerateNode(ie.Index);                                   // R0 = 下标
                    // R0 = idx*4 + 4（2 操作数是 dest 在前：Rd = Rd op Rs）
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]));
                    // 取元素值（**不是**自赋值 —— 原来写的是 `MOVE R0, R0`，空操作）
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
                    break;
                }
                GenerateNode(ie.Target); // 字符串：地址 in R0
                instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save addr
                GenerateNode(ie.Index); // index in R0
                instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // R1 = addr
                instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)])); // R0 = addr + idx
                // 取字节（原来写的是 `MOVEB R0, R0` —— 自赋值、空操作）
                instructions.Add(new(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
                break;
            }
            case LambdaExpr le: {
                // Generate anonymous function with unique label
                string lamLabel = $"__lam_{labelCounter++}";
                instructions.Add(new(OpCode.NOP, [], 0, "; --------------------------------------------"));
                var lamSourceDecl = $"{{ ";
                for (var i = 0; i < le.Parameters.Count; i++)
                {
                    lamSourceDecl += $"{le.Parameters[i]}: Any";
                    if (i < le.Parameters.Count - 1) lamSourceDecl += ", ";
                }
                lamSourceDecl += " -> ... }}";
                instructions.Add(new(OpCode.NOP, [], 0, $"; source   : {lamSourceDecl}"));
                instructions.Add(new(OpCode.NOP, [], 0, $"; function : {lamLabel}"));
                foreach (var param in le.Parameters)
                {
                    instructions.Add(new(OpCode.NOP, [], 0, $"; param   : Any {param}"));
                }
                instructions.Add(new(OpCode.NOP, [], 0, $"; return   : Any"));
                instructions.Add(new(OpCode.NOP, [], 0, "; --------------------------------------------"));
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, lamLabel)]));
                EmitPrologue();
                // Load lambda parameters from stack (same layout as function params)
                int lamParams = le.Parameters.Count;
                for (int pi = 0; pi < lamParams; pi++) {
                    int off = AllocVar(le.Parameters[pi], "Int");
                    int stackOff = 12 + (lamParams - 1 - pi) * 4;
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{stackOff}")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{off + 12}"), new Operand(OperandType.REGISTER, 0)]));
                }
                GenerateNode(le.Body);
                EmitEpilogue();
                // Load function address (for passing as value)
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lamLabel)]));
                break;
            }
            case MemberAccess ma: {
                GenerateNode(ma.Object); // obj addr in R0
                if (ma.Member == "length") {
                    string lenLoop = NewLabel(), lenEnd = NewLabel();
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, lenLoop)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]));
                    instructions.Add(new(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.JZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lenEnd)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")])); // addr
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)])); // addr++
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")])); // save addr
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")])); // count
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)])); // count++
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")])); // save count
                    instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, lenLoop)]));
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, lenEnd)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)])); // pop addr
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // count in R0
                } else if (ma.Object is VarRef vr && _classDefs.Values.Any(cd => cd.props.Any(p => p.Item1 == ma.Member))) {
                    foreach (var cd in _classDefs.Values) {
                        int idx = cd.props.FindIndex(p => p.Item1 == ma.Member);
                        if (idx >= 0) {
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R0+{(idx + 1) * 4}")]));
                            break;
                        }
                    }
                } else {
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]));
                }
                break;
            }
            case UnaryOp uo: {
                if (uo.Op == "-") _expr!.EmitNeg(WrapExpr(uo.Operand));
                else if (uo.Op == "!") _expr!.EmitNot(WrapExpr(uo.Operand));
                else if (uo.Op == "++" || uo.Op == "--") {
                    if (uo.Operand is VarRef varId && _varOffsets.ContainsKey(varId.Name))
                    {
                        var target = WrapTargetExpr(uo.Operand);
                        if (uo.Op == "++") _expr!.EmitPrefixInc(target);
                        else _expr!.EmitPrefixDec(target);
                    }
                }
                else if (uo.Op == "++post" || uo.Op == "--post") {
                    if (uo.Operand is VarRef varId && _varOffsets.ContainsKey(varId.Name))
                    {
                        var target = WrapTargetExpr(uo.Operand);
                        if (uo.Op == "++post") _expr!.EmitPostfixInc(target);
                        else _expr!.EmitPostfixDec(target);
                    }
                }
                break;
            }
            case BinaryOp bo: {
                // 短路逻辑由 ExpressionManager 统一处理
                if (bo.Op == "&&") { _expr!.EmitAnd(WrapExpr(bo.Left), WrapExpr(bo.Right)); break; }
                if (bo.Op == "||") { _expr!.EmitOr(WrapExpr(bo.Left), WrapExpr(bo.Right)); break; }
                if (bo.Op == "in") {
                    // x in start..end or x in collection
                    GenerateNode(bo.Left); // x
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    if (bo.Right is BinaryOp range && range.Op == "..") {
                        // Range check: start <= x && x <= end
                        GenerateNode(range.Left); // start
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // x
                        instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                        string inFail = NewLabel(), inEnd = NewLabel();
                        instructions.Add(new(OpCode.JL, [new Operand(OperandType.LABEL, inFail)])); // x < start → fail
                        // x <= end?
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // x
                        GenerateNode(range.Right); // end
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // x
                        instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new(OpCode.JG, [new Operand(OperandType.LABEL, inFail)])); // x > end → fail
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, inEnd)]));
                        instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, inFail)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                        instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, inEnd)]));
                    } else {
                        // Simple in check (membership) - just return 1
                        instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    }
                    break;
                }
                if (bo.Op == "is") {
                    EmitCompareToBool(() => GenerateNode(bo.Left), OpCode.JNE);
                    break;
                }
                if (bo.Op == "+" && (bo.Left is StringLiteral || bo.Right is StringLiteral)) {
                    // String concatenation: output both parts via SYSCALL 6
                    GenerateNode(bo.Left);
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    GenerateNode(bo.Right);
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 6)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    break;
                }
                if (bo.Op is "<" or "<=" or ">" or ">=" or "==" or "!=")
                    _expr!.EmitCmp(WrapExpr(bo.Left), WrapExpr(bo.Right), bo.Op);
                else
                    _expr!.EmitBinOp(WrapExpr(bo.Left), WrapExpr(bo.Right), bo.Op);
                break;
            }
            case CallExpr ce when ce.Name == "println": {
                if (ce.Args.Count > 0)
                {
                    bool isString = IsStringArg(ce.Args[0]);
                    EmitPrintArg(() => GenerateNode(ce.Args[0]), isString);
                }
                EmitPrintNewline();
                break;
            }
            case CallExpr ce when ce.Name == "print": {
                if (ce.Args.Count > 0)
                {
                    bool isString = IsStringArg(ce.Args[0]);
                    EmitPrintArg(() => GenerateNode(ce.Args[0]), isString);
                }
                break;
            }
            case CallExpr ce when ce.Name is "arrayOf" or "listOf" or "mutableListOf": {
                // 数组字面量：按 VML 数组布局 `[count, e0, e1, …]` 自己分配 + 填充。
                int elemCount = ce.Args.Count;
                // ⚠ v0.96.196：**不再 `CALL kotlin_array_alloc`** —— 那条路实测走不通，而且
                //   根因不在本文件：`Lib/kotlin/builtins_kotlin.vml` 的
                //   `kotlin_array_alloc: PUSH R0 / CALL array_alloc / RET` 里那句 `CALL array_alloc`
                //   被 `LibraryLinker` 的「已解析到包装器 → 寻找更好的实现体」那段启发式
                //   （`target.EndsWith("_" + bareName) && target.StartsWith("lib_")`）改写成了
                //   **通用 `alloc(size)`**（`lib_builtins_kotlin_array_alloc`.EndsWith("_alloc") ⇒
                //   重定向到 `lib_builtins_alloc`）。实测汇编里 `lib_builtins_alloc:` 就是
                //   `move R0 [R12+12]; syscall #40; ret` —— **只分配 `count` 个字节、
                //   且没有 `[count, e0, …]` 头**，于是元素 i 落在 `base + i*4 + 4` 的整套假设失效
                //   （`arrayOf(10,20,40,80)` 实测只有 `a[0]` 对，其余全是 0）。
                //   这里按 VML 数组布局**自己分配**：`(count+1)*4` 字节，`[0]` 存 count。
                instructions.Add(new(OpCode.MOVE, [Reg(0), Imm((elemCount + 1) * 4)]));
                instructions.Add(new(OpCode.SYSCALL, [Imm(40)]));          // R0 = 块地址
                // 存数组头 count，再存元素。
                // ⚠ v0.96.194 修：基址**必须每轮重新取**。原来的循环开头是
                //   `PUSH R0` + 注释「save arr ptr」—— 可 R0 只在 i=0 时才是数组指针，
                //   i≥1 时它已经被上一轮的 `GenerateNode(arg)` 覆盖成**上一个元素的值**
                //   ⇒ `POP R1` 拿到的是那个值、`[R1+off]` 写在野地址上。
                //   实测 `arrayOf(10,20,40,80)`：只有 a[0]=10 落到对的地方，
                //   a[1]/a[2]/a[3] 全丢（并且函数返回值也变成最后一个元素值）。
                //   现在基址常驻**栈顶**（元素表达式可能调函数，任何寄存器都靠不住）：
                //   用之前 POP 出来、用完立刻 PUSH 回去，循环结束再 POP 回 R0 当返回值。
                instructions.Add(new(OpCode.PUSH, [Reg(0)]));                     // 基址入栈（此后栈顶恒为基址）
                instructions.Add(new(OpCode.MOVE, [Reg(0), Imm(elemCount)]));
                instructions.Add(new(OpCode.POP, [Reg(1)]));                      // R1 = 基址
                instructions.Add(new(OpCode.PUSH, [Reg(1)]));                     // 放回
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)])); // [base] = count（VML 数组头）
                for (int i = 0; i < elemCount; i++) {
                    GenerateNode(ce.Args[i]);                                     // R0 = 元素值
                    instructions.Add(new(OpCode.POP, [Reg(1)]));                  // R1 = 基址
                    instructions.Add(new(OpCode.PUSH, [Reg(1)]));                 // 立刻放回
                    // `MOVE dest, src` 的 **dest 在前** ⇒ 存要写成 `[R1+off], R0`。
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{(i + 1) * 4}"), new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new(OpCode.POP, [Reg(0)]));                      // 返回值 = 基址
                break;
            }
            case CallExpr ce when ce.Name == "abs" && ce.Args.Count == 1: {
                GenerateNode(ce.Args[0]);
                EmitCallAbs();
                break;
            }
            case CallExpr ce when ce.Name == "min" && ce.Args.Count == 2: {
                GenerateNode(ce.Args[0]); instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateNode(ce.Args[1]); instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                EmitCallMin();
                break;
            }
            case CallExpr ce when ce.Name == "max" && ce.Args.Count == 2: {
                GenerateNode(ce.Args[0]); instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateNode(ce.Args[1]); instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                EmitCallMax();
                break;
            }
            case CallExpr ce when ce.Name == "readLine" && ce.Args.Count == 0: {
                instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "kotlin_read_line")]));
                break;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Kotlin 通过 Lib/c/vmlsys.c 调用系统功能
            case CallExpr ce when ce.Name == "chipasm" && ce.Args.Count >= 2:
                break;  // chipasm 转译必需，所有语言保留
            case CallExpr ce when ce.Name.StartsWith("peek") && ce.Args.Count >= 1: {
                string runtimeFn = "Kotlin_" + ce.Name;
                GenerateNode(ce.Args[0]);
                instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]));
                break;
            }
            case CallExpr ce when ce.Name.StartsWith("poke") && ce.Args.Count >= 2: {
                string runtimeFn = "Kotlin_" + ce.Name;
                GenerateNode(ce.Args[0]);
                instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateNode(ce.Args[1]);
                instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]));
                break;
            }
            case CallExpr ce: {
                // Check for built-in stdlib methods
                if (ce.Name == "toString" && ce.Args.Count == 1) {
                    GenerateNode(ce.Args[0]); // R0 = value
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save value
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 32)])); // alloc buf
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)])); // R1 = buf
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // buf
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "itoa")])); // vml_itoa(val, buf)
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)])); // R0 = buf
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 40)])); // clean
                    break;
                }
                if (ce.Name == "toInt" && ce.Args.Count == 1) {
                    GenerateNode(ce.Args[0]);
                    // 类型感知转换: Float→F2I, Double→D2I, Long→L2I (v1.66.64 修复)
                    OpCode cvt = InferType(ce.Args[0]) switch {
                        "Float" => OpCode.F2I,
                        "Double" => OpCode.D2I,
                        "Long" => OpCode.L2I,
                        _ => OpCode.MOVE,
                    };
                    if (cvt != OpCode.MOVE) instructions.Add(new(cvt, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 0)]));
                    break;
                }
                if (ce.Name == "toFloat" && ce.Args.Count == 1) {
                    GenerateNode(ce.Args[0]);
                    OpCode cvt = InferType(ce.Args[0]) switch {
                        "Int" => OpCode.I2F,
                        "Double" => OpCode.D2F,
                        "Long" => OpCode.L2F,
                        _ => OpCode.MOVE,
                    };
                    if (cvt != OpCode.MOVE) instructions.Add(new(cvt, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 0)]));
                    break;
                }
                if (ce.Name == "toDouble" && ce.Args.Count == 1) {
                    GenerateNode(ce.Args[0]);
                    OpCode cvt = InferType(ce.Args[0]) switch {
                        "Int" => OpCode.I2D,
                        "Float" => OpCode.F2D,
                        "Long" => OpCode.L2D,
                        _ => OpCode.MOVE,
                    };
                    if (cvt != OpCode.MOVE) instructions.Add(new(cvt, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 0)]));
                    break;
                }
                if (ce.Name == "toLong" && ce.Args.Count == 1) {
                    GenerateNode(ce.Args[0]);
                    OpCode cvt = InferType(ce.Args[0]) switch {
                        "Int" => OpCode.I2L,
                        "Float" => OpCode.F2L,
                        "Double" => OpCode.D2L,
                        _ => OpCode.MOVE,
                    };
                    if (cvt != OpCode.MOVE) instructions.Add(new(cvt, [new(OperandType.REGISTER, 0), new(OperandType.REGISTER, 0)]));
                    break;
                }
                // Resolve extension function call: obj.method() -> ReceiverType_method label
                string callLabel = ce.Name;
                foreach (var ekv in _extMethods) {
                    if (ekv.Key.EndsWith($"_{ce.Name}")) {
                        callLabel = ekv.Key;
                        break;
                    }
                }
                // Generic function call: push args right-to-left, CALL label
                // Track 64-bit args (double/long) that need 8-byte stack slots
                int totalStackBytes = 0;
                for (int i = ce.Args.Count - 1; i >= 0; i--) {
                    var arg = ce.Args[i];
                    bool is64Bit = arg is DoubleLiteral
                        || (arg is IntLiteral il && (il.Value < int.MinValue || il.Value > int.MaxValue));
                    GenerateNode(arg);
                    if (is64Bit) {
                        // Push 8 bytes for double/long: SUB R13,8 + MOVED/MOVEL @R13,R0
                        instructions.Add(new(OpCode.SUB, [Reg(13), Imm(8)]));
                        instructions.Add(new(arg is DoubleLiteral ? OpCode.MOVED : OpCode.MOVEL,
                            [new Operand(OperandType.INDIRECT, 13), Reg(0)]));
                        totalStackBytes += 8;
                    } else {
                        instructions.Add(new(OpCode.PUSH, [Reg(0)]));
                        totalStackBytes += 4;
                    }
                }
                instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, callLabel)]));
                if (totalStackBytes > 0)
                    instructions.Add(new(OpCode.ADD, [Reg(13), Reg(13), Imm(totalStackBytes)]));
                break;
            }
            case IfStmt ifs: {
                Sta!.EmitIf(
                    () => GenerateNode(ifs.Condition),
                    () => GenerateNode(ifs.Then),
                    ifs.Else != null ? () => GenerateNode(ifs.Else) : null);
                break;
            }
            case WhileStmt ws: {
                Sta!.EmitWhile(
                    () => GenerateNode(ws.Condition),
                    () => GenerateNode(ws.Body));
                break;
            }
            case DoWhileStmt dws: {
                Sta!.EmitDoWhile(
                    () => GenerateNode(dws.Body),
                    () => GenerateNode(dws.Condition));
                break;
            }
            case WhenStmt ws: {
                string wEnd = NewLabel();
                string resultLabel = $"__when_res_{labelCounter++}";
                dataSection[resultLabel] = 0;
                var coveredTypes = new HashSet<string>();
                // Push subject value onto stack for PUSH/POP protection
                GenerateNode(ws.Value);
                instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                foreach (var branch in ws.Branches)
                {
                    string wNext = NewLabel();
                    VarRef? typeRef = (branch.Condition is BinaryOp bo && bo.Op == "is" && bo.Right is VarRef tr) ? tr : null;
                    // POP subject → save back for comparison
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                    if (typeRef != null)
                    {
                        coveredTypes.Add(typeRef.Name);
                        // R1 = subject ptr → deref to get type_info
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.LABEL, $"{typeRef.Name}_typeid")]));
                        instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
                        instructions.Add(new(OpCode.JNE, [new Operand(OperandType.LABEL, wNext)]));
                    }
                    else
                    {
                        // Value comparison: case value → R0, compare with subject in R1
                        GenerateNode(branch.Condition);
                        instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.JNE, [new Operand(OperandType.LABEL, wNext)]));
                    }
                    // Match: POP cleanup subject from stack, execute body, store result, jump to end
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateNode(branch.Body);
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.LABEL, resultLabel), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, wEnd)]));
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, wNext)]));
                }
                // Sealed class exhaustiveness check: verify all subclasses are covered
                if (ws.Else == null && coveredTypes.Count > 0)
                {
                    bool found = false;
                    foreach (var coveredType in coveredTypes)
                    {
                        if (_classDefs.TryGetValue(coveredType, out var typeDef))
                        {
                            foreach (var parentName in typeDef.interfaces)
                            {
                                if (_classDefs.TryGetValue(parentName, out var parentDef) && parentDef.isSealed)
                                {
                                    if (_sealedSubclasses.TryGetValue(parentName, out var allSubs))
                                    {
                                        var missing = allSubs.Where(s => !coveredTypes.Contains(s)).ToList();
                                        if (missing.Count > 0)
                                        {
                                            throw new CompilationException(ErrorCode.Parser_TypeConflict, $"sealed class '{parentName}' when-expression must be exhaustive — missing subclasses: {string.Join(", ", missing)} (covered: {string.Join(", ", coveredTypes)})");
                                        }
                                    }
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (found) break;
                    }
                }
                // No branch matched: POP cleanup subject from stack
                instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                if (ws.Else != null)
                {
                    GenerateNode(ws.Else);
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.LABEL, resultLabel), new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, wEnd)]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel)]));
                break;
            }
            case ForStmt fs: {
                string loopL = NewLabel(), endL = NewLabel();
                Sta!.PushLoopLabels(endL, loopL);
                // varName = start
                GenerateNode(fs.Start);
                int varOff = AllocVar(fs.VarName, "Int");
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{varOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                // Evaluate end value and store as next var (register to count toward frame)
                int endOff = AllocVar($"__for_end_{fs.VarName}", "Int");
                GenerateNode(fs.End);
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{endOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                // `step n` —— 步长与终值一样**在进循环前求值一次**（Kotlin 的 range 是值而不是惰性序列）。
                // ⚠ 此前解析器把 `step` 吃掉了、代码生成这边**看都没看** ⇒ 增量写死 ±1，
                //   `for (i in 0..10 step 2)` 打出的是全部十一个数（**不报错、静默不生效**）。
                int stepOff = -1;
                if (fs.Step != null) {
                    stepOff = AllocVar($"__for_step_{fs.VarName}", "Int");
                    GenerateNode(fs.Step);
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{stepOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                }
                // loop label
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, loopL)]));
                // load var, compare with end
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{varOff + 12}")]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12-{endOff + 12}")]));
                instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                if (fs.Kind == "downTo")
                    instructions.Add(new(OpCode.JL, [new Operand(OperandType.LABEL, endL)]));
                else if (fs.Kind == "until")
                    instructions.Add(new(OpCode.JGE, [new Operand(OperandType.LABEL, endL)]));
                else
                    instructions.Add(new(OpCode.JG, [new Operand(OperandType.LABEL, endL)]));
                // body
                GenerateNode(fs.Body);
                // var increment
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{varOff + 12}")]));
                if (stepOff >= 0) {
                    // 有 step：`R0 = R0 ∓ 步长`（三操作数形式，与下面那条立即数版本同族）
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12-{stepOff + 12}")]));
                    instructions.Add(new(fs.Kind == "downTo" ? OpCode.SUB : OpCode.ADD,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                } else if (fs.Kind == "downTo")
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                else
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{varOff + 12}"), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, loopL)]));
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, endL)]));
                Sta!.PopLoopLabels();
                break;
            }
            case ThrowStmt ts:
                if (HandleMCUThrow(() => AddRI(OpCode.MOVE, 0, -1)))
                    break;
                if (ts.Expression != null) GenerateNode(ts.Expression);
                else AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new(OpCode.THROW, [Reg(0)]));
                break;
            case TryStmt ts:
                if (HandleMCUTry(() => { GenerateNode(ts.Body); if (ts.FinallyBlock != null) GenerateNode(ts.FinallyBlock); }))
                    break;
                else
                {
                    string catchLabel = NewLabel();
                    string endTryLabel = NewLabel();
                    instructions.Add(new(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
                    GenerateNode(ts.Body);
                    instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, endTryLabel)]));
                    labels[catchLabel] = instructions.Count;
                    foreach (var cc in ts.Catches)
                    {
                        if (cc.VarName != null)
                        {
                            int catchOff = AllocVar(cc.VarName, "Int");
                            instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12-{catchOff + 12}"), Reg(0)]));
                        }
                        GenerateNode(cc.Body);
                        instructions.Add(new(OpCode.ENDCATCH, []));
                    }
                    if (ts.FinallyBlock != null) GenerateNode(ts.FinallyBlock);
                    labels[endTryLabel] = instructions.Count;
                }
                break;
            case BreakStmt:
                Sta!.EmitBreak();
                break;
            case ContinueStmt:
                Sta!.EmitContinue();
                break;
            case ReturnStmt rs: {
                if (rs.Value != null) GenerateNode(rs.Value);
                // Inline epilogue: restore frame and return
                EmitEpilogue();
                break;
            }
            case ElvisExpr ee: {
                string eEnd = NewLabel();
                GenerateNode(ee.Left);
                Sta!.EmitJumpIfTrue(eEnd);
                GenerateNode(ee.Right);
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, eEnd)]));
                break;
            }
            case SafeCallExpr sc: {
                string scEnd = NewLabel();
                GenerateNode(sc.Object);
                Sta!.EmitJumpIfFalse(scEnd);
                instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                bool scFound = false;
                foreach (var cd in _classDefs.Values) {
                    int idx = cd.props.FindIndex(p => p.Item1 == sc.Member);
                    if (idx >= 0) {
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R1+{(idx + 1) * 4}")]));
                        scFound = true;
                        break;
                    }
                }
                if (!scFound)
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, scEnd)]));
                break;
            }
            case NotNullAssert nna: {
                GenerateNode(nna.Expr);
                // !! → null check: if R0 == 0 (null), throw / exit with error
                string nnaOk = NewLabel();
                Sta!.EmitJumpIfTrue(nnaOk); // if non-null, ok
                if (CurrentOptions.IsMCU)
                {
                    // MCU: print error and exit
                    instructions.Add(new(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, -1)]));
                    EmitExit();
                }
                else
                {
                    // OS: throw NullPointerException
                    instructions.Add(new(OpCode.THROW, [Reg(0)]));
                }
                instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, nnaOk)]));
                break;
            }
            case InterfaceDecl iface:
                // Generate interface vtable entries (static dispatch stubs)
                foreach (var method in iface.Methods) {
                    string vtableLabel = $"{iface.Name}_{method.Name}";
                    instructions.Add(new(OpCode.LABEL, [new Operand(OperandType.LABEL, vtableLabel)]));
                    // Interface method stub: load args and dispatch
                    EmitPrologue();
                    // Fallback: return 0 (concrete implementation overrides this at link time)
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    EmitEpilogue();
                }
                break;
        }
    }

    new Dictionary<string, int> _varOffsets = [];
    Dictionary<string, string> _varTypes = [];
    /// <summary>`arrayOf(...)` 声明出来的数组变量名（下标语义与字符串不同，见 IndexExpr）。</summary>
    HashSet<string> _arrayVars = [];
    int _frameBytes = -1; // -1 = 尚未分配局部变量

    static int VarSize(string type) => (type == "Double" || type == "Long") ? 8 : 4;
    int AllocVar(string name, string type) {
        int size = VarSize(type);
        // 帧向负方向增长; 变量向上存储 size 字节, 故下一个变量偏移 = 上一个偏移 + 自身 size (v1.66.64)
        int off = _frameBytes < 0 ? 0 : _frameBytes + size;
        _frameBytes = off;
        _varTypes[name] = type;
        _varOffsets[name] = off;
        return off;
    }

    static OpCode StoreOpFor(string type) => type switch {
        "Float" => OpCode.MOVEF,
        "Double" => OpCode.MOVED,
        "Long" => OpCode.MOVEL,
        _ => OpCode.MOVE,
    };
    static OpCode LoadOpFor(string type) => type switch {
        "Float" => OpCode.MOVEF,
        "Double" => OpCode.MOVED,
        "Long" => OpCode.MOVEL,
        _ => OpCode.MOVE,
    };
    string VarType(string name) => _varTypes.TryGetValue(name, out string? t) ? t
        : _globalTypes.TryGetValue(name, out string? g) ? g : "Int";

    string InferType(ASTNode n) => n switch {
        VarRef vr => VarType(vr.Name),
        FloatLiteral => "Float",
        DoubleLiteral => "Double",
        IntLiteral il => (il.Value < int.MinValue || il.Value > int.MaxValue) ? "Long" : "Int",
        _ => "Int",
    };

    /// <summary>类型转换 (源类型, 目标类型) → 转换指令; 相同类型跳过</summary>
    void EmitKotlinConvert(ASTNode src, string dst) {
        string st = InferType(src);
        if (st == dst) return;
        OpCode? op = (st, dst) switch {
            ("Float", "Double") => OpCode.F2D,
            ("Double", "Float") => OpCode.D2F,
            ("Int", "Float") => OpCode.I2F,
            ("Float", "Int") => OpCode.F2I,
            ("Int", "Double") => OpCode.I2D,
            ("Double", "Int") => OpCode.D2I,
            ("Int", "Long") => OpCode.I2L,
            ("Long", "Int") => OpCode.L2I,
            ("Float", "Long") => OpCode.F2L,
            ("Long", "Float") => OpCode.L2F,
            ("Double", "Long") => OpCode.D2L,
            ("Long", "Double") => OpCode.L2D,
            _ => null,
        };
        if (op.HasValue) instructions.Add(new(op.Value, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]));
    }
    Dictionary<string, (string name, List<(string, string)> props, bool isSealed, List<string> interfaces)> _classDefs = [];
    Dictionary<string, InterfaceDecl> _interfaces = [];
    Dictionary<string, ExtensionDecl> _extMethods = [];
    Dictionary<string, List<string>> _sealedSubclasses = []; // sealedClass -> List<subclassName>

    private ExpVar WrapExpr(ASTNode node) =>
        ExpVar.Eval(ExpType.I32, () => GenerateNode(node));

    private ExpVar WrapTargetExpr(ASTNode node)
    {
        if (node is VarRef varId && _varOffsets.TryGetValue(varId.Name, out int off))
            return ExpVar.Stack(-(off + 12), 12, ExpType.I32);
        return WrapExpr(node);
    }
}
