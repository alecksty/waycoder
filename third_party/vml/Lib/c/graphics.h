/* graphics.h —— **BGI（Borland Graphics Interface）兼容层**
 *
 * DOS 时代的老图形程序几乎都是这么开场的：
 *
 *     #include <graphics.h>
 *     int main(void) {
 *         int gd = DETECT, gm;
 *         initgraph(&gd, &gm, "");
 *         line(0, 0, 100, 100);
 *         circle(200, 200, 50);
 *         getch();
 *         closegraph();
 *     }
 *
 * 本头文件把**这一套名字**接过来，实现全部落在本平台的 `ui_*` 绘图接口上 ——
 * 老程序**一行不改**就能编译运行（这正是它存在的唯一理由）。
 *
 * ## 与「显存 / BGI 一整套不做」那条定案的关系（**不矛盾，是澄清**）
 *
 * 定案反对的是**直接操作内存**：往 `0xA0000` 显存直写、靠 BIOS 中断设模式、
 * `DEF SEG` 那类。它们在本平台上做不到，**也不该做**（见 docs/老程序兼容性.md 的 C 档）。
 *
 * 而这里做的是**转接**：BGI 的**函数名与语义**原样保留，落笔换成 `ui_*`
 * —— 程序看到的仍然是「画线 / 画圆 / 设色」，只是不再自己碰显存。
 * 换句话说：**要改的是"怎么画"，不改的是"程序怎么写"**。
 *
 * ## 落到哪个窗口
 *
 * `initgraph` 用 **`ui_win_open_pc`（第三种窗口：电脑屏）**：固定坐标系、不随旋转重排 ——
 * 正是为"按 640×480 排好版的老程序"准备的。老图形程序默认就是 VGA 640×480。
 *
 * ## ⚠ 颜色是**调色板索引**，不是 RGB
 *
 * BGI 的 `setcolor(4)` 是「红」而不是「RGB 0x000004」。老程序里到处是索引常量，
 * 所以这里按 **CGA/VGA 的 16 色标准调色板**翻译成 RGB（见下方 `_bgi_pal`）——
 * 不做这层翻译的话，老程序画面会**整片黑**（索引 0..15 当 RGB 用几乎全黑）。
 *
 * ## 覆盖范围
 *
 * 收的是**用得最多**的那些（用户定的规矩：只管大多数）。没做的（`getimage`/`putimage`
 * 位图块、`registerbgidriver`、字体文件、`floodfill` 的种子填充）**不假装支持** ——
 * 缺了就是编译期找不到符号，比"编得过、跑起来什么也没有"好排查。
 */
#ifndef _GRAPHICS_H
#define _GRAPHICS_H

#include <vml_compat.h>      /* `far`/`near` 这类扩展关键字按空宏抹掉 */
#include <waycoder_ui.h>     /* 落笔走 ui_* */

/* ── 模式与常量（老程序里到处在用）───────────────────────────── */
#define DETECT        0
#define VGA           9
#define EGA           5
#define CGA           3
#define IBM8514       6

#define MAXCOLORS     15

/* 16 色调色板索引（BGI 的常量名就是数字，这里给几个常用的别名） */
#define BLACK         0
#define BLUE          1
#define GREEN         2
#define CYAN          3
#define RED           4
#define MAGENTA       5
#define BROWN         6
#define LIGHTGRAY     7
#define DARKGRAY      8
#define LIGHTBLUE     9
#define LIGHTGREEN   10
#define LIGHTCYAN    11
#define LIGHTRED     12
#define LIGHTMAGENTA 13
#define YELLOW       14
#define WHITE        15

/* 填充样式 */
#define EMPTY_FILL    0
#define SOLID_FILL    1
#define LINE_FILL     2
#define LTSLASH_FILL  3
#define SLASH_FILL    4
#define BKSLASH_FILL  5
#define LTBKSLASH_FILL 6
#define HATCH_FILL    7
#define XHATCH_FILL   8
#define INTERLEAVE_FILL 9
#define WIDE_DOT_FILL 10
#define CLOSE_DOT_FILL 11
#define USER_FILL     12

/* 线型 / 线宽 */
#define SOLID_LINE    0
#define DOTTED_LINE   1
#define CENTER_LINE   2
#define DASHED_LINE   3
#define NORM_WIDTH    1
#define THICK_WIDTH   3

/* 文字对齐（settextjustify）*/
#define LEFT_TEXT     0
#define CENTER_TEXT   1
#define RIGHT_TEXT    2

/* 文字方向 */
#define HORIZ_DIR     0
#define VERT_DIR      1

/* 字体（settextstyle 的第一个参数）。
 * ⚠ 本平台**只有一种字形**，这些常量收下来只是为了"编得过"——
 *   按字体名切换字形的能力没有，字号（第三个参数）是**认的**（见 settextstyle）。
 *   老程序里 `settextstyle(SANS_SERIF_FONT, HORIZ_DIR, 2)` 这种写法极常见，
 *   不收常量的话整份源码一个字都编不过（实测：6 个经典 BGI 程序里有 2 个中招）。 */
#define DEFAULT_FONT        0
#define TRIPLEX_FONT        1
#define SMALL_FONT          2
#define SANS_SERIF_FONT     3
#define GOTHIC_FONT         4
#define SCRIPT_FONT         5
#define SIMPLEX_FONT        6
#define TRIPLEX_SCR_FONT    7
#define COMPLEX_FONT        8
#define EUROPEAN_FONT       9
#define BOLD_FONT           10

/* `NULL`：老程序普遍只 `#include <graphics.h>` 就拿它当空指针用（Turbo C 时代
 * 它由 BGI 的头**间接**带进来）。真去 `#include <stdlib.h>` 会连带牵进一大堆本平台
 * 用不着的东西，所以这里直接兜一个。
 * ⚠ 只兜"没定义过"的情形 —— 谁先定过就用谁的，避免重复定义（C 里 `#define` 重定义
 *   只在**展开后不同**时才报警，而 `((void*)0)` 与别的写法展开不同，会有警告刷屏）。 */
#ifndef NULL
#define NULL ((void *)0)
#endif

/* ── 内部状态（每个程序一份；本文件是头文件，用 static 隔离）────── */
static int _bgi_fg       = WHITE;   /* 当前前景（调色板索引）*/
static int _bgi_bg       = BLACK;   /* 当前背景 */
static int _bgi_fill_col = WHITE;   /* 当前填充色 */
static int _bgi_fill_pat = SOLID_FILL;
static int _bgi_line_w   = NORM_WIDTH;
static int _bgi_pen_x    = 0;       /* moveto/lineto 的当前点 */
static int _bgi_pen_y    = 0;
static int _bgi_txt_size = 16;      /* settextstyle 的字号 */
static int _bgi_txt_just = LEFT_TEXT;
static int _bgi_maxx     = 639;     /* getmaxx/getmaxy */
static int _bgi_maxy     = 479;
static int _bgi_opened   = 0;

/* CGA/VGA 标准 16 色调色板：BGI 的索引 → 本平台的 0xRRGGBB
 * ⚠ 不做这层翻译，`setcolor(4)` 会被当成 RGB 0x000004 ⇒ 画面几乎全黑。 */
static int _bgi_pal[16] = {
    0x000000,  /*  0 黑   */
    0x0000AA,  /*  1 蓝   */
    0x00AA00,  /*  2 绿   */
    0x00AAAA,  /*  3 青   */
    0xAA0000,  /*  4 红   */
    0xAA00AA,  /*  5 洋红 */
    0xAA5500,  /*  6 棕   */
    0xAAAAAA,  /*  7 浅灰 */
    0x555555,  /*  8 深灰 */
    0x5555FF,  /*  9 亮蓝 */
    0x55FF55,  /* 10 亮绿 */
    0x55FFFF,  /* 11 亮青 */
    0xFF5555,  /* 12 亮红 */
    0xFF55FF,  /* 13 亮洋红 */
    0xFFFF55,  /* 14 黄   */
    0xFFFFFF   /* 15 白   */
};

int _bgi_rgb(int idx)
{
    if (idx < 0) idx = 0;
    if (idx > 15) idx = 15;
    return _bgi_pal[idx];
}

/* ── 生命周期 ──────────────────────────────────────────────── */

/* 取路径里的文件名部分（`examples/c/old/x.c` → `x.c`）。
 * 老程序不设标题，用源文件名当标题最好认 —— 而 `__FILE__` 给的是**整条路径**。 */
char *_bgi_basename(char *p)
{
    char *last = p;
    char *q    = p;
    if (p == 0) return "";
    while (*q) {
        if (*q == '/' || *q == '\\') last = q + 1;
        q++;
    }
    return last;
}

/* 老程序的固定开场。`gd`/`gm` 按 BGI 语义是入参+出参，
 * 但本平台只有一种图形模式，所以只读取 `*gd` 是否 DETECT 来决定要不要自动选。
 *
 * ⚠ **`file` 参数存在的原因**：老程序 `initgraph(&gd,&gm,"")` **不设标题**，
 *   于是拿源文件名当窗口标题。而 `__FILE__` **不能写在头文件里** —— 它是文本替换，
 *   写在这儿会展开成 `graphics.h`（头自己的名字）。必须由下面的 `initgraph` 宏
 *   在**调用点**把它传进来，展开的才是**调用者那个文件**。 */
void _bgi_initgraph(int *gd, int *gm, char *path, char *file)
{
    int w = 640, h = 480;          /* VGA 默认 */

    (void)path;
    if (gd != 0 && *gd == VGA) { w = 640; h = 480; }
    if (gm != 0) *gm = VGA;

    /* ⚠ 用**电脑屏窗口**：固定坐标系、不随旋转重排 —— 老程序按 640×480 排的版
     *   换空间就会画到框外。第 5 个参数是要不要屏幕键盘（老图形程序常要按键）。*/
    ui_win_open_pc(_bgi_basename(file), w, h, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);

    _bgi_maxx   = w - 1;
    _bgi_maxy   = h - 1;
    _bgi_opened = 1;
    cleardevice();
}

/* 包一层的唯一目的：让 `__FILE__` 在**调用点**展开（见上面的 ⚠） */
#define initgraph(gd, gm, path)  _bgi_initgraph((gd), (gm), (path), __FILE__)

/* `initwindow` 是 **WinBGIm**（BGI 的 Windows 移植）的入口，语义就是"开一个指定大小的
 * 图形窗口"，不收 `gd`/`gm`：
 *
 *     initwindow(640, 480, "标题");
 *
 * 老程序里两条路都有（DOS 时代的用 `initgraph`，Windows 时代的用 `initwindow`），
 * 收下它一条程序都不用改。⚠ 窗口标题照 WinBGIm 的**第三参**给，不再拿源文件名兜
 * —— 这条与 `initgraph` 不同（那个的确不设标题）。
 *
 * ⚠ 同样要包一层宏：`__FILE__` 写在头文件里会展开成 `graphics.h` 自己。 */
void _bgi_initwindow(int w, int h, char *title, char *file)
{
    if (w <= 0) w = 640;
    if (h <= 0) h = 480;
    ui_win_open_pc((title != 0 && title[0] != 0) ? title : _bgi_basename(file),
                   w, h, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);
    _bgi_maxx   = w - 1;
    _bgi_maxy   = h - 1;
    _bgi_opened = 1;
    cleardevice();
}

#define initwindow(w, h, title)  _bgi_initwindow((w), (h), (title), __FILE__)

void closegraph(void)
{
    if (_bgi_opened) {
        ui_win_close();
        _bgi_opened = 0;
    }
}

void cleardevice(void)
{
    ui_clear(_bgi_rgb(_bgi_bg));
}

int getmaxx(void) { return _bgi_maxx; }
int getmaxy(void) { return _bgi_maxy; }

/* ── 设置 ──────────────────────────────────────────────────── */

void setcolor(int c)       { _bgi_fg = c; }
void setbkcolor(int c)     { _bgi_bg = c; }
void setfillstyle(int pattern, int color)
{
    (void)pattern;
    _bgi_fill_pat = pattern;
    _bgi_fill_col = color;
}
void setlinestyle(int style, unsigned pattern, int thickness)
{
    (void)style; (void)pattern;
    _bgi_line_w = (thickness == THICK_WIDTH) ? 3 : 1;
}
void settextstyle(int font, int dir, int size)
{
    (void)font; (void)dir;
    if (size > 0) _bgi_txt_size = size;
}
void settextjustify(int horiz, int vert)
{
    (void)vert;
    _bgi_txt_just = horiz;
}

/* ── 画图 ──────────────────────────────────────────────────── */

void line(int x1, int y1, int x2, int y2)
{
    ui_line(x1, y1, x2, y2, _bgi_rgb(_bgi_fg), _bgi_line_w);
}

void moveto(int x, int y) { _bgi_pen_x = x; _bgi_pen_y = y; }

void lineto(int x, int y)
{
    line(_bgi_pen_x, _bgi_pen_y, x, y);
    _bgi_pen_x = x;
    _bgi_pen_y = y;
}

void linerel(int dx, int dy)
{
    lineto(_bgi_pen_x + dx, _bgi_pen_y + dy);
}

/* ⚠ BGI 的 `rectangle(l, t, r, b)` 收的是**右下角坐标**，
 *   而本平台 `ui_rect` 收的是**宽高** —— 这层换算必须在这里做掉。 */
void rectangle(int l, int t, int r, int b)
{
    ui_rect(l, t, r - l, b - t, _bgi_rgb(_bgi_fg), 0, _bgi_line_w, 0);
}

void bar(int l, int t, int r, int b)
{
    ui_rect(l, t, r - l, b - t, _bgi_rgb(_bgi_fill_col), 1, 1, 0);
}

void bar3d(int l, int t, int r, int b, int depth, int topflag)
{
    bar(l, t, r, b);
    rectangle(l, t, r, b);
    if (topflag) {
        line(l, t, l + depth, t - depth);
        line(r, t, r + depth, t - depth);
        line(l + depth, t - depth, r + depth, t - depth);
        line(r, b, r + depth, b - depth);
        line(r + depth, b - depth, r + depth, t - depth);
    }
}

void circle(int x, int y, int r)
{
    ui_circle(x, y, r, _bgi_rgb(_bgi_fg), 0, _bgi_line_w);
}

void fillellipse(int x, int y, int xr, int yr)
{
    ui_ellipse(x, y, xr, yr, _bgi_rgb(_bgi_fill_col), 1, 1);
}

void ellipse(int x, int y, int st, int en, int xr, int yr)
{
    (void)st; (void)en;      /* 本平台的椭圆是整圈；起始/终止角暂不支持 */
    ui_ellipse(x, y, xr, yr, _bgi_rgb(_bgi_fg), 0, _bgi_line_w);
}

/* ⚠ 顶点数组必须传**具名数组**的地址 —— `(int[]){…}` 复合字面量本前端不支持
 *   而且不报错（见 Examples/c/draw_prims.c 的说明）。老 BGI 程序本来就是这么写的。 */
void drawpoly(int numpoints, int *polypoints)
{
    ui_polyline(polypoints, numpoints, _bgi_rgb(_bgi_fg), _bgi_line_w, 0);
}

void fillpoly(int numpoints, int *polypoints)
{
    ui_polygon(polypoints, numpoints, _bgi_rgb(_bgi_fill_col), _bgi_rgb(_bgi_fg),
               _bgi_line_w, 0);
}

void putpixel(int x, int y, int color)
{
    ui_pixel(x, y, _bgi_rgb(color));
}

/* ── 扇形 / 弧（`arc` / `pieslice` / `sector`）──────────────────
 *
 * 老程序画**饼图**就靠这三个（`Examples` 语料里 `pieslice` 用了 3 次、`arc` 2 次）。
 *
 * ## 为什么这三个走的是**另一套**绘图接口
 *
 * 上面那些图元用的是"颜色当参数"的老接口（`ui_circle(x,y,r,色,…)`）—— 那一套
 * **画不了扇形**（只能画整圈）。平台侧能画扇形的是新的刷子接口
 * `ui_draw_pie(cx, cy, r, a0, a1)`，它的颜色来自**当前刷子**而不是参数。
 * 所以这里先 `ui_set_fill`/`ui_set_pen` 设好再画。
 *
 * ## 两条硬约定
 *
 * ① **`ui_set_fill(0)` = 不填充**（宿主侧 `_fill = brush == 0 ? null : …`）——
 *    `arc` 要的正是"只有轮廓"，靠它表达；`pieslice`/`sector` 才填色。
 * ② **刷子是全局状态，画完必须复位** —— 不复位的话，这把刷子会**漏给后面的图元**
 *    （上一条 `pieslice` 的填充色粘到下一个 `rectangle` 上），而这类串色在画面上
 *    看着像"某个颜色配错了"，极难反查到是扇形留下的。
 *
 * ⚠ `sector` 在 BGI 里收的是**两个半径**（可画椭圆扇形），而平台侧只有单半径的
 *   `ui_draw_pie` ⇒ 这里按 `xr` 画**圆**扇形，椭圆扇形没做。这条是**能力缺口**，
 *   不是"忘了"：真遇到了要往宿主加一个椭圆扇形图元。
 */
static void _bgi_pie(int x, int y, int st, int en, int r, int filled)
{
    ui_set_fill(filled ? _bgi_rgb(_bgi_fill_col) : 0);
    ui_set_pen(_bgi_rgb(_bgi_fg), _bgi_line_w, 0, 0, 0);
    ui_draw_pie(x, y, r, st, en);

    /* 复位：不留状态给后面的图元（见上面第 ② 条）*/
    ui_set_fill(0);
    ui_set_pen(_bgi_rgb(_bgi_fg), NORM_WIDTH, 0, 0, 0);
}

void arc(int x, int y, int st, int en, int r)              { _bgi_pie(x, y, st, en, r, 0); }
void pieslice(int x, int y, int st, int en, int r)         { _bgi_pie(x, y, st, en, r, 1); }
void sector(int x, int y, int st, int en, int xr, int yr)  { (void)yr; _bgi_pie(x, y, st, en, xr, 1); }

/* ── 文字 ──────────────────────────────────────────────────── */

int _bgi_anchor(void)
{
    if (_bgi_txt_just == CENTER_TEXT) return VML_ANCHOR_CENTER;
    if (_bgi_txt_just == RIGHT_TEXT)  return VML_ANCHOR_RIGHT;
    return VML_ANCHOR_LEFT;
}

void outtextxy(int x, int y, char *s)
{
    ui_text(x, y, s, _bgi_rgb(_bgi_fg), _bgi_txt_size, _bgi_anchor());
}

void outtext(char *s)
{
    outtextxy(_bgi_pen_x, _bgi_pen_y, s);
}

/* ── 刷新与等待 ────────────────────────────────────────────── */

/* BGI 是**立即出图**（每画一笔就上屏），本平台是**保留模式**（改完调 present 才上屏）。
 * 为了让老程序的行为对得上，这里在"一段画完"的几个常见位置自动 present；
 * 另外老程序结束前一般会 `getch()`，那里也 present 一次。 */
void _bgi_flush(void) { ui_present(); }

void delay(int ms)
{
    /* 老程序用 delay 做动画节拍；这里用 tick 轮询实现（不占宿主线程）*/
    int t0 = ui_tick();
    while (ui_tick() - t0 < ms) {
        if (ui_win_closed()) break;
    }
}

/* kbhit 是**非阻塞**查询 —— 必须用 `ui_poll_msg()`，
 * ⚠ 别顺手写成 `ui_wait_msg(0)`：那个 timeout 0 的语义是**无限等**（不是不阻塞），
 *   会把 kbhit 变成一个卡死的调用（本平台已经踩过一次，见 CHANGELOG）。 */
int kbhit(void) { return ui_poll_msg() > 0; }

/* getch：等一个键。`ui_wait_msg` 的 timeout 0 是**无限等**（不是不阻塞）。
 * 返回 0 表示窗口被关了 —— 老程序一般不看返回值，所以这里保证不返回负值。*/
int getch(void)
{
    int m;
    _bgi_flush();
    if (ui_wait(&m, 0) <= 0) return 0;
    return m;
}

void getmouse(int *x, int *y, int *buttons)
{
    (void)x; (void)y;
    if (buttons) *buttons = 0;
}

#endif /* _GRAPHICS_H */
