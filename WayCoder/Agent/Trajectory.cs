using System.Text;

namespace WayCoder;

/// <summary>
/// 运行轨迹记录器 —— 对标 OpenClaw trajectory JSONL 回放。
///
/// 把每次 Agent 运行的完整过程（每轮 LLM 的 token 消耗、每个工具调用的入参/结果/耗时）
/// 以版本化 JSONL 事件流逐条落盘，形成可回放、可分析、可复现的「运行轨迹」。
/// 这是调试 agent 行为、评估模型质量、复现 bug 的基石。
///
/// 事件流（每行一个 JSON 对象）：
///   run_start  - 运行开始（runId/sessionId/model/schema 版本）
///   llm_turn   - 每轮 LLM（轮次/token/内容长度/工具数/推理长度）
///   tool_call  - 每个工具调用（工具名/入参摘要/结果摘要/成败/耗时）
///   run_end    - 运行结束（轮次/累计 token 汇总）
///
/// 落盘 .waycoder/trajectory/<runId>.jsonl（已被 .gitignore 覆盖）。
/// 开关：WAYCODER_TRAJECTORY=0 关闭（默认开）。
/// 纯手搓 JSONL 追加（File.AppendAllText + lock + Interlocked 序列号），AOT 安全，零依赖。
/// </summary>
public sealed class Trajectory
{
    /// <summary>轨迹 schema 标识（区分不同 agent 产生的轨迹格式）</summary>
    public const string SchemaId = "waycoder-trajectory";
    /// <summary>schema 版本号（结构变更时递增）</summary>
    public const int SchemaVersion = 1;
    /// <summary>工具入参/结果摘要的最大字符数（超长截断，避免轨迹文件膨胀）</summary>
    public const int MaxSummaryChars = 2000;

    /// <summary>是否启用轨迹记录（环境变量 WAYCODER_TRAJECTORY=0 关闭）</summary>
    public static bool Enabled => Environment.GetEnvironmentVariable("WAYCODER_TRAJECTORY") != "0";

    /// <summary>轨迹目录（相对当前工作目录的 .waycoder/trajectory）</summary>
    public static string TrajectoryDir =>
        Path.Combine(Directory.GetCurrentDirectory(), Global.ConfigDirName, "trajectory");

    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly string _runId;
    private readonly string _sessionId;
    private readonly string _model;
    private int _seq;
    private int _rounds;
    private long _totalPromptTokens;
    private long _totalCompletionTokens;

    /// <summary>轨迹文件完整路径（供测试/回放读取）。</summary>
    internal string FilePath => _filePath;

    private Trajectory(string runId, string sessionId, string model, string filePath)
    {
        _runId = runId;
        _sessionId = sessionId;
        _model = model;
        _filePath = filePath;
    }

    /// <summary>
    /// 创建一次运行轨迹（写 run_start 事件），返回记录器实例。
    /// 轨迹关闭（Enabled=false）或落盘失败时返回 null。
    /// </summary>
    public static Trajectory? Create(string model, string? sessionId = null, string? dir = null)
    {
        if (!Enabled) return null;
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var targetDir = dir ?? TrajectoryDir;
            Directory.CreateDirectory(targetDir);
            var filePath = Path.Combine(targetDir, $"{runId}.jsonl");
            var t = new Trajectory(runId, sessionId ?? "", model, filePath);
            t.Record("run_start", JNode.Object()
                .Set("runId", runId)
                .Set("sessionId", sessionId ?? "")
                .Set("model", model));
            return t;
        }
        catch { return null; } // 轨迹失败不影响主流程
    }

    /// <summary>记录一轮 LLM 交互（token 消耗 + 输出形态）。内部累计总轮次与总 token。</summary>
    public void RecordTurn(int round, int promptTokens, int completionTokens,
        int contentLen, int toolCallCount, int reasoningLen)
    {
        _rounds = round + 1;
        _totalPromptTokens += promptTokens;
        _totalCompletionTokens += completionTokens;
        Record("llm_turn", JNode.Object()
            .Set("round", round)
            .Set("promptTokens", promptTokens)
            .Set("completionTokens", completionTokens)
            .Set("contentLen", contentLen)
            .Set("toolCallCount", toolCallCount)
            .Set("reasoningLen", reasoningLen));
    }

    /// <summary>记录一个工具调用的结果（入参/结果摘要 + 成败 + 耗时）。</summary>
    public void RecordTool(string name, string argsBrief, string result, bool ok, long durationMs)
    {
        Record("tool_call", JNode.Object()
            .Set("name", name)
            .Set("args", Truncate(argsBrief, MaxSummaryChars))
            .Set("result", Truncate(result, MaxSummaryChars))
            .Set("ok", ok)
            .Set("durationMs", (double)durationMs));
    }

    /// <summary>记录运行结束（轮次 + 累计 token 汇总）。</summary>
    public void End()
    {
        Record("run_end", JNode.Object()
            .Set("rounds", _rounds)
            .Set("totalPromptTokens", (double)_totalPromptTokens)
            .Set("totalCompletionTokens", (double)_totalCompletionTokens)
            .Set("totalTokens", (double)(_totalPromptTokens + _totalCompletionTokens)));
    }

    /// <summary>追加一条轨迹事件（线程安全：seq 原子递增，文件写锁串行化）。</summary>
    private void Record(string type, JNode data)
    {
        try
        {
            var seq = Interlocked.Increment(ref _seq) - 1;
            var ev = JNode.Object()
                .Set("traceSchema", SchemaId)
                .Set("schemaVersion", SchemaVersion)
                .Set("runId", _runId)
                .Set("sessionId", _sessionId)
                .Set("model", _model)
                .Set("type", type)
                .Set("ts", DateTime.UtcNow.ToString("o"))
                .Set("seq", seq)
                .Set("data", data);
            lock (_lock)
            {
                File.AppendAllText(_filePath, ev.ToJson() + "\n", Encoding.UTF8);
            }
        }
        catch { /* 轨迹写入失败不影响主流程 */ }
    }

    /// <summary>
    /// 截断超长文本：保留头部 60% + 尾部，中间插入省略标记。
    /// 用于控制工具入参/结果摘要的体量，避免轨迹文件无限膨胀。
    /// </summary>
    internal static string Truncate(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s;
        const string mark = "\n…[已截断]…\n";
        var headLen = maxChars * 60 / 100;
        var tailLen = maxChars - headLen - mark.Length;
        if (tailLen <= 0) return ContextManager.TruncateByRunes(s, maxChars);
        return ContextManager.TruncateByRunes(s, headLen) + mark + ContextManager.TruncateTailByRunes(s, tailLen);
    }
}
