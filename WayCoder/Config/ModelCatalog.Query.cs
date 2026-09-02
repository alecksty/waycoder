using System.Text;
using WayCoder.Infra;
namespace WayCoder;

public static partial class ModelCatalog
{
    public static ModelInfo? Find(string id)
    {
        var builtIn = BuiltIn.FirstOrDefault(m => m.Id == id);
        if (builtIn != null) return builtIn;
        var custom = LoadCustom();
        return custom.Values.FirstOrDefault(m => m.Id == id);
    }

    /// <summary>按 id + baseUrl 精确查模型（地址不同 = 不同服务商）。baseUrl 为空回退 <see cref="Find(string)"/>。
    /// baseUrl 与目录条目均做尾斜杠规范化比较——槽位/配置里的网关可能带尾斜杠，与 ResolveBaseUrl 的 TrimEnd('/') 对齐，
    /// 否则带尾斜杠的网关匹配不到（归属退化为 Find(id) 内置官方优先）。</summary>
    public static ModelInfo? Find(string id, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return Find(id);
        var url = ModelCatalog.NormalizeBaseUrl(baseUrl);
        var custom = LoadCustom();
        var c = custom.Values.FirstOrDefault(m => m.Id == id
            && string.Equals(ModelCatalog.NormalizeBaseUrl(m.DefaultBaseUrl), url, StringComparison.OrdinalIgnoreCase));
        if (c != null) return c;
        return BuiltIn.FirstOrDefault(m => m.Id == id
            && string.Equals(ModelCatalog.NormalizeBaseUrl(m.DefaultBaseUrl), url, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 解析模型的上下文窗口大小。优先用内置模型目录的 ContextWindow，
    /// 未知模型或窗口为 0 时回退到 fallback（默认 1M）。
    /// 用于切换模型时同步 Agent 的上下文窗口上限。
    /// </summary>
    public static int ResolveContextWindow(string? modelId, int fallback = 128_000)
    {
        if (Config.Instance.TinyMode) return Config.Instance.TinyWindow;
        if (string.IsNullOrWhiteSpace(modelId)) return fallback;
        var info = Find(modelId);
        return info != null && info.ContextWindow > 0 ? info.ContextWindow : fallback;
    }

    /// <summary>模型调用参数约束（解析后最终取值）。</summary>
    public sealed record ModelCallConstraints(
        string? ReasoningEffortAllowed, int TemperaturePrecision,
        bool SupportsThinking, bool SupportsTools, bool SupportsVision);

    /// <summary>
    /// 两级合并：模型级 &gt; 厂商级 &gt; 全局默认。纯函数，可自测。
    /// 模型级显式 0（整数精度）不会被厂商级覆盖（null 才继承）。
    /// </summary>
    internal static (string? Allowed, int Precision) MergeModelProviderConstraints(
        string? modelAllowed, int? modelPrecision,
        string? providerAllowed, int? providerPrecision,
        int globalDefault = 2)
        => (
            !string.IsNullOrWhiteSpace(modelAllowed) ? modelAllowed : providerAllowed,
            modelPrecision ?? providerPrecision ?? globalDefault);

    /// <summary>bool 三级合并：模型显式（含 false）优先 > 厂商 > 推断兜底。null 才继承。</summary>
    internal static bool MergeBool(bool? model, bool? provider, bool fallback)
        => model ?? provider ?? fallback;

    /// <summary>
    /// 解析当前模型的有效调用参数约束 + 能力特性（LLM 每请求调用一次）。
    /// Find 带网关反查（同 id 不同网关是两个条目），再按 ProviderId 取厂商级，三级合并（模型 > 厂商 > 推断）。
    /// </summary>
    public static ModelCallConstraints ResolveModelCallConstraints(string? modelId, string? baseUrl)
    {
        var info = string.IsNullOrWhiteSpace(modelId) ? null : Find(modelId, baseUrl);
        string? provAllowed = null;
        int? provPrec = null;
        bool? provThink = null, provTools = null, provVision = null;
        if (info != null && Providers.TryGetValue(info.ProviderId, out var prov))
        {
            provAllowed = prov.ReasoningEffortAllowed;
            provPrec = prov.TemperaturePrecision;
            provThink = prov.SupportsThinking;
            provTools = prov.SupportsTools;
            provVision = prov.SupportsVision;
        }
        var (allowed, prec) = MergeModelProviderConstraints(
            info?.ReasoningEffortAllowed, info?.TemperaturePrecision,
            provAllowed, provPrec);
        return new ModelCallConstraints(
            allowed, prec,
            MergeBool(info?.SupportsThinking, provThink, InferSupportsThinking(modelId, info?.ReasoningEffortAllowed)),
            MergeBool(info?.SupportsTools, provTools, InferSupportsTools(modelId)),
            MergeBool(info?.SupportsVision, provVision, InferSupportsVision(modelId)));
    }

    /// <summary>厂商级 temperature 覆盖（per-provider 参数）：ProviderInfo.Temperature 优先，未声明返回 null 用全局。</summary>
    public static double? ResolveProviderTemperature(string? modelId, string? baseUrl)
    {
        var info = string.IsNullOrWhiteSpace(modelId) ? null : Find(modelId, baseUrl);
        if (info != null && Providers.TryGetValue(info.ProviderId, out var prov))
            return prov.Temperature;
        return null;
    }

    /// <summary>推断是否支持思考：声明了允许集（非 none）视为支持；Reasoning/推理家族（gpt-5/o/claude/gemini/deepseek-reasoner/qwen3-max 等）支持。</summary>
    private static bool InferSupportsThinking(string? modelId, string? reasoningAllowed)
    {
        if (!string.IsNullOrWhiteSpace(reasoningAllowed) && !reasoningAllowed.Equals("none", StringComparison.OrdinalIgnoreCase))
            return true;
        var m = (modelId ?? "").ToLowerInvariant();
        return m.StartsWith("gpt-5") || m.StartsWith("o1") || m.StartsWith("o3") || m.StartsWith("o4")
            || m.Contains("claude") || m.Contains("gemini") || m.Contains("deepseek-reasoner")
            || m.Contains("deepseek-v4") || m.Contains("qwen3-max") || m.Contains("glm-4") || m.Contains("glm-5")
            || m.Contains("kimi-k2") || m.Contains("grok-3") || m.Contains("minimax-m")
            || m.Contains("reasoner") || m.Contains("thinking");
    }

    /// <summary>推断是否支持工具：本地服务模型（ollama/lmstudio/local/cc-switch/embed 类）不支持，其余默认支持。</summary>
    private static bool InferSupportsTools(string? modelId)
    {
        var m = (modelId ?? "").ToLowerInvariant();
        if (m.Contains("embed")) return false;
        if (m.Contains(":") && (m.StartsWith("gemma") || m.Contains(":0.5b") || m.Contains(":1b") || m.Contains(":2b")))
            return false;  // Ollama 小模型如 gemma2:2b 不支持工具
        return true;
    }

    /// <summary>推断是否支持视觉：多模态家族子串（原 LLM.ModelSupportsVision 迁移，补齐 o4-mini/llama-4/gemma3 盲点）。
    /// 注意「vl」泛化覆盖 qwen-vl/internvl/cogvlm 等「-vl/vlm」家族，但 glm-4v 不含「vl」子串，须单独显式匹配。</summary>
    private static bool InferSupportsVision(string? modelId)
    {
        var m = (modelId ?? "").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(m)) return false;
        return m.Contains("gpt-4o") || m.Contains("gpt-4.1") || m.Contains("gpt-5")
            || m.StartsWith("o4-mini") || m.Contains("claude") || m.Contains("gemini")
            || m.Contains("vision") || m.Contains("vl") || m.Contains("glm-4v")
            || m.Contains("llama-4") || m.Contains("llava") || m.Contains("gemma3")
            || m.Contains("pixtral") || m.Contains("grok")
            || m.Contains("minimax") || m.Contains("doubao") || m.Contains("hunyuan");
    }

    /// <summary>
    /// 决定要发送的 reasoning_effort 值：全局值在有效允许集内→原样返回；不在→null（跳过字段，让模型用默认 thinking，避免 HTTP 400）。
    /// 无约束（allowedCsv 空）→ 原样返回全局值（现状）；全局未设置→null。
    /// </summary>
    public static string? ResolveReasoningEffort(string? allowedCsv, string? globalValue)
    {
        if (string.IsNullOrEmpty(globalValue)) return null;
        if (string.IsNullOrWhiteSpace(allowedCsv)) return globalValue;
        return allowedCsv.Split(',')
            .Any(v => v.Trim().Equals(globalValue.Trim(), StringComparison.OrdinalIgnoreCase))
            ? globalValue
            : null;
    }

    /// <summary>解析当前模型是否支持视觉（模型/厂商声明 > 家族推断）。</summary>
    public static bool ResolveSupportsVision(string? modelId, string? baseUrl)
        => ResolveModelCallConstraints(modelId, baseUrl).SupportsVision;

    /// <summary>
    /// 判断是否「本地服务」地址：localhost / 127.0.0.1 / 0.0.0.0 / 空。
    /// 注意：Ollama 也有云端（ollama.com），不能只看 providerId=ollama，必须按地址判断。
    /// </summary>
    public static bool IsLocalUrl(string? baseUrl)
        => string.IsNullOrWhiteSpace(baseUrl)
        || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || baseUrl.Contains("127.0.0.1")
        || baseUrl.Contains("0.0.0.0");

    /// <summary>
    /// 解析当前模型的 API 请求格式（openai / anthropic / gemini）。
    /// Find 带网关反查（同 id 不同网关是两个条目），再按 ProviderId 取厂商级；默认 openai。
    /// </summary>
    public static string ResolveApiFormat(string? modelId, string? baseUrl)
    {
        var info = string.IsNullOrWhiteSpace(modelId) ? null : Find(modelId, baseUrl);
        if (info != null && Providers.TryGetValue(info.ProviderId, out var prov)
            && !string.IsNullOrWhiteSpace(prov.ApiFormat))
            return prov.ApiFormat;
        return "openai";
    }

    /// <summary>
    /// 解析 tiny 模式的上下文窗口：
    ///   1. spec 显式指定（"8k"/"8192"）→ 直接用
    ///   2. 未指定 → 自动探测（Ollama /api/show 真实窗口 → 目录 → 4K 兜底）
    /// </summary>
    public static int ResolveTinyWindow(string? spec, string? modelId, string? baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(spec))
            return ParseWindowSpec(spec) ?? Config.TinyContextWindow;

        return ProbeModelWindow(modelId, baseUrl, Config.TinyContextWindow);
    }

    /// <summary>
    /// 探测模型真实上下文窗口：Ollama /api/show 优先（解决目录标称 128K 虚高问题），
    /// 其次内置目录 ContextWindow，最后回退 fallback。
    /// </summary>
    public static int ProbeModelWindow(string? modelId, string? baseUrl, int fallback)
    {
        if (IsOllamaBaseUrl(baseUrl) && !string.IsNullOrWhiteSpace(modelId))
        {
            var ctx = QueryOllamaContextLength(baseUrl!, modelId);
            // Ollama 探测失败直接回退 fallback（目录对本地模型标称值虚高，不可靠）
            return ctx > 0 ? ctx : fallback;
        }
        if (string.IsNullOrWhiteSpace(modelId)) return fallback;
        var info = Find(modelId);
        return info != null && info.ContextWindow > 0 ? info.ContextWindow : fallback;
    }

    /// <summary>解析窗口规格："8k"/"8192"/"8K" → 8192；非法返回 null</summary>
    public static int? ParseWindowSpec(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec)) return null;
        var s = spec.Trim().ToLowerInvariant();
        bool kilo = false;
        if (s.EndsWith('k'))
        {
            kilo = true;
            s = s[..^1].Trim();
        }
        if (!int.TryParse(s, out var n) || n <= 0) return null;
        return kilo ? (n > int.MaxValue / 1024 ? null : n * 1024) : n;
    }

    /// <summary>base url 是否指向本地 Ollama</summary>
    public static bool IsOllamaBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;
        return baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.Contains("127.0.0.1")
            || baseUrl.Contains("11434");
    }

    /// <summary>查询 Ollama /api/show 获取模型真实 context_length，失败返回 0</summary>
    private static int QueryOllamaContextLength(string baseUrl, string modelId)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var baseUri = ModelCatalog.NormalizeBaseUrl(baseUrl);
            var json = $"{{\"name\":\"{modelId}\"}}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = client.PostAsync($"{baseUri}/api/show", content).GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode) return 0;
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var root = Json.Parse(body);
            var modelInfo = Obj(root?["model_info"]);
            if (modelInfo == null) return 0;
            foreach (var (key, value) in modelInfo.Entries)
            {
                if (!key.Contains("context_length", StringComparison.OrdinalIgnoreCase)) continue;
                int ctx = 0;
                if (value.Kind == JKind.Number) ctx = (int)Math.Round(value.AsNumber());
                else if (value.Kind == JKind.String && int.TryParse(value.AsString(), out var c2)) ctx = c2;
                if (ctx > 0) return ctx;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public static ModelInfo[] ByProvider(string providerId) =>
        All.Where(m => m.ProviderId == providerId).ToArray();

    public static string[] ProviderIds =>
        All.Select(m => m.ProviderId).Distinct().ToArray();

    public static string[] Categories =>
        All.Select(m => m.Category).Distinct().ToArray();

    public static ModelInfo[] Search(string query)
    {
        return All.Where(m =>
            m.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.Provider.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.ProviderId.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToArray();
    }

    // Import from external configs (OpenCode / Crush / Cline / Continue)
}
