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

/* ⚠ 下面这几条**必须写在这个头文件里** —— `#param lib("库")` 的规矩就是
   「**用到了这个头文件，就连这个库**；不 include 就不连」。写在别处都不对：
     · 写在生成物 `Lib/shared/curses.vml` 上 ⇒ 下次 `GenLib` 一跑就**静默抹掉**
       （2026-09-22 实测：那次全量重生成把 `.linked "conio.vml"` 抹掉后，
        症状是编译 `20-curses-api.c` 报「未找到标签: kbhit」，而根因隔了几周）；
     · 写在实现体 `Lib/shared/src/curses.c` 里 ⇒ 语义也不对，那是**实现依赖**，
       不该由"C 程序 include 了什么头"来决定。
   `.linked` 列表**自动去重**，所以与 `conio.h` 里那条重复完全无害。

   各条的来由（**别删**）：
     conio  —— `getch`/`kbhit`/`gotoxy`/`cprintf` 的实现都在 conio.c 那一份
               （同名函数只能有一份定义，curses 这边绝不自己再写一个）
     printf —— `vsnprintf`/`format_arg_count`，`printw` 家族要
     util   —— `delay`，`napms` 要
     math   —— 实现体用到

   ⚠ `#param` 是**按行**解析的：这一行后面**不能挂跨行的块注释**
     （续行不再算注释，全角括号会让词法器报「未知字符」）。所以注释单独成块。 */
#param lib("conio")
#param lib("printf")
#param lib("util")
#param lib("math")


/* ⚠ 真 ncurses 会把 stdio.h 带出来 —— 老程序（如 tty-clock）就靠这一点
   拿到 `stderr`，自己**并不** include <stdio.h>。这里照做。 */
#include <stdio.h>

/* ⚠ 同理带出 `stdbool.h`：真 ncurses 在 C99 下就是这么做的，于是老程序
   直接用**小写** `true`/`false` 而不自己 include（实测 cmatrix 第 726/741
   行就是 `matrix[i][j].is_head = false;`，而它的 include 表里没有
   stdbool.h）。少了这一句报的是"未声明的变量 'false'"。 */
#include <stdbool.h>

/* `wchar_t`（`typedef unsigned int`，4 字节）—— `addwstr` 的形参要用。 */
#include <stddef.h>

/* ── 尺寸（与 conio 同为 80×25；curses 程序常拿它做布局） ── */
#define LINES 25
#define COLS  80

/* ── 返回值 ── */
#define OK   0
#define ERR (-1)

/* ── 布尔 ──
   ⚠ 真 ncurses 的 `curses.h` 里就有 TRUE/FALSE，老程序**默认它有**
   （`leaveok(stdscr, TRUE)` 这种写法遍地都是，而它们并不自己 include
   stdbool.h）—— 实测 cmatrix 第 470 行就是这么写的，报
   "未声明的变量 'TRUE'"。少这两个宏，凡是拿它们当参数的程序全挂。 */
#define TRUE  1
#define FALSE 0

/* ── 属性位（`attron`/`attrset` 用；低位是"修饰"，高位放颜色对号） ── */
#define A_NORMAL     0
#define A_BOLD       1
#define A_DIM        2
#define A_UNDERLINE  4
#define A_REVERSE    8
#define A_BLINK      16
#define A_STANDOUT   A_REVERSE

/* `A_ALTCHARSET`：**替代字符集**（老程序拿它把 `-` `|` `+` 画成线框字符）。
   ⚠ 本平台**没有这个概念**（终端那边看到什么就是什么），所以这里只给一个
   **空闲属性位**让程序编得过 —— `sc_sgr` 不认识它就静默忽略，字符按原样画。
   这正是我们要的退化：cmatrix 的方块在真 ncurses 下是 `ACS_*` 线框字符、
   换成本平台的普通字符后仍然是个能看的矩阵。
   ⚠ 值取 32 而不是真 ncurses 的 `0x4000` —— 本实现的属性布局是
   「低 8 位修饰位 / 高 8 位颜色对号」（见 `COLOR_PAIR`），往高位放会撞上颜色。 */
#define A_ALTCHARSET 32
#define A_CHARTEXT   64
#define A_ATTRIBUTES (A_BOLD|A_DIM|A_UNDERLINE|A_REVERSE|A_BLINK|A_ALTCHARSET|A_CHARTEXT)

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

/* `SCREEN` —— **不透明类型**（真 ncurses 里它是「一整屏」的句柄，`newterm()` 返回它）。
   本实现单窗口退化：它只是个占位类型。⚠ **但它必须存在** —— 老程序（`tty-clock`）
   把它当结构体字段的类型用（`SCREEN *ttyscr;`），而**未定义的类型会让整个结构体
   后面的字段偏移全算错**：实测 `option.color` 的地址算成结构体首地址、
   `nsdelay = 0` 写到别的字段上 ⇒ `running` 读成 0 ⇒ 主循环一次都不进 ⇒ 画面全空。 */
typedef void SCREEN;

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
/* `mvcur`：**立即**挪**真实终端**的光标（`move()` 只改缓冲里的，要 `refresh()` 才上屏）。
   ⚠ 参数序是 `(oldrow, oldcol, newrow, newcol)` —— 与 `move(y, x)` **相反**，
   老程序（`sl`）真的会直接调它。 */
int mvcur(int oldrow, int oldcol, int newrow, int newcol);
int wmove(WINDOW *w, int y, int x);
int getcury(WINDOW *w);
int getcurx(WINDOW *w);

/* ── 输出 ── */
int addch(int ch);
int addstr(const char *s);
int addwstr(const wchar_t *s);  /* 宽字符版：按码点转 UTF-8，见 curses.c 的说明 */
int mvaddstr(int y, int x, const char *s);
int mvaddch(int y, int x, int ch);
int printw(const char *fmt, ...);
int mvprintw(int y, int x, const char *fmt, ...);

/* ── 窗口版 `w*` 与"带窗口参数的移动版" `mvw*` ──
 *
 * 本实现只有一个窗口（`stdscr`，见文件头），所以这一族**全部转发**到上面那批
 * 不带窗口的函数，`w` 参数收下不用 —— 与 `newwin()` 返回 `stdscr` 是同一个退化。
 *
 * ⚠⚠ **必须有原型，这不是"为了让编译器别抱怨"**：实现在 `shared/src/curses.c` 里
 *   一直都有（90 个老程序要调），但本头文件此前**一条都没声明** ⇒ 实测后果是
 *   **整屏一个字符都画不出来**：
 *
 *     · 非变参的那几个（`mvwaddstr`/`mvwaddch`/`box`/`werase`…）**照样能画**；
 *     · **变参的那两个（`wprintw`/`mvwprintw`）会把参数传错**，
 *       程序在 `refresh()` **之前**就崩掉了 ⇒ 屏幕上一个字都没有
 *       （而屏上什么都没有，看起来就像"这个程序不兼容"）。
 *
 *   实测 `tty-clock`（689 行）：通篇只用 `mvwaddstr` / `mvwprintw` / `mvwaddch` /
 *   `werase` / `box` / `wattron` 这一族 ⇒ 修前全屏 4000 个空格、一个字符都没有。
 *   判据：`scripts/vml-c-probe/cases/35-window-api.c`。
 *
 *   教训与 `usleep` 那条同源、但**更隐蔽**：那次是"没实现 ⇒ 报未定义的函数"，
 *   这次是"实现了、没声明 ⇒ 不报错、只是画不出来"。 */
int wattron(WINDOW *w, int attrs);
int wattroff(WINDOW *w, int attrs);
int wattrset(WINDOW *w, int attrs);
int wclear(WINDOW *w);
int werase(WINDOW *w);
int wclrtoeol(WINDOW *w);
int waddch(WINDOW *w, int ch);
int waddstr(WINDOW *w, const char *s);
int wprintw(WINDOW *w, const char *fmt, ...);
int mvwprintw(WINDOW *w, int y, int x, const char *fmt, ...);
int mvwaddch(WINDOW *w, int y, int x, int ch);
int mvwaddstr(WINDOW *w, int y, int x, const char *s);
int box(WINDOW *w, int vch, int hch);
int wborder(WINDOW *w, int ls, int rs, int ts, int bs, int tl, int tr, int bl, int br);
int mvwin(WINDOW *w, int y, int x);

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

/* ── 窗口选项（**只记状态** —— 单窗口退化实现下它们没有可观察的效果，
      但老程序会把它们当"开关"调，收下即可） ── */
int leaveok(WINDOW *w, int bf);   /* 光标可留在任意位置（光标本来就不由我们管） */
int scrollok(WINDOW *w, int bf);  /* 到屏底自动滚屏（我们走 conio 那套滚屏） */
int idlok(WINDOW *w, int bf);     /* 插入/删除行优化（本平台无此概念） */
int raw(void);                    /* 关掉行缓冲/信号（本平台输入本来就是按行给的） */
int noraw(void);
int nonl(void);                   /* 输出不做 NL→CRLF 映射（本平台本就没有这层转换） */
int savetty(void);                /* 存/恢复终端状态：本平台无此概念，收下返回 OK */
int resetty(void);
int timeout(int ms);              /* 读键超时：**只收下**，见 curses.c 的说明 */
int wtimeout(WINDOW *w, int ms);
int napms(int ms);                /* 睡 N 毫秒（老程序拿它控帧率） */

/* ── 常用简写（真 ncurses 里就是宏/薄封装，老程序用得极多） ── */
int standout(void);               /* = attron(A_STANDOUT) */
int standend(void);               /* = attrset(A_NORMAL) */
int beep(void);                   /* 响铃：本平台无蜂鸣器，**返回 OK 不报错** */
int flash(void);                  /* 闪屏：同上 */
int has_ic(void);                 /* 恒 1：有插入/删除字符的能力（我们是重画整屏） */
int has_il(void);                 /* 恒 1：有插入/删除行的能力 */
int wnoutrefresh(WINDOW *w);      /* 延迟刷新 —— 直接做掉（见 curses.c） */
int doupdate(void);               /* 与 wnoutrefresh 配对，空操作 */

/* ── 窗口几何（照真 ncurses 用宏；本实现只有一个窗口，起点恒 (0,0)） ── */
#define getmaxyx(w,y,x)  ((y) = (w)->rows, (x) = (w)->cols)
#define getbegyx(w,y,x)  ((y) = 0, (x) = 0)
#define getparyx(w,y,x)  ((y) = -1, (x) = -1)

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
