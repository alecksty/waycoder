// demo_bgi.java —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
//
// 这一层与第 4 层（`ui_*`）的差别，是**两个年代的两套假设**：
//
//   · **固定分辨率**：开一次 `2` 就是 640×480，程序里到处写死坐标。
//   · **索引色**：`setcolor(4)` 说的是「红」，不是 RGB `0x000004`。全套 16 个色号。
//
// ## 实现在哪（这一条是这四门语言共有的坑，先读）
//
// C/C++ 走的是 `Lib/c/graphics.h` —— 那是一份**头文件里带函数体**的 BGI 垫层
// （`#include <graphics.h>` 就能用）。**其它语言 include 不了 C 头文件**，
// 所以走另一份实现：**`Lib/shared/bgi.vml`**（由 `Lib/shared/src/bgi.c` 编译而来，
// **函数名与 C 版逐个相同**：`initgraph` / `setcolor` / `line` / `circle` / `outtextxy`…）。
// 本文件就是它的 Java 绑定 + 一份标准的老式图形程序。
//
// ## ⚠ 它需要把 `bgi.vml` 链进来，而**默认配置没链**
//
// 实测（2026-09-24）：直接跑会报
//     <input>:N: error: 未定义的函数 'initgraph'（引用 1 次）…
// 原因：`vmltool.config.xml` 里 java 的 `Libs` 只有 `vmlui.vml`
//（`builtins.vml` 来自 `DefaultLibs`），**没有任何语言的 Libs 挂了 `bgi.vml`** ——
// 整个仓库里 `bgi.vml` 目前只被 Pascal 前端用 `uses graph` 自动链上
//（`PascalCompiler.cs:358` 的 `AutoLinkUnit`）。
//
// 两种跑法：
//   ① **桌面**：加 `--lib` 指到它（本文件就是这么验的）
//        vmlcli Examples/java/demo_bgi.java --lib Lib/shared/bgi.vml --frames /tmp/fr
//   ② **要能进包/上手机**：在 `vmltool.config.xml` 的 java（以及 kotlin/dart/csharp）
//      那几行 `Libs=` 后面补上 `bgi.vml` —— 一行的事，但那是个**共享配置**，
//      本份例程没有代改（见交付报告）。
//
// ## ⚠ 一个已知的 BGI 缺口
//
// `Lib/c/graphics.h` 的注释里写着 `textwidth`/`textheight` 是**问宿主注册的 `text`
// JSON 函数**算出来的；而 `Lib/shared/src/bgi.c` 那一份**有没有同源**本份没验
//（本 demo 不调这两个，避开了这个疑问）。真要用文字居中排版的程序，先量一下。
//
// 跑法（桌面）：
//   vmlcli Examples/java/demo_bgi.java --lib Lib/shared/bgi.vml --frames /tmp/fr

public class DemoBgi {

    // ── 生命周期 ──
    static native void initgraph(int[] gd, int[] gm, String path);
    static native void closegraph();
    static native void cleardevice();
    static native int  getmaxx();
    static native int  getmaxy();

    // ── 颜色（索引 0..15）与填充/线型 ──
    static native void setcolor(int c);
    static native void setbkcolor(int c);
    static native int  getcolor();
    static native void setfillstyle(int pattern, int color);
    static native void setlinestyle(int style, int pattern, int thickness);

    // ── 图元 ──
    static native void line(int x1, int y1, int x2, int y2);
    static native void moveto(int x, int y);
    static native void lineto(int x, int y);
    static native void linerel(int dx, int dy);
    static native void rectangle(int l, int t, int r, int b);
    static native void bar(int l, int t, int r, int b);
    static native void bar3d(int l, int t, int r, int b, int depth, int topflag);
    static native void circle(int x, int y, int r);
    static native void fillellipse(int x, int y, int xr, int yr);
    static native void ellipse(int x, int y, int st, int en, int xr, int yr);
    static native void arc(int x, int y, int st, int en, int r);
    static native void pieslice(int x, int y, int st, int en, int r);
    static native void putpixel(int x, int y, int color);
    static native int  getpixel(int x, int y);

    // ── 文字 ──
    static native void outtextxy(int x, int y, String s);
    static native void settextstyle(int font, int dir, int size);
    static native void settextjustify(int horiz, int vert);
    static native void ui_present();

    // ⚠ **这里一处常量都不定义** —— Java 前端的**类字段读出来是它的地址、不是它的值**
    //   （实测：`static int A = 9;` 读回 1024；`static final int B = 9;` 读回 1028；
    //    `static int C; C = 9;` 读回 1032。只有**局部变量**是对的）。
    //   所以下面的色号一律写成字面量，旁边的注释给出名字 —— 名字的唯一真源仍是
    //   `Lib/c/graphics.h` / `Lib/shared/src/bgi.c`。
    //
    //   ⚠ 这条代价不小：本文件原本用 `setcolor(14)` 这种写法，
    //     而 14 读出来是 1028 ⇒ bgi.c 里 `if (idx > 15) idx = 15` 会把它**钳成白**，
    //     整幅画面全是白的。改成字面量才对。

    public static void main(String[] args) {
        // ⚠ `int gd = 0, gm; initgraph(&gd, &gm, "")` 在 Java 里只能传**数组**
        //   （Java 没有取地址）—— 所以 gd/gm 各一个长度 1 的 int[]，
        //   `gm` 按 BGI 语义是**出参**（进来时的值会被覆盖成实际用的模式号）。
        int[] gd = new int[1];
        int[] gm = new int[1];
        gd[0] = 0;          // 0 = 让库自己挑；本平台挑出 VGA + 2
        gm[0] = 0;

        initgraph(gd, gm, "");

        // ── 1. 报一下这一层最要紧的两个数：分辨率是**固定的** ──
        System.out.println("BGI 已开：分辨率 640x480（2）");
        System.out.print("getmaxx() = "); System.out.println(getmaxx());   // 639
        System.out.print("getmaxy() = "); System.out.println(getmaxy());   // 479
        // ⚠ 这两行**故意打 0**：`initgraph` 的 gd/gm 按 BGI 语义是**出参**
        //   （`*gd = VGA`），而 Java 把 `int[]` 传给库的 `int*` 时指针落在"长度头"上
        //   （见 demo_ui.java 文件头那条），宿主写回来的值我们读不到。
        //   留着是为了**让这个缺口可见** —— 别以为是 initgraph 没生效：
        //   上面 getmaxx/getmaxy 是 639/479，模式明明已经切过去了。
        System.out.print("驱动号 gd（Java 侧读不到，实测恒 0）= "); System.out.println(gd[0]);
        System.out.print("模式号 gm（同上）= ");                      System.out.println(gm[0]);

        // ── 2. 铺底（背景色也是索引色）──
        setbkcolor(0);
        cleardevice();

        // ── 3. 标题（索引 14 = 黄；字号 3 = 3×16 像素）──
        setcolor(14);
        settextstyle(0, 0, 3);
        settextjustify(1, 0);
        outtextxy(getmaxx() / 2, 12, "BGI traditional graphics / demo_bgi.java");
        settextstyle(0, 0, 1);
        settextjustify(0, 0);

        setcolor(7);
        outtextxy(20, 62, "fixed resolution 640x480, 16-color indexed palette");

        // ── 4. 16 色色带（这一层「索引色」最直观的一张图）──
        int i = 0;
        while (i < 16) {
            setfillstyle(1, i);
            bar(40 + i * 34, 92, 40 + i * 34 + 30, 126);
            i = i + 1;
        }
        setcolor(15);
        rectangle(39, 91, 40 + 16 * 34 + 1, 127);

        setcolor(7);
        outtextxy(40, 134, "setfillstyle(1, n) + bar()  ->  n = 0..15");
        outtextxy(40, 150, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray");
        outtextxy(40, 166, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white");

        // ── 5. 直线与折线（moveto + lineto / linerel 是老程序的常规姿势）──
        setcolor(10);
        line(40, 210, 200, 210);
        moveto(40, 230);
        lineto(120, 230);
        linerel(80, 0);

        setcolor(11);
        moveto(40, 250);
        linerel(40, -20);
        linerel(40, 20);
        linerel(40, -20);
        linerel(40, 20);

        setcolor(7);
        outtextxy(40, 262, "line / moveto / lineto / linerel (polyline is open)");

        // ── 6. 矩形与两种"条" ──
        setcolor(14);
        rectangle(250, 205, 370, 265);
        setfillstyle(1, 9);
        bar(380, 205, 450, 265);
        setfillstyle(1, 2);
        setcolor(15);
        bar3d(465, 215, 545, 265, 14, 1);
        setcolor(7);
        outtextxy(250, 272, "rectangle / bar / bar3d");

        // ── 7. 圆与椭圆 ──
        setcolor(13);
        circle(80, 330, 40);
        circle(80, 330, 24);
        setfillstyle(1, 6);
        fillellipse(200, 330, 56, 34);
        setcolor(7);
        ellipse(200, 330, 0, 360, 70, 44);
        outtextxy(40, 388, "circle / fillellipse / ellipse");

        // ── 8. 弧与扇形 ──
        setcolor(12);
        arc(330, 330, 30, 150, 40);
        setcolor(15);
        setfillstyle(1, 14);
        pieslice(420, 330, 200, 340, 40);
        setcolor(7);
        outtextxy(290, 388, "arc (stroke) / pieslice (filled)");

        // ── 9. 像素与读回（老程序靠它做"这个点是什么颜色"的判断）──
        //    棋盘格：两种索引色交替，肉眼一看就知道 putpixel 生效了
        int x = 0;
        int y = 0;
        while (y < 6) {
            x = 0;
            while (x < 16) {
                if ((x + y) % 2 == 0) {
                    setcolor(15);
                } else {
                    setcolor(1);
                }
                putpixel(60 + x * 8, 410 + y * 8, getcolor());
                x = x + 1;
            }
            y = y + 1;
        }
        System.out.print("getpixel 读回 (60,410) = "); System.out.println(getpixel(60, 410));
        setcolor(7);
        outtextxy(40, 466, "putpixel / getpixel / getcolor");

        // ── 10. 三档对齐（老程序居中排版靠 settextjustify）──
        setcolor(15);
        settextstyle(0, 0, 2);
        settextjustify(0, 0);
        outtextxy(300, 405, "LEFT");
        settextjustify(1, 0);
        outtextxy(430, 405, "CENTER");
        settextjustify(2, 0);
        outtextxy(620, 405, "RIGHT");
        settextstyle(0, 0, 1);
        settextjustify(0, 0);
        outtextxy(300, 430, "settextstyle(size) + settextjustify(LEFT/CENTER/RIGHT)");

        // ── 11. 线宽 ──
        setcolor(9);
        setlinestyle(0, 0, 1);
        line(40, 440, 280, 440);
        setlinestyle(0, 0, 3);
        line(40, 450, 280, 450);
        setlinestyle(0, 0, 1);
        outtextxy(40, 430, "setlinestyle(1 / 3)");

        // ── 12. 收尾（不调 getch() —— 图形窗口里按不了键，会挂到超时）──
        ui_present();
        closegraph();
        System.out.println("=== demo_bgi (Java) 结束 ===");
    }
}
