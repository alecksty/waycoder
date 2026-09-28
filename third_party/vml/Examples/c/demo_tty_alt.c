/* demo_tty_alt.c —— **tty 副屏（弹窗）自检**
 * demo_tty_alt.c -- **self-check for the tty alternate screen (popup window)**
 *
 * 跑法（手机 App 的命令行页 / 桌面 vmlcli）：
 * How to run (mobile App command-line page / desktop vmlcli):
 *     vml run examples/c/demo_tty_alt.c
 *
 * ## 它验的是什么
 * ## What it verifies
 *
 * 用户要的链路：**开副屏弹窗 → 副屏上显示东西 → 等用户输入 → 按返回键程序终止**。
 * The chain the user asked for: **open the alternate-screen popup -> show things on it -> wait for user input -> pressing Back ends the program**.
 * 这条链上有四个环节，任何一环断了都表现为"点了没反应"，所以这份 demo 把每一环
 * There are four links in this chain, and if any one breaks the symptom is "nothing happens when I tap", so this demo makes every link
 * 都做成**屏幕上看得见**的：
 * **visible on screen**:
 *
 *   ① `tty_alt_open` 能不能开出那扇窗（失败会**退回主屏**打印一行说明，不是静默失败）；
 *   1) can `tty_alt_open` open that window (on failure it **falls back to the main screen** and prints a note, not a silent failure);
 *   ② 副屏里能不能**显示东西**（框线 + 中英文 + 彩色 + 可变的计数器）；
 *   2) can the alternate screen **display things** (box lines + Chinese/English + color + a changing counter);
 *   ③ `tty_wait` 能不能**收到输入**（每按一次键，`已按键: N` 加一 —— 数字在动就是收到了）；
 *   3) can `tty_wait` **receive input** (each key press adds one to the "keys pressed: N" counter -- if the number moves, input arrived);
 *   ④ `tty_closed()` 能不能**感知关窗**（按返回键 / 关掉那扇窗 ⇒ 循环退出 ⇒ 程序正常结束）。
 *   4) can `tty_closed()` **notice the window closing** (press Back / close that window => the loop exits => the program ends normally).
 *
 * ## 为什么用 `tty_closed()` 而不是只等 `tty_wait()` 返回 -1
 * ## Why use `tty_closed()` instead of only waiting for `tty_wait()` to return -1
 *
 * 两件事都要有，缺一就会"退不出去"：宿主关窗时**既置标志又投 `WINDOWCLOSE` 消息**
 * Both are needed; miss one and the program "cannot get out": when the host closes the window it **sets the flag and posts a `WINDOWCLOSE` message**
 * （只投消息的话 `ui_win_closed()` 仍报 0，那套主流循环就出不来 —— 本仓有实测记录）。
 * (posting only the message leaves `ui_win_closed()` reporting 0, and the usual main loop cannot exit -- this repo has a measured record).
 * 所以这里**循环条件读标志**、循环体里也把 `tty_wait()` 的 -1 当出口，两条都走。
 * So here the **loop condition reads the flag** and the body also treats `tty_wait()`'s -1 as an exit; both paths are taken.
 *
 * ## 已知约束（本程序刻意避开）
 * ## Known constraints (deliberately avoided by this program)
 *
 * `tty` 主屏那条路是转发给 `conio` 的，而 `conio` 目前**按字节当列**计数 ——
 * The `tty` main-screen path forwards to `conio`, and `conio` currently counts **bytes as columns** --
 * 一次"定位之后连续输出"的**字节跨度超过 80** 时，折行的定位转义会插进一个多字节
 * when one "goto then continuous output" run spans **more than 80 bytes**, the wrap-around goto escape lands inside a multi-byte
 * 字符中间，宿主的 UTF-8 重组校验失败后会**粘性**切成 CP437，之后整份输出全乱。
 * character; the host's UTF-8 reassembly check then fails and it **stickily** switches to CP437, garbling all output after that.
 * ⇒ 所以本程序**每段输出前都 `tty_goto`**，且每段都短（中文一行 ≤ 45 字节）。
 * => so this program does a `tty_goto` **before every output run** and keeps each run short (a Chinese line is <= 45 bytes).
 * 这是绕开，不是修好；真正的修法见 `ROADMAP.md` 零之二那节记的"坑"。
 * This is a workaround, not a fix; the real fix is noted under the "pitfalls" in the "zero point two" section of `ROADMAP.md`.
 */

#include <tty.h>
#include <waycoder_ui.h>       /* 只为 ui_get_language()（界面语言，见下面的 lang）*/
/* only for ui_get_language() (the UI language, see lang below) */

int main(void) {
    int rc;
    int n;
    int key;
    int lang;      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                   /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    lang = ui_get_language();

    /* ① 开副屏：`0,0` = 自适应尺寸（按宿主可画区算行列，上限 80×25）
     * 1) Open the alternate screen: `0,0` = auto size (rows/cols derived from the host's drawable area, capped at 80x25) */
    rc = tty_alt_open(0, 0, lang == 0 ? "TTY 副屏自检" : "TTY alternate screen self-check", 1);
    if (rc < 0) {
        /* 宿主不支持开窗（例如纯控制台环境）—— **说清楚**，别让人以为是程序卡住了
         * The host cannot open a window (a plain console, for example) -- **say so clearly**, do not let people think the program hung */
        tty_init(0, 0, 0);
        tty_color(12, 0);
        tty_puts(lang == 0 ? "这台宿主打不开副屏窗口（tty_alt_open 返回 -1）。\n" : "This host cannot open an alternate screen (tty_alt_open returned -1).\n");
        tty_color(7, 0);
        tty_puts(lang == 0 ? "主屏部分是好的；副屏需要能开窗的宿主（手机 App / 带绘图窗口的环境）。\n" : "Main screen OK; alternate screen needs a window-capable host (mobile App).\n");
        return 1;
    }

    /* ② 副屏上显示东西
     * 2) Show things on the alternate screen */
    tty_color(14, 1);                       /* 前景 14 黄 / 背景 1 蓝
                                             * foreground 14 yellow / background 1 blue */
    tty_goto(2, 1);
    tty_puts(lang == 0 ? "tty 副屏（弹窗）自检" : "tty alternate screen (popup) self-check");
    tty_box(1, 1, 46, 11, 1);               /* 单线框（UTF-8 框线）
                                             * single-line box (UTF-8 box drawing) */

    tty_color(11, 0);
    tty_goto(3, 3);
    tty_puts(lang == 0 ? "这一屏是在**副屏窗口**里，不是命令行页。" : "This screen is the **alternate-screen window**, not the command line.");

    tty_color(7, 0);
    tty_goto(3, 5);
    tty_puts(lang == 0 ? "按任意键 → 计数器加一（收得到输入）。" : "Press any key -> counter adds one (input arrives).");
    tty_goto(3, 6);
    tty_puts(lang == 0 ? "按手机返回键 / 关掉这扇窗 → 程序结束。" : "Phone Back key / close this window -> program ends.");

    tty_goto(3, 8);
    tty_puts(lang == 0 ? "已按键: " : "keys: ");
    tty_color(10, 0);
    tty_put_int(0);
    tty_color(7, 0);

    tty_goto(3, 10);
    tty_color(8, 0);
    tty_puts(lang == 0 ? "(窗口尺寸：" : "(window size: ");
    tty_put_int(tty_width());
    tty_puts(" x ");
    tty_put_int(tty_height());
    tty_puts(")");
    tty_color(7, 0);

    tty_refresh();                          /* ⚠ 副屏必须 refresh，否则屏上不会更新
                                             * WARNING: the alternate screen must be refreshed, otherwise nothing updates on it */

    /* ③④ 等输入 / 感知关窗
     * 3/4) Wait for input / notice the window closing */
    n = 0;
    while (tty_closed() == 0) {
        key = tty_wait();                   /* 阻塞等一个按键；窗口关了返回 -1
                                             * blocks for one key press; returns -1 once the window is closed */
        if (key < 0) break;                 /* 出口之一：收到"窗口关了"
                                             * one of the exits: we got the "window closed" signal */

        n = n + 1;
        tty_goto(11, 8);
        tty_color(10, 0);
        tty_put_int(n);
        tty_color(7, 0);
        tty_refresh();
    }

    tty_alt_close();                        /* 关副屏、回到主屏
                                             * close the alternate screen and return to the main screen */
    return 0;
}
