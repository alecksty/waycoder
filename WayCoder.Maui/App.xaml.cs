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

		// 自动化自测钩子（仅模拟器调试：检测 autotest.flag 时自动发一条写文件任务验证）
		_ = MauiBootstrap.RunAutoTestIfRequestedAsync();

		// 原生内存实验（检测 nativemem.flag 时申请 1GB 原生内存读写校验后释放）
		MauiBootstrap.RunNativeMemTestIfRequested();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
