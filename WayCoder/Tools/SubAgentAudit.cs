using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 子智能体明文审计日志（对标 Claude Code 的 subagent transcript，给人回查用的明文版）。
///
/// 每次子智能体任务把「提示词 + 授予的工具集 + 执行结果 + 耗时」追加到
/// <c>.waycoder/audit/subagents.log</c>（已 gitignore），同时在内存保留最近
/// <see cref="HistoryMax"/> 条记录供 UI/自测读取。与 ErrorLog（结构化/按天轮转）
/// 互补——这份是可读的任务级流水账，用于排查「子智能体到底接到了什么、做了什么」。
///
/// 线程安全：并行子智能体（tasks 数组）会在多线程并发写入，文件追加用独立锁串行化。
/// </summary>
public static class SubAgentAudit
{
    private static readonly Lock _lock = new();
    private static readonly Lock _appendLock = new();
    private static readonly List<Entry> _history = [];

    /// <summary>内存保留的最近记录数上限。</summary>
    private const int HistoryMax = 200;

    /// <summary>审计日志相对路径（基于当前工作目录，.waycoder 已 gitignore）。</summary>
    public const string RelativePath = ".waycoder/audit/subagents.log";

    /// <summary>是否启用（可通过 WAYCODER_SUBAGENT_AUDIT=0 关闭；默认开启）。</summary>
    public static bool Enabled { get; set; } =
        !string.Equals(Environment.GetEnvironmentVariable("WAYCODER_SUBAGENT_AUDIT"), "0", StringComparison.OrdinalIgnoreCase);

    /// <summary>单条审计记录（不可变快照）。</summary>
    public sealed record Entry(DateTime At, int Depth, string Task, string Tools, string Result, long DurationMs);

    /// <summary>内存中的最近记录（按时间升序，越靠后越新）。</summary>
    public static IReadOnlyList<Entry> History
    {
        get { lock (_lock) return _history.ToArray(); }
    }

    /// <summary>
    /// 记录一条子智能体任务。task/result 在写入前按 <see cref="ContextManager.TruncateByRunes"/>
    /// 截断（防单个超长任务/结果把审计文件写到失控），仅影响落盘与内存快照，不影响主流程返回值。
    /// </summary>
    public static void Record(int depth, string task, string tools, string result, long durationMs)
    {
        if (!Enabled) return;

        var entry = new Entry(
            DateTime.Now,
            depth,
            ContextManager.TruncateByRunes(task, 4000),
            tools,
            ContextManager.TruncateByRunes(result, 4000),
            durationMs);

        lock (_lock)
        {
            _history.Add(entry);
            if (_history.Count > HistoryMax)
                _history.RemoveAt(0);
        }

        AppendToFile(entry);
    }

    /// <summary>清空内存历史（自测用，避免用例间串扰）。</summary>
    internal static void ClearHistory()
    {
        lock (_lock) _history.Clear();
    }

    private static void AppendToFile(Entry e)
    {
        if (string.IsNullOrWhiteSpace(e.Task) && string.IsNullOrWhiteSpace(e.Result))
            return;

        lock (_appendLock)
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), ".waycoder", "audit");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "subagents.log");
                // 单文件 10MB 上限：超限滚动到 subagents_N.log，防磁盘无限累积
                try
                {
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > Global.AuditLogFileBytes)
                    {
                        int n = 1;
                        string rolled;
                        do { rolled = Path.Combine(dir, $"subagents_{n}.log"); n++; }
                        while (File.Exists(rolled));
                        path = rolled;
                    }
                }
                catch { /* 大小检查失败忽略 */ }
                var sb = new StringBuilder();
                sb.AppendLine("============================================================");
                sb.AppendLine($"[{e.At:yyyy-MM-dd HH:mm:ss}] 深度 {e.Depth} · 耗时 {e.DurationMs}ms");
                sb.AppendLine($"任务: {e.Task}");
                sb.AppendLine($"工具: {e.Tools}");
                sb.AppendLine($"结果: {e.Result}");
                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // 审计写入失败绝不影响子智能体主流程（磁盘满/只读目录等）
            }
        }
    }
}
