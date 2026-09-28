// ANSI 彩色输出体检 —— 一次把 `AnsiMarkup` 支持的全部子集打出来。
// ANSI color output health check — print the entire subset that `AnsiMarkup` supports in one go.
//
// 这是**命令行页彩色链路**的端到端判据：手机「命令行」页把程序 stdout 里的 ANSI
// This is the end-to-end criterion for the **command-line page color pipeline**: the phone's "command line" page turns the ANSI in a program's stdout
// 转成仓库统一的 «» 中间格式（`UI/Shared/AnsiMarkup.cs`），再由渲染器上色。
// into the repo-wide «» intermediate format (`UI/Shared/AnsiMarkup.cs`), and the renderer then colors it.
// 那条路此前只有**单元测试**（`SelfTest.Chunk10.cs` 的 TestAnsiMarkup 查转换结果），
// That path previously had only a **unit test** (TestAnsiMarkup in `SelfTest.Chunk10.cs` checks the conversion result),
// 没有能一眼看出"到底显示成什么样"的示例 —— 这个就是。
// with no example that shows at a glance "what it actually looks like on screen" — this is that one.
//
// ⚠ 转义写法**必须用 `\x1b`，不能用 `\033`**（实测）：
// ⚠ The escape form **must use `\x1b`, not `\033`** (measured):
//   · `\x1b[31m`  ✅ 正确输出 ESC(0x1B)
//   · `\x1b[31m`  ✅ correctly emits ESC(0x1B)
//   · `\033[31m`  ❌ C 前端把它解析成 `\0`（NUL）+ 字面量 `33` ⇒ **字符串在 NUL 处终止**，
//   · `\033[31m` ❌ the C frontend parses it as `\0` (NUL) + the literal `33` ⇒ **the string terminates at the NUL**,
//                    整行只剩第一个字符（`"A\033[31mB"` 只打出 `A`）
//                    so the whole line keeps only its first character (`"A\033[31mB"` prints only `A`)
//   · `\e[31m`    ❌ 不支持，退化成字面量字母 `e`
//   · `\e[31m`    ❌ not supported, degrades into the literal letter `e`
//   这条缺陷目前没登记判据 —— 写 ANSI 输出的程序全都会踩。
//   This defect has no registered criterion yet — every program that writes ANSI output trips over it.
//
// ⚠ 两个刻意的取舍（与 AnsiMarkup 一致）：**不做反白**（SGR 7，它要交换前景/背景，
// ⚠ Two deliberate trade-offs (consistent with AnsiMarkup): **no reverse video** (SGR 7, since it would swap foreground/background,
//   而本项目取色跟着日/夜主题走，"交换"在主题适配之后语义就变了）；
//   while this project picks colors by day/night theme, so "swapping" changes meaning once themes are adapted);
//   光标/擦除/OSC 等**非 SGR 序列会被吃掉**，所以这里也不演示。
//   cursor/erase/OSC and other **non-SGR sequences get eaten**, so they are not demonstrated here either.
//
// 用 `puts` 而不是 `printf`：桌面脚手架里 printf 的格式化路径会崩（见 CLAUDE.md
// Use `puts` rather than `printf`: in the desktop scaffold printf's formatting path crashes (see CLAUDE.md
// 的「桌面脚手架三条既有事实」），而 `puts` 正常。
// "three existing facts about the desktop scaffold"), whereas `puts` works fine.
//
// 跑法：命令行页输入  vml run examples/c/ansi_colors.c
// How to run: type this on the command-line page  vml run examples/c/ansi_colors.c

int main()
{
    int lang;

    // 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调）
    // UI language: queried once at start (ui_get_language is a syscall, do not call it every frame)
    lang = ui_get_language();

    // ── 标准 8 色前景（30–37）────────────────────────────────
    // ── Standard 8 foreground colors (30–37)────────────────────────────────
    puts(lang == 0 ? "\x1b[30m30 黑\x1b[0m  \x1b[31m31 红\x1b[0m  \x1b[32m32 绿\x1b[0m  \x1b[33m33 黄\x1b[0m"
                   : "\x1b[30m30 black\x1b[0m  \x1b[31m31 red\x1b[0m  \x1b[32m32 green\x1b[0m  \x1b[33m33 yellow\x1b[0m");
    puts(lang == 0 ? "\x1b[34m34 蓝\x1b[0m  \x1b[35m35 品红\x1b[0m  \x1b[36m36 青\x1b[0m  \x1b[37m37 白\x1b[0m"
                   : "\x1b[34m34 blue\x1b[0m  \x1b[35m35 magenta\x1b[0m  \x1b[36m36 cyan\x1b[0m  \x1b[37m37 white\x1b[0m");

    // ── 亮色前景（90–97）────────────────────────────────────
    // ── Bright foreground colors (90–97)────────────────────────────────────
    puts(lang == 0 ? "\x1b[90m90 亮黑(灰)\x1b[0m  \x1b[91m91 亮红\x1b[0m  \x1b[92m92 亮绿\x1b[0m  \x1b[93m93 亮黄\x1b[0m"
                   : "\x1b[90m90 gray\x1b[0m  \x1b[91m91 bright red\x1b[0m  \x1b[92m92 bright green\x1b[0m  \x1b[93m93 bright yellow\x1b[0m");
    puts(lang == 0 ? "\x1b[94m94 亮蓝\x1b[0m  \x1b[95m95 亮品红\x1b[0m  \x1b[96m96 亮青\x1b[0m  \x1b[97m97 亮白\x1b[0m"
                   : "\x1b[94m94 bright blue\x1b[0m  \x1b[95m95 bright magenta\x1b[0m  \x1b[96m96 bright cyan\x1b[0m  \x1b[97m97 bright white\x1b[0m");

    // ── 背景色（40–47 / 100–107）────────────────────────────
    // ── Background colors (40–47 / 100–107)────────────────────────────
    puts(lang == 0 ? "\x1b[41m 红底 \x1b[0m \x1b[42m 绿底 \x1b[0m \x1b[44m 蓝底 \x1b[0m \x1b[103m 亮黄底 \x1b[0m \x1b[105m 亮品红底 \x1b[0m"
                   : "\x1b[41m red bg \x1b[0m \x1b[42m green bg \x1b[0m \x1b[44m blue bg \x1b[0m \x1b[103m bright yellow bg \x1b[0m \x1b[105m bright magenta bg \x1b[0m");

    // ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────
    // ── Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough)─────
    puts(lang == 0 ? "\x1b[1m粗体 bold\x1b[0m  \x1b[2m暗淡 dim\x1b[0m  \x1b[3m斜体 italic\x1b[0m  \x1b[4m下划线 underline\x1b[0m  \x1b[9m删除线 strike\x1b[0m"
                   : "\x1b[1mbold\x1b[0m  \x1b[2mdim\x1b[0m  \x1b[3mitalic\x1b[0m  \x1b[4munderline\x1b[0m  \x1b[9mstrike\x1b[0m");

    // ── 组合：粗体 + 颜色 ───────────────────────────────────
    // ── Combination: bold + color ───────────────────────────────────
    // 「粗体 + 颜色」在本仓是编码进颜色高位的（AnsiTty.BoldFlag），
    // "Bold + color" is encoded into the color's high bits in this repo (AnsiTty.BoldFlag),
    // 所以这一行同时压住了那条路径。
    // so this line covers that path too.
    puts(lang == 0 ? "\x1b[1;31m粗红\x1b[0m  \x1b[1;92m粗亮绿\x1b[0m  \x1b[4;34m下划线蓝\x1b[0m"
                   : "\x1b[1;31mbold red\x1b[0m  \x1b[1;92mbold bright green\x1b[0m  \x1b[4;34munderline blue\x1b[0m");

    // ── 256 色（38;5;N）─────────────────────────────────────
    // ── 256 colors (38;5;N)─────────────────────────────────────
    puts(lang == 0 ? "\x1b[38;5;208m256-208 橙\x1b[0m  \x1b[38;5;46m256-46 亮绿\x1b[0m  \x1b[38;5;196m256-196 正红\x1b[0m  \x1b[38;5;240m256-240 灰\x1b[0m"
                   : "\x1b[38;5;208m256-208 orange\x1b[0m  \x1b[38;5;46m256-46 bright green\x1b[0m  \x1b[38;5;196m256-196 pure red\x1b[0m  \x1b[38;5;240m256-240 gray\x1b[0m");

    // ── 真彩（38;2;r;g;b）───────────────────────────────────
    // ── Truecolor (38;2;r;g;b)───────────────────────────────────
    // ⚠ 真彩码的数值**大于 255**，解析时必须排在 256 色之前判断，
    // ⚠ The truecolor code's value **exceeds 255**, so it must be tested before the 256-color case,
    //   否则会掉进 256 色分支、全部渲染成同一个颜色。
    //   otherwise it falls into the 256-color branch and everything renders as one and the same color.
    puts(lang == 0 ? "\x1b[38;2;255;128;0m真彩 橙\x1b[0m  \x1b[38;2;0;200;255m真彩 青\x1b[0m  \x1b[38;2;200;0;255m真彩 紫\x1b[0m"
                   : "\x1b[38;2;255;128;0mtruecolor orange\x1b[0m  \x1b[38;2;0;200;255mtruecolor cyan\x1b[0m  \x1b[38;2;200;0;255mtruecolor purple\x1b[0m");

    // ── 真彩背景 ────────────────────────────────────────────
    // ── Truecolor backgrounds ────────────────────────────────────────────
    puts(lang == 0 ? "\x1b[48;2;60;0;90m 真彩深紫底 \x1b[0m  \x1b[48;2;0;90;60m 真彩墨绿底 \x1b[0m"
                   : "\x1b[48;2;60;0;90m truecolor deep purple bg \x1b[0m  \x1b[48;2;0;90;60m truecolor dark green bg \x1b[0m");

    // ── 兜底：纯文本（不该被染色）───────────────────────────
    // ── Fallback: plain text (should not get colored)───────────────────────────
    puts(lang == 0 ? "以上全部结束 —— 这一行是纯文本，应当没有颜色。"
                   : "That is everything -- this line is plain text and should have no color.");
    return 0;
}
