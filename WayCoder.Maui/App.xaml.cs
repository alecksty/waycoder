namespace WayCoder.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// 启动引导：配置重定向（HomeOverride）+ 沙箱 workspace + cwd 锚点。
		// 必须最先、且在任何 Config/Agent 访问前执行。
		MauiBootstrap.Initialize();

		// 后台预热 Config（懒加载含重 IO），避免首次进设置页卡顿
		_ = MauiBootstrap.WarmupConfigAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
