namespace WayCoder.UI.Shared;

/// <summary>
/// 编辑器里那几件"手感"事的**纯逻辑**：回车后的自动缩进、光标处的括号配对。
///
/// ## 为什么放这里而不是留在 EditorPage 里
///
/// 三条理由，缺一条都够：
/// ① **可自测** —— 放在 `WayCoder.Maui/Pages/EditorPage.xaml.cs` 里就没有任何测试能碰到它们
///   （那个工程不进桌面自测）。而这两件事都是"边界一多、肉眼看不出来"的典型：
///   全空白行、`{` 后面跟注释、光标贴在右括号上、嵌套三层、扫描上限刚好卡住……
/// ② **四端共享** —— 桌面 TUI / AvaLonia GUI / Web 的编辑器也要同样的行为。
///   各写一份的下场本仓库见得太多了（"同一规则两处实现"是头号坑）。
/// ③ **纯逻辑不依赖平台** —— 它只吃字符串、吐字符串/坐标，没有 IME、没有画布。
///
/// ⚠ 需要写回平台控件的部分（比如自动补右括号要改 `LineEditor.Text`）**不在这里** ——
/// 那属于"什么时候、在哪个线程上写"，是调用方的事。这里只回答"该变成什么样"。
/// </summary>
public static class TextEditAssist
{
    /// <summary>
    /// 回车断行时，新行该带什么缩进。
    ///
    /// 规则只有两条，都是"所有语言都不会错"的那种：
    /// ① 继承左半段的**前导空白**（空格/制表符原样复制，不把 tab 换成空格 ——
    ///    那会让一份 tab 缩进的文件按一次回车就变成空格缩进，diff 里全是噪音）；
    /// ② 左半段以 `{` `(` `[` 收尾时再多一级 —— 这三种开括号在多行写法里后面必然跟一层缩进。
    ///    收尾判断前先 `TrimEnd`，因为 `if (x) {` 后面常带空格。
    ///
    /// ⚠ **刻意不做"语言感知"**：Python 的 `:`、Ruby 的 `do`/`then`、Lua 的 `then` 各要一套规则，
    /// 那是另一个量级的事；而缩进错了比不缩进更烦人（要手工退回去）。宁少勿错。
    /// </summary>
    /// <param name="left">被劈开那一行里、光标**左边**的那半段。</param>
    /// <param name="tabColumns">用空格缩进时，一级几列（来自编辑器设置）。</param>
    public static string IndentForNewLine(string left, int tabColumns)
    {
        var i = 0;
        while (i < left.Length && (left[i] == ' ' || left[i] == '\t')) i++;
        var indent = left[..i];

        var body = left[i..].TrimEnd();
        if (body.Length > 0 && (body[^1] == '{' || body[^1] == '(' || body[^1] == '['))
        {
            // 跟着这一行自己的风格加一级：本来用 tab 的就加 tab，用空格的加 tabColumns 个。
            // 混着来的（前面是 tab、后面补空格）在多数编辑器里也都是这么做的。
            indent += indent.Contains('\t') ? "\t" : new string(' ', Math.Max(1, tabColumns));
        }
        return indent;
    }

    /// <summary>三种括号的配对表（开 ↔ 闭）。</summary>
    public static readonly IReadOnlyDictionary<char, char> BracketPairs = new Dictionary<char, char>
    {
        ['('] = ')', [')'] = '(',
        ['['] = ']', [']'] = '[',
        ['{'] = '}', ['}'] = '{',
    };

    /// <summary>光标该拿哪个括号去配对（认不出返回 null）。</summary>
    /// <param name="col">0 基列号。</param>
    /// <returns>(该括号所在的列, 它自己, 它配对的那个, 是否向后扫)。</returns>
    public static (int Col, char Open, char Close, bool Forward)? BracketAtCaret(string line, int col)
    {
        if (col < 0 || col > line.Length) return null;

        // 先看光标**左边**那个字符 —— "刚打完左括号、光标停在它后面"是最常见的情形；
        // 不是括号才看光标**正下方**那个（"光标停在右括号前面"）。
        if (col > 0 && BracketPairs.TryGetValue(line[col - 1], out var m1))
        {
            var ch = line[col - 1];
            return IsOpen(ch) ? (col - 1, ch, m1, true) : (col - 1, ch, m1, false);
        }
        if (col < line.Length && BracketPairs.TryGetValue(line[col], out var m2))
        {
            var ch = line[col];
            return IsOpen(ch) ? (col, ch, m2, true) : (col, ch, m2, false);
        }
        return null;

        static bool IsOpen(char c) => c is '(' or '[' or '{';
    }

    /// <summary>
    /// 从 <paramref name="line"/>/<paramref name="col"/> 处的括号出发，按方向找出配对的另一个。
    ///
    /// 用**深度计数**而不是"找下一个同款字符"：`f(g(x))` 里后者会指向错误的那一个。
    /// 扫描有**字符上限**（<paramref name="maxScan"/>），超了就放弃 —— 这是每敲一个键都会跑的路径，
    /// 宁可不标，也不能为了一个高亮把输入卡住。
    ///
    /// ⚠ **不跳过字符串与注释** —— 那需要真正的词法分析。代价是 `puts("(")` 这种会算错一次配对；
    /// 它只影响"高亮画在哪两个格子"，不改任何文本，所以宁可偶尔标错也不为此引一层分词。
    /// （真要做，`WayCoder.UI.Tui.Edit.Syntax.ProtectedSpans` 是现成的入口。）
    /// </summary>
    /// <param name="lineAt">按行号取文本（返回 null 表示这一行取不到，直接放弃）。</param>
    /// <param name="lineCount">总行数。</param>
    public static (long Line, int Col)? MatchBracket(
        Func<long, string?> lineAt, long lineCount, long line, int col,
        char open, char close, bool forward, int maxScan)
    {
        var depth = 0;
        var scanned = 0;

        if (forward)
        {
            for (var i = line; i < lineCount; i++)
            {
                var t = lineAt(i);
                if (t == null) return null;
                for (var c = (i == line ? col : 0); c < t.Length; c++)
                {
                    if (++scanned > maxScan) return null;
                    if (t[c] == open) depth++;
                    else if (t[c] == close && --depth == 0) return (i, c);
                }
            }
        }
        else
        {
            for (var i = line; i >= 0; i--)
            {
                var t = lineAt(i);
                if (t == null) return null;
                for (var c = (i == line ? col : t.Length - 1); c >= 0; c--)
                {
                    if (++scanned > maxScan) return null;
                    if (t[c] == open) depth++;
                    else if (t[c] == close && --depth == 0) return (i, c);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 「刚敲进去一个字符」时，要不要自动补上右半边；要的话补什么。
    ///
    /// 判据（每一条都是为了不帮倒忙）：
    /// ① 只认 `(` `[` `{` `"` `'` —— 其余字符没有配对的另一半；
    /// ② 光标右边**紧挨着**的不能是字母/数字/下划线：那说明用户在已有内容中间插了一个，
    ///    补上去等于把后面的东西劈开（最常见的是 `don't` 里那个撇号）；
    /// ③ 单引号/双引号在**词中间**（左边是字母数字）不补 —— 那几乎都是撇号或缩写，
    ///    不是要开一个字符串。这条单独列出来，因为它是引号独有的坑。
    /// </summary>
    /// <param name="inserted">刚插进去的那个字符。</param>
    /// <param name="before">它左边紧挨着的字符（没有就传 '\0'）。</param>
    /// <param name="after">它右边紧挨着的字符（没有就传 '\0'）。</param>
    /// <returns>要补的那个字符；不补返回 null。</returns>
    public static char? AutoCloseFor(char inserted, char before, char after)
    {
        var closer = inserted switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            '"' => '"',
            '\'' => '\'',
            _ => '\0',
        };
        if (closer == '\0') return null;

        // 右边紧挨着字母数字下划线 ⇒ 用户是在已有内容中间插入，别把后面的劈开
        if (after != '\0' && (char.IsLetterOrDigit(after) || after == '_')) return null;

        // 引号在词中间 = 撇号/缩写，不是开字符串
        if ((inserted == '\'' || inserted == '"') &&
            before != '\0' && (char.IsLetterOrDigit(before) || before == '_'))
            return null;

        return closer;
    }
}
