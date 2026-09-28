// demo_tty.js —— **第 2 层：彩色控制台**
// demo_tty.js -- **layer 2: color console**
//
// 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
// The problem this layer solves is "**laying out a character display**": clear the screen, move the cursor to a given row and column,
// 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
// set foreground/background colors. Old DOS programs (menus, tables, status bars) were built entirely from these primitives.
//
// JavaScript 这一层**没有 conio/crt 绑定**，所以这里**直接输出 ANSI 转义序列**
// JavaScript has **no conio/crt binding** at this layer, so this file **emits ANSI escape sequences directly**
// —— 那正是"支持彩色的控制台程序"：
// -- which is exactly a "console program with color support":
//
//     清屏        ESC [ 2 J          ESC [ H
//     clear screen        ESC [ 2 J          ESC [ H
//     定位光标    ESC [ <行> ; <列> H
//     position cursor     ESC [ <row> ; <col> H
//     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
//     foreground/background  ESC [ 3x m / 4x m / 9x m / 10x m, 256 color 38;5;N, true color 38;2;r;g;b
//     复位        ESC [ 0 m
//     reset        ESC [ 0 m
//
// ## ⚠ 本前端的三条限制（写 demo 时避开）
// ## ⚠ Three limitations of this frontend (avoid them when writing a demo)
//
//   · **字符串 + 数字编出来的结果是错的** —— `"line " + i` 打出 `1059` 这类地址量级
//   · **String + number compiles to a wrong result** -- `"line " + i` prints address-sized values
//     的值。所以转义串**整条写字面量**，一个字都不拼。
//     like `1059`. So every escape sequence is **written whole as a literal**, never assembled.
//   · **没有 `String(n)` / `n.toString()`**（都报「未定义的函数」），
//   · **There is no `String(n)` / `n.toString()`** (both report "undefined function"),
//     `int_to_str(n)` 恒返回 `0` ⇒ 数字转字符串这条路是断的。
//     and `int_to_str(n)` always returns `0` => the number-to-string path is broken.
//   · `console.log` 会补换行，而且多实参**直接相连**（`console.log("a","b")` → `ab`）
//   · `console.log` appends a newline, and its multiple arguments are **joined directly** (`console.log("a","b")` -> `ab`)
//     ⇒ 这里一律用**不补换行**的 `print_str`（它在库映射表里，不用 `native function`
//     => this file always uses `print_str`, which **appends no newline** (it is in the library mapping table, so no `native function`
//     声明；本份也确实没写任何声明，可以直接对照 `Examples/javascript/q*.js` 的做法）。
//     declaration is needed; this file indeed writes none, and can be compared with the approach in `Examples/javascript/q*.js`).
//
// ## 在哪儿能看到什么
// ## What you can see where
//
// · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
// · **A real terminal** (`vmlcli ... | cat -v`): the whole escape set takes effect.
// · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
// · **The phone's "command line" page**: that layer turns ANSI on stdout into the repo's unified `«»` intermediate format
//   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
//   (`UI/Shared/AnsiMarkup.cs`) and **understands only SGR (color/style)**; cursor positioning (`ESC[...H`)
//   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
//   and clearing the screen (`ESC[2J`) are **eaten** -- a deliberately limited subset (see
//   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
//   the notes in `Examples/c/ansi_colors.c`). So every line ends with a newline,
//   定位被吃掉时输出仍然一行一句、读得通。
//   and when positioning is eaten the output is still one sentence per line and readable.
//
// 跑法：命令行页输入  vml run examples/javascript/demo_tty.js
// How to run: type this into the command-line page:  vml run examples/javascript/demo_tty.js

// 界面语言：开局查一次（`ui_get_language` 是 syscall，别每处都调）
// UI language: queried once at start (`ui_get_language` is a syscall, do not call it everywhere)
var lang = ui_get_language();

// ── 开场：清屏 + 回左上角 ───────────────────────────────────────
// -- Opening: clear the screen + go back to the top left --
print_str("\x1b[2J");
print_str("\x1b[H");

// ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
// -- Title bar: white on blue (ESC[44;97m) --
print_str("\x1b[1;1H");
print_str("\x1b[44;97m");
print_str(lang == 0 ? "  demo_tty (JavaScript) —— 彩色控制台 / ANSI 转义序列      " : "  demo_tty (JavaScript) -- color console / ANSI escapes     ");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[2;1H");
print_str("\x1b[90m");
print_str(lang == 0 ? "清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m" : "clear ESC[2J   cursor ESC[r;cH   color ESC[3xm / ESC[4xm   reset ESC[0m");
print_str("\x1b[0m");
print_str("\n");

// ── 标准 8 色前景（30–37）──────────────────────────────────────
// -- The standard 8 foreground colors (30-37) --
print_str("\x1b[4;1H");
print_str("\x1b[1;37m");
print_str(lang == 0 ? "标准 8 色前景：" : "standard 8 foreground colors:");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[5;1H");
print_str("\x1b[30m");
print_str(lang == 0 ? " 30 黑 " : " 30 black ");
print_str("\x1b[0m");
print_str("\x1b[31m");
print_str(lang == 0 ? " 31 红 " : " 31 red ");
print_str("\x1b[0m");
print_str("\x1b[32m");
print_str(lang == 0 ? " 32 绿 " : " 32 green ");
print_str("\x1b[0m");
print_str("\x1b[33m");
print_str(lang == 0 ? " 33 黄 " : " 33 yellow ");
print_str("\x1b[0m");
print_str("\x1b[34m");
print_str(lang == 0 ? " 34 蓝 " : " 34 blue ");
print_str("\x1b[0m");
print_str("\x1b[35m");
print_str(lang == 0 ? " 35 品红 " : " 35 magenta ");
print_str("\x1b[0m");
print_str("\x1b[36m");
print_str(lang == 0 ? " 36 青 " : " 36 cyan ");
print_str("\x1b[0m");
print_str("\x1b[37m");
print_str(lang == 0 ? " 37 白 " : " 37 white ");
print_str("\x1b[0m");
print_str("\n");

// ── 亮色前景（90–97）────────────────────────────────────────────
// -- Bright foreground colors (90-97) --
print_str("\x1b[6;1H");
print_str("\x1b[90m");
print_str(lang == 0 ? " 90 亮黑(灰) " : " 90 gray ");
print_str("\x1b[0m");
print_str("\x1b[91m");
print_str(lang == 0 ? " 91 亮红 " : " 91 red ");
print_str("\x1b[0m");
print_str("\x1b[92m");
print_str(lang == 0 ? " 92 亮绿 " : " 92 green ");
print_str("\x1b[0m");
print_str("\x1b[93m");
print_str(lang == 0 ? " 93 亮黄 " : " 93 yellow ");
print_str("\x1b[0m");
print_str("\x1b[94m");
print_str(lang == 0 ? " 94 亮蓝 " : " 94 blue ");
print_str("\x1b[0m");
print_str("\x1b[95m");
print_str(lang == 0 ? " 95 亮品红 " : " 95 magenta ");
print_str("\x1b[0m");
print_str("\x1b[96m");
print_str(lang == 0 ? " 96 亮青 " : " 96 cyan ");
print_str("\x1b[0m");
print_str("\x1b[97m");
print_str(lang == 0 ? " 97 亮白 " : " 97 white ");
print_str("\x1b[0m");
print_str("\n");

// ── 背景色（40–47 / 100–107）───────────────────────────────────
// -- Background colors (40-47 / 100-107) --
print_str("\x1b[8;1H");
print_str("\x1b[1;37m");
print_str(lang == 0 ? "背景色：" : "background colors:");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[9;1H");
print_str("\x1b[41m");
print_str(lang == 0 ? " 红底 " : " red bg ");
print_str("\x1b[0m");
print_str("\x1b[42m");
print_str(lang == 0 ? " 绿底 " : " green bg ");
print_str("\x1b[0m");
print_str("\x1b[44m");
print_str(lang == 0 ? " 蓝底 " : " blue bg ");
print_str("\x1b[0m");
print_str("\x1b[46m");
print_str(lang == 0 ? " 青底 " : " cyan bg ");
print_str("\x1b[0m");
print_str("\x1b[103m");
print_str(lang == 0 ? " 亮黄底 " : " bright yellow bg ");
print_str("\x1b[0m");
print_str("\x1b[105m");
print_str(lang == 0 ? " 亮品红底 " : " bright magenta bg ");
print_str("\x1b[0m");
print_str("\n");

// ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
// -- Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) --
print_str("\x1b[11;1H");
print_str("\x1b[1;37m");
print_str(lang == 0 ? "样式：" : "styles:");
print_str("\x1b[0m");
print_str("\x1b[1m");
print_str(lang == 0 ? " 粗体 " : " bold ");
print_str("\x1b[0m");
print_str("\x1b[2m");
print_str(lang == 0 ? " 暗淡 " : " dim ");
print_str("\x1b[0m");
print_str("\x1b[3m");
print_str(lang == 0 ? " 斜体 " : " italic ");
print_str("\x1b[0m");
print_str("\x1b[4m");
print_str(lang == 0 ? " 下划线 " : " underline ");
print_str("\x1b[0m");
print_str("\x1b[9m");
print_str(lang == 0 ? " 删除线 " : " strikethrough ");
print_str("\x1b[0m");
print_str("\n");

// ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
// -- 256 colors (38;5;N) and true color (38;2;r;g;b) --
print_str("\x1b[13;1H");
print_str("\x1b[1;37m");
print_str(lang == 0 ? "256 色 / 真彩：" : "256 colors / true color:");
print_str("\x1b[0m");
print_str("\x1b[38;5;208m");
print_str(lang == 0 ? " 256-208 橙 " : " 256-208 orange ");
print_str("\x1b[0m");
print_str("\x1b[38;5;46m");
print_str(lang == 0 ? " 256-46 亮绿 " : " 256-46 bright green ");
print_str("\x1b[0m");
print_str("\x1b[38;2;255;128;0m");
print_str(lang == 0 ? " 真彩橙 " : " true-color orange ");
print_str("\x1b[0m");
print_str("\x1b[48;2;60;0;90m");
print_str(lang == 0 ? " 真彩深紫底 " : " true-color purple bg ");
print_str("\x1b[0m");
print_str("\n");

// ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
// -- Draw a color band in a loop (12 cells, colors cycling through 8 steps) --
// 「算出来的」那一格：取模 + 分支挑字面量色码（字符串拼接不可用，见文件头）。
// The "computed" cell: modulo plus a branch picking a literal color code (string concatenation is unusable, see the file header).
print_str("\x1b[16;1H");
print_str("\x1b[1;37m");
print_str(lang == 0 ? "循环画 12 格色带：" : "12-cell color band (loop):");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[17;1H");
var i = 0;
while (i < 12) {
    var k = i % 8;
    if (k == 0) { print_str("\x1b[41m"); }
    if (k == 1) { print_str("\x1b[42m"); }
    if (k == 2) { print_str("\x1b[43m"); }
    if (k == 3) { print_str("\x1b[44m"); }
    if (k == 4) { print_str("\x1b[45m"); }
    if (k == 5) { print_str("\x1b[46m"); }
    if (k == 6) { print_str("\x1b[47m"); }
    if (k == 7) { print_str("\x1b[100m"); }
    print_str("   ");
    print_str("\x1b[0m");
    i = i + 1;
}
print_str("\n");

// ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
// -- Wrap-up: reset the colors + position to row 20 for the closing line --
print_str("\x1b[0m");
print_str("\x1b[20;1H");
print_str("\x1b[1;32m");
print_str(lang == 0 ? "demo_tty 结束 —— 没有按键等待，画完即退出。" : "demo_tty done -- no key wait, it draws and exits.");
print_str("\x1b[0m");
print_str("\n");
