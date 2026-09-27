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
        /// <summary>
        /// 冗余跳转消除（`O3`）—— 见 <c>RedundantJumpEliminationPass</c>。
        /// 默认 `false`：与 `DeadCodeElimination` 一样，**由 `OptimizationPolicy` 按级别显式决定**，
        /// 不跟着默认值走（默认值是这个类自己的历史，不是我们的策略）。
        /// </summary>
        public bool EnableRedundantJumpElimination { get; set; } = false;
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
                // ⚠ **必须在 pass 跑之前记下「指令对象 → 它当时的地址」**：pass 只会
                //   `Instructions.RemoveAt(...)`，删完**地址就全变了**，而 `Labels` 里存的
                //   还是旧地址。要把标签正确搬到新编号上，唯一可靠的锚是**指令对象本身**
                //   （引用不变）—— 拿它反查"这条指令原来是第几条"。
                //
                //   为什么不让 pass 自己报"删了哪些下标"：删除散在 10 个 pass 里
                //   （Nop / ConstantFolding / Peephole / CopyProp / DeadStore / JumpChaining /
                //   DeadCode …），漏一个就又是"标签悄悄指错"，而且是**跑起来才炸**的那一类。
                var indexBefore = new Dictionary<Instruction, int>(program.Instructions.Count);
                for (int i = 0; i < program.Instructions.Count; i++)
                    indexBefore[program.Instructions[i]] = i;

                bool changed = pass.Run(program, options);
                if (changed)
                {
                    // 重新计算标签地址
                    RebuildLabelAddresses(program, indexBefore);
                }
            }

            return program;
        }

        /// <summary>
        /// 指令被增删之后，把 `program.Labels` 的地址搬到新编号上。
        ///
        /// <para>
        /// <b>做法：按「指令对象」重映射。</b><paramref name="indexBefore"/> 是本次 pass 之前
        /// 的「指令 → 地址」，拿它与当前下标一比就得到「旧地址 → 新地址」表，然后**逐条标签**搬。
        /// 三个细节都是有意的：
        /// </para>
        /// <list type="bullet">
        /// <item><b>多个标签指向同一地址</b>（链接器造的别名，如 `lib_io_puts` / `puts`）自动正确 ——
        ///   两个名字查的是同一张表，不存在"只能留一个"的问题。（曾想过的"把标签写回
        ///   `Instruction.Label`"就死在这里：一个字段放不下两个名字，而链接后的程序里
        ///   20392 个标签**全都只在字典里**、指令上一个都没有。）</item>
        /// <item>标签指向的指令**被删了** ⇒ 该标签随之消失（它指向的代码已经不在了）。</item>
        /// <item>标签地址**越界**（历史遗留） ⇒ 一并消失。</item>
        /// </list>
        ///
        /// <para>
        /// ⚠ <b>旧实现是把标签列表"重算"一遍</b>：从 `Instruction.Label` 收集 + 保留
        /// `kv.Value &lt; Instructions.Count` 的旧条目。而链接后的程序里 `Instruction.Label`
        /// **恒为空**（实测 69637 条指令 / 带 Label 的 = 0），于是只剩后半个条件 ——
        /// 而删除之后 `Instructions.Count` 急剧变小，**几乎每个标签都"越界"被丢掉**。
        /// 实测 hello world：删除后只剩 28 条指令，`lib_io_puts` 的地址 22440 ≥ 28 ⇒ 标签没了
        /// ⇒ 一跑就崩在「未找到标签: lib_io_puts」。这是「删得越多、标签错得越离谱」的地基缺陷，
        /// 与 `DeadCodeEliminationPass` Phase 1 / `ControlFlowGraph` 那两处"从 `Instruction.Label`
        /// 读标签"是同一类错误的第三种形态。
        /// </para>
        /// </summary>
        private void RebuildLabelAddresses(VmlProgram program, Dictionary<Instruction, int> indexBefore)
        {
            // 旧地址 → 新地址（只包含活下来的指令）
            var remap = new Dictionary<int, int>();
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                instr.Address = i;
                if (indexBefore.TryGetValue(instr, out var oldIndex))
                    remap[oldIndex] = i;
            }

            var newLabels = new Dictionary<string, int>();
            foreach (var kv in program.Labels)
            {
                if (remap.TryGetValue(kv.Value, out var newIndex))
                    newLabels[kv.Key] = newIndex;
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
            pipeline.AddPass(new RedundantJumpEliminationPass());   // O3：删"跳到自己下一条"的 jmp
            pipeline.AddPass(new LoopOptimizationPass());
            pipeline.AddPass(new DataFlowAnalysisPass());
            pipeline.AddPass(new FinalCleanupPass());               // O3：数据段 + `.linked`（放最后）
            pipeline.AddPass(new NopEliminationPass()); // 第二轮清理
            return pipeline;
        }
    }
}
