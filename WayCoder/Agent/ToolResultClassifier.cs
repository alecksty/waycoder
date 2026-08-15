namespace WayCoder;

/// <summary>
/// 工具结果分类器 —— 统一识别工具返回文本的成功/错误/中止状态，
/// 供 Agent 决定是否注入「修正参数后重试」自恢复提示。
/// 对标 deepseek-harness 的类型化工具结果：真实错误与「用户取消 / 权限拒绝 /
/// 安全阻止」区分对待——后者不是错误，不应诱导模型重试。
/// </summary>
public static class ToolResultClassifier
{
    /// <summary>真实错误前缀（可重试，注入自恢复提示）。按前缀匹配。</summary>
    private static readonly string[] ErrorMarkers =
    [
        "错误", "Error", "❌", "失败", "运行命令时出错",
    ];

    /// <summary>中止类前缀（用户主动取消/权限拒绝/安全阻止，非错误，不注入重试提示）。</summary>
    private static readonly string[] AbortMarkers =
    [
        "用户取消", "操作被 Hook 阻止", "⚠ 已阻止", "⛔ 沙箱阻止",
    ];

    /// <summary>结果是否为「用户取消/权限拒绝/安全阻止」类中止（非错误）。</summary>
    public static bool IsAbort(string? result)
    {
        if (string.IsNullOrWhiteSpace(result)) return false;
        var head = result.TrimStart();
        foreach (var m in AbortMarkers)
            if (head.StartsWith(m, StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary>结果是否为真实错误（可重试）。中止类结果一律判 false。</summary>
    public static bool IsError(string? result)
    {
        if (IsAbort(result)) return false;
        if (string.IsNullOrWhiteSpace(result)) return false;
        var head = result.TrimStart();
        foreach (var m in ErrorMarkers)
            if (head.StartsWith(m, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
