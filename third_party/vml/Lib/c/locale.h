/* locale.h —— 本平台只有**一种 locale**（C / POSIX），但常量与函数得在
 *
 * ## 为什么需要它（以及为什么它是"空实现"）
 *
 * 老程序 `#include <locale.h>` 基本只为一件事：**`setlocale(LC_ALL, "")`** ——
 * 让 `strftime`/`toupper` 之类跟着环境走。手机上**没有"用户 locale"这个概念**
 * （界面语言是 App 自己的事，与 C 运行时的 locale 无关），所以这里：
 *
 *   · `setlocale` **恒返回 "C"** —— 老程序拿它判"设置成没成功"，给个真字符串
 *     比给 NULL 安全（给 NULL 的话不少程序会走"locale 不可用"的降级分支，
 *     反而把界面弄成英文）。
 *   · 常量（`LC_ALL`/`LC_TIME` …）**必须有** —— tty-clock 就是拿 `LC_TIME` 去调
 *     `setlocale` 的，少了它**编译都过不去**。
 *
 * 真做本地化（`strftime` 的 `%A` 按语言给"星期一"）是另一件事，等真有程序要再说。
 */
#ifndef _LOCALE_H
#define _LOCALE_H

#param lib("util")

#define LC_ALL      0
#define LC_COLLATE  1
#define LC_CTYPE    2
#define LC_MONETARY 3
#define LC_NUMERIC  4
#define LC_TIME     5
#define LC_MESSAGES 6

#define LC_MIN LC_ALL
#define LC_MAX LC_MESSAGES

struct lconv {
    char *decimal_point;
    char *thousands_sep;
    char *grouping;
};

char *setlocale(int category, const char *locale);
struct lconv *localeconv(void);

#endif /* _LOCALE_H */
