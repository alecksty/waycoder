using WayCoder;
using WayCoder.Maui.Services;

namespace WayCoder.Maui;

/// <summary>首页 —— 欢迎 + 当前配置状态 + 快速入口。</summary>
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        VersionLabel.Text = $"版本 {Global.Version}";

        var cfg = Config.Instance;
        // 模型 + 服务商显示名（同 id 跨服务商可区分，与 TUI/ChatPage 模型栏格式一致）
        ModelLabel.Text = ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.Provider), cfg.Model);
        // Key 按服务商存于 ApiKeyStore，不能用 Config.ApiKey 判断（同 ChatPage 修复）
        KeyLabel.Text = AgentService.HasUsableKey()
            ? $"已配置 Key · 服务商 {ModelCatalog.ProviderDisplayName(cfg.Provider)}"
            : "尚未配置 API Key，先去「设置」填写";
    }

    private async void OnChatClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//chat");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//settings");

    private async void OnAboutClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("about");

    private async void OnModelsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("models");

    private async void OnGitSyncClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("gitsync");
}
