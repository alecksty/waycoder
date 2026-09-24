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
    /// 这一行是不是**画面行** —— 整行可见字符**全是空格**，只靠底色作画。
    ///
    /// 为什么单立一条判据：命令行页的输出有两类，**排版规则相反**：
    ///   · 文本行（`ls -l`、日志、表格）—— 按列折行是对的；
    ///   · **画面行**（nyancat / tetris / 一切全屏程序）—— 每行就是屏幕上的一行像素，
    ///     **折一下就整幅画散架**（一行变两行、后面的行整体下移，图形被斜切）。
    /// 实测症状正是「有彩色了，但有点乱」：猫的彩虹与身体都出来了，形状却是剪开的。
    ///
    /// 判据取"可见字符**全是空格** + **有 `«…»` 标记**"两条同时成立：
    ///   · 只有空格没标记 = 空行（该折不该折都无所谓，交给文本那条路）
    ///   · 有可见字符 = 文本行（`printf("  #")` 那种带字的底色段也走文本，折它没坏处）
    /// </summary>
    public static bool IsPictureLine(string line)
    {
        bool sawTag = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\xAB')
            {
                if (i + 1 < line.Length && line[i + 1] == '\xAB') { i++; continue; }  // 转义的字面 «
                int close = line.IndexOf('\xBB', i + 1);
                if (close > i) { sawTag = true; i = close; continue; }
            }
            if (c != ' ') return false;      // 见到任何可见字符 ⇒ 不是画面行
        }
        return sawTag;
    }

    /// <summary>
    /// 一行的**可见宽度**（字符格，全角算 2）—— 与 <see cref="WrapMarkup"/> 同一把尺子。
    ///
    /// 用来给 Label 定"最小不许窄于"的宽度：画面行比屏幕宽时**不能让它折**，
    /// 得把 Label 撑到画面那么宽、让 ScrollView 横向滚（与固定列数那条路同一个做法）。
    /// </summary>
    public static int VisibleWidth(string line)
    {
        int w = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\xAB')
            {
                if (i + 1 < line.Length && line[i + 1] == '\xAB') { w += 1; i++; continue; }
                int close = line.IndexOf('\xBB', i + 1);
                if (close > i) { i = close; continue; }
            }
            w += AnsiString.CharWidth(new System.Text.Rune(c));
        }
        return w;
    }

    /// <summary>
    /// 等宽字符的**推进量 ÷ 字号** —— 也就是"一个字符格有多宽"，屏幕上几列的换算基准。
    ///
    /// **它就是输出区那个画布画格子用的同一个数**（`TerminalGrid.CellWidth = 字号 × 本值`，
    /// 那边直接引这个常量、不再另写一份）。两处必须是同一个值 —— 否则"折行时以为放得下
    /// N 个字符"与"实际画出来只占多少宽度"各说各话，症状正是用户报的
    /// 「**换行的地方距离右边界还很远**」：折行按 0.6 倍字号算、绘制按 0.5 倍画，
    /// 每列少摊 17% ⇒ 62 列的文字只铺满屏宽的 83%，右边空出一大片（真机实测空 200px ≈ 15 格）。
    ///
    /// 取值 **0.5**：`Sarasa Mono SC` 的设计值就是半角 0.5em、全角 1em（见 CLAUDE.md 的
    /// 「字体自检」那条），而输出区现在是 `TerminalGrid` **自绘**的（位置一律自己按格算、
    /// 不问平台度量）⇒ 设计值就是画出来的值。
    ///
    /// ⚠ 这里曾经是 **0.6**，依据写的是"`Label` 在 Android 上实测排版约 0.58 倍字号" ——
    ///   那是输出区**还交给 `Label` 排版**时的数。输出区改成自绘画布之后那条依据就不存在了，
    ///   而 0.6 一直留着，于是画布的尺子（0.5）与折行的尺子（0.6）不是同一把。
    ///   **凡是从"平台排版实测"得来的系数，底层渲染换掉之后必须连同依据一起复审 ——
    ///   系数本身不会报错，只会让画面在某个方向上"差一点点"。**
    /// </summary>
    public const double CharAspect = 0.5;

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
    /// ⚠ 特意**减 1 列**留余量：平台排版会把每个字形的推进量**取整到整数像素**
    ///   （本仓在移动端编辑器那条链上量过：设计 12.72px 的字形实际按 13px 走），
    ///   而取整是**往大**的 —— 满行时最后一格可能比算出来的宽一点点。
    ///   减 1 列只是每行少一个字；不减则会让最后那个字挤出屏宽、触发一次多余的回折
    ///   （固定档还会变成横向滚动）。
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
    /// <remarks>
    /// 钳位范围与 <c>MauiShellStore.MinFont/MaxFont</c>（6~96）**必须一致** ——
    /// 两边不一样的话，将来真接上这条线时会被夹到另一个区间去，
    /// 而那种"改了没反应"最难查（本仓那条：同一个范围写在两处，迟早漂）。
    /// </remarks>
    public static double FontSizeForColumns(double availableWidth, int cols, double min = 6, double max = 96)
    {
        if (cols <= 0 || availableWidth <= 0) return 0;
        var size = availableWidth / (cols * CharAspect);
        size = Math.Round(size * 2) / 2;
        return Math.Clamp(size, min, max);
    }
}
