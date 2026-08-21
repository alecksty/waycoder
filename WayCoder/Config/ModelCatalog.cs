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
        int MaxOutput = 0,
        double InputPriceOffpeak = 0,   // 闲时输入价（$ / MTok，0=无闲时价，只显示忙时价）
        double OutputPriceOffpeak = 0   // 闲时输出价（$ / MTok）
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
        new("claude-opus-5", "Claude Opus 5", "Anthropic", "anthropic", "A", "Flagship", 200_000, 5, 25, "https://api.anthropic.com", "Best code intelligence"),
        new("claude-sonnet-5", "Claude Sonnet 5", "Anthropic", "anthropic", "A", "Flagship", 200_000, 2, 10, "https://api.anthropic.com", "High-performance code"),
        new("claude-haiku-4-5", "Claude Haiku 4.5", "Anthropic", "anthropic", "A", "Light", 200_000, 1, 5, "https://api.anthropic.com", "Fast and light"),
        new("claude-opus-4-6", "Claude Opus 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 5, 25, "https://api.anthropic.com", "Best code (old)"),
        new("claude-sonnet-4-6", "Claude Sonnet 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 3, 15, "https://api.anthropic.com", "High-perf code (old)"),

        // DeepSeek
        new("deepseek-v4-pro", "DeepSeek V4 Pro", "DeepSeek", "deepseek", "D", "Flagship", 1_048_576, 0.66, 1.98, "https://api.deepseek.com", "Flagship deep reasoning"),
        new("deepseek-v4-flash", "DeepSeek V4 Flash", "DeepSeek", "deepseek", "D", "Light", 1_048_576, 0.14, 0.28, "https://api.deepseek.com", "Fast and cost-effective"),
        new("deepseek-chat", "DeepSeek V3 (old)", "DeepSeek", "deepseek", "D", "Flagship", 64_000, 0.27, 1.10, "https://api.deepseek.com", "V3 legacy"),
        new("deepseek-reasoner", "DeepSeek R1", "DeepSeek", "deepseek", "D", "Reasoning", 64_000, 0.55, 2.19, "https://api.deepseek.com", "Deep reasoning"),

        // Google
        new("gemini-2.5-pro", "Gemini 2.5 Pro", "Google", "google", "G", "Flagship", 1_000_000, 1.25, 10, "https://generativelanguage.googleapis.com/v1beta/openai", "Ultra-long context"),
        new("gemini-2.5-flash", "Gemini 2.5 Flash", "Google", "google", "G", "Light", 1_000_000, 0.30, 2.50, "https://generativelanguage.googleapis.com/v1beta/openai", "Ultra-long light"),
        new("gemini-2.0-flash", "Gemini 2.0 Flash", "Google", "google", "G", "Light", 1_000_000, 0.10, 0.4, "https://generativelanguage.googleapis.com/v1beta/openai", "Ultra-fast light"),

        // Alibaba Qwen
        new("qwen3-max", "Qwen3 Max", "Alibaba", "qwen", "Q", "Flagship", 128_000, 0.78, 3.9, "https://dashscope.aliyuncs.com/compatible-mode/v1", "Alibaba flagship"),
        new("qwen3-plus", "Qwen3 Plus", "Alibaba", "qwen", "Q", "Light", 128_000, 0.26, 0.78, null, "Alibaba cost-effective"),
        new("qwen-max", "Qwen Max", "Alibaba", "qwen", "Q", "Flagship", 32_000, 0.78, 3.9, null, "Alibaba old flagship"),
        new("qwen-plus", "Qwen Plus", "Alibaba", "qwen", "Q", "Light", 131_072, 0.26, 0.78, null, "Alibaba old light"),
        new("qwen-turbo", "Qwen Turbo", "Alibaba", "qwen", "Q", "Light", 1_000_000, 0.05, 0.15, null, "Alibaba ultra-fast"),

        // Moonshot Kimi
        new("kimi-k2.5", "Kimi K2.5", "Moonshot", "moonshot", "M", "Flagship", 128_000, 0.45, 2.25, "https://api.moonshot.cn", "Chinese flagship"),

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
            ["google"]     = ("Google",       "https://generativelanguage.googleapis.com/v1beta/openai"),
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
            ["opencode-go"]  = ("OpenCode Go",  "https://opencode.ai/zen/go/v1"),  // 订阅制
            ["opencode-zen"] = ("OpenCode Zen", "https://opencode.ai/zen/v1"),     // 按量付费
            ["opencode"]     = ("OpenCode",     "https://opencode.ai/zen/v1"),     // 旧数据兼容别名
            ["minimax"]    = ("MiniMax",      "https://api.minimaxi.com/v1"),
            ["aihubmix"]   = ("AIHubMix",     "https://aihubmix.com/v1"),
            ["local"]      = ("Local",        ""),
            ["custom"]     = ("Custom",       ""),
        };
        // 服务商数据库：首次运行生成 ~/.waycoder/providers.json，之后从它加载（用户可编辑扩展服务商）
        LoadOrCreateProvidersJson();
    }

    /// <summary>服务商数据库文件（~/.waycoder/providers.json）：id / name / base_url，可编辑扩展。</summary>
    public static string ProvidersJsonPath => Global.GlobalConfigPath("providers.json");

    /// <summary>从 providers.json 加载服务商（覆盖内置同名 + 新增自定义）；文件不存在则先生成。</summary>
    private static void LoadOrCreateProvidersJson()
    {
        try
        {
            if (!File.Exists(ProvidersJsonPath)) { SaveProvidersJson(); return; }
            var root = Json.Parse(NormalizeJson5(File.ReadAllText(ProvidersJsonPath)));
            foreach (var (id, p) in Obj(root?["providers"])?.Entries ?? [])
            {
                var name = p?["name"]?.AsString() ?? id;
                var url = p?["base_url"]?.AsString() ?? p?["baseUrl"]?.AsString() ?? "";
                Providers[id] = (name, url);
            }
        }
        catch { }
    }

    /// <summary>把当前内置服务商写为 providers.json 数据库（含 id / name / base_url 备注）。</summary>
    private static void SaveProvidersJson()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  // 服务商数据库：id / name / base_url。可编辑扩展，代码按 base_url 识别服务商。");
            sb.AppendLine("  // 新增服务商：{ \"providers\": { \"myai\": { \"name\": \"MyAI\", \"base_url\": \"https://myai.com/v1\" } } }");
            sb.AppendLine("  \"providers\": {");
            var entries = Providers.ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var kv = entries[i];
                var comma = i < entries.Count - 1 ? "," : "";
                sb.Append($"    \"{kv.Key}\": {{ \"name\": \"{kv.Value.DisplayName}\", \"base_url\": \"{kv.Value.DefaultBaseUrl}\" }}{comma}\n");
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");
            var dir = Path.GetDirectoryName(ProvidersJsonPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ProvidersJsonPath, sb.ToString());
        }
        catch { }
    }

    /// <summary>注册/更新服务商到 providers.json（导入的服务商地址确认可用后调用）。id 规范化（全小写、去特殊符号）。</summary>
    public static void RegisterProvider(string providerId, string displayName, string baseUrl)
    {
        providerId = NormalizeId(providerId);
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(baseUrl)) return;
        Providers[providerId] = (displayName, baseUrl);
        SaveProvidersJson();
    }

    /// <summary>移除服务商（providers.json），同时清除其 API Key。返回是否移除。</summary>
    public static bool RemoveProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) return false;
        if (!Providers.Remove(providerId)) return false;
        SaveProvidersJson();
        ApiKeyStore.Remove(providerId);
        return true;
    }

    // ════════════════════════════════════════════════════════════
    // 自定义模型库（按供应商分类分文件：全局 ~/.waycoder/provider/{供应商}.json + 本地 .waycoder/provider/）。
    // 兼容旧单文件 models.json：仍作为读源（迁移后写入 provider/ 分类文件）。
    // 内置目录为兜底，外置库按 Id 覆盖/追加，本地覆盖全局。
    // ════════════════════════════════════════════════════════════

    private static readonly object _lock = new();
    private static Dictionary<string, ModelInfo>? _custom;
    private static ModelInfo[]? _all;

    /// <summary>旧版单文件模型库路径（全局，仅兼容读 + 一次性迁移）</summary>
    public static string GlobalModelsPath => Global.GlobalConfigPath("models.json");

    /// <summary>旧版单文件模型库路径（本地，仅兼容读 + 一次性迁移）</summary>
    public static string LocalModelsPath =>
        Path.Combine(Environment.CurrentDirectory, Global.ConfigDirName, "models.json");

    /// <summary>按供应商分类的模型库目录（全局 ~/.waycoder/provider/）</summary>
    public static string GlobalProviderDir => Global.GlobalConfigPath("provider");

    /// <summary>按供应商分类的模型库目录（本地 .waycoder/provider/）</summary>
    public static string LocalProviderDir =>
        Path.Combine(Environment.CurrentDirectory, Global.ConfigDirName, "provider");

    /// <summary>供应商 → 分类文件名：local/custom/ollama/lmstudio 等本地模型归 locals.json，其余按 providerId 命名。</summary>
    public static string ProviderFile(string providerId, bool local)
    {
        var group = ProviderGroupName(providerId);
        var dir = local ? LocalProviderDir : GlobalProviderDir;
        return Path.Combine(dir, group + ".json");
    }

    /// <summary>计算供应商归属的分类文件名（不含扩展名）。id 规范化（全小写、去特殊符号），防路径穿越。</summary>
    public static string ProviderGroupName(string? providerId)
    {
        var pid = NormalizeId(providerId ?? "custom");
        if (pid is "local" or "custom" or "ollama" or "lmstudio" or "lm-studio" or "locals" or "open-webui" or "vllm" or "text-generation-webui" or "localai")
            return "locals";
        return pid.Length > 0 ? pid : "custom";
    }

    /// <summary>
    /// 规范化 id：全小写、特殊符号（空格/点/下划线/斜杠 等）统一为连字符或去掉，作为唯一 id 表示。
    /// 服务商和模型都用此规范化 id 区分。例：AIHubMix → aihubmix；01.AI → 01ai；AWS Bedrock → aws-bedrock。
    /// </summary>
    internal static string NormalizeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return id;
        var sb = new StringBuilder();
        foreach (var c in id.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is '-' or '_' or ' ' or '.' or '/')
            {
                if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
            }
            // 其他特殊符号（括号/emoji 等）直接去掉
        }
        return sb.ToString().Trim('-');
    }

    /// <summary>
    /// 模型唯一键：服务商 + 模型名（同一服务商下模型 id 唯一，不因导入来源/地址差异产生重复；
    /// 不同服务商可同名，如 opencode-go/deepseek-v4-pro 与 opencode-zen/deepseek-v4-pro 是两个不同服务商）。
    /// 服务商唯一性由 providers.json 按地址去重负责（同地址只一个 provider）。
    /// </summary>
    internal static string ModelKey(string providerId, string id) =>
        string.IsNullOrWhiteSpace(providerId) ? id + "|" : providerId + "|" + id;

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
                // 清空过内置模型（ClearAll）后 All 不再包含内置目录，只有自定义 —— 「清空后重新导入」的纯粹模型库
                var list = BuiltInCleared ? new List<ModelInfo>() : new List<ModelInfo>(BuiltIn);
                foreach (var (_, m) in LoadCustom())
                {
                    // 同 Id 且同 baseUrl 覆盖内置；不同 baseUrl 视为不同服务商追加（都保留显示）
                    var idx = list.FindIndex(x => x.Id == m.Id
                        && string.Equals(x.DefaultBaseUrl ?? "", m.DefaultBaseUrl ?? "", StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) list[idx] = m;
                    else list.Add(m);
                }
                _all = list.ToArray();
                return _all;
            }
        }
    }

    /// <summary>新增/更新自定义模型，返回写入的文件路径。local=true 写本地，否则写全局。
    /// 文件读-改-写持统一锁（_lock），防 Web 并发导入/删除 read-modify-write 竞争丢模型。</summary>
    public static string AddCustom(ModelInfo info, bool local = false)
    {
        lock (_lock)
        {
            var path = ProviderFile(info.ProviderId, local);
            var models = ReadFile(path);
            models[ModelKey(info.ProviderId, info.Id)] = info;
            SaveCustom(models, path);
            Invalidate();
            return path;
        }
    }

    /// <summary>批量新增/更新自定义模型：同分类文件合并为一次写（防 N 次磁盘写 + N 次缓存失效）。返回写入数。</summary>
    public static int AddCustomRange(IEnumerable<ModelInfo> infos, bool local = false)
    {
        var list = infos.ToList();
        if (list.Count == 0) return 0;
        lock (_lock)
        {
            foreach (var g in list.GroupBy(m => ProviderFile(m.ProviderId, local)))
            {
                var models = ReadFile(g.Key);
                foreach (var m in g) models[ModelKey(m.ProviderId, m.Id)] = m;
                SaveCustom(models, g.Key);
            }
            Invalidate();
            return list.Count;
        }
    }

    /// <summary>删除自定义模型（从全局和本地的所有模型文件移除），返回受影响文件列表。</summary>
    public static string[] RemoveCustom(string id)
    {
        lock (_lock)
        {
            var removed = new List<string>();
            foreach (var file in EnumerateModelFiles())
            {
                var models = ReadFile(file);
                // 移除该 id 的全部变体（providerId|id，跨服务商同名都删）
                var toRemove = models.Keys.Where(k =>
                    k.EndsWith("|" + id, StringComparison.OrdinalIgnoreCase)).ToList();
                if (toRemove.Count > 0)
                {
                    foreach (var k in toRemove) models.Remove(k);
                    // 分类文件删空后删除文件本身，避免残留空 [  ] 文件
                    if (models.Count == 0) { TryDeleteFile(file); }
                    else SaveCustom(models, file);
                    removed.Add(file);
                }
            }
            if (removed.Count > 0) Invalidate();
            return removed.ToArray();
        }
    }

    /// <summary>仅列出自定义模型（不含内置）</summary>
    public static ModelInfo[] ListCustom() => LoadCustom().Values.OrderBy(m => m.Id).ToArray();

    /// <summary>内置模型是否已被清空（清空标记文件存在）。持久化：重启后 All 也不含内置目录。</summary>
    public static string BuiltInClearedPath => Global.GlobalConfigPath("models_cleared");
    public static bool BuiltInCleared
    {
        get { try { return File.Exists(BuiltInClearedPath); } catch { return false; } }
    }

    /// <summary>清空全部模型（内置 + 所有自定义），供「清空后重新导入」获得纯粹的空模型库。
    /// 删除自定义模型文件 + 持久化内置已清空标记；返回删除的自定义文件数。清空后需 Invalidate 已由内部处理。</summary>
    public static int ClearAll()
    {
        int n;
        lock (_lock)
        {
            n = DeleteAllCustomFiles();
            try { File.WriteAllText(BuiltInClearedPath, "1"); } catch { }
            _all = null;
            _custom = null;
        }
        return n;
    }

    /// <summary>恢复内置模型目录（清除清空标记）。清空（ClearAll）后可经「本地导入→内置模型」恢复。</summary>
    public static void RestoreBuiltIn()
    {
        lock (_lock)
        {
            try { if (File.Exists(BuiltInClearedPath)) File.Delete(BuiltInClearedPath); } catch { }
            _all = null;
        }
    }

    /// <summary>删除全局+本地全部自定义模型文件（provider 分文件 + 汇总 models.json），返回删除数。</summary>
    private static int DeleteAllCustomFiles()
    {
        int n = 0;
        foreach (var dir in new[] { GlobalProviderDir, LocalProviderDir })
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                // 删除所有供应商模型列表 json 文件（两层架构：provider 下模型库）
                foreach (var f in Directory.EnumerateFiles(dir, "*.json"))
                { try { File.Delete(f); n++; } catch { } }
                // 删空后移除 provider 目录本身，避免残留空目录
                try { if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir); } catch { }
            }
            catch { }
        }
        TryDeleteFile(GlobalModelsPath);
        TryDeleteFile(LocalModelsPath);
        return n;
    }

    /// <summary>删除某服务商下的所有自定义模型（删除对应分类文件或从旧 models.json 移除），返回删除数量。</summary>
    public static int RemoveCustomByProvider(string providerId)
    {
        lock (_lock)
        {
            var removed = 0;
            // 新结构：直接删除该供应商的分类文件（全局+本地）
            foreach (var local in new[] { false, true })
            {
                var file = ProviderFile(providerId, local);
                if (File.Exists(file))
                {
                    var models = ReadFile(file);
                    var toRemove = models
                        .Where(kv => kv.Value.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Key)
                        .ToArray();
                    foreach (var id in toRemove)
                    {
                        models.Remove(id);
                        removed++;
                    }
                    // 分类文件删空后删除文件本身
                    if (toRemove.Length > 0)
                    {
                        if (models.Count == 0) TryDeleteFile(file);
                        else SaveCustom(models, file);
                    }
                }
            }
            // 兼容旧 models.json：按 providerId 移除
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
                if (toRemove.Length > 0)
                {
                    if (models.Count == 0) TryDeleteFile(path);
                    else SaveCustom(models, path);
                }
            }
            if (removed > 0) Invalidate();
            return removed;
        }
    }

    /// <summary>清除内存缓存并强制下次重新加载（外部改了模型文件后调用）</summary>
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
            MigrateLegacyModels(); // 首次加载：把旧 models.json 迁移到 provider/ 分类文件
            var merged = new Dictionary<string, ModelInfo>();
            foreach (var file in EnumerateModelFiles())
                foreach (var m in ReadFile(file).Values) merged[ModelKey(m.ProviderId, m.Id)] = m;
            _custom = merged;
            return _custom;
        }
    }

    /// <summary>枚举所有模型文件：provider/ 分类文件（全局+本地）+ 旧 models.json（全局+本地，兼容）。</summary>
    private static IEnumerable<string> EnumerateModelFiles()
    {
        foreach (var dir in new[] { GlobalProviderDir, LocalProviderDir })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                yield return file;
        }
        if (File.Exists(GlobalModelsPath)) yield return GlobalModelsPath;
        if (File.Exists(LocalModelsPath)) yield return LocalModelsPath;
    }

    /// <summary>一次性迁移：把旧 models.json 的模型按供应商分类合并写入 provider/ 文件，全部写成功后才删除旧文件。
    /// 迁移完成写 .migrated 标记（防误判：仅凭 provider/ 有文件就删旧数据，会让未迁移的旧模型永久丢失）。</summary>
    private static void MigrateLegacyModels()
    {
        foreach (var legacy in new[] { (GlobalModelsPath, false), (LocalModelsPath, true) })
        {
            var file = legacy.Item1;
            if (!File.Exists(file)) continue;
            var dir = legacy.Item2 ? LocalProviderDir : GlobalProviderDir;
            var mark = Path.Combine(dir, ".migrated");

            // 已有迁移标记：旧文件残留则清理，不再重复迁移
            if (File.Exists(mark))
            {
                TryDeleteFile(file);
                continue;
            }

            var models = ReadFile(file);
            if (models.Count == 0) { TryDeleteFile(file); continue; }

            // 合并写入各分类文件（保留 provider/ 已存在的其他模型，不覆盖）
            var allOk = true;
            foreach (var g in models.Values.GroupBy(m => ProviderGroupName(m.ProviderId)))
            {
                var path = Path.Combine(dir, g.Key + ".json");
                var existing = ReadFile(path);
                foreach (var m in g)
                    existing[ModelKey(m.ProviderId, m.Id)] = m;
                if (!SaveCustom(existing, path)) { allOk = false; break; }
            }

            // 全部写成功才删旧文件 + 写迁移标记；任何失败保留旧文件防数据丢失
            if (!allOk) continue;
            try { Directory.CreateDirectory(dir); File.WriteAllText(mark, DateTime.UtcNow.ToString("O")); } catch { }
            TryDeleteFile(file);
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
                    if (info != null) result[ModelKey(info.ProviderId, info.Id)] = info;
                }
            }
        }
        catch (Exception ex)
        {
            // 损坏/截断文件静默返回空会令模型「无声消失」并被后续写回固化，记录日志便于排查
            ErrorLog.Warning("ModelCatalog", $"读取模型文件失败（将视为空）: {path}", ex);
        }
        return result;
    }

    /// <summary>写模型文件（临时文件 + 原子 rename 替换，防崩溃/磁盘满留下截断文件覆盖全量数据）。返回是否成功。</summary>
    private static bool SaveCustom(Dictionary<string, ModelInfo> models, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            var arr = JNode.Array();
            foreach (var m in models.Values.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase))
                arr.Add(ToJson(m));
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, arr.ToJson(indent: true));
            File.Move(tmp, path, overwrite: true); // 同卷原子替换
            return true;
        }
        catch { return false; } // 写入失败不崩溃，由调用方决定是否保留现场
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
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
        .Set("inputPriceOffpeak", m.InputPriceOffpeak)
        .Set("outputPriceOffpeak", m.OutputPriceOffpeak)
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
            IntOpt(node["maxOutput"]) ?? 0,
            DblOpt(node["inputPriceOffpeak"]) ?? 0,
            DblOpt(node["outputPriceOffpeak"]) ?? 0
        );
    }

    // Query helpers
    /// <summary>
    /// 按 id 查模型（无 baseUrl 上下文）：内置官方网关优先（默认访问入口），无内置则第一个自定义变体。
    /// 同 id 不同 baseUrl 视为不同服务商——需精确指定服务商时用 <see cref="Find(string, string?)"/>。
    /// </summary>
    public static ModelInfo? Find(string id)
    {
        var builtIn = BuiltIn.FirstOrDefault(m => m.Id == id);
        if (builtIn != null) return builtIn;
        var custom = LoadCustom();
        return custom.Values.FirstOrDefault(m => m.Id == id);
    }

    /// <summary>按 id + baseUrl 精确查模型（地址不同 = 不同服务商）。baseUrl 为空回退 <see cref="Find(string)"/>。</summary>
    public static ModelInfo? Find(string id, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return Find(id);
        var url = baseUrl.Trim();
        var custom = LoadCustom();
        var c = custom.Values.FirstOrDefault(m => m.Id == id
            && string.Equals(m.DefaultBaseUrl ?? "", url, StringComparison.OrdinalIgnoreCase));
        if (c != null) return c;
        return BuiltIn.FirstOrDefault(m => m.Id == id
            && string.Equals(m.DefaultBaseUrl ?? "", url, StringComparison.OrdinalIgnoreCase));
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

        // 上下文 / 输出窗口：兼容 contextWindow / maxTokens / limit.context / limit.output / Crush snake_case
        var limit = Obj(node["limit"]);
        var contextWindow = IntOpt(node["contextWindow"])
            ?? IntOpt(limit?["context"])
            ?? IntOpt(node["contextLength"])
            ?? IntOpt(node["context_window"]) // Crush
            ?? 0;
        var maxOutput = IntOpt(node["maxOutput"])
            ?? IntOpt(limit?["output"])
            ?? IntOpt(node["maxTokens"])
            ?? IntOpt(node["default_max_tokens"]) // Crush
            ?? 0;

        // 计费：兼容 cost.input/output、pricing.input/output、inputPrice/outputPrice、Crush cost_per_1m_*
        var cost = Obj(node["cost"]);
        var inputPrice = DblOpt(node["inputPrice"])
            ?? DblOpt(cost?["input"])
            ?? DblOpt(node["pricing"]?["input"])
            ?? DblOpt(node["cost_per_1m_in"]) // Crush
            ?? 0;
        var outputPrice = DblOpt(node["outputPrice"])
            ?? DblOpt(cost?["output"])
            ?? DblOpt(node["pricing"]?["output"])
            ?? DblOpt(node["cost_per_1m_out"]) // Crush
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

    /// <summary>
    /// OpenCode 在线 API（OpenAI 兼容 /models 端点）：{data:[{id, object, created, owned_by}]}。
    /// 按模型 id 前缀推断真实供应商（minimax-*→minimax、kimi-*→moonshot 等），分类到各自供应商文件；
    /// baseUrl 保留 opencode zen 网关（模型经网关访问，保证可用性）。
    /// </summary>
    public static List<ModelInfo> ImportOpenCodeApi(string json, string baseUrl)
    {
        var result = new List<ModelInfo>();
        JNode? root;
        try { root = Json.Parse(NormalizeJson5(json)); }
        catch { return result; } // 畸形输入返回空
        var data = Arr(root?["data"]);
        if (data == null) return result;

        foreach (var m in data.Items)
        {
            var id = m?["id"]?.AsString();
            if (string.IsNullOrWhiteSpace(id)) continue;
            // 按服务地址归类：opencode 网关分 Go(zen/go/v1) / Zen(zen/v1) 两个服务商，地址决定归属
            var pid = InferProviderFromBaseUrl(baseUrl) ?? "opencode-go";
            var pname = pid == "opencode-zen" ? "OpenCode Zen" : "OpenCode Go";
            result.Add(new ModelInfo(id, id, pname, pid, "*", "Imported",
                0, 0, 0, baseUrl, $"从 OpenCode 在线导入（{pname}）", 0));
        }
        return result;
    }

    /// <summary>按模型 id 前缀/包含推断供应商（opencode 网关统一提供，但分类按真实供应商）。</summary>
    public static (string ProviderId, string ProviderName) InferProviderFromId(string modelId)
    {
        var id = modelId.ToLowerInvariant();
        (string pid, string pname) P(string pid, string pname) => (pid, pname);

        if (id.Contains("minimax") || id.Contains("mimo")) return P("minimax", "MiniMax");
        if (id.Contains("kimi")) return P("moonshot", "Kimi");
        if (id.Contains("glm") || id.Contains("zhipu")) return P("zhipu", "GLM");
        if (id.Contains("qwen") || id.Contains("qwq")) return P("qwen", "Alibaba Qwen");
        if (id.Contains("deepseek")) return P("deepseek", "DeepSeek");
        if (id.StartsWith("gpt") || id.StartsWith("o1") || id.StartsWith("o3") || id.StartsWith("o4")) return P("openai", "OpenAI");
        if (id.Contains("claude")) return P("anthropic", "Anthropic");
        if (id.Contains("gemini")) return P("google", "Google");
        if (id.Contains("grok")) return P("xai", "xAI");
        if (id.Contains("hunyuan") || id.StartsWith("hy")) return P("hunyuan", "Hunyuan");
        if (id.Contains("doubao") || id.Contains("seed")) return P("doubao", "Doubao");
        if (id.Contains("llama")) return P("meta", "Meta");
        if (id.Contains("mistral") || id.Contains("codestral")) return P("mistral", "Mistral");
        if (id.Contains("openrouter")) return P("openrouter", "OpenRouter");
        return P("opencode", "OpenCode");
    }

    /// <summary>
    /// 按服务地址(base_url)推断服务商：识别常见网关/供应商主机名，不可识别返回 null（调用方回退来源 pid）。
    /// 导入时优先用它归类 —— 服务商由「请求打到哪」决定，而非模型名或配置里的 pid（opencode 网关提供的
    /// deepseek-v4-flash 归 opencode，不归 deepseek）。
    /// </summary>
    public static string? InferProviderFromBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        string host;
        try { host = Uri.TryCreate(baseUrl, UriKind.Absolute, out var u) ? u.Host.ToLowerInvariant() : baseUrl.ToLowerInvariant(); }
        catch { host = baseUrl.ToLowerInvariant(); }

        if (host.Contains("opencode"))
        {
            // OpenCode 分两个服务商：Go（zen/go/v1，订阅制）与 Zen（zen/v1，按量付费），按路径区分
            var p = (baseUrl ?? "").ToLowerInvariant();
            return (p.Contains("/zen/go/") || p.Contains("/go/v1")) ? "opencode-go" : "opencode-zen";
        }
        if (host.Contains("openrouter")) return "openrouter";
        if (host.Contains("deepseek")) return "deepseek";
        if (host.Contains("openai")) return "openai";
        if (host.Contains("anthropic")) return "anthropic";
        if (host.Contains("generativelanguage") || host.Contains("googleapis")) return "google";
        if (host.Contains("dashscope") || host.Contains("aliyun")) return "qwen";
        if (host.Contains("bigmodel") || host.Contains("zhipu")) return "zhipu";
        if (host.Contains("moonshot")) return "moonshot";
        if (host.Contains("volces") || host.Contains("bytedance") || host.Contains("ark")) return "bytedance";
        if (host.Contains("x.ai") || host.Contains("xai")) return "xai";
        if (host.Contains("mistral")) return "mistral";
        if (host.Contains("groq")) return "groq";
        if (host.Contains("together")) return "together";
        if (host.StartsWith("localhost") || host.StartsWith("127.") || host.StartsWith("0.0.0.0")) return "local";
        return null;
    }

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
                // 按服务地址归类（opencode 网关地址 → opencode，而非配置里的 pid）
                var effPid = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant();
                result.Add(new ModelInfo(mid, name, pname, effPid, "*", "Imported",
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
                // OpenClaw 用 provider id 作为 key，模型节点无 provider 字段，需回填；服务商按 base_url 归类
                var effPid = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant();
                result.Add(info with
                {
                    ProviderId = effPid,
                    Provider = pid,
                    DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl,
                    Description = $"从 OpenClaw 导入（{pid}）",
                });
            }
        }
        return result;
    }

    /// <summary>
    /// Crush 模型数据。两种格式：
    ///   1. providers.json（Catwalk 内置目录）—— 数组 [{ id, name, api_endpoint, models:[{id,name,cost_per_1m_in/out,context_window,default_max_tokens}] }]
    ///   2. crush.json（用户自定义）—— { providers: { &lt;pid&gt;: { type, base_url, models:[...] } } }
    /// </summary>
    public static List<ModelInfo> ImportCrush(string json)
    {
        var result = new List<ModelInfo>();
        var root = Json.Parse(NormalizeJson5(json));
        if (root == null) return result;

        // 格式 1：providers.json 数组（Catwalk 目录）
        if (Arr(root) is { } list)
        {
            foreach (var item in list.Items)
            {
                var pid = item?["id"]?.AsString() ?? item?["name"]?.AsString();
                if (string.IsNullOrWhiteSpace(pid)) continue;
                var pname = item?["name"]?.AsString() ?? pid;
                var baseUrl = item?["api_endpoint"]?.AsString()
                           ?? item?["base_url"]?.AsString()
                           ?? item?["baseUrl"]?.AsString();
                if (Arr(item?["models"]) is not { } models) continue;
                foreach (var m in models.Items)
                {
                    var info = ParseModelNode(m);
                    if (info == null) continue;
                    // 服务商按 base_url 归类
                    var effPid = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant();
                    result.Add(info with
                    {
                        ProviderId = effPid,
                        Provider = pname,
                        DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl,
                        Description = $"从 Crush 导入（{pname}）",
                    });
                }
            }
            return result;
        }

        // 格式 2：crush.json providers 对象
        var providers = Obj(root["providers"])
                     ?? Obj(root["provider"]);
        if (providers != null)
        {
            foreach (var (pid, pcfg) in providers.Entries)
            {
                var baseUrl = pcfg?["base_url"]?.AsString() // Crush 用 snake_case
                           ?? pcfg?["baseUrl"]?.AsString()
                           ?? pcfg?["baseURL"]?.AsString();
                var modelsNode = pcfg?["models"];
                if (Arr(modelsNode) is { } arr)
                {
                    foreach (var m in arr.Items)
                    {
                        var info = ParseModelNode(m);
                        if (info == null) continue;
                        result.Add(info with
                        {
                            ProviderId = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant(),
                            Provider = pid,
                            DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl,
                            Description = $"从 Crush 导入（{pid}）",
                        });
                    }
                }
                else if (Obj(modelsNode) is { } mobj)
                {
                    foreach (var (mid, mcfg) in mobj.Entries)
                    {
                        var info = ParseModelNode(mcfg);
                        if (info != null)
                            result.Add(info with
                            {
                                Id = mid,
                                ProviderId = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant(),
                                Provider = pid,
                                DefaultBaseUrl = info.DefaultBaseUrl ?? baseUrl,
                                Description = $"从 Crush 导入（{pid}）",
                            });
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
            // 服务商按 base_url 归类（Claude Code 若配了 opencode/其它网关地址，归对应服务商而非 claude）
            var effPid = InferProviderFromBaseUrl(baseUrl) ?? "claude";
            result.Add(new ModelInfo(clean, clean, "Claude Code", effPid, "C", "Imported",
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
            {
                // 服务商按 base_url 归类（配了 opencode/其它网关地址则归对应服务商）
                var effPid = InferProviderFromBaseUrl(p.BaseUrl) ?? pid.ToLowerInvariant();
                result.Add(new ModelInfo(profModel, pname, pname, effPid, "*", "Imported",
                    0, 0, 0, p.BaseUrl, $"从 Codex 导入（profile {pid}）", 0));
            }
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

        // provider sections → 每个一个模型条目（当前激活的 provider 用全局 model 名）；服务商按 base_url 归类
        foreach (var (pid, (pname, baseUrl)) in providers)
        {
            var modelId = pid.Equals(globalProvider, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(globalModel)
                ? globalModel : pid;
            if (seen.Add(modelId))
            {
                var effPid = InferProviderFromBaseUrl(baseUrl) ?? pid.ToLowerInvariant();
                result.Add(new ModelInfo(modelId, pname, pname, effPid, "*", "Imported",
                    0, 0, 0, baseUrl, $"从 Codex 导入（{pname}）", 0));
            }
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
