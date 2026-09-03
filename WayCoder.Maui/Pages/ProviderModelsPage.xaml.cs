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
    private bool _editMode;     // false=选择模式（点模型直接设大小模型）；true=编辑模式（点模型弹编辑菜单）

    public ProviderModelsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var disp = ModelCatalog.ProviderDisplayName(_pid);
        Title = $"{disp} · 模型";
        UpdateModeButton();
        RefreshSizeButtons();
        Reload();
    }

    /// <summary>模式切换按钮：点击弹菜单（编辑模式 / 选择模式）。</summary>
    private async void OnModeClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("模式", "取消", null, "编辑模式", "选择模式");
        switch (action)
        {
            case "编辑模式": _editMode = true; break;
            case "选择模式": _editMode = false; break;
            default: return;
        }
        UpdateModeButton();
        Reload();
    }

    /// <summary>更新模式按钮文字与高亮（编辑模式紫色、选择模式蓝色）。</summary>
    private void UpdateModeButton()
    {
        ModeBtn.Text = _editMode ? "编辑模式" : "选择模式";
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        ModeBtn.BackgroundColor = _editMode
            ? (isDark ? Color.FromArgb("#5A3A6E") : Color.FromArgb("#EFE0F5"))
            : (isDark ? Color.FromArgb("#3A6EA5") : Color.FromArgb("#C9DFF5"));
    }

    private void Reload()
    {
        var cfg = Config.Instance;
        var current = cfg.Model;
        var small = cfg.SmallModel;
        // 当前大/小模型的实际供应商：优先 cfg.Provider / cfg.SmallProvider（ApplyModelChoice 写入），
        // 空则 Find 反推兜底。同 id 跨供应商（deepseek-v4-pro 分属 DeepSeek/AIHubMix）靠它区分勾选。
        var bigProvider = ConnectionConfig.ResolveActiveProviderId(cfg);
        var smallProvider = !string.IsNullOrWhiteSpace(cfg.SmallProvider)
            ? cfg.SmallProvider.Trim().ToLowerInvariant()
            : ModelCatalog.Find(small)?.ProviderId ?? "custom";
        var rows = ModelCatalog.ByProvider(_pid)
            .OrderBy(m => m.DisplayName)
            .Select(m => new ModelRow(
                Marker(_pid, m.Id, current, small, bigProvider, smallProvider), m.DisplayName, m.Id,
                $"{Global.FormatContext(m.ContextWindow)} 上下文 · {WayCoder.UI.Shared.ModelPrice.Format(m.InputPrice, m.OutputPrice, m.InputPriceOffpeak, m.OutputPriceOffpeak)} MTok",
                m.InputPrice == 0 && m.OutputPrice == 0))
            .ToList();
        ModelList.ItemsSource = rows;
        HintLabel.Text = _editMode
            ? "✏️ 编辑模式：点模型改名 / 删除 / 改地址 / 设大小模型"
            : (_isBig
                ? $"👆 点模型设大模型 · 当前大 {current}"
                : $"👆 点模型设小模型 · 当前小 {small}");
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

    /// <summary>两个选中勾：大✓ 小✓ 分开显示（大小模型可能是同一个，不能靠图标合并区分）。
    /// 只在「该行供应商 == 当前大小模型实际供应商」时打勾——同 id 跨供应商（deepseek-v4-pro
    /// 分属 DeepSeek/AIHubMix）不会误勾，每个供应商只勾属于自己的那条。</summary>
    private static string Marker(string pid, string id, string current, string small,
        string bigProvider, string smallProvider)
    {
        var big = string.Equals(bigProvider, pid, StringComparison.OrdinalIgnoreCase)
                  && string.Equals(id, current, StringComparison.OrdinalIgnoreCase) ? "大✓" : "";
        var sm = string.Equals(smallProvider, pid, StringComparison.OrdinalIgnoreCase)
                 && string.Equals(id, small, StringComparison.OrdinalIgnoreCase) ? "小✓" : "";
        return (big, sm) switch
        {
            ("", "") => "",
            ("", _) => sm,
            (_, "") => big,
            _ => $"{big} {sm}",
        };
    }

    /// <summary>点模型 → 选择模式：按右上角大/小切换设为当前大/小模型；编辑模式：弹编辑菜单。</summary>
    private async void OnModelSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ModelRow row) return;
        ModelList.SelectedItem = null;
        if (!_editMode)
        {
            await SetAsModel(row, _isBig);
            return;
        }
        await ShowEditMenu(row);
    }

    /// <summary>编辑模式菜单：设为大小模型 / 改名 / 删除 / 改地址。</summary>
    private async Task ShowEditMenu(ModelRow row)
    {
        var action = await DisplayActionSheetAsync($"{row.DisplayName}（{row.Id}）", "取消", null,
            "设为当前大模型", "设为当前小模型", "改名", "删除", "改地址");
        switch (action)
        {
            case "设为当前大模型": await SetAsModel(row, true); break;
            case "设为当前小模型": await SetAsModel(row, false); break;
            case "改名": await RenameModel(row); break;
            case "删除": await DeleteModel(row); break;
            case "改地址": await EditBaseUrl(); break;
        }
    }

    /// <summary>设为当前大/小模型并刷新。</summary>
    private async Task SetAsModel(ModelRow row, bool isLarge)
    {
        ConnectionConfig.ApplyModelChoice(_pid, row.Id, isLarge: isLarge, out _);
        AgentService.Reset();
        Reload();
        await DisplayAlertAsync($"已设为当前{(isLarge ? "大" : "小")}模型", $"{row.DisplayName}（{row.Id}）", "确定");
    }

    /// <summary>改名：保留原模型属性（价格/上下文/地址等），只改显示名（AddCustom 同 key 覆盖）。</summary>
    private async Task RenameModel(ModelRow row)
    {
        var m = ModelCatalog.ByProvider(_pid).FirstOrDefault(x => x.Id == row.Id);
        if (m == null) { await DisplayAlertAsync("无法编辑", "未找到该模型定义", "确定"); return; }
        var name = await DisplayPromptAsync("改名模型", "显示名称", initialValue: m.DisplayName);
        if (string.IsNullOrWhiteSpace(name)) return;
        ModelCatalog.AddCustom(m with { DisplayName = name.Trim() });
        Reload();
    }

    /// <summary>删除模型（内置不可删，RemoveCustom 只作用于自定义库）。</summary>
    private async Task DeleteModel(ModelRow row)
    {
        var ok = await DisplayAlertAsync("删除模型", $"{row.DisplayName}（{row.Id}）？内置模型不可删。", "删除", "取消");
        if (!ok) return;
        var removed = ModelCatalog.RemoveCustom(row.Id);
        if (removed.Length == 0) { await DisplayAlertAsync("无法删除", "内置模型不可删除", "确定"); return; }
        Reload();
    }

    /// <summary>改供应商默认地址（UpdateProviderUrl 作用到该供应商全部模型）。</summary>
    private async Task EditBaseUrl()
    {
        var cur = ModelCatalog.BaseUrlOf(_pid);
        var url = await DisplayPromptAsync("改地址", $"base_url（{_pid}）", initialValue: cur);
        if (string.IsNullOrWhiteSpace(url)) return;
        ModelCatalog.UpdateProviderUrl(_pid, url.Trim());
        Reload();
    }

    public sealed record ModelRow(string Marker, string DisplayName, string Id, string Subtitle, bool IsFree);
}
