namespace WayCoder;

/// <summary>
/// Agent 工作模式 —— 每个槽位独立设置，Shift+Tab 切换。
///
/// 四种模式：
///   Build (建造)  — 完整工具访问，正常编程（默认）
///   Plan  (计划)  — 只分析规划，不修改代码
///   Review(审查)  — 只读 + 审查工具，代码审查
///   Auto  (自动)  — 全工具 + SmartAuto 智能分级确认
/// </summary>
public enum WorkMode
{
    Build,
    Plan,
    Review,
    Auto,
}

/// <summary>
/// 工作模式管理器 —— 统一管理模式的工具约束、切换逻辑。
/// </summary>
public static class WorkModeManager
{
    /// <summary>每个模式的显示名称</summary>
    public static readonly Dictionary<WorkMode, string> Labels = new()
    {
        [WorkMode.Build]  = "建造",
        [WorkMode.Plan]   = "计划",
        [WorkMode.Review] = "审查",
        [WorkMode.Auto]   = "自动",
    };

    /// <summary>每个模式的 emoji 图标</summary>
    public static readonly Dictionary<WorkMode, string> Emojis = new()
    {
        [WorkMode.Build]  = "🔨",
        [WorkMode.Plan]   = "🧠",
        [WorkMode.Review] = "🔍",
        [WorkMode.Auto]   = "🤖",
    };

    /// <summary>Plan 模式下禁止的工具（写入 + 破坏性操作）</summary>
    private static readonly HashSet<string> PlanBlockedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "notebook_edit",
        "bash", "rm", "mkdir", "cp", "mv",
        "git", "kill", "agent",
    };

    /// <summary>Review 模式下禁止的工具（写入 + 破坏性操作，但保留 agent 用于审查子代理）</summary>
    private static readonly HashSet<string> ReviewBlockedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "notebook_edit",
        "bash", "rm", "mkdir", "cp", "mv",
        "git", "kill",
    };

    /// <summary>当前活跃槽位的工作模式（由 Program.cs 在切换时更新）</summary>
    public static WorkMode CurrentMode { get; set; } = WorkMode.Build;

    /// <summary>模式切换时触发（用于 UI 刷新）</summary>
    public static event Action<WorkMode>? ModeChanged;

    /// <summary>
    /// 切换到下一个模式（Build → Plan → Review → Auto → Build ...）
    /// </summary>
    public static WorkMode CycleNext()
    {
        CurrentMode = CurrentMode switch
        {
            WorkMode.Build  => WorkMode.Plan,
            WorkMode.Plan   => WorkMode.Review,
            WorkMode.Review => WorkMode.Auto,
            WorkMode.Auto   => WorkMode.Build,
            _               => WorkMode.Build,
        };
        ModeChanged?.Invoke(CurrentMode);
        return CurrentMode;
    }

    /// <summary>
    /// 设置指定模式。
    /// </summary>
    public static void SetMode(WorkMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        ModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// 检查指定工具在给定模式下是否允许执行。
    /// 返回 null 表示允许，返回字符串表示阻止原因。
    /// </summary>
    public static string? CheckToolAllowed(string toolName, WorkMode mode)
    {
        switch (mode)
        {
            case WorkMode.Plan:
                if (PlanBlockedTools.Contains(toolName))
                    return $"计划模式禁止执行 {toolName}。请切换到建造模式（Shift+Tab）后再操作。";
                break;
            case WorkMode.Review:
                if (ReviewBlockedTools.Contains(toolName))
                    return $"审查模式禁止执行 {toolName}。请切换到建造模式（Shift+Tab）后再操作。";
                break;
        }
        return null; // Build / Auto：所有工具允许
    }

    /// <summary>
    /// 获取模式的 System Prompt 前缀（附加在正常 prompt 前）。
    /// </summary>
    public static string GetModePrompt(WorkMode mode)
    {
        return mode switch
        {
            WorkMode.Plan => """
                # 当前模式：🧠 计划模式

                你当前处于**计划模式**——只能分析和规划，不能修改任何代码。
                禁止使用 write_file、edit_file、bash 等修改性工具。
                你需要：1. 分析需求 2. 探索代码 3. 制定详细执行计划
                用户确认计划后会切换到建造模式执行。

                """,
            WorkMode.Review => """
                # 当前模式：🔍 审查模式

                你当前处于**审查模式**——只读代码审查，不能修改任何代码。
                你需要：检查代码质量、发现潜在 bug、提出改进建议。
                使用 read_file、grep、glob、lsp、diff 等只读工具进行分析。

                """,
            WorkMode.Auto => """
                # 当前模式：🤖 自动模式

                你当前处于**自动模式**——所有工具可用，Safe 工具自动放行，
                Cautious 工具首次确认后记住，Dangerous 工具每次确认。
                你可以自由编写和修改代码。

                """,
            _ => "" // Build 模式：标准 prompt，不附加
        };
    }

    /// <summary>
    /// 获取模式切换的格式字符串（用于状态显示）。
    /// </summary>
    public static string Format(WorkMode mode)
    {
        var emoji = Emojis.GetValueOrDefault(mode, "?");
        var label = Labels.GetValueOrDefault(mode, mode.ToString());
        return $"{emoji} {label}";
    }
}
