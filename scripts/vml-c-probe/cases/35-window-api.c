// **窗口版 `w*`/`mvw*` 那一族：实现一直都有，头文件里却一条声明都没有**
// （`tty-clock` 画面全空的第一个根因）
//
// ## 症状
//
// `tty-clock`（689 行）通篇只用 `mvwaddstr` / `mvwprintw` / `mvwaddch` / `werase` /
// `box` / `wattron` 这一族**窗口版**函数。实测修前：整屏 **4000 个空格**、一个字符都没有。
//
// ## 根因：**没声明的函数，前端编不出正确的调用**
//
// 实测（grep `Lib/`）：这一族的**实现全都在** `shared/src/curses.c` 里（`newwin()` 返回
// `stdscr`，所以它们全部转发到不带窗口的那一族），而 `Lib/c/curses.h` 里
// **一条声明都没有**。后果**分两种，第二种才是致命的**：
//
//   · 非变参那几个（`mvwaddstr`/`mvwaddch`/`box`/`werase`…）：**照样能画**；
//   · **变参那两个（`wprintw` / `mvwprintw`）把参数传错** ⇒ 程序在 `refresh()`
//     **之前**就崩掉 ⇒ **屏幕上什么都没有**。
//
// 屏上什么都没有，看起来就像"这个程序不兼容"—— 而真正的毛病只是"少了个声明"。
//
// ## 与 `usleep` 那条的区别（为什么这条更隐蔽）
//
//   · `usleep`：**没实现** ⇒ 编译期报「未定义的函数 'usleep'」，一眼看得到；
//   · 这一条：**实现了、没声明** ⇒ **不报错**，只是画不出来。
//
// ⇒ 修法是把这一族的原型写进 `Lib/c/curses.h`（见那里"⚠⚠ 必须有原型"那段）。
//
// ## 判据为什么是"走到哪一步"而不是"屏上有什么"
//
// 修前的表现是**程序在 `mvwprintw` 之后死掉**（`refresh()` 根本没执行 ⇒ 一个字都上不了屏），
// 所以"能不能走到最后"就是判据；屏上内容（框线 + 三行字）另**人工看过**：
// 修后 probe 里 `A-DECLARED` / `B-WINAPI` / `C-42` 三行与边框都出现了。
//
// ⚠ 每个标记前先打一个换行：`initscr`/`refresh` 会把 ANSI 序列怼在行首，
//   不换行的话标记行**不以 `M` 开头**，判据脚本的 `^[A-Za-z0-9+-]+=` 就抓不到它。
// STDIN:
// EXPECT: M1=1|M2=1|M3=1|M4=1|M5=1|M6=1|M7=1
#include <curses.h>

int main()
{
    printf("\nM1=1\n");
    initscr();
    printf("\nM2=1\n");
    mvaddstr(2, 3, "A-DECLARED");            /* 头文件里**有**声明（对照组） */
    mvwaddstr(stdscr, 4, 3, "B-WINAPI");     /* 有实现、没声明 */
    printf("\nM3=1\n");
    mvwprintw(stdscr, 6, 3, "C-%d", 42);     /* 有实现、没声明、**变参** ← 修前死在这儿 */
    printf("\nM4=1\n");
    box(stdscr, 0, 0);                       /* 边框 */
    printf("\nM5=1\n");
    refresh();
    printf("\nM6=1\n");
    endwin();
    printf("\nM7=1\n");
    return 0;
}
