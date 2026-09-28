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
        Title = L.Pick($"{disp} · 模型", $"{disp} · Models");
        UpdateModeButton();
        RefreshSizeButtons();
        Reload();
    }

    /// <summary>模式切换按钮：点击弹菜单（编辑模式 / 选择模式）。</summary>
    private async void OnModeClicked(object? sender, EventArgs e)
    {
        // 菜单项与判据是同一批文案（平台只回传被点的**文字**、不回传序号）⇒ 用同一个标签数组按序号判定：
        // 免得「翻了菜单项、漏了 case」在英文下静默失配（点了没反应，且没有任何报错）。
        var labels = new[] { L.Pick("编辑模式", "Edit mode"), L.Pick("选择模式", "Select mode") };
        var action = await DisplayActionSheetAsync(L.Pick("模式", "Mode"), L.Pick("取消", "Cancel"), null, labels);
        switch (Array.IndexOf(labels, action!))
        {
            case 0: _editMode = true; break;
            case 1: _editMode = false; break;
            default: return;
        }
        UpdateModeButton();
        Reload();
    }

    /// <summary>更新模式按钮文字与高亮（中性反色：不再用紫/蓝两套字面色，见 MauiUi.ToggleColors）。</summary>
    private void UpdateModeButton()
    {
        ModeBtn.Text = _editMode ? L.Pick("编辑模式", "Edit mode") : L.Pick("选择模式", "Select mode");
        var c = MauiUi.ToggleColors(_editMode);
        ModeBtn.BackgroundColor = c.Bg;
        ModeBtn.TextColor = c.Text;
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
                $"{Global.FormatContext(m.ContextWindow)} {L.Pick("上下文", "context")} · {WayCoder.UI.Shared.ModelPrice.Format(m.InputPrice, m.OutputPrice, m.InputPriceOffpeak, m.OutputPriceOffpeak)} MTok",
                m.InputPrice == 0 && m.OutputPrice == 0))
            .ToList();
        ModelList.ItemsSource = rows;
        HintLabel.Text = _editMode
            ? L.Pick("✏️ 编辑模式：点模型改名 / 删除 / 改地址 / 设大小模型",
                     "✏️ Edit mode: tap a model to rename / delete / change URL / set as main or small")
            : (_isBig
                ? L.Pick($"👆 点模型设大模型 · 当前大 {current}", $"👆 Tap a model to set it as the main model · current: {current}")
                : L.Pick($"👆 点模型设小模型 · 当前小 {small}", $"👆 Tap a model to set it as the small model · current: {small}"));
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
        // 配色走 MauiUi.ToggleColors（唯一真源）：中性反色选中态
        var big = MauiUi.ToggleColors(_isBig);
        BigBtn.BackgroundColor = big.Bg;
        BigBtn.TextColor = big.Text;

        var small = MauiUi.ToggleColors(!_isBig);
        SmallBtn.BackgroundColor = small.Bg;
        SmallBtn.TextColor = small.Text;
    }

    /// <summary>两个选中勾：大✓ 小✓ 分开显示（大小模型可能是同一个，不能靠图标合并区分）。
    /// 只在「该行供应商 == 当前大小模型实际供应商」时打勾——同 id 跨供应商（deepseek-v4-pro
    /// 分属 DeepSeek/AIHubMix）不会误勾，每个供应商只勾属于自己的那条。</summary>
    private static string Marker(string pid, string id, string current, string small,
        string bigProvider, string smallProvider)
    {
        var big = string.Equals(bigProvider, pid, StringComparison.OrdinalIgnoreCase)
                  && string.Equals(id, current, StringComparison.OrdinalIgnoreCase) ? L.Pick("大✓", "Main✓") : "";
        var sm = string.Equals(smallProvider, pid, StringComparison.OrdinalIgnoreCase)
                 && string.Equals(id, small, StringComparison.OrdinalIgnoreCase) ? L.Pick("小✓", "Small✓") : "";
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
        // 同 OnModeClicked：菜单项与判据同源，用标签数组按序号判定
        var labels = new[]
        {
            L.Pick("设为当前大模型", "Set as main model"), L.Pick("设为当前小模型", "Set as small model"),
            L.Pick("改名", "Rename"), L.Pick("删除", "Delete"), L.Pick("改地址", "Change URL"),
        };
        var action = await DisplayActionSheetAsync(L.Pick($"{row.DisplayName}（{row.Id}）", $"{row.DisplayName} ({row.Id})"),
            L.Pick("取消", "Cancel"), null, labels);
        switch (Array.IndexOf(labels, action!))
        {
            case 0: await SetAsModel(row, true); break;
            case 1: await SetAsModel(row, false); break;
            case 2: await RenameModel(row); break;
            case 3: await DeleteModel(row); break;
            case 4: await EditBaseUrl(); break;
        }
    }

    /// <summary>设为当前大/小模型并刷新。</summary>
    private async Task SetAsModel(ModelRow row, bool isLarge)
    {
        ConnectionConfig.ApplyModelChoice(_pid, row.Id, isLarge: isLarge, out _);
        AgentService.Reset();
        Reload();
        await DisplayAlertAsync(
            L.Pick($"已设为当前{(isLarge ? "大" : "小")}模型", $"{(isLarge ? "Main" : "Small")} model updated"),
            L.Pick($"{row.DisplayName}（{row.Id}）", $"{row.DisplayName} ({row.Id})"), L.Pick("确定", "OK"));
    }

    /// <summary>改名：保留原模型属性（价格/上下文/地址等），只改显示名（AddCustom 同 key 覆盖）。</summary>
    private async Task RenameModel(ModelRow row)
    {
        var m = ModelCatalog.ByProvider(_pid).FirstOrDefault(x => x.Id == row.Id);
        if (m == null) { await DisplayAlertAsync(L.Pick("无法编辑", "Cannot edit"), L.Pick("未找到该模型定义", "Model definition not found"), L.Pick("确定", "OK")); return; }
        var name = await DisplayPromptAsync(L.Pick("改名模型", "Rename model"), L.Pick("显示名称", "Display name"), initialValue: m.DisplayName);
        if (string.IsNullOrWhiteSpace(name)) return;
        ModelCatalog.AddCustom(m with { DisplayName = name.Trim() });
        Reload();
    }

    /// <summary>删除模型（内置不可删，RemoveCustom 只作用于自定义库）。</summary>
    private async Task DeleteModel(ModelRow row)
    {
        var ok = await DisplayAlertAsync(L.Pick("删除模型", "Delete model"),
            L.Pick($"{row.DisplayName}（{row.Id}）？内置模型不可删。", $"{row.DisplayName} ({row.Id})? Built-in models cannot be deleted."),
            L.Pick("删除", "Delete"), L.Pick("取消", "Cancel"));
        if (!ok) return;
        var removed = ModelCatalog.RemoveCustom(row.Id);
        if (removed.Length == 0) { await DisplayAlertAsync(L.Pick("无法删除", "Cannot delete"), L.Pick("内置模型不可删除", "Built-in models cannot be deleted"), L.Pick("确定", "OK")); return; }
        Reload();
    }

    /// <summary>改供应商默认地址（UpdateProviderUrl 作用到该供应商全部模型）。</summary>
    private async Task EditBaseUrl()
    {
        var cur = ModelCatalog.BaseUrlOf(_pid);
        var url = await DisplayPromptAsync(L.Pick("改地址", "Change URL"), L.Pick($"base_url（{_pid}）", $"base_url ({_pid})"), initialValue: cur);
        if (string.IsNullOrWhiteSpace(url)) return;
        ModelCatalog.UpdateProviderUrl(_pid, url.Trim());
        Reload();
    }

    public sealed record ModelRow(string Marker, string DisplayName, string Id, string Subtitle, bool IsFree);
}
