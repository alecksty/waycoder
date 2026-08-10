namespace WayCoder;

/// <summary>
/// 模型回退链 —— LLM 调用失败时自动尝试备选模型。
/// 配置回退顺序，设置最大预算。
/// </summary>
public static class FallbackLLM
{
    /// <summary>默认回退链（按优先级）</summary>
    public static readonly string[] DefaultFallbackChain =
        ["deepseek-v4-flash", "deepseek-v4-pro", "deepseek-chat", "gpt-5.4-mini"];

    /// <summary>当前回退链</summary>
    public static string[] FallbackChain { get; set; } = DefaultFallbackChain;

    /// <summary>最大总花费（美元），null 表示无限制</summary>
    public static double? MaxBudget { get; set; } = 5.0;

    /// <summary>总花费跟踪</summary>
    public static double TotalSpent { get; private set; }

    /// <summary>当前使用的模型索引（-1 表示用原模型）</summary>
    public static int FallbackIndex { get; private set; } = -1;

    /// <summary>
    /// 尝试用回退模型执行。成功返回响应，失败尝试下一个模型。
    /// </summary>
    public static async Task<LLMResponse> TryWithFallback(
        LLM originalLlm,
        List<JsonObject> messages,
        List<JsonObject> tools,
        Action<string>? onToken,
        CancellationToken ct)
    {
        // 先尝试原模型
        FallbackIndex = -1;
        try
        {
            var resp = await originalLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
            TotalSpent += originalLlm.EstimatedCost ?? 0;
            return resp;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"[fallback] 模型 {originalLlm.Model} 失败: {ex.Message}");
        }

        // 回退链
        foreach (var (model, idx) in FallbackChain.Select((m, i) => (m, i)))
        {
            if (model == originalLlm.Model) continue;

            // 预算检查
            if (MaxBudget != null && TotalSpent >= MaxBudget)
                throw new InvalidOperationException($"已达最大预算 ${MaxBudget:F2}");

            FallbackIndex = idx;
            var fallbackLlm = new LLM(model, originalLlm.ApiKey, originalLlm.BaseUrl,
                originalLlm.MaxTokens, originalLlm.Temperature);

            try
            {
                Console.Error.WriteLine($"[fallback] 尝试 {model}...");
                var resp = await fallbackLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
                TotalSpent += fallbackLlm.EstimatedCost ?? 0;

                // 回退成功，更新原始 LLM 的模型
                originalLlm.Model = model;
                Console.Error.WriteLine($"[fallback] ✓ 已切换到 {model}");
                return resp;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"[fallback] {model} 也失败: {ex.Message}");
                continue;
            }
        }

        throw new InvalidOperationException("所有回退模型均已失败，请检查网络或 API 密钥。");
    }

    /// <summary>重置统计</summary>
    public static void Reset()
    {
        TotalSpent = 0;
        FallbackIndex = -1;
    }
}
