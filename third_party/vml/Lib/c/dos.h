/* dos.h —— Turbo C / MS-DOS 的那套 `dos.h`（**能移植的那部分**）
 *
 * ## 为什么第二个做它
 *
 * 按"老程序里出现得多不多"排的（与 `conio.h` 同一条规矩）：GitHub 代码搜索里含
 * `#include <dos.h>` 的 C 文件 **49,792** 个，紧跟在 `conio.h`(110,336) 后面**排第 2**，
 * 而且它与 conio **天然配套** —— DOS 文本程序几乎都是
 * `#include <conio.h>` + `#include <dos.h>` 一起用的（`gotoxy` 画界面、`delay` 控节奏、
 * `sound` 出音效）。
 *
 * ## 底线：**只做"能映射到本平台"的**，x86 专属的一律不做（写在明处）
 *
 * | 做 | 为什么能 | 底层 |
 * |---|---|---|
 * | `delay` / `sleep` | 就是等 | `SYSCALL #52` |
 * | `sound` / `nosound` | PC 喇叭 = 方波，本平台能合成 | `SYSCALL #57` / `#542` |
 * | `getdate` / `gettime` | 宿主有真实时钟 | `SYSCALL #55` / `#56` |
 *
 * | **不做** | 为什么 |
 * |---|---|
 * | `geninterrupt` / `int86` / `intdos` / `int86x` | **x86 软中断**，本平台没有中断向量表这回事 |
 * | `keep` / `_dos_keep` | TSR（常驻内存）—— 手机上没有"后台常驻的 DOS 进程" |
 * | `dosexterr` / `_doserrno` | DOS 错误码表，与 POSIX `errno` 不同源 |
 * | `setvect` / `getvect` | 中断向量，同 `geninterrupt` |
 * | `struct REGS` / `union REGS` | 只为上面那几个中断调用服务 |
 *
 * ## ⚠ `sound()` 与 DOS 的**语义差别**（这条最容易踩）
 *
 * DOS 的 `sound(f)` 是「**开始**持续发声，一直响到 `nosound()`」—— 状态是**保持**的。
 * 而本平台的音效原语是「**响一段固定时长**」（`#57` 的第二个参数，宿主侧上限 5000ms）。
 * 这里取**近似**：`sound(f)` = 起一个**尽量长（5 秒）** 的音，`nosound()` = 立刻掐掉。
 *
 * 对老程序的**实际影响很小**：它们几乎都是
 * `sound(440); delay(100); nosound();` 这样"响一下就关"——
 * 只要在 5 秒内 `nosound()`，听感与 DOS **完全一致**。
 * 只有"`sound()` 之后一直不关"的写法会在 5 秒后自己停（DOS 上会一直响）。
 *
 * ## 时间：`SYSCALL #55`/`#56` 返回的是**字符串指针**，不是打包整数
 *
 * ⚠ 踩过一次：`Lib/shared/src/dos.c` 里那版 `GetDate`/`GetTime`（Turbo Pascal 风格）
 * 把返回值当**打包整数**做位段解析（`>>16` / `>>8` / `&0xFF`）——
 * 实测 `#55` 给的是 `"2026-09-21"` 的**地址**、`#56` 给的是 `"23:00:44"` 的**地址**。
 * 本头的两个函数按**字符串**解析（`atoi` 从各字段的固定偏移处取）。
 */
#ifndef _DOS_H
#define _DOS_H

#param lib("dos")

/* ── Turbo C 的 struct date / struct time（字段名照抄，源码兼容） ── */
struct date {
    int  da_year;      /* 1980 - 2099 */
    char da_day;       /* 1 - 31 */
    char da_mon;       /* 1 - 12 */
};

struct time {
    unsigned char ti_min;
    unsigned char ti_hour;
    unsigned char ti_hund;   /* 百分秒（本平台恒 0） */
    unsigned char ti_sec;
};

/* ── 延时 ── */
void delay(unsigned ms);          /* 毫秒 */
void sleep(unsigned seconds);     /* 秒（包一层 delay） */

/* ── PC 喇叭（⚠ 语义差见文件头：`sound` 最长响 5 秒） ── */
void sound(unsigned freq);        /* 频率 Hz（宿主钳在 20–20000） */
void nosound(void);               /* 立刻停 */

/* ── 日期时间（⚠ 真实时钟，不是"打包整数"） ── */
void getdate(struct date *d);
void gettime(struct time *t);

/* ── 版本 ── */
#define DOS_VER_MAJOR 7
#define DOS_VER_MINOR 0
int DosVersion(void);             /* 恒 0x0700（"DOS 7.0"，老程序拿它判分支） */

#endif /* _DOS_H */
