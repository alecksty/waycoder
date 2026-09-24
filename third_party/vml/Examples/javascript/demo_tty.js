// demo_tty.js —— **第 2 层：彩色控制台**
//
// 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
// 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
//
// JavaScript 这一层**没有 conio/crt 绑定**，所以这里**直接输出 ANSI 转义序列**
// —— 那正是"支持彩色的控制台程序"：
//
//     清屏        ESC [ 2 J          ESC [ H
//     定位光标    ESC [ <行> ; <列> H
//     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
//     复位        ESC [ 0 m
//
// ## ⚠ 本前端的三条限制（写 demo 时避开）
//
//   · **字符串 + 数字编出来的结果是错的** —— `"line " + i` 打出 `1059` 这类地址量级
//     的值。所以转义串**整条写字面量**，一个字都不拼。
//   · **没有 `String(n)` / `n.toString()`**（都报「未定义的函数」），
//     `int_to_str(n)` 恒返回 `0` ⇒ 数字转字符串这条路是断的。
//   · `console.log` 会补换行，而且多实参**直接相连**（`console.log("a","b")` → `ab`）
//     ⇒ 这里一律用**不补换行**的 `print_str`（它在库映射表里，不用 `native function`
//     声明；本份也确实没写任何声明，可以直接对照 `Examples/javascript/q*.js` 的做法）。
//
// ## 在哪儿能看到什么
//
// · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
// · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
//   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
//   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
//   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
//   定位被吃掉时输出仍然一行一句、读得通。
//
// 跑法：命令行页输入  vml run examples/javascript/demo_tty.js

// ── 开场：清屏 + 回左上角 ───────────────────────────────────────
print_str("\x1b[2J");
print_str("\x1b[H");

// ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
print_str("\x1b[1;1H");
print_str("\x1b[44;97m");
print_str("  demo_tty (JavaScript) —— 彩色控制台 / ANSI 转义序列      ");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[2;1H");
print_str("\x1b[90m");
print_str("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m");
print_str("\x1b[0m");
print_str("\n");

// ── 标准 8 色前景（30–37）──────────────────────────────────────
print_str("\x1b[4;1H");
print_str("\x1b[1;37m");
print_str("标准 8 色前景：");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[5;1H");
print_str("\x1b[30m");
print_str(" 30 黑 ");
print_str("\x1b[0m");
print_str("\x1b[31m");
print_str(" 31 红 ");
print_str("\x1b[0m");
print_str("\x1b[32m");
print_str(" 32 绿 ");
print_str("\x1b[0m");
print_str("\x1b[33m");
print_str(" 33 黄 ");
print_str("\x1b[0m");
print_str("\x1b[34m");
print_str(" 34 蓝 ");
print_str("\x1b[0m");
print_str("\x1b[35m");
print_str(" 35 品红 ");
print_str("\x1b[0m");
print_str("\x1b[36m");
print_str(" 36 青 ");
print_str("\x1b[0m");
print_str("\x1b[37m");
print_str(" 37 白 ");
print_str("\x1b[0m");
print_str("\n");

// ── 亮色前景（90–97）────────────────────────────────────────────
print_str("\x1b[6;1H");
print_str("\x1b[90m");
print_str(" 90 亮黑(灰) ");
print_str("\x1b[0m");
print_str("\x1b[91m");
print_str(" 91 亮红 ");
print_str("\x1b[0m");
print_str("\x1b[92m");
print_str(" 92 亮绿 ");
print_str("\x1b[0m");
print_str("\x1b[93m");
print_str(" 93 亮黄 ");
print_str("\x1b[0m");
print_str("\x1b[94m");
print_str(" 94 亮蓝 ");
print_str("\x1b[0m");
print_str("\x1b[95m");
print_str(" 95 亮品红 ");
print_str("\x1b[0m");
print_str("\x1b[96m");
print_str(" 96 亮青 ");
print_str("\x1b[0m");
print_str("\x1b[97m");
print_str(" 97 亮白 ");
print_str("\x1b[0m");
print_str("\n");

// ── 背景色（40–47 / 100–107）───────────────────────────────────
print_str("\x1b[8;1H");
print_str("\x1b[1;37m");
print_str("背景色：");
print_str("\x1b[0m");
print_str("\n");

print_str("\x1b[9;1H");
print_str("\x1b[41m");
print_str(" 红底 ");
print_str("\x1b[0m");
print_str("\x1b[42m");
print_str(" 绿底 ");
print_str("\x1b[0m");
print_str("\x1b[44m");
print_str(" 蓝底 ");
print_str("\x1b[0m");
print_str("\x1b[46m");
print_str(" 青底 ");
print_str("\x1b[0m");
print_str("\x1b[103m");
print_str(" 亮黄底 ");
print_str("\x1b[0m");
print_str("\x1b[105m");
print_str(" 亮品红底 ");
print_str("\x1b[0m");
print_str("\n");

// ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
print_str("\x1b[11;1H");
print_str("\x1b[1;37m");
print_str("样式：");
print_str("\x1b[0m");
print_str("\x1b[1m");
print_str(" 粗体 ");
print_str("\x1b[0m");
print_str("\x1b[2m");
print_str(" 暗淡 ");
print_str("\x1b[0m");
print_str("\x1b[3m");
print_str(" 斜体 ");
print_str("\x1b[0m");
print_str("\x1b[4m");
print_str(" 下划线 ");
print_str("\x1b[0m");
print_str("\x1b[9m");
print_str(" 删除线 ");
print_str("\x1b[0m");
print_str("\n");

// ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
print_str("\x1b[13;1H");
print_str("\x1b[1;37m");
print_str("256 色 / 真彩：");
print_str("\x1b[0m");
print_str("\x1b[38;5;208m");
print_str(" 256-208 橙 ");
print_str("\x1b[0m");
print_str("\x1b[38;5;46m");
print_str(" 256-46 亮绿 ");
print_str("\x1b[0m");
print_str("\x1b[38;2;255;128;0m");
print_str(" 真彩橙 ");
print_str("\x1b[0m");
print_str("\x1b[48;2;60;0;90m");
print_str(" 真彩深紫底 ");
print_str("\x1b[0m");
print_str("\n");

// ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
// 「算出来的」那一格：取模 + 分支挑字面量色码（字符串拼接不可用，见文件头）。
print_str("\x1b[16;1H");
print_str("\x1b[1;37m");
print_str("循环画 12 格色带：");
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
print_str("\x1b[0m");
print_str("\x1b[20;1H");
print_str("\x1b[1;32m");
print_str("demo_tty 结束 —— 没有按键等待，画完即退出。");
print_str("\x1b[0m");
print_str("\n");
