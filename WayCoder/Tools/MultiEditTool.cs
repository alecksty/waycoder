using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Tools;

/// <summary>
/// 批量编辑工具 —— 对同一文件执行多个顺序编辑，减小 round-trip。
///
/// 核心思想：LLM 可以一次性对同一文件提出多个编辑操作（查找替换），
/// 工具在内存中顺序应用这些编辑，任一失败都会报告但不回滚已完成部分。
/// 首个编辑的 old_string 可以为空，表示创建新文件。
/// </summary>
public class MultiEditTool : ITool
{
    public string Name => "multiedit";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "对同一文件执行多个顺序编辑操作。减小文件修改的 round-trip。首个编辑的 old_string 若为空则表示创建新文件。每个编辑的 old_string 必须在当前文件内容中唯一（或指定 replace_all）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要编辑的文件绝对路径"))
            .Set("edits", JNode.Object()
                .Set("type", "array")
                .Set("description", "要顺序执行的编辑操作列表")
                .Set("items", JNode.Object()
                    .Set("type", "object")
                    .Set("properties", JNode.Object()
                        .Set("old_string", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "要查找的精确文本（首个编辑若为空则创建新文件）"))
                        .Set("new_string", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "替换文本"))
                        .Set("replace_all", JNode.Object()
                            .Set("type", "boolean")
                            .Set("description", "替换所有匹配项（默认 false，仅替换单个唯一匹配项）")))
                    .Set("required", JNode.Array().Add("old_string").Add("new_string")))))
        .Set("required", JNode.Array().Add("file_path").Add("edits"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        // 解析 edits 数组
        var edits = ParseEdits(arguments.GetValueOrDefault("edits"));
        if (edits.Count == 0)
            return "❌ 错误：至少需要一个编辑操作";

        if (string.IsNullOrWhiteSpace(filePath))
            return "错误：file_path 不能为空 — 请提供有效的文件路径。";

        var path = Path.GetFullPath(filePath, CwdContext.Current.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

        // 敏感路径防护（与 write/edit 对齐，此前缺失）
        var sensitive = PathSafety.CheckSensitive(path);
        if (sensitive != null)
            return $"❌ 已阻止：{sensitive}（安全策略：敏感文件读写受保护）";

        // 沙箱边界：项目写限（独立于权限模式）
        var sandbox = SandboxManager.CheckWritable(path);
        if (sandbox != null)
            return sandbox;

        // 验证编辑列表
        var validationError = ValidateEdits(edits);
        if (validationError != null)
            return validationError;

        // 文件锁
        var lockErr = FileLockManager.TryAcquireOrError(path, agentId, "请等待锁释放");
        if (lockErr != null) return lockErr;

        try
        {
            // 情况 1：创建新文件（首个编辑 old_string 为空）
            if (edits.Count > 0 && string.IsNullOrEmpty(edits[0].OldString))
                return await CreateNewFile(path, edits, agentId);

            // 情况 2：编辑已有文件
            return await EditExistingFile(path, edits, agentId);
        }
        catch (Exception ex)
        {
            return $"❌ 错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }

    // ========================================================================
    // 解析
    // ========================================================================

    private static List<EditOp> ParseEdits(object? editsObj)
    {
        var result = new List<EditOp>();
        if (editsObj is JNode arr)
        {
            foreach (var item in arr.Items)
            {
                if (item.Kind != JKind.Object) continue;
                var ra = item["replace_all"];
                var replaceAll = ra is { Kind: JKind.Bool } ? ra.AsBool()
                    : ra?.AsString()?.ToLowerInvariant() == "true";

                result.Add(new EditOp
                {
                    OldString = item["old_string"]?.AsString() ?? "",
                    NewString = item["new_string"]?.AsString() ?? "",
                    ReplaceAll = replaceAll,
                });
            }
        }
        // LLM 工具参数经 JNodeToObject 把 JSON 数组转成 List<object?>、对象转成 Dictionary<string,object?>，
        // 只判 is JNode 会让真实调用永远解析为空、工具完全失效。此处兼容该形态（对齐 AskUserQuestionTool）。
        else if (editsObj is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not Dictionary<string, object?> dict) continue;
                var ra = dict.GetValueOrDefault("replace_all");
                var replaceAll = ra switch
                {
                    bool b => b,
                    string s => s.ToLowerInvariant() == "true",
                    _ => false,
                };
                result.Add(new EditOp
                {
                    OldString = dict.GetValueOrDefault("old_string")?.ToString() ?? "",
                    NewString = dict.GetValueOrDefault("new_string")?.ToString() ?? "",
                    ReplaceAll = replaceAll,
                });
            }
        }
        return result;
    }

    private static string? ValidateEdits(List<EditOp> edits)
    {
        for (int i = 0; i < edits.Count; i++)
        {
            // 只有首个编辑的 old_string 可以为空（创建新文件）
            if (i > 0 && string.IsNullOrEmpty(edits[i].OldString))
                return $"❌ 错误：编辑 #{i + 1} — 只有首个编辑的 old_string 可以为空（用于创建新文件）";
        }
        return null;
    }

    // ========================================================================
    // 创建新文件
    // ========================================================================

    private static async Task<string> CreateNewFile(string path, List<EditOp> edits, string agentId)
    {
        if (File.Exists(path))
            return $"❌ 错误：文件已存在: {path}";

        // 首个编辑提供初始内容
        var content = edits[0].NewString;
        var failed = new List<FailedEdit>();

        // 应用后续编辑
        int applied;
        (content, applied, failed) = ApplyEdits(content, edits, 1);
        applied++; // 首编辑（提供初始内容）视为成功，否则单条创建输出「0/1 编辑成功」误导

        // Diff 预览
        var cfg = Config.Instance;
        string? diffMarkup = null;
        if (cfg.DiffPreview)
        {
            if (PermissionManager.CurrentMode == PermissionManager.Mode.Yolo)
            {
                // YOLO 自动放行：新建文件全量新增渲染进工具输出，聊天区显示（三端统一）
                diffMarkup = DiffPreview.RenderAsMarkup("", content, path);
            }
            else if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                var (decision, accepted) = DiffPreview.Show("", content, path);
                if (decision == DiffPreview.Decision.RejectAll)
                    return $"已取消创建 {path}（用户拒绝变更）";
            }
        }

        // 创建父目录
        Global.EnsureDir(path);

        Global.WriteAllTextPreserveBom(path, content);
        EditFileTool.RecordChange(path, null, content);
        FileTracker.RecordWrite(path);

        var total = edits.Count;
        var failedMsg = failed.Count > 0
            ? $"\n⚠ {failed.Count} 个编辑失败:\n{FormatFailedEdits(failed)}"
            : "";

        var multiResult1 = $"✅ 已创建 {path}（{applied}/{total} 编辑成功，+{CountLines(content)} 行）{failedMsg}";
        // YOLO 自动放行：diff 渲染进工具输出，聊天区显示源码对比
        if (!string.IsNullOrEmpty(diffMarkup))
            multiResult1 = diffMarkup + "\n\n" + multiResult1;

        // LSP 诊断自动附加
        var multiDiag1 = await DiagnosticManager.TryRunLintWithTimeout(path, 3000);
        if (multiDiag1 != null)
            multiResult1 += "\n\n" + multiDiag1;

        return multiResult1;
    }

    // ========================================================================
    // 编辑已有文件
    // ========================================================================

    private static async Task<string> EditExistingFile(string path, List<EditOp> edits, string agentId)
    {
        if (!File.Exists(path))
            return $"❌ 错误：文件未找到: {path}";

        // 先读后改保护
        var preEditWarning = FileTracker.ValidatePreEdit(path);
        if (preEditWarning != null)
            return preEditWarning;

        // 检测非 UTF-8
        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"❌ 错误：无法读取 {path}"; }

        try { _ = new UTF8Encoding(false, true).GetString(raw); }
        catch { return $"❌ 错误：{path} 不是 UTF-8 文本文件"; }

        // CRLF 行尾检测
        var hasCrlf = raw.AsSpan().IndexOf("\r\n"u8) >= 0;

        var oldContent = File.ReadAllText(path, Encoding.UTF8);
        // CRLF 归一化为 LF 后再匹配（与 EditFileTool 一致）：模型多行 old_string 以 \n 结尾，
        // 直接对 \r\n 内容匹配会永远失败
        if (hasCrlf)
            oldContent = oldContent.Replace("\r\n", "\n");

        // 顺序应用编辑
        var (newContent, applied, failed) = ApplyEdits(oldContent, edits, 0);

        if (oldContent == newContent && failed.Count > 0)
            return $"❌ 所有 {edits.Count} 个编辑均失败:\n{FormatFailedEdits(failed)}";

        if (oldContent == newContent)
            return "未做出任何修改 — 所有编辑应用后内容不变";

        // Diff 预览：YOLO（畅通）自动放行不弹窗——下方统一生成的 unified diff 已进工具输出，聊天区仍显示对比（三端统一）
        var cfg = Config.Instance;
        if (cfg.DiffPreview && !Console.IsInputRedirected && !Console.IsOutputRedirected
            && PermissionManager.CurrentMode != PermissionManager.Mode.Yolo)
        {
            var (decision, accepted) = DiffPreview.Show(oldContent, newContent, path);
            if (decision == DiffPreview.Decision.RejectAll)
                return $"已取消编辑 {path}（用户拒绝变更）";
            if (decision == DiffPreview.Decision.Partial && accepted != null)
                newContent = DiffPreview.ApplyAccepted(oldContent, DiffPreview.BuildHunks(oldContent, newContent), accepted);
        }

        // 生成 diff 与记录变更须在恢复 CRLF 前（此时 oldContent/newContent 都是 LF，行尾一致）
        var diff = EditFileTool_GenerateDiff(oldContent, newContent, path);
        EditFileTool.RecordChange(path, oldContent, newContent);

        // CRLF 行尾保留：先归一化为 LF 再统一转 CRLF，避免把已有 \r\n 二次转成 \r\r\n
        if (hasCrlf)
            newContent = newContent.Replace("\r\n", "\n").Replace("\n", "\r\n");

        Global.WriteAllTextPreserveBom(path, newContent);
        FileTracker.RecordWrite(path);

        var total = edits.Count;
        var failedMsg = failed.Count > 0
            ? $"\n⚠ {failed.Count} 个编辑失败:\n{FormatFailedEdits(failed)}"
            : "";

        // 二者一正一负，须钳制 ≥0，否则输出「+3/--3」双负号
        var additions = Math.Max(0, CountNewlines(newContent) - CountNewlines(oldContent));
        var removal = Math.Max(0, CountNewlines(oldContent) - CountNewlines(newContent));

        var multiResult2 = $"✅ 已编辑 {path}（{applied}/{total} 编辑成功，+{additions}/-{removal} 行）{failedMsg}\n{diff}";

        // LSP 诊断自动附加
        var multiDiag2 = await DiagnosticManager.TryRunLintWithTimeout(path, 3000);
        if (multiDiag2 != null)
            multiResult2 += "\n\n" + multiDiag2;

        return multiResult2;
    }

    // ========================================================================
    // 核心编辑逻辑
    // ========================================================================

    /// <summary>
    /// 在内容上顺序应用编辑列表（从 startIndex 开始），返回最终内容和失败列表。
    /// </summary>
    private static (string content, int applied, List<FailedEdit> failed) ApplyEdits(
        string content, List<EditOp> edits, int startIndex)
    {
        var current = content;
        var failed = new List<FailedEdit>();
        int applied = 0;

        for (int i = startIndex; i < edits.Count; i++)
        {
            var edit = edits[i];
            try
            {
                var result = ApplySingleEdit(current, edit);
                if (result.error != null)
                {
                    failed.Add(new FailedEdit { Index = i + 1, Error = result.error, Edit = edit });
                    continue;
                }
                current = result.content!;
                applied++;
            }
            catch (Exception ex)
            {
                failed.Add(new FailedEdit { Index = i + 1, Error = ex.Message, Edit = edit });
            }
        }

        return (current, applied, failed);
    }

    /// <summary>
    /// 应用单个编辑操作。返回 (新内容, 错误信息)。
    /// </summary>
    private static (string? content, string? error) ApplySingleEdit(string content, EditOp edit)
    {
        if (string.IsNullOrEmpty(edit.OldString) && string.IsNullOrEmpty(edit.NewString))
            return (content, null);

        if (string.IsNullOrEmpty(edit.OldString))
            return (null, "old_string 不能为空（非首个编辑）");

        if (edit.ReplaceAll)
        {
            var count = CountOccurrences(content, edit.OldString);
            if (count == 0)
                return (null, $"未找到 old_string: \"{ContextManager.TruncateWithEllipsis(edit.OldString, 60, "...")}\"");
            return (content.Replace(edit.OldString, edit.NewString), null);
        }
        else
        {
            var idx = content.IndexOf(edit.OldString, StringComparison.Ordinal);
            if (idx < 0)
                return (null, $"未找到 old_string: \"{ContextManager.TruncateWithEllipsis(edit.OldString, 60, "...")}\"");

            // 检查唯一性
            var lastIdx = content.LastIndexOf(edit.OldString, StringComparison.Ordinal);
            if (idx != lastIdx)
                return (null, $"old_string 出现了 {CountOccurrences(content, edit.OldString)} 次，请包含更多上下文以确保唯一性，或设置 replace_all=true");

            var newContent = content[..idx] + edit.NewString + content[(idx + edit.OldString.Length)..];
            return (newContent, null);
        }
    }

    private static int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(substring)) return 0;
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(substring, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += substring.Length;
        }
        return count;
    }

    // ========================================================================
    // Diff 生成（复用 EditFileTool 逻辑）
    // ========================================================================

    private static string EditFileTool_GenerateDiff(string old, string newText, string filename, int context = 3)
    {
        var oldLines = old.Split('\n');
        var newLines = newText.Split('\n');

        var sb = new StringBuilder();
        var diffLines = GenerateDiffLines(oldLines, newLines, context);

        sb.AppendLine($"--- a/{filename}");
        sb.AppendLine($"+++ b/{filename}");

        foreach (var dl in diffLines)
            sb.AppendLine(dl);

        var result = sb.ToString();
        if (result.Length > 3000)
            result = ContextManager.TruncateByRunes(result, 2500) + "\n...（diff 已截断）\n";

        return result;
    }

    private static List<string> GenerateDiffLines(string[] old, string[] newText, int context)
    {
        var result = new List<string>();
        int i = 0;
        while (i < old.Length && i < newText.Length && old[i] == newText[i]) i++;
        int changeStart = i;

        int jOld = old.Length - 1, jNew = newText.Length - 1;
        while (jOld > i && jNew > i && old[jOld] == newText[jNew])
        {
            jOld--;
            jNew--;
        }

        var contextStart = Math.Max(0, changeStart - context);
        var contextEndOld = Math.Min(old.Length, jOld + 1 + context);
        var contextEndNew = Math.Min(newText.Length, jNew + 1 + context);

        for (int line = contextStart; line < changeStart; line++)
            result.Add($"  {old[line].TrimEnd('\r')}");

        for (int line = changeStart; line <= jOld; line++)
            result.Add($"-{old[line].TrimEnd('\r')}");

        for (int line = changeStart; line <= jNew; line++)
            result.Add($"+{newText[line].TrimEnd('\r')}");

        var maxEnd = Math.Max(contextEndOld, contextEndNew);
        for (int line = Math.Max(jOld, jNew) + 1; line < maxEnd && line < old.Length; line++)
            result.Add($"  {old[line].TrimEnd('\r')}");

        return result;
    }

    // ========================================================================
    // 工具函数
    // ========================================================================


    private static int CountLines(string s) =>
        string.IsNullOrEmpty(s) ? 1 : s.TrimEnd('\n').Split('\n').Length;

    private static int CountNewlines(string s) =>
        string.IsNullOrEmpty(s) ? 0 : s.Count(c => c == '\n');

    private static string FormatFailedEdits(List<FailedEdit> failed)
    {
        var sb = new StringBuilder();
        foreach (var f in failed)
            sb.AppendLine($"  #{f.Index}: {f.Error}");
        return sb.ToString();
    }
}

// ========================================================================
// 内部类型
// ========================================================================

internal class EditOp
{
    public string OldString { get; set; } = "";
    public string NewString { get; set; } = "";
    public bool ReplaceAll { get; set; }
}

internal class FailedEdit
{
    public int Index { get; set; }
    public string Error { get; set; } = "";
    public EditOp Edit { get; set; } = null!;
}
