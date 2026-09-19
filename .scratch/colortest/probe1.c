/* probe1.c —— 颜色走样的最小复现：**渐变刷子会不会污染后面的纯色图形**。
 *
 * 分成六条横带，每条只差"画之前有没有先定义/使用过渐变刷子"：
 *   A 纯色（前面什么都没有）           —— 基准
 *   B 渐变矩形（用 "g"）               —— 渐变本身
 *   C 纯色（紧跟在渐变矩形之后）        —— 嫌疑最大的一条
 *   D 纯色（再跟一条）
 *   E 纯色（先 ui_clear 再画）          —— 清场能不能救回来
 *   F 纯色（中间夹一条描边矩形）
 *
 * 每条带用同一个颜色 0xFF2A3346（就是计算器数字键那个色），
 * 只要有一条跟别的**看起来不一样**，就是它。
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
    h = H / 7;

    ui_win_open_ex("probe1", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* A：基准（0xFF2A3346） */
    ui_rect(0, 0, W, h, 0xFF2A3346, 1, 0, 0);

    /* 定义并使用一个**线性**渐变刷子 */
    ui_gradient("g", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_rect_grad(0, h, W, h, "g", 0);

    /* C：紧跟渐变之后的纯色 */
    ui_rect(0, h * 2, W, h, 0xFF2A3346, 1, 0, 0);

    /* D：再来一条 */
    ui_rect(0, h * 3, W, h, 0xFF2A3346, 1, 0, 0);

    /* E：先清场再画 */
    ui_clear(0xFF000000);
    ui_rect(0, h * 4, W, h, 0xFF2A3346, 1, 0, 0);

    /* F：中间夹一条空心描边矩形 */
    ui_rect(0, h * 5, W, h, 0xFF00FF00, 0, 4, 0);
    ui_rect(0, h * 6, W, h, 0xFF2A3346, 1, 0, 0);

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
