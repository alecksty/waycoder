/* probe7.c —— 渐变刷子普查：每个**能引用刷子**的形状各来一格，看渐变是不是真的铺上去了。
 *
 * 线性刷子一律 红(左) → 蓝(右)，径向刷子一律 红(心) → 蓝(边)。
 * 判据不是"有没有颜色"，而是**同一个形状里左右两端是不是不同的色** ——
 * 整个刷子塌成纯色时（几何换算错了、被别的刷子接管了）两端会一样。
 *
 * 3 列 × 4 行：
 *   0 矩形(直角)    1 矩形(圆角)    2 圆
 *   3 多边形        4 路径(填充)    5 路径(多子路径挖洞)
 *   6 折线(**预期不生效**，见下)     7 椭圆(无渐变入口，纯色对照)  8 实心矩形再补一个刷子
 *   9 径向:矩形     10 径向:圆      11 回归格(渐变之后画纯色)
 *
 * ⚠ 折线那一格是**故意的**：`ui_polyline` 的最后一个参数虽然叫 grad，
 *   但折线只有描边没有填充面，矢量后端也没有"描边刷子"这个能力 ——
 *   参数会被静默丢掉。这一格用来**把这个事实钉住**（它不该出现渐变）。
 */
#include <waycoder_ui.h>

int W;
int H;
int cw;
int ch;

int tri[8];

int cxx(int col) { return col * cw + cw / 2; }
int cyy(int row) { return row * ch + ch / 2; }

int main(void)
{
    int m[4];

    W = ui_scr_w();
    H = ui_scr_h();
    cw = W / 3;
    ch = H / 4;

    ui_win_open_ex("probe7", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_clear(0xFF000000);

    /* 线性刷子（左右方向）与径向刷子，各定义一次 */
    ui_gradient("gl", 0, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
    ui_gradient("gr", 1, 0xFFFF0000, 0xFF0000FF, 500, 500, 500, 0);

    /* 0 矩形（直角）+ 线性 */
    ui_rect_grad(cxx(0) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, "gl", 0);

    /* 1 矩形（圆角）+ 线性 */
    ui_rect_grad(cxx(1) - cw / 3, cyy(0) - ch / 3, cw * 2 / 3, ch * 2 / 3, "gl", 14);

    /* 2 圆 + 线性 */
    ui_circle_grad(cxx(2), cyy(0), ch / 3, "gl");

    /* 3 多边形 + 线性（参数序：pts, count, fill, stroke, width, grad） */
    tri[0] = cxx(0);          tri[1] = cyy(1) - ch / 3;
    tri[2] = cxx(0) - cw / 3; tri[3] = cyy(1) + ch / 3;
    tri[4] = cxx(0) + cw / 3; tri[5] = cyy(1) + ch / 3;
    ui_polygon(tri, 3, 0, 0, 0, "gl");

    /* 4 路径 + 线性（参数序：d, stroke, width, fill, grad, cap, dash） */
    ui_path("M 10 10 L 120 10 L 120 100 L 10 100 Z", 0, 0, 0, "gl", 0, 0);

    /* 5 路径（多子路径挖洞）+ 线性 —— 奇偶规则，中间必须是空的 */
    ui_path("M 140 10 L 250 10 L 250 100 L 140 100 Z M 175 35 L 215 35 L 215 75 L 175 75 Z",
            0, 0, 0, "gl", 0, 0);

    /* 6 折线 + 线性 —— **预期不生效**（见文件头） */
    tri[0] = cxx(1) - cw / 3; tri[1] = cyy(2) + ch / 4;
    tri[2] = cxx(1);          tri[3] = cyy(2) - ch / 4;
    tri[4] = cxx(1) + cw / 3; tri[5] = cyy(2) + ch / 4;
    ui_polyline(tri, 3, 0xFF00FF00, 16, 0);

    /* 7 椭圆 —— **没有渐变入口**，纯色对照 */
    ui_ellipse(cxx(2), cyy(2), cw / 3, ch / 4, 0xFF00FF00, 1, 0);

    /* 8 实心矩形（纯色）—— 排在两个刷子用过之后，必须还是原色 */
    ui_rect(cxx(0) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, 0xFF3C6EB4, 1, 0, 0);

    /* 9 径向：矩形 */
    ui_rect_grad(cxx(1) - cw / 3, cyy(3) - ch / 3, cw * 2 / 3, ch * 2 / 3, "gr", 0);

    /* 10 径向：圆 */
    ui_circle_grad(cxx(2), cyy(3), ch / 3, "gr");

    ui_present();
    while (ui_win_closed() == 0) {
        ui_wait(m, 500);
    }
    return 0;
}
