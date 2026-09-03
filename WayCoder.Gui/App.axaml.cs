using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;

namespace WayCoder.UI.Gui;

public partial class App : Application
{
    /// <summary>当前是否深色主题。</summary>
    public static bool IsDark { get; private set; } = true;

    /// <summary>主题切换完成事件（EditorWindow 等持快照画刷的控件订阅，切主题后重解析自身画刷）。</summary>
    public static event Action? ThemeChanged;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 主题色真源在 App.axaml 的 ThemeDictionaries（Dark/Light），切变体即换整套配色。
        IsDark = Config.Instance.GuiTheme != "light";
        RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            // `--gui --edit <file>`：主窗口就绪后开编辑器窗口加载该文件
            if (!string.IsNullOrEmpty(Program.StartupEditFile))
            {
                var file = Program.StartupEditFile;
                Dispatcher.UIThread.Post(() => new EditorWindow(file).Show());
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>切换深/浅主题（切换 RequestedThemeVariant），并持久化到配置。</summary>
    public static void ToggleTheme()
    {
        if (Current is not App app) return;
        IsDark = !IsDark;
        app.RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        GuiColors.Invalidate(); // 颜色桥缓存按新主题重新解析（语法/状态/diff 也随主题）
        try
        {
            Config.Instance.GuiTheme = IsDark ? "dark" : "light";
            Config.Instance.SaveToEnvFile();
        }
        catch { /* 持久化失败不影响切换 */ }
        ThemeChanged?.Invoke();
    }
}
