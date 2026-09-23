/* keyraw.c —— **绕过 getch**，直接把消息队列里的原始消息打出来。
 * 用来分辨"垫层翻译错"还是"上游发来的就不对"。
 * 打出：序号 = 返回码  t=消息类型  a=载荷A  b=载荷B
 *   类型：1=KeyDown 2=KeyUp 10=WindowClose 11=WindowResize …
 */
#include <graphics.h>

int main(void)
{
    int gd = DETECT, gm;
    int msg[4];
    int i, r;

    initgraph(&gd, &gm, NULL);
    print_str("RAW 就绪，按键…\n");

    for (i = 0; i < 14; i++)
    {
        r = ui_wait(msg, 0);
        print_str("R"); print_int(i);
        print_str("="); print_int(r);
        print_str(" t="); print_int(msg[0]);
        print_str(" a="); print_int(msg[1]);
        print_str(" b="); print_int(msg[2]);
        print_str("\n");
    }
    print_str("RAW 结束\n");
    return 0;
}
