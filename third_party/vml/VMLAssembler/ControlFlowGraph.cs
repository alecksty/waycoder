using System.Collections.Generic;
using System.Text;

namespace VMLAssembler;

/// <summary>控制流图的基本块</summary>
public class BasicBlock
{
    public int Id { get; init; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public List<int> Successors { get; } = new();
    public List<int> Predecessors { get; } = new();
    public bool IsEntry => StartIndex == 0;
    public int InstructionCount => EndIndex - StartIndex + 1;

    public override string ToString() =>
        $"BB{Id}: [{StartIndex}..{EndIndex}] ({InstructionCount} insn) → [{string.Join(", ", Successors)}]";
}

/// <summary>
/// VML 控制流图构建器。为后续 SSA 构造提供基础。
/// 支持：基本块划分 · CFG 边构建 · 不可达代码检测
/// </summary>
public class ControlFlowGraph
{
    public List<BasicBlock> Blocks { get; } = new();
    public VmlProgram Program { get; }

    /// <summary>
    /// 程序里存在**目标不是标签**的 CALL/JMP（寄存器 / 内存间接跳转）⇒ 控制流图不完整。
    ///
    /// <para>
    /// 这是 <see cref="FindUnreachableBlocks"/> 的**安全护栏**：间接跳转的去向静态不可知，
    /// 而"不可达"这个结论会让调用方**删掉**那些代码 —— 猜错的代价是程序在真机上崩，
    /// 而且是"编得过、跑起来才炸"那一类。所以只要有**一个**间接跳转，就整体放弃分析
    /// （返回空 = 一条都不删）。宁可白留着，不可删错。
    /// </para>
    /// </summary>
    public bool HasIndirectJump { get; private set; }

    /// <summary>
    /// 标签名 → 指令下标（**从 `Program.Labels` 整表建**，理由见 <see cref="Build"/> 里的长注释）。
    /// <see cref="FindUnreachableBlocks"/> 靠它解析"取地址"引用，所以必须留到 Build 之后。
    /// </summary>
    private Dictionary<string, int> _labelToIndex = new();

    public ControlFlowGraph(VmlProgram program)
    {
        Program = program;
        Build();
    }

    /// <summary>从操作数列表中提取标签引用（若有）</summary>
    private static string? GetLabelRef(Instruction inst)
    {
        if (inst.Operands == null) return null;
        foreach (var op in inst.Operands)
        {
            if (op.Type == OperandType.LABEL && op.Value is string label)
                return label;
        }
        return null;
    }

    /// <summary>收集所有跳转目标标签</summary>
    private HashSet<string> FindJumpTargets()
    {
        var targets = new HashSet<string>();
        foreach (var inst in Program.Instructions)
        {
            if (IsJump(inst.Opcode) || inst.Opcode == OpCode.CALL)
            {
                var label = GetLabelRef(inst);
                if (!string.IsNullOrEmpty(label))
                    targets.Add(label);
            }
        }
        return targets;
    }

    /// <summary>构建控制流图</summary>
    public void Build()
    {
        Blocks.Clear();
        if (Program.Instructions.Count == 0) return;

        // 间接跳转检测 —— 安全护栏，见 `HasIndirectJump`
        foreach (var inst in Program.Instructions)
            if (IsBranch(inst.Opcode) && GetLabelRef(inst) is null)
            { HasIndirectJump = true; break; }

        var jumpTargets = FindJumpTargets();

        // ⚠ **标签表必须从 `Program.Labels` 取，不能从 `Instruction.Label` 取**（2026-09-27 修）：
        //   链接之后的程序里**没有任何指令带 Label** —— 实测 hello world：69637 条指令 /
        //   带 Label 的 = **0** / `Labels` 字典 20392 条，标签全在字典里。
        //   从 `Instruction.Label` 建这张表得到的是**空表** ⇒ 所有跳转都找不到目标 ⇒
        //   块之间**一条边都没有** ⇒ 除块 0 之外全部判成"不可达" ⇒ 整份库代码被删光
        //   （实测 69637 → 11 条，然后崩在 `未找到标签: lib_io_puts`）。
        //   与 `DeadCodeEliminationPass` Phase 1 那个"判据恒假"是**同一个根因**。
        _labelToIndex = new Dictionary<string, int>();
        var labelToIndex = _labelToIndex;
        foreach (var kv in Program.Labels)
            if (kv.Value >= 0 && kv.Value < Program.Instructions.Count)
                labelToIndex[kv.Key] = kv.Value;
        //
        // ⚠ **链接器给每个库函数造的两个名字，都要认**（用户 2026-09-27 定的规则）：
        //   带前缀的 `lib_<模块>_<函数>` 与**裸名** `<函数>`（如 `lib_io_puts` / `puts`），
        //   二者**指向同一地址**，程序里用哪个名字调用都可能。从 `Program.Labels` 整表建
        //   `labelToIndex` ⇒ 两个名字自然都在，**用哪个名字调用都算可达**。
        //   规则的原话是「**可以保留多，不能多删除**」—— 所以这里不做任何"只认裸名"或
        //   "只认前缀名"的筛选：多留一个函数只是产物大几字节，少留一个就是程序在真机上崩。

        // Phase 1: 识别基本块边界
        var leaders = new HashSet<int> { 0 };
        for (int i = 0; i < Program.Instructions.Count; i++)
        {
            var inst = Program.Instructions[i];
            if (IsJump(inst.Opcode) && i + 1 < Program.Instructions.Count)
                leaders.Add(i + 1);
        }
        // 跳转目标的**地址**是块首 —— 判据是"这个地址被跳转指令指向"，
        // 与"那个标签挂在哪条指令上"无关（链接后标签根本不在指令上）。
        foreach (var t in jumpTargets)
            if (labelToIndex.TryGetValue(t, out var targetIdx))
                leaders.Add(targetIdx);

        // Phase 2: 构建基本块
        var sortedLeaders = new List<int>(leaders);
        sortedLeaders.Sort();
        for (int li = 0; li < sortedLeaders.Count; li++)
        {
            int start = sortedLeaders[li];
            int end = (li + 1 < sortedLeaders.Count) ? sortedLeaders[li + 1] - 1 : Program.Instructions.Count - 1;
            Blocks.Add(new BasicBlock { Id = li, StartIndex = start, EndIndex = end });
        }

        // Phase 3: 构建边
        for (int bi = 0; bi < Blocks.Count; bi++)
        {
            var block = Blocks[bi];
            var lastInst = Program.Instructions[block.EndIndex];

            switch (lastInst.Opcode)
            {
                case OpCode.JMP:
                    HandleJump(bi, labelToIndex, single: true);
                    break;

                case OpCode.JZ: case OpCode.JNZ: case OpCode.JE: case OpCode.JNE:
                case OpCode.JG: case OpCode.JGE: case OpCode.JL: case OpCode.JLE:
                    HandleJump(bi, labelToIndex, single: false);
                    if (bi + 1 < Blocks.Count) AddEdge(bi, bi + 1);
                    break;

                case OpCode.RET:
                case OpCode.HALT:
                    break;

                case OpCode.CALL:
                    HandleJump(bi, labelToIndex, single: false);
                    if (bi + 1 < Blocks.Count) AddEdge(bi, bi + 1);
                    break;

                default:
                    if (bi + 1 < Blocks.Count) AddEdge(bi, bi + 1);
                    break;
            }
        }
    }

    private void HandleJump(int fromBlock, Dictionary<string, int> labelToIndex, bool single)
    {
        var inst = Program.Instructions[Blocks[fromBlock].EndIndex];
        var label = GetLabelRef(inst);
        if (label != null && labelToIndex.TryGetValue(label, out var targetIdx))
        {
            var targetBlock = FindBlockContaining(targetIdx);
            if (targetBlock >= 0 && targetBlock != fromBlock)
                AddEdge(fromBlock, targetBlock);
        }
    }

    private void AddEdge(int from, int to)
    {
        if (!Blocks[from].Successors.Contains(to))
            Blocks[from].Successors.Add(to);
        if (!Blocks[to].Predecessors.Contains(from))
            Blocks[to].Predecessors.Add(from);
    }

    private int FindBlockContaining(int instructionIndex)
    {
        for (int i = 0; i < Blocks.Count; i++)
            if (instructionIndex >= Blocks[i].StartIndex && instructionIndex <= Blocks[i].EndIndex)
                return i;
        return -1;
    }

    /// <summary>
    /// **带目标的**分支：JMP / CALL / 条件跳转。与 <see cref="IsJump"/> 的区别是**不含
    /// RET / HALT** —— 两者都没有目标操作数，拿"目标是不是标签"去判会把它们全当成间接跳转，
    /// 于是护栏永远触发、不可达分析永远不跑（这类"判据把自己锁死"的形态本仓见过多次）。
    /// </summary>
    private static bool IsBranch(OpCode op) => op switch
    {
        OpCode.JMP or OpCode.CALL or OpCode.JZ or OpCode.JNZ or OpCode.JE or OpCode.JNE
            or OpCode.JG or OpCode.JGE or OpCode.JL or OpCode.JLE => true,
        _ => false
    };

    /// <summary>
    /// "块的终结指令" —— 块尾是它们时才谈得上后继。
    ///
    /// ⚠ **`CALL` 必须在这里面**（2026-09-27 修）：边只在**块尾**建（见 Phase 3），
    /// 而 `CALL` 若不算块终结，它就会落在块**中间**，那条调用边**根本不会被建立**
    /// ⇒ 被调用的函数判成不可达 ⇒ 删掉。实测症状：启动代码 `[0..15]`（以 `RET` 结尾）
    /// 里那句 `call main` 没有边 ⇒ `main` 与它调用的一切（整个库）全被删光。
    /// </summary>
    private static bool IsJump(OpCode op) => op switch
    {
        OpCode.JMP or OpCode.CALL or OpCode.JZ or OpCode.JNZ or OpCode.JE or OpCode.JNE
            or OpCode.JG or OpCode.JGE or OpCode.JL or OpCode.JLE
            or OpCode.RET or OpCode.HALT => true,
        _ => false
    };

    /// <summary>找出从入口不可达的基本块（死代码）</summary>
    public List<BasicBlock> FindUnreachableBlocks()
    {
        // 有间接跳转 ⇒ 图不完整 ⇒ **一条都不删**（见 HasIndirectJump 的说明）
        if (HasIndirectJump) return new List<BasicBlock>();
        // 有中断向量表 ⇒ 向量指向的代码（MCU 场景）不在 CFG 里，而长度不明 —— 整份放弃（保守）
        if (Program.VectorTable != 0) return new List<BasicBlock>();
        if (Blocks.Count == 0) return new List<BasicBlock>();

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        visited.Add(0); queue.Enqueue(0);

        // 除块 0 之外的**起点**（见 CollectExtraRoots）—— 少一个根就是多删一片代码
        foreach (var root in CollectExtraRoots())
        {
            var b = FindBlockContaining(root);
            if (b >= 0 && visited.Add(b)) queue.Enqueue(b);
        }

        while (queue.Count > 0)
        {
            var block = Blocks[queue.Dequeue()];

            // **「取地址」也传播可达性**（不动点在这里天然成型）：本块里任何指令引用了
            // 别的标签（`move @R0 f` 这类**不是跳转**的引用 —— 函数指针 / 回调 / 跳转表），
            // 那个目标也变可达。放在出队时做 ⇒ 新加入的块会继续被扫描，不用外层再迭代。
            //
            // ⚠ 为什么不把这些引用一律当"根"（那是第一版做法，实测只省 10%）：链接进来的
            //   库代码里到处是取地址，全当根等于**几乎不删**。而"只有可达代码取的地址才可达"
            //   才是它真正的语义 —— 实测 hello world 就靠这条从 62463 条回到 28 条。
            for (int i = block.StartIndex; i <= block.EndIndex; i++)
            {
                var instr = Program.Instructions[i];
                if (instr.Operands == null) continue;
                foreach (var op in instr.Operands)
                {
                    if (op.Type != OperandType.LABEL || op.Value is not string name) continue;
                    if (!_labelToIndex.TryGetValue(name, out var addr)) continue;
                    var tb = FindBlockContaining(addr);
                    if (tb >= 0 && visited.Add(tb)) queue.Enqueue(tb);
                }
            }

            foreach (var succ in block.Successors)
            {
                if (!visited.Contains(succ))
                { visited.Add(succ); queue.Enqueue(succ); }
            }
        }
        var dead = new List<BasicBlock>();
        for (int i = 0; i < Blocks.Count; i++)
            if (!visited.Contains(i)) dead.Add(Blocks[i]);
        return dead;
    }

    /// <summary>
    /// 除了块 0 之外还必须当作"可达起点"的地址 —— 目前**只有 `.entry` 指的入口**
    /// （万一入口不是第一条指令）。
    ///
    /// <para>
    /// "取函数地址"（`move @R0 f`）**不在这里**：它由 <see cref="FindUnreachableBlocks"/> 的
    /// BFS 在出队时传播（**只有可达代码取的地址才让目标可达**）。第一版把它们一律当根，
    /// 实测只省 10%（链接进来的库代码里到处是取地址，全当根 ≈ 几乎不删）。
    /// </para>
    /// </summary>
    private IEnumerable<int> CollectExtraRoots()
    {
        if (!string.IsNullOrEmpty(Program.EntryPoint)
            && Program.Labels.TryGetValue(Program.EntryPoint, out var ep)
            && ep >= 0 && ep < Program.Instructions.Count)
            yield return ep;
    }

    /// <summary>诊断输出</summary>
    public string Dump()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CFG: {Blocks.Count} blocks, {Program.Instructions.Count} instructions");
        foreach (var block in Blocks)
        {
            sb.AppendLine($"  {block}");
            for (int i = block.StartIndex; i <= block.EndIndex && i < Program.Instructions.Count; i++)
                sb.AppendLine($"    [{i}] {Program.Instructions[i]}");
        }
        var dead = FindUnreachableBlocks();
        if (dead.Count > 0)
        {
            sb.AppendLine($"Unreachable blocks: {dead.Count}");
            foreach (var db in dead) sb.AppendLine($"  {db}");
        }
        return sb.ToString();
    }
}
