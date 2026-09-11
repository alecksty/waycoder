using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Tools;

/// <summary>
/// 搜索替换式文件编辑（Claude Code 的关键创新）。
///
/// 核心思想：LLM 指定一个精确的子串来查找及其替换内容。
/// 该子串必须在文件中恰好出现一次，从而消除歧义，使编辑安全且可审查。
/// </summary>
public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "精确字符串替换式编辑（先读后改）。old_string 必须与文件原文逐字符匹配（空格、Tab、换行），包含 3-5 行上下文确保唯一。仅首次匹配会被替换，设 replace_all=true 替换全部。不确定空白符时多含上下文。编辑前务必先 read_file 获取精确文本，不要凭记忆猜测。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Param("string", "要编辑的文件路径（绝对路径）。编辑前必须先 read_file 此文件。"))
            .Set("old_string", JNode.Param("string", "要查找并替换的精确文本。必须逐字符匹配原文，包括所有空白符、缩进、换行。含 3-5 行上下文行以确保唯一匹配（除非 replace_all=true）。从 read_file 输出中精确复制，不要凭记忆或近似猜测。"))
            .Set("new_string", JNode.Param("string", "替换后的新文本。保持与周围代码一致的缩进和风格。"))
            .Set("replace_all", JNode.Param("boolean", "设为 true 替换文件中该文本的所有匹配项。默认 false 仅替换首次匹配，且要求该文本在文件中唯一出现。")))
        .Set("required", JNode.Array("file_path", "old_string", "new_string"));

    /// <summary>
    /// 跟踪本次会话中修改的文件，供 /diff 使用。
    /// 静态集合，跨所有工具实例共享。线程安全（10 槽位并行写 / 主线程读）。
    /// </summary>
    public static readonly ThreadSafeStringSet ChangedFiles = new() { MaxCount = Global.MaxTrackedFiles };

    /// <summary>文件变更行数统计（绝对路径 → 新增/删除行数），供 Web 面板「修改文件」显示 +N/-M。</summary>
    public static readonly ConcurrentDictionary<string, (int Added, int Deleted)> ChangedFileStats = new();

    /// <summary>
    /// 记录一次文件变更：加入 ChangedFiles 并统计 +新增/-删除 行数（基于 diff hunk）。
    /// 纯静态便于各工具复用；统计失败不影响写入。
    /// </summary>
    public static void RecordChange(string path, string? oldContent, string newContent)
    {
        // ChangedFiles 超 MaxCount 时 Add 会清空重建；检测到将重置则同步清空统计字典，
        // 防 ChangedFileStats「只写不删」永久残留（面板只显示本会话，清空影响可接受）。
        if (ChangedFiles.MaxCount > 0 && ChangedFiles.Count >= ChangedFiles.MaxCount)
            ChangedFileStats.Clear();
        ChangedFiles.Add(path);
        int added = 0, deleted = 0;
        try
        {
            foreach (var h in DiffPreview.BuildHunks(oldContent ?? "", newContent))
                foreach (var l in h.Lines)
                {
                    if (l.Kind == '+') added++;
                    else if (l.Kind == '-') deleted++;
                }
        }
        catch { }
        ChangedFileStats[path] = (added, deleted);
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var oldString = arguments.GetValueOrDefault("old_string")?.ToString() ?? "";
        var newString = arguments.GetValueOrDefault("new_string")?.ToString() ?? "";
        var replaceAll = arguments.TryGetValue("replace_all", out var ra) &&
                         ra?.ToString()?.ToLowerInvariant() == "true";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        return await ExecuteAsync(filePath, oldString, newString, replaceAll, agentId);
    }

    private static async Task<string> ExecuteAsync(string filePath, string oldString, string newString, bool replaceAll, string agentId)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "错误：file_path 不能为空 — 请提供有效的文件路径。";

        var path = CwdContext.Resolve(filePath); // cd 后相对路径基于被跟踪工作目录

        // 敏感路径防护（SSH 密钥/shell 配置/系统凭据，防提示注入写后门）
        if (PathSafety.Guard(path) is { } guardErr) return guardErr;

        // 文件锁检查
        var lockErr = FileLockManager.TryAcquireOrError(path, agentId);
        if (lockErr != null) return lockErr;

        try
        {
            if (!File.Exists(path))
                return $"错误：{filePath} 未找到";

            // 先读后改保护：确保文件已被 read_file 读取且未被外部修改
            var preEditWarning = FileTracker.ValidatePreEdit(path);
            if (preEditWarning != null)
                return preEditWarning;

            // 读取 + UTF-8 校验 + CRLF 归一化（与 MultiEditTool 同源）
            if (FileText.ReadUtf8File(path, out var content, out var hasCrlf) is { } err)
                return err;

            var occurrences = FileText.CountOccurrences(content, oldString);

            if (occurrences == 0)
            {
                var preview = content.Length > 500 ? ContextManager.TruncateByRunes(content, 500) + "..." : content;
                return $"错误：在 {filePath} 中未找到 old_string。\n文件开头内容：\n{preview}";
            }

            string newContent;
            if (replaceAll)
            {
                newContent = content.Replace(oldString, newString);
            }
            else
            {
                if (occurrences > 1)
                {
                    return $"错误：old_string 在 {filePath} 中出现了 {occurrences} 次。请包含更多上下文行以确保唯一性，或设置 replace_all=true。";
                }
                newContent = content.ReplaceFirst(oldString, newString);
            }

            // 逐 hunk 确认（YOLO 自动放行；下方统一生成的 unified diff 仍进工具输出，聊天区照样显示对比）
            var (rejected, confirmed) = WritePipeline.ConfirmDiff(filePath, content, newContent);
            if (rejected) return $"已取消编辑 {filePath}（用户拒绝变更）";
            newContent = confirmed;

            // 生成 diff 与记录变更须在恢复 CRLF 前（此时 content/newContent 都是 LF，行尾一致，
            // 否则逐行比较 LF vs CRLF 会把整文件误判为改动）
            // 走 UnifiedDiff.Generate（多 hunk + @@ 头；引擎在 UI/Shared，MAUI 也编译得到）。此前本类与 MultiEditTool 各有一份
            // 逐字拷贝的私有实现，只找「首个差异行 → 末尾差异行」、**只支持单块改动且无 @@ 头** ——
            // 同文件两处相隔较远的小改动会被呈现成「删掉中间全部 + 重新加」，误导模型。
            var diff = WayCoder.UI.Shared.UnifiedDiff.Generate(content, newContent, filePath);
            RecordChange(path, content, newContent);

            // CRLF 行尾保留（共享实现，见 WritePipeline.RestoreCrlf）
            newContent = WritePipeline.RestoreCrlf(newContent, hasCrlf);

            Global.WriteAllTextPreserveBom(path, newContent);
            FileTracker.RecordWrite(path);
            var replacedMsg = replaceAll && occurrences > 1
                ? $"（{occurrences} 处替换）"
                : "";
            var result = $"已编辑 {filePath}{replacedMsg}\n{diff}";

            // LSP 诊断自动附加：运行 lint 检查新引入的错误
            var diagnostics = await DiagnosticManager.TryRunLintWithTimeout(path, 3000);
            if (diagnostics != null)
                result += "\n\n" + diagnostics;

            return result;
        }
        catch (Exception ex)
        {
            return ToolErrors.Error("", ex);
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }

}

/// <summary>
/// 字符串扩展：替换第一次出现。
/// </summary>
internal static class StringExtensions
{
    public static string ReplaceFirst(this string text, string oldValue, string newValue)
    {
        var idx = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (idx < 0) return text;
        return text[..idx] + newValue + text[(idx + oldValue.Length)..];
    }
}
