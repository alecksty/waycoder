// demo_bgi.kt —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
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
// 本文件就是它的 Kotlin 调用。
//
// ## ⚠⚠ 本份**当前跑不起来** —— 链不上 `bgi.vml`（两件事，都在这份例程之外）
//
//   ① **`Libs=` 里没有 `bgi.vml`** —— 这一条**仍然成立**，但只解释得了
//      **Java / C# / Swift** 那三份（它们要显式 `--lib Lib/shared/bgi.vml` 才跑）。
//      ⚠ **Kotlin 这两份不是这个原因** —— 见下。
//
//   ② **`--lib` 与 `Libs=` 对Kotlin（本前端）根本不生效** —— 真因**不是**「拿不到 `--lib`」：
//      `--lib` 的文件与语言 `Libs=` 的文件进的是同一个 `libraryPaths`
//      （`scripts/vmlcli/Program.cs`），而 `CompilerHelper.CompileFileStandard` 那次链接
//      **确实**带着这份清单（`LinkStandardLibrary(prog, langName, allLibPaths)`）。
//      真正卡住的是**更早的一步**：
//      `KotlinCompiler.cs:35` 在 `Compile` 里先自己调了一次
//      `CompilerHelper.LinkStandardLibrary(prog, "kotlin", null)` —— **库清单是 null**。
//      那一次只认 `builtins.vml`（自动）+ `SharedPrefixMap` 自动探测到的模块
//      （`ui_` → vmlui，所以 `demo_ui` 能跑），于是 `tty_*` / `initgraph` 当场被判成
//      「未定义的函数」**硬错误抛出**，后面那次「带着真库清单」的链接**根本没机会跑**。
//      ⚠ **别用 C 风格 `external` 声明去绕**（`external int tty_init(int,int,int);`）：
//        Kotlin 解析器只认 `external` 这个关键字本身（`Parser.cs:32` 置 `_pendingExternal`），
//        声明剩下的 token 被 `else Advance()` 逐个丢弃、**标志留着**，于是**紧跟着的第一个
//        `fun`** 被当成外部函数编译（`CodeGenerator.cs:105` 的 `if (fn.IsExternal) continue;`
//        ⇒ 不产标签、丢函数体）。症状是该 `fun` 报「未定义的函数」；而**若被吃掉的正好是
//        `main`，程序会编译通过、运行「成功」、然后什么都不做**（静默空跑）。
//        最小复现：`external int tty_init(int w,int h,int c);` + `fun foo(): Int { return 42 }`
//        + `fun main(){ print(foo()) }` ⇒ `error: 未定义的函数 'foo'`。
//      ⇒ 对照：`JavaCompiler.cs:37` / `CSharpCompiler.cs:38` **不做**这次预链接
//        （注释写着「LinkStandardLibrary 由调用方统一处理」），所以同一份 `--lib` 给它们就有效。
//      最小复现（2026-09-24，两步都跑过）：
//
//          // K2.java（同形，对照用）
//          class K2 { static native int tty_init(int w,int h,int c);
//                     public static void main(String[] a){ tty_init(80,25,1); } }
//          $ vmlcli Examples/java/K2.java
//            → ✔ 编译完成（java，59338 条指令）      // 不需要 --lib
//
//      ⚠ 与「`LIB.vml` 有没有挂上」**无关**：`tty.vml` 自提交 9b5e1aa7 起已在 22 门语言的
//        `Libs=` 里（本条初版写成「只有 vmlui.vml」，已过期）。挂上了也一样编不过 ——
//        卡点在前面那次预链接上，**不动共享代码修不了**。
// **两种修法（都动共享代码，本份例程没有代改）**：
//   · 把 `bgi.vml` 加进 `vmltool.config.xml` 里 kotlin / dart 那两行 `Libs=`；
//   · 或者照 Pascal 的 `AutoLinkUnit`（`PascalCompiler.cs:358`）给这两个前端也做一层
//     "源码里声明了就用"的自动链接。
//
// ## 本文件的状态：**写法已验证、运行未验证**
//
//   · BGI 这一段 API 调用与 `Examples/java/demo_bgi.java` **逐字同构**，而 Java 那份
//     已经跑通并出图（640×480、16 色调色板、96 个不同颜色）⇒ 库侧的用法是对的。
//   · Kotlin 这边能编到**只剩 BGI 符号未定义**为止（下面这条命令的报错清单里
//     除 `initgraph`/`setcolor`/… 之外没有别的错误）⇒ 语法与其它调用都是好的：
//
//         vmlcli Examples/kotlin/demo_bgi.kt --lib Lib/shared/bgi.vml
//
//   ⚠ 也就是说：**它现在编不过，但它不是空壳** —— 配置补齐后应当直接可用。
//     在没有补配置之前，请不要把它当成"已验证能跑"的例程。

fun main() {
    // ⚠ `int gd = DETECT, gm; initgraph(&gd, &gm, "")` 在 Kotlin 里只能传**数组**
    //   （没有取地址）—— 所以 gd/gm 各一个长度 1 的数组，`gm` 按 BGI 语义是**出参**。
    //   ⚠ 而且数组传参在这几个非 C 前端上是**错的**（指针落在"长度头"上，见 demo_ui.kt），
    //     所以宿主回写的驱动号/模式号我们读不到 —— 只当**入参**用就好。
    var gd = arrayOf(0)          // 0 = DETECT：让库自己挑
    var gm = arrayOf(0)

    initgraph(gd, gm, "")

    // ── 1. 这一层最要紧的两个数：分辨率是**固定的** ──
    println("BGI 已开：分辨率 640x480（VGAHI）")
    print("getmaxx() = "); println(getmaxx())      // 639
    print("getmaxy() = "); println(getmaxy())      // 479

    // ── 2. 铺底（背景色也是索引色）──
    setbkcolor(0)                                 // BLACK
    cleardevice()

    // ── 3. 标题（索引 14 = 黄；字号 3 = 3×16 像素）──
    setcolor(14)                                  // YELLOW
    settextstyle(0, 0, 3)                         // DEFAULT_FONT, HORIZ_DIR, size 3
    settextjustify(1, 0)                          // CENTER_TEXT
    outtextxy(getmaxx() / 2, 12, "BGI traditional graphics / demo_bgi.kt")
    settextstyle(0, 0, 1)
    settextjustify(0, 0)                          // LEFT_TEXT

    setcolor(7)                                   // LIGHTGRAY
    outtextxy(20, 62, "fixed resolution 640x480, 16-color indexed palette")

    // ── 4. 16 色色带（这一层「索引色」最直观的一张图）──
    var i = 0
    while (i < 16) {
        setfillstyle(1, i)                        // SOLID_FILL, 色号 i
        bar(40 + i * 34, 92, 40 + i * 34 + 30, 126)
        i = i + 1
    }
    setcolor(15)                                  // WHITE
    rectangle(39, 91, 40 + 16 * 34 + 1, 127)

    setcolor(7)
    outtextxy(40, 134, "setfillstyle(SOLID_FILL, n) + bar()  ->  n = 0..15")
    outtextxy(40, 150, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray")
    outtextxy(40, 166, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white")

    // ── 5. 直线与折线（moveto + lineto / linerel 是老程序的常规姿势）──
    setcolor(10)                                  // LIGHTGREEN
    line(40, 210, 200, 210)
    moveto(40, 230)
    lineto(120, 230)
    linerel(80, 0)

    setcolor(11)                                  // LIGHTCYAN
    moveto(40, 250)
    linerel(40, -20)
    linerel(40, 20)
    linerel(40, -20)
    linerel(40, 20)

    setcolor(7)
    outtextxy(40, 262, "line / moveto / lineto / linerel (polyline is open)")

    // ── 6. 矩形与两种"条" ──
    setcolor(14)
    rectangle(250, 205, 370, 265)
    setfillstyle(1, 9)                            // SOLID_FILL, LIGHTBLUE
    bar(380, 205, 450, 265)
    setfillstyle(1, 2)                            // SOLID_FILL, GREEN
    setcolor(15)
    bar3d(465, 215, 545, 265, 14, 1)
    setcolor(7)
    outtextxy(250, 272, "rectangle / bar / bar3d")

    // ── 7. 圆与椭圆 ──
    setcolor(13)                                  // LIGHTMAGENTA
    circle(80, 330, 40)
    circle(80, 330, 24)
    setfillstyle(1, 6)                            // SOLID_FILL, BROWN
    fillellipse(200, 330, 56, 34)
    setcolor(7)
    ellipse(200, 330, 0, 360, 70, 44)
    outtextxy(40, 388, "circle / fillellipse / ellipse")

    // ── 8. 弧与扇形 ──
    setcolor(12)                                  // LIGHTRED
    arc(330, 330, 30, 150, 40)
    setcolor(15)
    setfillstyle(1, 14)                           // SOLID_FILL, YELLOW
    pieslice(420, 330, 200, 340, 40)
    setcolor(7)
    outtextxy(290, 388, "arc (stroke) / pieslice (filled)")

    // ── 9. 像素与读回（老程序靠它做"这个点是什么颜色"的判断）──
    var x = 0
    var y = 0
    while (y < 6) {
        x = 0
        while (x < 16) {
            if ((x + y) % 2 == 0) {
                setcolor(15)                      // WHITE
            } else {
                setcolor(1)                       // BLUE
            }
            putpixel(60 + x * 8, 410 + y * 8, getcolor())
            x = x + 1
        }
        y = y + 1
    }
    print("getpixel 读回 (60,410) = "); println(getpixel(60, 410))
    setcolor(7)
    outtextxy(40, 466, "putpixel / getpixel / getcolor")

    // ── 10. 三档对齐（老程序居中排版靠 settextjustify）──
    setcolor(15)
    settextstyle(0, 0, 2)
    settextjustify(0, 0)
    outtextxy(300, 405, "LEFT")
    settextjustify(1, 0)
    outtextxy(430, 405, "CENTER")
    settextjustify(2, 0)
    outtextxy(620, 405, "RIGHT")
    settextstyle(0, 0, 1)
    settextjustify(0, 0)
    outtextxy(300, 430, "settextstyle(size) + settextjustify(LEFT/CENTER/RIGHT)")

    // ── 11. 线宽 ──
    setcolor(9)                                   // LIGHTBLUE
    setlinestyle(0, 0, 1)                         // SOLID_LINE, NORM_WIDTH
    line(40, 440, 280, 440)
    setlinestyle(0, 0, 3)                         // THICK_WIDTH
    line(40, 450, 280, 450)
    setlinestyle(0, 0, 1)
    outtextxy(40, 430, "setlinestyle(NORM_WIDTH / THICK_WIDTH)")

    // ── 12. 收尾（不调 getch() —— 图形窗口里按不了键，会挂到超时）──
    ui_present()
    closegraph()
    println("=== demo_bgi (Kotlin) 结束 ===")
}
