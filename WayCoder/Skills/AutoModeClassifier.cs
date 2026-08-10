namespace WayCoder;

/// <summary>
/// 智能 Auto Mode 分类器 —— 对工具调用进行三级风险评估。
///
/// 三级分类：
///   Safe      — 只读操作，自动放行
///   Cautious  — 文件修改，首次确认后会话内自动允许
///   Dangerous — 破坏性/外部操作，每次都必须确认
///
/// 连续阻止保护：连续 3 次拒绝危险操作后自动退回手动模式（Ask）。
/// </summary>
public static class AutoModeClassifier
{
    public enum RiskLevel
    {
        /// <summary>只读操作，自动放行，无需确认</summary>
        Safe,
        /// <summary>文件修改，首次确认后会话内记住</summary>
        Cautious,
        /// <summary>破坏性操作，每次都必须确认</summary>
        Dangerous
    }

    /// <summary>Safe 级 —— 只读操作，直接放行</summary>
    private static readonly HashSet<string> SafeTools = new(StringComparer.OrdinalIgnoreCase)
    {
        // 文件读取/搜索
        "read_file", "ls", "grep", "glob", "stat", "pwd", "wc", "diff", "tree",
        // 任务管理
        "todo", "task_create", "task_update", "task_list", "task_get",
        // LSP / 代码分析
        "lsp", "lint",
        // 外部查询（只读）
        "fetch", "web_search", "doc",
        // 记忆（只读）
        "memory",
        // 技能
        "skill",
        // 进程查看
        "ps",
    };

    /// <summary>Cautious 级 —— 文件修改，首次确认后可记住</summary>
    private static readonly HashSet<string> CautiousTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "notebook_edit",
        "mkdir", "cp", "mv", "cd",
    };

    /// <summary>Dangerous 级 —— 破坏性/外部操作，每次确认</summary>
    private static readonly HashSet<string> DangerousTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "bash", "git", "kill", "agent",
    };

    /// <summary>连续拒绝危险操作的计数</summary>
    public static int ConsecutiveDangerousBlocks { get; set; }

    /// <summary>触发自动退回手动模式的连续阻止阈值</summary>
    public const int BlockThreshold = 3;

    /// <summary>
    /// 当连续阻止达到阈值时触发。订阅者可将模式切回 Ask。
    /// </summary>
    public static event Action? FallbackToManualTriggered;

    /// <summary>
    /// 对工具名进行风险分级。未在列表中的工具默认为 Dangerous。
    /// </summary>
    public static RiskLevel Classify(string toolName)
    {
        if (SafeTools.Contains(toolName)) return RiskLevel.Safe;
        if (CautiousTools.Contains(toolName)) return RiskLevel.Cautious;
        return RiskLevel.Dangerous;
    }

    /// <summary>
    /// 记录一次危险操作被拒绝。若连续拒绝达阈值，触发退回手动模式。
    /// </summary>
    public static void RecordDangerousBlock()
    {
        ConsecutiveDangerousBlocks++;
        if (ConsecutiveDangerousBlocks >= BlockThreshold)
        {
            ConsecutiveDangerousBlocks = 0;
            FallbackToManualTriggered?.Invoke();
        }
    }

    /// <summary>
    /// 记录一次危险操作被允许（重置连续阻止计数）。
    /// </summary>
    public static void RecordDangerousAllow()
    {
        ConsecutiveDangerousBlocks = 0;
    }

    /// <summary>
    /// 重置所有状态。
    /// </summary>
    public static void Reset()
    {
        ConsecutiveDangerousBlocks = 0;
    }

    /// <summary>
    /// 获取当前分类统计信息（用于调试/状态显示）。
    /// </summary>
    public static string GetStats()
    {
        return $"安全: {SafeTools.Count} | 谨慎: {CautiousTools.Count} | 危险: {DangerousTools.Count} | 连续阻止: {ConsecutiveDangerousBlocks}/{BlockThreshold}";
    }
}
