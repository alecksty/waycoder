/* old_gfx_shapes.c —— 圆/椭圆/多边形（图形界面）
 *
 * 类别：graphic
 * 兼容面：`ui_circle`（填充与描边）/ `ui_ellipse` / `ui_polygon` / `ui_polyline`
 *         —— 对应 BGI 的 `circle`/`ellipse`/`fillpoly`/`drawpoly`
 * 出处：自写。
 *
 * ⚠ **顶点数组必须写成具名局部数组，不能用复合字面量** ——
 *   `ui_polygon((int[]){…}, …)` 本前端**不支持、而且不报错**：算不出字面量的地址，
 *   会把上一个寄存器的值当成指针传下去 ⇒ 宿主越界读、多边形**静默不画**。
 *   （见 `Examples/c/draw_prims.c` 的说明与 CHANGELOG v0.96.182。）
 */
#include <waycoder_ui.h>

#define W 320
#define H 240

int main(void)
{
    /* 具名数组 —— 见文件头的 ⚠ */
    int tri[6]  = { 160, 20,  60, 140,  260, 140 };
    int zig[14] = { 20, 200,  60, 170, 100, 200, 140, 170,
                   180, 200, 220, 170, 260, 200 };
    int quad[8] = { 230, 40,  300, 60,  280, 120,  220, 100 };
    int msg;

    ui_win_open("老式绘图：圆/多边形", W, H);
    ui_clear(0x101820);

    /* 圆：空心 / 实心 / 粗描边 */
    ui_circle(60, 60, 40, 0xE06C75, 0, 2);
    ui_circle(160, 60, 40, 0x98C379, 1, 1);
    ui_circle(260, 60, 40, 0x61AFEF, 0, 6);

    /* 椭圆（不是正圆 —— 长短轴不同） */
    ui_ellipse(80, 130, 60, 30, 0xE5C07B, 0, 2);
    ui_ellipse(240, 130, 50, 25, 0xC678DD, 1, 1);

    /* 多边形：填充三角 + 描边四边形 */
    ui_polygon(tri, 3, 0x56B6C2, 0xFFFFFF, 2, 0);
    ui_polygon(quad, 4, 0x000000, 0xD19A66, 3, 0);

    /* 折线：**必须开口**，不能连回起点 */
    ui_polyline(zig, 7, 0xABB2BF, 2, 0);

    ui_present();
    /* 画完等关窗（同 old_gfx_lines：立刻关会让快照随窗口一起没掉）*/
    while (!ui_win_closed())
        ui_wait(&msg, 0);
    ui_win_close();
    return 0;
}
