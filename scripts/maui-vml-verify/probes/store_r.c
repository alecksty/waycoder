/* store_r.c —— 存档读取端：**另一个进程**里读 store_w.c 写下的键。 */
#include <waycoder_ui.h>

char buf[64];

int main(void)
{
    int n;
    buf[0] = 0;
    n = ui_store_get("probe.persist", buf, 64);
    printf("R len=%d c0=%d c1=%d c2=%d c3=%d\n", n, buf[0], buf[1], buf[2], buf[3]);
    printf("STORE-R-DONE\n");
    return 0;
}
