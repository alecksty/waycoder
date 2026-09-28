namespace WayCoder.Tools;

/// <summary>
/// 工具错误文案统一 —— 消除各工具 catch 里手拼「{op}错误：{类型}: {消息}」的逐字重复。
/// 返回给 LLM 的稳定错误前缀，便于 Agent 识别哪些是工具失败而非模型问题。
/// </summary>
public static class ToolErrors
{
    /// <summary>
    /// 错误前缀（中文）。**这是 Agent 识别「工具失败而非模型问题」的机器可读标记**，
    /// 与 <see cref="WayCoder.ToolResultClassifier"/> 的标记表是同一份事实的两端 ——
    /// 改这里必须同步改那张表，否则 Agent 的自恢复注入会静默失效。
    /// </summary>
    public const string ZhPrefix = "错误：";

    /// <summary>
    /// 错误前缀（英文）。与 <see cref="ZhPrefix"/> 同义，供英文界面下生产使用。
    /// ⚠ 启用它（把下面三个方法改成按语言取前缀）时，**必须同时在
    /// <see cref="WayCoder.ToolResultClassifier"/> 的 <c>ErrorMarkers</c> 里保留
    /// <c>"Error"</c>** —— 那张表已经在列，但别顺手删掉。
    /// </summary>
    public const string EnPrefix = "Error: ";

    /// <summary>「{op}错误：{异常类型}: {Message}」——op 通常含尾随空格（如 "cd " / "❌ "）。</summary>
    public static string Error(string op, Exception ex)
        => $"{op}{ZhPrefix}{ex.GetType().Name}: {ex.Message}";

    /// <summary>「错误：{op}: {异常类型}: {Message}」—— 前缀顺序与 <see cref="Error(string, Exception)"/>
    /// **相反**（`错误：` 在前）。cp/ls/mv/rm/wc 五个工具一直手拼这一版，而 `错误：` 前缀是 Agent
    /// 识别「工具失败而非模型问题」的稳定标记 —— 手拼五份，将来任一处拼错就会破坏该标记。
    /// 文案保持逐字不变（不改既有输出）。</summary>
    public static string ErrorOpPrefix(string op, Exception ex)
        => $"{ZhPrefix}{op}: {ex.GetType().Name}: {ex.Message}";

    /// <summary>「错误：{Message}」——不带操作名前缀的简化版。</summary>
    public static string Error(Exception ex)
        => $"{ZhPrefix}{ex.Message}";
}
