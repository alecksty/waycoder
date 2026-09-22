/* old_std_primes.c —— 素数筛（命令行的，数组 + 循环）
 *
 * 类别：std
 * 兼容面：**大数组**（静态 1 万个元素）、嵌套循环、`memset(..., sizeof(数组))`
 *         这条最常见的 C 惯用法、用「每 10 个换行」这种老式排版
 * 出处：自写，仿《C 程序设计语言》里那种筛法练习。
 *
 * ⚠ 本文件**刻意保持老程序的原样写法**（`sizeof(flags)` 而不是显式长度）——
 *   它就是用来钉 `sizeof(<数组>)` 这条的：修复前该处返回元素大小（4），
 *   于是 `memset` 只清 4 个字节、筛法一个素数都筛不出（实测「共 2 个」）。
 *   详见 `third_party/vml/FRONTEND_DEFECTS.md`。
 */
#include <stdio.h>
#include <string.h>

#define LIMIT 10000

/* ⚠ 放**静态区**而不是栈上：老程序里这种大表都是全局的 ——
 *   一来当年栈很小，二来这正是"全局数组"那条兼容面（局部大数组另有一条）。 */
static char flags[LIMIT + 1];

int main(void)
{
    int i, j, count = 0;

    memset(flags, 1, sizeof(flags));
    flags[0] = flags[1] = 0;

    for (i = 2; i * i <= LIMIT; i++) {
        if (!flags[i]) continue;
        for (j = i * i; j <= LIMIT; j += i)
            flags[j] = 0;
    }

    printf("%d 以内的素数：\n", LIMIT);
    for (i = 2; i <= LIMIT; i++) {
        if (!flags[i]) continue;
        count++;
        printf("%5d", i);
        if (count % 10 == 0) printf("\n");   /* 老式排版：每 10 个换行 */
    }
    if (count % 10 != 0) printf("\n");

    printf("共 %d 个\n", count);
    return 0;
}
