/* probe4.c —— 把程序**自己算出来的值**用对话框打出来（手机上唯一看得见的输出）。
 *
 * probe3 里 `ui_rect(0, h*3, …)` 那几条整条不见，而 `h*4` 那条在。到底是
 * 「算错了坐标」还是「画了没显示」，得先看程序自己手里的数。
 */
#include <waycoder_ui.h>

char buf[256];
int bl;

void put_int(int v)
{
    char t[12];
    int j = 0;
    if (v < 0) { buf[bl] = '-'; bl = bl + 1; v = -v; }
    if (v == 0) { t[j] = '0'; j = j + 1; }
    while (v > 0 && j < 11) { t[j] = (char)('0' + v % 10); j = j + 1; v = v / 10; }
    while (j > 0) { j = j - 1; buf[bl] = t[j]; bl = bl + 1; }
}

void put_s(char* s)
{
    int i = 0;
    while (s[i] != 0) { buf[bl] = s[i]; bl = bl + 1; i = i + 1; }
}

int main(void)
{
    int m[4];
    int W;
    int H;
    int h;

    W = ui_scr_w();
    H = ui_scr_h();
    h = H / 6;

    bl = 0;
    put_s("W="); put_int(W);
    put_s(" H="); put_int(H);
    put_s(" h="); put_int(h);
    put_s("\n");
    put_s("h*0="); put_int(h * 0);
    put_s(" h*1="); put_int(h * 1);
    put_s("\n");
    put_s("h*2="); put_int(h * 2);
    put_s(" h*3="); put_int(h * 3);
    put_s("\n");
    put_s("h*4="); put_int(h * 4);
    put_s(" h*5="); put_int(h * 5);
    put_s("\n");
    put_s("h/4="); put_int(h / 4);
    put_s(" 2h+h/2="); put_int(h * 2 + h / 2);
    buf[bl] = 0;

    ui_dlg_msg("measure", buf, 0);
    return 0;
}
