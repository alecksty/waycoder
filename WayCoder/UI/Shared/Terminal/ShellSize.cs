namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 命令行窗口的**尺寸模式** —— 三个正交组合。
///
/// 之前只有"自动 / 固定"两档，漏掉了**横向固定**这一档。它不是可有可无的中间态：
/// 老 TTY 程序按 80 列排表格 ⇒ **列必须钉死**；而手机屏幕高度各家不同 ⇒
/// **行没必要跟着钉死**（钉死了输出区上下留白）。用户点名的原话是
/// 「都固定，或者横向固定，或者都不固定」。
/// </summary>
public enum ShellSizeMode
{
    /// <summary>都不固定：列、行都跟着屏幕（新程序默认）。</summary>
    Auto = 0,

    /// <summary>横向固定：列钉死、行跟着屏幕（老程序排表格用这档）。</summary>
    WidthFixed = 1,

    /// <summary>都固定：行列都钉死（80×25 那类真终端规格）。</summary>
    Fixed = 2,
}

/// <summary>
/// 尺寸模式的**纯逻辑**（模式 → 生效列行 / 老配置迁移 / 文案）。
///
/// 为什么单独一份而不是写在 <c>MauiShellStore</c> 里：那一份依赖 MAUI 的 `Preferences`，
/// **桌面自测碰不到**（MAUI 工程不进自测），而这里两条都是"错了也看不出来"的逻辑 ——
/// 派生错了表现为"切了档没反应"，迁移错了表现为"升级后模式莫名其妙变了"。
/// 所以按本仓规矩下沉到 <c>UI/Shared/</c>：四端可复用，且能被断言。
/// </summary>
public static class ShellSize
{
    /// <summary>都不固定时的列数记号（= 交给宽度自适应）。</summary>
    public const int AutoSize = 0;

    /// <summary>
    /// 生效列数 —— **0 = 自适应宽度**。
    ///
    /// ⚠ 生效值**由模式推导**，不另存一份：两份各记一份必然漂（会出现"模式说自适应、
    /// 值却是 80"这种自相矛盾的状态）。
    /// </summary>
    public static int EffectiveCols(ShellSizeMode mode, int rawCols)
        => mode == ShellSizeMode.Auto ? AutoSize : rawCols;

    /// <summary>生效行数 —— **0 = 自适应高度**。只有「都固定」那一档才钉行数。</summary>
    public static int EffectiveRows(ShellSizeMode mode, int rawRows)
        => mode == ShellSizeMode.Fixed ? rawRows : AutoSize;

    /// <summary>
    /// 老配置（只有"自动 / 固定"两档的版本）→ 三档的迁移。
    ///
    /// 判据是**存了什么**：既存了列数又存了行数 ⇒ 那时它是"固定" ⇒ 都固定；
    /// 只存了列数 ⇒ 横向固定；什么都没存 ⇒ 都不固定。
    /// （老版"自动"存的是 `0/0`，老版"固定"存的是真实行列。）
    /// </summary>
    public static ShellSizeMode Migrate(int storedCols, int storedRows)
        => storedCols > 0
            ? (storedRows > 0 ? ShellSizeMode.Fixed : ShellSizeMode.WidthFixed)
            : ShellSizeMode.Auto;

    /// <summary>模式显示名。</summary>
    public static string ModeText(ShellSizeMode mode) => mode switch
    {
        ShellSizeMode.WidthFixed => "横向固定（列固定 · 行自适应）",
        ShellSizeMode.Fixed      => "固定窗口（行列都固定）",
        _                        => "大小自适应（都不固定）",
    };

    /// <summary>点一下换下一个模式（侧栏那一行的"点一下换一档"）。</summary>
    public static ShellSizeMode Next(ShellSizeMode mode) => mode switch
    {
        ShellSizeMode.Auto       => ShellSizeMode.WidthFixed,
        ShellSizeMode.WidthFixed => ShellSizeMode.Fixed,
        _                        => ShellSizeMode.Auto,
    };
}
