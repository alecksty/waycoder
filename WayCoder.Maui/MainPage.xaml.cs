using WayCoder;

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
        ModelLabel.Text = cfg.Model;
        KeyLabel.Text = string.IsNullOrEmpty(cfg.ApiKey)
            ? "尚未配置 API Key，先去「设置」填写"
            : $"已配置 Key · 服务商 {cfg.Provider}";
    }

    private async void OnChatClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//chat");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//settings");
}
