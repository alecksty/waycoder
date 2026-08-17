using WayCoder.Infra;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.TUI;

/// <summary>
/// TUI 页面 —— 标记加载结果：窗口 + 按 id 查找控件的 code-behind 入口。
/// 对标 XAML 的「.xaml + .xaml.cs」：布局写 .tui，交互逻辑写在 C# 里通过 Find(id) 订阅事件。
/// </summary>
public sealed class TuiPage
{
    public TuiWindow Window { get; }
    private readonly Dictionary<string, TuiControl> _byId;

    internal TuiPage(TuiWindow window, Dictionary<string, TuiControl> byId)
    {
        Window = window;
        _byId = byId;
    }

    /// <summary>按 id 查找控件。</summary>
    public TuiControl? Find(string id)
        => id != null && _byId.TryGetValue(id, out var c) ? c : null;

    /// <summary>按 id 查找指定类型控件。</summary>
    public T? Find<T>(string id) where T : TuiControl => Find(id) as T;
}

/// <summary>
/// TUI 标记加载器 —— 把 .tui XML 声明式标记解析为 TuiControl 树（类似 Avalonia XAML）。
/// 布局写进资源文件，交互逻辑写进 C# code-behind（Find(id) 拿控件 + 订阅事件）。
///
/// 示例（布局 layout.tui + 交互 code-behind）：
/// <![CDATA[
/// <!-- layout.tui -->
/// <Window title="确认" width="40" height="9">
///   <VBox align="center">
///     <Label id="msg" text="是否继续？" />
///     <HBox align="center" spacing="2">
///       <Button id="ok" text="确定" />
///       <Button id="cancel" text="取消" />
///     </HBox>
///   </VBox>
/// </Window>
///
/// // code-behind
/// var page = TuiMarkup.Load(File.ReadAllText("layout.tui"));
/// page.Find<TuiButton>("ok")!.OnClick = _ => page.Find<TuiLabel>("msg")!.Text = "已确认";
/// page.Find<TuiButton>("cancel")!.OnClick = _ => page.Window.OnClosed?.Invoke();
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

    /// <summary>解析标记并构建页面。根元素为 &lt;Window&gt; 时返回窗口；否则包一层无边框窗口。</summary>
    public static TuiPage Load(string xml)
    {
        var root = Xml.Parse(xml) ?? throw new ArgumentException("标记为空或解析失败");
        var byId = new Dictionary<string, TuiControl>();

        TuiWindow win;
        if (root.Name == "Window")
        {
            win = BuildWindow(root);
            foreach (var child in root.Children)
                if (BuildControl(child, byId) is TuiView v) win.RootView = v;
        }
        else
        {
            var view = (TuiView)BuildControl(root, byId)!;
            win = new TuiWindow { RootView = view, Title = "", Border = WindowBorder.None };
        }

        return new TuiPage(win, byId);
    }

    /// <summary>递归构建控件（不含 Window），并登记 id。</summary>
    private static TuiControl? BuildControl(XNode node, Dictionary<string, TuiControl> byId)
    {
        if (node.Kind != XKind.Element) return null;

        TuiControl c = node.Name switch
        {
            "VBox" => new TuiVBox(),
            "HBox" => new TuiHBox(),
            "Label" => new TuiLabel(Attr(node, "text")),
            "Button" => new TuiButton(Attr(node, "text")),
            "Input" => new TuiInput(),
            "TextArea" => new TuiTextArea(),
            "List" => new TuiList(),
            "Checkbox" => new TuiCheckbox(Attr(node, "text")),
            "Progress" => new TuiProgress(),
            "Separator" => new TuiSeparator(),
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
                if (Int(node, "width") == null) // 未指定宽度 → 按文本显示宽度自动适配
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
        }

        // 登记 id（code-behind 入口）
        var id = node.GetAttr("id");
        if (!string.IsNullOrEmpty(id)) byId[id] = c;

        if (c is TuiView view)
        {
            foreach (var child in node.Children)
                if (BuildControl(child, byId) is { } built) view.Add(built);
        }

        return c;
    }

    private static TuiWindow BuildWindow(XNode node)
    {
        var win = new TuiWindow
        {
            Title = Attr(node, "title"),
            Width = Int(node, "width") ?? 40,
            Height = Int(node, "height") ?? 10,
            Border = ParseBorder(Attr(node, "border")),
            Modal = Bool(node, "modal") ?? false,
            HasMask = Bool(node, "mask") ?? false,
        };
        if (Int(node, "x") is int x) win.X = x;
        if (Int(node, "y") is int y) win.Y = y;
        if (Color(node, "borderColor") is int bc) win.BorderColor = bc;
        return win;
    }

    private static void ApplyCommon(XNode node, TuiControl c)
    {
        if (Int(node, "width") is int w) c.Width = w;
        if (Int(node, "height") is int h) c.Height = h;
        if (Int(node, "flex") is int f) c.Flex = f;
        if (Color(node, "fg") is int fg) c.Fg = fg;
        if (Color(node, "bg") is int bg) c.Bg = bg;
        if (Bool(node, "visible") is bool v) c.Visible = v;

        if (c is TuiLabel lbl && Enum.TryParse<EHAlign>(Attr(node, "align"), true, out var la))
            lbl.TextAlign = la;
    }

    // ── 属性解析辅助 ──

    private static string Attr(XNode node, string key, string def = "") => node.GetAttr(key) ?? def;

    private static int? Int(XNode node, string key)
        => int.TryParse(node.GetAttr(key), out var v) ? v : null;

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
        return null;
    }

    private static WindowBorder ParseBorder(string s) => s.ToLowerInvariant() switch
    {
        "none" => WindowBorder.None,
        "solid" => WindowBorder.Solid,
        "double" => WindowBorder.Double,
        _ => WindowBorder.Rounded,
    };
}
