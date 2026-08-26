using WayCoder;
using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 设置页 —— 服务商 / 模型 / API Key / BaseUrl / 参数。
/// 保存链路复用主工程配置 API（MAUI 已编译 Config/）：ApiKeyStore.Set 存密钥、
/// ConnectionConfig.ApplyModelChoice 切模型、Config.SaveToEnvFile 持久化、AgentService.Reset 重建 Agent。
/// </summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>服务商下拉项（展示名 + 内部 id）。</summary>
    private sealed record ProviderOption(string Id, string DisplayName);

    /// <summary>权限模式标签（索引与 <see cref="PermissionManager.Mode"/> 枚举顺序一致）。</summary>
    private static readonly string[] PermModeLabels =
        ["Ask（每次确认）", "Auto（改动必问）", "SmartAuto（危险必问）", "Yolo（不确认）"];

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadConfig();
    }

    private void LoadConfig()
    {
        var cfg = Config.Instance;

        // 服务商列表（排除 local/custom，按展示名排序；保留当前服务商）
        var providers = ModelCatalog.Providers
            .Where(kv => kv.Key is not ("local" or "custom"))
            .OrderBy(kv => kv.Value.DisplayName)
            .Select(kv => new ProviderOption(kv.Key, kv.Value.DisplayName))
            .ToList();

        var current = cfg.Provider.ToLowerInvariant();
        if (providers.All(p => p.Id != current))
        {
            var disp = ModelCatalog.Providers.TryGetValue(current, out var p) ? p.DisplayName : current;
            providers.Insert(0, new ProviderOption(current, disp));
        }

        ProviderPicker.ItemsSource = providers;
        ProviderPicker.SelectedItem = providers.FirstOrDefault(p => p.Id == current) ?? providers.FirstOrDefault();

        ReloadModels(current, cfg.Model);

        BaseUrlEntry.Text = cfg.BaseUrl ?? ModelCatalog.Providers.GetValueOrDefault(current)?.DefaultBaseUrl;
        MaxTokensEntry.Text = cfg.MaxTokens.ToString();
        TemperatureEntry.Text = cfg.Temperature.ToString("F1");

        EconomyPicker.ItemsSource = new List<string> { "off", "auto", "on", "extreme" };
        EconomyPicker.SelectedItem = cfg.EconomyMode.ToString().ToLowerInvariant();

        // 小模型（双模型架构的补全/摘要/压缩侧）
        SmallModelPicker.ItemsSource = ModelCatalog.All;
        SmallModelPicker.SelectedItem = ModelCatalog.All.FirstOrDefault(m => m.Id == cfg.SmallModel)
            ?? ModelCatalog.All.FirstOrDefault();

        // 推理深度（空=模型默认）
        ReasoningPicker.ItemsSource = new List<string> { "", "minimal", "low", "medium", "high", "max" };
        ReasoningPicker.SelectedItem = string.IsNullOrEmpty(cfg.ReasoningEffort) ? "" : cfg.ReasoningEffort;

        // 上下文窗口 + 预算上限
        MaxContextEntry.Text = cfg.MaxContextTokens.ToString();
        BudgetEntry.Text = cfg.MaxBudgetUsd?.ToString("0.##") ?? "";

        // Whisper 语音（空 Key 回退主 Key）
        WhisperModelEntry.Text = cfg.WhisperModel;
        WhisperBaseUrlEntry.Text = cfg.WhisperBaseUrl ?? "";
        WhisperKeyEntry.Text = "";

        // 权限模式（确认轴：Ask/Auto/SmartAuto/Yolo；索引与 enum 顺序一致）
        PermModePicker.ItemsSource = PermModeLabels;
        PermModePicker.SelectedItem = PermModeLabels[(int)PermissionManager.CurrentMode];

        UpdateKeyStatus(current);
    }

    /// <summary>按服务商刷新模型下拉；可选指定选中模型（含当前模型不在该服务商时的兜底）。</summary>
    private void ReloadModels(string providerId, string? selectModel = null)
    {
        var models = ModelCatalog.ByProvider(providerId).ToList();
        ModelPicker.ItemsSource = models;

        var target = selectModel ?? Config.Instance.Model;
        var match = models.FirstOrDefault(m => string.Equals(m.Id, target, StringComparison.OrdinalIgnoreCase));
        ModelPicker.SelectedItem = match ?? models.FirstOrDefault();
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderOption opt) return;
        ReloadModels(opt.Id);
        BaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(opt.Id)?.DefaultBaseUrl;
        UpdateKeyStatus(opt.Id);
    }

    private void UpdateKeyStatus(string providerId)
    {
        var masked = ApiKeyStore.Masked(providerId);
        KeyStatusLabel.Text = masked != null ? $"已保存：{masked}" : "未配置 Key";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderOption opt || ModelPicker.SelectedItem is not ModelCatalog.ModelInfo model)
        {
            await DisplayAlertAsync("未选择", "请选择服务商与模型", "确定");
            return;
        }

        // 1) API Key（非空才写；为空保留原 Key）
        var key = KeyEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(key))
            ApiKeyStore.Set(opt.Id, key);

        // 2) 服务商 + 模型 + BaseUrl 统一入口（自动持久化 + 从环境变量导 key）
        var baseUrl = string.IsNullOrWhiteSpace(BaseUrlEntry.Text) ? null : BaseUrlEntry.Text.Trim();
        ConnectionConfig.ApplyModelChoice(opt.Id, model.Id, isLarge: true, out _, baseUrl);

        // 3) 参数
        if (int.TryParse(MaxTokensEntry.Text, out var mt)) Config.Instance.MaxTokens = mt;
        if (float.TryParse(TemperatureEntry.Text, out var tp)) Config.Instance.Temperature = tp;
        if (int.TryParse(MaxContextEntry.Text, out var mct)) Config.Instance.MaxContextTokens = mct;
        Config.Instance.MaxBudgetUsd = double.TryParse(BudgetEntry.Text, out var bud) ? bud : null;
        if (SmallModelPicker.SelectedItem is ModelCatalog.ModelInfo sm) Config.Instance.SmallModel = sm.Id;
        if (ReasoningPicker.SelectedItem is string re)
            Config.Instance.ReasoningEffort = string.IsNullOrEmpty(re) ? "" : re;
        if (EconomyPicker.SelectedItem is string eco && Enum.TryParse<EconomyMode>(eco, true, out var em))
            Config.Instance.EconomyMode = em;

        // 4) 语音（Whisper 转录；空 Key 回退主 Key）
        Config.Instance.WhisperModel = string.IsNullOrWhiteSpace(WhisperModelEntry.Text) ? "whisper-1" : WhisperModelEntry.Text.Trim();
        Config.Instance.WhisperBaseUrl = string.IsNullOrWhiteSpace(WhisperBaseUrlEntry.Text) ? null : WhisperBaseUrlEntry.Text.Trim();
        var whisperKey = WhisperKeyEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(whisperKey))
            Config.Instance.WhisperApiKey = whisperKey;

        // 4.5) 权限模式（确认轴；仅内存，随 App 会话生效）
        if (PermModePicker.SelectedIndex >= 0)
            PermissionManager.CurrentMode = (PermissionManager.Mode)PermModePicker.SelectedIndex;

        // 5) 持久化 + 重建 Agent（下次发送按新配置）
        Config.Instance.SaveToEnvFile();
        AgentService.Reset();

        KeyEntry.Text = "";
        await DisplayAlertAsync("已保存", $"服务商 {opt.DisplayName} · 模型 {model.DisplayName}", "确定");
    }
}
