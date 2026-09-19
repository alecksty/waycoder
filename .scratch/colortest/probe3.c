/* probe3.c —— 渐变刷子会不会**污染后面的纯色图形**（probe1 里看到 0xFF2A3346 画成了 #272F41）。
 *
 * 六条横带**全是同一个颜色** 0xFF2A3346，只有"画之前经历了什么"不同：
 *   0  基准：此前没有任何渐变                  —— 必须正好是 #2A3346
 *   1  只**定义**过渐变（没使用）
 *   2  用过一次线性 ui_rect_grad 之后
 *   3  再用一次径向 ui_circle_grad 之后
 *   4  中间夹一条空心描边矩形之后
 *   5  最后一条                                 —— 污染是不是"一旦开始就一直在"
 *
 * ⚠ 全程**不调 ui_clear**：它会清空整张图元表，把前面几条带一并抹掉
 *   （probe1 就是这么把自己搞没的）。
 *
 * 六条颜色必须**完全一样**。差一点点（而不是差成渐变里的某个色）说明不是
 * "刷子没清"，而是整体被压暗了一档。
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

    ui_win_open_ex("probe3", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    ui_rect(0, h * 0, W, h, 0xFF2A3346, 1, 0, 0);

    ui_gradient("g1", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_rect(0, h * 1, W, h, 0xFF2A3346, 1, 0, 0);

    ui_rect_grad(0, h * 2, W, h, "g1", 0);
    ui_rect(0, h * 2 + h / 2, W, h / 4, 0xFF2A3346, 1, 0, 0);   /* 叠在渐变带的下半 */

    ui_gradient("g2", 1, 0xFF00FF00, 0xFF000000, 500, 500, 500, 0);
    ui_circle_grad(W / 2, h * 4, h / 3, "g2");
    ui_rect(0, h * 3, W, h / 4, 0xFF2A3346, 1, 0, 0);

    ui_rect(0, h * 4, W, h / 4, 0xFF00FF00, 0, 4, 0);
    ui_rect(0, h * 4 + h / 2, W, h / 4, 0xFF2A3346, 1, 0, 0);

    ui_rect(0, h * 5, W, h, 0xFF2A3346, 1, 0, 0);

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
