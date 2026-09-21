// 动态内存与内存函数：malloc / free / memset / memcpy
//
// curses 库要自己开屏缓冲（`malloc(rows * cols * sizeof(cell))`）、
// 清屏用 `memset`、滚屏用 `memcpy` —— 三条都得能用。
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

int main()
{
    char * p = malloc(16);
    memset(p, 65, 15);
    p[15] = 0;
    // ⚠ 这里**不打印** `p` 本身 —— 堆指针过 `%s` 会打出 `(null)`（缺陷，单独一条
    // 用例 `07-heap-str.c` 钉着）。内容本身是对的（下一行的 strlen 就是证据）。
    printf("M2=%d\n", strlen(p));
    printf("M2B=%c\n", p[0]);

    char buf[16];
    memcpy(buf, "hello", 6);
    printf("M3=%s\n", buf);

    int * nums = malloc(12);
    nums[0] = 11;
    nums[1] = 22;
    nums[2] = 33;
    printf("M4=%d\n", nums[0] + nums[2]);
    free(nums);
    free(p);
    printf("M5=%d\n", 1);
    return 0;
}
// EXPECT: M2=15|M2B=A|M3=hello|M4=44|M5=1
