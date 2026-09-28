/* demo_tty.c —— **第 2 层：彩色命令行**（`tty_*` / `Lib/c/tty.h`）
 * demo_tty.c -- **Layer 2: the color command line** (`tty_*` / `Lib/c/tty.h`)
 *
 * ## 这一层是干嘛的
 * ## What this layer is for
 *
 * 「**在一个字符界面上做版面**」—— 清屏、把光标挪到第几行第几列、设前景/背景色、
 * "Lay out a character screen" -- clear it, move the cursor to a row/column, set fg/bg colors,
 * 画个框。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
 * draw a box. Old DOS programs (menus, tables, status bars) are built from these primitives.
 *
 * ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
 * ⚠ **New programs use `tty_*`; only old programs use `conio` / `Crt`** (the agreed layering rule:
 *   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
 *   "new programs use the tty lib for text mode and the ui lib for graphics; old programs use conio, graphics, etc.").
 *   `tty_*` 是这套能力的**正式名字**，22 门语言写出来的是**同一串调用**；
 *   `tty_*` is the **official name** for this capability; all 22 languages emit the **same call sequence**;
 *   `Lib/c/conio.h` 那一套是**兼容层**，留给老源码。
 *   the `Lib/c/conio.h` set is a **compatibility layer**, kept for old sources.
 *
 * ## 它是字符库，不是图形库
 * ## It is a character library, not a graphics library
 *
 * **只有字符，没有任何图形功能**。要画图请用别的层：
 * **Characters only, no graphics features whatsoever**. To draw, use another layer:
 *   · 传统固定分辨率图形 → BGI（`Lib/c/graphics.h`，见 `demo_bgi.c`）
 *   · classic fixed-resolution graphics -> BGI (`Lib/c/graphics.h`, see `demo_bgi.c`)
 *   · 现代图元           → `ui_*`（`Lib/c/waycoder_ui.h`，见 `demo_ui.c`）
 *   · modern primitives             -> `ui_*` (`Lib/c/waycoder_ui.h`, see `demo_ui.c`)
 *
 * ## 坐标与颜色
 * ## Coordinates and colors
 *
 * · 坐标 **1 起算**（`(1,1)` 左上角）—— 与 `gotoxy` / `GotoXY` / `LOCATE` 一致。
 * · Coordinates are **1-based** (`(1,1)` is the top-left corner) -- same as `gotoxy` / `GotoXY` / `LOCATE`.
 * · 颜色 **0–15 索引色**：`0 黑 1 蓝 2 绿 3 青 4 红 5 品红 6 棕 7 浅灰
 * · Colors are **0-15 indexed**: 0 black 1 blue 2 green 3 cyan 4 red 5 magenta 6 brown 7 lightgray
 *   8 深灰 9 亮蓝 10 亮绿 11 亮青 12 亮红 13 亮洋红 14 黄 15 白`
 *   8 darkgray 9 lightblue 10 lightgreen 11 lightcyan 12 lightred 13 lightmagenta 14 yellow 15 white
 *
 * ## 它画到哪儿
 * ## Where it draws
 *
 * **主屏**（默认）= 命令行页那个 80×25 的字符网格。`tty_*` 只产生字节
 * The **main screen** (default) = the 80x25 character grid of the command-line page. `tty_*` only emits bytes
 * （光标定位 `ESC[{行};{列}H`、颜色 SGR、字符、清屏 `ESC[2J`），走既有的渲染链，
 * (cursor positioning `ESC[{row};{col}H`, color SGR, characters, clear `ESC[2J`), through the existing render chain,
 * **不弹窗**。副屏（`tty_alt_open`）才会弹一扇窗口当终端 —— 本 demo 不演示它，
 * and **does not pop up a window**. Only the alternate screen (`tty_alt_open`) pops up a window as a terminal -- this demo skips it,
 * 免得"画完就退出"的例程顺手弹一个窗出来。
 * so a "draw and exit" sample does not leave a window behind.
 *
 * ## ⚠ 版面是被一条**已量到的库缺陷**逼成这样的（不是审美选择）
 * ## ⚠ This layout is forced by a **measured library defect** (it is not an aesthetic choice)
 *
 * `Lib/shared/src/conio.c` 的 `putch()` 把**字节**当**列**计数：每个字节 `conX++`、
 * `putch()` in `Lib/shared/src/conio.c` counts **bytes** as **columns**: every byte does `conX++`,
 * 到 80 就折行并重发一条光标定位。而中文 / UTF-8 框线（`┌ ─ │`）一个字符是 **3 个字节**
 * wraps at 80 and re-emits a cursor positioning. But a CJK char / UTF-8 box char (`┌ ─ │`) is **3 bytes**
 * ⇒ 只要**某一段连续输出跨过第 80 列**，折行那一下就会落在**一个字符的中间**，
 * => as soon as **one continuous output span crosses column 80**, the wrap lands **in the middle of a character**,
 * 把一条 `ESC[…H` 插进这个字符的字节序列里。宿主的输出管线是**按字节重组 UTF-8** 的
 * splicing an `ESC[...H` into that character's byte sequence. The host output pipeline **reassembles UTF-8 byte by byte**
 * （`VMLRuntime.Syscall.cs` 的 `TryFlushOneOutputUnit`），校验一旦失败就**粘性**地
 * (`TryFlushOneOutputUnit` in `VMLRuntime.Syscall.cs`); once validation fails it **stickily**
 * 认定"这个程序写的是单字节老编码"（CP437）—— **此后整份输出全乱，不可恢复**。
 * concludes "this program writes an old single-byte encoding" (CP437) -- **from then on the whole output is garbled, unrecoverably**.
 *
 * 最小复现（2026-09-24 实测，`Examples/c/demo_tty.c` 交付报告里也记了）：
 * Minimal reproduction (measured 2026-09-24; also recorded in the `Examples/c/demo_tty.c` delivery report):
 *
 *     tty_goto(10, 2);  for (i=0;i<20;i++) tty_puts("┌");   // 字节跨度 10..70 → 干净 ✓
 // byte span 10..70 → clean ✓
 *     tty_goto(70, 2);  for (i=0;i<10;i++) tty_puts("┌");   // 字节跨度 70..100 → 第 4 个起全乱 ✗
 // byte span 70..100 → from the 4th one on everything is garbled ✗
 *
 * 不是 `tty_*` 的问题 —— 根在 `conio.c`（`tty_puts` 在主屏上就是转发给 `cputs`）。
 * Not a `tty_*` problem -- the root is in `conio.c` (`tty_puts` just forwards to `cputs` on the main screen).
 * 判据：把上面第二行的 `┌` 换成 `A`（1 字节 = 1 列）就完全正常。
 * Test: replace `┌` in the second line above with `A` (1 byte = 1 column) and it is perfectly fine.
 *
 * 所以本 demo 的规矩是：**每一段"定位之后连续输出"的字节跨度不越过第 80 列**，
 * So the rule for this demo is: **no "position then output continuously" span may cross column 80**,
 * 也就是 `起始列 + 3×字符数 ≤ 80`：
 * that is, `start column + 3 x char count <= 80`:
 *   · ASCII 行照常铺满（1 字节 = 1 列，正好是它的真实宽度）
 *   · ASCII lines fill the row as usual (1 byte = 1 column, exactly its real width)
 *   · 中文行都用 `tty_print_at` 重新定位（`tty_goto` 会把列计数清零）
 *   · CJK lines all re-position with `tty_print_at` (`tty_goto` resets the column counter)
 *   · UTF-8 框线只画**窄**的（19 格，从第 4 列起 ⇒ 61 字节）
 *   · UTF-8 box lines are drawn **narrow** only (19 cells, starting at column 4 => 61 bytes)
 *
 * 跑法：命令行页输入  vml run examples/c/demo_tty.c
 * How to run: type  vml run examples/c/demo_tty.c  on the command-line page
 */

#include <tty.h>
#include <waycoder_ui.h>       /* 只为 ui_get_language()（界面语言，见下面 g_lang）*/
/* only for ui_get_language() (the UI language, see g_lang below) */

#define COLS 80
#define ROWS 25

/* 界面语言：开局查一次（ui_get_language 是 syscall，别每行都调）*/
/* UI language: queried once at start (ui_get_language is a syscall, do not call it on every line) */
static int g_lang;

/* 在第 (x,y) 处用指定前景/背景打一串（打完颜色恢复成 7/0） */
/* Print a string at (x,y) with the given fg/bg (the color is restored to 7/0 afterwards) */
static void say(int x, int y, int fg, int bg, char *s)
{
    tty_print_at(x, y, s, fg, bg);
}

int main(void)
{
    int i;

    /* ── 1. 初始化 + 清屏 ──
     * ── 1. Init + clear the screen ──
     * `tty_init(0, 0, 0)` = 尺寸自适应、不清屏（最常用的那一句）；
     * `tty_init(0, 0, 0)` = auto-size and no clear (the most common form);
     * 这里要一块干净的屏，所以随后单独调 `tty_cls()`。
     * Here we want a clean screen, so `tty_cls()` is called separately right after. */
    tty_init(0, 0, 0);
    tty_cls();
    g_lang = ui_get_language();

    /* ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ── */
    /* ── 2. Title bar: bright yellow on blue, filling the whole first row ── */
    tty_color(14, 1);
    tty_goto(1, 1);
    tty_puts("                                                                                ");
    tty_goto(3, 1);
    tty_puts("WayCoder  tty_* demo (C)  --  color / cursor / box / int");

    tty_color(8, 0);
    tty_goto(3, 2);
    tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

    /* ── 3. ASCII 面板（61 列，安全）── */
    /* ── 3. ASCII panel (61 columns, safe) ── */
    tty_color(7, 0);
    tty_box(2, 3, 62, 12, 0);              /* style 0 = ASCII(`+ - |`) */

    /* 面板里的内容：每行一种前景色 —— 这就是"能设前景色"最直观的样子 */
    /* The panel contents: one foreground color per line -- the most direct look at "fg color works" */
    say(4, 4,  7,  0, g_lang == 0 ? "color  7  lightgray    普通正文" : "color  7  lightgray    body text");
    say(4, 5,  11, 0, g_lang == 0 ? "color 11  lightcyan    次要信息" : "color 11  lightcyan    secondary");
    say(4, 6,  10, 0, g_lang == 0 ? "color 10  lightgreen   ok / 成功" : "color 10  lightgreen   ok / success");
    say(4, 7,  14, 0, g_lang == 0 ? "color 14  yellow       状态栏高亮" : "color 14  yellow       status bar");
    say(4, 8,  12, 0, g_lang == 0 ? "color 12  lightred     错误 / 警告" : "color 12  lightred     error / warn");
    say(4, 9,  13, 0, g_lang == 0 ? "color 13  lightmagenta 强调" : "color 13  lightmagenta emphasis");

    /* 反白一行（"选中项"的长相）：黑字白底 */
    /* One reversed line (how a "selected item" looks): black text on white */
    say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

    /* ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）── */
    /* ── 4. UTF-8 single-line box (19 cells x 3 bytes = 57, from column 4 => 61 bytes, safe) ── */
    tty_color(11, 0);
    tty_box(4, 14, 22, 19, 1);             /* style 1 = ┌ ─ │ ┐ └ ┘ 单线框 */
    /* style 1 = ┌ ─ │ ┐ └ ┘ single-line box (UTF-8) */

    /* 框里的中文：每行重新定位，列计数从 0 起 */
    /* CJK text inside the box: each line re-positions, so the column counter restarts at 0 */
    say(6, 16, 14, 0, g_lang == 0 ? "中文也" : "CJK too");
    say(6, 17, 10, 0, g_lang == 0 ? "没问题" : "works");

    /* ── 5. 右侧说明（ASCII，列 26 起）── */
    /* ── 5. Notes on the right (ASCII, starting at column 26) ── */
    say(26, 14, 11, 0, "tty_box(..., style=1)");
    say(26, 15, 7,  0, "  = UTF-8 single-line box");
    say(26, 17, 11, 0, "tty_box(..., style=0)");
    say(26, 18, 7,  0, "  = ASCII box (used above)");

    /* ── 6. 16 色色带 —— 一条就能看出"亮色档"对不对 ── */
    /* ── 6. 16-color band -- one row is enough to tell whether the "bright" tier is right ── */
    for (i = 0; i < 16; i++) {
        tty_color(0, i);                   /* 黑字 + 第 i 号底色 */
        /* black text on background color number i */
        tty_goto(3 + i * 4, 21);
        tty_puts("    ");
    }
    tty_color(7, 0);
    tty_goto(3, 22);
    tty_puts("index 0..15: black blue green cyan red magenta brown gray / +8 = bright");

    /* ── 7. 数字与光标：`tty_put_int` + `tty_wherex/wherey` ── */
    /* ── 7. Numbers and cursor: `tty_put_int` + `tty_wherex/wherey` ── */
    tty_color(11, 0);
    tty_goto(3, 24);
    tty_puts("tty_put_int: ");
    tty_color(14, 0);
    tty_put_int(2026);
    tty_color(11, 0);
    tty_puts(" / ");
    tty_color(14, 0);
    tty_put_int(-42);
    tty_color(11, 0);
    tty_puts("   cursor=");
    tty_put_int(tty_wherex());
    tty_puts(",");
    tty_put_int(tty_wherey());
    tty_puts("   screen=");
    tty_put_int(tty_width());
    tty_puts("x");
    tty_put_int(tty_height());

    /* ── 8. 状态栏 + 收尾 ── */
    /* ── 8. Status bar + wrap-up ── */
    tty_color(0, 3);                       /* 黑字青底 */
    /* black text on cyan background */
    tty_goto(1, ROWS - 1);
    tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
    tty_color(8, 0);
    tty_goto(3, ROWS);
    tty_puts(g_lang == 0 ? "demo_tty (C) 结束 —— 画完即退出" : "demo_tty (C) done -- draws and exits");

    /* 复位颜色，别把终端留在某种底色上 */
    /* Reset the colors; do not leave the terminal on some background color */
    tty_color(7, 0);
    return 0;
}
