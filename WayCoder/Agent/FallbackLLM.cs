namespace WayCoder;

/// <summary>
/// 模型回退链 —— LLM 调用失败时自动尝试备选模型。
/// 配置回退顺序，设置最大预算。
/// </summary>
public static class FallbackLLM
{
    /// <summary>默认回退链（从 Config.Instance.FallbackChain 读取）</summary>
    public static string[] DefaultFallbackChain =>
        Config.Instance.FallbackChain.Split(',').Select(m => m.Trim()).Where(m => m.Length > 0).ToArray();

    /// <summary>当前回退链</summary>
    public static string[] FallbackChain { get; set; } = null!;  // 首次使用时从 DefaultFallbackChain 读取

    static FallbackLLM()
    {
        FallbackChain = DefaultFallbackChain;
    }

    /// <summary>最大总花费（美元），null 表示无限制。优先从 WAYCODER_FALLBACK_MAX_BUDGET 读取</summary>
    public static double? MaxBudget
    {
        get => Config.Instance.FallbackMaxBudget;
        set => _maxBudget = value;
    }
    private static double? _maxBudget = 5.0;

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
            ErrorLog.LlmError(originalLlm.Model, originalLlm.Endpoint,
                $"主模型失败，启动回退链: {ex.Message}", ex);
        }

        // 回退链
        foreach (var (model, idx) in FallbackChain.Select((m, i) => (m, i)))
        {
            if (model == originalLlm.Model) continue;

            // 预算检查：超过预算时记录警告并跳过回退（优雅降级，不崩溃）
            if (MaxBudget != null && TotalSpent >= MaxBudget)
            {
                Console.Error.WriteLine($"[fallback] ⚠ 已达回退预算上限 ${MaxBudget:F2}，停止尝试备选模型");
                break;
            }

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
                ErrorLog.LlmError(model, fallbackLlm.Endpoint,
                    $"回退模型也失败: {ex.Message}", ex);
                continue;
            }
        }

        Console.Error.WriteLine("[fallback] 所有回退模型均已失败，请检查网络或 API 密钥。");
        ErrorLog.Error("FallbackLLM", "所有回退模型均已失败（包括主模型），请检查网络或 API 密钥");
        return new LLMResponse
        {
            Content = "[错误] 所有模型（含回退链）均已失败，请检查网络或 API 密钥。",
        };
    }

    /// <summary>重置统计</summary>
    public static void Reset()
    {
        TotalSpent = 0;
        FallbackIndex = -1;
    }
}
