/* curses.h —— ncurses 的**常用子集**（那个 `#include <curses.h>` 的世界）
 *
 * ## 为什么做它、排第几
 *
 * 按"老程序里出现得多不多"排（与 `conio.h`/`dos.h` 同一条规矩）：
 * `curses.h` **43,264** + `ncurses.h` **40,448** ⇒ 合计 **83,712** 个 C 文件，
 * 是 conio 之后的**最大一块**。而它同时是**「经典老程序」那条路的门票** ——
 * `tty-clock` / `moon-buggy` / `asciiquarium` / `nethack` 这些全是 curses 程序，
 * 没有这个头它们连编译都过不去。
 *
 * ## ⚠ 与 `conio.h` 的三处**语义相反**（一起用过的代码最容易在这儿出错）
 *
 * | | `conio.h`（DOS） | `curses.h`（本头） |
 * |---|---|---|
 * | 坐标原点 | **1 起** —— `gotoxy(1,1)` 是左上角 | **0 起** —— `move(0,0)` 是左上角 |
 * | 何时上屏 | **写完立即**输出（DOS 无缓冲） | **`refresh()` 才**输出（有屏幕缓冲） |
 * | 光标 | 有概念（`wherex/wherey`） | `move()` 只改**缓冲里的**光标，不影响真实终端 |
 *
 * 差一格、或少调一次 `refresh()`，症状都是"画面对不上 / 一片空白"，**而且不报错**。
 *
 * ## 只做**单窗口 `stdscr`** —— 这是有意的取舍
 *
 * 全功能 ncurses 的 `WINDOW` 是可以 `newwin()` 出任意多个、还能互相 `overlay` 的。
 * 但"老程序里实际用到的"绝大多数只有 `stdscr` 一个窗口（`initscr` 之后再没 `newwin`）。
 * 所以这里 `stdscr` 是一个**真实存在的对象**，`newwin()` 返回它自己（而不是 NULL）——
 * 让那些"顺手 `newwin` 一下"的程序**能跑起来**，虽然多窗口语义是退化的。
 * 真遇到非要多窗口不可的程序，再按需扩。
 *
 * ## 缓冲与 `refresh()`
 *
 * 屏幕影子照 `conio.c` 的做法：两个普通数组（字符 + 属性），**不做任何"把指针当整数
 * 算偏移"的读写**（ROADMAP 第零节的硬规矩）。`refresh()` 把整屏重发一遍 ——
 * 简单、正确；**动画程序（每秒刷一次那种）够用**，要更快就得改成"只重发变化的行"。
 *
 * ## 不做（写在明处）
 *
 * · `newwin`/`subwin`/`derwin` 的多窗口语义（见上，返回 `stdscr`）
 * · `pad` / `panel` / `menu` / `form`（那是另外几个库）
 * · 鼠标、`resize` 处理、`use_default_colors`、256 色（只做 8 色 + 加亮）
 * · `wgetch` 的超时与非阻塞细节（`nodelay` 只记状态，本平台本来就是"按行"输入）
 */
#ifndef _CURSES_H
#define _CURSES_H

#param lib("curses")

/* ⚠ 真 ncurses 会把 stdio.h 带出来 —— 老程序（如 tty-clock）就靠这一点
   拿到 `stderr`，自己**并不** include <stdio.h>。这里照做。 */
#include <stdio.h>

/* ── 尺寸（与 conio 同为 80×25；curses 程序常拿它做布局） ── */
#define LINES 25
#define COLS  80

/* ── 返回值 ── */
#define OK   0
#define ERR (-1)

/* ── 属性位（`attron`/`attrset` 用；低位是"修饰"，高位放颜色对号） ── */
#define A_NORMAL     0
#define A_BOLD       1
#define A_DIM        2
#define A_UNDERLINE  4
#define A_REVERSE    8
#define A_BLINK      16
#define A_STANDOUT   A_REVERSE

/* ── 8 色（curses 的颜色号就是 ANSI 的 0-7 顺序：黑红绿黄蓝品青白） ── */
#define COLOR_BLACK   0
#define COLOR_RED     1
#define COLOR_GREEN   2
#define COLOR_YELLOW  3
#define COLOR_BLUE    4
#define COLOR_MAGENTA 5
#define COLOR_CYAN    6
#define COLOR_WHITE   7

/* 颜色对号占属性的高 8 位（照 ncurses 的老惯例），低 8 位留给 A_* 修饰 */
#define COLOR_PAIR(n)  (((n) & 0xFF) << 8)
#define PAIR_NUMBER(a) (((a) >> 8) & 0xFF)
#define A_COLOR        (0xFF << 8)

/* ── 窗口对象 ──
   ⚠ 结构体的**字段布局必须与 `Lib/shared/src/curses.c` 里那份逐字对齐**
   （实现文件照 `conio.c` 的惯例不 include 本头）。改这里就得改那里。 */
typedef struct {
    int rows;
    int cols;
    int cy;        /* 缓冲光标（0 起） */
    int cx;
    int attr;      /* 当前属性（attron/attroff 累加的结果） */
    int nodelay;   /* 只记状态：本平台输入本来就是"按行"的 */
    int keypad;
    int scr;       /* 保留：屏幕号（本实现恒 0） */
} WINDOW;

extern WINDOW *stdscr;

/* ── 生命周期 ── */
WINDOW *initscr(void);          /* 建屏、清屏、返回 stdscr（老程序都从这里开始） */
int endwin(void);               /* 收尾（把光标放回左下、恢复属性） */
WINDOW *newwin(int nlines, int ncols, int begin_y, int begin_x);  /* ⚠ 返回 stdscr，见文件头 */

/* ── 上屏 ── */
int refresh(void);              /* 把缓冲**整屏**发给终端 —— 少调它画面就是空白 */
int wrefresh(WINDOW *w);

/* ── 光标（**0 起**） ── */
int move(int y, int x);
int wmove(WINDOW *w, int y, int x);
int getcury(WINDOW *w);
int getcurx(WINDOW *w);

/* ── 输出 ── */
int addch(int ch);
int addstr(const char *s);
int mvaddstr(int y, int x, const char *s);
int mvaddch(int y, int x, int ch);
int printw(const char *fmt, ...);
int mvprintw(int y, int x, const char *fmt, ...);

/* ── 擦除 ── */
int clear(void);                /* 清空**缓冲**（要 refresh 才看得见） */
int erase(void);
int clrtoeol(void);

/* ── 属性与颜色 ── */
int attron(int attrs);
int attroff(int attrs);
int attrset(int attrs);
int start_color(void);
int init_pair(short pair, short f, short b);
int has_colors(void);           /* 恒 1 */

/* ── 输入 ── */
int getch(void);                /* = conio 的 getch（同一个底层） */
int cbreak(void);
int nocbreak(void);
int noecho(void);
int echo(void);
int keypad(WINDOW *w, int bf);
int nodelay(WINDOW *w, int bf);
int curs_set(int visibility);   /* 0/1/2；本平台只有"显示/隐藏"两档 */

/* ── 特殊键（`getch()` 在 keypad 模式下返回这些值；本平台拿不到方向键，
      但常量得有 —— 老程序拿它们做 switch 分支）── */
#define KEY_DOWN   258
#define KEY_UP     259
#define KEY_LEFT   260
#define KEY_RIGHT  261
#define KEY_HOME   262
#define KEY_BACKSPACE 263
#define KEY_F(n)   (264 + (n))
#define KEY_DC     330
#define KEY_IC     331
#define KEY_NPAGE  338
#define KEY_PPAGE  339
#define KEY_ENTER  343
#define KEY_RESIZE 410
#define KEY_MOUSE  409

#endif /* _CURSES_H */
