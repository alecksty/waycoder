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

    /// <summary>编辑器字号（磅）。</summary>
    public static float FontSize { get; private set; } = 13f;

    public static void SetFontSize(float size)
    {
        FontSize = Math.Clamp(size, 9f, 28f);
        Save();
    }

    /// <summary>制表符宽度（列）。</summary>
    public static int TabColumns { get; private set; } = 4;

    /// <summary>调试 HUD（可见行区间 / 帧耗时 / 缓存命中 / 索引状态 / 等宽自检）。</summary>
    public static bool ShowDebugHud { get; private set; }

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
            if (fs >= 9 && fs <= 28) FontSize = (float)fs;
            int tab = (int)root.GetNumber("tabColumns");
            if (tab is >= 1 and <= 16) TabColumns = tab;
            ShowDebugHud = root.GetBool("debugHud");
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

    private static void Save()
    {
        try
        {
            var root = JNode.Object();
            root.Set("readOnlyMaxMB", (int)(_readOnlyMaxBytes / (1024 * 1024)));
            root.Set("fontSize", (double)FontSize);
            root.Set("tabColumns", TabColumns);
            root.Set("debugHud", ShowDebugHud);
            Global.WriteAllTextAtomic(StorePath, root.ToJson());
        }
        catch { /* 保存失败不崩溃 */ }
    }
}
