using VMLAssembler;

namespace CompilerBase
{
    /// <summary>
    /// 统一表达式代码生成管理器 — 类型敏感指令选择 + 二元/比较/逻辑/转换模板
    /// 所有编译器通过 (int byteSize, bool isFloat, bool isDouble) 接口映射各自的类型系统
    /// </summary>
    public class ExpressionManager
    {
        private readonly Action<OpCode, List<Operand>> _emit;
        private readonly Func<string> _newLabel;
        private readonly Action<string> _placeLabel;
        private readonly int _fpReg;
        private readonly bool _threeOpInt;

        /// <summary>
        /// </summary>
        /// <param name="emit">指令发射回调 (opcode, operands)</param>
        /// <param name="newLabel">生成新标签名</param>
        /// <param name="placeLabel">在当前位置放置标签</param>
        /// <param name="framePointerReg">帧指针寄存器 (默认 R12, Go 用 R14)</param>
        /// <param name="threeOperandInt">整数运算是否使用 3 操作数 (R0,R1,R0) 格式</param>
        public ExpressionManager(
            Action<OpCode, List<Operand>> emit,
            Func<string> newLabel,
            Action<string> placeLabel,
            int framePointerReg = 12,
            bool threeOperandInt = false)
        {
            _emit = emit;
            _newLabel = newLabel;
            _placeLabel = placeLabel;
            _fpReg = framePointerReg;
            _threeOpInt = threeOperandInt;
        }

        // ====== 静态：类型大小 → OpCode 选择 ======

        public static OpCode SelectLoadOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.MOVEL;
            if (isDouble || byteSize == 8 && !isFloat) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return byteSize switch { 1 => OpCode.MOVEB, 2 => OpCode.MOVEH, _ => OpCode.MOVE };
        }

        // v1.65.170+: SelectStoreOp 统一使用 MOVE 系列操作码（dest-first 格式）
        // 运行时 dispatch 已删除 STORE/FSTORE/DSTORE, 只能使用 MOVE/MOVEF/MOVED
        public static OpCode SelectStoreOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.MOVEL;
            if (isDouble || byteSize == 8 && !isFloat) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return byteSize switch { 1 => OpCode.MOVEB, 2 => OpCode.MOVEH, _ => OpCode.MOVE };
        }

        public static OpCode SelectStoreUnifiedOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.MOVEL;
            if (isDouble || byteSize == 8 && !isFloat) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return byteSize switch { 1 => OpCode.MOVEB, 2 => OpCode.MOVEH, _ => OpCode.MOVE };
        }

        public static OpCode SelectPushOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.PUSHL;
            if (isDouble || byteSize == 8 && !isFloat) return OpCode.DPUSH;
            if (isFloat) return OpCode.FPUSH;
            return byteSize switch { 1 => OpCode.PUSHB, 2 => OpCode.PUSHH, _ => OpCode.PUSH };
        }

        public static OpCode SelectPopOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.POPL;
            if (isDouble || byteSize == 8 && !isFloat) return OpCode.DPOP;
            if (isFloat) return OpCode.FPOP;
            return byteSize switch { 1 => OpCode.POPB, 2 => OpCode.POPH, _ => OpCode.POP };
        }

        public static OpCode SelectMoveOp(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.MOVEL;
            if (isDouble) return OpCode.MOVED;
            if (isFloat) return OpCode.MOVEF;
            return byteSize switch { 1 => OpCode.MOVEB, 2 => OpCode.MOVEH, _ => OpCode.MOVE };
        }

        public static OpCode SelectArithmeticOp(string op, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return op switch { "+" => OpCode.ADDL, "-" => OpCode.SUBL, "*" => OpCode.MULL, "/" => OpCode.DIVL, "%" => OpCode.MODL, _ => SelectBitwiseOp(op) };
            if (isDouble) return op switch { "+" => OpCode.DADD, "-" => OpCode.DSUB, "*" => OpCode.DMUL, "/" => OpCode.DDIV, "%" => OpCode.MOD, _ => SelectBitwiseOp(op) };
            if (isFloat)  return op switch { "+" => OpCode.FADD, "-" => OpCode.FSUB, "*" => OpCode.FMUL, "/" => OpCode.FDIV, "%" => OpCode.MOD, _ => SelectBitwiseOp(op) };
            return op switch { "+" => OpCode.ADD, "-" => OpCode.SUB, "*" => OpCode.MUL, "/" => OpCode.DIV, "%" => OpCode.MOD, _ => SelectBitwiseOp(op) };
        }

        public static OpCode SelectArithOp(string op, int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => SelectArithmeticOp(op, isFloat, isDouble, isLong);

        public static OpCode SelectCompareOp(bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.CMPL;
            if (isDouble) return OpCode.DCMP;
            if (isFloat) return OpCode.FCMP;
            return OpCode.CMP;
        }

        public static OpCode SelectNegOp(bool isFloat, bool isDouble = false, bool isLong = false)
        {
            if (isLong) return OpCode.NEGL;
            if (isDouble) return OpCode.DNEG;
            if (isFloat) return OpCode.FNEG;
            return OpCode.NEG;
        }

        // ====== ExpVar：加载 / 存储 / 推栈 / 弹栈 ======

        /// <summary>将 ExpVar 的值加载到 R0。Eval() 变量在此触发求值</summary>
        public void EmitLoad(ExpVar v)
        {
            // 延迟求值：首次访问时执行
            v.EvalAction?.Invoke();

            var op = SelectLoadOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong);
            switch (v.Loc)
            {
                case ExpLoc.Reg:
                    if (v.RegNum != 0)
                        _emit(SelectMoveOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong), [R(0), R(v.RegNum)]);
                    break;
                case ExpLoc.Stack:
                    if (v.IsReference)
                    {
                        _emit(OpCode.MOVE, [R(0), Mem(v.FormatOffset())]);
                        _emit(op, [R(0), Mem("R0")]);
                    }
                    else
                        _emit(op, [R(0), Mem(v.FormatOffset())]);
                    break;
                case ExpLoc.Data:
                    if (v.IsReference)
                    {
                        _emit(OpCode.MOVE, [R(0), Mem(v.Label!)]);
                        _emit(op, [R(0), Mem("R0")]);
                    }
                    else
                        _emit(op, [R(0), Mem(v.Label!)]);
                    break;
                case ExpLoc.Imm:
                    _emit(SelectMoveOp(v.ByteSize, false, false, v.IsLong), [R(0), Imm(v.ImmValue!)]);
                    break;
            }
        }

        /// <summary>将 R0 存储到 ExpVar</summary>
        public void EmitStore(ExpVar v)
        {
            var op = SelectStoreUnifiedOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong);
            switch (v.Loc)
            {
                case ExpLoc.Reg:
                    if (v.RegNum != 0)
                        _emit(SelectMoveOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong), [R(v.RegNum), R(0)]);
                    break;
                case ExpLoc.Stack:
                    if (v.IsReference)
                    {
                        _emit(OpCode.MOVE, [R(1), R(0)]);
                        _emit(OpCode.MOVE, [R(0), Mem(v.FormatOffset())]);
                        _emit(op, [Mem("R0"), R(1)]);   // 统一 store: dest=mem, src=reg
                        _emit(OpCode.MOVE, [R(0), R(1)]);
                    }
                    else
                        _emit(op, [Mem(v.FormatOffset()), R(0)]);  // 统一 store: dest=mem, src=reg
                    break;
                case ExpLoc.Data:
                    if (v.IsReference)
                    {
                        _emit(OpCode.MOVE, [R(1), R(0)]);
                        _emit(OpCode.MOVE, [R(0), Mem(v.Label!)]);
                        _emit(op, [Mem("R0"), R(1)]);   // 统一 store: dest=mem, src=reg
                        _emit(OpCode.MOVE, [R(0), R(1)]);
                    }
                    else
                        _emit(op, [Mem(v.Label!), R(0)]);  // 统一 store: dest=mem, src=reg
                    break;
                // Imm 不存储
            }
        }

        /// <summary>Push ExpVar 到栈</summary>
        public void EmitPush(ExpVar v)
        {
            EmitLoad(v);
            _emit(SelectPushOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong), [R(0)]);
        }

        /// <summary>从栈 Pop 到 ExpVar</summary>
        public void EmitPop(ExpVar v)
        {
            var op = SelectPopOp(v.ByteSize, v.IsFloat, v.IsDouble, v.IsLong);
            _emit(op, [v.Loc == ExpLoc.Reg ? R(v.RegNum) : R(0)]);
            if (v.Loc != ExpLoc.Reg || v.RegNum == 0)
                EmitStore(v);
        }

        // ====== ExpVar：类型提升 ======

        /// <summary>二元运算结果类型：取 wider type</summary>
        public static ExpType WidenType(ExpType a, ExpType b)
        {
            if (a == b) return a;
            // 指针运算：指针 ± 整数 → 指针, 整数 ± 指针 → 指针
            if (a.IsPtr() && !b.IsPtr()) return a;
            if (!a.IsPtr() && b.IsPtr()) return b;
            // 指针 ± 指针 → 整数 (ptrdiff_t)
            if (a.IsPtr() && b.IsPtr()) return ExpType.I32;
            if (a == ExpType.F64 || b == ExpType.F64) return ExpType.F64;
            if (a == ExpType.F32 || b == ExpType.F32) return ExpType.F32;
            if (a == ExpType.I64 || b == ExpType.I64 || a == ExpType.U64 || b == ExpType.U64) return ExpType.I64;
            return ExpType.I32; // 默认
        }

        // ====== ExpVar：一元运算 ======

        /// <summary>一元取负 (-x)，结果在新 ExpVar(R0) 中</summary>
        public ExpVar EmitNeg(ExpVar v)
        {
            EmitLoad(v);
            _emit(SelectNegOp(v.IsFloat, v.IsDouble, v.IsLong), [R(0), R(0)]);
            return ExpVar.Reg(0, v.Type);
        }

        /// <summary>逻辑非 (!x)，结果在 ExpVar(R0, I32)</summary>
        public ExpVar EmitNot(ExpVar v)
        {
            EmitLoad(v);
            string isZero = _newLabel();
            string end = _newLabel();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(isZero)]);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _emit(OpCode.JMP, [Lbl(end)]);
            _placeLabel(isZero);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _placeLabel(end);
            return ExpVar.Reg(0, ExpType.I32);
        }

        // ====== ExpVar：位运算 ======

        public static OpCode SelectBitwiseOp(string op) => op switch
        {
            "&" => OpCode.AND, "|" => OpCode.OR, "^" => OpCode.XOR,
            "<<" => OpCode.SHL, ">>" => OpCode.SHR,
            _ => OpCode.AND
        };

        /// <summary>按位与 left &amp; right</summary>
        public ExpVar EmitBitAnd(ExpVar left, ExpVar right)
            => EmitBitOp(left, right, "&");

        /// <summary>按位或 left | right</summary>
        public ExpVar EmitBitOr(ExpVar left, ExpVar right)
            => EmitBitOp(left, right, "|");

        /// <summary>按位异或 left ^ right</summary>
        public ExpVar EmitBitXor(ExpVar left, ExpVar right)
            => EmitBitOp(left, right, "^");

        /// <summary>左移 left &lt;&lt; right</summary>
        public ExpVar EmitShl(ExpVar left, ExpVar right)
            => EmitBitOp(left, right, "<<");

        /// <summary>右移 left &gt;&gt; right</summary>
        public ExpVar EmitShr(ExpVar left, ExpVar right)
            => EmitBitOp(left, right, ">>");

        /// <summary>按位取反 ~x</summary>
        public ExpVar EmitBitNot(ExpVar v)
        {
            EmitLoad(v);
            _emit(OpCode.NOT, [R(0), R(0)]);
            return ExpVar.Reg(0, v.Type);
        }

        private ExpVar EmitBitOp(ExpVar left, ExpVar right, string op)
        {
            var resultType = WidenType(left.Type, right.Type);
            int s = resultType.ByteSize();
            bool l = resultType.IsLong();

            EmitLoad(left);
            _emit(SelectPushOp(s, false, false, l), [R(0)]);
            EmitLoad(right);
            _emit(SelectPopOp(s, false, false, l), [R(1)]);

            var bitOp = SelectBitwiseOp(op);
            _emit(bitOp, [R(1), R(0)]);
            _emit(SelectMoveOp(s, false, false, l), [R(0), R(1)]);
            return ExpVar.Reg(0, resultType);
        }

        // ====== ExpVar：自增/自减 ======

        /// <summary>前缀自增 var = var + 1，返回新值</summary>
        public ExpVar EmitPrefixInc(ExpVar target)
            => EmitIncDec(target, "+", prefix: true);

        /// <summary>前缀自减 var = var - 1，返回新值</summary>
        public ExpVar EmitPrefixDec(ExpVar target)
            => EmitIncDec(target, "-", prefix: true);

        /// <summary>后缀自增 var = var + 1，返回旧值</summary>
        public ExpVar EmitPostfixInc(ExpVar target)
            => EmitIncDec(target, "+", prefix: false);

        /// <summary>后缀自减 var = var - 1，返回旧值</summary>
        public ExpVar EmitPostfixDec(ExpVar target)
            => EmitIncDec(target, "-", prefix: false);

        private ExpVar EmitIncDec(ExpVar target, string op, bool prefix)
        {
            int scale = target.PointedTypeSize > 1 ? target.PointedTypeSize : 1;
            bool isLong = target.IsLong;
            var arithOp = op == "+" ? (isLong ? OpCode.ADDL : OpCode.ADD)
                                    : (isLong ? OpCode.SUBL : OpCode.SUB);

            if (prefix)
            {
                // ++x: R0 = load; R0 +/-= scale; store; return R0 (新值)
                EmitLoad(target);
                _emit(arithOp, [R(0), R(0), Imm(scale)]);
                EmitStore(target);
                return ExpVar.Reg(0, target.Type);
            }
            else
            {
                // x++: R0 = load (旧值); R1 = R0 (保存); R0 +/-= scale; store; R0 = R1 (恢复旧值)
                EmitLoad(target);
                _emit(SelectMoveOp(target.ByteSize, target.IsFloat, target.IsDouble, target.IsLong), [R(1), R(0)]);
                _emit(arithOp, [R(0), R(0), Imm(scale)]);
                EmitStore(target);
                _emit(SelectMoveOp(target.ByteSize, target.IsFloat, target.IsDouble, target.IsLong), [R(0), R(1)]);
                return ExpVar.Reg(0, target.Type);
            }
        }

        // ====== ExpVar：类型转换 ======

        /// <summary>类型转换，结果在 ExpVar(R0, targetType)</summary>
        public ExpVar EmitConvert(ExpVar v, ExpType targetType)
        {
            if (v.Type == targetType) return v.Loc == ExpLoc.Reg && v.RegNum == 0 ? v : ExpVar.Reg(0, targetType);
            EmitLoad(v);
            var op = SelectConversionOp(v.ByteSize, v.IsFloat, v.IsDouble,
                                         targetType.ByteSize(), targetType.IsFloat(), targetType.IsDouble(),
                                         v.IsLong, targetType.IsLong());
            if (op != null)
                _emit(op.Value, [R(0), R(0)]);
            return ExpVar.Reg(0, targetType);
        }

        // ====== ExpVar：二元运算 ======

        /// <summary>二元运算 left op right，结果在 ExpVar(R0, resultType)</summary>
        public ExpVar EmitBinOp(ExpVar left, ExpVar right, string op)
        {
            // 优化: x + 0 = x, x - 0 = x (避免多余的 PUSH/POP)
            if ((op == "+" || op == "-") && IsImmZero(right))
            {
                var resultType = WidenType(left.Type, right.Type);
                EmitLoad(left);
                // 指针 ± 整数缩放 (right=0, 0*size=0, 但 left 可能是指针, MUL R0,#1 不影响)
                if ((op == "+" || op == "-") && left.IsPtr && !right.IsPtr && left.PointedTypeSize > 1)
                    _emit(OpCode.MUL, [R(0), R(0), Imm(left.PointedTypeSize)]);
                if (op == "+" && !left.IsPtr && right.IsPtr && right.PointedTypeSize > 1)
                    _emit(OpCode.MUL, [R(0), R(0), Imm(right.PointedTypeSize)]);
                EmitConvertRaw(left.Type, resultType);
                return ExpVar.Reg(0, resultType);
            }

            // 优化: 0 + x = x
            if (op == "+" && IsImmZero(left))
            {
                var resultType = WidenType(left.Type, right.Type);
                EmitLoad(right);
                // 处理指针 + 整数: left=0 是整数, right 可能是指针
                if (!left.IsPtr && right.IsPtr && right.PointedTypeSize > 1)
                    _emit(OpCode.MUL, [R(0), R(0), Imm(right.PointedTypeSize)]);
                EmitConvertRaw(right.Type, resultType);
                return ExpVar.Reg(0, resultType);
            }

            var resultType2 = WidenType(left.Type, right.Type);
            int s = resultType2.ByteSize();
            bool f = resultType2.IsFloat(), d = resultType2.IsDouble(), l = resultType2.IsLong();

            // 左
            EmitLoad(left);
            // 整数 + 指针：将左操作数（整数）按 sizeof(*ptr) 缩放
            if (op == "+" && !left.IsPtr && right.IsPtr && right.PointedTypeSize > 1)
                _emit(OpCode.MUL, [R(0), R(0), Imm(right.PointedTypeSize)]);
            EmitConvertRaw(left.Type, resultType2);
            _emit(SelectPushOp(s, f, d, l), [R(0)]);

            // 右
            EmitLoad(right);
            // 指针 ± 整数：将右操作数（整数）按 sizeof(*ptr) 缩放
            if ((op == "+" || op == "-") && left.IsPtr && !right.IsPtr && left.PointedTypeSize > 1)
                _emit(OpCode.MUL, [R(0), R(0), Imm(left.PointedTypeSize)]);
            EmitConvertRaw(right.Type, resultType2);
            _emit(SelectPopOp(s, f, d, l), [R(1)]);

            // 计算 — 位运算和算术运算分别处理
            if (op == "&" || op == "|" || op == "^" || op == "<<" || op == ">>")
            {
                // 位运算：使用 SelectBitwiseOp (VML AND/OR/XOR/SHL/SHR)
                var bitOp = SelectBitwiseOp(op);
                if (_threeOpInt)
                    _emit(bitOp, [R(0), R(1), R(0)]);
                else
                {
                    _emit(bitOp, [R(1), R(0)]);
                    _emit(SelectMoveOp(s, false, false, l), [R(0), R(1)]);
                }
            }
            else
            {
                var arithOp = SelectArithmeticOp(op, f, d, l);
                if (f || d || l)
                    _emit(arithOp, [R(0), R(1), R(0)]);
                else if (_threeOpInt)
                    _emit(arithOp, [R(0), R(1), R(0)]);
                else
                {
                    _emit(arithOp, [R(1), R(0)]);
                    _emit(SelectMoveOp(s, false, false, l), [R(0), R(1)]);
                }
            }
            return ExpVar.Reg(0, resultType2);
        }

        private static bool IsImmZero(ExpVar v)
        {
            if (v.Loc != ExpLoc.Imm || v.ImmValue == null) return false;
            return v.ImmValue is 0 or 0L or 0U or 0UL or 0.0f or 0.0;
        }

        /// <summary>比较运算 left cmp right，结果在 ExpVar(R0, I32) 值为 0 或 1</summary>
        public ExpVar EmitCmp(ExpVar left, ExpVar right, string cmpOp)
        {
            var resultType = WidenType(left.Type, right.Type);
            int s = resultType.ByteSize();
            bool f = resultType.IsFloat(), d = resultType.IsDouble(), l = resultType.IsLong();

            EmitLoad(left);
            _emit(SelectPushOp(s, f, d, l), [R(0)]);
            EmitLoad(right);
            EmitConvertRaw(right.Type, resultType);
            _emit(SelectPopOp(s, f, d, l), [R(1)]);

            _emit(SelectCompareOp(f, d, l), [R(1), R(0)]);

            string trueLabel = _newLabel();
            string endLabel = _newLabel();
            OpCode jmpOp = cmpOp switch
            {
                "==" => OpCode.JE, "!=" => OpCode.JNE,
                "<" => OpCode.JL, "<=" => OpCode.JLE,
                ">" => OpCode.JG, ">=" => OpCode.JGE,
                _ => OpCode.JMP
            };
            _emit(jmpOp, [Lbl(trueLabel)]);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            if (l) _emit(OpCode.I2L, [R(0), R(0)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);
            _placeLabel(trueLabel);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            if (l) _emit(OpCode.I2L, [R(0), R(0)]);
            _placeLabel(endLabel);

            return ExpVar.Reg(0, ExpType.I32);
        }

        /// <summary>短路逻辑与 left &amp;&amp; right</summary>
        public ExpVar EmitAnd(ExpVar left, ExpVar right)
        {
            string falseLabel = _newLabel();
            string endLabel = _newLabel();

            EmitLoad(left);
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);
            EmitLoad(right);
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);
            _placeLabel(falseLabel);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _placeLabel(endLabel);
            return ExpVar.Reg(0, ExpType.I32);
        }

        /// <summary>短路逻辑或 left || right</summary>
        public ExpVar EmitOr(ExpVar left, ExpVar right)
        {
            string trueLabel = _newLabel();
            string falseLabel = _newLabel();
            string endLabel = _newLabel();

            EmitLoad(left);
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JNZ, [Lbl(trueLabel)]);
            EmitLoad(right);
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);
            _placeLabel(trueLabel);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);
            _placeLabel(falseLabel);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _placeLabel(endLabel);
            return ExpVar.Reg(0, ExpType.I32);
        }

        // ====== 快捷：变量赋值 ======

        /// <summary>dst = src，dst 被更新，返回 dst</summary>
        public ExpVar EmitAssign(ExpVar dst, ExpVar src)
        {
            if (src.Loc == ExpLoc.Imm)
                _emit(SelectMoveOp(dst.ByteSize, dst.IsFloat, dst.IsDouble, dst.IsLong), [R(0), Imm(src.ImmValue!)]);
            else
                EmitLoad(src);
            EmitStore(dst);
            return dst;
        }

        // ====== ExpVar：复合赋值 ======

        /// <summary>target op= value，如 a += b。支持算术和位运算。返回 target</summary>
        public ExpVar EmitCompoundAssign(ExpVar target, ExpVar value, string op)
        {
            switch (op)
            {
                case "&":  EmitBitAnd(target, value); break;
                case "|":  EmitBitOr(target, value); break;
                case "^":  EmitBitXor(target, value); break;
                case "<<": EmitShl(target, value); break;
                case ">>": EmitShr(target, value); break;
                case "//": EmitBinOp(target, value, "/"); break;  // Python 地板除 = 整数除法
                default:   EmitBinOp(target, value, op); break;
            }
            EmitStore(target);
            return target;
        }

        // ====== 标准二元运算分发（替代各编译器重复的 switch-case） ======

        /// <summary>
        /// 标准算术/比较/逻辑二元运算分发。返回 true=已处理，false=未识别（调用方自行处理语言特有运算符）。
        /// 替代 C#/JS/D/Dart/C/Rust/Go/Python 等编译器中重复的 13-case switch。
        /// </summary>
        public bool EmitStandardBinaryOps(string op, ExpVar left, ExpVar right)
        {
            switch (op)
            {
                case "+": case "-": case "*": case "/": case "%":
                    EmitBinOp(left, right, op); return true;
                case "==": case "!=": case "<": case "<=": case ">": case ">=":
                    EmitCmp(left, right, op); return true;
                case "&&": EmitAnd(left, right); return true;
                case "||": EmitOr(left, right); return true;
                default: return false;
            }
        }

        /// <summary>
        /// 标准位运算分发。返回 true=已处理，false=未识别。
        /// </summary>
        public bool EmitBitwiseOps(string op, ExpVar left, ExpVar right)
        {
            switch (op)
            {
                case "&": EmitBitAnd(left, right); return true;
                case "|": EmitBitOr(left, right); return true;
                case "^": EmitBitXor(left, right); return true;
                case "<<": EmitShl(left, right); return true;
                case ">>": EmitShr(left, right); return true;
                default: return false;
            }
        }

        // ====== ExpVar：三元条件 ======

        /// <summary>cond ? thenExpr : elseExpr，结果在 R0</summary>
        public ExpVar EmitConditional(ExpVar cond, ExpVar thenExpr, ExpVar elseExpr)
        {
            EmitLoad(cond);
            _emit(OpCode.TEST, [R(0), R(0)]);

            string elseLabel = _newLabel();
            string endLabel = _newLabel();

            _emit(OpCode.JZ, [Lbl(elseLabel)]);

            // then 分支
            EmitLoad(thenExpr);
            _emit(OpCode.JMP, [Lbl(endLabel)]);

            // else 分支
            _placeLabel(elseLabel);
            EmitLoad(elseExpr);

            _placeLabel(endLabel);

            var resultType = WidenType(thenExpr.Type, elseExpr.Type);
            return ExpVar.Reg(0, resultType);
        }

        // ====== 内部：raw 类型转换（无 ExpVar 包装，直接在 R0 上操作）=====

        private void EmitConvertRaw(ExpType from, ExpType to)
        {
            if (from == to) return;
            var op = SelectConversionOp(from.ByteSize(), from.IsFloat(), from.IsDouble(),
                                         to.ByteSize(), to.IsFloat(), to.IsDouble(),
                                         from.IsLong(), to.IsLong());
            if (op != null)
                _emit(op.Value, [R(0), R(0)]);
        }

        // ====== 保留旧 Action 委托方法（向后兼容）======

        // ====== 类型转换 ======

        /// <summary>获取从 from 转到 to 所需的转换指令，无需转换返回 null</summary>
        public static OpCode? SelectConversionOp(int fromSize, bool fromFloat, bool fromDouble,
                                                  int toSize,   bool toFloat,   bool toDouble,
                                                  bool fromLong = false, bool toLong = false)
        {
            bool fromF = fromFloat || fromDouble;
            bool toF = toFloat || toDouble;
            // Long conversions
            if (fromLong && !toLong && !toF) return OpCode.L2I;
            if (!fromLong && !fromF && toLong) return OpCode.I2L;
            if (fromLong && toFloat) return OpCode.L2F;
            if (fromFloat && toLong) return OpCode.F2L;
            if (fromLong && toDouble) return OpCode.L2D;
            if (fromDouble && toLong) return OpCode.D2L;
            if (fromF == toF && fromDouble == toDouble && fromLong == toLong) return null;
            // double 检查必须在 float 之前：toDouble=true 隐含 toF=true, 更具体的匹配优先
            if (!fromF && toDouble) return OpCode.I2D;
            if (!fromF && toFloat) return OpCode.I2F;
            if (fromDouble && !toF) return OpCode.D2I;
            if (fromFloat && !toF) return OpCode.F2I;
            if (fromDouble && toFloat) return OpCode.D2F;
            if (fromFloat && toDouble) return OpCode.F2D;
            return null;
        }

        /// <summary>发射类型转换指令（在 R0 上操作）</summary>
        public void EmitConversion(int fromSize, bool fromFloat, bool fromDouble,
                                    int toSize,   bool toFloat,   bool toDouble,
                                    bool fromLong = false, bool toLong = false)
        {
            var op = SelectConversionOp(fromSize, fromFloat, fromDouble, toSize, toFloat, toDouble, fromLong, toLong);
            if (op != null)
                _emit(op.Value, [R(0), R(0)]);
        }

        // ====== 二元表达式模板 ======

        /// <summary>
        /// 发射二元运算: eval left → convert → push → eval right → convert → pop → compute
        /// 结果在 R0 中
        /// </summary>
        public void EmitBinary(Action emitLeft, Action emitRight,
                               int leftSize, bool leftFloat, bool leftDouble,
                               int rightSize, bool rightFloat, bool rightDouble,
                               int resultSize, bool resultFloat, bool resultDouble,
                               string arithmeticOp,
                               bool leftLong = false, bool rightLong = false, bool resultLong = false)
        {
            // 左操作数
            emitLeft();
            EmitConversion(leftSize, leftFloat, leftDouble, resultSize, resultFloat, resultDouble, leftLong, resultLong);
            _emit(SelectPushOp(resultSize, resultFloat, resultDouble, resultLong), [R(0)]);

            // 右操作数
            emitRight();
            EmitConversion(rightSize, rightFloat, rightDouble, resultSize, resultFloat, resultDouble, rightLong, resultLong);

            // 弹出左操作数到 R1
            _emit(SelectPopOp(resultSize, resultFloat, resultDouble, resultLong), [R(1)]);

            // 运算
            var arithOp = SelectArithmeticOp(arithmeticOp, resultFloat, resultDouble, resultLong);
            if (resultFloat || resultDouble || resultLong)
            {
                // 浮点/双精度/长整数始终 3 操作数
                _emit(arithOp, [R(0), R(1), R(0)]);
            }
            else if (_threeOpInt)
            {
                _emit(arithOp, [R(0), R(1), R(0)]);
            }
            else
            {
                _emit(arithOp, [R(1), R(0)]);
                _emit(SelectMoveOp(resultSize, false, false, resultLong), [R(0), R(1)]);
            }
        }

        /// <summary>简化版：左右同类型</summary>
        public void EmitBinary(Action emitLeft, Action emitRight,
                               int byteSize, bool isFloat, bool isDouble, string arithmeticOp)
        {
            EmitBinary(emitLeft, emitRight,
                       byteSize, isFloat, isDouble,
                       byteSize, isFloat, isDouble,
                       byteSize, isFloat, isDouble,
                       arithmeticOp);
        }

        // ====== 比较表达式 ======

        /// <summary>
        /// 发射比较: eval left → push → eval right → pop → CMP → 条件跳转 → R0=0/1
        /// </summary>
        public void EmitCompare(Action emitLeft, Action emitRight,
                                string cmpOp, int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
        {
            // 左操作数
            emitLeft();
            _emit(SelectPushOp(byteSize, isFloat, isDouble, isLong), [R(0)]);
            // 右操作数
            emitRight();
            _emit(SelectPopOp(byteSize, isFloat, isDouble, isLong), [R(1)]);

            // 比较 R1 vs R0
            _emit(SelectCompareOp(isFloat, isDouble, isLong), [R(1), R(0)]);

            string trueLabel = _newLabel();
            string endLabel = _newLabel();

            OpCode jmpOp = cmpOp switch
            {
                "==" => OpCode.JE, "!=" => OpCode.JNE,
                "<" => OpCode.JL, "<=" => OpCode.JLE,
                ">" => OpCode.JG, ">=" => OpCode.JGE,
                _ => OpCode.JMP
            };

            _emit(jmpOp, [Lbl(trueLabel)]);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            if (isLong) _emit(OpCode.I2L, [R(0), R(0)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);

            _placeLabel(trueLabel);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            if (isLong) _emit(OpCode.I2L, [R(0), R(0)]);
            _placeLabel(endLabel);
        }

        // ====== 短路逻辑 ======

        public void EmitLogicalAnd(Action emitLeft, Action emitRight)
        {
            string falseLabel = _newLabel();
            string endLabel = _newLabel();

            emitLeft();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);
            emitRight();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);

            _placeLabel(falseLabel);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _placeLabel(endLabel);
        }

        public void EmitLogicalOr(Action emitLeft, Action emitRight)
        {
            string trueLabel = _newLabel();
            string falseLabel = _newLabel();
            string endLabel = _newLabel();

            emitLeft();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JNZ, [Lbl(trueLabel)]);
            emitRight();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(falseLabel)]);

            _placeLabel(trueLabel);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _emit(OpCode.JMP, [Lbl(endLabel)]);

            _placeLabel(falseLabel);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _placeLabel(endLabel);
        }

        // ====== 一元运算 ======

        public void EmitNegate(bool isFloat, bool isDouble = false)
        {
            _emit(SelectNegOp(isFloat, isDouble), [R(0), R(0)]);
        }

        /// <summary>逻辑非: R0 = (R0 == 0) ? 1 : 0</summary>
        public void EmitLogicalNot()
        {
            string isZero = _newLabel();
            string end = _newLabel();
            _emit(OpCode.TEST, [R(0), R(0)]);
            _emit(OpCode.JZ, [Lbl(isZero)]);
            _emit(OpCode.MOVE, [R(0), Imm(0)]);
            _emit(OpCode.JMP, [Lbl(end)]);
            _placeLabel(isZero);
            _emit(OpCode.MOVE, [R(0), Imm(1)]);
            _placeLabel(end);
        }

        // ====== 字符串操作 ======

        /// <summary>strlen: R0 = 内存地址，返回长度在 R0</summary>
        public void EmitStrLen()
        {
            _emit(OpCode.CALL, [Lbl("strlen")]);
        }

        /// <summary>strcpy: R0 = dst地址, R1 = src地址，发射CALL shared_strcpy</summary>
        public void EmitStrCpy()
        {
            _emit(OpCode.CALL, [Lbl("strcpy")]);
        }

        /// <summary>strcat: R0 = dst地址, R1 = src地址</summary>
        public void EmitStrCat()
        {
            _emit(OpCode.CALL, [Lbl("strcat")]);
        }

        /// <summary>strcmp: R0 = a地址, R1 = b地址，结果R0 (-1/0/1)</summary>
        public void EmitStrCmp()
        {
            _emit(OpCode.CALL, [Lbl("strcmp")]);
        }

        // ====== 内存操作 ======

        /// <summary>memcpy: R0 = dst, R1 = src, R2 = len，发射CALL shared_memcpy</summary>
        public void EmitMemCpy()
        {
            _emit(OpCode.CALL, [Lbl("memcpy")]);
        }

        /// <summary>memset: R0 = dst, R1 = value, R2 = len</summary>
        public void EmitMemSet()
        {
            _emit(OpCode.CALL, [Lbl("memset")]);
        }

        // ====== 便捷操作数工厂 ======

        public Operand R(int n) => new(OperandType.REGISTER, n);
        public static Operand Reg(int n) => new(OperandType.REGISTER, n);
        public static Operand Imm(object v) => new(OperandType.IMMEDIATE, v);
        public static Operand Lbl(string s) => new(OperandType.LABEL, s);
        public static Operand Mem(string s) => new(OperandType.MEMORY, s);
    }
}
