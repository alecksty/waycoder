/* store_w.c —— 存档写入端；与 store_r.c 配对，验**跨运行**（杀掉进程再起）读得到。 */
#include <waycoder_ui.h>

char buf[64];

int main(void)
{
    int n;
    ui_store_set("probe.persist", "run1");
    n = ui_store_get("probe.persist", buf, 64);
    printf("W len=%d c0=%d\n", n, buf[0]);      /* 期望 4 / 114（'r'） */
    printf("STORE-W-DONE\n");
    return 0;
}
