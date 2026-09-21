// `dos.h`（Turbo C / MS-DOS 那套）的判据。
//
// ## 为什么单独一条
//
// 这个头里的函数**全是 `void`**（`delay`/`sound`/`nosound`/`getdate`/`gettime`），
// 返回值那条路本来就判不了 —— 只能判**副作用**：
//
//   · `delay` 的副作用是"**时间真的过去了**" ⇒ 拿 `get_tick()` 当表夹着量。
//     ⚠ 这条非有不可：`delay` 写成空操作（或调用参数丢了）时**不报错**，
//       而老程序的节奏全靠它（`sound(440); delay(100); nosound();`）。
//   · `getdate`/`gettime` 的副作用是"**把真实时钟写进那个结构体**"。
//     ⚠ 这里踩过两次：先是拿 `asm("STORE ...")` 编译不过，改对写法后又按
//       **打包整数**做位段解析（`>>16`/`>>8`/`&0xFF`）—— 实测 `#55` 给的是
//       `"2026-09-21"` 的**字符串地址**。所以判据只认**语义**（年/月/日落在
//       合理区间），换个解析方式实现只要对就绿。
//
// ## 判据为什么这么"松"（只判区间不判具体值）
//
// 日期时间**每次跑都不一样**，逐字节比对没法写。但"松"在这里是**够用的**：
// 返回值丢了（0）、解析错位（年份变 9、月份变 21）、结构体布局对不上（字段串位）
// 这三种真实故障，**全都会掉出区间**。
//
// ## 音效：桌面验不了，如实说明
//
// `sound`/`nosound` 走 `#57`/`#542`，而桌面宿主 `CliVmlHost.Tone()` 只写一行日志、
// `StopAudio()` 只打一行 —— **不出声**（这正是 `#57` 当初"调了没反应"的桌面镜像）。
// 所以这里只验"调用不崩"，听感要真机。**别把"没崩"当成"音效对了"。**
#include <stdio.h>
#include <dos.h>

extern int get_tick(void);   /* builtins.c — SYSCALL #53，当秒表用 */

int main()
{
    struct date d;
    struct time t;
    int t0;
    int t1;

    /* ① `delay` 真的等了 —— 夹在两次读表之间 */
    t0 = get_tick();
    delay(150);
    t1 = get_tick();
    printf("\nW=%d", t1 - t0 >= 100);

    /* ② 日期：真实时钟（返回值丢了会得 0，解析错位会掉出区间） */
    getdate(&d);
    printf("\nY=%d", d.da_year >= 2020 && d.da_year <= 2100);
    printf("\nM=%d", d.da_mon >= 1 && d.da_mon <= 12);
    printf("\nD=%d", d.da_day >= 1 && d.da_day <= 31);

    /* ③ 时间同理 */
    gettime(&t);
    printf("\nH=%d", t.ti_hour <= 23);
    printf("\nS=%d", t.ti_sec <= 59);

    /* ④ 版本（老程序拿它判分支） */
    printf("\nV=%d", DosVersion() == 0x0700);

    /* ⑤ 喇叭：只验"不崩"（桌面无声，听感要真机） */
    sound(440);
    nosound();

    return 0;
}
// EXPECT: W=1|Y=1|M=1|D=1|H=1|S=1|V=1
