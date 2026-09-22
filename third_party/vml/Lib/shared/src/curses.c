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
extern void delay(int ms);   /* util.c —— `napms` 用它，见该处说明 */
extern int kbhit(void);      /* conio.c —— `wgetch` 的非阻塞探测，见该处说明 */

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

/* ncurses 的 `mvcur`：**立即**把**真实终端**的光标挪过去 —— 不走缓冲、不需要 `refresh()`。
 *
 * 与 `move()`/`wmove()` 的区别正是它存在的理由：那两个只改**缓冲里**的光标
 * （要 `refresh()` 才上屏），而 `mvcur` 作用于**物理光标**。
 * 所以这里**不碰** `sc_ch`/`sc_at` 那两块屏幕影子，也不改 `sc_cy`/`sc_cx`
 * —— 与 ncurses 的契约一致（`getcury()` 反映的是窗口缓冲的光标，不是物理光标）。
 *
 * ⚠ 参数序是 `(oldrow, oldcol, newrow, newcol)`，而内部原语 `sc_cup` 收的是
 * `(x, y)` —— **行列要倒过来传**，写反了不报错、只是光标跑到镜像位置。
 * 老程序（`sl` 就是）真的会直接调它。
 */
int mvcur(int oldrow, int oldcol, int newrow, int newcol) {
    (void)oldrow;
    (void)oldcol;
    if (newrow < 0 || newrow >= SCR_ROWS || newcol < 0 || newcol >= SCR_COLS) return -1;
    sc_cup(newcol, newrow);
    return 0;
}

/* ── 输出 ── */

/* ── 落位原语：`addch` 与 `sc_putwchar` 共用这一对 ──
   分开成"写一个字节"和"推进一列"两件事，是因为**它们的组合方式不同**：
   `addch` 是"写一个字节 + 推进"（一次调用 = 一个字符 = 一列），
   而一个多字节码点是"写 N 个字节 + 推进**一次**"。 */

/* 往**当前单元格**写一个字节，**不推进光标** */
static void sc_cell_byte(int b)
{
    int idx = sc_cy * SCR_COLS + sc_cx;
    sc_ch[idx] = (char)b;
    sc_at[idx] = sc_attr;          /* ✅ int 存 int：颜色对号在高 8 位，截了就全丢 */
    sc_dirty[sc_cy] = 1;
}

/* 光标推进一列（到行尾则换行） */
static void sc_advance(void)
{
    sc_cx = sc_cx + 1;
    if (sc_cx >= SCR_COLS) {
        sc_cx = 0;
        sc_cy = sc_cy + 1;
        if (sc_cy >= SCR_ROWS) sc_cy = SCR_ROWS - 1;
        sc_dirty[sc_cy] = 1;
    }
}

int addch(int ch);   /* 定义在本函数之后（下面还要用它，先声明） */

/* 把**一个 Unicode 码点**写进缓冲：编码成 UTF-8、逐字节落位。
 *
 * ⚠ 与 `addch` **分家**是有意的。此前这里把"多字节只占一列"做成了 `addch`
 * 里的一条特例（"UTF-8 续字节 0x80–0xBF 不推进光标"）—— **那条规则放错了层**：
 * 它改的是 `addch` 这个**公开语义**（ncurses 的契约是"一次 `addch` = 一个字符
 * = 一列"），代价是**直接**调用 `addch(0xB8)` 的代码被静默吞掉。
 *
 * 实测（`addch(0xE4); addch(0xB8); addch(0xAD);`）：第二次与第三次落在**同一个
 * `sc_cx`** 上 ⇒ `B8` 被 `AD` **覆盖**，host 只收到 `E4` `AD`
 * （`VML_TRACE_OUT=1` 的日志逐字节可验）。症状是"UTF-8 序列中间少一个字节"，
 * 而下游（host 的 UTF-8 积攒）拿到孤立续字节只能解成 U+FFFD。 */
static void sc_putwchar(int cp)
{
    /* ⚠ 逐字节走 `addch`（**每个字节都占一格**）—— 这与"一个汉字占一列"的
       直觉相反，但**我们的屏幕缓冲 `sc_ch` 是字节数组**：`refresh()` 是
       "把这一行的 N 个字节原样发出去"，终端那边再按 UTF-8 自己合成字形。
       所以"一个字节 = 一个缓冲位置"才是自洽的。
       （早先我按"一个码点占一列"写成"三次写同一个格子"，结果是后一个字节
       覆盖前一个 —— 实测 `addwstr` 传 `0x4E2D` 只发出 `AD`。） */
    if (cp < 0x80) {
        addch(cp);
    } else if (cp < 0x800) {
        addch(0xC0 | (cp >> 6));
        addch(0x80 | (cp & 0x3F));
    } else if (cp < 0x10000) {
        addch(0xE0 | (cp >> 12));
        addch(0x80 | ((cp >> 6) & 0x3F));
        addch(0x80 | (cp & 0x3F));
    } else if (cp <= 0x10FFFF) {
        addch(0xF0 | (cp >> 18));
        addch(0x80 | ((cp >> 12) & 0x3F));
        addch(0x80 | ((cp >> 6) & 0x3F));
        addch(0x80 | (cp & 0x3F));
    } else {
        addch(0xEF); addch(0xBF); addch(0xBD);   /* U+FFFD */
    }
}

int addch(int ch)
{
    sc_init();

    if (ch == 10 || ch == 13) {       /* addch('\n')：回列 0、下一行 */
        sc_cx = 0;
        sc_cy = sc_cy + 1;
        if (sc_cy >= SCR_ROWS) sc_cy = SCR_ROWS - 1;
        sc_dirty[sc_cy] = 1;
        return 0;
    }

    /* **一次调用 = 一列**（ncurses 的契约）—— 不再有"续字节不推进"的特例，
       那条挪去了 `sc_putwchar`，见那里的说明。 */
    sc_cell_byte(ch);
    sc_advance();
    return 0;
}

int addstr(const char *s)
{
    int i;
    for (i = 0; s[i] != 0; i++) addch(s[i]);
    return 0;
}

/* ⚠ **越界必须返回 ERR，而且不能画** —— 这是 ncurses 的契约（`wmove` 越界即失败）。
 *
 * 本实现原先写的是 `move(y, x); return addch(ch);` —— `move` 越界会失败，
 * 但这里**把它的返回值丢了**，于是照样 `addch`：它写在 `move` 失败后**残留的旧位置**
 * 上，而且永远返回 0。后果是**越界字符悄悄写进别的格子**（画面既不对、又看不出
 * 错在哪），而不是被裁掉。
 *
 * ⚠ 纠正一处**我一度写错的说法**：曾以为 `sl` 是靠 `mvaddch` 越界返回 ERR 来收尾的。
 * 读了源码才知道不是 —— `add_D51` 自己判 `if (x < - D51LENGTH) return ERR;`，
 * 而 `my_mvaddstr(...)` 的返回值**根本没被检查**。
 * 所以这条修的是**裁剪语义**（越界不画），不是某个程序的收尾条件。 */
/* ⚠ 这里写 `-1` 而不是 `ERR`：**本文件照惯例不 include `curses.h`**（实现体不要那个头），
   所以 `ERR` 在这个翻译单元里根本没定义 —— 写成 `ERR` 会**编不过**，而 GenLib 只会打一行
   「失败: ... 未声明的变量 'ERR'」然后**跳过这个模块**，产物保持旧版 ⇒ 症状是
   "改了没生效"，而日志末尾照样是"完成"。`ERR` 的值就是 `-1`，两者等价。 */
int mvaddch(int y, int x, int ch)
{
    if (move(y, x) < 0) return -1;   /* 越界：不画，返回 ERR(-1) */
    return addch(ch);
}

int mvaddstr(int y, int x, const char *s)
{
    if (move(y, x) < 0) return -1;
    return addstr(s);
}

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
/* ⚠ `wprintw`/`mvwprintw` 此前是**空壳**（`(void)fmt; return 0;`）——
   编得过、链接得上、**就是什么都不打印**，而且不报错：老程序里
   `wprintw(win, "...")` 遍地都是，症状是"这个程序界面上没字"。
   本仓记过太多次「能编译 ≠ 这条路通了」，这就是其中一处。
   实现与上面的 `printw`/`mvprintw` 逐字同源（差异只在 `w` 参数收下不用）。 */
int wprintw(WINDOW *w, const char *fmt, ...)
{
    char buf[512];
    va_list ap;
    (void)w;
    va_start(ap, fmt);
    sc_vformat(buf, fmt, ap, format_arg_count(fmt));
    va_end(ap);
    return addstr(buf);
}
int mvwprintw(WINDOW *w, int y, int x, const char *fmt, ...)
{
    char buf[512];
    va_list ap;
    (void)w;
    va_start(ap, fmt);
    sc_vformat(buf, fmt, ap, format_arg_count(fmt));
    va_end(ap);
    move(y, x);
    return addstr(buf);
}
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
/* `wgetch`：**非阻塞模式下没键要返回 `ERR`**（不是等一整行）。
   判据是 `nodelay(win, TRUE)` 或 `timeout(0)` 设下的标志 —— 老程序
   （cmatrix / tty-clock 那一类）把它当帧节拍器用，返回 ERR 就走
   "这一帧没按键"的分支继续画。见 `conio.c` 的 `kbhit`。 */
/* ⚠⚠ `wgetch` **必须先刷新窗口** —— 这是 ncurses 的文档化行为：
 *
 *     "If the window is not a pad, and it has been moved or modified since
 *      the last call to wrefresh, wrefresh will be called before another
 *      character is read."
 *
 * **老程序正是靠这一点省掉显式 refresh 的**。实测 cmatrix 的主循环里
 * **一个 `refresh()` 都没有**（全文只有 3 处，全在 `finish`/`c_die`/
 * `resize_screen` 里）：它每帧 `addch` 改完屏幕缓冲，接着调
 * `wgetch(stdscr)`，刷新**全部指望这里**。
 *
 * 漏掉这一步的症状极具迷惑性：**程序在跑（进程活着、计时器在走、
 * `addch` 也在改缓冲）、屏幕上一个字都不出** —— 因为缓冲从来没被
 * 送到终端。这正是 `cmatrix` / `tty-clock` 那类"画面空"的真根因
 * （一度怀疑到 `newwin` 的退化实现上，那是错的）。
 *
 * 顺序也有讲究：**先 refresh 再判有没有键** —— 不然非阻塞模式下
 * 第一帧就 `return -1` 走了，永远刷不到。 */
int wgetch(WINDOW *w) {
    WINDOW *win = w ? w : stdscr;
    refresh();
    if (win && win->nodelay) {
        if (!kbhit()) return -1;   /* -1 = ERR（本文件不 include curses.h，照 sc_sgr 的字面量风格） */
    }
    return getch();
}
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

/* ── 窗口选项（cmatrix 第三轮）：**只收下、不记状态** ──
 *
 * 这一组在真 ncurses 里是"窗口行为开关"，但在**单窗口 + 自绘缓冲**这个
 * 退化实现下，它们**没有任何可观察的效果**：
 *   · `leaveok`  —— "光标可以留在任意位置"：我们的光标本来就不往真实终端上放
 *   · `scrollok` —— "到屏底自动滚"：滚屏走 conio 那套（`sc_putc` 里判的）
 *   · `idlok`    —— "插入/删除行优化"：我们是重画整行，没有这个概念
 *   · `raw`      —— "关掉行缓冲与信号"：本平台输入本来就是按行给的
 *
 * 所以**刻意不往 WINDOW 结构体里加字段** —— 记了也没人读，反而多一处
 * "两个文件的结构体要对齐"的同步点（本仓记过：`Lib/c/` 与 `Lib/shared/src/`
 * 的实现文件互不 include，全靠人工对齐，是头号坑之一）。
 * 与 `wresize`/`mvwin` 同一处置。 */
int leaveok(WINDOW *w, int bf) { (void)w; (void)bf; return 0; }
int scrollok(WINDOW *w, int bf) { (void)w; (void)bf; return 0; }
int idlok(WINDOW *w, int bf) { (void)w; (void)bf; return 0; }
int raw(void) { return 0; }
int noraw(void) { return 0; }

/* ── 常用简写：这两个是**真实现**（转调属性函数，语义完全等价） ──
   ⚠ 用字面量而不是 `A_STANDOUT`/`A_NORMAL` 宏 —— 本文件**不 include**
   `Lib/c/curses.h`，自己再 `#define` 一份就是"同一规则两处实现"
   （本仓头号坑）。本文件既有的写法也是字面量 + 注释，见 `sc_sgr`。 */
int standout(void) { return attron(8); }   /* 8 = A_REVERSE = A_STANDOUT */
int standend(void) { return attrset(0); }  /* 0 = A_NORMAL */

/* ── 响铃/闪屏：**返回 OK，不报错** ──
   手机没有蜂鸣器，但返回 ERR 会让不少老程序走进"终端不支持"的降级分支、
   甚至直接退出（见 docs/老程序兼容性.md 第四节那条总原则：能返回成功的
   就返回成功）。真要做，接的是 `ui_beep`（见 docs/VML宿主接口.md）。 */
int beep(void)  { return 0; }
int flash(void) { return 0; }

/* ── 能力查询：恒 1 ──
   问的是"能不能插入/删除字符与行"。本实现是**重画整屏**，
   从调用方看结果一样（屏幕正确），所以答"能"。 */
int has_ic(void) { return 1; }
int has_il(void) { return 1; }

/* ── 延迟刷新：真 ncurses 是"先攒着、doupdate 时一次上屏" ──
   本实现的 `refresh()` 已经是"按脏行比对后一次性发出"，没有可攒的东西
   ⇒ `wnoutrefresh` 直接做掉、`doupdate` 空操作。**两件事都做了**，
   所以 `wnoutrefresh(w); doupdate();` 这对写法与 `wrefresh(w)` 等价。 */
int wnoutrefresh(WINDOW *w) { return wrefresh(w); }
int doupdate(void) { return 0; }

/* ── 又一批（cmatrix 第四轮）：终端状态、输入超时、宽字符串 ── */

/* `savetty`/`resetty`：保存/恢复终端状态（老程序在 `endwin` 前后各调一次）。
   本平台没有"终端状态"可存 —— 要存的东西就是我们自己那块屏幕缓冲，
   而它本来就是持久的 ⇒ 收下返回 OK。 */
int savetty(void) { return 0; }
int resetty(void) { return 0; }

/* `nonl`：别把输出里的换行再映射成"回车+换行"。本平台发出去的字节原样
   到达，没有 ONLCR 那层转换 ⇒ 收下即可。 */
int nonl(void) { return 0; }

/* `timeout(ms)` / `wtimeout(win, ms)`：读键超时（`wtimeout` 是它的窗口版）。
   ⚠ **本平台输入是按行给的**（见文件头"与 conio 的三处语义相反"），
   没有"等 N 毫秒没键就返回 ERR"这回事 ⇒ **只收下、不改变行为**。
   已知的语义差距：老程序写 `timeout(0); ch = getch();` 做非阻塞轮询时，
   本平台会**阻塞在 getch 上等一整行** —— 不崩溃、按键仍会被处理，
   只是节奏与 ncurses 不同（这条写在 docs/老程序兼容性.md 的已知差距里）。 */
/* ⚠ `ms == 0`（**非阻塞**）**必须真的生效** —— 这个参数是老程序主循环的
   节拍器：`cmatrix` 第 469 行 `timeout(0);`，紧接着主循环
   `if ((ch = wgetch(stdscr)) != ERR) {…}` 靠"没键"来推进每一帧。
   早先这里写成"只收下、不改变行为"，后果是整个程序**一帧都画不出来**
   （第一句 wgetch 就阻塞读一整行）—— 见 `conio.c` 的 `kbhit` 说明。
   `ms > 0`（等 N 毫秒）本平台做不到：输入按行给，退化成阻塞。 */
int timeout(int ms) {
    sc_init();
    sc_win.nodelay = (ms == 0) ? 1 : 0;
    return 0;
}
int wtimeout(WINDOW *w, int ms) { (void)w; return timeout(ms); }

/* `napms(ms)`：睡 N 毫秒（老程序拿它控动画帧率）。
   走 `util.c` 的 `delay`（`SYSCALL #52`）—— 与 `dos.h` 的 `delay` 同一个底层。
   ⚠ **不能调 `sleep`**：那个名字在本仓有**两份语义不同的定义**
   （`builtins.c` 是"毫秒"，`dos.c` 是"秒"），链接器静默取一个 ⇒ 帧率差 1000 倍。 */
int napms(int ms) { if (ms > 0) delay(ms); return 0; }

/* `addwstr(ws)`：**宽字符串**版 `addstr`。
 *
 * ⚠ 这里踩过一个"看着像、其实完全不同"的坑：`wchar_t` 在本平台是
 * **4 字节**（`Lib/c/stddef.h`: `typedef unsigned int wchar_t`），
 * 所以 `wchar_t*` **不是** `char*`。早先图省事写成"转发 addstr"
 * ⇒ 一个字符的那 4 个字节被当成 4 个字符原样发出，实测 cmatrix 的
 * 矩阵里出现 `\001\0\0\0` 这样四字节一组的垃圾
 * （cmatrix 的用法是 `wchar_t ca[2]; ca[0] = val; ca[1] = 0; addwstr(ca);`）。
 *
 * 正确的做法是**按码点逐个编码成 UTF-8** —— 本平台的文本出口就是
 * UTF-8 字节流。这与 ncurses 把宽字符按当前 locale 转多字节是同一件事，
 * 只是我们的 locale 恒为 UTF-8（见 `locale.h`）。
 *
 * 逐字节调 `addch`，靠它对续字节"不推进光标"的那条规则，让一个多字节
 * 字符整体只占一列（真 ncurses 用 `wcwidth` 区分宽窄，本平台没有那一层）。 */
int addwstr(const unsigned int *ws)
{
    int i;
    int cp;

    sc_init();
    for (i = 0; ws[i] != 0; i++) {
        cp = (int)ws[i];
        if (cp < 0) {
            cp = 0xFFFD;                       /* 非法码点按替换字符处理 */
        }
        /* 逐**码点**落位（不是逐字节调 addch）—— 一个多字节码点占一列，
           落位规则收在 `sc_putwchar` 一处。 */
        sc_putwchar(cp);
    }
    return 0;
}


