/* curses_demo.c —— `curses.h` 的验收程序（ncurses 常用子集）
 * curses_demo.c — the acceptance program for `curses.h` (the commonly used ncurses subset)
 *
 * 跑法：命令行页输入  vml run examples/c/curses_demo.c
 * How to run: type  vml run examples/c/curses_demo.c  on the command-line page
 *
 * ## 它验什么
 * ## What it verifies
 *
 * `curses` 与 `conio` 长得像但**语义相反**，所以这个示例刻意把三处差别都摆出来：
 * `curses` looks like `conio` but has the **opposite semantics**, so this demo deliberately shows three differences:
 *
 *   ① **坐标 0 起**：`move(0,0)` 是左上角（conio 是 `gotoxy(1,1)`）
 *   ① **coordinates start at 0**: `move(0,0)` is the top-left corner (conio's `gotoxy(1,1)`)
 *   ② **`refresh()` 才上屏**：前面所有的 `move`/`addstr` 只改**缓冲**，
 *   ② **only `refresh()` puts it on screen**: all the earlier `move`/`addstr` calls only edit the **buffer**,
 *      不调 `refresh()` 屏幕上什么都没有 —— 这是 curses 程序的头号"空白"来源
 *      without it the screen shows nothing — this is the number one source of a "blank" curses program
 *   ③ **颜色是"颜色对"**：`init_pair(1, fg, bg)` 定义、`COLOR_PAIR(1)` 引用，
 *   ③ **a colour is a "colour pair"**: defined by `init_pair(1, fg, bg)` and referenced by `COLOR_PAIR(1)`,
 *      而不是 conio 那种"当前前景/背景"两个全局状态
 *      instead of conio's two global "current foreground/background" states
 *
 * ## 与 conio 的 `conio_screen.c` 对着看
 * ## Read it side by side with `conio_screen.c` for conio
 *
 * 那个示例画的是同一类界面（标题栏 + 框 + 菜单 + 色块），但用的是 DOS 那套
 * That demo draws the same kind of screen (title bar + box + menu + colour patches), but with the DOS-style
 * 立即输出模型。两个放一起看，差别一眼可见。
 * immediate-output model. Put the two side by side and the difference is visible at a glance.
 */
#include <curses.h>

int main()
{
    int i;

    initscr();                  /* 清屏、建缓冲 —— 老程序都从这里开始 */
    /* clears the screen and builds the buffer — old programs all start here */
    start_color();

    /* 颜色对：号、前景、背景 */
    /* colour pairs: number, foreground, background */
    init_pair(1, COLOR_BLACK,  COLOR_CYAN);    /* 标题栏：青底黑字 */
    /* title bar: black text on cyan */
    init_pair(2, COLOR_WHITE,  COLOR_BLUE);    /* 正文：蓝底白字 */
    /* body: white text on blue */
    init_pair(3, COLOR_BLACK,  COLOR_WHITE);   /* 选中项：白底黑字（反白） */
    /* selected item: black text on white (reverse video) */
    init_pair(4, COLOR_YELLOW, COLOR_BLUE);    /* 状态行：蓝底黄字 */
    /* status line: yellow text on blue */

    /* ── 标题栏：铺满第 0 行 ── */
    /* ── title bar: fill row 0 ── */
    attrset(COLOR_PAIR(1) | A_BOLD);
    move(0, 0);
    for (i = 0; i < COLS; i++) addch(' ');
    mvaddstr(0, 2, "WayCoder   curses.h   demo   --   ncurses subset");

    /* ── 外框（第 2..7 行、第 10..27 列） ── */
    /* ── outer box (rows 2..7, columns 10..27) ── */
    attrset(COLOR_PAIR(2));
    mvaddstr(2, 10, "+----------------+");
    for (i = 0; i < 4; i++) mvaddstr(3 + i, 10, "|                |");
    mvaddstr(7, 10, "+----------------+");

    /* ── 菜单项：第 2 项反白（curses 里就是换个颜色对） ── */
    /* ── menu items: the 2nd one is reversed (in curses that is just switching colour pair) ── */
    mvaddstr(3, 12, "New");
    attrset(COLOR_PAIR(3));
    mvaddstr(4, 12, "Open");
    attrset(COLOR_PAIR(2));
    mvaddstr(5, 12, "Save");
    mvaddstr(6, 12, "Quit");

    /* ── 状态行 ── */
    /* ── status line ── */
    attrset(COLOR_PAIR(4));
    mvaddstr(9, 10, "move(0,0) is the TOP-LEFT corner  (conio's gotoxy(1,1) is the same spot)");

    /* ── 8 色色块：每个颜色对一格，一眼看出颜色对有没有配对 ── */
    /* ── 8 colour patches: one cell per colour pair, so a mis-paired colour is obvious at a glance ── */
    attrset(A_NORMAL);
    mvaddstr(11, 10, "8 colors:");
    for (i = 0; i < 8; i++) {
        init_pair((short)(10 + i), COLOR_BLACK, (short)i);
        attrset(COLOR_PAIR(10 + i));
        mvaddstr(12, 12 + i * 3, "   ");
    }

    /* ── 版本信息（走 printw 那条格式化路） ── */
    /* ── version info (going through the printw formatting path) ── */
    attrset(COLOR_PAIR(2));
    mvprintw(14, 10, "LINES=%d  COLS=%d  cursor=%d,%d",
             LINES, COLS, getcury(stdscr), getcurx(stdscr));

    /* ⚠ **这一句才是"上屏"** —— 前面全在改缓冲。
       ⚠ **This one call is what actually puts it on screen** — everything before only edits the buffer.
       不调它，跑完屏幕上什么都没有（curses 程序最经典的"空白"来源）。
       Without it the screen shows nothing when the program finishes (the most classic source of a blank curses program). */
    refresh();

    endwin();
    return 0;
}
