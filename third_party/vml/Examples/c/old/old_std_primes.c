/* old_std_primes.c —— 素数筛（命令行的，数组 + 循环）
 * old_std_primes.c -- prime sieve (command line, arrays + loops)
 *
 * 类别：std
 * Category: std
 * 兼容面：**大数组**（静态 1 万个元素）、嵌套循环、`memset(..., sizeof(数组))`
 * Compatibility: a **large array** (10,000 static elements), nested loops, and `memset(..., sizeof(array))`
 *         这条最常见的 C 惯用法、用「每 10 个换行」这种老式排版
 *         (that most common C idiom), plus old-style layout such as "a newline every 10 items"
 * 出处：自写，仿《C 程序设计语言》里那种筛法练习。
 * Origin: self-written, imitating the sieve exercise found in "The C Programming Language".
 *
 * ⚠ 本文件**刻意保持老程序的原样写法**（`sizeof(flags)` 而不是显式长度）——
 * ⚠ This file **deliberately keeps the old program's original style** (`sizeof(flags)` instead of an explicit length) --
 *   它就是用来钉 `sizeof(<数组>)` 这条的：修复前该处返回元素大小（4），
 *   it exists to pin down the `sizeof(<array>)` item: before the fix that spot returned the element size (4),
 *   于是 `memset` 只清 4 个字节、筛法一个素数都筛不出（实测「共 2 个」）。
 *   so `memset` cleared only 4 bytes and the sieve found no primes at all (measured: "2 in total").
 *   详见 `third_party/vml/FRONTEND_DEFECTS.md`。
 *   See `third_party/vml/FRONTEND_DEFECTS.md` for details.
 */
#include <stdio.h>
#include <string.h>
#include <waycoder_ui.h>     /* 只为 ui_get_language() —— 老程序不该猜系统语言 */
                            /* only for ui_get_language() -- an old program must not guess the system language */

#define LIMIT 10000

/* ⚠ 放**静态区**而不是栈上：老程序里这种大表都是全局的 ——
 * ⚠ Put it in **static storage** rather than on the stack: large tables like this were always global in old programs --
 *   一来当年栈很小，二来这正是"全局数组"那条兼容面（局部大数组另有一条）。
 *   first, the stack was tiny back then; second, this is exactly the "global array" compatibility surface (large local arrays are a separate one).
 */
static char flags[LIMIT + 1];

int main(void)
{
    int i, j, count = 0;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();

    memset(flags, 1, sizeof(flags));
    flags[0] = flags[1] = 0;

    for (i = 2; i * i <= LIMIT; i++) {
        if (!flags[i]) continue;
        for (j = i * i; j <= LIMIT; j += i)
            flags[j] = 0;
    }

    printf(lang == 0 ? "%d 以内的素数：\n" : "Primes up to %d:\n", LIMIT);
    for (i = 2; i <= LIMIT; i++) {
        if (!flags[i]) continue;
        count++;
        printf("%5d", i);
        if (count % 10 == 0) printf("\n");   /* 老式排版：每 10 个换行 */
                                             /* Old-style layout: a newline every 10 items */
    }
    if (count % 10 != 0) printf("\n");

    printf(lang == 0 ? "共 %d 个\n" : "%d in total\n", count);
    return 0;
}
