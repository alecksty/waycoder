using Avalonia;

namespace WayCoder.UI.Gui;

internal static class Program
{
    // Avalonia 在 Windows 需要 STAThread；其它平台忽略
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
