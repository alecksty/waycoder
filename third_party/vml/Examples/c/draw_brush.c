/* draw_brush.c —— **刷子模型**的端到端体检（`BRUSH` #575 / `SET_STYLE` #576 / `DRAW_SHAPE` #574）。
 * draw_brush.c — an end-to-end health check of the **brush model** (`BRUSH` #575 / `SET_STYLE` #576 / `DRAW_SHAPE` #574).
 *
 * 第 5 行单独验**渐变画笔**（v0.96.306）：画笔槽收渐变刷子之后，
 * Row 5 alone checks the **gradient pen** (v0.96.306): once the pen slot accepts a gradient brush,
 * "边框一个渐变、填充一个渐变"就成立了。
 * "one gradient for the border, one gradient for the fill" becomes possible.
 *
 * 与另外两个体检程序的分工：
 * How it splits the work with the other two health checks:
 *   draw_prims.c  管**形状**（圆角圆不圆、折线开不开口、路径挖不挖洞）
 *   draw_prims.c  covers **shapes** (is a rounded rect round, is a polyline open, does a path punch a hole)
 *   draw_colors.c 管**颜色**（同一条指令画出来的像素，是不是你给的那个色）
 *   draw_colors.c covers **color** (is the pixel a call drew the color you asked for)
 *   draw_brush.c  管**刷子**（样式与绘制分开之后，还能不能画对）—— 本程序
 *   draw_brush.c  covers **brushes** (can it still draw correctly once style is split from drawing) — this program
 *
 * ## 为什么值得单列
 * ## Why this deserves its own program
 *
 * 这一套与前两套的**调用形状完全不同**：老接口是「一条调用带齐颜色与开关」，
 * This set has a **completely different call shape** from the other two: the old API was "one call carrying color and switches together",
 * 新接口是「先设刷子、再画形状，绘制调用**不带颜色**」。样式成了**状态** ⇒
 * the new API is "set the brush first, then draw the shape, and the draw call **carries no color**". Style became **state** ⇒
 * 出错的形态也变了：**状态没设上**、**设错了槽**、**切形状时状态丢了**，
 * so the failure modes changed as well: **the state was never set**, **the wrong slot was set**, **the state was lost while switching shapes**,
 * 三种都表现为"画出来了，但不是你要的颜色"。
 * all three look like "it drew something, but not the color you wanted".
 *
 * ## 判据：逐格取格心像素
 * ## The criterion: read every cell at its center
 *
 * 每格用同一个颜色 `0xFF3C6EB4` 画，格心必须逐字节等于 `#3C6EB4`。
 * Every cell is drawn with the same color `0xFF3C6EB4`, and its center must be byte-for-byte `#3C6EB4`.
 * 另有两格专门验证**刷子状态**：
 * Two more cells specifically verify **brush state**:
 *   · 「线性渐变填充」格 —— 左端偏红、右端偏蓝才算渐变生效（不是纯色）；
 *   · the "linear gradient fill" cell — reddish at the left end and bluish at the right end means the gradient took effect (it is not a solid color);
 *   · 「换刷子」格 —— 改过填充刷子之后，**后面那格必须跟着变**（状态真的生效了）。
 *   · the "swap the brush" cell — after the fill brush is changed, **the next cell must change with it** (the state really took effect).
 *
 * 手机上：`vml run examples/c/draw_brush.c`  ⚠ C 前端 + 汇编 + 链接要一分多钟。
 * On a phone: `vml run examples/c/draw_brush.c`  ⚠ the C frontend plus assembler plus linker takes over a minute.
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
int gpen;

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
    ch = H / 6;   /* 6 行：第 5 行渐变画笔（v0.96.306），第 6 行渐变文字（v0.96.311） */
    /* 6 rows: row 5 gradient pen (v0.96.306), row 6 gradient text (v0.96.311) */

    ui_win_open_ex("刷子体检", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* 统一：填充 = 纯色刷子、不描边。**直接传颜色**也是合法的
       Uniform: fill = solid brush, no stroke. **Passing a color directly** is also legal
       （颜色 = 只有一个色标的刷子），这里走一遍 ui_brush_solid 验它。
       (a color = a brush with a single stop); this runs ui_brush_solid once to verify it. */
    g = ui_brush_solid(C);
    ui_set_fill(g);
    ui_set_pen(0, 0, 0, 0, 0);            /* 0 = 不描边 */
    /* 0 = no stroke */

    /* 0 矩形（样式来自状态） */
    /* 0 rectangle (style comes from state) */
    ui_draw_rect(cxx(0) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, 0);

    /* 1 圆角矩形 */
    /* 1 rounded rectangle */
    ui_draw_rect(cxx(1) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, 14);

    /* 2 圆 */
    /* 2 circle */
    ui_draw_circle(cxx(2), cyy(0), ch / 3);

    /* 3 椭圆 */
    /* 3 ellipse */
    ui_draw_ellipse(cxx(0), cyy(1), cw / 3, ch / 4);

    /* 4 多边形 */
    /* 4 polygon */
    tri[0] = cxx(1);          tri[1] = cyy(1) - ch / 4;
    tri[2] = cxx(1) - cw / 3; tri[3] = cyy(1) + ch / 4;
    tri[4] = cxx(1) + cw / 3; tri[5] = cyy(1) + ch / 4;
    ui_draw_poly(tri, 3, 1);

    /* 5 路径填充 */
    /* 5 path fill */
    ui_draw_path("M 210 130 L 270 130 L 270 190 L 210 190 Z");

    /* 6 星形 */
    /* 6 star */
    ui_draw_star(cxx(0), cyy(2), ch / 3, ch / 7, 5, 0);

    /* 7 正多边形 */
    /* 7 regular polygon */
    ui_draw_regular(cxx(1), cyy(2), ch / 3, 6, 0);

    /* 8 圆环（中间必须是空的） */
    /* 8 ring (its middle must be empty) */
    ui_draw_ring(cxx(2), cyy(2), ch / 3, ch / 6);

    /* 9 扇形 */
    /* 9 pie */
    ui_draw_pie(cxx(0), cyy(3), ch / 3, 0, 120);

    /* 10 心形 */
    /* 10 heart */
    ui_draw_heart(cxx(1), cyy(3), ch / 2);

    /* 11 椭圆渐变（唯一自带刷子的形状码） */
    /* 11 gradient ellipse (the one shape code that carries its own brush) */
    ui_gradient("eg", 0, RED, BLUE, 0, 0, 1000, 0);
    ui_ellipse_grad(cxx(2), cyy(3), cw / 3, ch / 4, "eg");

    /* ── 第 5 行：**渐变画笔**（v0.96.306「边框一个渐变」）──
       ── Row 5: the **gradient pen** (v0.96.306, "one gradient for the border") ──
       画笔槽与填充槽一样收刷子：这里造一个匿名线性渐变刷子给它。
       The pen slot accepts brushes just like the fill slot: an anonymous linear gradient brush is built for it here.
       判据看三格：12 只有渐变描边、13 填充纯色 + 边框渐变、14 直线渐变。
       The criterion looks at three cells: 12 gradient stroke only, 13 solid fill plus gradient border, 14 gradient line. */
    gpen = ui_brush_linear(RED, BLUE, 0, 0, 1000, 0);

    /* 12 圆：**只有**渐变描边、不填充 */
    /* 12 circle: **gradient stroke only**, no fill */
    ui_set_fill(0);
    ui_set_pen(gpen, 6, VML_CAP_BUTT, 0, VML_ARROW_NONE);
    ui_draw_circle(cxx(0), cyy(4), ch / 3);

    /* 13 矩形：填充纯色 + **边框渐变** —— "填充一个、边框另一个"的正面例子。
       13 rectangle: solid fill plus **gradient border** — a positive example of "one for the fill, another for the border".
       注意填充与描边是两个独立的槽，互不吃掉（解析侧曾经只有"第一个 @ = 填充"）。
       Note that fill and stroke are two independent slots that do not eat each other (parsing once assumed "the first @ = the fill"). */
    ui_set_fill(C);
    ui_set_pen(gpen, 8, VML_CAP_BUTT, 0, VML_ARROW_NONE);
    ui_draw_rect(cxx(1) - cw / 3, cyy(4) - ch / 3, cw * 2 / 3, ch * 2 / 3, 10);

    /* 14 上：直线渐变描边 */
    /* 14 above: gradient line stroke */
    ui_draw_line(cxx(2) - cw / 3, cyy(4), cxx(2) + cw / 3, cyy(4));

    /* 15 下：收尾换回**纯色画笔**，验证"改过刷子之后状态还生效" */
    /* 15 below: swap back to a **solid pen** at the end, verifying "state still takes effect after the brush is changed" */
    ui_set_pen(C, 2, VML_CAP_BUTT, 0, VML_ARROW_NONE);
    ui_draw_line(cxx(2) - cw / 3, cyy(4) + ch / 5, cxx(2) + cw / 3, cyy(4) + ch / 5);

    /* ── 第 6 行：**渐变文字**（v0.96.311）──
       ── Row 6: **gradient text** (v0.96.311) ──
       文字槽与填充/画笔一样收刷子；`ui_draw_text` 用当前文字刷子。
       The text slot accepts brushes like the fill and pen slots; `ui_draw_text` uses the current text brush.
       判据看颜色分布：纯色文字整行只有一个色相，渐变的**左端偏红、右端偏蓝**。
       The criterion looks at the color distribution: solid text has one hue across the whole line, while a gradient is **reddish at the left end and bluish at the right end**. */
    ui_set_text_brush(gpen);
    ui_set_font(ch / 3, VML_FONT_BOLD, 0, VML_ANCHOR_LEFT);
    ui_draw_text(cxx(0) - cw / 3, cyy(5) - ch / 4, "WWWWWW");

    /* 收尾：换回纯色文字刷子，验证"改过刷子之后状态还生效" */
    /* Wrap-up: swap back to a solid text brush, verifying "state still takes effect after the brush is changed" */
    ui_set_text_brush(RED);
    ui_set_font(ch / 4, 0, 0, VML_ANCHOR_LEFT);
    ui_draw_text(cxx(1) - cw / 3, cyy(5) - ch / 6, "RED");

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
