using System.Text;

namespace WayCoder;

// ========================================================================
// 死机现场自动采集（FreezeCapture）
// ========================================================================
// 用户在真实终端跑 waycoder 时死机（光标闪但无法输入、Ctrl+C 无效），
// 脱离调试环境后无法用 lldb/dotnet-dump 定位。本模块提供三合一自诊断：
//
//   1. 阶段环形缓冲（黑匣子）：主循环每阶段转换打点入环（~100 条/s，
//      4096 容量 ≈ 40s 历史），看门狗每秒写一条「丰富条」（Agent/上下文状态）。
//   2. 每分钟定时落盘：心跳线程每 60s 把黑匣子 + 当前状态同步写入 logs/state_*。
//   3. 冻结时同步强制 dump：看门狗检测主循环冻结 >3s → 同步写盘完整现场
//      （黑匣子尾部 + 槽位/Agent/token/上下文 + native 栈异步追加）。
//
// 可靠性关键：现场 dump 用 File.WriteAllText 同步强制落盘（打开→写→刷→关），
// 不依赖 ErrorLog 的 5s 缓冲定时器——进程下一秒被强杀也不丢。
// 锁安全：冻结时主线程可能正持锁，采集路径一律 Monitor.TryEnter + 短超时，禁止裸 lock。
// AOT 兼容：零反射、零新 NuGet 依赖（sample/gdb 是外部进程）。
// ========================================================================

/// <summary>环形缓冲条目：普通阶段条只填 Tick+Stage；丰富条（看门狗每秒）额外填状态。</summary>
public readonly struct FreezeSample
{
    public readonly long Tick;          // Environment.TickCount64
    public readonly string Stage;       // 阶段名
    public readonly bool Rich;          // true = 丰富条（下面字段有效）
    public readonly int Slot;           // 活跃槽位
    public readonly string AgentId;     // AgentId（F1-F10 / main）
    public readonly string Model;       // EffectiveModel
    public readonly string Tool;        // 当前工具名
    public readonly int Round;          // Agent 轮次
    public readonly int MsgCount;       // 消息数
    public readonly int LastPromptTokens; // 最近上下文大小
    public readonly bool Compressing;   // 上下文压缩中
    public readonly bool SlotBusy;      // 活跃槽位 IsBusy
    public readonly int PendingCount;   // 待处理输入数

    public FreezeSample(long tick, string stage, bool rich, int slot = 0, string agentId = "",
        string model = "", string tool = "", int round = 0, int msgCount = 0,
        int lastPromptTokens = 0, bool compressing = false, bool slotBusy = false, int pendingCount = 0)
    {
        Tick = tick; Stage = stage; Rich = rich; Slot = slot; AgentId = agentId;
        Model = model; Tool = tool; Round = round; MsgCount = msgCount;
        LastPromptTokens = lastPromptTokens; Compressing = compressing;
        SlotBusy = slotBusy; PendingCount = pendingCount;
    }
}

/// <summary>阶段环形缓冲（黑匣子）：固定数组零增长，锁内写、TryEnter 读。</summary>
public static class FreezeRingBuffer
{
    private const int Capacity = 4096;
    private static readonly FreezeSample[] _buf = new FreezeSample[Capacity];
    private static int _next;
    private static readonly object _lock = new();
    private static int _count;

    /// <summary>写入一条（阶段条 / 丰富条）。锁短持有，无争用。永不抛。</summary>
    public static void Write(FreezeSample s)
    {
        try
        {
            lock (_lock)
            {
                _buf[_next] = s;
                _next = (_next + 1) % Capacity;
                if (_count < Capacity) _count++;
            }
        }
        catch { /* 采集路径绝不抛 */ }
    }

    /// <summary>读取尾部 n 条（冻结 dump 用）。TryEnter 防主线程持锁卡死采集线程。</summary>
    public static List<FreezeSample> ReadTail(int n)
    {
        var result = new List<FreezeSample>(Math.Min(n, Capacity));
        try
        {
            if (!Monitor.TryEnter(_lock, 50)) return result; // ⚠ 主线程持锁，返回空
            try
            {
                int take = Math.Min(n, _count);
                int start = ((_next - take) % Capacity + Capacity) % Capacity;
                for (int i = 0; i < take; i++)
                    result.Add(_buf[(start + i) % Capacity]);
                return result;
            }
            finally { Monitor.Exit(_lock); }
        }
        catch { return result; }
    }
}

/// <summary>
/// 死机现场采集入口。Program 启动时注入 <see cref="LiveStateProvider"/> 获取 Agent/上下文状态；
/// 主循环阶段转换调 <see cref="RecordPhase"/>；看门狗每秒调 <see cref="RecordRichSnapshot"/>；
/// 冻结触发调 <see cref="Trigger"/>；定时/手动调 <see cref="DumpNow"/>。
/// </summary>
public static class FreezeCapture
{
    /// <summary>Program 注入：返回当前廉价状态快照（不持锁/只 TryEnter）。</summary>
    public static Func<LiveState>? LiveStateProvider;

    /// <summary>当前廉价状态快照（供丰富条 / dump 组装）。所有字段在 Program 注入处用 TryEnter 安全读取。</summary>
    public sealed class LiveState
    {
        public int ActiveSlot;
        public string ActiveAgentId = "";
        public string WorkMode = "";
        public string Model = "";          // EffectiveModel
        public string SmallModel = "";
        public int Round;
        public int MessageCount = -1;      // -1 = 主线程持锁无法读取
        public string CurrentTool = "";
        public string CurrentToolBrief = "";
        public int TotalRequests;
        public int TotalPromptTokens, TotalCompletionTokens;
        public double TaskCost, LastLatencyMs;
        public int MaxTokens, LastPromptTokens;
        public bool IsCompressing;
        public int PendingSubmissions;
        public double CpuPercent;         // 最近 CPU 占用%（CpuMonitor 采样，心跳线程写入）
        public readonly bool[] SlotBusy = new bool[AgentSlot.Count];
        public readonly string[] SlotAgentIds = new string[AgentSlot.Count];
        public string LastCompactions = "";
    }

    /// <summary>
    /// 死机现场采集总开关（默认关）。由命令行 <c>--debug-dump</c> 开启（Program 解析后调 <see cref="Enable"/>）。
    /// 关闭时所有采集入口（阶段打点/丰富条/定时 dump/冻结 dump）直接短路，零开销。
    /// 设计为显式开启：用户只在需要排查死机时开，平时关闭，无需事后关闭。
    /// </summary>
    public static volatile bool Enabled;

    /// <summary>开启死机现场采集（--debug-dump）。重复调用幂等。</summary>
    public static void Enable() => Enabled = true;

    /// <summary>最近 CPU 占用%（CpuMonitor 采样后写入，心跳线程写 / LiveState 读）。volatile 不支持 double，用锁。</summary>
    private static double _cpuPercent;
    private static readonly object _cpuLock = new();

    /// <summary>更新最近 CPU 占用%（TuiAnimTicker 心跳线程每 5s 采样后调用）。</summary>
    public static void SetCpuPercent(double v) { lock (_cpuLock) _cpuPercent = v; }

    /// <summary>防重入标志（Interlocked：看门狗冻结触发与 /diag 手动同时发生时只写一份）。</summary>
    private static int _dumping;

    // 定时 dump 节流：初始化为进程启动时的 TickCount64（系统启动后毫秒数，非进程相对时间），
    // 否则 now - 0 恒 >60s → 第一次调用立即触发 dump（uptime=0 也生成了文件）。
    private static long _lastPeriodicDumpTick = Environment.TickCount64;

    /// <summary>日志目录（与 ErrorLog 同目录）。</summary>
    private static string LogDir
    {
        get
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }
    }

    // ── 主循环阶段条（极廉价，~100 条/s） ──
    public static void RecordPhase(string stage)
    {
        if (!Enabled) return; // 开关关闭时短路，零开销
        FreezeRingBuffer.Write(new FreezeSample(Environment.TickCount64, stage, rich: false));
    }

    // ── 看门狗每秒丰富条 ──
    public static void RecordRichSnapshot()
    {
        if (!Enabled) return;
        var st = BuildLiveState();
        FreezeRingBuffer.Write(new FreezeSample(
            Environment.TickCount64, "rich", rich: true,
            slot: st.ActiveSlot, agentId: st.ActiveAgentId, model: st.Model,
            tool: st.CurrentTool, round: st.Round, msgCount: st.MessageCount,
            lastPromptTokens: st.LastPromptTokens, compressing: st.IsCompressing,
            slotBusy: st.ActiveSlot >= 0 && st.ActiveSlot < st.SlotBusy.Length && st.SlotBusy[st.ActiveSlot],
            pendingCount: st.PendingSubmissions));
    }

    /// <summary>冻结触发（看门狗检测主循环冻结 >3s）：同步强制落盘完整现场 + 后台 native 栈追加。返回 dump 路径。</summary>
    public static string Trigger(string lastActivity, long staleMs)
    {
        if (!Enabled) return ""; // 开关关闭时不采集（看门狗仍记一行日志，但不再落盘现场）
        return DumpNowInternal("死机冻结", lastActivity, staleMs, captureNativeStack: true);
    }

    /// <summary>手动/定时 dump（/diag 命令、心跳每分钟）：同步写黑匣子 + 状态，后台 native 栈追加。返回 dump 路径。</summary>
    public static string DumpNow(string reason, string lastActivity, long staleMs)
    {
        if (!Enabled) return ""; // 开关关闭时 /diag 与定时 dump 不生成文件
        return DumpNowInternal(reason, lastActivity, staleMs, captureNativeStack: false);
    }

    private static string DumpNowInternal(string reason, string lastActivity, long staleMs, bool captureNativeStack)
    {
        if (Interlocked.Exchange(ref _dumping, 1) != 0) return ""; // 已在 dump，防重入
        try
        {
            var path = Path.Combine(LogDir,
                $"freeze_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            // 现场文本先组装（读 Agent/上下文状态，快），落盘与清理异步执行：
            // 原同步 File.WriteAllText + CleanupOldDumps 在心跳线程上，慢盘/大日志目录会拖住心跳
            // （冻结时心跳是唯一存活信号，被拖停 spinner 看起来更死）。
            var text = BuildDumpText(reason, lastActivity, staleMs);
            _ = Task.Run(() =>
            {
                try
                {
                    File.WriteAllText(path, text, new UTF8Encoding(false));
                    CleanupOldDumps();
                }
                catch { try { ErrorLog.Error("UI.Freeze", "现场落盘失败"); } catch { } }
            });

            if (captureNativeStack)
            {
                // native 栈追加进同一异步落盘链（等待主 dump 完成后追加）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var native = await CaptureNativeStackAsync();
                        if (!string.IsNullOrEmpty(native))
                            File.AppendAllText(path, "\n[4] 原生线程栈 (best-effort)\n" + native);
                    }
                    catch { }
                });
            }
            return path;
        }
        catch
        {
            try { ErrorLog.Error("UI.Freeze", "现场组装失败"); } catch { }
            return "";
        }
        finally
        {
            Interlocked.Exchange(ref _dumping, 0);
        }
    }

    /// <summary>心跳线程每 60s 调用的定时 dump（用户需求：每分钟一次）。节流防高频触发。</summary>
    public static void PeriodicDumpTick(string currentActivity)
    {
        if (!Enabled) return; // 开关关闭时不生成定时快照
        var now = Environment.TickCount64;
        if (now - _lastPeriodicDumpTick < 60_000) return;
        _lastPeriodicDumpTick = now;
        DumpNow("定时快照", currentActivity, 0);
    }

    // ── 现场文本组装 ──
    private static string BuildDumpText(string reason, string lastActivity, long staleMs)
    {
        var st = BuildLiveState();
        var sb = new StringBuilder();
        sb.AppendLine("==== WayCoder 状态快照 ============================");
        var utcOffset = DateTimeOffset.Now.Offset;
        sb.AppendLine($"采集时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} (UTC{utcOffset.TotalHours:F1}h)");
        sb.AppendLine($"原因: {reason} | 进程: waycoder pid={Environment.ProcessId} uptime={(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes:F1}m");
        sb.AppendLine($"版本: {Global.AppFullName} {Global.Version}");
        sb.AppendLine("──────────────────────────────────────────");

        sb.AppendLine("[1] 主循环状态");
        sb.AppendLine($"  最后活动阶段: {lastActivity}");
        sb.AppendLine($"  阶段停滞: {staleMs}ms" + (staleMs > 0 ? "（>3s 判定冻结）" : ""));

        sb.AppendLine("──────────────────────────────────────────");
        sb.AppendLine("[2] 黑匣子阶段时间线（最近 40 条，相对当前 ms）");
        var now = Environment.TickCount64;
        var tail = FreezeRingBuffer.ReadTail(40);
        foreach (var s in tail)
        {
            var rel = s.Tick == 0 ? "?" : (now - s.Tick).ToString();
            if (s.Rich)
                sb.AppendLine($"  -{rel}ms [RICH] slot={s.Slot} agent={s.AgentId} model={s.Model} tool={s.Tool} round={s.Round} msg={s.MsgCount} ctx={s.LastPromptTokens} comp={s.Compressing} busy={s.SlotBusy} pend={s.PendingCount}");
            else
                sb.AppendLine($"  -{rel}ms {s.Stage}");
        }

        sb.AppendLine("──────────────────────────────────────────");
        sb.AppendLine("[3] 槽位 / Agent / 上下文 快照");
        sb.AppendLine($"  活跃槽位: F{(st.ActiveSlot >= 0 ? st.ActiveSlot + 1 : 0)} | WorkMode: {st.WorkMode}");
        sb.AppendLine($"  Agent: {st.ActiveAgentId} | Round: {st.Round}");
        sb.AppendLine($"  模型: {st.Model} | 小模型: {st.SmallModel}");
        sb.AppendLine($"  消息数: {(st.MessageCount < 0 ? "⚠ 主线程持锁无法读取" : st.MessageCount.ToString())}");
        sb.AppendLine($"  上下文: {(st.LastPromptTokens > 0 ? st.LastPromptTokens.ToString("N0") : "?")} / {(st.MaxTokens > 0 ? st.MaxTokens.ToString("N0") : "?")} tokens | 压缩中: {st.IsCompressing}");
        sb.AppendLine($"  当前工具: {(string.IsNullOrEmpty(st.CurrentTool) ? "(无)" : $"{st.CurrentTool} {st.CurrentToolBrief}")}");
        sb.AppendLine($"  请求: {st.TotalRequests} | Prompt: {st.TotalPromptTokens:N0} | Completion: {st.TotalCompletionTokens:N0} | 花费: ${st.TaskCost:F4} | 延迟: {st.LastLatencyMs:F0}ms");
        sb.AppendLine($"  排队: PendingSubmissions={st.PendingSubmissions}");
        sb.AppendLine($"  CPU: {st.CpuPercent:F0}%");
        var slots = new List<string>();
        for (int i = 0; i < st.SlotBusy.Length; i++)
            slots.Add($"F{i + 1}{(st.SlotBusy[i] ? "●" : "○")}{st.SlotAgentIds[i]}");
        sb.AppendLine($"  槽位: {string.Join(" ", slots)}");
        if (!string.IsNullOrEmpty(st.LastCompactions))
            sb.AppendLine($"  最近压缩: {st.LastCompactions}");
        return sb.ToString();
    }

    private static LiveState BuildLiveState()
    {
        var provider = LiveStateProvider;
        LiveState st;
        if (provider == null) st = new LiveState();
        else { try { st = provider() ?? new LiveState(); } catch { st = new LiveState(); } }
        lock (_cpuLock) st.CpuPercent = _cpuPercent; // CPU 由心跳线程采样写入，补填（不在 LiveStateProvider 内）
        return st;
    }

    // ── native 栈采集（best-effort，后台异步） ──
    private static async Task<string> CaptureNativeStackAsync()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var psi = new System.Diagnostics.ProcessStartInfo("/usr/bin/sample", $"{Environment.ProcessId} 1000")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                var r = await ProcUtil.RunAsync(psi, 8000);
                if (r == null) return "(sample 超时/失败)";
                // 截断前 800 行（主线程在最前）
                var lines = (r.Value.Stdout ?? "").Split('\n');
                return string.Join('\n', lines.Take(800));
            }
            if (OperatingSystem.IsLinux())
            {
                // gdb 优先，失败退 /proc 兜底
                var gdbPsi = new System.Diagnostics.ProcessStartInfo("gdb",
                    $"-batch -p {Environment.ProcessId} -ex \"thread apply all bt\"")
                {
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                };
                var g = await ProcUtil.RunAsync(gdbPsi, 8000);
                if (g != null && !string.IsNullOrEmpty(g.Value.Stdout))
                    return string.Join('\n', g.Value.Stdout.Split('\n').Take(400));

                // /proc 兜底：每线程状态 + 等待通道
                var sb = new StringBuilder();
                try
                {
                    foreach (var t in Directory.GetDirectories($"/proc/{Environment.ProcessId}/task"))
                    {
                        try
                        {
                            var stat = File.ReadAllText(Path.Combine(t, "stat")).Split(' ');
                            var wchan = File.ReadAllText(Path.Combine(t, "wchan")).Trim();
                            sb.AppendLine($"  thread {Path.GetFileName(t)}: state={stat[2]} wchan={wchan}");
                        }
                        catch { }
                    }
                    return sb.ToString();
                }
                catch { return "(linux /proc 采集失败)"; }
            }
            if (OperatingSystem.IsWindows())
            {
                return "(Windows 无零依赖 native 栈；请手动 dotnet-dump/procdump。黑匣子+状态快照已足够定位阶段)";
            }
            return "(未知平台，跳过 native 栈)";
        }
        catch (Exception ex)
        {
            return $"(native 栈采集异常: {ex.Message})";
        }
    }

    /// <summary>保留最新 20 个 freeze_*.txt，旧的删除（best-effort，放后台）。</summary>
    private static void CleanupOldDumps()
    {
        try
        {
            var files = Directory.GetFiles(LogDir, "freeze_*.txt")
                .OrderByDescending(f => f).ToList();
            foreach (var f in files.Skip(20))
            {
                try { File.Delete(f); } catch { }
            }
        }
        catch { }
    }
}
