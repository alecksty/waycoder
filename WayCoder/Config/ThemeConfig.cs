using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;

/// <summary>
/// 全局主题配置——控制所有窗口/菜单的默认外观。
/// 持久化到 .waycoder/theme.json（兼容读取 .corecoder/theme.json），可通过设置界面修改。
/// </summary>
public class ThemeConfig
{

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

    /// <summary>主题预设规范键（dark/ocean/...）。null=未记录（回退 .env ThemePreset）。</summary>
    public string? PresetKey { get; set; }

    /// <summary>强制应用主题（覆盖所有窗口属性）</summary>
    public void ApplyTo(TuiWindow win)
    {
        win.BorderStyle = BorderStyle switch
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
        // TuiTheme 规范键 → 窗口级边框样式桥接（配色真源在 TuiTheme）
        ["dark"] = Default,
        ["light"] = Default,
        ["hc"] = Default,
        ["retro"] = Default,
        ["ocean"] = Ocean,
        ["forest"] = Forest,
        ["sunset"] = Sunset,
        ["midnight"] = Midnight,
        ["mono"] = Mono,
    };

    public static ThemeConfig Instance { get; private set; } = Load();

    // ================================================================
    // 持久化
    // ================================================================

    private static string ThemePath => Global.GlobalConfigPath("theme.json");

    /// <summary>旧主题路径（向后兼容读取）</summary>
    private static string LegacyThemePath => Path.Combine(
        Global.Home,
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
                var node = Json.Parse(json);
                if (node != null)
                {
                    // 引用预设：返回该预设的窗口级样式 + 记录预设键（配色真源在 TuiTheme，按 PresetKey 应用）
                    var preset = node["preset"]?.AsString();
                    if (preset != null && Presets.TryGetValue(preset, out var p))
                        return CloneWith(p, preset);

                    return new ThemeConfig
                    {
                        BorderStyle = node["borderStyle"]?.AsString() ?? "single",
                        BorderColor = (int)(node["borderColor"]?.AsNumber() ?? 36),
                        WinBg = (int)(node["winBg"]?.AsNumber() ?? 0),
                        TitleFg = (int)(node["titleFg"]?.AsNumber() ?? 0),
                        ContentFg = (int)(node["contentFg"]?.AsNumber() ?? 0),
                        ItemFg = (int)(node["itemFg"]?.AsNumber() ?? 0),
                        SelFg = (int)(node["selFg"]?.AsNumber() ?? 30),
                        SelBg = (int)(node["selBg"]?.AsNumber() ?? 46),
                        PresetKey = null, // 旧格式无 preset 字段 → 视为自定义窗口样式
                    };
                }
            }
        }
        catch { }
        return Default;
    }

    /// <summary>复制预设窗口级样式并打上预设键（不污染共享静态预设实例）</summary>
    private static ThemeConfig CloneWith(ThemeConfig src, string presetKey) => new()
    {
        BorderStyle = src.BorderStyle,
        BorderColor = src.BorderColor,
        WinBg = src.WinBg,
        TitleFg = src.TitleFg,
        ContentFg = src.ContentFg,
        ItemFg = src.ItemFg,
        SelFg = src.SelFg,
        SelBg = src.SelBg,
        PresetKey = presetKey,
    };

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ThemePath)!;
            Directory.CreateDirectory(dir);
            var presetJson = PresetKey == null ? "null" : $"\"{PresetKey}\"";
            var json = $@"{{
  ""preset"": {presetJson},
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

    /// <summary>应用预设并保存，同步主界面。
    /// 统一配色真源为 TuiTheme（8 预设），ThemeConfig 只负责窗口级边框样式 + 持久化。</summary>
    public static void ApplyPreset(string name)
    {
        // 1. 应用 TuiTheme 配色（控件/窗口/对话框全部生效）
        TuiTheme.ApplyByName(name);

        // 2. 窗口级边框样式桥接（按归一化键查 ThemeConfig 预设，未命中回退 Default）
        var key = TuiTheme.NormalizeKey(name) ?? "dark";
        var preset = Presets.TryGetValue(key, out var p) ? p : Default;
        Instance = CloneWith(preset, key);
        Instance.Save();
        try { (TuiManager.Instance.ActiveScreen as ChatScreen)?.SyncTheme(); } catch { }
    }
}
