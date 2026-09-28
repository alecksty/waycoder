/* demo_bgi.c —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
 * demo_bgi.c -- **Layer 3: the classic graphics interface** (BGI / Borland Graphics Interface)
 *
 * 这一层与下面第 4 层（`ui_*`）的差别，是**两个年代的两套假设**：
 * What separates this layer from layer 4 (`ui_*`) below is two eras' two sets of assumptions:
 *
 *   · **固定分辨率**：程序开一个 `SCREEN 12` / `VGAHI` 就不再管屏幕大小了，
 *   · **Fixed resolution**: once a program opens `SCREEN 12` / `VGAHI` it stops caring about screen size,
 *     代码里到处是 `640`、`480` 这种写死的坐标。宿主负责把这块固定画布
 *     and the code is full of hard-coded coordinates like `640`, `480`. The host places that fixed canvas
 *     摆到真实屏幕上（本平台走的是 `ui_win_open_pc`，坐标系**不随旋转重排**）。
 *     onto the real screen (here through `ui_win_open_pc`, whose coordinate system is **not re-laid-out on rotation**).
 *   · **索引色**：`setcolor(4)` 说的是「红」，不是 RGB `0x000004`。
 *   · **Indexed color**: `setcolor(4)` means "red", not the RGB value `0x000004`.
 *     全套只有 16 个颜色号，老程序里 `RED`/`LIGHTGREEN`/`YELLOW` 这些名字满天飞。
 *     There are only 16 color numbers in all, and old programs are full of names like `RED`/`LIGHTGREEN`/`YELLOW`.
 *
 * `Lib/c/graphics.h` 把 BGI 的**函数名与语义**原样接过来，落笔换成平台的 `ui_*`。
 * `Lib/c/graphics.h` takes over BGI's **function names and semantics** as-is and swaps drawing to the platform's `ui_*`.
 * 所以这个 demo 就是一份**标准的老式图形程序**：开场 `initgraph`、画、`closegraph`。
 * So this demo is a **standard old-style graphics program**: open with `initgraph`, draw, `closegraph`.
 *
 * ## 覆盖了什么
 * ## What is covered
 *
 *   `initgraph(DETECT)` 取到 VGA 640×480 → 铺底 → 16 色色带 →
 *   `initgraph(DETECT)` yields VGA 640x480 -> fill the background -> a 16-color bar ->
 *   直线 / 折线 / 矩形 / 实心条 / 外框条 / 圆 / 实心椭圆 / 弧 / 扇形 / 多边形 →
 *   lines / polylines / rectangles / filled bars / 3D bars / circles / filled ellipses / arcs / pies / polygons ->
 *   三档字号 + 三种对齐的文字 → `closegraph`。
 *   three text sizes + three text alignments -> `closegraph`.
 *
 * ⚠ 本 demo **不调 `getch()`**（老程序常见的收尾）。`conio` 的 `getch()` 在本平台是
 * WARNING: this demo does **not** call `getch()` (the usual ending of old programs). Here `conio`'s `getch()` is
 *   "按行"取的（用户在输入框敲一行再提交），图形窗口里按不了键 —— 调它会把程序
 *   read "by line" (the user types a line into the input box and submits it), and the graphics window cannot take keys --
 *   挂在那儿等一个永远不来的输入。所以画完直接 `closegraph()` 正常结束。
 *   calling it would hang the program waiting for input that never arrives. So after drawing we call `closegraph()` and end normally.
 *   要交互请看 `demo_ui.c`（那一层有真正的消息循环）。
 *   For interaction see `demo_ui.c` (that layer has a real message loop).
 *
 * 跑法：命令行页输入  vml run examples/c/demo_bgi.c
 * Run: type in the command-line page  vml run examples/c/demo_bgi.c
 * 桌面出图：vmlcli Examples/c/demo_bgi.c --frame out.png
 * Desktop rendering: vmlcli Examples/c/demo_bgi.c --frame out.png
 */

#include <graphics.h>

#define VBAR_X   40          /* 调色板色带的起点
                              * start x of the palette color bar */
#define VBAR_Y   92

int main(void)
{
    int gd = DETECT;
    int gm = 0;
    int i;
    int x, y;
    int tri[8];

    /* ── 1. 开场：老程序的标准三行 ──
     * -- 1. Intro: the standard three lines of an old graphics program --
     * `DETECT` 让库自己挑驱动/模式；本平台挑出来是 VGA + VGAHI（640×480）。
     * `DETECT` lets the library pick the driver/mode; here it picks VGA + VGAHI (640x480). */
    initgraph(&gd, &gm, "");

    /* ── 2. 铺底：背景色是索引色，`cleardevice` 整屏刷成它 ──
     * -- 2. Background: the background color is an indexed color, and `cleardevice` floods the screen with it -- */
    setbkcolor(BLACK);
    cleardevice();

    /* ── 3. 标题（索引色 14 = 黄，字号 3 = 3×16 像素）──
     * -- 3. Title (indexed color 14 = yellow, text size 3 = 3x16 pixels) -- */
    setcolor(YELLOW);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 3);
    /* ⚠ 竖档必须写 `TOP_TEXT`（**盒顶**落在 y），**不能写 0** —— 0 是 `BOTTOM_TEXT`
     * WARNING: the vertical alignment must be `TOP_TEXT` (the **top** of the box lands on y), **never 0** --
     *   （盒**底**落在 y）。字号 3 的盒高是 48px，写 0 的话整块标题被顶到屏幕外、
     *   0 is `BOTTOM_TEXT` (the **bottom** of the box lands on y). At size 3 the box is 48px tall, so writing 0
     *   只剩底部十来行可见（`outtextxy` 的 y 是**盒顶**，这是 BGI 的缺省对齐）。
     *   pushes the whole title off the screen with only the bottom ten-odd rows visible (`outtextxy`'s y is the
     *   **top** of the box; that is BGI's default alignment). */
    settextjustify(CENTER_TEXT, TOP_TEXT);
    outtextxy(getmaxx() / 2, 12, "BGI 传统图形接口 / demo_bgi.c");
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    /* `0` = `BOTTOM_TEXT`：正文行按"盒底贴着 y"排。
     * `0` = `BOTTOM_TEXT`: body lines are laid out with the box bottom against y.
     * ⚠ **别改成 `TOP_TEXT`** —— 那会把下面每一行整体再下移约 0.8×字号（16px 字号 ≈ 13px），
     * WARNING: **do not change it to `TOP_TEXT`** -- that shifts every line below down by about 0.8x the font size
     *   底部的 `putpixel / getcolor` 会被顶出画布。
     * (16px font is about 13px) and pushes `putpixel / getcolor` at the bottom off the canvas. */
    settextjustify(LEFT_TEXT, 0);

    /* 一行说明：屏幕尺寸是**开窗时定死的**，程序照着它排的版。
     * One line of explanation: the screen size is **fixed when the window opens**, and the program lays out for it.
     * y 取 74（不是 62）：上面那行标题按 TOP 排时盒子占到 y=60，得给它让出位置。
     * y is 74 (not 62): the title line above occupies up to y=60 when laid out with TOP, so it needs the room. */
    setcolor(LIGHTGRAY);
    outtextxy(20, 74, "fixed resolution 640x480, 16-color indexed palette (setcolor(0..15))");

    /* ── 4. 16 色色带 —— 这一层「索引色」最直观的一张图 ──
     * -- 4. The 16-color bar -- the most direct picture of this layer's "indexed color" --
     * 每一格用 `setfillstyle(SOLID_FILL, i)` + `bar()`，色号就是 BGI 的索引。
     * Each cell uses `setfillstyle(SOLID_FILL, i)` + `bar()`; the color number is the BGI index. */
    for (i = 0; i < 16; i++) {
        setfillstyle(SOLID_FILL, i);
        bar(VBAR_X + i * 34, VBAR_Y, VBAR_X + i * 34 + 30, VBAR_Y + 34);
    }
    setcolor(WHITE);
    rectangle(VBAR_X - 1, VBAR_Y - 1, VBAR_X + 16 * 34 + 1, VBAR_Y + 35);
    setcolor(LIGHTGRAY);
    outtextxy(VBAR_X, VBAR_Y + 50, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray");
    outtextxy(VBAR_X, VBAR_Y + 66, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white");

    /* ── 5. 直线与折线（`moveto` + `lineto` / `linerel` 是老程序画的常规姿势）──
     * -- 5. Lines and polylines (`moveto` + `lineto` / `linerel` is how old programs usually draw) -- */
    setcolor(LIGHTGREEN);
    line(40, 210, 200, 210);
    moveto(40, 230);
    lineto(120, 230);
    linerel(80, 0);
    setcolor(LIGHTCYAN);
    moveto(40, 250);
    linerel(40, -20);
    linerel(40, 20);
    linerel(40, -20);
    linerel(40, 20);
    setcolor(LIGHTGRAY);
    outtextxy(40, 262, "line / moveto / lineto / linerel (折线开口)");

    /* ── 6. 矩形与两种"条"（`bar` 实心 / `bar3d` 立体）──
     * -- 6. Rectangles and two kinds of bar (`bar` solid / `bar3d` with depth) -- */
    setcolor(YELLOW);
    rectangle(250, 205, 370, 265);
    setfillstyle(SOLID_FILL, LIGHTBLUE);
    bar(380, 205, 450, 265);
    setfillstyle(SOLID_FILL, GREEN);
    setcolor(WHITE);
    bar3d(465, 215, 545, 265, 14, 1);
    setcolor(LIGHTGRAY);
    outtextxy(250, 272, "rectangle / bar / bar3d");

    /* ── 7. 圆与椭圆（索引色描边、`setfillstyle` 填充）──
     * -- 7. Circles and ellipses (stroked with indexed colors, filled by `setfillstyle`) -- */
    setcolor(LIGHTMAGENTA);
    circle(80, 330, 40);
    circle(80, 330, 24);
    setfillstyle(SOLID_FILL, BROWN);
    fillellipse(200, 330, 56, 34);
    setcolor(LIGHTGRAY);
    ellipse(200, 330, 0, 360, 70, 44);
    outtextxy(40, 388, "circle / fillellipse / ellipse");

    /* ── 8. 弧与扇形（这两样只有"刷子接口"画得了，见 graphics.h 的说明）──
     * -- 8. Arcs and pies (only the "brush interface" can draw these two; see the notes in graphics.h) -- */
    setcolor(LIGHTRED);
    arc(330, 330, 30, 150, 40);
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, YELLOW);
    pieslice(420, 330, 200, 340, 40);
    setcolor(LIGHTGRAY);
    outtextxy(290, 388, "arc (描边) / pieslice (填充)");

    /* ── 9. 多边形（⚠ BGI 的顶点表**首点要重复一次**才闭合，这是老规矩）──
     * -- 9. Polygons (WARNING: a BGI vertex list must **repeat the first point** to close; that is the old rule) -- */
    tri[0] = 520;  tri[1] = 370;
    tri[2] = 560;  tri[3] = 300;
    tri[4] = 600;  tri[5] = 370;
    tri[6] = tri[0]; tri[7] = tri[1];
    setcolor(LIGHTGREEN);
    drawpoly(4, tri);
    setfillstyle(SOLID_FILL, LIGHTMAGENTA);
    setcolor(WHITE);
    fillpoly(4, tri);
    setcolor(LIGHTGRAY);
    outtextxy(520, 388, "drawpoly / fillpoly");

    /* ── 10. 像素与读回（老程序靠它做"这个点是什么颜色"的判断）──
     * -- 10. Pixels and read-back (how old programs ask "what color is this point") -- */
    for (y = 0; y < 6; y++) {
        for (x = 0; x < 16; x++) {
            /* 棋盘格：两种索引色交替，肉眼一看就知道 putpixel 生效了
             * A checkerboard: two indexed colors alternate, so one glance shows putpixel took effect */
            setcolor((x + y) % 2 == 0 ? WHITE : BLUE);
            putpixel(60 + x * 8, 410 + y * 8, getcolor());
        }
    }
    setcolor(LIGHTGRAY);
    outtextxy(40, 466, "putpixel / getcolor");

    /* ── 11. 三档字号 + 三种对齐（老程序居中排版就靠 settextjustify）──
     * -- 11. Three text sizes + three alignments (old programs center text with settextjustify) -- */
    setcolor(WHITE);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 2);
    settextjustify(LEFT_TEXT, 0);
    outtextxy(300, 405, "LEFT");
    settextjustify(CENTER_TEXT, 0);
    outtextxy(430, 405, "CENTER");
    settextjustify(RIGHT_TEXT, 0);
    outtextxy(620, 405, "RIGHT");
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    settextjustify(LEFT_TEXT, 0);
    outtextxy(300, 430, "settextstyle(size) + settextjustify(LEFT/CENTER/RIGHT)");

    /* ── 12. 收尾 ──
     * -- 12. Wrap-up --
     * `closegraph` 关掉窗口。**不调 `getch()`**，理由见文件头。
     * `closegraph` closes the window. **No `getch()` call**; see the file header for why.
     *
     * 出图靠宿主：桌面是 `--frame out.png`，手机上是窗口自己贴。
     * The host does the rendering: `--frame out.png` on desktop, and the window blits itself on mobile. */
    ui_present();
    closegraph();

    return 0;
}
