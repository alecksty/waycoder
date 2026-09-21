using System.Text;

namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 命令行输出的**固定列数硬折行** —— 兼容老程序用的。
///
/// 为什么需要它：命令行页现在把输出交给 <c>Label</c> 按**显示宽度**自动折行（自适应宽度，
/// 也是默认行为）。但 1970~90 年代那批 TTY 程序**假设终端是 80×25** —— 它们的表格、
/// 边框、进度条是按 80 列排版的，按手机宽度折行会整片错位。
/// 所以给一个「固定 N 列」的开关：输出按**字符格**硬折，与真实终端一致。
///
/// 三条必须守住的规矩：
/// ① **按显示宽度算，不是按字符数** —— 全角占 2 格。宽度真源是
///    <see cref="AnsiString.CharWidth"/>（全仓唯一），这里不另写一张表。
/// ② **绝不折在 `«…»` 标签内部** —— 标签被劈成两半，渲染端认不出来，
///    整段标记会**原样显示**（用户看到的是 `«red»` 这种字面文本）。
/// ③ `««` / `»»` 是**转义的字面量**（`AnsiHelper.Esc` 的产物），占 1 格，
///    不能当成标签的开闭。
/// </summary>
public static class ShellWrap
{
    /// <summary>
    /// 把一段文本按 <paramref name="cols"/> 列硬折成多行。
    /// <paramref name="cols"/> &lt;= 0 时原样返回一行（= 不折，交给显示层自适应）。
    /// </summary>
    public static List<string> WrapMarkup(string text, int cols)
    {
        var outp = new List<string>();
        if (string.IsNullOrEmpty(text)) { outp.Add(""); return outp; }
        if (cols <= 0) { outp.Add(text); return outp; }

        var runes = text.EnumerateRunes().ToArray();
        var line = new StringBuilder();
        var width = 0;
        var inTag = false;

        for (var i = 0; i < runes.Length; i++)
        {
            var r = runes[i];

            // ── 标签开闭（`««`/`»»` 是转义，不算开闭）──
            if (r.Value == '«')
            {
                if (i + 1 < runes.Length && runes[i + 1].Value == '«')
                {
                    // 转义的字面 « ：占 1 格
                    if (width + 1 > cols && width > 0) { outp.Add(line.ToString()); line.Clear(); width = 0; }
                    line.Append("««"); width += 1; i++;
                    continue;
                }
                inTag = true;
                line.Append(r.ToString());
                continue;
            }

            if (r.Value == '»')
            {
                if (i + 1 < runes.Length && runes[i + 1].Value == '»')
                {
                    if (width + 1 > cols && width > 0) { outp.Add(line.ToString()); line.Clear(); width = 0; }
                    line.Append("»»"); width += 1; i++;
                    continue;
                }
                inTag = false;
                line.Append(r.ToString());
                continue;
            }

            // ── 标签内部：零宽、不折 ──
            if (inTag) { line.Append(r.ToString()); continue; }

            // ── 正文：按显示宽度累计 ──
            var w = AnsiString.CharWidth(r);
            if (width + w > cols && width > 0)
            {
                outp.Add(line.ToString());
                line.Clear();
                width = 0;
            }
            line.Append(r.ToString());
            width += w;
        }

        outp.Add(line.ToString());
        return outp;
    }

    /// <summary>
    /// 等宽字符的**推进量 ÷ 字号**。
    ///
    /// ⚠ 这里刻意**不取理论值 0.5**（`Sarasa Mono SC` 的设计值确实是 0.5em，见 CLAUDE.md
    /// 的「字体自检」那条）：那是"画出来"的宽度，而 `<c>Label</c>` 在 Android 上实际排版出来的
    /// 推进量**更宽** —— 实测（1080×2400 / 420dpi，字号 12）一行约 51 字，即 ≈ 0.58 倍字号。
    /// 取 0.6 比实测略保守：**宁小勿大** —— 估小了只是每行少放一个字，估大了会再多折一行。
    /// </summary>
    private const double CharAspect = 0.6;

    /// <summary>
    /// 自适应模式的列数**硬底线**：一行至少这么多个字符。
    ///
    /// 用户定的规矩：字号放大到"一行放不下 32 个字符"时，**不许把列数压到 32 以下、
    /// 也不许把字缩回去**，而是让内容**超出屏幕**（横向滚动）。
    /// 理由很实在：一屏只有十来个字的话，`ls -l` 那种按列排的输出已经完全没法看了 ——
    /// 宁可左右滚，也不要"字大但什么也读不出来"。
    /// </summary>
    public const int MinAdaptiveCols = 32;

    /// <summary>列数上限 —— 防屏宽异常（如折叠屏展开瞬间报了个巨大值）时算出天量列数。</summary>
    public const int MaxCols = 500;

    /// <summary>
    /// **自适应模式的列数** —— 按屏宽与字号算"这一屏能放下多少字符格"。
    ///
    /// 这就是"自适应"的准确含义：它**不是没有列数**，而是列数由屏宽推出来。
    /// 算出来之后折行按它走（而不是让 `Label` 各按各的度量去折），
    /// 于是"屏幕上看到几列"与"程序以为终端有几列"是同一个数 —— 报表、进度条才对得上。
    ///
    /// ⚠ 特意**减 1 列**留余量：`CharAspect` 是实测估的，估小一点只是每行少一个字；
    ///   估大了会让最后那个字挤出屏宽、触发一次多余的回折（固定档还会变成横向滚动）。
    /// 下限是 <see cref="MinAdaptiveCols"/>（32）而**不是**"能放多少算多少" ——
    /// 放不下就让内容超出屏幕去横向滚（见那个常量的说明）。
    /// </summary>
    public static int ColumnsForWidth(double availableWidth, double fontSize)
    {
        if (availableWidth <= 0 || fontSize <= 0) return 0;
        var n = (int)(availableWidth / (fontSize * CharAspect)) - 1;
        return Math.Clamp(n, MinAdaptiveCols, MaxCols);
    }

    /// <summary>
    /// <paramref name="cols"/> 列**应有的像素宽度** —— 给内容一个显式宽度用。
    ///
    /// 为什么不是靠 <c>LineBreakMode.NoWrap</c> 去"禁止折行"：
    /// ⚠ MAUI 的 `NoWrap` 在 Android 上会走 `setSingleLine(true)` ⇒ **整段只显示第一行**
    ///   （实测：命令行页的提示行与提示符全没、只剩最上面那行）。
    /// 正解是给 Label 一个"比视口宽"的显式宽度，让 ScrollView 去横向滚 ——
    /// 真终端也是这么做的：内容宽度 = 列数 × 字符宽，视口不够就滚。
    /// </summary>
    public static double WidthForColumns(int cols, double fontSize)
        => cols <= 0 || fontSize <= 0 ? 0 : cols * fontSize * CharAspect;

    /// <summary>
    /// 让 <paramref name="cols"/> 列**正好铺满** <paramref name="availableWidth"/> 时的字号。
    ///
    /// ⚠ **当前没有接线**：「固定大小」那一档走的不是"缩字号塞进屏宽"，而是
    /// **不折行 + 横向滚动**（用户定调：固定大小意味着字符格真的固定，超出就滚）。
    /// 两条路目的相反，不能混 —— 缩字号是牺牲格子尺寸去迁就屏宽，而固定大小的全部意义
    /// 就在于格子尺寸固定。留着它是因为 `CharAspect` 那条实测值有价值，
    /// 将来若真的要做"字号自动适配"那一档可以直接用。
    /// </summary>
    public static double FontSizeForColumns(double availableWidth, int cols, double min = 7, double max = 22)
    {
        if (cols <= 0 || availableWidth <= 0) return 0;
        var size = availableWidth / (cols * CharAspect);
        size = Math.Round(size * 2) / 2;
        return Math.Clamp(size, min, max);
    }
}
