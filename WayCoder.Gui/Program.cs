using Avalonia;

namespace WayCoder.UI.Gui;

internal static class Program
{
    /// <summary>`--gui --edit &lt;file&gt;` 透传的启动文件（App 启动后开编辑器窗口）。</summary>
    internal static string? StartupEditFile { get; private set; }

    // Avalonia 在 Windows 需要 STAThread；其它平台忽略
    [STAThread]
    public static void Main(string[] args)
    {
        // 解析 `--edit <file>`：主进程 `waycoder --gui --edit x.cs` 透传给 GUI 进程
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--edit", StringComparison.OrdinalIgnoreCase))
            {
                StartupEditFile = args[i + 1];
                break;
            }
        }

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
