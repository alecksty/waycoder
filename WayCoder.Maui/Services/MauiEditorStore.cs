using WayCoder.Infra;

namespace WayCoder.Maui.Services;

/// <summary>
/// 编辑器设置：只读阈值 / tab 宽度 / 调试 HUD。持久化到 <c>Global.Home/editor.json</c>。
///
/// **只读阈值的意义**：可编辑要求把文件装进内存行表（编辑需要可写模型），而这正是
/// 「打开 100MB 不 OOM」要避免的事 —— 两者不可兼得。所以这条线同时也是
/// 「编辑按钮什么时候置灰」的线：超过它的文件用索引型只读来源打开，照样能看能滚。
/// 按**字节**判，不是字符数（中文 UTF-8 一个字符 3 字节，按字符算会低估 3 倍）。
/// </summary>
public static class MauiEditorStore
{
    private static string StorePath => Path.Combine(WayCoder.Global.Home, "editor.json");

    /// <summary>默认只读阈值：2MB。手机内存有限，且这个量级下原生控件也不至于卡。</summary>
    public const long DefaultReadOnlyMaxBytes = 2L * 1024 * 1024;

    private static long _readOnlyMaxBytes = DefaultReadOnlyMaxBytes;

    public static long ReadOnlyMaxBytes => _readOnlyMaxBytes;

    // ── 诊断气泡每行几个字符 ──────────────────────────────────────────────────
    //
    // 用户定的规矩：**默认一个气泡就是一行字，超过这个字数才换行**；
    // 并且**不用管会不会超出屏幕**（屏幕本来就能滑动）。
    //
    // ⚠ 这条改的是**气泡宽度的来源**。原先宽度是 `min(300, 画布宽−16)` —— 宽度由屏幕定，
    //   每行几个字再由宽度反推出来（`宽 / 字号×0.62`），于是"一行多少字"随屏幕/字号漂移。
    //   现在是**反过来**：字数由设置定死，宽度 = 字数 × 字宽。这跟"不看屏幕"是同一件事
    //   的两面 —— 只要宽度还受画布约束，"每行 N 字"就随时可能被挤掉。
    public const int DefaultBubbleChars = 32;
    public const int MinBubbleChars = 16;
    public const int MaxBubbleChars = 1024;

    private static int _bubbleChars = DefaultBubbleChars;

    /// <summary>气泡每行最多几个字符（按**显示宽度**算：全角算 2）。</summary>
    public static int BubbleChars => _bubbleChars;

    // ── 诊断气泡总开关 ────────────────────────────────────────────────────────
    //
    // 模型是**「每条诊断一个展开/收起状态」**（见 `CodeCanvasView`）：展开 = 画气泡，
    // 收起 = 在波浪线起点画一个小圆点（点圆点又能展开）。本开关定的是**默认值**，不是「能不能显示」：
    //   · 开 → 默认展开（画气泡）
    //   · 关 → 默认收起（画小圆点）
    // 用户逐条的选择（✕ = 收起、点圆点 = 展开）**覆盖**这个默认值，且**切开关时不清空**。
    //
    // ⚠ 本开关**只管气泡/圆点**，不管行下那条波浪线 —— 波浪线是既有的独立指示
    // （`DrawDiagnosticWave`），数据源虽然同一份，但关掉气泡之后**只剩波浪线可以指示错误**。

    /// <summary>是否**默认展开**诊断气泡 —— **缺键 = 开**（老版本升上来的 editor.json 里没有这个键）。</summary>
    public static bool DefaultExpandBubbles { get; private set; } = true;

    public static void SetDefaultExpandBubbles(bool on)
    {
        DefaultExpandBubbles = on;
        Save();
    }

    /// <summary>把任意值夹进合法区间 —— 纯函数，让"夹取"这条规则只有一处实现。</summary>
    public static int ClampBubbleChars(int n) => Math.Clamp(n, MinBubbleChars, MaxBubbleChars);

    /// <summary>编辑器字号（磅）。默认值与真源一致（<see cref="Controls.EditorTypography.DefaultFontSize"/>）。</summary>
    public static float FontSize { get; private set; } = Controls.EditorTypography.DefaultFontSize;

    /// <summary>
    /// 字号上下限取 <see cref="Controls.EditorTypography"/> 的**那一对常量**，不在这里另写一份
    /// —— 这里原先硬编码 9/28，与排版层的 9/28 是两份平行拷贝，改一处就会「捏合能到 6、
    /// 存盘又被夹回 9」这种半生效的怪状。
    /// </summary>
    public static void SetFontSize(float size)
    {
        FontSize = Math.Clamp(size,
            Controls.EditorTypography.MinFontSize, Controls.EditorTypography.MaxFontSize);
        Save();
    }

    /// <summary>制表符宽度（列）。</summary>
    public static int TabColumns { get; private set; } = 4;

    /// <summary>调试 HUD（可见行区间 / 帧耗时 / 缓存命中 / 索引状态 / 等宽自检）。</summary>
    public static bool ShowDebugHud { get; private set; }

    /// <summary>
    /// 源码编辑时把全角标点（含全角字母数字）自动转半角。**默认开**。
    /// 中文字符串与注释里的标点不受影响（那部分由语法 token 判定后原样保留）。
    /// </summary>
    public static bool FullWidthToHalf { get; private set; } = true;

    /// <summary>保存**源码**时用的编码。</summary>
    public enum SaveEncoding
    {
        /// <summary>沿用文件打开时的编码（默认）—— 不静默转码。</summary>
        Keep,
        Utf8NoBom,
        Utf8Bom,
        Utf16Le,
        Oem,
    }

    /// <summary>保存**源码**时用的换行风格。</summary>
    public enum SaveNewline
    {
        /// <summary>沿用文件打开时的风格（默认）。</summary>
        Keep,
        Crlf,
        Lf,
    }

    /// <summary>
    /// 保存源码时用的编码 / 换行。
    ///
    /// **两个默认值都是「保持原样」**，这不是偷懒：把默认设成某个具体编码，等于
    /// 「用户打开一个 GBK 的老源文件、随手保存一下，它就被悄悄转成了 UTF-8」——
    /// 那是**改动了用户没打算改的东西**，而且不可逆。要哪种编码是用户的决定，
    /// 所以默认留给原样，想强制的人在设置里点一下。
    ///
    /// **只对源码文件生效**（用户明确要求）：Markdown、纯文本、数据文件一律沿用原样。
    /// </summary>
    public static SaveEncoding SaveAsEncoding { get; private set; } = SaveEncoding.Keep;

    public static SaveNewline SaveAsNewline { get; private set; } = SaveNewline.Keep;

    /// <summary>
    /// 保存编码的候选（标签 → 枚举值，顺序即下拉顺序）。
    ///
    /// **放在这里而不是设置页**：设置页的**下拉**与**首页摘要**要用同一份标签，
    /// 两处各写一遍就是「同一规则两处实现」—— 本仓库的头号坑，迟早一处改了另一处没改。
    /// </summary>
    public static readonly (string Label, SaveEncoding Value)[] SaveEncodingOptions =
    [
        ("保持原样（推荐）", SaveEncoding.Keep),
        ("UTF-8（无 BOM）", SaveEncoding.Utf8NoBom),
        ("UTF-8 带 BOM", SaveEncoding.Utf8Bom),
        ("UTF-16 LE", SaveEncoding.Utf16Le),
        ("OEM（系统区域代码页）", SaveEncoding.Oem),
    ];

    /// <summary>保存换行的候选。理由同上。</summary>
    public static readonly (string Label, SaveNewline Value)[] SaveNewlineOptions =
    [
        ("保持原样（推荐）", SaveNewline.Keep),
        ("LF+CR（Windows）", SaveNewline.Crlf),
        ("LF（Unix）", SaveNewline.Lf),
    ];

    /// <summary>枚举 → 下拉里那个标签（找不到就退回枚举名，绝不返回空）。</summary>
    public static string NameOf(SaveEncoding e)
        => SaveEncodingOptions.FirstOrDefault(o => o.Value == e).Label ?? e.ToString();

    public static string NameOf(SaveNewline e)
        => SaveNewlineOptions.FirstOrDefault(o => o.Value == e).Label ?? e.ToString();

    // ── 运行面板（编辑器底部那个「错误列表 / 输出」小窗）的高度 ────────────────
    //
    // 用户要的是「**一个地方能拖动改占比**」—— 原先 `HeightRequest="220"` 写死在 XAML 里，
    // 大屏上显得小、小屏上又占掉一半。拖完要**记住**（不然每次进来都得再拖一遍）。
    //
    // ⚠ **存的是设备无关像素的高度，不是比例**。用户说的是"我拖多少就是多少"，
    //   按比例存会让同一份设置在不同屏上呈现不同的绝对高度，手感漂移。
    //   换屏/转屏后超过当屏上限的，由页面按**当前**高度再 clamp 一次（见 `EditorPage`）。
    public const double DefaultPanelHeight = 220;

    /// <summary>再往下拖也留得住内容的最小高度（约 4~5 行文本 + Tab 行）。</summary>
    public const double MinPanelHeight = 120;

    /// <summary>存盘时的宽松上限 —— 真正的上限得看屏幕，由页面算（这里只挡明显离谱的值）。</summary>
    private const double MaxStoredPanelHeight = 4000;

    private static double _panelHeight = DefaultPanelHeight;

    /// <summary>运行面板高度（设备无关像素）。页面用之前应按当屏上限 clamp 一次。</summary>
    public static double PanelHeight => _panelHeight;

    /// <summary>把任意来源的值夹进合法区间（<c>NaN</c>/<c>Infinity</c> 退回默认值）。</summary>
    private static double ClampStoredPanelHeight(double h)
        => double.IsNaN(h) || double.IsInfinity(h)
            ? DefaultPanelHeight
            : Math.Clamp(h, MinPanelHeight, MaxStoredPanelHeight);

    /// <summary>
    /// 落盘面板高度。**拖动过程中不要每帧调它** —— 视图跟着手指走是 `HeightRequest` 的事，
    /// 这里只在**松手那一下**写一次（写盘是原子替换，每帧一次纯属浪费）。
    /// </summary>
    public static void SetPanelHeight(double h)
    {
        double v = ClampStoredPanelHeight(h);
        if (v == _panelHeight) return;   // 没变就不写
        _panelHeight = v;
        Save();
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(StorePath)) return;
            var root = Json.Parse(File.ReadAllText(StorePath));
            if (root == null) return;
            long mb = (long)root.GetNumber("readOnlyMaxMB");
            if (mb > 0) _readOnlyMaxBytes = mb * 1024 * 1024;
            double fs = root.GetNumber("fontSize");
            if (fs >= Controls.EditorTypography.MinFontSize && fs <= Controls.EditorTypography.MaxFontSize)
                FontSize = (float)fs;
            int tab = (int)root.GetNumber("tabColumns");
            if (tab is >= 1 and <= 16) TabColumns = tab;
            ShowDebugHud = root.GetBool("debugHud");
            // ⚠ 这一项**默认开**，所以「缺键」与「键=false」必须分开判：老版本升上来的
            // editor.json 里没有这个键，写成裸 GetBool 会得到 false ⇒ 功能「看着做了、
            // 其实静默关着」，而用户还以为是没生效。Has 判的就是这个区别。
            FullWidthToHalf = !root.Has("fullWidthToHalf") || root.GetBool("fullWidthToHalf");

            // 气泡每行字数：**缺键 = 用默认值**（不是 0）—— 老版本升上来的 editor.json 里
            // 没有这个键，写成 `(int)root.GetNumber("bubbleChars")` 会得到 0，
            // 再被 Clamp 夹成 16 ⇒ 用户的界面"自己变窄了"，而他根本没改过这项。
            if (root.Has("bubbleChars")) _bubbleChars = ClampBubbleChars((int)root.GetNumber("bubbleChars"));

            // 气泡**默认展开**（总开关）：与 fullWidthToHalf 同理 —— **默认开**，所以「缺键」与
            // 「键=false」必须分开判：裸 GetBool 会让老配置静默变成「气泡全收起来了」，而用户没关过。
            DefaultExpandBubbles = !root.Has("defaultExpandBubbles") || root.GetBool("defaultExpandBubbles");

            int enc = (int)root.GetNumber("saveEncoding");
            if (enc >= 0 && enc <= (int)SaveEncoding.Oem) SaveAsEncoding = (SaveEncoding)enc;
            int nl = (int)root.GetNumber("saveNewline");
            if (nl >= 0 && nl <= (int)SaveNewline.Lf) SaveAsNewline = (SaveNewline)nl;

            // 运行面板高度：**缺键 = 用默认值**（不是 0）—— 与 `bubbleChars` 同一条理由：
            // 老版本升上来的 editor.json 里没有这个键，写成裸 GetNumber 会得到 0，
            // 再被 clamp 夹成 MinPanelHeight ⇒ 用户的运行面板"自己变矮了"，而他根本没拖过。
            if (root.Has("panelHeight")) _panelHeight = ClampStoredPanelHeight(root.GetNumber("panelHeight"));
        }
        catch { /* 配置损坏 → 用默认值 */ }
    }

    /// <summary>设置只读阈值（MB）并落盘。</summary>
    public static void SetReadOnlyMaxMB(int mb)
    {
        _readOnlyMaxBytes = Math.Clamp(mb, 1, 512) * 1024L * 1024;
        Save();
    }

    public static void SetDebugHud(bool on)
    {
        ShowDebugHud = on;
        Save();
    }

    public static void SetFullWidthToHalf(bool on)
    {
        FullWidthToHalf = on;
        Save();
    }

    public static void SetBubbleChars(int n)
    {
        _bubbleChars = ClampBubbleChars(n);
        Save();
    }

    public static void SetSaveEncoding(SaveEncoding enc)
    {
        SaveAsEncoding = enc;
        Save();
    }

    public static void SetSaveNewline(SaveNewline nl)
    {
        SaveAsNewline = nl;
        Save();
    }

    private static void Save()
    {
        try
        {
            var root = JNode.Object();
            root.Set("readOnlyMaxMB", (int)(_readOnlyMaxBytes / (1024 * 1024)));
            root.Set("fontSize", (double)FontSize);
            root.Set("tabColumns", TabColumns);
            root.Set("debugHud", ShowDebugHud);
            root.Set("fullWidthToHalf", FullWidthToHalf);
            root.Set("bubbleChars", _bubbleChars);
            root.Set("defaultExpandBubbles", DefaultExpandBubbles);
            root.Set("saveEncoding", (int)SaveAsEncoding);
            root.Set("saveNewline", (int)SaveAsNewline);
            root.Set("panelHeight", _panelHeight);
            Global.WriteAllTextAtomic(StorePath, root.ToJson());
        }
        catch { /* 保存失败不崩溃 */ }
    }
}
