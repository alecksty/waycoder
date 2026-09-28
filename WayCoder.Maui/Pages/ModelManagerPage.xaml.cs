using System.Text.RegularExpressions;
using WayCoder;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 供应商与模型管理页：列出全部供应商及其模型，可导入供应商/模型、扫描连通性。
/// 自定义供应商/模型经 <see cref="ModelCatalog.AddCustom"/> 写入（供应商由自定义模型隐式创建）。
/// </summary>
public partial class ModelManagerPage : ContentPage
{
    /// <summary>各供应商连通性缓存（providerId → 可达）。</summary>
    private readonly Dictionary<string, bool?> _connectivity = new();

    public ModelManagerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Populate();
    }

    private void Populate()
    {
        ProviderList.Clear();

        // 按供应商分组模型（内置 + 自定义）
        var byProvider = ModelCatalog.All
            .Where(m => m.ProviderId is not "local" and not "custom")
            .GroupBy(m => m.ProviderId)
            .OrderBy(g => g.Key);

        foreach (var g in byProvider)
        {
            var display = ModelCatalog.ProviderDisplayName(g.Key);
            var baseUrl = g.FirstOrDefault()?.DefaultBaseUrl ?? ModelCatalog.Providers.GetValueOrDefault(g.Key)?.DefaultBaseUrl;
            var conn = _connectivity.GetValueOrDefault(g.Key);

            // 图标：本地 → 🌿；有 Key → 🔑；无 Key 非本地 → ⚠️（警告）
            var icon = g.Key is "local" or "custom" ? "🌿" : ApiKeyStore.Masked(g.Key) != null ? "🔑" : "⚠️";

            var card = new Border
            {
                BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#1F1F2E") : Color.FromArgb("#F2F2F7"),
                StrokeThickness = 0,
                Padding = new Thickness(12, 8),
            };
            card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 };
            // 点卡片 → 供应商菜单：管理模型 / 设Key / 清Key / 改名 / 改地址 / 删除（手机没有 /provider 快捷键，统一入口）
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                // 菜单项与判据是同一批文案（平台只回传被点的**文字**、不回传序号）⇒ 用同一个标签数组按序号判定：
                // 免得「翻了菜单项、漏了 case」在英文下静默失配（点了没反应，且没有任何报错）。
                var labels = new[]
                {
                    L.Pick("管理模型", "Manage models"), L.Pick("设Key", "Set key"), L.Pick("清Key", "Clear key"),
                    L.Pick("改名", "Rename"), L.Pick("改地址", "Change URL"), L.Pick("删除", "Delete"),
                };
                var action = await DisplayActionSheetAsync(L.Pick($"{display}（{g.Key}）", $"{display} ({g.Key})"),
                    L.Pick("取消", "Cancel"), null, labels);
                switch (Array.IndexOf(labels, action!))
                {
                    case 0:
                        await Shell.Current.GoToAsync($"providermodels?provider={Uri.EscapeDataString(g.Key)}");
                        break;
                    case 1:
                        await SetProviderKeyAsync(g.Key);
                        break;
                    case 2:
                        ApiKeyStore.Remove(g.Key);
                        Populate();
                        break;
                    case 3:
                        await RenameProviderAsync(g.Key);
                        break;
                    case 4:
                        await EditProviderUrlAsync(g.Key);
                        break;
                    case 5:
                        await DeleteProviderAsync(g.Key);
                        break;
                }
            };
            card.GestureRecognizers.Add(tap);

            var models = g.OrderBy(m => m.DisplayName).Select(m => m.DisplayName).ToList();
            var status = conn == true ? L.Pick("✅ 可达", "✅ Reachable")
                       : conn == false ? L.Pick("❌ 不可达", "❌ Unreachable")
                       : L.Pick("未扫描", "Not scanned");
            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(new Label
            {
                Text = L.Pick($"{icon} {display}（{g.Key}）· {g.Count()} 模型 · {status}",
                              $"{icon} {display} ({g.Key}) · {g.Count()} models · {status}"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#1A1A1A"),
            });
            stack.Add(new Label
            {
                Text = string.IsNullOrEmpty(baseUrl) ? L.Pick("无默认地址", "No default URL") : baseUrl,
                FontSize = 11,
                TextColor = Color.FromArgb("#888888"),
                LineBreakMode = LineBreakMode.TailTruncation,
            });
            stack.Add(new Label
            {
                Text = string.Join(L.Pick("、", ", "), models.Take(6))
                       + (models.Count > 6 ? L.Pick($" 等 {models.Count} 个", $" … ({models.Count} total)") : ""),
                FontSize = 11,
                TextColor = Color.FromArgb("#777777"),
                LineBreakMode = LineBreakMode.TailTruncation,
            });
            card.Content = stack;
            ProviderList.Add(card);
        }

        if (byProvider.Count() == 0)
            ProviderList.Add(new Label { Text = L.Pick("暂无供应商", "No providers yet"), FontSize = 13, TextColor = Color.FromArgb("#888888") });
    }

    /// <summary>扫描全部供应商连通性（探测 /models 接口，单点 4s 超时；委托核心 ModelCli）。</summary>
    private async void OnScanClicked(object? sender, EventArgs e)
    {
        ScanBtn.IsEnabled = false;
        ScanBtn.Text = L.Pick("扫描中…", "Scanning…");
        foreach (var p in ModelCli.ResolveScanTargets())
        {
            // 探测 /models（比 GET 首页更准确：首页可达 ≠ 接口可用）；Url 为空 → 不可达
            var (ok, _, _) = await ModelCli.ProbeEndpointAsync(p.Url, ApiKeyStore.Get(p.Id));
            _connectivity[p.Id] = ok;
        }
        ScanBtn.IsEnabled = true;
        ScanBtn.Text = L.Pick("📡 扫描", "📡 Scan");
        Populate();
    }

    /// <summary>导入供应商：名称 + 接口地址 + Key（生成一个占位模型使供应商出现，再导入真实模型）。</summary>
    private async void OnAddProviderClicked(object? sender, EventArgs e)
    {
        var title = L.Pick("导入供应商", "Import provider");
        var name = await DisplayPromptAsync(title, L.Pick("供应商名称（如：硅基流动）", "Provider name (e.g. SiliconFlow)"), accept: L.Pick("下一步", "Next"), maxLength: 30);
        if (string.IsNullOrWhiteSpace(name)) return;
        var id = await DisplayPromptAsync(title, L.Pick("供应商 ID（小写英文，如 siliconflow）", "Provider ID (lowercase, e.g. siliconflow)"), accept: L.Pick("下一步", "Next"), maxLength: 30);
        if (string.IsNullOrWhiteSpace(id)) return;
        // 规范化 ID：去掉 api- 前缀 / -ai 后缀（api-siliconflow-ai → siliconflow），保证名称/去重判断正确
        id = ModelCatalog.NormalizeProviderId(id);
        var baseUrl = await DisplayPromptAsync(title, L.Pick("接口地址 BaseUrl（OpenAI 兼容，如 https://api.siliconflow.cn/v1）", "Base URL (OpenAI-compatible, e.g. https://api.siliconflow.cn/v1)"), accept: L.Pick("下一步", "Next"), maxLength: 200);
        if (string.IsNullOrWhiteSpace(baseUrl)) return;
        var key = await DisplayPromptAsync(title, L.Pick("API Key（可留空稍后在设置填写）", "API key (leave blank to fill in later in Settings)"), accept: L.Pick("完成", "Done"), maxLength: 200);

        try
        {
            // 注册到 providers.json（/provider 列表可见）+ 占位模型（供应商卡片列表可见）
            var err = ModelCatalog.RegisterProviderResult(id, name, baseUrl);
            if (err != null)
            {
                await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), err, L.Pick("关闭", "Close"));
                return;
            }
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                $"{id}-placeholder", L.Pick($"{name} 占位", $"{name} placeholder"), name, id, "C", "Custom", 131_072, 0, 0, baseUrl,
                L.Pick($"自定义供应商 {name}（导入后请在「设置」填 Key）",
                       $"Custom provider {name} (enter the key in Settings after importing)")));
            if (!string.IsNullOrWhiteSpace(key)) ApiKeyStore.Set(id, key);
            _connectivity[id] = null;
            await DisplayAlertAsync(L.Pick("已导入", "Imported"),
                L.Pick($"供应商「{name}」已添加。再点「＋ 导入模型」添加真实模型，或在「设置」填 Key。",
                       $"Provider \"{name}\" was added. Tap \"+ Import model\" to add real models, or enter the key in Settings."),
                L.Pick("确定", "OK"));
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), ex.Message, L.Pick("关闭", "Close")); }
    }

    /// <summary>导入模型：多来源选择（对标 Web 版）——内置 / Claude Code / Codex / OpenCode / Crush / OpenClaw / 自定义。</summary>
    private async void OnAddModelClicked(object? sender, EventArgs e)
    {
        // ⚠ 「Claude Code / Codex / OpenCode / Crush / OpenClaw」既是菜单项**又是数据**：
        //    它们原样作为 sourceName 传下去（NormalizeProviderId → 供应商 ID，并写进模型描述）
        //    ⇒ 这五个**不能翻**（翻了就落到另一个供应商 ID 上）。只翻我们自己那三条。
        var labels = new[]
        {
            L.Pick("🌐 在线导入", "🌐 Import online"), L.Pick("内置模型目录", "Built-in catalog"),
            "Claude Code", "Codex", "OpenCode", "Crush", "OpenClaw",
            L.Pick("自定义添加", "Custom…"),
        };
        var source = await DisplayActionSheetAsync(L.Pick("导入模型 · 选择来源", "Import model · Choose source"),
            L.Pick("取消", "Cancel"), null, labels);
        switch (Array.IndexOf(labels, source!))
        {
            case 0:
                await ImportOnlineAsync();
                break;
            case 1:
                await DisplayAlertAsync(L.Pick("内置模型", "Built-in models"),
                    L.Pick($"内置目录已含 {ModelCatalog.All.Length} 个模型（DeepSeek/Qwen/Zhipu/OpenAI/Anthropic/AIHubMix 等），无需导入。",
                           $"The built-in catalog already has {ModelCatalog.All.Length} models (DeepSeek/Qwen/Zhipu/OpenAI/Anthropic/AIHubMix, etc.), so no import is needed."),
                    L.Pick("确定", "OK"));
                break;
            case 2 or 3 or 4 or 5 or 6:
                await ImportFromConfigFileAsync(source!);
                break;
            case 7:
                await AddCustomModelAsync();
                break;
        }
    }

    /// <summary>自定义添加模型：供应商 ID + 模型 ID + 显示名 + 上下文。</summary>
    private async Task AddCustomModelAsync()
    {
        var title = L.Pick("自定义添加模型", "Add custom model");
        var providerId = await DisplayPromptAsync(title, L.Pick("所属供应商 ID（如 siliconflow / deepseek）", "Provider ID (e.g. siliconflow / deepseek)"), accept: L.Pick("下一步", "Next"), maxLength: 30, initialValue: Config.Instance.Provider);
        if (string.IsNullOrWhiteSpace(providerId)) return;
        var modelId = await DisplayPromptAsync(title, L.Pick("模型 ID（API 调用用，如 deepseek-chat）", "Model ID (used for API calls, e.g. deepseek-chat)"), accept: L.Pick("下一步", "Next"), maxLength: 60);
        if (string.IsNullOrWhiteSpace(modelId)) return;
        var display = await DisplayPromptAsync(title, L.Pick("显示名（如 DeepSeek Chat）", "Display name (e.g. DeepSeek Chat)"), accept: L.Pick("下一步", "Next"), maxLength: 40, initialValue: modelId);
        if (string.IsNullOrWhiteSpace(display)) return;
        var context = await DisplayPromptAsync(title, L.Pick("上下文窗口（token，如 32768）", "Context window (tokens, e.g. 32768)"), accept: L.Pick("完成", "Done"), initialValue: "131072", maxLength: 10);

        try
        {
            var providerName = ModelCatalog.ProviderDisplayName(providerId);
            var baseUrl = ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl
                ?? ModelCatalog.All.FirstOrDefault(m => m.ProviderId == providerId)?.DefaultBaseUrl;
            var ctx = int.TryParse(context, out var c) ? c : 131_072;
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                modelId, display, providerName, providerId, "C", "Custom", ctx, 0, 0, baseUrl, L.Pick($"自定义导入 {display}", $"Custom import: {display}")));
            await DisplayAlertAsync(L.Pick("已导入", "Imported"),
                L.Pick($"模型「{display}」已加入供应商 {ModelCatalog.ProviderDisplayName(providerId)}。",
                       $"Model \"{display}\" was added to provider {ModelCatalog.ProviderDisplayName(providerId)}."),
                L.Pick("确定", "OK"));
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), ex.Message, L.Pick("关闭", "Close")); }
    }

    /// <summary>在线导入：选服务商端点（OpenCode Go/Zen、OpenRouter、Groq、SiliconFlow 等）→ 拉取 /models → 导入（对齐 Web/TUI）。</summary>
    private async Task ImportOnlineAsync()
    {
        var sources = ModelCli.OnlineSources;
        // 选项文案（数据 + 括号）与判据同源：只在这里拼一次，反查复用同一份数组（两处各拼一遍必然漂）
        var options = sources.Select(s => L.Pick($"{s.Name}（{s.KeyProvider}）", $"{s.Name} ({s.KeyProvider})")).ToArray();
        var cancel = L.Pick("取消", "Cancel");
        var chosen = await DisplayActionSheetAsync(L.Pick("🌐 在线导入 · 选择服务商", "🌐 Import online · Choose provider"), cancel, null, options);
        if (string.IsNullOrEmpty(chosen) || chosen == cancel) return;
        var idx = Array.IndexOf(options, chosen);
        if (idx < 0) return;
        var src = sources[idx];

        try
        {
            // 网络请求放后台线程，避免卡 UI
            var report = await Task.Run(() => ModelCli.ImportOnline(src));
            await DisplayAlertAsync(L.Pick($"在线导入 · {src.Name}", $"Online import · {src.Name}"), report, L.Pick("确定", "OK"));
            Populate();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(L.Pick("在线导入失败", "Online import failed"), ex.Message, L.Pick("关闭", "Close"));
        }
    }

    /// <summary>从配置文件导入（Claude Code / Codex / OpenCode / Crush / OpenClaw）：文件选择器 → 启发式解析模型/Key → 导入。</summary>
    private async Task ImportFromConfigFileAsync(string sourceName)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = L.Pick($"选择 {sourceName} 配置文件", $"Choose the {sourceName} config file") });
            if (result == null) return;
            var content = await File.ReadAllTextAsync(result.FullPath);

            // 启发式提取：模型 ID / API Key / baseUrl（AOT 安全正则）
            var modelIds = Regex.Matches(content, "(?i)\"model\"\\s*[:=]\\s*\"([^\"]+)\"|model\\s*=\\s*\"([^\"]+)\"|model\\s*=\\s*([a-zA-Z0-9_.-]+)")
                .Select(m => m.Groups[1].Value != "" ? m.Groups[1].Value : (m.Groups[2].Value != "" ? m.Groups[2].Value : m.Groups[3].Value))
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .ToList();
            var key = Regex.Match(content, "(?i)\"(api_key|apiKey|ANTHROPIC_API_KEY|OPENAI_API_KEY)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;
            var baseUrl = Regex.Match(content, "(?i)\"(base_url|baseUrl|api_base)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;

            var providerId = ModelCatalog.NormalizeProviderId(sourceName); // 规范化（去 api-/-ai），保证名称判断正确
            var providerName = sourceName;
            var imported = 0;
            foreach (var mid in modelIds.Take(10))
            {
                ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                    mid, mid, providerName, providerId, "C", "Custom", 131_072, 0, 0,
                    string.IsNullOrEmpty(baseUrl) ? null : baseUrl, L.Pick($"从 {sourceName} 导入", $"Imported from {sourceName}")));
                imported++;
            }
            if (!string.IsNullOrEmpty(key)) ApiKeyStore.Set(providerId, key);

            await DisplayAlertAsync(imported > 0 ? L.Pick("已导入", "Imported") : L.Pick("未识别", "Not recognized"),
                imported > 0
                    ? L.Pick($"从 {sourceName} 导入 {imported} 个模型", $"Imported {imported} model{(imported == 1 ? "" : "s")} from {sourceName}")
                      + (string.IsNullOrEmpty(key) ? "" : L.Pick(" + API Key。", " + API key."))
                    : L.Pick($"未能从该文件识别模型。{sourceName} 配置文件可能不含 model 字段。",
                             $"No models were recognized in that file. The {sourceName} config file may not contain a model field."),
                L.Pick("确定", "OK"));
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), ex.Message, L.Pick("关闭", "Close")); }
    }

    // ── 供应商 CRUD（设Key/改名/改地址/删除）──

    private async Task SetProviderKeyAsync(string providerId)
    {
        var key = await DisplayPromptAsync(L.Pick("设Key", "Set key"), L.Pick($"输入 {providerId} 的 API Key", $"API key for {providerId}"), accept: L.Pick("保存", "Save"), cancel: L.Pick("取消", "Cancel"), maxLength: 512);
        if (string.IsNullOrWhiteSpace(key)) return;
        // 合法性校验：只允许英文字母数字 + `+-_.` 逗号；环境变量引用（$VAR）会判非法
        if (!ApiKeyStore.IsValidApiKey(key))
        {
            await DisplayAlertAsync(L.Pick("Key 不合法", "Invalid key"),
                L.Pick("只允许英文字母数字 + - _ . ,（不要填环境变量引用 $VAR）",
                       "Only letters, digits and + - _ . , are allowed (do not use an environment variable reference like $VAR)"),
                L.Pick("确定", "OK"));
            return;
        }
        if (!ApiKeyStore.Set(providerId, key.Trim()))
        {
            await DisplayAlertAsync(L.Pick("保存失败", "Save failed"), L.Pick("Key 保存失败（请检查字符）", "Could not save the key (check the characters)"), L.Pick("确定", "OK"));
            return;
        }
        Populate();
        await DisplayAlertAsync(L.Pick("已保存", "Saved"),
            L.Pick($"已保存 {ModelCatalog.ProviderDisplayName(providerId)} 的 API Key",
                   $"Saved the API key for {ModelCatalog.ProviderDisplayName(providerId)}"), L.Pick("确定", "OK"));
    }

    private async Task RenameProviderAsync(string providerId)
    {
        var name = ModelCatalog.ProviderDisplayName(providerId);
        var newName = await DisplayPromptAsync(L.Pick("改名", "Rename"), L.Pick($"输入 {providerId} 的新显示名", $"New display name for {providerId}"),
            accept: L.Pick("保存", "Save"), cancel: L.Pick("取消", "Cancel"),
            initialValue: name, maxLength: 40);
        if (string.IsNullOrWhiteSpace(newName)) return;
        ModelCatalog.RenameProvider(providerId, newName.Trim());
        Populate();
    }

    private async Task EditProviderUrlAsync(string providerId)
    {
        var url = ModelCatalog.BaseUrlOf(providerId);
        var newUrl = await DisplayPromptAsync(L.Pick("改地址", "Change URL"), L.Pick($"输入 {providerId} 的 Base URL", $"Base URL for {providerId}"),
            accept: L.Pick("保存", "Save"), cancel: L.Pick("取消", "Cancel"),
            initialValue: url, maxLength: 200);
        if (newUrl == null) return;
        var urlErr = ModelCatalog.UpdateProviderUrlResult(providerId, newUrl.Trim());
        if (urlErr != null)
        {
            await DisplayAlertAsync(L.Pick("改地址失败", "Could not change the URL"), L.Pick("新", "New: ") + urlErr, L.Pick("关闭", "Close"));
            return;
        }
        Populate();
    }

    private async Task DeleteProviderAsync(string providerId)
    {
        var name = ModelCatalog.ProviderDisplayName(providerId);
        var confirmed = await DisplayAlertAsync(L.Pick("删除供应商", "Delete provider"),
            L.Pick($"确定删除 {name}（{providerId}）？此操作不可恢复。", $"Delete {name} ({providerId})? This cannot be undone."),
            L.Pick("删除", "Delete"), L.Pick("取消", "Cancel"));
        if (!confirmed) return;
        ModelCatalog.RemoveProvider(providerId);
        Populate();
    }
}
