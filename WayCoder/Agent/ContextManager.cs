using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 多层上下文压缩。
///
/// WayCoder 以 3 层实现：
///   - 第 1 层（tool_snip）：用截断版本替换冗长的工具结果
///   - 第 2 层（summarize）：LLM 驱动的旧对话摘要
///   - 第 3 层（hard_collapse）：最后手段：仅保留摘要 + 最近消息
///
/// 此外支持 Crush 风格的 StopWhen：基于真实 API token 使用量，
/// 当剩余窗口低于阈值时提前触发摘要（而非等估算值超限）。
/// </summary>
public class ContextManager
{
    public int MaxTokens { get; private set; }

    private int _snipAt;       // 50% -> 裁剪工具输出
    private int _summarizeAt;  // 70% -> LLM 摘要
    private int _collapseAt;   // 90% -> 硬折叠

    /// <summary>累计 prompt tokens（来自 API usage，会话总量统计，用于花费追踪）</summary>
    public int CumulativePromptTokens { get; private set; }
    /// <summary>累计 completion tokens（来自 API usage，会话总量统计，用于花费追踪）</summary>
    public int CumulativeCompletionTokens { get; private set; }
    /// <summary>
    /// 最近一次请求的真实 prompt tokens（来自 API usage）。
    /// 代表「当前上下文」的真实大小（每次请求的 prompt 都含完整 system + 工具定义 + 全部历史消息），
    /// 是判断「剩余窗口是否不足」的正确度量 —— 累计用量（Cumulative*）单调递增，不能用于此判断。
    /// </summary>
    public int LastPromptTokens { get; private set; }

    /// <summary>
    /// 固定开销 = 真实 prompt tokens − 估算 tokens。
    /// 覆盖 system prompt + 工具定义 + 消息元数据，用于校准估算消除系统性低估。
    /// </summary>
    private int _overheadTokens;

    /// <summary>上次摘要后是否已注入继续提示</summary>
    public bool ContinuePromptInjected { get; set; }

    /// <summary>压缩进度事件（当前层/总层, 消息, 百分比）— UI 可订阅以显示进度条</summary>
    public static event Action<int, string, double>? CompressProgress;
    /// <summary>压缩结束事件（无论是否实际压缩都触发）— UI 可订阅以隐藏「压缩中」指示</summary>
    public static event Action? CompressFinished;
    /// <summary>是否正在压缩中</summary>
    public static bool IsCompressing { get; private set; }

    public ContextManager(int maxTokens = 128_000)
    {
        // ≤0 视为未设置回退默认窗口，否则 MaxTokens=0 使三层阈值全 0 + ReportProgress 除零得 NaN
        MaxTokens = maxTokens > 0 ? maxTokens : 128_000;
        RecomputeThresholds();
    }

    /// <summary>
    /// 运行时更新上下文窗口上限（切换模型时窗口大小随之变化），并重算三层压缩阈值。
    /// </summary>
    /// <param name="maxTokens">新的窗口上限（token）。≤0 时忽略。</param>
    public void UpdateMaxTokens(int maxTokens)
    {
        if (maxTokens <= 0) return;
        MaxTokens = maxTokens;
        RecomputeThresholds();
    }

    /// <summary>重算三层压缩阈值（省 token 模式取更激进阈值，自动模式按任务轮数复杂度插值）。</summary>
    private void RecomputeThresholds()
    {
        var c = Complexity();
        _snipAt = MaxTokens * ResolveRatio(Config.Instance.ContextSnipRatio, Config.Instance.EconomySnipRatio, c) / 100;
        _summarizeAt = MaxTokens * ResolveRatio(Config.Instance.ContextSummarizeRatio, Config.Instance.EconomySummarizeRatio, c) / 100;
        _collapseAt = MaxTokens * ResolveRatio(Config.Instance.ContextCollapseRatio, Config.Instance.EconomyCollapseRatio, c) / 100;
    }

    /// <summary>当前任务轮数（由 Agent 主循环每轮更新），自动模式据此判断复杂度。</summary>
    private int _currentRound;

    /// <summary>更新任务轮数并重算阈值（简单任务省、复杂任务保质量）。</summary>
    public void SetRound(int round)
    {
        if (round == _currentRound) return;
        _currentRound = round;
        RecomputeThresholds();
    }

    /// <summary>任务复杂度系数 [0,1]：轮数越多越复杂。</summary>
    private double Complexity() =>
        Math.Clamp((double)_currentRound / Config.Instance.EconomyComplexRounds, 0, 1);

    /// <summary>
    /// 三态阈值：Off=正常值；On=省 token 值（二者较小值）；Auto=按复杂度在正常与省 token 之间插值。
    /// internal static 便于自测直接断言。
    /// </summary>
    internal static int ResolveRatio(int normal, int economy, double complexity)
    {
        var target = Math.Min(normal, economy);
        return Config.Instance.EconomyMode switch
        {
            EconomyMode.On => target,
            EconomyMode.Extreme => Math.Max(2000, (int)(target * 0.8)), // 极致：比 economy 再收紧 20%（下限 2000 防过小）
            EconomyMode.Auto => Lerp(normal, target, AutoAggressiveness(complexity)),
            _ => normal,
        };
    }

    /// <summary>
    /// 自动模式收紧系数 [0,1]（0=正常阈值，1=全量省 token），由复杂度 + 优先级决定：
    /// 质量优先=1-c（简单省/复杂保质量）；均衡=1-0.5c；费用优先=恒 1。
    /// </summary>
    internal static double AutoAggressiveness(double complexity) =>
        Config.Instance.EconomyPriority switch
        {
            EconomyPriority.Cost => 1.0,
            EconomyPriority.Balanced => 1.0 - 0.5 * complexity,
            _ => 1.0 - complexity,
        };

    /// <summary>在正常值与省 token 值之间线性插值（a=0→normal，a=1→target）。</summary>
    private static int Lerp(int normal, int target, double a) =>
        (int)Math.Round(normal + (target - normal) * a);

    /// <summary>当前模式下的工具输出裁剪字符阈值（自动模式按复杂度插值）。</summary>
    private int EffectiveSnipChars() =>
        ResolveRatio(Config.Instance.SnipCharsNormal, Config.Instance.EconomySnipChars, Complexity());

    /// <summary>
    /// 从 LLM 响应中累积真实 token 使用量。
    /// estimatedTokens 为同一请求消息列表的估算值（可选），用于校准固定开销。
    /// </summary>
    public void AddUsage(int promptTokens, int completionTokens, int estimatedTokens = 0)
    {
        CumulativePromptTokens += promptTokens;
        CumulativeCompletionTokens += completionTokens;
        // 记录最近一次真实 prompt（覆盖而非累加），代表当前上下文大小
        LastPromptTokens = promptTokens;

        // 用真实 API 报告校准估算：固定开销 = 真实 prompt − 估算（system prompt + 工具定义 + 元数据）。
        // 平滑收敛（移动平均），避免单次波动导致阈值抖动。
        if (estimatedTokens > 0 && promptTokens > 0)
        {
            var overhead = promptTokens - estimatedTokens;
            if (overhead > 0)
                _overheadTokens = _overheadTokens > 0 ? (_overheadTokens + overhead) / 2 : overhead;
        }
    }

    /// <summary>
    /// Crush 风格 StopWhen：检查「当前上下文」的真实 token 用量是否接近窗口上限。
    /// 用最近一次请求的真实 prompt tokens（LastPromptTokens）而非累计用量判断——
    /// 累计用量随轮数单调递增，即使上下文大小不变也会持续增长，用它判断会误触发压缩。
    /// 大窗口用固定 buffer，小窗口用比例阈值。
    /// </summary>
    /// <returns>剩余 token 数低于阈值时应触发摘要</returns>
    public bool ShouldStopAndSummarize()
    {
        var cfg = Config.Instance;
        var used = LastPromptTokens;
        var remaining = MaxTokens - used;

        long threshold;
        if (MaxTokens > cfg.ContextWindowLargeThreshold)
            threshold = cfg.ContextWindowLargeBuffer;       // 大窗口：固定 20K buffer
        else
            threshold = (long)(MaxTokens * cfg.ContextWindowSmallRatio);  // 小窗口：20% 比例

        return remaining <= threshold;
    }

    /// <summary>重置累计 token 计数（摘要后调用）</summary>
    public void ResetUsage()
    {
        CumulativePromptTokens = 0;
        CumulativeCompletionTokens = 0;
        LastPromptTokens = 0;
        ContinuePromptInjected = false;
    }

    /// <summary>
    /// 按需应用压缩层。返回是否发生了压缩。
    /// </summary>
    public async Task<bool> MaybeCompressAsync(List<JNode> messages, LLM? llm,
        Action<int, string>? onProgress = null)
    {
        var current = EstimateCalibratedTokens(messages);
        var compressed = false;
        IsCompressing = true;

        try
        {
            // 第 1 层：裁剪冗长的工具输出
            if (current > _snipAt)
            {
                var beforeSnip = current;
                ReportProgress(1, "裁剪工具输出...", current, onProgress);
                if (SnipToolOutputs(messages, EffectiveSnipChars()))
                {
                    compressed = true;
                    current = EstimateCalibratedTokens(messages);
                    // 上报本轮实际节省量（裁剪前 − 裁剪后），而非裁剪后的剩余容量
                    ReportProgress(1, "裁剪完成", current, onProgress, $"(-{Math.Max(0, beforeSnip - current)})");
                }
            }

            // 第 2 层：LLM 驱动的旧对话摘要
            if (current > _summarizeAt && messages.Count > 20) // 与 SummarizeOldAsync 的 keepRecent=20 对齐，否则 11-20 条时外层进但内层立即 return false
            {
                ReportProgress(2, "正在摘要旧对话...", current, onProgress);
                if (await SummarizeOldAsync(messages, llm))
                {
                    compressed = true;
                    current = EstimateCalibratedTokens(messages);
                    ReportProgress(2, "摘要完成", current, onProgress);
                }
            }

            // 第 3 层：硬折叠——最后手段
            if (current > _collapseAt && messages.Count > 4)
            {
                ReportProgress(3, "紧急压缩...", current, onProgress);
                await HardCollapseAsync(messages, llm);
                compressed = true;
                current = EstimateCalibratedTokens(messages);
                ReportProgress(3, "压缩完成", current, onProgress);
            }
        }
        finally
        {
            IsCompressing = false;
            CompressFinished?.Invoke();
        }

        return compressed;
    }

    /// <summary>生成 8 字符迷你进度条</summary>
    private static string ProgressBar(double percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var filled = (int)(clamped / 100 * 8);
        var empty = 8 - filled;
        return $"«{new string('█', filled)}{new string('░', empty)}» {clamped:F0}%";
    }

    /// <summary>
    /// 向 onProgress 回调与静态 CompressProgress 事件双路报告压缩进度，
    /// 消除 MaybeCompressAsync 三层压缩里重复的「百分比 + 进度条 + 事件」样板。
    /// extra 可选追加到 onProgress 文本（如裁剪完成的「-节省 N」后缀），不影响事件消息。
    /// </summary>
    private void ReportProgress(int layer, string label, int current, Action<int, string>? onProgress, string? extra = null)
    {
        var pct = (double)current / MaxTokens * 100;
        onProgress?.Invoke(layer, $"{label} {ProgressBar(pct)} {current}/{MaxTokens}{(extra != null ? " " + extra : "")}");
        CompressProgress?.Invoke(layer, label, pct);
    }

    /// <summary>
    /// CJK 感知 token 估算。CJK 字符约 1.5 tok/char，ASCII 约 0.25 tok/char（≈4 chars/tok）。
    /// 精度从 ±30% 提升至 ±15%。
    /// </summary>
    public static int EstimateTokens(List<JNode> messages)
    {
        var total = 0;
        foreach (var m in messages)
        {
            if (m["content"]?.AsString() is { } content)
                total += EstimateTokensText(content);
            if (m["tool_calls"] != null)
                total += EstimateTokensText(m["tool_calls"]!.ToJson());
        }
        return total;
    }

    /// <summary>
    /// 经真实 API 用量校准的 token 估算：CJK 感知估算 + 固定开销（system prompt + 工具定义 + 元数据）。
    /// 消除估算对系统提示词 / 工具 schema / 消息元数据的系统性低估，压缩分层判断更准。
    /// 未采集到真实用量时（首轮 / 自测）退化为原始估算。
    /// </summary>
    public int EstimateCalibratedTokens(List<JNode> messages) =>
        EstimateTokens(messages) + Math.Max(0, _overheadTokens);

    /// <summary>对单段文本做 CJK 感知 token 估算。</summary>
    private static int EstimateTokensText(string text)
    {
        int cjk = 0, ascii = 0;
        foreach (var r in text.EnumerateRunes())
        {
            var v = r.Value;
            // CJK 统一汉字 + 扩展区 + 兼容汉字
            if ((v >= 0x4E00 && v <= 0x9FFF) ||
                (v >= 0x3400 && v <= 0x4DBF) ||
                (v >= 0x20000 && v <= 0x2A6DF) ||
                (v >= 0xF900 && v <= 0xFAFF))
                cjk++;
            else if (v <= 127)
                ascii++;
            else
                cjk++; // 其他非 ASCII（日韩、Emoji 等）按宽字符计
        }
        return (int)(cjk * 1.5 + ascii * 0.25);
    }

    /// <summary>
    /// 第 1 层：将超过 4000 字符的工具结果裁剪为首尾几行。
    /// 保留错误行（编译错误、异常堆栈等），确保 Agent 能看到关键诊断信息。
    /// </summary>
    public static bool SnipToolOutputs(List<JNode> messages, int? snipChars = null)
    {
        var effective = snipChars ?? (Config.Instance.EconomyMode == EconomyMode.Extreme
            ? Config.Instance.EconomySnipChars / 2
            : Config.Instance.EconomyMode == EconomyMode.On
            ? Config.Instance.EconomySnipChars : Config.Instance.SnipCharsNormal);
        var changed = false;
        foreach (var m in messages)
        {
            if (m["role"]?.AsString() != "tool") continue;
            var content = m["content"]?.AsString();
            if (string.IsNullOrEmpty(content) || content.Length <= effective) continue;

            var lines = content.Split('\n');
            if (lines.Length <= 10) continue;

            // 提取错误行（编译错误 CS\d+、异常、error/Error/错误 等关键词）
            var errorLines = new HashSet<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("error CS") || line.Contains("Error CS") ||
                    line.Contains(": error ") || line.Contains(": fatal error ") ||
                    line.Contains("Unhandled exception") || line.Contains("Exception:") ||
                    line.Contains("❌") || line.Contains("⛔") ||
                    line.Contains("错误") || line.Contains("严重") ||
                    line.Contains("[stderr]") || line.Contains("[退出码"))
                {
                    // 保留错误行及其上下文（前后各 2 行）
                    for (int j = Math.Max(0, i - 2); j <= Math.Min(lines.Length - 1, i + 2); j++)
                        errorLines.Add(j);
                }
            }

            // 保留前 5 行 + 后 5 行 + 所有错误上下文
            var keepSet = new HashSet<int>();
            for (int i = 0; i < Math.Min(5, lines.Length); i++) keepSet.Add(i);
            for (int i = Math.Max(0, lines.Length - 5); i < lines.Length; i++) keepSet.Add(i);
            foreach (var e in errorLines) keepSet.Add(e);

            var sorted = keepSet.OrderBy(i => i).ToList();

            var sb = new System.Text.StringBuilder();
            // 初值 -1：首个保留索引 idx=0 时条件 0 > 0 为假，不输出虚假的「省略 1 行」
            int lastWritten = -1;
            foreach (var idx in sorted)
            {
                if (idx > lastWritten + 1)
                    sb.AppendLine($"...（省略 {idx - lastWritten - 1} 行）...");
                sb.AppendLine(lines[idx]);
                lastWritten = idx;
            }

            var snipped = sb.ToString().TrimEnd();
            if (errorLines.Count > 0)
                snipped += $"\n\n⚠ 已保留 {errorLines.Count} 处错误上下文。完整输出共 {lines.Length} 行 / {content.Length} 字符。";
            else
                snipped += $"\n\n...（共 {lines.Length} 行 / {content.Length} 字符，已裁剪以节省上下文。使用详细模式查看完整输出）...";

            m.Set("content", snipped);
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// 计算保留尾部应从哪个索引开始。
    /// 确保 tool 消息不会与产生它的 assistant 消息分离。
    /// </summary>
    public static int SafeSplit(List<JNode> messages, int keepRecent)
    {
        var split = Math.Max(0, messages.Count - keepRecent);
        while (split > 0 && messages[split]["role"]?.AsString() == "tool")
            split--;
        return split;
    }

    /// <summary>
    /// 第 2 层：摘要旧对话，保持最近消息不变。
    /// 保留更多最近消息（20 条）以避免丢失关键 API 契约和文件结构信息。
    /// </summary>
    private async Task<bool> SummarizeOldAsync(List<JNode> messages, LLM? llm, int keepRecent = 20)
    {
        if (messages.Count <= keepRecent) return false;

        var split = SafeSplit(messages, keepRecent);
        var old = messages.GetRange(0, split);
        var tail = messages.GetRange(split, messages.Count - split);

        var summary = await GetSummaryAsync(old, llm);

        messages.Clear();
        messages.Add(JNode.Object()
            .Set("role", "user")
            .Set("content", $"[上下文已压缩 - 对话摘要]\n{summary}"));
        messages.Add(JNode.Object()
            .Set("role", "assistant")
            .Set("content", "收到，我已了解之前对话的上下文。"));
        messages.AddRange(tail);
        return true;
    }

    /// <summary>
    /// 第 3 层：紧急压缩。保留更多最近消息（12 条）+ 项目状态快照。
    /// 在硬折叠前注入项目文件清单，防止 Agent 完全失忆。
    /// </summary>
    private async Task HardCollapseAsync(List<JNode> messages, LLM? llm)
    {
        var keep = messages.Count > 12 ? 12 : Math.Min(messages.Count, 6);
        var split = SafeSplit(messages, keep);
        var tail = messages.GetRange(split, messages.Count - split);
        var summary = await GetSummaryAsync(messages.GetRange(0, split), llm);

        // 注入项目状态快照，让 Agent 在硬重置后仍知道项目结构
        var snapshot = GenerateProjectSnapshot();

        messages.Clear();
        messages.Add(JNode.Object()
            .Set("role", "user")
            .Set("content", $"[硬重置上下文 — 项目恢复到关键状态]\n\n## 项目快照\n{snapshot}\n\n## 对话摘要\n{summary}"));
        messages.Add(JNode.Object()
            .Set("role", "assistant")
            .Set("content", "上下文已恢复。我已了解当前项目结构和之前的关键进展。从之前中断的地方继续。"));
        messages.AddRange(tail);
    }

    /// <summary>
    /// 通过 LLM 生成摘要，或回退到提取关键信息。
    /// </summary>
    private async Task<string> GetSummaryAsync(List<JNode> messages, LLM? llm)
    {
        var flat = FlattenMessages(messages);

        if (llm != null)
        {
            try
            {
                var resp = await llm.ChatAsync(
                    messages:
                    [
                        JNode.Object()
                            .Set("role", "system")
                            .Set("content", "你是一个对话压缩器。将以下对话压缩为结构化摘要。" +
                                          "必须保留：\n" +
                                          "1. 所有已创建/修改的文件路径及其用途\n" +
                                          "2. 关键 API 签名（方法名、参数、返回类型）\n" +
                                          "3. 数据模型/类结构（字段、关系）\n" +
                                          "4. 已做出的架构决策和原因\n" +
                                          "5. 遇到的错误及修复方案\n" +
                                          "6. 当前未完成的任务和下一步计划\n" +
                                          "7. 项目的命名空间/包结构\n" +
                                          "丢弃：冗长的命令输出、完整代码清单、" +
                                          "重复的来回对话、中间探索过程。\n" +
                                          "格式：使用 ## 标题分段，列表项用 - 前缀。"),
                        JNode.Object().Set("role", "user").Set("content", TruncateByRunes(flat, 20000)),
                    ]
                );
                return resp.Content;
            }
            catch
            {
                // LLM 摘要失败，回退到提取
            }
        }

        // 注入任务进度追踪（压缩时不丢失进度信息）
        var progress = TaskProgress.GetSummary();
        if (!string.IsNullOrEmpty(progress) && progress != "⏳ 就绪")
        {
            var summary = ExtractKeyInfo(messages);
            return summary + "\n\n## 当前进度\n" + progress;
        }

        // 回退方案：提取文件路径和错误
        return ExtractKeyInfo(messages);
    }

    private static string FlattenMessages(List<JNode> messages)
    {
        var parts = new List<string>();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "?";
            var text = m["content"]?.AsString() ?? "";
            if (!string.IsNullOrEmpty(text))
                parts.Add($"[{role}] {TruncateByRunes(text, 1000)}");
        }
        return string.Join("\n", parts);
    }

    /// <summary>
    /// 按 Unicode 码点（rune）截断文本，避免 UTF-16 码元切片在 emoji/扩展区汉字（代理对）中间切断字符。
    /// 与 <see cref="EstimateTokensText"/> 的 rune 感知一致；text.Length &lt;= maxRunes 时直接返回（无代理对风险）。
    /// </summary>
    internal static string TruncateByRunes(string text, int maxRunes)
    {
        if (maxRunes <= 0) return "";
        if (text.Length <= maxRunes) return text;
        var sb = new System.Text.StringBuilder();
        int n = 0;
        foreach (var r in text.EnumerateRunes())
        {
            if (n >= maxRunes) break;
            sb.Append(r.ToString());
            n++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 取末尾 maxRunes 个 Unicode 码点（不切半代理对），用于「保留头尾」截断的尾部。
    /// text 码点数 &lt;= maxRunes 时原样返回。
    /// </summary>
    internal static string TruncateTailByRunes(string text, int maxRunes)
    {
        if (maxRunes <= 0) return "";
        if (text.Length <= maxRunes) return text;
        var runes = text.EnumerateRunes().ToArray();
        if (runes.Length <= maxRunes) return text;
        var sb = new System.Text.StringBuilder();
        for (int i = runes.Length - maxRunes; i < runes.Length; i++)
            sb.Append(runes[i].ToString());
        return sb.ToString();
    }

    /// <summary>
    /// 生成项目状态快照：扫描工作目录的关键文件结构，
    /// 在硬折叠后注入上下文，防止 Agent 完全失忆。
    /// </summary>
    private static string GenerateProjectSnapshot()
    {
        var parts = new List<string>();
        try
        {
            var cwd = Directory.GetCurrentDirectory();
            var dirName = Path.GetFileName(cwd);

            // 1. 工作目录
            parts.Add($"- 工作目录：{cwd}");

            // 2. 关键配置文件
            var keyFiles = new[] {
                "CLAUDE.md", "README.md", ".gitignore", "package.json",
                "*.csproj", "*.sln", "Cargo.toml", "go.mod", "pyproject.toml",
                "Makefile", "Dockerfile", "docker-compose.yml",
                ".env", ".env.example", "tsconfig.json", "vite.config.*"
            };
            var foundConfigs = new List<string>();
            foreach (var pattern in keyFiles)
            {
                try
                {
                    var matches = System.IO.Directory.GetFiles(cwd, pattern, SearchOption.TopDirectoryOnly);
                    foreach (var m in matches)
                        foundConfigs.Add(Path.GetFileName(m));
                }
                catch { /* ignore */ }
            }
            if (foundConfigs.Count > 0)
                parts.Add($"- 配置文件：{string.Join(", ", foundConfigs.Distinct().Take(10))}");

            // 3. 顶层目录结构
            try
            {
                var topDirs = System.IO.Directory.GetDirectories(cwd)
                    .Select(d => Path.GetFileName(d))
                    .Where(d => !d.StartsWith('.') && d != "node_modules" && d != "obj" && d != "bin")
                    .Take(15)
                    .ToList();
                if (topDirs.Count > 0)
                    parts.Add($"- 顶层目录：{string.Join(", ", topDirs)}");
            }
            catch { /* ignore */ }

            // 4. 最近修改的文件（从 FileTracker 或 git status）
            try
            {
                var gitDir = Path.Combine(cwd, ".git");
                if (System.IO.Directory.Exists(gitDir))
                {
                    parts.Add("- Git 仓库：是");
                }
            }
            catch { /* ignore */ }

            if (parts.Count == 1)
                parts.Add("- （未检测到更多项目结构信息）");
        }
        catch
        {
            return "- 无法生成项目快照";
        }

        return string.Join("\n", parts);
    }

    /// <summary>
    /// 回退方案：无需 LLM，提取文件路径、错误和决策。
    /// 增强版：解析 C# 编译错误码、方法签名、命名空间。
    /// </summary>
    private static string ExtractKeyInfo(List<JNode> messages)
    {
        var filesSeen = new HashSet<string>();
        var errors = new List<string>();
        var namespaces = new HashSet<string>();
        var todos = new List<string>();
        var fileRegex = new Regex(@"(?:^|\s|[/""'`])([\w./\-]+\.\w{1,10})", RegexOptions.None, TimeSpan.FromSeconds(1));
        var nsRegex = new Regex(@"namespace\s+([\w.]+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var csErrorRegex = new Regex(@"CS\d{4}", RegexOptions.None, TimeSpan.FromSeconds(1));
        var funcRegex = new Regex(@"(?:public|private|protected|internal|static|async\s+)?\s*[\w<>[\],]+\s+(\w+)\s*\([^)]*\)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var reqRegex = new Regex(@"(?:需求|Requirement)\s*(\d+)\s*[：:]\s*(.+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var todoRegex = new Regex(@"-\s*\[\s*\]\s+(.+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var todoKwRegex = new Regex(@"(?:TODO|待办|待完成)\s*[：:]\s*(.+)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

        foreach (var m in messages)
        {
            var text = m["content"]?.AsString() ?? "";

            // 提取文件路径
            foreach (Match match in fileRegex.Matches(text))
            {
                var path = match.Groups[1].Value;
                if (path.Contains('.') && path.Length > 3 && path.Length < 200)
                    filesSeen.Add(path);
            }

            // 提取命名空间
            foreach (Match match in nsRegex.Matches(text))
                namespaces.Add(match.Groups[1].Value);

            // 提取 C# 编译错误码
            foreach (Match match in csErrorRegex.Matches(text))
                errors.Add(match.Value);

            // 提取错误行（更高精度：包含文件名+行号+错误码）
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (trimmed.Contains("error CS") || trimmed.Contains(": error ") ||
                    trimmed.Contains(": fatal error ") || trimmed.Contains("Exception") ||
                    trimmed.Contains("❌") || trimmed.Contains("⛔") ||
                    trimmed.Contains("错误") && trimmed.Length > 10)
                {
                    errors.Add(TruncateByRunes(trimmed, 200));
                }
            }

            // 提取需求/todo 条目（压缩保真度：保留"未完成任务清单"）
            foreach (Match match in reqRegex.Matches(text))
                todos.Add($"需求 {match.Groups[1].Value}: {match.Groups[2].Value.Trim()}");
            foreach (Match match in todoRegex.Matches(text))
                todos.Add(match.Groups[1].Value.Trim());
            foreach (Match match in todoKwRegex.Matches(text))
                todos.Add(match.Groups[1].Value.Trim());
        }

        var parts = new List<string>();

        // 项目结构
        if (namespaces.Count > 0)
            parts.Add($"命名空间：{string.Join(", ", namespaces.OrderBy(n => n).Take(10))}");
        if (filesSeen.Count > 0)
            parts.Add($"涉及的文件：{string.Join(", ", filesSeen.OrderBy(f => f).Take(25))}");

        // 错误（去重 + 限制数量）
        var uniqueErrors = errors.Distinct().Take(8).ToList();
        if (uniqueErrors.Count > 0)
            parts.Add($"遇到的错误：{string.Join("；", uniqueErrors)}");

        // 需求/todo 清单（压缩保真度：保留未完成任务）
        var uniqueTodos = todos.Distinct().Take(10).ToList();
        if (uniqueTodos.Count > 0)
            parts.Add($"待完成需求：{string.Join("；", uniqueTodos)}");

        return parts.Count > 0 ? string.Join("\n", parts) : "（无可提取的上下文）";
    }
}
