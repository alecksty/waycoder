using System.Text.Json;

namespace WayCoder;

/// <summary>
/// Model Catalog — built-in model registry + external config import (OpenCode / Crush / Continue / Cline).
/// Browse, search, and model metadata. Compatible with most OpenAI-compatible APIs.
/// </summary>
public static class ModelCatalog
{
    /// <summary>Model metadata</summary>
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
        string Description
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
            ["local"]      = ("Local",        ""),
            ["custom"]     = ("Custom",       ""),
        };
    }

    // Query helpers
    public static ModelInfo? Find(string id) =>
        BuiltIn.FirstOrDefault(m => m.Id == id);

    /// <summary>
    /// 解析模型的上下文窗口大小。优先用内置模型目录的 ContextWindow，
    /// 未知模型或窗口为 0 时回退到 fallback（默认 1M）。
    /// 用于切换模型时同步 Agent 的上下文窗口上限。
    /// </summary>
    public static int ResolveContextWindow(string? modelId, int fallback = 1_048_576)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return fallback;
        var info = Find(modelId);
        return info != null && info.ContextWindow > 0 ? info.ContextWindow : fallback;
    }

    public static ModelInfo[] ByProvider(string providerId) =>
        BuiltIn.Where(m => m.ProviderId == providerId).ToArray();

    public static string[] ProviderIds =>
        BuiltIn.Select(m => m.ProviderId).Distinct().ToArray();

    public static string[] Categories =>
        BuiltIn.Select(m => m.Category).Distinct().ToArray();

    public static ModelInfo[] Search(string query)
    {
        return BuiltIn.Where(m =>
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
            var root = JsonNode.Parse(json);
            if (root == null) return result;

            if (root["models"]?.AsArray() is { } modelsArr)
            {
                foreach (var m in modelsArr)
                {
                    var info = ParseModelNode(m);
                    if (info != null) result.Add(info);
                }
                return result;
            }

            if (root["apiProviders"]?.AsObject() is { } providers)
            {
                foreach (var (providerId, config) in providers)
                {
                    var providerName = config?["name"]?.GetValue<string>() ?? providerId;
                    var baseUrl = config?["baseUrl"]?.GetValue<string>();
                    if (config?["models"]?.AsArray() is { } providerModels)
                    {
                        foreach (var m in providerModels)
                        {
                            var modelId = m?.GetValue<string>()
                                ?? m?["model"]?.GetValue<string>()
                                ?? m?["id"]?.GetValue<string>();
                            if (string.IsNullOrWhiteSpace(modelId)) continue;
                            result.Add(new ModelInfo(modelId, modelId, providerName,
                                providerId.ToLowerInvariant(), "*", "Imported", 0, 0, 0, baseUrl, "Imported from Cline"));
                        }
                    }
                }
                return result;
            }

            if (root.AsArray() is { } simpleArr)
            {
                foreach (var item in simpleArr)
                {
                    var id = item?.GetValue<string>();
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

    private static ModelInfo? ParseModelNode(JsonNode? node)
    {
        if (node == null) return null;
        var id = node["model"]?.GetValue<string>()
              ?? node["id"]?.GetValue<string>()
              ?? node["name"]?.GetValue<string>()
              ?? node.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id)) return null;

        return new ModelInfo(
            id,
            node["displayName"]?.GetValue<string>() ?? node["name"]?.GetValue<string>() ?? id,
            node["provider"]?.GetValue<string>() ?? "Imported",
            (node["provider"]?.GetValue<string>() ?? "import").ToLowerInvariant().Replace(" ", "-"),
            "*", "Imported",
            node["contextWindow"]?.GetValue<int>() ?? node["maxTokens"]?.GetValue<int>() ?? 0,
            0, 0,
            node["baseUrl"]?.GetValue<string>() ?? node["apiBase"]?.GetValue<string>(),
            "Imported from external config"
        );
    }

    public static (List<ModelInfo> Models, string Format) TryImport(string json)
    {
        try
        {
            var root = JsonNode.Parse(json);
            if (root == null) return ([], "");

            if (root["models"]?.AsArray() is { } arr && arr.Count > 0)
                return (ImportFromJson(json), arr[0]?["provider"]?.GetValue<string>() != null
                    ? "Continue/Crush" : "Model array");

            if (root["apiProviders"]?.AsObject() is { } providers && providers.Count > 0)
                return (ImportFromJson(json), "Cline");

            if (root.AsArray() is { } sa && sa.Count > 0)
                return (ImportFromJson(json), "Simple array");

            return ([], "");
        }
        catch { return ([], ""); }
    }
}
