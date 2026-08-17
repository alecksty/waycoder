using WayCoder.Infra;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.TUI;

/// <summary>
/// TUI 标记加载结果 —— 根元素可能是 App/Screen（→ Screen）、Window/Dialog（→ Window）、
/// 或控件（→ View）。附带按 id 查找控件的 code-behind 入口。
/// </summary>
public sealed class TuiMarkupResult
{
    public TuiScreen? Screen { get; }
    public TuiWindow? Window { get; }
    public TuiView? View { get; }

    private readonly Dictionary<string, TuiControl> _byId;

    internal TuiMarkupResult(TuiScreen? screen, TuiWindow? window, TuiView? view, Dictionary<string, TuiControl> byId)
    {
        Screen = screen; Window = window; View = view; _byId = byId;
    }

    /// <summary>按 id 查找控件。</summary>
    public TuiControl? Find(string id)
        => id != null && _byId.TryGetValue(id, out var c) ? c : null;

    /// <summary>按 id 查找指定类型控件。</summary>
    public T? Find<T>(string id) where T : TuiControl => Find(id) as T;
}

/// <summary>标记创建的屏幕 —— 根视图与浮层窗口在激活时组装（窗口需在 Activate 拿到尺寸后 OnResize）。</summary>
public sealed class TuiMarkupScreen : TuiScreen
{
    private readonly List<TuiWindow> _windows = [];

    public void AddMarkupWindow(TuiWindow win) => _windows.Add(win);

    public override void Activate()
    {
        base.Activate();
        foreach (var win in _windows)
            if (!Windows.Contains(win)) AddWindow(win);
    }
}

/// <summary>
/// TUI 标记加载器 —— 把 .tui XML 声明式标记解析为 TUI 对象树（类似 Avalonia XAML）。
/// 根元素支持 App / Screen / Window / Dialog / 控件；布局写资源文件，交互写 C# code-behind。
///
/// 示例：
/// <![CDATA[
/// <App>
///   <Screen>
///     <VBox>
///       <Label id="msg" text="Hello" />
///       <Button id="ok" text="确定" />
///     </VBox>
///     <Dialog id="about" title="关于" width="40" height="8">
///       <Label text="WayCoder" />
///     </Dialog>
///   </Screen>
/// </App>
/// ]]>
/// </summary>
public static class TuiMarkup
{
    private static readonly Dictionary<string, int> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = AnsiColors.Black, ["red"] = AnsiColors.Red, ["green"] = AnsiColors.Green,
        ["yellow"] = AnsiColors.Yellow, ["blue"] = AnsiColors.Blue, ["magenta"] = AnsiColors.Magenta,
        ["cyan"] = AnsiColors.Cyan, ["white"] = AnsiColors.White,
        ["grey"] = AnsiColors.BrightBlack, ["gray"] = AnsiColors.BrightBlack,
        ["brightred"] = AnsiColors.BrightRed, ["brightgreen"] = AnsiColors.BrightGreen,
        ["brightyellow"] = AnsiColors.BrightYellow, ["brightblue"] = AnsiColors.BrightBlue,
        ["brightmagenta"] = AnsiColors.BrightMagenta, ["brightcyan"] = AnsiColors.BrightCyan,
        ["brightwhite"] = AnsiColors.BrightWhite,
    };

    /// <summary>解析标记，按根元素类型构建对应对象（App/Screen→屏幕、Window/Dialog→窗口、控件→视图）。</summary>
    public static TuiMarkupResult Load(string xml)
    {
        var root = Xml.Parse(xml) ?? throw new ArgumentException("标记为空或解析失败");
        var byId = new Dictionary<string, TuiControl>();

        switch (root.Name)
        {
            case "App":
            case "Screen":
                return new TuiMarkupResult(BuildScreen(root, byId), null, null, byId);
            case "Window":
            case "Dialog":
                return new TuiMarkupResult(null, BuildWindow(root, byId), null, byId);
            default:
                var view = (TuiView)BuildControl(root, byId)!;
                return new TuiMarkupResult(null, null, view, byId);
        }
    }

    /// <summary>从 .tui 文件加载。</summary>
    public static TuiMarkupResult LoadFile(string path) => Load(File.ReadAllText(path));

    // ── 屏幕 ──

    private static TuiMarkupScreen BuildScreen(XNode node, Dictionary<string, TuiControl> byId)
    {
        var screen = new TuiMarkupScreen();
        TuiView? rootView = null;

        foreach (var child in node.Children)
        {
            if (child.Kind != XKind.Element) continue;
            switch (child.Name)
            {
                case "App" or "Screen": // 嵌套（罕见）：忽略
                    break;
                case "Window":
                case "Dialog":
                    screen.AddMarkupWindow(BuildWindow(child, byId));
                    break;
                default:
                    if (rootView == null && BuildControl(child, byId) is TuiView v)
                        rootView = v;
                    break;
            }
        }

        screen.RootView = rootView ?? new TuiVBox { Width = 10, Height = 1 };
        return screen;
    }

    // ── 窗口/对话框 ──

    private static TuiWindow BuildWindow(XNode node, Dictionary<string, TuiControl> byId)
    {
        bool isDialog = node.Name == "Dialog";
        var win = new TuiWindow
        {
            Title = Attr(node, "title"),
            Width = Int(node, "width") ?? 40,
            Height = Int(node, "height") ?? 10,
            Border = ParseBorder(Attr(node, "border")),
            Modal = Bool(node, "modal") ?? isDialog,        // Dialog 默认模态
            HasMask = Bool(node, "mask") ?? isDialog,        // Dialog 默认带遮罩
        };
        if (Int(node, "x") is int x) win.X = x;
        if (Int(node, "y") is int y) win.Y = y;
        if (Color(node, "borderColor") is int bc) win.BorderColor = bc;

        // 可变尺寸：min/max 尺寸 + 比例缩放（scale = 宽占终端比例，scaleY = 高占终端比例）
        if (Int(node, "minWidth") is int minw) win.MinWidth = minw;
        if (Int(node, "minHeight") is int minh) win.MinHeight = minh;
        if (Int(node, "maxWidth") is int maxw) win.MaxWidth = maxw;
        if (Int(node, "maxHeight") is int maxh) win.MaxHeight = maxh;
        if (Double(node, "scale") is double s) win.XScale = s;
        if (Double(node, "scaleY") is double sy) win.YScale = sy;

        // 线框：自定义 6 字符 + 渐变（gradientStart/gradientEnd 用 RGB TrueColor）
        var custom = Attr(node, "customBorder");
        if (custom.Length > 0) win.CustomBorder = custom;
        if (Bool(node, "gradient") is bool grad) win.GradientBorder = grad;
        if (Color(node, "gradientStart") is int gs) win.GradientStart = gs;
        if (Color(node, "gradientEnd") is int ge) win.GradientEnd = ge;

        foreach (var child in node.Children)
            if (BuildControl(child, byId) is TuiView v) { win.RootView = v; break; }

        return win;
    }

    /// <summary>递归构建控件（不含 Window/Dialog/Screen），并登记 id。</summary>
    private static TuiControl? BuildControl(XNode node, Dictionary<string, TuiControl> byId)
    {
        if (node.Kind != XKind.Element) return null;

        TuiControl c = node.Name switch
        {
            "VBox" => new TuiVBox(),
            "HBox" => new TuiHBox(),
            "Grid" => new TuiGrid(),
            "WrapPanel" => new TuiWrapPanel(),
            "Label" => new TuiLabel(Attr(node, "text")),
            "Button" => new TuiButton(Attr(node, "text")),
            "Input" => new TuiInput(),
            "TextArea" => new TuiTextArea(),
            "List" => new TuiList(),
            "Checkbox" => new TuiCheckbox(Attr(node, "text")),
            "ComboBox" => new TuiComboBox(),
            "RadioGroup" => new TuiRadioGroup(),
            "SeekBar" => new TuiSeekBar(),
            "Progress" => new TuiProgress(),
            "Separator" => new TuiSeparator(),
            "Line" => new TuiLine(),
            "Rect" => new TuiRect(),
            "Panel" => new TuiPanel(),
            "Spinner" => new TuiSpinner(Attr(node, "text")),
            "Markdown" => new WayCoder.UI.Tui.Controls.TuiMarkdown(Attr(node, "text")),
            "TitleBar" => new TuiTitleBar(),
            "StatusBar" => new TuiStatusBar(),
            "Banner" => new TuiBanner(),
            "Scrollbar" => new TuiScrollbar(),
            "Icon" => new TuiIcon(Attr(node, "glyph", "•")),
            "Spacer" => new TuiLabel("") { Flex = 1 },
            _ => null,
        };
        if (c == null) return null;

        ApplyCommon(node, c);

        switch (node.Name)
        {
            case "Label":
                var lbl = (TuiLabel)c;
                lbl.Text = Attr(node, "text");
                if (Int(node, "width") == null)
                    lbl.Width = AnsiHelper.DisplayWidth(lbl.Text) + 2;
                break;
            case "Button":
                ((TuiButton)c).Text = Attr(node, "text");
                break;
            case "Input":
                ((TuiInput)c).Text = Attr(node, "value", Attr(node, "text"));
                break;
            case "TextArea":
                ((TuiTextArea)c).Text = Attr(node, "value", Attr(node, "text"));
                break;
            case "List":
                var list = (TuiList)c;
                var items = Attr(node, "items");
                if (items.Length > 0) list.Items = [.. items.Split(',')];
                if (Int(node, "selected") is int sel) list.SelectedIndex = sel;
                break;
            case "Progress":
                if (double.TryParse(Attr(node, "value", Attr(node, "percent")), out var pv))
                    ((TuiProgress)c).Percent = pv;
                break;
            case "Checkbox":
                ((TuiCheckbox)c).Checked = Bool(node, "checked") ?? false;
                break;
            case "ComboBox":
                var cb = (TuiComboBox)c;
                var cbItems = Attr(node, "items");
                if (cbItems.Length > 0) cb.Options = [.. cbItems.Split(',')];
                if (Int(node, "selected") is int cbs) cb.SelectedIndex = cbs;
                break;
            case "RadioGroup":
                var rg = (TuiRadioGroup)c;
                var rgItems = Attr(node, "items");
                if (rgItems.Length > 0) rg.Options = [.. rgItems.Split(',')];
                if (Int(node, "selected") is int rgs) rg.SelectedIndex = rgs;
                break;
            case "SeekBar":
                var sb = (TuiSeekBar)c;
                if (Int(node, "min") is int mn) sb.MinValue = mn;
                if (Int(node, "max") is int mx) sb.MaxValue = mx;
                if (Int(node, "value") is int sv) sb.Value = sv;
                break;
            case "Panel":
                ((TuiPanel)c).Title = Attr(node, "title");
                break;
            case "Line":
                ((TuiLine)c).Vertical = Bool(node, "vertical") ?? false;
                ((TuiLine)c).Style = ParseBorder(Attr(node, "style", "single"));
                break;
            case "Rect":
                ((TuiRect)c).Style = ParseBorder(Attr(node, "style", "single"));
                break;
            case "Markdown":
                if (Int(node, "maxWidth") is int mw) ((WayCoder.UI.Tui.Controls.TuiMarkdown)c).MaxWidth = mw;
                break;
            case "VBox":
                if (Int(node, "spacing") is int vsp) ((TuiVBox)c).Spacing = vsp;
                if (Enum.TryParse<EHAlign>(Attr(node, "align"), true, out var va))
                    ((TuiVBox)c).ChildHAlign = va;
                break;
            case "HBox":
                if (Int(node, "spacing") is int hsp) ((TuiHBox)c).Spacing = hsp;
                if (Enum.TryParse<EHAlign>(Attr(node, "align"), true, out var ha))
                    ((TuiHBox)c).ContentHAlign = ha;
                break;
            case "Grid":
                var grid = (TuiGrid)c;
                var gCols = Attr(node, "columns");
                if (gCols.Length > 0) grid.ColumnDefinitions = gCols;
                var gRows = Attr(node, "rows");
                if (gRows.Length > 0) grid.RowDefinitions = gRows;
                if (Int(node, "colGap") is int cg) grid.ColGap = cg;
                if (Int(node, "rowGap") is int rgap) grid.RowGap = rgap;
                break;
            case "WrapPanel":
                var wp = (TuiWrapPanel)c;
                if (Attr(node, "direction").ToLowerInvariant() == "vertical")
                    wp.Direction = Orientation.Vertical;
                if (Int(node, "colSpacing") is int cs) wp.ColumnSpacing = cs;
                if (Int(node, "rowSpacing") is int rs) wp.RowSpacing = rs;
                if (Int(node, "itemWidth") is int iw) wp.ItemWidth = iw;
                if (Int(node, "itemHeight") is int ih) wp.ItemHeight = ih;
                break;
            case "TitleBar":
                ((TuiTitleBar)c).Title = Attr(node, "title");
                ((TuiTitleBar)c).CenterText = Attr(node, "center");
                ((TuiTitleBar)c).Version = Attr(node, "version");
                break;
            case "StatusBar":
                ((TuiStatusBar)c).HintText = Attr(node, "hint");
                ((TuiStatusBar)c).RightText = Attr(node, "right");
                break;
            case "Banner":
                ((TuiBanner)c).Title = Attr(node, "title");
                ((TuiBanner)c).Subtitle = Attr(node, "subtitle");
                break;
            case "Scrollbar":
                var scr = (TuiScrollbar)c;
                if (Int(node, "content") is int ch) scr.ContentHeight = ch;
                if (Int(node, "viewport") is int vp) scr.ViewportHeight = vp;
                if (Int(node, "offset") is int off) scr.ScrollOffset = off;
                if (Bool(node, "autoHide") is bool ah) scr.AutoHide = ah;
                break;
        }

        var id = node.GetAttr("id");
        if (!string.IsNullOrEmpty(id)) byId[id] = c;

        if (c is TuiView view)
        {
            foreach (var child in node.Children)
                if (BuildControl(child, byId) is { } built) view.Add(built);
        }

        return c;
    }

    private static void ApplyCommon(XNode node, TuiControl c)
    {
        if (Int(node, "width") is int w) c.Width = w;
        if (Int(node, "height") is int h) c.Height = h;
        if (Int(node, "flex") is int f) c.Flex = f;
        if (Color(node, "fg") is int fg) c.Fg = fg;
        if (Color(node, "bg") is int bg) c.Bg = bg;
        if (Bool(node, "visible") is bool v) c.Visible = v;
        if (Bool(node, "focused") is bool fo) c.Focused = fo;
        if (Bool(node, "disabled") is bool dis) c.IsEnabled = !dis;
        if (Bool(node, "bold") is bool b) c.Bold = b;
        if (Bool(node, "italic") is bool it) c.Italic = it;
        if (Bool(node, "underline") is bool un) c.Underline = un;
        if (Bool(node, "dim") is bool dm) c.Dim = dm;

        if (c is TuiLabel lbl && Enum.TryParse<EHAlign>(Attr(node, "align"), true, out var la))
            lbl.TextAlign = la;
    }

    // ── 属性解析辅助 ──

    private static string Attr(XNode node, string key, string def = "") => node.GetAttr(key) ?? def;

    private static int? Int(XNode node, string key)
        => int.TryParse(node.GetAttr(key), out var v) ? v : null;

    private static double? Double(XNode node, string key)
        => double.TryParse(node.GetAttr(key), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static bool? Bool(XNode node, string key)
    {
        var s = node.GetAttr(key);
        if (s == null) return null;
        return s.Equals("true", StringComparison.OrdinalIgnoreCase)
            || s == "1" || s.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static int? Color(XNode node, string key)
    {
        var s = node.GetAttr(key);
        if (s == null) return null;
        if (Colors.TryGetValue(s, out var c)) return c;
        if (int.TryParse(s, out var n)) return n;
        if (TryParseRgbColor(s, out var rgb)) return rgb;
        return null;
    }

    /// <summary>解析 RGB 颜色：#RRGGBB、#RGB、rgb(r,g,b) → TrueColor 码（≥0x1000000）。</summary>
    private static bool TryParseRgbColor(string s, out int code)
    {
        code = 0;
        string hex;
        if (s.StartsWith('#') && s.Length is 4 or 7)
        {
            hex = s[1..];
            if (hex.Length == 3)
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }
        else if (s.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(')'))
        {
            var parts = s[4..^1].Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0].Trim(), out var r) &&
                int.TryParse(parts[1].Trim(), out var g) &&
                int.TryParse(parts[2].Trim(), out var b))
            {
                code = AnsiTty.RgbCode(r, g, b);
                return true;
            }
            return false;
        }
        else return false;

        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var v))
        {
            code = AnsiTty.RgbCode((v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
            return true;
        }
        return false;
    }

    private static WindowBorder ParseBorder(string s) => s.ToLowerInvariant() switch
    {
        "none" => WindowBorder.None,
        "single" => WindowBorder.Single,
        "double" => WindowBorder.Double,
        "rounded" => WindowBorder.Rounded,
        "thick" => WindowBorder.Thick,
        "solid" => WindowBorder.Solid,
        "dotted" => WindowBorder.Dotted,
        "dashed" => WindowBorder.Dashed,
        "ascii" => WindowBorder.Ascii,
        "slash" => WindowBorder.Slash,
        "triangle" => WindowBorder.Triangle,
        _ => WindowBorder.Rounded,
    };
}
