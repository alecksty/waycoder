using WayCoder.UI.TUI.Base;

namespace WayCoder;

/// <summary>
/// 模型回退链 —— LLM 调用失败时自动尝试备选模型。
/// 配置回退顺序，设置最大预算。
/// </summary>
public static class FallbackLLM
{
    /// <summary>默认回退链（从 ConnectionConfig.FallbackChain 读取，一串 connect 名）</summary>
    public static string[] DefaultFallbackChain =>
        ConnectionConfig.FallbackChain.ToArray();

    /// <summary>当前回退链（connect 名数组）</summary>
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
    /// 回退进度输出：TUI 全屏模式下回退发生在 Agent 后台线程，直接写 stderr 会与渲染交错花屏
    /// （TUI 渲染走 stdout，stderr 也落在同一终端）→ 仅非 TUI 模式输出；回退信息本身已进 ErrorLog + 聊天提示。
    /// </summary>
    private static void WriteFallback(string msg)
    {
        if (TuiManager.Instance?.IsActive != true)
            Console.Error.WriteLine(msg);
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
            WriteFallback(L.Pick($"[fallback] 模型 {originalLlm.Model} 失败: {ex.Message}",
                $"[fallback] model {originalLlm.Model} failed: {ex.Message}"));
            ErrorLog.LlmError(originalLlm.Model, originalLlm.Endpoint,
                $"主模型失败，启动回退链: {ex.Message}", ex);
        }

        // 回退链开关（默认关）：关 = 只用当前模型，失败即停，明确告诉用户当前模型就是它
        if (!Config.Instance.FallbackEnabled)
        {
            WriteFallback(L.Pick($"[fallback] 回退链已关闭，不再尝试备选模型（/config set FallbackEnabled true 开启）",
                $"[fallback] the fallback chain is disabled; not trying alternative models (enable it with /config set FallbackEnabled true)"));
            return new LLMResponse
            {
                Content = L.Pick($"[错误] {originalLlm.Model} 请求失败，回退链已关闭。修复网络/API Key，或 /config set FallbackEnabled true 开启自动回退。",
                    $"[Error] The request to {originalLlm.Model} failed and the fallback chain is disabled. Fix the network/API key, or enable automatic fallback with /config set FallbackEnabled true."),
                IsFatalError = true,
            };
        }

        // 回退链（一串 connect：{providerId, modelId}，回退时 model+key+baseUrl 一起换）
        int skipped = 0;
        foreach (var (connectName, idx) in FallbackChain.Select((m, i) => (m, i)))
        {
            var (model, fbKey, fbUrl) = ResolveConnect(connectName, originalLlm.ApiKey);
            if (model == originalLlm.Model) continue;

            // 预算检查：超过预算时记录警告并跳过回退（优雅降级，不崩溃）
            if (BudgetExceeded())
            {
                WriteFallback(L.Pick($"[fallback] ⚠ 已达回退预算上限 ${MaxBudget:F2}，停止尝试备选模型",
                    $"[fallback] ⚠ fallback budget of ${MaxBudget:F2} reached; not trying alternative models"));
                break;
            }

            FallbackIndex = idx;

            // connect → provider 解析 key/baseUrl（逻辑一体）
            if (string.IsNullOrEmpty(fbKey))
            {
                WriteFallback(L.Pick($"[fallback] ⏭ 跳过 {connectName}（无 API Key，/provider apikey set <pid> <key> 保存）",
                    $"[fallback] ⏭ skipping {connectName} (no API key; save one with /provider apikey set <pid> <key>)"));
                skipped++;
                continue;
            }

            var fallbackLlm = new LLM(model, fbKey, fbUrl,
                originalLlm.MaxTokens, originalLlm.Temperature);

            try
            {
                WriteFallback(L.Pick($"[fallback] 尝试 {model}（{connectName}）...",
                    $"[fallback] trying {model} ({connectName})..."));
                var resp = await fallbackLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
                AddSpent(fallbackLlm.EstimatedCost ?? 0);

                // 回退成功：应用整个 connect（模型 + key + baseUrl 一起换，走 Reconfigure）
                originalLlm.Reconfigure(fbKey, fbUrl);
                originalLlm.Model = model;
                WriteFallback(L.Pick($"[fallback] ✓ 已切换到 {model}（{connectName}）",
                    $"[fallback] ✓ switched to {model} ({connectName})"));
                return resp;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                WriteFallback(L.Pick($"[fallback] {model} 也失败: {ex.Message}",
                    $"[fallback] {model} also failed: {ex.Message}"));
                ErrorLog.LlmError(model, fallbackLlm.Endpoint,
                    $"回退模型也失败: {ex.Message}", ex);
                continue;
            }
        }

        // 全部回退模型失败或跳过 → 给原始模型最后一次重试机会（临时错误可能已恢复）
        string skipMsg = skipped > 0
            ? L.Pick($"（{skipped} 个因无 Key 跳过）", $" ({skipped} skipped for missing keys)")
            : "";
        WriteFallback(L.Pick($"[fallback] 所有回退模型均失败{skipMsg}，最后一次重试原始模型 {originalLlm.Model}...",
            $"[fallback] all fallback models failed{skipMsg}; retrying the original model {originalLlm.Model} one last time..."));
        try
        {
            var resp = await originalLlm.ChatAsync(messages, tools, onToken, cancellationToken: ct);
            AddSpent(originalLlm.EstimatedCost ?? 0);
            FallbackIndex = -1;
            WriteFallback(L.Pick($"[fallback] ✓ 原始模型 {originalLlm.Model} 重试成功",
                $"[fallback] ✓ the original model {originalLlm.Model} succeeded on retry"));
            return resp;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            WriteFallback(L.Pick($"[fallback] 原始模型 {originalLlm.Model} 重试也失败: {ex.Message}",
                $"[fallback] the original model {originalLlm.Model} also failed on retry: {ex.Message}"));
        }

        WriteFallback(L.Pick("[fallback] 所有回退模型均已失败，请检查网络或 API 密钥。",
            "[fallback] All fallback models failed. Check the network or the API key."));
        ErrorLog.Error("FallbackLLM", "所有回退模型均已失败（包括主模型），请检查网络或 API 密钥");
        return new LLMResponse
        {
            Content = L.Pick("[错误] 所有模型（含回退链）均已失败，请检查网络或 API 密钥。会话已自动保存，修复网络/API Key 后可恢复。",
                "[Error] All models (including the fallback chain) failed. Check the network or the API key. The session was saved automatically and can be resumed once the network/API key is fixed."),
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
    /// 按 connect 名解析回退端点：connect → (modelId, key, baseUrl)。
    /// connect 的 provider（逻辑一体：providers.json + api_keys.json）解析 key/baseUrl；
    /// 兼容旧配置：connect 名若是裸模型名，则从模型目录解析 provider。
    /// key 缺省回退 WAYCODER_API_KEY / API_KEY / 原始密钥。
    /// </summary>
    private static (string model, string? key, string? baseUrl) ResolveConnect(string connectName, string originalKey)
    {
        var connect = ConnectionConfig.FindConnect(connectName);
        if (connect == null)
        {
            // 兼容旧配置：connect 名可能是裸模型名
            var info = ModelCatalog.Find(connectName);
            var prov = info != null ? ConnectionConfig.ResolveProvider(info.ProviderId) : null;
            return (connectName,
                prov?.ApiKey ?? Env("WAYCODER_API_KEY") ?? Env("API_KEY") ?? originalKey,
                prov?.BaseUrl ?? info?.DefaultBaseUrl);
        }
        var provider = ConnectionConfig.ResolveProvider(connect.ProviderId);
        var key = provider?.ApiKey;
        if (string.IsNullOrEmpty(key)) key = Env("WAYCODER_API_KEY") ?? Env("API_KEY") ?? originalKey;
        var baseUrl = provider?.BaseUrl ?? ConnectionConfig.ResolveBaseUrl(connect.ProviderId);
        return (connect.ModelId, key, baseUrl);
    }

    private static string? Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : null;
}
