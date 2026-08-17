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
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要编辑的文件路径（绝对路径）。编辑前必须先 read_file 此文件。"))
            .Set("old_string", JNode.Object()
                .Set("type", "string")
                .Set("description", "要查找并替换的精确文本。必须逐字符匹配原文，包括所有空白符、缩进、换行。含 3-5 行上下文行以确保唯一匹配（除非 replace_all=true）。从 read_file 输出中精确复制，不要凭记忆或近似猜测。"))
            .Set("new_string", JNode.Object()
                .Set("type", "string")
                .Set("description", "替换后的新文本。保持与周围代码一致的缩进和风格。"))
            .Set("replace_all", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "设为 true 替换文件中该文本的所有匹配项。默认 false 仅替换首次匹配，且要求该文本在文件中唯一出现。")))
        .Set("required", JNode.Array().Add("file_path").Add("old_string").Add("new_string"));

    /// <summary>
    /// 跟踪本次会话中修改的文件，供 /diff 使用。
    /// 静态集合，跨所有工具实例共享。线程安全（10 槽位并行写 / 主线程读）。
    /// </summary>
    public static readonly ThreadSafeStringSet ChangedFiles = new();

    /// <summary>文件变更行数统计（绝对路径 → 新增/删除行数），供 Web 面板「修改文件」显示 +N/-M。</summary>
    public static readonly ConcurrentDictionary<string, (int Added, int Deleted)> ChangedFileStats = new();

    /// <summary>
    /// 记录一次文件变更：加入 ChangedFiles 并统计 +新增/-删除 行数（基于 diff hunk）。
    /// 纯静态便于各工具复用；统计失败不影响写入。
    /// </summary>
    public static void RecordChange(string path, string? oldContent, string newContent)
    {
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

        var path = Path.GetFullPath(filePath, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

        // 文件锁检查
        if (!FileLockManager.TryAcquire(path, agentId))
        {
            var lockInfo = FileLockManager.GetLockInfo(path);
            return $"❌ 文件被锁定: {lockInfo?.Status ?? "未知"} — 请等待锁释放";
        }

        try
        {
            if (!File.Exists(path))
                return $"错误：{filePath} 未找到";

            // 先读后改保护：确保文件已被 read_file 读取且未被外部修改
            var preEditWarning = FileTracker.ValidatePreEdit(path);
            if (preEditWarning != null)
                return preEditWarning;

            // 检测非 UTF-8 文件
            byte[] raw;
            try { raw = File.ReadAllBytes(path); }
            catch { return $"错误：无法读取 {filePath}"; }

            try { _ = new UTF8Encoding(false, true).GetString(raw); }
            catch { return $"错误：{filePath} 不是 UTF-8 文本文件（edit_file 只能编辑文本文件）"; }

            var content = File.ReadAllText(path, Encoding.UTF8);
            // 检测原始行尾格式（CRLF 保留，对标 crush）
            var hasCrlf = raw.AsSpan().IndexOf("\r\n"u8) >= 0;

            // CRLF 归一化为 LF 后再匹配：模型的多行 old_string 通常以 \n 结尾，
            // 直接对含 \r\n 的内容匹配会永远失败（写入时再按 hasCrlf 恢复）。
            if (hasCrlf)
                content = content.Replace("\r\n", "\n");

            var occurrences = CountOccurrences(content, oldString);

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

            // Diff 预览：仅当开关开启且非交互模式（管道/重定向/测试）时
            var cfg = Config.Instance;
            if (cfg.DiffPreview && !Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                var (decision, accepted) = DiffPreview.Show(content, newContent, filePath);
                if (decision == DiffPreview.Decision.RejectAll)
                    return $"已取消编辑 {filePath}（用户拒绝变更）";
                if (decision == DiffPreview.Decision.Partial && accepted != null)
                    newContent = DiffPreview.ApplyAccepted(content, DiffPreview.BuildHunks(content, newContent), accepted);
            }

            // CRLF 行尾保留：先归一化为 LF 再统一转 CRLF，避免把已有 \r\n 二次转成 \r\r\n
            if (hasCrlf)
                newContent = newContent.Replace("\r\n", "\n").Replace("\n", "\r\n");

            File.WriteAllText(path, newContent, Encoding.UTF8);
            RecordChange(path, content, newContent);
            FileTracker.RecordWrite(path);

            var diff = UnifiedDiff(content, newContent, path);
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
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }

    /// <summary>
    /// 计算子串在内容中的出现次数。
    /// </summary>
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

    /// <summary>
    /// 生成新旧文件内容之间的紧凑 unified diff。
    /// </summary>
    private static string UnifiedDiff(string old, string newText, string filename, int context = 3)
    {
        var oldLines = old.Split('\n');
        var newLines = newText.Split('\n');

        // 简单的逐行比较生成 diff
        var sb = new StringBuilder();
        var diffLines = GenerateDiff(oldLines, newLines, context);

        sb.AppendLine($"--- a/{filename}");
        sb.AppendLine($"+++ b/{filename}");

        foreach (var dl in diffLines)
        {
            sb.AppendLine(dl);
        }

        var result = sb.ToString();
        if (result.Length > 3000)
            result = ContextManager.TruncateByRunes(result, 2500) + "\n...（diff 已截断）\n";

        return result;
    }

    private static List<string> GenerateDiff(string[] old, string[] newText, int context)
    {
        var result = new List<string>();
        // 找到第一个不同的行
        int i = 0;
        while (i < old.Length && i < newText.Length && old[i] == newText[i]) i++;
        int changeStart = i;

        // 找到最后一个不同的行
        int jOld = old.Length - 1, jNew = newText.Length - 1;
        while (jOld > i && jNew > i && old[jOld] == newText[jNew])
        {
            jOld--;
            jNew--;
        }

        // 上下文行
        var contextStart = Math.Max(0, changeStart - context);
        var contextEndOld = Math.Min(old.Length, jOld + 1 + context);
        var contextEndNew = Math.Min(newText.Length, jNew + 1 + context);

        for (int line = contextStart; line < changeStart; line++)
            result.Add($"  {old[line].TrimEnd('\r')}");

        // 删除的行
        for (int line = changeStart; line <= jOld; line++)
            result.Add($"-{old[line].TrimEnd('\r')}");

        // 添加的行
        for (int line = changeStart; line <= jNew; line++)
            result.Add($"+{newText[line].TrimEnd('\r')}");

        // 后续上下文
        var maxEnd = Math.Max(contextEndOld, contextEndNew);
        for (int line = Math.Max(jOld, jNew) + 1; line < maxEnd && line < old.Length; line++)
            result.Add($"  {old[line].TrimEnd('\r')}");

        return result;
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
