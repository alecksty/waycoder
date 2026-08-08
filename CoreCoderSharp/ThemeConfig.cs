namespace CoreCoderSharp;
using CoreCoderSharp.UI;
using CoreCoderSharp.Terminal;

/// <summary>
/// 全局主题配置——控制所有窗口/菜单的默认外观。
/// 持久化到 .corecoder/theme.json，可通过设置界面修改。
/// </summary>
public class ThemeConfig
{
    public static ThemeConfig Instance { get; private set; } = Load();

    // 边框
    public string BorderStyle { get; set; } = "single";
    public int BorderColor { get; set; } = 36;

    // 背景
    public int WinBg { get; set; } = 0;

    // 文字颜色
    public int TitleFg { get; set; } = 0;
    public int ContentFg { get; set; } = 0;
    public int ItemFg { get; set; } = 0;

    // 选中项
    public int SelFg { get; set; } = 30;
    public int SelBg { get; set; } = 46;

    /// <summary>将主题应用到窗口（只填充未设置=0的项）</summary>
    public void ApplyTo(ManagedWindow win)
    {
        if (win.BorderStyle == "single" && string.IsNullOrEmpty(win.CustomBorder))
            win.BorderStyle = BorderStyle;
        if (win.BorderColor == 36) win.BorderColor = BorderColor;
        if (win.WinBg == 0) win.WinBg = WinBg;
        if (win.TitleFg == 0) win.TitleFg = TitleFg;
        if (win.ContentFg == 0) win.ContentFg = ContentFg;
        if (win.ItemFg == 0) win.ItemFg = ItemFg;
        if (win.SelFg == 30) win.SelFg = SelFg;
        if (win.SelBg == 46) win.SelBg = SelBg;
    }

    // ================================================================
    // 预设
    // ================================================================

    public static ThemeConfig Default => new()
    {
        BorderStyle = "single", BorderColor = Color.Cyan,
        SelFg = Color.Black, SelBg = Color.BgCyan,
    };

    public static ThemeConfig Ocean => new()
    {
        BorderStyle = "rounded", BorderColor = Color.Cyan,
        WinBg = Color.BgBlue, TitleFg = Color.White, ContentFg = Color.White, ItemFg = Color.Cyan,
        SelFg = Color.Black, SelBg = Color.BgCyan,
    };

    public static ThemeConfig Forest => new()
    {
        BorderStyle = "double", BorderColor = Color.Green,
        WinBg = Color.BgGreen, TitleFg = Color.White, ContentFg = Color.White, ItemFg = Color.Green,
        SelFg = Color.Black, SelBg = Color.BgYellow,
    };

    public static ThemeConfig Sunset => new()
    {
        BorderStyle = "thick", BorderColor = Color.Yellow,
        WinBg = Color.BgYellow, TitleFg = Color.Red, ContentFg = Color.Yellow, ItemFg = Color.Yellow,
        SelFg = Color.Black, SelBg = Color.BgRed,
    };

    public static ThemeConfig Midnight => new()
    {
        BorderStyle = "single", BorderColor = Color.Magenta,
        WinBg = Color.BgMagenta, TitleFg = Color.White, ContentFg = Color.White, ItemFg = Color.Magenta,
        SelFg = Color.White, SelBg = Color.BgBlue,
    };

    public static ThemeConfig Mono => new()
    {
        BorderStyle = "ascii", BorderColor = Color.White,
        WinBg = 0, TitleFg = Color.White, ContentFg = 0, ItemFg = Color.White,
        SelFg = Color.White, SelBg = Color.BgBlack,
    };

    internal static readonly Dictionary<string, ThemeConfig> Presets = new()
    {
        ["default"] = Default,
        ["ocean"] = Ocean,
        ["forest"] = Forest,
        ["sunset"] = Sunset,
        ["midnight"] = Midnight,
        ["mono"] = Mono,
    };

    // ================================================================
    // 持久化
    // ================================================================

    private static string ThemePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".corecoder", "theme.json");

    private static ThemeConfig Load()
    {
        try
        {
            if (File.Exists(ThemePath))
            {
                var json = File.ReadAllText(ThemePath);
                var node = JsonNode.Parse(json);
                if (node != null)
                {
                    // 检查是否引用预设
                    var preset = node["preset"]?.GetValue<string>();
                    if (preset != null && Presets.TryGetValue(preset, out var p))
                        return p;

                    return new ThemeConfig
                    {
                        BorderStyle = node["borderStyle"]?.GetValue<string>() ?? "single",
                        BorderColor = node["borderColor"]?.GetValue<int>() ?? 36,
                        WinBg = node["winBg"]?.GetValue<int>() ?? 0,
                        TitleFg = node["titleFg"]?.GetValue<int>() ?? 0,
                        ContentFg = node["contentFg"]?.GetValue<int>() ?? 0,
                        ItemFg = node["itemFg"]?.GetValue<int>() ?? 0,
                        SelFg = node["selFg"]?.GetValue<int>() ?? 30,
                        SelBg = node["selBg"]?.GetValue<int>() ?? 46,
                    };
                }
            }
        }
        catch { }
        return Default;
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ThemePath)!;
            Directory.CreateDirectory(dir);
            var json = $@"{{
  ""borderStyle"": ""{BorderStyle}"",
  ""borderColor"": {BorderColor},
  ""winBg"": {WinBg},
  ""titleFg"": {TitleFg},
  ""contentFg"": {ContentFg},
  ""itemFg"": {ItemFg},
  ""selFg"": {SelFg},
  ""selBg"": {SelBg}
}}";
            File.WriteAllText(ThemePath, json);
            Instance = this;
        }
        catch { }
    }

    /// <summary>应用预设并保存，同步主界面</summary>
    public static void ApplyPreset(string name)
    {
        if (Presets.TryGetValue(name, out var preset))
        {
            Instance = preset;
            preset.Save();
            try { ScreenManager.Instance.SyncTheme(); } catch { }
        }
    }
}
