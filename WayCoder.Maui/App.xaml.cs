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

	/// <summary>切后台（来电/Home/锁屏）时取消在途对话请求。
	/// 后台流式 SSE 连接可能被系统 Doze/网络策略挂起，不取消会导致 LLM 正文读取永久阻塞
	/// （最长 5 分钟 body 超时），切回后 IsRunning 卡 true、UI 卡「思考中」无法继续。</summary>
	protected override void OnSleep()
	{
		base.OnSleep();
		Services.AgentService.CancelActive();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
