// demo_bgi.dart —— **第 3 层：传统图形接口**（BGI / Borland Graphics Interface）
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
// **函数名与 C 版逐个相同**）。本文件就是它的 Dart 绑定。
//
// ## ⚠⚠ 本份**当前跑不起来** —— 链不上 `bgi.vml`（两件事，都在这份例程之外）
//
//   ① **`Libs=` 里没有 `bgi.vml`** —— 这一条**仍然成立**，但只解释得了
//      **Java / C# / Swift** 那三份（它们要显式 `--lib Lib/shared/bgi.vml` 才跑）。
//      ⚠ **Dart 这两份不是这个原因** —— 见下。
//
//   ② **`--lib` 与 `Libs=` 对Dart（本前端）根本不生效** —— 真因**不是**「拿不到 `--lib`」：
//      `--lib` 的文件与语言 `Libs=` 的文件进的是同一个 `libraryPaths`
//      （`scripts/vmlcli/Program.cs`），而 `CompilerHelper.CompileFileStandard` 那次链接
//      **确实**带着这份清单（`LinkStandardLibrary(prog, langName, allLibPaths)`）。
//      真正卡住的是**更早的一步**：
//      `DartCompiler.cs:34` 在 `Compile` 里先自己调了一次
//      `CompilerHelper.LinkStandardLibrary(prog, "dart", null)` —— **库清单是 null**。
//      那一次只认 `builtins.vml`（自动）+ `SharedPrefixMap` 自动探测到的模块
//      （`ui_` → vmlui，所以 `demo_ui` 能跑），于是 `tty_*` / `initgraph` 当场被判成
//      「未定义的函数」**硬错误抛出**，后面那次「带着真库清单」的链接**根本没机会跑**。
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
// **修法（都动共享代码，本份例程没有代改）**：把 `bgi.vml` 加进 `vmltool.config.xml`
//   里 dart / kotlin 那两行 `Libs=`。
//
// ## 本文件的状态：**写法已验证、运行未验证**
//
//   · BGI 这一段调用与 `Examples/java/demo_bgi.java` / `Examples/csharp/demo_bgi.cs`
//     （两份都已跑通出图：640×480、16 色调色板、96~114 个不同颜色）**逐句同构**
//     ⇒ 库侧用法是对的。
//   · Dart 这边能编到**只剩 `bgi` 那批符号未定义**为止（没有别的语法/调用错误）。
//   ⚠ 它现在编不过，但**不是空壳** —— 配置补齐后应当直接可用。
//
// ## ⚠ 两条 Dart 前端的写法约束（见 demo_std.dart 的文件头）
//
//   · 调库函数**必须 `external` 声明**；
//   · **没有 `println`**、字符串拼接 `+` 与 `.toString()` 也不可用
//     ⇒ 标签与值分两次写（`print_str` + `println_int`）。
//
// ⚠ `initgraph` 的 gd/gm 按 BGI 语义是**出参**，Dart 里只能传 `List<int>`；
//   而 Dart 的数组传库函数时指针落在"长度头"上（见 demo_ui.dart 的文件头），
//   所以宿主回填的值读不到 —— 只当入参用。

external void initgraph(List<int> gd, List<int> gm, String path);
external void closegraph();
external void cleardevice();
external int  getmaxx();
external int  getmaxy();
external void setcolor(int c);
external void setbkcolor(int c);
external int  getcolor();
external void setfillstyle(int pattern, int color);
external void setlinestyle(int style, int pattern, int thickness);
external void settextstyle(int font, int dir, int size);
external void settextjustify(int horiz, int vert);
external void line(int x1, int y1, int x2, int y2);
external void moveto(int x, int y);
external void lineto(int x, int y);
external void linerel(int dx, int dy);
external void rectangle(int l, int t, int r, int b);
external void bar(int l, int t, int r, int b);
external void bar3d(int l, int t, int r, int b, int depth, int topflag);
external void circle(int x, int y, int r);
external void fillellipse(int x, int y, int xr, int yr);
external void ellipse(int x, int y, int st, int en, int xr, int yr);
external void arc(int x, int y, int st, int en, int r);
external void pieslice(int x, int y, int st, int en, int r);
external void putpixel(int x, int y, int color);
external int  getpixel(int x, int y);
external void outtextxy(int x, int y, String s);
external void ui_present();
external void print_str(String s);
external void print_int(int n);
external void println_int(int n);
external void println_str(String s);

void main() {
  // ⚠ 状态全部是 main 的局部变量（文件级可变状态在这几个前端上出过问题，
  //   见 demo_ui.dart 的文件头 ①）
  List<int> gd = [0];       // 0 = DETECT：让库自己挑
  List<int> gm = [0];
  int i = 0;
  int x = 0;
  int y = 0;

  initgraph(gd, gm, "");

  // ── 1. 这一层最要紧的两个数：分辨率是**固定的** ──
  println_str("BGI 已开：分辨率 640x480（VGAHI）");
  print_str("getmaxx() = "); println_int(getmaxx());   // 639
  print_str("getmaxy() = "); println_int(getmaxy());   // 479

  // ── 2. 铺底（背景色也是索引色）──
  setbkcolor(0);                                        // BLACK
  cleardevice();

  // ── 3. 标题（索引 14 = 黄；字号 3 = 3×16 像素）──
  setcolor(14);                                         // YELLOW
  settextstyle(0, 0, 3);                                // DEFAULT_FONT, HORIZ_DIR, size 3
  settextjustify(1, 0);                                 // CENTER_TEXT
  outtextxy(getmaxx() ~/ 2, 12, "BGI traditional graphics / demo_bgi.dart");
  settextstyle(0, 0, 1);
  settextjustify(0, 0);                                 // LEFT_TEXT

  setcolor(7);                                          // LIGHTGRAY
  outtextxy(20, 62, "fixed resolution 640x480, 16-color indexed palette");

  // ── 4. 16 色色带（这一层「索引色」最直观的一张图）──
  while (i < 16) {
    setfillstyle(1, i);                                 // SOLID_FILL, 色号 i
    bar(40 + i * 34, 92, 40 + i * 34 + 30, 126);
    i = i + 1;
  }
  setcolor(15);                                         // WHITE
  rectangle(39, 91, 40 + 16 * 34 + 1, 127);

  setcolor(7);
  outtextxy(40, 134, "setfillstyle(SOLID_FILL, n) + bar()  ->  n = 0..15");
  outtextxy(40, 150, "0 black  1 blue  2 green  3 cyan  4 red  5 magenta  6 brown  7 lightgray");
  outtextxy(40, 166, "8 darkgray  9 lightblue  10 lightgreen  11 lightcyan  12 lightred  13 lightmagenta  14 yellow  15 white");

  // ── 5. 直线与折线 ──
  setcolor(10);                                         // LIGHTGREEN
  line(40, 210, 200, 210);
  moveto(40, 230);
  lineto(120, 230);
  linerel(80, 0);

  setcolor(11);                                         // LIGHTCYAN
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
  setfillstyle(1, 9);                                   // SOLID_FILL, LIGHTBLUE
  bar(380, 205, 450, 265);
  setfillstyle(1, 2);                                   // SOLID_FILL, GREEN
  setcolor(15);
  bar3d(465, 215, 545, 265, 14, 1);
  setcolor(7);
  outtextxy(250, 272, "rectangle / bar / bar3d");

  // ── 7. 圆与椭圆 ──
  setcolor(13);                                         // LIGHTMAGENTA
  circle(80, 330, 40);
  circle(80, 330, 24);
  setfillstyle(1, 6);                                   // SOLID_FILL, BROWN
  fillellipse(200, 330, 56, 34);
  setcolor(7);
  ellipse(200, 330, 0, 360, 70, 44);
  outtextxy(40, 388, "circle / fillellipse / ellipse");

  // ── 8. 弧与扇形 ──
  setcolor(12);                                         // LIGHTRED
  arc(330, 330, 30, 150, 40);
  setcolor(15);
  setfillstyle(1, 14);                                  // SOLID_FILL, YELLOW
  pieslice(420, 330, 200, 340, 40);
  setcolor(7);
  outtextxy(290, 388, "arc (stroke) / pieslice (filled)");

  // ── 9. 像素与读回 ──
  while (y < 6) {
    x = 0;
    while (x < 16) {
      if ((x + y) % 2 == 0) { setcolor(15); }           // WHITE
      else                  { setcolor(1);  }           // BLUE
      putpixel(60 + x * 8, 410 + y * 8, getcolor());
      x = x + 1;
    }
    y = y + 1;
  }
  print_str("getpixel 读回 (60,410) = "); println_int(getpixel(60, 410));
  setcolor(7);
  outtextxy(40, 466, "putpixel / getpixel / getcolor");

  // ── 10. 三档对齐 ──
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
  setcolor(9);                                          // LIGHTBLUE
  setlinestyle(0, 0, 1);                                // SOLID_LINE, NORM_WIDTH
  line(40, 440, 280, 440);
  setlinestyle(0, 0, 3);                                // THICK_WIDTH
  line(40, 450, 280, 450);
  setlinestyle(0, 0, 1);
  outtextxy(40, 430, "setlinestyle(NORM_WIDTH / THICK_WIDTH)");

  // ── 12. 收尾（不调 getch() —— 图形窗口里按不了键，会挂到超时）──
  ui_present();
  closegraph();
  println_str("=== demo_bgi (Dart) 结束 ===");
}
