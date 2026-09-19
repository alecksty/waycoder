/* draw_colors.c —— 绘图接口的**颜色体检**：每个能上色的调用各画一格，一屏看完。
 *
 * ## 为什么要有这个程序（它与 draw_prims.c 的分工）
 *
 * `draw_prims.c` 管的是**形状**（圆角是不是圆的、折线开不开口、路径挖不挖洞），
 * 本程序只管**颜色**：同一条指令画出来的像素，是不是你给的那个色。
 *
 * 这两件事会**各自独立地坏**。v0.96.297 修的那条就只坏颜色——
 * 平台把渐变挂成 Android `Paint` 的 shader，而 **shader 优先级高于颜色**，
 * 于是"这一帧里用过一个渐变"之后，后面所有纯色填充全被它接管
 * （详细机制见 `WayCoder.Maui/Services/MauiVectorTarget.cs` 类注释「渐变的余荫」）。
 * 症状是计算器的四档按键配色被**整体抹平**，而形状、文字、布局全对。
 *
 * ## 判据：**取像素，不靠眼睛**
 *
 * 全部图形取**同一个颜色** `0xFF3C6EB4`（r/g/b 三个通道互不相同）——
 * 通道序写反、被别的刷子接管、alpha 被吃掉，都会让它变成另一个色，一眼可辨。
 *
 *     for k in 0..14: 取第 k 格**格心**的像素 == #3C6EB4
 *
 * 第 10 格是**渐变对照格**（本来就该是渐变，不参与断言），
 * 第 11~13 格是**回归格**：它们排在用过渐变之后，必须还是 `#3C6EB4`。
 * 修 v0.96.297 那条之前，这三格画出来是**上一次那个渐变的颜色**。
 *
 * 用法（手机上）：`vml run examples/draw_colors.c`，然后截屏逐格比色。
 * ⚠ C 前端 + 汇编 + 链接在手机上要一分多钟，别当成卡死。
 *
 * ## 格子布局（3 列 × 5 行，格心取样）
 *
 *     0 实心矩形      1 圆角矩形      2 实心圆
 *     3 实心椭圆      4 实心多边形    5 粗折线
 *     6 粗直线        7 路径填充      8 路径描边
 *     9 空心矩形描边  10 线性渐变     11 渐变后的纯色矩形  ← 回归格
 *    12 径向渐变后的圆 13 再后一格纯色 14 收尾纯色圆
 *
 * （外加一格文字，文字用大号粗体、采样点落在笔画上。）
 *
 * ## ⚠ 路径指令 `ui_path` 收的是**绝对场景坐标**
 *
 * 它没有"相对某个格子"的写法，所以本程序自己把格心坐标拼进 `d` 字符串里
 * （本前端不认 `sprintf`，就手写了一个十几行的整数转字符串）。
 * 写别的东西时也要留意：`ui_path` 的坐标**不跟着任何布局走**。
 */
#include <waycoder_ui.h>

#define C 0xFF3C6EB4

int W;
int H;
int cw;
int ch;

int tri[6];

/* ── 小工具：拼字符串（本前端不认 sprintf） ──
 *
 * ⚠ 写成"往全局缓冲里追加"而不是"返回一个 char*"：这条前端对**返回数组地址**
 *   没有把握（它认字符串字面量，`label_of` 那种），而**把具名数组当实参传**是
 *   全文件到处在用的稳妥写法 —— 拼好之后直接把数组交给 `ui_path` 就行。
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
    ui_rect(cxx(0) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 1 圆角矩形 */
    ui_rect(cxx(1) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 14);

    /* 2 实心圆 */
    ui_circle(cxx(2), cyy(0), ch / 3, C, 1, 0);

    /* 3 实心椭圆 */
    ui_ellipse(cxx(0), cyy(1), cw / 3, ch / 4, C, 1, 0);

    /* 4 实心多边形（三角，fill=C / 无描边） */
    tri[0] = cxx(1);      tri[1] = cyy(1) - ch / 3;
    tri[2] = cxx(1) - cw / 3; tri[3] = cyy(1) + ch / 3;
    tri[4] = cxx(1) + cw / 3; tri[5] = cyy(1) + ch / 3;
    ui_polygon(tri, 3, C, 0, 0, 0);

    /* 5 粗折线（开口；顶点与上面那个三角相同，所以采样取**边上**而不是格心） */
    ui_polyline(tri, 3, C, 18, 0);

    /* 6 粗直线（横穿格子） */
    ui_line(cxx(0) - cw / 3, cyy(2), cxx(0) + cw / 3, cyy(2), C, 24);

    /* 7 路径填充 */
    pb_tri_path(cxx(1), cyy(2) + ch / 3, cw / 3, ch * 2 / 3);
    ui_path(pathBuf, C, 0, C, 0, 0, 0);

    /* 8 路径描边（粗） */
    pb_tri_path(cxx(2), cyy(2) + ch / 3, cw / 3, ch * 2 / 3);
    ui_path(pathBuf, 0, 20, C, 0, 0, 0);

    /* 9 空心矩形（粗描边、填充透明） */
    ui_rect(cxx(0) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 0, 20, 0);

    /* 10 线性渐变（**对照格**：本来就该是渐变，不出现在断言里） */
    ui_gradient("g1", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_rect_grad(cxx(1) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, "g1", 0);

    /* 11 **回归格**：用过渐变之后再画纯色 —— 必须还是 C */
    ui_rect(cxx(2) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 12 **回归格**：径向渐变之后再画纯色圆 */
    ui_gradient("g2", 1, 0xFF00FF00, 0xFF000000, 500, 500, 500, 0);
    ui_circle_grad(cxx(0), cyy(4), ch / 3, "g2");
    ui_circle(cxx(0), cyy(4) - ch / 5, ch / 5, C, 1, 0);

    /* 13 **回归格**：再往后一格 */
    ui_rect(cxx(1) - cw / 3, cyy(4) - ch / 3, cw * 2 / 3, ch * 2 / 3, C, 1, 0, 0);

    /* 14 收尾纯色圆 */
    ui_circle(cxx(2), cyy(4), ch / 3, C, 1, 0);

    /* 15 文字（大号粗体；采样点落在笔画上） */
    ui_set_font(ch / 2, 1, C, 1);
    ui_text_cur(cxx(2), cyy(4) - ch / 6, "H");

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
