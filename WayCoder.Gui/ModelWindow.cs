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
    private Dictionary<string, bool> _scanResult = new();
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
        btnRow.Children.Add(MakeBtn("📥 自动导入", () => Task.Run(ImportAsync)));
        btnRow.Children.Add(MakeBtn("🌐 OpenCode 在线", () => Task.Run(ImportOpenCodeAsync)));
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
        rightRow.Children.Add(MakeBtn("💾 保存", () => { _owner.SaveDefaultModel(_selectedId, _smallMode); }));
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
        AddCol(grid, 1, "模型", bold: true);
        AddCol(grid, 2, "厂商", bold: true);
        AddCol(grid, 3, "窗口", bold: true, alignRight: true);
        AddCol(grid, 4, "价格", bold: true, alignRight: true);
        AddCol(grid, 5, "大", bold: true, alignCenter: true);
        AddCol(grid, 6, "小", bold: true, alignCenter: true);
        return grid;
    }

    private static Grid NewModelGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("28")));
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
            if (_scanResult.TryGetValue(group.Key, out var ok))
                gname.Text += ok ? "  ✅" : "  ❌";
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

        var nameTb = new TextBlock { Text = m.DisplayName, FontSize = 12.5, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        ToolTip.SetTip(nameTb, m.Id);
        Grid.SetColumn(nameTb, 1);
        grid.Children.Add(nameTb);

        var provTb = new TextBlock { Text = m.Provider, FontSize = 11.5, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(provTb, 2);
        grid.Children.Add(provTb);

        var ctxTb = new TextBlock { Text = Panels.FormatCtx(m.ContextWindow), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(ctxTb, 3);
        grid.Children.Add(ctxTb);

        var priceTb = new TextBlock { Text = Panels.FormatPrice(m.InputPrice), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(priceTb, 4);
        grid.Children.Add(priceTb);

        var bigTb = new TextBlock { Text = isBig ? "✓" : "", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(bigTb, 5);
        grid.Children.Add(bigTb);

        var smallTb = new TextBlock { Text = isSmall ? "✓" : "", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(smallTb, 6);
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
            // 重新渲染全部行以刷新高亮
            RenderList(_search.Text ?? "");
        };
        return row;
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
            _owner.ApplySmallModel(_selectedId);
        }
        else
        {
            _owner.ApplyModel(_selectedId);
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

    private async Task ScanAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "扫描连通性…");
        try
        {
            var probes = await Task.Run(ModelCli.TestList);
            var dict = new Dictionary<string, bool>();
            foreach (var p in probes) dict[p.ProviderId] = p.Ok;
            Dispatcher.UIThread.Post(() =>
            {
                _scanResult = dict;
                var ok = dict.Count(x => x.Value);
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

    private async Task ImportAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "自动导入中…");
        try
        {
            var report = await Task.Run(() => ModelCli.Import(null));
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

    private async Task ImportOpenCodeAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "OpenCode 在线导入中…");
        try
        {
            var sb = new StringBuilder();
            await Task.Run(() =>
            {
                const string url = "https://opencode.ai/zen/go/v1/models";
                const string apiBase = "https://opencode.ai/zen/go/v1";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");
                var json = client.GetStringAsync(url).GetAwaiter().GetResult();
                var list = ModelCatalog.ImportOpenCodeApi(json, apiBase);
                var builtIn = new HashSet<string>(ModelCatalog.BuiltIn.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                var added = 0; var skipped = 0;
                foreach (var m in list)
                {
                    if (builtIn.Contains(m.Id)) { skipped++; continue; }
                    ModelCatalog.AddCustom(m);
                    added++;
                }
                sb.Append($"✅ 从 OpenCode 在线导入 {added} 个模型" + (skipped > 0 ? $"，跳过 {skipped} 个内置已有" : ""));
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
            Dispatcher.UIThread.Post(() => _status.Text = "OpenCode 导入失败: " + ex.Message);
        }
        finally { _busy = false; }
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
