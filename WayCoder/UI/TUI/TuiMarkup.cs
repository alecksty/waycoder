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

    /// <summary>
    /// 语义色 token：随主题切换（运行时读 TuiTheme.Current）。
    /// 标记里用 fg/bg 指定语义名（accent/danger/success/…），换主题后同一份标记自动换色。
    /// </summary>
    private static readonly Dictionary<string, Func<int>> SemanticColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accent"] = () => TuiTheme.Current.WindowBorderFocused,
        ["primary"] = () => TuiTheme.Current.WindowBorderFocused,
        ["panel"] = () => TuiTheme.Current.WindowBg,
        ["text"] = () => TuiTheme.Current.DialogFg,
        ["muted"] = () => AnsiColors.BrightBlack,
        ["danger"] = () => TuiTheme.Current.DialogErrorBorder,
        ["error"] = () => TuiTheme.Current.DialogErrorBorder,
        ["success"] = () => TuiTheme.Current.DialogSuccessBorder,
        ["warning"] = () => TuiTheme.Current.DialogWarnBorder,
        ["warn"] = () => TuiTheme.Current.DialogWarnBorder,
        ["info"] = () => TuiTheme.Current.DialogInfoBorder,
    };

    /// <summary>当前标记绑定的变量（{key} 占位符替换用），AsyncLocal 保证并发安全。</summary>
    private static readonly System.Threading.AsyncLocal<Dictionary<string, string>?> BindVars = new();

    /// <summary>解析标记，按根元素类型构建对应对象（App/Screen→屏幕、Window/Dialog→窗口、控件→视图）。vars 用于 {key} 占位符替换。</summary>
    public static TuiMarkupResult Load(string xml, Dictionary<string, string>? vars = null)
    {
        BindVars.Value = vars;
        try
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
                    var rootCtrl = BuildControl(root, byId);
                    if (rootCtrl is TuiView rootView)
                        return new TuiMarkupResult(null, null, rootView, byId);
                    // 根是叶子控件（Label/Button 等，如 cell 模板）：包装进 VBox，统一返回视图
                    if (rootCtrl != null)
                    {
                        var leafBox = new TuiVBox { Width = rootCtrl.Width, Height = rootCtrl.Height };
                        leafBox.Add(rootCtrl);
                        return new TuiMarkupResult(null, null, leafBox, byId);
                    }
                    throw new ArgumentException($"无法构建标记根元素 {root.Name}");
            }
        }
        finally
        {
            BindVars.Value = null;
        }
    }

    /// <summary>从 .tui 文件加载。</summary>
    public static TuiMarkupResult LoadFile(string path) => Load(File.ReadAllText(path));

    /// <summary>加载单元格模板（.tui 片段）并绑定数据，返回视图（供 list/tree/table 自定义单元格用）。</summary>
    public static TuiView LoadCell(string markup, Dictionary<string, string> vars)
        => (TuiView)Load(markup, vars).View!;

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
        FixupLayout(screen.RootView);
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
            BorderStyle = ParseBorder(Attr(node, "border")),
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

        // 修复居中容器宽度（HBox/VBox align=center/right 需宽度 ≥ 内容宽）
        FixupLayout(win.RootView);
        // 设置所属窗口引用（动画控件「父窗口焦点」门控用）
        SetWindowRef(win.RootView, win);

        // 内容自适应尺寸（size="content"）：按控件树自然尺寸计算窗口宽高；默认 size="screen" 用 width/height/scale
        if (Attr(node, "size") == "content" && win.RootView != null)
        {
            var (cw, ch) = MeasureContent(win.RootView);
            int borderW = win.BorderStyle == WindowBorder.None ? 0 : 2;
            int borderH = win.BorderStyle == WindowBorder.None ? 0 : 2;
            int titleH = win.ShowTitle && !string.IsNullOrEmpty(win.Title) ? 1 : 0;
            win.Width = cw + borderW;
            win.Height = ch + borderH + titleH;
        }

        // 快捷键：遍历 RootView 树注册按钮 shortcut，窗口 shortcut 映射到关闭
        RegisterButtonShortcuts(win, win.RootView);
        if (ParseShortcutKey(Attr(node, "shortcut")) is ConsoleKey wsc)
            win.RegisterShortcut(wsc, () => win.OnClosed?.Invoke());

        return win;
    }

    /// <summary>测量视图树的自然尺寸（VBox 纵向堆叠、HBox 横向排列，递归子视图）。</summary>
    private static (int w, int h) MeasureContent(TuiView view)
    {
        if (view is TuiVBox vbox)
        {
            int w = 0, h = 0;
            for (var i = 0; i < vbox.Children.Count; i++)
            {
                var (cw, ch) = MeasureChild(vbox.Children[i]);
                w = Math.Max(w, cw);
                h += ch + (i > 0 ? vbox.Spacing : 0);
            }
            return (w, h);
        }
        if (view is TuiHBox hbox)
        {
            int w = 0, h = 0;
            for (var i = 0; i < hbox.Children.Count; i++)
            {
                var (cw, ch) = MeasureChild(hbox.Children[i]);
                w += cw + (i > 0 ? hbox.Spacing : 0);
                h = Math.Max(h, ch);
            }
            return (w, h);
        }
        return (view.Width, view.Height);
    }

    private static (int w, int h) MeasureChild(TuiControl c)
        => c is TuiView v ? MeasureContent(v) : (c.Width, c.Height);

    /// <summary>递归设置控件树的所属窗口引用（供动画控件的焦点门控）。</summary>
    private static void SetWindowRef(TuiView view, TuiWindow win)
    {
        view.Window = win;
        foreach (var child in view.Children)
        {
            child.Window = win;
            if (child is TuiView cv) SetWindowRef(cv, win);
        }
    }

    /// <summary>
    /// 修复居中容器的宽度：HBox/VBox 的 align="center"/"right" 需要 Width ≥ 内容宽，
    /// 否则 ContentHAlign/ChildHAlign 用 (Width-totalW)/2 算出负偏移、子控件被裁剪。
    /// 递归（自底向上）：先修子视图，再修自身，使父容器测量用到已修正的子宽度。
    /// </summary>
    private static void FixupLayout(TuiView view)
    {
        foreach (var child in view.Children)
            if (child is TuiView cv) FixupLayout(cv);

        if (view is TuiHBox hbox && hbox.ContentHAlign != EHAlign.Left)
        {
            var (w, _) = MeasureContent(hbox);
            if (hbox.Width < w) hbox.Width = w;
        }
        else if (view is TuiVBox vbox && vbox.ChildHAlign != EHAlign.Left)
        {
            var (w, _) = MeasureContent(vbox);
            if (vbox.Width < w) vbox.Width = w;
        }
    }

    /// <summary>递归遍历视图树，注册按钮 shortcut → OnClick（code-behind 后设置 OnClick，lambda 延迟读取）。</summary>
    private static void RegisterButtonShortcuts(TuiWindow win, TuiView? view)
    {
        if (view == null) return;
        foreach (var child in view.Children)
        {
            if (child is TuiButton btn && btn.ShortcutKey is ConsoleKey k)
                win.RegisterShortcut(k, () => btn.OnClick?.Invoke(btn));
            if (child is TuiView cv) RegisterButtonShortcuts(win, cv);
        }
    }

    /// <summary>解析快捷键：单字母→ConsoleKey（大小写归一），其它用枚举名（Enter/Escape/F1…）。</summary>
    private static ConsoleKey? ParseShortcutKey(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (s.Length == 1 && char.IsLetter(s[0]))
            return (ConsoleKey)char.ToUpperInvariant(s[0]);
        if (Enum.TryParse<ConsoleKey>(s, true, out var k)) return k;
        return null;
    }

    /// <summary>递归构建控件（不含 Window/Dialog/Screen），并登记 id。</summary>
    private static TuiControl? BuildControl(XNode node, Dictionary<string, TuiControl> byId)
    {
        if (node.Kind != XKind.Element) return null;

        TuiControl? c = node.Name switch
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
            "DataList" => new TuiDataList(),
            "TreeView" => new TuiTreeView(),
            "TableList" => new TuiTableList(),
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
            "AnimatedText" => new TuiAnimatedText(),
            "Markdown" => new WayCoder.UI.Tui.Controls.TuiMarkdown(Attr(node, "text")),
            "TitleBar" => new TuiTitleBar(),
            "StatusBar" => new TuiStatusBar(),
            "Banner" => new TuiBanner(),
            "Scrollbar" => new TuiScrollbar(),
            "Icon" => new TuiIcon(Attr(node, "glyph", "•")),
            "Spacer" => new TuiLabel("") { Flex = 1 },
            "ListView" => new TuiListView(),
            "DynamicBar" => new TuiDynamicBar(),
            "PromptBar" => new TuiPromptBar(),
            "SidePanel" => new TuiSidePanel(),
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
                var btn = (TuiButton)c;
                btn.Text = Attr(node, "text");
                var sc = ParseShortcutKey(Attr(node, "shortcut"));
                if (sc != null)
                {
                    btn.ShortcutKey = sc;
                    var keyChar = char.ToUpperInvariant(Attr(node, "shortcut").Trim()[0]);
                    int idx = btn.Text.IndexOf(keyChar, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) btn.UnderlineIndex = idx;
                }
                break;
            case "Input":
                ((TuiInput)c).Text = Attr(node, "value", Attr(node, "text"));
                break;
            case "TextArea":
                var ta = (TuiTextArea)c;
                ta.Text = Attr(node, "value", Attr(node, "text"));
                ta.Placeholder = Attr(node, "placeholder");
                if (Bool(node, "showLineNumbers") is bool sln) ta.ShowLineNumbers = sln;
                break;
            case "List":
                var list = (TuiList)c;
                var items = Attr(node, "items");
                if (items.Length > 0) list.Items = [.. items.Split(',')];
                if (Int(node, "selected") is int sel) list.SelectedIndex = sel;
                break;
            case "DataList":
                var dl = (TuiDataList)c;
                var dlItems = Attr(node, "items");
                if (dlItems.Length > 0)
                {
                    var arr = dlItems.Split(',');
                    for (int i = 0; i < arr.Length; i++)
                        dl.Items.Add(new Dictionary<string, string> { ["text"] = arr[i].Trim(), ["index"] = i.ToString() });
                }
                dl.CellMarkup = Attr(node, "cell");
                if (Int(node, "selected") is int dlSel) dl.SelectedIndex = dlSel;
                break;
            case "TreeView":
                var tv = (TuiTreeView)c;
                tv.CellMarkup = Attr(node, "cell");
                if (Int(node, "indent") is int tvi) tv.IndentWidth = tvi;
                var tvItems = Attr(node, "items");
                if (tvItems.Length > 0)
                {
                    // items="文档>概览,文档>入门,文档>进阶>性能" → 沿路径建树，中间节点自动展开
                    foreach (var path in tvItems.Split(','))
                    {
                        var parts = path.Split('>');
                        var cur = tv.RootNodes.FirstOrDefault(n => n.Text == parts[0].Trim());
                        if (cur == null) cur = tv.AddRoot(parts[0].Trim());
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var name = parts[i].Trim();
                            var child = cur.Children.FirstOrDefault(n => n.Text == name);
                            if (child == null) child = cur.Add(new TuiTreeNode(name));
                            cur.IsExpanded = true;
                            cur = child;
                        }
                    }
                }
                break;
            case "TableList":
                var tl = (TuiTableList)c;
                var tlCols = Attr(node, "columns");
                if (tlCols.Length > 0)
                {
                    foreach (var seg in tlCols.Split(','))
                    {
                        var sp = seg.LastIndexOf(':');
                        if (sp <= 0) { tl.AddColumn(seg.Trim(), 12); continue; }
                        var t = seg[..sp].Trim();
                        int.TryParse(seg[(sp + 1)..].Trim(), out var w);
                        tl.AddColumn(t, w > 0 ? w : 12);
                    }
                }
                var tlItems = Attr(node, "items");
                if (tlItems.Length > 0)
                {
                    foreach (var row in tlItems.Split('|'))
                        tl.AddRow([.. row.Split(',').Select(s => s.Trim())]);
                }
                tl.CellMarkup = Attr(node, "cell");
                if (Bool(node, "showHeader") is bool tlSh) tl.ShowHeader = tlSh;
                if (Int(node, "selected") is int tlSel) tl.SelectedIndex = tlSel;
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
            case "AnimatedText":
                var at = (TuiAnimatedText)c;
                at.Text = Attr(node, "text");
                if (Enum.TryParse<AnimatedTextMode>(Attr(node, "mode"), true, out var am)) at.Mode = am;
                if (Int(node, "frameMs") is int fm) at.FrameMs = Math.Max(TuiAnimatedText.MinFrameMs, fm);
                var frames = Attr(node, "frames");
                if (frames.Length > 0) at.CustomFrames = [.. frames.Split(',')];
                if (Int(node, "width") == null) // 未指定宽度 → 按文本宽自动（spinner 额外留 4 列）
                    at.Width = AnsiHelper.DisplayWidth(at.Text) + 4;
                if (Bool(node, "directWrite") is bool dw) at.DirectWrite = dw;
                break;
            case "Line":
                ((TuiLine)c).Vertical = Bool(node, "vertical") ?? false;
                ((TuiLine)c).Style = ParseBorder(Attr(node, "style", "single"));
                break;
            case "Rect":
                ((TuiRect)c).Style = ParseBorder(Attr(node, "style", "single"));
                break;
            case "Markdown":
                var md = (WayCoder.UI.Tui.Controls.TuiMarkdown)c;
                md.Role = Attr(node, "role", "assistant");
                if (Bool(node, "plainText") is bool mdpt) md.IsPlainText = mdpt;
                if (Bool(node, "isError") is bool mde) md.IsError = mde;
                if (Int(node, "maxWidth") is int mw) md.MaxWidth = mw;
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
                ((TuiTitleBar)c).GitBranch = Attr(node, "gitBranch");
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
            case "Separator":
                var sep = (TuiSeparator)c;
                var sepChar = Attr(node, "lineChar");
                if (sepChar.Length > 0) sep.LineChar = sepChar;
                if (Color(node, "lineColor") is int sepLc) sep.LineColor = sepLc;
                break;
            case "ListView":
                var lv = (TuiListView)c;
                if (Bool(node, "autoScroll") is bool lvAs) lv.IsAutoScrollToEnd = lvAs;
                if (Int(node, "itemSpacing") is int lvIs) lv.ItemSpacing = lvIs;
                // 聊天项（TuiListItem：角色/时间戳/续接/缩进）由 code-behind AddItem 动态填，不在标记声明
                break;
            case "DynamicBar":
                // 运行态（Agent 状态/工具/压缩进度）由 ChatScreen.Render 每帧同步，标记无静态属性
                break;
            case "PromptBar":
                var pb = (TuiPromptBar)c;
                if (Int(node, "maxVisible") is int pbMv) pb.MaxVisible = pbMv;
                if (Int(node, "itemHeight") is int pbIh) pb.ItemHeight = pbIh;
                if (Color(node, "separatorColor") is int pbSc) pb.SeparatorColor = pbSc;
                break;
            case "SidePanel":
                var sPanel = (TuiSidePanel)c;
                if (Int(node, "borderWidth") is int spBw) sPanel.BorderWidth = spBw;
                if (Color(node, "borderColor") is int spBc) sPanel.BorderColor = spBc;
                if (Bool(node, "panelVisible") is bool spPv) sPanel.PanelVisible = spPv;
                // 分区（PanelSection）由 code-behind RefreshSidePanel() 填充；SidePanel 是叶子控件，不支持嵌套 Section 声明
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
        if (Bool(node, "floating") is bool fl) c.Floating = fl;

        if (c is TuiLabel lbl && Enum.TryParse<EHAlign>(Attr(node, "align"), true, out var la))
            lbl.TextAlign = la;
    }

    // ── 属性解析辅助 ──

    private static string Attr(XNode node, string key, string def = "")
    {
        var s = node.GetAttr(key) ?? def;
        return BindVars.Value is { } vars && s.Contains('{') ? Substitute(s, vars) : s;
    }

    /// <summary>替换 {key} 占位符。</summary>
    private static string Substitute(string s, Dictionary<string, string> vars)
    {
        foreach (var kv in vars)
            s = s.Replace("{" + kv.Key + "}", kv.Value);
        return s;
    }

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
        var s = AttrOrNull(node, key);
        if (s == null) return null;
        if (SemanticColors.TryGetValue(s, out var fn)) return fn(); // 语义色（随主题）
        if (Colors.TryGetValue(s, out var c)) return c;
        if (int.TryParse(s, out var n)) return n;
        if (TryParseRgbColor(s, out var rgb)) return rgb;
        return null;
    }

    /// <summary>取属性（含 {key} 占位符替换），缺失返回 null。</summary>
    private static string? AttrOrNull(XNode node, string key)
    {
        var raw = node.GetAttr(key);
        if (raw == null) return null;
        return BindVars.Value is { } vars && raw.Contains('{') ? Substitute(raw, vars) : raw;
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
