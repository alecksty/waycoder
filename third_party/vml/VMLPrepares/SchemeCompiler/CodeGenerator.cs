using VMLAssembler;
using CompilerBase;

namespace SchemeCompiler;
public partial class CodeGenerator : CodeGeneratorBase {
    SExpr _ast;
    Dictionary<string, int> vars = [];
    // 局部量分配游标。**做成属性**是为了同时维护 `_peakVarOff`：
    // 帧大小必须按「整个函数体内 **到过的最大** varOff」算，不能按生成结束时的值算 ——
    // `let` / `do` 在收尾时会把 `varOff` 还原（`savedVarOff`），于是
    // `(define (f) (let ((a 1) …二十个…)) …))` 生成完 `varOff` 回到 4，
    // 帧就被算成 64 字节，而局部量实际已经写到了 R12-80 —— **踩进调用方的帧**。
    // 走属性以后 `++varOff` / `varOff = n` 全都自动记账，不必在每个分配点手抄一遍。
    int _varOff;
    int _peakVarOff;
    int varOff {
        get => _varOff;
        set { _varOff = value; if (value > _peakVarOff) _peakVarOff = value; }
    }
    string? _currentFunc;
    int _currentFuncParams;

    List<string> heapCells = [];
    int nextStrId = 0;

    public CodeGenerator(SExpr ast) {
        _ast = ast;
        InitSimpleCompiler();
    }
    public override VmlProgram GenerateCode() {
        // Label at START so .entry main loads into correct address
        AddLabel("main");
        // 顶层也要占帧。**顶层绑定走的是 `R12 + (12 - off)`（off = 4、8、12…）**，
        // 也就是 `R12+8 / R12+4 / R12+0` 之后**继续往下** `R12-4 / R12-8 …`；
        // 而 `main` 此前不 `sub R13` ⇒ `R13 == R12`，**压栈正好写在那一片**：
        // 第 4 个顶层 `(define …)` 起就与实参压栈抢内存。
        // 实测 12 个顶层变量求和得 24（应 78）、5 个变量夹几次调用后得 65536。
        // 占一帧之后压栈落到 `R13-` 侧（帧之外），顶层绑定不再被冲。
        int mainFramePatch = instructions.Count;
        instructions.Add(new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], mainFramePatch));
        _peakVarOff = 0;
        GenTopLevel(_ast);
        int mainFrameSize = _peakVarOff * 4 + 32;
        if (mainFrameSize < 64) mainFrameSize = 64;
        instructions[mainFramePatch] = new Instruction(OpCode.SUB,
            [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, mainFrameSize)], mainFramePatch);
        EmitExit();
        return BuildProgram("main");
    }

    void GenTopLevel(SExpr e) {
        if (e is SList l && l.Items.Count > 0) {
            if (l.Items[0] is SSym s) {
                if (s.Name == "define" && l.Items.Count >= 3 && l.Items[1] is SList fn) {
                // (define (name args) body)
                string fname = ((SSym)fn.Items[0]).Name;
                var fnParams = fn.Items.Skip(1).Select(p => ((SSym)p).Name).ToList();
                int paramCount = fnParams.Count;
                var savedFunc = _currentFunc;
                var savedParams = _currentFuncParams;
                // ⚠ `varOff` / `vars` 是**顶层与函数体共用**的（函数形参直接写进 `vars`），
                //   而函数定义结束处原先**没有恢复**：函数体把 `varOff` 顶到了 4（形参 1 个、
                //   再被 `if (varOff < 4) varOff = 4` 抬到 4），这个值就**漏给了后面的顶层绑定**。
                //   后果不是「编号难看」而是**踩内存**：顶层变量按 `R12 + (12 - off)` 定位，
                //   `off = varOff*4`；而程序入口 `main` **没有序言**，VM 把 R12(BP) 和 R13(SP)
                //   一起初始化成栈顶（`VMLRuntime.cs:570-571`）⇒ `R12-4 / -8 / -12 …` **正是
                //   push 与被调用函数序言落笔的地方**。
                //   实测：`(define (inc x) …)` 之后的 `a`/`s`/`i` 落到 `R12-8 / -12 / -16`，
                //   于是循环里每次 `call inc` 的 `push R15; push R12` 都把 `i` 冲掉
                //   ⇒ `i` 永远到不了 4、**死循环**（`skel.scm` 实测跑 30 秒被 VM 取消）。
                //   恢复之后顶层绑定回到 `R12+8 / +4 / +0`（栈顶之上，push 够不着）。
                int savedVarOffFn = varOff;
                var savedVarsFn = new Dictionary<string, int>(vars);
                _currentFunc = fname;
                _currentFuncParams = paramCount;
                string afterFunc = NewLabel();
                Emit(OpCode.NOP, [], "; --------------------------------------------");
                var sourceDecl = $"(define ({fname}";
                foreach (var p in fnParams)
                {
                    sourceDecl += $" {p}";
                }
                sourceDecl += ") ...)";
                Emit(OpCode.NOP, [], $"; source   : {sourceDecl}");
                Emit(OpCode.NOP, [], $"; function : {fname}");
                foreach (var p in fnParams)
                {
                    Emit(OpCode.NOP, [], $"; param   : any {p}");
                }
                Emit(OpCode.NOP, [], $"; return   : any");
                Emit(OpCode.NOP, [], "; --------------------------------------------");
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFunc)]);
                AddLabel(fname);
                // 帧占位：**不能再写死 64** —— 固定 64 字节装不下真实函数的局部量，
                // 超出的部分直接写进调用方的帧（踩内存）。骨架照不出来，因为骨架的函数都极小。
                // 帧大小要等函数体生成完才知道，故先占位、最后回填（与 R 的 GenerateCode 同口径）。
                EmitPrologue();
                int framePatchIndex = instructions.Count;
                instructions.Add(new Instruction(OpCode.SUB,
                    [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], framePatchIndex));
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, $"{fname}_body")]);
                // 形参绑定：arg_i 在 **R12+12+4i**（GenCall 逆序压栈 ⇒ arg0 在最顶，
                // `call` 再压返回地址，序言压 R15/R12 —— 共 3 槽 = 12 字节）。
                // `vars` 的约定是「偏移 off → 读 `R12+(12-off)`」，故 **off_i = -4i**。
                //
                // ⚠ 这里原先写的是 `varOff = fnParams.Count; … = --varOff * 4;`（4、0、-4…），
                // 只在**单参数**时恰好等于 `-4*0`；从两个参数起整体错开一个槽：
                // 实测 `(define (pick a b) b)` 调 `(pick 11 22)` 得 **11**（读到的是 a 的槽），
                // `(define (add2 a b) (+ a b))` 调 `(add2 3 4)` 得 **26**。
                // 三个参数时更远。骨架照不出来 —— `skel.scm` 里的 `inc` 只有一个参数。
                for (int pi = 0; pi < fnParams.Count; pi++) vars[fnParams[pi]] = -4 * pi;
                // 局部变量从 BP 下方开始（R12-8 …）——形参在 BP 上方，两者不再共用计数器
                int savedPeak_fn = _peakVarOff;
                _peakVarOff = 4;
                varOff = 4;
                GenExpr(l.Items[2], true);
                // 回填帧大小（+32 安全边界；旧的 64 当保底，小函数行为不变）。
                // **按峰值算**：`let`/`do` 收尾会把 `varOff` 还原，用结束时的值会低估。
                int frameSize = _peakVarOff * 4 + 32;
                if (frameSize < 64) frameSize = 64;
                // 峰值记账是**按帧**的，出了这个函数要把外层的峰值还回去，
                // 否则内层函数的高峰值会白白撑大外层（无害，但也就失去了意义）
                _peakVarOff = savedPeak_fn;
                instructions[framePatchIndex] = new Instruction(OpCode.SUB,
                    [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)], framePatchIndex);
                // 释放临时栈空间后恢复帧
                AddInstruction(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)]);
                EmitEpilogue();
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, afterFunc)]);
                _currentFunc = savedFunc;
                _currentFuncParams = savedParams;
                // 恢复顶层作用域（形参不进 `vars`，否则后面的顶层代码会把形参名当变量读）
                varOff = savedVarOffFn;
                vars.Clear();
                foreach (var kv in savedVarsFn) vars[kv.Key] = kv.Value;
            } else if (s.Name == "define" && l.Items.Count >= 3) {
                // (define name value)
                string name = ((SSym)l.Items[1]).Name;
                GenExpr(l.Items[2]);
                vars[name] = ++varOff * 4;
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
            } else if (s.Name == "set!" && l.Items.Count >= 3) {
                // (set! var value)
                GenExpr(l.Items[2]);
                string setVar = ((SSym)l.Items[1]).Name;
                if (vars.TryGetValue(setVar, out int soff))
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - soff}"), new Operand(OperandType.REGISTER, 0)]);
            } else if (s.Name == "print") {
                EmitPrintArg(() => GenExpr(l.Items[1]), IsStringArg(l.Items[1]));
            } else if (s.Name == "display") {
                EmitPrintArg(() => GenExpr(l.Items[1]), IsStringArg(l.Items[1]));
            } else if (s.Name == "newline") {
                EmitPrintNewline();
            } else if (s.Name == "let" && l.Items.Count >= 4 && l.Items[1] is SSym letName && l.Items[2] is SList letBindings) {
                // Named let: (let name ((var init) ...) body ...)
                // Desugar to: internal function + tail-recursive call
                var nlParams = new List<string>();
                var nlInits = new List<SExpr>();
                foreach (var b in letBindings.Items) {
                    if (b is SList binding && binding.Items.Count >= 2) {
                        nlParams.Add(((SSym)binding.Items[0]).Name);
                        nlInits.Add(binding.Items[1]);
                    }
                }
                int savedVarOffNl = varOff;
                string funcLabel = $"__nl_{labelCounter++}";
                string afterNl = NewLabel();
                Emit(OpCode.NOP, [], "; --------------------------------------------");
                var nlSourceDecl = $"(let {letName.Name} (";
                for (var i = 0; i < nlParams.Count; i++)
                {
                    nlSourceDecl += $"({nlParams[i]} ...)";
                    if (i < nlParams.Count - 1) nlSourceDecl += " ";
                }
                nlSourceDecl += ") ...)";
                Emit(OpCode.NOP, [], $"; source   : {nlSourceDecl}");
                Emit(OpCode.NOP, [], $"; function : {funcLabel} (named let {letName.Name})");
                foreach (var p in nlParams)
                {
                    Emit(OpCode.NOP, [], $"; param   : any {p}");
                }
                Emit(OpCode.NOP, [], $"; return   : any");
                Emit(OpCode.NOP, [], "; --------------------------------------------");
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterNl)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, funcLabel)]);
                // 帧占位 + 回填，同 `define`。⚠ 这里**此前一条 `sub R13` 都没有**：
                // 命名 let 的函数体不占帧，而它的局部量（`let` / `do` 绑定）照样按
                // `R12+(12-off)` 往下写 ⇒ 直接写进 **push 区**，被参数压栈冲掉。
                // 只有「函数体里没有局部量」的命名 let（如 skel 的骨架）才看不出来。
                EmitPrologue();
                int nlFramePatch = instructions.Count;
                instructions.Add(new Instruction(OpCode.SUB,
                    [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], nlFramePatch));
                var savedFuncNl = _currentFunc;
                var savedParamsNl = _currentFuncParams;
                _currentFunc = letName.Name;
                _currentFuncParams = nlParams.Count;
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, $"{letName.Name}_body")]);
                // 形参槽位同 `define`：**off_i = -4i**（详见那处的说明）。
                // 这里原先写 `varOff = nlParams.Count - 1; … = --varOff * 4;`（0、-4、-8…），
                // 只在**两个参数**时恰好正确；单参数读成 `R12+16`、三个及以上整体前移一槽。
                for (int pi = 0; pi < nlParams.Count; pi++) vars[nlParams[pi]] = -4 * pi;
                int savedPeak_nl = _peakVarOff;
                _peakVarOff = 4;
                varOff = 4;
                // 函数体是**多形式**（`(let name (bindings) body ...)`）—— 原先只生成 `l.Items[3]`，
                // 第二个形式起全部被静默丢掉：实测
                //   `(let lp ((a 1)(b 2)(c 3)) (display "NL3=") (display (+ a b c)) (newline))`
                // 只打印出 "NL3="，后面的 display/newline 一个字都没编出来。
                // 同 `begin` 的口径：前面的按值丢弃，最后一个留在尾位置（可被尾递归优化）。
                for (int bi = 3; bi < l.Items.Count - 1; bi++) GenExpr(l.Items[bi]);
                GenExpr(l.Items[l.Items.Count - 1], true);
                int nlFrameSize = _peakVarOff * 4 + 32;
                if (nlFrameSize < 64) nlFrameSize = 64;
                _peakVarOff = savedPeak_nl;
                instructions[nlFramePatch] = new Instruction(OpCode.SUB,
                    [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, nlFrameSize)], nlFramePatch);
                EmitEpilogue();
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, afterNl)]);
                _currentFunc = savedFuncNl;
                _currentFuncParams = savedParamsNl;
                varOff = savedVarOffNl;
                for (int i = nlInits.Count - 1; i >= 0; i--) {
                    GenExpr(nlInits[i]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                }
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, funcLabel)]);
                if (nlParams.Count > 0)
                    AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, nlParams.Count * 4)]);
            } else if (s.Name == "let") {
                // (let ((var1 val1) (var2 val2)) body)
                int savedVarOff = varOff;
                if (l.Items.Count >= 2 && l.Items[1] is SList bindings) {
                    foreach (var b in bindings.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            GenExpr(binding.Items[1]);
                            vars[vname] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                        }
                    }
                    // body 是**多形式**：原先只生成 `l.Items[2]`，第二个形式起被静默丢掉。
                    for (int bi = 2; bi < l.Items.Count; bi++) GenTopLevel(l.Items[bi]);
                }
                varOff = savedVarOff;
            } else if (s.Name == "let*") {
                // (let* ((var1 val1) (var2 val2)) body) — sequential bindings
                if (l.Items.Count >= 2 && l.Items[1] is SList bindings) {
                    foreach (var b in bindings.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            GenExpr(binding.Items[1]);
                            vars[vname] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                        }
                    }
                    // body 是**多形式**：原先只生成 `l.Items[2]`，第二个形式起被静默丢掉。
                    for (int bi = 2; bi < l.Items.Count; bi++) GenTopLevel(l.Items[bi]);
                }
            } else if (s.Name == "letrec") {
                // (letrec ((var1 val1) (var2 val2)) body)
                // Two-pass: allocate all slots first, then evaluate inits (mutually recursive)
                if (l.Items.Count >= 2 && l.Items[1] is SList bindingsRec) {
                    var recVars = new List<(string name, SExpr init)>();
                    foreach (var b in bindingsRec.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            vars[vname] = ++varOff * 4;
                            recVars.Add((vname, binding.Items[1]));
                        }
                    }
                    foreach (var (vname, init) in recVars) {
                        GenExpr(init);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - vars[vname]}"), new Operand(OperandType.REGISTER, 0)]);
                    }
                    // body 是**多形式**：原先只生成 `l.Items[2]`，第二个形式起被静默丢掉。
                    for (int bi = 2; bi < l.Items.Count; bi++) GenTopLevel(l.Items[bi]);
                }
            } else if (s.Name == "do" && l.Items.Count >= 3) {
                // (do ((var init step) ...) (test result ...) body ...)
                int savedVarOff = varOff;
                var steps = new List<(string name, SExpr? step)>();
                if (l.Items[1] is SList bindings) {
                    foreach (var b in bindings.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            GenExpr(binding.Items[1]);
                            vars[vname] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                            steps.Add((vname, binding.Items.Count >= 3 ? binding.Items[2] : null));
                        }
                    }
                }
                string loopStart = NewLabel(), loopEnd = NewLabel();
                Sta!.PushLoopLabels(loopEnd, loopStart);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, loopStart)]);
                if (l.Items[2] is SList testClause && testClause.Items.Count > 0) {
                    GenExpr(testClause.Items[0]);
                    string bodyLabel = NewLabel();
                    Sta!.EmitJumpIfFalse(bodyLabel);
                    for (int ri = 1; ri < testClause.Items.Count; ri++)
                        GenExpr(testClause.Items[ri]);
                    // 循环的值取「本 do 的第一个绑定变量」（既定语义）。
                    // ⚠ 不能再用 `vars.Values.Min()`：那是**整个作用域**的表，外层只要还有别的绑定
                    // （函数形参、内层 define）就会选错变量；形参改负偏移后更会直接落到形参上。
                    if (steps.Count > 0 && vars.TryGetValue(steps[0].name, out int fo)) {
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{12 - fo}")]);
                    }
                    AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopEnd)]);
                    AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, bodyLabel)]);
                }
                for (int bi = 3; bi < l.Items.Count; bi++)
                    GenExpr(l.Items[bi]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                foreach (var (name, step) in steps) {
                    if (step != null && vars.TryGetValue(name, out int soff)) {
                        GenExpr(step);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - soff}"), new Operand(OperandType.REGISTER, 0)]);
                    }
                }
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopStart)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, loopEnd)]);
                Sta!.PopLoopLabels();
                varOff = savedVarOff;
            } else if (s.Name == "list") {
                // (list a b ...) — build cons chain from right to left
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // nil
                for (int idx = l.Items.Count - 1; idx >= 1; idx--) {
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push accumulated list
                    GenExpr(l.Items[idx]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push new element
                    EmitAlloc(8); // alloc 8 bytes
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // cdr = accumulated list
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // car = element
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                }
            } else if (s.Name == "append" && l.Items.Count >= 3) {
                // (append a b) — copy cons cells of a, last cdr points to b
                GenExpr(l.Items[1]);  // a in R0
                string apEnd = NewLabel(), apLoop = NewLabel(), apNext = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // stack[-1]=list a
                // Check if a is null
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, apEnd)]); // if a==0, return a (which is 0)
                // Copy first cell
                EmitAlloc(8);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // stack[-2]=head
                // Copy car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load a
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13+8")]); // load head
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]); // head.car = car(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load a
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // a = cdr(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // head
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // stack[-3]=cur (=head)
                // Loop: copy remaining cells
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, apLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // load a
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, apNext)]); // if a==0, done with copy
                // alloc new cell
                EmitAlloc(8); // R0 = new cell
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save new cell addr
                // cur.cdr = new
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // new → will be cur.cdr
                // Copy car to new cell
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+12")]); // load a
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car(a)
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // new cell addr in R1
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]); // new.car = car(a)
                // cur.cdr = new
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // old cur
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]); // cur.cdr = new
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]); // cur = new
                // a = cdr(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // load a
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr(a)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // a = cdr(a)
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, apLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, apNext)]);
                // cur.cdr = b
                GenExpr(l.Items[2]); // b in R0
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // cur
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1+4")]); // cur.cdr = b
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // head in R0
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop a
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, apEnd)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, apEnd)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // a (or b if a was null)
            } else if (s.Name == "quote" && l.Items.Count >= 2) {
                if (l.Items[1] is SInt qi) AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, qi.Value)]);
                else if (l.Items[1] is SList ql) {
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                    for (int q = ql.Items.Count - 1; q >= 0; q--) {
                        AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                        if (ql.Items[q] is SInt qv) AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, qv.Value)]);
                        else AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                        AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                EmitAlloc(8);
                        AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                        AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                    }
                } else AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
            } else if (s.Name == "member" && l.Items.Count >= 3) {
                // (member x lst) — find x in lst, return sublist or 0
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // target
                GenExpr(l.Items[2]); // lst
                string mbLoop = NewLabel(), mbFound = NewLabel(), mbEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // current list
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mbLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // peek current
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, mbEnd)]); // null → not found
                // Compare car with target
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car(lst)
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // target
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.JE, [new Operand(OperandType.LABEL, mbFound)]);
                // cdr → next
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, mbLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mbFound)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop lst
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop target
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, mbEnd)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mbEnd)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop lst (or return current=0)
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // target
            } else if (s.Name == "assoc" && l.Items.Count >= 3) {
                // (assoc key alist) — lookup key in association list
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // key
                GenExpr(l.Items[2]); // alist
                string asLoop = NewLabel(), asFound = NewLabel(), asEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, asLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, asEnd)]);
                // caar(alist) == key?
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car(pair)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // caar
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // key
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.JE, [new Operand(OperandType.LABEL, asFound)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, asLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, asFound)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, asEnd)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, asEnd)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // key
            } else if (s.Name == "remove" && l.Items.Count >= 3) {
                // (remove x lst) — return list without elements equal to x
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // target
                GenExpr(l.Items[2]); // lst
                string rmLoop = NewLabel(), rmEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // current list
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // result = nil
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rmLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load lst
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, rmEnd)]); // null → done
                // car(lst) vs target
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // target
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                string rmSkip = NewLabel();
                AddInstruction(OpCode.JE, [new Operand(OperandType.LABEL, rmSkip)]); // skip if equal
                // Not equal: cons to result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car again
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save car
                EmitAlloc(8); // alloc
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]); // new.car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]); // new.cdr = result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // result = new
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rmSkip)]);
                // cdr(lst) → next
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, rmLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rmEnd)]);
                // Result is in reverse order - reverse it back
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop old lst
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // target
                // Pop result (reversed), now reverse it
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // reversed result in R1
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save target
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // final result = nil
                string rvLoop = NewLabel(), rvEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save final result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]); // walk reversed
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rvLoop)]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, rvEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // current final
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // car
                // cons(car, final)
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]); // push car again
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push final
                EmitAlloc(8);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // final = cons
                // cdr(reversed)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // reversed
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]); // reversed = cdr
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, rvLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rvEnd)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // final result in R0
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop reversed
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop target
            } else if (s.Name == "list-ref" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); // lst
                GenExpr(l.Items[2]); // k
                // Walk lst by k steps, then return car
                string lrLoop = NewLabel(), lrEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save k
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lrLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // k
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, lrEnd)]); // k==0 → return car
                // lst = cdr(lst)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                // k--
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, lrLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lrEnd)]);
                // Return car(lst)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop k
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // discard lst
            } else if (s.Name == "list-tail" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); // lst
                GenExpr(l.Items[2]); // k
                string ltLoop = NewLabel(), ltEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save k
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, ltLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // k
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, ltEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, ltLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, ltEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop k
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // discard old lst
            } else if (s.Name == "map" && l.Items.Count >= 3 && l.Items[1] is SSym mapFn) {
                // (map f lst) — apply f to each element, return list of results
                // Compile-time expansion: f must be a known function name
                string mapEnd = NewLabel(), mapLoop = NewLabel();
                GenExpr(l.Items[2]); // lst
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save list
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // result = nil
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save result
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mapLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load lst
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, mapEnd)]);
                // car(lst) → arg
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push arg
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, mapFn.Name)]); // call f
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop arg
                // cons(result, acc)
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save f result
                EmitAlloc(8); // alloc
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // f result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]); // new.car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]); // new.cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // result = new
                // cdr(lst)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, mapLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mapEnd)]);
                // result is in reverse order; inline reverse before returning
                string mapRevLoop = NewLabel(), mapRevEnd = NewLabel();
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // load reversed result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // use lst slot for reverse input
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // result = nil
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mapRevLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, mapRevEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                EmitAlloc(8);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, mapRevLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mapRevEnd)]);
                // pop result and lst, result in R0
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop reverse result
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // R0 = result
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop original lst
            } else if (s.Name == "filter" && l.Items.Count >= 3 && l.Items[1] is SSym filterFn) {
                // (filter pred lst) — keep elements where (pred elem) is true
                string fltEnd = NewLabel(), fltLoop = NewLabel(), fltSkip = NewLabel();
                GenExpr(l.Items[2]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, fltLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, fltEnd)]);
                // car(lst) → call pred
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, filterFn.Name)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, fltSkip)]);
                // pred returned true: cons to result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                EmitAlloc(8);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, fltSkip)]);
                // cdr(lst)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, fltLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, fltEnd)]);
                // result is in reverse order; inline reverse before returning
                string fltRevLoop = NewLabel(), fltRevEnd = NewLabel();
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // load reversed result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // use lst slot for reverse input
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // result = nil
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, fltRevLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, fltRevEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                EmitAlloc(8);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, fltRevLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, fltRevEnd)]);
                // pop result and lst, result in R0
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop reverse result
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // R0 = result
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop original lst
            } else if (s.Name == "reverse" && l.Items.Count >= 2) {
                // (reverse lst) — build reversed cons chain
                GenExpr(l.Items[1]); // lst in R0
                string rvLoop = NewLabel(), rvEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // stack: lst
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // result = nil
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // stack: result, lst
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rvLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load lst
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, rvEnd)]); // if null, done
                // car(lst)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save car
                // cdr(lst) → update lst
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // lst = cdr
                // cons(car, result)
                EmitAlloc(8); // alloc 8
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]); // new.car = car
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]); // new.cdr = result
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // result = new
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, rvLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, rvEnd)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop lst
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // result in R0
            } else if (s.Name == "cons" && l.Items.Count >= 3) {
                // (cons a b) — allocate pair, store car/cdr, return addr
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                EmitAlloc(8); // alloc 8 bytes
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);     // cdr value
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);     // car value
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
            } else if (s.Name == "car" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);  // pair address in R0
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]);
            } else if (s.Name == "cdr" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);  // pair address in R0
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
            } else if (s.Name == "length" && l.Items.Count >= 2) {
                // (length lst) — walk cons cells, count until 0
                GenExpr(l.Items[1]);
                string lenLoop = NewLabel(), lenEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save list ptr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // count = 0
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save count
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lenLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // peek list ptr
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, lenEnd)]); // if null, done
                // count++
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // load count
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // save count
                // list = cdr(list)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // load list ptr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]); // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // save list ptr
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, lenLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lenEnd)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // discard list ptr
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // count in R0
            } else if (s.Name == "pair?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JNE);
            } else if (s.Name == "chipasm" && l.Items.Count >= 2) {
                // chipasm: ignore, handled by later stages
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Scheme 通过 Lib/shared/vmlsys.c 调用系统功能
            } else if (s.Name.StartsWith("peek") && l.Items.Count >= 2) {
                string runtimeFn = "Scheme_" + s.Name;
                GenExpr(l.Items[1]); AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]);
            } else if (s.Name.StartsWith("poke") && l.Items.Count >= 3) {
                string runtimeFn = "Scheme_" + s.Name;
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, runtimeFn)]);
            } else if (s.Name == "set!" && l.Items.Count >= 3) {
                GenExpr(l.Items[2]);
                string setVarE = ((SSym)l.Items[1]).Name;
                if (vars.TryGetValue(setVarE, out int soffE))
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - soffE}"), new Operand(OperandType.REGISTER, 0)]);
            } else if (s.Name == "do" && l.Items.Count >= 3) {
                int savedVarOffDo = varOff;
                var stepsDo = new List<(string name, SExpr? step)>();
                if (l.Items[1] is SList bindingsDo) {
                    foreach (var b in bindingsDo.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            GenExpr(binding.Items[1]);
                            vars[vname] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                            stepsDo.Add((vname, binding.Items.Count >= 3 ? binding.Items[2] : null));
                        }
                    }
                }
                string loopStartDo = NewLabel(), loopEndDo = NewLabel(), bodyLbl = NewLabel();
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, loopStartDo)]);
                if (l.Items[2] is SList testClauseDo && testClauseDo.Items.Count > 0) {
                    GenExpr(testClauseDo.Items[0]);
                    Sta!.EmitJumpIfFalse(bodyLbl);
                    for (int ri = 1; ri < testClauseDo.Items.Count; ri++)
                        GenExpr(testClauseDo.Items[ri]);
                    AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopEndDo)]);
                    AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, bodyLbl)]);
                }
                for (int bi = 3; bi < l.Items.Count; bi++)
                    GenExpr(l.Items[bi]);
                foreach (var (name, step) in stepsDo) {
                    if (step != null && vars.TryGetValue(name, out int soff2)) {
                        GenExpr(step);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - soff2}"), new Operand(OperandType.REGISTER, 0)]);
                    }
                }
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopStartDo)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, loopEndDo)]);
                varOff = savedVarOffDo;
            } else if (s.Name == "abs" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                string absNeg = NewLabel(), absEnd = NewLabel();
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.JGE, [new Operand(OperandType.LABEL, absEnd)]);
                AddInstruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, absEnd)]);
            } else if (s.Name == "max" && l.Items.Count >= 3) {
                // Multi-arg: (max a b c ...) → compare each arg, keep max in R0
                GenExpr(l.Items[1]);
                for (int i = 2; i < l.Items.Count; i++) {
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                    GenExpr(l.Items[i]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                    string mxDone = NewLabel();
                    AddInstruction(OpCode.JGE, [new Operand(OperandType.LABEL, mxDone)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mxDone)]);
                }
            } else if (s.Name == "min" && l.Items.Count >= 3) {
                // Multi-arg: (min a b c ...) → compare each arg, keep min in R0
                GenExpr(l.Items[1]);
                for (int i = 2; i < l.Items.Count; i++) {
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                    GenExpr(l.Items[i]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                    string mnDone = NewLabel();
                    AddInstruction(OpCode.JLE, [new Operand(OperandType.LABEL, mnDone)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mnDone)]);
                }
            } else if (s.Name == "mod" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
            } else if (s.Name == "quotient" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.DIV, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
            } else if (s.Name == "remainder" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
            } else if (s.Name == "zero?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JE);
            } else if (s.Name == "positive?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JG);
            } else if (s.Name == "negative?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JL);
            } else if (s.Name == "even?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => {
                    GenExpr(l.Items[1]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.AND, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                }, OpCode.JE);
            } else if (s.Name == "odd?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => {
                    GenExpr(l.Items[1]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.AND, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                }, OpCode.JNE);
            } else if (s.Name == "begin") {
                foreach (var item in l.Items.Skip(1)) GenTopLevel(item);
            } else GenExpr(l);
            } else {
                foreach (var item in l.Items) GenTopLevel(item);
            }
        } else GenExpr(e);
    }

}
