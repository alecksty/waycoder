using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using WayCoder.UI.TUI.Custom;

namespace WayCoder.UI.Gui;

/// <summary>
/// 模型选择对话框（对齐 Web 模型 modal）：静态壳在 ModelWindow.axaml，搜索 + 供应商分组列表 + 底部操作。
/// 支持扫描连通性 / 自动导入 / OpenCode 在线导入 / 设置 key / 保存默认 / 切换当前槽位。
/// </summary>
public sealed partial class ModelWindow : Window
{
    private readonly MainWindow _owner;
    private bool _smallMode; // 当前目标通道：false=大模型（default 锚点）/ true=小模型；可经顶部 Tab 切换
    private Button _bigTabBtn = null!, _smallTabBtn = null!;
    private string _selectedId = "";
    private string _selectedProviderId = "";
    private string? _selectedBaseUrl;
    private Dictionary<string, ModelPicker.ScanStatus> _scanResult = new();
    private volatile bool _busy; // volatile：快速连点扫描/导入防并发执行

    /// <summary>仅在 Avalonia XAML 加载器/预览器路径使用；生产流程走带 owner 的构造。</summary>
    public ModelWindow()
    {
        InitializeComponent();
        _owner = null!;
    }

    public ModelWindow(MainWindow owner, bool smallMode = false)
    {
        InitializeComponent();
        _owner = owner;
        _smallMode = smallMode;
        Title = smallMode ? "🔧 选择小模型" : "🤖 选择大模型";
        // 预选该通道当前模型（与 Web/TUI 弹窗一致：打开即见当前大/小模型行高亮）
        var cfg = Config.Instance;
        _selectedId = smallMode ? cfg.SmallModel : cfg.Model;
        _selectedProviderId = smallMode ? cfg.SmallProvider : cfg.Provider;

        BuildModeTabs();
        BuildToolbar();
        BuildHeader();
        RenderList("");
    }

    /// <summary>顶部「大模型 | 小模型」分段切换（模型栏不再并列小模型入口，切小模型经此 Tab）。</summary>
    private void BuildModeTabs()
    {
        _bigTabBtn = UiKit.MakeButton("🤖 大模型", ButtonVariant.Default, () => SetMode(small: false));
        _smallTabBtn = UiKit.MakeButton("🔧 小模型", ButtonVariant.Default, () => SetMode(small: true));
        ModeTabs.Children.Add(_bigTabBtn);
        ModeTabs.Children.Add(_smallTabBtn);
        ApplyModeStyle();
    }

    /// <summary>切换目标通道（大/小），同步标题、Tab 高亮，并把预选行定位到该通道当前模型。</summary>
    private void SetMode(bool small)
    {
        _smallMode = small;
        Title = small ? "🔧 选择小模型" : "🤖 选择大模型";
        var cfg = Config.Instance;
        _selectedId = small ? cfg.SmallModel : cfg.Model;
        _selectedProviderId = small ? cfg.SmallProvider : cfg.Provider;
        _selectedBaseUrl = null; // 切换时经 ModelCatalog/注册表解析网关
        ApplyModeStyle();
        RenderList(SearchBox.Text ?? "");
    }

    /// <summary>把大/小两个 Tab 的高亮刷到与当前 _smallMode 一致（主题切换后经 ApplyTabActive 重取动态画刷）。</summary>
    private void ApplyModeStyle()
    {
        ApplyTabActive(_bigTabBtn, !_smallMode);
        ApplyTabActive(_smallTabBtn, _smallMode);
    }

    private static void ApplyTabActive(Button b, bool active)
    {
        b[!Button.BackgroundProperty] = new DynamicResourceExtension(active ? "AccentBrush" : "Panel2BgBrush");
        b[!Button.ForegroundProperty] = new DynamicResourceExtension(active ? "ButtonText" : "TextBrush");
        b.FontWeight = active ? FontWeight.Bold : FontWeight.Normal;
    }

    /// <summary>底部按钮行：左工具栏 + 右操作。</summary>
    private void BuildToolbar()
    {
        BtnRow.Children.Add(UiKit.MakeButton("📡 扫描", ButtonVariant.Default, () => Task.Run(ScanAsync)));
        BtnRow.Children.Add(UiKit.MakeButton("📥 本地导入", ButtonVariant.Default, async () => await ImportAsync()));
        BtnRow.Children.Add(UiKit.MakeButton("🌐 在线导入", ButtonVariant.Default, async () => await ImportOnlineAsync()));
        BtnRow.Children.Add(UiKit.MakeButton("🧹 清空", ButtonVariant.Default, async () => await ClearAllAsync()));
        BtnRow.Children.Add(UiKit.MakeButton("✏️ 编辑", ButtonVariant.Default, () => ShowEditModelDialog()));
        BtnRow.Children.Add(UiKit.MakeButton("🔑 设置 key", ButtonVariant.Default, () => ShowKeyDialog(set: true)));
        BtnRow.Children.Add(UiKit.MakeButton("🗑 清除 key", ButtonVariant.Default, () => ShowKeyDialog(set: false)));

        // 右侧操作（右对齐 spacer）
        var spacer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var rightRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        spacer.Children.Add(rightRow);
        // rightRow.Children.Add(UiKit.MakeButton("取消", ButtonVariant.Ghost, Close));
        rightRow.Children.Add(UiKit.MakeButton("💾 保存", ButtonVariant.Default,
            () => _owner.SaveDefaultModel(_selectedId, _smallMode, _selectedProviderId, _selectedBaseUrl)));
        rightRow.Children.Add(UiKit.MakeButton("切换模型", ButtonVariant.Accent, SwitchModel));
        BtnRow.Children.Add(spacer);
    }

    // ── 构建 ──

    private static void AddColumns(Grid grid)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("28")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("56")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("96")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("58")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("68")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("36")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("36")));
    }

    private static Grid NewModelGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        AddColumns(grid);
        return grid;
    }

    /// <summary>表头（加列 + 8 个列题）。</summary>
    private void BuildHeader()
    {
        AddColumns(HeaderGrid);
        AddCol(HeaderGrid, 0, "🔑", bold: true);
        AddCol(HeaderGrid, 1, "状态", bold: true);
        AddCol(HeaderGrid, 2, "模型", bold: true);
        AddCol(HeaderGrid, 3, "厂商", bold: true);
        AddCol(HeaderGrid, 4, "窗口", bold: true, alignRight: true);
        AddCol(HeaderGrid, 5, "价格", bold: true, alignRight: true);
        AddCol(HeaderGrid, 6, "大", bold: true, alignCenter: true);
        AddCol(HeaderGrid, 7, "小", bold: true, alignCenter: true);
    }

    private static void AddCol(Grid grid, int col, string text, bool bold = false, bool alignRight = false, bool alignCenter = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(4, 2),
        };
        if (alignRight) tb.HorizontalAlignment = HorizontalAlignment.Right;
        if (alignCenter) tb.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetColumn(tb, col);
        grid.Children.Add(tb);
    }

    private void Search_TextChanged(object? sender, TextChangedEventArgs e)
        => RenderList(SearchBox.Text ?? "");

    private void RenderList(string filter)
    {
        ListHost.Children.Clear();
        var f = filter.Trim().ToLowerInvariant();
        ModelCatalog.Invalidate();
        var models = ModelCatalog.All
            .Where(m => string.IsNullOrEmpty(f)
                || m.Id.Contains(f, StringComparison.OrdinalIgnoreCase)
                || m.DisplayName.Contains(f, StringComparison.OrdinalIgnoreCase)
                || m.ProviderId.Contains(f, StringComparison.OrdinalIgnoreCase))
            .GroupBy(m => m.ProviderId)
            .OrderBy(g => g.Key);

        var cfg = Config.Instance;
        foreach (var group in models)
        {
            // 组头
            var gname = new TextBlock
            {
                Text = ModelCatalog.ProviderDisplayName(group.Key),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(4, 8, 4, 2),
            };
            gname[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush");
            if (_scanResult.TryGetValue(group.Key, out var gs))
                gname.Text += gs == ModelPicker.ScanStatus.Connected ? "  ✅" : "  ❌";
            ListHost.Children.Add(gname);

            // 组内按模型 ID 排序（对齐 Web/TUI：供应商ID 分组 + 模型ID 排序）
            foreach (var m in group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                ListHost.Children.Add(BuildRow(m, cfg));
        }

        if (ListHost.Children.Count == 0)
            ListHost.Children.Add(new TextBlock { Text = "无匹配模型", Foreground = GuiColors.DimText, Margin = new Thickness(8, 12) });
    }

    private Border BuildRow(ModelCatalog.ModelInfo m, Config cfg)
    {
        var grid = NewModelGrid();
        bool isBig = m.Id == cfg.Model;
        bool isSmall = m.Id == cfg.SmallModel;
        bool selected = m.Id == _selectedId;

        // 🔑 key 标记
        var hasKey = ApiKeyStore.HasKeyFor(m.ProviderId, m.Id);
        var keyTb = new TextBlock { Text = hasKey ? "🔑" : "", FontSize = 11 };
        Grid.SetColumn(keyTb, 0);
        grid.Children.Add(keyTb);

        // 状态列（对齐 Web/TUI：无key / 连通 / 欠费 / 不通…，仅显示不落盘）
        var stTb = new TextBlock { Text = RowStatusText(m), FontSize = 11 };
        Grid.SetColumn(stTb, 1);
        grid.Children.Add(stTb);

        var nameTb = new TextBlock { Text = m.DisplayName, FontSize = 12.5, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        ToolTip.SetTip(nameTb, m.Id);
        Grid.SetColumn(nameTb, 2);
        grid.Children.Add(nameTb);

        var provTb = new TextBlock { Text = ModelCatalog.ProviderDisplayName(m.ProviderId), FontSize = 11.5, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(provTb, 3);
        grid.Children.Add(provTb);

        var ctxTb = new TextBlock { Text = Panels.FormatCtx(m.ContextWindow), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(ctxTb, 4);
        grid.Children.Add(ctxTb);

        var priceTb = new TextBlock { Text = WayCoder.UI.Shared.ModelPrice.Format(m.InputPrice, m.OutputPrice, m.InputPriceOffpeak, m.OutputPriceOffpeak), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(priceTb, 5);
        grid.Children.Add(priceTb);

        var bigTb = new TextBlock { Text = isBig ? "✓" : "", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(bigTb, 6);
        grid.Children.Add(bigTb);

        var smallTb = new TextBlock { Text = isSmall ? "✓" : "", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(smallTb, 7);
        grid.Children.Add(smallTb);

        var row = new Border
        {
            Child = grid,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(2, 3),
            Background = selected ? GuiColors.Panel2Bg : Brushes.Transparent,
            BorderBrush = selected ? GuiColors.Accent : null,
            BorderThickness = selected ? new Thickness(1) : new Thickness(0),
        };
        row.PointerPressed += (_, _) =>
        {
            _selectedId = m.Id;
            // 记录所选模型的网关地址 + 服务商（地址不同=不同服务商，请求走对应网关）
            _selectedProviderId = m.ProviderId;
            _selectedBaseUrl = m.DefaultBaseUrl;
            RenderList(SearchBox.Text ?? ""); // 重新渲染全部行以刷新高亮
        };
        return row;
    }

    /// <summary>行状态文本（与 Web/TUI 状态列一致）：无key / 连通 / 欠费 / 不通 / 未测 / 本地。</summary>
    private string RowStatusText(ModelCatalog.ModelInfo m)
    {
        if (m.ProviderId is "local" or "custom")
            return _scanResult.TryGetValue(m.ProviderId, out var ls) && ls == ModelPicker.ScanStatus.Connected ? "✔本地" : "本地";
        if (!ApiKeyStore.HasKeyFor(m.ProviderId, m.Id)) return "无key";
        if (!_scanResult.TryGetValue(m.ProviderId, out var st)) return "未测";
        // ScanStatus → 中文映射收敛到核心 ModelPicker.StatusText（本地/无key/未测特判保留在上方）
        return ModelPicker.StatusText(st);
    }

    // ── 操作 ──

    private void SwitchModel()
    {
        if (string.IsNullOrEmpty(_selectedId))
        {
            StatusText.Text = "请先选择一个模型";
            return;
        }
        if (_smallMode)
        {
            // ApplySmallModel 经 connect 统一入口设置小模型（含 provider/baseUrl 持久化）
            _owner.ApplySmallModel(_selectedId, _selectedProviderId, _selectedBaseUrl);
        }
        else
        {
            _owner.ApplyModel(_selectedId, _selectedProviderId, _selectedBaseUrl);
        }
        Close();
    }

    private async void ShowKeyDialog(bool set)
    {
        var m = ModelCatalog.Find(_selectedId);
        if (m == null) { StatusText.Text = "请先选择一个模型"; return; }
        if (m.ProviderId is "local" or "custom") { StatusText.Text = "本地模型无需 API Key"; return; }

        var title = set ? "设置 API Key" : "清除 API Key";
        var label = $"为 {ModelCatalog.ProviderDisplayName(m.ProviderId)} 设置 API Key（保存后该供应商所有模型可用）：";
        var input = await UiKit.ShowPrompt(this, title, label, set ? (ApiKeyStore.Get(m.ProviderId) ?? "") : "");
        if (input == null) return;
        if (set) ApiKeyStore.Set(m.ProviderId, input.Trim());
        else ApiKeyStore.Remove(m.ProviderId);
        RenderList(SearchBox.Text ?? "");
    }

    /// <summary>编辑单个模型（两层架构：服务商/地址/APIKey/模型/上下文/价格）。</summary>
    private void ShowEditModelDialog()
    {
        if (string.IsNullOrEmpty(_selectedId)) { StatusText.Text = "请先选择一个模型"; return; }
        var info = ModelCatalog.Find(_selectedId);
        var win = UiKit.NewDialog(this, "✏️ 编辑模型", 440);
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        TextBox id, prov, url, key, ctx, price;
        void Row(string label, out TextBox box)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            sp.Children.Add(new TextBlock { Text = label, Width = 100, VerticalAlignment = VerticalAlignment.Center, Foreground = GuiColors.Text });
            box = new TextBox { MinWidth = 260 };
            sp.Children.Add(box);
            panel.Children.Add(sp);
        }
        Row("模型 ID", out id); id.Text = _selectedId;
        Row("服务商", out prov); prov.Text = _selectedProviderId;
        Row("地址", out url); url.Text = _selectedBaseUrl ?? "";
        Row("API Key", out key); key.Text = ApiKeyStore.Get(_selectedProviderId) ?? ""; key.PasswordChar = '•';
        Row("上下文", out ctx); ctx.Text = (info?.ContextWindow ?? 0).ToString();
        Row("价格 ($/MTok)", out price); price.Text = (info?.InputPrice ?? 0).ToString("0.##");
        var btns = UiKit.ButtonRow(
            UiKit.MakeButton("保存", ButtonVariant.Primary, () =>
            {
                var pid = string.IsNullOrWhiteSpace(prov.Text) ? "custom" : prov.Text.Trim();
                var apiKey = key.Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(apiKey)) ApiKeyStore.Set(pid, apiKey); // key 按服务商存
                int ctxV = int.TryParse(ctx.Text, out var c) ? c : 0;
                double priceV = double.TryParse(price.Text, out var p) ? p : 0;
                var mid = id.Text?.Trim() ?? _selectedId;
                ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                    mid, mid, pid, pid, "*", "Custom", ctxV, priceV, 0,
                    string.IsNullOrWhiteSpace(url.Text) ? null : url.Text?.Trim(), "手动编辑", 0));
                win.Close();
                RenderList(SearchBox.Text ?? "");
            }),
            UiKit.MakeButton("取消", ButtonVariant.Secondary, win.Close));
        panel.Children.Add(btns);
        win.Content = panel;
        win.ShowDialog(this);
    }

    private async Task ScanAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => StatusText.Text = "扫描连通性…");
        try
        {
            var probes = await Task.Run(ModelCli.TestList);
            var dict = new Dictionary<string, ModelPicker.ScanStatus>();
            foreach (var p in probes) dict[p.ProviderId] = ModelPicker.ProbeStatus(p);
            Dispatcher.UIThread.Post(() =>
            {
                _scanResult = dict;
                var ok = dict.Count(x => x.Value == ModelPicker.ScanStatus.Connected);
                StatusText.Text = $"{ok} 连通 / {dict.Count - ok} 不通";
                RenderList(SearchBox.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => StatusText.Text = "扫描失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    /// <summary>本地导入：弹来源勾选框（与 Web/TUI 一致），导入所选来源模型 + API Key。</summary>
    private async Task ImportAsync()
    {
        if (_busy) return;
        var options = new (string Key, string Label)[]
        {
            ("builtin", "内置模型（恢复被清空的内置目录）"),
            ("claudecode", "Claude Code（~/.claude/settings.json）"),
            ("codex", "Codex（~/.codex/config.toml）"),
            ("opencode", "OpenCode（~/.config/opencode）"),
            ("crush", "Crush（~/.config/crush）"),
            ("openclaw", "OpenClaw（~/.openclaw）"),
            ("ollama", "Ollama（本地接口实时拉取）"),
            ("lmstudio", "LM Studio（本地接口实时拉取）"),
            ("cc-switch", "CC Switch（本地路由实时拉取）"),
        };
        var picked = await ShowMultiCheckAsync("📥 本地导入 · 选择来源", options, preCheckAll: true);
        if (picked == null || picked.Count == 0) return; // 取消 / 未勾选
        var sources = string.Join(",", picked.Select(x => x.Key));
        _busy = true;
        Dispatcher.UIThread.Post(() => StatusText.Text = "本地导入中…");
        try
        {
            var report = await Task.Run(() =>
            {
                // 本地导入只导模型；key 仅由 api_keys.json + 环境变量决定（不自动同步来源文件的 key）
                // 本地服务（Ollama/LM Studio）从本地官方接口实时拉取真实模型；其余从第三方库导入
                bool IsLocalService(string s) => s.Equals("ollama", StringComparison.OrdinalIgnoreCase)
                    || s.Equals("lmstudio", StringComparison.OrdinalIgnoreCase)
                    || s.Equals("cc-switch", StringComparison.OrdinalIgnoreCase);
                var hasLocalService = sources.Split(',').Any(IsLocalService);
                string r;
                if (hasLocalService)
                {
                    var nonLocal = string.Join(",", sources.Split(',').Select(s => s.Trim()).Where(s =>
                        s.Length > 0 && !IsLocalService(s)));
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(nonLocal)) parts.Add(ModelCli.Import(nonLocal).Trim());
                    parts.Add(ModelCli.ImportLocalServices().Trim());
                    r = string.Join("\n", parts);
                }
                else
                {
                    r = ModelCli.Import(sources);
                }
                ModelCatalog.Invalidate();
                ApiKeyStore.ClearCache();
                return r;
            });
            Dispatcher.UIThread.Post(() =>
            {
                StatusText.Text = report;
                ModelCatalog.Invalidate();
                RenderList(SearchBox.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => StatusText.Text = "导入失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    /// <summary>在线导入：选择 OpenCode Go / Zen，用对应地址拉取。</summary>
    private async Task ImportOnlineAsync()
    {
        if (_busy) return;
        var onlineOptions = ModelCli.OnlineSources.Select(s => (s.Name, s.Name)).ToArray();
        var picked = await ShowMultiCheckAsync("🌐 在线导入 · 选择服务商", onlineOptions, preCheckAll: false);
        if (picked == null || picked.Count == 0) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => StatusText.Text = "在线导入中…");
        try
        {
            var sb = new StringBuilder();
            await Task.Run(() =>
            {
                foreach (var (name, _) in picked)
                {
                    var src = ModelCli.OnlineSources.FirstOrDefault(s => s.Name == name);
                    if (src != null)
                    {
                        // 进度实时上屏（拉取→解析→写入→完成），防「卡死感」；跨线程回 UI 线程更新 StatusText
                        var r = ModelCli.ImportOnline(src, msg =>
                            Dispatcher.UIThread.Post(() => StatusText.Text = msg));
                        sb.AppendLine(r);
                    }
                }
            });
            Dispatcher.UIThread.Post(() =>
            {
                StatusText.Text = sb.ToString();
                ModelCatalog.Invalidate();
                RenderList(SearchBox.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => StatusText.Text = "在线导入失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    /// <summary>清空全部模型（内置目录 + 自定义），确认后清空可重新导入。</summary>
    private async Task ClearAllAsync()
    {
        if (_busy) return;
        if (!await UiKit.ShowConfirm(this, "🧹 清空全部模型",
            "确定清空全部模型？内置目录与已导入的自定义模型都会移除，可清空后重新导入。")) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => StatusText.Text = "清空中…");
        try
        {
            var n = await Task.Run(() => ModelCatalog.ClearAll());
            Dispatcher.UIThread.Post(() =>
            {
                StatusText.Text = $"🗑 已清空全部模型（删除 {n} 个自定义模型文件，内置目录已隐藏）";
                ModelCatalog.Invalidate();
                RenderList(SearchBox.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => StatusText.Text = "清空失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    // ── 辅助对话框 ──

    /// <summary>多选勾选对话框（CheckBox 列表，默认全选），返回选中的 (Key, Label)，取消返回 null。</summary>
    private async Task<List<(string Key, string Label)>?> ShowMultiCheckAsync(string title, (string Key, string Label)[] options, bool preCheckAll = false)
    {
        var tcs = new TaskCompletionSource<List<(string, string)>?>();
        var dlg = UiKit.NewDialog(this, title, 440, 380);
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        var list = new StackPanel { Spacing = 6 };
        var checks = new List<CheckBox>();
        foreach (var (key, label) in options)
            checks.Add(new CheckBox { Content = label, IsChecked = preCheckAll });
        foreach (var c in checks) list.Children.Add(c);
        panel.Children.Add(list);
        panel.Children.Add(UiKit.ButtonRow(
            UiKit.MakeButton("导入所选", ButtonVariant.Primary, () =>
            {
                tcs.TrySetResult(options.Where((_, i) => checks[i].IsChecked == true).ToList());
                dlg.Close();
            }),
            UiKit.MakeButton("取消", ButtonVariant.Secondary, () => { tcs.TrySetResult(null); dlg.Close(); })));
        dlg.Content = panel;
        dlg.Closed += (_, _) => tcs.TrySetResult(null);
        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    /// <summary>单选下拉对话框，返回选中项 label，取消返回 null。</summary>
    private Task<string?> ShowSelectAsync(string title, string[] options)
        => UiKit.ShowSelect(this, title, options);
}
