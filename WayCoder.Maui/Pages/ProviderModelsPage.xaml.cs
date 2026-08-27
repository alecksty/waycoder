using WayCoder;
using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

/// <summary>供应商模型列表详情页（从 ModelManagerPage 点供应商右滑进入）：列出该供应商模型，当前选中的加指示。</summary>
[QueryProperty(nameof(ProviderId), "provider")]
public partial class ProviderModelsPage : ContentPage
{
    public string ProviderId
    {
        set { _pid = value; }
    }
    private string _pid = "";

    private bool _isBig = true; // 右上角切换：选大→点模型设大模型，选小→设小模型

    public ProviderModelsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var disp = ModelCatalog.Providers.TryGetValue(_pid, out var p) ? p.DisplayName : _pid;
        Title = $"{disp} · 模型";
        RefreshSizeButtons();
        Reload();
    }

    private void Reload()
    {
        var current = Config.Instance.Model;
        var small = Config.Instance.SmallModel;
        var rows = ModelCatalog.ByProvider(_pid)
            .OrderBy(m => m.DisplayName)
            .Select(m => new ModelRow(
                Marker(m.Id, current, small), m.DisplayName, m.Id,
                $"{FmtCtx(m.ContextWindow)} 上下文 · ${m.InputPrice:F2}/{m.OutputPrice:F2} MTok",
                m.InputPrice == 0 && m.OutputPrice == 0))
            .ToList();
        ModelList.ItemsSource = rows;
        HintLabel.Text = _isBig
            ? $"👆 点模型设大模型 · 当前大 {current}"
            : $"👆 点模型设小模型 · 当前小 {small}";
    }

    /// <summary>右上角大/小切换。</summary>
    private void OnToggleSizeClicked(object? sender, EventArgs e)
    {
        _isBig = !_isBig;
        RefreshSizeButtons();
        Reload();
    }

    private void RefreshSizeButtons()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var on = isDark ? Color.FromArgb("#3A6EA5") : Color.FromArgb("#C9DFF5");
        var off = isDark ? Color.FromArgb("#1F1F2E") : Color.FromArgb("#E8E8ED");
        BigBtn.BackgroundColor = _isBig ? on : off;
        SmallBtn.BackgroundColor = _isBig ? off : on;
    }

    private static string FmtCtx(int n) => n >= 1_000_000 ? $"{n / 1_000_000.0:F1}M" : n >= 1000 ? $"{n / 1000}k" : n.ToString();

    /// <summary>两个选中勾：大✓ 小✓ 分开显示（大小模型可能是同一个，不能靠图标合并区分）。</summary>
    private static string Marker(string id, string current, string small)
    {
        var big = string.Equals(id, current, StringComparison.OrdinalIgnoreCase) ? "大✓" : "";
        var sm = string.Equals(id, small, StringComparison.OrdinalIgnoreCase) ? "小✓" : "";
        return (big, sm) switch
        {
            ("", "") => "",
            ("", _) => sm,
            (_, "") => big,
            _ => $"{big} {sm}",
        };
    }

    /// <summary>点模型 → 按右上角大/小切换设为当前大/小模型，刷新指示。</summary>
    private async void OnModelSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ModelRow row) return;
        ModelList.SelectedItem = null;
        ConnectionConfig.ApplyModelChoice(_pid, row.Id, isLarge: _isBig, out _);
        AgentService.Reset();
        Reload();
        await DisplayAlertAsync($"已设为当前{(_isBig ? "大" : "小")}模型", $"{row.DisplayName}（{row.Id}）", "确定");
    }

    public sealed record ModelRow(string Marker, string DisplayName, string Id, string Subtitle, bool IsFree);
}
