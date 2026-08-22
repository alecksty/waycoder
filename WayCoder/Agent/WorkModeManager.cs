namespace WayCoder;

/// <summary>
/// Agent 工作模式 —— 每个槽位独立设置，Shift+Tab 切换。
///
/// 三种模式：
///   Build (建造) — 完整工具访问，正常编程（默认），工具与提示词受经济模式管理
///   Plan  (规划) — 只读分析/规划，白名单只读工具 + 精简提示词，产出计划后经审批门切回 Build
///   Chat  (聊天) — 纯聊天：0 工具 + 0 系统提示词，每轮只剩 user/assistant 消息
/// </summary>
public enum WorkMode
{
    Build,
    Plan,
    Chat,
}

/// <summary>
/// 工作模式管理器 —— 统一管理模式的工具约束、切换逻辑。
/// </summary>
public static class WorkModeManager
{
    /// <summary>每个模式的显示名称</summary>
    public static readonly Dictionary<WorkMode, string> Labels = new()
    {
        [WorkMode.Build] = "建造",
        [WorkMode.Plan]  = "计划",
        [WorkMode.Chat]  = "聊天",
    };

    /// <summary>每个模式的 emoji 图标</summary>
    public static readonly Dictionary<WorkMode, string> Emojis = new()
    {
        [WorkMode.Build] = "🔨",
        [WorkMode.Plan]  = "🧠",
        [WorkMode.Chat]  = "💬",
    };

    /// <summary>
    /// Plan 模式的只读工具白名单（schema 层硬过滤，单一事实源）。
    /// bash 特殊：仅放行 BashGuard.IsSafeReadOnly 的只读命令（git log/diff/status、ls/cat 等），
    /// 由 CheckToolAllowed 按命令参数门控 —— 使规划模式具备 git 历史/文件只读分析能力。
    /// 明确排除写/执行/风险工具：write_file/edit_file/multiedit/find_replace/notebook_edit/agent/git/git_pr/
    /// download/sqlite/test/draw/convert_image/export_chat/job_kill/kill/rm/mkdir/cp/mv/cd/screenshot/struct_todo。
    /// </summary>
    public static readonly HashSet<string> PlanReadOnlyTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "read_file", "glob", "grep", "ls", "tree", "stat", "wc", "pwd", "diff",
        "doc", "web_search", "fetch", "lsp", "lint", "skill", "memory",
        "transcribe", "view_image", "ask_user_question", "job_output", "ps", "todo",
        "bash",
    };

    /// <summary>当前活跃槽位的工作模式（由 Program.cs 在切换时更新）</summary>
    public static WorkMode CurrentMode { get; set; } = WorkMode.Build;

    /// <summary>模式切换时触发（用于 UI 刷新）</summary>
    public static event Action<WorkMode>? ModeChanged;

    /// <summary>
    /// 切换到下一个模式（Build → Plan → Chat → Build ...）
    /// </summary>
    public static WorkMode CycleNext()
    {
        CurrentMode = CurrentMode switch
        {
            WorkMode.Build => WorkMode.Plan,
            WorkMode.Plan  => WorkMode.Chat,
            WorkMode.Chat  => WorkMode.Build,
            _              => WorkMode.Build,
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
    /// args 供 bash 命令参数门控使用（Plan 模式仅放行只读命令）。
    /// </summary>
    public static string? CheckToolAllowed(string toolName, WorkMode mode, Dictionary<string, object?>? args = null)
    {
        switch (mode)
        {
            case WorkMode.Chat:
                return $"聊天模式不提供任何工具（纯聊天）。请切换到建造/规划模式（Shift+Tab）后再操作。";
            case WorkMode.Plan:
                // bash：仅放行只读命令（fail-closed：拿不到 command 参数一律阻止）
                if (string.Equals(toolName, "bash", StringComparison.OrdinalIgnoreCase))
                {
                    if (args is not null && args.TryGetValue("command", out var cmdObj) && cmdObj is string cmdStr &&
                        BashGuard.IsSafeReadOnly(cmdStr))
                        return null;
                    return $"规划模式仅允许只读 bash 命令（如 git log/diff/status、ls/cat/grep）。请切换到建造模式（Shift+Tab）后再操作。";
                }
                if (!PlanReadOnlyTools.Contains(toolName))
                    return $"规划模式仅允许只读工具（read_file/web_search/doc 等）。请切换到建造模式（Shift+Tab）后再操作。";
                break;
        }
        return null; // Build：所有工具允许
    }

    /// <summary>
    /// 获取模式的 System Prompt 前缀（附加在正常 prompt 前）。
    /// Chat 模式返回空串（纯聊天，连模式提示也不注入）。
    /// </summary>
    public static string GetModePrompt(WorkMode mode)
    {
        return mode switch
        {
            WorkMode.Plan => """
                # 当前模式：🧠 计划模式

                你当前处于**只读分析/规划模式** —— 只能读、查、规划，不能修改任何代码。
                禁止使用 write_file、edit_file 等写入工具；bash 仅限只读命令（git log/diff/status、ls/cat 等）。
                你需要：1. 分析需求 2. 探索代码/文档 3. 制定详细执行计划（含涉及文件与验证方式）。
                用户确认计划后会切换到建造模式执行。

                """,
            _ => "" // Build / Chat：不附加模式提示
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
