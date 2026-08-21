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
        set => Config.Instance.FallbackMaxBudget = value;
    }

    /// <summary>总花费跟踪（原子累加：多槽位 Agent 并发回退时避免丢失 increment）</summary>
    public static double TotalSpent { get; private set; }

    /// <summary>当前使用的模型索引（-1 表示用原模型）</summary>
    public static int FallbackIndex { get; private set; } = -1;

    /// <summary>保护 TotalSpent 跨线程累加/读取的锁（double 的 += 非原子，并发回退会丢增量）</summary>
    private static readonly object _stateLock = new();

    internal static void AddSpent(double cost)
    {
        lock (_stateLock) TotalSpent += cost;
    }

    private static bool BudgetExceeded()
    {
        lock (_stateLock) return MaxBudget != null && TotalSpent >= MaxBudget;
    }

    /// <summary>
    /// 尝试用回退模型执行。成功返回响应，失败尝试下一个模型。
    /// </summary>
    public static async Task<LLMResponse> TryWithFallback(
        LLM originalLlm,
        List<JNode> messages,
        List<JNode> tools,
        Action<string>? onToken,
        CancellationToken ct)
    {
        // 先尝试原模型
        FallbackIndex = -1;
        try
        {
            var resp = await originalLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
            AddSpent(originalLlm.EstimatedCost ?? 0);
            return resp;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Console.Error.WriteLine($"[fallback] 模型 {originalLlm.Model} 失败: {ex.Message}");
            ErrorLog.LlmError(originalLlm.Model, originalLlm.Endpoint,
                $"主模型失败，启动回退链: {ex.Message}", ex);
        }

        // 回退链
        int skipped = 0;
        foreach (var (model, idx) in FallbackChain.Select((m, i) => (m, i)))
        {
            if (model == originalLlm.Model) continue;

            // 预算检查：超过预算时记录警告并跳过回退（优雅降级，不崩溃）
            if (BudgetExceeded())
            {
                Console.Error.WriteLine($"[fallback] ⚠ 已达回退预算上限 ${MaxBudget:F2}，停止尝试备选模型");
                break;
            }

            FallbackIndex = idx;

            // 解析跨供应商 API Key 和 BaseUrl
            var (fbKey, fbUrl) = ResolveKeyAndUrl(model, originalLlm.ApiKey);
            if (string.IsNullOrEmpty(fbKey))
            {
                Console.Error.WriteLine($"[fallback] ⏭ 跳过 {model}（无 API Key，设置 {GetKeyEnvName(model)} 环境变量）");
                skipped++;
                continue;
            }

            var fallbackLlm = new LLM(model, fbKey, fbUrl,
                originalLlm.MaxTokens, originalLlm.Temperature);

            try
            {
                Console.Error.WriteLine($"[fallback] 尝试 {model}...");
                var resp = await fallbackLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
                AddSpent(fallbackLlm.EstimatedCost ?? 0);

                // 回退成功，更新原始 LLM 的模型
                originalLlm.Model = model;
                Console.Error.WriteLine($"[fallback] ✓ 已切换到 {model}");
                return resp;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                Console.Error.WriteLine($"[fallback] {model} 也失败: {ex.Message}");
                ErrorLog.LlmError(model, fallbackLlm.Endpoint,
                    $"回退模型也失败: {ex.Message}", ex);
                continue;
            }
        }

        // 全部回退模型失败或跳过 → 给原始模型最后一次重试机会（临时错误可能已恢复）
        string skipMsg = skipped > 0 ? $"（{skipped} 个因无 Key 跳过）" : "";
        Console.Error.WriteLine($"[fallback] 所有回退模型均失败{skipMsg}，最后一次重试原始模型 {originalLlm.Model}...");
        try
        {
            var resp = await originalLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
            AddSpent(originalLlm.EstimatedCost ?? 0);
            FallbackIndex = -1;
            Console.Error.WriteLine($"[fallback] ✓ 原始模型 {originalLlm.Model} 重试成功");
            return resp;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Console.Error.WriteLine($"[fallback] 原始模型 {originalLlm.Model} 重试也失败: {ex.Message}");
        }

        Console.Error.WriteLine("[fallback] 所有回退模型均已失败，请检查网络或 API 密钥。");
        ErrorLog.Error("FallbackLLM", "所有回退模型均已失败（包括主模型），请检查网络或 API 密钥");
        return new LLMResponse
        {
            Content = "[错误] 所有模型（含回退链）均已失败，请检查网络或 API 密钥。会话已自动保存，修复网络/API Key 后可恢复。",
            IsFatalError = true,
        };
    }

    /// <summary>重置统计</summary>
    public static void Reset()
    {
        lock (_stateLock)
        {
            TotalSpent = 0;
            FallbackIndex = -1;
        }
    }

    /// <summary>
    /// 根据模型查找其供应商，解析对应的 API Key 和 BaseUrl。
    /// </summary>
    private static (string? key, string? baseUrl) ResolveKeyAndUrl(string model, string originalKey)
    {
        var info = ModelCatalog.Find(model);
        var provider = info?.ProviderId ?? "";

        // 两层架构：provider 承载唯一地址（地址不同=不同服务商），provider 地址优先，模型默认地址兜底
        //（否则 qwen-turbo/glm-4-flash 等 DefaultBaseUrl=null 的模型会把 key 发到 OpenAI 端点）
        var baseUrl = (provider.Length > 0
                && ModelCatalog.Providers.TryGetValue(provider, out var p)
                && !string.IsNullOrEmpty(p.DefaultBaseUrl)
                ? p.DefaultBaseUrl : null)
            ?? info?.DefaultBaseUrl;

        // 按供应商查找专用密钥，回退到通用密钥
        var key = provider switch
        {
            "deepseek" => Env("DEEPSEEK_API_KEY"),
            "openai" => Env("OPENAI_API_KEY"),
            "google" => Env("GEMINI_API_KEY") ?? Env("GOOGLE_API_KEY"),
            "anthropic" => Env("ANTHROPIC_API_KEY"),
            "qwen" => Env("DASHSCOPE_API_KEY"),
            "zhipu" => Env("ZHIPU_API_KEY") ?? Env("GLM_API_KEY"),
            "bytedance" => Env("ARK_API_KEY") ?? Env("DOUBAO_API_KEY"),
            "moonshot" => Env("MOONSHOT_API_KEY"),
            "mistral" => Env("MISTRAL_API_KEY"),
            "xai" => Env("XAI_API_KEY") ?? Env("GROK_API_KEY"),
            "siliconflow" => Env("SILICONFLOW_API_KEY"),
            "groq" => Env("GROQ_API_KEY"),
            "together" => Env("TOGETHER_API_KEY"),
            "openrouter" => Env("OPENROUTER_API_KEY"),
            "local" => "ollama",   // 本地模型不需要密钥
            _ => null,
        };

        // 回退: WAYCODER_API_KEY → API_KEY → 原始密钥
        key ??= Env("WAYCODER_API_KEY") ?? Env("API_KEY") ?? originalKey;

        return (key, baseUrl);
    }

    /// <summary>获取模型对应的 API Key 环境变量名（用于提示）</summary>
    private static string GetKeyEnvName(string model)
    {
        var info = ModelCatalog.Find(model);
        return info?.ProviderId switch
        {
            "deepseek" => "DEEPSEEK_API_KEY",
            "openai" => "OPENAI_API_KEY",
            "google" => "GEMINI_API_KEY",
            "anthropic" => "ANTHROPIC_API_KEY",
            "qwen" => "DASHSCOPE_API_KEY",
            "zhipu" => "ZHIPU_API_KEY",
            "bytedance" => "ARK_API_KEY",
            "moonshot" => "MOONSHOT_API_KEY",
            "mistral" => "MISTRAL_API_KEY",
            "xai" => "XAI_API_KEY",
            "siliconflow" => "SILICONFLOW_API_KEY",
            "groq" => "GROQ_API_KEY",
            "together" => "TOGETHER_API_KEY",
            "openrouter" => "OPENROUTER_API_KEY",
            "local" => "(本地模型无需密钥)",
            _ => "WAYCODER_API_KEY",
        };
    }

    private static string? Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : null;
}
