/* old_gfx_shapes.c —— 圆/椭圆/多边形（图形界面）
 * old_gfx_shapes.c -- circles / ellipses / polygons (graphical window)
 *
 * 类别：graphic
 * Category: graphic
 * 兼容面：`ui_circle`（填充与描边）/ `ui_ellipse` / `ui_polygon` / `ui_polyline`
 * Compatibility: `ui_circle` (fill and stroke) / `ui_ellipse` / `ui_polygon` / `ui_polyline`
 *         —— 对应 BGI 的 `circle`/`ellipse`/`fillpoly`/`drawpoly`
 *         -- the counterpart of BGI's `circle` / `ellipse` / `fillpoly` / `drawpoly`
 * 出处：自写。
 * Origin: self-written.
 *
 * ⚠ **顶点数组必须写成具名局部数组，不能用复合字面量** ——
 * ⚠ **The vertex array must be written as a named local array; a compound literal will not do** --
 *   `ui_polygon((int[]){…}, …)` 本前端**不支持、而且不报错**：算不出字面量的地址，
 *   `ui_polygon((int[]){...}, ...)` is **not supported by this front end, and it reports no error**: the address of the literal cannot be computed,
 *   会把上一个寄存器的值当成指针传下去 ⇒ 宿主越界读、多边形**静默不画**。
 *   so the value of the previous register is passed down as a pointer => the host reads out of bounds and the polygon is **silently not drawn**.
 *   （见 `Examples/c/draw_prims.c` 的说明与 CHANGELOG v0.96.182。）
 *   (See the notes in `Examples/c/draw_prims.c` and CHANGELOG v0.96.182.)
 */
#include <waycoder_ui.h>

#define W 320
#define H 240

int main(void)
{
    /* 具名数组 —— 见文件头的 ⚠ */
    /* Named arrays -- see the warning at the top of the file */
    int tri[6]  = { 160, 20,  60, 140,  260, 140 };
    int zig[14] = { 20, 200,  60, 170, 100, 200, 140, 170,
                   180, 200, 220, 170, 260, 200 };
    int quad[8] = { 230, 40,  300, 60,  280, 120,  220, 100 };
    int msg;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();
    ui_win_open(lang == 0 ? "老式绘图：圆/多边形" : "Old-style gfx: circles / polygons", W, H);
    ui_clear(0x101820);

    /* 圆：空心 / 实心 / 粗描边 */
    /* Circles: hollow / filled / thick stroke */
    ui_circle(60, 60, 40, 0xE06C75, 0, 2);
    ui_circle(160, 60, 40, 0x98C379, 1, 1);
    ui_circle(260, 60, 40, 0x61AFEF, 0, 6);

    /* 椭圆（不是正圆 —— 长短轴不同） */
    /* Ellipses (not perfect circles -- the two axes differ) */
    ui_ellipse(80, 130, 60, 30, 0xE5C07B, 0, 2);
    ui_ellipse(240, 130, 50, 25, 0xC678DD, 1, 1);

    /* 多边形：填充三角 + 描边四边形 */
    /* Polygons: a filled triangle + a stroked quadrilateral */
    ui_polygon(tri, 3, 0x56B6C2, 0xFFFFFF, 2, 0);
    ui_polygon(quad, 4, 0x000000, 0xD19A66, 3, 0);

    /* 折线：**必须开口**，不能连回起点 */
    /* Polyline: it **must stay open** -- it must not connect back to the start point */
    ui_polyline(zig, 7, 0xABB2BF, 2, 0);

    ui_present();
    /* 画完等关窗（同 old_gfx_lines：立刻关会让快照随窗口一起没掉）*/
    /* Wait for the window to close after drawing (same as old_gfx_lines: closing right away loses the snapshot with the window) */
    while (!ui_win_closed())
        ui_wait(&msg, 0);
    ui_win_close();
    return 0;
}
