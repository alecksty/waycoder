using WayCoder;
using WayCoder.Infra;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 设置的**一级页**：只列分组与当前值摘要，点某行进二级页
/// （<see cref="SettingsGroupPage"/>，路由 `settingsgroup?group=xxx`）。
///
/// 摘要一律**现取**（`OnAppearing` 里重算），不缓存 —— 二级页改完返回时
/// 若拿旧值显示，用户会以为「改了没生效」。
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSummaries();
    }

    private void RefreshSummaries()
    {
        var cfg = Config.Instance;

        // ── 模型：大/小 + Key 有没有配（没配 Key 是最常见的「不工作」原因，摘要里必须看得见）
        var bigKey = DescribeKey(cfg.Provider);
        var smallKey = DescribeKey(cfg.SmallProvider);
        var small = string.IsNullOrEmpty(cfg.SmallModel) ? "未设（跟随大模型）" : cfg.SmallModel;
        ModelSummary.Text = $"{cfg.Provider} · {cfg.Model}　{bigKey}\n小模型 {small}　{smallKey}";

        // ── 参数
        var ctx = cfg.MaxContextTokens > 0 ? $"{cfg.MaxContextTokens / 1024}K" : "默认";
        var budget = cfg.MaxBudgetUsd is double b ? $"${b:F2}" : "不限";
        var economy = UiText.EconomyName(cfg.EconomyMode);
        ParamsSummary.Text = $"上下文 {ctx} · 温度 {cfg.Temperature} · 预算 {budget} · 经济 {economy}";

        // ── 权限（文案唯一真源在 UiText，别在这儿再写一份措辞）
        PermSummary.Text = UiText.PermFull(PermissionManager.CurrentMode);

        // ── 存储与编辑器
        var where = WayCoder.Maui.MauiBootstrap.WorkspaceExternal ? "外部存储 ✅" : "App 私有目录 ⚠️";
        var mb = (int)(Services.MauiEditorStore.ReadOnlyMaxBytes / (1024 * 1024));
        StorageSummary.Text = $"workspace：{where} · 可编辑上限 {mb}MB";

        // ── 语音
        var wm = string.IsNullOrEmpty(cfg.WhisperModel) ? "默认 whisper-1" : cfg.WhisperModel;
        VoiceSummary.Text = wm;

        // ── 关于
        AboutSummary.Text = $"WayCoder {Global.Version}";
    }

    /// <summary>Key 状态一句话。空 → 明确说「未配」，别只留空白让人猜。</summary>
    private static string DescribeKey(string providerId)
    {
        try
        {
            var key = ApiKeyStore.Get(providerId);
            return string.IsNullOrEmpty(key) ? "未配 Key ⚠️" : "已配 Key ✅";
        }
        catch
        {
            return "";
        }
    }

    private static Task Go(string group) =>
        Shell.Current.GoToAsync($"settingsgroup?group={group}");

    private async void OnModelTapped(object? sender, TappedEventArgs e) => await Go("model");
    private async void OnParamsTapped(object? sender, TappedEventArgs e) => await Go("params");
    private async void OnPermTapped(object? sender, TappedEventArgs e) => await Go("perm");
    private async void OnStorageTapped(object? sender, TappedEventArgs e) => await Go("storage");
    private async void OnVoiceTapped(object? sender, TappedEventArgs e) => await Go("voice");

    private async void OnAboutTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("about");
}
