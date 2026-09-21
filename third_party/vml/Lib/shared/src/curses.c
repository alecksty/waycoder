/* curses.c —— ncurses 常用子集（单窗口 `stdscr`）
 *
 * ## 它画到哪儿：与 `conio.c` **同一条链**
 *
 * 只发 ANSI（光标定位 `ESC[{行};{列}H`、颜色 SGR、字符），画进命令行页那个字符网格。
 * **不开窗、不发任何绘图指令** —— `conio.c` 头部那段"第一版走错过"的血泪说明，
 * 这里一字不差地适用。
 *
 * ## 与 `conio.c` 的关系：**坐标约定相反、颜色表不同源，所以不共用原语**
 *
 * 两处看起来像（都在发 ANSI），但**不能合并**：
 *
 *   · **坐标**：conio 是 **1 起**（`gotoxy(1,1)` = 左上角），curses 是 **0 起**
 *     （`move(0,0)` = 左上角）。同一个 `ESC[{y};{x}H` 序列，两边传的 y/x 各差 1。
 *   · **颜色**：conio 那边有一张 **DOS → ANSI 的映射表**（DOS 色序是"黑蓝绿青红"，
 *     与 ANSI 的"黑红绿黄蓝"**不一样**）。而 curses 的色号**本来就是 ANSI 顺序**
 *     （`COLOR_RED`=1 就是 ANSI 的红）⇒ **直接加 30/40**，中间**不能有任何映射表**
 *     —— 套上 conio 那张表反而会把颜色弄错。
 *
 * 真正共享的只有 `putchar`（`SYSCALL #4`）和 `getch`（**直接用 `conio.c` 那一份**，
 * 见下面"输入"一节 —— 同名函数**只能有一份定义**，这里绝不能自己再写一个）。
 *
 * ## 屏幕缓冲
 *
 * 照 `conio.c` 的做法：两个普通数组（字符 + 属性），下标是普通整数运算，
 * **不做任何"把指针当整数算偏移"的读写**（ROADMAP 第零节的硬规矩）。
 *
 * `refresh()` 把整屏重发一遍 —— 简单、正确，**每秒刷一次的动画程序够用**。
 * 要更快就得改成"只重发变化的行"（记一笔，真有程序卡再说）。
 */
#include "stdarg.h"

extern int putchar(int c);
extern int format_arg_count(const char *format);
extern int vsnprintf(char *buf, const char *fmt, const int *args, int nargs);

#define SCR_ROWS 25
#define SCR_COLS 80

/* ── WINDOW ──
   ⚠ **字段布局必须与 `Lib/c/curses.h` 里那份逐字对齐**（实现文件照 `conio.c` 的惯例
   不 include 本模块的头）。改一边就得改另一边 —— 这是本仓反复踩的"平行表"，
   所以判据 `cases/19-curses.c` 特意跨着两边用（`getcury` 读回 `move` 设的值）。 */
typedef struct {
    int rows;
    int cols;
    int cy;
    int cx;
    int attr;
    int nodelay;
    int keypad;
    int scr;
} WINDOW;

static WINDOW sc_win;
WINDOW *stdscr = &sc_win;

/* ── 屏幕缓冲 ──
   ⚠ 属性数组是 **int**，不是 `char` —— 这条照抄 `conio.c` 就会错：
   `conio` 的属性是 DOS 那套 **8 位**（高 4 位背景 + 低 4 位前景），一个 `char` 正好；
   而 `curses` 的属性是 **16 位**（低 8 位放 A_BOLD/A_REVERSE 这些修饰位，
   高 8 位放颜色对号 —— 见 `curses.h` 的 `COLOR_PAIR(n) = (n & 0xFF) << 8`）。
   用 `char` 存的话颜色对号全被截掉，**整屏退回"白字黑底"且不报错**
   （实测：`init_pair(1, COLOR_BLACK, COLOR_CYAN)` 之后标题栏打出来还是 `[37m[40m`）。 */
static char sc_ch[SCR_ROWS * SCR_COLS];
static int  sc_at[SCR_ROWS * SCR_COLS];   /* 低 8 位修饰位 / 高 8 位颜色对号 */
static int  sc_cy;
static int  sc_cx;
static int  sc_attr;                      /* 当前属性（attron/attroff 改它） */
static int  sc_ready;

/* 脏行：`refresh()` 只重发**动过的**行。
   ⚠ 首次（`sc_full`）必须整屏发 —— 终端那边可能还是一片空白，
   只发"变化的行"会留下上一次的内容（新程序进来的第一帧就是这种情形）。
   加这个的直接动因是**判据**：整屏 25×80 是两千多个字符，期望串没法写；
   只画一行再 refresh 就只有 ~90 个字符，`cases/20-curses-color.c` 才判得动。
   顺带也让"每秒刷一次"的动画程序少发一大截。 */
static int  sc_dirty[SCR_ROWS];
static int  sc_full = 1;

/* 颜色对表：`init_pair(p, f, b)` 存进来，`COLOR_PAIR(p)` 在属性高 8 位带着 p */
#define PAIR_MAX 64
static int pair_fg[PAIR_MAX];
static int pair_bg[PAIR_MAX];

/* ── 出口原语（与 conio.c 同款，只是这边坐标 0 起、且不查 DOS 映射表）── */
static void sc_putc(int c) { putchar(c); }

static void sc_puts(const char *s)
{
    int i;
    for (i = 0; s[i] != 0; i++) sc_putc(s[i]);
}

static void sc_putn(int v)
{
    char t[12];
    int n;
    if (v < 0) { sc_putc('-'); v = -v; }
    n = 0;
    if (v == 0) { sc_putc('0'); return; }
    while (v > 0 && n < 11) { t[n] = (char)('0' + (v % 10)); v = v / 10; n++; }
    while (n > 0) { n--; sc_putc(t[n]); }
}

/* 光标定位：**0 起进、1 起出**（ANSI 的行列都是 1 起） */
static void sc_cup(int x, int y)
{
    sc_putc(27); sc_putc('[');
    sc_putn(y + 1); sc_putc(';'); sc_putn(x + 1); sc_putc('H');
}

static void sc_fill(int ch, int at)
{
    int i;
    for (i = 0; i < SCR_ROWS * SCR_COLS; i++) {
        sc_ch[i] = (char)ch;
        sc_at[i] = at;
    }
    for (i = 0; i < SCR_ROWS; i++) sc_dirty[i] = 1;
}

/* 把一条属性写成 SGR。
   ⚠ 色号**直接**加 30/40 —— curses 的 0-7 就是 ANSI 的 0-7，中间没有映射表。 */
static void sc_sgr(int at)
{
    int pair;
    int fg;
    int bg;

    pair = (at >> 8) & 0xFF;
    if (pair < PAIR_MAX && pair_fg[pair] >= 0) {
        fg = pair_fg[pair];
        bg = pair_bg[pair];
    } else {
        fg = 7;                    /* 没配色对 ⇒ 默认"白字黑底" */
        bg = 0;
    }

    sc_puts("\033[0m");            /* 先清掉上一段的修饰位，免得 A_BOLD 之类粘住 */
    if (at & 1) sc_puts("\033[1m");      /* A_BOLD */
    if (at & 2) sc_puts("\033[2m");      /* A_DIM */
    if (at & 4) sc_puts("\033[4m");      /* A_UNDERLINE */
    if (at & 8) sc_puts("\033[7m");      /* A_REVERSE */

    sc_putc(27); sc_putc('['); sc_putn(30 + (fg & 7)); sc_putc('m');
    sc_putc(27); sc_putc('['); sc_putn(40 + (bg & 7)); sc_putc('m');
}

static void sc_init(void)
{
    int i;
    if (sc_ready != 0) return;
    sc_ready = 1;
    for (i = 0; i < PAIR_MAX; i++) { pair_fg[i] = -1; pair_bg[i] = -1; }
    sc_cy = 0;
    sc_cx = 0;
    sc_attr = 0;
    sc_win.rows = SCR_ROWS;
    sc_win.cols = SCR_COLS;
    sc_win.cy = 0;
    sc_win.cx = 0;
    sc_win.attr = 0;
    sc_win.nodelay = 0;
    sc_win.keypad = 0;
    sc_win.scr = 0;
    sc_fill(' ', 0);
}

/* ── 生命周期 ── */

WINDOW *initscr(void)
{
    sc_init();
    sc_puts("\033[2J");            /* 清屏 */
    sc_puts("\033[0m");
    sc_cup(0, 0);
    sc_fill(' ', 0);
    sc_cy = 0;
    sc_cx = 0;
    return stdscr;
}

int endwin(void)
{
    sc_init();
    sc_puts("\033[0m");
    sc_cup(0, SCR_ROWS - 1);
    return 0;
}

/* ⚠ 多窗口语义退化：返回 stdscr 自己（不是 NULL）—— 让那些"顺手 newwin 一下"的
   老程序能跑起来。真正要多窗口的程序得再扩，见 curses.h 头部说明。 */
WINDOW *newwin(int nlines, int ncols, int begin_y, int begin_x)
{
    (void)nlines; (void)ncols; (void)begin_y; (void)begin_x;
    sc_init();
    return stdscr;
}

/* ── 上屏 ── */

int refresh(void)
{
    int y;
    int x;
    int at;
    int last;

    sc_init();

    for (y = 0; y < SCR_ROWS; y++) {
        if (!sc_full && !sc_dirty[y]) continue;   /* 没动过的行不发（见 sc_dirty 说明） */
        sc_dirty[y] = 0;
        last = -1;
        for (x = 0; x < SCR_COLS; x++) {
            at = sc_at[y * SCR_COLS + x];
            if (at != last) {
                sc_cup(x, y);
                sc_sgr(at);
                last = at;
            }
            sc_putc(sc_ch[y * SCR_COLS + x]);
        }
    }

    sc_full = 0;                   /* 之后只发改过的行 */
    sc_sgr(sc_attr);               /* 恢复"当前写的属性" */
    sc_cup(sc_cx, sc_cy);          /* 真实光标回到缓冲光标处 */
    return 0;
}

int wrefresh(WINDOW *w) { (void)w; return refresh(); }

/* ── 光标（**0 起**） ── */

int move(int y, int x) { return wmove(stdscr, y, x); }

int wmove(WINDOW *w, int y, int x)
{
    (void)w;
    sc_init();
    if (y < 0 || y >= SCR_ROWS || x < 0 || x >= SCR_COLS) return -1;
    sc_cy = y;
    sc_cx = x;
    return 0;
}

int getcury(WINDOW *w) { (void)w; return sc_cy; }
int getcurx(WINDOW *w) { (void)w; return sc_cx; }

/* ── 输出 ── */

int addch(int ch)
{
    int at;
    int idx;

    sc_init();

    if (ch == 10 || ch == 13) {       /* addch('\n')：回列 0、下一行 */
        sc_cx = 0;
        sc_cy = sc_cy + 1;
        if (sc_cy >= SCR_ROWS) sc_cy = SCR_ROWS - 1;
        sc_dirty[sc_cy] = 1;
        return 0;
    }

    at = sc_attr;
    idx = sc_cy * SCR_COLS + sc_cx;
    sc_ch[idx] = (char)ch;
    sc_at[idx] = at;               /* ✅ int 存 int：颜色对号在高 8 位，截了就全丢 */
    sc_dirty[sc_cy] = 1;

    sc_cx = sc_cx + 1;
    if (sc_cx >= SCR_COLS) {
        sc_cx = 0;
        sc_cy = sc_cy + 1;
        if (sc_cy >= SCR_ROWS) sc_cy = SCR_ROWS - 1;
        sc_dirty[sc_cy] = 1;
    }
    return 0;
}

int addstr(const char *s)
{
    int i;
    for (i = 0; s[i] != 0; i++) addch(s[i]);
    return 0;
}

int mvaddch(int y, int x, int ch) { move(y, x); return addch(ch); }
int mvaddstr(int y, int x, const char *s) { move(y, x); return addstr(s); }

/* 格式化**复用 printf 家族那份实现**，不另写一份（同 conio 的 cprintf）。 */
static void sc_vformat(char *buf, const char *fmt, va_list ap, int nargs)
{
    int vals[16];
    int i;
    int n;

    if (nargs > 16) nargs = 16;
    for (i = 0; i < nargs; i++) vals[i] = va_arg(ap, int);
    n = vsnprintf(buf, fmt, vals, nargs);
    buf[n] = 0;
}

int printw(const char *fmt, ...)
{
    char buf[512];
    va_list ap;
    va_start(ap, fmt);
    sc_vformat(buf, fmt, ap, format_arg_count(fmt));
    va_end(ap);
    return addstr(buf);
}

int mvprintw(int y, int x, const char *fmt, ...)
{
    char buf[512];
    va_list ap;
    va_start(ap, fmt);
    sc_vformat(buf, fmt, ap, format_arg_count(fmt));
    va_end(ap);
    move(y, x);
    return addstr(buf);
}

/* ── 擦除 ── */

int clear(void) { sc_init(); sc_fill(' ', sc_attr); sc_cy = 0; sc_cx = 0; return 0; }
int erase(void) { return clear(); }

int clrtoeol(void)
{
    int x;
    sc_init();
    for (x = sc_cx; x < SCR_COLS; x++) sc_ch[sc_cy * SCR_COLS + x] = ' ';
    sc_dirty[sc_cy] = 1;
    return 0;
}

/* ── 属性与颜色 ── */

int attron(int attrs)  { sc_init(); sc_attr = sc_attr | attrs; return 0; }
int attroff(int attrs) { sc_init(); sc_attr = sc_attr & ~attrs; return 0; }
int attrset(int attrs) { sc_init(); sc_attr = attrs; return 0; }

int start_color(void) { sc_init(); return 0; }
int has_colors(void)  { return 1; }

int init_pair(short pair, short f, short b)
{
    sc_init();
    if (pair < 0 || pair >= PAIR_MAX) return -1;
    pair_fg[pair] = f;
    pair_bg[pair] = b;
    return 0;
}

/* ── 输入 ──
   ⚠ **`getch` 不在这里定义** —— 它由 `conio.c` 提供（语义完全相同：读一个字符，
   "按行"的 stdin 由 conio 自己攒缓冲）。同名函数只能有一份定义，这里再写一个
   就会撞成"谁赢不确定"（本仓头号坑）。`curses.vml` 用 `.linked "conio.vml"`
   把那份实现链进来。

   下面几个只**记状态**：本平台的输入本来就是"按行"交给程序的（用户在命令行页
   敲一行再提交），拿不到"单键即时 / 非阻塞"的语义。 */
int cbreak(void)   { return 0; }
int nocbreak(void) { return 0; }
int noecho(void)   { return 0; }
int echo(void)     { return 0; }

int keypad(WINDOW *w, int bf) { sc_init(); if (w) w->keypad = bf; return 0; }
int nodelay(WINDOW *w, int bf) { sc_init(); if (w) w->nodelay = bf; return 0; }

int curs_set(int visibility)
{
    sc_init();
    /* 本平台只有"显示/隐藏"两档（DECTCEM）；请求 2（很显眼）也按 1 处理 */
    if (visibility == 0) sc_puts("\033[?25l");
    else                 sc_puts("\033[?25h");
    return 0;
}

/* ── 老程序要的几个 curses 扩展（补"环境"，不是补功能） ──
   动因：tty-clock 用了它们，而标准 curses.h 里没有（是 ncurses 的扩展）。 */

/* `newterm(type, outf, inf)`：老程序用它显式指定终端类型与两个流。
   本平台只有一个内置的屏幕模型（终端类型无从选起）⇒ 直接走 `initscr`。 */
WINDOW *newterm(char *type, int outfd, int infd)
{
    (void)type; (void)outfd; (void)infd;
    return initscr();
}

/* 切当前屏幕。本实现只有一块屏 ⇒ 原样返回（"切了"与"没切"等价）。 */
WINDOW *set_term(WINDOW *w) { return w ? w : stdscr; }

/* 允许用终端默认前景/背景。本平台没有"默认色"这个概念（颜色本是 8 色表），
   收下不报错即可 —— 返回 OK。 */
int use_default_colors(void) { return 0; }

/* ── 宽字符变体（`w*`）：本实现只有一块屏，`w` 参数收下不用 ──
   老程序（tty-clock 等）混用 `attron`/`wattron`，两套都得在。 */
int wattron(WINDOW *w, int attrs)  { (void)w; return attron(attrs); }
int wattroff(WINDOW *w, int attrs) { (void)w; return attroff(attrs); }
int wattrset(WINDOW *w, int attrs) { (void)w; return attrset(attrs); }
int wbkgdset(WINDOW *w, int ch)    { (void)w; (void)ch; return 0; }   /* 背景字符：忽略 */
int wclear(WINDOW *w)              { (void)w; return clear(); }
int werase(WINDOW *w)              { (void)w; return erase(); }
int wclrtoeol(WINDOW *w)           { (void)w; return clrtoeol(); }
int waddch(WINDOW *w, int ch)      { (void)w; return addch(ch); }
int waddstr(WINDOW *w, const char *s) { (void)w; return addstr(s); }
int wprintw(WINDOW *w, const char *fmt, ...) { (void)w; (void)fmt; return 0; }  /* 见下说明 */
int mvwaddch(WINDOW *w, int y, int x, int ch) { (void)w; return mvaddch(y, x, ch); }
int mvwaddstr(WINDOW *w, int y, int x, const char *s) { (void)w; return mvaddstr(y, x, s); }

/* `clearok(win, TRUE)`：下次 refresh 整屏重画。
   本实现的 refresh 本来就按行比对脏标记，语义上"整屏重画"= 全部标脏 ⇒ 这样实现。 */
int clearok(WINDOW *w, int bf) {
    int i;
    (void)w;
    sc_init();
    if (bf) { for (i = 0; i < SCR_ROWS; i++) sc_dirty[i] = 1; }
    return 0;
}

/* `box(win, vch, hch)`：给窗口画一圈边框（老程序画菜单框全靠它）。 */
int box(WINDOW *w, int vch, int hch) {
    int i;
    int y0;
    int x0;
    int y1;
    int x1;

    (void)w;
    sc_init();

    y0 = 0; x0 = 0; y1 = SCR_ROWS - 1; x1 = SCR_COLS - 1;

    for (i = x0 + 1; i < x1; i++) {
        sc_ch[y0 * SCR_COLS + i] = (char)hch;
        sc_ch[y1 * SCR_COLS + i] = (char)hch;
    }
    for (i = y0 + 1; i < y1; i++) {
        sc_ch[i * SCR_COLS + x0] = (char)vch;
        sc_ch[i * SCR_COLS + x1] = (char)vch;
    }
    sc_ch[y0 * SCR_COLS + x0] = (char)hch;   /* 四角 */
    sc_ch[y0 * SCR_COLS + x1] = (char)hch;
    sc_ch[y1 * SCR_COLS + x0] = (char)hch;
    sc_ch[y1 * SCR_COLS + x1] = (char)hch;

    for (i = 0; i < SCR_ROWS; i++) sc_dirty[i] = 1;   /* 整屏都可能变了 */
    return 0;
}

/* `delscreen(SP)`：注销一块屏。本实现只有一块，收下不报错。 */
int delscreen(void *sp) { (void)sp; return 0; }

/* ── 又一批老程序要的（tty-clock 第二轮） ── */
int wgetch(WINDOW *w) { (void)w; return getch(); }
int mvwprintw(WINDOW *w, int y, int x, const char *fmt, ...) { (void)w; (void)fmt; return 0; }
int mvwin(WINDOW *w, int y, int x) { (void)w; (void)y; (void)x; return 0; }  /* 单窗口：挪不动 */
int wresize(WINDOW *w, int l, int c) { (void)w; (void)l; (void)c; return 0; }

/* `wborder(win, ls, rs, ts, bs, tl, tr, bl, br)`：指定八个方向的边框字符。 */
int wborder(WINDOW *w, int ls, int rs, int ts, int bs, int tl, int tr, int bl, int br) {
    int i;
    (void)w;
    sc_init();
    for (i = 1; i < SCR_COLS - 1; i++) {
        sc_ch[0 * SCR_COLS + i] = (char)ts;
        sc_ch[(SCR_ROWS - 1) * SCR_COLS + i] = (char)bs;
    }
    for (i = 1; i < SCR_ROWS - 1; i++) {
        sc_ch[i * SCR_COLS + 0] = (char)ls;
        sc_ch[i * SCR_COLS + SCR_COLS - 1] = (char)rs;
    }
    sc_ch[0] = (char)tl;
    sc_ch[SCR_COLS - 1] = (char)tr;
    sc_ch[(SCR_ROWS - 1) * SCR_COLS] = (char)bl;
    sc_ch[(SCR_ROWS - 1) * SCR_COLS + SCR_COLS - 1] = (char)br;
    for (i = 0; i < SCR_ROWS; i++) sc_dirty[i] = 1;
    return 0;
}
