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

            int enc = (int)root.GetNumber("saveEncoding");
            if (enc >= 0 && enc <= (int)SaveEncoding.Oem) SaveAsEncoding = (SaveEncoding)enc;
            int nl = (int)root.GetNumber("saveNewline");
            if (nl >= 0 && nl <= (int)SaveNewline.Lf) SaveAsNewline = (SaveNewline)nl;
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
            root.Set("saveEncoding", (int)SaveAsEncoding);
            root.Set("saveNewline", (int)SaveAsNewline);
            Global.WriteAllTextAtomic(StorePath, root.ToJson());
        }
        catch { /* 保存失败不崩溃 */ }
    }
}
