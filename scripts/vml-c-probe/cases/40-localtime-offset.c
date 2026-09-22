// **`localtime` 不是"本地"时间 —— 它和 `gmtime` 一模一样**
//
// ## 症状
//
// `tty-clock` 出钟之后，**小时差整整 8 小时**（本地 19:11 显示成 11:11），
// 而分/秒/日期**全对** —— 因为 UTC+8 是**整小时**，只有小时那一项被推掉。
// 这个形态最容易看成"程序自己算错了"，实际是库里根本没用时区。
//
// ## 根因：`_ts_to_tm` 是纯 UTC 换算，而 `localtime` 直接转调它
//
// `Lib/shared/src/util.c` 里 `_ts_to_tm` 是把 Unix 时间戳拆成 `struct tm` 的整数算法
// （`civil_from_days`），**输出的是 UTC**；而 `gmtime` 当时写的是"本平台无时区，两者相同"
// ⇒ `localtime(t)` **就是** `gmtime(t)`。`#54 GetDateTime` 给的又确实是**与地区无关**的
// 时间戳（本来就该如此），所以整条链上没有任何一处知道"本地在哪"。
//
// ## 修法：不动 `#54`，**新增 `#61 GetUtcOffset`**
//
// 本仓规矩「**新能力一律走新号**」（老号加参数 = 静默的未定义行为：宿主从寄存器读，
// 而老程序后面那几只寄存器里是它自己上一句留下的值）。所以：
//   · `#61` → R0 = 本地时区偏移（秒，东为正）；
//   · `localtime` 加它、`gmtime` **不加**（两者必须分开，否则"用哪个都一样"把这号的意义抹掉）；
//   · ⚠ 新号**必须登记进 `SyscallConstants.UserAllowed`** —— 用户态只放行本表的号，
//     漏了会打 `Permission denied: syscall 61 requires kernel mode` 并把 R0 置成错误码。
//
// ## 两条判据纪律（都踩过）
//
// · **`gmtime`/`localtime` 返回的是同一个静态缓冲**（标准 C 行为）⇒ 想同时比两个结果，
//   必须**每次调用后立刻把字段抄进局部变量**。直接留两个指针会让它们指向同一个对象，
//   于是"逐字段相等"恒成立、判据**看起来全绿其实什么都没测**。
// · 判据取**两个互相独立的来源**：① `gmtime` 与手算核对过的 UTC 值比；
//   ② `#56 GetTimeString` 给的是**宿主自己的**本地时间（`DateTime.Now`，"HH:mm:ss"）——
//   拿它的前两位和 `localtime(NULL)` 的小时比。一条走"宿主格式化"、一条走"库的整数算法"，
//   任何一边坏都会露；**这正是"钟面小时差 8 小时"的形状**。
// STDIN:
// EXPECT: GY=2026|GM=9|GD=22|GH=11|GMI=11|GS=48|GWD=2|E=1|OFFOK=1|HOK=1
#include <stdio.h>
#include <time.h>

/* 宿主自己的本地时间串（`#56 GetTimeString` → "HH:mm:ss"）。
   实现体 `get_time()` 在 `Lib/shared/src/sysinfo.c` 里，但**没进任何头文件**，
   探针自己拿一次即可（`asm` 必须是**表达式**，见 vmlui.c 头部那条）。 */
static const char *host_time_string(void) { return asm("SYSCALL 56"); }

int main()
{
    time_t fixed = 1790075508;          /* 2026-09-22 11:11:48 UTC（python 核对过） */
    int off = (int)asm("SYSCALL 61");   /* 本地时区偏移（秒，东为正） */
    struct tm *tp;
    const char *hs;
    int host_hour;
    struct tm *now;
    int same;

    /* 三个结果分开存（调用后立刻抄走，见头部纪律①） */
    int gy, gmo, gd, gh, gmi, gsec, gwd;
    int ly, lmo, ld, lh, lmi, lsec;
    int sy, smo, sd, sh, smi, ssec;

    /* ── ① gmtime 是纯 UTC（手算核对，与宿主时区无关） ── */
    tp = gmtime(&fixed);
    gy = tp->tm_year; gmo = tp->tm_mon; gd = tp->tm_mday;
    gh = tp->tm_hour; gmi = tp->tm_min; gsec = tp->tm_sec; gwd = tp->tm_wday;

    printf("GY=%d\n", gy + 1900);
    printf("GM=%d\n", gmo + 1);
    printf("GD=%d\n", gd);
    printf("GH=%d\n", gh);
    printf("GMI=%d\n", gmi);
    printf("GS=%d\n", gsec);
    printf("GWD=%d\n", gwd);            /* 2026-09-22 是周二 ⇒ 2（C 的 tm_wday 周日为 0） */

    /* ── ② 契约：`localtime(t)` == `gmtime(t + 本地偏移)`，逐字段 ── */
    tp = localtime(&fixed);
    ly = tp->tm_year; lmo = tp->tm_mon; ld = tp->tm_mday;
    lh = tp->tm_hour; lmi = tp->tm_min; lsec = tp->tm_sec;

    { time_t shifted = fixed + off;
      tp = gmtime(&shifted);
      sy = tp->tm_year; smo = tp->tm_mon; sd = tp->tm_mday;
      sh = tp->tm_hour; smi = tp->tm_min; ssec = tp->tm_sec; }

    same = (ly == sy) && (lmo == smo) && (ld == sd)
        && (lh == sh) && (lmi == smi) && (lsec == ssec);
    printf("E=%d\n", same ? 1 : 0);

    /* ── ③ 偏移本身要像个真时区：整刻钟、在 ±14 小时以内 ── */
    printf("OFFOK=%d\n", (off % 900 == 0 && off >= -50400 && off <= 50400) ? 1 : 0);

    /* ── ④ 与**宿主自己的**本地时间对账（独立来源） ── */
    hs = host_time_string();
    host_hour = (hs[0] - '0') * 10 + (hs[1] - '0');
    now = localtime(NULL);
    if (host_hour != now->tm_hour)
    {
        /* 跨小时那一秒的竞态：重取一次再判 */
        hs = host_time_string();
        host_hour = (hs[0] - '0') * 10 + (hs[1] - '0');
        now = localtime(NULL);
    }
    printf("HOK=%d\n", host_hour == now->tm_hour ? 1 : 0);
    return 0;
}
