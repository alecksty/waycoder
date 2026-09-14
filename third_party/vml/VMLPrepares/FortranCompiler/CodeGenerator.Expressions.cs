using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace FortranCompiler;

public partial class CodeGenerator
{
    private void GenerateExpression(ASTNode node)
    {
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
            default:
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(node.GetType().Name));
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
            // Global variable - auto-allocate in data section
            string dataLabel = $"var_{name}";
            if (!dataSection.ContainsKey(dataLabel))
                dataSection[dataLabel] = 0;

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
                // CALL shared_ipow (__stdcall: 被调用者清栈)
                GenerateExpression(node.Right);  // R0 = exp
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                GenerateExpression(node.Left);   // R0 = base
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "ipow")]));
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
                throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown binary operator: {node.Op}");
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
