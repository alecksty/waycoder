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

        var jumpTargets = FindJumpTargets();
        var labelToIndex = new Dictionary<string, int>();
        for (int i = 0; i < Program.Instructions.Count; i++)
        {
            var label = Program.Instructions[i].Label;
            if (label != null) labelToIndex[label] = i;
        }

        // Phase 1: 识别基本块边界
        var leaders = new HashSet<int> { 0 };
        for (int i = 0; i < Program.Instructions.Count; i++)
        {
            var inst = Program.Instructions[i];
            if (IsJump(inst.Opcode) && i + 1 < Program.Instructions.Count)
                leaders.Add(i + 1);

            if (inst.Label != null && jumpTargets.Contains(inst.Label))
                leaders.Add(i);
        }

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

    private static bool IsJump(OpCode op) => op switch
    {
        OpCode.JMP or OpCode.JZ or OpCode.JNZ or OpCode.JE or OpCode.JNE
            or OpCode.JG or OpCode.JGE or OpCode.JL or OpCode.JLE
            or OpCode.RET or OpCode.HALT => true,
        _ => false
    };

    /// <summary>找出从入口不可达的基本块（死代码）</summary>
    public List<BasicBlock> FindUnreachableBlocks()
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        if (Blocks.Count > 0) { visited.Add(0); queue.Enqueue(0); }
        while (queue.Count > 0)
        {
            foreach (var succ in Blocks[queue.Dequeue()].Successors)
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
