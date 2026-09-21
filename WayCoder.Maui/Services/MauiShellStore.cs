using WayCoder.UI.Shared.Terminal;

namespace WayCoder.Maui.Services;

/// <summary>
/// 命令行页的**显示设置**（Preferences 持久）。
///
/// 为什么需要"固定列数"这一档：命令行页默认按**显示宽度自适应折行**（手机上才好看），
/// 但 1970~90 年代那批 TTY 程序**假设终端是 80×25** —— 表格、边框、进度条都按 80 列排版，
/// 按手机宽度折行会整片错位。给一个开关按**字符格**硬折，老程序才走得对。
/// 折行的纯逻辑在 <see cref="WayCoder.UI.Shared.Terminal.ShellWrap"/>（桌面可自测），
/// 这里只管偏好读写与取值范围。
/// </summary>
public static class MauiShellStore
{
    private const string KeyCols = "shell.cols";
    private const string KeyRows = "shell.rows";
    private const string KeyMode = "shell.mode";
    private const string KeyFont = "shell.font";
    private const string KeyScrollback = "shell.scrollback";

    /// <summary>默认回滚行数 —— 与 ShellPage 原先写死的上限一致（不改变老行为）。</summary>
    public const int DefaultScrollback = 256;

    /// <summary>横向固定的默认列数（IBM VGA 文本模式的宽度，老程序的通用假设）。</summary>
    public const int DefaultCols = 80;

    /// <summary>固定窗口的默认行数。</summary>
    public const int DefaultRows = 25;

    /// <summary>列数候选（不含 0 —— 0 = 自适应，那由**模式**表达，不再是一个"列数"）。</summary>
    public static readonly int[] ColsChoices = [40, 80, 100, 132];

    /// <summary>行数候选（同上，不含 0）。</summary>
    public static readonly int[] RowsChoices = [24, 25, 30, 40, 50];

    /// <summary>回滚行数候选。</summary>
    public static readonly int[] ScrollbackChoices = [256, 500, 1000, 2000, 5000];

    /// <summary>
    /// 字号范围（与 <see cref="WayCoder.UI.Shared.Terminal.ShellWrap.FontSizeForColumns"/> 的钳位一致）。
    ///
    /// 6~96 是**用户定的**：下限 6 是"想尽量多塞几列"（80 列的报表要能一眼看全），
    /// 上限 96 是"想放大看清某一行"。别按"好看的默认值"去收窄它 ——
    /// 命令行页的字号是**无障碍**的一部分，不是排版偏好。
    /// </summary>
    public const double MinFont = 6;
    public const double MaxFont = 96;

    /// <summary>出厂字号（菜单里「重置字号」回到这里；也是首次启动的默认值）。</summary>
    public const double DefaultFont = 12;

    /// <summary>
    /// 当前尺寸模式（持久）。
    ///
    /// 老配置（v0.96.334 之前只有自动/固定两档）在**首次读取**时按"存了什么"迁移：
    /// 存了列数又存了行数 ⇒ 都固定；只存了列数 ⇒ 横向固定；什么都没存 ⇒ 都不固定。
    /// 迁移结果不写回 —— 读出来是哪个模式就是哪个，写回等用户主动切档（少一次无谓的写盘）。
    /// </summary>
    public static ShellSizeMode Mode
    {
        get
        {
            try
            {
                if (Preferences.ContainsKey(KeyMode))
                    return (ShellSizeMode)Preferences.Get(KeyMode, (int)ShellSizeMode.Auto);

                // 迁移判据在 `ShellSize.Migrate`（纯逻辑、桌面自测钉过）——
                // 写在这里的话那段就一条判据也测不到。
                return ShellSize.Migrate(Get(KeyCols, 0), Get(KeyRows, 0));
            }
            catch { return ShellSizeMode.Auto; }
        }
        set
        {
            try { Preferences.Set(KeyMode, (int)value); }
            catch { }
        }
    }

    /// <summary>**生效**列数；0 = 自适应宽度（由模式推导，别直接写 —— 用 <see cref="SetColumns"/>）。</summary>
    public static int Cols => ShellSize.EffectiveCols(Mode, RawCols);

    /// <summary>**生效**行数；0 = 自适应高度（同上，用 <see cref="SetRows"/>）。</summary>
    public static int Rows => ShellSize.EffectiveRows(Mode, RawRows);

    /// <summary>横向固定那一档记住的列数（与模式无关，切走再切回还在）。</summary>
    public static int RawCols
    {
        get => Math.Clamp(Get(KeyCols, DefaultCols), 20, 500);
        private set => Preferences.Set(KeyCols, Math.Clamp(value, 20, 500));
    }

    /// <summary>固定窗口那一档记住的行数。</summary>
    public static int RawRows
    {
        get => Math.Clamp(Get(KeyRows, DefaultRows), 4, 200);
        private set => Preferences.Set(KeyRows, Math.Clamp(value, 4, 200));
    }

    /// <summary>切档（不动各轴已选值）。</summary>
    public static void SetMode(ShellSizeMode mode) => Mode = mode;

    /// <summary>设列数 —— 只为「横向固定 / 固定窗口」两档用；<see cref="ShellSizeMode.Auto"/> 下不生效。</summary>
    public static void SetColumns(int cols) => RawCols = cols;

    /// <summary>设行数 —— 只为「固定窗口」一档用。</summary>
    public static void SetRows(int rows) => RawRows = rows;

    /// <summary>输出字号（缩放）。</summary>
    public static double Font
    {
        get
        {
            try { return Math.Clamp(Preferences.Get(KeyFont, DefaultFont), MinFont, MaxFont); }
            catch { return 12.0; }
        }
        set
        {
            try { Preferences.Set(KeyFont, Math.Clamp(value, MinFont, MaxFont)); }
            catch { }
        }
    }

    /// <summary>
    /// 把字号夹进合法范围 —— **范围只此一处**（`get`/`set` 与捏合路径共用它）。
    /// 捏合过程中的每一拍也要夹（`TerminalGrid` 会按两指距离比算出 3 或 60 这种值），
    /// 但那一拍**不该落盘**（写盘留给手势结束），所以需要这个不落盘的入口。
    /// </summary>
    public static double ClampFont(double value) => Math.Clamp(value, MinFont, MaxFont);

    /// <summary>回滚缓存行上限。</summary>
    public static int Scrollback
    {
        get => Get(KeyScrollback, DefaultScrollback);
        set
        {
            try { Preferences.Set(KeyScrollback, Math.Max(64, value)); }
            catch { }
        }
    }

    private static int Get(string key, int fallback)
    {
        try { return Preferences.Get(key, fallback); }
        catch { return fallback; }
    }

    /// <summary>在一组候选值里循环取下一个（设置项都是"点一下换一档"的形态）。</summary>
    public static int Next(int[] choices, int current)
    {
        var i = Array.IndexOf(choices, current);
        return choices[(i + 1) % choices.Length];
    }

    /// <summary>
    /// 字号候选（"点一下换一档"）—— 菜单里「加大字号 / 减小字号」走的就是这张表。
    ///
    /// 铺满 <see cref="MinFont"/>~<see cref="MaxFont"/> 全程，而且**下半段密、上半段疏**：
    /// 小字号区差 1 磅就是"多塞一列 / 少塞一列"，大字号区差 1 磅肉眼看不出来。
    /// 捏合是连续的（不走这张表），它只服务"点一下"那种离散操作。
    /// </summary>
    public static readonly double[] FontChoices =
        [6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 22, 26, 30, 34, 40, 46, 54, 62, 72, 84, 96];

    /// <summary>
    /// 常用终端尺寸预设 —— 「固定大小」按钮的可选项。
    ///
    /// 头几个是**真实终端的历史规格**，不是随手凑的数：
    ///   · 80×25 —— IBM VGA 文本模式的默认，PC 上绝大多数 DOS 程序假设的就是它
    ///   · 80×24 —— VT100 及一串 Unix 终端；比 80×25 少的那一行是状态行
    ///   · 40×25 —— 早期窄屏机器（部分 8 位机与 Apple II 文本模式）
    /// 老程序是按这些列数**排表格**的，选对了才不会错位；列数是精确的，行数近似（见 ShellPage）。
    /// </summary>
    public static readonly (int Cols, int Rows, string Label)[] SizePresets =
    [
        (80, 25, "80×25 · 经典 VGA 文本"),
        (80, 24, "80×24 · VT100 终端"),
        (40, 25, "40×25 · 窄屏老机器"),
        (100, 30, "100×30"),
        (132, 43, "132×43 · 宽终端"),
    ];

    /// <summary>字号循环取下一档 —— 比当前值大的那一档，到头绕回最小。</summary>
    public static double NextFont(double current)
    {
        foreach (var f in FontChoices)
            if (f > current + 0.01) return f;
        return FontChoices[0];
    }

    /// <summary>字号循环取上一档 —— 比当前值小的那一档，到头绕回最大。</summary>
    public static double PrevFont(double current)
    {
        for (int i = FontChoices.Length - 1; i >= 0; i--)
            if (FontChoices[i] < current - 0.01) return FontChoices[i];
        return FontChoices[^1];
    }

    /// <summary>设置项显示文案（"当前没生效"那一档要写清楚，否则用户以为改了没反应）。</summary>
    /// <summary>列数文案。⚠ 附「当前未生效」——<see cref="ShellSizeMode.Auto"/> 下列数是自适应的，
    /// 这里改的值要等切到另外两档才用得上；不写清楚就是"改了没反应"。</summary>
    public static string ColsText(int v)
        => v <= 0 ? "自适应宽度"
                  : Mode == ShellSizeMode.Auto ? $"{v} 列（切到固定档生效）" : $"{v} 列";

    /// <summary>行数文案（同理：只有「固定窗口」那一档才用得上）。</summary>
    public static string RowsText(int v)
        => v <= 0 ? "自适应高度"
                  : Mode == ShellSizeMode.Fixed ? $"{v} 行" : $"{v} 行（切到固定窗口生效）";

    public static string ScrollbackText(int v) => $"{v} 行";
}
