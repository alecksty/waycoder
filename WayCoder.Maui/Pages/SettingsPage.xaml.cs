using System.Text;
using System.Net.Http;
using System.Text.RegularExpressions;
using WayCoder;
using WayCoder.Infra;
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
            var disp = ModelCatalog.ProviderDisplayName(current);
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

        // 小模型（双模型架构的补全/摘要/压缩侧）：独立服务商/模型/地址/Key
        var smallCurrent = string.IsNullOrEmpty(cfg.SmallProvider) ? current : cfg.SmallProvider.ToLowerInvariant();
        if (providers.All(p => p.Id != smallCurrent))
        {
            var disp = ModelCatalog.ProviderDisplayName(smallCurrent);
            providers.Insert(0, new ProviderOption(smallCurrent, disp));
        }
        SmallProviderPicker.ItemsSource = providers;
        SmallProviderPicker.SelectedItem = providers.FirstOrDefault(p => p.Id == smallCurrent) ?? providers.FirstOrDefault();

        ReloadSmallModels(smallCurrent, cfg.SmallModel);
        SmallBaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(smallCurrent)?.DefaultBaseUrl;

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
        UpdateSmallKeyStatus(smallCurrent);
        RefreshWorkspaceStatus();
    }

    /// <summary>刷新 workspace 存储状态（外部/私有）。</summary>
    private void RefreshWorkspaceStatus()
    {
        var ext = WayCoder.Maui.MauiBootstrap.WorkspaceExternal;
        WorkspaceStatusLabel.Text = ext
            ? $"✅ workspace 在外部存储：{WayCoder.Maui.MauiBootstrap.WorkspaceDir}"
            : $"⚠️ workspace 在 App 私有目录（卸载重装会丢失代码）：\n{WayCoder.Maui.MauiBootstrap.WorkspaceDir}";
        ExternalWorkspaceBtn.Text = ext
            ? "🗂 外部存储已启用"
            : "🗂 启用外部存储 workspace（卸载重装代码不丢）";
    }

    /// <summary>启用外部存储 workspace：未授权先跳系统「所有文件访问」设置，授权后自动迁移。</summary>
    private async void OnExternalWorkspaceClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            try
            {
                // 「所有文件访问」设置页（Android 11+）
                var intent = new Android.Content.Intent(
                    "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION",
                    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch
            {
                // 部分 ROM 无该页面，退到应用详情页
                var intent = new Android.Content.Intent("android.settings.APPLICATION_DETAILS_SETTINGS",
                    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            await DisplayAlertAsync("已跳转设置", "请开启「允许访问所有文件」，返回后点本按钮完成迁移。", "确定");
            return;
        }

        var ok = WayCoder.Maui.MauiBootstrap.TryEnableExternalWorkspace();
        RefreshWorkspaceStatus();
        await DisplayAlertAsync(ok ? "已启用" : "启用失败",
            ok
                ? "workspace 已迁移到 sdcard/waycoder/workspace、配置到 sdcard/waycoder/config，卸载重装不丢。重启应用后配置完全生效。"
                : "无法启用外部存储，请检查权限。", "确定");
#else
        await DisplayAlertAsync("外部存储", "仅 Android 支持。", "确定");
#endif
    }

    /// <summary>应用模型选择：统一入口切换服务商+模型并刷新 UI。</summary>
    private void ApplyModel(string providerId, string modelId)
    {
        ConnectionConfig.ApplyModelChoice(providerId, modelId, isLarge: true, out _);
        var disp = ModelCatalog.ProviderDisplayName(providerId);
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
                // 环境变量引用（$VAR / ${VAR}）不是真实 key，跳过导入（防把环境变量当 key 误存）
                if (ApiKeyStore.IsEnvVarRef(key))
                {
                    await DisplayAlertAsync("已跳过", "检测到环境变量引用（$VAR），不是真实 Key，未导入。请填入真实 API Key。", "确定");
                    return;
                }
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

    /// <summary>小模型组：按服务商刷新模型下拉（含选中兜底）。</summary>
    private void ReloadSmallModels(string providerId, string? selectModel = null)
    {
        var models = ModelCatalog.ByProvider(providerId).ToList();
        SmallModelPicker.ItemsSource = models;

        var target = selectModel ?? Config.Instance.SmallModel;
        var match = models.FirstOrDefault(m => string.Equals(m.Id, target, StringComparison.OrdinalIgnoreCase));
        SmallModelPicker.SelectedItem = match ?? models.FirstOrDefault();
    }

    private void OnSmallProviderChanged(object? sender, EventArgs e)
    {
        if (SmallProviderPicker.SelectedItem is not ProviderOption opt) return;
        ReloadSmallModels(opt.Id);
        SmallBaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(opt.Id)?.DefaultBaseUrl;
        UpdateSmallKeyStatus(opt.Id);
    }

    private void UpdateSmallKeyStatus(string providerId)
    {
        var masked = ApiKeyStore.Masked(providerId);
        SmallKeyStatusLabel.Text = masked != null ? $"已保存：{masked}" : "未配置 Key";
    }

    /// <summary>大模型分组测试 Key。</summary>
    private async void OnTestKeyClicked(object? sender, EventArgs e) => await TestKeyFlowAsync(large: true);

    /// <summary>小模型分组测试 Key。</summary>
    private async void OnSmallTestKeyClicked(object? sender, EventArgs e) => await TestKeyFlowAsync(large: false);

    /// <summary>
    /// 测试 API Key 通用流程（大/小模型分组共用）：用输入框（或已存）key 发最小 chat 请求
    /// （max_tokens=1）。有效 → 立即写入 ApiKeyStore 并刷新状态；无效 → 弹错误（含服务端信息）。
    /// </summary>
    private async Task TestKeyFlowAsync(bool large)
    {
        var opt = (large ? ProviderPicker : SmallProviderPicker).SelectedItem as ProviderOption;
        var model = (large ? ModelPicker : SmallModelPicker).SelectedItem as ModelCatalog.ModelInfo;
        var keyEntry = large ? KeyEntry : SmallKeyEntry;
        var baseUrlEntry = large ? BaseUrlEntry : SmallBaseUrlEntry;
        var btn = large ? TestKeyBtn : SmallTestKeyBtn;
        var statusRefresher = large ? new Action<string>(UpdateKeyStatus) : UpdateSmallKeyStatus;

        if (opt == null)
        {
            await DisplayAlertAsync("未选择", "请先选择服务商", "确定");
            return;
        }
        var key = keyEntry.Text?.Trim();
        if (string.IsNullOrEmpty(key)) key = ApiKeyStore.Get(opt.Id);
        if (string.IsNullOrEmpty(key))
        {
            await DisplayAlertAsync("无 Key", "请在输入框填入要测试的 API Key", "确定");
            return;
        }

        var oldText = btn.Text;
        btn.Text = "⏳ 测试中…";
        btn.IsEnabled = false;
        try
        {
            var (ok, message) = await TestKeyCoreAsync(opt.Id, model?.Id ?? "", baseUrlEntry.Text, key);
            if (ok)
            {
                // 有效立即保存：环境变量引用（$VAR）测试会失败，这里双保险跳过
                if (ApiKeyStore.IsEnvVarRef(key))
                {
                    await DisplayAlertAsync("已跳过", "检测到环境变量引用（$VAR），不是真实 Key，未保存。", "确定");
                    keyEntry.Text = "";
                    return;
                }
                ApiKeyStore.Set(opt.Id, key);
                statusRefresher(opt.Id);        // 刷新状态标签
                keyEntry.Text = "";
                await DisplayAlertAsync("✅ Key 有效", $"服务商 {opt.DisplayName} 的 API Key 验证通过，已自动保存。", "确定");
            }
            else
            {
                await DisplayAlertAsync("❌ Key 无效", message, "确定");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("❌ 测试失败", $"{ex.GetType().Name}: {ex.Message}", "确定");
        }
        finally
        {
            btn.Text = oldText;
            btn.IsEnabled = true;
        }
    }

    /// <summary>用给定 key 发最小 chat 请求测试有效性。返回 (ok, message)。复用 LLM 端点拼接。</summary>
    private static async Task<(bool Ok, string Message)> TestKeyCoreAsync(
        string providerId, string modelId, string? baseUrlText, string key)
    {
        var baseUrl = string.IsNullOrWhiteSpace(baseUrlText)
            ? ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl
            : baseUrlText.Trim();
        var b = (baseUrl ?? "https://api.openai.com").TrimEnd('/');
        if (b.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) b = b[..^3].TrimEnd('/');
        var endpoint = b + "/v1/chat/completions";

        var msgs = JNode.Array();
        msgs.Add(JNode.Object().Set("role", "user").Set("content", "hi"));
        var body = JNode.Object()
            .Set("model", string.IsNullOrEmpty(modelId) ? "gpt-4o-mini" : modelId)
            .Set("max_tokens", 1)
            .Set("messages", msgs);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("Authorization", $"Bearer {key}");
        req.Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json");
        using var resp = await http.SendAsync(req);
        var respBody = await resp.Content.ReadAsStringAsync();

        if (resp.IsSuccessStatusCode) return (true, "验证通过");
        var msg = respBody.Length > 300 ? respBody[..300] + "…" : respBody;
        return (false, $"HTTP {(int)resp.StatusCode}\n{msg}");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderOption opt || ModelPicker.SelectedItem is not ModelCatalog.ModelInfo model)
        {
            await DisplayAlertAsync("未选择", "请选择服务商与模型", "确定");
            return;
        }

        // 1) API Key（非空才写；为空保留原 Key）——合法性校验：只允许字母数字 + `+-_.` 逗号，拒绝环境变量引用
        var key = KeyEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(key))
        {
            if (ApiKeyStore.IsEnvVarRef(key) || !ApiKeyStore.IsValidApiKey(key))
                await DisplayAlertAsync("Key 不合法", $"只允许英文字母数字 + - _ . ,（{opt.DisplayName} 的 Key 未保存，不要填环境变量引用 $VAR / %VAR%）", "确定");
            else
                ApiKeyStore.Set(opt.Id, key);
        }
        UpdateKeyStatus(opt.Id);   // 保存后立即刷新「已保存」状态（此前不刷新显示旧 key）

        // 2) 服务商 + 模型 + BaseUrl 统一入口（自动持久化 + 从环境变量导 key）
        var baseUrl = string.IsNullOrWhiteSpace(BaseUrlEntry.Text) ? null : BaseUrlEntry.Text.Trim();
        ConnectionConfig.ApplyModelChoice(opt.Id, model.Id, isLarge: true, out _, baseUrl);

        // 2.5) 小模型分组：独立服务商/模型/地址/Key（同一套 apply + ApiKeyStore）
        if (SmallProviderPicker.SelectedItem is ProviderOption sopt
            && SmallModelPicker.SelectedItem is ModelCatalog.ModelInfo smodel)
        {
            var skey = SmallKeyEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(skey))
            {
                if (ApiKeyStore.IsEnvVarRef(skey) || !ApiKeyStore.IsValidApiKey(skey))
                    await DisplayAlertAsync("Key 不合法", $"只允许英文字母数字 + - _ . ,（{sopt.DisplayName} 的小模型 Key 未保存）", "确定");
                else
                    ApiKeyStore.Set(sopt.Id, skey);
            }
            UpdateSmallKeyStatus(sopt.Id);   // 同样立即刷新
            var sbaseUrl = string.IsNullOrWhiteSpace(SmallBaseUrlEntry.Text) ? null : SmallBaseUrlEntry.Text.Trim();
            ConnectionConfig.ApplyModelChoice(sopt.Id, smodel.Id, isLarge: false, out _, sbaseUrl);
        }

        // 3) 参数
        if (int.TryParse(MaxTokensEntry.Text, out var mt)) Config.Instance.MaxTokens = mt;
        if (float.TryParse(TemperatureEntry.Text, out var tp)) Config.Instance.Temperature = tp;
        if (int.TryParse(MaxContextEntry.Text, out var mct)) Config.Instance.MaxContextTokens = mct;
        Config.Instance.MaxBudgetUsd = double.TryParse(BudgetEntry.Text, out var bud) ? bud : null;
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

        // 4.5) 权限模式（确认轴）
        if (PermModePicker.SelectedIndex >= 0)
            PermissionManager.CurrentMode = (PermissionManager.Mode)PermModePicker.SelectedIndex;
        // 记住工作/权限/经济三种模式（手机无快捷键，下次启动直接恢复）
        Services.MauiModeStore.Save(WorkModeManager.CurrentMode, PermissionManager.CurrentMode, Config.Instance.EconomyMode);

        // 5) 持久化 + 重建 Agent（下次发送按新配置）
        Config.Instance.SaveToEnvFile();
        AgentService.Reset();

        KeyEntry.Text = "";
        await DisplayAlertAsync("已保存", $"服务商 {opt.DisplayName} · 模型 {model.DisplayName}", "确定");
    }
}
