// demo_bgi.cpp —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
//
// 与 `Examples/c/demo_bgi.c` 是**同一个画面、同一套接口**，用 C++ 前端再编一遍。
// C 与 C++ 是两个独立前端，同一份源码两边行为不保证一致，所以两份都要有
// （`test_bgi.c` / `test_bgi.cpp` 一直是**逐字节相同**的两份，就是这个理由）。
//
// ## 这一层的两个特点
//
//   · **固定分辨率**：开一次 `VGAHI` 就是 640×480，程序里到处写死坐标。
//     宿主负责把这块固定画布摆到真实屏幕上（本平台走 `ui_win_open_pc`，不随旋转重排）。
//   · **索引色**：`setcolor(4)` 说的是「红」，不是 RGB `0x000004`。全套 16 个色号。
//
// `Lib/c/graphics.h` 把 BGI 的**函数名与语义**原样接过来，落笔换成平台的 `ui_*`。
// C++ 的 include 路径包含 `Lib/c`，所以 `#include <graphics.h>` 两边是**同一份**
// （C++ 没有自己的 BGI 头，也不需要 —— 这一层就是 C 的接口）。
//
// ## ⚠ 不调 `getch()` 收尾
//
// 老程序常见的结尾是 `getch(); closegraph();`。`conio` 的 `getch()` 在本平台是"按行"的，
// 图形窗口里按不了键 —— 调它会把程序挂在 `--timeout` 上（实测 30 秒）才结束。
// 所以画完直接 `closegraph()`，正常返回。
//
// 跑法：命令行页输入  vml run examples/cpp/demo_bgi.cpp
// 出图：vmlcli Examples/cpp/demo_bgi.cpp --frames /tmp/fr

#include <graphics.h>

#define VBAR_X 40
#define VBAR_Y 92

int main()
{
    int gd = DETECT;
    int gm = 0;
    int i;
    int x, y;
    int tri[8];

    // ── 1. 开场三行（DETECT ⇒ 库自己挑，本平台挑出 VGA + VGAHI 640×480）──
    initgraph(&gd, &gm, "");

    // ── 2. 铺底：`cleardevice` 整屏刷成当前背景色 ──
    setbkcolor(BLACK);
    cleardevice();

    // ── 3. 标题（索引 14 = 黄；字号 3 = 3×16 像素）──
    setcolor(YELLOW);
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 3);
    // ⚠ 竖档必须写 `TOP_TEXT`（**盒顶**落在 y），**不能写 0** —— 0 是 `BOTTOM_TEXT`
    //   （盒**底**落在 y）。字号 3 的盒高是 48px，写 0 的话整块标题被顶到屏幕外、
    //   只剩底部十来行可见（`outtextxy` 的 y 是**盒顶**，这是 BGI 的缺省对齐）。
    settextjustify(CENTER_TEXT, TOP_TEXT);
    outtextxy(getmaxx() / 2, 12, "BGI 传统图形接口 / demo_bgi.cpp");
    settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
    // `0` = `BOTTOM_TEXT`：正文行按"盒底贴着 y"排。
    // ⚠ **别改成 `TOP_TEXT`** —— 那会把下面每一行整体再下移约 13px，底部会被顶出画布。
    settextjustify(LEFT_TEXT, 0);

    // y 取 74（不是 62）：上面那行标题按 TOP 排时盒子占到 y=60，得给它让出位置
    setcolor(LIGHTGRAY);
    outtextxy(20, 74, "fixed resolution 640x480, 16-color indexed palette (setcolor 0..15)");

    // ── 4. 16 色色带（这一层「索引色」最直观的一张图）──
    for (i = 0; i < 16; i++) {
        setfillstyle(SOLID_FILL, i);
        bar(VBAR_X + i * 34, VBAR_Y, VBAR_X + i * 34 + 30, VBAR_Y + 34);
    }
    setcolor(WHITE);
    rectangle(VBAR_X - 1, VBAR_Y - 1, VBAR_X + 16 * 34 + 1, VBAR_Y + 35);
    setcolor(LIGHTGRAY);
    outtextxy(VBAR_X, VBAR_Y + 50, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray");
    outtextxy(VBAR_X, VBAR_Y + 66, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white");

    // ── 5. 直线 / 折线（moveto + lineto / linerel 是老程序的常规姿势）──
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
    outtextxy(40, 262, "line / moveto / lineto / linerel (polyline is open)");

    // ── 6. 矩形与两种"条" ──
    setcolor(YELLOW);
    rectangle(250, 205, 370, 265);
    setfillstyle(SOLID_FILL, LIGHTBLUE);
    bar(380, 205, 450, 265);
    setfillstyle(SOLID_FILL, GREEN);
    setcolor(WHITE);
    bar3d(465, 215, 545, 265, 14, 1);
    setcolor(LIGHTGRAY);
    outtextxy(250, 272, "rectangle / bar / bar3d");

    // ── 7. 圆与椭圆 ──
    setcolor(LIGHTMAGENTA);
    circle(80, 330, 40);
    circle(80, 330, 24);
    setfillstyle(SOLID_FILL, BROWN);
    fillellipse(200, 330, 56, 34);
    setcolor(LIGHTGRAY);
    ellipse(200, 330, 0, 360, 70, 44);
    outtextxy(40, 388, "circle / fillellipse / ellipse");

    // ── 8. 弧与扇形（只有"刷子接口"画得了这两样，见 graphics.h 的说明）──
    setcolor(LIGHTRED);
    arc(330, 330, 30, 150, 40);
    setcolor(WHITE);
    setfillstyle(SOLID_FILL, YELLOW);
    pieslice(420, 330, 200, 340, 40);
    setcolor(LIGHTGRAY);
    outtextxy(290, 388, "arc (stroke) / pieslice (filled)");

    // ── 9. 多边形（⚠ BGI 的顶点表**首点要重复一次**才闭合，老规矩）──
    tri[0] = 520;   tri[1] = 370;
    tri[2] = 560;   tri[3] = 300;
    tri[4] = 600;   tri[5] = 370;
    tri[6] = tri[0]; tri[7] = tri[1];
    setcolor(LIGHTGREEN);
    drawpoly(4, tri);
    setfillstyle(SOLID_FILL, LIGHTMAGENTA);
    setcolor(WHITE);
    fillpoly(4, tri);
    setcolor(LIGHTGRAY);
    outtextxy(520, 388, "drawpoly / fillpoly");

    // ── 10. 像素与读回 ──
    for (y = 0; y < 6; y++) {
        for (x = 0; x < 16; x++) {
            // 棋盘格：两种索引色交替，一眼看出 putpixel 生效了
            if ((x + y) % 2 == 0) {
                setcolor(WHITE);
            } else {
                setcolor(BLUE);
            }
            putpixel(60 + x * 8, 410 + y * 8, getcolor());
        }
    }
    setcolor(LIGHTGRAY);
    outtextxy(40, 466, "putpixel / getcolor");

    // ── 11. 三档对齐（老程序居中排版靠 settextjustify）──
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

    // ── 12. 收尾（不调 getch()，理由见文件头）──
    ui_present();
    closegraph();
    return 0;
}
