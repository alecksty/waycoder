using Avalonia;

namespace WayCoder.UI.Gui;

internal static class Program
{
    // Avalonia 在 Windows 需要 STAThread；其它平台忽略
    [STAThread]
    public static void Main(string[] args)
    {
        // GUI 是独立进程，不走主项目 Program.Main —— 启动初始化须自行补齐（错误日志/Hook/MCP…）
        GuiBootstrap.Initialize();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
