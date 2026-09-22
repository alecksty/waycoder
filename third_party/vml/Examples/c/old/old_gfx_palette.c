/* old_gfx_palette.c —— 调色板与渐变（图形界面）
 *
 * 类别：graphic
 * 兼容面：`ui_brush_solid` / `ui_brush_named` / `ui_set_fill`（刷子）、
 *         `ui_rect_grad` / `ui_gradient`（渐变）、`ui_text`（三种锚点）
 *         —— 对应 BGI 的 `setfillstyle` + "自己算颜色插值"那套
 * 出处：自写，仿当年显示适配器测试图（一屏看全所有颜色）。
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
    for (i = 0; i < 16; i++) {
        int g = i * 255 / 15;
        int col = (g << 16) | (g << 8) | g;
        ui_rect(10 + i * 19, 10, 18, 30, col, 1, 0, 0);
    }

    /* ② 线性渐变：左红右蓝（渲染成纯色 = 渐变没生效） */
    ui_rect_grad(10, 50, 140, 40, 1, 0xE06C75, 0x61AFEF, 0, 0);
    ui_rect_grad(170, 50, 140, 40, 1, 0x98C379, 0xE5C07B, 1, 0);   /* 竖向 */

    /* ③ 用刷子填形状：径向渐变圆 */
    ui_brush_radial("r1", 0xFFEEDD, 0x336699, 0, 0, 1);
    ui_set_fill(1);   /* 启用填充（1 = 用当前刷子） */
    ui_circle(80, 150, 45, 0xFFFFFF, 1, 1);

    /* ④ 纯色刷子对照 */
    ui_brush_solid("s1", 0xC678DD);
    ui_rect(150, 110, 60, 80, 0xFFFFFF, 1, 1, 8);

    /* ⑤ 文字三种锚点：左/中/右 —— 三行左端应**不在同一列** */
    ui_text(160, 200, "居中", 0xFFFFFF, 20, 1);
    ui_text(20,  200, "左对齐", 0xFFFFFF, 20, 0);
    ui_text(300, 200, "右对齐", 0xFFFFFF, 20, 2);

    ui_present();
    while (!ui_win_closed())
        ui_wait(&msg, 0);          /* 画完等关窗，别立刻关（快照会没）*/
    ui_win_close();
    return 0;
}
