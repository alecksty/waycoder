/* conio.c —— DOS / Turbo C 文本控制台（常用子集）
 *
 * 设计上三条硬约束（都来自本仓踩过的坑，不是风格偏好）：
 *
 * ① **不做任何"把指针当整数算偏移"的读写**（ROADMAP 第零节的硬规矩）。
 *    屏幕状态就是两个普通数组（字符 + 颜色），下标是普通整数运算。
 *
 * ② **不另立一套绘图原语**：全部落在既有的 `ui_*` 上（`ui_win_open` / `ui_clear` /
 *    `ui_rect` / `ui_text` / `ui_present` / `ui_wait` / `ui_poll_ex`）。
 *    自己再写一层"文本绘制"就是本仓排第一的坑 —— 同一规则两处实现。
 *
 * ③ **屏幕上每一个格子都有确切归属**：`conCh` 存字符、`conAt` 存颜色。
 *    `putch` 只重画**它改的那一格**（不整屏重刷）—— 老程序一次输出几个字符是常态，
 *    整屏重刷会让 80×25=2000 次绘图调用砸在每一行输出上。
 */
#include "waycoder_ui.h"

/* 变参：标准机制（前端内建），与 printf 家族同一份实现 */
#include "stdarg.h"

/* ⚠ **不引 `<conio.h>`**：那会把 `#param lib("conio")` 也带进来 —— 自己链自己。
   代价是这一层用不了头文件里的颜色名，所以下面只用 0..15 的**字面值**；
   颜色名的唯一定义仍在 `Lib/c/conio.h`（那是给用户程序看的公开面）。 */

/* printf 家族导出的两个符号（`Lib/shared/printf.vml`）。
   ⚠ **不引 `<stdio.h>`**：它把 `vsnprintf` 声明成标准四参形态（含 `va_list`），
     而实收的是**参数数组** —— 两者对不上（那条要单独理，见 CHANGELOG）。 */
extern int format_arg_count(const char *format);
extern int vsnprintf(char *buf, const char *fmt, const int *args, int nargs);

#define CON_ROWS 25
#define CON_COLS 80

/* ── 屏幕状态（普通数组，无地址算术）── */
static char conCh[CON_ROWS * CON_COLS];
static char conAt[CON_ROWS * CON_COLS];      /* 低 4 位前景 / 高 4 位背景 */
static int  conX;                            /* 光标列 0 起（对外 +1） */
static int  conY;                            /* 光标行 0 起（对外 +1） */
static int  conFg;
static int  conBg;
static int  conOpen;
static int  conCellW;
static int  conCellH;
static int  conFont;
static int  conPending;                      /* 扩展键：已取出 0，扫描码待下一次 getch 返回（-1 = 没有） */

/* 前置声明：滚动与整屏重画在 con_newline 里就要用（C 里"先用后声明"必须显式） */
static void con_clear_buffer_tail(void);
static void con_repaint_all(void);

/* ── DOS 16 色 → 0xAARRGGBB（实测取的是"DOS 默认调色板"那一档观感）── */
static int con_doscolor(int c)
{
    if (c == 0)  return 0xFF000000;   /* 黑 */
    if (c == 1)  return 0xFF0000AA;   /* 蓝 */
    if (c == 2)  return 0xFF00AA00;   /* 绿 */
    if (c == 3)  return 0xFF00AAAA;   /* 青 */
    if (c == 4)  return 0xFFAA0000;   /* 红 */
    if (c == 5)  return 0xFFAA00AA;   /* 品红 */
    if (c == 6)  return 0xFFAA5500;   /* 棕 */
    if (c == 7)  return 0xFFAAAAAA;   /* 浅灰 */
    if (c == 8)  return 0xFF555555;   /* 深灰 */
    if (c == 9)  return 0xFF5555FF;   /* 亮蓝 */
    if (c == 10) return 0xFF55FF55;   /* 亮绿 */
    if (c == 11) return 0xFF55FFFF;   /* 亮青 */
    if (c == 12) return 0xFFFF5555;   /* 亮红 */
    if (c == 13) return 0xFFFF55FF;   /* 亮品红 */
    if (c == 14) return 0xFFFFFF55;   /* 黄 */
    return 0xFFFFFFFF;                /* 白 */
}

/* 打开窗口 + 算出字符格尺寸。**只做一次**（惰性：老程序可能先算再打印）。 */
static void con_ensure(void)
{
    int sw;
    int sh;
    if (conOpen != 0) return;

    sw = ui_scr_w();
    sh = ui_scr_h();

    conCellW = sw / CON_COLS;
    if (conCellW < 2) conCellW = 2;
    conCellH = conCellW * 2;
    if (conCellH * CON_ROWS > sh && sh > 0) conCellH = sh / CON_ROWS;   /* 高度优先不超出 */
    if (conCellH < 4) conCellH = 4;
    conFont = conCellH - 2;
    if (conFont < 6) conFont = 6;

    ui_win_open("控制台", conCellW * CON_COLS, conCellH * CON_ROWS);
    conOpen = 1;
    conPending = -1;            /* ⚠ 文件作用域 static 初值是 0，而 0 是合法扫描码 —— 必须显式置 -1 */
    conFg = 7;                  /* = conio.h 的 LIGHTGRAY（DOS 定义的默认前景） */
    conBg = 0;                  /* = conio.h 的 BLACK */
    clrscr();
}

/* 清空内存里的屏幕缓冲（不动窗口） */
static void con_clear_buffer(void)
{
    int i;
    int at;
    at = (conBg << 4) | conFg;
    for (i = 0; i < CON_ROWS * CON_COLS; i++) {
        conCh[i] = ' ';
        conAt[i] = (char)at;
    }
}

/* 画一个格（先铺底色再写字）。**只画这一格** —— 调用方保证它确实变了。 */
static void con_paint(int x, int y)
{
    int idx;
    int at;
    char one[2];
    int px;
    int py;

    if (x < 0 || x >= CON_COLS || y < 0 || y >= CON_ROWS) return;
    idx = y * CON_COLS + x;
    at = conAt[idx];

    px = x * conCellW;
    py = y * conCellH;

    /* 底色：空格也要铺（老程序整屏都是"底色 + 空格"画出来的） */
    ui_rect(px, py, conCellW, conCellH, con_doscolor((at >> 4) & 15), 1, 0, 0);

    if (conCh[idx] == ' ') return;      /* 空格不必写字 */

    one[0] = conCh[idx];
    one[1] = 0;
    ui_text(px, py, one, con_doscolor(at & 15), conFont, VML_ANCHOR_LEFT);
}

/* ── 光标推进 / 换行 / 滚屏 ── */
static void con_newline(void)
{
    conX = 0;
    conY = conY + 1;
    if (conY >= CON_ROWS) {
        /* 滚一行：整屏上移（普通数组搬运，无地址算术） */
        int i;
        for (i = 0; i < (CON_ROWS - 1) * CON_COLS; i++) {
            conCh[i] = conCh[i + CON_COLS];
            conAt[i] = conAt[i + CON_COLS];
        }
        con_clear_buffer_tail();
        conY = CON_ROWS - 1;
        con_repaint_all();
    }
}

/* 只清最后一行（滚屏用） */
static void con_clear_buffer_tail(void)
{
    int i;
    int at;
    int base;
    at = (conBg << 4) | conFg;
    base = (CON_ROWS - 1) * CON_COLS;
    for (i = 0; i < CON_COLS; i++) {
        conCh[base + i] = ' ';
        conAt[base + i] = (char)at;
    }
}

static void con_repaint_all(void)
{
    int x;
    int y;
    for (y = 0; y < CON_ROWS; y++)
        for (x = 0; x < CON_COLS; x++)
            con_paint(x, y);
    ui_present();
}

/* ── 公开接口 ── */

void clrscr(void)
{
    con_ensure();
    con_clear_buffer();
    ui_clear(con_doscolor(conBg));
    ui_present();
    conX = 0;
    conY = 0;
}

void clreol(void)
{
    int x;
    int at;
    con_ensure();
    at = (conBg << 4) | conFg;
    for (x = conX; x < CON_COLS; x++) {
        conCh[conY * CON_COLS + x] = ' ';
        conAt[conY * CON_COLS + x] = (char)at;
        con_paint(x, conY);
    }
    ui_present();
}

void gotoxy(int x, int y)
{
    conX = x - 1;
    conY = y - 1;
    if (conX < 0) conX = 0;
    if (conX > CON_COLS - 1) conX = CON_COLS - 1;
    if (conY < 0) conY = 0;
    if (conY > CON_ROWS - 1) conY = CON_ROWS - 1;
}

int wherex(void) { return conX + 1; }
int wherey(void) { return conY + 1; }

void textcolor(int color)
{
    conFg = color & 15;
}

void textbackground(int color)
{
    conBg = color & 15;
}

void textattr(int attr)
{
    conFg = attr & 15;
    conBg = (attr >> 4) & 15;
}

void highvideo(void) { conFg = conFg | 8; }
void lowvideo(void)  { conFg = conFg & 7; }
void normvideo(void) { conFg = 7; conBg = 0; }   /* 7/0 = conio.h 的 LIGHTGRAY / BLACK */

void putch(int c)
{
    int idx;
    con_ensure();

    if (c == 10 || c == 13) {          /* \n 与 \r 都当换行（DOS 里 \n 自带 \r） */
        con_newline();
        return;
    }
    if (c == 8) {                      /* 退格 */
        if (conX > 0) conX = conX - 1;
        return;
    }

    idx = conY * CON_COLS + conX;
    conCh[idx] = (char)c;
    conAt[idx] = (char)((conBg << 4) | conFg);
    con_paint(conX, conY);
    ui_present();

    conX = conX + 1;
    if (conX >= CON_COLS) con_newline();
}

void cputs(const char *s)
{
    int i;
    con_ensure();
    for (i = 0; s[i] != 0; i++) putch(s[i]);
}

void delline(void)
{
    int y;
    int x;
    con_ensure();
    for (y = conY; y < CON_ROWS - 1; y++)
        for (x = 0; x < CON_COLS; x++) {
            conCh[y * CON_COLS + x] = conCh[(y + 1) * CON_COLS + x];
            conAt[y * CON_COLS + x] = conAt[(y + 1) * CON_COLS + x];
        }
    con_clear_buffer_tail();
    con_repaint_all();
}

void insline(void)
{
    int y;
    int x;
    int at;
    con_ensure();
    at = (conBg << 4) | conFg;
    for (y = CON_ROWS - 1; y > conY; y--)
        for (x = 0; x < CON_COLS; x++) {
            conCh[y * CON_COLS + x] = conCh[(y - 1) * CON_COLS + x];
            conAt[y * CON_COLS + x] = conAt[(y - 1) * CON_COLS + x];
        }
    for (x = 0; x < CON_COLS; x++) {
        conCh[conY * CON_COLS + x] = ' ';
        conAt[conY * CON_COLS + x] = (char)at;
    }
    con_repaint_all();
}

/* 方向键：DOS 的**扩展键扫描码**（先返回 0，下一次再返回它） */
static int con_scan_code(int vml_key)
{
    if (vml_key == VML_KEY_UP)    return 72;
    if (vml_key == VML_KEY_DOWN)  return 80;
    if (vml_key == VML_KEY_LEFT)  return 75;
    if (vml_key == VML_KEY_RIGHT) return 77;
    if (vml_key == VML_KEY_SELECT) return 28;   /* Enter 的扩展码（手柄 SELECT） */
    if (vml_key == VML_KEY_PAUSE)  return 1;    /* Esc 的扩展码（手柄 PAUSE） */
    return -1;                                   /* 不是扩展键 */
}

int getch(void)
{
    int msg[4];
    int k;
    int sc;

    if (conPending >= 0) {              /* 上一拍取出了 0，扫描码还没交出去 */
        k = conPending;
        conPending = -1;
        return k;
    }

    ui_wait(msg, 0);                    /* 0 = 一直等 */
    if (msg[0] != VML_MSG_KEYDOWN) return 0;

    k = msg[1];
    sc = con_scan_code(k);
    if (sc >= 0) {                      /* 扩展键：这一拍返回 0，扫描码留给下一次 */
        conPending = sc;
        return 0;
    }
    return k;
}

int getche(void)
{
    int c;
    c = getch();
    if (c != 0) putch(c);
    return c;
}

int kbhit(void)
{
    int msg[4];
    if (conPending >= 0) return 1;      /* 扫描码还压着，也算"有按键" */

    if (ui_poll_ex(msg, VML_MSG_KEEP) == 0) return 0;    /* 0 = 队列空 */
    if (msg[0] != VML_MSG_KEYDOWN) return 0;
    return 1;
}

/* `cprintf` 的格式化复用 `sprintf`（与 `printf` 同一套实现，不另写一份格式化）——
   所以这里只需要一个足够大的缓冲。见 printf.c 的说明。 */
void cprintf(const char *fmt, ...)
{
    /* 格式化**复用 printf 家族的实现**（`format_arg_count` + `vsnprintf`）——
       不在这里另写一份格式化（"同一规则两处实现"是本仓排第一的坑）。
       两个符号由 `Lib/shared/printf.vml` 导出，C 程序链接时本来就会带上它（printf 一直在）。

       ⚠ 变参走**标准内建**取，不自己算形参地址（ROADMAP 第零节的硬规矩）。 */
    char buf[512];
    int vals[16];
    int i;
    int nargs;
    int n;
    va_list ap;

    nargs = format_arg_count(fmt);
    if (nargs > 16) nargs = 16;

    va_start(ap, fmt);
    for (i = 0; i < nargs; i++) vals[i] = va_arg(ap, int);
    va_end(ap);

    n = vsnprintf(buf, fmt, vals, nargs);
    buf[n] = 0;
    cputs(buf);
}
