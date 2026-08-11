using System.Text.Json;

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
                var json = File.ReadAllText(FilePath);
                var data = JsonNode.Parse(json);
                if (data != null)
                {
                    // 读取统一模式
                    UniformMode = data["uniformMode"]?.GetValue<bool>() ?? false;
                    if (data["uniformTemplate"] is { } template)
                        UniformTemplate = JsonSerializer.Deserialize<SlotConfig>(template.ToJsonString()) ?? new();

                    // 读取槽位
                    if (data["slots"]?.AsArray() is { } arr)
                    {
                        _slots = new SlotConfig[SlotCount];
                        for (int i = 0; i < Math.Min(arr.Count, SlotCount); i++)
                        {
                            var slotJson = arr[i]?.ToJsonString();
                            _slots[i] = slotJson != null
                                ? JsonSerializer.Deserialize<SlotConfig>(slotJson) ?? new()
                                : new();
                        }
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
            var data = new JsonObject
            {
                ["uniformMode"] = UniformMode,
                ["uniformTemplate"] = JsonNode.Parse(JsonSerializer.Serialize(UniformTemplate)),
                ["slots"] = new JsonArray(slots.Select(s =>
                    JsonNode.Parse(JsonSerializer.Serialize(s))!).ToArray()),
            };
            File.WriteAllText(FilePath, data.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* 保存失败不崩溃 */ }
    }

    private static SlotConfig Clone(SlotConfig src) => new()
    {
        LargeModel = src.LargeModel,
        SmallModel = src.SmallModel,
        BaseUrl = src.BaseUrl,
        ApiKeyProviderId = src.ApiKeyProviderId,
        ApiKey = src.ApiKey,
        UseGlobal = src.UseGlobal,
    };

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { _slots = null; }
}
