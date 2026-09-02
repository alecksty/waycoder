using System.Text;

namespace WayCoder;

/// <summary>
/// Model Catalog — built-in model registry + external config import (OpenCode / Crush / Continue / Cline).
/// Browse, search, and model metadata. Compatible with most OpenAI-compatible APIs.
/// </summary>
public static partial class ModelCatalog
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
        double OutputPriceOffpeak = 0,  // 闲时输出价（$ / MTok）
        string? ReasoningEffortAllowed = null,  // 模型级 reasoning_effort 允许集（逗号分隔，如 "low,high,max"）；null=未声明→厂商/全局
        int? TemperaturePrecision = null,       // 模型级 temperature 小数位精度；null=未声明→厂商/全局默认 2
        bool? SupportsThinking = null,          // 是否支持思考（thinking/reasoning）；null=未声明→厂商/家族推断
        bool? SupportsTools = null,             // 是否支持工具调用；null=未声明→厂商/默认 true
        bool? SupportsVision = null             // 是否支持视觉（图片输入）；null=未声明→厂商/按 id 推断
    );



    // ════════════════════════════════════════════════════════════
    // 自定义模型库（按供应商分类分文件：全局 ~/.waycoder/provider/{供应商}.json + 本地 .waycoder/provider/）。
    // 兼容旧单文件 models.json：仍作为读源（迁移后写入 provider/ 分类文件）。
    // 内置目录为兜底，外置库按 Id 覆盖/追加，本地覆盖全局。
    // ════════════════════════════════════════════════════════════

    private static readonly object _lock = new();
    private static Dictionary<string, ModelInfo>? _custom;
    private static ModelInfo[]? _all;

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
    /// 模型唯一键（connectId = 规范化 providerId + 规范化 modelId 拼接）：两边都去空格、转小写、空格转横线，
    /// 保证同供应商同模型不因导入来源大小写/空格差异重复。同一供应商下模型 id 唯一；
    /// 不同服务商可同名（opencode-go/deepseek-v4-pro 与 opencode-zen/deepseek-v4-pro 是两个不同服务商）。
    /// 服务商唯一性由 providers.json 按地址去重负责（同地址只一个 provider）。
    /// </summary>
    internal static string ModelKey(string providerId, string id) =>
        string.IsNullOrWhiteSpace(providerId) ? NormalizeId(id) + "|" : NormalizeId(providerId) + "|" + NormalizeId(id);

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
                    // 同 Id 且同 baseUrl（规范化去尾部斜杠）覆盖内置；不同 baseUrl 视为不同服务商追加（都保留显示）
                    var idx = list.FindIndex(x => x.Id == m.Id
                        && string.Equals(NormalizeBaseUrl(x.DefaultBaseUrl), NormalizeBaseUrl(m.DefaultBaseUrl), StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) list[idx] = m;
                    else list.Add(m);
                }
                _all = list.ToArray();
                return _all;
            }
        }
    }

    /// <summary>持久化边界归一化：若模型 baseUrl 已属于某注册供应商且当前 ProviderId 不一致，
    /// 重写为注册归属者（防导入别名复发——同地址 = 同供应商，模型归属应跟随地址）。无匹配/一致则原样。</summary>
    private static ModelInfo NormalizeToRegisteredOwner(ModelInfo m)
    {
        var owner = FindProviderByBaseUrl(m.DefaultBaseUrl);
        if (owner != null && !owner.Equals(m.ProviderId, StringComparison.OrdinalIgnoreCase))
            return m with { ProviderId = owner };
        return m;
    }

    /// <summary>新增/更新自定义模型，返回写入的文件路径。local=true 写本地，否则写全局。
    /// 文件读-改-写持统一锁（_lock），防 Web 并发导入/删除 read-modify-write 竞争丢模型。</summary>
    public static string AddCustom(ModelInfo info, bool local = false)
    {
        lock (_lock)
        {
            info = NormalizeToRegisteredOwner(info);
            var path = ProviderFile(info.ProviderId, local);
            var models = ReadFile(path);
            var key = ModelKey(info.ProviderId, info.Id);
            // 单条护栏：净新增超上限拒绝（防循环单条调用绕过 AddCustomRange 上限），不写入、返回错误描述
            if (Global.MaxImportedModels > 0)
            {
                var existing = LoadCustom();
                if (!existing.ContainsKey(key) && existing.Count >= Global.MaxImportedModels)
                    return $"❌ 模型库已达上限 {Global.MaxImportedModels:N0}，请先删除部分模型再添加";
            }
            models[key] = models.TryGetValue(key, out var existing2) ? MergeModel(existing2, info) : info;
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
            // 持久化边界归一化：别名模型重写为注册归属者（防复发），在护栏与分组写盘前执行
            list = list.Select(NormalizeToRegisteredOwner).ToList();
            // 导入数量护栏：按「净新增 key 数」判超限——更新已有 key 不计入（避免已有 9990 条时
            // 导入 20 条纯更新被误拒），一次 models.dev 导入 7000+ 仍被挡。
            if (Global.MaxImportedModels > 0)
            {
                var existing = LoadCustom();
                var newKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var m in list) newKeys.Add(ModelKey(m.ProviderId, m.Id));
                int newCount = newKeys.Count(k => !existing.ContainsKey(k));
                if (existing.Count + newCount > Global.MaxImportedModels)
                {
                    ErrorLog.Warning("ModelCatalog",
                        $"导入被拒绝：模型总库将超过上限 {Global.MaxImportedModels:N0}（现有 {existing.Count:N0} + 净新增 {newCount:N0}）。请清理旧模型或分批导入。");
                    return 0;
                }
            }
            foreach (var g in list.GroupBy(m => ProviderFile(m.ProviderId, local)))
            {
                var models = ReadFile(g.Key);
                foreach (var m in g)
                {
                    var key = ModelKey(m.ProviderId, m.Id);
                    models[key] = models.TryGetValue(key, out var existing) ? MergeModel(existing, m) : m;
                }
                SaveCustom(models, g.Key);
            }
            Invalidate();
            return list.Count;
        }
    }

    /// <summary>合并去重：同供应商同模型，有价格的覆盖没价格的（多源导入后价格信息最全者胜）。</summary>
    internal static ModelInfo MergeModel(ModelInfo existing, ModelInfo incoming)
    {
        var existingPriced = existing.InputPrice > 0 || existing.OutputPrice > 0;
        var incomingPriced = incoming.InputPrice > 0 || incoming.OutputPrice > 0;
        if (existingPriced && !incomingPriced) return existing;   // 已有价格、新导入无价格 → 保留价格版
        return incoming;                                           // 否则用新导入（有价格覆盖无价格 / 新数据）
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
                // 移除该 id 的全部变体（providerId|id，跨服务商同名都删）；兼容旧格式原始 id 与新格式规范化 id
                var norm = NormalizeId(id);
                var toRemove = models.Keys.Where(k =>
                    k.EndsWith("|" + id, StringComparison.OrdinalIgnoreCase)
                    || k.EndsWith("|" + norm, StringComparison.OrdinalIgnoreCase)).ToList();
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
            ClearProviders(); // 模型列表清空时一并清空 providers.json，方便重新导入重建数据
        }
        return n;
    }

    /// <summary>恢复内置模型目录（清除清空标记）。清空（ClearAll）后可经「本地导入→内置模型」恢复。</summary>
    public static void RestoreBuiltIn()
    {
        lock (_lock)
        {
            try { if (File.Exists(BuiltInClearedPath)) File.Delete(BuiltInClearedPath); } catch { }
            // ClearAll 清空过服务商注册表（内存 + providers.json）：恢复内置时重建，
            // 否则当前进程内 Providers 字典为空，供应商解析/名称/去重降级（需重启才恢复）
            if (Providers.Count == 0)
            {
                Providers.Clear();
                foreach (var (k, v) in BuiltinProviders) Providers[k] = v;
                LoadOrCreateProvidersJson();
            }
            _all = null;
        }
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

    // ════════════════════════════════════════════════════════════
    // 序列化（AOT 安全手写，不用反射）
    // ════════════════════════════════════════════════════════════

    // JNode 便捷取值（AOT 安全，替代 System.Text.Json.Nodes 的 GetValue<T> / AsArray / AsObject）
    static JNode? Arr(JNode? n) => n != null && n.Kind == JKind.Array ? n : null;
    static JNode? Obj(JNode? n) => n != null && n.Kind == JKind.Object ? n : null;
    static string? StrOpt(JNode? n) => n != null && n.Kind == JKind.String ? n.AsString() : null;
    static int? IntOpt(JNode? n) => n != null && n.Kind == JKind.Number ? (int)Math.Round(n.AsNumber()) : null;
    static double? DblOpt(JNode? n) => n != null && n.Kind == JKind.Number ? n.AsNumber() : null;
    static bool? BoolOpt(JNode? n) => n != null && n.Kind == JKind.Bool ? n.AsBool() : null;

    private static JNode ToJson(ModelInfo m)
    {
        var n = JNode.Object()
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
        // 条件写非默认值：未声明约束的旧文件往返字节不变（不刷噪音 key）
        if (!string.IsNullOrWhiteSpace(m.ReasoningEffortAllowed))
            n.Set("reasoningEffortAllowed", m.ReasoningEffortAllowed);
        if (m.TemperaturePrecision is { } tp)
            n.Set("temperaturePrecision", tp);
        // 能力特性（条件写，null=未声明不刷）
        if (m.SupportsThinking is { } st)
            n.Set("supportsThinking", st);
        if (m.SupportsTools is { } st2)
            n.Set("supportsTools", st2);
        if (m.SupportsVision is { } sv)
            n.Set("supportsVision", sv);
        return n;
    }

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
            DblOpt(node["outputPriceOffpeak"]) ?? 0,
            StrOpt(node["reasoningEffortAllowed"]),
            IntOpt(node["temperaturePrecision"]),
            BoolOpt(node["supportsThinking"]),
            BoolOpt(node["supportsTools"]),
            BoolOpt(node["supportsVision"])
        );
    }

    // Query helpers
    /// <summary>
    /// 按 id 查模型（无 baseUrl 上下文）：内置官方网关优先（默认访问入口），无内置则第一个自定义变体。
    /// 同 id 不同 baseUrl 视为不同服务商——需精确指定服务商时用 <see cref="Find(string, string?)"/>。
    /// </summary>
}
