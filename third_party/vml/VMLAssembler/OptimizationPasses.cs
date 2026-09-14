using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>
    /// NOP消除优化 - 删除无意义的NOP指令
    /// </summary>
    public class NopEliminationPass : IOptimizationPass
    {
        public string Name => "NOP消除";
        public string Description => "删除无意义的NOP指令";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableNopElimination) return false;

            bool changed = false;
            var toRemove = new List<int>();

            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                if (instr.Opcode == OpCode.NOP && string.IsNullOrEmpty(instr.Label))
                {
                    toRemove.Add(i);
                    changed = true;
                }
            }

            // 从后向前删除以保持索引正确
            for (int i = toRemove.Count - 1; i >= 0; i--)
            {
                program.Instructions.RemoveAt(toRemove[i]);
            }

            return changed;
        }
    }

    /// <summary>
    /// 常量折叠优化 - 在编译时计算常量表达式
    /// </summary>
    public class ConstantFoldingPass : IOptimizationPass
    {
        public string Name => "常量折叠";
        public string Description => "在编译时计算常量表达式";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableConstantFolding) return false;

            bool changed = false;

            for (int i = 0; i < program.Instructions.Count - 1; i++)
            {
                var instr = program.Instructions[i];
                var next = program.Instructions[i + 1];

                // 模式: LOAD R0, #imm1; ADD R0, #imm2 -> LOAD R0, #(imm1+imm2)
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE &&
                    next.Opcode == OpCode.ADD && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.IMMEDIATE &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    int val1 = GetIntValue(instr.Operands[1].Value);
                    int val2 = GetIntValue(next.Operands[1].Value);
                    instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 + val2);
                    program.Instructions.RemoveAt(i + 1);
                    changed = true;
                    continue;
                }

                // 模式: LOAD R0, #imm1; SUB R0, #imm2 -> LOAD R0, #(imm1-imm2)
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE &&
                    next.Opcode == OpCode.SUB && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.IMMEDIATE &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    int val1 = GetIntValue(instr.Operands[1].Value);
                    int val2 = GetIntValue(next.Operands[1].Value);
                    instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 - val2);
                    program.Instructions.RemoveAt(i + 1);
                    changed = true;
                    continue;
                }

                // 模式: LOAD R0, #imm1; MUL R0, #imm2 -> LOAD R0, #(imm1*imm2)
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE &&
                    next.Opcode == OpCode.MUL && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.IMMEDIATE &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    int val1 = GetIntValue(instr.Operands[1].Value);
                    int val2 = GetIntValue(next.Operands[1].Value);
                    instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 * val2);
                    program.Instructions.RemoveAt(i + 1);
                    changed = true;
                    continue;
                }
            }

            return changed;
        }

        private int GetIntValue(object value)
        {
            if (value is int i) return i;
            if (value is string s && int.TryParse(s, out int parsed)) return parsed;
            return 0;
        }
    }

    /// <summary>
    /// 死存储消除 - 删除写入后从未读取的寄存器存储
    /// </summary>
    public class DeadStoreEliminationPass : IOptimizationPass
    {
        public string Name => "死存储消除";
        public string Description => "删除写入后从未读取的寄存器存储";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableDeadStoreElimination) return false;

            bool changed = false;
            var instructions = program.Instructions;

            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                if (!IsStoreInstruction(instr.Opcode)) continue;
                if (instr.Operands.Count == 0) continue;
                if (instr.Operands[0].Type != OperandType.REGISTER) continue;

                string reg = instr.Operands[0].Value?.ToString() ?? "";
                bool isRead = false;

                for (int j = i + 1; j < instructions.Count; j++)
                {
                    var next = instructions[j];
                    if (next.Label != null && program.Labels.TryGetValue(next.Label, out int labelTarget) && labelTarget <= i)
                    {
                        isRead = true;
                        break;
                    }
                    if (IsReadRegister(next, reg ?? ""))
                    {
                        isRead = true;
                        break;
                    }
                    if (IsStoreInstruction(next.Opcode) && next.Operands.Count > 0 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        break;
                    }
                    if (next.Opcode == OpCode.RET || next.Opcode == OpCode.HALT)
                    {
                        break;
                    }
                }

                if (!isRead)
                {
                    instructions[i] = new Instruction(OpCode.NOP, [], 0, "; dead store eliminated");
                    changed = true;
                }
            }

            return changed;
        }

        private bool IsStoreInstruction(OpCode opcode)
        {
            return opcode == OpCode.MOVE || opcode == OpCode.MOVEF;
        }

        private bool IsReadRegister(Instruction instr, string? reg)
        {
            foreach (var op in instr.Operands)
            {
                if (op.Type == OperandType.REGISTER && op.Value.ToString() == reg)
                {
                    if (instr.Opcode == OpCode.MOVE || instr.Opcode == OpCode.MOVEF)
                    {
                        if (op == instr.Operands[0]) continue;
                    }
                    return true;
                }
                if (op.Type == OperandType.MEMORY && op.Value.ToString() == reg)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 复制传播 - MOVE Rx, Ry 后用Ry替换Rx的使用
    /// </summary>
    public class CopyPropagationPass : IOptimizationPass
    {
        public string Name => "复制传播";
        public string Description => "传播复制指令: MOVE Rx, Ry 后用Ry替换Rx的使用";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableCopyPropagation) return false;

            bool changed = false;
            var instructions = program.Instructions;

            for (int i = 0; i < instructions.Count - 1; i++)
            {
                var instr = instructions[i];
                if (instr.Opcode != OpCode.MOVE || instr.Operands.Count < 2) continue;
                if (instr.Operands[0].Type != OperandType.REGISTER || instr.Operands[1].Type != OperandType.REGISTER) continue;

                string destReg = instr.Operands[0].Value?.ToString() ?? "";
                string srcReg = instr.Operands[1].Value?.ToString() ?? "";

                for (int j = i + 1; j < instructions.Count; j++)
                {
                    var next = instructions[j];
                    if (next.Label != null) break;
                    if (next.Opcode == OpCode.RET || next.Opcode == OpCode.HALT) break;

                    bool destModified = false;
                    foreach (var op in next.Operands)
                    {
                        if (op.Type == OperandType.REGISTER && op.Value?.ToString() == destReg)
                        {
                            if (next.Opcode == OpCode.MOVE && op == next.Operands[0])
                            {
                                destModified = true;
                                break;
                            }
                            // Preserve original value type: if source operand was int (register number),
                            // set destination to int too (not string) to avoid downstream casts failing.
                            if (instr.Operands[1].Value is int srcInt)
                                op.Value = srcInt;
                            else
                                op.Value = instr.Operands[1].Value;
                            changed = true;
                        }
                    }
                    if (destModified) break;
                }
            }

            return changed;
        }
    }

    /// <summary>
    /// 窥孔优化 - 多种指令模式优化
    /// </summary>
    public class PeepholeOptimizationPass : IOptimizationPass
    {
        public string Name => "窥孔优化";
        public string Description => "多种指令模式优化";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnablePeepholeOptimization) return false;

            bool changed = false;
            var instructions = program.Instructions;

            for (int i = 0; i < instructions.Count - 1; i++)
            {
                var instr = instructions[i];
                var next = instructions[i + 1];

                if (next.Label != null) continue;

                // MOVE Rx, Rx -> NOP
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.REGISTER &&
                    instr.Operands[0].Value.ToString() == instr.Operands[1].Value.ToString())
                {
                    instructions[i] = new Instruction(OpCode.NOP, [], 0, "; self-move eliminated");
                    changed = true;
                    continue;
                }

                // LOAD Rx, #0; ADD Rx, #imm -> LOAD Rx, #imm
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE &&
                    GetIntValue(instr.Operands[1].Value) == 0 &&
                    next.Opcode == OpCode.ADD && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.IMMEDIATE &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    int val = GetIntValue(next.Operands[1].Value);
                    instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val);
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    continue;
                }

                // LOAD Rx, #imm; ADD Rx, #0 -> LOAD Rx, #imm
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE &&
                    next.Opcode == OpCode.ADD && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.IMMEDIATE &&
                    GetIntValue(next.Operands[1].Value) == 0 &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    continue;
                }

                // PUSH Rx; POP Rx -> NOP; NOP
                if (instr.Opcode == OpCode.PUSH && instr.Operands.Count > 0 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    next.Opcode == OpCode.POP && next.Operands.Count > 0 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString())
                {
                    instructions[i] = new Instruction(OpCode.NOP, [], 0, "; push/pop eliminated");
                    instructions[i + 1] = new Instruction(OpCode.NOP, [], 0, "; push/pop eliminated");
                    changed = true;
                    continue;
                }

                // LOAD Rx, [Ry]; STORE Rx, [Ry] -> LOAD Rx, [Ry]; NOP
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.MEMORY &&
                    next.Opcode == OpCode.MOVE && next.Operands.Count >= 2 &&
                    next.Operands[0].Type == OperandType.REGISTER &&
                    next.Operands[1].Type == OperandType.MEMORY &&
                    instr.Operands[0].Value.ToString() == next.Operands[0].Value.ToString() &&
                    instr.Operands[1].Value.ToString() == next.Operands[1].Value.ToString())
                {
                    instructions[i + 1] = new Instruction(OpCode.NOP, [], 0, "; redundant store eliminated");
                    changed = true;
                    continue;
                }

                // Constant folding for bitwise ops
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count >= 2 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE)
                {
                    int val1 = GetIntValue(instr.Operands[1].Value);
                    string reg = instr.Operands[0].Value?.ToString() ?? "";

                    if (next.Opcode == OpCode.AND && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value?.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 & val2);
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        continue;
                    }

                    if (next.Opcode == OpCode.OR && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 | val2);
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        continue;
                    }

                    if (next.Opcode == OpCode.XOR && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 ^ val2);
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        continue;
                    }

                    if (next.Opcode == OpCode.SHL && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 << val2);
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        continue;
                    }

                    if (next.Opcode == OpCode.SHR && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 >> val2);
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        continue;
                    }

                    if (next.Opcode == OpCode.DIV && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        if (val2 != 0)
                        {
                            instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 / val2);
                            instructions.RemoveAt(i + 1);
                            changed = true;
                            continue;
                        }
                    }

                    if (next.Opcode == OpCode.MOD && next.Operands.Count >= 2 &&
                        next.Operands[0].Type == OperandType.REGISTER &&
                        next.Operands[1].Type == OperandType.IMMEDIATE &&
                        next.Operands[0].Value.ToString() == reg)
                    {
                        int val2 = GetIntValue(next.Operands[1].Value);
                        if (val2 != 0)
                        {
                            instr.Operands[1] = new Operand(OperandType.IMMEDIATE, val1 % val2);
                            instructions.RemoveAt(i + 1);
                            changed = true;
                            continue;
                        }
                    }
                }
            }

            return changed;
        }

        private int GetIntValue(object value)
        {
            if (value is int i) return i;
            if (value is string s && int.TryParse(s, out int parsed)) return parsed;
            return 0;
        }
    }

    /// <summary>
    /// 跳转链优化 - JMP L1; L1: JMP L2 -> JMP L2
    /// </summary>
    public class JumpChainingPass : IOptimizationPass
    {
        public string Name => "跳转链优化";
        public string Description => "优化连续跳转: JMP L1; L1: JMP L2 -> JMP L2";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableJumpChaining) return false;

            bool changed = false;

            for (int i = 0; i < program.Instructions.Count - 1; i++)
            {
                var instr = program.Instructions[i];
                var next = program.Instructions[i + 1];

                // 模式: JMP L1; L1: JMP L2 -> JMP L2
                if (instr.Opcode == OpCode.JMP && instr.Operands.Count > 0 &&
                    instr.Operands[0].Type == OperandType.LABEL &&
                    next.Label != null && next.Label == instr.Operands[0].Value.ToString() &&
                    next.Opcode == OpCode.JMP && next.Operands.Count > 0 &&
                    next.Operands[0].Type == OperandType.LABEL)
                {
                    instr.Operands[0] = new Operand(OperandType.LABEL, next.Operands[0].Value);
                    changed = true;
                }

                // 模式: JMP L1; L1: (空或NOP) JMP L2 -> JMP L2
                if (instr.Opcode == OpCode.JMP && instr.Operands.Count > 0 &&
                    instr.Operands[0].Type == OperandType.LABEL &&
                    i + 2 < program.Instructions.Count)
                {
                    var targetIdx = program.Instructions.IndexOf(program.Instructions[i + 2]);
                    var labelName = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(labelName, out int labelAddr) && labelAddr == i + 2)
                    {
                        var targetInstr = program.Instructions[i + 2];
                        if (targetInstr.Opcode == OpCode.JMP && targetInstr.Operands.Count > 0 &&
                            targetInstr.Operands[0].Type == OperandType.LABEL)
                        {
                            instr.Operands[0] = new Operand(OperandType.LABEL, targetInstr.Operands[0].Value);
                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }
    }

    /// <summary>
    /// 死代码消除 - 删除不可达代码
    /// </summary>
    public class DeadCodeEliminationPass : IOptimizationPass
    {
        public string Name => "死代码消除";
        public string Description => "删除不可达代码 (扫描 + CFG分析)";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableDeadCodeElimination) return false;
            if (program.Instructions.Count == 0) return false;

            var toRemove = new HashSet<int>();

            // Phase 1: 安全扫描 — 仅删除无条件 JMP 后的连续无标签代码
            // (不处理 HALT/RET/IRET，因为其后通常是链接的库函数代码)
            int reachable = 0;
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                if (reachable > 0)
                {
                    if (!string.IsNullOrEmpty(instr.Label))
                    { reachable = 0; continue; }
                    toRemove.Add(i);
                }
                // 仅无条件 JMP 视为终止 (HALT/RET/IRET 后可能有库函数)
                if (instr.Opcode == OpCode.JMP)
                    reachable = 1;
            }

            // Phase 2: CFG 分析 (检测复杂控制流中的死代码)
            if (options.OptimizationLevel >= 2)
            {
                try
                {
                    var cfg = new ControlFlowGraph(program);
                    foreach (var block in cfg.FindUnreachableBlocks())
                    {
                        if (block.Id == 0 || block.IsEntry) continue;
                        for (int i = block.StartIndex; i <= block.EndIndex; i++)
                            toRemove.Add(i);
                    }
                }
                catch { /* CFG 分析失败时回退到简单扫描 */ }
            }

            if (toRemove.Count == 0) return false;

            // 从后向前删除
            var sorted = new List<int>(toRemove);
            sorted.Sort((a, b) => b.CompareTo(a));
            foreach (var idx in sorted)
                program.Instructions.RemoveAt(idx);

            return true;
        }
    }
}
