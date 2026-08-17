using System.Text;
using WayCoder.Infra;

namespace WayCoder;

/// <summary>
/// Model Catalog — built-in model registry + external config import (OpenCode / Crush / Continue / Cline).
/// Browse, search, and model metadata. Compatible with most OpenAI-compatible APIs.
/// </summary>
public static class ModelCatalog
{
    /// <summary>Model metadata</summary>
    /// <param name="MaxOutput">输出窗口大小（单次最大输出 token 数，0=未指定，回退全局 MaxTokens）</param>
    public record ModelInfo(
        string Id,
        string DisplayName,
        string Provider,
        string ProviderId,
        string ProviderIcon,
        string Category,
        int ContextWindow,
        double InputPrice,
        double OutputPrice,
        string? DefaultBaseUrl,
        string Description,
        int MaxOutput = 0
    );

    public static readonly ModelInfo[] BuiltIn =
    [
        // OpenAI
        new("gpt-5.5", "GPT-5.5", "OpenAI", "openai", "O", "Flagship", 256_000, 5, 30, "https://api.openai.com", "Top reasoning + code + multimodal"),
        new("gpt-5.4", "GPT-5.4", "OpenAI", "openai", "O", "Flagship", 256_000, 2.5, 15, "https://api.openai.com", "Cost-effective flagship"),
        new("gpt-5.4-mini", "GPT-5.4 Mini", "OpenAI", "openai", "O", "Light", 256_000, 0.75, 4.5, "https://api.openai.com", "Small model daily tasks"),
        new("gpt-5.4-nano", "GPT-5.4 Nano", "OpenAI", "openai", "O", "Light", 256_000, 0.2, 1.25, "https://api.openai.com", "Tiny model"),
        new("o4-mini", "o4 Mini", "OpenAI", "openai", "O", "Reasoning", 200_000, 1.1, 4.4, "https://api.openai.com", "Reasoning specialist"),
        new("gpt-4.1", "GPT-4.1", "OpenAI", "openai", "O", "Flagship", 1_000_000, 2, 8, "https://api.openai.com", "Ultra-long context"),
        new("gpt-4.1-mini", "GPT-4.1 Mini", "OpenAI", "openai", "O", "Light", 1_000_000, 0.4, 1.6, "https://api.openai.com", "Ultra-long context light"),
        new("gpt-4.1-nano", "GPT-4.1 Nano", "OpenAI", "openai", "O", "Light", 1_000_000, 0.1, 0.4, "https://api.openai.com", "Ultra-long context tiny"),
        new("gpt-4o", "GPT-4o", "OpenAI", "openai", "O", "Flagship", 128_000, 2.5, 10, "https://api.openai.com", "Multimodal flagship (old)"),
        new("gpt-4o-mini", "GPT-4o Mini", "OpenAI", "openai", "O", "Light", 128_000, 0.15, 0.6, "https://api.openai.com", "Multimodal light (old)"),

        // Anthropic
        new("claude-opus-5", "Claude Opus 5", "Anthropic", "anthropic", "A", "Flagship", 200_000, 15, 75, "https://api.anthropic.com", "Best code intelligence"),
        new("claude-sonnet-5", "Claude Sonnet 5", "Anthropic", "anthropic", "A", "Flagship", 200_000, 3, 15, "https://api.anthropic.com", "High-performance code"),
        new("claude-haiku-4-5", "Claude Haiku 4.5", "Anthropic", "anthropic", "A", "Light", 200_000, 1, 5, "https://api.anthropic.com", "Fast and light"),
        new("claude-opus-4-6", "Claude Opus 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 5, 25, "https://api.anthropic.com", "Best code (old)"),
        new("claude-sonnet-4-6", "Claude Sonnet 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 3, 15, "https://api.anthropic.com", "High-perf code (old)"),

        // DeepSeek
        new("deepseek-v4-pro", "DeepSeek V4 Pro", "DeepSeek", "deepseek", "D", "Flagship", 1_048_576, 0.435, 0.87, "https://api.deepseek.com", "Flagship deep reasoning"),
        new("deepseek-v4-flash", "DeepSeek V4 Flash", "DeepSeek", "deepseek", "D", "Light", 1_048_576, 0.14, 0.28, "https://api.deepseek.com", "Fast and cost-effective"),
        new("deepseek-chat", "DeepSeek V3 (old)", "DeepSeek", "deepseek", "D", "Flagship", 64_000, 0.27, 1.10, "https://api.deepseek.com", "V3 legacy"),
        new("deepseek-reasoner", "DeepSeek R1", "DeepSeek", "deepseek", "D", "Reasoning", 64_000, 0.55, 2.19, "https://api.deepseek.com", "Deep reasoning"),

        // Google
        new("gemini-2.5-pro", "Gemini 2.5 Pro", "Google", "google", "G", "Flagship", 1_000_000, 1.25, 10, "https://generativelanguage.googleapis.com", "Ultra-long context"),
        new("gemini-2.5-flash", "Gemini 2.5 Flash", "Google", "google", "G", "Light", 1_000_000, 0.15, 0.6, "https://generativelanguage.googleapis.com", "Ultra-long light"),
        new("gemini-2.0-flash", "Gemini 2.0 Flash", "Google", "google", "G", "Light", 1_000_000, 0.10, 0.4, "https://generativelanguage.googleapis.com", "Ultra-fast light"),

        // Alibaba Qwen
        new("qwen3-max", "Qwen3 Max", "Alibaba", "qwen", "Q", "Flagship", 128_000, 0.78, 3.9, "https://dashscope.aliyuncs.com/compatible-mode/v1", "Alibaba flagship"),
        new("qwen3-plus", "Qwen3 Plus", "Alibaba", "qwen", "Q", "Light", 128_000, 0.26, 0.78, null, "Alibaba cost-effective"),
        new("qwen-max", "Qwen Max", "Alibaba", "qwen", "Q", "Flagship", 32_000, 0.78, 3.9, null, "Alibaba old flagship"),
        new("qwen-plus", "Qwen Plus", "Alibaba", "qwen", "Q", "Light", 131_072, 0.13, 0.39, null, "Alibaba old light"),
        new("qwen-turbo", "Qwen Turbo", "Alibaba", "qwen", "Q", "Light", 1_000_000, 0.05, 0.15, null, "Alibaba ultra-fast"),

        // Moonshot Kimi
        new("kimi-k2.5", "Kimi K2.5", "Moonshot", "moonshot", "M", "Flagship", 128_000, 0.6, 3, "https://api.moonshot.cn", "Chinese flagship"),

        // Zhipu GLM
        new("glm-4-plus", "GLM-4 Plus", "Zhipu", "zhipu", "Z", "Flagship", 128_000, 0.47, 0.54, "https://open.bigmodel.cn/api/paas/v4", "Chinese flagship"),
        new("glm-4-flash", "GLM-4 Flash", "Zhipu", "zhipu", "Z", "Light", 128_000, 0.07, 0.14, null, "Chinese cost-effective"),

        // ByteDance Doubao
        new("doubao-pro-1.5", "Doubao Pro 1.5", "ByteDance", "bytedance", "B", "Flagship", 128_000, 0.87, 2.6, "https://ark.cn-beijing.volces.com/api/v3", "Doubao flagship"),
        new("doubao-lite-1.5", "Doubao Lite 1.5", "ByteDance", "bytedance", "B", "Light", 128_000, 0.087, 0.26, null, "Doubao light"),

        // 01.AI Yi
        new("yi-large", "Yi Large", "01.AI", "01ai", "Y", "Flagship", 32_000, 0.5, 1.5, "https://api.lingyiwanwu.com", "Chinese flagship"),

        // xAI Grok
        new("grok-3", "Grok 3", "xAI", "xai", "X", "Flagship", 128_000, 3, 15, "https://api.x.ai", "xAI flagship"),

        // Mistral
        new("mistral-large", "Mistral Large", "Mistral", "mistral", "Mi", "Flagship", 128_000, 2, 6, "https://api.mistral.ai", "European flagship"),
        new("mistral-small", "Mistral Small", "Mistral", "mistral", "Mi", "Light", 32_000, 0.2, 0.6, null, "European light"),
        new("codestral", "Codestral", "Mistral", "mistral", "Mi", "Code", 256_000, 0.3, 0.9, null, "Code specialist"),

        // Meta Llama (via OpenRouter / Groq / Together)
        new("llama-4-maverick", "Llama 4 Maverick", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source flagship"),
        new("llama-4-scout", "Llama 4 Scout", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source light"),
        new("llama-3.1-405b", "Llama 3.1 405B", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source giant"),

        // SiliconFlow (Chinese proxy)
        new("Pro/deepseek-ai/DeepSeek-V3", "DeepSeek V3 (SiliconFlow)", "SiliconFlow", "siliconflow", "S", "Flagship", 64_000, 0, 0, "https://api.siliconflow.cn", "SiliconFlow proxy"),
        new("Pro/Qwen/Qwen3-235B-A22B", "Qwen3 235B (SiliconFlow)", "SiliconFlow", "siliconflow", "S", "Flagship", 128_000, 0, 0, null, "SiliconFlow proxy"),

        // Local / Ollama / LM Studio / vLLM (no API key needed, default base URL http://localhost:11434)
        new("qwen2.5-coder:latest", "Qwen2.5 Coder (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, "http://localhost:11434", "Ollama local code model"),
        new("qwen2.5-coder:3b", "Qwen2.5 Coder 3B (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama small code model"),
        new("qwen2.5-coder:7b", "Qwen2.5 Coder 7B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama mid code model"),
        new("qwen3:8b", "Qwen3 8B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama general model"),
        new("codellama:latest", "CodeLlama (Ollama)", "Local", "local", "L", "Local", 16_000, 0, 0, null, "Ollama local code model"),
        new("deepseek-coder-v2:latest", "DeepSeek Coder V2 (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama local code model"),
        new("deepseek-r1:8b", "DeepSeek R1 8B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama reasoning model"),
        new("deepseek-r1:14b", "DeepSeek R1 14B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama reasoning model"),
        new("llama3.2:3b", "Llama 3.2 3B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama tiny fast model"),
        new("llama3.1:latest", "Llama 3.1 (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama local model"),
        new("phi4:latest", "Phi-4 (Ollama)", "Local", "local", "L", "Local", 16_000, 0, 0, null, "Ollama local model"),
        new("mistral:latest", "Mistral (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama local model"),
        new("gemma3:latest", "Gemma 3 (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama local model"),
        new("local-model", "Local Model (Custom)", "Local", "local", "L", "Local", 0, 0, 0, "http://localhost:11434", "Any Ollama/LM Studio/vLLM model"),

        // Custom
        new("custom", "Custom Model", "Custom", "custom", "*", "Custom", 0, 0, 0, null, "Enter model ID and API endpoint"),
    ];

    /// <summary>Provider registry with default base URLs</summary>
    public static readonly Dictionary<string, (string DisplayName, string DefaultBaseUrl)> Providers;

    static ModelCatalog()
    {
        Providers = new Dictionary<string, (string, string)>
        {
            ["openai"]     = ("OpenAI",       "https://api.openai.com"),
            ["anthropic"]  = ("Anthropic",    "https://api.anthropic.com"),
            ["deepseek"]   = ("DeepSeek",     "https://api.deepseek.com"),
            ["google"]     = ("Google",       "https://generativelanguage.googleapis.com"),
            ["qwen"]       = ("Alibaba Qwen", "https://dashscope.aliyuncs.com/compatible-mode/v1"),
            ["moonshot"]   = ("Moonshot",     "https://api.moonshot.cn"),
            ["zhipu"]      = ("Zhipu GLM",    "https://open.bigmodel.cn/api/paas/v4"),
            ["bytedance"]  = ("ByteDance",    "https://ark.cn-beijing.volces.com/api/v3"),
            ["01ai"]       = ("01.AI",        "https://api.lingyiwanwu.com"),
            ["xai"]        = ("xAI",          "https://api.x.ai"),
            ["mistral"]    = ("Mistral",      "https://api.mistral.ai"),
            ["siliconflow"]= ("SiliconFlow",  "https://api.siliconflow.cn"),
            ["openrouter"] = ("OpenRouter",   "https://openrouter.ai/api/v1"),
            ["groq"]       = ("Groq",         "https://api.groq.com/openai/v1"),
            ["together"]   = ("Together AI",  "https://api.together.xyz/v1"),
            ["gitee"]      = ("Gitee AI",     "https://ai.gitee.com/v1"),
            ["bailian"]    = ("Alibaba Bailian", "https://dashscope.aliyuncs.com/compatible-mode/v1"),
            ["opencode"]   = ("OpenCode Zen", "https://opencode.ai/zen/v1"),
            ["minimax"]    = ("MiniMax",      "https://api.minimaxi.com/v1"),
            ["aihubmix"]   = ("AIHubMix",     "https://aihubmix.com/v1"),
            ["local"]      = ("Local",        ""),
            ["custom"]     = ("Custom",       ""),
        };
    }

    // ════════════════════════════════════════════════════════════
    // 自定义模型库（外置 JSON：全局 ~/.waycoder/models.json + 本地 .waycoder/models.json）
    // 内置目录为兜底，外置库按 Id 覆盖/追加，本地覆盖全局。
    // ════════════════════════════════════════════════════════════

    private static readonly object _lock = new();
    private static Dictionary<string, ModelInfo>? _custom;
    private static ModelInfo[]? _all;

    /// <summary>全局模型库路径（跨平台，所有用户共享）</summary>
    public static string GlobalModelsPath => Global.GlobalConfigPath("models.json");

    /// <summary>本地模型库路径（项目专属）</summary>
    public static string LocalModelsPath =>
        Path.Combine(Environment.CurrentDirectory, Global.ConfigDirName, "models.json");

    /// <summary>
    /// 完整模型目录 = 内置目录 + 自定义库（自定义按 Id 覆盖内置，新增项追加到末尾）。
    /// 内置目录始终可用：找不到外置库时，内置数据兜底。
    /// </summary>
    public static ModelInfo[] All
    {
        get
        {
            if (_all != null) return _all;
            lock (_lock)
            {
                if (_all != null) return _all;
                var list = new List<ModelInfo>(BuiltIn);
                foreach (var (id, m) in LoadCustom())
                {
                    var idx = list.FindIndex(x => x.Id == id);
                    if (idx >= 0) list[idx] = m;   // 覆盖内置
                    else list.Add(m);              // 追加自定义
                }
                _all = list.ToArray();
                return _all;
            }
        }
    }

    /// <summary>新增/更新自定义模型，返回写入的文件路径。local=true 写本地，否则写全局。</summary>
    public static string AddCustom(ModelInfo info, bool local = false)
    {
        var path = local ? LocalModelsPath : GlobalModelsPath;
        var models = ReadFile(path);
        models[info.Id] = info;
        SaveCustom(models, path);
        Invalidate();
        return path;
    }

    /// <summary>删除自定义模型（从全局和本地两个文件都移除），返回受影响文件列表。</summary>
    public static string[] RemoveCustom(string id)
    {
        var removed = new List<string>();
        foreach (var path in new[] { GlobalModelsPath, LocalModelsPath })
        {
            if (!File.Exists(path)) continue;
            var models = ReadFile(path);
            if (models.Remove(id))
            {
                SaveCustom(models, path);
                removed.Add(path);
            }
        }
        if (removed.Count > 0) Invalidate();
        return removed.ToArray();
    }

    /// <summary>仅列出自定义模型（不含内置）</summary>
    public static ModelInfo[] ListCustom() => LoadCustom().Values.OrderBy(m => m.Id).ToArray();

    /// <summary>删除某服务商下的所有自定义模型（从全局+本地两个文件移除），返回删除数量。</summary>
    public static int RemoveCustomByProvider(string providerId)
    {
        var removed = 0;
        foreach (var path in new[] { GlobalModelsPath, LocalModelsPath })
        {
            if (!File.Exists(path)) continue;
            var models = ReadFile(path);
            var toRemove = models
                .Where(kv => kv.Value.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToArray();
            foreach (var id in toRemove)
            {
                models.Remove(id);
                removed++;
            }
            if (toRemove.Length > 0) SaveCustom(models, path);
        }
        if (removed > 0) Invalidate();
        return removed;
    }

    /// <summary>清除内存缓存并强制下次重新加载（外部改了 models.json 后调用）</summary>
    public static void Invalidate()
    {
        lock (_lock) { _custom = null; _all = null; }
    }

    private static Dictionary<string, ModelInfo> LoadCustom()
    {
        if (_custom != null) return _custom;
        lock (_lock)
        {
            if (_custom != null) return _custom;
            var merged = new Dictionary<string, ModelInfo>();
            foreach (var m in ReadFile(GlobalModelsPath).Values) merged[m.Id] = m;   // 全局先
            foreach (var m in ReadFile(LocalModelsPath).Values) merged[m.Id] = m;    // 本地覆盖
            _custom = merged;
            return _custom;
        }
    }

    private static Dictionary<string, ModelInfo> ReadFile(string path)
    {
        var result = new Dictionary<string, ModelInfo>();
        if (!File.Exists(path)) return result;
        try
        {
            var root = Json.Parse(File.ReadAllText(path));
            var arr = Arr(root) ?? Arr(root?["models"]);
            if (arr != null)
            {
                foreach (var node in arr.Items)
                {
                    var info = FromJson(node);
                    if (info != null) result[info.Id] = info;
                }
            }
        }
        catch { }
        return result;
    }

    private static void SaveCustom(Dictionary<string, ModelInfo> models, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            var arr = JNode.Array();
            foreach (var m in models.Values.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase))
                arr.Add(ToJson(m));
            File.WriteAllText(path, arr.ToJson(indent: true));
        }
        catch { /* 写入失败不崩溃 */ }
    }

    // ════════════════════════════════════════════════════════════
    // 序列化（AOT 安全手写，不用反射）
    // ════════════════════════════════════════════════════════════

    // JNode 便捷取值（AOT 安全，替代 System.Text.Json.Nodes 的 GetValue<T> / AsArray / AsObject）
    static JNode? Arr(JNode? n) => n != null && n.Kind == JKind.Array ? n : null;
    static JNode? Obj(JNode? n) => n != null && n.Kind == JKind.Object ? n : null;
    static string? StrOpt(JNode? n) => n != null && n.Kind == JKind.String ? n.AsString() : null;
    static int? IntOpt(JNode? n) => n != null && n.Kind == JKind.Number ? (int)Math.Round(n.AsNumber()) : null;
    static double? DblOpt(JNode? n) => n != null && n.Kind == JKind.Number ? n.AsNumber() : null;

    private static JNode ToJson(ModelInfo m) => JNode.Object()
        .Set("id", m.Id)
        .Set("displayName", m.DisplayName)
        .Set("provider", m.Provider)
        .Set("providerId", m.ProviderId)
        .Set("icon", m.ProviderIcon)
        .Set("category", m.Category)
        .Set("contextWindow", m.ContextWindow)
        .Set("maxOutput", m.MaxOutput)
        .Set("inputPrice", m.InputPrice)
        .Set("outputPrice", m.OutputPrice)
        .Set("baseUrl", m.DefaultBaseUrl)
        .Set("description", m.Description);

    /// <summary>从 models.json 反序列化（精确读回所有字段，不推断 providerId/description，避免往返损坏）</summary>
    private static ModelInfo? FromJson(JNode? node)
    {
        if (Obj(node) == null) return null;
        var id = node!["id"]?.AsString();
        if (string.IsNullOrWhiteSpace(id)) return null;
        return new ModelInfo(
            id,
            node["displayName"]?.AsString() ?? id,
            node["provider"]?.AsString() ?? "Imported",
            node["providerId"]?.AsString() ?? "import",
            node["icon"]?.AsString() ?? "*",
            node["category"]?.AsString() ?? "Imported",
            IntOpt(node["contextWindow"]) ?? 0,
            DblOpt(node["inputPrice"]) ?? 0,
            DblOpt(node["outputPrice"]) ?? 0,
            node["baseUrl"]?.AsString(),
            node["description"]?.AsString() ?? "",
            IntOpt(node["maxOutput"]) ?? 0
        );
    }

    // Query helpers
    public static ModelInfo? Find(string id)
    {
        var custom = LoadCustom();
        return custom.TryGetValue(id, out var c) ? c : BuiltIn.FirstOrDefault(m => m.Id == id);
    }

    /// <summary>
    /// 解析模型的上下文窗口大小。优先用内置模型目录的 ContextWindow，
    /// 未知模型或窗口为 0 时回退到 fallback（默认 1M）。
    /// 用于切换模型时同步 Agent 的上下文窗口上限。
    /// </summary>
    public static int ResolveContextWindow(string? modelId, int fallback = 1_048_576)
    {
        if (Config.Instance.TinyMode) return Config.Instance.TinyWindow;
        if (string.IsNullOrWhiteSpace(modelId)) return fallback;
        var info = Find(modelId);
        return info != null && info.ContextWindow > 0 ? info.ContextWindow : fallback;
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
            var baseUri = baseUrl.TrimEnd('/');
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
    public static List<ModelInfo> ImportFromJson(string json)
    {
        var result = new List<ModelInfo>();
        try
        {
            var root = Json.Parse(json);
            if (root == null) return result;

            if (Arr(root["models"]) is { } modelsArr)
            {
                foreach (var m in modelsArr.Items)
                {
                    var info = ParseModelNode(m);
                    if (info != null) result.Add(info);
                }
                return result;
            }

            if (Obj(root["apiProviders"]) is { } providers)
            {
                foreach (var (providerId, config) in providers.Entries)
                {
                    var providerName = config?["name"]?.AsString() ?? providerId;
                    var baseUrl = config?["baseUrl"]?.AsString();
                    if (Arr(config?["models"]) is { } providerModels)
                    {
                        foreach (var m in providerModels.Items)
                        {
                            var modelId = m?.AsString()
                                ?? m?["model"]?.AsString()
                                ?? m?["id"]?.AsString();
                            if (string.IsNullOrWhiteSpace(modelId)) continue;
                            result.Add(new ModelInfo(modelId, modelId, providerName,
                                providerId.ToLowerInvariant(), "*", "Imported", 0, 0, 0, baseUrl, "Imported from Cline"));
                        }
                    }
                }
                return result;
            }

            if (Arr(root) is { } simpleArr)
            {
                foreach (var item in simpleArr.Items)
                {
                    var id = item?.AsString();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    result.Add(new ModelInfo(id, id, "Imported", "import", "*", "Imported", 0, 0, 0, null, "Imported from array"));
                }
                return result;
            }

            var single = ParseModelNode(root);
            if (single != null) result.Add(single);
        }
        catch { }
        return result;
    }

    private static ModelInfo? ParseModelNode(JNode? node)
    {
        if (node == null) return null;
        var id = node["model"]?.AsString()
              ?? node["id"]?.AsString()
              ?? node["name"]?.AsString()
              ?? node.AsString();
        if (string.IsNullOrWhiteSpace(id)) return null;

        // 上下文 / 输出窗口：兼容 contextWindow / maxTokens / limit.context / limit.output
        var limit = Obj(node["limit"]);
        var contextWindow = IntOpt(node["contextWindow"])
            ?? IntOpt(limit?["context"])
            ?? IntOpt(node["contextLength"])
            ?? 0;
        var maxOutput = IntOpt(node["maxOutput"])
            ?? IntOpt(limit?["output"])
            ?? IntOpt(node["maxTokens"])
            ?? 0;

        // 计费：兼容 cost.input/output、pricing.input/output、inputPrice/outputPrice
        var cost = Obj(node["cost"]);
        var inputPrice = DblOpt(node["inputPrice"])
            ?? DblOpt(cost?["input"])
            ?? DblOpt(node["pricing"]?["input"])
            ?? 0;
        var outputPrice = DblOpt(node["outputPrice"])
            ?? DblOpt(cost?["output"])
            ?? DblOpt(node["pricing"]?["output"])
            ?? 0;

        var providerId = (node["provider"]?.AsString() ?? "import").ToLowerInvariant().Replace(" ", "-");
        var baseUrl = node["baseUrl"]?.AsString()
            ?? node["apiBase"]?.AsString()
            ?? node["options"]?["baseURL"]?.AsString()
            ?? node["options"]?["baseUrl"]?.AsString();

        return new ModelInfo(
            id,
            node["displayName"]?.AsString() ?? node["name"]?.AsString() ?? id,
            node["provider"]?.AsString() ?? "Imported",
            providerId,
            "*", "Imported",
            contextWindow,
            inputPrice, outputPrice,
            baseUrl,
            "Imported from external config",
            maxOutput
        );
    }

    // ════════════════════════════════════════════════════════════
    // 外部工具导入（OpenCode / OpenClaw / Crush / 通用 JSON）
    // ════════════════════════════════════════════════════════════

    /// <summary>OpenCode 格式：provider.&lt;pid&gt;.models.&lt;mid&gt; = { name, limit{context,output}, options{baseURL} }</summary>
    public static List<ModelInfo> ImportOpenCode(string json)
    {
        var result = new List<ModelInfo>();
        var root = Json.Parse(NormalizeJson5(json));
        var providers = Obj(root?["provider"]);
        if (providers == null) return result;

        foreach (var (pid, pcfg) in providers.Entries)
        {
            var pname = pcfg?["name"]?.AsString() ?? pid;
            var baseUrl = pcfg?["options"]?["baseURL"]?.AsString()
                       ?? pcfg?["options"]?["baseUrl"]?.AsString();
            var models = Obj(pcfg?["models"]);
            if (models == null) continue;

            foreach (var (mid, mcfg) in models.Entries)
            {
                var name = mcfg?["name"]?.AsString() ?? mid;
                var limit = Obj(mcfg?["limit"]);
                var ctx = IntOpt(limit?["context"]) ?? 0;
                var maxOut = IntOpt(limit?["output"]) ?? 0;
                var cost = Obj(mcfg?["cost"]);
                var inPrice = DblOpt(cost?["input"]) ?? 0;
                var outPrice = DblOpt(cost?["output"]) ?? 0;
                result.Add(new ModelInfo(mid, name, pname, pid.ToLowerInvariant(), "*", "Imported",
                    ctx, inPrice, outPrice, baseUrl, $"从 OpenCode 导入（{pname}）", maxOut));
            }
        }
        return result;
    }

    /// <summary>OpenClaw 格式：models.providers.&lt;pid&gt;.models[] = { id, name, cost{input,output}, contextWindow, maxTokens }</summary>
    public static List<ModelInfo> ImportOpenClaw(string json)
    {
        var result = new List<ModelInfo>();
        var root = Json.Parse(NormalizeJson5(json));
        var providers = Obj(root?["models"]?["providers"]);
        if (providers == null) return result;

        foreach (var (pid, pcfg) in providers.Entries)
        {
            var baseUrl = pcfg?["baseUrl"]?.AsString();
            var models = Arr(pcfg?["models"]);
            if (models == null) continue;

            foreach (var m in models.Items)
            {
                var info = ParseModelNode(m);
                if (info == null) continue;
                // OpenClaw 用 provider id 作为 key，模型节点无 provider 字段，需回填
                result.Add(info with
                {
                    ProviderId = pid.ToLowerInvariant(),
                    Provider = pid,
                    DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl,
                    Description = $"从 OpenClaw 导入（{pid}）",
                });
            }
        }
        return result;
    }

    /// <summary>Crush 格式：config.providers（若为 JSON），否则返回空（Crush 用 SQLite 存储）</summary>
    public static List<ModelInfo> ImportCrush(string json)
    {
        var result = new List<ModelInfo>();
        var root = Json.Parse(NormalizeJson5(json));
        if (root == null) return result;
        var providers = Obj(root["providers"])
                     ?? Obj(root["provider"]);
        if (providers != null)
        {
            foreach (var (pid, pcfg) in providers.Entries)
            {
                var baseUrl = pcfg?["baseUrl"]?.AsString()
                           ?? pcfg?["baseURL"]?.AsString();
                var modelsNode = pcfg?["models"];
                if (Arr(modelsNode) is { } arr)
                {
                    foreach (var m in arr.Items)
                    {
                        var info = ParseModelNode(m);
                        if (info == null) continue;
                        result.Add(info with { ProviderId = pid.ToLowerInvariant(), Provider = pid, DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl });
                    }
                }
                else if (Obj(modelsNode) is { } mobj)
                {
                    foreach (var (mid, mcfg) in mobj.Entries)
                    {
                        var info = ParseModelNode(mcfg);
                        if (info != null)
                            result.Add(info with { Id = mid, ProviderId = pid.ToLowerInvariant(), Provider = pid, DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl });
                    }
                }
            }
        }
        return result;
    }

    /// <summary>Claude Code 格式：settings.json 的 env 中 ANTHROPIC_MODEL / *_MODEL + BASE_URL</summary>
    public static List<ModelInfo> ImportClaude(string json)
    {
        var result = new List<ModelInfo>();
        var root = Json.Parse(NormalizeJson5(json));
        var env = Obj(root?["env"]);
        if (env == null) return result;

        var baseUrl = env.Entries.FirstOrDefault(kv => kv.Key.Contains("BASE_URL", StringComparison.OrdinalIgnoreCase))
            .Value?.AsString();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, val) in env.Entries)
        {
            if (!key.Contains("MODEL", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.EndsWith("_NAME", StringComparison.OrdinalIgnoreCase)) continue;  // *_MODEL_NAME 是显示名变体
            var name = val.AsString()?.Trim();
            if (string.IsNullOrEmpty(name) || name.Equals("any", StringComparison.OrdinalIgnoreCase)) continue;
            var clean = name.Split('[')[0].Trim();  // 去 [1M] 后缀
            if (!seen.Add(clean)) continue;
            result.Add(new ModelInfo(clean, clean, "Claude Code", "claude", "C", "Imported",
                0, 0, 0, baseUrl, $"从 Claude Code 导入（{key}）", 0));
        }
        return result;
    }

    /// <summary>Codex 格式：config.toml 的 [model_providers.*]（name/base_url）+ 顶层 model + [profiles.*]</summary>
    public static List<ModelInfo> ImportCodex(string toml)
    {
        var result = new List<ModelInfo>();
        var providers = new Dictionary<string, (string Name, string BaseUrl)>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = toml.Replace("\r", "").Split('\n');

        string? globalModel = null;
        string? globalProvider = null;
        string? provSection = null;   // [model_providers.<pid>] 上下文
        string? profProvider = null;  // [profiles.*] 的 model_provider
        string? profModel = null;     // [profiles.*] 的 model
        bool inProfile = false;

        void FlushProfile()
        {
            if (!inProfile || string.IsNullOrEmpty(profModel)) return;
            var pid = profProvider ?? "codex";
            providers.TryGetValue(pid, out var p);
            var pname = string.IsNullOrEmpty(p.Name) ? pid : p.Name;
            if (seen.Add(profModel))
                result.Add(new ModelInfo(profModel, pname, pname, pid.ToLowerInvariant(), "*", "Imported",
                    0, 0, 0, p.BaseUrl, $"从 Codex 导入（profile {pid}）", 0));
            profProvider = null; profModel = null;
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            if (line.StartsWith("[[") && line.EndsWith("]]")) { FlushProfile(); provSection = null; inProfile = false; continue; }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                FlushProfile();
                inProfile = false;
                provSection = null;
                var sec = line.Trim('[', ']').Trim();
                if (sec.StartsWith("model_providers."))
                {
                    provSection = sec["model_providers.".Length..];
                    if (!providers.ContainsKey(provSection)) providers[provSection] = (provSection, "");
                }
                else if (sec.StartsWith("profiles."))
                {
                    inProfile = true;
                    profProvider = null; profModel = null;
                }
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim().Trim('"').Trim('\'');

            if (provSection != null)
            {
                var (name, baseUrl) = providers[provSection];
                if (key == "name") providers[provSection] = (val, baseUrl);
                else if (key == "base_url") providers[provSection] = (name, val);
            }
            else if (inProfile)
            {
                if (key == "model_provider") profProvider = val;
                else if (key == "model") profModel = val;
            }
            else
            {
                if (key == "model") globalModel = val;
                else if (key == "model_provider") globalProvider = val;
            }
        }
        FlushProfile();  // 最后一个 profile

        // provider sections → 每个一个模型条目（当前激活的 provider 用全局 model 名）
        foreach (var (pid, (pname, baseUrl)) in providers)
        {
            var modelId = pid.Equals(globalProvider, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(globalModel)
                ? globalModel : pid;
            if (seen.Add(modelId))
                result.Add(new ModelInfo(modelId, pname, pname, pid.ToLowerInvariant(), "*", "Imported",
                    0, 0, 0, baseUrl, $"从 Codex 导入（{pname}）", 0));
        }

        return result;
    }

    /// <summary>
    /// JSON5 容错标准化：去注释 + 裸 key 加引号 + 去尾逗号，返回纯 JSON。
    /// 用于 OpenClaw / Crush 等可能使用 JSON5/JSONC 的配置。
    /// </summary>
    public static string NormalizeJson5(string text)
    {
        var noComments = StripJsonComments(text);
        var result = new StringBuilder();
        var inString = false;
        var len = noComments.Length;

        for (int i = 0; i < len; i++)
        {
            var ch = noComments[i];
            if (inString)
            {
                result.Append(ch);
                if (ch == '\\' && i + 1 < len) { result.Append(noComments[i + 1]); i++; }
                else if (ch == '"') inString = false;
                continue;
            }
            if (ch == '"') { inString = true; result.Append(ch); continue; }

            // 裸 key：字母/数字/下划线/点/连字符 序列后紧跟冒号
            if (char.IsLetter(ch) || ch == '_' || ch == '$')
            {
                var j = i;
                while (j < len && (char.IsLetterOrDigit(noComments[j]) || noComments[j] is '_' or '-' or '.' or '$'))
                    j++;
                // 跳过空白看是否跟冒号
                var k = j;
                while (k < len && char.IsWhiteSpace(noComments[k])) k++;
                if (k < len && noComments[k] == ':')
                {
                    result.Append('"').Append(noComments, i, j - i).Append('"');
                    i = j - 1;
                    continue;
                }
            }
            result.Append(ch);
        }
        return result.ToString();
    }

    /// <summary>去除 JSONC/JSON5 注释（// 和 /* */）</summary>
    private static string StripJsonComments(string jsonc)
    {
        var result = new StringBuilder();
        var inString = false;
        var inBlockComment = false;
        var inLineComment = false;
        for (int i = 0; i < jsonc.Length; i++)
        {
            var ch = jsonc[i];
            var next = i + 1 < jsonc.Length ? jsonc[i + 1] : '\0';
            if (inBlockComment)
            {
                if (ch == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }
            if (inLineComment)
            {
                if (ch == '\n' || ch == '\r') { inLineComment = false; result.Append(ch); }
                continue;
            }
            if (inString)
            {
                result.Append(ch);
                if (ch == '\\' && next != '\0') { result.Append(next); i++; }
                else if (ch == '"') inString = false;
                continue;
            }
            if (ch == '"') { inString = true; result.Append(ch); continue; }
            if (ch == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (ch == '/' && next == '/') { inLineComment = true; i++; continue; }
            result.Append(ch);
        }
        return result.ToString();
    }

    public static (List<ModelInfo> Models, string Format) TryImport(string json)
    {
        try
        {
            var root = Json.Parse(json);
            if (root == null) return ([], "");

            if (Arr(root["models"]) is { } arr && arr.Count > 0)
                return (ImportFromJson(json), arr[0]?["provider"]?.AsString() != null
                    ? "Continue/Crush" : "Model array");

            if (Obj(root["apiProviders"]) is { } providers && providers.Count > 0)
                return (ImportFromJson(json), "Cline");

            if (Arr(root) is { } sa && sa.Count > 0)
                return (ImportFromJson(json), "Simple array");

            return ([], "");
        }
        catch { return ([], ""); }
    }
}
