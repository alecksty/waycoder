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
        return ToolSafetyRegistry.ClassifyRisk(toolName) switch
        {
            ToolSafetyRegistry.ToolRisk.Safe => RiskLevel.Safe,
            ToolSafetyRegistry.ToolRisk.Cautious => RiskLevel.Cautious,
            _ => RiskLevel.Dangerous,
        };
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
        return $"安全: {ToolSafetyRegistry.CountRisk(ToolSafetyRegistry.ToolRisk.Safe)}" +
               $" | 谨慎: {ToolSafetyRegistry.CountRisk(ToolSafetyRegistry.ToolRisk.Cautious)}" +
               $" | 危险: {ToolSafetyRegistry.CountRisk(ToolSafetyRegistry.ToolRisk.Dangerous)}" +
               $" | 连续阻止: {ConsecutiveDangerousBlocks}/{BlockThreshold}";
    }
}
