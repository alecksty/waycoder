using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WayCoder.Maui.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();

		// 【临时诊断｜定位完删】装全局异常钩子：WinUI 把托管异常包成 stowed exception
		// 直接终止进程，不装这个就什么都留不下（见 DiagLog 的注释）。
		DiagLog.Hook();
		this.UnhandledException += (_, e) =>
			DiagLog.Write("‼ WinUI.UnhandledException", e.Exception);
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

