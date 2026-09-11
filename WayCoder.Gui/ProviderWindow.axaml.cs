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
/// 静态壳在 ProviderWindow.axaml；列出全部供应商 + 设Key/清Key/测试连通/添加/改名/改地址/删除。
/// </summary>
public sealed partial class ProviderWindow : Window
{
    private readonly MainWindow _owner;
    private string _selectedPid = "";
    private Dictionary<string, ModelPicker.ScanStatus> _scanResult = new();
    private bool _busy;

    /// <summary>仅在 Avalonia XAML 加载器/预览器路径使用；生产流程走带 owner 的构造。</summary>
    public ProviderWindow()
    {
        InitializeComponent();
        _owner = null!;
    }

    public ProviderWindow(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;
        Title = "🗂 服务商管理";

        // ── 顶部操作按钮 ──
        TopBtn.Children.Add(UiKit.MakeButton("📡 测试", ButtonVariant.Ghost, () => _ = TestAllAsync()));
        TopBtn.Children.Add(UiKit.MakeButton("➕ 添加", ButtonVariant.Ghost, AddProvider));
        TopBtn.Children.Add(UiKit.MakeButton("🔑 设Key", ButtonVariant.Ghost, SetKey));
        TopBtn.Children.Add(UiKit.MakeButton("🗑 清Key", ButtonVariant.Ghost, ClearKey));
        TopBtn.Children.Add(UiKit.MakeButton("✏️ 改名", ButtonVariant.Ghost, Rename));
        TopBtn.Children.Add(UiKit.MakeButton("🌐 改地址", ButtonVariant.Ghost, EditUrl));
        TopBtn.Children.Add(UiKit.MakeButton("🗑 删除", ButtonVariant.Accent, Delete));
        TopBtn.Children.Add(UiKit.MakeButton("✓ 完成", ButtonVariant.Default, Close));

        BuildHeader();
        RenderList();
    }

    // ── 渲染 ──

    private void BuildHeader()
    {
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("28")));
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("1.6*")));
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("0.7*")));
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("0.5*")));
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("1*")));
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("2.2*")));
        HeaderGrid.Children.Add(HeadText("🔑", 0, center: true));
        HeaderGrid.Children.Add(HeadText("服务商", 1));
        HeaderGrid.Children.Add(HeadText("Key", 2, center: true));
        HeaderGrid.Children.Add(HeadText("模型", 3, right: true));
        HeaderGrid.Children.Add(HeadText("状态", 4, right: true));
        HeaderGrid.Children.Add(HeadText("地址", 5, right: true));
    }

    private static TextBlock HeadText(string text, int col, bool center = false, bool right = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = GuiColors.DimText,
        };
        if (center) tb.HorizontalAlignment = HorizontalAlignment.Center;
        if (right) tb.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(tb, col);
        return tb;
    }

    private void RenderList()
    {
        ListHost.Children.Clear();
        var providers = ModelCatalog.Providers
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (providers.Count == 0)
        {
            ListHost.Children.Add(new TextBlock { Text = "暂无供应商", FontSize = 12, Foreground = GuiColors.DimText });
            return;
        }
        foreach (var (pid, p) in providers)
        {
            var isLocal = pid is "local" or "custom";
            var hasKey = isLocal || ApiKeyStore.Has(pid);
            var icon = isLocal ? "🌿" : hasKey ? "🔑" : "⚠️";
            var keyTxt = isLocal ? "-" : hasKey ? "✔" : "无";
            var modelCount = isLocal ? "-" : ModelCatalog.ByProvider(pid).Length.ToString();
            var conn = RowStatusText(pid);
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
                BorderBrush = GuiColors.Border,
                Background = pid == _selectedPid
                    ? GuiColors.Panel2Bg
                    : GuiColors.PanelBg,
            };
            border.PointerPressed += (_, _) =>
            {
                _selectedPid = pid;
                RenderList();
            };
            ListHost.Children.Add(border);
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

    private string RowStatusText(string pid)
    {
        if (pid is "local" or "custom")
            return _scanResult.TryGetValue(pid, out var ls) && ls == ModelPicker.ScanStatus.Connected ? "✔本地" : "本地";
        if (!ApiKeyStore.Has(pid)) return "无key";
        if (!_scanResult.TryGetValue(pid, out var st)) return "未测";
        // ScanStatus → 中文映射收敛到核心 ModelPicker.StatusText（本地/无key/未测特判保留在上方）
        return ModelPicker.StatusText(st);
    }

    // ── 操作 ──

    private async Task TestAllAsync()
    {
        if (_busy) return;
        _busy = true;
        Dispatcher.UIThread.Post(() => StatusText.Text = "📡 测试全部供应商连通性…");
        try
        {
            // 扫描 + UI 线程回投的共享实现（与 ModelWindow.ScanAsync 同一份）
            await ProviderScanner.ScanAsync(dict =>
            {
                _scanResult = dict;
                var ok = dict.Count(x => x.Value == ModelPicker.ScanStatus.Connected);
                StatusText.Text = $"✅ 测试完成：可达 {ok} / {dict.Count}";
                RenderList();
            }, msg => StatusText.Text = "❌ 测试失败: " + msg);
        }
        finally { _busy = false; }
    }

    private async void AddProvider()
    {
        var input = await UiKit.ShowPrompt(this, "➕ 添加供应商", "格式：供应商ID|显示名|BaseUrl（可空）", "");
        if (input == null) return;
        var parts = input.Split('|');
        var id = parts.Length > 0 ? parts[0].Trim() : "";
        if (string.IsNullOrEmpty(id)) { StatusText.Text = "❌ 供应商 ID 不能为空"; return; }
        var name = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : id;
        var url = parts.Length > 2 ? parts[2].Trim() : "";
        var err = ModelCatalog.RegisterProviderResult(id, name, url);
        if (err != null)
        {
            StatusText.Text = "❌ " + err;
            return;
        }
        StatusText.Text = $"✅ 已添加供应商 {name}";
        RenderList();
    }

    private async void SetKey()
    {
        if (!RequireSelected()) return;
        var input = await UiKit.ShowPrompt(this, $"🔑 设置 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 API Key", "粘贴 Key（留空 = 清除）", "");
        if (input == null) return;
        if (string.IsNullOrWhiteSpace(input)) ApiKeyStore.Remove(_selectedPid);
        else ApiKeyStore.Set(_selectedPid, input.Trim());
        StatusText.Text = $"✅ 已保存 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 Key";
        RenderList();
    }

    private async void ClearKey()
    {
        if (!RequireSelected()) return;
        if (!await UiKit.ShowConfirm(this, "🗑 清Key", $"清除 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 API Key？")) return;
        ApiKeyStore.Remove(_selectedPid);
        StatusText.Text = $"🗑 已清除 {ModelCatalog.ProviderDisplayName(_selectedPid)} 的 Key";
        RenderList();
    }

    private async void Rename()
    {
        if (!RequireSelected()) return;
        var cur = ModelCatalog.ProviderDisplayName(_selectedPid);
        var input = await UiKit.ShowPrompt(this, $"✏️ 改名 {ModelCatalog.ProviderDisplayName(_selectedPid)}", "新显示名", cur);
        if (input == null || string.IsNullOrWhiteSpace(input)) return;
        ModelCatalog.RenameProvider(_selectedPid, input.Trim());
        StatusText.Text = $"✅ 已改名 → {input.Trim()}";
        RenderList();
    }

    private async void EditUrl()
    {
        if (!RequireSelected()) return;
        var cur = ModelCatalog.BaseUrlOf(_selectedPid);
        var input = await UiKit.ShowPrompt(this, $"🌐 改地址 {ModelCatalog.ProviderDisplayName(_selectedPid)}", "Base URL", cur);
        if (input == null) return;
        var urlErr = ModelCatalog.UpdateProviderUrlResult(_selectedPid, input.Trim());
        if (urlErr != null)
        {
            StatusText.Text = "❌ 新" + urlErr;
            return;
        }
        StatusText.Text = $"✅ 已更新地址";
        RenderList();
    }

    private async void Delete()
    {
        if (!RequireSelected()) return;
        if (!await UiKit.ShowConfirm(this, "🗑 删除供应商", $"删除 {ModelCatalog.ProviderDisplayName(_selectedPid)}？删除后不可恢复（连带清除 Key）。")) return;
        ModelCatalog.RemoveProvider(_selectedPid);
        ApiKeyStore.Remove(_selectedPid);
        StatusText.Text = $"🗑 已删除供应商 {ModelCatalog.ProviderDisplayName(_selectedPid)}";
        _selectedPid = "";
        RenderList();
    }

    private bool RequireSelected()
    {
        if (string.IsNullOrEmpty(_selectedPid))
        {
            StatusText.Text = "⚠ 请先选中一个供应商";
            return false;
        }
        if (_selectedPid is "local" or "custom")
        {
            StatusText.Text = "⚠ 本地供应商不可设置 Key / 改名 / 改地址 / 删除";
            return false;
        }
        return true;
    }
}
