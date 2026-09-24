/* demo_tty_alt.c —— **tty 副屏（弹窗）自检**
 *
 * 跑法（手机 App 的命令行页 / 桌面 vmlcli）：
 *     vml run examples/c/demo_tty_alt.c
 *
 * ## 它验的是什么
 *
 * 用户要的链路：**开副屏弹窗 → 副屏上显示东西 → 等用户输入 → 按返回键程序终止**。
 * 这条链上有四个环节，任何一环断了都表现为"点了没反应"，所以这份 demo 把每一环
 * 都做成**屏幕上看得见**的：
 *
 *   ① `tty_alt_open` 能不能开出那扇窗（失败会**退回主屏**打印一行说明，不是静默失败）；
 *   ② 副屏里能不能**显示东西**（框线 + 中英文 + 彩色 + 可变的计数器）；
 *   ③ `tty_wait` 能不能**收到输入**（每按一次键，`已按键: N` 加一 —— 数字在动就是收到了）；
 *   ④ `tty_closed()` 能不能**感知关窗**（按返回键 / 关掉那扇窗 ⇒ 循环退出 ⇒ 程序正常结束）。
 *
 * ## 为什么用 `tty_closed()` 而不是只等 `tty_wait()` 返回 -1
 *
 * 两件事都要有，缺一就会"退不出去"：宿主关窗时**既置标志又投 `WINDOWCLOSE` 消息**
 * （只投消息的话 `ui_win_closed()` 仍报 0，那套主流循环就出不来 —— 本仓有实测记录）。
 * 所以这里**循环条件读标志**、循环体里也把 `tty_wait()` 的 -1 当出口，两条都走。
 *
 * ## 已知约束（本程序刻意避开）
 *
 * `tty` 主屏那条路是转发给 `conio` 的，而 `conio` 目前**按字节当列**计数 ——
 * 一次"定位之后连续输出"的**字节跨度超过 80** 时，折行的定位转义会插进一个多字节
 * 字符中间，宿主的 UTF-8 重组校验失败后会**粘性**切成 CP437，之后整份输出全乱。
 * ⇒ 所以本程序**每段输出前都 `tty_goto`**，且每段都短（中文一行 ≤ 45 字节）。
 * 这是绕开，不是修好；真正的修法见 `ROADMAP.md` 零之二那节记的"坑"。
 */

#include <tty.h>

int main(void) {
    int rc;
    int n;
    int key;

    /* ① 开副屏：`0,0` = 自适应尺寸（按宿主可画区算行列，上限 80×25） */
    rc = tty_alt_open(0, 0, "TTY 副屏自检", 1);
    if (rc < 0) {
        /* 宿主不支持开窗（例如纯控制台环境）—— **说清楚**，别让人以为是程序卡住了 */
        tty_init(0, 0, 0);
        tty_color(12, 0);
        tty_puts("这台宿主打不开副屏窗口（tty_alt_open 返回 -1）。\n");
        tty_color(7, 0);
        tty_puts("主屏部分是好的；副屏需要能开窗的宿主（手机 App / 带绘图窗口的环境）。\n");
        return 1;
    }

    /* ② 副屏上显示东西 */
    tty_color(14, 1);                       /* 前景 14 黄 / 背景 1 蓝 */
    tty_goto(2, 1);
    tty_puts("tty 副屏（弹窗）自检");
    tty_box(1, 1, 46, 11, 1);               /* 单线框（UTF-8 框线） */

    tty_color(11, 0);
    tty_goto(3, 3);
    tty_puts("这一屏是在**副屏窗口**里，不是命令行页。");

    tty_color(7, 0);
    tty_goto(3, 5);
    tty_puts("按任意键 → 计数器加一（收得到输入）。");
    tty_goto(3, 6);
    tty_puts("按手机返回键 / 关掉这扇窗 → 程序结束。");

    tty_goto(3, 8);
    tty_puts("已按键: ");
    tty_color(10, 0);
    tty_put_int(0);
    tty_color(7, 0);

    tty_goto(3, 10);
    tty_color(8, 0);
    tty_puts("(窗口尺寸：");
    tty_put_int(tty_width());
    tty_puts(" x ");
    tty_put_int(tty_height());
    tty_puts(")");
    tty_color(7, 0);

    tty_refresh();                          /* ⚠ 副屏必须 refresh，否则屏上不会更新 */

    /* ③④ 等输入 / 感知关窗 */
    n = 0;
    while (tty_closed() == 0) {
        key = tty_wait();                   /* 阻塞等一个按键；窗口关了返回 -1 */
        if (key < 0) break;                 /* 出口之一：收到"窗口关了" */

        n = n + 1;
        tty_goto(11, 8);
        tty_color(10, 0);
        tty_put_int(n);
        tty_color(7, 0);
        tty_refresh();
    }

    tty_alt_close();                        /* 关副屏、回到主屏 */
    return 0;
}
