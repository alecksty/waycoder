/* keycodes.c —— 把 `getch()` 的返回值**原样打到 stdout**（命令行页能看到）。
 *
 * 为什么要有它：设备上"按键没反应"，而像素反推（画条再量宽）既慢又容易看错。
 * 直接打数值，一眼就知道垫层到底给了什么：
 *   · 新版语义（方向键应先 0、再扫描码）：按 → 应打出 `0` 然后 `77`
 *   · 旧版语义（返回消息类型）        ：会打出 `1`（VML_MSG_KEYDOWN）
 *   · 直接给扫描码                    ：只打出 `77`
 */
#include <graphics.h>
#include <conio.h>

int main(void)
{
    int gd = DETECT, gm;
    int c;
    int n = 0;

    initgraph(&gd, &gm, NULL);
    print_str("KEYPROBE 就绪，按键…\n");

    for (n = 0; n < 12; n++)
    {
        c = getch();
        print_str("K"); print_int(n); print_str("="); print_int(c); print_str("\n");
        if (c == 'q' || c == 'Q') break;
    }

    print_str("KEYPROBE 结束\n");
    closegraph();
    return 0;
}
