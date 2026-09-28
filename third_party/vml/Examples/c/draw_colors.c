/* draw_colors.c —— 绘图接口的**颜色体检**：每个能上色的调用各画一格，一屏看完。
 * draw_colors.c — a **color health check** for the drawing API: every color-capable call
 * paints one cell, so one screen shows them all.
 *
 * ## 为什么要有这个程序（它与 draw_prims.c 的分工）
 * ## Why this program exists (and how it splits the work with draw_prims.c)
 *
 * `draw_prims.c` 管的是**形状**（圆角是不是圆的、折线开不开口、路径挖不挖洞），
 * `draw_prims.c` covers **shapes** (is a rounded rect really round, is a polyline open,
 * does a path punch a hole),
 * 本程序只管**颜色**：同一条指令画出来的像素，是不是你给的那个色。
 * this program covers only **color**: is the pixel a call drew the color you asked for.
 *
 * 这两件事会**各自独立地坏**。v0.96.297 修的那条就只坏颜色——
 * The two break **independently**. The bug fixed in v0.96.297 broke color only —
 * 平台把渐变挂成 Android `Paint` 的 shader，而 **shader 优先级高于颜色**，
 * the platform attaches a gradient as an Android `Paint` shader, and **a shader outranks a color**,
 * 于是"这一帧里用过一个渐变"之后，后面所有纯色填充全被它接管
 * so once a gradient has been used in a frame, every later solid fill is taken over by it
 * （详细机制见 `WayCoder.Maui/Services/MauiVectorTarget.cs` 类注释「渐变的余荫」）。
 * (the full mechanism is in the class comment of `WayCoder.Maui/Services/MauiVectorTarget.cs`).
 * 症状是计算器的四档按键配色被**整体抹平**，而形状、文字、布局全对。
 * The symptom was the calculator's four key colors being **flattened into one**, while shapes, text and layout were all fine.
 *
 * ## 判据：**取像素，不靠眼睛**
 * ## The criterion: **read pixels, do not trust your eyes**
 *
 * 全部图形取**同一个颜色** `0xFF3C6EB4`（r/g/b 三个通道互不相同）——
 * every figure uses **the same color** `0xFF3C6EB4` (its r/g/b channels all differ) —
 * 通道序写反、被别的刷子接管、alpha 被吃掉，都会让它变成另一个色，一眼可辨。
 * a swapped channel order, another brush taking over, or a swallowed alpha all turn it into a different color, visible at a glance.
 *
 *     for k in 0..14: 取第 k 格**格心**的像素 == #3C6EB4
 *     for k in 0..14: read the pixel at the **center** of cell k == #3C6EB4
 *
 * 第 10 格是**渐变对照格**（本来就该是渐变，不参与断言），
 * cell 10 is the **gradient control cell** (it is meant to be a gradient, so it is not asserted),
 * 第 11~13 格是**回归格**：它们排在用过渐变之后，必须还是 `#3C6EB4`。
 * cells 11..13 are **regression cells**: they come after a gradient was used and must still be `#3C6EB4`.
 * 修 v0.96.297 那条之前，这三格画出来是**上一次那个渐变的颜色**。
 * Before the v0.96.297 fix, these three cells were painted with **the color of the previous gradient**.
 *
 * 用法（手机上）：`vml run examples/draw_colors.c`，然后截屏逐格比色。
 * Usage (on a phone): `vml run examples/draw_colors.c`, then screenshot and compare each cell.
 * ⚠ C 前端 + 汇编 + 链接在手机上要一分多钟，别当成卡死。
 * ⚠ The C frontend plus assembler plus linker takes over a minute on a phone; do not mistake it for a hang.
 *
 * ## 格子布局（3 列 × 5 行，格心取样）
 * ## Cell layout (3 columns x 5 rows, sampled at cell centers)
 *
 *     0 实心矩形      1 圆角矩形      2 实心圆
 *     0 solid rect    1 rounded rect  2 solid circle
 *     3 实心椭圆      4 实心多边形    5 粗折线
 *     3 solid ellipse 4 solid polygon 5 thick polyline
 *     6 粗直线        7 路径填充      8 路径描边
 *     6 thick line    7 path fill     8 path stroke
 *     9 空心矩形描边  10 线性渐变     11 渐变后的纯色矩形  ← 回归格
 *     9 stroked rect  10 linear grad  11 solid rect after grad  <- regression cell
 *    12 径向渐变后的圆 13 再后一格纯色 14 收尾纯色圆
 *    12 circle after radial grad 13 one more solid cell 14 closing solid circle
 *
 * （外加一格文字，文字用大号粗体、采样点落在笔画上。）
 * (Plus one text cell; the text is large and bold and the sample point lands on a stroke.)
 *
 * ## ⚠ 路径指令 `ui_path` 收的是**绝对场景坐标**
 * ## ⚠ The path command `ui_path` takes **absolute scene coordinates**
 *
 * 它没有"相对某个格子"的写法，所以本程序自己把格心坐标拼进 `d` 字符串里
 * It has no "relative to some cell" form, so this program builds cell-center coordinates into the `d` string itself
 * （本前端不认 `sprintf`，就手写了一个十几行的整数转字符串）。
 * (this frontend does not know `sprintf`, so a dozen-line integer-to-string routine is written by hand).
 * 写别的东西时也要留意：`ui_path` 的坐标**不跟着任何布局走**。
 * Keep this in mind when writing anything else: `ui_path` coordinates **do not follow any layout**.
 */
#include <waycoder_ui.h>

#define C 0xFF3C6EB4

int W;
int H;
int cw;
int ch;

int tri[6];

/* ── 小工具：拼字符串（本前端不认 sprintf） ──
 * ── Small helper: string building (this frontend does not know sprintf) ──
 *
 * ⚠ 写成"往全局缓冲里追加"而不是"返回一个 char*"：这条前端对**返回数组地址**
 * ⚠ Written as "append into a global buffer" rather than "return a char*": this frontend is
 *   没有把握（它认字符串字面量，`label_of` 那种），而**把具名数组当实参传**是
 *   not reliable about **returning an array address** (it knows string literals, via `label_of`),
 *   whereas **passing a named array as an argument** is
 *   全文件到处在用的稳妥写法 —— 拼好之后直接把数组交给 `ui_path` 就行。
 *   the safe pattern used all over this file — once built, just hand the array to `ui_path`.
 */
char pathBuf[96];
int pbN;

void pb_reset(void) { pbN = 0; }

void pb_put(char* s)
{
    int i = 0;
    while (s[i] != 0) { pathBuf[pbN] = s[i]; pbN = pbN + 1; i = i + 1; }
}

void pb_int(int v)
{
    char t[12];
    int j = 0;
    if (v < 0) { pb_put("-"); v = -v; }
    if (v == 0) { t[j] = '0'; j = j + 1; }
    while (v > 0 && j < 11) { t[j] = (char)('0' + v % 10); j = j + 1; v = v / 10; }
    while (j > 0) { j = j - 1; pathBuf[pbN] = t[j]; pbN = pbN + 1; }
}

/* 三角路径 "M x1 y1 L x2 y2 L x3 y3 Z"（格心 cx、底边 yb、半宽 hw、顶点高 tall）。 */
/* Triangle path "M x1 y1 L x2 y2 L x3 y3 Z" (center cx, base yb, half width hw, apex height tall). */
void pb_tri_path(int cx, int yb, int hw, int tall)
{
    pb_reset();
    pb_put("M "); pb_int(cx - hw); pb_put(" "); pb_int(yb);
    pb_put(" L "); pb_int(cx);     pb_put(" "); pb_int(yb - tall);
    pb_put(" L "); pb_int(cx + hw); pb_put(" "); pb_int(yb);
    pb_put(" Z");
    pathBuf[pbN] = 0;
}

int cxx(int col) { return col * cw + cw / 2; }
int cyy(int row) { return row * ch + ch / 2; }

int main(void)
{
    int m[4];

    W = ui_scr_w();
    H = ui_scr_h();
    cw = W / 3;
    ch = H / 5;

    ui_win_open_ex("颜色体检", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* 0 实心矩形 */
    /* 0 solid rectangle */
    ui_rect(cxx(0) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 1 圆角矩形 */
    /* 1 rounded rectangle */
    ui_rect(cxx(1) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 14);

    /* 2 实心圆 */
    /* 2 solid circle */
    ui_circle(cxx(2), cyy(0), ch / 3, C, 1, 0);

    /* 3 实心椭圆 */
    /* 3 solid ellipse */
    ui_ellipse(cxx(0), cyy(1), cw / 3, ch / 4, C, 1, 0);

    /* 4 实心多边形（三角，fill=C / 无描边） */
    /* 4 solid polygon (triangle, fill=C / no stroke) */
    tri[0] = cxx(1);      tri[1] = cyy(1) - ch / 3;
    tri[2] = cxx(1) - cw / 3; tri[3] = cyy(1) + ch / 3;
    tri[4] = cxx(1) + cw / 3; tri[5] = cyy(1) + ch / 3;
    ui_polygon(tri, 3, C, 0, 0, 0);

    /* 5 粗折线（开口；顶点与上面那个三角相同，所以采样取**边上**而不是格心） */
    /* 5 thick polyline (open; same vertices as the triangle above, so sample **on the edge**, not the center) */
    ui_polyline(tri, 3, C, 18, 0);

    /* 6 粗直线（横穿格子） */
    /* 6 thick line (across the cell) */
    ui_line(cxx(0) - cw / 3, cyy(2), cxx(0) + cw / 3, cyy(2), C, 24);

    /* 7 路径填充（只有填充，不描边）
       7 path fill (fill only, no stroke)
       ⚠ `ui_path` 的参数序是 **stroke, width, fill, grad, cap, dash** ——
       ⚠ the parameter order of `ui_path` is **stroke, width, fill, grad, cap, dash** —
          "描边色"排在"填充色"**前面**，与 `ui_rect` 那种"颜色在前、开关在后"不一样。
          the "stroke color" comes **before** the "fill color", unlike `ui_rect` where "color first, switches after".
          写反了不会报错，只是把"描边"当成了"填充"：本格想要空心轮廓，
          Swapping them raises no error, it just treats "stroke" as "fill": this cell wants a hollow outline,
          结果画出一个实心三角（第一次就是这么写错的）。
          but ends up drawing a solid triangle (which is exactly how it was first written wrong). */
    pb_tri_path(cxx(1), cyy(2) + ch / 3, cw / 3, ch * 2 / 3);
    ui_path(pathBuf, 0, 0, C, 0, 0, 0);

    /* 8 路径描边（粗，只有描边不填充 —— 必须是**空心轮廓**） */
    /* 8 path stroke (thick, stroke only with no fill — must be a **hollow outline**) */
    pb_tri_path(cxx(2), cyy(2) + ch / 3, cw / 3, ch * 2 / 3);
    ui_path(pathBuf, C, 20, 0, 0, 0, 0);

    /* 9 空心矩形（粗描边、填充透明） */
    /* 9 hollow rect (thick stroke, transparent fill) */
    ui_rect(cxx(0) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 0, 20, 0);

    /* 10 线性渐变（**对照格**：本来就该是渐变，不出现在断言里） */
    /* 10 linear gradient (**control cell**: meant to be a gradient, not part of the assertions) */
    ui_gradient("g1", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_rect_grad(cxx(1) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, "g1", 0);

    /* 11 **回归格**：用过渐变之后再画纯色 —— 必须还是 C */
    /* 11 **regression cell**: draw a solid color after a gradient was used — must still be C */
    ui_rect(cxx(2) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 12 **回归格**：径向渐变之后再画纯色圆 */
    /* 12 **regression cell**: draw a solid circle after a radial gradient */
    ui_gradient("g2", 1, 0xFF00FF00, 0xFF000000, 500, 500, 500, 0);
    ui_circle_grad(cxx(0), cyy(4), ch / 3, "g2");
    ui_circle(cxx(0), cyy(4) - ch / 5, ch / 5, C, 1, 0);

    /* 13 **回归格**：再往后一格 */
    /* 13 **regression cell**: one more cell later */
    ui_rect(cxx(1) - cw / 3, cyy(4) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 14 收尾纯色圆 */
    /* 14 closing solid circle */
    ui_circle(cxx(2), cyy(4), ch / 3, C, 1, 0);

    /* 15 文字（大号粗体；采样点落在笔画上） */
    /* 15 text (large bold; the sample point lands on a stroke) */
    ui_set_font(ch / 2, 1, C, 1);
    ui_text_cur(cxx(2), cyy(4) - ch / 6, "H");

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
