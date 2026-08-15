// =============================================================
// CodeGen.cs —— 语句到字节码的编译
//
// 把 Parser 产出的语句列表编译为 Chunk。负责：
//   - 表达式树 → 栈式指令（含幂、整除、字段数组、DEF FN 内联、例程函数）
//   - 语句 → 指令序列（含全部图形/交互语句）
//   - 例程（SUB/FUNCTION）编译到主代码之后，CallRoutine 跳转 + EndRoutine 返回
//   - GOTO/GOSUB/ON ERROR：收集标签地址，编译结束后统一回填
//   - RESTORE 按 DATA 标签定位数据偏移
// =============================================================

namespace QBasic.Compiler;

/// <summary>编译器：语句列表 → Chunk。</summary>
public sealed class CodeGen
{
    private readonly Chunk _chunk = new();
    private readonly List<Routine> _routines;
    private readonly Dictionary<string, DefFn> _defFns;
    private readonly Dictionary<string, UserType> _types;
    private readonly Dictionary<string, int> _labelAddr = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _dataLabelIdx = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _routineAddr = new(StringComparer.OrdinalIgnoreCase);
    // 待回填跳转：(操作数偏移, 标签名/数字, 是否数字标签)
    private readonly List<(int opOffset, string name, double num, bool isNum)> _patches = new();
    // 待回填例程调用：(操作数偏移, 例程名)
    private readonly List<(int opOffset, string name)> _routinePatches = new();
    // 待回填 ON ERROR 目标：(操作数偏移, 标签名)
    private readonly List<(int opOffset, string label)> _errPatches = new();

    private int _selTmp;

    public CodeGen(List<Routine>? routines = null, Dictionary<string, DefFn>? defFns = null, Dictionary<string, UserType>? types = null)
    {
        _routines = routines ?? new List<Routine>();
        _defFns = defFns ?? new Dictionary<string, DefFn>(StringComparer.OrdinalIgnoreCase);
        _types = types ?? new Dictionary<string, UserType>(StringComparer.OrdinalIgnoreCase);
    }

    public Chunk Compile(List<Stmt> stmts, List<DataItem>? data = null)
    {
        if (data != null) _chunk.Data.AddRange(data);
        var all = new List<Stmt>(stmts);
        foreach (var r in _routines) all.AddRange(r.Body);
        CollectLabels(all);
        // 主代码
        foreach (var s in stmts)
            if (s.Kind != StmtKind.Rem)
                CompileStmt(s);
        _chunk.Emit(OpCode.Halt, 0);
        // 例程代码
        foreach (var r in _routines)
        {
            _routineAddr[r.Name] = _chunk.Address;
            foreach (var s in r.Body)
                if (s.Kind != StmtKind.Rem)
                    CompileStmt(s);
            _chunk.Emit(OpCode.EndRoutine, r.Body.Count > 0 ? r.Body[0].Line : 0);
        }
        // 回填跳转
        foreach (var (opOffset, name, num, isNum) in _patches)
        {
            string key = isNum ? "#" + num.ToString("0") : name;
            if (_labelAddr.TryGetValue(key, out int addr))
                _chunk.PatchOperand(opOffset, addr);
            else
                throw new CompileException($"未定义的标签 '{name}'");
        }
        foreach (var (opOffset, name) in _routinePatches)
        {
            if (_routineAddr.TryGetValue(name, out int addr))
                _chunk.PatchOperand(opOffset, addr);
            else
                throw new CompileException($"未定义的例程 '{name}'");
        }
        foreach (var (opOffset, label) in _errPatches)
        {
            if (_labelAddr.TryGetValue(label, out int addr))
                _chunk.PatchOperand(opOffset, addr);
            else
                throw new CompileException($"未定义的 ON ERROR 标签 '{label}'");
        }
        return _chunk;
    }

    private void CollectLabels(List<Stmt> stmts)
    {
        foreach (var s in stmts)
        {
            if (s.Kind == StmtKind.Label)
            {
                string key = s.LabelIsNum ? "#" + s.LabelNum.ToString("0") : s.LabelName;
                if (!_labelAddr.ContainsKey(key))
                    _labelAddr[key] = -1;
                if (!s.LabelIsNum && s.LabelDataIdx >= 0 && !_dataLabelIdx.ContainsKey(key))
                    _dataLabelIdx[key] = s.LabelDataIdx;
            }
            else if (s.Kind == StmtKind.If)
            {
                CollectLabels(s.ThenStmts);
                CollectLabels(s.ElseStmts);
                if (s.SingleLineThen != null) CollectLabels(s.SingleLineThen);
                if (s.SingleLineElseStmts != null) CollectLabels(s.SingleLineElseStmts);
            }
            else if (s.Kind == StmtKind.For) CollectLabels(s.Body);
            else if (s.Kind == StmtKind.While) CollectLabels(s.Body);
            else if (s.Kind == StmtKind.DoLoop) CollectLabels(s.Body);
            else if (s.Kind == StmtKind.SelectCase && s.Cases != null)
                foreach (var c in s.Cases) CollectLabels(c.Body);
        }
    }

    private void CompileStmt(Stmt s)
    {
        switch (s.Kind)
        {
            case StmtKind.Label:
            {
                string key = s.LabelIsNum ? "#" + s.LabelNum.ToString("0") : s.LabelName;
                if (_labelAddr.TryGetValue(key, out int addr) && addr < 0)
                    _labelAddr[key] = _chunk.Address;
                break;
            }
            case StmtKind.Let: CompileLet(s); break;
            case StmtKind.Print: CompilePrint(s); break;
            case StmtKind.Input: CompileInput(s); break;
            case StmtKind.If: CompileIf(s); break;
            case StmtKind.For: CompileFor(s); break;
            case StmtKind.While: CompileWhile(s); break;
            case StmtKind.Goto: CompileGoto(s, false); break;
            case StmtKind.Gosub: CompileGoto(s, true); break;
            case StmtKind.Return: _chunk.Emit(OpCode.Return, s.Line); break;
            case StmtKind.End: _chunk.Emit(OpCode.Halt, s.Line); break;
            case StmtKind.Dim: CompileDim(s); break;
            case StmtKind.SelectCase: CompileSelect(s); break;
            case StmtKind.DoLoop: CompileDoLoop(s); break;
            case StmtKind.Randomize: _chunk.Emit(OpCode.Randomize, s.Line); break;
            case StmtKind.Read:
                if (s.ReadVars != null)
                    for (int ri = 0; ri < s.ReadVars.Count; ri++)
                    {
                        string v = s.ReadVars[ri];
                        int vidx = _chunk.ResolveVar(v);
                        if (s.ReadIndexes != null && ri < s.ReadIndexes.Count && s.ReadIndexes[ri] != null)
                        {
                            // READ 到数组元素：读 DATA 到临时，再存入数组
                            int tmp = _chunk.ResolveVar("~rd_" + v);
                            _chunk.Emit(OpCode.Read, tmp, s.Line);
                            _chunk.ArrayVars.Add(vidx);
                            CompileExpr(s.ReadIndexes[ri]!);
                            _chunk.Emit(OpCode.VarLoad, tmp, s.Line);
                            _chunk.Emit(OpCode.ArrStore, vidx, s.Line);
                        }
                        else
                            _chunk.Emit(OpCode.Read, vidx, s.Line);
                    }
                break;
            case StmtKind.Restore:
            {
                int idx = 0;
                if (!s.TargetIsNum && s.Target != "")
                {
                    string key = s.Target;
                    if (!_dataLabelIdx.TryGetValue(key, out idx)) idx = 0;
                }
                _chunk.Emit(OpCode.Restore, idx, s.Line);
                break;
            }
            // ---- 图形 / 交互 ----
            case StmtKind.Screen: CompileExpr(s.Fg!); _chunk.Emit(OpCode.GfxScreen, s.Line); break;
            case StmtKind.Cls: _chunk.Emit(OpCode.GfxCls, s.Line); break;
            case StmtKind.Line: CompileLine(s); break;
            case StmtKind.Circle: CompileCircle(s); break;
            case StmtKind.Pset: CompileExpr(s.X1!); CompileExpr(s.Y1!); CompileExpr(s.ColorExpr ?? Expr.NumLit(7)); _chunk.Emit(OpCode.GfxPset, s.Line); break;
            case StmtKind.Paint: CompileExpr(s.X1!); CompileExpr(s.Y1!); CompileExpr(s.ColorExpr ?? Expr.NumLit(7)); CompileExpr(s.X2 ?? s.ColorExpr ?? Expr.NumLit(7)); _chunk.Emit(OpCode.GfxPaint, s.Line); break;
            case StmtKind.Color: CompileExpr(s.Fg!); CompileExpr(s.Bg ?? Expr.NumLit(0)); _chunk.Emit(OpCode.GfxColor, s.Line); break;
            case StmtKind.Palette: CompileExpr(s.Fg!); CompileExpr(s.Bg ?? Expr.NumLit(0)); _chunk.Emit(OpCode.GfxPalette, s.Line); break;
            case StmtKind.Locate: CompileExpr(s.Row!); CompileExpr(s.Col ?? Expr.NumLit(1)); _chunk.Emit(OpCode.GfxLocate, s.Line); break;
            case StmtKind.GetSprite:
            {
                int varIdx = _chunk.ResolveVar(s.SpriteVar);
                _chunk.ArrayVars.Add(varIdx);
                CompileExpr(s.X1!); CompileExpr(s.Y1!); CompileExpr(s.X2!); CompileExpr(s.Y2!);
                _chunk.Emit(OpCode.GfxGet, varIdx, s.Line);
                break;
            }
            case StmtKind.PutSprite:
            {
                int varIdx = _chunk.ResolveVar(s.SpriteVar);
                _chunk.ArrayVars.Add(varIdx);
                CompileExpr(s.X1!); CompileExpr(s.Y1!);
                EmitPut(varIdx, s.SpriteXor ? 1 : 0, s.Line);
                break;
            }
            case StmtKind.Play:
                _chunk.Emit(OpCode.ConstStr, _chunk.AddConstStr(s.PlayStr), s.Line);
                _chunk.Emit(OpCode.GfxPlay, s.Line);
                break;
            case StmtKind.Sleep: CompileExpr(s.SleepSec!); _chunk.Emit(OpCode.GfxSleep, s.Line); break;
            case StmtKind.Beep: _chunk.Emit(OpCode.Beep, s.Line); break;
            case StmtKind.Width: CompileExpr(s.Fg!); _chunk.Emit(OpCode.GfxWidth, s.Line); break;
            case StmtKind.SubCall: CompileSubCall(s); break;
            case StmtKind.OnError: CompileOnError(s); break;
            case StmtKind.Resume: _chunk.Emit(OpCode.Resume, s.ResumeMode, s.Line); break;
            case StmtKind.LineInput:
                if (s.Value != null)
                {
                    CompileExpr(s.Value);
                    _chunk.Emit(OpCode.PrintSemicolon, s.Line);
                }
                _chunk.Emit(OpCode.LineInput, _chunk.ResolveVar(s.VarName), s.Line);
                break;
        }
    }

    /// <summary>GfxPut 带双操作数 [varIdx][mode]。</summary>
    private void EmitPut(int varIdx, int mode, int line)
    {
        _chunk.Code.Add((byte)OpCode.GfxPut);
        _chunk.Lines.Add(line);
        _chunk.Code.Add((byte)(varIdx & 0xFF));
        _chunk.Code.Add((byte)((varIdx >> 8) & 0xFF));
        _chunk.Code.Add((byte)(mode & 0xFF));
        _chunk.Code.Add((byte)((mode >> 8) & 0xFF));
        for (int i = 0; i < 4; i++) _chunk.Lines.Add(line);
        _chunk.Ip += 5;
    }

    private void CompileLine(Stmt s)
    {
        CompileExpr(s.X1!); CompileExpr(s.Y1!); CompileExpr(s.X2!); CompileExpr(s.Y2!);
        CompileExpr(s.ColorExpr ?? Expr.NumLit(7));
        _chunk.Emit(OpCode.GfxLine, s.GfxMode, s.Line);
    }

    private void CompileCircle(Stmt s)
    {
        CompileExpr(s.X1!); CompileExpr(s.Y1!);
        CompileExpr(s.Radius!);
        CompileExpr(s.ColorExpr ?? Expr.NumLit(7));
        CompileExpr(s.StartAngle ?? Expr.NumLit(0));
        CompileExpr(s.EndAngle ?? Expr.NumLit(0));
        CompileExpr(s.Aspect ?? Expr.NumLit(1));
        int has = (s.StartAngle != null || s.EndAngle != null) ? 1 : 0;
        _chunk.Emit(OpCode.ConstNum, _chunk.AddConstNum(has), s.Line);
        _chunk.Emit(OpCode.GfxCircle, s.Line);
    }

    private void CompileSubCall(Stmt s)
    {
        var r = FindRoutine(s.CallName);
        if (r == null)
            throw new CompileException($"未定义的 SUB '{s.CallName}'");
        var args = s.CallArgs ?? new List<Expr>();
        var isArr = s.CallArgIsArray ?? new List<bool>();
        for (int i = 0; i < args.Count && i < r.Params.Count; i++)
        {
            if (isArr.Count > i && isArr[i]) continue; // 整数组参数，全局绑定
            CompileExpr(args[i]);
            int pIdx = _chunk.ResolveVar(r.Params[i].Name);
            _chunk.Emit(OpCode.VarStore, pIdx, s.Line);
        }
        int opOffset = _chunk.Address;
        _chunk.Emit(OpCode.CallRoutine, 0, s.Line);
        _routinePatches.Add((opOffset + 1, s.CallName));
    }

    private void CompileOnError(Stmt s)
    {
        if (s.ErrZero)
        {
            _chunk.Emit(OpCode.ClearErrHandler, s.Line);
            return;
        }
        int opOffset = _chunk.Address;
        _chunk.Emit(OpCode.SetErrHandler, 0, s.Line);
        _errPatches.Add((opOffset + 1, s.ErrLabel));
    }

    private Routine? FindRoutine(string name)
    {
        foreach (var r in _routines)
            if (string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Expr.StripTypeSuffix(r.Name), Expr.StripTypeSuffix(name), StringComparison.OrdinalIgnoreCase))
                return r;
        return null;
    }

    private void CompileLet(Stmt s)
    {
        if (s.IsArray)
        {
            int varIdx = _chunk.ResolveVar(s.VarName);
            _chunk.ArrayVars.Add(varIdx);
            if (s.Indexes is { Count: 2 })
            {
                CompileExpr(s.Indexes[0]);
                CompileExpr(s.Indexes[1]);
                CompileExpr(s.Value!);
                _chunk.Emit(OpCode.Arr2Store, varIdx, s.Line);
                return;
            }
            CompileExpr(s.Index!);
            CompileExpr(s.Value!);
            _chunk.Emit(OpCode.ArrStore, varIdx, s.Line);
        }
        else
        {
            CompileExpr(s.Value!);
            int varIdx = _chunk.ResolveVar(s.VarName);
            _chunk.Emit(OpCode.VarStore, varIdx, s.Line);
        }
    }

    private void CompilePrint(Stmt s)
    {
        var items = s.PrintItems!;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.IsNewline) { _chunk.Emit(OpCode.PrintNewline, s.Line); continue; }
            if (item.TabCol != null)
            {
                // TAB(n)
                if (item.TabCol is Expr tc && tc.Kind == ExprKind.NumLit)
                {
                    _chunk.Emit(OpCode.PrintTab, (int)tc.Num, s.Line);
                }
                else
                {
                    CompileExpr(item.TabCol!);
                    _chunk.Emit(OpCode.PrintTab, 1, s.Line); // 运行时列值在栈顶（简化：仅常量支持）
                }
                if (item.Separator == ';' || item.Separator == ',') { /* TAB 后分隔符不换行 */ }
                continue;
            }
            if (item.Expr != null) CompileExpr(item.Expr);
            if (item.Separator == '\0') _chunk.Emit(OpCode.Print, s.Line);
            else if (item.Separator == ';') _chunk.Emit(OpCode.PrintSemicolon, s.Line);
            else _chunk.Emit(OpCode.PrintComma, s.Line);
        }
    }

    private void CompileInput(Stmt s)
    {
        if (s.Value != null)
        {
            CompileExpr(s.Value);
            _chunk.Emit(OpCode.PrintSemicolon, s.Line);
        }
        int varIdx = _chunk.ResolveVar(s.VarName);
        _chunk.Emit(OpCode.Input, varIdx, s.Line);
    }

    private void CompileIf(Stmt s)
    {
        CompileExpr(s.Cond!);
        if (s.SingleLineThen != null)
        {
            int elseJump = _chunk.Address;
            _chunk.Emit(OpCode.JumpIfFalse, 0, s.Line);
            foreach (var st in s.SingleLineThen) CompileStmt(st);
            int endJump = _chunk.Address;
            _chunk.Emit(OpCode.Jump, 0, s.Line);
            int elseAddr = _chunk.Address;
            _chunk.PatchOperand(elseJump + 1, elseAddr);
            if (s.SingleLineElseStmts != null)
                foreach (var st in s.SingleLineElseStmts) CompileStmt(st);
            int endAddr = _chunk.Address;
            _chunk.PatchOperand(endJump + 1, endAddr);
        }
        else
        {
            int elseJump = _chunk.Address;
            _chunk.Emit(OpCode.JumpIfFalse, 0, s.Line);
            foreach (var st in s.ThenStmts) CompileStmt(st);
            if (s.ElseStmts.Count > 0)
            {
                int endJump = _chunk.Address;
                _chunk.Emit(OpCode.Jump, 0, s.Line);
                int elseAddr = _chunk.Address;
                _chunk.PatchOperand(elseJump + 1, elseAddr);
                foreach (var st in s.ElseStmts) CompileStmt(st);
                int endAddr = _chunk.Address;
                _chunk.PatchOperand(endJump + 1, endAddr);
            }
            else
            {
                int endAddr = _chunk.Address;
                _chunk.PatchOperand(elseJump + 1, endAddr);
            }
        }
    }

    private void CompileFor(Stmt s)
    {
        int varIdx = _chunk.ResolveVar(s.ForVar);
        int limitIdx = _chunk.ResolveVar("~limit_" + s.ForVar);
        int stepIdx = _chunk.ResolveVar("~step_" + s.ForVar);
        CompileExpr(s.From!);
        _chunk.Emit(OpCode.VarStore, varIdx, s.Line);
        CompileExpr(s.To!);
        _chunk.Emit(OpCode.VarStore, limitIdx, s.Line);
        CompileExpr(s.Step!);
        _chunk.Emit(OpCode.VarStore, stepIdx, s.Line);
        int loopStart = _chunk.Address;
        foreach (var st in s.Body)
            if (st.Kind != StmtKind.Rem) CompileStmt(st);
        _chunk.Emit3(OpCode.ForCheck, varIdx, limitIdx, stepIdx, s.Line);
        _chunk.Emit(OpCode.JumpIfTrue, loopStart, s.Line);
    }

    private void CompileWhile(Stmt s)
    {
        int loopStart = _chunk.Address;
        CompileExpr(s.Cond!);
        int exitJump = _chunk.Address;
        _chunk.Emit(OpCode.JumpIfFalse, 0, s.Line);
        foreach (var st in s.Body)
            if (st.Kind != StmtKind.Rem) CompileStmt(st);
        _chunk.Emit(OpCode.Jump, loopStart, s.Line);
        int endAddr = _chunk.Address;
        _chunk.PatchOperand(exitJump + 1, endAddr);
    }

    private void CompileSelect(Stmt s)
    {
        int tmp = _chunk.ResolveVar("~sel_" + (_selTmp++));
        CompileExpr(s.SelectExpr!);
        _chunk.Emit(OpCode.VarStore, tmp, s.Line);

        var endJumps = new List<int>();
        int? lastTestJump = null;
        var cases = s.Cases ?? new List<CaseClause>();
        for (int ci = 0; ci < cases.Count; ci++)
        {
            var c = cases[ci];
            if (lastTestJump.HasValue)
                _chunk.PatchOperand(lastTestJump.Value + 1, _chunk.Address);

            if (c.IsElse)
            {
                if (lastTestJump.HasValue)
                    _chunk.PatchOperand(lastTestJump.Value + 1, _chunk.Address);
                lastTestJump = null;
                CompileBody(c.Body);
                continue;
            }

            bool first = true;
            void AddTest()
            {
                if (first) first = false;
                else _chunk.Emit(OpCode.Or, s.Line);
            }
            foreach (var v in c.Values)
            {
                _chunk.Emit(OpCode.VarLoad, tmp, s.Line);
                CompileExpr(v);
                _chunk.Emit(OpCode.Eq, s.Line);
                AddTest();
            }
            foreach (var (op, v) in c.Conds)
            {
                _chunk.Emit(OpCode.VarLoad, tmp, s.Line);
                CompileExpr(v);
                _chunk.Emit(OpToCode(op), s.Line);
                AddTest();
            }
            foreach (var (lo, hi) in c.Ranges)
            {
                _chunk.Emit(OpCode.VarLoad, tmp, s.Line);
                CompileExpr(lo);
                _chunk.Emit(OpCode.Ge, s.Line);
                _chunk.Emit(OpCode.VarLoad, tmp, s.Line);
                CompileExpr(hi);
                _chunk.Emit(OpCode.Le, s.Line);
                _chunk.Emit(OpCode.And, s.Line);
                AddTest();
            }

            lastTestJump = _chunk.Address;
            _chunk.Emit(OpCode.JumpIfFalse, 0, s.Line);
            CompileBody(c.Body);
            int endJump = _chunk.Address;
            _chunk.Emit(OpCode.Jump, 0, s.Line);
            endJumps.Add(endJump);
        }
        if (lastTestJump.HasValue)
            _chunk.PatchOperand(lastTestJump.Value + 1, _chunk.Address);
        int endAddr = _chunk.Address;
        foreach (var j in endJumps)
            _chunk.PatchOperand(j + 1, endAddr);
    }

    private void CompileBody(List<Stmt> body)
    {
        foreach (var st in body)
            if (st.Kind != StmtKind.Rem) CompileStmt(st);
    }

    private void CompileDoLoop(Stmt s)
    {
        bool hasCond = s.DoCond != null;
        bool pre = hasCond && !s.DoCondAfter;
        int loopStart = _chunk.Address;
        if (pre)
        {
            CompileExpr(s.DoCond!);
            int exitJump = _chunk.Address;
            _chunk.Emit(s.DoUntil ? OpCode.JumpIfTrue : OpCode.JumpIfFalse, 0, s.Line);
            CompileBody(s.Body);
            _chunk.Emit(OpCode.Jump, loopStart, s.Line);
            int endAddr = _chunk.Address;
            _chunk.PatchOperand(exitJump + 1, endAddr);
        }
        else
        {
            CompileBody(s.Body);
            if (hasCond)
            {
                CompileExpr(s.DoCond!);
                _chunk.Emit(s.DoUntil ? OpCode.JumpIfFalse : OpCode.JumpIfTrue, loopStart, s.Line);
            }
            else
            {
                _chunk.Emit(OpCode.Jump, loopStart, s.Line);
            }
        }
    }

    private void CompileGoto(Stmt s, bool gosub)
    {
        var op = gosub ? OpCode.Gosub : OpCode.Jump;
        int opOffset = _chunk.Address;
        _chunk.Emit(op, 0, s.Line);
        _patches.Add((opOffset + 1, s.Target, s.TargetNum, s.TargetIsNum));
    }

    private void EmitDim(string name, List<Expr> dims, List<Expr?> lowers, int line)
    {
        int varIdx = _chunk.ResolveVar(name);
        if (dims.Count == 0) return; // 标量：仅声明变量，不标记为数组（否则后续赋值会被误当作 DIM 尺寸）
        _chunk.ArrayVars.Add(varIdx);
        if (dims.Count == 2)
        {
            CompileExpr(dims[0]);
            CompileExpr(dims[1]);
            _chunk.Emit(OpCode.DimArray2, varIdx, line);
            return;
        }
        Expr? lower = lowers.Count > 0 ? lowers[0] : null;
        if (lower != null)
        {
            CompileExpr(lower);
            CompileExpr(dims[0]);
            _chunk.Emit(OpCode.DimRange, varIdx, line);
        }
        else
        {
            CompileExpr(dims[0]);
            _chunk.Emit(OpCode.VarStore, varIdx, line);
        }
    }

    private void CompileDim(Stmt s)
    {
        for (int i = 0; i < s.DimVars.Count; i++)
        {
            string name = s.DimVars[i];
            var dims = i < s.DimDims.Count ? s.DimDims[i] : new List<Expr>();
            var lowers = i < s.DimLowers.Count ? s.DimLowers[i]! : new List<Expr?>();
            string type = i < s.DimType.Count ? s.DimType[i] : "";
            if (type != "" && _types.TryGetValue(type, out var ut))
            {
                // 用户类型：为每个字段声明数组
                foreach (var field in ut.Fields)
                {
                    string fname = name + ".." + field;
                    EmitDim(fname, dims, lowers, s.Line);
                }
                // 也注册基名（用于整数组传递）
                _chunk.ResolveVar(name);
                continue;
            }
            EmitDim(name, dims, lowers, s.Line);
        }
    }

    // ---------- 表达式编译 ----------

    private void CompileExpr(Expr e)
    {
        switch (e.Kind)
        {
            case ExprKind.NumLit:
                _chunk.Emit(OpCode.ConstNum, _chunk.AddConstNum(e.Num), 0);
                break;
            case ExprKind.StrLit:
                _chunk.Emit(OpCode.ConstStr, _chunk.AddConstStr(e.Str), 0);
                break;
            case ExprKind.Var:
            {
                int idx = _chunk.ResolveVar(e.VarName);
                _chunk.Emit(OpCode.VarLoad, idx, 0);
                break;
            }
            case ExprKind.ArrayRef:
            {
                if (e.WholeArray)
                {
                    // 整数组引用作为函数实参：无操作（数组全局绑定）
                    break;
                }
                int idx = _chunk.ResolveVar(e.VarName);
                _chunk.ArrayVars.Add(idx);
                if (e.Indexes is { Count: 2 })
                {
                    CompileExpr(e.Indexes[0]);
                    CompileExpr(e.Indexes[1]);
                    _chunk.Emit(OpCode.Arr2Load, idx, 0);
                }
                else
                {
                    CompileExpr(e.Index!);
                    _chunk.Emit(OpCode.ArrLoad, idx, 0);
                }
                break;
            }
            case ExprKind.Unary:
            {
                CompileExpr(e.Left!);
                if (e.Op == "-") _chunk.Emit(OpCode.Neg, 0);
                else if (e.Op == "NOT") _chunk.Emit(OpCode.Not, 0);
                break;
            }
            case ExprKind.Binary:
            {
                CompileExpr(e.Left!);
                CompileExpr(e.Right!);
                _chunk.Emit(OpToCode(e.Op), 0);
                break;
            }
            case ExprKind.FuncCall:
            {
                if (_defFns.TryGetValue(e.FuncName, out var fn))
                {
                    // DEF FN 内联：赋参数，求值函数体
                    if (e.Args != null && e.Args.Count > 0 && fn.Param != "")
                    {
                        CompileExpr(e.Args[0]);
                        _chunk.Emit(OpCode.VarStore, _chunk.ResolveVar(fn.Param), 0);
                    }
                    CompileExpr(fn.Body);
                    break;
                }
                if (FindRoutine(e.FuncName) is { IsFunction: true } r)
                {
                    CompileRoutineCall(r, e.Args);
                    break;
                }
                if (e.Args != null)
                    foreach (var a in e.Args) CompileExpr(a);
                int argc = e.Args?.Count ?? 0;
                int fIdx = _chunk.ResolveFunc(e.FuncName);
                _chunk.EmitCall(fIdx, argc, 0);
                break;
            }
        }
    }

    private void CompileRoutineCall(Routine r, List<Expr>? args)
    {
        args ??= new List<Expr>();
        for (int i = 0; i < args.Count && i < r.Params.Count; i++)
        {
            if (args[i].WholeArray) continue;
            CompileExpr(args[i]);
            int pIdx = _chunk.ResolveVar(r.Params[i].Name);
            _chunk.Emit(OpCode.VarStore, pIdx, 0);
        }
        int opOffset = _chunk.Address;
        _chunk.Emit(OpCode.CallRoutine, 0, 0);
        _routinePatches.Add((opOffset + 1, r.Name));
        if (r.IsFunction)
            _chunk.Emit(OpCode.VarLoad, _chunk.ResolveVar(r.ReturnVar), 0);
    }

    private static OpCode OpToCode(string op) => op switch
    {
        "+" => OpCode.Add,
        "-" => OpCode.Sub,
        "*" => OpCode.Mul,
        "/" => OpCode.Div,
        "\\" => OpCode.Idiv,
        "^" => OpCode.Power,
        "MOD" => OpCode.Mod,
        "=" => OpCode.Eq,
        "<>" => OpCode.Ne,
        "<" => OpCode.Lt,
        "<=" => OpCode.Le,
        ">" => OpCode.Gt,
        ">=" => OpCode.Ge,
        "AND" => OpCode.And,
        "OR" => OpCode.Or,
        _ => OpCode.Nop,
    };
}

/// <summary>编译错误。</summary>
public class CompileException : Exception
{
    public CompileException(string msg) : base(msg) { }
}
