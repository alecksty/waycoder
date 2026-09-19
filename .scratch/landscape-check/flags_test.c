/* 开窗声明自检（`ui_win_open_ex` #570）——
 *   ① 不要手柄区：屏幕上不该有方向键/XYAB/SELECT/START，也不该有"▲ 收起手柄"那条；
 *   ② 锁死方向：转屏后窗口尺寸**不该变**（syscall 里量到的 w/h 两次一样）。
 * 同时把拿到的 w/h 打到 stdout —— 与"要手柄"的那份对比，应当明显更大（吃满整屏）。 */
#include <waycoder_ui.h>

int main(void) {
    int w;
    int h;
    int w2;
    int h2;
    int t;
    int msg[4];

    w = ui_scr_w();
    h = ui_scr_h();
    puts("F open-ex: fixed-orient + no-gamepad");

    ui_win_open_ex("声明自检", w, h, VML_WIN_FIXED_ORIENT, VML_WIN_NO_GAMEPAD);
    ui_timer_set(400, 3);

    /* 开窗后再问一次：尺寸应当与开窗前一致（程序据此排版，中途不该跳） */
    w2 = ui_scr_w();
    h2 = ui_scr_h();
    if (w2 == w && h2 == h) {
        puts("F size-stable-at-open");
    } else {
        puts("F size-CHANGED-at-open");
    }

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == VML_MSG_WINDOWCLOSE) {
            break;
        }
        if (t == VML_MSG_WINDOWRESIZE) {
            puts("F UNEXPECTED-resize（锁了方向不该收到）");
        }

        w = ui_scr_w();
        h = ui_scr_h();
        ui_clear(0xFF102030);
        ui_rect(2, 2, w - 4, h - 4, 0xFF00FF00, 0, 4, 0);
        ui_line(2, 2, w - 2, h - 2, 0xFF00C8FF, 2);
        ui_line(2, h - 2, w - 2, 2, 0xFFFF8800, 2);
        ui_present();
    }
    return 0;
}
