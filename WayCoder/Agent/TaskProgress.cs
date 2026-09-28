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
    /// 本会话**是否记录过真正的进度** —— 判断"要不要注入进度块"请用这个，**别比对
    /// <see cref="GetSummary"/> 的返回文案**。
    ///
    /// <para>
    /// ⚠ 两个消费方（<c>WorkReporter</c> 的工作汇报、<c>ContextManager</c> 的压缩摘要）此前都写着
    /// <c>progress != "⏳ 就绪"</c>，而 <see cref="GetSummary"/> **从不返回那个串**
    /// （空态是「（尚无进度记录）」/ <c>(no progress recorded yet)</c>）⇒ 判据**恒真**，
    /// 于是每轮都往汇报和压缩摘要里塞一段空进度 —— 一个错都不报。
    /// 这跟语言无关，两种语言下都是坏的；它是"拿文案当判据"留下的旧尸
    /// （同族教训见 <see cref="ToolResultClassifier"/> 的注释）。
    /// </para>
    ///
    /// <para>
    /// 判据与 <see cref="GetSummary"/> 的分支**逐条对齐**（有已完成的文件 / 有计划未做完 / 有错误），
    /// 保证「HasProgress 为真」⇔「GetSummary 返回的不是空态文案」。
    /// </para>
    /// </summary>
    public static bool HasProgress
    {
        get
        {
            var done = _files.Values.Count(a => a != FileAction.Read);
            return done > 0 || (_totalPlanned > 0 && done < _totalPlanned) || !_errors.IsEmpty;
        }
    }

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
            parts.Add(L.Pick($"✅ 已完成: {doneCount} 文件", $"✅ Done: {doneCount} file(s)"));
        if (created.Count > 0)
            parts.Add(L.Pick($"  创建: {string.Join(", ", created)}", $"  Created: {string.Join(", ", created)}"));
        if (modified.Count > 0)
            parts.Add(L.Pick($"  修改: {string.Join(", ", modified)}", $"  Modified: {string.Join(", ", modified)}"));
        if (deleted.Count > 0)
            parts.Add(L.Pick($"  删除: {string.Join(", ", deleted)}", $"  Deleted: {string.Join(", ", deleted)}"));

        if (_totalPlanned > 0 && doneCount < _totalPlanned)
            parts.Add(L.Pick($"⏳ 待完成: 约 {_totalPlanned - doneCount} 文件", $"⏳ Remaining: about {_totalPlanned - doneCount} file(s)"));

        var errs = _errors.ToList();
        if (errs.Count > 0)
        {
            parts.Add(L.Pick($"❌ 遇到 {errs.Count} 个错误", $"❌ {errs.Count} error(s) encountered"));
            foreach (var e in errs.Take(5))
                parts.Add($"  {e}");
        }

        return parts.Count > 0
            ? L.Pick("## 📊 当前进度\n", "## 📊 Current progress\n") + string.Join("\n", parts)
            : L.Pick("（尚无进度记录）", "(no progress recorded yet)");
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
