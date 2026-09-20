namespace WayCoder.UI.Shared;

/// <summary>
/// Markdown AST 节点。
/// </summary>
public abstract class MdNode
{
    public int StartLine { get; set; }
}

/// <summary>标题 # ~ ######（ATX），或 Setext 下划线式（=== / ---）</summary>
public class MdHeading : MdNode
{
    public int Level { get; set; }  // 1-6
    public string Text { get; set; } = "";
    /// <summary>Setext 形态（正文下一行 ===/---）—— 渲染层可据此区分（GUI 可画下划线）</summary>
    public bool Setext { get; set; }
}

/// <summary>普通段落</summary>
public class MdParagraph : MdNode
{
    public string Text { get; set; } = "";
}

/// <summary>代码块 ```lang\ncode\n```</summary>
public class MdCodeBlock : MdNode
{
    public string Language { get; set; } = "";
    public string Code { get; set; } = "";
}

/// <summary>表格 | a | b |</summary>
public class MdTable : MdNode
{
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
    /// <summary>
    /// 每列对齐（来自分隔行 <c>:---</c> / <c>:---:</c> / <c>---:</c>）。
    /// 0=默认、1=左、2=中、3=右。长度可能短于列数（缺的按 0 处理）。
    /// </summary>
    public List<int> Alignments { get; set; } = [];
}

/// <summary>列表项 - 或 * 或 1.（任务清单用 Checked 标记 [x]/[ ]）</summary>
public class MdListItem : MdNode
{
    public string Text { get; set; } = "";
    public bool Ordered { get; set; }
    public int OrderNum { get; set; }
    public int Level { get; set; }  // 缩进级别 (0/1/2...)
    public bool? Checked { get; set; }  // null=普通列表；true=[x]；false=[ ]
}

/// <summary>分割线 ---</summary>
public class MdRule : MdNode { }

/// <summary>
/// 引用块 <c>&gt;</c>。内容**递归解析成块** —— 引用里可以嵌代码块 / 列表 / 嵌套引用
/// （CommonMark：引用是容器块）。<see cref="Text"/> 保留为「内层纯文本」（不含内层 <c>&gt;</c>
/// 标记），只想要一行字的消费者照旧能用；要画嵌套缩进的消费者走 <see cref="Blocks"/>。
/// </summary>
public class MdBlockQuote : MdNode
{
    public string Text { get; set; } = "";
    /// <summary>剥掉一层 <c>&gt;</c> 之后的内部块（可能含嵌套 MdBlockQuote）</summary>
    public List<MdNode> Blocks { get; set; } = [];
}

/// <summary>«tag»…«/» 块级标记（跨多行的推理/思考内容，保留原始换行与空行）</summary>
public class MdMarkup : MdNode
{
    public string Text { get; set; } = "";
    public int Style { get; set; }
}

// ================================================================
// Markdown 解析器
// ================================================================

/// <summary>
/// 轻量 Markdown 解析器 —— 纯 C# 实现，AOT 兼容，零依赖。
/// 支持：标题、段落、代码块、表格、列表、分割线、内联格式。
/// </summary>
public static class MarkdownParser
{
    /// <summary>将 Markdown 文本解析为 AST 节点列表</summary>
    public static List<MdNode> Parse(string markdown)
    {
        var nodes = new List<MdNode>();
        if (string.IsNullOrWhiteSpace(markdown)) return nodes;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // 空行跳过
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            // «tag»…«/» 块级标记：跨多行（含空行/代码/列表）的推理内容，按原始文本整体渲染。
            // 与内联用法区分：仅当开标签在行首、且本行内无闭合 «/» 时才走块级（否则交 ParseInline 内联处理）。
            if (line.TrimStart().StartsWith('\xAB'))
            {
                var tline = line.TrimStart();
                int openClose = tline.IndexOf('\xBB');
                if (openClose > 1)
                {
                    string openTag = tline[1..openClose].Trim();
                    int style = openTag == "/" ? 0 : MapMarkupTag(openTag);
                    if (style > 0)
                    {
                        string rest = tline[(openClose + 1)..];
                        if (rest.IndexOf("\xAB/\xBB", StringComparison.Ordinal) < 0)
                        {
                            var sb = new System.Text.StringBuilder(rest);
                            i++;
                            while (i < lines.Length)
                            {
                                var rl = lines[i];
                                int close = rl.IndexOf("\xAB/\xBB", StringComparison.Ordinal);
                                if (close >= 0)
                                {
                                    if (sb.Length > 0) sb.Append('\n');
                                    sb.Append(rl[..close]);
                                    i++;
                                    break;
                                }
                                if (sb.Length > 0) sb.Append('\n');
                                sb.Append(rl);
                                i++;
                            }
                            nodes.Add(new MdMarkup { Text = sb.ToString(), Style = style });
                            continue;
                        }
                    }
                }
            }

            // 代码块 ```lang\n...\n```（含「反引号写少了」的容错，判据见 CodeFence）
            if (CodeFence.TryOpen(line, out var fenceLang, out var fenceTicks, out var fenceChar))
            {
                var sb = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length)
                {
                    if (CodeFence.IsClose(lines[i], fenceTicks, fenceChar)) { i++; break; }
                    sb.AppendLine(lines[i]);
                    i++;
                }
                nodes.Add(new MdCodeBlock { Language = fenceLang, Code = sb.ToString().TrimEnd(), StartLine = 0 });
                continue;
            }

            // 表格 | a | b |
            if (line.TrimStart().StartsWith('|') && line.TrimEnd().EndsWith('|'))
            {
                var table = ParseTable(lines, ref i);
                if (table != null)
                {
                    nodes.Add(table);
                    continue;
                }
                // 非表格竖线内容（如单行「| 文本 |」）→ 剥掉首尾竖线按普通段落处理，避免被吞行
                var stripped = line.Trim().Trim('|').Trim();
                if (stripped.Length > 0)
                {
                    nodes.Add(new MdParagraph { Text = stripped });
                    i++;
                    continue;
                }
            }

            // 标题 # ~ ######（CommonMark 1-6 级；此前只认到 ####，##### 会掉进段落字面显示）
            var headingLevel = 0;
            var trimmed = line.TrimStart();
            while (headingLevel < trimmed.Length && trimmed[headingLevel] == '#' && headingLevel < 6)
                headingLevel++;
            if (headingLevel > 0 && headingLevel <= 6 &&
                (headingLevel == trimmed.Length || trimmed[headingLevel] == ' '))
            {
                var htext = headingLevel < trimmed.Length ? trimmed[(headingLevel + 1)..].Trim() : "";
                nodes.Add(new MdHeading { Level = headingLevel, Text = StripClosingHashes(htext) });
                i++; continue;
            }

            // 引用块 > —— **容器块**：剥掉一层 `>` 后递归解析，所以引用里能嵌代码块/列表/嵌套引用
            // （此前只是把各行 `>` 剥掉拼成一个字符串：`>> x` 会渲染成 `│ > x`，
            //   引用里的 ``` 代码块也只剩字面行）
            if (line.TrimStart().StartsWith('>'))
            {
                var quoteLines = new List<string>();
                while (i < lines.Length)
                {
                    var q = lines[i].TrimStart();
                    if (!q.StartsWith('>')) break;
                    // ⚠ **只剥一层**：`>> x` → `> x`，嵌套由递归那一层负责
                    var inner = q[1..];
                    if (inner.StartsWith(' ')) inner = inner[1..];
                    quoteLines.Add(inner);
                    i++;
                }
                if (quoteLines.Count > 0)
                {
                    var innerText = string.Join("\n", quoteLines);
                    var innerBlocks = Parse(innerText);
                    nodes.Add(new MdBlockQuote
                    {
                        Blocks = innerBlocks,
                        Text = FlattenText(innerBlocks),
                    });
                }
                continue;
            }

            // 分割线 --- *** ___
            if (IsHorizontalRule(line.Trim()))
            {
                nodes.Add(new MdRule());
                i++; continue;
            }

            // 列表项 - 或 * 或 1.（根据前导空格判断层级）
            if (IsListItem(line.TrimStart(), out var isOrdered, out var orderNum, out var itemText))
            {
                var leading = line.Length - line.TrimStart().Length;
                var level = leading / 2; // 每2空格=1级缩进

                // 任务清单 - [ ] / - [x]
                bool? checkedBox = null;
                var text = itemText.Trim();
                if (text.StartsWith("[ ]", StringComparison.Ordinal) || text.StartsWith("[x]", StringComparison.Ordinal) || text.StartsWith("[X]", StringComparison.Ordinal))
                {
                    checkedBox = text.StartsWith("[x]", StringComparison.Ordinal) || text.StartsWith("[X]", StringComparison.Ordinal);
                    text = text[3..].TrimStart();
                }

                nodes.Add(new MdListItem
                {
                    Text = text,
                    Ordered = isOrdered,
                    OrderNum = orderNum,
                    Level = level,
                    Checked = checkedBox,
                });
                i++; continue;
            }

            // Setext 标题：本行是正文、**下一行**是 === 或 --- 下划线（CommonMark）。
            // ⚠ 必须排在「分割线」判定**之后**（`---` 前面没有正文时是分割线）而排在段落收集**之前** ——
            //   否则 `标题\n---` 会被拆成「段落 + 分割线」，两端都渲染错。
            if (i + 1 < lines.Length
                && !IsHorizontalRule(line.Trim())
                && !IsListItem(line.TrimStart(), out _, out _, out _)
                && IsSetextUnderline(lines[i + 1], out var setextLevel))
            {
                nodes.Add(new MdHeading { Level = setextLevel, Text = line.Trim(), Setext = true });
                i += 2; continue;
            }

            // 缩进代码块（4 空格 / 1 个 Tab 起头）—— 必须排在各「块起始」判定之后，
            // 否则嵌套列表项（缩进 4 空格的 `- x`）会被这里先吃掉
            if (line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith('\t'))
            {
                var codeSb = new System.Text.StringBuilder();
                while (i < lines.Length)
                {
                    var cl = lines[i];
                    if (string.IsNullOrWhiteSpace(cl)) { codeSb.Append('\n'); i++; continue; }
                    if (cl.StartsWith("    ", StringComparison.Ordinal)) { codeSb.Append(cl[4..]).Append('\n'); i++; continue; }
                    if (cl.StartsWith('\t')) { codeSb.Append(cl[1..]).Append('\n'); i++; continue; }
                    break;
                }
                var code = codeSb.ToString().TrimEnd('\n');
                if (code.Length > 0)
                    nodes.Add(new MdCodeBlock { Language = "", Code = code });
                continue;
            }

            // 普通段落（可能跨多行）
            {
                var paraSb = new System.Text.StringBuilder();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                    // 围栏行终止段落 —— 判据必须与上面的开栏分支同源（CodeFence）：
                    // 硬编码 StartsWith("```") 时，「先一句说明、再贴代码」的围栏会被当段落续行吃掉
                    // （而模型最常就是这么写的：`说明：\n`csharp\n…）
                    && !CodeFence.TryOpen(lines[i], out _, out _)
                    && !lines[i].TrimStart().StartsWith('|')
                    && !lines[i].TrimStart().StartsWith('#')
                    && !lines[i].TrimStart().StartsWith('>')
                    && !IsHorizontalRule(lines[i].Trim())
                    && !IsListItem(lines[i].TrimStart(), out _, out _, out _))
                {
                    if (paraSb.Length > 0) paraSb.Append('\n'); // 保留换行：多行内容不被空格连接压缩（否则长消息行数塌缩、条目高度不足滚不动）
                    paraSb.Append(lines[i].Trim());
                    i++;
                }
                if (paraSb.Length > 0)
                    nodes.Add(new MdParagraph { Text = paraSb.ToString() });
            }
        }
        return nodes;
    }

    // ================================================================
    // 内联格式处理
    // ================================================================

    /// <summary>
    /// 将一行文本中的内联格式转换为带 ANSI 颜色的片段。
    /// 支持 **加粗**、*斜体*、`代码`、~~删除线~~、[链接](url)，以及 «tag»…«/» 标记。
    /// 返回 (文本, ANSI颜色码, 背景色码) 列表。
    /// 颜色码语义：1-9=样式属性(粗体/淡化/斜体/下划线/反白/删除线)，30-37/90-97=标准色。
    /// </summary>
    /// <summary>
    /// 只解码 «tag» / «/» 样式标记，其余字符一律原样保留。
    /// 供「纯文本」消息（system / tool 输出）用：这类内容里的 `反引号`、**星号**、# 号
    /// 属于数据而不是格式，走完整内联解析会把它们当标记吃掉；但 «» 是我们自己的中间格式，
    /// 必须在渲染层解码成颜色，否则用户直接看到 «grey» 字面量。
    /// </summary>
    /// <summary>
    /// 转义书名号的还原：`««` → 一个字面量 `«`，`»»` → 一个字面量 `»`。
    ///
    /// 由 <see cref="AnsiHelper.Esc"/> 产出（外部命令输出里出现 `«red»` 这样的字面量时，
    /// 不转义就会被渲染层当**真标签**吃掉）。**两个解析循环共用这一份判据** ——
    /// <see cref="ParseInline"/> 与 <see cref="ParseMarkupOnly"/> 各有一套扫描循环，
    /// 规则写两份必然漂移（本仓库的头号坑）。
    ///
    /// ⚠ 必须排在「找闭合 `»`」**之前**：`««a»»` 里第一个 `«` 后面紧跟的还是 `«`，
    ///   按标签去找闭合会一路找到最后一个 `»`，把中间整段当成一个（不认识的）标签。
    ///
    /// ⚠ 此前这条还原**根本不存在** —— Esc 把 `«` 变成 `««`，却没有任何地方变回来，
    ///   于是"转义过"的文本在屏幕上显示成两个书名号。是 ANSI→标记那条往返用例把它逼出来的。
    /// </summary>
    private static bool TryReadEscapedBook(string text, int i, out char literal, out int consumed)
    {
        literal = '\0';
        consumed = 0;
        if (i + 1 >= text.Length) return false;
        if (text[i] == '\xAB' && text[i + 1] == '\xAB') { literal = '\xAB'; consumed = 2; return true; }
        if (text[i] == '\xBB' && text[i + 1] == '\xBB') { literal = '\xBB'; consumed = 2; return true; }
        return false;
    }

    public static List<(string Text, int Color, int Bg)> ParseMarkupOnly(string text,
        int defaultColor = 0, int defaultBg = 0)
    {
        var result = new List<(string Text, int Color, int Bg)>();
        var styleStack = new Stack<(int Fg, int Bg)>();
        var current = new System.Text.StringBuilder();
        int curColor = defaultColor, curBg = defaultBg;

        void Flush()
        {
            if (current.Length == 0) return;
            result.Add((current.ToString(), curColor, curBg));
            current.Clear();
        }

        for (int i = 0; i < text.Length;)
        {
            if (TryReadEscapedBook(text, i, out var lit, out var used))
            {
                current.Append(lit);
                i += used;
                continue;
            }

            if (text[i] == '\xAB') // «
            {
                int close = text.IndexOf('\xBB', i + 1);
                if (close > i)
                {
                    string tag = text[(i + 1)..close].Trim();
                    if (tag == "/")
                    {
                        Flush();
                        (curColor, curBg) = styleStack.Count > 0 ? styleStack.Pop() : (defaultColor, defaultBg);
                        i = close + 1;
                        continue;
                    }
                    if (TryMapTag(tag, out int code, out bool isBg) && code > 0)
                    {
                        Flush();
                        styleStack.Push((curColor, curBg));
                        // «bold» 编进颜色高位（见 AnsiTty.BoldFlag）—— 直接置 curColor=1 会被
                        // 内层的颜色码覆盖（`«bold»«orange»` 解析完只剩 orange），加粗就丢了
                        if (isBg) curBg = code;
                        else if (code == 1) curColor |= Terminal.AnsiTty.BoldFlag;
                        else curColor = (curColor & Terminal.AnsiTty.BoldFlag) | code;
                        i = close + 1;
                        continue;
                    }
                    // 未知标签：不认识就当普通文字，原样落到输出里暴露笔误
                }
            }
            current.Append(text[i++]);
        }
        Flush();
        if (result.Count == 0) result.Add(("", defaultColor, defaultBg));
        return result;
    }

    public static List<(string Text, int Color, int Bg)> ParseInline(string text,
        int defaultColor = 0, int defaultBg = 0)
    {
        var result = new List<(string Text, int Color, int Bg)>();
        if (string.IsNullOrEmpty(text))
        {
            result.Add(("", defaultColor, defaultBg));
            return result;
        }

        int i = 0;
        var current = new System.Text.StringBuilder();
        // «tag» 样式栈：进入 span 前压栈，«/» 弹栈恢复（支持嵌套与流式未闭合 span）。
        // 前景/背景一起进栈 —— «bg:#333» 里嵌 «red» 再 «/» 时，背景必须留着而不是被一并弹掉
        var styleStack = new Stack<(int Fg, int Bg)>();
        int curColor = defaultColor, curBg = defaultBg;

        void FlushCurrent()
        {
            if (current.Length > 0)
            {
                result.Add((current.ToString(), curColor, curBg));
                current.Clear();
            }
        }

        while (i < text.Length)
        {
            if (TryReadEscapedBook(text, i, out var lit, out var used))
            {
                current.Append(lit);
                i += used;
                continue;
            }

            // Markup 标记 «tag»（样式/颜色）与 «/»（复位到上一级）
            if (text[i] == '\xAB') // «
            {
                int close = text.IndexOf('\xBB', i + 1);
                if (close > i)
                {
                    string tag = text[(i + 1)..close].Trim();
                    if (tag == "/")
                    {
                        FlushCurrent();
                        (curColor, curBg) = styleStack.Count > 0 ? styleStack.Pop() : (defaultColor, defaultBg);
                        i = close + 1;
                        continue;
                    }
                    if (TryMapTag(tag, out int code, out bool isBg) && code > 0)
                    {
                        FlushCurrent();
                        styleStack.Push((curColor, curBg));
                        // «bold» 编进颜色高位（见 AnsiTty.BoldFlag）—— 直接置 curColor=1 会被
                        // 内层的颜色码覆盖（`«bold»«orange»` 解析完只剩 orange），加粗就丢了
                        if (isBg) curBg = code;
                        else if (code == 1) curColor |= Terminal.AnsiTty.BoldFlag;
                        else curColor = (curColor & Terminal.AnsiTty.BoldFlag) | code;
                        i = close + 1;
                        continue;
                    }
                    // 未知标签：不识别，按字面输出（保留 « 原样）
                }
            }

            // 反斜杠转义 `\*` `\_` `\`` `\[` `\\` …（CommonMark）
            if (text[i] == '\\' && i + 1 < text.Length && IsEscapable(text[i + 1]))
            {
                current.Append(text[i + 1]);
                i += 2;
                continue;
            }

            // HTML 实体 `&amp;` `&lt;` `&#39;` `&#x27;` → 还原成字符
            if (text[i] == '&' && TryReadEntity(text, i, out var entity, out var entLen))
            {
                current.Append(entity);
                i += entLen;
                continue;
            }

            // 自动链接 `<https://…>` / `<mailto:…>` / `<a@b.c>`
            if (text[i] == '<')
            {
                var gt = text.IndexOf('>', i + 1);
                if (gt > i + 1)
                {
                    var inner = text[(i + 1)..gt];
                    if (IsAutolink(inner))
                    {
                        FlushCurrent();
                        result.Add((inner, 36, curBg));
                        i = gt + 1;
                        continue;
                    }
                }
            }

            // 裸 URL（http/https）—— GitHub 风格自动链接
            if (text[i] == 'h' && LooksLikeBareUrl(text, i, out var bareLen))
            {
                FlushCurrent();
                result.Add((text.Substring(i, bareLen), 36, curBg));
                i += bareLen;
                continue;
            }

            // 图片 `![alt](url)` —— 必须**先于链接**处理，否则 `!` 会单独漏出来（渲染成孤零零一个感叹号）
            if (text[i] == '!' && i + 1 < text.Length && text[i + 1] == '['
                && TryReadLinkTarget(text, i + 1, out var imgText, out var imgUrl, out var imgEnd))
            {
                FlushCurrent();
                result.Add(("\U0001F5BC ", 2, curBg));                       // 🖼 前缀（图形界面可换成真图片）
                result.Add((imgText.Length > 0 ? imgText : "图片", 36, curBg));
                if (imgUrl.Length > 0) result.Add(($" ({imgUrl})", 2, curBg));
                i = imgEnd;
                continue;
            }

            // `***粗斜体***` —— 必须排在 `**` 与 `*` 之前，否则会拆成「`*x` 加粗 + 落单 `*`」
            if (text[i] == '*' && i + 2 < text.Length && text[i + 1] == '*' && text[i + 2] == '*')
            {
                var end3 = text.IndexOf("***", i + 3, StringComparison.Ordinal);
                if (end3 > i + 2)
                {
                    FlushCurrent();
                    // 加粗走 BoldFlag 高位、斜体走低位的 3 —— 与 «bold»«orange» 同一套编码
                    result.Add((text[(i + 3)..end3], Terminal.AnsiTty.BoldFlag | 3, curBg));
                    i = end3 + 3;
                    continue;
                }
            }

            // `__加粗__`（下划线形态；词内不触发，见 CanOpen/CloseUnderscore）
            if (text[i] == '_' && i + 1 < text.Length && text[i + 1] == '_'
                && CanOpenUnderscore(text, i))
            {
                var end2 = text.IndexOf("__", i + 2, StringComparison.Ordinal);
                if (end2 > i + 1 && CanCloseUnderscore(text, end2))
                {
                    FlushCurrent();
                    result.Add((text[(i + 2)..end2], 1, curBg));
                    i = end2 + 2;
                    continue;
                }
            }

            // `_斜体_`（下划线形态；词内下划线不算强调，CommonMark）
            if (text[i] == '_' && i + 1 < text.Length && text[i + 1] != '_'
                && CanOpenUnderscore(text, i))
            {
                var end1 = text.IndexOf('_', i + 1);
                if (end1 > i + 1 && CanCloseUnderscore(text, end1))
                {
                    FlushCurrent();
                    result.Add((text[(i + 1)..end1], 3, curBg));
                    i = end1 + 1;
                    continue;
                }
            }

            // 链接 [文字](url) 或 [文字](url "标题")（标题要剥掉，否则会当成 URL 的一部分显示）
            if (text[i] == '[' && TryReadLinkTarget(text, i, out var linkText, out var url, out var linkEnd))
            {
                FlushCurrent();
                result.Add((linkText, 36, curBg)); // 青色链接文字
                if (!string.IsNullOrEmpty(url) && url != linkText)
                    result.Add(($" ({url})", 2, curBg)); // 弱化显示 URL
                i = linkEnd;
                continue;
            }

            // 内联代码 `` `code` `` / `` ``a`b`` `` —— 反引号数量可变，闭合要用同样多个
            if (text[i] == '`')
            {
                int run = 0;
                while (i + run < text.Length && text[i + run] == '`') run++;
                var closeRun = new string('`', run);
                var end = text.IndexOf(closeRun, i + run, StringComparison.Ordinal);
                if (end > i + run)
                {
                    FlushCurrent();
                    // 黄色文字、不加底色 —— 曾经写 48 当「深色背景」，但 48 是 SGR 的
                    // 「扩展背景色引导码」，必须跟 5;n 或 2;r;g;b；裸发 ESC[48m 是残缺序列，
                    // 各终端解释不一（Windows Terminal 渲染成亮绿底），路径/命令一片刺眼绿
                    var code = text[(i + run)..end];
                    // CommonMark：两侧各有一个空格时剥掉一层（用于首尾本身就带反引号的内容）
                    if (code.Length > 2 && code.StartsWith(' ') && code.EndsWith(' ') && code.Trim().Length > 0)
                        code = code[1..^1];
                    result.Add((code, 33, curBg));
                    i = end + run;
                    continue;
                }
            }

            // **加粗**
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2);
                if (end > i)
                {
                    FlushCurrent();
                    result.Add((text[(i + 2)..end], 1, curBg)); // Bold
                    i = end + 2;
                    continue;
                }
            }

            // ~~删除线~~
            if (text[i] == '~' && i + 1 < text.Length && text[i + 1] == '~')
            {
                var end = text.IndexOf("~~", i + 2);
                if (end > i)
                {
                    FlushCurrent();
                    // 9 = 删除线（MAUI → TextDecorations.Strikethrough、AnsiMarkup → strike）。
                    // TUI 的样式位里没有删除线，由 FrameSnapshot 退化成淡化（见那里的注释）。
                    result.Add((text[(i + 2)..end], 9, curBg));
                    i = end + 2;
                    continue;
                }
            }

            // *斜体* (单独 *，不是 **)
            if (text[i] == '*' && (i == 0 || text[i - 1] != '*') &&
                (i + 1 >= text.Length || text[i + 1] != '*'))
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i + 1 && (end + 1 >= text.Length || text[end + 1] != '*'))
                {
                    FlushCurrent();
                    result.Add((text[(i + 1)..end], 3, curBg)); // Italic
                    i = end + 1;
                    continue;
                }
            }

            current.Append(text[i]);
            i++;
        }

        FlushCurrent();
        return result;
    }

    /// <summary>
    /// 将 «» 标记的标签名映射为颜色码（样式属性 1-9 / 标准色 30-37 / 亮色 90-97）。
    /// 未知标签返回 0（调用方按字面输出）。多词标签（如「bold yellow」「bright red」）
    /// 取颜色词、样式前缀丢弃（对齐 Program.cs MarkupLine 的「粗体=颜色」约定）。
    /// </summary>
    private static int MapMarkupTag(string tag) => TryMapTag(tag, out var code, out bool isBg) && !isBg ? code : 0;

    /// <summary>
    /// 解析 «tag» 标签为颜色/样式码，并告知它作用于前景还是背景。
    /// 支持三类写法：
    ///   «red» / «bold yellow» / «dim»    —— 命名色与样式（前景）
    ///   «fg:#rrggbb» / «#rgb»            —— 真彩前景（编码走 AnsiTty.RgbCode，≥0x1000000）
    ///   «bg:#rrggbb» / «bg:red»          —— 真彩/命名背景
    /// 返回 false 表示不认识这个标签，调用方应把它当普通文字原样输出（暴露笔误而非静默吞掉）。
    /// </summary>
    public static bool TryMapTag(string tag, out int code, out bool isBg)
    {
        tag = tag.Trim().ToLowerInvariant();
        isBg = false;
        code = 0;
        if (tag.Length == 0) return false;

        // fg:/bg: 前缀 —— 前缀只决定作用通道，值本身仍走下面的命名色/十六进制解析
        if (tag.StartsWith("bg:", StringComparison.Ordinal)) { isBg = true; tag = tag[3..].Trim(); }
        else if (tag.StartsWith("fg:", StringComparison.Ordinal)) tag = tag[3..].Trim();

        if (tag.StartsWith('#'))
        {
            if (!TryParseHex(tag, out int rgb)) return false;
            code = rgb;             // 真彩码，前景/背景同码，由 FgCode/BgCode 各自展开
            return true;
        }

        code = MapNamedTag(tag);
        if (code == 0) return false;
        // 命名色转背景：标准 16 色前景 30-37/90-97 → 背景 40-47/100-107；
        // 256 色码（16-255）与样式属性（1-9）无需偏移，BgCode 会按 48;5;N 展开
        if (isBg && code is >= 30 and <= 37 or >= 90 and <= 97) code += 10;
        return true;
    }

    /// <summary>解析 #rgb / #rrggbb 为 AnsiTty 真彩码（0x1000000 | rgb）。非法写法返回 false。</summary>
    private static bool TryParseHex(string tag, out int code)
    {
        code = 0;
        var hex = tag[1..];
        if (hex.Length == 3)   // #abc → #aabbcc
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6) return false;
        foreach (var c in hex)
            if (!Uri.IsHexDigit(c)) return false;
        int rgb = Convert.ToInt32(hex, 16);
        code = Terminal.AnsiTty.RgbCode((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        return true;
    }

    private static int MapNamedTag(string tag)
    {
        switch (tag)
        {
            case "bold": case "bright": return 1;    // 粗体/加亮
            case "dim": case "faint": return 2;      // 淡化
            case "italic": case "i": return 3;       // 斜体
            case "underline": case "u": return 4;    // 下划线
            case "blink": return 5;                  // 闪烁
            case "reverse": case "invert": return 7; // 反白
            case "strike": case "strikethrough": case "s": return 9; // 删除线
        }

        // 颜色（支持 bright 前缀与 bold/underline 等样式前缀）
        string colorName = tag;
        bool bright = false;
        if (tag.Contains(' '))
        {
            foreach (var p in tag.Split(' '))
            {
                if (p.Length == 0) continue;
                if (p is "bold" or "dim" or "underline" or "italic") continue;
                if (p == "bright") { bright = true; continue; }
                colorName = p;
                break;
            }
        }

        int code = colorName switch
        {
            "black" => 30,
            "red" => 31,
            "green" => 32,
            "yellow" => 33,
            "blue" => 34,
            "magenta" or "purple" => 35,
            "cyan" => 36,
            "white" => 37,
            "grey" or "gray" => 90,
            "orange3" => AnsiColors.Orange3,
            "orange" => AnsiColors.Orange,
            _ => 0,
        };
        if (code == 0) return 0;
        if (bright && code is >= 30 and <= 37)
            return code + 60;   // 亮色 90-97
        return code;
    }

    // ================================================================
    // 内部工具
    // ================================================================

    private static MdTable? ParseTable(string[] lines, ref int i)
    {
        // 先窥探连续的 | 行（不消费），不足 2 行不构成表格，交由调用方按普通文本处理，避免吞行
        int peek = i;
        var allRows = new List<string[]>();
        while (peek < lines.Length && lines[peek].TrimStart().StartsWith('|'))
        {
            var cells = SplitTableCells(lines[peek]);
            if (cells.Length > 0) allRows.Add(cells);
            peek++;
        }
        if (allRows.Count < 2) return null;

        i = peek; // 确认构成表格后才统一消费

        // 跳过分隔行 |---|:---:|---:|
        var hasSeparator = TryParseSeparator(allRows[1], out var aligns);
        var headers = allRows[0].ToList();
        var dataRows = hasSeparator
            ? allRows.Skip(2).Select(r => r.ToList()).ToList()
            : allRows.Skip(1).Select(r => r.ToList()).ToList();

        return new MdTable { Headers = headers, Rows = dataRows, Alignments = hasSeparator ? aligns : [] };
    }

    /// <summary>
    /// 解析表格分隔行 → 每列对齐（0=默认 1=左 2=中 3=右）。
    /// 判据与原先「整格只有 - : 空白」同源（空单元格仍算合法分隔格），只是额外读出 `:` 的位置。
    /// </summary>
    private static bool TryParseSeparator(string[] cells, out List<int> aligns)
    {
        aligns = new List<int>();
        foreach (var raw in cells)
        {
            var c = raw.Trim();
            foreach (var ch in c)
                if (ch != '-' && ch != ':' && ch != ' ') { aligns.Clear(); return false; }

            var body = c.Trim(':');
            var isDash = body.Length > 0 && body.All(ch => ch == '-');
            aligns.Add(isDash
                ? (c.StartsWith(':') && c.EndsWith(':') ? 2 : c.EndsWith(':') ? 3 : c.StartsWith(':') ? 1 : 0)
                : 0);
        }
        return true;
    }

    /// <summary>
    /// 按 | 拆分表格单元格，支持「\|」转义竖线（单元格内出现字面竖线时不误拆）。
    /// 首尾竖线剥除后，仅「\|」被视为转义（替换为 |），其余字符原样保留。
    /// </summary>
    private static string[] SplitTableCells(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];

        var cells = new List<string>();
        var sb = new System.Text.StringBuilder();
        for (var j = 0; j < s.Length; j++)
        {
            var ch = s[j];
            if (ch == '\\' && j + 1 < s.Length && s[j + 1] == '|')
            {
                sb.Append('|'); // 转义竖线
                j++;
                continue;
            }
            if (ch == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        cells.Add(sb.ToString().Trim());
        return cells.ToArray();
    }

    private static bool IsHorizontalRule(string line)
    {
        if (line.Length < 3) return false;
        var ch = line[0];
        if (ch != '-' && ch != '*' && ch != '_') return false;
        return line.All(c => c == ch || c == ' ');
    }

    /// <summary>
    /// Setext 下划线行：整行只有 <c>=</c>（一级）或 <c>-</c>（二级），可带尾随空白。
    /// 与 <see cref="IsHorizontalRule"/> 的区别是**这里只认单一字符**（`- - -` 是分割线不是下划线），
    /// 且 `-` 形态与 `===` 同族 —— 谁生效取决于它前面有没有正文（由调用方判断）。
    /// </summary>
    private static bool IsSetextUnderline(string line, out int level)
    {
        level = 0;
        var t = line.TrimEnd();
        if (t.Length == 0) return false;
        var ch = t[0];
        if (ch != '=' && ch != '-') return false;
        foreach (var c in t)
            if (c != ch) return false;
        level = ch == '=' ? 1 : 2;
        return true;
    }

    /// <summary>`## 标题 ##` 的关闭式井号串要剥掉（CommonMark）。</summary>
    private static string StripClosingHashes(string s)
    {
        var t = s.TrimEnd();
        if (!t.EndsWith('#')) return t;
        int k = t.Length;
        while (k > 0 && t[k - 1] == '#') k--;
        // 关闭串前面必须是空白（或整串都是 # ⇒ 空标题）
        if (k > 0 && !char.IsWhiteSpace(t[k - 1])) return t;
        return t[..k].TrimEnd();
    }

    /// <summary>
    /// 把一棵块树摊成纯文本（不含任何 markdown 标记）。供只要「一行字」的消费者用
    /// （如 <see cref="MdBlockQuote.Text"/>：图形界面走 Blocks 画嵌套，纯文本消费者拿 Text）。
    /// </summary>
    private static string FlattenText(List<MdNode> nodes)
    {
        var parts = new List<string>();
        foreach (var n in nodes)
        {
            switch (n)
            {
                case MdParagraph p: parts.Add(p.Text); break;
                case MdHeading h: parts.Add(h.Text); break;
                case MdListItem li: parts.Add(li.Text); break;
                case MdCodeBlock c: parts.Add(c.Code); break;
                case MdBlockQuote q: parts.Add(q.Text); break;
                case MdMarkup m: parts.Add(m.Text); break;
                case MdRule: break;
            }
        }
        return string.Join("\n", parts);
    }

    /// <summary>可被反斜杠转义的字符（CommonMark：ASCII 标点）。</summary>
    private static bool IsEscapable(char c)
        => c is (>= '!' and <= '/') or (>= ':' and <= '@') or (>= '[' and <= '`') or (>= '{' and <= '~');

    /// <summary>
    /// HTML 实体：<c>&amp;name;</c> / <c>&#nn;</c> / <c>&#xhh;</c> → 还原成字符。
    /// 认不出返回 false（调用方按字面保留）—— 未经解码时屏幕上会直接显示 <c>&amp;amp;</c>。
    /// </summary>
    private static bool TryReadEntity(string s, int i, out string value, out int len)
    {
        value = ""; len = 0;
        var semi = s.IndexOf(';', i + 1);
        if (semi < 0 || semi - i > 32) return false;   // 实体名不会太长；上限防止把普通 `&` 一路找到很远处
        var body = s[(i + 1)..semi];
        if (body.Length == 0) return false;

        if (body[0] == '#')
        {
            var num = body[1..];
            int cp;
            var ok = (num.StartsWith('x') || num.StartsWith('X'))
                ? int.TryParse(num[1..], System.Globalization.NumberStyles.HexNumber,
                               System.Globalization.CultureInfo.InvariantCulture, out cp)
                : int.TryParse(num, out cp);
            if (!ok || cp <= 0 || cp > 0x10FFFF) return false;
            try { value = char.ConvertFromUtf32(cp); } catch { return false; }
            len = semi - i + 1;
            return true;
        }

        value = body switch
        {
            "amp" => "&", "lt" => "<", "gt" => ">", "quot" => "\"", "apos" => "'", "nbsp" => " ",
            "copy" => "©", "reg" => "®", "trade" => "™", "hellip" => "…", "mdash" => "—", "ndash" => "–",
            "times" => "×", "divide" => "÷", "laquo" => "«", "raquo" => "»",
            "ldquo" => "“", "rdquo" => "”", "lsquo" => "‘", "rsquo" => "’",
            "deg" => "°", "plusmn" => "±", "middot" => "·", "bull" => "•", "dagger" => "†",
            "euro" => "€", "pound" => "£", "yen" => "¥", "cent" => "¢", "sect" => "§", "para" => "¶",
            _ => "",
        };
        if (value.Length == 0) return false;
        len = semi - i + 1;
        return true;
    }

    /// <summary>自动链接目标：<c>scheme:…</c>（无空白）或 <c>a@b.c</c>。</summary>
    private static bool IsAutolink(string s)
    {
        if (s.Length == 0 || s.Any(char.IsWhiteSpace)) return false;
        var colon = s.IndexOf(':');
        if (colon > 0 && colon < s.Length - 1)
        {
            var scheme = s[..colon];
            if (scheme.Length >= 2 && char.IsAsciiLetter(scheme[0])
                && scheme.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.'))
                return true;
        }
        var at = s.IndexOf('@');
        return at > 0 && at < s.Length - 1 && s.IndexOf('.', at) > at + 1;   // 邮箱
    }

    /// <summary>
    /// 裸 URL（GitHub 风格自动链接）：只认 <c>http://</c> / <c>https://</c>，到空白或 <c>&lt;</c> 为止。
    /// **尾部句读要剥掉** —— `见 https://x.com。` 里的句号不属于 URL。
    /// </summary>
    private static bool LooksLikeBareUrl(string s, int i, out int len)
    {
        len = 0;
        var rest = s[i..];
        if (!rest.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !rest.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
        int k = 0;
        while (k < rest.Length && !char.IsWhiteSpace(rest[k]) && rest[k] != '<') k++;
        while (k > 0 && rest[k - 1] is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']'
                                  or '。' or '，' or '；' or '：' or '！' or '？' or '）' or '】') k--;
        if (k <= "https://".Length) return false;
        len = k;
        return true;
    }

    /// <summary>
    /// 从 <paramref name="bracket"/>（指向 <c>[</c>）读一个链接：<c>[文字](url)</c> 或 <c>[文字](url "标题")</c>。
    /// **标题要剥掉**，否则它会被当成 URL 的一部分原样显示出来。
    /// </summary>
    private static bool TryReadLinkTarget(string s, int bracket, out string label, out string url, out int end)
    {
        label = ""; url = ""; end = bracket;
        if (bracket >= s.Length || s[bracket] != '[') return false;
        var closeBracket = s.IndexOf(']', bracket + 1);
        if (closeBracket < 0 || closeBracket + 1 >= s.Length || s[closeBracket + 1] != '(') return false;
        var closeParen = s.IndexOf(')', closeBracket + 2);
        if (closeParen < 0) return false;

        label = s[(bracket + 1)..closeBracket];
        var target = s[(closeBracket + 2)..closeParen].Trim();

        var q = target.IndexOfAny(['"', '\'']);
        if (q > 0) target = target[..q].Trim();
        else if (target.EndsWith(')') && target.Contains(" ("))
            target = target[..target.LastIndexOf(" (", StringComparison.Ordinal)].Trim();

        url = target;
        end = closeParen + 1;
        return true;
    }

    /// <summary>
    /// 下划线强调的开/闭判据（CommonMark「词内下划线不算强调」）：
    /// <c>foo_bar_baz</c> 不触发、<c>_x_</c> 触发 —— 开标记左侧不能是字母数字，闭标记右侧不能是字母数字。
    /// </summary>
    private static bool CanOpenUnderscore(string s, int i)
        => i == 0 || !char.IsLetterOrDigit(s[i - 1]);

    private static bool CanCloseUnderscore(string s, int i)
        => i + 1 >= s.Length || !char.IsLetterOrDigit(s[i + 1]);

    private static bool IsListItem(string line, out bool ordered,
        out int orderNum, out string text)
    {
        ordered = false; orderNum = 0; text = "";

        // 无序列表 - 或 * 或 +（CommonMark 三种记号等价；此前漏了 `+`，`+ 项` 会字面显示加号）
        if ((line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal)
             || line.StartsWith("+ ", StringComparison.Ordinal)) && line.Length > 2)
        {
            text = line[2..];
            return true;
        }

        // 有序列表 1. 2. etc
        int j = 0;
        while (j < line.Length && char.IsDigit(line[j])) j++;
        if (j > 0 && j < line.Length - 2 && line[j] == '.' && line[j + 1] == ' ')
        {
            if (int.TryParse(line[..j], out orderNum))
            {
                ordered = true;
                text = line[(j + 2)..];
                return true;
            }
        }

        return false;
    }
}
