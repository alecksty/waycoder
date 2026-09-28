namespace WayCoder;

/// <summary>
/// 工具结果分类器 —— 统一识别工具返回文本的成功/错误/中止状态，
/// 供 Agent 决定是否注入「修正参数后重试」自恢复提示。
/// 对标 deepseek-harness 的类型化工具结果：真实错误与「用户取消 / 权限拒绝 /
/// 安全阻止」区分对待——后者不是错误，不应诱导模型重试。
///
/// <para>
/// ⚠⚠ <b>这里的标记是「协议」，不是「文案」——永远双语识别，绝不跟着界面语言走。</b>
/// 理由：被识别的文本**不一定由当前进程产生** —— 存档会话、MCP 服务器返回、模型自述、
/// 跨版本的历史会话，都可能是另一种语言写的。若让标记跟着 <c>L.IsZh</c> 变，那么
/// "英文界面下加载了一段中文存档"就会判不出来 ⇒ Agent 不再注入自恢复提示、
/// <c>ContextManager</c> 压缩时也不再保留错误行 —— <b>表现是「AI 变笨了」，
/// 零报错、零日志</b>（本仓最怕的失败形态）。
/// </para>
///
/// <para>
/// 因此：**标记层不本地化，只有它包着的散文本地化**。要把工具错误文案翻成英文时，
/// 英文前缀必须同时进这张表（见 <see cref="WayCoder.Tools.ToolErrors"/> 的前缀常量）。
/// </para>
/// </summary>
public static class ToolResultClassifier
{
    /// <summary>真实错误前缀（可重试，注入自恢复提示）。按前缀匹配，中英并行。</summary>
    private static readonly string[] ErrorMarkers =
    [
        // 中文
        "错误", "失败", "运行命令时出错",
        // 英文 —— 与 ToolErrors 的英文前缀配套；改前缀常量时这里要跟着改
        "Error", "Failed", "failed to", "Timed out",
        // 符号（语言中性）
        "❌",
    ];

    /// <summary>中止类前缀（用户主动取消/权限拒绝/安全阻止，非错误，不注入重试提示）。</summary>
    private static readonly string[] AbortMarkers =
    [
        // 中文
        "用户取消", "操作被 Hook 阻止", "⚠ 已阻止", "⛔ 沙箱阻止",
        // 英文
        "Cancelled by user", "Canceled by user", "User cancelled", "User canceled",
        "Blocked by hook", "Blocked by sandbox", "⛔ Sandbox blocked",
    ];

    /// <summary>结果是否为「用户取消/权限拒绝/安全阻止」类中止（非错误）。</summary>
    public static bool IsAbort(string? result)
    {
        if (string.IsNullOrWhiteSpace(result)) return false;
        var head = result.TrimStart();
        foreach (var m in AbortMarkers)
            // ⚠ 与 IsError 同一个比较口径（原先是 Ordinal，加英文标记后必须放宽 ——
            //   否则 "⛔ sandbox blocked" 这种小写变体判不出来，而两条路径口径不同
            //   本身就是本仓记过的「同一规则两处实现」）。
            if (head.StartsWith(m, StringComparison.OrdinalIgnoreCase))
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
