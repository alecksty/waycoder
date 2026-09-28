/* old_gfx_bgi.c —— **原样的 BGI 程序**（图形界面）
 * old_gfx_bgi.c -- **a BGI program left exactly as it was** (graphical window)
 *
 * 类别：graphic
 * Category: graphic
 * 兼容面：`graphics.h` 整条 (BGI) —— `initgraph` / `closegraph` / `cleardevice`
 * Compatibility: the whole `graphics.h` (BGI) route -- `initgraph` / `closegraph` / `cleardevice`
 *         / `setcolor` / `setfillstyle` / `line` / `moveto` / `lineto` / `rectangle`
 *         / `bar` / `bar3d` / `circle` / `fillellipse` / `drawpoly` / `fillpoly`
 *         / `outtextxy` / `getmaxx` / `getmaxy` / `delay` / `kbhit` / `getch`
 *
 * ## 这个文件的写法是**刻意的**
 * ## This file is written the way it is **on purpose**
 *
 * 它按 Turbo C / Borland C++ 时代老图形程序的原貌写：**不 include 任何平台专有头**
 * It follows the original look of Turbo C / Borland C++ era graphics programs: **no platform-specific header is included**
 * （只用 `graphics.h` 与 `conio.h`）、不用 C99 之后的语法、变量都在函数开头声明、
 * (only `graphics.h` and `conio.h` are used), no post-C99 syntax, all variables declared at the top of the function,
 * 颜色一律写调色板**索引**（`RED` / `LIGHTGREEN`，而不是 RGB）。
 * colors are always written as palette **indexes** (`RED` / `LIGHTGREEN`, not RGB).
 *
 * 它的作用是**当判据**：这份源码能不能一行不改地编过、跑出画面 ——
 * Its role is to **serve as the criterion**: whether this source compiles unchanged and draws a picture --
 * 就是 `Lib/c/graphics.h` 这层转接是否成立的唯一标准。
 * that is the only standard for whether the `Lib/c/graphics.h` shim layer works.
 *
 * 手机上跑：`vml run examples/c/old/old_gfx_bgi.c`
 * On the phone: `vml run examples/c/old/old_gfx_bgi.c`
 */
#include <graphics.h>
#include <conio.h>

int main(void)
{
    int gd = DETECT;
    int gm;
    int i;
    int tri[8];     /* 顶点数组必须是**具名数组**：复合字面量本前端不支持且不报错 */
                    /* The vertex array must be a **named array**: this front end does not support compound literals and reports no error either */

    /* ── 老程序的标准开场 ── */
    /* -- The standard opening of an old program -- */
    initgraph(&gd, &gm, "");
    cleardevice();

    /* ── 画个坐标系，看看 getmaxx/getmaxy 报得对不对 ── */
    /* -- Draw a coordinate system to see whether getmaxx/getmaxy report the right values -- */
    setcolor(DARKGRAY);
    line(0, getmaxy() / 2, getmaxx(), getmaxy() / 2);
    line(getmaxx() / 2, 0, getmaxx() / 2, getmaxy());

    /* ── 三种线：直线、moveto/lineto 折线 ── */
    /* -- Three kinds of lines: a straight line, and moveto/lineto polylines -- */
    setcolor(LIGHTGREEN);
    line(10, 10, 200, 10);

    setcolor(LIGHTCYAN);
    moveto(10, 30);
    for (i = 0; i < 10; i++)
        lineto(10 + i * 18, 30 + (i % 2) * 14);

    /* ── 矩形与实心条 ── */
    /* -- Rectangles and filled bars -- */
    setcolor(YELLOW);
    rectangle(10, 70, 120, 130);

    setfillstyle(SOLID_FILL, RED);
    bar(140, 70, 250, 130);

    setfillstyle(SOLID_FILL, MAGENTA);
    bar3d(270, 70, 380, 130, 8, 1);

    /* ── 圆与椭圆（填充色由 setfillstyle 决定）── */
    /* -- Circle and ellipse (the fill color is decided by setfillstyle) -- */
    setcolor(WHITE);
    circle(80, 210, 40);

    setfillstyle(SOLID_FILL, LIGHTBLUE);
    fillellipse(220, 210, 55, 35);

    /* ── 多边形 ── */
    /* -- Polygons -- */
    tri[0] = 320; tri[1] = 250;
    tri[2] = 260; tri[3] = 170;
    tri[4] = 380; tri[5] = 170;
    tri[6] = 320; tri[7] = 250;
    setfillstyle(SOLID_FILL, LIGHTGREEN);
    fillpoly(4, tri);

    /* ── 文字（BGI 的 outtextxy 收的是左上角坐标）── */
    /* -- Text (BGI's outtextxy takes the top-left corner coordinates) -- */
    setcolor(WHITE);
    outtextxy(10, 270, "BGI 兼容层：initgraph / line / bar / circle / fillpoly");

    /* ── 老程序收尾：等一个键再退 ── */
    /* -- Old-program ending: wait for one key before exiting -- */
    outtextxy(10, 300, "按任意键退出（手机上点一下画面）");
    getch();
    closegraph();
    return 0;
}
