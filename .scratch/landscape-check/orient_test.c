/* 方向接口自检 —— 验两件事：
 *   ① `ui_orientation()`（#569）在开窗前就能问，且与画布形状一致；
 *   ② 转屏时**收到** `VML_MSG_WINDOWORIENT`（新方向）与 `VML_MSG_WINDOWRESIZE`。
 * 输出走 stdout（命令行页直接看得到）。 */
#include <waycoder_ui.h>

int main(void) {
    int o;
    int w;
    int h;
    int t;
    int msg[4];

    o = ui_orientation();
    w = ui_scr_w();
    h = ui_scr_h();

    if (o == VML_ORIENT_LANDSCAPE) { puts("A orient=LANDSCAPE"); } else { puts("A orient=PORTRAIT"); }
    if (o == 1) { puts("A raw=1"); } else { puts("A raw=0"); }
    if (w > h) { puts("A wh=WIDE"); } else { puts("A wh=TALL"); }

    ui_win_open("方向自检", w, h);
    puts("B window-open");

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == VML_MSG_WINDOWORIENT) {
            if (msg[1] == VML_ORIENT_LANDSCAPE) { puts("C msg-orient=LANDSCAPE"); } else { puts("C msg-orient=PORTRAIT"); }
        } else if (t == VML_MSG_WINDOWRESIZE) {
            puts("C msg-resize");
        } else if (t == VML_MSG_WINDOWCLOSE) {
            break;
        }
    }
    puts("D bye");
    return 0;
}
