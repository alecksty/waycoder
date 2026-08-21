using System.Net.Http;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using WayCoder.UI.TUI.Custom;

namespace WayCoder.UI.Gui;

/// <summary>
/// 模型选择对话框（对齐 Web 模型 modal）：搜索 + 供应商分组 7 列表格 + 底部操作。
/// 支持扫描连通性 / 自动导入 / OpenCode 在线导入 / 设置 key / 保存默认 / 切换当前槽位。
/// </summary>
public sealed class ModelWindow : Window
{
    private readonly MainWindow _owner;
    private readonly bool _smallMode;
    private readonly TextBox _search = new();
    private readonly StackPanel _listHost = new() { Spacing = 2 };
    private readonly TextBlock _status = new();
    private string _selectedId = "";
    private string _selectedProviderId = "";
    private string? _selectedBaseUrl;
    private Dictionary<string, ModelPicker.ScanStatus> _scanResult = new();
    private volatile bool _busy; // volatile：快速连点扫描/导入防并发执行

    public ModelWindow(MainWindow owner, bool smallMode = false)
    {
        _owner = owner;
        _smallMode = smallMode;
        Title = smallMode ? "🔧 选择小模型" : "🤖 选择大模型";
        Width = 860;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        this[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");

        var root = new DockPanel { Margin = new Thickness(16) };

        // ── 底部按钮行 ──
        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 10, 0, 0),
        };
        DockPanel.SetDock(btnRow, Dock.Bottom);
        btnRow.Children.Add(MakeBtn("📡 扫描", () => Task.Run(ScanAsync)));
        btnRow.Children.Add(MakeBtn("📥 本地导入", async () => await ImportAsync()));
        btnRow.Children.Add(MakeBtn("🌐 在线导入", async () => await ImportOnlineAsync()));
        btnRow.Children.Add(MakeBtn("🧹 清空", async () => await ClearAllAsync()));
        btnRow.Children.Add(MakeBtn("✏️ 编辑", () => ShowEditModelDialog()));
        btnRow.Children.Add(MakeBtn("🔑 设置 key", () => ShowKeyDialog(set: true)));
        btnRow.Children.Add(MakeBtn("🗑 清除 key", () => ShowKeyDialog(set: false)));
        var spacer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Children.Add(spacer);
        var rightRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        spacer.Children.Add(rightRow);
        rightRow.Children.Add(MakeBtn("取消", Close, ghost: true));
        rightRow.Children.Add(MakeBtn("💾 保存", () => { _owner.SaveDefaultModel(_selectedId, _smallMode, _selectedProviderId, _selectedBaseUrl); }));
        rightRow.Children.Add(MakeBtn("切换模型", SwitchModel, accent: true));
        root.Children.Add(btnRow);

        // ── 状态行 ──
        _status.FontSize = 12;
        _status[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush");
        _status.Margin = new Thickness(0, 8, 0, 0);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(_status);

        // ── 搜索 ──
        _search.PlaceholderText = "搜索模型名称 / 厂商…";
        _search.Margin = new Thickness(0, 0, 0, 8);
        _search.TextChanged += (_, _) => RenderList(_search.Text ?? "");
        DockPanel.SetDock(_search, Dock.Top);
        root.Children.Add(_search);

        // ── 表头 ──
        var head = BuildHeader();
        DockPanel.SetDock(head, Dock.Top);
        root.Children.Add(head);

        // ── 列表 ──
        var scroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _listHost };
        root.Children.Add(scroller);

        Content = root;
        RenderList("");
    }

    // ── 构建 ──

    private static Grid BuildHeader()
    {
        var grid = NewModelGrid();
        AddCol(grid, 0, "🔑", bold: true);
        AddCol(grid, 1, "状态", bold: true);
        AddCol(grid, 2, "模型", bold: true);
        AddCol(grid, 3, "厂商", bold: true);
        AddCol(grid, 4, "窗口", bold: true, alignRight: true);
        AddCol(grid, 5, "价格", bold: true, alignRight: true);
        AddCol(grid, 6, "大", bold: true, alignCenter: true);
        AddCol(grid, 7, "小", bold: true, alignCenter: true);
        return grid;
    }

    private static Grid NewModelGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("28")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("56")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("96")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("58")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("68")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("36")));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("36")));
        return grid;
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

    private void RenderList(string filter)
    {
        _listHost.Children.Clear();
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
                Text = group.Key,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(4, 8, 4, 2),
            };
            gname[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush");
            if (_scanResult.TryGetValue(group.Key, out var gs))
                gname.Text += gs == ModelPicker.ScanStatus.Connected ? "  ✅" : "  ❌";
            _listHost.Children.Add(gname);

            foreach (var m in group)
                _listHost.Children.Add(BuildRow(m, cfg));
        }

        if (_listHost.Children.Count == 0)
            _listHost.Children.Add(new TextBlock { Text = "无匹配模型", Foreground = Brushes.Gray, Margin = new Thickness(8, 12) });
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
        var stTb = new TextBlock { Text = StatusText(m), FontSize = 11 };
        Grid.SetColumn(stTb, 1);
        grid.Children.Add(stTb);

        var nameTb = new TextBlock { Text = m.DisplayName, FontSize = 12.5, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        ToolTip.SetTip(nameTb, m.Id);
        Grid.SetColumn(nameTb, 2);
        grid.Children.Add(nameTb);

        var provTb = new TextBlock { Text = m.Provider, FontSize = 11.5, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(provTb, 3);
        grid.Children.Add(provTb);

        var ctxTb = new TextBlock { Text = Panels.FormatCtx(m.ContextWindow), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(ctxTb, 4);
        grid.Children.Add(ctxTb);

        var priceTb = new TextBlock { Text = Panels.FormatPrice(m.InputPrice), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
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
            Background = selected ? new SolidColorBrush(Color.Parse("#1d2230")) : Brushes.Transparent,
            BorderBrush = selected ? new SolidColorBrush(Color.Parse("#4f8cff")) : null,
            BorderThickness = selected ? new Thickness(1) : new Thickness(0),
        };
        row.PointerPressed += (_, _) =>
        {
            _selectedId = m.Id;
            // 记录所选模型的网关地址 + 服务商（地址不同=不同服务商，请求走对应网关）
            _selectedProviderId = m.ProviderId;
            _selectedBaseUrl = m.DefaultBaseUrl;
            // 重新渲染全部行以刷新高亮
            RenderList(_search.Text ?? "");
        };
        return row;
    }

    /// <summary>行状态文本（与 Web/TUI 状态列一致）：无key / 连通 / 欠费 / 不通 / 未测 / 本地。</summary>
    private string StatusText(ModelCatalog.ModelInfo m)
    {
        if (m.ProviderId is "local" or "custom")
            return _scanResult.TryGetValue(m.ProviderId, out var ls) && ls == ModelPicker.ScanStatus.Connected ? "✔本地" : "本地";
        if (!ApiKeyStore.HasKeyFor(m.ProviderId, m.Id)) return "无key";
        if (!_scanResult.TryGetValue(m.ProviderId, out var st)) return "未测";
        return st switch
        {
            ModelPicker.ScanStatus.Connected => "✔连通",
            ModelPicker.ScanStatus.BadKey => "✖key",
            ModelPicker.ScanStatus.Overdue => "欠费",
            ModelPicker.ScanStatus.NoEndpoint => "无端点",
            ModelPicker.ScanStatus.Unreachable => "✖不通",
            _ => "未测",
        };
    }

    // ── 操作 ──

    private void SwitchModel()
    {
        if (string.IsNullOrEmpty(_selectedId))
        {
            _status.Text = "请先选择一个模型";
            return;
        }
        if (_smallMode)
        {
            var cfg = Config.Instance;
            cfg.SmallModel = _selectedId;
            cfg.SaveToEnvFile();
            _owner.ApplySmallModel(_selectedId, _selectedProviderId, _selectedBaseUrl);
        }
        else
        {
            _owner.ApplyModel(_selectedId, _selectedProviderId, _selectedBaseUrl);
        }
        Close();
    }

    private void ShowKeyDialog(bool set)
    {
        var m = ModelCatalog.Find(_selectedId);
        if (m == null) { _status.Text = "请先选择一个模型"; return; }
        if (m.ProviderId is "local" or "custom") { _status.Text = "本地模型无需 API Key"; return; }

        var win = new Window
        {
            Title = set ? "设置 API Key" : "清除 API Key",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#171a23")),
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = $"为 {m.ProviderId} 设置 API Key（保存后该供应商所有模型可用）：",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")),
        });
        var box = new TextBox { Text = set ? (ApiKeyStore.Get(m.ProviderId) ?? "") : "", PasswordChar = '•' };
        panel.Children.Add(box);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(MakeBtn("确定", () =>
        {
            if (set) ApiKeyStore.Set(m.ProviderId, box.Text.Trim());
            else ApiKeyStore.Remove(m.ProviderId);
            win.Close();
            RenderList(_search.Text ?? "");
        }));
        btns.Children.Add(MakeBtn("取消", win.Close, ghost: true));
        panel.Children.Add(btns);
        win.Content = panel;
        win.ShowDialog(this);
    }

    /// <summary>编辑单个模型（两层架构：服务商/地址/APIKey/模型/上下文/价格）。</summary>
    private void ShowEditModelDialog()
    {
        if (string.IsNullOrEmpty(_selectedId)) { _status.Text = "请先选择一个模型"; return; }
        var info = ModelCatalog.Find(_selectedId);
        var win = new Window
        {
            Title = "✏️ 编辑模型",
            Width = 440,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#171a23")),
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        TextBox id, prov, url, key, ctx, price;
        void Row(string label, out TextBox box)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            sp.Children.Add(new TextBlock { Text = label, Width = 100, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) });
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
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(MakeBtn("保存", () =>
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
            RenderList(_search.Text ?? "");
        }));
        btns.Children.Add(MakeBtn("取消", win.Close, ghost: true));
        panel.Children.Add(btns);
        win.Content = panel;
        win.ShowDialog(this);
    }

    private async Task ScanAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "扫描连通性…");
        try
        {
            var probes = await Task.Run(ModelCli.TestList);
            var dict = new Dictionary<string, ModelPicker.ScanStatus>();
            foreach (var p in probes) dict[p.ProviderId] = ModelPicker.ProbeStatus(p);
            Dispatcher.UIThread.Post(() =>
            {
                _scanResult = dict;
                var ok = dict.Count(x => x.Value == ModelPicker.ScanStatus.Connected);
                _status.Text = $"{ok} 连通 / {dict.Count - ok} 不通";
                RenderList(_search.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => _status.Text = "扫描失败: " + ex.Message);
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
        Dispatcher.UIThread.Post(() => _status.Text = "本地导入中…");
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
                _status.Text = report;
                ModelCatalog.Invalidate();
                RenderList(_search.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => _status.Text = "导入失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    /// <summary>在线导入：选择 OpenCode Go（zen/go/v1 订阅）/ Zen（zen/v1 按量），用对应地址拉取。</summary>
    private async Task ImportOnlineAsync()
    {
        if (_busy) return;
        var onlineOptions = ModelCli.OnlineSources.Select(s => (s.Name, s.Name)).ToArray();
        var picked = await ShowMultiCheckAsync("🌐 在线导入 · 选择服务商", onlineOptions, preCheckAll: false);
        if (picked == null || picked.Count == 0) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "在线导入中…");
        try
        {
            var sb = new StringBuilder();
            await Task.Run(() =>
            {
                foreach (var (name, _) in picked)
                {
                    var src = ModelCli.OnlineSources.FirstOrDefault(s => s.Name == name);
                    if (src != null)
                        sb.AppendLine(ModelCli.ImportOnline(src));
                }
            });
            Dispatcher.UIThread.Post(() =>
            {
                _status.Text = sb.ToString();
                ModelCatalog.Invalidate();
                RenderList(_search.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => _status.Text = "在线导入失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    /// <summary>清空全部模型（内置目录 + 自定义），确认后清空可重新导入。</summary>
    private async Task ClearAllAsync()
    {
        if (_busy) return;
        if (!await ConfirmAsync("🧹 清空全部模型",
            "确定清空全部模型？内置目录与已导入的自定义模型都会移除，可清空后重新导入。")) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "清空中…");
        try
        {
            var n = await Task.Run(() => ModelCatalog.ClearAll());
            Dispatcher.UIThread.Post(() =>
            {
                _status.Text = $"🗑 已清空全部模型（删除 {n} 个自定义模型文件，内置目录已隐藏）";
                ModelCatalog.Invalidate();
                RenderList(_search.Text ?? "");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => _status.Text = "清空失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    // ── Avalonia 辅助对话框（对齐 Web 交互）──

    /// <summary>多选勾选对话框（CheckBox 列表，默认全选），返回选中的 (Key, Label)，取消返回 null。</summary>
    private async Task<List<(string Key, string Label)>?> ShowMultiCheckAsync(string title, (string Key, string Label)[] options, bool preCheckAll = false)
    {
        var tcs = new TaskCompletionSource<List<(string, string)>?>();
        var dlg = new Window
        {
            Title = title,
            Width = 440,
            Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        dlg[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        var list = new StackPanel { Spacing = 6 };
        var checks = new List<CheckBox>();
        foreach (var (key, label) in options)
            checks.Add(new CheckBox { Content = label, IsChecked = preCheckAll });
        foreach (var c in checks) list.Children.Add(c);
        panel.Children.Add(list);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "取消" };
        var ok = new Button { Content = "导入所选", Classes = { "accent" } };
        btns.Children.Add(cancel);
        btns.Children.Add(ok);
        panel.Children.Add(btns);
        dlg.Content = panel;
        ok.Click += (_, _) => { tcs.TrySetResult(options.Where((_, i) => checks[i].IsChecked == true).ToList()); dlg.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(null); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(null);
        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    /// <summary>单选下拉对话框，返回选中项 label，取消返回 null。</summary>
    private async Task<string?> ShowSelectAsync(string title, string[] options)
    {
        var tcs = new TaskCompletionSource<string?>();
        var dlg = new Window
        {
            Title = title,
            Width = 420,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        dlg[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        var combo = new ComboBox { ItemsSource = options, SelectedIndex = 0 };
        panel.Children.Add(combo);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "取消" };
        var ok = new Button { Content = "导入", Classes = { "accent" } };
        btns.Children.Add(cancel);
        btns.Children.Add(ok);
        panel.Children.Add(btns);
        dlg.Content = panel;
        ok.Click += (_, _) => { tcs.TrySetResult(combo.SelectedItem as string ?? options[0]); dlg.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(null); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(null);
        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    /// <summary>确认对话框，返回是否确认。</summary>
    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = title,
            Width = 430,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        dlg[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "取消" };
        var ok = new Button { Content = "确定", Classes = { "accent" } };
        btns.Children.Add(cancel);
        btns.Children.Add(ok);
        panel.Children.Add(btns);
        dlg.Content = panel;
        ok.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(false);
        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    private static Button MakeBtn(string text, Action onClick, bool ghost = false, bool accent = false)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Thickness(12, 5),
            FontSize = 12,
        };
        if (accent) btn[!Button.BackgroundProperty] = new DynamicResourceExtension("AccentBrush");
        else if (!ghost) btn[!Button.BackgroundProperty] = new DynamicResourceExtension("Panel2BgBrush");
        btn[!Button.ForegroundProperty] = new DynamicResourceExtension("TextBrush");
        btn.Click += (_, _) => onClick();
        return btn;
    }
}
