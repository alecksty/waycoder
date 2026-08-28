using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCatalog
{
    /// <summary>
    /// 厂商/网关级注册信息（providers.json 条目）。
    /// ApiKeyEnvVar=官方 API key 环境变量名（无官方则为空）；ModelsEndpoint=官方模型列表接口（相对 baseUrl，无则为空）；
    /// CommonModels=官方常用模型 id（逗号分隔）；ReasoningEffortAllowed/TemperaturePrecision=厂商级调用参数默认（模型未声明时继承）。
    /// </summary>
    public sealed record ProviderInfo(
        string DisplayName,
        string DefaultBaseUrl,
        string? ApiKeyEnvVar = null,            // 官方 API key 环境变量名（如 DEEPSEEK_API_KEY）
        string? ModelsEndpoint = null,          // 官方模型列表接口（如 /v1/models）；没有为空
        string? CommonModels = null,            // 官方常用模型 id（逗号分隔）
        string? ReasoningEffortAllowed = null,  // 厂商级 reasoning_effort 允许集；null=不约束
        int? TemperaturePrecision = null,       // 厂商级 temperature 小数位；null=用全局默认 2
        string? ApiFormat = null,               // API 请求格式：null/空/openai=OpenAI 兼容；anthropic=原生 /v1/messages；gemini=原生 streamGenerateContent
        bool? SupportsThinking = null,          // 厂商级是否支持思考；null=模型/推断决定
        bool? SupportsTools = null,             // 厂商级是否支持工具；null=模型/默认 true
        bool? SupportsVision = null,            // 厂商级是否支持视觉；null=模型/按 id 推断
        double? Temperature = null);            // 厂商级 temperature 覆盖；null=用全局 Config.Temperature

    /// <summary>Provider registry with default base URLs</summary>
    public static readonly Dictionary<string, ProviderInfo> Providers;

    /// <summary>内置服务商注册表快照（providers.json 用户覆盖前），供测试/展示区分「内置默认」与「用户覆盖」。</summary>
    public static readonly Dictionary<string, ProviderInfo> BuiltinProviders;

    static ModelCatalog()
    {
        Providers = new Dictionary<string, ProviderInfo>
        {
            // 官方环境变量 / 常用模型 / 模型列表接口；不确定的留空（没有则为空）
            ["openai"]     = new("OpenAI",       "https://api.openai.com", "OPENAI_API_KEY", "/models", "gpt-5.4,gpt-5.4-mini,gpt-4o,gpt-4o-mini"),
            ["anthropic"]  = new("Anthropic",    "https://api.anthropic.com", "ANTHROPIC_API_KEY", "", "claude-opus-5,claude-sonnet-5,claude-haiku-4-5", ApiFormat: "anthropic"),
            ["deepseek"]   = new("DeepSeek",     "https://api.deepseek.com", "DEEPSEEK_API_KEY", "/models", "deepseek-v4-pro,deepseek-v4-flash,deepseek-chat,deepseek-reasoner", "low,medium,high"),
            ["google"]     = new("Google",       "https://generativelanguage.googleapis.com/v1beta/openai", "GEMINI_API_KEY", "/models", "gemini-2.5-pro,gemini-2.5-flash,gemini-2.0-flash", ApiFormat: "gemini"),
            ["qwen"]       = new("Alibaba Qwen", "https://dashscope.aliyuncs.com/compatible-mode/v1", "DASHSCOPE_API_KEY", "/models", "qwen3-max,qwen3-plus,qwen-turbo"),
            ["moonshot"]   = new("Moonshot",     "https://api.moonshot.cn", "MOONSHOT_API_KEY", "/models", "kimi-k2.5"),
            ["zhipu"]      = new("Zhipu GLM",    "https://open.bigmodel.cn/api/paas/v4", "ZHIPU_API_KEY", "/models", "glm-4-plus,glm-4-flash", "low,medium,high"),
            ["bytedance"]  = new("ByteDance",    "https://ark.cn-beijing.volces.com/api/v3", "ARK_API_KEY", "", ""),
            ["01ai"]       = new("01.AI",        "https://api.lingyiwanwu.com", "LINGYIWANWU_API_KEY", "/models", ""),
            ["xai"]        = new("xAI",          "https://api.x.ai", "XAI_API_KEY", "/models", "grok-4.5"),
            ["mistral"]    = new("Mistral",      "https://api.mistral.ai", "MISTRAL_API_KEY", "/models", ""),
            ["siliconflow"]= new("SiliconFlow",  "https://api.siliconflow.cn", "SILICONFLOW_API_KEY", "/models", ""),
            ["openrouter"] = new("OpenRouter",   "https://openrouter.ai/api/v1", "OPENROUTER_API_KEY", "/models", "openrouter/free,deepseek/deepseek-chat-v3-0324,google/gemini-2.5-flash"),
            ["groq"]       = new("Groq",         "https://api.groq.com/openai/v1", "GROQ_API_KEY", "/models", ""),
            ["together"]   = new("Together AI",  "https://api.together.xyz/v1", "TOGETHER_API_KEY", "/models", ""),
            ["gitee"]      = new("Gitee AI",     "https://ai.gitee.com/v1", "GITEE_AI_API_KEY", "", ""),
            ["bailian"]    = new("Alibaba Bailian", "https://dashscope.aliyuncs.com/compatible-mode/v1", "DASHSCOPE_API_KEY", "/models", "qwen3-max,qwen3-plus"),
            ["opencode-go"]  = new("OpenCode Go",  "https://opencode.ai/zen/go/v1", "OPENCODE_API_KEY", "", "", null, 2),  // 订阅制；网关级温度限 2 位
            ["opencode-zen"] = new("OpenCode Zen", "https://opencode.ai/zen/v1", "OPENCODE_API_KEY", "", ""),              // 按量付费
            ["opencode"]     = new("OpenCode",     "https://opencode.ai/zen/v1", "OPENCODE_API_KEY", "", ""),              // 旧数据兼容别名
            ["minimax"]    = new("MiniMax",      "https://api.minimaxi.com/v1", "MINIMAX_API_KEY", "/models", ""),
            ["aihubmix"]   = new("AIHubMix",     "https://api.inferera.com/v1", "AIHUBMIX_API_KEY", "/models", "deepseek-v4-pro,deepseek-v4-flash,coding-minimax-m3-free"),
            ["local"]      = new("Local",        "", "", "", ""),
            ["custom"]     = new("Custom",       "", "", "", ""),
        };
        // 内置快照先于 providers.json 覆盖保存，区分「内置默认」与「用户覆盖」
        BuiltinProviders = new Dictionary<string, ProviderInfo>(Providers);
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
                // 字段缺失时继承内置默认（旧 providers.json 只有 name/base_url，不能把内置的官方元数据覆盖为 null）
                var builtin = Providers.TryGetValue(id, out var b) ? b : null;
                var apiKeyEnv = p?["apiKeyEnvVar"]?.AsString() ?? p?["api_key_env"]?.AsString() ?? builtin?.ApiKeyEnvVar;
                var modelsEndpoint = p?["modelsEndpoint"]?.AsString() ?? p?["models_endpoint"]?.AsString() ?? builtin?.ModelsEndpoint;
                var commonModels = p?["commonModels"]?.AsString() ?? p?["common_models"]?.AsString() ?? builtin?.CommonModels;
                var reasoningAllowed = p?["reasoningEffortAllowed"]?.AsString()
                    ?? p?["reasoning_effort"]?.AsString()
                    ?? p?["reasoning_effort_allowed"]?.AsString()
                    ?? builtin?.ReasoningEffortAllowed;
                var tempPrec = IntOpt(p?["temperaturePrecision"])
                    ?? IntOpt(p?["temperature_precision"])
                    ?? builtin?.TemperaturePrecision;
                var apiFormat = p?["apiFormat"]?.AsString() ?? p?["api_format"]?.AsString() ?? builtin?.ApiFormat;
                var supThink = BoolOpt(p?["supportsThinking"]) ?? BoolOpt(p?["supports_thinking"]) ?? builtin?.SupportsThinking;
                var supTools = BoolOpt(p?["supportsTools"]) ?? BoolOpt(p?["supports_tools"]) ?? builtin?.SupportsTools;
                var supVision = BoolOpt(p?["supportsVision"]) ?? BoolOpt(p?["supports_vision"]) ?? builtin?.SupportsVision;
                Providers[id] = new ProviderInfo(name, url, apiKeyEnv, modelsEndpoint, commonModels, reasoningAllowed, tempPrec, apiFormat, supThink, supTools, supVision);
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
                sb.Append($"    \"{kv.Key}\": {{ \"name\": \"{kv.Value.DisplayName}\", \"base_url\": \"{kv.Value.DefaultBaseUrl}\"");
                if (!string.IsNullOrWhiteSpace(kv.Value.ApiKeyEnvVar))
                    sb.Append($", \"apiKeyEnvVar\": \"{kv.Value.ApiKeyEnvVar}\"");
                if (!string.IsNullOrWhiteSpace(kv.Value.ModelsEndpoint))
                    sb.Append($", \"modelsEndpoint\": \"{kv.Value.ModelsEndpoint}\"");
                if (!string.IsNullOrWhiteSpace(kv.Value.CommonModels))
                    sb.Append($", \"commonModels\": \"{kv.Value.CommonModels}\"");
                if (!string.IsNullOrWhiteSpace(kv.Value.ReasoningEffortAllowed))
                    sb.Append($", \"reasoningEffortAllowed\": \"{kv.Value.ReasoningEffortAllowed}\"");
                if (kv.Value.TemperaturePrecision is { } tp)
                    sb.Append($", \"temperaturePrecision\": {tp}");
                if (!string.IsNullOrWhiteSpace(kv.Value.ApiFormat) && !kv.Value.ApiFormat.Equals("openai", StringComparison.OrdinalIgnoreCase))
                    sb.Append($", \"apiFormat\": \"{kv.Value.ApiFormat}\"");
                if (kv.Value.SupportsThinking is { } st)
                    sb.Append($", \"supportsThinking\": {(st ? "true" : "false")}");
                if (kv.Value.SupportsTools is { } st2)
                    sb.Append($", \"supportsTools\": {(st2 ? "true" : "false")}");
                if (kv.Value.SupportsVision is { } sv)
                    sb.Append($", \"supportsVision\": {(sv ? "true" : "false")}");
                sb.Append($" }}{comma}\n");
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
        Providers[providerId] = new ProviderInfo(displayName, baseUrl);
        SaveProvidersJson();
    }

    /// <summary>移除服务商（providers.json），同时清除其 API Key。返回是否移除。</summary>
    public static bool RemoveProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) return false;
        if (!Providers.Remove(providerId)) return false;
        SaveProvidersJson();
        // 关键：不连带删除 API Key——key 永不自动删除（clean 等自动清理不得删 key），
        // 用户要删 key 请显式 --model key rm <provider>
        return true;
    }

    /// <summary>改供应商显示名（保留其他字段，providers.json 落盘）。</summary>
    public static void RenameProvider(string providerId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return;
        if (Providers.TryGetValue(providerId, out var p))
        {
            Providers[providerId] = p with { DisplayName = displayName };
            SaveProvidersJson();
        }
    }

    /// <summary>改供应商 Base URL（保留其他字段，providers.json 落盘）。</summary>
    public static void UpdateProviderUrl(string providerId, string baseUrl)
    {
        if (Providers.TryGetValue(providerId, out var p))
        {
            Providers[providerId] = p with { DefaultBaseUrl = baseUrl ?? "" };
            SaveProvidersJson();
        }
    }
}
