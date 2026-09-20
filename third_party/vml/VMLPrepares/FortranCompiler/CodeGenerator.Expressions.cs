using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace FortranCompiler;

public partial class CodeGenerator
{
    /// <summary>
    /// 让随后生成的指令/诊断带上**这个表达式自己的**行列（语义见
    /// `CodeGeneratorBase.CurrentSourceLine`/`CurrentSourceColumn`）。
    ///
    /// 与语句入口那句是同一个道理，但**粒度细一层**：语句入口给出的是「这一句从哪开始」，
    /// 而 `ReportUndefined` 要指的用户真正写错的那个名字。表达式生成是递归的 ⇒
    /// 越往里越精确，最后停在**最内层那个节点**上（`a + b + nosuch` 停在 `nosuch`）。
    ///
    /// 判据 `Line > 0`：位置由解析器的原子入口统一盖（`ParsePrimary`/`ParseFactor`），
    /// 二元/一元节点的 Line 仍是 0 —— 置 0 会把刚盖好的原子位置冲掉，
    /// 那正是「列停在语句起始」的老毛病。
    /// </summary>
    private void GenerateExpression(ASTNode node)
    {
    if (node.Line > 0) { CurrentSourceLine = node.Line; CurrentSourceColumn = node.Column; }
        switch (node)
        {
            case LiteralNode literal:
                GenerateLiteral(literal);
                break;
            case VarNode varNode:
                GenerateVar(varNode);
                break;
            case BinaryNode binary:
                GenerateBinary(binary);
                break;
            case UnaryNode unary:
                GenerateUnary(unary);
                break;
            case AssignNode assign:
                GenerateAssignExpr(assign);
                break;
            case FuncCallNode funcCall:
                GenerateFuncCall(funcCall);
                break;
            case ArrayElemNode arrayElem:
                GenerateArrayElemLoad(arrayElem);
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
        }
    }

    /// <summary>
    /// 数组元素读：`a(i)` → R0 = a[i-1]。
    ///
    /// <para>次序上**先算下标再算基地址**：下标表达式求值会用到 R0/R1，反过来就会被基地址覆盖。</para>
    /// </summary>
    private void GenerateArrayElemLoad(ArrayElemNode node)
    {
        GenerateExpression(node.Index);
        AddRI(OpCode.SUB, 0, 1);   // Fortran 1-based → 0-based
        AddRI(OpCode.MUL, 0, 4);   // 元素 4 字节（与写入侧 GenerateAssign 同一约定）
        EmitArrayBaseAddress(node.Name, 1);
        instructions.Add(new Instruction(OpCode.ADD,
            new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) },
            instructions.Count));
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") },
            instructions.Count));
    }

    /// <summary>
    /// 把数组 <paramref name="name"/> 的**基地址**（元素 0 的地址）算到 <paramref name="destReg"/>。
    /// 四种来源，判据按「这块内存在哪」分：
    /// <list type="bullet">
    /// <item>固定长度局部数组（`integer :: a(4)`，声明进了 ArraySizes）—— 整块就在栈帧里，
    ///       基地址 = R12 - 偏移；偏移是 <see cref="TypedCodeGen{T}.MemOff"/> 的正数约定。</item>
    /// <item>形参数组（`subroutine f(x)` 里的 x，偏移为负 = 帧上方参数区）—— 槽里存的是
    ///       调用方传进来的地址，解引用即可。</item>
    /// <item>可分配数组（`allocate(a(n))`）—— 槽里是 malloc 来的指针，且
    ///       <see cref="GenerateAllocate"/> 在 [0] 处放了长度头 ⇒ 元素从 +4 开始。</item>
    /// <item>其余（全局）—— 数据段标签本身就是地址。</item>
    /// </list>
    /// </summary>
    private void EmitArrayBaseAddress(string name, int destReg)
    {
        string key = name.ToLowerInvariant();
        if (symbolTable.TryGetValue(key, out int offset))
        {
            if (offset < 0)
            {
                // 形参数组：参数区在 R12 上方，槽里是地址
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.MEMORY, MemOff(offset)) },
                    instructions.Count));
            }
            else if (_arraySizes.ContainsKey(key))
            {
                // 固定长度局部数组：R12 - offset 就是元素 0 的地址
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.REGISTER, 12) },
                    instructions.Count));
                if (offset != 0)
                    instructions.Add(new Instruction(OpCode.SUB,
                        new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.IMMEDIATE, offset) },
                        instructions.Count));
            }
            else
            {
                // 可分配数组：解引用拿首地址，跳过 [0] 的长度头
                instructions.Add(new Instruction(OpCode.MOVE,
                    new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.MEMORY, MemOff(offset)) },
                    instructions.Count));
                instructions.Add(new Instruction(OpCode.ADD,
                    new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.IMMEDIATE, 4) },
                    instructions.Count));
            }
        }
        else
        {
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, destReg), new Operand(OperandType.LABEL, $"var_{key}") },
                instructions.Count));
        }
    }

    private void GenerateLiteral(LiteralNode node) => EmitLoadConstant(node.Value);

    private void GenerateVar(VarNode node)
    {
        string name = node.Name.ToLowerInvariant();
        // 获取变量的 Fortran 类型 (如果已记录) 用于类型感知指令选择
        FortranType ft = _varTypes.TryGetValue(name, out var t) ? t : FortranType.Integer;
        OpCode loadOp = GetLoadInstruction(ft);

        if (symbolTable.TryGetValue(name, out int offset))
        {
            instructions.Add(new Instruction(loadOp,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, MemOff(offset)) },
                instructions.Count));
        }
        else
        {
            // 局部表与 `dataSection` 都没有 ⇒ 这个名字**没有声明过**。
            //
            // 到这里该报错还是该放过，取决于**这门语言自己的语义**（用户原话：
            // 「少数语言不用声明，根据语言特性来定」）：
            //   · 写了 `implicit none` ⇒ 必须声明 ⇒ **错误**；
            //   · 没写 ⇒ Fortran 的默认隐式类型是**合法语义**（i-n 为 integer、其余 real）
            //     ⇒ 只**警告**，绝不能报错 —— 否则 `Examples/fortran/` 那些老风格程序全编不过。
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
            {
                if (StrictDeclarations)
                    ReportUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                else
                    WarnUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量",
                        "Fortran 默认按首字母隐式定型（i-n 为 integer、其余 real）；"
                        + "要让这类引用直接报错，请在程序开头写 `implicit none`。");
                dataSection[dataLabel] = 0;
            }

            instructions.Add(new Instruction(loadOp,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dataLabel) },
                instructions.Count));
        }
    }

    private void GenerateBinary(BinaryNode node)
    {
        var left = WrapExpr(node.Left);
        var right = WrapExpr(node.Right);

        switch (node.Op)
        {
            case "+": _expr!.EmitBinOp(left, right, "+"); break;
            case "-": _expr!.EmitBinOp(left, right, "-"); break;
            case "*": _expr!.EmitBinOp(left, right, "*"); break;
            case "/": _expr!.EmitBinOp(left, right, "/"); break;
            case "**":
                // ⚠ 这里是 **调用方清栈**（2026-09-17 调用约定统一后补的最后那句 `ADD R13 #8`）。
                //
                // 旧注释写「__stdcall: 被调用者清栈」—— 那时 `Lib` 里的 `ipow` 收尾是
                // `… move R1 @13; add R13 #8; push R1; ret`，替调用方多弹掉两个实参槽，
                // 所以调用方压完不用管。现在 `Lib/` 已按统一约定重生成（被调方一律裸 `ret`），
                // 那句话不再成立：**不补 `ADD R13 #8` 就是每次求幂净漏 8 字节**。
                // 实测（`2 ** i` 累加 i=1..6）：修复前得 `SUM=66`，正确值 `SUM=126`。
                GenerateExpression(node.Right);  // R0 = exp
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(node.Left);   // R0 = base
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "ipow")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 8)]));
                break;
            case "==": _expr!.EmitCmp(left, right, "=="); break;
            case "/=": _expr!.EmitCmp(left, right, "!="); break;
            case "<": _expr!.EmitCmp(left, right, "<"); break;
            case ">": _expr!.EmitCmp(left, right, ">"); break;
            case "<=": _expr!.EmitCmp(left, right, "<="); break;
            case ">=": _expr!.EmitCmp(left, right, ">="); break;
            case ".and.": _expr!.EmitAnd(left, right); break;
            case ".or.": _expr!.EmitOr(left, right); break;
            case ".eqv.": // logical equivalence: (a!=0) == (b!=0)
                _expr!.EmitCmp(WrapExpr(node.Left), WrapExpr(node.Right), "==");
                break;
            case ".neqv.": // logical non-equivalence: (a!=0) != (b!=0)
                _expr!.EmitCmp(WrapExpr(node.Left), WrapExpr(node.Right), "!=");
                break;
            default:
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"未知的二元运算符: {node.Op}");
        }
    }

    private void GenerateUnary(UnaryNode node)
    {
        var operand = WrapExpr(node.Operand);
        switch (node.Op)
        {
            case "-": _expr!.EmitNeg(operand); break;
            case "+": GenerateExpression(node.Operand); break;
            case ".not.": _expr!.EmitNot(operand); break;
            default:
                GenerateExpression(node.Operand);
                break;
        }
    }

    private void GenerateAssignExpr(AssignNode node)
    {
        // 数组元素赋值在 GenerateAssign 中处理，此处仅处理标量赋值
        if (node.ArrayIndices != null && node.ArrayIndices.Count > 0)
        {
            GenerateAssign(node); // delegate to statement handler
            return;
        }
        GenerateExpression(node.Value);
        EmitStoreVar(node.Name);
    }

    private void GenerateFuncCall(FuncCallNode node)
    {
        string fname = node.Name.ToLowerInvariant();

        // Handle Fortran intrinsic type conversion functions
        // All conversion ops require (dst, src) operand format
        if (fname == "int" || fname == "ifix" || fname == "idint")
        {
            // int(x) / ifix(x) / idint(x) — convert to integer
            var argType = GetExprType(node.Arguments[0]);
            GenerateExpression(node.Arguments[0]);
            if (argType == ExpType.F64)
                instructions.Add(new Instruction(OpCode.D2I, [Reg(0), Reg(0)], instructions.Count));
            else if (argType == ExpType.F32)
                instructions.Add(new Instruction(OpCode.F2I, [Reg(0), Reg(0)], instructions.Count));
            // else already integer, no-op
            return;
        }
        if (fname == "real" || fname == "float" || fname == "sngl")
        {
            // real(x) — convert to real (float)
            var argType = GetExprType(node.Arguments[0]);
            GenerateExpression(node.Arguments[0]);
            if (argType == ExpType.I32)
                instructions.Add(new Instruction(OpCode.I2F, [Reg(0), Reg(0)], instructions.Count));
            else if (argType == ExpType.F64)
                instructions.Add(new Instruction(OpCode.D2F, [Reg(0), Reg(0)], instructions.Count));
            // else already float, no-op
            return;
        }
        if (fname == "dble" || fname == "dfloat")
        {
            // dble(x) — convert to double precision
            var argType = GetExprType(node.Arguments[0]);
            GenerateExpression(node.Arguments[0]);
            if (argType == ExpType.I32)
                instructions.Add(new Instruction(OpCode.I2D, [Reg(0), Reg(0)], instructions.Count));
            else if (argType == ExpType.F32)
                instructions.Add(new Instruction(OpCode.F2D, [Reg(0), Reg(0)], instructions.Count));
            // else already double, no-op
            return;
        }
        if (fname == "ichar" || fname == "iachar")
        {
            // ichar(c) / iachar(c) — 字符转ASCII值（字符即按整数存储，本质无操作）
            GenerateExpression(node.Arguments[0]);
            return;
        }
        if (fname == "char" || fname == "achar")
        {
            // char(i) / achar(i) — 整数转字符（字符即按整数存储，本质无操作）
            GenerateExpression(node.Arguments[0]);
            return;
        }
        // mod(a, b) —— Fortran 的取模，VML 有原生 MOD 指令。
        // 不拦的话会走到下面的通用 `CALL func_mod`，而**库里没有 mod 这个标签**（实测），
        // 于是「未解析标签 func_mod」→ 运行期 KeyNotFound：gcd/质数/进制转换这类
        // 真例子（Examples/fortran 的 gcd2/prime2/sum_digits2）全都卡在这一句上。
        if (fname == "mod" || fname == "modulo")
        {
            _expr!.EmitBinOp(WrapExpr(node.Arguments[0]), WrapExpr(node.Arguments[1]), "%");
            return;
        }
        if (fname == "outint")
        {
            // outint(x) — SYSCALL #4: print_int
            var argType = GetExprType(node.Arguments[0]);
            GenerateExpression(node.Arguments[0]);
            if (argType == ExpType.F32)
                instructions.Add(new Instruction(OpCode.F2I, [Reg(0), Reg(0)], instructions.Count));
            else if (argType == ExpType.F64)
                instructions.Add(new Instruction(OpCode.D2I, [Reg(0), Reg(0)], instructions.Count));
            instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 4)], instructions.Count));
            return;
        }

        // Push arguments in reverse order (cdecl style)
        for (int i = node.Arguments.Count - 1; i >= 0; i--)
        {
            GenerateExpression(node.Arguments[i]);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
        }

        string funcLabel = $"func_{fname}";
        instructions.Add(new Instruction(OpCode.CALL,
            new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }, instructions.Count));

        // Pop arguments
        for (int i = 0; i < node.Arguments.Count; i++)
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
    }

    // ---- Helpers ----
}
