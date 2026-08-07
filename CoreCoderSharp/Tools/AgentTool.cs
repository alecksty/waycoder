namespace CoreCoderSharp.Tools;

/// <summary>
/// 子智能体生成（灵感源自 Claude Code 的 AgentTool）。
///
/// 核心思想：对于复杂的子任务，生成一个独立的智能体，
/// 拥有自己的对话历史和工具访问权限。这让主智能体可以委派工作。
/// 子智能体运行至完成，返回文本摘要。
/// </summary>
public class AgentTool : ITool
{
    public string Name => "agent";
    public string Description => "生成一个子智能体来独立处理复杂的子任务。子智能体拥有自己的上下文和工具访问权限。适用于：研究代码库、独立实现多步骤变更，或任何能从全新上下文中获益的任务。";

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

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var task = arguments.GetValueOrDefault("task")?.ToString() ?? "";

        if (ParentAgent == null)
            return "错误：agent 工具未初始化（没有父智能体）";

        // 子智能体的工具集排除 agent（禁止递归生成子智能体）
        var subTools = ParentAgent.Tools
            .Where(t => t.Name != "agent")
            .ToList();

        // 子智能体使用小模型（省钱），继承父模型路径
        var subLLM = ParentAgent.LlmClient;
        var savedOverride = subLLM.ModelOverride;
        subLLM.ModelOverride = subLLM.SmallModel;

        try
        {
            // 注入父上下文摘要（最近几轮对话），让子智能体了解背景
            var contextSummary = BuildParentContext();
            var fullTask = string.IsNullOrEmpty(contextSummary)
                ? task
                : $"{contextSummary}\n\n---\n## 子任务\n{task}";

            var subAgent = new Agent(subLLM, subTools,
                ParentAgent.Context.MaxTokens, maxRounds: 20);

            var result = await subAgent.ChatAsync(fullTask, onToken: null, onTool: null);
            // 截断过长结果，避免撑爆父智能体的上下文
            if (result.Length > 5000)
                result = result[..4500] + "\n...（子智能体输出已截断）";
            return $"[子智能体已完成]\n{result}";
        }
        catch (Exception ex)
        {
            return $"子智能体错误：{ex.Message}";
        }
        finally
        {
            subLLM.ModelOverride = savedOverride;
        }
    }

    /// <summary>提取父智能体最近几轮对话作为上下文摘要。</summary>
    private string BuildParentContext()
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

            return "## 父智能体背景（最近对话）\n" + string.Join("\n", recent);
        }
        catch
        {
            return "";
        }
    }
}
