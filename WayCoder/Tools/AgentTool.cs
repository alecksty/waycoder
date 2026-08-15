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

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("task", JNode.Object()
                .Set("type", "string")
                .Set("description", "子智能体应完成的任务（单任务模式）"))
            .Set("tasks", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object()
                    .Set("type", "string")
                    .Set("description", "单个并行子任务"))
                .Set("description", $"并行子任务数组（最多 {MaxParallelTasks} 个），各任务独立上下文并行执行")));

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

        if (tasksObj is JNode { Kind: JKind.Array } jsonArr)
        {
            CollectTaskTexts(jsonArr.Items, taskList);
        }
        else if (tasksObj is System.Collections.IEnumerable enumerable && tasksObj is not string)
        {
            // JsonElementToObject 解析后此处收到 List<object?>，每个元素为 string 标量或
            // Dictionary<string,object?> 嵌套对象（LLM 按 schema 的 items 结构传 {"description":...}）
            CollectTaskTexts(enumerable, taskList);
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
                    if (Json.Parse(trimmed) is JNode { Kind: JKind.Array } arr)
                        CollectTaskTexts(arr.Items, taskList);
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

        try
        {
            var runningTasks = taskList
                .Select(t => RunSubAgentAsync(t, depth, maxDepth))
                .ToList();
            var results = await Task.WhenAll(runningTasks);

            // 聚合结果，总长受 SubAgentParallelTotalMaxChars 上限约束：单个子智能体输出
            // 各截断到 SubAgentOutputMaxChars，但并行 N 个累加仍可能撑爆主智能体上下文。
            var sb = new System.Text.StringBuilder();
            var totalMax = Config.Instance.SubAgentParallelTotalMaxChars;
            var perItemMax = totalMax > 0 && results.Length > 0
                ? Math.Max(200, totalMax / results.Length)
                : -1;
            for (var i = 0; i < results.Length; i++)
            {
                sb.AppendLine($"--- 子任务 {i + 1} ---");
                sb.AppendLine(perItemMax > 0 ? TruncateKeepTail(results[i], perItemMax) : results[i]);
            }
            var summary = $"[并行子智能体完成 · {results.Length} 个任务]\n{sb}";
            return totalMax > 0 && summary.Length > totalMax
                ? TruncateKeepTail(summary, totalMax)
                : summary;
        }
        catch (Exception ex)
        {
            return $"并行子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>执行单个子任务（单任务模式与并行模式共用）。</summary>
    private async Task<string> RunSubAgentAsync(string task, int depth, int maxDepth)
    {
        // 子智能体的工具集：排除危险工具 + 深度限制下排除 agent
        var subTools = ToolRegistry.GetSubAgentTools(ParentAgent!.Tools, depth, maxDepth);

        // 子智能体使用独立的 LLM 实例（Clone），小模型省钱。不再共享父 LlmClient 的
        // ModelOverride —— 并行模式下共享可变状态会竞态（最后完成的子智能体把
        // ModelOverride 恢复成小模型，污染主智能体后续请求降级）。
        var subLLM = ParentAgent!.LlmClient.Clone();
        subLLM.ModelOverride = subLLM.SmallModel;

        // 子智能体轮次：随深度递减（顶层上限可配置）
        var subRounds = Math.Max(5, Config.Instance.SubAgentMaxRounds - depth * 5);

        try
        {
            // 注入父上下文摘要（最近几轮对话），让子智能体了解背景
            var contextSummary = BuildParentContext(depth);
            var depthNote = depth > 0 ? $"\n（当前为第 {depth + 1} 层子智能体，最大深度 {maxDepth}）" : "";
            // 注入子智能体纪律（不建 scratch/csproj、自测到通过、精简回报），固化压力测试铁律
            var discipline = SystemPrompt.SubAgentDiscipline;
            var fullTask = string.IsNullOrEmpty(contextSummary)
                ? $"{discipline}\n\n{task}{depthNote}"
                : $"{contextSummary}\n\n---\n{discipline}\n\n## 子任务{depthNote}\n{task}";

            // 进入下一层深度
            _currentDepth.Value = depth + 1;

            var subAgent = new Agent(subLLM, subTools,
                ParentAgent.Context.MaxTokens, maxRounds: subRounds);

            var result = await subAgent.ChatAsync(fullTask, onToken: null, onTool: null);
            // 截断过长结果，避免撑爆父智能体的上下文（保尾：保留开头实现过程 + 末尾结论）
            var outputMax = Config.Instance.SubAgentOutputMaxChars;
            if (outputMax > 0 && result.Length > outputMax)
                result = TruncateKeepTail(result, outputMax);
            return $"[子智能体已完成 · 深度 {depth + 1}]\n{result}";
        }
        catch (Exception ex)
        {
            return $"子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _currentDepth.Value = depth;
            // 回收子智能体实例的花费统计到父智能体（Clone 后统计独立，否则会丢失）
            ParentAgent!.LlmClient.MergeUsageFrom(subLLM);
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
                    var role = m["role"]?.AsString() ?? "";
                    var content = m["content"]?.AsString() ?? "";
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

    /// <summary>
    /// 保尾截断：保留开头实现过程（前 70%）与末尾结论（后 25%），中间省略。
    /// 旧逻辑只保留开头 90%，会把子智能体末尾的关键结论（如「Automata 7→0」）截掉。
    /// </summary>
    private static string TruncateKeepTail(string text, int maxLen)
    {
        if (maxLen <= 0 || text.Length <= maxLen)
            return text;
        int head = maxLen * 70 / 100;
        int tail = maxLen * 25 / 100;
        if (head + tail >= text.Length)
            return text;
        return text[..head] + $"\n...（中间省略 {text.Length - head - tail} 字符）...\n" + text[^tail..];
    }

    /// <summary>tasks 数组元素里优先提取任务文本的字段名（description 优先，与 schema items 对齐）。</summary>
    private static readonly string[] TaskTextKeys =
        ["description", "task", "name", "title", "text", "prompt", "instruction"];

    /// <summary>
    /// 从任意集合（JsonArray / List&lt;object?&gt; / 解析出的 JsonArray）中提取非空任务文本，
    /// 追加到 taskList。消除 ExecuteParallelAsync 三个解析分支里重复的
    /// 「ExtractTaskText + 非空判断 + Add」样板。
    /// </summary>
    private static void CollectTaskTexts(System.Collections.IEnumerable items, List<string> taskList)
    {
        foreach (var item in items)
        {
            var t = ExtractTaskText(item);
            if (!string.IsNullOrWhiteSpace(t))
                taskList.Add(t);
        }
    }

    /// <summary>
    /// 从 tasks 数组的单个元素中提取任务文本。元素可能为：
    /// - 纯字符串（正常情况，直接返回）
    /// - 对象（LLM 按 schema 的 items 结构传 {"description":"..."}，需提取字段）
    /// - 其他标量（long/bool 等，ToString 兜底）
    /// v0.53.1 修复：此前对象元素被 ToString() 成 "System.Collections.Generic.Dictionary..." 乱码，
    /// 导致子智能体收到不可读任务直接失败。现对 JsonObject / Dictionary 提取 description 等字段。
    /// </summary>
    internal static string? ExtractTaskText(object? item)
    {
        switch (item)
        {
            case null:
                return null;
            case string s:
                return s;
            case JNode jo:
                foreach (var key in TaskTextKeys)
                {
                    var str = jo[key]?.AsString();
                    if (!string.IsNullOrWhiteSpace(str))
                        return str;
                }
                return jo.ToJson();
            case Dictionary<string, object?> dict:
                foreach (var key in TaskTextKeys)
                {
                    if (dict.TryGetValue(key, out var v) && v is string vs && !string.IsNullOrWhiteSpace(vs))
                        return vs;
                }
                // 兜底：无已知字段时取第一个非空字符串值，避免返回 Dictionary 类型名乱码
                foreach (var v in dict.Values)
                {
                    if (v is string s2 && !string.IsNullOrWhiteSpace(s2))
                        return s2;
                }
                return dict.ToString();
            default:
                return item.ToString();
        }
    }
}
