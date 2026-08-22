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

    /// <summary>
    /// 执行单个工具调用，返回结果字符串。
    /// </summary>
    private async Task<string> ExecuteToolAsync(ToolCall tc, Action<string>? onToolOutput = null, CancellationToken cancellationToken = default)
    {
        if (!ToolByName.TryGetValue(tc.Name, out var tool))
            return $"错误：未知工具 '{tc.Name}'";

        // 注入本 Agent 唯一标识，供文件锁等跨 Agent 资源冲突检测（WriteFile/EditFile 等经 _agent_id 读取）
        tc.Arguments["_agent_id"] = AgentId;

        try
        {
            // 工作模式约束检查：Plan 阻止写工具（含只读 bash 门控）、Chat 全禁
            var modeBlock = WorkModeManager.CheckToolAllowed(tc.Name, WorkMode, tc.Arguments);
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

            // 写文件内容展示：edit/multiedit 与 write-append 需要编辑前的旧内容做 diff。
            // 仅交互 TUI（有 onToolOutput）且开关开启时才读，避免无关文件 IO。
            string? oldContentForDisplay = null;
            bool needOldForDisplay = tc.Name is "edit_file" or "multiedit" ||
                (tc.Name == "write_file" &&
                 tc.Arguments.TryGetValue("append", out var wcApArg) &&
                 wcApArg?.ToString()?.ToLowerInvariant() == "true");
            if (needOldForDisplay && onToolOutput != null && Config.Instance.WriteContentView &&
                tc.Arguments.TryGetValue("file_path", out var wcOldFpObj) &&
                wcOldFpObj is string wcOldFpStr && !string.IsNullOrWhiteSpace(wcOldFpStr))
            {
                try
                {
                    var wcOldPath = Path.GetFullPath(wcOldFpStr, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory());
                    if (File.Exists(wcOldPath)) oldContentForDisplay = File.ReadAllText(wcOldPath);
                }
                catch { }
            }

            // 可取消工具：中断（Web 停止按钮 / Ctrl+C）时能真正杀掉子进程（如 bash）。
            // bash 走流式路径（有 onToolOutput 时）；其余可取消工具统一走 ICancellableTool。
            var result = tool is BashTool bashTool && onToolOutput != null
                ? await bashTool.ExecuteStreamingAsync(tc.Arguments,
                    async line => { onToolOutput(line); await Task.CompletedTask; }, cancellationToken)
                : tool is ICancellableTool cancellable
                    ? await cancellable.ExecuteAsync(tc.Arguments, cancellationToken)
                    : await tool.ExecuteAsync(tc.Arguments);

            // 追踪修改的文件（用于自动 commit 精准暂存 + FileTracker 哈希更新）
            if (tc.Name is "write_file" or "edit_file" or "notebook_edit")
            {
                if (tc.Arguments.TryGetValue("file_path", out var fp) && fp is string path && !string.IsNullOrWhiteSpace(path))
                {
                    lock (_modifiedFiles)
                        _modifiedFiles.Add(path);
                    lock (_allSessionFiles)
                        _allSessionFiles.Add(path);
                    // 更新 FileTracker 哈希，防止下次编辑时误报 stale
                    FileTracker.RecordWrite(path);
                }
            }

            // ── 写文件内容聊天区展示 ──
            // 写盘成功后读回全文，按 diff 格式（行号 + 标记）经 onToolOutput 内联展示。
            // 仅展示、不进 LLM 上下文（result 保持摘要）；读取/格式化失败静默跳过。
            if (onToolOutput != null && Config.Instance.WriteContentView &&
                tc.Arguments.TryGetValue("file_path", out var wcFpObj) &&
                wcFpObj is string wcFpStr && !string.IsNullOrWhiteSpace(wcFpStr))
            {
                try
                {
                    bool isWrite = tc.Name == "write_file";
                    bool isEdit = tc.Name == "edit_file";
                    bool isMulti = tc.Name == "multiedit";
                    bool writeOk = isWrite && (result.StartsWith("已写入") || result.StartsWith("已追加"));
                    bool editOk = isEdit && result.StartsWith("已编辑");
                    bool multiOk = isMulti && (result.StartsWith("✅ 已创建") || result.StartsWith("✅ 已编辑"));
                    if (writeOk || editOk || multiOk)
                    {
                        var wcPath = Path.GetFullPath(wcFpStr, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory());
                        if (File.Exists(wcPath))
                        {
                            var wcNewContent = File.ReadAllText(wcPath);
                            bool addedView = (isWrite && !result.StartsWith("已追加")) ||
                                             (isMulti && result.StartsWith("✅ 已创建"));
                            string display = addedView
                                ? ContentDiffFormatter.FormatAddedContent(wcNewContent, wcPath)
                                : ContentDiffFormatter.FormatEditContent(oldContentForDisplay ?? "", wcNewContent, wcPath);
                            onToolOutput(display);
                        }
                    }
                }
                catch { }
            }

            // PostToolUse hook（可修改结果）
            var hookResult = await HooksManager.RunPostToolUseAsync(tc.Name, tc.Arguments, result);
            if (hookResult != null)
                result = hookResult;

            // 错误自恢复：工具返回真实错误时追加修正提示（用户取消/权限拒绝/安全阻止不提示）
            if (ToolResultClassifier.IsError(result))
                result += "\n[请分析错误原因，修正参数后重试]";

            DebugLog.LogToolResult(tc.Name, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw; // 中断信号（Web 停止按钮 / Ctrl+C），向上传播，不吞掉
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
    /// 执行一轮 LLM 返回的工具调用，结果按模型声明顺序回填到 Messages。
    /// 单工具走流式输出；多工具按 ExecutionMode 切分批次——批内并行（有界并发）、
    /// 批间串行，Exclusive 工具独占执行，避免共享状态/副作用的竞态
    /// （对标 deepseek-harness 的 executionMode + bounded rolling pool）。
    /// </summary>
    private async Task ExecuteToolCallsAsync(
        List<ToolCall> toolCalls, Action<string, string>? onTool, Action<string>? onToolOutput,
        CancellationToken cancellationToken = default)
    {
        if (toolCalls.Count == 1)
        {
            var tc = toolCalls[0];
            onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
            var result = await RunToolAndRecordAsync(tc, onToolOutput, cancellationToken);
            // 自动 lint 反馈闭环：写文件后立即检查，错误注入工具结果
            result = await AppendLintFeedbackAsync(tc, result);
            // 自动 test 反馈闭环：写源码文件后跑测试，失败注入工具结果
            result = await AppendTestFeedbackAsync(tc, result);
            AddMessage(JNode.Object()
                .Set("role", "tool")
                .Set("tool_call_id", tc.Id)
                .Set("content", result));
            return;
        }

        // 多工具：按执行模式分批执行，结果暂存后按模型声明顺序回填
        var batches = ToolCallScheduler.Partition(toolCalls, GetExecutionMode);
        var results = new Dictionary<string, string>();

        foreach (var batch in batches)
        {
            if (batch.Count == 1)
            {
                var tc = batch[0];
                onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                // 单工具批次独占执行（write/edit 为 Exclusive）：传 onToolOutput，
                // 使写文件内容展示与 bash 流式在多工具轮次中也生效（批次内无并发穿插）。
                results[tc.Id] = await RunToolAndRecordAsync(tc, onToolOutput, cancellationToken);
            }
            else
            {
                // 并行批次：SemaphoreSlim 有界并发，避免一轮 20 个工具调用同时起 20 个进程
                using var sem = new SemaphoreSlim(ToolCallScheduler.MaxParallelism);
                var tasks = batch.Select(async tc =>
                {
                    onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                    await sem.WaitAsync(cancellationToken);
                    try { return (tc, await RunToolAndRecordAsync(tc, null, cancellationToken)); }
                    finally { sem.Release(); }
                }).ToArray();
                try
                {
                    var batchResults = await Task.WhenAll(tasks);
                    foreach (var (tc, result) in batchResults)
                        results[tc.Id] = result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 中断：Task.WhenAll 已结束所有任务（SemaphoreSlim 无 in-flight 竞态），
                    // 提交已完成的工具结果——避免上层 AnswerPendingToolCalls 把已执行的工具
                    // 误标 [已中断]，恢复后重复执行（二次写文件）
                    foreach (var t in tasks)
                        if (t.IsCompletedSuccessfully)
                        {
                            var (tc, result) = t.Result;
                            AddMessage(JNode.Object()
                                .Set("role", "tool")
                                .Set("tool_call_id", tc.Id)
                                .Set("content", result));
                        }
                    throw;
                }
            }
        }

        // 按模型声明顺序提交（含 lint/test 反馈闭环）
        foreach (var tc in toolCalls)
        {
            var finalResult = await AppendLintFeedbackAsync(tc, results[tc.Id]);
            finalResult = await AppendTestFeedbackAsync(tc, finalResult);
            AddMessage(JNode.Object()
                .Set("role", "tool")
                .Set("tool_call_id", tc.Id)
                .Set("content", finalResult));
        }
    }

    /// <summary>执行单个工具并记录轨迹（耗时 + 成败 + 摘要），异常时记录失败后重抛。</summary>
    private async Task<string> RunToolAndRecordAsync(ToolCall tc, Action<string>? onToolOutput, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await ExecuteToolAsync(tc, onToolOutput, cancellationToken);
            _trajectory?.RecordTool(tc.Name, FormatBrief(tc.Arguments), result,
                ok: !ToolResultClassifier.IsError(result), durationMs: sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _trajectory?.RecordTool(tc.Name, FormatBrief(tc.Arguments), ex.Message,
                ok: false, durationMs: sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>按工具名解析执行模式（未知工具保守按 Exclusive 处理）。</summary>
    private ToolExecutionMode GetExecutionMode(string name)
        => ToolByName.TryGetValue(name, out var tool)
            ? tool.ExecutionMode
            : ToolExecutionMode.Exclusive;

    /// <summary>
    /// 为每个未收到工具回复的工具调用回填一条工具回复。
    /// 确保在执行被中断时历史记录保持有效。
    /// </summary>
    private void AnswerPendingToolCalls(List<ToolCall> toolCalls)
    {
        var answered = new HashSet<string>(
            Messages.Where(m => m["role"]?.AsString() == "tool")
                    .Select(m => m["tool_call_id"]?.AsString() ?? ""));

        foreach (var tc in toolCalls)
        {
            if (!answered.Contains(tc.Id))
            {
                AddMessage(JNode.Object()
                    .Set("role", "tool")
                    .Set("tool_call_id", tc.Id)
                    .Set("content", "[已中断]"));
            }
        }
    }
    // ── 省钱模式工具精简 ──
    // 设计原则：省钱关 = 全量工具；开 = 去掉重复（bash 可替代）；开的越大，工具越精简。
    // 档位顺序（Off→Auto→On→Extreme）工具数单调递减：
    //   Off      = 全量 46
    //   Auto     = 去掉 bash 基础命令可替代的重复工具（cd/pwd/ls/mkdir/cp/mv/rm/tree/wc/stat/ps/kill）
    //   On       = Auto + 再去掉搜索/编辑冗余（glob/grep/diff/find_replace/multiedit，bash 或 read+edit 可替代）
    //   Extreme  = 只留读改写 + 执行(bash) + 联网查/下载(web_search/fetch) + 问用户核心（白名单式保留）
    /// <summary>bash 基础命令可直接替代的重复工具（Auto 及以上去掉）</summary>
    internal static readonly HashSet<string> BashRedundantTools = new(StringComparer.OrdinalIgnoreCase)
    { "cd", "pwd", "ls", "mkdir", "cp", "mv", "rm", "tree", "wc", "stat", "ps", "kill" };
    /// <summary>搜索/编辑冗余工具（On 及以上再去掉）</summary>
    internal static readonly HashSet<string> EditRedundantTools = new(StringComparer.OrdinalIgnoreCase)
    { "glob", "grep", "diff", "find_replace", "multiedit" };
    /// <summary>Extreme 核心工具（极致省钱最小集：读改写 + 执行 + 联网查/下载 + 问用户）</summary>
    internal static readonly HashSet<string> ExtremeCoreTools = new(StringComparer.OrdinalIgnoreCase)
    { "read_file", "write_file", "edit_file", "bash", "web_search", "fetch", "ask_user_question" };

    /// <summary>
    /// 按省钱模式档位精简工具（去重复）。Off 返回原集不动。
    /// </summary>
    internal static List<ITool> TrimToolsForEconomy(List<ITool> tools, EconomyMode mode)
    {
        if (mode == EconomyMode.Off) return tools;
        if (mode == EconomyMode.Extreme)
            return tools.Where(t => ExtremeCoreTools.Contains(t.Name)).ToList();
        var keep = tools.Where(t => !BashRedundantTools.Contains(t.Name)).ToList();
        if (mode == EconomyMode.On)
            keep = keep.Where(t => !EditRedundantTools.Contains(t.Name)).ToList();
        return keep;
    }

    /// <summary>
    /// 根据配置过滤工具列表（工作模式 + 白名单 + 省钱精简 + 黑名单）。
    /// 决策链：Chat → 0 工具；Plan → 只读白名单（用户 PlanToolAllowList 可交集收窄）→ 黑名单；
    ///         Build → 显式白名单（YOLO 覆盖 YoloToolAllowList）→ 经济精简 → 黑名单。
    /// </summary>
    private static List<ITool> FilterTools(Agent agent, List<ITool> tools)
    {
        var config = Config.Instance;

        // 1. Chat 工作模式：纯聊天，0 工具（由行为轴 WorkMode 决定，不再依赖权限 TINY）
        if (agent.WorkMode == WorkMode.Chat)
            return [];

        // 2. Plan 工作模式：只读工具白名单（schema 层硬过滤，单一事实源 WorkModeManager.PlanReadOnlyTools）。
        //    bash 也在白名单，运行时由 CheckToolAllowed 按 BashGuard.IsSafeReadOnly 门控只读命令。
        //    用户 PlanToolAllowList 仅可交集收窄（只可更严，不可放开写工具）。
        //    YOLO 权限不覆盖 Plan 工具集 —— 只读是行为轴硬约束。
        if (agent.WorkMode == WorkMode.Plan)
        {
            var planUserAllow = ParseCsvSet(config.PlanToolAllowList);
            var disabled = ParseCsvSet(config.DisabledTools);
            return tools.Where(t =>
            {
                var name = t.Name.ToLowerInvariant();
                if (!WorkModeManager.PlanReadOnlyTools.Contains(name)) return false;
                if (planUserAllow is { Count: > 0 } && !planUserAllow.Contains(name)) return false;
                if (disabled is { Count: > 0 } && disabled.Contains(name)) return false;
                return true;
            }).ToList();
        }

        // 3. Build 工作模式：显式白名单（YOLO 覆盖）→ 经济精简 → 黑名单
        string? modeAllow = config.BuildToolAllowList;
        if (PermissionManager.CurrentMode == PermissionManager.Mode.Yolo)
            modeAllow = config.YoloToolAllowList;

        var allowed = ParseCsvSet(modeAllow);
        var disabledAll = ParseCsvSet(config.DisabledTools);

        var result = tools;
        if (allowed is { Count: > 0 })
        {
            // 显式白名单：完全按用户指定，省钱精简不干扰
            result = result.Where(t => allowed.Contains(t.Name.ToLowerInvariant())).ToList();
        }
        else
        {
            // 无白名单：省钱模式按档位去重复精简（关=全量，开=去重，开的越大越精简）
            result = TrimToolsForEconomy(result, config.EconomyMode);
        }
        if (disabledAll is { Count: > 0 })
            result = result.Where(t => !disabledAll.Contains(t.Name.ToLowerInvariant())).ToList();

        if (result.Count < tools.Count)
            DebugLog.Log("tool-filter", $"工具过滤: {tools.Count} → {result.Count} ({agent.WorkMode}, YOLO={PermissionManager.CurrentMode == PermissionManager.Mode.Yolo}, economy={config.EconomyMode})");

        return result;
    }

    /// <summary>把逗号分隔配置解析成小写工具名集合（null/空 → null）。</summary>
    private static HashSet<string>? ParseCsvSet(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? null
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim().ToLowerInvariant()).ToHashSet();
}
