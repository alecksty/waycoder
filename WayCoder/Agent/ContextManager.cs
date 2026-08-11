using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 多层上下文压缩。
///
/// WayCoder 以 3 层实现：
///   - 第 1 层（tool_snip）：用截断版本替换冗长的工具结果
///   - 第 2 层（summarize）：LLM 驱动的旧对话摘要
///   - 第 3 层（hard_collapse）：最后手段：仅保留摘要 + 最近消息
///
/// 此外支持 Crush 风格的 StopWhen：基于真实 API token 使用量，
/// 当剩余窗口低于阈值时提前触发摘要（而非等估算值超限）。
/// </summary>
public class ContextManager
{
    public int MaxTokens { get; }

    private readonly int _snipAt;       // 50% -> 裁剪工具输出
    private readonly int _summarizeAt;  // 70% -> LLM 摘要
    private readonly int _collapseAt;   // 90% -> 硬折叠

    /// <summary>累计 prompt tokens（来自 API usage）</summary>
    public int CumulativePromptTokens { get; private set; }
    /// <summary>累计 completion tokens（来自 API usage）</summary>
    public int CumulativeCompletionTokens { get; private set; }

    /// <summary>上次摘要后是否已注入继续提示</summary>
    public bool ContinuePromptInjected { get; set; }

    public ContextManager(int maxTokens = 128_000)
    {
        MaxTokens = maxTokens;
        _snipAt = maxTokens * Config.Instance.ContextSnipRatio / 100;
        _summarizeAt = maxTokens * Config.Instance.ContextSummarizeRatio / 100;
        _collapseAt = maxTokens * Config.Instance.ContextCollapseRatio / 100;
    }

    /// <summary>
    /// 从 LLM 响应中累积真实 token 使用量。
    /// </summary>
    public void AddUsage(int promptTokens, int completionTokens)
    {
        CumulativePromptTokens += promptTokens;
        CumulativeCompletionTokens += completionTokens;
    }

    /// <summary>
    /// Crush 风格 StopWhen：检查真实 token 使用量是否接近窗口上限。
    /// 大窗口用固定 buffer，小窗口用比例阈值。
    /// </summary>
    /// <returns>剩余 token 数低于阈值时应触发摘要</returns>
    public bool ShouldStopAndSummarize()
    {
        var cfg = Config.Instance;
        var used = CumulativePromptTokens + CumulativeCompletionTokens;
        var remaining = MaxTokens - used;

        long threshold;
        if (MaxTokens > cfg.ContextWindowLargeThreshold)
            threshold = cfg.ContextWindowLargeBuffer;       // 大窗口：固定 20K buffer
        else
            threshold = (long)(MaxTokens * cfg.ContextWindowSmallRatio);  // 小窗口：20% 比例

        return remaining <= threshold;
    }

    /// <summary>重置累计 token 计数（摘要后调用）</summary>
    public void ResetUsage()
    {
        CumulativePromptTokens = 0;
        CumulativeCompletionTokens = 0;
        ContinuePromptInjected = false;
    }

    /// <summary>
    /// 按需应用压缩层。返回是否发生了压缩。
    /// </summary>
    public async Task<bool> MaybeCompressAsync(List<JsonObject> messages, LLM? llm,
        Action<int, string>? onProgress = null)
    {
        var current = EstimateTokens(messages);
        var compressed = false;

        // 第 1 层：裁剪冗长的工具输出
        if (current > _snipAt)
        {
            onProgress?.Invoke(1, $"裁剪工具输出... ({current}/{MaxTokens})");
            if (SnipToolOutputs(messages))
            {
                compressed = true;
                current = EstimateTokens(messages);
                onProgress?.Invoke(1, $"裁剪完成 → {current}/{MaxTokens}");
            }
        }

        // 第 2 层：LLM 驱动的旧对话摘要
        if (current > _summarizeAt && messages.Count > 10)
        {
            onProgress?.Invoke(2, $"正在摘要旧对话... ({current}/{MaxTokens})");
            if (await SummarizeOldAsync(messages, llm, keepRecent: 8))
            {
                compressed = true;
                current = EstimateTokens(messages);
                onProgress?.Invoke(2, $"摘要完成 → {current}/{MaxTokens}");
            }
        }

        // 第 3 层：硬折叠——最后手段
        if (current > _collapseAt && messages.Count > 4)
        {
            onProgress?.Invoke(3, $"紧急压缩... ({current}/{MaxTokens})");
            await HardCollapseAsync(messages, llm);
            compressed = true;
            current = EstimateTokens(messages);
            onProgress?.Invoke(3, $"压缩完成 → {current}/{MaxTokens}");
        }

        return compressed;
    }

    /// <summary>
    /// CJK 感知 token 估算。CJK 字符约 1.5 tok/char，ASCII 约 0.25 tok/char（≈4 chars/tok）。
    /// 精度从 ±30% 提升至 ±15%。
    /// </summary>
    public static int EstimateTokens(List<JsonObject> messages)
    {
        var total = 0;
        foreach (var m in messages)
        {
            if (m["content"]?.GetValue<string>() is { } content)
                total += EstimateTokensText(content);
            if (m["tool_calls"] != null)
                total += EstimateTokensText(m["tool_calls"]!.ToJsonString());
        }
        return total;
    }

    /// <summary>对单段文本做 CJK 感知 token 估算。</summary>
    private static int EstimateTokensText(string text)
    {
        int cjk = 0, ascii = 0;
        foreach (var r in text.EnumerateRunes())
        {
            var v = r.Value;
            // CJK 统一汉字 + 扩展区 + 兼容汉字
            if ((v >= 0x4E00 && v <= 0x9FFF) ||
                (v >= 0x3400 && v <= 0x4DBF) ||
                (v >= 0x20000 && v <= 0x2A6DF) ||
                (v >= 0xF900 && v <= 0xFAFF))
                cjk++;
            else if (v <= 127)
                ascii++;
            else
                cjk++; // 其他非 ASCII（日韩、Emoji 等）按宽字符计
        }
        return (int)(cjk * 1.5 + ascii * 0.25);
    }

    /// <summary>
    /// 第 1 层：将超过 1500 字符的工具结果裁剪为首尾几行。
    /// </summary>
    public static bool SnipToolOutputs(List<JsonObject> messages)
    {
        var changed = false;
        foreach (var m in messages)
        {
            if (m["role"]?.GetValue<string>() != "tool") continue;
            var content = m["content"]?.GetValue<string>();
            if (string.IsNullOrEmpty(content) || content.Length <= 1500) continue;

            var lines = content.Split('\n');
            if (lines.Length <= 6) continue;

            // 保留前 3 行 + 后 3 行
            var snipped = string.Join("\n", lines.Take(3))
                + $"\n...（共 {lines.Length} 行，已裁剪以节省上下文）...\n"
                + string.Join("\n", lines.TakeLast(3));

            m["content"] = snipped;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// 计算保留尾部应从哪个索引开始。
    /// 确保 tool 消息不会与产生它的 assistant 消息分离。
    /// </summary>
    public static int SafeSplit(List<JsonObject> messages, int keepRecent)
    {
        var split = Math.Max(0, messages.Count - keepRecent);
        while (split > 0 && messages[split]["role"]?.GetValue<string>() == "tool")
            split--;
        return split;
    }

    /// <summary>
    /// 第 2 层：摘要旧对话，保持最近消息不变。
    /// </summary>
    private async Task<bool> SummarizeOldAsync(List<JsonObject> messages, LLM? llm, int keepRecent = 8)
    {
        if (messages.Count <= keepRecent) return false;

        var split = SafeSplit(messages, keepRecent);
        var old = messages.GetRange(0, split);
        var tail = messages.GetRange(split, messages.Count - split);

        var summary = await GetSummaryAsync(old, llm);

        messages.Clear();
        messages.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = $"[上下文已压缩 - 对话摘要]\n{summary}",
        });
        messages.Add(new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = "收到，我已了解之前对话的上下文。",
        });
        messages.AddRange(tail);
        return true;
    }

    /// <summary>
    /// 第 3 层：紧急压缩。仅保留最后 4 条消息 + 摘要。
    /// </summary>
    private async Task HardCollapseAsync(List<JsonObject> messages, LLM? llm)
    {
        var keep = messages.Count > 4 ? 4 : 2;
        var split = SafeSplit(messages, keep);
        var tail = messages.GetRange(split, messages.Count - split);
        var summary = await GetSummaryAsync(messages.GetRange(0, split), llm);

        messages.Clear();
        messages.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = $"[硬重置上下文]\n{summary}",
        });
        messages.Add(new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = "上下文已恢复。从之前中断的地方继续。",
        });
        messages.AddRange(tail);
    }

    /// <summary>
    /// 通过 LLM 生成摘要，或回退到提取关键信息。
    /// </summary>
    private async Task<string> GetSummaryAsync(List<JsonObject> messages, LLM? llm)
    {
        var flat = FlattenMessages(messages);

        if (llm != null)
        {
            try
            {
                var resp = await llm.ChatAsync(
                    messages:
                    [
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = "将此对话压缩为简要摘要。" +
                                          "保留：已编辑的文件路径、已做出的关键决策、" +
                                          "遇到的错误、当前任务状态。" +
                                          "丢弃：冗长的命令输出、代码清单、" +
                                          "重复的来回对话。",
                        },
                        new JsonObject { ["role"] = "user", ["content"] = flat.Length > 15000 ? flat[..15000] : flat },
                    ]
                );
                return resp.Content;
            }
            catch
            {
                // LLM 摘要失败，回退到提取
            }
        }

        // 回退方案：提取文件路径和错误
        return ExtractKeyInfo(messages);
    }

    private static string FlattenMessages(List<JsonObject> messages)
    {
        var parts = new List<string>();
        foreach (var m in messages)
        {
            var role = m["role"]?.GetValue<string>() ?? "?";
            var text = m["content"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrEmpty(text))
                parts.Add($"[{role}] {text[..Math.Min(400, text.Length)]}");
        }
        return string.Join("\n", parts);
    }

    /// <summary>
    /// 回退方案：无需 LLM，提取文件路径、错误和决策。
    /// </summary>
    private static string ExtractKeyInfo(List<JsonObject> messages)
    {
        var filesSeen = new HashSet<string>();
        var errors = new List<string>();
        var fileRegex = new Regex(@"[\w./\-]+\.\w{1,5}", RegexOptions.None, TimeSpan.FromSeconds(1));

        foreach (var m in messages)
        {
            var text = m["content"]?.GetValue<string>() ?? "";
            // 提取文件路径
            foreach (Match match in fileRegex.Matches(text))
                filesSeen.Add(match.Value);
            // 提取错误行
            foreach (var line in text.Split('\n'))
                if (line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    errors.Add(line.Trim()[..Math.Min(150, line.Trim().Length)]);
        }

        var parts = new List<string>();
        if (filesSeen.Count > 0)
            parts.Add($"涉及的文件：{string.Join(", ", filesSeen.OrderBy(f => f).Take(20))}");
        if (errors.Count > 0)
            parts.Add($"遇到的错误：{string.Join("；", errors.Take(5))}");

        return parts.Count > 0 ? string.Join("\n", parts) : "（无可提取的上下文）";
    }
}
