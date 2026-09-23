/* test_bgi.c —— **BGI 兼容层的全接口体检**（一屏"带标签的格子" + 一份文本小结）。
 *
 * 做法照 `draw_prims.c` 的先例：每个接口一个小格子，格子里画出它的效果，
 * 格子底下用 `outtextxy` 写上它的名字 —— 这样**文字接口本身也被覆盖**，
 * 人对着屏幕就能逐条打勾。
 *
 * ⚠ 与 `test_bgi.cpp` **逐字相同**（只有扩展名不同）。两份一起编、一起看，
 *   是因为 C 与 C++ 是**两个独立的前端**，同一份源码在两边的行为**并不保证一致** ——
 *   实测就有过只在一侧错的东西（`char s[] = "…"` 那条路：C++ 侧曾把字符数组
 *   落成"带长度头的 4 字节格数组"，于是 `outtextxy` 读到的是长度头，
 *   屏幕上只剩**一个乱字符**）。同一份源码两个前端各编一遍，是这类差异的唯一判据。
 *
 * ## 怎么读结果
 *
 * · **画面**：20 个格子，每个格子里是该接口的效果、格子底是它的名字。
 *   哪一格画得不对，一眼就能定位到接口名。
 * · **stdout**：能自判的接口把结论打成 `  ok   名字` / `  FAIL 名字`，
 *   最后一行是 `OK=n FAIL=m`。
 * · 输出**只用 `puts`/`putchar`**，不用 `printf` —— 本仓记过它在桌面脚手架里会崩
 *   （`MOVEB @2, R0`，地址是垃圾值）。
 *
 * ## 分辨率（`setgraphmode`）为什么不在这屏上画
 *
 * `setgraphmode` 的语义是"清屏 + 换模式"，而本平台上电脑屏窗口的坐标系**开窗时定死**
 * ⇒ 它只能**关掉重开**。所以多分辨率只在**开场**验一次（切过去、读 `getmaxx/getmaxy`、
 * 再切回来），画面本身仍然按 VGA 640×480 排 —— 挤在一屏里反而看不清。
 */

#include <graphics.h>
#include <stdlib.h>

/* ── 网格几何 ──────────────────────────────────────────────── */
#define COLS      5
#define ROWS      4
#define CELL_W    124
#define CELL_H    108
#define GRID_X    8
#define GRID_Y    30
#define IN_W      112       /* 格子内可画区（留给标签一条） */
#define IN_H      78

/* ── 自判统计 ──────────────────────────────────────────────── */
static int g_ok = 0;
static int g_bad = 0;

/* ── 文本输出小工具（绕开 printf）───────────────────────────── */

static void put_int(int v)
{
    char buf[12];
    int n = 0;
    if (v < 0) { putchar('-'); v = -v; }
    if (v == 0) { putchar('0'); return; }
    while (v > 0) { buf[n] = (char)('0' + v % 10); v = v / 10; n = n + 1; }
    while (n > 0) { n = n - 1; putchar(buf[n]); }
}

static void say(char *name, int v)
{
    puts(name);
    put_int(v);
    putchar('\n');
}

static void check(char *name, int ok)
{
    if (ok) { puts("  ok   "); g_ok  = g_ok + 1; }
    else    { puts("  FAIL "); g_bad = g_bad + 1; }
    puts(name);
    putchar('\n');
}

/* ── 格子定位（不用指针出参 —— 少一层"读不读得到"的疑点）───── */
static int cell_x(int idx) { return GRID_X + (idx % COLS) * CELL_W; }
static int cell_y(int idx) { return GRID_Y + (idx / COLS) * CELL_H; }
static int in_x(int idx)   { return cell_x(idx) + 6; }
static int in_y(int idx)   { return cell_y(idx) + 6; }

/* 格子的名字写在格底。**这一步就是 `outtextxy` 的判据**：
 * 名字画不出来的格子，是文字接口的问题，不是那一格图形的问题。 */
static void cell_label(int idx, char *name)
{
    setcolor(WHITE);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    settextjustify(LEFT_TEXT, 0);
    outtextxy(in_x(idx), in_y(idx) + IN_H + 6, name);
}

/* ── 多边形的顶点（必须是**具名数组**：`(int[]){…}` 复合字面量本前端不支持）── */
static int tri[8];
static int quad[10];

int main(void)
{
    int gd = DETECT;
    int gm = 0;
    int i;
    int cx, cy, w, h;
    void *img;
    int area;
    int t1, t2, t3;
    int px, py;

    initgraph(&gd, &gm, "");

    puts("=== BGI 自检开始 ===");

    /* ── 1. 模式与坐标系（DETECT ⇒ 驱动/模式由库选，本平台是 VGA 640×480）── */
    say("gd(driver)=", gd);
    say("gm(mode)=", gm);
    check("initgraph(DETECT) 选出 VGA 驱动", gd == VGA);
    check("initgraph(DETECT) 选出 VGAHI 模式", gm == VGAHI);
    say("getmaxx()=", getmaxx());
    say("getmaxy()=", getmaxy());
    check("VGAHI 是 640×480（maxx=639）", getmaxx() == 639);
    check("VGAHI 是 640×480（maxy=479）", getmaxy() == 479);

    /* ── 2. setgraphmode：换模式 = 换分辨率（只能关窗重开，见文件头）── */
    setgraphmode(VGALO);                       /* 640×200 */
    say("VGALO maxy=", getmaxy());
    check("setgraphmode(VGALO) 后 maxy=199", getmaxy() == 199);
    setgraphmode(VGAHI);                       /* 回到 640×480 */
    say("VGAHI maxy=", getmaxy());
    check("setgraphmode(VGAHI) 切回来 maxy=479", getmaxy() == 479);

    /* ── 3. cleardevice：整屏刷成背景色 ── */
    setbkcolor(BLACK);
    cleardevice();
    check("cleardevice 后左上角是背景色 BLACK", getpixel(0, 0) == BLACK);

    /* ── 4. 文字量测（`textwidth`/`textheight`）：老程序靠它居中排版 ──
     * ⚠ 这两个走的是宿主注册的 `text` JSON 函数（见 `Lib/c/graphics.h` 的
     *   "文字量测"一节）—— 与**渲染同源**才算得对，所以这里量出来的数
     *   与画面上的实际宽度是一回事。 */
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    t1 = textwidth("ABC");
    t2 = textheight("ABC");
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 3);
    t3 = textwidth("ABC");
    say("textwidth(ABC,size1)=", t1);
    say("textheight(ABC,size1)=", t2);
    say("textwidth(ABC,size3)=", t3);
    check("textwidth > 0", t1 > 0);
    check("textheight > 0", t2 > 0);
    check("字号变大 ⇒ textwidth 变大", t3 > t1);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);

    /* ── 5. 按格画图 ──────────────────────────────────────────── */
    /* ① line */
    cx = in_x(0); cy = in_y(0);
    setcolor(LIGHTGREEN);
    line(cx, cy, cx + IN_W - 2, cy + IN_H - 2);
    line(cx + IN_W - 2, cy, cx, cy + IN_H - 2);
    cell_label(0, "line");

    /* ② moveto / lineto / linerel */
    cx = in_x(1); cy = in_y(1);
    setcolor(CYAN);
    moveto(cx, cy);
    linerel(IN_W - 2, IN_H - 2);
    moveto(cx + IN_W - 2, cy);
    linerel(-(IN_W - 2), IN_H - 2);
    moveto(cx, cy + IN_H / 2);
    lineto(cx + IN_W - 2, cy + IN_H / 2);
    cell_label(1, "moveto/lineto");

    /* ③ rectangle */
    cx = in_x(2); cy = in_y(2);
    setcolor(YELLOW);
    rectangle(cx, cy, cx + IN_W - 2, cy + IN_H - 2);
    rectangle(cx + 20, cy + 16, cx + IN_W - 22, cy + IN_H - 18);
    cell_label(2, "rectangle");

    /* ④ bar */
    cx = in_x(3); cy = in_y(3);
    setfillstyle(SOLID_FILL, LIGHTBLUE);
    bar(cx, cy, cx + IN_W - 2, cy + IN_H - 2);
    setfillstyle(SOLID_FILL, LIGHTRED);
    bar(cx + 30, cy + 24, cx + IN_W - 32, cy + IN_H - 2);
    cell_label(3, "bar");

    /* ⑤ bar3d */
    cx = in_x(4); cy = in_y(4);
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, GREEN);
    bar3d(cx, cy + 16, cx + IN_W - 30, cy + IN_H - 18, 16, 1);
    cell_label(4, "bar3d");

    /* ⑥ circle */
    cx = in_x(5); cy = in_y(5);
    setcolor(LIGHTMAGENTA);
    circle(cx + IN_W / 2, cy + IN_H / 2, 30);
    circle(cx + IN_W / 2, cy + IN_H / 2, 18);
    cell_label(5, "circle");

    /* ⑦ fillellipse */
    cx = in_x(6); cy = in_y(6);
    setfillstyle(SOLID_FILL, BROWN);
    fillellipse(cx + IN_W / 2, cy + IN_H / 2, 44, 28);
    cell_label(6, "fillellipse");

    /* ⑧ ellipse */
    cx = in_x(7); cy = in_y(7);
    setcolor(LIGHTGRAY);
    ellipse(cx + IN_W / 2, cy + IN_H / 2, 0, 360, 46, 24);
    ellipse(cx + IN_W / 2, cy + IN_H / 2, 0, 360, 24, 34);
    cell_label(7, "ellipse");

    /* ⑨ arc（只描边、不填充）*/
    cx = in_x(8); cy = in_y(8);
    setcolor(LIGHTRED);
    arc(cx + IN_W / 2, cy + IN_H / 2, 30, 150, 32);
    cell_label(8, "arc");

    /* ⑩ pieslice */
    cx = in_x(9); cy = in_y(9);
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, YELLOW);
    pieslice(cx + IN_W / 2, cy + IN_H / 2, 0, 120, 32);
    cell_label(9, "pieslice");

    /* ⑪ sector（BGI 里可给两个半径；本平台按 `xr` 画**圆**扇形）*/
    cx = in_x(10); cy = in_y(10);
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, LIGHTCYAN);
    sector(cx + IN_W / 2, cy + IN_H / 2, 200, 320, 34, 20);
    cell_label(10, "sector");

    /* ⑫ drawpoly（BGI 的顶点表**首点要重复一次**才闭合）*/
    cx = in_x(11); cy = in_y(11);
    tri[0] = cx + 20;          tri[1] = cy + IN_H - 4;
    tri[2] = cx + IN_W / 2;    tri[3] = cy + 4;
    tri[4] = cx + IN_W - 20;   tri[5] = cy + IN_H - 4;
    tri[6] = tri[0];           tri[7] = tri[1];
    setcolor(LIGHTGREEN);
    drawpoly(4, tri);
    cell_label(11, "drawpoly");

    /* ⑬ fillpoly */
    cx = in_x(12); cy = in_y(12);
    quad[0] = cx + 16;         quad[1] = cy + 10;
    quad[2] = cx + IN_W - 16;  quad[3] = cy + 24;
    quad[4] = cx + IN_W - 30;  quad[5] = cy + IN_H - 6;
    quad[6] = cx + 6;          quad[7] = cy + IN_H - 20;
    quad[8] = quad[0];         quad[9] = quad[1];
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, LIGHTMAGENTA);
    fillpoly(5, quad);
    cell_label(12, "fillpoly");

    /* ⑭ putpixel / getpixel */
    cx = in_x(13); cy = in_y(13);
    for (py = 0; py < 6; py = py + 1)
        for (px = 0; px < 16; px = px + 1)
            putpixel(cx + 6 + px * 6, cy + 8 + py * 10,
                     (px + py) % 2 == 0 ? WHITE : BLUE);
    check("putpixel 后 getpixel 读回同一索引",
          getpixel(cx + 6, cy + 8) == WHITE);
    check("没画过的地方是背景色",
          getpixel(cx + IN_W - 2, cy + 2) == BLACK);
    cell_label(13, "putpixel");

    /* ⑮ floodfill（第三参是**边界色**，填什么由 setfillstyle 决定）*/
    cx = in_x(14); cy = in_y(14);
    setcolor(WHITE);
    rectangle(cx + 10, cy + 6, cx + IN_W - 12, cy + IN_H - 6);
    setfillstyle(SOLID_FILL, GREEN);
    floodfill(cx + IN_W / 2, cy + IN_H / 2, WHITE);
    check("floodfill 填进了 setfillstyle 的颜色",
          getpixel(cx + IN_W / 2, cy + IN_H / 2) == GREEN);
    cell_label(14, "floodfill");

    /* ⑯ imagesize / getimage / putimage */
    cx = in_x(15); cy = in_y(15);
    setfillstyle(SOLID_FILL, MAGENTA);
    bar(cx + 8, cy + 12, cx + 40, cy + 44);
    setcolor(WHITE);
    rectangle(cx + 8, cy + 12, cx + 40, cy + 44);
    area = imagesize(cx + 8, cy + 12, cx + 40, cy + 44);
    say("imagesize(33x33)=", area);
    check("imagesize > 0（老程序拿它去 malloc）", area > 0);
    img = malloc(area);
    getimage(cx + 8, cy + 12, cx + 40, cy + 44, img);
    putimage(cx + 56, cy + 12, img, COPY_PUT);
    putimage(cx + 30, cy + 52, img, XOR_PUT);      /* 贴回自己 ⇒ XOR 抹掉 */
    check("getimage+putimage(COPY) 贴出了颜色",
          getpixel(cx + 70, cy + 28) == MAGENTA);
    cell_label(15, "getimage/putimage");

    /* ⑰ setcolor：16 色调色板各来一条 */
    cx = in_x(16); cy = in_y(16);
    for (i = 0; i < 16; i = i + 1) {
        setcolor(i);
        line(cx + i * 7, cy + 4, cx + i * 7, cy + IN_H - 4);
    }
    check("setcolor(14) 后 getpixel 读到 14", (setcolor(14), getpixel(cx + 14 * 7, cy + 20)) == 14);
    cell_label(16, "setcolor(0..15)");

    /* ⑱ setfillstyle */
    cx = in_x(17); cy = in_y(17);
    setfillstyle(SOLID_FILL, BLUE);
    bar(cx, cy + 6, cx + 24, cy + IN_H - 6);
    setfillstyle(SOLID_FILL, RED);
    bar(cx + 28, cy + 6, cx + 52, cy + IN_H - 6);
    setfillstyle(SOLID_FILL, GREEN);
    bar(cx + 56, cy + 6, cx + 80, cy + IN_H - 6);
    setfillstyle(SOLID_FILL, MAGENTA);
    bar(cx + 84, cy + 6, cx + IN_W - 2, cy + IN_H - 6);
    check("setfillstyle 换色 ⇒ bar 跟着换",
          getpixel(cx + 12, cy + 20) == BLUE && getpixel(cx + 96, cy + 20) == MAGENTA);
    cell_label(17, "setfillstyle");

    /* ⑲ setlinestyle */
    cx = in_x(18); cy = in_y(18);
    setcolor(WHITE);
    setlinestyle(SOLID_LINE, 0, NORM_WIDTH);
    line(cx, cy + 12, cx + IN_W - 2, cy + 12);
    setlinestyle(SOLID_LINE, 0, THICK_WIDTH);
    line(cx, cy + 34, cx + IN_W - 2, cy + 34);
    setlinestyle(SOLID_LINE, 0, NORM_WIDTH);
    line(cx, cy + 58, cx + IN_W - 2, cy + 58);
    cell_label(18, "setlinestyle");

    /* ⑳ 文字三件套：outtextxy / outtext（跟着画笔）/ settextjustify */
    cx = in_x(19); cy = in_y(19);
    setcolor(WHITE);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 2);
    settextjustify(LEFT_TEXT, 0);
    outtextxy(cx, cy + 4, "outtextxy");
    moveto(cx, cy + 34);
    outtext("outtext");
    settextjustify(CENTER_TEXT, 0);
    outtextxy(cx + IN_W / 2, cy + 60, "CENTER");
    settextjustify(LEFT_TEXT, 0);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    cell_label(19, "outtextxy");

    /* ── 收尾 ────────────────────────────────────────────────── */
    puts("--- 颜色索引抽查 ---");
    say("getpixel(0,0)=", getpixel(0, 0));
    puts("--- 小结 ---");
    puts("OK=");
    put_int(g_ok);
    puts(" FAIL=");
    put_int(g_bad);
    putchar('\n');
    puts("=== BGI 自检结束 ===");

    ui_present();
    getch();
    closegraph();
    return 0;
}
