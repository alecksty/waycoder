namespace WayCoder.Infra;

/// <summary>
/// 全角 → 半角（标点与全角字母数字）。**唯一实现** —— 编辑器那四个「把输入框内容写进模型」
/// 的地方都调它，别处要用也调它。
///
/// **为什么强调是唯一实现**：本仓库反复栽在「同一规则两处实现、只改了其中一处」上。
/// 归一化一旦分家，就会出现「打字路径转了、粘贴路径没转」这种一半生效的怪状。
///
/// **映射是严格 1 字符换 1 字符** —— 这条性质很值钱：长度不变意味着调用方的光标下标
/// 永远不需要换算。编辑器正是靠它做到「只把归一化后的文本写进模型和画布，**不回写**
/// 平台输入框」（回写会打断中文输入法的组合态）。
/// </summary>
public static class FullWidthText
{
    /// <summary>
    /// 归一化一行。 <paramref name="keep"/>[i] 为 true 的字符**原样保留** ——
    /// 撑起「字符串与注释里的中文标点不要动」：那些位置由调用方（按语法 token 判定）标出来。
    ///
    /// <paramref name="keep"/> 短于 <paramref name="line"/> 时，超出的部分按「不保留」处理。
    /// </summary>
    public static string Normalize(string line, ReadOnlySpan<bool> keep)
    {
        if (line.Length == 0) return line;

        char[]? buf = null;
        for (int i = 0; i < line.Length; i++)
        {
            if (i < keep.Length && keep[i]) continue;
            if (!TryHalfWidth(line[i], out char half)) continue;
            // 复用同一个数组：逐字符复制一次，之后的改动都原地做
            buf ??= line.ToCharArray();
            buf[i] = half;
        }
        return buf == null ? line : new string(buf);
    }

    /// <summary>
    /// 整行不做保留判定时的归一化（测试与「不在乎字符串」的场景用）。
    /// </summary>
    public static string Normalize(string line) => Normalize(line, default);

    /// <summary>
    /// 单字符映射。<c>false</c> = 这个字符不该转（原样保留）。
    ///
    /// 分两类：
    /// ① <c>U+FF01..U+FF5E</c> 整段减 <c>0xFEE0</c> —— 这一段是「ASCII 的全角形式」，
    ///    含全角标点（，。；：（）！？…）与**全角字母数字**（０-９Ａ-ｚ）。字母数字一并转：
    ///    受保护的字符串/注释已经挡住了中文正文，剩下的场景里全角字母数字几乎必然是输入法误触。
    /// ② U+3000 段的标点单独映射 —— 它们**不在**上面那一整段里，是最容易漏掉的一类。
    ///
    /// **刻意排除**（没有 ASCII 对应物，猜一个等于静默产出错代码）：
    /// <list type="bullet">
    /// <item>全角空格 <c>U+3000</c> —— 中文排版用的真空格，转掉是**改坏内容**而不是改对</item>
    /// <item>「」『』（U+300C..300F）、《》〈〉（U+300A..300B）、【】〔〕（U+3010..3011）——
    ///       猜成 <c>"</c> 会凭空造出一个字符串定界符</item>
    /// <item>— … · ・（U+2014/2026/00B7/30FB）与 ￥（U+FFE5，本就在 FF5E 之外）</item>
    /// </list>
    /// </summary>
    public static bool TryHalfWidth(char c, out char half)
    {
        // ① ASCII 的全角形式
        if (c >= '！' && c <= '～')
        {
            half = (char)(c - 0xFEE0);
            return true;
        }

        // ② U+3000 段（以及引号）单独列 —— 注意**不含** U+3000 全角空格
        switch (c)
        {
            case '。': half = '.';  return true;   // 。 —— 用户点名要转的一个，也最容易漏
            case '、': half = ',';  return true;   // 、
            case '“':                             // “
            case '”': half = '"';  return true;   // ”
            case '‘':                             // ‘
            case '’': half = '\''; return true;   // ’
            case '〜': half = '~';  return true;   // 〜（日文波折号，与 FF5E 同形不同码）
            default: half = c; return false;
        }
    }

    /// <summary>
    /// 这一行里有没有「可能被转换」的字符 —— 给调用方做**短路**用。
    ///
    /// 编辑器每敲一个键都要决定要不要归一化，而归一化要先 tokenize 出「字符串/注释」区间。
    /// 绝大多数按键（英文代码、退格、方向键）行里压根没有全角字符，这一趟扫描只有几十次
    /// 字符比较，比 tokenize 便宜得多。
    /// </summary>
    public static bool MayContainCandidate(string line)
    {
        foreach (var c in line)
        {
            if (c >= '！' && c <= '～') return true;
            if (c is '。' or '、' or '“' or '”' or '‘' or '’' or '〜')
                return true;
        }
        return false;
    }
}
