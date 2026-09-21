/* curses_demo.c —— `curses.h` 的验收程序（ncurses 常用子集）
 *
 * 跑法：命令行页输入  vml run examples/c/curses_demo.c
 *
 * ## 它验什么
 *
 * `curses` 与 `conio` 长得像但**语义相反**，所以这个示例刻意把三处差别都摆出来：
 *
 *   ① **坐标 0 起**：`move(0,0)` 是左上角（conio 是 `gotoxy(1,1)`）
 *   ② **`refresh()` 才上屏**：前面所有的 `move`/`addstr` 只改**缓冲**，
 *      不调 `refresh()` 屏幕上什么都没有 —— 这是 curses 程序的头号"空白"来源
 *   ③ **颜色是"颜色对"**：`init_pair(1, fg, bg)` 定义、`COLOR_PAIR(1)` 引用，
 *      而不是 conio 那种"当前前景/背景"两个全局状态
 *
 * ## 与 conio 的 `conio_screen.c` 对着看
 *
 * 那个示例画的是同一类界面（标题栏 + 框 + 菜单 + 色块），但用的是 DOS 那套
 * 立即输出模型。两个放一起看，差别一眼可见。
 */
#include <curses.h>

int main()
{
    int i;

    initscr();                  /* 清屏、建缓冲 —— 老程序都从这里开始 */
    start_color();

    /* 颜色对：号、前景、背景 */
    init_pair(1, COLOR_BLACK,  COLOR_CYAN);    /* 标题栏：青底黑字 */
    init_pair(2, COLOR_WHITE,  COLOR_BLUE);    /* 正文：蓝底白字 */
    init_pair(3, COLOR_BLACK,  COLOR_WHITE);   /* 选中项：白底黑字（反白） */
    init_pair(4, COLOR_YELLOW, COLOR_BLUE);    /* 状态行：蓝底黄字 */

    /* ── 标题栏：铺满第 0 行 ── */
    attrset(COLOR_PAIR(1) | A_BOLD);
    move(0, 0);
    for (i = 0; i < COLS; i++) addch(' ');
    mvaddstr(0, 2, "WayCoder   curses.h   demo   --   ncurses subset");

    /* ── 外框（第 2..7 行、第 10..27 列） ── */
    attrset(COLOR_PAIR(2));
    mvaddstr(2, 10, "+----------------+");
    for (i = 0; i < 4; i++) mvaddstr(3 + i, 10, "|                |");
    mvaddstr(7, 10, "+----------------+");

    /* ── 菜单项：第 2 项反白（curses 里就是换个颜色对） ── */
    mvaddstr(3, 12, "New");
    attrset(COLOR_PAIR(3));
    mvaddstr(4, 12, "Open");
    attrset(COLOR_PAIR(2));
    mvaddstr(5, 12, "Save");
    mvaddstr(6, 12, "Quit");

    /* ── 状态行 ── */
    attrset(COLOR_PAIR(4));
    mvaddstr(9, 10, "move(0,0) is the TOP-LEFT corner  (conio's gotoxy(1,1) is the same spot)");

    /* ── 8 色色块：每个颜色对一格，一眼看出颜色对有没有配对 ── */
    attrset(A_NORMAL);
    mvaddstr(11, 10, "8 colors:");
    for (i = 0; i < 8; i++) {
        init_pair((short)(10 + i), COLOR_BLACK, (short)i);
        attrset(COLOR_PAIR(10 + i));
        mvaddstr(12, 12 + i * 3, "   ");
    }

    /* ── 版本信息（走 printw 那条格式化路） ── */
    attrset(COLOR_PAIR(2));
    mvprintw(14, 10, "LINES=%d  COLS=%d  cursor=%d,%d",
             LINES, COLS, getcury(stdscr), getcurx(stdscr));

    /* ⚠ **这一句才是"上屏"** —— 前面全在改缓冲。
       不调它，跑完屏幕上什么都没有（curses 程序最经典的"空白"来源）。 */
    refresh();

    endwin();
    return 0;
}
