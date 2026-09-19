/* probe6.c —— 绘图接口的**颜色普查**：每个能上色的调用各画一格，颜色全部取同一个
 * 0xFF3C6EB4（r/g/b 三个通道互不相同，通道序错了、被别的刷子接管了一眼就能看出）。
 *
 * 3 列 × 5 行，每格 131×130，图形居中、尽量填满格子，采样点取格心。
 * 最后三格是**回归格**：在用过渐变之后再画纯色，必须还是 0xFF3C6EB4。
 */
#include <waycoder_ui.h>

#define C 0xFF3C6EB4

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
    int j;
    int x;
    int y;

    W = ui_scr_w();
    H = ui_scr_h();
    cw = W / 3;
    ch = H / 5;

    ui_win_open_ex("probe6", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* 0 实心矩形 */
    ui_rect(cxx(0) - 55, cyy(0) - 55, 110, 110, C, 1, 0, 0);

    /* 1 圆角矩形 */
    ui_rect(cxx(1) - 55, cyy(0) - 55, 110, 110, C, 1, 0, 14);

    /* 2 实心圆 */
    ui_circle(cxx(2), cyy(0), 55, C, 1, 0);

    /* 3 实心椭圆 */
    ui_ellipse(cxx(0), cyy(1), 58, 45, C, 1, 0);

    /* 4 实心多边形（三角） */
    tri[0] = cxx(1);      tri[1] = cyy(1) - 50;
    tri[2] = cxx(1) - 55; tri[3] = cyy(1) + 50;
    tri[4] = cxx(1) + 55; tri[5] = cyy(1) + 50;
    ui_polygon(tri, 3, C, 0, 0, 0);

    /* 5 粗折线（开口，横穿格子） */
    ui_polyline(tri, 3, C, 30, 0);

    /* 6 粗直线 */
    ui_line(cxx(0) - 55, cyy(2), cxx(0) + 55, cyy(2), C, 40);

    /* 7 路径填充（三角） */
    ui_path("M 10 100 L 65 10 L 120 100 Z", C, 0, C, 0, 0, 0);

    /* 8 路径描边（粗） */
    ui_path("M 140 100 L 195 10 L 250 100 Z", 0, 24, C, 0, 0, 0);

    /* 9 空心矩形（粗描边，填充透明） */
    ui_rect(cxx(2) - 55, cyy(2) - 55, 110, 110, C, 0, 30, 0);

    /* 10 线性渐变（对照，本来就该是渐变） */
    ui_gradient("g6", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_rect_grad(cxx(0) - 55, cyy(3) - 55, 110, 110, "g6", 0);

    /* 11 **回归格**：用过渐变之后再画纯色 */
    ui_rect(cxx(1) - 55, cyy(3) - 55, 110, 110, C, 1, 0, 0);

    /* 12 **回归格**：径向渐变之后再画纯色圆 */
    ui_gradient("g7", 1, 0xFF00FF00, 0xFF000000, 500, 500, 500, 0);
    ui_circle_grad(cxx(2), cyy(3), 55, "g7");
    ui_circle(cxx(2), cyy(3) - 40, 40, C, 1, 0);

    /* 13 回归格：再往后一格 */
    ui_rect(cxx(0) - 55, cyy(4) - 55, 110, 110, C, 1, 0, 0);

    /* 14 收尾对照 */
    ui_circle(cxx(1), cyy(4), 50, C, 1, 0);

    /* 15 文字（大号粗体，采样点落在笔画上） */
    ui_set_font(90, 1, C, 1);
    ui_text_cur(cxx(2), cyy(4) - 20, "H");

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
