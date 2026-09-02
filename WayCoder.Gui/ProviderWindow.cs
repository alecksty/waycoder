using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using WayCoder.UI.TUI.Custom;

namespace WayCoder.UI.Gui;

/// <summary>
/// 服务商管理对话框（对齐 TUI ProviderPicker / Web 服务商弹窗 / 移动端供应商管理）。
/// 列出全部供应商 + 设Key/清Key/测试连通/添加/改名/改地址/删除。全部代码构建（无 XAML）。
/// </summary>
public sealed class ProviderWindow : Window
{
    private readonly MainWindow _owner;
    private readonly StackPanel _listHost = new() { Spacing = 6 };
    private readonly TextBlock _status = new() { FontSize = 12 };
    private string _selectedPid = "";
    private Dictionary<string, ModelPicker.ScanStatus> _scanResult = new();
    private bool _busy;

    public ProviderWindow(MainWindow owner)
    {
        _owner = owner;
        Title = "🗂 服务商管理";
        Width = 880;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        this[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");

        // ── 顶部操作按钮 ──
        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(12, 10, 12, 4) };
        top.Children.Add(MakeBtn("📡 测试", () => _ = TestAllAsync(), ghost: true));
        top.Children.Add(MakeBtn("➕ 添加", AddProvider, ghost: true));
        top.Children.Add(MakeBtn("🔑 设Key", SetKey, ghost: true));
        top.Children.Add(MakeBtn("🗑 清Key", ClearKey, ghost: true));
        top.Children.Add(MakeBtn("✏️ 改名", Rename, ghost: true));
        top.Children.Add(MakeBtn("🌐 改地址", EditUrl, ghost: true));
        top.Children.Add(MakeBtn("🗑 删除", Delete, accent: true));
        top.Children.Add(MakeBtn("✓ 完成", Close));

        // ── 表头 ──
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("28,1.6*,0.7*,0.5*,1*,2.2*"),
            Margin = new Thickness(12, 4, 12, 0),
        };
        header.Children.Add(HeadText("🔑", 0, center: true));
        header.Children.Add(HeadText("服务商", 1));
        header.Children.Add(HeadText("Key", 2, center: true));
        header.Children.Add(HeadText("模型", 3, right: true));
        header.Children.Add(HeadText("状态", 4, right: true));
        header.Children.Add(HeadText("地址", 5, right: true));

        // ── 列表 ──
        var scroll = new ScrollViewer
        {
            Content = _listHost,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(12, 4, 12, 4),
        };

        // ── 状态行 ──
        _status.Margin = new Thickness(12, 0, 12, 10);

        var root = new DockPanel();
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(top);
        root.Children.Add(header);
        root.Children.Add(_status);
        root.Children.Add(scroll);
        Content = root;

        RenderList();
    }

    // ── 渲染 ──

    private static TextBlock HeadText(string text, int col, bool center = false, bool right = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#8b93a7")),
        };
        if (center) tb.HorizontalAlignment = HorizontalAlignment.Center;
        if (right) tb.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(tb, col);
        return tb;
    }

    private void RenderList()
    {
        _listHost.Children.Clear();
        var providers = ModelCatalog.Providers
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (providers.Count == 0)
        {
            _listHost.Children.Add(new TextBlock { Text = "暂无供应商", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#8b93a7")) });
            return;
        }
        foreach (var (pid, p) in providers)
        {
            var isLocal = pid is "local" or "custom";
            var hasKey = isLocal || ApiKeyStore.Has(pid);
            var icon = isLocal ? "🌿" : hasKey ? "🔑" : "⚠️";
            var keyTxt = isLocal ? "-" : hasKey ? "✔" : "无";
            var modelCount = isLocal ? "-" : ModelCatalog.ByProvider(pid).Length.ToString();
            var conn = StatusText(pid);
            var addr = string.IsNullOrWhiteSpace(p.DefaultBaseUrl) ? "(未设地址)" : p.DefaultBaseUrl;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("28,1.6*,0.7*,0.5*,1*,2.2*"),
            };
            grid.Children.Add(Cell(icon, 0, center: true, bold: true));
            grid.Children.Add(Cell($"{p.DisplayName}（{pid}）", 1, bold: true, title: pid));
            grid.Children.Add(Cell(keyTxt, 2, center: true));
            grid.Children.Add(Cell(modelCount, 3, right: true));
            grid.Children.Add(Cell(conn, 4, right: true));
            grid.Children.Add(Cell(addr, 5, right: true, title: addr));

            var border = new Border
            {
                Child = grid,
                Padding = new Thickness(10, 6),
                CornerRadius = new CornerRadius(9),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#262b3a")),
                Background = pid == _selectedPid
                    ? new SolidColorBrush(Color.Parse("#1d2230"))
                    : new SolidColorBrush(Color.Parse("#171a23")),
            };
            border.PointerPressed += (_, _) =>
            {
                _selectedPid = pid;
                RenderList();
            };
            _listHost.Children.Add(border);
        }
    }

    private static TextBlock Cell(string text, int col, bool center = false, bool right = false, bool bold = false, string? title = null)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
        };
        if (bold) tb.FontWeight = FontWeight.SemiBold;
        if (center) tb.HorizontalAlignment = HorizontalAlignment.Center;
        if (right) tb.HorizontalAlignment = HorizontalAlignment.Right;
        if (title != null) ToolTip.SetTip(tb, title);
        Grid.SetColumn(tb, col);
        return tb;
    }

    private string StatusText(string pid)
    {
        if (pid is "local" or "custom")
            return _scanResult.TryGetValue(pid, out var ls) && ls == ModelPicker.ScanStatus.Connected ? "✔本地" : "本地";
        if (!ApiKeyStore.Has(pid)) return "无key";
        if (!_scanResult.TryGetValue(pid, out var st)) return "未测";
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

    private async Task TestAllAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => _status.Text = "📡 测试全部供应商连通性…");
        try
        {
            var probes = await Task.Run(ModelCli.TestList);
            var dict = new Dictionary<string, ModelPicker.ScanStatus>();
            foreach (var p in probes) dict[p.ProviderId] = ModelPicker.ProbeStatus(p);
            Dispatcher.UIThread.Post(() =>
            {
                _scanResult = dict;
                var ok = dict.Count(x => x.Value == ModelPicker.ScanStatus.Connected);
                _status.Text = $"✅ 测试完成：可达 {ok} / {dict.Count}";
                RenderList();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => _status.Text = "❌ 测试失败: " + ex.Message);
        }
        finally { _busy = false; }
    }

    private async void AddProvider()
    {
        var input = await PromptAsync("➕ 添加供应商", "格式：供应商ID|显示名|BaseUrl（可空）", "");
        if (input == null) return;
        var parts = input.Split('|');
        var id = parts.Length > 0 ? parts[0].Trim() : "";
        if (string.IsNullOrEmpty(id)) { _status.Text = "❌ 供应商 ID 不能为空"; return; }
        var name = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : id;
        var url = parts.Length > 2 ? parts[2].Trim() : "";
        if (!ModelCatalog.RegisterProvider(id, name, url))
        {
            _status.Text = $"❌ 地址已被「{ModelCatalog.FindProviderByBaseUrl(url)}」占用（同地址 = 同供应商）";
            return;
        }
        _status.Text = $"✅ 已添加供应商 {name}";
        RenderList();
    }

    private async void SetKey()
    {
        if (!RequireSelected()) return;
        var key = await PromptAsync($"🔑 设置 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 API Key", "粘贴 Key（留空 = 清除）", "");
        if (key == null) return;
        if (string.IsNullOrWhiteSpace(key)) ApiKeyStore.Remove(_selectedPid);
        else ApiKeyStore.Set(_selectedPid, key.Trim());
        _status.Text = $"✅ 已保存 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 Key";
        RenderList();
    }

    private async void ClearKey()
    {
        if (!RequireSelected()) return;
        if (!await ConfirmAsync("🗑 清Key", $"清除 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 API Key？")) return;
        ApiKeyStore.Remove(_selectedPid);
        _status.Text = $"🗑 已清除 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 Key";
        RenderList();
    }

    private async void Rename()
    {
        if (!RequireSelected()) return;
        var cur = ModelCatalog.Providers.TryGetValue(_selectedPid, out var p) ? p.DisplayName : _selectedPid;
        var name = await PromptAsync($"✏️ 改名 {ModelCatalog.ProviderDisplayName(_selectedPid)}", "新显示名", cur);
        if (name == null || string.IsNullOrWhiteSpace(name)) return;
        ModelCatalog.RenameProvider(_selectedPid, name.Trim());
        _status.Text = $"✅ 已改名 → {name.Trim()}";
        RenderList();
    }

    private async void EditUrl()
    {
        if (!RequireSelected()) return;
        var cur = ModelCatalog.Providers.TryGetValue(_selectedPid, out var p) ? p.DefaultBaseUrl ?? "" : "";
        var url = await PromptAsync($"🌐 改地址 {ModelCatalog.ProviderDisplayName(_selectedPid)}", "Base URL", cur);
        if (url == null) return;
        if (!ModelCatalog.UpdateProviderUrl(_selectedPid, url.Trim()))
        {
            _status.Text = $"❌ 新地址已被「{ModelCatalog.FindProviderByBaseUrl(url.Trim())}」占用（同地址 = 同供应商）";
            return;
        }
        _status.Text = $"✅ 已更新地址";
        RenderList();
    }

    private async void Delete()
    {
        if (!RequireSelected()) return;
        if (!await ConfirmAsync("🗑 删除供应商", $"删除 {ModelCatalog.ProviderDisplayName(_selectedPid)}？删除后不可恢复（连带清除 Key）。")) return;
        ModelCatalog.RemoveProvider(_selectedPid);
        ApiKeyStore.Remove(_selectedPid);
        _status.Text = $"🗑 已删除供应商 {ModelCatalog.ProviderDisplayName(_selectedPid)}";
        _selectedPid = "";
        RenderList();
    }

    private bool RequireSelected()
    {
        if (string.IsNullOrEmpty(_selectedPid))
        {
            _status.Text = "⚠ 请先选中一个供应商";
            return false;
        }
        if (_selectedPid is "local" or "custom")
        {
            _status.Text = "⚠ 本地供应商不可设置 Key / 改名 / 改地址 / 删除";
            return false;
        }
        return true;
    }

    // ── 对话框辅助（仿 ModelWindow）──

    private async Task<string?> PromptAsync(string title, string label, string defaultValue)
    {
        var tcs = new TaskCompletionSource<string?>();
        var dlg = new Window
        {
            Title = title,
            Width = 470,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        dlg[!BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
        var box = new TextBox { Text = defaultValue, MinWidth = 380 };
        panel.Children.Add(box);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "取消" };
        var ok = new Button { Content = "确定", Classes = { "accent" } };
        btns.Children.Add(cancel);
        btns.Children.Add(ok);
        panel.Children.Add(btns);
        dlg.Content = panel;
        ok.Click += (_, _) => { tcs.TrySetResult(box.Text); dlg.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(null); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(null);
        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

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
        var btn = new Button { Content = text, Padding = new Thickness(12, 5), FontSize = 12 };
        if (accent) btn[!Button.BackgroundProperty] = new DynamicResourceExtension("AccentBrush");
        else if (!ghost) btn[!Button.BackgroundProperty] = new DynamicResourceExtension("Panel2BgBrush");
        btn[!Button.ForegroundProperty] = new DynamicResourceExtension("TextBrush");
        btn.Click += (_, _) => onClick();
        return btn;
    }
}
