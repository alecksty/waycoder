using System.Text;

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
            // bailian（百炼）与 qwen 共用 dashscope 同一地址 → 同地址 = 同供应商，已合并进 qwen，不再单独注册
            // （旧 providers.json 若含 bailian，加载后 DeduplicateProviders 自动归并到 qwen）
            ["opencode-go"]  = new("OpenCode Go",  "https://opencode.ai/zen/go/v1", "OPENCODE_API_KEY", "", "", null, 2),  // 订阅制；网关级温度限 2 位
            ["opencode-zen"] = new("OpenCode Zen", "https://opencode.ai/zen/v1", "OPENCODE_API_KEY", "", ""),              // 按量付费
            // opencode 旧数据兼容别名与 opencode-zen 同地址 → 同地址 = 同供应商，并入 opencode-zen，不再单独注册
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
            DeduplicateProviders(); // 地址唯一性铁律：同地址 = 同供应商，加载后自动归并重复地址
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

    /// <summary>
    /// 注册/更新服务商到 providers.json（导入的服务商地址确认可用后调用）。id 规范化（全小写、去特殊符号）。
    /// 地址唯一性：base_url 已被其它供应商占用 → 拒绝注册（同地址 = 同供应商，不允许重复），返回 false。
    /// 同一 id 重复注册 = 更新（新地址未被占用则放行）。
    /// </summary>
    public static bool RegisterProvider(string providerId, string displayName, string baseUrl)
    {
        providerId = NormalizeId(providerId);
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(baseUrl)) return false;
        var owner = FindProviderByBaseUrl(baseUrl);
        if (owner != null && !owner.Equals(providerId, StringComparison.OrdinalIgnoreCase))
            return false; // 地址已被其它供应商占用
        Providers[providerId] = new ProviderInfo(displayName, baseUrl);
        SaveProvidersJson();
        return true;
    }

    /// <summary>规范化 base_url 用于地址唯一性比较：去首尾空白 + 去尾部斜杠（大小写不敏感比较）。</summary>
    internal static string NormalizeBaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        return url.Trim().TrimEnd('/');
    }

    /// <summary>按 base_url 反查拥有它的供应商 id（地址规范化后比较，大小写不敏感）；无匹配返回 null。</summary>
    public static string? FindProviderByBaseUrl(string? baseUrl)
    {
        var norm = NormalizeBaseUrl(baseUrl);
        if (norm.Length == 0) return null;
        foreach (var (pid, p) in Providers)
            if (string.Equals(NormalizeBaseUrl(p.DefaultBaseUrl), norm, StringComparison.OrdinalIgnoreCase))
                return pid;
        return null;
    }

    /// <summary>
    /// 供应商去重修复（同地址 = 同供应商）：把共享同一 base_url 的重复供应商合并为一个——
    /// 其自定义模型归并到保留供应商，从注册表移除重复项并落盘。返回合并对数。
    /// 保留规则：内置供应商优先，其次 id 字典序靠前者胜出。
    /// </summary>
    public static int DeduplicateProviders()
    {
        lock (_lock)
        {
            var survivors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // droppedId → survivorId
            var addressOwner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (pid, p) in Providers
                .OrderBy(kv => BuiltinProviders.ContainsKey(kv.Key) ? 0 : 1)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList())
            {
                var norm = NormalizeBaseUrl(p.DefaultBaseUrl);
                if (norm.Length == 0) continue;
                if (addressOwner.TryGetValue(norm, out var owner) && owner != pid)
                    survivors[pid] = owner;
                else
                    addressOwner[norm] = pid;
            }
            if (survivors.Count == 0) return 0;
            // 归并模型：被合并供应商的模型改挂到保留供应商
            foreach (var (drop, keep) in survivors)
            {
                var dropped = LoadCustom()
                    .Where(kv => kv.Value.ProviderId.Equals(drop, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value)
                    .ToList();
                if (dropped.Count > 0)
                    AddCustomRange(dropped.Select(m => m with { ProviderId = keep }), local: false);
                RemoveCustomByProvider(drop);
            }
            // 移除重复供应商条目
            foreach (var drop in survivors.Keys) Providers.Remove(drop);
            SaveProvidersJson();
            return survivors.Count;
        }
    }

    /// <summary>归并引擎里一条模型的处理记录。</summary>
    public sealed record ReconcileChange(
        string SourceProviderId, string TargetProviderId, string ModelId,
        string Action, // moved | duplicate-skip | already-canonical | no-url | unresolved
        string? SourceFile);

    /// <summary>归并报告：计数 + 备份/删除/失败文件 + 逐条变更（dry-run 时 Backups/Deleted/Failed 为空）。</summary>
    public sealed record ReconcileReport(
        bool DryRun,
        int Moved, int DuplicateSkip, int AlreadyCanonical, int NoUrl, int Unresolved,
        IReadOnlyList<string> Backups, IReadOnlyList<string> DeletedFiles, IReadOnlyList<string> FailedFiles,
        IReadOnlyList<ReconcileChange>? Changes);

    /// <summary>
    /// 模型归并：把自定义模型从「别名 providerId」迁移到拥有同 base_url 的已注册供应商名下。
    /// 别名来源 = 导入时按来源声明名/URL host 推导的 id 与注册表脱节（如 api-qnaigc-com → qiniucloud）。
    /// 规则：空/无 URL → no-url（不动）；URL 无注册归属 → unresolved（不动）；已 canonical → 计数；目标 key 已占 →
    /// duplicate-skip（保留现有、仍移除别名源条目）；否则 moved（写目标桶 + 删源桶）。
    /// 备份先行、先写目标后删源、原子写 + 幂等可重跑。dryRun 只统计不落盘。
    /// </summary>
    public static ReconcileReport ReconcileModels(bool dryRun = false)
    {
        lock (_lock)
        {
            Invalidate();
            var existing = LoadCustom(); // 触发 legacy 迁移，拿合并快照（按归一化 key）做 canonical 判定
            var changes = new List<ReconcileChange>();
            var moves = new List<(string SourceFile, bool Local, string SourceKey, ModelInfo Model, string Target, bool AddToTarget)>();
            int moved = 0, dup = 0, canonical = 0, noUrl = 0, unresolved = 0;

            foreach (var file in EnumerateModelFiles())
            {
                var local = IsLocalModelFile(file);
                foreach (var (key, m) in ReadFile(file))
                {
                    if (string.IsNullOrWhiteSpace(m.DefaultBaseUrl))
                    {
                        noUrl++;
                        changes.Add(new ReconcileChange(m.ProviderId, "", m.Id, "no-url", file));
                        continue;
                    }
                    var target = FindProviderByBaseUrl(m.DefaultBaseUrl);
                    if (target == null)
                    {
                        unresolved++;
                        changes.Add(new ReconcileChange(m.ProviderId, "", m.Id, "unresolved", file));
                        continue;
                    }
                    if (target.Equals(m.ProviderId, StringComparison.OrdinalIgnoreCase))
                    {
                        canonical++;
                        changes.Add(new ReconcileChange(m.ProviderId, target, m.Id, "already-canonical", file));
                        continue;
                    }
                    // 字段归一化：存储 providerId 规范化后 == 目标（如 wafer.ai → wafer-ai，ModelKey 归一化后同 key）
                    // —— 这本质就是 canonical 条目，只是存储字段格式不对，必迁移改写字段，不存在「重复跳过」。
                    if (NormalizeId(m.ProviderId) == NormalizeId(target))
                    {
                        moved++;
                        changes.Add(new ReconcileChange(m.ProviderId, target, m.Id, "moved", file));
                        moves.Add((file, local, key, m, target, true));
                        continue;
                    }
                    // 真正别名（如 pass-wafer-ai → wafer-ai）：仅当已存在真实 canonical 条目才 duplicate-skip——
                    // 判定用 existing 里该 key 的存储 providerId（规范化后==目标），防误把「别名自身」当 canonical。
                    var newKey = ModelKey(target, m.Id);
                    var hasCanonical = existing.TryGetValue(newKey, out var em)
                        && NormalizeId(em.ProviderId) == NormalizeId(target);
                    if (hasCanonical)
                    {
                        dup++;
                        changes.Add(new ReconcileChange(m.ProviderId, target, m.Id, "duplicate-skip", file));
                        moves.Add((file, local, key, m, target, false)); // 仍移除源条目，让别名 providerId 消失
                        continue;
                    }
                    moved++;
                    changes.Add(new ReconcileChange(m.ProviderId, target, m.Id, "moved", file));
                    moves.Add((file, local, key, m, target, true));
                }
            }

            if (dryRun || moves.Count == 0)
                return new ReconcileReport(dryRun, moved, dup, canonical, noUrl, unresolved,
                    Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), changes);

            // 备份受影响文件（源 ∪ 目标且存在），首个写入前完成
            var backups = new List<string>();
            var affected = moves.Select(mv => mv.SourceFile)
                .Concat(moves.Select(mv => ProviderFile(mv.Target, mv.Local)))
                .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var f in affected)
                if (BackupFile(f) is { } bak) backups.Add(bak);

            // 分组计划
            var additions = new Dictionary<string, List<ModelInfo>>(StringComparer.OrdinalIgnoreCase);
            var removals = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var mv in moves)
            {
                var tf = ProviderFile(mv.Target, mv.Local);
                // 源==目标：原地改写存储 providerId（归一化 key 不变），只写不删，防自删后丢模型
                var inPlace = string.Equals(tf, mv.SourceFile, StringComparison.OrdinalIgnoreCase);
                if (mv.AddToTarget || inPlace)
                {
                    if (!additions.TryGetValue(tf, out var al)) additions[tf] = al = new List<ModelInfo>();
                    al.Add(mv.Model with { ProviderId = mv.Target });
                }
                if (!inPlace)
                {
                    if (!removals.TryGetValue(mv.SourceFile, out var rl)) removals[mv.SourceFile] = rl = new List<string>();
                    rl.Add(mv.SourceKey);
                }
            }

            var failed = new List<string>();
            var deleted = new List<string>();

            // Pass 1：先写目标桶（canonical 副本先于别名删除存在，防崩溃丢模型）
            foreach (var (target, list) in additions)
            {
                var models = ReadFile(target);
                foreach (var m in list) models[ModelKey(m.ProviderId, m.Id)] = m;
                if (!SaveCustom(models, target)) failed.Add(target);
            }
            // Pass 2：从源桶移除；空文件删除
            foreach (var (src, keys) in removals)
            {
                var models = ReadFile(src);
                foreach (var k in keys) models.Remove(k);
                if (models.Count == 0)
                {
                    TryDeleteFile(src);
                    if (File.Exists(src)) failed.Add(src); else deleted.Add(src);
                }
                else if (!SaveCustom(models, src)) failed.Add(src);
            }

            Invalidate();
            return new ReconcileReport(false, moved, dup, canonical, noUrl, unresolved, backups, deleted, failed, changes);
        }
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

    /// <summary>清空服务商注册表（providers.json 重写为空）。供「清空模型列表」（ClearAll）调用——
    /// 模型与供应商一起清，方便重新导入时重建数据（不留旧供应商注册干扰新导入的去重/名称判断）。</summary>
    public static void ClearProviders()
    {
        lock (_lock)
        {
            Providers.Clear();
            SaveProvidersJson();
        }
    }

    /// <summary>providerId → 注册显示名（providers.json name）；未注册回退 id 本身。
    /// 供模型栏 `(provider)model`、分组头展示 —— 与 ModelPicker 厂商列保持一致的显示名。
    /// 例：aihubmix → AIHubMix；opencode-go → OpenCode Go；deepseek → DeepSeek。
    /// 大小写不敏感：先精确匹配，再忽略大小写遍历兜底（providers.json 手写混合大小写 key / 调用方传混合大小写 id 也能命中）。</summary>
    public static string ProviderDisplayName(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) return "";
        var pid = providerId.Trim();
        if (Providers.TryGetValue(pid, out var p) && !string.IsNullOrWhiteSpace(p.DisplayName))
            return p.DisplayName;
        foreach (var (k, v) in Providers)
            if (string.Equals(k, pid, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(v.DisplayName))
                return v.DisplayName;
        return pid;
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

    /// <summary>改供应商 Base URL（保留其他字段，providers.json 落盘）。
    /// 新地址已被其它供应商占用 → 拒绝修改（同地址 = 同供应商），返回 false。</summary>
    public static bool UpdateProviderUrl(string providerId, string baseUrl)
    {
        if (!Providers.TryGetValue(providerId, out var p)) return false;
        var owner = FindProviderByBaseUrl(baseUrl);
        if (owner != null && !owner.Equals(providerId, StringComparison.OrdinalIgnoreCase))
            return false; // 新地址已被其它供应商占用
        Providers[providerId] = p with { DefaultBaseUrl = baseUrl ?? "" };
        SaveProvidersJson();
        return true;
    }
}
