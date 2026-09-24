/* demo_tty.c —— **第 2 层：彩色命令行**（`tty_*` / `Lib/c/tty.h`）
 *
 * ## 这一层是干嘛的
 *
 * 「**在一个字符界面上做版面**」—— 清屏、把光标挪到第几行第几列、设前景/背景色、
 * 画个框。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
 *
 * ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
 *   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
 *   `tty_*` 是这套能力的**正式名字**，22 门语言写出来的是**同一串调用**；
 *   `Lib/c/conio.h` 那一套是**兼容层**，留给老源码。
 *
 * ## 它是字符库，不是图形库
 *
 * **只有字符，没有任何图形功能**。要画图请用别的层：
 *   · 传统固定分辨率图形 → BGI（`Lib/c/graphics.h`，见 `demo_bgi.c`）
 *   · 现代图元           → `ui_*`（`Lib/c/waycoder_ui.h`，见 `demo_ui.c`）
 *
 * ## 坐标与颜色
 *
 * · 坐标 **1 起算**（`(1,1)` 左上角）—— 与 `gotoxy` / `GotoXY` / `LOCATE` 一致。
 * · 颜色 **0–15 索引色**：`0 黑 1 蓝 2 绿 3 青 4 红 5 品红 6 棕 7 浅灰
 *   8 深灰 9 亮蓝 10 亮绿 11 亮青 12 亮红 13 亮洋红 14 黄 15 白`
 *
 * ## 它画到哪儿
 *
 * **主屏**（默认）= 命令行页那个 80×25 的字符网格。`tty_*` 只产生字节
 * （光标定位 `ESC[{行};{列}H`、颜色 SGR、字符、清屏 `ESC[2J`），走既有的渲染链，
 * **不弹窗**。副屏（`tty_alt_open`）才会弹一扇窗口当终端 —— 本 demo 不演示它，
 * 免得"画完就退出"的例程顺手弹一个窗出来。
 *
 * ## ⚠ 版面是被一条**已量到的库缺陷**逼成这样的（不是审美选择）
 *
 * `Lib/shared/src/conio.c` 的 `putch()` 把**字节**当**列**计数：每个字节 `conX++`、
 * 到 80 就折行并重发一条光标定位。而中文 / UTF-8 框线（`┌ ─ │`）一个字符是 **3 个字节**
 * ⇒ 只要**某一段连续输出跨过第 80 列**，折行那一下就会落在**一个字符的中间**，
 * 把一条 `ESC[…H` 插进这个字符的字节序列里。宿主的输出管线是**按字节重组 UTF-8** 的
 * （`VMLRuntime.Syscall.cs` 的 `TryFlushOneOutputUnit`），校验一旦失败就**粘性**地
 * 认定"这个程序写的是单字节老编码"（CP437）—— **此后整份输出全乱，不可恢复**。
 *
 * 最小复现（2026-09-24 实测，`Examples/c/demo_tty.c` 交付报告里也记了）：
 *
 *     tty_goto(10, 2);  for (i=0;i<20;i++) tty_puts("┌");   // 字节跨度 10..70 → 干净 ✓
 *     tty_goto(70, 2);  for (i=0;i<10;i++) tty_puts("┌");   // 字节跨度 70..100 → 第 4 个起全乱 ✗
 *
 * 不是 `tty_*` 的问题 —— 根在 `conio.c`（`tty_puts` 在主屏上就是转发给 `cputs`）。
 * 判据：把上面第二行的 `┌` 换成 `A`（1 字节 = 1 列）就完全正常。
 *
 * 所以本 demo 的规矩是：**每一段"定位之后连续输出"的字节跨度不越过第 80 列**，
 * 也就是 `起始列 + 3×字符数 ≤ 80`：
 *   · ASCII 行照常铺满（1 字节 = 1 列，正好是它的真实宽度）
 *   · 中文行都用 `tty_print_at` 重新定位（`tty_goto` 会把列计数清零）
 *   · UTF-8 框线只画**窄**的（19 格，从第 4 列起 ⇒ 61 字节）
 *
 * 跑法：命令行页输入  vml run examples/c/demo_tty.c
 */

#include <tty.h>

#define COLS 80
#define ROWS 25

/* 在第 (x,y) 处用指定前景/背景打一串（打完颜色恢复成 7/0） */
static void say(int x, int y, int fg, int bg, char *s)
{
    tty_print_at(x, y, s, fg, bg);
}

int main(void)
{
    int i;

    /* ── 1. 初始化 + 清屏 ──
     * `tty_init(0, 0, 0)` = 尺寸自适应、不清屏（最常用的那一句）；
     * 这里要一块干净的屏，所以随后单独调 `tty_cls()`。 */
    tty_init(0, 0, 0);
    tty_cls();

    /* ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ── */
    tty_color(14, 1);
    tty_goto(1, 1);
    tty_puts("                                                                                ");
    tty_goto(3, 1);
    tty_puts("WayCoder  tty_* demo (C)  --  color / cursor / box / int");

    tty_color(8, 0);
    tty_goto(3, 2);
    tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

    /* ── 3. ASCII 面板（61 列，安全）── */
    tty_color(7, 0);
    tty_box(2, 3, 62, 12, 0);              /* style 0 = ASCII(`+ - |`) */

    /* 面板里的内容：每行一种前景色 —— 这就是"能设前景色"最直观的样子 */
    say(4, 4,  7,  0, "color  7  lightgray    普通正文");
    say(4, 5,  11, 0, "color 11  lightcyan    次要信息");
    say(4, 6,  10, 0, "color 10  lightgreen   ok / 成功");
    say(4, 7,  14, 0, "color 14  yellow       状态栏高亮");
    say(4, 8,  12, 0, "color 12  lightred     错误 / 警告");
    say(4, 9,  13, 0, "color 13  lightmagenta 强调");

    /* 反白一行（"选中项"的长相）：黑字白底 */
    say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

    /* ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）── */
    tty_color(11, 0);
    tty_box(4, 14, 22, 19, 1);             /* style 1 = ┌ ─ │ ┐ └ ┘ 单线框 */

    /* 框里的中文：每行重新定位，列计数从 0 起 */
    say(6, 16, 14, 0, "中文也");
    say(6, 17, 10, 0, "没问题");

    /* ── 5. 右侧说明（ASCII，列 26 起）── */
    say(26, 14, 11, 0, "tty_box(..., style=1)");
    say(26, 15, 7,  0, "  = UTF-8 single-line box");
    say(26, 17, 11, 0, "tty_box(..., style=0)");
    say(26, 18, 7,  0, "  = ASCII box (used above)");

    /* ── 6. 16 色色带 —— 一条就能看出"亮色档"对不对 ── */
    for (i = 0; i < 16; i++) {
        tty_color(0, i);                   /* 黑字 + 第 i 号底色 */
        tty_goto(3 + i * 4, 21);
        tty_puts("    ");
    }
    tty_color(7, 0);
    tty_goto(3, 22);
    tty_puts("index 0..15: black blue green cyan red magenta brown gray / +8 = bright");

    /* ── 7. 数字与光标：`tty_put_int` + `tty_wherex/wherey` ── */
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
    tty_color(0, 3);                       /* 黑字青底 */
    tty_goto(1, ROWS - 1);
    tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
    tty_color(8, 0);
    tty_goto(3, ROWS);
    tty_puts("demo_tty (C) 结束 —— 画完即退出");

    /* 复位颜色，别把终端留在某种底色上 */
    tty_color(7, 0);
    return 0;
}
