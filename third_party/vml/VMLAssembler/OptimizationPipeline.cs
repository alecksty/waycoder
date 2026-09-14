using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>
    /// 优化选项
    /// </summary>
    public class OptimizationOptions
    {
        public int OptimizationLevel { get; set; } = 0; // 0=无, 1=基本, 2=激进
        public bool EnableDeadCodeElimination { get; set; } = true;
        public bool EnableConstantFolding { get; set; } = true;
        public bool EnableCopyPropagation { get; set; } = false;
        public bool EnableDeadStoreElimination { get; set; } = true;
        public bool EnableJumpChaining { get; set; } = true;
        public bool EnableNopElimination { get; set; } = true;
        public bool EnablePeepholeOptimization { get; set; } = true;
        public bool EnableLoopOptimization { get; set; } = false;
        public bool EnableDataFlowAnalysis { get; set; } = false;
    }

    /// <summary>
    /// 优化Pass接口
    /// </summary>
    public interface IOptimizationPass
    {
        string Name { get; }
        string Description { get; }
        bool Run(VmlProgram program, OptimizationOptions options);
    }

    /// <summary>
    /// 优化流水线
    /// </summary>
    public class OptimizationPipeline
    {
        private readonly List<IOptimizationPass> _passes = new();

        public void AddPass(IOptimizationPass pass)
        {
            _passes.Add(pass);
        }

        public VmlProgram Run(VmlProgram program, OptimizationOptions options)
        {
            foreach (var pass in _passes)
            {
                bool changed = pass.Run(program, options);
                if (changed)
                {
                    // 重新计算标签地址
                    RebuildLabelAddresses(program);
                }
            }

            return program;
        }

        /// <summary>
        /// 重新计算标签地址（当指令被删除或插入后需要调用）
        /// </summary>
        private void RebuildLabelAddresses(VmlProgram program)
        {
            // 更新指令地址，保留已有的别名标签 (库链接器创建的 lib_xxx 等)
            var oldLabels = new Dictionary<string, int>(program.Labels);
            var newLabels = new Dictionary<string, int>();
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                instr.Address = i;
                if (!string.IsNullOrEmpty(instr.Label))
                    newLabels[instr.Label] = i;
            }

            // 合并: 旧标签中指向已删除指令的 → 移除; 保留旧有且不在新标签中的别名
            foreach (var kv in oldLabels)
            {
                if (newLabels.ContainsKey(kv.Key)) continue;
                if (kv.Value < program.Instructions.Count) newLabels[kv.Key] = kv.Value;
            }

            program.Labels = newLabels;
        }

        /// <summary>
        /// 创建默认优化流水线
        /// </summary>
        public static OptimizationPipeline CreateDefault()
        {
            var pipeline = new OptimizationPipeline();
            pipeline.AddPass(new NopEliminationPass());
            pipeline.AddPass(new ConstantFoldingPass());
            pipeline.AddPass(new PeepholeOptimizationPass());
            pipeline.AddPass(new CopyPropagationPass());
            pipeline.AddPass(new DeadStoreEliminationPass());
            pipeline.AddPass(new JumpChainingPass());
            pipeline.AddPass(new DeadCodeEliminationPass());
            pipeline.AddPass(new LoopOptimizationPass());
            pipeline.AddPass(new DataFlowAnalysisPass());
            pipeline.AddPass(new NopEliminationPass()); // 第二轮清理
            return pipeline;
        }
    }
}
