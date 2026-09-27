using VMLAssembler;
using CompilerBase;

namespace PythonCompiler
{
    public partial class CodeGenerator
    {
        private static ExpType PythonTypeToExpType(PythonType t) => t switch
        {
            PythonType.Bool => ExpType.I8,
            PythonType.Int64 => ExpType.I64,
            PythonType.Float => ExpType.F32,
            PythonType.String or PythonType.List or PythonType.Dict or PythonType.Tuple or PythonType.Set => ExpType.Ptr32,
            _ => ExpType.I32,
        };

        private ExpVar WrapExpr(ASTNode node)
        {
            var pyType = InferExpressionType(node);
            return ExpVar.Eval(PythonTypeToExpType(pyType), () => node.Accept(this));
        }

        private ExpVar WrapTargetExpr(string name)
        {
            PythonType pyType = PythonType.Int;
            varTypes.TryGetValue(name, out pyType);
            if (localVars.TryGetValue(name, out int offset))
                return ExpVar.Stack(offset, 12, PythonTypeToExpType(pyType));
            if (globalVars.TryGetValue(name, out int _))
                return ExpVar.Data($"global_{name}", PythonTypeToExpType(pyType));
            // 不存在则按局部变量处理
            return ExpVar.Stack(0, 12, PythonTypeToExpType(pyType));
        }

        public void VisitBinOp(BinOpNode node)
        {
            // Power ** — 保持内联 (Python栈约定与C __stdcall不兼容)
            if (node.Op == "**")
            {
                node.Right.Accept(this);
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                node.Left.Accept(this);
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                string pwl = NewLabel("pl"); string pwe = NewLabel("pe");
                PlaceLabel(pwl);
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
                Emit(OpCode.JLE, new Operand(OperandType.LABEL, pwe));
                Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2));
                Emit(OpCode.SUB, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1));
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, pwl));
                PlaceLabel(pwe);
                return;
            }

            var left = WrapExpr(node.Left);
            var right = WrapExpr(node.Right);

            if (_expr!.EmitStandardBinaryOps(node.Op, left, right)) return;
            if (node.Op == "//") { _expr!.EmitBinOp(left, right, "/"); return; }
            _expr!.EmitBitwiseOps(node.Op, left, right);
        }

        public void VisitUnaryOp(UnaryOpNode node)
        {
            switch (node.Op)
            {
                case "-":
                    _expr!.EmitNeg(WrapExpr(node.Operand));
                    break;
                case "not":
                case "~":
                    _expr!.EmitNot(WrapExpr(node.Operand));
                    break;
            }
        }

        public void VisitCompare(CompareNode node)
        {
            // `in` operator: linear scan of list/tuple
            if (node.Op == "in")
            {
                node.Left.Accept(this);
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                node.Right.Accept(this);
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "0(R0)"));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0));
                string inLoop = NewLabel("in_loop");
                string inFound = NewLabel("in_found");
                string inNotFound = NewLabel("in_notfound");
                string inEnd = NewLabel("in_end");
                PlaceLabel(inLoop);
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2));
                Emit(OpCode.JGE, new Operand(OperandType.LABEL, inNotFound));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 4));
                Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 5));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "0(R4)"));
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 1));
                Emit(OpCode.JE, new Operand(OperandType.LABEL, inFound));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1));
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, inLoop));
                PlaceLabel(inNotFound);
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, inEnd));
                PlaceLabel(inFound);
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                PlaceLabel(inEnd);
                return;
            }

            var left = WrapExpr(node.Left);
            var right = WrapExpr(node.Right);
            string cmpOp = node.Op == "is" ? "==" : node.Op;
            _expr!.EmitCmp(left, right, cmpOp);
        }

        public void VisitBoolOp(BoolOpNode node)
        {
            // 简化实现：and/or
            string endLabel = NewLabel("bool_end");

            for (int i = 0; i < node.Values.Count; i++)
            {
                node.Values[i].Accept(this);

                if (i < node.Values.Count - 1)
                {
                    Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));

                    if (node.Op == "and")
                    {
                        // and: 如果为假，短路返回0
                        Emit(OpCode.JE, new Operand(OperandType.LABEL, endLabel));
                    }
                    else // or
                    {
                        // or: 如果为真，短路返回1
                        string nextLabel = NewLabel("bool_next");
                        Emit(OpCode.JNE, new Operand(OperandType.LABEL, nextLabel));
                        Emit(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));
                        PlaceLabel(nextLabel);
                    }
                }
            }

            PlaceLabel(endLabel);
        }

        public void VisitCall(CallNode node)
        {
            // chipasm：无需参数求值，直接发射
            // (asm() 仅限 C/ObjC/C++ 语言使用，其他语言通过 Lib/c/vmlsys.c 调用)
            if (node.FuncName == "chipasm" && node.Args.Count >= 2) { return; }
            // exit(n) → MOVE R0, n; SYSCALL 3
            if (node.FuncName == "exit" && node.Args.Count >= 1) { node.Args[0].Accept(this); EmitExit(); return; }

            // super() → return self reference (proxy for parent class dispatch)
            if (node.FuncName == "super" && _currentClassName != null)
            {
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.LABEL, $"{_currentClassName}_instance"));
                return;
            }

            // 参数压栈（从右到左）—— **按类型压**：
            //   原先一律 `PUSH R0`（4 字节）⇒ 浮点实参的值在 `F0`、64 位/双精度实参在
            //   `L0`/`D0`，压 `R0` 压到的是残留；8 字节值还会把布局挤歪。
            //   统一走 `EmitPushArg`（浮点 4 字节、双精度/64 位 8 字节，都写**主栈**）。
            int pushedArgBytes = 0;
            for (int i = node.Args.Count - 1; i >= 0; i--)
            {
                node.Args[i].Accept(this);
                var at = InferExpressionType(node.Args[i]);
                var (asz, af, ad, al) = GetTypeInfo(at);
                EmitPushArg(asz, af, ad, al);
                pushedArgBytes += al ? 8 : ad ? 8 : 4;
            }

            // 函数调用 / 内置函数
            string callTarget = node.FuncName;
            if (lambdaVars.ContainsKey(node.FuncName))
                callTarget = lambdaVars[node.FuncName];

            if (classInfo.ContainsKey(callTarget))
            {
                // 类实例化: ClassName(...) → 单实例简化模型 (全局 instance_ 字段)
                var ci = classInfo[callTarget];
                // 调用 __init__ 方法体初始化字段
                if (ci.Methods.TryGetValue("__init__", out string initLabel))
                    Emit(OpCode.CALL, new Operand(OperandType.LABEL, initLabel));
                // R0 = 实例指针 (全局实例标签地址)
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.LABEL, $"{callTarget}_instance"));
            }
            else if (labels.ContainsKey(callTarget))
            {
                Emit(OpCode.CALL, new Operand(OperandType.LABEL, callTarget));
            }
            else // 内置函数（print/len/abs/min/max/int/str/append/poke/peek/range/input 等）
            {
                switch (node.FuncName)
                {
                    case "println":
                    case "print":
                        for (int i = 0; i < node.Args.Count; i++)
                        {
                            // ⚠ 实参从**右往左**压栈 ⇒ arg_i 在 `[R13 + 4*i]`（最后压的 arg0 在栈顶）。
                            //   这里原先写的是 `(node.Args.Count - 1 - i) * 4` —— **整个反了**：
                            //   `Args.Count == 1` 时 `(1-1-0)*4 == 0` 恰好对，所以
                            //   `print("一个字面量")` 一直正常，**只有多实参才炸**，而且炸法很隐蔽：
                            //   `print("OUT-INT=", 42)` ⇒ 把 42 当**字符串指针**打、把字符串指针当
                            //   **整数**打，实测输出 ` 1036`（字面量丢了、整数也不对）。
                            //   （同族：Scheme 命名 let 那处 `nlParams.Count-1` 也只在两个参数时恰好对。）
                            int stackOffset = i * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));

                            if (node.Args[i] is ConstantNode cn && (cn.ValueType == "str" || cn.ValueType == "string"))
                            {
                                int syscallStr = VMLPlugins.CompilerOptionsContext.Current.IsMCU ? 1 : 391;
                                Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, syscallStr));
                            }
                            else if (node.Args[i] is ConstantNode cnf && cnf.ValueType == "float")
                            {
                                EmitPrintFloat();
                            }
                            else if (node.Args[i] is ConstantNode cnb && cnb.ValueType == "bool")
                            {
                                EmitPrintBool();
                            }
                            else
                                Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 6));

                            if (i < node.Args.Count - 1)
                            {
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.IMMEDIATE, 32));
                                Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 4));
                            }
                        }
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                             new Operand(OperandType.IMMEDIATE, 10));
                        Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 4));
                        break;
                    case "input":
                        Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 7));
                        break;
                    case "len":
                        if (node.Args.Count == 1)
                        {
                            // 字符串字面量: 编译期已知长度, 直接返回 (字符串无长度前缀)
                            if (node.Args[0] is ConstantNode cn && cn.Value is string strLit)
                            {
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.IMMEDIATE, strLit.Length));
                            }
                            else
                            {
                                int stackOffset = (node.Args.Count - 1) * 4;
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.MEMORY, "0(R0)"));
                            }
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "ord":
                        if (node.Args.Count == 1)
                        {
                            int so = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{so}(R13)"));
                            // If arg is a string, load first byte; otherwise return int as-is
                            Emit(OpCode.MOVEB, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, "0(R0)"));
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "chr":
                        if (node.Args.Count == 1)
                        {
                            int sc = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{sc}(R13)"));
                            // Store char in 2-byte stack buffer
                            Emit(OpCode.SUB, new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4));
                            Emit(OpCode.MOVEB, new Operand(OperandType.MEMORY, "0(R13)"), new Operand(OperandType.REGISTER, 0));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                            Emit(OpCode.MOVEB, new Operand(OperandType.MEMORY, "1(R13)"), new Operand(OperandType.REGISTER, 0));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 13));
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "range":
                        if (node.Args.Count >= 1)
                        {
                            int stackOffset = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "abs":
                        if (node.Args.Count == 1)
                        {
                            int stackOffset = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                            EmitCallAbs();
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "min":
                    case "max":
                        if (node.Args.Count == 2)
                        {
                            int s1 = (node.Args.Count - 1) * 4;
                            int s2 = (node.Args.Count - 2) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{s1}(R13)"));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                                 new Operand(OperandType.MEMORY, $"{s2}(R13)"));
                            if (node.FuncName == "min")
                                EmitCallMin();
                            else
                                EmitCallMax();
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "int":
                    case "str":
                        if (node.Args.Count == 1)
                        {
                            int stackOffset = (node.Args.Count - 1) * 4;
                            // ⚠ **`int(<浮点>)` 必须真的转换**。实参按 4 字节压栈（浮点值在栈上是
                            //   它的**位型**），取回来若只 `MOVE R0, [R13+0]` 就把位型当整数了 ——
                            //   实测 `println_int(int(3.14))` 打出 **188219392**（= 3.14f 的位型）。
                            //   正解：位型放回 F0（`MOVEF` 认的是同一份位型），再 `F2I` 取整。
                            //   `float`/`double` 之外的类型照旧直接搬运。
                            var argType = InferExpressionType(node.Args[0]);
                            if (argType == PythonType.Float)
                            {
                                // 浮点：栈上是 4 字节位型 → 放回 F0（`MOVEF` 认同一份位型）→ `F2I`
                                Emit(OpCode.MOVEF, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                                Emit(OpCode.F2I, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.REGISTER, 0));
                            }
                            else if (argType == PythonType.Int64)
                            {
                                // 64 位：栈上是 8 字节 → 取进 L0 → 收窄到 int（`int64 → int` 的截断）
                                Emit(OpCode.MOVEL, new Operand(OperandType.REGISTER, 24),
                                     new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                                Emit(OpCode.L2I, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.REGISTER, 24));
                            }
                            else
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                     new Operand(OperandType.MEMORY, $"{stackOffset}(R13)"));
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "append":
                        if (node.Args.Count == 2)
                        {
                            int listOffset = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{listOffset}(R13)"));
                            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                            int itemOffset = (node.Args.Count - 2) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{itemOffset}(R13)"));
                            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                                 new Operand(OperandType.MEMORY, "0(R0)"));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 3),
                                 new Operand(OperandType.REGISTER, 0));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3),
                                 new Operand(OperandType.IMMEDIATE, 4));
                            Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 2),
                                 new Operand(OperandType.IMMEDIATE, 4));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3),
                                 new Operand(OperandType.REGISTER, 2));
                            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R3)"), new Operand(OperandType.REGISTER, 1));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                                 new Operand(OperandType.MEMORY, "0(R0)"));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2),
                                 new Operand(OperandType.IMMEDIATE, 1));
                            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 2));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case "sum":
                        if (node.Args.Count == 1)
                        {
                            // R0 = list address (from stack)
                            int stackOff = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.MEMORY, $"{stackOff}(R13)"));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                                 new Operand(OperandType.MEMORY, "0(R0)")); // R1 = len
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 3),
                                 new Operand(OperandType.IMMEDIATE, 0)); // R3 = index
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                                 new Operand(OperandType.IMMEDIATE, 0)); // R0 = accumulator
                            string sumLoop = NewLabel("sum_loop");
                            string sumEnd = NewLabel("sum_end");
                            PlaceLabel(sumLoop);
                            Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1));
                            Emit(OpCode.JGE, new Operand(OperandType.LABEL, sumEnd));
                            // Load list+4+index*4
                            int sOff2 = (node.Args.Count - 1) * 4;
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                                 new Operand(OperandType.MEMORY, $"{sOff2}(R13)"));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4));
                            Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 4));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "0(R2)"));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2));
                            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1));
                            Emit(OpCode.JMP, new Operand(OperandType.LABEL, sumLoop));
                            PlaceLabel(sumEnd);
                        }
                        else
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        break;
                    case string pokeName when pokeName.StartsWith("poke"):
                        node.Args[0].Accept(this);
                        Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                        node.Args[1].Accept(this);
                        Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                        Emit(OpCode.CALL, new Operand(OperandType.LABEL, "vml_" + pokeName));
                        break;
                    case string peekName when peekName.StartsWith("peek"):
                        node.Args[0].Accept(this);
                        Emit(OpCode.CALL, new Operand(OperandType.LABEL, "vml_" + peekName));
                        break;

                    default:
                        // **不是内置函数 ⇒ 当成外部（库）函数，按标签直接 CALL。**
                        //
                        // ⚠ 这里原来是**静默丢弃**（switch 没命中就什么都不发）。后果很隐蔽：
                        //   链进来的库（`vmltool.config.xml` 里各语言的 `Libs`，例如共享调用库
                        //   `vmlui.vml`）标签不在编译期的 `labels` 表里，于是
                        //   `ui_win_open(...)` 编出来**一条 CALL 都没有** —— 程序照跑、
                        //   界面上什么都没有，也看不出哪儿错了（实测就是这样）。
                        //
                        //   「认不出来就报错」比「认不出来就装作没看见」好得多（CLI 那条铁律同理）：
                        //   名字拼错的函数现在会在**链接期**报「未找到标签」，而不是凭空消失。
                        //   参数已经按从右到左压好栈，与 C 库函数的 cdecl 约定一致，直接 CALL 即可。
                        Emit(OpCode.CALL, new Operand(OperandType.LABEL, callTarget));
                        break;
                }
            }

            // 清理栈上的参数（按**实际压入**的字节数 —— 双精度/64 位实参占 2 格）
            if (pushedArgBytes > 0)
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 13),
                     new Operand(OperandType.IMMEDIATE, pushedArgBytes));
        }

        public void VisitAttribute(AttributeNode node)
        {
            // 处理对象属性访问
            string attrName = node.Attr;
            string instanceVarLabel = $"instance_{attrName}";
            if (!dataSection.ContainsKey(instanceVarLabel))
                dataSection[instanceVarLabel] = 0;

            // 如果是self.属性，处理实例变量
            if (node.Value is NameNode nameNode && nameNode.Name == "self")
            {
                // 为实例变量生成访问代码
                // 简化：假设实例变量存储在固定偏移
                // 加载实例变量地址
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.LABEL, instanceVarLabel));

                // 加载值
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.MEMORY, "0(R0)"));
            }
            else
            {
                // 对象属性访问: obj.attr → 加载全局 instance_attr (单实例简化模型)
                node.Value.Accept(this); // 求值对象 (保持副作用)
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.LABEL, instanceVarLabel));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                     new Operand(OperandType.MEMORY, "0(R0)"));
            }
        }

        /// <summary>
        /// 计算下标元素地址到 R2；越界时跳到 <paramref name="outOfBounds"/> 标签（此时 R2 无意义）。
        ///
        /// 读（<see cref="VisitSubscript"/>）与写（VisitAssign / VisitAugAssign 的下标分支）**共用这一份**。
        /// 两处各算一遍地址，迟早会出现"读的偏移对、写的偏移差一格"那类缺陷 ——
        /// 本仓已记过同族问题多次（Go 的 a[i]=v 整段没代码生成，见 patches/0015-go-arrays.patch）。
        /// 列表布局见 <see cref="VisitList"/>：[长度, e0, e1, …]，元素 i 落在 base + 4 + i*4。
        /// </summary>
        private void EmitSubscriptAddress(SubscriptNode node, string outOfBounds)
        {
            // 获取容器（列表/字典/元组）地址
            node.Value.Accept(this); // 容器地址在R0
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0)); // 保存容器地址

            // 获取索引/键
            node.Index.Accept(this); // 索引/键在R0

            // 恢复容器地址到R1
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1)); // 容器地址在R1

            // 检查容器类型（简化：假设所有容器都有长度字段）
            // 加载长度
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                 new Operand(OperandType.MEMORY, "0(R1)")); // 长度在R2

            // 负索引支持：if index < 0, index = index + length
            string negSkip = NewLabel("neg_skip");
            Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            Emit(OpCode.JGE, new Operand(OperandType.LABEL, negSkip));
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2));
            PlaceLabel(negSkip);

            // 边界检查（对于列表/元组）：越界交给调用方处理 —— 读给 0，写整条跳过
            string boundsOk = NewLabel("bounds_ok");
            Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2));
            Emit(OpCode.JL, new Operand(OperandType.LABEL, boundsOk));
            Emit(OpCode.JMP, new Operand(OperandType.LABEL, outOfBounds));

            // 边界检查通过
            PlaceLabel(boundsOk);

            // 计算元素地址：基地址 + 4（长度） + 索引*4
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2),
                 new Operand(OperandType.REGISTER, 1)); // 基地址
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2),
                 new Operand(OperandType.IMMEDIATE, 4)); // 跳过长度
            Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 0),
                 new Operand(OperandType.IMMEDIATE, 4)); // 索引*4
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2),
                 new Operand(OperandType.REGISTER, 0)); // 最终地址在R2
        }

        public void VisitSubscript(SubscriptNode node)
        {
            string outOfBounds = NewLabel("sub_oob");
            string done = NewLabel("sub_done");

            EmitSubscriptAddress(node, outOfBounds);

            // 加载元素值
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                 new Operand(OperandType.MEMORY, "0(R2)"));
            Emit(OpCode.JMP, new Operand(OperandType.LABEL, done));

            // 越界：沿用原语义 —— 读回 0（不抛异常）
            PlaceLabel(outOfBounds);
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));

            PlaceLabel(done);
        }

        public void VisitName(NameNode node)
        {
            PythonType varType = PythonType.Int; // 默认整数类型
            if (varTypes.TryGetValue(node.Name, out PythonType type))
            {
                varType = type;
            }
            
            OpCode loadOp = GetLoadInstruction(varType);
            
            if (localVars.TryGetValue(node.Name, out int offset))
            {
                Emit(loadOp, new Operand(OperandType.REGISTER, VMLAssembler.RegisterClassTable.BankOfOperand(loadOp, 0)), new Operand(OperandType.MEMORY, Vars.FormatOffset(offset)));
            }
            else if (globalVars.TryGetValue(node.Name, out int gOffset))
            {
                // 全局变量：使用 data 段标签
                Emit(loadOp, new Operand(OperandType.REGISTER, VMLAssembler.RegisterClassTable.BankOfOperand(loadOp, 0)), new Operand(OperandType.LABEL, $"global_{node.Name}"));
            }
            else
            {
                // 未知变量
                Emit(loadOp, new Operand(OperandType.REGISTER, VMLAssembler.RegisterClassTable.BankOfOperand(loadOp, 0)), new Operand(OperandType.IMMEDIATE, 0));
            }
        }

        public void VisitConstant(ConstantNode node)
        {
            PythonType pythonType = GetPythonTypeFromValue(node.Value);
            OpCode loadOp = GetLoadInstruction(pythonType);
            // ⚠ 目的寄存器按**类**取（浮点 → `F0`、64 位 → `L0`、其余 → `R0`）——
            //   写死 `REGISTER 0` 时浮点值会落到 32 位通用寄存器上，后续按类取用就错位。
            var dst = new Operand(OperandType.REGISTER, VMLAssembler.RegisterClassTable.BankOfOperand(loadOp, 0));

            switch (node.Value)
            {
                case int i:
                    Emit(loadOp, dst, new Operand(OperandType.IMMEDIATE, i));
                    break;

                // ⚠ **Python 的字面量是 `double`**（词法器一律按 double 存）—— 原先这里只有
                //   `case float`，于是 `3.14` 掉进兜底分支 ⇒ 常量**恒 0**（实测 `int(3.5)` → 0）。
                //   本平台的 Python `float` 是 **32 位**（F 寄存器那一类），所以按 `(float)` 存
                //   4 字节 `.word`，用 `MOVEF` 取。
                case double dd when pythonType == PythonType.Float:
                    {
                        string flabel = $"flt_{labelCounter++}";
                        dataSection[flabel] = (float)dd;
                        Emit(loadOp, dst, new Operand(OperandType.MEMORY, flabel));
                        break;
                    }
                case float ff when pythonType == PythonType.Float:
                    {
                        string flabel = $"flt_{labelCounter++}";
                        dataSection[flabel] = ff;
                        Emit(loadOp, dst, new Operand(OperandType.MEMORY, flabel));
                        break;
                    }
                case double dd2:
                    Emit(loadOp, dst, new Operand(OperandType.IMMEDIATE, (int)dd2));
                    break;
                case float ff2:
                    Emit(loadOp, dst, new Operand(OperandType.IMMEDIATE, (int)ff2));
                    break;

                // 64 位整数字面量（超出 32 位范围）：数据段放 `.dword` + `MOVEL L0`
                case long lo:
                    {
                        string llabel = $"int64_{labelCounter++}";
                        dataSection[llabel] = lo;
                        Emit(loadOp, dst, new Operand(OperandType.MEMORY, llabel));
                        break;
                    }
                case bool b:
                    Emit(loadOp, dst, new Operand(OperandType.IMMEDIATE, b ? 1 : 0));
                    break;
                case string s:
                    string label = $"str_{labelCounter++}";
                    dataSection[label] = WStr(s);
                    Emit(loadOp, dst, new Operand(OperandType.LABEL, label));
                    break;
                default:
                    Emit(loadOp, dst, new Operand(OperandType.IMMEDIATE, 0));
                    break;
            }
        }

        public void VisitFString(FStringNode node)
        {
            // 运行时逐部分输出：字符串部分直接输出，表达式部分求值后输出
            bool hasExpr = false;
            foreach (var part in node.Parts)
            {
                if (part is string strPart)
                {
                    // 文字字符串部分：存入数据段，SYSCALL 1 输出
                    string strLabel = $"fstr_{labelCounter++}";
                    dataSection[strLabel] = WStr(strPart);
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, strLabel));
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 1));
                    hasExpr = true;
                }
                else if (part is ASTNode exprNode)
                {
                    // 表达式部分：求值后输出
                    exprNode.Accept(this);
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 6)); // OutputInt
                    hasExpr = true;
                }
            }
            // 如果没有任何部分（空f-string），输出空字符串
            if (!hasExpr)
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
        }

        public void VisitListComp(ListCompNode node)
        {
            // MCU-compatible list comprehension: [expr for var in iter if cond]
            Emit(OpCode.NOP, new List<Operand>(), $"; [listcomp: {node.VarName} in ...]");
            // Create empty list
            int initCap = 8;
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, initCap * 4 + 4));
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));
            // Save result list on stack
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            // For loop over iter
            string lcStart = NewLabel("lc_start");
            string lcBody = NewLabel("lc_body");
            string lcEnd = NewLabel("lc_end");
            // iter must be a range/list; generate iter-to-end check
            node.Iter.Accept(this);
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            PlaceLabel(lcStart);
            Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "0(R13)"));
            Emit(OpCode.JGE, new Operand(OperandType.LABEL, lcEnd));
            // Body: compute element, append to list
            PlaceLabel(lcBody);
            node.Expr.Accept(this);
            // Append: list[len] = expr; len++
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "4(R13)"));
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "0(R1)"));
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4));
            Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4));
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R3)"), new Operand(OperandType.REGISTER, 0));
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "4(R13)"));
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "0(R1)"));
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 2));
            // Increment loop counter
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            Emit(OpCode.JMP, new Operand(OperandType.LABEL, lcStart));
            PlaceLabel(lcEnd);
            // Cleanup: pop iter, restore result list to R0
            Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4));
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
        }

        public void VisitList(ListNode node)
        {
            // 基本列表实现：在堆上分配连续内存
            // 列表结构： [长度, 元素1, 元素2, ...]
            int elementCount = node.Elements.Count;
            
            // 为列表分配内存（长度 + 所有元素）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), 
                 new Operand(OperandType.IMMEDIATE, elementCount * 4 + 4)); // 总大小
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));
            // R0现在包含列表内存地址
            
            // 存储列表长度
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                 new Operand(OperandType.IMMEDIATE, elementCount));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));
            
            // 保存基地址到栈上，因为 Accept 会覆盖 R0
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

            // 存储列表元素
            for (int i = 0; i < elementCount; i++)
            {
                // 从栈加载基地址
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.MEMORY, "0(R13)"));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.IMMEDIATE, 4 + i * 4)); // 元素偏移

                // 计算元素值
                node.Elements[i].Accept(this); // 结果在R0

                // 存储元素值
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));
            }

            // 恢复基地址到 R0
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
        }

        public void VisitDict(DictNode node)
        {
            // 基本字典实现：键值对存储
            int pairCount = node.Items.Count;
            
            // 为字典分配内存（长度 + 键值对 * 2）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), 
                 new Operand(OperandType.IMMEDIATE, pairCount * 8 + 4)); // 总大小：长度 + 键值对*2
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));
            // R0现在包含字典内存地址
            
            // 存储字典长度（键值对数量）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), 
                 new Operand(OperandType.IMMEDIATE, pairCount));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));
            
            // 保存基地址到栈上，因为 Accept 会覆盖 R0
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

            // 存储键值对
            for (int i = 0; i < pairCount; i++)
            {
                var (key, value) = node.Items[i];

                // 从栈加载基地址，计算键地址
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.MEMORY, "0(R13)"));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.IMMEDIATE, 4 + i * 8)); // 键偏移

                // 计算键值
                key.Accept(this); // 结果在R0

                // 存储键值
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));

                // 计算值地址：键地址 + 4
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.IMMEDIATE, 4)); // 值偏移

                // 计算值
                value.Accept(this); // 结果在R0

                // 存储值
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));
            }

            // 恢复基地址到 R0
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
        }

        public void VisitTuple(TupleNode node)
        {
            // 元组实现：与列表类似，但不可变
            int elementCount = node.Elements.Count;
            
            // 为元组分配内存（长度 + 所有元素）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), 
                 new Operand(OperandType.IMMEDIATE, elementCount * 4 + 4)); // 总大小
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));
            // R0现在包含元组内存地址
            
            // 存储元组长度
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), 
                 new Operand(OperandType.IMMEDIATE, elementCount));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));
            
            // 存储元组元素
            for (int i = 0; i < elementCount; i++)
            {
                // 计算元素地址：基地址 + 4（长度） + i*4
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), 
                     new Operand(OperandType.REGISTER, 0)); // 基地址
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1), 
                     new Operand(OperandType.IMMEDIATE, 4 + i * 4)); // 元素偏移
                
                // 计算元素值
                node.Elements[i].Accept(this); // 结果在R0
                
                // 存储元素值
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));
            }
            
            // 元组地址在R0中
        }

        public void VisitMethodCall(MethodCallNode node)
        {
            // super().method(args) → dispatch to parent class method
            if (node.Receiver is CallNode superCall && superCall.FuncName == "super" && _currentClassName != null)
            {
                if (classInfo.TryGetValue(_currentClassName, out var ci) && ci.ParentName != null
                    && classInfo.TryGetValue(ci.ParentName, out var pi)
                    && pi.Methods.TryGetValue(node.Method, out string? parentMethodLabel))
                {
                    // Push args right-to-left
                    for (int i = node.Args.Count - 1; i >= 0; i--)
                    {
                        node.Args[i].Accept(this);
                        Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                    }
                    // Push self as first implicit argument
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                         new Operand(OperandType.LABEL, $"{_currentClassName}_instance"));
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                    // Call parent method
                    Emit(OpCode.CALL, new Operand(OperandType.LABEL, parentMethodLabel));
                    // Clean up stack (args + self)
                    int superPushed = node.Args.Count + 1;
                    if (superPushed > 0)
                        Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, superPushed * 4));
                    return;
                }
            }

            // 编译期字符串常量方法调用
            if (node.Receiver is ConstantNode receiverCn && receiverCn.Value is string strValue)
            {
                switch (node.Method)
                {
                    case "split":
                        if (node.Args.Count >= 1 && node.Args[0] is ConstantNode sepCn && sepCn.Value is string sep)
                        {
                            var parts = strValue.Split(new[] { sep }, System.StringSplitOptions.None);
                            // 分配列表: [length, elem1, elem2, ...]
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, parts.Length * 4 + 4));
                            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, parts.Length));
                            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));
                            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                            for (int i = 0; i < parts.Length; i++)
                            {
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "0(R13)"));
                                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4 + i * 4));
                                string partLabel = $"str_{labelCounter++}";
                                dataSection[partLabel] = WStr(parts[i]);
                                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, partLabel));
                                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));
                            }
                            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                            return;
                        }
                        break;

                    case "replace":
                        if (node.Args.Count >= 2 && node.Args[0] is ConstantNode oldCn && oldCn.Value is string oldStr
                            && node.Args[1] is ConstantNode newCn && newCn.Value is string newStr)
                        {
                            string result = strValue.Replace(oldStr, newStr);
                            string resultLabel = $"str_{labelCounter++}";
                            dataSection[resultLabel] = WStr(result);
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel));
                            return;
                        }
                        break;

                    case "join":
                        if (node.Args.Count >= 1 && node.Args[0] is ListNode listNode)
                        {
                            var sb = new System.Text.StringBuilder();
                            for (int i = 0; i < listNode.Elements.Count; i++)
                            {
                                if (listNode.Elements[i] is ConstantNode elemCn && elemCn.Value is string elemStr)
                                {
                                    if (i > 0) sb.Append(strValue);
                                    sb.Append(elemStr);
                                }
                                else
                                {
                                    goto runtime_method;
                                }
                            }
                            string result = sb.ToString();
                            string resultLabel = $"str_{labelCounter++}";
                            dataSection[resultLabel] = WStr(result);
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel));
                            return;
                        }
                        break;

                    case "upper":
                    {
                        string result = strValue.ToUpper();
                        string resultLabel = $"str_{labelCounter++}";
                        dataSection[resultLabel] = WStr(result);
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel));
                        return;
                    }

                    case "lower":
                    {
                        string result = strValue.ToLower();
                        string resultLabel = $"str_{labelCounter++}";
                        dataSection[resultLabel] = WStr(result);
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel));
                        return;
                    }

                    case "strip":
                    {
                        string result = strValue.Trim();
                        string resultLabel = $"str_{labelCounter++}";
                        dataSection[resultLabel] = WStr(result);
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, resultLabel));
                        return;
                    }

                    case "startswith":
                        if (node.Args.Count >= 1 && node.Args[0] is ConstantNode prefixCn && prefixCn.Value is string prefix)
                        {
                            bool startsWith = strValue.StartsWith(prefix);
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, startsWith ? 1 : 0));
                            return;
                        }
                        break;

                    case "endswith":
                        if (node.Args.Count >= 1 && node.Args[0] is ConstantNode suffixCn && suffixCn.Value is string suffix)
                        {
                            bool endsWith = strValue.EndsWith(suffix);
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, endsWith ? 1 : 0));
                            return;
                        }
                        break;

                    case "find":
                        if (node.Args.Count >= 1 && node.Args[0] is ConstantNode subCn && subCn.Value is string sub)
                        {
                            int idx = strValue.IndexOf(sub);
                            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, idx));
                            return;
                        }
                        break;
                }
            }

            runtime_method:
            // 运行时方法调用：求值 receiver，压栈，通过 SYSCALL 调用
            node.Receiver.Accept(this);

            // 压入参数（从右到左）
            for (int i = node.Args.Count - 1; i >= 0; i--)
            {
                node.Args[i].Accept(this);
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            }

            // 压入 receiver 作为第一个隐式参数
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

            // 通过 SYSCALL 分派字符串方法
            switch (node.Method)
            {
                case "split":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 50));
                    break;
                case "join":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 51));
                    break;
                case "replace":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 52));
                    break;
                case "upper":
                    EmitGetTick();
                    break;
                case "lower":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 54));
                    break;
                case "strip":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 55));
                    break;
                case "startswith":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 56));
                    break;
                case "endswith":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 57));
                    break;
                case "find":
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 58));
                    break;
                default:
                    // 未知方法：只保留 receiver
                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                    break;
            }

            // 清理栈上参数（args + receiver）
            int totalPushed = node.Args.Count + 1;
            if (totalPushed > 0)
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, totalPushed * 4));
        }
    }
}
