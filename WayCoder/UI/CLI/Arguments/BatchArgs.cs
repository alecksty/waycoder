using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 批量任务引擎 —— 多仓库并行处理（worktree 隔离）
// ═══════════════════════════════════════════════════════════════

public class BatchArg : CliArg
{
    public override string Description => "批量任务引擎：多仓库并行处理（--batch <JSON文件|内联JSON>，每个任务在独立克隆副本中隔离执行）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "JSON";
    public BatchArg() : base("batch", "--batch") { }
}

public class BatchRepoArg : CliArg
{
    public override string Description => "批量任务：添加一个仓库（可重复，配合 --batch-task 共享任务）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "仓库";
    public override bool AllowMultiple => true;
    public BatchRepoArg() : base("batch-repo", "--batch-repo") { }
}

public class BatchTaskArg : CliArg
{
    public override string Description => "批量任务：所有 --batch-repo 仓库的共享任务";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public BatchTaskArg() : base("batch-task", "--batch-task") { }
}

public class BatchKeepArg : CliArg
{
    public override string Description => "批量任务：保留克隆的工作副本（默认执行后清理）";
    public BatchKeepArg() : base("batch-keep", "--batch-keep") { }
}

// ═══════════════════════════════════════════════════════════════
// 槽位任务参数 — -p1 ~ -p0 对应 F1~F10
// ═══════════════════════════════════════════════════════════════

/// <summary>所有槽位任务的共享前缀（-pa "前缀" → 自动拼到每个 -pN 任务前面）</summary>
public class SlotPromptAllArg : CliArg
{
    public override string Description => "所有槽位任务的共享前缀（自动拼到每个 -pN 前面）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "前缀";
    public SlotPromptAllArg() : base("prompt-all", "-pa", "--prompt-all") { }
}

public class SlotPromptArg : CliArg
{
    /// <summary>目标槽位索引（0-based，-p1→0, -p2→1, ..., -p0→9）</summary>
    public int SlotIndex { get; }
    public override string Description => $"投递任务到槽位 F{SlotIndex + 1}（-p1~-p9, -p0=F10）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public override bool Internal => true; // 10 个参数不逐行显示
    public override bool AllowMultiple => true; // 同一槽位多次 -pN 可排队

    /// <param name="slotNum">用户输入的槽位号（1-9, 0=10），内部转为 0-based 索引</param>
    public SlotPromptArg(int slotNum) : base(
        $"slot-prompt-{slotNum}",
        $"-p{slotNum}",
        $"--prompt-slot-{slotNum}")
    {
        SlotIndex = slotNum switch
        {
            0 => 9,       // -p0 → F10 → 索引 9
            _ => slotNum - 1,  // -p1 → F1 → 索引 0, ...
        };
    }
}
