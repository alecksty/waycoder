/* old_tty_curses.c —— ncurses 风格（彩色的 tty）
 *
 * 类别：tty
 * 兼容面：**curses.h 那一套**（`initscr` / `move` / `addstr` / `printw` / `mvprintw`
 *         / `attrset` / `clear` / `refresh` / `endwin`）—— Unix 全屏程序的标准接口，
 *         也是"老程序不改一行就能跑"里最值钱的一类（vim/top/mc 都是它）
 * 出处：自写，仿 ncurses 教程里那段经典的"居中标题 + 循环写行"。
 */
#include <curses.h>

int main(void)
{
    int i;

    initscr();              /* 进 curses 模式（切备用屏）*/

    clear();
    mvprintw(1, 10, "ncurses 风格演示");

    /* 逐行写：move + addstr（老程序最常用的两步） */
    for (i = 0; i < 5; i++) {
        move(3 + i, 6);
        printw("第 %d 行：addstr / printw 混用", i + 1);
    }

    /* 属性：加粗 / 反白 */
    attron(1);              /* A_BOLD 之类的位常量，本平台按位直传 */
    move(9, 6);
    addstr("加粗一行");
    attroff(1);

    move(11, 6);
    addstr("反白一行");

    move(13, 6);
    printw("屏幕尺寸变化不该让 curses 崩 —— 这一行写到 (13,6)");

    refresh();              /* 一次性刷到屏幕 */
    endwin();               /* 退出 curses 模式 */

    return 0;
}
