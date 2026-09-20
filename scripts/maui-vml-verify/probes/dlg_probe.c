/* dlg_probe.c —— 对话框 / 闹钟暂停 / 存档 / 屏幕 四组**在真实设备上**的探针。
 *
 * 判据全部走 stdout（命令行页的输出区），由驱动回读 —— 不靠截图。
 * 期望值写在每行注释里，跑完与 `scripts/maui-vml-verify/probes/expect.txt` 对照。
 */
#include <waycoder_ui.h>

char buf[128];
char opts[16];

/* #552 宿主**实现了**，但 `Lib/shared/src/vmlui.c` 与 `Lib/c/waycoder_ui.h` 都没有包装函数
   ⇒ 22 门语言一个都调不到它。这里用内联 asm 直接发号，把宿主那一侧单独验掉。 */
void probe_store_del(char* key) { asm("SYSCALL #552, ${key}"); }

int main(void)
{
    int r, n, t0, c;

    /* ── ① 屏幕：可用绘图区 + 方向（与 CALLJSON 的 screen 同源）── */
    printf("S1 scr w=%d h=%d orient=%d\n", ui_scr_w(), ui_scr_h(), ui_orientation());

    /* ── ② 存档：写 → 读长度 → 读内容 → 读没有的键 → 删 → 再读 ── */
    ui_store_set("probe.k", "hi");
    n = ui_store_get("probe.k", buf, 128);
    printf("S2 store len=%d c0=%d c1=%d\n", n, buf[0], buf[1]);   /* 期望 2 / 104 / 105 */
    n = ui_store_get("probe.none", buf, 128);
    printf("S2 none=%d\n", n);                                     /* 期望 -1 */
    probe_store_del("probe.k");
    n = ui_store_get("probe.k", buf, 128);
    printf("S2 del=%d\n", n);                                      /* 期望 -1 */

    /* 选项块 = NUL 分隔的三项（宿主按 StrBlock 拆） */
    opts[0] = 'A'; opts[1] = 0;
    opts[2] = 'B'; opts[3] = 0;
    opts[4] = 'C'; opts[5] = 0;

    /* ── ③ 消息框（驱动点「允许」⇒ 期望 0）── */
    r = ui_dlg_msg("探针", "D1 消息框：点允许", 0);
    printf("D1 msg ret=%d\n", r);

    /* ── ④ 选择框（驱动点「C」⇒ 期望 2）── */
    r = ui_dlg_select("探针", "D2 选择框：点 C", opts, 3, 0);
    printf("D2 select ret=%d\n", r);

    /* ── ⑤ 多选框（驱动勾 A、C 再确定 ⇒ 期望 1|4 = 5）── */
    r = ui_dlg_multi("探针", "D3 多选框：勾 A 和 C", opts, 3);
    printf("D3 multi ret=%d\n", r);

    /* ── ⑥ 输入框（驱动输入 VML 再确定 ⇒ 期望 3 / 86 77 76 = "VML"）── */
    buf[0] = 0;
    r = ui_dlg_input("探针", "D4 输入框：输入 VML 再确定", buf, 128);
    printf("D4 input ret=%d c0=%d c1=%d c2=%d\n", r, buf[0], buf[1], buf[2]);   /* 期望 3 / 86 77 76 */

    /* ── ⑦ 弹框期间定时器必须**暂停** ──────────────────────────────
       50ms 的重复表 + 一个"驱动会等 3 秒才点"的消息框。
       没暂停 ⇒ 3 秒里堆 ~60 条；暂停 ⇒ ≤1 条。
       弹完必须**恢复走表** ⇒ 再等 260ms 应当有 ≥3 条。 */
    c = ui_timer_set(50, 55);
    r = ui_dlg_msg("探针", "D5 定时器暂停：请等 3 秒再点允许", 0);
    printf("D5 paused queued=%d\n", ui_msg_count());     /* 期望 ≤1 */
    t0 = ui_tick();
    while (ui_tick() - t0 < 260) { n = ui_tick(); }
    printf("D5 resumed queued=%d\n", ui_msg_count());    /* 期望 ≥3 */
    ui_timer_kill(c);
    ui_msg_clear();

    printf("DLG-DONE\n");
    return 0;
}
