namespace WayCoder.Tools;

/// <summary>
/// 工具错误文案统一 —— 消除各工具 catch 里手拼「{op}错误：{类型}: {消息}」的逐字重复。
/// 返回给 LLM 的稳定错误前缀，便于 Agent 识别哪些是工具失败而非模型问题。
/// </summary>
public static class ToolErrors
{
    /// <summary>「{op}错误：{异常类型}: {Message}」——op 通常含尾随空格（如 "cd " / "❌ "）。</summary>
    public static string Error(string op, Exception ex)
        => $"{op}错误：{ex.GetType().Name}: {ex.Message}";

    /// <summary>「错误：{op}: {异常类型}: {Message}」—— 前缀顺序与 <see cref="Error(string, Exception)"/>
    /// **相反**（`错误：` 在前）。cp/ls/mv/rm/wc 五个工具一直手拼这一版，而 `错误：` 前缀是 Agent
    /// 识别「工具失败而非模型问题」的稳定标记 —— 手拼五份，将来任一处拼错就会破坏该标记。
    /// 文案保持逐字不变（不改既有输出）。</summary>
    public static string ErrorOpPrefix(string op, Exception ex)
        => $"错误：{op}: {ex.GetType().Name}: {ex.Message}";

    /// <summary>「错误：{Message}」——不带操作名前缀的简化版。</summary>
    public static string Error(Exception ex)
        => $"错误：{ex.Message}";
}
