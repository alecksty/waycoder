using WayCoder;
using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

/// <summary>模型选择对话框（对标 TUI ModelPicker）：按供应商分组 + 大/小模型切换 + 实时搜索过滤。</summary>
public partial class ModelPickerPage : ContentPage
{
    /// <summary>列表展示项。</summary>
    public sealed record ModelEntry(string Id, string DisplayName, string ProviderId, string Subtitle, bool IsFree);

    /// <summary>分组（CollectionView IsGrouped 用，子项集合属性名 Items）。</summary>
    public sealed class ModelGroup
    {
        public string Name { get; }
        public List<ModelEntry> Items { get; }
        public ModelGroup(string name, List<ModelEntry> items) { Name = name; Items = items; }
    }

    private bool _isBig = true; // 默认选大模型槽位

    public ModelPickerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSizeButtons();
        Reload();
    }

    /// <summary>按搜索词 + 大/小槽位刷新模型分组列表。</summary>
    private void Reload()
    {
        var query = SearchEntry.Text?.Trim().ToLowerInvariant() ?? "";
        var models = ModelCatalog.All
            .Where(m => m.ProviderId is not ("local" or "custom"))
            .Where(m => query.Length == 0
                || m.Id.ToLowerInvariant().Contains(query)
                || m.DisplayName.ToLowerInvariant().Contains(query)
                || m.ProviderId.ToLowerInvariant().Contains(query))
            .OrderBy(m => m.DisplayName)
            .Select(m => new ModelEntry(m.Id, m.DisplayName, m.ProviderId,
                $"{FmtCtx(m.ContextWindow)} 上下文 · ${m.InputPrice:F2}/{m.OutputPrice:F2} MTok",
                m.InputPrice == 0 && m.OutputPrice == 0))
            .ToList();

        ModelList.ItemsSource = models
            .GroupBy(m => ProviderName(m.ProviderId))
            .OrderBy(g => g.Key)
            .Select(g => new ModelGroup(g.Key, g.ToList()))
            .ToList();
    }

    private static string ProviderName(string pid)
        => ModelCatalog.Providers.TryGetValue(pid, out var p) ? p.DisplayName : pid;

    private static string FmtCtx(int n) => n >= 1_000_000 ? $"{n / 1_000_000.0:F1}M" : n >= 1000 ? $"{n / 1000}k" : n.ToString();

    /// <summary>选中模型 → 应用到大/小槽位（连接层统一入口）→ 返回。</summary>
    private async void OnModelSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ModelEntry entry) return;
        ModelList.SelectedItem = null;
        ConnectionConfig.ApplyModelChoice(entry.ProviderId, entry.Id, isLarge: _isBig, out _);
        AgentService.Reset();
        await Shell.Current.Navigation.PopAsync();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Reload();

    /// <summary>切换大/小模型槽位（TUI ModelPicker 的 Tab 语义）。</summary>
    private void OnToggleSizeClicked(object? sender, EventArgs e)
    {
        _isBig = !_isBig;
        RefreshSizeButtons();
    }

    private void RefreshSizeButtons()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var primary = isDark ? Color.FromArgb("#3A6EA5") : Color.FromArgb("#C9DFF5");
        var plain = isDark ? Color.FromArgb("#1F1F2E") : Color.FromArgb("#E8E8ED");
        BigBtn.BackgroundColor = _isBig ? primary : plain;
        SmallBtn.BackgroundColor = _isBig ? plain : primary;
    }
}
