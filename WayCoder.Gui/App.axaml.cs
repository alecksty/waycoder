using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

public partial class App : Application
{
    private ResourceDictionary? _dark;
    private ResourceDictionary? _light;

    /// <summary>当前是否深色主题。</summary>
    public static bool IsDark { get; private set; } = true;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 代码构建两套主题资源（值对齐 Web style.css 深/浅 CSS 变量），切换时替换合并字典
        _dark = BuildTheme(dark: true);
        _light = BuildTheme(dark: false);
        IsDark = Config.Instance.GuiTheme != "light";
        Resources.MergedDictionaries.Add(IsDark ? _dark : _light);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>切换深/浅主题，并持久化到配置。</summary>
    public static void ToggleTheme()
    {
        var app = Current as App;
        if (app?._dark == null || app._light == null) return;
        IsDark = !IsDark;
        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(IsDark ? app._dark : app._light);
        try
        {
            Config.Instance.GuiTheme = IsDark ? "dark" : "light";
            Config.Instance.SaveToEnvFile();
        }
        catch { /* 持久化失败不影响切换 */ }
    }

    /// <summary>构建一套主题资源字典（深/浅共用键名）。</summary>
    private static ResourceDictionary BuildTheme(bool dark)
    {
        void Add(ResourceDictionary d, string key, string hex) =>
            d[key] = new SolidColorBrush(Color.Parse(hex));

        var dict = new ResourceDictionary();
        if (dark)
        {
            Add(dict, "WindowBgBrush", "#0f1117");
            Add(dict, "PanelBgBrush", "#171a23");
            Add(dict, "Panel2BgBrush", "#1d2230");
            Add(dict, "BorderBrush", "#262b3a");
            Add(dict, "TextBrush", "#e6e8ee");
            Add(dict, "DimTextBrush", "#8b93a7");
            Add(dict, "AccentBrush", "#4f8cff");
            Add(dict, "UserBubbleBgBrush", "#1f3a5f");
            Add(dict, "ToolBubbleBgBrush", "#2a2416");
            Add(dict, "DangerBrush", "#3a2a2a");
            Add(dict, "SuccessBrush", "#3fb950");
        }
        else
        {
            Add(dict, "WindowBgBrush", "#f5f6f8");
            Add(dict, "PanelBgBrush", "#ffffff");
            Add(dict, "Panel2BgBrush", "#f0f2f6");
            Add(dict, "BorderBrush", "#e2e5ec");
            Add(dict, "TextBrush", "#1a1d24");
            Add(dict, "DimTextBrush", "#6b7280");
            Add(dict, "AccentBrush", "#2f6bff");
            Add(dict, "UserBubbleBgBrush", "#e3ecff");
            Add(dict, "ToolBubbleBgBrush", "#fff4dc");
            Add(dict, "DangerBrush", "#ffe2e2");
            Add(dict, "SuccessBrush", "#1a7f37");
        }
        return dict;
    }
}
