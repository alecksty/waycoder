// DOS / Turbo C 文本控制台（`conio.h`）—— 光标的**坐标模型**判据
//
// 为什么先钉坐标模型：`conio` 的语义与 POSIX 那套**不一样**，而差别全在"1 起还是 0 起"
// 与"越界怎么办"这种细节上 —— 老程序把它们当契约用（`gotoxy(1,1)` 就是左上角、
// 屏幕是 80×25）。差一格，菜单边框整条错位，而且**不会报错**。
//
// 判据怎么取：`wherex()` / `wherey()` 把内部状态读出来，再用 `printf` 打到 stdout
// （stdout 是"程序自己的输出"，驱动脚本逐字节比对它；而 `conio` 画的是窗口，看不见）。
#include <stdio.h>
#include <conio.h>

int main()
{
    clrscr();

    // ① 定位是 **1 起**（DOS 语义）；左上角 = (1,1)
    gotoxy(10, 5);
    printf("\nP1=%d,%d\n", wherex(), wherey());

    // ② 输出推进光标：`cprintf` 4 个字符 ⇒ 列 +4
    textcolor(YELLOW);
    textbackground(BLUE);
    cprintf("menu");
    printf("\nP2=%d,%d\n", wherex(), wherey());

    // ③ `cputs` 同理
    cputs("XY");
    printf("\nP3=%d,%d\n", wherex(), wherey());

    // ④ 回到左上角
    gotoxy(1, 1);
    printf("\nP4=%d,%d\n", wherex(), wherey());

    // ⑤ 越界要**钳制**，不是崩溃也不是回绕（老程序会拿它当"到底了"用）
    gotoxy(200, 200);
    printf("\nP5=%d,%d\n", wherex(), wherey());
    gotoxy(0, 0);
    printf("\nP6=%d,%d\n", wherex(), wherey());

    // ⑥ 写到第 80 列后再写一个字符 ⇒ 自动折到下一行**行首**
    //
    // ⚠ 这里**分两拍量**：只量最后一次的话，「折行点在 80 还是 81」看不出来。
    //   而且第一版我把期望写成了 `1,2`（两个 putch 之后）—— **是我写错了**：
    //   折行只发生一次，第二个字符落在新一行并把它推进到第 2 列。写成 `1,2` 的话
    //   实现里"少折一次"和"多折一次"这两种错都会碰巧对上同一个值，判据就废了。
    gotoxy(80, 1);
    putch('A');
    printf("\nP7a=%d,%d\n", wherex(), wherey());
    putch('B');
    printf("\nP7b=%d,%d\n", wherex(), wherey());

    // ⑦ 颜色状态机：`textattr` 一个字节里高 4 位背景、低 4 位前景；
    //    `highvideo`/`lowvideo` 只动前景的加亮位（这些读不回来，靠后续输出不崩 + 真机观感）
    textattr(0x1E);
    highvideo();
    lowvideo();
    normvideo();
    printf("\nP8=ok\n");

    return 0;
}
// EXPECT: P1=10,5|P2=14,5|P3=16,5|P4=1,1|P5=80,25|P6=1,1|P7a=1,2|P7b=2,2|P8=ok
