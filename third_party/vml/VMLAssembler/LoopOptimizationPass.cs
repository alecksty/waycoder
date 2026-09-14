using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>
    /// 循环优化Pass - 实现循环展开和强度削弱
    /// </summary>
    public class LoopOptimizationPass : IOptimizationPass
    {
        public string Name => "循环优化";
        public string Description => "循环展开和强度削弱优化";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableLoopOptimization) return false;
            
            bool changed = false;
            
            // 查找循环模式
            var loops = FindLoops(program);
            
            foreach (var loop in loops)
            {
                // 尝试循环展开
                if (TryUnrollLoop(program, loop, options))
                {
                    changed = true;
                }
                
                // 尝试强度削弱
                if (TryStrengthReduction(program, loop, options))
                {
                    changed = true;
                }
            }
            
            return changed;
        }
        
        /// <summary>
        /// 循环结构
        /// </summary>
        private class LoopInfo
        {
            public string EntryLabel { get; set; } = "";
            public string? ExitLabel { get; set; }
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public int IterationCount { get; set; } = -1; // -1表示未知
            public List<string> InductionVariables { get; set; } = new();
        }
        
        /// <summary>
        /// 查找程序中的循环
        /// </summary>
        private List<LoopInfo> FindLoops(VmlProgram program)
        {
            var loops = new List<LoopInfo>();
            var instructions = program.Instructions;
            
            // 构建控制流图
            var cfg = BuildControlFlowGraph(program);
            
            // 查找循环: JMP到前面的标签
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                
                // 查找向后跳转
                if (instr.Opcode == OpCode.JMP && instr.Operands.Count > 0 &&
                    instr.Operands[0].Type == OperandType.LABEL)
                {
                    string targetLabel = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(targetLabel, out int targetIndex) && targetIndex < i)
                    {
                        // 找到循环
                        var loop = new LoopInfo
                        {
                            EntryLabel = targetLabel,
                            ExitLabel = null, // 需要分析退出条件
                            StartIndex = targetIndex,
                            EndIndex = i
                        };
                        
                        // 分析循环体
                        AnalyzeLoopBody(program, loop);
                        loops.Add(loop);
                    }
                }
            }
            
            return loops;
        }
        
        /// <summary>
        /// 构建控制流图
        /// </summary>
        private Dictionary<int, List<int>> BuildControlFlowGraph(VmlProgram program)
        {
            var cfg = new Dictionary<int, List<int>>();
            var instructions = program.Instructions;
            
            for (int i = 0; i < instructions.Count; i++)
            {
                cfg[i] = new List<int>();
                
                var instr = instructions[i];
                
                // 顺序执行下一条指令（除非是跳转或终止）
                if (instr.Opcode != OpCode.JMP && instr.Opcode != OpCode.JE && 
                    instr.Opcode != OpCode.JNE && instr.Opcode != OpCode.JG &&
                    instr.Opcode != OpCode.JGE && instr.Opcode != OpCode.JL &&
                    instr.Opcode != OpCode.JLE && instr.Opcode != OpCode.HALT &&
                    instr.Opcode != OpCode.RET && i + 1 < instructions.Count)
                {
                    cfg[i].Add(i + 1);
                }
                
                // 跳转目标
                if (instr.Opcode == OpCode.JMP && instr.Operands.Count > 0 &&
                    instr.Operands[0].Type == OperandType.LABEL)
                {
                    string label = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(label, out int target))
                    {
                        cfg[i].Add(target);
                    }
                }
                else if ((instr.Opcode == OpCode.JE || instr.Opcode == OpCode.JNE ||
                         instr.Opcode == OpCode.JG || instr.Opcode == OpCode.JGE ||
                         instr.Opcode == OpCode.JL || instr.Opcode == OpCode.JLE) &&
                         instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.LABEL)
                {
                    string label = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(label, out int target))
                    {
                        cfg[i].Add(target);
                    }
                }
            }
            
            return cfg;
        }
        
        /// <summary>
        /// 分析循环体
        /// </summary>
        private void AnalyzeLoopBody(VmlProgram program, LoopInfo loop)
        {
            var instructions = program.Instructions;
            
            // 查找归纳变量
            for (int i = loop.StartIndex; i <= loop.EndIndex; i++)
            {
                var instr = instructions[i];
                
                // 查找循环计数器模式: i = i + 1 或 i = i - 1
                if (instr.Opcode == OpCode.ADD && instr.Operands.Count >= 3 &&
                    instr.Operands[0].Type == OperandType.REGISTER &&
                    instr.Operands[1].Type == OperandType.REGISTER &&
                    instr.Operands[2].Type == OperandType.IMMEDIATE &&
                    instr.Operands[0].Value?.ToString() == instr.Operands[1].Value?.ToString())
                {
                    string reg = instr.Operands[0].Value?.ToString() ?? "";
                    int delta = GetIntValue(instr.Operands[2].Value);
                    
                    if (delta == 1 || delta == -1)
                    {
                        loop.InductionVariables.Add(reg);
                    }
                }
                
                // 查找循环退出条件
                if ((instr.Opcode == OpCode.JE || instr.Opcode == OpCode.JNE ||
                     instr.Opcode == OpCode.JG || instr.Opcode == OpCode.JGE ||
                     instr.Opcode == OpCode.JL || instr.Opcode == OpCode.JLE) &&
                     instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.LABEL)
                {
                    string exitLabel = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(exitLabel, out int exitIndex) && exitIndex > loop.EndIndex)
                    {
                        loop.ExitLabel = exitLabel;
                    }
                }
            }
        }
        
        /// <summary>
        /// 尝试循环展开
        /// </summary>
        private bool TryUnrollLoop(VmlProgram program, LoopInfo loop, OptimizationOptions options)
        {
            // 只展开小循环（迭代次数已知且较少）
            if (loop.IterationCount <= 0 || loop.IterationCount > 8) return false;
            
            var instructions = program.Instructions;
            var loopBody = new List<Instruction>();
            
            // 提取循环体
            for (int i = loop.StartIndex; i <= loop.EndIndex; i++)
            {
                loopBody.Add(instructions[i]);
            }
            
            // 展开循环
            var unrolledInstructions = new List<Instruction>();
            for (int iter = 0; iter < loop.IterationCount; iter++)
            {
                foreach (var instr in loopBody)
                {
                    // 复制指令，调整标签和寄存器
                    var newInstr = new Instruction(
                        instr.Opcode,
                        new List<Operand>(instr.Operands),
                        instr.Address,
                        instr.Label
                    );
                    
                    // 重命名标签以避免冲突
                    if (!string.IsNullOrEmpty(instr.Label))
                    {
                        newInstr.Label = $"{instr.Label}_unrolled_{iter}";
                    }
                    
                    unrolledInstructions.Add(newInstr);
                }
            }
            
            // 替换原循环
            instructions.RemoveRange(loop.StartIndex, loop.EndIndex - loop.StartIndex + 1);
            instructions.InsertRange(loop.StartIndex, unrolledInstructions);
            
            return true;
        }
        
        /// <summary>
        /// 尝试强度削弱
        /// </summary>
        private bool TryStrengthReduction(VmlProgram program, LoopInfo loop, OptimizationOptions options)
        {
            bool changed = false;
            var instructions = program.Instructions;
            
            // 查找乘法/除法操作，尝试替换为移位或加法
            for (int i = loop.StartIndex; i <= loop.EndIndex; i++)
            {
                var instr = instructions[i];
                
                // MUL R0, #power_of_two -> SHL R0, #log2(value)
                if (instr.Opcode == OpCode.MUL && instr.Operands.Count >= 2 &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE)
                {
                    int value = GetIntValue(instr.Operands[1].Value);
                    if (IsPowerOfTwo(value))
                    {
                        int shift = Log2(value);
                        instr.Opcode = OpCode.SHL;
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, shift);
                        changed = true;
                    }
                }
                
                // v1.66.17: 移除 DIV→SHR 强度削弱 — SHR 仅对无符号除法等价，
                // 对有符号负数结果错误 (例如 -5/4=-1, 但 -5>>2=-2)
                // VML 汇编层无法区分有无符号，故移除此优化
                /*
                // DIV R0, #power_of_two -> SHR R0, #log2(value)
                if (instr.Opcode == OpCode.DIV && instr.Operands.Count >= 2 &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE)
                {
                    int value = GetIntValue(instr.Operands[1].Value);
                    if (IsPowerOfTwo(value))
                    {
                        int shift = Log2(value);
                        instr.Opcode = OpCode.SHR;
                        instr.Operands[1] = new Operand(OperandType.IMMEDIATE, shift);
                        changed = true;
                    }
                }
                */
                
                // MUL R0, #constant -> 替换为移位和加法序列
                if (instr.Opcode == OpCode.MUL && instr.Operands.Count >= 2 &&
                    instr.Operands[1].Type == OperandType.IMMEDIATE)
                {
                    int value = GetIntValue(instr.Operands[1].Value);
                    if (value > 0 && value <= 100)
                    {
                        // 查找乘法前的LOAD指令
                        if (i > 0 && instructions[i - 1].Opcode == OpCode.MOVE &&
                            instructions[i - 1].Operands.Count >= 2 &&
                            instructions[i - 1].Operands[0].Type == OperandType.REGISTER &&
                            instructions[i - 1].Operands[0].Value.ToString() == instr.Operands[0].Value.ToString())
                        {
                            // 替换为移位和加法序列
                            var replacement = GenerateMultiplyByConstant(
                                instr.Operands[0].Value?.ToString() ?? "",
                                value,
                                instructions[i - 1].Operands[1].Value
                            );
                            
                            if (replacement.Count > 0)
                            {
                                instructions.RemoveAt(i);
                                instructions.InsertRange(i, replacement);
                                changed = true;
                                i += replacement.Count - 1;
                            }
                        }
                    }
                }
            }
            
            return changed;
        }
        
        /// <summary>
        /// 生成乘以常数的移位/加法序列
        /// </summary>
        private List<Instruction> GenerateMultiplyByConstant(string? reg, int constant, object baseValue)
        {
            var instructions = new List<Instruction>();
            
            if (string.IsNullOrEmpty(reg))
                return instructions;
            
            if (constant == 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, reg),
                    new Operand(OperandType.IMMEDIATE, 0)
                }, 0));
                return instructions;
            }
            
            if (constant == 1)
            {
                // 乘以1不需要操作
                return instructions;
            }
            
            // 检查是否是2的幂
            if (IsPowerOfTwo(constant))
            {
                int shift = Log2(constant);
                instructions.Add(new Instruction(OpCode.SHL, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, reg),
                    new Operand(OperandType.IMMEDIATE, shift)
                }, 0));
                return instructions;
            }
            
            // 尝试分解为移位和加法
            // 例如: x * 5 = (x << 2) + x
            // 例如: x * 10 = (x << 3) + (x << 1)
            // v1.66.17 修复: 使用 R1 作为暂存寄存器保存原始值，
            // 因为 SHL 会破坏目标寄存器导致后续 ADD 使用错误值

            // 查找最佳分解
            var decomposition = DecomposeConstant(constant);
            if (decomposition.Count > 0)
            {
                // 保存原始值到暂存寄存器: MOVE R1, reg
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, "R1"),
                    new Operand(OperandType.REGISTER, reg)
                }, 0));

                // 第一个移位: SHL reg, #decomposition[0]
                instructions.Add(new Instruction(OpCode.SHL, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, reg),
                    new Operand(OperandType.IMMEDIATE, decomposition[0])
                }, 0));

                // 后续分量: SHL R1, #decomposition[i]; ADD reg, reg, R1
                for (int i = 1; i < decomposition.Count; i++)
                {
                    if (decomposition[i] > 0)
                    {
                        // 移位暂存寄存器以产生对应分量
                        instructions.Add(new Instruction(OpCode.SHL, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, "R1"),
                            new Operand(OperandType.IMMEDIATE, decomposition[i])
                        }, 0));
                    }
                    // 累加到结果: ADD reg, reg, R1
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, reg),
                        new Operand(OperandType.REGISTER, reg),
                        new Operand(OperandType.REGISTER, "R1")
                    }, 0));
                }

                return instructions;
            }
            
            return instructions;
        }
        
        /// <summary>
        /// 分解常数为2的幂的和
        /// </summary>
        private List<int> DecomposeConstant(int constant)
        {
            var decomposition = new List<int>();
            int remaining = constant;
            
            for (int i = 31; i >= 0; i--)
            {
                int power = 1 << i;
                if (remaining >= power)
                {
                    decomposition.Add(i);
                    remaining -= power;
                }
            }
            
            // 如果分解后指令数比原乘法少，则使用分解
            if (decomposition.Count > 0 && decomposition.Count < 3) // 最多3条指令
            {
                return decomposition;
            }
            
            return new List<int>();
        }
        
        private bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }
        
        private int Log2(int n)
        {
            int log = 0;
            while (n > 1)
            {
                n >>= 1;
                log++;
            }
            return log;
        }
        
        private int GetIntValue(object value)
        {
            if (value is int i) return i;
            if (value is string s && int.TryParse(s, out int parsed)) return parsed;
            return 0;
        }
    }
}