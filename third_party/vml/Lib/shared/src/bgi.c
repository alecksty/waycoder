/* bgi.c —— BGI 兼容层的**跨语言转发模块**（只有转发，没有算法）
 *
 * ## 它解决的是什么
 *
 * `Lib/c/graphics.h` 是一份**纯头文件**的 BGI 实现，45 个函数、全部落在 `ui_*` 宿主图元上，
 * 真机验过（`Examples/c/test_bgi.c` 与 `Examples/cpp/test_bgi.cpp`，逐格体检）。
 * 但它是**头文件**：只有能吃 `#include` 的 C / C++ / ObjC 用得上。
 *
 * Pascal 的老程序 `uses Graph` 拿不到它 —— Pascal 前端不能 include C 头文件，
 * 它只能**按标签调用**库里的函数。于是那份实现对 Pascal（以及其余 19 个前端）等于不存在。
 *
 * 本文件就是那座桥：`#include <graphics.h>` 把头文件里的实现**收进一个 C 模块**，
 * 再给每个 BGI 门面配一个**同名的真函数**（薄薄一层转发）。编出 `bgi.vml` 之后，
 * 任何前端都能按标签调它。
 *
 * ## 为什么一个门面要写两遍（`InitGraph` 与 `initgraph`）
 *
 * `graphics.h` 里**大写的那些是宏**（`initgraph`/`initwindow`），宏存在的唯一理由是让
 * `__FILE__` 在**调用点**展开、好拿源文件名当窗口标题。宏传不到别的语言里，
 * 所以下面先 `#undef` 再给一对真函数（大写是 Pascal/BGI 文档的写法，
 * 小写是 C 的写法）。其余 39 个门面 `graphics.h` 本来就是真函数，
 * **小写那份在 `graphics.h` 里已经有了**，这里只补大写。
 *
 * ## 一处实现原则
 *
 * 本文件里**不许出现任何绘图算法**：换算（BGI 矩形右下角 → `ui_rect` 的宽高）、
 * 调色板、文字量测、扇形刷子复位……全都在 `graphics.h` 里。这里只做两件事：
 * 换个大小写的名字、把参数原样递过去。**抄一份算法过来就是第二份实现**，
 * 而两份实现漂移的症状是"Pascal 画出来和 C 不一样"，最难查。
 *
 * ## 名字的来路
 *
 * 大写门面的清单不是凭空写的，它照 `Lib/pascal/graph.pas`（Pascal 侧的 interface 声明）
 * 与 `vmltool.config.xml` 里 pascal 的既有约定，也沿用老 `Lib/shared/src/graph.c`
 * 的那套 Pascal 式命名（`InitGraph`/`GetMaxX`/`SetColor`…）——
 * 老程序源码里就是这么大小写的，而 VML 的标签表**区分大小写**。
 */
#include <graphics.h>

/* 撤掉那两个宏，好给它们真函数（见文件头「为什么一个门面要写两遍」）。 */
#undef initgraph
#undef initwindow

/* ── 生命周期 ──────────────────────────────────────────────── */

/* `initgraph(&gd, &gm, path)`：`gd`/`gm` 是**入参+出参**，
 * 所以这里必须把调用方给的地址原样递下去（老程序靠它回读实际模式）。 */
void InitGraph(int *driver, int *mode, char *path) { _bgi_initgraph(driver, mode, path, ""); }
void initgraph(int *driver, int *mode, char *path) { _bgi_initgraph(driver, mode, path, ""); }

/* `file` 那一路（拿源文件名当窗口标题）只有 C 的宏能提供；跨语言时给空串。 */
void InitWindow(int w, int h, char *title) { _bgi_initwindow(w, h, title, ""); }
void initwindow(int w, int h, char *title) { _bgi_initwindow(w, h, title, ""); }

void SetGraphMode(int mode) { setgraphmode(mode); }
void CloseGraph(void)       { closegraph(); }
void ClearDevice(void)      { cleardevice(); }
int  GetMaxX(void)          { return getmaxx(); }
int  GetMaxY(void)          { return getmaxy(); }
int  GetMaxColor(void)      { return getmaxcolor(); }

/* `graphresult`/`grapherrormsg`：老程序的**标准开场**（开窗 → 看错误码 → 不对就 Halt）。
 * 语料里 21 份程序调用它们，不做的话每一份都在链接期"未解析标签"。 */
int  GraphResult(void)              { return graphresult(); }
char *GraphErrorMsg(int code)       { return grapherrormsg(code); }

/* ── 设置 ──────────────────────────────────────────────────── */

void SetColor(int c)             { setcolor(c); }
void SetBkColor(int c)           { setbkcolor(c); }
int  GetColor(void)              { return getcolor(); }
int  GetBkColor(void)            { return getbkcolor(); }
void SetFillStyle(int p, int c)  { setfillstyle(p, c); }
void SetLineStyle(int s, int p, int t) { setlinestyle(s, p, t); }
void SetTextStyle(int f, int d, int s) { settextstyle(f, d, s); }
void SetTextJustify(int h, int v)      { settextjustify(h, v); }

/* ── 画图 ──────────────────────────────────────────────────── */

void Line(int x1, int y1, int x2, int y2) { line(x1, y1, x2, y2); }
void MoveTo(int x, int y)                 { moveto(x, y); }
void LineTo(int x, int y)                 { lineto(x, y); }
void LineRel(int dx, int dy)              { linerel(dx, dy); }
void Rectangle(int l, int t, int r, int b) { rectangle(l, t, r, b); }
void Bar(int l, int t, int r, int b)      { bar(l, t, r, b); }
void Bar3D(int l, int t, int r, int b, int depth, int topflag) { bar3d(l, t, r, b, depth, topflag); }
void Circle(int x, int y, int r)          { circle(x, y, r); }
void FillEllipse(int x, int y, int xr, int yr) { fillellipse(x, y, xr, yr); }
void Ellipse(int x, int y, int st, int en, int xr, int yr) { ellipse(x, y, st, en, xr, yr); }
void DrawPoly(int numpoints, int *polypoints) { drawpoly(numpoints, polypoints); }
void FillPoly(int numpoints, int *polypoints) { fillpoly(numpoints, polypoints); }
void PutPixel(int x, int y, int color)    { putpixel(x, y, color); }

void Arc(int x, int y, int st, int en, int r)   { arc(x, y, st, en, r); }
void PieSlice(int x, int y, int st, int en, int r) { pieslice(x, y, st, en, r); }
void Sector(int x, int y, int st, int en, int xr, int yr) { sector(x, y, st, en, xr, yr); }

void FloodFill(int x, int y, int border)  { floodfill(x, y, border); }

int  ImageSize(int l, int t, int r, int b) { return imagesize(l, t, r, b); }
void GetImage(int l, int t, int r, int b, void *p) { getimage(l, t, r, b, p); }
void PutImage(int l, int t, void *p, int op)       { putimage(l, t, p, op); }

/* ── 文字 ──────────────────────────────────────────────────── */

void OutTextXY(int x, int y, char *s) { outtextxy(x, y, s); }
void OutText(char *s)                 { outtext(s); }
int  TextWidth(char *s)               { return textwidth(s); }
int  TextHeight(char *s)              { return textheight(s); }

/* ── 像素读回 ──────────────────────────────────────────────── */

int GetPixel(int x, int y) { return getpixel(x, y); }
