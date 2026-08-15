using WayCoder.Tools;

namespace WayCoder;

/// <summary>
/// 工具调用调度器 —— 将一轮 LLM 返回的多个工具调用切分为「执行批次」，
/// 支持 Exclusive 独占串行 + Parallel 有界并发（对标 deepseek-harness 的
/// executionMode + bounded rolling pool），提交时按模型声明顺序回填。
/// </summary>
public static class ToolCallScheduler
{
    /// <summary>并行批次的最大并发数（对标子智能体 4 并发，避免一轮 20 个工具调用同时起 20 个进程）。</summary>
    public const int MaxParallelism = 4;

    /// <summary>
    /// 将按模型声明顺序排列的工具调用切分为执行批次（纯逻辑，便于自测）：
    /// - 连续的 Parallel 工具合并为一批（可并发执行）
    /// - 每个 Exclusive 工具独占一批（与前后批次串行）
    /// 批次间保持模型声明顺序。
    /// </summary>
    /// <param name="calls">按模型声明顺序的工具调用列表</param>
    /// <param name="modeOf">按工具名解析执行模式（未知工具按 Exclusive 保守处理）</param>
    public static List<List<ToolCall>> Partition(
        IReadOnlyList<ToolCall> calls,
        Func<string, ToolExecutionMode> modeOf)
    {
        var batches = new List<List<ToolCall>>();
        List<ToolCall>? parallelRun = null;

        foreach (var tc in calls)
        {
            var mode = modeOf(tc.Name);
            if (mode == ToolExecutionMode.Exclusive)
            {
                if (parallelRun is { Count: > 0 })
                {
                    batches.Add(parallelRun);
                    parallelRun = null;
                }
                batches.Add([tc]);
            }
            else
            {
                parallelRun ??= [];
                parallelRun.Add(tc);
            }
        }

        if (parallelRun is { Count: > 0 })
            batches.Add(parallelRun);

        return batches;
    }
}
