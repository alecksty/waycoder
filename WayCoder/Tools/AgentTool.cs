namespace WayCoder.Tools;

/// <summary>
/// 子智能体生成（灵感源自 Claude Code 的 AgentTool）。
///
/// 核心思想：对于复杂的子任务，生成一个独立的智能体，
/// 拥有自己的对话历史和工具访问权限。这让主智能体可以委派工作。
/// 子智能体运行至完成，返回文本摘要。
///
/// v0.17.5: 支持多层递归（最多 MaxDepth 层），深度达到上限时自动移除 agent 工具。
/// v0.18.0: 支持并行子智能体（tasks 数组参数），最多 4 个并发，结果聚合返回。
/// </summary>
public class AgentTool : ITool
{
    public string Name => "agent";
    public string Description => $"生成子智能体来独立处理复杂任务。支持单个任务（task）或并行批量任务（tasks 数组，最多 {MaxParallelTasks} 个并发）。子智能体拥有自己的上下文和工具访问权限，支持多层递归委派。适用于：研究代码库、独立实现多步骤变更，或任何能从全新上下文中获益的任务。并行模式适合同时探索多个独立方向。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["task"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "子智能体应完成的任务（单任务模式）",
            },
            ["tasks"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "单个并行子任务",
                },
                ["description"] = $"并行子任务数组（最多 {MaxParallelTasks} 个），各任务独立上下文并行执行",
            },
        },
    };

    /// <summary>
    /// 由 Agent 在构造后设置，用于访问父智能体。
    /// </summary>
    public Agent? ParentAgent { get; set; }

    /// <summary>最大递归深度（从 Config.Instance.SubAgentMaxDepth 动态读取，可通过 WAYCODER_SUBAGENT_DEPTH 环境变量配置）</summary>
    public static int MaxDepth => Config.Instance.SubAgentMaxDepth;

    /// <summary>并行子任务数量上限（从 Config.Instance.SubAgentMaxParallel 动态读取）</summary>
    public static int MaxParallelTasks => Config.Instance.SubAgentMaxParallel;

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

        // 并行批量任务模式：tasks 数组存在且非空时优先
        if (arguments.TryGetValue("tasks", out var tasksObj) && tasksObj != null)
        {
            return await ExecuteParallelAsync(tasksObj, depth);
        }

        if (string.IsNullOrWhiteSpace(task))
            return "错误：请提供 task（单任务）或 tasks 数组（并行任务）参数";

        return await RunSubAgentAsync(task, depth, maxDepth);
    }

    /// <summary>并行批量执行多个子任务，结果按序聚合返回。</summary>
    private async Task<string> ExecuteParallelAsync(object tasksObj, int depth)
    {
        var taskList = new List<string>();

        if (tasksObj is JsonArray jsonArr)
        {
            foreach (var item in jsonArr)
            {
                var t = item?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(t))
                    taskList.Add(t);
            }
        }
        else if (tasksObj is System.Collections.IEnumerable enumerable && tasksObj is not string)
        {
            // JsonArray 递归解析后此处收到 List<object?>，每个元素为标量或嵌套结构
            foreach (var item in enumerable)
            {
                var t = item?.ToString();
                if (!string.IsNullOrWhiteSpace(t))
                    taskList.Add(t);
            }
        }
        else if (tasksObj is string s)
        {
            // 防御：string 不是字符集合，绝不逐字符遍历。
            // 兼容 LLM 直接返回单个字符串，或旧版把数组序列化成 JSON 字符串的情况。
            var trimmed = s.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    if (JsonNode.Parse(trimmed) is JsonArray arr)
                    {
                        foreach (var item in arr)
                        {
                            var t = item?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(t))
                                taskList.Add(t);
                        }
                    }
                }
                catch { /* 解析失败则退回单任务 */ }
            }
            if (taskList.Count == 0 && !string.IsNullOrWhiteSpace(trimmed))
                taskList.Add(trimmed);
        }

        if (taskList.Count == 0)
            return "错误：tasks 数组不能为空，请提供至少一个子任务";

        if (taskList.Count > MaxParallelTasks)
            return $"错误：并行子任务数量不能超过 {MaxParallelTasks} 个，当前提供了 {taskList.Count} 个。请减少子任务数量或分批次执行。";

        var maxDepth = MaxDepth;

        // 子智能体使用小模型（省钱），继承父模型路径
        var subLLM = ParentAgent!.LlmClient;
        var savedOverride = subLLM.ModelOverride;
        subLLM.ModelOverride = subLLM.SmallModel;

        try
        {
            var runningTasks = taskList
                .Select(t => RunSubAgentAsync(t, depth, maxDepth))
                .ToList();
            var results = await Task.WhenAll(runningTasks);

            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < results.Length; i++)
            {
                sb.AppendLine($"--- 子任务 {i + 1} ---");
                sb.AppendLine(results[i]);
            }
            return $"[并行子智能体完成 · {results.Length} 个任务]\n{sb}";
        }
        catch (Exception ex)
        {
            return $"并行子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            subLLM.ModelOverride = savedOverride;
        }
    }

    /// <summary>执行单个子任务（单任务模式与并行模式共用）。</summary>
    private async Task<string> RunSubAgentAsync(string task, int depth, int maxDepth)
    {
        // 子智能体的工具集：排除危险工具 + 深度限制下排除 agent
        var subTools = ToolRegistry.GetSubAgentTools(ParentAgent!.Tools, depth, maxDepth);

        // 子智能体使用小模型（省钱），继承父模型路径
        var subLLM = ParentAgent!.LlmClient;
        var savedOverride = subLLM.ModelOverride;
        subLLM.ModelOverride = subLLM.SmallModel;

        // 子智能体轮次：随深度递减（顶层上限可配置）
        var subRounds = Math.Max(5, Config.Instance.SubAgentMaxRounds - depth * 5);

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
            var outputMax = Config.Instance.SubAgentOutputMaxChars;
            if (outputMax > 0 && result.Length > outputMax)
                result = result[..(outputMax * 90 / 100)] + "\n...（子智能体输出已截断）";
            return $"[子智能体已完成 · 深度 {depth + 1}]\n{result}";
        }
        catch (Exception ex)
        {
            return $"子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
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
