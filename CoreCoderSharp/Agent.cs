using System.Text.RegularExpressions;
using CoreCoderSharp.Tools;

namespace CoreCoderSharp;

/// <summary>
/// 核心智能体循环。这是 CoreCoder 的心脏。
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
    public LLM LlmClient { get; }
    public List<ITool> Tools { get; }
    public Dictionary<string, ITool> ToolByName { get; }
    public List<JsonObject> Messages { get; set; } = [];
    public ContextManager Context { get; }

    private readonly int _maxRounds;
    private readonly string _systemPrompt;

    public Agent(LLM llm, List<ITool>? tools = null,
        int maxContextTokens = 128_000, int maxRounds = 50)
    {
        LlmClient = llm;
        Tools = tools ?? ToolRegistry.AllTools;
        ToolByName = Tools.ToDictionary(t => t.Name);
        Context = new ContextManager(maxContextTokens);
        _maxRounds = maxRounds;
        _systemPrompt = SystemPrompt.Generate(Tools);

        // 连接子智能体能力
        foreach (var t in Tools)
        {
            if (t is AgentTool agentTool)
                agentTool.ParentAgent = this;
        }
    }

    /// <summary>
    /// 构建完整消息列表（包含系统提示词）。
    /// </summary>
    private List<JsonObject> FullMessages()
    {
        var result = new List<JsonObject>
        {
            new() { ["role"] = "system", ["content"] = _systemPrompt },
        };
        // 深克隆消息，避免 JsonNode 的 Parent 冲突（同一消息不能加入两个树）
        foreach (var m in Messages)
            result.Add(JsonNode.Parse(m.ToJsonString())!.AsObject());
        return result;
    }

    /// <summary>
    /// 获取工具 schema 列表。
    /// </summary>
    private List<JsonObject> ToolSchemas() => Tools.Select(t => t.Schema()).ToList();

    /// <summary>
    /// 处理一条用户消息。可能涉及多轮 LLM/工具交互。
    /// </summary>
    public async Task<string> ChatAsync(
        string userInput,
        Action<string>? onToken = null,
        Action<string, string>? onTool = null,
        CancellationToken cancellationToken = default)
    {
        Messages.Add(new JsonObject { ["role"] = "user", ["content"] = userInput });
        await Context.MaybeCompressAsync(Messages, LlmClient);

        for (int round = 0; round < _maxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resp = await LlmClient.ChatAsync(
                messages: FullMessages(),
                tools: ToolSchemas(),
                onToken: onToken,
                cancellationToken: cancellationToken);

            // 没有工具调用 -> LLM 完成，返回文本
            if (resp.ToolCalls.Count == 0)
            {
                Messages.Add(resp.ToMessage());
                return resp.Content;
            }

            // 有工具调用 -> 执行（多个时并行）
            Messages.Add(resp.ToMessage());

            try
            {
                if (resp.ToolCalls.Count == 1)
                {
                    var tc = resp.ToolCalls[0];
                    onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                    var result = await ExecuteToolAsync(tc);
                    // 自动 lint 反馈闭环：写文件后立即检查，错误注入工具结果
                    result = await AppendLintFeedbackAsync(tc, result);
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = tc.Id,
                        ["content"] = result,
                    });
                }
                else
                {
                    // 多个工具调用时并行执行
                    var tasks = resp.ToolCalls.Select(async tc =>
                    {
                        onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                        var result = await ExecuteToolAsync(tc);
                        return (tc, result);
                    });

                    var results = await Task.WhenAll(tasks);
                    foreach (var (tc, result) in results)
                    {
                        // 自动 lint 反馈闭环
                        var finalResult = await AppendLintFeedbackAsync(tc, result);
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

            // 如果工具输出太大则压缩上下文
            await Context.MaybeCompressAsync(Messages, LlmClient);
        }

        return "（已达到最大工具调用轮次）";
    }

    /// <summary>
    /// 执行单个工具调用，返回结果字符串。
    /// </summary>
    private async Task<string> ExecuteToolAsync(ToolCall tc)
    {
        if (!ToolByName.TryGetValue(tc.Name, out var tool))
            return $"错误：未知工具 '{tc.Name}'";

        try
        {
            // 权限检查：危险操作需要用户确认
            if (!await PermissionManager.CheckAsync(tc.Name, tc.Arguments))
                return "用户取消了此操作。";

            var result = await tool.ExecuteAsync(tc.Arguments);
            DebugLog.LogToolResult(tc.Name, result);
            return result;
        }
        catch (Exception ex)
        {
            return $"执行 {tc.Name} 时出错：{ex.Message}";
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
}
