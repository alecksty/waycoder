/* probe2.c —— 颜色标尺：十条已知色的实心横带，用来量"画出来的像素"与"给的颜色"差多少。
 *
 * 一条带一个颜色，从上到下。截屏后逐条采样，与左边的期望值比 ——
 * 差值是**均匀的**说明是整体色彩变换（gamma/色彩空间），
 * 只在某几条上差说明是通道序或缓存问题。
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
    h = H / 10;

    ui_win_open_ex("probe2", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    ui_rect(0, h * 0, W, h, 0xFFFFFFFF, 1, 0, 0);   /* 白 */
    ui_rect(0, h * 1, W, h, 0xFF808080, 1, 0, 0);   /* 灰 */
    ui_rect(0, h * 2, W, h, 0xFFFF0000, 1, 0, 0);   /* 红 */
    ui_rect(0, h * 3, W, h, 0xFF00FF00, 1, 0, 0);   /* 绿 */
    ui_rect(0, h * 4, W, h, 0xFF0000FF, 1, 0, 0);   /* 蓝 */
    ui_rect(0, h * 5, W, h, 0xFF2A3346, 1, 0, 0);   /* 计算器数字键 */
    ui_rect(0, h * 6, W, h, 0xFF9E4630, 1, 0, 0);   /* 计算器功能键（砖红） */
    ui_rect(0, h * 7, W, h, 0xFF7A5A28, 1, 0, 0);   /* 计算器运算符（暗金） */
    ui_rect(0, h * 8, W, h, 0xFF2E7DD1, 1, 0, 0);   /* 计算器等号（亮蓝） */
    ui_rect(0, h * 9, W, h, 0xFF1B2233, 1, 0, 0);   /* 计算器背景 */

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
