using System.Text;
using WayCoder.Infra;
namespace WayCoder;

public static partial class ModelCatalog
{
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
            // 按服务地址归类：opencode 网关分 Go(zen/go/v1) / Zen(zen/v1) 两个服务商，地址决定归属。
            // pname 跟随 providerId（注册表显示名），避免「从 DeepSeek 源在线导入」得到 providerId=deepseek 却标 OpenCode Go 的错配
            var pid = ResolveProviderId(baseUrl, "opencode-go");
            var pname = pid switch
            {
                "opencode-go" => "OpenCode Go",
                "opencode-zen" => "OpenCode Zen",
                _ => ModelCatalog.ProviderDisplayName(pid),
            };
            // OpenRouter 等模型 id 带厂商前缀（openai/gpt-5.4-mini）：显示用去前缀短名，调用仍用完整 id（路由需要）
            var displayName = id.Contains('/') ? id[(id.IndexOf('/') + 1)..] : id;
            result.Add(new ModelInfo(id, displayName, pname, pid, "*", "Imported",
                0, 0, 0, baseUrl, $"从 OpenCode 在线导入（{pname}）", 0));
        }
        return result;
    }

    /// <summary>
    /// 模型显示短名：OpenRouter 等在线导入的 id 带厂商路由前缀（openai/gpt-5.4-mini），
    /// 列表显示时去前缀（gpt-5.4-mini）更清爽；调用仍用完整 id（路由需要）。URL 不处理。
    /// </summary>
    public static string ShortDisplayName(string idOrName)
    {
        if (string.IsNullOrEmpty(idOrName) || idOrName.Contains("://")) return idOrName;
        var i = idOrName.IndexOf('/');
        if (i <= 0 || i >= idOrName.Length - 1) return idOrName;
        return idOrName[(i + 1)..];
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
        return P("opencode-zen", "OpenCode Zen");
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

    /// <summary>
    /// 供应商唯一 id 由 base_url 决定（「同地址 = 同供应商」去重）：识别到内置网关 → 规范 id
    /// （deepseek/openai…）；否则按 host 派生稳定 id（同 host 必得同 id，跨来源也能合并）；
    /// 地址为空/不可解析才回退来源 pid。这是「请求打到哪，就归哪个服务商」的唯一入口。
    /// </summary>
    /// <summary>规范化供应商 ID：去掉 `api-` 前缀和 `-ai` / `-com` 后缀（导入数据常见格式
    /// api-openai-ai.com → openai）。只影响导入解析（影响名称/图标/去重的判断），不改变已注册的规范 ID。
    /// api- 前缀来自 host 的 api.deepseek.com 子域、-ai 后缀来自 api-siliconflow-ai 这类服务商 slug、
    /// -com 后缀来自 .com 域（对名称判断无用）。</summary>
    public static string NormalizeProviderId(string? pid)
    {
        var s = NormalizeId(pid ?? "");
        if (s.StartsWith("api-", StringComparison.Ordinal))
            s = s["api-".Length..];
        if (s.EndsWith("-ai", StringComparison.Ordinal))
            s = s[..^"-ai".Length];
        if (s.EndsWith("-com", StringComparison.Ordinal))
            s = s[..^"-com".Length];
        return string.IsNullOrEmpty(s) ? "import" : s;
    }

    public static string ResolveProviderId(string? baseUrl, string? fallbackPid)
    {
        var known = InferProviderFromBaseUrl(baseUrl);
        if (known != null) return known;
        var host = ExtractHost(baseUrl);
        if (host.Length > 0) return NormalizeProviderId(host);
        return NormalizeProviderId(fallbackPid ?? "import");
    }

    /// <summary>从 base_url 提取规范化 host（小写、去 www.、去端口）：api.deepseek.com → api.deepseek.com。</summary>
    private static string ExtractHost(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return "";
        try
        {
            var host = Uri.TryCreate(baseUrl, UriKind.Absolute, out var u) && !string.IsNullOrWhiteSpace(u.Host)
                ? u.Host
                : baseUrl;
            host = host.ToLowerInvariant();
            if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
            // 去端口（localhost:11434 → localhost）；非标准端口用于区分服务商时，保留路径段交给 InferProviderFromBaseUrl 已先判断
            var colon = host.LastIndexOf(':');
            if (colon > 0 && host.IndexOf(':') == colon) host = host[..colon];
            return host;
        }
        catch { return ""; }
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
                var effPid = ResolveProviderId(baseUrl, pid);
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
                var effPid = ResolveProviderId(baseUrl, pid);
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
    /// models.dev api.json 导入（https://models.dev/api.json）：
    /// { "&lt;providerId&gt;": { name, api, models: { "&lt;id&gt;": { id, name,
    ///   cost{input,output,cache_read}, limit{context,output}, reasoning, tool_call } } } }。
    /// 覆盖 200+ 服务商、7000+ 模型，含每百万 token 价格与上下文。
    /// 模型 id 去首段服务商前缀（deepseek/deepseek-v4-flash → deepseek-v4-flash，API 调用用实际名；
    /// 聚合网关 openrouter/... 保留剩余段作路由）。
    /// </summary>
    public static List<ModelInfo> ImportModelsDev(string json)
    {
        var result = new List<ModelInfo>();
        JNode? root;
        try { root = Json.Parse(NormalizeJson5(json)); }
        catch { return result; }
        if (root == null || root.Kind != JKind.Object) return result;

        foreach (var (pid, prov) in root.Entries)
        {
            if (prov == null || prov.Kind != JKind.Object) continue;
            var provName = prov.GetString("name") ?? pid;
            var baseUrl = prov.GetString("api");
            // models.dev 部分官方供应商无 api 字段（api=None，如 openai/anthropic）：回退内置/已注册供应商的官方端点，
            // 否则这些官方模型以空 baseUrl 导入（不可用、且 RegisterImportProviders 不注册其供应商）
            if (string.IsNullOrWhiteSpace(baseUrl)
                && Providers.TryGetValue(pid, out var regProv) && !string.IsNullOrWhiteSpace(regProv.DefaultBaseUrl))
                baseUrl = regProv.DefaultBaseUrl;
            var models = prov.Get("models");
            if (models == null || models.Kind != JKind.Object) continue;

            foreach (var (mid, node) in models.Entries)
            {
                if (node == null || node.Kind != JKind.Object) continue;
                var fullId = node.GetString("id") ?? mid;
                var id = fullId.Contains('/') ? fullId[(fullId.IndexOf('/') + 1)..] : fullId;
                var display = node.GetString("name") ?? ShortDisplayName(id);
                var cost = node.Get("cost");
                double inP = cost?.GetNumber("input") ?? 0;
                double outP = cost?.GetNumber("output") ?? 0;
                var limit = node.Get("limit");
                int ctx = (int)(limit?.GetNumber("context") ?? 0);
                int maxOut = (int)(limit?.GetNumber("output") ?? 0);
                bool? thinking = node.Get("reasoning")?.Kind == JKind.Bool ? node.Get("reasoning")!.AsBool() : null;
                bool? tools = node.Get("tool_call")?.Kind == JKind.Bool ? node.Get("tool_call")!.AsBool() : null;

                // 供应商唯一 id 由 base_url 决定：同地址同供应商（deepseek 网关下的转售商也归 deepseek）
                var effPid = ResolveProviderId(baseUrl, pid);
                var provDisplay = Providers.TryGetValue(effPid, out var reg) && !string.IsNullOrWhiteSpace(reg.DisplayName)
                    ? reg.DisplayName : provName;
                result.Add(new ModelInfo(id, display, provDisplay, effPid, "*", "Imported",
                    ctx, inP, outP, baseUrl, $"从 models.dev 导入（{provDisplay}）", maxOut,
                    SupportsThinking: thinking, SupportsTools: tools));
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
                    var effPid = ResolveProviderId(baseUrl, pid);
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
                            ProviderId = ResolveProviderId(baseUrl, pid),
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
                                ProviderId = ResolveProviderId(baseUrl, pid),
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
            var effPid = ResolveProviderId(baseUrl, "claude");
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
                var effPid = ResolveProviderId(p.BaseUrl, pid);
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
                var effPid = ResolveProviderId(baseUrl, pid);
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
        var noComments = Json.StripComments(text);
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
