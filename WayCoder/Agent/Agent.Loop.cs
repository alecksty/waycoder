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
            if (msg["role"]?.AsString() != "assistant") continue;
            var toolCalls = msg["tool_calls"];
            if (toolCalls == null) continue;
            foreach (var tc in toolCalls.Items)
            {
                var id = tc?["id"]?.AsString();
                if (!string.IsNullOrEmpty(id))
                    callIds.Add(id);
            }
        }

        if (callIds.Count == 0) return; // 无工具调用，无需修复

        // 2. 收集所有 tool 结果消息的 tool_call_id
        var resultIds = new HashSet<string>();
        foreach (var msg in Messages)
        {
            if (msg["role"]?.AsString() != "tool") continue;
            var id = msg["tool_call_id"]?.AsString();
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
                if (msg["role"]?.AsString() != "assistant") continue;
                var tcs = msg["tool_calls"];
                if (tcs == null) continue;
                foreach (var tc in tcs.Items)
                {
                    if (tc?["id"]?.AsString() == orphanId)
                    {
                        callMsgIdx = i;
                        toolName = tc["function"]?["name"]?.AsString() ?? "unknown";
                        break;
                    }
                }
                if (callMsgIdx >= 0) break;
            }

            if (callMsgIdx < 0) continue;

            var errorMsg = $"[工具执行被中断] 工具 \"{toolName}\" 的调用未能完成执行。" +
                           $"可能原因：Agent 被中断、网络问题或进程异常退出。请重试或使用其他方法完成此操作。";

            var syntheticResult = JNode.Object()
                .Set("role", "tool")
                .Set("tool_call_id", orphanId)
                .Set("content", errorMsg);

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
            if (msg["role"]?.AsString() != "tool") continue;
            var id = msg["tool_call_id"]?.AsString();
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
    public static OrphanRepairResult TestOrphanRepair(List<JNode> messages)
    {
        var result = new OrphanRepairResult();

        // 收集所有 tool_call ID
        var callIds = new HashSet<string>();
        foreach (var msg in messages)
        {
            if (msg["role"]?.AsString() != "assistant") continue;
            var toolCalls = msg["tool_calls"];
            if (toolCalls == null) continue;
            foreach (var tc in toolCalls.Items)
            {
                var id = tc?["id"]?.AsString();
                if (!string.IsNullOrEmpty(id)) callIds.Add(id);
            }
        }

        // 收集所有 tool result 的 tool_call_id
        var resultIds = new HashSet<string>();
        foreach (var msg in messages)
        {
            if (msg["role"]?.AsString() != "tool") continue;
            var id = msg["tool_call_id"]?.AsString();
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
                if (msg["role"]?.AsString() != "assistant") continue;
                var tcs = msg["tool_calls"];
                if (tcs == null) continue;
                foreach (var tc in tcs.Items)
                {
                    if (tc?["id"]?.AsString() == orphanId)
                    {
                        callMsgIdx = i;
                        toolName = tc["function"]?["name"]?.AsString() ?? "unknown";
                        break;
                    }
                }
                if (callMsgIdx >= 0) break;
            }

            if (callMsgIdx < 0) continue;
            messages.Insert(callMsgIdx + 1, JNode.Object()
                .Set("role", "tool")
                .Set("tool_call_id", orphanId)
                .Set("content", $"[工具执行被中断] 工具 \"{toolName}\" 的调用未能完成执行。"));
            result.OrphanCallsFixed++;
        }

        // 删除孤立 tool-result
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msg = messages[i];
            if (msg["role"]?.AsString() != "tool") continue;
            var id = msg["tool_call_id"]?.AsString();
            if (!string.IsNullOrEmpty(id) && !callIds.Contains(id))
            {
                result.OrphanResultsDetected++;
            }
        }

        // 实际删除
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msg = messages[i];
            if (msg["role"]?.AsString() != "tool") continue;
            var id = msg["tool_call_id"]?.AsString();
            if (!string.IsNullOrEmpty(id) && !callIds.Contains(id))
            {
                messages.RemoveAt(i);
                result.OrphanResultsRemoved++;
            }
        }

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
    private void DetectAndBreakLoop(LLMResponse resp, List<JNode> messages)
    {
        const int outputSnipLen = 2000;

        // 收集本轮已执行的 tool 消息（tool_call_id 匹配 resp.ToolCalls 的 Id）
        var toolIds = new HashSet<string>(resp.ToolCalls.Select(tc => tc.Id));
        var executedCalls = new List<(string Name, string Args, string Output)>();
        foreach (var tc in resp.ToolCalls)
        {
            foreach (var m in messages)
            {
                if (m["role"]?.AsString() == "tool"
                    && m["tool_call_id"]?.AsString() == tc.Id)
                {
                    var output = m["content"]?.AsString() ?? "";
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

            messages.Add(JNode.Object()
                .Set("role", "user")
                .Set("content", nudge));

            // 重置窗口避免连续触发（给 Agent 几轮时间调整）
            _recentActionHashes.Clear();
        }
    }
}
