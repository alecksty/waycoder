namespace WayCoder.Maui.Pages;

/// <summary>关于页：图标 / App 名 / 版本号 / 更新日志（读内嵌 CHANGELOG.md 资源）。</summary>
public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        VersionLabel.Text = $"版本 {Global.Version}";
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("CHANGELOG.md");
            using var reader = new StreamReader(stream);
            ChangelogLabel.Text = await reader.ReadToEndAsync();
        }
        catch
        {
            ChangelogLabel.Text = "暂无更新日志";
        }
    }
}
