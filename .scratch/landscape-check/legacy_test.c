/* 坐标空间自检 —— 验「宿主改了场景尺寸之后，程序的坐标系真的跟着变了」。
 *
 * 判据是**看得见**的：每帧按**当前**可用绘图区画一圈绿边 + 一条对角线。
 *   · 空间跟上了 → 绿边正好贴着画布四边、对角线贯穿整块；
 *   · 没跟上     → 画面被等比缩放/居中留白，绿边离画布边有一截、对角线不够长。
 * 顺带把每次尺寸变化打到 stdout（`RESIZE w h`），与截图互为印证。
 * 每 300ms 重画一次，所以转完屏不用碰屏幕就自动跟上。 */
#include <waycoder_ui.h>

int main(void) {
    int w;
    int h;
    int t;
    int msg[4];

    w = ui_scr_w();
    h = ui_scr_h();
    /* **老接口对照**（VML_WIN_ROTATABLE）⇒ 视口一变宿主就换掉坐标系。
       不声明（用老 ui_win_open）的话宿主不换 —— 那是对老程序的保护，见 space_old.c。 */
    ui_win_open("空间自检", w, h);   /* 老接口：不该被换坐标系 */
    ui_timer_set(300, 7);

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == VML_MSG_WINDOWCLOSE) {
            break;
        }
        if (t == VML_MSG_WINDOWRESIZE) {
            puts("EV resize");
        }
        if (t == VML_MSG_WINDOWORIENT) {
            puts("EV orient");
        }

        /* 每轮都按**此刻**的可用绘图区重画（尺寸变了就自动跟上） */
        w = ui_scr_w();
        h = ui_scr_h();
        ui_clear(0xFF101020);
        ui_rect(2, 2, w - 4, h - 4, 0xFF00FF00, 0, 4, 0);
        ui_line(2, 2, w - 2, h - 2, 0xFF00C8FF, 2);
        ui_line(2, h - 2, w - 2, 2, 0xFFFF8800, 2);
        ui_present();
    }
    return 0;
}
