using VMLAssembler;
using CompilerBase;

namespace LadderCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(AssignmentNode node)
        {
            // 计算右侧表达式
            Visit(node.Value);
            // 存储到左侧变量
            StoreVariable(node.Variable);
        }

        public void Visit(ExpressionNode node)
        {
            switch (node)
            {
                case IdentifierNode id: Visit(id); break;
                case LiteralNode literal: Visit(literal); break;
                case BinaryExpressionNode binary: Visit(binary); break;
                case UnaryExpressionNode unary: Visit(unary); break;
                case CallNode call: Visit(call); break;
                case ArrayAccessNode array: Visit(array); break;
                case MemberAccessNode member: Visit(member); break;
            }
        }

        public void Visit(IdentifierNode node)
        {
            // 加载标识符（变量）的值到R0
            LoadVariable(node.Name);
        }

        public void Visit(ArrayAccessNode node)
        {
            string target = ExpressionToStorageName(node);
            if (!string.IsNullOrEmpty(target))
            {
                LoadVariable(target);
                return;
            }

            AddInstruction(OpCode.MOVE,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
        }

        public void Visit(MemberAccessNode node)
        {
            string target = ExpressionToStorageName(node);
            LoadVariable(target);
        }

        private string ExpressionToStorageName(ExpressionNode node)
        {
            switch (node)
            {
                case IdentifierNode id:
                    return id.Name;
                case MemberAccessNode member:
                    return $"{ExpressionToStorageName(member.Target)}.{member.Member}";
                case ArrayAccessNode array:
                    string target = ExpressionToStorageName(array.Target);
                    string index = ExpressionToConstantString(array.Index);
                    return string.IsNullOrEmpty(index) ? null : $"{target}[{index}]";
                default:
                    return null;
            }
        }

        private string ExpressionToConstantString(ExpressionNode node)
        {
            switch (node)
            {
                case LiteralNode lit:
                    return lit.Value?.ToString();
                case IdentifierNode id when _enumValues.ContainsKey(id.Name):
                    return _enumValues[id.Name].ToString();
                default:
                    return null;
            }
        }

        public void Visit(LiteralNode node)
        {
            // 加载字面量到R0
            switch (node.Value)
            {
                case bool b:
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, b ? 1 : 0));
                    break;
                case int i:
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, i));
                    break;
                case float f:
                    AddInstruction(OpCode.MOVEF,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, f));
                    break;
                case double d:
                    AddInstruction(OpCode.MOVED,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, d));
                    break;
                case string s:
                    // 字符串字面量
                    if (s.StartsWith("0x"))
                    {
                        int val = Convert.ToInt32(s.Substring(2), 16);
                        AddInstruction(OpCode.MOVE,
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, val));
                    }
                    else if (s.StartsWith("0b"))
                    {
                        int val = Convert.ToInt32(s.Substring(2), 2);
                        AddInstruction(OpCode.MOVE,
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, val));
                    }
                    else if (int.TryParse(s, out int intVal))
                    {
                        AddInstruction(OpCode.MOVE,
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, intVal));
                    }
                    else
                    {
                        // 字符串常量
                        string constName = $"_str{labelCounter++}";
                        dataSection[constName] = s;
                        AddInstruction(OpCode.MOVE,
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.LABEL, constName));
                    }
                    break;
                default:
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0));
                    break;
            }
        }

        public void Visit(BinaryExpressionNode node)
        {
            string op = node.Operator.ToUpper();
            // 比较运算符 — 需要手动 PUSH/POP (GenerateCompare 期望 R1=左 R0=右)
            if (op == "=" || op == "<>" || op == "<" || op == "<=" || op == ">" || op == ">=")
            {
                Visit(node.Left);
                AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                Visit(node.Right);
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                GenerateCompare(node, op switch {
                    "=" => OpCode.JE, "<>" => OpCode.JNE, "<" => OpCode.JL,
                    "<=" => OpCode.JLE, ">" => OpCode.JG, ">=" => OpCode.JGE,
                    _ => OpCode.JE
                }, 1, 0);
                return;
            }
            // 算术/位运算 — 使用基类 EmitBinaryOp
            EmitBinaryOp(() => Visit(node.Left), () => Visit(node.Right), op.ToLower());
        }

        private void GenerateCompare(BinaryExpressionNode node, OpCode jumpOp, int leftReg, int rightReg)
        {
            string trueLabel = NewLabel("cmp_true");
            string endLabel = NewLabel("cmp_end");
            
            AddInstruction(OpCode.CMP,
                new Operand(OperandType.REGISTER, leftReg),
                new Operand(OperandType.REGISTER, rightReg));
            AddInstruction(jumpOp, new Operand(OperandType.LABEL, trueLabel));
            AddInstruction(OpCode.MOVE,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));
            labels[trueLabel] = instructions.Count;
            AddInstruction(OpCode.MOVE,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 1));
            labels[endLabel] = instructions.Count;
        }

        public void Visit(UnaryExpressionNode node)
        {
            // 计算操作数
            Visit(node.Operand);
            
            switch (node.Operator.ToUpper())
            {
                case "NOT":
                    AddInstruction(OpCode.CMP,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0));
                    string notZero = NewLabel("not_zero");
                    string notEnd = NewLabel("not_end");
                    AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, notZero));
                    AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, notEnd));
                    labels[notZero] = instructions.Count;
                    AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                    labels[notEnd] = instructions.Count;
                    break;
                case "-":
                    AddInstruction(OpCode.NEG,
                        new Operand(OperandType.REGISTER, 0));
                    break;
            }
        }

        public void Visit(CallNode node)
        {
            string name = node.FunctionName.ToUpperInvariant();
            switch (name)
            {
                // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Ladder 通过 Lib/shared/vmlsys.c 调用系统功能
                case "SEL":
                    GenerateSel(node);
                    return;
                case "MUX":
                    GenerateMux(node);
                    return;
                case "ABS":
                    GenerateAbs(node);
                    return;
                case "SQRT":
                    GenerateSqrt(node);
                    return;
                case "LIMIT":
                    GenerateLimit(node);
                    return;
                case "MAX":
                    GenerateVariadicMinMax(node, true);
                    return;
                case "MIN":
                    GenerateVariadicMinMax(node, false);
                    return;
                case "PRINT_INT":
                    if (node.Arguments.Count > 0)
                        Visit(node.Arguments[0]);
                    AddInstruction(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 6));
                    return;
                case "PRINT_FLOAT":
                    if (node.Arguments.Count > 0)
                        Visit(node.Arguments[0]);
                    AddInstruction(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 8));
                    return;
                case "PRINT_STR":
                    if (node.Arguments.Count > 0)
                        Visit(node.Arguments[0]);
                    AddInstruction(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 1));
                    return;
                case "PRINT_CHAR":
                    if (node.Arguments.Count > 0)
                        Visit(node.Arguments[0]);
                    AddInstruction(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 4));
                    return;
            }

            int argCount = Math.Min(node.Arguments.Count, 8);
            for (int i = argCount - 1; i >= 0; i--)
            {
                Visit(node.Arguments[i]);
                AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            }

            for (int i = 0; i < argCount; i++)
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, i));

            // 寄存器保护 CALL (PUSH R0-R3; CALL; POP R3-R1; ADD R13,#4)
            EmitCallWithRegSave(name.StartsWith("LADDER_") ? name : $"LADDER_{name}");
        }

        private void GenerateSel(CallNode node)
        {
            if (node.Arguments.Count < 3) return;
            Visit(node.Arguments[0]);
            string falseLabel = NewLabel("sel_false");
            string endLabel = NewLabel("sel_end");
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JE, new Operand(OperandType.LABEL, falseLabel));
            Visit(node.Arguments[2]);
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));
            labels[falseLabel] = instructions.Count;
            Visit(node.Arguments[1]);
            labels[endLabel] = instructions.Count;
        }

        private void GenerateMux(CallNode node)
        {
            if (node.Arguments.Count < 2) return;
            Visit(node.Arguments[0]);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            string endLabel = NewLabel("mux_end");
            for (int i = 1; i < node.Arguments.Count; i++)
            {
                string nextLabel = NewLabel("mux_next");
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 1));
                AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, i - 1));
                AddInstruction(OpCode.JNE, new Operand(OperandType.LABEL, nextLabel));
                Visit(node.Arguments[i]);
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));
                labels[nextLabel] = instructions.Count;
            }
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            Visit(node.Arguments[1]);
            labels[endLabel] = instructions.Count;
        }

        private void GenerateAbs(CallNode node)
        {
            if (node.Arguments.Count < 1) return;
            Visit(node.Arguments[0]);
            string absEnd = NewLabel("ae");
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, absEnd));
            AddInstruction(OpCode.NEG, new Operand(OperandType.REGISTER, 0));
            labels[absEnd] = instructions.Count;
        }

        private void GenerateSqrt(CallNode node)
        {
            if (node.Arguments.Count < 1) return;
            Visit(node.Arguments[0]);
            string sl = NewLabel("sl"); string se = NewLabel("se");
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            var divOps1 = new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 2) };
            AddInstruction(OpCode.DIV, divOps1);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, se));
            labels[sl] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0));
            var divOps2 = new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1) };
            AddInstruction(OpCode.DIV, divOps2);
            var addOps = new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) };
            AddInstruction(OpCode.ADD, addOps);
            var divOps3 = new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 2) };
            AddInstruction(OpCode.DIV, divOps3);
            var mulOps = new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) };
            AddInstruction(OpCode.MUL, mulOps);
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0));
            AddInstruction(OpCode.JG, new Operand(OperandType.LABEL, sl));
            labels[se] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
        }

        private void GenerateLimit(CallNode node)
        {
            if (node.Arguments.Count < 3) return;
            Visit(node.Arguments[0]);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            Visit(node.Arguments[1]);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            string aboveMin = NewLabel("limit_above_min");
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            AddInstruction(OpCode.JGE, new Operand(OperandType.LABEL, aboveMin));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            labels[aboveMin] = instructions.Count;

            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 1));
            Visit(node.Arguments[2]);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            string belowMax = NewLabel("limit_below_max");
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            AddInstruction(OpCode.JLE, new Operand(OperandType.LABEL, belowMax));
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            labels[belowMax] = instructions.Count;
            AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
        }

        private void GenerateVariadicMinMax(CallNode node, bool max)
        {
            if (node.Arguments.Count == 0)
            {
                AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                return;
            }

            Visit(node.Arguments[0]);
            for (int i = 1; i < node.Arguments.Count; i++)
            {
                AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                Visit(node.Arguments[i]);
                AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                string keep = NewLabel(max ? "max_keep" : "min_keep");
                AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
                AddInstruction(max ? OpCode.JGE : OpCode.JLE, new Operand(OperandType.LABEL, keep));
                AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
                labels[keep] = instructions.Count;
                AddInstruction(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
            }
        }

        // ====== ST (Structured Text) 语句代码生成 ======

        public void Visit(StIfNode node)
        {
            Sta!.EmitIf(
                emitCondition: () => node.Condition?.Accept(this),
                emitThen: () => { foreach (var s in node.ThenBody) { if (s is AssignmentNode a) Visit(a); else if (s != null) s.Accept(this); } },
                emitElse: node.ElseBody.Count > 0 ? () => { foreach (var s in node.ElseBody) { if (s is AssignmentNode a) Visit(a); else if (s != null) s.Accept(this); } } : null);
        }

        public void Visit(StForNode node)
        {
            // FOR var := start TO end DO ... END_FOR
            string loopLabel = $"st_for_{labelCounter}";
            string endLabel = $"st_forend_{labelCounter++}";

            // Initialize: var := start
            if (node.Start != null)
                node.Start.Accept(this);
            StoreVariable(node.VarName);

            // Loop start
            AddLabel(loopLabel);
            // Check: var <= end
            LoadVariable(node.VarName);
            AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            if (node.End != null)
                node.End.Accept(this);
            AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 1));
            AddInstruction(OpCode.CMP, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
            AddInstruction(OpCode.JG, new Operand(OperandType.LABEL, endLabel));

            // Body
            foreach (var stmt in node.Body)
            {
                if (stmt is AssignmentNode assign)
                    Visit(assign);
                else if (stmt != null)
                    stmt.Accept(this);
            }

            // Increment: var := var + 1
            LoadVariable(node.VarName);
            AddInstruction(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
            StoreVariable(node.VarName);

            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, loopLabel));
            AddLabel(endLabel);
        }

        public void Visit(StWhileNode node)
        {
            Sta!.EmitWhile(
                emitCondition: () => node.Condition?.Accept(this),
                emitBody: () => { foreach (var s in node.Body) { if (s is AssignmentNode a) Visit(a); else if (s != null) s.Accept(this); } });
        }
    }
}
