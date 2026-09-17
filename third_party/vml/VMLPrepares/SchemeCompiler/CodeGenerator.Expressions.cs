using VMLAssembler;
using CompilerBase;

namespace SchemeCompiler;
public partial class CodeGenerator {
    void GenCall(SList l, bool tailPos = false) {
        string op = ((SSym)l.Items[0]).Name;
        // 尾位置**只有自递归**才能用 GenTailRecursive：它把实参搬进**当前帧**的形参槽、
        // 释放当前帧、再 `jmp <名>_body` —— 整个技巧成立的前提是「被调者与调用者共用同一个帧」。
        // 对别的函数（用户函数也好、库函数也好）那是错的：跳进 `_body` 等于**跳过序言**
        // （`push R15; push R12; move R12 R13`），被调者拿到的是调用者的 BP/返回地址。
        // 实测症状是「从用户函数里调库函数必崩」，而顶层直接调同一个库函数完全正常
        // （顶层 `_currentFunc == null`，走的本来就不是这条路）。
        if (tailPos && op == _currentFunc) {
            GenTailRecursive(l, op);
            return;
        }
        if ((op == "+" || op == "-" || op == "*" || op == "/") && l.Items.Count == 3) {
            bool isFloat = l.Items[1] is SDouble || l.Items[2] is SDouble;
            var pushOp = isFloat ? OpCode.FPUSH : OpCode.PUSH;
            var popOp = isFloat ? OpCode.FPOP : OpCode.POP;
            OpCode add = isFloat ? OpCode.FADD : OpCode.ADD;
            OpCode sub = isFloat ? OpCode.FSUB : OpCode.SUB;
            OpCode mul = isFloat ? OpCode.FMUL : OpCode.MUL;
            OpCode div = isFloat ? OpCode.FDIV : OpCode.DIV;
            GenExpr(l.Items[1]);
            AddInstruction(pushOp, [new Operand(OperandType.REGISTER, 0)]);
            GenExpr(l.Items[2]);
            AddInstruction(popOp, [new Operand(OperandType.REGISTER, 1)]);
            if (op == "+") AddInstruction(add, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
            else if (op == "-") AddInstruction(sub, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
            else if (op == "*") AddInstruction(mul, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
            else if (op == "/") AddInstruction(div, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
            if (isFloat) EmitF2I();
            return;
        }
        // exact->inexact: integer/char → float (I2F)
        if (op == "exact->inexact" && l.Items.Count == 2) {
            GenExpr(l.Items[1]);
            AddInstruction(OpCode.I2F, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]);
            return;
        }
        // inexact->exact: float → integer (F2I)
        if (op == "inexact->exact" && l.Items.Count == 2) {
            GenExpr(l.Items[1]);
            AddInstruction(OpCode.F2I, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0)]);
            return;
        }
        // conv 库函数 float_to_str 等: 首参数是 C float (F0), 必须 FPUSH 而非 PUSH (shared impl 用 movef 读取)
        bool floatConv = op is "float_to_str" or "float_to_wstr" or "float_to_ustr" or "ftoa";
        // Push arguments left to right
        for (int i = l.Items.Count - 1; i >= 1; i--) {
            GenExpr(l.Items[i]);
            AddInstruction(floatConv && i == 1 ? OpCode.FPUSH : OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
        }
        AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, op)]);
        AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, (l.Items.Count - 1) * 4)]);
    }

    void GenExpr(SExpr e, bool tailPos = false) {
        if (e is SInt i) { AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, i.Value)]); }
        else if (e is SBool sb) { AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, sb.Value ? 1 : 0)]); }
        else if (e is SDouble d) {
            // 浮点字面量: MOVEF 从数据段加载完整位模式到 F0 (避免截断小数部分, v1.66.64 修复)
            EmitLoadConstant((float)d.Value);
        }
        else if (e is SStr sStr) {
            string lbl = $"_str{nextStrId++}";
            dataSection[lbl] = sStr.Value;
            AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lbl)]);
        }
        else if (e is SSym sym) {
            if (vars.TryGetValue(sym.Name, out int off)) {
                if (sym.Name == "__static_link__") {
                    // Load static link value directly
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{12 - off}")]);
                } else {
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{12 - off}")]);
                }
            } else if (vars.TryGetValue("__static_link__", out int slOff)) {
                // Try accessing through static link (closure capture)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{12 - slOff}")]); // load static link
                // We don't know the exact offset in the enclosing scope, so skip for now
                // The enclosing vars are merged into the lambda's vars via savedVars
            }
        }
        else if (e is SList l && l.Items.Count > 0 && l.Items[0] is SSym sFirst) {
            if (sFirst.Name == "lambda") {
                // (lambda (args...) body)
                string lamLabel = $"__lambda_{labelCounter++}";
                // Generate function code inline (will be at the end of instruction stream)
                int savedPos = instructions.Count;
                if (l.Items[1] is SList lamPars) {
                    var lamParams = lamPars.Items.Select(p => ((SSym)p).Name).ToList();
                    Emit(OpCode.NOP, [], "; --------------------------------------------");
                    var lamSourceDecl = $"(lambda (";
                    for (var pi = 0; pi < lamParams.Count; pi++)
                    {
                        lamSourceDecl += lamParams[pi];
                        if (pi < lamParams.Count - 1) lamSourceDecl += " ";
                    }
                    lamSourceDecl += ") ...)";
                    Emit(OpCode.NOP, [], $"; source   : {lamSourceDecl}");
                    Emit(OpCode.NOP, [], $"; function : {lamLabel}");
                    foreach (var p in lamParams)
                    {
                        Emit(OpCode.NOP, [], $"; param   : any {p}");
                    }
                    Emit(OpCode.NOP, [], $"; return   : any");
                    Emit(OpCode.NOP, [], "; --------------------------------------------");
                }
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lamLabel)]);
                // 帧占位：**不能再写死 64** —— 固定 64 字节装不下真实函数的局部量，
                // 超出的部分直接写进调用方的帧（踩内存）。骨架照不出来，因为骨架的函数都极小。
                // 帧大小要等函数体生成完才知道，故先占位、最后回填（与 R 的 GenerateCode 同口径）。
                EmitPrologue();
                int framePatchIndex = instructions.Count;
                instructions.Add(new Instruction(OpCode.SUB,
                    [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 0)], framePatchIndex));
                // Parse params and body
                if (l.Items[1] is SList pars) {
                    int savedVarOff = varOff;
                    var savedVars = new Dictionary<string, int>(vars);
                    // Reset varOff for lambda-local bindings
                    // +1 accounts for static link slot (stored at R12-12)
                    varOff = pars.Items.Count + 1;
                    int savedStaticOff = --varOff * 4;
                    vars["__static_link__"] = savedStaticOff;
                    // 确保局部变量在 BP 下方
                    if (varOff < 4) varOff = 4;
                    // Store static link (passed as extra param via R1)
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12-{12 - savedStaticOff}")]);
                    foreach (var p in pars.Items) vars[((SSym)p).Name] = --varOff * 4;
                    // Restore saved vars for closure lookups
                    foreach (var kv in savedVars) {
                        if (!vars.ContainsKey(kv.Key))
                            vars[kv.Key] = kv.Value;
                    }
                    // Generate body
                    int savedVarCount = varOff;
                    if (l.Items.Count >= 3) GenExpr(l.Items[2]);
                    // Epilogue — 先释放临时栈空间
                    int frameSize = varOff * 4 + 32;
                    if (frameSize < 64) frameSize = 64;
                    instructions[framePatchIndex] = new Instruction(OpCode.SUB,
                        [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)], framePatchIndex);
                    AddInstruction(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, frameSize)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 12)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 12)]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 15)]);
                    AddInstruction(OpCode.RET, []);
                    vars.Clear();
                    foreach (var kv in savedVars) vars[kv.Key] = kv.Value;
                    varOff = savedVarOff;
                }
                // Load function address
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lamLabel)]);
            } else if (sFirst.Name == "if" && l.Items.Count >= 3) {
                Sta!.EmitIf(
                    () => GenExpr(l.Items[1]),
                    () => GenExpr(l.Items[2], tailPos),
                    l.Items.Count >= 4 ? () => GenExpr(l.Items[3], tailPos) : null);
            } else {
            if (sFirst.Name == "+" || sFirst.Name == "-") {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(sFirst.Name == "+" ? OpCode.ADD : OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
            } else if (sFirst.Name == "*" || sFirst.Name == "/") {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(sFirst.Name == "*" ? OpCode.MUL : OpCode.DIV, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
            } else if (sFirst.Name is "<" or ">" or "<=" or ">=" or "=" or "eq?" or "equal?") {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                string condTrue = NewLabel(), condEnd = NewLabel();
                var jmpOp = sFirst.Name switch {
                    "<" => OpCode.JL, ">" => OpCode.JG, "<=" => OpCode.JLE,
                    ">=" => OpCode.JGE, "=" or "eq?" or "equal?" => OpCode.JE, _ => OpCode.JE
                };
                AddInstruction(jmpOp, [new Operand(OperandType.LABEL, condTrue)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, condEnd)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, condTrue)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, condEnd)]);
            } else if (sFirst.Name == "not") {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JE);
            } else if (sFirst.Name == "and" || sFirst.Name == "or") {
                string andOrEnd = NewLabel();
                for (int idx = 1; idx < l.Items.Count; idx++) {
                    GenExpr(l.Items[idx], tailPos && idx == l.Items.Count - 1);
                    if (idx < l.Items.Count - 1) {
                        if (sFirst.Name == "and") Sta!.EmitJumpIfFalse(andOrEnd);
                        else Sta!.EmitJumpIfTrue(andOrEnd);
                    }
                }
                if (sFirst.Name == "or") {
                    // or: at end, if we got here all were false, R0=0
                }
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, andOrEnd)]);
            } else if (sFirst.Name == "cond") {
                var branches = new List<(System.Action, System.Action)>();
                System.Action? elseBody = null;
                for (int idx = 1; idx < l.Items.Count; idx++) {
                    if (l.Items[idx] is SList clause && clause.Items.Count >= 2) {
                        if (clause.Items[0] is SSym { Name: "else" }) {
                            elseBody = () => GenExpr(clause.Items[1], tailPos);
                        } else {
                            var test = clause.Items[0];
                            var body = clause.Items[1];
                            branches.Add((() => GenExpr(test), () => GenExpr(body, tailPos)));
                        }
                    }
                }
                Sta!.EmitIfChain(branches, elseBody);
            } else if (sFirst.Name == "case" && l.Items.Count >= 3) {
                // (case <key> ((<datum> ...) <expr>) ... (else <expr>))
                var emitCaseValues = new List<System.Action>();
                var caseBodies = new List<System.Action>();
                System.Action? defaultBody = null;
                for (int ci = 2; ci < l.Items.Count; ci++) {
                    if (l.Items[ci] is SList clause && clause.Items.Count >= 2) {
                        if (clause.Items[0] is SSym { Name: "else" }) {
                            defaultBody = () => GenExpr(clause.Items[1], tailPos);
                        } else if (clause.Items[0] is SList datums) {
                            foreach (var datum in datums.Items) {
                                var capClause = clause;
                                emitCaseValues.Add(() => GenExpr(datum));
                                caseBodies.Add(() => GenExpr(capClause.Items[1], tailPos));
                            }
                        }
                    }
                }
                Sta!.EmitSwitchCustom(
                    () => GenExpr(l.Items[1]),
                    emitCaseValues, caseBodies, defaultBody);
            } else if (sFirst.Name == "set!" && l.Items.Count >= 3) {
                GenExpr(l.Items[2]);
                string svar = ((SSym)l.Items[1]).Name;
                if (vars.TryGetValue(svar, out int soffE2))
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - soffE2}"), new Operand(OperandType.REGISTER, 0)]);
            } else if (sFirst.Name == "do" && l.Items.Count >= 3) {
                int svd = varOff;
                var sd = new List<(string name, SExpr? step)>();
                if (l.Items[1] is SList bd) {
                    foreach (var b in bd.Items) {
                        if (b is SList bind && bind.Items.Count >= 2) {
                            string vn = ((SSym)bind.Items[0]).Name;
                            GenExpr(bind.Items[1]);
                            vars[vn] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                            sd.Add((vn, bind.Items.Count >= 3 ? bind.Items[2] : null));
                        }
                    }
                }
                string ls = NewLabel(), le = NewLabel(), bl = NewLabel();
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, ls)]);
                if (l.Items[2] is SList tc && tc.Items.Count > 0) {
                    GenExpr(tc.Items[0]);
                    Sta!.EmitJumpIfFalse(bl);
                    for (int ri = 1; ri < tc.Items.Count; ri++) GenExpr(tc.Items[ri]);
                    // 循环的值取「本 do 的**第一个**绑定变量」（本前端的既定语义，见 `sd`）。
                    // ⚠ 不能用 `vars.Values.Min()` 找「第一个变量」：`vars` 是整个作用域的变量表，
                    // 只要外层还有别的绑定（函数形参、内层 define）就会选错 —— 实测
                    // `(define (f a b) (do ((i 0 (+ i 1)) (s 0 (+ s a))) ((= i 3) s)))` 调 `(f 10 20)`
                    // 得 20（b 的值）而不是第一个循环变量。形参改成负偏移后这个 Min 更会直接落到形参上。
                    if (sd.Count > 0 && vars.TryGetValue(sd[0].name, out int fo)) {
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12+{12 - fo}")]);
                    }
                    AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, le)]);
                    AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, bl)]);
                }
                for (int bi = 3; bi < l.Items.Count; bi++) GenExpr(l.Items[bi]);
                foreach (var (nm, st) in sd) {
                    if (st != null && vars.TryGetValue(nm, out int so2)) {
                        GenExpr(st);
                        AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - so2}"), new Operand(OperandType.REGISTER, 0)]);
                    }
                }
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, ls)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, le)]);
                varOff = svd;
            } else if (sFirst.Name == "begin") {
                for (int bi = 1; bi < l.Items.Count - 1; bi++) GenExpr(l.Items[bi]);
                if (l.Items.Count > 1) GenExpr(l.Items[l.Items.Count - 1], tailPos);
            } else if (sFirst.Name == "let") {
                // (let ((var1 val1) ...) body) — inside expressions
                int savedVarOffLet = varOff;
                var savedVarsLet = new Dictionary<string, int>(vars);
                if (l.Items.Count >= 2 && l.Items[1] is SList bindings) {
                    foreach (var b in bindings.Items) {
                        if (b is SList binding && binding.Items.Count >= 2) {
                            string vname = ((SSym)binding.Items[0]).Name;
                            GenExpr(binding.Items[1]);
                            vars[vname] = ++varOff * 4;
                            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 - varOff * 4}"), new Operand(OperandType.REGISTER, 0)]);
                        }
                    }
                    // body (may have multiple expressions like begin)
                    for (int bi = 2; bi < l.Items.Count - 1; bi++) GenExpr(l.Items[bi]);
                    if (l.Items.Count >= 3) GenExpr(l.Items[l.Items.Count - 1], tailPos);
                }
                varOff = savedVarOffLet;
                vars.Clear();
                foreach (var kv in savedVarsLet) vars[kv.Key] = kv.Value;
            } else if (sFirst.Name == "number?" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
            } else if (sFirst.Name == "boolean?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JLE, 1);
            } else if (sFirst.Name == "list?" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
            } else if (sFirst.Name is "symbol?" or "string?" or "procedure?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JNE);
            } else if (sFirst.Name == "string-length" && l.Items.Count >= 2) {
                // CALL shared_strlen
                GenExpr(l.Items[1]);  // R0 = string address
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strlen")]);
            } else if (sFirst.Name == "string-ref" && l.Items.Count >= 3) {
                // CALL shared_str_charat(str, index)
                GenExpr(l.Items[1]);  // R0 = string address
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]);  // R0 = index
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "str_charat")]);
            } else if (sFirst.Name == "string-set!" && l.Items.Count >= 4) {
                // (string-set! str index char) — mutate character at index
                GenExpr(l.Items[1]);  // R0 = string address
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]);  // R0 = index
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]); // R1 = str + index
                GenExpr(l.Items[3]);  // R0 = char value
                AddInstruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]); // store byte
            } else if (sFirst.Name == "make-string" && l.Items.Count >= 3) {
                // (make-string k char) — allocate string of length k filled with char
                GenExpr(l.Items[1]);  // R0 = k
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push k
                GenExpr(l.Items[2]);  // R0 = char
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // push char
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "Scheme_make_string")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)]); // pop args
            } else if (sFirst.Name == "string=?" && l.Items.Count >= 3) {
                // (string=? str1 str2) — compare two strings byte by byte
                GenExpr(l.Items[1]);  // R0 = str1
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]);  // R0 = str2
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]);
                string sqLoop = NewLabel(), sqTrue = NewLabel(), sqFalse = NewLabel(), sqEnd = NewLabel();
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, sqLoop)]);
                AddInstruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R1")]);
                AddInstruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2")]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.JNE, [new Operand(OperandType.LABEL, sqFalse)]);
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.JE, [new Operand(OperandType.LABEL, sqTrue)]);
                AddInstruction(OpCode.INC, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.INC, [new Operand(OperandType.REGISTER, 2)]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, sqLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, sqTrue)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, sqEnd)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, sqFalse)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, sqEnd)]);
            } else if (sFirst.Name == "substring" && l.Items.Count >= 4) {
                // (substring str start end) — allocate new string, copy [start, end)
                GenExpr(l.Items[1]);  // R0 = str
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]);  // R0 = start
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[3]);  // R0 = end
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "Scheme_substring")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 12)]);
            } else if (sFirst.Name == "string-append" && l.Items.Count >= 3) {
                // (string-append str1 str2 ...) — concatenate all strings
                for (int si = l.Items.Count - 1; si >= 1; si--) {
                    GenExpr(l.Items[si]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                }
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, l.Items.Count - 1)]); // arg count
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "Scheme_string_append")]);
                int totalArgBytes = (l.Items.Count) * 4; // count + all strings
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalArgBytes)]);
            } else if (sFirst.Name == "string-copy" && l.Items.Count >= 2) {
                // (string-copy str) — allocate new string, copy contents
                GenExpr(l.Items[1]);  // R0 = str
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "Scheme_string_copy")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
            } else if (sFirst.Name == "apply" && l.Items.Count >= 3) {
                // (apply f args...) — last arg must be a list
                // Push all args except the list
                for (int ai = l.Items.Count - 2; ai >= 2; ai--) {
                    GenExpr(l.Items[ai]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                }
                // Evaluate function value (supports both symbol and expression)
                bool applyIsSym = l.Items[1] is SSym;
                string applyFn = applyIsSym ? ((SSym)l.Items[1]).Name : "";
                if (!applyIsSym) GenExpr(l.Items[1]); // evaluate function expression
                // Now push list elements onto stack
                GenExpr(l.Items[l.Items.Count - 1]); // last arg = list
                string apLoop = NewLabel(), apEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save list ptr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]); // count = 0
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // count on stack
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, apLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]); // peek list
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, apEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                // count++
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+8")]);
                // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, apLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, apEnd)]);
                // Pop count and list
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // count
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop list
                // CALL f
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, applyFn)]);
                // Clean up stack
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 1)]);
                int explicitArgs = l.Items.Count - 3;
                if (explicitArgs > 0)
                    AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, explicitArgs * 4)]);
            } else if (sFirst.Name == "for-each" && l.Items.Count >= 3 && l.Items[1] is SSym feFn) {
                // (for-each f lst) — apply f to each element for side effects
                string feEnd = NewLabel(), feLoop = NewLabel();
                GenExpr(l.Items[2]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, feLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, feEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // car
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, feFn.Name)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]);
                // cdr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, feLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, feEnd)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]); // pop lst
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
            } else if (sFirst.Name == "cons" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                EmitAlloc(8);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
            } else if (sFirst.Name == "car" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]);
            } else if (sFirst.Name == "cdr" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
            } else if (sFirst.Name == "list" && l.Items.Count >= 2) {
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                for (int idx = l.Items.Count - 1; idx >= 1; idx--) {
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                    GenExpr(l.Items[idx]);
                    AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                EmitAlloc(8);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0+4")]);
                    AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]);
                }
            } else if (sFirst.Name == "length" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                string lenLoop = NewLabel(), lenEnd = NewLabel();
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lenLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JZ, [new Operand(OperandType.LABEL, lenEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]);
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, lenLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, lenEnd)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]);
            } else if (sFirst.Name == "pair?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JNZ);
            } else if (sFirst.Name == "vector?" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]);
            } else if (sFirst.Name == "vector" && l.Items.Count >= 1) {
                // (vector elem1 elem2 ...) — literal vector constructor
                int vecLen = l.Items.Count - 1;
                // Allocate vecLen*4 + 4 bytes (length + elements)
                EmitAlloc(vecLen * 4 + 4);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save vec addr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, vecLen)]);
                // ⚠ `MOVE dest, src` **dest 在前**。原来写成 `MOVE R1, [R0]`（**读**），
                //   长度没写进去且把 R1 冲成垃圾；元素那两句同病（`MOVE R0, [R1+n]` 也是读），
                //   ⇒ `(vector 1 2 3 4)` 建出来的块**一个元素都没写**，全是未初始化堆内存。
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)]); // vec[0] = len
                for (int vi = 1; vi < l.Items.Count; vi++) {
                    GenExpr(l.Items[vi]);
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // vec addr
                    AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{vi * 4}"), new Operand(OperandType.REGISTER, 0)]); // vec[vi] = 元素值
                }
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // vec addr in R0
            } else if (sFirst.Name == "unless" && l.Items.Count >= 3) {
                // (unless test body ...) = (if (not test) (begin body ...))
                GenExpr(l.Items[1]);
                string unlEnd = NewLabel();
                Sta!.EmitJumpIfTrue(unlEnd);
                for (int ui = 2; ui < l.Items.Count; ui++)
                    GenTopLevel(l.Items[ui]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, unlEnd)]);
            } else if (sFirst.Name == "when" && l.Items.Count >= 3) {
                // (when test body ...) = (if test (begin body ...))
                GenExpr(l.Items[1]);
                string whnEnd = NewLabel();
                Sta!.EmitJumpIfFalse(whnEnd);
                for (int wi = 2; wi < l.Items.Count; wi++)
                    GenTopLevel(l.Items[wi]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, whnEnd)]);
            } else if (sFirst.Name == "char->integer" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]); // char is just an integer in VML
            } else if (sFirst.Name == "integer->char" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]); // char and integer are the same in VML
            } else if (sFirst.Name == "symbol->string" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]); // symbols are stored as string labels in VML
            } else if (sFirst.Name == "string->symbol" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]); // same representation
            } else if (sFirst.Name == "make-vector" && l.Items.Count >= 2) {
                // (make-vector k [fill])
                GenExpr(l.Items[1]); // k
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save k
                SExpr fill = l.Items.Count >= 3 ? l.Items[2] : new SInt(0);
                GenExpr(fill);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // R1 = k
                // Allocate k*4 + 4 bytes (length + elements)
                AddInstruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save fill
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]);
                EmitAlloc();
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // R1 = fill
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // save vec addr
                // Store length at vec[0]
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R13+4")]); // k (saved above)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0")]); // vec[0] = k
                // Fill elements
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // restore fill
                string mvLoop = NewLabel(), mvEnd = NewLabel();
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0)]); // i = 0
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]); // save i
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mvLoop)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // i
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R13+8")]); // k
                AddInstruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2)]);
                AddInstruction(OpCode.JGE, [new Operand(OperandType.LABEL, mvEnd)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, $"R13+{(1+2)*4}")]); // vec addr
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]); // i
                AddInstruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R2+4")]); // vec[i+1] = fill (R0 still has fill)
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // i
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13")]); // i++
                AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, mvLoop)]);
                AddInstruction(OpCode.LABEL, [new Operand(OperandType.LABEL, mvEnd)]);
                // Pop i, load vec addr to R0
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)]); // pop k and i
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // vec addr in R0
            } else if (sFirst.Name == "vector-ref" && l.Items.Count >= 3) {
                GenExpr(l.Items[1]); // vec
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); // index
                AddInstruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // vec addr
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]);
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0+4")]);
            } else if (sFirst.Name == "vector-set!" && l.Items.Count >= 4) {
                // ⚠ 两处都错（`vector-ref` 那句方向本来是对的，可作对照）：
                //   ① `MOVE dest, src` **dest 在前** —— 原来是 `MOVE R0, [R1+4]`（**读**），
                //      `vector-set!` 整个成了空操作，元素一个都写不进去。
                //   ② **不能指望 R1 活过右值求值**：`(vector-set! a i (inc …))` 里的 `inc`
                //      是函数调用，它算 `(+ x 1)` 就会用掉 R1（实测调用返回后 R1 == 1）
                //      ⇒ 若直接写 `MOVE [R1+4], R0`，会往地址 5 写。
                //   所以先把**右值压栈**，算完地址再取回来（右值在 R0，地址在 R1，最后落笔）。
                GenExpr(l.Items[3]);                                                 // R0 = 右值
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]); // 存右值
                GenExpr(l.Items[1]); // vec
                AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
                GenExpr(l.Items[2]); // index
                AddInstruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]);
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]); // vec addr
                AddInstruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]); // R1 = vec + idx*4
                AddInstruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]); // R0 = 右值
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1+4"), new Operand(OperandType.REGISTER, 0)]); // vec[idx] = 右值
            } else if (sFirst.Name == "vector-length" && l.Items.Count >= 2) {
                GenExpr(l.Items[1]); // vec
                AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]); // vec[0] = length
            } else if (sFirst.Name == "null?" && l.Items.Count >= 2) {
                EmitCompareToBool(() => GenExpr(l.Items[1]), OpCode.JE);
            } else if (sFirst.Name == "display") {
                EmitPrintArg(() => GenExpr(l.Items[1]), IsStringArg(l.Items[1]));
            } else if (sFirst.Name == "print") {
                EmitPrintArg(() => GenExpr(l.Items[1]), IsStringArg(l.Items[1]));
            } else if (sFirst.Name == "newline") {
                EmitPrintNewline();
            } else GenCall(l, tailPos);
    }
}
}

    // display/print 参数判断: 字符串字面量 或 返回字符串的函数调用 (int_to_str 等)
    static bool IsStringArg(SExpr arg) =>
        arg is SStr
        || (arg is SList sl && sl.Items.Count >= 1 && sl.Items[0] is SSym sym && IsStringReturningFunc(sym.Name));

    void GenTailRecursive(SList l, string targetFunc) {
        int argCount = l.Items.Count - 1;
        for (int i = l.Items.Count - 1; i >= 1; i--) {
            GenExpr(l.Items[i]);
            AddInstruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]);
        }
        for (int i = 0; i < argCount; i++) {
            AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R13+{i * 4}")]);
            AddInstruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R12+{12 + i * 4}"), new Operand(OperandType.REGISTER, 0)]);
        }
        AddInstruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 12)]);
        AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, $"{targetFunc}_body")]);
    }

}
