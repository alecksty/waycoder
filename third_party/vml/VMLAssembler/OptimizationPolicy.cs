namespace VMLAssembler
{
    /// <summary>
    /// **优化策略的唯一真源** —— 哪个级别开哪些 pass、哪些 pass 还带着已知缺陷不能开。
    ///
    /// <para>
    /// 在这之前，这张"开关表"在本仓有**两份逐字相同的拷贝**：<c>scripts/vmlcli/Program.cs</c>
    /// 与上游 <c>VMLTool/Program.Compile.cs</c>（后者又是两处）。拷贝的代价不是难看，
    /// 而是**改一处忘一处**——本仓为此已经付过多次代价（见 CLAUDE.md 的"平行表漂移"）。
    /// 现在我们的两个消费方（桌面 <c>vmlcli</c> 与手机端 <c>MauiVml</c>）共用这一份；
    /// 上游 CLI 那两处不动（那是上游自己的代码）。
    /// </para>
    ///
    /// <para>
    /// <b>为什么放在 VMLAssembler 而不是 <c>WayCoder/UI/Shared/</c>：</b>
    /// 本类型要返回 <see cref="OptimizationOptions"/>，而主工程 <c>WayCoder.csproj</c>
    /// **没有引用 VML 项目**（那里只用纯数据/协议类型，见 <c>VmlHostRuntime</c> 的依赖）。
    /// 放这儿则 Maui 与 vmlcli **零 csproj 改动**（两边都已有 ProjectReference）。
    /// 而且"哪些 pass 可信"本来就是优化器自己的知识 —— 归它自己最自然。
    /// （<c>third_party/vml</c> 已于 2026-09-17 与上游分家，可直接改，见 <c>FORK.md</c>。）
    /// </para>
    ///
    /// <para>
    /// ⚠ <b>那些 <c>false</c> 是踩过的坑，不是保守</b> —— 注释逐条来自上游
    /// <c>Program.Compile.cs:231-241</c>。谁想"顺手打开"其中一个，请先看 <c>FRONTEND_DEFECTS.md</c>
    /// 与 CLAUDE.md 记的那几起"删错代码/标签丢失"事故。
    /// </para>
    /// </summary>
    public static class OptimizationPolicy
    {
        /// <summary>关闭（等价于从前的行为 —— 手机端接入前的恒定状态）。</summary>
        public const int Off = 0;

        /// <summary>
        /// 基本优化。**这是目前唯一有意义的档**：在现有开关表下，更高级别与它逐字节等效
        /// （<see cref="OptimizationOptions.OptimizationLevel"/> 唯一被读的地方是
        /// <c>DeadCodeEliminationPass</c> 里的 <c>&gt;= 2</c>，而那个 pass 被本表关掉了）
        /// ⇒ 所以设置界面**只列"关闭/基本"两档**，列 O2 就是一个"选了没区别"的假档位。
        /// </summary>
        public const int Basic = 1;

        /// <summary>这个级别要不要跑优化。`0` 之外都跑（关闭时调用方一个字都不做，连流水线都不建）。</summary>
        public static bool IsEnabled(int level) => level >= Basic;

        /// <summary>
        /// 按级别建选项。**这是全仓唯一构造 <see cref="OptimizationOptions"/> 的地方**
        /// （消费方：<c>scripts/vmlcli/Program.cs</c> 与 <c>WayCoder.Maui/Services/MauiVml.cs</c>）。
        /// </summary>
        public static OptimizationOptions Create(int level) => new()
        {
            OptimizationLevel = level,

            // ── 经过验证、可以开的 ──
            EnableNopElimination = level >= 1,      // 删掉无标签的 NOP；语义显然不变

            // **死代码消除（可达性分析）**：`O2` 起。
            //
            // 上游给它的评语是"实验性, 链接库程序误删除代码"，而那句话背后是**三个独立缺陷**，
            // 2026-09-27 全部查明并修掉（判据 `scripts/vml-opt-probe`）：
            //   ① `DeadCodeEliminationPass` 的 Phase 1 拿 `Instruction.Label` 判"有没有标签指向"，
            //      而**链接之后没有任何指令带 Label**（实测 69637 条 / 带 Label 的 = 0）⇒ 判据恒假
            //      ⇒ 遇到第一条 JMP 就把后面**整份程序**删光；
            //   ② `ControlFlowGraph` 建标签表、判块首同样从 `Instruction.Label` 读 ⇒ 空表 ⇒
            //      所有跳转都找不到目标 ⇒ 块间零边 ⇒ 除块 0 外全判"不可达"；
            //   ③ `OptimizationPipeline.RebuildLabelAddresses` 删除之后**从不重映射地址**，
            //      只保留"旧地址没越界"的标签 ⇒ 删得越多、标签丢得越多（`lib_io_puts` 就是这么没的）。
            // 实测效果：46 字节的 hello.c **69637 条 → 28 条指令**（2.0 MB → 约 2 KB），
            // 运行输出与不优化逐字节相同 —— 这才叫"打开优化真的变小"。
            //
            // ⚠ 它的规矩是「**可以保留多，不能多删除**」（用户 2026-09-27 定）：间接跳转、
            //   取函数地址、中断向量表这些"静态看不全"的情形一律**放弃删除**（见
            //   `ControlFlowGraph.HasIndirectJump` / `CollectExtraRoots`），宁可产物大一点。
            EnableDeadCodeElimination = level >= 2,

            // **`O3`（极致）**：在 `O2` 之上再做三件**安全**的事（都不改控制流形状）——
            //   ① 删掉"跳转目标就是下一条"的 `jmp`（`RedundantJumpEliminationPass`）；
            //   ② 死代码消除之后再清一遍数据段（`FinalCleanupPass`）；
            //   ③ 清空 `.linked` 声明，让产物自包含（不清的话重新汇编会把删掉的库再链回来）。
            //
            // ⚠ **上游那五个 pass 不在这里、也不在任何级别**：实测把常量折叠/跳转链/死存储/
            //   复写传播/窥孔在 `O3` 打开后，`hello world` 照样跑对，而**22 门语言的输出
            //   全线出错**（有的只剩一行、有的空白）⇒ 它们的 bug 不止"标签地址不重映射"那一个。
            //   上游注释里"有标签损坏 bug / 有输出损坏 bug"是真的。
            EnableRedundantJumpElimination = level >= 3,

            // ── 以下仍全部因**已知缺陷**关闭（上游注释原样保留）──
            //
            // ⚠ 2026-09-27 **实测过"地基修好后它们能不能用"**：把下面五个在 `O3` 打开后，
            //   `hello world` 照样跑对，而**22 门语言的输出全线出错**（有的只剩一行、有的空白）
            //   ⇒ 它们的 bug **不只是**"标签地址不重映射"那一个，上游的警告是真的。
            //   结论：**恒关**，不随级别开。谁想启用它们，先拿 `scripts/vml-opt-probe` 的第 ④ 条
            //   （跨语言 O0/O2 输出逐字节相同）跑通再说 —— 那条判据就是为这件事立的。
            EnableConstantFolding = false,          // 实验性
            EnableJumpChaining = false,             // 实验性, 有标签损坏 bug
            EnableDeadStoreElimination = false,     // O2 有标签丢失 bug
            EnableCopyPropagation = false,          // O2 有标签丢失 bug
            EnablePeepholeOptimization = false,     // O2+ 实验性, 有输出损坏 bug

            // `EnableLoopOptimization` / `EnableDataFlowAnalysis` 保持 `OptimizationOptions`
            // 的默认（false）—— 与 vmlcli 那份逐字一致，不在这里"顺手显式写一遍"，
            // 免得将来默认值变了而这里看不出来。
        };
    }
}
