using System.Text;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Shared;

/// <summary>
/// 裸 ANSI 转义序列 → <c>«»</c> 中间格式（**有限子集**）。
///
/// ## 为什么需要它
///
/// 外部进程的输出带的是**终端控制字节**（`ls --color`、`git status`、`grep --color`…）。
/// 三个消费端各有各的处理，唯独「支持富文本但不懂终端」的那一端最尴尬：
///
/// | 端 | 现状 |
/// |---|---|
/// | CLI / TUI | 有真终端，原样透传即可 |
/// | Web | `app.js` 的 `ansiToHtml` 解成 HTML |
/// | **移动端 / GUI 富文本** | **原先直接 <see cref="AnsiHelper.StripAnsi"/> 丢掉** |
///
/// 丢掉的结果不是"没颜色"而是"看不出重点"：`git status` 的红绿、`ls` 的目录蓝全没了。
/// 这里把 SGR 翻成仓库统一的 <c>«»</c> 中间格式（**内容层禁止硬写 ANSI** 是铁律），
/// 交给各端已有的 markup 渲染器去上色 —— 不新造一套颜色管道。
///
/// ## 「有限」是刻意的
///
/// 这里**不是**终端模拟器，不解释光标移动、不画进度条、不还原「覆盖重写同一行」：
///
/// · **支持** SGR：`0` 重置 · `1` 粗 · `2` 暗 · `3` 斜 · `4` 下划线 · `9` 删除线 ·
///   `21/22/23/24/29` 关掉对应样式 · `30-37 / 90-97` 前景 · `40-47 / 100-107` 背景 ·
///   `38;5;N` `48;5;N`（256 色）· `38;2;r;g;b` `48;2;r;g;b`（真彩）
/// · **吃掉**（不落到正文）：其余所有 CSI（光标/擦除/滚动）、OSC、字符集切换
/// · **不支持**（当没看见）：反白、闪烁、隐藏 —— 它们在富文本里没有对得上的表达，
///   硬套一个只会显示成别的东西
///
/// ⚠ **反白（SGR 7）刻意不做**：它要交换前景与背景，而本仓库的取色是**跟着日/夜主题走**的
/// （见 <c>MarkupToFormattedString.ResolveFg</c>），"交换"在主题适配之后语义就变了。
/// 与其显示成错的，不如不显示。
///
/// ## 与 <see cref="AnsiHelper.StripAnsi"/> 的关系
///
/// 后者是"只去色不保留"的旧路径（还有别的调用方在用），前者是"去色但把颜色留下来"。
/// 两者**共用同一套转义扫描规则**（都按 CSI 的参数/中间字节/终止字节切分），新增序列样式时
/// 记得两边一起看。
/// </summary>
public static class AnsiMarkup
{
    /// <summary>
    /// 把带 ANSI 的文本翻成 <c>«»</c> 中间格式。**没有转义时原样返回**（不分配）。
    ///
    /// 正文里的 `«` / `»` 会被转义（见 <see cref="AnsiHelper.Esc"/>），
    /// 否则外部输出里恰好出现 `«red»` 这样的字面量时，渲染层会把它当成真标签吃掉。
    /// </summary>
    public static string ToMarkup(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";

        var hasEsc = text.IndexOf(AnsiTty.AnsiCharPrefix) >= 0;
        var hasBook = text.IndexOf('\xAB') >= 0 || text.IndexOf('\xBB') >= 0;
        if (!hasEsc && !hasBook) return text;   // 快路径：绝大多数命令行输出没有转义

        var sb = new StringBuilder(text.Length + 32);
        var st = new Style();
        var open = 0;                            // 已经发出、还没闭合的标签数

        for (var i = 0; i < text.Length;)
        {
            var c = text[i];

            if (c == AnsiTty.AnsiCharPrefix)
            {
                if (TryReadSgr(text, ref i, st))
                {
                    Reopen(sb, st, ref open);
                    continue;
                }
                SkipEscape(text, ref i);         // 非 SGR（光标/OSC/字符集…）：吃掉
                continue;
            }

            // 回车：终端的"回到行首覆盖"在文本控件里没有对应语义（`\r50%\r60%` 拼起来是乱码），
            // 直接丢掉；`\r\n` 由调用方统一成 `\n` 之后这里不会见到成对的
            if (c == '\r') { i++; continue; }

            if (c == '\xAB') { sb.Append("\xAB\xAB"); i++; continue; }
            if (c == '\xBB') { sb.Append("\xBB\xBB"); i++; continue; }

            sb.Append(c);
            i++;
        }

        CloseAll(sb, ref open);
        return sb.ToString();
    }

    // ---------------------------------------------------------------- 样式状态

    private sealed class Style
    {
        public bool Bold, Dim, Italic, Underline, Strike;
        public int Fg = -1;      // -1 = 默认；否则是「前景码」（30-37 / 90-97）或真彩码
        public int Bg = -1;

        public bool IsPlain => !Bold && !Dim && !Italic && !Underline && !Strike && Fg < 0 && Bg < 0;

        public void Reset()
        {
            Bold = Dim = Italic = Underline = Strike = false;
            Fg = Bg = -1;
        }
    }

    /// <summary>先关掉所有已开标签，再按当前样式重新打开。样式没变时一个字节都不写。</summary>
    private static void Reopen(StringBuilder sb, Style st, ref int open)
    {
        var tags = Tags(st);
        if (open == tags.Count && open == 0) return;   // 两边都空（纯文本），不必动

        // 朴素做法：全关再全开。样式切换在真实输出里是低频事件（每行几次），
        // 而"找出哪几个标签没变、只补差异"要维护一份标签栈，复杂度换不来收益。
        CloseAll(sb, ref open);
        foreach (var t in tags) sb.Append('\xAB').Append(t).Append('\xBB');
        open = tags.Count;
    }

    private static void CloseAll(StringBuilder sb, ref int open)
    {
        for (var i = 0; i < open; i++) sb.Append("\xAB/\xBB");
        open = 0;
    }

    /// <summary>当前样式对应的标签名列表（顺序固定：先样式后颜色，与别处的写法一致）。</summary>
    private static List<string> Tags(Style st)
    {
        var list = new List<string>(6);
        if (st.Bold) list.Add("bold");
        if (st.Dim) list.Add("dim");
        if (st.Italic) list.Add("italic");
        if (st.Underline) list.Add("underline");
        if (st.Strike) list.Add("strike");
        if (st.Fg >= 0) list.Add(FgTag(st.Fg));
        if (st.Bg >= 0) list.Add("bg:" + ColorValue(st.Bg));
        return list;
    }

    /// <summary>
    /// 前景标签。**16 色优先走命名**（`«red»` / `«bright red»`）而不是十六进制 ——
    /// 命名色会走渲染端那张显式的 16 色表，那是"终端标准色"，与 `ls --color` 的语义一致；
    /// 写死十六进制反而绕开了那张表。
    /// </summary>
    private static string FgTag(int code)
    {
        if (code is >= 30 and <= 37) return BaseName(code - 30);
        if (code is >= 90 and <= 97) return "bright " + BaseName(code - 90);
        return ColorValue(code);   // 256 色 / 真彩
    }

    /// <summary>颜色值：`30-37`/`90-97` 之外的都写成十六进制（markup 只认命名色与 `#rrggbb`）。</summary>
    private static string ColorValue(int code)
    {
        if (code is >= 30 and <= 37) return BaseName(code - 30);
        if (code is >= 90 and <= 97) return "bright " + BaseName(code - 90);
        // 真彩码（AnsiTty.RgbCode = 0x1000000 | r<<16 | g<<8 | b）——
        // ⚠ 这一条必须排在 256 色之前：真彩码的数值**大于 255**，
        //   顺序反了会掉进兜底分支，`38;2;r;g;b` 全部渲染成同一个颜色。
        if (code >= 0x1000000)
            return $"#{(code >> 16) & 0xFF:x2}{(code >> 8) & 0xFF:x2}{code & 0xFF:x2}";
        if (code is >= 0 and <= 255) return AnsiTty.Xterm256ToHex(code);
        return AnsiTty.Xterm256ToHex(7);
    }

    private static string BaseName(int idx) => idx switch
    {
        0 => "black", 1 => "red", 2 => "green", 3 => "yellow",
        4 => "blue", 5 => "magenta", 6 => "cyan", _ => "white",
    };

    // ---------------------------------------------------------------- 转义扫描

    /// <summary>
    /// 在 <paramref name="i"/> 处（指向 ESC）尝试读一条 SGR（`CSI … m`）。
    /// 是就消费掉并更新 <paramref name="st"/>、返回 true；不是则**不动 i**、返回 false。
    /// </summary>
    private static bool TryReadSgr(string s, ref int i, Style st)
    {
        var j = i + 1;
        if (j >= s.Length || s[j] != AnsiTty.AnsiCharEscape) return false;
        j++;
        var start = j;
        while (j < s.Length && s[j] != 'm')
        {
            // 参数与中间字节之外的东西说明这不是 SGR（比如 CSI 之后的 'A'/'H'）——
            // 用范围判断而不是"扫到 m 为止"：`\x1b[2J\x1b[1m` 这种连写会被扫成一整条
            var ch = s[j];
            if (!(char.IsDigit(ch) || ch == ';' || ch == ':')) return false;
            j++;
        }
        if (j >= s.Length) return false;       // 没有终止的 'm'

        ApplySgr(s[start..j], st);
        i = j + 1;
        return true;
    }

    /// <summary>
    /// 吃掉一条转义序列（不是 SGR 的那些）。规则与 <see cref="AnsiHelper.StripAnsi"/> 同源：
    /// CSI = `ESC [ 参数 中间字节 终止字节`，终止字节在 0x40-0x7E；OSC 走 `ESC ] … BEL/ST`。
    /// </summary>
    private static void SkipEscape(string s, ref int i)
    {
        var j = i + 1;
        if (j >= s.Length) { i = j; return; }

        if (s[j] == ']')   // OSC：直到 BEL 或 ST(ESC \)
        {
            j++;
            while (j < s.Length && s[j] != '\x07')
            {
                if (s[j] == AnsiTty.AnsiCharPrefix && j + 1 < s.Length && s[j + 1] == '\\') { j += 2; break; }
                j++;
            }
            if (j < s.Length && s[j] == '\x07') j++;
            i = j;
            return;
        }

        if (s[j] == '[')   // CSI
        {
            j++;
            while (j < s.Length && s[j] >= '\x20' && s[j] <= '\x3F') j++;   // 参数 + 中间字节
            if (j < s.Length) j++;                                          // 终止字节
            i = j;
            return;
        }

        // ESC ( B 这类：吃掉 ESC + 下一个字节
        i = j + 1;
    }

    /// <summary>
    /// 应用一条 SGR 的参数串（`m` 之前的那部分，已经确定只含数字/`;`/`:`）。
    ///
    /// 空参数（`\x1b[m`）等价于 `\x1b[0m` —— 这是终端的老规矩，漏了会让
    /// "只发一个 ESC[m 收尾"的程序后面一直inherit着上一段颜色。
    /// </summary>
    private static void ApplySgr(string body, Style st)
    {
        if (body.Length == 0) { st.Reset(); return; }

        var parts = body.Split(';');
        for (var k = 0; k < parts.Length; k++)
        {
            if (!int.TryParse(parts[k], out var v)) continue;

            switch (v)
            {
                case 0: st.Reset(); break;
                case 1: st.Bold = true; break;
                case 2: st.Dim = true; break;
                case 3: st.Italic = true; break;
                case 4: st.Underline = true; break;
                case 9: st.Strike = true; break;
                case 21: case 22: st.Bold = st.Dim = false; break;
                case 23: st.Italic = false; break;
                case 24: st.Underline = false; break;
                case 29: st.Strike = false; break;
                case 39: st.Fg = -1; break;
                case 49: st.Bg = -1; break;

                case >= 30 and <= 37: st.Fg = v; break;
                case >= 90 and <= 97: st.Fg = v; break;
                case >= 40 and <= 47: st.Bg = v - 10; break;
                case >= 100 and <= 107: st.Bg = v - 10; break;

                // 扩展色：38/48 后面跟 "5;N"（256 色）或 "2;r;g;b"（真彩）。
                // 这几个参数要**连着读**，所以自己往前走 k。
                case 38 or 48:
                    var slot = v == 38 ? 1 : 2;
                    if (k + 2 < parts.Length && parts[k + 1] == "5" &&
                        int.TryParse(parts[k + 2], out var n))
                    {
                        SetColor(st, slot, n);
                        k += 2;
                    }
                    else if (k + 4 < parts.Length && parts[k + 1] == "2" &&
                             int.TryParse(parts[k + 2], out var r) &&
                             int.TryParse(parts[k + 3], out var g) &&
                             int.TryParse(parts[k + 4], out var b))
                    {
                        SetColor(st, slot, AnsiTty.RgbCode(r, g, b));
                        k += 4;
                    }
                    break;
            }
        }
    }

    /// <summary>写入前景/背景色。真彩码原样存（渲染端认 <c>≥0x1000000</c>）。</summary>
    private static void SetColor(Style st, int slot, int code)
    {
        // 256 色里 0-15 是标准 16 色，换算成 30-37/90-97 走命名表（与 FgTag 的分派一致）
        if (code is >= 0 and <= 7) code += 30;
        else if (code is >= 8 and <= 15) code += 90 - 8;

        if (slot == 1) st.Fg = code; else st.Bg = code;
    }
}
