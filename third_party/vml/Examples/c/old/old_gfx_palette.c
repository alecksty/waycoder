/* old_gfx_palette.c —— 调色板与渐变（图形界面）
 * old_gfx_palette.c -- palette and gradients (graphical window)
 *
 * 类别：graphic
 * Category: graphic
 * 兼容面：`ui_brush_solid` / `ui_brush_named` / `ui_set_fill`（刷子）、
 * Compatibility: `ui_brush_solid` / `ui_brush_named` / `ui_set_fill` (brushes),
 *         `ui_rect_grad` / `ui_gradient`（渐变）、`ui_text`（三种锚点）
 *         `ui_rect_grad` / `ui_gradient` (gradients), `ui_text` (three anchor modes)
 *         —— 对应 BGI 的 `setfillstyle` + "自己算颜色插值"那套
 *         -- the counterpart of BGI's `setfillstyle` plus the "work out the color interpolation yourself" approach
 * 出处：自写，仿当年显示适配器测试图（一屏看全所有颜色）。
 * Origin: self-written, imitating the display-adapter test patterns of that era (see every color at once on one screen).
 */
#include <waycoder_ui.h>

#define W 320
#define H 240

int main(void)
{
    int i;
    int msg;

    ui_win_open("老式绘图：调色板/渐变", W, H);
    ui_clear(0x101820);

    /* ① 16 级灰阶 + RGB 三段渐变块（每块 20×30） */
    /* 1) 16 gray levels + three RGB gradient blocks (each 20x30) */
    for (i = 0; i < 16; i++) {
        int g = i * 255 / 15;
        int col = (g << 16) | (g << 8) | g;
        ui_rect(10 + i * 19, 10, 18, 30, col, 1, 0, 0);
    }

    /* ② 线性渐变：左红右蓝（渲染成纯色 = 渐变没生效） */
    /* 2) Linear gradient: red on the left, blue on the right (rendered as a solid color = the gradient did not take effect) */
    ui_rect_grad(10, 50, 140, 40, 1, 0xE06C75, 0x61AFEF, 0, 0);
    ui_rect_grad(170, 50, 140, 40, 1, 0x98C379, 0xE5C07B, 1, 0);   /* 竖向 */
                                                                   /* Vertical */

    /* ③ 用刷子填形状：径向渐变圆 */
    /* 3) Fill a shape with a brush: a radial gradient circle */
    ui_brush_radial("r1", 0xFFEEDD, 0x336699, 0, 0, 1);
    ui_set_fill(1);   /* 启用填充（1 = 用当前刷子） */
                      /* Enable filling (1 = use the current brush) */
    ui_circle(80, 150, 45, 0xFFFFFF, 1, 1);

    /* ④ 纯色刷子对照 */
    /* 4) A solid-color brush, for comparison */
    ui_brush_solid("s1", 0xC678DD);
    ui_rect(150, 110, 60, 80, 0xFFFFFF, 1, 1, 8);

    /* ⑤ 文字三种锚点：左/中/右 —— 三行左端应**不在同一列** */
    /* 5) Three text anchor modes: left / center / right -- the left ends of the three lines should **not be in the same column** */
    ui_text(160, 200, "居中", 0xFFFFFF, 20, 1);
    ui_text(20,  200, "左对齐", 0xFFFFFF, 20, 0);
    ui_text(300, 200, "右对齐", 0xFFFFFF, 20, 2);

    ui_present();
    while (!ui_win_closed())
        ui_wait(&msg, 0);          /* 画完等关窗，别立刻关（快照会没）*/
                                   /* Wait for the window to close after drawing; do not close right away (the snapshot would be lost) */
    ui_win_close();
    return 0;
}
