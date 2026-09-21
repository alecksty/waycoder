/* crash_probe.c —— 把「开窗之后、VM 线程轮询屏幕查询」这条路切成两段，用**存档**记进度。
 *
 * 存档（Preferences）是**崩溃后仍然读得到**的通道（`adb shell cat /data/.../shared_prefs/*.xml`），
 * 而崩溃时 stdout 随进程一起没了 —— 所以阶段标记必须写进存档，不能只 printf。
 *
 *   阶段 1  开窗后**只** ui_poll/ui_tick           → 记 "2-poll-ok"
 *   阶段 2  再加 ui_scr_w/ui_scr_h/ui_orientation  → 记 "3-scr-ok"
 *
 * 崩在哪一段，存档就停在哪一段的前一个标记上。
 */
#include <waycoder_ui.h>

int msg[4];

int main(void)
{
    int r, t0, i, w, h, o;

    ui_store_set("probe.phase", "1-enter");
    r = ui_win_open_ex("崩溃探针", 300, 400, VML_WIN_ROTATABLE, VML_WIN_NEED_GAMEPAD);
    ui_store_set("probe.phase", "2-opened");

    /* ── 阶段 1：只取消息 + 时钟（对照：这一遍不该出任何事）── */
    t0 = ui_tick();
    while (ui_tick() - t0 < 5000) { r = ui_poll(msg); }
    ui_store_set("probe.phase", "2-poll-ok");

    /* ── 阶段 2：再加上屏幕查询（可疑的那三个号）── */
    w = 0; h = 0; o = 0;
    t0 = ui_tick();
    while (ui_tick() - t0 < 5000) {
        w = ui_scr_w(); h = ui_scr_h(); o = ui_orientation();
        r = ui_poll(msg);
    }
    ui_store_set("probe.phase", "3-scr-ok");

    ui_win_close();
    ui_store_set("probe.phase", "4-done");
    printf("w=%d h=%d o=%d\n", w, h, o);
    printf("CRASH-PROBE-DONE\n");
    return 0;
}
