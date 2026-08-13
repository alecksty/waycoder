using System.Text.RegularExpressions;
using WayCoder.Tools;

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
public class Agent
{
    /// <summary>LLM 客户端（大模型做复杂任务，小模型做压缩/摘要）</summary>
    public LLM LlmClient { get; }
    /// <summary>已注册的工具列表</summary>
    public List<ITool> Tools { get; }
    /// <summary>工具名 → 工具实例的快速查找字典</summary>
    public Dictionary<string, ITool> ToolByName { get; }
    /// <summary>对话消息历史（OpenAI 格式）</summary>
    public List<JsonObject> Messages { get; set; } = [];
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

    /// <summary>Architect 双模型模式：大模型出计划，小模型执行</summary>
    public bool ArchitectMode { get; set; }

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

        ToolByName = Tools.ToDictionary(t => t.Name);
        Context = new ContextManager(maxContextTokens);
        _maxRounds = maxRounds;
        _effectiveMaxRounds = maxRounds > 0 ? maxRounds : Config.Instance.MaxRounds;
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
    private List<JsonObject> FullMessages()
    {
        // 修复孤立的 tool-call/tool-result 配对（防止中断后对话损坏）
        RepairOrphanedToolPairs();

        // 模式专用提示（Plan/Review/Auto 模式会在主提示词前注入约束）
        var modePrompt = WorkModeManager.GetModePrompt(WorkModeManager.CurrentMode);
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

        var result = new List<JsonObject>
        {
            new() { ["role"] = "system", ["content"] = systemContent },
        };
        // 深克隆消息，避免 JsonNode 的 Parent 冲突（同一消息不能加入两个树）
        foreach (var m in Messages)
            result.Add(JsonNode.Parse(m.ToJsonString())!.AsObject());
        return result;
    }

    /// <summary>
    /// 修复孤立的工具调用/结果配对（对标 Crush filterOrphanedToolResults + syntheticToolResultsForOrphanedCalls）。
    ///
    /// 两种孤例：
    /// 1. 有 tool-call 但无对应 tool-result → 注入合成错误结果，防止下轮 API 拒绝请求
    /// 2. 有 tool-result 但无对应 tool-call → 删除该结果，避免污染上下文
    ///
    /// 场景：Agent 中断（Ctrl+C）、会话恢复、LLM 输出截断导致 tool-call 不完整。
    /// </summary>
    private void RepairOrphanedToolPairs()
    {
        // 1. 收集所有 assistant 消息中的 tool_call ID
        var callIds = new HashSet<string>();
        foreach (var msg in Messages)
        {
            if (msg["role"]?.GetValue<string>() != "assistant") continue;
            var toolCalls = msg["tool_calls"]?.AsArray();
            if (toolCalls == null) continue;
            foreach (var tc in toolCalls)
            {
                var id = tc?["id"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(id))
                    callIds.Add(id);
            }
        }

        if (callIds.Count == 0) return; // 无工具调用，无需修复

        // 2. 收集所有 tool 结果消息的 tool_call_id
        var resultIds = new HashSet<string>();
        foreach (var msg in Messages)
        {
            if (msg["role"]?.GetValue<string>() != "tool") continue;
            var id = msg["tool_call_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id))
                resultIds.Add(id);
        }

        // 3. 对无结果的 tool-call 注入合成错误结果
        var orphanCalls = callIds.Except(resultIds).ToList();
        foreach (var orphanId in orphanCalls)
        {
            // 找到该 tool-call 的 assistant 消息位置，在其后插入合成 tool-result
            int callMsgIdx = -1;
            string? toolName = null;
            for (int i = 0; i < Messages.Count; i++)
            {
                var msg = Messages[i];
                if (msg["role"]?.GetValue<string>() != "assistant") continue;
                var tcs = msg["tool_calls"]?.AsArray();
                if (tcs == null) continue;
                foreach (var tc in tcs)
                {
                    if (tc?["id"]?.GetValue<string>() == orphanId)
                    {
                        callMsgIdx = i;
                        toolName = tc["function"]?["name"]?.GetValue<string>() ?? "unknown";
                        break;
                    }
                }
                if (callMsgIdx >= 0) break;
            }

            if (callMsgIdx < 0) continue;

            var errorMsg = $"[工具执行被中断] 工具 \"{toolName}\" 的调用未能完成执行。" +
                           $"可能原因：Agent 被中断、网络问题或进程异常退出。请重试或使用其他方法完成此操作。";

            var syntheticResult = new JsonObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = orphanId,
                ["content"] = errorMsg,
            };

            // 插入到 assistant 消息之后
            Messages.Insert(callMsgIdx + 1, syntheticResult);
            resultIds.Add(orphanId);

            DebugLog.Log("agent",
                $"RepairOrphaned: 为孤立 tool-call [{orphanId}] ({toolName}) 注入合成错误结果");
        }

        // 4. 删除无对应 tool-call 的 tool-result（反向遍历，安全删除）
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
            var msg = Messages[i];
            if (msg["role"]?.GetValue<string>() != "tool") continue;
            var id = msg["tool_call_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id) && !callIds.Contains(id))
            {
                DebugLog.Log("agent",
                    $"RepairOrphaned: 删除孤立 tool-result [{id}]（无对应 tool-call）");
                Messages.RemoveAt(i);
            }
        }
    }

    /// <summary>测试钩子: 循环检测窗口大小</summary>
    public int LoopWindowForTest => PerToolLoopWindow;
    /// <summary>测试钩子: 循环检测阈值</summary>
    public int LoopThresholdForTest => PerToolLoopThreshold;

    /// <summary>孤儿修复结果 (测试用)</summary>
    public sealed class OrphanRepairResult
    {
        public int OrphanCallsDetected;
        public int OrphanCallsFixed;
        public int OrphanResultsDetected;
        public int OrphanResultsRemoved;
    }

    /// <summary>测试钩子: 对给定消息列表执行孤儿修复并返回统计</summary>
    public static OrphanRepairResult TestOrphanRepair(List<JsonObject> messages)
    {
        var result = new OrphanRepairResult();

        // 收集所有 tool_call ID
        var callIds = new HashSet<string>();
        foreach (var msg in messages)
        {
            if (msg["role"]?.GetValue<string>() != "assistant") continue;
            var toolCalls = msg["tool_calls"]?.AsArray();
            if (toolCalls == null) continue;
            foreach (var tc in toolCalls)
            {
                var id = tc?["id"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(id)) callIds.Add(id);
            }
        }

        // 收集所有 tool result 的 tool_call_id
        var resultIds = new HashSet<string>();
        foreach (var msg in messages)
        {
            if (msg["role"]?.GetValue<string>() != "tool") continue;
            var id = msg["tool_call_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id)) resultIds.Add(id);
        }

        // 统计孤儿调用
        var orphanCalls = callIds.Except(resultIds).ToList();
        result.OrphanCallsDetected = orphanCalls.Count;

        // 为每个孤儿调用注入合成错误
        foreach (var orphanId in orphanCalls)
        {
            int callMsgIdx = -1;
            string? toolName = null;
            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                if (msg["role"]?.GetValue<string>() != "assistant") continue;
                var tcs = msg["tool_calls"]?.AsArray();
                if (tcs == null) continue;
                foreach (var tc in tcs)
                {
                    if (tc?["id"]?.GetValue<string>() == orphanId)
                    {
                        callMsgIdx = i;
                        toolName = tc["function"]?["name"]?.GetValue<string>() ?? "unknown";
                        break;
                    }
                }
                if (callMsgIdx >= 0) break;
            }

            if (callMsgIdx < 0) continue;
            messages.Insert(callMsgIdx + 1, new JsonObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = orphanId,
                ["content"] = $"[工具执行被中断] 工具 \"{toolName}\" 的调用未能完成执行。",
            });
            result.OrphanCallsFixed++;
        }

        // 删除孤立 tool-result
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msg = messages[i];
            if (msg["role"]?.GetValue<string>() != "tool") continue;
            var id = msg["tool_call_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id) && !callIds.Contains(id))
            {
                result.OrphanResultsDetected++;
            }
        }

        // 实际删除
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msg = messages[i];
            if (msg["role"]?.GetValue<string>() != "tool") continue;
            var id = msg["tool_call_id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id) && !callIds.Contains(id))
            {
                messages.RemoveAt(i);
                result.OrphanResultsRemoved++;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取工具 schema 列表。
    /// </summary>
    private List<JsonObject> ToolSchemas() => Tools.Select(t => t.Schema()).ToList();

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

        // 检测快速模式：用户明确要求跳过探索（不要读文件/不要ls/不要规划等）
        if (SystemPrompt.DetectFastMode(userInput))
        {
            _fastMode = true;
            DebugLog.Log("agent", "检测到快速模式关键词，跳过探索工作流");
        }

        Messages.Add(new JsonObject { ["role"] = "user", ["content"] = userInput });
        await CompressWithSmallModel(onToken);

        // ── Architect 模式：大模型出计划 → 小模型执行 ──
        if (ArchitectMode && LlmClient.EffectiveModel != LlmClient.SmallModel)
        {
            var plan = await GenerateArchitectPlanAsync(onToken, cancellationToken);
            if (plan == null)
            {
                Messages.RemoveAt(Messages.Count - 1); // 回滚用户消息
                return "⚠ Architect 模式：大模型计划生成失败，已取消。";
            }
            // 将计划作为 system 消息注入，小模型继续执行
            Messages.Add(new JsonObject
            {
                ["role"] = "system",
                ["content"] = $"## 执行计划\n\n以下是 Architect 的分析和执行计划，请按步骤逐一执行：\n\n{plan}",
            });
            // 切换回小模型执行
            LlmClient.ModelOverride = LlmClient.SmallModel;
            onToken?.Invoke("\n📋 **计划已生成，切换到小模型执行...**\n\n");
        }

        int requeueCount = 0;
    Requeue:
        for (int round = 0; round < _effectiveMaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            // 累积真实 token 使用量（Crush 风格）
            Context.AddUsage(resp.PromptTokens, resp.CompletionTokens);
            // 自动省 token 模式：按任务轮数更新复杂度，动态调节压缩阈值
            Context.SetRound(round);

            // 没有工具调用 -> LLM 完成，返回文本
            if (resp.ToolCalls.Count == 0)
            {
                // 致命错误（如所有模型失败）：保存会话后退出
                if (resp.IsFatalError)
                {
                    try { SessionManager.SaveSession(Messages, LlmClient.EffectiveModel); } catch { }
                    return resp.Content ?? "[致命错误] 所有模型失败，会话已保存。";
                }

                Messages.Add(resp.ToMessage());
                // 自动继续检测：
                // 0. 模型输出大量推理内容但不产生实际输出（DeepSeek V4 等模型的常见问题）
                // 1. 模型首轮只输出分析不调用工具（toolCallCount==0, content>100）
                // 2. 模型用了一些工具后开始"口述代码"而非写入文件（content 包含代码特征 >300 字符）
                var toolCallCount = Messages.Count(m => m["role"]?.GetValue<string>() == "tool");
                var contentLen = resp.Content?.Length ?? 0;
                var reasoningLen = resp.ReasoningTokens;
                var hasCodeContent = contentLen > 300 &&
                    (resp.Content!.Contains("```") || resp.Content.Contains("class ") ||
                     resp.Content.Contains("public ") || resp.Content.Contains("def ") ||
                     resp.Content.Contains("func ") || resp.Content.Contains("function "));

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
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = nudge,
                    });
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
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = nudge,
                    });
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
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = nudge,
                    });
                    continue;
                }

                SaveWorkReport();
                return resp.Content ?? "";
            }

            // 有工具调用 -> 执行（多个时并行）
            _analysisOnlyStreak = 0; // 重置分析-不动手计数器
            _talksCodeStreak = 0;    // 重置口述代码计数器
            Messages.Add(resp.ToMessage());

            try
            {
                if (resp.ToolCalls.Count == 1)
                {
                    var tc = resp.ToolCalls[0];
                    onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                    var result = await ExecuteToolAsync(tc, onToolOutput);
                    // 自动 lint 反馈闭环：写文件后立即检查，错误注入工具结果
                    result = await AppendLintFeedbackAsync(tc, result);
                    // 自动 test 反馈闭环：写源码文件后跑测试，失败注入工具结果
                    result = await AppendTestFeedbackAsync(tc, result);
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = tc.Id,
                        ["content"] = result,
                    });
                }
                else
                {
                    // 多个工具调用时并行执行（不流式输出，避免交叉混乱）
                    var tasks = resp.ToolCalls.Select(async tc =>
                    {
                        onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                        var result = await ExecuteToolAsync(tc);
                        return (tc, result);
                    });

                    var results = await Task.WhenAll(tasks);
                    foreach (var (tc, result) in results)
                    {
                        // 自动 lint + test 反馈闭环
                        var finalResult = await AppendLintFeedbackAsync(tc, result);
                        finalResult = await AppendTestFeedbackAsync(tc, finalResult);
                        Messages.Add(new JsonObject
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = tc.Id,
                            ["content"] = finalResult,
                        });
                    }
                }
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
                Messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = "file_tracker",
                    ["content"] = changeWarning,
                });
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
                onToken?.Invoke($"\n⏳ **上下文压缩中... ({beforeTokens} tokens)**\n\n");
                await CompressWithSmallModel(onToken);
                var afterCount = Messages.Count;
                var afterTokens = ContextManager.EstimateTokens(Messages);
                DebugLog.Log("context", $"Crush-style auto-summarize: {beforeCount}→{afterCount} msgs, {beforeTokens}→{afterTokens} est.tokens");

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
                Messages.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = stopContext,
                });
            }
        }

        SaveWorkReport();

        // 检测任务是否可能仍在进行中（最近 5 轮有 write_file/edit_file 调用）
        var recentTools = Messages.TakeLast(10)
            .Where(m => m["role"]?.GetValue<string>() == "tool")
            .Select(m => m["content"]?.GetValue<string>() ?? "")
            .ToList();
        var wasWriting = recentTools.Any(c => c.Contains("✅ 已写入") || c.Contains("✅ 编辑完成"));
        var lastMsg = Messages.LastOrDefault(m => m["role"]?.GetValue<string>() == "assistant")
            ?["content"]?.GetValue<string>() ?? "";

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

    /// <summary>使用小模型执行上下文压缩（省钱）</summary>
    private async Task CompressWithSmallModel(Action<string>? onProgress = null)
    {
        // PreCompact hook
        var preCompactCtx = await HooksManager.RunPreCompactAsync(
            $"est.tokens={ContextManager.EstimateTokens(Messages)}/{Context.MaxTokens}");

        var saved = LlmClient.ModelOverride;
        LlmClient.ModelOverride = LlmClient.SmallModel;
        try
        {
            await Context.MaybeCompressAsync(Messages, LlmClient,
                onProgress: (layer, msg) => onProgress?.Invoke($"🔄 [{layer}/3] {msg}"));
        }
        finally { LlmClient.ModelOverride = saved; }

        // 注入 PreCompact 返回的额外上下文
        if (preCompactCtx != null)
        {
            Messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = preCompactCtx,
            });
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
            m["role"]?.GetValue<string>() == "user")?["content"]?.GetValue<string>() ?? "";
        if (originalUserMsg.Length > 200)
            originalUserMsg = originalUserMsg[..200] + "...";

        // 收集已创建/修改的文件清单（从 _allSessionFiles，比文本解析更准确）
        string[] fileArray;
        lock (_allSessionFiles)
            fileArray = _allSessionFiles.ToArray();
        var fileListStr = fileArray.Length > 0
            ? "\n\n已确认创建/修改的文件（" + fileArray.Length + " 个）：\n" + string.Join("\n", fileArray.Take(20).Select(f => $"  - {f}"))
                + (fileArray.Length > 20 ? $"\n  ...（共 {fileArray.Length} 个）" : "")
            : "";

        Messages.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = $"{reason}。原始用户请求是：`{originalUserMsg}`\n请从中断处继续，完成未完成的工作。不要重写或缩小已有文件——只创建新文件或向已有文件追加缺失内容。{fileListStr}",
        });
        Context.ResetUsage(); // 重置计数器，给新一轮足够的空间
    }

    /// <summary>
    /// 执行单个工具调用，返回结果字符串。
    /// </summary>
    private async Task<string> ExecuteToolAsync(ToolCall tc, Action<string>? onToolOutput = null)
    {
        if (!ToolByName.TryGetValue(tc.Name, out var tool))
            return $"错误：未知工具 '{tc.Name}'";

        try
        {
            // 工作模式约束检查：Plan/Review 模式下阻止修改性工具
            var modeBlock = WorkModeManager.CheckToolAllowed(tc.Name, WorkModeManager.CurrentMode);
            if (modeBlock != null)
                return modeBlock;

            // 权限检查：危险操作需要用户确认
            if (!await PermissionManager.CheckAsync(tc.Name, tc.Arguments))
                return "用户取消了此操作。";

            // PreToolUse hook
            var hookBlock = await HooksManager.RunPreToolUseAsync(tc.Name, tc.Arguments);
            if (hookBlock != null)
                return $"操作被 Hook 阻止: {hookBlock}";

            // Stale-read 检查：编辑/写入前确认文件未被外部修改（对标 Crush filetracker edit guard）
            string? staleWarning = null;
            if (tc.Name is "edit_file" or "write_file")
            {
                if (tc.Arguments.TryGetValue("file_path", out var fpObj) && fpObj is string fp && !string.IsNullOrWhiteSpace(fp))
                {
                    var (isTracked, isStale) = FileTracker.GetStatus(fp);
                    if (isTracked && isStale)
                    {
                        staleWarning =
                            $"⚠️ **Stale-Read 警告**：文件 `{fp}` 自上次读取后被外部修改。\n" +
                            $"请先用 read_file 重新读取该文件的最新内容，确认变更后再编辑。\n" +
                            $"如确认无需重新读取，可再次调用 edit_file（第二次调用会略过此检查）。";
                        DebugLog.Log("file-tracker", $"Stale-read 阻止: {fp}");
                    }
                    // 未追踪的文件：记录一次写入前的状态（即使没读过，也追踪写入后的哈希）
                    else if (!isTracked)
                    {
                        FileTracker.RecordWrite(fp);
                    }
                }
            }

            // 文件被外部修改 → 返回错误，强制重读（首次触发时）
            if (staleWarning != null)
                return staleWarning;

            var result = tool is BashTool bashTool && onToolOutput != null
                ? await bashTool.ExecuteStreamingAsync(tc.Arguments,
                    async line => { onToolOutput(line); await Task.CompletedTask; })
                : await tool.ExecuteAsync(tc.Arguments);

            // 追踪修改的文件（用于自动 commit 精准暂存 + FileTracker 哈希更新）
            if (tc.Name is "write_file" or "edit_file" or "notebook_edit")
            {
                if (tc.Arguments.TryGetValue("file_path", out var fp) && fp is string path && !string.IsNullOrWhiteSpace(path))
                {
                    lock (_modifiedFiles)
                    {
                        _modifiedFiles.Add(path);
                        _allSessionFiles.Add(path);
                    }
                    // 更新 FileTracker 哈希，防止下次编辑时误报 stale
                    FileTracker.RecordWrite(path);
                }
            }

            // PostToolUse hook（可修改结果）
            var hookResult = await HooksManager.RunPostToolUseAsync(tc.Name, tc.Arguments, result);
            if (hookResult != null)
                result = hookResult;

            // 错误自恢复：工具返回错误时追加修正提示
            if (result.StartsWith("错误") || result.StartsWith("Error"))
                result += "\n[请分析错误原因，修正参数后重试]";

            DebugLog.LogToolResult(tc.Name, result);
            return result;
        }
        catch (Exception ex)
        {
            // PostToolUseFailure hook
            await HooksManager.RunPostToolUseFailureAsync(tc.Name, tc.Arguments, ex.Message);

            ErrorLog.ToolError(tc.Name, $"工具执行异常: {ex.Message}", ex, tc.Arguments);
            return $"执行 {tc.Name} 时出错：{ex.Message}\n[请分析错误原因，尝试其他方式完成目标]";
        }
    }

    /// <summary>
    /// 写文件后自动运行 lint，错误注入工具结果，形成自动修复闭环。
    /// 仅对 write_file / edit_file 触发，lint 无错误则不追加。
    /// </summary>
    private async Task<string> AppendLintFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        // 文件修改后使仓库地图缓存失效
        RepoMapGenerator.Invalidate();

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath))
            return toolResult;

        try
        {
            var lang = LintTool.DetectLanguage(filePath);
            if (lang == null) return toolResult;

            var lintTool = new LintTool();
            var lintArgs = new Dictionary<string, object?> { ["path"] = filePath };
            var lintResult = await lintTool.ExecuteAsync(lintArgs);

            // 仅当 lint 发现问题时才追加反馈
            if (!lintResult.Contains("✅") && !lintResult.Contains("⚠"))
                return toolResult;

            // 截断过长输出
            if (lintResult.Length > 1500)
                lintResult = lintResult[..1500] + "\n... (已截断)";

            return toolResult + $"\n\n--- Lint 自动检查 ({lang}) ---\n{lintResult}";
        }
        catch
        {
            return toolResult; // lint 失败不影响主流程
        }
    }

    /// <summary>上次自动跑测试的时间（防抖：同一项目 60s 内不重复跑）</summary>
    private static DateTime _lastTestRun;
    private static string? _lastTestProject;

    /// <summary>
    /// 写源码文件后自动运行项目测试，失败结果注入工具结果，形成自动修复闭环。
    /// </summary>
    private async Task<string> AppendTestFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath)) return toolResult;

        // 仅源码文件触发测试
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var srcExts = new[] { ".cs", ".py", ".ts", ".js", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".cpp", ".rb" };
        if (!srcExts.Contains(ext)) return toolResult;

        // 防抖：同一项目 N 秒内不重复跑
        var cwd = Directory.GetCurrentDirectory();
        if (_lastTestProject == cwd && (DateTime.UtcNow - _lastTestRun).TotalSeconds < Config.Instance.AutoTestDebounceSec)
            return toolResult;

        var testCmd = DetectTestCommand();
        if (testCmd == null) return toolResult;

        // WayCoder 自己的自测可能很慢，跳过（编辑 WayCoder 自身时）
        if (testCmd.Contains("--test") && File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return toolResult;

        try
        {
            _lastTestProject = cwd;
            _lastTestRun = DateTime.UtcNow;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = testCmd.Split(' ')[0],
                Arguments = string.Join(' ', testCmd.Split(' ').Skip(1)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return toolResult;

            // 最多等 N 秒
            var readTask = proc.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Delay(Config.Instance.AutoTestTimeoutSec * 1000);
            var completed = await Task.WhenAny(readTask, timeoutTask);
            if (completed == timeoutTask)
            {
                try { proc.Kill(); } catch { }
                ErrorLog.Warning("Agent", $"自动测试超时（{Config.Instance.AutoTestTimeoutSec}s），已终止进程");
                return toolResult;
            }

            var output = await readTask;
            var errorOutput = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            var fullOutput = output + errorOutput;
            if (proc.ExitCode == 0)
                return toolResult; // 测试通过，不追加

            // 截断
            if (fullOutput.Length > 2000)
                fullOutput = fullOutput[..2000] + $"\n... (共 {fullOutput.Length} 字符)";

            return toolResult + $"\n\n--- 🔴 自动测试失败 (exit={proc.ExitCode}) ---\n{fullOutput}\n[请修复代码使测试通过]";
        }
        catch
        {
            return toolResult; // 测试失败不影响主流程
        }
    }

    /// <summary>检测当前项目的测试命令</summary>
    private static string? DetectTestCommand()
    {
        var cwd = Directory.GetCurrentDirectory();

        // WayCoder 自测 (内置 SelfTest)
        if (File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return "dotnet run -c Release -- --test 2>&1";

        // .NET 测试项目
        var testProjects = Directory.GetFiles(cwd, "*.Tests.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(cwd, "*.Test.csproj", SearchOption.AllDirectories)).ToArray();
        if (testProjects.Length > 0)
            return "dotnet test --nologo -v q 2>&1";

        // Node.js
        if (File.Exists(Path.Combine(cwd, "package.json")))
        {
            try
            {
                var pkg = System.Text.Json.Nodes.JsonNode.Parse(
                    File.ReadAllText(Path.Combine(cwd, "package.json")));
                if (pkg?["scripts"]?["test"] != null)
                    return "npm test --silent 2>&1";
            }
            catch { }
        }

        // Go
        if (File.Exists(Path.Combine(cwd, "go.mod")))
            return "go test ./... 2>&1";

        // Rust
        if (File.Exists(Path.Combine(cwd, "Cargo.toml")))
            return "cargo test -q 2>&1";

        // Python
        if (Directory.GetFiles(cwd, "test_*.py", SearchOption.AllDirectories).Any() ||
            Directory.GetFiles(cwd, "*_test.py", SearchOption.AllDirectories).Any())
            return "python -m pytest -q 2>&1";

        return null;
    }

    /// <summary>
    /// 为每个未收到工具回复的工具调用回填一条工具回复。
    /// 确保在执行被中断时历史记录保持有效。
    /// </summary>
    private void AnswerPendingToolCalls(List<ToolCall> toolCalls)
    {
        var answered = new HashSet<string>(
            Messages.Where(m => m["role"]?.GetValue<string>() == "tool")
                    .Select(m => m["tool_call_id"]?.GetValue<string>() ?? ""));

        foreach (var tc in toolCalls)
        {
            if (!answered.Contains(tc.Id))
            {
                Messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = tc.Id,
                    ["content"] = "[已中断]",
                });
            }
        }
    }

    /// <summary>
    /// 清空对话历史。
    /// </summary>
    /// <summary>清空对话历史，重置 Agent 状态。</summary>
    public void Reset() => Messages.Clear();

    private static string FormatBrief(Dictionary<string, object?> args, int maxLen = 80)
    {
        var s = string.Join(", ", args.Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
        return s.Length > maxLen ? s[..maxLen] + "..." : s;
    }

    private static string FormatValue(object? value)
    {
        var s = value?.ToString() ?? "null";
        return s.Length > 40 ? s[..40] + "..." : s;
    }

    /// <summary>工具执行后自动 git commit。用小模型生成提交信息。</summary>
    private async Task AutoCommitAsync()
    {
        try
        {
            var gitDir = await RunGitAsync("rev-parse --git-dir");
            if (string.IsNullOrWhiteSpace(gitDir)) return;

            // 收集本轮修改的文件列表
            string[] modifiedFiles;
            lock (_modifiedFiles)
            {
                modifiedFiles = _modifiedFiles.ToArray();
                _modifiedFiles.Clear();
            }

            // 如果没有追踪到文件修改，检查 git status 兜底
            if (modifiedFiles.Length == 0)
            {
                var status = await RunGitAsync("status --porcelain");
                if (string.IsNullOrWhiteSpace(status)) return;
                var lines = status.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                modifiedFiles = lines.Select(f => f.Length > 3 ? f[3..].Trim() : f.Trim()).Take(10).ToArray();
                if (modifiedFiles.Length == 0) return;
            }

            // 获取简要 diff 统计作为提交正文
            var fileList = string.Join(", ", modifiedFiles);
            var diffStat = await RunGitAsync($"diff --stat --cached {string.Join(" ", modifiedFiles.Select(EscArg))}");
            if (string.IsNullOrWhiteSpace(diffStat))
                diffStat = await RunGitAsync($"diff --stat {string.Join(" ", modifiedFiles.Select(EscArg))}");

            // 小模型生成 conventional-commit 标题
            var summary = await GenerateCommitMsgAsync(fileList);
            if (string.IsNullOrWhiteSpace(summary)) return;

            // 构建提交信息：标题 + 空行 + diff 统计
            var commitMsg = summary;
            if (!string.IsNullOrWhiteSpace(diffStat) && diffStat.Length < 500)
                commitMsg += "\n\n" + diffStat.Trim();

            // 精准暂存：只 add 实际修改的文件
            foreach (var f in modifiedFiles)
            {
                if (File.Exists(f))
                    await RunGitAsync($"add {EscArg(f)}");
            }

            // 提交
            var msgFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(msgFile, commitMsg);
            await RunGitAsync($"commit -F {EscArg(msgFile)}");
            try { File.Delete(msgFile); } catch { }

            // 用户反馈
            _onAutoCommit?.Invoke(summary, modifiedFiles.Length);

            DebugLog.Log("auto-commit", $"Committed: {summary} ({modifiedFiles.Length} files)");
        }
        catch (Exception ex) { DebugLog.Log("auto-commit", $"AutoCommitAsync failed: {ex.Message}"); }
    }

    /// <summary>给 shell 参数做安全转义（单引号包裹，内部单引号替换为 '\''）</summary>
    internal static string EscArg(string s) => $"'{s.Replace("'", "'\\''")}'";

    /// <summary>自动提交完成后的回调</summary>
    private Action<string, int>? _onAutoCommit;

    /// <summary>注册自动提交回调（用于 UI 反馈）</summary>
    public void OnAutoCommit(Action<string, int> callback) => _onAutoCommit = callback;

    private async Task<string> GenerateCommitMsgAsync(string fileList)
    {
        try
        {
            var saved = LlmClient.ModelOverride;
            LlmClient.ModelOverride = LlmClient.SmallModel;
            try
            {
                var msgs = new List<JsonObject>
                {
                    new() { ["role"] = "system", ["content"] = "You are a git commit message generator. Output a single line, English, conventional-commit style with a valid prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), <70 chars. Do NOT include any quotes, backticks, or extra text." },
                    new() { ["role"] = "user", ["content"] = "Modified files: " + fileList + "\n\nCommit message:" },
                };
                var result = await LlmClient.ChatAsync(msgs, tools: null);
                var msg = CleanCommitMsg(result?.Content ?? "");

                // 质量校验：不合格则重试一次
                if (!IsValidCommitMsg(msg))
                {
                    msgs[0]["content"] = "Your previous output was invalid. Strict rules: exactly one line, English, conventional-commit prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), no quotes/backticks/code fences, <70 chars. Output only the message.";
                    result = await LlmClient.ChatAsync(msgs, tools: null);
                    msg = CleanCommitMsg(result?.Content ?? "");
                }

                // 重试后仍不合格：回退到安全默认信息
                if (!IsValidCommitMsg(msg))
                {
                    var firstFile = fileList.Split(',')[0].Trim();
                    msg = "chore: update " + firstFile;
                }
                return msg.Length > 72 ? msg[..72] : msg;
            }
            finally { LlmClient.ModelOverride = saved; }
        }
        catch { return ""; }
    }

    /// <summary>清理提交信息：去引号/反引号/换行/多余空白。</summary>
    internal static string CleanCommitMsg(string raw)
    {
        var msg = (raw ?? "").Trim();
        // 去掉代码围栏和常见的模板包裹
        msg = msg.Replace("```", "").Replace("`", "");
        msg = msg.Replace("\"", "").Replace("'", "");
        msg = msg.Replace("\n", " ").Replace("\r", " ").Trim();
        // 去掉 "Here is the commit message:" 之类的 AI 模板前缀
        var colonIdx = msg.IndexOf(":");
        if (colonIdx > 0 && colonIdx < 30)
        {
            var prefix = msg[..colonIdx].ToLowerInvariant();
            if (prefix.Contains("commit") || prefix.Contains("message") || prefix.Contains("here"))
                msg = msg[(colonIdx + 1)..].Trim();
        }
        return msg;
    }

    /// <summary>校验提交信息质量：必须有合法 conventional 前缀、非空、不含中文。</summary>
    internal static bool IsValidCommitMsg(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return false;
        if (msg.Length < 5) return false;
        // 拒绝中文（提交信息约定为英文）
        if (System.Text.RegularExpressions.Regex.IsMatch(msg, @"[\u4e00-\u9fff]")) return false;
        // 拒绝继续出现的模板短语
        var lower = msg.ToLowerInvariant();
        if (lower.Contains("here is") || lower.Contains("generate a") || lower.Contains("i will"))
            return false;
        // 必须匹配 conventional-commit 前缀
        var firstWord = msg.Split(' ')[0].Split(':')[0].Trim().ToLowerInvariant();
        return firstWord is "feat" or "fix" or "docs" or "style" or "refactor"
            or "perf" or "test" or "chore" or "build" or "ci" or "revert";
    }

    private static async Task<string?> RunGitAsync(string args)
    {
        var (exitCode, stdout, _) = await GitRunner.RunAsync(args);
        return exitCode == 0 ? stdout.Trim() : null;
    }

    // ── Architect 双模型模式 ──

    /// <summary>
    /// 用大模型生成执行计划（无工具调用，纯思考输出）。
    /// 返回 null 表示失败/取消。
    /// </summary>
    private async Task<string?> GenerateArchitectPlanAsync(
        Action<string>? onToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var architectPrompt = SystemPrompt.GenerateArchitectPrompt();
            var msgs = new List<JsonObject>
            {
                new() { ["role"] = "system", ["content"] = architectPrompt },
            };
            // 克隆历史消息（不含工具往返）给 Architect 做上下文
            foreach (var m in Messages)
            {
                var role = (string?)m["role"];
                if (role is "tool" or "assistant_tool_calls") continue;
                msgs.Add(JsonNode.Parse(m.ToJsonString())!.AsObject());
            }

            // 切换到大模型，不带工具
            var savedOverride = LlmClient.ModelOverride;
            LlmClient.ModelOverride = LlmClient.Model; // 大模型

            var resp = await LlmClient.ChatAsync(
                messages: msgs,
                tools: null, // 不给工具，纯分析
                onToken: onToken,
                cancellationToken: cancellationToken);

            LlmClient.ModelOverride = savedOverride;

            return string.IsNullOrWhiteSpace(resp.Content) ? null : resp.Content.Trim();
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            DebugLog.Log("architect", $"计划生成异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据配置过滤工具列表（白名单 + 黑名单）。
    /// </summary>
    private static List<ITool> FilterTools(List<ITool> tools)
    {
        var config = Config.Instance;
        var allowed = config.AllowedTools?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim().ToLowerInvariant()).ToHashSet();
        var disabled = config.DisabledTools?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim().ToLowerInvariant()).ToHashSet();

        var result = tools;
        if (allowed is { Count: > 0 })
            result = result.Where(t => allowed.Contains(t.Name.ToLowerInvariant())).ToList();
        if (disabled is { Count: > 0 })
            result = result.Where(t => !disabled.Contains(t.Name.ToLowerInvariant())).ToList();

        if (result.Count < tools.Count)
            DebugLog.Log("tool-filter", $"工具过滤: {tools.Count} → {result.Count} (白名单={allowed?.Count ?? 0}, 黑名单={disabled?.Count ?? 0})");

        return result;
    }

    /// <summary>
    /// SHA256 循环检测（Crush 风格）。
    /// 对最近几轮的 assistant 消息内容 + 工具结果做哈希，
    /// 相同哈希重复出现 LoopDetectionThreshold+ 次说明 Agent 陷入循环。
    /// 此时注入反循环提示，强制 Agent 换策略。
    /// </summary>
    /// <summary>
    /// Per-tool-call 级循环检测（对标 Crush per-tool loop detection）。
    ///
    /// 对每一轮中每个已执行的工具调用，哈希 (tool_name + args + output 前 2000 字符)，
    /// 相同指纹在窗口内出现 5+ 次 → 循环警告。
    ///
    /// 与旧 per-round 方案的区别：更细粒度 — 同轮中其他工具不同不会掩盖
    /// 某个特定工具的重复调用模式。
    /// </summary>
    private void DetectAndBreakLoop(LLMResponse resp, List<JsonObject> messages)
    {
        const int outputSnipLen = 2000;

        // 收集本轮已执行的 tool 消息（tool_call_id 匹配 resp.ToolCalls 的 Id）
        var toolIds = new HashSet<string>(resp.ToolCalls.Select(tc => tc.Id));
        var executedCalls = new List<(string Name, string Args, string Output)>();
        foreach (var tc in resp.ToolCalls)
        {
            foreach (var m in messages)
            {
                if (m["role"]?.GetValue<string>() == "tool"
                    && m["tool_call_id"]?.GetValue<string>() == tc.Id)
                {
                    var output = m["content"]?.GetValue<string>() ?? "";
                    executedCalls.Add((tc.Name,
                        JsonHelper.SerializeArgs(tc.Arguments),
                        output.Length > outputSnipLen ? output[..outputSnipLen] : output));
                    break;
                }
            }
        }

        if (executedCalls.Count == 0) return;

        // 对每个已执行的工具调用，生成 per-tool 指纹并加入滑动窗口
        foreach (var (name, args, output) in executedCalls)
        {
            var fingerprint = $"{name}\x00{args}\x00{output}";
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(fingerprint)));

            _recentActionHashes.Add(hash);
        }

        // 保持滑动窗口大小
        while (_recentActionHashes.Count > PerToolLoopWindow)
            _recentActionHashes.RemoveAt(0);

        // 统计窗口内每个哈希的出现次数，任一超过阈值即触发
        var hashCounts = new Dictionary<string, int>();
        foreach (var h in _recentActionHashes)
        {
            hashCounts.TryGetValue(h, out var c);
            hashCounts[h] = c + 1;
        }

        var offendingHash = hashCounts
            .FirstOrDefault(kv => kv.Value >= PerToolLoopThreshold);

        if (offendingHash.Key != null)
        {
            _loopNudgeCount++;
            DebugLog.Log("loop",
                $"Per-tool 循环检测：哈希 {offendingHash.Key[..8]} 在最近 {_recentActionHashes.Count} 个工具调用中出现 {offendingHash.Value} 次（第 {_loopNudgeCount} 次反循环提示）");

            // 批量循环：显示涉及的重复工具数
            var duplicateCount = hashCounts.Values.Count(v => v >= PerToolLoopThreshold);
            var dupNote = duplicateCount > 1
                ? $"（共 {duplicateCount} 个工具调用模式在重复）"
                : "";

            var nudge = _loopNudgeCount switch
            {
                1 => $"检测到重复的工具调用模式{dupNote}。请换一种不同的方法或工具来完成任务。如果之前的方案反复失败，请尝试完全不同的思路。",
                2 => $"你仍在重复相同的操作模式{dupNote}。请停下来，重新评估问题，尝试一种完全不同的策略。检查之前的工具输出，找出失败的原因。",
                _ => $"严重警告：你已经多次重复相同的无效操作{dupNote}。立即停止当前方法。回顾整个任务目标，从第一步重新开始，使用完全不同的工具或顺序。如有必要，向用户报告卡住的原因。",
            };

            messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = nudge,
            });

            // 重置窗口避免连续触发（给 Agent 几轮时间调整）
            _recentActionHashes.Clear();
        }
    }

}
