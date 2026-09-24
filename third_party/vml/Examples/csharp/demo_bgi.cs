// demo_bgi.cs —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
//
// 这一层与第 4 层（`ui_*`）的差别，是**两个年代的两套假设**：
//
//   · **固定分辨率**：开一次 `VGAHI` 就是 640×480，程序里到处写死坐标。
//   · **索引色**：`setcolor(4)` 说的是「红」，不是 RGB `0x000004`。全套 16 个色号。
//
// ## 实现在哪
//
// C/C++ 走的是 `Lib/c/graphics.h` —— 一份**头文件里带函数体**的 BGI 垫层
// （`#include <graphics.h>` 就能用）。**其它语言 include 不了 C 头文件**，
// 所以走另一份实现：**`Lib/shared/bgi.vml`**（由 `Lib/shared/src/bgi.c` 编译而来，
// **函数名与 C 版逐个相同**：`initgraph` / `setcolor` / `line` / `circle` / `outtextxy`…）。
// C# 调库函数**不用声明**（对不认识的函数名发裸标签 CALL），所以直接写调用即可。
//
// ## ⚠ 它需要把 `bgi.vml` 链进来，而**默认配置没链**
//
// 实测（2026-09-24）：直接跑报 `<input>:N: error: 未定义的函数 'initgraph'（引用 1 次）`。
// 原因：`vmltool.config.xml` 里 csharp（以及**所有**其它语言）的 `Libs` 只有 `vmlui.vml`，
// **没有任何语言的 Libs 挂了 `bgi.vml`** —— 整个仓库里它目前只被 Pascal 前端用
// `uses graph` 自动链上（`PascalCompiler.cs:358` 的 `AutoLinkUnit`）。
//
//   · 桌面跑法（本文件就是这么验的）：
//         vmlcli Examples/csharp/demo_bgi.cs --lib Lib/shared/bgi.vml --frames /tmp/fr
//   · 上手机：要在 `vmltool.config.xml` 给 csharp 那一行 `Libs=` 补 `bgi.vml`
//     （共享配置，按约定未代改 —— 见交付报告）。
//
// ## ⚠ `int[]` 传给出参语义的 `initgraph` 是读不回来的
//
// BGI 的 `initgraph(&gd, &gm, "")` 里 gd/gm 是**出参**。C# 没有取地址，只能传
// `int[]`；而 C# 的数组传给库里 `int*` 形参时**指针落在数据起点前 4 字节**上
// （实测 `callwithint8({1,11,22,…})`：C 侧返回 84197531、C# 侧返回 -6）。
// 所以宿主回填的驱动号/模式号我们读不到 —— 下面那两行**故意打 0**，
// 留着是为了让这个缺口可见（`getmaxx/getmaxy` 是 639/479，模式明明已经切过去了）。
//
// 跑法（桌面）：
//   vmlcli Examples/csharp/demo_bgi.cs --lib Lib/shared/bgi.vml --frames /tmp/fr

class DemoBgi
{
    // ── 常量（源：Lib/c/graphics.h；C# 的 `const int` 是好的，实测）──
    const int DETECT = 0;
    const int VGAHI = 2;

    const int BLACK = 0, BLUE = 1, GREEN = 2, CYAN = 3;
    const int RED = 4, MAGENTA = 5, BROWN = 6, LIGHTGRAY = 7;
    const int DARKGRAY = 8, LIGHTBLUE = 9, LIGHTGREEN = 10, LIGHTCYAN = 11;
    const int LIGHTRED = 12, LIGHTMAGENTA = 13, YELLOW = 14, WHITE = 15;

    const int SOLID_FILL = 1;
    const int SOLID_LINE = 0, NORM_WIDTH = 1, THICK_WIDTH = 3;
    const int LEFT_TEXT = 0, CENTER_TEXT = 1, RIGHT_TEXT = 2;
    const int HORIZ_DIR = 0, DEFAULT_FONT = 0;

    static void Main()
    {
        int i;
        int x;
        int y;
        int[] gd = new int[1];
        int[] gm = new int[1];

        gd[0] = DETECT;        // 0 = 让库自己挑；本平台挑出 VGA + VGAHI
        gm[0] = 0;

        // ── 1. 开场三行（老程序的标准姿势）──
        initgraph(gd, gm, "");

        // ── 2. 报一下这一层最要紧的两个数：分辨率是**固定的** ──
        println_str("BGI 已开：分辨率 640x480（VGAHI）");
        print_str("getmaxx() = "); println_int(getmaxx());   // 639
        print_str("getmaxy() = "); println_int(getmaxy());   // 479
        // ⚠ 这两行故意打 0（出参回填读不到，见文件头）
        print_str("驱动号 gd（C# 侧读不到，实测恒 0）= "); println_int(gd[0]);
        print_str("模式号 gm（同上）= ");                    println_int(gm[0]);

        // ── 3. 铺底（背景色也是索引色）──
        setbkcolor(BLACK);
        cleardevice();

        // ── 4. 标题（索引 14 = 黄；字号 3 = 3×16 像素）──
        setcolor(YELLOW);
        settextstyle(DEFAULT_FONT, HORIZ_DIR, 3);
        settextjustify(CENTER_TEXT, 0);
        outtextxy(getmaxx() / 2, 12, "BGI traditional graphics / demo_bgi.cs");
        settextstyle(DEFAULT_FONT, HORIZ_DIR, 1);
        settextjustify(LEFT_TEXT, 0);

        setcolor(LIGHTGRAY);
        outtextxy(20, 62, "fixed resolution 640x480, 16-color indexed palette");

        // ── 5. 16 色色带（这一层「索引色」最直观的一张图）──
        i = 0;
        while (i < 16)
        {
            setfillstyle(SOLID_FILL, i);
            bar(40 + i * 34, 92, 40 + i * 34 + 30, 126);
            i = i + 1;
        }
        setcolor(WHITE);
        rectangle(39, 91, 40 + 16 * 34 + 1, 127);

        setcolor(LIGHTGRAY);
        outtextxy(40, 134, "setfillstyle(SOLID_FILL, n) + bar()  ->  n = 0..15");
        outtextxy(40, 150, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray");
        outtextxy(40, 166, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white");

        // ── 6. 直线与折线（moveto + lineto / linerel 是老程序的常规姿势）──
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

        // ── 7. 矩形与两种"条" ──
        setcolor(YELLOW);
        rectangle(250, 205, 370, 265);
        setfillstyle(SOLID_FILL, LIGHTBLUE);
        bar(380, 205, 450, 265);
        setfillstyle(SOLID_FILL, GREEN);
        setcolor(WHITE);
        bar3d(465, 215, 545, 265, 14, 1);
        setcolor(LIGHTGRAY);
        outtextxy(250, 272, "rectangle / bar / bar3d");

        // ── 8. 圆与椭圆 ──
        setcolor(LIGHTMAGENTA);
        circle(80, 330, 40);
        circle(80, 330, 24);
        setfillstyle(SOLID_FILL, BROWN);
        fillellipse(200, 330, 56, 34);
        setcolor(LIGHTGRAY);
        ellipse(200, 330, 0, 360, 70, 44);
        outtextxy(40, 388, "circle / fillellipse / ellipse");

        // ── 9. 弧与扇形 ──
        setcolor(LIGHTRED);
        arc(330, 330, 30, 150, 40);
        setcolor(WHITE);
        setfillstyle(SOLID_FILL, YELLOW);
        pieslice(420, 330, 200, 340, 40);
        setcolor(LIGHTGRAY);
        outtextxy(290, 388, "arc (stroke) / pieslice (filled)");

        // ── 10. 像素与读回 ──
        //     棋盘格：两种索引色交替，肉眼一看就知道 putpixel 生效了
        y = 0;
        while (y < 6)
        {
            x = 0;
            while (x < 16)
            {
                if ((x + y) % 2 == 0) setcolor(WHITE);
                else                  setcolor(BLUE);
                putpixel(60 + x * 8, 410 + y * 8, getcolor());
                x = x + 1;
            }
            y = y + 1;
        }
        print_str("getpixel 读回 (60,410) = "); println_int(getpixel(60, 410));
        setcolor(LIGHTGRAY);
        outtextxy(40, 466, "putpixel / getpixel / getcolor");

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

        // ── 12. 线宽 ──
        setcolor(LIGHTBLUE);
        setlinestyle(SOLID_LINE, 0, NORM_WIDTH);
        line(40, 440, 280, 440);
        setlinestyle(SOLID_LINE, 0, THICK_WIDTH);
        line(40, 450, 280, 450);
        setlinestyle(SOLID_LINE, 0, NORM_WIDTH);
        outtextxy(40, 430, "setlinestyle(NORM_WIDTH / THICK_WIDTH)");

        // ── 13. 收尾（不调 getch() —— 图形窗口里按不了键，会挂到超时）──
        ui_present();
        closegraph();
        println_str("=== demo_bgi (C#) 结束 ===");
    }
}
