/* keydiag.c —— 把按键链上最后两个未知量直读出来：
 *   W  = ui_win_closed()（0 开着 / 1 关掉了 / 2 从没开过窗口）
 *   T  = ui_wait(msg, 300) 带 300ms 超时：应为 0（超时）而不是立刻返回别的
 *   F  = 不按键调一次 getch()：旧行为会给 0，正确行为应当**卡住**
 */
#include <graphics.h>
#include <conio.h>

int main(void)
{
    int gd = DETECT, gm;
    int msg[4];
    int t0, dt;

    initgraph(&gd, &gm, NULL);
    print_str("W="); print_int(ui_win_closed()); print_str("\n");

    t0 = ui_tick();
    print_str("T="); print_int(ui_wait(msg, 300));
    dt = ui_tick() - t0;
    print_str(" dt="); print_int(dt); print_str("\n");

    print_str("READY\n");
    print_str("F="); print_int(getch()); print_str("\n");
    return 0;
}
