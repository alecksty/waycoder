/* old_tty_curses.c —— ncurses 风格（彩色的 tty）
 * old_tty_curses.c -- ncurses style (color tty)
 *
 * 类别：tty
 * Category: tty
 * 兼容面：**curses.h 那一套**（`initscr` / `move` / `addstr` / `printw` / `mvprintw`
 * Compatibility: the whole **curses.h** set (`initscr` / `move` / `addstr` / `printw` / `mvprintw`
 *         / `attrset` / `clear` / `refresh` / `endwin`）—— Unix 全屏程序的标准接口，
 *         / `attrset` / `clear` / `refresh` / `endwin`) -- the standard interface of Unix full-screen programs,
 *         也是"老程序不改一行就能跑"里最值钱的一类（vim/top/mc 都是它）
 *         and the most valuable class of all for "old programs that run without changing a single line" (vim/top/mc are all of this kind)
 * 出处：自写，仿 ncurses 教程里那段经典的"居中标题 + 循环写行"。
 * Origin: self-written, imitating the classic "centered title + write lines in a loop" passage from ncurses tutorials.
 */
#include <curses.h>
#include <waycoder_ui.h>     /* 只为 ui_get_language() —— 老程序不该猜系统语言 */
                            /* only for ui_get_language() -- an old program must not guess the system language */

int main(void)
{
    int i;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();

    initscr();              /* 进 curses 模式（切备用屏）*/
                            /* Enter curses mode (switch to the alternate screen) */

    clear();
    mvprintw(1, 10, lang == 0 ? "ncurses 风格演示" : "ncurses style demo");

    /* 逐行写：move + addstr（老程序最常用的两步） */
    /* Writing line by line: move + addstr (the two steps old programs used most) */
    for (i = 0; i < 5; i++) {
        move(3 + i, 6);
        printw(lang == 0 ? "第 %d 行：addstr / printw 混用" : "line %d: addstr / printw mixed", i + 1);
    }

    /* 属性：加粗 / 反白 */
    /* Attributes: bold / reverse video */
    attron(1);              /* A_BOLD 之类的位常量，本平台按位直传 */
                            /* Bit constants like A_BOLD; this platform passes the bits straight through */
    move(9, 6);
    addstr(lang == 0 ? "加粗一行" : "a bold line");
    attroff(1);

    move(11, 6);
    addstr(lang == 0 ? "反白一行" : "a reverse-video line");

    move(13, 6);
    printw(lang == 0 ? "屏幕尺寸变化不该让 curses 崩 —— 这一行写到 (13,6)"
                     : "a screen resize must not crash curses -- this line is written at (13,6)");

    refresh();              /* 一次性刷到屏幕 */
                            /* Flush everything to the screen at once */
    endwin();               /* 退出 curses 模式 */
                            /* Leave curses mode */

    return 0;
}
