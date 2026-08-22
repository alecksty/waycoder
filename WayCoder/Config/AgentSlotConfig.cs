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
    /// 优先级：SlotConfig.BaseUrl > 模型目录默认 Url > 全局 Config.BaseUrl
    /// </summary>
    public static string? ResolveBaseUrl(SlotConfig slot, string? largeModelId)
    {
        if (!string.IsNullOrWhiteSpace(slot.BaseUrl))
            return slot.BaseUrl;
        if (largeModelId != null)
        {
            var info = ModelCatalog.Find(largeModelId);
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
        // 按解析出的模型查目录
        var model = isLarge ? ResolveLargeModel(slot, slotIndex) : ResolveSmallModel(slot, slotIndex);
        var info = ModelCatalog.Find(model);
        if (info != null) return info.ProviderId;
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
