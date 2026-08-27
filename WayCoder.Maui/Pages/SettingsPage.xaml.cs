using System.Text.RegularExpressions;
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
        PopulateFreeModels();
    }

    /// <summary>免费模型（0 价）按服务商分组，点选即切换该服务商+模型。</summary>
    private void PopulateFreeModels()
    {
        FreeModelsLayout.Clear();
        var free = ModelCatalog.All
            .Where(m => m.InputPrice == 0 && m.OutputPrice == 0)
            .GroupBy(m => m.ProviderId)
            .OrderBy(g => g.Key);
        var any = false;
        foreach (var g in free)
        {
            var disp = ModelCatalog.Providers.TryGetValue(g.Key, out var p) ? p.DisplayName : g.Key;
            foreach (var m in g)
            {
                any = true;
                var pid = g.Key; var mid = m.Id;
                var btn = new Button
                {
                    Text = $"🆓 {disp} · {m.DisplayName}",
                    FontSize = 12,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#8AB4F8") : Color.FromArgb("#1A56DB"),
                    HorizontalOptions = LayoutOptions.Start,
                };
                btn.Clicked += async (_, _) =>
                {
                    ApplyModel(pid, mid);
                    await DisplayAlertAsync("已选用", $"{disp} · {m.DisplayName} 已设为当前模型（Key 按服务商在「连接」区填写）", "确定");
                };
                FreeModelsLayout.Add(btn);
            }
        }
        if (!any)
            FreeModelsLayout.Add(new Label
            {
                Text = "暂无 0 价模型（低价款见模型下拉列表）",
                FontSize = 12,
                TextColor = Color.FromArgb("#888888"),
            });
    }

    /// <summary>应用模型选择：统一入口切换服务商+模型并刷新 UI。</summary>
    private void ApplyModel(string providerId, string modelId)
    {
        ConnectionConfig.ApplyModelChoice(providerId, modelId, isLarge: true, out _);
        var disp = ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId;
        ProviderPicker.SelectedItem = new ProviderOption(providerId, disp);
        ReloadModels(providerId, modelId);
        UpdateKeyStatus(providerId);
        BaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl;
    }

    /// <summary>导入模型：内置目录 / 从配置文件（Claude/OpenCode 等）启发式解析 Key+模型。</summary>
    private async void OnImportModelsClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("导入模型", "取消", null, "从配置文件导入", "导入内置模型目录");
        if (action == "从配置文件导入")
            await ImportFromConfigFileAsync();
        else if (action == "导入内置模型目录")
            await DisplayAlertAsync("内置模型",
                $"内置模型目录已包含 {ModelCatalog.All.Length} 个模型（DeepSeek / Qwen / Zhipu / OpenAI / Anthropic / AIHubMix 等）。在「连接」区选服务商即可使用。", "确定");
    }

    /// <summary>文件选择器选 Claude/OpenCode 等配置，启发式提取 API Key + 模型并应用。</summary>
    private async Task ImportFromConfigFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "选择 Claude/OpenCode 等配置文件" });
            if (result == null) return;
            var content = await File.ReadAllTextAsync(result.FullPath);

            // 启发式正则（AOT 安全）：找 API Key 与模型
            var key = Regex.Match(content, "(?i)\"(api_key|apiKey|ANTHROPIC_API_KEY|OPENAI_API_KEY|DEEPSEEK_API_KEY)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;
            var model = Regex.Match(content, "(?i)\"(model|defaultModel)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;

            var applied = false;
            var pid = Config.Instance.Provider;
            if (!string.IsNullOrEmpty(key))
            {
                ApiKeyStore.Set(pid, key);
                UpdateKeyStatus(pid);
                applied = true;
            }
            if (!string.IsNullOrEmpty(model) && ModelCatalog.Find(model) is { } mi)
            {
                ApplyModel(mi.ProviderId, mi.Id);
                applied = true;
            }
            await DisplayAlertAsync(applied ? "已导入" : "未识别",
                applied ? "已从配置文件导入模型 / API Key（请到「保存」确认生效）。" : "未能从该文件识别模型或 API Key。", "确定");
        }
        catch (Exception ex) { await DisplayAlertAsync("导入失败", ex.Message, "关闭"); }
    }

    /// <summary>扫描全部服务商连接可达性（HTTP GET 默认地址，3s 超时）。</summary>
    private async void OnScanConnectionsClicked(object? sender, EventArgs e)
    {
        var providers = ModelCatalog.Providers
            .Where(kv => kv.Key is not ("local" or "custom"))
            .Select(kv => (Id: kv.Key, Name: kv.Value.DisplayName, Url: kv.Value.DefaultBaseUrl))
            .ToList();

        var results = new List<string>();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        foreach (var p in providers)
        {
            if (string.IsNullOrEmpty(p.Url)) { results.Add($"{p.Name}（{p.Id}）· 无默认地址"); continue; }
            var ok = false;
            try { using var _ = await http.GetAsync(p.Url); ok = true; }
            catch { ok = false; }
            results.Add($"{p.Name}（{p.Id}）· {(ok ? "✅ 可达" : "❌ 不可达")}");
        }
        await DisplayActionSheetAsync($"连接扫描（{providers.Count}）", "关闭", null, results.ToArray());
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
