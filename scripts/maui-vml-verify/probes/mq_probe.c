/* mq_probe.c —— **消息队列语义探针**（共享宿主层 `VmlHostRuntime` + `VmlMessageQueue`）。
 *
 * ## 为什么是这样一个程序
 *
 * v0.96.326 修了 `VmlMessageQueue` 的信号量账（`Post` 改成**无条件 Release**、
 * `TryRead` 取走时**消费一个许可**）。修之前的状态是「计数 > 0 而队列为空」，
 * 于是下一次 `Read` 会**空唤醒、当场返回 null** —— 程序那边看到的只是
 * "这一拍没有事件"，完全看不出是宿主的账没对上。
 *
 * 判据必须能**判别修没修**，所以先刻意把那个状态造出来：
 * 两个重复定时器先堆若干条消息，程序**先不读**，然后停表、把队列读空。
 * 此时（旧账）信号量计数还留着 ⇒ 紧接着的 `ui_wait(msg, 2000)` 会**秒回 0**；
 * （新账）计数归零 ⇒ 它会真的等到定时器消息到来（dt ≈ 600ms、返回 9）。
 *
 * 输出全部走 stdout（`命令行页` 的输出区），**不靠截图、不靠肉眼看画面** ——
 * 与 `scripts/vmlcli-verify/host_probe.c` 的 `ui_beep` 频率编码同一个思路，
 * 只是手机上没有"宿主原样打印蜂鸣"这条通道，用 stdout 更直接。
 *
 * 读法：每行 `<标号> <字段=值>…`，期望值写在每条的注释里。
 */
#include <waycoder_ui.h>

int msg[4];

int main(void)
{
    int a, b, c, r, dt, n, m1, m2, t0;

    /* ── ① 造出「计数 > 0 而队列为空」的状态 ───────────────────────── */
    a = ui_timer_set(20, 11);
    b = ui_timer_set(35, 22);
    t0 = ui_tick();
    while (ui_tick() - t0 < 260) { r = ui_tick(); }
    ui_timer_kill(a);
    ui_timer_kill(b);
    printf("A queued=%d\n", ui_msg_count());        /* 期望 ≥2 */

    n = 0;
    r = ui_poll(msg);
    while (r != 0) { n = n + 1; r = ui_poll(msg); }
    printf("A drained=%d\n", n);                    /* 期望 = 上面那个 queued */

    /* ── ② 核心判据：读空之后阻塞等待必须**真的等** ──────────────────
       修好 = 9 / b=33 / dt≈600；没修 = 0 / dt≈0（残留许可把 Wait 当场唤醒） */
    msg[0] = 0; msg[1] = 0; msg[2] = 0; msg[3] = 0;
    c = ui_timer_set(600, 33);
    t0 = ui_tick();
    r = ui_wait(msg, 2000);
    dt = ui_tick() - t0;
    ui_timer_kill(c);
    printf("B r=%d b=%d dt=%d\n", r, msg[2], dt);

    /* ── ③ 队列空时超时必须**等满**且如实返回 0 ───────────────────── */
    ui_msg_clear();
    t0 = ui_tick();
    r = ui_wait(msg, 300);
    dt = ui_tick() - t0;
    printf("C r=%d dt=%d\n", r, dt);                /* 期望 0 / ≈300 */

    /* ── ④ ui_poll 不阻塞（非阻塞读，空队列立刻返回 0）────────────── */
    t0 = ui_tick();
    r = ui_poll(msg);
    dt = ui_tick() - t0;
    printf("D r=%d dt=%d\n", r, dt);                /* 期望 0 / ≈0 */

    /* ── ⑤ ui_wait(msg, 0) = 无限等：定时器来了要把它叫醒 ─────────── */
    ui_msg_clear();
    c = ui_timer_set(400, 44);
    t0 = ui_tick();
    r = ui_wait(msg, 0);
    dt = ui_tick() - t0;
    ui_timer_kill(c);
    printf("E r=%d b=%d dt=%d\n", r, msg[2], dt);   /* 期望 9 / 44 / ≈400 */

    /* ── ⑥ TimerKill 之后不再有新消息 ─────────────────────────────── */
    ui_msg_clear();
    t0 = ui_tick();
    while (ui_tick() - t0 < 320) { r = ui_tick(); }
    printf("F count=%d\n", ui_msg_count());         /* 期望 0（表都停了）*/

    /* ── ⑦ 重复定时器：300ms 里 50ms 的表应当投 ~6 条 ─────────────── */
    c = ui_timer_set(50, 88);
    t0 = ui_tick();
    while (ui_tick() - t0 < 310) { r = ui_tick(); }
    ui_timer_kill(c);
    printf("G count=%d\n", ui_msg_count());         /* 期望 4..7 */

    /* ── ⑧ KEEP：只看队头不取走，计数与队头都不变；消费才是它 ─────── */
    ui_msg_clear();
    c = ui_timer_set(50, 77);
    t0 = ui_tick();
    while (ui_tick() - t0 < 180) { r = ui_tick(); }
    ui_timer_kill(c);
    n = ui_msg_count();
    r = ui_poll_ex(msg, 1);
    m1 = msg[2];
    r = ui_poll_ex(msg, 1);
    m2 = msg[2];
    printf("H n=%d keepA=%d keepB=%d tagsame=%d\n", n, ui_msg_count(), ui_msg_count(),
           (m1 == m2) ? 1 : 0);                 /* 期望 n≥2 / 计数不变 / tagsame=1 */
    r = ui_poll(msg);
    printf("H consume=%d after=%d\n", r, ui_msg_count());   /* 期望 9 / n-1 */

    /* ── ⑨ MsgClear 之后账要干净：再等一个 500ms 的表仍然是"真的等" ── */
    ui_msg_clear();
    c = ui_timer_set(500, 99);
    t0 = ui_tick();
    r = ui_wait(msg, 2000);
    dt = ui_tick() - t0;
    ui_timer_kill(c);
    printf("I r=%d b=%d dt=%d\n", r, msg[2], dt);   /* 期望 9 / 99 / ≈500 */

    printf("MQ-DONE\n");
    return 0;
}
