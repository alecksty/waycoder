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
public class AgentTool : ITool, ICancellableTool
{
    public string Name => "agent";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => $"生成子智能体来独立处理复杂任务。支持单个任务（task）或并行批量任务（tasks 数组，每批最多 {MaxParallelTasks} 个并发，超出自动分批串行，硬上限 {MaxTotalTasks} 个）。tasks 元素可为纯字符串，或对象 {{id, description, depends_on}} 表达任务依赖——依赖任务先执行、其输出注入后续任务，实现流水线编排（DAG 分层调度）。子智能体拥有自己的上下文和工具访问权限，支持多层递归委派。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("task", JNode.Object()
                .Set("type", "string")
                .Set("description", "子智能体应完成的任务（单任务模式）"))
            .Set("tasks", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object()
                    .Set("type", "object")
                    .Set("properties", JNode.Object()
                        .Set("description", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "子任务描述"))
                        .Set("id", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "子任务唯一标识（供其他任务的 depends_on 引用；省略则按序号 t0/t1...）"))
                        .Set("depends_on", JNode.Object()
                            .Set("type", "array")
                            .Set("items", JNode.Object().Set("type", "string"))
                            .Set("description", "依赖的子任务 id 列表：这些任务完成后才执行本任务，并注入其输出")))
                    .Set("description", "单个子任务（纯字符串等价于仅 description）"))
                .Set("description", $"子任务数组（每批最多 {MaxParallelTasks} 个并发，超出自动分批串行，硬上限 {MaxTotalTasks} 个），支持 id/depends_on 依赖编排")));

    /// <summary>
    /// 由 Agent 在构造后设置，用于访问父智能体。
    /// </summary>
    public Agent? ParentAgent { get; set; }

    /// <summary>最大递归深度（从 Config.Instance.SubAgentMaxDepth 动态读取，可通过 WAYCODER_SUBAGENT_DEPTH 环境变量配置）</summary>
    public static int MaxDepth => Config.Instance.SubAgentMaxDepth;

    /// <summary>并行子任务数量上限（从 Config.Instance.SubAgentMaxParallel 动态读取）</summary>
    public static int MaxParallelTasks => Config.Instance.SubAgentMaxParallel;

    /// <summary>子任务总数硬上限（从 Config.Instance.SubAgentMaxTotalTasks 动态读取，超出报错防失控）</summary>
    public static int MaxTotalTasks => Config.Instance.SubAgentMaxTotalTasks;

    /// <summary>当前递归深度（线程安全，AsyncLocal 确保异步上下文隔离）</summary>
    private static readonly AsyncLocal<int> _currentDepth = new();

    /// <summary>获取当前深度</summary>
    public static int CurrentDepth => _currentDepth.Value;

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时取消所有子智能体。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var task = arguments.GetValueOrDefault("task")?.ToString() ?? "";

        if (ParentAgent == null)
            return "错误：agent 工具未初始化（没有父智能体）";

        var depth = _currentDepth.Value;
        var maxDepth = MaxDepth;

        // 并行批量任务模式：tasks 数组存在且非空时优先
        if (arguments.TryGetValue("tasks", out var tasksObj) && tasksObj != null)
        {
            return await ExecuteParallelAsync(tasksObj, depth, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(task))
            return "错误：请提供 task（单任务）或 tasks 数组（并行任务）参数";

        return await RunSubAgentWithRetryAsync(task, depth, maxDepth, cancellationToken);
    }

    /// <summary>并行批量执行多个子任务，结果按序聚合返回。带依赖（depends_on）时走 DAG 拓扑调度。</summary>
    private async Task<string> ExecuteParallelAsync(object tasksObj, int depth, CancellationToken cancellationToken)
    {
        var specs = new List<SubTaskSpec>();
        CollectSpecs(tasksObj, specs);

        if (specs.Count == 0)
            return "错误：tasks 数组不能为空，请提供至少一个子任务";

        // 硬上限：防止 LLM 生成海量任务失控（超出并行数的部分本可分批串行，但总数超上限直接拒绝）
        if (specs.Count > MaxTotalTasks)
            return $"错误：子任务总数不能超过 {MaxTotalTasks} 个（当前 {specs.Count} 个）。请拆分任务或缩小范围。";

        var maxDepth = MaxDepth;

        // 任一子任务带 depends_on → 走依赖图（DAG 拓扑分层）调度
        if (specs.Any(s => s.DependsOn.Count > 0))
            return await ExecuteDependencyAsync(specs, depth, maxDepth, cancellationToken);

        try
        {
            // 超大规模分批调度：每批最多 MaxParallelTasks 个并行，批间串行。突破旧「超过并行数即报错」
            // 的限制，支持几十上百个任务自动流水线化（先并 4 个，再并下 4 个……），避免一次性火并撑爆资源。
            var results = new string[specs.Count];
            foreach (var (start, end) in ComputeBatches(specs.Count, MaxParallelTasks))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = new List<Task<string>>(end - start);
                for (int i = start; i < end; i++)
                    batch.Add(RunSubAgentWithRetryAsync(specs[i].Text, depth, maxDepth, cancellationToken));
                var batchResults = await Task.WhenAll(batch);
                for (int i = start; i < end; i++)
                    results[i] = batchResults[i - start];
            }

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
        catch (OperationCanceledException)
        {
            throw; // 中断信号向上传播
        }
        catch (Exception ex)
        {
            return $"并行子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 依赖编排执行（DAG 拓扑分层调度）：Kahn 算法按「入度」逐层放行，每层内并行、
    /// 层间串行，依赖任务的输出注入后续任务。突破纯并行「一次性火并」的限制，
    /// 支持「先分析→再实现」「先骨架→再填肉」等流水线式子任务编排。
    /// </summary>
    private async Task<string> ExecuteDependencyAsync(List<SubTaskSpec> specs, int depth, int maxDepth, CancellationToken cancellationToken)
    {
        try
        {
            List<List<int>> levels;
            try
            {
                levels = TopoSort(specs);
            }
            catch (InvalidOperationException ex)
            {
                return $"错误：{ex.Message}";
            }

            var idMap = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < specs.Count; i++)
                if (!idMap.ContainsKey(specs[i].Id)) idMap[specs[i].Id] = i;

            var results = new string[specs.Count];
            foreach (var level in levels)
            {
                // 每层内分批并行、层间串行：依赖任务的输出已写入 results，供 RunDependentAsync 注入。
                // 单层任务数可能超过 MaxParallelTasks（扇形展开），同样按批串行避免一次性火并。
                foreach (var (start, end) in ComputeBatches(level.Count, MaxParallelTasks))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = new List<Task<string>>(end - start);
                    for (int k = start; k < end; k++)
                        batch.Add(RunDependentAsync(level[k], specs, idMap, results, depth, maxDepth, cancellationToken));
                    var batchResults = await Task.WhenAll(batch);
                    for (int k = start; k < end; k++)
                        results[level[k]] = batchResults[k - start];
                }
            }

            // 聚合结果（保持 specs 原始顺序）
            var sb = new System.Text.StringBuilder();
            var totalMax = Config.Instance.SubAgentParallelTotalMaxChars;
            var perItemMax = totalMax > 0 ? Math.Max(200, totalMax / specs.Count) : -1;
            for (int i = 0; i < specs.Count; i++)
            {
                sb.AppendLine($"--- 子任务 {specs[i].Id} ---");
                sb.AppendLine(perItemMax > 0 ? TruncateKeepTail(results[i], perItemMax) : results[i]);
            }
            var summary = $"[子智能体流水线完成 · {specs.Count} 个任务 · 按依赖拓扑调度]\n{sb}";
            return totalMax > 0 && summary.Length > totalMax ? TruncateKeepTail(summary, totalMax) : summary;
        }
        catch (OperationCanceledException)
        {
            throw; // 中断信号向上传播
        }
        catch (Exception ex)
        {
            return $"依赖编排子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 依赖图拓扑分层（Kahn 算法）：校验 id 存在/自依赖/环，返回每层可并行的任务索引组。
    /// 纯逻辑（无 I/O），供 <see cref="ExecuteDependencyAsync"/> 调度与自测复用。
    /// </summary>
    internal static List<List<int>> TopoSort(List<SubTaskSpec> specs)
    {
        var idMap = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < specs.Count; i++)
            if (!idMap.ContainsKey(specs[i].Id)) idMap[specs[i].Id] = i;

        // indegree[i] = 任务 i 的未完成依赖数；dependents[di] = 依赖 di 的任务集合
        var indegree = new int[specs.Count];
        var dependents = new List<int>[specs.Count];
        for (int i = 0; i < specs.Count; i++) dependents[i] = new List<int>();

        for (int i = 0; i < specs.Count; i++)
        {
            foreach (var dep in specs[i].DependsOn)
            {
                if (!idMap.TryGetValue(dep, out var di))
                    throw new InvalidOperationException($"子任务「{specs[i].Id}」依赖的 id「{dep}」不存在");
                if (di == i)
                    throw new InvalidOperationException($"子任务「{specs[i].Id}」不能依赖自身");
                indegree[i]++;
                dependents[di].Add(i);
            }
        }

        var levels = new List<List<int>>();
        var completed = new bool[specs.Count];
        int done = 0;
        while (done < specs.Count)
        {
            var level = new List<int>();
            for (int i = 0; i < specs.Count; i++)
                if (!completed[i] && indegree[i] == 0)
                    level.Add(i);
            if (level.Count == 0)
                throw new InvalidOperationException("子任务依赖存在环（depends_on 形成循环），无法调度");
            foreach (var i in level)
            {
                completed[i] = true;
                done++;
                foreach (var d in dependents[i]) indegree[d]--;
            }
            levels.Add(level);
        }
        return levels;
    }

    /// <summary>执行带依赖的子任务：把已完成的依赖任务输出注入到任务文本，再交给子智能体。</summary>
    private async Task<string> RunDependentAsync(int idx, List<SubTaskSpec> specs, Dictionary<string, int> idMap, string[] results, int depth, int maxDepth, CancellationToken cancellationToken)
    {
        var spec = specs[idx];
        var task = spec.Text;

        if (spec.DependsOn.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("前置子任务已完成，其输出如下（请基于这些结果继续完成本任务）：");
            var depMax = Config.Instance.SubAgentOutputMaxChars > 0
                ? Config.Instance.SubAgentOutputMaxChars
                : 2000;
            foreach (var depId in spec.DependsOn)
            {
                if (!idMap.TryGetValue(depId, out var di)) continue;
                var outText = results[di];
                if (outText.Length > depMax)
                    outText = TruncateKeepTail(outText, depMax);
                sb.AppendLine($"### 依赖任务「{depId}」输出\n{outText}");
            }
            task = $"{sb}\n\n---\n## 本任务\n{task}";
        }

        return await RunSubAgentWithRetryAsync(task, depth, maxDepth, cancellationToken);
    }

    /// <summary>执行单个子任务（单任务模式与并行模式共用）。</summary>
    private async Task<string> RunSubAgentAsync(string task, int depth, int maxDepth, CancellationToken cancellationToken)
    {
        // 捕获父智能体到局部变量：ParentAgent 是实例字段，虽然修复后每个 Agent 持有
        // 独立实例，但局部捕获保证并行子智能体并发构造期间语义稳定，且读起来更清晰。
        var parent = ParentAgent!;

        // 子智能体的工具集：排除危险工具 + 深度限制下排除 agent
        var subTools = ToolRegistry.GetSubAgentTools(parent.Tools, depth, maxDepth);

        // 子智能体使用独立的 LLM 实例（Clone），小模型省钱。不再共享父 LlmClient 的
        // ModelOverride —— 并行模式下共享可变状态会竞态（最后完成的子智能体把
        // ModelOverride 恢复成小模型，污染主智能体后续请求降级）。
        var subLLM = parent.LlmClient.Clone();
        subLLM.ModelOverride = subLLM.SmallModel;

        // 子智能体轮次：随深度递减（顶层上限可配置）
        var subRounds = Math.Max(5, Config.Instance.SubAgentMaxRounds - depth * 5);

        // 子智能体 cd 泄漏防护：CwdContext.Current 是 static AsyncLocal，子智能体内部 cd 会
        // 沿同一 async 上下文回传污染父智能体 cwd（后续父 bash/edit 相对路径解析错）——执行前保存父值。
        var parentCwd = CwdContext.Current.Value;

        // 明文审计：预先构造任务全文与工具清单，供 finally 统一落盘（成功/失败/中断都留痕）
        var contextSummary = BuildParentContext(depth);
        var depthNote = depth > 0 ? $"\n（当前为第 {depth + 1} 层子智能体，最大深度 {maxDepth}）" : "";
        // 注入子智能体纪律（不建 scratch/csproj、自测到通过、精简回报），固化压力测试铁律
        var discipline = SystemPrompt.SubAgentDiscipline;
        var fullTask = string.IsNullOrEmpty(contextSummary)
            ? $"{discipline}\n\n{task}{depthNote}"
            : $"{contextSummary}\n\n---\n{discipline}\n\n## 子任务{depthNote}\n{task}";
        var toolsSummary = string.Join(", ", subTools.Select(t => t.Name));
        var auditSw = System.Diagnostics.Stopwatch.StartNew();
        string? auditResult = null;

        try
        {
            // 进入下一层深度
            _currentDepth.Value = depth + 1;

            var subAgent = new Agent(subLLM, subTools,
                parent.Context.MaxTokens, maxRounds: subRounds)
            {
                // 继承父智能体的标识，使子智能体文件写与父智能体同源，跨槽位冲突检测仍按槽位归属
                AgentId = parent.AgentId,
            };

            var result = await subAgent.ChatAsync(fullTask, onToken: null, onTool: null, cancellationToken: cancellationToken);
            // 截断过长结果，避免撑爆父智能体的上下文（保尾：保留开头实现过程 + 末尾结论）
            var outputMax = Config.Instance.SubAgentOutputMaxChars;
            if (outputMax > 0 && result.Length > outputMax)
                result = TruncateKeepTail(result, outputMax);
            var final = $"[子智能体已完成 · 深度 {depth + 1}]\n{result}";
            auditResult = final;
            return final;
        }
        catch (OperationCanceledException)
        {
            throw; // 中断信号向上传播
        }
        catch (Exception ex)
        {
            var err = $"子智能体错误（深度 {depth + 1}）：{ex.GetType().Name}: {ex.Message}";
            auditResult = err;
            return err;
        }
        finally
        {
            auditSw.Stop();
            SubAgentAudit.Record(depth + 1, fullTask, toolsSummary, auditResult ?? "(已中断)", auditSw.ElapsedMilliseconds);
            _currentDepth.Value = depth;
            // 回收子智能体实例的花费统计到父智能体（Clone 后统计独立，否则会丢失）
            parent.LlmClient.MergeUsageFrom(subLLM);
            // 恢复父智能体 cwd（子智能体 cd 污染防护）；父未设过 cwd 时回退进程目录
            CwdContext.Current.Value = parentCwd ?? Directory.GetCurrentDirectory();
        }
    }

    /// <summary>判断子智能体结果是否为失败（内部异常被吞并返回的错误文案）。纯逻辑，供自测复用。</summary>
    internal static bool IsSubAgentFailure(string result)
        => result.StartsWith("子智能体错误", StringComparison.Ordinal);

    /// <summary>
    /// 把 [0, total) 切成若干批，每批最多 batchSize 个（末批可不足）。纯逻辑（无 I/O），
    /// 供超大规模分批调度与自测复用——批内并行、批间串行，突破旧「超过并行数即报错」限制。
    /// </summary>
    internal static List<(int Start, int End)> ComputeBatches(int total, int batchSize)
    {
        if (batchSize <= 0) batchSize = 1;
        var batches = new List<(int Start, int End)>();
        for (int start = 0; start < total; start += batchSize)
            batches.Add((start, Math.Min(start + batchSize, total)));
        return batches;
    }

    /// <summary>
    /// 带重试执行单个子任务：检测到失败（返回「子智能体错误」）时，把「换一种方法重试」
    /// 的提示追加到任务文本再重跑，最多重试 <see cref="Config.SubAgentRetryCount"/> 次。
    /// 提高子任务健壮性——LLM 偶发抽风（错误工具选择、中途异常）时给第二次机会。
    /// </summary>
    private async Task<string> RunSubAgentWithRetryAsync(string task, int depth, int maxDepth, CancellationToken cancellationToken)
    {
        var retryCount = Config.Instance.SubAgentRetryCount;
        var result = await RunSubAgentAsync(task, depth, maxDepth, cancellationToken);
        for (int attempt = 0; attempt < retryCount && IsSubAgentFailure(result); attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var retryTask = $"{task}\n\n（上次尝试失败，请换一种方法重新尝试：避免重复同样的错误）";
            result = await RunSubAgentAsync(retryTask, depth, maxDepth, cancellationToken);
        }
        return result;
    }

    /// <summary>提取父智能体最近几轮对话作为上下文摘要。</summary>
    private string BuildParentContext(int depth)
    {
        try
        {
            var parentMsgs = ParentAgent?.SnapshotMessages();
            if (parentMsgs == null || parentMsgs.Count == 0)
                return "";

            // 取最近 6 条消息（约 3 轮对话）
            var recent = parentMsgs
                .TakeLast(6)
                .Select(m =>
                {
                    var role = m["role"]?.AsString() ?? "";
                    var content = m["content"]?.AsString() ?? "";
                    // 截断每条消息
                    if (content.Length > 300)
                        content = ContextManager.TruncateByRunes(content, 300) + "...";
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
        return ContextManager.TruncateByRunes(text, head) + $"\n...（中间省略 {text.Length - head - tail} 字符）...\n" + ContextManager.TruncateTailByRunes(text, tail);
    }

    /// <summary>tasks 数组元素里优先提取任务文本的字段名（description 优先，与 schema items 对齐）。</summary>
    private static readonly string[] TaskTextKeys =
        ["description", "task", "name", "title", "text", "prompt", "instruction"];

    /// <summary>单个子任务规格：文本 + 逻辑 id + 依赖 id 列表（纯字符串任务等价于仅 Text）。</summary>
    internal sealed class SubTaskSpec
    {
        public string Id = "";
        public string Text = "";
        public List<string> DependsOn = [];
    }

    /// <summary>
    /// 从 tasks 参数（JNode 数组 / List&lt;object?&gt; / JSON 字符串 / 裸字符串）解析出子任务规格列表，
    /// 统一三个形态分支的解析样板。纯字符串元素默认 id 为 t0/t1/...，无依赖。
    /// </summary>
    private static void CollectSpecs(object tasksObj, List<SubTaskSpec> specs)
    {
        if (tasksObj is JNode { Kind: JKind.Array } jsonArr)
        {
            int i = 0;
            foreach (var item in jsonArr.Items)
            {
                var spec = ExtractSpec(item, i++);
                if (!string.IsNullOrWhiteSpace(spec.Text)) specs.Add(spec);
            }
        }
        else if (tasksObj is System.Collections.IEnumerable enumerable && tasksObj is not string)
        {
            // JsonElementToObject 解析后此处收到 List<object?>，每个元素为 string 标量或
            // Dictionary<string,object?> 嵌套对象（LLM 按 schema 的 items 结构传 {"description":...}）
            int i = 0;
            foreach (var item in enumerable)
            {
                var spec = ExtractSpec(item, i++);
                if (!string.IsNullOrWhiteSpace(spec.Text)) specs.Add(spec);
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
                    if (Json.Parse(trimmed) is JNode { Kind: JKind.Array } arr)
                    {
                        int i = 0;
                        foreach (var item in arr.Items)
                        {
                            var spec = ExtractSpec(item, i++);
                            if (!string.IsNullOrWhiteSpace(spec.Text)) specs.Add(spec);
                        }
                    }
                }
                catch { /* 解析失败则退回单任务 */ }
            }
            if (specs.Count == 0 && !string.IsNullOrWhiteSpace(trimmed))
                specs.Add(new SubTaskSpec { Id = "t0", Text = trimmed });
        }
    }

    /// <summary>从 tasks 数组单个元素解析子任务规格（文本 + id + depends_on），纯字符串元素无 id/依赖。</summary>
    internal static SubTaskSpec ExtractSpec(object? item, int index)
    {
        var spec = new SubTaskSpec { Id = $"t{index}", Text = ExtractTaskText(item) ?? "" };

        switch (item)
        {
            case JNode jo:
                var idStr = jo["id"]?.AsString();
                if (!string.IsNullOrWhiteSpace(idStr)) spec.Id = idStr;
                CollectDependsOn(jo["depends_on"], spec.DependsOn);
                break;
            case Dictionary<string, object?> dict:
                if (dict.TryGetValue("id", out var idv) && idv is string ids && !string.IsNullOrWhiteSpace(ids))
                    spec.Id = ids;
                if (dict.TryGetValue("depends_on", out var dv))
                    CollectDependsOn(dv, spec.DependsOn);
                break;
        }
        return spec;
    }

    /// <summary>从 depends_on 字段解析依赖 id 列表（兼容字符串数组 / 单个字符串 / JNode 数组）。</summary>
    internal static void CollectDependsOn(object? value, List<string> deps)
    {
        switch (value)
        {
            case null:
                break;
            case string s:
                if (!string.IsNullOrWhiteSpace(s)) deps.Add(s);
                break;
            case JNode { Kind: JKind.Array } arr:
                foreach (var it in arr.Items)
                {
                    var s = it.AsString();
                    if (!string.IsNullOrWhiteSpace(s)) deps.Add(s);
                }
                break;
            case JNode other:
                var s2 = other.AsString();
                if (!string.IsNullOrWhiteSpace(s2)) deps.Add(s2);
                break;
            case System.Collections.IEnumerable en:
                foreach (var it in en)
                {
                    var s3 = it?.ToString();
                    if (!string.IsNullOrWhiteSpace(s3)) deps.Add(s3);
                }
                break;
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
