using System.Text;

namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 命令行输出里**标准控制字符**的语义（`\r` / `\t` / `\b`）—— 按终端标准实现。
///
/// 为什么必须做：老 CLI 工具的输出里有三种字符**不是"文字"**，而是终端指令：
///   · **`\r`（CR）** —— 光标回行首，后续输出**覆写同一行**。**进度条全靠它**
///     （`[####    ] 40%` 反复打在一条线上）。不实现的话，一个 5 步的进度条会刷出 5 行。
///   · **`\t`（TAB）** —— 跳到下一个制表位（标准每 8 列一个）。**表格对齐全靠它**
///     （`ls`/`df`/`ps` 少一个就整片歪）。
///   · **`\b`（BS）** —— 光标退一格，用于**叠打**。老手册页的粗体就是 `X\bX`
///     （打两遍、中间退一格）。至少不能把退格符当可见字符显示出来。
///
/// 处理**在 ANSI → 标记转换之前**做（`Append` 里第一件事）：这几个字符的语义是
/// "屏幕上占几格"，一旦转成 `«red»` 标记，列数就没法算了。
///
/// 两条容易漏的：
/// ① **ANSI 转义序列算 0 格** —— 它们不占屏幕位置；算进去制表位与退格全会错位。
/// ② **行尾的 `\r` 是 CRLF，不是"回行首"** —— 调用方按 `\n` 切段，Windows 换行会留下
///    一个孤零零的 `\r`；当成回行首就把**整行内容清掉**（实测会丢整整一行输出）。
/// </summary>
public static class ShellControls
{
    /// <summary>制表位间隔（终端标准是 8）。</summary>
    public const int TabStop = 8;

    /// <summary>
    /// 把一段**带 ANSI 的原始输出**里的 `\r`/`\t`/`\b` 按标准语义解析掉，
    /// 返回"只含可见字符 + 换行"的文本（ANSI 序列原样保留，后面还要转颜色）。
    /// </summary>
    /// <param name="raw">
    /// 原始输出。**单行或多行都行** —— 两个调用点形态不同：
    /// `ShellPage.Append` 给的是切好的一段，`MauiVml` 给的是整段多行输出（要在
    /// `AnsiMarkup` 之前跑，否则控制字符被它吃掉）。所以 `\n` 必须在这里结算当前行。
    /// </param>
    public static string Apply(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (raw.IndexOf('\r') < 0 && raw.IndexOf('\t') < 0 && raw.IndexOf('\b') < 0)
            return raw;                                   // 绝大多数输出走这条快路径

        // 行尾的 `\r` = CRLF 的残留，不是"回行首"（见类注释 ②）
        if (raw.EndsWith('\r')) raw = raw[..^1];
        if (raw.Length == 0) return raw;

        var outp = new StringBuilder(raw.Length + 16);
        var line = new StringBuilder(raw.Length);
        var col = 0;                                      // 当前行的**显示宽度**
        var esc = false;                                  // 是否在 ANSI 转义序列里

        foreach (var r in raw.EnumerateRunes())
        {
            // ── ANSI 转义序列：零宽，原样带着走（颜色后面还要用）──
            if (esc)
            {
                line.Append(r.ToString());
                // 终结字符：字母（`m`/`K`/`H`/`J`…）或 `~`（`\x1b[1~` 那种功能键序列）
                if (System.Text.Rune.IsLetter(r) || r.Value == '~') esc = false;
                continue;
            }
            // ⚠ ESC 一律走 `AnsiTty.AnsiCharPrefix` —— 本仓的 lint 禁止 UI 层出现裸转义字面量
            //   （`\x1b` 只允许写在 `AnsiTty.cs` 那一个文件里）
            if (r.Value == AnsiTty.AnsiCharPrefix) { esc = true; line.Append(r.ToString()); continue; }

            // ── `\n`：**结算当前行**。⚠ 必须有这一条：本函数既能收到"切好的一行"
            //   （`ShellPage.Append` 那段），也能收到**整段多行输出**（`MauiVml` 里
            //   在 `ToMarkup` 之前那一步）。不结算的话 `line` 会一路攒到底，
            //   后面任何一个 `\r` 就把**前面所有行**一起清掉 ——
            //   实测症状：进度条那节把自己的标题行也吃了，屏幕上只剩最后一帧。
            if (r.Value == '\n')
            {
                outp.Append(line);
                outp.Append('\n');
                line.Clear();
                col = 0;
                continue;
            }

            // ── `\r`：回行首 ⇒ 丢掉本行已写的内容，后续覆写它 ──
            if (r.Value == '\r')
            {
                line.Clear();
                col = 0;
                continue;
            }

            // ── `\t`：补空格到下一个制表位 ──
            if (r.Value == '\t')
            {
                var pad = TabStop - col % TabStop;
                line.Append(' ', pad);
                col += pad;
                continue;
            }

            // ── `\b`：退一格（吃掉行尾最后一个**可见**字符；空行时忽略）──
            if (r.Value == '\b')
            {
                if (line.Length > 0 && col > 0)
                {
                    // ⚠ 先把**尾部的 ANSI 序列**跳过去 —— 退格要落在可见字符上。
                    // 不跳的话退掉的是 `\x1b[0m` 里的 `m`（实测：`abc\x1b[0m\b` 会退成
                    // `abc\x1b[0`），既没退对字符、还把颜色标记弄坏了。
                    var s = line.ToString();
                    var end = s.Length;
                    if (end > 0 && Rune.IsLetter(Rune.GetRuneAt(s, end - 1)))
                    {
                        var p = s.LastIndexOf(AnsiTty.AnsiCharPrefix, end - 1);
                        if (p >= 0) end = p;
                    }

                    // 取最后一个 **rune**（不能按 char 取：emoji/CJK 扩展 B 是代理对，
                    // 按 char 退一格会把它劈成半个 —— 与仓库「截断必须按码点」同一条规矩）
                    var visible = s[..end];
                    var all = visible.EnumerateRunes().ToArray();
                    if (all.Length > 0)
                    {
                        var lastRune = all[^1];
                        // ⚠ **只删那一个 rune**，它后面的转义序列要留着 ——
                        // 真终端里那些序列（如 `\x1b[0m` 复位）**已经生效了**，
                        // 按"截断到 ESC 处"处理会把颜色复位一起吃掉。
                        line.Remove(end - lastRune.Utf16SequenceLength, lastRune.Utf16SequenceLength);
                        col = Math.Max(0, col - Math.Max(1, AnsiString.CharWidth(lastRune)));
                    }
                }
                continue;
            }

            // ── 普通字符：只累计显示宽度（**不在这里折行**）──
            // 折行是 `ShellWrap` 在**呈现**时干的活（它还要避开 `«»` 标记、按显示宽度算）。
            // 这里再折一遍就是二次折行，列对齐会散 —— 而且这条快路径根本走不到。
            line.Append(r.ToString());
            col += Math.Max(1, AnsiString.CharWidth(r));
        }

        outp.Append(line);
        return outp.ToString();
    }
}
