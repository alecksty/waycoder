/* draw_brush.c —— **刷子模型**的端到端体检（`BRUSH` #575 / `SET_STYLE` #576 / `DRAW_SHAPE` #574）。
 *
 * 与另外两个体检程序的分工：
 *   draw_prims.c  管**形状**（圆角圆不圆、折线开不开口、路径挖不挖洞）
 *   draw_colors.c 管**颜色**（同一条指令画出来的像素，是不是你给的那个色）
 *   draw_brush.c  管**刷子**（样式与绘制分开之后，还能不能画对）—— 本程序
 *
 * ## 为什么值得单列
 *
 * 这一套与前两套的**调用形状完全不同**：老接口是「一条调用带齐颜色与开关」，
 * 新接口是「先设刷子、再画形状，绘制调用**不带颜色**」。样式成了**状态** ⇒
 * 出错的形态也变了：**状态没设上**、**设错了槽**、**切形状时状态丢了**，
 * 三种都表现为"画出来了，但不是你要的颜色"。
 *
 * ## 判据：逐格取格心像素
 *
 * 每格用同一个颜色 `0xFF3C6EB4` 画，格心必须逐字节等于 `#3C6EB4`。
 * 另有两格专门验证**刷子状态**：
 *   · 「线性渐变填充」格 —— 左端偏红、右端偏蓝才算渐变生效（不是纯色）；
 *   · 「换刷子」格 —— 改过填充刷子之后，**后面那格必须跟着变**（状态真的生效了）。
 *
 * 手机上：`vml run examples/c/draw_brush.c`  ⚠ C 前端 + 汇编 + 链接要一分多钟。
 */
#include <waycoder_ui.h>

#define C 0xFF3C6EB4
#define RED 0xFFFF3020
#define BLUE 0xFF2050FF

int W;
int H;
int cw;
int ch;

int tri[6];

int cxx(int col) { return col * cw + cw / 2; }
int cyy(int row) { return row * ch + ch / 2; }

int main(void)
{
    int m[4];
    int i;
    int g;

    W = ui_scr_w();
    H = ui_scr_h();
    cw = W / 3;
    ch = H / 4;

    ui_win_open_ex("刷子体检", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* 统一：填充 = 纯色刷子、不描边。**直接传颜色**也是合法的
       （颜色 = 只有一个色标的刷子），这里走一遍 ui_brush_solid 验它。 */
    g = ui_brush_solid(C);
    ui_set_fill(g);
    ui_set_pen(0, 0, 0, 0, 0);            /* 0 = 不描边 */

    /* 0 矩形（样式来自状态） */
    ui_draw_rect(cxx(0) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, 0);

    /* 1 圆角矩形 */
    ui_draw_rect(cxx(1) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, 14);

    /* 2 圆 */
    ui_draw_circle(cxx(2), cyy(0), ch / 3);

    /* 3 椭圆 */
    ui_draw_ellipse(cxx(0), cyy(1), cw / 3, ch / 4);

    /* 4 多边形 */
    tri[0] = cxx(1);          tri[1] = cyy(1) - ch / 4;
    tri[2] = cxx(1) - cw / 3; tri[3] = cyy(1) + ch / 4;
    tri[4] = cxx(1) + cw / 3; tri[5] = cyy(1) + ch / 4;
    ui_draw_poly(tri, 3, 1);

    /* 5 路径填充 */
    ui_draw_path("M 210 130 L 270 130 L 270 190 L 210 190 Z");

    /* 6 星形 */
    ui_draw_star(cxx(0), cyy(2), ch / 3, ch / 7, 5, 0);

    /* 7 正多边形 */
    ui_draw_regular(cxx(1), cyy(2), ch / 3, 6, 0);

    /* 8 圆环（中间必须是空的） */
    ui_draw_ring(cxx(2), cyy(2), ch / 3, ch / 6);

    /* 9 扇形 */
    ui_draw_pie(cxx(0), cyy(3), ch / 3, 0, 120);

    /* 10 心形 */
    ui_draw_heart(cxx(1), cyy(3), ch / 2);

    /* 11 椭圆渐变（唯一自带刷子的形状码） */
    ui_gradient("eg", 0, RED, BLUE, 0, 0, 1000, 0);
    ui_ellipse_grad(cxx(2), cyy(3), cw / 3, ch / 4, "eg");

    /* 12 收尾：换回纯色，验证"改过刷子之后状态还生效" —— 必须在最下面一行之外画 */
    ui_set_fill(C);
    ui_draw_circle(cxx(0), cyy(3) + ch / 2 - 6, 8);

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
