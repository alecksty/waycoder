/* keyfirst.c —— **一个键都不按**，只调一次 getch()，看它返不返回。
 * 判别：垫层遇到"非按键消息"（窗口尺寸变化 / KeyUp）时是**跳过**还是**返回 0**。
 *   · 跳过（新语义）  ⇒ 这里**什么都不打**（一直等）
 *   · 返回 0（旧语义）⇒ 立刻打出 `F=0`
 */
#include <graphics.h>
#include <conio.h>

int main(void)
{
    int gd = DETECT, gm;
    int c;

    initgraph(&gd, &gm, NULL);
    print_str("READY（不按键，看下面出不出 F=）\n");
    c = getch();
    print_str("F="); print_int(c); print_str("\n");
    closegraph();
    return 0;
}
