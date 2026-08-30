using System.Net.Http;
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
            var display = ModelCatalog.Providers.TryGetValue(g.Key, out var p) ? p.DisplayName : g.Key;
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
                var action = await DisplayActionSheetAsync($"{display}（{g.Key}）", "取消", null,
                    "管理模型", "设Key", "清Key", "改名", "改地址", "删除");
                switch (action)
                {
                    case "管理模型":
                        await Shell.Current.GoToAsync($"providermodels?provider={Uri.EscapeDataString(g.Key)}");
                        break;
                    case "设Key":
                        await SetProviderKeyAsync(g.Key);
                        break;
                    case "清Key":
                        ApiKeyStore.Remove(g.Key);
                        Populate();
                        break;
                    case "改名":
                        await RenameProviderAsync(g.Key);
                        break;
                    case "改地址":
                        await EditProviderUrlAsync(g.Key);
                        break;
                    case "删除":
                        await DeleteProviderAsync(g.Key);
                        break;
                }
            };
            card.GestureRecognizers.Add(tap);

            var models = g.OrderBy(m => m.DisplayName).Select(m => m.DisplayName).ToList();
            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(new Label
            {
                Text = $"{icon} {display}（{g.Key}）· {g.Count()} 模型 · {(conn == true ? "✅ 可达" : conn == false ? "❌ 不可达" : "未扫描")}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#1A1A1A"),
            });
            stack.Add(new Label
            {
                Text = string.IsNullOrEmpty(baseUrl) ? "无默认地址" : baseUrl,
                FontSize = 11,
                TextColor = Color.FromArgb("#888888"),
                LineBreakMode = LineBreakMode.TailTruncation,
            });
            stack.Add(new Label
            {
                Text = string.Join("、", models.Take(6)) + (models.Count > 6 ? $" 等 {models.Count} 个" : ""),
                FontSize = 11,
                TextColor = Color.FromArgb("#777777"),
                LineBreakMode = LineBreakMode.TailTruncation,
            });
            card.Content = stack;
            ProviderList.Add(card);
        }

        if (byProvider.Count() == 0)
            ProviderList.Add(new Label { Text = "暂无供应商", FontSize = 13, TextColor = Color.FromArgb("#888888") });
    }

    /// <summary>扫描全部供应商连通性（HTTP GET 默认地址，3s 超时）。</summary>
    private async void OnScanClicked(object? sender, EventArgs e)
    {
        ScanBtn.IsEnabled = false;
        ScanBtn.Text = "扫描中…";
        var providers = ModelCatalog.Providers
            .Where(kv => kv.Key is not ("local" or "custom"))
            .Select(kv => (Id: kv.Key, Url: kv.Value.DefaultBaseUrl))
            .ToList();
        // 自定义供应商地址取自定义模型的 DefaultBaseUrl
        foreach (var custom in ModelCatalog.ListCustom().Where(m => !string.IsNullOrEmpty(m.DefaultBaseUrl)))
        {
            if (providers.All(x => x.Id != custom.ProviderId))
                providers.Add((custom.ProviderId, custom.DefaultBaseUrl));
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        foreach (var p in providers)
        {
            var ok = false;
            if (!string.IsNullOrEmpty(p.Url))
            {
                try { using var _ = await http.GetAsync(p.Url); ok = true; }
                catch { ok = false; }
            }
            _connectivity[p.Id] = ok;
        }
        ScanBtn.IsEnabled = true;
        ScanBtn.Text = "📡 扫描";
        Populate();
    }

    /// <summary>导入供应商：名称 + 接口地址 + Key（生成一个占位模型使供应商出现，再导入真实模型）。</summary>
    private async void OnAddProviderClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("导入供应商", "供应商名称（如：硅基流动）", accept: "下一步", maxLength: 30);
        if (string.IsNullOrWhiteSpace(name)) return;
        var id = await DisplayPromptAsync("导入供应商", "供应商 ID（小写英文，如 siliconflow）", accept: "下一步", maxLength: 30);
        if (string.IsNullOrWhiteSpace(id)) return;
        var baseUrl = await DisplayPromptAsync("导入供应商", "接口地址 BaseUrl（OpenAI 兼容，如 https://api.siliconflow.cn/v1）", accept: "下一步", maxLength: 200);
        if (string.IsNullOrWhiteSpace(baseUrl)) return;
        var key = await DisplayPromptAsync("导入供应商", "API Key（可留空稍后在设置填写）", accept: "完成", maxLength: 200);

        try
        {
            // 注册到 providers.json（/provider 列表可见）+ 占位模型（供应商卡片列表可见）
            if (!ModelCatalog.RegisterProvider(id, name, baseUrl))
            {
                var owner = ModelCatalog.FindProviderByBaseUrl(baseUrl);
                await DisplayAlertAsync("导入失败", $"地址已被服务商「{owner}」占用（同地址 = 同供应商，不允许重复）", "关闭");
                return;
            }
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                $"{id}-placeholder", $"{name} 占位", name, id, "C", "Custom", 131_072, 0, 0, baseUrl,
                $"自定义供应商 {name}（导入后请在「设置」填 Key）"));
            if (!string.IsNullOrWhiteSpace(key)) ApiKeyStore.Set(id, key);
            _connectivity[id] = null;
            await DisplayAlertAsync("已导入", $"供应商「{name}」已添加。再点「＋ 导入模型」添加真实模型，或在「设置」填 Key。", "确定");
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync("导入失败", ex.Message, "关闭"); }
    }

    /// <summary>导入模型：多来源选择（对标 Web 版）——内置 / Claude Code / Codex / OpenCode / Crush / OpenClaw / 自定义。</summary>
    private async void OnAddModelClicked(object? sender, EventArgs e)
    {
        var source = await DisplayActionSheetAsync("导入模型 · 选择来源", "取消", null,
            "🌐 在线导入", "内置模型目录", "Claude Code", "Codex", "OpenCode", "Crush", "OpenClaw", "自定义添加");
        switch (source)
        {
            case "🌐 在线导入":
                await ImportOnlineAsync();
                break;
            case "内置模型目录":
                await DisplayAlertAsync("内置模型",
                    $"内置目录已含 {ModelCatalog.All.Length} 个模型（DeepSeek/Qwen/Zhipu/OpenAI/Anthropic/AIHubMix 等），无需导入。", "确定");
                break;
            case "Claude Code":
            case "Codex":
            case "OpenCode":
            case "Crush":
            case "OpenClaw":
                await ImportFromConfigFileAsync(source);
                break;
            case "自定义添加":
                await AddCustomModelAsync();
                break;
        }
    }

    /// <summary>自定义添加模型：供应商 ID + 模型 ID + 显示名 + 上下文。</summary>
    private async Task AddCustomModelAsync()
    {
        var providerId = await DisplayPromptAsync("自定义添加模型", "所属供应商 ID（如 siliconflow / deepseek）", accept: "下一步", maxLength: 30, initialValue: Config.Instance.Provider);
        if (string.IsNullOrWhiteSpace(providerId)) return;
        var modelId = await DisplayPromptAsync("自定义添加模型", "模型 ID（API 调用用，如 deepseek-chat）", accept: "下一步", maxLength: 60);
        if (string.IsNullOrWhiteSpace(modelId)) return;
        var display = await DisplayPromptAsync("自定义添加模型", "显示名（如 DeepSeek Chat）", accept: "下一步", maxLength: 40, initialValue: modelId);
        if (string.IsNullOrWhiteSpace(display)) return;
        var context = await DisplayPromptAsync("自定义添加模型", "上下文窗口（token，如 32768）", accept: "完成", initialValue: "131072", maxLength: 10);

        try
        {
            var providerName = ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId;
            var baseUrl = ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl
                ?? ModelCatalog.All.FirstOrDefault(m => m.ProviderId == providerId)?.DefaultBaseUrl;
            var ctx = int.TryParse(context, out var c) ? c : 131_072;
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                modelId, display, providerName, providerId, "C", "Custom", ctx, 0, 0, baseUrl, $"自定义导入 {display}"));
            await DisplayAlertAsync("已导入", $"模型「{display}」已加入供应商 {providerId}。", "确定");
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync("导入失败", ex.Message, "关闭"); }
    }

    /// <summary>在线导入：选服务商端点（OpenCode Go/Zen、OpenRouter、Groq、SiliconFlow 等）→ 拉取 /models → 导入（对齐 Web/TUI）。</summary>
    private async Task ImportOnlineAsync()
    {
        var sources = ModelCli.OnlineSources;
        var options = sources.Select(s => $"{s.Name}（{s.KeyProvider}）").ToArray();
        var chosen = await DisplayActionSheetAsync("🌐 在线导入 · 选择服务商", "取消", null, options);
        if (string.IsNullOrEmpty(chosen) || chosen == "取消") return;
        var src = sources.First(s => $"{s.Name}（{s.KeyProvider}）" == chosen);

        try
        {
            // 网络请求放后台线程，避免卡 UI
            var report = await Task.Run(() => ModelCli.ImportOnline(src));
            await DisplayAlertAsync($"在线导入 · {src.Name}", report, "确定");
            Populate();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("在线导入失败", ex.Message, "关闭");
        }
    }

    /// <summary>从配置文件导入（Claude Code / Codex / OpenCode / Crush / OpenClaw）：文件选择器 → 启发式解析模型/Key → 导入。</summary>
    private async Task ImportFromConfigFileAsync(string sourceName)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = $"选择 {sourceName} 配置文件" });
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

            var providerId = sourceName.ToLowerInvariant().Replace(" ", "");
            var providerName = sourceName;
            var imported = 0;
            foreach (var mid in modelIds.Take(10))
            {
                ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                    mid, mid, providerName, providerId, "C", "Custom", 131_072, 0, 0,
                    string.IsNullOrEmpty(baseUrl) ? null : baseUrl, $"从 {sourceName} 导入"));
                imported++;
            }
            if (!string.IsNullOrEmpty(key)) ApiKeyStore.Set(providerId, key);

            await DisplayAlertAsync(imported > 0 ? "已导入" : "未识别",
                imported > 0
                    ? $"从 {sourceName} 导入 {imported} 个模型" + (string.IsNullOrEmpty(key) ? "" : " + API Key。")
                    : $"未能从该文件识别模型。{sourceName} 配置文件可能不含 model 字段。", "确定");
            Populate();
        }
        catch (Exception ex) { await DisplayAlertAsync("导入失败", ex.Message, "关闭"); }
    }

    // ── 供应商 CRUD（设Key/改名/改地址/删除）──

    private async Task SetProviderKeyAsync(string providerId)
    {
        var key = await DisplayPromptAsync("设Key", $"输入 {providerId} 的 API Key", accept: "保存", cancel: "取消", maxLength: 512);
        if (string.IsNullOrWhiteSpace(key)) return;
        // 合法性校验：只允许英文字母数字 + `+-_.` 逗号；环境变量引用（$VAR）会判非法
        if (!ApiKeyStore.IsValidApiKey(key))
        {
            await DisplayAlertAsync("Key 不合法",
                "只允许英文字母数字 + - _ . ,（不要填环境变量引用 $VAR）", "确定");
            return;
        }
        if (!ApiKeyStore.Set(providerId, key.Trim()))
        {
            await DisplayAlertAsync("保存失败", "Key 保存失败（请检查字符）", "确定");
            return;
        }
        Populate();
        await DisplayAlertAsync("已保存", $"已保存 {providerId} 的 API Key", "确定");
    }

    private async Task RenameProviderAsync(string providerId)
    {
        var name = ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId;
        var newName = await DisplayPromptAsync("改名", $"输入 {providerId} 的新显示名", accept: "保存", cancel: "取消",
            initialValue: name, maxLength: 40);
        if (string.IsNullOrWhiteSpace(newName)) return;
        ModelCatalog.RenameProvider(providerId, newName.Trim());
        Populate();
    }

    private async Task EditProviderUrlAsync(string providerId)
    {
        var url = ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DefaultBaseUrl : "";
        var newUrl = await DisplayPromptAsync("改地址", $"输入 {providerId} 的 Base URL", accept: "保存", cancel: "取消",
            initialValue: url, maxLength: 200);
        if (newUrl == null) return;
        if (!ModelCatalog.UpdateProviderUrl(providerId, newUrl.Trim()))
        {
            var owner = ModelCatalog.FindProviderByBaseUrl(newUrl.Trim());
            await DisplayAlertAsync("改地址失败", $"新地址已被服务商「{owner}」占用（同地址 = 同供应商，不允许重复）", "关闭");
            return;
        }
        Populate();
    }

    private async Task DeleteProviderAsync(string providerId)
    {
        var name = ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId;
        var confirmed = await DisplayAlertAsync("删除供应商", $"确定删除 {name}（{providerId}）？此操作不可恢复。", "删除", "取消");
        if (!confirmed) return;
        ModelCatalog.RemoveProvider(providerId);
        Populate();
    }
}
