using System.Collections.Concurrent;

namespace WayCoder;

/// <summary>
/// Agent 本轮工作进度追踪 —— 上下文压缩时保留"已完成/待完成"状态。
///
/// 在压缩前调用 <see cref="GetSummary"/> 获取结构化进度摘要，
/// 注入到 ContinuePrompt 中，避免 Agent 压缩后重复已完成的工作。
/// </summary>
public static class TaskProgress
{
    private static readonly ConcurrentDictionary<string, FileAction> _files = new();
    private static readonly ConcurrentBag<string> _errors = new();
    private static int _totalPlanned;

    /// <summary>文件操作记录</summary>
    public enum FileAction { Created, Modified, Deleted, Read }

    /// <summary>记录一个文件操作。</summary>
    public static void RecordFile(string path, FileAction action)
    {
        _files.AddOrUpdate(path, action, (_, existing) =>
        {
            // 优先级：Created > Modified > Deleted > Read
            return (int)action < (int)existing ? action : existing;
        });
    }

    /// <summary>便捷方法：记录文件创建。</summary>
    public static void RecordCreated(string path) => RecordFile(path, FileAction.Created);

    /// <summary>便捷方法：记录文件修改。</summary>
    public static void RecordModified(string path) => RecordFile(path, FileAction.Modified);

    /// <summary>便捷方法：记录文件删除。</summary>
    public static void RecordDeleted(string path) => RecordFile(path, FileAction.Deleted);

    /// <summary>记录一个错误。</summary>
    public static void RecordError(string context, string message)
    {
        _errors.Add($"[{context}] {message}");
    }

    /// <summary>设置计划的总工作量（文件数）。</summary>
    public static void SetPlanned(int count) => _totalPlanned = count;

    /// <summary>
    /// 生成结构化进度摘要。格式：
    /// 已完成: N 文件 | 待处理: M 文件 | 错误: E
    /// 文件列表: ...
    /// </summary>
    public static string GetSummary()
    {
        var created = _files.Where(kv => kv.Value == FileAction.Created).Select(kv => kv.Key).ToList();
        var modified = _files.Where(kv => kv.Value == FileAction.Modified).Select(kv => kv.Key).ToList();
        var deleted = _files.Where(kv => kv.Value == FileAction.Deleted).Select(kv => kv.Key).ToList();

        var parts = new List<string>();

        var doneCount = created.Count + modified.Count + deleted.Count;
        if (doneCount > 0)
            parts.Add($"✅ 已完成: {doneCount} 文件");
        if (created.Count > 0)
            parts.Add($"  创建: {string.Join(", ", created)}");
        if (modified.Count > 0)
            parts.Add($"  修改: {string.Join(", ", modified)}");
        if (deleted.Count > 0)
            parts.Add($"  删除: {string.Join(", ", deleted)}");

        if (_totalPlanned > 0 && doneCount < _totalPlanned)
            parts.Add($"⏳ 待完成: 约 {_totalPlanned - doneCount} 文件");

        var errs = _errors.ToList();
        if (errs.Count > 0)
        {
            parts.Add($"❌ 遇到 {errs.Count} 个错误");
            foreach (var e in errs.Take(5))
                parts.Add($"  {e}");
        }

        return parts.Count > 0
            ? "## 📊 当前进度\n" + string.Join("\n", parts)
            : "（尚无进度记录）";
    }

    /// <summary>列出所有被操作过的文件路径。</summary>
    public static IReadOnlyList<string> GetAllFiles() => _files.Keys.ToList();

    /// <summary>重置进度追踪（新会话开始时调用）。</summary>
    public static void Reset()
    {
        _files.Clear();
        while (_errors.TryTake(out _)) { }
        _totalPlanned = 0;
    }
}
