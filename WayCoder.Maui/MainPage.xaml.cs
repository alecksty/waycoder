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
        RefreshModeButtons();
    }

    /// <summary>刷新模式/权限按钮文本（显示当前值）。</summary>
    private void RefreshModeButtons()
    {
        ModeBtn.Text = $"⚙ {WorkModeManager.Format(WorkModeManager.CurrentMode)}";
        PermBtn.Text = $"🔐 {PermissionManager.FormatMode()}";
    }

    /// <summary>点「模式」按钮：循环切换工作模式（Build→Plan→Chat）并同步到 Agent。</summary>
    private void OnModeClicked(object? sender, EventArgs e)
    {
        WorkModeManager.CycleNext();
        if (AgentService.CurrentAgent is { } agent) agent.WorkMode = WorkModeManager.CurrentMode;
        RefreshModeButtons();
    }

    /// <summary>点「权限」按钮：循环切换确认轴（Ask→Auto→SmartAuto→Yolo）。</summary>
    private void OnPermClicked(object? sender, EventArgs e)
    {
        PermissionManager.CycleMode();
        RefreshModeButtons();
    }

    private void RefreshStatus()
    {
        VersionLabel.Text = $"版本 {Global.Version}";

        var cfg = Config.Instance;
        ModelLabel.Text = cfg.Model;
        // Key 按服务商存于 ApiKeyStore，不能用 Config.ApiKey 判断（同 ChatPage 修复）
        KeyLabel.Text = AgentService.HasUsableKey()
            ? $"已配置 Key · 服务商 {cfg.Provider}"
            : "尚未配置 API Key，先去「设置」填写";
    }

    private async void OnChatClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//chat");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//settings");
}
