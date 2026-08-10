using System.Text;
using WayCoder.UI;

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
    public string Description => "通过替换精确匹配的字符串来编辑文件。为安全起见，old_string 必须在文件中恰好出现一次。包含足够的上下文以确保唯一性。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["file_path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要编辑的文件路径",
            },
            ["old_string"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要查找的精确文本（必须在文件中唯一）",
            },
            ["new_string"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "替换文本",
            },
        },
        ["required"] = new JsonArray("file_path", "old_string", "new_string"),
    };

    /// <summary>
    /// 跟踪本次会话中修改的文件，供 /diff 使用。
    /// 静态集合，跨所有工具实例共享。
    /// </summary>
    public static readonly HashSet<string> ChangedFiles = [];

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var oldString = arguments.GetValueOrDefault("old_string")?.ToString() ?? "";
        var newString = arguments.GetValueOrDefault("new_string")?.ToString() ?? "";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        return await ExecuteAsync(filePath, oldString, newString, agentId);
    }

    private static async Task<string> ExecuteAsync(string filePath, string oldString, string newString, string agentId)
    {
        var path = Path.GetFullPath(filePath);

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

            // 检测非 UTF-8 文件
            byte[] raw;
            try { raw = File.ReadAllBytes(path); }
            catch { return $"错误：无法读取 {filePath}"; }

            try { _ = Encoding.UTF8.GetString(raw); }
            catch { return $"错误：{filePath} 不是 UTF-8 文本文件（edit_file 只能编辑文本文件）"; }

            var content = File.ReadAllText(path, Encoding.UTF8);
            var occurrences = CountOccurrences(content, oldString);

            if (occurrences == 0)
            {
                var preview = content.Length > 500 ? content[..500] + "..." : content;
                return $"错误：在 {filePath} 中未找到 old_string。\n文件开头内容：\n{preview}";
            }

            if (occurrences > 1)
            {
                return $"错误：old_string 在 {filePath} 中出现了 {occurrences} 次。请包含更多上下文行以确保唯一性。";
            }

            var newContent = content.ReplaceFirst(oldString, newString);

            // Diff 预览：仅当开关开启且非交互模式（管道/重定向/测试）时
            var cfg = Config.FromEnv();
            if (cfg.DiffPreview && !Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                var (decision, accepted) = DiffPreview.Show(content, newContent, filePath);
                if (decision == DiffPreview.Decision.RejectAll)
                    return $"已取消编辑 {filePath}（用户拒绝变更）";
                if (decision == DiffPreview.Decision.Partial && accepted != null)
                    newContent = DiffPreview.ApplyAccepted(content, DiffPreview.BuildHunks(content, newContent), accepted);
            }

            File.WriteAllText(path, newContent, Encoding.UTF8);
            ChangedFiles.Add(path);

            var diff = UnifiedDiff(content, newContent, path);
            return $"已编辑 {filePath}\n{diff}";
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
            result = result[..2500] + "\n...（diff 已截断）\n";

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
