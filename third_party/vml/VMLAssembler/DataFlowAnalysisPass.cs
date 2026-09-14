using System.Collections.Generic;
using System.Linq;

namespace VMLAssembler
{
    /// <summary>
    /// 数据流分析Pass - 活跃变量分析和到达定义分析
    /// </summary>
    public class DataFlowAnalysisPass : IOptimizationPass
    {
        public string Name => "数据流分析";
        public string Description => "活跃变量分析和到达定义分析";

        public bool Run(VmlProgram program, OptimizationOptions options)
        {
            if (!options.EnableDataFlowAnalysis) return false;
            
            bool changed = false;
            
            // 活跃变量分析
            var liveAnalysis = PerformLiveVariableAnalysis(program);
            
            // 到达定义分析
            var reachingDefs = PerformReachingDefinitionAnalysis(program);
            
            // 基于分析结果进行优化
            changed |= OptimizeBasedOnLiveAnalysis(program, liveAnalysis);
            changed |= OptimizeBasedOnReachingDefs(program, reachingDefs);
            
            return changed;
        }
        
        /// <summary>
        /// 活跃变量分析结果
        /// </summary>
        private class LiveVariableAnalysis
        {
            public Dictionary<int, HashSet<string>> LiveIn { get; set; } = new();
            public Dictionary<int, HashSet<string>> LiveOut { get; set; } = new();
            public Dictionary<int, HashSet<string>> Gen { get; set; } = new();
            public Dictionary<int, HashSet<string>> Kill { get; set; } = new();
        }
        
        /// <summary>
        /// 到达定义分析结果
        /// </summary>
        private class ReachingDefinitionAnalysis
        {
            public Dictionary<int, HashSet<int>> In { get; set; } = new();
            public Dictionary<int, HashSet<int>> Out { get; set; } = new();
            public Dictionary<int, HashSet<int>> Gen { get; set; } = new();
            public Dictionary<int, HashSet<int>> Kill { get; set; } = new();
        }
        
        /// <summary>
        /// 执行活跃变量分析
        /// </summary>
        private LiveVariableAnalysis PerformLiveVariableAnalysis(VmlProgram program)
        {
            var analysis = new LiveVariableAnalysis();
            var instructions = program.Instructions;
            
            // 初始化
            for (int i = 0; i < instructions.Count; i++)
            {
                analysis.LiveIn[i] = new HashSet<string>();
                analysis.LiveOut[i] = new HashSet<string>();
                analysis.Gen[i] = new HashSet<string>();
                analysis.Kill[i] = new HashSet<string>();
                
                var instr = instructions[i];
                
                // 计算Gen和Kill集合
                ComputeGenKill(instr, analysis.Gen[i], analysis.Kill[i]);
            }
            
            // 迭代求解数据流方程
            bool changed;
            do
            {
                changed = false;
                
                // 从后向前遍历
                for (int i = instructions.Count - 1; i >= 0; i--)
                {
                    var oldLiveOut = new HashSet<string>(analysis.LiveOut[i]);
                    var oldLiveIn = new HashSet<string>(analysis.LiveIn[i]);
                    
                    // LiveOut[i] = ∪ LiveIn[s] for s in successors(i)
                    analysis.LiveOut[i].Clear();
                    foreach (var succ in GetSuccessors(program, i))
                    {
                        analysis.LiveOut[i].UnionWith(analysis.LiveIn[succ]);
                    }
                    
                    // LiveIn[i] = Gen[i] ∪ (LiveOut[i] - Kill[i])
                    analysis.LiveIn[i].Clear();
                    analysis.LiveIn[i].UnionWith(analysis.Gen[i]);
                    var temp = new HashSet<string>(analysis.LiveOut[i]);
                    temp.ExceptWith(analysis.Kill[i]);
                    analysis.LiveIn[i].UnionWith(temp);
                    
                    // 检查是否变化
                    if (!analysis.LiveOut[i].SetEquals(oldLiveOut) || 
                        !analysis.LiveIn[i].SetEquals(oldLiveIn))
                    {
                        changed = true;
                    }
                }
            } while (changed);
            
            return analysis;
        }
        
        /// <summary>
        /// 执行到达定义分析
        /// </summary>
        private ReachingDefinitionAnalysis PerformReachingDefinitionAnalysis(VmlProgram program)
        {
            var analysis = new ReachingDefinitionAnalysis();
            var instructions = program.Instructions;
            
            // 初始化
            for (int i = 0; i < instructions.Count; i++)
            {
                analysis.In[i] = new HashSet<int>();
                analysis.Out[i] = new HashSet<int>();
                analysis.Gen[i] = new HashSet<int>();
                analysis.Kill[i] = new HashSet<int>();
                
                var instr = instructions[i];
                
                // 计算Gen和Kill集合
                ComputeReachingDefGenKill(program, i, instr, analysis.Gen[i], analysis.Kill[i]);
            }
            
            // 迭代求解数据流方程
            bool changed;
            do
            {
                changed = false;
                
                // 从前向后遍历
                for (int i = 0; i < instructions.Count; i++)
                {
                    var oldIn = new HashSet<int>(analysis.In[i]);
                    var oldOut = new HashSet<int>(analysis.Out[i]);
                    
                    // In[i] = ∪ Out[p] for p in predecessors(i)
                    analysis.In[i].Clear();
                    foreach (var pred in GetPredecessors(program, i))
                    {
                        analysis.In[i].UnionWith(analysis.Out[pred]);
                    }
                    
                    // Out[i] = Gen[i] ∪ (In[i] - Kill[i])
                    analysis.Out[i].Clear();
                    analysis.Out[i].UnionWith(analysis.Gen[i]);
                    var temp = new HashSet<int>(analysis.In[i]);
                    temp.ExceptWith(analysis.Kill[i]);
                    analysis.Out[i].UnionWith(temp);
                    
                    // 检查是否变化
                    if (!analysis.In[i].SetEquals(oldIn) || 
                        !analysis.Out[i].SetEquals(oldOut))
                    {
                        changed = true;
                    }
                }
            } while (changed);
            
            return analysis;
        }
        
        /// <summary>
        /// 计算Gen和Kill集合（活跃变量分析）
        /// </summary>
        private void ComputeGenKill(Instruction instr, HashSet<string> gen, HashSet<string> kill)
        {
            // 获取指令使用的变量（读取）
            var used = GetUsedVariables(instr);
            gen.UnionWith(used);
            
            // 获取指令定义的变量（写入）
            var defined = GetDefinedVariables(instr);
            kill.UnionWith(defined);
        }
        
        /// <summary>
        /// 计算Gen和Kill集合（到达定义分析）
        /// </summary>
        private void ComputeReachingDefGenKill(VmlProgram program, int index, Instruction instr, HashSet<int> gen, HashSet<int> kill)
        {
            // 如果指令定义了变量，则生成一个定义
            var defined = GetDefinedVariables(instr);
            if (defined.Count > 0)
            {
                gen.Add(index);
                
                // 杀死同一变量的其他定义
                for (int i = 0; i < program.Instructions.Count; i++)
                {
                    if (i != index)
                    {
                        var otherDefined = GetDefinedVariables(program.Instructions[i]);
                        if (defined.Any(d => otherDefined.Contains(d)))
                        {
                            kill.Add(i);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取指令使用的变量
        /// </summary>
        private HashSet<string> GetUsedVariables(Instruction instr)
        {
            var used = new HashSet<string>();
            
            foreach (var op in instr.Operands)
            {
                if (op.Type == OperandType.REGISTER)
                {
                    // 对于STORE指令，第一个操作数是值，第二个是地址
                    if (instr.Opcode == OpCode.MOVE || instr.Opcode == OpCode.MOVEF)
                    {
                        if (op == instr.Operands[0]) // 值
                        {
                            used.Add(op.Value?.ToString() ?? "");
                        }
                    }
                    else
                    {
                        used.Add(op.Value?.ToString() ?? "");
                    }
                }
                else if (op.Type == OperandType.MEMORY)
                {
                    used.Add(op.Value?.ToString() ?? "");
                }
                else if (op.Type == OperandType.LABEL)
                {
                    // 标签引用
                }
            }
            
            return used;
        }
        
        /// <summary>
        /// 获取指令定义的变量
        /// </summary>
        private HashSet<string> GetDefinedVariables(Instruction instr)
        {
            var defined = new HashSet<string>();
            
            if (instr.Opcode == OpCode.MOVE || instr.Opcode == OpCode.MOVEF)
            {
                if (instr.Operands.Count > 1 && instr.Operands[1].Type == OperandType.MEMORY)
                {
                    defined.Add(instr.Operands[1].Value?.ToString() ?? "");
                }
            }
            else if (instr.Opcode == OpCode.MOVE || instr.Opcode == OpCode.MOVEF ||
                     instr.Opcode == OpCode.ADD || instr.Opcode == OpCode.SUB ||
                     instr.Opcode == OpCode.MUL || instr.Opcode == OpCode.DIV ||
                     instr.Opcode == OpCode.MOD || instr.Opcode == OpCode.AND ||
                     instr.Opcode == OpCode.OR || instr.Opcode == OpCode.XOR ||
                     instr.Opcode == OpCode.SHL || instr.Opcode == OpCode.SHR ||
                     instr.Opcode == OpCode.MOVE || instr.Opcode == OpCode.NEG ||
                     instr.Opcode == OpCode.NOT || instr.Opcode == OpCode.INC ||
                     instr.Opcode == OpCode.DEC)
            {
                if (instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.REGISTER)
                {
                    defined.Add(instr.Operands[0].Value?.ToString() ?? "");
                }
            }
            
            return defined;
        }
        
        /// <summary>
        /// 获取指令的后继
        /// </summary>
        private List<int> GetSuccessors(VmlProgram program, int index)
        {
            var successors = new List<int>();
            var instructions = program.Instructions;
            
            if (index >= instructions.Count - 1) return successors;
            
            var instr = instructions[index];
            
            // 无条件跳转
            if (instr.Opcode == OpCode.JMP)
            {
                if (instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.LABEL)
                {
                    string label = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(label, out int target))
                    {
                        successors.Add(target);
                    }
                }
                return successors;
            }
            
            // 条件跳转
            if (instr.Opcode == OpCode.JE || instr.Opcode == OpCode.JNE ||
                instr.Opcode == OpCode.JG || instr.Opcode == OpCode.JGE ||
                instr.Opcode == OpCode.JL || instr.Opcode == OpCode.JLE)
            {
                // 跳转目标
                if (instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.LABEL)
                {
                    string label = instr.Operands[0].Value?.ToString() ?? "";
                    if (program.Labels.TryGetValue(label, out int target))
                    {
                        successors.Add(target);
                    }
                }
                // 顺序执行（条件不满足时）
                successors.Add(index + 1);
                return successors;
            }
            
            // 终止指令
            if (instr.Opcode == OpCode.HALT || instr.Opcode == OpCode.RET)
            {
                return successors;
            }
            
            // 顺序执行
            successors.Add(index + 1);
            return successors;
        }
        
        /// <summary>
        /// 获取指令的前驱
        /// </summary>
        private List<int> GetPredecessors(VmlProgram program, int index)
        {
            var predecessors = new List<int>();
            
            // 简单实现：检查所有可能跳转到index的指令
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                if (i == index) continue;
                
                var instr = program.Instructions[i];
                
                // 检查是否跳转到index
                if (instr.Opcode == OpCode.JMP || instr.Opcode == OpCode.JE ||
                    instr.Opcode == OpCode.JNE || instr.Opcode == OpCode.JG ||
                    instr.Opcode == OpCode.JGE || instr.Opcode == OpCode.JL ||
                    instr.Opcode == OpCode.JLE)
                {
                    if (instr.Operands.Count > 0 && instr.Operands[0].Type == OperandType.LABEL)
                    {
                        string label = instr.Operands[0].Value?.ToString() ?? "";
                        if (program.Labels.TryGetValue(label, out int target) && target == index)
                        {
                            predecessors.Add(i);
                        }
                    }
                }
                
                // 顺序执行
                if (i == index - 1 && 
                    instr.Opcode != OpCode.JMP && 
                    instr.Opcode != OpCode.HALT && 
                    instr.Opcode != OpCode.RET)
                {
                    predecessors.Add(i);
                }
            }
            
            return predecessors;
        }
        
        /// <summary>
        /// 基于活跃变量分析进行优化
        /// </summary>
        private bool OptimizeBasedOnLiveAnalysis(VmlProgram program, LiveVariableAnalysis analysis)
        {
            bool changed = false;
            var instructions = program.Instructions;
            
            // 删除死存储（写入后从不读取的变量）
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                var defined = GetDefinedVariables(instr);
                
                if (defined.Count > 0)
                {
                    // 检查定义的变量是否在LiveOut集合中
                    bool isDead = true;
                    foreach (var varName in defined)
                    {
                        if (analysis.LiveOut[i].Contains(varName))
                        {
                            isDead = false;
                            break;
                        }
                    }
                    
                    if (isDead)
                    {
                        // 替换为NOP
                        instructions[i] = new Instruction(OpCode.NOP, new List<Operand>(), 0, "; dead store eliminated by dataflow analysis");
                        changed = true;
                    }
                }
            }
            
            return changed;
        }
        
        /// <summary>
        /// 基于到达定义分析进行优化
        /// </summary>
        private bool OptimizeBasedOnReachingDefs(VmlProgram program, ReachingDefinitionAnalysis analysis)
        {
            bool changed = false;
            var instructions = program.Instructions;
            
            // 常量传播：如果变量只有一个定义，且该定义是常量，则传播该常量
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                var used = GetUsedVariables(instr);
                
                foreach (var varName in used)
                {
                    // 查找到达该变量的定义
                    var reachingDefs = new HashSet<int>();
                    foreach (var defIndex in analysis.In[i])
                    {
                        var defInstr = instructions[defIndex];
                        var defVars = GetDefinedVariables(defInstr);
                        if (defVars.Contains(varName))
                        {
                            reachingDefs.Add(defIndex);
                        }
                    }
                    
                    // 如果只有一个定义，且是常量加载
                    if (reachingDefs.Count == 1)
                    {
                        int defIndex = reachingDefs.First();
                        var defInstr = instructions[defIndex];
                        
                        if (defInstr.Opcode == OpCode.MOVE && defInstr.Operands.Count >= 2 &&
                            defInstr.Operands[1].Type == OperandType.IMMEDIATE)
                        {
                            // 传播常量
                            object constValue = defInstr.Operands[1].Value;
                            
                            // 替换变量引用为常量
                            for (int j = 0; j < instr.Operands.Count; j++)
                            {
                                var op = instr.Operands[j];
                                if (op.Type == OperandType.REGISTER && op.Value.ToString() == varName)
                                {
                                    instr.Operands[j] = new Operand(OperandType.IMMEDIATE, constValue);
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
            
            return changed;
        }
    }
}