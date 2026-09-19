/* probe5.c —— 二分之一：渐变刷子到底在哪一步把后面的**实心矩形**弄没了。
 *
 * probe3 的现象：用了 `ui_rect_grad` 之后，后面的实心 `ui_rect` 一条都不出现
 * （而空心描边那条正常）。probe4 已证明**坐标算得没错**（h*3=324 等），
 * 所以是画的那一侧丢的。这里按"哪一步之后开始丢"切六段：
 *
 *   0  ui_rect 实心（此前没碰过渐变）       —— 基准
 *   1  ui_gradient 只**定义**、不使用
 *   2  ui_rect 实心（定义之后）
 *   3  ui_rect_grad 用一次渐变
 *   4  ui_rect 实心（用过之后）             —— 嫌疑最大
 *   5  ui_rect 实心（再往后一条）
 *
 * 六条都在，颜色才叫对；从哪一条开始变黑，就是哪一步开始坏的。
 */
#include <waycoder_ui.h>

int main(void)
{
    int m[4];
    int W;
    int H;
    int h;

    W = ui_scr_w();
    H = ui_scr_h();
    h = H / 6;

    ui_win_open_ex("probe5", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    ui_rect(0, h * 0, W, h, 0xFF2A3346, 1, 0, 0);        /* 0 基准 */

    ui_gradient("g", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);   /* 只定义 */

    ui_rect(0, h * 1, W, h, 0xFF2A3346, 1, 0, 0);        /* 1 定义之后 */

    ui_rect_grad(W / 4, h * 2 + h / 4, W / 2, h / 2, "g", 0);     /* 用一次 */

    ui_rect(0, h * 2, W, h, 0xFF2A3346, 1, 0, 0);        /* 2 用过之后 */

    ui_rect(0, h * 3, W, h, 0xFF2A3346, 1, 0, 0);        /* 3 */

    ui_rect(0, h * 4, W, h, 0xFF2A3346, 1, 0, 0);        /* 4 */

    ui_rect(0, h * 5, W, h, 0xFF2A3346, 1, 0, 0);        /* 5 */

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
