// `dos.h` 的 `sleep`（**单位是秒**）与 `time.h` 的历法函数。
//
// ## 为什么 `sleep` 单独一条，而不是并进 `18-dos.c`
//
// `sleep` 这个名字在本库里**被定义了三遍**：
//
//     Lib/shared/src/builtins.c :  void sleep(int ms)  { SYSCALL #52 }   ← **毫秒**
//     Lib/shared/src/dos.c      :  void sleep(seconds) { delay(seconds*1000) } ← **秒**
//     Lib/shared/src/util.c     :  （同名族）
//
// 而 `Lib/c/dos.h` 声明的语义是**秒**（Turbo C 的 `sleep`）。
// 链接期会挑哪一个，**从源码上读不出来** —— 只有量时间才能分辨。
// 而"挑错了"的症状是**安静地快了 1000 倍**：不报错、不崩，只是老程序里
// `sleep(2)` 从"停 2 秒"变成"停 2 毫秒"，界面节奏整个塌掉。
//
// 所以判据做成**两侧夹逼**（不是"等于 1000"）：
//
//   `sleep(1)` ⇒ 秒语义 ≈1000ms、毫秒语义 ≈1ms
//     ⇒ `>= 900` 的判据能**同时**排除"毫秒语义"与"空操作"
//   `delay(200)` ⇒ 毫秒语义 ≈200ms、秒语义 ≈200000ms
//     ⇒ `>= 150 且 < 3000` 能排除"秒语义"与"没等"
//
// （实测 `sleep(1)` 得 **1000**、`delay(200)` 得 **210** —— 与两条推导都吻合。
//  写成区间是因为这类判据在负载机器上会抖，而本用例要卡的是**量级**不是**数值**。）
//
// ## `time.h` 那半边：判据只认**区间**，且**压字段顺序**
//
// `localtime`/`gmtime` 填的 `struct tm` **不是**由头文件驱动的（实现文件不 include
// 头，靠约定对齐字段 —— 见 `util.c` 里那段注释）。字段一旦串位，症状同样是"不报错
// 但值离谱"。几条判据各管一段：
//
//   · `tm_year`  ∈ [124, 200] ⇒ 同时否掉"年份写成 0/未填"与"月日串进年"
//   · `tm_mon`   ∈ [0, 11]    ⇒ 否掉"写成 1..12"（差一的经典病）
//   · `tm_mday`  ∈ [1, 31]    ⇒ 否掉"月份串进日"
//   · `tm_hour/min/sec` 各自上界 ⇒ 否掉"时间串进日期"
//   · `gmtime` 与 `localtime` 的**年份一致**（本平台无时区，两者应同源）
#include <stdio.h>
#include <dos.h>
#include <time.h>

extern int get_tick(void);   /* builtins.c — SYSCALL #53，当秒表用 */

int main()
{
    tm_t *lt;
    tm_t *gt;
    time_t now;
    int    t0;

    /* ① `sleep` 是**秒**（>900ms 才排除得掉"毫秒语义"与"空操作"） */
    t0 = get_tick();
    sleep(1);
    printf("\nA=%d", get_tick() - t0 >= 900);

    /* ② 反过来，`delay` 是**毫秒**（<3000ms 才排除得掉"秒语义"） */
    t0 = get_tick();
    delay(200);
    printf("\nB1=%d", get_tick() - t0 >= 150);
    printf("\nB2=%d", get_tick() - t0 < 3000);

    /* ③ `time()` 给的是 epoch 秒（本平台 > 1e9 = 2001 年之后） */
    now = time(&now);
    printf("\nC=%d", now > 1000000000);
    /* 传 0 指针也要能用（老程序 `srand(time(0))` 就这个形状） */
    printf("\nD=%d", time(0) >= now);

    /* ④ `struct tm` 的**字段顺序** —— 串位会掉出区间。
          ⚠ 先读进局部量再调 `gmtime`：两者多半返回**同一个 static 缓冲**
          （实现里是共用一个），先调再读会把 `localtime` 的结果覆盖掉，
          于是"两者一致"那两条变成恒真、判不出东西。 */
    lt = localtime(&now);
    int ly = lt->tm_year;
    int lm = lt->tm_mon;
    int ld = lt->tm_mday;
    int lh = lt->tm_hour;
    int lmin = lt->tm_min;
    int lsec = lt->tm_sec;

    printf("\nE=%d", ly >= 124 && ly <= 200);
    printf("\nF=%d", lm >= 0 && lm <= 11);
    printf("\nG=%d", ld >= 1 && ld <= 31);
    printf("\nH=%d", lh <= 23);
    printf("\nI=%d", lmin <= 59);
    printf("\nJ=%d", lsec <= 60);

    /* ⑤ 本平台没有时区 ⇒ `gmtime` 与 `localtime` 同年同月同日
          （读的是上一步的**副本**，不是被覆盖后的缓冲） */
    gt = gmtime(&now);
    printf("\nK=%d", gt->tm_year == ly);
    printf("\nL=%d", gt->tm_mday == ld);

    return 0;
}
// EXPECT: A=1|B1=1|B2=1|C=1|D=1|E=1|F=1|G=1|H=1|I=1|J=1|K=1|L=1
