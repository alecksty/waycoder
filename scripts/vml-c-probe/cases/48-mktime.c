// `mktime`：**结构体 → epoch 秒**，也就是 `localtime` 的逆。
//
// ## 为什么单独立一条
//
// `Lib/c/time.h:39` **声明了**它，而全 Lib **一处实现都没有**。
//
// ⚠ 判据的来源是**生成物**、不是头文件：前端认不认这个函数，看的是
//   `Lib/c/shared_bindings.h`（GenLib 从 `shared/src/*.c` 扫出来的导出清单）。
//   实测形态是**编译期**报错、而且**指到调用那一行**：
//
//       <input>:88: error: 未定义的函数 'mktime'（引用 2 次）
//
//   —— 也就是说"光在 time.h 里声明"不算数，**必须**在 `shared/src/*.c` 里实现
//   并重跑 `GenLib -A`。（这条纠正了本文件初版的一句想当然：当时写的是
//   "链接期报错、位置对不上"，实测不是。）
//
// 老程序用它做**日期加减**，靠的正是 C 规定的"越界字段会被归一化"：
//
//     t.tm_mday += 1;  mktime(&t);      /* 明天（跨月跨年都对） */
//
// ⇒ 判据里必须有一条**真的越界**，只测"原样填回"是测不出归一化的。
//
// ## 判据全部设计成**与宿主时区无关**
//
// `mktime` 按**本地**时间解释结构体（`localtime` 的逆），所以绝对时间戳会随
// 宿主时区变（本机 CST+8、CI 可能 UTC）—— 直接断言"mktime(2020-01-01) == 1577836800"
// 会在别的机器上红，而那是**判据的错**不是实现的错。
// 这里只用两种与机器无关的量：
//   ① **往返**（同一台机器上 `localtime`/`mktime` 用同一个偏移，来回必须还原）；
//   ② **差值**（`mday += 7` ⇒ 秒数正好多 7×86400；本平台的时区偏移是固定的 `#61`，
//      没有夏令时，所以这个等式是精确的）。
#include <time.h>

int main()
{
    time_t ts0;
    time_t ts1;
    time_t tsb;
    tm_t *lt;
    tm_t t;
    int sec0;

    ts0 = 1600000000;      /* 2020-09-13 12:26:40 UTC，随手挑的固定值 */

    /* ① 往返：拆开 → 原样填回 → 必须还原出同一个时间戳。
          ⚠ `localtime` 返回的是**它自己的 static 缓冲**，必须先读出来再动 mktime。 */
    lt = localtime(&ts0);
    sec0 = lt->tm_sec;
    t.tm_sec   = lt->tm_sec;
    t.tm_min   = lt->tm_min;
    t.tm_hour  = lt->tm_hour;
    t.tm_mday  = lt->tm_mday;
    t.tm_mon   = lt->tm_mon;
    t.tm_year  = lt->tm_year;
    t.tm_wday  = lt->tm_wday;
    t.tm_yday  = lt->tm_yday;
    t.tm_isdst = lt->tm_isdst;

    ts1 = mktime(&t);
    print_str("RT=");
    print_int(ts1 == ts0 ? 1 : 0);

    /* ② `mktime` 的另一半契约：把 `*tp` 的字段**归一化后写回**，
          其中包括 `tm_wday`/`tm_yday`（C 标准：这些值由 mktime 设置）。
          与 `localtime` 对**同一个** ts 的说法比 —— 这样不引入新的时区假设。 */
    print_str(" WD=");
    print_int(t.tm_wday == lt->tm_wday ? 1 : 0);
    print_str(" YD=");
    print_int(t.tm_yday == lt->tm_yday ? 1 : 0);

    /* ③ 日期加减：`mday += 7` ⇒ 正好多 7 天（越界由 mktime 归一化掉） */
    t.tm_mday = lt->tm_mday + 7;
    tsb = mktime(&t);
    print_str(" D7=");
    print_int((int)(tsb - ts0) == 7 * 86400 ? 1 : 0);

    /* ④ `mktime` **不许踩 `localtime` 的静态缓冲**。
          `lt` 还指着 `_tm_buf`，上面已经调过三次 mktime —— 它要是借用那个缓冲
          去算 `tm_wday`，程序手里这个 `lt` 就被悄悄改掉了（实测的形态：
          `lt = localtime(&now); ... mktime(&copy);` 之后 `lt` 变了）。 */
    print_str(" MB=");
    print_int(lt->tm_sec == sec0 ? 1 : 0);

    print_str("\n");
    return 0;
}

// EXPECT: RT=1 WD=1 YD=1 D7=1 MB=1
