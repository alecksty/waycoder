using WayCoder.UI.TuiScreens;

namespace WayCoder;
using WayCoder.UI;
using WayCoder.Terminal;

/// <summary>
/// 全局主题配置——控制所有窗口/菜单的默认外观。
/// 持久化到 .waycoder/theme.json（兼容读取 .corecoder/theme.json），可通过设置界面修改。
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

    /// <summary>强制应用主题（覆盖所有窗口属性）</summary>
    public void ApplyTo(TuiWindow win)
    {
        win.Border = BorderStyle switch
        {
            "double" => WindowBorder.Double,
            "rounded" => WindowBorder.Rounded,
            "thick" => WindowBorder.Thick,
            "dotted" => WindowBorder.Dotted,
            "dashed" => WindowBorder.Dashed,
            "ascii" => WindowBorder.Ascii,
            "slash" => WindowBorder.Slash,
            "triangle" => WindowBorder.Triangle,
            "none" => WindowBorder.None,
            _ => WindowBorder.Single,
        };
        win.BorderColor = BorderColor;
        win.WinBg = WinBg;
        win.TitleFg = TitleFg;
        win.ContentFg = ContentFg;
        win.ItemFg = ItemFg;
        win.SelFg = SelFg;
        win.SelBg = SelBg;
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

    private static string ThemePath => Global.GlobalConfigPath("theme.json");

    /// <summary>旧主题路径（向后兼容读取）</summary>
    private static string LegacyThemePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".corecoder", "theme.json");

    private static ThemeConfig Load()
    {
        try
        {
            var path = File.Exists(ThemePath) ? ThemePath :
                       File.Exists(LegacyThemePath) ? LegacyThemePath : null;
            if (path != null)
            {
                var json = File.ReadAllText(path);
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
            try { (TuiManager.Instance.ActiveScreen as ChatScreen)?.SyncTheme(); } catch { }
        }
    }
}
