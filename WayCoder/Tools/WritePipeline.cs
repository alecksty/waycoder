using WayCoder.UI.Tui;

namespace WayCoder.Tools;

/// <summary>
/// 写文件流水线里可共用的两步 —— 逐 hunk 确认与 CRLF 行尾恢复。
///
/// `EditFileTool` / `MultiEditTool` / `WriteFileTool` / `DownloadTool` 四个工具各写了一遍
/// 「guard → lock → try/finally → 先读后改 → diff 确认 → CRLF → record → lint」这条流水线，
/// 其中**逐 hunk 确认**与**CRLF 恢复**这两段是逐字重复的（连注释都一样），且守卫条件的写法
/// 已经开始分叉（三种写法当前语义等价，但改一处另两处不会跟着改）——所以先收这两段零风险的部分。
///
/// 不抽整个流水线（`FileWriteScope` 那种 `using` 式 guard+lock）的原因：四处的异常路径语义
/// 并不相同（如 `DownloadTool` 取消时要先删半成品文件再 rethrow），收益不抵重构风险。
/// </summary>
internal static class WritePipeline
{
    /// <summary>
    /// CRLF 行尾恢复：**先归一化为 LF 再统一转 CRLF**，避免把已有的 <c>\r\n</c> 二次转成 <c>\r\r\n</c>。
    /// 调用时机有要求：必须在生成 diff / 记录变更**之后**（那时内容都还是 LF，行尾一致，
    /// 否则逐行比较 LF vs CRLF 会把整文件误判为改动）。
    /// </summary>
    internal static string RestoreCrlf(string content, bool hasCrlf)
        => hasCrlf ? content.Replace("\r\n", "\n").Replace("\n", "\r\n") : content;

    /// <summary>
    /// 逐 hunk 确认（`/config DiffPreview true`）。返回 <c>(是否拒绝, 可能被裁剪的新内容)</c>。
    ///
    /// 触发条件三合一：开关开启 **且** 眼下有交互界面（<see cref="WayCoder.UI.Tui.UxHelper.CanConfirmInline"/>，
    /// **不是**裸判 `Console.IsInputRedirected` —— stdin 被重定向也可能正跑着 TUI，那样逐 hunk 确认
    /// 会被静默降级成自动应用）**且** 非 YOLO（畅通模式自动放行，聊天区仍显示 unified diff）。
    /// </summary>
    internal static (bool Rejected, string NewContent) ConfirmDiff(
        string filePath, string oldContent, string newContent)
    {
        var cfg = Config.Instance;
        if (!cfg.DiffPreview || !WayCoder.UI.Tui.UxHelper.CanConfirmInline
            || PermissionManager.CurrentMode == PermissionManager.Mode.Yolo)
            return (false, newContent);

        var (decision, accepted) = DiffPreview.Show(oldContent, newContent, filePath);
        if (decision == DiffPreview.Decision.RejectAll) return (true, newContent);
        if (decision == DiffPreview.Decision.Partial && accepted != null)
            newContent = DiffPreview.ApplyAccepted(
                oldContent, DiffPreview.BuildHunks(oldContent, newContent), accepted);
        return (false, newContent);
    }
}
