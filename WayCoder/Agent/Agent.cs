using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// 核心智能体循环。这是 WayCoder 的心脏。
///
/// 模式很简单：
///   用户消息 -> LLM（带工具）-> 有工具调用？-> 执行 -> 循环
///                             -> 文本回复？-> 返回给用户
///
/// 它会持续循环，直到 LLM 回复纯文本（没有工具调用），
/// 这意味着它已完成工作并准备报告结果。
/// </summary>
public partial class Agent
{
    /// <summary>LLM 客户端（大模型做复杂任务，小模型做压缩/摘要）</summary>
    public LLM LlmClient { get; }
    /// <summary>已注册的工具列表</summary>
    public List<ITool> Tools { get; }
    /// <summary>工具名 → 工具实例的快速查找字典</summary>
    public Dictionary<string, ITool> ToolByName { get; }
    /// <summary>对话消息历史（OpenAI 格式）</summary>
    private List<JNode> _messages = [];
    public List<JNode> Messages { get => _messages; set => _messages = value; }

    /// <summary>
    /// 保护 Messages 的锁：Agent 主循环线程（写）与 Web 序列化/退出保存线程（读）并发访问。
    /// List&lt;JNode&gt; 非线程安全，外部快照/遍历时与主循环追加并发会抛 InvalidOperationException。
    /// </summary>
    internal readonly object MessagesLock = new();

    /// <summary>追加一条消息（线程安全）</summary>
    internal void AddMessage(JNode msg) { lock (MessagesLock) _messages.Add(msg); }
    /// <summary>移除指定索引消息（线程安全）</summary>
    internal void RemoveMessageAt(int idx) { lock (MessagesLock) _messages.RemoveAt(idx); }
    /// <summary>在指定索引插入消息（线程安全）</summary>
    internal void InsertMessage(int idx, JNode msg) { lock (MessagesLock) _messages.Insert(idx, msg); }
    /// <summary>快照当前消息（线程安全，供外部只读遍历）</summary>
    internal List<JNode> SnapshotMessages() { lock (MessagesLock) return _messages.ToList(); }
    /// <summary>整体替换消息（线程安全，供恢复会话）</summary>
    internal void ReplaceMessages(IEnumerable<JNode> msgs) { lock (MessagesLock) { _messages.Clear(); _messages.AddRange(msgs); } }
    /// <summary>清空消息（线程安全）</summary>
    internal void ClearMessages() { lock (MessagesLock) _messages.Clear(); }

    /// <summary>上下文管理器（三层压缩 + token 预算）</summary>
    public ContextManager Context { get; }

    private readonly int _maxRounds;
    private readonly double? _maxBudgetUsd;
    private readonly string _systemPrompt;

    private bool _autoCommit;

    /// <summary>是否启用快速模式（跳过探索，直接执行）</summary>
    private bool _fastMode;

    /// <summary>连续纯文本轮次计数（用于渐进式催促）</summary>
    private int _analysisOnlyStreak;
    private int _talksCodeStreak; // 模型口述代码而非写入文件的连续次数

    /// <summary>本轮修改过的文件（用于精准 git add，每轮 AutoCommit 后清空）</summary>
    private readonly HashSet<string> _modifiedFiles = [];

    /// <summary>会话中所有修改过的文件（不清空，用于 ContinuePrompt 等需要全量清单的场景）</summary>
    private readonly HashSet<string> _allSessionFiles = [];

    /// <summary>SHA256 循环检测：最近几轮的哈希值（检测 Agent 是否陷入重复操作循环）</summary>
    private readonly List<string> _recentActionHashes = [];
    private const int PerToolLoopWindow = 10;
    private const int PerToolLoopThreshold = 5;
    private int _loopNudgeCount;

    /// <summary>本轮对话开始时间（供 WorkReporter 计算耗时）</summary>
    private DateTime _chatStartedAt;

    /// <summary>运行轨迹记录器（对标 OpenClaw trajectory，null=已关闭/未启用）</summary>
    private Trajectory? _trajectory;

    /// <summary>Architect 双模型模式：大模型出计划，小模型执行</summary>
    public bool ArchitectMode { get; set; }

    /// <summary>
    /// 该 Agent 实例的工作模式（Build/Plan/Review/Auto），默认 Build。
    /// 与全局 <see cref="WorkModeManager.CurrentMode"/> 解耦——每个槽位 Agent 持有自己的模式，
    /// 后台槽位并行时不再读到活跃槽位的模式（修复混合模式并行污染）。
    /// </summary>
    public WorkMode WorkMode { get; set; } = WorkMode.Build;

    /// <summary>
    /// 本 Agent 的唯一标识（默认 "main"）。槽位 Agent 设为 F1-F10、子智能体继承父标识，
    /// 供文件锁等跨 Agent 资源冲突检测与报错提示（WriteFile/EditFile 等工具经 _agent_id 读取）。
    /// </summary>
    public string AgentId { get; set; } = "main";

    /// <summary>
    /// 工作模式变化回调（由 Program.cs 在绑定槽位时接线，携带槽位索引）。
    /// Agent 内部切换模式（如计划审批门批准后自动切回建造模式）时调用，
    /// 使正确槽位的持久模式与状态栏同步，而非依赖全局 ModeChanged 事件污染活跃槽位。
    /// </summary>
    public Action<WorkMode>? OnWorkModeChanged;

    /// <summary>
    /// 优雅暂停请求标志（volatile：主线程写、Agent 循环线程读）。
    /// 置位后，Agent 在当前批次完成后的下一轮边界停机——提交进度 + 写检查点 + 存会话，
    /// 而非像 Esc 那样立即硬砍。
    /// </summary>
    public volatile bool PauseRequested;

    /// <summary>最大对话轮次（-1 表示从 Config.Instance.MaxRounds 读取）</summary>
    private readonly int _effectiveMaxRounds;

    /// <summary>是否启用自动 Git Commit（可运行时切换）</summary>
    public bool AutoCommitEnabled
    {
        get => _autoCommit;
        set => _autoCommit = value;
    }

    /// <summary>
    /// 创建 Agent 实例。
    /// </summary>
    /// <param name="llm">LLM 客户端</param>
    /// <param name="tools">工具列表（默认使用 ToolRegistry.AllTools）</param>
    /// <param name="maxContextTokens">上下文窗口上限</param>
    /// <param name="maxRounds">最大对话轮次</param>
    /// <param name="maxBudgetUsd">最大美元预算（null=无限制）</param>
    /// <param name="autoCommit">工具执行后自动 git commit</param>
    public Agent(LLM llm, List<ITool>? tools = null,
        int maxContextTokens = 128_000, int maxRounds = -1,
        double? maxBudgetUsd = null, bool autoCommit = false)
    {
        LlmClient = llm;
        _maxBudgetUsd = maxBudgetUsd;
        _autoCommit = autoCommit;
        Tools = tools ?? ToolRegistry.AllTools;

        // 工具白名单/黑名单过滤
        Tools = FilterTools(Tools);

        // 每个 Agent 持有独立的 AgentTool 实例：共享单例会让 ParentAgent 被后构造的
        // 子智能体覆写（AgentId 继承失效、花费归并到错误实例、跨槽位重绑竞态）。
        for (var i = 0; i < Tools.Count; i++)
        {
            if (Tools[i] is AgentTool)
                Tools[i] = new AgentTool();
        }

        ToolByName = Tools.ToDictionary(t => t.Name);
        Context = new ContextManager(maxContextTokens);
        _maxRounds = maxRounds;
        _effectiveMaxRounds = maxRounds > 0 ? maxRounds : Math.Max(1, Config.Instance.MaxRounds); // 下限 1，防 MaxRounds≤0 时循环体一次都不跑
        _systemPrompt = SystemPrompt.Generate(Tools);

        // 连接子智能体能力
        foreach (var t in Tools)
        {
            if (t is AgentTool agentTool)
                agentTool.ParentAgent = this;
        }
    }

    /// <summary>
    /// 运行时更新上下文窗口上限（切换模型时窗口大小随模型变化）。
    /// 转发给 ContextManager 重算三层压缩阈值。
    /// </summary>
    /// <param name="maxTokens">新的窗口上限（token）。</param>
    public void UpdateContextWindow(int maxTokens) => Context.UpdateMaxTokens(maxTokens);

    /// <summary>
    /// 构建完整消息列表（包含系统提示词 + 模式提示）。
    /// 发送前自动修复孤立的工具调用/结果配对（对标 Crush filterOrphanedToolResults + syntheticToolResultsForOrphanedCalls）。
    /// </summary>
    private List<JNode> FullMessages()
    {
        // 修复孤立的 tool-call/tool-result 配对（防止中断后对话损坏）
        RepairOrphanedToolPairs();

        // 模式专用提示（Plan/Review/Auto 模式会在主提示词前注入约束）
        var modePrompt = WorkModeManager.GetModePrompt(WorkMode);
        var systemContent = string.IsNullOrEmpty(modePrompt)
            ? _systemPrompt
            : modePrompt + "\n" + _systemPrompt;

        // 快速模式：替换工作流和规则 1 为直接执行版本
        if (_fastMode)
        {
            systemContent = systemContent
                .Replace(SystemPrompt.StandardWorkflow, SystemPrompt.FastModeWorkflow)
                .Replace(SystemPrompt.StandardRule1, SystemPrompt.FastModeRule1);
        }

        var result = new List<JNode>
        {
            JNode.Object().Set("role", "system").Set("content", systemContent),
        };
        // 深克隆消息，避免 JsonNode 的 Parent 冲突（同一消息不能加入两个树）
        foreach (var m in Messages)
            result.Add(Json.Parse(m.ToJson())!);

        // 视觉支持：view_image 工具附加的图片注入为多模态 user 消息（仅支持 vision 的模型）
        if (LLM.ModelSupportsVision(LlmClient.EffectiveModel))
        {
            var images = LLM.DrainImages();
            if (images.Count > 0)
                result.Add(LLM.BuildImageMessage("请查看以上图片，回答我的问题。", images));
        }
        else
        {
            // 当前模型不支持 vision：清空积压的图片（否则非 vision 槽位 view_image 后图片
            // 永久积压，泄漏给之后其他槽位的 vision 请求造成跨槽位串扰）
            LLM.DrainImages();
        }

        return result;
    }

    /// <summary>
    /// 获取工具 schema 列表。
    /// </summary>
    private List<JNode> ToolSchemas() => Tools.Select(t => t.Schema()).ToList();

    /// <summary>
    /// 处理一条用户消息，执行 Agent 主循环（多轮 LLM/工具交互直到完成或超限）。
    /// </summary>
    /// <param name="userInput">用户输入文本</param>
    /// <param name="onToken">流式 token 回调</param>
    /// <param name="onTool">工具调用回调（工具名, 结果摘要）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Agent 最终回复文本</returns>
    public async Task<string> ChatAsync(
        string userInput,
        Action<string>? onToken = null,
        Action<string, string>? onTool = null,
        Action<string>? onToolOutput = null,
        CancellationToken cancellationToken = default)
    {
        _chatStartedAt = DateTime.UtcNow;

        // 运行轨迹（对标 OpenClaw trajectory）：记录每轮 LLM 与每个工具调用的过程性元数据
        var trajectory = Trajectory.Create(LlmClient.EffectiveModel);
        _trajectory = trajectory;
        int startMessageCount = Messages.Count;
        try
        {
            return await ChatAsyncCore(userInput, onToken, onTool, onToolOutput, cancellationToken);
        }
        catch
        {
            // 模型调用失败（超时/HTTP 错误）时回滚本轮追加的消息：
            // REPL 回退链用同一 agent 重试，否则用户消息重复入史，任务被重复执行（二次写文件/跑命令）
            while (Messages.Count > startMessageCount)
                RemoveMessageAt(Messages.Count - 1);
            throw;
        }
        finally
        {
            // 无论正常完成/异常/取消，都落 run_end 汇总（累计轮次与 token）
            trajectory?.End();
            _trajectory = null;
        }
    }

    /// <summary>ChatAsync 的核心主循环（被 ChatAsync 包裹以统一落轨迹 run_end）。</summary>
    private async Task<string> ChatAsyncCore(
        string userInput,
        Action<string>? onToken,
        Action<string, string>? onTool,
        Action<string>? onToolOutput,
        CancellationToken cancellationToken)
    {
        // 复位上一轮 Architect 模式的模型覆盖：ModelOverride 在本方法末尾裸赋值成小模型，
        // 若不在此复位，第二轮起 EffectiveModel 恒为小模型、计划生成分支（下方 286 行）不再触发。
        LlmClient.ModelOverride = null;

        // 检测快速模式：用户明确要求跳过探索（不要读文件/不要ls/不要规划等）。
        // 直接赋值（而非 |= true）：快速模式只影响当轮，不能污染后续消息。
        _fastMode = SystemPrompt.DetectFastMode(userInput);
        if (_fastMode)
            DebugLog.Log("agent", "检测到快速模式关键词，跳过探索工作流");

        AddMessage(JNode.Object().Set("role", "user").Set("content", userInput));
        await CompressWithSmallModel(onToken);

        // ── Architect 模式：大模型出计划 → 小模型执行 ──
        if (ArchitectMode && LlmClient.EffectiveModel != LlmClient.SmallModel)
        {
            var plan = await GenerateArchitectPlanAsync(onToken, cancellationToken);
            if (plan == null)
            {
                RemoveMessageAt(Messages.Count - 1); // 回滚用户消息
                return "⚠ Architect 模式：大模型计划生成失败，已取消。";
            }
            // 将计划作为 system 消息注入，小模型继续执行
            AddMessage(JNode.Object()
                .Set("role", "system")
                .Set("content", $"## 执行计划\n\n以下是 Architect 的分析和执行计划，请按步骤逐一执行：\n\n{plan}"));
            // 切换回小模型执行
            LlmClient.ModelOverride = LlmClient.SmallModel;
            onToken?.Invoke("\n📋 **计划已生成，切换到小模型执行...**\n\n");
        }

        int requeueCount = 0;
    Requeue:
        for (int round = 0; round < _effectiveMaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // ── 优雅暂停（Ctrl+Z）──
            // 收到暂停请求后，在当前批次完成后的下一轮边界停机：提交进度 + 写检查点 + 存会话。
            if (PauseRequested)
            {
                PauseRequested = false;
                return await GracefulPauseAsync(onToken);
            }

            // 预算检查：超过上限则停止
            if (_maxBudgetUsd != null)
            {
                var spent = LlmClient.EstimatedCost ?? 0;
                if (spent >= _maxBudgetUsd.Value)
                    return $"🛑 已达到预算上限 ${_maxBudgetUsd:F2}（已花费 ${spent:F4}，{round} 轮）。增加预算请使用 --max-budget-usd。";
            }

            var resp = await LlmClient.ChatAsync(
                messages: FullMessages(),
                tools: ToolSchemas(),
                onToken: onToken,
                cancellationToken: cancellationToken);

            // 累积真实 token 使用量（Crush 风格），并用同请求消息估算值校准固定开销
            Context.AddUsage(resp.PromptTokens, resp.CompletionTokens,
                ContextManager.EstimateTokens(Messages));
            // 自动省 token 模式：按任务轮数更新复杂度，动态调节压缩阈值
            Context.SetRound(round);
            // 轨迹：记录本轮 LLM 交互（token 消耗 + 输出形态）
            _trajectory?.RecordTurn(round, resp.PromptTokens, resp.CompletionTokens,
                resp.Content?.Length ?? 0, resp.ToolCalls.Count, resp.ReasoningTokens);

            // 没有工具调用 -> LLM 完成，返回文本
            if (resp.ToolCalls.Count == 0)
            {
                // 致命错误（如所有模型失败）：保存会话后退出
                if (resp.IsFatalError)
                {
                    try { SessionManager.SaveSession(Messages, LlmClient.EffectiveModel); } catch { }
                    return resp.Content ?? "[致命错误] 所有模型失败，会话已保存。";
                }

                AddMessage(resp.ToMessage());
                // 自动继续检测：
                // 0. 模型输出大量推理内容但不产生实际输出（DeepSeek V4 等模型的常见问题）
                // 1. 模型首轮只输出分析不调用工具（toolCallCount==0, content>100）
                // 2. 模型用了一些工具后开始"口述代码"而非写入文件（content 包含代码特征 >300 字符）
                var toolCallCount = Messages.Count(m => m["role"]?.AsString() == "tool");
                var contentLen = resp.Content?.Length ?? 0;
                var reasoningLen = resp.ReasoningTokens;
                var hasCodeContent = contentLen > 300 &&
                    (resp.Content!.Contains("```") || resp.Content.Contains("class ") ||
                     resp.Content.Contains("public ") || resp.Content.Contains("def ") ||
                     resp.Content.Contains("func ") || resp.Content.Contains("function "));
                // 明确完成信号：模型主动宣告任务完成（否则"无工具调用"一律视为中途停滞）
                var hasCompletionSignal = resp.Content != null &&
                    (resp.Content.Contains("✅") || resp.Content.Contains("任务完成") ||
                     resp.Content.Contains("已完成") || resp.Content.Contains("全部完成"));

                // ── 计划审批门（Plan 模式）──
                // Plan 模式下模型产出计划（文本、无工具调用）后，不再自动催促执行，
                // 而是就地弹出审批框：批准 → 切回建造模式继续执行；拒绝 → 停止。
                if (ShouldPromptPlanApproval(WorkMode, contentLen))
                {
                    if (PromptPlanApproval(resp.Content!))
                    {
                        WorkMode = WorkMode.Build;
                        OnWorkModeChanged?.Invoke(WorkMode.Build);
                        AddMessage(JNode.Object()
                            .Set("role", "user")
                            .Set("content", "✅ 计划已获批准。现在切换到建造模式，按上述计划逐步执行，完成后汇报结果。"));
                        _analysisOnlyStreak = 0;
                        _talksCodeStreak = 0;
                        continue;
                    }
                    SaveWorkReport();
                    return resp.Content ?? "";
                }

                // 检测 0：DeepSeek V4 等模型将大量输出花在推理（reasoning）上而不产生实际内容
                // reasoning 被显示但不计入 Content，所以 contentLen 可能极短
                if (reasoningLen > 300 && toolCallCount == 0 && contentLen < 80)
                {
                    _analysisOnlyStreak++;
                    string nudge = _analysisOnlyStreak switch
                    {
                        1 => $"你的推理思考已消耗 {reasoningLen} 字符，但没有产生任何工具调用。请立即调用 write_file 或 bash 工具执行任务，不要再进行冗长的内部推理。",
                        2 => $"你已连续 {_analysisOnlyStreak} 轮只输出推理而不调用工具（本轮推理 {reasoningLen} 字符）。请立即调用工具——不要只思考不行动。",
                        _ => $"⚠️ 严重警告：连续 {_analysisOnlyStreak} 轮纯推理无行动（累计数万字推理内容）。立即停止思考，只输出工具调用。",
                    };
                    AddMessage(JNode.Object()
                        .Set("role", "user")
                        .Set("content", nudge));
                    continue;
                }

                if (toolCallCount == 0 && contentLen > 100)
                {
                    // 首轮分析但未行动 — 渐进式催促（逐次加强）
                    _analysisOnlyStreak++;
                    string nudge = _analysisOnlyStreak switch
                    {
                        1 => "请立即用工具执行上述计划。直接调用 write_file/edit_file/bash 等工具，不要再输出分析。",
                        2 => "你已连续两轮只输出分析不调用工具。请立即行动——调用 write_file 或 bash 执行具体操作。不要再做任何分析。",
                        _ => "⚠️ 严重警告：你已连续多轮不调用工具，浪费了大量上下文。立即调用工具执行任务，不要输出任何文字——只输出工具调用。",
                    };
                    AddMessage(JNode.Object()
                        .Set("role", "user")
                        .Set("content", nudge));
                    continue;
                }

                if (hasCodeContent && !resp.Content!.Contains("✅"))
                {
                    // 模型在"口述"代码而非写入文件 — 渐进式追问使其用工具
                    _talksCodeStreak++;
                    string nudge = _talksCodeStreak switch
                    {
                        1 => "不要用文字输出代码。立即调用 write_file 工具将上述代码写入文件。",
                        2 => "你已连续两次只输出代码文字而不调用 write_file。请立即使用 write_file 工具将代码写入磁盘。不要再输出代码文字。",
                        _ => "⚠️ 严重警告：你已连续多次在文字中输出代码而不使用工具。代码必须通过 write_file 写入文件——请立即调用 write_file，不要输出任何文字。",
                    };
                    AddMessage(JNode.Object()
                        .Set("role", "user")
                        .Set("content", nudge));
                    continue;
                }

                // 检测 3：任务进行中（已有工具调用历史）但本轮无工具调用且无明确完成信号
                // （推理型模型长链思考后流被截断、只读不写后输出"计划"、或"只思考不行动"的中途停滞）
                // → 催其继续而非误判完成退出。真正完成须带 ✅/完成 等信号，否则一律续跑。
                if (toolCallCount > 0 && !hasCompletionSignal)
                {
                    _analysisOnlyStreak++;
                    string nudge = _analysisOnlyStreak switch
                    {
                        1 => "本轮没有调用任何工具——任务尚未完成。请立即调用 write_file/bash 等工具继续执行下一步，不要停在计划或分析阶段。",
                        2 => "你已连续两轮没有调用工具。任务未完成，请立即调用工具继续执行，不要只输出文字。",
                        _ => "⚠️ 严重警告：连续多轮无工具调用，任务仍未完成。立即调用工具继续，直到真正完成并明确汇报结果。",
                    };
                    AddMessage(JNode.Object()
                        .Set("role", "user")
                        .Set("content", nudge));
                    continue;
                }

                SaveWorkReport();
                return resp.Content ?? "";
            }

            // 有工具调用 -> 执行（多个时并行）
            _analysisOnlyStreak = 0; // 重置分析-不动手计数器
            _talksCodeStreak = 0;    // 重置口述代码计数器
            AddMessage(resp.ToMessage());

            try
            {
                // 执行本轮工具调用：单工具流式，多工具按 ExecutionMode 分批（批内并行 + Exclusive 独占）
                await ExecuteToolCallsAsync(resp.ToolCalls, onTool, onToolOutput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ctrl+C 中断执行，回填缺失的工具回复
                AnswerPendingToolCalls(resp.ToolCalls);
                throw;
            }

            // ── SHA256 循环检测（Crush 风格）──
            // 对最近几轮的（assistant 内容 + 工具结果）做哈希，
            // 相同哈希重复出现 3+ 次说明 Agent 陷入循环，注入反循环提示。
            DetectAndBreakLoop(resp, Messages);

            // ── Stale-Read 文件变更检测（Crush 风格）──
            // bash 等外部命令可能修改 Agent 已读取的文件，
            // 检测到变更时注入警告让 LLM 重新读取过期文件。
            var changeWarning = FileTracker.GetChangeWarning();
            if (changeWarning != null)
            {
                AddMessage(JNode.Object()
                    .Set("role", "tool")
                    .Set("tool_call_id", "file_tracker")
                    .Set("content", changeWarning));
                DebugLog.Log("file-tracker", $"文件变更警告已注入");
            }

            // 自动 git commit（如果启用）
            if (_autoCommit) await AutoCommitAsync();

            // ── Crush 风格上下文预算检查 ──
            // 基于真实 API token 使用量，当剩余窗口低于阈值时提前触发摘要
            if (Config.Instance.AutoContinueAfterSummarize
                && Context.ShouldStopAndSummarize()
                && Messages.Count > 8)
            {
                var beforeCount = Messages.Count;
                var beforeTokens = ContextManager.EstimateTokens(Messages);
                // 触发依据是最近一次真实 prompt（当前上下文大小），而非累计用量——显示两者避免误判窗口未生效
                var usedTokens = Context.LastPromptTokens;
                onToken?.Invoke($"\n⏳ **上下文压缩中... (当前上下文 {usedTokens}/{Context.MaxTokens} tokens，消息估算 {beforeTokens})**\n\n");
                await CompressWithSmallModel(onToken);
                var afterCount = Messages.Count;
                var afterTokens = ContextManager.EstimateTokens(Messages);
                DebugLog.Log("context", $"Crush-style auto-summarize: {beforeCount}→{afterCount} msgs, {beforeTokens}→{afterTokens} est.tokens, last prompt={usedTokens}/{Context.MaxTokens}");

                // 如果 Agent 正在执行任务中（有工具调用历史），注入继续提示
                if (!Context.ContinuePromptInjected && afterCount < beforeCount)
                {
                    InjectContinuePrompt("之前的会话因上下文过长而被压缩");
                    onToken?.Invoke("\n🔄 **上下文已自动压缩，继续执行...**\n\n");
                }
            }

            // 如果工具输出太大则压缩上下文
            await CompressWithSmallModel(onToken);

            // ── Stop hook（每轮完成后触发）──
            var stopContext = await HooksManager.RunStopAsync();
            if (stopContext != null)
            {
                AddMessage(JNode.Object()
                    .Set("role", "user")
                    .Set("content", stopContext));
            }
        }

        SaveWorkReport();

        // 检测任务是否可能仍在进行中（最近 5 轮有 write_file/edit_file 调用）
        var recentTools = Messages.TakeLast(10)
            .Where(m => m["role"]?.AsString() == "tool")
            .Select(m => m["content"]?.AsString() ?? "")
            .ToList();
        // 匹配真实工具输出：write_file 返回「已写入 N 行到 …」，edit_file 返回「已编辑 …」，
        // multiedit 返回「✅ 已创建 … / ✅ 已编辑 …」。（旧标记「✅ 已写入/✅ 编辑完成」已无任何工具产出，导致自动续跑永远不触发。）
        var wasWriting = recentTools.Any(c =>
            c.Contains("已写入") || c.Contains("已编辑") || c.Contains("已创建"));
        var lastMsg = Messages.LastOrDefault(m => m["role"]?.AsString() == "assistant")
            ?["content"]?.AsString() ?? "";

        // 自动续跑：仍在写文件（任务未完成）且未超续跑上限 → 压缩 + 注入继续提示后重新跑
        if (wasWriting && requeueCount < Config.Instance.MaxAutoRequeue)
        {
            requeueCount++;
            onToken?.Invoke($"\n🔁 **已达到 {_effectiveMaxRounds} 轮上限，自动续跑（第 {requeueCount}/{Config.Instance.MaxAutoRequeue} 次）...**\n\n");
            await CompressWithSmallModel(onToken);
            InjectContinuePrompt($"已达到 {_effectiveMaxRounds} 轮工具调用上限，自动续跑");
            goto Requeue;
        }

        if (wasWriting)
        {
            return $"（已达到 {_effectiveMaxRounds} 轮工具调用上限 — ⚠ 任务可能未完成，最近仍在写文件。输入「继续」以恢复。）";
        }
        if (lastMsg.Length > 200)
        {
            return $"（已达到 {_effectiveMaxRounds} 轮工具调用上限 — 输入「继续」以从中断处恢复。）";
        }
        return $"（已达到 {_effectiveMaxRounds} 轮工具调用上限）";
    }

    /// <summary>
    /// 保存工作总结报告到 .waycoder/reports/latest.md。
    /// </summary>
    private void SaveWorkReport()
    {
        try
        {
            var report = WorkReporter.Generate(Messages, _chatStartedAt);
            var dir = Path.Combine(Environment.CurrentDirectory, ".waycoder", "reports");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "latest.md");
            File.WriteAllText(path, report);
        }
        catch { /* 报告生成失败不影响主流程 */ }
    }

    /// <summary>
    /// 优雅暂停：提交本轮进度 → 写检查点 → 保存会话，然后返回暂停提示。
    /// 与 Esc 硬中断的区别：不丢当前批次的工作，恢复后可从提交点继续。
    /// </summary>
    private async Task<string> GracefulPauseAsync(Action<string>? onToken)
    {
        onToken?.Invoke("\n⏸ **优雅暂停：提交进度 + 保存状态…**\n");

        // 1. 提交 Agent 本轮修改的文件。仅提交 Agent 自己改过的文件（不做 git-status 全量兜底），
        //    避免把用户与本任务无关的未提交改动一并卷进去。
        try
        {
            await AutoCommitAsync(fallbackToGitStatus: false);
        }
        catch (Exception ex) { DebugLog.Log("pause", $"暂停提交失败: {ex.Message}"); }

        // 2. 写检查点（未提交的残留改动可回滚；工作区干净则为空标记）
        try
        {
            var cp = await CheckpointManager.CreateAsync("pause 优雅暂停");
            if (cp != null)
                onToken?.Invoke($"  📦 检查点 #{cp.Id} 已创建\n");
        }
        catch (Exception ex) { DebugLog.Log("pause", $"暂停检查点失败: {ex.Message}"); }

        // 3. 保存会话到 _auto，下次启动 TryRestoreSession 可用 /resume 恢复
        try
        {
            SessionManager.SaveSession(Messages, LlmClient.EffectiveModel, "_auto");
        }
        catch (Exception ex) { DebugLog.Log("pause", $"暂停保存会话失败: {ex.Message}"); }

        var summary = "\n⏸ **已暂停**：当前批次已完成并提交，检查点与会话均已保存。\n输入「继续」或重启后 /resume 从提交点恢复。";
        onToken?.Invoke(summary);
        return summary;
    }

    /// <summary>使用小模型执行上下文压缩（省钱）</summary>
    private async Task CompressWithSmallModel(Action<string>? onProgress = null)
    {
        // PreCompact hook
        var preCompactCtx = await HooksManager.RunPreCompactAsync(
            $"est.tokens={Context.EstimateCalibratedTokens(Messages)}/{Context.MaxTokens}");

        await WithModelOverrideAsync(LlmClient, LlmClient.SmallModel, () =>
            Context.MaybeCompressAsync(Messages, LlmClient,
                onProgress: (layer, msg) => onProgress?.Invoke($"🔄 [{layer}/3] {msg}")));

        // 注入 PreCompact 返回的额外上下文
        if (preCompactCtx != null)
        {
            AddMessage(JNode.Object()
                .Set("role", "user")
                .Set("content", preCompactCtx));
        }
    }

    /// <summary>
    /// 注入"继续"提示：原始用户请求 + 已完成文件清单，
    /// 让 Agent 在上下文压缩或撞轮次上限后续跑时继续完成未完成工作。
    /// </summary>
    private void InjectContinuePrompt(string reason)
    {
        Context.ContinuePromptInjected = true;

        var originalUserMsg = Messages.FirstOrDefault(m =>
            m["role"]?.AsString() == "user")?["content"]?.AsString() ?? "";
        if (originalUserMsg.Length > 200)
            originalUserMsg = ContextManager.TruncateByRunes(originalUserMsg, 200) + "...";

        // 收集已创建/修改的文件清单（从 _allSessionFiles，比文本解析更准确）
        string[] fileArray;
        lock (_allSessionFiles)
            fileArray = _allSessionFiles.ToArray();
        var fileListStr = fileArray.Length > 0
            ? "\n\n已确认创建/修改的文件（" + fileArray.Length + " 个）：\n" + string.Join("\n", fileArray.Take(20).Select(f => $"  - {f}"))
                + (fileArray.Length > 20 ? $"\n  ...（共 {fileArray.Length} 个）" : "")
            : "";

        AddMessage(JNode.Object()
            .Set("role", "user")
            .Set("content", $"{reason}。原始用户请求是：`{originalUserMsg}`\n请从中断处继续，完成未完成的工作。不要重写或缩小已有文件——只创建新文件或向已有文件追加缺失内容。{fileListStr}"));
        Context.ResetUsage(); // 重置计数器，给新一轮足够的空间
    }

    /// <summary>
    /// 清空对话历史。
    /// </summary>
    /// <summary>清空对话历史，重置 Agent 状态。</summary>
    public void Reset() => ClearMessages();

    private static string FormatBrief(Dictionary<string, object?> args, int maxLen = 80)
    {
        var s = string.Join(", ", args.Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
        return s.Length > maxLen ? ContextManager.TruncateByRunes(s, maxLen) + "..." : s;
    }

    private static string FormatValue(object? value)
    {
        // 集合/字典/JsonNode 用递归序列化显示（而非 ToString 泄漏 System.Collections...）
        var s = value switch
        {
            null => "null",
            string str => str,
            System.Collections.IEnumerable or System.Collections.IDictionary or JNode => JsonHelper.SerializeValue(value),
            _ => value.ToString() ?? "null",
        };
        return s.Length > 40 ? ContextManager.TruncateByRunes(s, 40) + "..." : s;
    }
}
