/* keyloop.c —— 把 `_con_getch_window` 里那个循环**原样抄一遍**，每轮都打一行。
 * 判读：遇到非按键消息后应当**继续循环**（iter 递增），直到来一个真按键。
 *   若 iter1 打完就 end ⇒ 这个"跳出/继续"的形状在这个编译器+平台上不成立。
 */
#include <graphics.h>

int main(void)
{
    int gd = DETECT, gm;
    int msg[4];
    int n = 0;
    int r;

    initgraph(&gd, &gm, NULL);
    print_str("LOOP begin\n");

    for (;;)
    {
        n = n + 1;
        r = ui_wait(msg, 0);
        print_str(" iter"); print_int(n);
        print_str(" r="); print_int(r);
        print_str(" t="); print_int(msg[0]);
        print_str(" a="); print_int(msg[1]); print_str("\n");

        if (r <= 0) break;
        if (msg[0] == 1) break;      /* KEYDOWN ⇒ 出循环 */
        /* 非按键 ⇒ 继续下一轮 */
    }

    print_str("LOOP end n="); print_int(n); print_str("\n");
    return 0;
}
