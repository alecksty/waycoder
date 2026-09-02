using WayCoder.Infra;

namespace WayCoder;

/// <summary>
/// Agent 槽位模型配置 —— 10 个 F1-F10 槽位各自独立选择大小模型。
/// 支持"统一模式"一键设置所有槽位。
/// 保存到 {cwd}/.waycoder/agent_slots.json。
/// </summary>
public static class AgentSlotConfig
{
    public const int SlotCount = 10;

    /// <summary>单个槽位的模型配置</summary>
    public class SlotConfig
    {
        /// <summary>大模型 connect 名（「切换模型=切换 connect」的引用；空=由下方平铺字段解析）。</summary>
        public string? BigConnect { get; set; }

        /// <summary>小模型 connect 名（同上）。</summary>
        public string? SmallConnect { get; set; }

        /// <summary>大模型 ID（复杂任务），默认继承全局 Config.Model</summary>
        public string? LargeModel { get; set; }

        /// <summary>小模型 ID（简单任务），默认继承全局 Config.SmallModel</summary>
        public string? SmallModel { get; set; }

        /// <summary>API Base URL（null=继承全局或模型默认）</summary>
        public string? BaseUrl { get; set; }

        /// <summary>API Key 提供商标识（如 "deepseek"），从 ApiKeyStore 查找对应 Key</summary>
        public string? ApiKeyProviderId { get; set; }

        /// <summary>直接设置的 API Key（优先级高于 ApiKeyProviderId）</summary>
        public string? ApiKey { get; set; }

        /// <summary>是否使用全局配置（不独立设置）</summary>
        public bool UseGlobal { get; set; } = true;
    }

    private static SlotConfig[]? _slots;

    /// <summary>所有槽位是否使用统一配置</summary>
    public static bool UniformMode { get; set; }

    /// <summary>统一模式下的配置模板</summary>
    public static SlotConfig UniformTemplate { get; set; } = new();

    /// <summary>获取指定槽位配置</summary>
    public static SlotConfig Get(int slotIndex)
    {
        var slots = Load();
        if (slotIndex < 0 || slotIndex >= SlotCount) return new();
        return UniformMode ? Clone(UniformTemplate) : slots[slotIndex];
    }

    /// <summary>设置指定槽位配置并保存</summary>
    public static void Set(int slotIndex, SlotConfig config)
    {
        var slots = Load();
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        slots[slotIndex] = config;
        Save(slots);
    }

    /// <summary>设为统一模式：所有槽位使用相同配置</summary>
    public static void SetUniform(SlotConfig template)
    {
        UniformTemplate = template;
        UniformMode = true;
        // 同时更新所有槽位
        var slots = Load();
        for (int i = 0; i < SlotCount; i++)
            slots[i] = Clone(template);
        Save(slots);
    }

    /// <summary>取消统一模式：每个槽位独立</summary>
    public static void ClearUniform()
    {
        UniformMode = false;
    }

    /// <summary>重置所有槽位到默认（使用全局配置）</summary>
    public static void ResetAll()
    {
        UniformMode = false;
        var slots = new SlotConfig[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new();
        Save(slots);
    }

    /// <summary>
    /// 根据槽位配置解析出实际的 API Key。
    /// 优先级：SlotConfig.ApiKey > ApiKeyStore[ApiKeyProviderId] > 全局 Config.ApiKey
    /// </summary>
    public static string ResolveApiKey(SlotConfig slot)
    {
        if (!string.IsNullOrWhiteSpace(slot.ApiKey))
            return slot.ApiKey;
        if (!string.IsNullOrWhiteSpace(slot.ApiKeyProviderId))
        {
            var stored = ApiKeyStore.Get(slot.ApiKeyProviderId);
            if (!string.IsNullOrWhiteSpace(stored))
                return stored;
        }
        return Config.Instance.ApiKey;
    }

    /// <summary>
    /// 根据槽位配置解析出实际的 Base URL。
    /// 优先级：SlotConfig.BaseUrl > 槽位 connect 的 provider 注册表地址 > 与全局网关匹配的模型条目注册表地址
    ///          > 模型目录默认地址 > 全局 Config.BaseUrl。
    /// 同 id 多服务商（deepseek-v4-flash 分属 DeepSeek 官方 / AIHubMix 网关）时，网关地址决定归属——
    /// Find(id) 内置官方优先会误返官方地址（用户经 ModelPicker 选了 AIHubMix，网关却解析成 deepseek.com），
    /// 所以全局场景先按配置网关精确匹配目录条目；返回始终取「实时 providers.json 注册表地址」而非静态模型快照，
    /// 否则用户覆盖 base_url（如国内镜像）会被快照里的官方地址盖掉。
    /// </summary>
    public static string? ResolveBaseUrl(SlotConfig slot, string? modelId)
    {
        if (!string.IsNullOrWhiteSpace(slot.BaseUrl))
            return slot.BaseUrl;
        // 槽位显式 connect：请求走 connect 指定的网关（实时注册表地址，含 providers.json 覆盖）。
        // 按 modelId 匹配 BigConnect/SmallConnect（防同名模型跨服务商用错 connect），匹配不到回退 BigConnect。
        if (!slot.UseGlobal)
        {
            string? connName = null;
            foreach (var name in new[] { slot.BigConnect, slot.SmallConnect })
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var c = ConnectionConfig.FindConnect(name);
                if (c == null) continue;
                if (string.IsNullOrEmpty(modelId) || string.Equals(c.ModelId, modelId, StringComparison.OrdinalIgnoreCase))
                { connName = name; break; }
            }
            connName ??= slot.BigConnect;
            if (!string.IsNullOrWhiteSpace(connName))
            {
                var c = ConnectionConfig.FindConnect(connName);
                if (c != null)
                {
                    var prov = ConnectionConfig.ResolveProvider(c.ProviderId);
                    if (prov != null && !string.IsNullOrEmpty(prov.BaseUrl)) return prov.BaseUrl;
                }
            }
        }
        // 全局场景：按全局配置网关精确匹配目录条目 → 返回实时注册表地址
        if (slot.UseGlobal && !string.IsNullOrEmpty(modelId))
        {
            var cfgBase = Config.Instance.BaseUrl;
            if (!string.IsNullOrWhiteSpace(cfgBase))
            {
                var norm = cfgBase.Trim().TrimEnd('/');
                var match = ModelCatalog.All.FirstOrDefault(m => m.Id == modelId
                    && !string.IsNullOrEmpty(m.DefaultBaseUrl)
                    && string.Equals(m.DefaultBaseUrl.Trim().TrimEnd('/'), norm, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    var reg = ModelCatalog.Providers.TryGetValue(match.ProviderId, out var rp) ? rp.DefaultBaseUrl : null;
                    return !string.IsNullOrEmpty(reg) ? reg : match.DefaultBaseUrl;
                }
            }
        }
        // 模型目录默认（provider 注册表优先）
        if (!string.IsNullOrEmpty(modelId))
        {
            var info = ModelCatalog.Find(modelId);
            // 两层架构：provider 承载唯一地址（地址不同=不同服务商），模型用所属 provider 的地址连接
            if (info != null
                && ModelCatalog.Providers.TryGetValue(info.ProviderId, out var p)
                && !string.IsNullOrEmpty(p.DefaultBaseUrl))
                return p.DefaultBaseUrl;
            if (info?.DefaultBaseUrl != null)
                return info.DefaultBaseUrl;
        }
        return Config.Instance.BaseUrl;
    }

    /// <summary>
    /// 根据槽位配置解析出实际的大模型 ID。
    /// 未显式设置的槽位默认继承 F1（槽位 0）的模型设置。
    /// </summary>
    public static string ResolveLargeModel(SlotConfig slot, int slotIndex = -1)
    {
        // 有独立配置 → 直接返回
        if (!slot.UseGlobal && !string.IsNullOrWhiteSpace(slot.LargeModel))
            return slot.LargeModel;

        // 未设置 → 继承 F1（槽位 0），但 F1 自身回退到全局
        if (slotIndex != 0)
        {
            var f1 = Get(0);
            if (!f1.UseGlobal && !string.IsNullOrWhiteSpace(f1.LargeModel))
                return f1.LargeModel;
        }

        return Config.Instance.Model;
    }

    /// <summary>
    /// 根据槽位配置解析出实际的小模型 ID。
    /// 未显式设置的槽位默认继承 F1（槽位 0）的模型设置。
    /// </summary>
    public static string ResolveSmallModel(SlotConfig slot, int slotIndex = -1)
    {
        if (!slot.UseGlobal && !string.IsNullOrWhiteSpace(slot.SmallModel))
            return slot.SmallModel;

        if (slotIndex != 0)
        {
            var f1 = Get(0);
            if (!f1.UseGlobal && !string.IsNullOrWhiteSpace(f1.SmallModel))
                return f1.SmallModel;
        }

        return Config.Instance.SmallModel;
    }

    /// <summary>解析槽位实际生效的大模型 provider（优先槽位 connect，其次模型目录，最后全局）。供模型栏 `(provider)model` 展示。</summary>
    public static string ResolveLargeProvider(SlotConfig slot, int slotIndex = -1)
        => ResolveSlotProvider(slot, slotIndex, isLarge: true);

    /// <summary>解析槽位实际生效的小模型 provider。供模型栏 `(provider)model` 展示。</summary>
    public static string ResolveSmallProvider(SlotConfig slot, int slotIndex = -1)
        => ResolveSlotProvider(slot, slotIndex, isLarge: false);

    private static string ResolveSlotProvider(SlotConfig slot, int slotIndex, bool isLarge)
    {
        // 槽位显式 connect → 用其 provider
        var connName = !slot.UseGlobal && !string.IsNullOrWhiteSpace(isLarge ? slot.BigConnect : slot.SmallConnect)
            ? (isLarge ? slot.BigConnect : slot.SmallConnect) : null;
        if (connName == null && slotIndex != 0)
        {
            var f1 = Get(0);
            if (!f1.UseGlobal) connName = isLarge ? f1.BigConnect : f1.SmallConnect;
        }
        if (!string.IsNullOrWhiteSpace(connName))
        {
            var c = ConnectionConfig.FindConnect(connName);
            if (c != null) return c.ProviderId;
        }
        // 按「实际生效模型 + 网关」精确定位服务商：同 id 多服务商（deepseek-v4-flash 分属 DeepSeek 官方/AIHubMix）
        // 时 Find(model) 内置官方优先会误报（选了 AIHubMix 却解析成 deepseek），必须用 baseUrl 区分。
        var model = isLarge ? ResolveLargeModel(slot, slotIndex) : ResolveSmallModel(slot, slotIndex);
        var baseUrl = ResolveBaseUrl(slot, model);
        var info = ModelCatalog.Find(model, baseUrl);
        if (info != null) return info.ProviderId;
        var inferred = ModelCatalog.InferProviderFromBaseUrl(baseUrl);
        if (inferred != null) return inferred;
        return isLarge ? Config.Instance.Provider : Config.Instance.SmallProvider;
    }

    /// <summary>
    /// 解析槽位实际生效的大模型、API Key 与 BaseUrl —— 状态栏与实际请求共用的统一来源。
    /// 优先级：
    ///   1. 槽位配置（agent_slots.json）有模型 + 有可用 key → 用槽位配置，不碰全局 .env 模型；
    ///   2. 槽位缺 key（模型目录/服务商库无该模型的 key）→ 回退全局 .env（WAYCODER_MODEL + 对应 key）；
    ///   3. .env 可用 → 把 .env 的模型/provider/baseUrl 写回槽位配置持久化，
    ///      使下次状态栏显示与实际请求一致（不再出现「状态栏 A、实际用 B」）。
    /// 返回：(模型, key, baseUrl, 是否写回槽位配置)。
    /// </summary>
    public static (string Model, string ApiKey, string? BaseUrl, bool WroteBack) ResolveEffectiveModel(int slotIndex)
    {
        var slot = Get(slotIndex);
        var model = ResolveLargeModel(slot, slotIndex);
        var apiKey = ResolveApiKey(slot);
        var baseUrl = ResolveBaseUrl(slot, model);

        // 槽位模型已有可用 key → 直接使用槽位配置（含 UseGlobal 继承全局且有全局 key 的情况）
        if (!string.IsNullOrWhiteSpace(apiKey))
            return (model, apiKey, baseUrl, false);

        // 槽位缺 key → 回退全局 .env 模型（WAYCODER_MODEL），需有对应 key 才可用
        var cfg = Config.Instance;
        var envModel = cfg.Model;
        if (string.IsNullOrWhiteSpace(envModel) || envModel == model)
            return (model, apiKey, baseUrl, false);

        var envProvider = ModelCatalog.InferProviderFromId(envModel).ProviderId;
        var envKey = ApiKeyStore.Get(envProvider)
            ?? ApiKeyStore.ForModel(envModel)
            ?? cfg.ApiKey;
        if (string.IsNullOrWhiteSpace(envKey))
            return (model, apiKey, baseUrl, false);

        // .env 可用 → 写回槽位配置持久化（含 provider / base-url，保证模型与端点匹配）
        var info = ModelCatalog.Find(envModel);
        var envBaseUrl = (ModelCatalog.Providers.TryGetValue(envProvider, out var p) ? p.DefaultBaseUrl : null)
            ?? info?.DefaultBaseUrl
            ?? cfg.BaseUrl;
        slot.LargeModel = envModel;
        slot.ApiKeyProviderId = envProvider;
        slot.BaseUrl = string.IsNullOrWhiteSpace(envBaseUrl) ? null : envBaseUrl;
        slot.UseGlobal = false;
        Set(slotIndex, slot);

        return (envModel, envKey, envBaseUrl, true);
    }

    // ════════════════════════════════════════════════════════════
    // 持久化
    // ════════════════════════════════════════════════════════════

    private static string FilePath =>
        Global.WriteConfigPath(Directory.GetCurrentDirectory(), "agent_slots.json");

    private static SlotConfig[] Load()
    {
        if (_slots != null) return _slots;

        try
        {
            if (File.Exists(FilePath))
            {
                var data = Json.Parse(File.ReadAllText(FilePath));
                if (data != null)
                {
                    // 读取统一模式
                    UniformMode = data.GetBool("uniformMode");
                    if (data["uniformTemplate"] is { } template)
                        UniformTemplate = SlotFromNode(template);

                    // 读取槽位
                    var arr = data["slots"];
                    if (arr != null && arr.Kind == JKind.Array)
                    {
                        _slots = new SlotConfig[SlotCount];
                        for (int i = 0; i < Math.Min(arr.Count, SlotCount); i++)
                            _slots[i] = arr[i] is { } slotNode ? SlotFromNode(slotNode) : new();
                        // 补齐不足 10 个的槽位
                        for (int i = arr.Count; i < SlotCount; i++)
                            _slots[i] = new();
                        return _slots;
                    }
                }
            }
        }
        catch { /* 文件损坏，使用默认 */ }

        // 默认：全部使用全局配置
        _slots = new SlotConfig[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new();
        return _slots;
    }

    private static void Save(SlotConfig[] slots)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            _slots = slots;
            var arr = JNode.Array();
            foreach (var s in slots)
                arr.Add(SlotToNode(s));

            var data = JNode.Object()
                .Set("uniformMode", UniformMode)
                .Set("uniformTemplate", SlotToNode(UniformTemplate))
                .Set("slots", arr);

            File.WriteAllText(FilePath, Json.Serialize(data, indent: true));
        }
        catch { /* 保存失败不崩溃 */ }
    }

    private static SlotConfig Clone(SlotConfig src) => new()
    {
        BigConnect = src.BigConnect,
        SmallConnect = src.SmallConnect,
        LargeModel = src.LargeModel,
        SmallModel = src.SmallModel,
        BaseUrl = src.BaseUrl,
        ApiKeyProviderId = src.ApiKeyProviderId,
        ApiKey = src.ApiKey,
        UseGlobal = src.UseGlobal,
    };

    /// <summary>JNode → SlotConfig（手搓解析，零反射；键名保持 PascalCase 兼容历史文件）。</summary>
    internal static SlotConfig SlotFromNode(JNode n) => new()
    {
        BigConnect = n.GetString("BigConnect"),
        SmallConnect = n.GetString("SmallConnect"),
        LargeModel = n.GetString("LargeModel"),
        SmallModel = n.GetString("SmallModel"),
        BaseUrl = n.GetString("BaseUrl"),
        ApiKeyProviderId = n.GetString("ApiKeyProviderId"),
        ApiKey = n.GetString("ApiKey"),
        UseGlobal = !n.Has("UseGlobal") || n.GetBool("UseGlobal"),
    };

    /// <summary>SlotConfig → JNode（手搓序列化，零反射）。</summary>
    internal static JNode SlotToNode(SlotConfig s) => JNode.Object()
        .Set("BigConnect", s.BigConnect)
        .Set("SmallConnect", s.SmallConnect)
        .Set("LargeModel", s.LargeModel)
        .Set("SmallModel", s.SmallModel)
        .Set("BaseUrl", s.BaseUrl)
        .Set("ApiKeyProviderId", s.ApiKeyProviderId)
        .Set("ApiKey", s.ApiKey)
        .Set("UseGlobal", s.UseGlobal);

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { _slots = null; }
}
