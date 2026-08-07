namespace CoreCoderSharp.Tools;

/// <summary>
/// 子智能体生成（灵感源自 Claude Code 的 AgentTool）。
///
/// 核心思想：对于复杂的子任务，生成一个独立的智能体，
/// 拥有自己的对话历史和工具访问权限。这让主智能体可以委派工作。
/// 子智能体运行至完成，返回文本摘要。
///
/// v0.17.5: 支持多层递归（最多 MaxDepth 层），深度达到上限时自动移除 agent 工具。
/// </summary>
public class AgentTool : ITool
{
    public string Name => "agent";
    public string Description => "生成一个子智能体来独立处理复杂的子任务。子智能体拥有自己的上下文和工具访问权限，支持最多多层递归委派。适用于：研究代码库、独立实现多步骤变更，或任何能从全新上下文中获益的任务。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["task"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "子智能体应完成的任务",
            },
        },
        ["required"] = new JsonArray("task"),
    };

    /// <summary>
    /// 由 Agent 在构造后设置，用于访问父智能体。
    /// </summary>
    public Agent? ParentAgent { get; set; }

    /// <summary>最大递归深度（可通过 Config 修改）</summary>
    public static int MaxDepth = 3;

    /// <summary>当前递归深度（线程安全，AsyncLocal 确保异步上下文隔离）</summary>
    private static readonly AsyncLocal<int> _currentDepth = new();

    /// <summary>获取当前深度</summary>
    public static int CurrentDepth => _currentDepth.Value;

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var task = arguments.GetValueOrDefault("task")?.ToString() ?? "";

        if (ParentAgent == null)
            return "错误：agent 工具未初始化（没有父智能体）";

        var depth = _currentDepth.Value;
        var maxDepth = MaxDepth;

        // 子智能体的工具集：深度未达上限时保留 agent（允许递归），否则移除
        var subTools = depth < maxDepth - 1
            ? ParentAgent.Tools.ToList()  // 允许递归委派
            : ParentAgent.Tools.Where(t => t.Name != "agent").ToList();  // 最深一层禁止

        // 子智能体使用小模型（省钱），继承父模型路径
        var subLLM = ParentAgent.LlmClient;
        var savedOverride = subLLM.ModelOverride;
        subLLM.ModelOverride = subLLM.SmallModel;

        // 子智能体轮次：随深度递减
        var subRounds = Math.Max(5, 20 - depth * 5);

        try
        {
            // 注入父上下文摘要（最近几轮对话），让子智能体了解背景
            var contextSummary = BuildParentContext(depth);
            var depthNote = depth > 0 ? $"\n（当前为第 {depth + 1} 层子智能体，最大深度 {maxDepth}）" : "";
            var fullTask = string.IsNullOrEmpty(contextSummary)
                ? task + depthNote
                : $"{contextSummary}\n\n---\n## 子任务{depthNote}\n{task}";

            // 进入下一层深度
            _currentDepth.Value = depth + 1;

            var subAgent = new Agent(subLLM, subTools,
                ParentAgent.Context.MaxTokens, maxRounds: subRounds);

            var result = await subAgent.ChatAsync(fullTask, onToken: null, onTool: null);
            // 截断过长结果，避免撑爆父智能体的上下文
            if (result.Length > 5000)
                result = result[..4500] + "\n...（子智能体输出已截断）";
            return $"[子智能体已完成 · 深度 {depth + 1}]\n{result}";
        }
        catch (Exception ex)
        {
            return $"子智能体错误（深度 {depth + 1}）：{ex.Message}";
        }
        finally
        {
            _currentDepth.Value = depth;
            subLLM.ModelOverride = savedOverride;
        }
    }

    /// <summary>提取父智能体最近几轮对话作为上下文摘要。</summary>
    private string BuildParentContext(int depth)
    {
        try
        {
            if (ParentAgent == null || ParentAgent.Messages.Count == 0)
                return "";

            // 取最近 6 条消息（约 3 轮对话）
            var recent = ParentAgent.Messages
                .TakeLast(6)
                .Select(m =>
                {
                    var role = m["role"]?.GetValue<string>() ?? "";
                    var content = m["content"]?.GetValue<string>() ?? "";
                    // 截断每条消息
                    if (content.Length > 300)
                        content = content[..300] + "...";
                    return $"[{role}] {content}";
                });

            var depthInfo = depth > 0 ? $"（父智能体深度: {depth}）\n" : "";
            return depthInfo + "## 父智能体背景（最近对话）\n" + string.Join("\n", recent);
        }
        catch
        {
            return "";
        }
    }
}
